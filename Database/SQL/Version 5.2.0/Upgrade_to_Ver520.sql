/**************************************************************************/
/**                                                                      **/
/**   Source: MARK_20260827_Add_Item_Type_Schema.sql                     **/
/**                                                                      **/
/**************************************************************************/

-- 5.2.0: New "Item Type" concept for the workflow-based metadata submission redesign.
--
-- An Item Type is an admin-authored bundle of: whether the multivolume/series finder
-- shows before metadata, an ordered set of metadata blocks, type-specific entry widgets
-- (map footprint, map pin, etc. -- upload form shape now lives entirely here, not as a
-- separate enum), a MARC/MODS resource-type mapping, default metadata values stamped
-- onto new items, and who can select it.
--
-- Default visibility and the permissions agreement are deliberately NOT Type properties
-- -- they are set by an admin per submitting user (or user group) at the point submit
-- rights are granted, independent of which Type that user later picks. See the
-- "Permissions Agreements" and mySobek_User sections below.
--
-- Metadata block content is stored directly in the DB (SobekCM_Metadata_Block.BlockXml),
-- not linked out to a file -- but the XML shape and how it's parsed are unchanged, reusing
-- the existing Template_XML_Reader / Element_Factory machinery (see
-- SobekCM_Library/Citation/Template), just reading a string instead of a file path. This
-- is a storage-location change only: no UI to author new blocks or edit which fields
-- appear inside one exists yet, and that stays out of scope for 5.2.0, planned for 6.0.
--
-- The legacy SobekCM_Item_Group.Type varchar column (the MODS/MARC-ish classification:
-- Aerial, Archival, Book, Newspaper, ...) is left completely untouched -- it is too
-- embedded elsewhere to change. SobekCM_Item_Type.MARC_TypeOfResource instead specifies
-- what value a Type stamps into that legacy column, so existing code reading it keeps
-- working unchanged.
--
-- Storage level: an item's Type is set at the SobekCM_Item_Group (BibID/series) level
-- by default; SobekCM_Item (the individual VID) carries its own nullable override column,
-- where NULL means "inherit the group's Type."
--
-- Portals are deliberately NOT part of this schema -- a Type is not portal-scoped.


-- =====================================================================================
-- SobekCM_Item_Type -- the Type definition itself
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_Item_Type](
	[TypeID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](150) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[IsSystemType] [bit] NOT NULL DEFAULT 0,		-- system-shipped Types (Book, Map, Newspaper...) can only be disabled, never hard-deleted; custom institution Types (0) can be
	[ShowSeriesFinder] [bit] NOT NULL,				-- generalizes the Newspaper-only "attach to an existing title" branch to any Type that wants it
	[IncludeUserAsAuthor] [bit] NOT NULL,
	[DefaultCreateOcrFromMasters] [bit] NOT NULL DEFAULT 0,	-- default state of the upload screen's OCR toggle for this Type (e.g. off for Photograph, on for Book/Newspaper) -- a single flag for now; a real per-Type default-processing mechanism (more flags, or its own table) is likely needed later, same "still needs more thought" bucket as default metadata
	[BibIDRoot] [varchar](10) NULL,
	[MARC_TypeOfResource] [varchar](30) NULL,		-- value stamped into the legacy SobekCM_Item_Group.Type column for new items of this Type
	[HelpUrl] [varchar](500) NULL,
	[UploadCode] [varchar](50) NULL,				-- resolved by Upload_Step_Factory to the iUploadSubmissionStep implementation this Type's Upload screen uses (e.g. 'ORALHISTORY'); NULL/blank/unrecognized all fall back to the generic multi-file upload step
	[Enabled] [bit] NOT NULL,
	[IconCode] [varchar](50) NULL,
	[DateCreated] [datetime] NOT NULL,
	[LastModified] [datetime] NOT NULL,
 CONSTRAINT [PK_SobekCM_Item_Type] PRIMARY KEY CLUSTERED ([TypeID] ASC)
);
GO


-- =====================================================================================
-- SobekCM_Metadata_Block -- registry for metadata blocks. BlockXml holds the block's
-- actual XML content directly (same Template_Page/Template_Panel/element shape
-- Template_XML_Reader already parses -- it just needs a small change to read from a
-- string instead of requiring a filesystem path). This deliberately fixes the
-- hardcoded-file-path problem the current template system has, even though nothing
-- yet lets an admin EDIT which fields appear inside that XML -- that stays 6.0 scope;
-- storing the content in a DB column is a storage change, not an editing-UI change.
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_Metadata_Block](
	[BlockID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](150) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[Category] [varchar](50) NULL,					-- grouping for the future 6.0 block-builder UI
	[BlockXml] [nvarchar](max) NOT NULL,
	[Enabled] [bit] NOT NULL,
 CONSTRAINT [PK_SobekCM_Metadata_Block] PRIMARY KEY CLUSTERED ([BlockID] ASC)
);
GO


-- =====================================================================================
-- SobekCM_Item_Type_Block -- ordered Type -> Block bundle
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_Item_Type_Block](
	[TypeID] [int] NOT NULL,
	[BlockID] [int] NOT NULL,
	[SortOrder] [int] NOT NULL,
	[IsRemovable] [bit] NOT NULL,					-- false locks the block in (e.g. Title/Creator), so "add a block" on the metadata screen only ever touches optional ones
 CONSTRAINT [PK_SobekCM_Item_Type_Block] PRIMARY KEY CLUSTERED ([TypeID] ASC, [BlockID] ASC),
 CONSTRAINT [FK_SobekCM_Item_Type_Block_Type] FOREIGN KEY([TypeID]) REFERENCES [dbo].[SobekCM_Item_Type] ([TypeID]),
 CONSTRAINT [FK_SobekCM_Item_Type_Block_Block] FOREIGN KEY([BlockID]) REFERENCES [dbo].[SobekCM_Metadata_Block] ([BlockID])
);
GO


-- =====================================================================================
-- SobekCM_Item_Type_Widget -- bespoke, non-block UI a Type can add to a screen
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_Item_Type_Widget](
	[TypeID] [int] NOT NULL,
	[WidgetCode] [varchar](50) NOT NULL,			-- e.g. 'MAP_FOOTPRINT', 'MAP_POINT'
	[ScreenPlacement] [varchar](20) NOT NULL,		-- 'upload', 'metadata', or 'confirm'
	[SortOrder] [int] NOT NULL,
 CONSTRAINT [PK_SobekCM_Item_Type_Widget] PRIMARY KEY CLUSTERED ([TypeID] ASC, [WidgetCode] ASC),
 CONSTRAINT [FK_SobekCM_Item_Type_Widget_Type] FOREIGN KEY([TypeID]) REFERENCES [dbo].[SobekCM_Item_Type] ([TypeID])
);
GO


-- =====================================================================================
-- SobekCM_Item_Type_Default_Metadata -- constants stamped onto new items of this Type
-- (relocates the XML <constants> section of the current template system into the DB, per Type)
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_Item_Type_Default_Metadata](
	[DefaultMetadataID] [int] IDENTITY(1,1) NOT NULL,
	[TypeID] [int] NOT NULL,
	[ElementCode] [varchar](50) NOT NULL,			-- matches an abstract_Element type string, e.g. 'Source', 'Acquisition'
	[Value] [nvarchar](max) NOT NULL,
	[SortOrder] [int] NOT NULL,
 CONSTRAINT [PK_SobekCM_Item_Type_Default_Metadata] PRIMARY KEY CLUSTERED ([DefaultMetadataID] ASC),
 CONSTRAINT [FK_SobekCM_Item_Type_Default_Metadata_Type] FOREIGN KEY([TypeID]) REFERENCES [dbo].[SobekCM_Item_Type] ([TypeID])
);
GO


-- =====================================================================================
-- SobekCM_Item_Type_Assignment -- Type <-> User/Group, replaces the fixed-5-slot
-- mySobek_User_Template_Link / mySobek_User_Group_Template_Link pattern.
--
-- IMPORTANT -- this is an allowlist that defaults OPEN, the inverse of how template
-- assignment behaves today: a user with zero rows here can select every enabled Type
-- unrestricted. The moment a user (or a group they belong to) has even one row, they
-- are restricted to just their assigned Types. Application logic, not enforced by the
-- schema itself.
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_Item_Type_Assignment](
	[AssignmentID] [int] IDENTITY(1,1) NOT NULL,
	[TypeID] [int] NOT NULL,
	[UserID] [int] NULL,
	[UserGroupID] [int] NULL,
	[SortOrder] [int] NOT NULL,
 CONSTRAINT [PK_SobekCM_Item_Type_Assignment] PRIMARY KEY CLUSTERED ([AssignmentID] ASC),
 CONSTRAINT [CK_SobekCM_Item_Type_Assignment_ExactlyOne] CHECK (
	([UserID] IS NOT NULL AND [UserGroupID] IS NULL) OR
	([UserID] IS NULL AND [UserGroupID] IS NOT NULL)
 ),
 CONSTRAINT [FK_SobekCM_Item_Type_Assignment_Type] FOREIGN KEY([TypeID]) REFERENCES [dbo].[SobekCM_Item_Type] ([TypeID]),
 CONSTRAINT [FK_SobekCM_Item_Type_Assignment_User] FOREIGN KEY([UserID]) REFERENCES [dbo].[mySobek_User] ([UserID]),
 CONSTRAINT [FK_SobekCM_Item_Type_Assignment_UserGroup] FOREIGN KEY([UserGroupID]) REFERENCES [dbo].[mySobek_User_Group] ([UserGroupID])
);
GO

CREATE UNIQUE INDEX [UQ_SobekCM_Item_Type_Assignment_User] ON [dbo].[SobekCM_Item_Type_Assignment] ([TypeID], [UserID]) WHERE [UserID] IS NOT NULL;
GO

CREATE UNIQUE INDEX [UQ_SobekCM_Item_Type_Assignment_UserGroup] ON [dbo].[SobekCM_Item_Type_Assignment] ([TypeID], [UserGroupID]) WHERE [UserGroupID] IS NOT NULL;
GO


-- =====================================================================================
-- SobekCM_Item_Type_Aggregation_Link -- optional shortcut only, not a restriction.
-- Lets a permissioned user see e.g. "Add a new Map" directly on a collection's home
-- page or internal header, pre-selecting the Type and skipping the Type-grid screen.
-- A Type with no rows here is still reachable from the full Type grid.
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_Item_Type_Aggregation_Link](
	[TypeID] [int] NOT NULL,
	[AggregationID] [int] NOT NULL,
	[SortOrder] [int] NOT NULL,
 CONSTRAINT [PK_SobekCM_Item_Type_Aggregation_Link] PRIMARY KEY CLUSTERED ([TypeID] ASC, [AggregationID] ASC),
 CONSTRAINT [FK_SobekCM_Item_Type_Aggregation_Link_Type] FOREIGN KEY([TypeID]) REFERENCES [dbo].[SobekCM_Item_Type] ([TypeID]),
 CONSTRAINT [FK_SobekCM_Item_Type_Aggregation_Link_Aggregation] FOREIGN KEY([AggregationID]) REFERENCES [dbo].[SobekCM_Item_Aggregation] ([AggregationID])
);
GO


