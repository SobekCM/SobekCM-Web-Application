-- Get the information about the ALL aggregation - standard fron home page collection
-- Written by Mark Sullivan (September 2005), Updated ( January 2010 )
ALTER PROCEDURE [dbo].[SobekCM_Get_All_Groups]
	@metadata_count_to_use_cache int
AS
begin 

	-- No need to perform any locks here
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	-- Create the temporary table variable
	declare @TEMP_CHILDREN_BUILDER table ( AggregationID int primary key, Code varchar(20), ParentCode varchar(20), Name nvarchar(255), ShortName nvarchar(100), [Type] nvarchar(50), HierarchyLevel int, isActive bit, Hidden bit );

	-- Get the aggregation id for 'all'
	declare @aggregationid int;
	
	-- Get the aggregation id
	select @aggregationid = AggregationID
	from SobekCM_Item_Aggregation AS C 
	where ( C.Code = 'all' );

	-- Determine when the last item was made available and if the new browse should display
	declare @last_added_date datetime;
	set @last_added_date = ( select MAX(MadePublicDate) from SobekCM_Item I where I.Dark='false' and I.IP_Restriction_Mask >= 0 and I.IncludeInAll='true');

	declare @has_new_items bit;
	set @has_new_items = 'false';
	if ( coalesce(@last_added_date, '1/1/1900' ) > DATEADD(day, -14, getdate()))
	begin
		set @has_new_items='true';
	end;
	
	-- Return information about this aggregation
	select AggregationID, Code, [Name], isnull(ShortName,[Name]) AS ShortName, [Type], isActive, Hidden, @has_new_items as HasNewItems,
	   ContactEmail, DefaultInterface, [Description], Map_Display, Map_Search, OAI_Flag, OAI_Metadata, DisplayOptions, 
	  coalesce(@last_added_date, '1/1/1900' ) as LastItemAdded, Can_Browse_Items, Items_Can_Be_Described, External_Link, GroupResults
	from SobekCM_Item_Aggregation AS C 
	where ( C.AggregationID=@aggregationid );
	
	-- Return every metadata term for which any data is present
	if ( ( select COUNT(*) from SobekCM_Item_Aggregation_Metadata_Link where AggregationID=@aggregationid ) > @metadata_count_to_use_cache )
	begin
		-- Just pull the cached links here
		select distinct(S.MetadataTypeID), T.canFacetBrowse, DisplayTerm, T.SobekCode, T.SolrCode
		from SobekCM_Item_Aggregation_Metadata_Link S, 
			SobekCM_Metadata_Types T
		where ( S.MetadataTypeID = T.MetadataTypeID )
		  and ( S.AggregationID = @aggregationid )
		group by S.MetadataTypeID, DisplayTerm, T.canFacetBrowse, T.SobekCode, T.SolrCode
		order by DisplayTerm ASC;		
		
	end
	else
	begin
		-- Just pull this from the actual metadata links then
		select distinct(S.MetadataTypeID), T.canFacetBrowse, DisplayTerm, T.SobekCode, T.SolrCode
		from SobekCM_Metadata_Unique_Search_Table S, 
			SobekCM_Metadata_Types T
		where ( S.MetadataTypeID = T.MetadataTypeID )
		group by S.MetadataTypeID, DisplayTerm, T.canFacetBrowse, T.SobekCode, T.SolrCode
		order by DisplayTerm ASC;		
	end;

	-- Return the max/min of latitude and longitude - spatial footprint to cover all items with coordinate info
	select Min(F.Point_Latitude) as Min_Latitude, Max(F.Point_Latitude) as Max_Latitude, Min(F.Point_Longitude) as Min_Longitude, Max(F.Point_Longitude) as Max_Longitude
	from SobekCM_Item I, SobekCM_Item_Footprint F
	where ( F.ItemID = I.ItemID )
	  and ( F.Point_Latitude is not null )
	  and ( F.Point_Longitude is not null )
	  and ( I.Dark = 'false' );

	-- Return all of the key/value pairs of settings
	select Setting_Key, Setting_Value
	from SobekCM_Item_Aggregation_Settings 
	where AggregationID=@aggregationid;
	
	
	-- Get the result views linked to this aggrgeation and save in a temp table
	select T.ResultType, A.DefaultView, A.ItemAggregationResultTypeID, ItemAggregationResultID, T.DefaultOrder
	into #ResultViews
	from SobekCM_Item_Aggregation_Result_Views A, SobekCM_Item_Aggregation_Result_Types T
	where A.AggregationID=@aggregationid
	  and A.ItemAggregationResultTypeID=T.ItemAggregationResultTypeID;

	-- return just the data needed
	select ResultType, DefaultView
	from #ResultViews	
	order by DefaultOrder ASC;
	
	-- Get the fields for the facets
	select F.MetadataTypeID, coalesce(F.OverrideFacetTerm, T.FacetTerm) as FacetTerm, T.SobekCode, T.SolrCode_Facets
	from SobekCM_Item_Aggregation_Facets F, SobekCM_Metadata_Types T
	where ( F.AggregationID = @aggregationid ) 
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	order by FacetOrder;

	-- Get the fields for the result fields (some may be customized at the aggregation level)
	select A.ResultType, F.MetadataTypeID, coalesce(F.OverrideDisplayTerm, T.DisplayTerm) as DisplayTerm, T.SobekCode, T.SolrCode_Display, F.DisplayOrder, 'Custom' as [Source]
	from SobekCM_Item_Aggregation_Result_Fields F, SobekCM_Metadata_Types T, #ResultViews A
	where ( A.ItemAggregationResultID = F.ItemAggregationResultID )
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	union
	select A.ResultType, F.MetadataTypeID, coalesce(F.OverrideDisplayTerm, T.DisplayTerm) as DisplayTerm, T.SobekCode, T.SolrCode_Display, F.DisplayOrder, 'Default' as [Source]
	from SobekCM_Item_Aggregation_Default_Result_Fields F, SobekCM_Metadata_Types T, #ResultViews A
	where ( A.ItemAggregationResultTypeID = F.ItemAggregationResultTypeID )
	  and ( F.MetadataTypeID = T.MetadataTypeID )
	  and ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Fields X where A.ItemAggregationResultID = X.ItemAggregationResultID ))
	order by A.ResultType, DisplayOrder

	-- Drop the temp table
	drop table #ResultViews;


end;
