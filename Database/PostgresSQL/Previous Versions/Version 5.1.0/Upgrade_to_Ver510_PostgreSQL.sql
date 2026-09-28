/**
Upgrade_to_Ver510_PostgreSQL.sql  - OpenSobek

Takes an existing 5.0.1 PostgreSQL database and brings it up to Version 5.1.0.

 PostgreSQL port of Upgrade_to_Ver510.sql -- see that file for the full rationale
 behind each change. If your version is older than 5.0.1 you will need to run
 Upgrade_to_Ver501_PostgreSQL.sql first.

 */

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

DO $$
BEGIN
  IF (select count(*) from SobekCM_Builder_Module where Class = 'SobekCM.Builder_Library.Modules.Items.StageResourceFilesLocallyModule') = 0 THEN
    insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, Enabled, "Order")
    values (3, 'Stage existing GCS-hosted files locally before reprocessing, in GCS Hybrid mode', 'SobekCM.Builder_Library.Modules.Items.StageResourceFilesLocallyModule', true, 5);
  END IF;
END $$;

DO $$
BEGIN
  IF (select count(*) from SobekCM_Builder_Module where Class = 'SobekCM.Builder_Library.Modules.Items.PushMasterFilesToGcsModule') = 0 THEN
    insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, Enabled, "Order")
    values (3, 'Upload master/derivative image files to GCS and remove the local scratch copy, in GCS Hybrid mode', 'SobekCM.Builder_Library.Modules.Items.PushMasterFilesToGcsModule', true, 315);
  END IF;
END $$;


-- Adds per-extension settings storage, reusing the existing generic SobekCM_Settings
-- key/value table rather than a new table. Extension_Code is nullable so every existing
-- row (Extension_Code IS NULL) is unaffected. Extension-owned rows use namespaced
-- Setting_Key values (e.g. 'OIDC|{Provider_Code}|ClientSecret') so a customer can
-- eventually run more than one instance of the same provider type.
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_name = 'sobekcm_settings' AND column_name = 'extension_code'
  ) THEN
    ALTER TABLE SobekCM_Settings ADD COLUMN Extension_Code varchar(50) NULL;
  END IF;
END $$;

-- Gets all the settings rows belonging to a single extension, by extension code.
-- Deliberately a sibling of SobekCM_Get_Settings, not a modification of it, so the
-- existing settings loader (which pulls every unfiltered row) is unaffected.
CREATE OR REPLACE FUNCTION SobekCM_Get_Extension_Settings(
	p_Extension_Code varchar(50)
)
RETURNS TABLE (
	Setting_Key varchar(255),
	Setting_Value text
)
LANGUAGE sql
AS $$
	select Setting_Key, Setting_Value
	from SobekCM_Settings
	where Extension_Code = p_Extension_Code;
$$;

