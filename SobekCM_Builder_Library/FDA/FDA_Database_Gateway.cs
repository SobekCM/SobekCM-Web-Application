using System;
using System.Collections.Generic;
using System.Data;
using EngineAgnosticLayerDbAccess;
using SobekCM.Tools.FDA;

namespace SobekCM.Builder_Library.FDA
{
    /// <summary> Gateway to the FDA stored procedures, used by Florida SUS schools for saving
    /// archiving reports regarding their digital resources to the database </summary>
    public class FDA_Database_Gateway
    {

        /// <summary> Connection string to the main SobekCM databaase </summary>
		/// <remarks> This database hold all the information about items, item aggregationPermissions, statistics, and tracking information</remarks>
		public static string Connection_String { get; set; }

		/// <summary> Gets the type of database ( i.e. MSSQL v. PostgreSQL ) </summary>
		public static EalDbTypeEnum DatabaseType { get; set; }

		/// <summary> Static constructor for this class </summary>
        static FDA_Database_Gateway()
		{
			DatabaseType = EalDbTypeEnum.MSSQL;
		}


        /// <summary> Saves all the pertinent information from a received Florida Digital Archive (FDA) ingest report </summary>
        /// <param name="Report"> Object containing all the data from the received FDA report </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool FDA_Report_Save(FDA_Report_Data Report)
        {
            // Try to get the bibid and vid from the package name
            string bibid = String.Empty;
            string vid = String.Empty;
            if ((Report.Package.Length == 16) && (Report.Package[10] == '_'))
            {
                bibid = Report.Package.Substring(0, 10);
                vid = Report.Package.Substring(11, 5);
            }

            // If the package name was bib id without VID
            if (Report.Package.Length == 10)
            {
                bibid = Report.Package;
            }

            // Save the report information to the database
            int reportid = FDA_Report_Save(Report.Package, Report.IEID, Report.Report_Type_String, Report.Date, Report.Account, Report.Project, Report.Warnings, Report.Message_Note, bibid, vid);

            // If no error, continue
            return reportid > 0;
        }

        /// <summary> Save the information about a FDA report to the database </summary>
        /// <param name="Package">ID of the submission package sent to FDA.  (End user's id)</param>
        /// <param name="Ieid">Intellectual Entity ID assigned by FDA</param>
        /// <param name="FdaReportType">Type of FDA report received</param>
        /// <param name="ReportDate">Date FDA was generated</param>
        /// <param name="Account">Account information for the FDA submission package</param>
        /// <param name="Project">Project information for the FDA submission package</param>
        /// <param name="Warnings">Number of warnings in this package</param>
        /// <param name="BibID">Bibliographic Identifier</param>
        /// <param name="Vid">Volume Identifier</param>
        /// <param name="Message"> Message included in the FDA report received </param>
        /// <returns>Primary key for the report in the database, or -1 on failure</returns>
        /// <remarks>This calls the FDA_Report_Save stored procedure in the database</remarks>
        public static int FDA_Report_Save(string Package, string Ieid, string FdaReportType, DateTime ReportDate, string Account, string Project, int Warnings, string Message, string BibID, string Vid)
        {
            // If there is no connection string, return -1
            if (Connection_String.Length == 0)
                return -1;

            try
            {
                // Build the parameters list
                List<EalDbParameter> parameters = new List<EalDbParameter>
                {
                    new EalDbParameter("@Package", Package), 
                    new EalDbParameter("@IEID", Ieid), 
                    new EalDbParameter("@FdaReportType", FdaReportType), 
                    new EalDbParameter("@Report_Date", ReportDate), 
                    new EalDbParameter("@Account", Account), 
                    new EalDbParameter("@Project", Project), 
                    new EalDbParameter("@Warnings", Warnings), 
                    new EalDbParameter("@Message", Message), 
                    new EalDbParameter("@BibID", BibID), 
                    new EalDbParameter("@VID", Vid)
                };

                // Add a final parameter to receive the primary key back from the database
                EalDbParameter fdaReportParameter = new EalDbParameter("@FdaReportID", -1) { Direction = ParameterDirection.InputOutput };
                parameters.Add(fdaReportParameter);

                // Run the query
                EalDbAccess.ExecuteNonQuery(DatabaseType, Connection_String, CommandType.StoredProcedure, "FDA_Report_Save", parameters);

                // Get and return the primary key
                return Convert.ToInt32(fdaReportParameter.Value);
            }
            catch
            {
                // In the case of an error, return -1
                return -1;
            }
        }
    }
}