-- =====================================================================================
-- SobekCM_User_Type_Block_Preference -- the metadata-screen "add a block" scope choice:
-- (a) just this item needs no row here at all -- it is only that one item's stored
--     block list;
-- (b) "for all Oral Histories" is a row with TypeID set;
-- (c) "always" is a row with TypeID NULL (applies regardless of the chosen Type).
-- An admin promoting a preference institution-wide does not touch this table -- it is
-- a plain admin action that inserts a permanent row into SobekCM_Item_Type_Block instead.
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_User_Type_Block_Preference](
	[PreferenceID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[TypeID] [int] NULL,
	[BlockID] [int] NOT NULL,
 CONSTRAINT [PK_SobekCM_User_Type_Block_Preference] PRIMARY KEY CLUSTERED ([PreferenceID] ASC),
 CONSTRAINT [FK_SobekCM_User_Type_Block_Preference_User] FOREIGN KEY([UserID]) REFERENCES [dbo].[mySobek_User] ([UserID]),
 CONSTRAINT [FK_SobekCM_User_Type_Block_Preference_Type] FOREIGN KEY([TypeID]) REFERENCES [dbo].[SobekCM_Item_Type] ([TypeID]),
 CONSTRAINT [FK_SobekCM_User_Type_Block_Preference_Block] FOREIGN KEY([BlockID]) REFERENCES [dbo].[SobekCM_Metadata_Block] ([BlockID])
);
GO

CREATE UNIQUE INDEX [UQ_SobekCM_User_Type_Block_Preference_Typed] ON [dbo].[SobekCM_User_Type_Block_Preference] ([UserID], [TypeID], [BlockID]) WHERE [TypeID] IS NOT NULL;
GO

CREATE UNIQUE INDEX [UQ_SobekCM_User_Type_Block_Preference_Always] ON [dbo].[SobekCM_User_Type_Block_Preference] ([UserID], [BlockID]) WHERE [TypeID] IS NULL;
GO


-- =====================================================================================
-- New ItemTypeID columns: group carries the default, item carries an optional override.
-- Neither touches the existing SobekCM_Item_Group.Type varchar column.
-- =====================================================================================
ALTER TABLE [dbo].[SobekCM_Item_Group] ADD [ItemTypeID] [int] NULL;
GO
ALTER TABLE [dbo].[SobekCM_Item_Group] WITH CHECK ADD CONSTRAINT [FK_SobekCM_Item_Group_ItemType] FOREIGN KEY([ItemTypeID]) REFERENCES [dbo].[SobekCM_Item_Type] ([TypeID]);
GO

ALTER TABLE [dbo].[SobekCM_Item] ADD [ItemTypeID] [int] NULL;
GO
ALTER TABLE [dbo].[SobekCM_Item] WITH CHECK ADD CONSTRAINT [FK_SobekCM_Item_ItemType] FOREIGN KEY([ItemTypeID]) REFERENCES [dbo].[SobekCM_Item_Type] ([TypeID]);
GO


-- =====================================================================================
-- SobekCM_Permissions_Agreement -- permissions agreements are now DB objects (name +
-- text), so more than one can exist and an admin can assign a specific one per user.
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_Permissions_Agreement](
	[AgreementID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](150) NOT NULL,
	[AgreementText] [nvarchar](max) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[DateCreated] [datetime] NOT NULL,
	[LastModified] [datetime] NOT NULL,
 CONSTRAINT [PK_SobekCM_Permissions_Agreement] PRIMARY KEY CLUSTERED ([AgreementID] ASC)
);
GO


-- =====================================================================================
-- SobekCM_User_Permissions_Agreement_Acceptance -- records a user's semi-permanent
-- agreement. AgreementText is a FROZEN COPY of the agreement's text at the moment of
-- acceptance -- deliberately duplicated rather than joined live, so that if an admin
-- later edits SobekCM_Permissions_Agreement.AgreementText, this row still shows exactly
-- what the user actually agreed to. A user is asked again only if the agreement
-- assigned to them (mySobek_User.PermissionsAgreementID) changes to a different
-- AgreementID -- editing the text of their existing agreement does not re-prompt them.
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_User_Permissions_Agreement_Acceptance](
	[AcceptanceID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[AgreementID] [int] NOT NULL,
	[AgreementName] [nvarchar](150) NOT NULL,		-- snapshot, in case the agreement is later renamed or deleted
	[AgreementText] [nvarchar](max) NOT NULL,		-- snapshot of the text the user actually agreed to
	[AcceptedDate] [datetime] NOT NULL,
 CONSTRAINT [PK_SobekCM_User_Permissions_Agreement_Acceptance] PRIMARY KEY CLUSTERED ([AcceptanceID] ASC),
 CONSTRAINT [FK_SobekCM_User_Permissions_Agreement_Acceptance_User] FOREIGN KEY([UserID]) REFERENCES [dbo].[mySobek_User] ([UserID]),
 CONSTRAINT [FK_SobekCM_User_Permissions_Agreement_Acceptance_Agreement] FOREIGN KEY([AgreementID]) REFERENCES [dbo].[SobekCM_Permissions_Agreement] ([AgreementID])
);
GO

CREATE UNIQUE INDEX [UQ_SobekCM_User_Permissions_Agreement_Acceptance] ON [dbo].[SobekCM_User_Permissions_Agreement_Acceptance] ([UserID], [AgreementID]);
GO


-- =====================================================================================
-- mySobek_User -- new admin-set columns, assigned when submit rights are granted.
-- DefaultVisibility mirrors the old CompleteTemplate.Default_Visibility convention
-- (-1 = Private, 0 = Public). PermissionsAgreementID is nullable -- NULL means this user
-- is not required to accept any agreement before submitting.
-- =====================================================================================
ALTER TABLE [dbo].[mySobek_User] ADD [DefaultVisibility] [smallint] NULL;
GO
ALTER TABLE [dbo].[mySobek_User] ADD [PermissionsAgreementID] [int] NULL;
GO
ALTER TABLE [dbo].[mySobek_User] WITH CHECK ADD CONSTRAINT [FK_mySobek_User_PermissionsAgreement] FOREIGN KEY([PermissionsAgreementID]) REFERENCES [dbo].[SobekCM_Permissions_Agreement] ([AgreementID]);
GO


-- =====================================================================================
-- mySobek_User_Group -- the same two columns, mirrored, so a group's "Submissions" tab
-- can set defaults that get stamped onto new members provisioned under that group,
-- the same way other submission-related settings already work group-first in this system.
-- =====================================================================================
ALTER TABLE [dbo].[mySobek_User_Group] ADD [DefaultVisibility] [smallint] NULL;
GO
ALTER TABLE [dbo].[mySobek_User_Group] ADD [PermissionsAgreementID] [int] NULL;
GO
ALTER TABLE [dbo].[mySobek_User_Group] WITH CHECK ADD CONSTRAINT [FK_mySobek_User_Group_PermissionsAgreement] FOREIGN KEY([PermissionsAgreementID]) REFERENCES [dbo].[SobekCM_Permissions_Agreement] ([AgreementID]);
GO


-- Type-grid sort order is a per-user PREFERENCE, not a Type property (SortOrder removed
-- from SobekCM_Item_Type above) -- stored as an ordinary row in the existing generic
-- mySobek_User_Settings (UserID, Setting_Key, Setting_Value) table, no new table needed:
--   Setting_Key   = 'TypeGridSortOrder'
--   Setting_Value = 'Alphabetical' | 'MostUsedRepository' | 'MostUsedByUser'
--
-- No new tracking table for either "Most Used" mode -- both are live counts, not a
-- maintained counter (nothing to increment, nothing that can drift):
--   'MostUsedRepository' -- COUNT(*) GROUP BY ItemTypeID straight off SobekCM_Item_Group
--   'MostUsedByUser'     -- the same, but joined through the user's existing "Submitted
--                           Items" folder: mySobek_User_Folder (UserID, FolderName =
--                           'Submitted Items') -> mySobek_User_Item (UserFolderID ->
--                           ItemID) -> SobekCM_Item.ItemTypeID. This is the live,
--                           already-wired mechanism (see New_Group_And_Item_MySobekViewer.cs,
--                           which sets My_Sobek_SubMode = "Submitted Items" right after a
--                           successful submission) -- NOT mySobek_User_Item_Link, which has
--                           zero references anywhere in the current C# codebase.


-- CRUD stored procedures for the admin Type-builder UI, the Permissions Agreements
-- admin screen, and the acceptance check/write are intentionally not part of this
-- script -- they will follow once each UI's exact needs are worked out.


/**************************************************************************/
/**                                                                      **/
/**   Source: MARK_20260827_Add_Permission_Agreement_Procs.sql           **/
/**                                                                      **/
/**************************************************************************/

-- 5.2.0: CRUD stored procedures for the new Permissions Agreements admin screen
-- (SobekCM_Permissions_Agreement, added in MARK_20260827_Add_Item_Type_Schema.sql).
--
-- Agreements are retired (Enabled = 0), never hard-deleted -- so there is deliberately
-- no delete procedure here. SobekCM_Permissions_Agreement_Edit is a single upsert proc,
-- following the same "-1 = new row" convention as SobekCM_Edit_Thematic_Heading /
-- SobekCM_Save_Icon / SobekCM_Builder_Incoming_Folder_Edit elsewhere in this codebase.


-- Returns one row per agreement for the Mgmt list screen, with the counts that screen
-- displays computed inline rather than requiring the C# layer to run separate queries.
CREATE PROCEDURE [dbo].[SobekCM_Permissions_Agreement_Get_List]
AS
BEGIN

	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	SELECT
		a.AgreementID,
		a.[Name],
		a.Enabled,
		a.DateCreated,
		a.LastModified,
		( SELECT COUNT(*) FROM mySobek_User WHERE PermissionsAgreementID = a.AgreementID ) AS AssignedUserCount,
		( SELECT COUNT(*) FROM mySobek_User_Group WHERE PermissionsAgreementID = a.AgreementID ) AS AssignedGroupCount,
		( SELECT COUNT(*) FROM SobekCM_User_Permissions_Agreement_Acceptance WHERE AgreementID = a.AgreementID ) AS AcceptedCount
	FROM SobekCM_Permissions_Agreement a
	ORDER BY a.[Name];

END;
GO


-- Returns the single agreement row for the Single edit screen.
CREATE PROCEDURE [dbo].[SobekCM_Permissions_Agreement_Get_Single]
	@AgreementID int
AS
BEGIN

	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	SELECT AgreementID, [Name], AgreementText, Enabled, DateCreated, LastModified
	FROM SobekCM_Permissions_Agreement
	WHERE AgreementID = @AgreementID;

END;
GO


-- Upsert: @AgreementID = -1 inserts a new agreement (DateCreated stamped here), any other
-- value updates the existing row (LastModified always stamped here). Editing an existing
-- agreement's text does NOT touch SobekCM_User_Permissions_Agreement_Acceptance -- those
-- rows keep their own frozen snapshot regardless of what happens here.
CREATE PROCEDURE [dbo].[SobekCM_Permissions_Agreement_Edit]
	@AgreementID int,
	@Name nvarchar(150),
	@AgreementText nvarchar(max),
	@Enabled bit
AS
BEGIN

	IF ( @AgreementID = -1 )
	BEGIN
		INSERT INTO SobekCM_Permissions_Agreement ( [Name], AgreementText, Enabled, DateCreated, LastModified )
		VALUES ( @Name, @AgreementText, @Enabled, GETDATE(), GETDATE() );
	END
	ELSE
	BEGIN
		UPDATE SobekCM_Permissions_Agreement
		SET [Name] = @Name, AgreementText = @AgreementText, Enabled = @Enabled, LastModified = GETDATE()
		WHERE AgreementID = @AgreementID;
	END;

END;
GO


/**************************************************************************/
/**                                                                      **/
/**   Source: MARK_20260827_Add_User_Submission_Fields.sql               **/
/**                                                                      **/
/**************************************************************************/

