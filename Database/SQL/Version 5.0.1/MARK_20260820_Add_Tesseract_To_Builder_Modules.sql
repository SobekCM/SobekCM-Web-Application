
if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.TesseractOcrModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Use Tesseract to OCR TIFF files for textual material', 'SobekCM.Builder_Library.Modules.Items.TesseractOcrModule', 'true', 90);
end;
  GO

  update SobekCM_Builder_Module set ModuleDesc = 'OCR Tiff files with a generic OCR command prompt' where [Class]='SobekCM.Builder_Library.Modules.Items.OcrTiffsModule';
  GO
