/** 
Upgrade_to_Ver501.sql  - OpenSobek (5.0 Patch 1)

Takes an existing 5.0.0 database and brings it up to Version 5.0.1 (PATCH 1).

 If your version is older than 5.0.0 you will need 
 to find the original update scripts in the Previous Version, and run those 
 to get your db version up to 5.0.0 first. 
 
 */

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.ClearEngineCacheModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Tell the engine to clear its in-memory cache for this item', 'SobekCM.Builder_Library.Modules.Items.ClearEngineCacheModule', 'true', 320);
end;
GO

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.TesseractOcrModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Use Tesseract to OCR TIFF files for textual material', 'SobekCM.Builder_Library.Modules.Items.TesseractOcrModule', 'true', 90);
end;
GO

update SobekCM_Builder_Module set ModuleDesc = 'OCR Tiff files with a generic OCR command prompt' where [Class]='SobekCM.Builder_Library.Modules.Items.OcrTiffsModule';
GO


/**************************************************************************/
/**                                                                      **/
/**   Update Database Version                                            **/
/**                                                                      **/
/**************************************************************************/

-- Update the version number
if (( select count(*) from SobekCM_Database_Version ) = 0 )
begin
	insert into SobekCM_Database_Version ( Major_Version, Minor_Version, Release_Phase )
	values ( 5, 0, '1' );
end
else
begin
	update SobekCM_Database_Version
	set Major_Version=5, Minor_Version=0, Release_Phase='1';
end;
GO