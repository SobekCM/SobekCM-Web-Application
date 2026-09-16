# Rate Limiting and Crawler Controls

Every layer ships **disabled** (`Enabled: false`, `ManualMode: "None"`). All settings are read **once at startup**, so a change needs an app restart. The exceptions are the GCS URL lifetimes, which are database settings.

| Layer | Keyed on | Counts | Windows | When over |
|---|---|---|---|---|
| 1. Burst limiter | exact IP | item views | 30 s | IP banned, HTTP 429 |
| 1b. Range ban | /16 (IPv4), /32 (IPv6) | overlapping burst bans | while bans overlap | whole range banned for hours, HTTP 429 |
| 2. JP2 zoom budget | /24 subnet | zoom viewer opens | hour, day | zoom hidden, redirect to JPEG/citation |
| 2b. JP2 circuit breaker | whole site | zoom viewer opens | hour | zoom off for everyone |
| 3. Sustained item budget | /24 subnet | item views | hour, day | whole item page replaced by a message |
| 4a. Robot pause | whole site | item views | hour | identified robots get 503 for item pages |
| 4b. Login-only mode | whole site | item views | hour | items (or the whole site) need a logon |

Logged-on users are **never exempt**. They get higher ceilings, because a logon is only a cookie, and a cookie can be exported into a scraper. The subnet budgets count anonymous and logged-on traffic **separately**, so anonymous traffic can never get logged-on users blocked.

---

## Shared building blocks

- **Code location:** the gateways, `ClientSubnetKey` and `AnonymousRequest` live in `SobekCM_Core/RateLimiting` (namespace `SobekCM.Core.RateLimiting`). The log file name constants stay in `MemoryMgmt/LogFile_Names`, since `exceptions.txt` shares them.
- **Subnet key:** IPv4 is masked to /24 (`a.b.c.0/24`), IPv6 to /48. It's computed once per request in `UserIpInitializer` and stored in `RequestCache_Keys.UserSubnetKey`.
  - Read it with `ClientSubnetKey.From(context)`.
  - The masking algorithm is `ClientSubnetKey.For(ip)`, which only `UserIpInitializer` should call.
  - `SobekCM_ImageServer` keeps its own copy of the same algorithm (`subnet_key_for`), since it has no reference to Core.
- **Client IP** comes from `Connection.RemoteIpAddress`. `X-Forwarded-For` / `X-Forwarded-Proto` are honored only from proxies listed in `ForwardedHeaders:TrustedProxies` (main app) or `ImageServer:TrustedProxies`. Both lists are empty by default.
  - **Why empty:** production is IIS in-process with IIS terminating TLS, so the connection address is already the real client.
  - **Why it matters:** trusting those headers from any source would let a client choose its own IP. That dodges every limiter, gets another IP banned, or claims an IP-restricted range.
  - **Only list a real proxy** that sits in front of IIS and overwrites those headers.
- **Logged-on test:** `AnonymousRequest.Is_Logged_On(user)`.
- **Storage:** `SharedCache` holds the counters, in memory and **per server**. Counters reset on an app restart or recycle and aren't shared across a web farm.
- **Windows and lockouts:** counting windows are fixed; each starts when its counter is created, and more hits don't extend it.
  - **Lockout from the trigger:** when a subnet budget's count reaches a limit, that subnet is locked out right then, for requests with the same logon status.
  - **Lockout length:** `HourlyLockoutMinutes` (default 60) for an hourly limit, `DailyLockoutHours` (default 24) for a daily one.
  - **Afterwards:** the counter that tripped is cleared (both, for a daily lockout), so counting starts fresh when the lockout ends.
  - **Burst limiter:** its `BanMinutes` ban already works this way.
