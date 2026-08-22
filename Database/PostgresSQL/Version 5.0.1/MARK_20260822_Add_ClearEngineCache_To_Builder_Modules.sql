
insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, Enabled, "Order")
select 3, 'Tell the engine to clear its in-memory cache for this item', 'SobekCM.Builder_Library.Modules.Items.ClearEngineCacheModule', true, 320
where not exists (select 1 from SobekCM_Builder_Module where Class = 'SobekCM.Builder_Library.Modules.Items.ClearEngineCacheModule');
