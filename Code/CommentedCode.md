# Commented-Out Code Blocks Report

Scan of all in-project `.cs` files (build-excluded / `obj` / `bin` / `.vs` / `.claude` / `ItemViewer/Viewers/Excluded` folders skipped) for contiguous runs of 6+ `//`-commented lines that look like actual code (not prose or `///` doc comments).

- Files scanned: 1125
- Blocks found: 293
- Files with at least one block: 85

**Status: report only -- nothing has been modified or deleted.** Use this as a punch list to revisit later; each entry is a candidate for deletion (or restoration) after a closer look.

## Standout candidates

- `SobekCM_Library/AggregationViewer/Viewers/Map_Browse_AggregationViewer_Beta.cs` and `Map_Search_AggregationViewer_Beta.cs` -- essentially the entire viewer body (lines 78-326 and 84-332) is commented out; only the constructor header is live.
- `SobekCM_Library/Static_Pages_Builder.cs` -- heaviest concentration by far, 26 separate blocks scattered through the file.

## Very large blocks (40+ lines) -- 4

| File | Lines | Size |
|---|---|---|
| `SobekCM_Library/AggregationViewer/Viewers/Map_Browse_AggregationViewer_Beta.cs` | 78-326 | 249 lines |
| `SobekCM_Library/AggregationViewer/Viewers/Map_Search_AggregationViewer_Beta.cs` | 84-332 | 249 lines |
| `SobekCM_Engine_Library/Items/BriefItems/Mappers/Subjects_BriefItemMapper.cs` | 29-88 | 60 lines |
| `SobekCM_Library/HTML/Web_Content_HtmlSubwriter.cs` | 490-530 | 41 lines |

## Large blocks (20-39 lines) -- 18

| File | Lines | Size |
|---|---|---|
| `SobekCM/QueryInitializer.cs` | 624-652 | 29 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 800-827 | 28 lines |
| `SobekCM_Resource_Object/Metadata_File_ReaderWriters/INFO_File_ReaderWriter.cs` | 151-177 | 27 lines |
| `SobekCM_Resource_Object/Utilities/Image_Derivative_Creation_Processor.cs` | 705-731 | 27 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 739-764 | 26 lines |
| `SobekCM_Resource_Object/Metadata_File_ReaderWriters/MXF_File_ReaderWriter.cs` | 207-232 | 26 lines |
| `SobekCM_Resource_Object/Utilities/Image_Derivative_Creation_Processor.cs` | 749-774 | 26 lines |
| `SobekCM_Library/AdminViewer/Builder_AdminViewer.cs` | 528-551 | 24 lines |
| `SobekCM_Library/ItemViewer/HtmlSectionWriters/StandardMenu_ItemSectionWriter.cs` | 26-48 | 23 lines |
| `SobekCM_Tools/SecurityInfo.cs` | 26-48 | 23 lines |
| `SobekCM_Library/HTML/Item_HtmlSubwriter.cs` | 353-374 | 22 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 142-163 | 22 lines |
| `SobekCM_Core/Configuration/Engine/Engine_Server_Configuration.cs` | 316-336 | 21 lines |
| `SobekCM_Engine_Library/Database/Engine_Database.cs` | 4672-4692 | 21 lines |
| `SobekCM_Library/AdminViewer/WebContent_Single_AdminViewer.cs` | 697-717 | 21 lines |
| `SobekCM_Library/AdminViewer/Skins_AdminViewer.cs` | 287-306 | 20 lines |
| `SobekCM_Library/Database/SobekCM_Database.cs` | 89-108 | 20 lines |
| `SobekCM_Resource_Object/SobekCM_Item.cs` | 2010-2029 | 20 lines |

## Medium blocks (10-19 lines) -- 105

