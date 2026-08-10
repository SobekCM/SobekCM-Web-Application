/**
Ver5_DB_Complete_PostgreSQL.sql  - OpenSobek

PostgreSQL port of Ver5_DB_Complete.sql. Builds a complete Version 5.0.0 database
from scratch. Creates the `sobek_builder`/`sobek_user` roles, all tables, views, and
stored procedures (ported to PostgreSQL functions -- see note below), grants EXECUTE
on every routine to both roles, then loads all the reference/seed data every
installation needs (settings, metadata field definitions, viewer types, builder
modules, mime types, workflow types, etc.). Run this against a brand-new, empty
PostgreSQL database (createdb, then run this script with psql).

Porting notes:
 - The "dbo" schema is dropped; all objects live in the default "public" schema.
 - SQL Server stored procedures are ported to PostgreSQL FUNCTIONs (not native
   PostgreSQL PROCEDUREs), since functions can return result sets (RETURNS TABLE /
   SETOF) and OUT parameters the way the existing ADO.NET call pattern in
   EalDbAccess.cs (CommandType.StoredProcedure, ExecuteDataset/ExecuteDataReader/
   ExecuteNonQuery) expects. No C# changes were required to support this.
 - Identifiers are unquoted (and therefore case-folded to lowercase by PostgreSQL)
   except for a couple of column names that collide with reserved words ("Order",
   "Default"), which stay double-quoted with their original casing everywhere they
   are referenced.
 - int/bigint/smallint IDENTITY columns become GENERATED ALWAYS AS IDENTITY.
   bit -> boolean, datetime -> timestamp, nvarchar/varchar(max) -> text,
   float -> double precision, tinyint -> smallint (Postgres has no 1-byte int type).
 - The two SQL-Server "indexed views" (WITH SCHEMABINDING + unique clustered index)
   are ported as plain views -- PostgreSQL has no automatically-maintained
   materialized/indexed view equivalent; a real MATERIALIZED VIEW would need an
   explicit REFRESH and was judged unnecessary for this reporting-only view.
 */


/** !START_DATABASE_CREATE! **/

/** !START_CREATE_ROLES! **/

CREATE ROLE sobek_builder;
CREATE ROLE sobek_user;

/** !START_CREATE_TABLES! **/

CREATE TABLE SobekCM_Item_Aggregation_Item_Link(
	ItemID integer NOT NULL,
	AggregationID integer NOT NULL,
	impliedLink boolean NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Aggregation_Item_Link PRIMARY KEY 
(
	ItemID,
	AggregationID
)
);
/****** Object:  Table SobekCM_Item    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item(
	ItemID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	VID varchar(5) NOT NULL,
	PageCount integer NOT NULL,
	TextSearchable boolean NOT NULL,
	AssocFilePath varchar(50) NULL,
	Deleted boolean NOT NULL,
	Title varchar(500) NOT NULL,
	AccessMethod integer NOT NULL,
	Link varchar(500) NULL,
	CreateDate timestamp NULL,
	PubYear integer NULL,
	Locked boolean NOT NULL,
	MainThumbnail varchar(100) NULL,
	MainJPEG varchar(100) NULL,
	PubDate varchar(100) NULL,
	SortDate bigint NULL,
	Country varchar(250) NULL,
	State varchar(250) NULL,
	County varchar(250) NULL,
	City varchar(250) NULL,
	MainLatitude varchar(25) NULL,
	MainLongitude varchar(25) NULL,
	FileCount integer NOT NULL,
	Format varchar(100) NOT NULL,
	Donor varchar(250) NULL,
	Publisher varchar(1000) NULL,
	Author varchar(1000) NULL,
	Spatial_KML varchar(4000) NULL,
	GroupID integer NOT NULL,
	Level1_Text varchar(255) NULL,
	Level1_Index integer NULL,
	Level2_Text varchar(255) NULL,
	Level2_Index integer NULL,
	Level3_Text varchar(255) NULL,
	Level3_Index integer NULL,
	Level4_Text varchar(255) NULL,
	Level4_Index integer NULL,
	Level5_Text varchar(255) NULL,
	Level5_Index integer NULL,
	CheckoutRequired boolean NOT NULL,
	Spatial_KML_Distance double precision NOT NULL,
	DiskSize_KB bigint NOT NULL,
	IP_Restriction_Mask smallint NOT NULL,
	IncludeInAll boolean NOT NULL,
	SuppressOAI boolean NOT NULL,
	LastSaved timestamp NULL,
	VIDSource varchar(150) NULL,
	CreateYear smallint NOT NULL,
	CreateMonth smallint NOT NULL,
	Internal_Comments varchar(1000) NULL,
	TEMP_SourceCode varchar(10) NULL,
	TEMP_HoldingCode varchar(10) NULL,
	Dark boolean NOT NULL,
	CopyrightIndicator smallint NULL,
	VolumeID integer NOT NULL,
	Last_MileStone integer NOT NULL,
	Milestone_DigitalAcquisition timestamp NULL,
	Milestone_ImageProcessing timestamp NULL,
	Milestone_QualityControl timestamp NULL,
	Milestone_OnlineComplete timestamp NULL,
	Born_Digital boolean NOT NULL,
	Material_Received_Date timestamp NULL,
	Disposition_Advice integer NULL,
	Disposition_Date timestamp NULL,
	Disposition_Type integer NULL,
	Locally_Archived boolean NOT NULL,
	Remotely_Archived boolean NOT NULL,
	Material_Recd_Date_Estimated boolean NOT NULL,
	Tracking_Box varchar(25) NULL,
	AggregationCodes varchar(100) NULL,
	Left_To_Right boolean NOT NULL,
	Disposition_Advice_Notes varchar(150) NOT NULL,
	Disposition_Notes varchar(150) NOT NULL,
	Spatial_Display varchar(1000) NULL,
	Institution_Display varchar(1000) NULL,
	Edition_Display varchar(1000) NULL,
	Material_Display varchar(1000) NULL,
	Measurement_Display varchar(1000) NULL,
	StylePeriod_Display varchar(1000) NULL,
	Technique_Display varchar(1000) NULL,
	Subjects_Display varchar(1000) NULL,
	AdditionalWorkNeeded boolean NOT NULL,
	ExposeFullTextForHarvesting boolean NOT NULL,
	Total_Hits bigint NOT NULL,
	Total_Sessions bigint NOT NULL,
	SortTitle varchar(500) NOT NULL,
	TivoliSize_MB bigint NOT NULL,
	TivoliSize_Calculated timestamp NOT NULL,
	metadataProfile varchar(50) NOT NULL,
	COinS_OpenURL text NULL,
	Complete_KML text NULL,
	SpatialFootprint varchar(255) NULL,
	SpatialFootprintDistance double precision NOT NULL,
	CitationSet varchar(50) NULL,
	MadePublicDate timestamp NULL,
	RestrictionMessage varchar(1024) NULL,
 CONSTRAINT PK_Item PRIMARY KEY 
(
	ItemID
)
);

/****** Object:  Table Archive_Item_Archived_File    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE Archive_Item_Archived_File(
	ArchivedFileID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ItemID integer NOT NULL,
	FileName varchar(255) NOT NULL,
	FileExtension varchar(20) NOT NULL,
 CONSTRAINT PK_Archive_Item_Archived_File PRIMARY KEY 
(
	ArchivedFileID
),
 CONSTRAINT UQ_Archived_File_Item_FileName UNIQUE 
(
	ItemID,
	FileName
)
);
/****** Object:  Table Archive_Item_Archived_File_Copy    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE Archive_Item_Archived_File_Copy(
	ArchivedFileCopyID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	SnapshotID integer NOT NULL,
	ArchiveLocationID smallint NOT NULL,
	StoragePath varchar(1000) NOT NULL,
	StoredDate timestamp NOT NULL,
	VerifiedDate timestamp NULL,
	Status varchar(20) NOT NULL,
 CONSTRAINT PK_Archived_File_Copy PRIMARY KEY 
(
	ArchivedFileCopyID
)
);
/****** Object:  Table Archive_Item_Archived_File_Snapshot    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE Archive_Item_Archived_File_Snapshot(
	SnapshotID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ArchivedFileID integer NOT NULL,
	FileSize bigint NOT NULL,
	OriginalCreationDate timestamp NOT NULL,
	SHA256_Hash char(64) NOT NULL,
	SnapshotDate timestamp NOT NULL,
	MimeType varchar(100) NULL,
	EncodingDetails varchar(500) NULL,
 CONSTRAINT PK_Archived_File_Snapshot PRIMARY KEY 
(
	SnapshotID
)
);
/****** Object:  Table Archive_Location    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE Archive_Location(
	ArchiveLocationID smallint GENERATED ALWAYS AS IDENTITY NOT NULL,
	LocationName varchar(50) NOT NULL,
	LocationType varchar(20) NOT NULL,
	ContainerName varchar(255) NULL,
	IsActive boolean NOT NULL,
	Notes varchar(500) NULL,
 CONSTRAINT PK_SobekCM_Archive_Location PRIMARY KEY 
(
	ArchiveLocationID
)
);
/****** Object:  Table Auth_GeoRegion    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE Auth_GeoRegion(
	RegionID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	GeoAuthCode varchar(12) NOT NULL,
	RegionName varchar(255) NOT NULL,
	RegionTypeID integer NOT NULL,
	RegionFIPSCode varchar(10) NULL,
	ParentRegionID integer NULL,
 CONSTRAINT PK_GEMS_GeoRegion PRIMARY KEY 
(
	RegionID
)
);
/****** Object:  Table Auth_GeoRegion_Type    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE Auth_GeoRegion_Type(
	RegionTypeID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	RegionTypeName varchar(50) NOT NULL,
 CONSTRAINT PK_GEMS_GeoRegionType PRIMARY KEY 
(
	RegionTypeID
)
);
/****** Object:  Table mySobek_DefaultMetadata    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_DefaultMetadata(
	DefaultMetadataID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	MetadataName varchar(100) NOT NULL,
	MetadataCode varchar(20) NOT NULL,
	UserID integer NULL,
	Description varchar(255) NOT NULL,
 CONSTRAINT PK_mySobek_DefaultMetadata PRIMARY KEY 
(
	DefaultMetadataID
)
);
/****** Object:  Table mySobek_Editable_Regex    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_Editable_Regex(
	EditableID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Editable_Name varchar(250) NOT NULL,
	EditableRegex text NOT NULL,
 CONSTRAINT PK_UFDC_Editable_Regex PRIMARY KEY 
(
	EditableID
)
);
/****** Object:  Table mySobek_Template    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_Template(
	TemplateID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	TemplateName varchar(100) NOT NULL,
	TemplateCode varchar(20) NOT NULL,
	Description varchar(255) NOT NULL,
 CONSTRAINT PK_UFDC_Template PRIMARY KEY 
(
	TemplateID
)
);
/****** Object:  Table mySobek_User    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User(
	UserID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ShibbID char(8) NULL,
	UserName varchar(100) NULL,
	Password varchar(100) NULL,
	EmailAddress varchar(100) NULL,
	FirstName varchar(100) NULL,
	LastName varchar(100) NULL,
	DateCreated timestamp NOT NULL,
	LastActivity timestamp NOT NULL,
	isActive boolean NOT NULL,
	Note_Length integer NOT NULL,
	Can_Make_Folders_Public boolean NOT NULL,
	isTemporary_Password boolean NOT NULL,
	sendEmailOnSubmission boolean NOT NULL,
	Can_Submit_Items boolean NOT NULL,
	Lock_Out_Count integer NULL,
	Lock_Out_Date timestamp NULL,
	NickName varchar(100) NULL,
	Organization varchar(250) NULL,
	College varchar(250) NULL,
	Department varchar(250) NULL,
	Unit varchar(250) NULL,
	Default_Rights varchar(1000) NULL,
	UI_Language varchar(50) NULL,
	Internal_User boolean NOT NULL,
	OrganizationCode varchar(15) NOT NULL,
	EditTemplate varchar(20) NOT NULL,
	EditTemplateMarc varchar(20) NOT NULL,
	Receive_Stats_Emails boolean NOT NULL,
	Has_Item_Stats boolean NOT NULL,
	IsSystemAdmin boolean NOT NULL,
	IsPortalAdmin boolean NOT NULL,
	Include_Tracking_Standard_Forms boolean NOT NULL,
	qcProfile varchar(50) NOT NULL,
	Can_Delete_All_Items boolean NOT NULL,
	ScanningTechnician boolean NOT NULL,
	ProcessingTechnician boolean NOT NULL,
	InternalNotes varchar(500) NULL,
	IsHostAdmin boolean NOT NULL,
	IsUserAdmin boolean NOT NULL,
	ExternalProviderCode varchar(50) NULL,
	ExternalSubjectId varchar(450) NULL,
	AuthenticationSource varchar(100) NOT NULL,
 CONSTRAINT PK_sobek_user PRIMARY KEY 
(
	UserID
)
);
/****** Object:  Table mySobek_User_Bib_Link    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Bib_Link(
	UserID integer NOT NULL,
	GroupID integer NOT NULL,
 CONSTRAINT PK_mySobek_User_Bib_Link PRIMARY KEY 
(
	UserID,
	GroupID
)
);
/****** Object:  Table mySobek_User_DefaultMetadata_Link    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_DefaultMetadata_Link(
	UserID integer NOT NULL,
	DefaultMetadataID integer NOT NULL,
	CurrentlySelected boolean NOT NULL,
 CONSTRAINT PK_mySobek_User_DefaultMetadata_Link PRIMARY KEY 
(
	UserID,
	DefaultMetadataID
)
);
/****** Object:  Table mySobek_User_Description_Tags    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Description_Tags(
	TagID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	UserID integer NOT NULL,
	Description_Tag varchar(2000) NOT NULL,
	Date_Modified timestamp NOT NULL,
	ItemID integer NOT NULL,
 CONSTRAINT PK_sobek_user_Description_Tags PRIMARY KEY 
(
	TagID
)
);
/****** Object:  Table mySobek_User_Edit_Aggregation    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Edit_Aggregation(
	UserID integer NOT NULL,
	AggregationID integer NOT NULL,
	CanSelect boolean NOT NULL,
	CanEditItems boolean NOT NULL,
	OnHomePage boolean NOT NULL,
	IsCurator boolean NOT NULL,
	IsAdmin boolean NOT NULL,
	CanEditMetadata boolean NOT NULL,
	CanEditBehaviors boolean NOT NULL,
	CanPerformQc boolean NOT NULL,
	CanUploadFiles boolean NOT NULL,
	CanChangeVisibility boolean NOT NULL,
	CanDelete boolean NOT NULL,
 CONSTRAINT PK_sobek_user_Edit_Aggregation PRIMARY KEY 
(
	UserID,
	AggregationID
)
);
/****** Object:  Table mySobek_User_Editable_Link    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Editable_Link(
	UserID integer NOT NULL,
	EditableID integer NOT NULL,
	CanEditMetadata boolean NOT NULL,
	CanEditBehaviors boolean NOT NULL,
	CanPerformQc boolean NOT NULL,
	CanUploadFiles boolean NOT NULL,
	CanChangeVisibility boolean NOT NULL,
	CanDelete boolean NOT NULL,
 CONSTRAINT PK_sobek_user_Editable_Link PRIMARY KEY 
(
	UserID,
	EditableID
)
);
/****** Object:  Table mySobek_User_Folder    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Folder(
	UserFolderID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ParentFolderID integer NULL,
	UserID integer NOT NULL,
	FolderName varchar(255) NOT NULL,
	isPublic boolean NOT NULL,
	FolderDescription varchar(4000) NOT NULL,
 CONSTRAINT PK_sobek_user_Folder PRIMARY KEY 
(
	UserFolderID
)
);
/****** Object:  Table mySobek_User_Group    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Group(
	UserGroupID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	GroupName varchar(150) NOT NULL,
	GroupDescription varchar(1000) NOT NULL,
	Can_Submit_Items boolean NOT NULL,
	Internal_User boolean NOT NULL,
	IsSystemAdmin boolean NOT NULL,
	IsPortalAdmin boolean NOT NULL,
	Include_Tracking_Standard_Forms boolean NOT NULL,
	autoAssignUsers boolean NOT NULL,
	Can_Delete_All_Items boolean NOT NULL,
	IsSobekDefault boolean NOT NULL,
	IsShibbolethDefault boolean NOT NULL,
	IsLdapDefault boolean NOT NULL,
	IsSpecialGroup boolean NOT NULL,
 CONSTRAINT PK_mySobek_User_Group PRIMARY KEY 
(
	UserGroupID
)
);
/****** Object:  Table mySobek_User_Group_DefaultMetadata_Link    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Group_DefaultMetadata_Link(
	UserGroupID integer NOT NULL,
	DefaultMetadataID integer NOT NULL,
 CONSTRAINT PK_sobek_user_Group_DefaultMetadata_Link PRIMARY KEY 
(
	UserGroupID,
	DefaultMetadataID
)
);
/****** Object:  Table mySobek_User_Group_Edit_Aggregation    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Group_Edit_Aggregation(
	UserGroupID integer NOT NULL,
	AggregationID integer NOT NULL,
	CanSelect boolean NOT NULL,
	CanEditItems boolean NOT NULL,
	IsCurator boolean NOT NULL,
	IsAdmin boolean NOT NULL,
	CanEditMetadata boolean NOT NULL,
	CanEditBehaviors boolean NOT NULL,
	CanPerformQc boolean NOT NULL,
	CanUploadFiles boolean NOT NULL,
	CanChangeVisibility boolean NOT NULL,
	CanDelete boolean NOT NULL,
 CONSTRAINT PK_sobek_user_Group_Edit_Aggregation PRIMARY KEY 
(
	UserGroupID,
	AggregationID
)
);
/****** Object:  Table mySobek_User_Group_Editable_Link    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Group_Editable_Link(
	UserGroupID integer NOT NULL,
	EditableID integer NOT NULL,
	CanEditMetadata boolean NOT NULL,
	CanEditBehaviors boolean NOT NULL,
	CanPerformQc boolean NOT NULL,
	CanUploadFiles boolean NOT NULL,
	CanChangeVisibility boolean NOT NULL,
	CanDelete boolean NOT NULL,
 CONSTRAINT PK_sobek_user_Group_Editable_Link PRIMARY KEY 
(
	UserGroupID,
	EditableID
)
);
/****** Object:  Table mySobek_User_Group_Item_Permissions    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Group_Item_Permissions(
	UserGroupPermissionID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	UserGroupID integer NOT NULL,
	ItemID integer NULL,
	isOwner boolean NOT NULL,
	canView boolean NULL,
	canEditMetadata boolean NULL,
	canEditBehaviors boolean NULL,
	canPerformQc boolean NULL,
	canUploadFiles boolean NULL,
	canChangeVisibility boolean NULL,
	canDelete boolean NULL,
	customPermissions text NULL,
	isDefaultPermissions boolean NOT NULL,
 CONSTRAINT PK_mySobek_User_Group_Item_Permissions PRIMARY KEY 
(
	UserGroupPermissionID
)
);
/****** Object:  Table mySobek_User_Group_Link    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Group_Link(
	UserID integer NOT NULL,
	UserGroupID integer NOT NULL,
	IsGroupAdmin boolean NOT NULL,
 CONSTRAINT PK_mySobek_User_Group_Link PRIMARY KEY 
(
	UserID,
	UserGroupID
)
);
/****** Object:  Table mySobek_User_Group_Template_Link    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Group_Template_Link(
	UserGroupID integer NOT NULL,
	TemplateID integer NOT NULL,
 CONSTRAINT PK_sobek_user_Group_Template_Link PRIMARY KEY 
(
	UserGroupID,
	TemplateID
)
);
/****** Object:  Table mySobek_User_Item    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Item(
	UserItemID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	UserFolderID integer NOT NULL,
	ItemOrder integer NOT NULL,
	UserNotes varchar(2000) NULL,
	DateAdded timestamp NOT NULL,
	ItemID integer NOT NULL,
 CONSTRAINT PK_sobek_user_Item PRIMARY KEY 
(
	UserItemID
)
);
/****** Object:  Table mySobek_User_Item_Link    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Item_Link(
	UserID integer NOT NULL,
	ItemID integer NOT NULL,
	RelationshipID integer NOT NULL,
 CONSTRAINT PK_mySobek_User_Item_Link PRIMARY KEY 
(
	UserID,
	ItemID,
	RelationshipID
)
);
/****** Object:  Table mySobek_User_Item_Link_Relationship    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Item_Link_Relationship(
	RelationshipID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	RelationshipLabel varchar(50) NOT NULL,
	Include_In_Results boolean NOT NULL,
 CONSTRAINT PK_mySobek_User_Item_Link_Relationship PRIMARY KEY 
(
	RelationshipID
)
);
/****** Object:  Table mySobek_User_Item_Permissions    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Item_Permissions(
	UserPermissionID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	UserID integer NOT NULL,
	ItemID integer NOT NULL,
	isOwner boolean NOT NULL,
	canView boolean NULL,
	canEditMetadata boolean NULL,
	canEditBehaviors boolean NULL,
	canPerformQc boolean NULL,
	canUploadFiles boolean NULL,
	canChangeVisibility boolean NULL,
	canDelete boolean NULL,
	customPermissions text NULL,
 CONSTRAINT PK_mySobek_User_Item_Permissions PRIMARY KEY 
(
	UserPermissionID
)
);
/****** Object:  Table mySobek_User_Request    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Request(
	UserRequestID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	UserID integer NOT NULL,
	UserGroupID integer NULL,
	RequestDate timestamp NOT NULL,
	RequestSubmitPermissions boolean NOT NULL,
	RequestUrl varchar(255) NULL,
	Pending boolean NOT NULL,
	Approved boolean NOT NULL,
	Notes varchar(2000) NULL,
	ApproverUserID integer NULL,
	Code varchar(255) NULL,
 CONSTRAINT PK_mySobek_User_Request PRIMARY KEY 
(
	UserRequestID
)
);
/****** Object:  Table mySobek_User_Search    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Search(
	UserSearchID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	UserID integer NOT NULL,
	SearchURL varchar(500) NOT NULL,
	SearchDescription varchar(500) NOT NULL,
	ItemOrder integer NOT NULL,
	UserNotes varchar(2000) NOT NULL,
	DateAdded timestamp NOT NULL,
 CONSTRAINT PK_sobek_user_Search PRIMARY KEY 
(
	UserSearchID
)
);
/****** Object:  Table mySobek_User_Settings    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Settings(
	UserSettingID bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
	UserID integer NOT NULL,
	Setting_Key varchar(255) NOT NULL,
	Setting_Value text NOT NULL,
 CONSTRAINT PK_mySobek_User_Settings PRIMARY KEY 
(
	UserSettingID
)
);
/****** Object:  Table mySobek_User_Template_Link    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE mySobek_User_Template_Link(
	UserID integer NOT NULL,
	TemplateID integer NOT NULL,
	DefaultTemplate boolean NOT NULL,
 CONSTRAINT PK_sobek_user_Template_Link PRIMARY KEY 
(
	UserID,
	TemplateID
)
);
/****** Object:  Table SobekCM_Browse_Info_Statistics    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Browse_Info_Statistics(
	BrowseInfoStatsID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	AggregationID integer NULL,
	BrowseInfoCode varchar(150) NOT NULL,
	Year smallint NOT NULL,
	Month smallint NOT NULL,
	Hits integer NOT NULL,
 CONSTRAINT PK_SobekCM_Browse_Info_Statistics PRIMARY KEY 
(
	BrowseInfoStatsID
)
);
/****** Object:  Table SobekCM_Builder_Incoming_Folders    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Builder_Incoming_Folders(
	IncomingFolderId integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	NetworkFolder varchar(255) NOT NULL,
	ErrorFolder varchar(255) NOT NULL,
	ProcessingFolder varchar(255) NOT NULL,
	Perform_Checksum_Validation boolean NOT NULL,
	Archive_TIFF boolean NOT NULL,
	Archive_All_Files boolean NOT NULL,
	Allow_Deletes boolean NOT NULL,
	Allow_Folders_No_Metadata boolean NOT NULL,
	Allow_Metadata_Updates boolean NOT NULL,
	FolderName varchar(150) NOT NULL,
	Can_Move_To_Content_Folder boolean NULL,
	BibID_Roots_Restrictions varchar(255) NOT NULL,
	ModuleSetID integer NULL,
 CONSTRAINT PK_Builder_Incoming_Folders PRIMARY KEY 
(
	IncomingFolderId
)
);
/****** Object:  Table SobekCM_Builder_Log    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Builder_Log(
	BuilderLogID bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
	RelatedBuilderLogID bigint NULL,
	LogDate timestamp NULL,
	BibID_VID varchar(16) NULL,
	LogType varchar(25) NULL,
	LogMessage varchar(2000) NULL,
	SuccessMessage varchar(500) NULL,
	METS_Type varchar(50) NULL,
 CONSTRAINT PK_SobekCM_Builder_Log PRIMARY KEY 
(
	BuilderLogID
)
);
/****** Object:  Table SobekCM_Builder_Module    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Builder_Module(
	ModuleID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ModuleSetID integer NOT NULL,
	ModuleDesc varchar(200) NOT NULL,
	Assembly varchar(250) NULL,
	Class varchar(500) NOT NULL,
	Enabled boolean NOT NULL,
	"Order" integer NOT NULL,
	Argument1 text NULL,
	Argument2 text NULL,
	Argument3 text NULL,
 CONSTRAINT PK_SobekCM_Builder_Module PRIMARY KEY 
(
	ModuleID
)
);
/****** Object:  Table SobekCM_Builder_Module_Schedule    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Builder_Module_Schedule(
	ModuleScheduleID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ModuleSetID integer NOT NULL,
	DaysOfWeek varchar(7) NOT NULL,
	Enabled boolean NOT NULL,
	TimesOfDay varchar(100) NOT NULL,
	Description varchar(250) NOT NULL,
 CONSTRAINT PK_SobekCM_Builder_Module_Schedule PRIMARY KEY 
(
	ModuleScheduleID
)
);
/****** Object:  Table SobekCM_Builder_Module_Scheduled_Run    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Builder_Module_Scheduled_Run(
	ModuleSchedRunID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ModuleScheduleID integer NOT NULL,
	Timestamp timestamp NOT NULL,
	Outcome varchar(100) NOT NULL,
	Message text NULL,
 CONSTRAINT PK_SobekCM_Builder_Module_Scheduled_Run PRIMARY KEY 
(
	ModuleSchedRunID
)
);
/****** Object:  Table SobekCM_Builder_Module_Set    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Builder_Module_Set(
	ModuleSetID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ModuleTypeID integer NOT NULL,
	SetName varchar(50) NOT NULL,
	SetOrder integer NOT NULL,
	Enabled boolean NOT NULL,
 CONSTRAINT PK_SobekCM_Builder_Module_Set PRIMARY KEY 
(
	ModuleSetID
)
);
/****** Object:  Table SobekCM_Builder_Module_Type    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Builder_Module_Type(
	ModuleTypeID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	TypeAbbrev varchar(4) NOT NULL,
	TypeDescription varchar(200) NOT NULL,
 CONSTRAINT PK_SobekCM_Builder_Module_Types PRIMARY KEY 
(
	ModuleTypeID
)
);
/****** Object:  Table SobekCM_Database_Version    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Database_Version(
	Major_Version integer NULL,
	Minor_Version integer NULL,
	Release_Phase varchar(10) NULL
);
/****** Object:  Table SobekCM_Email_Log    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Email_Log(
	EmailID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Sender varchar(255) NOT NULL,
	Receipt_List varchar(500) NOT NULL,
	Subject_Line varchar(500) NOT NULL,
	Email_Body text NOT NULL,
	Sent_Date timestamp NOT NULL,
	HTML_Format boolean NOT NULL,
	Contact_Us boolean NOT NULL,
	ReplyToEmailId integer NULL,
	UserID integer NULL,
 CONSTRAINT PK_SobekCM_Email_Log PRIMARY KEY 
(
	EmailID
)
);
/****** Object:  Table SobekCM_Extension    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Extension(
	ExtensionID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Code varchar(50) NOT NULL,
	Name varchar(255) NOT NULL,
	CurrentVersion varchar(50) NOT NULL,
	IsEnabled boolean NOT NULL,
	EnabledDate timestamp NULL,
	LicenseKey text NULL,
	UpgradeUrl varchar(255) NULL,
	LatestVersion varchar(50) NULL,
 CONSTRAINT PK_SobekCM_Extension PRIMARY KEY 
(
	ExtensionID
)
);
/****** Object:  Table SobekCM_External_Record_Type    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_External_Record_Type(
	ExtRecordTypeID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ExtRecordType varchar(25) NOT NULL,
	repeatableTypeFlag boolean NOT NULL,
 CONSTRAINT PK_External_Record_Type PRIMARY KEY 
(
	ExtRecordTypeID
),
 CONSTRAINT IX_ExtRecordType UNIQUE 
(
	ExtRecordType
)
);
/****** Object:  Table SobekCM_Icon    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Icon(
	IconID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Icon_Name varchar(255) NOT NULL,
	Icon_URL varchar(255) NOT NULL,
	Link varchar(255) NULL,
	Height integer NOT NULL,
	Title varchar(255) NULL,
 CONSTRAINT PK_SobekCM_Icon PRIMARY KEY 
(
	IconID
)
);
/****** Object:  Table SobekCM_IP_Restriction_Range    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_IP_Restriction_Range(
	IP_RangeID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Title varchar(150) NOT NULL,
	Notes varchar(2000) NOT NULL,
	Not_Valid_Statement text NOT NULL,
	Deleted boolean NOT NULL,
 CONSTRAINT PK_SobekCM_IP_Restriction_Range PRIMARY KEY 
(
	IP_RangeID
)
);
/****** Object:  Table SobekCM_IP_Restriction_Single    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_IP_Restriction_Single(
	IP_SingleID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	IP_RangeID integer NOT NULL,
	StartIP char(15) NOT NULL,
	EndIP char(15) NULL,
	Notes varchar(100) NULL,
 CONSTRAINT PK_SobekCM_IP_Restriction_Single PRIMARY KEY 
(
	IP_SingleID
)
);
/****** Object:  Table SobekCM_Item_Aggregation    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item_Aggregation(
	AggregationID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Code varchar(20) NOT NULL,
	Name varchar(255) NOT NULL,
	ShortName varchar(100) NULL,
	Description varchar(1000) NULL,
	ThematicHeadingID integer NOT NULL,
	Type varchar(50) NULL,
	isActive boolean NOT NULL,
	Hidden boolean NOT NULL,
	DisplayOptions varchar(10) NOT NULL,
	Map_Search smallint NOT NULL,
	Map_Display smallint NOT NULL,
	OAI_Flag boolean NOT NULL,
	OAI_Metadata varchar(2000) NULL,
	ContactEmail varchar(255) NOT NULL,
	HasNewItems boolean NOT NULL,
	DefaultInterface varchar(50) NOT NULL,
	TEMP_OldID integer NULL,
	TEMP_OldType varchar(2) NULL,
	Items_Can_Be_Described smallint NOT NULL,
	LastItemAdded date NULL,
	External_Link varchar(255) NULL,
	DateAdded timestamp NOT NULL,
	Can_Browse_Items boolean NOT NULL,
	Include_In_Collection_Facet boolean NOT NULL,
	Current_Item_Count integer NOT NULL,
	Current_Title_Count integer NOT NULL,
	Deleted boolean NOT NULL,
	DeleteDate date NULL,
	LanguageVariants varchar(500) NOT NULL,
	GroupResults boolean NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Aggregation PRIMARY KEY 
(
	AggregationID
)
);
/****** Object:  Table SobekCM_Item_Aggregation_Alias    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item_Aggregation_Alias(
	AggregationAliasID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	AggregationAlias varchar(50) NOT NULL,
	AggregationID integer NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Aggregation_Alias PRIMARY KEY 
(
	AggregationAliasID
)
);
/****** Object:  Table SobekCM_Item_Aggregation_Default_Result_Fields    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item_Aggregation_Default_Result_Fields(
	ItemAggregationResultTypeID integer NOT NULL,
	MetadataTypeID smallint NOT NULL,
	OverrideDisplayTerm varchar(255) NULL,
	DisplayOrder integer NOT NULL,
	DisplayOptions varchar(255) NULL,
 CONSTRAINT PK_SobekCM_Item_Aggregation_Default_Result_Fields PRIMARY KEY 
(
	ItemAggregationResultTypeID,
	MetadataTypeID
)
);
/****** Object:  Table SobekCM_Item_Aggregation_Facets    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item_Aggregation_Facets(
	AggregationID integer NOT NULL,
	MetadataTypeID smallint NOT NULL,
	OverrideFacetTerm varchar(100) NULL,
	FacetOrder integer NOT NULL,
	FacetOptions varchar(2000) NULL,
 CONSTRAINT PK_SobekCM_Item_Aggregation_Facets PRIMARY KEY 
(
	AggregationID,
	MetadataTypeID
)
);
/****** Object:  Table SobekCM_Item_Aggregation_Hierarchy    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item_Aggregation_Hierarchy(
	ParentID integer NOT NULL,
	ChildID integer NOT NULL,
	Search_Parent_Only boolean NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Aggregation_Hierarchy PRIMARY KEY 
(
	ParentID,
	ChildID
)
);
/****** Object:  Table SobekCM_Item_Aggregation_Milestones    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item_Aggregation_Milestones(
	AggregationMilestoneID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	AggregationID integer NOT NULL,
	Milestone text NOT NULL,
	MilestoneDate date NOT NULL,
	MilestoneUser varchar(100) NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Aggregation_Milestones PRIMARY KEY 
(
	AggregationMilestoneID
)
);
/****** Object:  Table SobekCM_Item_Aggregation_Result_Fields    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item_Aggregation_Result_Fields(
	ItemAggregationResultID integer NOT NULL,
	MetadataTypeID smallint NOT NULL,
	OverrideDisplayTerm varchar(255) NULL,
	DisplayOrder integer NOT NULL,
	DisplayOptions varchar(2000) NULL,
 CONSTRAINT PK_SobekCM_Item_Aggregation_Result_Fields PRIMARY KEY 
(
	ItemAggregationResultID,
	MetadataTypeID
)
);
/****** Object:  Table SobekCM_Item_Aggregation_Result_Types    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item_Aggregation_Result_Types(
	ItemAggregationResultTypeID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ResultType varchar(50) NOT NULL,
	DefaultOrder integer NOT NULL,
	DefaultView boolean NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Aggregation_Result_Types PRIMARY KEY 
(
	ItemAggregationResultTypeID
),
 CONSTRAINT SobekCM_Item_Aggregation_Result_Types_Unique UNIQUE 
(
	ResultType
)
);
/****** Object:  Table SobekCM_Item_Aggregation_Result_Views    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item_Aggregation_Result_Views(
	ItemAggregationResultID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	AggregationID integer NOT NULL,
	ItemAggregationResultTypeID integer NOT NULL,
	DefaultView boolean NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Aggregation_Result_Views PRIMARY KEY 
(
	ItemAggregationResultID
)
);
/****** Object:  Table SobekCM_Item_Aggregation_Settings    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item_Aggregation_Settings(
	AggregationSettingID bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
	AggregationID integer NOT NULL,
	Setting_Key varchar(255) NOT NULL,
	Setting_Value text NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Aggregation_Settings PRIMARY KEY 
(
	AggregationSettingID
)
);
/****** Object:  Table SobekCM_Item_Aggregation_Statistics    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item_Aggregation_Statistics(
	AggregationStatsID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	AggregationID integer NOT NULL,
	Year smallint NOT NULL,
	Month smallint NOT NULL,
	Hits integer NOT NULL,
	Sessions integer NOT NULL,
	Home_Page_Views integer NOT NULL,
	Browse_Views integer NOT NULL,
	Advanced_Search_Views integer NOT NULL,
	Search_Results_Views integer NOT NULL,
	Title_Hits integer NULL,
	Item_Hits integer NULL,
	Item_JPEG_Views integer NULL,
	Item_Zoomable_Views integer NULL,
	Item_Citation_Views integer NULL,
	Item_Thumbnail_Views integer NULL,
	Item_Text_Search_Views integer NULL,
	Item_Flash_Views integer NULL,
	Item_Google_Map_Views integer NULL,
	Item_Download_Views integer NULL,
	Item_Static_Views integer NULL,
	Title_Count integer NULL,
	Item_Count integer NULL,
 CONSTRAINT PK_SobekCM_Item_Aggregation_Statistics PRIMARY KEY 
(
	AggregationStatsID
)
);
/****** Object:  Table SobekCM_Item_Alias    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item_Alias(
	ItemAliasID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Alias varchar(50) NOT NULL,
	ItemID integer NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Alias PRIMARY KEY 
(
	ItemAliasID
)
);
/****** Object:  Table SobekCM_Item_Error_Log    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item_Error_Log(
	ItemErrorID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	BibID varchar(50) NOT NULL,
	VID varchar(5) NOT NULL,
	ErrorDescription varchar(1000) NOT NULL,
	date timestamp NOT NULL,
	METS_Type varchar(20) NULL,
	ClearedBy varchar(100) NULL,
	ClearedDate timestamp NULL,
 CONSTRAINT PK_SobekCM_Item_Error_Log PRIMARY KEY 
(
	ItemErrorID
)
);
/****** Object:  Table SobekCM_Item_Footprint    Script Date: 7/25/2026 6:52:38 PM ******/
CREATE TABLE SobekCM_Item_Footprint(
	ItemGeoID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ItemID integer NOT NULL,
	Point_Latitude double precision NULL,
	Point_Longitude double precision NULL,
	Rect_Latitude_A double precision NULL,
	Rect_Longitude_A double precision NULL,
	Rect_Latitude_B double precision NULL,
	Rect_Longitude_B double precision NULL,
	Segment_KML text NULL,
 CONSTRAINT PK_SobekCM_Item_Footprint PRIMARY KEY 
(
	ItemGeoID
)
);
/****** Object:  Table SobekCM_Item_GeoRegion_Link    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_GeoRegion_Link(
	ItemID integer NOT NULL,
	RegionID integer NOT NULL,
 CONSTRAINT PK_GEMS_BibGeoLink PRIMARY KEY 
(
	ItemID,
	RegionID
)
);
/****** Object:  Table SobekCM_Item_Group    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_Group(
	GroupID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	GroupTitle varchar(1000) NULL,
	BibID varchar(10) NOT NULL,
	Deleted boolean NOT NULL,
	Type varchar(50) NOT NULL,
	SortTitle varchar(1000) NOT NULL,
	ItemCount integer NOT NULL,
	SuppressEndeca boolean NOT NULL,
	File_Root varchar(100) NOT NULL,
	GroupCreateDate timestamp NOT NULL,
	File_Location varchar(100) NULL,
	OCLC varchar(13) NOT NULL,
	ALEPH varchar(9) NOT NULL,
	OCLC_Number bigint NOT NULL,
	ALEPH_Number integer NOT NULL,
	GroupThumbnail varchar(500) NULL,
	Internal_Comments varchar(1000) NULL,
	Bib_Source varchar(255) NULL,
	TEMP_ReceivingID integer NOT NULL,
	Track_By_Month boolean NOT NULL,
	Large_Format boolean NOT NULL,
	Never_Overlay_Record boolean NOT NULL,
	Include_In_MarcXML_Prod_Feed boolean NOT NULL,
	Include_In_MarcXML_Test_Feed boolean NOT NULL,
	Suppress_OAI boolean NOT NULL,
	Primary_Identifier_Type varchar(50) NULL,
	Primary_Identifier varchar(100) NULL,
	HasGroupMetadata boolean NOT NULL,
	CustomThumbnail varchar(255) NULL,
	ThumbnailType smallint NOT NULL,
	FlagByte smallint NOT NULL,
	LastFourInt smallint NULL,
 CONSTRAINT PK_SobekCM_Item_Group PRIMARY KEY 
(
	GroupID
)
);
/****** Object:  Table SobekCM_Item_Group_External_Record    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_Group_External_Record(
	ExtRecordLinkID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	GroupID integer NOT NULL,
	ExtRecordTypeID integer NOT NULL,
	ExtRecordValue varchar(50) NOT NULL,
 CONSTRAINT PK_Bib_External_Record_Type_Link PRIMARY KEY 
(
	ExtRecordLinkID
)
);
/****** Object:  Table SobekCM_Item_Group_OAI    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_Group_OAI(
	GroupID integer NOT NULL,
	OAI_Data text NOT NULL,
	Locked boolean NOT NULL,
	OAI_Date date NULL,
	Data_Code varchar(20) NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Group_OAI PRIMARY KEY 
(
	GroupID
)
);
/****** Object:  Table SobekCM_Item_Group_Relationship    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_Group_Relationship(
	GroupA integer NOT NULL,
	GroupB integer NOT NULL,
	Relationship_A_to_B varchar(100) NOT NULL,
	Relationship_B_to_A varchar(100) NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Group_Relationship PRIMARY KEY 
(
	GroupA,
	GroupB
)
);
/****** Object:  Table SobekCM_Item_Group_Statistics    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_Group_Statistics(
	ItemGroupStatsID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	GroupID integer NOT NULL,
	Year smallint NOT NULL,
	Month smallint NOT NULL,
	Hits integer NULL,
	Sessions integer NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Group_Statistics PRIMARY KEY 
(
	ItemGroupStatsID
)
);
/****** Object:  Table SobekCM_Item_Group_Viewer_Types    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_Group_Viewer_Types(
	ItemGroupViewTypeID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ViewType varchar(50) NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Group_Viewer_Types PRIMARY KEY 
(
	ItemGroupViewTypeID
)
);
/****** Object:  Table SobekCM_Item_Group_Viewers    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_Group_Viewers(
	ItemGroupViewID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	GroupID integer NOT NULL,
	ItemGroupViewTypeID integer NOT NULL,
	Attribute varchar(250) NOT NULL,
	Label varchar(50) NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Group_Viewers PRIMARY KEY 
(
	ItemGroupViewID
)
);
/****** Object:  Table SobekCM_Item_Group_Web_Skin_Link    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_Group_Web_Skin_Link(
	WebSkinID integer NOT NULL,
	GroupID integer NOT NULL,
	Sequence integer NOT NULL,
 CONSTRAINT PK_Item_Group_Web_Skin_Link PRIMARY KEY 
(
	WebSkinID,
	GroupID
)
);
/****** Object:  Table SobekCM_Item_Icons    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_Icons(
	ItemID integer NOT NULL,
	IconID integer NOT NULL,
	Sequence integer NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Icons PRIMARY KEY 
(
	ItemID,
	IconID
)
);
/****** Object:  Table SobekCM_Item_OAI    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_OAI(
	ItemID integer NOT NULL,
	OAI_Data text NOT NULL,
	Locked boolean NOT NULL,
	OAI_Date date NULL,
	Data_Code varchar(20) NOT NULL,
 CONSTRAINT PK_SobekCM_Item_OAI PRIMARY KEY 
(
	ItemID,
	Data_Code
)
);
/****** Object:  Table SobekCM_Item_Settings    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_Settings(
	ItemSettingID bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
	ItemID integer NOT NULL,
	Setting_Key varchar(255) NOT NULL,
	Setting_Value text NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Settings PRIMARY KEY 
(
	ItemSettingID
)
);
/****** Object:  Table SobekCM_Item_Statistics    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_Statistics(
	ItemStatsID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ItemID integer NOT NULL,
	Year smallint NOT NULL,
	Month smallint NOT NULL,
	Hits integer NOT NULL,
	Sessions integer NOT NULL,
	JPEG_Views integer NOT NULL,
	Zoomable_Views integer NOT NULL,
	Citation_Views integer NOT NULL,
	Thumbnail_Views integer NOT NULL,
	Text_Search_Views integer NOT NULL,
	Flash_Views integer NOT NULL,
	Google_Map_Views integer NOT NULL,
	Download_Views integer NOT NULL,
	Static_Views integer NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Statistics PRIMARY KEY 
(
	ItemStatsID
)
);
/****** Object:  Table SobekCM_Item_Viewer_Types    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_Viewer_Types(
	ItemViewTypeID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ViewType varchar(50) NOT NULL,
	"Order" integer NOT NULL,
	DefaultView boolean NOT NULL,
	MenuOrder double precision NOT NULL,
 CONSTRAINT PK_SobekCM_Item_Viewer_Types PRIMARY KEY 
(
	ItemViewTypeID
),
 CONSTRAINT SobekCM_Item_Viewer_Types_Viewer_Unique UNIQUE 
(
	ViewType
)
);
/****** Object:  Table SobekCM_Item_Viewers    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Item_Viewers(
	ItemViewID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ItemID integer NOT NULL,
	ItemViewTypeID integer NOT NULL,
	Attribute varchar(250) NOT NULL,
	Label varchar(50) NOT NULL,
	OrderOverride integer NULL,
	Exclude boolean NOT NULL,
	MenuOrder double precision NULL,
 CONSTRAINT PK_SobekCM_Item_Viewers PRIMARY KEY 
(
	ItemViewID
)
);
/****** Object:  Table SobekCM_Metadata_Translation    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Metadata_Translation(
	TranslationID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	English varchar(50) NOT NULL,
	French varchar(50) NOT NULL,
	Spanish varchar(50) NOT NULL,
 CONSTRAINT PK_SobekCM_Metadata_Translation PRIMARY KEY 
(
	TranslationID
)
);
/****** Object:  Table SobekCM_Metadata_Types    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Metadata_Types(
	MetadataTypeID smallint GENERATED ALWAYS AS IDENTITY NOT NULL,
	MetadataName varchar(100) NOT NULL,
	SobekCode char(2) NULL,
	SolrCode varchar(100) NULL,
	DisplayTerm varchar(100) NULL,
	FacetTerm varchar(100) NULL,
	CustomField boolean NOT NULL,
	canFacetBrowse boolean NOT NULL,
	DefaultAdvancedSearch boolean NOT NULL,
	LegacySolrCode varchar(100) NULL,
	SolrCode_Facets varchar(100) NULL,
	SolrCode_Display varchar(100) NULL,
 CONSTRAINT PK_SobekCM_Metadata_Types PRIMARY KEY 
(
	MetadataTypeID
)
);
/****** Object:  Table SobekCM_Mime_Types    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Mime_Types(
	MimeTypeID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Extension varchar(20) NOT NULL,
	MimeType varchar(100) NOT NULL,
	isBlocked boolean NOT NULL,
	shouldForward boolean NOT NULL,
 CONSTRAINT PK_SobekCM_Mime_Types PRIMARY KEY 
(
	MimeTypeID
)
);
/****** Object:  Table SobekCM_OpenPublishing_Theme    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_OpenPublishing_Theme(
	ThemeID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ThemeName varchar(150) NOT NULL,
	Location varchar(255) NOT NULL,
	Author varchar(50) NULL,
	Description varchar(1000) NULL,
	Image varchar(255) NULL,
	AvailableForSelection boolean NOT NULL,
	"Default" boolean NOT NULL,
 CONSTRAINT PK_OpenPublishing_Theme PRIMARY KEY 
(
	ThemeID
)
);
/****** Object:  Table SobekCM_Portal_Item_Aggregation_Link    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Portal_Item_Aggregation_Link(
	PortalID integer NOT NULL,
	AggregationID integer NOT NULL,
	isDefault boolean NOT NULL,
 CONSTRAINT PK_SobekCM_Portal_Item_Aggregation_Link PRIMARY KEY 
(
	PortalID,
	AggregationID
)
);
/****** Object:  Table SobekCM_Portal_URL    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Portal_URL(
	PortalID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Base_URL varchar(150) NOT NULL,
	isActive boolean NOT NULL,
	isDefault boolean NOT NULL,
	Abbreviation varchar(10) NOT NULL,
	Name varchar(250) NOT NULL,
	Base_PURL varchar(150) NULL,
 CONSTRAINT PK_SobekCM_Portal_URL PRIMARY KEY 
(
	PortalID
)
);
/****** Object:  Table SobekCM_Portal_URL_Statistics    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Portal_URL_Statistics(
	PortalStatsID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	PortalID integer NOT NULL,
	Year smallint NOT NULL,
	Month smallint NOT NULL,
	Hits integer NOT NULL,
 CONSTRAINT PK_SobekCM_Portal_URL_Statistics PRIMARY KEY 
(
	PortalStatsID
)
);
/****** Object:  Table SobekCM_Portal_Web_Skin_Link    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Portal_Web_Skin_Link(
	PortalID integer NOT NULL,
	WebSkinID integer NOT NULL,
	isDefault boolean NOT NULL,
 CONSTRAINT PK_SobekCM_Portal_Web_Skin_Link PRIMARY KEY 
(
	PortalID,
	WebSkinID
)
);
/****** Object:  Table SobekCM_Project    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Project(
	ProjectID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ProjectCode varchar(20) NULL,
	ProjectName varchar(100) NOT NULL,
	ProjectManager varchar(100) NULL,
	GrantID varchar(20) NULL,
	GrantName varchar(250) NULL,
	StartDate date NULL,
	EndDate date NULL,
	isActive boolean NULL,
	Description varchar(1000) NULL,
	Specifications varchar(1000) NULL,
	Priority varchar(100) NULL,
	QC_Profile varchar(100) NULL,
	TargetItemCount integer NULL,
	TargetPageCount integer NULL,
	Comments varchar(1000) NULL,
	CopyrightPermissions varchar(1000) NULL,
 CONSTRAINT PK_SobekCM_Project PRIMARY KEY 
(
	ProjectID
)
);
/****** Object:  Table SobekCM_Project_Aggregation_Link    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Project_Aggregation_Link(
	ProjectID integer NOT NULL,
	AggregationID integer NOT NULL,
 CONSTRAINT PK_SobekCM_Project_Aggregation_Link PRIMARY KEY 
(
	ProjectID,
	AggregationID
)
);
/****** Object:  Table SobekCM_Project_DefaultMetadata_Link    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Project_DefaultMetadata_Link(
	ProjectID integer NOT NULL,
	DefaultMetadataID integer NOT NULL,
 CONSTRAINT PK_SobekCM_Project_DefaultMetadata_Link PRIMARY KEY 
(
	ProjectID,
	DefaultMetadataID
)
);
/****** Object:  Table SobekCM_Project_Item_Link    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Project_Item_Link(
	ProjectID integer NOT NULL,
	ItemID integer NOT NULL,
 CONSTRAINT PK_SobekCM_Project_Item_Link PRIMARY KEY 
(
	ProjectID,
	ItemID
)
);
/****** Object:  Table SobekCM_Project_Template_Link    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Project_Template_Link(
	ProjectID integer NOT NULL,
	TemplateID integer NOT NULL,
 CONSTRAINT PK_SobekCM_Project_Template_Link PRIMARY KEY 
(
	ProjectID,
	TemplateID
)
);
/****** Object:  Table SobekCM_QC_Errors    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_QC_Errors(
	ErrorID bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
	ItemID integer NOT NULL,
	FileName text NOT NULL,
	ErrorCode char(10) NOT NULL,
	isVolumeError boolean NULL,
	Description text NULL,
 CONSTRAINT PK_Table_1 PRIMARY KEY 
(
	ErrorID
)
);
/****** Object:  Table SobekCM_QC_Errors_History    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_QC_Errors_History(
	ErrorID bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
	ItemID integer NOT NULL,
	ErrorCode char(10) NOT NULL,
	isVolumeError boolean NULL,
	Count integer NOT NULL,
 CONSTRAINT PK_SobekCM_QC_Errors_History PRIMARY KEY 
(
	ErrorID
)
);
/****** Object:  Table SobekCM_Search_Stop_Words    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Search_Stop_Words(
	StopWordId integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	StopWord varchar(50) NOT NULL,
 CONSTRAINT PK_SobekCM_Search_Stop_Words PRIMARY KEY 
(
	StopWordId
)
);
/****** Object:  Table SobekCM_Settings    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Settings(
	Setting_Key varchar(255) NOT NULL,
	Setting_Value text NOT NULL,
	TabPage varchar(75) NULL,
	Heading varchar(75) NULL,
	Hidden boolean NOT NULL,
	Reserved smallint NOT NULL,
	Help text NULL,
	Options text NULL,
	SettingID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Dimensions varchar(100) NULL,
 CONSTRAINT PK_SobekCM_Settings PRIMARY KEY 
(
	Setting_Key
)
);
/****** Object:  Table SobekCM_Source_Line    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Source_Line(
	SourceLineID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	SourceLine varchar(500) NULL,
	ItemID integer NULL,
	PageSequence integer NULL,
 CONSTRAINT PK_SobekCM_Source_Line PRIMARY KEY 
(
	SourceLineID
)
);
/****** Object:  Table SobekCM_Statistics    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Statistics(
	StatisticsID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Year smallint NOT NULL,
	Month smallint NOT NULL,
	Hits integer NOT NULL,
	Sessions integer NOT NULL,
	Robot_Hits integer NOT NULL,
	XML_Hits integer NOT NULL,
	OAI_Hits integer NOT NULL,
	JSON_Hits integer NOT NULL,
	Aggregate_Statistics_Complete boolean NOT NULL,
 CONSTRAINT PK_SobekCM_Statistics PRIMARY KEY 
(
	StatisticsID
)
);
/****** Object:  Table SobekCM_Thematic_Heading    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Thematic_Heading(
	ThematicHeadingID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ThemeOrder integer NOT NULL,
	ThemeName varchar(100) NOT NULL,
 CONSTRAINT PK_SobekCM_Thematic_Heading PRIMARY KEY 
(
	ThematicHeadingID
)
);
/****** Object:  Table SobekCM_Web_Skin    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_Web_Skin(
	WebSkinID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	WebSkinCode varchar(20) NOT NULL,
	OverrideHeaderFooter boolean NULL,
	OverrideBanner boolean NULL,
	BannerLink varchar(255) NULL,
	BaseWebSkin varchar(20) NULL,
	Notes varchar(250) NULL,
	OldInterfaceID integer NULL,
	Build_On_Launch boolean NOT NULL,
	SuppressTopNavigation boolean NOT NULL,
 CONSTRAINT PK_SobekCM_WebSkin PRIMARY KEY 
(
	WebSkinID
)
);
/****** Object:  Table SobekCM_WebContent    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_WebContent(
	WebContentID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	Level1 varchar(100) NOT NULL,
	Level2 varchar(100) NULL,
	Level3 varchar(100) NULL,
	Level4 varchar(100) NULL,
	Level5 varchar(100) NULL,
	Level6 varchar(100) NULL,
	Level7 varchar(100) NULL,
	Level8 varchar(100) NULL,
	Deleted boolean NOT NULL,
	Title varchar(255) NULL,
	Summary varchar(1000) NULL,
	Redirect varchar(500) NULL,
	Locked boolean NOT NULL,
 CONSTRAINT PK_SobekCM_WebContent PRIMARY KEY 
(
	WebContentID
)
);
/****** Object:  Table SobekCM_WebContent_Milestones    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_WebContent_Milestones(
	WebContentMilestoneID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	WebContentID integer NOT NULL,
	Milestone text NOT NULL,
	MilestoneDate timestamp NOT NULL,
	MilestoneUser varchar(100) NOT NULL,
 CONSTRAINT PK_SobekCM_WebContent_Milestones PRIMARY KEY 
(
	WebContentMilestoneID
)
);
/****** Object:  Table SobekCM_WebContent_Statistics    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE SobekCM_WebContent_Statistics(
	Level1 varchar(100) NOT NULL,
	Level2 varchar(100) NULL,
	Level3 varchar(100) NULL,
	Level4 varchar(100) NULL,
	Level5 varchar(100) NULL,
	Level6 varchar(100) NULL,
	Level7 varchar(100) NULL,
	Level8 varchar(100) NULL,
	Year smallint NOT NULL,
	Month smallint NOT NULL,
	Hits integer NOT NULL,
	Hits_Complete integer NOT NULL,
	WebContentID integer NULL
);
/****** Object:  Table Tracking_Disposition_Type    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE Tracking_Disposition_Type(
	DispositionID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	DispositionFuture varchar(100) NOT NULL,
	DispositionPast varchar(100) NOT NULL,
	DispositionNotes varchar(1000) NOT NULL,
 CONSTRAINT PK_Tracking_Disposition_Type PRIMARY KEY 
(
	DispositionID
)
);
/****** Object:  Table Tracking_Item    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE Tracking_Item(
	ItemID integer NOT NULL,
	Original_AccessCode varchar(25) NULL,
	EmbargoEnd date NULL,
	Original_EmbargoEnd date NULL,
	UMI varchar(20) NULL,
 CONSTRAINT PK_Tracking_Item PRIMARY KEY 
(
	ItemID
)
);
/****** Object:  Table Tracking_Progress    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE Tracking_Progress(
	ProgressID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ItemID integer NOT NULL,
	WorkFlowID integer NOT NULL,
	DateCompleted timestamp NULL,
	WorkPerformedBy varchar(255) NULL,
	WorkingFilePath varchar(255) NULL,
	ProgressNote varchar(1000) NULL,
	DateStarted timestamp NULL,
	Duration integer NOT NULL,
	RelatedEquipment varchar(255) NULL,
	Start_Event_Number integer NULL,
	End_Event_Number integer NULL,
	Start_And_End_Event_Number integer NULL,
	WorkPerformedById integer NULL,
 CONSTRAINT PK_Progress PRIMARY KEY 
(
	ProgressID
)
);
/****** Object:  Table Tracking_ScanningEquipment    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE Tracking_ScanningEquipment(
	EquipmentID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	ScanningEquipment varchar(255) NOT NULL,
	Notes text NULL,
	Location varchar(255) NULL,
	EquipmentType varchar(255) NULL,
	isActive boolean NOT NULL,
	ProductionStartDate date NULL,
	ProductionEndDate date NULL,
 CONSTRAINT PK_Tracking_ScanningEquipment PRIMARY KEY 
(
	EquipmentID
)
);
/****** Object:  Table Tracking_WorkFlow    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE TABLE Tracking_WorkFlow(
	WorkFlowID integer GENERATED ALWAYS AS IDENTITY NOT NULL,
	WorkFlowName varchar(100) NOT NULL,
	WorkFlowNotes varchar(1000) NULL,
	Start_Event_Number integer NULL,
	End_Event_Number integer NULL,
	Start_And_End_Event_Number integer NULL,
	Start_Event_Desc varchar(100) NULL,
	End_Event_Desc varchar(100) NULL,
 CONSTRAINT PK_WorkFlow PRIMARY KEY 
(
	WorkFlowID
)
);
/****** Object:  Index IX_Archived_File_Copy_SnapshotID    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_Archived_File_Copy_SnapshotID ON Archive_Item_Archived_File_Copy
(
	SnapshotID
);
/****** Object:  Index IX_Archived_File_Snapshot_FileID    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_Archived_File_Snapshot_FileID ON Archive_Item_Archived_File_Snapshot
(
	ArchivedFileID
);
/****** Object:  Index IX_UFDC_GeoRegion_GeoAuthCode    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE UNIQUE INDEX IX_UFDC_GeoRegion_GeoAuthCode ON Auth_GeoRegion
(
	GeoAuthCode
);
/****** Object:  Index IX_UFDC_GeoRegion_RegionTypeID    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_UFDC_GeoRegion_RegionTypeID ON Auth_GeoRegion
(
	RegionTypeID
);
/****** Object:  Index IX_mySobek_User_ExternalLogin    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE UNIQUE INDEX IX_mySobek_User_ExternalLogin ON mySobek_User
(
	ExternalProviderCode,
	ExternalSubjectId
)
WHERE (ExternalProviderCode IS NOT NULL AND ExternalSubjectId IS NOT NULL);
/****** Object:  Index IX_User_Item_UserFolderID    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_User_Item_UserFolderID ON mySobek_User_Item
(
	UserFolderID
);
/****** Object:  Index IX_Email_Log    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_Email_Log ON SobekCM_Email_Log
(
	Sent_Date,
	UserID
);
/****** Object:  Index IX_SobekCM_Icon_Name    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Icon_Name ON SobekCM_Icon
(
	Icon_Name
);
/****** Object:  Index IX_Item_Create_Year_Month    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_Item_Create_Year_Month ON SobekCM_Item
(
	CreateYear,
	CreateMonth
);
/****** Object:  Index IX_Item_CreateDate_Index    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_Item_CreateDate_Index ON SobekCM_Item
(
	CreateDate
);
/****** Object:  Index IX_SobekCM_Item_Additional_Information    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Additional_Information ON SobekCM_Item
(
	VID
)
INCLUDE(PageCount,Country,State,County,City,MainLatitude,MainLongitude,Format,Donor,Publisher,Spatial_KML,GroupID);
/****** Object:  Index IX_SobekCM_Item_Additional_Information_2    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Additional_Information_2 ON SobekCM_Item
(
	VID,
	GroupID
)
INCLUDE(PageCount,Country,State,County,City,MainLatitude,MainLongitude,Format,Donor,Publisher,Spatial_KML);
/****** Object:  Index IX_SobekCM_Item_CreateDate_Restriction_Mask    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_CreateDate_Restriction_Mask ON SobekCM_Item
(
	CreateDate,
	IP_Restriction_Mask,
	Dark
)
INCLUDE(ItemID,GroupID);
/****** Object:  Index IX_SobekCM_Item_Deleted_IP_Restriction_Mask    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Deleted_IP_Restriction_Mask ON SobekCM_Item
(
	Deleted,
	IP_Restriction_Mask,
	IncludeInAll,
	Dark
)
INCLUDE(ItemID,SortDate,GroupID,VID,Title);
/****** Object:  Index IX_SobekCM_Item_Deleted_IP_Restriction_Mask2    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Deleted_IP_Restriction_Mask2 ON SobekCM_Item
(
	Deleted,
	IncludeInAll,
	IP_Restriction_Mask
)
INCLUDE(ItemID,SortDate,GroupID);
/****** Object:  Index IX_SobekCM_Item_Deleted_MileStone    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Deleted_MileStone ON SobekCM_Item
(
	Deleted,
	Milestone_OnlineComplete
)
INCLUDE(ItemID,PageCount,FileCount,GroupID);
/****** Object:  Index IX_SobekCM_Item_GroupID    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_GroupID ON SobekCM_Item
(
	GroupID
);
/****** Object:  Index IX_SobekCM_Item_MadePublicDate    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_MadePublicDate ON SobekCM_Item
(
	MadePublicDate
)
INCLUDE(ItemID);
/****** Object:  Index IX_SobekCM_Item_Aggregation_AggregationID_Include    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Aggregation_AggregationID_Include ON SobekCM_Item_Aggregation
(
	AggregationID,
	isActive,
	Hidden,
	Include_In_Collection_Facet
)
INCLUDE(Code,Name,ShortName,Current_Item_Count,Current_Title_Count);
/****** Object:  Index IX_SobekCM_Item_Aggregation_AggregationID_Include2    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Aggregation_AggregationID_Include2 ON SobekCM_Item_Aggregation
(
	isActive,
	Hidden,
	Include_In_Collection_Facet
)
INCLUDE(Code,Name,ShortName,Current_Item_Count,Current_Title_Count);
/****** Object:  Index IX_SobekCM_Item_Aggregation_Code    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Aggregation_Code ON SobekCM_Item_Aggregation
(
	Code
)
INCLUDE(AggregationID);
/****** Object:  Index IX_SobekCM_Item_Aggregation_Item_Link    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Aggregation_Item_Link ON SobekCM_Item_Aggregation_Item_Link
(
	AggregationID
)
INCLUDE(ItemID);
/****** Object:  Index IX_Item_Footprint_Point_Coordinates    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_Item_Footprint_Point_Coordinates ON SobekCM_Item_Footprint
(
	Point_Latitude,
	Point_Longitude
)
INCLUDE(ItemID);
/****** Object:  Index IX_SobekCM_Item_Group_BibID_Index    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Group_BibID_Index ON SobekCM_Item_Group
(
	BibID
);
/****** Object:  Index IX_SobekCM_Item_Group_Deleted    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Group_Deleted ON SobekCM_Item_Group
(
	Deleted
)
INCLUDE(GroupID,BibID);
/****** Object:  Index IX_SobekCM_Item_Group_OAI_Index    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Group_OAI_Index ON SobekCM_Item_Group
(
	Suppress_OAI
)
INCLUDE(GroupID,BibID);
/****** Object:  Index IX_SobekCM_Item_Group_OAI_Date    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Group_OAI_Date ON SobekCM_Item_Group_OAI
(
	OAI_Date
)
INCLUDE(GroupID);
/****** Object:  Index IX_SobekCM_Item_Group_Statistcs    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Group_Statistcs ON SobekCM_Item_Group_Statistics
(
	GroupID
)
INCLUDE(Year,Month,Hits);
/****** Object:  Index IX_SobekCM_Item_Group_Viewers_GroupID    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Group_Viewers_GroupID ON SobekCM_Item_Group_Viewers
(
	GroupID
);
/****** Object:  Index IX_Item_Group_Web_Skin_Link    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_Item_Group_Web_Skin_Link ON SobekCM_Item_Group_Web_Skin_Link
(
	GroupID
)
INCLUDE(WebSkinID,Sequence);
/****** Object:  Index IX_Item_OAI_Date_Code    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_Item_OAI_Date_Code ON SobekCM_Item_OAI
(
	OAI_Date,
	Data_Code
)
INCLUDE(ItemID);
/****** Object:  Index IX_SobekCM_Item_Stats_ItemID    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Stats_ItemID ON SobekCM_Item_Statistics
(
	ItemID
)
INCLUDE(Year,Month,Hits,JPEG_Views,Zoomable_Views,Citation_Views,Thumbnail_Views,Text_Search_Views,Flash_Views,Google_Map_Views,Download_Views);
/****** Object:  Index IX_SobekCM_Item_Stats_Year_Month    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Stats_Year_Month ON SobekCM_Item_Statistics
(
	Year,
	Month,
	ItemID
)
INCLUDE(Hits,Sessions);
/****** Object:  Index IX_SobekCM_Item_Viewer_Types_ViewType    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Viewer_Types_ViewType ON SobekCM_Item_Viewer_Types
(
	ViewType
)
INCLUDE(ItemViewTypeID);
/****** Object:  Index IX_SobekCM_Item_Viewers_ItemID    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Item_Viewers_ItemID ON SobekCM_Item_Viewers
(
	ItemID
);
/****** Object:  Index IX_SobekCM_Metadata_Types    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_Metadata_Types ON SobekCM_Metadata_Types
(
	MetadataName,
	CustomField
)
INCLUDE(MetadataTypeID);
/****** Object:  Index IX_SobekCM_WebSkin_Code    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_WebSkin_Code ON SobekCM_Web_Skin
(
	WebSkinCode
);
/****** Object:  Index IX_SobekCM_WebContent_Levels    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_WebContent_Levels ON SobekCM_WebContent
(
	Level1,
	Level2,
	Level3,
	Level4,
	Level5,
	Level6,
	Level7,
	Level8
)
INCLUDE(WebContentID,Deleted,Title,Summary);
/****** Object:  Index IX_SobekCM_WebContent_Milestones_Date_ID    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_WebContent_Milestones_Date_ID ON SobekCM_WebContent_Milestones
(
	WebContentID,
	MilestoneDate
)
INCLUDE(MilestoneUser);
/****** Object:  Index IX_SobekCM_WebContent_Stats_ID    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_SobekCM_WebContent_Stats_ID ON SobekCM_WebContent_Statistics
(
	WebContentID
)
INCLUDE(Year,Month,Hits,Hits_Complete);
/****** Object:  Index IX_Tracking_Progress_ItemID    Script Date: 7/25/2026 6:52:39 PM ******/
CREATE INDEX IX_Tracking_Progress_ItemID ON Tracking_Progress
(
	ItemID
);
ALTER TABLE Archive_Item_Archived_File_Copy ALTER COLUMN Status SET DEFAULT 'Stored';
ALTER TABLE Archive_Location ALTER COLUMN IsActive SET DEFAULT 1;
ALTER TABLE mySobek_DefaultMetadata ALTER COLUMN Description SET DEFAULT '';
ALTER TABLE mySobek_Template ALTER COLUMN Description SET DEFAULT '';
ALTER TABLE mySobek_User ALTER COLUMN OrganizationCode SET DEFAULT '';
ALTER TABLE mySobek_User ALTER COLUMN EditTemplate SET DEFAULT 'edit';
ALTER TABLE mySobek_User ALTER COLUMN EditTemplateMarc SET DEFAULT 'editmarc';
ALTER TABLE mySobek_User ALTER COLUMN Receive_Stats_Emails SET DEFAULT 'true';
ALTER TABLE mySobek_User ALTER COLUMN Has_Item_Stats SET DEFAULT 'false';
ALTER TABLE mySobek_User ALTER COLUMN IsSystemAdmin SET DEFAULT 'false';
ALTER TABLE mySobek_User ALTER COLUMN IsPortalAdmin SET DEFAULT 'false';
ALTER TABLE mySobek_User ALTER COLUMN Include_Tracking_Standard_Forms SET DEFAULT 'true';
ALTER TABLE mySobek_User ALTER COLUMN qcProfile SET DEFAULT '';
ALTER TABLE mySobek_User ALTER COLUMN Can_Delete_All_Items SET DEFAULT 'false';
ALTER TABLE mySobek_User ALTER COLUMN ScanningTechnician SET DEFAULT 0;
ALTER TABLE mySobek_User ALTER COLUMN ProcessingTechnician SET DEFAULT 0;
ALTER TABLE mySobek_User ALTER COLUMN IsHostAdmin SET DEFAULT 'false';
ALTER TABLE mySobek_User ALTER COLUMN IsUserAdmin SET DEFAULT 'false';
ALTER TABLE mySobek_User ALTER COLUMN AuthenticationSource SET DEFAULT '';
ALTER TABLE mySobek_User_Bib_Link ALTER COLUMN GroupID SET DEFAULT 1;
ALTER TABLE mySobek_User_Description_Tags ALTER COLUMN ItemID SET DEFAULT 1;
ALTER TABLE mySobek_User_Edit_Aggregation ALTER COLUMN OnHomePage SET DEFAULT 0;
ALTER TABLE mySobek_User_Edit_Aggregation ALTER COLUMN IsCurator SET DEFAULT 0;
ALTER TABLE mySobek_User_Edit_Aggregation ALTER COLUMN IsAdmin SET DEFAULT 'false';
ALTER TABLE mySobek_User_Edit_Aggregation ALTER COLUMN CanEditMetadata SET DEFAULT 'false';
ALTER TABLE mySobek_User_Edit_Aggregation ALTER COLUMN CanEditBehaviors SET DEFAULT 'false';
ALTER TABLE mySobek_User_Edit_Aggregation ALTER COLUMN CanPerformQc SET DEFAULT 'false';
ALTER TABLE mySobek_User_Edit_Aggregation ALTER COLUMN CanUploadFiles SET DEFAULT 'false';
ALTER TABLE mySobek_User_Edit_Aggregation ALTER COLUMN CanChangeVisibility SET DEFAULT 'false';
ALTER TABLE mySobek_User_Edit_Aggregation ALTER COLUMN CanDelete SET DEFAULT 'false';
ALTER TABLE mySobek_User_Editable_Link ALTER COLUMN CanEditMetadata SET DEFAULT 'false';
ALTER TABLE mySobek_User_Editable_Link ALTER COLUMN CanEditBehaviors SET DEFAULT 'false';
ALTER TABLE mySobek_User_Editable_Link ALTER COLUMN CanPerformQc SET DEFAULT 'false';
ALTER TABLE mySobek_User_Editable_Link ALTER COLUMN CanUploadFiles SET DEFAULT 'false';
ALTER TABLE mySobek_User_Editable_Link ALTER COLUMN CanChangeVisibility SET DEFAULT 'false';
ALTER TABLE mySobek_User_Editable_Link ALTER COLUMN CanDelete SET DEFAULT 'false';
ALTER TABLE mySobek_User_Folder ALTER COLUMN FolderDescription SET DEFAULT '';
ALTER TABLE mySobek_User_Group ALTER COLUMN Internal_User SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group ALTER COLUMN IsSystemAdmin SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group ALTER COLUMN IsPortalAdmin SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group ALTER COLUMN Include_Tracking_Standard_Forms SET DEFAULT 'true';
ALTER TABLE mySobek_User_Group ALTER COLUMN autoAssignUsers SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group ALTER COLUMN Can_Delete_All_Items SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group ALTER COLUMN IsSobekDefault SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group ALTER COLUMN IsShibbolethDefault SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group ALTER COLUMN IsLdapDefault SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group ALTER COLUMN IsSpecialGroup SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Edit_Aggregation ALTER COLUMN IsAdmin SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Edit_Aggregation ALTER COLUMN CanEditMetadata SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Edit_Aggregation ALTER COLUMN CanEditBehaviors SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Edit_Aggregation ALTER COLUMN CanPerformQc SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Edit_Aggregation ALTER COLUMN CanUploadFiles SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Edit_Aggregation ALTER COLUMN CanChangeVisibility SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Edit_Aggregation ALTER COLUMN CanDelete SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Editable_Link ALTER COLUMN CanEditMetadata SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Editable_Link ALTER COLUMN CanEditBehaviors SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Editable_Link ALTER COLUMN CanPerformQc SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Editable_Link ALTER COLUMN CanUploadFiles SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Editable_Link ALTER COLUMN CanChangeVisibility SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Editable_Link ALTER COLUMN CanDelete SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Item_Permissions ALTER COLUMN isDefaultPermissions SET DEFAULT 'false';
ALTER TABLE mySobek_User_Group_Link ALTER COLUMN IsGroupAdmin SET DEFAULT 'false';
ALTER TABLE mySobek_User_Item ALTER COLUMN ItemID SET DEFAULT 1;
ALTER TABLE mySobek_User_Item_Link_Relationship ALTER COLUMN Include_In_Results SET DEFAULT 'true';
ALTER TABLE SobekCM_Builder_Incoming_Folders ALTER COLUMN FolderName SET DEFAULT '';
ALTER TABLE SobekCM_Builder_Incoming_Folders ALTER COLUMN BibID_Roots_Restrictions SET DEFAULT '';
ALTER TABLE SobekCM_Builder_Module_Schedule ALTER COLUMN Description SET DEFAULT '';
ALTER TABLE SobekCM_Builder_Module_Set ALTER COLUMN Enabled SET DEFAULT 'true';
ALTER TABLE SobekCM_Database_Version ALTER COLUMN Release_Phase SET DEFAULT '';
ALTER TABLE SobekCM_Email_Log ALTER COLUMN Contact_Us SET DEFAULT 'false';
ALTER TABLE SobekCM_External_Record_Type ALTER COLUMN repeatableTypeFlag SET DEFAULT 0;
ALTER TABLE SobekCM_Icon ALTER COLUMN Height SET DEFAULT 80;
ALTER TABLE SobekCM_IP_Restriction_Range ALTER COLUMN Deleted SET DEFAULT 'false';
ALTER TABLE SobekCM_Item ALTER COLUMN TextSearchable SET DEFAULT 0;
ALTER TABLE SobekCM_Item ALTER COLUMN Locked SET DEFAULT 0;
ALTER TABLE SobekCM_Item ALTER COLUMN FileCount SET DEFAULT -1;
ALTER TABLE SobekCM_Item ALTER COLUMN Format SET DEFAULT '';
ALTER TABLE SobekCM_Item ALTER COLUMN CheckoutRequired SET DEFAULT 'false';
ALTER TABLE SobekCM_Item ALTER COLUMN Spatial_KML_Distance SET DEFAULT 0;
ALTER TABLE SobekCM_Item ALTER COLUMN DiskSize_KB SET DEFAULT 0;
ALTER TABLE SobekCM_Item ALTER COLUMN IP_Restriction_Mask SET DEFAULT 0;
ALTER TABLE SobekCM_Item ALTER COLUMN IncludeInAll SET DEFAULT 1;
ALTER TABLE SobekCM_Item ALTER COLUMN SuppressOAI SET DEFAULT 0;
ALTER TABLE SobekCM_Item ALTER COLUMN CreateYear SET DEFAULT -1;
ALTER TABLE SobekCM_Item ALTER COLUMN CreateMonth SET DEFAULT -1;
ALTER TABLE SobekCM_Item ALTER COLUMN Dark SET DEFAULT 0;
ALTER TABLE SobekCM_Item ALTER COLUMN VolumeID SET DEFAULT -1;
ALTER TABLE SobekCM_Item ALTER COLUMN Last_MileStone SET DEFAULT 0;
ALTER TABLE SobekCM_Item ALTER COLUMN Born_Digital SET DEFAULT 'false';
ALTER TABLE SobekCM_Item ALTER COLUMN Locally_Archived SET DEFAULT 'false';
ALTER TABLE SobekCM_Item ALTER COLUMN Remotely_Archived SET DEFAULT 'false';
ALTER TABLE SobekCM_Item ALTER COLUMN Material_Recd_Date_Estimated SET DEFAULT 'false';
ALTER TABLE SobekCM_Item ALTER COLUMN Left_To_Right SET DEFAULT 'false';
ALTER TABLE SobekCM_Item ALTER COLUMN Disposition_Advice_Notes SET DEFAULT '';
ALTER TABLE SobekCM_Item ALTER COLUMN Disposition_Notes SET DEFAULT '';
ALTER TABLE SobekCM_Item ALTER COLUMN AdditionalWorkNeeded SET DEFAULT 'false';
ALTER TABLE SobekCM_Item ALTER COLUMN ExposeFullTextForHarvesting SET DEFAULT 'true';
ALTER TABLE SobekCM_Item ALTER COLUMN Total_Hits SET DEFAULT 0;
ALTER TABLE SobekCM_Item ALTER COLUMN Total_Sessions SET DEFAULT 0;
ALTER TABLE SobekCM_Item ALTER COLUMN SortTitle SET DEFAULT '';
ALTER TABLE SobekCM_Item ALTER COLUMN TivoliSize_MB SET DEFAULT 0;
ALTER TABLE SobekCM_Item ALTER COLUMN TivoliSize_Calculated SET DEFAULT '1/1/2000';
ALTER TABLE SobekCM_Item ALTER COLUMN metadataProfile SET DEFAULT '';
ALTER TABLE SobekCM_Item ALTER COLUMN SpatialFootprint SET DEFAULT '';
ALTER TABLE SobekCM_Item ALTER COLUMN SpatialFootprintDistance SET DEFAULT 999;
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN ThematicHeadingID SET DEFAULT -1;
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN Hidden SET DEFAULT 0;
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN DisplayOptions SET DEFAULT '';
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN Map_Search SET DEFAULT 0;
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN Map_Display SET DEFAULT 0;
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN HasNewItems SET DEFAULT 0;
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN DefaultInterface SET DEFAULT '';
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN Items_Can_Be_Described SET DEFAULT 1;
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN DateAdded SET DEFAULT '1/1/1900';
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN Can_Browse_Items SET DEFAULT 'true';
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN Include_In_Collection_Facet SET DEFAULT 'true';
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN Current_Item_Count SET DEFAULT 0;
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN Current_Title_Count SET DEFAULT 0;
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN Deleted SET DEFAULT 'false';
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN LanguageVariants SET DEFAULT '';
ALTER TABLE SobekCM_Item_Aggregation ALTER COLUMN GroupResults SET DEFAULT 'false';
ALTER TABLE SobekCM_Item_Aggregation_Hierarchy ALTER COLUMN Search_Parent_Only SET DEFAULT 0;
ALTER TABLE SobekCM_Item_Aggregation_Item_Link ALTER COLUMN impliedLink SET DEFAULT 0;
ALTER TABLE SobekCM_Item_Aggregation_Result_Types ALTER COLUMN DefaultOrder SET DEFAULT 100;
ALTER TABLE SobekCM_Item_Aggregation_Result_Types ALTER COLUMN DefaultView SET DEFAULT 'false';
ALTER TABLE SobekCM_Item_Aggregation_Result_Views ALTER COLUMN DefaultView SET DEFAULT 'false';
ALTER TABLE SobekCM_Item_Group ALTER COLUMN Deleted SET DEFAULT 0;
ALTER TABLE SobekCM_Item_Group ALTER COLUMN SortTitle SET DEFAULT '';
ALTER TABLE SobekCM_Item_Group ALTER COLUMN ItemCount SET DEFAULT 0;
ALTER TABLE SobekCM_Item_Group ALTER COLUMN SuppressEndeca SET DEFAULT 1;
ALTER TABLE SobekCM_Item_Group ALTER COLUMN File_Root SET DEFAULT 'collect/image_files/';
ALTER TABLE SobekCM_Item_Group ALTER COLUMN OCLC SET DEFAULT '';
ALTER TABLE SobekCM_Item_Group ALTER COLUMN ALEPH SET DEFAULT '';
ALTER TABLE SobekCM_Item_Group ALTER COLUMN OCLC_Number SET DEFAULT -1;
ALTER TABLE SobekCM_Item_Group ALTER COLUMN ALEPH_Number SET DEFAULT -1;
ALTER TABLE SobekCM_Item_Group ALTER COLUMN TEMP_ReceivingID SET DEFAULT -1;
ALTER TABLE SobekCM_Item_Group ALTER COLUMN Track_By_Month SET DEFAULT 'false';
ALTER TABLE SobekCM_Item_Group ALTER COLUMN Large_Format SET DEFAULT 'false';
ALTER TABLE SobekCM_Item_Group ALTER COLUMN Never_Overlay_Record SET DEFAULT 'false';
ALTER TABLE SobekCM_Item_Group ALTER COLUMN Include_In_MarcXML_Prod_Feed SET DEFAULT 'true';
ALTER TABLE SobekCM_Item_Group ALTER COLUMN Include_In_MarcXML_Test_Feed SET DEFAULT 'false';
ALTER TABLE SobekCM_Item_Group ALTER COLUMN Suppress_OAI SET DEFAULT 'false';
ALTER TABLE SobekCM_Item_Group ALTER COLUMN HasGroupMetadata SET DEFAULT 'false';
ALTER TABLE SobekCM_Item_Group ALTER COLUMN ThumbnailType SET DEFAULT 0;
ALTER TABLE SobekCM_Item_Group ALTER COLUMN FlagByte SET DEFAULT 0;
ALTER TABLE SobekCM_Item_Group_OAI ALTER COLUMN Locked SET DEFAULT 'false';
ALTER TABLE SobekCM_Item_Group_OAI ALTER COLUMN Data_Code SET DEFAULT 'oai_dc';
ALTER TABLE SobekCM_Item_OAI ALTER COLUMN Locked SET DEFAULT 'false';
ALTER TABLE SobekCM_Item_OAI ALTER COLUMN Data_Code SET DEFAULT 'oai_dc';
ALTER TABLE SobekCM_Item_Viewer_Types ALTER COLUMN "Order" SET DEFAULT 100;
ALTER TABLE SobekCM_Item_Viewer_Types ALTER COLUMN DefaultView SET DEFAULT 'false';
ALTER TABLE SobekCM_Item_Viewer_Types ALTER COLUMN MenuOrder SET DEFAULT 1000;
ALTER TABLE SobekCM_Item_Viewers ALTER COLUMN Exclude SET DEFAULT 'false';
ALTER TABLE SobekCM_Metadata_Types ALTER COLUMN CustomField SET DEFAULT 'false';
ALTER TABLE SobekCM_Metadata_Types ALTER COLUMN canFacetBrowse SET DEFAULT 'true';
ALTER TABLE SobekCM_Metadata_Types ALTER COLUMN DefaultAdvancedSearch SET DEFAULT 'false';
ALTER TABLE SobekCM_OpenPublishing_Theme ALTER COLUMN AvailableForSelection SET DEFAULT 'true';
ALTER TABLE SobekCM_OpenPublishing_Theme ALTER COLUMN "Default" SET DEFAULT 'false';
ALTER TABLE SobekCM_Portal_URL ALTER COLUMN Name SET DEFAULT '';
ALTER TABLE SobekCM_Settings ALTER COLUMN Hidden SET DEFAULT 'false';
ALTER TABLE SobekCM_Settings ALTER COLUMN Reserved SET DEFAULT 0;
ALTER TABLE SobekCM_Statistics ALTER COLUMN Robot_Hits SET DEFAULT -1;
ALTER TABLE SobekCM_Statistics ALTER COLUMN XML_Hits SET DEFAULT -1;
ALTER TABLE SobekCM_Statistics ALTER COLUMN OAI_Hits SET DEFAULT -1;
ALTER TABLE SobekCM_Statistics ALTER COLUMN JSON_Hits SET DEFAULT -1;
ALTER TABLE SobekCM_Statistics ALTER COLUMN Aggregate_Statistics_Complete SET DEFAULT 'false';
ALTER TABLE SobekCM_Web_Skin ALTER COLUMN Build_On_Launch SET DEFAULT 'false';
ALTER TABLE SobekCM_Web_Skin ALTER COLUMN SuppressTopNavigation SET DEFAULT '0';
ALTER TABLE SobekCM_WebContent ALTER COLUMN Deleted SET DEFAULT 'false';
ALTER TABLE SobekCM_WebContent ALTER COLUMN Locked SET DEFAULT 'false';
ALTER TABLE SobekCM_WebContent_Statistics ALTER COLUMN Hits_Complete SET DEFAULT 0;
ALTER TABLE Tracking_Progress ALTER COLUMN Duration SET DEFAULT 0;
ALTER TABLE Archive_Item_Archived_File  ADD  CONSTRAINT FK_Archived_File_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE Archive_Item_Archived_File_Copy  ADD  CONSTRAINT FK_Archived_File_Copy_Location FOREIGN KEY(ArchiveLocationID)
REFERENCES Archive_Location (ArchiveLocationID);
ALTER TABLE Archive_Item_Archived_File_Copy  ADD  CONSTRAINT FK_Archived_File_Copy_Snapshot FOREIGN KEY(SnapshotID)
REFERENCES Archive_Item_Archived_File_Snapshot (SnapshotID);
ALTER TABLE Archive_Item_Archived_File_Snapshot  ADD  CONSTRAINT FK_Archived_File_Snapshot_File FOREIGN KEY(ArchivedFileID)
REFERENCES Archive_Item_Archived_File (ArchivedFileID);
ALTER TABLE Auth_GeoRegion  ADD  CONSTRAINT FK_GEMS_GeoRegion_GEMS_GeoRegionType FOREIGN KEY(RegionTypeID)
REFERENCES Auth_GeoRegion_Type (RegionTypeID);
ALTER TABLE Auth_GeoRegion  ADD  CONSTRAINT FK_UFDC_GeoRegion_UFDC_GeoRegion FOREIGN KEY(ParentRegionID)
REFERENCES Auth_GeoRegion (RegionID);
ALTER TABLE mySobek_User_Bib_Link  ADD  CONSTRAINT FK_User_Bib_Link_Item_Group FOREIGN KEY(GroupID)
REFERENCES SobekCM_Item_Group (GroupID);
ALTER TABLE mySobek_User_DefaultMetadata_Link  ADD  CONSTRAINT FK_mysobek_user_DefaultMetadata_Link_sobek_user FOREIGN KEY(UserID)
REFERENCES mySobek_User (UserID);
ALTER TABLE mySobek_User_DefaultMetadata_Link  ADD  CONSTRAINT FK_sobek_user_DefaultMetadata_Link FOREIGN KEY(DefaultMetadataID)
REFERENCES mySobek_DefaultMetadata (DefaultMetadataID);
ALTER TABLE mySobek_User_Description_Tags  ADD  CONSTRAINT FK_sobek_user_Description_Tags_sobek_user FOREIGN KEY(UserID)
REFERENCES mySobek_User (UserID);
ALTER TABLE mySobek_User_Description_Tags  ADD  CONSTRAINT FK_User_Description_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE mySobek_User_Editable_Link  ADD  CONSTRAINT FK_sobek_user_Editable_Link_sobek_user FOREIGN KEY(UserID)
REFERENCES mySobek_User (UserID);
ALTER TABLE mySobek_User_Editable_Link  ADD  CONSTRAINT FK_sobek_user_Editable_Link_UFDC_Editable_Regex FOREIGN KEY(EditableID)
REFERENCES mySobek_Editable_Regex (EditableID);
ALTER TABLE mySobek_User_Folder  ADD  CONSTRAINT FK_sobek_user_Folder_sobek_user FOREIGN KEY(UserID)
REFERENCES mySobek_User (UserID);
ALTER TABLE mySobek_User_Folder  ADD  CONSTRAINT FK_sobek_user_Folder_sobek_user_Folder FOREIGN KEY(ParentFolderID)
REFERENCES mySobek_User_Folder (UserFolderID);
ALTER TABLE mySobek_User_Group_DefaultMetadata_Link  ADD  CONSTRAINT FK_sobek_user_Group_DefaultMetadata_Link FOREIGN KEY(DefaultMetadataID)
REFERENCES mySobek_DefaultMetadata (DefaultMetadataID);
ALTER TABLE mySobek_User_Group_DefaultMetadata_Link  ADD  CONSTRAINT FK_sobek_user_Group_DefaultMetadata_Link_sobek_user FOREIGN KEY(UserGroupID)
REFERENCES mySobek_User_Group (UserGroupID);
ALTER TABLE mySobek_User_Group_Edit_Aggregation  ADD  CONSTRAINT FK_mySobek_User_Group_Edit_Aggregation_Aggregation FOREIGN KEY(AggregationID)
REFERENCES SobekCM_Item_Aggregation (AggregationID);
ALTER TABLE mySobek_User_Group_Edit_Aggregation  ADD  CONSTRAINT FK_mySobek_User_Group_Edit_Aggregation_User FOREIGN KEY(UserGroupID)
REFERENCES mySobek_User_Group (UserGroupID);
ALTER TABLE mySobek_User_Group_Editable_Link  ADD  CONSTRAINT FK_sobek_user_Group_Editable_Link_sobek_user FOREIGN KEY(UserGroupID)
REFERENCES mySobek_User_Group (UserGroupID);
ALTER TABLE mySobek_User_Group_Editable_Link  ADD  CONSTRAINT FK_sobek_user_Group_Editable_Link_UFDC_Editable_Regex FOREIGN KEY(EditableID)
REFERENCES mySobek_Editable_Regex (EditableID);
ALTER TABLE mySobek_User_Group_Item_Permissions  ADD  CONSTRAINT fk_mySobek_User_Group_Item_Permissions_ItemID FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE mySobek_User_Group_Item_Permissions  ADD  CONSTRAINT fk_mySobek_User_Group_Item_Permissions_UserGroupID FOREIGN KEY(UserGroupID)
REFERENCES mySobek_User_Group (UserGroupID);
ALTER TABLE mySobek_User_Group_Link  ADD  CONSTRAINT FK_mySobek_User_Group_Link_mySobek_User FOREIGN KEY(UserID)
REFERENCES mySobek_User (UserID);
ALTER TABLE mySobek_User_Group_Link  ADD  CONSTRAINT FK_mySobek_User_Group_Link_mySobek_User_Group FOREIGN KEY(UserGroupID)
REFERENCES mySobek_User_Group (UserGroupID);
ALTER TABLE mySobek_User_Group_Template_Link  ADD  CONSTRAINT FK_sobek_user_Group_Template_Link_sobek_user FOREIGN KEY(UserGroupID)
REFERENCES mySobek_User_Group (UserGroupID);
ALTER TABLE mySobek_User_Group_Template_Link  ADD  CONSTRAINT FK_sobek_user_Group_Template_Link_UFDC_Template FOREIGN KEY(TemplateID)
REFERENCES mySobek_Template (TemplateID);
ALTER TABLE mySobek_User_Item  ADD  CONSTRAINT FK_sobek_user_Item_sobek_user_Folder FOREIGN KEY(UserFolderID)
REFERENCES mySobek_User_Folder (UserFolderID);
ALTER TABLE mySobek_User_Item  ADD  CONSTRAINT FK_User_Item_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE mySobek_User_Item_Link  ADD  CONSTRAINT FK_mySobek_User_Item_Link_mySobek_User FOREIGN KEY(UserID)
REFERENCES mySobek_User (UserID);
ALTER TABLE mySobek_User_Item_Link  ADD  CONSTRAINT FK_mySobek_User_Item_Link_mySobek_User_Item_Link_Relationship FOREIGN KEY(RelationshipID)
REFERENCES mySobek_User_Item_Link_Relationship (RelationshipID);
ALTER TABLE mySobek_User_Item_Link  ADD  CONSTRAINT FK_mySobek_User_Item_Link_SobekCM_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE mySobek_User_Item_Permissions  ADD  CONSTRAINT fk_mySobek_User_Item_Permissions_ItemID FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE mySobek_User_Item_Permissions  ADD  CONSTRAINT FK_mySobek_User_Item_Permissions_mySobek_User FOREIGN KEY(UserID)
REFERENCES mySobek_User (UserID);
ALTER TABLE mySobek_User_Item_Permissions  ADD  CONSTRAINT FK_mySobek_User_Item_Permissions_SobekCM_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE mySobek_User_Item_Permissions  ADD  CONSTRAINT fk_mySobek_User_Item_Permissions_UserID FOREIGN KEY(UserID)
REFERENCES mySobek_User (UserID);
ALTER TABLE mySobek_User_Request  ADD  CONSTRAINT FK_mySobek_User_Request_mySobek_User FOREIGN KEY(UserID)
REFERENCES mySobek_User (UserID);
ALTER TABLE mySobek_User_Request  ADD  CONSTRAINT FK_mySobek_User_Request_mySobek_User_Group FOREIGN KEY(UserGroupID)
REFERENCES mySobek_User_Group (UserGroupID);
ALTER TABLE mySobek_User_Request  ADD  CONSTRAINT FK_mySobek_User_Request_mySobek_User1 FOREIGN KEY(ApproverUserID)
REFERENCES mySobek_User (UserID);
ALTER TABLE mySobek_User_Search  ADD  CONSTRAINT FK_sobek_user_Search_sobek_user FOREIGN KEY(UserID)
REFERENCES mySobek_User (UserID);
ALTER TABLE mySobek_User_Settings  ADD  CONSTRAINT FK_User_Settings_User FOREIGN KEY(UserID)
REFERENCES mySobek_User (UserID);
ALTER TABLE mySobek_User_Template_Link  ADD  CONSTRAINT FK_sobek_user_Template_Link_sobek_user FOREIGN KEY(UserID)
REFERENCES mySobek_User (UserID);
ALTER TABLE mySobek_User_Template_Link  ADD  CONSTRAINT FK_sobek_user_Template_Link_UFDC_Template FOREIGN KEY(TemplateID)
REFERENCES mySobek_Template (TemplateID);
ALTER TABLE SobekCM_Browse_Info_Statistics  ADD  CONSTRAINT FK_SobekCM_Browse_Info_Statistics_SobekCM_Item_Aggregation FOREIGN KEY(AggregationID)
REFERENCES SobekCM_Item_Aggregation (AggregationID);
ALTER TABLE SobekCM_Builder_Incoming_Folders  ADD  CONSTRAINT FK_SobekCM_Builder_Incoming_Folders_SobekCM_Builder_Module_Set FOREIGN KEY(ModuleSetID)
REFERENCES SobekCM_Builder_Module_Set (ModuleSetID);
ALTER TABLE SobekCM_Builder_Log  ADD  CONSTRAINT FK_Self_SobekCM_Builder_Log FOREIGN KEY(RelatedBuilderLogID)
REFERENCES SobekCM_Builder_Log (BuilderLogID);
ALTER TABLE SobekCM_Builder_Module  ADD  CONSTRAINT FK_SobekCM_Builder_Module_SobekCM_Builder_Module_Set FOREIGN KEY(ModuleSetID)
REFERENCES SobekCM_Builder_Module_Set (ModuleSetID);
ALTER TABLE SobekCM_Builder_Module_Schedule  ADD  CONSTRAINT FK_SobekCM_Builder_Module_Schedule_SobekCM_Builder_Module_Set FOREIGN KEY(ModuleSetID)
REFERENCES SobekCM_Builder_Module_Set (ModuleSetID);
ALTER TABLE SobekCM_Builder_Module_Scheduled_Run  ADD  CONSTRAINT FK_SobekCM_Builder_Module_Scheduled_Run_SobekCM_Builder_Module_Schedule FOREIGN KEY(ModuleScheduleID)
REFERENCES SobekCM_Builder_Module_Schedule (ModuleScheduleID);
ALTER TABLE SobekCM_Builder_Module_Set  ADD  CONSTRAINT FK_SobekCM_Builder_Module_Set_SobekCM_Builder_Module_Type FOREIGN KEY(ModuleTypeID)
REFERENCES SobekCM_Builder_Module_Type (ModuleTypeID);
ALTER TABLE SobekCM_IP_Restriction_Single  ADD  CONSTRAINT FK_SobekCM_IP_Restriction_Single_SobekCM_IP_Restriction_Range FOREIGN KEY(IP_RangeID)
REFERENCES SobekCM_IP_Restriction_Range (IP_RangeID);
ALTER TABLE SobekCM_Item  ADD  CONSTRAINT FK_SobekCM_Item_Tracking_Disposition_Type FOREIGN KEY(Disposition_Advice)
REFERENCES Tracking_Disposition_Type (DispositionID);
ALTER TABLE SobekCM_Item  ADD  CONSTRAINT FK_SobekCM_Item_Tracking_Disposition_Type1 FOREIGN KEY(Disposition_Type)
REFERENCES Tracking_Disposition_Type (DispositionID);
ALTER TABLE SobekCM_Item  ADD  CONSTRAINT FK_UFDC_Item_UFDC_Item_Group FOREIGN KEY(GroupID)
REFERENCES SobekCM_Item_Group (GroupID);
ALTER TABLE SobekCM_Item_Aggregation_Alias  ADD  CONSTRAINT FK_SobekCM_Item_Aggregation_Alias_SobekCM_Item_Aggregation FOREIGN KEY(AggregationID)
REFERENCES SobekCM_Item_Aggregation (AggregationID);
ALTER TABLE SobekCM_Item_Aggregation_Default_Result_Fields  ADD  CONSTRAINT FK_SobekCM_Item_Aggregation_Default_Result_Fields_SobekCM_Item_Aggregation_Result_Types FOREIGN KEY(ItemAggregationResultTypeID)
REFERENCES SobekCM_Item_Aggregation_Result_Types (ItemAggregationResultTypeID);
ALTER TABLE SobekCM_Item_Aggregation_Facets  ADD  CONSTRAINT FK_SobekCM_Item_Aggregation_Facets_SobekCM_Item_Aggregation FOREIGN KEY(AggregationID)
REFERENCES SobekCM_Item_Aggregation (AggregationID);
ALTER TABLE SobekCM_Item_Aggregation_Facets  ADD  CONSTRAINT FK_SobekCM_Item_Aggregation_Facets_SobekCM_Metadata_Types FOREIGN KEY(MetadataTypeID)
REFERENCES SobekCM_Metadata_Types (MetadataTypeID);
ALTER TABLE SobekCM_Item_Aggregation_Hierarchy  ADD  CONSTRAINT FK_SobekCM_Item_Aggregation_Hierarchy_SobekCM_Item_Aggregation FOREIGN KEY(ChildID)
REFERENCES SobekCM_Item_Aggregation (AggregationID);
ALTER TABLE SobekCM_Item_Aggregation_Hierarchy  ADD  CONSTRAINT FK_SobekCM_Item_Aggregation_Hierarchy_SobekCM_Item_Aggregation1 FOREIGN KEY(ParentID)
REFERENCES SobekCM_Item_Aggregation (AggregationID);
ALTER TABLE SobekCM_Item_Aggregation_Item_Link  ADD  CONSTRAINT FK_SobekCM_Item_Aggregation_Item_Link_SobekCM_Item_Aggregation FOREIGN KEY(AggregationID)
REFERENCES SobekCM_Item_Aggregation (AggregationID);
ALTER TABLE SobekCM_Item_Aggregation_Item_Link  ADD  CONSTRAINT FK_SobekCM_Item_Aggregation_Item_Link_UFDC_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE SobekCM_Item_Aggregation_Milestones  ADD  CONSTRAINT fk_ItemAggregationMilestones FOREIGN KEY(AggregationID)
REFERENCES SobekCM_Item_Aggregation (AggregationID);
ALTER TABLE SobekCM_Item_Aggregation_Result_Fields  ADD  CONSTRAINT FK_SobekCM_Item_Aggregation_Result_Fields_SobekCM_Item_Aggregation_Result_Views FOREIGN KEY(ItemAggregationResultID)
REFERENCES SobekCM_Item_Aggregation_Result_Views (ItemAggregationResultID);
ALTER TABLE SobekCM_Item_Aggregation_Result_Views  ADD  CONSTRAINT FK_SobekCM_Item_Aggregation_Result_Views_AggregationID FOREIGN KEY(AggregationID)
REFERENCES SobekCM_Item_Aggregation (AggregationID);
ALTER TABLE SobekCM_Item_Aggregation_Result_Views  ADD  CONSTRAINT FK_SobekCM_Item_Aggregation_Result_Views_SobekCM_Item_Aggregation_Result_Types FOREIGN KEY(ItemAggregationResultTypeID)
REFERENCES SobekCM_Item_Aggregation_Result_Types (ItemAggregationResultTypeID);
ALTER TABLE SobekCM_Item_Aggregation_Result_Views  ADD  CONSTRAINT FK_SobekCM_Item_Aggregation_Result_Views_TypeID FOREIGN KEY(ItemAggregationResultTypeID)
REFERENCES SobekCM_Item_Aggregation_Result_Types (ItemAggregationResultTypeID);
ALTER TABLE SobekCM_Item_Aggregation_Settings  ADD  CONSTRAINT FK_Aggregation_Settings_Aggregation FOREIGN KEY(AggregationID)
REFERENCES SobekCM_Item_Aggregation (AggregationID);
ALTER TABLE SobekCM_Item_Aggregation_Statistics  ADD  CONSTRAINT FK_SobekCM_Item_Aggregation_Statistics_SobekCM_Item_Aggregation FOREIGN KEY(AggregationID)
REFERENCES SobekCM_Item_Aggregation (AggregationID);
ALTER TABLE SobekCM_Item_Alias  ADD  CONSTRAINT FK_SobekCM_Item_Alias_SobekCM_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE SobekCM_Item_Footprint  ADD  CONSTRAINT FK_UFDC_Item_Footprint_UFDC_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE SobekCM_Item_GeoRegion_Link  ADD  CONSTRAINT FK_UFDC_Item_GeoRegion_Link_UFDC_GeoRegion FOREIGN KEY(RegionID)
REFERENCES Auth_GeoRegion (RegionID);
ALTER TABLE SobekCM_Item_GeoRegion_Link  ADD  CONSTRAINT FK_UFDC_Item_GeoRegion_Link_UFDC_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE SobekCM_Item_Group_External_Record  ADD  CONSTRAINT FK_ExtRecordID_Item_Group_External_Record FOREIGN KEY(ExtRecordTypeID)
REFERENCES SobekCM_External_Record_Type (ExtRecordTypeID);
ALTER TABLE SobekCM_Item_Group_External_Record  ADD  CONSTRAINT FK_GroupID_Item_Group_External_Record FOREIGN KEY(GroupID)
REFERENCES SobekCM_Item_Group (GroupID);
ALTER TABLE SobekCM_Item_Group_OAI  ADD  CONSTRAINT FK_SobekCM_Item_Group_OAI_SobekCM_Item_Group FOREIGN KEY(GroupID)
REFERENCES SobekCM_Item_Group (GroupID);
ALTER TABLE SobekCM_Item_Group_Relationship  ADD  CONSTRAINT FK_SobekCM_Item_Group_Relationship_SobekCM_Item_Group FOREIGN KEY(GroupA)
REFERENCES SobekCM_Item_Group (GroupID);
ALTER TABLE SobekCM_Item_Group_Relationship  ADD  CONSTRAINT FK_SobekCM_Item_Group_Relationship_SobekCM_Item_Group1 FOREIGN KEY(GroupB)
REFERENCES SobekCM_Item_Group (GroupID);
ALTER TABLE SobekCM_Item_Group_Statistics  ADD  CONSTRAINT FK_UFDC_Item_Group_Statistics_UFDC_Item_Group FOREIGN KEY(GroupID)
REFERENCES SobekCM_Item_Group (GroupID);
ALTER TABLE SobekCM_Item_Group_Viewers  ADD  CONSTRAINT FK_SobekCM_Item_Group_Viewers_Item_Group FOREIGN KEY(GroupID)
REFERENCES SobekCM_Item_Group (GroupID);
ALTER TABLE SobekCM_Item_Group_Viewers  ADD  CONSTRAINT FK_SobekCM_Item_Group_Viewers_Viewer_Types FOREIGN KEY(ItemGroupViewTypeID)
REFERENCES SobekCM_Item_Group_Viewer_Types (ItemGroupViewTypeID);
ALTER TABLE SobekCM_Item_Group_Web_Skin_Link  ADD  CONSTRAINT FK_Item_Group_Web_Skin_Link_Item_Group FOREIGN KEY(GroupID)
REFERENCES SobekCM_Item_Group (GroupID);
ALTER TABLE SobekCM_Item_Group_Web_Skin_Link  ADD  CONSTRAINT FK_Item_Group_Web_Skin_Link_Web_Skin FOREIGN KEY(WebSkinID)
REFERENCES SobekCM_Web_Skin (WebSkinID);
ALTER TABLE SobekCM_Item_Icons  ADD  CONSTRAINT FK_UFDC_Item_Icons_UFDC_Icon FOREIGN KEY(IconID)
REFERENCES SobekCM_Icon (IconID);
ALTER TABLE SobekCM_Item_Icons  ADD  CONSTRAINT FK_UFDC_Item_Icons_UFDC_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE SobekCM_Item_OAI  ADD  CONSTRAINT FK_SobekCM_Item_OAI_SobekCM_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE SobekCM_Item_Settings  ADD  CONSTRAINT FK_Item_Settings_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE SobekCM_Item_Statistics  ADD  CONSTRAINT FK_UFDC_Item_Statistics_UFDC_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE SobekCM_Item_Viewers  ADD  CONSTRAINT FK_SobekCM_Item_Viewers_SobekCM_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE SobekCM_Item_Viewers  ADD  CONSTRAINT FK_SobekCM_Item_Viewers_SobekCM_Item_Viewer_Types FOREIGN KEY(ItemViewTypeID)
REFERENCES SobekCM_Item_Viewer_Types (ItemViewTypeID);
ALTER TABLE SobekCM_Portal_Item_Aggregation_Link  ADD  CONSTRAINT FK_SobekCM_Portal_Item_Aggregation_Link_SobekCM_Item_Aggregation FOREIGN KEY(AggregationID)
REFERENCES SobekCM_Item_Aggregation (AggregationID);
ALTER TABLE SobekCM_Portal_Item_Aggregation_Link  ADD  CONSTRAINT FK_SobekCM_Portal_Item_Aggregation_Link_SobekCM_Portal_URL FOREIGN KEY(PortalID)
REFERENCES SobekCM_Portal_URL (PortalID);
ALTER TABLE SobekCM_Portal_URL_Statistics  ADD  CONSTRAINT FK_SobekCM_Portal_URL_Statistics_SobekCM_Portal_URL FOREIGN KEY(PortalID)
REFERENCES SobekCM_Portal_URL (PortalID);
ALTER TABLE SobekCM_Portal_Web_Skin_Link  ADD  CONSTRAINT FK_SobekCM_Portal_Web_Skin_Link_SobekCM_Portal_URL FOREIGN KEY(PortalID)
REFERENCES SobekCM_Portal_URL (PortalID);
ALTER TABLE SobekCM_Portal_Web_Skin_Link  ADD  CONSTRAINT FK_SobekCM_Portal_Web_Skin_Link_SobekCM_Web_Skin FOREIGN KEY(WebSkinID)
REFERENCES SobekCM_Web_Skin (WebSkinID);
ALTER TABLE SobekCM_Project_Aggregation_Link  ADD  CONSTRAINT FK_Project_Aggregation FOREIGN KEY(ProjectID)
REFERENCES SobekCM_Project (ProjectID)
ON UPDATE CASCADE
ON DELETE CASCADE;
ALTER TABLE SobekCM_Project_DefaultMetadata_Link  ADD  CONSTRAINT FK_DefaultMetadata FOREIGN KEY(DefaultMetadataID)
REFERENCES mySobek_DefaultMetadata (DefaultMetadataID)
ON UPDATE CASCADE
ON DELETE CASCADE;
ALTER TABLE SobekCM_Project_DefaultMetadata_Link  ADD  CONSTRAINT FK_Project FOREIGN KEY(ProjectID)
REFERENCES SobekCM_Project (ProjectID)
ON UPDATE CASCADE
ON DELETE CASCADE;
ALTER TABLE SobekCM_Project_Item_Link  ADD  CONSTRAINT FK_Project_Item_ItemID FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID)
ON UPDATE CASCADE
ON DELETE CASCADE;
ALTER TABLE SobekCM_Project_Item_Link  ADD  CONSTRAINT FK_Project_Item_ProjectID FOREIGN KEY(ProjectID)
REFERENCES SobekCM_Project (ProjectID)
ON UPDATE CASCADE
ON DELETE CASCADE;
ALTER TABLE SobekCM_Project_Template_Link  ADD  CONSTRAINT FK_Project_1 FOREIGN KEY(ProjectID)
REFERENCES SobekCM_Project (ProjectID)
ON UPDATE CASCADE
ON DELETE CASCADE;
ALTER TABLE SobekCM_Project_Template_Link  ADD  CONSTRAINT FK_Template FOREIGN KEY(TemplateID)
REFERENCES mySobek_Template (TemplateID)
ON UPDATE CASCADE
ON DELETE CASCADE;
ALTER TABLE SobekCM_QC_Errors  ADD  CONSTRAINT ItemID_FK FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE SobekCM_QC_Errors_History  ADD  CONSTRAINT ItemID_FK2 FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE SobekCM_WebContent_Milestones  ADD  CONSTRAINT FK_SobekCM_WebContent_Milestones_SobekCM_WebContent FOREIGN KEY(WebContentID)
REFERENCES SobekCM_WebContent (WebContentID);
ALTER TABLE Tracking_Item  ADD  CONSTRAINT FK_Tracking_Item_SobekCM_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE Tracking_Progress  ADD  CONSTRAINT FK_Progress_Item FOREIGN KEY(ItemID)
REFERENCES SobekCM_Item (ItemID);
ALTER TABLE Tracking_Progress  ADD  CONSTRAINT FK_Progress_WorkFlow FOREIGN KEY(WorkFlowID)
REFERENCES Tracking_WorkFlow (WorkFlowID);


/** !START_CREATE_VIEWS! **/

-- Originally a SQL-Server "indexed view" (WITH SCHEMABINDING + a unique clustered
-- index). PostgreSQL has no automatically-maintained equivalent, so this is ported
-- as a plain view; the query is re-evaluated on each use.
CREATE VIEW Statistics_Item_Aggregation_Link_View
AS
SELECT  AggregationID, I.ItemID, I.FileCount, I.PageCount, I.GroupID, Milestone_OnlineComplete
FROM  SobekCM_Item_Aggregation_Item_Link CL, SobekCM_Item I
WHERE ( CL.ItemID = I.ItemID )
  and ( I.Deleted = 'false' )
  and ( Milestone_OnlineComplete is not null );

-- Originally a SQL-Server "indexed view"; ported as a plain view (see note above).
CREATE VIEW Statistics_Item_Aggregation_Link_View2
AS
SELECT  AggregationID, I.ItemID, I.FileCount, I.PageCount, I.GroupID, coalesce(I.CreateDate, I.Milestone_DigitalAcquisition) as CreateDate
FROM  SobekCM_Item_Aggregation_Item_Link CL, SobekCM_Item I
WHERE ( CL.ItemID = I.ItemID )
  and ( I.Deleted = 'false' )
  and (( I.FileCount > 0 ) or ( I.PageCount > 0 ));

-- Originally a SQL-Server "indexed view"; ported as a plain view (see note above).
CREATE VIEW Statistics_Item_Aggregation_Link_View3
AS
SELECT  AggregationID, I.ItemID, I.FileCount, I.PageCount, I.GroupID, coalesce(I.CreateDate, I.Milestone_DigitalAcquisition) as CreateDate
FROM  SobekCM_Item_Aggregation_Item_Link CL, SobekCM_Item I
WHERE ( CL.ItemID = I.ItemID )
  and ( I.Deleted = 'false' );

/** !START_CREATE_STORED_PROCEDURES! **/

-- Not called from any C# code (likely run as a standalone/scheduled maintenance job).
-- Originally returned two result sets (unembargoed items, then the email list); since
-- nothing consumes either programmatically, this returns only the primary one (the
-- unembargoed items) and still performs all the same side effects (updates, workflow
-- logging, emails).
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
	set Dark='false', IP_Restriction_Mask=0, AdditionalWorkNeeded='true'
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


-- Get the full archiving history (files, snapshots, and stored copies) for a single item
CREATE OR REPLACE FUNCTION Archive_Get_Item_History(
	p_ItemID integer
)
RETURNS TABLE (
	ArchivedFileID integer,
	FileName varchar(255),
	FileExtension varchar(20),
	SnapshotID integer,
	FileSize bigint,
	OriginalCreationDate timestamp,
	SHA256_Hash char(64),
	SnapshotDate timestamp,
	MimeType varchar(100),
	EncodingDetails varchar(500),
	ArchivedFileCopyID integer,
	StoragePath varchar(1000),
	StoredDate timestamp,
	VerifiedDate timestamp,
	Status varchar(20),
	ArchiveLocationID smallint,
	LocationName varchar(50),
	LocationType varchar(20),
	ContainerName varchar(255)
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	select F.ArchivedFileID, F.FileName, F.FileExtension,
	       S.SnapshotID, S.FileSize, S.OriginalCreationDate, S.SHA256_Hash, S.SnapshotDate, S.MimeType, S.EncodingDetails,
	       C.ArchivedFileCopyID, C.StoragePath, C.StoredDate, C.VerifiedDate, C.Status,
	       L.ArchiveLocationID, L.LocationName, L.LocationType, L.ContainerName
	from Archive_Item_Archived_File F left outer join
	     Archive_Item_Archived_File_Snapshot S on S.ArchivedFileID = F.ArchivedFileID left outer join
	     Archive_Item_Archived_File_Copy C on C.SnapshotID = S.SnapshotID left outer join
	     Archive_Location L on L.ArchiveLocationID = C.ArchiveLocationID
	where F.ItemID = p_ItemID;
END;
$$;


-- Get the bare necessity archiving history (files, snapshots, and stored copies) for a single item
-- for public consumption online
CREATE OR REPLACE FUNCTION Archive_Get_Item_History_Public(
	p_ItemID integer
)
RETURNS TABLE (
	ArchivedFileID integer,
	FileName varchar(255),
	FileExtension varchar(20),
	FileSize bigint,
	OriginalCreationDate timestamp,
	StoredDate timestamp,
	Status varchar(20),
	LocationName varchar(50)
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	select F.ArchivedFileID, F.FileName, F.FileExtension, S.FileSize, S.OriginalCreationDate, C.StoredDate, C.Status, L.LocationName
	from Archive_Item_Archived_File F left outer join
	     Archive_Item_Archived_File_Snapshot S on S.ArchivedFileID = F.ArchivedFileID left outer join
	     Archive_Item_Archived_File_Copy C on C.SnapshotID = S.SnapshotID left outer join
	     Archive_Location L on L.ArchiveLocationID = C.ArchiveLocationID
	where F.ItemID = p_ItemID;
END;
$$;


-- Save information about an archived file, creating the file/snapshot/copy rows only as needed
CREATE OR REPLACE FUNCTION Archive_Save_File(
	p_ItemID integer,
	p_FileName varchar(255),
	p_FileSize bigint,
	p_SHA256_Hash char(64),
	p_OriginalCreationDate timestamp,
	p_StoragePath varchar(1000),
	p_StoredDate timestamp,
	p_LocationName varchar(50),
	p_MimeType varchar(100) DEFAULT null,
	p_EncodingDetails varchar(500) DEFAULT null
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_ArchivedFileID integer;
	v_SnapshotID integer;
	v_ArchiveLocationID smallint;
	v_FileExtension varchar(20);
BEGIN
	-- Pull the extension off the file name rather than taking it as a separate argument,
	-- so it can never drift out of sync with the actual file name
	v_FileExtension := case
		when STRPOS(REVERSE(p_FileName), '.') > 0
		then RIGHT(p_FileName, STRPOS(REVERSE(p_FileName), '.') - 1)
		else ''
	end;

	-- Find (or create) the stable file identity for this item/filename
	select ArchivedFileID into v_ArchivedFileID
	from Archive_Item_Archived_File
	where ItemID = p_ItemID and FileName = p_FileName;

	if ( v_ArchivedFileID is null ) then
		insert into Archive_Item_Archived_File ( ItemID, FileName, FileExtension )
		values ( p_ItemID, p_FileName, v_FileExtension )
		returning ArchivedFileID into v_ArchivedFileID;
	end if;

	-- Find (or create) a matching snapshot -- same size/hash/creation date means the same
	-- archiving event, even if this procedure gets called again for it (e.g. a retry)
	select SnapshotID into v_SnapshotID
	from Archive_Item_Archived_File_Snapshot
	where ArchivedFileID = v_ArchivedFileID
	  and FileSize = p_FileSize
	  and SHA256_Hash = p_SHA256_Hash
	  and OriginalCreationDate = p_OriginalCreationDate;

	if ( v_SnapshotID is null ) then
		insert into Archive_Item_Archived_File_Snapshot ( ArchivedFileID, FileSize, OriginalCreationDate, SHA256_Hash, SnapshotDate, MimeType, EncodingDetails )
		values ( v_ArchivedFileID, p_FileSize, p_OriginalCreationDate, p_SHA256_Hash, p_StoredDate, p_MimeType, p_EncodingDetails )
		returning SnapshotID into v_SnapshotID;
	end if;

	-- Resolve the storage location by name
	select ArchiveLocationID into v_ArchiveLocationID
	from Archive_Location
	where LocationName = p_LocationName;

	if ( v_ArchiveLocationID is null ) then
		RAISE EXCEPTION 'Archive_Save_File: Unknown archive location %', p_LocationName;
	end if;

	-- Find (or create) the copy of this snapshot at this location
	if not exists ( select 1 from Archive_Item_Archived_File_Copy where SnapshotID = v_SnapshotID and ArchiveLocationID = v_ArchiveLocationID ) then
		insert into Archive_Item_Archived_File_Copy ( SnapshotID, ArchiveLocationID, StoragePath, StoredDate, Status )
		values ( v_SnapshotID, v_ArchiveLocationID, p_StoragePath, p_StoredDate, 'Stored' );
	end if;
END;
$$;

CREATE OR REPLACE FUNCTION mySobek_Add_Description_Tag(
	p_UserID integer,
	p_TagID integer,
	p_ItemID integer,
	p_Description varchar(2000),
	OUT p_new_TagID integer
)
LANGUAGE plpgsql
AS $$
BEGIN
	p_new_TagID := -1;

	if ( COALESCE(p_TagID, -1 ) > 0 ) then
		update mySobek_User_Description_Tags
		set Description_Tag = p_Description, Date_Modified = now()
		where TagID=p_TagID and UserID=p_UserID;

		p_new_TagID := p_TagID;
	else
		-- Can have up to five comments on a single item
		if (( select COUNT(*) from mySobek_User_Description_Tags where UserID=p_UserID and ItemID=p_ItemID ) < 5) then
			insert into mySobek_User_Description_Tags( UserID, ItemID, Description_Tag, Date_Modified )
			values ( p_UserID, p_ItemID, p_Description, now() )
			returning TagID into p_new_TagID;
		end if;
	end if;
END;
$$;


-- Add an item to the user's folder
CREATE OR REPLACE FUNCTION mySobek_Add_Item_To_User_Folder(
	p_userid integer,
	p_foldername varchar(255),
	p_bibid varchar(10),
	p_vid varchar(5),
	p_itemorder integer,
	p_usernotes varchar(2000)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_itemid integer;
	v_userfolderid integer;
BEGIN
	-- Is there a match for this bib id /vid?
	if (( select COUNT(*) from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = p_bibid and I.VID = p_vid ) = 1 ) then
		-- Get the item id
		select ItemID into v_itemid from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = p_bibid and I.VID = p_vid;

		-- First, see if this user already has a folder named this
		if (( select count(*) from mySobek_User_Folder where UserID=p_userid and FolderName=p_foldername) > 0 ) then
			-- Get the existing folder id
			select UserFolderID into v_userfolderid from mySobek_User_Folder where UserID=p_userid and FolderName=p_foldername;
		else
			-- Add this folder
			insert into mySobek_User_Folder ( UserID, FolderName, isPublic )
			values ( p_userid, p_foldername, 'false' )
			returning UserFolderID into v_userfolderid;
		end if;

		-- Now, see if the item is already linked to the folder
		if (( select count(*) from mySobek_User_Item where ItemID=v_itemid and UserFolderID=v_userfolderid ) > 0 ) then
			-- Just update the existing link then
			update mySobek_User_Item
			set ItemOrder = p_itemorder, UserNotes=p_usernotes
			where UserFolderID = v_userfolderid and ItemID=v_itemid;
		else
			-- Add a new link then
			insert into mySobek_User_Item( UserFolderID, ItemID, ItemOrder, UserNotes, DateAdded )
			values ( v_userfolderid, v_itemid, p_itemorder, p_usernotes, now() );
		end if;
	end if;
END;
$$;


-- Procedure to add links between a user and item aggregations
CREATE OR REPLACE FUNCTION mySobek_Add_User_Aggregations_Link(
	p_UserID integer,
	p_AggregationCode1 varchar(20), p_canSelect1 boolean, p_canEditMetadata1 boolean, p_canEditBehaviors1 boolean, p_canPerformQc1 boolean, p_canUploadFiles1 boolean, p_canChangeVisibility1 boolean, p_canDelete1 boolean, p_isCurator1 boolean, p_onHomePage1 boolean, p_isAdmin1 boolean,
	p_AggregationCode2 varchar(20), p_canSelect2 boolean, p_canEditMetadata2 boolean, p_canEditBehaviors2 boolean, p_canPerformQc2 boolean, p_canUploadFiles2 boolean, p_canChangeVisibility2 boolean, p_canDelete2 boolean, p_isCurator2 boolean, p_onHomePage2 boolean, p_isAdmin2 boolean,
	p_AggregationCode3 varchar(20), p_canSelect3 boolean, p_canEditMetadata3 boolean, p_canEditBehaviors3 boolean, p_canPerformQc3 boolean, p_canUploadFiles3 boolean, p_canChangeVisibility3 boolean, p_canDelete3 boolean, p_isCurator3 boolean, p_onHomePage3 boolean, p_isAdmin3 boolean
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_Aggregation1_Id integer;
	v_Aggregation2_Id integer;
	v_Aggregation3_Id integer;
BEGIN
	-- Add the first aggregation
	if (( length(p_AggregationCode1) > 0 ) and ((select count(*) from SobekCM_Item_Aggregation where Code=p_AggregationCode1 ) = 1 )) then
		select AggregationID into v_Aggregation1_Id from SobekCM_Item_Aggregation where Code=p_AggregationCode1;

		if (( select count(*) from mySobek_User_Edit_Aggregation where UserID=p_UserID and AggregationID=v_Aggregation1_Id ) = 0 ) then
			insert into mySobek_User_Edit_Aggregation ( UserID, AggregationID, CanSelect, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete, IsCurator, OnHomePage, IsAdmin, CanEditItems )
			values ( p_UserID, v_Aggregation1_Id, p_canSelect1, p_canEditMetadata1, p_canEditBehaviors1, p_canPerformQc1, p_canUploadFiles1, p_canChangeVisibility1, p_canDelete1, p_isCurator1, p_onHomePage1, p_isAdmin1, p_canEditMetadata1 );
		else
			update mySobek_User_Edit_Aggregation
			set CanSelect=p_canSelect1, CanEditMetadata=p_canEditMetadata1, CanEditBehaviors=p_canEditBehaviors1, CanPerformQc=p_canPerformQc1, CanUploadFiles=p_canUploadFiles1, CanChangeVisibility=p_canChangeVisibility1, CanDelete=p_canDelete1, IsCurator=p_isCurator1, OnHomePage=p_onHomePage1, IsAdmin=p_isAdmin1, CanEditItems=p_canEditMetadata1
			where UserID=p_UserID and AggregationID=v_Aggregation1_Id;
		end if;
	end if;

	-- Add the second aggregation
	if (( length(p_AggregationCode2) > 0 ) and ((select count(*) from SobekCM_Item_Aggregation where Code=p_AggregationCode2 ) = 1 )) then
		select AggregationID into v_Aggregation2_Id from SobekCM_Item_Aggregation where Code=p_AggregationCode2;

		if (( select count(*) from mySobek_User_Edit_Aggregation where UserID=p_UserID and AggregationID=v_Aggregation2_Id ) = 0 ) then
			insert into mySobek_User_Edit_Aggregation ( UserID, AggregationID, CanSelect, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete, IsCurator, OnHomePage, IsAdmin, CanEditItems )
			values ( p_UserID, v_Aggregation2_Id, p_canSelect2, p_canEditMetadata2, p_canEditBehaviors2, p_canPerformQc2, p_canUploadFiles2, p_canChangeVisibility2, p_canDelete2, p_isCurator2, p_onHomePage2, p_isAdmin2, p_canEditMetadata2 );
		else
			update mySobek_User_Edit_Aggregation
			set CanSelect=p_canSelect2, CanEditMetadata=p_canEditMetadata2, CanEditBehaviors=p_canEditBehaviors2, CanPerformQc=p_canPerformQc2, CanUploadFiles=p_canUploadFiles2, CanChangeVisibility=p_canChangeVisibility2, CanDelete=p_canDelete2, IsCurator=p_isCurator2, OnHomePage=p_onHomePage2, IsAdmin=p_isAdmin2, CanEditItems=p_canEditMetadata2
			where UserID=p_UserID and AggregationID=v_Aggregation2_Id;
		end if;
	end if;

	-- Add the third aggregation
	if (( length(p_AggregationCode3) > 0 ) and ((select count(*) from SobekCM_Item_Aggregation where Code=p_AggregationCode3 ) = 1 )) then
		select AggregationID into v_Aggregation3_Id from SobekCM_Item_Aggregation where Code=p_AggregationCode3;

		if (( select count(*) from mySobek_User_Edit_Aggregation where UserID=p_UserID and AggregationID=v_Aggregation3_Id ) = 0 ) then
			insert into mySobek_User_Edit_Aggregation ( UserID, AggregationID, CanSelect, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete, IsCurator, OnHomePage, IsAdmin, CanEditItems )
			values ( p_UserID, v_Aggregation3_Id, p_canSelect3, p_canEditMetadata3, p_canEditBehaviors3, p_canPerformQc3, p_canUploadFiles3, p_canChangeVisibility3, p_canDelete3, p_isCurator3, p_onHomePage3, p_isAdmin3, p_canEditMetadata3 );
		else
			update mySobek_User_Edit_Aggregation
			set CanSelect=p_canSelect3, CanEditMetadata=p_canEditMetadata3, CanEditBehaviors=p_canEditBehaviors3, CanPerformQc=p_canPerformQc3, CanUploadFiles=p_canUploadFiles3, CanChangeVisibility=p_canChangeVisibility3, CanDelete=p_canDelete3, IsCurator=p_isCurator3, OnHomePage=p_onHomePage3, IsAdmin=p_isAdmin3, CanEditItems=p_canEditMetadata3
			where UserID=p_UserID and AggregationID=v_Aggregation3_Id;
		end if;
	end if;
END;
$$;


-- Add a link between a user and default metadata
CREATE OR REPLACE FUNCTION mySobek_Add_User_DefaultMetadata_Link(
	p_userid integer,
	p_metadata_default varchar(20),
	p_metadata2 varchar(20),
	p_metadata3 varchar(20),
	p_metadata4 varchar(20),
	p_metadata5 varchar(20)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_metadata_default_id integer;
	v_metadata2_id integer;
	v_metadata3_id integer;
	v_metadata4_id integer;
	v_metadata5_id integer;
BEGIN
	if (( length(p_metadata_default) > 0 ) and ( (select count(*) from mySobek_DefaultMetadata where MetadataCode = p_metadata_default ) = 1 )) then
		update mySobek_User_DefaultMetadata_Link set CurrentlySelected='false' where UserID = p_userid;

		select DefaultMetadataID into v_metadata_default_id from mySobek_DefaultMetadata where MetadataCode=p_metadata_default;

		insert into mySobek_User_DefaultMetadata_Link ( UserID, DefaultMetadataID, CurrentlySelected )
		values ( p_userid, v_metadata_default_id, 'true' );
	end if;

	if (( length(p_metadata2) > 0 ) and ((select count(*) from mySobek_DefaultMetadata where MetadataCode = p_metadata2 ) = 1 )) then
		select DefaultMetadataID into v_metadata2_id from mySobek_DefaultMetadata where MetadataCode=p_metadata2;
		insert into mySobek_User_DefaultMetadata_Link ( UserID, DefaultMetadataID, CurrentlySelected )
		values ( p_userid, v_metadata2_id, 'false' );
	end if;

	if (( length(p_metadata3) > 0 ) and ((select count(*) from mySobek_DefaultMetadata where MetadataCode = p_metadata3 ) = 1 )) then
		select DefaultMetadataID into v_metadata3_id from mySobek_DefaultMetadata where MetadataCode=p_metadata3;
		insert into mySobek_User_DefaultMetadata_Link ( UserID, DefaultMetadataID, CurrentlySelected )
		values ( p_userid, v_metadata3_id, 'false' );
	end if;

	if (( length(p_metadata4) > 0 ) and ((select count(*) from mySobek_DefaultMetadata where MetadataCode = p_metadata4 ) = 1 )) then
		select DefaultMetadataID into v_metadata4_id from mySobek_DefaultMetadata where MetadataCode=p_metadata4;
		insert into mySobek_User_DefaultMetadata_Link ( UserID, DefaultMetadataID, CurrentlySelected )
		values ( p_userid, v_metadata4_id, 'false' );
	end if;

	if (( length(p_metadata5) > 0 ) and ((select count(*) from mySobek_DefaultMetadata where MetadataCode = p_metadata5 ) = 1 )) then
		select DefaultMetadataID into v_metadata5_id from mySobek_DefaultMetadata where MetadataCode=p_metadata5;
		insert into mySobek_User_DefaultMetadata_Link ( UserID, DefaultMetadataID, CurrentlySelected )
		values ( p_userid, v_metadata5_id, 'false' );
	end if;
END;
$$;


-- Procedure to add links between a user group and item aggregations.
-- NOTE: The OnHomePage values are NOT used, but are included to keep this
--       signature the same as the single user aggregation link procedure
--       reducing overhead for maintenance
CREATE OR REPLACE FUNCTION mySobek_Add_User_Group_Aggregations_Link(
	p_UserGroupID integer,
	p_AggregationCode1 varchar(20), p_canSelect1 boolean, p_canEditMetadata1 boolean, p_canEditBehaviors1 boolean, p_canPerformQc1 boolean, p_canUploadFiles1 boolean, p_canChangeVisibility1 boolean, p_canDelete1 boolean, p_isCurator1 boolean, p_onHomePage1 boolean, p_isAdmin1 boolean,
	p_AggregationCode2 varchar(20), p_canSelect2 boolean, p_canEditMetadata2 boolean, p_canEditBehaviors2 boolean, p_canPerformQc2 boolean, p_canUploadFiles2 boolean, p_canChangeVisibility2 boolean, p_canDelete2 boolean, p_isCurator2 boolean, p_onHomePage2 boolean, p_isAdmin2 boolean,
	p_AggregationCode3 varchar(20), p_canSelect3 boolean, p_canEditMetadata3 boolean, p_canEditBehaviors3 boolean, p_canPerformQc3 boolean, p_canUploadFiles3 boolean, p_canChangeVisibility3 boolean, p_canDelete3 boolean, p_isCurator3 boolean, p_onHomePage3 boolean, p_isAdmin3 boolean
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_Aggregation1_Id integer;
	v_Aggregation2_Id integer;
	v_Aggregation3_Id integer;
BEGIN
	if (( length(p_AggregationCode1) > 0 ) and ((select count(*) from SobekCM_Item_Aggregation where Code=p_AggregationCode1 ) = 1 )) then
		select AggregationID into v_Aggregation1_Id from SobekCM_Item_Aggregation where Code=p_AggregationCode1;
		insert into mySobek_User_Group_Edit_Aggregation ( UserGroupID, AggregationID, CanSelect, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete, IsCurator, CanEditItems, IsAdmin )
		values ( p_UserGroupID, v_Aggregation1_Id, p_canSelect1, p_canEditMetadata1, p_canEditBehaviors1, p_canPerformQc1, p_canUploadFiles1, p_canChangeVisibility1, p_canDelete1, p_isCurator1, p_canEditMetadata1, p_isAdmin1 );
	end if;

	if (( length(p_AggregationCode2) > 0 ) and ((select count(*) from SobekCM_Item_Aggregation where Code=p_AggregationCode2 ) = 1 )) then
		select AggregationID into v_Aggregation2_Id from SobekCM_Item_Aggregation where Code=p_AggregationCode2;
		insert into mySobek_User_Group_Edit_Aggregation ( UserGroupID, AggregationID, CanSelect, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete, IsCurator, CanEditItems, IsAdmin )
		values ( p_UserGroupID, v_Aggregation2_Id, p_canSelect2, p_canEditMetadata2, p_canEditBehaviors2, p_canPerformQc2, p_canUploadFiles2, p_canChangeVisibility2, p_canDelete2, p_isCurator2, p_canEditMetadata2, p_isAdmin2 );
	end if;

	if (( length(p_AggregationCode3) > 0 ) and ((select count(*) from SobekCM_Item_Aggregation where Code=p_AggregationCode3 ) = 1 )) then
		select AggregationID into v_Aggregation3_Id from SobekCM_Item_Aggregation where Code=p_AggregationCode3;
		insert into mySobek_User_Group_Edit_Aggregation ( UserGroupID, AggregationID, CanSelect, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete, IsCurator, CanEditItems, IsAdmin )
		values ( p_UserGroupID, v_Aggregation3_Id, p_canSelect3, p_canEditMetadata3, p_canEditBehaviors3, p_canPerformQc3, p_canUploadFiles3, p_canChangeVisibility3, p_canDelete3, p_isCurator3, p_canEditMetadata3, p_isAdmin3 );
	end if;
END;
$$;


-- Add a link between a user and a set of default metadata
CREATE OR REPLACE FUNCTION mySobek_Add_User_Group_Metadata_Link(
	p_usergroupid integer,
	p_metadata1 varchar(20),
	p_metadata2 varchar(20),
	p_metadata3 varchar(20),
	p_metadata4 varchar(20),
	p_metadata5 varchar(20)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_metadata1_id integer;
	v_metadata2_id integer;
	v_metadata3_id integer;
	v_metadata4_id integer;
	v_metadata5_id integer;
BEGIN
	if (( length(p_metadata1) > 0 ) and ( (select count(*) from mySobek_DefaultMetadata where MetadataCode = p_metadata1 ) = 1 )) then
		select DefaultMetadataID into v_metadata1_id from mySobek_DefaultMetadata where MetadataCode=p_metadata1;
		insert into mySobek_User_Group_DefaultMetadata_Link ( UserGroupID, DefaultMetadataID )
		values ( p_usergroupid, v_metadata1_id );
	end if;

	if (( length(p_metadata2) > 0 ) and ( (select count(*) from mySobek_DefaultMetadata where MetadataCode = p_metadata2 ) = 1 )) then
		select DefaultMetadataID into v_metadata2_id from mySobek_DefaultMetadata where MetadataCode=p_metadata2;
		insert into mySobek_User_Group_DefaultMetadata_Link ( UserGroupID, DefaultMetadataID )
		values ( p_usergroupid, v_metadata2_id );
	end if;

	if (( length(p_metadata3) > 0 ) and ( (select count(*) from mySobek_DefaultMetadata where MetadataCode = p_metadata3 ) = 1 )) then
		select DefaultMetadataID into v_metadata3_id from mySobek_DefaultMetadata where MetadataCode=p_metadata3;
		insert into mySobek_User_Group_DefaultMetadata_Link ( UserGroupID, DefaultMetadataID )
		values ( p_usergroupid, v_metadata3_id );
	end if;

	if (( length(p_metadata4) > 0 ) and ( (select count(*) from mySobek_DefaultMetadata where MetadataCode = p_metadata4 ) = 1 )) then
		select DefaultMetadataID into v_metadata4_id from mySobek_DefaultMetadata where MetadataCode=p_metadata4;
		insert into mySobek_User_Group_DefaultMetadata_Link ( UserGroupID, DefaultMetadataID )
		values ( p_usergroupid, v_metadata4_id );
	end if;

	if (( length(p_metadata5) > 0 ) and ( (select count(*) from mySobek_DefaultMetadata where MetadataCode = p_metadata5 ) = 1 )) then
		select DefaultMetadataID into v_metadata5_id from mySobek_DefaultMetadata where MetadataCode=p_metadata5;
		insert into mySobek_User_Group_DefaultMetadata_Link ( UserGroupID, DefaultMetadataID )
		values ( p_usergroupid, v_metadata5_id );
	end if;
END;
$$;


-- Add a link between a user and a template
CREATE OR REPLACE FUNCTION mySobek_Add_User_Group_Templates_Link(
	p_usergroupid integer,
	p_template1 varchar(20),
	p_template2 varchar(20),
	p_template3 varchar(20),
	p_template4 varchar(20),
	p_template5 varchar(20)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_template1_id integer;
	v_template2_id integer;
	v_template3_id integer;
	v_template4_id integer;
	v_template5_id integer;
BEGIN
	if (( length(p_template1) > 0 ) and ( (select count(*) from mySobek_Template where TemplateCode = p_template1 ) = 1 )) then
		select TemplateID into v_template1_id from mySobek_Template where TemplateCode=p_template1;
		insert into mySobek_User_Group_Template_Link ( UserGroupID, TemplateID )
		values ( p_usergroupid, v_template1_id );
	end if;

	if (( length(p_template2) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = p_template2 ) = 1 )) then
		select TemplateID into v_template2_id from mySobek_Template where TemplateCode=p_template2;
		insert into mySobek_User_Group_Template_Link ( UserGroupID, TemplateID )
		values ( p_usergroupid, v_template2_id );
	end if;

	if (( length(p_template3) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = p_template3 ) = 1 )) then
		select TemplateID into v_template3_id from mySobek_Template where TemplateCode=p_template3;
		insert into mySobek_User_Group_Template_Link ( UserGroupID, TemplateID )
		values ( p_usergroupid, v_template3_id );
	end if;

	if (( length(p_template4) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = p_template4 ) = 1 )) then
		select TemplateID into v_template4_id from mySobek_Template where TemplateCode=p_template4;
		insert into mySobek_User_Group_Template_Link ( UserGroupID, TemplateID )
		values ( p_usergroupid, v_template4_id );
	end if;

	if (( length(p_template5) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = p_template5 ) = 1 )) then
		select TemplateID into v_template5_id from mySobek_Template where TemplateCode=p_template5;
		insert into mySobek_User_Group_Template_Link ( UserGroupID, TemplateID )
		values ( p_usergroupid, v_template5_id );
	end if;
END;
$$;


-- Preserves the original T-SQL logic exactly, including its apparent inversion of the
-- delete/update branches (a nonempty new value triggers a delete, not an update) --
-- ported as-is rather than silently "fixed" during the port.
CREATE OR REPLACE FUNCTION mySobek_Add_User_Setting(
	p_userid integer,
	p_setting_key varchar(255),
	p_setting_value text
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if ( (select count(*) from mySobek_User_Settings where UserID=p_userid and Setting_Key=p_setting_key ) > 0 ) then
		if ( length(p_setting_value) > 0 ) then
			delete from mySobek_User_Settings where UserID=p_userid and Setting_Key=p_setting_key;
		else
			update mySobek_User_Settings
			set Setting_Value=p_setting_value
			where UserID=p_userid and Setting_Key=p_setting_key;
		end if;
	else
		insert into mySobek_User_Settings ( UserID, Setting_Key, Setting_Value )
		values ( p_userid, p_setting_key, p_setting_value );
	end if;
END;
$$;


-- Add a link between a user and a template
CREATE OR REPLACE FUNCTION mySobek_Add_User_Templates_Link(
	p_userid integer,
	p_template_default varchar(20),
	p_template2 varchar(20),
	p_template3 varchar(20),
	p_template4 varchar(20),
	p_template5 varchar(20)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_template_default_id integer;
	v_template2_id integer;
	v_template3_id integer;
	v_template4_id integer;
	v_template5_id integer;
BEGIN
	if (( length(p_template_default) > 0 ) and ( (select count(*) from mySobek_Template where TemplateCode = p_template_default ) = 1 )) then
		update mySobek_User_Template_Link set DefaultTemplate='false' where UserID = p_userid;
		select TemplateID into v_template_default_id from mySobek_Template where TemplateCode=p_template_default;
		insert into mySobek_User_Template_Link ( UserID, TemplateID, DefaultTemplate )
		values ( p_userid, v_template_default_id, 'true' );
	end if;

	if (( length(p_template2) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = p_template2 ) = 1 )) then
		select TemplateID into v_template2_id from mySobek_Template where TemplateCode=p_template2;
		insert into mySobek_User_Template_Link ( UserID, TemplateID, DefaultTemplate )
		values ( p_userid, v_template2_id, 'false' );
	end if;

	if (( length(p_template3) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = p_template3 ) = 1 )) then
		select TemplateID into v_template3_id from mySobek_Template where TemplateCode=p_template3;
		insert into mySobek_User_Template_Link ( UserID, TemplateID, DefaultTemplate )
		values ( p_userid, v_template3_id, 'false' );
	end if;

	if (( length(p_template4) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = p_template4 ) = 1 )) then
		select TemplateID into v_template4_id from mySobek_Template where TemplateCode=p_template4;
		insert into mySobek_User_Template_Link ( UserID, TemplateID, DefaultTemplate )
		values ( p_userid, v_template4_id, 'false' );
	end if;

	if (( length(p_template5) > 0 ) and ((select count(*) from mySobek_Template where TemplateCode = p_template5 ) = 1 )) then
		select TemplateID into v_template5_id from mySobek_Template where TemplateCode=p_template5;
		insert into mySobek_User_Template_Link ( UserID, TemplateID, DefaultTemplate )
		values ( p_userid, v_template5_id, 'false' );
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION mySobek_Clear_User_Settings(
	p_userid integer
)
RETURNS void
LANGUAGE sql
AS $$
	delete from mySobek_User_Settings
	where UserID=p_userid;
$$;


-- Procedure to delete a default metadata set
CREATE OR REPLACE FUNCTION mySobek_Delete_DefaultMetadata(
	p_MetadataCode varchar(20)
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if ( p_MetadataCode != 'NONE' ) then
		delete from mySobek_DefaultMetadata where MetadataCode=p_MetadataCode;
	end if;
END;
$$;


-- Delete a user's tag
CREATE OR REPLACE FUNCTION mySobek_Delete_Description_Tag(
	p_TagID integer
)
RETURNS void
LANGUAGE sql
AS $$
	delete from mySobek_User_Description_Tags where TagID=p_TagID;
$$;


-- Delete an item from the user's folder
CREATE OR REPLACE FUNCTION mySobek_Delete_Item_From_All_User_Folders(
	p_userid integer,
	p_bibid varchar(10),
	p_vid varchar(5)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_itemid integer;
BEGIN
	if (( select COUNT(*) from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = p_bibid and I.VID = p_vid ) = 1 ) then
		select ItemID into v_itemid from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = p_bibid and I.VID = p_vid;

		delete from mySobek_User_Item
		where ( ItemID=v_itemid )
		  and exists (	select FolderName
						from mySobek_User_Folder F
						where F.UserID=p_userid
						  and F.UserFolderID=mySobek_User_Item.UserFolderID
						  and FolderName != 'Submitted Items' );
	end if;
END;
$$;


-- Delete an item from the user's folder
CREATE OR REPLACE FUNCTION mySobek_Delete_Item_From_User_Folder(
	p_userid integer,
	p_foldername varchar(255),
	p_bibid varchar(10),
	p_vid varchar(5)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_itemid integer;
	v_userfolderid integer;
BEGIN
	if (( select COUNT(*) from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = p_bibid and I.VID = p_vid ) = 1 ) then
		select ItemID into v_itemid from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = p_bibid and I.VID = p_vid;

		if (( select count(*) from mySobek_User_Folder where UserID=p_userid and FolderName=p_foldername) > 0 ) then
			select UserFolderID into v_userfolderid from mySobek_User_Folder where UserID=p_userid and FolderName=p_foldername;
			delete from mySobek_User_Item where UserFolderID=v_userfolderid and ItemID=v_itemid;
		end if;
	end if;
END;
$$;


-- Fully removes an existing user
CREATE OR REPLACE FUNCTION mySobek_Delete_User(
	p_username varchar(20)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_userid integer;
BEGIN
	if ( exists ( select 1 from mySobek_User where UserName=p_username )) then
		select UserID into v_userid from mySobek_User where Username=p_username;

		delete from mySobek_User_Bib_Link where UserID=v_userid;
		delete from mySobek_User_DefaultMetadata_Link where UserID=v_userid;
		delete from mySobek_User_Description_Tags where UserID=v_userid;
		delete from mySobek_User_Edit_Aggregation where UserID=v_userid;
		delete from mySobek_User_Editable_Link where UserID=v_userid;

		delete from mySobek_User_Item_Link where UserID=v_userid;
		delete from mySobek_User_Item_Permissions where UserID=v_userid;
		delete from mySobek_User_Search where UserID=v_userid;
		delete from mySobek_User_Settings where UserID=v_userid;
		delete from mySobek_User_Template_Link where UserID=v_userid;
		delete from mySobek_User_Group_Link where UserID=v_userid;

		delete from mySobek_User_Item where UserFolderID in ( select UserFolderID from mySobek_User_Folder where UserID=v_userid);
		delete from mySobek_User_Folder where UserID=v_userid;

		delete from mySobek_User where UserID=v_userid;
	end if;
END;
$$;


-- Delete a user's folder
CREATE OR REPLACE FUNCTION mySobek_Delete_User_Folder(
	p_userfolderid integer,
	p_userid integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if ((select count(*) from mySobek_User_Folder where userfolderid=p_userfolderid and userid=p_userid) > 0 ) then
		if (( select count(*) from mySobek_User_Folder where ParentFolderID = p_userfolderid ) <= 0 ) then
			DELETE FROM mySobek_User_Folder
			where UserID=p_userid and UserFolderID=p_userfolderid;
		end if;
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION mySobek_Delete_User_Group(
	p_usergroupid integer,
	OUT p_message integer
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ( exists ( select 1 from mySobek_User_Group_Link where UserGroupID=p_usergroupid )) then
		p_message := -1;
	elsif ( exists ( select 1 from mySobek_User_Group where UserGroupID=p_usergroupid and isSpecialGroup = 'true' )) then
		p_message := -2;
	else
		delete from mySobek_User_Group_DefaultMetadata_Link where UserGroupID=p_usergroupid;
		delete from mySobek_User_Group_Edit_Aggregation where UserGroupID=p_usergroupid;
		delete from mySobek_User_Group_Item_Permissions where UserGroupID=p_usergroupid;
		delete from mySobek_User_Group_Editable_Link where UserGroupID=p_usergroupid;
		delete from mySobek_User_Group_Template_Link where UserGroupID=p_usergroupid;
		delete from mySobek_User_Group where UserGroupID = p_usergroupid;

		p_message := 1;
	end if;
END;
$$;


-- Delete a saved search
CREATE OR REPLACE FUNCTION mySobek_Delete_User_Search(
	p_usersearchid integer
)
RETURNS void
LANGUAGE sql
AS $$
	delete from mySobek_User_Search
	where UserSearchID=p_usersearchid;
$$;


-- Edit a user's folder information
CREATE OR REPLACE FUNCTION mySobek_Edit_User_Folder(
	p_userfolderid integer,
	p_userid integer,
	p_parentfolderid integer,
	p_foldername varchar(255),
	p_is_public boolean,
	p_description varchar(4000),
	OUT p_new_folder_id integer
)
LANGUAGE plpgsql
AS $$
BEGIN
	-- Does this reference an existing folder?
	if ( p_userfolderid > 0 ) then
		update mySobek_User_Folder
		set FolderName=p_foldername, isPublic=p_is_public, FolderDescription=p_description
		where UserFolderID=p_userfolderid and UserID=p_userid;

		p_new_folder_id := p_userfolderid;
	else
		-- Ensure a folder of the same name does not exist
		if (( select count(*) from mySobek_User_Folder where UserID=p_userid and FolderName=p_foldername ) > 0 ) then
			update mySobek_User_Folder
			set FolderName=p_foldername, isPublic=p_is_public, FolderDescription=p_description
			where FolderName=p_foldername and UserID=p_userid;

			select UserFolderID into p_new_folder_id from mySobek_User_Folder where FolderName=p_foldername and UserID=p_userid;
		else
			-- Add this as a new folder
			if ( p_parentfolderid < 0 ) then
				insert into mySobek_User_Folder( UserID, FolderName, isPublic, FolderDescription )
				values ( p_userid, p_foldername, p_is_public, p_description )
				returning UserFolderID into p_new_folder_id;
			else
				insert into mySobek_User_Folder( UserID, FolderName, isPublic, FolderDescription, ParentFolderID )
				values ( p_userid, p_foldername, p_is_public, p_description, p_parentfolderid )
				returning UserFolderID into p_new_folder_id;
			end if;
		end if;
	end if;
END;
$$;

-- Originally returned 2 result sets (default metadata list, then template list); ported using
-- OUT refcursor parameters -- see the note on mySobek_Get_User_By_UserID below for why.
CREATE OR REPLACE FUNCTION mySobek_Get_All_Template_DefaultMetadatas(
	OUT cur_metadata refcursor,
	OUT cur_templates refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_metadata FOR
		select MetadataCode, MetadataName, Description, UserID
		from mySobek_DefaultMetadata
		order by MetadataCode;

	OPEN cur_templates FOR
		select TemplateCode, TemplateName, Description
		from mySobek_Template
		order by TemplateCode;
END;
$$;


CREATE OR REPLACE FUNCTION mySobek_Get_All_User_Groups()
RETURNS TABLE (
	UserGroupID integer,
	GroupName varchar(100),
	GroupDescription varchar(1000),
	UserCount bigint,
	IsSpecialGroup boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	with linked_users_cte ( UserGroupID, UserCount ) AS
	(
		select UserGroupID, count(*)
		from mySobek_User_Group_Link
		group by UserGroupID
	)
	select G.UserGroupID, G.GroupName, G.GroupDescription, coalesce(U.UserCount,0) as UserCount, G.IsSpecialGroup
	from mySobek_User_Group G
	     left outer join linked_users_cte U on U.UserGroupID=G.UserGroupID
	order by IsSpecialGroup, GroupName;
END;
$$;


-- Procedure gets settings across all the users that are like the key start
--
-- Since this uses like, you can pass in a string like 'TEI.%' and that will return
-- all the values that have a setting key that STARTS with 'TEI.'
--
-- If p_value is NULL, then all settings that match are returned.  If a value is
-- provided for p_value, then only the settings that match the key search and
-- have the same value in the database as p_value are returned.  This is particularly
-- useful for boolean settings, where you only want to the see the settings set to 'true'
CREATE OR REPLACE FUNCTION mySobek_Get_All_User_Settings_Like(
	p_keyStart varchar(255),
	p_value text
)
RETURNS TABLE (
	UserName varchar(50),
	UserID integer,
	FirstName varchar(50),
	LastName varchar(50),
	Setting_Key varchar(255),
	Setting_Value text
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ( p_value is null ) then
		RETURN QUERY
		select U.UserName, U.UserID, coalesce(U.FirstName,'') as FirstName, coalesce(U.LastName,'') as LastName, S.Setting_Key, S.Setting_Value
		from mySobek_User U, mySobek_User_Settings S
		where ( U.UserID = S.UserID )
		  and ( S.Setting_Key like p_keyStart )
		  and ( U.isActive = 'true' );
	else
		RETURN QUERY
		select U.UserName, U.UserID, coalesce(U.FirstName,'') as FirstName, coalesce(U.LastName,'') as LastName, S.Setting_Key, S.Setting_Value
		from mySobek_User U, mySobek_User_Settings S
		where ( U.UserID = S.UserID )
		  and ( S.Setting_Key like p_keyStart )
		  and ( U.isActive = 'true' )
		  and ( S.Setting_Value = p_value );
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION mySobek_Get_All_Users()
RETURNS TABLE (
	UserID integer,
	Full_Name text,
	UserName varchar(50),
	EmailAddress varchar(100),
	PendingRequests bigint
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
	select U.UserID, U.LastName || ', ' || U.FirstName AS Full_Name, U.UserName, U.EmailAddress, coalesce(R.PendingRequests,0) as PendingRequests
	from mySobek_User U left join
		 pending_cte R on U.UserID = R.UserID
	order by Full_Name;
END;
$$;


-- Get all the information about a folder, by folder id
CREATE OR REPLACE FUNCTION mySobek_Get_Folder_Information(
	p_folderid integer
)
RETURNS TABLE (
	UserFolderID integer,
	FolderName varchar(255),
	isPublic boolean,
	FolderDescription varchar(4000),
	UserID integer,
	FirstName varchar(50),
	LastName varchar(50),
	NickName varchar(50),
	EmailAddress varchar(100)
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	select F.UserFolderID, F.FolderName, F.isPublic, F.FolderDescription, U.UserID, U.FirstName, U.LastName, U.NickName, U.EmailAddress
	from mySobek_User_Folder F, mySobek_User U
	where ( F.UserFolderID=p_folderid )
	  and ( U.UserID = F.UserID );
END;
$$;


-- Get overall information about folders and searches for this user.
-- Originally returned 2 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION mySobek_Get_Folder_Search_Information(
	p_userid integer,
	OUT cur_folders refcursor,
	OUT cur_search_count refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_folders FOR
		select F.UserFolderID, coalesce(F.ParentFolderID,-1) as ParentFolderID, F.FolderName, F.isPublic, (select count(*) from mySobek_User_Item I where I.UserFolderID=F.UserFolderID ) as Item_Count
		from mySobek_User_Folder F
		where UserID=p_userid;

	OPEN cur_search_count FOR
		select count(*) as Search_Count
		from mySobek_User_Search
		where UserID=p_userid;
END;
$$;


-- Stored procedure used to return standard data across all user fetch stored procedures.
-- Originally returned 9 result sets (via 9 top-level SELECTs) plus performed a final UPDATE;
-- ported using OUT refcursor parameters -- one per original SELECT, in the same order, plus
-- the trailing UPDATE preserved as a side effect. Every C# call site that used to read
-- DataSet.Tables[0..8] keeps working unchanged: EalDbAccess.cs's Postgres ExecuteDataset path
-- detects the all-refcursor result shape and FETCHes each cursor into its own DataTable.
CREATE OR REPLACE FUNCTION mySobek_Get_User_By_UserID(
	p_userid integer,
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
	  AuthenticationSource
	from mySobek_User U
	where ( UserID = p_userid ) and ( isActive = 'true' );

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


CREATE OR REPLACE FUNCTION mySobek_Get_User_By_External_Login(
	p_provider_code varchar(50),
	p_external_subject_id varchar(450),
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
DECLARE
	v_userid integer;
BEGIN
	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	if (( select COUNT(*) from mySobek_User where ExternalProviderCode=p_provider_code and ExternalSubjectId=p_external_subject_id and isActive = 'true' ) = 1 ) then
		select UserID into v_userid from mySobek_User where ExternalProviderCode=p_provider_code and ExternalSubjectId=p_external_subject_id and isActive = 'true';

		-- Stored procedure used to return standard data across all user fetch stored procedures
		SELECT r.cur_user, r.cur_templates, r.cur_default_metadata, r.cur_submitted_bibids, r.cur_editable_regex, r.cur_aggregations, r.cur_folders, r.cur_folder_items, r.cur_user_groups, r.cur_settings
		INTO cur_user, cur_templates, cur_default_metadata, cur_submitted_bibids, cur_editable_regex, cur_aggregations, cur_folders, cur_folder_items, cur_user_groups, cur_settings
		FROM mySobek_Get_User_By_UserID(v_userid) r;
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION mySobek_Get_User_By_ShibbID(
	p_shibbid char(8),
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
DECLARE
	v_userid integer;
BEGIN
	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	if (( select COUNT(*) from mySobek_User where ShibbID=p_shibbid and isActive = 'true' ) = 1 ) then
		select UserID into v_userid from mySobek_User where ShibbID=p_shibbid and isActive = 'true';

		SELECT r.cur_user, r.cur_templates, r.cur_default_metadata, r.cur_submitted_bibids, r.cur_editable_regex, r.cur_aggregations, r.cur_folders, r.cur_folder_items, r.cur_user_groups, r.cur_settings
		INTO cur_user, cur_templates, cur_default_metadata, cur_submitted_bibids, cur_editable_regex, cur_aggregations, cur_folders, cur_folder_items, cur_user_groups, cur_settings
		FROM mySobek_Get_User_By_UserID(v_userid) r;
	end if;
END;
$$;


-- Gets all the user information by the username.  Hashed password will be compared in
-- the database routines (and possibly flagged to be replaced with new hash)
CREATE OR REPLACE FUNCTION mySobek_Get_User_By_UserName(
	p_username varchar(100),
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
DECLARE
	v_userid integer;
	v_userid2 integer;
BEGIN
	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	if (( select COUNT(*) from mySobek_User where UserName=p_username and isActive = 'true' ) = 1 ) then
		select UserID into v_userid from mySobek_User where UserName=p_username and isActive = 'true';

		SELECT r.cur_user, r.cur_templates, r.cur_default_metadata, r.cur_submitted_bibids, r.cur_editable_regex, r.cur_aggregations, r.cur_folders, r.cur_folder_items, r.cur_user_groups, r.cur_settings
		INTO cur_user, cur_templates, cur_default_metadata, cur_submitted_bibids, cur_editable_regex, cur_aggregations, cur_folders, cur_folder_items, cur_user_groups, cur_settings
		FROM mySobek_Get_User_By_UserID(v_userid) r;

	-- Look for current user by email and hashed password...
	elsif (( select COUNT(*) from mySobek_User where EmailAddress=p_username and isActive = 'true' ) = 1 ) then
		select UserID into v_userid2 from mySobek_User where EmailAddress=p_username and isActive = 'true';

		SELECT r.cur_user, r.cur_templates, r.cur_default_metadata, r.cur_submitted_bibids, r.cur_editable_regex, r.cur_aggregations, r.cur_folders, r.cur_folder_items, r.cur_user_groups, r.cur_settings
		INTO cur_user, cur_templates, cur_default_metadata, cur_submitted_bibids, cur_editable_regex, cur_aggregations, cur_folders, cur_folder_items, cur_user_groups, cur_settings
		FROM mySobek_Get_User_By_UserID(v_userid2) r;
	end if;
END;
$$;


-- Get list of items in a user's folder
CREATE OR REPLACE FUNCTION mySobek_Get_User_Folder_Items(
	p_userid integer,
	p_foldername varchar(255)
)
RETURNS TABLE (
	BibID varchar(10),
	VID varchar(5),
	ItemOrder integer,
	SortDate bigint,
	UserNotes varchar(2000)
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_folderid integer;
BEGIN
	select coalesce(UserFolderID,-1) into v_folderid from mySobek_User_Folder where UserID=p_userid and FolderName=p_foldername;

	RETURN QUERY
	select G.BibID, I.VID, A.ItemOrder, coalesce( I.SortDate,-1), coalesce(A.UserNotes,'' )
	from mySobek_User_Item A, SobekCM_Item I, SobekCM_Item_Group G
	where ( I.ItemID = A.ItemID )
	  and ( I.GroupID = G.GroupID )
	  and ( A.UserFolderID = v_folderid )
	  and ( I.Deleted = 'false' )
	  and ( G.Deleted = 'false' );
END;
$$;


-- Get information about a single user group, by user group id.
-- Originally returned 5 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION mySobek_Get_User_Group(
	p_usergroupid integer,
	OUT cur_group refcursor,
	OUT cur_templates refcursor,
	OUT cur_default_metadata refcursor,
	OUT cur_editable_regex refcursor,
	OUT cur_aggregations refcursor,
	OUT cur_users refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	-- Get the basic user group information
	OPEN cur_group FOR
	select *
	from mySobek_User_Group G
	where ( G.UserGroupID = p_usergroupid );

	-- Get the templates
	OPEN cur_templates FOR
	select T.TemplateCode, T.TemplateName
	from mySobek_Template T, mySobek_User_Group_Template_Link TL
	where ( TL.UserGroupID = p_usergroupid ) and ( TL.TemplateID = T.TemplateID );

	-- Get the default metadata
	OPEN cur_default_metadata FOR
	select P.MetadataCode, P.MetadataName
	from mySobek_DefaultMetadata P, mySobek_User_Group_DefaultMetadata_Link PL
	where ( PL.UserGroupID = p_usergroupid ) and ( PL.DefaultMetadataID = P.DefaultMetadataID );

	-- Get the regular expression for editable items
	OPEN cur_editable_regex FOR
	select R.EditableRegex, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete
	from mySobek_Editable_Regex R, mySobek_User_Group_Editable_Link L
	where ( L.UserGroupID = p_usergroupid ) and ( L.EditableID = R.EditableID );

	-- Get the list of aggregations associated with this user
	OPEN cur_aggregations FOR
	select A.Code, A.Name, L.CanSelect, L.CanEditItems, L.IsCurator, L.CanEditMetadata, L.CanEditBehaviors, L.CanPerformQc, L.CanUploadFiles, L.CanChangeVisibility, L.CanDelete, L.IsAdmin
	from SobekCM_Item_Aggregation A, mySobek_User_Group_Edit_Aggregation L
	where  ( L.AggregationID = A.AggregationID ) and ( L.UserGroupID = p_usergroupid );

	-- Get the list of all user's linked to this user group
	OPEN cur_users FOR
	select U.UserID, U.UserName, U.EmailAddress, U.FirstName, U.NickName, U.LastName
	from mySobek_User U, mySobek_User_Group_Link L
	where ( L.UserGroupID = p_usergroupid )
	  and ( L.UserID = U.UserID );
END;
$$;


-- Get list of saved searches
CREATE OR REPLACE FUNCTION mySobek_Get_User_Searches(
	p_userid integer
)
RETURNS SETOF mySobek_User_Search
LANGUAGE sql
AS $$
	select *
	from mySobek_User_Search
	where UserID = p_userid
	order by ItemOrder, DateAdded;
$$;


CREATE OR REPLACE FUNCTION mySobek_Link_User_To_User_Group(
	p_userid integer,
	p_usergroupid integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if (( select COUNT(*) from mySobek_User_Group_Link where UserID=p_userid and UserGroupID = p_usergroupid ) = 0 ) then
		insert into mySobek_User_Group_Link ( UserGroupID, UserID )
		values ( p_usergroupid, p_userid );
	end if;
END;
$$;

CREATE OR REPLACE FUNCTION mySobek_Permissions_Report()
RETURNS TABLE (
	GroupName varchar(150),
	UserID integer,
	UserName varchar(50),
	EmailAddress varchar(100),
	FirstName varchar(50),
	LastName varchar(50),
	Nickname varchar(50),
	DateCreated timestamp,
	LastActivity timestamp,
	isActive boolean,
	Can_Edit_All_Items text,
	Internal_User boolean,
	Can_Delete_All_Items boolean,
	IsPortalAdmin boolean,
	IsSystemAdmin boolean,
	IsHostAdmin boolean,
	IsUserAdmin boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	-- Return the top-level permissions (non-aggregation specific)
	select '' as GroupName, U.UserID, U.UserName, U.EmailAddress, U.FirstName, U.LastName, U.Nickname, U.DateCreated, U.LastActivity, U.isActive,
		case when e.UserID is null then 'false' else 'true' end as Can_Edit_All_Items,
		U.Internal_User, U.Can_Delete_All_Items, U.IsPortalAdmin, U.IsSystemAdmin, U.IsHostAdmin, U.IsUserAdmin
	from mySobek_User as U left outer join
		 mySobek_User_Editable_Link as E on E.UserID = U.UserID and E.EditableID = 1
	where      ( U.IsSystemAdmin = 'true' )
			or ( U.IsPortalAdmin = 'true' )
			or ( U.Can_Delete_All_Items = 'true' )
			or ( U.IsHostAdmin = 'true' )
			or ( U.Internal_User = 'true' )
	union
	select G.GroupName, U.UserID, U.UserName, U.EmailAddress, U.FirstName, U.LastName, U.Nickname, U.DateCreated, U.LastActivity, U.isActive,
		case when e.UserGroupID is null then 'false' else 'true' end as Can_Edit_All_Items,
		G.Internal_User, G.Can_Delete_All_Items, G.IsPortalAdmin, G.IsSystemAdmin, false, false
	from mySobek_User as U inner join
		 mySobek_User_Group_Link as L on U.UserID = L.UserID inner join
		 mySobek_User_Group as G on G.UserGroupID = L.UserGroupID left outer join
		 mySobek_User_Group_Editable_Link as E on E.UserGroupID = G.UserGroupID and E.EditableID = 1
	where      ( G.IsSystemAdmin = 'true' )
			or ( G.IsPortalAdmin = 'true' )
			or ( G.Can_Delete_All_Items = 'true' )
			or ( G.Internal_User = 'true' )
	order by LastName ASC, FirstName ASC, GroupName ASC;
END;
$$;


CREATE OR REPLACE FUNCTION mySobek_Permissions_Report_Aggregation(
	p_Code varchar(20)
)
RETURNS TABLE (
	GroupDefined boolean,
	GroupName varchar(150),
	UserGroupID integer,
	UserID integer,
	UserName varchar(50),
	EmailAddress varchar(100),
	FirstName varchar(50),
	LastName varchar(50),
	Nickname varchar(50),
	DateCreated timestamp,
	LastActivity timestamp,
	isActive boolean,
	CanSelect boolean,
	CanEditItems boolean,
	IsAggregationAdmin boolean,
	IsCollectionManager boolean,
	CanEditMetadata boolean,
	CanEditBehaviors boolean,
	CanPerformQc boolean,
	CanUploadFiles boolean,
	CanChangeVisibility boolean,
	CanDelete boolean
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_aggrId integer;
BEGIN
	v_aggrId := -1;
	if ( exists ( select 1 from SobekCM_Item_Aggregation where Code=p_Code )) then
		select AggregationID into v_aggrId from SobekCM_Item_Aggregation where Code=p_Code;
	end if;

	RETURN QUERY
	select false as GroupDefined, '' as GroupName, -1 as UserGroupID, U.UserID, U.UserName, U.EmailAddress, U.FirstName, U.LastName, U.Nickname, U.DateCreated, U.LastActivity, U.isActive,
		   P.CanSelect, P.CanEditItems, P.IsAdmin AS IsAggregationAdmin, P.IsCurator AS IsCollectionManager, P.CanEditMetadata, P.CanEditBehaviors, P.CanPerformQc, P.CanUploadFiles, P.CanChangeVisibility, P.CanDelete
	from mySobek_User U, mySobek_User_Edit_Aggregation P
	where ( U.UserID=P.UserID )
	  and ( P.AggregationID=v_aggrId )
	  and (    ( P.CanSelect = 'true' ) or ( P.CanEditItems = 'true' ) or ( P.IsAdmin = 'true' ) or ( P.IsCurator ='true' ) or ( P.CanEditMetadata = 'true' )
	        or ( P.CanEditBehaviors = 'true' ) or ( P.CanPerformQc = 'true' ) or ( P.CanUploadFiles = 'true' ) or ( P.CanChangeVisibility = 'true' ) or ( P.CanDelete = 'true' ))
	union
	select true as GroupDefined, G.GroupName, G.UserGroupID, U.UserID, U.UserName, U.EmailAddress, U.FirstName, U.LastName, U.Nickname, U.DateCreated, U.LastActivity, U.isActive,
		   P.CanSelect, P.CanEditItems, P.IsAdmin AS IsAggregationAdmin, P.IsCurator AS IsCollectionManager, P.CanEditMetadata, P.CanEditBehaviors, P.CanPerformQc, P.CanUploadFiles, P.CanChangeVisibility, P.CanDelete
	from mySobek_User U, mySobek_User_Group_Link L, mySobek_User_Group G, mySobek_User_Group_Edit_Aggregation P
	where ( U.UserID=L.UserID )
	  and ( L.UserGroupID=G.UserGroupID )
	  and ( G.UserGroupID=P.UserGroupID )
	  and ( P.AggregationID=v_aggrId )
	order by LastName ASC, FirstName ASC;
END;
$$;


CREATE OR REPLACE FUNCTION mySobek_Permissions_Report_Aggregation_Links()
RETURNS TABLE (
	UserID integer,
	UserName varchar(50),
	EmailAddress varchar(100),
	FirstName varchar(50),
	LastName varchar(50),
	Nickname varchar(50),
	DateCreated timestamp,
	LastActivity timestamp,
	isActive boolean,
	UserPermissioned text,
	GroupPermissioned text
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
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
	UserAgg as ( select UserID, string_agg(Code, ', ' ORDER BY Code ASC) as UserPermissioned from UserLevel group by UserID ),
	GroupAgg as ( select UserID, string_agg(Code, ', ' ORDER BY Code ASC) as GroupPermissioned from GroupLevel group by UserID )
	select U.UserID, U.UserName, U.EmailAddress, U.FirstName, U.LastName, U.Nickname, U.DateCreated, U.LastActivity, U.isActive,
	       coalesce(UA.UserPermissioned, '') as UserPermissioned,
	       coalesce(GA.GroupPermissioned, '') as GroupPermissioned
	from mySobek_User U inner join
	     ( select UserID from UserAgg union select UserID from GroupAgg ) AllUsers on AllUsers.UserID = U.UserID left outer join
	     UserAgg UA on UA.UserID = U.UserID left outer join
	     GroupAgg GA on GA.UserID = U.UserID
	order by U.LastName ASC, U.FirstName ASC;
END;
$$;


-- Get the list of aggregations that have special rights given to some users
CREATE OR REPLACE FUNCTION mySobek_Permissions_Report_Linked_Aggregations()
RETURNS TABLE (
	Code varchar(20),
	Name varchar(250),
	"Type" varchar(50)
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
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
$$;


CREATE OR REPLACE FUNCTION mySobek_Permissions_Report_Submission_Rights()
RETURNS TABLE (
	UserID integer,
	UserName varchar(50),
	EmailAddress varchar(100),
	FirstName varchar(50),
	LastName varchar(50),
	Nickname varchar(50),
	DateCreated timestamp,
	LastActivity timestamp,
	isActive boolean,
	Templates text,
	DefaultMetadatas text
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	-- Users who can submit items, either directly or via a group
	with SubmitUsers as (
		select UserID from mySobek_User where Can_Submit_Items = 'true'
		union
		select L.UserID
		from mySobek_User_Group_Link as L inner join
		     mySobek_User_Group as G on G.UserGroupID = L.UserGroupID
		where G.Can_Submit_Items = 'true'
	),
	TemplateAgg as (
		select L.UserID, string_agg(T.TemplateCode, ', ' ORDER BY T.TemplateCode ASC) as Templates
		from mySobek_User_Template_Link L inner join
		     mySobek_Template T on L.TemplateID = T.TemplateID
		group by L.UserID
	),
	DefaultMetadataAgg as (
		select L.UserID, string_agg(M.MetadataCode, ', ' ORDER BY M.MetadataCode ASC) as DefaultMetadatas
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
END;
$$;


-- Reset a user's password
CREATE OR REPLACE FUNCTION mySobek_Reset_User_Password(
	p_userid integer,
	p_password varchar(100),
	p_is_temporary boolean
)
RETURNS void
LANGUAGE sql
AS $$
	update mySobek_User
	set Password=p_password, isTemporary_Password=p_is_temporary
	where UserID = p_userid;
$$;


-- Add a new default metadata set to this database
CREATE OR REPLACE FUNCTION mySobek_Save_DefaultMetadata(
	p_metadata_code varchar(20),
	p_metadata_name varchar(100),
	p_description varchar(255),
	p_userid integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if (( select count(*) from mySobek_DefaultMetadata where MetadataCode=p_metadata_code ) > 0 ) then
		update mySobek_DefaultMetadata
		set Description = p_description, MetadataName = p_metadata_name
		where MetadataCode = p_metadata_code;
	else
		insert into mySobek_DefaultMetadata ( Description, MetadataCode, UserID, MetadataName )
		values ( p_description, p_metadata_code, p_userid, p_metadata_name );
	end if;
END;
$$;


-- Add a new template to this database
CREATE OR REPLACE FUNCTION mySobek_Save_Template(
	p_template_code varchar(20),
	p_template_name varchar(100),
	p_description varchar(255)
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if (( select count(*) from mySobek_Template where TemplateCode=p_template_code ) > 0 ) then
		update mySobek_Template
		set TemplateName = p_template_name, Description=p_description
		where TemplateCode = p_template_code;
	else
		insert into mySobek_Template ( TemplateName, TemplateCode, Description )
		values ( p_template_name, p_template_code, p_description );
	end if;
END;
$$;


-- Saves a user
CREATE OR REPLACE FUNCTION mySobek_Save_User(
	p_userid integer,
	p_shibbid char(8),
	p_username varchar(100),
	p_password varchar(100),
	p_emailaddress varchar(100),
	p_firstname varchar(100),
	p_lastname varchar(100),
	p_cansubmititems boolean,
	p_nickname varchar(100),
	p_organization varchar(250),
	p_college varchar(250),
	p_department varchar(250),
	p_unit varchar(250),
	p_rights varchar(1000),
	p_sendemail boolean,
	p_language varchar(50),
	p_default_template varchar(50),
	p_default_metadata varchar(50),
	p_organization_code varchar(15),
	p_receivestatsemail boolean,
	p_scanningtechnician boolean,
	p_processingtechnician boolean,
	p_internalnotes varchar(500),
	p_authentication varchar(20),
	p_external_provider_code varchar(50),
	p_external_subject_id varchar(450),
	p_authentication_source varchar(100)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_newuserid integer;
	v_templateid integer;
	v_projectid integer;
BEGIN
	if ( p_userid < 0 ) then
		insert into mySobek_User ( ShibbID, UserName, Password, EmailAddress, LastName, FirstName, DateCreated, LastActivity, isActive,  Note_Length, Can_Make_Folders_Public,
									isTemporary_Password, Can_Submit_Items, NickName, Organization, College, Department, Unit, Default_Rights, sendEmailOnSubmission, UI_Language,
									Internal_User, OrganizationCode, Receive_Stats_Emails, Include_Tracking_Standard_Forms, ScanningTechnician, ProcessingTechnician, InternalNotes,
									ExternalProviderCode, ExternalSubjectId, AuthenticationSource)
		values ( p_shibbid, p_username, p_password, p_emailaddress, p_lastname, p_firstname, now(), now(), 'true', 1000, 'true',
					'false', p_cansubmititems, p_nickname, p_organization, p_college, p_department, p_unit, p_rights, p_sendemail, p_language,
					'false', p_organization_code, p_receivestatsemail, 'false', p_scanningtechnician, p_processingtechnician, p_internalnotes,
					p_external_provider_code, p_external_subject_id, p_authentication_source)
		returning UserID into v_newuserid;

		-- This is a brand new user, so we must set the default groups, according to
		-- the authentication method
		-- Authentticated used the built-in Sobek authentication
		if (( p_authentication='sobek' ) and (( select count(*) from mySobek_user_Group where IsSobekDefault = 'true' ) > 0 )) then
			insert into mySobek_User_Group_Link ( UserID, UserGroupID )
			select v_newuserid, UserGroupID
			from mySobek_User_Group where IsSobekDefault='true';
		end if;

		-- Authenticated using Shibboleth authentication
		if (( p_authentication='shibboleth' ) and (( select count(*) from mySobek_user_Group where IsShibbolethDefault = 'true' ) > 0 )) then
			insert into mySobek_User_Group_Link ( UserID, UserGroupID )
			select v_newuserid, UserGroupID
			from mySobek_User_Group where IsShibbolethDefault='true';
		end if;

		-- Authenticated using Ldap authentication
		if (( p_authentication='ldap' ) and (( select count(*) from mySobek_user_Group where IsLdapDefault = 'true' ) > 0 )) then
			insert into mySobek_User_Group_Link ( UserID, UserGroupID )
			select v_newuserid, UserGroupID
			from mySobek_User_Group where IsLdapDefault='true';
		end if;
	else
		update mySobek_User
		set EmailAddress=p_emailaddress,
			Firstname = p_firstname, Lastname = p_lastname, Can_Submit_Items = p_cansubmititems,
			NickName = p_nickname, Organization=p_organization, College=p_college, Department=p_department,
			Unit=p_unit, Default_Rights=p_rights, sendEmailOnSubmission = p_sendemail, UI_Language=p_language,
			OrganizationCode=p_organization_code, Receive_Stats_Emails=p_receivestatsemail,
			ScanningTechnician=p_scanningtechnician, ProcessingTechnician=p_processingtechnician,
			InternalNotes=p_internalnotes
		where UserID = p_userid;

		-- Set the default template
		if ( length( p_default_template ) > 0 ) then
			select TemplateID into v_templateid from mySobek_Template where TemplateCode=p_default_template;

			update mySobek_User_Template_Link set DefaultTemplate = 'false' where UserID=p_userid;

			if (( select count(*) from mySobek_User_Template_Link where UserID=p_userid and TemplateID=v_templateid ) > 0 ) then
				update mySobek_User_Template_Link set DefaultTemplate = 'true' where UserID=p_userid and TemplateID=v_templateid;
			else
				insert into mySobek_User_Template_Link ( UserID, TemplateID, DefaultTemplate ) values ( p_userid, v_templateid, 'true' );
			end if;
		end if;

		-- Set the default metadata
		if ( length( p_default_metadata ) > 0 ) then
			select DefaultMetadataID into v_projectid from mySobek_DefaultMetadata where MetadataCode=p_default_metadata;

			update mySobek_User_DefaultMetadata_Link set CurrentlySelected = 'false' where UserID=p_userid;

			if (( select count(*) from mySobek_User_DefaultMetadata_Link where UserID=p_userid and DefaultMetadataID=v_projectid ) > 0 ) then
				update mySobek_User_DefaultMetadata_Link set CurrentlySelected = 'true' where UserID=p_userid and DefaultMetadataID=v_projectid;
			else
				insert into mySobek_User_DefaultMetadata_Link ( UserID, DefaultMetadataID, CurrentlySelected ) values ( p_userid, v_projectid, 'true' );
			end if;
		end if;
	end if;
END;
$$;

-- Saves information about a single user group.
-- DEVIATION FROM ORIGINAL: the source T-SQL never assigned @new_usergroupid on the
-- update-existing-group branch at all (a real bug -- callers updating an existing group would
-- get back an uninitialized/NULL output parameter). Every comparable Save_* proc in this
-- codebase (e.g. mySobek_Save_User_Search) does return an id on its update branch, so rather
-- than faithfully reproduce this one as a silent trap, p_new_usergroupid is explicitly set to
-- p_usergroupid here.
CREATE OR REPLACE FUNCTION mySobek_Save_User_Group(
	p_usergroupid integer,
	p_groupname varchar(150),
	p_groupdescription varchar(1000),
	p_can_submit_items boolean,
	p_is_internal boolean,
	p_can_edit_all boolean,
	p_is_system_admin boolean,
	p_is_portal_admin boolean,
	p_include_tracking_standard_forms boolean,
	p_clear_metadata_templates boolean,
	p_clear_aggregation_links boolean,
	p_clear_editable_links boolean,
	p_is_sobek_default boolean,
	p_is_shibboleth_default boolean,
	p_is_ldap_default boolean,
	OUT p_new_usergroupid integer
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ( p_usergroupid < 0 ) then
		insert into mySobek_User_Group ( GroupName, GroupDescription, Can_Submit_Items, Internal_User, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms, IsSobekDefault, IsShibbolethDefault, IsLdapDefault  )
		values ( p_groupname, p_groupdescription, p_can_submit_items, p_is_internal, p_is_system_admin, p_is_portal_admin, p_include_tracking_standard_forms, p_is_sobek_default, p_is_shibboleth_default, p_is_ldap_default )
		returning UserGroupID into p_new_usergroupid;
	else
		update mySobek_User_Group
		set GroupName = p_groupname, GroupDescription = p_groupdescription, Can_Submit_Items = p_can_submit_items, Internal_User=p_is_internal, IsSystemAdmin=p_is_system_admin, IsPortalAdmin=p_is_portal_admin, Include_Tracking_Standard_Forms=p_include_tracking_standard_forms,
			IsSobekDefault=p_is_sobek_default, IsShibbolethDefault=p_is_shibboleth_default, IsLdapDefault=p_is_ldap_default
		where UserGroupID = p_usergroupid;

		p_new_usergroupid := p_usergroupid;
	end if;

	-- Check the flag to edit all items
	if ( p_can_edit_all ) then
		if ( ( select count(*) from mySobek_User_Group_Editable_Link where EditableID=1 and UserGroupID=p_usergroupid ) = 0 ) then
			insert into mySobek_User_Group_Editable_Link ( UserGroupID, EditableID )
			values ( p_usergroupid, 1 );
		end if;
	else
		delete from mySobek_User_Group_Editable_Link where EditableID = 1 and UserGroupID=p_usergroupid;
	end if;

	-- Clear the projects/templates
	if ( p_clear_metadata_templates ) then
		delete from mySobek_User_Group_DefaultMetadata_Link where UserGroupID=p_usergroupid;
		delete from mySobek_User_Group_Template_Link where UserGroupID=p_usergroupid;
	end if;

	-- Clear the aggregations link
	if ( p_clear_aggregation_links ) then
		delete from mySobek_User_Group_Edit_Aggregation where UserGroupID=p_usergroupid;
	end if;

	-- Clear the editable link
	if ( p_clear_editable_links ) then
		delete from mySobek_User_Group_Editable_Link where UserGroupID=p_usergroupid;
	end if;
END;
$$;


-- Add a search to the user's list of saved searches
CREATE OR REPLACE FUNCTION mySobek_Save_User_Search(
	p_userid integer,
	p_searchurl varchar(500),
	p_searchdescription varchar(500),
	p_itemorder integer,
	p_usernotes varchar(2000),
	OUT p_new_usersearchid integer
)
LANGUAGE plpgsql
AS $$
BEGIN
	if (( select count(*) from mySobek_User_Search where UserID=p_userid and SearchURL=p_searchurl ) > 0 ) then
		update mySobek_User_Search
		set ItemOrder=p_itemorder, UserNotes=p_usernotes
		where UserID=p_userid and SearchURL=p_searchurl;

		p_new_usersearchid := -1;
	else
		insert into mySobek_User_Search( UserID, SearchURL, SearchDescription, ItemOrder, UserNotes, DateAdded )
		values ( p_userid, p_searchurl, p_searchdescription, p_itemorder, p_usernotes, now())
		returning UserSearchID into p_new_usersearchid;
	end if;
END;
$$;


-- Set aggregation home page flag
CREATE OR REPLACE FUNCTION mySobek_Set_Aggregation_Home_Page_Flag(
	p_userid integer,
	p_aggregationid integer,
	p_onhomepage boolean
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if ( ( select count(*) from mySobek_User_Edit_Aggregation where UserID=p_userid and AggregationID=p_aggregationid ) > 0 ) then
		update mySobek_User_Edit_Aggregation
		set OnHomePage=p_onhomepage
		where UserID = p_userid and AggregationID = p_aggregationid;

		delete from mySobek_User_Edit_Aggregation
		where CanSelect='false' and CanEditItems='false' and IsCurator='false' and OnHomePage='false';
	else
		insert into mySobek_User_Edit_Aggregation ( UserID, AggregationID, CanSelect, CanEditItems, IsCurator, OnHomePage )
		values ( p_userid, p_aggregationid, 'false', 'false', 'false', p_onhomepage );
	end if;
END;
$$;


-- Procedure allows an admin to edit permissions flags for this user
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
	p_clear_user_groups boolean
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
		IsUserAdmin=p_is_user_admin
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


-- Checks to see if the username or email exists
CREATE OR REPLACE FUNCTION mySobek_UserName_Exists(
	p_username varchar(100),
	p_email varchar(100),
	OUT p_username_exists boolean,
	OUT p_email_exists boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ( ( select count(*) from mySobek_User where UserName = p_username ) = 0 ) then
		p_username_exists := false;
	else
		p_username_exists := true;
	end if;

	if ( ( select count(*) from mySobek_User where EmailAddress = p_email ) = 0 ) then
		p_email_exists := false;
	else
		p_email_exists := true;
	end if;
END;
$$;


-- View all of a single user's tag
CREATE OR REPLACE FUNCTION mySobek_View_All_User_Tags(
	p_UserID integer
)
RETURNS TABLE (
	TagID integer,
	BibID varchar(10),
	VID varchar(5),
	Description_Tag varchar(2000),
	Date_Modified timestamp,
	UserID integer,
	FirstName varchar(50),
	NickName varchar(50),
	LastName varchar(50)
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ( p_UserID < 0 ) then
		RETURN QUERY
		select T.TagID, G.BibID, I.VID, T.Description_Tag, T.Date_Modified, U.UserID, U.FirstName, U.NickName, U.LastName
		from mySobek_User_Description_Tags T, mySobek_User U, SobekCM_Item I, SobekCM_Item_Group G
		where T.UserID=U.UserID
		  and T.ItemID = I.ItemID
		  and I.GroupID = G.GroupID;
	else
		RETURN QUERY
		select T.TagID, G.BibID, I.VID, T.Description_Tag, T.Date_Modified, U.UserID, U.FirstName, U.NickName, U.LastName
		from mySobek_User_Description_Tags T, mySobek_User U, SobekCM_Item I, SobekCM_Item_Group G
		where T.UserID=U.UserID
		  and T.UserID=p_UserID
		  and T.ItemID = I.ItemID
		  and I.GroupID = G.GroupID;
	end if;
END;
$$;


-- This procedure adds a new external record number to an existing item
CREATE OR REPLACE FUNCTION SobekCM_Add_External_Record_Number(
	p_groupID integer,
	p_extRecordValue varchar(50),
	p_extRecordType varchar(25)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_extRecordTypeID integer;
	v_extRecordLinkID integer;
BEGIN
	-- Look for an existing record type
	select coalesce(extRecordTypeID, -1) into v_extRecordTypeID
	from SobekCM_External_Record_Type
	where (extRecordType = p_extRecordType);

	-- Was this a new record type
	if ( coalesce( v_extRecordTypeID, -1 ) < 0 ) then
		insert into SobekCM_External_Record_Type ( ExtRecordType, repeatableTypeFlag )
		values ( p_extRecordType, 1 )
		returning extRecordTypeID into v_extRecordTypeID;
	end if;

	-- The linkID parameter is less than zero; query the database
	-- to see if one exists for this record type.
	select coalesce( extRecordLinkID, -1 ) into v_extRecordLinkID
	from SobekCM_Item_Group_External_Record
	where (GroupID = p_groupID )
	  and ( ExtRecordTypeID = v_extRecordTypeID )
	  and ( ExtRecordValue = p_extRecordValue );

	if (coalesce( v_extRecordLinkID, -1 ) < 0) then
		-- Check to see if this record type is singular type (nonrepeatable)
		if (( select count(*) from SobekCM_External_Record_Type where ExtRecordTypeID = v_extRecordTypeID and repeatableTypeFlag = 'False' ) > 0 ) then
			-- Look for an existing singular record for this item group
			if (( select count(*) from SobekCM_Item_Group_External_Record
				where ( ExtRecordTypeID = v_extRecordTypeID ) and ( GroupID = p_groupID )) > 0 ) then
				select extRecordLinkID into v_extRecordLinkID
				from SobekCM_Item_Group_External_Record
				where ( ExtRecordTypeID = v_extRecordTypeID ) and ( GroupID = p_groupID );

				update SobekCM_Item_Group_External_Record
				set extRecordValue = p_extRecordValue
				where (extRecordLinkID = v_extRecordLinkID);
			else
				insert into SobekCM_Item_Group_External_Record ( groupid, extRecordTypeID, extRecordValue)
				values ( p_groupID, v_extRecordTypeID, p_extRecordValue );
			end if;
		else
			-- Non-singular record type value, so just insert if it doesn't exist
			if (( select COUNT(*) from SobekCM_Item_Group_External_Record where GroupID=p_groupID and ExtRecordTypeID=v_extRecordTypeID and ExtRecordValue = p_extRecordValue ) = 0 ) then
				insert into SobekCM_Item_Group_External_Record ( groupid, extRecordTypeID, extRecordValue)
				values ( p_groupID, v_extRecordTypeID, p_extRecordValue );
			end if;
		end if;
	end if;
END;
$$;

CREATE OR REPLACE FUNCTION SobekCM_Add_Item_Aggregation_Milestone(
	p_AggregationCode varchar(20),
	p_Milestone varchar(150),
	p_MilestoneUser text
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_aggregationid integer;
BEGIN
	v_aggregationid := coalesce( (select AggregationID from SobekCM_Item_Aggregation where Code=p_AggregationCode), -1);

	if ( v_aggregationid > 0 ) then
		-- only enter one of these per day
		if ( (select count(*) from SobekCM_Item_Aggregation_Milestones where ( AggregationID = v_aggregationid ) and ( MilestoneUser=p_MilestoneUser ) and ( Milestone=p_Milestone) and ( MilestoneDate::date = now()::date )) = 0 ) then
			insert into SobekCM_Item_Aggregation_Milestones ( AggregationID, Milestone, MilestoneDate, MilestoneUser )
			values ( v_aggregationid, p_Milestone, now(), p_MilestoneUser );
		end if;
	end if;
END;
$$;


-- Add or update existing viewers for an item
-- NOTE: This does not delete any existing viewers
CREATE OR REPLACE FUNCTION SobekCM_Add_Item_Viewers(
	p_ItemID integer,
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
	v_Viewer1_TypeID integer;
	v_Viewer2_TypeID integer;
	v_Viewer3_TypeID integer;
	v_Viewer4_TypeID integer;
	v_Viewer5_TypeID integer;
	v_Viewer6_TypeID integer;
BEGIN
	-- Add the first viewer information, if provided
	if ( length(coalesce(p_Viewer1_Type, '')) > 0 ) then
		v_Viewer1_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer1_Type ), -1 );

		if ( v_Viewer1_TypeID > 0 ) then
			if ( exists ( select 1 from SobekCM_Item_Viewers where ItemID=p_ItemID and ItemViewTypeID=v_Viewer1_TypeID )) then
				update SobekCM_Item_Viewers
				set Attribute=p_Viewer1_Attribute, Label=p_Viewer1_Label, Exclude='false'
				where ( ItemID = p_ItemID )
				  and ( ItemViewTypeID = v_Viewer1_TypeID );
			else
				insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
				values ( p_ItemID, v_Viewer1_TypeID, p_Viewer1_Attribute, p_Viewer1_Label );
			end if;
		end if;
	end if;

	-- Add the second viewer information, if provided
	if ( length(coalesce(p_Viewer2_Type, '')) > 0 ) then
		v_Viewer2_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer2_Type ), -1 );

		if ( v_Viewer2_TypeID > 0 ) then
			if ( exists ( select 1 from SobekCM_Item_Viewers where ItemID=p_ItemID and ItemViewTypeID=v_Viewer2_TypeID )) then
				update SobekCM_Item_Viewers
				set Attribute=p_Viewer2_Attribute, Label=p_Viewer2_Label, Exclude='false'
				where ( ItemID = p_ItemID )
				  and ( ItemViewTypeID = v_Viewer2_TypeID );
			else
				insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
				values ( p_ItemID, v_Viewer2_TypeID, p_Viewer2_Attribute, p_Viewer2_Label );
			end if;
		end if;
	end if;

	-- Add the third viewer information, if provided
	if ( length(coalesce(p_Viewer3_Type, '')) > 0 ) then
		v_Viewer3_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer3_Type ), -1 );

		if ( v_Viewer3_TypeID > 0 ) then
			if ( exists ( select 1 from SobekCM_Item_Viewers where ItemID=p_ItemID and ItemViewTypeID=v_Viewer3_TypeID )) then
				update SobekCM_Item_Viewers
				set Attribute=p_Viewer3_Attribute, Label=p_Viewer3_Label, Exclude='false'
				where ( ItemID = p_ItemID )
					and ( ItemViewTypeID = v_Viewer3_TypeID );
			else
				insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
				values ( p_ItemID, v_Viewer3_TypeID, p_Viewer3_Attribute, p_Viewer3_Label );
			end if;
		end if;
	end if;

	-- Add the fourth viewer information, if provided
	if ( length(coalesce(p_Viewer4_Type, '')) > 0 ) then
		v_Viewer4_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer4_Type ), -1 );

		if ( v_Viewer4_TypeID > 0 ) then
			if ( exists ( select 1 from SobekCM_Item_Viewers where ItemID=p_ItemID and ItemViewTypeID=v_Viewer4_TypeID )) then
				update SobekCM_Item_Viewers
				set Attribute=p_Viewer4_Attribute, Label=p_Viewer4_Label, Exclude='false'
				where ( ItemID = p_ItemID )
				  and ( ItemViewTypeID = v_Viewer4_TypeID );
			else
				insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
				values ( p_ItemID, v_Viewer4_TypeID, p_Viewer4_Attribute, p_Viewer4_Label );
			end if;
		end if;
	end if;

	-- Add the fifth viewer information, if provided
	if ( length(coalesce(p_Viewer5_Type, '')) > 0 ) then
		v_Viewer5_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer5_Type ), -1 );

		if ( v_Viewer5_TypeID > 0 ) then
			if ( exists ( select 1 from SobekCM_Item_Viewers where ItemID=p_ItemID and ItemViewTypeID=v_Viewer5_TypeID )) then
				update SobekCM_Item_Viewers
				set Attribute=p_Viewer5_Attribute, Label=p_Viewer5_Label, Exclude='false'
				where ( ItemID = p_ItemID )
				  and ( ItemViewTypeID = v_Viewer5_TypeID );
			else
				insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
				values ( p_ItemID, v_Viewer5_TypeID, p_Viewer5_Attribute, p_Viewer5_Label );
			end if;
		end if;
	end if;

	-- Add the sixth viewer information, if provided
	if ( length(coalesce(p_Viewer6_Type, '')) > 0 ) then
		v_Viewer6_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer6_Type ), -1 );

		if ( v_Viewer6_TypeID > 0 ) then
			if ( exists ( select 1 from SobekCM_Item_Viewers where ItemID=p_ItemID and ItemViewTypeID=v_Viewer6_TypeID )) then
				update SobekCM_Item_Viewers
				set Attribute=p_Viewer6_Attribute, Label=p_Viewer6_Label, Exclude='false'
				where ( ItemID = p_ItemID )
				  and ( ItemViewTypeID = v_Viewer6_TypeID );
			else
				insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label )
				values ( p_ItemID, v_Viewer6_TypeID, p_Viewer6_Attribute, p_Viewer6_Label );
			end if;
		end if;
	end if;
END;
$$;


-- Add some OAI-PMH data to an item.  Included will be the data (usually in XML format)
-- and the OAI-PMH code for that data type.  The XML information is saved as text, rather
-- than xml, since this data is never sub-queried.  It is just returned while serving OAI.
CREATE OR REPLACE FUNCTION SobekCM_Add_OAI_PMH_Data(
	p_itemid integer,
	p_data_code varchar(20),
	p_oai_data text
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if (( select COUNT(*) from SobekCM_Item_OAI where ItemID=p_itemid and Data_Code=p_data_code ) = 0 ) then
		insert into SobekCM_Item_OAI ( ItemID, OAI_Data, OAI_Date, Data_Code )
		values ( p_itemid, p_oai_data, now(), p_data_code );
	else
		update SobekCM_Item_OAI
		set OAI_Data=p_oai_data, OAI_Date=now(), Data_Code=p_data_code
		where ItemID=p_itemid and Locked='false' and Data_Code=p_data_code;
	end if;
END;
$$;


-- Procedure to add a new web skin, or edit an existing web skin
CREATE OR REPLACE FUNCTION SobekCM_Add_Web_Skin(
	p_webskincode varchar(20),
	p_basewebskin varchar(20),
	p_overridebanner boolean,
	p_overrideheaderfooter boolean,
	p_bannerlink varchar(255),
	p_notes varchar(250),
	p_build_on_launch boolean,
	p_suppress_top_nav boolean
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if (( select count(*) from SobekCM_Web_Skin where WebSkinCode = p_webskincode ) = 0 ) then
		insert into SobekCM_Web_Skin ( WebSkinCode, OverrideHeaderFooter, OverrideBanner, BaseWebSkin, BannerLink, Notes, Build_On_Launch, SuppressTopNavigation )
		values ( p_webskincode, p_overrideheaderfooter, p_overridebanner, p_basewebskin, p_bannerlink, p_notes, p_build_on_launch, p_suppress_top_nav );
	else
		update SobekCM_Web_Skin
		set OverrideHeaderFooter=p_overrideheaderfooter, OverrideBanner=p_overridebanner, BaseWebSkin=p_basewebskin, BannerLink=p_bannerlink, Notes=p_notes, Build_On_Launch=p_build_on_launch, SuppressTopNavigation=p_suppress_top_nav
		where WebSkinCode = p_webskincode;
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Aggregation_Change_Log(
	p_Code varchar(20)
)
RETURNS TABLE (
	Milestone varchar(150),
	MilestoneDate timestamp,
	MilestoneUser text
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_aggrId integer;
BEGIN
	v_aggrId := -1;
	if ( exists ( select 1 from SobekCM_Item_Aggregation where Code=p_Code )) then
		select AggregationID into v_aggrId from SobekCM_Item_Aggregation where Code=p_Code;
	end if;

	RETURN QUERY
	select M.Milestone, M.MilestoneDate, M.MilestoneUser
	from SobekCM_Item_Aggregation_Milestones M
	where AggregationID = v_aggrId
	order by MilestoneDate ASC, AggregationMilestoneID ASC;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Builder_Add_Log(
	p_RelatedBuilderLogID bigint,
	p_BibID_VID varchar(16),
	p_LogType varchar(25),
	p_LogMessage varchar(2000),
	p_METS_Type varchar(50),
	OUT p_BuilderLogID bigint
)
LANGUAGE plpgsql
AS $$
BEGIN
	insert into SobekCM_Builder_Log ( RelatedBuilderLogID, LogDate, BibID_VID, LogType, LogMessage )
	values ( p_RelatedBuilderLogID, now(), p_BibID_VID, p_LogType, p_LogMessage )
	returning BuilderLogID into p_BuilderLogID;
END;
$$;

-- Procedure to remove expired log files
CREATE OR REPLACE FUNCTION SobekCM_Builder_Expire_Log_Entries(
	p_Retain_For_Days integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_expiredate timestamp;
BEGIN
	v_expiredate := now() + make_interval(days => -1 * p_Retain_For_Days);
	v_expiredate := date_trunc('hour', v_expiredate);

	delete from SobekCM_Builder_Log
	where ( LogDate <= v_expiredate )
	  and ( LogType = 'No Work' );
END;
$$;


-- Procedure returns the names (and details) of all the module sets used for folders.
-- Originally returned 2 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_Builder_Get_Folder_Module_Sets(
	OUT cur_sets refcursor,
	OUT cur_modules refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_sets FOR
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
	  and ( S.Enabled = true )
	order by UsedCount DESC;

	OPEN cur_modules FOR
	select S.ModuleSetID, S.SetName, M.Assembly, M.Class, M.Enabled, M.Argument1, M.Argument2, M.Argument3, M.ModuleDesc, M."Order"
	from SobekCM_Builder_Module_Set S inner join
		 SobekCM_Builder_Module_Type T on S.ModuleTypeID=T.ModuleTypeID inner join
		 SobekCM_Builder_Module M on M.ModuleSetID=S.ModuleSetID
	where ( T.TypeAbbrev = 'FOLD' )
	  and ( S.Enabled = true )
	order by S.ModuleSetID, M."Order";
END;
$$;


-- Get the information about a single incoming folder.
-- Originally returned 2 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_Builder_Get_Incoming_Folder(
	p_FolderId integer,
	OUT cur_folder refcursor,
	OUT cur_modules refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_folder FOR
	select F.IncomingFolderId, F.NetworkFolder, F.ErrorFolder, F.ProcessingFolder, F.Perform_Checksum_Validation, F.Archive_TIFF, F.Archive_All_Files,
		   F.Allow_Deletes, F.Allow_Folders_No_Metadata, F.Allow_Metadata_Updates, F.FolderName, F.BibID_Roots_Restrictions,
		   F.ModuleSetID, S.SetName
	from SobekCM_Builder_Incoming_Folders F left outer join
	     SobekCM_Builder_Module_Set S on F.ModuleSetID=S.ModuleSetID
	where F.IncomingFolderId=p_FolderId;

	OPEN cur_modules FOR
	select S.ModuleSetID, S.SetName, M.Assembly, M.Class, M.Enabled, M.Argument1, M.Argument2, M.Argument3, M.ModuleDesc, M."Order", S.Enabled
	from SobekCM_Builder_Incoming_Folders F inner join
		 SobekCM_Builder_Module_Set S on S.ModuleSetID=F.ModuleSetID inner join
		 SobekCM_Builder_Module M on M.ModuleSetID=S.ModuleSetID
	where F.IncomingFolderId=p_FolderId
	order by M."Order";
END;
$$;


-- Gets the latest and greatest for when the builder ran, version, etc.. and also scheduled task
-- information to show. Originally returned 2 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_Builder_Get_Latest_Update(
	OUT cur_status refcursor,
	OUT cur_schedule refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_status FOR
	select Setting_Key, Setting_Value, Help, Options
	from SobekCM_Settings
	where ( Hidden = 'false' )
	  and ( TabPage = 'Builder' )
	  and ( Heading = 'Status' )
	order by TabPage, Heading, Setting_Key;

	OPEN cur_schedule FOR
	with last_run_cte ( ModuleScheduleID, LastRun) as
	(
		select ModuleScheduleID, MAX("Timestamp")
		from SobekCM_Builder_Module_Scheduled_Run
		group by ModuleScheduleID
	)
	select S.ModuleSetID, S.SetName, S.Enabled as SetEnabled, C.ModuleScheduleID, C.Enabled as ScheduleEnabled, C.DaysOfWeek, C.TimesOfDay, C.Description, coalesce(L.LastRun::text,'') as LastRun, coalesce(R.Outcome,'') as Outcome, coalesce(R.Message,'') as Message
	from SobekCM_Builder_Module_Set S inner join
		 SobekCM_Builder_Module_Type T on S.ModuleTypeID = T.ModuleTypeID inner join
		 SobekCM_Builder_Module_Schedule C on C.ModuleSetID = S.ModuleSetID left outer join
		 last_run_cte L on L.ModuleScheduleID = C.ModuleScheduleID left outer join
		 SobekCM_Builder_Module_Scheduled_Run R on R.ModuleSchedRunID=L.ModuleScheduleID and R."Timestamp" = L.LastRun
	where T.TypeAbbrev = 'SCHD'
	order by C.Description, S.SetOrder;
END;
$$;


-- Originally returned 7 result sets (only when exactly one bibid/vid match was found, otherwise
-- none); ported using OUT refcursor parameters -- all 7 cursors are always opened (as empty
-- result sets) when there is no match, since a PostgreSQL function's OUT parameter list is fixed
-- regardless of which branch runs, unlike the conditional SQL Server result-set batch.
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
		select I.ItemID, I.MainThumbnail, I.IP_Restriction_Mask, I.Born_Digital, G.ItemCount, I.Dark, I.MadePublicDate
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


-- Routine returns all the BUILDER-specific settings.
-- Originally returned 3 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_Builder_Get_Settings(
	p_include_disabled boolean,
	OUT cur_folders refcursor,
	OUT cur_modules refcursor,
	OUT cur_scheduled_modules refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_folders FOR
	select IncomingFolderId, NetworkFolder, ErrorFolder, ProcessingFolder, Perform_Checksum_Validation, Archive_TIFF, Archive_All_Files,
		   Allow_Deletes, Allow_Folders_No_Metadata, Allow_Metadata_Updates, FolderName, BibID_Roots_Restrictions,
		   F.ModuleSetID, S.SetName
	from SobekCM_Builder_Incoming_Folders F left outer join
	     SobekCM_Builder_Module_Set S on F.ModuleSetID=S.ModuleSetID;

	if ( p_include_disabled ) then
		OPEN cur_modules FOR
		select M.ModuleID, M.Assembly, M.Class, M.ModuleDesc, M.Argument1, M.Argument2, M.Argument3, M.Enabled, S.ModuleSetID, S.SetName, S.Enabled as SetEnabled, T.TypeAbbrev, T.TypeDescription
		from SobekCM_Builder_Module M, SobekCM_Builder_Module_Set S, SobekCM_Builder_Module_Type T
		where M.ModuleSetID = S.ModuleSetID
		  and S.ModuleTypeID = T.ModuleTypeID
		  and T.TypeAbbrev <> 'SCHD'
		order by TypeAbbrev, S.SetOrder, M."Order";
	else
		OPEN cur_modules FOR
		select M.ModuleID, M.Assembly, M.Class, M.ModuleDesc, M.Argument1, M.Argument2, M.Argument3, M.Enabled, S.ModuleSetID, S.SetName, S.Enabled as SetEnabled, T.TypeAbbrev, T.TypeDescription
		from SobekCM_Builder_Module M, SobekCM_Builder_Module_Set S, SobekCM_Builder_Module_Type T
		where M.ModuleSetID = S.ModuleSetID
		  and S.ModuleTypeID = T.ModuleTypeID
		  and M.Enabled = true
		  and S.Enabled = true
		  and T.TypeAbbrev <> 'SCHD'
		order by TypeAbbrev, S.SetOrder, M."Order";
	end if;

	if ( p_include_disabled ) then
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
	else
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
		  and M.Enabled = true
		  and S.Enabled = true
		  and C.Enabled = true
		order by TypeAbbrev, S.SetOrder, M."Order";
	end if;
END;
$$;


-- Deletes an incoming folder from the builder settings
CREATE OR REPLACE FUNCTION SobekCM_Builder_Incoming_Folder_Delete(
	p_IncomingFolderId integer
)
RETURNS void
LANGUAGE sql
AS $$
	delete from SobekCM_Builder_Incoming_Folders
	where IncomingFolderId=p_IncomingFolderId;
$$;


-- Add a new incoming folder for the builder/bulk loader, or edit
-- an existing incoming folder (by incoming folder id)
CREATE OR REPLACE FUNCTION SobekCM_Builder_Incoming_Folder_Edit(
	p_IncomingFolderId integer,
	p_NetworkFolder varchar(255),
	p_ErrorFolder varchar(255),
	p_ProcessingFolder varchar(255),
	p_Perform_Checksum_Validation boolean,
	p_Archive_TIFF boolean,
	p_Archive_All_Files boolean,
	p_Allow_Deletes boolean,
	p_Allow_Folders_No_Metadata boolean,
	p_FolderName varchar(150),
	p_BibID_Roots_Restrictions varchar(255),
	p_ModuleSetID integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_lastFolder varchar(255);
BEGIN
	v_lastFolder := '';

	if (( select COUNT(*) from SobekCM_Builder_Incoming_Folders where IncomingFolderId=p_IncomingFolderId ) = 0 ) then
		insert into SobekCM_Builder_Incoming_Folders ( NetworkFolder, ErrorFolder, ProcessingFolder, Perform_Checksum_Validation, Archive_TIFF, Archive_All_Files, Allow_Deletes, Allow_Folders_No_Metadata, FolderName, Allow_Metadata_Updates, BibID_Roots_Restrictions, ModuleSetID )
		values ( p_NetworkFolder, p_ErrorFolder, p_ProcessingFolder, p_Perform_Checksum_Validation, p_Archive_TIFF, p_Archive_All_Files, p_Allow_Deletes, p_Allow_Folders_No_Metadata, p_FolderName, 'true', p_BibID_Roots_Restrictions, p_ModuleSetID );
	else
		select NetworkFolder into v_lastFolder from SobekCM_Builder_Incoming_Folders where IncomingFolderId=p_IncomingFolderId;

		update SobekCM_Builder_Incoming_Folders
		set NetworkFolder=p_NetworkFolder, ErrorFolder=p_ErrorFolder, ProcessingFolder=p_ProcessingFolder,
			Perform_Checksum_Validation=p_Perform_Checksum_Validation, Archive_TIFF=p_Archive_TIFF,
			Archive_All_Files=p_Archive_All_Files, Allow_Deletes=p_Allow_Deletes,
			Allow_Folders_No_Metadata=p_Allow_Folders_No_Metadata, FolderName=p_FolderName,
			BibID_Roots_Restrictions=p_BibID_Roots_Restrictions, ModuleSetID=p_ModuleSetID
		where IncomingFolderId = p_IncomingFolderId;
	end if;

	-- If this is the only folder, and there is no main builder folder, set that one
	if ( ( select count(*) from SobekCM_Builder_Incoming_Folders ) = 1 ) then
		if ( not exists ( select 1 from SobekCM_Settings where Setting_Key = 'Main Builder Input Folder' )) then
			insert into SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help  )
			values ( 'Main Builder Input Folder', p_NetworkFolder, 'Builder', 'Builder Settings', false, false, 'This is the network location to the SobekCM Builder''s main incoming folder.\n\nThis is used by the SMaRT tool when doing bulk imports from spreadsheet or MARC records.' );
		elsif ( not exists ( select 1 from SobekCM_Settings where Setting_Key = 'Main Builder Input Folder' and length(coalesce(Setting_Value,'')) > 0 )) then
			update SobekCM_Settings
			set Setting_Value = p_NetworkFolder
			where Setting_Key = 'Main Builder Input Folder';
		elsif ( exists ( select 1 from SobekCM_Settings where Setting_Key = 'Main Builder Input Folder' and Setting_Value=v_lastFolder )) then
			update SobekCM_Settings
			set Setting_Value = p_NetworkFolder
			where Setting_Key = 'Main Builder Input Folder';
		end if;
	end if;
END;
$$;

-- Procedure returns builder logs, by date range and/or by bibid_vid
CREATE OR REPLACE FUNCTION SobekCM_Builder_Log_Search(
	p_startdate timestamp,
	p_enddate timestamp,
	p_bibid_vid varchar(20),
	p_include_no_work_entries boolean
)
RETURNS TABLE (
	BuilderLogID bigint,
	RelatedBuilderLogID bigint,
	LogDate timestamp,
	BibID_VID varchar(16),
	LogType varchar(25),
	LogMessage varchar(2000),
	SuccessMessage varchar(500),
	METS_Type varchar(50)
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ( p_startdate is null ) then p_startdate := '2000-01-01'; end if;
	if ( p_enddate is null ) then p_enddate := now() + interval '1 day'; end if;

	-- If p_bibid_vid is NULL or empty, than this is only a date search
	if ( length(coalesce(p_bibid_vid,'')) = 0 ) then
		if ( p_include_no_work_entries ) then
			RETURN QUERY
			select BuilderLogID, RelatedBuilderLogID, LogDate, coalesce(BibID_VID,'') as BibID_VID, coalesce(LogType,'') as LogType, coalesce(LogMessage,'') as LogMessage, SuccessMessage, METS_Type
			from SobekCM_Builder_Log
			where ( LogDate >= p_startdate )
			  and ( LogDate <= p_enddate )
			order by LogDate DESC;
		else
			RETURN QUERY
			select BuilderLogID, RelatedBuilderLogID, LogDate, coalesce(BibID_VID,'') as BibID_VID, coalesce(LogType,'') as LogType, coalesce(LogMessage,'') as LogMessage, SuccessMessage, METS_Type
			from SobekCM_Builder_Log
			where ( LogDate >= p_startdate )
			  and ( LogDate <= p_enddate )
			  and ( LogType != 'No Work' )
			order by LogDate DESC;
		end if;
		RETURN;
	end if;

	-- Is this a LIKE search, or an exact search?
	if ( strpos(p_bibid_vid, '%' ) > 0 ) then
		RETURN QUERY
		select BuilderLogID, RelatedBuilderLogID, LogDate, coalesce(BibID_VID,'') as BibID_VID, coalesce(LogType,'') as LogType, coalesce(LogMessage,'') as LogMessage, SuccessMessage, METS_Type
		from SobekCM_Builder_Log
		where ( LogDate >= p_startdate )
		  and ( LogDate <= p_enddate )
		  and ( BibID_VID like p_bibid_vid )
		order by LogDate DESC;
	else
		RETURN QUERY
		select BuilderLogID, RelatedBuilderLogID, LogDate, coalesce(BibID_VID,'') as BibID_VID, coalesce(LogType,'') as LogType, coalesce(LogMessage,'') as LogMessage, SuccessMessage, METS_Type
		from SobekCM_Builder_Log
		where ( LogDate >= p_startdate )
		  and ( LogDate <= p_enddate )
		  and ( BibID_VID = p_bibid_vid )
		order by LogDate DESC;
	end if;
END;
$$;


-- Marks items from the item error log as cleared, by date.  This does not actually
-- clear the item error completely, just marks the error as cleared so the history
-- of the error log is maintained
CREATE OR REPLACE FUNCTION SobekCM_Clear_Item_Error_Log(
	p_BibID varchar(10),
	p_VID varchar(5),
	p_ClearedBy varchar(100)
)
RETURNS void
LANGUAGE sql
AS $$
	update SobekCM_Item_Error_Log
	set ClearedBy = p_ClearedBy, ClearedDate=now()
	where BibID=p_BibID and VID=p_VID;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Clear_Item_User_Group_Permissions(
	p_ItemId integer
)
RETURNS void
LANGUAGE sql
AS $$
	delete from mySobek_User_Group_Item_Permissions
	where ItemID = p_ItemId;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Clear_Item_User_Permissions(
	p_ItemId integer
)
RETURNS void
LANGUAGE sql
AS $$
	delete from mySobek_User_Item_Permissions
	where ItemID = p_ItemId;
$$;


-- Clears all the periphery data about an item in UFDC
CREATE OR REPLACE FUNCTION SobekCM_Clear_Old_Item_Info(
	p_ItemID integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	-- Delete all links to georegion (if table exists)
	IF ( to_regclass('SobekCM_Item_GeoRegion_Link') IS NOT NULL ) THEN
		delete from SobekCM_Item_GeoRegion_Link where ItemID = p_itemid;
	END IF;

	-- Deletes the immediate geographic footprint (if table exists)
	IF ( to_regclass('SobekCM_Item_Footprint') IS NOT NULL ) THEN
		delete from SobekCM_Item_Footprint where ItemID = p_ItemID;
	END IF;
END;
$$;


-- Gets the list of all point coordinates for a single aggregation
CREATE OR REPLACE FUNCTION SobekCM_Coordinate_Points_By_Aggregation(
	p_aggregation_code varchar(20)
)
RETURNS TABLE (
	Point_Latitude double precision,
	Point_Longitude double precision,
	BibID varchar(10),
	GroupTitle varchar(500),
	Thumbnail text,
	ItemCount integer,
	"Type" varchar(50)
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	with min_itemid_per_groupid as
	(
		-- Get the mininmum ItemID per group per coordinate point
		select GroupID, F.Point_Latitude, F.Point_Longitude, Min(I.ItemID) as MinItemID
		from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A, SobekCM_Item_Footprint F
		where ( I.ItemID = L.ItemID  )
		  and ( L.AggregationID = A.AggregationID )
		  and ( A.Code = p_aggregation_code )
		  and ( F.ItemID = I.ItemID )
		  and ( F.Point_Latitude is not null )
		  and ( F.Point_Longitude is not null )
		group by GroupID, F.Point_Latitude, F.Point_Longitude
	), min_item_thumbnail_per_group as
	(
	    -- Get the matching item thumbnail for the item per group per coordiante point
		select G.GroupID, G.Point_Latitude, G.Point_Longitude, I.VID || '/' || I.MainThumbnail as MinThumbnail
		from SobekCM_Item I, min_itemid_per_groupid G
		where G.MinItemID = I.ItemID
	)
	-- Return all matching group/coordinate point, with the group thumbnail, or item thumbnail from above WITH statements
	select F.Point_Latitude, F.Point_Longitude, G.BibID, G.GroupTitle, coalesce(NULLIF(G.GroupThumbnail,''), T.MinThumbnail) as Thumbnail, G.ItemCount, G.Type
	from SobekCM_Item_Group G, SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Footprint F, SobekCM_Item_Aggregation A, min_item_thumbnail_per_group T
	where ( G.GroupID = I.GroupID )
	  and ( I.ItemID = L.ItemID  )
	  and ( L.AggregationID = A.AggregationID )
	  and ( A.Code = p_aggregation_code )
	  and ( F.ItemID = I.ItemID )
	  and ( F.Point_Latitude is not null )
	  and ( F.Point_Longitude is not null )
	  and ( T.GroupID = G.GroupID )
	  and ( T.Point_Latitude = F.Point_Latitude )
	  and ( T.Point_Longitude = F.Point_Longitude )
	group by I.Spatial_KML, F.Point_Latitude, F.Point_Longitude, G.BibID, G.GroupTitle, coalesce(NULLIF(G.GroupThumbnail,''), T.MinThumbnail), G.ItemCount, G.Type
	order by I.Spatial_KML;
END;
$$;


-- Delete an existing Wordmark, and output the number of links to that wordmark
-- If there are any items linked to that wordmark, the icon is not deleted
CREATE OR REPLACE FUNCTION SobekCM_Delete_Icon(
	p_icon_code varchar(255),
	OUT p_links integer
)
LANGUAGE plpgsql
AS $$
BEGIN
	select count(*) into p_links from SobekCM_Item_Icons L, SobekCM_Icon I where I.Icon_Name = p_icon_code and I.IconID = L.IconID;

	if ( p_links = 0 ) then
		delete from SobekCM_Icon where Icon_Name = p_icon_code;
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Delete_IP_Range(
	p_rangeid integer
)
RETURNS void
LANGUAGE sql
AS $$
	UPDATE SobekCM_IP_Restriction_Range set Deleted='TRUE' where IP_RangeID=p_rangeid;
$$;


-- Deletes an item, and deletes the group if there are no additional items attached
CREATE OR REPLACE FUNCTION SobekCM_Delete_Item(
	p_bibid varchar(10),
	p_vid varchar(5),
	p_as_admin boolean,
	p_delete_message varchar(1000)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_itemid integer;
	v_groupid integer;
BEGIN
	-- Perform transactionally in case there is a problem deleting some of the rows
	-- so the entire delete is rolled back ( a function already runs atomically in its
	-- caller's transaction, so no explicit transaction control is needed here )

	v_itemid := 0;

	select coalesce(I.itemid, 0) into v_itemid
	from SobekCM_Item I, SobekCM_Item_Group G
	where (G.bibid = p_bibid)
	    and (I.vid = p_vid)
	    and ( I.GroupID = G.GroupID );

	if ( coalesce(v_itemid, 0 ) > 0) then
		-- Delete all references to this item
		delete from SobekCM_Item_Footprint where ItemID=v_itemid;
		delete from SobekCM_Item_Icons where ItemID=v_itemid;
		delete from SobekCM_Item_Statistics where ItemID=v_itemid;
		delete from SobekCM_Item_GeoRegion_Link where ItemID=v_itemid;
		delete from SobekCM_Item_Aggregation_Item_Link where ItemID=v_itemid;
		delete from mySobek_User_Item where ItemID=v_itemid;
		delete from mySobek_User_Item_Link where ItemID=v_itemid;
		delete from mySobek_User_Description_Tags where ItemID=v_itemid;
		delete from SobekCM_Item_Viewers where ItemID=v_itemid;
		delete from Tracking_Item where ItemID=v_itemid;
		delete from Tracking_Progress where ItemID=v_itemid;
		delete from SobekCM_Item_OAI where ItemID=v_itemid;
		delete from SobekCM_QC_Errors where ItemID=v_itemid;
		delete from SobekCM_QC_Errors_History where ItemID=v_itemid;
		delete from SobekCM_Item_Settings where ItemID=v_itemid;

		-- Finally, delete the item
		delete from SobekCM_Item where ItemID=v_itemid;

		-- Delete the item group if it is the last one existing
		if (( select count(I.ItemID) from SobekCM_Item_Group G, SobekCM_Item I where ( G.BibID = p_bibid ) and ( G.GroupID = I.GroupID ) and ( I.Deleted = false )) < 1 ) then
			v_groupid := 0;

			select coalesce(G.GroupID, 0) into v_groupid
			from SobekCM_Item_Group G
			where (G.bibid = p_bibid);

			if ( coalesce(v_groupid, 0 ) > 0 ) then
				delete from SobekCM_Item_Group_External_Record where GroupID=v_groupid;
				delete from SobekCM_Item_Group_Web_Skin_Link where GroupID=v_groupid;
				delete from SobekCM_Item_Group_Statistics where GroupID=v_groupid;
				delete from mySobek_User_Bib_Link where GroupID=v_groupid;
				delete from SobekCM_Item_Group_OAI where GroupID=v_groupid;
				delete from SobekCM_Item_Group where GroupID=v_groupid;
			end if;
		else
			-- Finally set the volume count for this group correctly
			update SobekCM_Item_Group
			set ItemCount = ( select count(*) from SobekCM_Item I where ( I.GroupID = SobekCM_Item_Group.GroupID ))
			where ( SobekCM_Item_Group.BibID = p_bibid );
		end if;
	end if;
END;
$$;

-- Procedure to delete an item aggregation and unlink it completely.
-- This fails if there are any child aggregations.  This does not really
-- delete the item aggregation, just marks it as DELETED and removed most
-- references.  The statistics are retained.
CREATE OR REPLACE FUNCTION SobekCM_Delete_Item_Aggregation(
	p_aggrcode varchar(20),
	p_isadmin boolean,
	p_username varchar(100),
	OUT p_message varchar(1000),
	OUT p_errorcode integer
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_aggrid integer;
BEGIN
	-- Do not delete 'ALL'
	if ( p_aggrcode = 'ALL' ) then
		p_message := 'Cannot delete the ALL aggregation.';
		p_errorcode := 3;
		return;
	end if;

	if (( select count(*) from SobekCM_Item_Aggregation where Code = p_aggrcode ) > 0 ) then
		select AggregationID into v_aggrid from SobekCM_Item_Aggregation where Code = p_aggrcode;

		-- Are there any children aggregations to this?
		if (( select COUNT(*) from SobekCM_Item_Aggregation_Hierarchy H, SobekCM_Item_Aggregation A where H.ParentID=v_aggrid and A.AggregationID=H.ChildID and A.Deleted='false' ) > 0 ) then
			p_message := 'Item aggregation still has child aggregations';
			p_errorcode := 2;
		else
			-- How many items are still linked to the item aggregation?
			if (( not p_isadmin ) and (( select count(*) from SobekCM_Item_Aggregation_Item_Link where AggregationID=v_aggrid ) > 0 )) then
				p_message := 'Only system admins can delete aggregations with digital resources';
				p_errorcode := 4;
			else
				p_message := 'Item aggregation removed';
				p_errorcode := 0;

				delete from mySobek_User_Group_Edit_Aggregation
				where AggregationID = v_aggrid;

				delete from mySobek_User_Edit_Aggregation
				where AggregationID = v_aggrid;

				-- Delete links to any items
				--delete from SobekCM_Item_Aggregation_Item_Link
				--where AggregationID = v_aggrid;

				delete from SobekCM_Item_Aggregation_Alias
				where AggregationID = v_aggrid;

				delete from SobekCM_Portal_Item_Aggregation_Link
				where AggregationID = v_aggrid;

				update SobekCM_Item_Aggregation
				set Deleted = 'true', DeleteDate=now()
				where AggregationID = v_aggrid;

				insert into SobekCM_Item_Aggregation_Milestones ( AggregationID, Milestone, MilestoneDate, MilestoneUser )
				values ( v_aggrid, 'Deleted', now(), p_username );
			end if;
		end if;
	else
		p_message := 'No matching item aggregation found';
		p_errorcode := 1;
	end if;
END;
$$;


-- Delete a single item aggregation alias (or forwarding) by alias
CREATE OR REPLACE FUNCTION SobekCM_Delete_Item_Aggregation_Alias(
	p_alias varchar(50)
)
RETURNS void
LANGUAGE sql
AS $$
	delete from SobekCM_Item_Aggregation_Alias
	where AggregationAlias = p_alias;
$$;


-- Delete an entire URL Portal, by URL portal ID
CREATE OR REPLACE FUNCTION SobekCM_Delete_Portal(
	p_PortalID integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	delete from SobekCM_Portal_Item_Aggregation_Link where PortalID=p_PortalID;
	delete from SobekCM_Portal_Web_Skin_Link where PortalID=p_PortalID;
	delete from SobekCM_Portal_URL_Statistics where PortalID=p_PortalID;
	delete from SobekCM_Portal_URL where PortalID = p_PortalID;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Delete_Project_Aggregation_Link(
	p_ProjectID integer,
	p_AggregationID integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if((select count(*) from SobekCM_Project_Aggregation_Link  where ( ProjectID = p_ProjectID and AggregationID=p_AggregationID ))  = 1 ) then
		delete from SobekCM_Project_Aggregation_Link
		where (ProjectID=p_ProjectID and AggregationID=p_AggregationID);
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Delete_Project_DefaultMetadata_Link(
	p_ProjectID integer,
	p_DefaultMetadataID integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if((select count(*) from SobekCM_Project_DefaultMetadata_Link  where ( ProjectID = p_ProjectID and DefaultMetadataID=p_DefaultMetadataID ))  = 1 ) then
		delete from SobekCM_Project_DefaultMetadata_Link
		where (ProjectID=p_ProjectID and DefaultMetadataID=p_DefaultMetadataID);
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Delete_Project_Item_Link(
	p_ProjectID integer,
	p_ItemID integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if((select count(*) from SobekCM_Project_Item_Link  where ( ProjectID = p_ProjectID and ItemID=p_ItemID ))  = 1 ) then
		delete from SobekCM_Project_Item_Link
		where (ProjectID=p_ProjectID and ItemID=p_ItemID);
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Delete_Project_Template_Link(
	p_ProjectID integer,
	p_TemplateID integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if((select count(*) from SobekCM_Project_Template_Link  where ( ProjectID = p_ProjectID and TemplateID=p_TemplateID ))  = 1 ) then
		delete from SobekCM_Project_Template_Link
		where (ProjectID=p_ProjectID and TemplateID=p_TemplateID);
	end if;
END;
$$;


-- Delete a single IP, from a larger IP restriction range
CREATE OR REPLACE FUNCTION SobekCM_Delete_Single_IP(
	p_ip_singleid integer
)
RETURNS void
LANGUAGE sql
AS $$
	delete from SobekCM_IP_Restriction_Single where IP_SingleID=p_ip_singleid;
$$;


-- Delete a single thematic heading, and unlink any aggregations currently
-- appearing under this thematic heading on the main library home page
CREATE OR REPLACE FUNCTION SobekCM_Delete_Thematic_Heading(
	p_ThematicHeadingID integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	update SobekCM_Item_Aggregation
	set ThematicHeadingID = -1 where ThematicHeadingID=p_ThematicHeadingID;

	delete from SobekCM_Thematic_Heading
	where ThematicHeadingID=p_ThematicHeadingID;
END;
$$;


-- Procedure to delete a web skin, and unlink any items or web portals which
-- were linked to this web skin
CREATE OR REPLACE FUNCTION SobekCM_Delete_Web_Skin(
	p_webskincode varchar(20),
	p_force_delete boolean,
	OUT p_links integer
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_webskinid integer;
BEGIN
	p_links := 0;

	if (( select count(*) from SobekCM_Web_Skin where WebSkinCode = p_webskincode ) > 0 ) then
		select WebSkinID into v_webskinid from SobekCM_Web_Skin where WebSkinCode=p_webskincode;

		if ( p_force_delete ) then
			delete from SobekCM_Item_Group_Web_Skin_Link
			where WebSkinID=v_webskinid;

			delete from SobekCM_Portal_Web_Skin_Link
			where WebSkinID=v_webskinid;

			update SobekCM_Item_Aggregation
			set DefaultInterface = ''
			where DefaultInterface = p_webskincode;

			delete from SobekCM_Web_Skin
			where WebSkinID=v_webskinid;
		else
			if ((( select count(*) from SobekCM_Item_Group_Web_Skin_Link where WebSkinID=v_webskinid ) > 0 ) or
			    (( select count(*) from SobekCM_Portal_Web_Skin_Link where WebSkinID=v_webskinid ) > 0 ) or
			    (( select count(*) from SobekCM_Item_Aggregation where DefaultInterface=p_webskincode ) > 0 )) then
				p_links := 1;
			else
				delete from SobekCM_Web_Skin
				where WebSkinID=v_webskinid;
			end if;
		end if;
	end if;
END;
$$;


-- Edit basic information about an ip restriction range, or add a new range
CREATE OR REPLACE FUNCTION SobekCM_Edit_IP_Range(
	p_rangeid integer,
	p_title varchar(150),
	p_notes varchar(2000),
	p_not_valid_statement text
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if ( p_rangeid in (select IP_RangeID from SobekCM_IP_Restriction_Range )) then
		update SobekCM_IP_Restriction_Range
		set Title=p_title, Notes=p_notes, Not_Valid_Statement=p_not_valid_statement
		where IP_RangeID = p_rangeid;
	else
		insert into SobekCM_IP_Restriction_Range ( Title, Notes, Not_Valid_Statement )
		values ( p_title, p_notes, p_not_valid_statement );
	end if;
END;
$$;

-- Procedure to edit an existing URL portal or saving a new URL portal
CREATE OR REPLACE FUNCTION SobekCM_Edit_Portal(
	p_PortalID integer,
	p_Base_URL varchar(150),
	p_isActive boolean,
	p_isDefault boolean,
	p_Abbreviation varchar(10),
	p_Name varchar(250),
	p_Default_Aggregation varchar(20),
	p_Base_PURL varchar(150),
	p_Default_Web_Skin varchar(20),
	OUT p_NewID integer
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_aggrid integer;
	v_skinid integer;
BEGIN
	if (( select COUNT(*) from SobekCM_Portal_URL where PortalID=p_PortalID ) = 0 ) then
		insert into SobekCM_Portal_URL ( Abbreviation, isActive, isDefault, Name, Base_URL, Base_PURL )
		values ( p_Abbreviation, p_isActive, p_isDefault, p_Name, p_Base_URL, p_Base_PURL )
		returning PortalID into p_NewID;
	else
		update SobekCM_Portal_URL
		set Abbreviation=p_Abbreviation, isActive=p_isActive, isDefault=p_isDefault, Name=p_Name, Base_URL=p_Base_URL, Base_PURL=p_Base_PURL
		where PortalID = p_PortalID;

		p_NewID := p_PortalID;
	end if;

	-- Clear any default aggregations and web skins
	delete from SobekCM_Portal_Item_Aggregation_Link where PortalID=p_NewID;
	delete from SobekCM_Portal_Web_Skin_Link where PortalID=p_NewID;

	-- Add the default aggregation, if one is chosen
	if ( length(coalesce(p_Default_Aggregation, '')) > 0 ) then
		if (( select COUNT(*) from SobekCM_Item_Aggregation where Code=p_Default_Aggregation ) = 1 ) then
			select AggregationID into v_aggrid from SobekCM_Item_Aggregation where Code=p_Default_Aggregation;

			insert into SobekCM_Portal_Item_Aggregation_Link ( PortalID, AggregationID, isDefault )
			values ( p_NewID, v_aggrid, 'true' );
		end if;
	end if;

	-- Add the web skin, if one is chosen
	if ( length(coalesce(p_Default_Web_Skin, '')) > 0 ) then
		if (( select COUNT(*) from SobekCM_Web_Skin where WebSkinCode=p_Default_Web_Skin ) = 1 ) then
			select WebSkinID into v_skinid from SobekCM_Web_Skin where WebSkinCode=p_Default_Web_Skin;

			insert into SobekCM_Portal_Web_Skin_Link ( PortalID, WebSkinID, isDefault )
			values ( p_NewID, v_skinid, 'true' );
		end if;
	end if;
END;
$$;


-- Edits a single ip point within an entire IP restriction set of ranges, or
-- else adds a new ip point, if the provided ip_singleid is zero or less
CREATE OR REPLACE FUNCTION SobekCM_Edit_Single_IP(
	p_ip_singleid integer,
	p_ip_rangeid integer,
	p_startip char(15),
	p_endip char(15),
	p_notes varchar(100),
	OUT p_new_ip_singleid integer
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ( p_ip_singleid > 0 ) then
		update SobekCM_IP_Restriction_Single
		set StartIP = p_startip, EndIP = p_endip, Notes=p_notes
		where IP_SingleID = p_ip_singleid;

		p_new_ip_singleid := p_ip_singleid;
	else
		insert into SobekCM_IP_Restriction_Single ( IP_RangeID, StartIP, EndIP, Notes )
		values ( p_ip_rangeid, p_startip, p_endip, p_notes )
		returning IP_SingleID into p_new_ip_singleid;
	end if;
END;
$$;


-- Edits the order and name for an existing themathic heading, or adds a new heading
-- if the provided thematic heading id is not valid
CREATE OR REPLACE FUNCTION SobekCM_Edit_Thematic_Heading(
	p_ThematicHeadingID integer,
	p_ThemeOrder integer,
	p_ThemeName varchar(100),
	OUT p_NewID integer
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ( p_ThematicHeadingID in ( select ThematicHeadingID from SobekCM_Thematic_Heading )) then
		update SobekCM_Thematic_Heading
		set ThemeOrder = p_ThemeOrder, ThemeName = p_ThemeName
		where ThematicHeadingID = p_ThematicHeadingID;

		p_NewID := p_ThematicHeadingID;
	else
		insert into SobekCM_Thematic_Heading ( ThemeOrder, ThemeName )
		values ( p_ThemeOrder, p_ThemeName )
		returning ThematicHeadingID into p_NewID;
	end if;
END;
$$;


-- Add information about a new extension, or update an existing extension
CREATE OR REPLACE FUNCTION SobekCM_Extensions_Add_Update(
	p_Code varchar(50),
	p_Name varchar(255),
	p_CurrentVersion varchar(50),
	p_LicenseKey text,
	p_UpgradeUrl varchar(255),
	p_LatestVersion varchar(50)
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if ( exists ( select 1 from SobekCM_Extension where Code=p_Code )) then
		update SobekCM_Extension
		set Name=p_Name,
			CurrentVersion=p_CurrentVersion,
			LicenseKey=p_LicenseKey,
			UpgradeUrl=p_UpgradeUrl,
			LatestVersion=p_LatestVersion
		where Code=p_Code;
	else
		insert into SobekCM_Extension (Code, Name, CurrentVersion, IsEnabled, LicenseKey, UpgradeUrl, LatestVersion )
		values ( p_Code, p_Name, p_CurrentVersion, 'false', p_LicenseKey, p_UpgradeUrl, p_LatestVersion );
	end if;
END;
$$;


-- Get the list of extensions in the system
CREATE OR REPLACE FUNCTION SobekCM_Extensions_Get_All()
RETURNS TABLE (
	ExtensionID integer,
	Code varchar(50),
	Name varchar(255),
	CurrentVersion varchar(50),
	IsEnabled boolean,
	EnabledDate timestamp,
	LicenseKey text,
	UpgradeUrl varchar(255),
	LatestVersion varchar(50)
)
LANGUAGE sql
AS $$
	select ExtensionID, Code, Name, CurrentVersion, IsEnabled, EnabledDate, LicenseKey, UpgradeUrl, LatestVersion
	from SobekCM_Extension
	order by Code;
$$;


-- Remove an extension completely from the database
CREATE OR REPLACE FUNCTION SobekCM_Extensions_Remove(
	p_Code varchar(50)
)
RETURNS void
LANGUAGE sql
AS $$
	delete from SobekCM_Extension
	where Code=p_Code;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Extensions_Set_Enable(
	p_Code varchar(50),
	p_EnableFlag boolean,
	OUT p_Message varchar(255)
)
LANGUAGE plpgsql
AS $$
BEGIN
	-- If the code is missing, do nothing
	if ( not exists ( select 1 from SobekCM_Extension where Code=p_Code )) then
		p_Message := 'ERROR: Unable to find matching extension in the database!';
		return;
	end if;

	-- If the enable flag in the database is already set that way, do nothing
	if ( exists ( select 1 from SobekCM_Extension where Code=p_Code and IsEnabled=p_EnableFlag )) then
		p_Message := 'Enabled flag was already set as requested for this plug-in';
		return;
	end if;

	if ( not p_EnableFlag ) then
		update SobekCM_Extension set IsEnabled='false', EnabledDate=null where Code=p_Code;
		p_Message := 'Disabled ' || p_Code || ' plugin';
	else
		update SobekCM_Extension set IsEnabled='true', EnabledDate=now() where Code=p_Code;
		p_Message := 'Enabled ' || p_Code || ' plugin';
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Get_Aggregations_By_ProjectID(
	p_ProjectID integer
)
RETURNS TABLE (
	AggregationID integer
)
LANGUAGE sql
AS $$
	select AggregationID from SobekCM_Project_Aggregation_Link
	where ProjectID=p_ProjectID;
$$;


-- Get the information about the ALL aggregation - standard for home page collection.
-- Written by Mark Sullivan (September 2005), Updated ( January 2010 ).
-- Originally returned 6 result sets; ported using OUT refcursor parameters. The original
-- @TEMP_CHILDREN_BUILDER table variable was declared but never referenced anywhere in the
-- procedure body, so it is dropped here as dead code.
CREATE OR REPLACE FUNCTION SobekCM_Get_All_Groups(
	OUT cur_main refcursor,
	OUT cur_bounds refcursor,
	OUT cur_settings refcursor,
	OUT cur_result_views refcursor,
	OUT cur_facets refcursor,
	OUT cur_result_fields refcursor
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_aggregationid integer;
	v_last_added_date timestamp;
	v_has_new_items boolean;
BEGIN
	select AggregationID into v_aggregationid
	from SobekCM_Item_Aggregation AS C
	where ( C.Code = 'all' );

	-- Determine when the last item was made available and if the new browse should display
	select MAX(MadePublicDate) into v_last_added_date from SobekCM_Item I where I.Dark='false' and I.IP_Restriction_Mask >= 0 and I.IncludeInAll='true';

	v_has_new_items := false;
	if ( coalesce(v_last_added_date, '1900-01-01' ) > now() - interval '14 days') then
		v_has_new_items := true;
	end if;

	OPEN cur_main FOR
	select AggregationID, Code, Name, coalesce(ShortName,Name) AS ShortName, Type, isActive, Hidden, v_has_new_items as HasNewItems,
	   ContactEmail, DefaultInterface, Description, Map_Display, Map_Search, OAI_Flag, OAI_Metadata, DisplayOptions,
	  coalesce(v_last_added_date, '1900-01-01' ) as LastItemAdded, Can_Browse_Items, Items_Can_Be_Described, External_Link, GroupResults
	from SobekCM_Item_Aggregation AS C
	where ( C.AggregationID=v_aggregationid );

	OPEN cur_bounds FOR
	select Min(F.Point_Latitude) as Min_Latitude, Max(F.Point_Latitude) as Max_Latitude, Min(F.Point_Longitude) as Min_Longitude, Max(F.Point_Longitude) as Max_Longitude
	from SobekCM_Item I, SobekCM_Item_Footprint F
	where ( F.ItemID = I.ItemID )
	  and ( F.Point_Latitude is not null )
	  and ( F.Point_Longitude is not null )
	  and ( I.Dark = 'false' );

	OPEN cur_settings FOR
	select Setting_Key, Setting_Value
	from SobekCM_Item_Aggregation_Settings
	where AggregationID=v_aggregationid;

	-- Get the result views linked to this aggregation and save in a temp table.
	-- ON COMMIT DROP cleans it up automatically once every cursor above has been
	-- fetched and the caller's transaction commits (see pg_execute_dataset in
	-- EalDbAccess.cs) -- an explicit DROP TABLE here would break cur_result_views
	-- and cur_result_fields below, which are still lazily bound to it.
	CREATE TEMP TABLE resultviews ON COMMIT DROP AS
	select T.ResultType, A.DefaultView, A.ItemAggregationResultTypeID, ItemAggregationResultID, T.DefaultOrder
	from SobekCM_Item_Aggregation_Result_Views A, SobekCM_Item_Aggregation_Result_Types T
	where A.AggregationID=v_aggregationid
	  and A.ItemAggregationResultTypeID=T.ItemAggregationResultTypeID;

	OPEN cur_result_views FOR
	select ResultType, DefaultView
	from resultviews
	order by DefaultOrder ASC;

	OPEN cur_facets FOR
	select F.MetadataTypeID, coalesce(F.OverrideFacetTerm, T.FacetTerm) as FacetTerm, T.SobekCode, T.SolrCode_Facets
	from SobekCM_Item_Aggregation_Facets F, SobekCM_Metadata_Types T
	where ( F.AggregationID = v_aggregationid )
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	order by FacetOrder;

	OPEN cur_result_fields FOR
	select A.ResultType, F.MetadataTypeID, coalesce(F.OverrideDisplayTerm, T.DisplayTerm) as DisplayTerm, T.SobekCode, T.SolrCode_Display, F.DisplayOrder, 'Custom' as Source
	from SobekCM_Item_Aggregation_Result_Fields F, SobekCM_Metadata_Types T, resultviews A
	where ( A.ItemAggregationResultID = F.ItemAggregationResultID )
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	union
	select A.ResultType, F.MetadataTypeID, coalesce(F.OverrideDisplayTerm, T.DisplayTerm) as DisplayTerm, T.SobekCode, T.SolrCode_Display, F.DisplayOrder, 'Default' as Source
	from SobekCM_Item_Aggregation_Default_Result_Fields F, SobekCM_Metadata_Types T, resultviews A
	where ( A.ItemAggregationResultTypeID = F.ItemAggregationResultTypeID )
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	  and ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Fields X where A.ItemAggregationResultID = X.ItemAggregationResultID ))
	order by A.ResultType, DisplayOrder;
END;
$$;

CREATE OR REPLACE FUNCTION SobekCM_Get_All_IP_Restrictions()
RETURNS TABLE (
	Title varchar(150),
	IP_RangeID integer,
	Not_Valid_Statement text,
	StartIP char(15),
	EndIP char(15),
	Notes varchar(2000)
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	select R.Title, R.IP_RangeID, R.Not_Valid_Statement, coalesce(S.StartIP,'') as StartIP, coalesce(S.EndIP,'') as EndIP, coalesce(R.Notes,'') as Notes
	from SobekCM_IP_Restriction_Range AS R LEFT JOIN
	     SobekCM_IP_Restriction_Single AS S ON R.IP_RangeID = S.IP_RangeID
	where R.Deleted = 'false'
	order by IP_RangeID ASC;
END;
$$;


-- Get all of the portal information for this digital library.
-- Originally returned 3 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_Get_All_Portals(
	p_activeonly boolean,
	OUT cur_portals refcursor,
	OUT cur_aggregation_links refcursor,
	OUT cur_webskin_links refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ( p_activeonly ) then
		OPEN cur_portals FOR
		select *
		from SobekCM_Portal_URL
		where isActive = 'true';

		OPEN cur_aggregation_links FOR
		select P.PortalID, A.Code
		from SobekCM_Portal_URL P, SobekCM_Portal_Item_Aggregation_Link AL, SobekCM_Item_Aggregation A
		where ( P.PortalID = AL.PortalID )
		  and ( AL.AggregationID = A.AggregationID )
		  and ( P.isActive = 'true' );

		OPEN cur_webskin_links FOR
		select P.PortalID, W.WebSkinCode
		from SobekCM_Portal_URL P, SobekCM_Portal_Web_Skin_Link WL, SobekCM_Web_Skin W
		where ( P.PortalID = WL.PortalID )
		  and ( WL.WebSkinID = W.WebSkinID )
		  and ( P.isActive = 'true' );
	else
		OPEN cur_portals FOR
		select *
		from SobekCM_Portal_URL;

		OPEN cur_aggregation_links FOR
		select P.PortalID, A.Code
		from SobekCM_Portal_URL P, SobekCM_Portal_Item_Aggregation_Link AL, SobekCM_Item_Aggregation A
		where ( P.PortalID = AL.PortalID )
		  and ( AL.AggregationID = A.AggregationID );

		OPEN cur_webskin_links FOR
		select P.PortalID, W.WebSkinCode
		from SobekCM_Portal_URL P, SobekCM_Portal_Web_Skin_Link WL, SobekCM_Web_Skin W
		where ( P.PortalID = WL.PortalID )
		  and ( WL.WebSkinID = W.WebSkinID );
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Get_Available_OpenPublishing_Themes(
	p_id integer
)
RETURNS TABLE (
	ThemeID integer,
	ThemeName varchar(100),
	Location varchar(255),
	Author varchar(100),
	Description varchar(2000),
	Image varchar(255),
	AvailableForSelection boolean,
	"Default" boolean
)
LANGUAGE sql
AS $$
	select ThemeID, ThemeName, Location, coalesce(Author,'') as Author, coalesce(Description,'') as Description, coalesce(Image, '') as Image, AvailableForSelection, "Default"
	from SobekCM_OpenPublishing_Theme
	where AvailableForSelection='true';
$$;


-- Allows a lookup of the BibID/VID for an item from the database's primary key.
-- This is used for legacy URLs which may reference the item by itemid.
CREATE OR REPLACE FUNCTION SobekCM_Get_BibID_VID_From_ItemID(
	p_itemid integer
)
RETURNS TABLE (
	BibID varchar(10),
	VID varchar(5)
)
LANGUAGE sql
AS $$
	select G.BibID, I.VID
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID )
	  and ( G.Deleted = false )
	  and ( I.Deleted = false )
	  and ( I.ItemID = p_itemid );
$$;


-- Get the build errors between two dates.  Due to the date comparison, the
-- second date should really be midnight on the NEXT day.  So, if you want all
-- the build errors between 1/1/2000 and 1/2/2000, the datetimes you should use
-- would be '1/1/2000' and '1/3/2000'.
CREATE OR REPLACE FUNCTION SobekCM_Get_Build_Error_Logs(
	p_firstdate timestamp,
	p_seconddate timestamp
)
RETURNS TABLE (
	BibID varchar(10),
	VID varchar(5),
	METS_Type varchar(50),
	ErrorDescription text,
	"Date" timestamp
)
LANGUAGE sql
AS $$
	select BibID, VID, coalesce(METS_Type,'') as METS_Type, coalesce(ErrorDescription,'') as ErrorDescription, "Date"
	from SobekCM_Item_Error_Log
	where ( length(coalesce(ClearedBy,'')) = 0 )
	  and ( ClearedDate is null )
      and ( "Date" >= p_firstdate )
	  and ( "Date" < p_seconddate )
	order by "Date" DESC;
$$;


-- Gets the lists of all item aggregation codes
CREATE OR REPLACE FUNCTION SobekCM_Get_Codes()
RETURNS TABLE (
	Code varchar(20),
	"Type" varchar(50),
	Name varchar(250),
	ShortName varchar(100),
	isActive boolean,
	Hidden boolean,
	AggregationID integer,
	Description varchar(2000),
	ThematicHeadingID integer,
	External_URL varchar(500),
	DateAdded timestamp
)
LANGUAGE sql
AS $$
	SELECT Code, Type, Name, coalesce(ShortName, Name) as ShortName, isActive, Hidden, AggregationID, coalesce(Description,'') as Description, coalesce(ThematicHeadingID, -1 ) as ThematicHeadingID, coalesce(External_Link,'') as External_URL, DateAdded
	FROM SobekCM_Item_Aggregation AS P
	WHERE Deleted = 'false'
	order by Code;
$$;


-- Return the hierarchies for all (non-institutional) aggregations
-- starting with the 'ALL' aggregation
-- Version 3.05 - Added check to exclude DELETED aggregations
--
-- Originally returned 2 result sets built by repeatedly TRUNCATEing and
-- repopulating a single #TEMP_CHILDREN_BUILDER temp table between the two SELECTs.
-- That's unsafe once the results are returned via lazily-fetched refcursors (a later
-- TRUNCATE could run before the first cursor is actually fetched), so this port uses
-- two separate temp tables -- one per hierarchy level -- instead of reusing one.
CREATE OR REPLACE FUNCTION SobekCM_Get_Collection_Hierarchies(
	OUT cur_collections refcursor,
	OUT cur_institutions refcursor
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_aggregationid integer;
BEGIN
	select AggregationID into v_aggregationid from SobekCM_Item_Aggregation where Code='ALL';

	CREATE TEMP TABLE temp_children_collection ( AggregationID integer, Code varchar(20), ParentCode varchar(20), Name varchar(255), "Type" varchar(50), ShortName varchar(100), isActive boolean, Hidden boolean, Parent_Name varchar(255), Parent_ShortName varchar(100), HierarchyLevel integer ) ON COMMIT DROP;

	insert into temp_children_collection ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, '' as ParentCode, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, '', '', -1
	from SobekCM_Item_Aggregation AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( P.AggregationID = v_aggregationid )
	  and ( C.Deleted = 'false' )
	  and ( C.Type not like 'Institution%' );

	insert into temp_children_collection ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -2
	from temp_children_collection AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( HierarchyLevel = -1 )
	  and ( C.Deleted = 'false' );

	insert into temp_children_collection ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -3
	from temp_children_collection AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( HierarchyLevel = -2 )
	  and ( C.Deleted = 'false' );

	insert into temp_children_collection ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -4
	from temp_children_collection AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( HierarchyLevel = -3 )
	  and ( C.Deleted = 'false' );

	insert into temp_children_collection ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -5
	from temp_children_collection AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( HierarchyLevel = -4 )
	  and ( C.Deleted = 'false' );

	OPEN cur_collections FOR
	select Code, ParentCode, Name, ShortName, "Type", HierarchyLevel, isActive, Hidden, Parent_Name, Parent_ShortName
	from temp_children_collection
	order by HierarchyLevel DESC, Name;

	CREATE TEMP TABLE temp_children_institution ( AggregationID integer, Code varchar(20), ParentCode varchar(20), Name varchar(255), "Type" varchar(50), ShortName varchar(100), isActive boolean, Hidden boolean, Parent_Name varchar(255), Parent_ShortName varchar(100), HierarchyLevel integer ) ON COMMIT DROP;

	insert into temp_children_institution ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, '' as ParentCode, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, '', '', -1
	from SobekCM_Item_Aggregation AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( P.AggregationID = v_aggregationid )
	  and ( C.Deleted = 'false' )
	  and ( C.Type like 'Institution%' );

	insert into temp_children_institution ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -2
	from temp_children_institution AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( HierarchyLevel = -1 )
	  and ( C.Deleted = 'false' );

	insert into temp_children_institution ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -3
	from temp_children_institution AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( HierarchyLevel = -2 )
	  and ( C.Deleted = 'false' );

	insert into temp_children_institution ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -4
	from temp_children_institution AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( HierarchyLevel = -3 )
	  and ( C.Deleted = 'false' );

	insert into temp_children_institution ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, Parent_Name, Parent_ShortName, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, P.Name, P.ShortName, -5
	from temp_children_institution AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( HierarchyLevel = -4 )
	  and ( C.Deleted = 'false' );

	OPEN cur_institutions FOR
	select Code, ParentCode, Name, ShortName, "Type", HierarchyLevel, isActive, Hidden, Parent_Name, Parent_ShortName
	from temp_children_institution
	order by HierarchyLevel DESC, Name;
END;
$$;

-- Return the usage statistical information about a single item aggregation (or collection).
-- If the code is 'ALL', then the usage stats are aggregated up for all aggregations and
-- all items within this system.
CREATE OR REPLACE FUNCTION SobekCM_Get_Collection_Statistics_History(
	p_code varchar(20)
)
RETURNS TABLE (
	"Year" integer,
	"Month" integer,
	Hits bigint,
	"Sessions" bigint,
	Home_Page_Views bigint,
	Browse_Views bigint,
	Advanced_Search_Views bigint,
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
	Item_Download_Views bigint,
	Item_Static_Views bigint
)
LANGUAGE plpgsql
AS $$
BEGIN
	-- Should this pull all the data for ALL collections?  This is a lot more work
	-- since data is not naturally aggregated up for ALL aggregations, but rather each
	-- individual aggregation.  The web application should be caching this by writing
	-- a small file, so that this is pulled only once a day or so...
	if (( length(p_code) = 0 ) or ( p_code = 'ALL' )) then
		CREATE TEMP TABLE temp_item_stats AS
		select "Year", "Month", sum( Hits ) as Item_Hits,
			sum( JPEG_Views ) as Item_JPEG_Views, sum ( Zoomable_Views ) as Item_Zoomable_Views,
			sum ( Citation_Views ) as Item_Citation_Views, sum ( Thumbnail_Views ) as Item_Thumbnail_Views,
			sum ( Text_Search_Views ) as Item_Text_Search_Views, sum ( Flash_Views ) as Item_Flash_Views,
			sum ( Google_Map_Views) as Item_Google_Map_Views, sum( Download_Views ) as item_Download_Views,
			sum ( Static_Views) as Item_Static_Views
		from SobekCM_Item_Statistics
		group by "Year", "Month";

		CREATE TEMP TABLE temp_group_stats AS
		select "Year", "Month", sum( Hits ) as Title_Hits
		from SobekCM_Item_Group_Statistics
		group by "Year", "Month";

		CREATE TEMP TABLE temp_hierarchy_stats AS
		select "Year", "Month", sum( Home_Page_Views ) as Home_Page_Views,
			sum( Browse_Views ) as Browse_Views, sum ( Advanced_Search_Views ) as Advanced_Search_Views,
			sum ( Search_Results_Views ) as Search_Results_Views
		from SobekCM_Item_Aggregation_Statistics
		group by "Year", "Month";

		CREATE TEMP TABLE temp_url_stats AS
		select "Year", "Month", sum( Hits ) as Hits, sum( "Sessions" ) as "Sessions"
		from SobekCM_Statistics
		group by "Year", "Month";

		RETURN QUERY
		select T3."Year", T3."Month", T2.Hits, T2."Sessions", T3.Home_Page_Views, T3.Browse_Views, T3.Advanced_Search_Views, T3.Search_Results_Views, T4.Title_Hits, T1.Item_Hits, T1.Item_JPEG_Views, T1.Item_Zoomable_Views, T1.Item_Citation_Views, T1.Item_Thumbnail_Views, T1.Item_Text_Search_Views, T1.Item_Flash_Views, T1.Item_Google_Map_Views, T1.item_Download_Views, T1.Item_Static_Views
		from temp_hierarchy_stats AS T3 LEFT OUTER JOIN
			 temp_item_stats AS T1 ON (( T3."Year" = T1."Year" ) and ( T3."Month" = T1."Month" )) LEFT OUTER JOIN
			 temp_url_stats AS T2 ON (( T3."Year" = T2."Year" ) and ( T3."Month" = T2."Month" )) LEFT OUTER JOIN
			 temp_group_stats AS T4 ON (( T3."Year" = T4."Year" ) and ( T3."Month" = T4."Month" ))
		order by T3."Year", T3."Month";

		drop table temp_item_stats;
		drop table temp_group_stats;
		drop table temp_url_stats;
		drop table temp_hierarchy_stats;
	else
		-- Since this is for a single aggregation, simply return the data from the
		-- aggregation statistics table
		RETURN QUERY
		select S."Year", S."Month", S.Hits, S."Sessions", S.Home_Page_Views, S.Browse_Views, S.Advanced_Search_Views, S.Search_Results_Views, S.Title_Hits, S.Item_Hits, S.Item_JPEG_Views, S.Item_Zoomable_Views, S.Item_Citation_Views, S.Item_Thumbnail_Views, S.Item_Text_Search_Views, S.Item_Flash_Views, S.Item_Google_Map_Views, S.Item_Download_Views, S.Item_Static_Views
		from SobekCM_Item_Aggregation_Statistics S, SobekCM_Item_Aggregation C
		where ( C.Code = p_code )
		  and ( C.AggregationID = S.AggregationID )
		order by S."Year", S."Month";
	end if;
END;
$$;


-- Returns the list of any descriptive tags entered by users and
-- linked to an item aggregation.  If no code, or 'ALL', is passed in
-- as the argument, then all descriptive tags are returned.
CREATE OR REPLACE FUNCTION SobekCM_Get_Description_Tags_By_Aggregation(
	p_aggregationcode varchar(20)
)
RETURNS TABLE (
	FirstName varchar(50),
	NickName varchar(50),
	LastName varchar(50),
	BibID varchar(10),
	VID varchar(5),
	Description_Tag varchar(2000),
	TagID integer,
	Date_Modified timestamp,
	UserID integer
)
LANGUAGE plpgsql
AS $$
BEGIN
	if (( length( p_aggregationcode) > 0 ) and ( p_aggregationcode != 'ALL' )) then
		RETURN QUERY
		select U.FirstName, U.NickName, U.LastName, G.BibID, I.VID, T.Description_Tag, T.TagID, T.Date_Modified, U.UserID
		from mySobek_User U, mySobek_User_Description_Tags T, SobekCM_Item I, SobekCM_Item_Group G, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A
		where ( I.ItemID = L.ItemID )
		  and ( L.AggregationID = A.AggregationID )
		  and ( A.Code = p_aggregationcode )
		  and ( I.GroupID = G.GroupID )
		  and ( T.ItemID = I.ItemID )
		  and ( T.UserID = U.UserID )
		order by T.Date_Modified DESC;
	else
		RETURN QUERY
		select U.FirstName, U.NickName, U.LastName, G.BibID, I.VID, T.Description_Tag, T.TagID, T.Date_Modified, U.UserID
		from mySobek_User U, mySobek_User_Description_Tags T, SobekCM_Item I, SobekCM_Item_Group G
		where ( I.GroupID = G.GroupID )
		  and ( T.ItemID = I.ItemID )
		  and ( T.UserID = U.UserID )
		order by T.Date_Modified DESC;
	end if;
END;
$$;


-- Gets an email from the email logging system, by the primary key for the Email.
-- This also includes any responses to this original email.
-- Originally returned 2 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_Get_Email(
	p_EmailID integer,
	OUT cur_email refcursor,
	OUT cur_responses refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_email FOR select * from SobekCM_Email_Log where EmailID=p_EmailID;
	OPEN cur_responses FOR select * from SobekCM_Email_Log where ReplyToEmailID=p_EmailID;
END;
$$;


-- Returns the list of emails from the email logging system.
-- p_Include_All_Types - if TRUE, all emails returned, otherwise just the 'Contact Us' emails
CREATE OR REPLACE FUNCTION SobekCM_Get_Email_List(
	p_Include_All_Types boolean,
	p_Top100_Only boolean
)
RETURNS TABLE (
	EmailID integer,
	Sender varchar(100),
	Receipt_List text,
	Subject_Line varchar(255),
	Sent_Date timestamp,
	Preview text,
	HTML_Format boolean,
	Contact_Us boolean,
	ReplyToEmailID integer
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ( p_Include_All_Types ) then
		if ( p_Top100_Only ) then
			RETURN QUERY
			select EmailID, Sender, Receipt_List, Subject_Line, Sent_Date, SUBSTRING(Email_Body,1,500) as Preview, HTML_Format, Contact_Us, coalesce(ReplyToEmailID, -1) as ReplyToEmailID
			from SobekCM_Email_Log
			order by Sent_Date DESC
			limit 100;
		else
			RETURN QUERY
			select EmailID, Sender, Receipt_List, Subject_Line, Sent_Date, SUBSTRING(Email_Body,1,500) as Preview, HTML_Format, Contact_Us, coalesce(ReplyToEmailID, -1) as ReplyToEmailID
			from SobekCM_Email_Log
			order by Sent_Date DESC;
		end if;
	else
		if ( p_Top100_Only ) then
			RETURN QUERY
			select EmailID, Sender, Receipt_List, Subject_Line, Sent_Date, SUBSTRING(Email_Body,1,500) as Preview, HTML_Format, Contact_Us, coalesce(ReplyToEmailID, -1) as ReplyToEmailID
			from SobekCM_Email_Log
			where Contact_Us = 'true'
			order by Sent_Date DESC
			limit 100;
		else
			RETURN QUERY
			select EmailID, Sender, Receipt_List, Subject_Line, Sent_Date, SUBSTRING(Email_Body,1,500) as Preview, HTML_Format, Contact_Us, coalesce(ReplyToEmailID, -1) as ReplyToEmailID
			from SobekCM_Email_Log
			where Contact_Us = 'true'
			order by Sent_Date DESC;
		end if;
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Get_Group_Titles_All()
RETURNS TABLE (
	BibID varchar(10),
	GroupTitle varchar(500),
	GroupThumbnail varchar(100)
)
LANGUAGE sql
AS $$
	select G.BibID, coalesce(G.GroupTitle, '') as GroupTitle, coalesce(G.GroupThumbnail,'') as GroupThumbnail
	from SobekCM_Item_Group G
	where G.Deleted='false';
$$;


-- Return details on an IP restriction range, including all of the individual IPs included.
-- Originally returned 2 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_Get_IP_Restriction_Range(
	p_ip_rangeid integer,
	OUT cur_range refcursor,
	OUT cur_singles refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_range FOR
	select *
	from SobekCM_IP_Restriction_Range
	where IP_RangeID = p_ip_rangeid;

	OPEN cur_singles FOR
	select IP_SingleID, StartIP, coalesce(EndIP,'') as EndIP, coalesce(Notes,'') as Notes
	from SobekCM_IP_Restriction_Single
	where IP_RangeID = p_ip_rangeid
	order by StartIP ASC;
END;
$$;


-- Gets all of the information about a single item aggregation.
-- Version 5 - Stopped returning the metadata fields that have data (need to hit solr).
-- Originally returned 8 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_Get_Item_Aggregation(
	p_code varchar(20),
	OUT cur_main refcursor,
	OUT cur_children refcursor,
	OUT cur_parents refcursor,
	OUT cur_bounds refcursor,
	OUT cur_settings refcursor,
	OUT cur_result_views refcursor,
	OUT cur_facets refcursor,
	OUT cur_result_fields refcursor
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_aggregationid integer;
	v_last_added_date timestamp;
	v_has_new_items boolean;
BEGIN
	v_aggregationid := coalesce((select AggregationID from SobekCM_Item_Aggregation AS C where C.Code = p_code and Deleted=false), -1 );

	select MAX(MadePublicDate) into v_last_added_date from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L where I.ItemID=L.ItemID and I.Dark='false' and I.IP_Restriction_Mask >= 0 and L.AggregationID=v_aggregationid;

	v_has_new_items := false;
	if ( coalesce(v_last_added_date, '1900-01-01' ) > now() - interval '14 days') then
		v_has_new_items := true;
	end if;

	OPEN cur_main FOR
	select AggregationID, Code, Name, coalesce(ShortName,Name) AS ShortName, Type, isActive, Hidden, v_has_new_items as HasNewItems,
	   ContactEmail, DefaultInterface, Description, Map_Display, Map_Search, OAI_Flag, OAI_Metadata, DisplayOptions, coalesce(v_last_added_date, '1900-01-01' ) as LastItemAdded,
	   Can_Browse_Items, Items_Can_Be_Described, External_Link, T.ThematicHeadingID, LanguageVariants, ThemeName, GroupResults
	from SobekCM_Item_Aggregation AS C left outer join
	     SobekCM_Thematic_Heading as T on C.ThematicHeadingID=T.ThematicHeadingID
	where C.AggregationID = v_aggregationid;

	CREATE TEMP TABLE temp_agg_children ( AggregationID integer, Code varchar(20), ParentCode varchar(20), Name varchar(255), "Type" varchar(50), ShortName varchar(100), isActive boolean, Hidden boolean, HierarchyLevel integer ) ON COMMIT DROP;

	insert into temp_agg_children ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, HierarchyLevel )
	select C.AggregationID, C.Code, p_code as ParentCode, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, -1
	from SobekCM_Item_Aggregation AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( P.AggregationID = v_aggregationid )
	  and ( C.Deleted = 'false' );

	insert into temp_agg_children ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, -2
	from temp_agg_children AS P INNER JOIN
			SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( HierarchyLevel = -1 )
	  and ( C.Deleted = 'false' );

	insert into temp_agg_children ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, -3
	from temp_agg_children AS P INNER JOIN
			SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( HierarchyLevel = -2 )
	  and ( C.Deleted = 'false' );

	insert into temp_agg_children ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, -4
	from temp_agg_children AS P INNER JOIN
			SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( HierarchyLevel = -3 )
	  and ( C.Deleted = 'false' );

	OPEN cur_children FOR
	select Code, ParentCode, Name, ShortName, "Type", HierarchyLevel, isActive, Hidden
	from temp_agg_children
	order by HierarchyLevel, Code ASC;

	OPEN cur_parents FOR
	select Code, Name, ShortName, Type, isActive
	from SobekCM_Item_Aggregation A, SobekCM_Item_Aggregation_Hierarchy H
	where A.AggregationID = H.ParentID
	  and H.ChildID = v_aggregationid
	  and A.Deleted = 'false';

	OPEN cur_bounds FOR
	select Min(F.Point_Latitude) as Min_Latitude, Max(F.Point_Latitude) as Max_Latitude, Min(F.Point_Longitude) as Min_Longitude, Max(F.Point_Longitude) as Max_Longitude
	from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Footprint F
	where ( I.ItemID = L.ItemID  )
	  and ( L.AggregationID = v_aggregationid )
	  and ( F.ItemID = I.ItemID )
	  and ( F.Point_Latitude is not null )
	  and ( F.Point_Longitude is not null )
	  and ( I.Dark = 'false' );

	OPEN cur_settings FOR
	select Setting_Key, Setting_Value
	from SobekCM_Item_Aggregation_Settings
	where AggregationID=v_aggregationid;

	CREATE TEMP TABLE agg_resultviews ON COMMIT DROP AS
	select T.ResultType, A.DefaultView, A.ItemAggregationResultTypeID, ItemAggregationResultID, T.DefaultOrder
	from SobekCM_Item_Aggregation_Result_Views A, SobekCM_Item_Aggregation_Result_Types T
	where A.AggregationID=v_aggregationid
	  and A.ItemAggregationResultTypeID=T.ItemAggregationResultTypeID;

	OPEN cur_result_views FOR
	select ResultType, DefaultView
	from agg_resultviews
	order by DefaultOrder ASC;

	OPEN cur_facets FOR
	select F.MetadataTypeID, coalesce(F.OverrideFacetTerm, T.FacetTerm) as FacetTerm, T.SobekCode, T.SolrCode_Facets
	from SobekCM_Item_Aggregation_Facets F, SobekCM_Metadata_Types T
	where ( F.AggregationID = v_aggregationid )
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	order by FacetOrder;

	OPEN cur_result_fields FOR
	select A.ResultType, F.MetadataTypeID, coalesce(F.OverrideDisplayTerm, T.DisplayTerm) as DisplayTerm, T.SobekCode, T.SolrCode_Display, F.DisplayOrder, 'Custom' as Source
	from SobekCM_Item_Aggregation_Result_Fields F, SobekCM_Metadata_Types T, agg_resultviews A
	where ( A.ItemAggregationResultID = F.ItemAggregationResultID )
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	union
	select A.ResultType, F.MetadataTypeID, coalesce(F.OverrideDisplayTerm, T.DisplayTerm) as DisplayTerm, T.SobekCode, T.SolrCode_Display, F.DisplayOrder, 'Default' as Source
	from SobekCM_Item_Aggregation_Default_Result_Fields F, SobekCM_Metadata_Types T, agg_resultviews A
	where ( A.ItemAggregationResultTypeID = F.ItemAggregationResultTypeID )
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	  and ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Fields X where A.ItemAggregationResultID = X.ItemAggregationResultID ))
	order by A.ResultType, DisplayOrder;
END;
$$;

CREATE OR REPLACE FUNCTION SobekCM_Get_Item_Aggregation_Aliases()
RETURNS TABLE (
	AggregationAliasID integer,
	AggregationAlias varchar(50),
	Code varchar(20)
)
LANGUAGE sql
AS $$
	select A.AggregationAliasID, A.AggregationAlias, C.Code
	from SobekCM_Item_Aggregation C, SobekCM_Item_Aggregation_Alias A
	where A.AggregationID = C.AggregationID
	order by AggregationAlias;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Get_Item_Aggregation_AllCodes()
RETURNS TABLE (
	Code varchar(20),
	"Type" varchar(50),
	Name varchar(250),
	ShortName varchar(100),
	isActive boolean,
	Hidden boolean,
	AggregationID integer,
	Description varchar(2000),
	ThematicHeadingID integer,
	External_URL varchar(500),
	DateAdded timestamp,
	LanguageVariants varchar(500),
	ThemeName varchar(100),
	ParentShortName varchar(100),
	ParentName varchar(250),
	ParentCode varchar(20)
)
LANGUAGE sql
AS $$
	SELECT P.Code, P.Type, P.Name, coalesce(P.ShortName, P.Name) as ShortName, P.isActive, P.Hidden, P.AggregationID,
	       coalesce(P.Description,'') as Description, coalesce(T.ThematicHeadingID, -1 ) as ThematicHeadingID,
		   coalesce(P.External_Link,'') as External_URL, P.DateAdded, P.LanguageVariants, T.ThemeName,
		   F.ShortName as ParentShortName, F.Name as ParentName, F.Code as ParentCode
	FROM SobekCM_Item_Aggregation AS P left outer join
	     SobekCM_Thematic_Heading as T on P.ThematicHeadingID=T.ThematicHeadingID left outer join
		 SobekCM_Item_Aggregation_Hierarchy as H on H.ChildID=P.AggregationID left outer join
		 SobekCM_Item_Aggregation as F on F.AggregationID=H.ParentID
	WHERE P.Deleted = 'false'
	  and ( F.Deleted = 'false' or F.Deleted is null )
	order by P.Code;
$$;


-- Originally returned between 2 and 4 result sets depending on the p_include_counts /
-- p_is_robot flags (main info + children always; counts and parents conditionally).
-- Ported as RETURNS SETOF refcursor rather than fixed OUT parameters so the returned
-- cursor count can vary the same way -- EalDbAccess.cs's Postgres ExecuteDataset path
-- (pg_execute_dataset) handles either shape identically, fetching whatever cursors come back.
CREATE OR REPLACE FUNCTION SobekCM_Get_Item_Aggregation2(
	p_code varchar(20),
	p_include_counts boolean,
	p_is_robot boolean
)
RETURNS SETOF refcursor
LANGUAGE plpgsql
AS $$
DECLARE
	v_aggregationid integer;
	cur refcursor;
BEGIN
	CREATE TEMP TABLE temp_agg2_children ( AggregationID integer, Code varchar(20), ParentCode varchar(20), Name varchar(255), "Type" varchar(50), ShortName varchar(100), isActive boolean, Hidden boolean, HierarchyLevel integer ) ON COMMIT DROP;

	v_aggregationid := coalesce((select AggregationID from SobekCM_Item_Aggregation AS C where C.Code = p_code and Deleted=false), -1 );

	OPEN cur FOR
	select AggregationID, Code, Name, coalesce(ShortName,Name) AS ShortName, Type, isActive, Hidden, HasNewItems,
	   ContactEmail, DefaultInterface, Description, Map_Display, Map_Search, OAI_Flag, OAI_Metadata, DisplayOptions, LastItemAdded,
	   Can_Browse_Items, Items_Can_Be_Described, External_Link, ThematicHeadingID
	from SobekCM_Item_Aggregation AS C
	where C.AggregationID = v_aggregationid;
	RETURN NEXT cur;

	insert into temp_agg2_children ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, HierarchyLevel )
	select C.AggregationID, C.Code, p_code as ParentCode, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, -1
	from SobekCM_Item_Aggregation AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
	where ( P.AggregationID = v_aggregationid );

	-- If this is a robot, no need to go further
	if ( not p_is_robot ) then
		insert into temp_agg2_children ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, HierarchyLevel )
		select C.AggregationID, C.Code, P.Code, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, -2
		from temp_agg2_children AS P INNER JOIN
			 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
		where ( HierarchyLevel = -1 );

		insert into temp_agg2_children ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, HierarchyLevel )
		select C.AggregationID, C.Code, P.Code, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, -3
		from temp_agg2_children AS P INNER JOIN
			 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
		where ( HierarchyLevel = -2 );

		insert into temp_agg2_children ( AggregationID, Code, ParentCode, Name, "Type", ShortName, isActive, Hidden, HierarchyLevel )
		select C.AggregationID, C.Code, P.Code, C.Name, C.Type, coalesce(C.ShortName,C.Name) AS ShortName, C.isActive, C.Hidden, -4
		from temp_agg2_children AS P INNER JOIN
			 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID
		where ( HierarchyLevel = -3 );
	end if;

	OPEN cur FOR
	select Code, ParentCode, Name, ShortName, "Type", HierarchyLevel, isActive, Hidden
	from temp_agg2_children
	order by HierarchyLevel, Code ASC;
	RETURN NEXT cur;

	-- Check to see if the counts should be included
	if ( p_include_counts ) then
		OPEN cur FOR
		select count(distinct(I.GroupID)) as Title_Count, count(*) as Item_Count, coalesce(SUM(I.PageCount),0) as Page_Count
		from SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item I, SobekCM_Item_Aggregation A
		where ( A.Code = p_code )
		  and ( A.AggregationID = L.AggregationID )
		  and ( L.ItemID = I.ItemID );
		RETURN NEXT cur;
	end if;

	-- Return all the parents (if not robot)
	if ( not p_is_robot ) then
		OPEN cur FOR
		select Code, Name, ShortName, Type, isActive
		from SobekCM_Item_Aggregation A, SobekCM_Item_Aggregation_Hierarchy H
		where A.AggregationID = H.ParentID
		  and H.ChildID = v_aggregationid;
		RETURN NEXT cur;
	end if;

	RETURN;
END;
$$;


-- Get some basic information about an item, which is pulled from the database before the item
-- is displayed online.  Many of these values correspond to the item group/title or how this
-- item relates to the item group and any item aggregations within the system.
--
-- Originally returned 1 or 2 result sets depending on p_include_aggregations; ported as
-- RETURNS SETOF refcursor for the same reason as SobekCM_Get_Item_Aggregation2 above.
CREATE OR REPLACE FUNCTION SobekCM_Get_Item_Brief_Info(
	p_bibid varchar(10),
	p_vid varchar(5),
	p_include_aggregations boolean
)
RETURNS SETOF refcursor
LANGUAGE plpgsql
AS $$
DECLARE
	cur refcursor;
BEGIN
	OPEN cur FOR
	select G.BibID, I.VID, G.GroupTitle,
			coalesce(I.Level1_Text, '') as Level1_Text, coalesce( I.Level1_Index, 0 ) as Level1_Index,
			coalesce(I.Level2_Text, '') as Level2_Text, coalesce( I.Level2_Index, 0 ) as Level2_Index,
			coalesce(I.Level3_Text, '') as Level3_Text, coalesce( I.Level3_Index, 0 ) as Level3_Index,
			coalesce(I.PubDate,'') as PubDate, coalesce( I.SortDate,-1) as SortDate, G.File_Location || '/' || VID || '/' || coalesce( I.MainThumbnail,'') as MainThumbnail,
			I.Title, coalesce(I.Author,'') as Author, IP_Restriction_Mask, G.OCLC_Number, G.ALEPH_Number, coalesce(I.MainThumbnail,'') as MainThumbnailFile, coalesce(I.MainJPEG,'') as MainJPEGFile
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID )
	  and ( G.BibID = p_bibid )
	  and ( I.VID = p_vid );
	RETURN NEXT cur;

	if( p_include_aggregations ) then
		OPEN cur FOR
		select A.Code, A.Name, A.ShortName, A.Type, A.Map_Search, A.DisplayOptions, A.Items_Can_Be_Described, L.impliedLink, A.Hidden, A.isActive, coalesce(A.External_Link,'') as External_Link
		from SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A, SobekCM_Item I, SobekCM_Item_Group G
		where ( L.ItemID = I.ItemID )
		  and ( A.AggregationID = L.AggregationID )
		  and ( I.GroupID = G.GroupID )
	      and ( G.BibID = p_bibid )
	      and ( I.VID = p_vid );
		RETURN NEXT cur;
	end if;

	RETURN;
END;
$$;


-- Pull any additional item details for one bib/vid before showing this item.
-- Ver 5: Split the old SobekCM_Get_Item_Details2 stored procedure.
--
-- Originally returned a variable number of result sets: 1 (an "ErrorMsg" row) if the BibID or
-- VID doesn't exist, otherwise 9 detail result sets plus a 10th (related item groups) that
-- always runs. The C# caller (SobekCM_METS_Based_ItemBuilder) explicitly checks
-- `itemDetails.Tables.Count == 1` with an "ErrorMsg" column to detect the error case, so the
-- returned cursor count must genuinely vary -- ported as RETURNS SETOF refcursor rather than
-- fixed OUT parameters.
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
				I.CitationSet, I.MadePublicDate, I.RestrictionMessage
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

-- Originally returned 1 result set (an "ErrorMsg" row) on an invalid BibID, otherwise 5;
-- ported as RETURNS SETOF refcursor for the same reason as SobekCM_Get_Item_Details above.
-- The original also built an unused #TEMP_ICON temp table (populated, then never referenced --
-- the actual icons query below uses its own identical inline derived table instead); that dead
-- temp-table population is dropped here.
CREATE OR REPLACE FUNCTION SobekCM_Get_Item_Group_Details(
	p_BibID varchar(10)
)
RETURNS SETOF refcursor
LANGUAGE plpgsql
AS $$
DECLARE
	cur refcursor;
BEGIN
	if (not exists ( select 1 from SobekCM_Item_Group where BibID = p_BibID )) then
		OPEN cur FOR select 'INVALID BIBID' as ErrorMsg, '' as BibID, '' as VID;
		RETURN NEXT cur;
		RETURN;
	end if;

	OPEN cur FOR
	select GroupTitle, BibID, G.Type, G.File_Location, coalesce(AGGS.Code,'') AS Code, G.GroupID, coalesce(GroupThumbnail,'') as Thumbnail, G.Track_By_Month, G.Large_Format, G.Never_Overlay_Record, G.Primary_Identifier_Type, G.Primary_Identifier
	from SobekCM_Item_Group AS G LEFT JOIN
		 ( select distinct(A.Code),  G2.GroupID
		   from SobekCM_Item_Group G2, SobekCM_Item IL, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A
	       where IL.ItemID=L.ItemID
	         and A.AggregationID=L.AggregationID
	         and G2.GroupID=IL.GroupID
	         and G2.BibID=p_BibID
	         and G2.Deleted='false'
	       group by A.Code, G2.GroupID ) AS AGGS ON G.GroupID=AGGS.GroupID
	where ( G.BibID = p_BibID )
	  and ( G.Deleted = 'false' );
	RETURN NEXT cur;

	OPEN cur FOR
	select Icon_URL, Link, Icon_Name, Title
	from SobekCM_Icon I, (	select distinct(IconID)
							from SobekCM_Item_Icons II, SobekCM_Item It, SobekCM_Item_Group G
							where ( It.GroupID = G.GroupID )
						 	  and ( G.BibID = p_bibid )
							  and ( It.Deleted = false )
							  and ( II.ItemID = It.ItemID )
							group by IconID) AS T
	where ( T.IconID = I.IconID );
	RETURN NEXT cur;

	OPEN cur FOR
	select S.WebSkinCode
	from SobekCM_Item_Group_Web_Skin_Link L, SobekCM_Item_Group G, SobekCM_Web_Skin S
	where ( L.GroupID = G.GroupID )
	  and ( L.WebSkinID = S.WebSkinID )
	  and ( G.BibID = p_BibID )
	order by L.Sequence;
	RETURN NEXT cur;

	OPEN cur FOR
	select distinct( Code )
	from SobekCM_Item_Aggregation A, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Group G, SobekCM_Item I
	where ( I.ItemID = L.ItemID )
	  and ( I.GroupID = G.GroupID )
	  and ( G.BibID = p_BibID )
	  and ( L.AggregationID = A.AggregationID );
	RETURN NEXT cur;

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


CREATE OR REPLACE FUNCTION SobekCM_Get_Item_Restrictions(
	p_bibid varchar(10),
	p_vid varchar(5)
)
RETURNS TABLE (
	IP_Restriction_Mask smallint,
	Dark boolean
)
LANGUAGE sql
AS $$
	select IP_Restriction_Mask, Dark
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.VID = p_vid )
	  and ( I.GroupID = G.GroupID )
	  and ( G.BibID=p_bibid)
	  and ( I.Deleted = 'false' )
	  and ( G.Deleted = 'false' );
$$;


-- Pull any additional item details before showing this item
CREATE OR REPLACE FUNCTION SobekCM_Get_Item_Statistics(
	p_BibID varchar(10),
	p_VID varchar(5)
)
RETURNS TABLE (
	"Year" integer,
	"Month" integer,
	Title_Views bigint,
	Title_Visitors bigint,
	Views bigint,
	Visitors bigint
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_itemid integer;
	v_groupid integer;
BEGIN
	v_itemid := coalesce( ( select I.ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID=G.GroupID and I.VID=p_vid and G.BibiD=p_bibid ), -1 );
	v_groupid := coalesce( ( select G.GroupID from SobekCM_Item_Group G where G.BibiD=p_bibid ), -1 );

	RETURN QUERY
	with item_month_years ("Month", "Year") as
	(
		select "Month", "Year" from SobekCM_Item_Group_Statistics G where G.GroupID=v_groupID
		union
		select "Month", "Year" from SobekCM_Item_Statistics I where I.ItemID=v_itemid
	)
	select M."Year", M."Month", coalesce(G.Hits,0) as Title_Views, coalesce(G."Sessions",0) as Title_Visitors, coalesce(I.Hits,0) as Views, coalesce(I."Sessions",0) as Visitors
	from item_month_years M left outer join
		 SobekCM_Item_Statistics as I on I."Month"=M."Month" and I."Year"=M."Year" and I.ItemID=v_itemid left outer join
		 SobekCM_Item_Group_Statistics as G on M."Month"=G."Month" and M."Year"=G."Year" and G.GroupID=v_groupid
	order by "Year" ASC, "Month" ASC;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Get_Item_Viewers(
	p_bibid varchar(10),
	p_vid varchar(5)
)
RETURNS TABLE (
	ViewType varchar(50),
	Attribute varchar(250),
	Label varchar(50),
	MenuOrder double precision,
	Exclude boolean,
	"Order" integer
)
LANGUAGE sql
AS $$
	select T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder) as MenuOrder, V.Exclude, coalesce(V.OrderOverride, T."Order") as "Order"
	from SobekCM_Item_Viewers V, SobekCM_Item_Viewer_Types T, SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID )
	  and ( G.BibID = p_bibid )
	  and ( I.VID = p_vid )
	  and ( V.ItemID = I.ItemID )
	  and ( V.ItemViewTypeID = T.ItemViewTypeID )
	group by T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder), V.Exclude, coalesce(V.OrderOverride, T."Order")
	order by coalesce(V.OrderOverride, T."Order") ASC;
$$;


-- Procedure returns the item id as a single row given the bibid and vid.
-- This also doubles as a quick way to check if a certain item exists in
-- the database and is employed by some of the workflow tools
CREATE OR REPLACE FUNCTION SobekCM_Get_ItemID(
	p_bibid varchar(10),
	p_vid varchar(5)
)
RETURNS TABLE (
	ItemID integer
)
LANGUAGE sql
AS $$
	select ItemID
	from SobekCM_Item I, SobekCM_Item_Group G
	where I.GroupID=G.GroupID
	  and I.VID=p_vid
	  and G.BibID=p_bibid;
$$;


-- Gets the list of items currently flagged for needing additional work.
-- This is used by the builder to determine what needs post-processing.
CREATE OR REPLACE FUNCTION SobekCM_Get_Items_Needing_Aditional_Work()
RETURNS TABLE (
	BibID varchar(10),
	VID varchar(5),
	ItemID integer
)
LANGUAGE sql
AS $$
	select G.BibID, I.VID, I.ItemID
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID )
	  and ( I.AdditionalWorkNeeded = 'true' )
	order by BibID, VID;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Get_Last_Open_Workflow_By_ItemID(
	p_ItemID integer,
	p_EventNumber integer
)
RETURNS TABLE (
	ItemID integer,
	ProgressID integer,
	WorkFlowName varchar(100),
	Start_Event_Desc varchar(255),
	End_Event_Desc varchar(255),
	Start_Event_Number integer,
	End_Event_Number integer,
	Start_And_End_Event_Number integer,
	DateStarted timestamp,
	DateCompleted timestamp,
	RelatedEquipment varchar(255),
	WorkPerformedBy varchar(100),
	WorkingFilePath varchar(500),
	ProgressNote varchar(2000)
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_workflowid integer;
BEGIN
	v_workflowid := coalesce((select WorkFlowID from Tracking_Workflow where Start_Event_Number = p_EventNumber or End_Event_Number = p_EventNumber ), -1);

	if ( v_workflowid > 0 ) then
		RETURN QUERY
		select P.ItemID,P.ProgressID, W.WorkFlowName, W.Start_Event_Desc, W.End_Event_Desc, W.Start_Event_Number, W.End_Event_Number, W.Start_And_End_Event_Number,
		       P.DateStarted, P.DateCompleted, P.RelatedEquipment, P.WorkPerformedBy, P.WorkingFilePath, P.ProgressNote
		from Tracking_Progress P, Tracking_Workflow W
		where ItemID = p_ItemID
		  and P.WorkFlowID = v_workflowid
		  and P.WorkFlowID = W.WorkFlowID
		  and ( DateCompleted is null );
	end if;
END;
$$;


-- Return the list of all metadata searchable fields
CREATE OR REPLACE FUNCTION SobekCM_Get_Metadata_Fields()
RETURNS SETOF SobekCM_Metadata_Types
LANGUAGE sql
AS $$
	select *
	from SobekCM_Metadata_Types
	order by DisplayTerm;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Get_Mime_Types()
RETURNS TABLE (
	Extension varchar(20),
	MimeType varchar(100),
	isBlocked boolean,
	shouldForward boolean,
	MimeTypeID integer
)
LANGUAGE sql
AS $$
	select Extension, MimeType, isBlocked, shouldForward, MimeTypeID
	from SobekCM_Mime_Types;
$$;


-- Stored procedure returns the information about all the items within a single
-- title or item/group
CREATE OR REPLACE FUNCTION SobekCM_Get_Multiple_Volumes(
	p_bibid varchar(10)
)
RETURNS TABLE (
	ItemID integer,
	Title varchar(500),
	Level1_Text varchar(255),
	Level1_Index integer,
	Level2_Text varchar(255),
	Level2_Index integer,
	Level3_Text varchar(255),
	Level3_Index integer,
	Level4_Text varchar(255),
	Level4_Index integer,
	Level5_Text varchar(255),
	Level5_Index integer,
	MainThumbnail varchar(100),
	VID varchar(5),
	IP_Restriction_Mask smallint
)
LANGUAGE sql
AS $$
	select I.ItemID, Title, coalesce(Level1_Text,'') as Level1_Text, coalesce(Level1_Index,-1) as Level1_Index, coalesce(Level2_Text, '') as Level2_Text, coalesce(Level2_Index, -1) as Level2_Index, coalesce(Level3_Text, '') as Level3_Text, coalesce(Level3_Index, -1) as Level3_Index, coalesce(Level4_Text, '') as Level4_Text, coalesce(Level4_Index, -1) as Level4_Index, coalesce(Level5_Text, '') as Level5_Text, coalesce(Level5_Index,-1) as Level5_Index, I.MainThumbnail, I.VID, I.IP_Restriction_Mask
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( G.GroupID = I.GroupID )
	  and ( G.BibID = p_bibid )
	  and ( I.Deleted = 'false' )
	  and ( G.Deleted = 'false' )
	order by Level1_Index ASC, Level2_Index ASC, Level3_Index ASC, Level4_Index ASC, Level5_Index ASC, Title ASC;
$$;

-- Gets the information about all the multi-volume titles
CREATE OR REPLACE FUNCTION SobekCM_Get_MultiVolume_Title_Info()
RETURNS TABLE (
	BibID varchar(10),
	CustomThumbnail varchar(100),
	FlagByte smallint,
	LastFourInt integer,
	GroupTitle varchar(500)
)
LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	with volume_count as
	(
		select BibID, count(*) as ItemCount
		from SobekCM_Item_Group G, SobekCM_Item I
		where G.GroupID = I.GroupID
		  and G.Deleted='false'
		  and I.Deleted='false'
		group by BibID
	)
	select G.BibID, G.CustomThumbnail, G.FlagByte, G.LastFourInt, coalesce(G.GroupTitle,'') as GroupTitle
	from SobekCM_Item_Group G, volume_count C
	where ( C.BibID=G.BibID )
	  and (( C.ItemCount > 1 ) or ( G.HasGroupMetadata = 'true' ));
END;
$$;


-- Return a list of the OAI data to serve through the OAI-PMH server
CREATE OR REPLACE FUNCTION SobekCM_Get_OAI_Data(
	p_aggregationcode varchar(20),
	p_data_code varchar(20),
	p_from date,
	p_until date,
	p_pagesize integer,
	p_pagenumber integer,
	p_include_data boolean
)
RETURNS TABLE (
	BibID varchar(10),
	VID varchar(5),
	OAI_Data text,
	OAI_Date date
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_rowstart integer;
	v_rowend integer;
	v_aggregationid integer;
BEGIN
	v_rowstart := (p_pagesize * ( p_pagenumber - 1 )) + 1;

	-- Rowend is calculated normally, but then an additional item is
	-- added at the end which will be used to determine if a resumption
	-- token should be issued
	v_rowend := (v_rowstart + p_pagesize - 1) + 1;

	if ( p_from is null ) then p_from := DATE '1900-01-01'; end if;
	if ( p_until is null ) then p_until := now(); end if;

	if (( p_aggregationcode is not null ) and ( length(p_aggregationcode) > 0 ) and ( p_aggregationcode != 'all' )) then
		v_aggregationid := ( select coalesce(AggregationID,-1) from SobekCM_Item_Aggregation where Code=p_aggregationcode );

		if ( p_include_data ) then
			RETURN QUERY
			with ITEMS_SELECT AS
			(	select BibID, I.ItemID, VID,
				ROW_NUMBER() OVER (order by O.OAI_Date ASC ) as RowNumber
				from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link CL, SobekCM_Item_Group G, SobekCM_Item_OAI O
				where ( CL.ItemID = I.ItemID )
				  and ( CL.AggregationID = v_aggregationid )
				  and ( I.GroupID = G.GroupID )
				  and ( I.ItemID = O.ItemID )
				  and ( G.Suppress_OAI = 'false' )
				  and ( O.OAI_Date >= p_from )
				  and ( O.OAI_Date <= p_until )
				  and ( O.Data_Code = p_data_code )
				  and ( I.Dark = 'false' )
				  and ( I.IP_Restriction_Mask = 0 )
			)
			select BibID, T.VID, O.OAI_Data, O.OAI_Date
			from ITEMS_SELECT T, SobekCM_Item_OAI O
			where RowNumber >= v_rowstart
			  and RowNumber <= v_rowend
			  and T.ItemID = O.ItemID
			  and O.Data_Code = p_data_code;
		else
			RETURN QUERY
			with ITEMS_SELECT AS
			(	select BibID, I.ItemID, VID,
				ROW_NUMBER() OVER (order by O.OAI_Date ASC ) as RowNumber
				from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link CL, SobekCM_Item_Group G, SobekCM_Item_OAI O
				where ( CL.ItemID = I.ItemID )
				  and ( CL.AggregationID = v_aggregationid )
				  and ( I.GroupID = G.GroupID )
				  and ( I.ItemID = O.ItemID )
				  and ( G.Suppress_OAI = 'false' )
				  and ( O.OAI_Date >= p_from )
				  and ( O.OAI_Date <= p_until )
				  and ( O.Data_Code = p_data_code )
				  and ( I.Dark = 'false' )
				  and ( I.IP_Restriction_Mask = 0 )
			)
			select BibID, T.VID, null::text as OAI_Data, O.OAI_Date
			from ITEMS_SELECT T, SobekCM_Item_OAI O
			where RowNumber >= v_rowstart
			  and RowNumber <= v_rowend
			  and T.ItemID = O.ItemID
			  and O.Data_Code = p_data_code;
		end if;
	else
		if ( p_include_data ) then
			RETURN QUERY
			with ITEMS_SELECT AS
			(	select BibID, I.ItemID, VID,
				ROW_NUMBER() OVER (order by O.OAI_Date ASC) as RowNumber
				from SobekCM_Item_Group G, SobekCM_Item I, SobekCM_Item_OAI O
				where ( G.GroupID = I.GroupID )
				  and ( I.ItemID = O.ItemID )
				  and ( G.Suppress_OAI = 'false' )
				  and ( O.OAI_Date >= p_from )
				  and ( O.OAI_Date <= p_until )
				  and ( O.Data_Code = p_data_code )
				  and ( I.Dark = 'false' )
				  and ( I.IP_Restriction_Mask = 0 )
			)
			select BibID, T.VID, O.OAI_Data, O.OAI_Date
			from ITEMS_SELECT T, SobekCM_Item_OAI O
			where RowNumber >= v_rowstart
			  and RowNumber <= v_rowend
			  and T.ItemID = O.ItemID
			  and O.Data_Code = p_data_code;
		else
			RETURN QUERY
			with ITEMS_SELECT AS
			(	select BibID, I.ItemID, VID,
				ROW_NUMBER() OVER (order by O.OAI_Date ASC) as RowNumber
				from SobekCM_Item_Group G, SobekCM_Item I, SobekCM_Item_OAI O
				where ( G.GroupID = I.GroupID )
				  and ( I.ItemID = O.ItemID )
				  and ( G.Suppress_OAI = 'false' )
				  and ( O.OAI_Date >= p_from )
				  and ( O.OAI_Date <= p_until )
				  and ( O.Data_Code = p_data_code )
				  and ( I.Dark = 'false' )
				  and ( I.IP_Restriction_Mask = 0 )
			)
			select BibID, T.VID, null::text as OAI_Data, O.OAI_Date
			from ITEMS_SELECT T, SobekCM_Item_OAI O
			where RowNumber >= v_rowstart
			  and RowNumber <= v_rowend
			  and T.ItemID = O.ItemID
			  and O.Data_Code = p_data_code;
		end if;
	end if;
END;
$$;


-- Returns the OAI data for a single item from the oai source tables
CREATE OR REPLACE FUNCTION SobekCM_Get_OAI_Data_Item(
	p_bibid varchar(10),
	p_vid varchar(5),
	p_data_code varchar(20)
)
RETURNS TABLE (
	GroupID integer,
	BibID varchar(10),
	OAI_Data text,
	OAI_Date date,
	VID varchar(5)
)
LANGUAGE sql
AS $$
	select G.GroupID, BibID, O.OAI_Data, O.OAI_Date, VID
	from SobekCM_Item_Group G, SobekCM_Item I, SobekCM_Item_OAI O
	where G.BibID = p_bibid
	  and G.GroupID = I.GroupID
	  and I.VID = p_vid
	  and I.ItemID = O.ItemID
	  and O.Data_Code = p_data_code;
$$;


-- Get the OAI set information from the database
-- This stored procedure is called from the UFDC Web
-- Written by Mark Sullivan (March, 2007)
CREATE OR REPLACE FUNCTION SobekCM_Get_OAI_Sets()
RETURNS TABLE (
	Code varchar(20),
	Name varchar(250),
	Description varchar(2000),
	LastItemAddedDate timestamp,
	OAI_Metadata text
)
LANGUAGE plpgsql
AS $$
BEGIN
	CREATE TEMP TABLE temp_oai_aggs AS
	select C.AggregationID, C.Code, C.Name, C.Description, coalesce(C.OAI_Metadata, '') as OAI_Metadata
	from SobekCM_Item_Aggregation C
	where ( C.isActive = true )
	  and ( C.OAI_Flag = true )
	  and ( C.Deleted = false )
	order by C.Code;

	RETURN QUERY
	select T.Code, T.Name, T.Description, MAX(I.CreateDate) as LastItemAddedDate, T.OAI_Metadata
	from temp_oai_aggs T, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item I
	where ( T.AggregationID = L.AggregationID )
      and ( L.ItemID = I.ItemID )
	group by Code, Name, Description, OAI_Metadata;

	drop table temp_oai_aggs;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Get_OpenPublishing_Theme(
	p_id integer
)
RETURNS TABLE (
	ThemeID integer,
	ThemeName varchar(100),
	Location varchar(255),
	Author varchar(100),
	Description varchar(2000),
	Image varchar(255),
	AvailableForSelection boolean,
	"Default" boolean
)
LANGUAGE sql
AS $$
	select ThemeID, ThemeName, Location, coalesce(Author,'') as Author, coalesce(Description,'') as Description, coalesce(Image, '') as Image, AvailableForSelection, "Default"
	from SobekCM_OpenPublishing_Theme
	where ThemeID=p_id;
$$;


-- Gets the list of all system-wide settings from the database, including the full list of all
-- metadata search fields, possible workflows, and all disposition data.
-- Originally returned 9 result sets; ported using OUT refcursor parameters.
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
		order by TabPage, Heading, Setting_Key;
	else
		OPEN cur_settings FOR
		select Setting_Key, Setting_Value
		from SobekCM_Settings;
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

-- Gets the year/month pairing for which this system appears to have
-- some usage statistics recorded.  This is for the drop-down select
-- boxes when viewing the usage statistics online
CREATE OR REPLACE FUNCTION SobekCM_Get_Statistics_Dates()
RETURNS TABLE (
	"Year" integer,
	"Month" integer
)
LANGUAGE sql
AS $$
	select "Year", "Month"
	from SobekCM_Statistics
	group by "Year", "Month";
$$;


CREATE OR REPLACE FUNCTION SobekCM_Get_Submittor(
	p_bibid varchar(20),
	p_vid varchar(10)
)
RETURNS TABLE (
	UserName text,
	EmailAddress varchar(100),
	UserID integer
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_itemid integer;
BEGIN
	select ItemID into v_itemid from SobekCM_Item_Group G, SobekCM_Item I where I.GroupID = G.GroupID and BibID=p_bibid and VID=p_vid;

	RETURN QUERY
	select U.FirstName || ' ' || U.LastName as UserName, U.EmailAddress, U.UserID
	from Tracking_Progress P inner join
		 Tracking_Workflow W on P.WorkFlowID=W.WorkFlowID inner join
		 mySobek_User U on U.UserID=P.WorkPerformedById
	where P.ItemID=v_itemid
	  and W.WorkFlowName='Online Submit';
END;
$$;


-- Stored procedure to get all the web skin information
CREATE OR REPLACE FUNCTION SobekCM_Get_Web_Skins()
RETURNS TABLE (
	WebSkinCode varchar(20),
	OverrideHeaderFooter boolean,
	OverrideBanner boolean,
	BannerLink varchar(255),
	BaseInterface varchar(20),
	Notes varchar(250),
	Build_On_Launch boolean,
	SuppressTopNavigation boolean
)
LANGUAGE sql
AS $$
	select WebSkinCode, coalesce(OverrideHeaderFooter,false) as OverrideHeaderFooter,
		coalesce(OverrideBanner, false) as OverrideBanner, coalesce(BannerLink,'') as BannerLink,
		coalesce(BaseWebSkin,'') as BaseInterface, coalesce(Notes,'') as Notes, Build_On_Launch,
		SuppressTopNavigation
	from SobekCM_Web_Skin
	order by WebSkinCode;
$$;


-- Returns the list of all icons used by the SobekCM web app
CREATE OR REPLACE FUNCTION SobekCM_Icon_List()
RETURNS TABLE (
	Icon_Name varchar(50),
	Icon_URL varchar(255),
	Link varchar(255),
	Title varchar(255)
)
LANGUAGE sql
AS $$
	select Icon_Name, Icon_URL, coalesce(Link,'') as Link, coalesce(Title,'') as Title
	from SobekCM_Icon;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Item_Count_By_Collection(
	p_option integer
)
RETURNS TABLE (
	code1 varchar(20),
	code2 varchar(20),
	code3 varchar(20),
	AllCodes varchar(20),
	Name varchar(255),
	Active text,
	title_count bigint,
	item_count bigint,
	page_count bigint
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_all_id integer;
	v_total_item_count integer;
	v_total_title_count integer;
	v_total_page_count integer;
BEGIN
	v_all_id := coalesce(( select AggregationID from SobekCM_Item_Aggregation where Code='all'), -1);

	CREATE TEMP TABLE temp_aggregation_list
	(
	  AggregationID integer,
	  Code varchar(20),
	  ChildCode varchar(20),
	  Child2Code varchar(20),
	  AllCodes varchar(20),
	  Name varchar(255),
	  ShortName varchar(100),
	  "Type" varchar(50),
	  isActive boolean
	) ON COMMIT DROP;

	insert into temp_aggregation_list ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, "Type", isActive )
	select AggregationID, Code, '', '', Code, Name, ShortName, Type, isActive
	from SobekCM_Item_Aggregation A
	where ( Type not like 'Institut%' )
	  and ( Deleted='false' )
	  and exists ( select * from SobekCM_Item_Aggregation_Hierarchy where ChildID=A.AggregationID and ParentID=v_all_id);

	insert into temp_aggregation_list ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, "Type", isActive )
	select A2.AggregationID, T.Code, A2.Code, '', A2.Code, A2.Name, A2.ShortName, A2.Type, A2.isActive
	from temp_aggregation_list T, SobekCM_Item_Aggregation A2, SobekCM_Item_Aggregation_Hierarchy H
	where ( A2.Type not like 'Institut%' )
	  and ( T.AggregationID = H.ParentID )
	  and ( A2.AggregationID = H.ChildID )
	  and ( Deleted='false' );

	insert into temp_aggregation_list ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, "Type", isActive )
	select A2.AggregationID, T.Code, T.ChildCode, A2.Code, A2.Code, A2.Name, A2.ShortName, A2.Type, A2.isActive
	from temp_aggregation_list T, SobekCM_Item_Aggregation A2, SobekCM_Item_Aggregation_Hierarchy H
	where ( A2.Type not like 'Institut%' )
	  and ( T.AggregationID = H.ParentID )
	  and ( A2.AggregationID = H.ChildID )
	  and ( Deleted='false' )
	  and ( ChildCode <> '' );

	if ( p_option = 1 ) then
		-- COUNT OF ALL ITEMS WITH SOME DIGITAL RESOURCES ATTACHED
		select count(*) into v_total_item_count from SobekCM_Item where Deleted = 'false' and (( FileCount > 0 ) or ( PageCount > 0 ));
		select count(*) into v_total_title_count from SobekCM_Item_Group G where G.Deleted = 'false' and exists ( select * from SobekCM_Item I where I.GroupID = G.GroupID and I.Deleted = 'false' and (( FileCount > 0 ) or ( PageCount > 0 )));
		select coalesce(sum( PageCount ), 0 ) into v_total_page_count from SobekCM_Item where Deleted = 'false'  and (( FileCount > 0 ) or ( PageCount > 0 ));

		RETURN QUERY
		select C.Code as code1,
			   C.ChildCode as code2,
			   C.Child2Code as code3,
			   C.AllCodes,
			C.Name,
			C.isActive::text AS Active,
			( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ) as title_count,
			( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ) as item_count,
			coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ), 0) as page_count
		from temp_aggregation_list C
		where ( C.Code <> 'TESTCOL' ) AND ( C.Code <> 'TESTG' )
		union
		select 'ZZZ','','', 'ZZZ', 'Total Count', 'false', v_total_title_count, v_total_item_count, v_total_page_count
		order by code1, code2, code3;
	elsif ( p_option = 2 ) then
		-- COUNT OF ALL ENTERED ITEMS
		select count(*) into v_total_item_count from SobekCM_Item where Deleted = 'false';
		select count(*) into v_total_title_count from SobekCM_Item_Group G where G.Deleted = 'false';
		select coalesce(sum( PageCount ), 0 ) into v_total_page_count from SobekCM_Item;

		RETURN QUERY
		select C.Code as code1,
			   C.ChildCode as code2,
			   C.Child2Code as code3,
			   C.AllCodes,
			C.Name,
			C.isActive::text AS Active,
			( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ) as title_count,
			( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ) as item_count,
			coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ), 0) as page_count
		from temp_aggregation_list C
		where ( C.Code <> 'TESTCOL' ) AND ( C.Code <> 'TESTG' )
		union
		select 'ZZZ','','', 'ZZZ', 'Total Count', 'false', v_total_title_count, v_total_item_count, v_total_page_count
		order by code1, code2, code3;
	else
		-- THIS IS THE OLDER OPTION, WHERE MILESTONE_COMPLETE MUST HAVE A DATE
		select count(*) into v_total_item_count from SobekCM_Item where Deleted = 'false' and Milestone_OnlineComplete is not null;
		select count(*) into v_total_title_count from SobekCM_Item_Group G where G.Deleted = 'false' and exists ( select * from SobekCM_Item I where I.GroupID = G.GroupID and I.Deleted = 'false' and Milestone_OnlineComplete is not null );
		select coalesce(sum( PageCount ), 0 ) into v_total_page_count from SobekCM_Item where Deleted = 'false'  and ( Milestone_OnlineComplete is not null );

		RETURN QUERY
		select C.Code as code1,
			   C.ChildCode as code2,
			   C.Child2Code as code3,
			   C.AllCodes,
			C.Name,
			C.isActive::text AS Active,
			( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ) as title_count,
			( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ) as item_count,
			coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ), 0) as page_count
		from temp_aggregation_list C
		where ( C.Code <> 'TESTCOL' ) AND ( C.Code <> 'TESTG' )
		union
		select 'ZZZ','','', 'ZZZ', 'Total Count', 'false', v_total_title_count, v_total_item_count, v_total_page_count
		order by code1, code2, code3;
	end if;
END;
$$;

-- Originally returned either 12 or 15 columns in its single result set, depending on whether
-- p_date2 was supplied. RETURNS TABLE requires a fixed column list at the function definition
-- level, so this is ported with a single OUT refcursor parameter instead -- each branch OPENs
-- the cursor against a query with the appropriate column list, and the actual returned shape
-- is determined dynamically by that query (matching the original variable-shape behavior).
CREATE OR REPLACE FUNCTION SobekCM_Item_Count_By_Collection_By_Date_Range(
	p_date1 timestamp,
	p_date2 timestamp,
	p_option integer,
	OUT cur_counts refcursor
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_all_id integer;
	v_total_item_count integer;
	v_total_title_count integer;
	v_total_page_count integer;
	v_total_item_count_date1 integer;
	v_total_title_count_date1 integer;
	v_total_page_count_date1 integer;
	v_total_item_count_date2 integer;
	v_total_title_count_date2 integer;
	v_total_page_count_date2 integer;
	v_one_date boolean;
BEGIN
	v_all_id := coalesce(( select AggregationID from SobekCM_Item_Aggregation where Code='all'), -1);

	CREATE TEMP TABLE temp_aggregation_list2
	(
	  AggregationID integer,
	  Code varchar(20),
	  ChildCode varchar(20),
	  Child2Code varchar(20),
	  AllCodes varchar(20),
	  Name varchar(255),
	  ShortName varchar(100),
	  "Type" varchar(50),
	  isActive boolean
	) ON COMMIT DROP;

	insert into temp_aggregation_list2 ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, "Type", isActive )
	select AggregationID, Code, '', '', Code, Name, ShortName, Type, isActive
	from SobekCM_Item_Aggregation A
	where ( Type not like 'Institut%' )
	  and ( Deleted='false' )
	  and exists ( select * from SobekCM_Item_Aggregation_Hierarchy where ChildID=A.AggregationID and ParentID=v_all_id);

	insert into temp_aggregation_list2 ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, "Type", isActive )
	select A2.AggregationID, T.Code, A2.Code, '', A2.Code, A2.Name, A2.ShortName, A2.Type, A2.isActive
	from temp_aggregation_list2 T, SobekCM_Item_Aggregation A2, SobekCM_Item_Aggregation_Hierarchy H
	where ( A2.Type not like 'Institut%' )
	  and ( T.AggregationID = H.ParentID )
	  and ( A2.AggregationID = H.ChildID )
	  and ( Deleted='false' );

	insert into temp_aggregation_list2 ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, "Type", isActive )
	select A2.AggregationID, T.Code, T.ChildCode, A2.Code, A2.Code, A2.Name, A2.ShortName, A2.Type, A2.isActive
	from temp_aggregation_list2 T, SobekCM_Item_Aggregation A2, SobekCM_Item_Aggregation_Hierarchy H
	where ( A2.Type not like 'Institut%' )
	  and ( T.AggregationID = H.ParentID )
	  and ( A2.AggregationID = H.ChildID )
	  and ( Deleted='false' )
	  and ( ChildCode <> '' );

	v_one_date := ( coalesce( p_date2, DATE '2000-01-01' ) = DATE '2000-01-01' );

	if ( p_option = 1 ) then
		-- COUNT OF ALL ITEMS WITH SOME DIGITAL RESOURCES ATTACHED
		select count(*) into v_total_item_count from SobekCM_Item where Deleted = 'false' and (( FileCount > 0 ) or ( PageCount > 0 ));
		select count(G.GroupID) into v_total_title_count from SobekCM_Item_Group G where exists ( select ItemID from SobekCM_Item I where ( I.Deleted = 'false' ) and (( FileCount > 0 ) or ( PageCount > 0 )) and ( I.GroupID = G.GroupID ));
		select coalesce(sum( PageCount ), 0 ) into v_total_page_count from SobekCM_Item where Deleted = 'false'  and (( FileCount > 0 ) or ( PageCount > 0 ));

		select count(ItemID) into v_total_item_count_date1 from SobekCM_Item I where ( I.Deleted = 'false' ) and (( FileCount > 0 ) or ( PageCount > 0 )) and ( CreateDate is not null ) and ( CreateDate <= p_date1 );
		select count(G.GroupID) into v_total_title_count_date1 from SobekCM_Item_Group G where exists ( select * from SobekCM_Item I where ( I.Deleted = 'false' ) and (( FileCount > 0 ) or ( PageCount > 0 )) and ( CreateDate is not null ) and ( CreateDate <= p_date1 ) and ( I.GroupID = G.GroupID ));
		select sum( coalesce(PageCount,0) ) into v_total_page_count_date1 from SobekCM_Item I where ( I.Deleted = 'false' ) and (( FileCount > 0 ) or ( PageCount > 0 )) and ( CreateDate is not null ) and ( CreateDate <= p_date1 );

		if ( v_one_date ) then
			OPEN cur_counts FOR
			select C.Code as code1, C.ChildCode as code2, C.Child2Code as code3, C.AllCodes, C.Name, C.isActive AS Active,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ) as title_count,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ) as item_count,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ), 0) as page_count,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date1 )) as title_count_date1,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date1 )) as item_count_date1,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date1 )), 0) as page_count_date1
			from temp_aggregation_list2 C
			union
			select 'ZZZ','','', 'ZZZ', 'Total Count', 'false', v_total_title_count, v_total_item_count, v_total_page_count,
				coalesce(v_total_title_count_date1,0), coalesce(v_total_item_count_date1,0), coalesce(v_total_page_count_date1,0)
			order by code1, code2, code3;
		else
			select count(ItemID) into v_total_item_count_date2 from SobekCM_Item I where ( I.Deleted = 'false' ) and (( FileCount > 0 ) or ( PageCount > 0 )) and ( CreateDate <= p_date2 );
			select count(G.GroupID) into v_total_title_count_date2 from SobekCM_Item_Group G where exists ( select * from SobekCM_Item I where ( I.Deleted = 'false' ) and (( FileCount > 0 ) or ( PageCount > 0 )) and ( CreateDate <= p_date2 ) and ( I.GroupID = G.GroupID ));
			select sum( coalesce(PageCount,0) ) into v_total_page_count_date2 from SobekCM_Item I where ( I.Deleted = 'false' ) and (( FileCount > 0 ) or ( PageCount > 0 )) and ( CreateDate <= p_date2 );

			OPEN cur_counts FOR
			select C.Code as code1, C.ChildCode as code2, C.Child2Code as code3, C.AllCodes, C.Name, C.isActive AS Active,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ) as title_count,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ) as item_count,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID ), 0) as page_count,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date1 )) as title_count_date1,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date1 )) as item_count_date1,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date1 )), 0) as page_count_date1,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date2 )) as title_count_date2,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date2 )) as item_count_date2,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View2 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date2 )), 0) as page_count_date2
			from temp_aggregation_list2 C
			union
			select 'ZZZ','','','ZZZ', 'Total Count', 'false', v_total_title_count, v_total_item_count, v_total_page_count,
					coalesce(v_total_title_count_date1,0), coalesce(v_total_item_count_date1,0), coalesce(v_total_page_count_date1,0),
					coalesce(v_total_title_count_date2,0), coalesce(v_total_item_count_date2,0), coalesce(v_total_page_count_date2,0)
			order by code1, code2, code3;
		end if;
	elsif ( p_option = 2 ) then
		-- COUNT OF ALL ENTERED ITEMS
		select count(*) into v_total_item_count from SobekCM_Item where Deleted = 'false';
		select count(G.GroupID) into v_total_title_count from SobekCM_Item_Group G where exists ( select ItemID from SobekCM_Item I where ( I.Deleted = 'false' ) and ( I.GroupID = G.GroupID ));
		select coalesce(sum( PageCount ), 0 ) into v_total_page_count from SobekCM_Item where Deleted = 'false';

		select count(ItemID) into v_total_item_count_date1 from SobekCM_Item I where ( I.Deleted = 'false' ) and ( CreateDate is not null ) and ( CreateDate <= p_date1 );
		select count(G.GroupID) into v_total_title_count_date1 from SobekCM_Item_Group G where exists ( select * from SobekCM_Item I where ( I.Deleted = 'false' ) and ( CreateDate is not null ) and ( CreateDate <= p_date1 ) and ( I.GroupID = G.GroupID ));
		select sum( coalesce(PageCount,0) ) into v_total_page_count_date1 from SobekCM_Item I where ( I.Deleted = 'false' ) and ( CreateDate is not null ) and ( CreateDate <= p_date1 );

		if ( v_one_date ) then
			OPEN cur_counts FOR
			select C.Code as code1, C.ChildCode as code2, C.Child2Code as code3, C.AllCodes, C.Name, C.isActive AS Active,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ) as title_count,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ) as item_count,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ), 0) as page_count,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date1 )) as title_count_date1,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date1 )) as item_count_date1,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date1 )), 0) as page_count_date1
			from temp_aggregation_list2 C
			union
			select 'ZZZ','','', 'ZZZ', 'Total Count', 'false', v_total_title_count, v_total_item_count, v_total_page_count,
				coalesce(v_total_title_count_date1,0), coalesce(v_total_item_count_date1,0), coalesce(v_total_page_count_date1,0)
			order by code1, code2, code3;
		else
			select count(ItemID) into v_total_item_count_date2 from SobekCM_Item I where ( I.Deleted = 'false' ) and ( CreateDate <= p_date2 );
			select count(G.GroupID) into v_total_title_count_date2 from SobekCM_Item_Group G where exists ( select * from SobekCM_Item I where ( I.Deleted = 'false' ) and ( CreateDate <= p_date2 ) and ( I.GroupID = G.GroupID ));
			select sum( coalesce(PageCount,0) ) into v_total_page_count_date2 from SobekCM_Item I where ( I.Deleted = 'false' ) and ( CreateDate <= p_date2 );

			OPEN cur_counts FOR
			select C.Code as code1, C.ChildCode as code2, C.Child2Code as code3, C.AllCodes, C.Name, C.isActive AS Active,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ) as title_count,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ) as item_count,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID ), 0) as page_count,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date1 )) as title_count_date1,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date1 )) as item_count_date1,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date1 )), 0) as page_count_date1,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date2 )) as title_count_date2,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date2 )) as item_count_date2,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View3 T where T.AggregationID = C.AggregationID and ( CreateDate is not null ) and ( CreateDate <= p_date2 )), 0) as page_count_date2
			from temp_aggregation_list2 C
			union
			select 'ZZZ','','','ZZZ', 'Total Count', 'false', v_total_title_count, v_total_item_count, v_total_page_count,
					coalesce(v_total_title_count_date1,0), coalesce(v_total_item_count_date1,0), coalesce(v_total_page_count_date1,0),
					coalesce(v_total_title_count_date2,0), coalesce(v_total_item_count_date2,0), coalesce(v_total_page_count_date2,0)
			order by code1, code2, code3;
		end if;
	else
		-- THIS IS THE OLDER OPTION, WHERE MILESTONE_COMPLETE MUST HAVE A DATE
		select count(*) into v_total_item_count from SobekCM_Item where Deleted = 'false' and Milestone_OnlineComplete is not null;
		select count(G.GroupID) into v_total_title_count from SobekCM_Item_Group G where exists ( select ItemID from SobekCM_Item I where ( I.Deleted = 'false' ) and ( Milestone_OnlineComplete is not null ) and ( I.GroupID = G.GroupID ));
		select coalesce(sum( PageCount ), 0 ) into v_total_page_count from SobekCM_Item where Deleted = 'false'  and ( Milestone_OnlineComplete is not null );

		select count(ItemID) into v_total_item_count_date1 from SobekCM_Item I where ( I.Deleted = 'false' ) and ( Milestone_OnlineComplete is not null ) and ( Milestone_OnlineComplete <= p_date1 );
		select count(G.GroupID) into v_total_title_count_date1 from SobekCM_Item_Group G where exists ( select * from SobekCM_Item I where ( I.Deleted = 'false' ) and ( Milestone_OnlineComplete is not null ) and ( Milestone_OnlineComplete <= p_date1 ) and ( I.GroupID = G.GroupID ));
		select sum( coalesce(PageCount,0) ) into v_total_page_count_date1 from SobekCM_Item I where ( I.Deleted = 'false' ) and ( Milestone_OnlineComplete is not null ) and ( Milestone_OnlineComplete <= p_date1 );

		if ( v_one_date ) then
			OPEN cur_counts FOR
			select C.Code as code1, C.ChildCode as code2, C.Child2Code as code3, C.AllCodes, C.Name, C.isActive AS Active,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ) as title_count,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ) as item_count,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ), 0) as page_count,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= p_date1) as title_count_date1,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= p_date1 ) as item_count_date1,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= p_date1 ), 0) as page_count_date1
			from temp_aggregation_list2 C
			union
			select 'ZZZ','','', 'ZZZ', 'Total Count', 'false', v_total_title_count, v_total_item_count, v_total_page_count,
				coalesce(v_total_title_count_date1,0), coalesce(v_total_item_count_date1,0), coalesce(v_total_page_count_date1,0)
			order by code1, code2, code3;
		else
			select count(ItemID) into v_total_item_count_date2 from SobekCM_Item I where ( I.Deleted = 'false' ) and ( Milestone_OnlineComplete is not null ) and ( Milestone_OnlineComplete <= p_date2 );
			select count(G.GroupID) into v_total_title_count_date2 from SobekCM_Item_Group G where exists ( select * from SobekCM_Item I where ( I.Deleted = 'false' ) and ( Milestone_OnlineComplete is not null ) and ( Milestone_OnlineComplete <= p_date2 ) and ( I.GroupID = G.GroupID ));
			select sum( coalesce(PageCount,0) ) into v_total_page_count_date2 from SobekCM_Item I where ( I.Deleted = 'false' ) and ( Milestone_OnlineComplete is not null ) and ( Milestone_OnlineComplete <= p_date2 );

			OPEN cur_counts FOR
			select C.Code as code1, C.ChildCode as code2, C.Child2Code as code3, C.AllCodes, C.Name, C.isActive AS Active,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ) as title_count,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ) as item_count,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ), 0) as page_count,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= p_date1) as title_count_date1,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= p_date1 ) as item_count_date1,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= p_date1 ), 0) as page_count_date1,
				( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= p_date2) as title_count_date2,
				( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= p_date2 ) as item_count_date2,
				coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= p_date2 ), 0) as page_count_date2
			from temp_aggregation_list2 C
			union
			select 'ZZZ','','','ZZZ', 'Total Count', 'false', v_total_title_count, v_total_item_count, v_total_page_count,
					coalesce(v_total_title_count_date1,0), coalesce(v_total_item_count_date1,0), coalesce(v_total_page_count_date1,0),
					coalesce(v_total_title_count_date2,0), coalesce(v_total_item_count_date2,0), coalesce(v_total_page_count_date2,0)
			order by code1, code2, code3;
		end if;
	end if;
END;
$$;

-- Gets a list of items and groups which exist within this instance
CREATE OR REPLACE FUNCTION SobekCM_Item_List(
	p_include_private boolean
)
RETURNS TABLE (
	BibID varchar(10),
	VID varchar(5),
	IP_Restriction_Mask smallint,
	Title varchar(500),
	"Type" varchar(50),
	Dark boolean,
	ItemID integer
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_lower_mask integer;
BEGIN
	v_lower_mask := 0;
	if ( p_include_private ) then
		v_lower_mask := -256;
	end if;

	RETURN QUERY
	select G.BibID, I.VID, I.IP_Restriction_Mask, I.Title, G.Type, I.Dark, I.ItemID
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID )
	  and ( G.Deleted = false )
	  and ( I.Deleted = false )
	  and ( I.IP_Restriction_Mask >= v_lower_mask )
	order by BibID, VID;
END;
$$;


-- Returns the list of all items within the library with some very basic information.
-- This is primarily utilized by the builder to step through all items in the library
-- and build the marc files, or links for the sitemap.
-- Originally returned 1 (private included) or 3 (public only) result sets; ported as
-- RETURNS SETOF refcursor for the same reason as the other variable-result-set procs above.
CREATE OR REPLACE FUNCTION SobekCM_Item_List_Brief2(
	p_include_private boolean
)
RETURNS SETOF refcursor
LANGUAGE plpgsql
AS $$
DECLARE
	cur refcursor;
BEGIN
	if ( p_include_private ) then
		OPEN cur FOR
		select G.BibID, I.VID, G.GroupTitle,
			coalesce(I.Level1_Text, '') as Level1_Text, coalesce( I.Level1_Index, 0 ) as Level1_Index,
			coalesce(I.Level2_Text, '') as Level2_Text, coalesce( I.Level2_Index, 0 ) as Level2_Index,
			coalesce(I.Level3_Text, '') as Level3_Text, coalesce( I.Level3_Index, 0 ) as Level3_Index,
			coalesce(I.PubDate,'') as PubDate, coalesce( I.SortDate,-1) as SortDate, G.File_Location || '/' || VID || '/' || coalesce( I.MainThumbnail,'') as MainThumbnail,
			I.Title, coalesce(I.Author,'') as Author, IP_Restriction_Mask, G.OCLC_Number, G.ALEPH_Number, I.LastSaved, I.AggregationCodes, G.Large_Format
		from SobekCM_Item I, SobekCM_Item_Group G
		where ( I.GroupID = G.GroupID )
		  and ( G.Deleted = false )
		  and ( I.Deleted = false );
		RETURN NEXT cur;

		OPEN cur FOR
		select G.BibID, G.GroupID, G.ItemCount as VID_COUNT, G.GroupTitle, G.Type, G.File_Location, coalesce(G.SortTitle, G.GroupTitle) as SortTitle, G.OCLC_Number, G.ALEPH_Number
		from SobekCM_Item_Group G;
		RETURN NEXT cur;
	else
		OPEN cur FOR
		select G.BibID, I.VID, G.GroupTitle,
			coalesce(I.Level1_Text, '') as Level1_Text, coalesce( I.Level1_Index, 0 ) as Level1_Index,
			coalesce(I.Level2_Text, '') as Level2_Text, coalesce( I.Level2_Index, 0 ) as Level2_Index,
			coalesce(I.Level3_Text, '') as Level3_Text, coalesce( I.Level3_Index, 0 ) as Level3_Index,
			coalesce(I.PubDate,'') as PubDate, coalesce( I.SortDate,-1) as SortDate, G.File_Location || '/' || VID || '/' || coalesce( I.MainThumbnail,'') as MainThumbnail,
			I.Title, coalesce(I.Author,'') as Author, IP_Restriction_Mask, G.OCLC_Number, G.ALEPH_Number, I.LastSaved, I.AggregationCodes, G.Large_Format
		from SobekCM_Item I, SobekCM_Item_Group G
		where ( I.GroupID = G.GroupID )
		  and ( G.Deleted = false )
		  and ( I.Deleted = false )
		  and ( I.IP_Restriction_Mask >= 0 );
		RETURN NEXT cur;

		CREATE TEMP TABLE temp_nonprivate_groups AS
		select distinct(GroupID) as GroupID
		from SobekCM_Item I
		where ( I.Deleted = false )
		  and ( I.IP_Restriction_Mask >= 0 );

		OPEN cur FOR
		select G.BibID, G.GroupID, G.ItemCount as VID_COUNT, G.GroupTitle, G.Type, G.File_Location, coalesce(G.SortTitle, G.GroupTitle) as SortTitle, G.OCLC_Number, G.ALEPH_Number
		from SobekCM_Item_Group G, temp_nonprivate_groups T
		where T.GroupID = G.GroupID and G.Deleted = false;
		RETURN NEXT cur;

		OPEN cur FOR
		select G.BibID, I.VID
		from SobekCM_Item I, SobekCM_Item_Group G
		where ( I.GroupID = G.GroupID )
		  and ( G.Deleted = false )
		  and ( I.Deleted = false )
		  and ( I.IP_Restriction_Mask < 0 );
		RETURN NEXT cur;
	end if;

	RETURN;
END;
$$;


-- Get the list of items by ALEPH number.
-- Originally returned 2 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_Items_By_ALEPH(
	p_aleph_number integer,
	OUT cur_items refcursor,
	OUT cur_titles refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_items FOR
	select BibID, VID, SortDate, Spatial_KML, I.GroupID as fk_TitleID, Title
	from SobekCM_Item I, SobekCM_Item_Group G
	where I.GroupID = G.GroupID
	  and G.ALEPH_Number = p_aleph_number
	order by BibID ASC, VID ASC;

	OPEN cur_titles FOR
	select G.BibID, G.SortTitle, G.GroupID as TitleID, -1 as "Rank"
	from SobekCM_Item_Group G
	where  G.ALEPH_Number = p_aleph_number
	order by BibID ASC;
END;
$$;


-- Get the list of items by OCLC number.
-- Originally returned 2 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_Items_By_OCLC(
	p_oclc_number bigint,
	OUT cur_items refcursor,
	OUT cur_titles refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_items FOR
	select BibID, VID, SortDate, Spatial_KML, I.GroupID as fk_TitleID, Title
	from SobekCM_Item I, SobekCM_Item_Group G
	where I.GroupID = G.GroupID
	  and G.OCLC_Number = p_oclc_number
	order by BibID ASC, VID ASC;

	OPEN cur_titles FOR
	select G.BibID, G.SortTitle, G.GroupID as TitleID, -1 as "Rank"
	from SobekCM_Item_Group G
	where  G.OCLC_Number = p_oclc_number
	order by BibID ASC;
END;
$$;


-- Log an email which was sent through a different method.  This does not
-- cause a database mail to be sent, just logs an email which was sent
CREATE OR REPLACE FUNCTION SobekCM_Log_Email(
	p_sender varchar(250),
	p_recipients_list varchar(500),
	p_subject_line varchar(240),
	p_email_body text,
	p_html_format boolean,
	p_contact_us boolean,
	p_replytoemailid integer
)
RETURNS void
LANGUAGE sql
AS $$
	insert into SobekCM_Email_Log( Sender, Receipt_List, Subject_Line, Email_Body, Sent_Date, HTML_Format, Contact_Us, ReplyToEmailID )
	values ( p_sender, p_recipients_list, p_subject_line || '( log only )', p_email_body, now(), p_html_format, p_contact_us, p_replytoemailid );
$$;


-- Stored procedure for pulling the list of thematic headings
CREATE OR REPLACE FUNCTION SobekCM_Manager_Get_Thematic_Headings()
RETURNS SETOF SobekCM_Thematic_Heading
LANGUAGE sql
AS $$
	select *
	from SobekCM_Thematic_Heading
	order by ThemeOrder;
$$;


-- Pulls the list of items for MARC XML Automation during
-- load of records to production mango
CREATE OR REPLACE FUNCTION SobekCM_MarcXML_Production_Feed()
RETURNS TABLE (
	BibID varchar(10),
	VID varchar(5),
	CreateDate timestamp,
	CollectionCode varchar(20),
	File_Location varchar(255)
)
LANGUAGE plpgsql
AS $$
BEGIN
	CREATE TEMP TABLE temp_one_vid_per_bib ON COMMIT DROP AS
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
	from temp
	where rownum = 1;

	RETURN QUERY
	select I.BibID, I.VID, I.CreateDate, C.Code as CollectionCode, I.File_Location
	from temp_one_vid_per_bib I, SobekCM_Item_Aggregation_Item_Link CL, SobekCM_Item_Aggregation C
	where ( CL.ItemID = I.ItemID )
	  and ( CL.AggregationID = C.AggregationID )
	  and ( CL.impliedLink = 'false' )
	order by BibID;

	drop table temp_one_vid_per_bib;
END;
$$;


-- Pulls the list of items for MARC XML Automation during
-- load of records to test mango
CREATE OR REPLACE FUNCTION SobekCM_MarcXML_Test_Feed()
RETURNS TABLE (
	BibID varchar(10),
	VID varchar(5),
	CreateDate timestamp,
	CollectionCode varchar(20),
	File_Location varchar(255)
)
LANGUAGE plpgsql
AS $$
BEGIN
	CREATE TEMP TABLE temp_one_vid_per_bib2 ON COMMIT DROP AS
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
	from temp
	where rownum = 1;

	RETURN QUERY
	select I.BibID, I.VID, I.CreateDate, C.Code as CollectionCode, I.File_Location
	from temp_one_vid_per_bib2 I, SobekCM_Item_Aggregation_Item_Link CL, SobekCM_Item_Aggregation C
	where ( CL.ItemID = I.ItemID )
	  and ( CL.AggregationID = C.AggregationID )
	  and ( CL.impliedLink = 'false' )
	order by BibID;

	drop table temp_one_vid_per_bib2;
END;
$$;


-- Add a link to the item aggregation (and all parents) to all the items
-- within a particular item group
CREATE OR REPLACE FUNCTION SobekCM_Mass_Update_Item_Aggregation_Link(
	p_groupid integer,
	p_code varchar(20)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_AggregationID integer;
BEGIN
	if ( length( coalesce( p_code,'')) > 0 ) then
		if ( p_code in ( select Code from SobekCM_Item_Aggregation )) then
			select AggregationID into v_AggregationID from SobekCM_Item_Aggregation where Code = p_code;

			update SobekCM_Item_Aggregation_Item_Link
			set impliedLink = 'false'
			where ( AggregationID = v_AggregationID )
			  and exists ( select * from SobekCM_Item I where I.GroupID=p_GroupID and I.ItemID=SobekCM_Item_Aggregation_Item_Link.ItemID );

			insert into SobekCM_Item_Aggregation_Item_Link ( AggregationID, ItemID, impliedLink )
			select v_AggregationID, I.ItemID, 'false'
			from SobekCM_Item I
			where I.GroupID = p_groupid
			  and not exists ( select * from SobekCM_Item_Aggregation_Item_Link L where L.ItemID = I.ItemID and L.AggregationID = v_AggregationID );

			update SobekCM_Item_Aggregation
			set LastItemAdded = ( select Milestone_OnlineComplete from SobekCM_Item where GroupID=p_groupid and Milestone_OnlineComplete is not null order by Milestone_OnlineComplete DESC limit 1 )
			where AggregationID = v_AggregationID
			  and LastItemAdded < ( select Milestone_OnlineComplete from SobekCM_Item where GroupID=p_groupid and Milestone_OnlineComplete is not null order by Milestone_OnlineComplete DESC limit 1 )
			  and exists ( select Milestone_OnlineComplete from SobekCM_Item where GroupID=p_groupid and Milestone_OnlineComplete is not null );

			CREATE TEMP TABLE temp_parents ( Code varchar(20), AggregationID integer, Hierarchy integer ) ON COMMIT DROP;

			insert into temp_parents ( Code, AggregationID, Hierarchy)
			select P.Code, P.AggregationID, 1
			from SobekCM_Item_Aggregation C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( C.Code = p_code )
			  and ( H.Search_Parent_Only = 'false' );

			insert into temp_parents ( Code, AggregationID, Hierarchy)
			select P.Code, P.AggregationID, 2
			from temp_parents C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( H.Search_Parent_Only = 'false' );

			insert into temp_parents ( Code, AggregationID, Hierarchy)
			select P.Code, P.AggregationID, 3
			from temp_parents C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( C.Hierarchy = 2 )
			  and ( H.Search_Parent_Only = 'false' );

			insert into temp_parents ( Code, AggregationID, Hierarchy)
			select P.Code, P.AggregationID, 4
			from temp_parents C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( C.Hierarchy = 3 )
			  and ( H.Search_Parent_Only = 'false' );

			insert into SobekCM_Item_Aggregation_Item_Link ( AggregationID, ItemID, impliedLink )
			select AggregationID, I.ItemID, 'true'
			from temp_parents P, SobekCM_Item I
			where I.GroupID=p_groupid
			  and not exists ( select *
								from SobekCM_Item_Aggregation_Item_Link L
								where ( P.AggregationID = L.AggregationID )
								  and ( L.ItemID = I.ItemID ));

			update SobekCM_Item_Aggregation
			set LastItemAdded = ( select Milestone_OnlineComplete from SobekCM_Item where GroupID=p_groupid and Milestone_OnlineComplete is not null order by Milestone_OnlineComplete DESC limit 1 )
			where exists ( select * from temp_parents T where T.AggregationID=SobekCM_Item_Aggregation.AggregationID )
			  and LastItemAdded < ( select Milestone_OnlineComplete from SobekCM_Item where GroupID=p_groupid and Milestone_OnlineComplete is not null order by Milestone_OnlineComplete DESC limit 1 )
			  and exists ( select Milestone_OnlineComplete from SobekCM_Item where GroupID=p_groupid and Milestone_OnlineComplete is not null );

			drop table temp_parents;
		end if;
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
		if (( select count(*) from SobekCM_Item_Aggregation where Code = p_HoldingCode ) = 0 ) then
			insert into SobekCM_Item_Aggregation ( Code, Name, ShortName, Description, ThematicHeadingID, Type, isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
			values ( p_HoldingCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
		end if;

		PERFORM SobekCM_Mass_Update_Item_Aggregation_Link(p_GroupID, p_HoldingCode);
	end if;

	-- Check for Source Institution Code
	if ( length ( coalesce ( p_SourceCode, '' ) ) > 0 ) then
		if (( select count(*) from SobekCM_Item_Aggregation where Code = p_SourceCode ) = 0 ) then
			insert into SobekCM_Item_Aggregation ( Code, Name, ShortName, Description, ThematicHeadingID, Type, isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
			values ( p_SourceCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
		end if;

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

-- Return the item and page count added each month
CREATE OR REPLACE FUNCTION SobekCM_Page_Item_Count_History()
RETURNS TABLE (
	"Year" integer,
	"Month" integer,
	Total bigint,
	ItemCount bigint
)
LANGUAGE plpgsql
AS $$
BEGIN
	CREATE TEMP TABLE temp_page_item_history ON COMMIT DROP AS
	select ItemID, EXTRACT(YEAR FROM Milestone_OnlineComplete)::integer as "Year", EXTRACT(MONTH FROM Milestone_OnlineComplete)::integer as "Month", PageCount
	from SobekCM_Item I
	where I.Deleted = 'false';

	RETURN QUERY
	select "Year", "Month", SUM( PageCount ) as Total, COUNT(*) as ItemCount
	from temp_page_item_history
	group by "Year", "Month"
	order by "Year", "Month";
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_QC_Delete_Error(
	p_itemID integer,
	p_filename text
)
RETURNS void
LANGUAGE sql
AS $$
	DELETE FROM SobekCM_QC_Errors WHERE ItemID=p_itemID AND FileName=p_filename;
$$;


CREATE OR REPLACE FUNCTION SobekCM_QC_Get_Errors(
	p_itemID integer
)
RETURNS SETOF SobekCM_QC_Errors
LANGUAGE sql
AS $$
	SELECT * FROM SobekCM_QC_Errors WHERE ItemID=p_itemID;
$$;


CREATE OR REPLACE FUNCTION SobekCM_QC_Save_Error(
	p_itemID integer,
	p_filename text,
	p_errorCode char(10),
	p_isVolumeError boolean,
	p_description text,
	OUT p_errorID integer
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_errorCount integer;
BEGIN
	if not exists(select * from SobekCM_QC_Errors where ItemID=p_itemID and FileName=p_filename) then
		INSERT INTO SobekCM_QC_Errors(ItemID, FileName,ErrorCode,isVolumeError,Description)
		VALUES(p_itemID,p_filename,p_errorCode, p_isVolumeError, p_description)
		RETURNING ErrorID INTO p_errorID;
	else
		Update SobekCM_QC_Errors set ErrorCode=p_errorCode, isVolumeError=p_isVolumeError,Description=p_description
		where ItemID=p_itemID AND FileName=p_filename
		RETURNING ErrorID INTO p_errorID;
	end if;

	--Also add this error into the the errors History table
	if not exists(select * from SobekCM_QC_Errors_History where ItemID=p_itemID and ErrorCode=p_errorCode) then
        INSERT INTO SobekCM_QC_Errors_History(ItemID,ErrorCode,isVolumeError,"Count")
        VALUES(p_itemID,p_errorCode,p_isVolumeError,1);
    else
      select "Count" into v_errorCount from SobekCM_QC_Errors_History
      where ItemID=p_itemID and ErrorCode=p_errorCode;

      update SobekCM_QC_Errors_History set "Count"=(v_errorCount+1)
      where ItemID=p_itemID and ErrorCode=p_errorCode;
    end if;
END;
$$;


-- Choose a random item from the entire digital library that is public
CREATE OR REPLACE FUNCTION SobekCM_Random_Item()
RETURNS TABLE (
	BibID varchar(10),
	VID varchar(5)
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_minid integer;
	v_maxid integer;
	v_randomid integer;
	v_attempt integer;
BEGIN
	select MIN(GroupID) into v_minid from SobekCM_Item_Group where Deleted = 'false';
	select MAX(GroupID) into v_maxid from SobekCM_Item_Group where Deleted = 'false';

	v_randomid := -1;
	v_attempt := 0;

	-- Loop here for about 20 times (since this is so relatively cheap)
	while (( v_attempt <= 20 ) and ( v_randomid < 0 )) loop
		v_randomid := v_minid + ( random() * (v_maxid - v_minid ))::integer;

		if ( not exists ( select * from SobekCM_Item_Group G where Deleted='false' and GroupID = v_randomid and exists ( select 1 from SobekCM_Item I where I.GroupID=v_randomid and I.Deleted='false' and I.IP_Restriction_Mask = 0 and I.Dark = 'false' and I.PageCount > 0))) then
			v_randomid := -1;
		end if;

		v_attempt := v_attempt + 1;
	end loop;

	-- Sometimes, the process above does not generate any BibID, so use the brute force method
	if ( v_randomid < 0 ) then
		with sample_rows_ordered AS (
			select GroupID, random() as randomid
			from SobekCM_Item_Group G
			where exists ( select 1 from SobekCM_Item I where I.GroupID=G.GroupID and I.Deleted='false' and I.IP_Restriction_Mask = 0 and I.Dark = 'false' and I.PageCount > 0)
		)
		select GroupID into v_randomid from sample_rows_ordered order by randomid limit 1;
	end if;

	-- With the bibid in hand, now select a random vid
	RETURN QUERY
	select I.BibID, I.VID
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.Deleted = 'false' )
	  and ( I.IP_Restriction_Mask = 0 )
	  and ( I.Dark = 'false' )
	  and ( G.GroupID = v_randomid )
	  and ( G.GroupID = I.GroupID )
	  and ( I.PageCount > 0 )
	order by random()
	limit 1;
END;
$$;


-- Remove an existing viewer for an item
CREATE OR REPLACE FUNCTION SobekCM_Remove_Item_Viewers(
	p_ItemID integer,
	p_Viewer1_Type varchar(50),
	p_Viewer2_Type varchar(50),
	p_Viewer3_Type varchar(50),
	p_Viewer4_Type varchar(50),
	p_Viewer5_Type varchar(50),
	p_Viewer6_Type varchar(50)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_TypeID integer;
BEGIN
	if ( length(coalesce(p_Viewer1_Type, '')) > 0 ) then
		v_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer1_Type ), -1 );
		if ( v_TypeID > 0 ) then
			update SobekCM_Item_Viewers set Exclude='true' where ItemID=p_ItemID and ItemViewTypeID=v_TypeID;
		end if;
	end if;

	if ( length(coalesce(p_Viewer2_Type, '')) > 0 ) then
		v_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer2_Type ), -1 );
		if ( v_TypeID > 0 ) then
			update SobekCM_Item_Viewers set Exclude='true' where ItemID=p_ItemID and ItemViewTypeID=v_TypeID;
		end if;
	end if;

	if ( length(coalesce(p_Viewer3_Type, '')) > 0 ) then
		v_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer3_Type ), -1 );
		if ( v_TypeID > 0 ) then
			update SobekCM_Item_Viewers set Exclude='true' where ItemID=p_ItemID and ItemViewTypeID=v_TypeID;
		end if;
	end if;

	if ( length(coalesce(p_Viewer4_Type, '')) > 0 ) then
		v_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer4_Type ), -1 );
		if ( v_TypeID > 0 ) then
			update SobekCM_Item_Viewers set Exclude='true' where ItemID=p_ItemID and ItemViewTypeID=v_TypeID;
		end if;
	end if;

	if ( length(coalesce(p_Viewer5_Type, '')) > 0 ) then
		v_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer5_Type ), -1 );
		if ( v_TypeID > 0 ) then
			update SobekCM_Item_Viewers set Exclude='true' where ItemID=p_ItemID and ItemViewTypeID=v_TypeID;
		end if;
	end if;

	if ( length(coalesce(p_Viewer6_Type, '')) > 0 ) then
		v_TypeID := coalesce(( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType = p_Viewer6_Type ), -1 );
		if ( v_TypeID > 0 ) then
			update SobekCM_Item_Viewers set Exclude='true' where ItemID=p_ItemID and ItemViewTypeID=v_TypeID;
		end if;
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_RightsMD_Save_Access_Embargo_UMI(
	p_ItemID integer,
	p_Original_AccessCode varchar(25),
	p_EmbargoEnd date,
	p_UMI varchar(20)
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if ( exists ( select * from Tracking_Item where ItemID=p_ItemID )) then
		update Tracking_Item
		set EmbargoEnd = p_EmbargoEnd, UMI=p_UMI
		where ItemID=p_ItemID;
	else
		insert into Tracking_Item ( ItemID, Original_AccessCode, Original_EmbargoEnd, EmbargoEnd, UMI )
		values ( p_ItemID, p_Original_AccessCode, p_EmbargoEnd, p_EmbargoEnd, p_UMI );
	end if;
END;
$$;

CREATE OR REPLACE FUNCTION SobekCM_Save_Icon(
	p_iconid integer,
	p_icon_name varchar(255),
	p_icon_url varchar(255),
	p_link varchar(255),
	p_height integer,
	p_title varchar(255),
	OUT p_new_iconid integer
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ((select count(*) from SobekCM_Icon where icon_name = p_icon_name) = 0 ) then
		insert into SobekCM_Icon(icon_name,icon_url, link, height, title )
		values(p_icon_name, p_icon_url, p_link, p_height, p_title )
		returning IconID into p_new_iconid;
	else
		update SobekCM_Icon
		set icon_url = p_icon_url, link = p_link, height = p_height, title = p_title
		where icon_name = p_icon_name;

		select IconID into p_new_iconid
		from SobekCM_Icon
		where icon_name = p_icon_name;
   end if;
END;
$$;


-- Add a link to the item aggregation (and all parents)
CREATE OR REPLACE FUNCTION SobekCM_Save_Item_Item_Aggregation_Link(
	p_itemid integer,
	p_code varchar(20)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_AggregationID integer;
BEGIN
	if ( length( coalesce( p_code,'')) > 0 ) then
		if (( select count(*) from SobekCM_Item_Aggregation where Code=p_code and Deleted='false' ) = 1 ) then
			select AggregationID into v_AggregationID from SobekCM_Item_Aggregation where Code = p_code;

			if (( select count(*) from SobekCM_Item_Aggregation_Item_Link where AggregationID = v_AggregationID and ItemID = p_ItemID ) = 0 ) then
				insert into SobekCM_Item_Aggregation_Item_Link ( AggregationID, ItemID, impliedLink )
				values (  v_AggregationID, p_ItemID, 'false' );
			else
				update SobekCM_Item_Aggregation_Item_Link
				set impliedLink = 'false'
				where ( AggregationID = v_AggregationID ) and ( ItemID = p_ItemID );
			end if;

			update SobekCM_Item_Aggregation
			set LastItemAdded = ( select CreateDate from SobekCM_Item where ItemID=p_itemid )
			where AggregationID = v_AggregationID
			  and LastItemAdded < ( select CreateDate from SobekCM_Item where ItemID=p_itemid );

			CREATE TEMP TABLE temp_parents2 ( Code varchar(20), AggregationID integer, Hierarchy integer ) ON COMMIT DROP;

			insert into temp_parents2 ( Code, AggregationID, Hierarchy)
			select P.Code, P.AggregationID, 1
			from SobekCM_Item_Aggregation C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( C.Code = p_code )
			  and ( H.Search_Parent_Only = 'false' );

			insert into temp_parents2 ( Code, AggregationID, Hierarchy)
			select P.Code, P.AggregationID, 2
			from temp_parents2 C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( H.Search_Parent_Only = 'false' );

			insert into temp_parents2 ( Code, AggregationID, Hierarchy)
			select P.Code, P.AggregationID, 3
			from temp_parents2 C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( C.Hierarchy = 2 )
			  and ( H.Search_Parent_Only = 'false' );

			insert into temp_parents2 ( Code, AggregationID, Hierarchy)
			select P.Code, P.AggregationID, 4
			from temp_parents2 C, SobekCM_Item_Aggregation P, SobekCM_Item_Aggregation_Hierarchy H
			where ( C.AggregationID = H.ChildID )
			  and ( P.AggregationID = H.ParentID )
			  and ( C.Hierarchy = 3 )
			  and ( H.Search_Parent_Only = 'false' );

			insert into SobekCM_Item_Aggregation_Item_Link ( AggregationID, ItemID, impliedLink )
			select AggregationID, p_itemid, 'true'
			from temp_parents2 P
			where not exists ( select *
								from SobekCM_Item_Aggregation_Item_Link L
								where ( P.AggregationID = L.AggregationID )
								  and ( L.ItemID = p_itemID ));

			update SobekCM_Item_Aggregation
			set LastItemAdded = ( select CreateDate from SobekCM_Item where ItemID=p_itemid )
			where exists ( select * from temp_parents2 T where T.AggregationID=SobekCM_Item_Aggregation.AggregationID )
			  and LastItemAdded < ( select CreateDate from SobekCM_Item where ItemID=p_itemid );

			drop table temp_parents2;
		end if;
	end if;
END;
$$;


-- Saves all the main data about an item in UFDC (but not behaviors)
-- Written by Mark Sullivan ( September 2005, Edited November 2021)
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
		if (( select count(*) from SobekCM_Item_Aggregation where Code = p_HoldingCode ) = 0 ) then
			insert into SobekCM_Item_Aggregation ( Code, Name, ShortName, Description, ThematicHeadingID, Type, isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
			values ( p_HoldingCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
		end if;

		PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_HoldingCode);
	end if;

	-- Check for Source Institution Code
	if ( length ( coalesce ( p_SourceCode, '' ) ) > 0 ) then
		if (( select count(*) from SobekCM_Item_Aggregation where Code = p_SourceCode ) = 0 ) then
			insert into SobekCM_Item_Aggregation ( Code, Name, ShortName, Description, ThematicHeadingID, Type, isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
			values ( p_SourceCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
		end if;

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

-- Stored procedure to save the basic item aggregation information
CREATE OR REPLACE FUNCTION SobekCM_Save_Item_Aggregation(
	p_aggregationid integer,
	p_code varchar(20),
	p_name varchar(255),
	p_shortname varchar(100),
	p_description varchar(1000),
	p_thematicHeadingId integer,
	p_type varchar(50),
	p_isactive boolean,
	p_hidden boolean,
	p_display_options varchar(10),
	p_map_search smallint,
	p_map_display smallint,
	p_oai_flag boolean,
	p_oai_metadata varchar(2000),
	p_contactemail varchar(255),
	p_defaultinterface varchar(10),
	p_externallink varchar(255),
	p_parentid integer,
	p_username varchar(100),
	p_languageVariants varchar(500),
	p_groupResults boolean,
	OUT p_newaggregationid integer
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_newly_added boolean;
	v_deletedid integer;
BEGIN
	v_newly_added := false;

	if ((p_aggregationid  < 1 ) and (( select COUNT(*) from SobekCM_Item_Aggregation where Code=p_code ) = 0 )) then
		insert into SobekCM_Item_Aggregation(Code, Name, Shortname, Description, ThematicHeadingID, Type, isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, OAI_Metadata, ContactEmail, HasNewItems, DefaultInterface, External_Link, DateAdded, LanguageVariants, GroupResults )
		values(p_code, p_name, p_shortname, p_description, p_thematicHeadingId, p_type, p_isActive, p_hidden, p_display_options, p_map_search, p_map_display, p_oai_flag, p_oai_metadata, p_contactemail, 'false', p_defaultinterface, p_externallink, now(), p_languageVariants, p_groupResults )
		returning AggregationID into p_newaggregationid;

		insert into SobekCM_Item_Aggregation_Milestones ( AggregationID, Milestone, MilestoneDate, MilestoneUser )
		values ( p_newaggregationid, 'Created', now(), p_username );

		v_newly_added := true;
	else
		if ( exists ( select 1 from SobekCM_Item_Aggregation where Code=p_Code and Deleted='true')) then
			select aggregationid into v_deletedid from SobekCM_Item_Aggregation where Code=p_Code;

			insert into SobekCM_Item_Aggregation_Milestones ( AggregationID, Milestone, MilestoneDate, MilestoneUser )
			values ( v_deletedid, 'Created (undeleted as previously existed)', now(), p_username );

			-- Since this was undeleted, let's make sure this collection isn't linked
			-- to any parent collections
			delete from SobekCM_Item_Aggregation_Hierarchy
			where ChildID=v_deletedid;

			v_newly_added := true;
		end if;

		update SobekCM_Item_Aggregation
		set
			Code = p_code,
			Name = p_name,
			ShortName = p_shortname,
			Description = p_description,
			ThematicHeadingID = p_thematicHeadingID,
			Type = p_type,
			isActive = p_isactive,
			Hidden = p_hidden,
			DisplayOptions = p_display_options,
			Map_Search = p_map_search,
			Map_Display = p_map_display,
			OAI_Flag = p_oai_flag,
			OAI_Metadata = p_oai_metadata,
			ContactEmail = p_contactemail,
			DefaultInterface = p_defaultinterface,
			External_Link = p_externallink,
			Deleted = 'false',
			DeleteDate = null,
			LanguageVariants = p_languageVariants,
			GroupResults = p_groupResults
		where AggregationID = p_aggregationid or Code = p_code;

		select aggregationid into p_newaggregationid from SobekCM_Item_Aggregation where Code=p_Code;
	end if;

	-- Was a parent id provided
	if ( coalesce(p_parentid, -1 ) > 0 ) then
		if (( select count(*) from SobekCM_Item_Aggregation_Hierarchy H where H.ParentID = p_parentid and H.ChildID = p_newaggregationid ) < 1 ) then
			insert into SobekCM_Item_Aggregation_Hierarchy ( ParentID, ChildID )
			values ( p_parentid, p_newaggregationid );
		end if;
	end if;

	-- If this was newly added (new or undeleted), ensure permissions and other things copied over from parent
	if ( v_newly_added ) then
		-- There should ALWAYS be a parent for new collections, even if it is the ALL collection
		if ( coalesce(p_parentid, -1 ) < 0 ) then
			select AggregationID into p_parentid from SobekCM_Item_Aggregation where Code='ALL';
		end if;

		update SobekCM_Item_Aggregation
		set GroupResults = ( select GroupResults from SobekCM_Item_Aggregation where AggregationID=p_parentid )
		where AggregationID=p_newaggregationid;

		insert into mySobek_User_Edit_Aggregation ( UserID, AggregationID, CanSelect, CanEditItems,
			IsCurator, IsAdmin, CanEditMetadata, CanEditBehaviors, CanPerformQc,
			CanUploadFiles, CanChangeVisibility, CanDelete )
		select UserID, p_newaggregationid, CanSelect, CanEditItems,
			IsCurator, IsAdmin, CanEditMetadata, CanEditBehaviors, CanPerformQc,
			CanUploadFiles, CanChangeVisibility, CanDelete
		from mySobek_User_Edit_Aggregation A
		where ( AggregationID = p_parentid )
		  and ( not exists ( select * from mySobek_User_Edit_Aggregation L where L.UserID=A.UserID and L.AggregationID=p_newaggregationid ))
		  and (    ( CanEditMetadata='true' )
                or ( CanEditBehaviors='true' )
                or ( CanPerformQc='true' )
                or ( CanUploadFiles='true' )
                or ( CanChangeVisibility='true' )
                or ( IsCurator='true' )
                or ( IsAdmin='true' ));

		insert into mySobek_User_Group_Edit_Aggregation ( UserGroupID, AggregationID, CanSelect, CanEditItems,
			IsCurator, IsAdmin, CanEditMetadata, CanEditBehaviors, CanPerformQc,
			CanUploadFiles, CanChangeVisibility, CanDelete )
		select UserGroupID, p_newaggregationid, CanSelect, CanEditItems,
			IsCurator, IsAdmin, CanEditMetadata, CanEditBehaviors, CanPerformQc,
			CanUploadFiles, CanChangeVisibility, CanDelete
		from mySobek_User_Group_Edit_Aggregation A
		where ( AggregationID = p_parentid )
		  and ( not exists ( select * from mySobek_User_Group_Edit_Aggregation L where L.UserGroupID=A.UserGroupID and L.AggregationID=p_newaggregationid ))
		  and (    ( CanEditMetadata='true' )
                or ( CanEditBehaviors='true' )
                or ( CanPerformQc='true' )
                or ( CanUploadFiles='true' )
                or ( CanChangeVisibility='true' )
                or ( IsCurator='true' )
                or ( IsAdmin='true' ));

		insert into SobekCM_Item_Aggregation_Facets ( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
		select p_newaggregationid, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions
		from SobekCM_Item_Aggregation_Facets
		where AggregationID=p_parentid;

		insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
		select p_newaggregationid, ItemAggregationResultTypeID, DefaultView
		from SobekCM_Item_Aggregation_Result_Views
		where AggregationID=p_parentid;

		insert into SobekCM_Item_Aggregation_Result_Fields ( ItemAggregationResultID, MetadataTypeID, OverrideDisplayTerm, DisplayOrder, DisplayOptions )
		select V2.ItemAggregationResultID, F1.MetadataTypeID, F1.OverrideDisplayTerm, F1.DisplayOrder, F1.DisplayOptions
		from SobekCM_Item_Aggregation_Result_Views V1, SobekCM_Item_Aggregation_Result_Fields F1, SobekCM_Item_Aggregation_Result_Views V2
		where V1.ItemAggregationResultID=F1.ItemAggregationResultID
		  and V1.AggregationID=p_parentid
		  and V2.ItemAggregationResultTypeID=V1.ItemAggregationResultTypeID
		  and V2.AggregationID=p_newaggregationid;
	end if;
END;
$$;


-- Procedure either adds a forwarding or edits an existing forward
CREATE OR REPLACE FUNCTION SobekCM_Save_Item_Aggregation_Alias(
	p_alias varchar(50),
	p_aggregation_code varchar(20)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_aggregationid integer;
BEGIN
	if (( select count(*) from SobekCM_Item_Aggregation where Code=p_aggregation_code and Deleted='false' ) = 1 ) then
		select AggregationID into v_aggregationid from SobekCM_Item_Aggregation where Code=p_aggregation_code;

		if (( select count(*) from SobekCM_Item_Aggregation_Alias where AggregationAlias=p_alias ) > 0 ) then
			update SobekCM_Item_Aggregation_Alias
			set AggregationID = v_aggregationID
			where AggregationAlias = p_alias;
		else
			insert into SobekCM_Item_Aggregation_Alias ( AggregationAlias, AggregationID )
			values ( p_alias, v_aggregationid );
		end if;
	end if;
END;
$$;

-- Preserves two apparent bugs present in the original T-SQL exactly as written, rather than
-- silently fixing them during the port: the facet-6 "still exists" UPDATE matches on
-- v_facet1_id instead of v_facet6_id, and the facet-7 FacetOrder update sets FacetOrder=1
-- instead of 7.
CREATE OR REPLACE FUNCTION SobekCM_Save_Item_Aggregation_Facets(
	p_code varchar(20),
	p_facet1_type varchar(100), p_facet1_display varchar(100),
	p_facet2_type varchar(100), p_facet2_display varchar(100),
	p_facet3_type varchar(100), p_facet3_display varchar(100),
	p_facet4_type varchar(100), p_facet4_display varchar(100),
	p_facet5_type varchar(100), p_facet5_display varchar(100),
	p_facet6_type varchar(100), p_facet6_display varchar(100),
	p_facet7_type varchar(100), p_facet7_display varchar(100),
	p_facet8_type varchar(100), p_facet8_display varchar(100)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_id integer;
	v_facet1_id integer; v_facet1_standard_display varchar(100);
	v_facet2_id integer; v_facet2_standard_display varchar(100);
	v_facet3_id integer; v_facet3_standard_display varchar(100);
	v_facet4_id integer; v_facet4_standard_display varchar(100);
	v_facet5_id integer; v_facet5_standard_display varchar(100);
	v_facet6_id integer; v_facet6_standard_display varchar(100);
	v_facet7_id integer; v_facet7_standard_display varchar(100);
	v_facet8_id integer; v_facet8_standard_display varchar(100);
BEGIN
	if ( exists ( select 1 from SobekCM_Item_Aggregation where Code = p_code )) then
		select AggregationID into v_id from SobekCM_Item_Aggregation where Code = p_code;

		CREATE TEMP TABLE temp_existing_facets ( MetadataTypeID integer primary key, ExTerm varchar(100), ExOrder integer, ExOptions varchar(2000), StillExists boolean ) ON COMMIT DROP;
		insert into temp_existing_facets
		select MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions, false
		from SobekCM_Item_Aggregation_Facets V
		where ( V.AggregationID=v_id );

		-- Add the FIRST facet
		if (( length(p_facet1_type) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=p_facet1_type or SobekCode=p_facet1_type))) then
			select MetadataTypeID into v_facet1_id from SobekCM_Metadata_Types where MetadataName=p_facet1_type or SobekCode=p_facet1_type;
			select FacetTerm into v_facet1_standard_display from SobekCM_Metadata_Types where MetadataTypeID=v_facet1_id;
			if ( v_facet1_standard_display = p_facet1_display ) then p_facet1_display := null; end if;

			if ( not exists ( select 1 from temp_existing_facets where MetadataTypeID=v_facet1_id )) then
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( v_id, v_facet1_id, p_facet1_display, 1, '' );
			else
				update temp_existing_facets set StillExists=true where MetadataTypeID=v_facet1_id;
				update SobekCM_Item_Aggregation_Facets
				set FacetOrder=1, OverrideFacetTerm=p_facet1_display
				where ( MetadataTypeID = v_facet1_id )
				  and ( AggregationID = v_id );
			end if;
		end if;

		-- Add the SECOND facet
		if (( length(p_facet2_type) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=p_facet2_type or SobekCode=p_facet2_type))) then
			select MetadataTypeID into v_facet2_id from SobekCM_Metadata_Types where MetadataName=p_facet2_type or SobekCode=p_facet2_type;
			select FacetTerm into v_facet2_standard_display from SobekCM_Metadata_Types where MetadataTypeID=v_facet2_id;
			if ( v_facet2_standard_display = p_facet2_display ) then p_facet2_display := null; end if;

			if ( not exists ( select 1 from temp_existing_facets where MetadataTypeID=v_facet2_id )) then
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( v_id, v_facet2_id, p_facet2_display, 2, '' );
			else
				update temp_existing_facets set StillExists=true where MetadataTypeID=v_facet2_id;
				update SobekCM_Item_Aggregation_Facets
				set FacetOrder=2, OverrideFacetTerm=p_facet2_display
				where ( MetadataTypeID = v_facet2_id )
				  and ( AggregationID = v_id );
			end if;
		end if;

		-- Add the THIRD facet
		if (( length(p_facet3_type ) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=p_facet3_type or SobekCode=p_facet3_type))) then
			select MetadataTypeID into v_facet3_id from SobekCM_Metadata_Types where MetadataName=p_facet3_type or SobekCode=p_facet3_type;
			select FacetTerm into v_facet3_standard_display from SobekCM_Metadata_Types where MetadataTypeID=v_facet3_id;
			if ( v_facet3_standard_display = p_facet3_display ) then p_facet3_display := null; end if;

			if ( not exists ( select 1 from temp_existing_facets where MetadataTypeID=v_facet3_id )) then
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( v_id, v_facet3_id, p_facet3_display, 3, '' );
			else
				update temp_existing_facets set StillExists=true where MetadataTypeID=v_facet3_id;
				update SobekCM_Item_Aggregation_Facets
				set FacetOrder=3, OverrideFacetTerm=p_facet3_display
				where ( MetadataTypeID = v_facet3_id )
				  and ( AggregationID = v_id );
			end if;
		end if;

		-- Add the FOURTH facet
		if (( length(p_facet1_type) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=p_facet4_type or SobekCode=p_facet4_type))) then
			select MetadataTypeID into v_facet4_id from SobekCM_Metadata_Types where MetadataName=p_facet4_type or SobekCode=p_facet4_type;
			select FacetTerm into v_facet4_standard_display from SobekCM_Metadata_Types where MetadataTypeID=v_facet4_id;
			if ( v_facet4_standard_display = p_facet4_display ) then p_facet4_display := null; end if;

			if ( not exists ( select 1 from temp_existing_facets where MetadataTypeID=v_facet4_id )) then
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( v_id, v_facet4_id, p_facet4_display, 4, '' );
			else
				update temp_existing_facets set StillExists=true where MetadataTypeID=v_facet4_id;
				update SobekCM_Item_Aggregation_Facets
				set FacetOrder=4, OverrideFacetTerm=p_facet4_display
				where ( MetadataTypeID = v_facet4_id )
				  and ( AggregationID = v_id );
			end if;
		end if;

		-- Add the FIFTH facet
		if (( length(p_facet5_type) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=p_facet5_type or SobekCode=p_facet5_type))) then
			select MetadataTypeID into v_facet5_id from SobekCM_Metadata_Types where MetadataName=p_facet5_type or SobekCode=p_facet5_type;
			select FacetTerm into v_facet5_standard_display from SobekCM_Metadata_Types where MetadataTypeID=v_facet5_id;
			if ( v_facet5_standard_display = p_facet5_display ) then p_facet5_display := null; end if;

			if ( not exists ( select 1 from temp_existing_facets where MetadataTypeID=v_facet5_id )) then
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( v_id, v_facet5_id, p_facet5_display, 5, '' );
			else
				update temp_existing_facets set StillExists=true where MetadataTypeID=v_facet5_id;
				update SobekCM_Item_Aggregation_Facets
				set FacetOrder=5, OverrideFacetTerm=p_facet5_display
				where ( MetadataTypeID = v_facet5_id )
				  and ( AggregationID = v_id );
			end if;
		end if;

		-- Add the SIXTH facet
		if (( length(p_facet6_type) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=p_facet6_type or SobekCode=p_facet6_type))) then
			select MetadataTypeID into v_facet6_id from SobekCM_Metadata_Types where MetadataName=p_facet6_type or SobekCode=p_facet6_type;
			select FacetTerm into v_facet6_standard_display from SobekCM_Metadata_Types where MetadataTypeID=v_facet6_id;
			if ( v_facet6_standard_display = p_facet6_display ) then p_facet6_display := null; end if;

			if ( not exists ( select 1 from temp_existing_facets where MetadataTypeID=v_facet6_id )) then
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( v_id, v_facet6_id, p_facet6_display, 6, '' );
			else
				update temp_existing_facets set StillExists=true where MetadataTypeID=v_facet1_id;
				update SobekCM_Item_Aggregation_Facets
				set FacetOrder=6, OverrideFacetTerm=p_facet6_display
				where ( MetadataTypeID = v_facet6_id )
				  and ( AggregationID = v_id );
			end if;
		end if;

		-- Add the SEVENTH facet
		if (( length(p_facet7_type) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=p_facet7_type or SobekCode=p_facet7_type))) then
			select MetadataTypeID into v_facet7_id from SobekCM_Metadata_Types where MetadataName=p_facet7_type or SobekCode=p_facet7_type;
			select FacetTerm into v_facet7_standard_display from SobekCM_Metadata_Types where MetadataTypeID=v_facet7_id;
			if ( v_facet7_standard_display = p_facet7_display ) then p_facet7_display := null; end if;

			if ( not exists ( select 1 from temp_existing_facets where MetadataTypeID=v_facet7_id )) then
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( v_id, v_facet7_id, p_facet7_display, 7, '' );
			else
				update temp_existing_facets set StillExists=true where MetadataTypeID=v_facet7_id;
				update SobekCM_Item_Aggregation_Facets
				set FacetOrder=1, OverrideFacetTerm=p_facet7_display
				where ( MetadataTypeID = v_facet7_id )
				  and ( AggregationID = v_id );
			end if;
		end if;

		-- Add the EIGHTH facet
		if (( length(p_facet8_type) > 0 ) and ( exists ( select 1 from SobekCM_Metadata_Types where MetadataName=p_facet8_type or SobekCode=p_facet8_type))) then
			select MetadataTypeID into v_facet8_id from SobekCM_Metadata_Types where MetadataName=p_facet8_type or SobekCode=p_facet8_type;
			select FacetTerm into v_facet8_standard_display from SobekCM_Metadata_Types where MetadataTypeID=v_facet8_id;
			if ( v_facet8_standard_display = p_facet8_display ) then p_facet8_display := null; end if;

			if ( not exists ( select 1 from temp_existing_facets where MetadataTypeID=v_facet8_id )) then
				insert into SobekCM_Item_Aggregation_Facets( AggregationID, MetadataTypeID, OverrideFacetTerm, FacetOrder, FacetOptions )
				values ( v_id, v_facet8_id, p_facet8_display, 8, '' );
			else
				update temp_existing_facets set StillExists=true where MetadataTypeID=v_facet8_id;
				update SobekCM_Item_Aggregation_Facets
				set FacetOrder=8, OverrideFacetTerm=p_facet8_display
				where ( MetadataTypeID = v_facet8_id )
				  and ( AggregationID = v_id );
			end if;
		end if;

		if (( select count(*) from temp_existing_facets ) > 0 ) then
			delete from SobekCM_Item_Aggregation_Facets
			where MetadataTypeID in ( select MetadataTypeID from temp_existing_facets V where V.StillExists=false)
			  and AggregationID = v_id;
		end if;

		drop table temp_existing_facets;
	end if;
END;
$$;

-- Stored procedure to save the basic item aggregation information
CREATE OR REPLACE FUNCTION SobekCM_Save_Item_Aggregation_ResultViews(
	p_code varchar(20),
	p_results1 varchar(50), p_results2 varchar(50), p_results3 varchar(50), p_results4 varchar(50), p_results5 varchar(50),
	p_results6 varchar(50), p_results7 varchar(50), p_results8 varchar(50), p_results9 varchar(50), p_results10 varchar(50),
	p_default varchar(50)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_id integer;
	v_results_id integer;
	v_default_id integer;
BEGIN
	if ( exists ( select 1 from SobekCM_Item_Aggregation where Code = p_code )) then
		select AggregationID into v_id from SobekCM_Item_Aggregation where Code = p_code;

		CREATE TEMP TABLE temp_existing_views ( ResultTypeId integer primary key, AggrSpecificId integer, StillExisting boolean ) ON COMMIT DROP;
		insert into temp_existing_views
		select ItemAggregationResultTypeID, ItemAggregationResultID, false
		from SobekCM_Item_Aggregation_Result_Views V
		where ( V.AggregationID=v_id );

		-- Add the FIRST results view
		if (( length(p_results1) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results1))) then
			select ItemAggregationResultTypeID into v_results_id from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results1;
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=v_id and ItemAggregationResultTypeID=v_results_id )) then
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( v_id, v_results_id, 'false' );
			else
				update temp_existing_views set StillExisting=true where ResultTypeId=v_results_id;
			end if;
		end if;

		-- Add the SECOND results view
		if (( length(p_results2) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results2))) then
			select ItemAggregationResultTypeID into v_results_id from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results2;
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=v_id and ItemAggregationResultTypeID=v_results_id )) then
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( v_id, v_results_id, 'false' );
			else
				update temp_existing_views set StillExisting=true where ResultTypeId=v_results_id;
			end if;
		end if;

		-- Add the THIRD results view
		if (( length(p_results3) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results3))) then
			select ItemAggregationResultTypeID into v_results_id from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results3;
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=v_id and ItemAggregationResultTypeID=v_results_id )) then
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( v_id, v_results_id, 'false' );
			else
				update temp_existing_views set StillExisting=true where ResultTypeId=v_results_id;
			end if;
		end if;

		-- Add the FOURTH results view
		if (( length(p_results4) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results4))) then
			select ItemAggregationResultTypeID into v_results_id from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results4;
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=v_id and ItemAggregationResultTypeID=v_results_id )) then
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( v_id, v_results_id, 'false' );
			else
				update temp_existing_views set StillExisting=true where ResultTypeId=v_results_id;
			end if;
		end if;

		-- Add the FIFTH results view
		if (( length(p_results5) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results5))) then
			select ItemAggregationResultTypeID into v_results_id from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results5;
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=v_id and ItemAggregationResultTypeID=v_results_id )) then
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( v_id, v_results_id, 'false' );
			else
				update temp_existing_views set StillExisting=true where ResultTypeId=v_results_id;
			end if;
		end if;

		-- Add the SIXTH results view
		if (( length(p_results6) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results6))) then
			select ItemAggregationResultTypeID into v_results_id from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results6;
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=v_id and ItemAggregationResultTypeID=v_results_id )) then
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( v_id, v_results_id, 'false' );
			else
				update temp_existing_views set StillExisting=true where ResultTypeId=v_results_id;
			end if;
		end if;

		-- Add the SEVENTH results view
		if (( length(p_results7) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results7))) then
			select ItemAggregationResultTypeID into v_results_id from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results7;
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=v_id and ItemAggregationResultTypeID=v_results_id )) then
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( v_id, v_results_id, 'false' );
			else
				update temp_existing_views set StillExisting=true where ResultTypeId=v_results_id;
			end if;
		end if;

		-- Add the EIGHTH results view
		if (( length(p_results8) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results8))) then
			select ItemAggregationResultTypeID into v_results_id from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results8;
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=v_id and ItemAggregationResultTypeID=v_results_id )) then
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( v_id, v_results_id, 'false' );
			else
				update temp_existing_views set StillExisting=true where ResultTypeId=v_results_id;
			end if;
		end if;

		-- Add the NINTH results view
		if (( length(p_results9) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results9))) then
			select ItemAggregationResultTypeID into v_results_id from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results9;
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=v_id and ItemAggregationResultTypeID=v_results_id )) then
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( v_id, v_results_id, 'false' );
			else
				update temp_existing_views set StillExisting=true where ResultTypeId=v_results_id;
			end if;
		end if;

		-- Add the TENTH results view
		if (( length(p_results10) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results10))) then
			select ItemAggregationResultTypeID into v_results_id from SobekCM_Item_Aggregation_Result_Types where ResultType=p_results10;
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=v_id and ItemAggregationResultTypeID=v_results_id )) then
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( v_id, v_results_id, 'false' );
			else
				update temp_existing_views set StillExisting=true where ResultTypeId=v_results_id;
			end if;
		end if;

		if (( select count(*) from temp_existing_views ) > 0 ) then
			delete from SobekCM_Item_Aggregation_Result_Fields
			where exists ( select 1 from temp_existing_views V where V.StillExisting=false and V.AggrSpecificId=ItemAggregationResultID);

			delete from SobekCM_Item_Aggregation_Result_Views
			where exists ( select 1 from temp_existing_views V where V.StillExisting=false and V.AggrSpecificId=ItemAggregationResultID);
		end if;

		-- Set the DEFAULT view
		if (( length(p_default) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=p_default ))) then
			select ItemAggregationResultTypeID into v_default_id from SobekCM_Item_Aggregation_Result_Types where ResultType=p_default;

			update SobekCM_Item_Aggregation_Result_Views
			set DefaultView = 'false'
			where AggregationID = v_id and ItemAggregationResultTypeID != v_default_id;

			update SobekCM_Item_Aggregation_Result_Views
			set DefaultView = 'true'
			where AggregationID = v_id and ItemAggregationResultTypeID = v_default_id;
		end if;

		drop table temp_existing_views;
	end if;
END;
$$;

-- Saves the behavior information about an item in this library
-- Written by Mark Sullivan
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
		if (( select count(*) from SobekCM_Item_Aggregation where Code = p_HoldingCode ) = 0 ) then
			insert into SobekCM_Item_Aggregation ( Code, Name, ShortName, Description, ThematicHeadingID, Type, isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
			values ( p_HoldingCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
		end if;

		PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_HoldingCode);
	end if;

	-- Check for Source Institution Code
	if ( length ( coalesce ( p_SourceCode, '' ) ) > 0 ) then
		if (( select count(*) from SobekCM_Item_Aggregation where Code = p_SourceCode ) = 0 ) then
			insert into SobekCM_Item_Aggregation ( Code, Name, ShortName, Description, ThematicHeadingID, Type, isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
			values ( p_SourceCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
		end if;

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


-- Saves the behavior information about an item in this library
-- Written by Mark Sullivan
CREATE OR REPLACE FUNCTION SobekCM_Save_Item_Behaviors_Minimal(
	p_ItemID integer,
	p_TextSearchable boolean
)
RETURNS void
LANGUAGE sql
AS $$
	update SobekCM_Item
	set TextSearchable = p_TextSearchable
	where ( ItemID = p_ItemID );
$$;

CREATE OR REPLACE FUNCTION SobekCM_Save_Item_Footprint(
	p_ItemID integer,
	p_point_latitude double precision,
	p_point_longitude double precision,
	p_rect_latitude_A double precision,
	p_rect_longitude_A double precision,
	p_rect_latitude_B double precision,
	p_rect_longitude_B double precision,
	p_segment_kml text
)
RETURNS void
LANGUAGE sql
AS $$
	insert into SobekCM_Item_Footprint( ItemID, Point_Latitude, Point_Longitude, Rect_Latitude_A, Rect_Longitude_A, Rect_Latitude_B, Rect_Longitude_B, Segment_KML )
	values ( p_itemid, p_point_latitude, p_point_longitude, p_rect_latitude_a, p_rect_longitude_a, p_rect_latitude_b, p_rect_longitude_b, p_segment_kml );
$$;


-- Saves all the main data about a group of items in UFDC
-- Written by Mark Sullivan (September 2006, Modified October 2011 )
CREATE OR REPLACE FUNCTION SobekCM_Save_Item_Group(
	p_BibID varchar(10),
	p_GroupTitle varchar(500),
	p_SortTitle varchar(255),
	p_Type varchar(50),
	p_File_Location varchar(100),
	p_OCLC_Number bigint,
	p_ALEPH_Number integer,
	p_Group_Thumbnail varchar(500),
	p_Large_Format boolean,
	p_Track_By_Month boolean,
	p_Never_Overlay_Record boolean,
	p_Update_Existing boolean,
	p_PrimaryIdentifierType varchar(50),
	p_PrimaryIdentifier varchar(100),
	OUT p_GroupID integer,
	OUT p_New_BibID varchar(10),
	OUT p_New_Group boolean
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_next_bibid_number integer;
BEGIN
	p_New_BibID := p_BibID;
	p_New_Group := false;

	if (( select count(*) from SobekCM_Item_Group  where ( BibID = p_BibID ))  < 1 ) then
		-- Verify the BibID is a complete bibid, otherwise find the next one
		if ( LENGTH(p_bibid) < 10 ) then
			select coalesce(CAST(REPLACE(MAX(BibID), p_bibid, '') as integer) + 1,-1) into v_next_bibid_number
			from SobekCM_Item_Group
			where BibID like p_bibid || '%';

			if ( v_next_bibid_number < 0 ) then
				p_New_BibID := p_bibid || RIGHT('00000001', 10-LENGTH(p_bibid));
			else
				p_New_BibID := p_bibid || RIGHT('00000000' || (CAST( v_next_bibid_number as varchar(10))), 10-LENGTH(p_bibid));
			end if;
		end if;

		if ( LENGTH(p_File_Location) = 0 ) then
			p_File_Location := SUBSTRING(p_New_BibID,1 ,2 ) || '\' || SUBSTRING(p_New_BibID,3,2) || '\' || SUBSTRING(p_New_BibID,5,2) || '\' || SUBSTRING(p_New_BibID,7,2) || '\' || SUBSTRING(p_New_BibID,9,2);
		end if;

		insert into SobekCM_Item_Group ( BibID, GroupTitle, Deleted, Type, SortTitle, ItemCount, File_Location, GroupCreateDate, OCLC_Number, ALEPH_Number, GroupThumbnail, Track_By_Month, Large_Format, Never_Overlay_Record, Primary_Identifier_Type, Primary_Identifier, LastFourInt )
		values ( p_New_BibID, p_GroupTitle, false, p_Type, p_SortTitle, 0, p_File_Location, now(), p_OCLC_Number, p_ALEPH_Number, p_Group_Thumbnail, p_Track_By_Month, p_Large_Format, p_Never_Overlay_Record, p_PrimaryIdentifierType, p_PrimaryIdentifier, cast(substring(p_BibID, 7, 4) as smallint ) )
		returning GroupID into p_GroupID;

		p_New_Group := true;
	else
		select GroupID into p_GroupID
		from SobekCM_Item_Group
		where BibID = p_BibID;

		if ( p_Update_Existing ) then
			update SobekCM_Item_Group
			set GroupTitle=p_GroupTitle, Type=p_Type, SortTitle=p_SortTitle, OCLC_Number=p_OCLC_Number, ALEPH_Number=p_ALEPH_Number, GroupThumbnail=p_Group_Thumbnail, Track_By_Month = p_Track_By_Month, Large_Format=p_Large_Format, Never_Overlay_Record = p_Never_Overlay_Record, Primary_Identifier_Type=p_PrimaryIdentifierType, Primary_Identifier=p_PrimaryIdentifier
			where BibID = p_BibID;
		end if;

		p_New_Group := false;
	end if;
END;
$$;


-- Saves all the web skin data about a group of items in UFDC
-- Written by Mark Sullivan (September 2006, Modified August 2010 )
CREATE OR REPLACE FUNCTION SobekCM_Save_Item_Group_Web_Skins(
	p_GroupID integer,
	p_Primary_WebSkin varchar(20),
	p_Alt_WebSkin1 varchar(20), p_Alt_WebSkin2 varchar(20), p_Alt_WebSkin3 varchar(20), p_Alt_WebSkin4 varchar(20),
	p_Alt_WebSkin5 varchar(20), p_Alt_WebSkin6 varchar(20), p_Alt_WebSkin7 varchar(20), p_Alt_WebSkin8 varchar(20),
	p_Alt_WebSkin9 varchar(20)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_InterfaceID integer;
BEGIN
	delete from SobekCM_Item_Group_Web_Skin_Link
	where GroupID = p_GroupID;

	if ( length( coalesce( p_Primary_WebSkin, '' )) > 0 ) then
		select WebSkinID into v_InterfaceID from SobekCM_Web_Skin where WebSkinCode = p_Primary_WebSkin;
		if ( coalesce(v_InterfaceID,-1) > 0 ) then
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, "Sequence" )
			values ( p_GroupID, v_InterfaceID, 1 );
		end if;
	end if;

	if ( length( coalesce( p_Alt_WebSkin1, '' )) > 0 ) then
		select WebSkinID into v_InterfaceID from SobekCM_Web_Skin where WebSkinCode = p_Alt_WebSkin1;
		if ( coalesce(v_InterfaceID,-1) > 0 ) then
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, "Sequence" )
			values ( p_GroupID, v_InterfaceID, 2 );
		end if;
	end if;

	if ( length( coalesce( p_Alt_WebSkin2, '' )) > 0 ) then
		select WebSkinID into v_InterfaceID from SobekCM_Web_Skin where WebSkinCode = p_Alt_WebSkin2;
		if ( coalesce(v_InterfaceID,-1) > 0 ) then
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, "Sequence" )
			values ( p_GroupID, v_InterfaceID, 3 );
		end if;
	end if;

	if ( length( coalesce( p_Alt_WebSkin3, '' )) > 0 ) then
		select WebSkinID into v_InterfaceID from SobekCM_Web_Skin where WebSkinCode = p_Alt_WebSkin3;
		if ( coalesce(v_InterfaceID,-1) > 0 ) then
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, "Sequence" )
			values ( p_GroupID, v_InterfaceID, 4 );
		end if;
	end if;

	if ( length( coalesce( p_Alt_WebSkin4, '' )) > 0 ) then
		select WebSkinID into v_InterfaceID from SobekCM_Web_Skin where WebSkinCode = p_Alt_WebSkin4;
		if ( coalesce(v_InterfaceID,-1) > 0 ) then
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, "Sequence" )
			values ( p_GroupID, v_InterfaceID, 5 );
		end if;
	end if;

	if ( length( coalesce( p_Alt_WebSkin5, '' )) > 0 ) then
		select WebSkinID into v_InterfaceID from SobekCM_Web_Skin where WebSkinCode = p_Alt_WebSkin5;
		if ( coalesce(v_InterfaceID,-1) > 0 ) then
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, "Sequence" )
			values ( p_GroupID, v_InterfaceID, 6 );
		end if;
	end if;

	if ( length( coalesce( p_Alt_WebSkin6, '' )) > 0 ) then
		select WebSkinID into v_InterfaceID from SobekCM_Web_Skin where WebSkinCode = p_Alt_WebSkin6;
		if ( coalesce(v_InterfaceID,-1) > 0 ) then
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, "Sequence" )
			values ( p_GroupID, v_InterfaceID, 7 );
		end if;
	end if;

	if ( length( coalesce( p_Alt_WebSkin7, '' )) > 0 ) then
		select WebSkinID into v_InterfaceID from SobekCM_Web_Skin where WebSkinCode = p_Alt_WebSkin7;
		if ( coalesce(v_InterfaceID,-1) > 0 ) then
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, "Sequence" )
			values ( p_GroupID, v_InterfaceID, 8 );
		end if;
	end if;

	if ( length( coalesce( p_Alt_WebSkin8, '' )) > 0 ) then
		select WebSkinID into v_InterfaceID from SobekCM_Web_Skin where WebSkinCode = p_Alt_WebSkin8;
		if ( coalesce(v_InterfaceID,-1) > 0 ) then
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, "Sequence" )
			values ( p_GroupID, v_InterfaceID, 9 );
		end if;
	end if;

	if ( length( coalesce( p_Alt_WebSkin9, '' )) > 0 ) then
		select WebSkinID into v_InterfaceID from SobekCM_Web_Skin where WebSkinCode = p_Alt_WebSkin9;
		if ( coalesce(v_InterfaceID,-1) > 0 ) then
			insert into SobekCM_Item_Group_Web_Skin_Link ( GroupID, WebSkinID, "Sequence" )
			values ( p_GroupID, v_InterfaceID, 10 );
		end if;
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Save_Item_User_Group_Permissions(
	p_ItemId integer,
	p_UserGroupId integer,
	p_isOwner boolean,
	p_canView boolean,
	p_canEditMetadata boolean,
	p_canEditBehaviors boolean,
	p_canPerformQc boolean,
	p_canUploadFiles boolean,
	p_canChangeVisibility boolean,
	p_canDelete boolean,
	p_customPermissions text,
	p_isDefaultPermissions boolean
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if (( select count(*) from mySobek_User_Group_Item_Permissions where ItemId=p_ItemId and UserGroupID=p_UserGroupId) > 0 ) then
		update mySobek_User_Group_Item_Permissions
		set isOwner=p_isOwner, canView=p_canView, canEditMetadata=p_canEditMetadata, canEditBehaviors=p_canEditBehaviors,
		    canPerformQc=p_canPerformQc, canUploadFiles=p_canUploadFiles, canChangeVisibility=p_canChangeVisibility,
			canDelete=p_canDelete, customPermissions=p_customPermissions, isDefaultPermissions=p_isDefaultPermissions
		where ItemId=p_ItemId and UserGroupId=p_UserGroupId;
	else
		insert into mySobek_User_Group_Item_Permissions ( UserGroupID, ItemID, isOwner, canView, canEditMetadata, canEditBehaviors, canPerformQc, canUploadFiles, canChangeVisibility, canDelete, customPermissions, isDefaultPermissions )
		values ( p_UserGroupId, p_ItemId, p_isOwner, p_canView, p_canEditMetadata, p_canEditBehaviors, p_canPerformQc, p_canUploadFiles, p_canChangeVisibility, p_canDelete, p_customPermissions, p_isDefaultPermissions);
	end if;
END;
$$;

CREATE OR REPLACE FUNCTION SobekCM_Save_Item_User_Permissions(
	p_ItemId integer,
	p_UserId integer,
	p_isOwner boolean,
	p_canView boolean,
	p_canEditMetadata boolean,
	p_canEditBehaviors boolean,
	p_canPerformQc boolean,
	p_canUploadFiles boolean,
	p_canChangeVisibility boolean,
	p_canDelete boolean,
	p_customPermissions text
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if (( select count(*) from mySobek_User_Item_Permissions where ItemId=p_ItemId and UserID=p_UserId) > 0 ) then
		update mySobek_User_Item_Permissions
		set isOwner=p_isOwner, canView=p_canView, canEditMetadata=p_canEditMetadata, canEditBehaviors=p_canEditBehaviors,
		    canPerformQc=p_canPerformQc, canUploadFiles=p_canUploadFiles, canChangeVisibility=p_canChangeVisibility,
			canDelete=p_canDelete, customPermissions=p_customPermissions
		where ItemId=p_ItemId and UserId=p_UserId;
	else
		insert into mySobek_User_Item_Permissions ( UserID, ItemID, isOwner, canView, canEditMetadata, canEditBehaviors, canPerformQc, canUploadFiles, canChangeVisibility, canDelete, customPermissions )
		values ( p_UserId, p_ItemId, p_isOwner, p_canView, p_canEditMetadata, p_canEditBehaviors, p_canPerformQc, p_canUploadFiles, p_canChangeVisibility, p_canDelete, p_customPermissions );
	end if;
END;
$$;


-- Saves all the main data for a new item in a SobekCM library,
-- including the serial hierarchy, behaviors, tracking, and basic item information
-- Written by Mark Sullivan ( January 2011 )
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
			if (( select count(*) from SobekCM_Item_Aggregation where Code = p_HoldingCode ) = 0 ) then
				insert into SobekCM_Item_Aggregation ( Code, Name, ShortName, Description, ThematicHeadingID, Type, isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
				values ( p_HoldingCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
			end if;

			PERFORM SobekCM_Save_Item_Item_Aggregation_Link(p_ItemID, p_HoldingCode);
		end if;

		if ( length ( coalesce ( p_SourceCode, '' ) ) > 0 ) then
			if (( select count(*) from SobekCM_Item_Aggregation where Code = p_SourceCode ) = 0 ) then
				insert into SobekCM_Item_Aggregation ( Code, Name, ShortName, Description, ThematicHeadingID, Type, isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
				values ( p_SourceCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
			end if;

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

CREATE OR REPLACE FUNCTION SobekCM_Save_Project(
	p_ProjectID integer,
	p_ProjectCode varchar(20),
	p_ProjectName varchar(100),
	p_ProjectManager varchar(100),
	p_GrantID varchar(250),
	p_GrantName bigint,
	p_StartDate date,
	p_EndDate date,
	p_isActive boolean,
	p_Description text,
	p_Specifications text,
	p_Priority varchar(100),
	p_QC_Profile varchar(100),
	p_TargetItemCount integer,
	p_TargetPageCount integer,
	p_Comments text,
	p_CopyrightPermissions varchar(1000),
	OUT p_New_ProjectID integer
)
LANGUAGE plpgsql
AS $$
BEGIN
	p_New_ProjectID := p_ProjectID;

	if (( select count(*) from SobekCM_Project  where ( ProjectID = p_ProjectID ))  < 1 ) then
		insert into SobekCM_Project (ProjectCode, ProjectName, ProjectManager, GrantID, GrantName, StartDate, EndDate, isActive, Description, Specifications, Priority,QC_Profile, TargetItemCount, TargetPageCount, Comments, CopyrightPermissions)
		values (p_ProjectCode, p_ProjectName, p_ProjectManager, p_GrantID, p_GrantName, p_StartDate, p_EndDate, p_isActive, p_Description, p_Specifications, p_Priority, p_QC_Profile, p_TargetItemCount, p_TargetPageCount, p_Comments, p_CopyrightPermissions)
		returning ProjectID into p_New_ProjectID;
	else
		update SobekCM_Project
		set ProjectCode=p_ProjectCode, ProjectName=p_ProjectName, ProjectManager=p_ProjectManager, GrantID=p_GrantID, GrantName=p_GrantName, StartDate=p_StartDate, EndDate=p_EndDate, isActive=p_isActive, Description=p_Description, Specifications=p_Specifications, Priority=p_Priority, QC_Profile=p_QC_Profile, TargetItemCount=p_TargetItemCount, TargetPageCount=p_TargetPageCount, Comments=p_Comments, CopyrightPermissions=p_CopyrightPermissions
		where ProjectID=p_ProjectID;
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Save_Project_Aggregation_Link(
	p_ProjectID integer,
	p_AggregationID integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if((select count(*) from SobekCM_Project_Aggregation_Link  where ( ProjectID = p_ProjectID and AggregationID=p_AggregationID ))  < 1 ) then
		insert into SobekCM_Project_Aggregation_Link(ProjectID, AggregationID)
		values(p_ProjectID, p_AggregationID);
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Save_Project_DefaultMetadata_Link(
	p_ProjectID integer,
	p_DefaultMetadataID integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if((select count(*) from SobekCM_Project_DefaultMetadata_Link  where ( ProjectID = p_ProjectID and DefaultMetadataID=p_DefaultMetadataID ))  < 1 ) then
		insert into SobekCM_Project_DefaultMetadata_Link(ProjectID, DefaultMetadataID)
		values(p_ProjectID, p_DefaultMetadataID);
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Save_Project_Item_Link(
	p_ProjectID integer,
	p_ItemID integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if((select count(*) from SobekCM_Project_Item_Link  where ( ProjectID = p_ProjectID and ItemID=p_ItemID ))  < 1 ) then
		insert into SobekCM_Project_Item_Link(ProjectID, ItemID)
		values(p_ProjectID, p_ItemID);
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Save_Project_Template_Link(
	p_ProjectID integer,
	p_TemplateID integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if((select count(*) from SobekCM_Project_Template_Link  where ( ProjectID = p_ProjectID and TemplateID=p_TemplateID ))  < 1 ) then
		insert into SobekCM_Project_Template_Link(ProjectID, TemplateID)
		values(p_ProjectID, p_TemplateID);
	end if;
END;
$$;


-- Adds the link between the item and the group and also adds the serial hierarchy
-- Stored procedure written by Mark Sullivan ( September 2006 )
CREATE OR REPLACE FUNCTION SobekCM_Save_Serial_Hierarchy(
	p_GroupID integer,
	p_ItemID integer,
	p_Level1_Text varchar(255), p_Level1_Index integer,
	p_Level2_Text varchar(255), p_Level2_Index integer,
	p_Level3_Text varchar(255), p_Level3_Index integer,
	p_Level4_Text varchar(255), p_Level4_Index integer,
	p_Level5_Text varchar(255), p_Level5_Index integer,
	p_SerialHierarchy varchar(500)
)
RETURNS void
LANGUAGE sql
AS $$
	update SobekCM_Item
	set Level1_Text = p_Level1_Text, Level1_Index = p_Level1_Index,
		Level2_Text = p_Level2_Text, Level2_Index = p_Level2_Index,
		Level3_Text = p_Level3_Text, Level3_Index = p_Level3_Index,
		Level4_Text = p_Level4_Text, Level4_Index = p_Level4_Index,
		Level5_Text = p_Level5_Text, Level5_Index = p_Level5_Index
	where ItemID=p_ItemID;
$$;


-- Sends an email via database mail and additionally logs that the email was sent.
--
-- PORTING GAP: SQL Server's Database Mail (msdb.dbo.sp_send_dbmail) has no PostgreSQL
-- equivalent -- PostgreSQL cannot send email from inside the database engine. This port
-- keeps the logging/governor logic (which is portable SQL) but the actual dispatch calls
-- are omitted; sending the email needs to move to the application layer (e.g. the C# caller
-- sending via SMTP after this function logs the attempt) for the PostgreSQL deployment.
CREATE OR REPLACE FUNCTION SobekCM_Send_Email(
	p_recipients_list varchar(250),
	p_subject_line varchar(500),
	p_email_body text,
	p_from_address varchar(250),
	p_reply_to varchar(250),
	p_html_format boolean,
	p_contact_us boolean,
	p_replytoemailid integer,
	p_userid integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if (( p_userid < 0 ) or (( select count(*) from SobekCM_Email_Log where UserID = p_userid and Sent_Date > now() - interval '1 day') < 20 )) then
		if ( ( select count(*) from SobekCM_Email_Log where Receipt_List=p_recipients_list and Sent_Date > now() - interval '1 day') > 250 ) then
			insert into SobekCM_Email_Log( Sender, Receipt_List, Subject_Line, Email_Body, Sent_Date, HTML_Format, Contact_Us, ReplyToEmailId, UserID )
			values ( 'sobekcm noreply profile', p_recipients_list, p_subject_line || '(not delivered)', 'Too many emails to this recipient list in last 24 hours.  Governer kicked in and this email was not sent.   ' || p_email_body, now(), p_html_format, p_contact_us, p_replytoemailid, p_userid );
		else
			insert into SobekCM_Email_Log( Sender, Receipt_List, Subject_Line, Email_Body, Sent_Date, HTML_Format, Contact_Us, ReplyToEmailId, UserID )
			values ( 'sobekcm noreply profile', p_recipients_list, p_subject_line, p_email_body, now(), p_html_format, p_contact_us, p_replytoemailid, p_userid );

			-- NOTE: actual email dispatch (sp_send_dbmail in the original) intentionally omitted -- see porting gap note above.
		end if;
	end if;
END;
$$;


-- Set the IP restriction mask on a single item, by a single user, and
-- add a progress note that this was done
CREATE OR REPLACE FUNCTION SobekCM_Set_IP_Restriction_Mask(
	p_itemid integer,
	p_newipmask integer,
	p_user varchar(50),
	p_progressnote varchar(1000)
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	update SobekCM_Item
	set IP_Restriction_Mask=p_newipmask
	where ItemID=p_itemid;

	if ( p_newipmask < 0 ) then
		insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote )
		values ( p_itemid, 35, now(), p_user, p_progressnote );
	else
		if ( p_newipmask = 0 ) then
			insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote )
			values ( p_itemid, 34, now(), p_user, p_progressnote );

			update SobekCM_Item_Aggregation
			set LastItemAdded = now()
			where exists ( select * from SobekCM_Item_Aggregation_Item_Link L where L.ItemID=p_itemid and L.AggregationID = SobekCM_Item_Aggregation.AggregationID );
		else
			insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote )
			values ( p_itemid, 36, now(), p_user, p_progressnote );
		end if;

		update SobekCM_Item
		set Milestone_DigitalAcquisition = coalesce(Milestone_DigitalAcquisition, now()),
		    Milestone_ImageProcessing = coalesce(Milestone_ImageProcessing, now()),
		    Milestone_QualityControl = coalesce(Milestone_QualityControl, now()),
		    Milestone_OnlineComplete = coalesce(Milestone_OnlineComplete, now()),
		    Last_MileStone=4
		where ItemID=p_itemid;
	end if;
END;
$$;

-- Procedure sets the internal comments for an item
CREATE OR REPLACE FUNCTION SobekCM_Set_Item_Comments(
	p_itemid integer,
	p_newcomments varchar(1000)
)
RETURNS void
LANGUAGE sql
AS $$
	update SobekCM_Item
	set Internal_Comments=p_newcomments
	where ItemID = p_itemid;
$$;


-- Sets a single item setting value, by key.  Adds a new one if this
-- is a new setting key, otherwise updates the existing value.
CREATE OR REPLACE FUNCTION SobekCM_Set_Item_Setting_Value(
	p_ItemID integer,
	p_Setting_Key varchar(255),
	p_Setting_Value text
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if ( ( select COUNT(*) from SobekCM_Item_Settings where Setting_Key=p_Setting_Key and ItemID=p_ItemID ) > 0 ) then
		update SobekCM_Item_Settings set Setting_Value=p_Setting_Value where Setting_Key = p_Setting_Key and ItemID=p_ItemID;
	else
		insert into SobekCM_Item_Settings( ItemID, Setting_Key, Setting_Value )
		values ( p_ItemID, p_Setting_Key, p_Setting_Value );
	end if;
END;
$$;


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
	set IP_Restriction_Mask = p_IpRestrictionMask, Dark = p_DarkFlag, AdditionalWorkNeeded = 'true'
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


-- Set the main thumbnail for an individual item
CREATE OR REPLACE FUNCTION SobekCM_Set_Main_Thumbnail(
	p_bibid varchar(10),
	p_vid varchar(5),
	p_mainthumb varchar(100)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_itemid integer;
BEGIN
	v_itemid := coalesce(( select ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID=p_bibid and I.VID=p_vid ), -1 );

	if ( v_itemid > 0 ) then
		update SobekCM_Item set MainThumbnail=p_mainthumb where ItemID=v_itemid;
	end if;
END;
$$;


-- Sets a single system-wide setting value, by key.  Adds a new one if this
-- is a new setting key, otherwise updates the existing value.
CREATE OR REPLACE FUNCTION SobekCM_Set_Setting_Value(
	p_Setting_Key varchar(255),
	p_Setting_Value text
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if ( ( select COUNT(*) from SobekCM_Settings where Setting_Key = p_Setting_Key ) > 0 ) then
		update SobekCM_Settings set Setting_Value=p_Setting_Value where Setting_Key = p_Setting_Key;
	else
		insert into SobekCM_Settings( Setting_Key, Setting_Value )
		values ( p_Setting_Key, p_Setting_Value );
	end if;
END;
$$;


-- Sets a single user setting value, by key.  Adds a new one if this
-- is a new setting key, otherwise updates the existing value.
CREATE OR REPLACE FUNCTION SobekCM_Set_User_Setting_Value(
	p_UserID integer,
	p_Setting_Key varchar(255),
	p_Setting_Value text
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if ( ( select COUNT(*) from mySobek_User_Settings where Setting_Key=p_Setting_Key and UserID=p_UserID ) > 0 ) then
		update mySobek_User_Settings set Setting_Value=p_Setting_Value where Setting_Key = p_Setting_Key and UserID=p_UserID;
	else
		insert into mySobek_User_Settings( UserID, Setting_Key, Setting_Value )
		values ( p_UserID, p_Setting_Key, p_Setting_Value );
	end if;
END;
$$;


-- Retrieve the very simple list of items to save in XML format or to step through
-- and add to the solr/lucene index, etc..
CREATE OR REPLACE FUNCTION SobekCM_Simple_Item_List(
	p_collection_code varchar(10)
)
RETURNS TABLE (
	BibID varchar(10),
	VID varchar(5),
	Title varchar(500),
	CreateDate timestamp,
	Resource_Link varchar(100),
	LastSaved timestamp
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ( length( coalesce( p_collection_code, '' )) = 0 ) then
		RETURN QUERY
		select G.BibID, I.VID, I.Title, I.CreateDate, I.File_Location as Resource_Link, I.LastSaved
		from SobekCM_Item_Group G, SobekCM_Item I
		where ( G.GroupID = I.GroupID )
		  and ( I.IP_Restriction_Mask = 0 )
		  and ( G.Deleted = false )
	      and ( I.Deleted = false )
		  and ( I.Dark = false );
	else
		RETURN QUERY
		select G.BibID, I.VID, I.Title, I.CreateDate, I.File_Location as Resource_Link, I.LastSaved
		from SobekCM_Item_Group G, SobekCM_Item I, SobekCM_Item_Aggregation C, SobekCM_Item_Aggregation_Item_Link CL
		where ( G.GroupID = I.GroupID )
		  and ( I.IP_Restriction_Mask = 0 )
		  and ( G.Deleted = false )
	      and ( I.Deleted = false )
		  and ( I.Dark = false )
		  and ( I.ItemID = CL.ItemID )
		  and ( CL.AggregationID = C.AggregationID )
		  and ( Code = p_collection_code );
	end if;
END;
$$;

-- Aggregates the item and title statistics to the subcollection, collection
-- and institutional level for a given month and year
CREATE OR REPLACE FUNCTION SobekCM_Statistics_Aggregate(
	p_statyear integer,
	p_statmonth integer,
	OUT p_message varchar(1000)
)
LANGUAGE plpgsql
AS $$
BEGIN
	-- Should only do this aggregation for each year month ONCE.
	if not exists ( select * from SobekCM_Statistics where "Year"=p_statyear and "Month"=p_statmonth ) then
		p_message := 'No row for this year/month is present in the SobekCM_Statistics table.  Add usage stats before trying to aggregate this month.';
		RAISE NOTICE '%', p_message;
		return;
	end if;

	if exists ( select * from SobekCM_Statistics where Aggregate_Statistics_Complete='true' and "Year"=p_statyear and "Month"=p_statmonth ) then
		p_message := 'Statistics for this month have already been aggregated.  You cannot aggregate the same year/month twice without introducing errors.';
		RAISE NOTICE '%', p_message;
		return;
	end if;

	CREATE TEMP TABLE temp_item_aggregation ON COMMIT DROP AS
	select AggregationID, Hits, JPEG_Views, Zoomable_Views, Citation_Views, Thumbnail_Views, Text_Search_Views, Flash_Views, Google_Map_Views, Download_Views, Static_Views
	from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Statistics S
	where ( S.ItemID = I.ItemID )
	  and ( I.ItemID = L.ItemID )
	  and ( S."Year" = p_statyear )
	  and ( S."Month" = p_statmonth )
	order by AggregationID;

	CREATE TEMP TABLE temp_aggregation_stats ON COMMIT DROP AS
	select distinct(AggregationID), sum( Hits) as Item_Hits, sum(JPEG_Views) as JPEG_Views, sum(Zoomable_Views) as Zoomable_Views,
	  sum ( Citation_Views) as Citation_Views, sum( Thumbnail_Views ) as Thumbnail_Views, sum( Text_Search_Views) as Text_Search_Views, sum (Flash_Views) as Flash_Views,
	  sum(Google_Map_Views) as Google_Map_Views, sum(Download_Views) as Download_Views, sum(Static_Views) as Static_Views
	from temp_item_aggregation
	Group by AggregationID;

	CREATE TEMP TABLE temp_title_aggregation ON COMMIT DROP AS
	select AggregationID, cast(AggregationID as varchar(10)) || '_' || cast(S.GroupID as varchar(10)) as Distincter, S.Hits
	from SobekCM_Item_Aggregation_Item_Link CL, SobekCM_Item I, SobekCM_Item_Group_Statistics S
	where ( I.ItemID = CL.ItemID )
	  and ( I.GroupID = S.GroupID )
	  and ( S."Year" = p_statyear )
	  and ( S."Month" = p_statmonth )
	order by Distincter;

	CREATE TEMP TABLE temp_title_aggregation_distinct ON COMMIT DROP AS
	select distinct(Distincter), AggregationID, Hits
	from temp_title_aggregation
	Group by Distincter, AggregationID, Hits;

	CREATE TEMP TABLE temp_aggregation_stats2 ON COMMIT DROP AS
	select distinct(AggregationID), sum( Hits) as Title_Hits
	from temp_title_aggregation_distinct
	Group by AggregationID;

	update SobekCM_Item_Aggregation_Statistics
	set Item_Hits = (select Item_Hits from temp_aggregation_stats where temp_aggregation_stats.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
		Item_JPEG_Views = (select JPEG_VIews from temp_aggregation_stats where temp_aggregation_stats.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
		Item_Zoomable_Views = (select Zoomable_Views from temp_aggregation_stats where temp_aggregation_stats.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
		Item_Citation_Views = (select Citation_Views from temp_aggregation_stats where temp_aggregation_stats.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
		Item_Thumbnail_Views = (select Thumbnail_Views from temp_aggregation_stats where temp_aggregation_stats.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
		Item_Text_Search_Views = (select Text_Search_Views from temp_aggregation_stats where temp_aggregation_stats.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
		Item_Flash_Views = (select Flash_Views from temp_aggregation_stats where temp_aggregation_stats.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
		Item_Google_Map_Views = (select Google_Map_Views from temp_aggregation_stats where temp_aggregation_stats.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
		Item_Download_Views = (select Download_Views from temp_aggregation_stats where temp_aggregation_stats.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID ),
		Item_Static_Views = (select Static_Views from temp_aggregation_stats where temp_aggregation_stats.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID )
	where "Year"=p_statyear and "Month" = p_statmonth;

	insert into SobekCM_Item_Aggregation_Statistics ( AggregationID, "Year",    "Month",     Hits, "Sessions", Home_Page_Views, Browse_Views, Advanced_Search_Views, Search_Results_Views, Title_Hits, Item_Hits, Item_JPEG_Views, Item_Zoomable_Views, Item_Citation_Views, Item_Thumbnail_Views, Item_Text_Search_Views, Item_Flash_Views, Item_Google_Map_Views, Item_Download_Views, Item_Static_Views )
	select                                            AggregationID, p_statyear, p_statmonth,  0,    0,          0,               0,            0,                     0,                    0,          Item_Hits, JPEG_Views,      Zoomable_Views,      Citation_Views,      Thumbnail_Views,      Text_Search_Views,      Flash_Views,      Google_Map_Views,      Download_Views,      Static_Views
	from temp_aggregation_stats
	where not exists ( select * from SobekCM_Item_Aggregation_Statistics S where S.AggregationID = temp_aggregation_stats.AggregationID and S."Year" = p_statyear and S."Month" = p_statmonth );

	update SobekCM_Item_Aggregation_Statistics
	set Title_Hits = (select Title_Hits from temp_aggregation_stats2 where temp_aggregation_stats2.AggregationID = SobekCM_Item_Aggregation_Statistics.AggregationID )
	where "Year"=p_statyear and "Month" = p_statmonth;

	update SobekCM_Item_Aggregation_Statistics
	set Hits = coalesce( Hits + Title_Hits + Item_Hits, 0)
	where "Year" = p_statyear and "Month" = p_statmonth;

	UPDATE SobekCM_Item
	set Total_Hits = coalesce(( select SUM(Hits) from SobekCM_Item_Statistics S where S.ItemID=SobekCM_Item.ItemID ), 0),
		Total_Sessions = coalesce(( select SUM("Sessions") from SobekCM_Item_Statistics S where S.ItemID=SobekCM_Item.ItemID ), 0);

	UPDATE SobekCM_Statistics
	SET Aggregate_Statistics_Complete='true'
	where "Year"=p_statyear and "Month"=p_statmonth;

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

	drop table temp_item_aggregation;
	drop table temp_aggregation_stats;
	drop table temp_title_aggregation;
	drop table temp_title_aggregation_distinct;
	drop table temp_aggregation_stats2;
END;
$$;


-- Returns most often hit titles and items for an aggregation.
-- Originally returned 2 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_Statistics_Aggregation_Titles(
	p_code varchar(20),
	OUT cur_items refcursor,
	OUT cur_titles refcursor
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_aggregationid integer;
BEGIN
	if (( p_code != 'all' ) and ( length(p_code) > 0 )) then
		v_aggregationid := coalesce( (select AggregationID from SobekCM_Item_Aggregation where Code=p_code), -1 );

		OPEN cur_items FOR
		select G.BibID, I.VID, G.GroupTitle, I.Total_Hits
		from SobekCM_Item I, SobekCM_Item_Group G, SobekCM_Item_Aggregation_Item_Link L
		where ( I.GroupID = G.GroupID )
		  and ( I.ItemID = L.ItemID )
		  and ( L.AggregationID = v_aggregationid )
		  and ( I.Total_Hits > 0 )
		order by I.Total_Hits DESC
		limit 100;

		OPEN cur_titles FOR
		select BibID, GroupTitle, SUM(I.Total_Hits) as Title_Hits
		from SobekCM_Item I, SobekCM_Item_Group G, SobekCM_Item_Aggregation_Item_Link L
		where ( I.GroupID = G.GroupID )
		  and ( I.ItemID = L.ItemID )
		  and ( L.AggregationID = v_aggregationid )
		group by BibID, GroupTitle
		having SUM(I.Total_Hits) > 0
		order by Title_Hits DESC
		limit 100;
	else
		OPEN cur_items FOR
		select G.BibID, I.VID, G.GroupTitle, I.Total_Hits
		from SobekCM_Item I, SobekCM_Item_Group G
		where ( I.GroupID = G.GroupID )
		  and ( I.Total_Hits > 0 )
		order by I.Total_Hits DESC
		limit 100;

		OPEN cur_titles FOR
		select BibID, GroupTitle, SUM(I.Total_Hits) as Title_Hits
		from SobekCM_Item I, SobekCM_Item_Group G
		where ( I.GroupID = G.GroupID )
		group by BibID, GroupTitle
		having SUM(I.Total_Hits) > 0
		order by Title_Hits DESC
		limit 100;
	end if;
END;
$$;

CREATE OR REPLACE FUNCTION SobekCM_Statistics_By_Date_Range(
	p_year1 smallint,
	p_month1 smallint,
	p_year2 smallint,
	p_month2 smallint
)
RETURNS TABLE (
	Code varchar(20),
	ChildCode varchar(20),
	Child2Code varchar(20),
	AllCodes varchar(20),
	Name varchar(255),
	ShortName varchar(100),
	"Type" varchar(50),
	isActive text,
	Hits bigint,
	"Sessions" bigint,
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
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_all_id integer;
BEGIN
	v_all_id := coalesce(( select AggregationID from SobekCM_Item_Aggregation where Code='all'), -1);

	CREATE TEMP TABLE temp_agg_list3
	(
	  AggregationID integer,
	  Code varchar(20),
	  ChildCode varchar(20),
	  Child2Code varchar(20),
	  AllCodes varchar(20),
	  Name varchar(255),
	  ShortName varchar(100),
	  "Type" varchar(50),
	  isActive boolean
	) ON COMMIT DROP;

	insert into temp_agg_list3 ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, "Type", isActive )
	select AggregationID, Code, '', '', Code, Name, ShortName, Type, isActive
	from SobekCM_Item_Aggregation A
	where ( Type not like 'Institut%' )
	  and ( Deleted='false' )
	  and ( exists ( select * from SobekCM_Item_Aggregation_Hierarchy where ChildID=A.AggregationID and ParentID=v_all_id)
	       or A.AggregationID=v_all_id );

	insert into temp_agg_list3 ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, "Type", isActive )
	select A2.AggregationID, T.Code, A2.Code, '', A2.Code, A2.Name, A2.ShortName, A2.Type, A2.isActive
	from temp_agg_list3 T, SobekCM_Item_Aggregation A2, SobekCM_Item_Aggregation_Hierarchy H
	where ( A2.Type not like 'Institut%' )
	  and ( T.AggregationID = H.ParentID )
	  and ( A2.AggregationID = H.ChildID )
	  and ( Deleted='false' )
	  and ( T.AggregationID <> v_all_id );

	insert into temp_agg_list3 ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, "Type", isActive )
	select A2.AggregationID, T.Code, T.ChildCode, A2.Code, A2.Code, A2.Name, A2.ShortName, A2.Type, A2.isActive
	from temp_agg_list3 T, SobekCM_Item_Aggregation A2, SobekCM_Item_Aggregation_Hierarchy H
	where ( A2.Type not like 'Institut%' )
	  and ( T.AggregationID = H.ParentID )
	  and ( A2.AggregationID = H.ChildID )
	  and ( Deleted='false' )
	  and ( ChildCode <> '' );

	CREATE TEMP TABLE temp_agg_stats3
	(
	  AggregationID integer,
	  Hits bigint,
	  "Sessions" bigint,
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
	) ON COMMIT DROP;

	insert into temp_agg_stats3 ( AggregationID, Hits, "Sessions", Home_Page_Views, Browse_Views, Search_Results_Views, Title_Hits, Item_Hits, Item_JPEG_Views, Item_Zoomable_Views, Item_Citation_Views, Item_Thumbnail_Views, Item_Text_Search_Views, Item_Flash_Views, Item_Google_Map_Views, Item_Download_Views )
	select S.AggregationID, sum( Hits ) as Hits, sum( "Sessions" ) as "Sessions",
		sum( Home_Page_Views) as Home_Page_Views, sum ( Browse_Views ) as Browse_Views,
		sum ( Search_Results_Views ) as Search_Results_Views,
		sum( Title_Hits ) as Title_Hits, sum ( Item_Hits ) as Item_Hits,
		sum( Item_JPEG_Views ) as Item_JPEG_Views, sum ( Item_Zoomable_Views ) as Item_Zoomable_Views,
		sum ( Item_Citation_Views ) as Item_Citation_Views, sum ( Item_Thumbnail_Views ) as Item_Thumbnail_Views,
		sum ( Item_Text_Search_Views ) as Item_Text_Search_Views, sum ( Item_Flash_Views ) as Item_Flash_Views,
		sum ( Item_Google_Map_Views) as Item_Google_Map_Views, sum( Item_Download_Views ) as item_Download_Views
	from SobekCM_Item_Aggregation_Statistics S, temp_agg_list3 L
	where ( S.AggregationID = L.AggregationID )
	  and ((( p_year1 < p_year2 ) and ((( "Month" >= p_month1 ) and ( "Year" = p_year1 ))
	  or (( "Year" > p_year1 ) and ( "Year" < p_year2 ))
	  or (( "Month" <= p_month2 ) and ( "Year" = p_year2 ))))
	  or (( p_year1 = p_year2 ) and ( "Year" = p_year1 ) and ( "Month" >= p_month1 ) and ( "Month" <= p_month2 )))
	group by S.AggregationID;

	CREATE TEMP TABLE temp_item_stats3 ON COMMIT DROP AS
	select sum( Hits ) as Item_Hits,
		sum( JPEG_Views ) as Item_JPEG_Views, sum ( Zoomable_Views ) as Item_Zoomable_Views,
		sum ( Citation_Views ) as Item_Citation_Views, sum ( Thumbnail_Views ) as Item_Thumbnail_Views,
		sum ( Text_Search_Views ) as Item_Text_Search_Views, sum ( Flash_Views ) as Item_Flash_Views,
		sum ( Google_Map_Views) as Item_Google_Map_Views, sum( Download_Views ) as item_Download_Views
	from SobekCM_Item_Statistics
	where ((( p_year1 < p_year2 ) and ((( "Month" >= p_month1 ) and ( "Year" = p_year1 ))
	  or (( "Year" > p_year1 ) and ( "Year" < p_year2 ))
	  or (( "Month" <= p_month2 ) and ( "Year" = p_year2 ))))
	  or (( p_year1 = p_year2 ) and ( "Year" = p_year1 ) and ( "Month" >= p_month1 ) and ( "Month" <= p_month2 )));

	CREATE TEMP TABLE temp_group_stats3 ON COMMIT DROP AS
	select sum( Hits ) as Title_Hits
	from SobekCM_Item_Group_Statistics
	where ((( p_year1 < p_year2 ) and ((( "Month" >= p_month1 ) and ( "Year" = p_year1 ))
	  or (( "Year" > p_year1 ) and ( "Year" < p_year2 ))
	  or (( "Month" <= p_month2 ) and ( "Year" = p_year2 ))))
	  or (( p_year1 = p_year2 ) and ( "Year" = p_year1 ) and ( "Month" >= p_month1 ) and ( "Month" <= p_month2 )));

	CREATE TEMP TABLE temp_aggregation_stats3b ON COMMIT DROP AS
	select sum(Home_Page_Views) as Home_Page_Views, sum(Browse_Views) as Browse_Views,
		  sum(Search_Results_Views) as Search_Results_Views
	from SobekCM_Item_Aggregation_Statistics
	where ((( p_year1 < p_year2 ) and ((( "Month" >= p_month1 ) and ( "Year" = p_year1 ))
	  or (( "Year" > p_year1 ) and ( "Year" < p_year2 ))
	  or (( "Month" <= p_month2 ) and ( "Year" = p_year2 ))))
	  or (( p_year1 = p_year2 ) and ( "Year" = p_year1 ) and ( "Month" >= p_month1 ) and ( "Month" <= p_month2 )));

	CREATE TEMP TABLE temp_url_stats3 ON COMMIT DROP AS
	select sum( Hits ) as Hits, sum( "Sessions" ) as "Sessions"
	from SobekCM_Statistics
	where ((( p_year1 < p_year2 ) and ((( "Month" >= p_month1 ) and ( "Year" = p_year1 ))
	  or (( "Year" > p_year1 ) and ( "Year" < p_year2 ))
	  or (( "Month" <= p_month2 ) and ( "Year" = p_year2 ))))
	  or (( p_year1 = p_year2 ) and ( "Year" = p_year1 ) and ( "Month" >= p_month1 ) and ( "Month" <= p_month2 )));

	RETURN QUERY
	select C.Code, C.ChildCode, C.Child2Code, C.AllCodes, C.Name, C.ShortName, C.Type, C.isActive::text,
		coalesce( S.Hits, 0 ) as Hits, coalesce( S."Sessions", 0 ) as "Sessions",
		coalesce( S.Home_Page_Views, 0) as Home_Page_Views, coalesce ( S.Browse_Views, 0 ) as Browse_Views,
		coalesce ( S.Search_Results_Views, 0 ) as Search_Results_Views,
		coalesce( S.Title_Hits, 0 ) as Title_Hits, coalesce ( S.Item_Hits, 0 ) as Item_Hits,
		coalesce( S.Item_JPEG_Views, 0 ) as Item_JPEG_Views, coalesce ( S.Item_Zoomable_Views, 0 ) as Item_Zoomable_Views,
		coalesce ( S.Item_Citation_Views, 0 ) as Item_Citation_Views, coalesce ( S.Item_Thumbnail_Views, 0 ) as Item_Thumbnail_Views,
		coalesce ( S.Item_Text_Search_Views, 0 ) as Item_Text_Search_Views, coalesce ( S.Item_Flash_Views, 0 ) as Item_Flash_Views,
		coalesce ( S.Item_Google_Map_Views, 0 ) as Item_Google_Map_Views, coalesce( S.Item_Download_Views, 0 ) as item_Download_Views
	from temp_agg_list3 AS C LEFT OUTER JOIN
	     temp_agg_stats3 AS S on ( C.AggregationID = S.AggregationID )
	union
	select 'ZZZ', '', '', 'ZZZ', 'TOTAL', 'TOTAL', 'TOTAL', 'false',
		A.Hits, A."Sessions", Cc.Home_Page_Views, Cc.Browse_Views, Cc.Search_Results_Views, G.Title_Hits,
		I.Item_Hits, I.Item_JPEG_Views, I.Item_Zoomable_Views, I.Item_Citation_Views, I.Item_Thumbnail_Views,
		I.Item_Text_Search_Views, I.Item_Flash_Views, I.Item_Google_Map_Views, I.Item_Download_Views
	from temp_item_stats3 I, temp_group_stats3 G, temp_url_stats3 A, temp_aggregation_stats3b Cc
	order by Code, ChildCode, Child2Code;

	drop table temp_agg_list3;
	drop table temp_agg_stats3;
END;
$$;


-- Returns the lookup tables for assembling the statistics information.
-- Originally returned 4 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_Statistics_Lookup_Tables(
	OUT cur_items refcursor,
	OUT cur_groups refcursor,
	OUT cur_aggregations refcursor,
	OUT cur_portals refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_items FOR
	select I.ItemID, G.BibID, I.VID
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( I.GroupID = G.GroupID );

	OPEN cur_groups FOR
	select G.GroupID, G.BibID
	from SobekCM_Item_Group G;

	OPEN cur_aggregations FOR
	select S.AggregationID, S.Code, S.Type
	from SobekCM_Item_Aggregation S;

	OPEN cur_portals FOR
	select P.PortalID, P.Base_URL, P.Abbreviation, P.isDefault
	from SobekCM_Portal_URL P
	where P.isActive = 'true';
END;
$$;


CREATE OR REPLACE FUNCTION SobekCM_Statistics_Save_Aggregation(
	p_aggregationid integer,
	p_year smallint,
	p_month smallint,
	p_hits integer,
	p_sessions integer,
	p_home_page_views integer,
	p_browse_views integer,
	p_advanced_search_views integer,
	p_search_results_views integer
)
RETURNS void
LANGUAGE sql
AS $$
	insert into SobekCM_Item_Aggregation_Statistics ( AggregationID, "Year", "Month", Hits, "Sessions", Home_Page_Views, Browse_Views, Advanced_Search_Views, Search_Results_Views )
	values ( p_aggregationid, p_year, p_month, p_hits, p_sessions, p_home_page_views, p_browse_views, p_advanced_search_views, p_search_results_views );
$$;


CREATE OR REPLACE FUNCTION SobekCM_Statistics_Save_Item(
	p_year smallint,
	p_month smallint,
	p_hits integer,
	p_sessions integer,
	p_itemid integer,
	p_jpeg_views integer,
	p_zoomable_views integer,
	p_citation_views integer,
	p_thumbnail_views integer,
	p_text_search_views integer,
	p_flash_views integer,
	p_google_map_views integer,
	p_download_views integer,
	p_static_views integer
)
RETURNS void
LANGUAGE sql
AS $$
	insert into SobekCM_Item_Statistics ( ItemID, "Year", "Month", Hits, "Sessions", JPEG_Views, Zoomable_Views, Citation_Views,
		Thumbnail_Views, Text_Search_Views, Flash_Views, Google_Map_Views, Download_Views, Static_Views )
	values ( p_itemid, p_year, p_month, p_hits, p_sessions, p_jpeg_views, p_zoomable_views, p_citation_views,
		p_thumbnail_views, p_text_search_views, p_flash_views, p_google_map_views, p_download_views, p_static_views );
$$;


CREATE OR REPLACE FUNCTION SobekCM_Statistics_Save_Item_Group(
	p_year smallint,
	p_month smallint,
	p_hits integer,
	p_sessions integer,
	p_groupid integer
)
RETURNS void
LANGUAGE sql
AS $$
	insert into SobekCM_Item_Group_Statistics ( GroupID, "Year", "Month", Hits, "Sessions" )
	values ( p_groupid, p_year, p_month, p_hits, p_sessions );
$$;


CREATE OR REPLACE FUNCTION SobekCM_Statistics_Save_Portal(
	p_year smallint,
	p_month smallint,
	p_hits integer,
	p_portalid integer
)
RETURNS void
LANGUAGE sql
AS $$
	insert into SobekCM_Portal_URL_Statistics ( PortalID, "Year", "Month", Hits )
	values ( p_portalid, p_year, p_month, p_hits );
$$;


CREATE OR REPLACE FUNCTION SobekCM_Statistics_Save_TopLevel(
	p_year smallint,
	p_month smallint,
	p_hits integer,
	p_sessions integer,
	p_robot_hits integer,
	p_xml_hits integer,
	p_oai_hits integer,
	p_json_hits integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	delete from SobekCM_Statistics where "Year"=p_year and "Month"=p_month;

	insert into SobekCM_Statistics ( "Year", "Month", Hits, "Sessions", Robot_Hits, XML_Hits, OAI_Hits, JSON_Hits )
	values ( p_year, p_Month, p_hits, p_sessions, p_robot_hits, p_xml_hits, p_oai_hits, p_json_hits);
END;
$$;

CREATE OR REPLACE FUNCTION SobekCM_Statistics_Save_Webcontent(
	p_year smallint,
	p_month smallint,
	p_hits integer,
	p_hits_complete integer,
	p_level1 varchar(100), p_level2 varchar(100), p_level3 varchar(100), p_level4 varchar(100),
	p_level5 varchar(100), p_level6 varchar(100), p_level7 varchar(100), p_level8 varchar(100)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_webcontentid integer;
BEGIN
	v_webcontentid := coalesce((    select WebContentID
									  from SobekCM_WebContent W
									  where coalesce(W.Level1,'') = coalesce(p_level1, '' )
										 and coalesce(W.Level2,'') = coalesce(p_level2, '' )
										 and coalesce(W.Level3,'') = coalesce(p_level3, '' )
										 and coalesce(W.Level4,'') = coalesce(p_level4, '' )
										 and coalesce(W.Level5,'') = coalesce(p_level5, '' )
										 and coalesce(W.Level6,'') = coalesce(p_level6, '' )
										 and coalesce(W.Level7,'') = coalesce(p_level7, '' )
										 and coalesce(W.Level8,'') = coalesce(p_level8, '' )), -1 );

	if ( v_webcontentid > 0 ) then
		insert into SobekCM_Webcontent_Statistics ( WebContentID, Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8, "Year", "Month", Hits, Hits_Complete )
		values ( v_webcontentid, p_level1, p_level2, p_level3, p_level4, p_level5, p_level6, p_level7, p_level8, p_year, p_month, p_hits, p_hits_complete );
	end if;
END;
$$;


-- Get the list of items linked to this user, along with usage for that month
CREATE OR REPLACE FUNCTION SobekCM_Stats_Get_User_Linked_Items_Stats(
	p_userid integer,
	p_month integer,
	p_year integer
)
RETURNS TABLE (
	ItemID integer,
	RelationshipID integer,
	RelationshipLabel varchar(100),
	Title varchar(500),
	BibID varchar(10),
	VID varchar(5),
	CreateDate timestamp,
	Total_Hits bigint,
	Total_Sessions bigint,
	Month_Hits bigint,
	Month_Sessions bigint
)
LANGUAGE sql
AS $$
	select L.ItemID, L.RelationshipID, R.RelationshipLabel, I.Title, G.BibID, I.VID, I.CreateDate, I.Total_Hits, I.Total_Sessions, coalesce(S2.Hits,0) as Month_Hits, coalesce(S2."Sessions",0) as Month_Sessions
	from mySobek_User_Item_Link_Relationship AS R join
		 mySobek_User_Item_Link AS L ON ( L.RelationshipID=R.RelationshipID ) join
		 SobekCM_Item AS I ON ( L.ItemID=I.ItemID ) join
		 SobekCM_Item_Group AS G ON ( G.GroupID=I.GroupID) left join
		 SobekCM_Item_Statistics AS S2 ON ( S2.ItemID=L.ItemID and S2."Month"=p_month and S2."Year"=p_year)
	where ( L.UserID=p_userid ) and ( R.Include_In_Results = 'true' );
$$;


-- Get the list of all users that have items which may have statistics
CREATE OR REPLACE FUNCTION SobekCM_Stats_Get_Users_Linked_To_Items()
RETURNS TABLE (
	FirstName varchar(50),
	LastName varchar(50),
	NickName varchar(50),
	UserName varchar(50),
	UserID integer,
	EmailAddress varchar(100)
)
LANGUAGE sql
AS $$
	select U.FirstName, U.LastName, U.NickName, U.UserName, U.UserID, U.EmailAddress
	from mySobek_User U
	where ( Receive_Stats_Emails = 'true' )
	   and exists ( select * from mySobek_User_Item_Link L, mySobek_User_Item_Link_Relationship R where L.UserID=U.UserID and L.RelationshipID=R.RelationshipID and R.Include_In_Results = 'true' );
$$;


-- Updates the 'additional work needed' flag for an item, which tells
-- the builder that it should be post-processed.
CREATE OR REPLACE FUNCTION SobekCM_Update_Additional_Work_Needed_Flag(
	p_itemid integer,
	p_newflag boolean
)
RETURNS void
LANGUAGE sql
AS $$
	update SobekCM_Item set AdditionalWorkNeeded=p_newflag where ItemID=p_itemid;
$$;


-- Procedure to change some basic information about an item group
CREATE OR REPLACE FUNCTION SobekCM_Update_Item_Group(
	p_BibID varchar(10),
	p_GroupTitle varchar(500),
	p_SortTitle varchar(500),
	p_GroupThumbnail varchar(500),
	p_PrimaryIdentifierType varchar(50),
	p_PrimaryIdentifier varchar(100)
)
RETURNS void
LANGUAGE sql
AS $$
	update SobekCM_Item_Group
	set GroupTitle = p_GroupTitle, SortTitle = p_SortTitle, GroupThumbnail=p_GroupThumbnail,
	    Primary_Identifier_Type=p_PrimaryIdentifierType, Primary_Identifier=p_PrimaryIdentifier
	where BibID = p_BibID;
$$;


-- Add a new web content page
CREATE OR REPLACE FUNCTION SobekCM_WebContent_Add(
	p_Level1 varchar(100), p_Level2 varchar(100), p_Level3 varchar(100), p_Level4 varchar(100),
	p_Level5 varchar(100), p_Level6 varchar(100), p_Level7 varchar(100), p_Level8 varchar(100),
	p_UserName varchar(100),
	p_Title varchar(255),
	p_Summary varchar(1000),
	p_Redirect varchar(500),
	OUT p_WebContentID integer
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ( EXISTS ( select 1 from SobekCM_WebContent
	              where ( Level1=p_Level1 )
	                and ((Level2 is null and p_Level2 is null ) or ( Level2=p_Level2))
					and ((Level3 is null and p_Level3 is null ) or ( Level3=p_Level3))
					and ((Level4 is null and p_Level4 is null ) or ( Level4=p_Level4))
					and ((Level5 is null and p_Level5 is null ) or ( Level5=p_Level5))
					and ((Level6 is null and p_Level6 is null ) or ( Level6=p_Level6))
					and ((Level7 is null and p_Level7 is null ) or ( Level7=p_Level7))
					and ((Level8 is null and p_Level8 is null ) or ( Level8=p_Level8)))) then
		select WebContentID into p_WebContentID
		from SobekCM_WebContent
		where ( Level1=p_Level1 )
		  and ((Level2 is null and p_Level2 is null ) or ( Level2=p_Level2))
		  and ((Level3 is null and p_Level3 is null ) or ( Level3=p_Level3))
		  and ((Level4 is null and p_Level4 is null ) or ( Level4=p_Level4))
		  and ((Level5 is null and p_Level5 is null ) or ( Level5=p_Level5))
		  and ((Level6 is null and p_Level6 is null ) or ( Level6=p_Level6))
		  and ((Level7 is null and p_Level7 is null ) or ( Level7=p_Level7))
		  and ((Level8 is null and p_Level8 is null ) or ( Level8=p_Level8))
		limit 1;

		update SobekCM_WebContent set Title=p_Title, Summary=p_Summary, Redirect=p_Redirect where WebContentID=p_WebContentID;

		if ( EXISTS ( select 1 from SobekCM_WebContent where Deleted='true' and WebContentID=p_WebContentID )) then
			update SobekCM_WebContent
			set Deleted='false'
			where WebContentID = p_WebContentID;

			insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneDate, MilestoneUser )
			values ( p_WebContentID, 'Restored previously deleted page', now(), p_UserName );
		end if;
	else
		insert into SobekCM_WebContent ( Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8, Title, Summary, Deleted, Redirect )
		values ( p_Level1, p_Level2, p_Level3, p_Level4, p_Level5, p_Level6, p_Level7, p_Level8, p_Title, p_Summary, 'false', p_Redirect )
		returning WebContentID into p_WebContentID;

		insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneDate, MilestoneUser )
		values ( p_WebContentID, 'Add new page', now(), p_UserName );
	end if;
END;
$$;


-- Add a new milestone to an existing web content page
CREATE OR REPLACE FUNCTION SobekCM_WebContent_Add_Milestone(
	p_WebContentID integer,
	p_Milestone text,
	p_MilestoneUser varchar(100)
)
RETURNS void
LANGUAGE sql
AS $$
	insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneUser, MilestoneDate )
	values ( p_WebContentID, p_Milestone, p_MilestoneUser, now());
$$;


-- Return all the web content pages, regardless of whether they are redirects or an actual content page.
-- Originally returned 3 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_WebContent_All(
	OUT cur_pages refcursor,
	OUT cur_level1 refcursor,
	OUT cur_level1_2 refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_pages FOR
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

	OPEN cur_level1 FOR
	select distinct(W.Level1)
	from SobekCM_WebContent W
	where ( Deleted = 'false' )
	order by W.Level1;

	OPEN cur_level1_2 FOR
	select W.Level1, W.Level2
	from SobekCM_WebContent W
	where ( W.Level2 is not null )
	  and ( Deleted = 'false' )
	group by W.Level1, W.Level2
	order by W.Level1, W.Level2;
END;
$$;


-- Return a brief account of all the web content pages, regardless of whether they are redirects or an actual content page
CREATE OR REPLACE FUNCTION SobekCM_WebContent_All_Brief()
RETURNS TABLE (
	WebContentID integer,
	Level1 varchar(100), Level2 varchar(100), Level3 varchar(100), Level4 varchar(100),
	Level5 varchar(100), Level6 varchar(100), Level7 varchar(100), Level8 varchar(100),
	Redirect varchar(500)
)
LANGUAGE sql
AS $$
	select W.WebContentID, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Redirect
	from SobekCM_WebContent W
	where Deleted = 'false'
	order by W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8;
$$;


-- Return all the web content pages that are not set as redirects.
-- Originally returned 3 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_WebContent_All_Pages(
	OUT cur_pages refcursor,
	OUT cur_level1 refcursor,
	OUT cur_level1_2 refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_pages FOR
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
	where ( length(coalesce(W.Redirect,'')) = 0 ) and ( Deleted = 'false' )
	order by W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8;

	OPEN cur_level1 FOR
	select distinct(W.Level1)
	from SobekCM_WebContent W
	where ( length(coalesce(W.Redirect,'')) = 0 ) and ( Deleted = 'false' )
	order by W.Level1;

	OPEN cur_level1_2 FOR
	select W.Level1, W.Level2
	from SobekCM_WebContent W
	where ( length(coalesce(W.Redirect,'')) = 0 )
	  and ( W.Level2 is not null )
	  and ( Deleted = 'false' )
	group by W.Level1, W.Level2
	order by W.Level1, W.Level2;
END;
$$;

-- Originally returned 3 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_WebContent_All_Redirects(
	OUT cur_pages refcursor,
	OUT cur_level1 refcursor,
	OUT cur_level1_2 refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_pages FOR
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
	where ( length(coalesce(W.Redirect,'')) > 0 ) and ( Deleted = 'false' )
	order by W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8;

	OPEN cur_level1 FOR
	select distinct(W.Level1)
	from SobekCM_WebContent W
	where ( length(coalesce(W.Redirect,'')) > 0 ) and ( Deleted = 'false' )
	order by W.Level1;

	OPEN cur_level1_2 FOR
	select W.Level1, W.Level2
	from SobekCM_WebContent W
	where ( length(coalesce(W.Redirect,'')) > 0 )
	  and ( W.Level2 is not null )
	  and ( Deleted = 'false' )
	group by W.Level1, W.Level2
	order by W.Level1, W.Level2;
END;
$$;


-- Delete an existing web content page (and mark in the milestones)
CREATE OR REPLACE FUNCTION SobekCM_WebContent_Delete(
	p_WebContentID integer,
	p_Reason text,
	p_MilestoneUser varchar(100)
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	update SobekCM_WebContent
	set Deleted='true'
	where WebContentID=p_WebContentID;

	if (( p_Reason is not null ) and ( length(p_Reason) > 0 )) then
		insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneUser, MilestoneDate )
		values ( p_WebContentID, 'Page Deleted - ' || p_Reason, p_MilestoneUser, now());
	else
		insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneUser, MilestoneDate )
		values ( p_WebContentID, 'Page Deleted', p_MilestoneUser, now());
	end if;
END;
$$;


-- Edit basic information on an existing web content page
CREATE OR REPLACE FUNCTION SobekCM_WebContent_Edit(
	p_WebContentID integer,
	p_UserName varchar(100),
	p_Title varchar(255),
	p_Summary varchar(1000),
	p_Redirect varchar(500),
	p_MilestoneText text
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	update SobekCM_WebContent
	set Title=p_Title, Summary=p_Summary, Redirect=p_Redirect
	where WebContentID=p_WebContentID;

	if ( length(coalesce(p_MilestoneText,'')) > 0 ) then
		insert into SobekCM_WebContent_Milestones (WebContentID, Milestone, MilestoneDate, MilestoneUser )
		values ( p_WebContentID, p_MilestoneText, now(), p_UserName );
	else
		insert into SobekCM_WebContent_Milestones (WebContentID, Milestone, MilestoneDate, MilestoneUser )
		values ( p_WebContentID, 'Edited', now(), p_UserName );
	end if;
END;
$$;


-- Get the milestones for a webcontent page (by ID)
CREATE OR REPLACE FUNCTION SobekCM_WebContent_Get_Milestones(
	p_WebContentID integer
)
RETURNS TABLE (
	Milestone text,
	MilestoneDate timestamp,
	MilestoneUser varchar(100)
)
LANGUAGE sql
AS $$
	select Milestone, MilestoneDate, MilestoneUser
	from SobekCM_WebContent_Milestones
	where WebContentID=p_WebContentID
	order by MilestoneDate;
$$;


-- Get basic details about an existing web content page
CREATE OR REPLACE FUNCTION SobekCM_WebContent_Get_Page(
	p_Level1 varchar(100), p_Level2 varchar(100), p_Level3 varchar(100), p_Level4 varchar(100),
	p_Level5 varchar(100), p_Level6 varchar(100), p_Level7 varchar(100), p_Level8 varchar(100)
)
RETURNS TABLE (
	WebContentID integer,
	Title varchar(255),
	Summary varchar(1000),
	Deleted boolean,
	MilestoneDate timestamp,
	MilestoneUser varchar(100),
	Redirect varchar(500),
	Level1 varchar(100), Level2 varchar(100), Level3 varchar(100), Level4 varchar(100),
	Level5 varchar(100), Level6 varchar(100), Level7 varchar(100), Level8 varchar(100),
	Locked boolean
)
LANGUAGE sql
AS $$
	select W.WebContentID, W.Title, W.Summary, W.Deleted, M.MilestoneDate, M.MilestoneUser, W.Redirect, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Locked
	from SobekCM_WebContent W left outer join
	     SobekCM_WebContent_Milestones M on W.WebContentID=M.WebContentID
	where ( Level1=p_Level1 )
	  and ((Level2 is null and p_Level2 is null ) or ( Level2=p_Level2))
	  and ((Level3 is null and p_Level3 is null ) or ( Level3=p_Level3))
	  and ((Level4 is null and p_Level4 is null ) or ( Level4=p_Level4))
	  and ((Level5 is null and p_Level5 is null ) or ( Level5=p_Level5))
	  and ((Level6 is null and p_Level6 is null ) or ( Level6=p_Level6))
	  and ((Level7 is null and p_Level7 is null ) or ( Level7=p_Level7))
	  and ((Level8 is null and p_Level8 is null ) or ( Level8=p_Level8))
	order by M.MilestoneDate DESC
	limit 1;
$$;


-- Get basic details about an existing web content page
CREATE OR REPLACE FUNCTION SobekCM_WebContent_Get_Page_ID(
	p_WebContentID integer
)
RETURNS TABLE (
	WebContentID integer,
	Title varchar(255),
	Summary varchar(1000),
	Deleted boolean,
	MilestoneDate timestamp,
	MilestoneUser varchar(100),
	Redirect varchar(500),
	Level1 varchar(100), Level2 varchar(100), Level3 varchar(100), Level4 varchar(100),
	Level5 varchar(100), Level6 varchar(100), Level7 varchar(100), Level8 varchar(100),
	Locked boolean
)
LANGUAGE sql
AS $$
	select W.WebContentID, W.Title, W.Summary, W.Deleted, M.MilestoneDate, M.MilestoneUser, W.Redirect, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Locked
	from SobekCM_WebContent W left outer join
	     SobekCM_WebContent_Milestones M on W.WebContentID=M.WebContentID
	where W.WebContentID = p_WebContentID
	order by M.MilestoneDate DESC
	limit 1;
$$;


-- Get the list of recent changes to all web content pages.
-- Originally returned 4 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_WebContent_Get_Recent_Changes(
	OUT cur_changes refcursor,
	OUT cur_users refcursor,
	OUT cur_level1 refcursor,
	OUT cur_level1_2 refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_changes FOR
	select W.WebContentID, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, MilestoneDate, MilestoneUser, Milestone, W.Title
	from SobekCM_WebContent_Milestones M, SobekCM_WebContent W
	where M.WebContentID=W.WebContentID
	order by MilestoneDate DESC;

	OPEN cur_users FOR
	select MilestoneUser
	from SobekCM_WebContent_Milestones
	group by MilestoneUser
	order by MilestoneUser;

	OPEN cur_level1 FOR
	select Level1
	from SobekCM_WebContent_Milestones M, SobekCM_WebContent W
	where M.WebContentID=W.WebContentID
	group by Level1
	order by Level1;

	OPEN cur_level1_2 FOR
	select Level1, Level2
	from SobekCM_WebContent_Milestones M, SobekCM_WebContent W
	where M.WebContentID=W.WebContentID
	group by Level1, Level2
	order by Level1, Level2;
END;
$$;


-- Get the usage stats for a webcontent page (by ID)
CREATE OR REPLACE FUNCTION SobekCM_WebContent_Get_Usage(
	p_WebContentID integer
)
RETURNS TABLE (
	"Year" integer,
	"Month" integer,
	Hits integer,
	Hits_Complete integer
)
LANGUAGE sql
AS $$
	select "Year", "Month", Hits, Hits_Complete
	from SobekCM_WebContent_Statistics
	where WebContentID=p_WebContentID
	order by "Year", "Month";
$$;


-- Pull the flag indicating if this instance has any web content usage logged
CREATE OR REPLACE FUNCTION SobekCM_WebContent_Has_Usage(
	OUT p_value boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
	if ( exists ( select 1 from SobekCM_WebContent_Statistics )) then
		p_value := true;
	else
		p_value := false;
	end if;
END;
$$;


-- Pull the usage for all top-level web content pages between two dates.
-- Originally returned 3 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION SobekCM_WebContent_Usage_Report(
	p_year1 smallint,
	p_month1 smallint,
	p_year2 smallint,
	p_month2 smallint,
	OUT cur_stats refcursor,
	OUT cur_level1 refcursor,
	OUT cur_level1_2 refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	CREATE TEMP TABLE temp_webcontent_usage ON COMMIT DROP AS
	with stats_compiled as
	(
		select Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8, sum(Hits) as Hits, sum(Hits_Complete) as HitsHierarchical
		from SobekCM_WebContent_Statistics
		where ((( "Month" >= p_month1 ) and ( "Year" = p_year1 )) or ("Year" > p_year1 ))
		  and ((( "Month" <= p_month2 ) and ( "Year" = p_year2 )) or ("Year" < p_year2 ))
		group by Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8
	)
	select coalesce(W.Level1, S.Level1) as Level1, coalesce(W.Level2, S.Level2) as Level2, coalesce(W.Level3, S.Level3) as Level3,
	       coalesce(W.Level4, S.Level4) as Level4, coalesce(W.Level5, S.Level5) as Level5, coalesce(W.Level6, S.Level6) as Level6,
		   coalesce(W.Level7, S.Level7) as Level7, coalesce(W.Level8, S.Level8) as Level8, W.Deleted, coalesce(W.Title,'(no title)') as Title, S.Hits, S.HitsHierarchical
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

	OPEN cur_stats FOR select * from temp_webcontent_usage;

	OPEN cur_level1 FOR
	select Level1
	from temp_webcontent_usage
	group by Level1
	order by Level1;

	OPEN cur_level1_2 FOR
	select Level1, Level2
	from temp_webcontent_usage
	group by Level1, Level2
	order by Level1, Level2;
END;
$$;

CREATE OR REPLACE FUNCTION Tracking_Add_New_Workflow(
	p_itemid integer,
	p_user varchar(50),
	p_dateStarted timestamp,
	p_dateCompleted timestamp,
	p_relatedEquipment varchar(1000),
	p_EventNumber integer,
	p_StartEventNumber integer,
	p_EndEventNumber integer,
	p_Start_End_Event integer,
	OUT p_workflow_entry_id integer
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_workflowid integer;
BEGIN
	v_workflowid := coalesce((select WorkFlowID from Tracking_Workflow where Start_Event_Number = p_EventNumber or End_Event_Number = p_EventNumber ), -1);

	insert into Tracking_Progress ( ItemID, WorkFlowID, DateStarted, DateCompleted, WorkPerformedBy, RelatedEquipment, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number)
	values ( p_itemid, v_workflowid, p_dateStarted, p_dateCompleted, p_user, p_relatedEquipment, p_StartEventNumber, p_EndEventNumber, p_Start_End_Event )
	returning ProgressID into p_workflow_entry_id;
END;
$$;


CREATE OR REPLACE FUNCTION Tracking_Add_Workflow_By_ItemID(
	p_itemid integer,
	p_user varchar(50),
	p_progressnote varchar(1000),
	p_workflow varchar(100),
	p_storagelocation varchar(255)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_workflowid integer;
BEGIN
	if ( coalesce( p_itemid, -1 ) > 0 ) then
		if ( ( select COUNT(*) from Tracking_WorkFlow where ( WorkFlowName=p_workflow)) > 0 ) then
			select workflowid into v_workflowid from Tracking_WorkFlow where WorkFlowName=p_workflow;
		else
			insert into Tracking_WorkFlow ( WorkFlowName, WorkFlowNotes )
			values ( p_workflow, 'Added ' || to_char(now(), 'MM/DD/YYYY') || ' by ' || p_user )
			returning WorkFlowID into v_workflowid;
		end if;

		insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath )
		values ( p_itemid, v_workflowid, now(), p_user, p_progressnote, p_storagelocation );
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION Tracking_Add_Workflow_Once_Per_Day(
	p_bibid varchar(10),
	p_vid varchar(5),
	p_user varchar(50),
	p_progressnote varchar(1000),
	p_workflowid integer,
	p_storagelocation varchar(255)
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_itemid integer;
BEGIN
	select ItemID into v_itemid
	from SobekCM_Item_Group G, SobekCM_Item I
	where ( BibID = p_bibid )
	    and ( I.GroupID = G.GroupID )
	    and ( VID = p_vid);

	if ( coalesce( v_itemid, -1 ) > 0 ) then
		if ( (select count(*) from Tracking_Progress where ( ItemID = v_itemid ) and ( WorkFlowID = p_workflowid ) and ( DateCompleted is null )) > 0 ) then
			update Tracking_Progress
			set DateCompleted = now(), WorkPerformedBy = p_user, WorkingFilePath=p_storagelocation, ProgressNote = p_progressnote
			where ( ItemID = v_itemid ) and ( WorkFlowID = p_workflowid ) and ( DateCompleted is null );
		else
			-- only enter one of these per day
			if ( (select count(*) from Tracking_Progress where ( ItemID = v_itemid ) and ( WorkFlowID=p_workflowid ) and ( DateCompleted::date = now()::date )) = 0 ) then
				insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath )
				values ( v_itemid, p_workflowid, now(), p_user, p_progressnote, p_storagelocation );
			end if;
		end if;
	end if;
END;
$$;


-- Stored procedure to delete a workflow entry
CREATE OR REPLACE FUNCTION Tracking_Delete_Workflow(
	p_workflow_entry_id integer
)
RETURNS void
LANGUAGE sql
AS $$
	delete from Tracking_Progress
	where ProgressID=p_workflow_entry_id;
$$;


-- Marks this volume image processing complete
-- Written by Mark Sullivan
CREATE OR REPLACE FUNCTION Tracking_Digital_Acquisition_Complete(
	p_bibid varchar(10),
	p_vid varchar(5),
	p_user varchar(255),
	p_storagelocation varchar(255),
	p_date timestamp
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_itemid integer;
BEGIN
	select ItemID into v_itemid
	from SobekCM_Item_Group G, SobekCM_Item I
	where ( BibID = p_bibid )
	    and ( I.GroupID = G.GroupID )
	    and ( VID = p_vid);

	if ( coalesce( v_itemid, -1 ) > 0 ) then
		update SobekCM_Item
		set Milestone_DigitalAcquisition = coalesce(Milestone_DigitalAcquisition, p_date)
		where ItemID=v_itemid;
	end if;
END;
$$;


--Stored procedure for getting all the tracking workflow entries by user
--entered through the tracking sheet
CREATE OR REPLACE FUNCTION Tracking_Get_All_Entries_By_User(
	p_username varchar(50)
)
RETURNS TABLE (
	ItemID integer,
	ProgressID integer,
	WorkFlowName varchar(100),
	Start_Event_Desc varchar(255),
	End_Event_Desc varchar(255),
	Start_Event_Number integer,
	End_Event_Number integer,
	Start_And_End_Event_Number integer,
	DateStarted timestamp,
	DateCompleted timestamp,
	RelatedEquipment varchar(1000),
	WorkPerformedBy varchar(100),
	WorkingFilePath varchar(500),
	ProgressNote varchar(2000)
)
LANGUAGE sql
AS $$
	select P.ItemID,P.ProgressID, W.WorkFlowName, W.Start_Event_Desc, W.End_Event_Desc, W.Start_Event_Number, W.End_Event_Number, W.Start_And_End_Event_Number,
	       P.DateStarted, P.DateCompleted, P.RelatedEquipment, P.WorkPerformedBy, P.WorkingFilePath, P.ProgressNote
	from Tracking_Progress P, Tracking_Workflow W
	where P.WorkFlowID = W.WorkFlowID
	and P.WorkPerformedBy = p_username;
$$;


-- version 5 - Removed first table (CDs) and third table (TIVOLI).
-- Originally returned 3 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION Tracking_Get_History_Archives(
	p_itemid integer,
	OUT cur_progress refcursor,
	OUT cur_item refcursor,
	OUT cur_aggregations refcursor
)
LANGUAGE plpgsql
AS $$
BEGIN
	OPEN cur_progress FOR
	select P.WorkFlowID, WorkFlowName as "Workflow Name", coalesce(to_char(DateCompleted, 'YYYY.MM.DD'),'') as "Completed Date", coalesce(WorkPerformedBy, '') as WorkPerformedBy, coalesce(WorkingFilePath,'') as WorkingFilePath, coalesce(ProgressNote,'') as Note
	from Tracking_Progress P, Tracking_Workflow W
	where (P.workflowid = W.workflowid)
	  and (P.ItemID = p_itemid )
	order by DateCompleted ASC;

	OPEN cur_item FOR select * from SobekCM_Item where ItemID=p_itemid;

	OPEN cur_aggregations FOR
	select A.Code, A.Name, A.ShortName, A.Type, L.impliedLink, A.Hidden, A.isActive
	from SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A
	where ( L.ItemID = p_ItemID )
	  and ( A.AggregationID = L.AggregationID );
END;
$$;


CREATE OR REPLACE FUNCTION Tracking_Get_Item_Info_from_ItemID(
	p_itemID integer
)
RETURNS TABLE (
	VID varchar(5),
	BibID varchar(10),
	Title varchar(500)
)
LANGUAGE sql
AS $$
	SELECT I.VID, G.BibID, I.Title
	FROM SobekCM_Item I, SobekCM_Item_Group G
	WHERE I.GroupID = G.GroupID
	   AND I.ItemID =p_itemID;
$$;


-- Stored procedure returns the information about all the items within a single
-- title or item/group
-- Written by Mark Sullivan ( November 2006 )
CREATE OR REPLACE FUNCTION Tracking_Get_Multiple_Volumes(
	p_bibid varchar(10)
)
RETURNS TABLE (
	ItemID integer,
	Title varchar(500),
	Level1_Text varchar(255), Level1_Index integer,
	Level2_Text varchar(255), Level2_Index integer,
	Level3_Text varchar(255), Level3_Index integer,
	Level4_Text varchar(255), Level4_Index integer,
	Level5_Text varchar(255), Level5_Index integer,
	MainThumbnail varchar(100),
	VID varchar(5),
	IP_Restriction_Mask smallint,
	Author varchar(1000),
	Publisher varchar(1000),
	AggregationCodes varchar(100),
	Tracking_Box varchar(25),
	Born_Digital boolean,
	Material_Received_Date timestamp,
	Material_Recd_Date_Estimated boolean,
	Disposition_Advice integer,
	Disposition_Advice_Notes varchar(150),
	Disposition_Type integer,
	Disposition_Date timestamp,
	Disposition_Notes varchar(150),
	PubDate varchar(100),
	SortDate bigint,
	SortTitle varchar(500),
	Last_MileStone integer,
	Remotely_Archived boolean,
	Locally_Archived boolean
)
LANGUAGE sql
AS $$
	select I.ItemID, Title, coalesce(Level1_Text,'') as Level1_Text, coalesce(Level1_Index,-1) as Level1_Index, coalesce(Level2_Text, '') as Level2_Text, coalesce(Level2_Index, -1) as Level2_Index, coalesce(Level3_Text, '') as Level3_Text, coalesce(Level3_Index, -1) as Level3_Index, coalesce(Level4_Text, '') as Level4_Text, coalesce(Level4_Index, -1) as Level4_Index, coalesce(Level5_Text, '') as Level5_Text, coalesce(Level5_Index,-1) as Level5_Index, I.MainThumbnail, I.VID, I.IP_Restriction_Mask, I.Author, I.Publisher, I.AggregationCodes, I.Tracking_Box, I.Born_Digital, I.Material_Received_Date, I.Material_Recd_Date_Estimated, I.Disposition_Advice, I.Disposition_Advice_Notes, I.Disposition_Type, I.Disposition_Date, I.Disposition_Notes, PubDate, SortDate, I.SortTitle, I.Last_MileStone, I.Remotely_Archived, I.Locally_Archived
	from SobekCM_Item I, SobekCM_Item_Group G
	where ( G.GroupID = I.GroupID )
	  and ( G.BibID = p_bibid )
	  and ( I.Deleted = 'false' )
	  and ( G.Deleted = 'false' )
	order by Level1_Index ASC, Level2_Index ASC, Level3_Index ASC, Level4_Index ASC, Level5_Index ASC, I.SortTitle ASC;
$$;


CREATE OR REPLACE FUNCTION Tracking_Get_Scanners_List()
RETURNS TABLE (
	ScanningEquipment varchar(100),
	Notes varchar(500),
	Location varchar(100),
	EquipmentType varchar(50)
)
LANGUAGE sql
AS $$
	SELECT ScanningEquipment, Notes, Location,EquipmentType
	FROM Tracking_ScanningEquipment
	WHERE isActive=true;
$$;


CREATE OR REPLACE FUNCTION Tracking_Get_Users_Scanning_Processing()
RETURNS TABLE (
	UserName varchar(50),
	EmailAddress varchar(100),
	FirstName varchar(50),
	LastName varchar(50),
	ScanningTechnician boolean,
	ProcessingTechnician boolean
)
LANGUAGE sql
AS $$
	SELECT UserName,EmailAddress,FirstName,LastName,ScanningTechnician, ProcessingTechnician
	FROM mySobek_User
	WHERE ScanningTechnician=true OR ProcessingTechnician=true;
$$;

-- Get the tracking work history against this item and the milestones.
-- Originally returned 2 result sets; ported using OUT refcursor parameters.
CREATE OR REPLACE FUNCTION Tracking_Get_Work_History(
	p_bibid varchar(10),
	p_vid varchar(5),
	OUT cur_progress refcursor,
	OUT cur_milestones refcursor
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_itemid integer;
	v_groupid integer;
BEGIN
	v_itemid := coalesce( ( select I.ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID=G.GroupID and I.VID=p_vid and G.BibiD=p_bibid ), -1 );
	v_groupid := coalesce( ( select G.GroupID from SobekCM_Item_Group G where G.BibiD=p_bibid ), -1 );

	OPEN cur_progress FOR
	select P.WorkFlowID, WorkFlowName as "Workflow Name", coalesce(to_char(DateCompleted, 'YYYY.MM.DD'),'') as "Completed Date", coalesce(WorkPerformedBy, '') as WorkPerformedBy, coalesce(ProgressNote,'') as Note, coalesce(WorkPerformedById, -1) as WorkPerformedById
	from Tracking_Progress P, Tracking_Workflow W
	where (P.workflowid = W.workflowid)
	  and (P.ItemID = v_itemid )
	order by DateCompleted ASC;

	OPEN cur_milestones FOR
	select CreateDate, Milestone_DigitalAcquisition, Milestone_ImageProcessing, Milestone_QualityControl, Milestone_OnlineComplete, Material_Received_Date, Disposition_Date from SobekCM_Item where ItemID=v_itemid;
END;
$$;


-- Marks this volume image processing complete
-- Written by Mark Sullivan
CREATE OR REPLACE FUNCTION Tracking_Image_Processing_Complete(
	p_bibid varchar(10),
	p_vid varchar(5),
	p_user varchar(255),
	p_storagelocation varchar(255),
	p_date timestamp
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_itemid integer;
BEGIN
	select ItemID into v_itemid
	from SobekCM_Item_Group G, SobekCM_Item I
	where ( BibID = p_bibid )
	    and ( I.GroupID = G.GroupID )
	    and ( VID = p_vid);

	if ( coalesce( v_itemid, -1 ) > 0 ) then
		update SobekCM_Item
		set Milestone_DigitalAcquisition = coalesce(Milestone_DigitalAcquisition, p_date),
		    Milestone_ImageProcessing = coalesce(Milestone_ImageProcessing, p_date)
		where ItemID=v_itemid;
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION Tracking_Item_Milestone_Report(
	p_aggregation_code varchar(20)
)
RETURNS TABLE (
	MileStone text,
	title_count bigint,
	item_count bigint,
	page_count bigint,
	file_count bigint,
	Last_MileStone integer
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_aggregationid integer;
BEGIN
	if ( length( coalesce( p_aggregation_code,'')) = 0 ) then
		RETURN QUERY
		select CASE Last_MileStone
		                  WHEN 0 THEN 'NO WORK COMPLETED'
		                  WHEN 1 THEN 'SCANNED'
		                  WHEN 2 THEN 'PROCESSED'
		                  WHEN 3 THEN 'QC PERFORMED'
		                  WHEN 4 THEN 'ONLINE COMPLETE'
		                  ELSE 'DATABASE ERROR'
		            END AS MileStone, count(distinct(GroupID)) as title_count, count(*) as item_count, SUM(PageCount) as page_count, SUM(FileCount) as file_count, Last_MileStone
		from SobekCM_Item
		group by Last_MileStone
		union
		select 'TOTAL', count(distinct(GroupID)), count(*), SUM(PageCount), SUM(FileCount), -1
		from SobekCM_Item
		order by Last_MileStone DESC;
	else
		v_aggregationid := (select AggregationID from SobekCM_Item_Aggregation where Code=p_aggregation_code limit 1);

		if ( coalesce(v_aggregationid,-1) > 0 ) then
			RETURN QUERY
			select CASE Last_MileStone
		                  WHEN 0 THEN 'NO WORK COMPLETED'
		                  WHEN 1 THEN 'SCANNED'
		                  WHEN 2 THEN 'PROCESSED'
		                  WHEN 3 THEN 'QC PERFORMED'
		                  WHEN 4 THEN 'ONLINE COMPLETE'
		                  ELSE 'DATABASE ERROR'
		            END AS MileStone, count(distinct(GroupID)) as title_count, count(*) as item_count, SUM(PageCount) as page_count, SUM(FileCount) as file_count, Last_MileStone
			  from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L
			  where ( I.ItemID = L.ItemID ) and ( L.AggregationID = v_aggregationid )
			  group by Last_MileStone
			  union
			  select 'TOTAL', count(distinct(GroupID)), count(*), SUM(PageCount), SUM(FileCount), -1
			  from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L
			  where ( I.ItemID = L.ItemID ) and ( L.AggregationID = v_aggregationid )
			  order by Last_MileStone DESC;
		end if;
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION Tracking_Item_Visibility_Report(
	p_aggregation_code varchar(20)
)
RETURNS TABLE (
	Restriction text,
	title_count bigint,
	item_count bigint,
	page_count bigint,
	file_count bigint,
	OrderBy integer
)
LANGUAGE plpgsql
AS $$
DECLARE
	v_aggregationid integer;
BEGIN
	if ( length( coalesce( p_aggregation_code,'')) = 0 ) then
		RETURN QUERY
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
		select I.Restriction, count(distinct(GroupID)) as title_count, count(*) as item_count, SUM(PageCount) as page_count, SUM(FileCount) as file_count, I.OrderBy
		from items_cte I
		group by I.Restriction, I.OrderBy
		union
		select 'TOTAL', count(distinct(GroupID)), count(*), SUM(PageCount), SUM(FileCount), 5 as OrderBy
		from items_cte I
		order by OrderBy;
	else
		v_aggregationid := (select AggregationID from SobekCM_Item_Aggregation where Code=p_aggregation_code limit 1);

		if ( coalesce(v_aggregationid,-1) > 0 ) then
			RETURN QUERY
			with items_cte as
			(
				select GroupID, I.ItemID, PageCount, FileCount,
				  CASE IP_Restriction_Mask WHEN 0 THEN 'PUBLIC' WHEN -1 THEN 'PRIVATE' ELSE 'IP RESTRICTED' END as Restriction,
				  CASE IP_Restriction_Mask WHEN 0 THEN 0 WHEN -1 THEN 4 ELSE 3 END as OrderBy
				from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L
				where ( I.Deleted='false')
				  and ( L.ItemID=I.ItemID )
				  and ( L.AggregationID = v_aggregationid )
				  and ( not exists ( select 1 from mySobek_User_Group_Item_Permissions P where P.ItemID=I.ItemID and P.canView='true' ))
				UNION
				select GroupID, I.ItemID, PageCount, FileCount, 'USER GROUP RESTRICTED' as Restriction, 2 as OrderBy
				from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L
				where ( I.Deleted='false')
				  and ( L.ItemID=I.ItemID )
				  and ( L.AggregationID = v_aggregationid )
				  and ( exists ( select 1 from mySobek_User_Group_Item_Permissions P where P.ItemID=I.ItemID and P.canView='true' ))
			)
			select I.Restriction, count(distinct(GroupID)) as title_count, count(*) as item_count, SUM(PageCount) as page_count, SUM(FileCount) as file_count, I.OrderBy
			from items_cte I
			group by I.Restriction, I.OrderBy
			union
			select 'TOTAL', count(distinct(GroupID)), count(*), SUM(PageCount), SUM(FileCount), 5 as OrderBy
			from items_cte I
			order by OrderBy;
		end if;
	end if;
END;
$$;


-- Marks this volume as having processed a metadata update for it
-- This is called when an item successfully passes 'UFDC Loader'
-- Written by Mark Sullivan (April 2007)
CREATE OR REPLACE FUNCTION Tracking_Load_Metadata_Update_Complete(
	p_bibid varchar(10),
	p_vid varchar(5),
	p_user varchar(50),
	p_usernotes varchar(1000)
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	PERFORM Tracking_Add_Workflow_Once_Per_Day(p_bibid, p_vid, p_user, p_usernotes, 11, null);
END;
$$;


-- Marks this volume as having been edited online
CREATE OR REPLACE FUNCTION Tracking_Online_Edit_Complete(
	p_itemid integer,
	p_user varchar(50),
	p_usernotes varchar(1000)
)
RETURNS void
LANGUAGE sql
AS $$
	insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath )
	values ( p_itemid, 30, now(), p_user, p_usernotes, '' );
$$;


-- Marks this volume as having been submitted online
CREATE OR REPLACE FUNCTION Tracking_Online_Submit_Complete(
	p_itemid integer,
	p_user varchar(50),
	p_usernotes varchar(1000)
)
RETURNS void
LANGUAGE sql
AS $$
	insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath )
	values ( p_itemid, 29, now(), p_user, p_usernotes, '' );
$$;


-- Submit a log about QCing a volume
-- Written by Mark Sullivan ( July 2013 )
CREATE OR REPLACE FUNCTION Tracking_Submit_Online_Page_Division(
	p_itemid integer,
	p_notes varchar(255),
	p_onlineuser varchar(100),
	p_mainthumbnail varchar(100),
	p_mainjpeg varchar(100),
	p_pagecount integer,
	p_filecount integer,
	p_disksize_kb bigint
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if (( select count(*)
	      from Tracking_Progress T
	      where ( T.ItemID=p_itemid ) and ( ProgressNote=p_notes ) and ( WorkPerformedBy=p_onlineuser )
	        and ( now()::date = DateCompleted::date)
			and ( WorkFlowID=45 )) = 0 ) then
		insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, ProgressNote, WorkPerformedBy, WorkingFilePath )
		values ( p_itemid, 45, now(), p_notes, p_onlineuser, '' );
	end if;

	update SobekCM_Item
	set Milestone_DigitalAcquisition = coalesce(Milestone_DigitalAcquisition, now()),
	    Milestone_ImageProcessing = coalesce(Milestone_ImageProcessing, now()),
	    Milestone_QualityControl = coalesce(Milestone_QualityControl, now())
	where ItemID=p_itemid;

	update SobekCM_Item
	set Last_Milestone = 3
	where ItemID = p_itemid and Last_Milestone < 3;

	if ( ( select COUNT(*) from SobekCM_Item where ItemID=p_itemid and (( Dark = 'true' ) or ( IP_Restriction_Mask >= 0 ))) > 0 ) then
		update SobekCM_Item
		set Milestone_OnlineComplete = coalesce(Milestone_OnlineComplete, now()),
			Last_MileStone=4
		where ItemID=p_itemid;
	end if;

	update SobekCM_Item set PageCount=p_pagecount, MainThumbnail = p_mainthumbnail, MainJPEG = p_mainjpeg, FileCount = p_filecount, DiskSize_KB = p_disksize_kb
	where ItemID = p_itemid;
END;
$$;


CREATE OR REPLACE FUNCTION Tracking_Update_Digitization_Milestones(
	p_ItemID integer,
	p_Last_Milestone integer,
	p_Milestone_Date date
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	if ( p_Last_Milestone = 1 ) then
		update SobekCM_Item
		set Milestone_DigitalAcquisition = coalesce(Milestone_DigitalAcquisition, p_Milestone_Date),
		    Milestone_ImageProcessing = null,
		    Milestone_QualityControl = null,
		    Milestone_OnlineComplete = null,
		    Last_MileStone=1
		where ItemID=p_itemid;
	end if;

	if ( p_Last_Milestone = 2 ) then
		update SobekCM_Item
		set Milestone_DigitalAcquisition = coalesce(Milestone_DigitalAcquisition, p_Milestone_Date),
		    Milestone_ImageProcessing = coalesce(Milestone_ImageProcessing, p_Milestone_Date),
		    Milestone_QualityControl = null,
		    Milestone_OnlineComplete = null,
		    Last_MileStone=2
		where ItemID=p_itemid;
	end if;

	if ( p_Last_Milestone = 3 ) then
		update SobekCM_Item
		set Milestone_DigitalAcquisition = coalesce(Milestone_DigitalAcquisition, p_Milestone_Date),
		    Milestone_ImageProcessing = coalesce(Milestone_ImageProcessing, p_Milestone_Date),
		    Milestone_QualityControl = coalesce(Milestone_QualityControl, p_Milestone_Date),
		    Milestone_OnlineComplete = null,
		    Last_MileStone=3
		where ItemID=p_itemid;
	end if;

	if ( p_Last_Milestone = 4 ) then
		update SobekCM_Item
		set Milestone_DigitalAcquisition = coalesce(Milestone_DigitalAcquisition, p_Milestone_Date),
		    Milestone_ImageProcessing = coalesce(Milestone_ImageProcessing, p_Milestone_Date),
		    Milestone_QualityControl = coalesce(Milestone_QualityControl, p_Milestone_Date),
		    Milestone_OnlineComplete = coalesce(Milestone_OnlineComplete, p_Milestone_Date),
		    Last_MileStone=4
		where ItemID=p_itemid;
	end if;
END;
$$;


-- Procedure pulls the bibs and vids updated on SobekCM since the provided date.
CREATE OR REPLACE FUNCTION Tracking_Update_List(
	p_sinceDate varchar(10)
)
RETURNS TABLE (
	BibID varchar(10),
	VID varchar(5),
	DateCompleted timestamp,
	WorkFlowName varchar(100),
	WorkPerformedBy varchar(100)
)
LANGUAGE sql
AS $$
	select G.BibID, I.VID, P.DateCompleted, W.WorkFlowName, P.WorkPerformedBy
	from Tracking_WorkFlow W, Tracking_Progress P, SobekCM_Item_Group G, SobekCM_Item I
	where ( W.WorkFlowID = P.WorkFlowID )
	  and ( P.ItemID = I.ItemID )
	  and ( I.GroupID = G.GroupID )
	  and (( W.WorkFlowID = 29 ) or ( W.WorkFlowID = 30 ) or ( W.WorkFlowID = 34 ) or ( W.WorkFlowID = 35 ) or ( W.WorkFlowID = 36 ) or ( W.WorkFlowID=40 ) or (W.WorkFlowID=44))
	  and ( P.DateCompleted > p_sinceDate::timestamp )
	order by P.DateCompleted DESC;
$$;


CREATE OR REPLACE FUNCTION Tracking_Update_Material_Received(
	p_Material_Received_Date timestamp,
	p_Material_Recd_Date_Estimated boolean,
	p_ItemID integer,
	p_User varchar(255),
	p_ProgressNote varchar(1000)
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	update SobekCM_Item
	set Material_Received_Date=p_Material_Received_Date, Material_Recd_Date_Estimated=p_Material_Recd_Date_Estimated
	where ItemID = p_ItemID;

	if ( not p_Material_Recd_Date_Estimated ) then
		insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath )
		values ( p_ItemID, 42, p_Material_Received_Date, p_User, p_ProgressNote, '' );
	end if;
END;
$$;


CREATE OR REPLACE FUNCTION Tracking_Update_Tracking_Box(
	p_Tracking_Box varchar(25),
	p_ItemID integer
)
RETURNS void
LANGUAGE sql
AS $$
	update SobekCM_Item set Tracking_Box = p_Tracking_Box where ItemID = p_ItemID;
$$;


CREATE OR REPLACE FUNCTION Tracking_Update_Workflow(
	p_itemid integer,
	p_user varchar(50),
	p_dateStarted timestamp,
	p_dateCompleted timestamp,
	p_relatedEquipment varchar(1000),
	p_EventNumber integer,
	p_StartEventNumber integer,
	p_EndEventNumber integer,
	p_workflow_entry_id integer
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
	v_workflowid integer;
BEGIN
	v_workflowid := coalesce((select WorkFlowID from Tracking_Workflow where Start_Event_Number = p_EventNumber or End_Event_Number = p_EventNumber ), -1);

	Update Tracking_Progress
	set DateStarted=p_dateStarted,
	    DateCompleted=p_dateCompleted,
	    RelatedEquipment=p_relatedEquipment,
	    Start_Event_Number=p_StartEventNumber,
	    End_Event_Number = p_EndEventNumber,
	    WorkFlowID = v_workflowid,
	    WorkPerformedBy = p_user
	where ProgressID=p_workflow_entry_id;
END;
$$;

/** !START_GRANT_PERMISSIONS! **/

-- The original T-SQL script's GRANT EXECUTE section only actually enumerated 88 of the
-- (237) procedures individually -- the rest had no grant statement at all, even though the
-- file's own header comment states the intent is to grant EXECUTE on every procedure to both
-- roles. Rather than reproduce that apparent drift/bug, this grants every function that exists
-- right now (i.e. every function this script just created) to both roles.
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO sobek_user;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO sobek_builder;

-- Unlike SQL Server's schema-scoped GRANT (GRANT EXECUTE ON SCHEMA::dbo), the statement above is
-- a one-time snapshot: it does NOT cover functions created after this script runs. This sets the
-- default privilege so that any function created *by whichever role runs this script* (i.e. the
-- role executing this install), from this point forward, is automatically EXECUTE-granted to both
-- roles too -- matching the SQL Server side's "no need to remember a GRANT for new procs" property.
-- If a future migration creates functions as a DIFFERENT role than the one that ran this script,
-- re-run these two statements as that role (or add `FOR ROLE that_role` below) so new functions
-- stay covered.
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT EXECUTE ON FUNCTIONS TO sobek_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT EXECUTE ON FUNCTIONS TO sobek_builder;

/** !START_ROW_INSERTS! **/

insert into mySobek_Editable_Regex ( Editable_Name, EditableRegex )
values ( 'ALL', '[A-Z]{2}[A-Z|0-9]{4}[0-9]{4}' );

insert into SobekCM_External_Record_Type (ExtRecordType, repeatableTypeFlag) values ('OCLC', true);
insert into SobekCM_External_Record_Type (ExtRecordType, repeatableTypeFlag) values ('ALEPH', true);
insert into SobekCM_External_Record_Type (ExtRecordType, repeatableTypeFlag) values ('LCCN', true);
insert into SobekCM_External_Record_Type (ExtRecordType, repeatableTypeFlag) values ('ISSN', true);
insert into SobekCM_External_Record_Type (ExtRecordType, repeatableTypeFlag) values ('ISBN', true);
insert into SobekCM_External_Record_Type (ExtRecordType, repeatableTypeFlag) values ('ACCESSION', true);


insert into mySobek_Template (TemplateName, TemplateCode) values ('Internal Template', 'INTERNAL');
insert into mySobek_Template (TemplateName, TemplateCode) values ('IR Template', 'IR');
insert into mySobek_Template (TemplateName, TemplateCode) values ('dLOC Template', 'DLOC');

insert into mySobek_DefaultMetadata (MetadataName, MetadataCode, Description) values ('No default values', 'NONE', 'Default metadata set which represents NO default metadata');


insert into SobekCM_Item_Aggregation_Result_Types (ItemAggregationResultTypeID, ResultType, DefaultOrder, DefaultView) values (1, 'BRIEF', 1, true);
insert into SobekCM_Item_Aggregation_Result_Types (ItemAggregationResultTypeID, ResultType, DefaultOrder, DefaultView) values (2, 'THUMBNAIL', 2, true);
insert into SobekCM_Item_Aggregation_Result_Types (ItemAggregationResultTypeID, ResultType, DefaultOrder, DefaultView) values (3, 'TABLE', 3, true);
insert into SobekCM_Item_Aggregation_Result_Types (ItemAggregationResultTypeID, ResultType, DefaultOrder, DefaultView) values (4, 'EXPORT', 4, false);
insert into SobekCM_Item_Aggregation_Result_Types (ItemAggregationResultTypeID, ResultType, DefaultOrder, DefaultView) values (5, 'GMAP', 5, true);

insert into SobekCM_Builder_Module_Type (ModuleTypeID, TypeAbbrev, TypeDescription) values (1, 'PRE', 'Pre-Process modules run each time BEFORE processing any pending items/requests');
insert into SobekCM_Builder_Module_Type (ModuleTypeID, TypeAbbrev, TypeDescription) values (2, 'POST', 'Post-Process modules run each time AFTER processing any pending items/requests');
insert into SobekCM_Builder_Module_Type (ModuleTypeID, TypeAbbrev, TypeDescription) values (3, 'NEW', 'Submission modules run for each incoming item (or items set to reprocess)');
insert into SobekCM_Builder_Module_Type (ModuleTypeID, TypeAbbrev, TypeDescription) values (4, 'DELT', 'Submission modules run for each incoming DELETE request');
insert into SobekCM_Builder_Module_Type (ModuleTypeID, TypeAbbrev, TypeDescription) values (5, 'SCHD', 'Schedulable modules run as a scheduled task by the builder');
insert into SobekCM_Builder_Module_Type (ModuleTypeID, TypeAbbrev, TypeDescription) values (6, 'FOLD', 'Folder-level modules are run to prepare and find items in incoming folders');

insert into SobekCM_Builder_Module_Set (ModuleSetID, ModuleTypeID, SetName, SetOrder, Enabled) values (1, 1, 'Standard PRE-process modules', 1, true);
insert into SobekCM_Builder_Module_Set (ModuleSetID, ModuleTypeID, SetName, SetOrder, Enabled) values (2, 2, 'Standard POST-process modules', 1, true);
insert into SobekCM_Builder_Module_Set (ModuleSetID, ModuleTypeID, SetName, SetOrder, Enabled) values (3, 3, 'Incoming item processing', 1, true);
insert into SobekCM_Builder_Module_Set (ModuleSetID, ModuleTypeID, SetName, SetOrder, Enabled) values (4, 4, 'Incoming delete processing', 1, true);
insert into SobekCM_Builder_Module_Set (ModuleSetID, ModuleTypeID, SetName, SetOrder, Enabled) values (5, 5, 'Expire old builder logs', 1, false);
insert into SobekCM_Builder_Module_Set (ModuleSetID, ModuleTypeID, SetName, SetOrder, Enabled) values (6, 5, 'Rebuild all aggregation browse files', 1, false);
insert into SobekCM_Builder_Module_Set (ModuleSetID, ModuleTypeID, SetName, SetOrder, Enabled) values (7, 5, 'Send new item emails', 1, false);
insert into SobekCM_Builder_Module_Set (ModuleSetID, ModuleTypeID, SetName, SetOrder, Enabled) values (8, 5, 'Solr/Lucene index optimization', 1, false);
insert into SobekCM_Builder_Module_Set (ModuleSetID, ModuleTypeID, SetName, SetOrder, Enabled) values (9, 5, 'Update cached aggregation browses', 1, true);
insert into SobekCM_Builder_Module_Set (ModuleSetID, ModuleTypeID, SetName, SetOrder, Enabled) values (10, 6, 'Standard folder processing', 1, true);
insert into SobekCM_Builder_Module_Set (ModuleSetID, ModuleTypeID, SetName, SetOrder, Enabled) values (11, 5, 'Usage statistics calculation', 1, true);

insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (1, 1, 'Load reports from FDA (Florida Digital Archives) for Florida universities', NULL, 'SobekCM.Builder_Library.Modules.PreProcess.ProcessPendingFdaReportsModule', false, 10, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (2, 2, 'Build the aggregation browse files', NULL, 'SobekCM.Builder_Library.Modules.PostProcess.BuildAggregationBrowsesModule', true, 10, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (3, 3, 'Convert office files to PDFs', NULL, 'SobekCM.Builder_Library.Modules.Items.ConvertOfficeFilesToPdfModule', true, 10, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (4, 3, 'Extract text from all PDFs', NULL, 'SobekCM.Builder_Library.Modules.Items.ExtractTextFromPdfModule', true, 20, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (5, 3, 'Create thumbnails for all PDFs', NULL, 'SobekCM.Builder_Library.Modules.Items.CreatePdfThumbnailModule', true, 30, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (6, 3, 'Extract the text from included HTML files', NULL, 'SobekCM.Builder_Library.Modules.Items.ExtractTextFromHtmlModule', true, 40, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (7, 3, 'Extract the text from included (non-standard) XML files', NULL, 'SobekCM.Builder_Library.Modules.Items.ExtractTextFromXmlModule', true, 50, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (8, 3, 'OCR tiff files', NULL, 'SobekCM.Builder_Library.Modules.Items.OcrTiffsModule', true, 80, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (9, 3, 'Clean any dirty ocr (non-unicode friendly)', NULL, 'SobekCM.Builder_Library.Modules.Items.CleanDirtyOcrModule', false, 90, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (10, 3, 'Check for SSNs in any loaded text', NULL, 'SobekCM.Builder_Library.Modules.Items.CheckForSsnModule', true, 100, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (11, 3, 'Handle extra large JPEGs', NULL, 'SobekCM.Builder_Library.Modules.Items.ConvertLargeJpegsItemModule', true, 70, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (12, 3, 'Create image derivatives (jpegs and jpeg2000s)', NULL, 'SobekCM.Builder_Library.Modules.Items.CreateImageDerivativesModule', true, 120, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (13, 3, 'Copy all incoming files to the archive folder', NULL, 'SobekCM.Builder_Library.Modules.Items.CopyToArchiveFolderModule', true, 140, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (14, 3, 'Move files to the image server', NULL, 'SobekCM.Builder_Library.Modules.Items.MoveFilesToImageServerModule', true, 160, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (15, 3, 'Reload the METS and basic database info', NULL, 'SobekCM.Builder_Library.Modules.Items.ReloadMetsAndBasicDbInfoModule', true, 170, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (16, 3, 'Update JPEG attributes (width and height)', NULL, 'SobekCM.Builder_Library.Modules.Items.UpdateJpegAttributesModule', true, 180, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (17, 3, 'Attach all non-image files to the item', NULL, 'SobekCM.Builder_Library.Modules.Items.AttachAllNonImageFilesModule', true, 190, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (19, 3, 'Ensure a main thumbnail is referenced', NULL, 'SobekCM.Builder_Library.Modules.Items.EnsureMainThumbnailModule', true, 210, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (20, 3, 'Get number of pages for PDF-only types', NULL, 'SobekCM.Builder_Library.Modules.Items.GetPageCountFromPdfModule', true, 220, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (21, 3, 'Update the web.config for restricted items', NULL, 'SobekCM.Builder_Library.Modules.Items.UpdateWebConfigModule', true, 230, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (22, 3, 'Save the service METS file', NULL, 'SobekCM.Builder_Library.Modules.Items.SaveServiceMetsModule', true, 240, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (23, 3, 'Save a Marc21 XML file', NULL, 'SobekCM.Builder_Library.Modules.Items.SaveMarcXmlModule', true, 250, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (24, 3, 'Save to the database', NULL, 'SobekCM.Builder_Library.Modules.Items.SaveToDatabaseModule', true, 260, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (25, 3, 'Save to the old solr/lucene legacy indexes', NULL, 'SobekCM.Builder_Library.Modules.Items.SaveToSolrLuceneModule_Legacy', true, 270, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (26, 3, 'Clean the web resource folder', NULL, 'SobekCM.Builder_Library.Modules.Items.CleanWebResourceFolderModule', true, 290, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (27, 3, 'Build static version for SEO', NULL, 'SobekCM.Builder_Library.Modules.Items.CreateStaticVersionModule', true, 300, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (28, 3, 'Add tracking information', NULL, 'SobekCM.Builder_Library.Modules.Items.AddTrackingWorkflowModule', true, 310, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (29, 4, 'Loads the METS and basic database info', NULL, 'SobekCM.Builder_Library.Modules.Items.ReloadMetsAndBasicDbInfoModule', true, 10, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (30, 4, 'Delete item in database and folder', NULL, 'SobekCM.Builder_Library.Modules.Items.DeleteItemModule', true, 20, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (31, 5, 'Expire old builder logs', NULL, 'SobekCM.Builder_Library.Modules.Schedulable.ExpireOldLogEntriesModule', true, 10, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (32, 6, 'Rebuild all aggregation browse files', NULL, 'SobekCM.Builder_Library.Modules.Schedulable.RebuildAllAggregationBrowsesModule', true, 10, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (33, 7, 'Send new item emails', NULL, 'SobekCM.Builder_Library.Modules.Schedulable.SendNewItemEmailsModule', true, 10, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (34, 8, 'Solr/Lucene index optimization', NULL, 'SobekCM.Builder_Library.Modules.Schedulable.SolrLuceneIndexOptimizationModule', true, 10, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (35, 9, 'Update cached aggregation browses', NULL, 'SobekCM.Builder_Library.Modules.Schedulable.UpdatedCachedAggregationMetadataModule', true, 10, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (36, 10, 'Check packages for age and move', NULL, 'SobekCM.Builder_Library.Modules.Folders.MoveAgedPackagesToProcessModule', true, 10, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (37, 10, 'Check for any bib id restrictions on this folder', NULL, 'SobekCM.Builder_Library.Modules.Folders.ApplyBibIdRestrictionModule', true, 20, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (38, 10, 'Validate each folder and classify (delete v. new/update)', NULL, 'SobekCM.Builder_Library.Modules.Folders.ValidateAndClassifyModule', true, 30, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (39, 3, 'Attach ALL the images in the resource folder to the item', NULL, 'SobekCM.Builder_Library.Modules.Items.AttachImagesAllModule', true, 200, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (40, 11, 'Usage statistics calculation and usage email sends', NULL, 'SobekCM.Builder_Library.Modules.Schedulable.CalculateUsageStatisticsModule', true, 10, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (41, 3, 'Save to the new version 5 beta solr/lucene indexes.', NULL, 'SobekCM.Builder_Library.Modules.Items.SaveToSolrLuceneModule_v5', true, 280, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (42, 3, 'Convert JPEG2000s to NonMaster TIFFs', NULL, 'SobekCM.Builder_Library.Modules.Items.ConvertJpeg2000sItemModule', true, 60, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (43, 3, 'Delete any NonMaster TIFFs', NULL, 'SobekCM.Builder_Library.Modules.Items.DeleteNonMasterTiffsModule', true, 130, NULL, NULL, NULL);
insert into SobekCM_Builder_Module (ModuleID, ModuleSetID, ModuleDesc, Assembly, Class, Enabled, "Order", Argument1, Argument2, Argument3) values (44, 3, 'Delete any files that should not be retained', NULL, 'SobekCM.Builder_Library.Modules.Items.DeleteNonRetainedFilesModule', true, 150, NULL, NULL, NULL);

insert into SobekCM_Builder_Module_Schedule (ModuleScheduleID, ModuleSetID, DaysOfWeek, Enabled, TimesOfDay, Description) values (1, 11, 'M', true, '0600', 'Calculate the usage statistics');
insert into SobekCM_Builder_Module_Schedule (ModuleScheduleID, ModuleSetID, DaysOfWeek, Enabled, TimesOfDay, Description) values (2, 5, 'MWF', true, '0530', 'Expire old builder logs');
insert into SobekCM_Builder_Module_Schedule (ModuleScheduleID, ModuleSetID, DaysOfWeek, Enabled, TimesOfDay, Description) values (3, 6, 'MTWRF', true, '0900', 'Rebuild all aggregation browse files');
insert into SobekCM_Builder_Module_Schedule (ModuleScheduleID, ModuleSetID, DaysOfWeek, Enabled, TimesOfDay, Description) values (4, 7, 'MTWRF', true, '2100', 'Send new item emails');
insert into SobekCM_Builder_Module_Schedule (ModuleScheduleID, ModuleSetID, DaysOfWeek, Enabled, TimesOfDay, Description) values (5, 8, 'S', true, '2200', 'Solr/Lucene index optimization');
insert into SobekCM_Builder_Module_Schedule (ModuleScheduleID, ModuleSetID, DaysOfWeek, Enabled, TimesOfDay, Description) values (6, 9, 'MWF', true, '2130', 'Update all cached aggregation browses');

insert into mySobek_User_Item_Link_Relationship (RelationshipID, RelationshipLabel, Include_In_Results) values (1, 'Submittor', true);
insert into mySobek_User_Item_Link_Relationship (RelationshipID, RelationshipLabel, Include_In_Results) values (2, 'Author', true);
insert into mySobek_User_Item_Link_Relationship (RelationshipID, RelationshipLabel, Include_In_Results) values (3, 'Contributor', true);
insert into mySobek_User_Item_Link_Relationship (RelationshipID, RelationshipLabel, Include_In_Results) values (4, 'ANALYZED; NO RELATION', false);
insert into mySobek_User_Item_Link_Relationship (RelationshipID, RelationshipLabel, Include_In_Results) values (5, 'Thesis Advisor', true);
insert into mySobek_User_Item_Link_Relationship (RelationshipID, RelationshipLabel, Include_In_Results) values (6, 'Other', true);

insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (1, 'JPEG', 10, true, 500.1);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (2, 'JPEG2000', 12, true, 500.2);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (3, 'TEXT', 15, false, 500.3);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (4, 'PAGE_TURNER', 20, false, 120);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (5, 'GOOGLE_MAP', 18, true, 118);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (6, 'HTML', 14, false, 116);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (7, 'HTML Map Viewer', 100, false, 200);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (8, 'RELATED_IMAGES', 21, true, 400);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (9, 'TOC', 26, false, 126);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (10, 'TEI', 25, false, 125);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (11, 'DATASET_CODEBOOK', 1, false, 101);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (12, 'DATASET_REPORTS', 2, false, 102);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (13, 'DATASET_VIEWDATA', 3, false, 103);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (14, 'JPEG_TEXT_TWO_UP', 11, false, 111);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (15, 'EAD_CONTAINER_LIST', 4, false, 104);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (16, 'EAD_DESCRIPTION', 5, false, 105);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (17, 'YOUTUBE_VIDEO', 6, false, 106);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (18, 'EMBEDDED_VIDEO', 7, false, 107);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (19, 'FLASH', 9, false, 109);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (20, 'PDF', 13, true, 113);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (21, 'DOWNLOADS', 16, true, 114);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (22, 'CITATION', 17, true, 10.1);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (23, 'FEATURES', 19, false, 119);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (24, 'SEARCH', 22, false, 30);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (25, 'SIMPLE_HTML_LINK', 23, false, 123);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (26, 'STREETS', 24, false, 124);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (27, 'ALL_VOLUMES', 27, true, 20);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (28, 'VIDEO', 7, true, 107);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (29, 'MARC', 100, true, 10.2);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (30, 'METADATA', 100, true, 10.3);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (31, 'USAGE', 100, true, 10.4);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (32, 'WEBSITE', 14, false, 116);
insert into SobekCM_Item_Viewer_Types (ItemViewTypeID, ViewType, "Order", DefaultView, MenuOrder) values (33, 'OPEN_TEXTBOOK', 15, false, 117);

insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (1, 'Title', 'TI', 'title', 'Title', 'Title', false, true, true, 'title', NULL, 'title');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (2, 'Type', 'TY', 'type', 'Resource Type', 'Resource Type', false, true, true, 'type', 'type_facets', 'type');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (3, 'Language', 'LA', 'language', 'Language', 'Language', false, true, true, 'language', 'language_facets', 'language');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (4, 'Creator', 'AU', 'creator', 'Creator', 'Creator', false, true, true, 'creator', 'creator_facets', 'creator.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (5, 'Publisher', 'PU', 'publisher', 'Publisher', 'Publisher', false, true, true, 'publisher', 'publisher_facets', 'publisher.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (6, 'Publication Place', 'PP', 'publication_place', 'Publication Place', 'Publication Place', false, true, true, 'publication place', 'publication_place_facets', 'publication_place');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (7, 'Subject Keyword', 'TO', 'subject', 'Subject Keyword', 'Subject: Topic', false, true, true, 'subject keyword', 'subject_facets', 'subject.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (8, 'Genre', 'GE', 'genre', 'Material Type', 'Material Type', false, true, false, 'genre', 'genre_facets', 'genre.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (9, 'Target Audience', 'TA', 'audience', 'Target Audience', 'Target Audience', false, true, false, 'target audience', 'audience_facets', 'audience');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (10, 'Spatial Coverage', 'SP', 'spatial_standard', 'Spatial Coverage', 'Subject: Geographic Area', false, true, false, 'spatial coverage', 'spatial_standard_facets', 'spatial_standard.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (11, 'Country', 'CO', 'country', 'Country', 'Country', false, true, false, 'country', 'country_facets', 'country');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (12, 'State', 'ST', 'state', 'State', 'State', false, true, false, 'state', 'state_facets', 'state');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (13, 'County', 'CT', 'county', 'County', 'County', false, true, false, 'county', 'county_facets', 'county');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (14, 'City', 'CI', 'city', 'City', 'City', false, true, false, 'city', 'city_facets', 'city');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (15, 'Source Institution', 'SO', 'source', 'Source Institution', 'Source Institution', false, true, false, 'source institution', 'source_facets', 'source');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (16, 'Holding Location', 'HO', 'holding', 'Holding Location', 'Holding Location', false, true, false, 'holding location', 'holding_facets', 'holding');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (17, 'Identifier', 'ID', 'identifier', 'Identifier', 'Identifier', false, false, false, 'identifier', 'identifier_facets', 'identifier.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (18, 'Notes', 'NO', 'notes', 'Notes', 'Notes', false, false, false, 'notes', NULL, 'notes');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (19, 'Other_Citation', '  ', 'other', 'Other_Citation', 'Other_Citation', false, false, false, 'other_citation', NULL, 'other');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (20, 'Tickler', 'TL', 'tickler', 'Tickler', 'Tickler', false, true, false, 'tickler', 'tickler_facets', 'tickler');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (21, 'Donor', 'DO', 'donor', 'Donor', 'Donor', false, true, false, 'donor', 'donor_facets', 'donor');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (22, 'Format', 'FO', 'format', 'Description', 'Description', false, true, false, 'format', 'format_facets', 'format');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (23, 'BibID', 'BI', 'bibid', 'BibID', 'BibID', false, false, false, 'bibid', NULL, 'bibid');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (24, 'Publication Date', 'DA', 'date', 'Publication Date', 'Publication Date', false, true, false, 'publication date', 'date_facets', 'date.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (25, 'Affiliation', 'AF', 'affiliation', 'Affiliation', 'Affiliation', false, true, false, 'affiliation', 'affiliation_facets', 'affiliation.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (26, 'Frequency', 'FR', 'frequency', 'Frequency', 'Frequency', false, true, false, 'frequency', 'frequency_facets', 'frequency');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (27, 'Name as Subject', 'SN', 'name_as_subject', 'Name as Subject', 'Name as Subject', false, true, false, 'name as subject', 'name_as_subject_facets', 'name_as_subject.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (28, 'Title as Subject', 'TS', 'title_as_subject', 'Title as Subject', 'Title as Subject', false, true, false, 'title as subject', 'title_as_subject_facets', 'title_as_subject.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (29, 'All Subjects', 'SU', 'subject_all', 'All Subjects', 'All Subjects', false, true, false, 'all subjects', NULL, NULL);
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (30, 'Temporal Subject', 'TE', 'temporal_subject', 'Temporal Subject', 'Temporal Subject', false, true, false, 'temporal_subject', NULL, 'temporal subject');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (31, 'Attribution', 'AT', 'attribution', 'Attribution', 'Attribution', false, true, false, 'attribution', NULL, 'attribution');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (32, 'User Description', 'DE', 'User_Description', 'User Description', 'User Description', false, false, false, 'User_Description', NULL, 'user description');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (33, 'Temporal Decade', 'DD', 'temporal_decade', 'Temporal Decade', 'Temporal Decade', false, true, false, 'temporal_decade', NULL, 'temporal decade');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (34, 'MIME Type', 'MI', 'mime_type', 'MIME Type', 'MIME Type', false, true, false, 'mime type', 'mime_type_facets', 'mime_type');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (35, 'Full Citation', 'FC', 'fullcitation', 'Full Citation', 'Full Citation', false, false, false, 'allfields', NULL, NULL);
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (36, 'Tracking Box', 'TB', 'tracking_box', 'Tracking Box', 'Tracking Box', false, true, false, 'tracking box', 'tracking_box_facets', 'tracking_box');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (37, 'Abstract', 'AB', 'abstract', 'Abstract', 'Abstract', false, false, false, 'abstract', NULL, 'abstract');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (38, 'Edition', 'ET', 'edition', 'Edition', 'Edition', false, true, false, 'edition', 'edition_facets', 'edition');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (39, 'TOC', 'TC', 'toc', 'TOC', 'TOC', false, false, false, 'toc', NULL, NULL);
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (40, 'ZT Kingdom', 'ZK', 'zt_kingdom', 'Taxonomic Kingdom', 'Taxonomic Kingdom', false, true, false, 'zt kingdom', 'zt_kingdom_facets', 'zt_kingdom');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (41, 'ZT Phylum', 'ZP', 'zt_phylum', 'Taxonomic Phylum', 'Taxonomic Phylum', false, true, false, 'zt phylum', 'zt_phylum_facets', 'zt_phylum');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (42, 'ZT Class', 'ZC', 'zt_class', 'Taxonomic Class', 'Taxonomic Class', false, true, false, 'zt class', 'zt_class_facets', 'zt_class');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (43, 'ZT Order', 'ZO', 'zt_order', 'Taxonomic Order', 'Taxonomic Order', false, true, false, 'zt order', 'zt_order_facets', 'zt_order');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (44, 'ZT Family', 'ZF', 'zt_family', 'Taxonomic Family', 'Taxonomic Family', false, true, false, 'zt family', 'zt_family_facets', 'zt_family');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (45, 'ZT Genus', 'ZG', 'zt_genus', 'Taxonomic Genus', 'Taxonomic Genus', false, true, false, 'zt genus', 'zt_genus_facets', 'zt_genus');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (46, 'ZT Species', 'ZS', 'zt_species', 'Taxonomic Species', 'Taxonomic Species', false, true, false, 'zt species', 'zt_species_facets', 'zt_species');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (47, 'ZT Common Name', 'ZN', 'zt_common_name', 'Taxonomic Common Name', 'Taxonomic Common Name', false, true, false, 'zt common name', 'zt_common_name_facets', 'zt_common_name');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (48, 'ZT Scientific Name', 'ZI', 'zt_scientific_name', 'Taxonomic Scientific Name', 'Taxonomic Scientific Name', false, true, false, 'zt scientific name', 'zt_scientific_name_facets', 'zt_scientific_name');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (49, 'ZT All Taxonomy', 'ZA', '', 'Taxonomic All Taxonomy', 'Taxonomic All Taxonomy', false, true, false, '', NULL, 'zt all taxonomy');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (50, 'Cultural Context', 'CC', 'cultural_context', 'Cultural Context', 'Cultural Context', false, true, false, 'cultural context', 'cultural_context_facets', 'cultural_context');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (51, 'Inscription', 'IN', 'inscription', 'Inscription', 'Inscription', false, true, false, 'inscription', NULL, 'inscription');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (52, 'Material', 'MA', 'material', 'Material', 'Material', false, true, false, 'material', 'material_facets', 'material.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (53, 'Style Period', 'SY', 'style_period', 'Style Period', 'Style Period', false, true, false, 'style period', 'style_period_facets', 'style_period');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (54, 'Technique', 'TQ', 'technique', 'Technique', 'Technique', false, true, false, 'technique', 'technique_facets', 'technique');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (55, 'Accession Number', 'AN', 'accession_number', 'Accession Number', 'Accession Number', false, true, false, 'accession', 'accession_number_facets', 'accession_number.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (56, 'ETD Committee', 'EC', 'etd_committee', 'ETD Committee', 'ETD Committee', false, true, false, 'etd committee', 'etd_committee_facets', 'etd_committee');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (57, 'ETD Degree', 'ED', 'etd_degree', 'ETD Degree', 'ETD Degree', false, true, false, 'etd degree', 'etd_degree_facets', 'etd_degree');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (58, 'ETD Degree Discipline', 'EI', 'etd_degree_discipline', 'ETD Degree Discipline', 'ETD Degree Discipline', false, true, false, 'etd degree discipline', 'etd_degree_discipline_facets', 'etd_degree_discipline');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (59, 'ETD Degree Grantor', 'EG', 'etd_degree_grantor', 'ETD Degree Grantor', 'ETD Degree Grantor', false, true, false, 'etd degree grantor', 'etd_degree_grantor_facets', 'etd_degree_grantor');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (60, 'ETD Degree Level', 'EL', 'etd_degree_level', 'ETD Degree Level', 'ETD Degree Level', false, true, false, 'etd degree level', 'etd_degree_level_facets', 'etd_degree_level');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (61, 'Temporal Year', 'DY', 'temporal_year', 'Temporal Year', 'Temporal Year', false, true, false, 'temporal_year', NULL, 'temporal year');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (62, 'Interviewee', 'OI', 'interviewee', 'Intervewiee', 'Intervewiee', false, true, false, 'interviewee', 'interviewee_facets', 'interviewee');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (63, 'Interviewer', 'OV', 'interviewer', 'Intervewer', 'Intervewer', false, true, false, 'interviewer', 'interviewer_facets', 'interviewer');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (64, 'UserDefined01', 'UA', 'user_defined_01', 'Temporal Subject Display', 'Temporal Subject Display', true, true, false, 'userdefined01', 'user_defined_01_facets', 'user_defined_01.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (65, 'UserDefined02', 'UB', 'user_defined_02', 'LOM Resource Type Display', 'LOM Resource Type Display', true, true, false, 'userdefined02', 'user_defined_02_facets', 'user_defined_02.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (66, 'UserDefined03', 'UC', 'user_defined_03', 'LOM Intended End User Display', 'LOM Intended End User Display', true, true, false, 'userdefined03', 'user_defined_03_facets', 'user_defined_03.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (67, 'UserDefined04', 'UD', 'user_defined_04', 'Course Title', 'Course Title', true, true, false, 'userdefined04', 'user_defined_04_facets', 'user_defined_04.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (68, 'UserDefined05', 'UE', 'user_defined_05', 'Licensing', 'Licensing', true, true, false, 'userdefined05', 'user_defined_05_facets', 'user_defined_05.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (69, 'UserDefined06', 'UF', 'user_defined_06', 'Undefined', 'Undefined', true, true, false, 'userdefined06', 'user_defined_06_facets', 'user_defined_06.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (70, 'UserDefined07', 'UG', 'user_defined_07', 'Undefined', 'Undefined', true, true, false, 'userdefined07', 'user_defined_07_facets', 'user_defined_07.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (71, 'UserDefined08', 'UH', 'user_defined_08', 'Undefined', 'Undefined', true, true, false, 'userdefined08', 'user_defined_08_facets', 'user_defined_08.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (72, 'UserDefined09', 'UI', 'user_defined_09', 'Undefined', 'Undefined', true, true, false, 'userdefined09', 'user_defined_09_facets', 'user_defined_09.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (73, 'UserDefined10', 'UJ', 'user_defined_10', 'Undefined', 'Undefined', true, true, false, 'userdefined10', 'user_defined_10_facets', 'user_defined_10.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (74, 'UserDefined11', 'UK', 'user_defined_11', 'Undefined', 'Undefined', true, true, false, 'userdefined11', 'user_defined_11_facets', 'user_defined_11.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (75, 'UserDefined12', 'UL', 'user_defined_12', 'Undefined', 'Undefined', true, true, false, 'userdefined12', 'user_defined_12_facets', 'user_defined_12.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (76, 'UserDefined13', 'UM', 'user_defined_13', 'Undefined', 'Undefined', true, true, false, 'userdefined13', 'user_defined_13_facets', 'user_defined_13.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (77, 'UserDefined14', 'UN', 'user_defined_14', 'Undefined', 'Undefined', true, true, false, 'userdefined14', 'user_defined_14_facets', 'user_defined_14.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (78, 'UserDefined15', 'UO', 'user_defined_15', 'Undefined', 'Undefined', true, true, false, 'userdefined15', 'user_defined_15_facets', 'user_defined_15.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (79, 'UserDefined16', 'UP', 'user_defined_16', 'Undefined', 'Undefined', true, true, false, 'userdefined16', 'user_defined_16_facets', 'user_defined_16.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (80, 'UserDefined17', 'UQ', 'user_defined_17', 'Undefined', 'Undefined', true, true, false, 'userdefined17', 'user_defined_17_facets', 'user_defined_17.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (81, 'UserDefined18', 'UR', 'user_defined_18', 'Undefined', 'Undefined', true, true, false, 'userdefined18', 'user_defined_18_facets', 'user_defined_18.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (82, 'UserDefined19', 'US', 'user_defined_19', 'Undefined', 'Undefined', true, true, false, 'userdefined19', 'user_defined_19_facets', 'user_defined_19.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (83, 'UserDefined20', 'UT', 'user_defined_20', 'Undefined', 'Undefined', true, true, false, 'userdefined20', 'user_defined_20_facets', 'user_defined_20.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (84, 'UserDefined21', 'UU', 'user_defined_21', 'Undefined', 'Undefined', true, true, false, 'userdefined21', 'user_defined_21_facets', 'user_defined_21.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (85, 'UserDefined22', 'UV', 'user_defined_22', 'Undefined', 'Undefined', true, true, false, 'userdefined22', 'user_defined_22_facets', 'user_defined_22.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (86, 'UserDefined23', 'UW', 'user_defined_23', 'Undefined', 'Undefined', true, true, false, 'userdefined23', 'user_defined_23_facets', 'user_defined_23.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (87, 'UserDefined24', 'UX', 'user_defined_24', 'Undefined', 'Undefined', true, true, false, 'userdefined24', 'user_defined_24_facets', 'user_defined_24.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (88, 'UserDefined25', 'UY', 'user_defined_25', 'Undefined', 'Undefined', true, true, false, 'userdefined25', 'user_defined_25_facets', 'user_defined_25.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (89, 'UserDefined26', 'UZ', 'user_defined_26', 'Undefined', 'Undefined', true, true, false, 'userdefined26', 'user_defined_26_facets', 'user_defined_26.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (90, 'UserDefined27', 'VA', 'user_defined_27', 'Undefined', 'Undefined', true, true, false, 'userdefined27', 'user_defined_27_facets', 'user_defined_27.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (91, 'UserDefined28', 'VB', 'user_defined_28', 'Undefined', 'Undefined', true, true, false, 'userdefined28', 'user_defined_28_facets', 'user_defined_28.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (92, 'UserDefined29', 'VC', 'user_defined_29', 'Undefined', 'Undefined', true, true, false, 'userdefined29', 'user_defined_29_facets', 'user_defined_29.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (93, 'UserDefined30', 'VD', 'user_defined_30', 'Undefined', 'Undefined', true, true, false, 'userdefined30', 'user_defined_30_facets', 'user_defined_30.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (94, 'UserDefined31', 'VE', 'user_defined_31', 'Undefined', 'Undefined', true, true, false, 'userdefined31', 'user_defined_31_facets', 'user_defined_31.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (95, 'UserDefined32', 'VF', 'user_defined_32', 'Undefined', 'Undefined', true, true, false, 'userdefined32', 'user_defined_32_facets', 'user_defined_32.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (96, 'UserDefined33', 'VG', 'user_defined_33', 'Undefined', 'Undefined', true, true, false, 'userdefined33', 'user_defined_33_facets', 'user_defined_33.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (97, 'UserDefined34', 'VH', 'user_defined_34', 'Undefined', 'Undefined', true, true, false, 'userdefined34', 'user_defined_34_facets', 'user_defined_34.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (98, 'UserDefined35', 'VI', 'user_defined_35', 'Undefined', 'Undefined', true, true, false, 'userdefined35', 'user_defined_35_facets', 'user_defined_35.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (99, 'UserDefined36', 'VJ', 'user_defined_36', 'Undefined', 'Undefined', true, true, false, 'userdefined36', 'user_defined_36_facets', 'user_defined_36.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (100, 'UserDefined37', 'VK', 'user_defined_37', 'Undefined', 'Undefined', true, true, false, 'userdefined37', 'user_defined_37_facets', 'user_defined_37.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (101, 'UserDefined38', 'VL', 'user_defined_38', 'Undefined', 'Undefined', true, true, false, 'userdefined38', 'user_defined_38_facets', 'user_defined_38.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (102, 'UserDefined39', 'VM', 'user_defined_39', 'Undefined', 'Undefined', true, true, false, 'userdefined39', 'user_defined_39_facets', 'user_defined_39.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (103, 'UserDefined40', 'VN', 'user_defined_40', 'Undefined', 'Undefined', true, true, false, 'userdefined40', 'user_defined_40_facets', 'user_defined_40.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (104, 'UserDefined41', 'VO', 'user_defined_41', 'Undefined', 'Undefined', true, true, false, 'userdefined41', 'user_defined_41_facets', 'user_defined_41.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (105, 'UserDefined42', 'VP', 'user_defined_42', 'Undefined', 'Undefined', true, true, false, 'userdefined42', 'user_defined_42_facets', 'user_defined_42.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (106, 'UserDefined43', 'VQ', 'user_defined_43', 'Undefined', 'Undefined', true, true, false, 'userdefined43', 'user_defined_43_facets', 'user_defined_43.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (107, 'UserDefined44', 'VR', 'user_defined_44', 'Undefined', 'Undefined', true, true, false, 'userdefined44', 'user_defined_44_facets', 'user_defined_44.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (108, 'UserDefined45', 'VS', 'user_defined_45', 'Undefined', 'Undefined', true, true, false, 'userdefined45', 'user_defined_45_facets', 'user_defined_45.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (109, 'UserDefined46', 'VT', 'user_defined_46', 'Undefined', 'Undefined', true, true, false, 'userdefined46', 'user_defined_46_facets', 'user_defined_46.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (110, 'UserDefined47', 'VU', 'user_defined_47', 'Undefined', 'Undefined', true, true, false, 'userdefined47', 'user_defined_47_facets', 'user_defined_47.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (111, 'UserDefined48', 'VV', 'user_defined_48', 'Undefined', 'Undefined', true, true, false, 'userdefined48', 'user_defined_48_facets', 'user_defined_48.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (112, 'UserDefined49', 'VW', 'user_defined_49', 'Undefined', 'Undefined', true, true, false, 'userdefined49', 'user_defined_49_facets', 'user_defined_49.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (113, 'UserDefined50', 'VX', 'user_defined_50', 'Undefined', 'Undefined', true, true, false, 'userdefined50', 'user_defined_50_facets', 'user_defined_50.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (114, 'UserDefined51', 'VY', 'user_defined_51', 'Undefined', 'Undefined', true, true, false, 'userdefined51', 'user_defined_51_facets', 'user_defined_51.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (115, 'UserDefined52', 'VZ', 'user_defined_52', 'Undefined', 'Undefined', true, true, false, 'userdefined52', 'user_defined_52_facets', 'user_defined_52.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (116, 'Publisher.Display', '  ', '', 'Publisher', 'Publisher', false, false, false, '', NULL, '');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (117, 'Spatial Coverage.Display', '  ', '', 'Spatial Coverage', 'Subject: Geographic Area', false, false, false, '', NULL, '');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (118, 'Measurements', '  ', 'measurements', 'Measurements', 'Measurements', false, false, false, '', 'measurements_facets', 'measurements.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (119, 'Subjects.Display', '  ', '', 'Subjects', 'Subjects', false, false, false, '', NULL, '');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (120, 'Aggregations', '  ', 'aggregations', 'Aggregations', 'Aggregations', false, true, false, '', NULL, 'aggregations');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (121, 'LOM Aggregation', 'LB', 'lom_aggregation', 'Aggregation (LOM)', 'Aggregation (LOM)', false, true, false, '', 'lom_aggregation_facets', 'lom_aggregation');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (122, 'LOM Context', 'LC', 'lom_context', 'Context', 'Context', false, true, false, '', 'lom_context_facets', 'lom_context.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (123, 'LOM Classification', 'LL', 'lom_classification', 'Classification', 'Classification', false, true, false, '', 'lom_classification_facets', 'lom_classification.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (124, 'LOM Difficulty', 'LD', 'lom_difficulty', 'Difficulty', 'Difficulty', false, true, false, '', 'lom_difficulty_facets', 'lom_difficulty');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (125, 'LOM Intended End User', 'LU', 'lom_intended_end_user', 'Intended End User', 'Intended End User', false, true, false, '', 'lom_intended_end_user_facets', 'lom_intended_end_user.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (126, 'LOM Interactivity Level', 'LI', 'lom_interactivity_level', 'Interactivity Level', 'Interactivity Level', false, true, false, '', 'lom_interactivity_level_facets', 'lom_interactivity_level.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (127, 'LOM Interactivity Type', 'LJ', 'lom_interactivity_type', 'Interactivity Type', 'Interactivity Type', false, true, false, '', 'lom_interactivity_type_facets', 'lom_interactivity_type.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (128, 'LOM Status', 'LS', 'lom_status', 'Status', 'Status', false, true, false, '', 'lom_status_facets', 'lom_status');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (129, 'LOM Requirement', 'LR', 'lom_requirement', 'Requirements', 'Requirements', false, true, false, '', 'lom_requirement_facets', 'lom_requirement.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (130, 'LOM Age Range', 'LG', 'lom_age_range', 'Typical Age Range', 'Typical Age Range', false, true, false, '', 'lom_age_range_facets', 'lom_age_range');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (131, 'ETD Degree Division', 'EJ', 'etd_degree_division', 'ETD Degree Division', 'ETD Degree Division', false, true, false, 'etd degree division', 'etd_degree_division_facets', 'etd_degree_division');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (132, 'Performance', 'PE', 'performance', 'Performance', 'Peformance', false, true, false, '', 'peformance_facets', 'performance.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (133, 'Performance Date', 'PD', 'performance_date', 'Performance Date', 'Peformance Date', false, true, false, '', 'peformance_date_facets', 'performance_date');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (134, 'Performer', 'PR', 'performer', 'Performer', 'Peformer', false, true, false, '', 'peformer_facets', 'performer.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (135, 'LOM Resource Type', 'LE', 'lom_resource_type', 'Learning Object Type', 'Learning Object Type', false, true, false, '', 'lom_resource_type_facets', 'lom_resource_type.display');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (136, 'LOM Learning Time', 'LT', 'lom_learning_time', 'Learning Time', 'Learning Time', false, true, false, '', 'lom_learning_time_facets', 'lom_learning_time');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (137, 'Timeline Date', '  ', 'timeline_date', 'Timeline Date', 'Timeline Date', false, true, false, '', 'timeline_date.display', 'timeline_date');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (138, 'Series Title', 'SE', 'seriestitle', 'Series Title', 'Series Title', false, true, false, '', 'seriestitle_facets', 'seriestitle');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (139, 'Accessibility', 'AC', 'accessibility', 'Accessibility', 'Accessibility', false, true, false, '', 'accessibility_facets', 'accessibility');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (140, 'Licensing', 'LN', 'licensing', 'Licensing', 'Licensing', false, true, false, '', 'licensing_facets', 'licensing');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (141, 'Course Title', 'CU', 'coursetitle', 'Course Title', 'Course Title', false, true, false, '', 'coursetitle_facets', 'coursetitle');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (142, 'Restriction Message', '  ', 'restricted_msg', 'Access Restriction', '', false, false, false, NULL, NULL, 'restricted_msg');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (143, 'Group Restrictions', '  ', 'group_restrictions', 'Group Restrictions', '', false, false, false, NULL, NULL, 'group_restrictions');
insert into SobekCM_Metadata_Types (MetadataTypeID, MetadataName, SobekCode, SolrCode, DisplayTerm, FacetTerm, CustomField, canFacetBrowse, DefaultAdvancedSearch, LegacySolrCode, SolrCode_Facets, SolrCode_Display) values (144, 'Instances', '  ', 'instance', 'Instances', '', false, false, false, NULL, NULL, 'instance');

insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (1, '.avi', 'video/x-msvideo', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (2, '.bmp', 'image/bmp', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (3, '.csv', 'application/octet-stream', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (4, '.doc', 'application/msword', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (5, '.docx', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (6, '.dtd', 'text/xml', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (7, '.fla', 'application/octet-stream', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (8, '.gif', 'image/gif', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (9, '.gtar', 'application/x-gtar', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (10, '.gz', 'application/x-gzip', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (11, '.htm', 'text/html', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (12, '.html', 'text/html', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (13, '.ico', 'image/x-icon', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (14, '.jpeg', 'image/jpeg', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (15, '.jpg', 'image/jpeg', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (16, '.js', 'application/x-javascript', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (17, '.mov', 'video/quicktime', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (18, '.movie', 'video/x-sgi-movie', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (19, '.mp2', 'video/mpeg', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (20, '.mp3', 'audio/mpeg', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (21, '.mpa', 'video/mpeg', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (22, '.mpe', 'video/mpeg', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (23, '.mpeg', 'video/mpeg', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (24, '.mpg', 'video/mpeg', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (25, '.mpp', 'application/vnd.ms-project', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (26, '.mpv2', 'video/mpeg', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (27, '.msi', 'application/octet-stream', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (28, '.pdf', 'application/pdf', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (29, '.pgm', 'image/x-portable-graymap', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (30, '.png', 'image/png', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (31, '.ppt', 'application/vnd.ms-powerpoint', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (32, '.pptx', 'application/vnd.openxmlformats-officedocument.presentationml.presentation', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (33, '.ra', 'audio/x-pn-realaudio', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (34, '.ram', 'audio/x-pn-realaudio', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (35, '.rm', 'application/vnd.rn-realmedia', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (36, '.sgml', 'text/sgml', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (37, '.swf', 'application/x-shockwave-flash', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (38, '.tar', 'application/x-tar', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (39, '.tif', 'image/tiff', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (40, '.tiff', 'image/tiff', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (41, '.txt', 'text/plain', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (42, '.vsd', 'application/vnd.visio', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (43, '.wav', 'audio/wav', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (44, '.wm', 'video/x-ms-wm', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (45, '.wma', 'audio/x-ms-wma', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (46, '.wmv', 'video/x-ms-wmv', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (47, '.xls', 'application/vnd.ms-excel', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (48, '.xlsx', 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (49, '.xml', 'text/xml', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (50, '.xsd', 'text/xml', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (51, '.xsf', 'text/xml', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (52, '.xsl', 'text/xml', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (53, '.xslt', 'text/xml', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (54, '.zip', 'application/x-zip-compressed', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (55, '.jp2', 'image/jp2', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (56, '.ogg', 'application/ogg', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (57, '.mp4', 'video/mpeg', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (58, '.ogm', 'application/ogg', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (59, '.m4a', 'audio/mpeg', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (60, '.m4v', 'video/mpeg', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (61, '.sql', 'text/plain', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (62, '.mkv', 'video/x-matroksa', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (63, '.webm', 'video/webm', false, true);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (64, '.mxf', 'application/mxf', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (65, '.mets', 'text/xml', false, false);
insert into SobekCM_Mime_Types (MimeTypeID, Extension, MimeType, isBlocked, shouldForward) values (66, '.archive', 'archive', true, false);

insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Ace Editor Theme', 'chrome', 'General Settings', 'UI Settings', false, 0, 'Set the theme for the Ace editor, used for CSS and Javascript editing, as well as TEI editing, if that plug-in is enabled.', '{ACE_THEMES}', 77, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Allow Mass Behavior Update', 'false', 'Digital Resource Settings', 'Online Management Settings', false, 0, 'Whether the administrative options to mass update the behaviors is available', 'true|false', 84, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Allow Page Image File Management', 'true', 'Deprecated', 'Deprecated', false, 0, 'Help for Allow Page Image File Management', 'true|false', 1, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Application Server Network', '', 'System / Server Settings', 'Server Settings', false, 2, 'Server share for the web application''s network location.\n\nExample: ''\\\\lib-sandbox\\Production\\''', NULL, 2, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Application Server URL', '', 'System / Server Settings', 'Server Settings', false, 2, 'Base URL which points to the web application.\n\nExamples: ''http://localhost/sobekcm/'', ''http://ufdc.ufl.edu/'', etc..', NULL, 3, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Archive DropBox', '', 'Builder', 'Archive Settings', false, 0, 'Network location for the archive drop box.  If this is set to a value, the builder/bulk loader will place a copy of the package in this folder for archiving purposes.  This folder is where any of your archiving processes should look for new packages.', NULL, 4, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Builder IIS Logs Directory', '', 'Builder', 'Builder Settings', false, 0, 'IIS web log location (usually a network share) for the builder to read the logs and add the usage statistics to the database.', NULL, 55, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Builder Last Message', '', 'Builder', 'Status', false, 0, 'Help for Builder Last Message', NULL, 56, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Builder Last Run Finished', '', 'Builder', 'Status', false, 0, 'Help for Builder Last Run Finished', NULL, 57, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Builder Log Expiration in Days', '10', 'Builder', 'Builder Settings', false, 0, 'Number of days the SobekCM Builder logs are retained.', '10|30|365|99999', 58, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Builder Operation Flag', 'STANDARD OPERATION', 'Builder', 'Status', false, 0, 'Last flag set when the builder/bulk loader ran.', 'STANDARD OPERATION|PAUSE REQUESTED|ABORT REQUESTED|NO BUILDER REQUESTED ', 5, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Builder Seconds Between Polls', '60', 'Builder', 'Builder Settings', false, 0, 'Number of seconds the builder remains idle before checking for new incoming package again.', '15|60|300|600', 6, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Builder Send Usage Emails', 'false', 'Builder', 'Builder Settings', false, 0, 'Flag indicates is usage emails should be sent automatically after the stats usage has been calculated and added to the database.', 'true|false', 59, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Builder Version', '', 'Builder', 'Status', false, 0, 'Help for Builder Version', NULL, 60, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Can Remove Single Search Term', 'true', 'General Settings', 'Search Settings', false, 0, 'When this is set to TRUE, users can remove a single search term from their current search.  Setting this to FALSE, makes the display slightly cleaner.', 'true|false', 7, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Can Submit Edit Online', 'true', 'Digital Resource Settings', 'Online Management Settings', false, 0, 'Flag dictates if users can submit items online, or if this is disabled in this system.', 'true|false', 8, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Can Submit Items Online', 'true', 'System / Server Settings', 'System Settings', false, 2, 'Flag dictates if users can submit items online, or if this is disabled in this system.', 'true|false', 61, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Convert Office Files to PDF', 'true', 'Builder', 'Builder Settings', false, 0, 'Flag dictates if users can submit items online, or if this is disabled in this system.', 'true|false', 9, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Create MARC Feed By Default', 'false', 'Builder', 'Builder Settings', false, 0, 'Flag indicates if the builder/bulk loader should create the MARC feed by default when operating in background mode.', 'true|false', 10, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Detailed User Permissions', 'false', 'System / Server Settings', 'System Settings', false, 2, 'Flag indicates if more refined user permissions can be assigned, such as if a user can edit behaviors of an item in a collection vs. a more general flag that says a RequestSpecificValues.Current_User can make all changes to an item in a collection.', 'true|false', 11, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Disable Standard User Logon Flag', 'false', 'System / Server Settings', 'System Settings', false, 2, 'Flag indicates if non system administrators are temporarily barred from logging on.', 'true|false', 62, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Disable Standard User Logon Message', '', 'System / Server Settings', 'System Settings', false, 2, 'Message displayed if non syste administrators are temporarily barred from logging on.', NULL, 63, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Document Solr Index URL', '', 'System / Server Settings', 'Search Preferences', false, 2, 'URL for the document-level solr index.\n\nExample: ''http://localhost:8983/solr/documents''', NULL, 12, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Email Default From Address', '', 'General Settings', 'Email Settings', false, 0, 'Email address that emails from this system should utilize', NULL, 64, '300');
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Email Default From Name', '', 'General Settings', 'Email Settings', false, 0, 'Display name to associate with emails sent from this system (otherwise the instance/portal name will be used)', NULL, 65, '300');
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Email Method', 'DATABASE MAIL', 'System / Server Settings', 'Email Setup', false, 2, 'Indicated whether the database mail system or the SMTP direct email system should be utilizied', 'DATABASE MAIL|SMTP DIRECT', 13, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Email On User Registration', '', 'General Settings', 'Email Settings', false, 0, 'If an email address is provided here, an email will be sent when each new user registers.\n\nIf you are using multiple email addresses, seperate them with a semi-colon.\n\nExample: ''person1@corp.edu;person2@corp.edu''', NULL, 78, '300');
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Email SMTP Port', '25', 'System / Server Settings', 'Email Setup', false, 2, 'If direct SMTP email sending is used, the port to utilize.  This must be numeric.', NULL, 66, '70');
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Email SMTP Server', '', 'System / Server Settings', 'Email Setup', false, 2, 'If direct SMTP email sending is used, the server name to send emails to.', NULL, 67, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Facets Collapsible', 'false', 'General Settings', 'Search Settings', false, 0, 'Flag determines if the facets are collapsible like an accordian, or if they all start fully expanded.', 'true|false', 14, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Files To Exclude From Downloads', '((.*?)\.(jpg|tif|jp2|jpx|bmp|jpeg|gif|png|txt|pro|mets|db|xml|bak|job)$|qc_error.html)', 'Digital Resource Settings', 'General Settings', false, 0, 'Regular expressions used to exclude files from being added by default to the downloads of resources.\n\nExample: ''((.*?)\\.(jpg|tif|jp2|jpx|bmp|jpeg|gif|png|txt|pro|mets|db|xml|bak|job)$|qc_error.html)''', NULL, 16, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Google Map API Key', '', 'System / Server Settings', 'System Settings', false, 2, 'Google Map API key for displaying geographic displays within this system.  Help is found at http://sobekrepository.org/software/config/googlemaps.', NULL, 17, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Help Metadata URL', 'http://sobekrepository.org/', 'General Settings', 'Help Settings', false, 0, 'URL used for the help pages when users request help on metadata elements during online submit and editing.\n\nExample (and default): ''http://sobekrepository.org/''', NULL, 18, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Help URL', 'http://sobekrepository.org/', 'General Settings', 'Help Settings', false, 0, 'URL used for the main help pages about this system''s basic functionality.\n\nExample (and default): ''http://sobekrepository.org/''', NULL, 19, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Image Server Network', '', 'System / Server Settings', 'Server Settings', false, 2, 'Network location to the content for all of the digital resources (images, metadata, etc.).\n\nExample: ''C:\\inetpub\\wwwroot\\UFDC Web\\SobekCM\\content\\'' or ''\\\\ufdc-images\\content\\''', NULL, 20, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Image Server URL', '', 'System / Server Settings', 'Server Settings', false, 2, 'URL which points to the digital resource images.\n\nExample: ''http://localhost/sobekcm/content/'' or ''http://ufdcimages.uflib.ufl.edu/''', NULL, 21, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Include Partners On System Home', 'false', 'General Settings', 'Instance Settings', false, 0, 'This option controls whether a PARTNERS option appears on the main system home page, assuming there are multiple institutional aggregations.', 'true|false', 22, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Include Result Count In Text', 'true', 'General Settings', 'Search Settings', false, 0, 'When this is set to TRUE, the result count will be displayed in the search explanation text ( i.e., Your search for ... resulted in 2 results ).  Setting this to FALSE will not show the final portion in that text.', 'true|false', 76, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Include TreeView On System Home', 'true', 'General Settings', 'Instance Settings', false, 0, 'This option controls whether a TREE VIEW option appears on the main system home page which displays all the active aggregations hierarchically in a tree view.', 'true|false', 23, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Instance Code', 'SOBEK', 'System / Server Settings', 'System Settings', false, 2, 'Instance code used for shared database or solr instances', NULL, 85, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('JPEG Height', '1000', 'Digital Resource Settings', 'Image Settings', false, 0, 'Restriction on the size of the jpeg page images'' height (in pixels) when generated automatically by the builder/bulk loader.\n\nDefault: ''1000''', NULL, 24, '60');
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('JPEG Width', '630', 'Digital Resource Settings', 'Image Settings', false, 0, 'Restriction on the size of the jpeg page images'' width (in pixels) when generated automatically by the builder/bulk loader.\n\nDefault: ''630''', NULL, 25, '60');
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('JPEG2000 Server', '', 'System / Server Settings', 'Server Settings', false, 2, 'URL for the Aware JPEG2000 Server for displaying and zooming into JPEG2000 images.', NULL, 26, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('JPEG2000 Server Type', 'Built-In IIPImage', 'System / Server Settings', 'Server Settings', false, 2, 'Type of the JPEG2000 server found at the URL above.', 'Built-In IIPImage|None', 27, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Kakadu JPEG2000 Create Command', '', 'Builder', 'Builder Settings', false, 0, 'Kakadu JPEG2000 script will override the specifications used when creating zoomable images.\n\nIf this is blank, the default specifications will be used which match those used by the National Digital Newspaper Program and University of Florida Digital Collections.', NULL, 68, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Main Builder Input Folder', '', 'Builder', 'Builder Settings', false, 0, 'This is the network location to the SobekCM Builder''s main incoming folder.\n\nThis is used by the SMaRT tool when doing bulk imports from spreadsheet or MARC records.', NULL, 28, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Manage GeoSpatial Data', 'false', 'Digital Resource Settings', 'Online Management Settings', false, 0, 'Whether the beta options to manage geo-spatial data will be displayed', 'true|false', 83, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('MARC Cataloging Source Code', '', 'Digital Resource Settings', 'Metadata Settings', false, 0, 'Cataloging source code for the 040 field, ( for example ''FUG'' for University of Florida )', NULL, 69, '60');
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('MARC Location Code', '', 'Digital Resource Settings', 'Metadata Settings', false, 0, 'Location code for the 852 |a - if none is given the system abbreviation will be used. Otherwise, the system abbreviation will be put in the 852 |b field.', NULL, 70, '60');
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('MARC Reproduction Agency', '', 'Digital Resource Settings', 'Metadata Settings', false, 0, 'Agency responsible for reproduction, or primary agency associated with the SobekCM instance ( for the added 533 |c field )\n\nThis 533 is not added for born digital items.', NULL, 71, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('MARC Reproduction Place', '', 'Digital Resource Settings', 'Metadata Settings', false, 0, 'Place of reproduction, or primary location associated with the SobekCM instance ( for the added 533 |b field ).\n\nThis 533 is not added for born digital items.', NULL, 72, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('MARC XSLT File', '', 'Digital Resource Settings', 'Metadata Settings', false, 0, 'XSLT file to use as a final transform, after the standard MarcXML file is written.\n\nThis only affects generated MarcXML ( for the feeds and OAI ) not the dispayed in-system MARC ( as of January 2015 ).  This file should appear in the config/users folder.', NULL, 73, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('MarcXML Feed Location', '', 'Builder', 'Builder Settings', false, 0, 'Network location or share where any geneated MarcXML feed should be written.\n\nExample: ''\\\\lib-sandbox\\Data\\''', NULL, 31, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('OCR Engine Command', '', 'Builder', 'Builder Settings', false, 0, 'If you wish to utilize an OCR engine in the builder/bulk loader, add the command-line call to the engine here.\n\nUse %1 as a place holder for the ingoing image file name and %2 as a placeholder for the output text file name.\n\nExample: ''C:\\OCR\\Engine.exe -in %1 -out %2''', NULL, 32, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Page Solr Index URL', '', 'System / Server Settings', 'Search Preferences', false, 2, 'URL for the resource-level solr index used when searching for matching pages within a single document.\n\nExample: ''http://localhost:8983/solr/pages''', NULL, 33, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('PostArchive Files To Delete', '(.*?)\.(tif|QC\.jpg)', 'Builder', 'Archive Settings', false, 0, 'Regular expression indicates which files should be deleted AFTER being archived by the builder/bulk loader.\n\nExample: ''(.*?)\\.(tif)''', NULL, 34, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Files To Omit From Archive', '(.*?)\.(QC.jpg)', 'Builder', 'Archive Settings', false, 0, 'Regular expression indicates which files should be deleted BEFORE being archived by the builder/bulk loader.\n\nExample: ''(.*?)\\.(QC.jpg)''', NULL, 35, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Privacy Email Address', '', 'General Settings', 'Email Settings', false, 0, 'Email address which receives notification if personal information (such as Social Security Numbers) is potentially found while loading or post-processing an item.\n\nIf you are using multiple email addresses, seperate them with a semi-colon.\n\nExample: ''person1@corp.edu;person2@corp.edu''', NULL, 36, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Search System', 'Solr 7', 'System / Server Settings', 'Search Preferences', false, 0, 'Which system and schema to use for searching - "Solr 7" uses the legacy Solr field names (safe for Solr 7 and older); "Solr 9+" uses the updated docValues-backed sort/group field names, which requires the current schema.xml and a full reindex.', 'Solr 7|Solr 9+', 79, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Send Email On Added Aggregation', 'Always', 'General Settings', 'Email Settings', false, 0, 'Flag indicates when emails should be sent after new item aggregations are added through the web interface.', 'Always|Never', 74, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Show Citation For Dark Items', 'true', 'Digital Resource Settings', 'Online Behavior', false, 0, 'Flag indicates if the citation is displayed online for DARK items.', 'true|false', 37, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Show Florida SUS Settings', 'false', 'Deprecated', 'Deprecated', false, 0, 'Some system settings are only applicable to institutions which are part of the Florida State University System.  Setting this value to TRUE will show these settings, while FALSE will suppress them.\n\nIf this value is changed, you willl need to save the settings for it to reload and reflect the change.', 'true|false', 38, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('SobekCM Web Server IP', '', 'System / Server Settings', 'Server Settings', false, 2, 'IP address for the web server running this web repository software.\n\nThis is used for setting restricted or dark material to only be available for the web server, which then acts as a proxy/web server to serve that content to authenticated users.', NULL, 39, '200');
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Static Pages Location', '', 'System / Server Settings', 'Caching Settings', false, 2, 'Location where the static files are located for providing the full citation and text for indexing, either on the same server as the web application or as a network share.\n\nIt is recommended that these files be on the same server as the web server, rather than remote storage, to increase the speed in which requests from search engine indexers can be fulfilled.\n\nExample: ''C:\\inetpub\\wwwroot\\UFDC Web\\SobekCM\\data\\''.', NULL, 41, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Static Resources Source', 'cdn secure', 'System / Server Settings', 'Server Settings', false, 2, 'Indicates the general source of all the static resources, such as javascript, system default stylesheets, images, and included libraries.\n\nUsing CDN will result in better performance, but can only be used when users will have access to the database.\n\nThis actually indicates which configuration file to read to determine the base location of the default resources.', '{STATIC_SOURCE_CODES}', 75, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Statistics Caching Enabled', 'false', 'System / Server Settings', 'Caching Settings', false, 2, 'Flag indicates if the basic usage and item count information should be cached for up to 24 hours as static XML files written in the web server''s temp directory.\n\nThis should be enabled if your library is quite large as it can take a fair amount of time to retrieve this information and these screens are made available for search engine index robots for indexing.', 'true|false', 42, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('System Base Abbreviation', 'SOBEK', 'General Settings', 'Instance Settings', false, 0, 'Base abbreviation to be used when the system refers to itself to the RequestSpecificValues.Current_User, such as the main tabs to take a user to the home pages.\n\nThis abbreviation should be kept as short as possible.\n\nExamples: ''UFDC'', ''dLOC'', ''Sobek'', etc..', NULL, 43, '100');
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('System Base Name', 'Sobek', 'General Settings', 'Instance Settings', false, 0, 'Overall name of the system, to be used when creating MARC records and in several other locations.', NULL, 44, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('System Base URL', '', 'System / Server Settings', 'Server Settings', false, 2, 'Base URL which points to the web application.\n\nExamples: ''http://localhost/sobekcm/'', ''http://ufdc.ufl.edu/'', etc..', NULL, 45, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('System Default Language', 'English', 'General Settings', 'Instance Settings', false, 0, 'Default system user interface language.  If the user''s HTML request does not include a language supported by the interface or which does not include specific translations for a field, this default language is utilized.', NULL, 46, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('System Email', '', 'General Settings', 'Email Settings', false, 0, 'Default email address for the system, which is sent emails when users opt to contact the administrators.\n\nThis can be changed for individual aggregations but at least one email is required for the overall system.\n\nIf you are using multiple email addresses, seperate them with a semi-colon.\n\nExample: ''person1@corp.edu;person2@corp.edu''', NULL, 47, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('System Error Email', '', 'General Settings', 'Email Settings', false, 0, 'Email address used when a critical system error occurs which may require investigation or correction.\n\nIf you are using multiple email addresses, seperate them with a semi-colon.\n\nExample: ''person1@corp.edu;person2@corp.edu''', NULL, 48, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Thumbnail Height', '300', 'Digital Resource Settings', 'Image Settings', false, 0, 'Restriction on the size of the page image thumbnails'' height (in pixels) when generated automatically by the builder/bulk loader.\n\nDefault: ''300''', NULL, 49, '60');
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Thumbnail Width', '150', 'Digital Resource Settings', 'Image Settings', false, 0, 'Restriction on the size of the page image thumbnails'' width (in pixels) when generated automatically by the builder/bulk loader.\n\nDefault: ''150''', NULL, 50, '60');
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Upload File Types', '.aif,.aifc,.aiff,.au,.avi,.bz2,.c,.c++,.css,.csv,.dbf,.ddl,.doc,.docx,.dtd,.dvi,.epub,.flac,.gz,.htm,.html,.java,.jps,.js,.m4a,.m4p,.mid,.midi,.mkv,.mp2,.mp3,.mp4,.mpg,.odp,.ogg,.ogm,.pdf,.pgm,.ppt,.pptx,.ps,.ra,.ram,.rar,.rm,.rtf,.sgml,.swf,.sxi,.tbz2,.tgz,.vtt,.wav,.wave,.webm,.wma,.wmv,.xls,.xlsx,.xml,.zip', 'Digital Resource Settings', 'Online Management Settings', false, 0, 'List of non-image extensions which are allowed to be uploaded into a digital resource.\n\nList should be the extensions, with the period, separated by commas.\n\nExample: .aif,.aifc,.aiff,.au,.avi,.bz2,.c,.c++,.css,.dbf,.ddl,...', NULL, 51, '600|3');
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Upload Image Types', '.txt,.tif,.jpg,.jp2,.pro', 'Digital Resource Settings', 'Online Management Settings', false, 0, 'List of page image extensions which are allowed to be uploaded into a digital resource to display as page images.\n\nList should be the extensions, with the period, separated by commas.\n\nExample: .txt,.tif,.jpg,.jp2,.pro', NULL, 52, '600');
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Use Tracking Sheet', 'false', 'Digital Resource Settings', 'Online Management Settings', false, 0, 'Whether the administrative options to use the tracking sheet will be displayed', 'true|false', 82, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Web In Process Submission Location', '', 'System / Server Settings', 'Server Settings', false, 2, 'Location where packages are built by users during online submissions and metadata updates.\n\nThis generally needs to be on the web server and have appropriate access for read/write.\n\nIf nothing is indicated in this field, the system will automatically use the ''mySobek\\InProcess'' subfolder under the web application.', NULL, 53, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options, SettingID, Dimensions) values ('Web Output Caching Minutes', '1', 'System / Server Settings', 'Caching Settings', false, 2, 'This setting controls how long the client''s browser is instructed to cache the served web page.\n\nSetting this value higher removes the round-trip when requesting a recently requested page.  It also means that some changes may not be reflected until the refresh button is pressed.\n\nIn general, this setting is only applied to public-style pages, and not personalized pages, such as the bookshelf views.', '0|1|2|3|5|10|15', 54, NULL);
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options) values ( 'Enable OpenTelemetry', 'false', 'System / Server Settings', 'Server Settings', false, 2, 'Flag indicates whether OpenTelemetry instrumentation (tracing) should be enabled for this instance.  The OTLP collector endpoint itself is configured separately, in appsettings.json.', 'true|false' );
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options) values ( 'Solr Username', '', 'System / Server Settings', 'Search Preferences', false, 2, 'Username for HTTP Basic Authentication against the Solr document and page indexes, if the Solr instance requires it.  Leave blank if Solr does not require authentication.', NULL );
insert into SobekCM_Settings (Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options) values ( 'Solr Password', '', 'System / Server Settings', 'Search Preferences', false, 2, 'Password for HTTP Basic Authentication against the Solr document and page indexes, if the Solr instance requires it.  Leave blank if Solr does not require authentication.', NULL );

insert into Tracking_Disposition_Type (DispositionID, DispositionFuture, DispositionPast, DispositionNotes) values (1, 'Return', 'Returned', 'Returned material to collection manager, or original requestor');
insert into Tracking_Disposition_Type (DispositionID, DispositionFuture, DispositionPast, DispositionNotes) values (2, 'Request Withdraw', 'Requested Withdraw', 'Sent to cataloging to request a withdraw');
insert into Tracking_Disposition_Type (DispositionID, DispositionFuture, DispositionPast, DispositionNotes) values (3, 'Discard', 'Discarded', 'Returned material to collection manager, or original requestor');
insert into Tracking_Disposition_Type (DispositionID, DispositionFuture, DispositionPast, DispositionNotes) values (4, 'No Physical Copy', 'No Physical Copy', 'There is no physical copy to be disposed of here.');

insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (1, 'Record Created', 'A record for this item was created', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (3, 'Scanning', 'Some portion of this item was scanned', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (4, 'PreQC', 'This item was prepared for Quality Control', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (5, 'QC Accept', 'Quality control was performed on this item', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (6, 'OCR', 'OCR was performed on this item', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (9, 'UFDC New', 'This item was loaded into UFDC as a new item', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (10, 'UFDC Replacement', 'This item was loaded into UFDC as a replacement', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (11, 'Metadata Update', 'A metadata update was applied by the SobekCM Bulk Loader', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (22, 'FDA Error', 'FDA was unable to load the item', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (23, 'FDA Ingest', 'FDA ingested the item', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (28, 'Archived to Tivoli', 'Files saved into CNS Tivoli backup solution', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (29, 'Online Submit', 'Item was submitted via the online interface', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (30, 'Online Edit', 'Metadata was edited for this item online', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (31, 'QC Reject', 'Rejected during quality control', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (34, 'Made Public', 'Item was switched to PUBLIC visibility', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (35, 'Made Private', 'Item was switched to PRIVATE visibility', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (36, 'Made Restricted', 'Item was switch to some IP RESTRICTED visibility', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (37, 'Digitization Requested', 'Digitization of this item was requested by an individual or organization', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (38, 'OCLC Number Added', 'New OCLC number provided for this item', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (39, 'Image Processing', 'Post-acquisition image processing ( copyright blur, cropping, color managment, etc.. )', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (40, 'Bulk Loaded', 'Loaded into SobekCM through the bulk loader', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (41, 'QC Preliminary', 'Preliminary QC performed, but neither rejected nor finalized', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (42, 'Material Received', 'Physical material received into the digitization location', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (43, 'Material Disposition', 'Physical material handled post-digitization', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (44, 'Post-Processed', 'Bulk Loader performed post-loading processes for derivative creation, thumbnails, etc..', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (45, 'Updated Pages/Divisions', 'Using the online QC tool, updated the page names, divisions, page order, etc..', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (46, 'Uploaded Page Images', 'Uploaded new page images for the item', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (47, 'Updated Coordinates', 'Used the online map edit feature to add or edit coordinates associated with this item', NULL, NULL, NULL, NULL, NULL);
insert into Tracking_WorkFlow (WorkFlowID, WorkFlowName, WorkFlowNotes, Start_Event_Number, End_Event_Number, Start_And_End_Event_Number, Start_Event_Desc, End_Event_Desc) values (48, 'Managed Downloads', 'Managed the download files for this item', NULL, NULL, NULL, NULL, NULL);


/** Setup default skin, portal, and collection **/
insert into SobekCM_Web_Skin ( WebSkinCode, OverrideHeaderFooter, OverrideBanner, BannerLink, BaseWebSkin, Notes )
values ( 'SAMPLE', true, false, '', '', 'Sample sobekcm web skin' );

insert into SobekCM_Portal_URL ( Base_URL, isActive, isDefault, Abbreviation, Name )
values ( '', 'true', 'true', 'Demo', 'Default demo SobekCM library portal' );

insert into SobekCM_Portal_Web_Skin_Link (PortalID, WebSkinID, isDefault) values (1, 1, 'true');

insert into SobekCM_Item_Aggregation (Code, Name, ShortName, Description, ThematicHeadingID, Type, isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, OAI_Metadata, ContactEmail, HasNewItems, DefaultInterface, Items_Can_Be_Described, LastItemAdded, External_Link, DateAdded, Can_Browse_Items )
values ( 'ALL', 'All Collection Groups', 'Search all Groups', '', -1, 'Collection Group', true, false, '', 0, 0, false, '', '', 'false', '', 1, '1900-01-01', '', '1900-01-01', false );

insert into mySobek_User_Group ( UserGroupID, GroupName, GroupDescription, Can_Submit_Items, Internal_User, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms, autoAssignUsers, Can_Delete_All_Items, IsSobekDefault, IsShibbolethDefault, IsLdapDefault, IsSpecialGroup )
values ( -1, 'Everyone', 'Default everyone group within the SobekCM system', 'false', 'false', 'false', 'false', 'false', 'true', 'false', 'false', 'false', 'false', 'true' );


-- Insert values to the version table
insert into SobekCM_Database_Version ( Major_Version, Minor_Version, Release_Phase )
values ( 5, 0, '0' );
