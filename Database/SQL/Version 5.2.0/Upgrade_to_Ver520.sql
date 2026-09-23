-- Upgrade_to_Ver520.sql - OpenSobek
--
-- Takes an existing 5.1.0 SQL Server database and brings it up to Version 5.2.0. If your
-- version is older than 5.1.0, run Upgrade_to_Ver510.sql first.


-- Splits signed GCS URL lifetimes by how the page uses the URL, instead of one lifetime for
-- everything. "GCS Signed URL Expiration Minutes" (default 240) keeps its value and now only
-- covers files the page keeps requesting while it is open (PDF, audio, video, page turner).
-- Page images and thumbnails the browser fetches immediately get "GCS Page Load URL Expiration
-- Minutes" (default 10), and links clicked later, such as the Downloads list, get "GCS Download
-- URL Expiration Minutes" (default 60). Restricted items never exceed "GCS Restricted URL
-- Expiration Minutes". Shorter lifetimes stop harvested or shared links from being reused.

	if ( NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Page Load URL Expiration Minutes' and Extension_Code is null))
	begin
		insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, [Hidden], Reserved, Help )
		values ( 'GCS Page Load URL Expiration Minutes', '10', 'System / Server Settings', 'Server Settings', 0, 2, 'How long (in minutes) a signed URL stays valid for a GCS-hosted file the browser fetches immediately as the page renders, such as page images and thumbnails. Can be very short: nothing reuses the URL once the page has loaded, and a short value makes harvested or shared links stop working quickly. Restricted items never exceed GCS Restricted URL Expiration Minutes. Only used when File System Mode is "GCS Hybrid" or "GCS Full".' );
	end;

	if ( NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Download URL Expiration Minutes' and Extension_Code is null))
	begin
		insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, [Hidden], Reserved, Help )
		values ( 'GCS Download URL Expiration Minutes', '60', 'System / Server Settings', 'Server Settings', 0, 2, 'How long (in minutes) a signed URL stays valid for a GCS-hosted file offered as a link the user clicks later, such as the Downloads list. Only needs to cover the time between the page loading and the click, since a download that has already started is not cut off when its URL expires. Restricted items never exceed GCS Restricted URL Expiration Minutes. Only used when File System Mode is "GCS Hybrid" or "GCS Full".' );
	end;

	update SobekCM_Settings
	set Help = 'How long (in minutes) a signed URL stays valid for a GCS-hosted file that keeps being requested while the page is open, such as PDFs, audio, video and the page turner, which make fresh requests as the user scrolls, seeks or turns pages. Page images and download links use the shorter GCS Page Load URL Expiration Minutes and GCS Download URL Expiration Minutes instead. Only used when File System Mode is "GCS Hybrid" or "GCS Full".'
	where Setting_Key = 'GCS Signed URL Expiration Minutes' and Extension_Code is null;
GO


-- Gives restricted items a separate, longer cap for files the page keeps requesting (PDF, audio,
-- video). Before this, every signed URL on a restricted item was capped at "GCS Restricted URL
-- Expiration Minutes" (default 15), so an authorized user seeking or scrolling past 15 minutes
-- hit an expired URL. That setting now caps only page images, thumbnails and download links.

	if ( NOT EXISTS (select 1 from SobekCM_Settings where Setting_Key = 'GCS Restricted Streaming URL Expiration Minutes' and Extension_Code is null))
	begin
		insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, [Hidden], Reserved, Help )
		values ( 'GCS Restricted Streaming URL Expiration Minutes', '60', 'System / Server Settings', 'Server Settings', 0, 2, 'How long (in minutes) a signed URL stays valid for a file on an IP- or user-group-restricted (but not dark) item that keeps being requested while the page is open, such as PDFs, audio and video, which make fresh requests as the user scrolls or seeks. Longer than GCS Restricted URL Expiration Minutes because a shorter value breaks playback and reading partway through. Never exceeds GCS Signed URL Expiration Minutes. Only used when File System Mode is "GCS Hybrid" or "GCS Full".' );
	end;

	update SobekCM_Settings
	set Help = 'How long (in minutes) a signed URL stays valid for a page image, thumbnail or download link on an IP- or user-group-restricted (but not dark) item. Deliberately much shorter than the public lifetimes, since a signed URL is a bearer token that works for anyone holding it once handed out. Files the page keeps requesting, such as PDFs, audio and video, use GCS Restricted Streaming URL Expiration Minutes instead. Only used when File System Mode is "GCS Hybrid" or "GCS Full".'
	where Setting_Key = 'GCS Restricted URL Expiration Minutes' and Extension_Code is null;
GO


-- Institution aggregations added automatically during an item save (when an item has a holding
-- or source institution code that does not exist yet) now get the same defaults as any other new
-- collection. Before this, SobekCM_Save_New_Item, SobekCM_Save_Item, SobekCM_Save_Item_Behaviors
-- and SobekCM_Mass_Update_Item_Behaviors each inserted a bare 'Added automatically' row, skipping
-- everything SobekCM_Save_Item_Aggregation copies from the parent (ALL) collection: facets,
-- result views, result fields, the GroupResults flag, editing permissions and the 'Created'
-- milestone. All four now call the new SobekCM_Add_Automatic_Institution, which goes through
-- SobekCM_Save_Item_Aggregation. As before, no hierarchy link is added for the new institution.

-- Procedures keep the SET options in effect when they are created. sqlcmd defaults QUOTED_IDENTIFIER
-- to OFF, which breaks deletes against tables with filtered indexes, so set both explicitly.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF object_id('SobekCM_Add_Automatic_Institution') IS NULL EXEC ('create procedure dbo.SobekCM_Add_Automatic_Institution as select 1;');
GO

-- Adds an institution aggregation automatically, when an item is saved with a holding or source
-- institution code that does not exist yet.  Does nothing if the code is empty or already exists.
ALTER PROCEDURE [dbo].[SobekCM_Add_Automatic_Institution]
	@code varchar(20)
AS
BEGIN
	-- Nothing to do if no code, or it already exists (deleted or not)
	if (( len(isnull(@code, '')) = 0 ) or ( exists ( select 1 from SobekCM_Item_Aggregation where Code = @code )))
		return;

	-- Save through the standard procedure, so the new institution inherits the ALL collection's
	-- facets, result views, result fields, GroupResults flag and editing permissions.  No parent
	-- id is passed, so no hierarchy link is added.
	declare @newaggregationid int;
	exec SobekCM_Save_Item_Aggregation
		@aggregationid = -1,
		@code = @code,
		@name = 'Added automatically',
		@shortname = 'Added automatically',
		@description = 'Added automatically',
		@thematicHeadingId = -1,
		@type = 'Institution',
		@isactive = 'false',
		@hidden = 'true',
		@display_options = '',
		@map_search = 0,
		@map_display = 0,
		@oai_flag = 'false',
		@oai_metadata = '',
		@contactemail = '',
		@defaultinterface = '',
		@externallink = '',
		@parentid = -1,
		@username = 'Added automatically',
		@languageVariants = '',
		@groupResults = 'false',
		@newaggregationid = @newaggregationid output;
END;
GO

ALTER PROCEDURE [dbo].[SobekCM_Save_New_Item]
	@GroupID int,
	@VID varchar(5),
	@PageCount int,
	@FileCount int,
	@Title nvarchar(500),
	@SortTitle nvarchar(500), 
	@AccessMethod int,
	@Link varchar(500),
	@CreateDate datetime,
	@PubDate nvarchar(100),
	@SortDate bigint,
	@Author nvarchar(1000),
	@Spatial_KML varchar(4000),
	@Spatial_KML_Distance float,
	@DiskSize_KB bigint,
	@Spatial_Display nvarchar(1000), 
	@Institution_Display nvarchar(1000), 
	@Edition_Display nvarchar(1000),
	@Material_Display nvarchar(1000),
	@Measurement_Display nvarchar(1000), 
	@StylePeriod_Display nvarchar(1000), 
	@Technique_Display nvarchar(1000), 
	@Subjects_Display nvarchar(1000), 
	@Donor nvarchar(250),
	@Publisher nvarchar(1000),
	@TextSearchable bit,
	@MainThumbnail varchar(100),
	@MainJPEG varchar(100),
	@IP_Restriction_Mask smallint,
	@CheckoutRequired bit,
	@AggregationCode1 varchar(20),
	@AggregationCode2 varchar(20),
	@AggregationCode3 varchar(20),
	@AggregationCode4 varchar(20),
	@AggregationCode5 varchar(20),
	@AggregationCode6 varchar(20),
	@AggregationCode7 varchar(20),
	@AggregationCode8 varchar(20),
	@HoldingCode varchar(20),
	@SourceCode varchar(20),
	@Icon1_Name varchar(50),
	@Icon2_Name varchar(50),
	@Icon3_Name varchar(50),
	@Icon4_Name varchar(50),
	@Icon5_Name varchar(50),
	@Level1_Text varchar(255),
	@Level1_Index int,
	@Level2_Text varchar(255),
	@Level2_Index int,
	@Level3_Text varchar(255),
	@Level3_Index int,
	@Level4_Text varchar(255),
	@Level4_Index int,
	@Level5_Text varchar(255),
	@Level5_Index int,
	@VIDSource varchar(150),
	@CopyrightIndicator smallint, 
	@Born_Digital bit,
	@Dark bit,
	@Material_Received_Date datetime,
	@Material_Recd_Date_Estimated bit,
	@Disposition_Advice int,
	@Disposition_Advice_Notes varchar(150),
	@Internal_Comments nvarchar(1000),
	@Tracking_Box varchar(25),
	@Online_Submit bit,
	@User varchar(50),
	@UserNotes varchar(1000),
	@UserID_To_Link int,
	@RestrictionMessage varchar(1000),
	@ItemID int output,
	@New_VID varchar(5) output
