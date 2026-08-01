/** 
Upgrade_to_Ver500.sql  - OpenSobek

Takes an existing 4.11.0 database and brings it up to Version 5.0.0: adds
 new tables and columns, widens/retypes a few existing columns, backfills data 
 for the new columns where needed, and updates stored procedures to their 
 current definitions. Run this against a production database that's still 
 on an older version.  If your version is older than 4.11.0 you will need 
 to find the original update scripts in the Previous Version, and run those 
 to get your db version up to 4.11.0 first. 
 
 */


/**************************************************************************/
/**                                                                      **/
/**   Drop old entities which are no longer needed                       **/
/**                                                                      **/
/**************************************************************************/

/* Drop the indexed view of metadata tables, if it exists */
IF OBJECT_ID('dbo.Metadata_Item_Link_Indexed_View', 'V') IS NOT NULL
BEGIN
	DROP VIEW dbo.Metadata_Item_Link_Indexed_View;
END
GO

/* Drop the old metadata tables */
drop table SobekCM_Metadata_Basic_Search_Table;
drop table SobekCM_Item_Aggregation_Metadata_Link;
drop table SobekCM_Metadata_Unique_Link;
drop table SobekCM_Metadata_Unique_Search_Table;
drop table SobekCM_Search_Stop_Words;
drop table SobekCM_Metadata_Translation;
GO

/* Drop some UF-specific and legacy tables */
drop table FDA_Report;
drop table FDA_Report_Type;
drop table Tivoli_File_Log;
drop table Tivoli_File_Request;
drop table Tracking_Archive_Item_Link;
drop table Tracking_ArchiveMedia;
GO

/* Drop procedures no longer needed */
DROP PROCEDURE dbo.Admin_Update_Cached_Aggregation_Metadata_Links;
DROP PROCEDURE dbo.Admin_Update_Single_Cached_Aggregation_Metadata_Links; 
DROP PROCEDURE dbo.FDA_All_Reports;
DROP PROCEDURE dbo.FDA_All_Reports_By_Date;
DROP PROCEDURE dbo.FDA_Get_Report_By_ID;
DROP PROCEDURE dbo.FDA_Get_Reports_By_Package;
DROP PROCEDURE dbo.FDA_Report_Save;
DROP PROCEDURE dbo.mySobek_Get_Public_Folder_Browse; 
DROP PROCEDURE dbo.mySobek_Get_User_Folder_Browse; 
DROP PROCEDURE dbo.SobekCM_Create_Full_Citation_Value;
DROP PROCEDURE dbo.SobekCM_Get_Aggregation_Browse_Paged;
DROP PROCEDURE dbo.SobekCM_Get_Aggregation_Browse_Paged2;
DROP PROCEDURE dbo.SobekCM_Get_All_Browse_Paged;
DROP PROCEDURE dbo.SobekCM_Get_All_Browse_Paged2;
DROP PROCEDURE dbo.SobekCM_Get_Metadata_Browse;
DROP PROCEDURE dbo.SobekCM_Metadata_Basic_Search_Paged;
DROP PROCEDURE dbo.SobekCM_Metadata_Basic_Search_Paged2 
DROP PROCEDURE dbo.SobekCM_Metadata_By_Bib_Vid;
DROP PROCEDURE dbo.SobekCM_Metadata_Clear2;
DROP PROCEDURE dbo.SobekCM_Metadata_Exact_Search_Paged;
DROP PROCEDURE dbo.SobekCM_Metadata_Exact_Search_Paged2;
DROP PROCEDURE dbo.SobekCM_Metadata_Save;
DROP PROCEDURE dbo.SobekCM_Metadata_Search_Paged;
DROP PROCEDURE dbo.SobekCM_Metadata_Save_Single;
DROP PROCEDURE dbo.SobekCM_Online_Archived_Space;
DROP PROCEDURE dbo.SobekCM_Save_Item_Ticklers;
DROP PROCEDURE dbo.SobekCM_Save_Item_VRACore_Extensions;
DROP PROCEDURE dbo.Tivoli_Add_File_Archive_Log;
DROP PROCEDURE dbo.Tivoli_Admin_Update;
DROP PROCEDURE dbo.Tivoli_Complete_File_Request;
DROP PROCEDURE dbo.Tivoli_Get_File_By_Bib_VID;
DROP PROCEDURE dbo.Tivoli_Outstanding_File_Requests;
DROP PROCEDURE dbo.Tivoli_Request_File;
DROP PROCEDURE dbo.Tracking_Metadata_Basic_Search;
DROP PROCEDURE dbo.Tracking_Metadata_Exact_Search;
DROP PROCEDURE dbo.Tracking_Metadata_Search;
DROP PROCEDURE dbo.Tracking_Get_Aggregation_Privates;
DROP PROCEDURE dbo.SobekCM_Get_BibID_VID_From_Identifier;
DROP PROCEDURE dbo.SobekCM_Admin_Suggest_User_Item_Links;
DROP PROCEDURE dbo.SobekCM_Get_Items_By_Coordinates;
DROP PROCEDURE dbo.Admin_Update_All_AggregationCodes_Values;
DROP PROCEDURE dbo.Importer_Load_Lookup_Tables;
DROP PROCEDURE dbo.SobekCM_Add_Item_Error_Log;
DROP PROCEDURE dbo.SobekCM_Aggregation_Change_Parent;
DROP PROCEDURE dbo.SobekCM_Clear_External_Record_Numbers;
DROP PROCEDURE dbo.SobekCM_Clear_Web_Skin_Portal_Link;
DROP PROCEDURE dbo.SobekCM_Edit_Aggregation_Details;
DROP PROCEDURE dbo.SobekCM_Get_DefaultMetadata_By_ProjectID;
DROP PROCEDURE dbo.SobekCM_Get_Group_Titles;
DROP PROCEDURE dbo.SobekCM_Get_Item_Aggregation_Count;
DROP PROCEDURE dbo.SobekCM_Get_Item_Aggregation_Milestone;
DROP PROCEDURE dbo.SobekCM_Get_Items_By_ProjectID;
DROP PROCEDURE dbo.SobekCM_Get_OAI_Data_Codes;
DROP PROCEDURE dbo.SobekCM_Get_Templates_By_ProjectID;
DROP PROCEDURE dbo.SobekCM_Importer_Load_Lookup_Tables;
DROP PROCEDURE dbo.SobekCM_Link_Aggregation_Thematic_Heading;
DROP PROCEDURE dbo.SobekCM_Recreate_All_Implied_Links;
DROP PROCEDURE dbo.SobekCM_Save_Item_Aggregation_Hierarchy_Link;
DROP PROCEDURE dbo.SobekCM_Save_Item_Complete_KML;
DROP PROCEDURE dbo.SobekCM_Save_Item_Group_Behaviors;
DROP PROCEDURE dbo.SobekCM_Set_Additional_Work_Needed;
DROP PROCEDURE dbo.SobekCM_Statistics_Item_Group;
DROP PROCEDURE dbo.SobekCM_Stats_Get_User_Linked_Items;
DROP PROCEDURE dbo.SobekCM_Web_Skin_Portal_Add;
DROP PROCEDURE dbo.Tracking_Add_Past_Workflow;
DROP PROCEDURE dbo.Tracking_Add_Workflow;
DROP PROCEDURE dbo.Tracking_Born_Digital_Item_Count;
DROP PROCEDURE dbo.Tracking_Edit_OCR_Progress;
DROP PROCEDURE dbo.Tracking_OCR_Complete;
DROP PROCEDURE dbo.Tracking_PreQC_Complete;
DROP PROCEDURE dbo.Tracking_Submit_QC_Log;
DROP PROCEDURE dbo.Tracking_Update_Physical_Milestones;
DROP PROCEDURE dbo.mySobek_Add_User_Request;
DROP PROCEDURE dbo.mySobek_Delete_Template;
DROP PROCEDURE dbo.mySobek_Get_All_Projects_DefaultMetadatas;
DROP PROCEDURE dbo.mySobek_Get_User_Item_Link_Relationships;
DROP PROCEDURE dbo.SobekCM_Get_All_Groups_First_VID;
DROP PROCEDURE dbo.mySobek_Set_Receive_Stats_Email_Flag;
DROP PROCEDURE dbo.mySobek_Link_User_To_Item;
DROP PROCEDURE dbo.SobekCM_Link_User_To_Item;
DROP PROCEDURE dbo.SobekCM_Get_All_Regions;
DROP PROCEDURE dbo.SobekCM_Clear_Region_Link_By_Item;
DROP PROCEDURE dbo.SobekCM_Save_Item_Views;
DROP PROCEDURE dbo.Tracking_Update_Disposition_Advice;
DROP PROCEDURE dbo.Tracking_Update_Disposition;
DROP PROCEDURE dbo.Tracking_Add_Past_Workflow_By_ItemID;
DROP PROCEDURE dbo.Tracking_Update_Born_Digital;
DROP PROCEDURE dbo.SobekCM_Update_Item_Online_Statistics;
DROP PROCEDURE dbo.SobekCM_Check_For_Record_Existence;
DROP PROCEDURE dbo.Tracking_Box_List;
DROP PROCEDURE dbo.Tracking_Get_All_Possible_Disposition_Types;
DROP PROCEDURE dbo.Tracking_Get_All_Possible_Workflows;
DROP PROCEDURE dbo.SobekCM_Manager_GroupID_From_BibID;
DROP PROCEDURE dbo.Tracking_Items_Pending_Online_Complete;
DROP PROCEDURE dbo.SobekCM_Manager_Newspapers_Without_Serial_Info;
DROP PROCEDURE dbo.Tracking_Archive_Complete;
DROP PROCEDURE dbo.Tracking_Get_Aggregation_Browse;
DROP PROCEDURE dbo.Tracking_Items_By_OCLC;
DROP PROCEDURE dbo.Tracking_Items_By_ALEPH;
DROP PROCEDURE dbo.SobekCM_Delete_Setting;
DROP PROCEDURE dbo.mySobek_Change_Password;
DROP PROCEDURE dbo.mySobek_Get_User_By_UserName_Password;
DROP PROCEDURE dbo.SobekCM_Get_Search_Stop_Words;
DROP PROCEDURE dbo.SobekCM_Get_Translation;
DROP PROCEDURE dbo.SobekCM_Get_Item_Details2;
GO

DROP FULLTEXT CATALOG [BasicSearchCatalog];
GO

DROP FULLTEXT CATALOG [UniqueMetadataCatalog];
GO

DROP TYPE [dbo].[TempPagedItemsTableType];
GO

DROP FULLTEXT STOPLIST [SobekStopList];
GO


/**************************************************************************/
/**                                                                      **/
/**   Double check these column definition changes                       **/
/**                                                                      **/
/**************************************************************************/

-- SobekCM_Item.RestrictionMessage: nvarchar(1000) -> nvarchar(1024)
ALTER TABLE dbo.SobekCM_Item ALTER COLUMN RestrictionMessage nvarchar(1024) NULL;
GO

-- SobekCM_Extension.Name: varchar(100) NULL -> nvarchar(255) NOT NULL
-- Backfill first: existing rows may have NULL, which ALTER COLUMN ... NOT NULL would reject.
UPDATE dbo.SobekCM_Extension SET Name = '' WHERE Name IS NULL;
GO

ALTER TABLE dbo.SobekCM_Extension ALTER COLUMN Name nvarchar(255) NOT NULL;
GO

-- SobekCM_Item_Aggregation_Facets.FacetOptions: nvarchar(255) -> nvarchar(2000)
ALTER TABLE dbo.SobekCM_Item_Aggregation_Facets ALTER COLUMN FacetOptions nvarchar(2000) NULL;
GO

-- SobekCM_Item_Aggregation_Result_Fields.DisplayOptions: nvarchar(255) -> nvarchar(2000)
ALTER TABLE dbo.SobekCM_Item_Aggregation_Result_Fields ALTER COLUMN DisplayOptions nvarchar(2000) NULL;
GO

-- SobekCM_Item_Aggregation_Milestones.Milestone: varchar(max) -> nvarchar(max)
ALTER TABLE dbo.SobekCM_Item_Aggregation_Milestones ALTER COLUMN Milestone nvarchar(max) NOT NULL;
GO

-- SobekCM_WebContent_Milestones.Milestone: varchar(max) -> nvarchar(max)
ALTER TABLE dbo.SobekCM_WebContent_Milestones ALTER COLUMN Milestone nvarchar(max) NOT NULL;
GO

-- Add the new item group columns, if they don't exist
if ( COL_LENGTH('dbo.SobekCM_Item_Group', 'HasGroupMetadata') is null )
begin
	alter table SobekCM_Item_Group add HasGroupMetadata bit default('false') not null;
end;
GO

if ( COL_LENGTH('dbo.SobekCM_Item_Group', 'CustomThumbnail') is null )
begin
	alter table SobekCM_Item_Group add CustomThumbnail nvarchar(255) null;
end;
GO

if ( COL_LENGTH('dbo.SobekCM_Item_Group', 'ThumbnailType') is null )
begin
	alter table SobekCM_Item_Group add ThumbnailType tinyint default(0) not null;
end;
GO

if ( COL_LENGTH('dbo.SobekCM_Item_Group', 'FlagByte') is null )
begin
	alter table SobekCM_Item_Group add FlagByte tinyint default(0) not null;
end;
GO

if ( COL_LENGTH('dbo.SobekCM_Item_Group', 'LastFourInt') is null )
begin
	alter table SobekCM_Item_Group add LastFourInt smallint null;
end;
GO

print 'Updating LastFourInt column... for all item group rows'

update SobekCM_Item_Group 
set LastFourInt = cast(substring(BibID, 7, 4) as smallint );
GO



/**************************************************************************/
/**                                                                      **/
/**   Drop existing column from table                                    **/
/**                                                                      **/
/**************************************************************************/

/* Drop the default constraint on Browse_Results_Display_SQL, then the column itself */
DECLARE @constraintName sysname;

SELECT @constraintName = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c
	ON c.object_id = dc.parent_object_id
   AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.SobekCM_Item_Aggregation')
  AND c.name = 'Browse_Results_Display_SQL';

IF @constraintName IS NOT NULL
BEGIN
	EXEC('ALTER TABLE dbo.SobekCM_Item_Aggregation DROP CONSTRAINT [' + @constraintName + ']');
END
GO

ALTER TABLE dbo.SobekCM_Item_Aggregation DROP COLUMN Browse_Results_Display_SQL;
GO



/**************************************************************************/
/**                                                                      **/
/**   New tables and columns on existing tables                          **/
/**                                                                      **/
/**************************************************************************/

-- Lookup of storage backends -- lets Glacier (or anything else) get added later with no schema change
CREATE TABLE Archive_Location (
    ArchiveLocationID smallint IDENTITY(1,1) NOT NULL,
    LocationName varchar(50) NOT NULL,          -- e.g. 'GCS Cold Storage', 'AWS Glacier'
    LocationType varchar(20) NOT NULL,          -- e.g. 'GCS', 'Glacier'
    ContainerName varchar(255) NULL,            -- bucket/container name
    IsActive bit NOT NULL DEFAULT(1),
    Notes nvarchar(500) NULL,
    CONSTRAINT PK_SobekCM_Archive_Location PRIMARY KEY CLUSTERED (ArchiveLocationID)
);
GO

-- Stable identity: this page/file of this item has been archived, period.
-- Exactly one row per (ItemID, FileName), no matter how many times it's re-archived later.
CREATE TABLE Archive_Item_Archived_File (
    ArchivedFileID   int IDENTITY(1,1) NOT NULL,
    ItemID           int NOT NULL,
    [FileName]         varchar(255) NOT NULL,
    FileExtension    varchar(20) NOT NULL,          -- e.g. 'tif', 'mp3' -- denormalized off FileName for easy analysis
    CONSTRAINT PK_Archive_Item_Archived_File PRIMARY KEY CLUSTERED (ArchivedFileID),
    CONSTRAINT FK_Archived_File_Item FOREIGN KEY (ItemID) REFERENCES SobekCM_Item(ItemID),
    CONSTRAINT UQ_Archived_File_Item_FileName UNIQUE (ItemID, FileName)
);
GO

-- One row per archiving EVENT for that file -- captures size/hash/creation-date as they
-- were at that moment. Re-archiving (correction, reprocessing) adds a new snapshot rather
-- than overwriting, so history is preserved.
CREATE TABLE Archive_Item_Archived_File_Snapshot (
    SnapshotID              int IDENTITY(1,1) NOT NULL,
    ArchivedFileID           int NOT NULL,
    FileSize                 bigint NOT NULL,
    OriginalCreationDate      datetime NOT NULL,
    SHA256_Hash               char(64) NOT NULL,
    SnapshotDate              datetime NOT NULL,     -- was ArchivedDate
    MimeType                  varchar(100) NULL,     -- e.g. 'audio/mpeg', 'image/tiff'
    EncodingDetails           varchar(500) NULL,     -- e.g. codec/bitrate/compression details, for format-obsolescence tracking
    CONSTRAINT PK_Archived_File_Snapshot PRIMARY KEY CLUSTERED (SnapshotID),
    CONSTRAINT FK_Archived_File_Snapshot_File FOREIGN KEY (ArchivedFileID) REFERENCES Archive_Item_Archived_File(ArchivedFileID)
);
CREATE INDEX IX_Archived_File_Snapshot_FileID ON Archive_Item_Archived_File_Snapshot(ArchivedFileID);
GO

-- One row per stored copy of a specific snapshot -- one per location, so a given
-- snapshot can have both a GCS row and (later) a Glacier row simultaneously
CREATE TABLE Archive_Item_Archived_File_Copy (
    ArchivedFileCopyID int IDENTITY(1,1) NOT NULL,
    SnapshotID int NOT NULL,                          -- FK to SobekCM_Item_Archived_File_Snapshot
    ArchiveLocationID smallint NOT NULL,              -- FK to SobekCM_Archive_Location
    StoragePath varchar(1000) NOT NULL,               -- full path/key, e.g. UOC\AA00008198\00001\20260214\...
    StoredDate  datetime NOT NULL,
    VerifiedDate datetime NULL,
    [Status] varchar(20) NOT NULL DEFAULT('Stored'),  -- Pending / Stored / Verified / Failed / Deleted
    CONSTRAINT PK_Archived_File_Copy PRIMARY KEY CLUSTERED (ArchivedFileCopyID),
    CONSTRAINT FK_Archived_File_Copy_Snapshot FOREIGN KEY (SnapshotID) REFERENCES Archive_Item_Archived_File_Snapshot(SnapshotID),
    CONSTRAINT FK_Archived_File_Copy_Location FOREIGN KEY (ArchiveLocationID) REFERENCES Archive_Location(ArchiveLocationID)
);
CREATE INDEX IX_Archived_File_Copy_SnapshotID ON Archive_Item_Archived_File_Copy(SnapshotID);
GO

ALTER TABLE mySobek_User ADD ExternalProviderCode nvarchar(50) null;
ALTER TABLE mySobek_User ADD ExternalSubjectId nvarchar(450) null;
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_mySobek_User_ExternalLogin
ON mySobek_User (ExternalProviderCode, ExternalSubjectId)
WHERE ExternalProviderCode IS NOT NULL AND ExternalSubjectId IS NOT NULL;
GO

alter table mySobek_User add AuthenticationSource varchar(100) not null default('');
GO

update mySobek_User set AuthenticationSource = 'Registered' where AuthenticationSource = '';
GO


/**************************************************************************/
/**                                                                      **/
/**   Create / alter all stored procedures so they are current           **/
/**                                                                      **/
/**************************************************************************/

/****** Object:  StoredProcedure [dbo].[Admin_Unembargo_Items_Past_Embargo_Date]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[Admin_Unembargo_Items_Past_Embargo_Date] 
	@subject_line varchar(500),
	@email_message varchar(max),
	@send_email bit
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	SET NOCOUNT ON;

	-- Get the items that need to be processed
	select I.ItemID, G.BibID, I.VID, CONVERT(nvarchar(10), T.EmbargoEnd, 102) as EmbargoEnd, substring(I.Title,4,1000) as Title, substring(I.Author, 4, 1000) as Author
	into #Unembargo_Items
	from SobekCM_Item I, Tracking_Item T, SobekCM_Item_Group G
	where ( I.ItemID=T.ItemID )
	  and (( I.IP_Restriction_Mask <> 0 ) or ( I.Dark = 'true' ))
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
	set Dark='false', IP_Restriction_Mask=0, AdditionalWorkNeeded='true'
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
/****** Object:  StoredProcedure [dbo].[Archive_Get_Item_History]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the full archiving history (files, snapshots, and stored copies) for a single item
CREATE OR ALTER PROCEDURE [dbo].[Archive_Get_Item_History]
	@ItemID int
AS
BEGIN

	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select F.ArchivedFileID, F.[FileName], F.FileExtension,
	       S.SnapshotID, S.FileSize, S.OriginalCreationDate, S.SHA256_Hash, S.SnapshotDate, S.MimeType, S.EncodingDetails,
	       C.ArchivedFileCopyID, C.StoragePath, C.StoredDate, C.VerifiedDate, C.[Status],
	       L.ArchiveLocationID, L.LocationName, L.LocationType, L.ContainerName
	from Archive_Item_Archived_File F left outer join
	     Archive_Item_Archived_File_Snapshot S on S.ArchivedFileID = F.ArchivedFileID left outer join
	     Archive_Item_Archived_File_Copy C on C.SnapshotID = S.SnapshotID left outer join
	     Archive_Location L on L.ArchiveLocationID = C.ArchiveLocationID
	where F.ItemID = @ItemID;

END;
GO
/****** Object:  StoredProcedure [dbo].[Archive_Get_Item_History_Public]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the bare necessity archiving history (files, snapshots, and stored copies) for a single item
-- for public consumption online
CREATE OR ALTER PROCEDURE [dbo].[Archive_Get_Item_History_Public]
	@ItemID int
AS
BEGIN

	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select F.ArchivedFileID, F.[FileName], F.FileExtension, S.FileSize, S.OriginalCreationDate, C.StoredDate, C.[Status], L.LocationName
	from Archive_Item_Archived_File F left outer join
	     Archive_Item_Archived_File_Snapshot S on S.ArchivedFileID = F.ArchivedFileID left outer join
	     Archive_Item_Archived_File_Copy C on C.SnapshotID = S.SnapshotID left outer join
	     Archive_Location L on L.ArchiveLocationID = C.ArchiveLocationID
	where F.ItemID = @ItemID;

END;
GO
/****** Object:  StoredProcedure [dbo].[Archive_Save_File]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Save information about an archived file, creating the file/snapshot/copy rows only as needed
CREATE OR ALTER PROCEDURE [dbo].[Archive_Save_File]
	@ItemID int,
	@FileName varchar(255),
	@FileSize bigint,
	@SHA256_Hash char(64),
	@OriginalCreationDate datetime,
	@StoragePath varchar(1000),
	@StoredDate datetime,
	@LocationName varchar(50),
	@MimeType varchar(100) = null,
	@EncodingDetails varchar(500) = null
AS
BEGIN

	declare @ArchivedFileID int;
	declare @SnapshotID int;
	declare @ArchiveLocationID smallint;
	declare @FileExtension varchar(20);

	-- Pull the extension off the file name rather than taking it as a separate argument,
	-- so it can never drift out of sync with the actual file name
	set @FileExtension = case
		when CHARINDEX('.', REVERSE(@FileName)) > 0
		then RIGHT(@FileName, CHARINDEX('.', REVERSE(@FileName)) - 1)
		else ''
	end;

	-- Find (or create) the stable file identity for this item/filename
	select @ArchivedFileID = ArchivedFileID
	from Archive_Item_Archived_File
	where ItemID = @ItemID and FileName = @FileName;

	if ( @ArchivedFileID is null )
	begin
		insert into Archive_Item_Archived_File ( ItemID, FileName, FileExtension )
		values ( @ItemID, @FileName, @FileExtension );

		set @ArchivedFileID = SCOPE_IDENTITY();
	end;

	-- Find (or create) a matching snapshot -- same size/hash/creation date means the same
	-- archiving event, even if this procedure gets called again for it (e.g. a retry)
	select @SnapshotID = SnapshotID
	from Archive_Item_Archived_File_Snapshot
	where ArchivedFileID = @ArchivedFileID
	  and FileSize = @FileSize
	  and SHA256_Hash = @SHA256_Hash
	  and OriginalCreationDate = @OriginalCreationDate;

	if ( @SnapshotID is null )
	begin
		insert into Archive_Item_Archived_File_Snapshot ( ArchivedFileID, FileSize, OriginalCreationDate, SHA256_Hash, SnapshotDate, MimeType, EncodingDetails )
		values ( @ArchivedFileID, @FileSize, @OriginalCreationDate, @SHA256_Hash, @StoredDate, @MimeType, @EncodingDetails );

		set @SnapshotID = SCOPE_IDENTITY();
	end;

	-- Resolve the storage location by name
	select @ArchiveLocationID = ArchiveLocationID
	from Archive_Location
	where LocationName = @LocationName;

	if ( @ArchiveLocationID is null )
	begin
		RAISERROR('Archive_Save_File: Unknown archive location ''%s''', 16, 1, @LocationName);
		return;
	end;

	-- Find (or create) the copy of this snapshot at this location
	if not exists ( select 1 from Archive_Item_Archived_File_Copy where SnapshotID = @SnapshotID and ArchiveLocationID = @ArchiveLocationID )
	begin
		insert into Archive_Item_Archived_File_Copy ( SnapshotID, ArchiveLocationID, StoragePath, StoredDate, Status )
		values ( @SnapshotID, @ArchiveLocationID, @StoragePath, @StoredDate, 'Stored' );
	end;

END;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Add_Description_Tag]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add a user tag
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Add_Description_Tag] 
	@UserID int,
	@TagID int,
	@ItemID int,
	@Description nvarchar(2000),
	@new_TagID int output
AS
begin

	set @new_TagID = -1;

	if ( ISNULL(@TagID, -1 ) > 0 )
	begin
		update mySobek_User_Description_Tags
		set Description_Tag = @Description, Date_Modified = GETDATE()
		where TagID=@TagID and UserID=@UserID
		
		set @new_TagID = @TagID;	
	end
	else
	begin
		-- Can have up to five comments on a single item 
		if (( select COUNT(*) from mySobek_User_Description_Tags where UserID=@UserID and ItemID=@ItemID ) < 5)
		begin
			insert into mySobek_User_Description_Tags( UserID, ItemID, Description_Tag, Date_Modified )
			values ( @UserID, @ItemID, @Description, GETDATE() )	
			
			set @new_TagID = @@identity
		end
	end
end
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Add_Item_To_User_Folder]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add an item to the user's folder
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Add_Item_To_User_Folder]
	@userid int,
	@foldername varchar(255),
	@bibid varchar(10),
	@vid varchar(5),
	@itemorder int,
	@usernotes nvarchar(2000)
AS
begin

	-- Is there a match for this bib id /vid?
	if (( select COUNT(*) from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = @bibid and I.VID = @vid ) = 1 )
	begin
		-- Get the item id
		declare @itemid int
		select @itemid = ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = @bibid and I.VID = @vid
	
		-- First, see if this user already has a folder named this
		declare @userfolderid int
		if (( select count(*) from mySobek_User_Folder where UserID=@userid and FolderName=@foldername) > 0 )
		begin
			-- Get the existing folder id
			select @userfolderid = UserFolderID from mySobek_User_Folder where UserID=@userid and FolderName=@foldername
		end
		else
		begin
			-- Add this folder
			insert into mySobek_User_Folder ( UserID, FolderName, isPublic )
			values ( @userid, @foldername, 'false' )

			-- Get the new id
			select @userfolderid = @@identity
		end	

		-- Now, see if the item is already linked to the folder
		if (( select count(*) from mySobek_User_Item where ItemID=@itemid and UserFolderID=@userfolderid ) > 0 )
		begin
			-- Just update the existing link then
			update mySobek_User_Item
			set ItemOrder = @itemorder, UserNotes=@usernotes
			where UserFolderID = @userfolderid and ItemID=@itemid
		end
		else
		begin
			-- Add a new link then
			insert into mySobek_User_Item( UserFolderID, ItemID, ItemOrder, UserNotes, DateAdded )
			values ( @userfolderid, @itemid, @itemorder, @usernotes, getdate() )
		end
	end
end
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Add_User_Aggregations_Link]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Procedure to add links between a user and item aggregations
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Add_User_Aggregations_Link]
	@UserID int,
	@AggregationCode1 varchar(20),
	@canSelect1 bit,
	@canEditMetadata1 bit,
	@canEditBehaviors1 bit,
	@canPerformQc1 bit,
	@canUploadFiles1 bit,
	@canChangeVisibility1 bit,
	@canDelete1 bit,
	@isCurator1 bit,
	@onHomePage1 bit,
	@isAdmin1 bit,
	@AggregationCode2 varchar(20),
	@canSelect2 bit,
	@canEditMetadata2 bit,
	@canEditBehaviors2 bit,
	@canPerformQc2 bit,
	@canUploadFiles2 bit,
	@canChangeVisibility2 bit,
	@canDelete2 bit,
	@isCurator2 bit,
	@onHomePage2 bit,
	@isAdmin2 bit,
	@AggregationCode3 varchar(20),
	@canSelect3 bit,
	@canEditMetadata3 bit,
	@canEditBehaviors3 bit,
	@canPerformQc3 bit,
	@canUploadFiles3 bit,
	@canChangeVisibility3 bit,
	@canDelete3 bit,
	@isCurator3 bit,
	@onHomePage3 bit,
	@isAdmin3 bit
AS
BEGIN
	
	-- Add the first aggregation
	if (( len(@AggregationCode1) > 0 ) and ((select count(*) from SobekCM_Item_Aggregation where Code=@AggregationCode1 ) = 1 ))
	begin
		-- Get the id for this one
		declare @Aggregation1_Id int;
		select @Aggregation1_Id = AggregationID from SobekCM_Item_Aggregation where Code=@AggregationCode1;

		-- Is this user already linked to the aggreagtion?
		if (( select count(*) from mySobek_User_Edit_Aggregation where UserID=@UserID and AggregationID=@Aggregation1_Id ) = 0 )
		begin
			-- Add this one
			insert into mySobek_User_Edit_Aggregation ( UserID, AggregationID, CanSelect, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete, IsCurator, OnHomePage, IsAdmin, CanEditItems )
			values ( @UserID, @Aggregation1_Id, @canSelect1, @canEditMetadata1, @canEditBehaviors1, @canPerformQc1, @canUploadFiles1, @canChangeVisibility1, @canDelete1, @isCurator1, @onHomePage1, @isAdmin1, @canEditMetadata1 );
		end
		else
		begin
			-- Update the existing link
			update mySobek_User_Edit_Aggregation
			set CanSelect=@canSelect1, CanEditMetadata=@canEditMetadata1, CanEditBehaviors=@canEditBehaviors1, CanPerformQc=@canPerformQc1, CanUploadFiles=@canUploadFiles1, CanChangeVisibility=@canChangeVisibility1, CanDelete=@canDelete1, IsCurator=@isCurator1, OnHomePage=@onHomePage1, IsAdmin=@isAdmin1, CanEditItems=@canEditMetadata1
			where UserID=@UserID and AggregationID=@Aggregation1_Id;
		end;		
	end;
	
	-- Add the second aggregation
	if (( len(@AggregationCode2) > 0 ) and ((select count(*) from SobekCM_Item_Aggregation where Code=@AggregationCode2 ) = 1 ))
	begin
		-- Get the id for this one
		declare @Aggregation2_Id int;
		select @Aggregation2_Id = AggregationID from SobekCM_Item_Aggregation where Code=@AggregationCode2;

		-- Is this user already linked to the aggreagtion?
		if (( select count(*) from mySobek_User_Edit_Aggregation where UserID=@UserID and AggregationID=@Aggregation2_Id ) = 0 )
		begin
			-- Add this one
			insert into mySobek_User_Edit_Aggregation ( UserID, AggregationID, CanSelect, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete, IsCurator, OnHomePage, IsAdmin, CanEditItems )
			values ( @UserID, @Aggregation2_Id, @canSelect2, @canEditMetadata2, @canEditBehaviors2, @canPerformQc2, @canUploadFiles2, @canChangeVisibility2, @canDelete2, @isCurator2, @onHomePage2, @isAdmin2, @canEditMetadata2 );
		end
		else
		begin
			-- Update the existing link
			update mySobek_User_Edit_Aggregation
			set CanSelect=@canSelect2, CanEditMetadata=@canEditMetadata2, CanEditBehaviors=@canEditBehaviors2, CanPerformQc=@canPerformQc2, CanUploadFiles=@canUploadFiles2, CanChangeVisibility=@canChangeVisibility2, CanDelete=@canDelete2, IsCurator=@isCurator2, OnHomePage=@onHomePage2, IsAdmin=@isAdmin2, CanEditItems=@canEditMetadata2
			where UserID=@UserID and AggregationID=@Aggregation2_Id;
		end;	
	end;

	-- Add the third aggregation
	if (( len(@AggregationCode3) > 0 ) and ((select count(*) from SobekCM_Item_Aggregation where Code=@AggregationCode3 ) = 1 ))
	begin
		-- Get the id for this one
		declare @Aggregation3_Id int;
		select @Aggregation3_Id = AggregationID from SobekCM_Item_Aggregation where Code=@AggregationCode3;

		-- Is this user already linked to the aggreagtion?
		if (( select count(*) from mySobek_User_Edit_Aggregation where UserID=@UserID and AggregationID=@Aggregation3_Id ) = 0 )
		begin
			-- Add this one
			insert into mySobek_User_Edit_Aggregation ( UserID, AggregationID, CanSelect, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete, IsCurator, OnHomePage, IsAdmin, CanEditItems )
			values ( @UserID, @Aggregation3_Id, @canSelect3, @canEditMetadata3, @canEditBehaviors3, @canPerformQc3, @canUploadFiles3, @canChangeVisibility3, @canDelete3, @isCurator3, @onHomePage3, @isAdmin3, @canEditMetadata3 );
		end
		else
		begin
			-- Update the existing link
			update mySobek_User_Edit_Aggregation
			set CanSelect=@canSelect3, CanEditMetadata=@canEditMetadata3, CanEditBehaviors=@canEditBehaviors3, CanPerformQc=@canPerformQc3, CanUploadFiles=@canUploadFiles3, CanChangeVisibility=@canChangeVisibility3, CanDelete=@canDelete3, IsCurator=@isCurator3, OnHomePage=@onHomePage3, IsAdmin=@isAdmin3, CanEditItems=@canEditMetadata3
			where UserID=@UserID and AggregationID=@Aggregation3_Id;
		end;	
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Add_User_DefaultMetadata_Link]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/****** Object:  StoredProcedure [dbo].[[mySobek_Add_User_DefaultMetadata_Link]]    Script Date: 12/20/2013 05:43:35 ******/
-- Add a link between a user and default metadata 
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Add_User_DefaultMetadata_Link]
	@userid int,
	@metadata_default varchar(20),
	@metadata2 varchar(20),
	@metadata3 varchar(20),
	@metadata4 varchar(20),
	@metadata5 varchar(20)
AS
begin

	-- Add the default default metadata
	if (( len(@metadata_default) > 0 ) and ( (select count(*) from mySobek_DefaultMetadata where MetadataCode = @metadata_default ) = 1 ))
	begin
		-- Clear any previous default
		update mySobek_User_DefaultMetadata_Link set CurrentlySelected='false' where UserID = @userid;

		-- Get the id for this one
		declare @metadata_default_id int;
		select @metadata_default_id = DefaultMetadataID from mySobek_DefaultMetadata where MetadataCode=@metadata_default;

		-- Add this one as a default
		insert into mySobek_User_DefaultMetadata_Link ( UserID, DefaultMetadataID, CurrentlySelected )
		values ( @userid, @metadata_default_id, 'true' );
	end;

	-- Add the second default metadata
	if (( len(@metadata2) > 0 ) and ((select count(*) from mySobek_DefaultMetadata where MetadataCode = @metadata2 ) = 1 ))
	begin
		-- Get the id for this one
		declare @metadata2_id int;
		select @metadata2_id = DefaultMetadataID from mySobek_DefaultMetadata where MetadataCode=@metadata2;

		-- Add this one
		insert into mySobek_User_DefaultMetadata_Link ( UserID, DefaultMetadataID, CurrentlySelected )
		values ( @userid, @metadata2_id, 'false' );
	end;

	-- Add the third default metadata
	if (( len(@metadata3) > 0 ) and ((select count(*) from mySobek_DefaultMetadata where MetadataCode = @metadata3 ) = 1 ))
	begin
		-- Get the id for this one
		declare @metadata3_id int;
		select @metadata3_id = DefaultMetadataID from mySobek_DefaultMetadata where MetadataCode=@metadata3;

		-- Add this one
		insert into mySobek_User_DefaultMetadata_Link ( UserID, DefaultMetadataID, CurrentlySelected )
		values ( @userid, @metadata3_id, 'false' );
	end;

	-- Add the fourth default metadata
	if (( len(@metadata4) > 0 ) and ((select count(*) from mySobek_DefaultMetadata where MetadataCode = @metadata4 ) = 1 ))
	begin
		-- Get the id for this one
		declare @metadata4_id int;
		select @metadata4_id = DefaultMetadataID from mySobek_DefaultMetadata where MetadataCode=@metadata4;

		-- Add this one
		insert into mySobek_User_DefaultMetadata_Link ( UserID, DefaultMetadataID, CurrentlySelected )
		values ( @userid, @metadata4_id, 'false' );
	end;

	-- Add the fifth default metadata
	if (( len(@metadata5) > 0 ) and ((select count(*) from mySobek_DefaultMetadata where MetadataCode = @metadata5 ) = 1 ))
	begin
		-- Get the id for this one
		declare @metadata5_id int;
		select @metadata5_id = DefaultMetadataID from mySobek_DefaultMetadata where MetadataCode=@metadata5;

		-- Add this one
		insert into mySobek_User_DefaultMetadata_Link ( UserID, DefaultMetadataID, CurrentlySelected )
		values ( @userid, @metadata5_id, 'false' );
	end;
end;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Add_User_Group_Aggregations_Link]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Procedure to add links between a user group and item aggregations
-- NOTE: The OnHomePage values are NOT used, but are included to keep this
--       signature the same as the single user aggregation link procedure
--       reducing overhead for maintenance
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Add_User_Group_Aggregations_Link]
	@UserGroupID int,
	@AggregationCode1 varchar(20),
	@canSelect1 bit,
	@canEditMetadata1 bit,
	@canEditBehaviors1 bit,
	@canPerformQc1 bit,
	@canUploadFiles1 bit,
	@canChangeVisibility1 bit,
	@canDelete1 bit,
	@isCurator1 bit,
	@onHomePage1 bit,
	@isAdmin1 bit,
	@AggregationCode2 varchar(20),
	@canSelect2 bit,
	@canEditMetadata2 bit,
	@canEditBehaviors2 bit,
	@canPerformQc2 bit,
	@canUploadFiles2 bit,
	@canChangeVisibility2 bit,
	@canDelete2 bit,
	@isCurator2 bit,
	@onHomePage2 bit,
	@isAdmin2 bit,
	@AggregationCode3 varchar(20),
	@canSelect3 bit,
	@canEditMetadata3 bit,
	@canEditBehaviors3 bit,
	@canPerformQc3 bit,
	@canUploadFiles3 bit,
	@canChangeVisibility3 bit,
	@canDelete3 bit,
	@isCurator3 bit,
	@onHomePage3 bit,
	@isAdmin3 bit
AS
BEGIN
	
	-- Add the first aggregation
	if (( len(@AggregationCode1) > 0 ) and ((select count(*) from SobekCM_Item_Aggregation where Code=@AggregationCode1 ) = 1 ))
	begin
		-- Get the id for this one
		declare @Aggregation1_Id int;
		select @Aggregation1_Id = AggregationID from SobekCM_Item_Aggregation where Code=@AggregationCode1;

		-- Add this one
		insert into mySobek_User_Group_Edit_Aggregation ( UserGroupID, AggregationID, CanSelect, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete, IsCurator, CanEditItems, IsAdmin )
		values ( @UserGroupID, @Aggregation1_Id, @canSelect1, @canEditMetadata1, @canEditBehaviors1, @canPerformQc1, @canUploadFiles1, @canChangeVisibility1, @canDelete1, @isCurator1, @canEditMetadata1, @isAdmin1 );
	end;
	
	-- Add the second aggregation
	if (( len(@AggregationCode2) > 0 ) and ((select count(*) from SobekCM_Item_Aggregation where Code=@AggregationCode2 ) = 1 ))
	begin
		-- Get the id for this one
		declare @Aggregation2_Id int;
		select @Aggregation2_Id = AggregationID from SobekCM_Item_Aggregation where Code=@AggregationCode2;

		-- Add this one
		insert into mySobek_User_Group_Edit_Aggregation ( UserGroupID, AggregationID, CanSelect, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete, IsCurator, CanEditItems, IsAdmin )
		values ( @UserGroupID, @Aggregation2_Id, @canSelect2, @canEditMetadata2, @canEditBehaviors2, @canPerformQc2, @canUploadFiles2, @canChangeVisibility2, @canDelete2, @isCurator2, @canEditMetadata2, @isAdmin2 );
	end;
	
	-- Add the third aggregation
	if (( len(@AggregationCode3) > 0 ) and ((select count(*) from SobekCM_Item_Aggregation where Code=@AggregationCode3 ) = 1 ))
	begin
		-- Get the id for this one
		declare @Aggregation3_Id int;
		select @Aggregation3_Id = AggregationID from SobekCM_Item_Aggregation where Code=@AggregationCode3;

		-- Add this one
		insert into mySobek_User_Group_Edit_Aggregation ( UserGroupID, AggregationID, CanSelect, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete, IsCurator, CanEditItems, IsAdmin )
		values ( @UserGroupID, @Aggregation3_Id, @canSelect3, @canEditMetadata3, @canEditBehaviors3, @canPerformQc3, @canUploadFiles3, @canChangeVisibility3, @canDelete3, @isCurator3, @canEditMetadata3, @isAdmin3 );
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Add_User_Group_Metadata_Link]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add a link between a user and a set of default metadata 
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Add_User_Group_Metadata_Link]
	@usergroupid int,
	@metadata1 varchar(20),
	@metadata2 varchar(20),
	@metadata3 varchar(20),
	@metadata4 varchar(20),
	@metadata5 varchar(20)
AS
begin

	-- Add the first default metadata
	if (( len(@metadata1) > 0 ) and ( (select count(*) from mySobek_DefaultMetadata where MetadataCode = @metadata1 ) = 1 ))
	begin
		-- Get the id for this one
		declare @metadata1_id int;
		select @metadata1_id = DefaultMetadataID from mySobek_DefaultMetadata where MetadataCode=@metadata1;

		-- Add this one as a default
		insert into mySobek_User_Group_DefaultMetadata_Link ( UserGroupID, DefaultMetadataID )
		values ( @usergroupid, @metadata1_id );
	end;

	-- Add the second default metadata
	if (( len(@metadata2) > 0 ) and ( (select count(*) from mySobek_DefaultMetadata where MetadataCode = @metadata2 ) = 1 ))
	begin
		-- Get the id for this one
		declare @metadata2_id int;
		select @metadata2_id = DefaultMetadataID from mySobek_DefaultMetadata where MetadataCode=@metadata2;

		-- Add this one as a default
		insert into mySobek_User_Group_DefaultMetadata_Link ( UserGroupID, DefaultMetadataID )
		values ( @usergroupid, @metadata2_id );
	end;

	-- Add the third detault metadata
	if (( len(@metadata3) > 0 ) and ( (select count(*) from mySobek_DefaultMetadata where MetadataCode = @metadata3 ) = 1 ))
	begin
		-- Get the id for this one
		declare @metadata3_id int;
		select @metadata3_id = DefaultMetadataID from mySobek_DefaultMetadata where MetadataCode=@metadata3;

		-- Add this one as a default
		insert into mySobek_User_Group_DefaultMetadata_Link ( UserGroupID, DefaultMetadataID )
		values ( @usergroupid, @metadata3_id );
	end;

	-- Add the fourth default metadata
	if (( len(@metadata4) > 0 ) and ( (select count(*) from mySobek_DefaultMetadata where MetadataCode = @metadata4 ) = 1 ))
	begin
		-- Get the id for this one
		declare @metadata4_id int;
		select @metadata4_id = DefaultMetadataID from mySobek_DefaultMetadata where MetadataCode=@metadata4;

		-- Add this one as a default
		insert into mySobek_User_Group_DefaultMetadata_Link ( UserGroupID, DefaultMetadataID )
		values ( @usergroupid, @metadata4_id );
	end;

	-- Add the fifth default metadata
	if (( len(@metadata5) > 0 ) and ( (select count(*) from mySobek_DefaultMetadata where MetadataCode = @metadata5 ) = 1 ))
	begin
		-- Get the id for this one
		declare @metadata5_id int;
		select @metadata5_id = DefaultMetadataID from mySobek_DefaultMetadata where MetadataCode=@metadata5;

		-- Add this one as a default
		insert into mySobek_User_Group_DefaultMetadata_Link ( UserGroupID, DefaultMetadataID )
		values ( @usergroupid, @metadata5_id );
	end;
end;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Add_User_Group_Templates_Link]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add a link between a user and a template 
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Add_User_Group_Templates_Link]
	@usergroupid int,
	@template1 varchar(20),
	@template2 varchar(20),
	@template3 varchar(20),
	@template4 varchar(20),
	@template5 varchar(20)
AS
begin

	-- Add the default template
	if (( len(@template1) > 0 ) and ( (select count(*) from mySobek_Template where TemplateCode = @template1 ) = 1 ))
	begin
		-- Get the id for this one
		declare @template1_id int
		select @template1_id = TemplateID from mySobek_Template where TemplateCode=@template1

		-- Add this one as a default
		insert into mySobek_User_Group_Template_Link ( UserGroupID, TemplateID )
		values ( @usergroupid, @template1_id )
	end

	-- Add the second template
	if (( len(@template2) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = @template2 ) = 1 ))
	begin
		-- Get the id for this one
		declare @template2_id int
		select @template2_id = TemplateID from mySobek_Template where TemplateCode=@template2

		-- Add this one
		insert into mySobek_User_Group_Template_Link ( UserGroupID, TemplateID )
		values ( @usergroupid, @template2_id )
	end

	-- Add the third template
	if (( len(@template3) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = @template3 ) = 1 ))
	begin
		-- Get the id for this one
		declare @template3_id int
		select @template3_id = TemplateID from mySobek_Template where TemplateCode=@template3

		-- Add this one
		insert into mySobek_User_Group_Template_Link ( UserGroupID, TemplateID )
		values ( @usergroupid, @template3_id )
	end

	-- Add the fourth template
	if (( len(@template4) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = @template4 ) = 1 ))
	begin
		-- Get the id for this one
		declare @template4_id int
		select @template4_id = TemplateID from mySobek_Template where TemplateCode=@template4

		-- Add this one
		insert into mySobek_User_Group_Template_Link ( UserGroupID, TemplateID )
		values ( @usergroupid, @template4_id )
	end

	-- Add the fifth template
	if (( len(@template5) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = @template5 ) = 1 ))
	begin
		-- Get the id for this one
		declare @template5_id int
		select @template5_id = TemplateID from mySobek_Template where TemplateCode=@template5

		-- Add this one
		insert into mySobek_User_Group_Template_Link ( UserGroupID, TemplateID )
		values ( @usergroupid, @template5_id )
	end
end
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Add_User_Setting]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



CREATE OR ALTER PROCEDURE [dbo].[mySobek_Add_User_Setting]
	@userid int,
	@setting_key nvarchar(255),
	@setting_value nvarchar(max)
AS
begin

	-- Does this already exist?
	if ( (select count(*) from mySobek_User_Settings where UserID=@userid and Setting_Key=@setting_key ) > 0 )
	begin
		-- If this clears the value, remove the key
		if ( len(@setting_value) > 0 ) 
		begin
			delete from mySobek_User_Settings where UserID=@userid and Setting_Key=@setting_key;
		end
		else
		begin
			update mySobek_User_Settings 
			set Setting_Value=@setting_value
			where UserID=@userid and Setting_Key=@setting_key;
		end;
	end
	else
	begin

		insert into mySobek_User_Settings ( UserID, Setting_Key, Setting_Value )
		values ( @userid, @setting_key, @setting_value );

	end;
	
end;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Add_User_Templates_Link]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add a link between a user and a template 
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Add_User_Templates_Link]
	@userid int,
	@template_default varchar(20),
	@template2 varchar(20),
	@template3 varchar(20),
	@template4 varchar(20),
	@template5 varchar(20)
AS
begin

	-- Add the default template
	if (( len(@template_default) > 0 ) and ( (select count(*) from mySobek_Template where TemplateCode = @template_default ) = 1 ))
	begin
		-- Clear any previous default
		update mySobek_User_Template_Link set DefaultTemplate='false' where UserID = @userid

		-- Get the id for this one
		declare @template_default_id int
		select @template_default_id = TemplateID from mySobek_Template where TemplateCode=@template_default

		-- Add this one as a default
		insert into mySobek_User_Template_Link ( UserID, TemplateID, DefaultTemplate )
		values ( @userid, @template_default_id, 'true' )
	end

	-- Add the second template
	if (( len(@template2) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = @template2 ) = 1 ))
	begin
		-- Get the id for this one
		declare @template2_id int
		select @template2_id = TemplateID from mySobek_Template where TemplateCode=@template2

		-- Add this one
		insert into mySobek_User_Template_Link ( UserID, TemplateID, DefaultTemplate )
		values ( @userid, @template2_id, 'false' )
	end

	-- Add the third template
	if (( len(@template3) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = @template3 ) = 1 ))
	begin
		-- Get the id for this one
		declare @template3_id int
		select @template3_id = TemplateID from mySobek_Template where TemplateCode=@template3

		-- Add this one
		insert into mySobek_User_Template_Link ( UserID, TemplateID, DefaultTemplate )
		values ( @userid, @template3_id, 'false' )
	end

	-- Add the fourth template
	if (( len(@template4) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = @template4 ) = 1 ))
	begin
		-- Get the id for this one
		declare @template4_id int
		select @template4_id = TemplateID from mySobek_Template where TemplateCode=@template4

		-- Add this one
		insert into mySobek_User_Template_Link ( UserID, TemplateID, DefaultTemplate )
		values ( @userid, @template4_id, 'false' )
	end

	-- Add the fifth template
	if (( len(@template5) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = @template5 ) = 1 ))
	begin
		-- Get the id for this one
		declare @template5_id int
		select @template5_id = TemplateID from mySobek_Template where TemplateCode=@template5

		-- Add this one
		insert into mySobek_User_Template_Link ( UserID, TemplateID, DefaultTemplate )
		values ( @userid, @template5_id, 'false' )
	end
end
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Clear_User_Settings]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE OR ALTER PROCEDURE [dbo].[mySobek_Clear_User_Settings]
	@userid int
AS 
begin

	delete from mySobek_User_Settings
	where UserID=@userid;

end;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Delete_DefaultMetadata]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Procedure to delete a default metadata set
-- were linked to this web skin
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Delete_DefaultMetadata]
	@MetadataCode varchar(20)
AS
BEGIN

	if ( @MetadataCode != 'NONE' )
	begin
		delete from mySobek_DefaultMetadata where MetadataCode=@MetadataCode;
	end;

END;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Delete_Description_Tag]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Delete a user's tag
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Delete_Description_Tag] 
	@TagID int
AS
begin
	delete from mySobek_User_Description_Tags where TagID=@TagID
end
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Delete_Item_From_All_User_Folders]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Delete an item from the user's folder
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Delete_Item_From_All_User_Folders]
	@userid int,
	@bibid varchar(10),
	@vid varchar(5)
AS
begin
	
		-- Is there a match for this bib id /vid?
	if (( select COUNT(*) from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = @bibid and I.VID = @vid ) = 1 )
	begin
		-- Get the item id
		declare @itemid int
		select @itemid = ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = @bibid and I.VID = @vid
	
		-- Delete this item from this users folder
		delete from mySobek_User_Item
		where ( ItemID=@itemid ) 
		  and exists (	select FolderName 
						from mySobek_User_Folder F 
						where F.UserID=@userid 
						  and F.UserFolderID=mySobek_User_Item.UserFolderID
						  and FolderName != 'Submitted Items' )
	end
end
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Delete_Item_From_User_Folder]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Delete an item from the user's folder
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Delete_Item_From_User_Folder]
	@userid int,
	@foldername varchar(255),
	@bibid varchar(10),
	@vid varchar(5)
AS
begin
	
	-- Is there a match for this bib id /vid?
	if (( select COUNT(*) from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = @bibid and I.VID = @vid ) = 1 )
	begin
		-- Get the item id
		declare @itemid int
		select @itemid = ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = @bibid and I.VID = @vid
	
		-- First, get the user folder id for this
		if (( select count(*) from mySobek_User_Folder where UserID=@userid and FolderName=@foldername) > 0 )
		begin
			-- Get the existing folder id
			declare @userfolderid int
			select @userfolderid = UserFolderID from mySobek_User_Folder where UserID=@userid and FolderName=@foldername

			-- Now, delete this item
			delete from mySobek_User_Item where UserFolderID=@userfolderid and ItemID=@itemid
		end
	end
end
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Delete_User]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Fully removes an existing user
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Delete_User] 
	@username varchar(20)
as
begin transaction

	if ( exists ( select 1 from mySobek_User where UserName=@username ))
	begin

		-- Get the 	user id
		declare @userid int;
		set @userid = ( select UserID from mySobek_User where Username=@username);

		-- Delete from all the satellite tables
		delete from mySobek_User_Bib_Link where UserID=@userid;
		delete from mySobek_User_DefaultMetadata_Link where UserID=@userid;
		delete from mySobek_User_Description_Tags where UserID=@userid;
		delete from mySobek_User_Edit_Aggregation where UserID=@userid;
		delete from mySobek_User_Editable_Link where UserID=@userid;

		delete from mySobek_User_Item_Link where UserID=@userid;
		delete from mySobek_User_Item_Permissions where UserID=@userid;
		delete from mySobek_User_Search where UserID=@userid;
		delete from mySobek_User_Settings where UserID=@userid;
		delete from mySobek_User_Template_Link where UserID=@userid;
		delete from mySobek_User_Group_Link where UserID=@userid;

		-- Delete the folder
		delete from mySobek_User_Item where UserFolderID in ( select UserFolderID from mySobek_User_Folder where UserID=@userid);
		delete from mySobek_User_Folder where UserID=@userid;

		-- Delete from main user table
		delete from mySobek_User where UserID=@userid;

	end;

commit transaction
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Delete_User_Folder]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Delete a user's folder
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Delete_User_Folder]
	@userfolderid int,
	@userid int
AS
begin transaction
	
	-- Only continue if the folder exists and is tagged to this user
	if ((select count(*) from mySobek_User_Folder where userfolderid=@userfolderid and userid=@userid) > 0 )
	begin
		
		-- Only continue if there are no subfolders
		if (( select count(*) from mySobek_User_Folder where ParentFolderID = @userfolderid ) <= 0 )
		begin
			DELETE FROM mySobek_User_Folder
			where UserID=@userid and UserFolderID=@userfolderid
		end
	end
commit transaction
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Delete_User_Group]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[mySobek_Delete_User_Group]
	@usergroupid int,
	@message int output
AS
begin transaction

	if ( exists ( select 1 from mySobek_User_Group_Link where UserGroupID=@usergroupid ))
	begin
		set @message = -1;
	end
	else if ( exists ( select 1 from mySobek_User_Group where UserGroupID=@usergroupid and isSpecialGroup = 'true' ))
	begin
		set @message = -2;
	end
	else
	begin

		delete from mySobek_User_Group_DefaultMetadata_Link where UserGroupID=@usergroupid;
		delete from mySobek_User_Group_Edit_Aggregation where UserGroupID=@usergroupid;
		delete from mySobek_User_Group_Item_Permissions where UserGroupID=@usergroupid;
		delete from mySobek_User_Group_Editable_Link where UserGroupID=@usergroupid;
		delete from mySobek_User_Group_Template_Link where UserGroupID=@usergroupid;
		delete from mySobek_User_Group where UserGroupID = @usergroupid;

		set @message = 1;
	end;

commit transaction;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Delete_User_Search]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Delete a saved search
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Delete_User_Search]
	@usersearchid int
AS
BEGIN
	delete from mySobek_User_Search
	where UserSearchID=@usersearchid
END
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Edit_User_Folder]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Edit a user's folder information
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Edit_User_Folder]
	@userfolderid int,
	@userid int,
	@parentfolderid int,
	@foldername nvarchar(255),
	@is_public bit,
	@description nvarchar(4000),
	@new_folder_id int out
AS
begin transaction

	-- Does this reference an existing folder?
	if ( @userfolderid > 0 )
	begin
		-- Update the existing folder
		update mySobek_User_Folder 
		set FolderName=@foldername, isPublic=@is_public, FolderDescription=@description
		where UserFolderID=@userfolderid and UserID=@userid;

		-- Return the old folder id
		set @new_folder_id = @userfolderid;
	end
	else
	begin
		-- Ensure a folder of the same name does not exist
		if (( select count(*) from mySobek_User_Folder where UserID=@userid and FolderName=@foldername ) > 0 )
		begin
			-- update this existing folder
			update mySobek_User_Folder 
			set FolderName=@foldername, isPublic=@is_public, FolderDescription=@description
			where FolderName=@foldername and UserID=@userid;

			-- Get the primary key
			set @new_folder_id = ( select UserFolderID from mySobek_User_Folder where FolderName=@foldername and UserID=@userid);
		end
		else
		begin		
			-- Add this as a new folder
			if ( @parentfolderid < 0 )
			begin
				-- Insert this as a new folder, without a parent
				insert into mySobek_User_Folder( UserID, FolderName, isPublic, FolderDescription )
				values ( @userid, @foldername, @is_public, @description );
			end
			else
			begin
				-- Insert this as a new folder, with a parent
				insert into mySobek_User_Folder( UserID, FolderName, isPublic, FolderDescription, ParentFolderID )
				values ( @userid, @foldername, @is_public, @description, @parentfolderid );
			end;	

			-- Return the new folder id
			set @new_folder_id = @@identity;
		end;
	end;
commit transaction;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Get_All_Template_DefaultMetadatas]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the list of all templates and default metadata sets 
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Get_All_Template_DefaultMetadatas]
AS
BEGIN
	
	select MetadataCode, MetadataName, [Description], UserID
	from mySobek_DefaultMetadata
	order by MetadataCode;

	select TemplateCode, TemplateName, [Description]
	from mySobek_Template
	order by TemplateCode;

END;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Get_All_User_Groups]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[mySobek_Get_All_User_Groups] AS
BEGIN

	with linked_users_cte ( UserGroupID, UserCount ) AS
	(
		select UserGroupID, count(*)
		from mySobek_User_Group_Link
		group by UserGroupID
	)
	select G.UserGroupID, GroupName, GroupDescription, coalesce(UserCount,0) as UserCount, IsSpecialGroup
	from mySobek_User_Group G 
	     left outer join linked_users_cte U on U.UserGroupID=G.UserGroupID
	order by IsSpecialGroup, GroupName;

END
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Get_All_User_Settings_Like]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Procedure gets settings across all the users that are like the key start
--
-- Since this uses like, you can pass in a string like 'TEI.%' and that will return
-- all the values that have a setting key that STARTS with 'TEI.'
--
-- If @value is NULL, then all settings that match are returned.  If a value is
-- provided for @value, then only the settings that match the key search and 
-- have the same value in the database as @value are returned.  This is particularly
-- useful for boolean settings, where you only want to the see the settings set to 'true'
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Get_All_User_Settings_Like]
	@keyStart nvarchar(255),
	@value nvarchar(max)
AS
begin

	-- User can request settings that are only one value (useful for boolean settings really)
	if ( @value is null )
	begin
	
		-- Just return all that are like the setting key
		select U.UserName, U.UserID, coalesce(U.FirstName,'') as FirstName, coalesce(U.LastName,'') as LastName, S.Setting_Key, S.Setting_Value
		from mySobek_User U, mySobek_User_Settings S
		where ( U.UserID = S.UserID )
		  and ( Setting_Key like @keyStart )
		  and ( U.isActive = 'true' );

	end
	else
	begin
		
		-- Return information on settings like the setting key and set to @value then
		select U.UserName, U.UserID, coalesce(U.FirstName,'') as FirstName, coalesce(U.LastName,'') as LastName, S.Setting_Key, S.Setting_Value
		from mySobek_User U, mySobek_User_Settings S
		where ( U.UserID = S.UserID )
		  and ( Setting_Key like @keyStart )
		  and ( U.isActive = 'true' )
		  and ( Setting_Value = @value );
	end;

end;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Get_All_Users]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Get_All_Users] AS
BEGIN
	
	-- Get the list of users and pending user requests
	with pending_cte as 
	(
		select UserID, count(*) as PendingRequests
		from mySobek_User_Request
		where Pending='true'
		group by UserID
	)
	select U.UserID, LastName + ', ' + FirstName AS [Full_Name], UserName, EmailAddress, coalesce(R.PendingRequests,0) as PendingRequests
	from mySobek_User U left join 
		 pending_cte R on U.UserID = R.UserID 
	order by Full_Name;
END;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Get_Folder_Information]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get all the information about a folder, by folder id
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Get_Folder_Information]
	@folderid int
AS
BEGIN
	select UserFolderID, FolderName, isPublic, FolderDescription, U.UserID, FirstName, LastName, NickName, EmailAddress
	from mySobek_User_Folder F, mySobek_User U
	where ( UserFolderID=@folderid )
	  and ( U.UserID = F.UserID )
END
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Get_Folder_Search_Information]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get overall information about folders and searches for this user
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Get_Folder_Search_Information]
	@userid int
AS
BEGIN
	-- Return the names of all the folders first
	select F.UserFolderID, ParentFolderID=isnull(F.ParentFolderID,-1), F.FolderName, F.isPublic, Item_Count=(select count(*) from mySobek_User_Item I where I.UserFolderID=F.UserFolderID )
	from mySobek_User_Folder F
	where UserID=@userid

	-- Return the number of searches
	select Search_Count=count(*)
	from mySobek_User_Search
	where UserID=@userid
END
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Get_User_By_External_Login]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE OR ALTER PROCEDURE [dbo].[mySobek_Get_User_By_External_Login]
	@provider_code nvarchar(50),
	@external_subject_id nvarchar(450)
AS
BEGIN  

	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Look for the user by Shibboleth ID.  Does one exist?
	if (( select COUNT(*) from mySobek_User where ExternalProviderCode=@provider_code and ExternalSubjectId=@external_subject_id and isActive = 'true' ) = 1 )
	begin
		-- Get the userid for this user
		declare @userid int;
		select @userid = UserID from mySobek_User where ExternalProviderCode=@provider_code and ExternalSubjectId=@external_subject_id and isActive = 'true';  
  
		-- Stored procedure used to return standard data across all user fetch stored procedures
		exec mySobek_Get_User_By_UserID @userid; 
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Get_User_By_ShibbID]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[mySobek_Get_User_By_ShibbID]
	@shibbid char(8)
AS
BEGIN  

	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Look for the user by Shibboleth ID.  Does one exist?
	if (( select COUNT(*) from mySobek_User where ShibbID=@shibbid and isActive = 'true' ) = 1 )
	begin
		-- Get the userid for this user
		declare @userid int;
		select @userid = UserID from mySobek_User where ShibbID=@shibbid and isActive = 'true';  
  
		-- Stored procedure used to return standard data across all user fetch stored procedures
		exec mySobek_Get_User_By_UserID @userid; 
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Get_User_By_UserID]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE OR ALTER PROCEDURE [dbo].[mySobek_Get_User_By_UserID]
	@userid int
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
	  AuthenticationSource
	from mySobek_User U
	where ( UserID = @userid ) and ( isActive = 'true' );

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
/****** Object:  StoredProcedure [dbo].[mySobek_Get_User_By_UserName]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Gets all the user information by the username.  Hashed password will be compared in 
-- the database routines (and possibly flagged to be replaced with new hash)
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Get_User_By_UserName]
	@username varchar(100)
AS
BEGIN

	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Look for the current user by username and hashed password.  Does one exist?
	if (( select COUNT(*) from mySobek_User where UserName=@username and isActive = 'true' ) = 1 )
	begin
		-- Get the userid for this user
		declare @userid int;
		select @userid = UserID from mySobek_User where UserName=@username and isActive = 'true';
		
		-- Stored procedure used to return standard data across all user fetch stored procedures
		exec mySobek_Get_User_By_UserID @userid;

	end  -- Look for current user by email and hashed password...
	else if (( select COUNT(*) from mySobek_User where EmailAddress=@username and isActive = 'true' ) = 1 )
	begin
		-- Get the userid for this user by email and hashed password
		declare @userid2 int;
		select @userid2 = UserID from mySobek_User where EmailAddress=@username and isActive = 'true';
		
		-- Stored procedure used to return standard data across all user fetch stored procedures
		exec mySobek_Get_User_By_UserID @userid2;
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Get_User_Folder_Items]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



-- Get list of items in a user's folder
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Get_User_Folder_Items]
	@userid int,
	@foldername varchar(255)
AS
BEGIN

	-- Get the folder id
	declare @folderid int
	set @folderid = ( select ISNULL(UserFolderID,-1) from mySobek_User_Folder where UserID=@userid and FolderName=@foldername );
	
	-- Get the list of items in the folder
	select G.BibID, I.VID, A.ItemOrder, isnull( I.SortDate,-1), ISNULL(A.UserNotes,'' )
	from mySobek_User_Item A, SobekCM_Item I, SobekCM_Item_Group G
	where ( I.ItemID = A.ItemID )
	  and ( I.GroupID = G.GroupID )
	  and ( A.UserFolderID = @folderid )
	  and ( I.Deleted = 'false' )
	  and ( G.Deleted = 'false' );

END;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Get_User_Group]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get information about a single user group, by user group id
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Get_User_Group]
	@usergroupid int
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Get the basic user group information
	select *
	from mySobek_User_Group G
	where ( G.UserGroupID = @usergroupid );

	-- Get the templates
	select T.TemplateCode, T.TemplateName
	from mySobek_Template T, mySobek_User_Group_Template_Link TL
	where ( TL.UserGroupID = @usergroupid ) and ( TL.TemplateID = T.TemplateID );

	-- Get the default metadata
	select P.MetadataCode, P.MetadataName
	from mySobek_DefaultMetadata P, mySobek_User_Group_DefaultMetadata_Link PL
	where ( PL.UserGroupID = @usergroupid ) and ( PL.DefaultMetadataID = P.DefaultMetadataID );

	-- Get the regular expression for editable items
	select R.EditableRegex, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from mySobek_Editable_Regex R, mySobek_User_Group_Editable_Link L
	where ( L.UserGroupID = @usergroupid ) and ( L.EditableID = R.EditableID );

	-- Get the list of aggregations associated with this user
	select A.Code, A.[Name], L.CanSelect, L.CanEditItems, L.IsCurator, L.CanEditMetadata, L.CanEditBehaviors, L.CanPerformQc, L.CanUploadFiles, L.CanChangeVisibility, L.CanDelete, L.IsAdmin
	from SobekCM_Item_Aggregation A, mySobek_User_Group_Edit_Aggregation L
	where  ( L.AggregationID = A.AggregationID ) and ( L.UserGroupID = @usergroupid );

	-- Get the list of all user's linked to this user group
	select U.UserID, U.UserName, U.EmailAddress, U.FirstName, U.NickName, U.LastName
	from mySobek_User U, mySobek_User_Group_Link L
	where ( L.UserGroupID = @usergroupid )
	  and ( L.UserID = U.UserID );
END;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Get_User_Searches]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get list of saved searches
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Get_User_Searches]
	@userid int
AS
BEGIN
	select * 
	from mySobek_User_Search
	where UserID = @userid
	order by ItemOrder, DateAdded
END
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Link_User_To_User_Group]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[mySobek_Link_User_To_User_Group]
	@userid int,
	@usergroupid int
AS
begin

	if (( select COUNT(*) from mySobek_User_Group_Link where UserID=@userid and UserGroupID = @usergroupid ) = 0 )
	begin
	
		insert into mySobek_User_Group_Link ( UserGroupID, UserID )
		values ( @usergroupid, @userid );
	
	end;

end;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Permissions_Report]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[mySobek_Permissions_Report] as
begin

	-- Return the top-level permissions (non-aggregation specific)
	select '' as GroupName, U.UserID, UserName, EmailAddress, FirstName, LastName, Nickname, DateCreated, LastActivity, isActive, 
		case when e.UserID is null then 'false' else 'true' end as Can_Edit_All_Items,
		Internal_User, Can_Delete_All_Items, IsPortalAdmin, IsSystemAdmin, IsHostAdmin, IsUserAdmin
	from mySobek_User as U left outer join
		 mySobek_User_Editable_Link as E on E.UserID = U.UserID and E.EditableID = 1 
	where      ( IsSystemAdmin = 'true' )
			or ( IsPortalAdmin = 'true' )
			or ( Can_Delete_All_Items = 'true' )
			or ( IsHostAdmin = 'true' )
			or ( Internal_User = 'true' )
	union
	select G.GroupName, U.UserID, UserName, EmailAddress, FirstName, LastName, Nickname, DateCreated, LastActivity, isActive, 
		case when e.UserGroupID is null then 'false' else 'true' end as Can_Edit_All_Items,
		G.Internal_User, G.Can_Delete_All_Items, G.IsPortalAdmin, G.IsSystemAdmin, 'false', 'false'
	from mySobek_User as U inner join
		 mySobek_User_Group_Link as L on U.UserID = L.UserID inner join
		 mySobek_User_Group as G on G.UserGroupID = L.UserGroupID left outer join
		 mySobek_User_Group_Editable_Link as E on E.UserGroupID = G.UserGroupID and E.EditableID = 1 
	where      ( G.IsSystemAdmin = 'true' )
			or ( G.IsPortalAdmin = 'true' )
			or ( G.Can_Delete_All_Items = 'true' )
			or ( G.Internal_User = 'true' )
	order by LastName ASC, FirstName ASC, GroupName ASC;
end;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Permissions_Report_Aggregation]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE OR ALTER PROCEDURE [dbo].[mySobek_Permissions_Report_Aggregation]
	@Code varchar(20)
as
begin

	-- Get the aggregation id
	declare @aggrId int;
	set @aggrId=-1;
	if ( exists ( select 1 from SobekCM_Item_Aggregation where Code=@Code ))
	begin
		set @aggrId = ( select AggregationID from SobekCM_Item_Aggregation where Code=@Code );
	end;

	-- Can the unioned permissions
	select GroupDefined='false', GroupName='', UserGroupID=-1, U.UserID, UserName, EmailAddress, FirstName, LastName, Nickname, DateCreated, LastActivity, isActive, 
		   P.CanSelect, P.CanEditItems, P.IsAdmin AS IsAggregationAdmin, P.IsCurator AS IsCollectionManager, P.CanEditMetadata, P.CanEditBehaviors, P.CanPerformQc, P.CanUploadFiles, P.CanChangeVisibility, P.CanDelete
	from mySobek_User U, mySobek_User_Edit_Aggregation P
	where ( U.UserID=P.UserID )
	  and ( P.AggregationID=@aggrId )
	  and (    ( CanSelect = 'true' ) or ( CanEditItems = 'true' ) or ( P.IsAdmin = 'true' ) or ( P.IsCurator ='true' ) or ( P.CanEditMetadata = 'true' )
	        or ( CanEditBehaviors = 'true' ) or ( P.CanPerformQc = 'true' ) or ( P.CanUploadFiles = 'true' ) or ( P.CanChangeVisibility = 'true' ) or ( P.CanDelete = 'true' ))
	union
	select GroupDefined='true', GroupName=G.GroupName, G.UserGroupID, U.UserID, UserName, EmailAddress, FirstName, LastName, Nickname, DateCreated, LastActivity, isActive, 
		   P.CanSelect, P.CanEditItems, P.IsAdmin AS IsAggregationAdmin, P.IsCurator AS IsCollectionManager, P.CanEditMetadata, P.CanEditBehaviors, P.CanPerformQc, P.CanUploadFiles, P.CanChangeVisibility, P.CanDelete
	from mySobek_User U, mySobek_User_Group_Link L, mySobek_User_Group G, mySobek_User_Group_Edit_Aggregation P
	where ( U.UserID=L.UserID )
	  and ( L.UserGroupID=G.UserGroupID )
	  and ( G.UserGroupID=P.UserGroupID )
	  and ( P.AggregationID=@aggrId )
	order by LastName ASC, FirstName ASC;
end;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Permissions_Report_Aggregation_Links]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[mySobek_Permissions_Report_Aggregation_Links] as
begin
	-- Distinct (UserID, Code) pairs where the user personally has aggregation edit rights
	with UserLevel as (
		select distinct P.UserID, A.Code
		from mySobek_User_Edit_Aggregation as P inner join
		     SobekCM_Item_Aggregation A on A.AggregationID = P.AggregationID
		where ( P.CanEditMetadata='true' )
		   or ( P.CanEditBehaviors='true' )
		   or ( P.CanPerformQc='true' )
		   or ( P.CanUploadFiles='true' )
		   or ( P.CanChangeVisibility='true' )
		   or ( P.IsCurator='true' )
		   or ( P.IsAdmin='true' )
	),
	-- Distinct (UserID, Code) pairs where a group the user belongs to has aggregation edit rights
	GroupLevel as (
		select distinct L.UserID, A.Code
		from mySobek_User_Group_Link as L inner join
		     mySobek_User_Group_Edit_Aggregation as P on P.UserGroupID = L.UserGroupID inner join
		     SobekCM_Item_Aggregation A on A.AggregationID = P.AggregationID
		where ( P.CanEditMetadata='true' )
		   or ( P.CanEditBehaviors='true' )
		   or ( P.CanPerformQc='true' )
		   or ( P.CanUploadFiles='true' )
		   or ( P.CanChangeVisibility='true' )
		   or ( P.IsCurator='true' )
		   or ( P.IsAdmin='true' )
	),
	-- NOTE FOR POSTGRESQL PORT: WITHIN GROUP (ORDER BY ...) is SQL-Server-only syntax.
	-- PostgreSQL equivalent: string_agg(Code, ', ' ORDER BY Code ASC) -- ORDER BY moves inside the parens, no WITHIN GROUP
	UserAgg as ( select UserID, string_agg(Code, ', ') WITHIN GROUP (ORDER BY Code ASC) as UserPermissioned from UserLevel group by UserID ),
	GroupAgg as ( select UserID, string_agg(Code, ', ') WITHIN GROUP (ORDER BY Code ASC) as GroupPermissioned from GroupLevel group by UserID )
	select U.UserID, U.UserName, U.EmailAddress, U.FirstName, U.LastName, U.Nickname, U.DateCreated, U.LastActivity, U.isActive,
	       coalesce(UA.UserPermissioned, '') as UserPermissioned,
	       coalesce(GA.GroupPermissioned, '') as GroupPermissioned
	from mySobek_User U inner join
	     ( select UserID from UserAgg union select UserID from GroupAgg ) AllUsers on AllUsers.UserID = U.UserID left outer join
	     UserAgg UA on UA.UserID = U.UserID left outer join
	     GroupAgg GA on GA.UserID = U.UserID
	order by U.LastName ASC, U.FirstName ASC;
end;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Permissions_Report_Linked_Aggregations]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Get the list of aggregations that have special rights given to some users
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Permissions_Report_Linked_Aggregations] AS
BEGIN


	-- Get the list of all aggregations that have special links
	with aggregations_permissioned as
	(
		select distinct AggregationID 
		from mySobek_User_Edit_Aggregation
		union
		select distinct AggregationID 
		from mySobek_User_Group_Edit_Aggregation
	)
	select A.Code, A.Name, A.Type
	from SobekCM_Item_Aggregation A, aggregations_permissioned P
	where A.AggregationID = P.AggregationID
	order by A.Code;

END;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Permissions_Report_Submission_Rights]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[mySobek_Permissions_Report_Submission_Rights] as
begin
	-- Users who can submit items, either directly or via a group
	with SubmitUsers as (
		select UserID from mySobek_User where Can_Submit_Items = 'true'
		union
		select L.UserID
		from mySobek_User_Group_Link as L inner join
		     mySobek_User_Group as G on G.UserGroupID = L.UserGroupID
		where G.Can_Submit_Items = 'true'
	),
	-- NOTE FOR POSTGRESQL PORT: WITHIN GROUP (ORDER BY ...) is SQL-Server-only syntax.
	-- PostgreSQL equivalent: string_agg(T.TemplateCode, ', ' ORDER BY T.TemplateCode ASC)
	TemplateAgg as (
		select L.UserID, string_agg(T.TemplateCode, ', ') WITHIN GROUP (ORDER BY T.TemplateCode ASC) as Templates
		from mySobek_User_Template_Link L inner join
		     mySobek_Template T on L.TemplateID = T.TemplateID
		group by L.UserID
	),
	-- NOTE FOR POSTGRESQL PORT: same as above -- string_agg(M.MetadataCode, ', ' ORDER BY M.MetadataCode ASC)
	DefaultMetadataAgg as (
		select L.UserID, string_agg(M.MetadataCode, ', ') WITHIN GROUP (ORDER BY M.MetadataCode ASC) as DefaultMetadatas
		from mySobek_User_DefaultMetadata_Link L inner join
		     mySobek_DefaultMetadata M on L.DefaultMetadataID = M.DefaultMetadataID
		group by L.UserID
	)
	select U.UserID, U.UserName, U.EmailAddress, U.FirstName, U.LastName, U.Nickname, U.DateCreated, U.LastActivity, U.isActive,
	       coalesce(TA.Templates, '') as Templates,
	       coalesce(DA.DefaultMetadatas, '') as DefaultMetadatas
	from mySobek_User U inner join
	     SubmitUsers S on S.UserID = U.UserID left outer join
	     TemplateAgg TA on TA.UserID = U.UserID left outer join
	     DefaultMetadataAgg DA on DA.UserID = U.UserID
	order by U.LastName ASC, U.FirstName ASC;
end;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Reset_User_Password]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Reset a user's password
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Reset_User_Password]
	@userid int,
	@password varchar(100),
	@is_temporary bit
AS
BEGIN
	
	update mySobek_User
	set [Password]=@password, isTemporary_Password=@is_temporary
	where UserID = @userid

END
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Save_DefaultMetadata]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add a new default metadata set to this database
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Save_DefaultMetadata]
	@metadata_code varchar(20),
	@metadata_name varchar(100),
	@description varchar(255),
	@userid int
AS
BEGIN
	
	-- Does this project already exist?
	if (( select count(*) from mySobek_DefaultMetadata where MetadataCode=@metadata_code ) > 0 )
	begin
		-- Update the existing default metadata
		update mySobek_DefaultMetadata
		set [Description] = @description, [MetadataName] = @metadata_name
		where MetadataCode = @metadata_code;
	end
	else
	begin
		-- Add a new set
		insert into mySobek_DefaultMetadata ( [Description], MetadataCode, UserID, MetadataName )
		values ( @description, @metadata_code, @userid, @metadata_name );
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Save_Template]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add a new template to this database
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Save_Template]
	@template_code varchar(20),
	@template_name varchar(100),
	@description varchar(255)
AS
BEGIN
	
	-- Does this template already exist?
	if (( select count(*) from mySobek_Template where TemplateCode=@template_code ) > 0 )
	begin
		-- Update the existing template
		update mySobek_Template
		set TemplateName = @template_name, [Description]=@description
		where TemplateCode = @template_code
	end
	else
	begin
		-- Add a new template
		insert into mySobek_Template ( TemplateName, TemplateCode, [Description] )
		values ( @template_name, @template_code, @description )
	end
END
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Save_User]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Saves a user
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Save_User]
	@userid int,
	@shibbid char(8),
	@username nvarchar(100),
	@password nvarchar(100),
	@emailaddress nvarchar(100),
	@firstname nvarchar(100),
	@lastname nvarchar(100),
	@cansubmititems bit,
	@nickname nvarchar(100),
	@organization nvarchar(250),
	@college nvarchar(250),
	@department nvarchar(250),
	@unit nvarchar(250),
	@rights nvarchar(1000),
	@sendemail bit,
	@language nvarchar(50),
	@default_template varchar(50),
	@default_metadata varchar(50),
	@organization_code varchar(15),
	@receivestatsemail bit,
	@scanningtechnician bit,
	@processingtechnician bit,
	@internalnotes nvarchar(500),
	@authentication varchar(20),
	@external_provider_code nvarchar(50),
	@external_subject_id nvarchar(450),
	@authentication_source nvarchar(100)
AS
BEGIN

	if ( @userid < 0 )
	begin

		-- Add this into the user table first
		insert into mySobek_User ( ShibbID, UserName, [Password], EmailAddress, LastName, FirstName, DateCreated, LastActivity, isActive,  Note_Length, Can_Make_Folders_Public, 
									isTemporary_Password, Can_Submit_Items, NickName, Organization, College, Department, Unit, Default_Rights, sendEmailOnSubmission, UI_Language, 
									Internal_User, OrganizationCode, Receive_Stats_Emails, Include_Tracking_Standard_Forms, ScanningTechnician, ProcessingTechnician, InternalNotes,
									ExternalProviderCode, ExternalSubjectId, AuthenticationSource)
		values ( @shibbid, @username, @password, @emailaddress, @lastname, @firstname, getdate(), getDate(), 'true', 1000, 'true', 
					'false', @cansubmititems, @nickname, @organization, @college, @department, @unit, @rights, @sendemail, @language, 
					'false', @organization_code, @receivestatsemail, 'false', @scanningtechnician, @processingtechnician, @internalnotes,
					@external_provider_code, @external_subject_id, @authentication_source);

		-- Get the user is
		declare @newuserid int;
		set @newuserid = @@identity;
		
		-- This is a brand new user, so we must set the default groups, according to
		-- the authentication method
		-- Authentticated used the built-in Sobek authentication
		if (( @authentication='sobek' ) and (( select count(*) from mySobek_user_Group where IsSobekDefault = 'true' ) > 0 ))
		begin
			-- insert any groups set as default for this
			insert into mySobek_User_Group_Link ( UserID, UserGroupID )
			select @newuserid, UserGroupID
			from mySobek_User_Group where IsSobekDefault='true';
		end;
		
		-- Authenticated using Shibboleth authentication
		if (( @authentication='shibboleth' ) and (( select count(*) from mySobek_user_Group where IsShibbolethDefault = 'true' ) > 0 ))
		begin
			-- insert any groups set as default for this
			insert into mySobek_User_Group_Link ( UserID, UserGroupID )
			select @newuserid, UserGroupID
			from mySobek_User_Group where IsShibbolethDefault='true';
		end;
		
		-- Authenticated using Ldap authentication
		if (( @authentication='ldap' ) and (( select count(*) from mySobek_user_Group where IsLdapDefault = 'true' ) > 0 ))
		begin
			-- insert any groups set as default for this
			insert into mySobek_User_Group_Link ( UserID, UserGroupID )
			select @newuserid, UserGroupID
			from mySobek_User_Group where IsLdapDefault='true';
		end;
	end
	else
	begin

		-- Update this user
		update mySobek_User
		set EmailAddress=@emailAddress,
			Firstname = @firstname, Lastname = @lastname, Can_Submit_Items = @cansubmititems,
			NickName = @nickname, Organization=@organization, College=@college, Department=@department,
			Unit=@unit, Default_Rights=@rights, sendEmailOnSubmission = @sendemail, UI_Language=@language,
			OrganizationCode=@organization_code, Receive_Stats_Emails=@receivestatsemail,
			ScanningTechnician=@scanningtechnician, ProcessingTechnician=@processingtechnician,
			InternalNotes=@internalnotes
		where UserID = @userid;

		-- Set the default template
		if ( len( @default_template ) > 0 )
		begin
			-- Get the template id
			declare @templateid int;
			select @templateid = TemplateID from mySobek_Template where TemplateCode=@default_template;

			-- Clear the current default template
			update mySobek_User_Template_Link set DefaultTemplate = 'false' where UserID=@userid;

			-- Does this link already exist?
			if (( select count(*) from mySobek_User_Template_Link where UserID=@userid and TemplateID=@templateid ) > 0 )
			begin
				-- Update the link
				update mySobek_User_Template_Link set DefaultTemplate = 'true' where UserID=@userid and TemplateID=@templateid;
			end
			else
			begin
				-- Just add this link
				insert into mySobek_User_Template_Link ( UserID, TemplateID, DefaultTemplate ) values ( @userid, @templateid, 'true' );
			end;
		end;

		-- Set the default metadata
		if ( len( @default_metadata ) > 0 )
		begin
			-- Get the project id
			declare @projectid int;
			select @projectid = DefaultMetadataID from mySobek_DefaultMetadata where MetadataCode=@default_metadata;

			-- Clear the current default project
			update mySobek_User_DefaultMetadata_Link set CurrentlySelected = 'false' where UserID=@userid;

			-- Does this link already exist?
			if (( select count(*) from mySobek_User_DefaultMetadata_Link where UserID=@userid and DefaultMetadataID=@projectid ) > 0 )
			begin
				-- Update the link
				update mySobek_User_DefaultMetadata_Link set CurrentlySelected = 'true' where UserID=@userid and DefaultMetadataID=@projectid;
			end
			else
			begin
				-- Just add this link
				insert into mySobek_User_DefaultMetadata_Link ( UserID, DefaultMetadataID, CurrentlySelected ) values ( @userid, @projectid, 'true' );
			end;
		end;
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Save_User_Group]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Saves information about a single user group
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Save_User_Group]
	@usergroupid int,
	@groupname nvarchar(150),
	@groupdescription nvarchar(1000),
	@can_submit_items bit,
	@is_internal bit,
	@can_edit_all bit,
	@is_system_admin bit,
	@is_portal_admin bit,
	@include_tracking_standard_forms bit,
	@clear_metadata_templates bit,
	@clear_aggregation_links bit,
	@clear_editable_links bit,
	@is_sobek_default bit,
	@is_shibboleth_default bit,
	@is_ldap_default bit,
	@new_usergroupid int output
AS 
begin
	
	-- Is there a user group id provided
	if ( @usergroupid < 0 )
	begin
		-- Insert as a new user group
		insert into mySobek_User_Group ( GroupName, GroupDescription, Can_Submit_Items, Internal_User, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms, IsSobekDefault, IsShibbolethDefault, IsLdapDefault  )
		values ( @groupname, @groupdescription, @can_submit_items, @is_internal, @is_system_admin, @is_portal_admin, @include_tracking_standard_forms, @is_sobek_default, @is_shibboleth_default, @is_ldap_default );
		
		-- Return the new primary key
		set @new_usergroupid = @@IDENTITY;	
	end
	else
	begin
		-- Update, if it exists
		update mySobek_User_Group
		set GroupName = @groupname, GroupDescription = @groupdescription, Can_Submit_Items = @can_submit_items, Internal_User=@is_internal, IsSystemAdmin=@is_system_admin, IsPortalAdmin=@is_portal_admin, Include_Tracking_Standard_Forms=@include_tracking_standard_forms, 
			IsSobekDefault=@is_sobek_default, IsShibbolethDefault=@is_shibboleth_default, IsLdapDefault=@is_ldap_default
		where UserGroupID = @usergroupid;
	
	end;
	
	-- Check the flag to edit all items
	if ( @can_edit_all = 'true' )
	begin	
		if ( ( select count(*) from mySobek_User_Group_Editable_Link where EditableID=1 and UserGroupID=@usergroupid ) = 0 )
		begin
			-- Add the link to the ALL EDITABLE
			insert into mySobek_User_Group_Editable_Link ( UserGroupID, EditableID )
			values ( @usergroupid, 1 );
		end
	end
	else
	begin
		-- Delete the link to all
		delete from mySobek_User_Group_Editable_Link where EditableID = 1 and UserGroupID=@usergroupid;
	end;
	
		-- Clear the projects/templates
	if ( @clear_metadata_templates = 'true' )
	begin
		delete from mySobek_User_Group_DefaultMetadata_Link where UserGroupID=@usergroupid;
		delete from mySobek_User_Group_Template_Link where UserGroupID=@usergroupid;
	end;

	-- Clear the aggregations link
	if ( @clear_aggregation_links = 'true' )
	begin
		delete from mySobek_User_Group_Edit_Aggregation where UserGroupID=@usergroupid;
	end;
	
	-- Clear the editable link
	if ( @clear_editable_links = 'true' )
	begin
		delete from mySobek_User_Group_Editable_Link where UserGroupID=@usergroupid;
	end;

end;
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Save_User_Search]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add a sarch to the user's list of saved searches
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Save_User_Search]
	@userid int,
	@searchurl nvarchar(500),
	@searchdescription nvarchar(500),
	@itemorder int,
	@usernotes nvarchar(2000),
	@new_usersearchid int output
AS
begin

	-- See if this already exists
	if (( select count(*) from mySobek_User_Search where UserID=@userid and SearchURL=@searchurl ) > 0 )
	begin
		-- update existing
		update mySobek_User_Search
		set ItemOrder=@itemorder, UserNotes=@usernotes
		where UserID=@userid and SearchURL=@searchurl

		-- Just set this to -1, since nothing new was added
		set @new_usersearchid = -1
	end
	else
	begin
		-- Add a new search
		insert into mySobek_User_Search( UserID, SearchURL, SearchDescription, ItemOrder, UserNotes, DateAdded )
		values ( @userid, @searchurl, @searchdescription, @itemorder, @usernotes, getdate())

		-- Return the new identifier
		set @new_usersearchid = @@identity
	end
end
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Set_Aggregation_Home_Page_Flag]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Set aggregation home page flag 
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Set_Aggregation_Home_Page_Flag]
	@userid int,
	@aggregationid int,
	@onhomepage bit
AS
begin transaction

	-- Check to see if this aggregation is already tied to this user
	if ( ( select count(*) from mySobek_User_Edit_Aggregation where UserID=@userid and AggregationID=@aggregationid ) > 0 )
	begin
		-- update existing link
		update mySobek_User_Edit_Aggregation
		set OnHomePage=@onhomepage
		where UserID =  @userid and AggregationID = @aggregationid

		-- delete any links that have nothing flagged
		delete from mySobek_User_Edit_Aggregation
		where CanSelect='false' and CanEditItems='false' and IsCurator='false' and OnHomePage='false'
	end
	else
	begin
		-- Insert new link with no permissions
		insert into mySobek_User_Edit_Aggregation ( UserID, AggregationID, CanSelect, CanEditItems, IsCurator, OnHomePage )
		values ( @userid, @aggregationid, 'false', 'false', 'false', @onhomepage )
	end

commit transaction
GO
/****** Object:  StoredProcedure [dbo].[mySobek_Update_User]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Procedure allows an admin to edit permissions flags for this user
CREATE OR ALTER PROCEDURE [dbo].[mySobek_Update_User]
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
      @clear_user_groups bit
AS
begin transaction

      -- Update the simple table values
      update mySobek_User
      set Can_Submit_Items=@can_submit, Internal_User=@is_internal, 
            IsPortalAdmin=@is_portal_admin, IsSystemAdmin=@is_system_admin, 
            Include_Tracking_Standard_Forms=@include_tracking_standard_forms, 
            EditTemplate=@edit_template, Can_Delete_All_Items = @can_delete_all,
            EditTemplateMarc=@edit_template_marc, IsHostAdmin=@is_host_admin,
			IsUserAdmin=@is_user_admin
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
/****** Object:  StoredProcedure [dbo].[mySobek_UserName_Exists]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Checks to see if the username or email exists
CREATE OR ALTER PROCEDURE [dbo].[mySobek_UserName_Exists]
	@username nvarchar(100),
	@email varchar(100),
	@username_exists bit output,
	@email_exists bit output
AS
BEGIN

	-- Check if username exists
	if ( ( select count(*) from mySobek_User where UserName = @username ) = 0 )
	begin
		set @username_exists = 'false';
	end
	else
	begin
		set @username_exists = 'true';
	end	

	-- Check if email exists
	if ( ( select count(*) from mySobek_User where EmailAddress = @email ) = 0 )
	begin
		set @email_exists = 'false';
	end
	else
	begin
		set @email_exists = 'true';
	end	

END
GO
/****** Object:  StoredProcedure [dbo].[mySobek_View_All_User_Tags]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- View all of a single user's tag
CREATE OR ALTER PROCEDURE [dbo].[mySobek_View_All_User_Tags]
	@UserID int
AS
begin

	if ( @UserID < 0 )
	begin
		select T.TagID, G.BibID, I.VID, T.Description_Tag, T.Date_Modified, U.UserID, U.FirstName, U.NickName, U.LastName 
		from mySobek_User_Description_Tags T, mySobek_User U, SobekCM_Item I, SobekCM_Item_Group G
		where T.UserID=U.UserID
		  and T.ItemID = I.ItemID
		  and I.GroupID = G.GroupID
	end
	else
	begin
		select T.TagID, G.BibID, I.VID, T.Description_Tag, T.Date_Modified, U.UserID, U.FirstName, U.NickName, U.LastName 
		from mySobek_User_Description_Tags T, mySobek_User U, SobekCM_Item I, SobekCM_Item_Group G
		where T.UserID=U.UserID 
		  and T.UserID=@UserID
		  and T.ItemID = I.ItemID
		  and I.GroupID = G.GroupID
	end
end
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Add_External_Record_Number]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- This procedure adds a new external record number to an existing item
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Add_External_Record_Number]
	@groupID int,
	@extRecordValue varchar(50),
	@extRecordType varchar(25)
AS
begin transaction

	-- declare two variables that may be needed for this
	declare @extRecordTypeID int;
	declare @extRecordLinkID int;

	-- Look for an existing record type
	select @extRecordTypeID = isnull(extRecordTypeID, -1)
	from SobekCM_External_Record_Type
	where (extRecordType = @extRecordType);

	-- Was this a new record type
	if ( isnull( @extRecordTypeID, -1 ) < 0 )
	begin
		-- Add this new record type
		insert into SobekCM_External_Record_Type ( ExtRecordType, repeatableTypeFlag )
		values ( @extRecordType, 1 );

		-- Save this new id
		set @extRecordTypeID = @@identity;
	end;

	-- The linkID parameter is less than zero; query the database 
	-- to see if one exists for this record type.		
	select @extRecordLinkID = isnull( extRecordLinkID, -1 )
	from [SobekCM_Item_Group_External_Record]
	where (GroupID = @groupID )
	  and ( ExtRecordTypeID = @extRecordTypeID )
	  and ( ExtRecordValue = @extRecordValue );

	if (isnull( @extRecordLinkID, -1 ) < 0)
	begin	
		-- Check to see if this record type is singular type (nonrepeatable)
		if (( select count(*) from SobekCM_External_Record_Type where ExtRecordTypeID = @extRecordTypeID and repeatableTypeFlag = 'False' ) > 0 )
		begin
			-- Look for an existing singular record for this item group
			if (( select count(*) from SobekCM_Item_Group_External_Record 
				where ( ExtRecordTypeID = @extRecordTypeID ) and ( GroupID = @groupID )) > 0 )
			begin
				-- Get the link id
				select @extRecordLinkID = extRecordLinkID
				from SobekCM_Item_Group_External_Record 
				where ( ExtRecordTypeID = @extRecordTypeID ) and ( GroupID = @groupID );

				--Update existing link
				update SobekCM_Item_Group_External_Record
				set extRecordValue = @extRecordValue 
				where (extRecordLinkID = @extRecordLinkID);
			end
			else
			begin
				-- No existing record for this singular record type, so just insert
				insert into SobekCM_Item_Group_External_Record ( groupid, extRecordTypeID, extRecordValue)
				values ( @groupID, @extRecordTypeID, @extRecordValue );
			end;
		end
		else
		begin
			-- Non-singular record type value, so just insert if it doesn't exist
			if (( select COUNT(*) from SobekCM_Item_Group_External_Record where GroupID=@groupID and ExtRecordTypeID=@extRecordTypeID and ExtRecordValue = @extRecordValue ) = 0 )
			begin
				insert into SobekCM_Item_Group_External_Record ( groupid, extRecordTypeID, extRecordValue)
				values ( @groupID, @extRecordTypeID, @extRecordValue );
			end;
		end;
	end;
	
commit transaction;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Add_Item_Aggregation_Milestone]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Add_Item_Aggregation_Milestone]
	@AggregationCode varchar(20),
	@Milestone nvarchar(150),
	@MilestoneUser nvarchar(max)
AS
begin transaction

	-- get the aggregation id
	declare @aggregationid int;
	set @aggregationid = coalesce( (select AggregationID from SobekCM_Item_Aggregation where Code=@AggregationCode), -1);
	
	if ( @aggregationid > 0 )
	begin
		
		-- only enter one of these per day
		if ( (select count(*) from [SobekCM_Item_Aggregation_Milestones] where ( AggregationID = @aggregationid ) and ( MilestoneUser=@MilestoneUser ) and ( Milestone=@Milestone) and ( CONVERT(VARCHAR(10), MilestoneDate, 102) = CONVERT(VARCHAR(10), getdate(), 102) )) = 0 )
		--if ( (select count(*) from [SobekCM_Item_Aggregation_Milestones] where ( AggregationID = @aggregationid ) and ( MilestoneUser=@MilestoneUser ) and ( Milestone=@Milestone) and ( MilestoneDate=getdate())) = 0 )
		begin
			-- Just add this new milestone then
			insert into [SobekCM_Item_Aggregation_Milestones] ( AggregationID, Milestone, MilestoneDate, MilestoneUser )
			values ( @aggregationid, @Milestone, getdate(), @MilestoneUser );
		end;	
	end;

commit transaction;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Add_Item_Viewers]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add or update existing viewers for an item
-- NOTE: This does not delete any existing viewers
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Add_Item_Viewers] 
	@ItemID int,
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
BEGIN 

	
	-- Add the first viewer information, if provided
	if ( len(coalesce(@Viewer1_Type, '')) > 0 )
	begin
		-- Get the primary key for this viewer type
		declare @Viewer1_TypeID int;
		set @Viewer1_TypeID = coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = @Viewer1_Type ), -1 );

		-- Only continue if that viewer type was found
		if ( @Viewer1_TypeID > 0 )
		begin
			-- Does this already exist?
			if ( exists ( select 1 from SobekCM_Item_Viewers where ItemID=@ItemID and ItemViewTypeID=@Viewer1_TypeID ))
			begin
				-- Update this viewer information
				update SobekCM_Item_Viewers
				set Attribute=@Viewer1_Attribute, Label=@Viewer1_Label, Exclude='false'
				where ( ItemID = @ItemID )
				  and ( ItemViewTypeID = @Viewer1_TypeID );
			end
			else
			begin
				-- Insert this viewer information
				insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
				values ( @ItemID, @Viewer1_TypeID, @Viewer1_Attribute, @Viewer1_Label );
			end;
		end;
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
			-- Does this already exist?
			if ( exists ( select 1 from SobekCM_Item_Viewers where ItemID=@ItemID and ItemViewTypeID=@Viewer2_TypeID ))
			begin
				-- Update this viewer information
				update SobekCM_Item_Viewers
				set Attribute=@Viewer2_Attribute, Label=@Viewer2_Label, Exclude='false'
				where ( ItemID = @ItemID )
				  and ( ItemViewTypeID = @Viewer2_TypeID );
			end
			else
			begin
				-- Insert this viewer information
				insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
				values ( @ItemID, @Viewer2_TypeID, @Viewer2_Attribute, @Viewer2_Label );
			end;
		end;
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
			-- Does this already exist?
			if ( exists ( select 1 from SobekCM_Item_Viewers where ItemID=@ItemID and ItemViewTypeID=@Viewer3_TypeID ))
			begin
				-- Update this viewer information
				update SobekCM_Item_Viewers
				set Attribute=@Viewer3_Attribute, Label=@Viewer3_Label, Exclude='false'
				where ( ItemID = @ItemID )
					and ( ItemViewTypeID = @Viewer3_TypeID );
			end
			else
			begin
				-- Insert this viewer information
				insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
				values ( @ItemID, @Viewer3_TypeID, @Viewer3_Attribute, @Viewer3_Label );
			end;
		end;
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
			-- Does this already exist?
			if ( exists ( select 1 from SobekCM_Item_Viewers where ItemID=@ItemID and ItemViewTypeID=@Viewer4_TypeID ))
			begin
				-- Update this viewer information
				update SobekCM_Item_Viewers
				set Attribute=@Viewer4_Attribute, Label=@Viewer4_Label, Exclude='false'
				where ( ItemID = @ItemID )
				  and ( ItemViewTypeID = @Viewer4_TypeID );
			end
			else
			begin
				-- Insert this viewer information
				insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
				values ( @ItemID, @Viewer4_TypeID, @Viewer4_Attribute, @Viewer4_Label );
			end;
		end;
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
			-- Does this already exist?
			if ( exists ( select 1 from SobekCM_Item_Viewers where ItemID=@ItemID and ItemViewTypeID=@Viewer5_TypeID ))
			begin
				-- Update this viewer information
				update SobekCM_Item_Viewers
				set Attribute=@Viewer5_Attribute, Label=@Viewer5_Label, Exclude='false'
				where ( ItemID = @ItemID )
				  and ( ItemViewTypeID = @Viewer5_TypeID );
			end
			else
			begin
				-- Insert this viewer information
				insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
				values ( @ItemID, @Viewer5_TypeID, @Viewer5_Attribute, @Viewer5_Label );
			end;
		end;
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
			-- Does this already exist?
			if ( exists ( select 1 from SobekCM_Item_Viewers where ItemID=@ItemID and ItemViewTypeID=@Viewer6_TypeID ))
			begin
				-- Update this viewer information
				update SobekCM_Item_Viewers
				set Attribute=@Viewer6_Attribute, Label=@Viewer6_Label, Exclude='false'
				where ( ItemID = @ItemID )
				  and ( ItemViewTypeID = @Viewer6_TypeID );
			end
			else
			begin
				-- Insert this viewer information
				insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
				values ( @ItemID, @Viewer6_TypeID, @Viewer6_Attribute, @Viewer6_Label );
			end;
		end;
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Add_OAI_PMH_Data]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add some OAI-PMH data to an item.  Included will be the data (usually in XML format)
-- and the OAI-PMH code for that data type.  The XML information is saved as nvarchar, rather
-- than XML, since this data is never sub-queried.  It is just returned while serving OAI.
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Add_OAI_PMH_Data]
	@itemid int,
	@data_code nvarchar(20),
	@oai_data nvarchar(max)
AS
begin

	-- Does this already exists?
	if (( select COUNT(*) from SobekCM_Item_OAI where ItemID=@itemid and Data_Code=@data_code ) = 0 )
	begin
		insert into SobekCM_Item_OAI ( ItemID, OAI_Data, OAI_Date, Data_Code )
		values ( @itemid, @oai_data, GETDATE(), @data_code );
	end
	else
	begin
		update SobekCM_Item_OAI
		set OAI_Data=@oai_data, OAI_Date=GETDATE(), Data_Code=@data_code
		where ItemID=@itemid and Locked='false' and Data_Code=@data_code;
	end;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Add_Web_Skin]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Procedure to add a new web skin, or edit an existing web skin
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Add_Web_Skin]
	@webskincode varchar(20),
	@basewebskin varchar(20),
	@overridebanner bit,
	@overrideheaderfooter bit,
	@bannerlink varchar(255),
	@notes varchar(250),
	@build_on_launch bit,
	@suppress_top_nav bit	
AS
BEGIN
	-- Does a web skin with this code already exist?
	if (( select count(*) from SobekCM_Web_Skin where WebSkinCode = @webskincode ) = 0 )
	begin
		-- No?  Add a new one
		insert into SobekCM_Web_Skin ( WebSkinCode, OverrideHeaderFooter, OverrideBanner, BaseWebSkin, BannerLink, Notes, Build_On_Launch, SuppressTopNavigation )
		values ( @webskincode, @overrideheaderfooter, @overridebanner, @basewebskin, @bannerlink, @notes, @build_on_launch, @suppress_top_nav );

	end
	else
	begin
		-- Yes? Update the existing web skin with the same code
		update SobekCM_Web_Skin
		set OverrideHeaderFooter=@overrideheaderfooter, OverrideBanner=@overridebanner, BaseWebSkin=@basewebskin, BannerLink=@bannerlink, Notes=@notes, Build_On_Launch=@build_on_launch, SuppressTopNavigation=@suppress_top_nav
		where WebSkinCode = @webskincode;
	
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Aggregation_Change_Log]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Aggregation_Change_Log]
	@Code varchar(20)
as
begin

	-- Get the aggregation id
	declare @aggrId int;
	set @aggrId=-1;
	if ( exists ( select 1 from SobekCM_Item_Aggregation where Code=@Code ))
	begin
		set @aggrId = ( select AggregationID from SobekCM_Item_Aggregation where Code=@Code );
	end;

	-- Get the history
	select Milestone, MilestoneDate, MilestoneUser
	from SobekCM_Item_Aggregation_Milestones 
	where AggregationID = @aggrId
	order by MilestoneDate ASC, AggregationMilestoneID ASC;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Builder_Add_Log]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Builder_Add_Log]
	@RelatedBuilderLogID bigint,
	@BibID_VID varchar(16),
	@LogType varchar(25),
	@LogMessage varchar(2000),
	@METS_Type varchar(50),
	@BuilderLogID bigint output
AS
BEGIN

	insert into SobekCM_Builder_Log ( RelatedBuilderLogID, LogDate, BibID_VID, LogType, LogMessage )
	values ( @RelatedBuilderLogID, getdate(), @BibID_VID, @LogType, @LogMessage );
	
	set @BuilderLogID = @@IDENTITY;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Builder_Expire_Log_Entries]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Procedure to remove expired log files
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Builder_Expire_Log_Entries]
	@Retain_For_Days int
AS 
BEGIN
	-- Calculate the expiration date time
	declare @expiredate datetime;
	set @expiredate = dateadd(day, (-1 * @Retain_For_Days), getdate());
	set @expiredate = dateadd(hour, -1 * datepart(hour,@expiredate), @expiredate);
	
	-- Delete all logs from before this time
	delete from SobekCM_Builder_Log
	where ( LogDate <= @expiredate )
	  and ( LogType = 'No Work' );

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Builder_Get_Folder_Module_Sets]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Procedure returns the names (and details) of all the module sets used for folders
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Builder_Get_Folder_Module_Sets]
as
begin

	-- Get the count of used folder modules
	with folder_modules_used ( ModuleSetID, UsedCount ) as
	( 
		select ModuleSetID, count(*) as UsedCount
		from SobekCM_Builder_Incoming_Folders 
		group by ModuleSetID
	) 
	select S.ModuleSetID, S.SetName, coalesce(U.UsedCount, 0) as UsedCount
	from SobekCM_Builder_Module_Set S inner join 
		 SobekCM_Builder_Module_Type T on S.ModuleTypeID=T.ModuleTypeID left outer join
		 folder_modules_used U on U.ModuleSetID=S.ModuleSetID
	where ( T.TypeAbbrev = 'FOLD' )
	  and ( S.[Enabled] = 1 )
	order by UsedCount DESC;

	-- Also return the modules linked to each (enabled) folder module set
	select S.ModuleSetID, S.SetName, M.[Assembly], M.Class, M.[Enabled], M.Argument1, M.Argument2, M.Argument3, M.ModuleDesc, M.[Order]
	from SobekCM_Builder_Module_Set S inner join 
		 SobekCM_Builder_Module_Type T on S.ModuleTypeID=T.ModuleTypeID inner join
		 SobekCM_Builder_Module M on M.ModuleSetID=S.ModuleSetID
	where ( T.TypeAbbrev = 'FOLD' )
	  and ( S.[Enabled] = 1 )
	order by S.ModuleSetID, M.[Order];

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Builder_Get_Incoming_Folder]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the information about a single incoming folder
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Builder_Get_Incoming_Folder] 
	@FolderId int
AS
BEGIN

	-- Return all the data about it 
	select IncomingFolderId, NetworkFolder, ErrorFolder, ProcessingFolder, Perform_Checksum_Validation, Archive_TIFF, Archive_All_Files,
		   Allow_Deletes, Allow_Folders_No_Metadata, Allow_Metadata_Updates, FolderName, BibID_Roots_Restrictions,
		   F.ModuleSetID, S.SetName
	from SobekCM_Builder_Incoming_Folders F left outer join 
	     SobekCM_Builder_Module_Set S on F.ModuleSetID=S.ModuleSetID
	where F.IncomingFolderId=@FolderId;

	-- Also return the modules linked to each (enabled) folder module set
	select S.ModuleSetID, S.SetName, M.[Assembly], M.Class, M.[Enabled], M.Argument1, M.Argument2, M.Argument3, M.ModuleDesc, M.[Order], S.[Enabled]
	from SobekCM_Builder_Incoming_Folders F inner join
		 SobekCM_Builder_Module_Set S on S.ModuleSetID=F.ModuleSetID inner join 
		 SobekCM_Builder_Module M on M.ModuleSetID=S.ModuleSetID 	 
	where F.IncomingFolderId=@FolderId
	order by M.[Order];

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Builder_Get_Latest_Update]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Gets the latest and greatest for when the builder ran, version, etc.. and also scheduled task information to show
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Builder_Get_Latest_Update]
as
begin

	-- Get the latest status / builder values which are stored in the settings table
	select Setting_Key, Setting_Value, Help, Options
	from SobekCM_Settings
	where ( Hidden = 'false' )
	  and ( TabPage = 'Builder' )
	  and ( Heading = 'Status' )
	order by TabPage, Heading, Setting_Key;

	
	-- Return all the scheduled type modules, with the schedule and the last run info
	with last_run_cte ( ModuleScheduleID, LastRun) as 
	(
		select ModuleScheduleID, MAX([Timestamp])
		from SobekCM_Builder_Module_Scheduled_Run
		group by ModuleScheduleID
	)
	-- Return all the scheduled type modules, along with information on when it was last run
	select S.ModuleSetID, S.SetName, S.[Enabled] as SetEnabled, C.ModuleScheduleID, C.[Enabled] as ScheduleEnabled, C.DaysOfWeek, C.TimesOfDay, C.[Description], coalesce(L.LastRun,'') as LastRun, coalesce(R.Outcome,'') as Outcome, coalesce(R.[Message],'') as [Message]
	from SobekCM_Builder_Module_Set S inner join
		 SobekCM_Builder_Module_Type T on S.ModuleTypeID = T.ModuleTypeID inner join
		 SobekCM_Builder_Module_Schedule C on C.ModuleSetID = S.ModuleSetID left outer join
		 last_run_cte L on L.ModuleScheduleID = C.ModuleScheduleID left outer join
		 SobekCM_Builder_Module_Scheduled_Run R on R.ModuleSchedRunID=L.ModuleScheduleID and R.[Timestamp] = L.LastRun
	where T.TypeAbbrev = 'SCHD'
	order by C.[Description], S.SetOrder;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Builder_Get_Minimum_Item_Information]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/****** Object:  StoredProcedure [dbo].[SobekCM_Builder_Get_Minimum_Item_Information]    Script Date: 12/20/2013 05:43:36 ******/
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Builder_Get_Minimum_Item_Information]
	@bibid varchar(10),
	@vid varchar(5)
AS
begin

	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Only continue if there is ONE match
	if (( select COUNT(*) from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = @BibID and I.VID = @VID ) = 1 )
	begin
		-- Get the itemid
		declare @ItemID int;
		select @ItemID = ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = @BibID and I.VID = @VID;

		-- Get the item id and mainthumbnail
		select I.ItemID, I.MainThumbnail, I.IP_Restriction_Mask, I.Born_Digital, G.ItemCount, I.Dark, I.MadePublicDate
		from SobekCM_Item I, SobekCM_Item_Group G
		where ( I.VID = @vid )
		  and ( G.BibID = @bibid )
		  and ( I.GroupID = G.GroupID );

		-- Get the links to the aggregations
		select A.Code, A.Name, A.[Type]
		from SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A
		where ( L.ItemID = @itemid )
		  and ( L.AggregationID = A.AggregationID );
	 
		-- Return the icons for this item
		select Icon_URL, Link, Icon_Name, I.Title
		from SobekCM_Icon I, SobekCM_Item_Icons L
		where ( L.IconID = I.IconID ) 
		  and ( L.ItemID = @ItemID )
		order by Sequence;
			  
		-- Return any web skin restrictions
		select S.WebSkinCode
		from SobekCM_Item_Group_Web_Skin_Link L, SobekCM_Item I, SobekCM_Web_Skin S
		where ( L.GroupID = I.GroupID )
		  and ( L.WebSkinID = S.WebSkinID )
		  and ( I.ItemID = @ItemID )
		order by L.Sequence;

		-- Return the viewers for this item
		select T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder) as MenuOrder, V.Exclude, coalesce(V.OrderOverride, T.[Order])
		from SobekCM_Item_Viewers V, SobekCM_Item_Viewer_Types T
		where ( V.ItemID = @ItemID )
		  and ( V.ItemViewTypeID = T.ItemViewTypeID )
		group by T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder), V.Exclude, coalesce(V.OrderOverride, T.[Order])
		order by coalesce(V.OrderOverride, T.[Order]) ASC;

		-- Return any special user group restriction information
		select I.UserGroupID, G.GroupName, I.canView, I.isOwner, I.canEditMetadata, I.canEditBehaviors, I.canPerformQc, I.canUploadFiles, I.canChangeVisibility, I.canDelete, I.customPermissions
		from mySobek_User_Group_Item_Permissions I, mySobek_User_Group G
		where G.UserGroupID=I.UserGroupID
		  and ItemID=@ItemID;

		-- Return any special user restriction information
		select I.UserID, U.UserName, U.UserID, I.canView, I.isOwner, I.canEditMetadata, I.canEditBehaviors, I.canPerformQc, I.canUploadFiles, I.canChangeVisibility, I.canDelete, I.customPermissions
		from mySobek_User_Item_Permissions I, mySobek_User U
		where U.UserID=I.UserID
		  and ItemID=@ItemID;
		
	end;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Builder_Get_Settings]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Routine returns all the BUILDER-specific settings
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Builder_Get_Settings]
	@include_disabled bit
as
begin

	-- Always return all the incoming folders
	select IncomingFolderId, NetworkFolder, ErrorFolder, ProcessingFolder, Perform_Checksum_Validation, Archive_TIFF, Archive_All_Files,
		   Allow_Deletes, Allow_Folders_No_Metadata, Allow_Metadata_Updates, FolderName, BibID_Roots_Restrictions,
		   F.ModuleSetID, S.SetName
	from SobekCM_Builder_Incoming_Folders F left outer join 
	     SobekCM_Builder_Module_Set S on F.ModuleSetID=S.ModuleSetID;

	-- Return all the non-scheduled type modules
	if ( @include_disabled = 'true' )
	begin
		select M.ModuleID, M.[Assembly], M.Class, M.ModuleDesc, M.Argument1, M.Argument2, M.Argument3, M.[Enabled], S.ModuleSetID, S.SetName, S.[Enabled] as SetEnabled, T.TypeAbbrev, T.TypeDescription
		from SobekCM_Builder_Module M, SobekCM_Builder_Module_Set S, SobekCM_Builder_Module_Type T
		where M.ModuleSetID = S.ModuleSetID
		  and S.ModuleTypeID = T.ModuleTypeID
		  and T.TypeAbbrev <> 'SCHD'
		order by TypeAbbrev, S.SetOrder, M.[Order];
	end
	else
	begin
		select M.ModuleID, M.[Assembly], M.Class, M.ModuleDesc, M.Argument1, M.Argument2, M.Argument3, M.[Enabled], S.ModuleSetID, S.SetName, S.[Enabled] as SetEnabled, T.TypeAbbrev, T.TypeDescription
		from SobekCM_Builder_Module M, SobekCM_Builder_Module_Set S, SobekCM_Builder_Module_Type T
		where M.ModuleSetID = S.ModuleSetID
		  and S.ModuleTypeID = T.ModuleTypeID
		  and M.[Enabled] = 'true'
		  and S.[Enabled] = 'true'
		  and T.TypeAbbrev <> 'SCHD'
		order by TypeAbbrev, S.SetOrder, M.[Order];
	end;

	-- Return all the scheduled type modules, with the schedule and the last run info
	if ( @include_disabled = 'true' )
	begin
		with last_run_cte ( ModuleScheduleID, LastRun) as 
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
	end 
	else
	begin
		with last_run_cte ( ModuleScheduleID, LastRun) as 
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
		  and M.[Enabled] = 'true'
		  and S.[Enabled] = 'true'
		  and C.[Enabled] = 'true'
		order by TypeAbbrev, S.SetOrder, M.[Order];
	end;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Builder_Incoming_Folder_Delete]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Deletes an incoming folder from the builder settings 
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Builder_Incoming_Folder_Delete]
	@IncomingFolderId int
AS 
BEGIN
	delete from SobekCM_Builder_Incoming_Folders 
	where IncomingFolderId=@IncomingFolderId;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Builder_Incoming_Folder_Edit]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Add a new incoming folder for the builder/bulk loader, or edit
-- an existing incoming folder (by incoming folder id)
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Builder_Incoming_Folder_Edit]
	@IncomingFolderId int,
	@NetworkFolder varchar(255),
	@ErrorFolder varchar(255),
	@ProcessingFolder varchar(255),
	@Perform_Checksum_Validation bit,
	@Archive_TIFF bit,
	@Archive_All_Files bit,
	@Allow_Deletes bit,
	@Allow_Folders_No_Metadata bit,
	@FolderName nvarchar(150),
	@BibID_Roots_Restrictions varchar(255),
	@ModuleSetID int
AS 
BEGIN

	-- Keep the last network folder value
	declare @lastFolder varchar(255);
	set @lastFolder = '';

	-- Is this a new incoming folder?
	if (( select COUNT(*) from SobekCM_Builder_Incoming_Folders where IncomingFolderId=@IncomingFolderId ) = 0 )
	begin	
		-- Insert new incoming folder
		insert into SobekCM_Builder_Incoming_Folders ( NetworkFolder, ErrorFolder, ProcessingFolder, Perform_Checksum_Validation, Archive_TIFF, Archive_All_Files, Allow_Deletes, Allow_Folders_No_Metadata, FolderName, Allow_Metadata_Updates, BibID_Roots_Restrictions, ModuleSetID )
		values ( @NetworkFolder, @ErrorFolder, @ProcessingFolder, @Perform_Checksum_Validation, @Archive_TIFF, @Archive_All_Files, @Allow_Deletes, @Allow_Folders_No_Metadata, @FolderName, 'true', @BibID_Roots_Restrictions, @ModuleSetID );
	end
	else
	begin

		-- Since it exists, get the old network folder
		set @lastFolder = ( select NetworkFolder from SobekCM_Builder_Incoming_Folders where IncomingFolderId=@IncomingFolderId );

		-- update existing incoming folder
		update SobekCM_Builder_Incoming_Folders
		set NetworkFolder=@NetworkFolder, ErrorFolder=@ErrorFolder, ProcessingFolder=@ProcessingFolder, 
			Perform_Checksum_Validation=@Perform_Checksum_Validation, Archive_TIFF=@Archive_TIFF, 
			Archive_All_Files=@Archive_All_Files, Allow_Deletes=@Allow_Deletes, 
			Allow_Folders_No_Metadata=@Allow_Folders_No_Metadata, FolderName=@FolderName,
			BibID_Roots_Restrictions=@BibID_Roots_Restrictions, ModuleSetID=@ModuleSetID
		where IncomingFolderId = @IncomingFolderId;
	end;
		
	-- If this is the only folder, and there is no main builder folder, set that one
	if ( ( select count(*) from SObekCM_Builder_Incoming_Folders ) = 1 )
	begin
		-- Is there a valid Main Builder Folder setting?
		if ( not exists ( select 1 from SobekCM_Settings where Setting_Key = 'Main Builder Input Folder' ))
		begin
			-- There was no match
			insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help  )
			values ( 'Main Builder Input Folder', @NetworkFolder, 'Builder', 'Builder Settings', 0, 0, 'This is the network location to the SobekCM Builder''s main incoming folder.\n\nThis is used by the SMaRT tool when doing bulk imports from spreadsheet or MARC records.' );
		end
		else if ( not exists ( select 1 from SobekCM_Settings where Setting_Key = 'Main Builder Input Folder' and len(coalesce(Setting_Value,'')) > 0 ))
		begin
			-- One existed, but apparently it had no value
			update SobekCM_Settings
			set Setting_Value = @NetworkFolder
			where Setting_Key = 'Main Builder Input Folder';
		end
		else if ( exists ( select 1 from SobekCM_Settings where Setting_Key = 'Main Builder Input Folder' and Setting_Value=@lastFolder ))
		begin
			-- One existed, pointed at the OLD network folder, so change it
			update SobekCM_Settings
			set Setting_Value = @NetworkFolder
			where Setting_Key = 'Main Builder Input Folder';
		end;
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Builder_Log_Search]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Procedure returns builder logs, by date range and/or by bibid_vid
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Builder_Log_Search]
	@startdate datetime,
	@enddate datetime,
	@bibid_vid varchar(20),
	@include_no_work_entries bit
AS
BEGIN

	-- Set the start date and end date if they are null
	if ( @startdate is null ) set @startdate = '1/1/2000';
	if ( @enddate is null ) set @enddate = dateadd(day, 1, getdate());

	-- If the @bibid_vid is NULL or empty, than this is only a date search
	if ( len(coalesce(@bibid_vid,'')) = 0 )
	begin
		-- Date search only needs to pay attention to the 'include no work' flag
		if ( @include_no_work_entries = 'true' )
		begin
			-- Just return all the date ranged rows
			select BuilderLogID, RelatedBuilderLogID, LogDate, coalesce(BibID_VID,'') as BibID_VID, coalesce(LogType,'') as LogType, coalesce(LogMessage,'') as LogMessage, SuccessMessage, METS_Type
			from SobekCM_Builder_Log
			where ( LogDate >= @startdate )
			  and ( LogDate <= @enddate )
			order by LogDate DESC;
		end
		else
		begin
			-- Only include the rows that are NOT 'No Work'
			select BuilderLogID, RelatedBuilderLogID, LogDate, coalesce(BibID_VID,'') as BibID_VID, coalesce(LogType,'') as LogType, coalesce(LogMessage,'') as LogMessage, SuccessMessage, METS_Type
			from SobekCM_Builder_Log
			where ( LogDate >= @startdate )
			  and ( LogDate <= @enddate )
			  and ( LogType != 'No Work' )
			order by LogDate DESC;
		end;
		return;
	end;

	-- Is this a LIKE search, or an exact search?
	if ( charindex('%', @bibid_vid ) > 0 )
	begin
		-- This is a LIKE expression
		select BuilderLogID, RelatedBuilderLogID, LogDate, coalesce(BibID_VID,'') as BibID_VID, coalesce(LogType,'') as LogType, coalesce(LogMessage,'') as LogMessage, SuccessMessage, METS_Type
		from SobekCM_Builder_Log
		where ( LogDate >= @startdate )
		  and ( LogDate <= @enddate )
		  and ( BibID_VID like @bibid_vid )
		order by LogDate DESC;
	end
	else
	begin
		-- This is an EXACT match
		select BuilderLogID, RelatedBuilderLogID, LogDate, coalesce(BibID_VID,'') as BibID_VID, coalesce(LogType,'') as LogType, coalesce(LogMessage,'') as LogMessage, SuccessMessage, METS_Type
		from SobekCM_Builder_Log
		where ( LogDate >= @startdate )
		  and ( LogDate <= @enddate )
		  and ( BibID_VID = @bibid_vid )
		order by LogDate DESC;
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Clear_Item_Error_Log]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/****** Object:  StoredProcedure [dbo].[SobekCM_Clear_Item_Error_Log]    Script Date: 12/20/2013 05:43:36 ******/
-- Marks items from the item error log as cleared, by date.  This does not actually
-- clear the item error completely, just marks the error as cleared so the history 
-- of the error log is maintained
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Clear_Item_Error_Log]
	@BibID varchar(10),
	@VID varchar(5),
	@ClearedBy varchar(100)
AS
BEGIN

	update SobekCM_Item_Error_Log
	set ClearedBy = @ClearedBy, ClearedDate=getdate()
	where BibID=@BibID and VID=@VID;
	
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Clear_Item_User_Group_Permissions]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Clear_Item_User_Group_Permissions]
	@ItemId int
AS
BEGIN
	delete from mySobek_User_Group_Item_Permissions
	where ItemID = @ItemId;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Clear_Item_User_Permissions]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Clear_Item_User_Permissions]
	@ItemId int
AS
BEGIN
	delete from mySobek_User_Item_Permissions
	where ItemID = @ItemId;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Clear_Old_Item_Info]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/****** Object:  StoredProcedure [dbo].[SobekCM_Clear_Old_Item_Info]    Script Date: 12/20/2013 05:43:36 ******/
-- Clears all the periphery data about an item in UFDC
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Clear_Old_Item_Info]
	@ItemID int
AS
begin

		-- Delete all lnks to georegion (if table exists)
		IF ( OBJECT_ID('SobekCM_Item_GeoRegion_Link') IS NOT NULL )
		BEGIN
			delete from SobekCM_Item_GeoRegion_Link where ItemID = @itemid;
		END;

		-- Deletes the immediate geographic footprint (if table exists)
		IF ( OBJECT_ID('SobekCM_Item_Footprint') IS NOT NULL )
		BEGIN
			delete from SobekCM_Item_Footprint where ItemID = @ItemID;
		END;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Coordinate_Points_By_Aggregation]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Gets the list of all point coordinates for a single aggregation
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Coordinate_Points_By_Aggregation]
	@aggregation_code varchar(20)
AS
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Return the groups/items/points
	with min_itemid_per_groupid as
	(
		-- Get the mininmum ItemID per group per coordinate point
		select GroupID, F.Point_Latitude, F.Point_Longitude, Min(I.ItemID) as MinItemID
		from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A, SobekCM_Item_Footprint F
		where ( I.ItemID = L.ItemID  )
		  and ( L.AggregationID = A.AggregationID )
		  and ( A.Code = @aggregation_code ) 
		  and ( F.ItemID = I.ItemID )
		  and ( F.Point_Latitude is not null )
		  and ( F.Point_Longitude is not null )
		group by GroupID, F.Point_Latitude, F.Point_Longitude
	), min_item_thumbnail_per_group as
	(
	    -- Get the matching item thumbnail for the item per group per coordiante point
		select G.GroupID, G.Point_Latitude, G.Point_Longitude, I.VID + '/' + I.MainThumbnail as MinThumbnail
		from SobekCM_Item I, min_itemid_per_groupid G
		where G.MinItemID = I.ItemID
	)
	-- Return all matchint group/coordinate point, with the group thumbnail, or item thumbnail from above WITH statements
	select F.Point_Latitude, F.Point_Longitude, G.BibID, G.GroupTitle, coalesce(NULLIF(G.GroupThumbnail,''), T.MinThumbnail) as Thumbnail, G.ItemCount, G.[Type]
	from SobekCM_Item_Group G, SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Footprint F, SobekCM_Item_Aggregation A, min_item_thumbnail_per_group T
	where ( G.GroupID = I.GroupID )
	  and ( I.ItemID = L.ItemID  )
	  and ( L.AggregationID = A.AggregationID )
	  and ( A.Code = @aggregation_code ) 
	  and ( F.ItemID = I.ItemID )
	  and ( F.Point_Latitude is not null )
	  and ( F.Point_Longitude is not null )
	  and ( T.GroupID = G.GroupID )
	  and ( T.Point_Latitude = F.Point_Latitude )
	  and ( T.Point_Longitude = F.Point_Longitude )
	group by I.Spatial_KML, F.Point_Latitude, F.Point_Longitude, G.BibID, G.GroupTitle, coalesce(NULLIF(G.GroupThumbnail,''), T.MinThumbnail), G.ItemCount, G.[Type]
	order by I.Spatial_KML;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Delete_Icon]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Delete an existing Wordmark, and output the number of links to that wordmark
-- If there are any items linked to that wordmark, the icon is not deleted
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Delete_Icon]
	@icon_code varchar(255),
	@links int output
AS
begin

	-- Get the number of links
	select @links = count(*) from SobekCM_Item_Icons L, SobekCM_Icon I where I.Icon_Name = @icon_code and I.IconID = L.IconID;

	-- If there are no links, delete this icon
	if ( @links = 0 )
	begin
		delete from SobekCM_Icon where Icon_Name = @icon_code;
	end;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Delete_IP_Range]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Delete_IP_Range]
	@rangeid int
AS
BEGIN
	UPDATE SobekCM_IP_Restriction_Range set Deleted='TRUE' where IP_RangeID=@rangeid;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Delete_Item]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Deletes an item, and deletes the group if there are no additional items attached
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Delete_Item] 
	@bibid varchar(10),
	@vid varchar(5),
	@as_admin bit,
	@delete_message varchar(1000)
AS
begin transaction
	-- Perform transactionally in case there is a problem deleting some of the rows
	-- so the entire delete is rolled back

   declare @itemid int;
   set @itemid = 0;

    -- first to get the itemid of the specified bibid and vid
   select @itemid = isnull(I.itemid, 0)
   from SobekCM_Item I, SobekCM_Item_Group G
   where (G.bibid = @bibid) 
       and (I.vid = @vid)
       and ( I.GroupID = G.GroupID );

   -- if there is such an itemid in the UFDC database, then delete this item and its related information
  if ( isnull(@itemid, 0 ) > 0)
  begin

	-- Delete all references to this item 
	delete from SobekCM_Item_Footprint where ItemID=@itemid;
	delete from SobekCM_Item_Icons where ItemID=@itemid;
	delete from SobekCM_Item_Statistics where ItemID=@itemid;
	delete from SobekCM_Item_GeoRegion_Link where ItemID=@itemid;
	delete from SobekCM_Item_Aggregation_Item_Link where ItemID=@itemid;
	delete from mySobek_User_Item where ItemID=@itemid;
	delete from mySobek_User_Item_Link where ItemID=@itemid;
	delete from mySobek_User_Description_Tags where ItemID=@itemid;
	delete from SobekCM_Item_Viewers where ItemID=@itemid;
	delete from Tracking_Item where ItemID=@itemid;
	delete from Tracking_Progress where ItemID=@itemid;
	delete from SobekCM_Item_OAI where ItemID=@itemid;
	delete from SobekCM_QC_Errors where ItemID=@itemid;
	delete from SobekCM_QC_Errors_History where ItemID=@itemid;
	delete from SobekCM_Item_Settings where ItemID=@itemid;
	
	-- Finally, delete the item 
	delete from SobekCM_Item where ItemID=@itemid;
	
	-- Delete the item group if it is the last one existing
	if (( select count(I.ItemID) from SobekCM_Item_Group G, SobekCM_Item I where ( G.BibID = @bibid ) and ( G.GroupID = I.GroupID ) and ( I.Deleted = 0 )) < 1 )
	begin
		
		declare @groupid int;
		set @groupid = 0;	
		
		-- first to get the itemid of the specified bibid and vid
		select @groupid = isnull(G.GroupID, 0)
		from SobekCM_Item_Group G
		where (G.bibid = @bibid);
		
		-- Delete if this selected something
		if ( ISNULL(@groupid, 0 ) > 0 )
		begin		
			-- delete from the item group table	and all references
			delete from SobekCM_Item_Group_External_Record where GroupID=@groupid;
			delete from SobekCM_Item_Group_Web_Skin_Link where GroupID=@groupid;
			delete from SobekCM_Item_Group_Statistics where GroupID=@groupid;
			delete from mySobek_User_Bib_Link where GroupID=@groupid;
			delete from SobekCM_Item_Group_OAI where GroupID=@groupid;
			delete from SobekCM_Item_Group where GroupID=@groupid;
		end;
	end
	else
	begin
		-- Finally set the volume count for this group correctly
		update SobekCM_Item_Group
		set ItemCount = ( select count(*) from SobekCM_Item I where ( I.GroupID = SobekCM_Item_Group.GroupID ))	
		where ( SobekCM_Item_Group.BibID = @bibid );
	end;
  end;
   
commit transaction;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Delete_Item_Aggregation]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO




-- Procedure to delete an item aggregation and unlink it completely.
-- This fails if there are any child aggregations.  This does not really
-- delete the item aggregation, just marks it as DELETED and removed most
-- references.  The statistics are retained.
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Delete_Item_Aggregation]
	@aggrcode varchar(20),
	@isadmin bit,
	@username varchar(100),
	@message varchar(1000) output,
	@errorcode int output
AS
BEGIN TRANSACTION
	-- Do not delete 'ALL'
	if ( @aggrcode = 'ALL' )
	begin
		-- Set the message and code
		set @message = 'Cannot delete the ALL aggregation.';
		set @errorcode = 3;
		return;	
	end;
	
	-- Only continue if the web skin code exists
	if (( select count(*) from SobekCM_Item_Aggregation where Code = @aggrcode ) > 0 )
	begin	
	
		-- Get the web skin code
		declare @aggrid int;
		select @aggrid=AggregationID from SobekCM_Item_Aggregation where Code = @aggrcode;
		
		-- Are there any children aggregations to this?
		if (( select COUNT(*) from SobekCM_Item_Aggregation_Hierarchy H, SobekCM_Item_Aggregation A where H.ParentID=@aggrid and A.AggregationID=H.ChildID and A.Deleted='false' ) > 0 )
		begin
			-- Set the message and code
			set @message = 'Item aggregation still has child aggregations';
			set @errorcode = 2;
		
		end
		else
		begin	
		
			-- How many items are still linked to the item aggregation?
			if (( @isadmin = 'false' ) and (( select count(*) from SobekCM_Item_Aggregation_Item_Link where AggregationID=@aggrid ) > 0 ))
			begin
					-- Set the message and code
				set @message = 'Only system admins can delete aggregations with digital resources';
				set @errorcode = 4;
			end
			else
			begin
		
				-- Set the message and error code initially
				set @message = 'Item aggregation removed';
				set @errorcode = 0;
			
				-- Delete the aggregations to users group links
				delete from mySobek_User_Group_Edit_Aggregation
				where AggregationID = @aggrid;
				
				-- Delete the aggregations to users links
				delete from mySobek_User_Edit_Aggregation
				where AggregationID = @aggrid;
				
				-- Delete links to any items
				--delete from SobekCM_Item_Aggregation_Item_Link
				--where AggregationID = @aggrid;
				
			
				-- Delete from the item aggregation aliases
				delete from SobekCM_Item_Aggregation_Alias
				where AggregationID = @aggrid;
				
				-- Delete the links to portals
				delete from SobekCM_Portal_Item_Aggregation_Link
				where AggregationID = @aggrid;
		
				-- Set the deleted flag
				update SobekCM_Item_Aggregation
				set Deleted = 'true', DeleteDate=getdate()
				where AggregationID = @aggrid;
				
				-- Add the milestone
				insert into SobekCM_Item_Aggregation_Milestones ( AggregationID, Milestone, MilestoneDate, MilestoneUser )
				values ( @aggrid, 'Deleted', getdate(), @username );
			
			end;
			
		end;
	end
	else
	begin
		-- Since there was no match, set an error code and message
		set @message = 'No matching item aggregation found';
		set @errorcode = 1;
	end;
COMMIT TRANSACTION;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Delete_Item_Aggregation_Alias]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Delete a single item aggregation alias (or forwarding) by alias
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Delete_Item_Aggregation_Alias]
	@alias varchar(50)
AS
BEGIN
	delete from SobekCM_Item_Aggregation_Alias
	where AggregationAlias = @alias;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Delete_Portal]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Delete an entire URL Portal, by URL portal ID
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Delete_Portal]
	@PortalID int
AS
BEGIN

	-- Remove anything linked to this one
	delete from SobekCM_Portal_Item_Aggregation_Link where PortalID=@PortalID;
	delete from SobekCM_Portal_Web_Skin_Link where PortalID=@PortalID;
	delete from SobekCM_Portal_URL_Statistics where PortalID=@PortalID;
	delete from SobekCM_Portal_URL where PortalID = @PortalID;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Delete_Project_Aggregation_Link]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Delete_Project_Aggregation_Link]
	@ProjectID int,
	@AggregationID int	
AS
Begin
  --If this link exists, delete it
  if((select count(*) from SobekCM_Project_Aggregation_Link  where ( ProjectID = @ProjectID and AggregationID=@AggregationID ))  = 1 )
    delete from SobekCM_Project_Aggregation_Link
    where (ProjectID=@ProjectID and AggregationID=@AggregationID);
End
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Delete_Project_DefaultMetadata_Link]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Delete_Project_DefaultMetadata_Link]
	@ProjectID int,
	@DefaultMetadataID int	
AS
Begin
  --If this link exists, delete it
  if((select count(*) from SobekCM_Project_DefaultMetadata_Link  where ( ProjectID = @ProjectID and DefaultMetadataID=@DefaultMetadataID ))  = 1 )
    delete from SobekCM_Project_DefaultMetadata_Link
    where (ProjectID=@ProjectID and DefaultMetadataID=@DefaultMetadataID);
End
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Delete_Project_Item_Link]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Delete_Project_Item_Link]
	@ProjectID int,
	@ItemID int	
AS
Begin
  --If this link exists, delete it
  if((select count(*) from SobekCM_Project_Item_Link  where ( ProjectID = @ProjectID and ItemID=@ItemID ))  = 1 )
    delete from SobekCM_Project_Item_Link
    where (ProjectID=@ProjectID and ItemID=@ItemID);
End
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Delete_Project_Template_Link]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Delete_Project_Template_Link]
	@ProjectID int,
	@TemplateID int	
AS
Begin
  --If this link exists, delete it
  if((select count(*) from SobekCM_Project_Template_Link  where ( ProjectID = @ProjectID and TemplateID=@TemplateID ))  = 1 )
    delete from SobekCM_Project_Template_Link
    where (ProjectID=@ProjectID and TemplateID=@TemplateID);
End
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Delete_Single_IP]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Delate a single IP, from a larger IP restriction range
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Delete_Single_IP]
	@ip_singleid int
AS
BEGIN
	
	delete from SobekCM_IP_Restriction_Single where IP_SingleID=@ip_singleid;

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Delete_Thematic_Heading]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Delete a single thematic heading, and unlink any aggregations currently
-- appearing under this thematic heading on the main library home page
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Delete_Thematic_Heading]
	@ThematicHeadingID int
AS
BEGIN

	-- Remove anything linked to this one
	update SobekCM_Item_Aggregation
	set ThematicHeadingID = -1 where ThematicHeadingID=@ThematicHeadingID;
	
	--Remove this from the list of thematic headings
	delete from SobekCM_Thematic_Heading
	where ThematicHeadingID=@ThematicHeadingID;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Delete_Web_Skin]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Procedure to delete a web skin, and unlink any items or web portals which
-- were linked to this web skin
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Delete_Web_Skin]
	@webskincode varchar(20),
	@force_delete bit,
	@links int output
AS
BEGIN

	-- set default links return value
	set @links = 0;
	
	-- Only continue if the web skin code exists
	if (( select count(*) from SobekCM_Web_Skin where WebSkinCode = @webskincode ) > 0 )
	begin	
	
		-- Get the web skin id, from the code
		declare @webskinid int;
		select @webskinid=WebSkinID from SobekCM_Web_Skin where WebSkinCode=@webskincode;	
	
		-- Should this force delete?
		if ( @force_delete = 'true' )
		begin	
		
			-- Delete the web skins to item group links
			delete from SobekCM_Item_Group_Web_Skin_Link 
			where WebSkinID=@webskinid;
			
			-- Delete the web skin links to URL portals
			delete from SobekCM_Portal_Web_Skin_Link 
			where WebSkinID=@webskinid;
			
			-- Remove any links to the item aggregation
			update SobekCM_Item_Aggregation
			set DefaultInterface = '' 
			where DefaultInterface = @webskincode;
			
			-- Delete the web skins themselves
			delete from SobekCM_Web_Skin
			where WebSkinID=@webskinid;		
		end
		else
		begin
			if ((( select count(*) from SobekCM_Item_Group_Web_Skin_Link where WebSkinID=@webskinid ) > 0 ) or
			    (( select count(*) from SobekCM_Portal_Web_Skin_Link where WebSkinID=@webskinid ) > 0 ) or
			    (( select count(*) from SobekCM_Item_Aggregation where DefaultInterface=@webskincode ) > 0 ))
			begin
				set @links = 1;
			end
			else
			begin
				-- Delete the web skins themselves, since no links found
				delete from SobekCM_Web_Skin
				where WebSkinID=@webskinid;					
			end;			
		end;
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Edit_IP_Range]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Edit basic information about an ip restriction range, or add a new range
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Edit_IP_Range]
	@rangeid int,
	@title nvarchar(150),
	@notes nvarchar(2000),
	@not_valid_statement nvarchar(max)
AS
begin

	-- Does this range id exist?
	if ( @rangeid in (select IP_RangeID from SobekCM_IP_Restriction_Range ))
	begin
		-- Range id existed, so update the existing IP range
		update SobekCM_IP_Restriction_Range
		set Title=@title, Notes=@notes, Not_Valid_Statement=@not_valid_statement
		where IP_RangeID = @rangeid;
	end
	else
	begin
		-- New range id, so add this IP range
		insert into SobekCM_IP_Restriction_Range ( Title, Notes, Not_Valid_Statement )
		values ( @title, @notes, @not_valid_statement );	
	end;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Edit_Portal]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Procedure to edit an existing URL portal or saving a new URL portal
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Edit_Portal]
	@PortalID int,
	@Base_URL nvarchar(150),
	@isActive bit,
	@isDefault bit,
	@Abbreviation nvarchar(10),
	@Name nvarchar(250),
	@Default_Aggregation nvarchar(20),
	@Base_PURL nvarchar(150),
	@Default_Web_Skin nvarchar(20),
	@NewID int output
AS
BEGIN TRANSACTION

	-- Is this a new portal?
	if (( select COUNT(*) from SobekCM_Portal_URL where PortalID=@PortalID ) = 0 )
	begin	
		-- Insert new portal
		insert into SobekCM_Portal_URL ( Abbreviation, isActive, isDefault, Name, Base_URL, Base_PURL )
		values ( @Abbreviation, @isActive, @isDefault, @Name, @Base_URL, @Base_PURL );
		
		-- Save the new id
		set @NewID = @@Identity;	
	end
	else
	begin
		-- update existing portal
		update SobekCM_Portal_URL
		set Abbreviation=@Abbreviation, isActive=@isActive, isDefault=@isDefault, Name=@Name, Base_URL=@Base_URL, Base_PURL=@Base_PURL
		where PortalID = @PortalID;
		
		-- Just return the same id
		set @NewID = @PortalID;	
	end;
	
	-- Clear any default aggregations and web skins
	delete from SobekCM_Portal_Item_Aggregation_Link where PortalID=@NewID;
	delete from SobekCM_Portal_Web_Skin_Link where PortalID=@NewID;

	-- Add the default aggregation, if one is chosen
	if ( LEN(isnull(@Default_Aggregation, '')) > 0 )
	begin
		-- Does this aggregation exists
		if (( select COUNT(*) from SobekCM_Item_Aggregation where Code=@Default_Aggregation ) = 1 )
		begin
			declare @aggrid int;
			select @aggrid=AggregationID from SobekCM_Item_Aggregation where Code=@Default_Aggregation;
			
			insert into SobekCM_Portal_Item_Aggregation_Link ( PortalID, AggregationID, isDefault )
			values ( @NewID, @aggrid, 'true' );		
		end;	
	end;	
	
	-- Add the web skin, if one is chosen
	if ( LEN(isnull(@Default_Web_Skin, '')) > 0 )
	begin
		-- Does this aggregation exists
		if (( select COUNT(*) from SobekCM_Web_Skin where WebSkinCode=@Default_Web_Skin ) = 1 )
		begin
			declare @skinid int;
			select @skinid=WebSkinID from SobekCM_Web_Skin where WebSkinCode=@Default_Web_Skin;
			
			insert into SobekCM_Portal_Web_Skin_Link ( PortalID, WebSkinID, isDefault )
			values ( @NewID, @skinid, 'true' );		
		end;	
	end;	
COMMIT TRANSACTION;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Edit_Single_IP]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Edits a single ip point within an entire IP restriction set of ranges, or
-- else adds a new ip point, if the provided ip_singleid is zero or less
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Edit_Single_IP]
	@ip_singleid int,
	@ip_rangeid int,
	@startip char(15),
	@endip char(15),
	@notes nvarchar(100),
	@new_ip_singleid int output
AS
BEGIN
	
	-- Was a primary key provided?
	if ( @ip_singleid > 0 )
	begin
	
		-- Update existing if there was one
		update SobekCM_IP_Restriction_Single
		set StartIP = @startip, EndIP = @endip, Notes=@notes
		where IP_SingleID = @ip_singleid;
		
		-- Return the existing ID
		set @new_ip_singleid = @ip_singleid;
	
	end
	else
	begin
	
		-- Insert new
		insert into SobekCM_IP_Restriction_Single ( IP_RangeID, StartIP, EndIP, Notes )
		values ( @ip_rangeid, @startip, @endip, @notes );
		
		-- Return the new primary key
		set @new_ip_singleid = @@identity;
	
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Edit_Thematic_Heading]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Edits the order and name for an existing themathic heading, or adds a new heading
-- if the provided thematic heading id is not valid
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Edit_Thematic_Heading]
	@ThematicHeadingID int,
	@ThemeOrder int,
	@ThemeName nvarchar(100),
	@NewID int output
AS
BEGIN

	-- Is this a new theme?  Does the thematic heading id exist?
	if ( @ThematicHeadingID in ( select ThematicHeadingID from SobekCM_Thematic_Heading ))
	begin	
		-- Yes, exists.. so update existing thematic heading
		update SobekCM_Thematic_Heading
		set ThemeOrder = @ThemeOrder, ThemeName = @ThemeName
		where ThematicHeadingID = @ThematicHeadingID
		
		-- Just return the same id
		set @NewID = @ThematicHeadingID;
	end
	else
	begin
		-- No, it doesn't exist, so insert a new thematic heading
		insert into SobekCM_Thematic_Heading ( ThemeOrder, ThemeName )
		values ( @ThemeOrder, @ThemeName );
		
		-- Save the new id
		set @NewID = @@Identity;
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Extensions_Add_Update]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add information about a new extension, or update an existing extension
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Extensions_Add_Update]
	@Code nvarchar(50),
	@Name nvarchar(255),
	@CurrentVersion varchar(50),
	@LicenseKey nvarchar(max),
	@UpgradeUrl nvarchar(255),
	@LatestVersion nvarchar(50)
AS
BEGIN
	-- Does this already exist?
	if ( exists ( select 1 from SobekCM_Extension where Code=@Code ))
	begin
		update SobekCM_Extension
		set Name=@Name,
			CurrentVersion=@CurrentVersion,
			LicenseKey=@LicenseKey,
			UpgradeUrl=@UpgradeUrl,
			LatestVersion=@LatestVersion
		where Code=@Code;    
	end
	else
	begin
		insert into SobekCM_Extension (Code, Name, CurrentVersion, IsEnabled, LicenseKey, UpgradeUrl, LatestVersion )
		values ( @Code, @Name, @CurrentVersion, 'false', @LicenseKey, @UpgradeUrl, @LatestVersion );
	end;
	
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Extensions_Get_All]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the list of extensions in the system
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Extensions_Get_All]
AS
BEGIN
	-- Return all the information about the extensions from the database
	select ExtensionID, Code, Name, CurrentVersion, IsEnabled, EnabledDate, LicenseKey, UpgradeUrl, LatestVersion 
	from SobekCM_Extension
	order by Code;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Extensions_Remove]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Remove an extension completely from the database
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Extensions_Remove]
	@Code nvarchar(50)
AS
BEGIN
	delete from SobekCM_Extension
	where Code=@Code;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Extensions_Set_Enable]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Extensions_Set_Enable]
	@Code nvarchar(50),
	@EnableFlag bit,
	@Message varchar(255) output
AS
BEGIN
	-- If the code is missing, do nothing
	if ( not exists ( select 1 from SobekCM_Extension where Code=@Code ))
	begin
		set @Message = 'ERROR: Unable to find matching extension in the database!';
		return;
	end;

	-- If the enable flag in the database is already set that way, do nothing
	if ( exists ( select 1 from SobekCM_Extension where Code=@Code and IsEnabled=@EnableFlag ))
	begin
		set @Message = 'Enabled flag was already set as requested for this plug-in';
		return;
	end;

	-- plug-in exists and flag is new
	if ( @EnableFlag = 'false' )
	begin
		update SobekCM_Extension set IsEnabled='false', EnabledDate=null where Code=@Code;
		set @Message='Disabled ' + @Code + ' plugin';
	end
	else
	begin
		update SobekCM_Extension set IsEnabled='true', EnabledDate=getdate() where Code=@Code;
		set @Message='Enabled ' + @Code + ' plugin';
	end;

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Aggregations_By_ProjectID]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Aggregations_By_ProjectID]
	@ProjectID int
AS
Begin
  
  select AggregationID from SobekCM_Project_Aggregation_Link
  where ProjectID=@ProjectID;
 End

GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_All_Groups]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the information about the ALL aggregation - standard fron home page collection
-- Written by Mark Sullivan (September 2005), Updated ( January 2010 )
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_All_Groups]
AS
begin 

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Create the temporary table variable
	declare @TEMP_CHILDREN_BUILDER table ( AggregationID int primary key, Code varchar(20), ParentCode varchar(20), Name nvarchar(255), ShortName nvarchar(100), [Type] nvarchar(50), HierarchyLevel int, isActive bit, Hidden bit );

	-- Get the aggregation id for 'all'
	declare @aggregationid int;
	
	-- Get the aggregation id
	select @aggregationid = AggregationID
	from SobekCM_Item_Aggregation AS C 
	where ( C.Code = 'all' );

	-- Determine when the last item was made available and if the new browse should display
	declare @last_added_date datetime;
	set @last_added_date = ( select MAX(MadePublicDate) from SobekCM_Item I where I.Dark='false' and I.IP_Restriction_Mask >= 0 and I.IncludeInAll='true');

	declare @has_new_items bit;
	set @has_new_items = 'false';
	if ( coalesce(@last_added_date, '1/1/1900' ) > DATEADD(day, -14, getdate()))
	begin
		set @has_new_items='true';
	end;
	
	-- Return information about this aggregation
	select AggregationID, Code, [Name], isnull(ShortName,[Name]) AS ShortName, [Type], isActive, Hidden, @has_new_items as HasNewItems,
	   ContactEmail, DefaultInterface, [Description], Map_Display, Map_Search, OAI_Flag, OAI_Metadata, DisplayOptions, 
	  coalesce(@last_added_date, '1/1/1900' ) as LastItemAdded, Can_Browse_Items, Items_Can_Be_Described, External_Link, GroupResults
	from SobekCM_Item_Aggregation AS C 
	where ( C.AggregationID=@aggregationid );
	
	-- Return the max/min of latitude and longitude - spatial footprint to cover all items with coordinate info
	select Min(F.Point_Latitude) as Min_Latitude, Max(F.Point_Latitude) as Max_Latitude, Min(F.Point_Longitude) as Min_Longitude, Max(F.Point_Longitude) as Max_Longitude
	from SobekCM_Item I, SobekCM_Item_Footprint F
	where ( F.ItemID = I.ItemID )
	  and ( F.Point_Latitude is not null )
	  and ( F.Point_Longitude is not null )
	  and ( I.Dark = 'false' );

	-- Return all of the key/value pairs of settings
	select Setting_Key, Setting_Value
	from SobekCM_Item_Aggregation_Settings 
	where AggregationID=@aggregationid;	
	
	-- Get the result views linked to this aggrgeation and save in a temp table
	select T.ResultType, A.DefaultView, A.ItemAggregationResultTypeID, ItemAggregationResultID, T.DefaultOrder
	into #ResultViews
	from SobekCM_Item_Aggregation_Result_Views A, SobekCM_Item_Aggregation_Result_Types T
	where A.AggregationID=@aggregationid
	  and A.ItemAggregationResultTypeID=T.ItemAggregationResultTypeID;

	-- return just the data needed
	select ResultType, DefaultView
	from #ResultViews	
	order by DefaultOrder ASC;
	
	-- Get the fields for the facets
	select F.MetadataTypeID, coalesce(F.OverrideFacetTerm, T.FacetTerm) as FacetTerm, T.SobekCode, T.SolrCode_Facets
	from SobekCM_Item_Aggregation_Facets F, SobekCM_Metadata_Types T
	where ( F.AggregationID = @aggregationid ) 
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	order by FacetOrder;

	-- Get the fields for the result fields (some may be customized at the aggregation level)
	select A.ResultType, F.MetadataTypeID, coalesce(F.OverrideDisplayTerm, T.DisplayTerm) as DisplayTerm, T.SobekCode, T.SolrCode_Display, F.DisplayOrder, 'Custom' as [Source]
	from SobekCM_Item_Aggregation_Result_Fields F, SobekCM_Metadata_Types T, #ResultViews A
	where ( A.ItemAggregationResultID = F.ItemAggregationResultID )
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	union
	select A.ResultType, F.MetadataTypeID, coalesce(F.OverrideDisplayTerm, T.DisplayTerm) as DisplayTerm, T.SobekCode, T.SolrCode_Display, F.DisplayOrder, 'Default' as [Source]
	from SobekCM_Item_Aggregation_Default_Result_Fields F, SobekCM_Metadata_Types T, #ResultViews A
	where ( A.ItemAggregationResultTypeID = F.ItemAggregationResultTypeID )
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	  and ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Fields X where A.ItemAggregationResultID = X.ItemAggregationResultID ))
	order by A.ResultType, DisplayOrder

	-- Drop the temp table
	drop table #ResultViews;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_All_IP_Restrictions]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_All_IP_Restrictions]
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Get all the IP information
	select R.Title, R.IP_RangeID, R.Not_Valid_Statement, isnull(S.StartIP,'') as StartIP, isnull(S.EndIP,'') as EndIP, coalesce(R.Notes,'') as Notes
	from SobekCM_IP_Restriction_Range AS R LEFT JOIN 
	     SobekCM_IP_Restriction_Single AS S ON R.IP_RangeID = S.IP_RangeID
	where R.Deleted = 'false'
	order by IP_RangeID ASC;

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_All_Portals]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get all of the portal information for this digital library
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_All_Portals]
	@activeonly bit
AS
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	if ( @activeonly = 'true' )
	begin
	
		select *
		from SobekCM_Portal_URL
		where isActive = 'true';
	
		select P.PortalID, A.Code
		from SobekCM_Portal_URL P, SobekCM_Portal_Item_Aggregation_Link AL, SobekCM_Item_Aggregation A
		where ( P.PortalID = AL.PortalID )
		  and ( AL.AggregationID = A.AggregationID )
		  and ( P.isActive = 'true' );
		  
		select P.PortalID, W.WebSkinCode
		from SobekCM_Portal_URL P, SobekCM_Portal_Web_Skin_Link WL, SobekCM_Web_Skin W
		where ( P.PortalID = WL.PortalID )
		  and ( WL.WebSkinID = W.WebSkinID )
		  and ( P.isActive = 'true' );
	end
	else
	begin
	
		select *
		from SobekCM_Portal_URL;
	
		select P.PortalID, A.Code
		from SobekCM_Portal_URL P, SobekCM_Portal_Item_Aggregation_Link AL, SobekCM_Item_Aggregation A
		where ( P.PortalID = AL.PortalID )
		  and ( AL.AggregationID = A.AggregationID );
		  
		select P.PortalID, W.WebSkinCode
		from SobekCM_Portal_URL P, SobekCM_Portal_Web_Skin_Link WL, SobekCM_Web_Skin W
		where ( P.PortalID = WL.PortalID )
		  and ( WL.WebSkinID = W.WebSkinID );
	
	end;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Available_OpenPublishing_Themes]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Available_OpenPublishing_Themes]
	@id int
AS
begin

	select ThemeID, ThemeName, Location, isnull(Author,'') as Author, isnull([Description],'') as [Description], isnull([Image], '') as [Image], AvailableForSelection, [Default]
	from SobekCM_OpenPublishing_Theme
	where AvailableForSelection='true';

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_BibID_VID_From_ItemID]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Allows a lookup of the BibID/VID for an item from the database's primary key.
-- This is used for legacy URLs which may reference the item by itemid.
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_BibID_VID_From_ItemID]
	@itemid int
as
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Return the item group / item information exactly like it is returned in the Item list brief procedure
	select G.BibID, I.VID
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID )
	  and ( G.Deleted = CONVERT(bit,0) )
	  and ( I.Deleted = CONVERT(bit,0) )
	  and ( I.ItemID = @itemid );

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Build_Error_Logs]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the build errors between two dates.  Due to the date comparison, the
-- second date should really be midnight on the NEXT day.  So, if you want all
-- the build errors between 1/1/2000 and 1/2/2000, the datetimes you should use
-- would be '1/1/2000' and '1/3/2000'.
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Build_Error_Logs]
	@firstdate datetime,
	@seconddate datetime
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Return the errors within the range, which were not cleared
	select BibID, VID, METS_Type=isnull(METS_Type,''), ErrorDescription=isnull(ErrorDescription,''), [Date]
	from SobekCM_Item_Error_Log
	where ( len(isnull(ClearedBy,'')) = 0 ) 
	  and ( ClearedDate is null )
      and ( [DATE] >= @firstdate )
	  and ( [Date] < @seconddate )
	order by [Date] DESC;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Codes]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Gets the lists of all item aggregation codes
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Codes]
AS
begin
	
	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Get the first aggregations
	SELECT Code, [Type], Name, ShortName=isnull(ShortName, Name), isActive, Hidden, AggregationID, [Description]=isnull([Description],''), ThematicHeadingID=isnull(ThematicHeadingID, -1 ), External_URL=ISNULL(External_Link,''), DateAdded
	FROM SobekCM_Item_Aggregation AS P
	WHERE Deleted = 'false'
	order by Code;
	
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Collection_Hierarchies]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Return the hierarchies for all (non-institutional) aggregations
-- starting with the 'ALL' aggregation
-- Version 3.05 - Added check to exclude DELETED aggregations
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Collection_Hierarchies]
as
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Get the aggregation id for ALL
	declare @aggregationid int;
	select @aggregationid=AggregationID from SobekCM_Item_Aggregation where Code='ALL';

	-- Create the temporary table
	create table #TEMP_CHILDREN_BUILDER (AggregationID int, Code varchar(20), ParentCode varchar(20), Name nvarchar(255), [Type] varchar(50), ShortName nvarchar(100), isActive bit, Hidden bit, Parent_Name nvarchar(255), Parent_ShortName nvarchar(100), HierarchyLevel int );
	
	-- Drive down through the children in the item aggregation hierarchy (first level below)
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, ParentCode='', C.[Name], C.[Type], isnull(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, '', '', -1
	from SobekCM_Item_Aggregation AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( P.AggregationID = @aggregationid )
	  and ( C.Deleted = 'false' )
	  and ( C.Type not like 'Institution%' );
	
	-- Now, try to find any children to this ( second level below )
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], isnull(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -2
	from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( HierarchyLevel = -1 )
	  and ( C.Deleted = 'false' );

	-- Now, try to find any children to this ( third level below )
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], isnull(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -3
	from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( HierarchyLevel = -2 )
	  and ( C.Deleted = 'false' );

	-- Now, try to find any children to this ( fourth level below )
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], isnull(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -4
	from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( HierarchyLevel = -3 ) 
	  and ( C.Deleted = 'false' );
	
	-- Now, try to find any children to this ( fifth level below )
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], isnull(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -5
	from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( HierarchyLevel = -4 )
	  and ( C.Deleted = 'false' );

	-- Return all the COLLECTION children
	select Code, ParentCode, [Name], [ShortName], [Type], HierarchyLevel, isActive, Hidden, Parent_Name, Parent_ShortName
	from #TEMP_CHILDREN_BUILDER
	order by HierarchyLevel DESC, Name;

	-- Clear the temp table
	truncate table #TEMP_CHILDREN_BUILDER;

		-- Drive down through the children in the item aggregation hierarchy (first level below)
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, ParentCode='', C.[Name], C.[Type], isnull(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, '', '', -1
	from SobekCM_Item_Aggregation AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( P.AggregationID = @aggregationid )
	  and ( C.Deleted = 'false' )
	  and ( C.Type like 'Institution%' );
	
	-- Now, try to find any children to this ( second level below )
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], isnull(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -2
	from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( HierarchyLevel = -1 )
	  and ( C.Deleted = 'false' );

	-- Now, try to find any children to this ( third level below )
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], isnull(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -3
	from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( HierarchyLevel = -2 )
	  and ( C.Deleted = 'false' );

	-- Now, try to find any children to this ( fourth level below )
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], isnull(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -4
	from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( HierarchyLevel = -3 ) 
	  and ( C.Deleted = 'false' );
	
	-- Now, try to find any children to this ( fifth level below )
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], isnull(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -5
	from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( HierarchyLevel = -4 )
	  and ( C.Deleted = 'false' );
	  	  
	-- Return all the INSTITUTION children
	select Code, ParentCode, [Name], [ShortName], [Type], HierarchyLevel, isActive, Hidden, Parent_Name, Parent_ShortName
	from #TEMP_CHILDREN_BUILDER
	order by HierarchyLevel DESC, Name;
	
	-- drop the temporary tables
	drop table #TEMP_CHILDREN_BUILDER;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Collection_Statistics_History]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Return the usage statistical information about a single item aggregation (or collection).
-- If the code is 'ALL', then the usage stats are aggregated up for all aggregations and
-- all items within this system.
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Collection_Statistics_History]
	@code varchar(20)
AS
BEGIN
	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Should this pull all the data for ALL collections?  This is a lot more work
	-- since data is not naturally aggregated up for ALL aggregations, but rather each
	-- individual aggregation.  The web application should be caching this by writing
	-- a small file, so that this is pulled only once a day or so...
	if (( len(@code) = 0 ) or ( @code = 'ALL' ))
	begin

		-- Pull all the statistical data by item
		select [Year], [Month], sum( Hits ) as Item_Hits,
			sum( JPEG_Views ) as Item_JPEG_Views, sum ( Zoomable_Views ) as Item_Zoomable_Views,
			sum ( Citation_Views ) as Item_Citation_Views, sum ( Thumbnail_Views ) as Item_Thumbnail_Views,
			sum ( Text_Search_Views ) as Item_Text_Search_Views, sum ( Flash_Views ) as Item_Flash_Views,
			sum ( Google_Map_Views) as Item_Google_Map_Views, sum( Download_Views ) as item_Download_Views,
			sum ( Static_Views) as Item_Static_Views
		into #TEMP_ITEM_STATS
		from SobekCM_Item_Statistics
		group by [Year], [Month];

		-- Pull all the statistical data by group
		select [Year], [Month], sum( Hits ) as Title_Hits
		into #TEMP_GROUP_STATS
		from SobekCM_Item_Group_Statistics
		group by [Year], [Month];

		-- Pull the collection statistical information
		select [Year], [Month], sum( Home_Page_Views ) as Home_Page_Views,
			sum( Browse_Views ) as Browse_Views, sum ( Advanced_Search_Views ) as Advanced_Search_Views,
			sum ( Search_Results_Views ) as Search_Results_Views
		into #TEMP_HIERARCHY_STATS
		from SobekCM_Item_Aggregation_Statistics
		group by [Year], [Month];

		-- Pull all the statistical overall data (could be multiple if we have two URLs)
		select [Year], [Month], sum( Hits ) as Hits, sum( [Sessions] ) as Sessions
		into #TEMP_URL_STATS
		from SobekCM_Statistics
		group by [Year], [Month];

		-- Return the data
		select T3.[Year], T3.[Month], Hits, [Sessions], [Home_Page_Views], [Browse_Views], [Advanced_Search_Views], [Search_Results_Views], [Title_Hits], [Item_Hits], Item_JPEG_Views, Item_Zoomable_Views, Item_Citation_Views, Item_Thumbnail_Views, Item_Text_Search_Views, Item_Flash_Views, Item_Google_Map_Views, Item_Download_Views, Item_Static_Views
		from #TEMP_HIERARCHY_STATS AS T3 LEFT OUTER JOIN
			 #TEMP_ITEM_STATS AS T1 ON (( T3.[Year] = T1.[Year] ) and ( T3.[Month] = T1.[Month] )) LEFT OUTER JOIN
			 #TEMP_URL_STATS AS T2 ON (( T3.[Year] = T2.[Year] ) and ( T3.[Month] = T2.[Month] )) LEFT OUTER JOIN
			 #TEMP_GROUP_STATS AS T4 ON (( T3.[Year] = T4.[Year] ) and ( T3.[Month] = T4.[Month] ))
		order by T3.[Year], T3.[Month];

		-- Drop the temporary tables
		drop table #TEMP_ITEM_STATS;
		drop table #TEMP_GROUP_STATS;
		drop table #TEMP_URL_STATS;
		drop table #TEMP_HIERARCHY_STATS;

	end
	else
	begin

		-- Since this is for a single aggregation, simply return the data from the 
		-- aggregation statistics table
		select [Year], [Month], Hits, [Sessions], [Home_Page_Views], [Browse_Views], [Advanced_Search_Views], [Search_Results_Views], [Title_Hits], [Item_Hits], Item_JPEG_Views, Item_Zoomable_Views, Item_Citation_Views, Item_Thumbnail_Views, Item_Text_Search_Views, Item_Flash_Views, Item_Google_Map_Views, Item_Download_Views, Item_Static_Views
		from SobekCM_Item_Aggregation_Statistics S, SobekCM_Item_Aggregation C
		where ( C.Code = @code )
		  and ( C.AggregationID = S.AggregationID )
		order by [Year], [Month];

	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Description_Tags_By_Aggregation]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns the list of any descriptive tags entered by users and
-- linked to an item aggregation.  If no code, or 'ALL', is passed in 
-- as the argument, then all descriptive tags are returned.
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Description_Tags_By_Aggregation]
	@aggregationcode varchar(20)
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- If the code has length and is not 'ALL', return the descriptive tags for that aggregation
	if (( len( @aggregationcode) > 0 ) and ( @aggregationcode != 'ALL' ))
	begin
		-- Return tags linked to that aggregation code
		select U.FirstName, U.NickName, U.LastName, G.BibID, I.VID, T.Description_Tag, T.TagID, T.Date_Modified, U.UserID
		from mySobek_User U, mySobek_User_Description_Tags T, SobekCM_Item I, SobekCM_Item_Group G, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A
		where ( I.ItemID = L.ItemID )
		  and ( L.AggregationID = A.AggregationID )
		  and ( A.Code = @aggregationcode )
		  and ( I.GroupID = G.GroupID )
		  and ( T.ItemID = I.ItemID )
		  and ( T.UserID = U.UserID )
		order by T.Date_Modified DESC;
	end
	else
	begin
		-- Return any descriptive tags
		select U.FirstName, U.NickName, U.LastName, G.BibID, I.VID, T.Description_Tag, T.TagID, T.Date_Modified, U.UserID
		from mySobek_User U, mySobek_User_Description_Tags T, SobekCM_Item I, SobekCM_Item_Group G
		where ( I.GroupID = G.GroupID )
		  and ( T.ItemID = I.ItemID )
		  and ( T.UserID = U.UserID )
		order by T.Date_Modified DESC;
	end;		  
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Email]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Gets an email from the email logging system, by the primary key for the Email.
-- This also includes any responses to this original email
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Email]
	@EmailID int
AS
begin
	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Get the original email
	select * from SobekCM_Email_Log where EmailID=@EmailID;
	
	-- Get any responses to this email	
	select * from SobekCM_Email_Log where ReplyToEmailID=@EmailID;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Email_List]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns the list of emails from the email logging system.  
-- @Include_All_Types - if TRUE, all emails returned, otherwise just the 'Contact Us' emails
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Email_List]
	@Include_All_Types bit,
	@Top100_Only bit
AS
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Should ALL types of emails be returned, or just the 'Contact Us' emails?
	if ( @Include_All_Types = 'true' )
	begin
		-- Should only the top 100 be returned?
		if ( @Top100_Only = 'true' )
		begin
			-- Return the top 100 emails of any type
			select top 100 EmailID, Sender, Receipt_List, Subject_Line, Sent_Date, SUBSTRING(Email_Body,0,500) as Preview, HTML_Format, Contact_Us, isnull(ReplyToEmailID, -1) as ReplyToEmailID 
			from SobekCM_Email_Log
			order by Sent_Date DESC;
		end
		else
		begin
			-- Return all emails of any type
			select EmailID, Sender, Receipt_List, Subject_Line, Sent_Date, SUBSTRING(Email_Body,0,500) as Preview, HTML_Format, Contact_Us, isnull(ReplyToEmailID, -1) as ReplyToEmailID 
			from SobekCM_Email_Log
			order by Sent_Date DESC;
		end;
	end
	else
	begin
		-- Should only the top 100 be returned?
		if ( @Top100_Only = 'true' )
		begin
			-- Return the top 100 'contact us' emails
			select top 100 EmailID, Sender, Receipt_List, Subject_Line, Sent_Date, SUBSTRING(Email_Body,0,500) as Preview, HTML_Format, Contact_Us, isnull(ReplyToEmailID, -1) as ReplyToEmailID 
			from SobekCM_Email_Log
			where Contact_Us = 'true'
			order by Sent_Date DESC;
		end
		else
		begin
			-- Return all 'contact us' emails
			select EmailID, Sender, Receipt_List, Subject_Line, Sent_Date, SUBSTRING(Email_Body,0,500) as Preview, HTML_Format, Contact_Us, isnull(ReplyToEmailID, -1) as ReplyToEmailID 
			from SobekCM_Email_Log
			where Contact_Us = 'true'
			order by Sent_Date DESC;
		end;
	end;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Group_Titles_All]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Group_Titles_All]
AS
BEGIN

	select G.BibID, coalesce(G.GroupTitle, '') as GroupTitle, coalesce(G.GroupThumbnail,'') as GroupThumbnail
	from SobekCM_Item_Group G
	where G.Deleted='false';

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_IP_Restriction_Range]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Return details on an IP restriction range, including all of the individual IPs included
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_IP_Restriction_Range]
	@ip_rangeid int
AS
BEGIN
	
	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Get all information (includes notes this time)
	select *
	from SobekCM_IP_Restriction_Range
	where IP_RangeID = @ip_rangeid;

	-- Get all associated single ip ranges
	select IP_SingleID, StartIP, ISNULL(EndIP,'') as EndIP, ISNULL(Notes,'') as Notes
	from SobekCM_IP_Restriction_Single
	where IP_RangeID = @ip_rangeid
	order by StartIP ASC;

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Item_Aggregation]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Gets all of the information about a single item aggregation
-- VErsion 5 - Stopped returning the metadata fields that have data (need to hit solr)
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Aggregation]
	@code varchar(20)
AS
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Create the temporary table
	create table #TEMP_CHILDREN_BUILDER (AggregationID int, Code varchar(20), ParentCode varchar(20), Name varchar(255), [Type] varchar(50), ShortName varchar(100), isActive bit, Hidden bit, HierarchyLevel int );
	
	-- Get the aggregation id
	declare @aggregationid int
	set @aggregationid = coalesce((select AggregationID from SobekCM_Item_Aggregation AS C where C.Code = @code and Deleted=0), -1 );

	-- Determine when the last item was made available and if the new browse should display
	declare @last_added_date datetime;
	set @last_added_date = ( select MAX(MadePublicDate) from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L where I.ItemID=L.ItemID and I.Dark='false' and I.IP_Restriction_Mask >= 0 and L.AggregationID=@aggregationid);

	declare @has_new_items bit;
	set @has_new_items = 'false';
	if ( coalesce(@last_added_date, '1/1/1900' ) > DATEADD(day, -14, getdate()))
	begin
		set @has_new_items='true';
	end;
	
	-- Return information about this aggregation
	select AggregationID, Code, [Name], coalesce(ShortName,[Name]) AS ShortName, [Type], isActive, Hidden, @has_new_items as HasNewItems,
	   ContactEmail, DefaultInterface, [Description], Map_Display, Map_Search, OAI_Flag, OAI_Metadata, DisplayOptions, coalesce(@last_added_date, '1/1/1900' ) as LastItemAdded, 
	   Can_Browse_Items, Items_Can_Be_Described, External_Link, T.ThematicHeadingID, LanguageVariants, ThemeName, GroupResults
	from SobekCM_Item_Aggregation AS C left outer join
	     SobekCM_Thematic_Heading as T on C.ThematicHeadingID=T.ThematicHeadingID 
	where C.AggregationID = @aggregationid;

	-- Drive down through the children in the item aggregation hierarchy (first level below)
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, HierarchyLevel )
	select C.AggregationID, C.Code, ParentCode=@code, C.[Name], C.[Type], coalesce(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, -1
	from SobekCM_Item_Aggregation AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( P.AggregationID = @aggregationid )
	  and ( C.Deleted = 'false' );
	
	-- Now, try to find any children to this ( second level below )
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], coalesce(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, -2
	from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
			SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( HierarchyLevel = -1 )
	  and ( C.Deleted = 'false' );

	-- Now, try to find any children to this ( third level below )
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], coalesce(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, -3
	from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
			SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( HierarchyLevel = -2 )
	  and ( C.Deleted = 'false' );

	-- Now, try to find any children to this ( fourth level below )
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], coalesce(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, -4
	from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
			SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( HierarchyLevel = -3 )
	  and ( C.Deleted = 'false' );

	-- Return all the children
	select Code, ParentCode, [Name], [ShortName], [Type], HierarchyLevel, isActive, Hidden
	from #TEMP_CHILDREN_BUILDER
	order by HierarchyLevel, Code ASC;
	
	-- drop the temporary tables
	drop table #TEMP_CHILDREN_BUILDER;
		
	-- Return all the parents 
	select Code, [Name], [ShortName], [Type], isActive
	from SobekCM_Item_Aggregation A, SobekCM_Item_Aggregation_Hierarchy H
	where A.AggregationID = H.ParentID 
	  and H.ChildID = @aggregationid
	  and A.Deleted = 'false';

	-- Return the max/min of latitude and longitude - spatial footprint to cover all items with coordinate info
	select Min(F.Point_Latitude) as Min_Latitude, Max(F.Point_Latitude) as Max_Latitude, Min(F.Point_Longitude) as Min_Longitude, Max(F.Point_Longitude) as Max_Longitude
	from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Footprint F
	where ( I.ItemID = L.ItemID  )
	  and ( L.AggregationID = @aggregationid )
	  and ( F.ItemID = I.ItemID )
	  and ( F.Point_Latitude is not null )
	  and ( F.Point_Longitude is not null )
	  and ( I.Dark = 'false' );

	-- Return all of the key/value pairs of settings
	select Setting_Key, Setting_Value
	from SobekCM_Item_Aggregation_Settings 
	where AggregationID=@aggregationid;

	-- Get the result views linked to this aggrgeation and save in a temp table
	select T.ResultType, A.DefaultView, A.ItemAggregationResultTypeID, ItemAggregationResultID, T.DefaultOrder
	into #ResultViews
	from SobekCM_Item_Aggregation_Result_Views A, SobekCM_Item_Aggregation_Result_Types T
	where A.AggregationID=@aggregationid
	  and A.ItemAggregationResultTypeID=T.ItemAggregationResultTypeID;

	-- return just the data needed
	select ResultType, DefaultView
	from #ResultViews	
	order by DefaultOrder ASC;
	
	-- Get the fields for the facets
	select F.MetadataTypeID, coalesce(F.OverrideFacetTerm, T.FacetTerm) as FacetTerm, T.SobekCode, T.SolrCode_Facets
	from SobekCM_Item_Aggregation_Facets F, SobekCM_Metadata_Types T
	where ( F.AggregationID = @aggregationid ) 
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	order by FacetOrder;

	-- Get the fields for the result fields (some may be customized at the aggregation level)
	select A.ResultType, F.MetadataTypeID, coalesce(F.OverrideDisplayTerm, T.DisplayTerm) as DisplayTerm, T.SobekCode, T.SolrCode_Display, F.DisplayOrder, 'Custom' as [Source]
	from SobekCM_Item_Aggregation_Result_Fields F, SobekCM_Metadata_Types T, #ResultViews A
	where ( A.ItemAggregationResultID = F.ItemAggregationResultID )
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	union
	select A.ResultType, F.MetadataTypeID, coalesce(F.OverrideDisplayTerm, T.DisplayTerm) as DisplayTerm, T.SobekCode, T.SolrCode_Display, F.DisplayOrder, 'Default' as [Source]
	from SobekCM_Item_Aggregation_Default_Result_Fields F, SobekCM_Metadata_Types T, #ResultViews A
	where ( A.ItemAggregationResultTypeID = F.ItemAggregationResultTypeID )
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	  and ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Fields X where A.ItemAggregationResultID = X.ItemAggregationResultID ))
	order by A.ResultType, DisplayOrder

	-- Drop the temp table
	drop table #ResultViews;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Item_Aggregation_Aliases]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Gets the list of all item aggregation aliases and what they forward to
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Aggregation_Aliases]
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Return all teh alias information
	select A.AggregationAliasID, A.AggregationAlias, C.Code
	from SobekCM_Item_Aggregation C, SobekCM_Item_Aggregation_Alias A
	where A.AggregationID = C.AggregationID
	order by AggregationAlias;
	
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Item_Aggregation_AllCodes]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Aggregation_AllCodes]
AS
begin
	
	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- First, get the aggregations
	SELECT P.Code, P.[Type], P.Name, ShortName=coalesce(P.ShortName, P.Name), P.isActive, P.Hidden, P.AggregationID, 
	       [Description]=coalesce(P.[Description],''), ThematicHeadingID=coalesce(T.ThematicHeadingID, -1 ),
		   External_URL=coalesce(P.External_Link,''), P.DateAdded, P.LanguageVariants, T.ThemeName, 
		   F.ShortName as ParentShortName, F.Name as ParentName, F.Code as ParentCode
	FROM SobekCM_Item_Aggregation AS P left outer join
	     SobekCM_Thematic_Heading as T on P.ThematicHeadingID=T.ThematicHeadingID left outer join
		 SobekCM_Item_Aggregation_Hierarchy as H on H.ChildID=P.AggregationID left outer join
		 SobekCM_Item_Aggregation as F on F.AggregationID=H.ParentID
	WHERE P.Deleted = 'false'
	  and ( F.Deleted = 'false' or F.Deleted is null )
	order by P.Code;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Item_Aggregation2]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Aggregation2]
	@code varchar(20),
	@include_counts bit,
	@is_robot bit
AS
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Create the temporary table
	create table #TEMP_CHILDREN_BUILDER (AggregationID int, Code varchar(20), ParentCode varchar(20), Name varchar(255), [Type] varchar(50), ShortName varchar(100), isActive bit, Hidden bit, HierarchyLevel int );
	
	-- Get the aggregation id
	declare @aggregationid int
	set @aggregationid = isnull((select AggregationID from SobekCM_Item_Aggregation AS C where C.Code = @code and Deleted=0), -1 );
	
	-- Return information about this aggregation
	select AggregationID, Code, [Name], isnull(ShortName,[Name]) AS ShortName, [Type], isActive, Hidden, HasNewItems,
	   ContactEmail, DefaultInterface, [Description], Map_Display, Map_Search, OAI_Flag, OAI_Metadata, DisplayOptions, LastItemAdded, 
	   Can_Browse_Items, Items_Can_Be_Described, External_Link, ThematicHeadingID
	from SobekCM_Item_Aggregation AS C 
	where C.AggregationID = @aggregationid;

	-- Drive down through the children in the item aggregation hierarchy (first level below)
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, HierarchyLevel )
	select C.AggregationID, C.Code, ParentCode=@code, C.[Name], C.[Type], isnull(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, -1
	from SobekCM_Item_Aggregation AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( P.AggregationID = @aggregationid );
	
	-- If this is a robot, no need to go further
	if ( @is_robot = 'false' )
	begin

		-- Now, try to find any children to this ( second level below )
		insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, HierarchyLevel )
		select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], isnull(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, -2
		from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
			 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
		where ( HierarchyLevel = -1 );

		-- Now, try to find any children to this ( third level below )
		insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, HierarchyLevel )
		select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], isnull(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, -3
		from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
			 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
		where ( HierarchyLevel = -2 ); 

		-- Now, try to find any children to this ( fourth level below )
		insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, HierarchyLevel )
		select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], isnull(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, -4
		from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
			 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
		where ( HierarchyLevel = -3 );
	end;

	-- Return all the children
	select Code, ParentCode, [Name], [ShortName], [Type], HierarchyLevel, isActive, Hidden
	from #TEMP_CHILDREN_BUILDER
	order by HierarchyLevel, Code ASC;
	
	-- drop the temporary tables
	drop table #TEMP_CHILDREN_BUILDER;
	
	-- Check to see if the counts should be included
	if ( @include_counts = 'true' )
	begin
		-- Return some counts as well
		select count(distinct(I.GroupID)) as Title_Count, count(*) as Item_Count, isnull(SUM([PageCount]),0) as Page_Count
		from SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item I, SobekCM_Item_Aggregation A
		where ( A.Code = @code )
		  and ( A.AggregationID = L.AggregationID )
		  and ( L.ItemID = I.ItemID );
	end;
	
	-- Return all the parents (if not robot)
	if ( @is_robot = 'false' )
	begin
		select Code, [Name], [ShortName], [Type], isActive
		from SobekCM_Item_Aggregation A, SobekCM_Item_Aggregation_Hierarchy H
		where A.AggregationID = H.ParentID 
		  and H.ChildID = @aggregationid;	
	end;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Item_Brief_Info]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get some basic information about an item, which is pulled from the database before the item
-- is displayed online.  Many of these values correspond to the item group/title or how this
-- item relates to the item group and any item aggregations within the system.
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Brief_Info]
	@bibid varchar(10),
	@vid varchar(5),
	@include_aggregations bit
as
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Return the item group / item information exactly like it is returned in the Item list brief procedure
	select G.BibID, I.VID, G.GroupTitle, 
			isnull(I.Level1_Text, '') as Level1_Text, isnull( I.Level1_Index, 0 ) as Level1_Index, 
			isnull(I.Level2_Text, '') as Level2_Text, isnull( I.Level2_Index, 0 ) as Level2_Index, 
			isnull(I.Level3_Text, '') as Level3_Text, isnull( I.Level3_Index, 0 ) as Level3_Index, 
			PubDate=isnull(I.PubDate,''), SortDate=isnull( I.SortDate,-1), MainThumbnail=G.File_Location + '/' + VID + '/' + isnull( I.MainThumbnail,''), 
			I.Title, Author=isnull(I.Author,''), IP_Restriction_Mask, G.OCLC_Number, G.ALEPH_Number, MainThumbnailFile=ISNULL(I.MainThumbnail,''), MainJPEGFile=ISNULL(I.MainJPEG,'')
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID )
	  and ( G.BibID = @bibid )
	  and ( I.VID = @vid );  
	  
	-- Check to see if aggregation information should be returned
	if( @include_aggregations = 'true' )
	begin
  		-- Return the aggregation information linked to this item
		select A.Code, A.Name, A.ShortName, A.[Type], A.Map_Search, A.DisplayOptions, A.Items_Can_Be_Described, L.impliedLink, A.Hidden, A.isActive, ISNULL(A.External_Link,'') as External_Link
		from SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A, SobekCM_Item I, SobekCM_Item_Group G
		where ( L.ItemID = I.ItemID )
		  and ( A.AggregationID = L.AggregationID )
		  and ( I.GroupID = G.GroupID )
	      and ( G.BibID = @bibid )
	      and ( I.VID = @vid );
	end;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Item_Details]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO




-- Pull any additional item details for one bib/vid before showing this item
-- Ver 5: Split the old SobekCM_Get_Item_Details2 stored procedure
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Details]
	@BibID varchar(10),
	@VID varchar(5)
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Does this BIbID exist?
	if (not exists ( select 1 from SobekCM_Item_Group where BibID = @BibID ))
	begin
		select 'INVALID BIBID' as ErrorMsg, '' as BibID, '' as VID;
		return;
	end;

		-- Does this VID exist in that stored procedure?
		if ( not exists ( select 1 from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID=@BibID and I.VID = @VID ))
		begin

			select top 1 'INVALID VID' as ErrorMsg, @BibID as BibID, VID
			from SobekCM_Item I, SobekCM_Item_Group G
			where I.GroupID = G.GroupID 
			  and G.BibID = @BibID
			order by VID;

			return;
		end;
	
		-- Only continue if there is ONE match
		if (( select COUNT(*) from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = @BibID and I.VID = @VID ) = 1 )
		begin
			-- Get the itemid
			declare @ItemID int;
			select @ItemID = ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = @BibID and I.VID = @VID;

			-- Return any descriptive tags
			select U.FirstName, U.NickName, U.LastName, G.BibID, I.VID, T.Description_Tag, T.TagID, T.Date_Modified, U.UserID, isnull([PageCount], 0) as Pages, ExposeFullTextForHarvesting
			from mySobek_User U, mySobek_User_Description_Tags T, SobekCM_Item I, SobekCM_Item_Group G
			where ( T.ItemID = @ItemID )
			  and ( I.ItemID = T.ItemID )
			  and ( I.GroupID = G.GroupID )
			  and ( T.UserID = U.UserID );
			
			-- Return the aggregation information linked to this item
			select A.Code, A.Name, A.ShortName, A.[Type], A.Map_Search, A.DisplayOptions, A.Items_Can_Be_Described, L.impliedLink, A.Hidden, A.isActive, ISNULL(A.External_Link,'') as External_Link
			from SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A
			where ( L.ItemID = @ItemID )
			  and ( A.AggregationID = L.AggregationID );
		  
			-- Return information about the actual item/group
			select G.BibID, I.VID, G.File_Location, G.SuppressEndeca, 'true' as [Public], I.IP_Restriction_Mask, G.GroupID, I.ItemID, I.CheckoutRequired, Total_Volumes=(select COUNT(*) from SobekCM_Item J where G.GroupID = J.GroupID ),
				isnull(I.Level1_Text, '') as Level1_Text, isnull( I.Level1_Index, 0 ) as Level1_Index, 
				isnull(I.Level2_Text, '') as Level2_Text, isnull( I.Level2_Index, 0 ) as Level2_Index, 
				isnull(I.Level3_Text, '') as Level3_Text, isnull( I.Level3_Index, 0 ) as Level3_Index,
				G.GroupTitle, I.TextSearchable, Comments=isnull(I.Internal_Comments,''), Dark, G.[Type],
				I.Title, I.Publisher, I.Author, I.Donor, I.PubDate, G.ALEPH_Number, G.OCLC_Number, I.Born_Digital, 
				I.Disposition_Advice, I.Material_Received_Date, I.Material_Recd_Date_Estimated, I.Tracking_Box, I.Disposition_Advice_Notes, 
				I.Left_To_Right, I.Disposition_Notes, G.Track_By_Month, G.Large_Format, G.Never_Overlay_Record, I.CreateDate, I.SortDate, 
				G.Primary_Identifier_Type, G.Primary_Identifier, G.[Type] as GroupType, coalesce(I.MainThumbnail,'') as MainThumbnail,
				T.EmbargoEnd, coalesce(T.UMI,'') as UMI, T.Original_EmbargoEnd, coalesce(T.Original_AccessCode,'') as Original_AccessCode,
				I.CitationSet, I.MadePublicDate, I.RestrictionMessage
			from SobekCM_Item as I inner join
				 SobekCM_Item_Group as G on G.GroupID=I.GroupID left outer join
				 Tracking_Item as T on T.ItemID=I.ItemID
			where ( I.ItemID = @ItemID );
		  		
			-- Return the viewers for this item
			select T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder) as MenuOrder, V.Exclude, coalesce(V.OrderOverride, T.[Order])
			from SobekCM_Item_Viewers V, SobekCM_Item_Viewer_Types T
			where ( V.ItemID = @ItemID )
			  and ( V.ItemViewTypeID = T.ItemViewTypeID )
			group by T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder), V.Exclude, coalesce(V.OrderOverride, T.[Order])
			order by coalesce(V.OrderOverride, T.[Order]) ASC;
				
			-- Return the icons for this item
			select Icon_URL, Link, Icon_Name, I.Title
			from SobekCM_Icon I, SobekCM_Item_Icons L
			where ( L.IconID = I.IconID ) 
			  and ( L.ItemID = @ItemID )
			order by Sequence;
			  
			-- Return any web skin restrictions
			select S.WebSkinCode
			from SobekCM_Item_Group_Web_Skin_Link L, SobekCM_Item I, SobekCM_Web_Skin S
			where ( L.GroupID = I.GroupID )
			  and ( L.WebSkinID = S.WebSkinID )
			  and ( I.ItemID = @ItemID )
			order by L.Sequence;

			-- Return all of the key/value pairs of settings
			select Setting_Key, Setting_Value
			from SobekCM_Item_Settings 
			where ItemID=@ItemID;

			-- Return any special user group restriction information
			select I.UserGroupID, G.GroupName, I.canView, I.isOwner, I.canEditMetadata, I.canEditBehaviors, I.canPerformQc, I.canUploadFiles, I.canChangeVisibility, I.canDelete, I.customPermissions
			from mySobek_User_Group_Item_Permissions I, mySobek_User_Group G
			where G.UserGroupID=I.UserGroupID
			  and ItemID=@ItemID;

			-- Return any special user restriction information
			select I.UserID, U.UserName, U.UserID, I.canView, I.isOwner, I.canEditMetadata, I.canEditBehaviors, I.canPerformQc, I.canUploadFiles, I.canChangeVisibility, I.canDelete, I.customPermissions
			from mySobek_User_Item_Permissions I, mySobek_User U
			where U.UserID=I.UserID
			  and ItemID=@ItemID;

		end;		

		
	-- Get the list of related item groups
	select B.BibID, B.GroupTitle, R.Relationship_A_to_B AS Relationship
	from SobekCM_Item_Group A, SobekCM_Item_Group_Relationship R, SobekCM_Item_Group B
	where ( A.BibID = @bibid ) 
	  and ( R.GroupA = A.GroupID )
	  and ( R.GroupB = B.GroupID )
	union
	select A.BibID, A.GroupTitle, R.Relationship_B_to_A AS Relationship
	from SobekCM_Item_Group A, SobekCM_Item_Group_Relationship R, SobekCM_Item_Group B
	where ( B.BibID = @bibid ) 
	  and ( R.GroupB = B.GroupID )
	  and ( R.GroupA = A.GroupID );
		  
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Item_Group_Details]    Script Date: 7/25/2026 6:59:31 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Pull any additional item details for a bib before showing this title
-- Ver 5: Split the old SobekCM_Get_Item_Details2 stored procedure
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Group_Details]
	@BibID varchar(10)
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Does this BIbID exist?
	if (not exists ( select 1 from SobekCM_Item_Group where BibID = @BibID ))
	begin
		select 'INVALID BIBID' as ErrorMsg, '' as BibID, '' as VID;
		return;
	end;

		-- Return the aggregation information linked to these items
		select GroupTitle, BibID, G.[Type], G.File_Location, isnull(AGGS.Code,'') AS Code, G.GroupID, isnull(GroupThumbnail,'') as Thumbnail, G.Track_By_Month, G.Large_Format, G.Never_Overlay_Record, G.Primary_Identifier_Type, G.Primary_Identifier
		from SobekCM_Item_Group AS G LEFT JOIN
			 ( select distinct(A.Code),  G2.GroupID
			   from SobekCM_Item_Group G2, SobekCM_Item IL, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A
		       where IL.ItemID=L.ItemID 
		         and A.AggregationID=L.AggregationID
		         and G2.GroupID=IL.GroupID
		         and G2.BibID=@BibID
		         and G2.Deleted='false'
		       group by A.Code, G2.GroupID ) AS AGGS ON G.GroupID=AGGS.GroupID
		where ( G.BibID = @BibID )
		  and ( G.Deleted = 'false' );

		-- Get list of icon ids
		select distinct(IconID)
		into #TEMP_ICON
		from SobekCM_Item_Icons II, SobekCM_Item It, SobekCM_Item_Group G
		where ( It.GroupID = G.GroupID )
			and ( G.BibID = @bibid )
			and ( It.Deleted = 0 )
			and ( II.ItemID = It.ItemID )
		group by IconID;

		-- Return icons
		select Icon_URL, Link, Icon_Name, Title
		from SobekCM_Icon I, (	select distinct(IconID)
								from SobekCM_Item_Icons II, SobekCM_Item It, SobekCM_Item_Group G
								where ( It.GroupID = G.GroupID )
							 	  and ( G.BibID = @bibid )
								  and ( It.Deleted = 0 )
								  and ( II.ItemID = It.ItemID )
								group by IconID) AS T
		where ( T.IconID = I.IconID );
		
		-- Return any web skin restrictions
		select S.WebSkinCode
		from SobekCM_Item_Group_Web_Skin_Link L, SobekCM_Item_Group G, SobekCM_Web_Skin S
		where ( L.GroupID = G.GroupID )
		  and ( L.WebSkinID = S.WebSkinID )
		  and ( G.BibID = @BibID )
		order by L.Sequence;
		
		-- Get the distinct list of all aggregations linked to this item
		select distinct( Code )
		from SobekCM_Item_Aggregation A, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Group G, SobekCM_Item I
		where ( I.ItemID = L.ItemID )
		  and ( I.GroupID = G.GroupID )
		  and ( G.BibID = @BibID )
		  and ( L.AggregationID = A.AggregationID );		

		
	-- Get the list of related item groups
	select B.BibID, B.GroupTitle, R.Relationship_A_to_B AS Relationship
	from SobekCM_Item_Group A, SobekCM_Item_Group_Relationship R, SobekCM_Item_Group B
	where ( A.BibID = @bibid ) 
	  and ( R.GroupA = A.GroupID )
	  and ( R.GroupB = B.GroupID )
	union
	select A.BibID, A.GroupTitle, R.Relationship_B_to_A AS Relationship
	from SobekCM_Item_Group A, SobekCM_Item_Group_Relationship R, SobekCM_Item_Group B
	where ( B.BibID = @bibid ) 
	  and ( R.GroupB = B.GroupID )
	  and ( R.GroupA = A.GroupID );
		  
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Item_Restrictions]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Restrictions]
	@bibid varchar(10),
	@vid varchar(5)
AS
BEGIN
	select IP_Restriction_Mask, Dark
	from SObekCM_Item I, SobekCM_Item_Group G
	where ( I.VID = @vid )
	  and ( I.GroupID = G.GroupID )
	  and ( G.BibID=@bibid)
	  and ( I.Deleted = 'false' )
	  and ( G.Deleted = 'false' );
END
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Item_Statistics]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Pull any additional item details before showing this item
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Statistics]
	@BibID varchar(10),
	@VID varchar(5)
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Get the item id
	declare @itemid int;
	set @itemid = coalesce( ( select I.ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID=G.GroupID and I.VID=@vid and G.BibiD=@bibid ), -1 );

	-- Get the item id
	declare @groupid int;
	set @groupid = coalesce( ( select G.GroupID from SobekCM_Item_Group G where G.BibiD=@bibid ), -1 );

	with item_month_years ([Month], [Year]) as 
	(
		select [Month], [Year] from SobekCM_Item_Group_Statistics G where G.GroupID=@groupID
		union
		select [Month], [Year] from SobekCM_Item_Statistics I where I.ItemID=@itemid
	)
	select M.[Year], M.[Month], coalesce(G.Hits,0) as Title_Views, coalesce(G.[Sessions],0) as Title_Visitors, coalesce(I.Hits,0) as [Views], coalesce(I.[Sessions],0) as Visitors
	from item_month_years M left outer join
		 SobekCM_Item_Statistics as I on I.[Month]=M.[Month] and I.[Year]=M.[Year] and I.ItemID=@itemid left outer join
		 SobekCM_Item_Group_Statistics as G on M.[Month]=G.[Month] and M.[Year]=G.[Year] and G.GroupID=@groupid
	order by [Year] ASC, [Month] ASC;			  
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Item_Viewers]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Viewers]
	@bibid varchar(10),
	@vid varchar(5)
as
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Return the current viewer information
	select T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder) as MenuOrder, V.Exclude, coalesce(V.OrderOverride, T.[Order]) as [Order]
	from SobekCM_Item_Viewers V, SobekCM_Item_Viewer_Types T, SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID )
	  and ( G.BibID = @bibid )
	  and ( I.VID = @vid )
	  and ( V.ItemID = I.ItemID )
	  and ( V.ItemViewTypeID = T.ItemViewTypeID )
	group by T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder), V.Exclude, coalesce(V.OrderOverride, T.[Order])
	order by coalesce(V.OrderOverride, T.[Order]) ASC;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_ItemID]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Procedure returns the item id as a single row given the bibid and vid.
-- This also doubles as a quick way to check if a certain item exists in
-- the database and is employed by some of the workflow tools
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_ItemID]
	@bibid varchar(10),
	@vid varchar(5)
AS
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Return the item id as a single row relation
	select ItemID
	from SobekCM_Item I, SobekCM_Item_Group G
	where I.GroupID=G.GroupID 
	  and I.VID=@vid
	  and G.BibID=@bibid;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Items_Needing_Aditional_Work]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Gets the list of itesm currently flagged for needing additional work.
-- This is used by the builder to determine what needs post-processing.
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Items_Needing_Aditional_Work]
as
begin

	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Return the bibid, vid, and primary key to the items which are flagged
	select G.BibID, I.VID, I.ItemID
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID )
	  and ( I.AdditionalWorkNeeded = 'true' )
	order by BibID, VID;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Last_Open_Workflow_By_ItemID]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Last_Open_Workflow_By_ItemID]
	@ItemID int,
	@EventNumber int
AS
BEGIN

	-- Get the workflow id
	declare @workflowid int;
	set @workflowid = coalesce((select WorkFlowID from Tracking_Workflow where Start_Event_Number = @EventNumber or End_Event_Number = @EventNumber ), -1);
	
	-- If there is a match continue
	if ( @workflowid > 0 )
	begin
	
		select P.ItemID,P.ProgressID, W.WorkFlowName, W.Start_Event_Desc, W.End_Event_Desc, W.Start_Event_Number, W.End_Event_Number, W.Start_And_End_Event_Number,
		       P.DateStarted, P.DateCompleted, P.RelatedEquipment, P.WorkPerformedBy, P.WorkingFilePath, P.ProgressNote
		from Tracking_Progress P, Tracking_Workflow W
		where ItemID = @ItemID
		  and P.WorkFlowID = @workflowid
		  and P.WorkFlowID = W.WorkFlowID
		  and ( DateCompleted is null );
		  
	
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Metadata_Fields]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Return the list of all metadata searchable fields
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Metadata_Fields]
AS
begin

	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Just return all the values, but sort by display term
	select * 
	from SobekCM_Metadata_Types
	order by DisplayTerm;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Mime_Types]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Mime_Types]
AS
BEGIN
	select Extension, MimeType, isBlocked, shouldForward, MimeTypeID
	from SobekCM_Mime_Types;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Multiple_Volumes]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Stored procedure returns the information about all the items within a single 
-- title or item/group
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Multiple_Volumes] 
	@bibid varchar(10)
AS
begin

	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Return the individual volumes
	select I.ItemID, Title, Level1_Text=isnull(Level1_Text,''), Level1_Index=isnull(Level1_Index,-1), Level2_Text=isnull(Level2_Text, ''), Level2_Index=isnull(Level2_Index, -1), Level3_Text=isnull(Level3_Text, ''), Level3_Index=isnull(Level3_Index, -1), Level4_Text=isnull(Level4_Text, ''), Level4_Index=isnull(Level4_Index, -1), Level5_Text=isnull(Level5_Text, ''), Level5_Index=isnull(Level5_Index,-1), I.MainThumbnail, I.VID, I.IP_Restriction_Mask
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( G.GroupID = I.GroupID )
	  and ( G.BibID = @bibid )
	  and ( I.Deleted = 'false' )
	  and ( G.Deleted = 'false' )
	order by Level1_Index ASC, Level2_Index ASC, Level3_Index ASC, Level4_Index ASC, Level5_Index ASC, Title ASC;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_MultiVolume_Title_Info]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Gets the information about all the multi-volume titles
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_MultiVolume_Title_Info] 
AS
begin

	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Return the multiple volumes
	with volume_count as 
	(
		select BibID, count(*) as ItemCount
		from SobekCM_Item_Group G, SobekCM_Item I
		where G.GroupID = I.GroupID 
		  and G.Deleted='false'
		  and I.Deleted='false'
		group by BibID
	)
	select G.BibID, CustomThumbnail, FlagByte, LastFourInt, coalesce(GroupTitle,'') as GroupTitle
	from SobekCM_Item_Group G, volume_count C
	where ( C.BibID=G.BibID )
	  and (( C.ItemCount > 1 ) or ( G.HasGroupMetadata = 'true' ));

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_OAI_Data]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Return a list of the OAI data to server through the OAI-PMH server
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_OAI_Data]
	@aggregationcode varchar(20),
	@data_code varchar(20),
	@from date,
	@until date,
	@pagesize int, 
	@pagenumber int,
	@include_data bit
AS
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Do not need to maintain row counts
	SET NoCount ON;

	-- Create the temporary tables first
	-- Create the temporary table to hold all the item id's
		
	-- Determine the start and end rows
	declare @rowstart int;
	declare @rowend int; 
	set @rowstart = (@pagesize * ( @pagenumber - 1 )) + 1;
	
	-- Rowend is calculated normally, but then an additional item is
	-- added at the end which will be used to determine if a resumption
	-- token should be issued
	set @rowend = (@rowstart + @pagesize - 1) + 1; 
	
	-- Ensure there are date values
	if ( @from is null )
		set @from = CONVERT(date,'19000101');
	if ( @until is null )
		set @until = GETDATE();
	
	-- Is this for a single aggregation
	if (( @aggregationcode is not null ) and ( LEN(@aggregationcode) > 0 ) and ( @aggregationcode != 'all' ))
	begin	
		-- Determine the aggregationid
		declare @aggregationid int;
		set @aggregationid = ( select ISNULL(AggregationID,-1) from SobekCM_Item_Aggregation where Code=@aggregationcode );
			  
		-- Should the actual data be returned, or just the identifiers?
		if ( @include_data='true')
		begin
			-- Create saved select across items/title for row numbers
			with ITEMS_SELECT AS
			(	select BibID, I.ItemID, VID,
				ROW_NUMBER() OVER (order by O.OAI_Date ASC ) as RowNumber
				from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link CL, SobekCM_Item_Group G, SobekCM_Item_OAI O
				where ( CL.ItemID = I.ItemID )
				  and ( CL.AggregationID = @aggregationid )
				  and ( I.GroupID = G.GroupID )
				  and ( I.ItemID = O.ItemID )
				  and ( G.Suppress_OAI = 'false' )
				  and ( O.OAI_Date >= @from )
				  and ( O.OAI_Date <= @until )
				  and ( O.Data_Code = @data_code )
				  and ( I.Dark = 'false' )
				  and ( I.IP_Restriction_Mask = 0 )
			)
			-- Select the matching rows
			select BibID, T.VID, O.OAI_Data, O.OAI_Date
			from ITEMS_SELECT T, SobekCM_Item_OAI O
			where RowNumber >= @rowstart
			  and RowNumber <= @rowend
			  and T.ItemID = O.ItemID			  
			  and O.Data_Code = @data_code;		 
		end
		else
		begin
			-- Create saved select across titles for row numbers
			with ITEMS_SELECT AS
			(	select BibID, I.ItemID, VID,
				ROW_NUMBER() OVER (order by O.OAI_Date ASC ) as RowNumber
				from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link CL, SobekCM_Item_Group G, SobekCM_Item_OAI O
				where ( CL.ItemID = I.ItemID )
				  and ( CL.AggregationID = @aggregationid )
				  and ( I.GroupID = G.GroupID )
				  and ( I.ItemID = O.ItemID )
				  and ( G.Suppress_OAI = 'false' )
				  and ( O.OAI_Date >= @from )
				  and ( O.OAI_Date <= @until )
				  and ( O.Data_Code = @data_code )
				  and ( I.Dark = 'false' )				  
				  and ( I.IP_Restriction_Mask = 0 )
			)				
			-- Select the matching rows
			select BibID, T.VID, O.OAI_Date
			from ITEMS_SELECT T, SobekCM_Item_OAI O
			where RowNumber >= @rowstart
			  and RowNumber <= @rowend
			  and T.ItemID = O.ItemID
			  and O.Data_Code = @data_code;	
		end;		  
	end
	else
	begin
				  
		-- Should the actual data be returned, or just the identifiers?
		if ( @include_data='true')
		begin
			-- Create saved select across titles for row numbers
			with ITEMS_SELECT AS
			(	select BibID, I.ItemID, VID,
				ROW_NUMBER() OVER (order by O.OAI_Date ASC) as RowNumber
				from SobekCM_Item_Group G, SobekCM_Item I, SobekCM_Item_OAI O
				where ( G.GroupID = I.GroupID )
				  and ( I.ItemID = O.ItemID )
				  and ( G.Suppress_OAI = 'false' )
				  and ( O.OAI_Date >= @from )
				  and ( O.OAI_Date <= @until )
				  and ( O.Data_Code = @data_code )
				  and ( I.Dark = 'false' )				  
				  and ( I.IP_Restriction_Mask = 0 )
			)												
			-- Select the matching rows
			select BibID, T.VID, O.OAI_Data, O.OAI_Date
			from ITEMS_SELECT T, SobekCM_Item_OAI O
			where RowNumber >= @rowstart
			  and RowNumber <= @rowend
			  and T.ItemID = O.ItemID
			  and O.Data_Code = @data_code;				 
		end
		else
		begin
			-- Create saved select across titles for row numbers
			with ITEMS_SELECT AS
			(	select BibID, I.ItemID, VID,
				ROW_NUMBER() OVER (order by O.OAI_Date ASC) as RowNumber
				from SobekCM_Item_Group G, SobekCM_Item I, SobekCM_Item_OAI O
				where ( G.GroupID = I.GroupID )
				  and ( I.ItemID = O.ItemID )
				  and ( G.Suppress_OAI = 'false' )
				  and ( O.OAI_Date >= @from )
				  and ( O.OAI_Date <= @until )
				  and ( O.Data_Code = @data_code )
				  and ( I.Dark = 'false' )				  
				  and ( I.IP_Restriction_Mask = 0 )
			)										
			-- Select the matching rows
			select BibID, T.VID, O.OAI_Date
			from ITEMS_SELECT T, SobekCM_Item_OAI O
			where RowNumber >= @rowstart
			  and RowNumber <= @rowend
			  and T.ItemID = O.ItemID
			  and O.Data_Code = @data_code;	
		end;
	end;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_OAI_Data_Item]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns the OAI data for a single item from the oai source tables
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_OAI_Data_Item]
	@bibid varchar(10),
	@vid varchar(5),
	@data_code varchar(20)
AS
begin
	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Select the matching rows
	select G.GroupID, BibID, O.OAI_Data, O.OAI_Date, VID
	from SobekCM_Item_Group G, SobekCM_Item I, SobekCM_Item_OAI O
	where G.BibID = @bibid
	  and G.GroupID = I.GroupID
	  and I.VID = @vid
	  and I.ItemID = O.ItemID	
	  and O.Data_Code = @data_code;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_OAI_Sets]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



-- Get the OAI set information from the database
-- This stored procedure is called from the UFDC Web
-- Written by Mark Sullivan (March, 2007)
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_OAI_Sets] AS
begin transaction

	-- Get the basic collection information that supports OAI
	select C.AggregationID, C.Code, C.[Name], C.Description, OAI_Metadata=isnull(C.OAI_Metadata, '')
	into #TEMP1
	from SobekCM_Item_Aggregation C
	where ( C.isActive = 1 )
	  and ( C.OAI_Flag = 1 )
	  and ( C.Deleted = 0 )
	order by C.Code;

	select T.Code, T.[Name], T.Description, LastItemAddedDate=MAX(I.CreateDate), T.OAI_Metadata
	from #TEMP1 T, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item I
	where ( T.AggregationID = L.AggregationID )
      and ( L.ItemID = I.ItemID )
	group by Code, [Name], Description, OAI_Metadata;

	-- drop the temporary tables
	drop table #TEMP1;

commit transaction;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_OpenPublishing_Theme]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_OpenPublishing_Theme]
	@id int
AS
begin

	select ThemeID, ThemeName, Location, isnull(Author,'') as Author, isnull([Description],'') as [Description], isnull([Image], '') as [Image], AvailableForSelection, [Default]
	from SobekCM_OpenPublishing_Theme
	where ThemeID=@id;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Settings]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



-- Gets the list of all system-wide settings from the database, including the full list of all
-- metadata search fields, possible workflows, and all disposition data
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Settings]
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
		order by TabPage, Heading, Setting_Key;
	end 
	else
	begin
		select Setting_Key, Setting_Value
		from SobekCM_Settings;
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
	with last_run_cte ( ModuleScheduleID, LastRun) as 
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
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Statistics_Dates]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Gets the year/month pairing for which this system appears to have 
-- some usage statistics recorded.  This is for the drop-down select 
-- boxes when viewing the usage statistics online
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Statistics_Dates]
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Get the distinct years and months
	select [Year], [Month]
	from SobekCM_Statistics
	group by [Year], [Month];

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Submittor]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Submittor]
	@bibid varchar(20),
	@vid varchar(10)
AS
BEGIN

	declare @itemid int;
	set @itemid = ( select ItemID from SobekCM_Item_Group G, SobekCM_Item I where I.GroupID = G.GroupID and BibID=@bibid and VID=@vid);

	select U.FirstName + ' ' + U.LastName as UserName, EmailAddress, U.UserID
	from Tracking_Progress P inner join
		 Tracking_Workflow W on P.WorkFlowID=W.WorkFlowID inner join
		 mySobek_User U on U.UserID=P.WorkPerformedById
	where P.ItemID=@itemid 
	  and W.WorkFlowName='Online Submit';

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Get_Web_Skins]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Stored procedure to get all the web skin information 
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Get_Web_Skins] AS
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select WebSkinCode, OverrideHeaderFooter=isnull(OverrideHeaderFooter,'false'), 
		OverrideBanner=isnull(OverrideBanner, 'false'), BannerLink=isnull(BannerLink,''),
		BaseInterface=isnull(BaseWebSkin,''), Notes=isnull(Notes,''), Build_On_Launch,
		SuppressTopNavigation
	from SobekCM_Web_Skin
	order by WebSkinCode;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Icon_List]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns the list of all icons used by the SobekCM web app
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Icon_List]
as
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Return all the icon information, in any sort order
	select Icon_Name, Icon_URL, Link=isnull(Link,''), Title=isnull(Title,'')
	from SobekCM_Icon;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Item_Count_By_Collection]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Item_Count_By_Collection]
	@option int
AS
BEGIN

	-- No need to perform any locks here, especially given the possible
	-- length of this search
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	SET NOCOUNT ON;
	SET ARITHABORT ON;
	
	-- Get the id for the ALL aggregation
	declare @all_id int;
	set @all_id = coalesce(( select AggregationID from SObekCM_Item_Aggregation where Code='all'), -1);
	
	declare @Aggregation_List TABLE
	(
	  AggregationID int,
	  Code varchar(20),
	  ChildCode varchar(20),
	  Child2Code varchar(20),
	  AllCodes varchar(20),
	  Name nvarchar(255),
	  ShortName nvarchar(100),
	  [Type] varchar(50),
	  isActive bit
	);

	-- Insert the list of items linked to ALL or linked to NONE (include ALL)
	insert into @Aggregation_List ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, [Type], isActive )
	select AggregationID, Code, '', '', Code, Name, ShortName, [Type], isActive
	from SobekCM_Item_Aggregation A
	where ( [Type] not like 'Institut%' )
	  and ( Deleted='false' )
	  and exists ( select * from SobekCM_Item_Aggregation_Hierarchy where ChildID=A.AggregationID and ParentID=@all_id);
	  
	-- Insert the children under those top-level collections
	insert into @Aggregation_List ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, [Type], isActive )
	select A2.AggregationID, T.Code, A2.Code, '', A2.Code, A2.Name, A2.SHortName, A2.[Type], A2.isActive
	from @Aggregation_List T, SobekCM_Item_Aggregation A2, SobekCM_Item_Aggregation_Hierarchy H
	where ( A2.[Type] not like 'Institut%' )
	  and ( T.AggregationID = H.ParentID )
	  and ( A2.AggregationID = H.ChildID )
	  and ( Deleted='false' );
	  
	-- Insert the grand-children under those child collections
	insert into @Aggregation_List ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, [Type], isActive )
	select A2.AggregationID, T.Code, T.ChildCode, A2.Code, A2.Code, A2.Name, A2.SHortName, A2.[Type], A2.isActive
	from @Aggregation_List T, SobekCM_Item_Aggregation A2, SobekCM_Item_Aggregation_Hierarchy H
	where ( A2.[Type] not like 'Institut%' )
	  and ( T.AggregationID = H.ParentID )
	  and ( A2.AggregationID = H.ChildID )
	  and ( Deleted='false' )
	  and ( ChildCode <> '' );

	-- declare the values
	declare @total_item_count int;
	declare @total_title_count int;
	declare @total_page_count int;

	-- Based on the option, select differently
	if ( @option = 1 )
	begin
		  
		-- COUNT OF ALL ITEMS WITH SOME DIGITAL RESOURCES ATTACHED
		-- Get total counts
		select @total_item_count =  ( select count(*) from SobekCM_Item where Deleted = 'false' and (( FileCount > 0 ) or ( [PageCount] > 0 )));
		select @total_title_count =  ( select count(*) from SobekCM_Item_Group G where G.Deleted = 'false' and exists ( select * from SobekCM_Item I where I.GroupID = G.GroupID and I.Deleted = 'false' and (( FileCount > 0 ) or ( [PageCount] > 0 ))));
		select @total_page_count =  coalesce(( select sum( [PageCount] ) from SobekCM_Item where Deleted = 'false'  and (( FileCount > 0 ) or ( [PageCount] > 0 ))), 0 );

		-- Start to build the return set of values
		select code1 = Code, 
			   code2 = ChildCode,
			   code3 = Child2Code,
			   AllCodes,
			[Name], 
			C.isActive AS Active,
			title_count = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ),
			item_count = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ), 
			page_count = coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ), 0)
		from @Aggregation_List C
		where ( C.Code <> 'TESTCOL' ) AND ( C.Code <> 'TESTG' )
		union
		select 'ZZZ','','', 'ZZZ', 'Total Count', 'false', @total_title_count, @total_item_count, @total_page_count
		order by code, code2, code3;
	end
	else if ( @option = 2 )
	begin  

		-- COUNT OF ALL ENTERED ITEMS
		-- Get total counts
		select @total_item_count =  ( select count(*) from SobekCM_Item where Deleted = 'false');
		select @total_title_count =  ( select count(*) from SobekCM_Item_Group G where G.Deleted = 'false');
		select @total_page_count =  coalesce(( select sum( [PageCount] ) from SobekCM_Item ), 0 );

		-- Start to build the return set of values
		select code1 = Code, 
			   code2 = ChildCode,
			   code3 = Child2Code,
			   AllCodes,
			[Name], 
			C.isActive AS Active,
			title_count = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ),
			item_count = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ), 
			page_count = coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ), 0)
		from @Aggregation_List C
		where ( C.Code <> 'TESTCOL' ) AND ( C.Code <> 'TESTG' )
		union
		select 'ZZZ','','', 'ZZZ', 'Total Count', 'false', @total_title_count, @total_item_count, @total_page_count
		order by code, code2, code3;
	end
	else
	begin
			  
		-- THIS IS THE OLDER OPTION, WHERE MILESTONE_COMPLETE MUST HAVE A DATE
		-- Get total counts
		select @total_item_count =  ( select count(*) from SobekCM_Item where Deleted = 'false' and Milestone_OnlineComplete is not null );
		select @total_title_count =  ( select count(*) from SobekCM_Item_Group G where G.Deleted = 'false' and exists ( select * from SobekCM_Item I where I.GroupID = G.GroupID and I.Deleted = 'false' and Milestone_OnlineComplete is not null ));
		select @total_page_count =  coalesce(( select sum( [PageCount] ) from SobekCM_Item where Deleted = 'false'  and ( Milestone_OnlineComplete is not null )), 0 );

		-- Start to build the return set of values
		select code1 = Code, 
			   code2 = ChildCode,
			   code3 = Child2Code,
			   AllCodes,
			[Name], 
			C.isActive AS Active,
			title_count = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ),
			item_count = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ), 
			page_count = coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ), 0)
		from @Aggregation_List C
		where ( C.Code <> 'TESTCOL' ) AND ( C.Code <> 'TESTG' )
		union
		select 'ZZZ','','', 'ZZZ', 'Total Count', 'false', @total_title_count, @total_item_count, @total_page_count
		order by code, code2, code3;
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Item_Count_By_Collection_By_Date_Range]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Item_Count_By_Collection_By_Date_Range]
	@date1 datetime,
	@date2 datetime,
	@option int
AS
BEGIN

	-- No need to perform any locks here, especially given the possible
	-- length of this search
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	SET NOCOUNT ON;
	SET ARITHABORT ON;

	-- Get the id for the ALL aggregation
	declare @all_id int;
	set @all_id = coalesce(( select AggregationID from SObekCM_Item_Aggregation where Code='all'), -1);
	
	declare @Aggregation_List TABLE
	(
	  AggregationID int,
	  Code varchar(20),
	  ChildCode varchar(20),
	  Child2Code varchar(20),
	  AllCodes varchar(20),
	  Name nvarchar(255),
	  ShortName nvarchar(100),
	  [Type] varchar(50),
	  isActive bit
	);
	
	-- Insert the list of items linked to ALL or linked to NONE (include ALL)
	insert into @Aggregation_List ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, [Type], isActive )
	select AggregationID, Code, '', '', Code, Name, ShortName, [Type], isActive
	from SobekCM_Item_Aggregation A
	where ( [Type] not like 'Institut%' )
	  and ( Deleted='false' )
	  and exists ( select * from SobekCM_Item_Aggregation_Hierarchy where ChildID=A.AggregationID and ParentID=@all_id);
	  
	-- Insert the children under those top-level collections
	insert into @Aggregation_List ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, [Type], isActive )
	select A2.AggregationID, T.Code, A2.Code, '', A2.Code, A2.Name, A2.SHortName, A2.[Type], A2.isActive
	from @Aggregation_List T, SobekCM_Item_Aggregation A2, SobekCM_Item_Aggregation_Hierarchy H
	where ( A2.[Type] not like 'Institut%' )
	  and ( T.AggregationID = H.ParentID )
	  and ( A2.AggregationID = H.ChildID )
	  and ( Deleted='false' );
	  
	-- Insert the grand-children under those child collections
	insert into @Aggregation_List ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, [Type], isActive )
	select A2.AggregationID, T.Code, T.ChildCode, A2.Code, A2.Code, A2.Name, A2.SHortName, A2.[Type], A2.isActive
	from @Aggregation_List T, SobekCM_Item_Aggregation A2, SobekCM_Item_Aggregation_Hierarchy H
	where ( A2.[Type] not like 'Institut%' )
	  and ( T.AggregationID = H.ParentID )
	  and ( A2.AggregationID = H.ChildID )
	  and ( Deleted='false' )
	  and ( ChildCode <> '' );

	-- Prepare to collect the total counts
	declare @total_item_count int;
	declare @total_title_count int;
	declare @total_page_count int;
	declare @total_item_count_date1 int;
	declare @total_title_count_date1 int;
	declare @total_page_count_date1 int;
	declare @total_item_count_date2 int;
	declare @total_title_count_date2 int;
	declare @total_page_count_date2 int;

		-- Based on the option, select differently
	if ( @option = 1 )
	begin
		  
		-- COUNT OF ALL ITEMS WITH SOME DIGITAL RESOURCES ATTACHED
		-- Get total item count	
		select @total_item_count =  ( select count(*) from SobekCM_Item where Deleted = 'false' and (( FileCount > 0 ) or ( [PageCount] > 0 )));

		-- Get total title count	
		select @total_title_count = ( select count(G.GroupID)
										from SobekCM_Item_Group G
										where exists ( select ItemID
														from SobekCM_Item I
														where ( I.Deleted = 'false' )
														and (( FileCount > 0 ) or ( [PageCount] > 0 ))
														and ( I.GroupID = G.GroupID )));
		-- Get total title count	
		select @total_page_count =  coalesce(( select sum( [PageCount] ) from SobekCM_Item where Deleted = 'false'  and (( FileCount > 0 ) or ( [PageCount] > 0 ))), 0 );

		-- Get total item count	
		select @total_item_count_date1 =  ( select count(ItemID) 
											from SobekCM_Item I
											where ( I.Deleted = 'false' )
											  and (( FileCount > 0 ) or ( [PageCount] > 0 ))
											  and ( CreateDate is not null )
											  and ( CreateDate <= @date1 ));

		-- Get total title count	
		select @total_title_count_date1 =  ( select count(G.GroupID)
												from SobekCM_Item_Group G
												where exists ( select *
															from SobekCM_Item I
															where ( I.Deleted = 'false' )
																and (( FileCount > 0 ) or ( [PageCount] > 0 ))
																and ( CreateDate is not null )
																and ( CreateDate <= @date1 )
																and ( I.GroupID = G.GroupID )));


		-- Get total title count	
		select @total_page_count_date1 =  ( select sum( coalesce([PageCount],0) ) 
											from SobekCM_Item I
											where ( I.Deleted = 'false' )
												and (( FileCount > 0 ) or ( [PageCount] > 0 ))
												and ( CreateDate is not null )
												and ( CreateDate <= @date1 ));

		-- Return these values if this has just one date
		if ( isnull( @date2, '1/1/2000' ) = '1/1/2000' )
		begin
	
			-- Start to build the return set of values
			select code1 = Code, 
					code2 = ChildCode,
					code3 = Child2Code,
					AllCodes,
				[Name], 
				C.isActive AS Active,
				title_count = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ),
				item_count = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ), 
				page_count = coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ), 0),
				title_count_date1 = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date1 )),
				item_count_date1 = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date1 )),
				page_count_date1 = coalesce(( select sum( [PageCount] ) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date1 )), 0)
			from @Aggregation_List C
			union
			select 'ZZZ','','', 'ZZZ', 'Total Count', 'false', @total_title_count, @total_item_count, @total_page_count, 
				coalesce(@total_title_count_date1,0), coalesce(@total_item_count_date1,0), coalesce(@total_page_count_date1,0)
			order by code, code2, code3;
		
		end
		else
		begin

			-- Get total item count		
			select @total_item_count_date2 =  ( select count(ItemID) 
												from SobekCM_Item I
												where ( I.Deleted = 'false' )
													and (( FileCount > 0 ) or ( [PageCount] > 0 ))
													and ( CreateDate <= @date2 ));

			-- Get total title count		
			select @total_title_count_date2 =  ( select count(G.GroupID)
													from SobekCM_Item_Group G
													where exists ( select *
																from SobekCM_Item I
																where ( I.Deleted = 'false' )
																	and (( FileCount > 0 ) or ( [PageCount] > 0 ))
																	and ( CreateDate <= @date2 ) 
																	and ( I.GroupID = G.GroupID )));


			-- Get total title count		
			select @total_page_count_date2 =  ( select sum( coalesce([PageCount],0) ) 
												from SobekCM_Item I
												where ( I.Deleted = 'false' )
													and (( FileCount > 0 ) or ( [PageCount] > 0 ))
													and ( CreateDate <= @date2 ));


			-- Start to build the return set of values
			select code1 = Code, 
					code2 = ChildCode,
					code3 = Child2Code,
					AllCodes,
				[Name], 
				C.isActive AS Active,
				title_count = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ),
				item_count = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ), 
				page_count = coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ), 0),
				title_count_date1 = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date1 )),
				item_count_date1 = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date1 )), 
				page_count_date1 = coalesce(( select sum( [PageCount] ) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date1 )), 0),
				title_count_date2 = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date2 )),
				item_count_date2 = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date2 )), 
				page_count_date2 = coalesce(( select sum( [PageCount] ) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date2 )), 0)
			from @Aggregation_List C
			union
			select 'ZZZ','','','ZZZ', 'Total Count', 'false', @total_title_count, @total_item_count, @total_page_count, 
					coalesce(@total_title_count_date1,0), coalesce(@total_item_count_date1,0), coalesce(@total_page_count_date1,0),
					coalesce(@total_title_count_date2,0), coalesce(@total_item_count_date2,0), coalesce(@total_page_count_date2,0)
			order by code, code2, code3;
		end;

	end
	else if ( @option = 2 )
	begin
		-- COUNT OF ALL ENTERED ITEMS
						-- Get total item count	
		select @total_item_count =  ( select count(*) from SobekCM_Item where Deleted = 'false');

		-- Get total title count	
		select @total_title_count = ( select count(G.GroupID)
										from SobekCM_Item_Group G
										where exists ( select ItemID
														from SobekCM_Item I
														where ( I.Deleted = 'false' )
														and ( I.GroupID = G.GroupID )));
		-- Get total title count	
		select @total_page_count =  coalesce(( select sum( [PageCount] ) from SobekCM_Item where Deleted = 'false'), 0 );

		-- Get total item count	
		select @total_item_count_date1 =  ( select count(ItemID) 
											from SobekCM_Item I
											where ( I.Deleted = 'false' )
											  and ( CreateDate is not null )
											  and ( CreateDate <= @date1 ));

		-- Get total title count	
		select @total_title_count_date1 =  ( select count(G.GroupID)
												from SobekCM_Item_Group G
												where exists ( select *
															from SobekCM_Item I
															where ( I.Deleted = 'false' )
																and ( CreateDate is not null )
																and ( CreateDate <= @date1 )
																and ( I.GroupID = G.GroupID )));


		-- Get total title count	
		select @total_page_count_date1 =  ( select sum( coalesce([PageCount],0) ) 
											from SobekCM_Item I
											where ( I.Deleted = 'false' )
												and ( CreateDate is not null )
												and ( CreateDate <= @date1 ));

		-- Return these values if this has just one date
		if ( isnull( @date2, '1/1/2000' ) = '1/1/2000' )
		begin
	
			-- Start to build the return set of values
			select code1 = Code, 
					code2 = ChildCode,
					code3 = Child2Code,
					AllCodes,
				[Name], 
				C.isActive AS Active,
				title_count = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ),
				item_count = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ), 
				page_count = coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ), 0),
				title_count_date1 = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date1 )),
				item_count_date1 = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date1 )),
				page_count_date1 = coalesce(( select sum( [PageCount] ) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date1 )), 0)
			from @Aggregation_List C
			union
			select 'ZZZ','','', 'ZZZ', 'Total Count', 'false', @total_title_count, @total_item_count, @total_page_count, 
				coalesce(@total_title_count_date1,0), coalesce(@total_item_count_date1,0), coalesce(@total_page_count_date1,0)
			order by code, code2, code3;
		
		end
		else
		begin

			-- Get total item count		
			select @total_item_count_date2 =  ( select count(ItemID) 
												from SobekCM_Item I
												where ( I.Deleted = 'false' )
													and ( CreateDate <= @date2 ));

			-- Get total title count		
			select @total_title_count_date2 =  ( select count(G.GroupID)
													from SobekCM_Item_Group G
													where exists ( select *
																from SobekCM_Item I
																where ( I.Deleted = 'false' )
																	and ( CreateDate <= @date2 ) 
																	and ( I.GroupID = G.GroupID )));


			-- Get total title count		
			select @total_page_count_date2 =  ( select sum( coalesce([PageCount],0) ) 
												from SobekCM_Item I
												where ( I.Deleted = 'false' )
													and ( CreateDate <= @date2 ));


			-- Start to build the return set of values
			select code1 = Code, 
					code2 = ChildCode,
					code3 = Child2Code,
					AllCodes,
				[Name], 
				C.isActive AS Active,
				title_count = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ),
				item_count = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ), 
				page_count = coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ), 0),
				title_count_date1 = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date1 )),
				item_count_date1 = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date1 )), 
				page_count_date1 = coalesce(( select sum( [PageCount] ) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date1 )), 0),
				title_count_date2 = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date2 )),
				item_count_date2 = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date2 )), 
				page_count_date2 = coalesce(( select sum( [PageCount] ) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= @date2 )), 0)
			from @Aggregation_List C
			union
			select 'ZZZ','','','ZZZ', 'Total Count', 'false', @total_title_count, @total_item_count, @total_page_count, 
					coalesce(@total_title_count_date1,0), coalesce(@total_item_count_date1,0), coalesce(@total_page_count_date1,0),
					coalesce(@total_title_count_date2,0), coalesce(@total_item_count_date2,0), coalesce(@total_page_count_date2,0)
			order by code, code2, code3;
		end;
	end
	else 
	begin

		-- THIS IS THE OLDER OPTION, WHERE MILESTONE_COMPLETE MUST HAVE A DATE

		-- Get total item count	
		select @total_item_count =  ( select count(*) from SobekCM_Item where Deleted = 'false' and Milestone_OnlineComplete is not null );

		-- Get total title count	
		select @total_title_count = ( select count(G.GroupID)
										from SobekCM_Item_Group G
										where exists ( select ItemID
														from SobekCM_Item I
														where ( I.Deleted = 'false' )
														and ( Milestone_OnlineComplete is not null )
														and ( I.GroupID = G.GroupID )));
		-- Get total title count	
		select @total_page_count =  coalesce(( select sum( [PageCount] ) from SobekCM_Item where Deleted = 'false'  and ( Milestone_OnlineComplete is not null )), 0 );

		-- Get total item count	
		select @total_item_count_date1 =  ( select count(ItemID) 
											from SobekCM_Item I
											where ( I.Deleted = 'false' )
												and ( Milestone_OnlineComplete is not null )
												and ( Milestone_OnlineComplete <= @date1 ));

		-- Get total title count	
		select @total_title_count_date1 =  ( select count(G.GroupID)
												from SobekCM_Item_Group G
												where exists ( select *
															from SobekCM_Item I
															where ( I.Deleted = 'false' )
																and ( Milestone_OnlineComplete is not null )
																and ( Milestone_OnlineComplete <= @date1 ) 
																and ( I.GroupID = G.GroupID )));


		-- Get total title count	
		select @total_page_count_date1 =  ( select sum( coalesce([PageCount],0) ) 
											from SobekCM_Item I
											where ( I.Deleted = 'false' )
												and ( Milestone_OnlineComplete is not null )
												and ( Milestone_OnlineComplete <= @date1 ));

		-- Return these values if this has just one date
		if ( isnull( @date2, '1/1/2000' ) = '1/1/2000' )
		begin
	
			-- Start to build the return set of values
			select code1 = Code, 
					code2 = ChildCode,
					code3 = Child2Code,
					AllCodes,
				[Name], 
				C.isActive AS Active,
				title_count = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ),
				item_count = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ), 
				page_count = coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ), 0),
				title_count_date1 = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date1),
				item_count_date1 = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date1 ), 
				page_count_date1 = coalesce(( select sum( [PageCount] ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date1 ), 0)
			from @Aggregation_List C
			union
			select 'ZZZ','','', 'ZZZ', 'Total Count', 'false', @total_title_count, @total_item_count, @total_page_count, 
				coalesce(@total_title_count_date1,0), coalesce(@total_item_count_date1,0), coalesce(@total_page_count_date1,0)
			order by code, code2, code3;
		
		end
		else
		begin

			-- Get total item count		
			select @total_item_count_date2 =  ( select count(ItemID) 
												from SobekCM_Item I
												where ( I.Deleted = 'false' )
													and ( Milestone_OnlineComplete is not null )
													and ( Milestone_OnlineComplete <= @date2 ));

			-- Get total title count		
			select @total_title_count_date2 =  ( select count(G.GroupID)
													from SobekCM_Item_Group G
													where exists ( select *
																from SobekCM_Item I
																where ( I.Deleted = 'false' )
																	and ( Milestone_OnlineComplete is not null )
																	and ( Milestone_OnlineComplete <= @date2 ) 
																	and ( I.GroupID = G.GroupID )));


			-- Get total title count		
			select @total_page_count_date2 =  ( select sum( coalesce([PageCount],0) ) 
												from SobekCM_Item I
												where ( I.Deleted = 'false' )
													and ( Milestone_OnlineComplete is not null )
													and ( Milestone_OnlineComplete <= @date2 ));


			-- Start to build the return set of values
			select code1 = Code, 
					code2 = ChildCode,
					code3 = Child2Code,
					AllCodes,
				[Name], 
				C.isActive AS Active,
				title_count = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ),
				item_count = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ), 
				page_count = coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ), 0),
				title_count_date1 = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date1),
				item_count_date1 = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date1 ), 
				page_count_date1 = coalesce(( select sum( [PageCount] ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date1 ), 0),
				title_count_date2 = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date2),
				item_count_date2 = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date2 ), 
				page_count_date2 = coalesce(( select sum( [PageCount] ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date2 ), 0)
			from @Aggregation_List C
			union
			select 'ZZZ','','','ZZZ', 'Total Count', 'false', @total_title_count, @total_item_count, @total_page_count, 
					coalesce(@total_title_count_date1,0), coalesce(@total_item_count_date1,0), coalesce(@total_page_count_date1,0),
					coalesce(@total_title_count_date2,0), coalesce(@total_item_count_date2,0), coalesce(@total_page_count_date2,0)
			order by code, code2, code3;
		end;
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Item_List]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Gets a list of items and groups which exist within this instance
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Item_List]
	@include_private bit
as
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Set value for filtering privates
	declare @lower_mask int;
	set @lower_mask = 0;
	if ( @include_private = 'true' )
	begin
		set @lower_mask = -256;
	end;

	-- Return the item group / item information in one large table
	select G.BibID, I.VID, IP_Restriction_Mask, I.Title, G.[Type], I.Dark, I.ItemID
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID )
	  and ( G.Deleted = CONVERT(bit,0) )
	  and ( I.Deleted = CONVERT(bit,0) )
	  and ( I.IP_Restriction_Mask >= @lower_mask )
	order by BibID, VID;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Item_List_Brief2]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns the list of all items within the library with some very basic information.
-- This is primarily utilized by the builder to step through all items in the library
-- and build the marc files, or links for the sitemap
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Item_List_Brief2]
	@include_private bit
as
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	if ( @include_private = 'true' )
	begin
		-- Return the item group / item information in one large table
		select G.BibID, I.VID, G.GroupTitle, 
			isnull(I.Level1_Text, '') as Level1_Text, isnull( I.Level1_Index, 0 ) as Level1_Index, 
			isnull(I.Level2_Text, '') as Level2_Text, isnull( I.Level2_Index, 0 ) as Level2_Index, 
			isnull(I.Level3_Text, '') as Level3_Text, isnull( I.Level3_Index, 0 ) as Level3_Index, 
			PubDate=isnull(I.PubDate,''), SortDate=isnull( I.SortDate,-1), MainThumbnail=G.File_Location + '/' + VID + '/' + isnull( I.MainThumbnail,''), 
			I.Title, Author=isnull(I.Author,''), IP_Restriction_Mask, G.OCLC_Number, G.ALEPH_Number, I.LastSaved, I.AggregationCodes, G.Large_Format
		from SobekCM_Item I, SobekCM_Item_Group G
		where ( I.GroupID = G.GroupID )
		  and ( G.Deleted = CONVERT(bit,0) )
		  and ( I.Deleted = CONVERT(bit,0) );

		-- Get the items that are really multiple (having more than one volume)
		select G.BibID, G.GroupID, VID_COUNT = ItemCount, G.GroupTitle, G.[Type], G.File_Location, SortTitle=isnull(G.SortTitle, G.GroupTitle), G.OCLC_Number, G.ALEPH_Number
		from SobekCM_Item_Group G;
	end
	else
	begin
			-- Return the item group / item information in one large table
		select G.BibID, I.VID, G.GroupTitle, 
			isnull(I.Level1_Text, '') as Level1_Text, isnull( I.Level1_Index, 0 ) as Level1_Index, 
			isnull(I.Level2_Text, '') as Level2_Text, isnull( I.Level2_Index, 0 ) as Level2_Index, 
			isnull(I.Level3_Text, '') as Level3_Text, isnull( I.Level3_Index, 0 ) as Level3_Index, 
			PubDate=isnull(I.PubDate,''), SortDate=isnull( I.SortDate,-1), MainThumbnail=G.File_Location + '/' + VID + '/' + isnull( I.MainThumbnail,''), 
			I.Title, Author=isnull(I.Author,''), IP_Restriction_Mask, G.OCLC_Number, G.ALEPH_Number, I.LastSaved, I.AggregationCodes, G.Large_Format
		from SobekCM_Item I, SobekCM_Item_Group G
		where ( I.GroupID = G.GroupID )
		  and ( G.Deleted = CONVERT(bit,0) )
		  and ( I.Deleted = CONVERT(bit,0) )
		  and ( I.IP_Restriction_Mask >= 0 );
		  
		-- Get the list of groups which have at least one item non-private
		select distinct(GroupID) as GroupID
		into #TEMP1
		from SobekCM_Item I
		where ( I.Deleted = CONVERT(bit,0) )
		  and ( I.IP_Restriction_Mask >= 0 ); 
		  
		-- Get the items that are really multiple (having more than one volume)
		select G.BibID, G.GroupID, VID_COUNT = ItemCount, G.GroupTitle, G.[Type], G.File_Location, SortTitle=isnull(G.SortTitle, G.GroupTitle), G.OCLC_Number, G.ALEPH_Number
		from SobekCM_Item_Group G, #TEMP1 T
		where T.GroupID = G.GroupID and G.Deleted = CONVERT(bit,0);
		
		-- Drop the temporary table
		drop table #TEMP1;
		
		-- Get list of items / groups which are private
		select G.BibID, I.VID
		from SobekCM_Item I, SobekCM_Item_Group G
		where ( I.GroupID = G.GroupID )
		  and ( G.Deleted = CONVERT(bit,0) )
		  and ( I.Deleted = CONVERT(bit,0) )
		  and ( I.IP_Restriction_Mask < 0 );
	end;	
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Items_By_ALEPH]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the list of items by ALEPH number
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Items_By_ALEPH] 
	@aleph_number int
AS
BEGIN
	
	-- Return the item informaiton
	select BibID, VID, SortDate, Spatial_KML, fk_TitleID = I.GroupID, Title 
	from SobekCM_Item I, SobekCM_Item_Group G
	where I.GroupID = G.GroupID 
	  and G.ALEPH_Number = @aleph_number
	order by BibID ASC, VID ASC

	-- Return the title information
	select G.BibID, G.SortTitle, TitleID=G.GroupID, [Rank]=-1
	from SobekCM_Item_Group G
	where  G.ALEPH_Number = @aleph_number
	order by BibID ASC
	
END
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Items_By_OCLC]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the list of items by OCLC number
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Items_By_OCLC] 
	@oclc_number bigint
AS
BEGIN
	
	-- Return the item informaiton
	select BibID, VID, SortDate, Spatial_KML, fk_TitleID = I.GroupID, Title 
	from SobekCM_Item I, SobekCM_Item_Group G
	where I.GroupID = G.GroupID 
	  and G.OCLC_Number = @oclc_number
	order by BibID ASC, VID ASC

	-- Return the title information
	select G.BibID, G.SortTitle, TitleID=G.GroupID, [Rank]=-1
	from SobekCM_Item_Group G
	where  G.OCLC_Number = @oclc_number
	order by BibID ASC
	
END
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Log_Email]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Log an email which was sent through a different method.  This does not
-- cause a database mail to be sent, just logs an email which was sent
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Log_Email] 
	@sender varchar(250),
	@recipients_list varchar(500),
	@subject_line varchar(240),
	@email_body nvarchar(max),
	@html_format bit,
	@contact_us bit,
	@replytoemailid int
AS
begin

	-- Log this email
	insert into SobekCM_Email_Log( Sender, Receipt_List, Subject_Line, Email_Body, Sent_Date, HTML_Format, Contact_Us, ReplyToEmailID )
	values ( @sender, @recipients_list, @subject_line + '( log only )', @email_body, GETDATE(), @html_format, @contact_us, @replytoemailid );
	
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Manager_Get_Thematic_Headings]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Stored procedure for pulling the list of thematic headings
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Manager_Get_Thematic_Headings] 
AS
BEGIN
	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Return all the thematic heading information
	select * 
	from SobekCM_Thematic_Heading
	order by ThemeOrder;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_MarcXML_Production_Feed]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Pulls the list of items for MARC XML Automation during
-- load of records to production mango
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_MarcXML_Production_Feed]
AS
BEGIN
	
	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Get the list of each BibID and the first VID
	with temp as (
		select G.BibID, G.GroupID, I.VID, I.ItemID, CreateDate, File_Location,
    		row_number() over (partition by G.GroupID order by I.VID) as rownum
		from SobekCM_Item_Group G, SobekCM_Item I
		where ( G.GroupID=I.GroupID )
		  and ( I.Deleted = 'false' )
		  and ( I.IP_Restriction_Mask = 0 )
		  and ( G.Deleted = 'false' )
		  and ( G.Include_In_MarcXML_Prod_Feed = 'true' )
	)
	select BibID, GroupID, VID, ItemID, CreateDate, File_Location
	into #ONE_VID_PER_BIB
	from temp 
	where rownum = 1;
	
	-- Get the list of all public items which are marked to include in 
	-- the marc xml production feed
	select BibID, VID, CreateDate, CollectionCode=C.Code, File_Location
	from #ONE_VID_PER_BIB I, SobekCM_Item_Aggregation_Item_Link CL, SobekCM_Item_Aggregation C
	where ( CL.ItemID = I.ItemID )
	  and ( CL.AggregationID = C.AggregationID )
	  and ( CL.impliedLink = 'false' )
	order by BibID;
	  
	-- Drop the temporary table we are completed with
	drop table #ONE_VID_PER_BIB;

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_MarcXML_Test_Feed]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Pulls the list of items for MARC XML Automation during
-- load of records to test mango
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_MarcXML_Test_Feed]
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Get the list of each BibID and the first VID
	with temp as (
		select G.BibID, G.GroupID, I.VID, I.ItemID, CreateDate, File_Location,
    		row_number() over (partition by G.GroupID order by I.VID) as rownum
		from SobekCM_Item_Group G, SobekCM_Item I
		where ( G.GroupID=I.GroupID )
		  and ( I.Deleted = 'false' )
		  and ( I.IP_Restriction_Mask = 0 )
		  and ( G.Deleted = 'false' )
		  and ( G.Include_In_MarcXML_Test_Feed = 'true' )
	)
	select BibID, GroupID, VID, ItemID, CreateDate, File_Location
	into #ONE_VID_PER_BIB
	from temp 
	where rownum = 1;
	
	-- Get the list of all public items which are marked to include in 
	-- the marc xml production feed
	select BibID, VID, CreateDate, CollectionCode=C.Code, File_Location
	from #ONE_VID_PER_BIB I, SobekCM_Item_Aggregation_Item_Link CL, SobekCM_Item_Aggregation C
	where ( CL.ItemID = I.ItemID )
	  and ( CL.AggregationID = C.AggregationID )
	  and ( CL.impliedLink = 'false' )
	order by BibID;
	  
	-- Drop the temporary table we are completed with
	drop table #ONE_VID_PER_BIB;


END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Mass_Update_Item_Aggregation_Link]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add a link to the item aggregation (and all parents) to all the items
-- within a particular item group
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Mass_Update_Item_Aggregation_Link]
	@groupid int,
	@code varchar(20)
AS
begin

	-- Only continue if the code exists
	if ( len( isnull( @code,'')) > 0 )
	begin
		-- Ensure this aggregation code exists
		if ( @code in ( select Code from SobekCM_Item_Aggregation ))
		begin
			-- Get the ID for this aggregation code
			declare @AggregationID int;
			select @AggregationID = AggregationID from SobekCM_Item_Aggregation where Code = @code;
			
			-- For any existing links, make sure this does not say implied, since this was explicitly connected
			update SobekCM_Item_Aggregation_Item_Link
			set impliedLink = 'false'
			where ( AggregationID = @AggregationID ) 
			  and exists ( select * from SobekCM_Item I where I.GroupID=@GroupID and I.ItemID=SobekCM_Item_Aggregation_Item_Link.ItemID );

			-- Tie this item to this primary collection, if not present
			insert into SobekCM_Item_Aggregation_Item_Link ( AggregationID, ItemID, impliedLink )
			select @AggregationID, I.ItemID, 'false' 
			from SobekCM_Item I 
			where I.GroupID = @groupid
			  and not exists ( select * from SobekCM_Item_Aggregation_Item_Link L where L.ItemID = I.ItemID and L.AggregationID = @AggregationID );
			
			-- Update the last item added date time
			update SobekCM_Item_Aggregation
			set LastItemAdded = ( select top 1 Milestone_OnlineComplete from SobekCM_Item where GroupID=@groupid and Milestone_OnlineComplete is not null order by Milestone_OnlineComplete DESC )
			where AggregationID = @AggregationID
			  and LastItemAdded < ( select top 1 Milestone_OnlineComplete from SobekCM_Item where GroupID=@groupid and Milestone_OnlineComplete is not null order by Milestone_OnlineComplete DESC )
			  and exists ( select Milestone_OnlineComplete from SobekCM_Item where GroupID=@groupid and Milestone_OnlineComplete is not null );

			-- Select parent codes
			select P.Code, P.AggregationID, Hierarchy=1
			into #TEMP_PARENTS
			from SobekCM_Item_Aggregation C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( C.Code = @code )
			  and ( H.Search_Parent_Only = 'false' );

			-- Select the grandparent codes
			insert into #TEMP_PARENTS ( Code, AggregationID, Hierarchy)
			select P.Code, P.AggregationID, 2 
			from #TEMP_PARENTS C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( H.Search_Parent_Only = 'false' );

			-- Select the grand-grandparent codes
			insert into #TEMP_PARENTS ( Code, AggregationID, Hierarchy)
			select P.Code, P.AggregationID, 3
			from #TEMP_PARENTS C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( C.Hierarchy = 2 )
			  and ( H.Search_Parent_Only = 'false' );

			-- Select the grand-grand-grandparent codes
			insert into #TEMP_PARENTS ( Code, AggregationID, Hierarchy)
			select P.Code, P.AggregationID, 4
			from #TEMP_PARENTS C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( C.Hierarchy = 3 )
			  and ( H.Search_Parent_Only = 'false' );

			-- Insert the link anywhere it does not exist
			insert into SobekCM_Item_Aggregation_Item_Link ( AggregationID, ItemID, impliedLink )
			select AggregationID, I.ItemID, 'true'
			from #TEMP_PARENTS P, SobekCM_Item I
			where I.GroupID=@groupid 
			  and not exists ( select * 
								from SobekCM_Item_Aggregation_Item_Link L
								where ( P.AggregationID = L.AggregationID )
								  and ( L.ItemID = I.ItemID ));
								  
			-- Also update the last item added date
			update SobekCM_Item_Aggregation
			set LastItemAdded = ( select top 1 Milestone_OnlineComplete from SobekCM_Item where GroupID=@groupid and Milestone_OnlineComplete is not null order by Milestone_OnlineComplete DESC )
			where exists ( select * from #TEMP_PARENTS T where T.AggregationID=SobekCM_Item_Aggregation.AggregationID )
			  and LastItemAdded < ( select top 1 Milestone_OnlineComplete from SobekCM_Item where GroupID=@groupid and Milestone_OnlineComplete is not null order by Milestone_OnlineComplete DESC )
			  and exists ( select Milestone_OnlineComplete from SobekCM_Item where GroupID=@groupid and Milestone_OnlineComplete is not null );

			-- drop the temporary table
			drop table #TEMP_PARENTS;
		end;
	end;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Mass_Update_Item_Behaviors]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Modifies the item behaviors in a mass update for all items in 
-- a particular item group
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Mass_Update_Item_Behaviors]
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
		-- Does this institution already exist?
		if (( select count(*) from SobekCM_Item_Aggregation where Code = @HoldingCode ) = 0 )
		begin
			-- Add new institution
			insert into SobekCM_Item_Aggregation ( Code, [Name], ShortName, Description, ThematicHeadingID, [Type], isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
			values ( @HoldingCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
		end;
		
		-- Add the link to this holding code ( and any legitimate parent aggregations )
		exec [SobekCM_Mass_Update_Item_Aggregation_Link] @GroupID, @HoldingCode;
	end;

	-- Check for Source Institution Code
	if ( len ( isnull ( @SourceCode, '' ) ) > 0 )
	begin
		-- Does this institution already exist?
		if (( select count(*) from SobekCM_Item_Aggregation where Code = @SourceCode ) = 0 )
		begin
			-- Add new institution
			insert into SobekCM_Item_Aggregation ( Code, [Name], ShortName, Description, ThematicHeadingID, [Type], isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
			values ( @SourceCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
		end;

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
/****** Object:  StoredProcedure [dbo].[SobekCM_Page_Item_Count_History]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Return the item and page count added each month
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Page_Item_Count_History]
as
begin

	select ItemID, YEAR(Milestone_OnlineComplete) as [Year], MONTH(Milestone_OnlineComplete) as [Month], [PageCount]
	into #TEMP1
	from SobekCM_Item I
	where I.Deleted = 'false'

	select [Year], [MONTH], Total = SUM( [PageCount] ), ItemCount = COUNT(*)
	from #TEMP1
	group by [Year], [Month]
	order by [Year], [Month]

	drop table #TEMP1

end
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_QC_Delete_Error]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_QC_Delete_Error]
	-- Add the parameters for the stored procedure here
	@itemID int,
	@filename nvarchar(MAX)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	DELETE FROM SobekCM_QC_Errors WHERE ItemID=@itemID AND [FileName]=@filename;
    	
END
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_QC_Get_Errors]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_QC_Get_Errors]
	-- Add the parameters for the stored procedure here
	@itemID int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT * FROM SobekCM_QC_Errors WHERE ItemID=@itemID; 
     	
END
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_QC_Save_Error]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_QC_Save_Error] 
	-- Add the parameters for the stored procedure here
	@itemID int,
	@filename nvarchar(MAX),
	@errorCode nchar(10),
	@isVolumeError bit,
	@description nvarchar(MAX),
	@errorID int out
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	--Insert this error into the SobekCM_QC_Errors Table
	if not exists(select * from SobekCM_QC_Errors where ItemID=@itemID and [FileName]=@filename)
	Begin
		-- Insert statements for procedure here
		INSERT INTO SobekCM_QC_Errors(ItemID, [FileName],ErrorCode,isVolumeError,[Description])
		VALUES(@itemID,@filename,@errorCode, @isVolumeError, @description);
	End
	else
	Begin
		Update SobekCM_QC_Errors set ErrorCode=@errorCode, isVolumeError=@isVolumeError,[Description]=@description
		where ItemID=@itemID AND [FileName]=@filename;
 
	End
	set @errorID=@@IDENTITY;   
  
	--Also add this error into the the errors History	table
	if not exists(select * from SobekCM_QC_Errors_History where ItemID=@itemID and ErrorCode=@errorCode)
    BEGIN
        INSERT INTO SobekCM_QC_Errors_History(ItemID,ErrorCode,isVolumeError,[Count])
        VALUES(@itemID,@errorCode,@isVolumeError,1);
    END
    else
    Begin
      Declare @errorCount int
      select @errorCount = [Count] from SobekCM_QC_Errors_History
      where ItemID=@itemID and ErrorCode=@errorCode;
      
      update SobekCM_QC_Errors_History set [Count]=(@errorCount+1)
      where ItemID=@itemID and ErrorCode=@errorCode; 
    End
END
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Random_Item]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Choose a random item from the entire digital library
-- that is public
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Random_Item] AS
BEGIN

	-- Get the minimum and maximum ids
	declare @minid int;
	declare @maxid int;
	set @minid = ( select MIN(GroupID) from SobekCM_Item_Group where Deleted = 'false' );
	set @maxid = ( select MAX(GroupID) from SobekCM_Item_Group where Deleted = 'false' );

	-- Pick a random if
	declare @randomid int;
	set @randomid = -1;
	declare @attempt int;
	set @attempt = 0;

	-- Loop here for about 20 times (since this is so relatively cheap)
	while (( @attempt <= 20 ) and ( @randomid < 0 ))
	begin

		set @randomid = @minid + ( RAND() * (@maxid - @minid ));

		if ( not exists ( select * from SobekCM_Item_Group G where Deleted='false' and GroupID = @randomid and exists ( select 1 from SobekCM_Item I where I.GroupID=@randomid and I.Deleted='false' and I.IP_Restriction_Mask = 0 and I.Dark = 'false' and I.[PageCount] > 0)))
		begin
			set @randomid = -1;
		end;

		set @attempt = @attempt + 1;
	end;

	-- Sometimes, the process above does not generate any BibID, so use the brute force method
	if ( @randomid < 0 )
	begin

		-- Get a small sample of rows, and assign top value
		with sample_rows_ordered AS (
			select GroupID, newid() as randomid
			from SobekCM_Item_Group G
			where exists ( select 1 from SobekCM_Item I where I.GroupID=G.GroupID and I.Deleted='false' and I.IP_Restriction_Mask = 0 and I.Dark = 'false' and I.[PageCount] > 0)
		)
		select @randomid = (select top 1 GroupID from sample_rows_ordered order by randomid);

	end;

	-- With the bibid in hand, now select a random vid
	select top 1 BibID, VID
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.Deleted = 'false' )
	  and ( I.IP_Restriction_Mask = 0 )
	  and ( I.Dark = 'false' )
	  and ( G.GroupID = @randomid )
	  and ( G.GroupID = I.GroupID )
	  and ( I.[PageCount] > 0 )
	order by newid();

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Remove_Item_Viewers]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Remove an existing viewer for an item
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Remove_Item_Viewers] 
	@ItemID int,
	@Viewer1_Type varchar(50),
	@Viewer2_Type varchar(50),
	@Viewer3_Type varchar(50),
	@Viewer4_Type varchar(50),
	@Viewer5_Type varchar(50),
	@Viewer6_Type varchar(50)
AS
BEGIN 

	-- Exclude the first viewer
	if ( len(coalesce(@Viewer1_Type, '')) > 0 )
	begin
		-- Get the primary key for this viewer type
		declare @Viewer1_TypeID int;
		set @Viewer1_TypeID = coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = @Viewer1_Type ), -1 );

		-- Only continue if that viewer type was found
		if ( @Viewer1_TypeID > 0 )
		begin
			update SobekCM_Item_Viewers 
			set Exclude='true' 
			where ItemID=@ItemID and ItemViewTypeID=@Viewer1_TypeID;
		end;
	end;

	-- Exclude the second viewer
	if ( len(coalesce(@Viewer2_Type, '')) > 0 )
	begin
		-- Get the primary key for this viewer type
		declare @Viewer2_TypeID int;
		set @Viewer2_TypeID = coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = @Viewer2_Type ), -1 );

		-- Only continue if that viewer type was found
		if ( @Viewer2_TypeID > 0 )
		begin
			update SobekCM_Item_Viewers 
			set Exclude='true' 
			where ItemID=@ItemID and ItemViewTypeID=@Viewer2_TypeID;
		end;
	end;

	-- Exclude the third viewer
	if ( len(coalesce(@Viewer3_Type, '')) > 0 )
	begin
		-- Get the primary key for this viewer type
		declare @Viewer3_TypeID int;
		set @Viewer3_TypeID = coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = @Viewer3_Type ), -1 );

		-- Only continue if that viewer type was found
		if ( @Viewer3_TypeID > 0 )
		begin
			update SobekCM_Item_Viewers 
			set Exclude='true' 
			where ItemID=@ItemID and ItemViewTypeID=@Viewer3_TypeID;
		end;
	end;

	-- Exclude the fourth viewer
	if ( len(coalesce(@Viewer4_Type, '')) > 0 )
	begin
		-- Get the primary key for this viewer type
		declare @Viewer4_TypeID int;
		set @Viewer4_TypeID = coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = @Viewer4_Type ), -1 );

		-- Only continue if that viewer type was found
		if ( @Viewer4_TypeID > 0 )
		begin
			update SobekCM_Item_Viewers 
			set Exclude='true' 
			where ItemID=@ItemID and ItemViewTypeID=@Viewer4_TypeID;
		end;
	end;

	-- Exclude the fifth viewer
	if ( len(coalesce(@Viewer5_Type, '')) > 0 )
	begin
		-- Get the primary key for this viewer type
		declare @Viewer5_TypeID int;
		set @Viewer5_TypeID = coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = @Viewer5_Type ), -1 );

		-- Only continue if that viewer type was found
		if ( @Viewer5_TypeID > 0 )
		begin
			update SobekCM_Item_Viewers 
			set Exclude='true' 
			where ItemID=@ItemID and ItemViewTypeID=@Viewer5_TypeID;
		end;
	end;

	-- Exclude the sixth viewer
	if ( len(coalesce(@Viewer6_Type, '')) > 0 )
	begin
		-- Get the primary key for this viewer type
		declare @Viewer6_TypeID int;
		set @Viewer6_TypeID = coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = @Viewer6_Type ), -1 );

		-- Only continue if that viewer type was found
		if ( @Viewer6_TypeID > 0 )
		begin
			update SobekCM_Item_Viewers 
			set Exclude='true' 
			where ItemID=@ItemID and ItemViewTypeID=@Viewer6_TypeID;
		end;
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_RightsMD_Save_Access_Embargo_UMI]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_RightsMD_Save_Access_Embargo_UMI]
	@ItemID int,
	@Original_AccessCode varchar(25),
	@EmbargoEnd date,
	@UMI varchar(20)
AS
BEGIN

	-- Only insert if it doesn't exist
	if ( exists ( select * from Tracking_Item where ItemID=@ItemID ))
	begin
		--update existing, not updating 'original_' columns
		update Tracking_Item
		set EmbargoEnd = @EmbargoEnd, UMI=@UMI
		where ItemID=@ItemID;
	end
	else
	begin
		-- Insert ALL the data
		insert into Tracking_Item ( ItemID, Original_AccessCode, Original_EmbargoEnd, EmbargoEnd, UMI )
		values ( @ItemID, @Original_AccessCode, @EmbargoEnd, @EmbargoEnd, @UMI );
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Icon]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Icon]
	@iconid int,
	@icon_name varchar(255),
	@icon_url varchar(255),
	@link varchar(255), 
	@height int,
	@title varchar(255),
	@new_iconid int output
as
begin transaction	

	-- Does an icon with this icon name (code) exists?
    if ((select count(*) from SobekCM_Icon where icon_name = @icon_name) = 0 )
    begin     
		-- None existed, so insert a new one 
		insert into SobekCM_Icon(icon_name,icon_url, link, height, title )
		values(@icon_name, @icon_url, @link, @height, @title )
		select @new_iconid = @@identity
    end
	else
	begin
		-- Update the existing row
		update SobekCM_Icon
		set icon_url = @icon_url, link = @link, height = @height, title = @title
		where icon_name = @icon_name
   		
		-- Return this icon id
		select @new_iconid = IconID
		from SobekCM_Icon
		where icon_name = @icon_name
   end  
  
commit transaction
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Item]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Saves all the main data about an item in UFDC (but not behaviors)
-- Written by Mark Sullivan ( September 2005, Edited November 2021)
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Item]
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
		-- Does this institution already exist?
		if (( select count(*) from SobekCM_Item_Aggregation where Code = @HoldingCode ) = 0 )
		begin
			-- Add new institution
			insert into SobekCM_Item_Aggregation ( Code, [Name], ShortName, Description, ThematicHeadingID, [Type], isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
			values ( @HoldingCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
		end;
		
		-- Add the link to this holding code ( and any legitimate parent aggregations )
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @HoldingCode;		
	end;

	-- Check for Source Institution Code
	if ( len ( isnull ( @SourceCode, '' ) ) > 0 )
	begin
		-- Does this institution already exist?
		if (( select count(*) from SobekCM_Item_Aggregation where Code = @SourceCode ) = 0 )
		begin
			-- Add new institution
			insert into SobekCM_Item_Aggregation ( Code, [Name], ShortName, Description, ThematicHeadingID, [Type], isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
			values ( @SourceCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
		end;

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
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Item_Aggregation]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Stored procedure to save the basic item aggregation information
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Item_Aggregation]
	@aggregationid int,
	@code varchar(20),
	@name nvarchar(255),
	@shortname nvarchar(100),
	@description nvarchar(1000),
	@thematicHeadingId int,
	@type varchar(50),
	@isactive bit,
	@hidden bit,
	@display_options varchar(10),
	@map_search tinyint,
	@map_display tinyint,
	@oai_flag bit,
	@oai_metadata nvarchar(2000),
	@contactemail varchar(255),
	@defaultinterface varchar(10),
	@externallink nvarchar(255),
	@parentid int,
	@username varchar(100),
	@languageVariants varchar(500),
	@groupResults bit,
	@newaggregationid int output
AS
begin transaction

	-- Set flag to see if this was basically just created (either new or undeleted)
	declare @newly_added bit;
	set @newly_added = 'false';

   -- If the aggregation id is less than 1 then this is for a new aggregation
   if ((@aggregationid  < 1 ) and (( select COUNT(*) from SobekCM_Item_Aggregation where Code=@code ) = 0 ))
   begin

		-- Insert a new row
		insert into SobekCM_Item_Aggregation(Code, [Name], Shortname, Description, ThematicHeadingID, [Type], isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, OAI_Metadata, ContactEmail, HasNewItems, DefaultInterface, External_Link, DateAdded, LanguageVariants, GroupResults )
		values(@code, @name, @shortname, @description, @thematicHeadingId, @type, @isActive, @hidden, @display_options, @map_search, @map_display, @oai_flag, @oai_metadata, @contactemail, 'false', @defaultinterface, @externallink, GETDATE(), @languageVariants, @groupResults );

		-- Get the primary key
		set @newaggregationid = @@identity;
       
		-- insert the CREATED milestone
		insert into [SobekCM_Item_Aggregation_Milestones] ( AggregationID, Milestone, MilestoneDate, MilestoneUser )
		values ( @newaggregationid, 'Created', getdate(), @username );

		-- Since this was a brand new, set flag
		set @newly_added='true';
   end
   else
   begin

	  -- Add special code to indicate if this aggregation was undeleted
	  if ( exists ( select 1 from SobekCM_Item_Aggregation where Code=@Code and Deleted='true'))
	  begin
		declare @deletedid int;
		set @deletedid = ( select aggregationid from SobekCM_Item_Aggregation where Code=@Code );

		-- insert the UNDELETED milestone
		insert into [SobekCM_Item_Aggregation_Milestones] ( AggregationID, Milestone, MilestoneDate, MilestoneUser )
		values ( @deletedid, 'Created (undeleted as previously existed)', getdate(), @username );

		-- Since this was undeleted, let's make sure this collection isn't linked 
		-- to any parent collections
		delete from SobekCM_Item_Aggregation_Hierarchy
		where ChildID=@deletedid;

		-- Since this was UNDELETED, set flag
		set @newly_added='true';
	  end;

      -- Update the existing row
      update SobekCM_Item_Aggregation
      set  
		Code = @code,
		[Name] = @name,
		ShortName = @shortname,
		[Description] = @description,
		ThematicHeadingID = @thematicHeadingID,
		[Type] = @type,
		isActive = @isactive,
		Hidden = @hidden,
		DisplayOptions = @display_options,
		Map_Search = @map_search,
		Map_Display = @map_display,
		OAI_Flag = @oai_flag,
		OAI_Metadata = @oai_metadata,
		ContactEmail = @contactemail,
		DefaultInterface = @defaultinterface,
		External_Link = @externallink,
		Deleted = 'false',
		DeleteDate = null,
		LanguageVariants = @languageVariants,
		GroupResults = @groupResults
      where AggregationID = @aggregationid or Code = @code;

      -- Set the return value to the existing id
      set @newaggregationid = ( select aggregationid from SobekCM_Item_Aggregation where Code=@Code );

   end;



	-- Was a parent id provided
	if ( isnull(@parentid, -1 ) > 0 )
	begin
		-- Now, see if the link to the parent exists
		if (( select count(*) from SobekCM_Item_Aggregation_Hierarchy H where H.ParentID = @parentid and H.ChildID = @newaggregationid ) < 1 )
		begin			
			insert into SobekCM_Item_Aggregation_Hierarchy ( ParentID, ChildID )
			values ( @parentid, @newaggregationid );
		end;
	end;

	-- If this was newly added (new or undeleted), ensure permissions and other things copied over from parent
	if ( @newly_added = 'true' )
	begin
		-- There should ALWAYS be a parent for new collections, even if it is the ALL collection
		if ( isnull(@parentid, -1 ) < 0 )
		begin
			set @parentid = ( select AggregationID from SobekCM_Item_Aggregation where Code='ALL' );
		end;

		-- Since this is NEW, set the group results based on the parent
		update SobekCM_Item_Aggregation
		set GroupResults = ( select GroupResults from SobekCM_Item_Aggregation where AggregationID=@parentid )
		where AggregationID=@newaggregationid;

			-- Add individual user permissions first
			insert into mySobek_User_Edit_Aggregation ( UserID, AggregationID, CanSelect, CanEditItems, 
				IsCurator, IsAdmin, CanEditMetadata, CanEditBehaviors, CanPerformQc, 
				CanUploadFiles, CanChangeVisibility, CanDelete )
			select UserID, @newaggregationid, CanSelect, CanEditItems, 
				IsCurator, IsAdmin, CanEditMetadata, CanEditBehaviors, CanPerformQc, 
				CanUploadFiles, CanChangeVisibility, CanDelete
			from mySobek_User_Edit_Aggregation A
			where ( AggregationID = @parentid )
			  and ( not exists ( select * from mySobek_User_Edit_Aggregation L where L.UserID=A.UserID and L.AggregationID=@newaggregationid ))
			  and (    ( CanEditMetadata='true' ) 
	                or ( CanEditBehaviors='true' )
	                or ( CanPerformQc='true' )
	                or ( CanUploadFiles='true' )
	                or ( CanChangeVisibility='true' )
	                or ( IsCurator='true' )
	                or ( IsAdmin='true' ));

			-- Add user group permissions next 
			insert into mySobek_User_Group_Edit_Aggregation ( UserGroupID, AggregationID, CanSelect, CanEditItems, 
				IsCurator, IsAdmin, CanEditMetadata, CanEditBehaviors, CanPerformQc, 
				CanUploadFiles, CanChangeVisibility, CanDelete )
			select UserGroupID, @newaggregationid, CanSelect, CanEditItems, 
				IsCurator, IsAdmin, CanEditMetadata, CanEditBehaviors, CanPerformQc, 
				CanUploadFiles, CanChangeVisibility, CanDelete
			from mySobek_User_Group_Edit_Aggregation A
			where ( AggregationID = @parentid )
			  and ( not exists ( select * from mySobek_User_Group_Edit_Aggregation L where L.UserGroupID=A.UserGroupID and L.AggregationID=@newaggregationid ))
			  and (    ( CanEditMetadata='true' ) 
	                or ( CanEditBehaviors='true' )
	                or ( CanPerformQc='true' )
	                or ( CanUploadFiles='true' )
	                or ( CanChangeVisibility='true' )
	                or ( IsCurator='true' )
	                or ( IsAdmin='true' ));

			-- Copy over the facet fields
			insert into SobekCM_Item_Aggregation_Facets ( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
			select @newaggregationid, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions
			from SobekCM_Item_Aggregation_Facets
			where AggregationID=@parentid;

			-- Copy over the results views from the parent
			insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
			select @newaggregationid, ItemAggregationResultTypeID, DefaultView
			from SobekCM_Item_Aggregation_Result_Views
			where AggregationID=@parentid;

			-- Now, add the result view fields from the parent
			insert into SobekCM_Item_Aggregation_Result_Fields ( ItemAggregationResultID, MetadataTypeID, OverrideDisplayTerm, DisplayOrder, DisplayOptions )
			select V2.ItemAggregationResultID, F1.MetadataTypeID, F1.OverrideDisplayTerm, F1.DisplayOrder, F1.DisplayOptions
			from SobekCM_Item_Aggregation_Result_Views V1, SobekCM_Item_Aggregation_Result_Fields F1, SobekCM_Item_Aggregation_Result_Views V2
			where V1.ItemAggregationResultID=F1.ItemAggregationResultID
			  and V1.AggregationID=@parentid
			  and V2.ItemAggregationResultTypeID=V1.ItemAggregationResultTypeID
			  and V2.AggregationID=@newaggregationid;
		end;

commit transaction;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Item_Aggregation_Alias]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Procedure either adds a forwarding or edits an existing forward
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Item_Aggregation_Alias]
	@alias varchar(50),
	@aggregation_code varchar(20)	
AS
BEGIN
	
	-- Get the aggregation id from the aggregation code
	if (( select count(*) from SobekCM_Item_Aggregation where Code=@aggregation_code and Deleted='false' ) = 1 )
	begin
		-- Get the aggregation id
		declare @aggregationid int;
		select @aggregationid = AggregationID from SobekCM_Item_Aggregation where Code=@aggregation_code;

		-- Does this alias already exist?
		if (( select count(*) from SobekCM_Item_Aggregation_Alias where AggregationAlias=@alias ) > 0 )
		begin
			-- Update existing
			update SobekCM_Item_Aggregation_Alias
			set AggregationID = @aggregationID
			where AggregationAlias = @alias;
		end
		else
		begin
			-- Not existing, so add new one
			insert into SobekCM_Item_Aggregation_Alias ( AggregationAlias, AggregationID )
			values ( @alias, @aggregationid );
		end;
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Item_Aggregation_Facets]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Stored procedure to save the item aggregation facet information
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Item_Aggregation_Facets]
	@code varchar(20),
	@facet1_type varchar(100),
	@facet1_display varchar(100),
	@facet2_type varchar(100),
	@facet2_display varchar(100),
	@facet3_type varchar(100),
	@facet3_display varchar(100),
	@facet4_type varchar(100),
	@facet4_display varchar(100),
	@facet5_type varchar(100),
	@facet5_display varchar(100),
	@facet6_type varchar(100),
	@facet6_display varchar(100),
	@facet7_type varchar(100),
	@facet7_display varchar(100),
	@facet8_type varchar(100),
	@facet8_display varchar(100)
AS
begin transaction

	-- Only continue if there is a match on the aggregation code
	if ( exists ( select 1 from SobekCM_Item_Aggregation where Code = @code ))
	begin
		declare @id int;
		set @id = ( select AggregationID from SobekCM_Item_Aggregation where Code = @code );

		-- Keep list of any existing facets
		declare @existing_facets table(MetadataTypeID int primary key, ExTerm varchar(100), ExOrder int, ExOptions varchar(2000), StillExists bit );
		insert into @existing_facets 
		select MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions, 'false'
		from SobekCM_Item_Aggregation_Facets V
		where ( V.AggregationID=@id );

		-- Add the FIRST facet
		if (( len(@facet1_type) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=@facet1_type or SobekCode=@facet1_type)))
		begin
			declare @facet1_id int;
			set @facet1_id = ( select MetadataTypeID from SobekCM_Metadata_Types where MetadataName=@facet1_type or SobekCode=@facet1_type );

			-- If the standard facet term is the same as what came in, just clear it
			declare @facet1_standard_display varchar(100);
			set @facet1_standard_display = ( select FacetTerm from SobekCM_Metadata_Types where MetadataTypeID=@facet1_id);
			if ( @facet1_standard_display = @facet1_display ) set @facet1_display = null;

			-- Does this facet already exist?
			if ( not exists ( select 1 from @existing_facets where MetadataTypeID=@facet1_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( @id, @facet1_id, @facet1_display, 1, '' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_facets	
				set StillExists='true'
				where MetadataTypeID=@facet1_id;

				-- Order and display term may have changed though
				update SobekCM_Item_Aggregation_Facets 
				set FacetOrder=1, OverrideFacetTerm=@facet1_display
				where ( MetadataTypeID = @facet1_id )
				  and ( AggregationID = @id );
			end;
		end;

		-- Add the SECOND facet
		if (( len(@facet2_type) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=@facet2_type or SobekCode=@facet2_type)))
		begin
			declare @facet2_id int;
			set @facet2_id = ( select MetadataTypeID from SobekCM_Metadata_Types where MetadataName=@facet2_type or SobekCode=@facet2_type );

			-- If the standard facet term is the same as what came in, just clear it
			declare @facet2_standard_display varchar(100);
			set @facet2_standard_display = ( select FacetTerm from SobekCM_Metadata_Types where MetadataTypeID=@facet2_id);
			if ( @facet2_standard_display = @facet2_display ) set @facet2_display = null;

			-- Does this facet already exist?
			if ( not exists ( select 1 from @existing_facets where MetadataTypeID=@facet2_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( @id, @facet2_id, @facet2_display, 2, '' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_facets	
				set StillExists='true'
				where MetadataTypeID=@facet2_id;

				-- Order and display term may have changed though
				update SobekCM_Item_Aggregation_Facets 
				set FacetOrder=2, OverrideFacetTerm=@facet2_display
				where ( MetadataTypeID = @facet2_id )
				  and ( AggregationID = @id );
			end;
		end;

		-- Add the THIRD facet
		if (( len(@facet3_type ) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=@facet3_type or SobekCode=@facet3_type)))
		begin
			declare @facet3_id int;
			set @facet3_id = ( select MetadataTypeID from SobekCM_Metadata_Types where MetadataName=@facet3_type or SobekCode=@facet3_type );

			-- If the standard facet term is the same as what came in, just clear it
			declare @facet3_standard_display varchar(100);
			set @facet3_standard_display = ( select FacetTerm from SobekCM_Metadata_Types where MetadataTypeID=@facet3_id);
			if ( @facet3_standard_display = @facet3_display ) set @facet3_display = null;

			-- Does this facet already exist?
			if ( not exists ( select 1 from @existing_facets where MetadataTypeID=@facet3_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( @id, @facet3_id, @facet3_display, 3, '' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_facets	
				set StillExists='true'
				where MetadataTypeID=@facet3_id;

				-- Order and display term may have changed though
				update SobekCM_Item_Aggregation_Facets 
				set FacetOrder=3, OverrideFacetTerm=@facet3_display
				where ( MetadataTypeID = @facet3_id )
				  and ( AggregationID = @id );
			end;
		end;
		
		-- Add the FOURTH facet
		if (( len(@facet1_type) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=@facet4_type or SobekCode=@facet4_type)))
		begin
			declare @facet4_id int;
			set @facet4_id = ( select MetadataTypeID from SobekCM_Metadata_Types where MetadataName=@facet4_type or SobekCode=@facet4_type );

			-- If the standard facet term is the same as what came in, just clear it
			declare @facet4_standard_display varchar(100);
			set @facet4_standard_display = ( select FacetTerm from SobekCM_Metadata_Types where MetadataTypeID=@facet4_id);
			if ( @facet4_standard_display = @facet4_display ) set @facet4_display = null;

			-- Does this facet already exist?
			if ( not exists ( select 1 from @existing_facets where MetadataTypeID=@facet4_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( @id, @facet4_id, @facet4_display, 4, '' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_facets	
				set StillExists='true'
				where MetadataTypeID=@facet4_id;

				-- Order and display term may have changed though
				update SobekCM_Item_Aggregation_Facets 
				set FacetOrder=4, OverrideFacetTerm=@facet4_display
				where ( MetadataTypeID = @facet4_id )
				  and ( AggregationID = @id );
			end;
		end;

		-- Add the FIFTH facet
		if (( len(@facet5_type) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=@facet5_type or SobekCode=@facet5_type)))
		begin
			declare @facet5_id int;
			set @facet5_id = ( select MetadataTypeID from SobekCM_Metadata_Types where MetadataName=@facet5_type or SobekCode=@facet5_type );

			-- If the standard facet term is the same as what came in, just clear it
			declare @facet5_standard_display varchar(100);
			set @facet5_standard_display = ( select FacetTerm from SobekCM_Metadata_Types where MetadataTypeID=@facet5_id);
			if ( @facet5_standard_display = @facet5_display ) set @facet5_display = null;

			-- Does this facet already exist?
			if ( not exists ( select 1 from @existing_facets where MetadataTypeID=@facet5_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( @id, @facet5_id, @facet5_display, 5, '' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_facets	
				set StillExists='true'
				where MetadataTypeID=@facet5_id;

				-- Order and display term may have changed though
				update SobekCM_Item_Aggregation_Facets 
				set FacetOrder=5, OverrideFacetTerm=@facet5_display
				where ( MetadataTypeID = @facet5_id )
				  and ( AggregationID = @id );
			end;
		end;

		-- Add the SIXTH facet
		if (( len(@facet6_type) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=@facet6_type or SobekCode=@facet6_type)))
		begin
			declare @facet6_id int;
			set @facet6_id = ( select MetadataTypeID from SobekCM_Metadata_Types where MetadataName=@facet6_type or SobekCode=@facet6_type );

			-- If the standard facet term is the same as what came in, just clear it
			declare @facet6_standard_display varchar(100);
			set @facet6_standard_display = ( select FacetTerm from SobekCM_Metadata_Types where MetadataTypeID=@facet6_id);
			if ( @facet6_standard_display = @facet6_display ) set @facet6_display = null;

			-- Does this facet already exist?
			if ( not exists ( select 1 from @existing_facets where MetadataTypeID=@facet6_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( @id, @facet6_id, @facet6_display, 6, '' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_facets	
				set StillExists='true'
				where MetadataTypeID=@facet1_id;

				-- Order and display term may have changed though
				update SobekCM_Item_Aggregation_Facets 
				set FacetOrder=6, OverrideFacetTerm=@facet6_display
				where ( MetadataTypeID = @facet6_id )
				  and ( AggregationID = @id );
			end;
		end;

		-- Add the SEVENTH facet
		if (( len(@facet7_type) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=@facet7_type or SobekCode=@facet7_type)))
		begin
			declare @facet7_id int;
			set @facet7_id = ( select MetadataTypeID from SobekCM_Metadata_Types where MetadataName=@facet7_type or SobekCode=@facet7_type );

			-- If the standard facet term is the same as what came in, just clear it
			declare @facet7_standard_display varchar(100);
			set @facet7_standard_display = ( select FacetTerm from SobekCM_Metadata_Types where MetadataTypeID=@facet7_id);
			if ( @facet7_standard_display = @facet7_display ) set @facet7_display = null;

			-- Does this facet already exist?
			if ( not exists ( select 1 from @existing_facets where MetadataTypeID=@facet7_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( @id, @facet7_id, @facet7_display, 7, '' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_facets	
				set StillExists='true'
				where MetadataTypeID=@facet7_id;

				-- Order and display term may have changed though
				update SobekCM_Item_Aggregation_Facets 
				set FacetOrder=1, OverrideFacetTerm=@facet7_display
				where ( MetadataTypeID = @facet7_id )
				  and ( AggregationID = @id );
			end;
		end;

		-- Add the EIGHTH facet
		if (( len(@facet8_type) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=@facet8_type or SobekCode=@facet8_type)))
		begin
			declare @facet8_id int;
			set @facet8_id = ( select MetadataTypeID from SobekCM_Metadata_Types where MetadataName=@facet8_type or SobekCode=@facet8_type);

			-- If the standard facet term is the same as what came in, just clear it
			declare @facet8_standard_display varchar(100);
			set @facet8_standard_display = ( select FacetTerm from SobekCM_Metadata_Types where MetadataTypeID=@facet8_id);
			if ( @facet8_standard_display = @facet8_display ) set @facet8_display = null;

			-- Does this facet already exist?
			if ( not exists ( select 1 from @existing_facets where MetadataTypeID=@facet8_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( @id, @facet8_id, @facet8_display, 8, '' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_facets	
				set StillExists='true'
				where MetadataTypeID=@facet8_id;

				-- Order and display term may have changed though
				update SobekCM_Item_Aggregation_Facets 
				set FacetOrder=8, OverrideFacetTerm=@facet8_display
				where ( MetadataTypeID = @facet8_id )
				  and ( AggregationID = @id );
			end;
		end;

		-- Now, remove any 
		if (( select count(*) from @existing_facets ) > 0 )
		begin
			-- First delete the fields
			delete from SobekCM_Item_Aggregation_Facets
			where MetadataTypeID in ( select MetadataTypeID from @existing_facets V where V.StillExists='false')
			  and AggregationID = @id;
		end;
	end;

commit transaction;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Item_Aggregation_ResultViews]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Stored procedure to save the basic item aggregation information
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Item_Aggregation_ResultViews]
	@code varchar(20),
	@results1 varchar(50),
	@results2 varchar(50),
	@results3 varchar(50),
	@results4 varchar(50),
	@results5 varchar(50),
	@results6 varchar(50),
	@results7 varchar(50),
	@results8 varchar(50),
	@results9 varchar(50),
	@results10 varchar(50),
	@default varchar(50)
AS
begin transaction

	-- Only continue if there is a match on the aggregation code
	if ( exists ( select 1 from SobekCM_Item_Aggregation where Code = @code ))
	begin
		declare @id int;
		set @id = ( select AggregationID from SobekCM_Item_Aggregation where Code = @code );

		-- Keep list of any existing view
		declare @existing_views table(ResultTypeId int primary key, AggrSpecificId int, StillExisting bit );
		insert into @existing_views 
		select ItemAggregationResultTypeID, ItemAggregationResultID, 'false'
		from SobekCM_Item_Aggregation_Result_Views V
		where ( V.AggregationID=@id );

		-- Add the FIRST results view
		if (( len(@results1) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results1)))
		begin
			declare @results1_id int;
			set @results1_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results1 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results1_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results1_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results1_id;
			end;
		end;

		-- Add the SECOND results view
		if (( len(@results2) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results2)))
		begin
			declare @results2_id int;
			set @results2_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results2 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results2_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results2_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results2_id;
			end;
		end;

		-- Add the THIRD results view
		if (( len(@results3) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results3)))
		begin
			declare @results3_id int;
			set @results3_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results3 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results3_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results3_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results3_id;
			end;
		end;

		-- Add the FOURTH results view
		if (( len(@results4) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results4)))
		begin
			declare @results4_id int;
			set @results4_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results4 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results4_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results4_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results4_id;
			end;
		end;

		-- Add the FIFTH results view
		if (( len(@results5) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results5)))
		begin
			declare @results5_id int;
			set @results5_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results5 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results5_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results5_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results5_id;
			end;
		end;

		-- Add the SIXTH results view
		if (( len(@results6) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results6)))
		begin
			declare @results6_id int;
			set @results6_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results6 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results6_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results6_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results6_id;
			end;
		end;

		-- Add the SEVENTH results view
		if (( len(@results7) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results7)))
		begin
			declare @results7_id int;
			set @results7_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results7 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results7_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results7_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results7_id;
			end;
		end;

		-- Add the EIGHTH results view
		if (( len(@results8) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results8)))
		begin
			declare @results8_id int;
			set @results8_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results8 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results8_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results8_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results8_id;
			end;
		end;

		-- Add the NINTH results view
		if (( len(@results9) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results9)))
		begin
			declare @results9_id int;
			set @results9_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results9 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results9_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results9_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results9_id;
			end;
		end;

		-- Add the TENTH results view
		if (( len(@results10) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results10)))
		begin
			declare @results10_id int;
			set @results10_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results10 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results10_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results10_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results10_id;
			end;
		end;

		-- Now, remove any 
		if (( select count(*) from @existing_views ) > 0 )
		begin
			-- First delete the fields
			delete from SobekCM_Item_Aggregation_Result_Fields
			where exists ( select 1 from @existing_views V where V.StillExisting='false' and V.AggrSpecificId=ItemAggregationResultID);

			-- Now, delete this results view
			delete from SobekCM_Item_Aggregation_Result_Views 
			where exists ( select 1 from @existing_views V where V.StillExisting='false' and V.AggrSpecificId=ItemAggregationResultID);

		end;

		-- Set the DEFAULT view
		if (( len(@default) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@default )))
		begin
			-- Get the ID for the default
			declare @default_id int;
			set @default_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@default );

			-- Update, if it exists
			update SobekCM_Item_Aggregation_Result_Views
			set DefaultView = 'false'
			where AggregationID = @id and ItemAggregationResultTypeID != @default_id;

			-- Update, if it exists
			update SobekCM_Item_Aggregation_Result_Views
			set DefaultView = 'true'
			where AggregationID = @id and ItemAggregationResultTypeID = @default_id;
		end;

	end;

commit transaction;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Item_Behaviors]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



-- Saves the behavior information about an item in this library
-- Written by Mark Sullivan 
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Item_Behaviors]
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
		-- Does this institution already exist?
		if (( select count(*) from SobekCM_Item_Aggregation where Code = @HoldingCode ) = 0 )
		begin
			-- Add new institution
			insert into SobekCM_Item_Aggregation ( Code, [Name], ShortName, Description, ThematicHeadingID, [Type], isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
			values ( @HoldingCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
		end;
		
		-- Add the link to this holding code ( and any legitimate parent aggregations )
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @HoldingCode;		
	end;

	-- Check for Source Institution Code
	if ( len ( isnull ( @SourceCode, '' ) ) > 0 )
	begin
		-- Does this institution already exist?
		if (( select count(*) from SobekCM_Item_Aggregation where Code = @SourceCode ) = 0 )
		begin
			-- Add new institution
			insert into SobekCM_Item_Aggregation ( Code, [Name], ShortName, Description, ThematicHeadingID, [Type], isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
			values ( @SourceCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
		end;

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
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Item_Behaviors_Minimal]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Saves the behavior information about an item in this library
-- Written by Mark Sullivan 
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Item_Behaviors_Minimal]
	@ItemID int,
	@TextSearchable bit
AS
begin transaction;

	--Update the main item
	update SobekCM_Item
	set TextSearchable = @TextSearchable
	where ( ItemID = @ItemID );

commit transaction;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Item_Footprint]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Procedure links an item to a region
-- Written by Mark Sullivan ( August 2007 )
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Item_Footprint]
	@ItemID int,
	@point_latitude float,
	@point_longitude float,
	@rect_latitude_A float,
	@rect_longitude_A float,
	@rect_latitude_B float,
	@rect_longitude_B float,
	@segment_kml varchar(max)
AS
begin transaction

	insert into SobekCM_Item_Footprint( ItemID, Point_Latitude, Point_Longitude, Rect_Latitude_A, Rect_Longitude_A, Rect_Latitude_B, Rect_Longitude_B, Segment_KML )
	values ( @itemid, @point_latitude, @point_longitude, @rect_latitude_a, @rect_longitude_a, @rect_latitude_b, @rect_longitude_b, @segment_kml )

commit transaction
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Item_Group]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Saves all the main data about a group of items in UFDC
-- Written by Mark Sullivan (September 2006, Modified October 2011 )
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Item_Group]
	@BibID varchar(10),
	@GroupTitle nvarchar(500),
	@SortTitle varchar(255),
	@Type varchar(50),
	@File_Location varchar(100),
	@OCLC_Number bigint,
	@ALEPH_Number int,
	@Group_Thumbnail varchar(500),
	@Large_Format bit,
	@Track_By_Month bit,
	@Never_Overlay_Record bit,
	@Update_Existing bit,
	@PrimaryIdentifierType nvarchar(50),
	@PrimaryIdentifier nvarchar(100),
	@GroupID int output,
	@New_BibID varchar(10) output,
	@New_Group bit output
AS
begin transaction

	-- Set the return BibID value first
	set @New_BibID = @BibID;
	set @New_Group = 'false';

	-- If this group does not exists (BibID) insert, else update
	if (( select count(*) from SobekCM_Item_Group  where ( BibID = @BibID ))  < 1 )
	begin	
		-- Verify the BibID is a complete bibid, otherwise find the next one
		if ( LEN(@bibid) < 10 )
		begin
			declare @next_bibid_number int;

			-- Find the next bibid number
			select @next_bibid_number = isnull(CAST(REPLACE(MAX(BibID), @bibid, '') as int) + 1,-1)
			from SobekCM_Item_Group
			where BibID like @bibid + '%';
			
			-- If no matches to this BibID, just start at 0000001
			if ( @next_bibid_number < 0 )
			begin
				select @New_BibID = @bibid + RIGHT('00000001', 10-LEN(@bibid));
			end
			else
			begin
				select @New_BibID = @bibid + RIGHT('00000000' + (CAST( @next_bibid_number as varchar(10))), 10-LEN(@bibid));
			end;
		end;
		
		-- Compute the file location if needed
		if ( LEN(@File_Location) = 0 )
		begin
			set @File_Location = SUBSTRING(@New_BibID,1 ,2 ) + '\' + SUBSTRING(@New_BibID,3,2) + '\' + SUBSTRING(@New_BibID,5,2) + '\' + SUBSTRING(@New_BibID,7,2) + '\' + SUBSTRING(@New_BibID,9,2);
		end;
		
		-- Add the values to the main SobekCM_Item table first
		insert into SobekCM_Item_Group ( BibID, GroupTitle, Deleted, [Type], SortTitle, ItemCount, File_Location, GroupCreateDate, OCLC_Number, ALEPH_Number, GroupThumbnail, Track_By_Month, Large_Format, Never_Overlay_Record, Primary_Identifier_Type, Primary_Identifier, LastFourInt )
		values ( @New_BibID, @GroupTitle, 0, @Type, @SortTitle, 0, @File_Location, getdate(), @OCLC_Number, @ALEPH_Number, @Group_Thumbnail, @Track_By_Month, @Large_Format, @Never_Overlay_Record, @PrimaryIdentifierType, @PrimaryIdentifier, cast(substring(@BibID, 7, 4) as smallint ) );

		-- Get the item id identifier for this row
		set @GroupID = @@identity;
		set @New_Group = 'true';
	end
	else
	begin

		-- This already existed, so just return the existing group id
		select @GroupID = GroupID
		from SobekCM_Item_Group
		where BibID = @BibID;

		-- If we are supposed to update it, do this
		if ( @Update_Existing = 'true' )
		begin

			update SobekCM_Item_Group
			set GroupTitle=@GroupTitle, [Type]=@Type, SortTitle=@SortTitle, OCLC_Number=@OCLC_Number, ALEPH_Number=@ALEPH_Number, GroupThumbnail=@Group_Thumbnail, Track_By_Month = @Track_By_Month, Large_Format=@Large_Format, Never_Overlay_Record = @Never_Overlay_Record, Primary_Identifier_Type=@PrimaryIdentifierType, Primary_Identifier=@PrimaryIdentifier
			where BibID = @BibID;

		end;
		
		set @New_Group = 'false';
	end;

commit transaction;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Item_Group_Web_Skins]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Saves all the web skin data about a group of items in UFDC
-- Written by Mark Sullivan (September 2006, Modified August 2010 )
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Item_Group_Web_Skins]
	@GroupID int,
	@Primary_WebSkin varchar(20),
	@Alt_WebSkin1 varchar(20),
	@Alt_WebSkin2 varchar(20),
	@Alt_WebSkin3 varchar(20),
	@Alt_WebSkin4 varchar(20),
	@Alt_WebSkin5 varchar(20),
	@Alt_WebSkin6 varchar(20),
	@Alt_WebSkin7 varchar(20),
	@Alt_WebSkin8 varchar(20),
	@Alt_WebSkin9 varchar(20)	
AS
begin transaction

	-- Clear existing web skins
	delete from SobekCM_Item_Group_Web_Skin_Link 
	where GroupID = @GroupID

	-- Add the first web skin to this object  (this requires the web skins have been pre-established )
	declare @InterfaceID int
	if ( len( isnull( @Primary_WebSkin, '' )) > 0 ) 
	begin
		-- Get the Interface ID for this interface
		select @InterfaceID = WebSkinID from SobekCM_Web_Skin where WebSkinCode = @Primary_WebSkin

		-- Ensure this web skin exists
		if ( ISNULL(@InterfaceID,-1) > 0 )
		begin		
			-- Tie this item to this interface
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, [Sequence] )
			values ( @GroupID, @InterfaceID, 1 )
		end
	end

	-- Add the second web skin to this object  (this requires the web skins have been pre-established )
	if ( len( isnull( @Alt_WebSkin1, '' )) > 0 ) 
	begin	
		-- Get the Interface ID for this interface
		select @InterfaceID = WebSkinID from SobekCM_Web_Skin where WebSkinCode = @Alt_WebSkin1

		-- Ensure this web skin exists
		if ( ISNULL(@InterfaceID,-1) > 0 )
		begin		
			-- Tie this item to this interface
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, [Sequence] )
			values ( @GroupID, @InterfaceID, 2 )
		end
	end

	-- Add the third web skin to this object  (this requires the web skins have been pre-established )
	if ( len( isnull( @Alt_WebSkin2, '' )) > 0 ) 
	begin		
		-- Get the Interface ID for this interface
		select @InterfaceID = WebSkinID from SobekCM_Web_Skin where WebSkinCode = @Alt_WebSkin2

		-- Ensure this web skin exists
		if ( ISNULL(@InterfaceID,-1) > 0 )
		begin		
			-- Tie this item to this interface
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, [Sequence] )
			values ( @GroupID, @InterfaceID, 3 )
		end
	end

	-- Add the fourth web skin to this object  (this requires the web skins have been pre-established )
	if ( len( isnull( @Alt_WebSkin3, '' )) > 0 ) 
	begin	
		-- Get the Interface ID for this interface
		select @InterfaceID = WebSkinID from SobekCM_Web_Skin where WebSkinCode = @Alt_WebSkin3

		-- Ensure this web skin exists
		if ( ISNULL(@InterfaceID,-1) > 0 )
		begin		
			-- Tie this item to this interface
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, [Sequence] )
			values ( @GroupID, @InterfaceID, 4 )
		end
	end
	
	-- Add the fifth web skin to this object  (this requires the web skins have been pre-established )
	if ( len( isnull( @Alt_WebSkin4, '' )) > 0 ) 
	begin	
		-- Get the Interface ID for this interface
		select @InterfaceID = WebSkinID from SobekCM_Web_Skin where WebSkinCode = @Alt_WebSkin4

		-- Ensure this web skin exists
		if ( ISNULL(@InterfaceID,-1) > 0 )
		begin		
			-- Tie this item to this interface
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, [Sequence] )
			values ( @GroupID, @InterfaceID, 5 )
		end
	end

	-- Add the sixth web skin to this object  (this requires the web skins have been pre-established )
	if ( len( isnull( @Alt_WebSkin5, '' )) > 0 ) 
	begin		
		-- Get the Interface ID for this interface
		select @InterfaceID = WebSkinID from SobekCM_Web_Skin where WebSkinCode = @Alt_WebSkin5

		-- Ensure this web skin exists
		if ( ISNULL(@InterfaceID,-1) > 0 )
		begin		
			-- Tie this item to this interface
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, [Sequence] )
			values ( @GroupID, @InterfaceID, 6 )
		end
	end

	-- Add the seventh web skin to this object  (this requires the web skins have been pre-established )
	if ( len( isnull( @Alt_WebSkin6, '' )) > 0 ) 
	begin	
		-- Get the Interface ID for this interface
		select @InterfaceID = WebSkinID from SobekCM_Web_Skin where WebSkinCode = @Alt_WebSkin6

		-- Ensure this web skin exists
		if ( ISNULL(@InterfaceID,-1) > 0 )
		begin		
			-- Tie this item to this interface
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, [Sequence] )
			values ( @GroupID, @InterfaceID, 7 )
		end
	end

-- Add the eight web skin to this object  (this requires the web skins have been pre-established )
	if ( len( isnull( @Alt_WebSkin7, '' )) > 0 ) 
	begin	
		-- Get the Interface ID for this interface
		select @InterfaceID = WebSkinID from SobekCM_Web_Skin where WebSkinCode = @Alt_WebSkin7

		-- Ensure this web skin exists
		if ( ISNULL(@InterfaceID,-1) > 0 )
		begin		
			-- Tie this item to this interface
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, [Sequence] )
			values ( @GroupID, @InterfaceID, 8 )
		end
	end

	-- Add the ninth web skin to this object  (this requires the web skins have been pre-established )
	if ( len( isnull( @Alt_WebSkin8, '' )) > 0 ) 
	begin		
		-- Get the Interface ID for this interface
		select @InterfaceID = WebSkinID from SobekCM_Web_Skin where WebSkinCode = @Alt_WebSkin8

		-- Ensure this web skin exists
		if ( ISNULL(@InterfaceID,-1) > 0 )
		begin		
			-- Tie this item to this interface
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, [Sequence] )
			values ( @GroupID, @InterfaceID, 9 )
		end
	end

	-- Add the tenth web skin to this object  (this requires the web skins have been pre-established )
	if ( len( isnull( @Alt_WebSkin9, '' )) > 0 ) 
	begin	
		-- Get the Interface ID for this interface
		select @InterfaceID = WebSkinID from SobekCM_Web_Skin where WebSkinCode = @Alt_WebSkin9

		-- Ensure this web skin exists
		if ( ISNULL(@InterfaceID,-1) > 0 )
		begin		
			-- Tie this item to this interface
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, [Sequence] )
			values ( @GroupID, @InterfaceID, 10 )
		end
	end		
commit transaction
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Item_Item_Aggregation_Link]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add a link to the item aggregation (and all parents)
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Item_Item_Aggregation_Link]
	@itemid int,
	@code varchar(20)
AS
begin

	-- Only continue if the code exists
	if ( len( isnull( @code,'')) > 0 )
	begin
		if (( select count(*) from SobekCM_Item_Aggregation where Code=@code and Deleted='false' ) = 1 )
		begin
			-- Get the ID for this aggregation code
			declare @AggregationID int;
			select @AggregationID = AggregationID from SobekCM_Item_Aggregation where Code = @code;

			-- Make sure the link does not already exist (two collection codes match)
			if (( select count(*) from SobekCM_Item_Aggregation_Item_Link where AggregationID = @AggregationID and ItemID = @ItemID ) = 0 )
			begin
				-- Tie this item to this primary collection
				insert into SobekCM_Item_Aggregation_Item_Link ( AggregationID, ItemID, impliedLink )
				values (  @AggregationID, @ItemID, 'false' );
			end
			else
			begin
				-- Make sure this does not say implied, since this was explicitly connected
				update SobekCM_Item_Aggregation_Item_Link
				set impliedLink = 'false'
				where ( AggregationID = @AggregationID ) and ( ItemID = @ItemID );
			end;
			
			-- Update the last create date time
			update SobekCM_Item_Aggregation
			set LastItemAdded = ( select CreateDate from SobekCM_Item where ItemID=@itemid )
			where AggregationID = @AggregationID
			  and LastItemAdded < ( select CreateDate from SobekCM_Item where ItemID=@itemid );

			-- Select parent codes
			select P.Code, P.AggregationID, Hierarchy=1
			into #TEMP_PARENTS
			from SobekCM_Item_Aggregation C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( C.Code = @code )
			  and ( H.Search_Parent_Only = 'false' );

			-- Select the grandparent codes
			insert into #TEMP_PARENTS ( Code, AggregationID, Hierarchy)
			select P.Code, P.AggregationID, 2 
			from #TEMP_PARENTS C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( H.Search_Parent_Only = 'false' );

			-- Select the grand-grandparent codes
			insert into #TEMP_PARENTS ( Code, AggregationID, Hierarchy)
			select P.Code, P.AggregationID, 3
			from #TEMP_PARENTS C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( C.Hierarchy = 2 )
			  and ( H.Search_Parent_Only = 'false' );

			-- Select the grand-grand-grandparent codes
			insert into #TEMP_PARENTS ( Code, AggregationID, Hierarchy)
			select P.Code, P.AggregationID, 4
			from #TEMP_PARENTS C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( C.Hierarchy = 3 )
			  and ( H.Search_Parent_Only = 'false' );

			-- Insert the link anywhere it does not exist
			insert into SobekCM_Item_Aggregation_Item_Link ( AggregationID, ItemID, impliedLink )
			select AggregationID, @itemid, 'true'
			from #TEMP_PARENTS P
			where not exists ( select * 
								from SobekCM_Item_Aggregation_Item_Link L
								where ( P.AggregationID = L.AggregationID )
								  and ( L.ItemID = @itemID ));
								  
			-- Also update the last create date
			update SobekCM_Item_Aggregation
			set LastItemAdded = ( select CreateDate from SobekCM_Item where ItemID=@itemid )
			where exists ( select * from #TEMP_PARENTS T where T.AggregationID=SobekCM_Item_Aggregation.AggregationID )
			  and LastItemAdded < ( select CreateDate from SobekCM_Item where ItemID=@itemid );

			-- drop the temporary table
			drop table #TEMP_PARENTS;
		end;
	end;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Item_User_Group_Permissions]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Item_User_Group_Permissions]
	@ItemId int,
	@UserGroupId int,
	@isOwner bit,
	@canView bit,
	@canEditMetadata bit,
	@canEditBehaviors bit,
	@canPerformQc bit,
	@canUploadFiles bit,
	@canChangeVisibility bit,
	@canDelete bit,
	@customPermissions varchar(max),
	@isDefaultPermissions bit
AS
BEGIN

	-- Does a similar entry already exist?
	if (( select count(*) from mySobek_User_Group_Item_Permissions where ItemId=@ItemId and UserGroupID=@UserGroupId) > 0 )
	begin
		update mySobek_User_Group_Item_Permissions
		set isOwner=@isOwner, canView=@canView, canEditMetadata=@canEditMetadata, canEditBehaviors=@canEditBehaviors, 
		    canPerformQc=@canPerformQc, canUploadFiles=@canUploadFiles, canChangeVisibility=@canChangeVisibility,
			canDelete=@canDelete, customPermissions=@customPermissions, isDefaultPermissions=@isDefaultPermissions
		where ItemId=@ItemId and UserGroupId=@UserGroupId;
	end
	else
	begin
		insert into mySobek_User_Group_Item_Permissions ( UserGroupID, ItemID, isOwner, canView, canEditMetadata, canEditBehaviors, canPerformQc, canUploadFiles, canChangeVisibility, canDelete, customPermissions, isDefaultPermissions )
		values ( @UserGroupId, @ItemId, @isOwner, @canView, @canEditMetadata, @canEditBehaviors, @canPerformQc, @canUploadFiles, @canChangeVisibility, @canDelete, @customPermissions, @isDefaultPermissions);
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Item_User_Permissions]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Item_User_Permissions]
	@ItemId int,
	@UserId int,
	@isOwner bit,
	@canView bit,
	@canEditMetadata bit,
	@canEditBehaviors bit,
	@canPerformQc bit,
	@canUploadFiles bit,
	@canChangeVisibility bit,
	@canDelete bit,
	@customPermissions varchar(max)
AS
BEGIN

	-- Does a similar entry already exist?
	if (( select count(*) from mySobek_User_Item_Permissions where ItemId=@ItemId and UserID=@UserId) > 0 )
	begin
		update mySobek_User_Item_Permissions
		set isOwner=@isOwner, canView=@canView, canEditMetadata=@canEditMetadata, canEditBehaviors=@canEditBehaviors, 
		    canPerformQc=@canPerformQc, canUploadFiles=@canUploadFiles, canChangeVisibility=@canChangeVisibility,
			canDelete=@canDelete, customPermissions=@customPermissions
		where ItemId=@ItemId and UserId=@UserId;
	end
	else
	begin
		insert into mySobek_User_Item_Permissions ( UserID, ItemID, isOwner, canView, canEditMetadata, canEditBehaviors, canPerformQc, canUploadFiles, canChangeVisibility, canDelete, customPermissions )
		values ( @UserId, @ItemId, @isOwner, @canView, @canEditMetadata, @canEditBehaviors, @canPerformQc, @canUploadFiles, @canChangeVisibility, @canDelete, @customPermissions );
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_New_Item]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Saves all the main data for a new item in a SobekCM library, 
-- including the serial hierarchy, behaviors, tracking, and basic item information
-- Written by Mark Sullivan ( January 2011 )
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_New_Item]
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
			-- Does this institution already exist?
			if (( select count(*) from SobekCM_Item_Aggregation where Code = @HoldingCode ) = 0 )
			begin
				-- Add new institution
				insert into SobekCM_Item_Aggregation ( Code, [Name], ShortName, Description, ThematicHeadingID, [Type], isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
				values ( @HoldingCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
			end;
			
			-- Add the link to this holding code ( and any legitimate parent aggregations )
			exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @HoldingCode;
		end;

		-- Check for Source Institution Code
		if ( len ( isnull ( @SourceCode, '' ) ) > 0 )
		begin
			-- Does this institution already exist?
			if (( select count(*) from SobekCM_Item_Aggregation where Code = @SourceCode ) = 0 )
			begin
				-- Add new institution
				insert into SobekCM_Item_Aggregation ( Code, [Name], ShortName, Description, ThematicHeadingID, [Type], isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
				values ( @SourceCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
			end;

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
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Project]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Project]
	@ProjectID int,
	@ProjectCode nvarchar(20),
	@ProjectName nvarchar(100),
	@ProjectManager nvarchar(100),
	@GrantID nvarchar(250),
	@GrantName bigint,
	@StartDate date,
	@EndDate date,
	@isActive bit,
	@Description nvarchar(MAX),
	@Specifications nvarchar(MAX),
	@Priority nvarchar(100),
	@QC_Profile nvarchar(100),
	@TargetItemCount int,
	@TargetPageCount int,
	@Comments nvarchar(MAX),
	@CopyrightPermissions nvarchar(1000),
	@New_ProjectID int output
	
AS
Begin transaction

	-- Set the return ProjectID value first
	set @New_ProjectID = @ProjectID;
	

	-- If this project does not exist (ProjectID) insert, else update
	if (( select count(*) from SobekCM_Project  where ( ProjectID = @ProjectID ))  < 1 )
	   begin	
	    	-- begin insert
		    insert into SobekCM_Project (ProjectCode, ProjectName, ProjectManager, GrantID, GrantName, StartDate, EndDate, isActive, [Description], Specifications, [Priority],QC_Profile, TargetItemCount, TargetPageCount, Comments, CopyrightPermissions)
		    values (@ProjectCode, @ProjectName, @ProjectManager, @GrantID, @GrantName, @StartDate, @EndDate, @isActive, @Description, @Specifications, @Priority, @QC_Profile, @TargetItemCount, @TargetPageCount, @Comments, @CopyrightPermissions);
     	--Get the new ProjectID for this row
     	set @New_ProjectID = @@IDENTITY;
     	end
	else
	    begin
	    --update the corresponding row in the SobekCM_Project table
	    update SobekCM_Project
	    set ProjectCode=@ProjectCode, ProjectName=@ProjectName, ProjectManager=@ProjectManager, GrantID=@GrantID, GrantName=@GrantName, StartDate=@StartDate, EndDate=@EndDate, isActive=@isActive, [Description]=@Description, Specifications=@Specifications, [Priority]=@Priority, QC_Profile=@QC_Profile, TargetItemCount=@TargetItemCount, TargetPageCount=@TargetPageCount, Comments=@Comments, CopyrightPermissions=@CopyrightPermissions
	    where ProjectID=@ProjectID;
	    end	
		
commit transaction;		
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Project_Aggregation_Link]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Project_Aggregation_Link]
	@ProjectID int,
	@AggregationID int
AS
Begin
  --If this link does not already exist, insert it
  if((select count(*) from SobekCM_Project_Aggregation_Link  where ( ProjectID = @ProjectID and AggregationID=@AggregationID ))  < 1 )
    insert into SobekCM_Project_Aggregation_Link(ProjectID, AggregationID)
    values(@ProjectID, @AggregationID);
End
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Project_DefaultMetadata_Link]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Project_DefaultMetadata_Link]
	@ProjectID int,
	@DefaultMetadataID int
AS
Begin
  --If this link does not already exist, insert it
  if((select count(*) from SobekCM_Project_DefaultMetadata_Link  where ( ProjectID = @ProjectID and DefaultMetadataID=@DefaultMetadataID ))  < 1 )
    insert into SobekCM_Project_DefaultMetadata_Link(ProjectID, DefaultMetadataID)
    values(@ProjectID, @DefaultMetadataID);
End
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Project_Item_Link]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Project_Item_Link]
	@ProjectID int,
	@ItemID int
AS
Begin
  --If this link does not already exist, insert it
  if((select count(*) from SobekCM_Project_Item_Link  where ( ProjectID = @ProjectID and ItemID=@ItemID ))  < 1 )
    insert into SobekCM_Project_Item_Link(ProjectID, ItemID)
    values(@ProjectID, @ItemID);
End
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Project_Template_Link]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Project_Template_Link]
	@ProjectID int,
	@TemplateID int
AS
Begin
  --If this link does not already exist, insert it
  if((select count(*) from SobekCM_Project_Template_Link  where ( ProjectID = @ProjectID and TemplateID=@TemplateID ))  < 1 )
    insert into SobekCM_Project_Template_Link(ProjectID, TemplateID)
    values(@ProjectID, @TemplateID);
End
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Save_Serial_Hierarchy]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Adds the link between the item and the group and also adds the serial hierarchy
-- Stored procedure written by Mark Sullivan ( September 2006 )
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Save_Serial_Hierarchy]
	@GroupID int,
	@ItemID int,
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
	@SerialHierarchy varchar(500)
AS
begin transaction

	update SobekCM_Item
	set Level1_Text = @Level1_Text, Level1_Index = @Level1_Index, 
		Level2_Text = @Level2_Text, Level2_Index = @Level2_Index,
		Level3_Text = @Level3_Text, Level3_Index = @Level3_Index, 
		Level4_Text = @Level4_Text, Level4_Index = @Level4_Index,
		Level5_Text = @Level5_Text, Level5_Index = @Level5_Index
	where ItemID=@ItemID

commit transaction
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Send_Email]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Sends an email via database mail and additionally logs that the email was sent
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Send_Email] 
	@recipients_list varchar(250),
	@subject_line varchar(500),
	@email_body nvarchar(max),
	@from_address nvarchar(250),
	@reply_to nvarchar(250), 
	@html_format bit,
	@contact_us bit,
	@replytoemailid int,
	@userid int
AS
begin transaction

	if (( @userid < 0 ) or (( select count(*) from SobekCM_Email_Log where UserID = @userid and Sent_Date > DateAdd( DAY, -1, GETDATE())) < 20 ))
	begin

		-- Look for an exact match for the recipients_list.  One recipient list should AT MOST get 250 emails over a 24 hours period
		if ( ( select count(*) from SobekCM_Email_Log where Receipt_List=@recipients_list and Sent_Date > DateAdd( DAY, -1, GETDATE())) > 250 )
		begin
			-- Just add this to the email log, but indicate not sent
			insert into SobekCM_Email_Log( Sender, Receipt_List, Subject_Line, Email_Body, Sent_Date, HTML_Format, Contact_Us, ReplyToEmailId, UserID )
			values ( 'sobekcm noreply profile', @recipients_list, @subject_line + '(not delivered)', 'Too many emails to this recipient list in last 24 hours.  Governer kicked in and this email was not sent.   ' + @email_body, GETDATE(), @html_format, @contact_us, @replytoemailid, @userid );

		end
		else
		begin

			-- Log this email
			insert into SobekCM_Email_Log( Sender, Receipt_List, Subject_Line, Email_Body, Sent_Date, HTML_Format, Contact_Us, ReplyToEmailId, UserID )
			values ( 'sobekcm noreply profile', @recipients_list, @subject_line, @email_body, GETDATE(), @html_format, @contact_us, @replytoemailid, @userid );
		
			-- Send the email
			if ( @html_format = 'true' )
			begin
				if ( len(coalesce(@from_address,'')) > 0 )
				begin
					if ( len(coalesce(@reply_to,'')) > 0 )
					begin
						EXEC msdb.dbo.sp_send_dbmail
							@profile_name= 'sobekcm noreply profile',
							@recipients = @recipients_list,
							@body = @email_body,
							@subject = @subject_line,
							@body_format = 'html',
							@from_address = @from_address,
							@reply_to = @reply_to;
					end
					else
					begin
						EXEC msdb.dbo.sp_send_dbmail
							@profile_name= 'sobekcm noreply profile',
							@recipients = @recipients_list,
							@body = @email_body,
							@subject = @subject_line,
							@body_format = 'html',
							@from_address = @from_address;
					end;
				end
				else
				begin
					if ( len(coalesce(@reply_to,'')) > 0 )
					begin
						EXEC msdb.dbo.sp_send_dbmail
							@profile_name= 'sobekcm noreply profile',
							@recipients = @recipients_list,
							@body = @email_body,
							@subject = @subject_line,
							@body_format = 'html',
							@reply_to = @reply_to;
					end
					else
					begin
						EXEC msdb.dbo.sp_send_dbmail
							@profile_name= 'sobekcm noreply profile',
							@recipients = @recipients_list,
							@body = @email_body,
							@subject = @subject_line,
							@body_format = 'html';
					end;
				end;
			end
			else
			begin
				if ( len(coalesce(@from_address,'')) > 0 )
				begin
					if ( len(coalesce(@reply_to,'')) > 0 )
					begin
						EXEC msdb.dbo.sp_send_dbmail
							@profile_name= 'sobekcm noreply profile',
							@recipients = @recipients_list,
							@body = @email_body,
							@subject = @subject_line,
							@from_address = @from_address,
							@reply_to = @reply_to;
					end
					else
					begin
						EXEC msdb.dbo.sp_send_dbmail
							@profile_name= 'sobekcm noreply profile',
							@recipients = @recipients_list,
							@body = @email_body,
							@subject = @subject_line,
							@from_address = @from_address;
					end;
				end
				else
				begin
					if ( len(coalesce(@reply_to,'')) > 0 )
					begin
						EXEC msdb.dbo.sp_send_dbmail
							@profile_name= 'sobekcm noreply profile',
							@recipients = @recipients_list,
							@body = @email_body,
							@subject = @subject_line,
							@reply_to = @reply_to;
					end
					else
					begin
						EXEC msdb.dbo.sp_send_dbmail
							@profile_name= 'sobekcm noreply profile',
							@recipients = @recipients_list,
							@body = @email_body,
							@subject = @subject_line;
					end;
				end;
			end;
		end;
	end;
	
commit transaction;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Set_IP_Restriction_Mask]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Set the IP restriction mask on a single item, by a single user, and
-- add a progress note that this was done
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Set_IP_Restriction_Mask]
	@itemid int,
	@newipmask int,
	@user varchar(50),
	@progressnote varchar(1000)
AS
begin transaction

	-- Update the item table
	update SobekCM_Item
	set IP_Restriction_Mask=@newipmask
	where ItemID=@itemid;
	
	-- Update the workhistory and possibly milestones
	if ( @newipmask < 0 )
	begin
		-- Add a worklog for this making the item PRIVATE
		insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote )
		values ( @itemid, 35, GETDATE(), @user, @progressnote );
	end
	else
	begin
		if ( @newipmask = 0 )
		begin
			-- Add a worklog for this making the item PUBLIC
			insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote )
			values ( @itemid, 34, GETDATE(), @user, @progressnote )		;
			
			-- Set the aggregations linked to this item's LastItemAdded date
			update SobekCM_Item_Aggregation
			set LastItemAdded = GETDATE()
			where exists ( select * from SobekCM_Item_Aggregation_Item_Link L where L.ItemID=@itemid and L.AggregationID = SobekCM_Item_Aggregation.AggregationID );	
		end
		else
		begin
			-- Add a worklog for this making the item RESTRICTED
			insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote )
			values ( @itemid, 36, GETDATE(), @user, @progressnote );
		end;
		
		-- Move along to the COMPLETED milestone
		update SobekCM_Item
		set Milestone_DigitalAcquisition = ISNULL(Milestone_DigitalAcquisition, getdate()),
		    Milestone_ImageProcessing = ISNULL(Milestone_ImageProcessing, getdate()),
		    Milestone_QualityControl = ISNULL(Milestone_QualityControl, getdate()),
		    Milestone_OnlineComplete = ISNULL(Milestone_OnlineComplete, getdate()),
		    Last_MileStone=4
		where ItemID=@itemid;
	end;

commit transaction;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Set_Item_Comments]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Procedure sets the internal comments for an item
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Set_Item_Comments]
	@itemid int,
	@newcomments nvarchar(1000)
AS
begin

	-- Update the item table
	update SobekCM_Item
	set Internal_Comments=@newcomments
	where ItemID = @itemid;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Set_Item_Setting_Value]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Sets a single item setting value, by key.  Adds a new one if this
-- is a new setting key, otherwise updates the existing value.
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Set_Item_Setting_Value]
	@ItemID int,
	@Setting_Key varchar(255),
	@Setting_Value varchar(max)
AS
BEGIN

	-- Does this setting exist?
	if ( ( select COUNT(*) from SobekCM_Item_Settings where Setting_Key=@Setting_Key and ItemID=@ItemID ) > 0 )
	begin
		-- Just update existing then
		update SobekCM_Item_Settings set Setting_Value=@Setting_Value where Setting_Key = @Setting_Key and ItemID=@ItemID;
	end
	else
	begin
		-- insert a new settting key/value pair
		insert into SobekCM_Item_Settings( ItemID, Setting_Key, Setting_Value )
		values ( @ItemID, @Setting_Key, @Setting_Value );
	end;	
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Set_Item_Visibility]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Set_Item_Visibility] 
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
		if ( exists ( select 1 from Tracking_Item where ItemID=@ItemID and EmbargoEnd is not null ))
		begin
			update Tracking_Item set EmbargoEnd=null where ItemID=@ItemID;

			set @noteText = 'Embargo date removed.  ';
		end;
	end
	else
	begin
		if ( exists ( select 1 from Tracking_Item where ItemID=@ItemID ))
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

	-- Update the main item table ( and set for the builder to review this)
	update SobekCM_Item 
	set IP_Restriction_Mask = @IpRestrictionMask, Dark = @DarkFlag, AdditionalWorkNeeded = 'true' 
	where ItemID=@ItemID;

	insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, DateStarted )
	values ( @ItemID, @workflowId, getdate(), @User, @noteText, getdate() );

	-- If this is being made public, set the public data
	if (( @DarkFlag = 'false' ) and ( @IpRestrictionMask >= 0 ))
	begin
		update SobekCM_Item 
		set MadePublicDate = coalesce(MadePublicDate, getdate())
		where ItemID=@ItemID;
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Set_Main_Thumbnail]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Set the main thumbnail for an individual item 
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Set_Main_Thumbnail]
	@bibid varchar(10),
	@vid varchar(5),
	@mainthumb varchar(100)
AS
BEGIN

	-- Get the item id
	declare @itemid int;
	set @itemid = ISNULL(( select ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID=@bibid and I.VID=@vid ), -1 );
	
	-- Set the main thumb
	if ( @itemid > 0 )
	begin
		update SobekCM_Item set MainThumbnail=@mainthumb where ItemID=@itemid;
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Set_Setting_Value]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Sets a single system-wide setting value, by key.  Adds a new one if this
-- is a new setting key, otherwise updates the existing value.
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Set_Setting_Value]
	@Setting_Key varchar(255),
	@Setting_Value varchar(max)
AS
BEGIN

	-- Does this setting exist?
	if ( ( select COUNT(*) from SobekCM_Settings where Setting_Key = @Setting_Key ) > 0 )
	begin
		-- Just update existing then
		update SobekCM_Settings set Setting_Value=@Setting_Value where Setting_Key = @Setting_Key;
	end
	else
	begin
		-- insert a new settting key/value pair
		insert into SobekCM_Settings( Setting_Key, Setting_Value )
		values ( @Setting_Key, @Setting_Value );
	end;	
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Set_User_Setting_Value]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Sets a single user setting value, by key.  Adds a new one if this
-- is a new setting key, otherwise updates the existing value.
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Set_User_Setting_Value]
	@UserID int,
	@Setting_Key varchar(255),
	@Setting_Value varchar(max)
AS
BEGIN

	-- Does this setting exist?
	if ( ( select COUNT(*) from mySobek_User_Settings where Setting_Key=@Setting_Key and UserID=@UserID ) > 0 )
	begin
		-- Just update existing then
		update mySobek_User_Settings set Setting_Value=@Setting_Value where Setting_Key = @Setting_Key and UserID=@UserID;
	end
	else
	begin
		-- insert a new settting key/value pair
		insert into mySobek_User_Settings( UserID, Setting_Key, Setting_Value )
		values ( @UserID, @Setting_Key, @Setting_Value );
	end;	
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Simple_Item_List]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Retrive the very simple list of items to save in XML format or to step through
-- and add to the solr/lucene index, etc..  
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Simple_Item_List]
	@collection_code varchar(10)
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	if ( len( isnull( @collection_code, '' )) = 0 )
	begin

		select G.BibID, I.VID, I.Title, I.CreateDate, Resource_Link = File_Location, I.LastSaved
		from SobekCM_Item_Group G, SobekCM_Item I
		where ( G.GroupID = I.GroupID )
		  and ( I.IP_Restriction_Mask = 0 )
		  and ( G.Deleted = CONVERT(bit,0) )
	      and ( I.Deleted = CONVERT(bit,0) )
		  and ( I.Dark = 0 )

	end
	else
	begin

		select G.BibID, I.VID, I.Title, I.CreateDate, Resource_Link = File_Location, I.LastSaved
		from SobekCM_Item_Group G, SobekCM_Item I, SobekCM_Item_Aggregation C, SobekCM_Item_Aggregation_Item_Link CL
		where ( G.GroupID = I.GroupID )
		  and ( I.IP_Restriction_Mask = 0 )
		  and ( G.Deleted = CONVERT(bit,0) )
	      and ( I.Deleted = CONVERT(bit,0) )
		  and ( I.Dark = 0 )
		  and ( I.ItemID = CL.ItemID )
		  and ( CL.AggregationID = C.AggregationID )
		  and ( Code = @collection_code );
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Statistics_Aggregate]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Aggregates the item and title statistics to the subcollection, collection
-- and institutional level for a given month and year
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Statistics_Aggregate]
	@statyear int,
	@statmonth int,
	@message varchar(1000) output
AS
begin

	-- No need to perform any locks here.  We will define a transaction later
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Should only do this aggregation for each year month ONCE.
	if not exists ( select * from SobekCM_Statistics where [Year]=@statyear and [Month]=@statmonth )
	begin
		set @message='No row for this year/month is present in the SobekCM_Statistics table.  Add usage stats before trying to aggregate this month.';
		print @message;
		return;
	end;

	-- Has this been aggregated before?
	if exists ( select * from SobekCM_Statistics where Aggregate_Statistics_Complete='true' and [Year]=@statyear and [Month]=@statmonth )
	begin
		set @message='Statistics for this month have already been aggregated.  You cannot aggregate the same year/month twice without introducing errors.';
		print @message;
		return;
	end;	

	-- Get items statistics and aggregation id
	select AggregationID, Hits, JPEG_Views, Zoomable_Views, Citation_Views, Thumbnail_Views, Text_Search_Views, Flash_Views, Google_Map_Views, Download_Views, Static_Views
	into #TEMP_ITEM_AGGREGATION
	from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Statistics S
	where ( S.ItemID = I.ItemID )
	  and ( I.ItemID = L.ItemID )
	  and ( S.[Year] = @statyear )
	  and ( S.[Month] = @statmonth )
	order by AggregationID;

	-- Aggregate these statistics
	select distinct(AggregationID), sum( Hits) as Item_Hits, sum(JPEG_Views) as JPEG_Views, sum(Zoomable_Views) as Zoomable_Views, 
	  sum ( Citation_Views) as Citation_Views, sum( Thumbnail_Views ) as Thumbnail_Views, sum( Text_Search_Views) as Text_Search_Views, sum (Flash_Views) as Flash_Views,
	  sum(Google_Map_Views) as Google_Map_Views, sum(Download_Views) as Download_Views, sum(Static_Views) as Static_Views
	into #TEMP_AGGREGATION_STATS
	from #TEMP_ITEM_AGGREGATION
	Group by AggregationID;	

	-- Get group statistics and collection id
	select AggregationID, Distincter = cast(AggregationID as varchar(10)) + '_' + cast(S.GroupID as varchar(10)), S.Hits
	into #TEMP_TITLE_AGGREGATION
	from SobekCM_Item_Aggregation_Item_Link CL, SobekCM_Item I, SobekCM_Item_Group_Statistics S
	where ( I.ItemID = CL.ItemID )
	  and ( I.GroupID = S.GroupID )
	  and ( S.[Year] = @statyear )
	  and ( S.[Month] = @statmonth )
	order by Distincter;

	-- Get the distinct hits by group
	select distinct(Distincter), AggregationID, Hits
	into #TEMP_TITLE_AGGREGATION_DISTINCT
	from #TEMP_TITLE_AGGREGATION
	Group by Distincter, AggregationID, Hits;

	-- Aggregate these statistics
	select distinct(AggregationID), sum( Hits) as Title_Hits
	into #TEMP_AGGREGATION_STATS2
	from #TEMP_TITLE_AGGREGATION_DISTINCT
	Group by AggregationID;
	
	-- Now update the tables within a transaction
	begin transaction
		
		-- Add these stats to the collection list
		update SobekCM_Item_Aggregation_Statistics
		set Item_Hits = (select Item_Hits from #TEMP_AGGREGATION_STATS where #TEMP_AGGREGATION_STATS.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
			Item_JPEG_Views = (select JPEG_VIews from #TEMP_AGGREGATION_STATS where #TEMP_AGGREGATION_STATS.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
			Item_Zoomable_Views = (select Zoomable_Views from #TEMP_AGGREGATION_STATS where #TEMP_AGGREGATION_STATS.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
			Item_Citation_Views = (select Citation_Views from #TEMP_AGGREGATION_STATS where #TEMP_AGGREGATION_STATS.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
			Item_Thumbnail_Views = (select Thumbnail_Views from #TEMP_AGGREGATION_STATS where #TEMP_AGGREGATION_STATS.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
			Item_Text_Search_Views = (select Text_Search_Views from #TEMP_AGGREGATION_STATS where #TEMP_AGGREGATION_STATS.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
			Item_Flash_Views = (select Flash_Views from #TEMP_AGGREGATION_STATS where #TEMP_AGGREGATION_STATS.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
			Item_Google_Map_Views = (select Google_Map_Views from #TEMP_AGGREGATION_STATS where #TEMP_AGGREGATION_STATS.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
			Item_Download_Views = (select Download_Views from #TEMP_AGGREGATION_STATS where #TEMP_AGGREGATION_STATS.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
			Item_Static_Views = (select Static_Views from #TEMP_AGGREGATION_STATS where #TEMP_AGGREGATION_STATS.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID )
		where [Year]=@statyear and [Month] = @statmonth;
		
		-- If there is no matching row though, insert this
		insert into SobekCM_Item_Aggregation_Statistics ( AggregationID, [Year],    [Month],     Hits, [Sessions], Home_Page_Views, Browse_Views, Advanced_Search_Views, Search_Results_Views, Title_Hits, Item_Hits, Item_JPEG_Views, Item_Zoomable_Views, Item_Citation_Views, Item_Thumbnail_Views, Item_Text_Search_Views, Item_Flash_Views, Item_Google_Map_Views, Item_Download_Views, Item_Static_Views )
		select                                            AggregationID, @statyear, @statmonth,  0,    0,          0,               0,            0,                     0,                    0,          Item_Hits, JPEG_Views,      Zoomable_Views,      Citation_Views,      Thumbnail_Views,      Text_Search_Views,      Flash_Views,      Google_Map_Views,      Download_Views,      Static_Views
		from #TEMP_AGGREGATION_STATS
		where not exists ( select * from SobekCM_Item_Aggregation_Statistics S where S.AggregationID = #TEMP_AGGREGATION_STATS.AggregationID and S.[Year] = @statyear and S.[Month] = @statmonth );

		-- Add these stats to the collection list
		update SobekCM_Item_Aggregation_Statistics
		set Title_Hits = (select Title_Hits from #TEMP_AGGREGATION_STATS2 where #TEMP_AGGREGATION_STATS2.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID )
		where [Year]=@statyear and [Month] = @statmonth;

		-- Update the total hits at collection level
		update SobekCM_Item_Aggregation_Statistics
		set Hits = isnull( Hits + Title_Hits + Item_Hits, 0)
		where [Year] = @statyear and [Month] = @statmonth;

		-- Update the total number of hits on the items	
		UPDATE SobekCM_Item
		set Total_Hits = isnull(( select SUM(Hits) from SobekCM_Item_Statistics S where S.ItemID=SobekCM_Item.ItemID ), 0),
			Total_Sessions = isnull(( select SUM([Sessions]) from SobekCM_Item_Statistics S where S.ItemID=SobekCM_Item.ItemID ), 0);
			
		-- Update the top level that these have been aggregated
		UPDATE SobekCM_Statistics
		SET Aggregate_Statistics_Complete='true'
		where [Year]=@statyear and [Month]=@statmonth;
		
	commit transaction;

	-- Update which users are linked to items with statistics
	update mySobek_User
	set Has_Item_Stats='true'
	where exists ( select * 
				   from mySobek_User_Item_Link L, mySobek_User_Item_Link_Relationship R, SobekCM_Item_Statistics S
				   where L.UserID=mySObek_User.UserID
				     and L.RelationshipID=R.RelationshipID 
				     and R.Include_In_Results = 'true' 
				     and L.ItemID=S.ItemID
				     and S.Hits > 0 );
				   
	-- drop the temporary tables
	drop table #TEMP_ITEM_AGGREGATION;
	drop table #TEMP_AGGREGATION_STATS;
	drop table #TEMP_TITLE_AGGREGATION;
	drop table #TEMP_TITLE_AGGREGATION_DISTINCT;
	drop table #TEMP_AGGREGATION_STATS2;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Statistics_Aggregation_Titles]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns most often hit titles and items for an aggregation
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Statistics_Aggregation_Titles]
	@code varchar(20)
AS
BEGIN

	-- No need to perform any locks here.
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Is this for the whole library, or just one aggregation?
	if (( @code != 'all' ) and ( LEN(@code) > 0 ))
	begin
		-- Get the aggregation id
		declare @aggregationid int;
		set @aggregationid = ISNULL( (select AggregationID from SobekCM_Item_Aggregation where Code=@code), -1 );
		
		-- Return top 100 items
		select top 100 G.BibID, I.VID, G.GroupTitle, I.Total_Hits
		from SobekCM_Item I, SobekCM_Item_Group G, SobekCM_Item_Aggregation_Item_Link L
		where ( I.GroupID = G.GroupID )
		  and ( I.ItemID = L.ItemID )
		  and ( L.AggregationID = @aggregationid )
		  and ( I.Total_Hits > 0 )
		order by I.Total_Hits DESC;
		
		-- Get the top 100 titles with the most hits
		select top 100 BibID, GroupTitle, SUM(I.Total_Hits) as Title_Hits
		from SobekCM_Item I, SobekCM_Item_Group G, SobekCM_Item_Aggregation_Item_Link L
		where ( I.GroupID = G.GroupID )
		  and ( I.ItemID = L.ItemID )
		  and ( L.AggregationID = @aggregationid )		  
		group by BibID, GroupTitle
		having SUM(I.Total_Hits) > 0
		order by Title_Hits DESC;
	end
	else
	begin
		-- Return top 100 items, library-wide
		select top 100 G.BibID, I.VID, G.GroupTitle, I.Total_Hits
		from SobekCM_Item I, SobekCM_Item_Group G
		where ( I.GroupID = G.GroupID )
		  and ( I.Total_Hits > 0 )
		order by I.Total_Hits DESC;
		
		-- Get the top 100 titles with the most hits, library-wide
		select top 100 BibID, GroupTitle, SUM(I.Total_Hits) as Title_Hits
		from SobekCM_Item I, SobekCM_Item_Group G
		where ( I.GroupID = G.GroupID )
		group by BibID, GroupTitle
		having SUM(I.Total_Hits) > 0
		order by Title_Hits DESC	
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Statistics_By_Date_Range]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Statistics_By_Date_Range]
	@year1 smallint,
	@month1 smallint,
	@year2 smallint,
	@month2 smallint
AS
BEGIN

	-- No need to perform any locks here, especially given the possible
	-- length of this search
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	SET NOCOUNT ON;
	SET ARITHABORT ON;
	
	-- Get the id for the ALL aggregation
	declare @all_id int;
	set @all_id = coalesce(( select AggregationID from SObekCM_Item_Aggregation where Code='all'), -1);
	
	-- Build the table of the top three levels of the aggregation hierarchy for reporting
	declare @Aggregation_List TABLE
	(
	  AggregationID int,
	  Code varchar(20),
	  ChildCode varchar(20),
	  Child2Code varchar(20),
	  AllCodes varchar(20),
	  Name nvarchar(255),
	  ShortName nvarchar(100),
	  [Type] varchar(50),
	  isActive bit
	);
			
	-- Insert the list of items linked to ALL or linked to NONE (include ALL)
	insert into @Aggregation_List ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, [Type], isActive )
	select AggregationID, Code, '', '', Code, Name, ShortName, [Type], isActive
	from SobekCM_Item_Aggregation A
	where ( [Type] not like 'Institut%' )
	  and ( Deleted='false' )
	  and ( exists ( select * from SobekCM_Item_Aggregation_Hierarchy where ChildID=A.AggregationID and ParentID=@all_id)
	       or A.AggregationID=@all_id );
	  
	-- Insert the children under those top-level collections
	insert into @Aggregation_List ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, [Type], isActive )
	select A2.AggregationID, T.Code, A2.Code, '', A2.Code, A2.Name, A2.SHortName, A2.[Type], A2.isActive
	from @Aggregation_List T, SobekCM_Item_Aggregation A2, SobekCM_Item_Aggregation_Hierarchy H
	where ( A2.[Type] not like 'Institut%' )
	  and ( T.AggregationID = H.ParentID )
	  and ( A2.AggregationID = H.ChildID )
	  and ( Deleted='false' )
	  and ( T.AggregationID <> @all_id );
	  
	-- Insert the grand-children under those child collections
	insert into @Aggregation_List ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, [Type], isActive )
	select A2.AggregationID, T.Code, T.ChildCode, A2.Code, A2.Code, A2.Name, A2.SHortName, A2.[Type], A2.isActive
	from @Aggregation_List T, SobekCM_Item_Aggregation A2, SobekCM_Item_Aggregation_Hierarchy H
	where ( A2.[Type] not like 'Institut%' )
	  and ( T.AggregationID = H.ParentID )
	  and ( A2.AggregationID = H.ChildID )
	  and ( Deleted='false' )
	  and ( ChildCode <> '' );
	  	
	-- Build the table of all the statistcs
	declare @Aggregation_Stats TABLE
	(
	  AggregationID int,
	  Hits bigint,
	  [Sessions] bigint,
	  Home_Page_Views bigint,
	  Browse_Views bigint,
	  Search_Results_Views bigint,
	  Title_Hits bigint,
	  Item_Hits bigint,
	  Item_JPEG_Views bigint,
	  Item_Zoomable_Views bigint,
	  Item_Citation_Views bigint,
	  Item_Thumbnail_Views bigint,
	  Item_Text_Search_Views bigint,
	  Item_Flash_Views bigint,
	  Item_Google_Map_Views bigint,
	  Item_Download_Views bigint
	);

	insert into @Aggregation_Stats ( AggregationID, Hits, [Sessions], Home_Page_Views, Browse_Views, Search_Results_Views, Title_Hits, Item_Hits, Item_JPEG_Views, Item_Zoomable_Views, Item_Citation_Views, Item_Thumbnail_Views, Item_Text_Search_Views, Item_Flash_Views, Item_Google_Map_Views, Item_Download_Views )
	select S.AggregationID, sum( Hits ) as Hits, sum( [Sessions] ) as [Sessions], 
		sum( Home_Page_Views) as Home_Page_Views, sum ( Browse_Views ) as Browse_Views,
		sum ( Search_Results_Views ) as Search_Results_Views,
		sum( Title_Hits ) as Title_Hits, sum ( Item_Hits ) as Item_Hits,
		sum( Item_JPEG_Views ) as Item_JPEG_Views, sum ( Item_Zoomable_Views ) as Item_Zoomable_Views,
		sum ( Item_Citation_Views ) as Item_Citation_Views, sum ( Item_Thumbnail_Views ) as Item_Thumbnail_Views,
		sum ( Item_Text_Search_Views ) as Item_Text_Search_Views, sum ( Item_Flash_Views ) as Item_Flash_Views,
		sum ( Item_Google_Map_Views) as Item_Google_Map_Views, sum( Item_Download_Views ) as item_Download_Views
	from SobekCM_Item_Aggregation_Statistics S, @Aggregation_List L
	where ( S.AggregationID = L.AggregationID )
	  and ((( @year1 < @year2 ) and ((( [Month] >= @month1 ) and ( [Year] = @year1 ))
	  or (( [Year] > @year1 ) and ( [Year] < @year2 ))
	  or (( [Month] <= @month2 ) and ( [Year] = @year2 ))))
	  or (( @year1 = @year2 ) and ( [Year] = @year1 ) and ( [Month] >= @month1 ) and ( [Month] <= @month2 )))
	group by S.AggregationID;
	
	-- Pull all the statistical data by item, for the TOTAL ROW
	select sum( Hits ) as Item_Hits,
		sum( JPEG_Views ) as Item_JPEG_Views, sum ( Zoomable_Views ) as Item_Zoomable_Views,
		sum ( Citation_Views ) as Item_Citation_Views, sum ( Thumbnail_Views ) as Item_Thumbnail_Views,
		sum ( Text_Search_Views ) as Item_Text_Search_Views, sum ( Flash_Views ) as Item_Flash_Views,
		sum ( Google_Map_Views) as Item_Google_Map_Views, sum( Download_Views ) as item_Download_Views
	into #TEMP_ITEM_STATS
	from SobekCM_Item_Statistics
	where ((( @year1 < @year2 ) and ((( [Month] >= @month1 ) and ( [Year] = @year1 ))
	  or (( [Year] > @year1 ) and ( [Year] < @year2 ))
	  or (( [Month] <= @month2 ) and ( [Year] = @year2 ))))
	  or (( @year1 = @year2 ) and ( [Year] = @year1 ) and ( [Month] >= @month1 ) and ( [Month] <= @month2 )));

	-- Pull all the statistical data by item group, for the TOTAL ROW
	select sum( Hits ) as Title_Hits
	into #TEMP_GROUP_STATS
	from SobekCM_Item_Group_Statistics
	where ((( @year1 < @year2 ) and ((( [Month] >= @month1 ) and ( [Year] = @year1 ))
	  or (( [Year] > @year1 ) and ( [Year] < @year2 ))
	  or (( [Month] <= @month2 ) and ( [Year] = @year2 ))))
	  or (( @year1 = @year2 ) and ( [Year] = @year1 ) and ( [Month] >= @month1 ) and ( [Month] <= @month2 )));

	-- Pull all the statistical data by aggregation, for the TOTAL ROW
	select sum(Home_Page_Views) as Home_Page_Views, sum(Browse_Views) as Browse_Views,
		  sum(Search_Results_Views) as Search_Results_Views
	into #TEMP_AGGREGATION_STATS
	from SobekCM_Item_Aggregation_Statistics
	where ((( @year1 < @year2 ) and ((( [Month] >= @month1 ) and ( [Year] = @year1 ))
	  or (( [Year] > @year1 ) and ( [Year] < @year2 ))
	  or (( [Month] <= @month2 ) and ( [Year] = @year2 ))))
	  or (( @year1 = @year2 ) and ( [Year] = @year1 ) and ( [Month] >= @month1 ) and ( [Month] <= @month2 )));

	-- Pull all the statistical overall data, for the TOTAL ROW
	select sum( Hits ) as Hits, sum( [Sessions] ) as Sessions
	into #TEMP_URL_STATS
	from SobekCM_Statistics
	where ((( @year1 < @year2 ) and ((( [Month] >= @month1 ) and ( [Year] = @year1 ))
	  or (( [Year] > @year1 ) and ( [Year] < @year2 ))
	  or (( [Month] <= @month2 ) and ( [Year] = @year2 ))))
	  or (( @year1 = @year2 ) and ( [Year] = @year1 ) and ( [Month] >= @month1 ) and ( [Month] <= @month2 )));

	-- Return the list of all the aggregations stats, unioned with the TOTAL row
	select Code, ChildCode, Child2Code, AllCodes, Name, ShortName, [Type], isActive,	
		coalesce( Hits, 0 ) as Hits, coalesce( [Sessions], 0 ) as [Sessions],
		coalesce( Home_Page_Views, 0) as Home_Page_Views, coalesce ( Browse_Views, 0 ) as Browse_Views,
		coalesce ( Search_Results_Views, 0 ) as Search_Results_Views,
		coalesce( Title_Hits, 0 ) as Title_Hits, coalesce ( Item_Hits, 0 ) as Item_Hits,
		coalesce( Item_JPEG_Views, 0 ) as Item_JPEG_Views, coalesce ( Item_Zoomable_Views, 0 ) as Item_Zoomable_Views,
		coalesce ( Item_Citation_Views, 0 ) as Item_Citation_Views, coalesce ( Item_Thumbnail_Views, 0 ) as Item_Thumbnail_Views,
		coalesce ( Item_Text_Search_Views, 0 ) as Item_Text_Search_Views, coalesce ( Item_Flash_Views, 0 ) as Item_Flash_Views,
		coalesce ( Item_Google_Map_Views, 0 ) as Item_Google_Map_Views, coalesce( Item_Download_Views, 0 ) as item_Download_Views
	from @Aggregation_List AS C LEFT OUTER JOIN
	     @Aggregation_Stats AS S on ( C.AggregationID = S.AggregationID )
	union
	select 'ZZZ', '', '', 'ZZZ', 'TOTAL', 'TOTAL', 'TOTAL', 'false',
		A.Hits, A.[Sessions], C.Home_Page_Views, C.Browse_Views, C.Search_Results_Views, G.Title_Hits,
		Item_Hits, Item_JPEG_Views, Item_Zoomable_Views, Item_Citation_Views, Item_Thumbnail_Views,
		Item_Text_Search_Views, Item_Flash_Views, Item_Google_Map_Views, Item_Download_Views
	from #TEMP_ITEM_STATS I, #TEMP_GROUP_STATS G, #TEMP_URL_STATS A, #TEMP_AGGREGATION_STATS C
	order by Code, ChildCode, Child2Code;
	     
	drop table #TEMP_AGGREGATION_STATS;
	drop table #TEMP_ITEM_STATS;
	drop table #TEMP_GROUP_STATS;
	drop table #TEMP_URL_STATS;

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Statistics_Lookup_Tables]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns the lookup tables for assembling the statistics information
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Statistics_Lookup_Tables]
AS
BEGIN
	
	-- No need to perform any locks here.
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Return the item id
	select I.ItemID, G.BibID, I.VID
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID );

	-- Return the group id
	select G.GroupID, G.BibID
	from SobekCM_Item_Group G;

	-- Return the aggregation ids
	select S.AggregationID, S.Code, S.[Type]
	from SobekCM_Item_Aggregation S;
	
	-- Return the portal ids
	select P.PortalID, P.Base_URL, P.Abbreviation, P.isDefault
	from SobekCM_Portal_URL P
	where P.isActive = 'true';

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Statistics_Save_Aggregation]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Statistics_Save_Aggregation]
	@aggregationid int,
	@year smallint,
	@month smallint,
	@hits int,
	@sessions int,
	@home_page_views int,
	@browse_views int,
	@advanced_search_views int,
	@search_results_views int
as
begin
	insert into SobekCM_Item_Aggregation_Statistics ( AggregationID, [Year], [Month], [Hits], [Sessions], Home_Page_Views, Browse_Views, Advanced_Search_Views, Search_Results_Views ) 
	values ( @aggregationid, @year, @month, @hits, @sessions, @home_page_views, @browse_views, @advanced_search_views, @search_results_views );
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Statistics_Save_Item]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Statistics_Save_Item]
	@year smallint,
	@month smallint,
	@hits int,
	@sessions int,
	@itemid int,
	@jpeg_views int,
	@zoomable_views int,
	@citation_views int,
	@thumbnail_views int,
	@text_search_views int,
	@flash_views int,
	@google_map_views int,
	@download_views int,
	@static_views int
as
begin

	insert into SobekCM_Item_Statistics ( ItemID, [Year], [Month], [Hits], [Sessions], JPEG_Views, Zoomable_Views, Citation_Views,
		Thumbnail_Views, Text_Search_Views, Flash_Views, Google_Map_Views, Download_Views, Static_Views ) 
	values ( @itemid, @year, @month, @hits, @sessions, @jpeg_views, @zoomable_views, @citation_views,
		@thumbnail_views, @text_search_views, @flash_views, @google_map_views, @download_views, @static_views );

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Statistics_Save_Item_Group]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Statistics_Save_Item_Group]
	@year smallint,
	@month smallint,
	@hits int,
	@sessions int,
	@groupid int
as
begin

	insert into SobekCM_Item_Group_Statistics ( GroupID, [Year], [Month], [Hits], [Sessions] ) 
	values ( @groupid, @year, @month, @hits, @sessions );

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Statistics_Save_Portal]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Statistics_Save_Portal]
	@year smallint,
	@month smallint,
	@hits int,
	@portalid int
as
begin

	insert into SobekCM_Portal_URL_Statistics ( PortalID, [Year], [Month], [Hits] )
	values ( @portalid, @year, @month, @hits );

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Statistics_Save_TopLevel]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Statistics_Save_TopLevel]
	@year smallint,
	@month smallint,
	@hits int,
	@sessions int,
	@robot_hits int,
	@xml_hits int,
	@oai_hits int,
	@json_hits int
as
begin

	-- Clear any existing one
	delete from SobekCM_Statistics where [Year]=@year and [Month]=@month;

	-- Add this
	insert into SobekCM_Statistics ( [Year], [Month], [Hits], [Sessions], Robot_Hits, XML_Hits, OAI_Hits, JSON_Hits )
	values ( @year, @Month, @hits, @sessions, @robot_hits, @xml_hits, @oai_hits, @json_hits);
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Statistics_Save_Webcontent]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
                     
					 
-- Insert statistics for a top-level web content page
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Statistics_Save_Webcontent]
	@year smallint,
	@month smallint,
	@hits int,
	@hits_complete int,
	@level1 varchar(100),
	@level2 varchar(100),
	@level3 varchar(100),
	@level4 varchar(100),
	@level5 varchar(100),
	@level6 varchar(100),
	@level7 varchar(100),
	@level8 varchar(100)
as
begin

	-- Get the WebContent ID
	declare @webcontentid int;
	set @webcontentid = coalesce((    select WebContentID 
									  from SobekCM_WebContent W
									  where coalesce(W.Level1,'') = coalesce(@level1, '' )
										 and coalesce(W.Level2,'') = coalesce(@level2, '' )
										 and coalesce(W.Level3,'') = coalesce(@level3, '' )
										 and coalesce(W.Level4,'') = coalesce(@level4, '' )
										 and coalesce(W.Level5,'') = coalesce(@level5, '' )
										 and coalesce(W.Level6,'') = coalesce(@level6, '' )
										 and coalesce(W.Level7,'') = coalesce(@level7, '' )
										 and coalesce(W.Level8,'') = coalesce(@level8, '' )), -1 );

	-- Only add if there is a web content id
	if ( @webcontentid > 0 )
	begin
		insert into SobekCM_Webcontent_Statistics ( WebContentID, Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8, [Year], [Month], [Hits], Hits_Complete ) 
		values ( @webcontentid, @level1, @level2, @level3, @level4, @level5, @level6, @level7, @level8, @year, @month, @hits, @hits_complete );
	end;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Stats_Get_User_Linked_Items_Stats]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the list of items linked to this user, along with usage for that month
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Stats_Get_User_Linked_Items_Stats]
	@userid int,
	@month int,
	@year int
AS
begin
	-- No need to perform any locks here.
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select L.ItemID, L.RelationshipID, R.RelationshipLabel, I.Title, G.BibID, I.VID, I.CreateDate, I.Total_Hits, I.Total_Sessions, ISNULL(S2.Hits,0) as Month_Hits, ISNULL(S2.[Sessions],0) as Month_Sessions
	from mySobek_User_Item_Link_Relationship AS R join
		 mySobek_User_Item_Link AS L ON ( L.RelationshipID=R.RelationshipID ) join
		 SobekCM_Item AS I ON ( L.ItemID=I.ItemID ) join
		 SobekCM_Item_Group AS G ON ( G.GroupID=I.GroupID) left join
		 SobekCM_Item_Statistics AS S2 ON ( S2.ItemID=L.ItemID and S2.[Month]=@month and S2.[Year]=@year)		  
	where ( L.UserID=@userid ) and ( R.Include_In_Results = 'true' );
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Stats_Get_Users_Linked_To_Items]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the list of all users that have items which may have statistics
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Stats_Get_Users_Linked_To_Items] AS
begin
	-- No need to perform any locks here.
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	select U.FirstName, U.LastName, U.NickName, U.UserName, U.UserID, U.EmailAddress
	from mySobek_User U
	where ( Receive_Stats_Emails = 'true' )
	   and exists ( select * from mySobek_User_Item_Link L, mySobek_User_Item_Link_Relationship R where L.UserID=U.UserID and L.RelationshipID=R.RelationshipID and R.Include_In_Results = 'true' );
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Update_Additional_Work_Needed_Flag]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Updates the 'additional work needed' flag for an item, which tells
-- the builder that it should be post-processed.
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Update_Additional_Work_Needed_Flag] 
	@itemid int,
	@newflag bit
as
begin
	update SobekCM_Item set AdditionalWorkNeeded=@newflag where ItemID=@itemid;
end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_Update_Item_Group]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Procedure to change some basic information about an item group
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Update_Item_Group]
	@BibID varchar(10),
	@GroupTitle nvarchar(500),
	@SortTitle varchar(500),
	@GroupThumbnail varchar(500),
	@PrimaryIdentifierType nvarchar(50),
	@PrimaryIdentifier nvarchar(100)	
AS
begin

	update SobekCM_Item_Group
	set GroupTitle = @GroupTitle, SortTitle = @SortTitle, GroupThumbnail=@GroupThumbnail,
	    Primary_Identifier_Type=@PrimaryIdentifierType, Primary_Identifier=@PrimaryIdentifier
	where BibID = @BibID;

end;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_Add]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Add a new web content page
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_Add]
	@Level1 varchar(100),
	@Level2 varchar(100),
	@Level3 varchar(100),
	@Level4 varchar(100),
	@Level5 varchar(100),
	@Level6 varchar(100),
	@Level7 varchar(100),
	@Level8 varchar(100),
	@UserName nvarchar(100),
	@Title nvarchar(255),
	@Summary nvarchar(1000),
	@Redirect nvarchar(500),
	@WebContentID int output
AS
BEGIN	
	-- Is there a match already for this?
	if ( EXISTS ( select 1 from SobekCM_WebContent 
	              where ( Level1=@Level1 )
	                and ((Level2 is null and @Level2 is null ) or ( Level2=@Level2)) 
					and ((Level3 is null and @Level3 is null ) or ( Level3=@Level3))
					and ((Level4 is null and @Level4 is null ) or ( Level4=@Level4))
					and ((Level5 is null and @Level5 is null ) or ( Level5=@Level5))
					and ((Level6 is null and @Level6 is null ) or ( Level6=@Level6))
					and ((Level7 is null and @Level7 is null ) or ( Level7=@Level7))
					and ((Level8 is null and @Level8 is null ) or ( Level8=@Level8))))
	begin
		-- Get the web content id
		set @WebContentID = (   select top 1 WebContentID 
								from SobekCM_WebContent 
								where ( Level1=@Level1 )
								  and ((Level2 is null and @Level2 is null ) or ( Level2=@Level2)) 
								  and ((Level3 is null and @Level3 is null ) or ( Level3=@Level3))
								  and ((Level4 is null and @Level4 is null ) or ( Level4=@Level4))
								  and ((Level5 is null and @Level5 is null ) or ( Level5=@Level5))
								  and ((Level6 is null and @Level6 is null ) or ( Level6=@Level6))
								  and ((Level7 is null and @Level7 is null ) or ( Level7=@Level7))
								  and ((Level8 is null and @Level8 is null ) or ( Level8=@Level8)));

		-- Ensure the title and summary are correct
		update SobekCM_WebContent set Title=@Title, Summary=@Summary, Redirect=@Redirect where WebContentID=@WebContentID;
		
		-- Was this previously deleted?
		if ( EXISTS ( select 1 from SobekCM_WebContent where Deleted='true' and WebContentID=@WebContentID ))
		begin
			-- Undelete this 
			update SobekCM_WebContent
			set Deleted='false'
			where WebContentID = @WebContentID;

			-- Mark this in the milestones then
			insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneDate, MilestoneUser )
			values ( @WebContentID, 'Restored previously deleted page', getdate(), @UserName );
		end;
	end
	else
	begin
		-- Add the new web content then
		insert into SobekCM_WebContent ( Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8, Title, Summary, Deleted, Redirect )
		values ( @Level1, @Level2, @Level3, @Level4, @Level5, @Level6, @Level7, @Level8, @Title, @Summary, 'false', @Redirect );

		-- Get the new ID for this
		set @WebContentID = SCOPE_IDENTITY();

		-- Now, add this to the milestones table
		insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneDate, MilestoneUser )
		values ( @WebContentID, 'Add new page', getdate(), @UserName );
	end;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_Add_Milestone]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Add a new milestone to an existing web content page
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_Add_Milestone]
	@WebContentID int,
	@Milestone nvarchar(max),
	@MilestoneUser nvarchar(100)
AS
BEGIN

	-- Insert milestone
	insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneUser, MilestoneDate )
	values ( @WebContentID, @Milestone, @MilestoneUser, getdate());

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_All]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Return all the web content pages, regardless of whether they are redirects or an actual content page
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_All]
AS
BEGIN

	-- Get the pages, with the time last updated
	with webcontent_last_update as
	(
		select WebContentID, Max(WebContentMilestoneID) as MaxMilestoneID
		from SobekCM_WebContent_Milestones
		group by WebContentID
	)
	select W.WebContentID, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Title, W.Summary, W.Deleted, W.Redirect, M.MilestoneDate, M.MilestoneUser
	from SobekCM_WebContent W left outer join
		 webcontent_last_update L on L.WebContentID=W.WebContentID left outer join
	     SobekCM_WebContent_Milestones M on M.WebContentMilestoneID=L.MaxMilestoneID
	where Deleted='false'
	order by W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8;

	-- Get the distinct top level pages
	select distinct(W.Level1)
	from SobekCM_WebContent W
	where ( Deleted = 'false' )
	order by W.Level1;

	-- Get the distinct top TWO level pages
	select W.Level1, W.Level2
	from SobekCM_WebContent W
	where ( W.Level2 is not null )
	  and ( Deleted = 'false' )
	group by W.Level1, W.Level2
	order by W.Level1, W.Level2;

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_All_Brief]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Return a brief account of all the web content pages, regardless of whether they are redirects or an actual content page
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_All_Brief]
AS
BEGIN

	-- Get the complete list of all active web content pages, with segment level names, primary key, and redirect URL
	select W.WebContentID, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Redirect
	from SobekCM_WebContent W 
	where Deleted = 'false'
	order by W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8;

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_All_Pages]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Return all the web content pages that are not set as redirects
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_All_Pages]
AS
BEGIN

	-- Get the pages, with the time last updated
	with webcontent_last_update as
	(
		select WebContentID, Max(WebContentMilestoneID) as MaxMilestoneID
		from SobekCM_WebContent_Milestones
		group by WebContentID
	)
	select W.WebContentID, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Title, W.Summary, W.Deleted, W.Redirect, M.MilestoneDate, M.MilestoneUser
	from SobekCM_WebContent W left outer join
		 webcontent_last_update L on L.WebContentID=W.WebContentID left outer join
	     SobekCM_WebContent_Milestones M on M.WebContentMilestoneID=L.MaxMilestoneID
	where ( len(coalesce(W.Redirect,'')) = 0 ) and ( Deleted = 'false' )
	order by W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8;

	-- Get the distinct top level pages
	select distinct(W.Level1)
	from SobekCM_WebContent W
	where ( len(coalesce(W.Redirect,'')) = 0 ) and ( Deleted = 'false' )
	order by W.Level1;

	-- Get the distinct top TWO level pages
	select W.Level1, W.Level2
	from SobekCM_WebContent W
	where ( len(coalesce(W.Redirect,'')) = 0 )
	  and ( W.Level2 is not null )
	  and ( Deleted = 'false' )
	group by W.Level1, W.Level2
	order by W.Level1, W.Level2;

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_All_Redirects]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Return all the web content pages that are set as redirects
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_All_Redirects]
AS
BEGIN

	-- Get the pages, with the time last updated
	with webcontent_last_update as
	(
		select WebContentID, Max(WebContentMilestoneID) as MaxMilestoneID
		from SobekCM_WebContent_Milestones
		group by WebContentID
	)
	select W.WebContentID, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Title, W.Summary, W.Deleted, W.Redirect, M.MilestoneDate, M.MilestoneUser
	from SobekCM_WebContent W left outer join
		 webcontent_last_update L on L.WebContentID=W.WebContentID left outer join
	     SobekCM_WebContent_Milestones M on M.WebContentMilestoneID=L.MaxMilestoneID
	where ( len(coalesce(W.Redirect,'')) > 0 ) and ( Deleted = 'false' )
	order by W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8;

	-- Get the distinct top level pages
	select distinct(W.Level1)
	from SobekCM_WebContent W
	where ( len(coalesce(W.Redirect,'')) > 0 ) and ( Deleted = 'false' )
	order by W.Level1;

	-- Get the distinct top TWO level pages
	select W.Level1, W.Level2
	from SobekCM_WebContent W
	where ( len(coalesce(W.Redirect,'')) > 0 )
	  and ( W.Level2 is not null )
	  and ( Deleted = 'false' )
	group by W.Level1, W.Level2
	order by W.Level1, W.Level2;

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_Delete]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Delete an existing web content page (and mark in the milestones)
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_Delete]
	@WebContentID int,
	@Reason nvarchar(max),
	@MilestoneUser nvarchar(100)
AS
BEGIN

	-- Mark web page as deleted
	update SobekCM_WebContent
	set Deleted='true'
	where WebContentID=@WebContentID;

	-- Add a milestone for this
	if (( @Reason is not null ) and ( len(@Reason) > 0 ))
	begin
		insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneUser, MilestoneDate )
		values ( @WebContentID, 'Page Deleted - ' + @Reason, @MilestoneUser, getdate());
	end
	else
	begin
		insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneUser, MilestoneDate )
		values ( @WebContentID, 'Page Deleted', @MilestoneUser, getdate());
	end;

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_Edit]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



-- Edit basic information on an existing web content page
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_Edit]
	@WebContentID int,
	@UserName nvarchar(100),
	@Title nvarchar(255),
	@Summary nvarchar(1000),
	@Redirect varchar(500),
	@MilestoneText varchar(max)
AS
BEGIN	
	-- Make the change
	update SobekCM_WebContent
	set Title=@Title, Summary=@Summary, Redirect=@Redirect
	where WebContentID=@WebContentID;

	-- Now, add a milestone
	if ( len(coalesce(@MilestoneText,'')) > 0 )
	begin
		insert into SobekCM_WebContent_Milestones (WebContentID, Milestone, MilestoneDate, MilestoneUser )
		values ( @WebContentID, @MilestoneText, getdate(), @UserName );
	end
	else
	begin
		insert into SobekCM_WebContent_Milestones (WebContentID, Milestone, MilestoneDate, MilestoneUser )
		values ( @WebContentID, 'Edited', getdate(), @UserName );
	end;

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_Get_Milestones]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the milestones for a webcontent page (by ID)
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_Get_Milestones]
	@WebContentID int
AS
BEGIN

	-- Get all milestones
	select Milestone, MilestoneDate, MilestoneUser
	from SobekCM_WebContent_Milestones
	where WebContentID=@WebContentID
	order by MilestoneDate;

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_Get_Page]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get basic details about an existing web content page
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_Get_Page]
	@Level1 varchar(100),
	@Level2 varchar(100),
	@Level3 varchar(100),
	@Level4 varchar(100),
	@Level5 varchar(100),
	@Level6 varchar(100),
	@Level7 varchar(100),
	@Level8 varchar(100)
AS
BEGIN	
	-- Return the couple of requested pieces of information
	select top 1 W.WebContentID, W.Title, W.Summary, W.Deleted, M.MilestoneDate, M.MilestoneUser, W.Redirect, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Locked
	from SobekCM_WebContent W left outer join
	     SobekCM_WebContent_Milestones M on W.WebContentID=M.WebContentID
	where ( Level1=@Level1 )
	  and ((Level2 is null and @Level2 is null ) or ( Level2=@Level2)) 
	  and ((Level3 is null and @Level3 is null ) or ( Level3=@Level3))
	  and ((Level4 is null and @Level4 is null ) or ( Level4=@Level4))
	  and ((Level5 is null and @Level5 is null ) or ( Level5=@Level5))
	  and ((Level6 is null and @Level6 is null ) or ( Level6=@Level6))
	  and ((Level7 is null and @Level7 is null ) or ( Level7=@Level7))
	  and ((Level8 is null and @Level8 is null ) or ( Level8=@Level8))
	order by M.MilestoneDate DESC;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_Get_Page_ID]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get basic details about an existing web content page
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_Get_Page_ID]
	@WebContentID int
AS
BEGIN	
	-- Return the couple of requested pieces of information
	select top 1 W.WebContentID, W.Title, W.Summary, W.Deleted, M.MilestoneDate, M.MilestoneUser, W.Redirect, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Locked
	from SobekCM_WebContent W left outer join
	     SobekCM_WebContent_Milestones M on W.WebContentID=M.WebContentID
	where W.WebContentID = @WebContentID
	order by M.MilestoneDate DESC;
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_Get_Recent_Changes]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the list of recent changes to all web content pages
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_Get_Recent_Changes]
AS
BEGIN

	-- Get all milestones
	select W.WebContentID, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, MilestoneDate, MilestoneUser, Milestone, W.Title
	from SobekCM_WebContent_Milestones M, SobekCM_WebContent W
	where M.WebContentID=W.WebContentID
	order by MilestoneDate DESC;

	-- Get the distinct list of users that made changes
	select MilestoneUser
	from SobekCM_WebContent_Milestones
	group by MilestoneUser
	order by MilestoneUser;

	-- Return the distinct first level
	select Level1 
	from SobekCM_WebContent_Milestones M, SobekCM_WebContent W
	where M.WebContentID=W.WebContentID
	group by Level1
	order by Level1;
	
	-- Return the distinct first TWO level					
	select Level1, Level2
	from SobekCM_WebContent_Milestones M, SobekCM_WebContent W
	where M.WebContentID=W.WebContentID
	group by Level1, Level2
	order by Level1, Level2;


END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_Get_Usage]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the usage stats for a webcontent page (by ID)
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_Get_Usage]
	@WebContentID int
AS
BEGIN

	-- Get all stats
	select [Year], [Month], Hits, Hits_Complete
	from SobekCM_WebContent_Statistics
	where WebContentID=@WebContentID
	order by [Year], [Month];

END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_Has_Usage]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Pull the flag indicating if this instance has any web content usage logged
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_Has_Usage]
	@value bit output
AS
BEGIN	

	if ( exists ( select 1 from SobekCM_WebContent_Statistics ))
		set @value = 'true';
	else
		set @value = 'false';
	
END;
GO
/****** Object:  StoredProcedure [dbo].[SobekCM_WebContent_Usage_Report]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Pull the usage for all top-level web content pages between two dates
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_WebContent_Usage_Report]
	@year1 smallint,
	@month1 smallint,
	@year2 smallint,
	@month2 smallint
AS
BEGIN	

	with stats_compiled as
	(	
		select Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8, sum(Hits) as Hits, sum(Hits_Complete) as HitsHierarchical
		from SobekCM_WebContent_Statistics
		where ((( [Month] >= @month1 ) and ( [Year] = @year1 )) or ([Year] > @year1 ))
		  and ((( [Month] <= @month2 ) and ( [Year] = @year2 )) or ([Year] < @year2 ))
		group by Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8
	)
	select coalesce(W.Level1, S.Level1) as Level1, coalesce(W.Level2, S.Level2) as Level2, coalesce(W.Level3, S.Level3) as Level3,
	       coalesce(W.Level4, S.Level4) as Level4, coalesce(W.Level5, S.Level5) as Level5, coalesce(W.Level6, S.Level6) as Level6,
		   coalesce(W.Level7, S.Level7) as Level7, coalesce(W.Level8, S.Level8) as Level8, W.Deleted, coalesce(W.Title,'(no title)') as Title, S.Hits, S.HitsHierarchical
	into #TEMP1
	from stats_compiled S left outer join
	     SobekCM_WebContent W on     ( W.Level1=S.Level1 ) 
		                         and ( coalesce(W.Level2,'')=coalesce(S.Level2,''))
								 and ( coalesce(W.Level3,'')=coalesce(S.Level3,''))
								 and ( coalesce(W.Level4,'')=coalesce(S.Level4,''))
								 and ( coalesce(W.Level5,'')=coalesce(S.Level5,''))
								 and ( coalesce(W.Level6,'')=coalesce(S.Level6,''))
								 and ( coalesce(W.Level7,'')=coalesce(S.Level7,''))
								 and ( coalesce(W.Level8,'')=coalesce(S.Level8,''))
	order by Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8;	
	
	-- Return the full stats
	select * from #TEMP1;
	
	-- Return the distinct first level
	select Level1 
	from #TEMP1
	group by Level1
	order by Level1;
	
	-- Return the distinct first TWO level					
	select Level1, Level2
	from #TEMP1
	group by Level1, Level2
	order by Level1, Level2;

END;
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Add_New_Workflow]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[Tracking_Add_New_Workflow]
	@itemid int,
	@user varchar(50),
	@dateStarted DateTime,
	@dateCompleted DateTime,
	@relatedEquipment varchar(1000),
	@EventNumber int,
	@StartEventNumber int,
	@EndEventNumber int,
	@Start_End_Event int,
	@workflow_entry_id int output
AS
begin transaction
	
	begin
		-- Get the workflow id
		declare @workflowid int
		
		-- Get the matching ID for this workflow
			
	    set @workflowid = coalesce((select WorkFlowID from Tracking_Workflow where Start_Event_Number = @EventNumber or End_Event_Number = @EventNumber ), -1);
	
		-- Add this new workflow entry 
		insert into Tracking_Progress ( ItemID, WorkFlowID, DateStarted, DateCompleted, WorkPerformedBy, RelatedEquipment, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number)
		values ( @itemid, @workflowid, @dateStarted, @dateCompleted, @user, @relatedEquipment, @StartEventNumber, @EndEventNumber, @Start_End_Event );
		
		set @workflow_entry_id=@@IDENTITY;
	end
commit transaction
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Add_Workflow_By_ItemID]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[Tracking_Add_Workflow_By_ItemID]
	@itemid int,
	@user varchar(50),
	@progressnote varchar(1000),
	@workflow varchar(100),
	@storagelocation varchar(255)
AS
begin transaction
	    
	-- continue if an itemid was located
	if ( isnull( @itemid, -1 ) > 0 )
	begin
		-- Get the workflow id
		declare @workflowid int;
		if ( ( select COUNT(*) from Tracking_WorkFlow where ( WorkFlowName=@workflow)) > 0 )
		begin
			-- Get the existing ID for this workflow
			select @workflowid = workflowid from Tracking_WorkFlow where WorkFlowName=@workflow;
		end
		else
		begin 
			-- Create the workflow for this
			insert into Tracking_WorkFlow ( WorkFlowName, WorkFlowNotes )
			values ( @workflow, 'Added ' + CONVERT(VARCHAR(10), GETDATE(), 101) + ' by ' + @user );
			
			-- Get this ID
			set @workflowid = SCOPE_IDENTITY();
		end;
	
		-- Just add this new progress then
		insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath )
		values ( @itemid, @workflowid, GETDATE(), @user, @progressnote, @storagelocation );
	end;
commit transaction;
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Add_Workflow_Once_Per_Day]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[Tracking_Add_Workflow_Once_Per_Day]
	@bibid varchar(10),
	@vid varchar(5),
	@user varchar(50),
	@progressnote varchar(1000),
	@workflowid int,
	@storagelocation varchar(255)
AS
begin transaction

	-- Get the volume id
	declare @itemid int

	select @itemid = ItemID
	from SobekCM_Item_Group G, SobekCM_Item I
	where ( BibID = @bibid )
	    and ( I.GroupID = G.GroupID ) 
	    and ( VID = @vid)

	-- continue if an itemid was located
	if ( isnull( @itemid, -1 ) > 0 )
	begin
		-- Does a progress already exist for this which is not completed?
		if ( (select count(*) from Tracking_Progress where ( ItemID = @itemid ) and ( WorkFlowID = @workflowid ) and ( DateCompleted is null )) > 0 )
		begin
			-- If this is to mark it complete, alter existing progress
			update Tracking_Progress
			set DateCompleted = getdate(), WorkPerformedBy = @user, WorkingFilePath=@storagelocation, ProgressNote = @progressnote
			where ( ItemID = @itemid ) and ( WorkFlowID = @workflowid ) and ( DateCompleted is null )
		end
		else
		begin
			-- only enter one of these per day
			if ( (select count(*) from Tracking_Progress where ( ItemID = @itemid ) and ( WorkFlowID=@workflowid ) and ( isnull( CONVERT(VARCHAR(10), DateCompleted, 102), '') = CONVERT(VARCHAR(10), getdate(), 102) )) = 0 )
			begin
				-- Just add this new progress then
				insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath )
				values ( @itemid, @workflowid, getdate(), @user, @progressnote, @storagelocation )
			end
		end
	end
commit transaction
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Delete_Workflow]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* Stored procedure to delete a workflow entry */
CREATE OR ALTER PROCEDURE [dbo].[Tracking_Delete_Workflow]
	@workflow_entry_id int 
AS	
	begin	
	 
		-- Delete this workflow entry 
		delete from Tracking_Progress
		where ProgressID=@workflow_entry_id;	 

	end;
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Digital_Acquisition_Complete]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Marks this volume image processing complete
-- Written by Mark Sullivan 
CREATE OR ALTER PROCEDURE [dbo].[Tracking_Digital_Acquisition_Complete]
	@bibid varchar(10),
	@vid varchar(5),
	@user varchar(255),
	@storagelocation varchar(255),
	@date datetime
AS
begin transaction

	-- Get the volume id
	declare @itemid int
	select @itemid = ItemID
	from SobekCM_Item_Group G, SobekCM_Item I
	where ( BibID = @bibid )
	    and ( I.GroupID = G.GroupID ) 
	    and ( VID = @vid)

	-- continue if a volumeid was located
	if ( isnull( @itemid, -1 ) > 0 )
	begin
		-- Update the milestones
		update SobekCM_Item
		set Milestone_DigitalAcquisition = ISNULL(Milestone_DigitalAcquisition, @date)
		where ItemID=@itemid
	end

commit transaction
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Get_All_Entries_By_User]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

--Stored procedure for getting all the tracking workflow entries by user
--entered through the tracking sheet
CREATE OR ALTER PROCEDURE [dbo].[Tracking_Get_All_Entries_By_User]
	@username nvarchar(50)
	
AS
BEGIN

	
		select P.ItemID,P.ProgressID, W.WorkFlowName, W.Start_Event_Desc, W.End_Event_Desc, W.Start_Event_Number, W.End_Event_Number, W.Start_And_End_Event_Number,
		       P.DateStarted, P.DateCompleted, P.RelatedEquipment, P.WorkPerformedBy, P.WorkingFilePath, P.ProgressNote
		from Tracking_Progress P, Tracking_Workflow W
		where P.WorkFlowID = W.WorkFlowID
		and P.WorkPerformedBy = @username;


END;
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Get_History_Archives]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- version 5 - Removed first table (CDs) and  third table (TIVOLI)
CREATE OR ALTER PROCEDURE [dbo].[Tracking_Get_History_Archives]
	@itemid int
AS
BEGIN	

		-- The (newly) first return table is the progress information for this volume
		select P.WorkFlowID, [Workflow Name]=WorkFlowName, [Completed Date]=isnull(CONVERT(CHAR(10), DateCompleted, 102),''), WorkPerformedBy=isnull(WorkPerformedBy, ''), WorkingFilePath=isnull(WorkingFilePath,''), Note=isnull(ProgressNote,'')
		from Tracking_Progress P, Tracking_Workflow W
		where (P.workflowid = W.workflowid)
		  and (P.ItemID = @itemid )
		order by DateCompleted ASC;		
		
		-- The (newly) second return table has the item information (used by SMaRT app)
		select * from SobekCM_Item where ItemID=@itemid;
		
		-- The (newly) third return table has the aggregations this item is linked to
		select A.Code, A.Name, A.ShortName, A.[Type], L.impliedLink, A.Hidden, A.isActive
		from SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A
		where ( L.ItemID = @ItemID )
		  and ( A.AggregationID = L.AggregationID );
			
END;
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Get_Item_Info_from_ItemID]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[Tracking_Get_Item_Info_from_ItemID]
@itemID int	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	-- Return the item information
	SELECT I.VID, G.BibID, I.Title
	FROM SobekCM_Item I, SobekCM_Item_Group G
	WHERE I.GroupID = G.GroupID
	   AND I.ItemID =@itemID
  
END
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Get_Multiple_Volumes]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Stored procedure returns the information about all the items within a single 
-- title or item/group
-- Written by Mark Sullivan ( November 2006 )
CREATE OR ALTER PROCEDURE [dbo].[Tracking_Get_Multiple_Volumes] 
	@bibid varchar(10)
AS
begin transaction

		-- Return the individual volumes
		select I.ItemID, Title, Level1_Text=isnull(Level1_Text,''), Level1_Index=isnull(Level1_Index,-1), Level2_Text=isnull(Level2_Text, ''), Level2_Index=isnull(Level2_Index, -1), Level3_Text=isnull(Level3_Text, ''), Level3_Index=isnull(Level3_Index, -1), Level4_Text=isnull(Level4_Text, ''), Level4_Index=isnull(Level4_Index, -1), Level5_Text=isnull(Level5_Text, ''), Level5_Index=isnull(Level5_Index,-1), I.MainThumbnail, I.VID, I.IP_Restriction_Mask, I.Author, I.Publisher, I.AggregationCodes, I.Tracking_Box, I.Born_Digital, I.Material_Received_Date, I.Material_Recd_Date_Estimated, I.Disposition_Advice, I.Disposition_Advice_Notes, I.Disposition_Type, I.Disposition_Date, I.Disposition_Notes, PubDate, SortDate, I.SortTitle, I.Last_MileStone, I.Remotely_Archived, I.Locally_Archived
		from SobekCM_Item I, SobekCM_Item_Group G
		where ( G.GroupID = I.GroupID )
		  and ( G.BibID = @bibid )
		  and ( I.Deleted = 'false' )
		  and ( G.Deleted = 'false' )
		order by Level1_Index ASC, Level2_Index ASC, Level3_Index ASC, Level4_Index ASC, Level5_Index ASC, I.SortTitle ASC;

commit transaction
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Get_Scanners_List]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[Tracking_Get_Scanners_List]	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	SELECT ScanningEquipment, Notes, Location,EquipmentType 
	FROM Tracking_ScanningEquipment
	WHERE isActive=1
END
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Get_Users_Scanning_Processing]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[Tracking_Get_Users_Scanning_Processing]	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	SELECT UserName,EmailAddress,FirstName,LastName,ScanningTechnician, ProcessingTechnician 
	FROM mySobek_User
	WHERE ScanningTechnician=1 OR ProcessingTechnician=1
END
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Get_Work_History]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Get the tracking work history against this item and the milestones
CREATE OR ALTER PROCEDURE [dbo].[Tracking_Get_Work_History]
	@bibid varchar(10),
	@vid varchar(5)
AS
BEGIN	

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Get the item id
	declare @itemid int;
	set @itemid = coalesce( ( select I.ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID=G.GroupID and I.VID=@vid and G.BibiD=@bibid ), -1 );

	-- Get the item id
	declare @groupid int;
	set @groupid = coalesce( ( select G.GroupID from SobekCM_Item_Group G where G.BibiD=@bibid ), -1 );

	-- Return all the progress information for this volume
	select P.WorkFlowID, [Workflow Name]=WorkFlowName, [Completed Date]=isnull(CONVERT(CHAR(10), DateCompleted, 102),''), WorkPerformedBy=isnull(WorkPerformedBy, ''), Note=isnull(ProgressNote,''), isnull(WorkPerformedById, -1) as WorkPerformedById
	from Tracking_Progress P, Tracking_Workflow W
	where (P.workflowid = W.workflowid)
	  and (P.ItemID = @itemid )
	order by DateCompleted ASC;		

	-- Return the milestones as well
	select CreateDate, Milestone_DigitalAcquisition, Milestone_ImageProcessing, Milestone_QualityControl, Milestone_OnlineComplete, Material_Received_Date, Disposition_Date from SobekCM_Item where ItemID=@itemid;
		
END
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Image_Processing_Complete]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Marks this volume image processing complete
-- Written by Mark Sullivan 
CREATE OR ALTER PROCEDURE [dbo].[Tracking_Image_Processing_Complete]
	@bibid varchar(10),
	@vid varchar(5),
	@user varchar(255),
	@storagelocation varchar(255),
	@date datetime
AS
begin transaction

	-- Get the volume id
	declare @itemid int
	select @itemid = ItemID
	from SobekCM_Item_Group G, SobekCM_Item I
	where ( BibID = @bibid )
	    and ( I.GroupID = G.GroupID ) 
	    and ( VID = @vid)

	-- continue if a volumeid was located
	if ( isnull( @itemid, -1 ) > 0 )
	begin
		-- Update the milestones
		update SobekCM_Item
		set Milestone_DigitalAcquisition = ISNULL(Milestone_DigitalAcquisition, @date),
		    Milestone_ImageProcessing = ISNULL(Milestone_ImageProcessing, @date)
		where ItemID=@itemid
	end

commit transaction
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Item_Milestone_Report]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[Tracking_Item_Milestone_Report]
	@aggregation_code varchar(20)
AS
BEGIN

	if ( LEN( ISNULL( @aggregation_code,'')) = 0 )
	begin
      select CASE Last_MileStone
                        WHEN 0 THEN 'NO WORK COMPLETED'
                        WHEN 1 THEN 'SCANNED'
                        WHEN 2 THEN 'PROCESSED'
                        WHEN 3 THEN 'QC PERFORMED'
                        WHEN 4 THEN 'ONLINE COMPLETE'
                        ELSE 'DATABASE ERROR'
                  END AS MileStone, title_count=count(distinct(GroupID)), item_count=count(*), page_count = SUM([PageCount]), file_count=SUM(FileCount), Last_MileStone
      from SobekCM_Item
      group by Last_MileStone
      union
      select 'TOTAL', title_count=count(distinct(GroupID)), item_count=count(*), page_count = SUM([PageCount]), file_count=SUM(FileCount), -1
      from SobekCM_Item
      order by Last_MileStone DESC
    end
    else
    begin
    
		declare @aggregationid int
		set @aggregationid = (select top 1 AggregationID from SobekCM_Item_Aggregation where Code=@aggregation_code)
		
		if ( ISNULL(@aggregationid,-1) > 0 )
		begin
		      select CASE Last_MileStone
                        WHEN 0 THEN 'NO WORK COMPLETED'
                        WHEN 1 THEN 'SCANNED'
                        WHEN 2 THEN 'PROCESSED'
                        WHEN 3 THEN 'QC PERFORMED'
                        WHEN 4 THEN 'ONLINE COMPLETE'
                        ELSE 'DATABASE ERROR'
                  END AS MileStone, title_count=count(distinct(GroupID)), item_count=count(*), page_count = SUM([PageCount]), file_count=SUM(FileCount), Last_MileStone
			  from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L
			  where ( I.ItemID = L.ItemID ) and ( L.AggregationID = @aggregationid )
			  group by Last_MileStone
			  union
			  select 'TOTAL', title_count=count(distinct(GroupID)), item_count=count(*), page_count = SUM([PageCount]), file_count=SUM(FileCount), -1
			  from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L
			  where ( I.ItemID = L.ItemID ) and ( L.AggregationID = @aggregationid )
			  order by Last_MileStone DESC
		end   
    end
END
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Item_Visibility_Report]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[Tracking_Item_Visibility_Report]
	@aggregation_code varchar(20)
AS
BEGIN

	if ( LEN( ISNULL( @aggregation_code,'')) = 0 )
	begin
      		    with items_cte as 
			(
				select GroupID, I.ItemID, PageCount, FileCount, 
				  CASE IP_Restriction_Mask WHEN 0 THEN 'PUBLIC' WHEN -1 THEN 'PRIVATE' ELSE 'IP RESTRICTED' END as Restriction,
				  CASE IP_Restriction_Mask WHEN 0 THEN 0 WHEN -1 THEN 4 ELSE 3 END as OrderBy
				from SobekCM_Item I
				where ( I.Deleted='false') 
				  and ( not exists ( select 1 from mySobek_User_Group_Item_Permissions P where P.ItemID=I.ItemID and P.canView='true' ))
				UNION
				select GroupID, I.ItemID, PageCount, FileCount, 'USER GROUP RESTRICTED' as Restriction, 2 as OrderBy
				from SobekCM_Item I
				where ( I.Deleted='false') 
				  and ( exists ( select 1 from mySobek_User_Group_Item_Permissions P where P.ItemID=I.ItemID and P.canView='true' ))
			)
			select Restriction, title_count=count(distinct(GroupID)), item_count=count(*), page_count = SUM([PageCount]), file_count=SUM(FileCount), OrderBy
			from items_cte I
			group by Restriction, OrderBy
			union
			select 'TOTAL', title_count=count(distinct(GroupID)), item_count=count(*), page_count = SUM([PageCount]), file_count=SUM(FileCount), 5 as OrderBy
			from items_cte I
			order by OrderBy;
    end
    else
    begin
    
		declare @aggregationid int
		set @aggregationid = (select top 1 AggregationID from SobekCM_Item_Aggregation where Code=@aggregation_code)
		
		if ( ISNULL(@aggregationid,-1) > 0 )
		begin
		    with items_cte as 
			(
				select GroupID, I.ItemID, PageCount, FileCount, 
				  CASE IP_Restriction_Mask WHEN 0 THEN 'PUBLIC' WHEN -1 THEN 'PRIVATE' ELSE 'IP RESTRICTED' END as Restriction,
				  CASE IP_Restriction_Mask WHEN 0 THEN 0 WHEN -1 THEN 4 ELSE 3 END as OrderBy
				from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L
				where ( I.Deleted='false') 
				  and ( L.ItemID=I.ItemID )
				  and ( L.AggregationID = @aggregationid )
				  and ( not exists ( select 1 from mySobek_User_Group_Item_Permissions P where P.ItemID=I.ItemID and P.canView='true' ))
				UNION
				select GroupID, I.ItemID, PageCount, FileCount, 'USER GROUP RESTRICTED' as Restriction, 2 as OrderBy
				from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L
				where ( I.Deleted='false') 
				  and ( L.ItemID=I.ItemID )
				  and ( L.AggregationID = @aggregationid )
				  and ( exists ( select 1 from mySobek_User_Group_Item_Permissions P where P.ItemID=I.ItemID and P.canView='true' ))
			)
			select Restriction, title_count=count(distinct(GroupID)), item_count=count(*), page_count = SUM([PageCount]), file_count=SUM(FileCount), OrderBy
			from items_cte I
			group by Restriction, OrderBy
			union
			select 'TOTAL', title_count=count(distinct(GroupID)), item_count=count(*), page_count = SUM([PageCount]), file_count=SUM(FileCount), 5 as OrderBy
			from items_cte I
			order by OrderBy;
		end   
    end
END
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Load_Metadata_Update_Complete]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Marks this volume as having processed a metadata update for it
-- This is called when an item successfully passes 'UFDC Loader'
-- Written by Mark Sullivan (April 2007)
CREATE OR ALTER PROCEDURE [dbo].[Tracking_Load_Metadata_Update_Complete]
	@bibid varchar(10),
	@vid varchar(5),
	@user varchar(50),
	@usernotes varchar(1000)
AS
begin transaction

	exec [Tracking_Add_Workflow_Once_Per_Day] @bibid, @vid, @user, @usernotes, 11, null

commit transaction
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Online_Edit_Complete]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Marks this volume as having been edited online
CREATE OR ALTER PROCEDURE [dbo].[Tracking_Online_Edit_Complete]
	@itemid int,
	@user varchar(50),
	@usernotes varchar(1000)
AS
begin transaction

	-- Just add this new progress then
	insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath )
	values ( @itemid, 30, getdate(), @user, @usernotes, '' )

commit transaction
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Online_Submit_Complete]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Marks this volume as having been submitted online
CREATE OR ALTER PROCEDURE [dbo].[Tracking_Online_Submit_Complete]
	@itemid int,
	@user varchar(50),
	@usernotes varchar(1000)
AS
begin transaction

	-- Just add this new progress then
	insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath )
	values ( @itemid, 29, getdate(), @user, @usernotes, '' )

commit transaction
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Submit_Online_Page_Division]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Submit a log about QCing a volume
-- Written by Mark Sullivan ( July 2013 )
CREATE OR ALTER PROCEDURE [dbo].[Tracking_Submit_Online_Page_Division]
	@itemid int,
	@notes varchar(255),
	@onlineuser varchar(100),
	@mainthumbnail varchar(100),
	@mainjpeg varchar(100),
	@pagecount int,
	@filecount int,
	@disksize_kb bigint
AS
begin transaction

		-- Add this new progress 
		if (( select count(*)
		      from Tracking_Progress T
		      where ( T.ItemID=@itemid ) and ( ProgressNote=@notes ) and ( WorkPerformedBy=@onlineuser ) 
		        and ( CONVERT(varchar(10), getdate(), 10) = CONVERT(varchar(10), DateCompleted, 10))
				and ( WorkFlowID=45 )) = 0 )
		begin
			insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, ProgressNote, WorkPerformedBy, WorkingFilePath )
			values ( @itemid, 45, getdate(), @notes, @onlineuser, '' );
		end;
		
		-- Update the QC milestones
		update SobekCM_Item
		set Milestone_DigitalAcquisition = ISNULL(Milestone_DigitalAcquisition, getdate()),
		    Milestone_ImageProcessing = ISNULL(Milestone_ImageProcessing, getdate()),
		    Milestone_QualityControl = ISNULL(Milestone_QualityControl, getdate())
		where ItemID=@itemid;
		
		-- update last mileston
		update SobekCM_Item
		set Last_Milestone = 3
		where ItemID = @itemid and Last_Milestone < 3;
		
		-- If the item is public, update the last milestone as well
		if ( ( select COUNT(*) from SobekCM_Item where ItemID=@itemid and (( Dark = 'true' ) or ( IP_Restriction_Mask >= 0 ))) > 0 )
		begin		
			-- Move along to the COMPLETED milestone
			update SobekCM_Item
			set Milestone_OnlineComplete = ISNULL(Milestone_OnlineComplete, getdate()),
				Last_MileStone=4
			where ItemID=@itemid		
		end;		

		--Update the item table
		update SobekCM_Item set [PageCount]=@pagecount, MainThumbnail = @mainthumbnail, MainJPEG = @mainjpeg, FileCount = @filecount, DiskSize_KB = @disksize_kb
		where ItemID = @itemid;
		
commit transaction
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Update_Digitization_Milestones]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[Tracking_Update_Digitization_Milestones]
	@ItemID int,
	@Last_Milestone int,
	@Milestone_Date date
AS
begin

	-- Digital Acquisition milestone
	if ( @Last_Milestone = 1 )
	begin
		-- Move along to the Post-acquisition processing
		update SobekCM_Item
		set Milestone_DigitalAcquisition = ISNULL(Milestone_DigitalAcquisition, @Milestone_Date),
		    Milestone_ImageProcessing = null,
		    Milestone_QualityControl = null,
		    Milestone_OnlineComplete = null,
		    Last_MileStone=1
		where ItemID=@itemid	
	end
	
	-- Post acquisition processing
	if ( @Last_Milestone = 2 )
	begin
		-- Move along to the Post-acquisition processing
		update SobekCM_Item
		set Milestone_DigitalAcquisition = ISNULL(Milestone_DigitalAcquisition, @Milestone_Date),
		    Milestone_ImageProcessing = ISNULL(Milestone_ImageProcessing, @Milestone_Date),
		    Milestone_QualityControl = null,
		    Milestone_OnlineComplete = null,
		    Last_MileStone=2
		where ItemID=@itemid		
	end
	
	-- Quality control complete
	if ( @Last_Milestone = 3 )
	begin
		-- Move along to the QC Complete milestone
		update SobekCM_Item
		set Milestone_DigitalAcquisition = ISNULL(Milestone_DigitalAcquisition, @Milestone_Date),
		    Milestone_ImageProcessing = ISNULL(Milestone_ImageProcessing, @Milestone_Date),
		    Milestone_QualityControl = ISNULL(Milestone_QualityControl, @Milestone_Date),
		    Milestone_OnlineComplete = null,
		    Last_MileStone=3
		where ItemID=@itemid	
	end
	
	-- Digitization complete
	if ( @Last_Milestone = 4 )
	begin	
		-- Move along to the COMPLETED milestone
		update SobekCM_Item
		set Milestone_DigitalAcquisition = ISNULL(Milestone_DigitalAcquisition, @Milestone_Date),
		    Milestone_ImageProcessing = ISNULL(Milestone_ImageProcessing, @Milestone_Date),
		    Milestone_QualityControl = ISNULL(Milestone_QualityControl, @Milestone_Date),
		    Milestone_OnlineComplete = ISNULL(Milestone_OnlineComplete, @Milestone_Date),
		    Last_MileStone=4
		where ItemID=@itemid	
	end
end
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Update_List]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Procedure pulls the bibs and vids updated on SobekCM since the provided date. 
CREATE OR ALTER PROCEDURE [dbo].[Tracking_Update_List]
      @sinceDate varchar(10)
as
begin
      select BibID, VID, DateCompleted, WorkFlowName, WorkPerformedBy
      from Tracking_WorkFlow W, Tracking_Progress P, SobekCM_Item_Group G, SobekCM_Item I
      where ( W.WorkFlowID = P.WorkFlowID )
        and ( P.ItemID = I.ItemID ) 
        and ( I.GroupID = G.GroupID )
        and (( W.WorkFlowID = 29 ) or ( W.WorkFlowID = 30 ) or ( W.WorkFlowID = 34 ) or ( W.WorkFlowID = 35 ) or ( W.WorkFlowID = 36 ) or ( W.WorkFlowID=40 ) or (W.WorkFlowID=44))
        and ( DateCompleted > @sinceDate )
      order by DateCompleted DESC
end
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Update_Material_Received]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[Tracking_Update_Material_Received]
	@Material_Received_Date datetime,
	@Material_Recd_Date_Estimated bit,
	@ItemID int,
	@User varchar(255),
	@ProgressNote varchar(1000)
AS
begin

	-- Update the item row
	update SobekCM_Item 
	set Material_Received_Date=@Material_Received_Date, Material_Recd_Date_Estimated=@Material_Recd_Date_Estimated
	where ItemID = @ItemID
	
	-- If this is not a widely innacurate estimate, add this as a worklog entry as well
	if ( @Material_Recd_Date_Estimated = 'false' )
	begin
		-- Add into worklog history
		insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath )
		values ( @ItemID, 42, @Material_Received_Date, @User, @ProgressNote, '' )	
	end
end
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Update_Tracking_Box]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[Tracking_Update_Tracking_Box]
	@Tracking_Box varchar(25),
	@ItemID int
AS
begin

	-- Update the item row
	update SobekCM_Item set Tracking_Box = @Tracking_Box where ItemID = @ItemID
	
end
GO
/****** Object:  StoredProcedure [dbo].[Tracking_Update_Workflow]    Script Date: 7/25/2026 6:59:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[Tracking_Update_Workflow]
	@itemid int,
	@user varchar(50),
	@dateStarted DateTime,
	@dateCompleted DateTime,
	@relatedEquipment varchar(1000),
	@EventNumber int,
	@StartEventNumber int,
	@EndEventNumber int,
	@workflow_entry_id int 
AS
	
	begin
		-- Get the workflow id
		declare @workflowid int
		
		-- Get the existing ID for this workflow
			
	    set @workflowid = coalesce((select WorkFlowID from Tracking_Workflow where Start_Event_Number = @EventNumber or End_Event_Number = @EventNumber ), -1);
	
		-- Update this workflow entry 
		Update Tracking_Progress
		set DateStarted=@dateStarted, 
		    DateCompleted=@dateCompleted,
		    RelatedEquipment=@relatedEquipment,
		    Start_Event_Number=@StartEventNumber,
		    End_Event_Number = @EndEventNumber,
		    WorkFlowID = @workflowid,
		    WorkPerformedBy = @user
		where ProgressID=@workflow_entry_id;
		 
	end
GO


/**************************************************************************/
/**                                                                      **/
/**   Grant persmissions on new (and old) stored procedures              **/
/**                                                                      **/
/**************************************************************************/

-- Grant EXECUTE on every stored procedure in the dbo schema to both roles. This is schema-scoped
-- (GRANT ... ON SCHEMA::dbo), so unlike granting each procedure individually it also automatically
-- covers any procedure added to dbo in the future -- no need to remember to add a matching GRANT
-- here every time a new procedure is created. (The previous per-procedure GRANT list had drifted
-- out of sync: as of early 2026 it only covered 88 of the database's 237 procedures.)
GRANT EXECUTE ON SCHEMA::dbo TO sobek_user;
GRANT EXECUTE ON SCHEMA::dbo TO sobek_builder;
GO

/**************************************************************************/
/**                                                                      **/
/**   Data changes                                                       **/
/**                                                                      **/
/**************************************************************************/

delete from SobekCM_Settings where Setting_Key='Document Solr Legacy Index';
delete from SobekCM_Settings where Setting_Key='Page Solr Legacy Index';
GO

update SobekCM_Settings set Setting_Value='OpenSobek', Options='OpenSobek' , Help='Which system and schema to use for searching' where Setting_Key='Search System';
GO

Update SobekCM_Metadata_Types set SolrCode='', LegacySolrCode='' where MetadataName='ZT All Taxonomy';
Update SobekCM_Metadata_Types set SolrCode='temporal_decade', LegacySolrCode='temporal_decade' where MetadataName='Temporal Decade';
Update SobekCM_Metadata_Types set SolrCode='temporal_subject', LegacySolrCode='temporal_subject' where MetadataName='Temporal Subject';
Update SobekCM_Metadata_Types set SolrCode='temporal_year', LegacySolrCode='temporal_year' where MetadataName='Temporal Year';
Update SobekCM_Metadata_Types set SolrCode='User_Description', LegacySolrCode='User_Description' where MetadataName='User Description';
GO

delete from dbo.SobekCM_Settings where Setting_Key='FDA Report DropBox';
delete from dbo.SobekCM_Settings where Setting_Key='Mango Union Search Base URL';
delete from dbo.SobekCM_Settings where Setting_Key='Mango Union Search Text';
delete from dbo.SobekCM_Settings where Setting_Key='Spreadsheet Library License';
GO

delete from SobekCM_Builder_Module where Class='SobekCM.Builder_Library.Modules.Items.SaveToSolrLuceneModule_Legacy';
delete from SobekCM_Builder_Module where Class='SobekCM.Builder_Library.Modules.Schedulable.UpdatedCachedAggregationMetadataModule';
GO

BEGIN TRANSACTION;

DECLARE @DefaultsMatch BIT = 0;

IF EXISTS (SELECT 1 FROM SobekCM_Settings WHERE Setting_Key = 'PostArchive Files To Delete' AND Setting_Value = '(.*?)\.(tif)')
   AND EXISTS (SELECT 1 FROM SobekCM_Settings WHERE Setting_Key = 'PreArchive Files To Delete' AND Setting_Value = '(.*?)\.(QC.jpg)')
    SET @DefaultsMatch = 1;

IF @DefaultsMatch = 1
    UPDATE SobekCM_Settings
    SET Setting_Value = '(.*?)\.(tif|QC\.jpg)'
    WHERE Setting_Key = 'PostArchive Files To Delete';

UPDATE SobekCM_Settings
SET Setting_Key = 'Files To Omit From Archive'
WHERE Setting_Key = 'PreArchive Files To Delete';

COMMIT TRANSACTION;
GO


-- Make room for creating master TIFFs from JPEG2000s or JPEGs
UPDATE SobekCM_Builder_Module 
SET [Order]=[Order] + 2 
WHERE [Order] >= ( select [Order] from SobekCM_Builder_Module where Class='SobekCM.Builder_Library.Modules.Items.OcrTiffsModule');
GO

INSERT into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, [Assembly], Class, [Enabled], [Order])
VALUES ( 3, 'Convert JPEG2000s to NonMaster TIFFs', null, 'SobekCM.Builder_Library.Modules.Items.ConvertJpeg2000sItemModule', 'true', (select [Order]-2 from SobekCM_Builder_Module where Class='SobekCM.Builder_Library.Modules.Items.OcrTiffsModule'));
GO

UPDATE SobekCM_Builder_Module 
SET [Order]= ( select [Order] - 1 from SobekCM_Builder_Module where Class='SobekCM.Builder_Library.Modules.Items.OcrTiffsModule') 
WHERE class='SobekCM.Builder_Library.Modules.Items.ConvertLargeJpegsItemModule';
GO

UPDATE SobekCM_Builder_Module
SET [Order]=[Order] + 1
WHERE [Order] >=  ( select [Order] from SobekCM_Builder_Module where Class='SobekCM.Builder_Library.Modules.Items.CopyToArchiveFolderModule');
GO

INSERT into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, [Assembly], Class, [Enabled], [Order])
VALUES ( 3, 'Delete any NonMaster TIFFs', null, 'SobekCM.Builder_Library.Modules.Items.DeleteNonMasterTiffsModule', 'true', (select [Order]-1 from SobekCM_Builder_Module where Class='SobekCM.Builder_Library.Modules.Items.CopyToArchiveFolderModule'));
GO

UPDATE SobekCM_Builder_Module
SET [Order]=[Order] + 1
WHERE [Order] >=  ( select [Order] from SobekCM_Builder_Module where Class='SobekCM.Builder_Library.Modules.Items.MoveFilesToImageServerModule');
GO

INSERT into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, [Assembly], Class, [Enabled], [Order])
VALUES ( 3, 'Delete any files that should not be retained', null, 'SobekCM.Builder_Library.Modules.Items.DeleteNonRetainedFilesModule', 'true', (select [Order]-1 from SobekCM_Builder_Module where Class='SobekCM.Builder_Library.Modules.Items.MoveFilesToImageServerModule'));
GO

UPDATE SobekCM_Builder_Module
SET [Order]=[Order] + 1
WHERE Class='SobekCM.Builder_Library.Modules.Items.AttachImagesAllModule';
GO

DELETE FROM SobekCM_Builder_Module
WHERE Class='SobekCM.Builder_Library.Modules.Items.AddNewImagesAndViewsModule';
GO

UPDATE SobekCM_Builder_Module
SET ModuleDesc = 'Build static version for SEO'
WHERE Class='SobekCM.Builder_Library.Modules.Items.CreateStaticVersionModule';
GO

UPDATE SobekCM_Builder_Module
SET [Order] = [Order] * 10;
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
	values ( 5, 0, '0' );
end
else
begin
	update SobekCM_Database_Version
	set Major_Version=5, Minor_Version=0, Release_Phase='0';
end;
GO