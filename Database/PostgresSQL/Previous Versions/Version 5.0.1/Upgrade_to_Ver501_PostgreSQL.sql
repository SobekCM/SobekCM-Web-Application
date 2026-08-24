/**
Upgrade_to_Ver501_PostgreSQL.sql  - OpenSobek (5.0 Patch 1)

Takes an existing 5.0.0 PostgreSQL database and brings it up to Version 5.0.1 (PATCH 1).

 This is the first PostgreSQL upgrade script -- PostgreSQL support was introduced in
 5.0.0, so there is no earlier PostgreSQL upgrade path to run first. If you are running
 5.0.0, this is the only script you need.

 */

DO $$
BEGIN
  IF (select count(*) from SobekCM_Builder_Module where Class = 'SobekCM.Builder_Library.Modules.Items.ClearEngineCacheModule') = 0 THEN
    insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, Enabled, "Order")
    values (3, 'Tell the engine to clear its in-memory cache for this item', 'SobekCM.Builder_Library.Modules.Items.ClearEngineCacheModule', true, 320);
  END IF;
END $$;

DO $$
BEGIN
  IF (select count(*) from SobekCM_Builder_Module where Class = 'SobekCM.Builder_Library.Modules.Items.TesseractOcrModule') = 0 THEN
    insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, Enabled, "Order")
    values (3, 'Use Tesseract to OCR TIFF files for textual material', 'SobekCM.Builder_Library.Modules.Items.TesseractOcrModule', true, 90);
  END IF;
END $$;

update SobekCM_Builder_Module set ModuleDesc = 'OCR Tiff files with a generic OCR command prompt' where Class = 'SobekCM.Builder_Library.Modules.Items.OcrTiffsModule';


/**************************************************************************/
/**                                                                      **/
/**   Update Database Version                                            **/
/**                                                                      **/
/**************************************************************************/

DO $$
BEGIN
  IF (select count(*) from SobekCM_Database_Version) = 0 THEN
    insert into SobekCM_Database_Version (Major_Version, Minor_Version, Release_Phase)
    values (5, 0, '1');
  ELSE
    update SobekCM_Database_Version
    set Major_Version = 5, Minor_Version = 0, Release_Phase = '1';
  END IF;
END $$;
