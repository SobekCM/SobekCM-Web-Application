/**
Upgrade_to_Ver520_PostgreSQL.sql  - OpenSobek

Takes an existing 5.1.0 PostgreSQL database and brings it up to Version 5.2.0.

 PostgreSQL port of Upgrade_to_Ver520.sql -- see that file for the full rationale
 behind each change. If your version is older than 5.1.0 you will need to run
 Upgrade_to_Ver510_PostgreSQL.sql first.

 */

-- Splits signed GCS URL lifetimes by how the page uses the URL, instead of one lifetime for
-- everything. "GCS Signed URL Expiration Minutes" (default 240) keeps its value and now only
-- covers files the page keeps requesting while it is open (PDF, audio, video, page turner).
-- Page images and thumbnails the browser fetches immediately get "GCS Page Load URL Expiration
-- Minutes" (default 10), and links clicked later, such as the Downloads list, get "GCS Download
-- URL Expiration Minutes" (default 60). Restricted items never exceed "GCS Restricted URL
-- Expiration Minutes". Shorter lifetimes stop harvested or shared links from being reused.

DO $$
BEGIN
  IF NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Page Load URL Expiration Minutes' and Extension_Code is null) THEN
    insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help )
    values ( 'GCS Page Load URL Expiration Minutes', '10', 'System / Server Settings', 'Server Settings', false, 2, 'How long (in minutes) a signed URL stays valid for a GCS-hosted file the browser fetches immediately as the page renders, such as page images and thumbnails. Can be very short: nothing reuses the URL once the page has loaded, and a short value makes harvested or shared links stop working quickly. Restricted items never exceed GCS Restricted URL Expiration Minutes. Only used when File System Mode is "GCS Hybrid" or "GCS Full".' );
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Download URL Expiration Minutes' and Extension_Code is null) THEN
    insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help )
    values ( 'GCS Download URL Expiration Minutes', '60', 'System / Server Settings', 'Server Settings', false, 2, 'How long (in minutes) a signed URL stays valid for a GCS-hosted file offered as a link the user clicks later, such as the Downloads list. Only needs to cover the time between the page loading and the click, since a download that has already started is not cut off when its URL expires. Restricted items never exceed GCS Restricted URL Expiration Minutes. Only used when File System Mode is "GCS Hybrid" or "GCS Full".' );
  END IF;
END $$;

update SobekCM_Settings
set Help = 'How long (in minutes) a signed URL stays valid for a GCS-hosted file that keeps being requested while the page is open, such as PDFs, audio, video and the page turner, which make fresh requests as the user scrolls, seeks or turns pages. Page images and download links use the shorter GCS Page Load URL Expiration Minutes and GCS Download URL Expiration Minutes instead. Only used when File System Mode is "GCS Hybrid" or "GCS Full".'
where Setting_Key = 'GCS Signed URL Expiration Minutes' and Extension_Code is null;


-- Gives restricted items a separate, longer cap for files the page keeps requesting (PDF, audio,
-- video). Before this, every signed URL on a restricted item was capped at "GCS Restricted URL
-- Expiration Minutes" (default 15), so an authorized user seeking or scrolling past 15 minutes
-- hit an expired URL. That setting now caps only page images, thumbnails and download links.

DO $$
BEGIN
  IF NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Restricted Streaming URL Expiration Minutes' and Extension_Code is null) THEN
    insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help )
    values ( 'GCS Restricted Streaming URL Expiration Minutes', '60', 'System / Server Settings', 'Server Settings', false, 2, 'How long (in minutes) a signed URL stays valid for a file on an IP- or user-group-restricted (but not dark) item that keeps being requested while the page is open, such as PDFs, audio and video, which make fresh requests as the user scrolls or seeks. Longer than GCS Restricted URL Expiration Minutes because a shorter value breaks playback and reading partway through. Never exceeds GCS Signed URL Expiration Minutes. Only used when File System Mode is "GCS Hybrid" or "GCS Full".' );
  END IF;
END $$;

update SobekCM_Settings
set Help = 'How long (in minutes) a signed URL stays valid for a page image, thumbnail or download link on an IP- or user-group-restricted (but not dark) item. Deliberately much shorter than the public lifetimes, since a signed URL is a bearer token that works for anyone holding it once handed out. Files the page keeps requesting, such as PDFs, audio and video, use GCS Restricted Streaming URL Expiration Minutes instead. Only used when File System Mode is "GCS Hybrid" or "GCS Full".'
where Setting_Key = 'GCS Restricted URL Expiration Minutes' and Extension_Code is null;


-- Institution aggregations added automatically during an item save (when an item has a holding
-- or source institution code that does not exist yet) now get the same defaults as any other new
-- collection. Before this, SobekCM_Save_New_Item, SobekCM_Save_Item, SobekCM_Save_Item_Behaviors
-- and SobekCM_Mass_Update_Item_Behaviors each inserted a bare 'Added automatically' row, skipping
-- everything SobekCM_Save_Item_Aggregation copies from the parent (ALL) collection: facets,
-- result views, result fields, the GroupResults flag, editing permissions and the 'Created'
-- milestone. All four now call the new SobekCM_Add_Automatic_Institution, which goes through
-- SobekCM_Save_Item_Aggregation. As before, no hierarchy link is added for the new institution.

