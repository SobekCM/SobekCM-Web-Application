#region Using directives

using System;
using System.IO;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module updates the item-level web.config file based on restriction information </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class UpdateWebConfigModule : abstractSubmissionPackageModule
    {
        /// <summary> Updates the item-level web.config file based on restriction information </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("UpdateWebConfigModule.DoWork");

            // Delete any existing web.config file and write is as necessary
            try
            {
                string web_config = Path.Combine(Resource.Resource_Folder, "web.config");
                if (File.Exists(web_config))
                    File.Delete(web_config);
                if ((Resource.Metadata.Behaviors.Dark_Flag) || (Resource.Metadata.Behaviors.IP_Restriction_Membership > 0))
                {
                    var writer = new StreamWriter(web_config, false);
                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    writer.WriteLine("<configuration>");
                    writer.WriteLine("    <system.webServer>");
                    writer.WriteLine("        <security>");
                    writer.WriteLine("            <ipSecurity allowUnlisted=\"false\">");
                    writer.WriteLine("                 <clear />");
                    writer.WriteLine("                 <add ipAddress=\"127.0.0.1\" allowed=\"true\" />");
                    if ( !String.IsNullOrEmpty(Settings.Servers.SobekCM_Web_Server_IP))
                        writer.WriteLine("                 <add ipAddress=\"" + Settings.Servers.SobekCM_Web_Server_IP.Trim() + "\" allowed=\"true\" />");
                    writer.WriteLine("            </ipSecurity>");
                    writer.WriteLine("        </security>");
                    writer.WriteLine("        <modules runAllManagedModulesForAllRequests=\"true\" />");
                    writer.WriteLine("    </system.webServer>");

                    // Is there now a main thumbnail?
                    if ((Resource.Metadata.Behaviors.Main_Thumbnail.Length > 0) && (Resource.Metadata.Behaviors.Main_Thumbnail.IndexOf("http:") < 0))
                    {
                        writer.WriteLine("    <location path=\"" + Resource.Metadata.Behaviors.Main_Thumbnail + "\">");
                        writer.WriteLine("        <system.webServer>");
                        writer.WriteLine("            <security>");
                        writer.WriteLine("                <ipSecurity allowUnlisted=\"true\" />");
                        writer.WriteLine("            </security>");
                        writer.WriteLine("        </system.webServer>");
                        writer.WriteLine("    </location>");
                    }

                    writer.WriteLine("</configuration>");
                    writer.Flush();
                    writer.Close();
                }
            }
            catch (Exception ee)
            {
                OnError("Unable to update the resource web.config file", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                Tracer?.Add_Trace("UpdateWebConfigModule.DoWork", "Unable to update the resource web.config file: " + ee.Message, Custom_Trace_Type_Enum.Error);
                return false;
            }

            return true;
        }
    }
}