| File | Lines | Size |
|---|---|---|
| `SobekCM_Engine_Library/Database/Engine_Database.cs` | 1296-1314 | 19 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 766-784 | 19 lines |
| `SobekCM_Core/Configuration/Engine/Engine_Server_Configuration.cs` | 285-302 | 18 lines |
| `SobekCM_Engine_Library/Items/SobekCM_Item_Updater.cs` | 131-148 | 18 lines |
| `SobekCM_Builder_Library/Modules/Items/CreateStaticVersionModule.cs` | 104-120 | 17 lines |
| `SobekCM_Builder_Library/Worker_Controller.cs` | 703-719 | 17 lines |
| `SobekCM_Library/AdminViewer/WebContent_Single_AdminViewer.cs` | 675-691 | 17 lines |
| `SobekCM_Library/Citation/Elements/implemented elements/Projects_Element.cs` | 42-58 | 17 lines |
| `SobekCM_Library/MySobekViewer/Edit_TEI_Item_MySobekViewer.cs` | 592-608 | 17 lines |
| `SobekCM_Library/MySobekViewer/New_Group_And_Item_MySobekViewer.cs` | 1036-1052 | 17 lines |
| `SobekCM_Library/MySobekViewer/New_TEI_MySobekViewer.cs` | 935-951 | 17 lines |
| `SobekCM_Library/RequestCache.cs` | 36-52 | 17 lines |
| `SobekCM_Library/ResultsViewer/abstract_ResultsViewer.cs` | 109-125 | 17 lines |
| `SobekCM_Builder_Library/Modules/Schedulable/SendNewItemEmailsModule.cs` | 45-60 | 16 lines |
| `SobekCM_Builder_Library/Modules/Schedulable/SolrLuceneIndexOptimizationModule.cs` | 18-33 | 16 lines |
| `SobekCM_Builder_Library/Worker_Controller.cs` | 540-555 | 16 lines |
| `SobekCM_Core/Configuration/Engine/Engine_Server_Configuration.cs` | 178-193 | 16 lines |
| `SobekCM_Library/Citation/Elements/nonstandard elements/Serial_Hierarchy_Panel_Element.cs` | 114-129 | 16 lines |
| `SobekCM_Library/MySobekViewer/File_Management_MySobekViewer.cs` | 365-380 | 16 lines |
| `SobekCM_Resource_Object/SobekCM_Item.cs` | 1428-1443 | 16 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 325-339 | 15 lines |
| `SobekCM_Library/Database/SobekCM_Database.cs` | 830-844 | 15 lines |
| `SobekCM_Library/Database/SobekCM_Database.cs` | 874-888 | 15 lines |
| `SobekCM_Library/MainWriters/DataProvider_MainWriter.cs` | 162-176 | 15 lines |
| `SobekCM_Library/RequestCache.cs` | 66-80 | 15 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 553-567 | 15 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 602-616 | 15 lines |
| `SobekCM_Resource_Object/Metadata_File_ReaderWriters/INFO_File_ReaderWriter.cs` | 120-134 | 15 lines |
| `SobekCM_Resource_Object/Metadata_File_ReaderWriters/MXF_File_ReaderWriter.cs` | 302-316 | 15 lines |
| `SobekCM_Core/Configuration/Engine/Engine_Server_Configuration.cs` | 248-261 | 14 lines |
| `SobekCM_Library/Citation/Elements/implemented elements/Language_Script_Element.cs` | 55-68 | 14 lines |
| `SobekCM_Library/HTML/Item_HtmlSubwriter.cs` | 331-344 | 14 lines |
| `SobekCM_Library/ItemViewer/Viewers/Milestones_ItemViewer.cs` | 228-241 | 14 lines |
| `SobekCM_Library/MySobekViewer/Delete_Item_MySobekViewer.cs` | 160-173 | 14 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 569-582 | 14 lines |
| `SobekCM_Resource_Object/Metadata_File_ReaderWriters/MXF_File_ReaderWriter.cs` | 183-196 | 14 lines |
| `SobekCM_Resource_Object/Metadata_File_ReaderWriters/MXF_File_ReaderWriter.cs` | 251-264 | 14 lines |
| `SobekCM_Resource_Object/Utilities/Image_Derivative_Creation_Processor.cs` | 790-803 | 14 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 404-416 | 13 lines |
| `SobekCM_Library/HTML/HtmlHelpers/MainMenus_HtmlHelper.cs` | 918-930 | 13 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 233-245 | 13 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 643-655 | 13 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 666-678 | 13 lines |
| `SobekCM_Resource_Object/Utilities/SobekCM_METS_Validator.cs` | 178-190 | 13 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 359-370 | 12 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 383-394 | 12 lines |
| `SobekCM_Builder_Library/Modules/Schedulable/SendNewItemEmailsModule.cs` | 32-43 | 12 lines |
| `SobekCM_Builder_Library/Worker_Controller.cs` | 682-693 | 12 lines |
| `SobekCM_Core/Configuration/Engine/Engine_Server_Configuration.cs` | 267-278 | 12 lines |
| `SobekCM_Library/AdminViewer/Aggregation_Single_AdminViewer.cs` | 2032-2043 | 12 lines |
| `SobekCM_Library/AdminViewer/WebContent_History_AdminViewer.cs` | 43-54 | 12 lines |
| `SobekCM_Library/AdminViewer/WebContent_Mgmt_AdminViewer.cs` | 43-54 | 12 lines |
| `SobekCM_Library/AdminViewer/WebContent_Usage_AdminViewer.cs` | 49-60 | 12 lines |
| `SobekCM_Library/HTML/Aggregation_HtmlSubwriter.cs` | 124-135 | 12 lines |
| `SobekCM_Library/ItemViewer/Viewers/Citation_Standard_ItemViewer.cs` | 743-754 | 12 lines |
| `SobekCM_Library/MainWriters/DataProvider_MainWriter.cs` | 108-119 | 12 lines |
| `SobekCM_Library/MySobekViewer/New_TEI_MySobekViewer.cs` | 2695-2706 | 12 lines |
| `SobekCM_Library/MySobekViewer/Track_Item_MySobekViewer.cs` | 1536-1547 | 12 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 317-328 | 12 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 341-352 | 12 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 867-878 | 12 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 901-912 | 12 lines |
| `SobekCM/QueryInitializer.cs` | 654-664 | 11 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 484-494 | 11 lines |
| `SobekCM_Builder_Library/Worker_Controller.cs` | 609-619 | 11 lines |
| `SobekCM_Core/Configuration/Engine/Engine_Server_Configuration.cs` | 161-171 | 11 lines |
| `SobekCM_Core/Configuration/Engine/Engine_Server_Configuration.cs` | 304-314 | 11 lines |
| `SobekCM_Engine_Library/Configuration/Configuration_Files_Reader.cs` | 2807-2817 | 11 lines |
| `SobekCM_Library/AdminViewer/Settings_AdminViewer.cs` | 2717-2727 | 11 lines |
| `SobekCM_Library/AdminViewer/WebContent_Single_AdminViewer.cs` | 796-806 | 11 lines |
| `SobekCM_Library/Citation/Elements/nonstandard elements/Viewer_Element.cs` | 282-292 | 11 lines |
| `SobekCM_Library/HTML/Item_HtmlSubwriter.cs` | 319-329 | 11 lines |
| `SobekCM_Library/MainWriters/DataProvider_MainWriter.cs` | 196-206 | 11 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 84-94 | 11 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 278-288 | 11 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 418-428 | 11 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 541-551 | 11 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 587-597 | 11 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 887-897 | 11 lines |
| `SobekCM_Resource_Object/SobekCM_Item.cs` | 1327-1337 | 11 lines |
| `SobekCM/QueryInitializer.cs` | 546-555 | 10 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 314-323 | 10 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 372-381 | 10 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 427-436 | 10 lines |
| `SobekCM_Builder_Library/Modules/Schedulable/SendNewItemEmailsModule.cs` | 94-103 | 10 lines |
| `SobekCM_Builder_Library/Worker_Controller.cs` | 650-659 | 10 lines |
| `SobekCM_Core/Configuration/Engine/Engine_Server_Configuration.cs` | 230-239 | 10 lines |
| `SobekCM_Core/WebContent/HTML_Based_Content.cs` | 419-428 | 10 lines |
| `SobekCM_Library/AdminViewer/User_Group_AdminViewer.cs` | 475-484 | 10 lines |
| `SobekCM_Library/Citation/Elements/implemented elements/Projects_Element.cs` | 114-123 | 10 lines |
| `SobekCM_Library/Citation/Elements/nonstandard elements/Other_Title_Form_Element.cs` | 105-114 | 10 lines |
| `SobekCM_Library/Citation/Elements/nonstandard elements/Other_Title_Form_Element.cs` | 328-337 | 10 lines |
| `SobekCM_Library/Citation/Elements/nonstandard elements/Viewer_Element.cs` | 149-158 | 10 lines |
| `SobekCM_Library/HTML/HtmlHelpers/MainMenus_HtmlHelper.cs` | 1160-1169 | 10 lines |
| `SobekCM_Library/ItemViewer/Viewers/QC_ItemViewer.cs` | 2575-2584 | 10 lines |
| `SobekCM_Library/MainWriters/DataProvider_MainWriter.cs` | 70-79 | 10 lines |
| `SobekCM_Library/MainWriters/DataProvider_MainWriter.cs` | 213-222 | 10 lines |
| `SobekCM_Library/ResultsViewer/Google_Map_ResultsViewer_Beta.cs` | 47-56 | 10 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 445-454 | 10 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 683-692 | 10 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 719-728 | 10 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 833-842 | 10 lines |
| `SobekCM_Resource_Object/Metadata_File_ReaderWriters/MXF_File_ReaderWriter.cs` | 240-249 | 10 lines |
| `SobekCM_Resource_Object/Metadata_File_ReaderWriters/MXF_File_ReaderWriter.cs` | 291-300 | 10 lines |
| `SobekCM_Resource_Object/SobekCM_Item.cs` | 1400-1409 | 10 lines |

## Small blocks (6-9 lines) -- 166

