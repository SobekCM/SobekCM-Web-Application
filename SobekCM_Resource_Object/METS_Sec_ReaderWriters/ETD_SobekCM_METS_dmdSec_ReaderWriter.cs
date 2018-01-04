﻿#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SobekCM.Resource_Object.Metadata_Modules;

#endregion

namespace SobekCM.Resource_Object.METS_Sec_ReaderWriters
{
	/// <summary> Class is used to read and write Electronic Theses and Dissertations (ETD) metadata
	/// in the SobekCM schema </summary>
	/// <remarks> I would really like to find a better (inter)national standard, but none seem to contain the 
	/// details that we utilize here. (Mark) </remarks>
	public class ETD_SobekCM_METS_dmdSec_ReaderWriter : XML_Writing_Base_Type, iPackage_dmdSec_ReaderWriter
	{
		#region iPackage_dmdSec_ReaderWriter Members

		/// <summary> Flag indicates if this active reader/writer will write a dmdSec </summary>
		/// <param name="METS_Item"> Package with all the metadata to save</param>
		/// <param name="Options"> Dictionary of any options which this METS section writer may utilize</param>
		/// <returns> TRUE if the package has data to be written, otherwise fALSE </returns>
		public bool Include_dmdSec(SobekCM_Item METS_Item, Dictionary<string, object> Options)
		{
			// Ensure this metadata module extension exists and has data
			Thesis_Dissertation_Info thesisInfo = METS_Item.Get_Metadata_Module(GlobalVar.THESIS_METADATA_MODULE_KEY) as Thesis_Dissertation_Info;
			if ((thesisInfo == null) || (!thesisInfo.hasData))
				return false;
			return true;
		}

		/// <summary> Writes the dmdSec for the entire package to the text writer </summary>
		/// <param name="Output_Stream">Stream to which the formatted text is written </param>
		/// <param name="METS_Item">Package with all the metadata to save</param>
		/// <param name="Options"> Dictionary of any options which this METS section writer may utilize</param>
		/// <returns>TRUE if successful, otherwise FALSE </returns>
		public virtual bool Write_dmdSec(TextWriter Output_Stream, SobekCM_Item METS_Item, Dictionary<string, object> Options)
		{
			// Ensure this metadata module extension exists and has data
			Thesis_Dissertation_Info thesisInfo = METS_Item.Get_Metadata_Module(GlobalVar.THESIS_METADATA_MODULE_KEY) as Thesis_Dissertation_Info;
			if ((thesisInfo == null) || (!thesisInfo.hasData))
				return true;
			
	 		Output_Stream.WriteLine("<etd:etd>");
			if (!String.IsNullOrEmpty(thesisInfo.Committee_Chair))
				Output_Stream.WriteLine("<etd:committeeChair>" + Convert_String_To_XML_Safe(thesisInfo.Committee_Chair) + "</etd:committeeChair>");
			if (!String.IsNullOrEmpty(thesisInfo.Committee_Co_Chair))
				Output_Stream.WriteLine("<etd:committeeCoChair>" + Convert_String_To_XML_Safe(thesisInfo.Committee_Co_Chair) + "</etd:committeeCoChair>");
			if (thesisInfo.Committee_Members_Count > 0)
			{
				foreach (string thisCommitteeMember in thesisInfo.Committee_Members)
				{
					Output_Stream.WriteLine("<etd:committeeMember>" + Convert_String_To_XML_Safe(thisCommitteeMember) + "</etd:committeeMember>");
				}
			}
			if (thesisInfo.Graduation_Date.HasValue)
			{
				string encoded_date = thesisInfo.Graduation_Date.Value.Year + "-" + thesisInfo.Graduation_Date.Value.Month.ToString().PadLeft(2, '0') + "-" + thesisInfo.Graduation_Date.Value.Day.ToString().PadLeft(2, '0');
				Output_Stream.WriteLine("<etd:graduationDate>" + encoded_date + "</etd:graduationDate>");
			}
			if (!String.IsNullOrEmpty(thesisInfo.Degree))
				Output_Stream.WriteLine("<etd:degree>" + Convert_String_To_XML_Safe(thesisInfo.Degree) + "</etd:degree>");

			if (thesisInfo.Degree_Disciplines_Count > 0)
			{
				foreach (string thisDiscipline in thesisInfo.Degree_Disciplines)
				{
					Output_Stream.WriteLine("<etd:degreeDiscipline>" + Convert_String_To_XML_Safe(thisDiscipline) + "</etd:degreeDiscipline>");
				}
			}

			if (thesisInfo.Degree_Divisions_Count > 0)
			{
				foreach (string thisDivision in thesisInfo.Degree_Divisions)
				{
					Output_Stream.WriteLine("<etd:degreeDivision>" + Convert_String_To_XML_Safe(thisDivision) + "</etd:degreeDivision>");
				}
			}

			if (!String.IsNullOrEmpty(thesisInfo.Degree_Grantor))
				Output_Stream.WriteLine("<etd:degreeGrantor>" + Convert_String_To_XML_Safe(thesisInfo.Degree_Grantor) + "</etd:degreeGrantor>");

		    switch (thesisInfo.Degree_Level)
		    {
		        case Thesis_Dissertation_Info.Thesis_Degree_Level_Enum.Bachelors:
                    Output_Stream.WriteLine("<etd:degreeLevel>Bachelors</etd:degreeLevel>");
		            break;

                case Thesis_Dissertation_Info.Thesis_Degree_Level_Enum.Masters:
                    Output_Stream.WriteLine("<etd:degreeLevel>Masters</etd:degreeLevel>");
                    break;

                case Thesis_Dissertation_Info.Thesis_Degree_Level_Enum.Doctorate:
                    Output_Stream.WriteLine("<etd:degreeLevel>Doctorate</etd:degreeLevel>");
                    break;

                case Thesis_Dissertation_Info.Thesis_Degree_Level_Enum.PostDoctorate:
                    Output_Stream.WriteLine("<etd:degreeLevel>Post-Doctorate</etd:degreeLevel>");
                    break;
		    }

            if (!String.IsNullOrEmpty(thesisInfo.Graduation_Semester))
                Output_Stream.WriteLine("<etd:semester>" + Convert_String_To_XML_Safe(thesisInfo.Graduation_Semester) + "</etd:semester>");

			Output_Stream.WriteLine("</etd:etd>");
			return true;
		}

