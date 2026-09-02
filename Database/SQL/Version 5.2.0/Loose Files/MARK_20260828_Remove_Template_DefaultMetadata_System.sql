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