| File | Lines | Size |
|---|---|---|
| `SobekCM/Program.cs` | 433-441 | 9 lines |
| `SobekCM/QueryInitializer.cs` | 570-578 | 9 lines |
| `SobekCM/QueryInitializer.cs` | 594-602 | 9 lines |
| `SobekCM/QueryInitializer.cs` | 666-674 | 9 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 343-351 | 9 lines |
| `SobekCM_Core/Configuration/Engine/Engine_Server_Configuration.cs` | 203-211 | 9 lines |
| `SobekCM_Core/Configuration/Engine/Engine_Server_Configuration.cs` | 346-354 | 9 lines |
| `SobekCM_Engine_Library/Database/Engine_Database.cs` | 4698-4706 | 9 lines |
| `SobekCM_Engine_Library/Database/Engine_Database.cs` | 5360-5368 | 9 lines |
| `SobekCM_Library/AdminViewer/WebContent_Single_AdminViewer.cs` | 313-321 | 9 lines |
| `SobekCM_Library/AdminViewer/WebContent_Single_AdminViewer.cs` | 323-331 | 9 lines |
| `SobekCM_Library/AdminViewer/WebContent_Single_AdminViewer.cs` | 659-667 | 9 lines |
| `SobekCM_Library/AggregationViewer/Viewers/Banner_Search_AggregationViewer.cs` | 148-156 | 9 lines |
| `SobekCM_Library/AggregationViewer/Viewers/Basic_Search_YearRange_AggregationViewer.cs` | 191-199 | 9 lines |
| `SobekCM_Library/AggregationViewer/Viewers/Rotating_Highlight_MimeType_AggregationViewer.cs` | 287-295 | 9 lines |
| `SobekCM_Library/AggregationViewer/Viewers/Rotating_Highlight_Search_AggregationViewer.cs` | 273-281 | 9 lines |
| `SobekCM_Library/Citation/Elements/implemented elements/Projects_Element.cs` | 75-83 | 9 lines |
| `SobekCM_Library/Citation/Elements/implemented elements/Projects_Element.cs` | 148-156 | 9 lines |
| `SobekCM_Library/ItemViewer/Viewers/Milestones_ItemViewer.cs` | 243-251 | 9 lines |
| `SobekCM_Library/ItemViewer/Viewers/OpenTextbook_Divisions_ItemViewer.cs` | 300-308 | 9 lines |
| `SobekCM_Library/ItemViewer/Viewers/OpenTextbook_ItemViewer.cs` | 323-331 | 9 lines |
| `SobekCM_Library/ItemViewer/Viewers/QC_ItemViewer.cs` | 254-262 | 9 lines |
| `SobekCM_Library/MainWriters/DataProvider_MainWriter.cs` | 127-135 | 9 lines |
| `SobekCM_Library/MainWriters/DataProvider_MainWriter.cs` | 225-233 | 9 lines |
| `SobekCM_Library/MySobekViewer/Folder_Mgmt_MySobekViewer.cs` | 211-219 | 9 lines |
| `SobekCM_Library/ResultsViewer/Brief_ResultsViewer.cs` | 103-111 | 9 lines |
| `SobekCM_Library/ResultsViewer/Table_ResultsViewer.cs` | 89-97 | 9 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 23-31 | 9 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 39-47 | 9 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 331-339 | 9 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 480-488 | 9 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 504-512 | 9 lines |
| `SobekCM_Resource_Object/Metadata_File_ReaderWriters/MXF_File_ReaderWriter.cs` | 165-173 | 9 lines |
| `SobekCM_Resource_Object/Metadata_File_ReaderWriters/MXF_File_ReaderWriter.cs` | 266-274 | 9 lines |
| `SobekCM_Resource_Object/Tracking/Tracking_Info.cs` | 53-61 | 9 lines |
| `SobekCM/QueryInitializer.cs` | 557-564 | 8 lines |
| `SobekCM/QueryInitializer.cs` | 612-619 | 8 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 418-425 | 8 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 452-459 | 8 lines |
| `SobekCM_Builder_Library/Modules/Schedulable/SendNewItemEmailsModule.cs` | 85-92 | 8 lines |
| `SobekCM_Core/Aggregations/Complete_Item_Aggregation_Child_Page.cs` | 46-53 | 8 lines |
| `SobekCM_Core/Aggregations/Item_Aggregation_Child_Page.cs` | 37-44 | 8 lines |
| `SobekCM_Core/Configuration/Engine/Engine_Server_Configuration.cs` | 221-228 | 8 lines |
| `SobekCM_Library/AdminViewer/Builder_Folder_Mgmt_AdminViewer.cs` | 389-396 | 8 lines |
| `SobekCM_Library/AdminViewer/Builder_Folder_Mgmt_AdminViewer.cs` | 419-426 | 8 lines |
| `SobekCM_Library/AdminViewer/Skin_Single_AdminViewer.cs` | 569-576 | 8 lines |
| `SobekCM_Library/AdminViewer/WebContent_Single_AdminViewer.cs` | 650-657 | 8 lines |
| `SobekCM_Library/AdminViewer/WebContent_Single_AdminViewer.cs` | 1129-1136 | 8 lines |
| `SobekCM_Library/Citation/Elements/implemented elements/Projects_Element.cs` | 125-132 | 8 lines |
| `SobekCM_Library/ItemViewer/Viewers/Milestones_ItemViewer.cs` | 263-270 | 8 lines |
| `SobekCM_Library/MARC_Record_Z3950_Retriever.cs` | 70-77 | 8 lines |
| `SobekCM_Library/MainWriters/DataProvider_MainWriter.cs` | 52-59 | 8 lines |
| `SobekCM_Library/MainWriters/DataProvider_MainWriter.cs` | 61-68 | 8 lines |
| `SobekCM_Library/MySobekViewer/File_Management_MySobekViewer.cs` | 356-363 | 8 lines |
| `SobekCM_Library/SobekCM_Assistant.cs` | 723-730 | 8 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 63-70 | 8 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 99-106 | 8 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 113-120 | 8 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 184-191 | 8 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 198-205 | 8 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 257-264 | 8 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 293-300 | 8 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 302-309 | 8 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 354-361 | 8 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 403-410 | 8 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 631-638 | 8 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 786-793 | 8 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 970-977 | 8 lines |
| `SobekCM_Resource_Object/Bib_Info/MODS_Info/Subject_Info_HierarchicalGeographic.cs` | 319-326 | 8 lines |
| `SobekCM_Resource_Object/GenericXml/Reader/GenericXmlReader.cs` | 126-133 | 8 lines |
| `SobekCM_Resource_Object/GenericXml/Reader/GenericXmlReader.cs` | 135-142 | 8 lines |
| `SobekCM_Resource_Object/GenericXml/Reader/GenericXmlReader.cs` | 295-302 | 8 lines |
| `SobekCM_Resource_Object/GenericXml/Reader/GenericXmlReader.cs` | 304-311 | 8 lines |
| `SobekCM_Resource_Object/GenericXml/Reader/GenericXmlReader.cs` | 462-469 | 8 lines |
| `SobekCM_Resource_Object/GenericXml/Reader/GenericXmlReader.cs` | 471-478 | 8 lines |
| `SobekCM_Resource_Object/GenericXml/Reader/GenericXmlReader.cs` | 633-640 | 8 lines |
| `SobekCM_Resource_Object/SobekCM_Item.cs` | 1371-1378 | 8 lines |
| `SobekCM_Resource_Object/SobekCM_Item.cs` | 1386-1393 | 8 lines |
| `SobekCM_Resource_Object/SobekCM_Item.cs` | 3123-3130 | 8 lines |
| `SobekCM_Resource_Object/Utilities/SobekCM_METS_Validator.cs` | 148-155 | 8 lines |
| `SobekCM/QueryInitializer.cs` | 580-586 | 7 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 396-402 | 7 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 461-467 | 7 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 496-502 | 7 lines |
| `SobekCM_Builder_Library/Modules/Schedulable/SendNewItemEmailsModule.cs` | 66-72 | 7 lines |
| `SobekCM_Builder_Library/Worker_Controller.cs` | 621-627 | 7 lines |
| `SobekCM_Builder_Library/Worker_Controller.cs` | 695-701 | 7 lines |
| `SobekCM_Builder_Library/Worker_Controller.cs` | 723-729 | 7 lines |
| `SobekCM_Core/Builder/Incoming_Digital_Resource.cs` | 459-465 | 7 lines |
| `SobekCM_Engine_Library/Database/Engine_Database.cs` | 5352-5358 | 7 lines |
| `SobekCM_Engine_Library/Endpoints/WebContentServices.cs` | 925-931 | 7 lines |
| `SobekCM_Engine_Library/Solr/v5/v5_SolrDocument.cs` | 1177-1183 | 7 lines |
| `SobekCM_Library/AggregationViewer/Viewers/Map_Browse_AggregationViewer_Beta.cs` | 43-49 | 7 lines |
| `SobekCM_Library/Citation/Elements/implemented elements/Projects_Element.cs` | 99-105 | 7 lines |
| `SobekCM_Library/ItemViewer/Menu/StandardItemMenuProvider.cs` | 39-45 | 7 lines |
| `SobekCM_Library/ItemViewer/Viewers/Milestones_ItemViewer.cs` | 292-298 | 7 lines |
| `SobekCM_Library/ItemViewer/Viewers/QC_ItemViewer.cs` | 2521-2527 | 7 lines |
| `SobekCM_Library/ItemViewer/Viewers/QC_ItemViewer.cs` | 2688-2694 | 7 lines |
| `SobekCM_Library/MainWriters/DataProvider_MainWriter.cs` | 188-194 | 7 lines |
| `SobekCM_Library/MySobekViewer/Import_Spreadsheet_MySobekViewer.cs` | 46-52 | 7 lines |
| `SobekCM_Library/MySobekViewer/Track_Item_MySobekViewer.cs` | 1528-1534 | 7 lines |
| `SobekCM_Library/ResultsViewer/Google_Map_ResultsViewer.cs` | 39-45 | 7 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 207-213 | 7 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 249-255 | 7 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 363-369 | 7 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 395-401 | 7 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 519-525 | 7 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 711-717 | 7 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 731-737 | 7 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 929-935 | 7 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 947-953 | 7 lines |
| `SobekCM_Resource_Database/SobekCM_Item_Database.cs` | 285-291 | 7 lines |
| `SobekCM_Resource_Database/SobekCM_Item_Database.cs` | 955-961 | 7 lines |
| `SobekCM_Resource_Object/GenericXml/Reader/GenericXmlReader.cs` | 588-594 | 7 lines |
| `SobekCM_Resource_Object/GenericXml/Reader/GenericXmlReader.cs` | 596-602 | 7 lines |
| `SobekCM_Resource_Object/GenericXml/Reader/GenericXmlReader.cs` | 610-616 | 7 lines |
| `SobekCM_Resource_Object/GenericXml/Reader/GenericXmlReader.cs` | 648-654 | 7 lines |
| `SobekCM_Resource_Object/SobekCM_Item.cs` | 179-185 | 7 lines |
| `SobekCM_Resource_Object/SobekCM_Item.cs` | 1034-1040 | 7 lines |
| `SobekCM_Resource_Object/SobekCM_Item.cs` | 3249-3255 | 7 lines |
| `SobekCM/QueryInitializer.cs` | 604-609 | 6 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 438-443 | 6 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 445-450 | 6 lines |
| `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` | 477-482 | 6 lines |
| `SobekCM_Builder_Library/Modules/Items/CreateImageDerivativesModule.cs` | 56-61 | 6 lines |
| `SobekCM_Core/MemoryMgmt/CachedDataManager.cs` | 1459-1464 | 6 lines |
| `SobekCM_Core/MemoryMgmt/CachedDataManager.cs` | 1511-1516 | 6 lines |
| `SobekCM_Engine_Library/Items/SobekCM_Item_Updater.cs` | 124-129 | 6 lines |
| `SobekCM_Engine_Library/Navigation/QueryString_Analyzer.cs` | 1070-1075 | 6 lines |
| `SobekCM_Library/AdminViewer/WebContent_Single_AdminViewer.cs` | 640-645 | 6 lines |
| `SobekCM_Library/Database/SobekCM_Database.cs` | 56-61 | 6 lines |
| `SobekCM_Library/Database/SobekCM_Database.cs` | 73-78 | 6 lines |
| `SobekCM_Library/Database/SobekCM_Database.cs` | 110-115 | 6 lines |
| `SobekCM_Library/Database/SobekCM_Database.cs` | 806-811 | 6 lines |
| `SobekCM_Library/Database/SobekCM_Database.cs` | 851-856 | 6 lines |
| `SobekCM_Library/HTML/Item_HtmlSubwriter.cs` | 590-595 | 6 lines |
| `SobekCM_Library/HTML/Statistics_HtmlSubwriter.cs` | 1252-1257 | 6 lines |
| `SobekCM_Library/HTML/Statistics_HtmlSubwriter.cs` | 1556-1561 | 6 lines |
| `SobekCM_Library/HTML/Statistics_HtmlSubwriter.cs` | 1827-1832 | 6 lines |
| `SobekCM_Library/HTML/Web_Content_HtmlSubwriter.cs` | 479-484 | 6 lines |
| `SobekCM_Library/ItemViewer/Viewers/Milestones_ItemViewer.cs` | 285-290 | 6 lines |
| `SobekCM_Library/ItemViewer/Viewers/Milestones_ItemViewer.cs` | 302-307 | 6 lines |
| `SobekCM_Library/ItemViewer/Viewers/QC_ItemViewer.cs` | 1790-1795 | 6 lines |
| `SobekCM_Library/MARC_Record_Z3950_Retriever.cs` | 2-7 | 6 lines |
| `SobekCM_Library/MARC_Record_Z3950_Retriever.cs` | 10-15 | 6 lines |
| `SobekCM_Library/MARC_Record_Z3950_Retriever.cs` | 57-62 | 6 lines |
| `SobekCM_Library/MainWriters/DataProvider_MainWriter.cs` | 152-157 | 6 lines |
| `SobekCM_Library/MySobekViewer/Edit_TEI_Item_MySobekViewer.cs` | 585-590 | 6 lines |
| `SobekCM_Library/MySobekViewer/Folder_Mgmt_MySobekViewer.cs` | 496-501 | 6 lines |
| `SobekCM_Library/MySobekViewer/New_Group_And_Item_MySobekViewer.cs` | 1029-1034 | 6 lines |
| `SobekCM_Library/MySobekViewer/New_TEI_MySobekViewer.cs` | 928-933 | 6 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 384-389 | 6 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 467-472 | 6 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 624-629 | 6 lines |
| `SobekCM_Library/Static_Pages_Builder.cs` | 920-925 | 6 lines |
| `SobekCM_Resource_Object/Behaviors/Behaviors_Info.cs` | 634-639 | 6 lines |
| `SobekCM_Resource_Object/GenericXml/Reader/GenericXmlReader.cs` | 254-259 | 6 lines |
| `SobekCM_Resource_Object/GenericXml/Reader/GenericXmlReader.cs` | 421-426 | 6 lines |
| `SobekCM_Resource_Object/Metadata_File_ReaderWriters/INFO_File_ReaderWriter.cs` | 136-141 | 6 lines |
| `SobekCM_Resource_Object/Metadata_File_ReaderWriters/MXF_File_ReaderWriter.cs` | 176-181 | 6 lines |
| `SobekCM_Resource_Object/SobekCM_Item.cs` | 1348-1353 | 6 lines |
| `SobekCM_Resource_Object/SobekCM_Item.cs` | 1411-1416 | 6 lines |
| `SobekCM_Resource_Object/SobekCM_Item.cs` | 1421-1426 | 6 lines |
| `SobekCM_Resource_Object/Utilities/Image_Derivative_Creation_Processor.cs` | 739-744 | 6 lines |
| `SobekCM_Resource_Object/Utilities/Image_Derivative_Creation_Processor.cs` | 805-810 | 6 lines |
| `SobekCM_Tools/SecurityInfo.cs` | 53-58 | 6 lines |

