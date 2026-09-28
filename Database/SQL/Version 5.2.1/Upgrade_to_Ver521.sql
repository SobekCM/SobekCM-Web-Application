-- Upgrade_to_Ver521.sql - OpenSobek
--
-- Takes an existing 5.2.0 SQL Server database and brings it up to Version 5.2.1. If your
-- version is older than 5.2.0, run Upgrade_to_Ver520.sql first.


-- The map browse for the ALL aggregation only showed items with an explicit or implied link to ALL, which is
-- only added when a collection rolls up to ALL through SobekCM_Item_Aggregation_Hierarchy.  Automatically
-- created institutions, and any collection that is not a child of ALL, never add that link, so their
-- items' points were missing from the ALL map.  ALL now returns every item with a point,
-- without going through SobekCM_Item_Aggregation_Item_Link.  Deleted and dark items are now left out of the
-- map browse for every aggregation, ALL included, since neither should be shown publicly.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[SobekCM_Coordinate_Points_By_Aggregation]
	@aggregation_code varchar(20)
AS
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	if ( @aggregation_code = 'ALL' )
	begin
		-- ALL is the root of every hierarchy, and items are not guaranteed to have a link to it (the
		-- link is only added as an implied link when a collection rolls up to ALL, which auto-created
		-- institutions and collections outside the hierarchy never do), so do not go through the link
		-- table.  Every non-deleted, non-dark item with a point counts.
		with min_itemid_per_groupid as
		(
			-- Get the mininmum ItemID per group per coordinate point
			select I.GroupID, F.Point_Latitude, F.Point_Longitude, Min(I.ItemID) as MinItemID
			from SobekCM_Item I, SobekCM_Item_Footprint F
			where ( F.ItemID = I.ItemID )
			  and ( I.Deleted = 'false' )
			  and ( I.Dark = 'false' )
			  and ( F.Point_Latitude is not null )
			  and ( F.Point_Longitude is not null )
			group by I.GroupID, F.Point_Latitude, F.Point_Longitude
		), min_item_thumbnail_per_group as
		(
			-- Get the matching item thumbnail for the item per group per coordiante point
			select G.GroupID, G.Point_Latitude, G.Point_Longitude, I.VID + '/' + I.MainThumbnail as MinThumbnail
			from SobekCM_Item I, min_itemid_per_groupid G
			where G.MinItemID = I.ItemID
		)
		-- Return all matching group/coordinate point, with the group thumbnail, or item thumbnail from above WITH statements
		select F.Point_Latitude, F.Point_Longitude, G.BibID, G.GroupTitle, coalesce(NULLIF(G.GroupThumbnail,''), T.MinThumbnail) as Thumbnail, G.ItemCount, G.[Type]
		from SobekCM_Item_Group G, SobekCM_Item I, SobekCM_Item_Footprint F, min_item_thumbnail_per_group T
		where ( G.GroupID = I.GroupID )
		  and ( F.ItemID = I.ItemID )
		  and ( I.Deleted = 'false' )
		  and ( I.Dark = 'false' )
		  and ( F.Point_Latitude is not null )
		  and ( F.Point_Longitude is not null )
		  and ( T.GroupID = G.GroupID )
		  and ( T.Point_Latitude = F.Point_Latitude )
		  and ( T.Point_Longitude = F.Point_Longitude )
		group by I.Spatial_KML, F.Point_Latitude, F.Point_Longitude, G.BibID, G.GroupTitle, coalesce(NULLIF(G.GroupThumbnail,''), T.MinThumbnail), G.ItemCount, G.[Type]
		order by I.Spatial_KML;
	end
	else
	begin
		-- Return the groups/items/points
		with min_itemid_per_groupid as
		(
			-- Get the mininmum ItemID per group per coordinate point
			select GroupID, F.Point_Latitude, F.Point_Longitude, Min(I.ItemID) as MinItemID
			from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A, SobekCM_Item_Footprint F
			where ( I.ItemID = L.ItemID  )
			  and ( L.AggregationID = A.AggregationID )
			  and ( A.Code = @aggregation_code ) 
			  and ( F.ItemID = I.ItemID )
			  and ( I.Deleted = 'false' )
			  and ( I.Dark = 'false' )
			  and ( F.Point_Latitude is not null )
			  and ( F.Point_Longitude is not null )
			group by GroupID, F.Point_Latitude, F.Point_Longitude
		), min_item_thumbnail_per_group as
		(
		    -- Get the matching item thumbnail for the item per group per coordiante point
			select G.GroupID, G.Point_Latitude, G.Point_Longitude, I.VID + '/' + I.MainThumbnail as MinThumbnail
			from SobekCM_Item I, min_itemid_per_groupid G
			where G.MinItemID = I.ItemID
		)
		-- Return all matchint group/coordinate point, with the group thumbnail, or item thumbnail from above WITH statements
		select F.Point_Latitude, F.Point_Longitude, G.BibID, G.GroupTitle, coalesce(NULLIF(G.GroupThumbnail,''), T.MinThumbnail) as Thumbnail, G.ItemCount, G.[Type]
		from SobekCM_Item_Group G, SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Footprint F, SobekCM_Item_Aggregation A, min_item_thumbnail_per_group T
		where ( G.GroupID = I.GroupID )
		  and ( I.ItemID = L.ItemID  )
		  and ( L.AggregationID = A.AggregationID )
		  and ( A.Code = @aggregation_code ) 
		  and ( F.ItemID = I.ItemID )
		  and ( I.Deleted = 'false' )
		  and ( I.Dark = 'false' )
		  and ( F.Point_Latitude is not null )
		  and ( F.Point_Longitude is not null )
		  and ( T.GroupID = G.GroupID )
		  and ( T.Point_Latitude = F.Point_Latitude )
		  and ( T.Point_Longitude = F.Point_Longitude )
		group by I.Spatial_KML, F.Point_Latitude, F.Point_Longitude, G.BibID, G.GroupTitle, coalesce(NULLIF(G.GroupThumbnail,''), T.MinThumbnail), G.ItemCount, G.[Type]
		order by I.Spatial_KML;
	end;
