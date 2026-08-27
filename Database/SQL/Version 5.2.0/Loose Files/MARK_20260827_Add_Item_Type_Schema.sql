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
