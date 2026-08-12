using BriefItemRewriter;
using SobekCM.Builder_Library.Settings;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

// Running spot -- both the content root (used by config-file-path lookups elsewhere, e.g.
// DatabaseConnectionInitializer) and the Builder settings' executable directory below need to agree on
// this same directory, so compute it once and reuse it for both.
string startupDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
ContentRoot_Gateway.ContentRootPath = startupDirectory + "\\";

// Set the default metadata configuration values
ResourceObjectSettings.MetadataConfig.Set_Default_Values();
ResourceObjectSettings.MetadataConfig.Finalize_Metadata_Configuration();

Console.WriteLine("Reading connection string information from config");

// Same sobekcm.config-reading pattern as SolrReindexer/Solr_Reindexer_Controller: locate config\sobekcm.config
// next to the running executable, read it, and use the first active instance's connection string
MultiInstance_Builder_Settings.Builder_Executable_Directory = startupDirectory;

string configurationFile = Path.Combine(startupDirectory ?? String.Empty, "config", "sobekcm.config");
if (!File.Exists(configurationFile))
{
    Console.WriteLine("Configuration file is missing: " + configurationFile);
    return;
}

if (!MultiInstance_Builder_Settings_Reader.Read_Config(configurationFile))
{
    Console.WriteLine("Error encountered reading the configuration file: " + configurationFile);
    return;
}

Single_Instance_Configuration instance = MultiInstance_Builder_Settings.Instances.FirstOrDefault(x => x.Is_Active);
if ((instance == null) || (String.IsNullOrEmpty(instance.DatabaseConnection.Connection_String)))
{
    Console.WriteLine("No active instance with a valid connection string was found in the configuration file");
    return;
}

Console.WriteLine("Refresh the engine cache settings and configuration");
Engine_ApplicationCache_Gateway.RefreshAll(instance.DatabaseConnection, "\\\\sobek-frontend\\wwwroot\\test");

var briefItemWriter = new BriefItemWriter();

Console.WriteLine("Getting list of all mets files");
var mets_files = Directory.GetFiles("C:\\METS", "*.mets.xml");

int complete = 0;
foreach( var mets_file in mets_files )
{
    if ( !File.Exists(mets_file) )
    {
        Console.WriteLine("METS file does not exist: " + mets_file);
        complete++;
        continue;
    }
    
    Console.WriteLine();
    Console.WriteLine("Processing METS file: " + mets_file);

    // May seem useless, but will be useful later...
    var directory = (new FileInfo(mets_file)).Directory.ToString();

    
    DateTime start = DateTime.Now;
    SobekCM_Item metsItem = SobekCM_Item.Read_METS(mets_file);
    Console.WriteLine("     Read METS file in " + (DateTime.Now - start).TotalSeconds + " seconds");

    string protobugFile = Path.Combine(directory, metsItem.BibID + "_" + metsItem.VID + ".protobuf");

    if (!File.Exists(protobugFile))
    {
        briefItemWriter.WriteBriefItemProtobuf(metsItem, protobugFile);
    }

    // Test read the Brief item
    start = DateTime.Now;
    var briefItem = briefItemWriter.ReadBriefItemProtobuf(protobugFile);
    Console.WriteLine("     Read Brief item in " + (DateTime.Now - start).TotalSeconds + " seconds");

    // Read the SAME file again immediately -- if this second read is dramatically faster, the first
    // read's cost is a one-time warm-up (e.g. protobuf-net's reflection/IL-emit model compilation for
    // this type, possibly per distinct metadata-module subtype), not something inherent to every read.
    start = DateTime.Now;
    var briefItemSecondRead = briefItemWriter.ReadBriefItemProtobuf(protobugFile);
    Console.WriteLine("     Read Brief item AGAIN (same file) in " + (DateTime.Now - start).TotalSeconds + " seconds");

    complete++;
    Console.WriteLine($"Completed {metsItem.BibID}:{metsItem.VID}  ( {complete} out of {mets_files.Length} )");
}

Console.WriteLine("Complete");
Console.ReadKey();