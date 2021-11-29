#region Using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using EngineAgnosticLayerDbAccess;
using SobekCM.Core.Items;
using SobekCM.Core.OAI;
using SobekCM.Core.Results;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Database;
using SobekCM.Library.HTML;
using SobekCM.Library.MainWriters;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Behaviors;
using SobekCM.Tools;
using SobekCM.Tools.FDA;

#endregion

namespace SobekCM.Library.Database
{
	/// <summary> Gateway to the databases used by SobekCM </summary>
	public class SobekCM_Database
	{

        private const int LOOKAHEAD_FACTOR = 5;

		private static string connectionString;
		private static Exception lastException;

        /// <summary> Gets the type of database ( i.e. MSSQL v. PostgreSQL ) </summary>
        public static EalDbTypeEnum DatabaseType { get; set; }

        /// <summary> Static constructor for this class </summary>
        static SobekCM_Database()
        {
            DatabaseType = EalDbTypeEnum.MSSQL;
        }


		#region Temporary dataset work 

		//static SobekCM_Database()
		//{
		//	DataSet set = EPC_Export;

		//	set.DataSetName = "Key_West_City_Directory";
		//	set.Tables[0].TableName = "Person";
		//	set.Tables[1].TableName = "Corporation";
		//	set.Tables[2].TableName = "Person_Occupation_Link";

		//	// Declare parent column and child column variables.
		//	DataColumn parentColumn;
		//	DataColumn childColumn;
		//	ForeignKeyConstraint foreignKeyConstraint;

		//	// Set parent and child column variables.
		//	parentColumn = set.Tables[0].Columns[0];
		//	childColumn = set.Tables[2].Columns[0];
		//	UniqueConstraint uniqueCon = new UniqueConstraint("PersonIdUniqueConstraint", parentColumn);
		//	set.Tables[0].Constraints.Add(uniqueCon);
		//	foreignKeyConstraint = new ForeignKeyConstraint("PersonOccupationForeignKeyConstraint", parentColumn, childColumn);

		//	set.Tables[2].Constraints.Add(foreignKeyConstraint);




		//	// Declare parent column and child column variables.
		//	DataColumn parentColumn2;
		//	DataColumn childColumn2;
		//	ForeignKeyConstraint foreignKeyConstraint2;

		//	// Set parent and child column variables.
		//	parentColumn2 = set.Tables[1].Columns[0];
		//	childColumn2 = set.Tables[2].Columns[1];
		//	UniqueConstraint uniqueCon2 = new UniqueConstraint("CorporationIdUniqueConstraint", parentColumn2);
		//	set.Tables[1].Constraints.Add(uniqueCon2);
		//	foreignKeyConstraint2 = new ForeignKeyConstraint("CorporationOccupationForeignKeyConstraint", parentColumn2, childColumn2);

		//	set.Tables[2].Constraints.Add(foreignKeyConstraint2);

		//	DataColumn uniqueColumn = set.Tables[1].Columns[1];
		//	UniqueConstraint uniqueCon3 = new UniqueConstraint("CorporationNameUniqueConstraint", uniqueColumn);
		//	set.Tables[1].Constraints.Add(uniqueCon3);

		//	set.Tables[0].Columns["Sex"].MaxLength = 1;


		//	set.Tables[0].ExtendedProperties.Add("Description", "Table contains information about all of the people which appeared within the city directory.  These may also be linked to a corporate body and/or an historic occupation.");
		//	set.Tables[0].Columns[0].Caption = "Unique key for this person within the dataset";
		//	set.Tables[0].Columns[0].AllowDBNull = false;
		//	set.Tables[0].Columns[0].AutoIncrement = true;
		//	set.Tables[0].Columns[1].Caption = "Title of the city directory from which this person's data was derived";
		//	set.Tables[0].Columns[1].AllowDBNull = false;
		//	set.Tables[0].Columns[2].Caption = "Year of the city directory from which this person's data was derived";
		//	set.Tables[0].Columns[2].AllowDBNull = false;
		//	set.Tables[0].Columns[3].Caption = "Page sequence within the city directory that this person's data was derived";
		//	set.Tables[0].Columns[4].Caption = "Original source line from which this person's data was derived";
		//	set.Tables[0].Columns[5].Caption = "Last or family name of this person";
		//	set.Tables[0].Columns[5].AllowDBNull = false;
		//	set.Tables[0].Columns[6].Caption = "First or given name of this person";
		//	set.Tables[0].Columns[6].AllowDBNull = false;
		//	set.Tables[0].Columns[7].Caption = "Middle name of this person";
		//	set.Tables[0].Columns[8].Caption = "Title associated with this person ( i.e., Mr., Reverand, etc..)";
		//	set.Tables[0].Columns[9].Caption = "Normalized racial-ethno affiliation associated with this person by the city directory";
		//	set.Tables[0].Columns[10].Caption = "Name of any spouse (usually just the first name)";
		//	set.Tables[0].Columns[11].Caption = "General note field";
		//	set.Tables[0].Columns[12].Caption = "Location of the home, usually cross-streets or an street address";

		//	set.Tables[1].ExtendedProperties.Add("Description", "Table contains all the corporate bodies from the city directory which were linked directly to a person.");
		//	set.Tables[1].Columns[0].Caption = "Unique key for this corporate body within the dataset";
		//	set.Tables[1].Columns[0].AllowDBNull = false;
		//	set.Tables[1].Columns[0].AutoIncrement = true;
		//	set.Tables[1].Columns[1].Caption = "Name of the corporate body";
		//	set.Tables[1].Columns[1].AllowDBNull = false;

		//	set.Tables[2].ExtendedProperties.Add("Description", "Table joins the people from the city directory with any occupation which was recorded in the directory and also to any corporate body from the directory.");
		//	set.Tables[2].Columns[0].Caption = "Link to a person derived from a city directory";
		//	set.Tables[2].Columns[0].AllowDBNull = false;
		//	set.Tables[2].Columns[1].Caption = "Possible link to a corporate body";
		//	set.Tables[2].Columns[2].Caption = "Historic occupation, as recorded within the city directory";

		//	set.WriteXml(@"C:\GitRepository\SobekCM\SobekCM-Web-Application\SobekCM\temp\gville_epc.xml", XmlWriteMode.WriteSchema);
		//}


		///// <summary> Gets the datatable containging all possible disposition types </summary>
		///// <remarks> This calls the 'Tracking_Get_All_Possible_Disposition_Types' stored procedure. </remarks>
		//public static DataSet EPC_Export
		//{
		//	get
		//	{
		//		DataSet returnSet = EalDbAccess.ExecuteDataset(Database_Type, @"data source=lib-ufdc-cache\UFDCPROD;initial catalog=EPC;integrated security=Yes;", CommandType.StoredProcedure, "Export_DataSet");
		//		return returnSet;
		//	}
		//}

		#endregion

		/// <summary> Gets the last exception caught by a database call through this gateway class  </summary>
		public static Exception Last_Exception
		{
			get { return lastException; }
		}

		/// <summary> Connection string to the main SobekCM databaase </summary>
		/// <remarks> This database hold all the information about items, item aggregationPermissions, statistics, and tracking information</remarks>
		public static string Connection_String
		{
		    set
		    {

		        connectionString = value;

                Engine_Database.Connection_String = value;
		        
		    }
			get	{	return connectionString;	}
		}

		/// <summary> Test connectivity to the database </summary>
		/// <returns> TRUE if connection can be made, otherwise FALSE </returns>
		public static bool Test_Connection()
		{
		    return EalDbAccess.Test(DatabaseType, connectionString);
		}

		/// <summary> Test connectivity to the database </summary>
		/// <returns> TRUE if connection can be made, otherwise FALSE </returns>
		public static bool Test_Connection( string TestConnectionString )
		{
            return EalDbAccess.Test(DatabaseType, TestConnectionString);
		}

		#region Methods relating to usage statistics and item aggregation count statistics

		/// <summary> Pulls the most often hit titles and items, by item aggregation  </summary>
		/// <param name="AggregationCode"> Code for the item aggregation of interest </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> DataSet with the most often hit items and titles and the number of hits </returns>
		/// <remarks> This calls the 'SobekCM_Statistics_Aggregation_Titles' stored procedure <br /><br />
		/// This is used by the <see cref="Statistics_HtmlSubwriter"/> class</remarks>
		public static DataSet Statistics_Aggregation_Titles( string AggregationCode, Custom_Tracer Tracer )
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Statistics_Aggregation_Titles", "Pulling data from database");
			}

			try
			{
				// Execute this query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@code", AggregationCode);
				DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Statistics_Aggregation_Titles", paramList);
				return tempSet;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Statistics_Aggregation_Titles", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Statistics_Aggregation_Titles", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Statistics_Aggregation_Titles", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
		}

		/// <summary> Pulls the complete usage statistics, broken down by each level of the item aggregation hierarchy, between two dates </summary>
		/// <param name="Early_Year">Year portion of the start date</param>
		/// <param name="Early_Month">Month portion of the start date</param>
		/// <param name="Last_Year">Year portion of the last date</param>
		/// <param name="Last_Month">Month portion of the last date</param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> Complete usage statistics, broken down by each level of the item aggregation hierarchy, between the provided dates</returns>
		/// <remarks> This calls the 'SobekCM_Statistics_By_Date_Range' stored procedure <br /><br />
		/// This is used by the <see cref="Statistics_HtmlSubwriter"/> class</remarks>
		public static DataTable Statistics_By_Date_Range(int Early_Year, int Early_Month, int Last_Year, int Last_Month, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Statistics_By_Date_Range", "Pulling data from database");
			}

			try
			{
				// Execute this query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[4];
				paramList[0] = new EalDbParameter("@year1", Early_Year);
				paramList[1] = new EalDbParameter("@month1", Early_Month);
				paramList[2] = new EalDbParameter("@year2", Last_Year);
				paramList[3] = new EalDbParameter("@month2", Last_Month);
				DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Statistics_By_Date_Range", paramList);
				return tempSet.Tables[0];
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Statistics_By_Date_Range", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Statistics_By_Date_Range", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Statistics_By_Date_Range", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
		}



		/// <summary> Returns the month-by-month usage statistics details by item aggregation </summary>
		/// <param name="AggregationCode"> Code for the item aggregation of interest </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> Month-by-month usage statistics for item aggregation of interest </returns>
		/// <remarks> Passing 'ALL' in as the aggregation code returns the statistics for all item aggregationPermissions within this library <br /><br />
		/// This calls the 'SobekCM_Get_Collection_Statistics_History' stored procedure <br /><br />
		/// This is used by the <see cref="Statistics_HtmlSubwriter"/> class</remarks>
		public static DataTable Get_Aggregation_Statistics_History(string AggregationCode, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Get_Collection_Statistics_History", "Pulling history for '" + AggregationCode + "' from database");
			}

			try
			{
				// Execute this query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@code", AggregationCode);
				DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Get_Collection_Statistics_History", paramList);
				return tempSet.Tables[0];
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Get_Aggregation_Statistics_History", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Aggregation_Statistics_History", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Aggregation_Statistics_History", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
		}



		/// <summary> Gets the current title, item, and page count for each item aggregation in the item aggregation hierarchy </summary>
        /// <param name="Option"> Option tells which items to include ( 0 = completed, 1 = entered with files, 2 = all entered items )</param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> Datatable with all the current title, item, and page count for each item aggregation</returns>
		/// <remarks> This calls the 'SobekCM_Item_Count_By_Collection' stored procedure  <br /><br />
		/// This is used by the <see cref="Internal_HtmlSubwriter"/> class</remarks>
		public static DataTable Get_Item_Aggregation_Count(int Option, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Get_Item_Aggregation_Count", "Pulling list from database");
			}

			try
			{
                // Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[1];
                paramList[0] = new EalDbParameter("@option", Option);

				// Execute this query stored procedure
				DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Item_Count_By_Collection", paramList);
				return tempSet.Tables[0];
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Get_Item_Aggregation_Count", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Item_Aggregation_Count", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Item_Aggregation_Count", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
		}

		/// <summary> Gets the title, item, and page count for each item aggregation currently and at some previous point of time </summary>
		/// <param name="Date1"> Date from which to additionally include item count </param>
        /// <param name="Option"> Option tells which items to include ( 0 = completed, 1 = entered with files, 2 = all entered items )</param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> Datatable with all the  title, item, and page count for each item aggregation currently and at some previous point of time </returns>
		/// <remarks> This calls the 'SobekCM_Item_Count_By_Collection_By_Dates' stored procedure  <br /><br />
		/// This is used by the <see cref="Internal_HtmlSubwriter"/> class</remarks>
		public static DataTable Get_Item_Aggregation_Count(DateTime Date1, int Option, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Get_Item_Aggregation_Count", "Pulling from database ( includes fytd starting " + Date1.ToShortDateString() + ")");
			}

			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[3];
				paramList[0] = new EalDbParameter("@date1", Date1);
				paramList[1] = new EalDbParameter("@date2", DBNull.Value);
                paramList[2] = new EalDbParameter("@option", Option);

