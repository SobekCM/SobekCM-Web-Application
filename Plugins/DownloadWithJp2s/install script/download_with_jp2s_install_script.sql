-- Add DOWNLOADS (with JPEG2000) as item view type
if ( not exists ( select 1 from SobekCM_Item_Viewer_Types where ViewType = 'DOWNLOADS_JP2s' ))
begin
	
	insert into SobekCM_Item_Viewer_Types ( ViewType, [Order], DefaultView, MenuOrder )
	values ( 'DOWNLOADS_JP2s', 12, 0, 500.2 );

end;
GO

-- You can use this portion to change items in bulk to use the new downloads viewer, rather than the old
-- one.  Just change the collection code below to your collection code.
declare @collection_code varchar(20);
set @collection_code = 'COLLECTION_CODE_HERE';

declare @collectionid int;
set @collectionid = ( select AggregationID from SobekCM_Item_Aggregation where Code=@collection_code );

declare @old_download_id int;
set @old_download_id = ( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType='DOWNLOADS');

declare @new_download_id int;
set @new_download_id = ( select ItemViewTypeID from SobekCM_Item_Viewer_Types where ViewType='DOWNLOADS_JP2s');

if (( coalesce(@collectionid, -1 ) > 0 ) and ( coalesce(@old_download_id, -1 ) > 0 ) and ( coalesce( @new_download_id, -1) > 0 ))
begin
	update SobekCM_Item_Viewers
	set ItemViewTypeID = @new_download_id
	where ItemViewTypeID=@old_download_id
	  and exists ( select 1 from SobekCM_Item_Aggregation_Item_Link L where L.ItemID=SobekCM_Item_Viewers.ItemID and L.AggregationID = @collectionid );
end
else
begin
	print 'Error trying to assign the new view to the collection.. some value is null';
	print '     Collection ID = ' + cast(coalesce(@collectionid, -1) as varchar(5));
	print '     Old Downloads ID = ' + cast(coalesce(@old_download_id, -1) as varchar(5));
	print '     New Downloads ID = ' + cast(coalesce(@new_download_id, -1) as varchar(5));
end;
GO


