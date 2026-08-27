-- Adds the two Builder modules needed for "GCS Hybrid" file system mode: staging existing
-- GCS-hosted files back to local disk before reprocessing an item, and pushing master
-- image files to GCS (deleting the local scratch copy) once the rest of the item's
-- processing has finished. Both modules self-no-op when File System Mode is "Local", so
-- these rows are safe to enable unconditionally in every deployment.

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.StageResourceFilesLocallyModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Stage existing GCS-hosted files locally before reprocessing, in GCS Hybrid mode', 'SobekCM.Builder_Library.Modules.Items.StageResourceFilesLocallyModule', 'true', 5);
end;
GO

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.PushMasterFilesToGcsModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Upload master/derivative image files to GCS and remove the local scratch copy, in GCS Hybrid mode', 'SobekCM.Builder_Library.Modules.Items.PushMasterFilesToGcsModule', 'true', 270);
end;
GO


-- Adds per-extension settings storage, reusing the existing generic SobekCM_Settings
-- key/value table rather than a new table. Extension_Code is nullable so every existing
-- row (Extension_Code IS NULL) is unaffected. Extension-owned rows use namespaced
-- Setting_Key values (e.g. 'OIDC|{Provider_Code}|ClientSecret') so a customer can
-- eventually run more than one instance of the same provider type.
if ( NOT EXISTS (select * from sys.columns where Name = N'Extension_Code' and Object_ID = Object_ID(N'SobekCM_Settings')))
begin
	alter table dbo.SobekCM_Settings add Extension_Code nvarchar(50) NULL;
end;
GO

-- Gets all the settings rows belonging to a single extension, by extension code.
-- Deliberately a sibling of SobekCM_Get_Settings, not a modification of it, so the
-- existing settings loader (which pulls every unfiltered row) is unaffected.
IF object_id('SobekCM_Get_Extension_Settings') IS NULL EXEC ('create procedure dbo.SobekCM_Get_Extension_Settings as select 1;');
GO

ALTER PROCEDURE [dbo].[SobekCM_Get_Extension_Settings]
	@Extension_Code nvarchar(50)
AS
BEGIN

	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select Setting_Key, Setting_Value
	from SobekCM_Settings
	where Extension_Code = @Extension_Code;

END;
GO

-- Sets a single setting value scoped to one extension, by extension code and key.
-- Adds a new row if this is a new key for that extension, otherwise updates the
-- existing value. Sibling of SobekCM_Set_Setting_Value, same upsert shape.
IF object_id('SobekCM_Set_Extension_Setting_Value') IS NULL EXEC ('create procedure dbo.SobekCM_Set_Extension_Setting_Value as select 1;');
GO

ALTER PROCEDURE [dbo].[SobekCM_Set_Extension_Setting_Value]
	@Extension_Code nvarchar(50),
	@Setting_Key varchar(255),
	@Setting_Value varchar(max)
AS
BEGIN

	if ( ( select COUNT(*) from SobekCM_Settings where Extension_Code = @Extension_Code and Setting_Key = @Setting_Key ) > 0 )
	begin
		update SobekCM_Settings set Setting_Value = @Setting_Value where Extension_Code = @Extension_Code and Setting_Key = @Setting_Key;
	end
	else
	begin
		insert into SobekCM_Settings ( Setting_Key, Setting_Value, [Hidden], Reserved, Extension_Code )
		values ( @Setting_Key, @Setting_Value, 1, 0, @Extension_Code );
	end;

END;
GO