-- 5.2.0: wires up the two new columns added to mySobek_User / mySobek_User_Group back in
-- MARK_20260827_Add_Item_Type_Schema.sql (DefaultVisibility, PermissionsAgreementID), and adds
-- CRUD for SobekCM_Item_Type_Assignment (the Item-Type restriction allowlist) -- both needed by
-- the new "Submissions" tab on the User and User Group admin editors.
--
-- Item_Type_Assignment CRUD deliberately does NOT follow the fixed-slot-per-call convention used
-- by mySobek_Add_User_Templates_Link / Update_SobekCM_User_Group_Aggregations elsewhere in this
-- codebase (a hardcoded number of slots per stored-proc call, batched from C#) -- that is exactly
-- the pattern the Item_Type_Assignment table was designed to move away from. Instead this uses the
-- same simple "single link, call in a loop" idiom already used by mySobek_Link_User_To_User_Group:
-- one clear-all proc plus one add-one-link proc per side (user/group), called from a loop in C#.


-- =====================================================================================
-- mySobek_Get_User_By_UserID -- adds DefaultVisibility/PermissionsAgreementID to the
-- existing first result set (basic user info), and DROPS the "Get the templates"/"Get
-- the default metadata" result sets entirely -- the old per-user Templates/Default
-- Metadata Sets assignment concept is being removed in 5.2.0, superseded by Item Type
-- assignment (SobekCM_Item_Type_Assignment, above). Everything else is reproduced
-- unchanged from the current definition.
--
-- NOTE: an earlier draft of this same ALTER (still on this branch, never run against a
-- live database) accidentally dropped five trailing result sets -- aggregations,
-- folders, non-submitted bookshelf items, user groups, user settings -- plus the
-- LastActivity update, when only the Templates/DefaultMetadata sets should ever have
-- been touched. Restored here from the real baseline definition (Ver501_DB_Complete.sql)
-- since this ALTER supersedes that draft outright, not layered on top of it.
-- =====================================================================================
ALTER PROCEDURE [dbo].[mySobek_Get_User_By_UserID]
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
	  AuthenticationSource,
	  DefaultVisibility, PermissionsAgreementID
	from mySobek_User U
	where ( UserID = @userid ) and ( isActive = 'true' );

	-- Get the bib id's of items submitted
	select distinct(G.BibID)
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


-- =====================================================================================
-- mySobek_Update_User -- adds @default_visibility/@permissions_agreement_id, persisted
-- alongside the other permission-flag columns this proc already updates. Also drops
-- @clear_projects_templates and the two DELETEs it drove (mySobek_User_DefaultMetadata_Link/
-- mySobek_User_Template_Link) -- the per-user Templates/Default Metadata Sets concept those
-- tables backed is being removed in 5.2.0, superseded by Item Type assignment.
-- =====================================================================================
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
	@clear_aggregation_links bit,
	@clear_user_groups bit,
	@default_visibility smallint = NULL,
	@permissions_agreement_id int = NULL
AS
begin transaction

	-- Update the simple table values
	update mySobek_User
	set Can_Submit_Items=@can_submit, Internal_User=@is_internal,
		IsPortalAdmin=@is_portal_admin, IsSystemAdmin=@is_system_admin,
		Include_Tracking_Standard_Forms=@include_tracking_standard_forms,
		EditTemplate=@edit_template, Can_Delete_All_Items = @can_delete_all,
		EditTemplateMarc=@edit_template_marc, IsHostAdmin=@is_host_admin,
		IsUserAdmin=@is_user_admin,
		DefaultVisibility=@default_visibility, PermissionsAgreementID=@permissions_agreement_id
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

	-- Clear the aggregation links
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


-- =====================================================================================
-- mySobek_Save_User_Group -- adds @default_visibility/@permissions_agreement_id, persisted
-- on both the insert and update branches. Also drops @clear_metadata_templates and the two
-- DELETEs it drove (mySobek_User_Group_DefaultMetadata_Link/mySobek_User_Group_Template_Link)
-- -- the per-group Templates/Default Metadata Sets concept those tables backed is being
-- removed in 5.2.0, superseded by Item Type assignment.
-- =====================================================================================
ALTER PROCEDURE [dbo].[mySobek_Save_User_Group]
	@usergroupid int,
	@groupname nvarchar(150),
	@groupdescription nvarchar(1000),
	@can_submit_items bit,
	@is_internal bit,
	@can_edit_all bit,
	@is_system_admin bit,
	@is_portal_admin bit,
	@include_tracking_standard_forms bit,
	@clear_aggregation_links bit,
	@clear_editable_links bit,
	@is_sobek_default bit,
	@is_shibboleth_default bit,
	@is_ldap_default bit,
	@new_usergroupid int output,
	@default_visibility smallint = NULL,
	@permissions_agreement_id int = NULL
AS
begin

	-- Is there a user group id provided
	if ( @usergroupid < 0 )
	begin
		-- Insert as a new user group
		insert into mySobek_User_Group ( GroupName, GroupDescription, Can_Submit_Items, Internal_User, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms, IsSobekDefault, IsShibbolethDefault, IsLdapDefault, DefaultVisibility, PermissionsAgreementID )
		values ( @groupname, @groupdescription, @can_submit_items, @is_internal, @is_system_admin, @is_portal_admin, @include_tracking_standard_forms, @is_sobek_default, @is_shibboleth_default, @is_ldap_default, @default_visibility, @permissions_agreement_id );

		-- Return the new primary key
		set @new_usergroupid = @@IDENTITY;
	end
	else
	begin
		-- Update, if it exists
		update mySobek_User_Group
		set GroupName = @groupname, GroupDescription = @groupdescription, Can_Submit_Items = @can_submit_items, Internal_User=@is_internal, IsSystemAdmin=@is_system_admin, IsPortalAdmin=@is_portal_admin, Include_Tracking_Standard_Forms=@include_tracking_standard_forms,
			IsSobekDefault=@is_sobek_default, IsShibbolethDefault=@is_shibboleth_default, IsLdapDefault=@is_ldap_default,
			DefaultVisibility=@default_visibility, PermissionsAgreementID=@permissions_agreement_id
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


-- =====================================================================================
-- SobekCM_Item_Type_Assignment CRUD -- deliberately simple: one clear-all proc plus one
-- add-one-link proc per side (user/group), matching mySobek_Link_User_To_User_Group's
-- existing "single link, call in a loop from C#" idiom, NOT the fixed-slot-per-call
-- convention used elsewhere for Templates/Aggregations.
-- =====================================================================================

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Assignment_Get_For_User]
	@UserID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	select TypeID from SobekCM_Item_Type_Assignment where UserID = @UserID order by SortOrder;
END;
GO

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Assignment_Get_For_Group]
	@UserGroupID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	select TypeID from SobekCM_Item_Type_Assignment where UserGroupID = @UserGroupID order by SortOrder;
END;
GO

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Assignment_Clear_For_User]
	@UserID int
AS
BEGIN
	delete from SobekCM_Item_Type_Assignment where UserID = @UserID;
END;
GO

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Assignment_Clear_For_Group]
	@UserGroupID int
AS
BEGIN
	delete from SobekCM_Item_Type_Assignment where UserGroupID = @UserGroupID;
END;
GO

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Assignment_Add_For_User]
	@UserID int,
	@TypeID int,
	@SortOrder int
AS
BEGIN
	if ( ( select count(*) from SobekCM_Item_Type_Assignment where UserID = @UserID and TypeID = @TypeID ) = 0 )
	begin
		insert into SobekCM_Item_Type_Assignment ( TypeID, UserID, UserGroupID, SortOrder )
		values ( @TypeID, @UserID, NULL, @SortOrder );
	end;
END;
GO

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Assignment_Add_For_Group]
	@UserGroupID int,
	@TypeID int,
	@SortOrder int
AS
BEGIN
	if ( ( select count(*) from SobekCM_Item_Type_Assignment where UserGroupID = @UserGroupID and TypeID = @TypeID ) = 0 )
	begin
		insert into SobekCM_Item_Type_Assignment ( TypeID, UserID, UserGroupID, SortOrder )
		values ( @TypeID, NULL, @UserGroupID, @SortOrder );
	end;
END;
GO


-- =====================================================================================
-- SobekCM_Item_Type_Get_List -- minimal read used only to populate the Submissions tab's
-- Item Type restriction checklist. NOT the full Item Type admin CRUD (that screen and its
-- procs -- add/edit a Type, its blocks, widgets, default metadata -- are separate,
-- not-yet-built work; this is just enough to check a box next to a Type's name).
-- =====================================================================================
CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Get_List]
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	select TypeID, [Name], Enabled from SobekCM_Item_Type order by [Name];
END;
GO


/**************************************************************************/
/**                                                                      **/
/**   Source: MARK_20260828_Add_Item_Type_Blocks_Widgets_Access_Procs.sql **/
/**                                                                      **/
/**************************************************************************/

-- 5.2.0: CRUD for the Item Type "Blocks, Widgets & Access" tab -- the second tab on
-- Item_Type_Single_AdminViewer, covering SobekCM_Item_Type_Block, SobekCM_Item_Type_Widget,
-- SobekCM_Item_Type_Default_Metadata, and the Type-side view of SobekCM_Item_Type_Assignment
-- and SobekCM_Item_Type_Aggregation_Link (both already exist from
-- MARK_20260827_Add_Item_Type_Schema.sql; only the reverse "given a TypeID" procs are new here
-- -- the existing Assignment procs are all keyed by UserID/UserGroupID for the Submissions tab).
--
-- Reordering is a simple adjacent-row SortOrder swap (one admin action = one postback = one
-- swap), not drag-and-drop -- matches this codebase's plain-postback admin screen style
-- elsewhere; no JS reorder library is introduced.


-- =====================================================================================
-- SobekCM_Item_Type_Block
-- =====================================================================================

-- Returns every block currently bundled into a Type, joined to the block registry for
-- display, ordered by SortOrder.
CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Block_Get_For_Type]
	@TypeID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	SELECT tb.TypeID, tb.BlockID, b.[Name], b.Description, b.Category, b.BlockXml, tb.SortOrder, tb.IsRemovable
	FROM SobekCM_Item_Type_Block tb
		INNER JOIN SobekCM_Metadata_Block b ON b.BlockID = tb.BlockID
	WHERE tb.TypeID = @TypeID
	ORDER BY tb.SortOrder;
END;
GO

-- Adds a block to a Type, at the next SortOrder. No-ops (rather than erroring) if the
-- block is already on this Type.
CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Block_Add]
	@TypeID int,
	@BlockID int,
	@IsRemovable bit
AS
BEGIN
	IF ( ( SELECT COUNT(*) FROM SobekCM_Item_Type_Block WHERE TypeID = @TypeID AND BlockID = @BlockID ) = 0 )
	BEGIN
		DECLARE @NextSort int;
		SELECT @NextSort = ISNULL(MAX(SortOrder), 0) + 1 FROM SobekCM_Item_Type_Block WHERE TypeID = @TypeID;

		INSERT INTO SobekCM_Item_Type_Block ( TypeID, BlockID, SortOrder, IsRemovable )
		VALUES ( @TypeID, @BlockID, @NextSort, @IsRemovable );
	END;
END;
GO

-- Removes a block from a Type.
CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Block_Remove]
	@TypeID int,
	@BlockID int
AS
BEGIN
	DELETE FROM SobekCM_Item_Type_Block WHERE TypeID = @TypeID AND BlockID = @BlockID;
END;
GO

-- Flips the "locked" state of a block already on a Type (IsRemovable = 0 means locked --
-- the submitter cannot remove it on the metadata screen).
CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Block_Set_Removable]
	@TypeID int,
	@BlockID int,
	@IsRemovable bit
AS
BEGIN
	UPDATE SobekCM_Item_Type_Block SET IsRemovable = @IsRemovable WHERE TypeID = @TypeID AND BlockID = @BlockID;
END;
GO

-- Swaps this block's SortOrder with its neighbor in the given direction ('up' or 'down').
-- A no-op at either end of the list.
CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Block_Move]
	@TypeID int,
	@BlockID int,
	@Direction varchar(4)
AS
BEGIN
	DECLARE @ThisSort int, @OtherBlockID int, @OtherSort int;
	SELECT @ThisSort = SortOrder FROM SobekCM_Item_Type_Block WHERE TypeID = @TypeID AND BlockID = @BlockID;

	IF ( @Direction = 'up' )
		SELECT TOP 1 @OtherBlockID = BlockID, @OtherSort = SortOrder FROM SobekCM_Item_Type_Block WHERE TypeID = @TypeID AND SortOrder < @ThisSort ORDER BY SortOrder DESC;
	ELSE
		SELECT TOP 1 @OtherBlockID = BlockID, @OtherSort = SortOrder FROM SobekCM_Item_Type_Block WHERE TypeID = @TypeID AND SortOrder > @ThisSort ORDER BY SortOrder ASC;

	IF ( @OtherBlockID IS NOT NULL )
	BEGIN
		UPDATE SobekCM_Item_Type_Block SET SortOrder = @OtherSort WHERE TypeID = @TypeID AND BlockID = @BlockID;
		UPDATE SobekCM_Item_Type_Block SET SortOrder = @ThisSort WHERE TypeID = @TypeID AND BlockID = @OtherBlockID;
	END;