AS
begin transaction

	-- Set the return VID value and itemid first
	set @New_VID = @VID;
	set @ItemID = -1;

	-- Verify this is a new item before doing anything
	if ( (	 select count(*) from SobekCM_Item I where ( I.VID = @VID ) and ( I.GroupID = @GroupID ))  =  0 )
	begin
	
		-- Verify the VID is a complete bibid, otherwise find the next one
		if ( LEN(@VID) < 5 )
		begin
			declare @next_vid_number int;

			-- Find the next vid number
			select @next_vid_number = isnull(CAST(MAX(VID) as int) + 1,-1)
			from SobekCM_Item
			where GroupID = @GroupID;
			
			-- If no matches to this BibID, just start at 00001
			if ( @next_vid_number < 0 )
			begin
				select @New_VID = '00001'
			end
			else
			begin
				select @New_VID = RIGHT('0000' + (CAST( @next_vid_number as varchar(5))), 5);	
			end;	
		end;

		-- Add the values to the main SobekCM_Item table first
		insert into SobekCM_Item ( VID, [PageCount], FileCount, Deleted, Title, SortTitle, AccessMethod, Link, CreateDate, PubDate, SortDate, Author, Spatial_KML, Spatial_KML_Distance, GroupID, LastSaved, Donor, Publisher, TextSearchable, MainThumbnail, MainJPEG, CheckoutRequired, IP_Restriction_Mask, Level1_Text, Level1_Index, Level2_Text, Level2_Index, Level3_Text, Level3_Index, Level4_Text, Level4_Index, Level5_Text, Level5_Index, Last_MileStone, VIDSource, Born_Digital, Dark, Material_Received_Date, Material_Recd_Date_Estimated, Disposition_Advice, Internal_Comments, Tracking_Box, Disposition_Advice_Notes, Spatial_Display, Institution_Display, Edition_Display, Material_Display, Measurement_Display, StylePeriod_Display, Technique_Display, Subjects_Display, RestrictionMessage )
		values (  @New_VID, @PageCount, @FileCount, 0, @Title, @SortTitle, @AccessMethod, @Link, @CreateDate, @PubDate, @SortDate, @Author, @Spatial_KML, @Spatial_KML_Distance, @GroupID, GETDATE(), @Donor, @Publisher, @TextSearchable, @MainThumbnail, @MainJPEG, @CheckoutRequired, @IP_Restriction_Mask, @Level1_Text, @Level1_Index, @Level2_Text, @Level2_Index, @Level3_Text, @Level3_Index, @Level4_Text, @Level4_Index, @Level5_Text, @Level5_Index, 0, @VIDSource, @Born_Digital, @Dark, @Material_Received_Date, @Material_Recd_Date_Estimated, @Disposition_Advice, @Internal_Comments, @Tracking_Box, @Disposition_Advice_Notes, @Spatial_Display, @Institution_Display, @Edition_Display, @Material_Display, @Measurement_Display, @StylePeriod_Display, @Technique_Display, @Subjects_Display, @RestrictionMessage  );
		
		-- Get the item id identifier for this row
		set @ItemID = @@identity;	
		
		-- Set the milestones to complete if this is NON-PRIVATE, NON-DARK, and BORN DIGITAL
		if (( @IP_Restriction_Mask >= 0 ) and ( @Dark = 'false' ) and ( @Born_Digital = 'true' ))
		begin
			update SobekCM_Item
			set Last_MileStone = 4, Milestone_DigitalAcquisition = CreateDate, Milestone_ImageProcessing=CreateDate, Milestone_QualityControl=CreateDate, Milestone_OnlineComplete=CreateDate 
			where ItemID=@ItemID;		
		end;
				
		-- If a size was included, set that value
		if ( @DiskSize_KB > 0 )
		begin
			update SobekCM_Item set DiskSize_KB = @DiskSize_KB where ItemID=@ItemID;
		end;

		-- Finally set the volume count for this group correctly
		update SobekCM_Item_Group
		set ItemCount = ( select count(*) from SobekCM_Item I where ( I.GroupID = @GroupID ) and ( I.Deleted = 'false' ))
		where GroupID = @GroupID;
		
		-- Add the first icon to this object  (this requires the icons have been pre-established )
		declare @IconID int;
		if ( len( isnull( @Icon1_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon1_Name;

			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				values ( @ItemID, @IconID, 1 );
			end;
		end;

		-- Add the second icon to this object  (this requires the icons have been pre-established )
		if ( len( isnull( @Icon2_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon2_Name;

			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				values ( @ItemID, @IconID, 2 );
			end;
		end;

		-- Add the third icon to this object  (this requires the icons have been pre-established )
		if ( len( isnull( @Icon3_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon3_Name;

			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				values ( @ItemID, @IconID, 3 );
			end;
		end;

		-- Add the fourth icon to this object  (this requires the icons have been pre-established )
		if ( len( isnull( @Icon4_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon4_Name;
			
			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				values ( @ItemID, @IconID, 4 );
			end;
		end;

		-- Add the fifth icon to this object  (this requires the icons have been pre-established )
		if ( len( isnull( @Icon5_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon5_Name;

			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				values ( @ItemID, @IconID, 5 );
			end;
		end;

		-- Clear all links to aggregations
		delete from SobekCM_Item_Aggregation_Item_Link where ItemID = @ItemID;

		-- Add all of the aggregations
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode1;
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode2;
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode3;
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode4;
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode5;
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode6;
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode7;
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode8;
		
		-- Create one string of all the aggregation codes
		declare @aggregationCodes varchar(100);
		set @aggregationCodes = rtrim(isnull(@AggregationCode1,'') + ' ' + isnull(@AggregationCode2,'') + ' ' + isnull(@AggregationCode3,'') + ' ' + isnull(@AggregationCode4,'') + ' ' + isnull(@AggregationCode5,'') + ' ' + isnull(@AggregationCode6,'') + ' ' + isnull(@AggregationCode7,'') + ' ' + isnull(@AggregationCode8,''));
	
		-- Update matching items to have the aggregation codes value
		update SobekCM_Item set AggregationCodes = @aggregationCodes where ItemID=@ItemID;

		-- Check for Holding Institution Code
		declare @AggregationID int;
		if ( len ( isnull ( @HoldingCode, '' ) ) > 0 )
		begin
			-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
			exec SobekCM_Add_Automatic_Institution @HoldingCode;
			
			-- Add the link to this holding code ( and any legitimate parent aggregations )
			exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @HoldingCode;
		end;

		-- Check for Source Institution Code
		if ( len ( isnull ( @SourceCode, '' ) ) > 0 )
		begin
			-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
			exec SobekCM_Add_Automatic_Institution @SourceCode;

			-- Add the link to this holding code ( and any legitimate parent aggregations )
			exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @SourceCode;
		end;

		-- Just in case somehow some viewers existed
		delete from SobekCM_Item_Viewers 
		where ItemID=@itemid;
		
		-- Copy over all the default viewer information
		insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label, Exclude )
		select @itemid, ItemViewTypeID, '', '', 'false' 
		from SobekCM_Item_Viewer_Types
		where ( DefaultView = 'true' );

		-- Add the workhistory for this item being loaded
		if ( @Online_Submit = 'true' )
		begin
			-- Add progress for online submission completed
			insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath, WorkPerformedById )
			values ( @itemid, 29, getdate(), @user, @usernotes, '', @UserID_To_Link );
		end
		else
		begin  
			-- Add progress for bulk loaded into the system through the Builder
			insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath )
			values ( @itemid, 40, getdate(), @user, @usernotes, '' );	
		end;		

		-- Is this non-dark and public?
		if (( @Dark = 'false' ) and ( @IP_Restriction_Mask >= 0 ))
		begin
			update SobekCM_Item 
			set MadePublicDate = getdate()
			where ItemID=@ItemID;
		end;
		
		-- Link this to the user?
		if ( @UserID_To_Link >= 1 )
		begin
			-- Link this user to the bibid, if not already linked
			if (( select COUNT(*) from mySobek_User_Bib_Link where UserID=@UserID_To_Link and GroupID = @groupid ) = 0 )
			begin
				insert into mySobek_User_Bib_Link ( UserID, GroupID )
				values ( @UserID_To_Link, @groupid );
			end;
			
			-- First, see if this user already has a folder named 'Submitted Items'
			declare @userfolderid int
			if (( select count(*) from mySobek_User_Folder where UserID=@UserID_To_Link and FolderName='Submitted Items') > 0 )
			begin
				-- Get the existing folder id
				select @userfolderid = UserFolderID from mySobek_User_Folder where UserID=@UserID_To_Link and FolderName='Submitted Items';
			end
			else
			begin
				-- Add this folder
				insert into mySobek_User_Folder ( UserID, FolderName, isPublic )
				values ( @UserID_To_Link, 'Submitted Items', 'false' );

				-- Get the new id
				select @userfolderid = @@identity;
			end;
			
			-- Add a new link then
			insert into mySobek_User_Item( UserFolderID, ItemID, ItemOrder, UserNotes, DateAdded )
			values ( @userfolderid, @itemid, 1, '', getdate() );
			
			-- Also link using the newer system, which links for statistical reporting, etc..
			-- This will likely replace the 'submitted items' folder technique from above
			insert into mySobek_User_Item_Link( UserID, ItemID, RelationshipID )
			values ( @UserID_To_Link, @ItemID, 1 );
		
		end;
	end;

commit transaction;
GO

ALTER PROCEDURE [dbo].[SobekCM_Save_Item]
	@GroupID int,
	@VID varchar(5),
	@PageCount int,
	@FileCount int,
	@Title nvarchar(500),
	@SortTitle nvarchar(500), --NEW
	@AccessMethod int,
	@Link varchar(500),
	@CreateDate datetime,
	@PubDate nvarchar(100),
	@SortDate bigint,
	@HoldingCode varchar(20),
	@SourceCode varchar(20),
	@Author nvarchar(1000),
	@Spatial_KML varchar(4000),
	@Spatial_KML_Distance float,
	@DiskSize_KB bigint,
	@Spatial_Display nvarchar(1000), 
	@Institution_Display nvarchar(1000), 
	@Edition_Display nvarchar(1000),
	@Material_Display nvarchar(1000),
	@Measurement_Display nvarchar(1000), 
	@StylePeriod_Display nvarchar(1000), 
	@Technique_Display nvarchar(1000), 
	@Subjects_Display nvarchar(1000), 
	@Donor nvarchar(250),
	@Publisher nvarchar(1000),
	@RestrictionMessage nvarchar(1000),
	@ItemID int output,
	@Existing bit output,
	@New_VID varchar(5) output
AS
begin transaction

	-- Set the return VID value first
	set @New_VID = @VID;

	-- If this already exists (BibID, VID) then just update
	if ( (	 select count(*) from SobekCM_Item I where ( I.VID = @VID ) and ( I.GroupID = @GroupID ) )  > 0 )
	begin
		-- Save the item id
		select @ItemID = I.ItemID
		from SobekCM_Item I
		where  ( I.VID = @VID ) and ( I.GroupID = @GroupID );

		--Update the main item
		update SobekCM_Item
		set [PageCount] = @PageCount, 
			Deleted = 0, Title=@Title, SortTitle=@SortTitle, AccessMethod=@AccessMethod, Link=@Link,
			PubDate=@PubDate, SortDate=@SortDate, FileCount=@FileCount, Author=@Author, 
			Spatial_KML=@Spatial_KML, Spatial_KML_Distance=@Spatial_KML_Distance,  
			Donor=@Donor, Publisher=@Publisher, 
			GroupID = GroupID, LastSaved=GETDATE(), Spatial_Display=@Spatial_Display, Institution_Display=@Institution_Display, 
			Edition_Display=@Edition_Display, Material_Display=@Material_Display, Measurement_Display=@Measurement_Display, 
			StylePeriod_Display=@StylePeriod_Display, Technique_Display=@Technique_Display, Subjects_Display=@Subjects_Display,
			RestrictionMessage=@RestrictionMessage  
		where ( ItemID = @ItemID );

		-- Set the existing flag to true (1)
		set @Existing = 1;
	end
	else
	begin
	
		-- Verify the VID is a complete bibid, otherwise find the next one
		if ( LEN(@VID) < 5 )
		begin
			declare @next_vid_number int;

			-- Find the next vid number
			select @next_vid_number = isnull(CAST(MAX(VID) as int) + 1,-1)
			from SobekCM_Item
			where GroupID = @GroupID;
			
			-- If no matches to this BibID, just start at 00001
			if ( @next_vid_number < 0 )
			begin
				select @New_VID = '00001';
			end
			else
			begin
				select @New_VID = RIGHT('0000' + (CAST( @next_vid_number as varchar(5))), 5);	
			end;	
		end;
		
		-- Add the values to the main SobekCM_Item table first
		insert into SobekCM_Item ( VID, [PageCount], FileCount, Deleted, Title, SortTitle, AccessMethod, Link, CreateDate, PubDate, SortDate, Author, Spatial_KML, Spatial_KML_Distance, GroupID, LastSaved, Donor, Publisher, Spatial_Display, Institution_Display, Edition_Display, Material_Display, Measurement_Display, StylePeriod_Display, Technique_Display, Subjects_Display, RestrictionMessage )
		values (  @New_VID, @PageCount, @FileCount, 0, @Title, @SortTitle, @AccessMethod, @Link, @CreateDate, @PubDate, @SortDate, @Author, @Spatial_KML, @Spatial_KML_Distance, @GroupID, GETDATE(), @Donor, @Publisher, @Spatial_Display, @Institution_Display, @Edition_Display, @Material_Display, @Measurement_Display, @StylePeriod_Display, @Technique_Display, @Subjects_Display, @RestrictionMessage );

		-- Get the item id identifier for this row
		set @ItemID = @@identity;

		-- Set existing flag to false
		set @Existing = 0;
		
		-- Copy over all the default viewer information
		insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label, Exclude )
		select @itemid, ItemViewTypeID, '', '', 'false' 
		from SobekCM_Item_Viewer_Types
		where ( DefaultView = 'true' );
	end;

	-- Check for Holding Institution Code
	declare @AggregationID int;
	if ( len ( isnull ( @HoldingCode, '' ) ) > 0 )
	begin
		-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
		exec SobekCM_Add_Automatic_Institution @HoldingCode;
		
		-- Add the link to this holding code ( and any legitimate parent aggregations )
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @HoldingCode;		
	end;

	-- Check for Source Institution Code
	if ( len ( isnull ( @SourceCode, '' ) ) > 0 )
	begin
		-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
		exec SobekCM_Add_Automatic_Institution @SourceCode;

		-- Add the link to this holding code ( and any legitimate parent aggregations )
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @SourceCode;	
	end;
	
	-- If a size was included, set that value
	if ( @DiskSize_KB > 0 )
	begin
		update SobekCM_Item set DiskSize_KB = @DiskSize_KB where ItemID=@ItemID;
	end;

	-- Finally set the volume count for this group correctly
	declare @itemcount int;
	set @itemcount = ( select count(*) from SobekCM_Item I where ( I.GroupID = @GroupID ) and ( I.Deleted = 'false' ));

	-- Update the item group count
	update SobekCM_Item_Group
	set ItemCount = @itemcount
	where GroupID = @GroupID;

	-- If this was an update, and this group had only this one VID, look at changing the
	-- group title to match the item title
	if (( @Existing = 1 ) and ( @itemcount = 1 ))
	begin
		-- Only make this update if this is not a SERIAL or NEWSPAPER
		if ( exists ( select 1 from SobekCM_Item_Group where GroupID=@GroupID and [Type] != 'Serial' and [Type] != 'Newspaper' ))
		begin
			update SobekCM_Item_Group 
			set GroupTitle = @Title, SortTitle = @SortTitle
			where GroupID=@GroupID;
		end;
	end;

commit transaction;
GO

ALTER PROCEDURE [dbo].[SobekCM_Save_Item_Behaviors]
	@ItemID int,
	@TextSearchable bit,
	@MainThumbnail varchar(100),
	@MainJPEG varchar(100),
	@IP_Restriction_Mask smallint,
	@CheckoutRequired bit,
	@Dark_Flag bit,
	@Born_Digital bit,
	@Disposition_Advice int,
	@Disposition_Advice_Notes varchar(150),
	@Material_Received_Date datetime,
	@Material_Recd_Date_Estimated bit,
	@Tracking_Box varchar(25),
	@AggregationCode1 varchar(20),
	@AggregationCode2 varchar(20),
	@AggregationCode3 varchar(20),
	@AggregationCode4 varchar(20),
	@AggregationCode5 varchar(20),
	@AggregationCode6 varchar(20),
	@AggregationCode7 varchar(20),
	@AggregationCode8 varchar(20),
	@HoldingCode varchar(20),
	@SourceCode varchar(20),
	@Icon1_Name varchar(50),
	@Icon2_Name varchar(50),
	@Icon3_Name varchar(50),
	@Icon4_Name varchar(50),
	@Icon5_Name varchar(50),
	@Left_To_Right bit,
	@CitationSet varchar(50)
AS
begin transaction

	--Update the main item
	update SobekCM_Item
	set TextSearchable = @TextSearchable, Deleted = 0, MainThumbnail=@MainThumbnail,
		MainJPEG=@MainJPEG, CheckoutRequired=@CheckoutRequired, IP_Restriction_Mask=@IP_Restriction_Mask,
		Dark=@Dark_Flag, Born_Digital=@Born_Digital, Disposition_Advice=@Disposition_Advice,
		Material_Received_Date=@Material_Received_Date, Material_Recd_Date_Estimated=@Material_Recd_Date_Estimated,
		Tracking_Box=@Tracking_Box, Disposition_Advice_Notes = @Disposition_Advice_Notes, Left_To_Right=@Left_To_Right,
		CitationSet=@CitationSet
	where ( ItemID = @ItemID );

	-- Clear the links to all existing icons
	delete from SobekCM_Item_Icons where ItemID=@ItemID;
	
	-- Add the first icon to this object  (this requires the icons have been pre-established )
	declare @IconID int
	if ( len( isnull( @Icon1_Name, '' )) > 0 ) 
	begin
		-- Get the Icon ID for this icon
		select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon1_Name;

		-- Tie this item to this icon
		if ( ISNULL(@IconID,-1) > 0 )
		begin
			insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
			values ( @ItemID, @IconID, 1 );
		end;
	end;

	-- Add the second icon to this object  (this requires the icons have been pre-established )
	if ( len( isnull( @Icon2_Name, '' )) > 0 ) 
	begin
		-- Get the Icon ID for this icon
		select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon2_Name;

		-- Tie this item to this icon
		if (( ISNULL(@IconID,-1) > 0 )  and ( not exists ( select 1 from SobekCM_Item_Icons where ItemID=@ItemID and IconID=@IconID )))
		begin
			insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
			values ( @ItemID, @IconID, 2 );
		end;
	end;

	-- Add the third icon to this object  (this requires the icons have been pre-established )
	if ( len( isnull( @Icon3_Name, '' )) > 0 ) 
	begin
		-- Get the Icon ID for this icon
		select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon3_Name;

		-- Tie this item to this icon
		if (( ISNULL(@IconID,-1) > 0 ) and ( not exists ( select 1 from SobekCM_Item_Icons where ItemID=@ItemID and IconID=@IconID )))
		begin
			insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
			values ( @ItemID, @IconID, 3 );
		end;
	end;

	-- Add the fourth icon to this object  (this requires the icons have been pre-established )
	if ( len( isnull( @Icon4_Name, '' )) > 0 ) 
	begin
		-- Get the Icon ID for this icon
		select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon4_Name;
		
		-- Tie this item to this icon
		if (( ISNULL(@IconID,-1) > 0 ) and ( not exists ( select 1 from SobekCM_Item_Icons where ItemID=@ItemID and IconID=@IconID )))
		begin
			insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
			values ( @ItemID, @IconID, 4 );
		end;
	end;

	-- Add the fifth icon to this object  (this requires the icons have been pre-established )
	if ( len( isnull( @Icon5_Name, '' )) > 0 ) 
	begin
		-- Get the Icon ID for this icon
		select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon5_Name;

		-- Tie this item to this icon
		if (( ISNULL(@IconID,-1) > 0 ) and ( not exists ( select 1 from SobekCM_Item_Icons where ItemID=@ItemID and IconID=@IconID )))
		begin
			insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
			values ( @ItemID, @IconID, 5 );
		end;
	end;

	-- Clear all links to aggregations
	delete from SobekCM_Item_Aggregation_Item_Link where ItemID = @ItemID;

	-- Add all of the aggregations
	exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode1;
	exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode2;
	exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode3;
	exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode4;
	exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode5;
	exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode6;
	exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode7;
	exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode8;
	
	-- Create one string of all the aggregation codes
	declare @aggregationCodes varchar(100);
	set @aggregationCodes = rtrim(isnull(@AggregationCode1,'') + ' ' + isnull(@AggregationCode2,'') + ' ' + isnull(@AggregationCode3,'') + ' ' + isnull(@AggregationCode4,'') + ' ' + isnull(@AggregationCode5,'') + ' ' + isnull(@AggregationCode6,'') + ' ' + isnull(@AggregationCode7,'') + ' ' + isnull(@AggregationCode8,''));
	
	-- Update matching items to have the aggregation codes value
	update SobekCM_Item set AggregationCodes = @aggregationCodes where ItemID=@ItemID;

	-- Check for Holding Institution Code
	declare @AggregationID int;
	if ( len ( isnull ( @HoldingCode, '' ) ) > 0 )
	begin
		-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
		exec SobekCM_Add_Automatic_Institution @HoldingCode;
		
		-- Add the link to this holding code ( and any legitimate parent aggregations )
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @HoldingCode;		
	end;

	-- Check for Source Institution Code
	if ( len ( isnull ( @SourceCode, '' ) ) > 0 )
	begin
		-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
		exec SobekCM_Add_Automatic_Institution @SourceCode;

		-- Add the link to this holding code ( and any legitimate parent aggregations )
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @SourceCode;
	end;

	-- If this is being made public, set the public data
	if (( @Dark_Flag = 'false' ) and ( @IP_Restriction_Mask >= 0 ))
	begin
		update SobekCM_Item 
		set MadePublicDate = coalesce(MadePublicDate, getdate())
		where ItemID=@ItemID;
	end;
	
commit transaction;
GO

ALTER PROCEDURE [dbo].[SobekCM_Mass_Update_Item_Behaviors]
	@GroupID int,
	@IP_Restriction_Mask smallint,
	@CheckoutRequired bit,
	@Dark_Flag bit,
	@Born_Digital bit,
	@AggregationCode1 varchar(20),
	@AggregationCode2 varchar(20),
	@AggregationCode3 varchar(20),
	@AggregationCode4 varchar(20),
	@AggregationCode5 varchar(20),
	@AggregationCode6 varchar(20),
	@AggregationCode7 varchar(20),
	@AggregationCode8 varchar(20),
	@HoldingCode varchar(20),
	@SourceCode varchar(20),
	@Icon1_Name varchar(50),
	@Icon2_Name varchar(50),
	@Icon3_Name varchar(50),
	@Icon4_Name varchar(50),
	@Icon5_Name varchar(50),
	@Viewer1_Type varchar(50),
	@Viewer1_Label nvarchar(50),
	@Viewer1_Attribute nvarchar(250),
	@Viewer2_Type varchar(50),
	@Viewer2_Label nvarchar(50),
	@Viewer2_Attribute nvarchar(250),
	@Viewer3_Type varchar(50),
	@Viewer3_Label nvarchar(50),
	@Viewer3_Attribute nvarchar(250),
	@Viewer4_Type varchar(50),
	@Viewer4_Label nvarchar(50),
	@Viewer4_Attribute nvarchar(250),
	@Viewer5_Type varchar(50),
	@Viewer5_Label nvarchar(50),
	@Viewer5_Attribute nvarchar(250),
	@Viewer6_Type varchar(50),
	@Viewer6_Label nvarchar(50),
	@Viewer6_Attribute nvarchar(250)
AS
begin transaction

	--Update the main item's flags if provided
	if ( @IP_Restriction_Mask is not null )
	begin
		update SobekCM_Item
		set IP_Restriction_Mask=@IP_Restriction_Mask
		where ( GroupID = @GroupID );
	end;
	
	if ( @CheckoutRequired is not null )
	begin
		update SobekCM_Item
		set CheckoutRequired=@CheckoutRequired
		where ( GroupID = @GroupID );
	end;
	
	if ( @Dark_Flag is not null )
	begin
		update SobekCM_Item
		set Dark=@Dark_Flag
		where ( GroupID = @GroupID );
	end;
	
	if ( @Born_Digital is not null )
	begin
		update SobekCM_Item
		set Born_Digital=@Born_Digital
		where ( GroupID = @GroupID );
	end;
	
	-- Only do icon stuff if the first icon has length
	if ( len( isnull( @Icon1_Name, '' )) > 0 ) 
	begin

		-- Clear the links to all existing icons
		delete from SobekCM_Item_Icons 
		where exists (  select *
						from SobekCM_Item
						where ( SobekCM_Item.GroupID=@GroupID )
						  and ( SobekCM_Item.ItemID = SobekCM_Item_Icons.ItemID ));
		
		-- Add the first icon to this object  (this requires the icons have been pre-established )
		declare @IconID int;
		if ( len( isnull( @Icon1_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon1_Name;

			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				select ItemID, @IconID, 1 from SobekCM_Item I where I.GroupID=@GroupID;
			end;
		end;

		-- Add the second icon to this object  (this requires the icons have been pre-established )
		if ( len( isnull( @Icon2_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon2_Name;

			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				select ItemID, @IconID, 2 from SobekCM_Item I where I.GroupID=@GroupID;
			end;
		end;

		-- Add the third icon to this object  (this requires the icons have been pre-established )
		if ( len( isnull( @Icon3_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon3_Name;

			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				select ItemID, @IconID, 3 from SobekCM_Item I where I.GroupID=@GroupID;
			end;
		end;

		-- Add the fourth icon to this object  (this requires the icons have been pre-established )
		if ( len( isnull( @Icon4_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon4_Name;
			
			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				select ItemID, @IconID, 4 from SobekCM_Item I where I.GroupID=@GroupID;
			end;
		end;

		-- Add the fifth icon to this object  (this requires the icons have been pre-established )
		if ( len( isnull( @Icon5_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon5_Name;

			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				select ItemID, @IconID, 5 from SobekCM_Item I where I.GroupID=@GroupID;
			end;
		end;
	end;
	
	-- Only modify the aggregation codes if they have length
	if ( LEN ( ISNULL( @AggregationCode1, '')) > 0 )
	begin
	
		-- Clear all links to aggregations
		delete from SobekCM_Item_Aggregation_Item_Link 
		where exists ( select * from SobekCM_Item I where I.GroupID=@GroupID and I.ItemID=SobekCM_Item_Aggregation_Item_Link.ItemID );

		-- Add all of the aggregations
		exec [SobekCM_Mass_Update_Item_Aggregation_Link] @GroupID, @AggregationCode1;
		exec [SobekCM_Mass_Update_Item_Aggregation_Link] @GroupID, @AggregationCode2;
		exec [SobekCM_Mass_Update_Item_Aggregation_Link] @GroupID, @AggregationCode3;
		exec [SobekCM_Mass_Update_Item_Aggregation_Link] @GroupID, @AggregationCode4;
		exec [SobekCM_Mass_Update_Item_Aggregation_Link] @GroupID, @AggregationCode5;
		exec [SobekCM_Mass_Update_Item_Aggregation_Link] @GroupID, @AggregationCode6;
		exec [SobekCM_Mass_Update_Item_Aggregation_Link] @GroupID, @AggregationCode7;
		exec [SobekCM_Mass_Update_Item_Aggregation_Link] @GroupID, @AggregationCode8;

	end;

	-- Check for Holding Institution Code
	declare @AggregationID int;
	if ( len ( isnull ( @HoldingCode, '' ) ) > 0 )
	begin
		-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
		exec SobekCM_Add_Automatic_Institution @HoldingCode;
		
		-- Add the link to this holding code ( and any legitimate parent aggregations )
		exec [SobekCM_Mass_Update_Item_Aggregation_Link] @GroupID, @HoldingCode;
	end;

	-- Check for Source Institution Code
	if ( len ( isnull ( @SourceCode, '' ) ) > 0 )
	begin
		-- Add this institution if it does not exist yet (inherits the ALL collection's defaults)
		exec SobekCM_Add_Automatic_Institution @SourceCode;

		-- Add the link to this holding code ( and any legitimate parent aggregations )
		exec [SobekCM_Mass_Update_Item_Aggregation_Link] @GroupID, @SourceCode;
	end;
		
	-- Add the first viewer information, if provided
	if ( len(coalesce(@Viewer1_Type, '')) > 0 )
	begin
		-- Get the primary key for this viewer type
		declare @Viewer1_TypeID int;
		set @Viewer1_TypeID = coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = @Viewer1_Type ), -1 );

		-- Only continue if that viewer type was found
		if ( @Viewer1_TypeID > 0 )
		begin
			-- Insert this viewer information to all items, where it does not already exist
			insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
			select I.ItemID, @Viewer1_TypeID, @Viewer1_Attribute, @Viewer1_Label 
			from SobekCM_Item I 
			where ( I.GroupID=@GroupID )
				and ( not exists ( select 1 from SobekCM_Item_Viewers where ItemID=I.ItemID and ItemViewTypeID=@Viewer1_TypeID ))
		end
	end;

	-- Add the second viewer information, if provided
	if ( len(coalesce(@Viewer2_Type, '')) > 0 )
	begin
		-- Get the primary key for this viewer type
		declare @Viewer2_TypeID int;
		set @Viewer2_TypeID = coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = @Viewer2_Type ), -1 );

		-- Only continue if that viewer type was found
		if ( @Viewer2_TypeID > 0 )
		begin
			-- Insert this viewer information to all items, where it does not already exist
			insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
			select I.ItemID, @Viewer2_TypeID, @Viewer2_Attribute, @Viewer2_Label 
			from SobekCM_Item I 
			where ( I.GroupID=@GroupID )
				and ( not exists ( select 1 from SobekCM_Item_Viewers where ItemID=I.ItemID and ItemViewTypeID=@Viewer2_TypeID ))
		end
	end;

	-- Add the third viewer information, if provided
	if ( len(coalesce(@Viewer3_Type, '')) > 0 )
	begin
		-- Get the primary key for this viewer type
		declare @Viewer3_TypeID int;
		set @Viewer3_TypeID = coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = @Viewer3_Type ), -1 );

		-- Only continue if that viewer type was found
		if ( @Viewer3_TypeID > 0 )
		begin
			-- Insert this viewer information to all items, where it does not already exist
			insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
			select I.ItemID, @Viewer3_TypeID, @Viewer3_Attribute, @Viewer3_Label 
			from SobekCM_Item I 
			where ( I.GroupID=@GroupID )
				and ( not exists ( select 1 from SobekCM_Item_Viewers where ItemID=I.ItemID and ItemViewTypeID=@Viewer3_TypeID ))
		end
	end;

	-- Add the fourth viewer information, if provided
	if ( len(coalesce(@Viewer4_Type, '')) > 0 )
	begin
		-- Get the primary key for this viewer type
		declare @Viewer4_TypeID int;
		set @Viewer4_TypeID = coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = @Viewer4_Type ), -1 );

		-- Only continue if that viewer type was found
		if ( @Viewer4_TypeID > 0 )
		begin
			-- Insert this viewer information to all items, where it does not already exist
			insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
			select I.ItemID, @Viewer4_TypeID, @Viewer4_Attribute, @Viewer4_Label 
			from SobekCM_Item I 
			where ( I.GroupID=@GroupID )
				and ( not exists ( select 1 from SobekCM_Item_Viewers where ItemID=I.ItemID and ItemViewTypeID=@Viewer4_TypeID ))
		end
	end;

	-- Add the fifth viewer information, if provided
	if ( len(coalesce(@Viewer5_Type, '')) > 0 )
	begin
		-- Get the primary key for this viewer type
		declare @Viewer5_TypeID int;
		set @Viewer5_TypeID = coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = @Viewer5_Type ), -1 );

		-- Only continue if that viewer type was found
		if ( @Viewer5_TypeID > 0 )
		begin
			-- Insert this viewer information to all items, where it does not already exist
			insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
			select I.ItemID, @Viewer5_TypeID, @Viewer5_Attribute, @Viewer5_Label 
			from SobekCM_Item I 
			where ( I.GroupID=@GroupID )
				and ( not exists ( select 1 from SobekCM_Item_Viewers where ItemID=I.ItemID and ItemViewTypeID=@Viewer5_TypeID ))
		end
	end;

	-- Add the sixth viewer information, if provided
	if ( len(coalesce(@Viewer6_Type, '')) > 0 )
	begin
		-- Get the primary key for this viewer type
		declare @Viewer6_TypeID int;
		set @Viewer6_TypeID = coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = @Viewer6_Type ), -1 );

		-- Only continue if that viewer type was found
		if ( @Viewer6_TypeID > 0 )
		begin
			-- Insert this viewer information to all items, where it does not already exist
			insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
			select I.ItemID, @Viewer6_TypeID, @Viewer6_Attribute, @Viewer6_Label 
			from SobekCM_Item I 
			where ( I.GroupID=@GroupID )
				and ( not exists ( select 1 from SobekCM_Item_Viewers where ItemID=I.ItemID and ItemViewTypeID=@Viewer6_TypeID ))
		end
	end;

commit transaction;
GO

-- One-time backfill for institutions that were added automatically before this fix.  Any
-- non-deleted Institution aggregation with no result views at all gets the ALL collection's result
-- views and result fields, its facets (only if it has none) and its editing permissions (only for
-- users and groups without a row for that institution yet).  GroupResults is left alone, since it
-- may have been set deliberately since.
declare @allid int;
set @allid = ( select AggregationID from SobekCM_Item_Aggregation where Code = 'ALL' );

if ( @allid is not null )
begin
	-- Find the institutions needing the defaults
	select A.AggregationID
	into #NeedsDefaults
	from SobekCM_Item_Aggregation A
	where ( A.[Type] = 'Institution' )
	  and ( A.Deleted = 'false' )
	  and ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views V where V.AggregationID = A.AggregationID ));

	-- Copy over the facet fields (only where none exist)
	insert into SobekCM_Item_Aggregation_Facets ( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
	select N.AggregationID, F.MetadataTypeID, F.OverrideFacetTerm, F.FacetOrder, F.FacetOptions
	from #NeedsDefaults N, SobekCM_Item_Aggregation_Facets F
	where ( F.AggregationID = @allid )
	  and ( not exists ( select 1 from SobekCM_Item_Aggregation_Facets X where X.AggregationID = N.AggregationID ));

	-- Copy over the results views
	insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
	select N.AggregationID, V.ItemAggregationResultTypeID, V.DefaultView
	from #NeedsDefaults N, SobekCM_Item_Aggregation_Result_Views V
	where V.AggregationID = @allid;

	-- Now, add the result view fields
	insert into SobekCM_Item_Aggregation_Result_Fields ( ItemAggregationResultID, MetadataTypeID, OverrideDisplayTerm, DisplayOrder, DisplayOptions )
	select V2.ItemAggregationResultID, F1.MetadataTypeID, F1.OverrideDisplayTerm, F1.DisplayOrder, F1.DisplayOptions
	from SobekCM_Item_Aggregation_Result_Views V1, SobekCM_Item_Aggregation_Result_Fields F1, SobekCM_Item_Aggregation_Result_Views V2, #NeedsDefaults N
	where V1.ItemAggregationResultID = F1.ItemAggregationResultID
	  and V1.AggregationID = @allid
	  and V2.ItemAggregationResultTypeID = V1.ItemAggregationResultTypeID
	  and V2.AggregationID = N.AggregationID;

	-- Add individual user permissions
	insert into mySobek_User_Edit_Aggregation ( UserID, AggregationID, CanSelect, CanEditItems,
		IsCurator, IsAdmin, CanEditMetadata, CanEditBehaviors, CanPerformQc,
		CanUploadFiles, CanChangeVisibility, CanDelete )
	select A.UserID, N.AggregationID, A.CanSelect, A.CanEditItems,
		A.IsCurator, A.IsAdmin, A.CanEditMetadata, A.CanEditBehaviors, A.CanPerformQc,
		A.CanUploadFiles, A.CanChangeVisibility, A.CanDelete
	from mySobek_User_Edit_Aggregation A, #NeedsDefaults N
	where ( A.AggregationID = @allid )
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
	from mySobek_User_Group_Edit_Aggregation A, #NeedsDefaults N
	where ( A.AggregationID = @allid )
	  and ( not exists ( select * from mySobek_User_Group_Edit_Aggregation L where L.UserGroupID = A.UserGroupID and L.AggregationID = N.AggregationID ))
	  and (    ( A.CanEditMetadata = 'true' )
	        or ( A.CanEditBehaviors = 'true' )
	        or ( A.CanPerformQc = 'true' )
	        or ( A.CanUploadFiles = 'true' )
	        or ( A.CanChangeVisibility = 'true' )
	        or ( A.IsCurator = 'true' )
	        or ( A.IsAdmin = 'true' ));

	drop table #NeedsDefaults;
end;
GO


-- Seeds the install-wide default result fields, which collections use for any result view without
-- fields of their own.  These were seeded by Upgrade_to_Ver500.sql (for 4.x databases), but never
-- by the 5.x complete scripts, so every database built from scratch since 5.0 had an empty table and
-- fell back to a list hardcoded in the engine.  Same list as the 5.0 upgrade (with Publication Date
-- moved to its intended display order of 3), plus the VRA Core fields.  Added for every result type,
-- including THUMBNAIL, whose hover tooltip shows these fields.  Only runs if the table is empty, so
-- any customized defaults are left alone.
if ( not exists ( select 1 from SobekCM_Item_Aggregation_Default_Result_Fields ))
begin
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
end;
GO


-- Adds SobekCM_Save_Item_Aggregation_Result_Fields, so collection admins can choose the result fields shown
-- with each title in a collection's brief results view (and in the thumbnail view's hover tooltip, which shows
-- the same fields) from the Results tab.  Nothing wrote to SobekCM_Item_Aggregation_Result_Fields before this,
-- so every collection used the install-wide defaults.  The fields are stored against the collection's BRIEF
-- and THUMBNAIL result views, and the TABLE view (which does not show them) keeps the defaults.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF object_id('SobekCM_Save_Item_Aggregation_Result_Fields') IS NULL EXEC ('create procedure dbo.SobekCM_Save_Item_Aggregation_Result_Fields as select 1;');
GO

-- Saves the result fields customized for an item aggregation's brief and thumbnail results views.  With
-- @use_defaults set, just removes any customized fields, so the install-wide defaults are used.  Otherwise
-- each line of @fields is a metadata type id, a tab, then an optional override display term (blank to use
-- the standard term), in display order.
ALTER PROCEDURE [dbo].[SobekCM_Save_Item_Aggregation_Result_Fields]
	@code varchar(20),
	@use_defaults bit,
	@fields nvarchar(max)
AS
begin transaction

	-- Only continue if there is a match on the aggregation code
	declare @id int;
	set @id = ( select AggregationID from SobekCM_Item_Aggregation where Code = @code );

	if ( @id is not null )
	begin
		-- Get this aggregation's brief and thumbnail views, which share the same fields
		declare @views table ( ItemAggregationResultID int primary key );
		insert into @views
		select V.ItemAggregationResultID
		from SobekCM_Item_Aggregation_Result_Views V, SobekCM_Item_Aggregation_Result_Types T
		where ( V.AggregationID = @id )
		  and ( V.ItemAggregationResultTypeID = T.ItemAggregationResultTypeID )
		  and ( T.ResultType in ( 'BRIEF', 'THUMBNAIL' ));

		-- Remove the existing customized fields
		delete from SobekCM_Item_Aggregation_Result_Fields
		where ItemAggregationResultID in ( select ItemAggregationResultID from @views );

		-- Add the new fields, unless going back to the defaults
		if ( isnull(@use_defaults, 'false') = 'false' )
		begin
			declare @remaining nvarchar(max);
			declare @line nvarchar(max);
			declare @newline int;
			declare @tab int;
			declare @metadataid smallint;
			declare @term nvarchar(255);
			declare @order int;
			set @remaining = isnull(@fields, '');
			set @order = 0;

			-- Step through each line
			while ( len(@remaining) > 0 )
			begin
				-- Pull off the next line
				set @newline = charindex(char(10), @remaining);
				if ( @newline = 0 ) set @newline = len(@remaining) + 1;
				set @line = left(@remaining, @newline - 1);
				set @remaining = substring(@remaining, @newline + 1, len(@remaining));

				-- Split it into the metadata type id and the override display term
				set @tab = charindex(char(9), @line);
				if ( @tab > 0 )
				begin
					set @metadataid = try_cast(left(@line, @tab - 1) as smallint);
					set @term = left(ltrim(rtrim(substring(@line, @tab + 1, len(@line)))), 255);
				end
				else
				begin
					set @metadataid = try_cast(@line as smallint);
					set @term = '';
				end;

				-- Add this field to each view, skipping unknown metadata types and repeats
				if (( @metadataid is not null )
				  and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataTypeID = @metadataid ))
				  and ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Fields F, @views V where F.ItemAggregationResultID = V.ItemAggregationResultID and F.MetadataTypeID = @metadataid )))
				begin
					set @order = @order + 1;

					insert into SobekCM_Item_Aggregation_Result_Fields ( ItemAggregationResultID, MetadataTypeID, OverrideDisplayTerm, DisplayOrder, DisplayOptions )
					select ItemAggregationResultID, @metadataid, nullif(@term, ''), @order, null
					from @views;
				end;
			end;
		end;
	end;

commit transaction;
GO


-- Lets user admins see and change whether a user is active.  Deactivated users (isActive = false)
-- cannot log on by any method, since every user fetch procedure already filters on isActive.  Before
-- this, nothing in the application could change the flag, and the users admin screen could not even
-- open a deactivated user, because mySobek_Get_User_By_UserID only returns active users.
--   * mySobek_Get_User_By_UserID gains an optional @include_inactive parameter (default false, so every
--     existing caller is unchanged) and now returns the isActive column
--   * mySobek_Get_All_Users now returns the isActive column, for the active/all filter on the list
--   * New mySobek_Set_User_Active sets the flag

-- Procedures keep the SET options in effect when they are created. sqlcmd defaults QUOTED_IDENTIFIER
-- to OFF, which breaks deletes against tables with filtered indexes, so set both explicitly.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[mySobek_Get_User_By_UserID]
	@userid int,
	@include_inactive bit = 'false'
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Get the basic user information
	select UserID, ShibbID=coalesce(ShibbID,''), UserName=coalesce(UserName,''), EmailAddress=coalesce(EmailAddress,''), 
	  FirstName=coalesce(FirstName,''), LastName=coalesce(LastName,''), Note_Length, 
	  Can_Make_Folders_Public, isTemporary_Password, sendEmailOnSubmission, Can_Submit_Items, 
	  NickName=coalesce(NickName,''), Organization=coalesce(Organization, ''), College=coalesce(College,''),
	  Department=coalesce(Department,''), Unit=coalesce(Unit,''), Rights=coalesce(Default_Rights,''), Language=coalesce([UI_Language], ''), 
	  Internal_User, OrganizationCode, EditTemplate, EditTemplateMarc, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms,
	  Descriptions=( select COUNT(*) from mySobek_User_Description_Tags T where T.UserID=U.UserID),
	  Receive_Stats_Emails, Has_Item_Stats, Can_Delete_All_Items, ScanningTechnician, ProcessingTechnician, InternalNotes=coalesce(InternalNotes,''),
	  IsHostAdmin, IsUserAdmin, [Password]=coalesce([Password],''), ExternalProviderCode=coalesce(ExternalProviderCode,''), ExternalSubjectId=coalesce(ExternalSubjectId,''),
	  AuthenticationSource, isActive
	from mySobek_User U
	where ( UserID = @userid ) and (( isActive = 'true' ) or ( @include_inactive = 'true' ));

	-- Get the templates
	select T.TemplateCode, T.TemplateName, GroupDefined='false', DefaultTemplate
	from mySobek_Template T, mySobek_User_Template_Link L
	where ( L.UserID = @userid ) and ( L.TemplateID = T.TemplateID )
	union
	select T.TemplateCode, T.TemplateName, GroupDefined='true', 'false'
	from mySobek_Template T, mySobek_User_Group_Template_Link TL, mySobek_User_Group_Link GL
	where ( GL.UserID = @userid ) and ( GL.UserGroupID = TL.UserGroupID ) and ( TL.TemplateID = T.TemplateID )
	order by DefaultTemplate DESC, TemplateCode ASC;
	
	-- Get the default metadata
	select P.MetadataCode, P.MetadataName, GroupDefined='false', CurrentlySelected
	from mySobek_DefaultMetadata P, mySobek_User_DefaultMetadata_Link L
	where ( L.UserID = @userid ) and ( L.DefaultMetadataID = P.DefaultMetadataID )
	union
	select P.MetadataCode, P.MetadataName, GroupDefined='true', 'false'
	from mySobek_DefaultMetadata P, mySobek_User_Group_DefaultMetadata_Link PL, mySobek_User_Group_Link GL
	where ( GL.UserID = @userid ) and ( GL.UserGroupID = PL.UserGroupID ) and ( PL.DefaultMetadataID = P.DefaultMetadataID )
	order by CurrentlySelected DESC, MetadataCode ASC;

	-- Get the bib id's of items submitted
	select distinct( G.BibID )
	from mySobek_User_Folder F, mySobek_User_Item B, SobekCM_Item I, SobekCM_Item_Group G
	where ( F.UserID = @userid ) and ( B.UserFolderID = F.UserFolderID ) and ( F.FolderName = 'Submitted Items' ) and ( B.ItemID = I.ItemID ) and ( I.GroupID = G.GroupID );

	-- Get the regular expression for editable items
	select R.EditableRegex, GroupDefined='false', CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from mySobek_Editable_Regex R, mySobek_User_Editable_Link L
	where ( L.UserID = @userid ) and ( L.EditableID = R.EditableID )
	union
	select R.EditableRegex, GroupDefined='true', CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from mySobek_Editable_Regex R, mySobek_User_Group_Editable_Link L, mySobek_User_Group_Link GL
	where ( GL.UserID = @userid ) and ( GL.UserGroupID = L.UserGroupID ) and ( L.EditableID = R.EditableID );

	-- Get the list of aggregations associated with this user
	select A.Code, A.[Name], L.CanSelect, L.CanEditItems, L.IsAdmin AS IsAggregationAdmin, L.OnHomePage, L.IsCurator AS IsCollectionManager, GroupDefined='false', CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from SobekCM_Item_Aggregation A, mySobek_User_Edit_Aggregation L
	where  ( L.AggregationID = A.AggregationID ) and ( L.UserID = @userid )
	union
	select A.Code, A.[Name], L.CanSelect, L.CanEditItems, L.IsAdmin AS IsAggregationAdmin, OnHomePage = 'false', L.IsCurator AS IsCollectionManager, GroupDefined='true', CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from SobekCM_Item_Aggregation A, mySobek_User_Group_Edit_Aggregation L, mySobek_User_Group_Link GL
	where  ( L.AggregationID = A.AggregationID ) and ( GL.UserID = @userid ) and ( GL.UserGroupID = L.UserGroupID );

	-- Return the names of all the folders
	select F.FolderName, F.UserFolderID, ParentFolderID=isnull(F.ParentFolderID,-1), isPublic
	from mySobek_User_Folder F
	where ( F.UserID=@userid );

	-- Get the list of all items associated with a user folder (other than submitted items)
	select G.BibID, I.VID
	from mySobek_User_Folder F, mySobek_User_Item B, SobekCM_Item I, SobekCM_Item_Group G
	where ( F.UserID = @userid ) and ( B.UserFolderID = F.UserFolderID ) and ( F.FolderName != 'Submitted Items' ) and ( B.ItemID = I.ItemID ) and ( I.GroupID = G.GroupID );
	
	-- Get the list of all user groups associated with this user
	select G.GroupName, Can_Submit_Items, Internal_User, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms, G.UserGroupID
	from mySobek_User_Group G, mySobek_User_Group_Link L
	where ( G.UserGroupID = L.UserGroupID )
	  and ( L.UserID = @userid );
	  
	-- Get the user settings
	select * from mySobek_User_Settings where UserID=@userid order by Setting_Key;
	  
	-- Update the user table to include this as the last activity
	update mySobek_User
	set LastActivity = getdate()
	where UserID=@userid;
END;
GO

ALTER PROCEDURE [dbo].[mySobek_Get_All_Users] AS
BEGIN
	
	-- Get the list of users and pending user requests
	with pending_cte as 
	(
		select UserID, count(*) as PendingRequests
		from mySobek_User_Request
		where Pending='true'
		group by UserID
	)
	select U.UserID, LastName + ', ' + FirstName AS [Full_Name], UserName, EmailAddress, coalesce(R.PendingRequests,0) as PendingRequests, U.isActive
	from mySobek_User U left join 
		 pending_cte R on U.UserID = R.UserID 
	order by Full_Name;
END;
GO

IF object_id('mySobek_Set_User_Active') IS NULL EXEC ('create procedure dbo.mySobek_Set_User_Active as select 1;');
GO

-- Activates or deactivates a user.  A deactivated user cannot log on by any method.
ALTER PROCEDURE [dbo].[mySobek_Set_User_Active]
	@userid int,
	@isActive bit
AS
BEGIN
	update mySobek_User
	set isActive = @isActive
	where UserID = @userid;
END;
GO


-- Adds user news: short HTML messages shown in a banner at the very top of every page, for the
-- users each message targets, until that user closes it.  Once closed, a message is not shown to
-- that user again.  A message can target everyone, including visitors who are not logged on (for
-- example 'The library will be closed on Labor Day'), who close it with a cookie instead.  This
-- also lets upgrade scripts tell the administrators about a new release (see the 5.2.0 release
-- news at the end of this section) without emailing every customer about every patch.
--   * mySobek_News holds each message, who it targets and the dates it is shown between.  It can
--     target everyone, all logged-on users, administrators (system, portal, user and news
--     administrators, but never host administrators) and collection managers
--   * mySobek_News_User_Group_Link also targets a message at the members of specific user groups
--   * mySobek_News_User_Dismissed records each user who closed a message
--   * New procedures: mySobek_Get_Pending_News, mySobek_Dismiss_News, mySobek_Get_All_News,
--     mySobek_Save_News, mySobek_Delete_News and mySobek_Reset_News_Dismissals
--   * New News Administrator role (mySobek_User.IsNewsAdmin): can manage the news, and nothing else.
--     mySobek_Get_User_By_UserID returns it, and mySobek_Update_User gains an optional
--     @is_news_admin parameter (NULL leaves it unchanged, so existing callers are unaffected).
--     This redefines mySobek_Get_User_By_UserID, so it must run after the user active flag change.

if ( not exists ( select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'mySobek_News' ))
begin
	CREATE TABLE [dbo].[mySobek_News](
		[NewsID] [int] IDENTITY(1,1) NOT NULL,
		[Title] [nvarchar](255) NOT NULL,
		[Body] [nvarchar](max) NOT NULL,
		[ForEveryone] [bit] NOT NULL CONSTRAINT [DF_mySobek_News_ForEveryone] DEFAULT ((0)),
		[ForAllUsers] [bit] NOT NULL CONSTRAINT [DF_mySobek_News_ForAllUsers] DEFAULT ((0)),
		[ForAdmins] [bit] NOT NULL CONSTRAINT [DF_mySobek_News_ForAdmins] DEFAULT ((0)),
		[ForCollectionManagers] [bit] NOT NULL CONSTRAINT [DF_mySobek_News_ForCollectionManagers] DEFAULT ((0)),
		[StartDate] [date] NOT NULL CONSTRAINT [DF_mySobek_News_StartDate] DEFAULT (getdate()),
		[EndDate] [date] NULL,
		[IsActive] [bit] NOT NULL CONSTRAINT [DF_mySobek_News_IsActive] DEFAULT ((1)),
		[DateCreated] [datetime] NOT NULL CONSTRAINT [DF_mySobek_News_DateCreated] DEFAULT (getdate()),
		[CreatedBy] [nvarchar](100) NOT NULL CONSTRAINT [DF_mySobek_News_CreatedBy] DEFAULT (''),
		[DateModified] [datetime] NULL,
	 CONSTRAINT [PK_mySobek_News] PRIMARY KEY CLUSTERED ( [NewsID] ASC )
	);
end;
GO

-- No foreign key to mySobek_User_Group, so deleting a user group needs no change.  A link to a
-- deleted group simply never matches anyone.
if ( not exists ( select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'mySobek_News_User_Group_Link' ))
begin
	CREATE TABLE [dbo].[mySobek_News_User_Group_Link](
		[NewsID] [int] NOT NULL,
		[UserGroupID] [int] NOT NULL,
	 CONSTRAINT [PK_mySobek_News_User_Group_Link] PRIMARY KEY CLUSTERED ( [NewsID] ASC, [UserGroupID] ASC ),
	 CONSTRAINT [FK_mySobek_News_User_Group_Link_News] FOREIGN KEY ( [NewsID] ) REFERENCES [dbo].[mySobek_News] ( [NewsID] )
	);
end;
GO

if ( not exists ( select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'mySobek_News_User_Dismissed' ))
begin
	CREATE TABLE [dbo].[mySobek_News_User_Dismissed](
		[NewsID] [int] NOT NULL,
		[UserID] [int] NOT NULL,
		[DateDismissed] [datetime] NOT NULL CONSTRAINT [DF_mySobek_News_User_Dismissed_Date] DEFAULT (getdate()),
	 CONSTRAINT [PK_mySobek_News_User_Dismissed] PRIMARY KEY CLUSTERED ( [UserID] ASC, [NewsID] ASC ),
	 CONSTRAINT [FK_mySobek_News_User_Dismissed_News] FOREIGN KEY ( [NewsID] ) REFERENCES [dbo].[mySobek_News] ( [NewsID] )
	);
end;
GO

-- The News Administrator role
if ( not exists ( select 1 from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = 'mySobek_User' and COLUMN_NAME = 'IsNewsAdmin' ))
begin
	ALTER TABLE [dbo].[mySobek_User] ADD [IsNewsAdmin] [bit] NOT NULL CONSTRAINT [DF_mySobek_User_IsNewsAdmin] DEFAULT ((0));
end;
GO

-- Procedures keep the SET options in effect when they are created. sqlcmd defaults QUOTED_IDENTIFIER
-- to OFF, which breaks deletes against tables with filtered indexes, so set both explicitly.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns IsNewsAdmin as well.  Redefines the version from the user active flag change.
ALTER PROCEDURE [dbo].[mySobek_Get_User_By_UserID]
	@userid int,
	@include_inactive bit = 'false'
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Get the basic user information
	select UserID, ShibbID=coalesce(ShibbID,''), UserName=coalesce(UserName,''), EmailAddress=coalesce(EmailAddress,''), 
	  FirstName=coalesce(FirstName,''), LastName=coalesce(LastName,''), Note_Length, 
	  Can_Make_Folders_Public, isTemporary_Password, sendEmailOnSubmission, Can_Submit_Items, 
	  NickName=coalesce(NickName,''), Organization=coalesce(Organization, ''), College=coalesce(College,''),
	  Department=coalesce(Department,''), Unit=coalesce(Unit,''), Rights=coalesce(Default_Rights,''), Language=coalesce([UI_Language], ''), 
	  Internal_User, OrganizationCode, EditTemplate, EditTemplateMarc, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms,
	  Descriptions=( select COUNT(*) from mySobek_User_Description_Tags T where T.UserID=U.UserID),
	  Receive_Stats_Emails, Has_Item_Stats, Can_Delete_All_Items, ScanningTechnician, ProcessingTechnician, InternalNotes=coalesce(InternalNotes,''),
	  IsHostAdmin, IsUserAdmin, [Password]=coalesce([Password],''), ExternalProviderCode=coalesce(ExternalProviderCode,''), ExternalSubjectId=coalesce(ExternalSubjectId,''),
	  AuthenticationSource, isActive, IsNewsAdmin
	from mySobek_User U
	where ( UserID = @userid ) and (( isActive = 'true' ) or ( @include_inactive = 'true' ));

	-- Get the templates
	select T.TemplateCode, T.TemplateName, GroupDefined='false', DefaultTemplate
	from mySobek_Template T, mySobek_User_Template_Link L
	where ( L.UserID = @userid ) and ( L.TemplateID = T.TemplateID )
	union
	select T.TemplateCode, T.TemplateName, GroupDefined='true', 'false'
	from mySobek_Template T, mySobek_User_Group_Template_Link TL, mySobek_User_Group_Link GL
	where ( GL.UserID = @userid ) and ( GL.UserGroupID = TL.UserGroupID ) and ( TL.TemplateID = T.TemplateID )
	order by DefaultTemplate DESC, TemplateCode ASC;
	
	-- Get the default metadata
	select P.MetadataCode, P.MetadataName, GroupDefined='false', CurrentlySelected
	from mySobek_DefaultMetadata P, mySobek_User_DefaultMetadata_Link L
	where ( L.UserID = @userid ) and ( L.DefaultMetadataID = P.DefaultMetadataID )
	union
	select P.MetadataCode, P.MetadataName, GroupDefined='true', 'false'
	from mySobek_DefaultMetadata P, mySobek_User_Group_DefaultMetadata_Link PL, mySobek_User_Group_Link GL
	where ( GL.UserID = @userid ) and ( GL.UserGroupID = PL.UserGroupID ) and ( PL.DefaultMetadataID = P.DefaultMetadataID )
	order by CurrentlySelected DESC, MetadataCode ASC;

	-- Get the bib id's of items submitted
	select distinct( G.BibID )
	from mySobek_User_Folder F, mySobek_User_Item B, SobekCM_Item I, SobekCM_Item_Group G
	where ( F.UserID = @userid ) and ( B.UserFolderID = F.UserFolderID ) and ( F.FolderName = 'Submitted Items' ) and ( B.ItemID = I.ItemID ) and ( I.GroupID = G.GroupID );

	-- Get the regular expression for editable items
	select R.EditableRegex, GroupDefined='false', CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from mySobek_Editable_Regex R, mySobek_User_Editable_Link L
	where ( L.UserID = @userid ) and ( L.EditableID = R.EditableID )
	union
	select R.EditableRegex, GroupDefined='true', CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from mySobek_Editable_Regex R, mySobek_User_Group_Editable_Link L, mySobek_User_Group_Link GL
	where ( GL.UserID = @userid ) and ( GL.UserGroupID = L.UserGroupID ) and ( L.EditableID = R.EditableID );

	-- Get the list of aggregations associated with this user
	select A.Code, A.[Name], L.CanSelect, L.CanEditItems, L.IsAdmin AS IsAggregationAdmin, L.OnHomePage, L.IsCurator AS IsCollectionManager, GroupDefined='false', CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from SobekCM_Item_Aggregation A, mySobek_User_Edit_Aggregation L
	where  ( L.AggregationID = A.AggregationID ) and ( L.UserID = @userid )
	union
	select A.Code, A.[Name], L.CanSelect, L.CanEditItems, L.IsAdmin AS IsAggregationAdmin, OnHomePage = 'false', L.IsCurator AS IsCollectionManager, GroupDefined='true', CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from SobekCM_Item_Aggregation A, mySobek_User_Group_Edit_Aggregation L, mySobek_User_Group_Link GL
	where  ( L.AggregationID = A.AggregationID ) and ( GL.UserID = @userid ) and ( GL.UserGroupID = L.UserGroupID );

	-- Return the names of all the folders
	select F.FolderName, F.UserFolderID, ParentFolderID=isnull(F.ParentFolderID,-1), isPublic
	from mySobek_User_Folder F
	where ( F.UserID=@userid );

	-- Get the list of all items associated with a user folder (other than submitted items)
	select G.BibID, I.VID
	from mySobek_User_Folder F, mySobek_User_Item B, SobekCM_Item I, SobekCM_Item_Group G
	where ( F.UserID = @userid ) and ( B.UserFolderID = F.UserFolderID ) and ( F.FolderName != 'Submitted Items' ) and ( B.ItemID = I.ItemID ) and ( I.GroupID = G.GroupID );
	
	-- Get the list of all user groups associated with this user
	select G.GroupName, Can_Submit_Items, Internal_User, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms, G.UserGroupID
	from mySobek_User_Group G, mySobek_User_Group_Link L
	where ( G.UserGroupID = L.UserGroupID )
	  and ( L.UserID = @userid );
	  
	-- Get the user settings
	select * from mySobek_User_Settings where UserID=@userid order by Setting_Key;
	  
	-- Update the user table to include this as the last activity
	update mySobek_User
	set LastActivity = getdate()
	where UserID=@userid;
END;
GO

-- Edits the permission flags for a user.  @is_news_admin is optional, and NULL leaves it unchanged.
ALTER PROCEDURE [dbo].[mySobek_Update_User]
      @userid int,
      @can_submit bit,
      @is_internal bit,
      @can_edit_all bit,
      @can_delete_all bit,
	  @is_user_admin bit,
      @is_portal_admin bit,
      @is_system_admin bit,
	  @is_host_admin bit,
      @include_tracking_standard_forms bit,
      @edit_template varchar(20),
      @edit_template_marc varchar(20),
      @clear_projects_templates bit,
      @clear_aggregation_links bit,
      @clear_user_groups bit,
      @is_news_admin bit = null
AS
begin transaction

      -- Update the simple table values
      update mySobek_User
      set Can_Submit_Items=@can_submit, Internal_User=@is_internal,
            IsPortalAdmin=@is_portal_admin, IsSystemAdmin=@is_system_admin,
            Include_Tracking_Standard_Forms=@include_tracking_standard_forms,
            EditTemplate=@edit_template, Can_Delete_All_Items = @can_delete_all,
            EditTemplateMarc=@edit_template_marc, IsHostAdmin=@is_host_admin,
			IsUserAdmin=@is_user_admin, IsNewsAdmin=coalesce(@is_news_admin, IsNewsAdmin)
      where UserID=@userid;

      -- Check the flag to edit all items
      if ( @can_edit_all = 'true' )
      begin
            if ( ( select count(*) from mySobek_User_Editable_Link where EditableID=1 and UserID=@userid ) = 0 )
            begin
                  -- Add the link to the ALL EDITABLE
                  insert into mySobek_User_Editable_Link ( UserID, EditableID )
                  values ( @userid, 1 );
            end;
      end
      else
      begin
            -- Delete the link to all
            delete from mySobek_User_Editable_Link where EditableID = 1 and UserID=@userid;
      end;

      -- Clear the projects/templates
      if ( @clear_projects_templates = 'true' )
      begin
            delete from mySobek_User_DefaultMetadata_Link where UserID=@userid;
            delete from mySobek_User_Template_Link where UserID=@userid;
      end;

      -- Clear the projects/templates
      if ( @clear_aggregation_links = 'true' )
      begin
            delete from mySobek_User_Edit_Aggregation where UserID=@userid;
      end;

      -- Clear the user groups
      if ( @clear_user_groups = 'true' )
      begin
            delete from mySobek_User_Group_Link where UserID=@userid;
      end;

commit transaction;
GO

IF object_id('mySobek_Get_Pending_News') IS NULL EXEC ('create procedure dbo.mySobek_Get_Pending_News as select 1;');
GO

-- Gets the active news, within its display dates, that this user has not closed yet.  Who each
-- message targets is returned too, and the application picks the ones that apply to this user,
-- using the same role flags it uses for everything else.  Newest first.  Pass -1 to get only the
-- news for everyone, for visitors who are not logged on.
ALTER PROCEDURE [dbo].[mySobek_Get_Pending_News]
	@userid int
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select N.NewsID, N.Title, N.Body, N.ForEveryone, N.ForAllUsers, N.ForAdmins, N.ForCollectionManagers,
	  N.StartDate, N.EndDate, N.IsActive,
	  UserGroupIDs=coalesce(( select STRING_AGG(cast(L.UserGroupID as varchar(12)), ',') from mySobek_News_User_Group_Link L where L.NewsID = N.NewsID ), '')
	from mySobek_News N
	where ( N.IsActive = 'true' )
	  and ( N.StartDate <= cast(getdate() as date))
	  and (( N.EndDate is null ) or ( N.EndDate >= cast(getdate() as date)))
	  and (( @userid > 0 ) or ( N.ForEveryone = 'true' ))
	  and ( not exists ( select 1 from mySobek_News_User_Dismissed D where D.UserID = @userid and D.NewsID = N.NewsID ))
	order by N.StartDate DESC, N.NewsID DESC;
END;
GO

IF object_id('mySobek_Dismiss_News') IS NULL EXEC ('create procedure dbo.mySobek_Dismiss_News as select 1;');
GO

-- Records that a user closed a news message, so it is not shown to them again
ALTER PROCEDURE [dbo].[mySobek_Dismiss_News]
	@userid int,
	@newsid int
AS
BEGIN
	if (( exists ( select 1 from mySobek_News where NewsID = @newsid )) and ( not exists ( select 1 from mySobek_News_User_Dismissed where UserID = @userid and NewsID = @newsid )))
	begin
		insert into mySobek_News_User_Dismissed ( NewsID, UserID, DateDismissed )
		values ( @newsid, @userid, getdate());
	end;
END;
GO

IF object_id('mySobek_Get_All_News') IS NULL EXEC ('create procedure dbo.mySobek_Get_All_News as select 1;');
GO

-- Gets every news message, for the news admin screen, with how many users have closed each one
ALTER PROCEDURE [dbo].[mySobek_Get_All_News]
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select N.NewsID, N.Title, N.Body, N.ForEveryone, N.ForAllUsers, N.ForAdmins, N.ForCollectionManagers,
	  N.StartDate, N.EndDate, N.IsActive, N.DateCreated, N.CreatedBy, N.DateModified,
	  UserGroupIDs=coalesce(( select STRING_AGG(cast(L.UserGroupID as varchar(12)), ',') from mySobek_News_User_Group_Link L where L.NewsID = N.NewsID ), ''),
	  DismissedCount=( select count(*) from mySobek_News_User_Dismissed D where D.NewsID = N.NewsID )
	from mySobek_News N
	order by N.StartDate DESC, N.NewsID DESC;
END;
GO

IF object_id('mySobek_Save_News') IS NULL EXEC ('create procedure dbo.mySobek_Save_News as select 1;');
GO

-- Adds a new news message (when @newsid does not exist yet) or edits an existing one.  @usergroupids
-- is a comma-separated list of the user groups targeted, and replaces any existing group links.
-- Editing a message does not show it again to users who already closed it; call
-- mySobek_Reset_News_Dismissals for that.
ALTER PROCEDURE [dbo].[mySobek_Save_News]
	@newsid int,
	@title nvarchar(255),
	@body nvarchar(max),
	@foreveryone bit,
	@forallusers bit,
	@foradmins bit,
	@forcollectionmanagers bit,
	@startdate date,
	@enddate date,
	@isactive bit,
	@usergroupids varchar(max),
	@username nvarchar(100),
	@newid int output
AS
BEGIN
	SET NOCOUNT ON;

	BEGIN TRANSACTION;

	if ( exists ( select 1 from mySobek_News where NewsID = @newsid ))
	begin
		update mySobek_News
		set Title=@title, Body=@body, ForEveryone=@foreveryone, ForAllUsers=@forallusers, ForAdmins=@foradmins,
		    ForCollectionManagers=@forcollectionmanagers, StartDate=coalesce(@startdate, StartDate), EndDate=@enddate,
		    IsActive=@isactive, DateModified=getdate()
		where NewsID = @newsid;

		set @newid = @newsid;
	end
	else
	begin
		insert into mySobek_News ( Title, Body, ForEveryone, ForAllUsers, ForAdmins, ForCollectionManagers,
		    StartDate, EndDate, IsActive, DateCreated, CreatedBy )
		values ( @title, @body, @foreveryone, @forallusers, @foradmins, @forcollectionmanagers,
		    coalesce(@startdate, cast(getdate() as date)), @enddate, @isactive, getdate(), coalesce(@username, ''));

		set @newid = SCOPE_IDENTITY();
	end;

	-- Replace the user group links (parsed by hand, since STRING_SPLIT needs compatibility level 130)
	delete from mySobek_News_User_Group_Link where NewsID = @newid;

	declare @list varchar(max) = coalesce(@usergroupids, '') + ',';
	declare @pos int = charindex(',', @list);
	declare @groupid int;
	while ( @pos > 0 )
	begin
		set @groupid = TRY_CAST(nullif(ltrim(rtrim(left(@list, @pos - 1))), '') as int);
		set @list = substring(@list, @pos + 1, len(@list) + 1);

		if (( @groupid is not null ) and ( not exists ( select 1 from mySobek_News_User_Group_Link where NewsID = @newid and UserGroupID = @groupid )))
		begin
			insert into mySobek_News_User_Group_Link ( NewsID, UserGroupID )
			values ( @newid, @groupid );
		end;

		set @pos = charindex(',', @list);
	end;

	COMMIT TRANSACTION;
END;
GO

IF object_id('mySobek_Delete_News') IS NULL EXEC ('create procedure dbo.mySobek_Delete_News as select 1;');
GO

-- Deletes a news message, along with its user group links and the record of who closed it
ALTER PROCEDURE [dbo].[mySobek_Delete_News]
	@newsid int
AS
BEGIN
	delete from mySobek_News_User_Dismissed where NewsID = @newsid;
	delete from mySobek_News_User_Group_Link where NewsID = @newsid;
	delete from mySobek_News where NewsID = @newsid;
END;
GO

IF object_id('mySobek_Reset_News_Dismissals') IS NULL EXEC ('create procedure dbo.mySobek_Reset_News_Dismissals as select 1;');
GO

-- Forgets who closed a news message, so it is shown again to everyone it targets
ALTER PROCEDURE [dbo].[mySobek_Reset_News_Dismissals]
	@newsid int
AS
BEGIN
	delete from mySobek_News_User_Dismissed where NewsID = @newsid;
END;
GO

-- Release news for the administrators.  Each upgrade script can add one of these.  The title
-- check means running the script twice does not add it twice.  It stops showing after 90 days,
-- so administrators added long after the upgrade are not told about it.
if ( not exists ( select 1 from mySobek_News where Title = 'SobekCM has been upgraded to version 5.2.0' ))
begin
	declare @news511 int;
	declare @news511_end date = dateadd(day, 90, getdate());
	exec mySobek_Save_News -1, 'SobekCM has been upgraded to version 5.2.0',
		'<p>This site is now running SobekCM 5.2.0, a new release. Changes you may notice:</p><ul><li><strong>Site news:</strong> messages like this one now appear at the top of the page for the people they are meant for, until each person closes them. Use <em>Admin &gt; Site News</em> to post your own, for everyone (such as a holiday closing) or just certain users. The new <em>News Administrator</em> role lets someone manage the news without any other administrative rights.</li><li><strong>Collection result fields:</strong> the <em>Results</em> tab of each collection can now choose the fields shown with each title in the brief view and the thumbnail tooltip.</li><li><strong>Inactive users:</strong> the users admin screen can now deactivate a user, who can then no longer log on.</li><li><strong>New institutions</strong> added automatically when an item is loaded now get the same facets, result views and permissions as any other new collection.</li></ul><p>For more information, see the <a href="https://sobekrepository.org/sobekcm/currentversion/">Release Notes</a>.</p>',
		'false', 'false', 'true', 'false',
		null, @news511_end, 'true', '', 'Upgrade_to_Ver520.sql', @news511 output;
end;
GO


-- Adds a System User flag (mySobek_User.IsSystemUser), for accounts that must never be locked out
-- (e.g. service/integration accounts). Not setable anywhere in the UI - defaults to 0 and is only
-- ever set directly against the database. When set, the users admin screen's deactivate checkbox is
-- replaced with a "System users cannot be deactivated" message, and the flag cannot be changed
-- through a form postback either. System users are also hidden entirely from the users admin list
-- for anyone who is not the top-level admin (Host Administrator if hosted, otherwise System
-- Administrator); the top-level admin can choose to show them via a System Users filter.
--   * mySobek_Get_User_By_UserID now returns IsSystemUser, so the users admin screen can see it.
--     This redefines mySobek_Get_User_By_UserID, so it must run after the user active flag change
--     and the user news change above.
--   * mySobek_Get_All_Users now returns IsSystemUser too, for the users admin list and its filter.

if ( not exists ( select 1 from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = 'mySobek_User' and COLUMN_NAME = 'IsSystemUser' ))
begin
	ALTER TABLE [dbo].[mySobek_User] ADD [IsSystemUser] [bit] NOT NULL CONSTRAINT [DF_mySobek_User_IsSystemUser] DEFAULT ((0));
end;
GO

-- Procedures keep the SET options in effect when they are created. sqlcmd defaults QUOTED_IDENTIFIER
-- to OFF, which breaks deletes against tables with filtered indexes, so set both explicitly.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns IsSystemUser as well. Redefines the version from the user news change above.
ALTER PROCEDURE [dbo].[mySobek_Get_User_By_UserID]
	@userid int,
	@include_inactive bit = 'false'
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Get the basic user information
	select UserID, ShibbID=coalesce(ShibbID,''), UserName=coalesce(UserName,''), EmailAddress=coalesce(EmailAddress,''),
	  FirstName=coalesce(FirstName,''), LastName=coalesce(LastName,''), Note_Length,
	  Can_Make_Folders_Public, isTemporary_Password, sendEmailOnSubmission, Can_Submit_Items,
	  NickName=coalesce(NickName,''), Organization=coalesce(Organization, ''), College=coalesce(College,''),
	  Department=coalesce(Department,''), Unit=coalesce(Unit,''), Rights=coalesce(Default_Rights,''), Language=coalesce([UI_Language], ''),
	  Internal_User, OrganizationCode, EditTemplate, EditTemplateMarc, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms,
	  Descriptions=( select COUNT(*) from mySobek_User_Description_Tags T where T.UserID=U.UserID),
	  Receive_Stats_Emails, Has_Item_Stats, Can_Delete_All_Items, ScanningTechnician, ProcessingTechnician, InternalNotes=coalesce(InternalNotes,''),
	  IsHostAdmin, IsUserAdmin, [Password]=coalesce([Password],''), ExternalProviderCode=coalesce(ExternalProviderCode,''), ExternalSubjectId=coalesce(ExternalSubjectId,''),
	  AuthenticationSource, isActive, IsNewsAdmin, IsSystemUser
	from mySobek_User U
	where ( UserID = @userid ) and (( isActive = 'true' ) or ( @include_inactive = 'true' ));

	-- Get the templates
	select T.TemplateCode, T.TemplateName, GroupDefined='false', DefaultTemplate
	from mySobek_Template T, mySobek_User_Template_Link L
	where ( L.UserID = @userid ) and ( L.TemplateID = T.TemplateID )
	union
	select T.TemplateCode, T.TemplateName, GroupDefined='true', 'false'
	from mySobek_Template T, mySobek_User_Group_Template_Link TL, mySobek_User_Group_Link GL
	where ( GL.UserID = @userid ) and ( GL.UserGroupID = TL.UserGroupID ) and ( TL.TemplateID = T.TemplateID )
	order by DefaultTemplate DESC, TemplateCode ASC;

	-- Get the default metadata
	select P.MetadataCode, P.MetadataName, GroupDefined='false', CurrentlySelected
	from mySobek_DefaultMetadata P, mySobek_User_DefaultMetadata_Link L
	where ( L.UserID = @userid ) and ( L.DefaultMetadataID = P.DefaultMetadataID )
	union
	select P.MetadataCode, P.MetadataName, GroupDefined='true', 'false'
	from mySobek_DefaultMetadata P, mySobek_User_Group_DefaultMetadata_Link PL, mySobek_User_Group_Link GL
	where ( GL.UserID = @userid ) and ( GL.UserGroupID = PL.UserGroupID ) and ( PL.DefaultMetadataID = P.DefaultMetadataID )
	order by CurrentlySelected DESC, MetadataCode ASC;

	-- Get the bib id's of items submitted
	select distinct( G.BibID )
	from mySobek_User_Folder F, mySobek_User_Item B, SobekCM_Item I, SobekCM_Item_Group G
	where ( F.UserID = @userid ) and ( B.UserFolderID = F.UserFolderID ) and ( F.FolderName = 'Submitted Items' ) and ( B.ItemID = I.ItemID ) and ( I.GroupID = G.GroupID );

	-- Get the regular expression for editable items
	select R.EditableRegex, GroupDefined='false', CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from mySobek_Editable_Regex R, mySobek_User_Editable_Link L
	where ( L.UserID = @userid ) and ( L.EditableID = R.EditableID )
	union
	select R.EditableRegex, GroupDefined='true', CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from mySobek_Editable_Regex R, mySobek_User_Group_Editable_Link L, mySobek_User_Group_Link GL
	where ( GL.UserID = @userid ) and ( GL.UserGroupID = L.UserGroupID ) and ( L.EditableID = R.EditableID );

	-- Get the list of aggregations associated with this user
	select A.Code, A.[Name], L.CanSelect, L.CanEditItems, L.IsAdmin AS IsAggregationAdmin, L.OnHomePage, L.IsCurator AS IsCollectionManager, GroupDefined='false', CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from SobekCM_Item_Aggregation A, mySobek_User_Edit_Aggregation L
	where  ( L.AggregationID = A.AggregationID ) and ( L.UserID = @userid )
	union
	select A.Code, A.[Name], L.CanSelect, L.CanEditItems, L.IsAdmin AS IsAggregationAdmin, OnHomePage = 'false', L.IsCurator AS IsCollectionManager, GroupDefined='true', CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from SobekCM_Item_Aggregation A, mySobek_User_Group_Edit_Aggregation L, mySobek_User_Group_Link GL
	where  ( L.AggregationID = A.AggregationID ) and ( GL.UserID = @userid ) and ( GL.UserGroupID = L.UserGroupID );

	-- Return the names of all the folders
	select F.FolderName, F.UserFolderID, ParentFolderID=isnull(F.ParentFolderID,-1), isPublic
	from mySobek_User_Folder F
	where ( F.UserID=@userid );

	-- Get the list of all items associated with a user folder (other than submitted items)
	select G.BibID, I.VID
	from mySobek_User_Folder F, mySobek_User_Item B, SobekCM_Item I, SobekCM_Item_Group G
	where ( F.UserID = @userid ) and ( B.UserFolderID = F.UserFolderID ) and ( F.FolderName != 'Submitted Items' ) and ( B.ItemID = I.ItemID ) and ( I.GroupID = G.GroupID );

	-- Get the list of all user groups associated with this user
	select G.GroupName, Can_Submit_Items, Internal_User, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms, G.UserGroupID
	from mySobek_User_Group G, mySobek_User_Group_Link L
	where ( G.UserGroupID = L.UserGroupID )
	  and ( L.UserID = @userid );

	-- Get the user settings
	select * from mySobek_User_Settings where UserID=@userid order by Setting_Key;

	-- Update the user table to include this as the last activity
	update mySobek_User
	set LastActivity = getdate()
	where UserID=@userid;
END;
GO

-- Returns IsSystemUser as well, for the users admin list's System Users filter
ALTER PROCEDURE [dbo].[mySobek_Get_All_Users] AS
BEGIN

	-- Get the list of users and pending user requests
	with pending_cte as
	(
		select UserID, count(*) as PendingRequests
		from mySobek_User_Request
		where Pending='true'
		group by UserID
	)
	select U.UserID, LastName + ', ' + FirstName AS [Full_Name], UserName, EmailAddress, coalesce(R.PendingRequests,0) as PendingRequests, U.isActive, U.IsSystemUser
	from mySobek_User U left join
		 pending_cte R on U.UserID = R.UserID
	order by Full_Name;
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
	values ( 5, 2, '0' );
end
else
begin
	update SobekCM_Database_Version
	set Major_Version=5, Minor_Version=2, Release_Phase='0';
end;
GO
