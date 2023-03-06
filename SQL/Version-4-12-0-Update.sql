/****** Object:  Table [dbo].[mySobek_User_Request]    Script Date: 10/14/2021 5:53:48 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF ( NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'mySobek_User_Request'))
begin
	CREATE TABLE dbo.mySobek_User_Request (
		UserRequestID int IDENTITY(1,1) NOT NULL,
		UserID int NOT NULL,
		UserGroupID int NULL,
		RequestDate datetime NOT NULL,
		RequestSubmitPermissions bit NOT NULL,
		RequestUrl nvarchar(255) NULL,
		Pending bit NOT NULL,
		Approved bit NOT NULL,
		Notes nvarchar(2000) NULL,
		ApproverUserID int NULL,
		Code nvarchar(255) NULL,
	 CONSTRAINT PK_mySobek_User_Request PRIMARY KEY CLUSTERED 
	(
		[UserRequestID] ASC
		)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]
end;
GO

IF ( NOT EXISTS ( SELECT *  FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS  WHERE CONSTRAINT_NAME ='FK_mySobek_User_Request_mySobek_User' ))
begin
	ALTER TABLE [dbo].[mySobek_User_Request]  WITH CHECK ADD  CONSTRAINT [FK_mySobek_User_Request_mySobek_User] FOREIGN KEY([UserID])
	REFERENCES [dbo].[mySobek_User] ([UserID])
end;
GO

IF ( NOT EXISTS ( SELECT *  FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS  WHERE CONSTRAINT_NAME ='FK_mySobek_User_Request_mySobek_User_Group' ))
begin
	ALTER TABLE [dbo].[mySobek_User_Request]  WITH CHECK ADD  CONSTRAINT [FK_mySobek_User_Request_mySobek_User_Group] FOREIGN KEY([UserGroupID])
	REFERENCES [dbo].[mySobek_User_Group] ([UserGroupID])
end;
GO


IF ( NOT EXISTS ( SELECT *  FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS  WHERE CONSTRAINT_NAME ='FK_mySobek_User_Request_mySobek_User1' ))
begin
	ALTER TABLE [dbo].[mySobek_User_Request]  WITH CHECK ADD  CONSTRAINT [FK_mySobek_User_Request_mySobek_User1] FOREIGN KEY([ApproverUserID])
	REFERENCES [dbo].[mySobek_User] ([UserID])
end;
GO

IF ( NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SobekCM_OpenPublishing_Theme'))
begin
	CREATE TABLE [dbo].[SobekCM_OpenPublishing_Theme](
		[ThemeID] [int] IDENTITY(1,1) NOT NULL,
		[ThemeName] [nvarchar](150) NOT NULL,
		[Location] [varchar](255) NOT NULL,
		[Author] [nvarchar](50) NULL,
		[Description] [nvarchar](1000) NULL,
		[Image] [nvarchar](255) NULL,
		[AvailableForSelection] bit NOT NULL default('true'),
		[Default] bit NOT NULL default('false')
	CONSTRAINT [PK_OpenPublishing_Theme] PRIMARY KEY CLUSTERED 
	(
		[ThemeID] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]
end;
GO

/** NEW COLUMNS ON EXISING TABLES **/

if ( COL_LENGTH('dbo.mySobek_User_Group_Link', 'IsGroupAdmin') is null )
begin
	ALTER TABLE dbo.mySobek_User_Group_Link add IsGroupAdmin bit NOT NULL default('false');
end;
GO

if ( COL_LENGTH('dbo.mySobek_User', 'IsUserAdmin') is null )
begin
	ALTER TABLE dbo.mySobek_User add IsUserAdmin bit NOT NULL default('false');
end;
GO


if ( COL_LENGTH('dbo.SobekCM_Item', 'RestrictionMessage') is null )
begin
	alter table dbo.SobekCM_Item add RestrictionMessage nvarchar(1000) null;
end;
GO

if ( COL_LENGTH('dbo.Tracking_Progress', 'WorkPerformedById') is null )
begin
	alter table dbo.Tracking_Progress add WorkPerformedById int null;
end;
GO

/** NEW STORED PROCEDURES **/

IF object_id('mySobek_Add_User_Request') IS NULL EXEC ('create procedure dbo.mySobek_Add_User_Request as select 1;');
GO

IF object_id('SobekCM_Clear_Item_User_Group_Permissions') IS NULL EXEC ('create procedure dbo.SobekCM_Clear_Item_User_Group_Permissions as select 1;');
GO

IF object_id('SobekCM_Save_Item_User_Group_Permissions') IS NULL EXEC ('create procedure dbo.SobekCM_Save_Item_User_Group_Permissions as select 1;');
GO

IF object_id('SobekCM_Clear_Item_User_Permissions') IS NULL EXEC ('create procedure dbo.SobekCM_Clear_Item_User_Permissions as select 1;');
GO

IF object_id('SobekCM_Save_Item_User_Permissions') IS NULL EXEC ('create procedure dbo.SobekCM_Save_Item_User_Permissions as select 1;');
GO

IF object_id('mySobek_Add_User_Setting') IS NULL EXEC ('create procedure dbo.mySobek_Add_User_Setting as select 1;');
GO

IF object_id('mySobek_Clear_User_Settings') IS NULL EXEC ('create procedure dbo.mySobek_Clear_User_Settings as select 1;');
GO

IF object_id('Tracking_Item_Visibility_Report') IS NULL EXEC ('create procedure dbo.Tracking_Item_Visibility_Report as select 1;');
GO

IF object_id('SobekCM_Get_Submittor') IS NULL EXEC ('create procedure dbo.SobekCM_Get_Submittor as select 1;');
GO

IF object_id('SobekCM_Get_OpenPublishing_Theme') IS NULL EXEC ('create procedure dbo.SobekCM_Get_OpenPublishing_Theme as select 1;');
GO

IF object_id('SobekCM_Get_Available_OpenPublishing_Themes') IS NULL EXEC ('create procedure dbo.SobekCM_Get_Available_OpenPublishing_Themes as select 1;');
GO


-- Add a link between a user and a user request
ALTER PROCEDURE [dbo].[mySobek_Add_User_Request]
	@userid int,
	@usergroupid int,
	@requestsubmitpermissions bit,
	@requesturl nvarchar(255),
	@notes nvarchar(2000),
	@code nvarchar(255)
AS
begin

	insert into mySobek_User_Request(UserID, UserGroupID, RequestSubmitPermissions, RequestUrl, Notes, Pending, RequestDate, Approved, Code )
	values ( @Userid, @usergroupid, @requestsubmitpermissions, @requesturl, @notes, 'true', getdate(), 'false', @code);
	
end;
GO

ALTER PROCEDURE dbo.SobekCM_Clear_Item_User_Group_Permissions
	@ItemId int
AS
BEGIN
	delete from mySobek_User_Group_Item_Permissions
	where ItemID = @ItemId;
END;
GO

ALTER PROCEDURE dbo.SobekCM_Save_Item_User_Group_Permissions
	@ItemId int,
	@UserGroupId int,
	@isOwner bit,
	@canView bit,
	@canEditMetadata bit,
	@canEditBehaviors bit,
	@canPerformQc bit,
	@canUploadFiles bit,
	@canChangeVisibility bit,
	@canDelete bit,
	@customPermissions varchar(max),
	@isDefaultPermissions bit
