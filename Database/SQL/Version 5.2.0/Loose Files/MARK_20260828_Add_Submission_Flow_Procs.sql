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
