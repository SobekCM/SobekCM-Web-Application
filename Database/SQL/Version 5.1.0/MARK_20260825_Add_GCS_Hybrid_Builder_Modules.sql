-- Adds the two Builder modules needed for "GCS Hybrid" file system mode: staging existing
-- GCS-hosted files back to local disk before reprocessing an item, and pushing master
-- image files to GCS (deleting the local scratch copy) once the rest of the item's
-- processing has finished. Both modules self-no-op when File System Mode is "Local", so
-- these rows are safe to enable unconditionally in every deployment.

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.StageResourceFilesLocallyModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Stage existing GCS-hosted files locally before reprocessing, in GCS Hybrid mode', 'SobekCM.Builder_Library.Modules.Items.StageResourceFilesLocallyModule', 'true', 5);
end;
  GO

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.PushMasterFilesToGcsModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Upload master/derivative image files to GCS and remove the local scratch copy, in GCS Hybrid mode', 'SobekCM.Builder_Library.Modules.Items.PushMasterFilesToGcsModule', 'true', 270);
end;
  GO
