SET NOCOUNT ON;

/*
	SobekCM Version 5.0.0 seed data verification script

	Compares this database's actual row-level data against the full seed data
	set captured in seed_complete.sql, table by table. Run after each upgrade
	to confirm every expected seed row is present.

	Only PRINTs when an expected row is missing - a clean run produces just the
	final "Seed data comparison complete." line.

	Matching is by full-row equality across all columns, except the following
	(still shown in the PRINT message for context, just not required to match):
	  - SobekCM_Settings: Setting_Value (varies per instance), SettingID (identity,
	    not referenced by Setting_Key lookups)
	  - SobekCM_Mime_Types: MimeTypeID (identity)
	  - Tracking_WorkFlow: WorkFlowID (identity)
	  - Tracking_Disposition_Type: DispositionID (identity)
	  - SobekCM_Item_Viewer_Types: ItemViewTypeID (identity)
	  - SobekCM_Metadata_Types: MetadataTypeID (identity)

	Every other table's identity column IS still part of the match, because it's
	referenced elsewhere (FK columns in other tables, or hardcoded IDs in stored
	procedures) and a mismatch there is a meaningful bug, not just drift.

	Extra rows (present in the database but not in seed_complete.sql) are NOT
	reported - only missing rows.
*/


-------------------------------------------------------------------------------
-- SobekCM_Builder_Module
-------------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#Expected_SobekCM_Builder_Module') IS NOT NULL DROP TABLE #Expected_SobekCM_Builder_Module;

CREATE TABLE #Expected_SobekCM_Builder_Module (
	[ModuleID] int NULL,
	[ModuleSetID] int NULL,
	[ModuleDesc] varchar(200) NULL,
	[Assembly] varchar(250) NULL,
	[Class] varchar(500) NULL,
	[Enabled] bit NULL,
	[Order] int NULL,
	[Argument1] varchar(max) NULL,
	[Argument2] varchar(max) NULL,
	[Argument3] varchar(max) NULL
);

INSERT INTO #Expected_SobekCM_Builder_Module ([ModuleID], [ModuleSetID], [ModuleDesc], [Assembly], [Class], [Enabled], [Order], [Argument1], [Argument2], [Argument3]) VALUES
(1,1,N'Load reports from FDA (Florida Digital Archives) for Florida universities',NULL,N'SobekCM.Builder_Library.Modules.PreProcess.ProcessPendingFdaReportsModule',0,1,NULL,NULL,NULL),
(2,2,N'Build the aggregation browse files',NULL,N'SobekCM.Builder_Library.Modules.PostProcess.BuildAggregationBrowsesModule',1,1,NULL,NULL,NULL),
(3,3,N'Convert office files to PDFs',NULL,N'SobekCM.Builder_Library.Modules.Items.ConvertOfficeFilesToPdfModule',1,1,NULL,NULL,NULL),
(4,3,N'Extract text from all PDFs',NULL,N'SobekCM.Builder_Library.Modules.Items.ExtractTextFromPdfModule',1,2,NULL,NULL,NULL),
(5,3,N'Create thumbnails for all PDFs',NULL,N'SobekCM.Builder_Library.Modules.Items.CreatePdfThumbnailModule',1,3,NULL,NULL,NULL),
(6,3,N'Extract the text from included HTML files',NULL,N'SobekCM.Builder_Library.Modules.Items.ExtractTextFromHtmlModule',1,4,NULL,NULL,NULL),
(7,3,N'Extract the text from included (non-standard) XML files',NULL,N'SobekCM.Builder_Library.Modules.Items.ExtractTextFromXmlModule',1,5,NULL,NULL,NULL),
(8,3,N'OCR tiff files',NULL,N'SobekCM.Builder_Library.Modules.Items.OcrTiffsModule',1,6,NULL,NULL,NULL),
(9,3,N'Clean any dirty ocr (non-unicode friendly)',NULL,N'SobekCM.Builder_Library.Modules.Items.CleanDirtyOcrModule',0,7,NULL,NULL,NULL),
(10,3,N'Check for SSNs in any loaded text',NULL,N'SobekCM.Builder_Library.Modules.Items.CheckForSsnModule',1,8,NULL,NULL,NULL),
(11,3,N'Handle extra large JPEGs',NULL,N'SobekCM.Builder_Library.Modules.Items.ConvertLargeJpegsItemModule',1,9,NULL,NULL,NULL),
(12,3,N'Create image derivatives (jpegs and jpeg2000s)',NULL,N'SobekCM.Builder_Library.Modules.Items.CreateImageDerivativesModule',1,10,NULL,NULL,NULL),
(13,3,N'Copy all incoming files to the archive folder',NULL,N'SobekCM.Builder_Library.Modules.Items.CopyToArchiveFolderModule',1,11,NULL,NULL,NULL),
(14,3,N'Move files to the image server',NULL,N'SobekCM.Builder_Library.Modules.Items.MoveFilesToImageServerModule',1,12,NULL,NULL,NULL),
(15,3,N'Reload the METS and basic database info',NULL,N'SobekCM.Builder_Library.Modules.Items.ReloadMetsAndBasicDbInfoModule',1,13,NULL,NULL,NULL),
(16,3,N'Update JPEG attributes (width and height)',NULL,N'SobekCM.Builder_Library.Modules.Items.UpdateJpegAttributesModule',1,14,NULL,NULL,NULL),
(17,3,N'Attach all non-image files to the item',NULL,N'SobekCM.Builder_Library.Modules.Items.AttachAllNonImageFilesModule',1,15,NULL,NULL,NULL),
(18,3,N'Add new image files (and associated views) to the item',NULL,N'SobekCM.Builder_Library.Modules.Items.AddNewImagesAndViewsModule',0,16,NULL,NULL,NULL),
(19,3,N'Ensure a main thumbnail is referenced',NULL,N'SobekCM.Builder_Library.Modules.Items.EnsureMainThumbnailModule',1,17,NULL,NULL,NULL),
(20,3,N'Get number of pages for PDF-only types',NULL,N'SobekCM.Builder_Library.Modules.Items.GetPageCountFromPdfModule',1,18,NULL,NULL,NULL),
(21,3,N'Update the web.config for restricted items',NULL,N'SobekCM.Builder_Library.Modules.Items.UpdateWebConfigModule',1,19,NULL,NULL,NULL),
(22,3,N'Save the service METS file',NULL,N'SobekCM.Builder_Library.Modules.Items.SaveServiceMetsModule',1,20,NULL,NULL,NULL),
(23,3,N'Save a Marc21 XML file',NULL,N'SobekCM.Builder_Library.Modules.Items.SaveMarcXmlModule',1,21,NULL,NULL,NULL),
(24,3,N'Save to the database',NULL,N'SobekCM.Builder_Library.Modules.Items.SaveToDatabaseModule',1,22,NULL,NULL,NULL),
(25,3,N'Save to the old solr/lucene legacy indexes',NULL,N'SobekCM.Builder_Library.Modules.Items.SaveToSolrLuceneModule_Legacy',1,23,NULL,NULL,NULL),
(26,3,N'Clean the web resource folder',NULL,N'SobekCM.Builder_Library.Modules.Items.CleanWebResourceFolderModule',1,25,NULL,NULL,NULL),
(27,3,N'Build statice version for SEO',NULL,N'SobekCM.Builder_Library.Modules.Items.CreateStaticVersionModule',1,26,NULL,NULL,NULL),
(28,3,N'Add tracking information',NULL,N'SobekCM.Builder_Library.Modules.Items.AddTrackingWorkflowModule',1,27,NULL,NULL,NULL),
(29,4,N'Loads the METS and basic database info',NULL,N'SobekCM.Builder_Library.Modules.Items.ReloadMetsAndBasicDbInfoModule',1,1,NULL,NULL,NULL),
(30,4,N'Delete item in database and folder',NULL,N'SobekCM.Builder_Library.Modules.Items.DeleteItemModule',1,2,NULL,NULL,NULL),
(31,5,N'Expire old builder logs',NULL,N'SobekCM.Builder_Library.Modules.Schedulable.ExpireOldLogEntriesModule',1,1,NULL,NULL,NULL),
(32,6,N'Rebuild all aggregation browse files',NULL,N'SobekCM.Builder_Library.Modules.Schedulable.RebuildAllAggregationBrowsesModule',1,1,NULL,NULL,NULL),
(33,7,N'Send new item emails',NULL,N'SobekCM.Builder_Library.Modules.Schedulable.SendNewItemEmailsModule',1,1,NULL,NULL,NULL),
(34,8,N'Solr/Lucene index optimization',NULL,N'SobekCM.Builder_Library.Modules.Schedulable.SolrLuceneIndexOptimizationModule',1,1,NULL,NULL,NULL),
(35,9,N'Update cached aggregation browses',NULL,N'SobekCM.Builder_Library.Modules.Schedulable.UpdatedCachedAggregationMetadataModule',1,1,NULL,NULL,NULL),
(36,10,N'Check packages for age and move',NULL,N'SobekCM.Builder_Library.Modules.Folders.MoveAgedPackagesToProcessModule',1,1,NULL,NULL,NULL),
(37,10,N'Check for any bib id restrictions on this folder',NULL,N'SobekCM.Builder_Library.Modules.Folders.ApplyBibIdRestrictionModule',1,2,NULL,NULL,NULL),
(38,10,N'Validate each folder and classify (delete v. new/update)',NULL,N'SobekCM.Builder_Library.Modules.Folders.ValidateAndClassifyModule',1,3,NULL,NULL,NULL),
(39,3,N'Attach ALL the images in the resource folder to the item',NULL,N'SobekCM.Builder_Library.Modules.Items.AttachImagesAllModule',1,15,NULL,NULL,NULL),
(40,11,N'Usage statistics calculation and usage email sends',NULL,N'SobekCM.Builder_Library.Modules.Schedulable.CalculateUsageStatisticsModule',1,1,NULL,NULL,NULL),
(41,3,N'Save to the new version 5 beta solr/lucene indexes.',NULL,N'SobekCM.Builder_Library.Modules.Items.SaveToSolrLuceneModule_v5',1,24,NULL,NULL,NULL);

DECLARE @Msg nvarchar(max);
DECLARE cur_SobekCM_Builder_Module CURSOR LOCAL FAST_FORWARD FOR
	SELECT CONCAT('ModuleID=', e.[ModuleID], ', ', 'ModuleSetID=', e.[ModuleSetID], ', ', 'ModuleDesc=', e.[ModuleDesc], ', ', 'Assembly=', e.[Assembly], ', ', 'Class=', e.[Class], ', ', 'Enabled=', e.[Enabled], ', ', 'Order=', e.[Order], ', ', 'Argument1=', e.[Argument1], ', ', 'Argument2=', e.[Argument2], ', ', 'Argument3=', e.[Argument3])
	FROM #Expected_SobekCM_Builder_Module e
	WHERE NOT EXISTS (
		SELECT 1 FROM dbo.SobekCM_Builder_Module r
		WHERE (r.[ModuleID] = e.[ModuleID] OR (r.[ModuleID] IS NULL AND e.[ModuleID] IS NULL))
		  AND (r.[ModuleSetID] = e.[ModuleSetID] OR (r.[ModuleSetID] IS NULL AND e.[ModuleSetID] IS NULL))
		  AND (r.[ModuleDesc] = e.[ModuleDesc] OR (r.[ModuleDesc] IS NULL AND e.[ModuleDesc] IS NULL))
		  AND (r.[Assembly] = e.[Assembly] OR (r.[Assembly] IS NULL AND e.[Assembly] IS NULL))
		  AND (r.[Class] = e.[Class] OR (r.[Class] IS NULL AND e.[Class] IS NULL))
		  AND (r.[Enabled] = e.[Enabled] OR (r.[Enabled] IS NULL AND e.[Enabled] IS NULL))
		  AND (r.[Order] = e.[Order] OR (r.[Order] IS NULL AND e.[Order] IS NULL))
		  AND (r.[Argument1] = e.[Argument1] OR (r.[Argument1] IS NULL AND e.[Argument1] IS NULL))
		  AND (r.[Argument2] = e.[Argument2] OR (r.[Argument2] IS NULL AND e.[Argument2] IS NULL))
		  AND (r.[Argument3] = e.[Argument3] OR (r.[Argument3] IS NULL AND e.[Argument3] IS NULL))
	);

OPEN cur_SobekCM_Builder_Module;
FETCH NEXT FROM cur_SobekCM_Builder_Module INTO @Msg;
WHILE @@FETCH_STATUS = 0
BEGIN
	PRINT 'MISSING ROW: SobekCM_Builder_Module (' + @Msg + ')';
	FETCH NEXT FROM cur_SobekCM_Builder_Module INTO @Msg;
END;
CLOSE cur_SobekCM_Builder_Module;
DEALLOCATE cur_SobekCM_Builder_Module;

DROP TABLE #Expected_SobekCM_Builder_Module;
GO

-------------------------------------------------------------------------------
-- SobekCM_Builder_Module_Schedule
-------------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#Expected_SobekCM_Builder_Module_Schedule') IS NOT NULL DROP TABLE #Expected_SobekCM_Builder_Module_Schedule;

CREATE TABLE #Expected_SobekCM_Builder_Module_Schedule (
	[ModuleScheduleID] int NULL,
	[ModuleSetID] int NULL,
	[DaysOfWeek] varchar(7) NULL,
	[Enabled] bit NULL,
	[TimesOfDay] varchar(100) NULL,
	[Description] varchar(250) NULL
);

