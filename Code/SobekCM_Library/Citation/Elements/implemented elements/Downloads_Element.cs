#region Using directives

using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Users;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Divisions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

#endregion

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Element allows entry of the downloads for an item </summary>
    /// <remarks> This class extends the <see cref="ComboBox_TextBox_Element"/> class. </remarks>
    public class Downloads_Element : ComboBox_TextBox_Element
    {
        /// <summary> Constructor for a new instance of the Downloads_Element class </summary>
        public Downloads_Element()
            : base("Downloads", "download")
        {
            Repeatable = true;
            PossibleSelectItems.Clear();

            SecondLabel = "Label";

        }


        /// <summary> Renders the HTML for this element </summary>
        /// <param name="Output"> Textwriter to write the HTML for this element </param>
        /// <param name="Bib"> Object to populate this element from </param>
        /// <param name="Skin_Code"> Code for the current skin </param>
        /// <param name="IsMozilla"> Flag indicates if the current browse is Mozilla Firefox (different css choices for some elements)</param>
        /// <param name="PopupFormBuilder"> Builder for any related popup forms for this element </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        /// <remarks> This simple element does not append any popup form to the popup_form_builder</remarks>
        public override void Render_Template_HTML(TextWriter Output, SobekCM_Item Bib, string Skin_Code, bool IsMozilla, StringBuilder PopupFormBuilder, User_Object Current_User, string CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {

            // Check that an acronym exists
            if (Acronym.Length == 0)
            {
                const string defaultAcronym = "Select files to make downloadable and provide labels for them.";
                switch (CurrentLanguage)
                {
                    case "en":
                        Acronym = defaultAcronym;
                        break;

                    case "es":
                        Acronym = defaultAcronym;
                        break;

                    case "fr":
                        Acronym = defaultAcronym;
                        break;

                    default:
                        Acronym = defaultAcronym;
                        break;
                }
            }

            // Clear the list of possible download-eligible files
            PossibleSelectItems.Clear();
            PossibleSelectItems.Add(String.Empty);

            // Add the actual downloads from this package
            var files = new List<string>();
            var labels = new List<string>();
            List<abstract_TreeNode> downloadGroups = Bib.Divisions.Download_Tree.Pages_PreOrder;
            foreach (Page_TreeNode thisDownload in downloadGroups)
            {
                if (thisDownload.Files.Count > 0)
                {
                    string base_file = thisDownload.Files[0].File_Name_Sans_Extension.ToLower();
                    if (!PossibleSelectItems.Contains(base_file + ".*"))
                    {
                        PossibleSelectItems.Add(base_file + ".*");
                    }

                    if (!files.Contains(base_file + ".*"))
                    {
                        files.Add(base_file + ".*");
                        labels.Add(thisDownload.Label);
                    }
                }
            }

            // Now, add all the other download-eligible files
            ReadOnlyCollection<string> otherFiles = Bib.Web.Get_Download_Eligible_Files(UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network + Bib.Web.AssocFilePath);
            foreach (string thisOtherFile in otherFiles)
            {
                if (!PossibleSelectItems.Contains(thisOtherFile))
                    PossibleSelectItems.Add(thisOtherFile);
            }

            if (files.Count == 0)
            {
                render_helper(Output, String.Empty, String.Empty, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL, false);
            }
            else
            {
                render_helper(Output, files, labels, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
            }

        }

        /// <summary> Prepares the bib object for the save, by clearing any existing data in this element's related field(s) </summary>
        /// <param name="Bib"> Existing digital resource object which may already have values for this element's data field(s) </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <remarks> This currently does not to anything, as this element is not fully implemented </remarks>
        public override void Prepare_For_Save(SobekCM_Item Bib, User_Object Current_User)
        {
            // Do nothing, since we will use more advanced logic.
            // If the downloads are the same, we will not disturb the download tree
            // If the downloads are different, any deep download tree will be cleared
            // and the downloads will appear flattened
        }

        /// <summary> Saves the data rendered by this element to the provided bibliographic object during postback </summary>
        /// <param name="Bib"> Object into which to save the user's data, entered into the html rendered by this element </param>
        /// <remarks> This currently does not to anything, as this element is not fully implemented </remarks>
        public override void Save_To_Bib(SobekCM_Item Bib)
        {
            // Collect the list of download_files and download_labels from the form
            var getKeys = Context.Request.Form.Keys;
            string filename = String.Empty;
            var download_files = new List<string>();
            var download_labels = new List<string>();
            foreach (string thisKey in getKeys)
            {
                if (thisKey.IndexOf(html_element_name.Replace("_", "") + "_select") == 0)
                {
                    filename = Context.Request.Form[thisKey];
                }

                if (thisKey.IndexOf(html_element_name.Replace("_", "") + "_text") == 0)
                {
                    if (filename.Length > 0)
                    {
                        string label = Context.Request.Form[thisKey];
                        download_files.Add(filename.Replace(".*", ""));
                        download_labels.Add(label);
                    }
                    filename = String.Empty;
                }
            }

            // Collect the list of download files and download labels from the package
            var existing_files = new List<string>();
            var existing_labels = new List<string>();
            List<abstract_TreeNode> downloadGroups = Bib.Divisions.Download_Tree.Pages_PreOrder;
            foreach (Page_TreeNode thisDownload in downloadGroups)
            {
                if (thisDownload.Files.Count > 0)
                {
                    string base_file = thisDownload.Files[0].File_Name_Sans_Extension;
                    if (!existing_files.Contains(base_file))
                    {
                        existing_files.Add(base_file);
                        existing_labels.Add(thisDownload.Label);
                    }
                }
            }

            // Now, compare the current list of downloads to the new list
            bool different = false;
            if (download_files.Count != existing_files.Count)
            {
                different = true;
            }
            else
            {
                // Same number of downloads, so step through and compare the files and labels
                for (int i = 0; i < download_files.Count; i++)
                {
                    // Get the index of this on the existing list
                    int index = existing_files.IndexOf(download_files[i].ToUpper());
                    if (index < 0)
                    {
                        different = true;
                        break;
                    }
                    if (existing_labels[index].Trim() != download_labels[i].Trim())
                    {
                        different = true;
                        break;
                    }
                }
            }

            // If this was different clear the existing downloads and load the new ones
            if (different)
            {
                // Get the directory for this package
                string directory = UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network + Bib.Web.AssocFilePath;

                // Clear existing
                Bib.Divisions.Download_Tree.Clear();

                // No nodes exist, so add a MAIN division node
                var newDivNode = new Division_TreeNode("Main", String.Empty);
                Bib.Divisions.Download_Tree.Roots.Add(newDivNode);

                // Add a page for each 
                for (int i = 0; i < download_files.Count; i++)
                {
                    // Get the list of matching files
                    string[] files = Directory.GetFiles(directory, download_files[i] + ".*");
                    if (files.Length > 0)
                    {
                        // Add this as a new page on the new division
                        var newPage = new Page_TreeNode(download_labels[i]);
                        newDivNode.Add_Child(newPage);

                        // Add all the files next
                        foreach (SobekCM_File_Info newFile in files.Select(thisFile => (new FileInfo(thisFile)).Name).Select(add_filename => new SobekCM_File_Info(add_filename)))
                        {
                            newPage.Files.Add(newFile);
                        }
                    }
                }
            }
        }
    }
}