## Full list grouped by file

Files ordered by number of blocks (most first), each block listed with its line range and size.

### `SobekCM_Library/Static_Pages_Builder.cs` (55 blocks)

- Lines 23-31 (9 lines, 9 code-like)
- Lines 39-47 (9 lines, 8 code-like)
- Lines 63-70 (8 lines, 7 code-like)
- Lines 84-94 (11 lines, 11 code-like)
- Lines 99-106 (8 lines, 8 code-like)
- Lines 113-120 (8 lines, 8 code-like)
- Lines 142-163 (22 lines, 22 code-like)
- Lines 184-191 (8 lines, 8 code-like)
- Lines 198-205 (8 lines, 6 code-like)
- Lines 207-213 (7 lines, 7 code-like)
- Lines 233-245 (13 lines, 11 code-like)
- Lines 249-255 (7 lines, 7 code-like)
- Lines 257-264 (8 lines, 7 code-like)
- Lines 278-288 (11 lines, 10 code-like)
- Lines 293-300 (8 lines, 7 code-like)
- Lines 302-309 (8 lines, 7 code-like)
- Lines 317-328 (12 lines, 11 code-like)
- Lines 331-339 (9 lines, 9 code-like)
- Lines 341-352 (12 lines, 11 code-like)
- Lines 354-361 (8 lines, 7 code-like)
- Lines 363-369 (7 lines, 6 code-like)
- Lines 384-389 (6 lines, 6 code-like)
- Lines 395-401 (7 lines, 6 code-like)
- Lines 403-410 (8 lines, 7 code-like)
- Lines 418-428 (11 lines, 11 code-like)
- Lines 445-454 (10 lines, 9 code-like)
- Lines 467-472 (6 lines, 5 code-like)
- Lines 480-488 (9 lines, 8 code-like)
- Lines 504-512 (9 lines, 6 code-like)
- Lines 519-525 (7 lines, 5 code-like)
- Lines 541-551 (11 lines, 9 code-like)
- Lines 553-567 (15 lines, 11 code-like)
- Lines 569-582 (14 lines, 12 code-like)
- Lines 587-597 (11 lines, 10 code-like)
- Lines 602-616 (15 lines, 13 code-like)
- Lines 624-629 (6 lines, 5 code-like)
- Lines 631-638 (8 lines, 7 code-like)
- Lines 643-655 (13 lines, 11 code-like)
- Lines 666-678 (13 lines, 12 code-like)
- Lines 683-692 (10 lines, 9 code-like)
- Lines 711-717 (7 lines, 7 code-like)
- Lines 719-728 (10 lines, 9 code-like)
- Lines 731-737 (7 lines, 7 code-like)
- Lines 739-764 (26 lines, 24 code-like)
- Lines 766-784 (19 lines, 17 code-like)
- Lines 786-793 (8 lines, 6 code-like)
- Lines 800-827 (28 lines, 25 code-like)
- Lines 833-842 (10 lines, 10 code-like)
- Lines 867-878 (12 lines, 12 code-like)
- Lines 887-897 (11 lines, 11 code-like)
- Lines 901-912 (12 lines, 12 code-like)
- Lines 920-925 (6 lines, 6 code-like)
- Lines 929-935 (7 lines, 7 code-like)
- Lines 947-953 (7 lines, 7 code-like)
- Lines 970-977 (8 lines, 8 code-like)