INSERT INTO #Expected_SobekCM_Builder_Module_Schedule ([ModuleScheduleID], [ModuleSetID], [DaysOfWeek], [Enabled], [TimesOfDay], [Description]) VALUES
(1,11,N'M',1,N'0600',N'Calculate the usage statistics'),
(2,5,N'MWF',1,N'0530',N'Expire old builder logs'),
(3,6,N'MTWRF',1,N'0900',N'Rebuild all aggregation browse files'),
(4,7,N'MTWRF',1,N'2100',N'Send new item emails'),
(5,8,N'S',1,N'2200',N'Solr/Lucene index optimization'),
(6,9,N'MWF',1,N'2130',N'Update all cached aggregation browses');

DECLARE @Msg nvarchar(max);
DECLARE cur_SobekCM_Builder_Module_Schedule CURSOR LOCAL FAST_FORWARD FOR
	SELECT CONCAT('ModuleScheduleID=', e.[ModuleScheduleID], ', ', 'ModuleSetID=', e.[ModuleSetID], ', ', 'DaysOfWeek=', e.[DaysOfWeek], ', ', 'Enabled=', e.[Enabled], ', ', 'TimesOfDay=', e.[TimesOfDay], ', ', 'Description=', e.[Description])
	FROM #Expected_SobekCM_Builder_Module_Schedule e
	WHERE NOT EXISTS (
		SELECT 1 FROM dbo.SobekCM_Builder_Module_Schedule r
		WHERE (r.[ModuleScheduleID] = e.[ModuleScheduleID] OR (r.[ModuleScheduleID] IS NULL AND e.[ModuleScheduleID] IS NULL))
		  AND (r.[ModuleSetID] = e.[ModuleSetID] OR (r.[ModuleSetID] IS NULL AND e.[ModuleSetID] IS NULL))
		  AND (r.[DaysOfWeek] = e.[DaysOfWeek] OR (r.[DaysOfWeek] IS NULL AND e.[DaysOfWeek] IS NULL))
		  AND (r.[Enabled] = e.[Enabled] OR (r.[Enabled] IS NULL AND e.[Enabled] IS NULL))
		  AND (r.[TimesOfDay] = e.[TimesOfDay] OR (r.[TimesOfDay] IS NULL AND e.[TimesOfDay] IS NULL))
		  AND (r.[Description] = e.[Description] OR (r.[Description] IS NULL AND e.[Description] IS NULL))
	);

OPEN cur_SobekCM_Builder_Module_Schedule;
FETCH NEXT FROM cur_SobekCM_Builder_Module_Schedule INTO @Msg;
WHILE @@FETCH_STATUS = 0
BEGIN
	PRINT 'MISSING ROW: SobekCM_Builder_Module_Schedule (' + @Msg + ')';
	FETCH NEXT FROM cur_SobekCM_Builder_Module_Schedule INTO @Msg;
END;
CLOSE cur_SobekCM_Builder_Module_Schedule;
DEALLOCATE cur_SobekCM_Builder_Module_Schedule;

DROP TABLE #Expected_SobekCM_Builder_Module_Schedule;
GO

-------------------------------------------------------------------------------
-- SobekCM_Builder_Module_Set
-------------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#Expected_SobekCM_Builder_Module_Set') IS NOT NULL DROP TABLE #Expected_SobekCM_Builder_Module_Set;

CREATE TABLE #Expected_SobekCM_Builder_Module_Set (
	[ModuleSetID] int NULL,
	[ModuleTypeID] int NULL,
	[SetName] varchar(50) NULL,
	[SetOrder] int NULL,
	[Enabled] bit NULL
);

INSERT INTO #Expected_SobekCM_Builder_Module_Set ([ModuleSetID], [ModuleTypeID], [SetName], [SetOrder], [Enabled]) VALUES
(1,1,N'Standard PRE-process modules',1,1),
(2,2,N'Standard POST-process modules',1,1),
(3,3,N'Incoming item processing',1,1),
(4,4,N'Incoming delete processing',1,1),
(5,5,N'Expire old builder logs',1,0),
(6,5,N'Rebuild all aggregation browse files',1,0),
(7,5,N'Send new item emails',1,0),
(8,5,N'Solr/Lucene index optimization',1,0),
(9,5,N'Update cached aggregation browses',1,1),
(10,6,N'Standard folder processing',1,1),
(11,5,N'Usage statistics calculation',1,1);

DECLARE @Msg nvarchar(max);
DECLARE cur_SobekCM_Builder_Module_Set CURSOR LOCAL FAST_FORWARD FOR
	SELECT CONCAT('ModuleSetID=', e.[ModuleSetID], ', ', 'ModuleTypeID=', e.[ModuleTypeID], ', ', 'SetName=', e.[SetName], ', ', 'SetOrder=', e.[SetOrder], ', ', 'Enabled=', e.[Enabled])
	FROM #Expected_SobekCM_Builder_Module_Set e
	WHERE NOT EXISTS (
		SELECT 1 FROM dbo.SobekCM_Builder_Module_Set r
		WHERE (r.[ModuleSetID] = e.[ModuleSetID] OR (r.[ModuleSetID] IS NULL AND e.[ModuleSetID] IS NULL))
		  AND (r.[ModuleTypeID] = e.[ModuleTypeID] OR (r.[ModuleTypeID] IS NULL AND e.[ModuleTypeID] IS NULL))
		  AND (r.[SetName] = e.[SetName] OR (r.[SetName] IS NULL AND e.[SetName] IS NULL))
		  AND (r.[SetOrder] = e.[SetOrder] OR (r.[SetOrder] IS NULL AND e.[SetOrder] IS NULL))
		  AND (r.[Enabled] = e.[Enabled] OR (r.[Enabled] IS NULL AND e.[Enabled] IS NULL))
	);

OPEN cur_SobekCM_Builder_Module_Set;
FETCH NEXT FROM cur_SobekCM_Builder_Module_Set INTO @Msg;
WHILE @@FETCH_STATUS = 0
BEGIN
	PRINT 'MISSING ROW: SobekCM_Builder_Module_Set (' + @Msg + ')';
	FETCH NEXT FROM cur_SobekCM_Builder_Module_Set INTO @Msg;
END;
CLOSE cur_SobekCM_Builder_Module_Set;
DEALLOCATE cur_SobekCM_Builder_Module_Set;

DROP TABLE #Expected_SobekCM_Builder_Module_Set;
GO

-------------------------------------------------------------------------------
-- SobekCM_Builder_Module_Type
-------------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#Expected_SobekCM_Builder_Module_Type') IS NOT NULL DROP TABLE #Expected_SobekCM_Builder_Module_Type;

CREATE TABLE #Expected_SobekCM_Builder_Module_Type (
	[ModuleTypeID] int NULL,
	[TypeAbbrev] varchar(4) NULL,
	[TypeDescription] varchar(200) NULL
);

INSERT INTO #Expected_SobekCM_Builder_Module_Type ([ModuleTypeID], [TypeAbbrev], [TypeDescription]) VALUES
(1,N'PRE',N'Pre-Process modules run each time BEFORE processing any pending items/requests'),
(2,N'POST',N'Post-Process modules run each time AFTER processing any pending items/requests'),
(3,N'NEW',N'Submission modules run for each incoming item (or items set to reprocess)'),
(4,N'DELT',N'Submission modules run for each incoming DELETE request'),
(5,N'SCHD',N'Schedulable modules run as a scheduled task by the builder'),
(6,N'FOLD',N'Folder-level modules are run to prepare and find items in incoming folders');

DECLARE @Msg nvarchar(max);
DECLARE cur_SobekCM_Builder_Module_Type CURSOR LOCAL FAST_FORWARD FOR
	SELECT CONCAT('ModuleTypeID=', e.[ModuleTypeID], ', ', 'TypeAbbrev=', e.[TypeAbbrev], ', ', 'TypeDescription=', e.[TypeDescription])
	FROM #Expected_SobekCM_Builder_Module_Type e
	WHERE NOT EXISTS (
		SELECT 1 FROM dbo.SobekCM_Builder_Module_Type r
		WHERE (r.[ModuleTypeID] = e.[ModuleTypeID] OR (r.[ModuleTypeID] IS NULL AND e.[ModuleTypeID] IS NULL))
		  AND (r.[TypeAbbrev] = e.[TypeAbbrev] OR (r.[TypeAbbrev] IS NULL AND e.[TypeAbbrev] IS NULL))
		  AND (r.[TypeDescription] = e.[TypeDescription] OR (r.[TypeDescription] IS NULL AND e.[TypeDescription] IS NULL))
	);

OPEN cur_SobekCM_Builder_Module_Type;
FETCH NEXT FROM cur_SobekCM_Builder_Module_Type INTO @Msg;
WHILE @@FETCH_STATUS = 0
BEGIN
	PRINT 'MISSING ROW: SobekCM_Builder_Module_Type (' + @Msg + ')';
	FETCH NEXT FROM cur_SobekCM_Builder_Module_Type INTO @Msg;
END;
CLOSE cur_SobekCM_Builder_Module_Type;
DEALLOCATE cur_SobekCM_Builder_Module_Type;

DROP TABLE #Expected_SobekCM_Builder_Module_Type;
GO

-------------------------------------------------------------------------------
-- SobekCM_Item_Aggregation_Result_Types
-------------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#Expected_SobekCM_Item_Aggregation_Result_Types') IS NOT NULL DROP TABLE #Expected_SobekCM_Item_Aggregation_Result_Types;

CREATE TABLE #Expected_SobekCM_Item_Aggregation_Result_Types (
	[ItemAggregationResultTypeID] int NULL,
	[ResultType] varchar(50) NULL,
	[DefaultOrder] int NULL,
	[DefaultView] bit NULL
);

INSERT INTO #Expected_SobekCM_Item_Aggregation_Result_Types ([ItemAggregationResultTypeID], [ResultType], [DefaultOrder], [DefaultView]) VALUES
(1,N'BRIEF',1,1),
(2,N'THUMBNAIL',2,1),
(3,N'TABLE',3,1),
(4,N'EXPORT',4,0),
(5,N'GMAP',5,1);

DECLARE @Msg nvarchar(max);
DECLARE cur_SobekCM_Item_Aggregation_Result_Types CURSOR LOCAL FAST_FORWARD FOR
	SELECT CONCAT('ItemAggregationResultTypeID=', e.[ItemAggregationResultTypeID], ', ', 'ResultType=', e.[ResultType], ', ', 'DefaultOrder=', e.[DefaultOrder], ', ', 'DefaultView=', e.[DefaultView])
	FROM #Expected_SobekCM_Item_Aggregation_Result_Types e
	WHERE NOT EXISTS (
		SELECT 1 FROM dbo.SobekCM_Item_Aggregation_Result_Types r
		WHERE (r.[ItemAggregationResultTypeID] = e.[ItemAggregationResultTypeID] OR (r.[ItemAggregationResultTypeID] IS NULL AND e.[ItemAggregationResultTypeID] IS NULL))
		  AND (r.[ResultType] = e.[ResultType] OR (r.[ResultType] IS NULL AND e.[ResultType] IS NULL))
		  AND (r.[DefaultOrder] = e.[DefaultOrder] OR (r.[DefaultOrder] IS NULL AND e.[DefaultOrder] IS NULL))
		  AND (r.[DefaultView] = e.[DefaultView] OR (r.[DefaultView] IS NULL AND e.[DefaultView] IS NULL))
	);

OPEN cur_SobekCM_Item_Aggregation_Result_Types;
FETCH NEXT FROM cur_SobekCM_Item_Aggregation_Result_Types INTO @Msg;
WHILE @@FETCH_STATUS = 0
BEGIN
	PRINT 'MISSING ROW: SobekCM_Item_Aggregation_Result_Types (' + @Msg + ')';
	FETCH NEXT FROM cur_SobekCM_Item_Aggregation_Result_Types INTO @Msg;
END;
CLOSE cur_SobekCM_Item_Aggregation_Result_Types;
DEALLOCATE cur_SobekCM_Item_Aggregation_Result_Types;

DROP TABLE #Expected_SobekCM_Item_Aggregation_Result_Types;
GO

-------------------------------------------------------------------------------
-- SobekCM_Item_Viewer_Types  (ignoring ItemViewTypeID)
-------------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#Expected_SobekCM_Item_Viewer_Types') IS NOT NULL DROP TABLE #Expected_SobekCM_Item_Viewer_Types;

CREATE TABLE #Expected_SobekCM_Item_Viewer_Types (
	[ItemViewTypeID] int NULL,
	[ViewType] varchar(50) NULL,
	[Order] int NULL,
	[DefaultView] bit NULL,
	[MenuOrder] float NULL
);

