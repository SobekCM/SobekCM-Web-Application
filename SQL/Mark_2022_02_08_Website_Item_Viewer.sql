  insert into SobekCM_Item_Viewer_Types (ViewType, [Order], DefaultView, MenuOrder)
  values ( 'WEBSITE', 14, 0, 116);

  select * from SobekCM_Item_Viewer_Types



  select * from SobekCM_Item_Viewers where ItemViewTypeID=32;

  update SobekCM_Item_Viewers 
  set Label='Presentation'
  where ItemViewID=3530;

  update SobekCM_Item_Viewers
  set Attribute='web/story.html'
  where ItemViewID=3530;