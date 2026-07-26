# SobekCM — Claude Context

## Project overview

SobekCM is an open-source digital library management system. This repository is a migration from **.NET 4.7.2 ASP.NET WebForms** to **.NET 10 ASP.NET Core**. Work is ongoing on the `UpgradeNet10` branch.

The main web application entry point is the `SobekCM` project. Core domain models live in `SobekCM_Core`. Rendering/HTML subwriters live in `SobekCM_Library`.

---

## Session storage — deliberate design split

All session keys are constants in `SobekCM_Core/MemoryMgmt/SessionCache_Keys.cs`. Never use raw string literals for session keys. Likewise, `Context.Items[...]` request-scoped keys are constants in `SobekCM_Core/MemoryMgmt/RequestCache_Keys.cs` — never use raw string literals there either.

| What | Where | How |
|---|---|---|
| Primitive/string values | `ISession` | `context.Session.SetString(key, value)` / `GetString(key)` |
| Complex objects (workflow state, etc.) | `SessionObjectStore` | `context.SessionObject()[key] = obj` |
| `User_Object` | `ISession` via Protobuf | `CachedDataManager_UserCacheServices` (see below) |

**Why the split:** `ISession` (string-based) participates in ASP.NET Core's distributed session store and expires naturally. `SessionObjectStore` holds complex objects in `SharedCache` (see below) keyed by `"SESSION|" + sessionId + "|" + key`, with a sliding expiration equal to `Session:IdleTimeoutMinutes` — the same idle timeout `ISession` itself uses, set once in `Program.cs` into `SessionObjectStore.IdleTimeout`. An entry not re-accessed within that window expires on its own; accessing it resets the clock. `ClearSession(sessionId)` (prefix-scans `SharedCache` and removes matches) additionally fires on logout (`MySobek_HtmlSubwriter.cs`) for immediate cleanup, though the sliding expiration is what actually bounds memory — nothing needs to call `ClearSession` for the leak to be fixed.

**`User_Object` specifically uses `ISession`** (not `SessionObjectStore`) because it is stored for every logged-in user and predates the `SharedCache`-backed fix — no strong reason to move it now, but nothing blocks it either.

---

## User_Object session helper

`SobekCM_Core/MemoryMgmt/CachedDataManager_UserCacheServices.cs` — static class, two methods:

```csharp
string CachedDataManager_UserCacheServices.UserToString(User_Object user)
User_Object CachedDataManager_UserCacheServices.StringToUser(string value)
```

Serializes/deserializes via Protobuf (Base64). `User_Object` is `[ProtoContract]`-attributed.

Files that read/write the user session key (`SessionCache_Keys.User`):
- `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` — read, write×2, clear
- `SobekCM_Library/MySobekViewer/Logon_MySobekViewer.cs` — write
- `SobekCM_Library/MySobekViewer/Preferences_MySobekViewer.cs` — null check×2, write×2
- `SobekCM_Library/MySobekViewer/OpenNJ_Register_MySobekViewer.cs` — null check×3, write×2
- `SobekCM_Library/MySobekViewer/NewPassword_MySobekViewer.cs` — null check
- `SobekCM_Library/HTML/MySobek_HtmlSubwriter.cs` — clear on logout

---

## In-process caching — SharedCache

`SobekCM_Core/MemoryMgmt/SharedCache.cs` is the single in-process cache for the whole app, replacing what used to be three separate stores: `System.Runtime.Caching.MemoryCache.Default` (used throughout `CachedDataManager` and a handful of other files), the `ConcurrentDictionary`-based `SobekCM_Application` (application-wide state, replacing `HttpApplicationState`), and `SessionObjectStore`'s old unbounded per-session dictionary.

It's a thin wrapper around `Microsoft.Extensions.Caching.Memory.MemoryCache`, deliberately shaped like the old `System.Runtime.Caching.MemoryCache.Default` (indexer, `Get`/`Set`/`Remove`, and `IEnumerable<KeyValuePair<string,object>>`) so call sites — including LINQ queries written against it — needed only a name swap, not a rewrite. Three call patterns share it, distinguished by key prefix and expiration policy:

| Caller | Key shape | Expiration |
|---|---|---|
| `CachedDataManager` (+ sub-services) | content-keyed, e.g. `"AGGR\|" + code`, `"SKIN\|" + code` | `SlidingExpiration`, typically 1–5 min |
| `SobekCM_Application.State[key]` | raw key, e.g. `"NORESULTS"` | none — `CacheItemPriority.NeverRemove` |
| `SessionObjectStore` (via `context.SessionObject()[key]`) | `"SESSION\|" + sessionId + "\|" + key` | `SlidingExpiration` = `Session:IdleTimeoutMinutes` |

Because they share one physical cache, `SobekCM_Application.State.RemoveAll()` and `CachedDataManager.Clear_Cache()` both clear everything, not just their own slice — that's intentional (see `QueryInitializer.Reset_Memory()`, which already called both together even before the merge).