		/// <summary> Reads the dmdSec at the current position in the XmlTextReader and associates it with the 
		/// entire package  </summary>
		/// <param name="Input_XmlReader"> Open XmlReader from which to read the metadata </param>
		/// <param name="Return_Package"> Package into which to read the metadata</param>
		/// <param name="Options"> Dictionary of any options which this METS section reader may utilize</param>
		/// <returns> TRUE if successful, otherwise FALSE</returns>
		public bool Read_dmdSec(XmlReader Input_XmlReader, SobekCM_Item Return_Package, Dictionary<string, object> Options)
		{
			// Ensure this metadata module extension exists
			Thesis_Dissertation_Info thesisInfo = Return_Package.Get_Metadata_Module(GlobalVar.THESIS_METADATA_MODULE_KEY) as Thesis_Dissertation_Info;
			if (thesisInfo == null)
			{
				thesisInfo = new Thesis_Dissertation_Info();
				Return_Package.Add_Metadata_Module(GlobalVar.THESIS_METADATA_MODULE_KEY, thesisInfo);
			}

			// Loop through reading each XML node
			do
			{
				// If this is the end of this section, return
				if ((Input_XmlReader.NodeType == XmlNodeType.EndElement) && ((Input_XmlReader.Name == "METS:mdWrap") || (Input_XmlReader.Name == "mdWrap")))
					return true;

				// get the right division information based on node type
				if (Input_XmlReader.NodeType == XmlNodeType.Element)
				{
					string name = Input_XmlReader.Name.ToLower();
					if (name.IndexOf("palmm:") == 0)
						name = name.Substring(6);
					if (name.IndexOf("etd:") == 0)
						name = name.Substring(4);

					switch (name)
					{
						case "committeechair":
							Input_XmlReader.Read();
							if ((Input_XmlReader.NodeType == XmlNodeType.Text) && (Input_XmlReader.Value.Trim().Length > 0))
							{
								thesisInfo.Committee_Chair = Input_XmlReader.Value.Trim();
							}
							break;

						case "committeecochair":
							Input_XmlReader.Read();
							if ((Input_XmlReader.NodeType == XmlNodeType.Text) && (Input_XmlReader.Value.Trim().Length > 0))
							{
								thesisInfo.Committee_Co_Chair = Input_XmlReader.Value.Trim();
							}
							break;

						case "committeemember":
							Input_XmlReader.Read();
							if ((Input_XmlReader.NodeType == XmlNodeType.Text) && (Input_XmlReader.Value.Trim().Length > 0))
							{
								thesisInfo.Add_Committee_Member(Input_XmlReader.Value);
							}
							break;

						case "graduationdate":
							Input_XmlReader.Read();
							if ((Input_XmlReader.NodeType == XmlNodeType.Text) && (Input_XmlReader.Value.Trim().Length > 0))
							{
								DateTime convertedDate;
								if (DateTime.TryParse(Input_XmlReader.Value, out convertedDate))
								{
									thesisInfo.Graduation_Date = convertedDate;
								}
							}
							break;

						case "degree":
							Input_XmlReader.Read();
							if ((Input_XmlReader.NodeType == XmlNodeType.Text) && (Input_XmlReader.Value.Trim().Length > 0))
							{
								thesisInfo.Degree = Input_XmlReader.Value;
							}
							break;

						case "degreediscipline":
							Input_XmlReader.Read();
							if ((Input_XmlReader.NodeType == XmlNodeType.Text) && (Input_XmlReader.Value.Trim().Length > 0))
							{
								string degreeDiscipline = Input_XmlReader.Value;
								if (degreeDiscipline.IndexOf(";") > 0)
								{
									string[] splitter = degreeDiscipline.Split(";".ToCharArray());
									foreach (string thisSplit in splitter)
										thesisInfo.Add_Degree_Discipline(thisSplit);
								}
								else
								{
									thesisInfo.Add_Degree_Discipline(degreeDiscipline);
								}
							}
							break;

						case "degreedivision":
							Input_XmlReader.Read();
							if ((Input_XmlReader.NodeType == XmlNodeType.Text) && (Input_XmlReader.Value.Trim().Length > 0))
							{
								string degreeDivision = Input_XmlReader.Value;
								if (degreeDivision.IndexOf(";") > 0)
								{
									string[] splitter = degreeDivision.Split(";".ToCharArray());
									foreach (string thisSplit in splitter)
										thesisInfo.Add_Degree_Division(thisSplit);
								}
								else
								{
									thesisInfo.Add_Degree_Division(degreeDivision);
								}
							}
							break;


						case "degreegrantor":
							Input_XmlReader.Read();
							if ((Input_XmlReader.NodeType == XmlNodeType.Text) && (Input_XmlReader.Value.Trim().Length > 0))
							{
								thesisInfo.Degree_Grantor = Input_XmlReader.Value;
							}
							break;

						case "degreelevel":
							Input_XmlReader.Read();
							if ((Input_XmlReader.NodeType == XmlNodeType.Text) && (Input_XmlReader.Value.Trim().Length > 0))
							{
								string temp = Input_XmlReader.Value.ToLower();
								if ((temp == "doctorate") || (temp == "doctoral"))
									thesisInfo.Degree_Level = Thesis_Dissertation_Info.Thesis_Degree_Level_Enum.Doctorate;
								if ((temp == "masters") || (temp == "master's"))
									thesisInfo.Degree_Level = Thesis_Dissertation_Info.Thesis_Degree_Level_Enum.Masters;
								if ((temp == "bachelors") || ( temp ==  "bachelor's"))
									thesisInfo.Degree_Level = Thesis_Dissertation_Info.Thesis_Degree_Level_Enum.Bachelors;
								if ((temp == "post-doctorate") || ( temp ==  "post-doctoral"))
									thesisInfo.Degree_Level = Thesis_Dissertation_Info.Thesis_Degree_Level_Enum.Bachelors;

							}
							break;

                        case "semester":
                            Input_XmlReader.Read();
                            if ((Input_XmlReader.NodeType == XmlNodeType.Text) && (Input_XmlReader.Value.Trim().Length > 0))
                            {
                                thesisInfo.Graduation_Semester = Input_XmlReader.Value;
                            }
                            break;
					}
				}
			} while (Input_XmlReader.Read());

			return true;
		}