end;
GO

-- Adds the ExtractTextFromAltoModule Builder module, which reads page-level ALTO OCR files
-- (e.g. 0001.alto.xml) and writes a plain-text sidecar (0001.txt) built from each TextLine's
-- String/CONTENT values -- discarding the coordinate/style/confidence attributes that make up
-- the bulk of an ALTO file, and de-duplicating overlapping sibling String elements (some ALTO
-- producers, e.g. multi-engine OCR merges, leave one per contributing engine's guess for the
-- same word) by keeping whichever has the higher WC. It no-ops for any package with no
-- *.alto.xml files, so this row is safe to enable unconditionally in every deployment.
--
-- Order 52 is deliberate: it runs immediately after ExtractTextFromXmlModule (50), which
-- explicitly skips *.alto.xml (a generic tag-stripper produces garbage against ALTO's
-- word/coordinate structure -- see ExtractTextFromXmlModule.cs), and before OcrTiffsModule
-- (80), whose existing-file check (`if (!File.Exists(text_file))`) will now see the .txt this
-- module just wrote and correctly skip a redundant Tesseract/OCR pass over the companion TIFF.
-- It also runs before CheckForSsnModule (100), so text recovered from ALTO gets the same SSN
-- scan as text from every other source.

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.ExtractTextFromAltoModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Extract indexable full text from page-level ALTO OCR files', 'SobekCM.Builder_Library.Modules.Items.ExtractTextFromAltoModule', 'true', 52);
end;
GO