---

## Pretty URL rewriting

`SobekCM\Program.cs` has a `PrettyUrl_Rewrite` middleware (registered right after `UseStaticFiles`, before the `Map`/`MapFallback` routes) that turns paths like `/AA00008275/00001/3j` into the `urlrelative` query param `QueryString_Analyzer` expects. It replaces the old `SobekCM_URL_Rewriter` project (a `System.Web` `IHttpModule`) — that project still exists in the solution but is **dead code**: `Program.cs` registers no `System.Web` compatibility layer at all (the `Microsoft.AspNetCore.SystemWebAdapters` package was removed once its only real call sites — a couple of `HeaderDictionary.UserAgent` reads — were rewritten to the plain `Headers["User-Agent"]` indexer), so there's no `IHttpModule` pipeline under Kestrel to run it. Don't assume it does anything.

The new middleware deliberately does **not** replicate the old rewriter's static-file extension checks or its `&portal=` query param injection — `UseStaticFiles` (registered earlier) already filters out real files before this middleware runs, and `QueryInitializerHelpers/UrlInitializer.cs` already derives `Base_URL` straight from the request host. It also skips the old `rss` branch and the "serve robots a pre-rendered static page" shortcut — dropped as out of scope, not ported. It keeps: robots.txt/AmazonBot/OHPi/favicon-per-host passthroughs, the `dataset/`/`xml/`/`json/`/`dataprovider/` → `sobekcm_data.aspx` branch, and the USFLDC PURL redirection service (USF-specific; may be dropped later).

---

## HtmlSubwriter itemNavForm construction

`Html_MainWriter.Write_Body` calls exactly one method on the subwriter: `subwriter.Write_HTML(Output, Tracer)`. There is no more `Add_ItemNavForm_Content`/`Write_Final_HTML` call at that level, and no more `Include_Navigation_Form` gating in `Html_MainWriter` — each subwriter decides for itself, inline in its own `Write_HTML`, whether and when to open/close the `<form id="itemNavForm">` wrapper, using the protected `Write_ItemNavForm_Opening(Output)` / `Write_ItemNavForm_Closing(Output)` helpers on `abstractHtmlSubwriter`.

8 classes had their formerly-separate `Write_HTML` / `Add_ItemNavForm_Content` / `Write_Final_HTML` methods merged into a single `Write_HTML`, with `// ===== Begin/End original ... =====` markers left in place wherever content came from more than one original method (so the merge history stays visible in-code): `Internal_HtmlSubwriter`, `Web_Content_HtmlSubwriter`, `MySobek_HtmlSubwriter`, `Admin_HtmlSubwriter`, `Search_Results_HtmlSubwriter`, `Public_Folder_HtmlSubwriter`, `Aggregation_HtmlSubwriter`, `Item_HtmlSubwriter`.

**`PagedResults_HtmlHelper` is the one exception, deliberately left un-merged.** It's a composed helper (never mode-dispatched, always addressed by concrete type — see `HtmlSubwriterFactory`, which doesn't create it), used by 4 different consumers: `Search_Results_HtmlSubwriter` and `Public_Folder_HtmlSubwriter` (both merged, so they now call `writeResult.Write_HTML(...)` → `writeResult.Add_ItemNavForm_Content(...)` → `writeResult.Write_Final_HTML(...)` back-to-back inside their own single `Write_HTML`), plus `DataSet_Browse_Info_AggregationViewer` and `Folder_Mgmt_MySobekViewer` (different interface hierarchies, calling `Write_HTML` and `Add_ItemNavForm_Content` at genuinely separate rendering phases, not back-to-back). Because two of the four callers still need the methods independently callable, `PagedResults_HtmlHelper` keeps `Write_HTML` / `Add_ItemNavForm_Content` / `Write_Final_HTML` as three separate overrides — which is also why `abstractHtmlSubwriter` still declares `Add_ItemNavForm_Content`/`Write_Final_HTML` as virtual (base no-op) methods at all; nothing else overrides them anymore.

**Every override in this area must carry the `override` keyword** — dispatch is purely virtual; a missing `override` silently falls back to the base no-op instead of erroring. This already bit `Item_HtmlSubwriter.Add_Main_Viewer_Section` once, before the original three-method split was removed.

Which `Display_Mode_Enum` modes actually get a `<form>` wrapper at all now comes down entirely to each subwriter's own `Write_HTML` logic (no more central gate) — check each class directly rather than relying on a table here, since that decision moved out of `Html_MainWriter`.

---

## Migration conventions

- `Session.Add("key", value)` / `Session["key"]` (WebForms) → `Session.SetString` / `SessionObject()` per the table above
- `Response.Redirect` → `context.Response.Redirect` or `UrlWriterHelper.Redirect`
- `HttpContext.Current` → injected `HttpContext` passed through the request pipeline
- `Global.asax Session_End` does not exist in ASP.NET Core — no session expiry event fires
