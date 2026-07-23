using EngineAgnosticLayerDbAccess;
using SobekCM.Engine_Library.Database;
using System.Data;
using System.Globalization;

// Folder holding the archiving manifest (*_archived.txt) files to import
string location = @"\\sobek-frontend\Files\ver5";

// Name of the Archive_Location row these files were stored to (must already exist in Archive_Location)
string locationName = "GCS Cold Storage";

string db_connection_string = "data source=sobek-backend\\SQLEXPRESS;initial catalog=ver5;user id=sobekcm-sql;password=zda5hef9afx9WBA@wbe;TrustServerCertificate=True;Encrypt=False;";

Engine_Database.DatabaseType = EalDbTypeEnum.MSSQL;
Engine_Database.Connection_String = db_connection_string;

Console.WriteLine("Pulling the full item list (including privates)...");
DataSet itemListSet = Engine_Database.Item_List(true, null);
if ((itemListSet == null) || (itemListSet.Tables.Count == 0))
{
    Console.WriteLine("ERROR: Unable to pull the item list.  Aborting.");
    Console.ReadKey();
    return;
}

// Build a BibID/VID -> ItemID lookup
var itemIdLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
foreach (DataRow itemRow in itemListSet.Tables[0].Rows)
{
    string bibid = itemRow["BibID"].ToString() ?? string.Empty;
    string vid = itemRow["VID"].ToString() ?? string.Empty;
    int itemId = Convert.ToInt32(itemRow["ItemID"]);

    itemIdLookup[bibid + "_" + vid] = itemId;
}
Console.WriteLine($"Found {itemIdLookup.Count} items.");

string[] manifestFiles = Directory.GetFiles(location, "*_archived.txt");
Console.WriteLine($"Found {manifestFiles.Length} archiving manifest file(s) in {location}.");

int filesSaved = 0;
int filesSkipped = 0;
int manifestErrors = 0;

foreach (string manifestFile in manifestFiles)
{
    string manifestFileName = Path.GetFileName(manifestFile);

    try
    {
        // Parse the manifest file name, e.g. 'AA00008198_00001_20260214_archived.txt'
        string fileNameSansExtension = Path.GetFileNameWithoutExtension(manifestFile);
        string[] nameParts = fileNameSansExtension.Split('_');
        if (nameParts.Length < 3)
        {
            Console.WriteLine($"  SKIPPED {manifestFileName}: Unable to parse BibID/VID/date from the file name.");
            manifestErrors++;
            continue;
        }

        string bibid = nameParts[0];
        string vid = nameParts[1];
        string folderDateString = nameParts[2];

        if (!itemIdLookup.TryGetValue(bibid + "_" + vid, out int itemId))
        {
            Console.WriteLine($"  SKIPPED {manifestFileName}: No matching item found for BibID '{bibid}', VID '{vid}'.");
            manifestErrors++;
            continue;
        }

        // The folder name used during archiving doubles as the date this batch was stored
        if (!DateTime.TryParseExact(folderDateString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime storedDate))
        {
            Console.WriteLine($"  SKIPPED {manifestFileName}: Unable to parse archive date '{folderDateString}'.");
            manifestErrors++;
            continue;
        }

        Console.WriteLine($"Processing {manifestFileName} (ItemID {itemId})...");

        string[] lines = File.ReadAllLines(manifestFile);
        for (int i = 1; i < lines.Length; i++)   // skip the header row
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                string[] fields = line.Split('\t');
                if (fields.Length < 4)
                {
                    Console.WriteLine($"    SKIPPED line {i + 1}: Expected 4 tab-separated fields, found {fields.Length}.");
                    filesSkipped++;
                    continue;
                }

                string fileName = fields[0];
                long fileSize = long.Parse(fields[1], CultureInfo.InvariantCulture);
                DateTime originalCreationDate = DateTime.Parse(fields[2], CultureInfo.InvariantCulture);
                string sha256Hash = fields[3];

                // The storage path is relative to the archive location's root, matching the
                // BibID/VID/date folder structure used during archiving
                string storagePath = Path.Combine(bibid, vid, folderDateString, fileName);

                string mimeType = mime_type_from_extension(Path.GetExtension(fileName));

                bool saved = Engine_Database.Archive_Save_File(itemId, fileName, fileSize, sha256Hash, originalCreationDate, storagePath, storedDate, locationName, mimeType, null, null);
                if (saved)
                {
                    filesSaved++;
                }
                else
                {
                    Console.WriteLine($"    ERROR saving {fileName}: {Engine_Database.Last_Exception?.Message}");
                    filesSkipped++;
                }
            }
            catch (Exception lineEx)
            {
                Console.WriteLine($"    ERROR on line {i + 1} of {manifestFileName}: {lineEx.Message}");
                filesSkipped++;
            }
        }
    }
    catch (Exception manifestEx)
    {
        Console.WriteLine($"  ERROR processing {manifestFileName}: {manifestEx.Message}");
        manifestErrors++;
    }
}

Console.WriteLine();
Console.WriteLine($"Files saved: {filesSaved}");
Console.WriteLine($"Files skipped due to errors: {filesSkipped}");
Console.WriteLine($"Manifests skipped due to errors: {manifestErrors}");
Console.WriteLine("Complete");
Console.ReadKey();


static string mime_type_from_extension(string extension)
{
    // Basic extension-based mapping only, for now -- see MimeTypeCalculationMethod discussion
    // for when byte-level sniffing gets added later
    return extension.ToLower() switch
    {
        ".tif" or ".tiff" => "image/tiff",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".jp2" => "image/jp2",
        ".pdf" => "application/pdf",
        _ => string.Empty
    };
}