END;
GO


-- =====================================================================================
-- SobekCM_Item_Type_Widget
-- =====================================================================================

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Widget_Get_For_Type]
	@TypeID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	SELECT TypeID, WidgetCode, ScreenPlacement, SortOrder
	FROM SobekCM_Item_Type_Widget
	WHERE TypeID = @TypeID
	ORDER BY SortOrder;
END;
GO

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Widget_Add]
	@TypeID int,
	@WidgetCode varchar(50),
	@ScreenPlacement varchar(20)
AS
BEGIN
	IF ( ( SELECT COUNT(*) FROM SobekCM_Item_Type_Widget WHERE TypeID = @TypeID AND WidgetCode = @WidgetCode ) = 0 )
	BEGIN
		DECLARE @NextSort int;
		SELECT @NextSort = ISNULL(MAX(SortOrder), 0) + 1 FROM SobekCM_Item_Type_Widget WHERE TypeID = @TypeID;

		INSERT INTO SobekCM_Item_Type_Widget ( TypeID, WidgetCode, ScreenPlacement, SortOrder )
		VALUES ( @TypeID, @WidgetCode, @ScreenPlacement, @NextSort );
	END;
END;
GO

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Widget_Remove]
	@TypeID int,
	@WidgetCode varchar(50)
AS
BEGIN
	DELETE FROM SobekCM_Item_Type_Widget WHERE TypeID = @TypeID AND WidgetCode = @WidgetCode;
END;
GO


-- =====================================================================================
-- SobekCM_Item_Type_Default_Metadata
-- =====================================================================================

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Default_Metadata_Get_For_Type]
	@TypeID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	SELECT DefaultMetadataID, TypeID, ElementCode, [Value], SortOrder
	FROM SobekCM_Item_Type_Default_Metadata
	WHERE TypeID = @TypeID
	ORDER BY SortOrder;
END;
GO

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Default_Metadata_Add]
	@TypeID int,
	@ElementCode varchar(50),
	@Value nvarchar(max)
AS
BEGIN
	DECLARE @NextSort int;
	SELECT @NextSort = ISNULL(MAX(SortOrder), 0) + 1 FROM SobekCM_Item_Type_Default_Metadata WHERE TypeID = @TypeID;

	INSERT INTO SobekCM_Item_Type_Default_Metadata ( TypeID, ElementCode, [Value], SortOrder )
	VALUES ( @TypeID, @ElementCode, @Value, @NextSort );
END;
GO

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Default_Metadata_Remove]
	@DefaultMetadataID int
AS
BEGIN
	DELETE FROM SobekCM_Item_Type_Default_Metadata WHERE DefaultMetadataID = @DefaultMetadataID;
END;
GO


-- =====================================================================================
-- SobekCM_Item_Type_Assignment -- Type-side view. The existing
-- SobekCM_Item_Type_Assignment_Get_For_User/_Get_For_Group/_Add_For_User/_Add_For_Group/
-- _Clear_For_User/_Clear_For_Group procs (MARK_20260827_Add_User_Submission_Fields.sql)
-- stay exactly as they are -- they're the Submissions tab's per-user/group side and are
-- reused as-is for adding a row from this screen too. Only the reverse listing and a
-- single-row remove (as opposed to _Clear_For_User/Group's clear-everything) are new.
-- =====================================================================================

-- Returns every user/group assigned to a Type, with a display name resolved for whichever
-- side is set (exactly one of UserID/UserGroupID per the table's own CHECK constraint).
CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Assignment_Get_For_Type]
	@TypeID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	SELECT
		a.AssignmentID,
		a.UserID,
		a.UserGroupID,
		DisplayName = CASE
			WHEN a.UserID IS NOT NULL THEN COALESCE(NULLIF(u.NickName, ''), u.FirstName) + ' ' + u.LastName + ' (' + u.UserName + ')'
			ELSE g.GroupName
		END,
		IsGroup = CASE WHEN a.UserGroupID IS NOT NULL THEN 1 ELSE 0 END,
		a.SortOrder
	FROM SobekCM_Item_Type_Assignment a
		LEFT OUTER JOIN mySobek_User u ON u.UserID = a.UserID
		LEFT OUTER JOIN mySobek_User_Group g ON g.UserGroupID = a.UserGroupID
	WHERE a.TypeID = @TypeID
	ORDER BY IsGroup, DisplayName;
END;
GO

-- Removes a single assignment row by its own primary key -- unlike
-- SobekCM_Item_Type_Assignment_Clear_For_User/_Clear_For_Group (which clear every Type
-- restriction for one user/group), this only removes one Type's restriction on one
-- user/group, leaving any of their other Type restrictions untouched.
CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Assignment_Remove_By_ID]
	@AssignmentID int
AS
BEGIN
	DELETE FROM SobekCM_Item_Type_Assignment WHERE AssignmentID = @AssignmentID;
END;
GO


-- =====================================================================================
-- SobekCM_Item_Type_Aggregation_Link
-- =====================================================================================

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Aggregation_Link_Get_For_Type]
	@TypeID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	SELECT l.TypeID, l.AggregationID, a.Code, a.[Name], l.SortOrder
	FROM SobekCM_Item_Type_Aggregation_Link l
		INNER JOIN SobekCM_Item_Aggregation a ON a.AggregationID = l.AggregationID
	WHERE l.TypeID = @TypeID
	ORDER BY l.SortOrder;
END;
GO

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Aggregation_Link_Add]
	@TypeID int,
	@AggregationID int
AS
BEGIN
	IF ( ( SELECT COUNT(*) FROM SobekCM_Item_Type_Aggregation_Link WHERE TypeID = @TypeID AND AggregationID = @AggregationID ) = 0 )
	BEGIN
		DECLARE @NextSort int;
		SELECT @NextSort = ISNULL(MAX(SortOrder), 0) + 1 FROM SobekCM_Item_Type_Aggregation_Link WHERE TypeID = @TypeID;

		INSERT INTO SobekCM_Item_Type_Aggregation_Link ( TypeID, AggregationID, SortOrder )
		VALUES ( @TypeID, @AggregationID, @NextSort );
	END;
END;
GO

CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Aggregation_Link_Remove]
	@TypeID int,
	@AggregationID int
AS
BEGIN
	DELETE FROM SobekCM_Item_Type_Aggregation_Link WHERE TypeID = @TypeID AND AggregationID = @AggregationID;
END;
GO


/**************************************************************************/
/**                                                                      **/
/**   Source: MARK_20260828_Add_Item_Type_Procs.sql                      **/
/**                                                                      **/
/**************************************************************************/

-- 5.2.0: CRUD stored procedures for the new Item Types admin screen (SobekCM_Item_Type).
--
-- First pass covers only the Details fields (Name, Description, IsSystemType, ShowSeriesFinder,
-- IncludeUserAsAuthor, DefaultCreateOcrFromMasters, BibIDRoot, MARC_TypeOfResource, HelpUrl,
-- Enabled, IconCode) -- the Blocks/Widgets/Access screen from the wireframe is deliberately NOT
-- part of this pass, since it needs a Metadata Block registry admin screen (add/edit a block's
-- BlockXml) that doesn't exist yet either. SobekCM_Item_Type_Get_List (returning just
-- TypeID/Name/Enabled, added in MARK_20260827_Add_User_Submission_Fields.sql for the Submissions
-- tab's checklist) is untouched -- this file adds a separate, richer list proc for the admin
-- screen itself.
--
-- Unlike Permissions Agreements, a Type CAN be hard-deleted -- but only a custom (IsSystemType = 0)
-- one; standard/system-shipped Types can only be disabled. SobekCM_Item_Type_Delete enforces this
-- itself (silently no-ops for a system Type, rather than trusting the caller to have already
-- checked); a Type still referenced by SobekCM_Item_Group/SobekCM_Item is additionally protected by
-- the existing FK constraints, which will simply fail the DELETE -- the C# layer catches that like
-- any other DB exception (same pattern as Wordmarks' Delete_Icon and its "still referenced" case).


-- Richer list for the admin Mgmt screen: includes counts the wireframe's list view showed
-- (assigned users/groups, blocks -- though block counting has nothing to count against yet
-- since no block-to-type linking UI exists, so BlockCount is always 0 for now).
CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Get_Mgmt_List]
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select
		t.TypeID,
		t.[Name],
		t.IsSystemType,
		t.Enabled,
		( select COUNT(*) from SobekCM_Item_Type_Block where TypeID = t.TypeID ) as BlockCount,
		( select COUNT(*) from SobekCM_Item_Type_Assignment where TypeID = t.TypeID and UserID is not null ) as AssignedUserCount,
		( select COUNT(*) from SobekCM_Item_Type_Assignment where TypeID = t.TypeID and UserGroupID is not null ) as AssignedGroupCount
	from SobekCM_Item_Type t
	order by t.[Name];
END;
GO


-- Full single-row read, for the edit screen.
CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Get_Single]
	@TypeID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select TypeID, [Name], [Description], IsSystemType, ShowSeriesFinder, IncludeUserAsAuthor,
		DefaultCreateOcrFromMasters, BibIDRoot, MARC_TypeOfResource, HelpUrl, UploadCode, Enabled, IconCode,
		DateCreated, LastModified
	from SobekCM_Item_Type
	where TypeID = @TypeID;
END;
GO


-- Upsert: @TypeID = -1 inserts a new Type (DateCreated stamped here, IsSystemType always 0 for a
-- newly admin-created Type -- only pre-seeded standard Types are ever system Types), any other
-- value updates the existing row (LastModified always stamped here, IsSystemType left alone since
-- it's not editable after creation).
CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Edit]
	@TypeID int,
	@Name nvarchar(150),
	@Description nvarchar(500),
	@ShowSeriesFinder bit,
	@IncludeUserAsAuthor bit,
	@DefaultCreateOcrFromMasters bit,
	@BibIDRoot varchar(10),
	@MARC_TypeOfResource varchar(30),
	@HelpUrl varchar(500),
	@UploadCode varchar(50),
	@Enabled bit,
	@IconCode varchar(50)
AS
BEGIN

	IF ( @TypeID = -1 )
	BEGIN
		INSERT INTO SobekCM_Item_Type
			( [Name], [Description], IsSystemType, ShowSeriesFinder, IncludeUserAsAuthor, DefaultCreateOcrFromMasters, BibIDRoot, MARC_TypeOfResource, HelpUrl, UploadCode, Enabled, IconCode, DateCreated, LastModified )
		VALUES
			( @Name, @Description, 0, @ShowSeriesFinder, @IncludeUserAsAuthor, @DefaultCreateOcrFromMasters, @BibIDRoot, @MARC_TypeOfResource, @HelpUrl, @UploadCode, @Enabled, @IconCode, GETDATE(), GETDATE() );
	END
	ELSE
	BEGIN
		UPDATE SobekCM_Item_Type
		SET [Name] = @Name, [Description] = @Description, ShowSeriesFinder = @ShowSeriesFinder, IncludeUserAsAuthor = @IncludeUserAsAuthor,
			DefaultCreateOcrFromMasters = @DefaultCreateOcrFromMasters, BibIDRoot = @BibIDRoot, MARC_TypeOfResource = @MARC_TypeOfResource,
			HelpUrl = @HelpUrl, UploadCode = @UploadCode, Enabled = @Enabled, IconCode = @IconCode, LastModified = GETDATE()
		WHERE TypeID = @TypeID;
	END;

END;
GO


-- Only ever deletes a custom (IsSystemType = 0) Type; silently no-ops for a system Type rather
-- than trusting the caller to have already checked. Still subject to the FK constraints on
-- SobekCM_Item_Group.ItemTypeID / SobekCM_Item.ItemTypeID -- a Type already in use by real items
-- will fail this DELETE, which the C# layer surfaces as a plain "unable to delete" result.
CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Delete]
	@TypeID int
AS
BEGIN
	DELETE FROM SobekCM_Item_Type WHERE TypeID = @TypeID AND IsSystemType = 0;
END;
GO


/**************************************************************************/
/**                                                                      **/
/**   Source: MARK_20260828_Add_Metadata_Block_Procs.sql                 **/
/**                                                                      **/
/**************************************************************************/