### `SobekCM/QueryInitializerHelpers/UserObjectInitializer.cs` (17 blocks)

- Lines 314-323 (10 lines, 10 code-like)
- Lines 325-339 (15 lines, 14 code-like)
- Lines 343-351 (9 lines, 8 code-like)
- Lines 359-370 (12 lines, 10 code-like)
- Lines 372-381 (10 lines, 10 code-like)
- Lines 383-394 (12 lines, 11 code-like)
- Lines 396-402 (7 lines, 7 code-like)
- Lines 404-416 (13 lines, 12 code-like)
- Lines 418-425 (8 lines, 7 code-like)
- Lines 427-436 (10 lines, 10 code-like)
- Lines 438-443 (6 lines, 5 code-like)
- Lines 445-450 (6 lines, 6 code-like)
- Lines 452-459 (8 lines, 7 code-like)
- Lines 461-467 (7 lines, 6 code-like)
- Lines 477-482 (6 lines, 5 code-like)
- Lines 484-494 (11 lines, 11 code-like)
- Lines 496-502 (7 lines, 7 code-like)

### `SobekCM_Resource_Object/SobekCM_Item.cs` (13 blocks)

- Lines 179-185 (7 lines, 7 code-like)
- Lines 1034-1040 (7 lines, 6 code-like)
- Lines 1327-1337 (11 lines, 10 code-like)
- Lines 1348-1353 (6 lines, 6 code-like)
- Lines 1371-1378 (8 lines, 7 code-like)
- Lines 1386-1393 (8 lines, 8 code-like)
- Lines 1400-1409 (10 lines, 10 code-like)
- Lines 1411-1416 (6 lines, 6 code-like)
- Lines 1421-1426 (6 lines, 5 code-like)
- Lines 1428-1443 (16 lines, 15 code-like)
- Lines 2010-2029 (20 lines, 20 code-like)
- Lines 3123-3130 (8 lines, 8 code-like)
- Lines 3249-3255 (7 lines, 7 code-like)

