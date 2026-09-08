-- Adds the two Builder modules needed for "GCS Hybrid" file system mode: staging existing
-- GCS-hosted files back to local disk before reprocessing an item, and pushing master
-- image files to GCS (deleting the local scratch copy) once the rest of the item's
-- processing has finished. Both modules self-no-op when File System Mode is "Local", so
-- these rows are safe to enable unconditionally in every deployment.
--
-- PushMasterFilesToGcsModule's Order (315) deliberately sits after every other item-level
-- module in this set -- SaveToSolrLuceneModule_v5 (280) needs local page .txt files to
-- index without a wasted GCS round-trip, and CleanWebResourceFolderModule (290) /
-- CreateStaticVersionModule (300) need *.mets.bak/original.mets.xml/citation_mets.xml
-- still present at the top of the resource folder so they can relocate them into the
-- Backup_Files subfolder before this module's (non-recursive) GCS-only sweep would
-- otherwise delete them. It still runs before ClearEngineCacheModule (320), so the
-- engine's cache is invalidated only once the item's storage migration is fully done.

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.StageResourceFilesLocallyModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Stage existing GCS-hosted files locally before reprocessing, in GCS Hybrid mode', 'SobekCM.Builder_Library.Modules.Items.StageResourceFilesLocallyModule', 'true', 5);
end;
GO

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.PushMasterFilesToGcsModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Upload master/derivative image files to GCS and remove the local scratch copy, in GCS Hybrid mode', 'SobekCM.Builder_Library.Modules.Items.PushMasterFilesToGcsModule', 'true', 315);
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

	if ( NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Restricted URL Expiration Minutes' and Extension_Code is null))
	begin
		insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, [Hidden], Reserved, Help )
		values ( 'GCS Restricted URL Expiration Minutes', '15', 'System / Server Settings', 'Server Settings', 0, 2, 'How long (in minutes) a signed URL stays valid for a file on an IP- or user-group-restricted (but not dark) item. Deliberately much shorter than GCS Signed URL Expiration Minutes, since a signed URL is a bearer token that works for anyone holding it once handed out. Only used when File System Mode is "GCS Hybrid" or "GCS Full".' );
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

-- Re-adds 'Can Submit Edit Online' as a genuinely distinct flag from 'Can Submit Items Online':
-- the existing 'Can Submit Items Online' setting only ever gated creating a brand-new item/volume
-- (New_Item, New_TEI_Item, etc). This one gates ANY change to an EXISTING item -- metadata,
-- behaviors, permissions, deletion, and so on. The key existed in schema history back to v4 but
-- was dropped from the live schema at some point and was never wired into any C# code, so this
-- check guards against re-inserting a duplicate on a DB that still has the old row.
--
-- Also adds 'Disabled Online Changes Link': when a user reaches a mySobek viewer that would let
-- them submit a new item or edit an existing one while the relevant flag above is off, they're
-- now redirected here instead of just seeing an inline disabled message -- previously the flag
-- only hid the menu link, it never actually stopped someone who had a direct URL. Left blank,
-- the redirect falls back to the site's main home page instead of a configured link.

if not exists (select 1 from dbo.SobekCM_Settings where Setting_Key = 'Can Submit Edit Online')
begin
	insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
	values ( 'Can Submit Edit Online', 'true', 'System / Server Settings', 'Disable Behavior', 0, 2, 'Flag dictates if users can make ANY changes to an existing item online (metadata, behaviors, permissions, deletion, etc) -- separate from submitting a brand-new item, which is controlled by "Can Submit Items Online".', 'true|false' );
end
else
begin
	update dbo.SobekCM_Settings set Reserved=2, TabPage='System / Server Settings', Heading='Disable Behavior' where Setting_Key='Can Submit Edit Online';
end;
GO