-- Excludes extension-owned rows (Extension_Code IS NOT NULL) from the general settings
-- loader, which previously selected every row in SobekCM_Settings unfiltered. Extension
-- rows are meant to be read only through SobekCM_Get_Extension_Settings, scoped to one
-- extension at a time -- without this filter, the Builder's Get_Settings() call (which
-- invokes this procedure with no @IncludeAdminViewInfo value, hitting the "else" branch
-- below) would receive every extension's settings flattened into its general settings
-- dictionary, including credentials such as the oidc_auth/saml_auth extensions' ClientSecret
-- values. The @IncludeAdminViewInfo = 'true' branch already excluded them incidentally
-- (extension rows are always inserted with Hidden = 1, see SobekCM_Set_Extension_Setting_Value
-- above), but that was never a deliberate protection, so both branches get the explicit filter.
ALTER PROCEDURE [dbo].[SobekCM_Get_Settings]
	@IncludeAdminViewInfo bit
AS
begin

	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Get all the standard SobekCM settings
	if ( @IncludeAdminViewInfo = 'true' )
	begin
		select Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions
		from SobekCM_Settings
		where Hidden = 'false'
		    and Extension_Code is null
		order by TabPage, Heading, Setting_Key;
	end
	else
	begin
		select Setting_Key, Setting_Value
		from SobekCM_Settings
		where Extension_Code is null;
	end;

	-- Return all the metadata search fields
	select MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse,
	              coalesce(SolrCode_Facets,'') as SolrCode_Facets,
		       coalesce(SolrCode_Display,'') as SolrCode_Display,
		       coalesce(LegacySolrCode,'') as LegacySolrCode
	from SobekCM_Metadata_Types
	order by DisplayTerm;

	-- Return all the possible workflow types
	select WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc
	from Tracking_WorkFlow;

	-- Return all the possible disposition options
	select DispositionID, DispositionFuture, DispositionPast, DispositionNotes
	from Tracking_Disposition_Type;

	-- Always return all the incoming folders
	select IncomingFolderId, NetworkFolder, ErrorFolder, ProcessingFolder, Perform_Checksum_Validation, Archive_TIFF, Archive_All_Files,
	              Allow_Deletes, Allow_Folders_No_Metadata, Allow_Metadata_Updates, FolderName, Can_Move_To_Content_Folder, BibID_Roots_Restrictions,
	              F.ModuleSetID, S.SetName
	from SobekCM_Builder_Incoming_Folders F left outer join
	          SobekCM_Builder_Module_Set S on F.ModuleSetID=S.ModuleSetID;

	-- Return all the non-scheduled type modules
	select M.ModuleID, M.[Assembly], M.Class, M.ModuleDesc, M.Argument1, M.Argument2, M.Argument3, M.[Enabled], S.ModuleSetID, S.SetName, S.[Enabled] as SetEnabled, T.TypeAbbrev, T.TypeDescription
	from SobekCM_Builder_Module M, SobekCM_Builder_Module_Set S, SobekCM_Builder_Module_Type T
	where M.ModuleSetID = S.ModuleSetID
	    and S.ModuleTypeID = T.ModuleTypeID
	    and T.TypeAbbrev <> 'SCHD'
	order by TypeAbbrev, S.SetOrder, M.[Order];


	-- Return all the scheduled type modules, with the schedule and the last run info
	with last_run_cte (ModuleScheduleID, LastRun) as
	(
		select ModuleScheduleID, MAX([Timestamp])
		from SobekCM_Builder_Module_Scheduled_Run
		group by ModuleScheduleID
	)
	-- Return all the scheduled type modules, along with information on when it was last run
	select M.ModuleID, M.[Assembly], M.Class, M.ModuleDesc, M.Argument1, M.Argument2, M.Argument3, M.[Enabled], S.ModuleSetID, S.SetName, S.[Enabled] as SetEnabled, T.TypeAbbrev, T.TypeDescription, C.ModuleScheduleID, C.[Enabled] as ScheduleEnabled, C.DaysOfWeek, C.TimesOfDay, L.LastRun
	from SobekCM_Builder_Module M inner join
		   SobekCM_Builder_Module_Set S on M.ModuleSetID = S.ModuleSetID inner join
		   SobekCM_Builder_Module_Type T on S.ModuleTypeID = T.ModuleTypeID inner join
		   SobekCM_Builder_Module_Schedule C on C.ModuleSetID = S.ModuleSetID left outer join
		   last_run_cte L on L.ModuleScheduleID = C.ModuleScheduleID
	where T.TypeAbbrev = 'SCHD'
	order by TypeAbbrev, S.SetOrder, M.[Order];

	-- Return all the item viewer config information
	select ItemViewTypeID, ViewType, [Order], DefaultView, MenuOrder
	from SobekCM_item_Viewer_Types
	order by ViewType;

	-- Return all the information about the extensions from the database
	select ExtensionID, Code, Name, CurrentVersion, IsEnabled, EnabledDate, LicenseKey, UpgradeUrl, LatestVersion
	from SobekCM_Extension
	order by Code;

