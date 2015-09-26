
-- Procedure returns the items by a coordinate search
ALTER PROCEDURE [dbo].[SobekCM_Get_Items_By_Coordinates]
	@lat1 float,
	@long1 float,
	@lat2 float,
	@long2 float,
	@include_private bit,
	@aggregationcode varchar(20),
	@pagesize int, 
	@pagenumber int,
	@sort int,	
	@minpagelookahead int,
	@maxpagelookahead int,
	@lookahead_factor float,
	@include_facets bit,
	@facettype1 smallint,
	@facettype2 smallint,
	@facettype3 smallint,
	@facettype4 smallint,
	@facettype5 smallint,
	@facettype6 smallint,
	@facettype7 smallint,
	@facettype8 smallint,
	@total_items int output,
	@total_titles int output
AS
BEGIN

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Create the temporary tables first
	-- Create the temporary table to hold all the item id's
	create table #TEMPSUBZERO ( ItemID int );
	create table #TEMPZERO ( ItemID int );
	create table #TEMP_ITEMS ( ItemID int, fk_TitleID int, SortDate bigint, Spatial_KML varchar(4000), Spatial_KML_Distance float );

	-- Is this really just a point search?
	if (( isnull(@lat2,1000) = 1000 ) or ( isnull(@long2,1000) = 1000 ) or (( @lat1=@lat2 ) and ( @long1=@long2 )))
	begin

		-- Select all matching item ids
		insert into #TEMPZERO
		select distinct(itemid) 
		from SobekCM_Item_Footprint
		where (( Point_Latitude = @lat1 ) and ( Point_Longitude = @long1 ))
		   or (((( Rect_Latitude_A >= @lat1 ) and ( Rect_Latitude_B <= @lat1 )) or (( Rect_Latitude_A <= @lat1 ) and ( Rect_Latitude_B >= @lat1)))
	        and((( Rect_Longitude_A >= @long1 ) and ( Rect_Longitude_B <= @long1 )) or (( Rect_Longitude_A <= @long1 ) and ( Rect_Longitude_B >= @long1 ))));

	end
	else
	begin

		-- Select all matching item ids by rectangle
		insert into #TEMPSUBZERO
		select distinct(itemid)
		from SobekCM_Item_Footprint
		where ((( Point_Latitude <= @lat1 ) and ( Point_Latitude >= @lat2 )) or (( Point_Latitude >= @lat1 ) and ( Point_Latitude <= @lat2 )))
		  and ((( Point_Longitude <= @long1 ) and ( Point_Longitude >= @long2 )) or (( Point_Longitude >= @long1 ) and ( Point_Longitude <= @long2 )));
		


		-- Select rectangles which OVERLAP with this rectangle
		insert into #TEMPSUBZERO
		select distinct(itemid)
		from SobekCM_Item_Footprint
		where (((( Rect_Latitude_A >= @lat1 ) and ( Rect_Latitude_A <= @lat2 )) or (( Rect_Latitude_A <= @lat1 ) and ( Rect_Latitude_A >= @lat2 )))
			or ((( Rect_Latitude_B >= @lat1 ) and ( Rect_Latitude_B <= @lat2 )) or (( Rect_Latitude_B <= @lat1 ) and ( Rect_Latitude_B >= @lat2 ))))
		  and (((( Rect_Longitude_A >= @long1 ) and ( Rect_Longitude_A <= @long2 )) or (( Rect_Longitude_A <= @long1 ) and ( Rect_Longitude_A >= @long2 )))
			or ((( Rect_Longitude_B >= @long1 ) and ( Rect_Longitude_B <= @long2 )) or (( Rect_Longitude_B <= @long1 ) and ( Rect_Longitude_B >= @long2 ))));
		
		-- Select rectangles that INCLUDE this rectangle by picking overlaps with one point
		insert into #TEMPSUBZERO
		select distinct(itemid)
		from SobekCM_Item_Footprint
		where ((( @lat1 <= Rect_Latitude_A ) and ( @lat1 >= Rect_Latitude_B )) or (( @lat1 >= Rect_Latitude_A ) and ( @lat1 <= Rect_Latitude_B )))
		  and ((( @long1 <= Rect_Longitude_A ) and ( @long1 >= Rect_Longitude_B )) or (( @long1 >= Rect_Longitude_A ) and ( @long1 <= Rect_Longitude_B )));

		-- Make sure uniqueness applies here as well
		insert into #TEMPZERO
		select distinct(itemid)
		from #TEMPSUBZERO;

	end;
	
	-- Determine the start and end rows
	declare @rowstart int;
	declare @rowend int; 
	set @rowstart = (@pagesize * ( @pagenumber - 1 )) + 1;
	set @rowend = @rowstart + @pagesize - 1; 

	-- Set value for filtering privates
	declare @lower_mask int;
	set @lower_mask = 0;
	if ( @include_private = 'true' )
	begin
		set @lower_mask = -256;
	end;

	-- Determine the aggregationid
	declare @aggregationid int;
	set @aggregationid = coalesce(( select AggregationID from SobekCM_Item_Aggregation where Code=@aggregationcode ), -1);

	-- Get the sql which will be used to return the aggregation-specific display values for all the items in this page of results
	declare @item_display_sql nvarchar(max);
	if ( @aggregationid < 0 )
	begin
		select @item_display_sql=coalesce(Browse_Results_Display_SQL, 'select S.ItemID, S.Publication_Date, S.Creator, S.[Publisher.Display], S.Format, S.Edition, S.Material, S.Measurements, S.Style_Period, S.Technique, S.[Subjects.Display], S.Source_Institution, S.Donor from SobekCM_Metadata_Basic_Search_Table S, @itemtable T where S.ItemID = T.ItemID order by T.RowNumber;')
		from SobekCM_Item_Aggregation
		where Code='all';
	end
	else
	begin
		select @item_display_sql=coalesce(Browse_Results_Display_SQL, 'select S.ItemID, S.Publication_Date, S.Creator, S.[Publisher.Display], S.Format, S.Edition, S.Material, S.Measurements, S.Style_Period, S.Technique, S.[Subjects.Display], S.Source_Institution, S.Donor from SobekCM_Metadata_Basic_Search_Table S, @itemtable T where S.ItemID = T.ItemID order by T.RowNumber;')
		from SobekCM_Item_Aggregation
		where AggregationID=@aggregationid;
	end;
			
	-- Was an aggregation included?
	if ( LEN(ISNULL( @aggregationcode,'' )) > 0 )
	begin	
		-- Look for matching the provided aggregation
		insert into #TEMP_ITEMS ( ItemID, fk_TitleID, SortDate, Spatial_KML, Spatial_KML_Distance )
		select I.ItemID, I.GroupID, SortDate=isnull( I.SortDate,-1), Spatial_KML=isnull(Spatial_KML,''), Spatial_KML_Distance
		from #TEMPZERO T1, SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link CL
		where ( CL.ItemID = I.ItemID )
		  and ( CL.AggregationID = @aggregationid )
		  and ( I.Deleted = 'false' )
		  and ( T1.ItemID = I.ItemID )
		  and ( I.IP_Restriction_Mask >= @lower_mask );
	end
	else
	begin	
		-- Look for matching the provided aggregation
		insert into #TEMP_ITEMS ( ItemID, fk_TitleID, SortDate, Spatial_KML, Spatial_KML_Distance )
		select I.ItemID, I.GroupID, SortDate=isnull( I.SortDate,-1), Spatial_KML=isnull(Spatial_KML,''), Spatial_KML_Distance
		from #TEMPZERO T1, SobekCM_Item I
		where ( I.Deleted = 'false' )
		  and ( T1.ItemID = I.ItemID )
		  and ( I.IP_Restriction_Mask >= @lower_mask );
	end;
	

	
	-- Create the temporary item table variable for paging purposes
	declare @TEMP_PAGED_ITEMS TempPagedItemsTableType;
	
	-- There are essentially THREE major paths of execution, depending on whether this should
	-- be grouped as items within the page requested titles ( sorting by title or the basic
	-- sorting by rank, which ranks this way ) or whether each item should be
	-- returned by itself, such as sorting by individual publication dates, etc..
	-- The default sort for this search is by spatial coordiantes, in which case the same 
	-- title should appear multiple times, if the items in the volume have different coordinates
	
	if ( @sort = 0 )
	begin
		-- create the temporary title table definition
		create table #TEMP_TITLES_ITEMS ( TitleID int, BibID varchar(10), RowNumber int, Spatial_KML varchar(4000), Spatial_Distance float );
		
		-- Compute the number of seperate titles/coordinates
		select fk_TitleID, (COUNT(Spatial_KML)) as assign_value
		into #TEMP1
		from #TEMP_ITEMS I
		group by fk_TitleID, Spatial_KML;
		
		-- Get the TOTAL count of spatial_kmls
		select @total_titles = isnull(SUM(assign_value), 0) from #TEMP1;
		drop table #TEMP1;
		
		-- Total items is simpler to computer
		select @total_items = COUNT(*) from #TEMP_ITEMS;	
		
		-- For now, always return the max lookahead pages
		set @rowend = @rowstart + ( @pagesize * @maxpagelookahead ) - 1; 
		
		-- Create saved select across titles for row numbers
		with TITLES_SELECT AS
			(	select GroupID, G.BibID, Spatial_KML, Spatial_KML_Distance,
					ROW_NUMBER() OVER (order by Spatial_KML_Distance ASC, Spatial_KML ASC) as RowNumber
				from #TEMP_ITEMS I, SobekCM_Item_Group G
				where I.fk_TitleID = G.GroupID
				group by G.GroupID, G.BibID, G.SortTitle, Spatial_KML, Spatial_KML_Distance )

		-- Insert the correct rows into the temp title table	
		insert into #TEMP_TITLES_ITEMS ( TitleID, BibID, RowNumber, Spatial_KML, Spatial_Distance )
		select GroupID, BibID, RowNumber, Spatial_KML, Spatial_KML_Distance
		from TITLES_SELECT
		where RowNumber >= @rowstart
		  and RowNumber <= @rowend;
		  
		-- Return the title information for this page
		select RowNumber as TitleID, T.BibID, G.GroupTitle, G.ALEPH_Number as OPAC_Number, G.OCLC_Number, isnull(G.GroupThumbnail,'') as GroupThumbnail, G.[Type], isnull(G.Primary_Identifier_Type,'') as Primary_Identifier_Type, isnull(G.Primary_Identifier, '') as Primary_Identifier, Spatial_KML, Spatial_Distance
		from #TEMP_TITLES_ITEMS T, SobekCM_Item_Group G
		where ( T.TitleID = G.GroupID )
		order by RowNumber ASC;
		
		-- Get the item id's for the items related to these titles (using rownumber as the new group id)
		insert into @TEMP_PAGED_ITEMS
		select I.ItemID, RowNumber
		from #TEMP_TITLES_ITEMS T, #TEMP_ITEMS M, SobekCM_Item I
		where ( T.TitleID = M.fk_TitleID )
		  and ( M.ItemID = I.ItemID )
		  and ( M.Spatial_KML = T.Spatial_KML )
		  and ( M.Spatial_KML_Distance = T.Spatial_Distance );  
			
		-- Return the basic system required item information for this page of results
		select T.RowNumber as fk_TitleID, I.ItemID, VID, Title, IP_Restriction_Mask, coalesce(I.MainThumbnail,'') as MainThumbnail, coalesce(I.Level1_Index, -1) as Level1_Index, coalesce(I.Level1_Text,'') as Level1_Text, coalesce(I.Level2_Index, -1) as Level2_Index, coalesce(I.Level2_Text,'') as Level2_Text, coalesce(I.Level3_Index,-1) as Level3_Index, coalesce(I.Level3_Text,'') as Level3_Text, isnull(I.PubDate,'') as PubDate, I.[PageCount], coalesce(I.Link,'') as Link, coalesce( Spatial_KML, '') as Spatial_KML, coalesce(COinS_OpenURL, '') as COinS_OpenURL		
		from SobekCM_Item I, @TEMP_PAGED_ITEMS T
		where ( T.ItemID = I.ItemID )
		order by T.RowNumber, Level1_Index, Level2_Index, Level3_Index;			
								
		-- Return the aggregation-specific display values for all the items in this page of results
		execute sp_Executesql @item_display_sql, N' @itemtable TempPagedItemsTableType READONLY', @TEMP_PAGED_ITEMS; 
		
		-- drop the temporary table
		drop table #TEMP_TITLES_ITEMS;	
	end;
	
	if (( @sort < 10 ) and ( @sort > 0 ))
	begin	
		-- create the temporary title table definition
		create table #TEMP_TITLES ( TitleID int, BibID varchar(10), RowNumber int );

		-- Get the total counts
		select @total_items=COUNT(*), @total_titles=COUNT(distinct fk_TitleID)
		from #TEMP_ITEMS; 

		-- If there are some titles, continue
		if ( @total_titles > 0 )
		begin
		
			-- Now, calculate the actual ending row, based on the ration, page information,
			-- and the lookahead factor
		
			-- Compute equation to determine possible page value ( max - log(factor, (items/title)/2))
			declare @computed_value int;
			select @computed_value = (@maxpagelookahead - CEILING( LOG10( ((cast(@total_items as float)) / (cast(@total_titles as float)))/@lookahead_factor)));
		
			-- Compute the minimum value.  This cannot be less than @minpagelookahead.
			declare @floored_value int;
			select @floored_value = 0.5 * ((@computed_value + @minpagelookahead) + ABS(@computed_value - @minpagelookahead));
		
			-- Compute the maximum value.  This cannot be more than @maxpagelookahead.
			declare @actual_pages int;
			select @actual_pages = 0.5 * ((@floored_value + @maxpagelookahead) - ABS(@floored_value - @maxpagelookahead)); 

			-- Set the final row again then
			set @rowend = @rowstart + ( @pagesize * @actual_pages ) - 1; 		
		  
			-- Create saved select across titles for row numbers
			with TITLES_SELECT AS
				(	select GroupID, G.BibID, 
						ROW_NUMBER() OVER (order by case when @sort=1 THEN G.SortTitle end ASC,											
													case when @sort=2 THEN BibID end ASC,
													case when @sort=3 THEN BibID end DESC) as RowNumber
					from #TEMP_ITEMS I, SobekCM_Item_Group G
					where I.fk_TitleID = G.GroupID
					group by G.GroupID, G.BibID, G.SortTitle )

			-- Insert the correct rows into the temp title table	
			insert into #TEMP_TITLES ( TitleID, BibID, RowNumber )
			select GroupID, BibID, RowNumber
			from TITLES_SELECT
			where RowNumber >= @rowstart
			  and RowNumber <= @rowend;
	
			-- Return the title information for this page
			select RowNumber as TitleID, T.BibID, G.GroupTitle, G.ALEPH_Number, G.OCLC_Number, isnull(G.GroupThumbnail,'') as GroupThumbnail, G.[Type], isnull(G.Primary_Identifier_Type,'') as Primary_Identifier_Type, isnull(G.Primary_Identifier, '') as Primary_Identifier
			from #TEMP_TITLES T, SobekCM_Item_Group G
			where ( T.TitleID = G.GroupID )
			order by RowNumber ASC;
		
			-- Get the item id's for the items related to these titles
			insert into @TEMP_PAGED_ITEMS
			select ItemID, RowNumber
			from #TEMP_TITLES T, SobekCM_Item I
			where ( T.TitleID = I.GroupID );			  
			
			-- Return the basic system required item information for this page of results
			select T.RowNumber as fk_TitleID, I.ItemID, VID, Title, IP_Restriction_Mask, coalesce(I.MainThumbnail,'') as MainThumbnail, coalesce(I.Level1_Index, -1) as Level1_Index, coalesce(I.Level1_Text,'') as Level1_Text, coalesce(I.Level2_Index, -1) as Level2_Index, coalesce(I.Level2_Text,'') as Level2_Text, coalesce(I.Level3_Index,-1) as Level3_Index, coalesce(I.Level3_Text,'') as Level3_Text, isnull(I.PubDate,'') as PubDate, I.[PageCount], coalesce(I.Link,'') as Link, coalesce( Spatial_KML, '') as Spatial_KML, coalesce(COinS_OpenURL, '') as COinS_OpenURL		
			from SobekCM_Item I, @TEMP_PAGED_ITEMS T
			where ( T.ItemID = I.ItemID )
			order by T.RowNumber, Level1_Index, Level2_Index, Level3_Index;			
								
			-- Return the aggregation-specific display values for all the items in this page of results
			execute sp_Executesql @item_display_sql, N' @itemtable TempPagedItemsTableType READONLY', @TEMP_PAGED_ITEMS; 	
		
			-- drop the temporary table
			drop table #TEMP_TITLES;

		end;
	end;
	
	if ( @sort >= 10 )
	begin	
		-- Since these sorts make each item paired with a single title row,
		-- number of items and titles are equal
		select @total_items=COUNT(*), @total_titles=COUNT(*)
		from #TEMP_ITEMS; 
		
		-- In addition, always return the max lookahead pages
		set @rowend = @rowstart + ( @pagesize * @maxpagelookahead ) - 1; 
		
		-- Create saved select across items for row numbers
		with ITEMS_SELECT AS
		 (	select I.ItemID, 
				ROW_NUMBER() OVER (order by case when @sort=10 THEN SortDate end ASC,
											case when @sort=11 THEN SortDate end DESC) as RowNumber
				from #TEMP_ITEMS I
				group by I.ItemID, SortDate )
					  
		-- Insert the correct rows into the temp item table	
		insert into @TEMP_PAGED_ITEMS ( ItemID, RowNumber )
		select ItemID, RowNumber
		from ITEMS_SELECT
		where RowNumber >= @rowstart
		  and RowNumber <= @rowend;
		  
		-- Return the title information for this page
		select RowNumber as TitleID, G.BibID, G.GroupTitle, G.ALEPH_Number, G.OCLC_Number, isnull(G.GroupThumbnail,'') as GroupThumbnail, G.[Type], isnull(G.Primary_Identifier_Type,'') as Primary_Identifier_Type, isnull(G.Primary_Identifier, '') as Primary_Identifier
		from @TEMP_PAGED_ITEMS T, SobekCM_Item I, SobekCM_Item_Group G
		where ( T.ItemID = I.ItemID )
		  and ( I.GroupID = G.GroupID )
		order by RowNumber ASC;
		
		-- Return the basic system required item information for this page of results
		select T.RowNumber as fk_TitleID, I.ItemID, VID, Title, IP_Restriction_Mask, coalesce(I.MainThumbnail,'') as MainThumbnail, coalesce(I.Level1_Index, -1) as Level1_Index, coalesce(I.Level1_Text,'') as Level1_Text, coalesce(I.Level2_Index, -1) as Level2_Index, coalesce(I.Level2_Text,'') as Level2_Text, coalesce(I.Level3_Index,-1) as Level3_Index, coalesce(I.Level3_Text,'') as Level3_Text, isnull(I.PubDate,'') as PubDate, I.[PageCount], coalesce(I.Link,'') as Link, coalesce( Spatial_KML, '') as Spatial_KML, coalesce(COinS_OpenURL, '') as COinS_OpenURL		
		from SobekCM_Item I, @TEMP_PAGED_ITEMS T
		where ( T.ItemID = I.ItemID )
		order by T.RowNumber, Level1_Index, Level2_Index, Level3_Index;			
			
		-- Return the aggregation-specific display values for all the items in this page of results
		execute sp_Executesql @item_display_sql, N' @itemtable TempPagedItemsTableType READONLY', @TEMP_PAGED_ITEMS; 
	end;
	
	-- Return the facets if asked for
	if ( @include_facets = 'true' )
	begin	
		-- Only return the aggregation codes if this was a search across all collections	
		if (( LEN( isnull( @aggregationcode, '')) = 0 ) or ( @aggregationcode='all'))
		begin
			-- Build the aggregation list
			select A.Code, A.ShortName, Metadata_Count=Count(*)
			from SobekCM_Item_Aggregation A, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item I, #TEMP_ITEMS T
			where ( T.ItemID = I.ItemID )
			  and ( I.ItemID = L.ItemID )
			  and ( L.AggregationID = A.AggregationID )
			  and ( A.Hidden = 'false' )
			  and ( A.isActive = 'true' )
			  and ( A.Include_In_Collection_Facet = 'true' )
			group by A.Code, A.ShortName
			order by Metadata_Count DESC, ShortName ASC;	
		end;	
		
		-- Return the FIRST facet
		if ( @facettype1 > 0 )
		begin
			-- Return the first 100 values
			select MetadataValue, Metadata_Count
			from (	select top(100) U.MetadataID, Metadata_Count = COUNT(*)
					from #TEMP_ITEMS I, Metadata_Item_Link_Indexed_View U with (NOEXPAND)
					where ( U.ItemID = I.ItemID )
					  and ( U.MetadataTypeID = @facettype1 )
					group by U.MetadataID
					order by Metadata_Count DESC ) F, SobekCM_Metadata_Unique_Search_Table M
			where M.MetadataID = F.MetadataID
			order by Metadata_Count DESC, MetadataValue ASC;
		end;
		
		-- Return the SECOND facet
		if ( @facettype2 > 0 )
		begin
			-- Return the first 100 values
			select MetadataValue, Metadata_Count
			from (	select top(100) U.MetadataID, Metadata_Count = COUNT(*)
					from #TEMP_ITEMS I, Metadata_Item_Link_Indexed_View U with (NOEXPAND)
					where ( U.ItemID = I.ItemID )
					  and ( U.MetadataTypeID = @facettype2 )
					group by U.MetadataID
					order by Metadata_Count DESC ) F, SobekCM_Metadata_Unique_Search_Table M
			where M.MetadataID = F.MetadataID
			order by Metadata_Count DESC, MetadataValue ASC;
		end;
		
		-- Return the THIRD facet
		if ( @facettype3 > 0 )
		begin
			-- Return the first 100 values
			select MetadataValue, Metadata_Count
			from (	select top(100) U.MetadataID, Metadata_Count = COUNT(*)
					from #TEMP_ITEMS I, Metadata_Item_Link_Indexed_View U with (NOEXPAND)
					where ( U.ItemID = I.ItemID )
					  and ( U.MetadataTypeID = @facettype3 )
					group by U.MetadataID
					order by Metadata_Count DESC ) F, SobekCM_Metadata_Unique_Search_Table M
			where M.MetadataID = F.MetadataID
			order by Metadata_Count DESC, MetadataValue ASC;
		end;	
		
		-- Return the FOURTH facet
		if ( @facettype4 > 0 )
		begin
			-- Return the first 100 values
			select MetadataValue, Metadata_Count
			from (	select top(100) U.MetadataID, Metadata_Count = COUNT(*)
					from #TEMP_ITEMS I, Metadata_Item_Link_Indexed_View U with (NOEXPAND)
					where ( U.ItemID = I.ItemID )
					  and ( U.MetadataTypeID = @facettype4 )
					group by U.MetadataID
					order by Metadata_Count DESC ) F, SobekCM_Metadata_Unique_Search_Table M
			where M.MetadataID = F.MetadataID
			order by Metadata_Count DESC, MetadataValue ASC;
		end;
		
		-- Return the FIFTH facet
		if ( @facettype5 > 0 )
		begin
			-- Return the first 100 values
			select MetadataValue, Metadata_Count
			from (	select top(100) U.MetadataID, Metadata_Count = COUNT(*)
					from #TEMP_ITEMS I, Metadata_Item_Link_Indexed_View U with (NOEXPAND)
					where ( U.ItemID = I.ItemID )
					  and ( U.MetadataTypeID = @facettype5 )
					group by U.MetadataID
					order by Metadata_Count DESC ) F, SobekCM_Metadata_Unique_Search_Table M
			where M.MetadataID = F.MetadataID
			order by Metadata_Count DESC, MetadataValue ASC;
		end;
		
		-- Return the SIXTH facet
		if ( @facettype6 > 0 )
		begin
			-- Return the first 100 values
			select MetadataValue, Metadata_Count
			from (	select top(100) U.MetadataID, Metadata_Count = COUNT(*)
					from #TEMP_ITEMS I, Metadata_Item_Link_Indexed_View U with (NOEXPAND)
					where ( U.ItemID = I.ItemID )
					  and ( U.MetadataTypeID = @facettype6 )
					group by U.MetadataID
					order by Metadata_Count DESC ) F, SobekCM_Metadata_Unique_Search_Table M
			where M.MetadataID = F.MetadataID
			order by Metadata_Count DESC, MetadataValue ASC;
		end;
		
		-- Return the SEVENTH facet
		if ( @facettype7 > 0 )
		begin
			-- Return the first 100 values
			select MetadataValue, Metadata_Count
			from (	select top(100) U.MetadataID, Metadata_Count = COUNT(*)
					from #TEMP_ITEMS I, Metadata_Item_Link_Indexed_View U with (NOEXPAND)
					where ( U.ItemID = I.ItemID )
					  and ( U.MetadataTypeID = @facettype7 )
					group by U.MetadataID
					order by Metadata_Count DESC ) F, SobekCM_Metadata_Unique_Search_Table M
			where M.MetadataID = F.MetadataID
			order by Metadata_Count DESC, MetadataValue ASC;
		end;
		
		-- Return the EIGHTH facet
		if ( @facettype8 > 0 )
		begin
			-- Return the first 100 values
			select MetadataValue, Metadata_Count
			from (	select top(100) U.MetadataID, Metadata_Count = COUNT(*)
					from #TEMP_ITEMS I, Metadata_Item_Link_Indexed_View U with (NOEXPAND)
					where ( U.ItemID = I.ItemID )
					  and ( U.MetadataTypeID = @facettype8 )
					group by U.MetadataID
					order by Metadata_Count DESC ) F, SobekCM_Metadata_Unique_Search_Table M
			where M.MetadataID = F.MetadataID
			order by Metadata_Count DESC, MetadataValue ASC;
		end;
	end;

	-- drop the temporary tables
	drop table #TEMP_ITEMS;
	drop table #TEMPZERO;
	drop table #TEMPSUBZERO;