if not exists (select 1 from dbo.SobekCM_Settings where Setting_Key = 'Disabled Online Changes Link')
begin
	insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help )
	values ( 'Disabled Online Changes Link', '', 'System / Server Settings', 'Disable Behavior', 0, 2, 'When set, a user who reaches a mySobek viewer that would let them submit a new item or edit an existing one -- while online submissions/edits are disabled -- is redirected here instead of just being shown a disabled message. Leave blank to fall back to the site''s main home page.' );
end
else
begin
	update dbo.SobekCM_Settings set Reserved=2, TabPage='System / Server Settings', Heading='Disable Behavior' where Setting_Key='Disabled Online Changes Link';
end;
GO

update dbo.SobekCM_Settings set Reserved=2, TabPage='System / Server Settings', Heading='Disable Behavior' where Setting_Key='Can Submit Items Online';
GO

UPDATE SobekCM_Settings SET Options = 'Built-In IIPImage|None|GCS Scratch' WHERE Setting_Key = 'JPEG2000 Server Type';
GO


-- Adds the SaveStructMapModule Builder module, which restores the structure map (physical,
-- download, and open-textbook division trees) and main thumbnail reference onto a
-- METADATA_UPDATE package from the item's currently-published METS -- a METADATA_UPDATE
-- submission carries only updated descriptive metadata, with no structMap of its own, so
-- without this the item's file list and thumbnail would otherwise get wiped out on save.
-- It then immediately re-saves the merged item back over the incoming METS file, in the
-- processing folder it was just read from. No-ops immediately for any package that isn't a
-- METADATA_UPDATE, so this row is safe to enable unconditionally in every deployment.
--
-- Order 155 is deliberate: it must run BEFORE MoveFilesToImageServerModule (160), so the METS
-- file that module renames to "recd_....mets.bak" and carries into the final image-server
-- folder is already the complete, merged one -- ReloadMetsAndBasicDbInfoModule (170)
-- unconditionally re-reads whatever METS is on disk at that point and wholesale-replaces the
-- in-memory item, so running this module any later than 160 would just get silently
-- discarded by that re-read. Everything in between (180-230) doesn't matter either way, since
-- all of those already short-circuit for a METADATA_UPDATE package in code (source control
-- commit 6edf9c61, "Early exit on many builder modules when metadata update").

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.SaveStructMapModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Restore the structure map and main thumbnail from the active METS, for metadata-only updates', 'SobekCM.Builder_Library.Modules.Items.SaveStructMapModule', 'true', 155);
end;
GO


-- Adds a second flag, AdditionalWork_MetadataOnly, alongside the existing AdditionalWorkNeeded
-- flag on SobekCM_Item. AdditionalWorkNeeded alone tells the Builder "look at this item again"
-- for any reason (new pages, changed permissions, embargo expiration, etc); the new flag narrows
-- that down to "the only outstanding work is metadata", so a Builder module can later tell a
-- metadata-only pass apart from a full reprocessing pass. Defaults to 'false' so every existing
-- row -- and every existing caller that never sets it -- is unaffected.
--
-- SobekCM_Set_Item_Visibility and Admin_Unembargo_Items_Past_Embargo_Date both set
-- AdditionalWorkNeeded directly (not through the proc below), and both are updated here to set
-- AdditionalWork_MetadataOnly = 'true' at the same time -- visibility/embargo changes don't
-- touch any files, so the outstanding work they flag is metadata-only.

if ( NOT EXISTS (select * from sys.columns where Name = N'AdditionalWork_MetadataOnly' and Object_ID = Object_ID(N'SobekCM_Item')))
begin
	alter table dbo.SobekCM_Item add AdditionalWork_MetadataOnly bit not null default('false');
end;
GO

-- Now takes a second flag to set alongside the existing one. Clearing AdditionalWorkNeeded
-- (@newflag = 0) always clears AdditionalWork_MetadataOnly too, regardless of @metadataOnly --
-- a cleared item has no outstanding work of any kind, so the two flags can never end up with
-- AdditionalWorkNeeded false and AdditionalWork_MetadataOnly true.
ALTER procedure [dbo].[SobekCM_Update_Additional_Work_Needed_Flag]
	@itemid int,
	@newflag bit,
	@metadataOnly bit
