

 update SobekCM_Item_Viewer_Types set [Order]=1 where ViewType='TEI';
 GO

IF object_id('mySobek_Get_All_User_Settings_Like') IS NULL EXEC ('create procedure dbo.mySobek_Get_All_User_Settings_Like as select 1;');
GO


-- Procedure gets settings across all the users that are like the key start
--
-- Since this uses like, you can pass in a string like 'TEI.%' and that will return
-- all the values that have a setting key that STARTS with 'TEI.'
--
-- If @value is NULL, then all settings that match are returned.  If a value is
-- provided for @value, then only the settings that match the key search and 
-- have the same value in the database as @value are returned.  This is particularly
-- useful for boolean settings, where you only want to the see the settings set to 'true'
ALTER PROCEDURE mySobek_Get_All_User_Settings_Like
	@keyStart nvarchar(255),
	@value nvarchar(max)
AS
begin

	-- User can request settings that are only one value (useful for boolean settings really)
	if ( @value is null )
	begin
	
		-- Just return all that are like the setting key
		select U.UserName, U.UserID, coalesce(U.FirstName,'') as FirstName, coalesce(U.LastName,'') as LastName, S.Setting_Key, S.Setting_Value
		from mySobek_User U, mySobek_User_Settings S
		where ( U.UserID = S.UserID )
		  and ( Setting_Key like @keyStart )
		  and ( U.isActive = 'true' );

	end
	else
	begin
		
		-- Return information on settings like the setting key and set to @value then
		select U.UserName, U.UserID, coalesce(U.FirstName,'') as FirstName, coalesce(U.LastName,'') as LastName, S.Setting_Key, S.Setting_Value
		from mySobek_User U, mySobek_User_Settings S
		where ( U.UserID = S.UserID )
		  and ( Setting_Key like @keyStart )
		  and ( U.isActive = 'true' )
		  and ( Setting_Value = @value );
	end;

end;
GO

GRANT EXECUTE ON mySobek_Get_All_User_Settings_Like to sobek_user;
GO

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
	  Department=coalesce(Department,''), Unit=coalesce(Unit,''), Rights=coalesce(Default_Rights,''), Language=coalesce(UI_Language, ''), 
	  Internal_User, OrganizationCode, EditTemplate, EditTemplateMarc, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms,
	  Descriptions=( select COUNT(*) from mySobek_User_Description_Tags T where T.UserID=U.UserID),
	  Receive_Stats_Emails, Has_Item_Stats, Can_Delete_All_Items, ScanningTechnician, ProcessingTechnician, InternalNotes=coalesce(InternalNotes,''),
	  IsHostAdmin
	from mySobek_User U
	where ( UserID = @userid ) and ( isActive = 'true' );

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
	select G.GroupName, Can_Submit_Items, Internal_User, IsSystemAdmin, IsPortalAdmin, Include_Tracking_Standard_Forms 
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

IF object_id('SobekCM_Set_Item_Setting_Value') IS NULL EXEC ('create procedure dbo.SobekCM_Set_Item_Setting_Value as select 1;');
GO

-- Sets a single item setting value, by key.  Adds a new one if this
-- is a new setting key, otherwise updates the existing value.
ALTER PROCEDURE [dbo].[SobekCM_Set_Item_Setting_Value]
	@ItemID int,
	@Setting_Key varchar(255),
	@Setting_Value varchar(max)
AS
BEGIN

	-- Does this setting exist?
	if ( ( select COUNT(*) from SobekCM_Item_Settings where Setting_Key=@Setting_Key and ItemID=@ItemID ) > 0 )
	begin
		-- Just update existing then
		update SobekCM_Item_Settings set Setting_Value=@Setting_Value where Setting_Key = @Setting_Key and ItemID=@ItemID;
	end
	else
	begin
		-- insert a new settting key/value pair
		insert into SobekCM_Item_Settings( ItemID, Setting_Key, Setting_Value )
		values ( @ItemID, @Setting_Key, @Setting_Value );
	end;	
END;
GO

GRANT EXECUTE ON [dbo].[SobekCM_Set_Item_Setting_Value] to sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_Set_Item_Setting_Value] to sobek_builder;
GO
