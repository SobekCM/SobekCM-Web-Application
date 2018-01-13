using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ClosedXML.Excel;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Helpers.UploadiFive;
using SobekCM.Library.HTML;
using SobekCM.Library.UI;
using SobekCM.Tools;

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Class to import a spreadsheet of metadata into the system </summary>
    public class Import_Spreadsheet_MySobekViewer : abstract_MySobekViewer
    {
        // Page 1: Upload metadata file and choose worksheet (if Excel)
        // Page 2: Column mapping (if excel or CSV)
        // Page 3: Add defaults
        // Page 4: Review and submit

        private enum Import_File_Type_Enum : byte
        {
            None,
            
            Excel,

            CSV,

            Marc21,

            MarcXML
        }

        private readonly string guid;
        private readonly string taskDirectory;
        private readonly string taskUrl;
        private readonly string file_name;
        private readonly int page;
        private readonly Import_File_Type_Enum file_type;

        public Import_Spreadsheet_MySobekViewer(RequestCache RequestSpecificValues) : base(RequestSpecificValues)
        {
            RequestSpecificValues.Tracer.Add_Trace("Import_Spreadsheet_MySobekViewer.Constructor", String.Empty);

            // If the RequestSpecificValues.Current_User cannot submit items, go back
            //if (!RequestSpecificValues.Current_User.Can_Submit)
            //{
            //    RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
            //    UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
            //    return;
            //}

            // Create a new GUID for this task, or get from the session state
            string guid = HttpContext.Current.Session["Import_Data_Current_GUID"] as string;
            if (String.IsNullOrEmpty(guid))
            {
                guid = Guid.NewGuid().ToString();
                HttpContext.Current.Session.Add("Import_Data_Current_GUID", guid);
            }

            // Determine the in process directory for this
            if (RequestSpecificValues.Current_User.ShibbID.Trim().Length > 0)
            {
                taskUrl = RequestSpecificValues.Current_Mode.Base_URL + "mySobek/InProcess/" + RequestSpecificValues.Current_User.ShibbID + "/task/" + guid;
                taskDirectory = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.In_Process_Submission_Location, RequestSpecificValues.Current_User.ShibbID, "task", guid);
            }
            else
            {
                taskUrl = RequestSpecificValues.Current_Mode.Base_URL + "mySobek/InProcess/" + RequestSpecificValues.Current_User.UserName.Replace(".", "").Replace("@", "") + "/task/" + guid;
                taskDirectory = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.In_Process_Submission_Location, RequestSpecificValues.Current_User.UserName.Replace(".", "").Replace("@", ""), "task", guid);
            }

            // Try to determine the current page
            page = 1;
            int.TryParse(RequestSpecificValues.Current_Mode.My_Sobek_SubMode, out page);
            if (page < 1) page = 1;

            // Is there a data file here?
            string extension = null;
            if (Directory.Exists(taskDirectory))
            {
                string[] source_file = SobekCM_File_Utilities.GetFiles(taskDirectory, "*.xls|*.xlsx|*.csv|*.mrc");
                if (source_file.Length > 0)
                {
                    file_name = Path.GetFileName(source_file[0]);
                    extension = Path.GetExtension(source_file[0]);
                }
            }

            // Determine file type
            if ((!String.IsNullOrEmpty(file_name)) && ( !String.IsNullOrEmpty(extension)))
            {
                switch (extension.ToUpper())
                {
                    case ".XLS":
                    case ".XLSX":
                        file_type = Import_File_Type_Enum.Excel;
                        break;

                    case ".CSV":
                    case ".TXT":
                        file_type = Import_File_Type_Enum.CSV;
                        break;

                    case ".MRC":
                        file_type = Import_File_Type_Enum.Marc21;
                        break;

                    case ".XML":
                        file_type = Import_File_Type_Enum.MarcXML;
                        break;
                }
            }
            else
            {
                file_type = Import_File_Type_Enum.None;
            }

            // Handle postback for changing the CompleteTemplate or project
            if (RequestSpecificValues.Current_Mode.isPostBack)
            {
                string action1 = HttpContext.Current.Request.Form["action"];
                if (action1 == "cancel") 
                {
                    if (Directory.Exists(taskDirectory))
                    {
                        string[] allFiles = Directory.GetFiles(taskDirectory);
                        foreach( string thisFile in allFiles )
                            File.Delete(thisFile);
                        Directory.Delete(taskDirectory);
                    }
                    HttpContext.Current.Session["Import_Data_Current_GUID"] = null;
                    HttpContext.Current.Session["Import_Data_Current_Worksheet"] = null;
                    RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                    UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                    return;
                }

                if (action1 == "delete")
                {
                    if (Directory.Exists(taskDirectory))
                    {
                        string file = Path.Combine(taskDirectory, file_name);
                        if ( File.Exists(file))
                            File.Delete(file);
                    }

                    page = 1;
                    file_name = String.Empty;
                    file_type = Import_File_Type_Enum.None;
                    HttpContext.Current.Session["Import_Data_Current_Worksheet"] = null;
                }
            }
        }

        public override string Web_Title
        {
            get { return "Import Spreadsheet"; }
        }

        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            //Output.WriteLine("IMPORT SPREADSHEET STUFF HERE");
        }

        public override void Write_ItemNavForm_Opening(TextWriter Output, Custom_Tracer Tracer)
        {
            // Add the hidden fields first
            Output.WriteLine("<!-- Hidden field is used for postbacks to indicate what to save and reset -->");
            Output.WriteLine("<input type=\"hidden\" id=\"action\" name=\"action\" value=\"\" />");
            Output.WriteLine("<input type=\"hidden\" id=\"phase\" name=\"phase\" value=\"\" />");
            Output.WriteLine("<script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Sobekcm_Admin_Js + "\" ></script>");

            if (page == 1)
            {
                write_upload_page(Output);
            }


        }

        public override void Write_ItemNavForm_Closing(TextWriter Output, Custom_Tracer Tracer)
        {
            if (page == 1)
            {
                Output.WriteLine("<br /><br />");
                Output.WriteLine("        <button onclick=\"return set_hidden_value_postback('action', 'cancel');\" class=\"sbkMySobek_BigButton\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_LeftImg\" alt=\"\" /> CANCEL </button> &nbsp; &nbsp; ");
                Output.WriteLine("<br /><br />");

            }


        }

        private void write_upload_page(TextWriter Output)
        {
            if ( file_type == Import_File_Type_Enum.Excel )
                Output.WriteLine("<h3>Step 1a: Upload a valid data file</h3>");
            else
                Output.WriteLine("<h3>Step 1: Upload a valid data file</h3>");

            if (String.IsNullOrEmpty(file_name))
            {
                Output.WriteLine("Upload an Excel spreadsheet (preferred) or a comma-seperate value (.csv) file to begin the import process.<br /><br />");
            }
            else
            {
                Output.WriteLine("The current file is already uploaded: <a href=\"" + taskUrl + "/" + file_name + "\">" + file_name + "</a>.<br /><br />");
                Output.WriteLine("You can delete this to start over and re-select your data file.");

                Output.WriteLine("        <button onclick=\"return set_hidden_value_postback('action', 'delete');\" class=\"sbkMySobek_BigButton\"> DELETE </button> &nbsp; &nbsp; ");

                if (file_type == Import_File_Type_Enum.Excel)
                {
                    Output.WriteLine("<br /><br />");

                    Output.WriteLine("<h3>Step 1b: Select the worksheet</h3>");

                    

                    List<string> worksheets = excel_get_worksheet_names(Path.Combine(taskDirectory, file_name));

                    if (worksheets == null)
                    {
                        Output.WriteLine("ERROR GETTING WORKSHEET NAMES");
                    }
                    else
                    {
                        Output.WriteLine("Select the worksheet from the Excel workbook.<br />");

                        Output.WriteLine("<select id=\"sbkIsmsv_Worksheet\" name=\"sbkIsmsv_Worksheet\">");

                        Output.WriteLine("<option value=\"-1\" selected=\"selected\"></option>");

                        int i = 0;
                        foreach (string thisSheet in worksheets)
                        {
                            Output.WriteLine("<option value=\"" + i + "\">" + thisSheet + "</option>");
                            i++;
                        }

                        Output.WriteLine("</select>");

                    }

                    Output.WriteLine("<br /><br />");

                }


            }
        }



        public override void Add_Controls(System.Web.UI.WebControls.PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            if ((page == 1) && ( String.IsNullOrEmpty(file_name)))
            {
                Tracer.Add_Trace("Import_Spreadsheet_MySobekViewer.Add_Controls", "Add upload controls for the spreadsheet upload");

                // Add the upload controls to the file place holder
                add_upload_controls(MainPlaceHolder, Tracer);
            }
        }


        private void add_upload_controls(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Import_Spreadsheet_MySobekViewer.add_upload_controls", String.Empty);

            StringBuilder filesBuilder = new StringBuilder(2000);
            filesBuilder.AppendLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Js + "\" type=\"text/javascript\"></script>");
            filesBuilder.AppendLine("Upload an Excel spreadsheet or CSV file:");
            filesBuilder.AppendLine("<blockquote>");

            LiteralControl filesLiteral2 = new LiteralControl(filesBuilder.ToString());
            MainPlaceHolder.Controls.Add(filesLiteral2);
            filesBuilder.Remove(0, filesBuilder.Length);

            UploadiFiveControl uploadControl = new UploadiFiveControl();
            uploadControl.UploadPath = taskDirectory;
            uploadControl.UploadScript = RequestSpecificValues.Current_Mode.Base_URL + "UploadiFiveFileHandler.ashx";
            uploadControl.SubmitWhenQueueCompletes = true;
            uploadControl.RemoveCompleted = true;
            uploadControl.Swf = Static_Resources_Gateway.Uploadify_Swf;
            uploadControl.RevertToFlashVersion = true;
            uploadControl.AllowedFileExtensions = ".xls|.xlsx|.csv";
            MainPlaceHolder.Controls.Add(uploadControl);


            filesBuilder.AppendLine("</blockquote><br />");

            LiteralControl literal1 = new LiteralControl(filesBuilder.ToString());
            MainPlaceHolder.Controls.Add(literal1);
        }

        /// <summary> Returns a flag indicating whether the file upload specific holder in the itemNavForm form will be utilized 
        /// for the current request, or if it can be hidden/omitted. </summary>
        /// <value> Returns TRUE since files can be be uploaded through this viewer </value>
        public override bool Upload_File_Possible { get { return true; } }



        private List<string> excel_get_worksheet_names(string file_name)
        {
            List<string> returnValue = new List<string>();

            try
            {
                XLWorkbook workbook = new XLWorkbook(file_name);
                int worksheets_count = workbook.Worksheets.Count;
                foreach (IXLWorksheet worksheet in workbook.Worksheets)
                {
                    returnValue.Add(worksheet.Name);
                }
            }
            catch (Exception ee)
            {
                return null;
            }

            return returnValue;
        }
    }
}
