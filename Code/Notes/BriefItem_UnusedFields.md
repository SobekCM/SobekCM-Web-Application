# BriefItem Unused/Dead Field Report

Audit of `SobekCM.Core.BriefItem` (`SobekCM_Core/BriefItem/*.cs`) -- plain data-holder classes, no
polymorphism, used to render item info in the UI. Every public property was checked against the rest
of the codebase (`SobekCM_Core`, `SobekCM_Library`, `SobekCM`, `SobekCM_Engine_Library`,
`SobekCM_Resource_Object`, `SobekCM_Builder_Library`, `SobekCM_Tools`, `Utilities`) for genuine reads
and writes -- serialization attributes (`[DataMember]`/`[XmlElement]`/`[ProtoMember]`) don't count as
usage on their own.

**Status: report only -- nothing has been modified or deleted.** Doing this cleanup means bumping/removing
`[ProtoMember]` slots, which invalidates the item-metadata protobuf cache -- bundle this in with the
protobuf cache rewrite work rather than doing it as a standalone pass.

Coordinate/map-related fields are intentionally left off this list (aside from `BriefItem_Coordinate_Circle`,
which is fully dead) since the map item view is being revived and will start exercising them again.

## Classes to delete entirely

- **`BriefItem_Wordmark.cs`** -- never constructed anywhere. Per-item wordmark data actually lives as a
  `List<string>` of codes on `BriefItem_Behaviors.Wordmarks`, resolved at render time against the global
  `UI_ApplicationCache_Gateway.Icon_List` dictionary (see `Wordmarks_ItemSectionWriter.cs`). This class
  looks like an earlier/alternate design that never got wired up.
- **`BriefItem_ExtensionData.cs`** -- never instantiated anywhere. Also remove `BriefItemInfo.Extensions`
  (its only reference, itself unused).
- **`BriefItem_Namespace.cs`** -- its only creator, `BriefItemInfo.Add_Namespace(...)`, is itself never
  called from anywhere. Also remove `BriefItemInfo.Namespaces` and the `Add_Namespace` method.
- **`BriefItem_Coordinate_Circle.cs`** -- never instantiated (the geospatial mapper only ever handles
  Points/Lines/Polygons). Also remove `BriefItem_GeoSpatial.Circles` and `Circle_Count`, its only
  references.

## Individual properties to remove

| File | Property | Type | Note |
|---|---|---|---|
| `BriefItem_Behaviors.cs` | `Embedded_Web_Content_Title` | `string` | contrast with `Embedded_Web_Content`, which is live |
| `BriefItem_GeoSpatial.cs` | `KML_Reference` | `string` | never mapped, never read |
| `BriefItem_File.cs` | `Attributes` | `string` | never even written |
| `BriefItem_File.cs` | `Width_AsString`, `Height_AsString` | `string` | dead XML-serialization shims |
| `BriefItem_TocElement.cs` | `Level_AsString` | `string` | same shim pattern |
| `BriefItem_DescriptiveTerm.cs` | `References` | `List<string>` | never read or written |
| `BriefItem_Web.cs` | `Made_Public_Date` | `DateTime?` | write-only -- mapped in, never read back |
| `BriefItem_UserTag.cs` | `UserName`, `Description_Tag`, `Date_Added` | -- | write-only -- set on the tag object but never read off it. Worth noting: `BriefItem_Web.User_Tags` as a whole doesn't appear to be read by the UI at all -- `User_Tags_MySobekViewer.cs` reads raw `DataRow`s instead -- so this may be a bigger dead feature, not just three fields |
| `BriefItem_UserGroupRestrictions.cs` | `GroupName` | `string` | write-only -- `GroupID`/`CanView` on the same class are genuinely used |

## Parked -- leave alone (map view revival will use these)

Everything on `BriefItem_Coordinate_Point`, `BriefItem_Coordinate_Line`, and `BriefItem_Coordinate_Polygon`
(`Altitude`, `FeatureType`, `Points`, `Point_Count`, `Rotation`, `PolygonType`, `Inner_Points`,
`Inner_Points_Count`, etc.) -- currently write-only or unused, but that's expected for a display feature
that's dormant, not dead.

## Not touching

`BriefItem_CitationResponse.cs` properties (`BibID`, `VID`, `Title`, `Namespaces`, `Description`) -- no
direct C# reads, but the object is serialized wholesale for the citation API endpoint
(`SobekCM_Engine_Library/Endpoints/ItemServices.cs`), so that's real usage.

## Also worth revisiting alongside this cleanup -- `BriefItem_Web.Siblings` isn't a real count

Found 2026-08-21 while fixing `ReloadMetsAndBasicDbInfoModule` to call the fuller
`SobekCM_METS_Based_ItemBuilder.Finish_Building_Item` database load (bringing it in line with what the
web/engine item-loading path does). `Web.Siblings` (`int?`) is set two different ways in the codebase,
and neither is an accurate sibling count:

- `SobekCM_METS_Based_ItemBuilder.Finish_Building_Item` (the web/engine path, and now also the builder
  path after the above fix): `if (Multiple) Package_To_Finalize.Web.Siblings = 2;` -- a hardcoded `2`,
  not a real count, and only set at all when the title has more than one volume; left `null` otherwise.
- `SobekCM_Item_Database.cs` (`Siblings = Convert.ToInt32(...) - 1`, near line 73) -- an actual computed
  count. This is what the builder's `Engine_Database.Add_Minimum_Builder_Information` used to populate
  before today's fix; that method is no longer called by `ReloadMetsAndBasicDbInfoModule`, but the DB
  method itself is still there and may still be used elsewhere.

Every current consumer only checks the `null`/`1` boundary, not the actual number, so the hardcoded `2`
happens to work today:
- `MultiVolumes_ItemViewer.cs` (~line 53) -- `CurrentItem.Web.Siblings > 1` decides whether the "All
  Volumes" tab shows at all.
- `Usage_Stats_ItemViewer.cs` (~line 244) -- `(!Siblings.HasValue) || (Siblings.Value <= 1)` treated as
  "single item" for usage stats purposes.
- Two spots already invalidate a specific item's `cache.protobuf` when this boundary is crossed (a title
  drops to, or grows past, one volume), via `BriefItem_Cache.DeleteCache`: `Delete_Item_MySobekViewer.cs`
  (~line 243) and `Group_Add_Volume_MySobekViewer.cs` (~line 225) -- both already have comments explaining
  why, worth reading before touching this.

**If this is ever changed to hold a real count** (e.g. to actually display "3 other volumes" somewhere,
rather than just gating a boolean), every existing `cache.protobuf` bakes in the old null-or-2 value and
needs invalidating/regenerating -- same consideration as the unused-field removals above, so worth doing
at the same time as that cleanup pass rather than as a separate surprise later. Not investigated: whether
a real count is even wanted, or whether the field should instead be renamed/retyped to reflect what it
actually is (e.g. `bool Has_Siblings`) since every consumer only ever treats it as one.