### `SobekCM_Resource_Object/GenericXml/Reader/GenericXmlReader.cs` (13 blocks)

- Lines 126-133 (8 lines, 7 code-like)
- Lines 135-142 (8 lines, 7 code-like)
- Lines 254-259 (6 lines, 6 code-like)
- Lines 295-302 (8 lines, 7 code-like)
- Lines 304-311 (8 lines, 7 code-like)
- Lines 421-426 (6 lines, 6 code-like)
- Lines 462-469 (8 lines, 7 code-like)
- Lines 471-478 (8 lines, 7 code-like)
- Lines 588-594 (7 lines, 5 code-like)
- Lines 596-602 (7 lines, 6 code-like)
- Lines 610-616 (7 lines, 6 code-like)
- Lines 633-640 (8 lines, 7 code-like)
- Lines 648-654 (7 lines, 6 code-like)

### `SobekCM_Core/Configuration/Engine/Engine_Server_Configuration.cs` (11 blocks)

- Lines 161-171 (11 lines, 10 code-like)
- Lines 178-193 (16 lines, 16 code-like)
- Lines 203-211 (9 lines, 9 code-like)
- Lines 221-228 (8 lines, 8 code-like)
- Lines 230-239 (10 lines, 10 code-like)
- Lines 248-261 (14 lines, 14 code-like)
- Lines 267-278 (12 lines, 12 code-like)
- Lines 285-302 (18 lines, 18 code-like)
- Lines 304-314 (11 lines, 11 code-like)
- Lines 316-336 (21 lines, 21 code-like)
- Lines 346-354 (9 lines, 9 code-like)

### `SobekCM_Library/MainWriters/DataProvider_MainWriter.cs` (11 blocks)

- Lines 52-59 (8 lines, 6 code-like)
- Lines 61-68 (8 lines, 6 code-like)
- Lines 70-79 (10 lines, 9 code-like)
- Lines 108-119 (12 lines, 12 code-like)
- Lines 127-135 (9 lines, 9 code-like)
- Lines 152-157 (6 lines, 6 code-like)
- Lines 162-176 (15 lines, 15 code-like)
- Lines 188-194 (7 lines, 6 code-like)
- Lines 196-206 (11 lines, 10 code-like)
- Lines 213-222 (10 lines, 9 code-like)
- Lines 225-233 (9 lines, 9 code-like)

### `SobekCM/QueryInitializer.cs` (10 blocks)

- Lines 546-555 (10 lines, 10 code-like)
- Lines 557-564 (8 lines, 8 code-like)
- Lines 570-578 (9 lines, 8 code-like)
- Lines 580-586 (7 lines, 6 code-like)
- Lines 594-602 (9 lines, 9 code-like)
- Lines 604-609 (6 lines, 5 code-like)
- Lines 612-619 (8 lines, 7 code-like)
- Lines 624-652 (29 lines, 28 code-like)
- Lines 654-664 (11 lines, 11 code-like)
- Lines 666-674 (9 lines, 7 code-like)

### `SobekCM_Resource_Object/Metadata_File_ReaderWriters/MXF_File_ReaderWriter.cs` (9 blocks)

- Lines 165-173 (9 lines, 8 code-like)
- Lines 176-181 (6 lines, 5 code-like)
- Lines 183-196 (14 lines, 11 code-like)
- Lines 207-232 (26 lines, 24 code-like)
- Lines 240-249 (10 lines, 8 code-like)
- Lines 251-264 (14 lines, 12 code-like)
- Lines 266-274 (9 lines, 7 code-like)
- Lines 291-300 (10 lines, 9 code-like)
- Lines 302-316 (15 lines, 14 code-like)

### `SobekCM_Library/AdminViewer/WebContent_Single_AdminViewer.cs` (9 blocks)

- Lines 313-321 (9 lines, 9 code-like)
- Lines 323-331 (9 lines, 9 code-like)
- Lines 640-645 (6 lines, 6 code-like)
- Lines 650-657 (8 lines, 7 code-like)
- Lines 659-667 (9 lines, 9 code-like)
- Lines 675-691 (17 lines, 16 code-like)
- Lines 697-717 (21 lines, 21 code-like)
- Lines 796-806 (11 lines, 11 code-like)
- Lines 1129-1136 (8 lines, 7 code-like)

### `SobekCM_Library/Database/SobekCM_Database.cs` (8 blocks)

- Lines 56-61 (6 lines, 5 code-like)
- Lines 73-78 (6 lines, 5 code-like)
- Lines 89-108 (20 lines, 20 code-like)
- Lines 110-115 (6 lines, 6 code-like)
- Lines 806-811 (6 lines, 5 code-like)
- Lines 830-844 (15 lines, 14 code-like)
- Lines 851-856 (6 lines, 5 code-like)
- Lines 874-888 (15 lines, 14 code-like)

### `SobekCM_Builder_Library/Worker_Controller.cs` (8 blocks)

- Lines 540-555 (16 lines, 16 code-like)
- Lines 609-619 (11 lines, 10 code-like)
- Lines 621-627 (7 lines, 7 code-like)
- Lines 650-659 (10 lines, 9 code-like)
- Lines 682-693 (12 lines, 11 code-like)
- Lines 695-701 (7 lines, 5 code-like)
- Lines 703-719 (17 lines, 16 code-like)
- Lines 723-729 (7 lines, 7 code-like)

### `SobekCM_Library/Citation/Elements/implemented elements/Projects_Element.cs` (6 blocks)

- Lines 42-58 (17 lines, 17 code-like)
- Lines 75-83 (9 lines, 8 code-like)
- Lines 99-105 (7 lines, 7 code-like)
- Lines 114-123 (10 lines, 10 code-like)
- Lines 125-132 (8 lines, 8 code-like)
- Lines 148-156 (9 lines, 9 code-like)

### `SobekCM_Library/ItemViewer/Viewers/Milestones_ItemViewer.cs` (6 blocks)

- Lines 228-241 (14 lines, 14 code-like)
- Lines 243-251 (9 lines, 9 code-like)
- Lines 263-270 (8 lines, 8 code-like)
- Lines 285-290 (6 lines, 6 code-like)
- Lines 292-298 (7 lines, 7 code-like)
- Lines 302-307 (6 lines, 6 code-like)

### `SobekCM_Resource_Object/Utilities/Image_Derivative_Creation_Processor.cs` (5 blocks)

- Lines 705-731 (27 lines, 27 code-like)
- Lines 739-744 (6 lines, 6 code-like)
- Lines 749-774 (26 lines, 26 code-like)
- Lines 790-803 (14 lines, 14 code-like)
- Lines 805-810 (6 lines, 6 code-like)

### `SobekCM_Engine_Library/Database/Engine_Database.cs` (5 blocks)