- **Recording:** `ItemViewRateLimitInitializer` records the burst and login-only hits for `Item_Display` and `Item_Print` requests. The two subnet budgets only count what was actually served, so they record after their own check: the item-view budget in `Item_HtmlSubwriter` / `Print_Item_HtmlSubwriter`, the JP2 budget in `JPEG2000_ItemViewer`.
- **Logging:** every limiter writes to `temp/ratelimiting.txt` through `RateLimitLog_Gateway`. The file names are constants in `LogFile_Names`.
  - **Format:** one tab-separated line per event: time, event, `anonymous` or `logged on`, IP (burst ban) or subnet (everything else), details, user agent.
  - **User agent:** the one request that tripped the event, so it isn't necessarily typical of the traffic behind it. `UserIpInitializer` caches it in the request cache, and `RateLimitingMiddleware` hands `RateLimitLog_Gateway` a delegate to read it, since `SobekCM_Core` can't see the `HttpContext`. A file created before this column existed keeps its old header line.
  - **Central monitoring database:** when `Monitoring:ConnectionString` is set, events go to `Monitoring_RateLimit_Event` instead, and the file is only the fallback. See `Database/SQL/Monitoring/README.md`.
  - **Events:** `BURST BAN`, `RANGE BAN`, `ROBOT PAUSE`, `JP2 ZOOM BUDGET`, `JP2 CIRCUIT BREAKER`, `ITEM VIEW BUDGET`, `LOGIN-ONLY FUSE`.
  - **Only the moment something trips:** a ban, a subnet lockout starting (once per lockout), a site-wide fuse. Requests turned away afterwards aren't logged, so a crawler can't flood the file.
  - **Site-wide fuses:** the address is whichever request happened to cross the threshold, not a culprit.
  - **Off switch:** `RateLimiting:LoggingEnabled: false` turns off all of it. Manual modes are settings, so they're never logged.

## 1. Burst limiter (predates this work)

Code: `RateLimiting_Gateway`, `RateLimitingMiddleware`, `ItemViewRateLimitInitializer`.

```json
"RateLimiting": { "Enabled": false, "RequestLimit": 30, "LoggedOnRequestLimit": 60,
                  "WindowSeconds": 30, "BanMinutes": 10, "LoggingEnabled": true,
                  "RangeBanThreshold": 4, "RangeBanHours": 8 }
```

- **Counting:** item views per exact IP, with **separate counters** for anonymous and logged-on requests. A shared counter would let an anonymous crawler on a NAT'd network get a patron banned.
- **Over the limit:** the whole IP is banned for `BanMinutes`. The middleware checks for a ban before any other work and returns **429** with `Retry-After`. The ban starts on the IP's *next* request.
- **Exempt:** loopback, every IP in the Engine restriction ranges, and `/health`.
- **Logging:** each new ban goes to `temp/ratelimiting.txt` as `BURST BAN`, with the exact IP.
- **Range ban:** when `RangeBanThreshold` (4) different IPs from the same /16 (IPv4) or /32 (IPv6) have burst bans running at the same time, the whole range is banned for `RangeBanHours` (8).
  - **Why:** it answers a coordinated crawl spread across one provider's addresses. The incident had 18 IPs from two 3xK Tech GmbH ranges burst-banned within 5 seconds.
  - **Trigger:** overlapping bans, not traffic volume, so ordinary visitors sharing a big range almost never set it off.
  - **429 page:** says access from the visitor's *network* is blocked, and doesn't suggest logging on, since logged-on visitors in the range are blocked too.
  - **Log:** `RANGE BAN`, with the range and the IPs that triggered it. Exempt IPs are never banned, even inside a banned range.
  - **Off switch:** `RangeBanThreshold: 0`.

## 2. JP2 zoom budget + circuit breaker

Code: `JP2RateLimiting_Gateway`, `JPEG2000_ItemViewer(_Prototyper).Budget_Exceeded`.

```json
"JP2RateLimiting": { "Enabled": false, "HourlyLimit": 30, "DailyLimit": 100,
                     "LoggedOnHourlyLimit": 60, "LoggedOnDailyLimit": 300,
                     "HourlyLockoutMinutes": 60, "DailyLockoutHours": 24,
                     "SiteWideHourlyThreshold": 2000, "CircuitBreakerHours": 1,
                     "ManualDisable": false }
```

- **Counting:** zoom viewer opens per subnet, with **separate counters** for anonymous and logged-on requests, each against its own ceiling. A hit is recorded only when the viewer actually renders.
- **Over budget:**
  - the "Zoomable" menu link is hidden, and the JPEG viewer's page image stops linking to zoom (along with its "switch to zoomable" prompt)
  - in place of that prompt, the JPEG viewer shows a short notice that zoom is temporarily unavailable, but only on pages that have a JP2:
    - **anonymous budget used up:** includes a log-on link that returns to the zoomable view, since logged-on visitors are counted separately
    - **logged-on budget used up:** "from your network, please try again later"
    - **circuit breaker:** no log-on suggestion, since it applies to everyone
    - **robots:** no notice
  - a direct zoom URL redirects to the **JPEG viewer for the same page** if that page has a JPG, otherwise to the **citation**
  - robots always get the same redirect, and never get the JPEG viewer's zoom link
