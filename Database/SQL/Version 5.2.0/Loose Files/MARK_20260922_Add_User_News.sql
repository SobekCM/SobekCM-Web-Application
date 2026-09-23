-- Adds user news: short HTML messages shown in a banner at the very top of every page, for the
-- users each message targets, until that user closes it.  Once closed, a message is not shown to
-- that user again.  A message can target everyone, including visitors who are not logged on (for
-- example 'The library will be closed on Labor Day'), who close it with a cookie instead.  This
-- also lets upgrade scripts tell the administrators about a new release (see the 5.2.0 release
-- news at the end of this section) without emailing every customer about every patch.
--   * mySobek_News holds each message, who it targets and the dates it is shown between.  It can
--     target everyone, all logged-on users, administrators (system, portal, user and news
--     administrators, but never host administrators) and collection managers
--   * mySobek_News_User_Group_Link also targets a message at the members of specific user groups
--   * mySobek_News_User_Dismissed records each user who closed a message
--   * New procedures: mySobek_Get_Pending_News, mySobek_Dismiss_News, mySobek_Get_All_News,
--     mySobek_Save_News, mySobek_Delete_News and mySobek_Reset_News_Dismissals
--   * New News Administrator role (mySobek_User.IsNewsAdmin): can manage the news, and nothing else.
--     mySobek_Get_User_By_UserID returns it, and mySobek_Update_User gains an optional
--     @is_news_admin parameter (NULL leaves it unchanged, so existing callers are unaffected).
--     This redefines mySobek_Get_User_By_UserID, so it must run after the user active flag change.

if ( not exists ( select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'mySobek_News' ))
begin
	CREATE TABLE [dbo].[mySobek_News](
		[NewsID] [int] IDENTITY(1,1) NOT NULL,
		[Title] [nvarchar](255) NOT NULL,
		[Body] [nvarchar](max) NOT NULL,
		[ForEveryone] [bit] NOT NULL CONSTRAINT [DF_mySobek_News_ForEveryone] DEFAULT ((0)),
		[ForAllUsers] [bit] NOT NULL CONSTRAINT [DF_mySobek_News_ForAllUsers] DEFAULT ((0)),
		[ForAdmins] [bit] NOT NULL CONSTRAINT [DF_mySobek_News_ForAdmins] DEFAULT ((0)),
		[ForCollectionManagers] [bit] NOT NULL CONSTRAINT [DF_mySobek_News_ForCollectionManagers] DEFAULT ((0)),
		[StartDate] [date] NOT NULL CONSTRAINT [DF_mySobek_News_StartDate] DEFAULT (getdate()),
		[EndDate] [date] NULL,
		[IsActive] [bit] NOT NULL CONSTRAINT [DF_mySobek_News_IsActive] DEFAULT ((1)),
		[DateCreated] [datetime] NOT NULL CONSTRAINT [DF_mySobek_News_DateCreated] DEFAULT (getdate()),
		[CreatedBy] [nvarchar](100) NOT NULL CONSTRAINT [DF_mySobek_News_CreatedBy] DEFAULT (''),
		[DateModified] [datetime] NULL,
	 CONSTRAINT [PK_mySobek_News] PRIMARY KEY CLUSTERED ( [NewsID] ASC )
	);
end;
GO

-- No foreign key to mySobek_User_Group, so deleting a user group needs no change.  A link to a
-- deleted group simply never matches anyone.
if ( not exists ( select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'mySobek_News_User_Group_Link' ))
begin
	CREATE TABLE [dbo].[mySobek_News_User_Group_Link](
		[NewsID] [int] NOT NULL,
		[UserGroupID] [int] NOT NULL,
	 CONSTRAINT [PK_mySobek_News_User_Group_Link] PRIMARY KEY CLUSTERED ( [NewsID] ASC, [UserGroupID] ASC ),
	 CONSTRAINT [FK_mySobek_News_User_Group_Link_News] FOREIGN KEY ( [NewsID] ) REFERENCES [dbo].[mySobek_News] ( [NewsID] )
	);
end;
GO

if ( not exists ( select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'mySobek_News_User_Dismissed' ))
begin
	CREATE TABLE [dbo].[mySobek_News_User_Dismissed](
		[NewsID] [int] NOT NULL,
		[UserID] [int] NOT NULL,
		[DateDismissed] [datetime] NOT NULL CONSTRAINT [DF_mySobek_News_User_Dismissed_Date] DEFAULT (getdate()),
	 CONSTRAINT [PK_mySobek_News_User_Dismissed] PRIMARY KEY CLUSTERED ( [UserID] ASC, [NewsID] ASC ),
	 CONSTRAINT [FK_mySobek_News_User_Dismissed_News] FOREIGN KEY ( [NewsID] ) REFERENCES [dbo].[mySobek_News] ( [NewsID] )
	);
end;
GO

