﻿#region Using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using SobekCM.Core.Aggregations;
using SobekCM.Core.BriefItem;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.UI_Configuration.Citation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Email;
using SobekCM.Engine_Library.Items.BriefItems;
using SobekCM.Library.AdminViewer;
using SobekCM.Library.Citation;
using SobekCM.Library.Citation.SectionWriter;
using SobekCM.Library.Citation.Template;
using SobekCM.Library.Database;
using SobekCM.Library.Helpers.UploadiFive;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Bib_Info;
using SobekCM.Resource_Object.Divisions;
using SobekCM.Resource_Object.GenericXml.Reader;
using SobekCM.Resource_Object.GenericXml.Results;
using SobekCM.Resource_Object.Mapping;
using SobekCM.Resource_Object.Metadata_File_ReaderWriters;
using SobekCM.Resource_Object.Utilities;
using SobekCM.Tools;
using SobekCM_Resource_Database;
using Image = System.Drawing.Image;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> TEI MySobek viewer is used to submit a new TEI type digital resource to the SobekCM repository </summary>
    public class New_TEI_MySobekViewer : abstract_MySobekViewer
    {
        private bool criticalErrorEncountered;
        private readonly int currentProcessStep;
        private SobekCM_Item item;
        private readonly CompleteTemplate completeTemplate;
        private readonly string templateCode = "ir";
        private readonly string toolTitle;
        private readonly int totalTemplatePages;
        private readonly string userInProcessDirectory;
        private readonly List<string> validationErrors;

        private readonly string tei_file;
        private readonly string mapping_file;
        private readonly string xslt_file;
        private readonly string css_file;

        private string error_message;



        #region Constructor

        /// <summary> Constructor for a new instance of the New_TEI_MySobekViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public New_TEI_MySobekViewer(RequestCache RequestSpecificValues) : base(RequestSpecificValues)
        {
            RequestSpecificValues.Tracer.Add_Trace("New_TEI_MySobekViewer.Constructor", String.Empty);

            // If the RequestSpecificValues.Current_User cannot submit items, go back
            if (!RequestSpecificValues.Current_User.Can_Submit)
            {
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }

            // Ensure the TEI plug-in is enabled
            if ((UI_ApplicationCache_Gateway.Configuration.Extensions == null) ||
                (UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Extension("TEI") == null) ||
                (!UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Extension("TEI").Enabled))
            {
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }
            
            // Ensure this user is enabled to add TEI 
            string user_tei_enabled = RequestSpecificValues.Current_User.Get_Setting("TEI.Enabled", "false");
            if (String.Compare(user_tei_enabled, "true", StringComparison.OrdinalIgnoreCase) != 0)
            {
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }

            // Determine the in process directory for this
            if (RequestSpecificValues.Current_User.ShibbID.Trim().Length > 0)
                userInProcessDirectory = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.In_Process_Submission_Location, RequestSpecificValues.Current_User.ShibbID + "\\tei");
            else
                userInProcessDirectory = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.In_Process_Submission_Location, RequestSpecificValues.Current_User.UserName.Replace(".", "").Replace("@", "") + "\\tei");

            // Load the CompleteTemplate
            completeTemplate = Template_MemoryMgmt_Utility.Retrieve_Template("tei", RequestSpecificValues.Tracer);
            if (completeTemplate != null)
            {
                RequestSpecificValues.Tracer.Add_Trace("New_TEI_MySobekViewer.Constructor", "Found template in cache");
            }
            else
            {
                RequestSpecificValues.Tracer.Add_Trace("New_TEI_MySobekViewer.Constructor", "Reading template");

                string user_template = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network, "plugins\\tei\\templates\\user\\template.xml");
                if (!File.Exists(user_template))
                    user_template = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network, "plugins\\tei\\templates\\default\\template.xml");


                // Read this CompleteTemplate
                Template_XML_Reader reader = new Template_XML_Reader();
                completeTemplate = new CompleteTemplate();
                reader.Read_XML(user_template, completeTemplate, true);

                // Save this into the cache
                Template_MemoryMgmt_Utility.Store_Template("tei", completeTemplate, RequestSpecificValues.Tracer);
            }

            // Determine the number of total CompleteTemplate pages
            totalTemplatePages = completeTemplate.InputPages_Count + 3;
            if (completeTemplate.Permissions_Agreement.Length > 0)
                totalTemplatePages++;
            if (completeTemplate.Upload_Types != CompleteTemplate.Template_Upload_Types.None)
                totalTemplatePages++;

            // Determine the title for this CompleteTemplate, or use a default
            toolTitle = completeTemplate.Title;
            if (toolTitle.Length == 0)
                toolTitle = "TEI Submission Tool";

            // Determine the current phase
            currentProcessStep = 1;
            if ((RequestSpecificValues.Current_Mode.My_Sobek_SubMode.Length > 0) && (Char.IsNumber(RequestSpecificValues.Current_Mode.My_Sobek_SubMode[0])))
            {
                Int32.TryParse(RequestSpecificValues.Current_Mode.My_Sobek_SubMode.Substring(0), out currentProcessStep);
            }

            // Load some information from the session
            if ( HttpContext.Current.Session["New_TEI_mySobekViewer.Mapping_File"] != null )
                mapping_file = HttpContext.Current.Session["New_TEI_mySobekViewer.Mapping_File"] as string;
            if ( HttpContext.Current.Session["New_TEI_mySobekViewer.XSLT_File"] != null )
                xslt_file = HttpContext.Current.Session["New_TEI_mySobekViewer.XSLT_File"] as string;
            if ( HttpContext.Current.Session["New_TEI_mySobekViewer.CSS_File"] != null )
                css_file = HttpContext.Current.Session["New_TEI_mySobekViewer.CSS_File"] as string;


            // If this is process step 1 and there is no permissions statement in the CompleteTemplate,
            // just go to step 2
            if ((currentProcessStep == 1) && (completeTemplate.Permissions_Agreement.Length == 0))
            {
                // Delete any pre-existing agreement from an earlier aborted submission process
                if (File.Exists(userInProcessDirectory + "\\agreement.txt"))
                    File.Delete(userInProcessDirectory + "\\agreement.txt");

                // Skip the permissions step
                currentProcessStep = 2;
            }

            // If there is a boundary infraction here, go back to step 2
            if (currentProcessStep < 0)
                currentProcessStep = 2;
            if ((currentProcessStep > completeTemplate.InputPages.Count + 4) && (currentProcessStep != 8) && (currentProcessStep != 9))
                currentProcessStep = 2;

            // Look for the item in the session, then directory, then just create a new one
            if (HttpContext.Current.Session["TEI_Item"] == null)
            {
                // Clear any old files (older than 24 hours) that are in the directory
                if (!Directory.Exists(userInProcessDirectory))
                    Directory.CreateDirectory(userInProcessDirectory);
                else
                {
                    // Anything older than a day should be deleted
                    string[] files = Directory.GetFiles(userInProcessDirectory);
                    foreach (string thisFile in files)
                    {
                        DateTime modifiedDate = ((new FileInfo(thisFile)).LastWriteTime);
                        if (DateTime.Now.Subtract(modifiedDate).TotalHours > (24 * 7))
                        {
                            try
                            {
                                File.Delete(thisFile);
                            }
                            catch (Exception)
                            {
                                // Unable to delete existing file in the RequestSpecificValues.Current_User's folder.
                                // This is an error, but how to report it?
                            }
                        }
                    }
                }

                // First, look for an existing METS file
                string[] existing_mets = Directory.GetFiles(userInProcessDirectory, "*.mets*");
                if (existing_mets.Length > 0)
                {
                    RequestSpecificValues.Tracer.Add_Trace("New_TEI_MySobekViewer.Constructor", "Reading existing METS file<br />(" + existing_mets[0] + ")");
                    item = SobekCM_Item.Read_METS(existing_mets[0]);

                    // Set the visibility information from the CompleteTemplate
                    item.Behaviors.IP_Restriction_Membership = completeTemplate.Default_Visibility;
                }

                // If there is still no item, just create a new one
                if (item == null)
                {
                    // Build a new empty METS file
                    new_item(RequestSpecificValues.Tracer);
                }

                // Save this to the session state now
                HttpContext.Current.Session["Item"] = item;
            }
            else
            {
                RequestSpecificValues.Tracer.Add_Trace("New_TEI_MySobekViewer.Constructor", "Item found in session cache");
                item = (SobekCM_Item)HttpContext.Current.Session["Item"];
            }

            // Find the TEI file
            if (Directory.Exists(userInProcessDirectory))
            {
                string[] tei_files = Directory.GetFiles(userInProcessDirectory, "*.xml");
                if (tei_files.Length > 1)
                {
                    // Two XML files, so delete all but the latest
                    string latest_tei_file = String.Empty;
                    DateTime latest_timestamp = DateTime.MinValue;

                    // Find the latest TEI file
                    foreach (string thisTeiFile in tei_files)
                    {
                        // If this is marc.xml, skip it
                        if ((Path.GetFileName(thisTeiFile).ToLower().IndexOf("marc.xml") >= 0) || (Path.GetFileName(thisTeiFile).ToLower().IndexOf("mets.xml") >= 0))
                            continue;

                        DateTime file_timestamp = File.GetLastWriteTime(thisTeiFile);

                        if (DateTime.Compare(latest_timestamp, file_timestamp) < 0)
                        {
                            latest_tei_file = thisTeiFile;
                            latest_timestamp = file_timestamp;
                        }
                    }

                    // If a latest file as found, delete the others
                    if (!String.IsNullOrEmpty(latest_tei_file))
                    {
                        foreach (string thisTeiFile in tei_files)
                        {
                            // If this is marc.xml, skip it
                            if ((Path.GetFileName(thisTeiFile).ToLower().IndexOf("marc.xml") >= 0) || (Path.GetFileName(thisTeiFile).ToLower().IndexOf("mets.xml") >= 0))
                                continue;

                            // Was this the latest file?
                            if (String.Compare(thisTeiFile, latest_tei_file, StringComparison.OrdinalIgnoreCase) == 0)
                                continue;

                            try
                            {
                                File.Delete(thisTeiFile);
                            }
                            catch { }

                        }
                    }

                    tei_file = latest_tei_file;
                }
                else if (tei_files.Length == 1)
                {
                    tei_file = Path.GetFileName(tei_files[0]);
                }
            }

            #region Special code to handle any uploaded files

            // Any post-processing to do?
            if ((currentProcessStep == 8) && (Directory.Exists(userInProcessDirectory)))
            {
                string[] processFiles = Directory.GetFiles(userInProcessDirectory);
                foreach (string thisFile in processFiles)
                {
                    FileInfo thisFileInfo = new FileInfo(thisFile);
                    if ((thisFileInfo.Extension.ToUpper() == ".TIF") || (thisFileInfo.Extension.ToUpper() == ".TIFF"))
                    {
                        // Is there a JPEG and/or thumbnail?
                        string jpeg = userInProcessDirectory + "\\" + thisFileInfo.Name.Replace(thisFileInfo.Extension, "") + ".jpg";
                        string jpeg_thumbnail = userInProcessDirectory + "\\" + thisFileInfo.Name.Replace(thisFileInfo.Extension, "") + "thm.jpg";

                        // Is one missing?
                        if ((!File.Exists(jpeg)) || (!File.Exists(jpeg_thumbnail)))
                        {
                            using (Image tiffImg = Image.FromFile(thisFile))
                            {
                                try
                                {
                                    var mainImg = ScaleImage(tiffImg, UI_ApplicationCache_Gateway.Settings.Resources.JPEG_Width, UI_ApplicationCache_Gateway.Settings.Resources.JPEG_Height);
                                    mainImg.Save(jpeg, ImageFormat.Jpeg);
                                    mainImg.Dispose();
                                    var thumbnailImg = ScaleImage(tiffImg, 150, 400);
                                    thumbnailImg.Save(jpeg_thumbnail, ImageFormat.Jpeg);
                                    thumbnailImg.Dispose();
                                }
                                catch
                                {

                                }
                                finally
                                {
                                    if (tiffImg != null)
                                        tiffImg.Dispose();
                                }
                            }

                        }
                    }
                }
            }

            #endregion

            #region Handle any other post back requests

            // If this is post-back, handle it
            if (RequestSpecificValues.Current_Mode.isPostBack)
            {
                // If this is a request from stage 8, save the new labels and url first
                if (currentProcessStep == 8)
                {
                    string[] getKeys = HttpContext.Current.Request.Form.AllKeys;
                    string file_name_from_keys = String.Empty;
                    string label_from_keys = String.Empty;
                    foreach (string thisKey in getKeys)
                    {
                        if (thisKey.IndexOf("upload_file") == 0)
                        {
                            file_name_from_keys = HttpContext.Current.Request.Form[thisKey];
                        }
                        if (thisKey.IndexOf("upload_label") == 0)
                        {
                            label_from_keys = HttpContext.Current.Request.Form[thisKey];
                        }
                        if ((file_name_from_keys.Length > 0) && (label_from_keys.Length > 0))
                        {
                            HttpContext.Current.Session["file_" + file_name_from_keys.Trim()] = label_from_keys.Trim();
                            file_name_from_keys = String.Empty;
                            label_from_keys = String.Empty;
                        }

                        if (thisKey == "url_input")
                        {
                            item.Bib_Info.Location.Other_URL = HttpContext.Current.Request.Form[thisKey];
                        }
                    }
                }

                // Was this where the mapping, xslt, and css is set?
                if (currentProcessStep == 3)
                {
                    string[] getKeys = HttpContext.Current.Request.Form.AllKeys;
                    foreach (string thisKey in getKeys)
                    {
                        if (thisKey.IndexOf("mapping_select") == 0)
                        {
                            mapping_file = HttpContext.Current.Request.Form[thisKey];
                            HttpContext.Current.Session["New_TEI_mySobekViewer.Mapping_File"] = mapping_file;
                        }
                        if (thisKey.IndexOf("xslt_select") == 0)
                        {
                            xslt_file = HttpContext.Current.Request.Form[thisKey];
                            HttpContext.Current.Session["New_TEI_mySobekViewer.XSLT_File"] = xslt_file;
                        }
                        if (thisKey.IndexOf("css_select") == 0)
                        {
                            css_file = HttpContext.Current.Request.Form[thisKey];
                            HttpContext.Current.Session["New_TEI_mySobekViewer.CSS_File"] = css_file;
                        }
                    }
                }

                string action = HttpContext.Current.Request.Form["action"];
                if (action == "cancel")
                {
                    // Clear all files in the RequestSpecificValues.Current_User process folder
                    try
                    {
                        string[] all_files = Directory.GetFiles(userInProcessDirectory);
                        foreach (string thisFile in all_files)
                            File.Delete(thisFile);
                        Directory.Delete(userInProcessDirectory);
                    }
                    catch (Exception ee)
                    {
                        tei_file = ee.Message;
                        // Unable to delete existing file in the RequestSpecificValues.Current_User's folder.
                        // This is an error, but how to report it?
                    }

                    // Clear all the information in memory
                    HttpContext.Current.Session["agreement_date"] = null;
                    HttpContext.Current.Session["item"] = null;

                    HttpContext.Current.Session["New_TEI_mySobekViewer.Mapping_File"] = null;
                    HttpContext.Current.Session["New_TEI_mySobekViewer.XSLT_File"] = null;
                    HttpContext.Current.Session["New_TEI_mySobekViewer.CSS_File"] = null;


                    // Clear any temporarily assigned current project and CompleteTemplate
                    RequestSpecificValues.Current_User.Current_Default_Metadata = null;
                    RequestSpecificValues.Current_User.Current_Template = null;

                    // Forward back to my Sobek home
                    RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                    UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                }

                if (action == "delete")
                {
                    string filename = HttpContext.Current.Request.Form["phase"];
                    try
                    {
                        if (File.Exists(userInProcessDirectory + "\\" + filename))
                            File.Delete(userInProcessDirectory + "\\" + filename);

                        // Forward
                        UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                        return;
                    }
                    catch (Exception)
                    {
                        // Unable to delete existing file in the RequestSpecificValues.Current_User's folder.
                        // This is an error, but how to report it?
                    }
                }

                if (action == "clear")
                {
                    // If there is an old METS file, delete it
                    if (File.Exists(userInProcessDirectory + "\\TEMP000001_00001.mets"))
                        File.Delete(userInProcessDirectory + "\\TEMP000001_00001.mets");

                    // Create the new METS file and add to the session
                    new_item(null);
                    HttpContext.Current.Session["Item"] = item;

                    // Forward back to the same URL
                    RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "2";
                    UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                    return;
                }

                if (action == "next_phase")
                {

                    string next_phase = HttpContext.Current.Request.Form["phase"];

                    // If this goes from step 1 to step 2, write the permissions first
                    if ((currentProcessStep == 1) && (next_phase == "2") && (completeTemplate.Permissions_Agreement.Length > 0))
                    {
                        // Store this agreement in the session state
                        DateTime agreement_date = DateTime.Now;
                        HttpContext.Current.Session["agreement_date"] = agreement_date;

                        // Also, save this as a text file
                        string agreement_file = userInProcessDirectory + "\\agreement.txt";
                        StreamWriter writer = new StreamWriter(agreement_file, false);
                        writer.WriteLine("Permissions Agreement");
                        writer.WriteLine();
                        if (!String.IsNullOrWhiteSpace(RequestSpecificValues.Current_User.ShibbID))
                            writer.WriteLine("User: " + RequestSpecificValues.Current_User.Full_Name + " ( " + RequestSpecificValues.Current_User.ShibbID + " )");
                        else
                            writer.WriteLine("User: " + RequestSpecificValues.Current_User.Full_Name);

                        writer.WriteLine("Date: " + agreement_date.ToString());
                        writer.WriteLine("IP Address: " + HttpContext.Current.Request.UserHostAddress);
                        writer.WriteLine();
                        writer.WriteLine(completeTemplate.Permissions_Agreement);
                        writer.Flush();
                        writer.Close();

                        if (HttpContext.Current.Request.Form["setNewDefaultCheckBox"] != null)
                        {
                            // Set the default metadata preference first
                            string prefProject = HttpContext.Current.Request.Form["prefProject"];
                            if (!String.IsNullOrEmpty(prefProject))
                                RequestSpecificValues.Current_User.Set_Current_Default_Metadata(prefProject.Trim());

                            // Set the template code next
                            string prefTemplate = HttpContext.Current.Request.Form["prefTemplate"];
                            if (!String.IsNullOrEmpty(prefTemplate))
                                RequestSpecificValues.Current_User.Set_Default_Template(prefTemplate.Trim());

                            // Save the user preferences
                            SobekCM_Database.Save_User(RequestSpecificValues.Current_User, String.Empty, RequestSpecificValues.Current_User.Authentication_Type, RequestSpecificValues.Tracer);
                        }
                    }

                    // If this goes from step 2 (upload TEI) to step 3, validate the TEI XML file
                    if ((currentProcessStep == 2) && (next_phase == "3"))
                    {
                        // Should be a TEI file to continue
                        if (!String.IsNullOrEmpty(tei_file))
                        {
                            XmlValidator validator = new XmlValidator();
                            string tei_filepath = Path.Combine(userInProcessDirectory, tei_file);
                            bool isValid = validator.IsValid(tei_filepath);
                            if (!isValid)
                            {
                                string validatorErrors = validator.Errors.Replace("\n", "<br />\n");
                                error_message = "Uploaded TEI file is not a valid XML source file.<br /><br />\n" + validatorErrors + "<br />";
                                next_phase = "2";
                            }
                        }
                    }

                    // If this is going from a step that includes the metadata entry portion, save this to the item
                    if ((currentProcessStep > 4) && (currentProcessStep < 8))
                    {
                        // Save to the item
                        completeTemplate.Save_To_Bib(item, RequestSpecificValues.Current_User, currentProcessStep - 4);
                        item.Save_METS();
                        HttpContext.Current.Session["Item"] = item;

                        // Save the pertinent data to the METS file package
                        item.METS_Header.Create_Date = DateTime.Now;
                        if ((HttpContext.Current.Session["agreement_date"] != null) && (HttpContext.Current.Session["agreement_date"].ToString().Length > 0))
                        {
                            DateTime asDateTime;
                            if (DateTime.TryParse(HttpContext.Current.Session["agreement_date"].ToString(), out asDateTime))
                                item.METS_Header.Create_Date = asDateTime;
                        }
                        HttpContext.Current.Session["Item"] = item;

                        // Save this item, just in case it gets lost somehow
                        item.Source_Directory = userInProcessDirectory;
                        string acquisition_append = "Submitted by " + RequestSpecificValues.Current_User.Full_Name + ".";
                        if (item.Bib_Info.Notes_Count > 0)
                        {
                            foreach (Note_Info thisNote in item.Bib_Info.Notes.Where(ThisNote => ThisNote.Note_Type == Note_Type_Enum.Acquisition))
                            {
                                if (thisNote.Note.IndexOf(acquisition_append) < 0)
                                    thisNote.Note = thisNote.Note.Trim() + "  " + acquisition_append;
                                break;
                            }
                        }

                        // Also, check all the authors to add the current users attribution information
                        if (RequestSpecificValues.Current_User.Organization.Length > 0)
                        {
                            if ((item.Bib_Info.Main_Entity_Name.Full_Name.IndexOf(RequestSpecificValues.Current_User.Family_Name) >= 0) && ((item.Bib_Info.Main_Entity_Name.Full_Name.IndexOf(RequestSpecificValues.Current_User.Given_Name) >= 0) || ((RequestSpecificValues.Current_User.Nickname.Length > 2) && (item.Bib_Info.Main_Entity_Name.Full_Name.IndexOf(RequestSpecificValues.Current_User.Nickname) > 0))))
                            {
                                item.Bib_Info.Main_Entity_Name.Affiliation = RequestSpecificValues.Current_User.Organization;
                                if (RequestSpecificValues.Current_User.College.Length > 0)
                                    item.Bib_Info.Main_Entity_Name.Affiliation = item.Bib_Info.Main_Entity_Name.Affiliation + " -- " + RequestSpecificValues.Current_User.College;
                                if (RequestSpecificValues.Current_User.Department.Length > 0)
                                    item.Bib_Info.Main_Entity_Name.Affiliation = item.Bib_Info.Main_Entity_Name.Affiliation + " -- " + RequestSpecificValues.Current_User.Department;
                                if (RequestSpecificValues.Current_User.Unit.Length > 0)
                                    item.Bib_Info.Main_Entity_Name.Affiliation = item.Bib_Info.Main_Entity_Name.Affiliation + " -- " + RequestSpecificValues.Current_User.Unit;
                            }
                            if (item.Bib_Info.Names_Count > 0)
                            {
                                foreach (Name_Info thisName in item.Bib_Info.Names)
                                {
                                    if ((thisName.Full_Name.IndexOf(RequestSpecificValues.Current_User.Family_Name) >= 0) && ((thisName.Full_Name.IndexOf(RequestSpecificValues.Current_User.Given_Name) >= 0) || ((RequestSpecificValues.Current_User.Nickname.Length > 2) && (thisName.Full_Name.IndexOf(RequestSpecificValues.Current_User.Nickname) > 0))))
                                    {
                                        thisName.Affiliation = RequestSpecificValues.Current_User.Organization;
                                        if (RequestSpecificValues.Current_User.College.Length > 0)
                                            thisName.Affiliation = thisName.Affiliation + " -- " + RequestSpecificValues.Current_User.College;
                                        if (RequestSpecificValues.Current_User.Department.Length > 0)
                                            thisName.Affiliation = thisName.Affiliation + " -- " + RequestSpecificValues.Current_User.Department;
                                        if (RequestSpecificValues.Current_User.Unit.Length > 0)
                                            thisName.Affiliation = thisName.Affiliation + " -- " + RequestSpecificValues.Current_User.Unit;

                                    }
                                }
                            }
                        }
                        item.Save_METS();
                        HttpContext.Current.Session["Item"] = item;
                    }

                    // For now, just forward to the next phase
                    if (currentProcessStep.ToString() != next_phase)
                    {
                        RequestSpecificValues.Current_Mode.My_Sobek_SubMode = next_phase;
                        UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                        return;
                    }
                }
            }

            #endregion

            #region Perform some validation to determine if the RequestSpecificValues.Current_User should be at this step

            // If this is past the agreement phase, check that an agreement exists
            if (currentProcessStep > 1)
            {
                // Validate that an agreement.txt file exists, if the CompleteTemplate has permissions
                if ((completeTemplate.Permissions_Agreement.Length > 0) && (!File.Exists(userInProcessDirectory + "\\agreement.txt")))
                {
                    RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "1";
                    UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                    return;
                }

                // Get the validation errors
                validationErrors = new List<string>();
                SobekCM_Item_Validator.Validate_SobekCM_Item(item, validationErrors);
            }

            // If this is past the step to upload a TEI file, ensure a TEI file exists
            if ((currentProcessStep > 2) && (String.IsNullOrEmpty(tei_file)))
            {
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "2";
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }


            // If this is past the step to upload a TEI file, ensure a TEI file exists
            if ((currentProcessStep > 3) && ((String.IsNullOrEmpty(mapping_file)) || (String.IsNullOrEmpty(xslt_file))))
            {
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "3";
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }

            // If this is to put up items or complete the item, validate the METS
            if (currentProcessStep >= 8)
            {
                // Validate that a METS file exists
                if (Directory.GetFiles(userInProcessDirectory, "*.mets*").Length == 0)
                {
                    RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "2";
                    UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                    return;
                }

                // Get the validation errors
                if (validationErrors.Count == 0)
                    item.Save_METS();
                else
                {
                    item.Web.Show_Validation_Errors = true;
                    RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "2";
                    UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                    return;
                }
            }

            // If this is for step 8, ensure that this even takes this information, or go to step 9
            if ((currentProcessStep == 8) && (completeTemplate.Upload_Types == CompleteTemplate.Template_Upload_Types.None))
            {
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "9";
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }

            // If this is going into the last process step, check that any mandatory info (file, url, .. ) 
            // from the last step is present
            if (currentProcessStep == 9)
            {
                // Complete the item submission
                complete_item_submission(item, RequestSpecificValues.Tracer);
            }

            #endregion
        }

        #endregion

        #region Code to re-scale an image

        /// <summary> Scales an existing SourceImage to a new max width / max height </summary>
        /// <param name="SourceImage"> Source image </param>
        /// <param name="MaxWidth"> Maximum width for the new image </param>
        /// <param name="MaxHeight"> Maximum height for the new image </param>
        /// <returns> Newly scaled image, without changing the original source image </returns>
        public static Image ScaleImage(Image SourceImage, int MaxWidth, int MaxHeight)
        {
            var ratioX = (double)MaxWidth / SourceImage.Width;
            var ratioY = (double)MaxHeight / SourceImage.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(SourceImage.Width * ratio);
            var newHeight = (int)(SourceImage.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);
            Graphics.FromImage(newImage).DrawImage(SourceImage, 0, 0, newWidth, newHeight);
            return newImage;
        }

        #endregion

        #region Method commpletes the item submission on the way to the congratulations screen

        private bool complete_item_submission(SobekCM_Item Item_To_Complete, Custom_Tracer Tracer)
        {
            // Set an initial flag 
            criticalErrorEncountered = false;

            string[] all_files = Directory.GetFiles(userInProcessDirectory);
            SortedList<string, List<string>> image_files = new SortedList<string, List<string>>();
            SortedList<string, List<string>> download_files = new SortedList<string, List<string>>();
            foreach (string thisFile in all_files)
            {
                FileInfo thisFileInfo = new FileInfo(thisFile);

                if ((thisFileInfo.Name.IndexOf("agreement.txt") != 0) && (thisFileInfo.Name.IndexOf("TEMP000001_00001.mets") != 0) && (thisFileInfo.Name.IndexOf("doc.xml") != 0) && (thisFileInfo.Name.IndexOf("ufdc_mets.xml") != 0) && (thisFileInfo.Name.IndexOf("marc.xml") != 0))
                {
                    // Get information about this files name and extension
                    string extension_upper = thisFileInfo.Extension.ToUpper();
                    string filename_sans_extension = thisFileInfo.Name.Replace(thisFileInfo.Extension, "");
                    string name_upper = thisFileInfo.Name.ToUpper();

                    // Was this the TEI file?
                    if (String.Compare(tei_file, name_upper, StringComparison.OrdinalIgnoreCase) == 0)
                        continue;

                    // Is this a page image?
                    if ((extension_upper == ".JPG") || (extension_upper == ".TIF") || (extension_upper == ".JP2") || (extension_upper == ".JPX"))
                    {
                        // Exclude .QC.jpg files
                        if (name_upper.IndexOf(".QC.JPG") < 0)
                        {
                            // If this is a thumbnail, trim off the THM part on the file name
                            if (name_upper.IndexOf("THM.JPG") > 0)
                            {
                                filename_sans_extension = filename_sans_extension.Substring(0, filename_sans_extension.Length - 3);
                            }

                            // Is this the first image file with this name?
                            if (image_files.ContainsKey(filename_sans_extension.ToLower()))
                            {
                                image_files[filename_sans_extension.ToLower()].Add(thisFileInfo.Name);
                            }
                            else
                            {
                                List<string> newImageGrouping = new List<string> { thisFileInfo.Name };
                                image_files[filename_sans_extension.ToLower()] = newImageGrouping;
                            }
                        }
                    }
                    else
                    {
                        // If this does not match the exclusion regular expression, than add this
                        if (!Regex.Match(thisFileInfo.Name, UI_ApplicationCache_Gateway.Settings.Resources.Files_To_Exclude_From_Downloads, RegexOptions.IgnoreCase).Success)
                        {
                            // Also, exclude files that are .XML and marc.xml, or doc.xml, or have the bibid in the name
                            if ((thisFileInfo.Name.IndexOf("marc.xml", StringComparison.OrdinalIgnoreCase) != 0) && (thisFileInfo.Name.IndexOf("marc.xml", StringComparison.OrdinalIgnoreCase) != 0) && (thisFileInfo.Name.IndexOf(".mets", StringComparison.OrdinalIgnoreCase) < 0))
                            {
                                // Is this the first image file with this name?
                                if (download_files.ContainsKey(filename_sans_extension.ToLower()))
                                {
                                    download_files[filename_sans_extension.ToLower()].Add(thisFileInfo.Name);
                                }
                                else
                                {
                                    List<string> newDownloadGrouping = new List<string> { thisFileInfo.Name };
                                    download_files[filename_sans_extension.ToLower()] = newDownloadGrouping;
                                }
                            }
                        }
                    }
                }
            }

            // This package is good to go, so build it, save, etc...
            try
            {
                // Save the METS file to the database and back to the directory
                Item_To_Complete.Source_Directory = userInProcessDirectory;

                // Step through and add each file 
                Item_To_Complete.Divisions.Download_Tree.Clear();
                if ((completeTemplate.Upload_Types == CompleteTemplate.Template_Upload_Types.File_or_URL) || (completeTemplate.Upload_Types == CompleteTemplate.Template_Upload_Types.File))
                {
                    // Step through each file

                    bool error_reading_file_occurred = false;

                    // Add the image files first
                    foreach (string thisFileKey in image_files.Keys)
                    {
                        // Get the list of files
                        List<string> theseFiles = image_files[thisFileKey];

                        // Add each file
                        foreach (string thisFile in theseFiles)
                        {
                            // Create the new file object and compute a label
                            FileInfo fileInfo = new FileInfo(thisFile);
                            SobekCM_File_Info newFile = new SobekCM_File_Info(fileInfo.Name);
                            string label = fileInfo.Name.Replace(fileInfo.Extension, "");
                            if (HttpContext.Current.Session["file_" + thisFileKey] != null)
                            {
                                string possible_label = HttpContext.Current.Session["file_" + thisFileKey].ToString();
                                if (possible_label.Length > 0)
                                    label = possible_label;
                            }

                            // Add this file
                            Item_To_Complete.Divisions.Physical_Tree.Add_File(newFile, label);

                            // Seperate code for JP2 and JPEG type files
                            string extension = fileInfo.Extension.ToUpper();
                            if (extension.IndexOf("JP2") >= 0)
                            {
                                if (!error_reading_file_occurred)
                                {
                                    if (!newFile.Compute_Jpeg2000_Attributes(userInProcessDirectory))
                                        error_reading_file_occurred = true;
                                }
                            }
                            else
                            {
                                if (!error_reading_file_occurred)
                                {
                                    if (!newFile.Compute_Jpeg_Attributes(userInProcessDirectory))
                                        error_reading_file_occurred = true;
                                }
                            }
                        }
                    }

                    // Add the download files next
                    foreach (string thisFileKey in download_files.Keys)
                    {
                        // Get the list of files
                        List<string> theseFiles = download_files[thisFileKey];

                        // Add each file
                        foreach (string thisFile in theseFiles)
                        {
                            // Create the new file object and compute a label
                            FileInfo fileInfo = new FileInfo(thisFile);
                            SobekCM_File_Info newFile = new SobekCM_File_Info(fileInfo.Name);
                            string label = fileInfo.Name.Replace(fileInfo.Extension, "");
                            if (HttpContext.Current.Session["file_" + thisFileKey] != null)
                            {
                                string possible_label = HttpContext.Current.Session["file_" + thisFileKey].ToString();
                                if (possible_label.Length > 0)
                                    label = possible_label;
                            }

                            // Add this file
                            Item_To_Complete.Divisions.Download_Tree.Add_File(newFile, label);
                        }
                    }

                    // Now, add the TEI file
                    SobekCM_File_Info tei_newFile = new SobekCM_File_Info(tei_file);
                    string tei_label = tei_file + " (TEI)";
                    Item_To_Complete.Divisions.Download_Tree.Add_File(tei_newFile, tei_label);
                }

                // Determine the total size of the package before saving
                string[] all_files_final = Directory.GetFiles(userInProcessDirectory);
                double size = all_files_final.Aggregate<string, double>(0, (Current, ThisFile) => Current + (((new FileInfo(ThisFile)).Length) / 1024));
                Item_To_Complete.DiskSize_KB = size;

                // BibID and VID will be automatically assigned
                Item_To_Complete.BibID = completeTemplate.BibID_Root;
                Item_To_Complete.VID = String.Empty;

                // Set some values in the tracking portion
                if (Item_To_Complete.Divisions.Files.Count > 0)
                {
                    Item_To_Complete.Tracking.Born_Digital = true;
                }
                Item_To_Complete.Tracking.VID_Source = "SobekCM:" + templateCode;

                // Save to the database
                try
                {
                    SobekCM_Item_Database.Save_New_Digital_Resource(Item_To_Complete, false, true, RequestSpecificValues.Current_User.UserName, String.Empty, RequestSpecificValues.Current_User.UserID);
                }
                catch (Exception ee)
                {
                    StreamWriter writer = new StreamWriter(userInProcessDirectory + "\\exception.txt", false);
                    writer.WriteLine("ERROR CAUGHT WHILE SAVING NEW DIGITAL RESOURCE");
                    writer.WriteLine(DateTime.Now.ToString());
                    writer.WriteLine();
                    writer.WriteLine(ee.Message);
                    writer.WriteLine(ee.StackTrace);
                    writer.Flush();
                    writer.Close();
                    throw;
                }


                // Assign the file root and assoc file path
                Item_To_Complete.Web.File_Root = Item_To_Complete.BibID.Substring(0, 2) + "\\" + Item_To_Complete.BibID.Substring(2, 2) + "\\" + Item_To_Complete.BibID.Substring(4, 2) + "\\" + Item_To_Complete.BibID.Substring(6, 2) + "\\" + Item_To_Complete.BibID.Substring(8, 2);
                Item_To_Complete.Web.AssocFilePath = Item_To_Complete.Web.File_Root + "\\" + Item_To_Complete.VID + "\\";

                // Save the item settings
                SobekCM_Item_Database.Set_Item_Setting_Value(Item_To_Complete.Web.ItemID, "TEI.Source_File", tei_file);
                SobekCM_Item_Database.Set_Item_Setting_Value(Item_To_Complete.Web.ItemID, "TEI.CSS", css_file);
                SobekCM_Item_Database.Set_Item_Setting_Value(Item_To_Complete.Web.ItemID, "TEI.Mapping", mapping_file);

                // Find the actual XSLT file
                string xslt_directory = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network, "plugins", "tei", "xslt");
                string[] xslt_files = Directory.GetFiles(xslt_directory, xslt_file + ".xsl*");
                SobekCM_Item_Database.Set_Item_Setting_Value(Item_To_Complete.Web.ItemID, "TEI.XSLT", Path.GetFileName(xslt_files[0]));

                // Add the TEI viewer
                string tei_filename = Path.GetFileName(tei_file);
                SobekCM_Item_Database.Save_Item_Add_Viewer(Item_To_Complete.Web.ItemID, "TEI", tei_filename.Replace(".xml", "").Replace(".XML", "") + " (TEI)", tei_filename);

                //// Create the static html pages
                //string base_url = RequestSpecificValues.Current_Mode.Base_URL;
                //try
                //{
                //    Static_Pages_Builder staticBuilder = new Static_Pages_Builder(UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL, UI_ApplicationCache_Gateway.Settings.Servers.Base_Data_Directory, RequestSpecificValues.HTML_Skin.Skin_Code);
                //    string filename = userInProcessDirectory + "\\" + Item_To_Complete.BibID + "_" + Item_To_Complete.VID + ".html";
                //    staticBuilder.Create_Item_Citation_HTML(Item_To_Complete, filename, String.Empty);

                //    // Copy the static HTML file to the web server
                //    try
                //    {
                //        if (!Directory.Exists(UI_ApplicationCache_Gateway.Settings.Servers.Static_Pages_Location + item.BibID.Substring(0, 2) + "\\" + item.BibID.Substring(2, 2) + "\\" + item.BibID.Substring(4, 2) + "\\" + item.BibID.Substring(6, 2) + "\\" + item.BibID.Substring(8)))
                //            Directory.CreateDirectory(UI_ApplicationCache_Gateway.Settings.Servers.Static_Pages_Location + item.BibID.Substring(0, 2) + "\\" + item.BibID.Substring(2, 2) + "\\" + item.BibID.Substring(4, 2) + "\\" + item.BibID.Substring(6, 2) + "\\" + item.BibID.Substring(8));
                //        if (File.Exists(userInProcessDirectory + "\\" + item.BibID + "_" + item.VID + ".html"))
                //            File.Copy(userInProcessDirectory + "\\" + item.BibID + "_" + item.VID + ".html", UI_ApplicationCache_Gateway.Settings.Servers.Static_Pages_Location + item.BibID.Substring(0, 2) + "\\" + item.BibID.Substring(2, 2) + "\\" + item.BibID.Substring(4, 2) + "\\" + item.BibID.Substring(6, 2) + "\\" + item.BibID.Substring(8) + "\\" + item.BibID + "_" + item.VID + ".html", true);
                //    }
                //    catch (Exception)
                //    {
                //        // This is not critical
                //    }
                //}
                //catch (Exception)
                //{
                //    // An error here is not catastrophic
                //}

                //RequestSpecificValues.Current_Mode.Base_URL = base_url;

                // Save the rest of the metadata
                Item_To_Complete.Save_SobekCM_METS();

                // Create the options dictionary used when saving information to the database, or writing MarcXML
                Dictionary<string, object> options = new Dictionary<string, object>();
                if (UI_ApplicationCache_Gateway.Settings.MarcGeneration != null)
                {
                    options["MarcXML_File_ReaderWriter:MARC Cataloging Source Code"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Cataloging_Source_Code;
                    options["MarcXML_File_ReaderWriter:MARC Location Code"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Location_Code;
                    options["MarcXML_File_ReaderWriter:MARC Reproduction Agency"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Reproduction_Agency;
                    options["MarcXML_File_ReaderWriter:MARC Reproduction Place"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Reproduction_Place;
                    options["MarcXML_File_ReaderWriter:MARC XSLT File"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.XSLT_File;
                }
                options["MarcXML_File_ReaderWriter:System Name"] = UI_ApplicationCache_Gateway.Settings.System.System_Name;
                options["MarcXML_File_ReaderWriter:System Abbreviation"] = UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation;

                // Save the marc xml file
                MarcXML_File_ReaderWriter marcWriter = new MarcXML_File_ReaderWriter();
                string errorMessage;
                marcWriter.Write_Metadata(Item_To_Complete.Source_Directory + "\\marc.xml", Item_To_Complete, options, out errorMessage);

                // Delete the TEMP mets file
                if (File.Exists(userInProcessDirectory + "\\TEMP000001_00001.mets"))
                    File.Delete(userInProcessDirectory + "\\TEMP000001_00001.mets");

                // Rename the METS file to the XML file                
                if ((!File.Exists(userInProcessDirectory + "\\" + Item_To_Complete.BibID + "_" + Item_To_Complete.VID + ".mets.xml")) &&
                    (File.Exists(userInProcessDirectory + "\\" + Item_To_Complete.BibID + "_" + Item_To_Complete.VID + ".mets")))
                {
                    File.Move(userInProcessDirectory + "\\" + Item_To_Complete.BibID + "_" + Item_To_Complete.VID + ".mets", userInProcessDirectory + "\\" + Item_To_Complete.BibID + "_" + Item_To_Complete.VID + ".mets.xml");
                }

                string serverNetworkFolder = UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network + Item_To_Complete.Web.AssocFilePath;

                // Create the folder
                if (!Directory.Exists(serverNetworkFolder))
                    Directory.CreateDirectory(serverNetworkFolder);
                if (!Directory.Exists(serverNetworkFolder + "\\" + UI_ApplicationCache_Gateway.Settings.Resources.Backup_Files_Folder_Name))
                    Directory.CreateDirectory(serverNetworkFolder + "\\" + UI_ApplicationCache_Gateway.Settings.Resources.Backup_Files_Folder_Name);

                // Copy the static HTML page over first
                if (File.Exists(userInProcessDirectory + "\\" + item.BibID + "_" + item.VID + ".html"))
                {
                    File.Copy(userInProcessDirectory + "\\" + item.BibID + "_" + item.VID + ".html", serverNetworkFolder + "\\" + UI_ApplicationCache_Gateway.Settings.Resources.Backup_Files_Folder_Name + "\\" + item.BibID + "_" + item.VID + ".html", true);
                    File.Delete(userInProcessDirectory + "\\" + item.BibID + "_" + item.VID + ".html");
                }

                // Copy all the files
                string[] allFiles = Directory.GetFiles(userInProcessDirectory);
                foreach (string thisFile in allFiles)
                {
                    string destination_file = serverNetworkFolder + "\\" + (new FileInfo(thisFile)).Name;
                    File.Copy(thisFile, destination_file, true);
                }

                // Incrememnt the count of number of items submitted by this RequestSpecificValues.Current_User
                RequestSpecificValues.Current_User.Items_Submitted_Count++;
                if (!RequestSpecificValues.Current_User.BibIDs.Contains(Item_To_Complete.BibID))
                    RequestSpecificValues.Current_User.Add_BibID(Item_To_Complete.BibID);


                // Now, delete all the files here
                all_files = Directory.GetFiles(userInProcessDirectory);
                foreach (string thisFile in all_files)
                {
                    File.Delete(thisFile);
                }

                // Always set the additional work needed flag, to give the builder a  chance to look at it
                SobekCM_Item_Database.Update_Additional_Work_Needed_Flag(Item_To_Complete.Web.ItemID, true);

                // Clear any temporarily assigned current project and CompleteTemplate
                RequestSpecificValues.Current_User.Current_Default_Metadata = null;
                RequestSpecificValues.Current_User.Current_Template = null;

            }
            catch (Exception ee)
            {
                validationErrors.Add("Error encountered during item save!");
                validationErrors.Add(ee.ToString().Replace("\r", "<br />"));

                // Set an initial flag 
                criticalErrorEncountered = true;

                string error_body = "<strong>ERROR ENCOUNTERED DURING ONLINE SUBMITTAL PROCESS</strong><br /><br /><blockquote>Title: " + Item_To_Complete.Bib_Info.Main_Title.Title + "<br />Permanent Link: <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "/" + Item_To_Complete.BibID + "/" + Item_To_Complete.VID + "\">" + RequestSpecificValues.Current_Mode.Base_URL + "/" + Item_To_Complete.BibID + "/" + Item_To_Complete.VID + "</a><br />User: " + RequestSpecificValues.Current_User.Full_Name + "<br /><br /></blockquote>" + ee.ToString().Replace("\n", "<br />");
                string error_subject = "Error during submission for '" + Item_To_Complete.Bib_Info.Main_Title.Title + "'";
                string email_to = UI_ApplicationCache_Gateway.Settings.Email.System_Error_Email;
                if (email_to.Length == 0)
                    email_to = UI_ApplicationCache_Gateway.Settings.Email.System_Email;
                Email_Helper.SendEmail(email_to, error_subject, error_body, true, RequestSpecificValues.Current_Mode.Instance_Name);
            }

            if (!criticalErrorEncountered)
            {
                // Send email to the email from the CompleteTemplate, if one was provided
                if (completeTemplate.Email_Upon_Receipt.Length > 0)
                {
                    string body = "New item submission complete!<br /><br /><blockquote>Title: " + Item_To_Complete.Bib_Info.Main_Title.Title + "<br />Submittor: " + RequestSpecificValues.Current_User.Full_Name + " ( " + RequestSpecificValues.Current_User.Email + " )<br />Link: <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "/" + Item_To_Complete.BibID + "/" + Item_To_Complete.VID + "\">" + Item_To_Complete.BibID + ":" + Item_To_Complete.VID + "</a></blockquote>";
                    string subject = "Item submission complete for '" + Item_To_Complete.Bib_Info.Main_Title.Title + "'";
                    Email_Helper.SendEmail(completeTemplate.Email_Upon_Receipt, subject, body, true, RequestSpecificValues.Current_Mode.Instance_Name);
                }

                // If the RequestSpecificValues.Current_User wants to have a message sent, send one
                if (RequestSpecificValues.Current_User.Send_Email_On_Submission)
                {
                    // Create the mail message
                    string body2 = "<strong>CONGRATULATIONS!</strong><br /><br />Your item has been successfully added to the digital library and will appear immediately.  Search indexes may take a couple minutes to build, at which time this item will be discoverable through the search interface. <br /><br /><blockquote>Title: " + Item_To_Complete.Bib_Info.Main_Title.Title + "<br />Permanent Link: <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "/" + Item_To_Complete.BibID + "/" + Item_To_Complete.VID + "\">" + RequestSpecificValues.Current_Mode.Base_URL + "/" + Item_To_Complete.BibID + "/" + Item_To_Complete.VID + "</a></blockquote>";
                    string subject2 = "Item submission complete for '" + Item_To_Complete.Bib_Info.Main_Title.Title + "'";
                    Email_Helper.SendEmail(RequestSpecificValues.Current_User.Email, subject2, body2, true, RequestSpecificValues.Current_Mode.Instance_Name);
                }

                // Also clear any searches or browses ( in the future could refine this to only remove those
                // that are impacted by this save... but this is good enough for now )
                CachedDataManager.Clear_Search_Results_Browses();
            }

            return criticalErrorEncountered;
        }

        #endregion

        /// <summary> Gets the banner from the current CompleteTemplate, if there is one </summary>
        public string Current_Template_Banner
        {
            get
            {
                return completeTemplate != null ? completeTemplate.Banner : String.Empty;
            }
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <value> This returns the title of the current <see cref="CompleteTemplate"/> object </value>
        public override string Web_Title
        {
            get
            {
                return toolTitle;
            }
        }

        #region Methods to write the HTML directly to the output stream

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This adds the CompleteTemplate HTML for step 2 and the congratulations text for step 4 </remarks>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("New_TEI_MySobekViewer.Write_HTML", "Do nothing");

            if (currentProcessStep == 2)
            {
                Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Js + "\" type=\"text/javascript\"></script>");
                Output.WriteLine("<div class=\"sbkMySobek_HomeText\">");
                Output.WriteLine("<br />");
                Output.Write("<h2>Step 2 of " + totalTemplatePages + ": Upload TEI </h2>");

                // Was there a basic XML validation error?
                if (!String.IsNullOrEmpty(error_message))
                {
                    Output.WriteLine("<div style=\"padding-left:30px;font-weight:bold; color:Red; font-size:1.1em;\">");
                    Output.WriteLine(error_message);
                    Output.WriteLine("</div>");
                }

                string explanation = "Upload the TEI XML file for your new item and pick a short label for the TEI.";
                Output.WriteLine("<blockquote>");
                Output.WriteLine("  " + explanation);

                // Is there a TEI file?
                if (!String.IsNullOrEmpty(tei_file))
                {
                    string fileName = Path.GetFileName(tei_file);
                    Output.WriteLine("<br /><br /><br />Current TEI file is: <i>" + fileName + "</i>.<br /><br />Select a new TEI file to replace this file or select NEXT to go to the next step.");
                }

                Output.WriteLine("</blockquote><br />");
            }

            if (currentProcessStep == 8)
            {
                Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Js + "\" type=\"text/javascript\"></script>");
                Output.WriteLine("<div class=\"sbkMySobek_HomeText\">");
                Output.WriteLine("<br />");
                Output.Write("<h2>Step " + totalTemplatePages + " of " + totalTemplatePages + ": Upload Related Files (Optional) </h2>");

                string explanation = "Upload the related files for your new item.  You can also provide labels for each file, once they are uploaded.";
                Output.WriteLine("<blockquote>" + explanation + "</blockquote><br />");
            }
        }

        /// <summary> This is an opportunity to write HTML directly into the main form, without
        /// using the pop-up html form architecture </summary>
        /// <param name="Output"> Textwriter to write the pop-up form HTML for this viewer </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This text will appear within the ItemNavForm form tags </remarks>
        public override void Write_ItemNavForm_Closing(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("New_TEI_MySobekViewer.Write_ItemNavForm_Closing", "");
            }

            // Add the hidden fields first
            Output.WriteLine("<!-- Hidden field is used for postbacks to indicate what to save and reset -->");
            Output.WriteLine("<input type=\"hidden\" id=\"action\" name=\"action\" value=\"\" />");
            Output.WriteLine("<input type=\"hidden\" id=\"phase\" name=\"phase\" value=\"\" />");
            Output.WriteLine("<script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Js + "\" ></script>");

            #region Add the agreement HTML for the first step

            if (currentProcessStep == 1)
            {
                Output.WriteLine("<div class=\"sbkMySobek_HomeText\" >");
                Output.WriteLine("<br />");
                if (completeTemplate.Permissions_Agreement.Length > 0)
                {
                    Output.WriteLine("<h2>Step 1 of " + totalTemplatePages + ": Grant of Permission</h2>");

                    Output.WriteLine("<blockquote>You must read and accept the below permissions to continue.<br /><br />");
                    Output.WriteLine(completeTemplate.Permissions_Agreement.Replace("<%BASEURL%>", RequestSpecificValues.Current_Mode.Base_URL).Replace("[%BASEURL%]", RequestSpecificValues.Current_Mode.Base_URL).Replace("[%SYSTEMNAME%]", RequestSpecificValues.Current_Mode.Instance_Name));
                    //     Output.WriteLine("<p>Please review the <a href=\"?g=ufirg&amp;m=hitauthor_faq#policies&amp;n=gs\">Policies</A> if you have any questions or please contact us with any questions prior to submitting files. </p>\n");
                    Output.WriteLine("<table id=\"sbkNgi_GrantPermissionsAgreeSubTable\">");
                    Output.WriteLine("  <tr>");
                    Output.WriteLine("    <td>You must read and accept the above permissions agreement to continue. &nbsp; &nbsp; </td>");
                    Output.WriteLine("    <td>");
                    Output.WriteLine("        <button onclick=\"return new_item_cancel();\" class=\"sbkMySobek_BigButton\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_LeftImg\" alt=\"\" /> CANCEL </button> &nbsp; &nbsp; ");
                    Output.WriteLine("        <button onclick=\"return new_item_next_phase(2);\" class=\"sbkMySobek_BigButton\"> ACCEPT <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_RightImg\" alt=\"\" /></button>");
                    Output.WriteLine("    </td>");
                    Output.WriteLine("  </tr>");
                    Output.WriteLine("</table>");
                    Output.WriteLine("</blockquote><br />");

                }

                Output.WriteLine("<br />");
                Output.WriteLine("</div>");
            }

            #endregion

            #region Add the HTML to select the mapping, css, and xslt

            if (currentProcessStep == 3)
            {
                Output.WriteLine("<div class=\"sbkMySobek_HomeText\" >");
                Output.WriteLine("<br />");

                Output.WriteLine("<h2>Step 3 of " + totalTemplatePages + ": Select Mapping and Display Parameters</h2>");

                Output.WriteLine("Select the metadata mapping for importing metadata from your TEI and select the XSLT and CSS to display your TEI file within this system.<br /><br />");

                Output.WriteLine("<table class=\"sbkMySobek_TemplateTbl\" cellpadding=\"4px\" >");
                Output.WriteLine("  <tr>");
                Output.WriteLine("    <td colspan=\"3\" class=\"sbkMySobek_TemplateTblTitle_first\">Metadata Mapping</td>");
                Output.WriteLine("  </tr>");

                // Get the list of Mapping files that exist and this user is enabled for
                List<string> mapping_files = new List<string>();
                foreach (string thisSettingKey in RequestSpecificValues.Current_User.SettingsKeys)
                {
                    if (thisSettingKey.IndexOf("TEI.MAPPING.") == 0)
                    {
                        // Only show enabled options
                        string enabled = RequestSpecificValues.Current_User.Get_Setting(thisSettingKey, "false");
                        if (String.Compare(enabled, "true", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            // Get this file name
                            string file = thisSettingKey.Replace("TEI.MAPPING.", "");

                            // Also verify this mapping file exists
                            string filepath = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network, "plugins", "tei", "mapping", file + ".xml");
                            if (!File.Exists(filepath))
                                continue;

                            // Since this exists, add to the mapping file list
                            mapping_files.Add(file);
                        }
                    }
                }

                // Show an error message if no mapping file exists
                if (mapping_files.Count == 0)
                {
                    Output.WriteLine("  <tr>");
                    Output.WriteLine("    <td colspan=\"3\" style=\"padding-left:30px;font-weight:bold; color:Red; font-size:1.1em;\">You are not approved for any TEI mapping file.  Please let your system administrator know so they can approve you for an existing TEI mapping file.</td>");
                    Output.WriteLine("  </tr>");
                }
                else
                {

                    Output.WriteLine("  <tr>");
                    if (mapping_files.Count == 1)
                        Output.WriteLine("    <td colspan=\"3\" style=\"padding-left:30px;font-style:italic; color:#333; font-size:0.9em;\">The mapping file below will read the header information from your TEI file into the system, to facilitate searching and discovery of this resource.</td>");
                    else
                        Output.WriteLine("    <td colspan=\"3\" style=\"padding-left:30px;font-style:italic; color:#333; font-size:0.9em;\">Select the metadata mapping file below.  This mapping file will read the header information from your TEI file into the system, to facilitate searching and discovery of this resource.</td>");

                    Output.WriteLine("  </tr>");

                    Output.WriteLine("  <tr>");
                    Output.WriteLine("    <td style=\"width:15px\" > &nbsp;</td>");
                    Output.WriteLine("    <td class=\"metadata_label\">Metadata Mapping:</a></td>");


                    // If they are approved for only one mapping file, just show that one as text, not a select box
                    if (mapping_files.Count == 1)
                    {
                        Output.WriteLine("    <td>");
                        Output.WriteLine("      " + mapping_files[0]);
                        Output.WriteLine("      <input type=\"hidden\" id=\"mapping_select\" name=\"mapping_select\" value=\"" + mapping_files[0] + "\" />");
                        Output.WriteLine("    </td>");
                    }
                    else
                    {

                        Output.WriteLine("    <td>");
                        Output.WriteLine("      <table>");
                        Output.WriteLine("        <tr>");
                        Output.WriteLine("          <td>");
                        Output.WriteLine("            <div id=\"mapping_div\">");
                        Output.WriteLine("              <select class=\"type_select\" name=\"mapping_select\" id=\"mapping_select\" >");

                        foreach (string file in mapping_files)
                        {
                            // Add this mapping information
                            if (String.Compare(file, mapping_file, StringComparison.OrdinalIgnoreCase) == 0)
                                Output.WriteLine("              <option value=\"" + file + "\" selected=\"selected\">" + file + "</option>");
                            else
                                Output.WriteLine("              <option value=\"" + file + "\">" + file + "</option>");
                        }

                        Output.WriteLine("              </select>");
                        Output.WriteLine("            </div>");
                        Output.WriteLine("          </td>");

                        //Output.WriteLine("          <td style=\"vertical-align:bottom\">");
                        //Output.WriteLine("            <a target=\"_TYPE\"  title=\"Get help.\" href=\"http://sobekrepository.org/help/typesimple\"><img class=\"help_button\" src=\"http://cdn.sobekrepository.org/images/misc/help_button.jpg\" /></a>");
                        //Output.WriteLine("          </td>");

                        Output.WriteLine("        </tr>");
                        Output.WriteLine("      </table>");
                        Output.WriteLine("    </td>");
                    }
                    Output.WriteLine("  </tr>");
                }

                Output.WriteLine("  <tr>");
                Output.WriteLine("    <td colspan=\"3\" class=\"sbkMySobek_TemplateTblTitle\" style=\"padding-top:25px\">Display Parameters (XSLT and CSS)</td>");
                Output.WriteLine("  </tr>");

                // Get the list of XSLT files that exist and this user is enabled for
                List<string> xslt_files = new List<string>();
                foreach (string thisSettingKey in RequestSpecificValues.Current_User.SettingsKeys)
                {
                    if (thisSettingKey.IndexOf("TEI.XSLT.") == 0)
                    {
                        // Only show enabled options
                        string enabled = RequestSpecificValues.Current_User.Get_Setting(thisSettingKey, "false");
                        if (String.Compare(enabled, "true", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            // Get this file name
                            string file = thisSettingKey.Replace("TEI.XSLT.", "");

                            // Also verify this mapping file exists
                            string filepath = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network, "plugins", "tei", "xslt", file);
                            if ((!File.Exists(filepath + ".xslt")) && (!File.Exists(filepath + ".xsl")))
                                continue;

                            // Since this exists, add to the xslt file list
                            xslt_files.Add(file);
                        }
                    }
                }

                // Show an error message if no XSLT file exists
                if (xslt_files.Count == 0)
                {
                    Output.WriteLine("  <tr>");
                    Output.WriteLine("    <td colspan=\"3\" style=\"padding-left:30px;font-weight:bold; color:Red; font-size:1.1em;\">You are not approved for any TEI XSLT file.  Please let your system administrator know so they can approve you for an existing TEI XSLT file.</td>");
                    Output.WriteLine("  </tr>");
                }
                else
                {
                    Output.WriteLine("  <tr>");
                    Output.WriteLine("    <td colspan=\"3\" style=\"padding-left:30px;font-style:italic; color:#333; font-size:0.9em;\">The values below determine how the TEI will display within this system.  The XSLT will transform your TEI into HTML for display and the CSS file can add additional style to the resulting display.</td>");
                    Output.WriteLine("  </tr>");

                    Output.WriteLine("  <tr>");
                    Output.WriteLine("    <td style=\"width:15px\" > &nbsp;</td>");
                    Output.WriteLine("    <td class=\"metadata_label\">XSLT File:</a></td>");

                    // If they are approved for only one XSLT file, just show that one as text, not a select box
                    if (xslt_files.Count == 1)
                    {
                        Output.WriteLine("    <td>");
                        Output.WriteLine("      " + xslt_files[0]);
                        Output.WriteLine("      <input type=\"hidden\" id=\"xslt_select\" name=\"xslt_select\" value=\"" + xslt_files[0] + "\" />");
                        Output.WriteLine("    </td>");
                    }
                    else
                    {

                        Output.WriteLine("    <td>");
                        Output.WriteLine("      <table>");
                        Output.WriteLine("        <tr>");
                        Output.WriteLine("          <td>");
                        Output.WriteLine("            <div id=\"xslt_div\">");
                        Output.WriteLine("              <select class=\"type_select\" name=\"xslt_select\" id=\"xslt_select\" >");

                        foreach (string file in xslt_files)
                        {

                            // Add this XSLT option
                            if (String.Compare(file, xslt_file, StringComparison.OrdinalIgnoreCase) == 0)
                                Output.WriteLine("              <option value=\"" + file + "\" selected=\"selected\">" + file + "</option>");
                            else
                                Output.WriteLine("              <option value=\"" + file + "\">" + file + "</option>");
                        }

                        Output.WriteLine("              </select>");
                        Output.WriteLine("            </div>");
                        Output.WriteLine("          </td>");

                        //Output.WriteLine("          <td style=\"vertical-align:bottom\">");
                        //Output.WriteLine("            <a target=\"_TYPE\"  title=\"Get help.\" href=\"http://sobekrepository.org/help/typesimple\"><img class=\"help_button\" src=\"http://cdn.sobekrepository.org/images/misc/help_button.jpg\" /></a>");
                        //Output.WriteLine("          </td>");

                        Output.WriteLine("        </tr>");
                        Output.WriteLine("      </table>");
                        Output.WriteLine("    </td>");
                    }
                    Output.WriteLine("  </tr>");
                }

                // CSS is not required, so check to see if any enable CSS's exist
                List<string> css_files = new List<string>();
                foreach (string thisSettingKey in RequestSpecificValues.Current_User.SettingsKeys)
                {
                    if (thisSettingKey.IndexOf("TEI.CSS.") == 0)
                    {
                        // Only show enabled options
                        string enabled = RequestSpecificValues.Current_User.Get_Setting(thisSettingKey, "false");
                        if (String.Compare(enabled, "true", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            // Get this file name
                            string file = thisSettingKey.Replace("TEI.CSS.", "");

                            // Also verify this mapping file exists
                            string filepath = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network, "plugins", "tei", "css", file + ".css");
                            if (!File.Exists(filepath))
                                continue;

                            // Since this exists, add to the css file list
                            css_files.Add(file);
                         }
                    }
                }

                // Only show the CSS options, if there are CSS options
                if (css_files.Count > 0)
                {
                    Output.WriteLine("  <tr>");
                    Output.WriteLine("    <td style=\"width:15px\" > &nbsp;</td>");
                    Output.WriteLine("    <td class=\"metadata_label\">CSS File:</a></td>");

                    Output.WriteLine("    <td>");
                    Output.WriteLine("      <table>");
                    Output.WriteLine("        <tr>");
                    Output.WriteLine("          <td>");
                    Output.WriteLine("            <div id=\"css_div\">");
                    Output.WriteLine("              <select class=\"type_select\" name=\"css_select\" id=\"css_select\" >");
                    Output.WriteLine("                <option value=\"\">(none)</option>");
                    foreach (string file in css_files)
                    {
                        if (String.Compare(file, css_file, StringComparison.OrdinalIgnoreCase) == 0)
                            Output.WriteLine("                <option value=\"" + file + "\" selected=\"selected\">" + file + "</option>");
                        else
                            Output.WriteLine("                <option value=\"" + file + "\">" + file + "</option>");
                    }

                    Output.WriteLine("              </select>");
                    Output.WriteLine("            </div>");
                    Output.WriteLine("          </td>");

                    //Output.WriteLine("          <td style=\"vertical-align:bottom\">");
                    //Output.WriteLine("            <a target=\"_TYPE\"  title=\"Get help.\" href=\"http://sobekrepository.org/help/typesimple\"><img class=\"help_button\" src=\"http://cdn.sobekrepository.org/images/misc/help_button.jpg\" /></a>");
                    //Output.WriteLine("          </td>");

                    Output.WriteLine("        </tr>");
                    Output.WriteLine("      </table>");
                    Output.WriteLine("    </td>");

                    Output.WriteLine("  </tr>");
                }
                Output.WriteLine("</table>");


                // Add the bottom buttons
                Output.WriteLine("      <div class=\"sbkMySobek_RightButtons\">");
                Output.WriteLine("        <button onclick=\"return new_item_next_phase(2);\" class=\"sbkMySobek_BigButton\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_LeftImg\" alt=\"\" /> BACK </button> &nbsp; &nbsp; ");
                Output.WriteLine("        <button onclick=\"return new_item_next_phase(4);\" class=\"sbkMySobek_BigButton\"> NEXT <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_RightImg\" alt=\"\" /></button>");
                Output.WriteLine("      </div>");

                Output.WriteLine("<br /><br /><br />");

                Output.WriteLine("</div>");
            }

            #endregion

            #region Add the metadata preview for this item

            if (currentProcessStep == 4)
            {
                // Get the mapping file
                string complete_mapping_file = Path.Combine(UI.UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network, "plugins\\tei\\mapping", mapping_file + ".xml");
                string complete_tei_file = Path.Combine(userInProcessDirectory, tei_file);
                bool error = false;

                try
                {
                    // Create a new item again
                    new_item(RequestSpecificValues.Tracer);

                    // Use the mapper and pull the results
                    GenericXmlReader testMapper = new GenericXmlReader();
                    GenericXmlReaderResults returnValue = testMapper.ProcessFile(complete_tei_file, complete_mapping_file);

                    // Was there an error converting using the selected mapping?
                    if ((returnValue == null) || (!String.IsNullOrEmpty(returnValue.ErrorMessage)))
                    {
                        error = true;
                        if (returnValue != null)
                            error_message = "Error mapping the TEI XML file into the SobekCM item.<br /><br />" + returnValue.ErrorMessage + "<br /><br />Try a different mapping or contact your system administrator.<br /><br />";
                        else
                            error_message = "Error mapping the TEI XML file into the SobekCM item.<br /><br />Try a different mapping or contact your system administrator.<br /><br />";
                    }
                    else
                    {
                        // Create the mapper to map these values into the SobekCM object
                        Standard_Bibliographic_Mapper mappingObject = new Standard_Bibliographic_Mapper();

                        // Add all this information
                        foreach (MappedValue mappedValue in returnValue.MappedValues)
                        {
                            // If NONE mapping, just go on
                            if ((String.IsNullOrEmpty(mappedValue.Mapping)) || (String.Compare(mappedValue.Mapping, "None", StringComparison.OrdinalIgnoreCase) == 0))
                                continue;

                            if (!String.IsNullOrEmpty(mappedValue.Value))
                            {
                                // One mappig that is NOT bibliographic in nature is the full text
                                if ((String.Compare(mappedValue.Mapping, "FullText", StringComparison.OrdinalIgnoreCase) == 0) ||
                                    (String.Compare(mappedValue.Mapping, "Text", StringComparison.OrdinalIgnoreCase) == 0) ||
                                    (String.Compare(mappedValue.Mapping, "Full Text", StringComparison.OrdinalIgnoreCase) == 0))
                                {
                                    // Ensure no other TEXT file exists here ( in case a different file was uploaded )
                                    try
                                    {
                                        string text_file = Path.Combine(userInProcessDirectory, "fulltext.txt");
                                        StreamWriter writer = new StreamWriter(text_file);
                                        writer.Write(mappedValue.Value);
                                        writer.Flush();
                                        writer.Close();
                                    }
                                    catch
                                    {

                                    }
                                }
                                else
                                {
                                    mappingObject.Add_Data(item, mappedValue.Value, mappedValue.Mapping);
                                }
                            }
                        }

                        item.Save_METS();
                        HttpContext.Current.Session["Item"] = item;
                    }

                }
                catch (Exception ee)
                {
                    error_message = ee.Message;
                }

                Output.WriteLine("<div class=\"sbkMySobek_HomeText\" >");
                Output.WriteLine("<br />");

                Output.WriteLine("<h2>Step 4 of " + totalTemplatePages + ": Metadata Preview</h2>");

                // Was there a basic XML validation error?
                if ((error) && (!String.IsNullOrEmpty(error_message)))
                {
                    Output.WriteLine("<div style=\"padding-left:30px;font-weight:bold; color:Red; font-size:1.1em;\">");
                    Output.WriteLine(error_message);
                    Output.WriteLine("</div>");
                }

                Output.WriteLine("<blockquote>Below is a preview of the metadata extracted from your TEI file.<br /><br />");


                string citation = Standard_Citation_String(false, Tracer);
                Output.WriteLine(citation);

                // Add the bottom buttons
                Output.WriteLine("      <div class=\"sbkMySobek_RightButtons\">");
                Output.WriteLine("        <button onclick=\"return new_item_next_phase(3);\" class=\"sbkMySobek_BigButton\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_LeftImg\" alt=\"\" /> BACK </button> &nbsp; &nbsp; ");

                if ( !error )
                    Output.WriteLine("        <button onclick=\"return new_item_next_phase(5);\" class=\"sbkMySobek_BigButton\"> NEXT <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_RightImg\" alt=\"\" /></button>");

                Output.WriteLine("      </div>");

                Output.WriteLine("</blockquote><br />");

                Output.WriteLine("<br />");
                Output.WriteLine("</div>");
            }

            #endregion

            #region Add the CompleteTemplate and surrounding HTML for the CompleteTemplate page step(s)

            if ((currentProcessStep >= 5) && (currentProcessStep <= (completeTemplate.InputPages_Count + 4)))
            {
                Output.WriteLine("<script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Ui_1_10_3_Custom_Js + "\"></script>");

                Output.WriteLine("<div class=\"sbkMySobek_HomeText\">");
                Output.WriteLine("<br />");
                string template_page_title = completeTemplate.InputPages[currentProcessStep - 5].Title;
                if (template_page_title.Length == 0)
                    template_page_title = "Additional Item Description";
                string template_page_instructions = completeTemplate.InputPages[currentProcessStep - 5].Instructions;
                if (template_page_instructions.Length == 0)
                    template_page_instructions = "Enter additional basic information for your new item.";

                // Get the adjusted process step number ( for skipping permissions, usual step1 )
                int adjusted_process_step = currentProcessStep;
                if (completeTemplate.Permissions_Agreement.Length == 0)
                    adjusted_process_step--;

                Output.WriteLine("<h2>Step " + adjusted_process_step + " of " + totalTemplatePages + ": " + template_page_title + "</h2>");
                Output.WriteLine("<blockquote>" + template_page_instructions + "</blockquote>");
                if ((validationErrors != null) && (validationErrors.Count > 0) && (item.Web.Show_Validation_Errors))
                {
                    Output.WriteLine("<span style=\"color: red;\"><b>The following errors were detected:</b>");
                    Output.WriteLine("<blockquote>");
                    foreach (string validation_error in validationErrors)
                    {
                        Output.WriteLine(validation_error + "<br />");
                    }
                    Output.WriteLine("</blockquote>");
                    Output.WriteLine("</span>");
                    Output.WriteLine("<br />");
                    Output.WriteLine();
                }

                int next_step = currentProcessStep + 1;
                if (currentProcessStep == completeTemplate.InputPages_Count + 4)
                {
                    next_step = completeTemplate.Upload_Types == CompleteTemplate.Template_Upload_Types.None ? 9 : 8;
                }
                Output.WriteLine("<div id=\"tabContainer\" class=\"fulltabs\">");
                Output.WriteLine("  <div class=\"graytabscontent\">");
                Output.WriteLine("    <div class=\"tabpage\" id=\"tabpage_1\">");

                // Add the top buttons
                Output.WriteLine("      <div class=\"sbkMySobek_RightButtons\">");
                Output.WriteLine("        <button onclick=\"return new_item_next_phase(" + (currentProcessStep - 1) + ");\" class=\"sbkMySobek_BigButton\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_LeftImg\" alt=\"\" /> BACK </button> &nbsp; &nbsp; ");
                Output.WriteLine("        <button onclick=\"return new_item_next_phase(" + next_step + ");\" class=\"sbkMySobek_BigButton\"> NEXT <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_RightImg\" alt=\"\" /></button>");
                Output.WriteLine("      </div>");
                Output.WriteLine("      <br /><br />");
                Output.WriteLine();

                bool isMozilla = ((!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Browser_Type)) && (RequestSpecificValues.Current_Mode.Browser_Type.ToUpper().IndexOf("FIREFOX") >= 0));

                string popup_forms = completeTemplate.Render_Template_HTML(Output, item, RequestSpecificValues.Current_Mode.Skin, isMozilla, RequestSpecificValues.Current_User, RequestSpecificValues.Current_Mode.Language, UI_ApplicationCache_Gateway.Translation, RequestSpecificValues.Current_Mode.Base_URL, currentProcessStep - 4);


                // Add the bottom buttons
                Output.WriteLine("      <div class=\"sbkMySobek_RightButtons\">");
                Output.WriteLine("        <button onclick=\"return new_item_next_phase(" + (currentProcessStep - 1) + ");\" class=\"sbkMySobek_BigButton\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_LeftImg\" alt=\"\" /> BACK </button> &nbsp; &nbsp; ");
                Output.WriteLine("        <button onclick=\"return new_item_next_phase(" + next_step + ");\" class=\"sbkMySobek_BigButton\"> NEXT <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_RightImg\" alt=\"\" /></button>");
                Output.WriteLine("      </div>");
                Output.WriteLine("      <br />");
                Output.WriteLine();

                Output.WriteLine("    </div>");
                Output.WriteLine("  </div>");
                Output.WriteLine("</div>");
                Output.WriteLine("<br /><br />");


                if (popup_forms.Length > 0)
                    Output.WriteLine(popup_forms);

                Output.WriteLine("</div>");
            }

            #endregion

            if (currentProcessStep == 2)
            {

                Output.WriteLine("<div class=\"sbkMySobek_FileRightButtons\">");
                Output.WriteLine("      <button onclick=\"return new_upload_next_phase(1);\" class=\"sbkMySobek_BigButton\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_LeftImg\" alt=\"\" /> BACK </button> &nbsp; &nbsp; ");
                Output.WriteLine("      <button onclick=\"return new_upload_next_phase(3);\" class=\"sbkMySobek_BigButton\"> NEXT <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_RightImg\" alt=\"\" /></button>");
                Output.WriteLine("      <div id=\"circular_progress\" name=\"circular_progress\" class=\"hidden_progress\">&nbsp;</div>");
                Output.WriteLine("</div>");

                Output.WriteLine("<br /><br /><br />");
                Output.WriteLine("</div>");
                Output.WriteLine();
            }

            #region Add the list of all existing files and the URL box for the upload file/enter URL step 

            if (currentProcessStep == 8)
            {
                if ((completeTemplate.Upload_Types == CompleteTemplate.Template_Upload_Types.File) || (completeTemplate.Upload_Types == CompleteTemplate.Template_Upload_Types.File_or_URL))
                {
                    string[] all_files = Directory.GetFiles(userInProcessDirectory);
                    SortedList<string, List<string>> image_files = new SortedList<string, List<string>>();
                    SortedList<string, List<string>> download_files = new SortedList<string, List<string>>();
                    foreach (string thisFile in all_files)
                    {
                        FileInfo thisFileInfo = new FileInfo(thisFile);

                        if ((thisFileInfo.Name.IndexOf("agreement.txt") != 0) && (thisFileInfo.Name.IndexOf("TEMP000001_00001.mets") != 0) && (thisFileInfo.Name.IndexOf("doc.xml") != 0) && (thisFileInfo.Name.IndexOf("sobek_mets.xml") != 0) && (thisFileInfo.Name.IndexOf("marc.xml") != 0))
                        {
                            // Get information about this files name and extension
                            string extension_upper = thisFileInfo.Extension.ToUpper();
                            string filename_sans_extension = thisFileInfo.Name.Replace(thisFileInfo.Extension, "");
                            string name_upper = thisFileInfo.Name.ToUpper();

                            // Is this a page image?
                            if ((extension_upper == ".JPG") || (extension_upper == ".TIF") || (extension_upper == ".JP2") || (extension_upper == ".JPX"))
                            {
                                // Exclude .QC.jpg files
                                if (name_upper.IndexOf(".QC.JPG") < 0)
                                {
                                    // If this is a thumbnail, trim off the THM part on the file name
                                    if (name_upper.IndexOf("THM.JPG") > 0)
                                    {
                                        filename_sans_extension = filename_sans_extension.Substring(0, filename_sans_extension.Length - 3);
                                    }

                                    // Is this the first image file with this name?
                                    if (image_files.ContainsKey(filename_sans_extension.ToLower()))
                                    {
                                        image_files[filename_sans_extension.ToLower()].Add(thisFileInfo.Name);
                                    }
                                    else
                                    {
                                        List<string> newImageGrouping = new List<string> { thisFileInfo.Name };
                                        image_files[filename_sans_extension.ToLower()] = newImageGrouping;
                                    }
                                }
                            }
                            else
                            {
                                // If this does not match the exclusion regular expression, than add this
                                if (!Regex.Match(thisFileInfo.Name, UI_ApplicationCache_Gateway.Settings.Resources.Files_To_Exclude_From_Downloads, RegexOptions.IgnoreCase).Success)
                                {
                                    if ((thisFileInfo.Name.IndexOf("marc.xml", StringComparison.OrdinalIgnoreCase) != 0) && (thisFileInfo.Name.IndexOf("marc.xml", StringComparison.OrdinalIgnoreCase) != 0) && (thisFileInfo.Name.IndexOf(".mets", StringComparison.OrdinalIgnoreCase) < 0))
                                    {
                                        // Is this the first image file with this name?
                                        if (download_files.ContainsKey(filename_sans_extension.ToLower()))
                                        {
                                            download_files[filename_sans_extension.ToLower()].Add(thisFileInfo.Name);
                                        }
                                        else
                                        {
                                            List<string> newDownloadGrouping = new List<string> { thisFileInfo.Name };
                                            download_files[filename_sans_extension.ToLower()] = newDownloadGrouping;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Any page images?
                    int file_counter = 0;
                    if (image_files.Count > 0)
                    {
                        Output.WriteLine("The following page images are already uploaded for this package:");
                        Output.WriteLine("<table class=\"sbkMySobek_FileTable\">");
                        Output.WriteLine("  <tr>");
                        Output.WriteLine("    <th style=\"width:350px;\">FILENAME</th>");
                        Output.WriteLine("    <th style=\"width:90px;\">SIZE</th>");
                        Output.WriteLine("    <th style=\"width:170px;\">DATE UPLOADED</th>");
                        Output.WriteLine("    <th style=\"width:90px;text-align:center;\">ACTION</th>");
                        Output.WriteLine("  </tr>");

                        int totalFileCount = 0;

                        //Determine the total number of files
                        foreach (string fileKey in image_files.Keys)
                        {
                            // Get this group of files
                            List<string> fileGroup = image_files[fileKey];
                            totalFileCount += fileGroup.Count();
                        }

                        // Step through all the page image file groups
                        foreach (string fileKey in image_files.Keys)
                        {
                            // Get this group of files
                            List<string> fileGroup = image_files[fileKey];

                            // Add each individual file
                            foreach (string thisFile in fileGroup)
                            {
                                file_counter++;

                                // Add the file name literal
                                FileInfo fileInfo = new FileInfo(userInProcessDirectory + "\\" + thisFile);
                                Output.WriteLine("  <tr style=\"min-height:22px\">");
                                Output.WriteLine("    <td>" + fileInfo.Name + "</td>");
                                if (fileInfo.Length < 1024)
                                    Output.WriteLine("    <td>" + fileInfo.Length + "</td>");
                                else
                                {
                                    if (fileInfo.Length < (1024 * 1024))
                                        Output.WriteLine("    <td>" + (fileInfo.Length / 1024) + " KB</td>");
                                    else
                                        Output.WriteLine("    <td>" + (fileInfo.Length / (1024 * 1024)) + " MB</td>");
                                }

                                Output.WriteLine("    <td>" + fileInfo.LastWriteTime + "</td>");

                                //add by Keven:replace single & double quote with ascII characters
                                string strFileName = fileInfo.Name;
                                if (strFileName.Contains("'") || strFileName.Contains("\""))
                                {
                                    strFileName = strFileName.Replace("'", "\\&#39;");
                                    strFileName = strFileName.Replace("\"", "\\&#34;");
                                }
                                Output.WriteLine("    <td style=\"text-align:center\"> <span class=\"sbkMySobek_ActionLink\">( <a href=\"\" onclick=\"return file_delete('" + strFileName + "');\">delete</a> )</span></td>");

                                Output.WriteLine("  </tr>");
                            }

                            // Now add the row to include the label
                            string input_name = "upload_label" + file_counter.ToString();
                            Output.WriteLine("  <tr style=\"min-height: 30px;\">");
                            Output.WriteLine("    <td colspan=\"4\">");
                            Output.WriteLine("      <div style=\"padding-left: 90px;\">");
                            Output.WriteLine("        <span style=\"color:gray\">Label:</span>");
                            Output.WriteLine("        <input type=\"hidden\" id=\"upload_file" + file_counter.ToString() + "\" name=\"upload_file" + file_counter.ToString() + "\" value=\"" + fileKey + "\" />");
                            if (HttpContext.Current.Session["file_" + fileKey] == null)
                            {
                                Output.WriteLine("      <input type=\"text\" class=\"sbkNgi_UploadFileLabel sbk_Focusable\" id=\"" + input_name + "\" name=\"" + input_name + "\" value=\"\" onchange=\"upload_label_fieldChanged(this.id," + totalFileCount + ");\"></input>");
                            }
                            else
                            {
                                string label_from_session = HttpContext.Current.Session["file_" + fileKey].ToString();
                                Output.WriteLine("      <input type=\"text\" class=\"sbkNgi_UploadFileLabel sbk_Focusable\" id=\"" + input_name + "\" name=\"" + input_name + "\" value=\"" + label_from_session + "\" onchange=\"upload_label_fieldChanged(this.id," + totalFileCount + ");\"></input>");
                            }
                            Output.WriteLine("      </div>");
                            Output.WriteLine("    </td>");
                            Output.WriteLine("  </tr>");
                            Output.WriteLine("  <tr><td class=\"sbkMySobek_FileTableRule\" colspan=\"4\"></td></tr>");
                        }
                        Output.WriteLine("</table>");
                    }

                    // Any download files?
                    if (download_files.Count > 0)
                    {
                        Output.WriteLine("The following files are already uploaded for this package and will be included as downloads:");
                        Output.WriteLine("<table class=\"sbkMySobek_FileTable\">");
                        Output.WriteLine("  <tr>");
                        Output.WriteLine("    <th style=\"width:350px;\">FILENAME</th>");
                        Output.WriteLine("    <th style=\"width:90px;\">SIZE</th>");
                        Output.WriteLine("    <th style=\"width:170px;\">DATE UPLOADED</th>");
                        Output.WriteLine("    <th style=\"width:90px;text-align:center;\">ACTION</th>");
                        Output.WriteLine("  </tr>");

                        int totalFileCount = 0;

                        //Determine the total number of files
                        foreach (string fileKey in download_files.Keys)
                        {
                            // Get this group of files
                            List<string> fileGroup = download_files[fileKey];
                            totalFileCount += fileGroup.Count();
                        }

                        // Step through all the download file groups
                        foreach (string fileKey in download_files.Keys)
                        {
                            // Get this group of files
                            List<string> fileGroup = download_files[fileKey];

                            // Add each individual file
                            foreach (string thisFile in fileGroup)
                            {
                                file_counter++;

                                // Add the file name literal
                                FileInfo fileInfo = new FileInfo(userInProcessDirectory + "\\" + thisFile);
                                Output.WriteLine("  <tr>");
                                Output.WriteLine("    <td>" + fileInfo.Name + "</td>");
                                if (fileInfo.Length < 1024)
                                    Output.WriteLine("    <td>" + fileInfo.Length + "</td>");
                                else
                                {
                                    if (fileInfo.Length < (1024 * 1024))
                                        Output.WriteLine("    <td>" + (fileInfo.Length / 1024) + " KB</td>");
                                    else
                                        Output.WriteLine("    <td>" + (fileInfo.Length / (1024 * 1024)) + " MB</td>");
                                }

                                Output.WriteLine("    <td>" + fileInfo.LastWriteTime + "</td>");

                                //add by Keven:replace single & double quote with ascII characters
                                string strFileName = fileInfo.Name;
                                if (strFileName.Contains("'") || strFileName.Contains("\""))
                                {
                                    strFileName = strFileName.Replace("'", "\\&#39;");
                                    strFileName = strFileName.Replace("\"", "\\&#34;");
                                }
                                Output.WriteLine("    <td style=\"text-align:center\"> <span class=\"sbkMySobek_ActionLink\">( <a href=\"\" onclick=\"return file_delete('" + strFileName + "');\">delete</a> )</span></td>");

                                Output.WriteLine("  </tr>");
                            }

                            // Now add the row to include the label
                            string input_name = "upload_label" + file_counter.ToString();
                            Output.WriteLine("  <tr>");
                            Output.WriteLine("    <td style=\"text-align:right; color:gray;\">Label:</td>");
                            Output.WriteLine("    <td colspan=\"4\">");
                            Output.WriteLine("      <input type=\"hidden\" id=\"upload_file" + file_counter.ToString() + "\" name=\"upload_file" + file_counter.ToString() + "\" value=\"" + fileKey + "\" />");
                            if (HttpContext.Current.Session["file_" + fileKey] == null)
                            {
                                Output.WriteLine("      <input type=\"text\" class=\"sbkNgi_UploadFileLabel sbk_Focusable\" id=\"" + input_name + "\" name=\"" + input_name + "\" onchange=\"upload_label_fieldChanged(this.id," + totalFileCount + ");\"></input>");
                            }
                            else
                            {
                                string label_from_session = HttpContext.Current.Session["file_" + fileKey].ToString();
                                Output.WriteLine("      <input type=\"text\" class=\"sbkNgi_UploadFileLabel sbk_Focusable\" id=\"" + input_name + "\" name=\"" + input_name + "\" value=\"" + label_from_session + "\" onchange=\"upload_label_fieldChanged(this.id," + totalFileCount + ");\"></input>");
                            }
                            Output.WriteLine("    </td>");
                            Output.WriteLine("  </tr>");
                            Output.WriteLine("  <tr><td class=\"sbkMySobek_FileTableRule\" colspan=\"4\"></td></tr>");
                        }
                        Output.WriteLine("</table>");
                    }
                }

                if ((completeTemplate.Upload_Types == CompleteTemplate.Template_Upload_Types.File_or_URL) || (completeTemplate.Upload_Types == CompleteTemplate.Template_Upload_Types.URL))
                {
                    Output.WriteLine("Enter a URL for this digital resource:");
                    Output.WriteLine("<blockquote>");
                    Output.WriteLine("<input type=\"text\" class=\"upload_url_input\" id=\"url_input\" name=\"url_input\" value=\"" + HttpUtility.HtmlEncode(item.Bib_Info.Location.Other_URL) + "\" ></input>");
                    Output.WriteLine("</blockquote>");
                }

                string completion_message;
                switch (completeTemplate.Upload_Types)
                {
                    case CompleteTemplate.Template_Upload_Types.URL:
                        completion_message = "Once the URL is entered, press SUBMIT to finish this item.";
                        break;

                    case CompleteTemplate.Template_Upload_Types.File_or_URL:
                        completion_message = "Once you enter any files and/or URL, press SUBMIT to finish this item.";
                        break;

                    case CompleteTemplate.Template_Upload_Types.File:
                        completion_message = "Once all files are uploaded, press SUBMIT to finish this item.";
                        break;

                    default:
                        completion_message = "Once complete, press SUBMIT to finish this item.";
                        break;
                }


                Output.WriteLine("<div class=\"sbkMySobek_FileRightButtons\">");
                Output.WriteLine("      <button onclick=\"return new_upload_next_phase(" + (completeTemplate.InputPages.Count + 4) + ");\" class=\"sbkMySobek_BigButton\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_LeftImg\" alt=\"\" /> BACK </button> &nbsp; &nbsp; ");
                Output.WriteLine("      <button onclick=\"return new_upload_next_phase(9);\" class=\"sbkMySobek_BigButton\"> SUBMIT <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_RightImg\" alt=\"\" /></button>");
                Output.WriteLine("      <div id=\"circular_progress\" name=\"circular_progress\" class=\"hidden_progress\">&nbsp;</div>");
                Output.WriteLine("</div>");
                Output.WriteLine();

                Output.WriteLine("<div class=\"sbkMySobek_FileCompletionMsg\">" + completion_message + "</div>");
                Output.WriteLine();
                Output.WriteLine("<br />");
                Output.WriteLine("</div>");
            }

            #endregion

            if (currentProcessStep == 9)
            {
                add_congratulations_html(Output, Tracer);
            }
        }

        #endregion

        #region Code to create the regular citation string

        /// <summary> Returns the basic information about this digital resource in standard format </summary>
        /// <param name="Include_Links"> Flag tells whether to include the search links from this citation view </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> HTML string with the basic information about this digital resource for display </returns>
        public string Standard_Citation_String(bool Include_Links, Custom_Tracer Tracer)
        {
            Navigation_Object CurrentRequest = RequestSpecificValues.Current_Mode;

            // Compute the URL to use for all searches from the citation
            Display_Mode_Enum lastMode = CurrentRequest.Mode;
            CurrentRequest.Mode = Display_Mode_Enum.Results;
            CurrentRequest.Search_Type = Search_Type_Enum.Advanced;
            CurrentRequest.Search_String = "<%VALUE%>";
            CurrentRequest.Search_Fields = "<%CODE%>";
            string search_link = "<a href=\"" + UrlWriterHelper.Redirect_URL(CurrentRequest).Replace("&", "&amp;").Replace("%3c%25", "<%").Replace("%25%3e", "%>").Replace("<%VALUE%>", "&quot;<%VALUE%>&quot;") + "\" target=\"_BLANK\">";
            string search_link_end = "</a>";
            CurrentRequest.Aggregation = String.Empty;
            CurrentRequest.Search_String = String.Empty;
            CurrentRequest.Search_Fields = String.Empty;
            CurrentRequest.Mode = lastMode;

            // If no search links should should be included, clear the search strings
            if (!Include_Links)
            {
                search_link = String.Empty;
                search_link_end = String.Empty;
            }


            if (Tracer != null)
            {
                Tracer.Add_Trace("Citation_Standard_ItemViewer.Standard_Citation_String", "Configuring brief item data into standard citation format");
            }

            // Use string builder to build this
            const string INDENT = "    ";
            StringBuilder result = new StringBuilder();

            // Need to convert this current item to a brief item
            BriefItemInfo BriefItem = BriefItem_Factory.Create(item, Tracer); 

            // Now, try to add the thumbnail from any page images here
            if (BriefItem.Behaviors.Dark_Flag != true)
            {
                string name_for_image = HttpUtility.HtmlEncode(BriefItem.Title);

                if (!String.IsNullOrEmpty(BriefItem.Behaviors.Main_Thumbnail))
                {

                    result.AppendLine();
                    result.AppendLine(INDENT + "<div id=\"Sbk_CivThumbnailDiv\"><a href=\"" + CurrentRequest.Base_URL + BriefItem.BibID + "/" + BriefItem.VID + "\" ><img src=\"" + BriefItem.Web.Source_URL + "/" + BriefItem.Behaviors.Main_Thumbnail + "\" alt=\"" + name_for_image + "\" id=\"Sbk_CivThumbnailImg\" itemprop=\"primaryImageOfPage\" /></a></div>");
                    result.AppendLine();
                }
                else if ((BriefItem.Images != null) && (BriefItem.Images.Count > 0))
                {
                    if (BriefItem.Images[0].Files.Count > 0)
                    {
                        string jpeg = String.Empty;
                        foreach (BriefItem_File thisFileInfo in BriefItem.Images[0].Files)
                        {
                            if (thisFileInfo.Name.ToLower().IndexOf(".jpg") > 0)
                            {
                                if (jpeg.Length == 0)
                                    jpeg = thisFileInfo.Name;
                                else if (thisFileInfo.Name.ToLower().IndexOf("thm.jpg") < 0)
                                    jpeg = thisFileInfo.Name;
                            }
                        }

                        string name_of_page = BriefItem.Images[0].Label;
                        name_for_image = name_for_image + " - " + HttpUtility.HtmlEncode(name_of_page);


                        // If a jpeg was found, show it
                        if (jpeg.Length > 0)
                        {
                            result.AppendLine();
                            result.AppendLine(INDENT + "<div id=\"Sbk_CivThumbnailDiv\"><a href=\"" + CurrentRequest.Base_URL + BriefItem.BibID + "/" + BriefItem.VID + "\" ><img src=\"" + BriefItem.Web.Source_URL + "/" + jpeg + "\" alt=\"" + name_for_image + "\" id=\"Sbk_CivThumbnailImg\" itemprop=\"primaryImageOfPage\" /></a></div>");
                            result.AppendLine();
                        }
                    }
                }
            }

            // Step through the citation configuration here
            CitationSet citationSet = UI_ApplicationCache_Gateway.Configuration.UI.CitationViewer.Get_CitationSet();
            foreach (CitationFieldSet fieldsSet in citationSet.FieldSets)
            {
                // Check to see if any of the values indicated in this field set exist
                bool foundExistingData = false;
                foreach (CitationElement thisField in fieldsSet.Elements)
                {
                    // Was this a custom writer?
                    if ((thisField.SectionWriter != null) && (!String.IsNullOrWhiteSpace(thisField.SectionWriter.Class_Name)))
                    {
                        // Try to get the section writer
                        iCitationSectionWriter sectionWriter = SectionWriter_Factory.GetSectionWriter(thisField.SectionWriter.Assembly, thisField.SectionWriter.Class_Name);

                        // If it was found and there is data, then we found some
                        if ((sectionWriter != null) && (sectionWriter.Has_Data_To_Write(thisField, BriefItem)))
                        {
                            foundExistingData = true;
                            break;
                        }
                    }
                    else // Not a custom writer
                    {
                        // Look for a match in the item description
                        BriefItem_DescriptiveTerm briefTerm = BriefItem.Get_Description(thisField.MetadataTerm);

                        // If no match, just continue
                        if ((briefTerm != null) && (briefTerm.Values.Count > 0))
                        {
                            foundExistingData = true;
                            break;
                        }
                    }
                }

                // If no data was found to put in this field set, skip it
                if (!foundExistingData)
                    continue;

                // Start this section
                result.AppendLine(INDENT + "<div class=\"sbkCiv_CitationSection\" id=\"sbkCiv_" + fieldsSet.ID.Replace(" ", "_") + "Section\" >");
                if (!String.IsNullOrEmpty(fieldsSet.Heading))
                {
                    result.AppendLine(INDENT + "<h2>" + UI_ApplicationCache_Gateway.Translation.Get_Translation(fieldsSet.Heading, CurrentRequest.Language) + "</h2>");
                }
                result.AppendLine(INDENT + "  <dl>");

                // Step through all the fields in this field set and write them
                foreach (CitationElement thisField in fieldsSet.Elements)
                {
                    // Was this a custom writer?
                    if ((thisField.SectionWriter != null) && (!String.IsNullOrWhiteSpace(thisField.SectionWriter.Class_Name)))
                    {
                        // Try to get the section writer
                        iCitationSectionWriter sectionWriter = SectionWriter_Factory.GetSectionWriter(thisField.SectionWriter.Assembly, thisField.SectionWriter.Class_Name);

                        // If it was found and there is data, then we found some
                        if ((sectionWriter != null) && (sectionWriter.Has_Data_To_Write(thisField, BriefItem)))
                        {
                            sectionWriter.Write_Citation_Section(thisField, result, BriefItem, 180, search_link, search_link_end, Tracer, CurrentRequest);
                        }
                    }
                    else // Not a custom writer
                    {

                        // Look for a match in the item description
                        BriefItem_DescriptiveTerm briefTerm = BriefItem.Get_Description(thisField.MetadataTerm);

                        // If no match, just continue
                        if ((briefTerm == null) || (briefTerm.Values.Count == 0))
                            continue;

                        // If they can all be listed one after the other do so now
                        if (!thisField.IndividualFields)
                        {
                            List<string> valueArray = new List<string>();
                            foreach (BriefItem_DescTermValue thisValue in briefTerm.Values)
                            {
                                if (!String.IsNullOrEmpty(thisField.SearchCode))
                                {
                                    // It is possible a different search term is valid for this item, so check it
                                    string searchTerm = (!String.IsNullOrWhiteSpace(thisValue.SearchTerm)) ? thisValue.SearchTerm : thisValue.Value;

                                    if (String.IsNullOrEmpty(thisField.ItemProp))
                                    {
                                        if (String.IsNullOrEmpty(thisValue.Authority))
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Language))
                                            {
                                                valueArray.Add(search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end);
                                            }
                                            else
                                            {
                                                valueArray.Add(search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end + " ( " + thisValue.Language + " )");
                                            }
                                        }
                                        else
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Language))
                                            {
                                                valueArray.Add(search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end + " ( " + thisValue.Authority + " )");
                                            }
                                            else
                                            {
                                                valueArray.Add(search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end + " ( " + thisValue.Authority + ", " + thisValue.Language + " )");
                                            }
                                        }

                                    }
                                    else
                                    {
                                        if (String.IsNullOrEmpty(thisValue.Authority))
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Language))
                                            {
                                                valueArray.Add("<span itemprop=\"" + thisField.ItemProp + "\">" + search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end + "</span>");
                                            }
                                            else
                                            {
                                                valueArray.Add("<span itemprop=\"" + thisField.ItemProp + "\">" + search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end + " ( " + thisValue.Language + " )" + "</span>");
                                            }
                                        }
                                        else
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Language))
                                            {
                                                valueArray.Add("<span itemprop=\"" + thisField.ItemProp + "\">" + search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end + " ( " + thisValue.Authority + " )" + "</span>");
                                            }
                                            else
                                            {
                                                valueArray.Add("<span itemprop=\"" + thisField.ItemProp + "\">" + search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end + " ( " + thisValue.Authority + ", " + thisValue.Language + " )" + "</span>");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (String.IsNullOrEmpty(thisField.ItemProp))
                                    {
                                        if (String.IsNullOrEmpty(thisValue.Authority))
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Language))
                                            {
                                                valueArray.Add(display_text_from_value(thisValue.Value));
                                            }
                                            else
                                            {
                                                valueArray.Add(display_text_from_value(thisValue.Value) + " ( " + thisValue.Language + " )");
                                            }
                                        }
                                        else
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Language))
                                            {
                                                valueArray.Add(display_text_from_value(thisValue.Value) + " ( " + thisValue.Authority + " )");
                                            }
                                            else
                                            {
                                                valueArray.Add(display_text_from_value(thisValue.Value) + " ( " + thisValue.Authority + ", " + thisValue.Language + " )");
                                            }
                                        }

                                    }
                                    else
                                    {
                                        if (String.IsNullOrEmpty(thisValue.Authority))
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Language))
                                            {
                                                valueArray.Add("<span itemprop=\"" + thisField.ItemProp + "\">" + display_text_from_value(thisValue.Value) + "</span>");
                                            }
                                            else
                                            {
                                                valueArray.Add("<span itemprop=\"" + thisField.ItemProp + "\">" + display_text_from_value(thisValue.Value) + " ( " + thisValue.Language + " )" + "</span>");
                                            }
                                        }
                                        else
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Language))
                                            {
                                                valueArray.Add("<span itemprop=\"" + thisField.ItemProp + "\">" + display_text_from_value(thisValue.Value) + " ( " + thisValue.Authority + " )" + "</span>");
                                            }
                                            else
                                            {
                                                valueArray.Add("<span itemprop=\"" + thisField.ItemProp + "\">" + display_text_from_value(thisValue.Value) + " ( " + thisValue.Authority + ", " + thisValue.Language + " )" + "</span>");
                                            }
                                        }
                                    }
                                }
                            }

                            // Now, add this to the citation HTML
                            Add_Citation_HTML_Rows(thisField.DisplayTerm, valueArray, INDENT, result);
                        }
                        else
                        {
                            // In this case, each individual value gets its own citation html row
                            foreach (BriefItem_DescTermValue thisValue in briefTerm.Values)
                            {
                                // Determine the label
                                string label = thisField.DisplayTerm;
                                if (thisField.OverrideDisplayTerm == CitationElement_OverrideDispayTerm_Enum.subterm)
                                {
                                    if (!String.IsNullOrEmpty(thisValue.SubTerm))
                                        label = thisValue.SubTerm;
                                }

                                // It is possible a different search term is valid for this item, so check it
                                string searchTerm = (!String.IsNullOrWhiteSpace(thisValue.SearchTerm)) ? thisValue.SearchTerm : thisValue.Value;

                                if (!String.IsNullOrEmpty(thisField.SearchCode))
                                {
                                    if (String.IsNullOrEmpty(thisField.ItemProp))
                                    {
                                        if (String.IsNullOrEmpty(thisValue.Authority))
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Language))
                                            {
                                                result.Append(Single_Citation_HTML_Row(label, search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end, INDENT));
                                            }
                                            else
                                            {
                                                result.Append(Single_Citation_HTML_Row(label, search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end + " ( " + thisValue.Language + " )", INDENT));
                                            }
                                        }
                                        else
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Language))
                                            {
                                                result.Append(Single_Citation_HTML_Row(label, search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end + " ( " + thisValue.Authority + " )", INDENT));
                                            }
                                            else
                                            {
                                                result.Append(Single_Citation_HTML_Row(label, search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end + " ( " + thisValue.Authority + ", " + thisValue.Language + " )", INDENT));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (String.IsNullOrEmpty(thisValue.Authority))
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Language))
                                            {
                                                result.Append(Single_Citation_HTML_Row(label, "<span itemprop=\"" + thisField.ItemProp + "\">" + search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end + "</span>", INDENT));
                                            }
                                            else
                                            {
                                                result.Append(Single_Citation_HTML_Row(label, "<span itemprop=\"" + thisField.ItemProp + "\">" + search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end + " ( " + thisValue.Language + " )" + "</span>", INDENT));
                                            }
                                        }
                                        else
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Language))
                                            {
                                                result.Append(Single_Citation_HTML_Row(label, "<span itemprop=\"" + thisField.ItemProp + "\">" + search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end + " ( " + thisValue.Authority + " )" + "</span>", INDENT));
                                            }
                                            else
                                            {
                                                result.Append(Single_Citation_HTML_Row(label, "<span itemprop=\"" + thisField.ItemProp + "\">" + search_link.Replace("<%VALUE%>", search_link_from_value(searchTerm)).Replace("<%CODE%>", thisField.SearchCode) + display_text_from_value(thisValue.Value) + search_link_end + " ( " + thisValue.Authority + ", " + thisValue.Language + " )" + "</span>", INDENT));
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Since this isn't tied to a search code, we won't build a URL.  But the
                                    // data could still HAVE a URL associated with it.
                                    if ((thisValue.URIs == null) || (thisValue.URIs.Count == 0))
                                    {
                                        if (String.IsNullOrEmpty(thisField.ItemProp))
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Authority))
                                            {
                                                if (String.IsNullOrEmpty(thisValue.Language))
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, display_text_from_value(thisValue.Value), INDENT));
                                                }
                                                else
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, display_text_from_value(thisValue.Value) + " ( " + thisValue.Language + " )", INDENT));
                                                }
                                            }
                                            else
                                            {
                                                if (String.IsNullOrEmpty(thisValue.Language))
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, display_text_from_value(thisValue.Value) + " ( " + thisValue.Authority + " )", INDENT));
                                                }
                                                else
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, display_text_from_value(thisValue.Value) + " ( " + thisValue.Authority + ", " + thisValue.Language + " )", INDENT));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Authority))
                                            {
                                                if (String.IsNullOrEmpty(thisValue.Language))
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, "<span itemprop=\"" + thisField.ItemProp + "\">" + display_text_from_value(thisValue.Value) + "</span>", INDENT));
                                                }
                                                else
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, "<span itemprop=\"" + thisField.ItemProp + "\">" + display_text_from_value(thisValue.Value) + " ( " + thisValue.Language + " )" + "</span>", INDENT));
                                                }
                                            }
                                            else
                                            {
                                                if (String.IsNullOrEmpty(thisValue.Language))
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, "<span itemprop=\"" + thisField.ItemProp + "\">" + display_text_from_value(thisValue.Value) + " ( " + thisValue.Authority + " )" + "</span>", INDENT));
                                                }
                                                else
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, "<span itemprop=\"" + thisField.ItemProp + "\">" + display_text_from_value(thisValue.Value) + " ( " + thisValue.Authority + ", " + thisValue.Language + " )" + "</span>", INDENT));
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // This has a URI
                                        if (String.IsNullOrEmpty(thisField.ItemProp))
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Authority))
                                            {
                                                if (String.IsNullOrEmpty(thisValue.Language))
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, "<a href=\"" + thisValue.URIs[0] + "\">" + display_text_from_value(thisValue.Value) + "</a>", INDENT));
                                                }
                                                else
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, "<a href=\"" + thisValue.URIs[0] + "\">" + display_text_from_value(thisValue.Value) + "</a>" + " ( " + thisValue.Language + " )", INDENT));
                                                }
                                            }
                                            else
                                            {
                                                if (String.IsNullOrEmpty(thisValue.Language))
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, "<a href=\"" + thisValue.URIs[0] + "\">" + display_text_from_value(thisValue.Value) + "</a>" + " ( " + thisValue.Authority + " )", INDENT));
                                                }
                                                else
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, "<a href=\"" + thisValue.URIs[0] + "\">" + display_text_from_value(thisValue.Value) + "</a>" + " ( " + thisValue.Authority + ", " + thisValue.Language + " )", INDENT));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (String.IsNullOrEmpty(thisValue.Authority))
                                            {
                                                if (String.IsNullOrEmpty(thisValue.Language))
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, "<span itemprop=\"" + thisField.ItemProp + "\">" + "<a href=\"" + thisValue.URIs[0] + "\">" + display_text_from_value(thisValue.Value) + "</a>" + "</span>", INDENT));
                                                }
                                                else
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, "<span itemprop=\"" + thisField.ItemProp + "\">" + "<a href=\"" + thisValue.URIs[0] + "\">" + display_text_from_value(thisValue.Value) + "</a>" + " ( " + thisValue.Language + " )" + "</span>", INDENT));
                                                }
                                            }
                                            else
                                            {
                                                if (String.IsNullOrEmpty(thisValue.Language))
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, "<span itemprop=\"" + thisField.ItemProp + "\">" + "<a href=\"" + thisValue.URIs[0] + "\">" + display_text_from_value(thisValue.Value) + "</a>" + " ( " + thisValue.Authority + " )" + "</span>", INDENT));
                                                }
                                                else
                                                {
                                                    result.Append(Single_Citation_HTML_Row(label, "<span itemprop=\"" + thisField.ItemProp + "\">" + "<a href=\"" + thisValue.URIs[0] + "\">" + display_text_from_value(thisValue.Value) + "</a>" + " ( " + thisValue.Authority + ", " + thisValue.Language + " )" + "</span>", INDENT));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // End this division
                result.AppendLine(INDENT + "  </dl>");
                result.AppendLine(INDENT + "</div>");
            }

            result.AppendLine(INDENT + "<br />");
            result.AppendLine("</div>");

            // Return the built string
            return result.ToString();
        }

        private static string display_text_from_value(string Value)
        {
            return HttpUtility.HtmlEncode(Value).Replace("&lt;i&gt;", "<i>").Replace("&lt;/i&gt;", "</i>");
        }

        private static string search_link_from_value(string Value)
        {
            string replacedValue = Value.Replace("&amp;", "&").Replace("&", "").Replace("  ", " ");
            string urlEncode = HttpUtility.UrlEncode(replacedValue);
            return urlEncode != null ? urlEncode.Replace(",", "").Replace("&amp;", "&").Replace("&", "").Replace(" ", "+") : String.Empty;
        }

        private void Add_Citation_HTML_Rows(string Row_Name, List<string> Values, string Indent, StringBuilder Results)
        {
            // Only add if there is a value
            if (Values.Count <= 0) return;

            Results.Append(Indent + "    <dt class=\"sbk_Civ" + Row_Name.ToUpper().Replace(" ", "_") + "_Element\" style=\"width:180px;\" >");

            // Add with proper language
            Results.Append(UI_ApplicationCache_Gateway.Translation.Get_Translation(Row_Name, RequestSpecificValues.Current_Mode.Language));

            Results.AppendLine(": </dt>");
            Results.Append(Indent + "    <dd class=\"sbk_Civ" + Row_Name.ToUpper().Replace(" ", "_") + "_Element\" style=\"margin-left:180px;\">");
            bool first = true;
            foreach (string thisValue in Values.Where(ThisValue => ThisValue.Length > 0))
            {
                if (first)
                {
                    Results.Append(thisValue);
                    first = false;
                }
                else
                {
                    Results.Append("<br />" + thisValue);
                }
            }
            Results.AppendLine("</dd>");
            Results.AppendLine();
        }

        private string Single_Citation_HTML_Row(string Row_Name, string Value, string Indent)
        {
            // Only add if there is a value
            if (Value.Length > 0)
            {
                return Indent + "    <dt class=\"sbk_Civ" + Row_Name.ToUpper().Replace(" ", "_") + "_Element\" style=\"width:180px;\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation(Row_Name, RequestSpecificValues.Current_Mode.Language) + ": </dt>" + Environment.NewLine + Indent + "    <dd class=\"sbk_Civ" + Row_Name.ToUpper().Replace(" ", "_") + "_Element\" style=\"margin-left:180px;\">" + Value + "</dd>" + Environment.NewLine + Environment.NewLine;
            }
            return String.Empty;
        }

        #endregion

        /// <summary> Add controls directly to the form in the main control area placeholder </summary>
        /// <param name="MainPlaceHolder"> Main place holder to which all main controls are added </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Add_Controls(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("New_TEI_MySobekViewer.Add_Controls", String.Empty);

            // Do nothing if this is the very last step
            if (currentProcessStep == 2)
            {
                // Add the upload controls to the file place holder
                add_upload_controls_tei(MainPlaceHolder, "", ".xml", Tracer);
            }

            if  (currentProcessStep == 8)
            {
                // Add the upload controls to the file place holder
                add_upload_controls(MainPlaceHolder, "Add a new related file for this package:", UI_ApplicationCache_Gateway.Settings.Resources.Upload_Image_Types + "," + UI_ApplicationCache_Gateway.Settings.Resources.Upload_File_Types, Tracer);
            }
        }

        #region Step 3: Upload Related Files

        private void add_upload_controls_tei(PlaceHolder MainPlaceholder, string Prompt, string AllowedFileExtensions, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("New_TEI_MySobekViewer.add_upload_controls", String.Empty);

            StringBuilder filesBuilder = new StringBuilder(2000);
            filesBuilder.AppendLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Js + "\" type=\"text/javascript\"></script>");

            if (currentProcessStep == 2) 
            {
                filesBuilder.AppendLine(Prompt);
                filesBuilder.AppendLine("<blockquote>");

                LiteralControl filesLiteral2 = new LiteralControl(filesBuilder.ToString());
                MainPlaceholder.Controls.Add(filesLiteral2);
                filesBuilder.Remove(0, filesBuilder.Length);


                UploadiFiveControl uploadControl = new UploadiFiveControl();
                uploadControl.UploadPath = userInProcessDirectory;
                uploadControl.UploadScript = RequestSpecificValues.Current_Mode.Base_URL + "UploadiFiveFileHandler.ashx";
                uploadControl.AllowedFileExtensions = AllowedFileExtensions;
                uploadControl.SubmitWhenQueueCompletes = true;
                uploadControl.RemoveCompleted = true;
                uploadControl.Swf = Static_Resources_Gateway.Uploadify_Swf;
                uploadControl.RevertToFlashVersion = true;
                uploadControl.QueueSizeLimit = 1;
                uploadControl.ButtonText = "Select TEI File";
                uploadControl.ButtonWidth = 175;
                MainPlaceholder.Controls.Add(uploadControl);

                filesBuilder.AppendLine("</blockquote><br />");
            }

            LiteralControl literal1 = new LiteralControl(filesBuilder.ToString());
            MainPlaceholder.Controls.Add(literal1);
        }

        private void add_upload_controls(PlaceHolder MainPlaceholder, string Prompt, string AllowedFileExtensions, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("New_TEI_MySobekViewer.add_upload_controls", String.Empty);

            StringBuilder filesBuilder = new StringBuilder(2000);
            filesBuilder.AppendLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Js + "\" type=\"text/javascript\"></script>");

            if (( currentProcessStep == 2 ) || ((completeTemplate.Upload_Types == CompleteTemplate.Template_Upload_Types.File) || (completeTemplate.Upload_Types == CompleteTemplate.Template_Upload_Types.File_or_URL)))
            {
                filesBuilder.AppendLine(Prompt);
                filesBuilder.AppendLine("<blockquote>");

                LiteralControl filesLiteral2 = new LiteralControl(filesBuilder.ToString());
                MainPlaceholder.Controls.Add(filesLiteral2);
                filesBuilder.Remove(0, filesBuilder.Length);


                UploadiFiveControl uploadControl = new UploadiFiveControl();
                uploadControl.UploadPath = userInProcessDirectory;
                uploadControl.UploadScript = RequestSpecificValues.Current_Mode.Base_URL + "UploadiFiveFileHandler.ashx";
                uploadControl.AllowedFileExtensions = AllowedFileExtensions;
                uploadControl.SubmitWhenQueueCompletes = true;
                uploadControl.RemoveCompleted = true;
                uploadControl.Swf = Static_Resources_Gateway.Uploadify_Swf;
                uploadControl.RevertToFlashVersion = true;
                MainPlaceholder.Controls.Add(uploadControl);

                filesBuilder.AppendLine("</blockquote><br />");
            }

            LiteralControl literal1 = new LiteralControl(filesBuilder.ToString());
            MainPlaceholder.Controls.Add(literal1);
        }

        #endregion

        #region Step 4 : Congratulations!

        private void add_congratulations_html(TextWriter Output, Custom_Tracer Tracer)
        {

            Tracer.Add_Trace("New_TEI_MySobekViewer.add_congratulations_html", String.Empty);

            Output.WriteLine("<div class=\"SobekHomeText\">");
            Output.WriteLine("<br />");
            if (!criticalErrorEncountered)
            {
                Output.WriteLine("<strong><center><h1>CONGRATULATIONS!</h1></center></strong>");
                Output.WriteLine("<br />");
                Output.WriteLine("Your item has been successfully added to the digital library and will appear immediately.<br />");
                Output.WriteLine("<br />");
                Output.WriteLine("Search indexes may take a couple minutes to build, at which time this item will be discoverable through the search interface.<br />");
                Output.WriteLine("<br />");
                if (RequestSpecificValues.Current_User.Send_Email_On_Submission)
                {
                    Output.WriteLine("An email has been sent to you with the new item information.<br />");
                    Output.WriteLine("<br />");
                }
            }
            else
            {
                Output.WriteLine("<strong><center><h1>Ooops!! We encountered a problem!</h1></center></strong>");
                Output.WriteLine("<br />");
                Output.WriteLine("An email has been sent to the programmer who will attempt to correct your issue.  You should be contacted within the next 24-48 hours regarding this issue.<br />");
                Output.WriteLine("<br />");
            }
            Output.WriteLine("<div style=\"font-size:larger\">");
            Output.WriteLine("<table width=\"700\"><tr><td width=\"100px\">&nbsp;</td>");
            Output.WriteLine("<td>");
            Output.WriteLine("What would you like to do next?<br />");
            Output.WriteLine("<blockquote>");

            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
            Output.WriteLine("<a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">Return to my home</a><br/><br />");

            if (!criticalErrorEncountered)
            {
                Output.WriteLine("<a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "l/" + item.BibID + "/" + item.VID + "\">View this item</a><br/><br />");

                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.New_TEI_Item;
                RequestSpecificValues.Current_Mode.BibID = String.Empty;
                RequestSpecificValues.Current_Mode.VID = String.Empty;
                Output.WriteLine("<a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">Add another TEI</a><br /><br />");
            }
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Folder_Management;
            RequestSpecificValues.Current_Mode.Result_Display_Type = "brief";
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "Submitted Items";
            Output.WriteLine("<a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">View all my submitted items</a><br /><br />");

            Output.WriteLine("</blockquote>");
            Output.WriteLine("</td></tr></table></div>");
            Output.WriteLine("<br />");
            item = null;
            HttpContext.Current.Session["Item"] = null;

            Output.WriteLine("</div>");
        }

        #endregion

        #region Private method for creating a new digital resource

        private void new_item(Custom_Tracer Tracer)
        {
            // Build a new empty METS file
            item = new SobekCM_Item();
            item.Bib_Info.SobekCM_Type_String = String.Empty;

            // Set the source directory first
            item.Source_Directory = userInProcessDirectory;
            string orgcode = RequestSpecificValues.Current_User.Organization_Code;
            if ((orgcode.Length > 0) && (orgcode.ToLower()[0] != 'i'))
            {
                orgcode = "i" + orgcode;
            }

            // Determine if there are multiple source and holdings for this institution
            bool multipleInstitutionsSelectable = false;
            string institutionCode = String.Empty;
            string institutionName = String.Empty;

            foreach (string thisType in UI_ApplicationCache_Gateway.Aggregations.All_Types)
            {
                if (thisType.IndexOf("Institution", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ReadOnlyCollection<Item_Aggregation_Related_Aggregations> matchingAggr = UI_ApplicationCache_Gateway.Aggregations.Aggregations_By_Type(thisType);
                    foreach (Item_Aggregation_Related_Aggregations thisAggr in matchingAggr)
                    {
                        if (thisAggr.Code.Length > 1)
                        {
                            if (institutionCode.Length > 0)
                            {
                                multipleInstitutionsSelectable = true;
                                break;
                            }
                            if ((thisAggr.Code[0] == 'i') || (thisAggr.Code[0] == 'I'))
                            {
                                institutionCode = thisAggr.Code.Substring(1).ToUpper();
                                institutionName = thisAggr.Name;
                            }
                            else
                            {
                                institutionCode = thisAggr.Code.ToUpper();
                                institutionName = thisAggr.Name;
                            }
                        }
                    }
                }
            }

            // If there is no source code, use the users's code or the only option available
            if (item.Bib_Info.Source.Code.Length == 0)
            {
                // If only one option for the user, assign that
                if ((!multipleInstitutionsSelectable) && (institutionCode.Length > 0))
                {
                    item.Bib_Info.Source.Code = institutionCode;
                    item.Bib_Info.Source.Statement = institutionName;
                }
                else
                {
                    item.Bib_Info.Source.Code = orgcode;
                    item.Bib_Info.Source.Statement = RequestSpecificValues.Current_User.Organization;
                }
            }

            // If there is no holding code, use the user's code or the only option avaiable
            if (item.Bib_Info.Location.Holding_Code.Length == 0)
            {
                if ((!multipleInstitutionsSelectable) && (institutionCode.Length > 0))
                {
                    item.Bib_Info.Location.Holding_Code = institutionCode;
                    item.Bib_Info.Location.Holding_Name = institutionName;
                }
                else
                {
                    item.Bib_Info.Location.Holding_Code = orgcode;
                    item.Bib_Info.Location.Holding_Name = RequestSpecificValues.Current_User.Organization;
                }
            }

            // For testing
            //if (item.Bib_Info.Source.Code.Length == 0)
            //{
            //    item.Bib_Info.Source.Code = "iSD";
            //    item.Bib_Info.Source.Statement = "Testing purposes only";
            //}
            //if (item.Bib_Info.Location.Holding_Code.Length == 0)
            //{
            //    item.Bib_Info.Location.Holding_Code = "iSD";
            //    item.Bib_Info.Location.Holding_Name = "Testing purposes only";
            //}
            //item.Bib_Info.Main_Title.Title = "TEI Item";
            item.Bib_Info.Type.MODS_Type = TypeOfResource_MODS_Enum.Mixed_Material;

            // Set some values from the CompleteTemplate
            if (completeTemplate.Include_User_As_Author)
            {
                item.Bib_Info.Main_Entity_Name.Full_Name = RequestSpecificValues.Current_User.Family_Name + ", " + RequestSpecificValues.Current_User.Given_Name;
            }
            item.Behaviors.IP_Restriction_Membership = completeTemplate.Default_Visibility;

            item.VID = "00001";
            item.BibID = "TEMP000001";
            item.METS_Header.Create_Date = DateTime.Now;
            item.METS_Header.Modify_Date = item.METS_Header.Create_Date;
            item.METS_Header.Creator_Individual = RequestSpecificValues.Current_User.Full_Name;
            item.METS_Header.Add_Creator_Org_Notes("Created using online TEI submission form");

            if (RequestSpecificValues.Current_User.Default_Rights.Length > 0)
                item.Bib_Info.Access_Condition.Text = RequestSpecificValues.Current_User.Default_Rights.Replace("[name]", RequestSpecificValues.Current_User.Full_Name).Replace("[year]", DateTime.Now.Year.ToString());
        }

        #endregion


        /// <summary> Navigation type to be displayed (mostly used by the mySobek viewers) </summary>
        /// <value> This returns none since this viewer writes all the necessary navigational elements </value>
        /// <remarks> This is set to NONE if the viewer will write its own navigation and ADMIN if the standard
        /// administrative tabs should be included as well.  </remarks>
        public override MySobek_Admin_Included_Navigation_Enum Standard_Navigation_Type { get { return MySobek_Admin_Included_Navigation_Enum.NONE; } }

        /// <summary> Returns a flag indicating whether the file upload specific holder in the itemNavForm form will be utilized 
        /// for the current request, or if it can be hidden/omitted. </summary>
        /// <value> Sometimes returns TRUE when files can be be uploaded through this viewer </value>
        public override bool Upload_File_Possible
        {
            get
            {
                return (currentProcessStep == 8 || currentProcessStep == 2);
            }
        }

        /// <summary> Write any additional values within the HTML Head of the final served page </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if this should completely override the default added by the admin or mySobek viewer </returns>
        public override bool Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer)
        {
            Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");

            if (currentProcessStep == 4)
            {
                Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Item_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");    
            }

            return false;
        }

        /// <summary> Gets the CSS class of the container that the page is wrapped within </summary>
        public override string Container_CssClass { get { return "container-inner1000"; } }
    }
}