AS
BEGIN

	-- Does a similar entry already exist?
	if (( select count(*) from mySobek_User_Group_Item_Permissions where ItemId=@ItemId and UserGroupID=@UserGroupId) > 0 )
	begin
		update mySobek_User_Group_Item_Permissions
		set isOwner=@isOwner, canView=@canView, canEditMetadata=@canEditMetadata, canEditBehaviors=@canEditBehaviors, 
		    canPerformQc=@canPerformQc, canUploadFiles=@canUploadFiles, canChangeVisibility=@canChangeVisibility,
			canDelete=@canDelete, customPermissions=@customPermissions, isDefaultPermissions=@isDefaultPermissions
		where ItemId=@ItemId and UserGroupId=@UserGroupId;
	end
	else
	begin
		insert into mySobek_User_Group_Item_Permissions ( UserGroupID, ItemID, isOwner, canView, canEditMetadata, canEditBehaviors, canPerformQc, canUploadFiles, canChangeVisibility, canDelete, customPermissions, isDefaultPermissions )
		values ( @UserGroupId, @ItemId, @isOwner, @canView, @canEditMetadata, @canEditBehaviors, @canPerformQc, @canUploadFiles, @canChangeVisibility, @canDelete, @customPermissions, @isDefaultPermissions);
	end;
END;
GO

ALTER PROCEDURE dbo.SobekCM_Clear_Item_User_Permissions
	@ItemId int
AS
BEGIN
	delete from mySobek_User_Item_Permissions
	where ItemID = @ItemId;
END;
GO



ALTER PROCEDURE dbo.SobekCM_Save_Item_User_Permissions
	@ItemId int,
	@UserId int,
	@isOwner bit,
	@canView bit,
	@canEditMetadata bit,
	@canEditBehaviors bit,
	@canPerformQc bit,
	@canUploadFiles bit,
	@canChangeVisibility bit,
	@canDelete bit,
	@customPermissions varchar(max)
AS
BEGIN

	-- Does a similar entry already exist?
	if (( select count(*) from mySobek_User_Item_Permissions where ItemId=@ItemId and UserID=@UserId) > 0 )
	begin
		update mySobek_User_Item_Permissions
		set isOwner=@isOwner, canView=@canView, canEditMetadata=@canEditMetadata, canEditBehaviors=@canEditBehaviors, 
		    canPerformQc=@canPerformQc, canUploadFiles=@canUploadFiles, canChangeVisibility=@canChangeVisibility,
			canDelete=@canDelete, customPermissions=@customPermissions
		where ItemId=@ItemId and UserId=@UserId;
	end
	else
	begin
		insert into mySobek_User_Item_Permissions ( UserID, ItemID, isOwner, canView, canEditMetadata, canEditBehaviors, canPerformQc, canUploadFiles, canChangeVisibility, canDelete, customPermissions )
		values ( @UserId, @ItemId, @isOwner, @canView, @canEditMetadata, @canEditBehaviors, @canPerformQc, @canUploadFiles, @canChangeVisibility, @canDelete, @customPermissions );
	end;
END;
GO



ALTER PROCEDURE [dbo].mySobek_Add_User_Setting
	@userid int,
	@setting_key nvarchar(255),
	@setting_value nvarchar(max)
AS
begin

	-- Does this already exist?
	if ( (select count(*) from mySobek_User_Settings where UserID=@userid and Setting_Key=@setting_key ) > 0 )
	begin
		-- If this clears the value, remove the key
		if ( len(@setting_value) > 0 ) 
		begin
			delete from mySobek_User_Settings where UserID=@userid and Setting_Key=@setting_key;
		end
		else
		begin
			update mySobek_User_Settings 
			set Setting_Value=@setting_value
			where UserID=@userid and Setting_Key=@setting_key;
		end;
	end
	else
	begin

		insert into mySobek_User_Settings ( UserID, Setting_Key, Setting_Value )
		values ( @userid, @setting_key, @setting_value );

	end;
	
end;
GO


ALTER PROCEDURE [dbo].mySobek_Clear_User_Settings
	@userid int
AS 
begin

	delete from mySobek_User_Settings
	where UserID=@userid;

end;
GO

ALTER PROCEDURE [dbo].[Tracking_Item_Visibility_Report]
	@aggregation_code varchar(20)
AS
BEGIN

	if ( LEN( ISNULL( @aggregation_code,'')) = 0 )
	begin
      		    with items_cte as 
			(
				select GroupID, I.ItemID, PageCount, FileCount, 
				  CASE IP_Restriction_Mask WHEN 0 THEN 'PUBLIC' WHEN -1 THEN 'PRIVATE' ELSE 'IP RESTRICTED' END as Restriction,
				  CASE IP_Restriction_Mask WHEN 0 THEN 0 WHEN -1 THEN 4 ELSE 3 END as OrderBy
				from SobekCM_Item I
				where ( I.Deleted='false') 
				  and ( not exists ( select 1 from mySobek_User_Group_Item_Permissions P where P.ItemID=I.ItemID and P.canView='true' ))
				UNION
				select GroupID, I.ItemID, PageCount, FileCount, 'USER GROUP RESTRICTED' as Restriction, 2 as OrderBy
				from SobekCM_Item I
				where ( I.Deleted='false') 
				  and ( exists ( select 1 from mySobek_User_Group_Item_Permissions P where P.ItemID=I.ItemID and P.canView='true' ))
			)
			select Restriction, title_count=count(distinct(GroupID)), item_count=count(*), page_count = SUM([PageCount]), file_count=SUM(FileCount), OrderBy
			from items_cte I
			group by Restriction, OrderBy
			union
			select 'TOTAL', title_count=count(distinct(GroupID)), item_count=count(*), page_count = SUM([PageCount]), file_count=SUM(FileCount), 5 as OrderBy
			from items_cte I
			order by OrderBy;
    end
    else
    begin
    
		declare @aggregationid int
		set @aggregationid = (select top 1 AggregationID from SobekCM_Item_Aggregation where Code=@aggregation_code)
		
		if ( ISNULL(@aggregationid,-1) > 0 )
		begin
		    with items_cte as 
			(
				select GroupID, I.ItemID, PageCount, FileCount, 
				  CASE IP_Restriction_Mask WHEN 0 THEN 'PUBLIC' WHEN -1 THEN 'PRIVATE' ELSE 'IP RESTRICTED' END as Restriction,
				  CASE IP_Restriction_Mask WHEN 0 THEN 0 WHEN -1 THEN 4 ELSE 3 END as OrderBy
				from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L
				where ( I.Deleted='false') 
				  and ( L.ItemID=I.ItemID )
				  and ( L.AggregationID = @aggregationid )
				  and ( not exists ( select 1 from mySobek_User_Group_Item_Permissions P where P.ItemID=I.ItemID and P.canView='true' ))
				UNION
				select GroupID, I.ItemID, PageCount, FileCount, 'USER GROUP RESTRICTED' as Restriction, 2 as OrderBy
				from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L
				where ( I.Deleted='false') 
				  and ( L.ItemID=I.ItemID )
				  and ( L.AggregationID = @aggregationid )
				  and ( exists ( select 1 from mySobek_User_Group_Item_Permissions P where P.ItemID=I.ItemID and P.canView='true' ))
			)
			select Restriction, title_count=count(distinct(GroupID)), item_count=count(*), page_count = SUM([PageCount]), file_count=SUM(FileCount), OrderBy
			from items_cte I
			group by Restriction, OrderBy
			union
			select 'TOTAL', title_count=count(distinct(GroupID)), item_count=count(*), page_count = SUM([PageCount]), file_count=SUM(FileCount), 5 as OrderBy
			from items_cte I
			order by OrderBy;
		end   
    end