END;
GO


-- Just double check these columns were added
if ( NOT EXISTS (select * from sys.columns where Name = N'Redirect' and Object_ID = Object_ID(N'SobekCM_WebContent')))
BEGIN
	ALTER TABLE [dbo].SobekCM_WebContent add Redirect nvarchar(500) null;
END;
GO


IF NOT EXISTS(select * from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = 'SobekCM_WebContent' and COLUMN_NAME = 'Locked') 
begin
	ALTER TABLE SobekCM_WebContent ADD Locked bit not null default('false');
end;
GO

-- Ensure the SobekCM_WebContent_Add stored procedure exists
IF object_id('SobekCM_WebContent_Add') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_Add as select 1;');
GO

-- Add a new web content page
ALTER PROCEDURE [dbo].[SobekCM_WebContent_Add]
	@Level1 varchar(100),
	@Level2 varchar(100),
	@Level3 varchar(100),
	@Level4 varchar(100),
	@Level5 varchar(100),
	@Level6 varchar(100),
	@Level7 varchar(100),
	@Level8 varchar(100),
	@UserName nvarchar(100),
	@Title nvarchar(255),
	@Summary nvarchar(1000),
	@Redirect nvarchar(500),
	@WebContentID int output
AS
BEGIN	
	-- Is there a match already for this?
	if ( EXISTS ( select 1 from SobekCM_WebContent 
	              where ( Level1=@Level1 )
	                and ((Level2 is null and @Level2 is null ) or ( Level2=@Level2)) 
					and ((Level3 is null and @Level3 is null ) or ( Level3=@Level3))
					and ((Level4 is null and @Level4 is null ) or ( Level4=@Level4))
					and ((Level5 is null and @Level5 is null ) or ( Level5=@Level5))
					and ((Level6 is null and @Level6 is null ) or ( Level6=@Level6))
					and ((Level7 is null and @Level7 is null ) or ( Level7=@Level7))
					and ((Level8 is null and @Level8 is null ) or ( Level8=@Level8))))
	begin
		-- Get the web content id
		set @WebContentID = (   select top 1 WebContentID 
								from SobekCM_WebContent 
								where ( Level1=@Level1 )
								  and ((Level2 is null and @Level2 is null ) or ( Level2=@Level2)) 
								  and ((Level3 is null and @Level3 is null ) or ( Level3=@Level3))
								  and ((Level4 is null and @Level4 is null ) or ( Level4=@Level4))
								  and ((Level5 is null and @Level5 is null ) or ( Level5=@Level5))
								  and ((Level6 is null and @Level6 is null ) or ( Level6=@Level6))
								  and ((Level7 is null and @Level7 is null ) or ( Level7=@Level7))
								  and ((Level8 is null and @Level8 is null ) or ( Level8=@Level8)));

		-- Ensure the title and summary are correct
		update SobekCM_WebContent set Title=@Title, Summary=@Summary, Redirect=@Redirect where WebContentID=@WebContentID;
		
		-- Was this previously deleted?
		if ( EXISTS ( select 1 from SobekCM_WebContent where Deleted='true' and WebContentID=@WebContentID ))
		begin
			-- Undelete this 
			update SobekCM_WebContent
			set Deleted='false'
			where WebContentID = @WebContentID;

			-- Mark this in the milestones then
			insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneDate, MilestoneUser )
			values ( @WebContentID, 'Restored previously deleted page', getdate(), @UserName );
		end;
	end
	else
	begin
		-- Add the new web content then
		insert into SobekCM_WebContent ( Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8, Title, Summary, Deleted, Redirect )
		values ( @Level1, @Level2, @Level3, @Level4, @Level5, @Level6, @Level7, @Level8, @Title, @Summary, 'false', @Redirect );

		-- Get the new ID for this
		set @WebContentID = SCOPE_IDENTITY();

		-- Now, add this to the milestones table
		insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneDate, MilestoneUser )
		values ( @WebContentID, 'Add new page', getdate(), @UserName );
	end;
