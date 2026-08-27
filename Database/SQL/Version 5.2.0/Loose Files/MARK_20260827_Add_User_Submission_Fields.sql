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