-- Sets a single setting value scoped to one extension, by extension code and key.
-- Adds a new row if this is a new key for that extension, otherwise updates the
-- existing value. Sibling of SobekCM_Set_Setting_Value, same upsert shape.
CREATE OR REPLACE FUNCTION SobekCM_Set_Extension_Setting_Value(
	p_Extension_Code varchar(50),
	p_Setting_Key varchar(255),
	p_Setting_Value text
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if ( ( select COUNT(*) from SobekCM_Settings where Extension_Code = p_Extension_Code and Setting_Key = p_Setting_Key ) > 0 ) then
		update SobekCM_Settings set Setting_Value = p_Setting_Value where Extension_Code = p_Extension_Code and Setting_Key = p_Setting_Key;
	else
		insert into SobekCM_Settings ( Setting_Key, Setting_Value, Hidden, Reserved, Extension_Code )
		values ( p_Setting_Key, p_Setting_Value, true, 0, p_Extension_Code );
	end if;
END;
$$;

-- Excludes extension-owned rows (Extension_Code IS NOT NULL) from the general settings
-- loader, which previously selected every row in SobekCM_Settings unfiltered. Extension
-- rows are meant to be read only through SobekCM_Get_Extension_Settings, scoped to one
-- extension at a time -- without this filter, the Builder's Get_Settings() call (which
-- invokes this function with no p_IncludeAdminViewInfo value, hitting the "else" branch
-- below) would receive every extension's settings flattened into its general settings
-- dictionary, including credentials such as the oidc_auth/saml_auth extensions' ClientSecret
-- values. The p_IncludeAdminViewInfo = true branch already excluded them incidentally
-- (extension rows are always inserted with Hidden = true, see SobekCM_Set_Extension_Setting_Value
-- above), but that was never a deliberate protection, so both branches get the explicit filter.
CREATE OR REPLACE FUNCTION SobekCM_Get_Settings(
	p_IncludeAdminViewInfo boolean,
	OUT cur_settings refcursor,
	OUT cur_metadata_fields refcursor,
	OUT cur_workflows refcursor,
	OUT cur_dispositions refcursor,
	OUT cur_folders refcursor,
	OUT cur_modules refcursor,
	OUT cur_scheduled_modules refcursor,
	OUT cur_viewer_types refcursor,
	OUT cur_extensions refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ( p_IncludeAdminViewInfo ) then
		OPEN cur_settings FOR
		select Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions
		from SobekCM_Settings
		where Hidden = 'false'
		    and Extension_Code is null
		order by TabPage, Heading, Setting_Key;
	else
		OPEN cur_settings FOR
		select Setting_Key, Setting_Value
		from SobekCM_Settings
		where Extension_Code is null;
	end if;

	OPEN cur_metadata_fields FOR
	select MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse,
	       coalesce(SolrCode_Facets,'') as SolrCode_Facets,
		   coalesce(SolrCode_Display,'') as SolrCode_Display,
		   coalesce(LegacySolrCode,'') as LegacySolrCode
	from SobekCM_Metadata_Types
	order by DisplayTerm;

	OPEN cur_workflows FOR
	select WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc
	from Tracking_WorkFlow;

	OPEN cur_dispositions FOR
	select DispositionID, DispositionFuture, DispositionPast, DispositionNotes
	from Tracking_Disposition_Type;

	OPEN cur_folders FOR
	select IncomingFolderId, NetworkFolder, ErrorFolder, ProcessingFolder, Perform_Checksum_Validation, Archive_TIFF, Archive_All_Files,
		   Allow_Deletes, Allow_Folders_No_Metadata, Allow_Metadata_Updates, FolderName, Can_Move_To_Content_Folder, BibID_Roots_Restrictions,
		   F.ModuleSetID, S.SetName
	from SobekCM_Builder_Incoming_Folders F left outer join
	     SobekCM_Builder_Module_Set S on F.ModuleSetID=S.ModuleSetID;

	OPEN cur_modules FOR
	select M.ModuleID, M.Assembly, M.Class, M.ModuleDesc, M.Argument1, M.Argument2, M.Argument3, M.Enabled, S.ModuleSetID, S.SetName, S.Enabled as SetEnabled, T.TypeAbbrev, T.TypeDescription
	from SobekCM_Builder_Module M, SobekCM_Builder_Module_Set S, SobekCM_Builder_Module_Type T
	where M.ModuleSetID = S.ModuleSetID
	  and S.ModuleTypeID = T.ModuleTypeID
	  and T.TypeAbbrev <> 'SCHD'
	order by TypeAbbrev, S.SetOrder, M."Order";

	OPEN cur_scheduled_modules FOR
	with last_run_cte ( ModuleScheduleID, LastRun) as
	(
		select ModuleScheduleID, MAX("Timestamp")
		from SobekCM_Builder_Module_Scheduled_Run
		group by ModuleScheduleID
	)
	select M.ModuleID, M.Assembly, M.Class, M.ModuleDesc, M.Argument1, M.Argument2, M.Argument3, M.Enabled, S.ModuleSetID, S.SetName, S.Enabled as SetEnabled, T.TypeAbbrev, T.TypeDescription, C.ModuleScheduleID, C.Enabled as ScheduleEnabled, C.DaysOfWeek, C.TimesOfDay, L.LastRun
	from SobekCM_Builder_Module M inner join
		 SobekCM_Builder_Module_Set S on M.ModuleSetID = S.ModuleSetID inner join
		 SobekCM_Builder_Module_Type T on S.ModuleTypeID = T.ModuleTypeID inner join
		 SobekCM_Builder_Module_Schedule C on C.ModuleSetID = S.ModuleSetID left outer join
		 last_run_cte L on L.ModuleScheduleID = C.ModuleScheduleID
	where T.TypeAbbrev = 'SCHD'
	order by TypeAbbrev, S.SetOrder, M."Order";

	OPEN cur_viewer_types FOR
	select ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder
	from SobekCM_item_Viewer_Types
	order by ViewType;

	OPEN cur_extensions FOR
	select ExtensionID, Code, Name, CurrentVersion, IsEnabled, EnabledDate, LicenseKey, UpgradeUrl, LatestVersion
	from SobekCM_Extension
	order by Code;
END;
$$;


-- Adds the three settings needed to enable "GCS Hybrid" file system mode: which mode is
-- active, which GCS bucket master files go to, and how long signed URLs to GCS-hosted
-- files remain valid. The service-account JSON key file path is deliberately NOT a
-- setting -- it's a fixed convention off Base_Directory (config\gcs-service-account.json),
-- kept out of the DB since it's a much higher-stakes credential than the other settings
-- stored here. Every existing deployment defaults to 'Local' and is unaffected until an
-- admin explicitly switches File System Mode.

DO $$
BEGIN
  IF NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'File System Mode' and Extension_Code is null) THEN
    insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
    values ( 'File System Mode', 'Local', 'System / Server Settings', 'Server Settings', false, 2, 'Determines where digital resource files are stored/served from. "Local" uses the on-disk pairtree structure. "GCS Hybrid" stores master image files in Google Cloud Storage while keeping METS/marc.xml/thumbnails locally as well.', 'Local|GCS Hybrid' );
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Bucket Name' and Extension_Code is null) THEN
    insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help )
    values ( 'GCS Bucket Name', '', 'System / Server Settings', 'Server Settings', false, 2, 'Name of the Google Cloud Storage bucket used when File System Mode is "GCS Hybrid".' );
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Signed URL Expiration Minutes' and Extension_Code is null) THEN
    insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help )
    values ( 'GCS Signed URL Expiration Minutes', '240', 'System / Server Settings', 'Server Settings', false, 2, 'How long (in minutes) a signed URL to a GCS-hosted file stays valid before expiring. Only used when File System Mode is "GCS Hybrid".' );
  END IF;
