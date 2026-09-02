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