-- Adds an institution aggregation automatically, when an item is saved with a holding or source
-- institution code that does not exist yet.  Does nothing if the code is empty or already exists.
CREATE OR REPLACE FUNCTION SobekCM_Add_Automatic_Institution(
	p_code varchar(20)
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	-- Nothing to do if no code, or it already exists (deleted or not)
	if (( length(coalesce(p_code, '')) = 0 ) or ( exists ( select 1 from SobekCM_Item_Aggregation where Code = p_code ))) then
		return;
	end if;

	-- Save through the standard function, so the new institution inherits the ALL collection's
	-- facets, result views, result fields, GroupResults flag and editing permissions.  No parent
	-- id is passed, so no hierarchy link is added.
	PERFORM SobekCM_Save_Item_Aggregation(
		-1,                      -- p_aggregationid
		p_code,                  -- p_code
		'Added automatically',   -- p_name
		'Added automatically',   -- p_shortname
		'Added automatically',   -- p_description
		-1,                      -- p_thematicHeadingId
		'Institution',           -- p_type
		false,                   -- p_isactive
		true,                    -- p_hidden
		'',                      -- p_display_options
		0::smallint,             -- p_map_search
		0::smallint,             -- p_map_display
		false,                   -- p_oai_flag
		'',                      -- p_oai_metadata
		'',                      -- p_contactemail
		'',                      -- p_defaultinterface
		'',                      -- p_externallink
		-1,                      -- p_parentid
		'Added automatically',   -- p_username
		'',                      -- p_languageVariants
		false );                 -- p_groupResults
END;
$$;

CREATE OR REPLACE FUNCTION SobekCM_Save_New_Item(
	p_GroupID integer,
	p_VID varchar(5),
	p_PageCount integer,
	p_FileCount integer,
	p_Title varchar(500),
	p_SortTitle varchar(500),
	p_AccessMethod integer,
	p_Link varchar(500),
	p_CreateDate timestamp,
	p_PubDate varchar(100),
	p_SortDate bigint,
	p_Author varchar(1000),
	p_Spatial_KML varchar(4000),
	p_Spatial_KML_Distance double precision,
	p_DiskSize_KB bigint,
	p_Spatial_Display varchar(1000),
	p_Institution_Display varchar(1000),
	p_Edition_Display varchar(1000),
	p_Material_Display varchar(1000),
	p_Measurement_Display varchar(1000),
	p_StylePeriod_Display varchar(1000),
	p_Technique_Display varchar(1000),
	p_Subjects_Display varchar(1000),
	p_Donor varchar(250),
	p_Publisher varchar(1000),
	p_TextSearchable boolean,
	p_MainThumbnail varchar(100),
	p_MainJPEG varchar(100),
	p_IP_Restriction_Mask smallint,
	p_CheckoutRequired boolean,
	p_AggregationCode1 varchar(20), p_AggregationCode2 varchar(20), p_AggregationCode3 varchar(20), p_AggregationCode4 varchar(20),
	p_AggregationCode5 varchar(20), p_AggregationCode6 varchar(20), p_AggregationCode7 varchar(20), p_AggregationCode8 varchar(20),
	p_HoldingCode varchar(20),
	p_SourceCode varchar(20),
	p_Icon1_Name varchar(50), p_Icon2_Name varchar(50), p_Icon3_Name varchar(50), p_Icon4_Name varchar(50), p_Icon5_Name varchar(50),
	p_Level1_Text varchar(255), p_Level1_Index integer,
	p_Level2_Text varchar(255), p_Level2_Index integer,
	p_Level3_Text varchar(255), p_Level3_Index integer,
	p_Level4_Text varchar(255), p_Level4_Index integer,
	p_Level5_Text varchar(255), p_Level5_Index integer,
	p_VIDSource varchar(150),
	p_CopyrightIndicator smallint,
	p_Born_Digital boolean,
	p_Dark boolean,
	p_Material_Received_Date timestamp,
	p_Material_Recd_Date_Estimated boolean,
	p_Disposition_Advice integer,
	p_Disposition_Advice_Notes varchar(150),
	p_Internal_Comments varchar(1000),
	p_Tracking_Box varchar(25),
	p_Online_Submit boolean,
	p_User varchar(50),
	p_UserNotes varchar(1000),
	p_UserID_To_Link integer,
	p_RestrictionMessage varchar(1000),
	OUT p_ItemID integer,
	OUT p_New_VID varchar(5)
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_next_vid_number integer;
	v_IconID integer;
	v_aggregationCodes varchar(100);
	v_userfolderid integer;
BEGIN
	p_New_VID := p_VID;
	p_ItemID := -1;

	if ( (	 select count(*) from SobekCM_Item I where ( I.VID = p_VID ) and ( I.GroupID = p_GroupID ))  =  0 ) then
		if ( LENGTH(p_VID) < 5 ) then
			select coalesce(CAST(MAX(VID) as integer) + 1,-1) into v_next_vid_number
			from SobekCM_Item
			where GroupID = p_GroupID;

			if ( v_next_vid_number < 0 ) then
				p_New_VID := '00001';
			else
				p_New_VID := RIGHT('0000' || (CAST( v_next_vid_number as varchar(5))), 5);
			end if;
		end if;

		insert into SobekCM_Item ( VID, PageCount, FileCount, Deleted, Title, SortTitle, AccessMethod, Link, CreateDate, PubDate, SortDate, Author, Spatial_KML, Spatial_KML_Distance, GroupID, LastSaved, Donor, Publisher, TextSearchable, MainThumbnail, MainJPEG, CheckoutRequired, IP_Restriction_Mask, Level1_Text, Level1_Index, Level2_Text, Level2_Index, Level3_Text, Level3_Index, Level4_Text, Level4_Index, Level5_Text, Level5_Index, Last_MileStone, VIDSource, Born_Digital, Dark, Material_Received_Date, Material_Recd_Date_Estimated, Disposition_Advice, Internal_Comments, Tracking_Box, Disposition_Advice_Notes, Spatial_Display, Institution_Display, Edition_Display, Material_Display, Measurement_Display, StylePeriod_Display, Technique_Display, Subjects_Display, RestrictionMessage )
		values (  p_New_VID, p_PageCount, p_FileCount, false, p_Title, p_SortTitle, p_AccessMethod, p_Link, p_CreateDate, p_PubDate, p_SortDate, p_Author, p_Spatial_KML, p_Spatial_KML_Distance, p_GroupID, now(), p_Donor, p_Publisher, p_TextSearchable, p_MainThumbnail, p_MainJPEG, p_CheckoutRequired, p_IP_Restriction_Mask, p_Level1_Text, p_Level1_Index, p_Level2_Text, p_Level2_Index, p_Level3_Text, p_Level3_Index, p_Level4_Text, p_Level4_Index, p_Level5_Text, p_Level5_Index, 0, p_VIDSource, p_Born_Digital, p_Dark, p_Material_Received_Date, p_Material_Recd_Date_Estimated, p_Disposition_Advice, p_Internal_Comments, p_Tracking_Box, p_Disposition_Advice_Notes, p_Spatial_Display, p_Institution_Display, p_Edition_Display, p_Material_Display, p_Measurement_Display, p_StylePeriod_Display, p_Technique_Display, p_Subjects_Display, p_RestrictionMessage  )
		returning ItemID into p_ItemID;

		-- Set the milestones to complete if this is NON-PRIVATE, NON-DARK, and BORN DIGITAL
		if (( p_IP_Restriction_Mask >= 0 ) and ( not p_Dark ) and ( p_Born_Digital )) then
			update SobekCM_Item
			set Last_MileStone = 4, Milestone_DigitalAcquisition = CreateDate, Milestone_ImageProcessing=CreateDate, Milestone_QualityControl=CreateDate, Milestone_OnlineComplete=CreateDate
			where ItemID=p_ItemID;
		end if;

		if ( p_DiskSize_KB > 0 ) then
			update SobekCM_Item set DiskSize_KB = p_DiskSize_KB where ItemID=p_ItemID;
		end if;

		update SobekCM_Item_Group
		set ItemCount = ( select count(*) from SobekCM_Item I where ( I.GroupID = p_GroupID ) and ( I.Deleted = 'false' ))
		where GroupID = p_GroupID;

		if ( length( coalesce( p_Icon1_Name, '' )) > 0 ) then
			select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon1_Name;
			if ( coalesce(v_IconID,-1) > 0 ) then
				insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
				values ( p_ItemID, v_IconID, 1 );
			end if;
		end if;

		if ( length( coalesce( p_Icon2_Name, '' )) > 0 ) then
			select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon2_Name;
			if ( coalesce(v_IconID,-1) > 0 ) then
				insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
				values ( p_ItemID, v_IconID, 2 );
			end if;
		end if;

		if ( length( coalesce( p_Icon3_Name, '' )) > 0 ) then
			select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon3_Name;
			if ( coalesce(v_IconID,-1) > 0 ) then
				insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
				values ( p_ItemID, v_IconID, 3 );
			end if;
		end if;

		if ( length( coalesce( p_Icon4_Name, '' )) > 0 ) then
			select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon4_Name;
			if ( coalesce(v_IconID,-1) > 0 ) then
				insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
				values ( p_ItemID, v_IconID, 4 );
			end if;
		end if;

		if ( length( coalesce( p_Icon5_Name, '' )) > 0 ) then
			select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon5_Name;
			if ( coalesce(v_IconID,-1) > 0 ) then
				insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
				values ( p_ItemID, v_IconID, 5 );
			end if;
		end if;

		delete from SobekCM_Item_Aggregation_Item_Link where ItemID = p_ItemID;

		PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode1);
		PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode2);
		PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode3);
		PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode4);
		PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode5);
		PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode6);
		PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode7);
		PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode8);

		v_aggregationCodes := rtrim(coalesce(p_AggregationCode1,'') || ' ' || coalesce(p_AggregationCode2,'') || ' ' || coalesce(p_AggregationCode3,'') || ' ' || coalesce(p_AggregationCode4,'') || ' ' || coalesce(p_AggregationCode5,'') || ' ' || coalesce(p_AggregationCode6,'') || ' ' || coalesce(p_AggregationCode7,'') || ' ' || coalesce(p_AggregationCode8,''));

		update SobekCM_Item set AggregationCodes = v_aggregationCodes where ItemID=p_ItemID;

		if ( length ( coalesce ( p_HoldingCode, '' ) ) > 0 ) then
			-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
			PERFORM SobekCM_Add_Automatic_Institution(p_HoldingCode);

			PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_HoldingCode);
		end if;

		if ( length ( coalesce ( p_SourceCode, '' ) ) > 0 ) then
			-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
			PERFORM SobekCM_Add_Automatic_Institution(p_SourceCode);

			PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_SourceCode);
		end if;

		delete from SobekCM_Item_Viewers
		where ItemID=p_itemid;

		insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label, Exclude )
		select p_itemid, ItemViewTypeID, '', '', 'false'
		from SobekCM_Item_Viewer_Types
		where ( DefaultView = 'true' );

		if ( p_Online_Submit ) then
			insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath, WorkPerformedById )
			values ( p_itemid, 29, now(), p_user, p_usernotes, '', p_UserID_To_Link );
		else
			insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath )
			values ( p_itemid, 40, now(), p_user, p_usernotes, '' );
		end if;

		if (( not p_Dark ) and ( p_IP_Restriction_Mask >= 0 )) then
			update SobekCM_Item
			set MadePublicDate = now()
			where ItemID=p_ItemID;
		end if;

		if ( p_UserID_To_Link >= 1 ) then
			if (( select COUNT(*) from mySobek_User_Bib_Link where UserID=p_UserID_To_Link and GroupID = p_groupid ) = 0 ) then
				insert into mySobek_User_Bib_Link ( UserID, GroupID )
				values ( p_UserID_To_Link, p_groupid );
			end if;

			if (( select count(*) from mySobek_User_Folder where UserID=p_UserID_To_Link and FolderName='Submitted Items') > 0 ) then
				select UserFolderID into v_userfolderid from mySobek_User_Folder where UserID=p_UserID_To_Link and FolderName='Submitted Items';
			else
				insert into mySobek_User_Folder ( UserID, FolderName, isPublic )
				values ( p_UserID_To_Link, 'Submitted Items', 'false' )
				returning UserFolderID into v_userfolderid;
			end if;

			insert into mySobek_User_Item( UserFolderID, ItemID, ItemOrder, UserNotes, DateAdded )
			values ( v_userfolderid, p_itemid, 1, '', now() );

			-- Also link using the newer system, which links for statistical reporting, etc..
			-- This will likely replace the 'submitted items' folder technique from above
			insert into mySobek_User_Item_Link( UserID, ItemID, RelationshipID )
			values ( p_UserID_To_Link, p_ItemID, 1 );
		end if;
	end if;