END $$;

-- NOTE: the EXISTS check intentionally matches the key actually inserted below
-- ('GCS Restricted URL Expiration Minutes', without "Signed") -- the SQL Server version of
-- this script originally checked a key name that was never the one inserted ('GCS Restricted
-- Signed URL Expiration Minutes'), which would have thrown a primary key violation on a
-- second run since the guard could never see the row it had just created. Fixed here to
-- match Setting_Key to what SobekCM_Engine_Library\Settings\InstanceWide_Settings_Builder.cs
-- actually reads ("GCS Restricted URL Expiration Minutes").
DO $$
BEGIN
  IF NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Restricted URL Expiration Minutes' and Extension_Code is null) THEN
    insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help )
    values ( 'GCS Restricted URL Expiration Minutes', '15', 'System / Server Settings', 'Server Settings', false, 2, 'How long (in minutes) a signed URL stays valid for a file on an IP- or user-group-restricted (but not dark) item. Deliberately much shorter than GCS Signed URL Expiration Minutes, since a signed URL is a bearer token that works for anyone holding it once handed out. Only used when File System Mode is "GCS Hybrid" or "GCS Full".' );
  END IF;
END $$;

update SobekCM_Settings
set Setting_Key='System Base Code', Reserved=3, Help='Base abbreviation for this instance which is immutable once the site is running.\n\nExamples: UFDC, dLOC, SOBEK, etc..'
where Setting_Key='System Base Abbreviation';

update SobekCM_Settings
set Reserved=3
where Setting_Key in ('Builder Last Message', 'Builder Last Run Finished', 'Builder Version');

update SobekCM_Settings
set Reserved=2, Setting_Value='STANDARD OPERATION', Options='STANDARD OPERATION|PAUSE REQUESTED'
where Setting_Key = 'Builder Operation Flag';

DO $$
BEGIN
  IF NOT EXISTS (select 1 from SobekCM_Item_Viewer_Types where ViewType = 'AUDIO') THEN
    insert into SobekCM_Item_Viewer_Types ( ViewType, "Order", DefaultView, MenuOrder)
    values ( 'AUDIO', 8, true, 108);
  END IF;
END $$;

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

