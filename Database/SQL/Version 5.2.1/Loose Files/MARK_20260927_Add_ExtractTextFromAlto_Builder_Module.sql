-- Adds the ExtractTextFromAltoModule Builder module, which reads page-level ALTO OCR files
-- (e.g. 0001.alto.xml) and writes a plain-text sidecar (0001.txt) built from each TextLine's
-- String/CONTENT values -- discarding the coordinate/style/confidence attributes that make up
-- the bulk of an ALTO file, and de-duplicating overlapping sibling String elements (some ALTO
-- producers, e.g. multi-engine OCR merges, leave one per contributing engine's guess for the
-- same word) by keeping whichever has the higher WC. It no-ops for any package with no
-- *.alto.xml files, so this row is safe to enable unconditionally in every deployment.
--
-- Order 52 is deliberate: it runs immediately after ExtractTextFromXmlModule (50), which
-- explicitly skips *.alto.xml (a generic tag-stripper produces garbage against ALTO's
-- word/coordinate structure -- see ExtractTextFromXmlModule.cs), and before OcrTiffsModule
-- (80), whose existing-file check (`if (!File.Exists(text_file))`) will now see the .txt this
-- module just wrote and correctly skip a redundant Tesseract/OCR pass over the companion TIFF.
-- It also runs before CheckForSsnModule (100), so text recovered from ALTO gets the same SSN
-- scan as text from every other source.

if (( select count(*) from SobekCM_Builder_Module where [Class]='SobekCM.Builder_Library.Modules.Items.ExtractTextFromAltoModule') = 0)
begin
  insert into SobekCM_Builder_Module (ModuleSetID, ModuleDesc, Class, [Enabled], [Order])
  values (3, 'Extract indexable full text from page-level ALTO OCR files', 'SobekCM.Builder_Library.Modules.Items.ExtractTextFromAltoModule', 'true', 52);
end;
GO
