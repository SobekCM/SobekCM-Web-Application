

IF ( NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SobekCM_Item_Aggregation_Default_Result_Fields'))
begin
	CREATE TABLE [dbo].[SobekCM_Item_Aggregation_Default_Result_Fields](
		[ItemAggregationResultTypeID] [int] NOT NULL,
		[MetadataTypeID] [smallint] NOT NULL,
		[OverrideDisplayTerm] [nvarchar](255) NULL,
		[DisplayOrder] [int] NOT NULL,
		[DisplayOptions] [nvarchar](255) NULL,
	 CONSTRAINT [PK_SobekCM_Item_Aggregation_Default_Result_Fields] PRIMARY KEY CLUSTERED 
	(
		[ItemAggregationResultTypeID] ASC,
		[MetadataTypeID] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]
end;
GO

IF ( NOT EXISTS ( SELECT *  FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS  WHERE CONSTRAINT_NAME ='FK_SobekCM_Item_Aggregation_Default_Result_Fields_SobekCM_Item_Aggregation_Result_Types' ))
begin
	ALTER TABLE [dbo].[SobekCM_Item_Aggregation_Default_Result_Fields]  WITH CHECK ADD  CONSTRAINT [FK_SobekCM_Item_Aggregation_Default_Result_Fields_SobekCM_Item_Aggregation_Result_Types] FOREIGN KEY([ItemAggregationResultTypeID])
	REFERENCES [dbo].[SobekCM_Item_Aggregation_Result_Types] ([ItemAggregationResultTypeID])
end;
GO

-- Add the default result fields
if (( select count(*) from SobekCM_Item_Aggregation_Default_Result_Fields ) = 0 )
begin
	insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder ) select ItemAggregationResultTypeID, 4, 1 from SobekCM_Item_Aggregation_Result_Types where ResultType != 'THUMBNAIL';
	insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder ) select ItemAggregationResultTypeID, 5, 2 from SobekCM_Item_Aggregation_Result_Types where ResultType != 'THUMBNAIL';
	insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder ) select ItemAggregationResultTypeID, 24, 1 from SobekCM_Item_Aggregation_Result_Types where ResultType != 'THUMBNAIL';
	insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder ) select ItemAggregationResultTypeID, 2, 4 from SobekCM_Item_Aggregation_Result_Types where ResultType != 'THUMBNAIL';
	insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder ) select ItemAggregationResultTypeID, 22, 5 from SobekCM_Item_Aggregation_Result_Types where ResultType != 'THUMBNAIL';
	insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder ) select ItemAggregationResultTypeID, 38, 6 from SobekCM_Item_Aggregation_Result_Types where ResultType != 'THUMBNAIL';
	insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder ) select ItemAggregationResultTypeID, 15, 7 from SobekCM_Item_Aggregation_Result_Types where ResultType != 'THUMBNAIL';
	insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder ) select ItemAggregationResultTypeID, 16, 8 from SobekCM_Item_Aggregation_Result_Types where ResultType != 'THUMBNAIL';
	insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder ) select ItemAggregationResultTypeID, 21, 9 from SobekCM_Item_Aggregation_Result_Types where ResultType != 'THUMBNAIL';
	insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder ) select ItemAggregationResultTypeID, 7, 10 from SobekCM_Item_Aggregation_Result_Types where ResultType != 'THUMBNAIL';
	insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder ) select ItemAggregationResultTypeID, 10, 11 from SobekCM_Item_Aggregation_Result_Types where ResultType != 'THUMBNAIL';
	insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder ) select ItemAggregationResultTypeID, 8, 12 from SobekCM_Item_Aggregation_Result_Types where ResultType != 'THUMBNAIL';
	insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder ) select ItemAggregationResultTypeID, 3, 13 from SobekCM_Item_Aggregation_Result_Types where ResultType != 'THUMBNAIL';
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

	-- Determine when the last item was made available and if the new browse should display
	declare @last_added_date datetime;
	set @last_added_date = ( select MAX(MadePublicDate) from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L where I.ItemID=L.ItemID and I.Dark='false' and I.IP_Restriction_Mask >= 0 and L.AggregationID=@aggregationid);

	declare @has_new_items bit;
	set @has_new_items = 'false';
	if ( coalesce(@last_added_date, '1/1/1900' ) > DATEADD(day, -14, getdate()))
	begin
		set @has_new_items='true';
	end;
	
	-- Return information about this aggregation
	select AggregationID, Code, [Name], coalesce(ShortName,[Name]) AS ShortName, [Type], isActive, Hidden, @has_new_items as HasNewItems,
	   ContactEmail, DefaultInterface, [Description], Map_Display, Map_Search, OAI_Flag, OAI_Metadata, DisplayOptions, coalesce(@last_added_date, '1/1/1900' ) as LastItemAdded, 
	   Can_Browse_Items, Items_Can_Be_Described, External_Link, T.ThematicHeadingID, LanguageVariants, ThemeName, GroupResults
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

	-- Return the max/min of latitude and longitude - spatial footprint to cover all items with coordinate info
	select Min(F.Point_Latitude) as Min_Latitude, Max(F.Point_Latitude) as Max_Latitude, Min(F.Point_Longitude) as Min_Longitude, Max(F.Point_Longitude) as Max_Longitude
	from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Footprint F
	where ( I.ItemID = L.ItemID  )
	  and ( L.AggregationID = @aggregationid )
	  and ( F.ItemID = I.ItemID )
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
GO