-- 5.2.0: CRUD stored procedures for the new Metadata Block registry admin screen
-- (SobekCM_Metadata_Block, added in MARK_20260827_Add_Item_Type_Schema.sql).
--
-- Deliberately basic -- this is the "just list/edit with a big XML textbox" first pass
-- Mark asked for, not the block-authoring UI (a form that lets an admin add/remove
-- individual fields inside a block) that stays out of scope until 6.0 per the original
-- schema comment. BlockXml is edited as raw text here.
--
-- No delete: SobekCM_Metadata_Block has no IsSystemType-style distinction between
-- "built-in" and "custom" blocks the way SobekCM_Item_Type does, and blocks are
-- referenced by SobekCM_Item_Type_Block, so this follows the same disable-only
-- convention as Permissions Agreements rather than Item Types' allow-delete-if-custom.


-- Returns one row per block for the Mgmt list screen, with the count of Item Types
-- currently using it computed inline.
CREATE PROCEDURE [dbo].[SobekCM_Metadata_Block_Get_Mgmt_List]
AS
BEGIN

	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	SELECT
		b.BlockID,
		b.[Name],
		b.Description,
		b.Category,
		b.Enabled,
		( SELECT COUNT(*) FROM SobekCM_Item_Type_Block WHERE BlockID = b.BlockID ) AS AssignedTypeCount
	FROM SobekCM_Metadata_Block b
	ORDER BY b.[Name];

END;
GO


-- Returns the single block row (including the full BlockXml) for the Single edit screen.
CREATE PROCEDURE [dbo].[SobekCM_Metadata_Block_Get_Single]
	@BlockID int
AS
BEGIN

	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	SELECT BlockID, [Name], Description, Category, BlockXml, Enabled
	FROM SobekCM_Metadata_Block
	WHERE BlockID = @BlockID;

END;
GO


-- Upsert: @BlockID = -1 inserts a new block, any other value updates the existing row.
CREATE PROCEDURE [dbo].[SobekCM_Metadata_Block_Edit]
	@BlockID int,
	@Name nvarchar(150),
	@Description nvarchar(500),
	@Category varchar(50),
	@BlockXml nvarchar(max),
	@Enabled bit
AS
BEGIN

	IF ( @BlockID = -1 )
	BEGIN
		INSERT INTO SobekCM_Metadata_Block ( [Name], Description, Category, BlockXml, Enabled )
		VALUES ( @Name, @Description, @Category, @BlockXml, @Enabled );
	END
	ELSE
	BEGIN
		UPDATE SobekCM_Metadata_Block
		SET [Name] = @Name, Description = @Description, Category = @Category, BlockXml = @BlockXml, Enabled = @Enabled
		WHERE BlockID = @BlockID;
	END;

END;
GO


/**************************************************************************/
/**                                                                      **/
/**   Source: MARK_20260828_Add_Permission_Agreement_Roster_Proc.sql     **/
/**                                                                      **/
/**************************************************************************/

-- 5.2.0: adds the read-only Acceptance Roster screen for a single Permissions Agreement
-- (SobekCM.Library.AdminViewer.Permission_Agreement_Roster_AdminViewer) -- who has accepted
-- it, when, and (since acceptance rows are a frozen snapshot per
-- MARK_20260827_Add_Item_Type_Schema.sql's SobekCM_User_Permissions_Agreement_Acceptance
-- comment) whether the agreement's current wording has since changed underneath them.

-- Returns one row per user who has accepted the given agreement. IsEarlierWording flags a
-- row whose frozen AgreementText no longer matches the agreement's current text -- the
-- admin screen uses this to show an "EARLIER WORDING" badge and the frozen text itself.
CREATE PROCEDURE [dbo].[SobekCM_Permissions_Agreement_Get_Acceptances]
	@AgreementID int
AS
BEGIN

	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	SELECT
		acc.AcceptanceID,
		acc.UserID,
		FullName = LTRIM(RTRIM(COALESCE(NULLIF(u.NickName, ''), u.FirstName) + ' ' + u.LastName)),
		acc.AgreementText,
		acc.AcceptedDate,
		IsEarlierWording = CASE WHEN acc.AgreementText <> a.AgreementText THEN 1 ELSE 0 END
	FROM SobekCM_User_Permissions_Agreement_Acceptance acc
		INNER JOIN mySobek_User u ON u.UserID = acc.UserID
		INNER JOIN SobekCM_Permissions_Agreement a ON a.AgreementID = acc.AgreementID
	WHERE acc.AgreementID = @AgreementID
	ORDER BY acc.AcceptedDate DESC;

END;
GO


/**************************************************************************/
/**                                                                      **/
/**   Source: MARK_20260828_Add_Submission_Flow_Procs.sql                **/
/**                                                                      **/
/**************************************************************************/

-- 5.2.0: stored procedures for the first three real screens of the new Type-driven submission wizard
-- (SobekCM.Library.MySobekViewer.Submission.Steps.Permissions_SubmissionStep / TypeSelection_SubmissionStep
-- / SeriesFinder_SubmissionStep).


