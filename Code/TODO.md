# TODO

Running list of known follow-up work on the .NET 10 migration (`UpgradeNet10` branch).

- [ ] Rename `Add_Secondary_Controls` (and related `Add_Controls`/`iAggregationViewer` methods) across the aggregation/MySobek/HTML viewer hierarchy. These names are leftover WebForms control-tree terminology; now they just write an HTML block to a `TextWriter`. Update implementations (e.g. `DataSet_Browse_Info_AggregationViewer`, `Search_Results_HtmlSubwriter`, `Public_Folder_HtmlSubwriter`, etc.) and call sites (`Aggregation_HtmlSubwriter.cs`, `Html_MainWriter.cs`).
- [ ] Item view: the main viewer is being rendered twice on the item display page. **Likely root cause found (2026-07-21):** in `Item_HtmlSubwriter`'s now-merged `Add_ItemNavForm_Content` (see reference table below), for any item layout containing a `Viewer_Section` entry, the "opening" half calls `add_viewer_area_start(...)`, which itself already calls `pageViewer.Write_Main_Viewer_Section(Output, Tracer)` and returns — but the "main viewer section" half that follows calls `pageViewer.Write_Main_Viewer_Section(Output, Tracer)` again, unconditionally. That's two calls per request whenever a `Viewer_Section` layout entry is present. Needs a decision on which call is redundant before removing it (mechanical merge only, not fixed yet — see method's remarks).
- [ ] `PagedResults_HtmlSubwriter.cs:1103,1107` — dead `.Replace("%2c", ",")` no-op left over from the `HttpUtility`→`WebUtility` swap; it's chained after `WebUtility.HtmlEncode`, which never emits `%XX` sequences, so it never matches anything. Harmless but pointless; clean up later.
- [ ] Some admin viewers need to draw the header/footer if they have this behavior: `HtmlSubwriter_Behaviors_Enum.MySobek_Subwriter_Mimic_Item_Subwriter`?

## Reference: itemNavForm content per HtmlSubwriter

`abstractHtmlSubwriter.Add_ItemNavForm_Content` (in `abstractHtmlSubwriter.cs`) is the single call invoked from `Html_MainWriter.Write_Body` via virtual dispatch on the base-typed `subwriter` field. Its default implementation still calls `Write_ItemNavForm_Opening` → `Add_Main_Viewer_Section` → `Write_ItemNavForm_Closing` in order, for any subwriter that only overrides one or two of those three. Three subwriters that used to override all three now instead override `Add_ItemNavForm_Content` directly, with the three original bodies merged in sequence (see each file for `// ===== Begin/End original ... =====` markers): `Item_HtmlSubwriter`, `MySobek_HtmlSubwriter`, `Admin_HtmlSubwriter`. **`Item_HtmlSubwriter`'s merge was mechanical only** (early `return;` statements from the original `Write_ItemNavForm_Opening` were left as-is rather than restructured) — see its `Add_ItemNavForm_Content` remarks and the "rendered twice" item above; still needs review. **Because dispatch is now purely virtual, every override in this area must carry the `override` keyword** — `Item_HtmlSubwriter.Add_Main_Viewer_Section` was missing it before the merge (fixed 2026-07-21); that's the kind of bug to watch for when touching this area.

Which of the three original steps each subwriter (under `SobekCM_Library/HTML`) implements, as of this pass:

| Subwriter (Display_Mode_Enum) | Opening | Add_Main_Viewer_Section | Closing | How |
|---|---|---|---|---|
| `Item_HtmlSubwriter` (Item_Display) | ✅ | ✅ | ✅ | merged into one `Add_ItemNavForm_Content` override (mechanical, needs review) |
| `MySobek_HtmlSubwriter` (My_Sobek) | ✅ | ✅ | ✅ | merged into one `Add_ItemNavForm_Content` override |
| `Admin_HtmlSubwriter` (Administrative) | ✅ | ✅ | ✅ | merged into one `Add_ItemNavForm_Content` override |
| `Aggregation_HtmlSubwriter` (Search, Aggregation) | — | ✅ | — | still overrides `Add_Main_Viewer_Section` alone (nothing to collapse) |
| `Search_Results_HtmlSubwriter` (Results) | — | ✅ | — | still overrides `Add_Main_Viewer_Section` alone (nothing to collapse) |
| `Public_Folder_HtmlSubwriter` (Public_Folder) | — | ✅ | — | still overrides `Add_Main_Viewer_Section` alone (nothing to collapse) |
| `PagedResults_HtmlSubwriter` (composed helper, not mode-dispatched directly) | — | ✅ | — | left alone — its `Add_Main_Viewer_Section` is called directly as a standalone step by 4 external composers (`Search_Results_HtmlSubwriter`, `Public_Folder_HtmlSubwriter`, `DataSet_Browse_Info_AggregationViewer`, `Folder_Mgmt_MySobekViewer`), so it can't be folded into `Add_ItemNavForm_Content` without breaking them |
| Everything else (`Internal_`, `Statistics_`, `Preferences_`, `Empty_`, `Error_`, `LegacyUrl_`, `Print_Item_`, `Contact_`, `Web_Content_HtmlSubwriter`) | — | — | — | uses the base no-op default |

Which `Display_Mode_Enum` values actually get a nav form at all — from `Html_MainWriter.Include_Navigation_Form`, so the table above only matters where this is true:
- **Always false** (nav form skipped entirely, subwriter overrides above never fire): `Item_Print`, `Internal`, `Statistics`, `Preferences`, `Search`, `Contact_Sent`, `Error`, `Legacy_URL`.
- **Always true**: `Simple_HTML_CMS`, and the default case (`My_Sobek`, `Administrative`, `Results`, `Public_Folder`, `Item_Display`, `Contact`, anything not explicitly listed).
- **Conditional**: `Aggregation` — delegates to `Aggregation_HtmlSubwriter.Include_Navigation_Form` (true unless `Aggregation_Type` is `Home`/`Home_Edit`, or true always for `Thumbnails_Home_AggregationViewer`).
- **Overridden to false regardless of mode**: any subwriter whose `Subwriter_Behaviors` includes `HtmlSubwriter_Behaviors_Enum.Omit_Main_Navigation_Form` — currently only `Empty_HtmlSubwriter`/`Empty_AggregationViewer`.
