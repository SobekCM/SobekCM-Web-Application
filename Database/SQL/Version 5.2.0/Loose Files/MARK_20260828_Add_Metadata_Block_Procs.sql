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