end;
GO



-- Adds the three settings needed to enable "GCS Hybrid" file system mode: which mode is
-- active, which GCS bucket master files go to, and how long signed URLs to GCS-hosted
-- files remain valid. The service-account JSON key file path is deliberately NOT a
-- setting -- it's a fixed convention off Base_Directory (config\gcs-service-account.json),
-- kept out of the DB since it's a much higher-stakes credential than the other settings
-- stored here. Every existing deployment defaults to 'Local' and is unaffected until an
-- admin explicitly switches File System Mode.

	if ( NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'File System Mode' and Extension_Code is null))
	begin
		insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, [Hidden], Reserved, Help, Options )
		values ( 'File System Mode', 'Local', 'System / Server Settings', 'Server Settings', 0, 2, 'Determines where digital resource files are stored/served from. "Local" uses the on-disk pairtree structure. "GCS Hybrid" stores master image files in Google Cloud Storage while keeping METS/marc.xml/thumbnails locally as well.', 'Local|GCS Hybrid' );
	end;

	if ( NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Bucket Name' and Extension_Code is null))
	begin
		insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, [Hidden], Reserved, Help )
		values ( 'GCS Bucket Name', '', 'System / Server Settings', 'Server Settings', 0, 2, 'Name of the Google Cloud Storage bucket used when File System Mode is "GCS Hybrid".' );
	end;

	if ( NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Signed URL Expiration Minutes' and Extension_Code is null))
	begin
		insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, [Hidden], Reserved, Help )
		values ( 'GCS Signed URL Expiration Minutes', '240', 'System / Server Settings', 'Server Settings', 0, 2, 'How long (in minutes) a signed URL to a GCS-hosted file stays valid before expiring. Only used when File System Mode is "GCS Hybrid".' );
	end;

GO

update SobekCM_Settings 
set Setting_Key='System Base Code', Reserved=3, Help='Base abbreviation for this instance which is immutable once the site is running.\n\nExamples: UFDC, dLOC, SOBEK, etc..'
where Setting_Key='System Base Abbreviation';
GO


update SobekCM_Settings 
set Reserved=3
where Setting_Key in ('Builder Last Message', 'Builder Last Run Finished', 'Builder Version');
GO

update SobekCM_Settings
set Reserved=2, Setting_Value='STANDARD OPERATION', Options='STANDARD OPERATION|PAUSE REQUESTED'
where Setting_Key = 'Builder Operation Flag';
GO


if (NOT EXISTS ( Select 1 from SobekCM_Item_Viewer_Types where ViewType = 'AUDIO' ))
begin
	insert into SobekCM_Item_Viewer_Types ( ViewType, [Order], DefaultView, MenuOrder)
	values ( 'AUDIO', 8, 'true', 108);
end;
GO


/**************************************************************************/
/**                                                                      **/
/**   Update Database Version                                            **/
/**                                                                      **/
/**************************************************************************/

-- Update the version number
if (( select count(*) from SobekCM_Database_Version ) = 0 )
begin
	insert into SobekCM_Database_Version ( Major_Version, Minor_Version, Release_Phase )
	values ( 5, 1, '0' );
end
else
begin
	update SobekCM_Database_Version
	set Major_Version=5, Minor_Version=1, Release_Phase='0';
end;
GO