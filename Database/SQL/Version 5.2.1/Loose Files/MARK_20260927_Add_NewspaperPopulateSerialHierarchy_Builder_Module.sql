-- Adds the NewspaperPopulateSerialHierarchyModule Builder module, which generates a newspaper item's
-- serial hierarchy (year/month/day) from its date issued when the incoming package did not already
-- supply one. The generation logic itself lives in Serial_Info.Synchronize_Newspaper_Hierarchy_With_Date_Issued,
-- shared with the online new-item-submission and metadata-editing paths -- this module just invokes it
-- for whatever package the builder is currently processing. It no-ops for non-newspaper items and for
-- newspaper items that already have a hierarchy, so this row is safe to enable unconditionally in every
-- deployment.
--
-- Order 235 is deliberate: it runs after UpdateWebConfigModule (230), and immediately before
-- SaveServiceMetsModule (240), so any hierarchy it generates is captured in the service METS that
-- module writes out.

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.NewspaperPopulateSerialHierarchyModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Generate a newspaper item''s serial hierarchy from its date issued, if missing', 'SobekCM.Builder_Library.Modules.Items.NewspaperPopulateSerialHierarchyModule', 'true', 235);
end;
GO
