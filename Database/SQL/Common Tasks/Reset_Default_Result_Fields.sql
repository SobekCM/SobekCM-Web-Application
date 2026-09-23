-- Resets the install-wide default result fields (SobekCM_Item_Aggregation_Default_Result_Fields)
-- to the standard list introduced in 5.2.0.  Collections use these defaults for any result view
-- without fields of their own.
--
-- The 5.2.0 upgrade only seeds that table when it is empty, so it leaves alone any database that
-- already has rows: either customized ones, or the older list from Upgrade_to_Ver500.sql (which put
-- Publication Date at the same display order as Creator).  Run this to discard those rows and start
-- from the standard list.  This is deliberately NOT part of any upgrade script.
--
-- Nothing else references this table, so clearing it is safe.  The same statements work in
-- PostgreSQL, except the seed insert, which is in the 5.2.0 PostgreSQL upgrade script instead.


-- 1. See what is there now, before clearing it
select R.ResultType, F.MetadataTypeID, T.DisplayTerm, F.OverrideDisplayTerm, F.DisplayOrder
from SobekCM_Item_Aggregation_Default_Result_Fields F
join SobekCM_Item_Aggregation_Result_Types R on R.ItemAggregationResultTypeID = F.ItemAggregationResultTypeID
join SobekCM_Metadata_Types T on T.MetadataTypeID = F.MetadataTypeID
order by R.ResultType, F.DisplayOrder;
GO


-- 2. Clear the table
delete from SobekCM_Item_Aggregation_Default_Result_Fields;
GO


-- 3. Re-seed with the standard list.  Same as the seed in Version 5.2.0/Upgrade_to_Ver520.sql, so
--    if you run this BEFORE the 5.2.0 upgrade you can stop after step 2 and let the upgrade seed it.
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
