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


-- Seeds the install-wide default result fields, which collections use for any result view without
-- fields of their own.  These were seeded by the SQL Server Upgrade_to_Ver500.sql (for 4.x
-- databases), but never by the 5.x complete scripts, so every database built from scratch since 5.0
-- had an empty table and fell back to a list hardcoded in the engine.  Same list as that 5.0
-- upgrade (with Publication Date moved to its intended display order of 3), plus the VRA Core
-- fields.  Added for every result type, including THUMBNAIL, whose hover tooltip shows these
-- fields.  Only runs if the table is empty, so any customized defaults are left alone.
DO $$
BEGIN
	IF NOT EXISTS ( select 1 from SobekCM_Item_Aggregation_Default_Result_Fields ) THEN
		insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder )
		select R.ItemAggregationResultTypeID, D.MetadataTypeID, D.DisplayOrder
		from SobekCM_Item_Aggregation_Result_Types R
		cross join ( values
			(   4,  1 ),   -- Creator
			(   5,  2 ),   -- Publisher
			(  24,  3 ),   -- Publication Date
			(   2,  4 ),   -- Resource Type
			(  22,  5 ),   -- Format (displays as Description)
			(  38,  6 ),   -- Edition
			(  15,  7 ),   -- Source Institution
			(  16,  8 ),   -- Holding Location
			(  21,  9 ),   -- Donor
			(   7, 10 ),   -- Subject Keyword
			(  10, 11 ),   -- Spatial Coverage
			(   8, 12 ),   -- Genre (displays as Material Type)
			(   3, 13 ),   -- Language
			(  52, 14 ),   -- VRA Core: Material
			( 118, 15 ),   -- VRA Core: Measurements
			(  54, 16 ),   -- VRA Core: Technique
			(  53, 17 ),   -- VRA Core: Style Period
			(  50, 18 ),   -- VRA Core: Cultural Context
			(  51, 19 )    -- VRA Core: Inscription
		) as D ( MetadataTypeID, DisplayOrder )
		where exists ( select 1 from SobekCM_Metadata_Types T where T.MetadataTypeID = D.MetadataTypeID );
	END IF;
END $$;


-- Adds SobekCM_Save_Item_Aggregation_Result_Fields, so collection admins can choose the result fields shown
-- with each title in a collection's brief results view (and in the thumbnail view's hover tooltip, which shows
-- the same fields) from the Results tab.  Nothing wrote to SobekCM_Item_Aggregation_Result_Fields before this,
-- so every collection used the install-wide defaults.  The fields are stored against the collection's BRIEF
-- and THUMBNAIL result views, and the TABLE view (which does not show them) keeps the defaults.

-- Saves the result fields customized for an item aggregation's brief and thumbnail results views.  With
-- p_use_defaults set, just removes any customized fields, so the install-wide defaults are used.  Otherwise
-- each line of p_fields is a metadata type id, a tab, then an optional override display term (blank to use
-- the standard term), in display order.
CREATE OR REPLACE FUNCTION SobekCM_Save_Item_Aggregation_Result_Fields(
	p_code varchar(20),
	p_use_defaults boolean,
	p_fields text
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_id integer;
	v_line text;
	v_idtext text;
	v_metadataid smallint;
	v_term varchar(255);
	v_order integer := 0;
BEGIN
	-- Only continue if there is a match on the aggregation code
	select AggregationID into v_id from SobekCM_Item_Aggregation where Code = p_code;
	if ( v_id is null ) then
		return;
	end if;

	-- Remove the existing customized fields from this aggregation's brief and thumbnail views
	delete from SobekCM_Item_Aggregation_Result_Fields
	where ItemAggregationResultID in (
		select V.ItemAggregationResultID
		from SobekCM_Item_Aggregation_Result_Views V, SobekCM_Item_Aggregation_Result_Types T
		where ( V.AggregationID = v_id )
		  and ( V.ItemAggregationResultTypeID = T.ItemAggregationResultTypeID )
		  and ( T.ResultType in ( 'BRIEF', 'THUMBNAIL' )));

	-- Add the new fields, unless going back to the defaults
	if ( coalesce(p_use_defaults, false) = false ) then
		foreach v_line in array string_to_array(coalesce(p_fields, ''), E'\n') loop
			-- Split the line into the metadata type id and the override display term
			v_idtext := trim(split_part(v_line, E'\t', 1));
			if ( v_idtext !~ '^[0-9]{1,4}$' ) then
				continue;
			end if;
			v_metadataid := v_idtext::smallint;
			v_term := left(trim(split_part(v_line, E'\t', 2)), 255);

			-- Add this field to each view, skipping unknown metadata types and repeats
			if (( exists ( select 1 from SobekCM_Metadata_Types where MetadataTypeID = v_metadataid ))
			  and ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Fields F, SobekCM_Item_Aggregation_Result_Views V
			                     where F.ItemAggregationResultID = V.ItemAggregationResultID and V.AggregationID = v_id and F.MetadataTypeID = v_metadataid ))) then
				v_order := v_order + 1;

				insert into SobekCM_Item_Aggregation_Result_Fields ( ItemAggregationResultID, MetadataTypeID, OverrideDisplayTerm, DisplayOrder, DisplayOptions )
				select V.ItemAggregationResultID, v_metadataid, nullif(v_term, ''), v_order, null
				from SobekCM_Item_Aggregation_Result_Views V, SobekCM_Item_Aggregation_Result_Types T
				where ( V.AggregationID = v_id )
				  and ( V.ItemAggregationResultTypeID = T.ItemAggregationResultTypeID )
				  and ( T.ResultType in ( 'BRIEF', 'THUMBNAIL' ));
			end if;
		end loop;
	end if;
END;
$$;


-- Lets user admins see and change whether a user is active.  Deactivated users (isActive = false)
-- cannot log on by any method, since every user fetch function already filters on isActive.
--   * mySobek_Get_User_By_UserID gains an optional p_include_inactive parameter (default false, so
--     every existing caller is unchanged) and now returns the isActive column
--   * mySobek_Get_All_Users now returns the isActive column, for the active/all filter on the list
--   * New mySobek_Set_User_Active sets the flag
-- Both changed functions change signature or return type, so they are dropped first.  The functions
-- that call mySobek_Get_User_By_UserID look it up by name at run time, so they are unaffected.

DROP FUNCTION IF EXISTS mySobek_Get_User_By_UserID(integer);