END;
GO


-- Ensure the SobekCM_WebContent_Get_Page stored procedure exists
IF object_id('SobekCM_WebContent_Get_Page') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_Get_Page as select 1;');
GO

-- Get basic details about an existing web content page
ALTER PROCEDURE [dbo].[SobekCM_WebContent_Get_Page]
	@Level1 varchar(100),
	@Level2 varchar(100),
	@Level3 varchar(100),
	@Level4 varchar(100),
	@Level5 varchar(100),
	@Level6 varchar(100),
	@Level7 varchar(100),
	@Level8 varchar(100)
AS
BEGIN	
	-- Return the couple of requested pieces of information
	select top 1 W.WebContentID, W.Title, W.Summary, W.Deleted, M.MilestoneDate, M.MilestoneUser, W.Redirect, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Locked
	from SobekCM_WebContent W left outer join
	     SobekCM_WebContent_Milestones M on W.WebContentID=M.WebContentID
	where ( Level1=@Level1 )
	  and ((Level2 is null and @Level2 is null ) or ( Level2=@Level2)) 
	  and ((Level3 is null and @Level3 is null ) or ( Level3=@Level3))
	  and ((Level4 is null and @Level4 is null ) or ( Level4=@Level4))
	  and ((Level5 is null and @Level5 is null ) or ( Level5=@Level5))
	  and ((Level6 is null and @Level6 is null ) or ( Level6=@Level6))
	  and ((Level7 is null and @Level7 is null ) or ( Level7=@Level7))
	  and ((Level8 is null and @Level8 is null ) or ( Level8=@Level8))
	order by M.MilestoneDate DESC;