				// Execute this query stored procedure
				DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Item_Count_By_Collection_By_Date_Range", paramList);
				return tempSet.Tables[0];
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Get_Item_Aggregation_Count", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Item_Aggregation_Count", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Item_Aggregation_Count", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
		}

	    /// <summary> Gets the title, item, and page count for each item aggregation currently and at some previous point of time </summary>
	    /// <param name="Date1"> Date from which to additionally include item count </param>
	    /// <param name="Date2"> Date to which to additionally include item count </param>
	    /// <param name="Option"> Option tells which items to include ( 0 = completed, 1 = entered with files, 2 = all entered items )</param>
	    /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
	    /// <returns> Datatable with all the  title, item, and page count for each item aggregation at some previous point of time and then the increase in these counts between the two provided dates </returns>
	    /// <remarks> This calls the 'SobekCM_Item_Count_By_Collection_By_Date_Range' stored procedure  <br /><br />
	    /// This is used by the <see cref="Internal_HtmlSubwriter"/> class</remarks>
	    public static DataTable Get_Item_Aggregation_Count_DateRange(DateTime Date1, DateTime Date2, int Option, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Get_Item_Aggregation_Count_DateRange", "Pulling from database");
			}

			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[3];
				paramList[0] = new EalDbParameter("@date1", Date1);
				paramList[1] = new EalDbParameter("@date2", Date2);
                paramList[2] = new EalDbParameter("@option", Option);

				// Execute this query stored procedure
				DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Item_Count_By_Collection_By_Date_Range", paramList);
				return tempSet.Tables[0];
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Get_Item_Aggregation_Count_DateRange", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Item_Aggregation_Count_DateRange", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Item_Aggregation_Count_DateRange", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
		}



		/// <summary> Gets the item and page count loaded to this digital library by month and year </summary>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> DataTable of the count of all items and pages loaded to this digital library by month and year </returns>
		/// <remarks> This calls the 'SobekCM_Page_Item_Count_History' stored procedure </remarks>
		public static DataTable Get_Page_Item_Count_History( Custom_Tracer Tracer )
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Get_Page_Item_Count_History", "Pulling from database");
			}

			try
			{
				// Define a temporary dataset
				DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Page_Item_Count_History");

				// If there was no data for this collection and entry point, return null (an ERROR occurred)
				if ((tempSet.Tables.Count == 0) || (tempSet.Tables[0] == null) || (tempSet.Tables[0].Rows.Count == 0))
				{
					return null;
				}

				// Return the first table from the returned dataset
				return tempSet.Tables[0];
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Get_Page_Item_Count_History", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Page_Item_Count_History", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Page_Item_Count_History", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
		}


		#endregion

        #region Method to return DATATABLE of all items from an aggregation

	    /// <summary> Gets the list of unique coordinate points and associated bibid and group title for a single 
	    /// item aggregation </summary>
	    /// <param name="AggregationCode"> Code for the item aggregation </param>
	    /// <param name="FIDs"> FileIDs </param>
	    /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
	    /// <returns> DataTable with all the coordinate values </returns>
	    /// <remarks> This calls the 'SobekCM_Coordinate_Points_By_Aggregation' stored procedure </remarks>
	    public static DataTable Get_All_Items_By_AggregationID(string AggregationCode, List<string> FIDs, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Get_All_Items_By_AggregationID", "Pull the item list");
            }

            string HOOK_FIDDBCallPrefix = "SobekCM_Metadata_Basic_Search_Table"; //this is the correct sql syntax for searching the db table for a specific metadata type
            int nonFIDsParamCount = 2; //how many non fids are there?

            // Build the parameter list
            EalDbParameter[] paramList = new EalDbParameter[(FIDs.Count + nonFIDsParamCount)];
            paramList[0] = new EalDbParameter("@aggregation_code", AggregationCode);
            //paramList[1] = new EalDbParameter("@FID1_PassIn", FIDs[0]);
            //paramList[2] = new EalDbParameter("@FID2_PassIn", FIDs[1]);
            //paramList[3] = new EalDbParameter("@FID3_PassIn", FIDs[2]);
            //paramList[4] = new EalDbParameter("@FID4_PassIn", FIDs[3]);
            //paramList[5] = new EalDbParameter("@FID5_PassIn", FIDs[4]);
            //paramList[6] = new EalDbParameter("@FID6_PassIn", FIDs[5]);
            //paramList[7] = new EalDbParameter("@FID7_PassIn", FIDs[6]);
            //paramList[8] = new EalDbParameter("@FID8_PassIn", FIDs[7]);
            int paramListIndex = 0; //set where we are at
            int fidIndex = 0; //where do the fids start (zero)
            foreach (string fiD in FIDs)
            {
                paramListIndex++;
                fidIndex++;
                paramList[paramListIndex] = new EalDbParameter("@FID" + fidIndex.ToString(), fiD);
            }
            paramList[(paramListIndex + 1)] = new EalDbParameter("FIDDBCallPrefix", HOOK_FIDDBCallPrefix);

            // Define a temporary dataset
            DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Get_All_Items_By_AggregationID", paramList);
            return tempSet == null ? null : tempSet.Tables[0];
        }

        #endregion

        #region Method to return STIRNG of the human readable metadata code

        /// <summary> Gets the human readable name of a metadate id</summary>
        /// <param name="MetadataTypeId"> Code for the metadata</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <returns> String with the name of the metadata </returns>
        /// <remarks> This calls the 'SobekCM_Get_Metadata_Name_From_MetadataTypeID' stored procedure </remarks>
        public static string Get_Metadata_Name_From_MetadataTypeID(short MetadataTypeId, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Get_Metadata_Name_From_MetadataTypeID", "Get the metadataID name");
            }

            // Build the parameter list
            EalDbParameter[] paramList = new EalDbParameter[1];
            paramList[0] = new EalDbParameter("@metadataTypeID", MetadataTypeId);

            // Define a temporary dataset
            DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Get_Metadata_Name_From_MetadataTypeID", paramList);
            DataTable tempResult = tempSet.Tables[0];
            return tempResult.Rows[0][0].ToString();
        }

        #endregion

        #region Methods to get the information about an ITEM or ITEM GROUP

	    /// <summary> Determines what restrictions are present on an item  </summary>
	    /// <param name="BibID"> Bibliographic identifier for the volume to retrieve </param>
	    /// <param name="Vid"> Volume identifier for the volume to retrieve </param>
	    /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
	    /// <param name="IsDark"></param>
        /// <param name="IP_Restrction_Mask"></param>
	    /// <returns> DataSet with detailed information about this item from the database </returns>
	    /// <remarks> This calls the 'SobekCM_Get_Item_Restrictions' stored procedure </remarks> 
	    public static void Get_Item_Restrictions(string BibID, string Vid, Custom_Tracer Tracer, out bool IsDark, out short IP_Restrction_Mask )
		{
			IsDark = true;
			IP_Restrction_Mask = -1;

			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Get_Item_Restrictions", "");
			}

			try
			{
				EalDbParameter[] parameters = new EalDbParameter[2];
				parameters[0] = new EalDbParameter("@BibID", BibID);
				parameters[1] = new EalDbParameter("@VID", Vid);


				// Define a temporary dataset
				DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Get_Item_Restrictions", parameters);

				// Was there an answer?
				if ((tempSet.Tables.Count > 0) && (tempSet.Tables[0].Rows.Count > 0))
				{
					IP_Restrction_Mask = short.Parse(tempSet.Tables[0].Rows[0]["IP_Restriction_Mask"].ToString());
					IsDark = bool.Parse(tempSet.Tables[0].Rows[0]["Dark"].ToString());
				}
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Get_Item_Restrictions", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Item_Restrictions", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Item_Restrictions", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
			}
		}

		/// <summary> Gets some basic information about an item before displaing it, such as the descriptive notes from the database, ability to add notes, etc.. </summary>
		/// <param name="ItemID"> Bibliographic identifier for the volume to retrieve </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> DataSet with detailed information about this item from the database </returns>
		/// <remarks> This calls the 'SobekCM_Get_BibID_VID_From_ItemID' stored procedure </remarks> 
		public static DataRow Lookup_Item_By_ItemID( int ItemID, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Lookup_Item_By_ItemID", "Trying to pull information for " + ItemID );
			}

			try
			{
				EalDbParameter[] parameters = new EalDbParameter[1];
				parameters[0] = new EalDbParameter("@itemid", ItemID);

				// Define a temporary dataset
				DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Get_BibID_VID_From_ItemID", parameters);

				// If there was no data for this collection and entry point, return null (an ERROR occurred)
				if ((tempSet.Tables.Count == 0) || (tempSet.Tables[0] == null) || (tempSet.Tables[0].Rows.Count == 0))
				{
					return null;
				}

				// Return the first table from the returned dataset
				return tempSet.Tables[0].Rows[0];
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Lookup_Item_By_ItemID", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Lookup_Item_By_ItemID", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Lookup_Item_By_ItemID", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
		}


		#endregion

		#region Methods to get the URL portals, edit and save new URL portals, and delete URL portals



		/// <summary> Delete a URL Portal from the database, by primary key </summary>
		/// <param name="PortalID"> Primary key for the URL portal to be deleted </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successul, otherwise FALSE </returns>
		/// <remarks> This calls the 'SobekCM_Delete_Portal' stored procedure </remarks>
		public static bool Delete_URL_Portal( int PortalID, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Delete_URL_Portal", "Delete a URL Portal by portal id ( " + PortalID + " )");
			}

			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@portalid", PortalID);

				// Define a temporary dataset
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Delete_Portal", paramList);

				return true;
			}
			catch (Exception ee)
			{
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Delete_URL_Portal", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_URL_Portal", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_URL_Portal", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Edit an existing URL Portal or add a new URL portal, by primary key </summary>
		/// <param name="PortalID"> Primary key for the URL portal to be edited, or -1 if this is a new URL portal </param>
		/// <param name="DefaultAggregation"> Default aggregation for this URL portal </param>
		/// <param name="DefaultWebSkin"> Default web skin for this URL portal </param>
		/// <param name="BasePurl"> Base PURL , used to override the default PURL built from the current URL</param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <param name="BaseUrl"> URL used to match the incoming request with this URL portal</param>
		/// <param name="IsActive"> Flag indicates if this URL portal is active</param>
		/// <param name="IsDefault"> Flag indicates if this is the default URL portal, if no other portal match is found</param>
		/// <param name="Abbreviation"> Abbreviation for this system, when referenced by this URL portal</param>
		/// <param name="Name"> Name of this system, when referenced by this URL portal </param>
		/// <returns> New primary key (or existing key) for the URL portal added or edited </returns>
		/// <remarks> This calls the 'SobekCM_Edit_Portal' stored procedure </remarks>
		public static int Edit_URL_Portal(int PortalID, string BaseUrl, bool IsActive, bool IsDefault, string Abbreviation, string Name, string DefaultAggregation, string DefaultWebSkin, string BasePurl, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Edit_URL_Portal", "Edit an existing URL portal, or add a new one");
			}

			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[10];
				paramList[0] = new EalDbParameter("@PortalID", PortalID);
				paramList[1] = new EalDbParameter("@Base_URL", BaseUrl);
				paramList[2] = new EalDbParameter("@isActive", IsActive);
				paramList[3] = new EalDbParameter("@isDefault", IsDefault);
				paramList[4] = new EalDbParameter("@Abbreviation", Abbreviation);
				paramList[5] = new EalDbParameter("@Name", Name);
				paramList[6] = new EalDbParameter("@Default_Aggregation", DefaultAggregation);
				paramList[7] = new EalDbParameter("@Base_PURL", BasePurl);
				paramList[8] = new EalDbParameter("@Default_Web_Skin", DefaultWebSkin);
				paramList[9] = new EalDbParameter("@NewID", PortalID) {Direction = ParameterDirection.InputOutput};

				// Define a temporary dataset
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Edit_Portal", paramList);

				return Convert.ToInt32( paramList[9].Value );
			}
			catch (Exception ee)
			{
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Edit_URL_Portal", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Edit_URL_Portal", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Edit_URL_Portal", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return -1;
			}
		}

		#endregion

		#region Methods to get all of the Application-Wide values





		/// <summary> Get the list of groups, with the top item (VID) </summary>
		/// <returns> List of groups, with the top item (VID) </returns>
		public static DataTable Get_All_Groups_First_VID()
		{
            DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Get_All_Groups_First_VID" );

			// If there was no data for this collection and entry point, return null (an ERROR occurred)
			if ((tempSet.Tables.Count == 0) || (tempSet.Tables[0] == null) || (tempSet.Tables[0].Rows.Count == 0))
			{
				return null;
			}

			// Return the first table from the returned dataset
			return tempSet.Tables[0];
		}


		/// <summary> Gets the dataset of all public items and item groups </summary>
		/// <param name="IncludePrivate"> Flag indicates whether to include private items in this list </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> Dataset of all items and item groups </returns>
		/// <remarks> This calls the 'SobekCM_Item_List_Brief2' stored procedure </remarks> 
		public static DataSet Get_Item_List( bool IncludePrivate, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Get_Item_List", String.Empty);
			}

            DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Item_List_Brief2", new List<EalDbParameter> { new EalDbParameter("@include_private", IncludePrivate) });
				
			// If there was no data for this collection and entry point, return null (an ERROR occurred)
			if ((tempSet.Tables.Count == 0) || (tempSet.Tables[0] == null))
			{
				return null;
			}

			// Return the first table from the returned dataset
			return tempSet;
		}



		#endregion

		#region Methods to support the restriction by IP addresses

        /// <summary> Gets the list of all the IP ranges for restriction, including each single IP information in those ranges </summary>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <returns> DataTable with all the data about the IP ranges used for restrictions </returns>
        /// <remarks> This calls the 'SobekCM_Get_All_IP_Restrictions' stored procedure </remarks> 
        public static DataTable Get_IP_Restriction_Ranges(Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("Engine_Database.Get_IP_Restriction_Range", "Pulls all the IP restriction range information");
            }

            try
            {
                DataSet fillSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Get_All_IP_Restrictions");

                // Was there a match?
                if ((fillSet.Tables.Count == 0) || (fillSet.Tables[0].Rows.Count == 0))
                    return null;

                // Return the fill set
                return fillSet.Tables[0];
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Get_IP_Restriction_Ranges", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_IP_Restriction_Ranges", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_IP_Restriction_Ranges", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return null;
            }
        }


        /// <summary> Gets the details for a single IP restriction ranges, including each single IP and the complete notes associated with the range </summary>
        /// <param name="PrimaryID"> Primary id to the IP restriction range to pull details for </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <returns> DataTable with all the data about the IP ranges used for restrictions </returns>
        /// <remarks> This calls the 'SobekCM_Get_IP_Restriction_Range' stored procedure </remarks> 
        public static DataSet Get_IP_Restriction_Range_Details(int PrimaryID, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Get_IP_Restriction_Range_Details", "Pulls all the IP restriction range details for range #" + PrimaryID);
            }

            try
           

            {
                EalDbParameter[] parameters = new EalDbParameter[1];
                parameters[0] = new EalDbParameter("@ip_rangeid", PrimaryID);

                // Define a temporary dataset
                DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Get_IP_Restriction_Range", parameters);

                // If there was no data for this collection and entry point, return null (an ERROR occurred)
                if ((tempSet.Tables.Count == 0) || (tempSet.Tables[0] == null) || (tempSet.Tables[0].Rows.Count == 0))
                {
                    return null;
                }

                // Return the first table from the returned dataset
                return tempSet;
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Get_IP_Restriction_Range_Details", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_IP_Restriction_Range_Details", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_IP_Restriction_Range_Details", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return null;
            }
        }

		/// <summary> Deletes a single IP address information from an IP restriction range </summary>
		/// <param name="PrimaryID"> Primary key for this single IP address information to delete </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'SobekCM_Delete_Single_IP' stored procedure </remarks> 
		public static bool Delete_Single_IP(int PrimaryID, Custom_Tracer Tracer )
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Delete_Single_IP", "Delete single IP information within a range");
			}

			try
			{
				EalDbParameter[] parameters = new EalDbParameter[1];
				parameters[0] = new EalDbParameter("@ip_singleid", PrimaryID);

				// Define a temporary dataset
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Delete_Single_IP", parameters);

				// Return the first table from the returned dataset
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Delete_Single_IP", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Single_IP", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Single_IP", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Adds or edits a single IP address information in an IP restriction range </summary>
		/// <param name="PrimaryID"> Primary key for this single IP address information to add, or -1 to add a new IP address </param>
		/// <param name="IpRangeID"> Primary key for the IP restriction range to add this single IP address information </param>
		/// <param name="StartIp"> Beginning of the IP range, or the complete IP address </param>
		/// <param name="EndIp"> End of the IP range, if this was a true range </param>
		/// <param name="Note"> Any note associated with this single IP information </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> Primary key for the single IP address information, if no primary key was originally provided </returns>
		/// <remarks> This calls the 'SobekCM_Edit_Single_IP' stored procedure </remarks> 
		public static int Edit_Single_IP(int PrimaryID, int IpRangeID, string StartIp, string EndIp, string Note, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Edit_Single_IP", "Edit a single IP within a restriction range");
			}

			try
			{
				EalDbParameter[] parameters = new EalDbParameter[6];
				parameters[0] = new EalDbParameter("@ip_singleid", PrimaryID);
				parameters[1] = new EalDbParameter("@ip_rangeid", IpRangeID );
				parameters[2] = new EalDbParameter("@startip", StartIp);
				parameters[3] = new EalDbParameter("@endip", EndIp);
				parameters[4] = new EalDbParameter("@notes", Note );
				parameters[5] = new EalDbParameter("@new_ip_singleid", -1) {Direction = ParameterDirection.InputOutput};

				// Define a temporary dataset
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Edit_Single_IP", parameters);

				// Return the first table from the returned dataset
				return Convert.ToInt32(parameters[5].Value);
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Edit_Single_IP", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Edit_Single_IP", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Edit_Single_IP", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return -1;
			}
		}


		/// <summary> Edits an existing IP restriction range, or adds a new one </summary>
		/// <param name="IpRangeID"> Primary key for the IP restriction range  </param>
		/// <param name="Title"> Title for this IP Restriction Range </param>
		/// <param name="Notes"> Notes about this IP Restriction Range (for system admins)</param>
		/// <param name="ItemRestrictedStatement"> Statement used when a user directly requests an item for which they do not the pre-requisite access </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'SobekCM_Edit_IP_Range' stored procedure </remarks> 
		public static bool Edit_IP_Range(int IpRangeID, string Title, string Notes, string ItemRestrictedStatement, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Edit_IP_Range", "Edit an existing IP restriction range");
			}

			try
			{
				EalDbParameter[] parameters = new EalDbParameter[4];
				parameters[0] = new EalDbParameter("@rangeid", IpRangeID);
				parameters[1] = new EalDbParameter("@title", Title);
				parameters[2] = new EalDbParameter("@notes", Notes);
				parameters[3] = new EalDbParameter("@not_valid_statement", ItemRestrictedStatement);

				// Execute the stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Edit_IP_Range", parameters);

				// Return true if successful
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Edit_IP_Range", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Edit_IP_Range", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Edit_IP_Range", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

        /// <summary> Delete a complete IP range group </summary>
        /// <param name="IdToDelete"> Primary key of the IP range to delete </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        /// <remarks> This calls the 'SobekCM_Delete_IP_Range' stored procedure </remarks> 
        public static bool Delete_IP_Range(int IdToDelete, Custom_Tracer Tracer)
	    {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Delete_IP_Range", "Delete an existing IP restriction range");
            }

            try
            {
                EalDbParameter[] parameters = new EalDbParameter[1];
                parameters[0] = new EalDbParameter("@rangeid", IdToDelete);

                // Execute the stored procedure
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Delete_IP_Range", parameters);

                // Return true if successful
                return true;
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Delete_IP_Range", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Delete_IP_Range", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Delete_IP_Range", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return false;
            }
	    }

		#endregion

		#region Methods to get authority type information

        ///// <summary> Gets the list of all map features linked to a particular item  </summary>
        ///// <param name="ItemID"> ItemID for the item of interest</param>
        ///// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        ///// <returns> List of all features linked to the item of interest </returns>
        ///// <remarks> This calls the 'Auth_Get_All_Features_By_Item' stored procedure </remarks> 
        //public static Map_Features_DataSet Get_All_Features_By_Item(int ItemID, Custom_Tracer Tracer)
        //{
        //    try
        //    {
        //        // Create the connection
        //        SqlConnection connect = new SqlConnection( connectionString );

        //        // Create the command 
        //        SqlCommand executeCommand = new SqlCommand("Auth_Get_All_Features_By_Item", connect)
        //                                        {CommandType = CommandType.StoredProcedure};
        //        executeCommand.Parameters.AddWithValue( "@itemid", ItemID );
        //        executeCommand.Parameters.AddWithValue( "@filter", 1 );

        //        // Create the adapter
        //        SqlDataAdapter adapter = new SqlDataAdapter( executeCommand );

        //        // Add appropriate table mappings
        //        adapter.TableMappings.Add("Table", "Features");
        //        adapter.TableMappings.Add("Table1", "Types");

        //        // Fill the strongly typed dataset
        //        Map_Features_DataSet features = new Map_Features_DataSet();
        //        adapter.Fill( features );

        //        // Return the fully built object
        //        return features;
        //    }
        //    catch (Exception ee)
        //    {
        //        lastException = ee;
        //        if (Tracer != null)
        //        {
        //            Tracer.Add_Trace("SobekCM_Database.Get_All_Features_By_Item", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
        //            Tracer.Add_Trace("SobekCM_Database.Get_All_Features_By_Item", ee.Message, Custom_Trace_Type_Enum.Error);
        //            Tracer.Add_Trace("SobekCM_Database.Get_All_Features_By_Item", ee.StackTrace, Custom_Trace_Type_Enum.Error);
        //        }
        //        return null;
        //    }
        //}

        ///// <summary> Gets the list of all streets linked to a particular item  </summary>
        ///// <param name="ItemID"> ItemID for the item of interest</param>
        ///// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        ///// <returns> List of all streets linked to the item of interest </returns>
        ///// <remarks> This calls the 'Auth_Get_All_Streets_By_Item' stored procedure </remarks> 
        //public static Map_Streets_DataSet Get_All_Streets_By_Item(int ItemID, Custom_Tracer Tracer)
        //{
        //    try
        //    {
        //        // Create the connection
        //        SqlConnection connect = new SqlConnection( connectionString );

        //        // Create the command 
        //        SqlCommand executeCommand = new SqlCommand("Auth_Get_All_Streets_By_Item", connect)
        //                                        {CommandType = CommandType.StoredProcedure};
        //        executeCommand.Parameters.AddWithValue( "@itemid", ItemID );

        //        // Create the adapter
        //        SqlDataAdapter adapter = new SqlDataAdapter( executeCommand );

        //        // Add appropriate table mappings
        //        adapter.TableMappings.Add("Table", "Streets");


        //        // Fill the strongly typed dataset
        //        Map_Streets_DataSet streets = new Map_Streets_DataSet();
        //        adapter.Fill( streets );

        //        // Return the fully built object
        //        return streets;
        //    }
        //    catch ( Exception ee )
        //    {
        //        lastException = ee;
        //        if (Tracer != null)
        //        {
        //            Tracer.Add_Trace("SobekCM_Database.Get_All_Streets_By_Item", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
        //            Tracer.Add_Trace("SobekCM_Database.Get_All_Streets_By_Item", ee.Message, Custom_Trace_Type_Enum.Error);
        //            Tracer.Add_Trace("SobekCM_Database.Get_All_Streets_By_Item", ee.StackTrace, Custom_Trace_Type_Enum.Error);
        //        }
        //        return null;
        //    }
        //}


		#endregion

		#region My Sobek database calls

		/// <summary> Saves information about a single user, including user settings </summary>
		/// <param name="User"> <see cref="SobekCM.Core.Users.User_Object"/> with all the information about the single user</param>
		/// <param name="Password"> Plain-text password, which is then encrypted prior to saving</param>
        /// <param name="AuthenticationType"> String which indicates the type of authentication utilized, only important if this is the first time this user has authenticated/registered </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE</returns>
		/// <remarks> This calls the 'mySobek_Save_User' stored procedure</remarks> 
		public static bool Save_User(User_Object User, string Password, User_Authentication_Type_Enum AuthenticationType, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Save_User", String.Empty);
			}

            // Save the basic information about this user
            bool result = save_user_basic_information(User, Password, AuthenticationType, Tracer);
            if (!result) return false;

            // Clear existing user settings
            result = clear_user_settings(User.UserID, Tracer);

            // Save all user settings
            bool userSettingSaveSuccess = true;
            if ( User.SettingsCount > 0 )
            {
                foreach( string key in User.SettingsKeys)
                {
                    string value = User.Get_Setting(key).ToString();
                    if (!save_user_settings(User.UserID, key, value, Tracer))
                        userSettingSaveSuccess = false;
                }
            }



            return true;
		}

        /// <summary> Saves basic information information about an existing single user </summary>
        /// <param name="User"> <see cref="SobekCM.Core.Users.User_Object"/> with all the information about the single user</param>
        /// <param name="Password"> Plain-text password, which is then encrypted prior to saving</param>
        /// <param name="AuthenticationType"> String which indicates the type of authentication utilized, only important if this is the first time this user has authenticated/registered </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <returns> TRUE if successful, otherwise FALSE</returns>
        /// <remarks> This calls the 'mySobek_Save_User' stored procedure</remarks> 
        private static bool save_user_basic_information(User_Object User, string Password, User_Authentication_Type_Enum AuthenticationType, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.save_user_basic_information", String.Empty);
            }

            const string SALT = "This is my salt to add to the password";
            string encryptedPassword = SecurityInfo.SHA1_EncryptString(Password + SALT);

            string auth_string = String.Empty;
            if (AuthenticationType == User_Authentication_Type_Enum.Sobek)
                auth_string = "sobek";
            if (AuthenticationType == User_Authentication_Type_Enum.Shibboleth)
                auth_string = "shibboleth";
            if ((AuthenticationType == User_Authentication_Type_Enum.Windows) || (AuthenticationType == User_Authentication_Type_Enum.LDAP))
                auth_string = "ldap";

            try
            {
                // Execute this non-query stored procedure
                EalDbParameter[] paramList = new EalDbParameter[24];
                paramList[0] = new EalDbParameter("@userid", User.UserID);
                paramList[1] = new EalDbParameter("@shibbid", User.ShibbID);
                paramList[2] = new EalDbParameter("@username", User.UserName);
                paramList[3] = new EalDbParameter("@password", encryptedPassword);
                paramList[4] = new EalDbParameter("@emailaddress", User.Email);
                paramList[5] = new EalDbParameter("@firstname", User.Given_Name);
                paramList[6] = new EalDbParameter("@lastname", User.Family_Name);
                paramList[7] = new EalDbParameter("@cansubmititems", User.Can_Submit);
                paramList[8] = new EalDbParameter("@nickname", User.Nickname);
                paramList[9] = new EalDbParameter("@organization", User.Organization);
                paramList[10] = new EalDbParameter("@college", User.College);
                paramList[11] = new EalDbParameter("@department", User.Department);
                paramList[12] = new EalDbParameter("@unit", User.Unit);
                paramList[13] = new EalDbParameter("@rights", User.Default_Rights);
                paramList[14] = new EalDbParameter("@sendemail", User.Send_Email_On_Submission);
                paramList[15] = new EalDbParameter("@language", User.Preferred_Language);
                if (User.Templates.Count > 0)
                {
                    paramList[16] = new EalDbParameter("@default_template", User.Templates[0]);
                }
                else
                {
                    paramList[16] = new EalDbParameter("@default_template", String.Empty);
                }
                if (User.Default_Metadata_Sets.Count > 0)
                {
                    paramList[17] = new EalDbParameter("@default_metadata", User.Default_Metadata_Sets[0]);
                }
                else
                {
                    paramList[17] = new EalDbParameter("@default_metadata", String.Empty);
                }
                paramList[18] = new EalDbParameter("@organization_code", User.Organization_Code);
                paramList[19] = new EalDbParameter("@receivestatsemail", User.Receive_Stats_Emails);
                paramList[20] = new EalDbParameter("@scanningtechnician", User.Scanning_Technician);
                paramList[21] = new EalDbParameter("@processingtechnician", User.Processing_Technician);
                paramList[22] = new EalDbParameter("@internalnotes", User.Internal_Notes);
                paramList[23] = new EalDbParameter("@authentication", auth_string);

                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Save_User", paramList);
                return true;
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Save_User", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Save_User", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Save_User", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return false;
            }
        }

        /// <summary> Clears the user settings associated with an existing single user </summary>
        /// <param name="userid"> Primary key fpr the user </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <returns> TRUE if successful, otherwise FALSE</returns>
        /// <remarks> This calls the 'mySobek_Save_User' stored procedure</remarks> 
        private static bool clear_user_settings(int userid, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.clear_user_settings", String.Empty);
            }

            try
            {
                // Execute this non-query stored procedure
                EalDbParameter[] paramList = new EalDbParameter[1];
                paramList[0] = new EalDbParameter("@userid", userid);
 
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Clear_User_Settings", paramList);
                return true;
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Save_User", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Save_User", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Save_User", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return false;
            }
        }

        /// <summary> Saves a single user setting for an existing single user </summary>
        /// <param name="userid"> Primary key for the user </param>
        /// <param name="setting_key"> Key for this user setting </param>
        /// <param name="setting_value"> Value (as a string) for this user setting </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <returns> TRUE if successful, otherwise FALSE</returns>
        /// <remarks> This calls the 'mySobek_Save_User' stored procedure</remarks> 
        private static bool save_user_settings(int userid, string setting_key, string setting_value, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.save_user_settings", "Saving setting for " + setting_key);
            }

            try
            {
                // Execute this non-query stored procedure
                EalDbParameter[] paramList = new EalDbParameter[3];
                paramList[0] = new EalDbParameter("@userid", userid);
                paramList[1] = new EalDbParameter("@setting_key", setting_key);
                paramList[2] = new EalDbParameter("@setting_value", setting_value);

                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_User_Setting", paramList);
                return true;
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Save_User", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Save_User", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Save_User", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return false;
            }
        }


        /// <summary> Links a single user to a user group  </summary>
        /// <param name="UserID"> Primary key for the user </param>
        /// <param name="UserGroupID"> Primary key for the user group </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        /// <remarks> This calls the 'mySobek_Link_User_To_User_Group' stored procedure</remarks> 
        public static bool Link_User_To_User_Group( int UserID, int UserGroupID )
		{
			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[2];
				paramList[0] = new EalDbParameter("@userid", UserID);
				paramList[1] = new EalDbParameter("@usergroupid", UserGroupID);

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Link_User_To_User_Group", paramList);
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				return false;
			}
		}

		/// <summary> Change an existing user's password </summary>
		/// <param name="Username"> Username for the user </param>
		/// <param name="CurrentPassword"> Old plain-text password, which is then encrypted prior to saving</param>
		/// <param name="NewPassword"> New plain-text password, which is then encrypted prior to saving</param>
		/// <param name="IsTemporary"> Flag indicates if the new password is temporary and must be changed on the next logon</param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE</returns>
		/// <remarks> This calls the 'mySobek_Change_Password' stored procedure</remarks> 
		public static bool Change_Password(string Username, string CurrentPassword, string NewPassword, bool IsTemporary,  Custom_Tracer Tracer )
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Change_Password", String.Empty);
			}

			const string SALT = "This is my salt to add to the password";
			string encryptedCurrentPassword = SecurityInfo.SHA1_EncryptString(CurrentPassword + SALT);
			string encryptedNewPassword = SecurityInfo.SHA1_EncryptString(NewPassword + SALT);
			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[5];
				paramList[0] = new EalDbParameter("@username", Username);
				paramList[1] = new EalDbParameter("@current_password", encryptedCurrentPassword);
				paramList[2] = new EalDbParameter("@new_password", encryptedNewPassword);
				paramList[3] = new EalDbParameter("@isTemporaryPassword", IsTemporary);
				paramList[4] = new EalDbParameter("@password_changed", false) {Direction = ParameterDirection.InputOutput};

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Change_Password", paramList);


				return Convert.ToBoolean(paramList[4].Value);
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Change_Password", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Change_Password", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Change_Password", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}

		}

		/// <summary> Checks to see if a username or email exist </summary>
		/// <param name="UserName"> Username to check</param>
		/// <param name="Email"> Email address to check</param>
		/// <param name="UserNameExists"> [OUT] Flag indicates if the username exists</param>
		/// <param name="EmailExists"> [OUT] Flag indicates if the email exists </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE</returns>
		/// <remarks> This calls the 'mySobek_UserName_Exists' stored procedure<br /><br />
		/// This is used to enforce uniqueness during registration </remarks> 
		public static bool UserName_Exists(string UserName, string Email, out bool UserNameExists, out bool EmailExists, Custom_Tracer Tracer )
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.UserName_Exists", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[4];
				paramList[0] = new EalDbParameter("@username", UserName);
				paramList[1] = new EalDbParameter("@email", Email);
				paramList[2] = new EalDbParameter("@UserName_Exists", true) {Direction = ParameterDirection.InputOutput};
				paramList[3] = new EalDbParameter("@Email_Exists", true) {Direction = ParameterDirection.InputOutput};

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_UserName_Exists", paramList);

				UserNameExists = Convert.ToBoolean(paramList[2].Value);
				EmailExists = Convert.ToBoolean(paramList[3].Value);
				return true;
			}
			catch ( Exception ee )
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.UserName_Exists", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.UserName_Exists", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.UserName_Exists", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				UserNameExists = true;
				EmailExists = true;
				return false;
			}
		}

		/// <summary> Updates the flag that indicates the user would like to receive a monthly usage statistics email </summary>
		/// <param name="UserID"> Primary key for this user in the database </param>
		/// <param name="NewFlag"> New value for the flag </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE</returns>
		/// <remarks> This calls the 'mySobek_Set_Receive_Stats_Email_Flag' stored procedure</remarks> 
		public static bool Set_User_Receive_Stats_Email( int UserID, bool NewFlag, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Set_Receive_Stats_Email_Flag", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[2];
				paramList[0] = new EalDbParameter("@userid", UserID);
				paramList[1] = new EalDbParameter("@newflag", NewFlag);

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Set_Receive_Stats_Email_Flag", paramList);
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Set_Receive_Stats_Email_Flag", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Set_Receive_Stats_Email_Flag", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Set_Receive_Stats_Email_Flag", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}



		/// <summary> Add a link between a user and an existing item group (by GroupID) </summary>
		/// <param name="UserID"> Primary key for this user in the database </param>
		/// <param name="GroupID"> Primary key for the item group to link this user to</param>
		/// <returns>TRUE if successful, otherwise FALSE</returns>
		/// <remarks> This calls the 'mySobek_Link_User_To_Item' stored procedure</remarks> 
		public static bool Add_User_BibID_Link(int UserID, int GroupID)
		{
			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[2];
				paramList[0] = new EalDbParameter("@userid", UserID);
				paramList[1] = new EalDbParameter("@groupid", GroupID);

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Link_User_To_Item", paramList);

				// Return the browse id
				return true;

			}
			catch (Exception ee)
			{
				lastException = ee;
				return false;
			}
		}

		/// <summary> Add a link between a user and an existing item and include the type of relationship </summary>
		/// <param name="UserID"> Primary key for this user in the database </param>
		/// <param name="ItemID"> Primary key for the item to link this user to</param>
		/// <param name="RelationshipID"> Primary key for the type of relationship to use </param>
		/// <param name="ChangeExisting"> If a relationship already exists, should this override it? </param>
		/// <returns>TRUE if successful, otherwise FALSE</returns>
		/// <remarks> This calls the 'SobekCM_Link_User_To_Item' stored procedure</remarks> 
		public static bool Add_User_Item_Link(int UserID, int ItemID, int RelationshipID, bool ChangeExisting )
		{
			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[4];
				paramList[0] = new EalDbParameter("@itemid", ItemID);
				paramList[1] = new EalDbParameter("@userid", UserID);
				paramList[2] = new EalDbParameter("@relationshipid", RelationshipID);
				paramList[3] = new EalDbParameter("@change_existing", ChangeExisting);

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Link_User_To_Item", paramList);

				// Return the browse id
				return true;

			}
			catch (Exception ee)
			{
				lastException = ee;
				return false;
			}
		}
		
		/// <summary> Gets basic information about all the folders and searches saved for a single user </summary>
		/// <param name="UserID"> Primary key for this user in the database </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> Information about all folders (and number of items) and saved searches for a user </returns>
		/// <remarks> This calls the 'mySobek_Get_Folder_Search_Information' stored procedure</remarks> 
		public static DataSet Get_Folder_Search_Information(int UserID, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Get_Folder_Search_Information", String.Empty);
			}

			try
			{

				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@userid", UserID);

				DataSet resultSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Get_Folder_Search_Information", paramList);

				return resultSet;

			}
			catch ( Exception ee )
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Get_Folder_Search_Information", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Folder_Search_Information", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Folder_Search_Information", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
		}

		/// <summary> Deletes a user search from the collection of saved searches </summary>
		/// <param name="UserSearchID"> Primary key for this saved search </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'mySobek_Delete_User_Search' stored procedure</remarks> 
		public static bool Delete_User_Search(int UserSearchID, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Delete_User_Search", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@usersearchid", UserSearchID);

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Delete_User_Search", paramList);

				// Return TRUE
				return true;

			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Delete_User_Search", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_User_Search", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_User_Search", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Gets the list of all saved user searches and any user comments </summary>
		/// <param name="UserID"> Primary key for this user in the database </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> Table of all the saved searches for this user </returns>
		/// <remarks> This calls the 'mySobek_Get_User_Searches' stored procedure</remarks> 
		public static DataTable Get_User_Searches(int UserID, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Get_User_Searches", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@userid", UserID);

				DataSet resultSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Get_User_Searches", paramList);

				return resultSet.Tables[0];

			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Get_User_Searches", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_User_Searches", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_User_Searches", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
		}

		/// <summary> Saves a search to the user's saved searches </summary>
		/// <param name="UserID"> Primary key for this user in the database </param>
		/// <param name="SearchUrl"> SobekCM search URL </param>
		/// <param name="SearchDescription"> Programmatic description of this search</param>
		/// <param name="ItemOrder"> Order for this search within the folder</param>
		/// <param name="UserNotes"> Notes from the user about this search </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> New UserSearchID, or -1 if this edits an existing one </returns>
		/// <remarks> This calls the 'mySobek_Save_User_Search' stored procedure</remarks> 
		public static int Save_User_Search(int UserID, string SearchUrl, string SearchDescription, int ItemOrder, string UserNotes, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Save_User_Search", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[6];
				paramList[0] = new EalDbParameter("@userid", UserID);
				paramList[1] = new EalDbParameter("@searchurl", SearchUrl);
				paramList[2] = new EalDbParameter("@searchdescription", SearchDescription);
				paramList[3] = new EalDbParameter("@itemorder", ItemOrder);
				paramList[4] = new EalDbParameter("@usernotes", UserNotes);
				paramList[5] = new EalDbParameter("@new_usersearchid", -1) {Direction = ParameterDirection.InputOutput};

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Save_User_Search", paramList);


				// Return TRUE
				return Convert.ToInt32(paramList[5].Value);

			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Save_User_Search", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Save_User_Search", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Save_User_Search", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return -1000;
			}
		}

		/// <summary> Remove an item from the user's folder </summary>
		/// <param name="UserID"> Primary key for this user in the database </param>
		/// <param name="FolderName"> Name of this user's folder </param>
		/// <param name="BibID"> Bibliographic identifier for this title / item group </param>
		/// <param name="Vid"> Volume identifier for this one volume within a title / item group </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'mySobek_Delete_Item_From_User_Folder' stored procedure</remarks> 
		public static bool Delete_Item_From_User_Folder(int UserID, string FolderName, string BibID, string Vid, Custom_Tracer Tracer )
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Delete_Item_From_User_Folder", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[4];
				paramList[0] = new EalDbParameter("@userid", UserID);
				paramList[1] = new EalDbParameter("@foldername", FolderName);
				paramList[2] = new EalDbParameter("@bibid", BibID);
				paramList[3] = new EalDbParameter("@vid", Vid);

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Delete_Item_From_User_Folder", paramList);

				// Return TRUE
				return true;

			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Delete_Item_From_User_Folder", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Item_From_User_Folder", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Item_From_User_Folder", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Remove an item from any user folder it currently resides in (besides Submitted Items)</summary>
		/// <param name="UserID"> Primary key for this user in the database </param>
		/// <param name="BibID"> Bibliographic identifier for this title / item group </param>
		/// <param name="Vid"> Volume identifier for this one volume within a title / item group </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'mySobek_Delete_Item_From_All_User_Folders' stored procedure</remarks> 
		public static bool Delete_Item_From_User_Folders(int UserID, string BibID, string Vid, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Delete_Item_From_User_Folder", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[3];
				paramList[0] = new EalDbParameter("@userid", UserID);
				paramList[1] = new EalDbParameter("@bibid", BibID);
				paramList[2] = new EalDbParameter("@vid", Vid);

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Delete_Item_From_All_User_Folders", paramList);

				// Return TRUE
				return true;

			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Delete_Item_From_User_Folders", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Item_From_User_Folders", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Item_From_User_Folders", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Adds a digital resource to a user folder, or edits an existing item </summary>
		/// <param name="UserID"> Primary key for this user in the database </param>
		/// <param name="FolderName"> Name of this user's folder </param>
		/// <param name="BibID"> Bibliographic identifier for this title / item group </param>
		/// <param name="Vid"> Volume identifier for this one volume within a title / item group </param>
		/// <param name="ItemOrder"> Order for this item within the folder</param>
		/// <param name="UserNotes"> Notes from the user about this item </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'mySobek_Add_Item_To_User_Folder' stored procedure</remarks> 
		public static bool Add_Item_To_User_Folder(int UserID, string FolderName, string BibID, string Vid, int ItemOrder, string UserNotes, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Add_Item_To_User_Folder", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[6];
				paramList[0] = new EalDbParameter("@userid", UserID);
				paramList[1] = new EalDbParameter("@foldername", FolderName);
				paramList[2] = new EalDbParameter("@bibid", BibID);
				paramList[3] = new EalDbParameter("@vid", Vid);
				paramList[4] = new EalDbParameter("@itemorder", ItemOrder);
				paramList[5] = new EalDbParameter("@usernotes", UserNotes);

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_Item_To_User_Folder", paramList);

				// Return TRUE
				return true;

			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Add_Item_To_User_Folder", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Add_Item_To_User_Folder", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Add_Item_To_User_Folder", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}



		/// <summary> Deletes a folder from a user </summary>
		/// <param name="UserID"> Primary key for this user from the database</param>
		/// <param name="UserFolderID"> Primary key for this folder from the database</param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'mySobek_Delete_User_Folder' stored procedure</remarks> 
		public static bool Delete_User_Folder(int UserID, int UserFolderID, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Delete_User_Folder", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[2];
				paramList[0] = new EalDbParameter("@userfolderid", UserFolderID);
				paramList[1] = new EalDbParameter("@userid", UserID);

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Delete_User_Folder", paramList);

				// Return TRUE
				return true;

			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Delete_User_Folder", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_User_Folder", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_User_Folder", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Edit an existing user folder, or add a new user folder </summary>
		/// <param name="UserFolderID"> Primary key for the folder, if this is an edit, otherwise -1</param>
		/// <param name="UserID"> Primary key for this user from the database</param>
		/// <param name="ParentFolderID"> Key for the parent folder for this new folder</param>
		/// <param name="FolderName"> Name for this new folder</param>
		/// <param name="IsPublic"> Flag indicates if this folder is public </param>
		/// <param name="Description"> Description for this folder </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> Primary key for this new folder, or -1 if an error occurred </returns>
		/// <remarks> This calls the 'mySobek_Edit_User_Folder' stored procedure</remarks> 
		public static int Edit_User_Folder(int UserFolderID, int UserID, int ParentFolderID, string FolderName, bool IsPublic, string Description, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Edit_User_Folder", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[7];
				paramList[0] = new EalDbParameter("@userfolderid", UserFolderID);
				paramList[1] = new EalDbParameter("@userid", UserID);
				paramList[2] = new EalDbParameter("@parentfolderid", ParentFolderID);
				paramList[3] = new EalDbParameter("@foldername", FolderName);
				paramList[4] = new EalDbParameter("@is_public", IsPublic);
				paramList[5] = new EalDbParameter("@description", Description);
				paramList[6] = new EalDbParameter("@new_folder_id", 0) {Direction = ParameterDirection.InputOutput};

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Edit_User_Folder", paramList);

				// Return TRUE
				return Convert.ToInt32(paramList[6].Value);

			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Edit_User_Folder", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Edit_User_Folder", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Edit_User_Folder", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return -1;
			}
		}

		/// <summary> Sets the flag indicating an aggregation should appear on a user's home page </summary>
		/// <param name="UserID"> Primary key for the user</param>
		/// <param name="AggregationID"> Primary key for the aggregation </param>
		/// <param name="NewFlag"> New flag indicates if this should be on the home page </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'mySobek_Set_Aggregation_Home_Page_Flag' stored procedure</remarks> 
		public static bool User_Set_Aggregation_Home_Page_Flag(int UserID, int AggregationID, bool NewFlag, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.User_Set_Aggregation_Home_Page_Flag", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[3];
				paramList[0] = new EalDbParameter("@userid", UserID);
				paramList[1] = new EalDbParameter("@aggregationid", AggregationID);
				paramList[2] = new EalDbParameter("@onhomepage", NewFlag);

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Set_Aggregation_Home_Page_Flag", paramList);

				// Return TRUE
				return true;

			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.User_Set_Aggregation_Home_Page_Flag", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.User_Set_Aggregation_Home_Page_Flag", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.User_Set_Aggregation_Home_Page_Flag", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Gets the information about a folder which should be public </summary>
		/// <param name="UserFolderID"> ID for the user folder to retrieve </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> Built public user folder regardless if it is public or not.  A non-public folder will only be populated with FALSE for the isPublic field. </returns>
		/// <remarks> This calls the 'mySobek_Get_Folder_Information' stored procedure</remarks> 
		public static Public_User_Folder Get_Public_User_Folder(int UserFolderID, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Get_Public_User_Folder", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@folderid", UserFolderID);

				DataSet resultSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Get_Folder_Information", paramList);

				// Build the returnvalue
				if ((resultSet == null) || (resultSet.Tables.Count == 0) || (resultSet.Tables[0].Rows.Count == 0))
					return new Public_User_Folder(false);

				// Check that it is really public
				bool isPublic = Convert.ToBoolean(resultSet.Tables[0].Rows[0]["isPublic"]);
				if ( !isPublic )
					return new Public_User_Folder(false);

				// Pull out the row and all the values
				DataRow thisRow = resultSet.Tables[0].Rows[0];
				string folderName = thisRow["FolderName"].ToString();
				string folderDescription = thisRow["FolderDescription"].ToString();
				int userID = Convert.ToInt32(thisRow["UserID"]);
				string firstName = thisRow["FirstName"].ToString();
				string lastName = thisRow["LastName"].ToString();
				string nickname = thisRow["NickName"].ToString();
				string email = thisRow["EmailAddress"].ToString();               

				// Return the folder object
				Public_User_Folder returnValue = new Public_User_Folder(UserFolderID, folderName, folderDescription, userID, firstName, lastName, nickname, email, true);
				return returnValue;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Get_Public_User_Folder", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Public_User_Folder", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_Public_User_Folder", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
		}


		/// <summary> Gets the information about a single user group </summary>
		/// <param name="UserGroupID"> Primary key for this user group from the database </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> Fully built <see cref="SobekCM.Core.Users.User_Group"/> object </returns>
		/// <remarks> This calls the 'mySobek_Get_User_Group' stored procedure </remarks> 
		public static User_Group Get_User_Group(int UserGroupID, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Get_User_Group", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@usergroupid", UserGroupID);

				DataSet resultSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Get_User_Group", paramList);

				if ((resultSet.Tables.Count > 0) && (resultSet.Tables[0].Rows.Count > 0))
				{


					DataRow userRow = resultSet.Tables[0].Rows[0];
					string name = userRow["GroupName"].ToString();
					string description = userRow["GroupDescription"].ToString();
					int usergroupid = Convert.ToInt32(userRow["UserGroupID"]);
					User_Group group = new User_Group(name, description, usergroupid);
					group.CanSubmit = Convert.ToBoolean(userRow["Can_Submit_Items"]);
					group.IsInternalUser = Convert.ToBoolean(userRow["Internal_User"]);
					group.IsSystemAdmin = Convert.ToBoolean(userRow["IsSystemAdmin"]);

					foreach (DataRow thisRow in resultSet.Tables[1].Rows)
					{
						group.Add_Template(thisRow["TemplateCode"].ToString());
					}

					foreach (DataRow thisRow in resultSet.Tables[2].Rows)
					{
						group.Add_Default_Metadata_Set(thisRow["MetadataCode"].ToString());
					}

					// Add links to regular expressions
					foreach (DataRow thisRow in resultSet.Tables[3].Rows)
					{
						group.Add_Editable_Regular_Expression(thisRow["EditableRegex"].ToString());
					}

					// Add links to aggregationPermissions
					foreach (DataRow thisRow in resultSet.Tables[4].Rows)
					{
						group.Add_Aggregation(thisRow["Code"].ToString(), thisRow["Name"].ToString(), Convert.ToBoolean(thisRow["CanSelect"]), Convert.ToBoolean(thisRow["CanEditMetadata"]), Convert.ToBoolean(thisRow["CanEditBehaviors"]), Convert.ToBoolean(thisRow["CanPerformQc"]), Convert.ToBoolean(thisRow["CanUploadFiles"]), Convert.ToBoolean(thisRow["CanChangeVisibility"]), Convert.ToBoolean(thisRow["CanDelete"]), Convert.ToBoolean(thisRow["IsCurator"]), Convert.ToBoolean(thisRow["IsAdmin"]));
					}

					// Add the basic information about users in this user group
					foreach (DataRow thisRow in resultSet.Tables[5].Rows)
					{
						int userid = Convert.ToInt32(thisRow["UserID"]);
						string username = thisRow["UserName"].ToString();
						string email = thisRow["EmailAddress"].ToString();
						string firstname = thisRow["FirstName"].ToString();
						string nickname = thisRow["NickName"].ToString();
						string lastname = thisRow["LastName"].ToString();
						string fullname = firstname + " " + lastname;
						if (nickname.Length > 0)
						{
							fullname = nickname + " " + lastname;
						}

						group.Add_User(username, fullname, email, userid);
					}

					return group;
				}

				// Return NULL if there was an error
				return null;

			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Get_User_Group", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_User_Group", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Get_User_Group", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
		}


 

		#endregion

		#region Methods used in support of semi-public descriptive tagging

		/// <summary> Adds a descriptive tag to an existing item by a logged-in user </summary>
		/// <param name="UserID"> Primary key for the user adding the descriptive tag </param>
		/// <param name="TagID"> Primary key for a descriptive tag, if this is an edit </param>
		/// <param name="ItemID"> Primary key for the digital resource to tag </param>
		/// <param name="AddedDescription"> User-entered descriptive tag </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> New tag id if this is a new descriptive tag </returns>
		/// <remarks> This calls the 'mySobek_Add_Description_Tag' stored procedure</remarks> 
		public static int Add_Description_Tag(int UserID, int TagID, int ItemID, string AddedDescription, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Add_Description_Tag", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[5];
				paramList[0] = new EalDbParameter("@UserID", UserID);
				paramList[1] = new EalDbParameter("@TagID", TagID);
				paramList[2] = new EalDbParameter("@ItemID", ItemID);
				paramList[3] = new EalDbParameter("@Description ", AddedDescription);
				paramList[4] = new EalDbParameter("@new_TagID", -1) {Direction = ParameterDirection.InputOutput};

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_Description_Tag", paramList);

				// Return TRUE
				int returnValue = Convert.ToInt32(paramList[4].Value);
				return returnValue;

			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Add_Description_Tag", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Add_Description_Tag", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Add_Description_Tag", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return -1;
			}
		}

		/// <summary> Delete's a user's descriptive tage </summary>
		/// <param name="TagID"> Primary key for the entered the descriptive tag </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successul, otherwise FALSE </returns>
		/// <remarks> This calls the 'mySobek_Delete_Description_Tag' stored procedure</remarks> 
		public static bool Delete_Description_Tag(int TagID, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Delete_Description_Tag", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@TagID", TagID);

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Delete_Description_Tag", paramList);

				// Return TRUE
				return true;

			}
			catch (Exception ee)
			{
				lastException = ee;
				if ( Tracer != null )
				{
					Tracer.Add_Trace("SobekCM_Database.Delete_Description_Tag", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Description_Tag", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Description_Tag", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> List all descriptive tags added by a single user </summary>
		/// <param name="UserID"> Primary key for the user that entered the descriptive tags (or -1 to get ALL tags)</param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> DataTable with all of the user's descriptive tags </returns>
		/// <remarks> This calls the 'mySobek_View_All_User_Tags' stored procedure</remarks> 
		public static DataTable View_Tags_By_User(int UserID, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.View_Tags_By_User", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@UserID", UserID);

				DataSet resultSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_View_All_User_Tags", paramList);

				return resultSet.Tables[0];
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.View_Tags_By_User", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.View_Tags_By_User", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.View_Tags_By_User", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
		}


		/// <summary> List all descriptive tags added by a single user </summary>
		/// <param name="AggregationCode"> Aggregation code for which to pull all descriptive tags added </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> DataTable with all of the descriptive tags added to items within the aggregation of interest </returns>
		/// <remarks> This calls the 'SobekCM_Get_Description_Tags_By_Aggregation' stored procedure  </remarks> 
		public static DataTable View_Tags_By_Aggregation( string AggregationCode, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.View_Tags_By_Aggregation", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@aggregationcode", AggregationCode);

				DataSet resultSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Get_Description_Tags_By_Aggregation", paramList);

				return resultSet.Tables[0];
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.View_Tags_By_Aggregation", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.View_Tags_By_Aggregation", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.View_Tags_By_Aggregation", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
		}


		#endregion

		#region Methods used for SobekCM Administrative Tasks (moved from SobekCM Manager )


		/// <summary> Saves a item aggregation alias for future use </summary>
		/// <param name="Alias"> Alias string which will forward to a item aggregation </param>
		/// <param name="AggregationCode"> Code for the item aggregation to forward to </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE</returns>
		/// <remarks> This calls the 'SobekCM_Save_Item_Aggregation_Alias' stored procedure </remarks> 
		public static bool Save_Aggregation_Alias(string Alias, string AggregationCode, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Save_Aggregation_Alias", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[2];
				paramList[0] = new EalDbParameter("@alias", Alias);
				paramList[1] = new EalDbParameter("@aggregation_code", AggregationCode);

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Save_Item_Aggregation_Alias", paramList);
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Save_Aggregation_Alias", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Save_Aggregation_Alias", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Save_Aggregation_Alias", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Deletes an item aggregation alias by alias code </summary>
		/// <param name="Alias"> Alias string which forwarded to a item aggregation </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE</returns>
		/// <remarks> This calls the 'SobekCM_Delete_Item_Aggregation_Alias' stored procedure </remarks> 
		public static bool Delete_Aggregation_Alias(string Alias, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Delete_Aggregation_Alias", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@alias", Alias);

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Delete_Item_Aggregation_Alias", paramList);
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Delete_Aggregation_Alias", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Aggregation_Alias", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Aggregation_Alias", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Saves a HTML skin to the database </summary>
		/// <param name="SkinCode"> Code for this HTML skin </param>
		/// <param name="BaseSkinCode"> Base skin code from which this html skin inherits </param>
		/// <param name="OverrideBanner"> Flag indicates this skin overrides the default banner </param>
		/// <param name="OverrideHeaderFooter"> Flag indicates this skin overrides the default header/footer</param>
		/// <param name="BannerLink"> Link to which the banner sends the user </param>
		/// <param name="Notes"> Notes on this skin ( name, use, etc...) </param>
		/// <param name="SuppressTopNavigation"> Flag indicates if the top-level aggregation navigation should be suppressed for this web skin ( i.e., is the top-level navigation embedded into the header file already? )</param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'SobekCM_Add_Web_Skin' stored procedure </remarks> 
		public static bool Save_Web_Skin(string SkinCode, string BaseSkinCode, bool OverrideBanner, bool OverrideHeaderFooter, string BannerLink, string Notes, bool SuppressTopNavigation, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Save_Skin", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[8];
				paramList[0] = new EalDbParameter("@webskincode", SkinCode);
				paramList[1] = new EalDbParameter("@basewebskin", BaseSkinCode);
				paramList[2] = new EalDbParameter("@overridebanner", OverrideBanner);
				paramList[3] = new EalDbParameter("@overrideheaderfooter", OverrideHeaderFooter);
				paramList[4] = new EalDbParameter("@bannerlink", BannerLink);
				paramList[5] = new EalDbParameter("@notes", Notes);
				paramList[6] = new EalDbParameter("@build_on_launch", false);
				paramList[7] = new EalDbParameter("@suppress_top_nav", SuppressTopNavigation  );

				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Add_Web_Skin", paramList);
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Save_Web_Skin", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Save_Web_Skin", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Save_Web_Skin", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Deletes a HTML web skin fromo the database </summary>
		/// <param name="SkinCode"> Code for the  HTML web skin to delete </param>
		/// <param name="ForceDelete"> Flag indicates if this should be deleted, even if things are still attached to this web skin (system admin)</param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'SobekCM_Delete_Web_Skin' stored procedure </remarks> 
		public static bool Delete_Web_Skin(string SkinCode, bool ForceDelete, Custom_Tracer Tracer)
		{
			lastException = null;

			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Delete_Web_Skin", String.Empty);
			}

			try
			{
				// Execute this non-query stored procedure
				EalDbParameter[] paramList = new EalDbParameter[3];
				paramList[0] = new EalDbParameter("@webskincode", SkinCode);
				paramList[1] = new EalDbParameter("@force_delete", ForceDelete);
				paramList[2] = new EalDbParameter("@links", -1) { Direction = ParameterDirection.Output };

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Delete_Web_Skin", paramList);

				if (Convert.ToInt32(paramList[2].Value) > 0)
				{
					return false;
				}
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Delete_Web_Skin", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Web_Skin", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Web_Skin", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Saves information about a new icon/wordmark or modify an existing one </summary>
		/// <param name="IconName"> Code identifier for this icon/wordmark</param>
		/// <param name="IconFile"> Filename for this icon/wordmark</param>
		/// <param name="IconLink">  Link that clicking on this icon/wordmark will forward the user to</param>
		/// <param name="IconTitle"> Title for this icon, which appears when you hover over the icon </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> Primary key for this new icon (or wordmark), or -1 if this action failed</returns>
		/// <remarks> This calls the 'SobekCM_Save_Icon' stored procedure </remarks> 
		public static int Save_Icon(string IconName, string IconFile, string IconLink, string IconTitle, Custom_Tracer Tracer)
		{
			return Save_Icon( IconName, IconFile, IconLink, 80, IconTitle, Tracer);
		}

		/// <summary> Saves information about a new icon/wordmark or modify an existing one </summary>
		/// <param name="IconName"> Code identifier for this icon/wordmark</param>
		/// <param name="IconFile"> Filename for this icon/wordmark</param>
		/// <param name="IconLink">  Link that clicking on this icon/wordmark will forward the user to</param>
		/// <param name="Height"> Height for this icon/wordmark </param>
		/// <param name="IconTitle"> Title for this icon, which appears when you hover over the icon </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> Primary key for this new icon (or wordmark), or -1 if this action failed</returns>
		/// <remarks> This calls the 'SobekCM_Save_Icon' stored procedure </remarks> 
		public static int Save_Icon(string IconName, string IconFile, string IconLink, int Height, string IconTitle, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Save_Icon", String.Empty);
			}

			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[7];
				paramList[0] = new EalDbParameter("@iconid", -1 );
				paramList[1] = new EalDbParameter("@icon_name", IconName);
				paramList[2] = new EalDbParameter("@icon_url", IconFile);
				paramList[3] = new EalDbParameter("@link", IconLink);
				paramList[4] = new EalDbParameter("@height", Height);
				paramList[5] = new EalDbParameter("@title", IconTitle);
				paramList[6] = new EalDbParameter("@new_iconid", -1) {Direction = ParameterDirection.Output};

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Save_Icon", paramList);

				// Return the new icon id
				return Convert.ToInt32(paramList[6].Value);
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Save_Icon", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Save_Icon", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Save_Icon", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return -1;
			}
		}

		/// <summary> Deletes an existing wordmark/icon if it is not linked to any titles in the database </summary>
		/// <param name="IconCode"> Wordmark/icon code for the wordmark to delete </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successfully deleted, otherwise FALSE indicating the icon is linked to some titles and cannot be deleted </returns>
		/// <remarks> This calls the 'SobekCM_Delete_Icon' stored procedure </remarks> 
		public static bool Delete_Icon( string IconCode, Custom_Tracer Tracer )
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Delete_Icon", String.Empty);
			}

			try
			{
				// Clear the last exception first
				lastException = null;

				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[2];
				paramList[0] = new EalDbParameter("@icon_code", IconCode);
				paramList[1] = new EalDbParameter("@links", -1) {Direction = ParameterDirection.Output};

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Delete_Icon", paramList);

				if (Convert.ToInt32(paramList[1].Value) > 0)
				{
					return false;
				}
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Delete_Icon", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Icon", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Icon", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}


			   
		/// <summary> Gets the datatable of all users from the mySobek / personalization database </summary>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> DataTable with list of all users' id, full name, and username </returns>
		/// <remarks> This calls the 'mySobek_Get_All_Users' stored procedure</remarks> 
		public static DataTable Get_All_Users(Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Get_All_Users", String.Empty);
			}

			// Define a temporary dataset
			DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Get_All_Users");
			return tempSet.Tables[0];
		}


		/// <summary> Updates an existing item aggregation's data that appears in the basic edit aggregation form </summary>
		/// <param name="Code"> Code for this item aggregation </param>
		/// <param name="Name"> Name for this item aggregation </param>
		/// <param name="ShortName"> Short version of this item aggregation </param>
		/// <param name="IsActive"> Flag indicates if this item aggregation is active</param>
		/// <param name="IsHidden"> Flag indicates if this item is hidden</param>
		/// <param name="ExternalLink">External link for this item aggregation (usually used for institutional aggregationPermissions)</param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'SobekCM_Update_Item_Aggregation' stored procedure in the SobekCM database</remarks> 
		public static bool Update_Item_Aggregation(string Code, string Name, string ShortName, bool IsActive, bool IsHidden, string ExternalLink, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Update_Item_Aggregation", String.Empty);
			}

			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[6];
				paramList[0] = new EalDbParameter("@code", Code);
				paramList[1] = new EalDbParameter("@name", Name);
				paramList[2] = new EalDbParameter("@shortname", ShortName);
				paramList[3] = new EalDbParameter("@isActive", IsActive);
				paramList[4] = new EalDbParameter("@hidden", IsHidden);
				paramList[5] = new EalDbParameter("@externallink", ExternalLink);

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Update_Item_Aggregation", paramList);

				// Succesful, so return true
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Update_Item_Aggregation", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_Item_Aggregation", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_Item_Aggregation", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}



		/// <summary> Delete an item aggregation from the database </summary>
		/// <param name="Code"> Aggregation code for the aggregation to delete</param>
		/// <param name="Username"> Name of the user that deleted this aggregation, for the milestones </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <param name="ErrorMessage"> [OUT] Error message, if there was an error</param>
		/// <param name="IsSysAdmin"> Flag indicates if this is a system admin, who can delete aggregationPermissions with items </param>
		/// <returns> Error code - 0 if there was no error </returns>
		/// <remarks> This calls the 'SobekCM_Delete_Item_Aggregation' stored procedure</remarks> 
		public static int Delete_Item_Aggregation(string Code, bool IsSysAdmin, string Username, Custom_Tracer Tracer, out string ErrorMessage )
		{
			ErrorMessage = String.Empty;

			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Delete_Item_Aggregation", String.Empty);
			}

			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[5];
				paramList[0] = new EalDbParameter("@aggrcode", Code);
				paramList[1] = new EalDbParameter("@isadmin", IsSysAdmin);
				paramList[2] = new EalDbParameter("@username", Username);
				paramList[3] = new EalDbParameter("@message", "                                                                                               ") { Direction = ParameterDirection.InputOutput };
				paramList[4] = new EalDbParameter("@errorcode", -1) { Direction = ParameterDirection.InputOutput };

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Delete_Item_Aggregation", paramList);

				ErrorMessage = paramList[3].Value.ToString();

				// Save the error message
				// Succesful, so return new id, if there was one
				return Convert.ToInt32(paramList[4].Value);
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Delete_Item_Aggregation", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Item_Aggregation", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Item_Aggregation", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return -1;
			}
		}

		/// <summary> Add a new milestone to an existing item aggregation </summary>
		/// <param name="AggregationCode"> Item aggregation code </param>
		/// <param name="Milestone"> Milestone to add to this item aggregation </param>
		/// <param name="User"> User name which performed this work </param>
		/// <returns> TRUE if successful saving the new milestone </returns>
		/// <remarks> This calls the 'SobekCM_Add_Item_Aggregation_Milestone' stored procedure</remarks> 
		public static bool Save_Item_Aggregation_Milestone(string AggregationCode, string Milestone, string User )
		{
			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[3];
				paramList[0] = new EalDbParameter("@AggregationCode", AggregationCode);
				paramList[1] = new EalDbParameter("@Milestone", Milestone);
				paramList[2] = new EalDbParameter("@MilestoneUser", User);

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Add_Item_Aggregation_Milestone", paramList);

				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				return false;
			}
		}

		/// <summary> Gets all the milestones for a single item aggregation  </summary>
		/// <param name="AggregationCode"> Item aggregation code </param>
		/// <returns> Table of latest updates </returns>
		/// <remarks> This calls the 'SobekCM_Add_Item_Aggregation_Milestone' stored procedure</remarks> 
		public static DataTable Get_Item_Aggregation_Milestone(string AggregationCode)
		{
			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@AggregationCode", AggregationCode);

				// Execute this query stored procedure
				DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Add_Item_Aggregation_Milestone", paramList);

				return tempSet.Tables[0];
			}
			catch (Exception ee)
			{
				lastException = ee;
				return null;
			}
		}

		/// <summary> Sets a user's password to the newly provided one </summary>
		/// <param name="UserID"> Primary key for this user from the database </param>
		/// <param name="NewPassword"> New password (unencrypted) to set for this user </param>
		/// <param name="IsTemporaryPassword"> Flag indicates if this is a temporary password that must be reset the first time the user logs on</param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwsie FALSE  </returns>
		/// <remarks> This calls the 'mySobek_Reset_User_Password' stored procedure</remarks> 
		public static bool Reset_User_Password(int UserID, string NewPassword, bool IsTemporaryPassword, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Reset_User_Password", String.Empty);
			}

			const string SALT = "This is my salt to add to the password";
			string encryptedPassword = SecurityInfo.SHA1_EncryptString(NewPassword + SALT);

			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[3];
				paramList[0] = new EalDbParameter("@userid", UserID);
				paramList[1] = new EalDbParameter("@password", encryptedPassword);
				paramList[2] = new EalDbParameter("@is_temporary", IsTemporaryPassword);

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Reset_User_Password", paramList);

				// Succesful, so return true
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Reset_User_Password", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Reset_User_Password", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Reset_User_Password", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

        /// <summary> Sets some of the permissions values for a single user </summary>
        /// <param name="UserID"> Primary key for this user from the database </param>
        /// <param name="CanSubmit"> Flag indicates if this user can submit items </param>
        /// <param name="IsInternal"> Flag indicates if this user is considered an 'internal user'</param>
        /// <param name="CanEditAll"> Flag indicates if this user is authorized to edit all items in the library</param>
        /// <param name="IsUserAdmin"> Flag indicates if this user is a user Administrator </param>
        /// <param name="IsHostAdmin"> Flag indicates if this used is the host administrator (if this is a hosted instance) </param>
        /// <param name="IsPortalAdmin"> Flag indicates if this user is a portal Administrator </param>
        /// <param name="CanDeleteAll"> Flag indicates if this user can delete anything in the repository </param>
        /// <param name="IsSystemAdmin"> Flag indicates if this user is a system Administrator</param>
        /// <param name="IncludeTrackingStandardForms"> Flag indicates if this user should have tracking portions appear in their standard forms </param>
        /// <param name="EditTemplate"> CompleteTemplate name for editing non-MARC records </param>
        /// <param name="EditTemplateMarc"> CompleteTemplate name for editing MARC-derived records </param>
        /// <param name="ClearProjectsTemplates"> Flag indicates whether to clear projects and templates for this user </param>
        /// <param name="ClearAggregationLinks"> Flag indicates whether to clear item aggregationPermissions linked to this user</param>
        /// <param name="ClearUserGroups"> Flag indicates whether to clear user group membership for this user </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        /// <remarks> This calls the 'mySobek_Update_User' stored procedure</remarks> 
        public static bool Update_SobekCM_User(int UserID, bool CanSubmit, bool IsInternal, bool CanEditAll, bool CanDeleteAll, bool IsUserAdmin, bool IsSystemAdmin, bool IsHostAdmin, bool IsPortalAdmin, bool IncludeTrackingStandardForms, string EditTemplate, string EditTemplateMarc, bool ClearProjectsTemplates, bool ClearAggregationLinks, bool ClearUserGroups, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User", String.Empty);
			}

			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[15];
				paramList[0] = new EalDbParameter("@userid", UserID);
				paramList[1] = new EalDbParameter("@can_submit", CanSubmit);
				paramList[2] = new EalDbParameter("@is_internal", IsInternal);
				paramList[3] = new EalDbParameter("@can_edit_all", CanEditAll);
				paramList[4] = new EalDbParameter("@can_delete_all", CanDeleteAll);
                paramList[5] = new EalDbParameter("@is_user_admin", IsUserAdmin);
                paramList[6] = new EalDbParameter("@is_portal_admin", IsPortalAdmin);
				paramList[7] = new EalDbParameter("@is_system_admin", IsSystemAdmin);
                paramList[8] = new EalDbParameter("@is_host_admin", IsHostAdmin);
				paramList[9] = new EalDbParameter("@include_tracking_standard_forms", IncludeTrackingStandardForms);
				paramList[10] = new EalDbParameter("@edit_template", EditTemplate);
				paramList[11] = new EalDbParameter("@edit_template_marc", EditTemplateMarc);
				paramList[12] = new EalDbParameter("@clear_projects_templates", ClearProjectsTemplates);
				paramList[13] = new EalDbParameter("@clear_aggregation_links", ClearAggregationLinks);
				paramList[14] = new EalDbParameter("@clear_user_groups", ClearUserGroups);

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Update_User", paramList);

				// Succesful, so return true
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Sets the list of templates possible for a given user </summary>
		/// <param name="UserID"> Primary key for this user from the database </param>
		/// <param name="Templates"> List of templates to link to this user </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'mySobek_Add_User_Templates_Link' stored procedure</remarks> 
		public static bool Update_SobekCM_User_Templates(int UserID, ReadOnlyCollection<string> Templates, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Templates", String.Empty);
			}

			// Call the routine
			try
			{
				// Build the parameter list for the first run
				EalDbParameter[] paramList = new EalDbParameter[6];
				paramList[0] = new EalDbParameter("@userid", UserID);

				if (Templates.Count > 0)
					paramList[1] = new EalDbParameter("@template_default", Templates[0]);
				else
					paramList[1] = new EalDbParameter("@template_default", String.Empty);

				if (Templates.Count > 1)
					paramList[2] = new EalDbParameter("@template2", Templates[1]);
				else
					paramList[2] = new EalDbParameter("@template2", String.Empty);

				if (Templates.Count > 2)
					paramList[3] = new EalDbParameter("@template3", Templates[2]);
				else
					paramList[3] = new EalDbParameter("@template3", String.Empty);

				if (Templates.Count > 3)
					paramList[4] = new EalDbParameter("@template4", Templates[3]);
				else
					paramList[4] = new EalDbParameter("@template4", String.Empty);

				if (Templates.Count > 4)
					paramList[5] = new EalDbParameter("@template5", Templates[4]);
				else
					paramList[5] = new EalDbParameter("@template5", String.Empty);

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_User_Templates_Link", paramList);

				int currentIndex = 5;
				while (Templates.Count > currentIndex)
				{
					paramList[0] = new EalDbParameter("@userid", UserID);
					paramList[1] = new EalDbParameter("@template_default", String.Empty);

					if (Templates.Count > currentIndex)
						paramList[2] = new EalDbParameter("@template2", Templates[currentIndex]);
					else
						paramList[2] = new EalDbParameter("@template2", String.Empty);

					if (Templates.Count > currentIndex + 1 )
						paramList[3] = new EalDbParameter("@template3", Templates[currentIndex + 1]);
					else
						paramList[3] = new EalDbParameter("@template3", String.Empty);

					if (Templates.Count > currentIndex + 2)
						paramList[4] = new EalDbParameter("@template4", Templates[currentIndex + 2]);
					else
						paramList[4] = new EalDbParameter("@template4", String.Empty);

					if (Templates.Count > currentIndex + 3)
						paramList[5] = new EalDbParameter("@template5", Templates[currentIndex + 3]);
					else
						paramList[5] = new EalDbParameter("@template5", String.Empty);

					// Execute this query stored procedure
					EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_User_Templates_Link", paramList);

					currentIndex += 4;
				} 

				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Templates", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Templates", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Templates", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Sets the list of default metadata sets possible for a given user </summary>
		/// <param name="UserID"> Primary key for this user from the database </param>
		/// <param name="MetadataSets"> List of default metadata sets to link to this user</param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'mySobek_Add_User_DefaultMetadata_Link' stored procedure</remarks> 
		public static bool Update_SobekCM_User_DefaultMetadata(int UserID, ReadOnlyCollection<string> MetadataSets, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_DefaultMetadata", String.Empty);
			}

			// Call the routine
			try
			{
				// Build the parameter list for the first run
				EalDbParameter[] paramList = new EalDbParameter[6];
				paramList[0] = new EalDbParameter("@userid", UserID);
				if (MetadataSets.Count > 0)
					paramList[1] = new EalDbParameter("@metadata_default", MetadataSets[0]);
				else
					paramList[1] = new EalDbParameter("@metadata_default", String.Empty);

				if (MetadataSets.Count > 1)
					paramList[2] = new EalDbParameter("@metadata2", MetadataSets[1]);
				else
					paramList[2] = new EalDbParameter("@metadata2", String.Empty);

				if (MetadataSets.Count > 2)
					paramList[3] = new EalDbParameter("@metadata3", MetadataSets[2]);
				else
					paramList[3] = new EalDbParameter("@metadata3", String.Empty);

				if (MetadataSets.Count > 3)
					paramList[4] = new EalDbParameter("@metadata4", MetadataSets[3]);
				else
					paramList[4] = new EalDbParameter("@metadata4", String.Empty);

				if (MetadataSets.Count > 4)
					paramList[5] = new EalDbParameter("@metadata5", MetadataSets[4]);
				else
					paramList[5] = new EalDbParameter("@metadata5", String.Empty);

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_User_DefaultMetadata_Link", paramList);

				int currentIndex = 5;
				while (MetadataSets.Count > currentIndex)
				{
					paramList[0] = new EalDbParameter("@userid", UserID);
					paramList[1] = new EalDbParameter("@metadata_default", String.Empty);

					if (MetadataSets.Count > currentIndex)
						paramList[2] = new EalDbParameter("@metadata2", MetadataSets[currentIndex]);
					else
						paramList[2] = new EalDbParameter("@metadata2", String.Empty);

					if (MetadataSets.Count > currentIndex + 1)
						paramList[3] = new EalDbParameter("@metadata3", MetadataSets[currentIndex + 1]);
					else
						paramList[3] = new EalDbParameter("@metadata3", String.Empty);

					if (MetadataSets.Count > currentIndex + 2)
						paramList[4] = new EalDbParameter("@metadata4", MetadataSets[currentIndex + 2]);
					else
						paramList[4] = new EalDbParameter("@metadata4", String.Empty);

					if (MetadataSets.Count > currentIndex + 3)
						paramList[5] = new EalDbParameter("@metadata5", MetadataSets[currentIndex + 3]);
					else
						paramList[5] = new EalDbParameter("@metadata5", String.Empty);

					// Execute this query stored procedure
					EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_User_DefaultMetadata_Link", paramList);

					currentIndex += 4;
				}

				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_DefaultMetadata", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_DefaultMetadata", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_DefaultMetadata", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Sets the list of aggregationPermissions and permissions tagged to a given user </summary>
		/// <param name="UserID"> Primary key for this user from the database </param>
		/// <param name="Aggregations"> List of aggregationPermissions and permissions to link to this user </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'SobekCM_Add_User_Aggregations_Link' stored procedure</remarks> 
		public static bool Update_SobekCM_User_Aggregations(int UserID, List<User_Permissioned_Aggregation> Aggregations, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Aggregations", String.Empty);
			}

			// Call the routine
			try
			{
				// Build the parameter list for the first run
				EalDbParameter[] paramList = new EalDbParameter[34];
				paramList[0] = new EalDbParameter("@UserID", UserID);

				if (( Aggregations != null ) && ( Aggregations.Count > 0))
				{
					paramList[1] = new EalDbParameter("@AggregationCode1", Aggregations[0].Code);
					paramList[2] = new EalDbParameter("@canSelect1", Aggregations[0].CanSelect);
					paramList[3] = new EalDbParameter("@canEditMetadata1", Aggregations[0].CanEditMetadata);
					paramList[4] = new EalDbParameter("@canEditBehaviors1", Aggregations[0].CanEditBehaviors);
					paramList[5] = new EalDbParameter("@canPerformQc1", Aggregations[0].CanPerformQc);
					paramList[6] = new EalDbParameter("@canUploadFiles1", Aggregations[0].CanUploadFiles);
					paramList[7] = new EalDbParameter("@canChangeVisibility1", Aggregations[0].CanChangeVisibility);
					paramList[8] = new EalDbParameter("@canDelete1", Aggregations[0].CanDelete);
					paramList[9] = new EalDbParameter("@isCurator1", Aggregations[0].IsCurator);
					paramList[10] = new EalDbParameter("@onHomePage1", Aggregations[0].OnHomePage);
					paramList[11] = new EalDbParameter("@isAdmin1", Aggregations[0].IsAdmin);
				}
				else
				{
					paramList[1] = new EalDbParameter("@AggregationCode1", String.Empty);
					paramList[2] = new EalDbParameter("@canSelect1", false);
					paramList[3] = new EalDbParameter("@canEditMetadata1", false);
					paramList[4] = new EalDbParameter("@canEditBehaviors1", false);
					paramList[5] = new EalDbParameter("@canPerformQc1", false);
					paramList[6] = new EalDbParameter("@canUploadFiles1", false);
					paramList[7] = new EalDbParameter("@canChangeVisibility1", false);
					paramList[8] = new EalDbParameter("@canDelete1", false);
					paramList[9] = new EalDbParameter("@isCurator1", false);
					paramList[10] = new EalDbParameter("@onHomePage1", false);
					paramList[11] = new EalDbParameter("@isAdmin1", false);
				}

				if (( Aggregations != null ) && ( Aggregations.Count > 1))
				{
					paramList[12] = new EalDbParameter("@AggregationCode2", Aggregations[1].Code);
					paramList[13] = new EalDbParameter("@canSelect2", Aggregations[1].CanSelect);
					paramList[14] = new EalDbParameter("@canEditMetadata2", Aggregations[1].CanEditMetadata);
					paramList[15] = new EalDbParameter("@canEditBehaviors2", Aggregations[1].CanEditBehaviors);
					paramList[16] = new EalDbParameter("@canPerformQc2", Aggregations[1].CanPerformQc);
					paramList[17] = new EalDbParameter("@canUploadFiles2", Aggregations[1].CanUploadFiles);
					paramList[18] = new EalDbParameter("@canChangeVisibility2", Aggregations[1].CanChangeVisibility);
					paramList[19] = new EalDbParameter("@canDelete2", Aggregations[1].CanDelete);
					paramList[20] = new EalDbParameter("@isCurator2", Aggregations[1].IsCurator);
					paramList[21] = new EalDbParameter("@onHomePage2", Aggregations[1].OnHomePage);
					paramList[22] = new EalDbParameter("@isAdmin2", Aggregations[1].IsAdmin);
				}
				else
				{
					paramList[12] = new EalDbParameter("@AggregationCode2", String.Empty);
					paramList[13] = new EalDbParameter("@canSelect2", false);
					paramList[14] = new EalDbParameter("@canEditMetadata2", false);
					paramList[15] = new EalDbParameter("@canEditBehaviors2", false);
					paramList[16] = new EalDbParameter("@canPerformQc2", false);
					paramList[17] = new EalDbParameter("@canUploadFiles2", false);
					paramList[18] = new EalDbParameter("@canChangeVisibility2", false);
					paramList[19] = new EalDbParameter("@canDelete2", false);
					paramList[20] = new EalDbParameter("@isCurator2", false);
					paramList[21] = new EalDbParameter("@onHomePage2", false);
					paramList[22] = new EalDbParameter("@isAdmin2", false);
				}


				if (( Aggregations != null ) && ( Aggregations.Count > 2))
				{
					paramList[23] = new EalDbParameter("@AggregationCode3", Aggregations[2].Code);
					paramList[24] = new EalDbParameter("@canSelect3", Aggregations[2].CanSelect);
					paramList[25] = new EalDbParameter("@canEditMetadata3", Aggregations[2].CanEditMetadata);
					paramList[26] = new EalDbParameter("@canEditBehaviors3", Aggregations[2].CanEditBehaviors);
					paramList[27] = new EalDbParameter("@canPerformQc3", Aggregations[2].CanPerformQc);
					paramList[28] = new EalDbParameter("@canUploadFiles3", Aggregations[2].CanUploadFiles);
					paramList[29] = new EalDbParameter("@canChangeVisibility3", Aggregations[2].CanChangeVisibility);
					paramList[30] = new EalDbParameter("@canDelete3", Aggregations[2].CanDelete);
					paramList[31] = new EalDbParameter("@isCurator3", Aggregations[2].IsCurator);
					paramList[32] = new EalDbParameter("@onHomePage3", Aggregations[2].OnHomePage);
					paramList[33] = new EalDbParameter("@isAdmin3", Aggregations[2].IsAdmin);
				}
				else
				{
					paramList[23] = new EalDbParameter("@AggregationCode3", String.Empty);
					paramList[24] = new EalDbParameter("@canSelect3", false);
					paramList[25] = new EalDbParameter("@canEditMetadata3", false);
					paramList[26] = new EalDbParameter("@canEditBehaviors3", false);
					paramList[27] = new EalDbParameter("@canPerformQc3", false);
					paramList[28] = new EalDbParameter("@canUploadFiles3", false);
					paramList[29] = new EalDbParameter("@canChangeVisibility3", false);
					paramList[30] = new EalDbParameter("@canDelete3", false);
					paramList[31] = new EalDbParameter("@isCurator3", false);
					paramList[32] = new EalDbParameter("@onHomePage3", false);
					paramList[33] = new EalDbParameter("@isAdmin3", false);
				}
				
				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_User_Aggregations_Link", paramList);

				int currentIndex = 3;
				while (( Aggregations != null ) && ( Aggregations.Count > currentIndex))
				{
					// Build the parameter list for the first run
					paramList[0] = new EalDbParameter("@UserID", UserID);

					if (Aggregations.Count > currentIndex)
					{
						paramList[1] = new EalDbParameter("@AggregationCode1", Aggregations[currentIndex].Code);
						paramList[2] = new EalDbParameter("@canSelect1", Aggregations[currentIndex].CanSelect);
						paramList[3] = new EalDbParameter("@canEditMetadata1", Aggregations[currentIndex].CanEditMetadata);
						paramList[4] = new EalDbParameter("@canEditBehaviors1", Aggregations[currentIndex].CanEditBehaviors);
						paramList[5] = new EalDbParameter("@canPerformQc1", Aggregations[currentIndex].CanPerformQc);
						paramList[6] = new EalDbParameter("@canUploadFiles1", Aggregations[currentIndex].CanUploadFiles);
						paramList[7] = new EalDbParameter("@canChangeVisibility1", Aggregations[currentIndex].CanChangeVisibility);
						paramList[8] = new EalDbParameter("@canDelete1", Aggregations[currentIndex].CanDelete);
						paramList[9] = new EalDbParameter("@isCurator1", Aggregations[currentIndex].IsCurator);
						paramList[10] = new EalDbParameter("@onHomePage1", Aggregations[currentIndex].OnHomePage);
						paramList[11] = new EalDbParameter("@isAdmin1", Aggregations[currentIndex].IsAdmin);
					}
					else
					{
						paramList[1] = new EalDbParameter("@AggregationCode1", String.Empty);
						paramList[2] = new EalDbParameter("@canSelect1", false);
						paramList[3] = new EalDbParameter("@canEditMetadata1", false);
						paramList[4] = new EalDbParameter("@canEditBehaviors1", false);
						paramList[5] = new EalDbParameter("@canPerformQc1", false);
						paramList[6] = new EalDbParameter("@canUploadFiles1", false);
						paramList[7] = new EalDbParameter("@canChangeVisibility1", false);
						paramList[8] = new EalDbParameter("@canDelete1", false);
						paramList[9] = new EalDbParameter("@isCurator1", false);
						paramList[10] = new EalDbParameter("@onHomePage1", false);
						paramList[11] = new EalDbParameter("@isAdmin1", false);
					}

					if (Aggregations.Count > currentIndex + 1)
					{
						paramList[12] = new EalDbParameter("@AggregationCode2", Aggregations[currentIndex + 1].Code);
						paramList[13] = new EalDbParameter("@canSelect2", Aggregations[currentIndex + 1].CanSelect);
						paramList[14] = new EalDbParameter("@canEditMetadata2", Aggregations[currentIndex + 1].CanEditMetadata);
						paramList[15] = new EalDbParameter("@canEditBehaviors2", Aggregations[currentIndex + 1].CanEditBehaviors);
						paramList[16] = new EalDbParameter("@canPerformQc2", Aggregations[currentIndex + 1].CanPerformQc);
						paramList[17] = new EalDbParameter("@canUploadFiles2", Aggregations[currentIndex + 1].CanUploadFiles);
						paramList[18] = new EalDbParameter("@canChangeVisibility2", Aggregations[currentIndex + 1].CanChangeVisibility);
						paramList[19] = new EalDbParameter("@canDelete2", Aggregations[currentIndex + 1].CanDelete);
						paramList[20] = new EalDbParameter("@isCurator2", Aggregations[currentIndex + 1].IsCurator);
						paramList[21] = new EalDbParameter("@onHomePage2", Aggregations[currentIndex + 1].OnHomePage);
						paramList[22] = new EalDbParameter("@isAdmin2", Aggregations[currentIndex + 1].IsAdmin);
					}
					else
					{
						paramList[12] = new EalDbParameter("@AggregationCode2", String.Empty);
						paramList[13] = new EalDbParameter("@canSelect2", false);
						paramList[14] = new EalDbParameter("@canEditMetadata2", false);
						paramList[15] = new EalDbParameter("@canEditBehaviors2", false);
						paramList[16] = new EalDbParameter("@canPerformQc2", false);
						paramList[17] = new EalDbParameter("@canUploadFiles2", false);
						paramList[18] = new EalDbParameter("@canChangeVisibility2", false);
						paramList[19] = new EalDbParameter("@canDelete2", false);
						paramList[20] = new EalDbParameter("@isCurator2", false);
						paramList[21] = new EalDbParameter("@onHomePage2", false);
						paramList[22] = new EalDbParameter("@isAdmin2", false);
					}


					if (Aggregations.Count > currentIndex + 2)
					{
						paramList[23] = new EalDbParameter("@AggregationCode3", Aggregations[currentIndex + 2].Code);
						paramList[24] = new EalDbParameter("@canSelect3", Aggregations[currentIndex + 2].CanSelect);
						paramList[25] = new EalDbParameter("@canEditMetadata3", Aggregations[currentIndex + 2].CanEditMetadata);
						paramList[26] = new EalDbParameter("@canEditBehaviors3", Aggregations[currentIndex + 2].CanEditBehaviors);
						paramList[27] = new EalDbParameter("@canPerformQc3", Aggregations[currentIndex + 2].CanPerformQc);
						paramList[28] = new EalDbParameter("@canUploadFiles3", Aggregations[currentIndex + 2].CanUploadFiles);
						paramList[29] = new EalDbParameter("@canChangeVisibility3", Aggregations[currentIndex + 2].CanChangeVisibility);
						paramList[30] = new EalDbParameter("@canDelete3", Aggregations[currentIndex + 2].CanDelete);
						paramList[31] = new EalDbParameter("@isCurator3", Aggregations[currentIndex + 2].IsCurator);
						paramList[32] = new EalDbParameter("@onHomePage3", Aggregations[currentIndex + 2].OnHomePage);
						paramList[33] = new EalDbParameter("@isAdmin3", Aggregations[currentIndex + 2].IsAdmin);
					}
					else
					{
						paramList[23] = new EalDbParameter("@AggregationCode3", String.Empty);
						paramList[24] = new EalDbParameter("@canSelect3", false);
						paramList[25] = new EalDbParameter("@canEditMetadata3", false);
						paramList[26] = new EalDbParameter("@canEditBehaviors3", false);
						paramList[27] = new EalDbParameter("@canPerformQc3", false);
						paramList[28] = new EalDbParameter("@canUploadFiles3", false);
						paramList[29] = new EalDbParameter("@canChangeVisibility3", false);
						paramList[30] = new EalDbParameter("@canDelete3", false);
						paramList[31] = new EalDbParameter("@isCurator3", false);
						paramList[32] = new EalDbParameter("@onHomePage3", false);
						paramList[33] = new EalDbParameter("@isAdmin3", false);
					}
					 
					// Execute this query stored procedure
					EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_User_Aggregations_Link", paramList);

					currentIndex += 3;
				}

				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Aggregations", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Aggregations", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Aggregations", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}


	    /// <summary> Sets some of the basic information and global permissions values for a single user group </summary>
	    /// <param name="UserGroupID"> Primary key for this user group from the database, or -1 for a new user group </param>
	    /// <param name="GroupName"> Name of this user group </param>
	    /// <param name="GroupDescription"> Basic description of this user group </param>
	    /// <param name="CanSubmit"> Flag indicates if this user group can submit items </param>
	    /// <param name="IsInternal"> Flag indicates if this user group is considered an 'internal user'</param>
	    /// <param name="CanEditAll"> Flag indicates if this user group is authorized to edit all items in the library</param>
	    /// <param name="IsSystemAdmin"> Flag indicates if this user group is a system Administrator</param>
	    /// <param name="IsPortalAdmin"> Flag indicated if this user group is a portal administrator </param>
	    /// <param name="IncludeTrackingStandardForms"> Should this user's settings include the tracking form portions? </param>
	    /// <param name="ClearMetadataTemplates"> Flag indicates whether to clear default metadata sets and templates for this user group </param>
	    /// <param name="ClearAggregationLinks"> Flag indicates whether to clear item aggregationPermissions linked to this user group </param>
	    /// <param name="ClearEditableLinks"> Flag indicates whether to clear the link between this user group and editable regex expressions  </param>
	    /// <param name="IsLdapDefault"></param>
	    /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
	    /// <param name="IsSobekDefault"></param>
	    /// <param name="IsShibbolethDefault"></param>
	    /// <returns> UserGroupId for a new user group, if this was to save a new one </returns>
	    /// <remarks> This calls the 'mySobek_Save_User_Group' stored procedure</remarks> 
	    public static int Save_User_Group(int UserGroupID, string GroupName, string GroupDescription, bool CanSubmit, bool IsInternal, bool CanEditAll, bool IsSystemAdmin, bool IsPortalAdmin, bool IncludeTrackingStandardForms, bool ClearMetadataTemplates, bool ClearAggregationLinks, bool ClearEditableLinks, bool IsSobekDefault, bool IsShibbolethDefault, bool IsLdapDefault, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Save_User_Group", String.Empty);
			}

			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[16];
				paramList[0] = new EalDbParameter("@usergroupid", UserGroupID);
				paramList[1] = new EalDbParameter("@groupname", GroupName);
				paramList[2] = new EalDbParameter("@groupdescription", GroupDescription);
				paramList[3] = new EalDbParameter("@can_submit_items", CanSubmit);
				paramList[4] = new EalDbParameter("@is_internal", IsInternal);
				paramList[5] = new EalDbParameter("@can_edit_all", CanEditAll);
				paramList[6] = new EalDbParameter("@is_system_admin", IsSystemAdmin);
				paramList[7] = new EalDbParameter("@is_portal_admin", IsPortalAdmin);
				paramList[8] = new EalDbParameter("@include_tracking_standard_forms", IncludeTrackingStandardForms );
				paramList[9] = new EalDbParameter("@clear_metadata_templates", ClearMetadataTemplates);
				paramList[10] = new EalDbParameter("@clear_aggregation_links", ClearAggregationLinks);
				paramList[11] = new EalDbParameter("@clear_editable_links", ClearEditableLinks);
				paramList[12] = new EalDbParameter("@is_sobek_default", IsSobekDefault);
                paramList[13] = new EalDbParameter("@is_shibboleth_default", IsShibbolethDefault);
                paramList[14] = new EalDbParameter("@is_ldap_default", IsLdapDefault);
				paramList[15] = new EalDbParameter("@new_usergroupid", UserGroupID) {Direction = ParameterDirection.InputOutput};

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Save_User_Group", paramList);

				// Succesful, so return new id, if there was one
				return Convert.ToInt32(paramList[15].Value);
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Save_User_Group", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Save_User_Group", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Save_User_Group", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return -1;
			}
		}

		/// <summary> Sets the list of templates possible for a given user group </summary>
		/// <param name="UserGroupID"> Primary key for this user group from the database </param>
		/// <param name="Templates"> List of templates to link to this user group </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'mySobek_Add_User_Group_Templates_Link' stored procedure</remarks> 
		public static bool Update_SobekCM_User_Group_Templates(int UserGroupID, List<string> Templates, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Group_Templates", String.Empty);
			}

			// Ensure five values
			while (Templates.Count < 5)
				Templates.Add(String.Empty);

			// Call the routine
			try
			{
				// Build the parameter list for the first run
				EalDbParameter[] paramList = new EalDbParameter[6];
				paramList[0] = new EalDbParameter("@usergroupid", UserGroupID);
				paramList[1] = new EalDbParameter("@template1", Templates[0]);
				paramList[2] = new EalDbParameter("@template2", Templates[1]);
				paramList[3] = new EalDbParameter("@template3", Templates[2]);
				paramList[4] = new EalDbParameter("@template4", Templates[3]);
				paramList[5] = new EalDbParameter("@template5", Templates[4]);

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_User_Group_Templates_Link", paramList);

				int currentIndex = 5;
				while (Templates.Count > currentIndex)
				{
					while (Templates.Count < currentIndex + 4)
						Templates.Add(String.Empty);

					paramList[0] = new EalDbParameter("@usergroupid", UserGroupID);
					paramList[1] = new EalDbParameter("@template1", String.Empty);
					paramList[2] = new EalDbParameter("@template2", Templates[currentIndex]);
					paramList[3] = new EalDbParameter("@template3", Templates[currentIndex + 1]);
					paramList[4] = new EalDbParameter("@template4", Templates[currentIndex + 2]);
					paramList[5] = new EalDbParameter("@template5", Templates[currentIndex + 3]);

					// Execute this query stored procedure
					EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_User_Group_Templates_Link", paramList);

					currentIndex += 4;
				}

				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Group_Templates", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Group_Templates", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Group_Templates", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Sets the list of default metadata sets possible for a given user group </summary>
		/// <param name="UserGroupID"> Primary key for this user group from the database </param>
		/// <param name="MetadataSets"> List of default metadata sets to link to this user group</param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'mySobek_Add_User_Group_Metadata_Link' stored procedure</remarks> 
		public static bool Update_SobekCM_User_Group_DefaultMetadata(int UserGroupID, List<string> MetadataSets, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Group_DefaultMetadata", String.Empty);
			}

			// Ensure five values
			while (MetadataSets.Count < 5)
				MetadataSets.Add(String.Empty);

			// Call the routine
			try
			{
				// Build the parameter list for the first run
				EalDbParameter[] paramList = new EalDbParameter[6];
				paramList[0] = new EalDbParameter("@usergroupid", UserGroupID);
				paramList[1] = new EalDbParameter("@metadata1", MetadataSets[0]);
				paramList[2] = new EalDbParameter("@metadata2", MetadataSets[1]);
				paramList[3] = new EalDbParameter("@metadata3", MetadataSets[2]);
				paramList[4] = new EalDbParameter("@metadata4", MetadataSets[3]);
				paramList[5] = new EalDbParameter("@metadata5", MetadataSets[4]);

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_User_Group_Metadata_Link", paramList);

				int currentIndex = 5;
				while (MetadataSets.Count > currentIndex)
				{
					while (MetadataSets.Count < currentIndex + 4)
						MetadataSets.Add(String.Empty);

					paramList[0] = new EalDbParameter("@usergroupid", UserGroupID);
					paramList[1] = new EalDbParameter("@metadata1", String.Empty);
					paramList[2] = new EalDbParameter("@metadata2", MetadataSets[currentIndex]);
					paramList[3] = new EalDbParameter("@metadata3", MetadataSets[currentIndex + 1]);
					paramList[4] = new EalDbParameter("@metadata4", MetadataSets[currentIndex + 2]);
					paramList[5] = new EalDbParameter("@metadata5", MetadataSets[currentIndex + 3]);

					// Execute this query stored procedure
					EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_User_Group_Metadata_Link", paramList);

					currentIndex += 4;
				}

				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Group_DefaultMetadata", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Group_DefaultMetadata", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Group_DefaultMetadata", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Sets the list of aggregationPermissions and permissions tagged to a given user group</summary>
		/// <param name="UserGroupID"> Primary key for this user group from the database </param>
		/// <param name="Aggregations"> List of aggregationPermissions and permissions to link to this user group </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'SobekCM_Add_User_Group_Aggregations_Link' stored procedure</remarks> 
        public static bool Update_SobekCM_User_Group_Aggregations(int UserGroupID, List<User_Permissioned_Aggregation> Aggregations, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Group_Aggregations", String.Empty);
			}

			// Call the routine
			try
			{
				// Build the parameter list for the first run
				EalDbParameter[] paramList = new EalDbParameter[34];
				paramList[0] = new EalDbParameter("@UserGroupID", UserGroupID);

				if (Aggregations.Count > 0)
				{
					paramList[1] = new EalDbParameter("@AggregationCode1", Aggregations[0].Code);
					paramList[2] = new EalDbParameter("@canSelect1", Aggregations[0].CanSelect);
					paramList[3] = new EalDbParameter("@canEditMetadata1", Aggregations[0].CanEditMetadata);
					paramList[4] = new EalDbParameter("@canEditBehaviors1", Aggregations[0].CanEditBehaviors);
					paramList[5] = new EalDbParameter("@canPerformQc1", Aggregations[0].CanPerformQc);
					paramList[6] = new EalDbParameter("@canUploadFiles1", Aggregations[0].CanUploadFiles);
					paramList[7] = new EalDbParameter("@canChangeVisibility1", Aggregations[0].CanChangeVisibility);
					paramList[8] = new EalDbParameter("@canDelete1", Aggregations[0].CanDelete);
					paramList[9] = new EalDbParameter("@isCurator1", Aggregations[0].IsCurator);
					paramList[10] = new EalDbParameter("@onHomePage1", false);
					paramList[11] = new EalDbParameter("@isAdmin1", Aggregations[0].IsAdmin);
				}
				else
				{
					paramList[1] = new EalDbParameter("@AggregationCode1", String.Empty);
					paramList[2] = new EalDbParameter("@canSelect1", false);
					paramList[3] = new EalDbParameter("@canEditMetadata1", false);
					paramList[4] = new EalDbParameter("@canEditBehaviors1", false);
					paramList[5] = new EalDbParameter("@canPerformQc1", false);
					paramList[6] = new EalDbParameter("@canUploadFiles1", false);
					paramList[7] = new EalDbParameter("@canChangeVisibility1", false);
					paramList[8] = new EalDbParameter("@canDelete1", false);
					paramList[9] = new EalDbParameter("@isCurator1", false);
					paramList[10] = new EalDbParameter("@onHomePage1", false);
					paramList[11] = new EalDbParameter("@isAdmin1", false);
				}

				if (Aggregations.Count > 1)
				{
					paramList[12] = new EalDbParameter("@AggregationCode2", Aggregations[1].Code);
					paramList[13] = new EalDbParameter("@canSelect2", Aggregations[1].CanSelect);
					paramList[14] = new EalDbParameter("@canEditMetadata2", Aggregations[1].CanEditMetadata);
					paramList[15] = new EalDbParameter("@canEditBehaviors2", Aggregations[1].CanEditBehaviors);
					paramList[16] = new EalDbParameter("@canPerformQc2", Aggregations[1].CanPerformQc);
					paramList[17] = new EalDbParameter("@canUploadFiles2", Aggregations[1].CanUploadFiles);
					paramList[18] = new EalDbParameter("@canChangeVisibility2", Aggregations[1].CanChangeVisibility);
					paramList[19] = new EalDbParameter("@canDelete2", Aggregations[1].CanDelete);
					paramList[20] = new EalDbParameter("@isCurator2", Aggregations[1].IsCurator);
					paramList[21] = new EalDbParameter("@onHomePage2", false);
					paramList[22] = new EalDbParameter("@isAdmin2", Aggregations[1].IsAdmin);
				}
				else
				{
					paramList[12] = new EalDbParameter("@AggregationCode2", String.Empty);
					paramList[13] = new EalDbParameter("@canSelect2", false);
					paramList[14] = new EalDbParameter("@canEditMetadata2", false);
					paramList[15] = new EalDbParameter("@canEditBehaviors2", false);
					paramList[16] = new EalDbParameter("@canPerformQc2", false);
					paramList[17] = new EalDbParameter("@canUploadFiles2", false);
					paramList[18] = new EalDbParameter("@canChangeVisibility2", false);
					paramList[19] = new EalDbParameter("@canDelete2", false);
					paramList[20] = new EalDbParameter("@isCurator2", false);
					paramList[21] = new EalDbParameter("@onHomePage2", false);
					paramList[22] = new EalDbParameter("@isAdmin2", false);
				}


				if (Aggregations.Count > 2)
				{
					paramList[23] = new EalDbParameter("@AggregationCode3", Aggregations[2].Code);
					paramList[24] = new EalDbParameter("@canSelect3", Aggregations[2].CanSelect);
					paramList[25] = new EalDbParameter("@canEditMetadata3", Aggregations[2].CanEditMetadata);
					paramList[26] = new EalDbParameter("@canEditBehaviors3", Aggregations[2].CanEditBehaviors);
					paramList[27] = new EalDbParameter("@canPerformQc3", Aggregations[2].CanPerformQc);
					paramList[28] = new EalDbParameter("@canUploadFiles3", Aggregations[2].CanUploadFiles);
					paramList[29] = new EalDbParameter("@canChangeVisibility3", Aggregations[2].CanChangeVisibility);
					paramList[30] = new EalDbParameter("@canDelete3", Aggregations[2].CanDelete);
					paramList[31] = new EalDbParameter("@isCurator3", Aggregations[2].IsCurator);
					paramList[32] = new EalDbParameter("@onHomePage3", false);
					paramList[33] = new EalDbParameter("@isAdmin3", Aggregations[2].IsAdmin);
				}
				else
				{
					paramList[23] = new EalDbParameter("@AggregationCode3", String.Empty);
					paramList[24] = new EalDbParameter("@canSelect3", false);
					paramList[25] = new EalDbParameter("@canEditMetadata3", false);
					paramList[26] = new EalDbParameter("@canEditBehaviors3", false);
					paramList[27] = new EalDbParameter("@canPerformQc3", false);
					paramList[28] = new EalDbParameter("@canUploadFiles3", false);
					paramList[29] = new EalDbParameter("@canChangeVisibility3", false);
					paramList[30] = new EalDbParameter("@canDelete3", false);
					paramList[31] = new EalDbParameter("@isCurator3", false);
					paramList[32] = new EalDbParameter("@onHomePage3", false);
					paramList[33] = new EalDbParameter("@isAdmin3", false);
				}

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_User_Group_Aggregations_Link", paramList);

				int currentIndex = 3;
				while (Aggregations.Count > currentIndex)
				{
					// Build the parameter list for the first run
					paramList[0] = new EalDbParameter("@UserGroupID", UserGroupID);

					if (Aggregations.Count > currentIndex)
					{
						paramList[1] = new EalDbParameter("@AggregationCode1", Aggregations[currentIndex].Code);
						paramList[2] = new EalDbParameter("@canSelect1", Aggregations[currentIndex].CanSelect);
						paramList[3] = new EalDbParameter("@canEditMetadata1", Aggregations[currentIndex].CanEditMetadata);
						paramList[4] = new EalDbParameter("@canEditBehaviors1", Aggregations[currentIndex].CanEditBehaviors);
						paramList[5] = new EalDbParameter("@canPerformQc1", Aggregations[currentIndex].CanPerformQc);
						paramList[6] = new EalDbParameter("@canUploadFiles1", Aggregations[currentIndex].CanUploadFiles);
						paramList[7] = new EalDbParameter("@canChangeVisibility1", Aggregations[currentIndex].CanChangeVisibility);
						paramList[8] = new EalDbParameter("@canDelete1", Aggregations[currentIndex].CanDelete);
						paramList[9] = new EalDbParameter("@isCurator1", Aggregations[currentIndex].IsCurator);
						paramList[10] = new EalDbParameter("@onHomePage1", false);
						paramList[11] = new EalDbParameter("@isAdmin1", Aggregations[currentIndex].IsAdmin);
					}
					else
					{
						paramList[1] = new EalDbParameter("@AggregationCode1", String.Empty);
						paramList[2] = new EalDbParameter("@canSelect1", false);
						paramList[3] = new EalDbParameter("@canEditMetadata1", false);
						paramList[4] = new EalDbParameter("@canEditBehaviors1", false);
						paramList[5] = new EalDbParameter("@canPerformQc1", false);
						paramList[6] = new EalDbParameter("@canUploadFiles1", false);
						paramList[7] = new EalDbParameter("@canChangeVisibility1", false);
						paramList[8] = new EalDbParameter("@canDelete1", false);
						paramList[9] = new EalDbParameter("@isCurator1", false);
						paramList[10] = new EalDbParameter("@onHomePage1", false);
						paramList[11] = new EalDbParameter("@isAdmin1", false);
					}

					if (Aggregations.Count > currentIndex + 1)
					{
						paramList[12] = new EalDbParameter("@AggregationCode2", Aggregations[currentIndex + 1].Code);
						paramList[13] = new EalDbParameter("@canSelect2", Aggregations[currentIndex + 1].CanSelect);
						paramList[14] = new EalDbParameter("@canEditMetadata2", Aggregations[currentIndex + 1].CanEditMetadata);
						paramList[15] = new EalDbParameter("@canEditBehaviors2", Aggregations[currentIndex + 1].CanEditBehaviors);
						paramList[16] = new EalDbParameter("@canPerformQc2", Aggregations[currentIndex + 1].CanPerformQc);
						paramList[17] = new EalDbParameter("@canUploadFiles2", Aggregations[currentIndex + 1].CanUploadFiles);
						paramList[18] = new EalDbParameter("@canChangeVisibility2", Aggregations[currentIndex + 1].CanChangeVisibility);
						paramList[19] = new EalDbParameter("@canDelete2", Aggregations[currentIndex + 1].CanDelete);
						paramList[20] = new EalDbParameter("@isCurator2", Aggregations[currentIndex + 1].IsCurator);
						paramList[21] = new EalDbParameter("@onHomePage2", false);
						paramList[22] = new EalDbParameter("@isAdmin2", Aggregations[currentIndex + 1].IsAdmin);
					}
					else
					{
						paramList[12] = new EalDbParameter("@AggregationCode2", String.Empty);
						paramList[13] = new EalDbParameter("@canSelect2", false);
						paramList[14] = new EalDbParameter("@canEditMetadata2", false);
						paramList[15] = new EalDbParameter("@canEditBehaviors2", false);
						paramList[16] = new EalDbParameter("@canPerformQc2", false);
						paramList[17] = new EalDbParameter("@canUploadFiles2", false);
						paramList[18] = new EalDbParameter("@canChangeVisibility2", false);
						paramList[19] = new EalDbParameter("@canDelete2", false);
						paramList[20] = new EalDbParameter("@isCurator2", false);
						paramList[21] = new EalDbParameter("@onHomePage2", false);
						paramList[22] = new EalDbParameter("@isAdmin2", false);
					}


					if (Aggregations.Count > currentIndex + 2)
					{
						paramList[23] = new EalDbParameter("@AggregationCode3", Aggregations[currentIndex + 2].Code);
						paramList[24] = new EalDbParameter("@canSelect3", Aggregations[currentIndex + 2].CanSelect);
						paramList[25] = new EalDbParameter("@canEditMetadata3", Aggregations[currentIndex + 2].CanEditMetadata);
						paramList[26] = new EalDbParameter("@canEditBehaviors3", Aggregations[currentIndex + 2].CanEditBehaviors);
						paramList[27] = new EalDbParameter("@canPerformQc3", Aggregations[currentIndex + 2].CanPerformQc);
						paramList[28] = new EalDbParameter("@canUploadFiles3", Aggregations[currentIndex + 2].CanUploadFiles);
						paramList[29] = new EalDbParameter("@canChangeVisibility3", Aggregations[currentIndex + 2].CanChangeVisibility);
						paramList[30] = new EalDbParameter("@canDelete3", Aggregations[currentIndex + 2].CanDelete);
						paramList[31] = new EalDbParameter("@isCurator3", Aggregations[currentIndex + 2].IsCurator);
						paramList[32] = new EalDbParameter("@onHomePage3", false);
						paramList[33] = new EalDbParameter("@isAdmin3", Aggregations[currentIndex + 2].IsAdmin);
					}
					else
					{
						paramList[23] = new EalDbParameter("@AggregationCode3", String.Empty);
						paramList[24] = new EalDbParameter("@canSelect3", false);
						paramList[25] = new EalDbParameter("@canEditMetadata3", false);
						paramList[26] = new EalDbParameter("@canEditBehaviors3", false);
						paramList[27] = new EalDbParameter("@canPerformQc3", false);
						paramList[28] = new EalDbParameter("@canUploadFiles3", false);
						paramList[29] = new EalDbParameter("@canChangeVisibility3", false);
						paramList[30] = new EalDbParameter("@canDelete3", false);
						paramList[31] = new EalDbParameter("@isCurator3", false);
						paramList[32] = new EalDbParameter("@onHomePage3", false);
						paramList[33] = new EalDbParameter("@isAdmin3", false);
					}

					// Execute this query stored procedure
					EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Add_User_Group_Aggregations_Link", paramList);

					currentIndex += 3;
				}


				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Group_Aggregations", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Group_Aggregations", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Update_SobekCM_User_Group_Aggregations", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

	    /// <summary> Deletes a user group, if there are no users attached and if it is not a special group </summary>
	    /// <param name="UserGroupID"> Primary key for this user group from the database</param>
	    /// <param name="Tracer"></param>
	    /// <returns> Message value ( -1=users attached, -2=special group, -3=exception, 1 = success) </returns>
	    /// <remarks> This calls the 'mySobek_Delete_User_Group' stored procedure</remarks> 
	    public static int Delete_User_Group(int UserGroupID, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Delete_User_Group", String.Empty);
            }

            try
            {
                // Build the parameter list
                EalDbParameter[] paramList = new EalDbParameter[2];
                paramList[0] = new EalDbParameter("@usergroupid", UserGroupID);
                paramList[1] = new EalDbParameter("@message", 1) { Direction = ParameterDirection.InputOutput };

                // Execute this query stored procedure
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Delete_User_Group", paramList);

                // Succesful, so return new id, if there was one
                return Convert.ToInt32(paramList[1].Value);
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Delete_User_Group", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Delete_User_Group", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Delete_User_Group", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return -3;
            }
        }

	    /// <summary> Saves a new default metadata set, or edits an existing default metadata name </summary>
	    /// <param name="Code"> Code for the new default metadata set, or set to edit </param>
	    /// <param name="Name"> Name for this default metadata set </param>
	    /// <param name="Description"> Full description for this default metadata set </param>
	    /// <param name="UserID"> UserID, if this is not a global default metadata set </param>
	    /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
	    /// <returns> TRUE if successful, otherwise FALSE </returns>
	    /// <remarks> This calls the 'mySobek_Save_DefaultMetadata' stored procedure</remarks> 
	    public static bool Save_Default_Metadata(string Code, string Name, string Description, int UserID, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Save_Default_Metadata", String.Empty);
			}

			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[4];
				paramList[0] = new EalDbParameter("@metadata_code", Code);
                paramList[1] = new EalDbParameter("@metadata_name", Name);
                paramList[2] = new EalDbParameter("@description", Description);
                paramList[3] = new EalDbParameter("@userid", UserID);

                if (UserID <= 0)
                    paramList[3].Value = DBNull.Value;

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Save_DefaultMetadata", paramList);

				// Succesful, so return true
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Save_Default_Metadata", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Save_Default_Metadata", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Save_Default_Metadata", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Deletes an existing default metadata </summary>
        /// <param name="Code"> Code for the default metadata to delete </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'mySobek_Delete_Project' stored procedure</remarks> 
		public static bool Delete_Default_Metadata(string Code, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Delete_Project", String.Empty);
			}

			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@MetadataCode", Code);

				// Execute this query stored procedure
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Delete_DefaultMetadata", paramList);

				// Succesful, so return true
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Delete_Project", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Project", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Delete_Project", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}

		/// <summary> Saves a new template, or edits an existing template name </summary>
		/// <param name="Code"> Code for the new template, or template to edit </param>
		/// <param name="Name"> Name for this template </param>
		/// <param name="Description"> Complete description of this template </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
		/// <returns> TRUE if successful, otherwise FALSE </returns>
		/// <remarks> This calls the 'mySobek_Save_Template' stored procedure</remarks> 
		public static bool Save_Template(string Code, string Name, string Description, Custom_Tracer Tracer)
		{
			if (Tracer != null)
			{
				Tracer.Add_Trace("SobekCM_Database.Save_Template", String.Empty);
			}

			try
			{
				// Build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[3];
				paramList[0] = new EalDbParameter("@project_code", Code);
				paramList[1] = new EalDbParameter("@project_name", Name);
                paramList[2] = new EalDbParameter("@description", Name);

				// Execute this query stored procedure
				EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "mySobek_Save_Template", paramList);

				// Succesful, so return true
				return true;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
					Tracer.Add_Trace("SobekCM_Database.Save_Template", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Save_Template", ee.Message, Custom_Trace_Type_Enum.Error);
					Tracer.Add_Trace("SobekCM_Database.Save_Template", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return false;
			}
		}


		/// <summary> Gets the build log for a particular aggregation </summary>
		/// <param name="AggregationID"> Primary key for this aggregation in the database </param>
		/// <returns> Aggregation build log table </returns>
		/// <remarks> This calls the 'SobekCM_Build_Log_Get' stored procedure </remarks> 
		public static DataTable Get_Aggregation_Build_Log(int AggregationID)
		{

			try
			{
				// build the parameter list
				EalDbParameter[] paramList = new EalDbParameter[1];
				paramList[0] = new EalDbParameter("@aggregationid", AggregationID);

				// Get the table
				DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Build_Log_Get", paramList);

				// Return true, since no exception caught
				return tempSet.Tables[0];

			}
			catch (Exception ee)
			{
				lastException = ee;
				return null;
			}
		}

		#endregion

		#region Methods related to OAI-PMH

		/// <summary> Gets the list of all OAI-enabled item aggregationPermissions </summary>
		/// <returns> DataTable with all the data about the OAI-enabled item aggregationPermissions, including code, name, description, last item added date, and any aggregation-level OAI_Metadata  </returns>
		/// <remarks> This calls the 'SobekCM_Get_OAI_Sets' stored procedure  <br /><br />
		/// This is called by the <see cref="Oai_MainWriter"/> class. </remarks> 
		public static DataTable Get_OAI_Sets()
		{
			// Define a temporary dataset
			DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Get_OAI_Sets");

			// If there was no data for this collection and entry point, return null (an ERROR occurred)
			if ((tempSet.Tables.Count == 0) || (tempSet.Tables[0] == null))
			{
				return null;
			}

			// Return the first table from the returned dataset
			return tempSet.Tables[0];
		}

	    /// <summary> Returns a list of either identifiers or records for either the entire system or a single
	    /// set, to be served through the OAI-PMH server  </summary>
	    /// <param name="SetCode"> Code the OAI-PMH set (which is really an aggregation code)</param>
	    /// <param name="DataCode"> Code for the metadata to be served ( usually oai_dc )</param>
	    /// <param name="FromDate"> Date from which to pull records which have changed </param>
	    /// <param name="UntilDate"> Date to pull up to by last modified date on the records </param>
	    /// <param name="PageSize"> Number of records to include in a single 'page' of OAI-PMH results </param>
	    /// <param name="PageNumber"> Page number of the results to return </param>
	    /// <param name="IncludeRecord"> Flag indicates whether the full records should be included, or just the identifier </param>
	    /// <returns> DataTable of all the OAI-PMH record information </returns>
	    /// <remarks> This calls the 'SobekCM_Get_OAI_Data' stored procedure  <br /><br />
	    /// This is called by the <see cref="Oai_MainWriter"/> class. </remarks> 
	    public static List<OAI_Record> Get_OAI_Data(string SetCode, string DataCode, DateTime FromDate, DateTime UntilDate, int PageSize, int PageNumber, bool IncludeRecord)
	    {

	        // Build the parameter list
	        List<EalDbParameter> parameters = new List<EalDbParameter>
	        {
	            new EalDbParameter("@aggregationcode", SetCode), 
                new EalDbParameter("@data_code", DataCode), 
                new EalDbParameter("@from", FromDate), 
                new EalDbParameter("@until", UntilDate), 
                new EalDbParameter("@pagesize", PageSize), 
                new EalDbParameter("@pagenumber", PageNumber), 
                new EalDbParameter("@include_data", IncludeRecord)
	        };

	        // Create the database agnostic reader
	        EalDbReaderWrapper readerWrapper = EalDbAccess.ExecuteDataReader(DatabaseType, Connection_String, CommandType.StoredProcedure, "SobekCM_Get_OAI_Data", parameters);

            // Pull out the database reader
            DbDataReader reader = readerWrapper.Reader;

	        // Read in each row
            List<OAI_Record> returnVal = new List<OAI_Record>();
	        while (reader.Read())
	        {
	            returnVal.Add(IncludeRecord ? new OAI_Record(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetDateTime(3)) : new OAI_Record(reader.GetString(0), reader.GetString(1), reader.GetDateTime(2)));
	        }

	        // Close the reader (which also closes the connection)
	        readerWrapper.Close();

	        return returnVal;
	    }

	    /// <summary> Returns a single OAI-PMH record, by identifier ( BibID and VID ) </summary>
	    /// <param name="BibID"> BibID the OAI-PMH record )</param>
	    /// <param name="Vid"> VID for the OAI-PMH record </param>
	    /// <param name="DataCode"> Code for the metadata to be served ( usually oai_dc or marc21)</param>
	    /// <returns> Single OAI-PMH record </returns>
	    /// <remarks> This calls the 'SobekCM_Get_OAI_Data_Item' stored procedure  <br /><br />
	    /// This is called by the <see cref="Oai_MainWriter"/> class. </remarks> 
	    public static OAI_Record Get_OAI_Record(string BibID, string Vid, string DataCode)
	    {
	        // Build the parameter list
	        List<EalDbParameter> parameters = new List<EalDbParameter>
	        {
	            new EalDbParameter("@bibid", BibID), 
                new EalDbParameter("@vid", Vid), 
                new EalDbParameter("@data_code", DataCode)
	        };

	        // Create the database agnostic reader
	        EalDbReaderWrapper readerWrapper = EalDbAccess.ExecuteDataReader(DatabaseType, Connection_String, CommandType.StoredProcedure, "SobekCM_Get_OAI_Data_Item", parameters);

	        // Pull out the database reader
	        DbDataReader reader = readerWrapper.Reader;

	        // Read in the first row
	        OAI_Record returnRecord = null;
	        if (reader.Read())
	        {
	            returnRecord = new OAI_Record(BibID, Vid, reader.GetString(2), reader.GetDateTime(3));
	        }

	        // Close the reader (which also closes the connection)
	        readerWrapper.Close();

	        return returnRecord;
	    }

	    #endregion

		#region Methods used by the Track_Item_MySobekViewer
	 
		/// <summary> Gets the list of users who are Scanning or Processing Technicians </summary>
		/// <returns>DataTable containing users who are Scanning or Processing Technicians</returns>
		public static DataTable Tracking_Get_Users_Scanning_Processing()
		{
			
			try
			{
                // Define a temporary dataset
                DataSet returnSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "Tracking_Get_Users_Scanning_Processing");

                //Return the data table
                return returnSet.Tables[0];
			}
			catch (Exception ee)
			{
				throw new ApplicationException("Error retrieving the list of users who are scanning/processing technicians from the Database"+ee.Message);
			}

		}

		/// <summary> Gets the list of users who are Scanning or Processing Technicians </summary>
		/// <returns>DataTable containing users who are Scanning or Processing Technicians</returns>
		public static DataTable Tracking_Get_Scanners_List()
		{
			try
			{
                // Define a temporary dataset
                DataSet returnSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "Tracking_Get_Scanners_List" );

                //Return the data table
                return returnSet.Tables[0];

			}
			catch (Exception ee)
			{
				throw new ApplicationException("Error retrieving the list of users who are scanning/processing technicians from the Database" + ee.Message);
			}

		}

		/// <summary> Gets the corresponding BibID, VID for a given itemID </summary>
		/// <param name="ItemID"> Primary identifier for this item from the database </param>
		/// <returns> Datarow with the BibID/VID </returns>
		public static DataRow Tracking_Get_Item_Info_from_ItemID(int ItemID)
		{
			try
			{
                EalDbParameter[] parameters = new EalDbParameter[1];
                parameters[0] = new EalDbParameter("@itemID", ItemID);

                // Define a temporary dataset
                DataSet returnSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "Tracking_Get_Item_Info_from_ItemID", parameters);

                //Return the data table
                return returnSet.Tables[0].Rows[0];
			}
			catch (Exception ee)
			{
				throw new ApplicationException("Error retrieving item details from itemID from the Database" + ee.Message);
			}

		}

		/// <summary> Gets the related workflows for an item by ItemID </summary>
		/// <param name="ItemID"> Primary key for this item in the database </param>
		/// <param name="EventNum"> Number of the event </param>
		/// <returns> DataTable of previously saved workflows for this item</returns>
		public static DataTable Tracking_Get_Open_Workflows(int ItemID, int EventNum)
		{
			try
			{
                EalDbParameter[] parameters = new EalDbParameter[2];
                parameters[0] = new EalDbParameter("@itemID", ItemID);
                parameters[1] = new EalDbParameter("@EventNumber", EventNum);

                // Define a temporary dataset
                DataSet returnSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Get_Last_Open_Workflow_By_ItemID", parameters);

                //Return the data table
                return returnSet.Tables[0];
			}
			catch (Exception ee)
			{
				throw new ApplicationException("Error retrieving last open workflow by itemID from the Database" + ee.Message);
			}

		}


		/// <summary>Gets all tracking workflow entries created by a single user </summary>
		/// <param name="Username">User Name</param>
		/// <returns>DataTable of all previous entries for this user</returns>
		public static DataTable Tracking_Get_All_Entries_By_User(string Username)
		{
			try
			{
                EalDbParameter[] parameters = new EalDbParameter[1];
                parameters[0] = new EalDbParameter("@username", Username);

                // Define a temporary dataset
                DataSet returnSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "Tracking_Get_All_Entries_By_User", parameters);

				//Return the data table
                return returnSet.Tables[0];

			}
			catch (Exception ee)
			{
				throw new ApplicationException("Error retrieving previous tracking entries for user "+Username+" from the DB. " + ee.Message);
			}

		}


		/// <summary> Save a new workflow entry during tracking</summary>
		/// <param name="ItemID"></param>
		/// <param name="WorkPerformedBy"></param>
		/// <param name="RelatedEquipment"></param>
		/// <param name="DateStarted"></param>
		/// <param name="DateCompleted"></param>
		/// <param name="EventNum"></param>
		/// <param name="StartEvent"></param>
		/// <param name="EndEvent"></param>
		/// <param name="StartEndEvent"></param>
		/// <returns></returns>
		public static int Tracking_Save_New_Workflow(int ItemID, string WorkPerformedBy, string RelatedEquipment, DateTime? DateStarted, DateTime? DateCompleted, int EventNum, int StartEvent, int EndEvent, int StartEndEvent)
		{
			int this_workflow_id;  

            // Build the parameters
		    List<EalDbParameter> parameters = new List<EalDbParameter>
		    {
		        new EalDbParameter("@itemid", ItemID), 
                new EalDbParameter("@user", WorkPerformedBy), 
                DateStarted.HasValue ? new EalDbParameter("@dateStarted", DateStarted.Value) : new EalDbParameter("@dateStarted", DBNull.Value), 
                DateCompleted.HasValue ? new EalDbParameter("@dateCompleted", DateCompleted.Value) : new EalDbParameter("@dateCompleted", DBNull.Value), 
                new EalDbParameter("@relatedEquipment", RelatedEquipment), 
                new EalDbParameter("@EventNumber", EventNum), 
                new EalDbParameter("@StartEventNumber", StartEvent),
                new EalDbParameter("@EndEventNumber", EndEvent), 
                new EalDbParameter("@Start_End_Event", StartEndEvent)
		    };

		    //Add the output parameter to get back the workflow id for this entry
		    EalDbParameter outputParam = new EalDbParameter("@workflow_entry_id", DbType.Int32) {Direction = ParameterDirection.Output};
		    parameters.Add(outputParam);

			try
			{
                EalDbAccess.ExecuteNonQuery(DatabaseType, Connection_String, CommandType.StoredProcedure, "Tracking_Add_New_Workflow", parameters);
				this_workflow_id = Convert.ToInt32(outputParam.Value);
			}
			catch (Exception ee)
			{
				throw new ApplicationException("Error saving workflow entry to the database. "+ee.Message);
			}
			

			return this_workflow_id;
		}


        /// <summary> Update an already saved tracking workflow entry </summary>
        /// <param name="WorkflowID"></param>
        /// <param name="ItemID"></param>
        /// <param name="WorkPerformedBy"></param>
        /// <param name="DateStarted"></param>
        /// <param name="DateCompleted"></param>
        /// <param name="RelatedEquipment"></param>
        /// <param name="EventNumber"></param>
        /// <param name="StartEventNumber"></param>
        /// <param name="EndEventNum"></param>
		public static void Tracking_Update_Workflow(int WorkflowID, int ItemID, string WorkPerformedBy, DateTime? DateStarted, DateTime? DateCompleted, string RelatedEquipment, int EventNumber, int StartEventNumber, int EndEventNum)
		{
			try
			{
                // Build the parameters list
			    List<EalDbParameter> parameters = new List<EalDbParameter>
			    {
			        new EalDbParameter("@workflow_entry_id", WorkflowID), 
                    new EalDbParameter("@itemid", ItemID), 
                    new EalDbParameter("@user", WorkPerformedBy), 
                    DateStarted.HasValue ? new EalDbParameter("@dateStarted", DateStarted.Value) : new EalDbParameter("@dateStarted", DBNull.Value), 
                    DateCompleted.HasValue ? new EalDbParameter("@dateCompleted", DateCompleted.Value) : new EalDbParameter("@dateCompleted", DBNull.Value), 
                    new EalDbParameter("@relatedEquipment", RelatedEquipment), 
                    new EalDbParameter("@EventNumber", EventNumber), 
                    new EalDbParameter("@StartEventNumber", StartEventNumber), 
                    new EalDbParameter("@EndEventNumber", EndEventNum)
			    };

			    // Run the SQL 
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "Tracking_Update_Workflow", parameters); 
			}
			catch (Exception ee)
			{
				throw new ApplicationException("Error updating tracking workflow "+ee.Message);
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="WorkflowID"></param>
		/// <exception cref="ApplicationException"></exception>
		public static void Tracking_Delete_Workflow(int WorkflowID)
		{
			try
			{
                // Build the parameters list
                List<EalDbParameter> parameters = new List<EalDbParameter> {  new EalDbParameter("@workflow_entry_id", WorkflowID)  };

                // Run the SQL 
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "Tracking_Delete_Workflow", parameters); 
			}
			catch (Exception ee)
			{
				throw new ApplicationException("Error deleting workflow" +ee.Message);
			}
		}

		#endregion

		#region Methods supporting USFLDC_Redirection_Service method in SobekCM_URL_Rewriter

		/// <summary> Gets aggregation code from CID in aggregation description</summary>
		/// <param name="Cid"> CID for the digital collection </param>
		/// <returns> Aggregation Code </returns>
		public static String Get_AggregationCode_From_CID(String Cid)
		{
			try
			{
				EalDbParameter[] parameters = new EalDbParameter[1];
				parameters[0] = new EalDbParameter("@cid", Cid);

				// Define a temporary dataset
				DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "USF_Get_AggregationCode_From_CID", parameters);

				// If there was no data for this collection and entry point, return null (an ERROR occurred)
				if ((tempSet.Tables.Count == 0) || (tempSet.Tables[0] == null) || (tempSet.Tables[0].Rows.Count == 0))
				{
					return null;
				}

				// Return the aggregation code from the first table
				return tempSet.Tables[0].Rows[0][0].ToString();
			}
			catch (Exception ee)
			{
				lastException = ee;
				return null;
			}
		}

		/// <summary> Pulls the BibID, VID via the Identifier </summary>
		/// <param name="Identifier"> Identifier (PURL Handle) for the digital resource object </param>
		/// <returns> BibID_VID </returns>
		public static String Get_BibID_VID_From_Identifier(string Identifier)
		{
			try
			{
				EalDbParameter[] parameters = new EalDbParameter[1];
				parameters[0] = new EalDbParameter("@identifier", Identifier);
		   
				// Define a temporary dataset
				DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Get_BibID_VID_From_Identifier", parameters);

				// If there was no data for this collection and entry point, return null (an ERROR occurred)
				if ((tempSet.Tables.Count == 0) || (tempSet.Tables[0] == null) || (tempSet.Tables[0].Rows.Count == 0))
				{
					return null;
				}

				// return BibID and VID
				return tempSet.Tables[0].Rows[0][0] + "/" + tempSet.Tables[0].Rows[0][1];
			}
			catch (Exception ee)
			{
				lastException = ee;
				return null;
			}
		}

		#endregion

        #region Methods used to support the SobekCM_Project Element (saving, deleting, retrieving,...)

	    /// <summary> Save a new, or edit an existing Project in the database </summary>
	    /// <param name="Tracer"></param>
	    /// <param name="ProjectID"></param>
	    /// <param name="ProjectCode"></param>
	    /// <param name="ProjectName"></param>
	    /// <param name="ProjectManager"></param>
	    /// <param name="GrantID"></param>
	    /// <param name="StartDate"></param>
	    /// <param name="EndDate"></param>
	    /// <param name="isActive"></param>
	    /// <param name="Description"></param>
	    /// <param name="Specifications"></param>
	    /// <param name="Priority"></param>
	    /// <param name="QcProfile"></param>
	    /// <param name="TargetItemCount"></param>
	    /// <param name="TargetPageCount"></param>
	    /// <param name="Comments"></param>
	    /// <param name="CopyrightPermissions"></param>
	    /// <returns>The ProjectID of the inserted/edited row</returns>
	    public static int Save_Project(Custom_Tracer Tracer, int ProjectID, string ProjectCode, string ProjectName, string ProjectManager, string GrantID, DateTime StartDate, DateTime EndDate, bool isActive, string Description, string Specifications, string Priority, string QcProfile, int TargetItemCount, int TargetPageCount, string Comments, string CopyrightPermissions)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Save_Project", "Saving to the database");
            }

            int newProjectID;


            // Build the parameters list
            List<EalDbParameter> parameters = new List<EalDbParameter>
            {
                new EalDbParameter("@ProjectID", ProjectID), 
                new EalDbParameter("@ProjectCode", ProjectCode), 
                new EalDbParameter("@ProjectName", ProjectName), 
                new EalDbParameter("@ProjectManager", ProjectManager), 
                new EalDbParameter("@GrantID", GrantID), 
                new EalDbParameter("@StartDate", StartDate), 
                new EalDbParameter("@EndDate", EndDate), 
                new EalDbParameter("@isActive", isActive), 
                new EalDbParameter("@Description", Description), 
                new EalDbParameter("@Specifications", Specifications), 
                new EalDbParameter("@Priority", Priority), 
                new EalDbParameter("@QC_Profile", QcProfile), 
                new EalDbParameter("@TargetItemCount", TargetItemCount), 
                new EalDbParameter("@TargetPageCount", TargetPageCount),
                new EalDbParameter("@Comments", Comments), 
                new EalDbParameter("@CopyrightPermissions", CopyrightPermissions)
            };

	        //Add the output parameter to get back the new ProjectID for this newly added project, or existing ProjectID if this has been updated
            EalDbParameter outputParam = new EalDbParameter("@New_ProjectID", SqlDbType.Int) {Direction = ParameterDirection.Output};
            parameters.Add(outputParam);
           
            try
            {
                // Create the database agnostic reader
                EalDbAccess.ExecuteNonQuery(DatabaseType, Connection_String, CommandType.StoredProcedure, "SobekCM_Save_Project", parameters);
                
                newProjectID = Convert.ToInt32(outputParam.Value);

            }
            catch (Exception ee)
            {
                throw new ApplicationException("Error saving this Project to the database. " + ee.Message);
            }

            return newProjectID;
        }

        /// <summary> Save the new Project-Aggregation link to the database. </summary>
        /// <param name="Tracer"></param>
        /// <param name="ProjectID"></param>
        /// <param name="AggregationID"></param>
        public static void Add_Project_Aggregation_Link(Custom_Tracer Tracer,int ProjectID, int AggregationID)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Add_Project_Aggregation_Link", "Saving link to the database");
            }

            try
            {
                // Build the parameters list
                List<EalDbParameter> parameters = new List<EalDbParameter>
                {
                    new EalDbParameter("@ProjectID", ProjectID), 
                    new EalDbParameter("@AggregationID", AggregationID)
                };

                // Run the SQL 
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Save_Project_Aggregation_Link", parameters ); 
            }
            catch (Exception ee)
            {
                throw new ApplicationException("Error adding Project-Aggregation link" + ee.Message);
            }
        }

        /// <summary> Save the Project_Default Metadata link to the database </summary>
        /// <param name="Tracer"></param>
        /// <param name="ProjectID"></param>
        /// <param name="DefaultMetadataID"></param>
        public static void Add_Project_DefaultMetadata_Link(Custom_Tracer Tracer, int ProjectID, int DefaultMetadataID)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Add_Project_DefaultMetadata_Link", "Saving link to the database");
            }

            try
            {
                // Build the parameters list
                List<EalDbParameter> parameters = new List<EalDbParameter>
                {
                    new EalDbParameter("@ProjectID", ProjectID), 
                    new EalDbParameter("@DefaultMetadataID", DefaultMetadataID)
                };

                // Run the SQL 
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Save_Project_DefaultMetadata_Link", parameters); 
            }
            catch (Exception ee)
            {
                throw new ApplicationException("Error adding Project-DefaultMetadata link" + ee.Message);
            }
        }

        /// <summary> Save Project, Input template link to the database </summary>
        /// <param name="Tracer"></param>
        /// <param name="ProjectID"></param>
        /// <param name="TemplateID"></param>
        public static void Add_Project_Template_Link(Custom_Tracer Tracer, int ProjectID, int TemplateID)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Add_Project_Template_Link", "Saving link to the database");
            }

            try
            {
                // Build the parameters list
                List<EalDbParameter> parameters = new List<EalDbParameter>
                {
                    new EalDbParameter("@ProjectID", ProjectID), 
                    new EalDbParameter("@TemplateID", TemplateID)
                };

                // Run the SQL 
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Save_Project_Template_Link", parameters); 
            }
            catch (Exception ee)
            {
                throw new ApplicationException("Error adding Project-TemplateID link" + ee.Message);
            }
        }

        /// <summary> Save Project, Item link to the database </summary>
        /// <param name="Tracer"></param>
        /// <param name="ProjectID"></param>
        /// <param name="ItemID"></param>
        public static void Add_Project_Item_Link(Custom_Tracer Tracer, int ProjectID, int ItemID)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Add_Project_Item_Link", "Saving link to the database");
            }

            try
            {
                // Build the parameters list
                List<EalDbParameter> parameters = new List<EalDbParameter>
                {
                    new EalDbParameter("@ProjectID", ProjectID), 
                    new EalDbParameter("@ItemID", ItemID)
                };

                // Run the SQL 
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Save_Project_Item_Link", parameters); 
            }
            catch (Exception ee)
            {
                throw new ApplicationException("Error adding Project-ItemID link" + ee.Message);
            }
        }

        /// <summary> Delete a Project, Item link from the database </summary>
        /// <param name="Tracer"></param>
        /// <param name="ProjectID"></param>
        /// <param name="ItemID"></param>
        public static void Delete_Project_Item_Link(Custom_Tracer Tracer, int ProjectID, int ItemID)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Delete_Project_Item_Link", "Deleting link from the database");
            }

            try
            {
                // Build the parameters list
                List<EalDbParameter> parameters = new List<EalDbParameter>
                {
                    new EalDbParameter("@ProjectID", ProjectID), 
                    new EalDbParameter("@ItemID", ItemID)
                };

                // Run the SQL 
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Delete_Project_Item_Link", parameters); 
            }
            catch (Exception ee)
            {
                throw new ApplicationException("Error deleting Project-ItemID link" + ee.Message);
            }
        }

        /// <summary> Delete a project, CompleteTemplate link from the database </summary>
        /// <param name="Tracer"></param>
        /// <param name="ProjectID"></param>
        /// <param name="TemplateID"></param>
        public static void Delete_Project_Template_Link(Custom_Tracer Tracer, int ProjectID, int TemplateID)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Delete_Project_Template_Link", "Deleting link from the database");
            }

            try
            {
                // Build the parameters list
                List<EalDbParameter> parameters = new List<EalDbParameter>
                {
                    new EalDbParameter("@ProjectID", ProjectID), 
                    new EalDbParameter("@TemplateID", TemplateID)
                };

                // Run the SQL 
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Delete_Project_Template_Link", parameters); 
            }
            catch (Exception ee)
            {
                throw new ApplicationException("Error deleting Project-CompleteTemplate link" + ee.Message);
            }
        }

        /// <summary> Delete the Project, default metadata link from the database </summary>
        /// <param name="Tracer"></param>
        /// <param name="ProjectID"></param>
        /// <param name="DefaultMetadataID"></param>
        public static void Delete_Project_DefaultMetadata_Link(Custom_Tracer Tracer, int ProjectID, int DefaultMetadataID)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Delete_Project_DefaultMetadata_Link", "Deleting link from the database");
            }

            try
            {
                // Build the parameters list
                List<EalDbParameter> parameters = new List<EalDbParameter>
                {
                    new EalDbParameter("@ProjectID", ProjectID), 
                    new EalDbParameter("@DefaultMetadataID", DefaultMetadataID)
                };

                // Run the SQL 
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Delete_Project_DefaultMetadata_Link", parameters); 
            }
            catch (Exception ee)
            {
                throw new ApplicationException("Error deleting Project-DefaultMetadata link" + ee.Message);
            }
        }

        /// <summary> Delete Project, Aggregation Link from the database </summary>
        /// <param name="Tracer"></param>
        /// <param name="ProjectID"></param>
        /// <param name="AggregationID"></param>
        public static void Delete_Project_Aggregation_Link(Custom_Tracer Tracer, int ProjectID, int AggregationID)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Delete_Project_Aggregation_Link", "Deleting link from the database");
            }

            try
            {
                // Build the parameters list
                List<EalDbParameter> parameters = new List<EalDbParameter>
                {
                    new EalDbParameter("@ProjectID", ProjectID), 
                    new EalDbParameter("@AggregationID", AggregationID)
                };

                // Run the SQL 
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Delete_Project_Aggregation_Link", parameters); 
            }
            catch (Exception ee)
            {
                throw new ApplicationException("Error deleting Project-Aggregation link" + ee.Message);
            }
        }

        /// <summary> Get the list of Aggregation IDs associated with a project </summary>
        /// <param name="Tracer"></param>
        /// <param name="ProjectID"></param>
        /// <returns></returns>
        public static List<int> Get_Aggregations_By_ProjectID(Custom_Tracer Tracer, int ProjectID)
        {
            if (Tracer != null)
			{
                Tracer.Add_Trace("SobekCM_Database.Get_Aggregations_By_ProjectID", "Pulling from database");
			}

			try
			{
				List<int> returnValue = new List<int>();

				// Define a temporary dataset
                DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Get_Aggregations_By_ProjectID");

				// If there was no data for this collection and entry point, return null (an ERROR occurred)
				if ((tempSet.Tables.Count == 0) || (tempSet.Tables[0] == null) || (tempSet.Tables[0].Rows.Count == 0))
				{
					return returnValue;
				}

				// Return the first table from the returned dataset
				foreach (DataRow thisRow in tempSet.Tables[0].Rows)
				{
					returnValue.Add(Convert.ToInt32(thisRow[0]));
				}
				return returnValue;
			}
			catch (Exception ee)
			{
				lastException = ee;
				if (Tracer != null)
				{
                    Tracer.Add_Trace("SobekCM_Database.Get_Aggregations_By_ProjectID", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_Aggregations_By_ProjectID", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_Aggregations_By_ProjectID", ee.StackTrace, Custom_Trace_Type_Enum.Error);
				}
				return null;
			}
	}

        //TODO: Add methods to get the default metadata, current input template
        //TODO: Add the method to get the list of active, inactive projects

        
        #endregion

        #region Methods to support the top-level user permissions reports

        /// <summary> Get the list of users that have top-level permissions, such as editing all items, 
        /// being an admin, deleting all items, or a power user  </summary>
        /// <param name="Tracer"></param>
        /// <returns></returns>
	    public static DataTable Get_Global_User_Permissions(Custom_Tracer Tracer )
	    {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions", "");
            }

            try
            {
                // Define a temporary dataset
                DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, Connection_String, CommandType.StoredProcedure, "mySobek_Permissions_Report");

                // Return the first table from the returned dataset
                return tempSet.Tables[0];
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return null;
            }
	    }

        /// <summary> Get the list of users and for each user the list of aggregations they 
        /// have special rights over (wither by user or through user group ) </summary>
        /// <param name="Tracer"></param>
        /// <returns></returns>
        public static DataTable Get_Global_User_Permissions_Aggregations_Links(Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions_Aggregations_Links", "");
            }

            try
            {
                // Define a temporary dataset
                DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, Connection_String, CommandType.StoredProcedure, "mySobek_Permissions_Report_Aggregation_Links");

                // Return the first table from the returned dataset
                return tempSet.Tables[0];
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions_Aggregations_Links", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions_Aggregations_Links", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions_Aggregations_Links", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return null;
            }
        }

        /// <summary> Get the list of aggregations that have special rights given to some users </summary>
        /// <param name="Tracer"></param>
        /// <returns></returns>
        public static DataTable Get_Global_User_Permissions_Linked_Aggregations(Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions_Linked_Aggregations", "");
            }

            try
            {
                // Define a temporary dataset
                DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, Connection_String, CommandType.StoredProcedure, "mySobek_Permissions_Report_Linked_Aggregations");

                // Return the first table from the returned dataset
                return tempSet.Tables[0];
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions_Linked_Aggregations", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions_Linked_Aggregations", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions_Linked_Aggregations", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return null;
            }
        }

        /// <summary> Get the list of users, with informaiton about the templates and default metadata, 
        /// that can submit material to this instance  </summary>
        /// <param name="Tracer"></param>
        /// <returns></returns>
        public static DataTable Get_Global_User_Permissions_Submission_Rights(Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions_Submission_Rights", "");
            }

            try
            {
                // Define a temporary dataset
                DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, Connection_String, CommandType.StoredProcedure, "mySobek_Permissions_Report_Submission_Rights");

                // Return the first table from the returned dataset
                return tempSet.Tables[0];
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions_Submission_Rights", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions_Submission_Rights", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_Global_User_Permissions_Submission_Rights", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return null;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="AggregationCode"></param>
        /// <param name="Tracer"></param>
        /// <returns></returns>
        public static DataTable Get_Aggregation_User_Permissions(string AggregationCode, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Get_Aggregation_User_Permissions", "");
            }

            try
            {
                EalDbParameter[] parameters = new EalDbParameter[1];
                parameters[0] = new EalDbParameter("@Code", AggregationCode);

                // Define a temporary dataset
                DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, Connection_String, CommandType.StoredProcedure, "mySobek_Permissions_Report_Aggregation", parameters);

                if ((tempSet == null) || (tempSet.Tables.Count == 0))
                    return null;

                // Return the first table from the returned dataset
                return tempSet.Tables[0];
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Get_Aggregation_User_Permissions", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_Aggregation_User_Permissions", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_Aggregation_User_Permissions", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="AggregationCode"></param>
        /// <param name="Tracer"></param>
        /// <returns></returns>
        public static DataTable Get_Aggregation_Change_Log(string AggregationCode, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Get_Aggregation_Change_Log", "");
            }

            try
            {
                EalDbParameter[] parameters = new EalDbParameter[1];
                parameters[0] = new EalDbParameter("@Code", AggregationCode);

                // Define a temporary dataset
                DataSet tempSet = EalDbAccess.ExecuteDataset(DatabaseType, Connection_String, CommandType.StoredProcedure, "SobekCM_Aggregation_Change_Log", parameters);

                if ((tempSet == null) || (tempSet.Tables.Count == 0))
                    return null;

                // Return the first table from the returned dataset
                return tempSet.Tables[0];
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Get_Aggregation_Change_Log", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_Aggregation_Change_Log", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Get_Aggregation_Change_Log", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return null;
            }
        }

        #endregion

        #region Methods used to edit or delete incoming builder folders

	    /// <summary> Deletes an existing builder incoming folder from the table </summary>
	    /// <param name="FolderID"> Primary key for the builder incoming folder to delete </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
	    /// <returns> TRUE if successful, otherwise FALSE </returns>
	    /// <remarks> This calls the 'SobekCM_Builder_Incoming_Folder_Delete' stored procedure </remarks> 
	    public static bool Builder_Folder_Delete(int FolderID, Custom_Tracer Tracer )
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Builder_Folder_Delete", String.Empty);
            }

            try
            {
                // Build the parameter list
                EalDbParameter[] paramList = new EalDbParameter[1];
                paramList[0] = new EalDbParameter("@IncomingFolderId", FolderID);

                // Execute this query stored procedure
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Builder_Incoming_Folder_Delete", paramList);

                // Succesful, so return true
                return true;
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Builder_Folder_Delete", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Builder_Folder_Delete", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Builder_Folder_Delete", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return false;
            }
        }

	    /// <summary> Edits an existing builder incoming folder or adds a new folder </summary>
	    /// <param name="FolderID"> Primary key for the builder incoming folder to delete </param>
	    /// <param name="Folder_Name"></param>
	    /// <param name="Network_Folder"></param>
	    /// <param name="Error_Folder"></param>
	    /// <param name="Processing_Folder"></param>
	    /// <param name="Perform_Checksum"></param>
	    /// <param name="Archive_TIFF"></param>
	    /// <param name="Archive_All_Files"></param>
	    /// <param name="Allow_Deletes"></param>
	    /// <param name="Allow_Folders_No_Metadata"></param>
	    /// <param name="BibID_Roots_Restrictions"></param>
	    /// <param name="ModuleSetID"></param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
	    /// <returns> TRUE if successful, otherwise FALSE </returns>
	    /// <remarks> This calls the 'SobekCM_Builder_Incoming_Folder_Edit' stored procedure </remarks> 
        public static bool Builder_Folder_Edit(int FolderID, string Folder_Name, string Network_Folder, string Error_Folder, string Processing_Folder,
            bool Perform_Checksum, bool Archive_TIFF, bool Archive_All_Files, bool Allow_Deletes, bool Allow_Folders_No_Metadata,
            string BibID_Roots_Restrictions, int ModuleSetID, Custom_Tracer Tracer )
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Database.Builder_Folder_Edit", String.Empty);
            }

            try
            {
                // Build the parameter list
                EalDbParameter[] paramList = new EalDbParameter[12];
                paramList[0] = new EalDbParameter("@IncomingFolderId", FolderID);
                paramList[1] = new EalDbParameter("@NetworkFolder", Network_Folder);
                paramList[2] = new EalDbParameter("@ErrorFolder", Error_Folder);
                paramList[3] = new EalDbParameter("@ProcessingFolder", Processing_Folder);
                paramList[4] = new EalDbParameter("@Perform_Checksum_Validation", Perform_Checksum);
                paramList[5] = new EalDbParameter("@Archive_TIFF", Archive_TIFF);
                paramList[6] = new EalDbParameter("@Archive_All_Files", Archive_All_Files);
                paramList[7] = new EalDbParameter("@Allow_Deletes", Allow_Deletes);
                paramList[8] = new EalDbParameter("@Allow_Folders_No_Metadata", Allow_Folders_No_Metadata);
                paramList[9] = new EalDbParameter("@FolderName", Folder_Name);
                paramList[10] = new EalDbParameter("@BibID_Roots_Restrictions", BibID_Roots_Restrictions ?? String.Empty );
                paramList[11] = new EalDbParameter("@ModuleSetID", ( ModuleSetID  > 0 ) ? ModuleSetID : 10);

                // Execute this query stored procedure
                EalDbAccess.ExecuteNonQuery(DatabaseType, connectionString, CommandType.StoredProcedure, "SobekCM_Builder_Incoming_Folder_Edit", paramList);

                // Succesful, so return true
                return true;
            }
            catch (Exception ee)
            {
                lastException = ee;
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Database.Builder_Folder_Edit", "Exception caught during database work", Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Builder_Folder_Edit", ee.Message, Custom_Trace_Type_Enum.Error);
                    Tracer.Add_Trace("SobekCM_Database.Builder_Folder_Edit", ee.StackTrace, Custom_Trace_Type_Enum.Error);
                }
                return false;
            }
        }

        #endregion

    }

}