- **Circuit breaker** (applies to everyone, logged on or not):
  - **Automatic:** once zoom opens across the whole site reach `SiteWideHourlyThreshold` in an hour, zoom turns off for `CircuitBreakerHours` (default **1 hour**) and clears itself. The trip is logged as `JP2 CIRCUIT BREAKER`.
  - **Manual:** `ManualDisable: true` never expires, and needs a restart to turn on and another to turn off. It works even when `Enabled` is false, which only turns off the budget and the automatic fuse.
- **No enforcement in the ImageServer is needed.** Only the main app can mint a `/render` token, so blocking the viewer blocks the fetch.

## 3. Sustained item-view budget

Code: `SustainedRateLimiting_Gateway`, `Item_HtmlSubwriter.Write_HTML`, `Print_Item_HtmlSubwriter.Write_HTML`.

```json
"SustainedRateLimiting": { "Enabled": false, "HourlyLimit": 400, "DailyLimit": 2500,
                           "LoggedOnHourlyLimit": 1200, "LoggedOnDailyLimit": 7500,
                           "HourlyLockoutMinutes": 60, "DailyLockoutHours": 24 }
```

- **Aimed at the crawler that never bursts:** one request every 1.5 s stays under the burst limit forever but adds up to about 2,400 an hour.
- **Why item views:** full-size images reach the browser as **signed GCS URLs** that never touch the app, so the item view that mints the URL is what gets counted.
- **Counting:** item views per subnet, with **separate counters** for anonymous and logged-on views, each against its own ceiling. Only views actually served count. A crawler that keeps hitting the message adds nothing, and stays blocked for the whole lockout.
- **Over budget:** the **entire** item display, citation included, becomes "Temporary Item Rate Limit Reached", with a log-on link for anonymous visitors. The print page gets a single line. No 429.

## 4. Login-only mode

Code: `LoginOnlyMode_Gateway`, `LoginOnlyModeInitializer`, plus both item subwriters.

```json
"LoginOnlyMode": { "Enabled": false, "RobotItemHitsPerHourThreshold": 5000, "RobotPauseHours": 2,
                   "ItemHitsPerHourThreshold": 10000, "FuseHours": 2, "ManualMode": "None" }
```

- **Robot level (automatic, first):** once item views across the whole site reach `RobotItemHitsPerHourThreshold`, identified robots get **HTTP 503** with `Retry-After` for item pages, for `RobotPauseHours`. The trip is logged as `ROBOT PAUSE`.
  - **Why 503:** crawlers read it as "temporarily overloaded" and slow down without dropping pages from their index; 404/403 would risk deindexing.
  - **People see nothing,** and robots can still crawl the home page, aggregations and search pages.
  - **Paused robot requests aren't counted,** so the crawl stops pushing the total toward the items level below. That's the main point of this level.
  - **Robots also get the 503 whenever items require a logon** (fuse or manual), since a crawler can't log on and shouldn't index the logon message.
  - Enforced in `RobotItemPauseInitializer`, before the hit counting.
- **Items level:**
  - **Automatic:** once item views across the whole site (all users) reach `ItemHitsPerHourThreshold` in an hour, anonymous visitors see "Log On to View Items" for `FuseHours`, then it clears itself. The trip is logged as `LOGIN-ONLY FUSE`.
  - **Manual:** `ManualMode: "Items"`.
  - This is checked **before** the sustained budget, so the visitor sees the real reason.
- **Site level (manual only):** `ManualMode: "Site"` sends every anonymous **page** request to the logon screen, which returns the visitor to the original URL after a successful logon.
  - **Still reachable:** mySobek (logon, register, OIDC/SAML), contact, error, cache reload/reset, legacy redirects.
  - **Data feeds stay open, but only while their plugin writer is registered:** OAI-PMH, IIIF, JSON, XML, dataset, dataprovider. The check fails closed. A feed whose plugin is disabled gets the logon screen, because it would otherwise fall back to a normal HTML page. So does any unrecognized writer code, and so does a registered plugin whose assembly or class fails to load. `MainWriter_Factory` checks each plugin class once and leaves out any that don't load.
  - **Never reach this check:** `/engine`, `/files`, robots.txt, `/health`, static files.
- **Manual modes** work even when `Enabled` is false, and never expire.
- `ItemHitsPerHourThreshold` is a **placeholder**. Set it from real traffic.

## 5. Robot detection

Code: `Navigation_Object.Is_UserAgent_Robot` (user agent only; the old IP matching is gone), `SearchEngineRobotNavigationInitializer`.

- **Gates *how* a crawler is served, never *whether*.** A robot gets:
  - the fast static item page with full text
  - no zoom
  - no mySobek, search results, print, browse-by or public folders
  - "RESTRICTED ITEM" from `/files`