-- =====================================================================================
-- SobekCM_Item_Type_Get_For_Submission -- the Type grid's data source. Deliberately NOT the
-- same as SobekCM_Item_Type_Get_List (the minimal read backing the Submissions tab's checklist)
-- or SobekCM_Item_Type_Get_Mgmt_List (the admin list, with counts a submitter has no use for) --
-- this one applies the allowlist-defaults-open rule live: a user with zero
-- SobekCM_Item_Type_Assignment rows (through themselves or any group they belong to) sees every
-- enabled Type; the moment they have even one row (directly or via a group), they see only the
-- Types assigned to them.
-- =====================================================================================
CREATE PROCEDURE [dbo].[SobekCM_Item_Type_Get_For_Submission]
	@UserID int
AS
BEGIN

	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	DECLARE @HasRestriction bit = 0;
	IF ( EXISTS (
		SELECT 1
		FROM SobekCM_Item_Type_Assignment a
		WHERE a.UserID = @UserID
		   OR a.UserGroupID IN ( SELECT UserGroupID FROM mySobek_User_Group_Link WHERE UserID = @UserID )
	) )
		SET @HasRestriction = 1;

	IF ( @HasRestriction = 0 )
	BEGIN
		SELECT TypeID, [Name], [Description], IconCode, ShowSeriesFinder, UploadCode, BibIDRoot, MARC_TypeOfResource
		FROM SobekCM_Item_Type
		WHERE Enabled = 1
		ORDER BY [Name];
	END
	ELSE
	BEGIN
		SELECT DISTINCT t.TypeID, t.[Name], t.[Description], t.IconCode, t.ShowSeriesFinder, t.UploadCode, t.BibIDRoot, t.MARC_TypeOfResource
		FROM SobekCM_Item_Type t
			INNER JOIN SobekCM_Item_Type_Assignment a ON a.TypeID = t.TypeID
		WHERE t.Enabled = 1
		  AND ( a.UserID = @UserID
		     OR a.UserGroupID IN ( SELECT UserGroupID FROM mySobek_User_Group_Link WHERE UserID = @UserID ) )
		ORDER BY t.[Name];
	END;

END;
GO


-- =====================================================================================
-- SobekCM_Item_Group_Search_For_Submission -- the Series Finder step's search. No existing
-- "search titles by name" mechanism exists anywhere in this codebase to reuse (confirmed by
-- direct investigation) -- the old Newspaper/MultiVolume flows only ever attached to an
-- ALREADY-KNOWN BibID (reached by navigating from an existing item's page), never searched for
-- one by title. Matches on either the modern SobekCM_Item_Group.ItemTypeID (new groups, once
-- retyped) or the legacy Type varchar column via this Type's MARC_TypeOfResource (existing,
-- never-retyped groups) -- @MarcTypeOfResource may be NULL/blank, in which case only the
-- ItemTypeID match applies.
-- =====================================================================================
CREATE PROCEDURE [dbo].[SobekCM_Item_Group_Search_For_Submission]
	@SearchText nvarchar(250),
	@TypeID int,
	@MarcTypeOfResource varchar(30)
AS
BEGIN

	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	SELECT TOP 25
		g.BibID,
		g.GroupTitle,
		( SELECT COUNT(*) FROM SobekCM_Item i WHERE i.GroupID = g.GroupID ) AS ItemCount
	FROM SobekCM_Item_Group g
	WHERE g.GroupTitle LIKE '%' + @SearchText + '%'
	  AND (
	         g.ItemTypeID = @TypeID
	      OR ( g.ItemTypeID IS NULL AND @MarcTypeOfResource IS NOT NULL AND g.[Type] = @MarcTypeOfResource )
	      )
	ORDER BY g.GroupTitle;

END;
GO


-- =====================================================================================
-- SobekCM_Permissions_Agreement_Has_Accepted -- whether this user has already accepted this
-- exact agreement in a prior submission, so a returning user is never asked twice for the same
-- AgreementID (only re-prompted if the agreement assigned to them changes to a different one).
-- =====================================================================================
CREATE PROCEDURE [dbo].[SobekCM_Permissions_Agreement_Has_Accepted]
	@UserID int,
	@AgreementID int
AS
BEGIN

	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	SELECT COUNT(*) AS AcceptedCount
	FROM SobekCM_User_Permissions_Agreement_Acceptance
	WHERE UserID = @UserID AND AgreementID = @AgreementID;

END;
GO


-- =====================================================================================
-- SobekCM_Permissions_Agreement_Record_Acceptance -- the first write path to
-- SobekCM_User_Permissions_Agreement_Acceptance anywhere in this codebase. Freezes a copy of the
-- agreement's current Name/AgreementText at the moment of acceptance, per that table's original
-- design comment (MARK_20260827_Add_Item_Type_Schema.sql) -- a later edit to the live agreement's
-- text does not change what this row shows. A no-op (not an error) if this exact (UserID,
-- AgreementID) pair was already recorded, matching the table's unique index.
-- =====================================================================================
CREATE PROCEDURE [dbo].[SobekCM_Permissions_Agreement_Record_Acceptance]
	@UserID int,
	@AgreementID int
AS
BEGIN

	IF ( ( SELECT COUNT(*) FROM SobekCM_User_Permissions_Agreement_Acceptance WHERE UserID = @UserID AND AgreementID = @AgreementID ) = 0 )
	BEGIN
		DECLARE @Name nvarchar(150), @Text nvarchar(max);
		SELECT @Name = [Name], @Text = AgreementText FROM SobekCM_Permissions_Agreement WHERE AgreementID = @AgreementID;

		INSERT INTO SobekCM_User_Permissions_Agreement_Acceptance ( UserID, AgreementID, AgreementName, AgreementText, AcceptedDate )
		VALUES ( @UserID, @AgreementID, @Name, @Text, GETDATE() );
	END;

END;
GO


/**************************************************************************/
/**                                                                      **/
/**   Source: MARK_20260828_Remove_Template_DefaultMetadata_System.sql   **/
/**                                                                      **/
/**************************************************************************/

-- 5.2.0: removes the legacy per-user/per-group "Templates" and "Default Metadata Sets"
-- assignment system entirely -- the concept of restricting which submittal Templates and
-- Default Metadata (project) codes a user/group could pick from. Fully superseded by the
-- Item Type system (SobekCM_Item_Type + SobekCM_Item_Type_Assignment, see
-- MARK_20260827_Add_Item_Type_Schema.sql / MARK_20260827_Add_User_Submission_Fields.sql).
--
-- This does NOT touch mySobek_DefaultMetadata itself (the master "project" registry, still
-- actively managed by Default_Metadata_AdminViewer / mySobek_Save_DefaultMetadata /
-- mySobek_Delete_DefaultMetadata) -- only the per-user/group LINK tables that assigned
-- restricted subsets of it, plus the Templates side in full (mySobek_Template has no
-- surviving admin screen or any other consumer once its two link tables are gone).
--
-- Removed from the live submission flow (New_Group_And_Item_MySobekViewer, New_TEI_MySobekViewer,
-- Preferences_MySobekViewer, Register_MySobekViewer, OpenNJ_Register_MySobekViewer) in the same
-- pass -- per Mark: "The template/project idea is going away completely by the time we roll out
-- 5.2.0", so no replacement selection UI was built; those viewers now use a single fixed
-- template code (the pre-existing "ir" field-default) and an empty default project code.


-- =====================================================================================
-- mySobek_Get_All_DefaultMetadata -- replaces mySobek_Get_All_Template_DefaultMetadatas
-- (dropped below) for the one thing that combined proc did that is NOT going away: giving
-- Engine_ApplicationCache_Gateway.Global_Default_Metadata (still used by
-- Default_Metadata_AdminViewer and AdminViewer_Factory's project-existence check) the
-- master mySobek_DefaultMetadata list. Same query as that proc's first result set; the
-- second ("select ... from mySobek_Template") is simply gone, not replaced.
-- =====================================================================================
CREATE PROCEDURE [dbo].[mySobek_Get_All_DefaultMetadata]
AS
BEGIN
	select MetadataCode, MetadataName, [Description], UserID
	from mySobek_DefaultMetadata
	order by MetadataCode;
END;
GO


-- =====================================================================================
-- mySobek_Get_User_Group -- drops the "Get the templates"/"Get the default metadata"
-- result sets; every result set after them shifts down by two positions to match.
-- =====================================================================================
ALTER PROCEDURE [dbo].[mySobek_Get_User_Group]
	@usergroupid int
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Get the basic user group information
	select *
	from mySobek_User_Group G
	where ( G.UserGroupID = @usergroupid );

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


-- =====================================================================================
-- mySobek_Save_User -- drops @default_template/@default_metadata and the two blocks that
-- used them to link mySobek_User_Template_Link/mySobek_User_DefaultMetadata_Link. Every
-- other parameter and both insert/update branches are reproduced unchanged.
-- =====================================================================================
ALTER PROCEDURE [dbo].[mySobek_Save_User]
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
	end;
END;
GO


-- =====================================================================================
-- Drop the now-unused stored procedures backing the old per-user/group Template and
-- Default Metadata Set assignment lists.
-- =====================================================================================
DROP PROCEDURE IF EXISTS [dbo].[mySobek_Get_All_Template_DefaultMetadatas];
GO
DROP PROCEDURE IF EXISTS [dbo].[mySobek_Add_User_Templates_Link];
GO
DROP PROCEDURE IF EXISTS [dbo].[mySobek_Add_User_DefaultMetadata_Link];
GO
DROP PROCEDURE IF EXISTS [dbo].[mySobek_Add_User_Group_Templates_Link];
GO
DROP PROCEDURE IF EXISTS [dbo].[mySobek_Add_User_Group_Metadata_Link];
GO


-- =====================================================================================
-- Drop the now-unused tables. mySobek_DefaultMetadata (the master project registry) is
-- deliberately NOT dropped -- Default_Metadata_AdminViewer still manages it directly.
-- mySobek_Template has no surviving consumer at all once its two link tables are gone,
-- so it is dropped along with them -- including SobekCM_Project_Template_Link, a third
-- FK reference to it left over from the SobekCM_Project feature. That feature itself is
-- already dead (Save_Project_Template_Link/Delete_Project_Template_Link in
-- SobekCM_Database.cs have no callers anywhere in the codebase), so dropping the link
-- table is safe -- it is dropped here rather than alongside SobekCM_Project itself since
-- removing that whole feature is out of scope for this pass.
-- =====================================================================================
DROP TABLE IF EXISTS [dbo].[mySobek_User_Template_Link];
GO
DROP TABLE IF EXISTS [dbo].[mySobek_User_Group_Template_Link];
GO
DROP TABLE IF EXISTS [dbo].[mySobek_User_DefaultMetadata_Link];
GO
DROP TABLE IF EXISTS [dbo].[mySobek_User_Group_DefaultMetadata_Link];
GO
DROP TABLE IF EXISTS [dbo].[SobekCM_Project_Template_Link];
GO
DROP TABLE IF EXISTS [dbo].[mySobek_Template];
GO


/**************************************************************************/
/**                                                                      **/
/**   Source: MARK_20260831_Add_GoogleDrive_Staging_Schema.sql           **/
/**                                                                      **/
/**************************************************************************/

-- 5.2.0: Google Drive import -- lets a submitter pull large masters (TIFFs, audio, video)
-- out of their own Google Drive ahead of time, into a per-user GCS staging area, then
-- attach from that staged list during the new submission wizard's Upload step instead of
-- browser-uploading them (which is bounded by Kestrel's MaxRequestBodySize).
--
-- Three tables:
--
-- SobekCM_User_Process is deliberately generic, not Google-Drive-specific -- it backs a
-- planned "running processes" indicator in the site chrome (next to the login/user link
-- in HeaderFooter_HtmlHelper.cs), styled after the Google Cloud Console notification
-- tray: an icon showing pending work, a click-through list, and a completion toast.
-- Google Drive downloads are the first process type to use it, but it is meant to cover
-- any slow user-initiated request, not just this one -- item reprocessing, a Drive
-- staging pull, a large spreadsheet import, a MARC21 report built from search criteria,
-- a weak-metadata audit across a user's materials, and whatever gets added after that.
-- ProcessType is a free string rather than an enum column so new process types never
-- need a schema change. Status/UserID is indexed because the chrome's poll-or-not
-- decision depends on a cheap "does this user have anything active" check done inline
-- during normal page rendering (no separate probe request, no polling at all for users
-- with nothing running) -- see [[project_googledrive_import_process_tracker]] for the
-- full client-side design.
--
-- Scope is deliberately three typed FK columns plus a discriminator rather than one
-- polymorphic (TypeCode, ID) pair -- keeps real FK/cascade integrity for the three known
-- entity levels a process can run against (a single item, a whole item group/series, an
-- aggregation/collection), while ScopeType = 'Custom' (all three columns NULL) covers
-- everything else -- a saved search, arbitrary criteria -- with the specifics living in
-- DetailsXml instead of forcing a fourth kind of ID column that doesn't reference
-- anything. DetailsXml itself is nvarchar(max) holding serialized XML, matching the
-- existing SobekCM_Metadata_Block.BlockXml convention (not a native xml column) --
-- untyped and unqueryable, the known tradeoff of a field like this, but it is what makes
-- one table cover five-plus wildly different process shapes without a column explosion.
-- ReportLocation is the process's output artifact when it has one (an imported
-- spreadsheet with BibID/VID stamped back in, a generated MARC21 report file) --
-- NULL for process types with no discrete output, like a plain reprocess.
--
-- DateCreated (queued) is kept separate from DateProcessStarted (a worker actually
-- picked it up) because a Batch-priority job (see SobekCM_GoogleDrive_Staged_File.
-- Priority) can sit queued behind Interactive work for a while before it starts.
--
-- Notification delivery: UserNotifiedDate (renamed from a plain Notified bit) doubles as
-- flag and timestamp, same convention as the other DateX columns here -- and deliberately
-- channel-agnostic, so it means "notified, by whatever means eventually did it" rather than
-- "toast shown." A process that completes while nobody is logged in needs no special
-- queuing logic -- it simply stays NULL, exactly as honest as "not yet delivered," until
-- the next real delivery opportunity (a login, or someday an email digest) stamps it.
-- The user's global notification preference ('On' | 'Paused' | 'Skip' -- one setting, not
-- per-ProcessType, confirmed) lives as an ordinary mySobek_User_Settings row
-- (Setting_Key = 'ProcessNotificationMode'), not a new column here, following the same
-- precedent as the Type-grid sort-order preference elsewhere in this same 5.2.0 work.
-- 'Paused' needs no schema support at all -- it is purely a "don't deliver right now" gate
-- checked at read/delivery time (the tray, someday an email job), leaving UserNotifiedDate
-- untouched so everything is still there once switched back to 'On'. 'Skip' is the one that
-- needs active handling, baked directly into SobekCM_User_Process_Update_Status: when a
-- process reaches a terminal status, that proc checks the owning user's preference and, if
-- 'Skip', auto-stamps UserNotifiedDate right then -- centralized in the one proc every
-- producer already calls, rather than requiring every current and future producer to
-- remember to do it themselves.
--
-- Cancellation: IsCancelable is set by the producer at creation, defaulting to 0 -- a
-- process only gets a Cancel button once its producer has actually been written to notice
-- and honor a cancel request, not just because it happens to be a type that theoretically
-- could support it. ItemReprocessing's two producers never pass 1. CancelRequestedDate
-- doubles as both flag and timestamp (same convention as ConsumedDate on the Link table
-- below), and is only ever set on a row where IsCancelable = 1 (enforced by the CHECK
-- constraint, not just caller discipline). A process still Pending when cancelled resolves
-- immediately straight to Status = 'Cancelled', since nothing is running yet that needs to
-- notice; one already Running instead moves to the transitional Status = 'Cancelling' and
-- waits for whatever is doing the work to actually stop and confirm -- see
-- SobekCM_User_Process_Request_Cancel and the worker contract note in
-- [[project_googledrive_import_process_tracker]]. StatusMessage (renamed from an
-- error-only field) carries either a real error, or a human summary of how far a
-- cancelled process got before stopping, e.g. "Cancelled after 3 of 12 downloads" --
-- useful well beyond Google Drive imports, for any process type that works through
-- multiple internal items (a spreadsheet import's rows, a weak-metadata audit's items).
--
-- SobekCM_GoogleDrive_Staged_File is the pre-staging area itself. Rows are created the
-- moment a user picks files in the Google Picker (before any item exists) and updated by
-- a new background worker (a BackgroundService in the SobekCM web app, not the Builder --
-- deliberately kept off the Builder's queue, which can be busy for hours with OCR/JP2/
-- conversion work having nothing to do with an interactive submitter waiting on a small
-- pull) as it streams each file from Drive straight into GCS. Priority distinguishes an
-- "I'm sitting here waiting" quick pull (a single small file mid-submission) from an
-- "I queued 12 files and I'm going to go build 12 items" batch pull -- the worker always
-- drains Interactive rows first.
--
-- SobekCM_Item_GoogleDrive_Staged_File_Link is written when a staged file is attached to
-- an item during metadata entry. It is a REFERENCE, not a copy -- attaching does not move
-- any bytes. The item is marked Additional_Work_Needed (existing flag, existing proc:
-- SobekCM_Update_Additional_Work_Needed_Flag) and a new Builder module, running inside
-- the existing ItemProcessModules chain that Worker_BulkLoader.Complete_Any_Recent_Loads_
-- Requiring_Additional_Work already drives, looks up unconsumed links for the item and
-- copies each staged GCS object down into the same local resource folder that reprocessing
-- already expects (Image_Server_Network\{file_root}\{vid}), before the rest of that
-- module chain runs unchanged. ConsumedDate is set -- and the staged GCS object deleted --
-- only after the permanent copy is confirmed written, not eagerly, so a crash mid-pipeline
-- never loses the only remaining copy of a file pulled from someone's personal Drive.
--
-- OAuth client credentials for Drive access are deliberately NOT stored in these tables
-- or in SobekCM_Settings -- following the same precedent as the GCS service-account key
-- (kept off the DB for credential-sensitivity reasons), the Drive OAuth client secret
-- lives in the same file/env convention. Only non-sensitive per-instance config (staging
-- bucket name, staging object prefix, storage class) belongs in SobekCM_Settings, added
-- in a separate loose script the same way the GCS settings rows were.


-- =====================================================================================
-- SobekCM_User_Process -- generic long-running-process tracker, backs the chrome's
-- process tray/toast. Not specific to Google Drive.
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_User_Process](
	[ProcessID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[ProcessType] [varchar](50) NOT NULL,			-- e.g. 'GoogleDriveImport', 'ItemReprocessing', 'SpreadsheetImport', 'Marc21Report', 'WeakMetadataAudit' -- open string, new types need no schema change
	[Title] [nvarchar](250) NOT NULL,				-- user-facing text, e.g. 'Downloading masters from Google Drive'
	[Status] [varchar](20) NOT NULL,				-- 'Pending' | 'Running' | 'Cancelling' | 'Cancelled' | 'Complete' | 'Error'
	[PercentComplete] [int] NULL,
	[StatusMessage] [nvarchar](500) NULL,			-- an error detail, OR a human summary of how far a cancelled process got, e.g. "Cancelled after 3 of 12 downloads"
	[ScopeType] [varchar](20) NOT NULL,			-- 'Item' | 'Group' | 'Aggregation' | 'Custom' -- which ID column (if any) applies; 'Custom' means none of them do and DetailsXml carries the specifics
	[ItemID] [int] NULL,
	[GroupID] [int] NULL,
	[AggregationID] [int] NULL,
	[ReportLocation] [nvarchar](500) NULL,			-- link to the process's output artifact, if any
	[DetailsXml] [nvarchar](max) NULL,				-- process-specific structured detail that doesn't earn its own column
	[IsCancelable] [bit] NOT NULL DEFAULT 0,		-- set by the producer at creation -- a process is only cancelable once its producer has actually been written to honor a cancel request
	[CancelRequestedDate] [datetime] NULL,			-- doubles as flag and timestamp, same convention as ConsumedDate on the Link table below
	[DateCreated] [datetime] NOT NULL,				-- when the request was queued
	[DateProcessStarted] [datetime] NULL,			-- when a worker actually picked it up
	[DateCompleted] [datetime] NULL,
	[UserNotifiedDate] [datetime] NULL,			-- doubles as flag and timestamp -- "notified, by whatever means eventually did it" (a tray toast today, maybe email later), not tied to one delivery channel
 CONSTRAINT [PK_SobekCM_User_Process] PRIMARY KEY CLUSTERED ([ProcessID] ASC),
 CONSTRAINT [FK_SobekCM_User_Process_User] FOREIGN KEY([UserID]) REFERENCES [dbo].[mySobek_User] ([UserID]),
 CONSTRAINT [FK_SobekCM_User_Process_Item] FOREIGN KEY([ItemID]) REFERENCES [dbo].[SobekCM_Item] ([ItemID]),
 CONSTRAINT [FK_SobekCM_User_Process_Group] FOREIGN KEY([GroupID]) REFERENCES [dbo].[SobekCM_Item_Group] ([GroupID]),
 CONSTRAINT [FK_SobekCM_User_Process_Aggregation] FOREIGN KEY([AggregationID]) REFERENCES [dbo].[SobekCM_Item_Aggregation] ([AggregationID]),
 CONSTRAINT [CK_SobekCM_User_Process_Scope] CHECK (
	([ScopeType] = 'Item' AND [ItemID] IS NOT NULL AND [GroupID] IS NULL AND [AggregationID] IS NULL) OR
	([ScopeType] = 'Group' AND [GroupID] IS NOT NULL AND [ItemID] IS NULL AND [AggregationID] IS NULL) OR
	([ScopeType] = 'Aggregation' AND [AggregationID] IS NOT NULL AND [ItemID] IS NULL AND [GroupID] IS NULL) OR
	([ScopeType] = 'Custom' AND [ItemID] IS NULL AND [GroupID] IS NULL AND [AggregationID] IS NULL)
 ),
 CONSTRAINT [CK_SobekCM_User_Process_CancelRequiresCancelable] CHECK (
	([CancelRequestedDate] IS NULL) OR ([IsCancelable] = 1)
 )
);
GO

CREATE INDEX [IX_SobekCM_User_Process_User_Status] ON [dbo].[SobekCM_User_Process] ([UserID], [Status]);
GO


-- =====================================================================================
-- SobekCM_GoogleDrive_Staged_File -- the pre-staging area. A row exists from the moment
-- a file is picked in Google Picker, independent of any item.
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_GoogleDrive_Staged_File](
	[StagedFileID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[ProcessID] [int] NULL,						-- the SobekCM_User_Process row tracking this file's download
	[DriveFileID] [varchar](200) NOT NULL,
	[DriveFileName] [nvarchar](500) NOT NULL,
	[FileSizeBytes] [bigint] NULL,
	[GcsStagingObjectKey] [nvarchar](500) NULL,	-- populated once the worker finishes the pull
	[Status] [varchar](20) NOT NULL,				-- 'Pending' | 'Downloading' | 'Ready' | 'Error' | 'Consumed'
	[Priority] [varchar](20) NOT NULL,				-- 'Interactive' | 'Batch' -- worker always drains Interactive first
	[QueuedDate] [datetime] NOT NULL,
	[ReadyDate] [datetime] NULL,
 CONSTRAINT [PK_SobekCM_GoogleDrive_Staged_File] PRIMARY KEY CLUSTERED ([StagedFileID] ASC),
 CONSTRAINT [FK_SobekCM_GoogleDrive_Staged_File_User] FOREIGN KEY([UserID]) REFERENCES [dbo].[mySobek_User] ([UserID]),
 CONSTRAINT [FK_SobekCM_GoogleDrive_Staged_File_Process] FOREIGN KEY([ProcessID]) REFERENCES [dbo].[SobekCM_User_Process] ([ProcessID])
);
GO

CREATE INDEX [IX_SobekCM_GoogleDrive_Staged_File_User_Status] ON [dbo].[SobekCM_GoogleDrive_Staged_File] ([UserID], [Status]);
GO


-- =====================================================================================
-- SobekCM_Item_GoogleDrive_Staged_File_Link -- attach-time reference from an item to a
-- staged file. Written when the user picks a staged file during metadata entry; a
-- reference only, no bytes moved yet. Consumed by the new Builder module that runs
-- inside the existing Additional-Work-Needed ItemProcessModules chain.
-- =====================================================================================
CREATE TABLE [dbo].[SobekCM_Item_GoogleDrive_Staged_File_Link](
	[LinkID] [int] IDENTITY(1,1) NOT NULL,
	[ItemID] [int] NOT NULL,
	[StagedFileID] [int] NOT NULL,
	[TargetFileName] [nvarchar](500) NOT NULL,		-- filename to use once copied into the item's resource folder
	[LinkedDate] [datetime] NOT NULL,
	[ConsumedDate] [datetime] NULL,				-- set only after the permanent copy is confirmed written -- not eagerly, so a mid-pipeline crash can't lose the only copy
 CONSTRAINT [PK_SobekCM_Item_GoogleDrive_Staged_File_Link] PRIMARY KEY CLUSTERED ([LinkID] ASC),
 CONSTRAINT [FK_SobekCM_Item_GoogleDrive_Staged_File_Link_Item] FOREIGN KEY([ItemID]) REFERENCES [dbo].[SobekCM_Item] ([ItemID]),
 CONSTRAINT [FK_SobekCM_Item_GoogleDrive_Staged_File_Link_StagedFile] FOREIGN KEY([StagedFileID]) REFERENCES [dbo].[SobekCM_GoogleDrive_Staged_File] ([StagedFileID])
);
GO

CREATE INDEX [IX_SobekCM_Item_GoogleDrive_Staged_File_Link_Item_Unconsumed] ON [dbo].[SobekCM_Item_GoogleDrive_Staged_File_Link] ([ItemID]) WHERE [ConsumedDate] IS NULL;
GO


-- CRUD stored procedures (staged-file listing, process tray polling, link consumption)
-- are intentionally not part of this script -- they follow once the wizard's Upload-step
-- UI and the new Builder module's exact query shapes are worked out, matching how the
-- Item Type schema/procs were split into separate loose files above.


/**************************************************************************/
/**                                                                      **/
/**   Source: MARK_20260831_Add_GoogleDrive_Staging_Procs.sql            **/
/**                                                                      **/
/**************************************************************************/

-- 5.2.0: Stored procedures for the Google Drive staging schema
-- (MARK_20260831_Add_GoogleDrive_Staging_Schema.sql).
--
-- SobekCM_User_Process deliberately does NOT get a single wide '_Edit' upsert like the
-- Item Type / Permissions Agreement admin screens -- those are saved whole from one form.
-- A process row instead gets written incrementally by background code across a lifecycle
-- (created once, then ticked forward through progress/status many times, then completed) --
-- so it gets a narrow '_Add' (insert only, mirrors the Item Type shape but there's no
-- "edit an existing process's identity" case) plus a lightweight '_Update_Status' for the
-- frequent partial updates, rather than resending the full row on every tick.
--
-- New-row id return follows the existing '@new_xxxid int output' / '@@IDENTITY' convention
-- (see mySobek_User_Group_Edit in MARK_20260827_Add_User_Submission_Fields.sql), not a
-- SELECT SCOPE_IDENTITY() result set.
--
-- Process_mySobekViewer (planned, not part of this script) needs both a user's own list
-- and an admin's system-wide list -- same split as SobekCM_Item_Type_Get_List (Submissions
-- tab checklist) vs SobekCM_Item_Type_Get_Mgmt_List (admin screen): Get_List_For_User is
-- scope-limited by @UserID and callable by anyone; Get_Mgmt_List has no user filter at all
-- and is trusted to be gated by an admin check in the C# layer, same as every other
-- Get_Mgmt_List proc in this system.
--
-- SobekCM_GoogleDrive_Staged_File_Claim_Next_Pending uses an UPDATE...OUTPUT to select and
-- mark a row Downloading in one atomic statement, so the new BackgroundService worker stays
-- correct even if more than one web app instance ends up running it.
--
-- SobekCM_User_Process_Update_Status also handles 'Skip' notification mode inline: when a
-- process lands in a terminal status, it checks the owning user's mySobek_User_Settings
-- 'ProcessNotificationMode' row and, if 'Skip', auto-stamps UserNotifiedDate right there --
-- centralized in the one proc every producer already calls, rather than requiring every
-- producer to remember it. 'Paused' needs no proc support at all -- it is a pure read-time
-- gate the tray (or someday an email job) checks before delivering, not something that
-- touches this row.
--
-- SobekCM_User_Process_Request_Cancel is two guarded UPDATEs, not a CASE/branch in one
-- statement -- a Pending row (never claimed) resolves straight to 'Cancelled', since
-- nothing is running yet that needs to notice; a Running row instead moves to 'Cancelling'
-- and waits for whatever is doing the work to notice, stop, and confirm via the existing
-- Update_Status proc (Status = 'Cancelled', StatusMessage set to a human summary like
-- "Cancelled after 3 of 12 downloads"). Both branches require IsCancelable = 1; anything
-- else (already terminal, or never cancelable) is a silent no-op, matching the
-- SobekCM_Item_Type_Delete guarded-proc precedent rather than raising an error.