-- The News Administrator role
if ( not exists ( select 1 from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = 'mySobek_User' and COLUMN_NAME = 'IsNewsAdmin' ))
begin
	ALTER TABLE [dbo].[mySobek_User] ADD [IsNewsAdmin] [bit] NOT NULL CONSTRAINT [DF_mySobek_User_IsNewsAdmin] DEFAULT ((0));
end;
GO

-- Procedures keep the SET options in effect when they are created. sqlcmd defaults QUOTED_IDENTIFIER
-- to OFF, which breaks deletes against tables with filtered indexes, so set both explicitly.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns IsNewsAdmin as well.  Redefines the version from the user active flag change.
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
	  AuthenticationSource, isActive, IsNewsAdmin
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

-- Edits the permission flags for a user.  @is_news_admin is optional, and NULL leaves it unchanged.
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
      @clear_user_groups bit,
      @is_news_admin bit = null
AS
begin transaction

      -- Update the simple table values
      update mySobek_User
      set Can_Submit_Items=@can_submit, Internal_User=@is_internal,
            IsPortalAdmin=@is_portal_admin, IsSystemAdmin=@is_system_admin,
            Include_Tracking_Standard_Forms=@include_tracking_standard_forms,
            EditTemplate=@edit_template, Can_Delete_All_Items = @can_delete_all,
            EditTemplateMarc=@edit_template_marc, IsHostAdmin=@is_host_admin,
			IsUserAdmin=@is_user_admin, IsNewsAdmin=coalesce(@is_news_admin, IsNewsAdmin)
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

IF object_id('mySobek_Get_Pending_News') IS NULL EXEC ('create procedure dbo.mySobek_Get_Pending_News as select 1;');
GO

-- Gets the active news, within its display dates, that this user has not closed yet.  Who each
-- message targets is returned too, and the application picks the ones that apply to this user,
-- using the same role flags it uses for everything else.  Newest first.  Pass -1 to get only the
-- news for everyone, for visitors who are not logged on.
ALTER PROCEDURE [dbo].[mySobek_Get_Pending_News]
	@userid int
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select N.NewsID, N.Title, N.Body, N.ForEveryone, N.ForAllUsers, N.ForAdmins, N.ForCollectionManagers,
	  N.StartDate, N.EndDate, N.IsActive,
	  UserGroupIDs=coalesce(( select STRING_AGG(cast(L.UserGroupID as varchar(12)), ',') from mySobek_News_User_Group_Link L where L.NewsID = N.NewsID ), '')
	from mySobek_News N
	where ( N.IsActive = 'true' )
	  and ( N.StartDate <= cast(getdate() as date))
	  and (( N.EndDate is null ) or ( N.EndDate >= cast(getdate() as date)))
	  and (( @userid > 0 ) or ( N.ForEveryone = 'true' ))
	  and ( not exists ( select 1 from mySobek_News_User_Dismissed D where D.UserID = @userid and D.NewsID = N.NewsID ))
	order by N.StartDate DESC, N.NewsID DESC;
END;
GO

IF object_id('mySobek_Dismiss_News') IS NULL EXEC ('create procedure dbo.mySobek_Dismiss_News as select 1;');
GO

-- Records that a user closed a news message, so it is not shown to them again
ALTER PROCEDURE [dbo].[mySobek_Dismiss_News]
	@userid int,
	@newsid int
AS
BEGIN
	if (( exists ( select 1 from mySobek_News where NewsID = @newsid )) and ( not exists ( select 1 from mySobek_News_User_Dismissed where UserID = @userid and NewsID = @newsid )))
	begin
		insert into mySobek_News_User_Dismissed ( NewsID, UserID, DateDismissed )
		values ( @newsid, @userid, getdate());
	end;
END;
GO

IF object_id('mySobek_Get_All_News') IS NULL EXEC ('create procedure dbo.mySobek_Get_All_News as select 1;');
GO

-- Gets every news message, for the news admin screen, with how many users have closed each one
ALTER PROCEDURE [dbo].[mySobek_Get_All_News]
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select N.NewsID, N.Title, N.Body, N.ForEveryone, N.ForAllUsers, N.ForAdmins, N.ForCollectionManagers,
	  N.StartDate, N.EndDate, N.IsActive, N.DateCreated, N.CreatedBy, N.DateModified,
	  UserGroupIDs=coalesce(( select STRING_AGG(cast(L.UserGroupID as varchar(12)), ',') from mySobek_News_User_Group_Link L where L.NewsID = N.NewsID ), ''),
	  DismissedCount=( select count(*) from mySobek_News_User_Dismissed D where D.NewsID = N.NewsID )
	from mySobek_News N
	order by N.StartDate DESC, N.NewsID DESC;
END;
GO

IF object_id('mySobek_Save_News') IS NULL EXEC ('create procedure dbo.mySobek_Save_News as select 1;');
GO