END
GO

ALTER PROCEDURE [dbo].SobekCM_Get_Submittor
	@bibid varchar(20),
	@vid varchar(10)
AS
BEGIN

	declare @itemid int;
	set @itemid = ( select ItemID from SobekCM_Item_Group G, SobekCM_Item I where I.GroupID = G.GroupID and BibID=@bibid and VID=@vid);

	select U.FirstName + ' ' + U.LastName as UserName, EmailAddress, U.UserID
	from Tracking_Progress P inner join
		 Tracking_Workflow W on P.WorkFlowID=W.WorkFlowID inner join
		 mySobek_User U on U.UserID=P.WorkPerformedById
	where P.ItemID=@itemid 
	  and W.WorkFlowName='Online Submit';

END;
GO

ALTER PROCEDURE [dbo].[SobekCM_Get_OpenPublishing_Theme]
	@id int
AS
begin

	select ThemeID, ThemeName, Location, isnull(Author,'') as Author, isnull([Description],'') as [Description], isnull([Image], '') as [Image], AvailableForSelection, [Default]
	from SobekCM_OpenPublishing_Theme
	where ThemeID=@id;

end;
GO


ALTER PROCEDURE [dbo].[SobekCM_Get_Available_OpenPublishing_Themes]
	@id int
AS
begin

	select ThemeID, ThemeName, Location, isnull(Author,'') as Author, isnull([Description],'') as [Description], isnull([Image], '') as [Image], AvailableForSelection, [Default]
	from SobekCM_OpenPublishing_Theme
	where AvailableForSelection='true';

end;
GO

GRANT EXECUTE ON mySobek_Add_User_Request to sobek_user;
GRANT EXECUTE ON SobekCM_Clear_Item_User_Group_Permissions to sobek_user;
GRANT EXECUTE ON SobekCM_Save_Item_User_Group_Permissions to sobek_user;
GRANT EXECUTE ON SobekCM_Clear_Item_User_Permissions to sobek_user;
GRANT EXECUTE ON SobekCM_Save_Item_User_Permissions to sobek_user;
GRANT EXECUTE ON mySobek_Add_User_Setting to sobek_user;
GRANT EXECUTE ON mySobek_Clear_User_Settings to sobek_user;
GRANT EXECUTE ON Tracking_Item_Visibility_Report to sobek_user;
GRANT EXECUTE ON SobekCM_Get_Submittor to sobek_user;
GRANT EXECUTE ON SobekCM_Get_OpenPublishing_Theme to sobek_user;
GRANT EXECUTE ON SobekCM_Get_Available_OpenPublishing_Themes to sobek_user;
GO

/** PREEXISTING STORED PROCEDURES **/



ALTER PROCEDURE [dbo].[mySobek_Permissions_Report] as
begin

	-- Return the top-level permissions (non-aggregation specific)
	select '' as GroupName, U.UserID, UserName, EmailAddress, FirstName, LastName, Nickname, DateCreated, LastActivity, isActive, 
		case when e.UserID is null then 'false' else 'true' end as Can_Edit_All_Items,
		Internal_User, Can_Delete_All_Items, IsPortalAdmin, IsSystemAdmin, IsHostAdmin, IsUserAdmin
	from mySobek_User as U left outer join
		 mySobek_User_Editable_Link as E on E.UserID = U.UserID and E.EditableID = 1 
	where      ( IsSystemAdmin = 'true' )
			or ( IsPortalAdmin = 'true' )
			or ( Can_Delete_All_Items = 'true' )
			or ( IsHostAdmin = 'true' )
			or ( Internal_User = 'true' )
	union
	select G.GroupName, U.UserID, UserName, EmailAddress, FirstName, LastName, Nickname, DateCreated, LastActivity, isActive, 
		case when e.UserGroupID is null then 'false' else 'true' end as Can_Edit_All_Items,
		G.Internal_User, G.Can_Delete_All_Items, G.IsPortalAdmin, G.IsSystemAdmin, 'false', 'false'
	from mySobek_User as U inner join
		 mySobek_User_Group_Link as L on U.UserID = L.UserID inner join
		 mySobek_User_Group as G on G.UserGroupID = L.UserGroupID left outer join
		 mySobek_User_Group_Editable_Link as E on E.UserGroupID = G.UserGroupID and E.EditableID = 1 
	where      ( G.IsSystemAdmin = 'true' )
			or ( G.IsPortalAdmin = 'true' )
			or ( G.Can_Delete_All_Items = 'true' )
			or ( G.Internal_User = 'true' )
	order by LastName ASC, FirstName ASC, GroupName ASC;
end;
GO


-- Procedure allows an admin to edit permissions flags for this user
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
      @clear_projects_templates bit,
      @clear_aggregation_links bit,
      @clear_user_groups bit
AS
begin transaction

      -- Update the simple table values
      update mySobek_User
      set Can_Submit_Items=@can_submit, Internal_User=@is_internal, 
            IsPortalAdmin=@is_portal_admin, IsSystemAdmin=@is_system_admin, 
            Include_Tracking_Standard_Forms=@include_tracking_standard_forms, 
            EditTemplate=@edit_template, Can_Delete_All_Items = @can_delete_all,
            EditTemplateMarc=@edit_template_marc, IsHostAdmin=@is_host_admin,
			IsUserAdmin=@is_user_admin
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

      -- Clear the projects/templates
      if ( @clear_projects_templates = 'true' )
      begin
            delete from mySobek_User_DefaultMetadata_Link where UserID=@userid;
            delete from mySobek_User_Template_Link where UserID=@userid;
      end;

      -- Clear the projects/templates
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
	  IsHostAdmin, IsUserAdmin
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


-- Saves all the main data about an item in UFDC (but not behaviors)
-- Written by Mark Sullivan ( September 2005, Edited November 2021)
ALTER PROCEDURE [dbo].[SobekCM_Save_Item]
	@GroupID int,
	@VID varchar(5),
	@PageCount int,
	@FileCount int,
	@Title nvarchar(500),
	@SortTitle nvarchar(500), --NEW
	@AccessMethod int,
	@Link varchar(500),
	@CreateDate datetime,
	@PubDate nvarchar(100),
	@SortDate bigint,
	@HoldingCode varchar(20),
	@SourceCode varchar(20),
	@Author nvarchar(1000),
	@Spatial_KML varchar(4000),
	@Spatial_KML_Distance float,
	@DiskSize_KB bigint,
	@Spatial_Display nvarchar(1000), 
	@Institution_Display nvarchar(1000), 
	@Edition_Display nvarchar(1000),
	@Material_Display nvarchar(1000),
	@Measurement_Display nvarchar(1000), 
	@StylePeriod_Display nvarchar(1000), 
	@Technique_Display nvarchar(1000), 
	@Subjects_Display nvarchar(1000), 
	@Donor nvarchar(250),
	@Publisher nvarchar(1000),
	@RestrictionMessage nvarchar(1000),
	@ItemID int output,
	@Existing bit output,
	@New_VID varchar(5) output