as
begin
	if ( @newflag = 0 )
	begin
		update SobekCM_Item set AdditionalWorkNeeded = @newflag, AdditionalWork_MetadataOnly = 0 where ItemID = @itemid;
	end
	else
	begin
		update SobekCM_Item set AdditionalWorkNeeded = @newflag, AdditionalWork_MetadataOnly = @metadataOnly where ItemID = @itemid;
	end;
end;
GO

-- Also return the new metadata-only flag alongside the existing item info, so the Builder can
-- tell a metadata-only pass apart from a full reprocessing pass.
ALTER procedure [dbo].[SobekCM_Get_Items_Needing_Aditional_Work]
as
begin

	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Return the bibid, vid, primary key, and metadata-only flag to the items which are flagged
	select G.BibID, I.VID, I.ItemID, I.AdditionalWork_MetadataOnly
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID )
	    and ( I.AdditionalWorkNeeded = 'true' )
	order by BibID, VID;
end;
GO

-- Also flags visibility/embargo changes as metadata-only work, alongside the existing
-- AdditionalWorkNeeded = 'true'. Full body reproduced below since SQL Server requires the
-- complete procedure text on ALTER -- the only change from the prior definition is the added
-- AdditionalWork_MetadataOnly = 'true' in the "Update the main item table" step.
ALTER PROCEDURE [dbo].[SobekCM_Set_Item_Visibility]
	@ItemID int,
	@IpRestrictionMask smallint,
	@DarkFlag bit,
	@EmbargoDate datetime,
	@User varchar(255)
AS
BEGIN

	-- Build the note text and value
	declare @noteText varchar(200);
	set @noteText = '';

	-- Set the embargo date
	if ( @EmbargoDate is null )
	begin
		if ( exists ( select 1 from Tracking_Item where ItemID=@ItemID and EmbargoEnd is not null ) )
		begin
			update Tracking_Item set EmbargoEnd=null where ItemID=@ItemID;

			set @noteText = 'Embargo date removed.  ';
		end;
	end
	else
	begin
		if ( exists ( select 1 from Tracking_Item where ItemID=@ItemID ) )
		begin
			update Tracking_Item set EmbargoEnd=@EmbargoDate where ItemID=@ItemID;
		end
		else
		begin
			insert into Tracking_Item ( ItemID, Original_EmbargoEnd, EmbargoEnd )
			values ( @ItemID, @EmbargoDate, @EmbargoDate );
		end;

		set @noteText = 'Embargo date of ' + convert(varchar(20), @EmbargoDate, 102) + '.  ';
	end;

	-- Set the workflow id
	declare @workflowId int;
	set @workflowId = 34;
	if ( @IpRestrictionMask < 0 )
		set @workflowId = 35;
	if ( @IpRestrictionMask < 0 )
		set @workflowId = 36;
	if ( @DarkFlag = 'true' )
	begin
		set @workflowId = 35;
		set @noteText = @noteText + 'Item made dark.';
	end;

	-- Update the main item table (and set for the builder to review this)
	update SobekCM_Item
	set IP_Restriction_Mask = @IpRestrictionMask, Dark = @DarkFlag, AdditionalWorkNeeded = 'true', AdditionalWork_MetadataOnly = 'true'
	where ItemID=@ItemID;

	insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, DateStarted )
	values ( @ItemID, @workflowId, getdate(), @User, @noteText, getdate() );

	-- If this is being made public, set the public data
	if ( ( @DarkFlag = 'false' ) and ( @IpRestrictionMask >= 0 ) )
	begin
		update SobekCM_Item
		set MadePublicDate = coalesce(MadePublicDate, getdate())
		where ItemID=@ItemID;
	end;
END;
GO