END;
GO

-- Ensure the SobekCM_WebContent_Get_Page stored procedure exists
IF object_id('SobekCM_WebContent_Get_Page_ID') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_Get_Page_ID as select 1;');
GO

-- Get basic details about an existing web content page
ALTER PROCEDURE [dbo].[SobekCM_WebContent_Get_Page_ID]
	@WebContentID int
AS
BEGIN	
	-- Return the couple of requested pieces of information
	select top 1 W.WebContentID, W.Title, W.Summary, W.Deleted, M.MilestoneDate, M.MilestoneUser, W.Redirect, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Locked
	from SobekCM_WebContent W left outer join
	     SobekCM_WebContent_Milestones M on W.WebContentID=M.WebContentID
	where W.WebContentID = @WebContentID
	order by M.MilestoneDate DESC;
END;
GO

-- Ensure the SobekCM_WebContent_All stored procedure exists
IF object_id('SobekCM_WebContent_All') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_All as select 1;');
GO

-- Return all the web content pages, regardless of whether they are redirects or an actual content page
ALTER PROCEDURE [dbo].[SobekCM_WebContent_All]
AS
BEGIN

	-- Get the pages, with the time last updated
	with webcontent_last_update as
	(
		select WebContentID, Max(WebContentMilestoneID) as MaxMilestoneID
		from SobekCM_WebContent_Milestones
		group by WebContentID
	)
	select W.WebContentID, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Title, W.Summary, W.Deleted, W.Redirect, M.MilestoneDate, M.MilestoneUser
	from SobekCM_WebContent W left outer join
		 webcontent_last_update L on L.WebContentID=W.WebContentID left outer join
	     SobekCM_WebContent_Milestones M on M.WebContentMilestoneID=L.MaxMilestoneID
	where Deleted='false'
	order by W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8;

	-- Get the distinct top level pages
	select distinct(W.Level1)
	from SobekCM_WebContent W
	where ( Deleted = 'false' )
	order by W.Level1;

	-- Get the distinct top TWO level pages
	select W.Level1, W.Level2
	from SobekCM_WebContent W
	where ( W.Level2 is not null )
	  and ( Deleted = 'false' )
	group by W.Level1, W.Level2
	order by W.Level1, W.Level2;