-- Adds a new news message (when @newsid does not exist yet) or edits an existing one.  @usergroupids
-- is a comma-separated list of the user groups targeted, and replaces any existing group links.
-- Editing a message does not show it again to users who already closed it; call
-- mySobek_Reset_News_Dismissals for that.
ALTER PROCEDURE [dbo].[mySobek_Save_News]
	@newsid int,
	@title nvarchar(255),
	@body nvarchar(max),
	@foreveryone bit,
	@forallusers bit,
	@foradmins bit,
	@forcollectionmanagers bit,
	@startdate date,
	@enddate date,
	@isactive bit,
	@usergroupids varchar(max),
	@username nvarchar(100),
	@newid int output
AS
BEGIN
	SET NOCOUNT ON;

	BEGIN TRANSACTION;

	if ( exists ( select 1 from mySobek_News where NewsID = @newsid ))
	begin
		update mySobek_News
		set Title=@title, Body=@body, ForEveryone=@foreveryone, ForAllUsers=@forallusers, ForAdmins=@foradmins,
		    ForCollectionManagers=@forcollectionmanagers, StartDate=coalesce(@startdate, StartDate), EndDate=@enddate,
		    IsActive=@isactive, DateModified=getdate()
		where NewsID = @newsid;

		set @newid = @newsid;
	end
	else
	begin
		insert into mySobek_News ( Title, Body, ForEveryone, ForAllUsers, ForAdmins, ForCollectionManagers,
		    StartDate, EndDate, IsActive, DateCreated, CreatedBy )
		values ( @title, @body, @foreveryone, @forallusers, @foradmins, @forcollectionmanagers,
		    coalesce(@startdate, cast(getdate() as date)), @enddate, @isactive, getdate(), coalesce(@username, ''));

		set @newid = SCOPE_IDENTITY();
	end;

	-- Replace the user group links (parsed by hand, since STRING_SPLIT needs compatibility level 130)
	delete from mySobek_News_User_Group_Link where NewsID = @newid;

	declare @list varchar(max) = coalesce(@usergroupids, '') + ',';
	declare @pos int = charindex(',', @list);
	declare @groupid int;
	while ( @pos > 0 )
	begin
		set @groupid = TRY_CAST(nullif(ltrim(rtrim(left(@list, @pos - 1))), '') as int);
		set @list = substring(@list, @pos + 1, len(@list) + 1);

		if (( @groupid is not null ) and ( not exists ( select 1 from mySobek_News_User_Group_Link where NewsID = @newid and UserGroupID = @groupid )))
		begin
			insert into mySobek_News_User_Group_Link ( NewsID, UserGroupID )
			values ( @newid, @groupid );
		end;

		set @pos = charindex(',', @list);
	end;

	COMMIT TRANSACTION;
END;
GO

IF object_id('mySobek_Delete_News') IS NULL EXEC ('create procedure dbo.mySobek_Delete_News as select 1;');
GO

-- Deletes a news message, along with its user group links and the record of who closed it
ALTER PROCEDURE [dbo].[mySobek_Delete_News]
	@newsid int
AS
BEGIN
	delete from mySobek_News_User_Dismissed where NewsID = @newsid;
	delete from mySobek_News_User_Group_Link where NewsID = @newsid;
	delete from mySobek_News where NewsID = @newsid;
END;
GO

IF object_id('mySobek_Reset_News_Dismissals') IS NULL EXEC ('create procedure dbo.mySobek_Reset_News_Dismissals as select 1;');
GO

-- Forgets who closed a news message, so it is shown again to everyone it targets
ALTER PROCEDURE [dbo].[mySobek_Reset_News_Dismissals]
	@newsid int
AS
BEGIN
	delete from mySobek_News_User_Dismissed where NewsID = @newsid;
END;
GO

-- Release news for the administrators.  Each upgrade script can add one of these.  The title
-- check means running the script twice does not add it twice.  It stops showing after 90 days,
-- so administrators added long after the upgrade are not told about it.
if ( not exists ( select 1 from mySobek_News where Title = 'SobekCM has been upgraded to version 5.2.0' ))
begin
	declare @news511 int;
	declare @news511_end date = dateadd(day, 90, getdate());
	exec mySobek_Save_News -1, 'SobekCM has been upgraded to version 5.2.0',
		'<p>This site is now running SobekCM 5.2.0, a new release. Changes you may notice:</p><ul><li><strong>Site news:</strong> messages like this one now appear at the top of the page for the people they are meant for, until each person closes them. Use <em>Admin &gt; Site News</em> to post your own, for everyone (such as a holiday closing) or just certain users. The new <em>News Administrator</em> role lets someone manage the news without any other administrative rights.</li><li><strong>Collection result fields:</strong> the <em>Results</em> tab of each collection can now choose the fields shown with each title in the brief view and the thumbnail tooltip.</li><li><strong>Inactive users:</strong> the users admin screen can now deactivate a user, who can then no longer log on.</li><li><strong>New institutions</strong> added automatically when an item is loaded now get the same facets, result views and permissions as any other new collection.</li></ul><p>For more information, see the <a href="https://sobekrepository.org/sobekcm/currentversion/">Release Notes</a>.</p>',
		'false', 'false', 'true', 'false',
		null, @news511_end, 'true', '', 'Upgrade_to_Ver520.sql', @news511 output;
end;
GO