END;
$$;

CREATE OR REPLACE FUNCTION SobekCM_Save_Item(
	p_GroupID integer,
	p_VID varchar(5),
	p_PageCount integer,
	p_FileCount integer,
	p_Title varchar(500),
	p_SortTitle varchar(500),
	p_AccessMethod integer,
	p_Link varchar(500),
	p_CreateDate timestamp,
	p_PubDate varchar(100),
	p_SortDate bigint,
	p_HoldingCode varchar(20),
	p_SourceCode varchar(20),
	p_Author varchar(1000),
	p_Spatial_KML varchar(4000),
	p_Spatial_KML_Distance double precision,
	p_DiskSize_KB bigint,
	p_Spatial_Display varchar(1000),
	p_Institution_Display varchar(1000),
	p_Edition_Display varchar(1000),
	p_Material_Display varchar(1000),
	p_Measurement_Display varchar(1000),
	p_StylePeriod_Display varchar(1000),
	p_Technique_Display varchar(1000),
	p_Subjects_Display varchar(1000),
	p_Donor varchar(250),
	p_Publisher varchar(1000),
	p_RestrictionMessage varchar(1000),
	OUT p_ItemID integer,
	OUT p_Existing boolean,
	OUT p_New_VID varchar(5)
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_next_vid_number integer;
	v_AggregationID integer;
	v_itemcount integer;
BEGIN
	p_New_VID := p_VID;

	if ( (	 select count(*) from SobekCM_Item I where ( I.VID = p_VID ) and ( I.GroupID = p_GroupID ) )  > 0 ) then
		select I.ItemID into p_ItemID
		from SobekCM_Item I
		where  ( I.VID = p_VID ) and ( I.GroupID = p_GroupID );

		update SobekCM_Item
		set PageCount = p_PageCount,
			Deleted = false, Title=p_Title, SortTitle=p_SortTitle, AccessMethod=p_AccessMethod, Link=p_Link,
			PubDate=p_PubDate, SortDate=p_SortDate, FileCount=p_FileCount, Author=p_Author,
			Spatial_KML=p_Spatial_KML, Spatial_KML_Distance=p_Spatial_KML_Distance,
			Donor=p_Donor, Publisher=p_Publisher,
			GroupID = GroupID, LastSaved=now(), Spatial_Display=p_Spatial_Display, Institution_Display=p_Institution_Display,
			Edition_Display=p_Edition_Display, Material_Display=p_Material_Display, Measurement_Display=p_Measurement_Display,
			StylePeriod_Display=p_StylePeriod_Display, Technique_Display=p_Technique_Display, Subjects_Display=p_Subjects_Display,
			RestrictionMessage=p_RestrictionMessage
		where ( ItemID = p_ItemID );

		p_Existing := true;
	else
		-- Verify the VID is a complete bibid, otherwise find the next one
		if ( LENGTH(p_VID) < 5 ) then
			select coalesce(CAST(MAX(VID) as integer) + 1,-1) into v_next_vid_number
			from SobekCM_Item
			where GroupID = p_GroupID;

			if ( v_next_vid_number < 0 ) then
				p_New_VID := '00001';
			else
				p_New_VID := RIGHT('0000' || (CAST( v_next_vid_number as varchar(5))), 5);
			end if;
		end if;

		insert into SobekCM_Item ( VID, PageCount, FileCount, Deleted, Title, SortTitle, AccessMethod, Link, CreateDate, PubDate, SortDate, Author, Spatial_KML, Spatial_KML_Distance, GroupID, LastSaved, Donor, Publisher, Spatial_Display, Institution_Display, Edition_Display, Material_Display, Measurement_Display, StylePeriod_Display, Technique_Display, Subjects_Display, RestrictionMessage )
		values (  p_New_VID, p_PageCount, p_FileCount, false, p_Title, p_SortTitle, p_AccessMethod, p_Link, p_CreateDate, p_PubDate, p_SortDate, p_Author, p_Spatial_KML, p_Spatial_KML_Distance, p_GroupID, now(), p_Donor, p_Publisher, p_Spatial_Display, p_Institution_Display, p_Edition_Display, p_Material_Display, p_Measurement_Display, p_StylePeriod_Display, p_Technique_Display, p_Subjects_Display, p_RestrictionMessage )
		returning ItemID into p_ItemID;

		p_Existing := false;

		insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label, Exclude )
		select p_itemid, ItemViewTypeID, '', '', 'false'
		from SobekCM_Item_Viewer_Types
		where ( DefaultView = 'true' );
	end if;

	-- Check for Holding Institution Code
	if ( length ( coalesce ( p_HoldingCode, '' ) ) > 0 ) then
		-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
		PERFORM SobekCM_Add_Automatic_Institution(p_HoldingCode);

		PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_HoldingCode);
	end if;

	-- Check for Source Institution Code
	if ( length ( coalesce ( p_SourceCode, '' ) ) > 0 ) then
		-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
		PERFORM SobekCM_Add_Automatic_Institution(p_SourceCode);

		PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_SourceCode);
	end if;

	if ( p_DiskSize_KB > 0 ) then
		update SobekCM_Item set DiskSize_KB = p_DiskSize_KB where ItemID=p_ItemID;
	end if;

	select count(*) into v_itemcount from SobekCM_Item I where ( I.GroupID = p_GroupID ) and ( I.Deleted = 'false' );

	update SobekCM_Item_Group
	set ItemCount = v_itemcount
	where GroupID = p_GroupID;

	-- If this was an update, and this group had only this one VID, look at changing the
	-- group title to match the item title
	if (( p_Existing ) and ( v_itemcount = 1 )) then
		if ( exists ( select 1 from SobekCM_Item_Group where GroupID=p_GroupID and Type != 'Serial' and Type != 'Newspaper' )) then
			update SobekCM_Item_Group
			set GroupTitle = p_Title, SortTitle = p_SortTitle
			where GroupID=p_GroupID;
		end if;
	end if;