END;
GO

-- Ensure the SobekCM_WebContent_All_Pages stored procedure exists
IF object_id('SobekCM_WebContent_All_Pages') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_All_Pages as select 1;');
GO

-- Return all the web content pages that are not set as redirects
ALTER PROCEDURE [dbo].[SobekCM_WebContent_All_Pages]
AS
BEGIN

	-- Get the pages, with the time last updated
	with webcontent_last_update as
	(
		select WebContentID, Max(WebContentMilestoneID) as MaxMilestoneID
		from SobekCM_WebContent_Milestones
		group by WebContentID
	)
	select W.WebContentID, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Title, W.Summary, W.Deleted, W.Redirect, M.MilestoneDate, M.MilestoneUser
	from SobekCM_WebContent W left outer join
		 webcontent_last_update L on L.WebContentID=W.WebContentID left outer join
	     SobekCM_WebContent_Milestones M on M.WebContentMilestoneID=L.MaxMilestoneID
	where ( len(coalesce(W.Redirect,'')) = 0 ) and ( Deleted = 'false' )
	order by W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8;

	-- Get the distinct top level pages
	select distinct(W.Level1)
	from SobekCM_WebContent W
	where ( len(coalesce(W.Redirect,'')) = 0 ) and ( Deleted = 'false' )
	order by W.Level1;

	-- Get the distinct top TWO level pages
	select W.Level1, W.Level2
	from SobekCM_WebContent W
	where ( len(coalesce(W.Redirect,'')) = 0 )
	  and ( W.Level2 is not null )
	  and ( Deleted = 'false' )
	group by W.Level1, W.Level2
	order by W.Level1, W.Level2;

END;
GO

-- Ensure the SobekCM_WebContent_All_Redirects stored procedure exists
IF object_id('SobekCM_WebContent_All_Redirects') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_All_Redirects as select 1;');
GO

-- Return all the web content pages that are set as redirects
ALTER PROCEDURE [dbo].[SobekCM_WebContent_All_Redirects]
AS
BEGIN

	-- Get the pages, with the time last updated
	with webcontent_last_update as
	(
		select WebContentID, Max(WebContentMilestoneID) as MaxMilestoneID
		from SobekCM_WebContent_Milestones
		group by WebContentID
	)
	select W.WebContentID, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Title, W.Summary, W.Deleted, W.Redirect, M.MilestoneDate, M.MilestoneUser
	from SobekCM_WebContent W left outer join
		 webcontent_last_update L on L.WebContentID=W.WebContentID left outer join
	     SobekCM_WebContent_Milestones M on M.WebContentMilestoneID=L.MaxMilestoneID
	where ( len(coalesce(W.Redirect,'')) > 0 ) and ( Deleted = 'false' )
	order by W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8;

	-- Get the distinct top level pages
	select distinct(W.Level1)
	from SobekCM_WebContent W
	where ( len(coalesce(W.Redirect,'')) > 0 ) and ( Deleted = 'false' )
	order by W.Level1;

	-- Get the distinct top TWO level pages
	select W.Level1, W.Level2
	from SobekCM_WebContent W
	where ( len(coalesce(W.Redirect,'')) > 0 )
	  and ( W.Level2 is not null )
	  and ( Deleted = 'false' )
	group by W.Level1, W.Level2
	order by W.Level1, W.Level2;

END;
GO

-- Ensure the SobekCM_WebContent_All_Redirects stored procedure exists
IF object_id('SobekCM_WebContent_Get_Recent_Changes') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_Get_Recent_Changes as select 1;');
GO

-- Get the list of recent changes to all web content pages
ALTER PROCEDURE [dbo].[SobekCM_WebContent_Get_Recent_Changes]
AS
BEGIN

	-- Get all milestones
	select W.WebContentID, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, MilestoneDate, MilestoneUser, Milestone, W.Title
	from SobekCM_WebContent_Milestones M, SobekCM_WebContent W
	where M.WebContentID=W.WebContentID
	order by MilestoneDate DESC;

	-- Get the distinct list of users that made changes
	select MilestoneUser
	from SobekCM_WebContent_Milestones
	group by MilestoneUser
	order by MilestoneUser;

	-- Return the distinct first level
	select Level1 
	from SobekCM_WebContent_Milestones M, SobekCM_WebContent W
	where M.WebContentID=W.WebContentID
	group by Level1
	order by Level1;
	
	-- Return the distinct first TWO level					
	select Level1, Level2
	from SobekCM_WebContent_Milestones M, SobekCM_WebContent W
	where M.WebContentID=W.WebContentID
	group by Level1, Level2
	order by Level1, Level2;


END;
GO

-- Ensure the SobekCM_WebContent_All_Redirects stored procedure exists
IF object_id('SobekCM_WebContent_Edit') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_Edit as select 1;');
GO

-- Edit basic information on an existing web content page
ALTER PROCEDURE [dbo].[SobekCM_WebContent_Edit]
	@WebContentID int,
	@UserName nvarchar(100),
	@Title nvarchar(255),
	@Summary nvarchar(1000),
	@Redirect varchar(500),
	@MilestoneText varchar(max)
AS
BEGIN	
	-- Make the change
	update SobekCM_WebContent
	set Title=@Title, Summary=@Summary, Redirect=@Redirect
	where WebContentID=@WebContentID;

	-- Now, add a milestone
	if ( len(coalesce(@MilestoneText,'')) > 0 )
	begin
		insert into SobekCM_WebContent_Milestones (WebContentID, Milestone, MilestoneDate, MilestoneUser )
		values ( @WebContentID, @MilestoneText, getdate(), @UserName );
	end
	else
	begin
		insert into SobekCM_WebContent_Milestones (WebContentID, Milestone, MilestoneDate, MilestoneUser )
		values ( @WebContentID, 'Edited', getdate(), @UserName );
	end;

END;
GO

-- Ensure the SobekCM_WebContent_All_Redirects stored procedure exists
IF object_id('SobekCM_WebContent_Usage_Report') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_Usage_Report as select 1;');
GO

-- Pull the usage for all top-level web content pages between two dates
ALTER PROCEDURE [dbo].[SobekCM_WebContent_Usage_Report]
	@year1 smallint,
	@month1 smallint,
	@year2 smallint,
	@month2 smallint
AS
BEGIN	

	with stats_compiled as
	(	
		select Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8, sum(Hits) as Hits, sum(Hits_Complete) as HitsHierarchical
		from SobekCM_WebContent_Statistics
		where ((( [Month] >= @month1 ) and ( [Year] = @year1 )) or ([Year] > @year1 ))
		  and ((( [Month] <= @month2 ) and ( [Year] = @year2 )) or ([Year] < @year2 ))
		group by Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8
	)
	select coalesce(W.Level1, S.Level1) as Level1, coalesce(W.Level2, S.Level2) as Level2, coalesce(W.Level3, S.Level3) as Level3,
	       coalesce(W.Level4, S.Level4) as Level4, coalesce(W.Level5, S.Level5) as Level5, coalesce(W.Level6, S.Level6) as Level6,
		   coalesce(W.Level7, S.Level7) as Level7, coalesce(W.Level8, S.Level8) as Level8, W.Deleted, coalesce(W.Title,'(no title)') as Title, S.Hits, S.HitsHierarchical
	into #TEMP1
	from stats_compiled S left outer join
	     SobekCM_WebContent W on     ( W.Level1=S.Level1 ) 
		                         and ( coalesce(W.Level2,'')=coalesce(S.Level2,''))
								 and ( coalesce(W.Level3,'')=coalesce(S.Level3,''))
								 and ( coalesce(W.Level4,'')=coalesce(S.Level4,''))
								 and ( coalesce(W.Level5,'')=coalesce(S.Level5,''))
								 and ( coalesce(W.Level6,'')=coalesce(S.Level6,''))
								 and ( coalesce(W.Level7,'')=coalesce(S.Level7,''))
								 and ( coalesce(W.Level8,'')=coalesce(S.Level8,''))
	order by Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8;	
	
	-- Return the full stats
	select * from #TEMP1;
	
	-- Return the distinct first level
	select Level1 
	from #TEMP1
	group by Level1
	order by Level1;
	
	-- Return the distinct first TWO level					
	select Level1, Level2
	from #TEMP1
	group by Level1, Level2
	order by Level1, Level2;