INSERT INTO #Expected_SobekCM_Item_Viewer_Types ([ItemViewTypeID], [ViewType], [Order], [DefaultView], [MenuOrder]) VALUES
(1,N'JPEG',10,1,500.1),
(2,N'JPEG2000',12,1,500.2),
(3,N'TEXT',15,0,500.3),
(4,N'PAGE_TURNER',20,0,120),
(5,N'GOOGLE_MAP',18,1,118),
(6,N'HTML',14,0,116),
(7,N'HTML Map Viewer',100,0,200),
(8,N'RELATED_IMAGES',21,1,400),
(9,N'TOC',26,0,126),
(10,N'TEI',25,0,125),
(11,N'DATASET_CODEBOOK',1,0,101),
(12,N'DATASET_REPORTS',2,0,102),
(13,N'DATASET_VIEWDATA',3,0,103),
(14,N'JPEG_TEXT_TWO_UP',11,0,111),
(15,N'EAD_CONTAINER_LIST',4,0,104),
(16,N'EAD_DESCRIPTION',5,0,105),
(17,N'YOUTUBE_VIDEO',6,0,106),
(18,N'EMBEDDED_VIDEO',7,0,107),
(19,N'FLASH',9,0,109),
(20,N'PDF',13,1,113),
(21,N'DOWNLOADS',16,1,114),
(22,N'CITATION',17,1,10.1),
(23,N'FEATURES',19,0,119),
(24,N'SEARCH',22,0,30),
(25,N'SIMPLE_HTML_LINK',23,0,123),
(26,N'STREETS',24,0,124),
(27,N'ALL_VOLUMES',27,1,20),
(28,N'VIDEO',7,1,107),
(29,N'MARC',100,1,10.2),
(30,N'METADATA',100,1,10.3),
(31,N'USAGE',100,1,10.4),
(32,N'WEBSITE',14,0,116),
(33,N'OPEN_TEXTBOOK',15,0,117);

DECLARE @Msg nvarchar(max);
DECLARE cur_SobekCM_Item_Viewer_Types CURSOR LOCAL FAST_FORWARD FOR
	SELECT CONCAT('ItemViewTypeID=', e.[ItemViewTypeID], ', ', 'ViewType=', e.[ViewType], ', ', 'Order=', e.[Order], ', ', 'DefaultView=', e.[DefaultView], ', ', 'MenuOrder=', e.[MenuOrder])
	FROM #Expected_SobekCM_Item_Viewer_Types e
	WHERE NOT EXISTS (
		SELECT 1 FROM dbo.SobekCM_Item_Viewer_Types r
		WHERE (r.[ViewType] = e.[ViewType] OR (r.[ViewType] IS NULL AND e.[ViewType] IS NULL))
		  AND (r.[Order] = e.[Order] OR (r.[Order] IS NULL AND e.[Order] IS NULL))
		  AND (r.[DefaultView] = e.[DefaultView] OR (r.[DefaultView] IS NULL AND e.[DefaultView] IS NULL))
		  AND (r.[MenuOrder] = e.[MenuOrder] OR (r.[MenuOrder] IS NULL AND e.[MenuOrder] IS NULL))
	);

OPEN cur_SobekCM_Item_Viewer_Types;
FETCH NEXT FROM cur_SobekCM_Item_Viewer_Types INTO @Msg;
WHILE @@FETCH_STATUS = 0
BEGIN
	PRINT 'MISSING ROW: SobekCM_Item_Viewer_Types (' + @Msg + ')';
	FETCH NEXT FROM cur_SobekCM_Item_Viewer_Types INTO @Msg;
END;
CLOSE cur_SobekCM_Item_Viewer_Types;
DEALLOCATE cur_SobekCM_Item_Viewer_Types;

DROP TABLE #Expected_SobekCM_Item_Viewer_Types;
GO

-------------------------------------------------------------------------------
-- SobekCM_Metadata_Types  (ignoring MetadataTypeID)
-------------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#Expected_SobekCM_Metadata_Types') IS NOT NULL DROP TABLE #Expected_SobekCM_Metadata_Types;

CREATE TABLE #Expected_SobekCM_Metadata_Types (
	[MetadataTypeID] smallint NULL,
	[MetadataName] varchar(100) NULL,
	[SobekCode] char(2) NULL,
	[SolrCode] varchar(100) NULL,
	[DisplayTerm] nvarchar(100) NULL,
	[FacetTerm] varchar(100) NULL,
	[CustomField] bit NULL,
	[canFacetBrowse] bit NULL,
	[DefaultAdvancedSearch] bit NULL,
	[LegacySolrCode] varchar(100) NULL,
	[SolrCode_Facets] varchar(100) NULL,
	[SolrCode_Display] varchar(100) NULL
);