END;
$$;

CREATE OR REPLACE FUNCTION SobekCM_Save_Item_Behaviors(
	p_ItemID integer,
	p_TextSearchable boolean,
	p_MainThumbnail varchar(100),
	p_MainJPEG varchar(100),
	p_IP_Restriction_Mask smallint,
	p_CheckoutRequired boolean,
	p_Dark_Flag boolean,
	p_Born_Digital boolean,
	p_Disposition_Advice integer,
	p_Disposition_Advice_Notes varchar(150),
	p_Material_Received_Date timestamp,
	p_Material_Recd_Date_Estimated boolean,
	p_Tracking_Box varchar(25),
	p_AggregationCode1 varchar(20), p_AggregationCode2 varchar(20), p_AggregationCode3 varchar(20), p_AggregationCode4 varchar(20),
	p_AggregationCode5 varchar(20), p_AggregationCode6 varchar(20), p_AggregationCode7 varchar(20), p_AggregationCode8 varchar(20),
	p_HoldingCode varchar(20),
	p_SourceCode varchar(20),
	p_Icon1_Name varchar(50), p_Icon2_Name varchar(50), p_Icon3_Name varchar(50), p_Icon4_Name varchar(50), p_Icon5_Name varchar(50),
	p_Left_To_Right boolean,
	p_CitationSet varchar(50)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_IconID integer;
	v_aggregationCodes varchar(100);
BEGIN
	update SobekCM_Item
	set TextSearchable = p_TextSearchable, Deleted = false, MainThumbnail=p_MainThumbnail,
		MainJPEG=p_MainJPEG, CheckoutRequired=p_CheckoutRequired, IP_Restriction_Mask=p_IP_Restriction_Mask,
		Dark=p_Dark_Flag, Born_Digital=p_Born_Digital, Disposition_Advice=p_Disposition_Advice,
		Material_Received_Date=p_Material_Received_Date, Material_Recd_Date_Estimated=p_Material_Recd_Date_Estimated,
		Tracking_Box=p_Tracking_Box, Disposition_Advice_Notes = p_Disposition_Advice_Notes, Left_To_Right=p_Left_To_Right,
		CitationSet=p_CitationSet
	where ( ItemID = p_ItemID );

	delete from SobekCM_Item_Icons where ItemID=p_ItemID;

	if ( length( coalesce( p_Icon1_Name, '' )) > 0 ) then
		select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon1_Name;

		if ( coalesce(v_IconID,-1) > 0 ) then
			insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
			values ( p_ItemID, v_IconID, 1 );
		end if;
	end if;

	if ( length( coalesce( p_Icon2_Name, '' )) > 0 ) then
		select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon2_Name;

		if (( coalesce(v_IconID,-1) > 0 )  and ( not exists ( select 1 from SobekCM_Item_Icons where ItemID=p_ItemID and IconID=v_IconID ))) then
			insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
			values ( p_ItemID, v_IconID, 2 );
		end if;
	end if;

	if ( length( coalesce( p_Icon3_Name, '' )) > 0 ) then
		select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon3_Name;

		if (( coalesce(v_IconID,-1) > 0 ) and ( not exists ( select 1 from SobekCM_Item_Icons where ItemID=p_ItemID and IconID=v_IconID ))) then
			insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
			values ( p_ItemID, v_IconID, 3 );
		end if;
	end if;

	if ( length( coalesce( p_Icon4_Name, '' )) > 0 ) then
		select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon4_Name;

		if (( coalesce(v_IconID,-1) > 0 ) and ( not exists ( select 1 from SobekCM_Item_Icons where ItemID=p_ItemID and IconID=v_IconID ))) then
			insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
			values ( p_ItemID, v_IconID, 4 );
		end if;
	end if;

	if ( length( coalesce( p_Icon5_Name, '' )) > 0 ) then
		select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon5_Name;

		if (( coalesce(v_IconID,-1) > 0 ) and ( not exists ( select 1 from SobekCM_Item_Icons where ItemID=p_ItemID and IconID=v_IconID ))) then
			insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
			values ( p_ItemID, v_IconID, 5 );
		end if;
	end if;

	delete from SobekCM_Item_Aggregation_Item_Link where ItemID = p_ItemID;

	PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode1);
	PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode2);
	PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode3);
	PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode4);
	PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode5);
	PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode6);
	PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode7);
	PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_AggregationCode8);

	v_aggregationCodes := rtrim(coalesce(p_AggregationCode1,'') || ' ' || coalesce(p_AggregationCode2,'') || ' ' || coalesce(p_AggregationCode3,'') || ' ' || coalesce(p_AggregationCode4,'') || ' ' || coalesce(p_AggregationCode5,'') || ' ' || coalesce(p_AggregationCode6,'') || ' ' || coalesce(p_AggregationCode7,'') || ' ' || coalesce(p_AggregationCode8,''));

	update SobekCM_Item set AggregationCodes = v_aggregationCodes where ItemID=p_ItemID;

	-- Check for Holding Institution Code
	if ( length ( coalesce ( p_HoldingCode, '' ) ) > 0 ) then
		-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
		PERFORM SobekCM_Add_Automatic_Institution(p_HoldingCode);

		PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_HoldingCode);
	end if;

	-- Check for Source Institution Code
	if ( length ( coalesce ( p_SourceCode, '' ) ) > 0 ) then
		-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
		PERFORM SobekCM_Add_Automatic_Institution(p_SourceCode);

		PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_SourceCode);
	end if;

	-- If this is being made public, set the public data
	if (( not p_Dark_Flag ) and ( p_IP_Restriction_Mask >= 0 )) then
		update SobekCM_Item
		set MadePublicDate = coalesce(MadePublicDate, now())
		where ItemID=p_ItemID;
	end if;
