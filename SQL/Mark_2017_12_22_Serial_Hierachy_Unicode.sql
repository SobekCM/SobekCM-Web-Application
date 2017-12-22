

-- Change level columns to be unicode
alter table SobekCM_Item alter column Level1_Text nvarchar(255) null;
alter table SobekCM_Item alter column Level2_Text nvarchar(255) null;
alter table SobekCM_Item alter column Level3_Text nvarchar(255) null;
alter table SobekCM_Item alter column Level4_Text nvarchar(255) null;
alter table SobekCM_Item alter column Level5_Text nvarchar(255) null;
GO

-- Change stored procedures to use unicode

-- SobekCM_Save_Serial_Hierarchy
-- SobekCM_Save_New_Item
-- 