DO $$
BEGIN
  IF NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'Can Submit Edit Online') THEN
    insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
    values ( 'Can Submit Edit Online', 'true', 'System / Server Settings', 'Disable Behavior', false, 2, 'Flag dictates if users can make ANY changes to an existing item online (metadata, behaviors, permissions, deletion, etc) -- separate from submitting a brand-new item, which is controlled by "Can Submit Items Online".', 'true|false' );
  ELSE
    update SobekCM_Settings set Reserved=2, TabPage='System / Server Settings', Heading='Disable Behavior' where Setting_Key='Can Submit Edit Online';
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'Disabled Online Changes Link') THEN
    insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help )
    values ( 'Disabled Online Changes Link', '', 'System / Server Settings', 'Disable Behavior', false, 2, 'When set, a user who reaches a mySobek viewer that would let them submit a new item or edit an existing one -- while online submissions/edits are disabled -- is redirected here instead of just being shown a disabled message. Leave blank to fall back to the site''s main home page.' );
  ELSE
    update SobekCM_Settings set Reserved=2, TabPage='System / Server Settings', Heading='Disable Behavior' where Setting_Key='Disabled Online Changes Link';
  END IF;
END $$;

update SobekCM_Settings set Reserved=2, TabPage='System / Server Settings', Heading='Disable Behavior' where Setting_Key='Can Submit Items Online';

update SobekCM_Settings SET Options = 'Built-In IIPImage|None|GCS Scratch' WHERE Setting_Key = 'JPEG2000 Server Type';


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

DO $$
BEGIN
  IF (select count(*) from SobekCM_Builder_Module where Class = 'SobekCM.Builder_Library.Modules.Items.SaveStructMapModule') = 0 THEN
    insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, Enabled, "Order")
    values (3, 'Restore the structure map and main thumbnail from the active METS, for metadata-only updates', 'SobekCM.Builder_Library.Modules.Items.SaveStructMapModule', true, 155);
  END IF;
END $$;


-- Adds a second flag, AdditionalWork_MetadataOnly, alongside the existing AdditionalWorkNeeded
-- flag on SobekCM_Item. AdditionalWorkNeeded alone tells the Builder "look at this item again"
-- for any reason (new pages, changed permissions, embargo expiration, etc); the new flag narrows
-- that down to "the only outstanding work is metadata", so a Builder module can later tell a
-- metadata-only pass apart from a full reprocessing pass. Defaults to false so every existing
-- row -- and every existing caller that never sets it -- is unaffected.
--
-- SobekCM_Set_Item_Visibility and Admin_Unembargo_Items_Past_Embargo_Date both set
-- AdditionalWorkNeeded directly (not through the function below), and both are updated here to
-- flag AdditionalWork_MetadataOnly at the same time -- visibility/embargo changes don't touch
-- any files, so the outstanding work they flag is metadata-only. But in both functions, and in
-- the one below, that flag is only ever a claim of "this particular change is metadata-only" --
-- it must never overwrite an existing TRUE-AdditionalWorkNeeded/FALSE-AdditionalWork_MetadataOnly
-- row back to metadata-only, since that would silently downgrade an item that still has real,
-- non-metadata work outstanding (e.g. new pages attached) from a prior, unrelated flagging call.
-- All three writers therefore only ever narrow the flag (AND semantics): metadata-only sticks
-- only if every flagging call since the item was last cleared agreed it was metadata-only; a
-- single non-metadata-only call latches AdditionalWork_MetadataOnly to false until the next clear.

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_name = 'sobekcm_item' AND column_name = 'additionalwork_metadataonly'
  ) THEN
    ALTER TABLE SobekCM_Item ADD COLUMN AdditionalWork_MetadataOnly boolean NOT NULL DEFAULT false;
  END IF;
END $$;

