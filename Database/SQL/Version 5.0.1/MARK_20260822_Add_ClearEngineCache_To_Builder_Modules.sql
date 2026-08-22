
if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.ClearEngineCacheModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Tell the engine to clear its in-memory cache for this item', 'SobekCM.Builder_Library.Modules.Items.ClearEngineCacheModule', 'true', 320);
end;
  GO
