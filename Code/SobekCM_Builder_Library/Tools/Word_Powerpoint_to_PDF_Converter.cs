#region Using directives

using System;
using System.Diagnostics;
using System.IO;
using SobekCM.Builder_Library.Settings;

#endregion

namespace SobekCM.Builder_Library.Tools
{
    /// <summary> Class is used to convert Word and Powerpoint files to PDF, by shelling out to a
    /// LibreOffice headless conversion (soffice --headless --convert-to pdf). LibreOffice is free,
    /// cross-platform (Windows and Linux), and does not require MS Office or any COM automation. </summary>
    public static class Word_Powerpoint_to_PDF_Converter
    {
        /// <summary> Convert a Microsoft Word file to a PDF </summary>
        /// <param name="Word_In_File"> Input file (word) </param>
        /// <param name="PDF_Out_File"> Outpuf file (pdf) </param>
        /// <returns>An error value</returns>
        public static int Word_To_PDF( string Word_In_File, string PDF_Out_File )
        {
            return Convert_To_PDF(Word_In_File, PDF_Out_File);
        }

        /// <summary> Convert a Microsoft Powerpoint file to a PDF </summary>
        /// <param name="Powerpoint_In_File"> Input file (powerpoint) </param>
        /// <param name="PDF_Out_File"> Outpuf file (pdf) </param>
        /// <returns>An error value</returns>
        public static int Powerpoint_To_PDF(string Powerpoint_In_File, string PDF_Out_File)
        {
            return Convert_To_PDF(Powerpoint_In_File, PDF_Out_File);
        }

        /// <summary> Convert a Word or Powerpoint file to a PDF via a LibreOffice headless conversion </summary>
        /// <param name="Input_File"> Input file (word or powerpoint) </param>
        /// <param name="PDF_Out_File"> Output file (pdf) </param>
        /// <returns>An error value</returns>
        /// <remarks> LibreOffice's headless converter only lets you choose an output directory, not an
        /// output filename - it always names the result after the input file's base name.  So this
        /// converts into a scratch temp directory and then moves the result to the requested output path. </remarks>
        private static int Convert_To_PDF(string Input_File, string PDF_Out_File)
        {
            //0 - Converting successfully
            //1 - Can't open input file
            //2 - Can't create output file
            //3 - Converting failed
            //4 - LibreOffice isn't installed/configured

            string libreoffice_executable = MultiInstance_Builder_Settings.LibreOffice_Executable;
            if ((String.IsNullOrEmpty(libreoffice_executable)) || (!File.Exists(libreoffice_executable)))
                return 4;

            if (!File.Exists(Input_File))
                return 1;

            string temp_dir = Path.Combine(Path.GetTempPath(), "sobekcm_lo_" + Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(temp_dir);

                var convert = new Process
                {
                    StartInfo =
                    {
                        FileName = libreoffice_executable,
                        Arguments = "--headless --convert-to pdf --outdir \"" + temp_dir + "\" \"" + Input_File + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                convert.Start();
                convert.StandardOutput.ReadToEnd();
                convert.StandardError.ReadToEnd();

                if (!convert.WaitForExit(120000))
                {
                    try { convert.Kill(); } catch { }
                    return 3;
                }

                string converted_file = Path.Combine(temp_dir, Path.GetFileNameWithoutExtension(Input_File) + ".pdf");
                if (!File.Exists(converted_file))
                    return 3;

                try
                {
                    if (File.Exists(PDF_Out_File))
                        File.Delete(PDF_Out_File);

                    File.Move(converted_file, PDF_Out_File);
                }
                catch
                {
                    return 2;
                }

                return 0;
            }
            catch
            {
                return 3;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(temp_dir))
                        Directory.Delete(temp_dir, true);
                }
                catch
                {
                    // Nothing more to be done if the scratch temp directory can't be cleaned up
                }
            }
        }
    }
}
