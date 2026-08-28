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