- Lines 1296-1314 (19 lines, 19 code-like)
- Lines 4672-4692 (21 lines, 20 code-like)
- Lines 4698-4706 (9 lines, 8 code-like)
- Lines 5352-5358 (7 lines, 6 code-like)
- Lines 5360-5368 (9 lines, 8 code-like)

### `SobekCM_Builder_Library/Modules/Schedulable/SendNewItemEmailsModule.cs` (5 blocks)

- Lines 32-43 (12 lines, 11 code-like)
- Lines 45-60 (16 lines, 14 code-like)
- Lines 66-72 (7 lines, 6 code-like)
- Lines 85-92 (8 lines, 7 code-like)
- Lines 94-103 (10 lines, 9 code-like)

### `SobekCM_Library/ItemViewer/Viewers/QC_ItemViewer.cs` (5 blocks)

- Lines 254-262 (9 lines, 9 code-like)
- Lines 1790-1795 (6 lines, 6 code-like)
- Lines 2521-2527 (7 lines, 5 code-like)
- Lines 2575-2584 (10 lines, 8 code-like)
- Lines 2688-2694 (7 lines, 7 code-like)

### `SobekCM_Library/HTML/Item_HtmlSubwriter.cs` (4 blocks)

- Lines 319-329 (11 lines, 8 code-like)
- Lines 331-344 (14 lines, 14 code-like)
- Lines 353-374 (22 lines, 20 code-like)
- Lines 590-595 (6 lines, 6 code-like)

### `SobekCM_Library/MARC_Record_Z3950_Retriever.cs` (4 blocks)

- Lines 2-7 (6 lines, 6 code-like)
- Lines 10-15 (6 lines, 6 code-like)
- Lines 57-62 (6 lines, 5 code-like)
- Lines 70-77 (8 lines, 8 code-like)

### `SobekCM_Resource_Object/Metadata_File_ReaderWriters/INFO_File_ReaderWriter.cs` (3 blocks)

- Lines 120-134 (15 lines, 13 code-like)
- Lines 136-141 (6 lines, 5 code-like)
- Lines 151-177 (27 lines, 25 code-like)

### `SobekCM_Library/MySobekViewer/New_TEI_MySobekViewer.cs` (3 blocks)

- Lines 928-933 (6 lines, 6 code-like)
- Lines 935-951 (17 lines, 14 code-like)
- Lines 2695-2706 (12 lines, 11 code-like)

### `SobekCM_Library/HTML/Statistics_HtmlSubwriter.cs` (3 blocks)

- Lines 1252-1257 (6 lines, 6 code-like)
- Lines 1556-1561 (6 lines, 6 code-like)
- Lines 1827-1832 (6 lines, 6 code-like)

### `SobekCM_Library/AggregationViewer/Viewers/Map_Browse_AggregationViewer_Beta.cs` (2 blocks)

- Lines 43-49 (7 lines, 7 code-like)
- Lines 78-326 (249 lines, 249 code-like)

### `SobekCM_Library/HTML/Web_Content_HtmlSubwriter.cs` (2 blocks)

- Lines 479-484 (6 lines, 6 code-like)
- Lines 490-530 (41 lines, 41 code-like)

### `SobekCM_Tools/SecurityInfo.cs` (2 blocks)

- Lines 26-48 (23 lines, 22 code-like)
- Lines 53-58 (6 lines, 6 code-like)

### `SobekCM_Engine_Library/Items/SobekCM_Item_Updater.cs` (2 blocks)

- Lines 124-129 (6 lines, 6 code-like)
- Lines 131-148 (18 lines, 15 code-like)

### `SobekCM_Library/MySobekViewer/Edit_TEI_Item_MySobekViewer.cs` (2 blocks)

- Lines 585-590 (6 lines, 6 code-like)
- Lines 592-608 (17 lines, 14 code-like)

### `SobekCM_Library/MySobekViewer/New_Group_And_Item_MySobekViewer.cs` (2 blocks)

- Lines 1029-1034 (6 lines, 6 code-like)
- Lines 1036-1052 (17 lines, 14 code-like)

### `SobekCM_Library/RequestCache.cs` (2 blocks)

- Lines 36-52 (17 lines, 11 code-like)
- Lines 66-80 (15 lines, 10 code-like)

### `SobekCM_Library/MySobekViewer/File_Management_MySobekViewer.cs` (2 blocks)

- Lines 356-363 (8 lines, 8 code-like)
- Lines 365-380 (16 lines, 14 code-like)

### `SobekCM_Library/HTML/HtmlHelpers/MainMenus_HtmlHelper.cs` (2 blocks)

- Lines 918-930 (13 lines, 13 code-like)
- Lines 1160-1169 (10 lines, 10 code-like)

### `SobekCM_Resource_Object/Utilities/SobekCM_METS_Validator.cs` (2 blocks)

- Lines 148-155 (8 lines, 4 code-like)
- Lines 178-190 (13 lines, 13 code-like)

### `SobekCM_Library/MySobekViewer/Track_Item_MySobekViewer.cs` (2 blocks)

- Lines 1528-1534 (7 lines, 6 code-like)
- Lines 1536-1547 (12 lines, 12 code-like)

### `SobekCM_Library/Citation/Elements/nonstandard elements/Viewer_Element.cs` (2 blocks)

- Lines 149-158 (10 lines, 10 code-like)
- Lines 282-292 (11 lines, 11 code-like)

### `SobekCM_Library/Citation/Elements/nonstandard elements/Other_Title_Form_Element.cs` (2 blocks)

- Lines 105-114 (10 lines, 10 code-like)
- Lines 328-337 (10 lines, 10 code-like)

### `SobekCM_Library/MySobekViewer/Folder_Mgmt_MySobekViewer.cs` (2 blocks)

- Lines 211-219 (9 lines, 9 code-like)
- Lines 496-501 (6 lines, 6 code-like)

### `SobekCM_Library/AdminViewer/Builder_Folder_Mgmt_AdminViewer.cs` (2 blocks)

- Lines 389-396 (8 lines, 8 code-like)
- Lines 419-426 (8 lines, 8 code-like)

### `SobekCM_Resource_Database/SobekCM_Item_Database.cs` (2 blocks)

- Lines 285-291 (7 lines, 7 code-like)
- Lines 955-961 (7 lines, 7 code-like)

### `SobekCM_Core/MemoryMgmt/CachedDataManager.cs` (2 blocks)

- Lines 1459-1464 (6 lines, 5 code-like)
- Lines 1511-1516 (6 lines, 5 code-like)

### `SobekCM_Library/AggregationViewer/Viewers/Map_Search_AggregationViewer_Beta.cs` (1 block)

- Lines 84-332 (249 lines, 249 code-like)

### `SobekCM_Engine_Library/Items/BriefItems/Mappers/Subjects_BriefItemMapper.cs` (1 block)

- Lines 29-88 (60 lines, 60 code-like)

### `SobekCM_Library/AdminViewer/Builder_AdminViewer.cs` (1 block)

- Lines 528-551 (24 lines, 24 code-like)

### `SobekCM_Library/ItemViewer/HtmlSectionWriters/StandardMenu_ItemSectionWriter.cs` (1 block)