END;
$$;

CREATE OR REPLACE FUNCTION SobekCM_Mass_Update_Item_Behaviors(
	p_GroupID integer,
	p_IP_Restriction_Mask smallint,
	p_CheckoutRequired boolean,
	p_Dark_Flag boolean,
	p_Born_Digital boolean,
	p_AggregationCode1 varchar(20),
	p_AggregationCode2 varchar(20),
	p_AggregationCode3 varchar(20),
	p_AggregationCode4 varchar(20),
	p_AggregationCode5 varchar(20),
	p_AggregationCode6 varchar(20),
	p_AggregationCode7 varchar(20),
	p_AggregationCode8 varchar(20),
	p_HoldingCode varchar(20),
	p_SourceCode varchar(20),
	p_Icon1_Name varchar(50),
	p_Icon2_Name varchar(50),
	p_Icon3_Name varchar(50),
	p_Icon4_Name varchar(50),
	p_Icon5_Name varchar(50),
	p_Viewer1_Type varchar(50), p_Viewer1_Label varchar(50), p_Viewer1_Attribute varchar(250),
	p_Viewer2_Type varchar(50), p_Viewer2_Label varchar(50), p_Viewer2_Attribute varchar(250),
	p_Viewer3_Type varchar(50), p_Viewer3_Label varchar(50), p_Viewer3_Attribute varchar(250),
	p_Viewer4_Type varchar(50), p_Viewer4_Label varchar(50), p_Viewer4_Attribute varchar(250),
	p_Viewer5_Type varchar(50), p_Viewer5_Label varchar(50), p_Viewer5_Attribute varchar(250),
	p_Viewer6_Type varchar(50), p_Viewer6_Label varchar(50), p_Viewer6_Attribute varchar(250)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_IconID integer;
	v_Viewer1_TypeID integer;
	v_Viewer2_TypeID integer;
	v_Viewer3_TypeID integer;
	v_Viewer4_TypeID integer;
	v_Viewer5_TypeID integer;
	v_Viewer6_TypeID integer;
BEGIN
	if ( p_IP_Restriction_Mask is not null ) then
		update SobekCM_Item
		set IP_Restriction_Mask=p_IP_Restriction_Mask
		where ( GroupID = p_GroupID );
	end if;

	if ( p_CheckoutRequired is not null ) then
		update SobekCM_Item
		set CheckoutRequired=p_CheckoutRequired
		where ( GroupID = p_GroupID );
	end if;

	if ( p_Dark_Flag is not null ) then
		update SobekCM_Item
		set Dark=p_Dark_Flag
		where ( GroupID = p_GroupID );
	end if;

	if ( p_Born_Digital is not null ) then
		update SobekCM_Item
		set Born_Digital=p_Born_Digital
		where ( GroupID = p_GroupID );
	end if;

	-- Only do icon stuff if the first icon has length
	if ( length( coalesce( p_Icon1_Name, '' )) > 0 ) then
		delete from SobekCM_Item_Icons
		where exists (  select *
						from SobekCM_Item
						where ( SobekCM_Item.GroupID=p_GroupID )
						  and ( SobekCM_Item.ItemID = SobekCM_Item_Icons.ItemID ));

		if ( length( coalesce( p_Icon1_Name, '' )) > 0 ) then
			select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon1_Name;

			if ( coalesce(v_IconID,-1) > 0 ) then
				insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
				select ItemID, v_IconID, 1 from SobekCM_Item I where I.GroupID=p_GroupID;
			end if;
		end if;

		if ( length( coalesce( p_Icon2_Name, '' )) > 0 ) then
			select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon2_Name;

			if ( coalesce(v_IconID,-1) > 0 ) then
				insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
				select ItemID, v_IconID, 2 from SobekCM_Item I where I.GroupID=p_GroupID;
			end if;
		end if;

		if ( length( coalesce( p_Icon3_Name, '' )) > 0 ) then
			select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon3_Name;

			if ( coalesce(v_IconID,-1) > 0 ) then
				insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
				select ItemID, v_IconID, 3 from SobekCM_Item I where I.GroupID=p_GroupID;
			end if;
		end if;

		if ( length( coalesce( p_Icon4_Name, '' )) > 0 ) then
			select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon4_Name;

			if ( coalesce(v_IconID,-1) > 0 ) then
				insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
				select ItemID, v_IconID, 4 from SobekCM_Item I where I.GroupID=p_GroupID;
			end if;
		end if;

		if ( length( coalesce( p_Icon5_Name, '' )) > 0 ) then
			select IconID into v_IconID from SobekCM_Icon where Icon_Name = p_Icon5_Name;

			if ( coalesce(v_IconID,-1) > 0 ) then
				insert into SobekCM_Item_Icons ( ItemID, IconID, "Sequence" )
				select ItemID, v_IconID, 5 from SobekCM_Item I where I.GroupID=p_GroupID;
			end if;
		end if;
	end if;

	-- Only modify the aggregation codes if they have length
	if ( length ( coalesce( p_AggregationCode1, '')) > 0 ) then
		delete from SobekCM_Item_Aggregation_Item_Link
		where exists ( select * from SobekCM_Item I where I.GroupID=p_GroupID and I.ItemID=SobekCM_Item_Aggregation_Item_Link.ItemID );

		PERFORM SobekCM_Mass_Update_Item_Aggregation_Link(p_GroupID, p_AggregationCode1);
		PERFORM SobekCM_Mass_Update_Item_Aggregation_Link(p_GroupID, p_AggregationCode2);
		PERFORM SobekCM_Mass_Update_Item_Aggregation_Link(p_GroupID, p_AggregationCode3);
		PERFORM SobekCM_Mass_Update_Item_Aggregation_Link(p_GroupID, p_AggregationCode4);
		PERFORM SobekCM_Mass_Update_Item_Aggregation_Link(p_GroupID, p_AggregationCode5);
		PERFORM SobekCM_Mass_Update_Item_Aggregation_Link(p_GroupID, p_AggregationCode6);
		PERFORM SobekCM_Mass_Update_Item_Aggregation_Link(p_GroupID, p_AggregationCode7);
		PERFORM SobekCM_Mass_Update_Item_Aggregation_Link(p_GroupID, p_AggregationCode8);
	end if;

	-- Check for Holding Institution Code
	if ( length ( coalesce ( p_HoldingCode, '' ) ) > 0 ) then
		-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
		PERFORM SobekCM_Add_Automatic_Institution(p_HoldingCode);

		PERFORM SobekCM_Mass_Update_Item_Aggregation_Link(p_GroupID, p_HoldingCode);
	end if;

	-- Check for Source Institution Code
	if ( length ( coalesce ( p_SourceCode, '' ) ) > 0 ) then
		-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
		PERFORM SobekCM_Add_Automatic_Institution(p_SourceCode);

		PERFORM SobekCM_Mass_Update_Item_Aggregation_Link(p_GroupID, p_SourceCode);
	end if;

	-- Add the first viewer information, if provided
	if ( length(coalesce(p_Viewer1_Type, '')) > 0 ) then
		v_Viewer1_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer1_Type ), -1 );

		if ( v_Viewer1_TypeID > 0 ) then
			insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
			select I.ItemID, v_Viewer1_TypeID, p_Viewer1_Attribute, p_Viewer1_Label
			from SobekCM_Item I
			where ( I.GroupID=p_GroupID )
				and ( not exists ( select 1 from SobekCM_Item_Viewers where ItemID=I.ItemID and ItemViewTypeID=v_Viewer1_TypeID ));
		end if;
	end if;

	-- Add the second viewer information, if provided
	if ( length(coalesce(p_Viewer2_Type, '')) > 0 ) then
		v_Viewer2_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer2_Type ), -1 );

		if ( v_Viewer2_TypeID > 0 ) then
			insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
			select I.ItemID, v_Viewer2_TypeID, p_Viewer2_Attribute, p_Viewer2_Label
			from SobekCM_Item I
			where ( I.GroupID=p_GroupID )
				and ( not exists ( select 1 from SobekCM_Item_Viewers where ItemID=I.ItemID and ItemViewTypeID=v_Viewer2_TypeID ));
		end if;
	end if;

	-- Add the third viewer information, if provided
	if ( length(coalesce(p_Viewer3_Type, '')) > 0 ) then
		v_Viewer3_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer3_Type ), -1 );

		if ( v_Viewer3_TypeID > 0 ) then
			insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
			select I.ItemID, v_Viewer3_TypeID, p_Viewer3_Attribute, p_Viewer3_Label
			from SobekCM_Item I
			where ( I.GroupID=p_GroupID )
				and ( not exists ( select 1 from SobekCM_Item_Viewers where ItemID=I.ItemID and ItemViewTypeID=v_Viewer3_TypeID ));
		end if;
	end if;

	-- Add the fourth viewer information, if provided
	if ( length(coalesce(p_Viewer4_Type, '')) > 0 ) then
		v_Viewer4_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer4_Type ), -1 );

		if ( v_Viewer4_TypeID > 0 ) then
			insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
			select I.ItemID, v_Viewer4_TypeID, p_Viewer4_Attribute, p_Viewer4_Label
			from SobekCM_Item I
			where ( I.GroupID=p_GroupID )
				and ( not exists ( select 1 from SobekCM_Item_Viewers where ItemID=I.ItemID and ItemViewTypeID=v_Viewer4_TypeID ));
		end if;
	end if;

	-- Add the fifth viewer information, if provided
	if ( length(coalesce(p_Viewer5_Type, '')) > 0 ) then
		v_Viewer5_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer5_Type ), -1 );

		if ( v_Viewer5_TypeID > 0 ) then
			insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
			select I.ItemID, v_Viewer5_TypeID, p_Viewer5_Attribute, p_Viewer5_Label
			from SobekCM_Item I
			where ( I.GroupID=p_GroupID )
				and ( not exists ( select 1 from SobekCM_Item_Viewers where ItemID=I.ItemID and ItemViewTypeID=v_Viewer5_TypeID ));
		end if;
	end if;

	-- Add the sixth viewer information, if provided
	if ( length(coalesce(p_Viewer6_Type, '')) > 0 ) then
		v_Viewer6_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer6_Type ), -1 );

		if ( v_Viewer6_TypeID > 0 ) then
			insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
			select I.ItemID, v_Viewer6_TypeID, p_Viewer6_Attribute, p_Viewer6_Label
			from SobekCM_Item I
			where ( I.GroupID=p_GroupID )
				and ( not exists ( select 1 from SobekCM_Item_Viewers where ItemID=I.ItemID and ItemViewTypeID=v_Viewer6_TypeID ));
		end if;
	end if;