CREATE OR REPLACE FUNCTION mySobek_Get_User_By_UserID(
	p_userid integer,
	p_include_inactive boolean DEFAULT false,
	OUT cur_user refcursor,
	OUT cur_templates refcursor,
	OUT cur_default_metadata refcursor,
	OUT cur_submitted_bibids refcursor,
	OUT cur_editable_regex refcursor,
	OUT cur_aggregations refcursor,
	OUT cur_folders refcursor,
	OUT cur_folder_items refcursor,
	OUT cur_user_groups refcursor,
	OUT cur_settings refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	-- Get the basic user information
	OPEN cur_user FOR
	select UserID, coalesce(ShibbID,'') as ShibbID, coalesce(UserName,'') as UserName, coalesce(EmailAddress,'') as EmailAddress,
	  coalesce(FirstName,'') as FirstName, coalesce(LastName,'') as LastName, Note_Length,
	  Can_Make_Folders_Public, isTemporary_Password, sendEmailOnSubmission, Can_Submit_Items,
	  coalesce(NickName,'') as NickName, coalesce(Organization, '') as Organization, coalesce(College,'') as College,
	  coalesce(Department,'') as Department, coalesce(Unit,'') as Unit, coalesce(Default_Rights,'') as Rights, coalesce(UI_Language, '') as Language,
	  Internal_User, OrganizationCode, EditTemplate, EditTemplateMarc, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms,
	  ( select COUNT(*) from mySobek_User_Description_Tags T where T.UserID=U.UserID) as Descriptions,
	  Receive_Stats_Emails, Has_Item_Stats, Can_Delete_All_Items, ScanningTechnician, ProcessingTechnician, coalesce(InternalNotes,'') as InternalNotes,
	  IsHostAdmin, IsUserAdmin, coalesce(Password,'') as Password, coalesce(ExternalProviderCode,'') as ExternalProviderCode, coalesce(ExternalSubjectId,'') as ExternalSubjectId,
	  AuthenticationSource, isActive
	from mySobek_User U
	where ( UserID = p_userid ) and (( isActive = 'true' ) or ( p_include_inactive ));

	-- Get the templates
	OPEN cur_templates FOR
	select T.TemplateCode, T.TemplateName, 'false' as GroupDefined, DefaultTemplate
	from mySobek_Template T, mySobek_User_Template_Link L
	where ( L.UserID = p_userid ) and ( L.TemplateID = T.TemplateID )
	union
	select T.TemplateCode, T.TemplateName, 'true' as GroupDefined, 'false'
	from mySobek_Template T, mySobek_User_Group_Template_Link TL, mySobek_User_Group_Link GL
	where ( GL.UserID = p_userid ) and ( GL.UserGroupID = TL.UserGroupID ) and ( TL.TemplateID = T.TemplateID )
	order by DefaultTemplate DESC, TemplateCode ASC;

	-- Get the default metadata
	OPEN cur_default_metadata FOR
	select P.MetadataCode, P.MetadataName, 'false' as GroupDefined, CurrentlySelected
	from mySobek_DefaultMetadata P, mySobek_User_DefaultMetadata_Link L
	where ( L.UserID = p_userid ) and ( L.DefaultMetadataID = P.DefaultMetadataID )
	union
	select P.MetadataCode, P.MetadataName, 'true' as GroupDefined, 'false'
	from mySobek_DefaultMetadata P, mySobek_User_Group_DefaultMetadata_Link PL, mySobek_User_Group_Link GL
	where ( GL.UserID = p_userid ) and ( GL.UserGroupID = PL.UserGroupID ) and ( PL.DefaultMetadataID = P.DefaultMetadataID )
	order by CurrentlySelected DESC, MetadataCode ASC;

	-- Get the bib id's of items submitted
	OPEN cur_submitted_bibids FOR
	select distinct( G.BibID )
	from mySobek_User_Folder F, mySobek_User_Item B, SobekCM_Item I, SobekCM_Item_Group G
	where ( F.UserID = p_userid ) and ( B.UserFolderID = F.UserFolderID ) and ( F.FolderName = 'Submitted Items' ) and ( B.ItemID = I.ItemID ) and ( I.GroupID = G.GroupID );

	-- Get the regular expression for editable items
	OPEN cur_editable_regex FOR
	select R.EditableRegex, 'false' as GroupDefined, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from mySobek_Editable_Regex R, mySobek_User_Editable_Link L
	where ( L.UserID = p_userid ) and ( L.EditableID = R.EditableID )
	union
	select R.EditableRegex, 'true' as GroupDefined, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from mySobek_Editable_Regex R, mySobek_User_Group_Editable_Link L, mySobek_User_Group_Link GL
	where ( GL.UserID = p_userid ) and ( GL.UserGroupID = L.UserGroupID ) and ( L.EditableID = R.EditableID );

	-- Get the list of aggregations associated with this user
	OPEN cur_aggregations FOR
	select A.Code, A.Name, L.CanSelect, L.CanEditItems, L.IsAdmin AS IsAggregationAdmin, L.OnHomePage, L.IsCurator AS IsCollectionManager, 'false' as GroupDefined, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from SobekCM_Item_Aggregation A, mySobek_User_Edit_Aggregation L
	where  ( L.AggregationID = A.AggregationID ) and ( L.UserID = p_userid )
	union
	select A.Code, A.Name, L.CanSelect, L.CanEditItems, L.IsAdmin AS IsAggregationAdmin, 'false' as OnHomePage, L.IsCurator AS IsCollectionManager, 'true' as GroupDefined, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from SobekCM_Item_Aggregation A, mySobek_User_Group_Edit_Aggregation L, mySobek_User_Group_Link GL
	where  ( L.AggregationID = A.AggregationID ) and ( GL.UserID = p_userid ) and ( GL.UserGroupID = L.UserGroupID );

	-- Return the names of all the folders
	OPEN cur_folders FOR
	select F.FolderName, F.UserFolderID, coalesce(F.ParentFolderID,-1) as ParentFolderID, F.isPublic
	from mySobek_User_Folder F
	where ( F.UserID=p_userid );

	-- Get the list of all items associated with a user folder (other than submitted items)
	OPEN cur_folder_items FOR
	select G.BibID, I.VID
	from mySobek_User_Folder F, mySobek_User_Item B, SobekCM_Item I, SobekCM_Item_Group G
	where ( F.UserID = p_userid ) and ( B.UserFolderID = F.UserFolderID ) and ( F.FolderName != 'Submitted Items' ) and ( B.ItemID = I.ItemID ) and ( I.GroupID = G.GroupID );

	-- Get the list of all user groups associated with this user
	OPEN cur_user_groups FOR
	select G.GroupName, G.Can_Submit_Items, G.Internal_User, G.IsSystemAdmin, G.IsPortalAdmin, G.Include_Tracking_Standard_Forms, G.UserGroupID
	from mySobek_User_Group G, mySobek_User_Group_Link L
	where ( G.UserGroupID = L.UserGroupID )
	  and ( L.UserID = p_userid );

	-- Get the user settings
	OPEN cur_settings FOR
	select * from mySobek_User_Settings where UserID=p_userid order by Setting_Key;

	-- Update the user table to include this as the last activity
	update mySobek_User
	set LastActivity = now()
	where UserID=p_userid;
END;
$$;


DROP FUNCTION IF EXISTS mySobek_Get_All_Users();

CREATE OR REPLACE FUNCTION mySobek_Get_All_Users()
RETURNS TABLE (
	UserID integer,
	Full_Name text,
	UserName varchar(50),
	EmailAddress varchar(100),
	PendingRequests bigint,
	isActive boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	with pending_cte as
	(
		select UserID, count(*) as PendingRequests
		from mySobek_User_Request
		where Pending='true'
		group by UserID
	)
	select U.UserID, U.LastName || ', ' || U.FirstName AS Full_Name, U.UserName, U.EmailAddress, coalesce(R.PendingRequests,0) as PendingRequests, U.isActive
	from mySobek_User U left join
		 pending_cte R on U.UserID = R.UserID
	order by Full_Name;
END;
$$;


-- Activates or deactivates a user.  A deactivated user cannot log on by any method.
CREATE OR REPLACE FUNCTION mySobek_Set_User_Active(
	p_userid integer,
	p_isactive boolean
)
RETURNS void
LANGUAGE sql
AS $$
	update mySobek_User
	set isActive = p_isactive
	where UserID = p_userid;
$$;


-- Adds user news: short HTML messages shown in a banner at the very top of every page, for the
-- users each message targets, until that user closes it.  Once closed, a message is not shown to
-- that user again.  A message can target everyone, including visitors who are not logged on (for
-- example 'The library will be closed on Labor Day'), who close it with a cookie instead.  Also adds
-- the News Administrator role (mySobek_User.IsNewsAdmin), who can manage the news and nothing else.

CREATE TABLE IF NOT EXISTS mySobek_News(
	NewsID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Title varchar(255) NOT NULL,
	Body text NOT NULL,
	ForEveryone boolean NOT NULL DEFAULT false,
	ForAllUsers boolean NOT NULL DEFAULT false,
	ForAdmins boolean NOT NULL DEFAULT false,
	ForCollectionManagers boolean NOT NULL DEFAULT false,
	StartDate date NOT NULL DEFAULT current_date,
	EndDate date NULL,
	IsActive boolean NOT NULL DEFAULT true,
	DateCreated timestamp NOT NULL DEFAULT now(),
	CreatedBy varchar(100) NOT NULL DEFAULT '',
	DateModified timestamp NULL,
 CONSTRAINT PK_mySobek_News PRIMARY KEY ( NewsID )
);

-- No foreign key to mySobek_User_Group, so deleting a user group needs no change.  A link to a
-- deleted group simply never matches anyone.
CREATE TABLE IF NOT EXISTS mySobek_News_User_Group_Link(
	NewsID integer NOT NULL,
	UserGroupID integer NOT NULL,
 CONSTRAINT PK_mySobek_News_User_Group_Link PRIMARY KEY ( NewsID, UserGroupID ),
 CONSTRAINT FK_mySobek_News_User_Group_Link_News FOREIGN KEY ( NewsID ) REFERENCES mySobek_News ( NewsID )
);

CREATE TABLE IF NOT EXISTS mySobek_News_User_Dismissed(
	NewsID integer NOT NULL,
	UserID integer NOT NULL,
	DateDismissed timestamp NOT NULL DEFAULT now(),
 CONSTRAINT PK_mySobek_News_User_Dismissed PRIMARY KEY ( UserID, NewsID ),
 CONSTRAINT FK_mySobek_News_User_Dismissed_News FOREIGN KEY ( NewsID ) REFERENCES mySobek_News ( NewsID )
);

-- The News Administrator role
ALTER TABLE mySobek_User ADD COLUMN IF NOT EXISTS IsNewsAdmin boolean NOT NULL DEFAULT false;


-- Returns IsNewsAdmin as well.  Redefines the version from the user active flag change above.
CREATE OR REPLACE FUNCTION mySobek_Get_User_By_UserID(
	p_userid integer,
	p_include_inactive boolean DEFAULT false,
	OUT cur_user refcursor,
	OUT cur_templates refcursor,
	OUT cur_default_metadata refcursor,
	OUT cur_submitted_bibids refcursor,
	OUT cur_editable_regex refcursor,
	OUT cur_aggregations refcursor,
	OUT cur_folders refcursor,
	OUT cur_folder_items refcursor,
	OUT cur_user_groups refcursor,
	OUT cur_settings refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	-- Get the basic user information
	OPEN cur_user FOR
	select UserID, coalesce(ShibbID,'') as ShibbID, coalesce(UserName,'') as UserName, coalesce(EmailAddress,'') as EmailAddress,
	  coalesce(FirstName,'') as FirstName, coalesce(LastName,'') as LastName, Note_Length,
	  Can_Make_Folders_Public, isTemporary_Password, sendEmailOnSubmission, Can_Submit_Items,
	  coalesce(NickName,'') as NickName, coalesce(Organization, '') as Organization, coalesce(College,'') as College,
	  coalesce(Department,'') as Department, coalesce(Unit,'') as Unit, coalesce(Default_Rights,'') as Rights, coalesce(UI_Language, '') as Language,
	  Internal_User, OrganizationCode, EditTemplate, EditTemplateMarc, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms,
	  ( select COUNT(*) from mySobek_User_Description_Tags T where T.UserID=U.UserID) as Descriptions,
	  Receive_Stats_Emails, Has_Item_Stats, Can_Delete_All_Items, ScanningTechnician, ProcessingTechnician, coalesce(InternalNotes,'') as InternalNotes,
	  IsHostAdmin, IsUserAdmin, coalesce(Password,'') as Password, coalesce(ExternalProviderCode,'') as ExternalProviderCode, coalesce(ExternalSubjectId,'') as ExternalSubjectId,
	  AuthenticationSource, isActive, IsNewsAdmin
	from mySobek_User U
	where ( UserID = p_userid ) and (( isActive = 'true' ) or ( p_include_inactive ));

	-- Get the templates
	OPEN cur_templates FOR
	select T.TemplateCode, T.TemplateName, 'false' as GroupDefined, DefaultTemplate
	from mySobek_Template T, mySobek_User_Template_Link L
	where ( L.UserID = p_userid ) and ( L.TemplateID = T.TemplateID )
	union
	select T.TemplateCode, T.TemplateName, 'true' as GroupDefined, 'false'
	from mySobek_Template T, mySobek_User_Group_Template_Link TL, mySobek_User_Group_Link GL
	where ( GL.UserID = p_userid ) and ( GL.UserGroupID = TL.UserGroupID ) and ( TL.TemplateID = T.TemplateID )
	order by DefaultTemplate DESC, TemplateCode ASC;

	-- Get the default metadata
	OPEN cur_default_metadata FOR
	select P.MetadataCode, P.MetadataName, 'false' as GroupDefined, CurrentlySelected
	from mySobek_DefaultMetadata P, mySobek_User_DefaultMetadata_Link L
	where ( L.UserID = p_userid ) and ( L.DefaultMetadataID = P.DefaultMetadataID )
	union
	select P.MetadataCode, P.MetadataName, 'true' as GroupDefined, 'false'
	from mySobek_DefaultMetadata P, mySobek_User_Group_DefaultMetadata_Link PL, mySobek_User_Group_Link GL
	where ( GL.UserID = p_userid ) and ( GL.UserGroupID = PL.UserGroupID ) and ( PL.DefaultMetadataID = P.DefaultMetadataID )
	order by CurrentlySelected DESC, MetadataCode ASC;

	-- Get the bib id's of items submitted
	OPEN cur_submitted_bibids FOR
	select distinct( G.BibID )
	from mySobek_User_Folder F, mySobek_User_Item B, SobekCM_Item I, SobekCM_Item_Group G
	where ( F.UserID = p_userid ) and ( B.UserFolderID = F.UserFolderID ) and ( F.FolderName = 'Submitted Items' ) and ( B.ItemID = I.ItemID ) and ( I.GroupID = G.GroupID );

	-- Get the regular expression for editable items
	OPEN cur_editable_regex FOR
	select R.EditableRegex, 'false' as GroupDefined, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from mySobek_Editable_Regex R, mySobek_User_Editable_Link L
	where ( L.UserID = p_userid ) and ( L.EditableID = R.EditableID )
	union
	select R.EditableRegex, 'true' as GroupDefined, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from mySobek_Editable_Regex R, mySobek_User_Group_Editable_Link L, mySobek_User_Group_Link GL
	where ( GL.UserID = p_userid ) and ( GL.UserGroupID = L.UserGroupID ) and ( L.EditableID = R.EditableID );

	-- Get the list of aggregations associated with this user
	OPEN cur_aggregations FOR
	select A.Code, A.Name, L.CanSelect, L.CanEditItems, L.IsAdmin AS IsAggregationAdmin, L.OnHomePage, L.IsCurator AS IsCollectionManager, 'false' as GroupDefined, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from SobekCM_Item_Aggregation A, mySobek_User_Edit_Aggregation L
	where  ( L.AggregationID = A.AggregationID ) and ( L.UserID = p_userid )
	union
	select A.Code, A.Name, L.CanSelect, L.CanEditItems, L.IsAdmin AS IsAggregationAdmin, 'false' as OnHomePage, L.IsCurator AS IsCollectionManager, 'true' as GroupDefined, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from SobekCM_Item_Aggregation A, mySobek_User_Group_Edit_Aggregation L, mySobek_User_Group_Link GL
	where  ( L.AggregationID = A.AggregationID ) and ( GL.UserID = p_userid ) and ( GL.UserGroupID = L.UserGroupID );

	-- Return the names of all the folders
	OPEN cur_folders FOR
	select F.FolderName, F.UserFolderID, coalesce(F.ParentFolderID,-1) as ParentFolderID, F.isPublic
	from mySobek_User_Folder F
	where ( F.UserID=p_userid );

	-- Get the list of all items associated with a user folder (other than submitted items)
	OPEN cur_folder_items FOR
	select G.BibID, I.VID
	from mySobek_User_Folder F, mySobek_User_Item B, SobekCM_Item I, SobekCM_Item_Group G
	where ( F.UserID = p_userid ) and ( B.UserFolderID = F.UserFolderID ) and ( F.FolderName != 'Submitted Items' ) and ( B.ItemID = I.ItemID ) and ( I.GroupID = G.GroupID );

	-- Get the list of all user groups associated with this user
	OPEN cur_user_groups FOR
	select G.GroupName, G.Can_Submit_Items, G.Internal_User, G.IsSystemAdmin, G.IsPortalAdmin, G.Include_Tracking_Standard_Forms, G.UserGroupID
	from mySobek_User_Group G, mySobek_User_Group_Link L
	where ( G.UserGroupID = L.UserGroupID )
	  and ( L.UserID = p_userid );

	-- Get the user settings
	OPEN cur_settings FOR
	select * from mySobek_User_Settings where UserID=p_userid order by Setting_Key;

	-- Update the user table to include this as the last activity
	update mySobek_User
	set LastActivity = now()
	where UserID=p_userid;
END;
$$;


-- Edits the permission flags for a user.  p_is_news_admin is optional, and NULL leaves it unchanged.
-- Dropped and recreated, since the parameter list changes.
DROP FUNCTION IF EXISTS mySobek_Update_User(integer, boolean, boolean, boolean, boolean, boolean, boolean, boolean, boolean, boolean, varchar, varchar, boolean, boolean, boolean);

CREATE OR REPLACE FUNCTION mySobek_Update_User(
	p_userid integer,
	p_can_submit boolean,
	p_is_internal boolean,
	p_can_edit_all boolean,
	p_can_delete_all boolean,
	p_is_user_admin boolean,
	p_is_portal_admin boolean,
	p_is_system_admin boolean,
	p_is_host_admin boolean,
	p_include_tracking_standard_forms boolean,
	p_edit_template varchar(20),
	p_edit_template_marc varchar(20),
	p_clear_projects_templates boolean,
	p_clear_aggregation_links boolean,
	p_clear_user_groups boolean,
	p_is_news_admin boolean DEFAULT null
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	update mySobek_User
	set Can_Submit_Items=p_can_submit, Internal_User=p_is_internal,
		IsPortalAdmin=p_is_portal_admin, IsSystemAdmin=p_is_system_admin,
		Include_Tracking_Standard_Forms=p_include_tracking_standard_forms,
		EditTemplate=p_edit_template, Can_Delete_All_Items = p_can_delete_all,
		EditTemplateMarc=p_edit_template_marc, IsHostAdmin=p_is_host_admin,
		IsUserAdmin=p_is_user_admin, IsNewsAdmin=coalesce(p_is_news_admin, IsNewsAdmin)
	where UserID=p_userid;

	if ( p_can_edit_all ) then
		if ( ( select count(*) from mySobek_User_Editable_Link where EditableID=1 and UserID=p_userid ) = 0 ) then
			insert into mySobek_User_Editable_Link ( UserID, EditableID )
			values ( p_userid, 1 );
		end if;
	else
		delete from mySobek_User_Editable_Link where EditableID = 1 and UserID=p_userid;
	end if;

	if ( p_clear_projects_templates ) then
		delete from mySobek_User_DefaultMetadata_Link where UserID=p_userid;
		delete from mySobek_User_Template_Link where UserID=p_userid;
	end if;

	if ( p_clear_aggregation_links ) then
		delete from mySobek_User_Edit_Aggregation where UserID=p_userid;
	end if;

	if ( p_clear_user_groups ) then
		delete from mySobek_User_Group_Link where UserID=p_userid;
	end if;
END;
$$;


-- Gets the active news, within its display dates, that this user has not closed yet.  Who each
-- message targets is returned too, and the application picks the ones that apply to this user,
-- using the same role flags it uses for everything else.  Newest first.  Pass -1 to get only the
-- news for everyone, for visitors who are not logged on.
CREATE OR REPLACE FUNCTION mySobek_Get_Pending_News(
	p_userid integer
)
RETURNS TABLE (
	NewsID integer,
	Title varchar(255),
	Body text,
	ForEveryone boolean,
	ForAllUsers boolean,
	ForAdmins boolean,
	ForCollectionManagers boolean,
	StartDate date,
	EndDate date,
	IsActive boolean,
	UserGroupIDs text
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	select N.NewsID, N.Title, N.Body, N.ForEveryone, N.ForAllUsers, N.ForAdmins, N.ForCollectionManagers,
	  N.StartDate, N.EndDate, N.IsActive,
	  coalesce(( select string_agg(L.UserGroupID::text, ',') from mySobek_News_User_Group_Link L where L.NewsID = N.NewsID ), '') as UserGroupIDs
	from mySobek_News N
	where ( N.IsActive = true )
	  and ( N.StartDate <= current_date )
	  and (( N.EndDate is null ) or ( N.EndDate >= current_date ))
	  and (( p_userid > 0 ) or ( N.ForEveryone = true ))
	  and ( not exists ( select 1 from mySobek_News_User_Dismissed D where D.UserID = p_userid and D.NewsID = N.NewsID ))
	order by N.StartDate DESC, N.NewsID DESC;
END;
$$;


-- Records that a user closed a news message, so it is not shown to them again
CREATE OR REPLACE FUNCTION mySobek_Dismiss_News(
	p_userid integer,
	p_newsid integer
)
RETURNS void
LANGUAGE sql
AS $$
	insert into mySobek_News_User_Dismissed ( NewsID, UserID, DateDismissed )
	select p_newsid, p_userid, now()
	where exists ( select 1 from mySobek_News where NewsID = p_newsid )
	on conflict do nothing;
$$;


-- Gets every news message, for the news admin screen, with how many users have closed each one
CREATE OR REPLACE FUNCTION mySobek_Get_All_News()
RETURNS TABLE (
	NewsID integer,
	Title varchar(255),
	Body text,
	ForEveryone boolean,
	ForAllUsers boolean,
	ForAdmins boolean,
	ForCollectionManagers boolean,
	StartDate date,
	EndDate date,
	IsActive boolean,
	DateCreated timestamp,
	CreatedBy varchar(100),
	DateModified timestamp,
	UserGroupIDs text,
	DismissedCount bigint
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	select N.NewsID, N.Title, N.Body, N.ForEveryone, N.ForAllUsers, N.ForAdmins, N.ForCollectionManagers,
	  N.StartDate, N.EndDate, N.IsActive, N.DateCreated, N.CreatedBy, N.DateModified,
	  coalesce(( select string_agg(L.UserGroupID::text, ',') from mySobek_News_User_Group_Link L where L.NewsID = N.NewsID ), '') as UserGroupIDs,
	  ( select count(*) from mySobek_News_User_Dismissed D where D.NewsID = N.NewsID ) as DismissedCount
	from mySobek_News N
	order by N.StartDate DESC, N.NewsID DESC;
END;
$$;


-- Adds a new news message (when p_newsid does not exist yet) or edits an existing one.  p_usergroupids
-- is a comma-separated list of the user groups targeted, and replaces any existing group links.
-- Editing a message does not show it again to users who already closed it; call
-- mySobek_Reset_News_Dismissals for that.
CREATE OR REPLACE FUNCTION mySobek_Save_News(
	p_newsid integer,
	p_title varchar(255),
	p_body text,
	p_foreveryone boolean,
	p_forallusers boolean,
	p_foradmins boolean,
	p_forcollectionmanagers boolean,
	p_startdate timestamp,
	p_enddate timestamp,
	p_isactive boolean,
	p_usergroupids varchar,
	p_username varchar(100),
	OUT p_newid integer
)
LANGUAGE plpgsql
AS $$
BEGIN
	if exists ( select 1 from mySobek_News where NewsID = p_newsid ) then
		update mySobek_News
		set Title=p_title, Body=p_body, ForEveryone=p_foreveryone, ForAllUsers=p_forallusers, ForAdmins=p_foradmins,
		    ForCollectionManagers=p_forcollectionmanagers, StartDate=coalesce(p_startdate::date, StartDate), EndDate=p_enddate::date,
		    IsActive=p_isactive, DateModified=now()
		where NewsID = p_newsid;

		p_newid := p_newsid;
	else
		insert into mySobek_News ( Title, Body, ForEveryone, ForAllUsers, ForAdmins, ForCollectionManagers,
		    StartDate, EndDate, IsActive, DateCreated, CreatedBy )
		values ( p_title, p_body, p_foreveryone, p_forallusers, p_foradmins, p_forcollectionmanagers,
		    coalesce(p_startdate::date, current_date), p_enddate::date, p_isactive, now(), coalesce(p_username, ''))
		returning NewsID into p_newid;
	end if;

	-- Replace the user group links
	delete from mySobek_News_User_Group_Link where NewsID = p_newid;

	insert into mySobek_News_User_Group_Link ( NewsID, UserGroupID )
	select distinct p_newid, trim(G.id)::integer
	from unnest(string_to_array(coalesce(p_usergroupids, ''), ',')) as G(id)
	where trim(G.id) ~ '^[0-9]+$';
END;
$$;


-- Deletes a news message, along with its user group links and the record of who closed it
CREATE OR REPLACE FUNCTION mySobek_Delete_News(
	p_newsid integer
)
RETURNS void
LANGUAGE sql
AS $$
	delete from mySobek_News_User_Dismissed where NewsID = p_newsid;
	delete from mySobek_News_User_Group_Link where NewsID = p_newsid;
	delete from mySobek_News where NewsID = p_newsid;
$$;


-- Forgets who closed a news message, so it is shown again to everyone it targets
CREATE OR REPLACE FUNCTION mySobek_Reset_News_Dismissals(
	p_newsid integer
)
RETURNS void
LANGUAGE sql
AS $$
	delete from mySobek_News_User_Dismissed where NewsID = p_newsid;
$$;


-- Release news for the administrators.  Each upgrade script can add one of these.  The title
-- check means running the script twice does not add it twice.  It stops showing after 90 days,
-- so administrators added long after the upgrade are not told about it.
DO $$
BEGIN
  IF NOT EXISTS ( select 1 from mySobek_News where Title = 'SobekCM has been upgraded to version 5.2.0' ) THEN
    PERFORM mySobek_Save_News( -1, 'SobekCM has been upgraded to version 5.2.0',
      '<p>This site is now running SobekCM 5.2.0, a new release. Changes you may notice:</p><ul><li><strong>Site news:</strong> messages like this one now appear at the top of the page for the people they are meant for, until each person closes them. Use <em>Admin &gt; Site News</em> to post your own, for everyone (such as a holiday closing) or just certain users. The new <em>News Administrator</em> role lets someone manage the news without any other administrative rights.</li><li><strong>Collection result fields:</strong> the <em>Results</em> tab of each collection can now choose the fields shown with each title in the brief view and the thumbnail tooltip.</li><li><strong>Inactive users:</strong> the users admin screen can now deactivate a user, who can then no longer log on.</li><li><strong>New institutions</strong> added automatically when an item is loaded now get the same facets, result views and permissions as any other new collection.</li></ul><p>For more information, see the <a href="https://sobekrepository.org/sobekcm/currentversion/">Release Notes</a>.</p>',
      false, false, true, false,
      null, (current_date + 90)::timestamp, true, '', 'Upgrade_to_Ver520_PostgreSQL.sql' );
  END IF;
END $$;


-- Adds a System User flag (mySobek_User.IsSystemUser), for accounts that must never be locked out
-- (e.g. service/integration accounts). Not setable anywhere in the UI - defaults to false and is
-- only ever set directly against the database. When set, the users admin screen's deactivate
-- checkbox is replaced with a "System users cannot be deactivated" message, and the flag cannot be
-- changed through a form postback either. System users are also hidden entirely from the users
-- admin list for anyone who is not the top-level admin (Host Administrator if hosted, otherwise
-- System Administrator); the top-level admin can choose to show them via a System Users filter.
--   * mySobek_Get_User_By_UserID now returns IsSystemUser, so the users admin screen can see it.
--     Signature is unchanged, so no DROP FUNCTION is needed - just CREATE OR REPLACE.
--   * mySobek_Get_All_Users now returns IsSystemUser too, for the users admin list and its filter.
--     Return type changes, so it is dropped and recreated, same as when isActive was added to it.

ALTER TABLE mySobek_User ADD COLUMN IF NOT EXISTS IsSystemUser boolean NOT NULL DEFAULT false;


-- Returns IsSystemUser as well. Redefines the version from the user news change above.
CREATE OR REPLACE FUNCTION mySobek_Get_User_By_UserID(
	p_userid integer,
	p_include_inactive boolean DEFAULT false,
	OUT cur_user refcursor,
	OUT cur_templates refcursor,
	OUT cur_default_metadata refcursor,
	OUT cur_submitted_bibids refcursor,
	OUT cur_editable_regex refcursor,
	OUT cur_aggregations refcursor,
	OUT cur_folders refcursor,
	OUT cur_folder_items refcursor,
	OUT cur_user_groups refcursor,
	OUT cur_settings refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	-- Get the basic user information
	OPEN cur_user FOR
	select UserID, coalesce(ShibbID,'') as ShibbID, coalesce(UserName,'') as UserName, coalesce(EmailAddress,'') as EmailAddress,
	  coalesce(FirstName,'') as FirstName, coalesce(LastName,'') as LastName, Note_Length,
	  Can_Make_Folders_Public, isTemporary_Password, sendEmailOnSubmission, Can_Submit_Items,
	  coalesce(NickName,'') as NickName, coalesce(Organization, '') as Organization, coalesce(College,'') as College,
	  coalesce(Department,'') as Department, coalesce(Unit,'') as Unit, coalesce(Default_Rights,'') as Rights, coalesce(UI_Language, '') as Language,
	  Internal_User, OrganizationCode, EditTemplate, EditTemplateMarc, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms,
	  ( select COUNT(*) from mySobek_User_Description_Tags T where T.UserID=U.UserID) as Descriptions,
	  Receive_Stats_Emails, Has_Item_Stats, Can_Delete_All_Items, ScanningTechnician, ProcessingTechnician, coalesce(InternalNotes,'') as InternalNotes,
	  IsHostAdmin, IsUserAdmin, coalesce(Password,'') as Password, coalesce(ExternalProviderCode,'') as ExternalProviderCode, coalesce(ExternalSubjectId,'') as ExternalSubjectId,
	  AuthenticationSource, isActive, IsNewsAdmin, IsSystemUser
	from mySobek_User U
	where ( UserID = p_userid ) and (( isActive = 'true' ) or ( p_include_inactive ));

	-- Get the templates
	OPEN cur_templates FOR
	select T.TemplateCode, T.TemplateName, 'false' as GroupDefined, DefaultTemplate
	from mySobek_Template T, mySobek_User_Template_Link L
	where ( L.UserID = p_userid ) and ( L.TemplateID = T.TemplateID )
	union
	select T.TemplateCode, T.TemplateName, 'true' as GroupDefined, 'false'
	from mySobek_Template T, mySobek_User_Group_Template_Link TL, mySobek_User_Group_Link GL
	where ( GL.UserID = p_userid ) and ( GL.UserGroupID = TL.UserGroupID ) and ( TL.TemplateID = T.TemplateID )
	order by DefaultTemplate DESC, TemplateCode ASC;

	-- Get the default metadata
	OPEN cur_default_metadata FOR
	select P.MetadataCode, P.MetadataName, 'false' as GroupDefined, CurrentlySelected
	from mySobek_DefaultMetadata P, mySobek_User_DefaultMetadata_Link L
	where ( L.UserID = p_userid ) and ( L.DefaultMetadataID = P.DefaultMetadataID )
	union
	select P.MetadataCode, P.MetadataName, 'true' as GroupDefined, 'false'
	from mySobek_DefaultMetadata P, mySobek_User_Group_DefaultMetadata_Link PL, mySobek_User_Group_Link GL
	where ( GL.UserID = p_userid ) and ( GL.UserGroupID = PL.UserGroupID ) and ( PL.DefaultMetadataID = P.DefaultMetadataID )
	order by CurrentlySelected DESC, MetadataCode ASC;

	-- Get the bib id's of items submitted
	OPEN cur_submitted_bibids FOR
	select distinct( G.BibID )
	from mySobek_User_Folder F, mySobek_User_Item B, SobekCM_Item I, SobekCM_Item_Group G
	where ( F.UserID = p_userid ) and ( B.UserFolderID = F.UserFolderID ) and ( F.FolderName = 'Submitted Items' ) and ( B.ItemID = I.ItemID ) and ( I.GroupID = G.GroupID );

	-- Get the regular expression for editable items
	OPEN cur_editable_regex FOR
	select R.EditableRegex, 'false' as GroupDefined, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from mySobek_Editable_Regex R, mySobek_User_Editable_Link L
	where ( L.UserID = p_userid ) and ( L.EditableID = R.EditableID )
	union
	select R.EditableRegex, 'true' as GroupDefined, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from mySobek_Editable_Regex R, mySobek_User_Group_Editable_Link L, mySobek_User_Group_Link GL
	where ( GL.UserID = p_userid ) and ( GL.UserGroupID = L.UserGroupID ) and ( L.EditableID = R.EditableID );

	-- Get the list of aggregations associated with this user
	OPEN cur_aggregations FOR
	select A.Code, A.Name, L.CanSelect, L.CanEditItems, L.IsAdmin AS IsAggregationAdmin, L.OnHomePage, L.IsCurator AS IsCollectionManager, 'false' as GroupDefined, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from SobekCM_Item_Aggregation A, mySobek_User_Edit_Aggregation L
	where  ( L.AggregationID = A.AggregationID ) and ( L.UserID = p_userid )
	union
	select A.Code, A.Name, L.CanSelect, L.CanEditItems, L.IsAdmin AS IsAggregationAdmin, 'false' as OnHomePage, L.IsCurator AS IsCollectionManager, 'true' as GroupDefined, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from SobekCM_Item_Aggregation A, mySobek_User_Group_Edit_Aggregation L, mySobek_User_Group_Link GL
	where  ( L.AggregationID = A.AggregationID ) and ( GL.UserID = p_userid ) and ( GL.UserGroupID = L.UserGroupID );

	-- Return the names of all the folders
	OPEN cur_folders FOR
	select F.FolderName, F.UserFolderID, coalesce(F.ParentFolderID,-1) as ParentFolderID, F.isPublic
	from mySobek_User_Folder F
	where ( F.UserID=p_userid );

	-- Get the list of all items associated with a user folder (other than submitted items)
	OPEN cur_folder_items FOR
	select G.BibID, I.VID
	from mySobek_User_Folder F, mySobek_User_Item B, SobekCM_Item I, SobekCM_Item_Group G
	where ( F.UserID = p_userid ) and ( B.UserFolderID = F.UserFolderID ) and ( F.FolderName != 'Submitted Items' ) and ( B.ItemID = I.ItemID ) and ( I.GroupID = G.GroupID );

	-- Get the list of all user groups associated with this user
	OPEN cur_user_groups FOR
	select G.GroupName, G.Can_Submit_Items, G.Internal_User, G.IsSystemAdmin, G.IsPortalAdmin, G.Include_Tracking_Standard_Forms, G.UserGroupID
	from mySobek_User_Group G, mySobek_User_Group_Link L
	where ( G.UserGroupID = L.UserGroupID )
	  and ( L.UserID = p_userid );

	-- Get the user settings
	OPEN cur_settings FOR
	select * from mySobek_User_Settings where UserID=p_userid order by Setting_Key;

	-- Update the user table to include this as the last activity
	update mySobek_User
	set LastActivity = now()
	where UserID=p_userid;
END;
$$;


-- Returns IsSystemUser as well, for the users admin list's System Users filter. Return type changes,
-- so it is dropped and recreated.
DROP FUNCTION IF EXISTS mySobek_Get_All_Users();

CREATE OR REPLACE FUNCTION mySobek_Get_All_Users()
RETURNS TABLE (
	UserID integer,
	Full_Name text,
	UserName varchar(50),
	EmailAddress varchar(100),
	PendingRequests bigint,
	isActive boolean,
	IsSystemUser boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	with pending_cte as
	(
		select UserID, count(*) as PendingRequests
		from mySobek_User_Request
		where Pending='true'
		group by UserID
	)
	select U.UserID, U.LastName || ', ' || U.FirstName AS Full_Name, U.UserName, U.EmailAddress, coalesce(R.PendingRequests,0) as PendingRequests, U.isActive, U.IsSystemUser
	from mySobek_User U left join
		 pending_cte R on U.UserID = R.UserID
	order by Full_Name;
END;
$$;


-- Adds a per-item "serve files locally" flag, SobekCM_Item.Serve_Files_Locally. When set, an item's
-- WHOLE file folder is kept and served from local disk, even under the GCS Hybrid / GCS Full file
-- system modes. It exists for the handful of items whose viewer loads sub-files by relative path
-- (a self-contained web site, an HTML file with its own images, an open textbook), which GCS can
-- not serve. Replaces the old rule where merely having a WEBSITE / HTML / OPEN_TEXTBOOK /
-- OPEN_DIVISIONS viewer registered forced the whole item local. Defaults to false; the save
-- functions are deliberately NOT changed, so re-saving an item from METS never clears the flag.
-- PostgreSQL port of the SQL Server script, matching it change for change (including the one-time
-- backfill: WEBSITE / OPEN_TEXTBOOK / OPEN_DIVISIONS viewers, plus non-excluded HTML viewers whose
-- attribute names an .htm/.html file).

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_name = 'sobekcm_item' AND column_name = 'serve_files_locally'
  ) THEN
    ALTER TABLE SobekCM_Item ADD COLUMN Serve_Files_Locally boolean NOT NULL DEFAULT false;
  END IF;
END $$;

-- Now also returns Serve_Files_Locally in the main item row
CREATE OR REPLACE FUNCTION SobekCM_Get_Item_Details(
	p_BibID varchar(10),
	p_VID varchar(5)
)
RETURNS SETOF refcursor
LANGUAGE plpgsql
AS $$
DECLARE
	v_ItemID integer;
	cur refcursor;
BEGIN
	if (not exists ( select 1 from SobekCM_Item_Group where BibID = p_BibID )) then
		OPEN cur FOR select 'INVALID BIBID' as ErrorMsg, '' as BibID, '' as VID;
		RETURN NEXT cur;
		RETURN;
	end if;

	if ( not exists ( select 1 from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID=p_BibID and I.VID = p_VID )) then
		OPEN cur FOR
		select 'INVALID VID' as ErrorMsg, p_BibID as BibID, VID
		from SobekCM_Item I, SobekCM_Item_Group G
		where I.GroupID = G.GroupID
		  and G.BibID = p_BibID
		order by VID
		limit 1;
		RETURN NEXT cur;
		RETURN;
	end if;

	if (( select COUNT(*) from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = p_BibID and I.VID = p_VID ) = 1 ) then
		select ItemID into v_ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = p_BibID and I.VID = p_VID;

		OPEN cur FOR
		select U.FirstName, U.NickName, U.LastName, G.BibID, I.VID, T.Description_Tag, T.TagID, T.Date_Modified, U.UserID, coalesce(I.PageCount, 0) as Pages, I.ExposeFullTextForHarvesting
		from mySobek_User U, mySobek_User_Description_Tags T, SobekCM_Item I, SobekCM_Item_Group G
		where ( T.ItemID = v_ItemID )
		  and ( I.ItemID = T.ItemID )
		  and ( I.GroupID = G.GroupID )
		  and ( T.UserID = U.UserID );
		RETURN NEXT cur;

		OPEN cur FOR
		select A.Code, A.Name, A.ShortName, A.Type, A.Map_Search, A.DisplayOptions, A.Items_Can_Be_Described, L.impliedLink, A.Hidden, A.isActive, coalesce(A.External_Link,'') as External_Link
		from SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A
		where ( L.ItemID = v_ItemID )
		  and ( A.AggregationID = L.AggregationID );
		RETURN NEXT cur;

		OPEN cur FOR
		select G.BibID, I.VID, G.File_Location, G.SuppressEndeca, true as "Public", I.IP_Restriction_Mask, G.GroupID, I.ItemID, I.CheckoutRequired, (select COUNT(*) from SobekCM_Item J where G.GroupID = J.GroupID ) as Total_Volumes,
				coalesce(I.Level1_Text, '') as Level1_Text, coalesce( I.Level1_Index, 0 ) as Level1_Index,
				coalesce(I.Level2_Text, '') as Level2_Text, coalesce( I.Level2_Index, 0 ) as Level2_Index,
				coalesce(I.Level3_Text, '') as Level3_Text, coalesce( I.Level3_Index, 0 ) as Level3_Index,
				G.GroupTitle, I.TextSearchable, coalesce(I.Internal_Comments,'') as Comments, I.Dark, G.Type,
				I.Title, I.Publisher, I.Author, I.Donor, I.PubDate, G.ALEPH_Number, G.OCLC_Number, I.Born_Digital,
				I.Disposition_Advice, I.Material_Received_Date, I.Material_Recd_Date_Estimated, I.Tracking_Box, I.Disposition_Advice_Notes,
				I.Left_To_Right, I.Disposition_Notes, G.Track_By_Month, G.Large_Format, G.Never_Overlay_Record, I.CreateDate, I.SortDate,
				G.Primary_Identifier_Type, G.Primary_Identifier, G.Type as GroupType, coalesce(I.MainThumbnail,'') as MainThumbnail,
				T.EmbargoEnd, coalesce(T.UMI,'') as UMI, T.Original_EmbargoEnd, coalesce(T.Original_AccessCode,'') as Original_AccessCode,
				I.CitationSet, I.MadePublicDate, I.RestrictionMessage, I.Serve_Files_Locally
		from SobekCM_Item as I inner join
			 SobekCM_Item_Group as G on G.GroupID=I.GroupID left outer join
			 Tracking_Item as T on T.ItemID=I.ItemID
		where ( I.ItemID = v_ItemID );
		RETURN NEXT cur;

		OPEN cur FOR
		select T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder) as MenuOrder, V.Exclude, coalesce(V.OrderOverride, T."Order")
		from SobekCM_Item_Viewers V, SobekCM_Item_Viewer_Types T
		where ( V.ItemID = v_ItemID )
		  and ( V.ItemViewTypeID = T.ItemViewTypeID )
		group by T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder), V.Exclude, coalesce(V.OrderOverride, T."Order")
		order by coalesce(V.OrderOverride, T."Order") ASC;
		RETURN NEXT cur;

		OPEN cur FOR
		select Icon_URL, Link, Icon_Name, I.Title
		from SobekCM_Icon I, SobekCM_Item_Icons L
		where ( L.IconID = I.IconID )
		  and ( L.ItemID = v_ItemID )
		order by Sequence;
		RETURN NEXT cur;

		OPEN cur FOR
		select S.WebSkinCode
		from SobekCM_Item_Group_Web_Skin_Link L, SobekCM_Item I, SobekCM_Web_Skin S
		where ( L.GroupID = I.GroupID )
		  and ( L.WebSkinID = S.WebSkinID )
		  and ( I.ItemID = v_ItemID )
		order by L.Sequence;
		RETURN NEXT cur;

		OPEN cur FOR
		select Setting_Key, Setting_Value
		from SobekCM_Item_Settings
		where ItemID=v_ItemID;
		RETURN NEXT cur;

		OPEN cur FOR
		select I.UserGroupID, G.GroupName, I.canView, I.isOwner, I.canEditMetadata, I.canEditBehaviors, I.canPerformQc, I.canUploadFiles, I.canChangeVisibility, I.canDelete, I.customPermissions
		from mySobek_User_Group_Item_Permissions I, mySobek_User_Group G
		where G.UserGroupID=I.UserGroupID
		  and ItemID=v_ItemID;
		RETURN NEXT cur;

		OPEN cur FOR
		select I.UserID, U.UserName, U.UserID, I.canView, I.isOwner, I.canEditMetadata, I.canEditBehaviors, I.canPerformQc, I.canUploadFiles, I.canChangeVisibility, I.canDelete, I.customPermissions
		from mySobek_User_Item_Permissions I, mySobek_User U
		where U.UserID=I.UserID
		  and ItemID=v_ItemID;
		RETURN NEXT cur;
	end if;

	-- Get the list of related item groups
	OPEN cur FOR
	select B.BibID, B.GroupTitle, R.Relationship_A_to_B AS Relationship
	from SobekCM_Item_Group A, SobekCM_Item_Group_Relationship R, SobekCM_Item_Group B
	where ( A.BibID = p_bibid )
	  and ( R.GroupA = A.GroupID )
	  and ( R.GroupB = B.GroupID )
	union
	select A.BibID, A.GroupTitle, R.Relationship_B_to_A AS Relationship
	from SobekCM_Item_Group A, SobekCM_Item_Group_Relationship R, SobekCM_Item_Group B
	where ( B.BibID = p_bibid )
	  and ( R.GroupB = B.GroupID )
	  and ( R.GroupA = A.GroupID );
	RETURN NEXT cur;

	RETURN;
END;
$$;

-- Now also returns Serve_Files_Locally, so the Builder knows which items must stay local
CREATE OR REPLACE FUNCTION SobekCM_Builder_Get_Minimum_Item_Information(
	p_bibid varchar(10),
	p_vid varchar(5),
	OUT cur_item refcursor,
	OUT cur_aggregations refcursor,
	OUT cur_icons refcursor,
	OUT cur_webskins refcursor,
	OUT cur_viewers refcursor,
	OUT cur_group_permissions refcursor,
	OUT cur_user_permissions refcursor
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_ItemID integer;
BEGIN
	OPEN cur_item FOR select null::integer where false;
	OPEN cur_aggregations FOR select null::integer where false;
	OPEN cur_icons FOR select null::integer where false;
	OPEN cur_webskins FOR select null::integer where false;
	OPEN cur_viewers FOR select null::integer where false;
	OPEN cur_group_permissions FOR select null::integer where false;
	OPEN cur_user_permissions FOR select null::integer where false;

	if (( select COUNT(*) from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = p_BibID and I.VID = p_VID ) = 1 ) then
		select ItemID into v_ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = p_BibID and I.VID = p_VID;

		OPEN cur_item FOR
		select I.ItemID, I.MainThumbnail, I.IP_Restriction_Mask, I.Born_Digital, G.ItemCount, I.Dark, I.MadePublicDate, I.Serve_Files_Locally
		from SobekCM_Item I, SobekCM_Item_Group G
		where ( I.VID = p_vid )
		  and ( G.BibID = p_bibid )
		  and ( I.GroupID = G.GroupID );

		OPEN cur_aggregations FOR
		select A.Code, A.Name, A.Type
		from SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A
		where ( L.ItemID = v_itemid )
		  and ( L.AggregationID = A.AggregationID );

		OPEN cur_icons FOR
		select Icon_URL, Link, Icon_Name, I.Title
		from SobekCM_Icon I, SobekCM_Item_Icons L
		where ( L.IconID = I.IconID )
		  and ( L.ItemID = v_ItemID )
		order by Sequence;

		OPEN cur_webskins FOR
		select S.WebSkinCode
		from SobekCM_Item_Group_Web_Skin_Link L, SobekCM_Item I, SobekCM_Web_Skin S
		where ( L.GroupID = I.GroupID )
		  and ( L.WebSkinID = S.WebSkinID )
		  and ( I.ItemID = v_ItemID )
		order by L.Sequence;

		OPEN cur_viewers FOR
		select T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder) as MenuOrder, V.Exclude, coalesce(V.OrderOverride, T."Order")
		from SobekCM_Item_Viewers V, SobekCM_Item_Viewer_Types T
		where ( V.ItemID = v_ItemID )
		  and ( V.ItemViewTypeID = T.ItemViewTypeID )
		group by T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder), V.Exclude, coalesce(V.OrderOverride, T."Order")
		order by coalesce(V.OrderOverride, T."Order") ASC;

		OPEN cur_group_permissions FOR
		select I.UserGroupID, G.GroupName, I.canView, I.isOwner, I.canEditMetadata, I.canEditBehaviors, I.canPerformQc, I.canUploadFiles, I.canChangeVisibility, I.canDelete, I.customPermissions
		from mySobek_User_Group_Item_Permissions I, mySobek_User_Group G
		where G.UserGroupID=I.UserGroupID
		  and ItemID=v_ItemID;

		OPEN cur_user_permissions FOR
		select I.UserID, U.UserName, U.UserID, I.canView, I.isOwner, I.canEditMetadata, I.canEditBehaviors, I.canPerformQc, I.canUploadFiles, I.canChangeVisibility, I.canDelete, I.customPermissions
		from mySobek_User_Item_Permissions I, mySobek_User U
		where U.UserID=I.UserID
		  and ItemID=v_ItemID;
	end if;
END;
$$;

-- Sets or clears the serve-files-locally flag for one item
CREATE OR REPLACE FUNCTION SobekCM_Set_Item_Serve_Files_Locally(
	p_itemid integer,
	p_serve_locally boolean
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	update SobekCM_Item set Serve_Files_Locally = p_serve_locally where ItemID = p_itemid;
END;
$$;

-- One-time backfill. Only ever sets the flag, never clears it.
update SobekCM_Item I
set Serve_Files_Locally = true
where I.Serve_Files_Locally = false
  and exists ( select 1
               from SobekCM_Item_Viewers V inner join SobekCM_Item_Viewer_Types T on T.ItemViewTypeID = V.ItemViewTypeID
               where V.ItemID = I.ItemID
                 and (( T.ViewType in ( 'WEBSITE', 'OPEN_TEXTBOOK', 'OPEN_DIVISIONS' ))
                   or (( T.ViewType = 'HTML' ) and ( coalesce(V.Exclude, false) = false ) and ( V.Attribute like '%.htm%' ))));


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