INSERT INTO #Expected_SobekCM_Metadata_Types ([MetadataTypeID], [MetadataName], [SobekCode], [SolrCode], [DisplayTerm], [FacetTerm], [CustomField], [canFacetBrowse], [DefaultAdvancedSearch], [LegacySolrCode], [SolrCode_Facets], [SolrCode_Display]) VALUES
(1,N'Title',N'TI',N'title',N'Title',N'Title',0,1,1,N'title',NULL,N'title'),
(2,N'Type',N'TY',N'type',N'Resource Type',N'Resource Type',0,1,1,N'type',N'type_facets',N'type'),
(3,N'Language',N'LA',N'language',N'Language',N'Language',0,1,1,N'language',N'language_facets',N'language'),
(4,N'Creator',N'AU',N'creator',N'Creator',N'Creator',0,1,1,N'creator',N'creator_facets',N'creator.display'),
(5,N'Publisher',N'PU',N'publisher',N'Publisher',N'Publisher',0,1,1,N'publisher',N'publisher_facets',N'publisher.display'),
(6,N'Publication Place',N'PP',N'publication_place',N'Publication Place',N'Publication Place',0,1,1,N'publication place',N'publication_place_facets',N'publication_place'),
(7,N'Subject Keyword',N'TO',N'subject',N'Subject Keyword',N'Subject: Topic',0,1,1,N'subject keyword',N'subject_facets',N'subject.display'),
(8,N'Genre',N'GE',N'genre',N'Material Type',N'Material Type',0,1,0,N'genre',N'genre_facets',N'genre.display'),
(9,N'Target Audience',N'TA',N'audience',N'Target Audience',N'Target Audience',0,1,0,N'target audience',N'audience_facets',N'audience'),
(10,N'Spatial Coverage',N'SP',N'spatial_standard',N'Spatial Coverage',N'Subject: Geographic Area',0,1,0,N'spatial coverage',N'spatial_standard_facets',N'spatial_standard.display'),
(11,N'Country',N'CO',N'country',N'Country',N'Country',0,1,0,N'country',N'country_facets',N'country'),
(12,N'State',N'ST',N'state',N'State',N'State',0,1,0,N'state',N'state_facets',N'state'),
(13,N'County',N'CT',N'county',N'County',N'County',0,1,0,N'county',N'county_facets',N'county'),
(14,N'City',N'CI',N'city',N'City',N'City',0,1,0,N'city',N'city_facets',N'city'),
(15,N'Source Institution',N'SO',N'source',N'Source Institution',N'Source Institution',0,1,0,N'source institution',N'source_facets',N'source'),
(16,N'Holding Location',N'HO',N'holding',N'Holding Location',N'Holding Location',0,1,0,N'holding location',N'holding_facets',N'holding'),
(17,N'Identifier',N'ID',N'identifier',N'Identifier',N'Identifier',0,0,0,N'identifier',N'identifier_facets',N'identifier.display'),
(18,N'Notes',N'NO',N'notes',N'Notes',N'Notes',0,0,0,N'notes',NULL,N'notes'),
(19,N'Other_Citation',N'  ',N'other',N'Other_Citation',N'Other_Citation',0,0,0,N'other_citation',NULL,N'other'),
(20,N'Tickler',N'TL',N'tickler',N'Tickler',N'Tickler',0,1,0,N'tickler',N'tickler_facets',N'tickler'),
(21,N'Donor',N'DO',N'donor',N'Donor',N'Donor',0,1,0,N'donor',N'donor_facets',N'donor'),
(22,N'Format',N'FO',N'format',N'Description',N'Description',0,1,0,N'format',N'format_facets',N'format'),
(23,N'BibID',N'BI',N'bibid',N'BibID',N'BibID',0,0,0,N'bibid',NULL,N'bibid'),
(24,N'Publication Date',N'DA',N'date',N'Publication Date',N'Publication Date',0,1,0,N'publication date',N'date_facets',N'date.display'),
(25,N'Affiliation',N'AF',N'affiliation',N'Affiliation',N'Affiliation',0,1,0,N'affiliation',N'affiliation_facets',N'affiliation.display'),
(26,N'Frequency',N'FR',N'frequency',N'Frequency',N'Frequency',0,1,0,N'frequency',N'frequency_facets',N'frequency'),
(27,N'Name as Subject',N'SN',N'name_as_subject',N'Name as Subject',N'Name as Subject',0,1,0,N'name as subject',N'name_as_subject_facets',N'name_as_subject.display'),
(28,N'Title as Subject',N'TS',N'title_as_subject',N'Title as Subject',N'Title as Subject',0,1,0,N'title as subject',N'title_as_subject_facets',N'title_as_subject.display'),
(29,N'All Subjects',N'SU',N'subject_all',N'All Subjects',N'All Subjects',0,1,0,N'all subjects',NULL,NULL),
(30,N'Temporal Subject',N'TE',N'temporal_subject',N'Temporal Subject',N'Temporal Subject',0,1,0,N'temporal_subject',NULL,N'temporal subject'),
(31,N'Attribution',N'AT',N'attribution',N'Attribution',N'Attribution',0,1,0,N'attribution',NULL,N'attribution'),
(32,N'User Description',N'DE',N'User_Description',N'User Description',N'User Description',0,0,0,N'User_Description',NULL,N'user description'),
(33,N'Temporal Decade',N'DD',N'temporal_decade',N'Temporal Decade',N'Temporal Decade',0,1,0,N'temporal_decade',NULL,N'temporal decade'),
(34,N'MIME Type',N'MI',N'mime_type',N'MIME Type',N'MIME Type',0,1,0,N'mime type',N'mime_type_facets',N'mime_type'),
(35,N'Full Citation',N'FC',N'fullcitation',N'Full Citation',N'Full Citation',0,0,0,N'allfields',NULL,NULL),
(36,N'Tracking Box',N'TB',N'tracking_box',N'Tracking Box',N'Tracking Box',0,1,0,N'tracking box',N'tracking_box_facets',N'tracking_box'),
(37,N'Abstract',N'AB',N'abstract',N'Abstract',N'Abstract',0,0,0,N'abstract',NULL,N'abstract'),
(38,N'Edition',N'ET',N'edition',N'Edition',N'Edition',0,1,0,N'edition',N'edition_facets',N'edition'),
(39,N'TOC',N'TC',N'toc',N'TOC',N'TOC',0,0,0,N'toc',NULL,NULL),
(40,N'ZT Kingdom',N'ZK',N'zt_kingdom',N'Taxonomic Kingdom',N'Taxonomic Kingdom',0,1,0,N'zt kingdom',N'zt_kingdom_facets',N'zt_kingdom'),
(41,N'ZT Phylum',N'ZP',N'zt_phylum',N'Taxonomic Phylum',N'Taxonomic Phylum',0,1,0,N'zt phylum',N'zt_phylum_facets',N'zt_phylum'),
(42,N'ZT Class',N'ZC',N'zt_class',N'Taxonomic Class',N'Taxonomic Class',0,1,0,N'zt class',N'zt_class_facets',N'zt_class'),
(43,N'ZT Order',N'ZO',N'zt_order',N'Taxonomic Order',N'Taxonomic Order',0,1,0,N'zt order',N'zt_order_facets',N'zt_order'),
(44,N'ZT Family',N'ZF',N'zt_family',N'Taxonomic Family',N'Taxonomic Family',0,1,0,N'zt family',N'zt_family_facets',N'zt_family'),
(45,N'ZT Genus',N'ZG',N'zt_genus',N'Taxonomic Genus',N'Taxonomic Genus',0,1,0,N'zt genus',N'zt_genus_facets',N'zt_genus'),
(46,N'ZT Species',N'ZS',N'zt_species',N'Taxonomic Species',N'Taxonomic Species',0,1,0,N'zt species',N'zt_species_facets',N'zt_species'),
(47,N'ZT Common Name',N'ZN',N'zt_common_name',N'Taxonomic Common Name',N'Taxonomic Common Name',0,1,0,N'zt common name',N'zt_common_name_facets',N'zt_common_name'),
(48,N'ZT Scientific Name',N'ZI',N'zt_scientific_name',N'Taxonomic Scientific Name',N'Taxonomic Scientific Name',0,1,0,N'zt scientific name',N'zt_scientific_name_facets',N'zt_scientific_name'),
(49,N'ZT All Taxonomy',N'ZA',N'',N'Taxonomic All Taxonomy',N'Taxonomic All Taxonomy',0,1,0,N'',NULL,N'zt all taxonomy'),
(50,N'Cultural Context',N'CC',N'cultural_context',N'Cultural Context',N'Cultural Context',0,1,0,N'cultural context',N'cultural_context_facets',N'cultural_context'),
(51,N'Inscription',N'IN',N'inscription',N'Inscription',N'Inscription',0,1,0,N'inscription',NULL,N'inscription'),
(52,N'Material',N'MA',N'material',N'Material',N'Material',0,1,0,N'material',N'material_facets',N'material.display'),
(53,N'Style Period',N'SY',N'style_period',N'Style Period',N'Style Period',0,1,0,N'style period',N'style_period_facets',N'style_period'),
(54,N'Technique',N'TQ',N'technique',N'Technique',N'Technique',0,1,0,N'technique',N'technique_facets',N'technique'),
(55,N'Accession Number',N'AN',N'accession_number',N'Accession Number',N'Accession Number',0,1,0,N'accession',N'accession_number_facets',N'accession_number.display'),
(56,N'ETD Committee',N'EC',N'etd_committee',N'ETD Committee',N'ETD Committee',0,1,0,N'etd committee',N'etd_committee_facets',N'etd_committee'),
(57,N'ETD Degree',N'ED',N'etd_degree',N'ETD Degree',N'ETD Degree',0,1,0,N'etd degree',N'etd_degree_facets',N'etd_degree'),
(58,N'ETD Degree Discipline',N'EI',N'etd_degree_discipline',N'ETD Degree Discipline',N'ETD Degree Discipline',0,1,0,N'etd degree discipline',N'etd_degree_discipline_facets',N'etd_degree_discipline'),
(59,N'ETD Degree Grantor',N'EG',N'etd_degree_grantor',N'ETD Degree Grantor',N'ETD Degree Grantor',0,1,0,N'etd degree grantor',N'etd_degree_grantor_facets',N'etd_degree_grantor'),
(60,N'ETD Degree Level',N'EL',N'etd_degree_level',N'ETD Degree Level',N'ETD Degree Level',0,1,0,N'etd degree level',N'etd_degree_level_facets',N'etd_degree_level'),
(61,N'Temporal Year',N'DY',N'temporal_year',N'Temporal Year',N'Temporal Year',0,1,0,N'temporal_year',NULL,N'temporal year'),
(62,N'Interviewee',N'OI',N'interviewee',N'Intervewiee',N'Intervewiee',0,1,0,N'interviewee',N'interviewee_facets',N'interviewee'),
(63,N'Interviewer',N'OV',N'interviewer',N'Intervewer',N'Intervewer',0,1,0,N'interviewer',N'interviewer_facets',N'interviewer'),
(64,N'UserDefined01',N'UA',N'user_defined_01',N'Temporal Subject Display',N'Temporal Subject Display',1,1,0,N'userdefined01',N'user_defined_01_facets',N'user_defined_01.display'),
(65,N'UserDefined02',N'UB',N'user_defined_02',N'LOM Resource Type Display',N'LOM Resource Type Display',1,1,0,N'userdefined02',N'user_defined_02_facets',N'user_defined_02.display'),
(66,N'UserDefined03',N'UC',N'user_defined_03',N'LOM Intended End User Display',N'LOM Intended End User Display',1,1,0,N'userdefined03',N'user_defined_03_facets',N'user_defined_03.display'),
(67,N'UserDefined04',N'UD',N'user_defined_04',N'Course Title',N'Course Title',1,1,0,N'userdefined04',N'user_defined_04_facets',N'user_defined_04.display'),
(68,N'UserDefined05',N'UE',N'user_defined_05',N'Licensing',N'Licensing',1,1,0,N'userdefined05',N'user_defined_05_facets',N'user_defined_05.display'),
(69,N'UserDefined06',N'UF',N'user_defined_06',N'Undefined',N'Undefined',1,1,0,N'userdefined06',N'user_defined_06_facets',N'user_defined_06.display'),
(70,N'UserDefined07',N'UG',N'user_defined_07',N'Undefined',N'Undefined',1,1,0,N'userdefined07',N'user_defined_07_facets',N'user_defined_07.display'),
(71,N'UserDefined08',N'UH',N'user_defined_08',N'Undefined',N'Undefined',1,1,0,N'userdefined08',N'user_defined_08_facets',N'user_defined_08.display'),
(72,N'UserDefined09',N'UI',N'user_defined_09',N'Undefined',N'Undefined',1,1,0,N'userdefined09',N'user_defined_09_facets',N'user_defined_09.display'),
(73,N'UserDefined10',N'UJ',N'user_defined_10',N'Undefined',N'Undefined',1,1,0,N'userdefined10',N'user_defined_10_facets',N'user_defined_10.display'),
(74,N'UserDefined11',N'UK',N'user_defined_11',N'Undefined',N'Undefined',1,1,0,N'userdefined11',N'user_defined_11_facets',N'user_defined_11.display'),
(75,N'UserDefined12',N'UL',N'user_defined_12',N'Undefined',N'Undefined',1,1,0,N'userdefined12',N'user_defined_12_facets',N'user_defined_12.display'),
(76,N'UserDefined13',N'UM',N'user_defined_13',N'Undefined',N'Undefined',1,1,0,N'userdefined13',N'user_defined_13_facets',N'user_defined_13.display'),
(77,N'UserDefined14',N'UN',N'user_defined_14',N'Undefined',N'Undefined',1,1,0,N'userdefined14',N'user_defined_14_facets',N'user_defined_14.display'),
(78,N'UserDefined15',N'UO',N'user_defined_15',N'Undefined',N'Undefined',1,1,0,N'userdefined15',N'user_defined_15_facets',N'user_defined_15.display'),
(79,N'UserDefined16',N'UP',N'user_defined_16',N'Undefined',N'Undefined',1,1,0,N'userdefined16',N'user_defined_16_facets',N'user_defined_16.display'),
(80,N'UserDefined17',N'UQ',N'user_defined_17',N'Undefined',N'Undefined',1,1,0,N'userdefined17',N'user_defined_17_facets',N'user_defined_17.display'),
(81,N'UserDefined18',N'UR',N'user_defined_18',N'Undefined',N'Undefined',1,1,0,N'userdefined18',N'user_defined_18_facets',N'user_defined_18.display'),
(82,N'UserDefined19',N'US',N'user_defined_19',N'Undefined',N'Undefined',1,1,0,N'userdefined19',N'user_defined_19_facets',N'user_defined_19.display'),
(83,N'UserDefined20',N'UT',N'user_defined_20',N'Undefined',N'Undefined',1,1,0,N'userdefined20',N'user_defined_20_facets',N'user_defined_20.display'),
(84,N'UserDefined21',N'UU',N'user_defined_21',N'Undefined',N'Undefined',1,1,0,N'userdefined21',N'user_defined_21_facets',N'user_defined_21.display'),
(85,N'UserDefined22',N'UV',N'user_defined_22',N'Undefined',N'Undefined',1,1,0,N'userdefined22',N'user_defined_22_facets',N'user_defined_22.display'),
(86,N'UserDefined23',N'UW',N'user_defined_23',N'Undefined',N'Undefined',1,1,0,N'userdefined23',N'user_defined_23_facets',N'user_defined_23.display'),
(87,N'UserDefined24',N'UX',N'user_defined_24',N'Undefined',N'Undefined',1,1,0,N'userdefined24',N'user_defined_24_facets',N'user_defined_24.display'),
(88,N'UserDefined25',N'UY',N'user_defined_25',N'Undefined',N'Undefined',1,1,0,N'userdefined25',N'user_defined_25_facets',N'user_defined_25.display'),
(89,N'UserDefined26',N'UZ',N'user_defined_26',N'Undefined',N'Undefined',1,1,0,N'userdefined26',N'user_defined_26_facets',N'user_defined_26.display'),
(90,N'UserDefined27',N'VA',N'user_defined_27',N'Undefined',N'Undefined',1,1,0,N'userdefined27',N'user_defined_27_facets',N'user_defined_27.display'),
(91,N'UserDefined28',N'VB',N'user_defined_28',N'Undefined',N'Undefined',1,1,0,N'userdefined28',N'user_defined_28_facets',N'user_defined_28.display'),
(92,N'UserDefined29',N'VC',N'user_defined_29',N'Undefined',N'Undefined',1,1,0,N'userdefined29',N'user_defined_29_facets',N'user_defined_29.display'),
(93,N'UserDefined30',N'VD',N'user_defined_30',N'Undefined',N'Undefined',1,1,0,N'userdefined30',N'user_defined_30_facets',N'user_defined_30.display'),
(94,N'UserDefined31',N'VE',N'user_defined_31',N'Undefined',N'Undefined',1,1,0,N'userdefined31',N'user_defined_31_facets',N'user_defined_31.display'),
(95,N'UserDefined32',N'VF',N'user_defined_32',N'Undefined',N'Undefined',1,1,0,N'userdefined32',N'user_defined_32_facets',N'user_defined_32.display'),
(96,N'UserDefined33',N'VG',N'user_defined_33',N'Undefined',N'Undefined',1,1,0,N'userdefined33',N'user_defined_33_facets',N'user_defined_33.display'),
(97,N'UserDefined34',N'VH',N'user_defined_34',N'Undefined',N'Undefined',1,1,0,N'userdefined34',N'user_defined_34_facets',N'user_defined_34.display'),
(98,N'UserDefined35',N'VI',N'user_defined_35',N'Undefined',N'Undefined',1,1,0,N'userdefined35',N'user_defined_35_facets',N'user_defined_35.display'),
(99,N'UserDefined36',N'VJ',N'user_defined_36',N'Undefined',N'Undefined',1,1,0,N'userdefined36',N'user_defined_36_facets',N'user_defined_36.display'),
(100,N'UserDefined37',N'VK',N'user_defined_37',N'Undefined',N'Undefined',1,1,0,N'userdefined37',N'user_defined_37_facets',N'user_defined_37.display'),
(101,N'UserDefined38',N'VL',N'user_defined_38',N'Undefined',N'Undefined',1,1,0,N'userdefined38',N'user_defined_38_facets',N'user_defined_38.display'),
(102,N'UserDefined39',N'VM',N'user_defined_39',N'Undefined',N'Undefined',1,1,0,N'userdefined39',N'user_defined_39_facets',N'user_defined_39.display'),
(103,N'UserDefined40',N'VN',N'user_defined_40',N'Undefined',N'Undefined',1,1,0,N'userdefined40',N'user_defined_40_facets',N'user_defined_40.display'),
(104,N'UserDefined41',N'VO',N'user_defined_41',N'Undefined',N'Undefined',1,1,0,N'userdefined41',N'user_defined_41_facets',N'user_defined_41.display'),
(105,N'UserDefined42',N'VP',N'user_defined_42',N'Undefined',N'Undefined',1,1,0,N'userdefined42',N'user_defined_42_facets',N'user_defined_42.display'),
(106,N'UserDefined43',N'VQ',N'user_defined_43',N'Undefined',N'Undefined',1,1,0,N'userdefined43',N'user_defined_43_facets',N'user_defined_43.display'),
(107,N'UserDefined44',N'VR',N'user_defined_44',N'Undefined',N'Undefined',1,1,0,N'userdefined44',N'user_defined_44_facets',N'user_defined_44.display'),
(108,N'UserDefined45',N'VS',N'user_defined_45',N'Undefined',N'Undefined',1,1,0,N'userdefined45',N'user_defined_45_facets',N'user_defined_45.display'),
(109,N'UserDefined46',N'VT',N'user_defined_46',N'Undefined',N'Undefined',1,1,0,N'userdefined46',N'user_defined_46_facets',N'user_defined_46.display'),
(110,N'UserDefined47',N'VU',N'user_defined_47',N'Undefined',N'Undefined',1,1,0,N'userdefined47',N'user_defined_47_facets',N'user_defined_47.display'),
(111,N'UserDefined48',N'VV',N'user_defined_48',N'Undefined',N'Undefined',1,1,0,N'userdefined48',N'user_defined_48_facets',N'user_defined_48.display'),
(112,N'UserDefined49',N'VW',N'user_defined_49',N'Undefined',N'Undefined',1,1,0,N'userdefined49',N'user_defined_49_facets',N'user_defined_49.display'),
(113,N'UserDefined50',N'VX',N'user_defined_50',N'Undefined',N'Undefined',1,1,0,N'userdefined50',N'user_defined_50_facets',N'user_defined_50.display'),
(114,N'UserDefined51',N'VY',N'user_defined_51',N'Undefined',N'Undefined',1,1,0,N'userdefined51',N'user_defined_51_facets',N'user_defined_51.display'),
(115,N'UserDefined52',N'VZ',N'user_defined_52',N'Undefined',N'Undefined',1,1,0,N'userdefined52',N'user_defined_52_facets',N'user_defined_52.display'),
(116,N'Publisher.Display',N'  ',N'',N'Publisher',N'Publisher',0,0,0,N'',NULL,N''),
(117,N'Spatial Coverage.Display',N'  ',N'',N'Spatial Coverage',N'Subject: Geographic Area',0,0,0,N'',NULL,N''),
(118,N'Measurements',N'  ',N'measurements',N'Measurements',N'Measurements',0,0,0,N'',N'measurements_facets',N'measurements.display'),
(119,N'Subjects.Display',N'  ',N'',N'Subjects',N'Subjects',0,0,0,N'',NULL,N''),
(120,N'Aggregations',N'  ',N'aggregations',N'Aggregations',N'Aggregations',0,1,0,N'',NULL,N'aggregations'),
(121,N'LOM Aggregation',N'LB',N'lom_aggregation',N'Aggregation (LOM)',N'Aggregation (LOM)',0,1,0,N'',N'lom_aggregation_facets',N'lom_aggregation'),
(122,N'LOM Context',N'LC',N'lom_context',N'Context',N'Context',0,1,0,N'',N'lom_context_facets',N'lom_context.display'),
(123,N'LOM Classification',N'LL',N'lom_classification',N'Classification',N'Classification',0,1,0,N'',N'lom_classification_facets',N'lom_classification.display'),
(124,N'LOM Difficulty',N'LD',N'lom_difficulty',N'Difficulty',N'Difficulty',0,1,0,N'',N'lom_difficulty_facets',N'lom_difficulty'),
(125,N'LOM Intended End User',N'LU',N'lom_intended_end_user',N'Intended End User',N'Intended End User',0,1,0,N'',N'lom_intended_end_user_facets',N'lom_intended_end_user.display'),
(126,N'LOM Interactivity Level',N'LI',N'lom_interactivity_level',N'Interactivity Level',N'Interactivity Level',0,1,0,N'',N'lom_interactivity_level_facets',N'lom_interactivity_level.display'),
(127,N'LOM Interactivity Type',N'LJ',N'lom_interactivity_type',N'Interactivity Type',N'Interactivity Type',0,1,0,N'',N'lom_interactivity_type_facets',N'lom_interactivity_type.display'),
(128,N'LOM Status',N'LS',N'lom_status',N'Status',N'Status',0,1,0,N'',N'lom_status_facets',N'lom_status'),
(129,N'LOM Requirement',N'LR',N'lom_requirement',N'Requirements',N'Requirements',0,1,0,N'',N'lom_requirement_facets',N'lom_requirement.display'),
(130,N'LOM Age Range',N'LG',N'lom_age_range',N'Typical Age Range',N'Typical Age Range',0,1,0,N'',N'lom_age_range_facets',N'lom_age_range'),
(131,N'ETD Degree Division',N'EJ',N'etd_degree_division',N'ETD Degree Division',N'ETD Degree Division',0,1,0,N'etd degree division',N'etd_degree_division_facets',N'etd_degree_division'),
(132,N'Performance',N'PE',N'performance',N'Performance',N'Peformance',0,1,0,N'',N'peformance_facets',N'performance.display'),
(133,N'Performance Date',N'PD',N'performance_date',N'Performance Date',N'Peformance Date',0,1,0,N'',N'peformance_date_facets',N'performance_date'),
(134,N'Performer',N'PR',N'performer',N'Performer',N'Peformer',0,1,0,N'',N'peformer_facets',N'performer.display'),
(135,N'LOM Resource Type',N'LE',N'lom_resource_type',N'Learning Object Type',N'Learning Object Type',0,1,0,N'',N'lom_resource_type_facets',N'lom_resource_type.display'),
(136,N'LOM Learning Time',N'LT',N'lom_learning_time',N'Learning Time',N'Learning Time',0,1,0,N'',N'lom_learning_time_facets',N'lom_learning_time'),
(137,N'Timeline Date',N'  ',N'timeline_date',N'Timeline Date',N'Timeline Date',0,1,0,N'',N'timeline_date.display',N'timeline_date'),
(138,N'Series Title',N'SE',N'seriestitle',N'Series Title',N'Series Title',0,1,0,N'',N'seriestitle_facets',N'seriestitle'),
(139,N'Accessibility',N'AC',N'accessibility',N'Accessibility',N'Accessibility',0,1,0,N'',N'accessibility_facets',N'accessibility'),
(140,N'Licensing',N'LN',N'licensing',N'Licensing',N'Licensing',0,1,0,N'',N'licensing_facets',N'licensing'),
(141,N'Course Title',N'CU',N'coursetitle',N'Course Title',N'Course Title',0,1,0,N'',N'coursetitle_facets',N'coursetitle'),
(142,N'Restriction Message',N'  ',N'restricted_msg',N'Access Restriction',N'',0,0,0,NULL,NULL,N'restricted_msg'),
(143,N'Group Restrictions',N'  ',N'group_restrictions',N'Group Restrictions',N'',0,0,0,NULL,NULL,N'group_restrictions'),
(144,N'Instances',N'  ',N'instance',N'Instances',N'',0,0,0,NULL,NULL,N'instance');

