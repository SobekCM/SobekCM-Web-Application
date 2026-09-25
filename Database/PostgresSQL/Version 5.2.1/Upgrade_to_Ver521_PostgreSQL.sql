-- Upgrade_to_Ver521_PostgreSQL.sql - OpenSobek
--
-- Takes an existing 5.2.0 PostgreSQL database and brings it up to Version 5.2.1.


-- The map browse for the ALL aggregation only showed items with an explicit or implied link to ALL, which is
-- only added when a collection rolls up to ALL through SobekCM_Item_Aggregation_Hierarchy.  Automatically
-- created institutions, and any collection that is not a child of ALL, never add that link, so their
-- items' points were missing from the ALL map.  ALL now returns every item with a point,
-- without going through SobekCM_Item_Aggregation_Item_Link.  Deleted and dark items are now left out of the
-- map browse for every aggregation, ALL included, since neither should be shown publicly.


-- Gets the list of all point coordinates for a single aggregation
CREATE OR REPLACE FUNCTION SobekCM_Coordinate_Points_By_Aggregation(
	p_aggregation_code varchar(20)
)
RETURNS TABLE (
	Point_Latitude double precision,
	Point_Longitude double precision,
	BibID varchar(10),
	GroupTitle varchar(500),
	Thumbnail text,
	ItemCount integer,
	"Type" varchar(50)
)
LANGUAGE plpgsql
AS $$
BEGIN
	IF ( p_aggregation_code = 'ALL' ) THEN
		RETURN QUERY
		with min_itemid_per_groupid as
		(
			-- Get the mininmum ItemID per group per coordinate point
			select I.GroupID, F.Point_Latitude, F.Point_Longitude, Min(I.ItemID) as MinItemID
			from SobekCM_Item I, SobekCM_Item_Footprint F
			where ( F.ItemID = I.ItemID )
			  and ( I.Deleted = false )
			  and ( I.Dark = false )
			  and ( F.Point_Latitude is not null )
			  and ( F.Point_Longitude is not null )
			group by I.GroupID, F.Point_Latitude, F.Point_Longitude
		), min_item_thumbnail_per_group as
		(
		    -- Get the matching item thumbnail for the item per group per coordiante point
			select G.GroupID, G.Point_Latitude, G.Point_Longitude, I.VID || '/' || I.MainThumbnail as MinThumbnail
			from SobekCM_Item I, min_itemid_per_groupid G
			where G.MinItemID = I.ItemID
		)
		-- Return all matching group/coordinate point, with the group thumbnail, or item thumbnail from above WITH statements
		select F.Point_Latitude, F.Point_Longitude, G.BibID, G.GroupTitle, coalesce(NULLIF(G.GroupThumbnail,''), T.MinThumbnail) as Thumbnail, G.ItemCount, G.Type
		from SobekCM_Item_Group G, SobekCM_Item I, SobekCM_Item_Footprint F, min_item_thumbnail_per_group T
		where ( G.GroupID = I.GroupID )
		  and ( F.ItemID = I.ItemID )
		  and ( I.Deleted = false )
		  and ( I.Dark = false )
		  and ( F.Point_Latitude is not null )
		  and ( F.Point_Longitude is not null )
		  and ( T.GroupID = G.GroupID )
		  and ( T.Point_Latitude = F.Point_Latitude )
		  and ( T.Point_Longitude = F.Point_Longitude )
		group by I.Spatial_KML, F.Point_Latitude, F.Point_Longitude, G.BibID, G.GroupTitle, coalesce(NULLIF(G.GroupThumbnail,''), T.MinThumbnail), G.ItemCount, G.Type
		order by I.Spatial_KML;
	ELSE
		RETURN QUERY
		with min_itemid_per_groupid as
		(
			-- Get the mininmum ItemID per group per coordinate point
			select GroupID, F.Point_Latitude, F.Point_Longitude, Min(I.ItemID) as MinItemID
			from SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Aggregation A, SobekCM_Item_Footprint F
			where ( I.ItemID = L.ItemID  )
			  and ( L.AggregationID = A.AggregationID )
			  and ( A.Code = p_aggregation_code )
			  and ( F.ItemID = I.ItemID )
			  and ( I.Deleted = false )
			  and ( I.Dark = false )
			  and ( F.Point_Latitude is not null )
			  and ( F.Point_Longitude is not null )
			group by GroupID, F.Point_Latitude, F.Point_Longitude
		), min_item_thumbnail_per_group as
		(
		    -- Get the matching item thumbnail for the item per group per coordiante point
			select G.GroupID, G.Point_Latitude, G.Point_Longitude, I.VID || '/' || I.MainThumbnail as MinThumbnail
			from SobekCM_Item I, min_itemid_per_groupid G
			where G.MinItemID = I.ItemID
		)
		-- Return all matching group/coordinate point, with the group thumbnail, or item thumbnail from above WITH statements
		select F.Point_Latitude, F.Point_Longitude, G.BibID, G.GroupTitle, coalesce(NULLIF(G.GroupThumbnail,''), T.MinThumbnail) as Thumbnail, G.ItemCount, G.Type
		from SobekCM_Item_Group G, SobekCM_Item I, SobekCM_Item_Aggregation_Item_Link L, SobekCM_Item_Footprint F, SobekCM_Item_Aggregation A, min_item_thumbnail_per_group T
		where ( G.GroupID = I.GroupID )
		  and ( I.ItemID = L.ItemID  )
		  and ( L.AggregationID = A.AggregationID )
		  and ( A.Code = p_aggregation_code )
		  and ( F.ItemID = I.ItemID )
		  and ( I.Deleted = false )
		  and ( I.Dark = false )
		  and ( F.Point_Latitude is not null )
		  and ( F.Point_Longitude is not null )
		  and ( T.GroupID = G.GroupID )
		  and ( T.Point_Latitude = F.Point_Latitude )
		  and ( T.Point_Longitude = F.Point_Longitude )
		group by I.Spatial_KML, F.Point_Latitude, F.Point_Longitude, G.BibID, G.GroupTitle, coalesce(NULLIF(G.GroupThumbnail,''), T.MinThumbnail), G.ItemCount, G.Type
		order by I.Spatial_KML;
	END IF;
END;
$$;


/**************************************************************************/
/**                                                                      **/
/**   Update Database Version                                            **/
/**                                                                      **/
/**************************************************************************/

-- Update the version number
DO $$
BEGIN
	IF (select count(*) from SobekCM_Database_Version) = 0 THEN
		insert into SobekCM_Database_Version (Major_Version, Minor_Version, Release_Phase)
		values (5, 2, '1');
	ELSE
		update SobekCM_Database_Version
		set Major_Version = 5, Minor_Version = 2, Release_Phase = '1';
	END IF;
END;
$$;
