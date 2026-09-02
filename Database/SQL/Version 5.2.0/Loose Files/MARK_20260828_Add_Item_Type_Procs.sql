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