-- Now takes a second flag to set alongside the existing one. Clearing AdditionalWorkNeeded
-- (p_newflag = false) always clears AdditionalWork_MetadataOnly too, regardless of
-- p_metadataOnly -- a cleared item has no outstanding work of any kind, so the two flags can
-- never end up with AdditionalWorkNeeded false and AdditionalWork_MetadataOnly true.
--
-- Setting p_newflag = true does NOT just overwrite AdditionalWork_MetadataOnly with
-- p_metadataOnly -- if the item is already flagged (AdditionalWorkNeeded was already true), the
-- new value is ANDed with whatever is already there, so a metadata-only call
-- (p_metadataOnly = true) can never clear a previously-flagged non-metadata-only need back to
-- "metadata only". Only a fresh flagging call (item wasn't previously flagged at all) takes
-- p_metadataOnly at face value. Expressed as a single UPDATE with a CASE rather than an IF/ELSE
-- so this can stay LANGUAGE sql, matching the function's prior form, rather than switching to
-- plpgsql just for the branch.
CREATE OR REPLACE FUNCTION SobekCM_Update_Additional_Work_Needed_Flag(
	p_itemid integer,
	p_newflag boolean,
	p_metadataOnly boolean
)
RETURNS void
LANGUAGE sql
AS $$
	update SobekCM_Item
	set AdditionalWorkNeeded = p_newflag,
	    AdditionalWork_MetadataOnly = CASE
	        WHEN NOT p_newflag THEN false
	        WHEN NOT AdditionalWorkNeeded THEN p_metadataOnly
	        ELSE (AdditionalWork_MetadataOnly AND p_metadataOnly)
	    END
	where ItemID = p_itemid;
$$;

-- Also return the new metadata-only flag alongside the existing item info, so the Builder can
-- tell a metadata-only pass apart from a full reprocessing pass.
CREATE OR REPLACE FUNCTION SobekCM_Get_Items_Needing_Aditional_Work()
RETURNS TABLE (
	BibID varchar(10),
	VID varchar(5),
	ItemID integer,
	AdditionalWork_MetadataOnly boolean
)
LANGUAGE sql
AS $$
	select G.BibID, I.VID, I.ItemID, I.AdditionalWork_MetadataOnly
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID )
	  and ( I.AdditionalWorkNeeded = 'true' )
	order by BibID, VID;
$$;

-- Also flags visibility/embargo changes as metadata-only work, alongside the existing
-- AdditionalWorkNeeded = true -- but only if the item wasn't already flagged for something
-- else; if it was, that prior flag is left untouched rather than downgraded to metadata-only
-- (see the note above SobekCM_Update_Additional_Work_Needed_Flag). Full body reproduced below
-- since CREATE OR REPLACE FUNCTION requires the complete body -- the only change from the prior
-- definition is the added AdditionalWork_MetadataOnly logic in the "Update the main item table"
-- step.
CREATE OR REPLACE FUNCTION SobekCM_Set_Item_Visibility(
	p_ItemID integer,
	p_IpRestrictionMask smallint,
	p_DarkFlag boolean,
	p_EmbargoDate timestamp,
	p_User varchar(255)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_noteText varchar(200);
	v_workflowId integer;
BEGIN
	v_noteText := '';

	if ( p_EmbargoDate is null ) then
		if ( exists ( select 1 from Tracking_Item where ItemID=p_ItemID and EmbargoEnd is not null )) then
			update Tracking_Item set EmbargoEnd=null where ItemID=p_ItemID;
			v_noteText := 'Embargo date removed.  ';
		end if;
	else
		if ( exists ( select 1 from Tracking_Item where ItemID=p_ItemID )) then
			update Tracking_Item set EmbargoEnd=p_EmbargoDate where ItemID=p_ItemID;
		else
			insert into Tracking_Item ( ItemID, Original_EmbargoEnd, EmbargoEnd )
			values ( p_ItemID, p_EmbargoDate, p_EmbargoDate );
		end if;

		v_noteText := 'Embargo date of ' || to_char(p_EmbargoDate, 'YYYY.MM.DD') || '.  ';
	end if;

	v_workflowId := 34;
	if ( p_IpRestrictionMask < 0 ) then v_workflowId := 35; end if;
	if ( p_IpRestrictionMask < 0 ) then v_workflowId := 36; end if;
	if ( p_DarkFlag ) then
		v_workflowId := 35;
		v_noteText := v_noteText || 'Item made dark.';
	end if;

	update SobekCM_Item
	set IP_Restriction_Mask = p_IpRestrictionMask, Dark = p_DarkFlag, AdditionalWorkNeeded = 'true',
	    AdditionalWork_MetadataOnly = CASE WHEN NOT AdditionalWorkNeeded THEN true ELSE AdditionalWork_MetadataOnly END
	where ItemID=p_ItemID;

	insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, DateStarted )
	values ( p_ItemID, v_workflowId, now(), p_User, v_noteText, now() );

	if (( not p_DarkFlag ) and ( p_IpRestrictionMask >= 0 )) then
		update SobekCM_Item
		set MadePublicDate = coalesce(MadePublicDate, now())
		where ItemID=p_ItemID;
	end if;
END;
$$;