END;
GO


-- Ensure the SobekCM_WebContent_Has_Usage stored procedure exists
IF object_id('SobekCM_WebContent_Has_Usage') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_Has_Usage as select 1;');
GO

-- Pull the flag indicating if this instance has any web content usage logged
ALTER PROCEDURE [dbo].SobekCM_WebContent_Has_Usage
	@value bit output
AS
BEGIN	

	if ( exists ( select 1 from SobekCM_WebContent_Statistics ))
		set @value = 'true';
	else
		set @value = 'false';
	
END;
GO

-- Ensure the SobekCM_WebContent_All_Brief stored procedure exists
IF object_id('SobekCM_WebContent_All_Brief') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_All_Brief as select 1;');
GO

-- Return a brief account of all the web content pages, regardless of whether they are redirects or an actual content page
ALTER PROCEDURE [dbo].[SobekCM_WebContent_All_Brief]
AS
BEGIN

	-- Get the complete list of all active web content pages, with segment level names, primary key, and redirect URL
	select W.WebContentID, W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8, W.Redirect
	from SobekCM_WebContent W 
	where Deleted = 'false'
	order by W.Level1, W.Level2, W.Level3, W.Level4, W.Level5, W.Level6, W.Level7, W.Level8;

END;
GO

-- Ensure the SobekCM_WebContent_Add_Milestone stored procedure exists
IF object_id('SobekCM_WebContent_Add_Milestone') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_Add_Milestone as select 1;');
GO

-- Add a new milestone to an existing web content page
ALTER PROCEDURE [dbo].[SobekCM_WebContent_Add_Milestone]
	@WebContentID int,
	@Milestone nvarchar(max),
	@MilestoneUser nvarchar(100)
AS
BEGIN

	-- Insert milestone
	insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneUser, MilestoneDate )
	values ( @WebContentID, @Milestone, @MilestoneUser, getdate());

END;
GO

-- Ensure the SobekCM_WebContent_Delete stored procedure exists
IF object_id('SobekCM_WebContent_Delete') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_Delete as select 1;');
GO

-- Delete an existing web content page (and mark in the milestones)
ALTER PROCEDURE [dbo].[SobekCM_WebContent_Delete]
	@WebContentID int,
	@Reason nvarchar(max),
	@MilestoneUser nvarchar(100)
AS
BEGIN

	-- Mark web page as deleted
	update SobekCM_WebContent
	set Deleted='true'
	where WebContentID=@WebContentID;

	-- Add a milestone for this
	if (( @Reason is not null ) and ( len(@Reason) > 0 ))
	begin
		insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneUser, MilestoneDate )
		values ( @WebContentID, 'Page Deleted - ' + @Reason, @MilestoneUser, getdate());
	end
	else
	begin
		insert into SobekCM_WebContent_Milestones ( WebContentID, Milestone, MilestoneUser, MilestoneDate )
		values ( @WebContentID, 'Page Deleted', @MilestoneUser, getdate());
	end;

END;
GO

-- Ensure the SobekCM_WebContent_Get_Milestones stored procedure exists
IF object_id('SobekCM_WebContent_Get_Milestones') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_Get_Milestones as select 1;');
GO

-- Get the milestones for a webcontent page (by ID)
ALTER PROCEDURE [dbo].[SobekCM_WebContent_Get_Milestones]
	@WebContentID int
AS
BEGIN

	-- Get all milestones
	select Milestone, MilestoneDate, MilestoneUser
	from SobekCM_WebContent_Milestones
	where WebContentID=@WebContentID
	order by MilestoneDate;

END;
GO

-- Ensure the SobekCM_WebContent_Get_Usage stored procedure exists
IF object_id('SobekCM_WebContent_Get_Usage') IS NULL EXEC ('create procedure dbo.SobekCM_WebContent_Get_Usage as select 1;');
GO

-- Get the usage stats for a webcontent page (by ID)
ALTER PROCEDURE [dbo].[SobekCM_WebContent_Get_Usage]
	@WebContentID int
AS
BEGIN

	-- Get all stats
	select [Year], [Month], Hits, Hits_Complete
	from SobekCM_WebContent_Statistics
	where WebContentID=@WebContentID
	order by [Year], [Month];

END;
GO


GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Add] TO sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Add] TO sobek_builder;

GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Add_Milestone] TO sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Add_Milestone] TO sobek_builder;

GRANT EXECUTE ON [dbo].[SobekCM_WebContent_All] TO sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_All] TO sobek_builder;

GRANT EXECUTE ON [dbo].[SobekCM_WebContent_All_Brief] to sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_All_Brief] to sobek_builder;

GRANT EXECUTE ON [dbo].[SobekCM_WebContent_All_Pages] TO sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_All_Pages] TO sobek_builder;

GRANT EXECUTE ON [dbo].[SobekCM_WebContent_All_Redirects] TO sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_All_Redirects] TO sobek_builder;

GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Delete] TO sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Delete] TO sobek_builder;

GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Edit] TO sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Edit] TO sobek_builder;

GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Get_Milestones] TO sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Get_Milestones] TO sobek_builder;

GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Get_Page] TO sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Get_Page] TO sobek_builder;

GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Get_Page_ID] TO sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Get_Page_ID] TO sobek_builder;

GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Get_Recent_Changes] TO sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Get_Recent_Changes] TO sobek_builder;

GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Get_Usage] TO sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Get_Usage] TO sobek_builder;

GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Has_Usage] to sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Has_Usage] to sobek_builder;

GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Usage_Report] to sobek_user;
GRANT EXECUTE ON [dbo].[SobekCM_WebContent_Usage_Report] to sobek_builder;
GO



-- Drop index, if it exists 
if ( EXISTS ( select 1 from sys.indexes WHERE name='IX_SobekCM_WebContent_Milestones_Date_ID' AND object_id = OBJECT_ID('SobekCM_WebContent_Milestones')))
	DROP INDEX IX_SobekCM_WebContent_Milestones_Date_ID ON [dbo].SobekCM_WebContent_Milestones
GO

alter table SobekCM_WebContent_Milestones 
alter column MilestoneDate datetime not null;
GO

