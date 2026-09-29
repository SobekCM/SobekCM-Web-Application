-- Removes the "Manage GeoSpatial Data" setting. It only showed or hid the old (beta) geo-spatial editor
-- in an item's Manage menu. That editor was replaced by two mySobek tools (Set Location Points and
-- Georeference Page Images), which are now shown whenever a Google Maps API key is configured, since
-- they cannot work without one.
--
-- The same statement works unchanged against the PostgreSQL database.

delete from SobekCM_Settings where Setting_Key = 'Manage GeoSpatial Data';
GO
