-- Seeds the install-wide default result fields, which collections use for any result view without
-- fields of their own.  These were seeded by Upgrade_to_Ver500.sql (for 4.x databases), but never
-- by the 5.x complete scripts, so every database built from scratch since 5.0 had an empty table and
-- fell back to a list hardcoded in the engine.  Same list as the 5.0 upgrade (with Publication Date
-- moved to its intended display order of 3), plus the VRA Core fields.  Added for every result type,
-- including THUMBNAIL, whose hover tooltip shows these fields.  Only runs if the table is empty, so
-- any customized defaults are left alone.
if ( not exists ( select 1 from SobekCM_Item_Aggregation_Default_Result_Fields ))
begin
	insert into SobekCM_Item_Aggregation_Default_Result_Fields ( ItemAggregationResultTypeID, MetadataTypeID, DisplayOrder )
	select R.ItemAggregationResultTypeID, D.MetadataTypeID, D.DisplayOrder
	from SobekCM_Item_Aggregation_Result_Types R
	cross join ( values
		(   4,  1 ),   -- Creator
		(   5,  2 ),   -- Publisher
		(  24,  3 ),   -- Publication Date
		(   2,  4 ),   -- Resource Type
		(  22,  5 ),   -- Format (displays as Description)
		(  38,  6 ),   -- Edition
		(  15,  7 ),   -- Source Institution
		(  16,  8 ),   -- Holding Location
		(  21,  9 ),   -- Donor
		(   7, 10 ),   -- Subject Keyword
		(  10, 11 ),   -- Spatial Coverage
		(   8, 12 ),   -- Genre (displays as Material Type)
		(   3, 13 ),   -- Language
		(  52, 14 ),   -- VRA Core: Material
		( 118, 15 ),   -- VRA Core: Measurements
		(  54, 16 ),   -- VRA Core: Technique
		(  53, 17 ),   -- VRA Core: Style Period
		(  50, 18 ),   -- VRA Core: Cultural Context
		(  51, 19 )    -- VRA Core: Inscription
	) as D ( MetadataTypeID, DisplayOrder )
	where exists ( select 1 from SobekCM_Metadata_Types T where T.MetadataTypeID = D.MetadataTypeID );
end;
GO