-- Also flags bulk-unembargoed items as metadata-only work, alongside the existing
-- AdditionalWorkNeeded = true -- but only if not already flagged for something else, same
-- non-downgrading rule as SobekCM_Set_Item_Visibility above. Full body reproduced below since
-- CREATE OR REPLACE FUNCTION requires the complete body -- the only change from the prior
-- definition is the added AdditionalWork_MetadataOnly logic in the "Actually mark the items as
-- unembargoed" step.
CREATE OR REPLACE FUNCTION Admin_Unembargo_Items_Past_Embargo_Date(
	p_subject_line varchar(500),
	p_email_message text,
	p_send_email boolean
)
RETURNS TABLE (
	ItemID integer,
	BibID varchar,
	VID varchar(5),
	EmbargoEnd varchar,
	Title varchar,
	Author varchar
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_emailaddress varchar(255);
	v_itemlist text;
	v_emailbody text;
	rec record;
BEGIN
	-- Get the items that need to be processed
	CREATE TEMP TABLE unembargo_items AS
	select I.ItemID, G.BibID, I.VID, to_char(T.EmbargoEnd, 'YYYY.MM.DD') as EmbargoEnd, substring(I.Title,4,1000) as Title, substring(I.Author, 4, 1000) as Author
	from SobekCM_Item I, Tracking_Item T, SobekCM_Item_Group G
	where ( I.ItemID=T.ItemID )
	  and (( I.IP_Restriction_Mask <> 0 ) or ( I.Dark = 'true' ))
	  and ( T.EmbargoEnd < now() )
	  and ( I.GroupID = G.GroupID );

	-- One row per (item, owning-aggregation-contact-email) pair, with the HTML blurb for that item.
	-- Title carried through here so the next step can order the concatenation by it, matching the original cursor's "order by Title".
	CREATE TEMP TABLE item_aggregation_emails AS
	select distinct U.ItemID, U.Title, A.ContactEmail,
	       '<br /><br /><i>' || U.Title || '</i>, by ' || U.Author || ' ( ' || U.BibID || ':' || U.VID || ' ) - ' || U.EmbargoEnd as ItemBlurb
	from unembargo_items U inner join
	     SobekCM_Item_Aggregation_Item_Link L on L.ItemID = U.ItemID and L.impliedLink = 'false' inner join
	     SobekCM_Item_Aggregation A on A.AggregationID = L.AggregationID and length(A.ContactEmail) > 0;

	-- Collapse to one row per contact email, items concatenated in Title order
	CREATE TEMP TABLE emailprep AS
	select ContactEmail as EmailAddress, string_agg(ItemBlurb, '' ORDER BY Title ASC) as ItemList
	from item_aggregation_emails
	group by ContactEmail;

	-- Actually mark the items as unembargoed next
	update SobekCM_Item
	set Dark='false', IP_Restriction_Mask=0, AdditionalWorkNeeded='true',
	    AdditionalWork_MetadataOnly = CASE WHEN NOT AdditionalWorkNeeded THEN true ELSE AdditionalWork_MetadataOnly END
	where exists ( select * from unembargo_items T where T.ItemID=SobekCM_Item.ItemID );

	-- Also add a workflow progress for this
	insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, DateStarted )
	select ItemID, 34, now(), 'Builder Service', 'Automatically unembargoed ( original unembargo date of ' || EmbargoEnd || ' )', now()
	from unembargo_items;

	-- Send emails via database email?
	if ( p_send_email ) then
		FOR rec IN SELECT EmailAddress, ItemList FROM emailprep LOOP
			v_emailbody := REPLACE(p_email_message, '{0}', rec.itemlist);
			PERFORM SobekCM_Send_Email(rec.emailaddress, p_subject_line, v_emailbody, null, null, true, false, -1, -1);
		END LOOP;
	end if;

	-- Return the list of items unembargoed
	RETURN QUERY select * from unembargo_items;

	-- Drop the temporary tables
	drop table unembargo_items;
	drop table item_aggregation_emails;
	drop table emailprep;
END;
$$;


/**************************************************************************/
/**                                                                      **/
/**   Update Database Version                                            **/
/**                                                                      **/
/**************************************************************************/

DO $$
BEGIN
  IF (select count(*) from SobekCM_Database_Version) = 0 THEN
    insert into SobekCM_Database_Version (Major_Version, Minor_Version, Release_Phase)
    values (5, 1, '0');
  ELSE
    update SobekCM_Database_Version
    set Major_Version = 5, Minor_Version = 1, Release_Phase = '0';
  END IF;
END $$;
