-- Adds a System User flag (mySobek_User.IsSystemUser), for accounts that must never be locked out
-- (e.g. service/integration accounts). Not setable anywhere in the UI - defaults to 0 and is only
-- ever set directly against the database. When set, the users admin screen's deactivate checkbox is
-- replaced with a "System users cannot be deactivated" message, and the flag cannot be changed
-- through a form postback either. System users are also hidden entirely from the users admin list
-- for anyone who is not the top-level admin (Host Administrator if hosted, otherwise System
-- Administrator); the top-level admin can choose to show them via a System Users filter.
--   * mySobek_Get_User_By_UserID now returns IsSystemUser, so the users admin screen can see it.
--     This redefines mySobek_Get_User_By_UserID, so it must run after the user active flag change
--     and the user news change (MARK_20260922_User_Active_Flag_Admin.sql / MARK_20260922_Add_User_News.sql).
--   * mySobek_Get_All_Users now returns IsSystemUser too, for the users admin list and its filter.

if ( not exists ( select 1 from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = 'mySobek_User' and COLUMN_NAME = 'IsSystemUser' ))
begin
	ALTER TABLE [dbo].[mySobek_User] ADD [IsSystemUser] [bit] NOT NULL CONSTRAINT [DF_mySobek_User_IsSystemUser] DEFAULT ((0));
end;
GO

-- Procedures keep the SET options in effect when they are created. sqlcmd defaults QUOTED_IDENTIFIER
-- to OFF, which breaks deletes against tables with filtered indexes, so set both explicitly.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns IsSystemUser as well. Redefines the version from the user news change.
ALTER PROCEDURE [dbo].[mySobek_Get_User_By_UserID]
	@userid int,
	@include_inactive bit = 'false'
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
	  AuthenticationSource, isActive, IsNewsAdmin, IsSystemUser
	from mySobek_User U
	where ( UserID = @userid ) and (( isActive = 'true' ) or ( @include_inactive = 'true' ));

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

-- Returns IsSystemUser as well, for the users admin list's System Users filter
ALTER PROCEDURE [dbo].[mySobek_Get_All_Users] AS
BEGIN

	-- Get the list of users and pending user requests
	with pending_cte as
	(
		select UserID, count(*) as PendingRequests
		from mySobek_User_Request
		where Pending='true'
		group by UserID
	)
	select U.UserID, LastName + ', ' + FirstName AS [Full_Name], UserName, EmailAddress, coalesce(R.PendingRequests,0) as PendingRequests, U.isActive, U.IsSystemUser
	from mySobek_User U left join
		 pending_cte R on U.UserID = R.UserID
	order by Full_Name;
END;
GO