-- Adds the NewspaperPopulateSerialHierarchyModule Builder module, which generates a newspaper item's
-- serial hierarchy (year/month/day) from its date issued when the incoming package did not already
-- supply one. The generation logic itself lives in Serial_Info.Synchronize_Newspaper_Hierarchy_With_Date_Issued,
-- shared with the online new-item-submission and metadata-editing paths -- this module just invokes it
-- for whatever package the builder is currently processing. It no-ops for non-newspaper items and for
-- newspaper items that already have a hierarchy, so this row is safe to enable unconditionally in every
-- deployment.
--
-- Order 235 is deliberate: it runs after UpdateWebConfigModule (230), and immediately before
-- SaveServiceMetsModule (240), so any hierarchy it generates is captured in the service METS that
-- module writes out.

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.NewspaperPopulateSerialHierarchyModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Generate a newspaper item''s serial hierarchy from its date issued, if missing', 'SobekCM.Builder_Library.Modules.Items.NewspaperPopulateSerialHierarchyModule', 'true', 235);
end;
GO

-- Adds a per-item "serve files locally" flag, SobekCM_Item.Serve_Files_Locally. When set, an item's
-- WHOLE file folder is kept and served from local disk, even under the GCS Hybrid / GCS Full file
-- system modes. It exists for the handful of items whose viewer loads sub-files by relative path
-- (a self-contained web site, an HTML file with its own images, an open textbook), which GCS can
-- not serve.
--
-- This replaces the old rule, where the presence of a WEBSITE / HTML / OPEN_TEXTBOOK /
-- OPEN_DIVISIONS viewer registration alone forced the whole item local. Many items have one of
-- those viewers registered without ever using it, and were wrongly being kept local (and having
-- their masters left off GCS) because of it.
--   * Defaults to 0, so no existing item changes behavior until the backfill below runs.
--   * SobekCM_Get_Item_Details and SobekCM_Builder_Get_Minimum_Item_Information now return it.
--   * SobekCM_Set_Item_Serve_Files_Locally sets it (used by the Edit Item Behaviors screen).
--   * The save procedures are deliberately NOT changed, so re-saving an item from a METS file
--     never clears a flag an administrator set.
--   * One-time backfill, so items that really depend on local serving keep working: every item with
--     a WEBSITE, OPEN_TEXTBOOK or OPEN_DIVISIONS viewer, plus every item with a non-excluded HTML
--     viewer that names an .htm/.html file in its attribute. An HTML viewer with no file named is
--     the "registered but never used" case and is NOT flagged. Review afterwards with:
--       select G.BibID, I.VID from SobekCM_Item I inner join SobekCM_Item_Group G on G.GroupID=I.GroupID where I.Serve_Files_Locally = 1;

if ( not exists ( select 1 from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = 'SobekCM_Item' and COLUMN_NAME = 'Serve_Files_Locally' ))
begin
	ALTER TABLE [dbo].[SobekCM_Item] ADD [Serve_Files_Locally] [bit] NOT NULL CONSTRAINT [DF_SobekCM_Item_Serve_Files_Locally] DEFAULT ((0));
end;
GO


-- Now also returns Serve_Files_Locally in the main item row (third result set)
ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Details]
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
				I.CitationSet, I.MadePublicDate, I.RestrictionMessage, I.Serve_Files_Locally
			from SobekCM_Item as I inner join
				 SobekCM_Item_Group as G on G.GroupID=I.GroupID left outer join
				 Tracking_Item as T on T.ItemID=I.ItemID
			where ( I.ItemID = @ItemID );
		  		
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
			where G.UserGroupID=I.UserGroupID
			  and ItemID=@ItemID;

			-- Return any special user restriction information
			select I.UserID, U.UserName, U.UserID, I.canView, I.isOwner, I.canEditMetadata, I.canEditBehaviors, I.canPerformQc, I.canUploadFiles, I.canChangeVisibility, I.canDelete, I.customPermissions
			from mySobek_User_Item_Permissions I, mySobek_User U
			where U.UserID=I.UserID
			  and ItemID=@ItemID;

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

-- Now also returns Serve_Files_Locally, so the Builder knows which items must stay local
ALTER PROCEDURE [dbo].[SobekCM_Builder_Get_Minimum_Item_Information]
	@bibid varchar(10),
	@vid varchar(5)
