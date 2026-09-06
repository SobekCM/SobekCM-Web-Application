-- Adds the SaveStructMapModule Builder module, which restores the structure map (physical,
-- download, and open-textbook division trees) and main thumbnail reference onto a
-- METADATA_UPDATE package from the item's currently-published METS -- a METADATA_UPDATE
-- submission carries only updated descriptive metadata, with no structMap of its own, so
-- without this the item's file list and thumbnail would otherwise get wiped out on save.
-- It then immediately re-saves the merged item back over the incoming METS file, in the
-- processing folder it was just read from. No-ops immediately for any package that isn't a
-- METADATA_UPDATE, so this row is safe to enable unconditionally in every deployment.
--
-- Order 155 is deliberate: it must run BEFORE MoveFilesToImageServerModule (160), so the METS
-- file that module renames to "recd_....mets.bak" and carries into the final image-server
-- folder is already the complete, merged one -- ReloadMetsAndBasicDbInfoModule (170)
-- unconditionally re-reads whatever METS is on disk at that point and wholesale-replaces the
-- in-memory item, so running this module any later than 160 would just get silently
-- discarded by that re-read. Everything in between (180-230) doesn't matter either way, since
-- all of those already short-circuit for a METADATA_UPDATE package in code (source control
-- commit 6edf9c61, "Early exit on many builder modules when metadata update").

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.SaveStructMapModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Restore the structure map and main thumbnail from the active METS, for metadata-only updates', 'SobekCM.Builder_Library.Modules.Items.SaveStructMapModule', 'true', 155);
end;
GO
