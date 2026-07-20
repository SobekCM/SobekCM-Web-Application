# TODO

Running list of known follow-up work on the .NET 10 migration (`UpgradeNet10` branch).

- [ ] Rename `Add_Secondary_Controls` (and related `Add_Controls`/`iAggregationViewer` methods) across the aggregation/MySobek/HTML viewer hierarchy. These names are leftover WebForms control-tree terminology; now they just write an HTML block to a `TextWriter`. Update implementations (e.g. `DataSet_Browse_Info_AggregationViewer`, `Search_Results_HtmlSubwriter`, `Public_Folder_HtmlSubwriter`, etc.) and call sites (`Aggregation_HtmlSubwriter.cs`, `Html_MainWriter.cs`).
- [ ] Item view: the main viewer is being rendered twice on the item display page. Needs investigation into where it's being written more than once.
- [ ] `PagedResults_HtmlSubwriter.cs:1103,1107` — dead `.Replace("%2c", ",")` no-op left over from the `HttpUtility`→`WebUtility` swap; it's chained after `WebUtility.HtmlEncode`, which never emits `%XX` sequences, so it never matches anything. Harmless but pointless; clean up later.
