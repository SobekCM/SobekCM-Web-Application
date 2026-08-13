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
