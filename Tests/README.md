# SobekCM Playwright tests

Two suites run together from `npm test`:

| Suite | Where | Runs against | Used by |
|---|---|---|---|
| Smoke | `tests/smoke/*.spec.ts` | any SobekCM site | `Upgrade-CustomerSite.ps1` after a deploy (`npm run test:smoke`) |
| BDD acceptance | `features/**/*.feature` | a copy of **testing.sobeklibrary.com** | sobek-infra-tests workflows, against a release candidate |

The BDD features assert on the testing site's own data (collection codes, result counts, facet
values), so they are expected to fail against any other site.

## Running

```
npm ci
npx playwright install chromium
BASE_URL=https://testing.sobeklibrary.com npm test        # both suites
BASE_URL=https://testing.sobeklibrary.com npm run test:bdd  # BDD only
```

`npm test` runs `bddgen` first, which compiles the Gherkin features into Playwright specs under
`.features-gen/` (git-ignored). A bare `npx playwright test` skips that step and runs stale specs.

Optional environment variables:

- `SOBEKCM_EXPECTED_VERSION`: the version the footer should show (e.g. `5.2.0`). The footer version scenario skips without it.
- `CI`: caps the run at 4 workers, so a full run can't trip the app's per-IP rate limiter if that is enabled.
- `ROBOTS_ALLOWED_BASE_URL` / `BLOCKALL_BASE_URL`: a second address for the same site running with the other
  `Robots:BlockAll` setting. That setting is read once when the site starts, so a scenario can't flip it; the robot
  scenarios instead start with `Given a site that allows robots` or `Given a site that blocks all robots`. With the
  matching variable set they use that address (and fail if it doesn't behave as named). Without it they use
  `BASE_URL` when that site happens to match, and skip otherwise. The e2e environment serves one deployment twice:
  port 80 blocks all robots, like the demo and testing sites, and port 8080 allows them (`ROBOTS_ALLOWED_BASE_URL`).

## Layout

- `features/` holds the Gherkin feature files, grouped by area: `aggregation`, `search`, `chrome`, `api`, `maps`.
- `steps/` holds the step definitions:
  - `fixtures.ts` has the per-scenario `world` state and collects page script errors.
  - `common.steps.ts` has navigation and generic page checks.
  - `search.steps.ts`, `chrome.steps.ts` and `http.steps.ts` cover search, header/menus and raw HTTP.
- `support/regions.ts` maps plain-language region names to CSS selectors, so features read like `the "facet column" should contain "..."`. Add a region there instead of putting selectors in a feature.

## Tags

- `@aggregation`, `@search`, `@chrome`, `@i18n`, `@api` and `@security` mark the area a scenario covers.
- `@maps`: map pages. Most use a stand-in for Google Maps (`support/google-maps-stub.ts`); `@real-google` ones need the site's real key.
- `@known-bug @fail`: the scenario describes the correct behaviour for a bug that is found but deliberately left open, and runs as `test.fail()`. None are open today, so any failing scenario is a real bug.
  - The run stays green while the bug exists.
  - Once the bug is fixed, the scenario reports "expected to fail, but passed". Remove both tags then.
  - A comment above each one names the cause.
- Stage tags (`@logon-readonly`, `@content`, `@structure`, `@site-settings`): see Stages below.

Filter by tag with `--grep`, e.g. `npm run test:bdd -- --grep @search`.

## Stages

The BDD scenarios are split into Playwright projects by stage tag, ordered by how far their changes reach:

| Project | Tag | What it holds |
|---|---|---|
| `public` | *(no stage tag)* | Anonymous and read-only: everything that doesn't carry a stage tag |
| `logon-readonly` | `@logon-readonly` | Each role logs on and only looks |
| `content` | `@content` | Submitting items, editing metadata, attaching files |
| `structure` | `@structure` | Creating or deleting collections, changing collection settings |
| `site-settings` | `@site-settings` | System-wide settings, e.g. clearing the Google Maps key |

- **The e2e pipeline runs them one after another**, one `playwright test --project=<stage>` step each, so a stage starts only when the one before has finished changing the site. Each step may fail without stopping the next, so every stage reports. Each run writes a blob report (`PLAYWRIGHT_BLOB_NAME=<stage>`), and `npm run merge-reports` combines them into one HTML report and one `test-results/junit.xml`.
- **The state-altering stages (`@content`, `@structure`, `@site-settings`) only run in the disposable e2e environment.** A hook in `steps/environment.steps.ts` skips them unless `E2E_DISPOSABLE=true`, which only the e2e pipeline sets, so running the suite against a real site never changes it. The tag alone is enough; no step has to remember.
- **Within a stage, scenarios run in parallel.** A scenario that changes something should put it back where it can (clear the key, check the message, restore the key), so the order within a stage matters less.