-- Also flags bulk-unembargoed items as metadata-only work, alongside the existing
-- AdditionalWorkNeeded = 'true'. Full body reproduced below since SQL Server requires the
-- complete procedure text on ALTER -- the only change from the prior definition is the added
-- AdditionalWork_MetadataOnly = 'true' in the "Actually mark the items as unembargoed" step.
ALTER PROCEDURE [dbo].[Admin_Unembargo_Items_Past_Embargo_Date]
	@subject_line varchar(500),
	@email_message varchar(max),
	@send_email bit
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	SET NOCOUNT ON;

	-- Get the items that need to be processed
	select I.ItemID, G.BibID, I.VID, CONVERT(nvarchar(10), T.EmbargoEnd, 102) as EmbargoEnd, substring(I.Title, 4, 1000) as Title, substring(I.Author, 4, 1000) as Author
	into #Unembargo_Items
	from SobekCM_Item I, Tracking_Item T, SobekCM_Item_Group G
	where ( I.ItemID=T.ItemID )
	    and ( ( I.IP_Restriction_Mask <> 0 ) or ( I.Dark = 'true' ) )
	    and ( T.EmbargoEnd < getdate() )
	    and ( I.GroupID = G.GroupID );

	-- One row per (item, owning-aggregation-contact-email) pair, with the HTML blurb for that item.
	-- Title carried through here so the next step can order the concatenation by it, matching the original cursor's "order by Title".
	select distinct U.ItemID, U.Title, A.ContactEmail,
	              '<br /><br /><i>' + U.Title + '</i>, by ' + U.Author + ' ( ' + U.BibID + ':' + U.VID + ' ) - ' + U.EmbargoEnd as ItemBlurb
	into #Item_Aggregation_Emails
	from #Unembargo_Items U inner join
	          SobekCM_Item_Aggregation_Item_Link L on L.ItemID = U.ItemID and L.impliedLink = 'false' inner join
	          SobekCM_Item_Aggregation A on A.AggregationID = L.AggregationID and len(A.ContactEmail) > 0;

	-- Collapse to one row per contact email, items concatenated in Title order (matches the original cursor's "order by Title").
	-- NOTE FOR POSTGRESQL PORT: WITHIN GROUP (ORDER BY ...) is SQL-Server-only syntax.
	-- PostgreSQL equivalent: string_agg(ItemBlurb, '' ORDER BY Title ASC)
	select ContactEmail as EmailAddress, string_agg(ItemBlurb, '') WITHIN GROUP (ORDER BY Title ASC) as ItemList
	into #EmailPrep
	from #Item_Aggregation_Emails
	group by ContactEmail;

	-- Actually mark the items as unembargoed next
	update SobekCM_Item
	set Dark='false', IP_Restriction_Mask=0, AdditionalWorkNeeded='true', AdditionalWork_MetadataOnly='true'
	where exists ( select * from #Unembargo_Items T where T.ItemID=SobekCM_Item.ItemID );

	-- Also add a workflow progress for this
	insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, DateStarted )
	select ItemID, 34, getdate(), 'Builder Service', 'Automatically unembargoed ( original unembargo date of ' + EmbargoEnd + ' )', getdate()
	from #Unembargo_Items;

	-- Send emails via database email?
	if ( @send_email = 'true' )
	begin
		declare @emailaddress varchar(255);
		declare @itemlist varchar(max);
		declare @emailbody varchar(max);

		declare email_cursor cursor for
		select EmailAddress, ItemList
		from #EmailPrep;

		open email_cursor;
		fetch next from email_cursor into @emailaddress, @itemlist;

		while ( @@FETCH_STATUS = 0 )
		begin
			set @emailbody = REPLACE(@email_message, '{0}', @itemlist);
			exec [SobekCM_Send_Email] @emailaddress, @subject_line, @emailbody, null, null, 'true', 'false', -1, -1;
			fetch next from email_cursor into @emailaddress, @itemlist;
		end;
		close email_cursor;
		deallocate email_cursor;
	end;

	-- Return the list of items unembargoed
	select * from #Unembargo_Items;

	-- Return the email information as well
	select * from #EmailPrep;

	-- Drop the temporary tables
	drop table #Unembargo_Items;
	drop table #Item_Aggregation_Emails;
	drop table #EmailPrep;
END;
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