DECLARE @Msg nvarchar(max);
DECLARE cur_SobekCM_Metadata_Types CURSOR LOCAL FAST_FORWARD FOR
	SELECT CONCAT('MetadataTypeID=', e.[MetadataTypeID], ', ', 'MetadataName=', e.[MetadataName], ', ', 'SobekCode=', e.[SobekCode], ', ', 'SolrCode=', e.[SolrCode], ', ', 'DisplayTerm=', e.[DisplayTerm], ', ', 'FacetTerm=', e.[FacetTerm], ', ', 'CustomField=', e.[CustomField], ', ', 'canFacetBrowse=', e.[canFacetBrowse], ', ', 'DefaultAdvancedSearch=', e.[DefaultAdvancedSearch], ', ', 'LegacySolrCode=', e.[LegacySolrCode], ', ', 'SolrCode_Facets=', e.[SolrCode_Facets], ', ', 'SolrCode_Display=', e.[SolrCode_Display])
	FROM #Expected_SobekCM_Metadata_Types e
	WHERE NOT EXISTS (
		SELECT 1 FROM dbo.SobekCM_Metadata_Types r
		WHERE (r.[MetadataName] = e.[MetadataName] OR (r.[MetadataName] IS NULL AND e.[MetadataName] IS NULL))
		  AND (r.[SobekCode] = e.[SobekCode] OR (r.[SobekCode] IS NULL AND e.[SobekCode] IS NULL))
		  AND (r.[SolrCode] = e.[SolrCode] OR (r.[SolrCode] IS NULL AND e.[SolrCode] IS NULL))
		  AND (r.[DisplayTerm] = e.[DisplayTerm] OR (r.[DisplayTerm] IS NULL AND e.[DisplayTerm] IS NULL))
		  AND (r.[FacetTerm] = e.[FacetTerm] OR (r.[FacetTerm] IS NULL AND e.[FacetTerm] IS NULL))
		  AND (r.[CustomField] = e.[CustomField] OR (r.[CustomField] IS NULL AND e.[CustomField] IS NULL))
		  AND (r.[canFacetBrowse] = e.[canFacetBrowse] OR (r.[canFacetBrowse] IS NULL AND e.[canFacetBrowse] IS NULL))
		  AND (r.[DefaultAdvancedSearch] = e.[DefaultAdvancedSearch] OR (r.[DefaultAdvancedSearch] IS NULL AND e.[DefaultAdvancedSearch] IS NULL))
		  AND (r.[LegacySolrCode] = e.[LegacySolrCode] OR (r.[LegacySolrCode] IS NULL AND e.[LegacySolrCode] IS NULL))
		  AND (r.[SolrCode_Facets] = e.[SolrCode_Facets] OR (r.[SolrCode_Facets] IS NULL AND e.[SolrCode_Facets] IS NULL))
		  AND (r.[SolrCode_Display] = e.[SolrCode_Display] OR (r.[SolrCode_Display] IS NULL AND e.[SolrCode_Display] IS NULL))
	);

OPEN cur_SobekCM_Metadata_Types;
FETCH NEXT FROM cur_SobekCM_Metadata_Types INTO @Msg;
WHILE @@FETCH_STATUS = 0
BEGIN
	PRINT 'MISSING ROW: SobekCM_Metadata_Types (' + @Msg + ')';
	FETCH NEXT FROM cur_SobekCM_Metadata_Types INTO @Msg;
END;
CLOSE cur_SobekCM_Metadata_Types;
DEALLOCATE cur_SobekCM_Metadata_Types;

DROP TABLE #Expected_SobekCM_Metadata_Types;
GO

-------------------------------------------------------------------------------
-- SobekCM_Mime_Types  (ignoring MimeTypeID)
-------------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#Expected_SobekCM_Mime_Types') IS NOT NULL DROP TABLE #Expected_SobekCM_Mime_Types;

CREATE TABLE #Expected_SobekCM_Mime_Types (
	[MimeTypeID] int NULL,
	[Extension] varchar(20) NULL,
	[MimeType] varchar(100) NULL,
	[isBlocked] bit NULL,
	[shouldForward] bit NULL
);

INSERT INTO #Expected_SobekCM_Mime_Types ([MimeTypeID], [Extension], [MimeType], [isBlocked], [shouldForward]) VALUES
(1,N'.avi',N'video/x-msvideo',0,1),
(2,N'.bmp',N'image/bmp',0,0),
(3,N'.csv',N'application/octet-stream',0,0),
(4,N'.doc',N'application/msword',0,0),
(5,N'.docx',N'application/vnd.openxmlformats-officedocument.wordprocessingml.document',0,0),
(6,N'.dtd',N'text/xml',0,0),
(7,N'.fla',N'application/octet-stream',0,0),
(8,N'.gif',N'image/gif',0,0),
(9,N'.gtar',N'application/x-gtar',0,0),
(10,N'.gz',N'application/x-gzip',0,0),
(11,N'.htm',N'text/html',0,0),
(12,N'.html',N'text/html',0,0),
(13,N'.ico',N'image/x-icon',0,0),
(14,N'.jpeg',N'image/jpeg',0,0),
(15,N'.jpg',N'image/jpeg',0,0),
(16,N'.js',N'application/x-javascript',0,0),
(17,N'.mov',N'video/quicktime',0,1),
(18,N'.movie',N'video/x-sgi-movie',0,1),
(19,N'.mp2',N'video/mpeg',0,1),
(20,N'.mp3',N'audio/mpeg',0,1),
(21,N'.mpa',N'video/mpeg',0,1),
(22,N'.mpe',N'video/mpeg',0,1),
(23,N'.mpeg',N'video/mpeg',0,1),
(24,N'.mpg',N'video/mpeg',0,1),
(25,N'.mpp',N'application/vnd.ms-project',0,1),
(26,N'.mpv2',N'video/mpeg',0,1),
(27,N'.msi',N'application/octet-stream',0,0),
(28,N'.pdf',N'application/pdf',0,0),
(29,N'.pgm',N'image/x-portable-graymap',0,0),
(30,N'.png',N'image/png',0,0),
(31,N'.ppt',N'application/vnd.ms-powerpoint',0,0),
(32,N'.pptx',N'application/vnd.openxmlformats-officedocument.presentationml.presentation',0,0),
(33,N'.ra',N'audio/x-pn-realaudio',0,0),
(34,N'.ram',N'audio/x-pn-realaudio',0,0),
(35,N'.rm',N'application/vnd.rn-realmedia',0,0),
(36,N'.sgml',N'text/sgml',0,0),
(37,N'.swf',N'application/x-shockwave-flash',0,0),
(38,N'.tar',N'application/x-tar',0,0),
(39,N'.tif',N'image/tiff',0,0),
(40,N'.tiff',N'image/tiff',0,0),
(41,N'.txt',N'text/plain',0,0),
(42,N'.vsd',N'application/vnd.visio',0,0),
(43,N'.wav',N'audio/wav',0,1),
(44,N'.wm',N'video/x-ms-wm',0,1),
(45,N'.wma',N'audio/x-ms-wma',0,1),
(46,N'.wmv',N'video/x-ms-wmv',0,1),
(47,N'.xls',N'application/vnd.ms-excel',0,0),
(48,N'.xlsx',N'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',0,0),
(49,N'.xml',N'text/xml',0,0),
(50,N'.xsd',N'text/xml',0,0),
(51,N'.xsf',N'text/xml',0,0),
(52,N'.xsl',N'text/xml',0,0),
(53,N'.xslt',N'text/xml',0,0),
(54,N'.zip',N'application/x-zip-compressed',0,0),
(55,N'.jp2',N'image/jp2',0,0),
(56,N'.ogg',N'application/ogg',0,1),
(57,N'.mp4',N'video/mpeg',0,1),
(58,N'.ogm',N'application/ogg',0,0),
(59,N'.m4a',N'audio/mpeg',0,1),
(60,N'.m4v',N'video/mpeg',0,1),
(61,N'.sql',N'text/plain',0,0),
(62,N'.mkv',N'video/x-matroksa',0,1),
(63,N'.webm',N'video/webm',0,1),
(64,N'.mxf',N'application/mxf',0,0),
(65,N'.mets',N'text/xml',0,0),
(66,N'.archive',N'archive',1,0);

DECLARE @Msg nvarchar(max);
DECLARE cur_SobekCM_Mime_Types CURSOR LOCAL FAST_FORWARD FOR
	SELECT CONCAT('MimeTypeID=', e.[MimeTypeID], ', ', 'Extension=', e.[Extension], ', ', 'MimeType=', e.[MimeType], ', ', 'isBlocked=', e.[isBlocked], ', ', 'shouldForward=', e.[shouldForward])
	FROM #Expected_SobekCM_Mime_Types e
	WHERE NOT EXISTS (
		SELECT 1 FROM dbo.SobekCM_Mime_Types r
		WHERE (r.[Extension] = e.[Extension] OR (r.[Extension] IS NULL AND e.[Extension] IS NULL))
		  AND (r.[MimeType] = e.[MimeType] OR (r.[MimeType] IS NULL AND e.[MimeType] IS NULL))
		  AND (r.[isBlocked] = e.[isBlocked] OR (r.[isBlocked] IS NULL AND e.[isBlocked] IS NULL))
		  AND (r.[shouldForward] = e.[shouldForward] OR (r.[shouldForward] IS NULL AND e.[shouldForward] IS NULL))
	);

OPEN cur_SobekCM_Mime_Types;
FETCH NEXT FROM cur_SobekCM_Mime_Types INTO @Msg;
WHILE @@FETCH_STATUS = 0
BEGIN
	PRINT 'MISSING ROW: SobekCM_Mime_Types (' + @Msg + ')';
	FETCH NEXT FROM cur_SobekCM_Mime_Types INTO @Msg;