- Lines 26-48 (23 lines, 21 code-like)

### `SobekCM_Library/AdminViewer/Skins_AdminViewer.cs` (1 block)

- Lines 287-306 (20 lines, 20 code-like)

### `SobekCM_Builder_Library/Modules/Items/CreateStaticVersionModule.cs` (1 block)

- Lines 104-120 (17 lines, 16 code-like)

### `SobekCM_Library/ResultsViewer/abstract_ResultsViewer.cs` (1 block)

- Lines 109-125 (17 lines, 16 code-like)

### `SobekCM_Builder_Library/Modules/Schedulable/SolrLuceneIndexOptimizationModule.cs` (1 block)

- Lines 18-33 (16 lines, 16 code-like)

### `SobekCM_Library/Citation/Elements/nonstandard elements/Serial_Hierarchy_Panel_Element.cs` (1 block)

- Lines 114-129 (16 lines, 15 code-like)

### `SobekCM_Library/Citation/Elements/implemented elements/Language_Script_Element.cs` (1 block)

- Lines 55-68 (14 lines, 14 code-like)

### `SobekCM_Library/MySobekViewer/Delete_Item_MySobekViewer.cs` (1 block)

- Lines 160-173 (14 lines, 12 code-like)

### `SobekCM_Library/AdminViewer/Aggregation_Single_AdminViewer.cs` (1 block)

- Lines 2032-2043 (12 lines, 12 code-like)

### `SobekCM_Library/AdminViewer/WebContent_History_AdminViewer.cs` (1 block)

- Lines 43-54 (12 lines, 11 code-like)

### `SobekCM_Library/AdminViewer/WebContent_Mgmt_AdminViewer.cs` (1 block)

- Lines 43-54 (12 lines, 11 code-like)

### `SobekCM_Library/AdminViewer/WebContent_Usage_AdminViewer.cs` (1 block)

- Lines 49-60 (12 lines, 11 code-like)

### `SobekCM_Library/HTML/Aggregation_HtmlSubwriter.cs` (1 block)

- Lines 124-135 (12 lines, 11 code-like)

### `SobekCM_Library/ItemViewer/Viewers/Citation_Standard_ItemViewer.cs` (1 block)

- Lines 743-754 (12 lines, 12 code-like)

### `SobekCM_Engine_Library/Configuration/Configuration_Files_Reader.cs` (1 block)

- Lines 2807-2817 (11 lines, 11 code-like)

### `SobekCM_Library/AdminViewer/Settings_AdminViewer.cs` (1 block)

- Lines 2717-2727 (11 lines, 11 code-like)

### `SobekCM_Core/WebContent/HTML_Based_Content.cs` (1 block)

- Lines 419-428 (10 lines, 10 code-like)

### `SobekCM_Library/AdminViewer/User_Group_AdminViewer.cs` (1 block)

- Lines 475-484 (10 lines, 9 code-like)

### `SobekCM_Library/ResultsViewer/Google_Map_ResultsViewer_Beta.cs` (1 block)

- Lines 47-56 (10 lines, 10 code-like)

### `SobekCM/Program.cs` (1 block)

- Lines 433-441 (9 lines, 8 code-like)

### `SobekCM_Library/AggregationViewer/Viewers/Banner_Search_AggregationViewer.cs` (1 block)

- Lines 148-156 (9 lines, 9 code-like)

### `SobekCM_Library/AggregationViewer/Viewers/Basic_Search_YearRange_AggregationViewer.cs` (1 block)

- Lines 191-199 (9 lines, 9 code-like)

### `SobekCM_Library/AggregationViewer/Viewers/Rotating_Highlight_MimeType_AggregationViewer.cs` (1 block)

- Lines 287-295 (9 lines, 9 code-like)

### `SobekCM_Library/AggregationViewer/Viewers/Rotating_Highlight_Search_AggregationViewer.cs` (1 block)

- Lines 273-281 (9 lines, 9 code-like)

### `SobekCM_Library/ItemViewer/Viewers/OpenTextbook_Divisions_ItemViewer.cs` (1 block)

- Lines 300-308 (9 lines, 8 code-like)

### `SobekCM_Library/ItemViewer/Viewers/OpenTextbook_ItemViewer.cs` (1 block)

- Lines 323-331 (9 lines, 8 code-like)

### `SobekCM_Library/ResultsViewer/Brief_ResultsViewer.cs` (1 block)

- Lines 103-111 (9 lines, 9 code-like)

### `SobekCM_Library/ResultsViewer/Table_ResultsViewer.cs` (1 block)

- Lines 89-97 (9 lines, 9 code-like)

### `SobekCM_Resource_Object/Tracking/Tracking_Info.cs` (1 block)

- Lines 53-61 (9 lines, 8 code-like)

### `SobekCM_Core/Aggregations/Complete_Item_Aggregation_Child_Page.cs` (1 block)

- Lines 46-53 (8 lines, 8 code-like)

### `SobekCM_Core/Aggregations/Item_Aggregation_Child_Page.cs` (1 block)

- Lines 37-44 (8 lines, 8 code-like)

### `SobekCM_Library/AdminViewer/Skin_Single_AdminViewer.cs` (1 block)

- Lines 569-576 (8 lines, 8 code-like)

### `SobekCM_Library/SobekCM_Assistant.cs` (1 block)

- Lines 723-730 (8 lines, 8 code-like)

### `SobekCM_Resource_Object/Bib_Info/MODS_Info/Subject_Info_HierarchicalGeographic.cs` (1 block)

- Lines 319-326 (8 lines, 7 code-like)

### `SobekCM_Core/Builder/Incoming_Digital_Resource.cs` (1 block)

- Lines 459-465 (7 lines, 6 code-like)

### `SobekCM_Engine_Library/Endpoints/WebContentServices.cs` (1 block)

- Lines 925-931 (7 lines, 7 code-like)

### `SobekCM_Engine_Library/Solr/v5/v5_SolrDocument.cs` (1 block)

- Lines 1177-1183 (7 lines, 7 code-like)

### `SobekCM_Library/ItemViewer/Menu/StandardItemMenuProvider.cs` (1 block)

- Lines 39-45 (7 lines, 7 code-like)

### `SobekCM_Library/MySobekViewer/Import_Spreadsheet_MySobekViewer.cs` (1 block)

- Lines 46-52 (7 lines, 6 code-like)

### `SobekCM_Library/ResultsViewer/Google_Map_ResultsViewer.cs` (1 block)

- Lines 39-45 (7 lines, 6 code-like)

### `SobekCM_Builder_Library/Modules/Items/CreateImageDerivativesModule.cs` (1 block)

- Lines 56-61 (6 lines, 6 code-like)

### `SobekCM_Engine_Library/Navigation/QueryString_Analyzer.cs` (1 block)

- Lines 1070-1075 (6 lines, 6 code-like)

### `SobekCM_Resource_Object/Behaviors/Behaviors_Info.cs` (1 block)

- Lines 634-639 (6 lines, 6 code-like)