AS
begin transaction

	-- Set the return VID value first
	set @New_VID = @VID;

	-- If this already exists (BibID, VID) then just update
	if ( (	 select count(*) from SobekCM_Item I where ( I.VID = @VID ) and ( I.GroupID = @GroupID ) )  > 0 )
	begin
		-- Save the item id
		select @ItemID = I.ItemID
		from SobekCM_Item I
		where  ( I.VID = @VID ) and ( I.GroupID = @GroupID );

		--Update the main item
		update SobekCM_Item
		set [PageCount] = @PageCount, 
			Deleted = 0, Title=@Title, SortTitle=@SortTitle, AccessMethod=@AccessMethod, Link=@Link,
			PubDate=@PubDate, SortDate=@SortDate, FileCount=@FileCount, Author=@Author, 
			Spatial_KML=@Spatial_KML, Spatial_KML_Distance=@Spatial_KML_Distance,  
			Donor=@Donor, Publisher=@Publisher, 
			GroupID = GroupID, LastSaved=GETDATE(), Spatial_Display=@Spatial_Display, Institution_Display=@Institution_Display, 
			Edition_Display=@Edition_Display, Material_Display=@Material_Display, Measurement_Display=@Measurement_Display, 
			StylePeriod_Display=@StylePeriod_Display, Technique_Display=@Technique_Display, Subjects_Display=@Subjects_Display,
			RestrictionMessage=@RestrictionMessage  
		where ( ItemID = @ItemID );

		-- Set the existing flag to true (1)
		set @Existing = 1;
	end
	else
	begin
	
		-- Verify the VID is a complete bibid, otherwise find the next one
		if ( LEN(@VID) < 5 )
		begin
			declare @next_vid_number int;

			-- Find the next vid number
			select @next_vid_number = isnull(CAST(MAX(VID) as int) + 1,-1)
			from SobekCM_Item
			where GroupID = @GroupID;
			
			-- If no matches to this BibID, just start at 00001
			if ( @next_vid_number < 0 )
			begin
				select @New_VID = '00001';
			end
			else
			begin
				select @New_VID = RIGHT('0000' + (CAST( @next_vid_number as varchar(5))), 5);	
			end;	
		end;
		
		-- Add the values to the main SobekCM_Item table first
		insert into SobekCM_Item ( VID, [PageCount], FileCount, Deleted, Title, SortTitle, AccessMethod, Link, CreateDate, PubDate, SortDate, Author, Spatial_KML, Spatial_KML_Distance, GroupID, LastSaved, Donor, Publisher, Spatial_Display, Institution_Display, Edition_Display, Material_Display, Measurement_Display, StylePeriod_Display, Technique_Display, Subjects_Display, RestrictionMessage )
		values (  @New_VID, @PageCount, @FileCount, 0, @Title, @SortTitle, @AccessMethod, @Link, @CreateDate, @PubDate, @SortDate, @Author, @Spatial_KML, @Spatial_KML_Distance, @GroupID, GETDATE(), @Donor, @Publisher, @Spatial_Display, @Institution_Display, @Edition_Display, @Material_Display, @Measurement_Display, @StylePeriod_Display, @Technique_Display, @Subjects_Display, @RestrictionMessage );

		-- Get the item id identifier for this row
		set @ItemID = @@identity;

		-- Set existing flag to false
		set @Existing = 0;
		
		-- Copy over all the default viewer information
		insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label, Exclude )
		select @itemid, ItemViewTypeID, '', '', 'false' 
		from SobekCM_Item_Viewer_Types
		where ( DefaultView = 'true' );
	end;

	-- Check for Holding Institution Code
	declare @AggregationID int;
	if ( len ( isnull ( @HoldingCode, '' ) ) > 0 )
	begin
		-- Does this institution already exist?
		if (( select count(*) from SobekCM_Item_Aggregation where Code = @HoldingCode ) = 0 )
		begin
			-- Add new institution
			insert into SobekCM_Item_Aggregation ( Code, [Name], ShortName, Description, ThematicHeadingID, [Type], isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
			values ( @HoldingCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
		end;
		
		-- Add the link to this holding code ( and any legitimate parent aggregations )
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @HoldingCode;		
	end;

	-- Check for Source Institution Code
	if ( len ( isnull ( @SourceCode, '' ) ) > 0 )
	begin
		-- Does this institution already exist?
		if (( select count(*) from SobekCM_Item_Aggregation where Code = @SourceCode ) = 0 )
		begin
			-- Add new institution
			insert into SobekCM_Item_Aggregation ( Code, [Name], ShortName, Description, ThematicHeadingID, [Type], isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
			values ( @SourceCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
		end;

		-- Add the link to this holding code ( and any legitimate parent aggregations )
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @SourceCode;	
	end;
	
	-- If a size was included, set that value
	if ( @DiskSize_KB > 0 )
	begin
		update SobekCM_Item set DiskSize_KB = @DiskSize_KB where ItemID=@ItemID;
	end;

	-- Finally set the volume count for this group correctly
	declare @itemcount int;
	set @itemcount = ( select count(*) from SobekCM_Item I where ( I.GroupID = @GroupID ) and ( I.Deleted = 'false' ));

	-- Update the item group count
	update SobekCM_Item_Group
	set ItemCount = @itemcount
	where GroupID = @GroupID;

	-- If this was an update, and this group had only this one VID, look at changing the
	-- group title to match the item title
	if (( @Existing = 1 ) and ( @itemcount = 1 ))
	begin
		-- Only make this update if this is not a SERIAL or NEWSPAPER
		if ( exists ( select 1 from SobekCM_Item_Group where GroupID=@GroupID and [Type] != 'Serial' and [Type] != 'Newspaper' ))
		begin
			update SobekCM_Item_Group 
			set GroupTitle = @Title, SortTitle = @SortTitle
			where GroupID=@GroupID;
		end;
	end;

commit transaction;
GO


-- Pull any additional item details before showing this item
ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Details2]
	@BibID varchar(10),
	@VID varchar(5)
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Does this BIbID exist?
	if (not exists ( select 1 from SobekCM_Item_Group where BibID = @BibID ))
	begin
		select 'INVALID BIBID' as ErrorMsg, '' as BibID, '' as VID;
		return;
	end;

	-- Was this for one item within a group?
	if ( LEN( ISNULL(@VID,'')) > 0 )
	begin	

		-- Does this VID exist in that stored procedure?
		if ( not exists ( select 1 from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID=@BibID and I.VID = @VID ))
		begin

			select top 1 'INVALID VID' as ErrorMsg, @BibID as BibID, VID
			from SobekCM_Item I, SobekCM_Item_Group G
			where I.GroupID = G.GroupID 
			  and G.BibID = @BibID
			order by VID;

			return;
		end;
	
		-- Only continue if there is ONE match
		if (( select COUNT(*) from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = @BibID and I.VID = @VID ) = 1 )
		begin
			-- Get the itemid
			declare @ItemID int;
			select @ItemID = ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = @BibID and I.VID = @VID;

			-- Return any descriptive tags
			select U.FirstName, U.NickName, U.LastName, G.BibID, I.VID, T.Description_Tag, T.TagID, T.Date_Modified, U.UserID, isnull([PageCount], 0) as Pages, ExposeFullTextForHarvesting
			from mySobek_User U, mySobek_User_Description_Tags T, SobekCM_Item I, SobekCM_Item_Group G
			where ( T.ItemID = @ItemID )
			  and ( I.ItemID = T.ItemID )
			  and ( I.GroupID = G.GroupID )
			  and ( T.UserID = U.UserID );
			
			-- Return the aggregation information linked to this item
			select A.Code, A.Name, A.ShortName, A.[Type], A.Map_Search, A.DisplayOptions, A.Items_Can_Be_Described, L.impliedLink, A.Hidden, A.isActive, ISNULL(A.External_Link,'') as External_Link
			from SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A
			where ( L.ItemID = @ItemID )
			  and ( A.AggregationID = L.AggregationID );
		  
			-- Return information about the actual item/group
			select G.BibID, I.VID, G.File_Location, G.SuppressEndeca, 'true' as [Public], I.IP_Restriction_Mask, G.GroupID, I.ItemID, I.CheckoutRequired, Total_Volumes=(select COUNT(*) from SobekCM_Item J where G.GroupID = J.GroupID ),
				isnull(I.Level1_Text, '') as Level1_Text, isnull( I.Level1_Index, 0 ) as Level1_Index, 
				isnull(I.Level2_Text, '') as Level2_Text, isnull( I.Level2_Index, 0 ) as Level2_Index, 
				isnull(I.Level3_Text, '') as Level3_Text, isnull( I.Level3_Index, 0 ) as Level3_Index,
				G.GroupTitle, I.TextSearchable, Comments=isnull(I.Internal_Comments,''), Dark, G.[Type],
				I.Title, I.Publisher, I.Author, I.Donor, I.PubDate, G.ALEPH_Number, G.OCLC_Number, I.Born_Digital, 
				I.Disposition_Advice, I.Material_Received_Date, I.Material_Recd_Date_Estimated, I.Tracking_Box, I.Disposition_Advice_Notes, 
				I.Left_To_Right, I.Disposition_Notes, G.Track_By_Month, G.Large_Format, G.Never_Overlay_Record, I.CreateDate, I.SortDate, 
				G.Primary_Identifier_Type, G.Primary_Identifier, G.[Type] as GroupType, coalesce(I.MainThumbnail,'') as MainThumbnail,
				T.EmbargoEnd, coalesce(T.UMI,'') as UMI, T.Original_EmbargoEnd, coalesce(T.Original_AccessCode,'') as Original_AccessCode,
				I.CitationSet, I.MadePublicDate, I.RestrictionMessage
			from SobekCM_Item as I inner join
				 SobekCM_Item_Group as G on G.GroupID=I.GroupID left outer join
				 Tracking_Item as T on T.ItemID=I.ItemID
			where ( I.ItemID = @ItemID );
		  
			-- Return any ticklers associated with this item
			select MetadataValue
			from SobekCM_Metadata_Unique_Search_Table M, SobekCM_Metadata_Unique_Link L
			where ( L.ItemID = @ItemID ) 
			  and ( L.MetadataID = M.MetadataID )
			  and ( M.MetadataTypeID = 20 );
			
			-- Return the viewers for this item
			select T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder) as MenuOrder, V.Exclude, coalesce(V.OrderOverride, T.[Order])
			from SobekCM_Item_Viewers V, SobekCM_Item_Viewer_Types T
			where ( V.ItemID = @ItemID )
			  and ( V.ItemViewTypeID = T.ItemViewTypeID )
			group by T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder), V.Exclude, coalesce(V.OrderOverride, T.[Order])
			order by coalesce(V.OrderOverride, T.[Order]) ASC;
				
			-- Return the icons for this item
			select Icon_URL, Link, Icon_Name, I.Title
			from SobekCM_Icon I, SobekCM_Item_Icons L
			where ( L.IconID = I.IconID ) 
			  and ( L.ItemID = @ItemID )
			order by Sequence;
			  
			-- Return any web skin restrictions
			select S.WebSkinCode
			from SobekCM_Item_Group_Web_Skin_Link L, SobekCM_Item I, SobekCM_Web_Skin S
			where ( L.GroupID = I.GroupID )
			  and ( L.WebSkinID = S.WebSkinID )
			  and ( I.ItemID = @ItemID )
			order by L.Sequence;

			-- Return all of the key/value pairs of settings
			select Setting_Key, Setting_Value
			from SobekCM_Item_Settings 
			where ItemID=@ItemID;

			-- Return any special user group restriction information
			select I.UserGroupID, G.GroupName, I.canView, I.isOwner, I.canEditMetadata, I.canEditBehaviors, I.canPerformQc, I.canUploadFiles, I.canChangeVisibility, I.canDelete, I.customPermissions
			from mySobek_User_Group_Item_Permissions I, mySobek_User_Group G
			where G.UserGroupID=I.UserGroupID;

			-- Return any special user restriction information
			select I.UserID, U.UserName, U.UserID, I.canView, I.isOwner, I.canEditMetadata, I.canEditBehaviors, I.canPerformQc, I.canUploadFiles, I.canChangeVisibility, I.canDelete, I.customPermissions
			from mySobek_User_Item_Permissions I, mySobek_User U
			where U.UserID=I.UserID;

		end;		
	end
	else
	begin
		-- Return the aggregation information linked to this item
		select GroupTitle, BibID, G.[Type], G.File_Location, isnull(AGGS.Code,'') AS Code, G.GroupID, isnull(GroupThumbnail,'') as Thumbnail, G.Track_By_Month, G.Large_Format, G.Never_Overlay_Record, G.Primary_Identifier_Type, G.Primary_Identifier
		from SobekCM_Item_Group AS G LEFT JOIN
			 ( select distinct(A.Code),  G2.GroupID
			   from SobekCM_Item_Group G2, SobekCM_Item IL, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A
		       where IL.ItemID=L.ItemID 
		         and A.AggregationID=L.AggregationID
		         and G2.GroupID=IL.GroupID
		         and G2.BibID=@BibID
		         and G2.Deleted='false'
		       group by A.Code, G2.GroupID ) AS AGGS ON G.GroupID=AGGS.GroupID
		where ( G.BibID = @BibID )
		  and ( G.Deleted = 'false' );

		-- Get list of icon ids
		select distinct(IconID)
		into #TEMP_ICON
		from SobekCM_Item_Icons II, SobekCM_Item It, SobekCM_Item_Group G
		where ( It.GroupID = G.GroupID )
			and ( G.BibID = @bibid )
			and ( It.Deleted = 0 )
			and ( II.ItemID = It.ItemID )
		group by IconID;

		-- Return icons
		select Icon_URL, Link, Icon_Name, Title
		from SobekCM_Icon I, (	select distinct(IconID)
								from SobekCM_Item_Icons II, SobekCM_Item It, SobekCM_Item_Group G
								where ( It.GroupID = G.GroupID )
							 	  and ( G.BibID = @bibid )
								  and ( It.Deleted = 0 )
								  and ( II.ItemID = It.ItemID )
								group by IconID) AS T
		where ( T.IconID = I.IconID );
		
		-- Return any web skin restrictions
		select S.WebSkinCode
		from SobekCM_Item_Group_Web_Skin_Link L, SobekCM_Item_Group G, SobekCM_Web_Skin S
		where ( L.GroupID = G.GroupID )
		  and ( L.WebSkinID = S.WebSkinID )
		  and ( G.BibID = @BibID )
		order by L.Sequence;
		
		-- Get the distinct list of all aggregations linked to this item
		select distinct( Code )
		from SobekCM_Item_Aggregation A, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Group G, SobekCM_Item I
		where ( I.ItemID = L.ItemID )
		  and ( I.GroupID = G.GroupID )
		  and ( G.BibID = @BibID )
		  and ( L.AggregationID = A.AggregationID );		
	end;
		
	-- Get the list of related item groups
	select B.BibID, B.GroupTitle, R.Relationship_A_to_B AS Relationship
	from SobekCM_Item_Group A, SobekCM_Item_Group_Relationship R, SobekCM_Item_Group B
	where ( A.BibID = @bibid ) 
	  and ( R.GroupA = A.GroupID )
	  and ( R.GroupB = B.GroupID )
	union
	select A.BibID, A.GroupTitle, R.Relationship_B_to_A AS Relationship
	from SobekCM_Item_Group A, SobekCM_Item_Group_Relationship R, SobekCM_Item_Group B
	where ( B.BibID = @bibid ) 
	  and ( R.GroupB = B.GroupID )
	  and ( R.GroupA = A.GroupID );
		  
END;
GO


-- Saves all the main data for a new item in a SobekCM library, 
-- including the serial hierarchy, behaviors, tracking, and basic item information
-- Written by Mark Sullivan ( January 2011 )
ALTER PROCEDURE [dbo].[SobekCM_Save_New_Item]
	@GroupID int,
	@VID varchar(5),
	@PageCount int,
	@FileCount int,
	@Title nvarchar(500),
	@SortTitle nvarchar(500), 
	@AccessMethod int,
	@Link varchar(500),
	@CreateDate datetime,
	@PubDate nvarchar(100),
	@SortDate bigint,
	@Author nvarchar(1000),
	@Spatial_KML varchar(4000),
	@Spatial_KML_Distance float,
	@DiskSize_KB bigint,
	@Spatial_Display nvarchar(1000), 
	@Institution_Display nvarchar(1000), 
	@Edition_Display nvarchar(1000),
	@Material_Display nvarchar(1000),
	@Measurement_Display nvarchar(1000), 
	@StylePeriod_Display nvarchar(1000), 
	@Technique_Display nvarchar(1000), 
	@Subjects_Display nvarchar(1000), 
	@Donor nvarchar(250),
	@Publisher nvarchar(1000),
	@TextSearchable bit,
	@MainThumbnail varchar(100),
	@MainJPEG varchar(100),
	@IP_Restriction_Mask smallint,
	@CheckoutRequired bit,
	@AggregationCode1 varchar(20),
	@AggregationCode2 varchar(20),
	@AggregationCode3 varchar(20),
	@AggregationCode4 varchar(20),
	@AggregationCode5 varchar(20),
	@AggregationCode6 varchar(20),
	@AggregationCode7 varchar(20),
	@AggregationCode8 varchar(20),
	@HoldingCode varchar(20),
	@SourceCode varchar(20),
	@Icon1_Name varchar(50),
	@Icon2_Name varchar(50),
	@Icon3_Name varchar(50),
	@Icon4_Name varchar(50),
	@Icon5_Name varchar(50),
	@Level1_Text varchar(255),
	@Level1_Index int,
	@Level2_Text varchar(255),
	@Level2_Index int,
	@Level3_Text varchar(255),
	@Level3_Index int,
	@Level4_Text varchar(255),
	@Level4_Index int,
	@Level5_Text varchar(255),
	@Level5_Index int,
	@VIDSource varchar(150),
	@CopyrightIndicator smallint, 
	@Born_Digital bit,
	@Dark bit,
	@Material_Received_Date datetime,
	@Material_Recd_Date_Estimated bit,
	@Disposition_Advice int,
	@Disposition_Advice_Notes varchar(150),
	@Internal_Comments nvarchar(1000),
	@Tracking_Box varchar(25),
	@Online_Submit bit,
	@User varchar(50),
	@UserNotes varchar(1000),
	@UserID_To_Link int,
	@RestrictionMessage varchar(1000),
	@ItemID int output,
	@New_VID varchar(5) output
AS
begin transaction

	-- Set the return VID value and itemid first
	set @New_VID = @VID;
	set @ItemID = -1;

	-- Verify this is a new item before doing anything
	if ( (	 select count(*) from SobekCM_Item I where ( I.VID = @VID ) and ( I.GroupID = @GroupID ))  =  0 )
	begin
	
		-- Verify the VID is a complete bibid, otherwise find the next one
		if ( LEN(@VID) < 5 )
		begin
			declare @next_vid_number int;

			-- Find the next vid number
			select @next_vid_number = isnull(CAST(MAX(VID) as int) + 1,-1)
			from SobekCM_Item
			where GroupID = @GroupID;
			
			-- If no matches to this BibID, just start at 00001
			if ( @next_vid_number < 0 )
			begin
				select @New_VID = '00001'
			end
			else
			begin
				select @New_VID = RIGHT('0000' + (CAST( @next_vid_number as varchar(5))), 5);	
			end;	
		end;

		-- Add the values to the main SobekCM_Item table first
		insert into SobekCM_Item ( VID, [PageCount], FileCount, Deleted, Title, SortTitle, AccessMethod, Link, CreateDate, PubDate, SortDate, Author, Spatial_KML, Spatial_KML_Distance, GroupID, LastSaved, Donor, Publisher, TextSearchable, MainThumbnail, MainJPEG, CheckoutRequired, IP_Restriction_Mask, Level1_Text, Level1_Index, Level2_Text, Level2_Index, Level3_Text, Level3_Index, Level4_Text, Level4_Index, Level5_Text, Level5_Index, Last_MileStone, VIDSource, Born_Digital, Dark, Material_Received_Date, Material_Recd_Date_Estimated, Disposition_Advice, Internal_Comments, Tracking_Box, Disposition_Advice_Notes, Spatial_Display, Institution_Display, Edition_Display, Material_Display, Measurement_Display, StylePeriod_Display, Technique_Display, Subjects_Display, RestrictionMessage )
		values (  @New_VID, @PageCount, @FileCount, 0, @Title, @SortTitle, @AccessMethod, @Link, @CreateDate, @PubDate, @SortDate, @Author, @Spatial_KML, @Spatial_KML_Distance, @GroupID, GETDATE(), @Donor, @Publisher, @TextSearchable, @MainThumbnail, @MainJPEG, @CheckoutRequired, @IP_Restriction_Mask, @Level1_Text, @Level1_Index, @Level2_Text, @Level2_Index, @Level3_Text, @Level3_Index, @Level4_Text, @Level4_Index, @Level5_Text, @Level5_Index, 0, @VIDSource, @Born_Digital, @Dark, @Material_Received_Date, @Material_Recd_Date_Estimated, @Disposition_Advice, @Internal_Comments, @Tracking_Box, @Disposition_Advice_Notes, @Spatial_Display, @Institution_Display, @Edition_Display, @Material_Display, @Measurement_Display, @StylePeriod_Display, @Technique_Display, @Subjects_Display, @RestrictionMessage  );
		
		-- Get the item id identifier for this row
		set @ItemID = @@identity;	
		
		-- Set the milestones to complete if this is NON-PRIVATE, NON-DARK, and BORN DIGITAL
		if (( @IP_Restriction_Mask >= 0 ) and ( @Dark = 'false' ) and ( @Born_Digital = 'true' ))
		begin
			update SobekCM_Item
			set Last_MileStone = 4, Milestone_DigitalAcquisition = CreateDate, Milestone_ImageProcessing=CreateDate, Milestone_QualityControl=CreateDate, Milestone_OnlineComplete=CreateDate 
			where ItemID=@ItemID;		
		end;
				
		-- If a size was included, set that value
		if ( @DiskSize_KB > 0 )
		begin
			update SobekCM_Item set DiskSize_KB = @DiskSize_KB where ItemID=@ItemID;
		end;

		-- Finally set the volume count for this group correctly
		update SobekCM_Item_Group
		set ItemCount = ( select count(*) from SobekCM_Item I where ( I.GroupID = @GroupID ) and ( I.Deleted = 'false' ))
		where GroupID = @GroupID;
		
		-- Add the first icon to this object  (this requires the icons have been pre-established )
		declare @IconID int;
		if ( len( isnull( @Icon1_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon1_Name;

			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				values ( @ItemID, @IconID, 1 );
			end;
		end;

		-- Add the second icon to this object  (this requires the icons have been pre-established )
		if ( len( isnull( @Icon2_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon2_Name;

			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				values ( @ItemID, @IconID, 2 );
			end;
		end;

		-- Add the third icon to this object  (this requires the icons have been pre-established )
		if ( len( isnull( @Icon3_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon3_Name;

			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				values ( @ItemID, @IconID, 3 );
			end;
		end;

		-- Add the fourth icon to this object  (this requires the icons have been pre-established )
		if ( len( isnull( @Icon4_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon4_Name;
			
			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				values ( @ItemID, @IconID, 4 );
			end;
		end;

		-- Add the fifth icon to this object  (this requires the icons have been pre-established )
		if ( len( isnull( @Icon5_Name, '' )) > 0 ) 
		begin
			-- Get the Icon ID for this icon
			select @IconID = IconID from SobekCM_Icon where Icon_Name = @Icon5_Name;

			-- Tie this item to this icon
			if ( ISNULL(@IconID,-1) > 0 )
			begin
				insert into SobekCM_Item_Icons ( ItemID, IconID, [Sequence] )
				values ( @ItemID, @IconID, 5 );
			end;
		end;

		-- Clear all links to aggregations
		delete from SobekCM_Item_Aggregation_Item_Link where ItemID = @ItemID;

		-- Add all of the aggregations
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode1;
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode2;
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode3;
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode4;
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode5;
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode6;
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode7;
		exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @AggregationCode8;
		
		-- Create one string of all the aggregation codes
		declare @aggregationCodes varchar(100);
		set @aggregationCodes = rtrim(isnull(@AggregationCode1,'') + ' ' + isnull(@AggregationCode2,'') + ' ' + isnull(@AggregationCode3,'') + ' ' + isnull(@AggregationCode4,'') + ' ' + isnull(@AggregationCode5,'') + ' ' + isnull(@AggregationCode6,'') + ' ' + isnull(@AggregationCode7,'') + ' ' + isnull(@AggregationCode8,''));
	
		-- Update matching items to have the aggregation codes value
		update SobekCM_Item set AggregationCodes = @aggregationCodes where ItemID=@ItemID;

		-- Check for Holding Institution Code
		declare @AggregationID int;
		if ( len ( isnull ( @HoldingCode, '' ) ) > 0 )
		begin
			-- Does this institution already exist?
			if (( select count(*) from SobekCM_Item_Aggregation where Code = @HoldingCode ) = 0 )
			begin
				-- Add new institution
				insert into SobekCM_Item_Aggregation ( Code, [Name], ShortName, Description, ThematicHeadingID, [Type], isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
				values ( @HoldingCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
			end;
			
			-- Add the link to this holding code ( and any legitimate parent aggregations )
			exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @HoldingCode;
		end;

		-- Check for Source Institution Code
		if ( len ( isnull ( @SourceCode, '' ) ) > 0 )
		begin
			-- Does this institution already exist?
			if (( select count(*) from SobekCM_Item_Aggregation where Code = @SourceCode ) = 0 )
			begin
				-- Add new institution
				insert into SobekCM_Item_Aggregation ( Code, [Name], ShortName, Description, ThematicHeadingID, [Type], isActive, Hidden, DisplayOptions, Map_Search, Map_Display, OAI_Flag, ContactEmail, HasNewItems )
				values ( @SourceCode, 'Added automatically', 'Added automatically', 'Added automatically', -1, 'Institution', 'false', 'true', '', 0, 0, 'false', '', 'false' );
			end;

			-- Add the link to this holding code ( and any legitimate parent aggregations )
			exec SobekCM_Save_Item_Item_Aggregation_Link @ItemID, @SourceCode;
		end;

		-- Just in case somehow some viewers existed
		delete from SobekCM_Item_Viewers 
		where ItemID=@itemid;
		
		-- Copy over all the default viewer information
		insert into SobekCM_Item_Viewers ( ItemID, ItemViewTypeID, Attribute, Label, Exclude )
		select @itemid, ItemViewTypeID, '', '', 'false' 
		from SobekCM_Item_Viewer_Types
		where ( DefaultView = 'true' );

		-- Add the workhistory for this item being loaded
		if ( @Online_Submit = 'true' )
		begin
			-- Add progress for online submission completed
			insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath, WorkPerformedById )
			values ( @itemid, 29, getdate(), @user, @usernotes, '', @UserID_To_Link );
		end
		else
		begin  
			-- Add progress for bulk loaded into the system through the Builder
			insert into Tracking_Progress ( ItemID, WorkFlowID, DateCompleted, WorkPerformedBy, ProgressNote, WorkingFilePath )
			values ( @itemid, 40, getdate(), @user, @usernotes, '' );	
		end;		

		-- Is this non-dark and public?
		if (( @Dark = 'false' ) and ( @IP_Restriction_Mask >= 0 ))
		begin
			update SobekCM_Item 
			set MadePublicDate = getdate()
			where ItemID=@ItemID;
		end;
		
		-- Link this to the user?
		if ( @UserID_To_Link >= 1 )
		begin
			-- Link this user to the bibid, if not already linked
			if (( select COUNT(*) from mySobek_User_Bib_Link where UserID=@UserID_To_Link and GroupID = @groupid ) = 0 )
			begin
				insert into mySobek_User_Bib_Link ( UserID, GroupID )
				values ( @UserID_To_Link, @groupid );
			end;
			
			-- First, see if this user already has a folder named 'Submitted Items'
			declare @userfolderid int
			if (( select count(*) from mySobek_User_Folder where UserID=@UserID_To_Link and FolderName='Submitted Items') > 0 )
			begin
				-- Get the existing folder id
				select @userfolderid = UserFolderID from mySobek_User_Folder where UserID=@UserID_To_Link and FolderName='Submitted Items';
			end
			else
			begin
				-- Add this folder
				insert into mySobek_User_Folder ( UserID, FolderName, isPublic )
				values ( @UserID_To_Link, 'Submitted Items', 'false' );

				-- Get the new id
				select @userfolderid = @@identity;
			end;
			
			-- Add a new link then
			insert into mySobek_User_Item( UserFolderID, ItemID, ItemOrder, UserNotes, DateAdded )
			values ( @userfolderid, @itemid, 1, '', getdate() );
			
			-- Also link using the newer system, which links for statistical reporting, etc..
			-- This will likely replace the 'submitted items' folder technique from above
			insert into mySobek_User_Item_Link( UserID, ItemID, RelationshipID )
			values ( @UserID_To_Link, @ItemID, 1 );
		
		end;
	end;

commit transaction;
GO

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
	select U.UserID, LastName + ', ' + FirstName AS [Full_Name], UserName, EmailAddress, coalesce(R.PendingRequests,0) as PendingRequests
	from mySobek_User U left join 
		 pending_cte R on U.UserID = R.UserID 
	order by Full_Name;
END;
GO


-- Get the tracking work history against this item and the milestones
ALTER PROCEDURE [dbo].[Tracking_Get_Work_History]
	@bibid varchar(10),
	@vid varchar(5)
AS
BEGIN	

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Get the item id
	declare @itemid int;
	set @itemid = coalesce( ( select I.ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID=G.GroupID and I.VID=@vid and G.BibiD=@bibid ), -1 );

	-- Get the item id
	declare @groupid int;
	set @groupid = coalesce( ( select G.GroupID from SobekCM_Item_Group G where G.BibiD=@bibid ), -1 );

	-- Return all the progress information for this volume
	select P.WorkFlowID, [Workflow Name]=WorkFlowName, [Completed Date]=isnull(CONVERT(CHAR(10), DateCompleted, 102),''), WorkPerformedBy=isnull(WorkPerformedBy, ''), Note=isnull(ProgressNote,''), isnull(WorkPerformedById, -1) as WorkPerformedById
	from Tracking_Progress P, Tracking_Workflow W
	where (P.workflowid = W.workflowid)
	  and (P.ItemID = @itemid )
	order by DateCompleted ASC;		

	-- Return the milestones as well
	select CreateDate, Milestone_DigitalAcquisition, Milestone_ImageProcessing, Milestone_QualityControl, Milestone_OnlineComplete, Material_Received_Date, Disposition_Date from SobekCM_Item where ItemID=@itemid;
		
END
GO




/** NEW DATA **/

-- Add new system-wide settings
if ( not exists ( select 1 from dbo.SobekCM_Settings where Setting_Key = 'Use Tracking Sheet'))
begin
	insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
	values ( 'Use Tracking Sheet', 'false', 'Digital Resource Settings', 'Online Management Settings', 0, 0, 'Whether the administrative options to use the tracking sheet will be displayed', 'true|false' );
end;
GO

if ( not exists ( select 1 from dbo.SobekCM_Settings where Setting_Key = 'Manage GeoSpatial Data'))
begin
	insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
	values ( 'Manage GeoSpatial Data', 'false', 'Digital Resource Settings', 'Online Management Settings', 0, 0, 'Whether the beta options to manage geo-spatial data will be displayed', 'true|false' );
end;
GO

if ( not exists ( select 1 from dbo.SobekCM_Settings where Setting_Key = 'Allow Mass Behavior Update'))
begin
	insert into dbo.SobekCM_Settings ( Setting_Key, Setting_Value, TabPage, Heading, Hidden, Reserved, Help, Options )
	values ( 'Allow Mass Behavior Update', 'false', 'Digital Resource Settings', 'Online Management Settings', 0, 0, 'Whether the administrative options to mass update the behaviors is available', 'true|false' );
end;
GO

if ( not exists ( select 1 from dbo.SobekCM_Metadata_Types where MetadataName = 'Series Title' ))
begin
	insert into dbo.SobekCM_Metadata_Types ( MetadataName, SobekCode, SolrCode, SolrCode_Facets, SolrCode_Display,  DisplayTerm, FacetTerm, CustomField, canFacetBrowse) 
	values ( 'Series Title','SE','seriestitle','seriestitle_facets','seriestitle','Series Title','Series Title',0,1);
end;
GO

if ( not exists ( select 1 from dbo.SobekCM_Metadata_Types where MetadataName = 'Accessibility' ))
begin
	insert into dbo.SobekCM_Metadata_Types ( MetadataName, SobekCode, SolrCode, SolrCode_Facets, SolrCode_Display, DisplayTerm, FacetTerm, CustomField, canFacetBrowse) 
	values ( 'Accessibility','AC','accessibility','accessibility_facets','accessibility','Accessibility','Accessibility',0,1);
end;
GO

if ( not exists ( select 1 from dbo.SobekCM_Metadata_Types where MetadataName = 'Licensing' ))
begin
	insert into dbo.SobekCM_Metadata_Types ( MetadataName, SobekCode, SolrCode, SolrCode_Facets, SolrCode_Display,  DisplayTerm, FacetTerm, CustomField, canFacetBrowse) 
	values ( 'Licensing','LN','licensing','licensing_facets','licensing','Licensing','Licensing',0,1);
end;
GO

if ( not exists ( select 1 from dbo.SobekCM_Metadata_Types where MetadataName = 'Course Title' ))
begin
	insert into dbo.SobekCM_Metadata_Types ( MetadataName, SobekCode, SolrCode, SolrCode_Facets, SolrCode_Display, DisplayTerm, FacetTerm, CustomField, canFacetBrowse) 
	values ( 'Course Title','CU','coursetitle','coursetitle_facets','coursetitle','Course Title','Course Title',0,1);
end;
GO

if ( not exists ( select 1 from dbo.SobekCM_Item_Viewer_Types where ViewType = 'WEBSITE' ))
begin
  insert into SobekCM_Item_Viewer_Types (ViewType, [Order], DefaultView, MenuOrder)
  values ( 'WEBSITE', 14, 0, 116);
end;
GO
