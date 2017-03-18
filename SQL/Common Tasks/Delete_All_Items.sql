

	-- Delete all item level tables
	delete from SobekCM_Metadata_Unique_Link;
	delete from SobekCM_Metadata_Basic_Search_Table;
	delete from SobekCM_Item_Footprint;
	delete from SobekCM_Item_Icons;
	delete from SobekCM_Item_Statistics;
	delete from SobekCM_Item_GeoRegion_Link;
	delete from SobekCM_Item_Aggregation_Item_Link;
	delete from mySobek_User_Item;
	delete from mySobek_User_Item_Link;
	delete from mySobek_User_Description_Tags;
	delete from SobekCM_Item_Viewers;
	delete from Tracking_Item;
	delete from Tracking_Progress;
	delete from SobekCM_Item_OAI;
	delete from Tracking_Archive_Item_Link;
	delete from SobekCM_QC_Errors;
	delete from SobekCM_QC_Errors_History;
	delete from SobekCM_Item;


	-- delete from the item group table	and all references
	delete from SobekCM_Item_Group_External_Record;
	delete from SobekCM_Item_Group_Web_Skin_Link;
	delete from SobekCM_Item_Group_Statistics;
	delete from mySobek_User_Bib_Link;
	delete from SobekCM_Item_Group_OAI;
	delete from SobekCM_Item_Group;