END;
CLOSE cur_SobekCM_Mime_Types;
DEALLOCATE cur_SobekCM_Mime_Types;

DROP TABLE #Expected_SobekCM_Mime_Types;
GO

-------------------------------------------------------------------------------
-- SobekCM_Settings  (ignoring SettingID, Setting_Value)
-------------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#Expected_SobekCM_Settings') IS NOT NULL DROP TABLE #Expected_SobekCM_Settings;

CREATE TABLE #Expected_SobekCM_Settings (
	[Setting_Key] varchar(255) NULL,
	[Setting_Value] varchar(max) NULL,
	[TabPage] nvarchar(75) NULL,
	[Heading] nvarchar(75) NULL,
	[Hidden] bit NULL,
	[Reserved] smallint NULL,
	[Help] varchar(max) NULL,
	[Options] varchar(max) NULL,
	[SettingID] int NULL,
	[Dimensions] varchar(100) NULL
);

INSERT INTO #Expected_SobekCM_Settings ([Setting_Key], [Setting_Value], [TabPage], [Heading], [Hidden], [Reserved], [Help], [Options], [SettingID], [Dimensions]) VALUES
(N'Ace Editor Theme',N'chrome',N'General Settings',N'UI Settings',0,0,N'Set the theme for the Ace editor, used for CSS and Javascript editing, as well as TEI editing, if that plug-in is enabled.',N'{ACE_THEMES}',77,NULL),
(N'Allow Mass Behavior Update',N'false',N'Digital Resource Settings',N'Online Management Settings',0,0,N'Whether the administrative options to mass update the behaviors is available',N'true|false',84,NULL),
(N'Allow Page Image File Management',N'true',N'Deprecated',N'Deprecated',0,0,N'Help for Allow Page Image File Management',N'true|false',1,NULL),
(N'Application Server Network',N'C:\GitHub\SobekCM-Web-Application\Code\SobekCM\',N'System / Server Settings',N'Server Settings',0,2,N'Server share for the web application''s network location.\n\nExample: ''\\\\lib-sandbox\\Production\\''',NULL,2,NULL),
(N'Application Server URL',N'https://localhost:51186/',N'System / Server Settings',N'Server Settings',0,2,N'Base URL which points to the web application.\n\nExamples: ''http://localhost/sobekcm/'', ''http://ufdc.ufl.edu/'', etc..',NULL,3,NULL),
(N'Archive DropBox',N'',N'Builder',N'Archive Settings',0,0,N'Network location for the archive drop box.  If this is set to a value, the builder/bulk loader will place a copy of the package in this folder for archiving purposes.  This folder is where any of your archiving processes should look for new packages.',NULL,4,NULL),
(N'Builder IIS Logs Directory',N'\\sobek-frontend\f$\LogFiles\opennj\W3SVC7',N'Builder',N'Builder Settings',0,0,N'IIS web log location (usually a network share) for the builder to read the logs and add the usage statistics to the database.',NULL,55,NULL),
(N'Builder Last Message',N'No New Packages - Process Complete',N'Builder',N'Status',0,0,N'Help for Builder Last Message',NULL,56,NULL),
(N'Builder Last Run Finished',N'7/22/2026 1:35:02 PM',N'Builder',N'Status',0,0,N'Help for Builder Last Run Finished',NULL,57,NULL),
(N'Builder Log Expiration in Days',N'10',N'Builder',N'Builder Settings',0,0,N'Number of days the SobekCM Builder logs are retained.',N'10|30|365|99999',58,NULL),
(N'Builder Operation Flag',N'STANDARD OPERATION',N'Builder',N'Status',0,0,N'Last flag set when the builder/bulk loader ran.',N'STANDARD OPERATION|PAUSE REQUESTED|ABORT REQUESTED|NO BUILDER REQUESTED ',5,NULL),
(N'Builder Seconds Between Polls',N'60',N'Builder',N'Builder Settings',0,0,N'Number of seconds the builder remains idle before checking for new incoming package again.',N'15|60|300|600',6,NULL),
(N'Builder Send Usage Emails',N'false',N'Builder',N'Builder Settings',0,0,N'Flag indicates is usage emails should be sent automatically after the stats usage has been calculated and added to the database.',N'true|false',59,NULL),
(N'Builder Version',N'4.12.0',N'Builder',N'Status',0,0,N'Help for Builder Version',NULL,60,NULL),
(N'Can Remove Single Search Term',N'true',N'General Settings',N'Search Settings',0,0,N'When this is set to TRUE, users can remove a single search term from their current search.  Setting this to FALSE, makes the display slightly cleaner.',N'true|false',7,NULL),
(N'Can Submit Edit Online',N'true',N'Digital Resource Settings',N'Online Management Settings',0,0,N'Flag dictates if users can submit items online, or if this is disabled in this system.',N'true|false',8,NULL),
(N'Can Submit Items Online',N'true',N'System / Server Settings',N'System Settings',0,2,N'Flag dictates if users can submit items online, or if this is disabled in this system.',N'true|false',61,NULL),
(N'Convert Office Files to PDF',N'true',N'Builder',N'Builder Settings',0,0,N'Flag dictates if users can submit items online, or if this is disabled in this system.',N'true|false',9,NULL),
(N'Create MARC Feed By Default',N'false',N'Builder',N'Builder Settings',0,0,N'Flag indicates if the builder/bulk loader should create the MARC feed by default when operating in background mode.',N'true|false',10,NULL),
(N'Detailed User Permissions',N'false',N'System / Server Settings',N'System Settings',0,2,N'Flag indicates if more refined user permissions can be assigned, such as if a user can edit behaviors of an item in a collection vs. a more general flag that says a RequestSpecificValues.Current_User can make all changes to an item in a collection.',N'true|false',11,NULL),
(N'Disable Standard User Logon Flag',N'false',N'System / Server Settings',N'System Settings',0,2,N'Flag indicates if non system administrators are temporarily barred from logging on.',N'true|false',62,NULL),
(N'Disable Standard User Logon Message',N'',N'System / Server Settings',N'System Settings',0,2,N'Message displayed if non syste administrators are temporarily barred from logging on.',NULL,63,NULL),
(N'Document Solr Index URL',N'http://10.100.0.3:8983/solr/opennj_documents/',N'System / Server Settings',N'Search Preferences',0,2,N'URL for the document-level solr index.\n\nExample: ''http://localhost:8983/solr/documents''',NULL,12,NULL),
(N'Email Default From Address',N'Mark.V.Sullivan@sobekdigital.com',N'General Settings',N'Email Settings',0,0,N'Email address that emails from this system should utilize',NULL,64,N'300'),
(N'Email Default From Name',N'',N'General Settings',N'Email Settings',0,0,N'Display name to associate with emails sent from this system (otherwise the instance/portal name will be used)',NULL,65,N'300'),
(N'Email Method',N'DATABASE MAIL',N'System / Server Settings',N'Email Setup',0,2,N'Indicated whether the database mail system or the SMTP direct email system should be utilizied',N'DATABASE MAIL|SMTP DIRECT',13,NULL),
(N'Email On User Registration',N'mochoa@middlesexcc.edu;schudnick@middlesexcc.edu;rpieters@middlesexcc.edu',N'General Settings',N'Email Settings',0,0,N'If an email address is provided here, an email will be sent when each new user registers.\n\nIf you are using multiple email addresses, seperate them with a semi-colon.\n\nExample: ''person1@corp.edu;person2@corp.edu''',NULL,78,N'300'),
(N'Email SMTP Port',N'25',N'System / Server Settings',N'Email Setup',0,2,N'If direct SMTP email sending is used, the port to utilize.  This must be numeric.',NULL,66,N'70'),
(N'Email SMTP Server',N'',N'System / Server Settings',N'Email Setup',0,2,N'If direct SMTP email sending is used, the server name to send emails to.',NULL,67,NULL),
(N'Facets Collapsible',N'false',N'General Settings',N'Search Settings',0,0,N'Flag determines if the facets are collapsible like an accordian, or if they all start fully expanded.',N'true|false',14,NULL),
(N'FDA Report DropBox',N'',N'Florida SUS Settings',N'General Settings',1,0,N'Location for the builder/bulk loader to look for incoming Florida Dark Archive XML reports to process and add to the history of digital resources.',NULL,15,NULL),
(N'Files To Exclude From Downloads',N'((.*?)\.(jpg|tif|jp2|jpx|bmp|jpeg|gif|png|txt|pro|mets|db|xml|bak|job)$|qc_error.html)',N'Digital Resource Settings',N'General Settings',0,0,N'Regular expressions used to exclude files from being added by default to the downloads of resources.\n\nExample: ''((.*?)\\.(jpg|tif|jp2|jpx|bmp|jpeg|gif|png|txt|pro|mets|db|xml|bak|job)$|qc_error.html)''',NULL,16,NULL),
(N'Google Map API Key',N'',N'System / Server Settings',N'System Settings',0,2,N'Google Map API key for displaying geographic displays within this system.  Help is found at http://sobekrepository.org/software/config/googlemaps.',NULL,17,NULL),
(N'Help Metadata URL',N'http://sobekrepository.org/',N'General Settings',N'Help Settings',0,0,N'URL used for the help pages when users request help on metadata elements during online submit and editing.\n\nExample (and default): ''http://sobekrepository.org/''',NULL,18,NULL),
(N'Help URL',N'http://sobekrepository.org/',N'General Settings',N'Help Settings',0,0,N'URL used for the main help pages about this system''s basic functionality.\n\nExample (and default): ''http://sobekrepository.org/''',NULL,19,NULL),
(N'Image Server Network',N'\\sobek-frontend\e$\open-nj\',N'System / Server Settings',N'Server Settings',0,2,N'Network location to the content for all of the digital resources (images, metadata, etc.).\n\nExample: ''C:\\inetpub\\wwwroot\\UFDC Web\\SobekCM\\content\\'' or ''\\\\ufdc-images\\content\\''',NULL,20,NULL),
(N'Image Server URL',N'https://opennj.net/content/',N'System / Server Settings',N'Server Settings',0,2,N'URL which points to the digital resource images.\n\nExample: ''http://localhost/sobekcm/content/'' or ''http://ufdcimages.uflib.ufl.edu/''',NULL,21,NULL),
(N'Include Partners On System Home',N'false',N'General Settings',N'Instance Settings',0,0,N'This option controls whether a PARTNERS option appears on the main system home page, assuming there are multiple institutional aggregations.',N'true|false',22,NULL),
(N'Include Result Count In Text',N'true',N'General Settings',N'Search Settings',0,0,N'When this is set to TRUE, the result count will be displayed in the search explanation text ( i.e., Your search for ... resulted in 2 results ).  Setting this to FALSE will not show the final portion in that text.',N'true|false',76,NULL),
(N'Include TreeView On System Home',N'true',N'General Settings',N'Instance Settings',0,0,N'This option controls whether a TREE VIEW option appears on the main system home page which displays all the active aggregations hierarchically in a tree view.',N'true|false',23,NULL),
(N'Instance Code',N'OpenNJ',N'System / Server Settings',N'System Settings',0,2,N'Instance code used for shared database or solr instances',NULL,85,NULL),
(N'JPEG Height',N'1000',N'Digital Resource Settings',N'Image Settings',0,0,N'Restriction on the size of the jpeg page images'' height (in pixels) when generated automatically by the builder/bulk loader.\n\nDefault: ''1000''',NULL,24,N'60'),
(N'JPEG Width',N'630',N'Digital Resource Settings',N'Image Settings',0,0,N'Restriction on the size of the jpeg page images'' width (in pixels) when generated automatically by the builder/bulk loader.\n\nDefault: ''630''',NULL,25,N'60'),
(N'JPEG2000 Server',N'',N'System / Server Settings',N'Server Settings',0,2,N'URL for the Aware JPEG2000 Server for displaying and zooming into JPEG2000 images.',NULL,26,NULL),
(N'JPEG2000 Server Type',N'Built-In IIPImage',N'System / Server Settings',N'Server Settings',0,2,N'Type of the JPEG2000 server found at the URL above.',N'Built-In IIPImage|None',27,NULL),
(N'Kakadu JPEG2000 Create Command',N'',N'Builder',N'Builder Settings',0,0,N'Kakadu JPEG2000 script will override the specifications used when creating zoomable images.\n\nIf this is blank, the default specifications will be used which match those used by the National Digital Newspaper Program and University of Florida Digital Collections.',NULL,68,NULL),
(N'Main Builder Input Folder',N'C:\FTP\OpenNJ\builder\',N'Builder',N'Builder Settings',0,0,N'This is the network location to the SobekCM Builder''s main incoming folder.\n\nThis is used by the SMaRT tool when doing bulk imports from spreadsheet or MARC records.',NULL,28,NULL),
(N'Manage GeoSpatial Data',N'false',N'Digital Resource Settings',N'Online Management Settings',0,0,N'Whether the beta options to manage geo-spatial data will be displayed',N'true|false',83,NULL),
(N'Mango Union Search Base URL',N'',N'Florida SUS Settings',N'General Settings',1,0,N'Florida SUS state-wide catalog base URL for determining the number of physical holdings which match a given search.\n\nExample: ''http://solrcits.fcla.edu/citsZ.jsp?type=search&base=uf''',NULL,29,NULL),
(N'Mango Union Search Text',N'',N'Florida SUS Settings',N'General Settings',1,0,N'Text to display the number of hits found in the Florida SUS-wide catalog.\n\nUse the value ''%1'' in the string where the number of hits should be inserted.\n\nExample: ''%1 matches found in the statewide catalog''',NULL,30,NULL),
(N'MARC Cataloging Source Code',N'',N'Digital Resource Settings',N'Metadata Settings',0,0,N'Cataloging source code for the 040 field, ( for example ''FUG'' for University of Florida )',NULL,69,N'60'),
(N'MARC Location Code',N'',N'Digital Resource Settings',N'Metadata Settings',0,0,N'Location code for the 852 |a - if none is given the system abbreviation will be used. Otherwise, the system abbreviation will be put in the 852 |b field.',NULL,70,N'60'),
(N'MARC Reproduction Agency',N'',N'Digital Resource Settings',N'Metadata Settings',0,0,N'Agency responsible for reproduction, or primary agency associated with the SobekCM instance ( for the added 533 |c field )\n\nThis 533 is not added for born digital items.',NULL,71,NULL),
(N'MARC Reproduction Place',N'',N'Digital Resource Settings',N'Metadata Settings',0,0,N'Place of reproduction, or primary location associated with the SobekCM instance ( for the added 533 |b field ).\n\nThis 533 is not added for born digital items.',NULL,72,NULL),
(N'MARC XSLT File',N'',N'Digital Resource Settings',N'Metadata Settings',0,0,N'XSLT file to use as a final transform, after the standard MarcXML file is written.\n\nThis only affects generated MarcXML ( for the feeds and OAI ) not the dispayed in-system MARC ( as of January 2015 ).  This file should appear in the config/users folder.',NULL,73,NULL),
(N'MarcXML Feed Location',N'',N'Builder',N'Builder Settings',0,0,N'Network location or share where any geneated MarcXML feed should be written.\n\nExample: ''\\\\lib-sandbox\\Data\\''',NULL,31,NULL),
(N'OCR Engine Command',N'',N'Builder',N'Builder Settings',0,0,N'If you wish to utilize an OCR engine in the builder/bulk loader, add the command-line call to the engine here.\n\nUse %1 as a place holder for the ingoing image file name and %2 as a placeholder for the output text file name.\n\nExample: ''C:\\OCR\\Engine.exe -in %1 -out %2''',NULL,32,NULL),
(N'Page Solr Index URL',N'http://10.100.0.3:8983/solr/opennj_pages/',N'System / Server Settings',N'Search Preferences',0,2,N'URL for the resource-level solr index used when searching for matching pages within a single document.\n\nExample: ''http://localhost:8983/solr/pages''',NULL,33,NULL),
(N'PostArchive Files To Delete',N'(.*?)\.(tif)',N'Builder',N'Archive Settings',0,0,N'Regular expression indicates which files should be deleted AFTER being archived by the builder/bulk loader.\n\nExample: ''(.*?)\\.(tif)''',NULL,34,NULL),
(N'PreArchive Files To Delete',N'(.*?)\.(QC.jpg)',N'Builder',N'Archive Settings',0,0,N'Regular expression indicates which files should be deleted BEFORE being archived by the builder/bulk loader.\n\nExample: ''(.*?)\\.(QC.jpg)''',NULL,35,NULL),
(N'Privacy Email Address',N'Mark.V.Sullivan@sobekdigital.com',N'General Settings',N'Email Settings',0,0,N'Email address which receives notification if personal information (such as Social Security Numbers) is potentially found while loading or post-processing an item.\n\nIf you are using multiple email addresses, seperate them with a semi-colon.\n\nExample: ''person1@corp.edu;person2@corp.edu''',NULL,36,NULL),
(N'Search System',N'OpenSobek',N'System / Server Settings',N'Search Preferences',0,0,N'Which system and schema to use for searching',N'OpenSobek',79,NULL),
(N'Send Email On Added Aggregation',N'Always',N'General Settings',N'Email Settings',0,0,N'Flag indicates when emails should be sent after new item aggregations are added through the web interface.',N'Always|Never',74,NULL),
(N'Show Citation For Dark Items',N'true',N'Digital Resource Settings',N'Online Behavior',0,0,N'Flag indicates if the citation is displayed online for DARK items.',N'true|false',37,NULL),
(N'Show Florida SUS Settings',N'false',N'Deprecated',N'Deprecated',0,0,N'Some system settings are only applicable to institutions which are part of the Florida State University System.  Setting this value to TRUE will show these settings, while FALSE will suppress them.\n\nIf this value is changed, you willl need to save the settings for it to reload and reflect the change.',N'true|false',38,NULL),
(N'SobekCM Web Server IP',N'10.100.0.9',N'System / Server Settings',N'Server Settings',0,2,N'IP address for the web server running this web repository software.\n\nThis is used for setting restricted or dark material to only be available for the web server, which then acts as a proxy/web server to serve that content to authenticated users.',NULL,39,N'200'),
(N'Spreadsheet Library License',N'rptfH59FQbdh/xnbn2HROqPjiaMPmz3L',N'System / Server Settings',N'System Settings',0,2,N'License code (encrypted) for spreadsheet library',NULL,40,NULL),
(N'Static Pages Location',N'\\sobek-frontend\wwwroot\open-nj\data\',N'System / Server Settings',N'Caching Settings',0,2,N'Location where the static files are located for providing the full citation and text for indexing, either on the same server as the web application or as a network share.\n\nIt is recommended that these files be on the same server as the web server, rather than remote storage, to increase the speed in which requests from search engine indexers can be fulfilled.\n\nExample: ''C:\\inetpub\\wwwroot\\UFDC Web\\SobekCM\\data\\''.',NULL,41,NULL),
(N'Static Resources Source',N'cdn secure',N'System / Server Settings',N'Server Settings',0,2,N'Indicates the general source of all the static resources, such as javascript, system default stylesheets, images, and included libraries.\n\nUsing CDN will result in better performance, but can only be used when users will have access to the database.\n\nThis actually indicates which configuration file to read to determine the base location of the default resources.',N'{STATIC_SOURCE_CODES}',75,NULL),
(N'Statistics Caching Enabled',N'false',N'System / Server Settings',N'Caching Settings',0,2,N'Flag indicates if the basic usage and item count information should be cached for up to 24 hours as static XML files written in the web server''s temp directory.\n\nThis should be enabled if your library is quite large as it can take a fair amount of time to retrieve this information and these screens are made available for search engine index robots for indexing.',N'true|false',42,NULL),
(N'System Base Abbreviation',N'OPENNJ',N'General Settings',N'Instance Settings',0,0,N'Base abbreviation to be used when the system refers to itself to the RequestSpecificValues.Current_User, such as the main tabs to take a user to the home pages.\n\nThis abbreviation should be kept as short as possible.\n\nExamples: ''UFDC'', ''dLOC'', ''Sobek'', etc..',NULL,43,N'100'),
(N'System Base Name',N'Open-NJ',N'General Settings',N'Instance Settings',0,0,N'Overall name of the system, to be used when creating MARC records and in several other locations.',NULL,44,NULL),
(N'System Base URL',N'https://localhost:51186/',N'System / Server Settings',N'Server Settings',0,2,N'Base URL which points to the web application.\n\nExamples: ''http://localhost/sobekcm/'', ''http://ufdc.ufl.edu/'', etc..',NULL,45,NULL),
(N'System Default Language',N'English',N'General Settings',N'Instance Settings',0,0,N'Default system user interface language.  If the user''s HTML request does not include a language supported by the interface or which does not include specific translations for a field, this default language is utilized.',NULL,46,NULL),
(N'System Email',N'Mark.V.Sullivan@sobekdigital.com;mochoa@middlesexcc.edu;schudnick@middlesexcc.edu',N'General Settings',N'Email Settings',0,0,N'Default email address for the system, which is sent emails when users opt to contact the administrators.\n\nThis can be changed for individual aggregations but at least one email is required for the overall system.\n\nIf you are using multiple email addresses, seperate them with a semi-colon.\n\nExample: ''person1@corp.edu;person2@corp.edu''',NULL,47,NULL),
(N'System Error Email',N'Mark.V.Sullivan@sobekdigital.com',N'General Settings',N'Email Settings',0,0,N'Email address used when a critical system error occurs which may require investigation or correction.\n\nIf you are using multiple email addresses, seperate them with a semi-colon.\n\nExample: ''person1@corp.edu;person2@corp.edu''',NULL,48,NULL),
(N'Thumbnail Height',N'300',N'Digital Resource Settings',N'Image Settings',0,0,N'Restriction on the size of the page image thumbnails'' height (in pixels) when generated automatically by the builder/bulk loader.\n\nDefault: ''300''',NULL,49,N'60'),
(N'Thumbnail Width',N'150',N'Digital Resource Settings',N'Image Settings',0,0,N'Restriction on the size of the page image thumbnails'' width (in pixels) when generated automatically by the builder/bulk loader.\n\nDefault: ''150''',NULL,50,N'60'),
(N'Upload File Types',N'.aif,.aifc,.aiff,.au,.avi,.bz2,.c,.c++,.css,.csv,.dbf,.ddl,.doc,.docx,.dtd,.dvi,.epub,.flac,.gz,.htm,.html,.java,.jps,.js,.m4a,.m4p,.mid,.midi,.mkv,.mp2,.mp3,.mp4,.mpg,.odp,.ogg,.ogm,.pdf,.pgm,.ppt,.pptx,.ps,.ra,.ram,.rar,.rm,.rtf,.sgml,.swf,.sxi,.tbz2,.tgz,.vtt,.wav,.wave,.webm,.wma,.wmv,.xls,.xlsx,.xml,.zip',N'Digital Resource Settings',N'Online Management Settings',0,0,N'List of non-image extensions which are allowed to be uploaded into a digital resource.\n\nList should be the extensions, with the period, separated by commas.\n\nExample: .aif,.aifc,.aiff,.au,.avi,.bz2,.c,.c++,.css,.dbf,.ddl,...',NULL,51,N'600|3'),
(N'Upload Image Types',N'.txt,.tif,.jpg,.jp2,.pro',N'Digital Resource Settings',N'Online Management Settings',0,0,N'List of page image extensions which are allowed to be uploaded into a digital resource to display as page images.\n\nList should be the extensions, with the period, separated by commas.\n\nExample: .txt,.tif,.jpg,.jp2,.pro',NULL,52,N'600'),
(N'Use Tracking Sheet',N'false',N'Digital Resource Settings',N'Online Management Settings',0,0,N'Whether the administrative options to use the tracking sheet will be displayed',N'true|false',82,NULL),
(N'Web In Process Submission Location',N'\\sobek-frontend\wwwroot\open-nj\mySobek\InProcess\',N'System / Server Settings',N'Server Settings',0,2,N'Location where packages are built by users during online submissions and metadata updates.\n\nThis generally needs to be on the web server and have appropriate access for read/write.\n\nIf nothing is indicated in this field, the system will automatically use the ''mySobek\\InProcess'' subfolder under the web application.',NULL,53,NULL),
(N'Web Output Caching Minutes',N'1',N'System / Server Settings',N'Caching Settings',0,2,N'This setting controls how long the client''s browser is instructed to cache the served web page.\n\nSetting this value higher removes the round-trip when requesting a recently requested page.  It also means that some changes may not be reflected until the refresh button is pressed.\n\nIn general, this setting is only applied to public-style pages, and not personalized pages, such as the bookshelf views.',N'0|1|2|3|5|10|15',54,NULL);

DECLARE @Msg nvarchar(max);
DECLARE cur_SobekCM_Settings CURSOR LOCAL FAST_FORWARD FOR
	SELECT CONCAT('Setting_Key=', e.[Setting_Key], ', ', 'Setting_Value=', e.[Setting_Value], ', ', 'TabPage=', e.[TabPage], ', ', 'Heading=', e.[Heading], ', ', 'Hidden=', e.[Hidden], ', ', 'Reserved=', e.[Reserved], ', ', 'Help=', e.[Help], ', ', 'Options=', e.[Options], ', ', 'SettingID=', e.[SettingID], ', ', 'Dimensions=', e.[Dimensions])
	FROM #Expected_SobekCM_Settings e
	WHERE NOT EXISTS (
		SELECT 1 FROM dbo.SobekCM_Settings r
		WHERE (r.[Setting_Key] = e.[Setting_Key] OR (r.[Setting_Key] IS NULL AND e.[Setting_Key] IS NULL))
		  AND (r.[TabPage] = e.[TabPage] OR (r.[TabPage] IS NULL AND e.[TabPage] IS NULL))
		  AND (r.[Heading] = e.[Heading] OR (r.[Heading] IS NULL AND e.[Heading] IS NULL))
		  AND (r.[Hidden] = e.[Hidden] OR (r.[Hidden] IS NULL AND e.[Hidden] IS NULL))
		  AND (r.[Reserved] = e.[Reserved] OR (r.[Reserved] IS NULL AND e.[Reserved] IS NULL))
		  AND (r.[Help] = e.[Help] OR (r.[Help] IS NULL AND e.[Help] IS NULL))
		  AND (r.[Options] = e.[Options] OR (r.[Options] IS NULL AND e.[Options] IS NULL))
		  AND (r.[Dimensions] = e.[Dimensions] OR (r.[Dimensions] IS NULL AND e.[Dimensions] IS NULL))
	);

OPEN cur_SobekCM_Settings;
FETCH NEXT FROM cur_SobekCM_Settings INTO @Msg;
WHILE @@FETCH_STATUS = 0
BEGIN
	PRINT 'MISSING ROW: SobekCM_Settings (' + @Msg + ')';
	FETCH NEXT FROM cur_SobekCM_Settings INTO @Msg;
END;
CLOSE cur_SobekCM_Settings;
DEALLOCATE cur_SobekCM_Settings;

DROP TABLE #Expected_SobekCM_Settings;
GO

-------------------------------------------------------------------------------
-- Tracking_Disposition_Type  (ignoring DispositionID)
-------------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#Expected_Tracking_Disposition_Type') IS NOT NULL DROP TABLE #Expected_Tracking_Disposition_Type;

CREATE TABLE #Expected_Tracking_Disposition_Type (
	[DispositionID] int NULL,
	[DispositionFuture] varchar(100) NULL,
	[DispositionPast] varchar(100) NULL,
	[DispositionNotes] varchar(1000) NULL
);

INSERT INTO #Expected_Tracking_Disposition_Type ([DispositionID], [DispositionFuture], [DispositionPast], [DispositionNotes]) VALUES
(1,N'Return',N'Returned',N'Returned material to collection manager, or original requestor'),
(2,N'Request Withdraw',N'Requested Withdraw',N'Sent to cataloging to request a withdraw'),
(3,N'Discard',N'Discarded',N'Returned material to collection manager, or original requestor'),
(4,N'No Physical Copy',N'No Physical Copy',N'There is no physical copy to be disposed of here.');

DECLARE @Msg nvarchar(max);
DECLARE cur_Tracking_Disposition_Type CURSOR LOCAL FAST_FORWARD FOR
	SELECT CONCAT('DispositionID=', e.[DispositionID], ', ', 'DispositionFuture=', e.[DispositionFuture], ', ', 'DispositionPast=', e.[DispositionPast], ', ', 'DispositionNotes=', e.[DispositionNotes])
	FROM #Expected_Tracking_Disposition_Type e
	WHERE NOT EXISTS (
		SELECT 1 FROM dbo.Tracking_Disposition_Type r
		WHERE (r.[DispositionFuture] = e.[DispositionFuture] OR (r.[DispositionFuture] IS NULL AND e.[DispositionFuture] IS NULL))
		  AND (r.[DispositionPast] = e.[DispositionPast] OR (r.[DispositionPast] IS NULL AND e.[DispositionPast] IS NULL))
		  AND (r.[DispositionNotes] = e.[DispositionNotes] OR (r.[DispositionNotes] IS NULL AND e.[DispositionNotes] IS NULL))
	);

OPEN cur_Tracking_Disposition_Type;
FETCH NEXT FROM cur_Tracking_Disposition_Type INTO @Msg;
WHILE @@FETCH_STATUS = 0
BEGIN
	PRINT 'MISSING ROW: Tracking_Disposition_Type (' + @Msg + ')';
	FETCH NEXT FROM cur_Tracking_Disposition_Type INTO @Msg;
END;
CLOSE cur_Tracking_Disposition_Type;
DEALLOCATE cur_Tracking_Disposition_Type;

DROP TABLE #Expected_Tracking_Disposition_Type;
GO

-------------------------------------------------------------------------------
-- Tracking_WorkFlow  (ignoring WorkFlowID)
-------------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#Expected_Tracking_WorkFlow') IS NOT NULL DROP TABLE #Expected_Tracking_WorkFlow;

CREATE TABLE #Expected_Tracking_WorkFlow (
	[WorkFlowID] int NULL,
	[WorkFlowName] varchar(100) NULL,
	[WorkFlowNotes] varchar(1000) NULL,
	[Start_Event_Number] int NULL,
	[End_Event_Number] int NULL,
	[Start_And_End_Event_Number] int NULL,
	[Start_Event_Desc] nvarchar(100) NULL,
	[End_Event_Desc] nvarchar(100) NULL
);

INSERT INTO #Expected_Tracking_WorkFlow ([WorkFlowID], [WorkFlowName], [WorkFlowNotes], [Start_Event_Number], [End_Event_Number], [Start_And_End_Event_Number], [Start_Event_Desc], [End_Event_Desc]) VALUES
(1,N'Record Created',N'A record for this item was created',NULL,NULL,NULL,NULL,NULL),
(3,N'Scanning',N'Some portion of this item was scanned',NULL,NULL,NULL,NULL,NULL),
(4,N'PreQC',N'This item was prepared for Quality Control',NULL,NULL,NULL,NULL,NULL),
(5,N'QC Accept',N'Quality control was performed on this item',NULL,NULL,NULL,NULL,NULL),
(6,N'OCR',N'OCR was performed on this item',NULL,NULL,NULL,NULL,NULL),
(9,N'UFDC New',N'This item was loaded into UFDC as a new item',NULL,NULL,NULL,NULL,NULL),
(10,N'UFDC Replacement',N'This item was loaded into UFDC as a replacement',NULL,NULL,NULL,NULL,NULL),
(11,N'Metadata Update',N'A metadata update was applied by the SobekCM Bulk Loader',NULL,NULL,NULL,NULL,NULL),
(22,N'FDA Error',N'FDA was unable to load the item',NULL,NULL,NULL,NULL,NULL),
(23,N'FDA Ingest',N'FDA ingested the item',NULL,NULL,NULL,NULL,NULL),
(28,N'Archived to Tivoli',N'Files saved into CNS Tivoli backup solution',NULL,NULL,NULL,NULL,NULL),
(29,N'Online Submit',N'Item was submitted via the online interface',NULL,NULL,NULL,NULL,NULL),
(30,N'Online Edit',N'Metadata was edited for this item online',NULL,NULL,NULL,NULL,NULL),
(31,N'QC Reject',N'Rejected during quality control',NULL,NULL,NULL,NULL,NULL),
(34,N'Made Public',N'Item was switched to PUBLIC visibility',NULL,NULL,NULL,NULL,NULL),
(35,N'Made Private',N'Item was switched to PRIVATE visibility',NULL,NULL,NULL,NULL,NULL),
(36,N'Made Restricted',N'Item was switch to some IP RESTRICTED visibility',NULL,NULL,NULL,NULL,NULL),
(37,N'Digitization Requested',N'Digitization of this item was requested by an individual or organization',NULL,NULL,NULL,NULL,NULL),
(38,N'OCLC Number Added',N'New OCLC number provided for this item',NULL,NULL,NULL,NULL,NULL),
(39,N'Image Processing',N'Post-acquisition image processing ( copyright blur, cropping, color managment, etc.. )',NULL,NULL,NULL,NULL,NULL),
(40,N'Bulk Loaded',N'Loaded into SobekCM through the bulk loader',NULL,NULL,NULL,NULL,NULL),
(41,N'QC Preliminary',N'Preliminary QC performed, but neither rejected nor finalized',NULL,NULL,NULL,NULL,NULL),
(42,N'Material Received',N'Physical material received into the digitization location',NULL,NULL,NULL,NULL,NULL),
(43,N'Material Disposition',N'Physical material handled post-digitization',NULL,NULL,NULL,NULL,NULL),
(44,N'Post-Processed',N'Bulk Loader performed post-loading processes for derivative creation, thumbnails, etc..',NULL,NULL,NULL,NULL,NULL),
(45,N'Updated Pages/Divisions',N'Using the online QC tool, updated the page names, divisions, page order, etc..',NULL,NULL,NULL,NULL,NULL),
(46,N'Uploaded Page Images',N'Uploaded new page images for the item',NULL,NULL,NULL,NULL,NULL),
(47,N'Updated Coordinates',N'Used the online map edit feature to add or edit coordinates associated with this item',NULL,NULL,NULL,NULL,NULL),
(48,N'Managed Downloads',N'Managed the download files for this item',NULL,NULL,NULL,NULL,NULL);

DECLARE @Msg nvarchar(max);
DECLARE cur_Tracking_WorkFlow CURSOR LOCAL FAST_FORWARD FOR
	SELECT CONCAT('WorkFlowID=', e.[WorkFlowID], ', ', 'WorkFlowName=', e.[WorkFlowName], ', ', 'WorkFlowNotes=', e.[WorkFlowNotes], ', ', 'Start_Event_Number=', e.[Start_Event_Number], ', ', 'End_Event_Number=', e.[End_Event_Number], ', ', 'Start_And_End_Event_Number=', e.[Start_And_End_Event_Number], ', ', 'Start_Event_Desc=', e.[Start_Event_Desc], ', ', 'End_Event_Desc=', e.[End_Event_Desc])
	FROM #Expected_Tracking_WorkFlow e
	WHERE NOT EXISTS (
		SELECT 1 FROM dbo.Tracking_WorkFlow r
		WHERE (r.[WorkFlowName] = e.[WorkFlowName] OR (r.[WorkFlowName] IS NULL AND e.[WorkFlowName] IS NULL))
		  AND (r.[WorkFlowNotes] = e.[WorkFlowNotes] OR (r.[WorkFlowNotes] IS NULL AND e.[WorkFlowNotes] IS NULL))
		  AND (r.[Start_Event_Number] = e.[Start_Event_Number] OR (r.[Start_Event_Number] IS NULL AND e.[Start_Event_Number] IS NULL))
		  AND (r.[End_Event_Number] = e.[End_Event_Number] OR (r.[End_Event_Number] IS NULL AND e.[End_Event_Number] IS NULL))
		  AND (r.[Start_And_End_Event_Number] = e.[Start_And_End_Event_Number] OR (r.[Start_And_End_Event_Number] IS NULL AND e.[Start_And_End_Event_Number] IS NULL))
		  AND (r.[Start_Event_Desc] = e.[Start_Event_Desc] OR (r.[Start_Event_Desc] IS NULL AND e.[Start_Event_Desc] IS NULL))
		  AND (r.[End_Event_Desc] = e.[End_Event_Desc] OR (r.[End_Event_Desc] IS NULL AND e.[End_Event_Desc] IS NULL))
	);

OPEN cur_Tracking_WorkFlow;
FETCH NEXT FROM cur_Tracking_WorkFlow INTO @Msg;
WHILE @@FETCH_STATUS = 0
BEGIN
	PRINT 'MISSING ROW: Tracking_WorkFlow (' + @Msg + ')';
	FETCH NEXT FROM cur_Tracking_WorkFlow INTO @Msg;
END;
CLOSE cur_Tracking_WorkFlow;
DEALLOCATE cur_Tracking_WorkFlow;

DROP TABLE #Expected_Tracking_WorkFlow;
GO

-------------------------------------------------------------------------------
-- mySobek_User_Item_Link_Relationship
-------------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#Expected_mySobek_User_Item_Link_Relationship') IS NOT NULL DROP TABLE #Expected_mySobek_User_Item_Link_Relationship;

CREATE TABLE #Expected_mySobek_User_Item_Link_Relationship (
	[RelationshipID] int NULL,
	[RelationshipLabel] nvarchar(50) NULL,
	[Include_In_Results] bit NULL
);