AS
begin

	-- No need to perform any locks here.  A slightly dirty read won't hurt much
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Only continue if there is ONE match
	if (( select COUNT(*) from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = @BibID and I.VID = @VID ) = 1 )
	begin
		-- Get the itemid
		declare @ItemID int;
		select @ItemID = ItemID from SobekCM_Item I, SobekCM_Item_Group G where I.GroupID = G.GroupID and G.BibID = @BibID and I.VID = @VID;

		-- Get the item id and mainthumbnail
		select I.ItemID, I.MainThumbnail, I.IP_Restriction_Mask, I.Born_Digital, G.ItemCount, I.Dark, I.MadePublicDate, I.Serve_Files_Locally
		from SobekCM_Item I, SobekCM_Item_Group G
		where ( I.VID = @vid )
		  and ( G.BibID = @bibid )
		  and ( I.GroupID = G.GroupID );

		-- Get the links to the aggregations
		select A.Code, A.Name, A.[Type]
		from SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A
		where ( L.ItemID = @itemid )
		  and ( L.AggregationID = A.AggregationID );
	 
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

		-- Return the viewers for this item
		select T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder) as MenuOrder, V.Exclude, coalesce(V.OrderOverride, T.[Order])
		from SobekCM_Item_Viewers V, SobekCM_Item_Viewer_Types T
		where ( V.ItemID = @ItemID )
		  and ( V.ItemViewTypeID = T.ItemViewTypeID )
		group by T.ViewType, V.Attribute, V.Label, coalesce(V.MenuOrder, T.MenuOrder), V.Exclude, coalesce(V.OrderOverride, T.[Order])
		order by coalesce(V.OrderOverride, T.[Order]) ASC;

		-- Return any special user group restriction information
		select I.UserGroupID, G.GroupName, I.canView, I.isOwner, I.canEditMetadata, I.canEditBehaviors, I.canPerformQc, I.canUploadFiles, I.canChangeVisibility, I.canDelete, I.customPermissions
		from mySobek_User_Group_Item_Permissions I, mySobek_User_Group G
		where G.UserGroupID=I.UserGroupID
		  and ItemID=@ItemID;

		-- Return any special user restriction information
		select I.UserID, U.UserName, U.UserID, I.canView, I.isOwner, I.canEditMetadata, I.canEditBehaviors, I.canPerformQc, I.canUploadFiles, I.canChangeVisibility, I.canDelete, I.customPermissions
		from mySobek_User_Item_Permissions I, mySobek_User U
		where U.UserID=I.UserID
		  and ItemID=@ItemID;
		
	end;

end;
GO

-- Sets or clears the serve-files-locally flag for one item
CREATE OR ALTER PROCEDURE [dbo].[SobekCM_Set_Item_Serve_Files_Locally]
	@itemid int,
	@serve_locally bit
AS
begin
	update SobekCM_Item set Serve_Files_Locally = @serve_locally where ItemID = @itemid;
end;
GO

IF object_id('SobekCM_Get_Item_Aggregation_Statistics') IS NULL EXEC ('create procedure dbo.SobekCM_Get_Item_Aggregation_Statistics as select 1;');
GO

-- Returns the title, item, and page count for a single item aggregation, by code
ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Aggregation_Statistics]
	@code varchar(20)
AS
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	select count(distinct(I.GroupID)) as Title_Count, count(*) as Item_Count, isnull(SUM(I.[PageCount]),0) as Page_Count
	from SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item I, SobekCM_Item_Aggregation A
	where (A.Code = @code)
	  and (A.AggregationID = L.AggregationID)
	  and (L.ItemID = I.ItemID);

end;
GO



/**************************************************************************/
/**                                                                      **/
/**   Update Database Version                                            **/
/**                                                                      **/
/**************************************************************************/

-- Update the version number
if (( select count(*) from SobekCM_Database_Version ) = 0 )
begin
	insert into SobekCM_Database_Version ( Major_Version, Minor_Version, Release_Phase )
	values ( 5, 2, '1' );
end
else
begin
	update SobekCM_Database_Version
	set Major_Version=5, Minor_Version=2, Release_Phase='1';
end;
GO
