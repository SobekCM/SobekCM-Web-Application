# TODO

Running list of known follow-up work on the .NET 10 migration (`UpgradeNet10` branch).

- [ ] Rename `Add_Secondary_Controls` (and related `Add_Controls`/`iAggregationViewer` methods) across the aggregation/MySobek/HTML viewer hierarchy. These names are leftover WebForms control-tree terminology; now they just write an HTML block to a `TextWriter`. Update implementations (e.g. `DataSet_Browse_Info_AggregationViewer`, `Search_Results_HtmlSubwriter`, `Public_Folder_HtmlSubwriter`, etc.) and call sites (`Aggregation_HtmlSubwriter.cs`, `Html_MainWriter.cs`).
- [ ] Item view: the main viewer is being rendered twice on the item display page. **Likely root cause found (2026-07-21):** in `Item_HtmlSubwriter`'s now-merged `Add_ItemNavForm_Content` (see reference table below), for any item layout containing a `Viewer_Section` entry, the "opening" half calls `add_viewer_area_start(...)`, which itself already calls `pageViewer.Write_Main_Viewer_Section(Output, Tracer)` and returns — but the "main viewer section" half that follows calls `pageViewer.Write_Main_Viewer_Section(Output, Tracer)` again, unconditionally. That's two calls per request whenever a `Viewer_Section` layout entry is present. Needs a decision on which call is redundant before removing it (mechanical merge only, not fixed yet — see method's remarks).
- [ ] `PagedResults_HtmlHelper.cs:1106,1110` (renamed from `PagedResults_HtmlSubwriter.cs`) — dead `.Replace("%2c", ",")` no-op left over from the `HttpUtility`→`WebUtility` swap; it's chained after `WebUtility.HtmlEncode`, which never emits `%XX` sequences, so it never matches anything. Harmless but pointless; clean up later.
- [ ] Some admin viewers need to draw the header/footer if they have this behavior: `HtmlSubwriter_Behaviors_Enum.MySobek_Subwriter_Mimic_Item_Subwriter`?

## Reference: which HtmlSubwriters actually use the itemNavForm

`abstractHtmlSubwriter` no longer has the old three-method split (`Write_ItemNavForm_Opening` / `Add_Main_Viewer_Section` / `Write_ItemNavForm_Closing`) — those were fully removed. `Add_ItemNavForm_Content(Output, Tracer)` is now the *only* nav-form-content hook, called from `Html_MainWriter.Write_Body` via virtual dispatch on the base-typed `subwriter` field, default no-op. **Every override in this area must carry the `override` keyword** — dispatch is purely virtual now, so a missing `override` silently falls back to the no-op (this bit `Item_HtmlSubwriter.Add_Main_Viewer_Section` once already, before the three-method split was removed).

Whether a subwriter's `Add_ItemNavForm_Content` ever actually runs depends on **two independent gates**: (1) `Html_MainWriter.Include_Navigation_Form` must be true for the current `Display_Mode_Enum` — if false, the `<form>` wrapper is skipped entirely and the subwriter is never asked; (2) the subwriter must actually override `Add_ItemNavForm_Content` with something — several inherit the base no-op and get an empty form wrapper instead. Full picture, per `Display_Mode_Enum`:

| Mode | Subwriter | Form wrapper written? | Subwriter contributes content? |
|---|---|---|---|
| `Item_Display` | `Item_HtmlSubwriter` | yes | yes (mechanical merge, needs review — see "rendered twice" item above) |
| `My_Sobek` | `MySobek_HtmlSubwriter` | yes | yes |
| `Administrative` | `Admin_HtmlSubwriter` | yes | yes |
| `Aggregation` | `Aggregation_HtmlSubwriter` | conditional — false for `Aggregation_Type` `Home`/`Home_Edit`, otherwise true (always true for `Thumbnails_Home_AggregationViewer`) | yes, when the wrapper is written |
| `Results` | `Search_Results_HtmlSubwriter` | yes | yes, via composed `PagedResults_HtmlHelper.Add_ItemNavForm_Content` |
| `Public_Folder` | `Public_Folder_HtmlSubwriter` | yes | yes, via composed `PagedResults_HtmlHelper.Add_ItemNavForm_Content` |
| `Simple_HTML_CMS` | `Web_Content_HtmlSubwriter` | yes (unconditionally true) | **no** — empty `<form></form>`, never overrode the method |
| `Contact` | `Contact_HtmlSubwriter` | yes (default-case true) | **no** — empty `<form></form>`, never overrode the method |
| `Search` | `Aggregation_HtmlSubwriter` | **no** — hard-false regardless of subwriter, even though the same class handles `Aggregation` mode | n/a |
| `Item_Print`, `Internal`, `Statistics`, `Preferences`, `Contact_Sent`, `Error`, `Legacy_URL` | (respective subwriter) | no — explicit `false` case | n/a |
| `Empty` | `Empty_HtmlSubwriter` | no — forced false via `Subwriter_Behaviors.Omit_Main_Navigation_Form` (also set by `Empty_AggregationViewer`) | n/a |

`PagedResults_HtmlHelper` (renamed from `PagedResults_HtmlSubwriter`, still extends `abstractHtmlSubwriter` for the shared `Context`/`Get_Collection` plumbing) is never itself mode-dispatched — it's a composed helper, always addressed by concrete type, instantiated by `Search_Results_HtmlSubwriter`, `Public_Folder_HtmlSubwriter`, `DataSet_Browse_Info_AggregationViewer`, and `Folder_Mgmt_MySobekViewer`.
