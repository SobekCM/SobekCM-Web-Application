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
