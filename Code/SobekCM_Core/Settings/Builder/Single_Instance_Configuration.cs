using SobekCM.Core.Configuration;
using SobekCM.Core.MicroservicesClient;
using System;

namespace SobekCM.Builder_Library.Settings
{
    /// <summary> Information about a single instance of SobekCM maintained
    /// by the SobekCM builder application </summary>
    public class Single_Instance_Configuration
    {
        /// <summary> Database connection information for this instance </summary>
        public Database_Instance_Configuration DatabaseConnection { get; set; }

        /// <summary> Name for this instance of SobekCM </summary>
        /// <remarks> This is only used by the SobekCM builder to be able to report the instance
        /// name, in the event that the database referenced is inaccessible. </remarks>
        public string Name { get; set; }

        /// <summary> Flag indicates if this database instance is active for the builder </summary>
        public bool Is_Active { get; set; }

        /// <summary> Collection of all the microservice endpoints necessary for the builder on this instance </summary>
        public MicroservicesClient_Configuration Microservices { get; set; }

        /// <summary> Full path to this instance's GCS service account JSON key file, read directly from
        /// this Instance's block in the Builder's own sobekcm.config -- deliberately kept out of the
        /// database (unlike <see cref="Database_Instance_Configuration.Connection_String"/>) since the
        /// Builder machine is expected to keep its own local copy of the key, and different instances
        /// serviced by the same Builder may eventually use different service accounts. Empty/not
        /// configured means this instance's file system falls back to the same Base_Directory-relative
        /// default the web application uses. </summary>
        public string Gcs_Service_Account_Json_Path { get; set; }

        /// <summary> Constructor for a new instance of the Single_Instance_Configuration class </summary>
        public Single_Instance_Configuration()
        {
            Is_Active = true;
            Name = String.Empty;
            Microservices = new MicroservicesClient_Configuration();
            DatabaseConnection = new Database_Instance_Configuration();
            Gcs_Service_Account_Json_Path = String.Empty;

        }
    }
}