INSERT INTO #Expected_mySobek_User_Item_Link_Relationship ([RelationshipID], [RelationshipLabel], [Include_In_Results]) VALUES
(1,N'Submittor',1),
(2,N'Author',1),
(3,N'Contributor',1),
(4,N'ANALYZED; NO RELATION',0),
(5,N'Thesis Advisor',1),
(6,N'Other',1);

DECLARE @Msg nvarchar(max);
DECLARE cur_mySobek_User_Item_Link_Relationship CURSOR LOCAL FAST_FORWARD FOR
	SELECT CONCAT('RelationshipID=', e.[RelationshipID], ', ', 'RelationshipLabel=', e.[RelationshipLabel], ', ', 'Include_In_Results=', e.[Include_In_Results])
	FROM #Expected_mySobek_User_Item_Link_Relationship e
	WHERE NOT EXISTS (
		SELECT 1 FROM dbo.mySobek_User_Item_Link_Relationship r
		WHERE (r.[RelationshipID] = e.[RelationshipID] OR (r.[RelationshipID] IS NULL AND e.[RelationshipID] IS NULL))
		  AND (r.[RelationshipLabel] = e.[RelationshipLabel] OR (r.[RelationshipLabel] IS NULL AND e.[RelationshipLabel] IS NULL))
		  AND (r.[Include_In_Results] = e.[Include_In_Results] OR (r.[Include_In_Results] IS NULL AND e.[Include_In_Results] IS NULL))
	);

OPEN cur_mySobek_User_Item_Link_Relationship;
FETCH NEXT FROM cur_mySobek_User_Item_Link_Relationship INTO @Msg;
WHILE @@FETCH_STATUS = 0
BEGIN
	PRINT 'MISSING ROW: mySobek_User_Item_Link_Relationship (' + @Msg + ')';
	FETCH NEXT FROM cur_mySobek_User_Item_Link_Relationship INTO @Msg;
END;
CLOSE cur_mySobek_User_Item_Link_Relationship;
DEALLOCATE cur_mySobek_User_Item_Link_Relationship;

DROP TABLE #Expected_mySobek_User_Item_Link_Relationship;
GO

PRINT 'Seed data comparison complete.';
GO