-- Stored procedure to save the basic item aggregation information
ALTER PROCEDURE [dbo].[SobekCM_Save_Item_Aggregation_ResultViews]
	@code varchar(20),
	@results1 varchar(50),
	@results2 varchar(50),
	@results3 varchar(50),
	@results4 varchar(50),
	@results5 varchar(50),
	@results6 varchar(50),
	@results7 varchar(50),
	@results8 varchar(50),
	@results9 varchar(50),
	@results10 varchar(50),
	@default varchar(50)
AS
begin transaction

	-- Only continue if there is a match on the aggregation code
	if ( exists ( select 1 from SobekCM_Item_Aggregation where Code = @code ))
	begin
		declare @id int;
		set @id = ( select AggregationID from SobekCM_Item_Aggregation where Code = @code );

		-- Keep list of any existing view
		declare @existing_views table(ResultTypeId int primary key, AggrSpecificId int, StillExisting bit );
		insert into @existing_views 
		select ItemAggregationResultTypeID, ItemAggregationResultID, 'false'
		from SobekCM_Item_Aggregation_Result_Views V
		where ( V.AggregationID=@id );

		-- Add the FIRST results view
		if (( len(@results1) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results1)))
		begin
			declare @results1_id int;
			set @results1_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results1 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results1_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results1_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results1_id;
			end;
		end;

		-- Add the SECOND results view
		if (( len(@results2) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results2)))
		begin
			declare @results2_id int;
			set @results2_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results2 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results2_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results2_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results2_id;
			end;
		end;

		-- Add the THIRD results view
		if (( len(@results3) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results3)))
		begin
			declare @results3_id int;
			set @results3_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results3 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results3_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results3_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results3_id;
			end;
		end;

		-- Add the FOURTH results view
		if (( len(@results4) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results4)))
		begin
			declare @results4_id int;
			set @results4_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results4 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results4_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results4_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results4_id;
			end;
		end;

		-- Add the FIFTH results view
		if (( len(@results5) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results5)))
		begin
			declare @results5_id int;
			set @results5_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results5 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results5_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results5_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results5_id;
			end;
		end;

		-- Add the SIXTH results view
		if (( len(@results6) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results6)))
		begin
			declare @results6_id int;
			set @results6_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results6 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results6_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results6_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results6_id;
			end;
		end;

		-- Add the SEVENTH results view
		if (( len(@results7) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results7)))
		begin
			declare @results7_id int;
			set @results7_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results7 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results7_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results7_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results7_id;
			end;
		end;

		-- Add the EIGHTH results view
		if (( len(@results8) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results8)))
		begin
			declare @results8_id int;
			set @results8_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results8 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results8_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results8_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results8_id;
			end;
		end;

		-- Add the NINTH results view
		if (( len(@results9) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results9)))
		begin
			declare @results9_id int;
			set @results9_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results9 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results9_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results9_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results9_id;
			end;
		end;

		-- Add the TENTH results view
		if (( len(@results10) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@results10)))
		begin
			declare @results10_id int;
			set @results10_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@results10 );

			-- Does this result view already exist?
			if ( not exists ( select 1 from SobekCM_Item_Aggregation_Result_Views where AggregationID=@id and ItemAggregationResultTypeID=@results10_id ))
			begin
				-- Doesn't exist, so add it
				insert into SobekCM_Item_Aggregation_Result_Views ( AggregationID, ItemAggregationResultTypeID, DefaultView )
				values ( @id, @results10_id, 'false' );
			end
			else
			begin
				-- It did exist, so mark it in the temp table
				update @existing_views
				set StillExisting='true'
				where ResultTypeId=@results10_id;
			end;
		end;

		-- Now, remove any 
		if (( select count(*) from @existing_views ) > 0 )
		begin
			-- First delete the fields
			delete from SobekCM_Item_Aggregation_Result_Fields
			where exists ( select 1 from @existing_views V where V.StillExisting='false' and V.AggrSpecificId=ItemAggregationResultID);

			-- Now, delete this results view
			delete from SobekCM_Item_Aggregation_Result_Views 
			where exists ( select 1 from @existing_views V where V.StillExisting='false' and V.AggrSpecificId=ItemAggregationResultID);

		end;

		-- Set the DEFAULT view
		if (( len(@default) > 0 ) and ( exists ( select 1 from SobekCM_Item_Aggregation_Result_Types where ResultType=@default )))
		begin
			-- Get the ID for the default
			declare @default_id int;
			set @default_id = ( select ItemAggregationResultTypeID from SobekCM_Item_Aggregation_Result_Types where ResultType=@default );

			-- Update, if it exists
			update SobekCM_Item_Aggregation_Result_Views
			set DefaultView = 'false'
			where AggregationID = @id and ItemAggregationResultTypeID != @default_id;

			-- Update, if it exists
			update SobekCM_Item_Aggregation_Result_Views
			set DefaultView = 'true'
			where AggregationID = @id and ItemAggregationResultTypeID = @default_id;
		end;

	end;

commit transaction;
GO