/****** Object:  Index [IX_SobekCM_WebContent_Milestones_Date_ID]    Script Date: 6/4/2015 6:55:43 AM ******/
CREATE NONCLUSTERED INDEX [IX_SobekCM_WebContent_Milestones_Date_ID] ON [dbo].[SobekCM_WebContent_Milestones]
(
	[WebContentID] ASC,
	[MilestoneDate] ASC
)
INCLUDE ( 	[MilestoneUser]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO


ALTER PROCEDURE [dbo].[SobekCM_Item_Count_By_Collection_By_Date_Range]
	@date1 datetime,
	@date2 datetime
AS
BEGIN

	-- No need to perform any locks here, especially given the possible
	-- length of this search
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	SET NOCOUNT ON;
	SET ARITHABORT ON;

	-- Get the id for the ALL aggregation
	declare @all_id int;
	set @all_id = coalesce(( select AggregationID from SObekCM_Item_Aggregation where Code='all'), -1);
	
	declare @Aggregation_List TABLE
	(
	  AggregationID int,
	  Code varchar(20),
	  ChildCode varchar(20),
	  Child2Code varchar(20),
	  AllCodes varchar(20),
	  Name nvarchar(255),
	  ShortName nvarchar(100),
	  [Type] varchar(50),
	  isActive bit
	);
	
	-- Insert the list of items linked to ALL or linked to NONE (include ALL)
	insert into @Aggregation_List ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, [Type], isActive )
	select AggregationID, Code, '', '', Code, Name, ShortName, [Type], isActive
	from SobekCM_Item_Aggregation A
	where ( [Type] not like 'Institut%' )
	  and ( Deleted='false' )
	  and exists ( select * from SobekCM_Item_Aggregation_Hierarchy where ChildID=A.AggregationID and ParentID=@all_id);
	  
	-- Insert the children under those top-level collections
	insert into @Aggregation_List ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, [Type], isActive )
	select A2.AggregationID, T.Code, A2.Code, '', A2.Code, A2.Name, A2.SHortName, A2.[Type], A2.isActive
	from @Aggregation_List T, SobekCM_Item_Aggregation A2, SobekCM_Item_Aggregation_Hierarchy H
	where ( A2.[Type] not like 'Institut%' )
	  and ( T.AggregationID = H.ParentID )
	  and ( A2.AggregationID = H.ChildID )
	  and ( Deleted='false' );
	  
	-- Insert the grand-children under those child collections
	insert into @Aggregation_List ( AggregationID, Code, ChildCode, Child2Code, AllCodes, Name, ShortName, [Type], isActive )
	select A2.AggregationID, T.Code, T.ChildCode, A2.Code, A2.Code, A2.Name, A2.SHortName, A2.[Type], A2.isActive
	from @Aggregation_List T, SobekCM_Item_Aggregation A2, SobekCM_Item_Aggregation_Hierarchy H
	where ( A2.[Type] not like 'Institut%' )
	  and ( T.AggregationID = H.ParentID )
	  and ( A2.AggregationID = H.ChildID )
	  and ( Deleted='false' )
	  and ( ChildCode <> '' );
	  
	-- Get total item count
	declare @total_item_count int;
	select @total_item_count =  ( select count(*) from SobekCM_Item where Deleted = 'false' and Milestone_OnlineComplete is not null );

	-- Get total title count
	declare @total_title_count int;
    select @total_title_count = ( select count(G.GroupID)
                                  from SobekCM_Item_Group G
                                  where exists ( select ItemID
                                                 from SobekCM_Item I
                                                 where ( I.Deleted = 'false' )
                                                   and ( Milestone_OnlineComplete is not null )
                                                   and ( I.GroupID = G.GroupID )));
	-- Get total title count
	declare @total_page_count int;
	select @total_page_count =  coalesce(( select sum( [PageCount] ) from SobekCM_Item where Deleted = 'false'  and ( Milestone_OnlineComplete is not null )), 0 );

	-- Get total item count
	declare @total_item_count_date1 int;
	select @total_item_count_date1 =  ( select count(ItemID) 
										from SobekCM_Item I
										where ( I.Deleted = 'false' )
										  and ( Milestone_OnlineComplete is not null )
										  and ( Milestone_OnlineComplete <= @date1 ));

	-- Get total title count
	declare @total_title_count_date1 int;
	select @total_title_count_date1 =  ( select count(G.GroupID)
										 from SobekCM_Item_Group G
										 where exists ( select *
														from SobekCM_Item I
														where ( I.Deleted = 'false' )
														  and ( Milestone_OnlineComplete is not null )
														  and ( Milestone_OnlineComplete <= @date1 ) 
														  and ( I.GroupID = G.GroupID )));


	-- Get total title count
	declare @total_page_count_date1 int;
	select @total_page_count_date1 =  ( select sum( coalesce([PageCount],0) ) 
										from SobekCM_Item I
										where ( I.Deleted = 'false' )
										  and ( Milestone_OnlineComplete is not null )
										  and ( Milestone_OnlineComplete <= @date1 ));

	-- Return these values if this has just one date
	if ( isnull( @date2, '1/1/2000' ) = '1/1/2000' )
	begin
	
		-- Start to build the return set of values
		select code1 = Code, 
		       code2 = ChildCode,
		       code3 = Child2Code,
		       AllCodes,
		    [Name], 
		    C.isActive AS Active,
			title_count = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ),
			item_count = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ), 
			page_count = coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ), 0),
			title_count_date1 = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date1),
			item_count_date1 = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date1 ), 
			page_count_date1 = coalesce(( select sum( [PageCount] ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date1 ), 0)
		from @Aggregation_List C
		union
		select 'ZZZ','','', 'ZZZ', 'Total Count', 'false', @total_title_count, @total_item_count, @total_page_count, 
			coalesce(@total_title_count_date1,0), coalesce(@total_item_count_date1,0), coalesce(@total_page_count_date1,0)
		order by code, code2, code3;
		
	end
	else
	begin

		-- Get total item count
		declare @total_item_count_date2 int
		select @total_item_count_date2 =  ( select count(ItemID) 
											from SobekCM_Item I
											where ( I.Deleted = 'false' )
											  and ( Milestone_OnlineComplete is not null )
											  and ( Milestone_OnlineComplete <= @date2 ));

		-- Get total title count
		declare @total_title_count_date2 int
		select @total_title_count_date2 =  ( select count(G.GroupID)
											 from SobekCM_Item_Group G
											 where exists ( select *
															from SobekCM_Item I
															where ( I.Deleted = 'false' )
															  and ( Milestone_OnlineComplete is not null )
															  and ( Milestone_OnlineComplete <= @date2 ) 
															  and ( I.GroupID = G.GroupID )));


		-- Get total title count
		declare @total_page_count_date2 int
		select @total_page_count_date2 =  ( select sum( coalesce([PageCount],0) ) 
											from SobekCM_Item I
											where ( I.Deleted = 'false' )
											  and ( Milestone_OnlineComplete is not null )
											  and ( Milestone_OnlineComplete <= @date2 ));


		-- Start to build the return set of values
		select code1 = Code, 
		       code2 = ChildCode,
		       code3 = Child2Code,
		       AllCodes,
		    [Name], 
		    C.isActive AS Active,
			title_count = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ),
			item_count = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ), 
			page_count = coalesce(( select sum( PageCount ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID ), 0),
			title_count_date1 = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date1),
			item_count_date1 = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date1 ), 
			page_count_date1 = coalesce(( select sum( [PageCount] ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date1 ), 0),
			title_count_date2 = ( select count(distinct(GroupID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date2),
			item_count_date2 = ( select count(distinct(ItemID)) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date2 ), 
			page_count_date2 = coalesce(( select sum( [PageCount] ) from Statistics_Item_Aggregation_Link_View T where T.AggregationID = C.AggregationID and Milestone_OnlineComplete is not null and Milestone_OnlineComplete <= @date2 ), 0)
		from @Aggregation_List C
		union
		select 'ZZZ','','','ZZZ', 'Total Count', 'false', @total_title_count, @total_item_count, @total_page_count, 
				coalesce(@total_title_count_date1,0), coalesce(@total_item_count_date1,0), coalesce(@total_page_count_date1,0),
				coalesce(@total_title_count_date2,0), coalesce(@total_item_count_date2,0), coalesce(@total_page_count_date2,0)
		order by code, code2, code3;
	end;
END;
GO

/****** Object:  StoredProcedure [dbo].[SobekCM_Delete_Item]    Script Date: 12/20/2013 05:43:36 ******/
-- Deletes an item, and deletes the group if there are no additional items attached
ALTER PROCEDURE [dbo].[SobekCM_Delete_Item] 
@bibid varchar(10),
@vid varchar(5),
@as_admin bit,
@delete_message varchar(1000)
AS
begin transaction
	-- Perform transactionally in case there is a problem deleting some of the rows
	-- so the entire delete is rolled back

   declare @itemid int;
   set @itemid = 0;

    -- first to get the itemid of the specified bibid and vid
   select @itemid = isnull(I.itemid, 0)
   from SobekCM_Item I, SobekCM_Item_Group G
   where (G.bibid = @bibid) 
       and (I.vid = @vid)
       and ( I.GroupID = G.GroupID );

   -- if there is such an itemid in the UFDC database, then delete this item and its related information
  if ( isnull(@itemid, 0 ) > 0)
  begin

	-- Delete all references to this item 
	delete from SobekCM_Metadata_Unique_Link where ItemID=@itemid;
	delete from SobekCM_Metadata_Basic_Search_Table where ItemID=@itemid;
	delete from SobekCM_Item_Footprint where ItemID=@itemid;
	delete from SobekCM_Item_Icons where ItemID=@itemid;
	delete from SobekCM_Item_Statistics where ItemID=@itemid;
	delete from SobekCM_Item_GeoRegion_Link where ItemID=@itemid;
	delete from SobekCM_Item_Aggregation_Item_Link where ItemID=@itemid;
	delete from mySobek_User_Item where ItemID=@itemid;
	delete from mySobek_User_Item_Link where ItemID=@itemid;
	delete from mySobek_User_Description_Tags where ItemID=@itemid;
	delete from SobekCM_Item_Viewers where ItemID=@itemid;
	delete from Tracking_Item where ItemID=@itemid;
	delete from Tracking_Progress where ItemID=@itemid;
	delete from SobekCM_Item_OAI where ItemID=@itemid;
	
	if ( @as_admin = 'true' )
	begin
		delete from Tracking_Archive_Item_Link where ItemID=@itemid;
		update Tivoli_File_Log set DeleteMsg=@delete_message, ItemID = -1 where ItemID=@itemid;
	end;
	
	-- Finally, delete the item 
	delete from SobekCM_Item where ItemID=@itemid;
	
	-- Delete the item group if it is the last one existing
	if (( select count(I.ItemID) from SobekCM_Item_Group G, SobekCM_Item I where ( G.BibID = @bibid ) and ( G.GroupID = I.GroupID ) and ( I.Deleted = 0 )) < 1 )
	begin
		
		declare @groupid int;
		set @groupid = 0;	
		
		-- first to get the itemid of the specified bibid and vid
		select @groupid = isnull(G.GroupID, 0)
		from SobekCM_Item_Group G
		where (G.bibid = @bibid);
		
		-- Delete if this selected something
		if ( ISNULL(@groupid, 0 ) > 0 )
		begin		
			-- delete from the item group table	and all references
			delete from SobekCM_Item_Group_External_Record where GroupID=@groupid;
			delete from SobekCM_Item_Group_Web_Skin_Link where GroupID=@groupid;
			delete from SobekCM_Item_Group_Statistics where GroupID=@groupid;
			delete from mySobek_User_Bib_Link where GroupID=@groupid;
			delete from SobekCM_Item_Group_OAI where GroupID=@groupid;
			delete from SobekCM_Item_Group where GroupID=@groupid;
		end;
	end
	else
	begin
		-- Finally set the volume count for this group correctly
		update SobekCM_Item_Group
		set ItemCount = ( select count(*) from SobekCM_Item I where ( I.GroupID = SobekCM_Item_Group.GroupID ))	
		where ( SobekCM_Item_Group.BibID = @bibid );
	end;
  end;
   
commit transaction;
GO


/****** Object:  StoredProcedure [dbo].[mySobek_Update_User]    Script Date: 12/20/2013 05:43:36 ******/
-- Procedure allows an admin to edit permissions flags for this user
ALTER PROCEDURE [dbo].[mySobek_Update_User]
      @userid int,
      @can_submit bit,
      @is_internal bit,
      @can_edit_all bit,
      @can_delete_all bit,
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
            EditTemplateMarc=@edit_template_marc, IsHostAdmin=@is_host_admin
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


-- Gets the list of all point coordinates for a single aggregation
ALTER PROCEDURE [dbo].[SobekCM_Coordinate_Points_By_Aggregation]
	@aggregation_code varchar(20)
AS
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

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
	  and ( F.Point_Latitude is not null )
	  and ( F.Point_Longitude is not null )
	  and ( T.GroupID = G.GroupID )
	  and ( T.Point_Latitude = F.Point_Latitude )
	  and ( T.Point_Longitude = F.Point_Longitude )
	group by I.Spatial_KML, F.Point_Latitude, F.Point_Longitude, G.BibID, G.GroupTitle, coalesce(NULLIF(G.GroupThumbnail,''), T.MinThumbnail), G.ItemCount, G.[Type]
	order by I.Spatial_KML;
end;
GO

-- Gets all of the information about a single item aggregation
ALTER PROCEDURE [dbo].[SobekCM_Get_Item_Aggregation]
	@code varchar(20)
AS
begin

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Create the temporary table
	create table #TEMP_CHILDREN_BUILDER (AggregationID int, Code varchar(20), ParentCode varchar(20), Name varchar(255), [Type] varchar(50), ShortName varchar(100), isActive bit, Hidden bit, HierarchyLevel int );
	
	-- Get the aggregation id
	declare @aggregationid int
	set @aggregationid = coalesce((select AggregationID from SobekCM_Item_Aggregation AS C where C.Code = @code and Deleted=0), -1 );
	
	-- Return information about this aggregation
	select AggregationID, Code, [Name], coalesce(ShortName,[Name]) AS ShortName, [Type], isActive, Hidden, HasNewItems,
	   ContactEmail, DefaultInterface, [Description], Map_Display, Map_Search, OAI_Flag, OAI_Metadata, DisplayOptions, LastItemAdded, 
	   Can_Browse_Items, Items_Can_Be_Described, External_Link, T.ThematicHeadingID, LanguageVariants, ThemeName
	from SobekCM_Item_Aggregation AS C left outer join
	     SobekCM_Thematic_Heading as T on C.ThematicHeadingID=T.ThematicHeadingID 
	where C.AggregationID = @aggregationid;

	-- Drive down through the children in the item aggregation hierarchy (first level below)
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, HierarchyLevel )
	select C.AggregationID, C.Code, ParentCode=@code, C.[Name], C.[Type], coalesce(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, -1
	from SobekCM_Item_Aggregation AS P INNER JOIN
		 SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
		 SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( P.AggregationID = @aggregationid )
	  and ( C.Deleted = 'false' );
	
	-- Now, try to find any children to this ( second level below )
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], coalesce(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, -2
	from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
			SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( HierarchyLevel = -1 )
	  and ( C.Deleted = 'false' );

	-- Now, try to find any children to this ( third level below )
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], coalesce(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, -3
	from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
			SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( HierarchyLevel = -2 )
	  and ( C.Deleted = 'false' );

	-- Now, try to find any children to this ( fourth level below )
	insert into #TEMP_CHILDREN_BUILDER ( AggregationID, Code, ParentCode, Name, [Type], ShortName, isActive, Hidden, HierarchyLevel )
	select C.AggregationID, C.Code, P.Code, C.[Name], C.[Type], coalesce(C.ShortName,C.[Name]) AS ShortName, C.isActive, C.Hidden, -4
	from #TEMP_CHILDREN_BUILDER AS P INNER JOIN
			SobekCM_Item_Aggregation_Hierarchy AS H ON H.ParentID = P.AggregationID INNER JOIN
			SobekCM_Item_Aggregation AS C ON H.ChildID = C.AggregationID 
	where ( HierarchyLevel = -3 )
	  and ( C.Deleted = 'false' );

	-- Return all the children
	select Code, ParentCode, [Name], [ShortName], [Type], HierarchyLevel, isActive, Hidden
	from #TEMP_CHILDREN_BUILDER
	order by HierarchyLevel, Code ASC;
	
	-- drop the temporary tables
	drop table #TEMP_CHILDREN_BUILDER;

	-- Return all the metadata ids for metadata types which have values 
	select T.MetadataTypeID, T.canFacetBrowse, T.DisplayTerm, T.SobekCode, T.SolrCode
	into #TEMP_METADATA
	from SobekCM_Metadata_Types T
	where ( LEN(T.SobekCode) > 0 )
	  and exists ( select * from SobekCM_Item_Aggregation_Metadata_Link L where L.AggregationID=@aggregationid and L.MetadataTypeID=T.MetadataTypeID and L.Metadata_Count > 0 );

	if (( select count(*) from #TEMP_METADATA ) > 0 )
	begin
		select * from #TEMP_METADATA order by DisplayTerm ASC;
	end
	else
	begin
		select MetadataTypeID, canFacetBrowse, DisplayTerm, SobekCode, SolrCode
		from SobekCM_Metadata_Types 
		where DefaultAdvancedSearch = 'true'
		order by DisplayTerm ASC;
	end;
			
	-- Return all the parents 
	select Code, [Name], [ShortName], [Type], isActive
	from SobekCM_Item_Aggregation A, SobekCM_Item_Aggregation_Hierarchy H
	where A.AggregationID = H.ParentID 
	  and H.ChildID = @aggregationid
	  and A.Deleted = 'false';

	-- Return all matchint group/coordinate point, with the group thumbnail, or item thumbnail from above WITH statements
	select Min(F.Point_Latitude) as Min_Latitude, Max(F.Point_Latitude) as Max_Latitude, Min(F.Point_Longitude) as Min_Longitude, Max(F.Point_Longitude) as Max_Longitude
	from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Footprint F
	where ( I.ItemID = L.ItemID  )
	  and ( L.AggregationID = @aggregationid )
	  and ( F.ItemID = I.ItemID )
	  and ( F.Point_Latitude is not null )
	  and ( F.Point_Longitude is not null );

end;
GO


-- Update the version number
if (( select count(*) from SobekCM_Database_Version ) = 0 )
begin
	insert into SobekCM_Database_Version ( Major_Version, Minor_Version, Release_Phase )
	values ( 4, 9, '0' );
end
else
begin
	update SobekCM_Database_Version
	set Major_Version=4, Minor_Version=9, Release_Phase='0';
end;
GO