-- =====================================================================================
-- SobekCM_User_Process
-- =====================================================================================

-- Insert only. Status always starts 'Pending'; DateCreated stamped here. Returns the new
-- ProcessID so the caller can immediately link it (e.g. SobekCM_GoogleDrive_Staged_File.ProcessID).
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Add]
	@UserID int,
	@ProcessType varchar(50),
	@Title nvarchar(250),
	@ScopeType varchar(20),
	@ItemID int = NULL,
	@GroupID int = NULL,
	@AggregationID int = NULL,
	@DetailsXml nvarchar(max) = NULL,
	@IsCancelable bit = 0,
	@new_processid int output
AS
BEGIN

	INSERT INTO SobekCM_User_Process
		( UserID, ProcessType, Title, Status, ScopeType, ItemID, GroupID, AggregationID, DetailsXml, IsCancelable, DateCreated )
	VALUES
		( @UserID, @ProcessType, @Title, 'Pending', @ScopeType, @ItemID, @GroupID, @AggregationID, @DetailsXml, @IsCancelable, GETDATE() );

	set @new_processid = @@IDENTITY;

END;
GO


-- Frequent, lightweight status tick -- Status transitions to 'Running' stamp DateProcessStarted
-- the first time only (left alone on later calls); Status of 'Complete', 'Error', or
-- 'Cancelled' stamps DateCompleted. @ReportLocation and @StatusMessage are both optional
-- since most ticks set neither -- StatusMessage is also how a worker reports a human summary
-- of a confirmed cancellation, e.g. "Cancelled after 3 of 12 downloads".
--
-- A terminal Status also auto-stamps UserNotifiedDate, silently, if the owning user's
-- 'ProcessNotificationMode' setting is 'Skip' -- see the schema file's notes and
-- [[project_googledrive_import_process_tracker]]. 'On' (the default when the setting is
-- absent) and 'Paused' both leave UserNotifiedDate untouched here.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Update_Status]
	@ProcessID int,
	@Status varchar(20),
	@PercentComplete int = NULL,
	@StatusMessage nvarchar(500) = NULL,
	@ReportLocation nvarchar(500) = NULL
