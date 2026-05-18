using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ArchivingTool
{
    public class Archiver
    {
        private string sourceDir;
        private string targetDir;

        private int foldersCount = 0;
        private int tiffsCount = 0;
        private string outputFile;

        public Archiver(string SourceDirectory,  string TargetDirectory)
        {
            // Calculate the output file name (TEMP)
            string fileName = Path.Combine(SourceDirectory, "archiving_" + DateTime.Now.Year + DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0'));
            if (File.Exists(fileName + ".txt"))
            {
                char final_letter = 'A';
                while( File.Exists(fileName + final_letter + ".txt"))
                {
                    final_letter++;
                }
                fileName = fileName + final_letter;
            }
            outputFile = fileName + ".txt";

            sourceDir = SourceDirectory;
            targetDir = TargetDirectory;

            recurse_directories(SourceDirectory, String.Empty, 0);

            Console.WriteLine();
            Console.WriteLine("COMPLETE");
            Console.WriteLine($"Found {tiffsCount} TIFF files in {foldersCount} folders");
            Console.ReadKey();
        }

        public void recurse_directories(string sourceFolder, string folderBuilder, int depth)
        {
            if ( depth == 5 )
            {
                handle_volumes(sourceFolder, folderBuilder);
                return;
            }

            var subfolders = Directory.GetDirectories(sourceFolder);
            foreach (var subfolder in subfolders)
            {
                var subfolderName = Path.GetFileName(subfolder);

                // Skip recycle bin or any other strangely named folders
                if (subfolderName.Length != 2) continue;

                recurse_directories(subfolder, folderBuilder + subfolderName, depth+1);
            }
        }

        public void handle_volumes(string sourceFolder, string folderBuilder)
        {
            var subfolders = Directory.GetDirectories(sourceFolder);
            foreach (var subfolder in subfolders)
            {
                var vidName = Path.GetFileName(subfolder);

                // Skip recycle bin or any other strangely named folders
                if (vidName.Length != 5) continue;

                process_item(folderBuilder, vidName, subfolder);                
            }
        }

        public void process_item(string bibid, string vid, string folder)
        {
            string dateString = DateTime.Now.Year + DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0');

            // Get list of tiff files
            var tiffFiles = Directory.GetFiles(folder, "*.tif").ToList().Concat(Directory.GetFiles(folder, "*.tiff")).ToList();

            // For now, no TIFFs means no archiving (just for this pass)
            if (tiffFiles.Count ==  0) return;


            logData($"{bibid}:{vid} [{tiffFiles.Count}]: {folder}");

            foldersCount++;
            tiffsCount += tiffFiles.Count;

            string destinationFolder = Path.Combine(targetDir, bibid, vid, dateString);

            var archivedFiles = new List<ArchivedFile>();
            foreach ( string thisTiff in tiffFiles)
            {
                var returnValue = process_tiff(thisTiff, folder, destinationFolder);
                if (returnValue != null) archivedFiles.Add(returnValue);
            }

            if (archivedFiles.Count > 0)
            {
                string archiveFileName = bibid + "_" + vid + "_" + dateString + "_archived.txt";
                string archiveDataFile = Path.Combine(sourceDir, archiveFileName);
                StreamWriter writer = new StreamWriter(archiveDataFile);
                writer.WriteLine("FileName\tSize\tCreationDate\tSHA256 Hash");
                foreach (var file in archivedFiles)
                {
                    writer.WriteLine(file.ToString());
                }
                writer.Flush();
                writer.Close();

                string destinationFile = Path.Combine(destinationFolder, archiveFileName);
                if ((File.Exists(archiveDataFile)) && (!File.Exists(destinationFile)))
                    File.Copy(archiveDataFile, destinationFile);
            }

            logData(String.Empty);
        }

        public ArchivedFile process_tiff(string tiff, string folder, string destinationFolder)
        {
            string tiffName = Path.GetFileName(tiff);

            try
            {
                string fileSansExtension = Path.GetFileNameWithoutExtension(tiff);

                // Must have a resulting JPEG file and JP2 file
                if ((!File.Exists(Path.Combine(folder, fileSansExtension + ".jpg"))) || (!File.Exists(Path.Combine(folder, fileSansExtension + ".jp2"))))
                {
                    logData($"\t{tiffName} not processed to jpegs/jp2000's");
                    return null;
                }

                var tiffInfo = new FileInfo(tiff);
                var returnValue = new ArchivedFile { FileName = tiffName, Size = tiffInfo.Length, CreationDate = tiffInfo.CreationTime };

                returnValue.Sha256Hash = ComputeFileSha256Hash(tiff);

                if (!Directory.Exists(destinationFolder)) Directory.CreateDirectory(destinationFolder);

                // Attempt to copy this to the destinationfolder
                string destinationTiff = Path.Combine(destinationFolder, tiffName);
                if (!File.Exists(destinationTiff))
                {
                    File.Copy(tiff, destinationTiff, true);

                    // Check it exists and is same length (check hash eventually?)
                    long newSize = (new FileInfo(destinationTiff)).Length;
                    if ( returnValue.Size == newSize)
                    {
                        File.Delete(tiff);
                        logData($"\t{tiffName} archived");
                        return returnValue;
                    }                     
                }
            }
            catch ( Exception ee )
            {
                logData($"\t{tiffName} encountered an exception ( {ee.Message} )");                
            }

            return null;
        }


        public void logData(string value)
        {
            Console.WriteLine(value);

            using (StreamWriter writer = new StreamWriter(outputFile, true))
            {
                writer.WriteLine(value);
                writer.Flush();
                writer.Close();
            }
        }

        public static string ComputeFileSha256Hash(string filePath)
        {
            // Use 'using' statements to ensure streams and hash objects are properly disposed
            using (FileStream stream = File.OpenRead(filePath))
            {
                // Create an instance of the hash algorithm
                using (SHA256 sha256 = SHA256.Create())
                {
                    // Compute the hash of the file stream
                    byte[] hashBytes = sha256.ComputeHash(stream);

                    // Convert the byte array to a hexadecimal string
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2")); // "x2" formats as a two-digit hexadecimal number
                    }
                    return sb.ToString();
                }
            }
        }

    }
}