- **Matching:** `Robot_UserAgent_Tokens` is about 90 substrings for search, AI, SEO and archive crawlers, compiled once into a single `SearchValues<string>` matcher (case-insensitive). `Robot_UserAgent_Prefixes` holds `LSSBOT` and `JAVA/`, which match only at the start of the user agent.
  - The matcher field must stay declared **after** the token array, because static fields initialize in declaration order.
- **Deliberately excluded:**
  - A bare `BOT` catch-all, because Cubot phones would be flagged.
  - Social link-preview fetchers (Facebook, X, LinkedIn, Slack, Discord, WhatsApp, Telegram), which need the normal page for share cards.
- **AmazonBot** is in the list like everything else. The old redirect in `PrettyUrlRewriteMiddleware` is gone.
- **Only catches honest crawlers.** Spoofed browser user agents are left to layers 1–4.

**robots.txt** decides allow/deny, and only for crawlers that obey it:
- **Default:** `Code/SobekCM/robots.txt`, based on budc.barry.edu's. It's **not published**, so merge it into each site by hand. `Disallow: /iipimage/` sits inside the Googlebot and Bingbot groups, because a crawler follows only its best-matching group.
- **ImageServer:** serves `/robots.txt` from code (`Disallow: /`).
- **Zoom pages** already carry `noindex, nofollow`, like every item page.

## 6. Signed GCS URL lifetimes (database settings, 5.2.0 scripts)

Code: `Signed_Url_Lifetime_Enum`, `Signed_Url_Durations.For()`, `GCS_FileSystem`.

| Setting | Default | Used for |
|---|---|---|
| GCS Signed URL Expiration Minutes | 240 | `Continuous`: PDF, audio, video, page turner, and anything that doesn't choose a lifetime |
| GCS Page Load URL Expiration Minutes | 10 | `Page_Load`: `<img>` page images and thumbnails |
| GCS Download URL Expiration Minutes | 60 | `Download`: links clicked later (`ForceDownload` counts as this automatically) |
| GCS Restricted URL Expiration Minutes | 15 | Cap on page-load and download URLs for restricted items |
| GCS Restricted Streaming URL Expiration Minutes | 60 | Cap on continuous URLs for restricted items |

- **What these limit:** reuse of harvested or shared URLs, not how much can be fetched. GCS checks expiry only when a request starts.
- **The PDF viewer's expired-link overlay** calls the same `For()`, so it can't drift from the real expiry.
- **QC tool images** stay `Continuous`, because the page's script reuses them after render.

## 7. ImageServer JP2 pull logging

```json
"ImageServer": { "EnableJp2PullLogging": true, "Jp2PullLogPath": "C:\\SobekCM\\Logs\\jp2-pulls.log" }
```

- **Logged:** one CSV line per **real GCS download**, never cache hits: `timestamp,bucket,tag,objectKey,clientIp,subnet`. `UseForwardedHeaders` keeps the client IP correct behind IIS.
- **Single-flight:** when several requests wait on one download, only the first requester is logged.
- **For tuning the cache:** the same `objectKey` from the same IP just past the cache window means a viewer whose file expired mid-session. The sliding cache resets only on `/render`, never on tile fetches.

---

## Request order

1. `RateLimitingMiddleware`: is this IP banned? If so, 429.
2. `UserIpInitializer`: compute the subnet key.
3. `UserObjectInitializer`: who's logged on.
4. `LoginOnlyModeInitializer`: in site mode, send the request to the logon screen. It isn't counted.
5. `ItemViewRateLimitInitializer`: record burst, sustained and login-only hits.
6. `Item_HtmlSubwriter` / `Print_Item_HtmlSubwriter`: check login-only for items first, then the sustained budget.
7. `JPEG2000_ItemViewer`: robot redirect, then the JP2 budget and circuit breaker, then record the zoom open.

## Testing on demo

- **Trip things quickly:** set any limit to `1` and restart. Log on to confirm the higher ceilings take over.
- **Force the manual modes:** `JP2RateLimiting:ManualDisable: true`, `LoginOnlyMode:ManualMode: "Items"` or `"Site"`.
- **Watch** `temp/ratelimiting.txt` (every ban, budget ceiling and fuse trip) and the ImageServer's `jp2-pulls.log`.
- **Zoom fallback:** request a zoom URL for a page that has a JP2 but no JPG; it should land on the citation. While over budget, the JPEG viewer's page image shouldn't link to zoom.
- **URL lifetimes:** a page image should still load, and a Downloads link should still work a few minutes after the page loads.
- **Not yet translated:** "Temporary Item Rate Limit Reached", "Log On to View Items" and their explanations show in English everywhere.