AS
BEGIN

	DECLARE @notificationMode nvarchar(max);
	SELECT TOP (1) @notificationMode = us.Setting_Value
	FROM mySobek_User_Settings us
		INNER JOIN SobekCM_User_Process p ON p.UserID = us.UserID
	WHERE p.ProcessID = @ProcessID AND us.Setting_Key = 'ProcessNotificationMode';

	UPDATE SobekCM_User_Process
	SET Status = @Status,
		PercentComplete = COALESCE(@PercentComplete, PercentComplete),
		StatusMessage = COALESCE(@StatusMessage, StatusMessage),
		ReportLocation = COALESCE(@ReportLocation, ReportLocation),
		DateProcessStarted = CASE WHEN @Status = 'Running' AND DateProcessStarted IS NULL THEN GETDATE() ELSE DateProcessStarted END,
		DateCompleted = CASE WHEN @Status IN ('Complete', 'Error', 'Cancelled') THEN GETDATE() ELSE DateCompleted END,
		UserNotifiedDate = CASE WHEN (@Status IN ('Complete', 'Error', 'Cancelled')) AND (@notificationMode = 'Skip') THEN GETDATE() ELSE UserNotifiedDate END
	WHERE ProcessID = @ProcessID;

END;
GO


-- Requests cancellation. A Pending row (never claimed by a worker) resolves immediately --
-- nothing is running that needs to notice -- straight to 'Cancelled', with @Reason (or a
-- default) recorded as StatusMessage. A Running row instead moves to the transitional
-- 'Cancelling' and waits for whatever is doing the work to notice (checking between
-- discrete units of work, not attempting to abort mid-transfer), stop, and confirm via
-- Update_Status. Only one of the two branches will ever actually match a given row, since
-- Status is exactly one value at a time. Both require IsCancelable = 1; anything else --
-- already terminal, or never cancelable -- is a silent no-op.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Request_Cancel]
	@ProcessID int,
	@Reason nvarchar(500) = NULL
AS
BEGIN

	UPDATE SobekCM_User_Process
	SET Status = 'Cancelled',
		CancelRequestedDate = GETDATE(),
		DateCompleted = GETDATE(),
		StatusMessage = COALESCE(@Reason, 'Cancelled before starting')
	WHERE ProcessID = @ProcessID AND IsCancelable = 1 AND Status = 'Pending';

	UPDATE SobekCM_User_Process
	SET Status = 'Cancelling',
		CancelRequestedDate = GETDATE()
	WHERE ProcessID = @ProcessID AND IsCancelable = 1 AND Status = 'Running';

END;
GO


-- Fires once the chrome tray has actually delivered a completion notification for this
-- process (a toast shown, or someday an email sent), so a later poll or page refresh does
-- not deliver it again. Channel-agnostic on purpose -- it does not care which delivery
-- mechanism called it.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Mark_Notified]
	@ProcessID int
AS
BEGIN
	UPDATE SobekCM_User_Process SET UserNotifiedDate = GETDATE() WHERE ProcessID = @ProcessID;
END;
GO


-- Full single-row read, for the process tray's click-through detail and Process_mySobekViewer.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Get_Single]
	@ProcessID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select ProcessID, UserID, ProcessType, Title, Status, PercentComplete, StatusMessage,
		ScopeType, ItemID, GroupID, AggregationID, ReportLocation, DetailsXml,
		IsCancelable, CancelRequestedDate,
		DateCreated, DateProcessStarted, DateCompleted, UserNotifiedDate
	from SobekCM_User_Process
	where ProcessID = @ProcessID;
END;
GO


-- A user's own processes -- the tray dropdown and Process_mySobekViewer's "My Processes" list.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Get_List_For_User]
	@UserID int,
	@ActiveOnly bit = 0
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select ProcessID, ProcessType, Title, Status, PercentComplete, StatusMessage,
		ScopeType, ItemID, GroupID, AggregationID, ReportLocation,
		IsCancelable, CancelRequestedDate,
		DateCreated, DateProcessStarted, DateCompleted, UserNotifiedDate
	from SobekCM_User_Process
	where UserID = @UserID
		and ( (@ActiveOnly = 0) OR (Status in ('Pending', 'Running')) )
	order by DateCreated desc;
END;
GO


-- Every user's processes -- Process_mySobekViewer's admin "All Processes" list. No UserID
-- filter at all; trusted to be gated by an admin check in the C# layer, same as every other
-- _Get_Mgmt_List proc in this system.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Get_Mgmt_List]
	@ActiveOnly bit = 1
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select p.ProcessID, p.UserID, u.UserName, u.EmailAddress, p.ProcessType, p.Title, p.Status,
		p.PercentComplete, p.StatusMessage, p.ScopeType, p.ItemID, p.GroupID, p.AggregationID,
		p.ReportLocation, p.IsCancelable, p.CancelRequestedDate,
		p.DateCreated, p.DateProcessStarted, p.DateCompleted
	from SobekCM_User_Process p
		inner join mySobek_User u on p.UserID = u.UserID
	where (@ActiveOnly = 0) OR (p.Status in ('Pending', 'Running'))
	order by p.DateCreated desc;
END;
GO


-- The cheap check the chrome does inline during normal page rendering, to decide whether
-- the client should even start its poll loop -- deliberately its own tiny proc rather than
-- reusing Get_List_For_User, since this runs on every page view for every logged-in user.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Get_Active_Count_For_User]
	@UserID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select COUNT(*) as ActiveCount
	from SobekCM_User_Process
	where UserID = @UserID and Status in ('Pending', 'Running');
END;
GO


-- Builder-side: finds the active tracked process for a given item + process type, so
-- Worker_BulkLoader can tick it to 'Running'/'Complete'/'Error' as it works through an
-- Additional-Work-Needed reprocess. Most recent active (Pending/Running) row wins -- there
-- should only ever be one, but this is defensive against a second request landing before
-- the first is picked up.
CREATE PROCEDURE [dbo].[SobekCM_User_Process_Get_Active_For_Item]
	@ItemID int,
	@ProcessType varchar(50)
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select TOP (1) ProcessID
	from SobekCM_User_Process
	where ItemID = @ItemID and ProcessType = @ProcessType and Status in ('Pending', 'Running')
	order by DateCreated desc;
END;
GO


-- =====================================================================================
-- SobekCM_GoogleDrive_Staged_File
-- =====================================================================================

-- Insert only, fired the moment a file is picked in Google Picker. Status always starts
-- 'Pending'; QueuedDate stamped here. Returns the new StagedFileID.
CREATE PROCEDURE [dbo].[SobekCM_GoogleDrive_Staged_File_Add]
	@UserID int,
	@ProcessID int,
	@DriveFileID varchar(200),
	@DriveFileName nvarchar(500),
	@FileSizeBytes bigint = NULL,
	@Priority varchar(20),
	@new_stagedfileid int output
AS
BEGIN

	INSERT INTO SobekCM_GoogleDrive_Staged_File
		( UserID, ProcessID, DriveFileID, DriveFileName, FileSizeBytes, Status, Priority, QueuedDate )
	VALUES
		( @UserID, @ProcessID, @DriveFileID, @DriveFileName, @FileSizeBytes, 'Pending', @Priority, GETDATE() );

	set @new_stagedfileid = @@IDENTITY;

END;
GO


-- Atomically claims the next pending file for the worker to download -- Interactive rows
-- always drain before Batch ones, oldest first within each. UPDATE...OUTPUT so the claim
-- and the read happen in one statement, safe even if more than one worker instance is running.
CREATE PROCEDURE [dbo].[SobekCM_GoogleDrive_Staged_File_Claim_Next_Pending]
AS
BEGIN

	UPDATE TOP (1) SobekCM_GoogleDrive_Staged_File
	SET Status = 'Downloading'
	OUTPUT inserted.StagedFileID, inserted.UserID, inserted.DriveFileID, inserted.DriveFileName, inserted.Priority
	WHERE StagedFileID = (
		select TOP (1) StagedFileID
		from SobekCM_GoogleDrive_Staged_File
		where Status = 'Pending'
		order by
			case Priority when 'Interactive' then 0 else 1 end,
			QueuedDate asc
	);

END;
GO


-- Fired by the worker once a claimed download finishes (Status = 'Ready', with the GCS key
-- it landed at) or fails (Status = 'Error'). ReadyDate stamped only on success.
CREATE PROCEDURE [dbo].[SobekCM_GoogleDrive_Staged_File_Update_Status]
	@StagedFileID int,
	@Status varchar(20),
	@GcsStagingObjectKey nvarchar(500) = NULL
AS
BEGIN

	UPDATE SobekCM_GoogleDrive_Staged_File
	SET Status = @Status,
		GcsStagingObjectKey = COALESCE(@GcsStagingObjectKey, GcsStagingObjectKey),
		ReadyDate = CASE WHEN @Status = 'Ready' THEN GETDATE() ELSE ReadyDate END
	WHERE StagedFileID = @StagedFileID;

END;
GO


-- The wizard Upload step's "attach from pre-staged content" list -- everything this user has
-- ready and not yet attached to an item.
CREATE PROCEDURE [dbo].[SobekCM_GoogleDrive_Staged_File_Get_Ready_For_User]
	@UserID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select StagedFileID, DriveFileName, FileSizeBytes, ReadyDate
	from SobekCM_GoogleDrive_Staged_File
	where UserID = @UserID and Status = 'Ready';
END;
GO


-- =====================================================================================
-- SobekCM_Item_GoogleDrive_Staged_File_Link
-- =====================================================================================

-- Written when a staged file is attached to an item during metadata entry -- a reference
-- only. The caller is still responsible for separately calling the existing
-- SobekCM_Update_Additional_Work_Needed_Flag proc to flag the item for Builder; that stays
-- a separate call rather than folding it in here, matching how the rest of this system
-- composes several focused proc calls from C# instead of one do-everything proc.
CREATE PROCEDURE [dbo].[SobekCM_Item_GoogleDrive_Staged_File_Link_Add]
	@ItemID int,
	@StagedFileID int,
	@TargetFileName nvarchar(500),
	@new_linkid int output
AS
BEGIN

	INSERT INTO SobekCM_Item_GoogleDrive_Staged_File_Link
		( ItemID, StagedFileID, TargetFileName, LinkedDate )
	VALUES
		( @ItemID, @StagedFileID, @TargetFileName, GETDATE() );

	set @new_linkid = @@IDENTITY;

END;
GO


-- Builder-side: everything still owed to this item, joined with the staged file so the new
-- module has the GCS key and target filename in one query.
CREATE PROCEDURE [dbo].[SobekCM_Item_GoogleDrive_Staged_File_Link_Get_Pending_For_Item]
	@ItemID int
AS
BEGIN
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select l.LinkID, l.TargetFileName, s.StagedFileID, s.GcsStagingObjectKey, s.DriveFileName
	from SobekCM_Item_GoogleDrive_Staged_File_Link l
		inner join SobekCM_GoogleDrive_Staged_File s on l.StagedFileID = s.StagedFileID
	where l.ItemID = @ItemID and l.ConsumedDate is null;
END;
GO


-- Fired only after the permanent copy is confirmed durably written -- not eagerly. Marks
-- both the link (ConsumedDate) and the staged file (Status = 'Consumed') together, since
-- this is the one moment both change state at once -- the point where the staged GCS copy
-- is safe to delete.
CREATE PROCEDURE [dbo].[SobekCM_Item_GoogleDrive_Staged_File_Link_Mark_Consumed]
	@LinkID int
AS
BEGIN

	UPDATE SobekCM_Item_GoogleDrive_Staged_File_Link
	SET ConsumedDate = GETDATE()
	WHERE LinkID = @LinkID;

	UPDATE s
	SET s.Status = 'Consumed'
	FROM SobekCM_GoogleDrive_Staged_File s
		inner join SobekCM_Item_GoogleDrive_Staged_File_Link l on l.StagedFileID = s.StagedFileID
	WHERE l.LinkID = @LinkID;

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