END;
$$;

-- One-time backfill for institutions that were added automatically before this fix.  Any
-- non-deleted Institution aggregation with no result views at all gets the ALL collection's result
-- views and result fields, its facets (only if it has none) and its editing permissions (only for
-- users and groups without a row for that institution yet).  GroupResults is left alone, since it
-- may have been set deliberately since.
DO $$
DECLARE
	v_allid integer;
BEGIN
	select AggregationID into v_allid from SobekCM_Item_Aggregation where Code = 'ALL';

	if ( v_allid is not null ) then
		-- Find the institutions needing the defaults
		create temp table tmp_needs_defaults on commit drop as
		select A.AggregationID
		from SobekCM_Item_Aggregation A
		where ( A.Type = 'Institution' )
		  and ( A.Deleted = false )
		  and ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views V where V.AggregationID = A.AggregationID ));

		-- Copy over the facet fields (only where none exist)
		insert into SobekCM_Item_Aggregation_Facets ( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
		select N.AggregationID, F.MetadataTypeID, F.OverrideFacetTerm, F.FacetOrder, F.FacetOptions
		from tmp_needs_defaults N, SobekCM_Item_Aggregation_Facets F
		where ( F.AggregationID = v_allid )
		  and ( not exists ( select 1 from SobekCM_Item_Aggregation_Facets X where X.AggregationID = N.AggregationID ));

		-- Copy over the results views
		insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
		select N.AggregationID, V.ItemAggregationResultTypeID, V.DefaultView
		from tmp_needs_defaults N, SobekCM_Item_Aggregation_Result_Views V
		where V.AggregationID = v_allid;

		-- Now, add the result view fields
		insert into SobekCM_Item_Aggregation_Result_Fields ( ItemAggregationResultID, MetadataTypeID, OverrideDisplayTerm, DisplayOrder, DisplayOptions )
		select V2.ItemAggregationResultID, F1.MetadataTypeID, F1.OverrideDisplayTerm, F1.DisplayOrder, F1.DisplayOptions
		from SobekCM_Item_Aggregation_Result_Views V1, SobekCM_Item_Aggregation_Result_Fields F1, SobekCM_Item_Aggregation_Result_Views V2, tmp_needs_defaults N
		where V1.ItemAggregationResultID = F1.ItemAggregationResultID
		  and V1.AggregationID = v_allid
		  and V2.ItemAggregationResultTypeID = V1.ItemAggregationResultTypeID
		  and V2.AggregationID = N.AggregationID;

		-- Add individual user permissions
		insert into mySobek_User_Edit_Aggregation ( UserID, AggregationID, CanSelect, CanEditItems,
			IsCurator, IsAdmin, CanEditMetadata, CanEditBehaviors, CanPerformQc,
			CanUploadFiles, CanChangeVisibility, CanDelete )
		select A.UserID, N.AggregationID, A.CanSelect, A.CanEditItems,
			A.IsCurator, A.IsAdmin, A.CanEditMetadata, A.CanEditBehaviors, A.CanPerformQc,
			A.CanUploadFiles, A.CanChangeVisibility, A.CanDelete
		from mySobek_User_Edit_Aggregation A, tmp_needs_defaults N
		where ( A.AggregationID = v_allid )
		  and ( not exists ( select * from mySobek_User_Edit_Aggregation L where L.UserID = A.UserID and L.AggregationID = N.AggregationID ))
		  and (    ( A.CanEditMetadata = 'true' )
		        or ( A.CanEditBehaviors = 'true' )
		        or ( A.CanPerformQc = 'true' )
		        or ( A.CanUploadFiles = 'true' )
		        or ( A.CanChangeVisibility = 'true' )
		        or ( A.IsCurator = 'true' )
		        or ( A.IsAdmin = 'true' ));

		-- Add user group permissions
		insert into mySobek_User_Group_Edit_Aggregation ( UserGroupID, AggregationID, CanSelect, CanEditItems,
			IsCurator, IsAdmin, CanEditMetadata, CanEditBehaviors, CanPerformQc,
			CanUploadFiles, CanChangeVisibility, CanDelete )
		select A.UserGroupID, N.AggregationID, A.CanSelect, A.CanEditItems,
			A.IsCurator, A.IsAdmin, A.CanEditMetadata, A.CanEditBehaviors, A.CanPerformQc,
			A.CanUploadFiles, A.CanChangeVisibility, A.CanDelete
		from mySobek_User_Group_Edit_Aggregation A, tmp_needs_defaults N
		where ( A.AggregationID = v_allid )
		  and ( not exists ( select * from mySobek_User_Group_Edit_Aggregation L where L.UserGroupID = A.UserGroupID and L.AggregationID = N.AggregationID ))
		  and (    ( A.CanEditMetadata = 'true' )
		        or ( A.CanEditBehaviors = 'true' )
		        or ( A.CanPerformQc = 'true' )
		        or ( A.CanUploadFiles = 'true' )
		        or ( A.CanChangeVisibility = 'true' )
		        or ( A.IsCurator = 'true' )
		        or ( A.IsAdmin = 'true' ));
	end if;
END $$;


/**************************************************************************/
/**                                                                      **/
/**   Update Database Version                                            **/
/**                                                                      **/
/**************************************************************************/

DO $$
BEGIN
  IF (select count(*) from SobekCM_Database_Version) = 0 THEN
    insert into SobekCM_Database_Version (Major_Version, Minor_Version, Release_Phase)
    values (5, 2, '0');
  ELSE
    update SobekCM_Database_Version
    set Major_Version = 5, Minor_Version = 2, Release_Phase = '0';
  END IF;
END $$;