		/// <summary> Flag indicates if this active reader/writer needs to append schema reference information
		/// to the METS XML header by analyzing the contents of the digital resource item </summary>
		/// <param name="METS_Item"> Package with all the metadata to save</param>
		/// <returns> TRUE if the schema should be attached, otherwise fALSE </returns>
		public bool Schema_Reference_Required_Package(SobekCM_Item METS_Item)
		{
			Thesis_Dissertation_Info thesisInfo = METS_Item.Get_Metadata_Module(GlobalVar.THESIS_METADATA_MODULE_KEY) as Thesis_Dissertation_Info;
			if (thesisInfo == null)
				return false;

			return thesisInfo.hasData;
		}

		/// <summary> Returns the schema namespace (xmlns) information to be written in the XML/METS Header</summary>
		/// <param name="METS_Item"> Package with all the metadata to save</param>
		/// <returns> Formatted schema namespace info for the METS header</returns>
		public virtual string[] Schema_Namespace(SobekCM_Item METS_Item)
		{
			return new string[] { "etd=\"http://sobekrepository.org/schemas/sobekcm_etd/\"" };
		}

		/// <summary> Returns the schema location information to be written in the XML/METS Header</summary>
		/// <param name="METS_Item"> Package with all the metadata to save</param>
		/// <returns> Formatted schema location for the METS header</returns>
		public virtual string[] Schema_Location(SobekCM_Item METS_Item)
		{
			return new string[] { "    http://sobekrepository.org/schemas/sobekcm_etd/\r\n    http://sobekrepository.org/schemas/sobekcm_etd.xsd" };
		}

		#endregion
	}
}