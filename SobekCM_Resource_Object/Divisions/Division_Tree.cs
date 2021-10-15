#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

#endregion

namespace SobekCM.Resource_Object.Divisions
{
    /// <summary> Division_Tree is a data object in memory which stores a hierarchy of 
    /// TreeNode objects which represent divisions, pages, and files </summary>
    /// <remarks> Object created by Mark V Sullivan (2006) for University of Florida's Digital Library Center. </remarks>
    [Serializable]
    [DataContract]
    public class Division_Tree : XML_Writing_Base_Type
    {
        /// <summary> Stores the root node for this tree </summary>
        private readonly List<abstract_TreeNode> rootNodes;

        /// <summary> Constructor creates a new instance of the Division_Tree class </summary>
        public Division_Tree()
        {
            rootNodes = new List<abstract_TreeNode>();
        }

        /// <summary> Gets the root nodes for this tree </summary>
        [DataMember]
        public List<abstract_TreeNode> Roots
        {
            get { return rootNodes; }
        }

        /// <summary> Flag indicates if there are any files in this division tree  </summary>
        public bool Has_Files
        {
            get
            {
                foreach (abstract_TreeNode treeNode in rootNodes)
                {
                    if (recursively_check_for_any_files(treeNode))
                        return true;
                }
                return false;
            }
        }

        private bool recursively_check_for_any_files(abstract_TreeNode Node)
        {
            if (Node.Page)
            {
                if (((Page_TreeNode)Node).Files.Count > 0)
                    return true;
            }

            if (!Node.Page)
            {
                Division_TreeNode divNode = (Division_TreeNode)Node;
                foreach (abstract_TreeNode treeNode in divNode.Nodes)
                {
                    if (recursively_check_for_any_files(treeNode))
                        return true;
                }
            }

            return false;
        }

        /// <summary> Clears this tree completely </summary>
        public void Clear()
        {
            rootNodes.Clear();
        }

        /// <summary> Adds a file (with the appropriate divisions and pages) to this tree by filename  </summary>
        /// <param name="FileName"> Name of the file to add </param>
        /// <returns> Newly built <see cref="SobekCM_File_Info" /> object which has been added to this tree </returns>
        /// <remarks> This is generally used to add just a single file.  To add many files, better logic should be implemented </remarks>
        public SobekCM_File_Info Add_File(string FileName)
        {
            return Add_File(FileName, String.Empty);
        }

        /// <summary> Adds a file (with the appropriate divisions and pages) to this tree by filename  </summary>
        /// <param name="FileName"> Name of the file to add </param>
        /// <param name="Label"> Label for the page containing this file, if it is a new page </param>
        /// <returns> Newly built <see cref="SobekCM_File_Info" /> object which has been added to this tree </returns>
        /// <remarks> This is generally used to add just a single file.  To add many files, better logic should be implemented </remarks>
        public SobekCM_File_Info Add_File(string FileName, string Label)
        {
            SobekCM_File_Info newFile = new SobekCM_File_Info(FileName);
            Add_File(newFile, Label);
            return newFile;
        }

        /// <summary> Adds a file  object (with the appropriate divisions and pages) to this tree </summary>
        /// <param name="New_File"> New file object to add </param>
        /// <remarks> This is generally used to add just a single file.  To add many files, better logic should be implemented </remarks>
        public void Add_File(SobekCM_File_Info New_File)
        {
            Add_File(New_File, String.Empty);
        }

        /// <summary> Adds a file  object (with the appropriate divisions and pages) to this tree </summary>
        /// <param name="New_File"> New file object to add </param>
        /// <param name="Label"> Label for the page containing this file, if it is a new page </param>
        /// <remarks> This is generally used to add just a single file.  To add many files, better logic should be implemented </remarks>
        public void Add_File(SobekCM_File_Info New_File, string Label)
        {
            // Determine the upper case name
            string systemname_upper = New_File.File_Name_Sans_Extension;

            // Look for a page/entity which has the same file name, else it will be added to the last division
            foreach (abstract_TreeNode rootNode in Roots)
            {
                if (recursively_add_file(rootNode, New_File, systemname_upper))
                {
                    return;
                }
            }

            // If not found, find the last division
            if (Roots.Count > 0)
            {
                if (!Roots[Roots.Count - 1].Page)
                {
                    // Get his last division
                    Division_TreeNode lastDivision = (Division_TreeNode)Roots[Roots.Count - 1];

                    // Find the last division then
                    while ((lastDivision.Nodes.Count > 0) && (!lastDivision.Nodes[lastDivision.Nodes.Count - 1].Page))
                    {
                        lastDivision = (Division_TreeNode)lastDivision.Nodes[lastDivision.Nodes.Count - 1];
                    }

                    // Add this as a new page on the last division
                    Page_TreeNode newPage = new Page_TreeNode(Label);
                    lastDivision.Add_Child(newPage);

                    // Now, add this file to the page
                    newPage.Files.Add(New_File);
                }
                else
                {
                    // No divisions at all, but pages exist at the top level, which is okay
                    Page_TreeNode pageNode = (Page_TreeNode)Roots[Roots.Count - 1];

                    // Now, add this file to the page
                    pageNode.Files.Add(New_File);
                }
            }
            else
            {
                // No nodes exist, so add a MAIN division node
                Division_TreeNode newDivNode = new Division_TreeNode("Main", String.Empty);
                Roots.Add(newDivNode);

                // Add this as a new page on the new division
                Page_TreeNode newPage = new Page_TreeNode(Label);
                newDivNode.Add_Child(newPage);

                // Now, add this file to the page
                newPage.Files.Add(New_File);
            }
        }

        private bool recursively_add_file(abstract_TreeNode Node, SobekCM_File_Info New_File, string SystemName_Upper)
        {
            // If this is a page, check for a match first
            if (Node.Page)
            {
                Page_TreeNode pageNode = (Page_TreeNode)Node;
                if (pageNode.Files.Count >= 1)
                {
                    if (pageNode.Files[0].File_Name_Sans_Extension == SystemName_Upper)
                    {
                        // Belongs to this page.  Now, just make sure it doesn't already exist
                        foreach (SobekCM_File_Info thisFile in pageNode.Files)
                        {
                            if (thisFile.System_Name.ToUpper() == New_File.System_Name.ToUpper())
                                return true;
                        }

                        // Not found, so add it to this page
                        pageNode.Files.Add(New_File);
                        return true;
                    }
                }
            }

            // If this was a division, check all pages
            if (!Node.Page)
            {
                Division_TreeNode divNode = (Division_TreeNode)Node;
                foreach (abstract_TreeNode childNodes in divNode.Nodes)
                {
                    if (recursively_add_file(childNodes, New_File, SystemName_Upper))
                    {
                        return true;
                    }
                }
            }

            // If nothing found that matches under this node, return false
            return false;
        }

        /// <summary> Returns the page sequence for the indicated file name </summary>
        /// <param name="FileName"> Name of the file to check for similar existence </param>
        /// <returns> The page sequence (first page = index 1), or -1 if it does not exist </returns>
        public int Page_Sequence_By_FileName(string FileName)
        {
            List<abstract_TreeNode> pages = Pages_PreOrder;
            string filename_upper = FileName.ToUpper();
            int page_sequence = 1;
            foreach (Page_TreeNode thisPage in pages)
            {
                if ((thisPage.Files.Count > 0) && (filename_upper == thisPage.Files[0].File_Name_Sans_Extension))
                    return page_sequence;
                page_sequence++;
            }

            return -1;
        }

        #region Methods to return all divisions or the page divisions in preorder 

        /// <summary> Gets all the nodes on the tree in pre-order traversal </summary>
        public List<abstract_TreeNode> Divisions_PreOrder
        {
            get
            {
                // Build the return collection
                List<abstract_TreeNode> returnVal = new List<abstract_TreeNode>();

                // Do the preorder build on each root node
                foreach (abstract_TreeNode rootNode in Roots)
                {
                    preorder_build(returnVal, rootNode, false, false);
                }

                // Return the built collection
                return returnVal;
            }
        }

        /// <summary> Gets all the page nodes on the tree in pre-order traversal </summary>
        public List<abstract_TreeNode> Pages_PreOrder
        {
            get
            {
                // Build the return collection
                List<abstract_TreeNode> returnVal = new List<abstract_TreeNode>();

                // Do the preorder build on each root node
                foreach (abstract_TreeNode rootNode in Roots)
                {
                    preorder_build(returnVal, rootNode, true, false);
                }

                // Return the built collection
                return returnVal;
            }
        }

        /// <summary> Gets all the page nodes on the tree in pre-order traversal, omitting any empty pages </summary>
        public List<abstract_TreeNode> Pages_PreOrder_With_Files
        {
            get
            {
                // Build the return collection
                List<abstract_TreeNode> returnVal = new List<abstract_TreeNode>();

                // Do the preorder build on each root node
                foreach (abstract_TreeNode rootNode in Roots)
                {
                    preorder_build(returnVal, rootNode, true, true);
                }

                // Return the built collection
                return returnVal;
            }
        }

        private void preorder_build(List<abstract_TreeNode> Collection, abstract_TreeNode ThisNode, bool OnlyAddPages, bool PagesMustHaveFiles)
        {
            // Since this is pre-order, first 'visit' this
            if (!OnlyAddPages)
            {
                Collection.Add(ThisNode);
            }
            else
            {
                // If we are just getting pages, only add if it is not already added
                if ((ThisNode.Page) && (!Collection.Contains(ThisNode)))
                {
                    // If you just add all files, just add it here
                    if (!PagesMustHaveFiles)
                        Collection.Add(ThisNode);
                    else
                    {
                        // Must ensure this page has files to add it
                        Page_TreeNode asPage = ThisNode as Page_TreeNode;
                        if (asPage != null)
                        {
                            if ((asPage.Files != null) && (asPage.Files.Count > 0))
                                Collection.Add(asPage);
                        }
                    }

                }
            }

            // is this a division node? .. which can have children ..
            if (!ThisNode.Page)
            {
                Division_TreeNode thisDivNode = (Division_TreeNode)ThisNode;

                // Do the same for all the children
                foreach (abstract_TreeNode childNode in thisDivNode.Nodes)
                {
                    preorder_build(Collection, childNode, OnlyAddPages, PagesMustHaveFiles);
                }
            }
        }

        #endregion

        #region Method to return the list of all files

        /// <summary> Gets the list of all files which belong to this division tree  </summary>
        public List<SobekCM_File_Info> All_Files
        {
            get
            {
                List<SobekCM_File_Info> returnValue = new List<SobekCM_File_Info>();
                List<Page_TreeNode> handledPages = new List<Page_TreeNode>();
                foreach (abstract_TreeNode thisNode in rootNodes)
                    recursively_build_all_files_list(returnValue, handledPages, thisNode);
                return returnValue;
            }
        }

        private void recursively_build_all_files_list(List<SobekCM_File_Info> ReturnValue, List<Page_TreeNode> HandledPages, abstract_TreeNode ThisNode)
        {
            // Since this is pre-order, first 'visit' this
            if (ThisNode.Page)
            {
                Page_TreeNode pageNode = (Page_TreeNode)ThisNode;
                if (!HandledPages.Contains(pageNode))
                {
                    foreach (SobekCM_File_Info file in pageNode.Files)
                    {
                        ReturnValue.Add(file);
                    }
                    HandledPages.Add(pageNode);
                }
            }

            // is this a division node? .. which can have children ..
            if (!ThisNode.Page)
            {
                Division_TreeNode thisDivNode = (Division_TreeNode)ThisNode;

                // Do the same for all the children
                foreach (abstract_TreeNode childNode in thisDivNode.Nodes)
                {
                    recursively_build_all_files_list(ReturnValue, HandledPages, childNode);
                }
            }
        }

        #endregion

        #region Methods that assist with standard METS writing

        /// <summary> Clears all the file and page id's </summary>
        public void Clear_Ids()
        {
            List<abstract_TreeNode> physicalDivisions = Divisions_PreOrder;

            // Clear any existing ID's here since the ID's are only used for writing METS files
            foreach (abstract_TreeNode thisNode in physicalDivisions)
            {
                thisNode.ID = String.Empty;
                thisNode.DMDID = String.Empty;
                thisNode.ADMID = String.Empty;

                // Was this a PAGE or a DIVISION node?
                if (thisNode.Page)
                {
                    // Get this page node
                    Page_TreeNode pageNode = (Page_TreeNode)thisNode;

                    // Only do anything if there are actually any files here
                    if ((pageNode.Files != null) && (pageNode.Files.Count > 0))
                    {
                        // Step through any files under this page
                        foreach (SobekCM_File_Info thisFile in pageNode.Files)
                        {
                            // Set the ADMID and DMDID to empty in preparation for METS writing
                            thisFile.ADMID = String.Empty;
                            thisFile.DMDID = String.Empty;
                        }
                    }
                }
            }
        }

        /// <summary> First, assign group numbers for all the files on each page (physical or other). 
        /// Group numbers in the METS file correspond to files on the same page (physical or other) </summary>
        /// <param name="PageIdPrefix"> Prefix to use on page id's for METS generation </param>
        /// <param name="DivIdPrefix"> Prefix to use on division id's for METS generation </param>
        /// <param name="HasFiles"> OUT parameter indicates if any files were found </param>
        /// <param name="Mimes_to_Exclude">Optionally, provide a list of mimes to exclude</param>
        public void Ensure_Ids_Assigned(string PageIdPrefix, string DivIdPrefix, out bool HasFiles, HashSet<string> Mimes_to_Exclude = null)
        {
            int page_and_group_number = 1;
            int division_number = 1;
            HasFiles = false;
            HashSet<string> fileids_used = new HashSet<string>();

            List<abstract_TreeNode> physicalDivisions = Divisions_PreOrder;

            // First, assign group numbers for all the files on each page (physical or other)
            // Group numbers in the METS file correspond to files on the same page (physical or other)
            // At the same time, we will build the list of all files and files by mime type
            foreach (abstract_TreeNode thisNode in physicalDivisions)
            {
                // If this node was already hit (perhaps if a div has two parent divs), 
                // then skip it
                if (thisNode.ID.Length > 0)
                    continue;

                // Was this a PAGE or a DIVISION node?
                if (thisNode.Page)
                {
                    // Get this page node
                    Page_TreeNode pageNode = (Page_TreeNode)thisNode;

                    // Only do anything if there are actually any files here
                    if ((pageNode.Files != null) && (pageNode.Files.Count > 0))
                    {
                        // Set the page ID here
                        pageNode.ID = PageIdPrefix + page_and_group_number;

                        // Step through any files under this page
                        foreach (SobekCM_File_Info thisFile in pageNode.Files)
                        {
                            // If no file name, skip this
                            if (String.IsNullOrEmpty(thisFile.System_Name))
                                continue;

                            // Get this file extension and MIME type
                            string fileExtension = thisFile.File_Extension;
                            string mimetype = thisFile.MIME_Type(thisFile.File_Extension);

                            // If this is going to be excluded from appearing in the METS file, just skip 
                            // it here as well.
                            if ((Mimes_to_Exclude == null) || (!Mimes_to_Exclude.Contains(mimetype)))
                            {
                                // Set the group number on this file
                                thisFile.Group_Number = "G" + page_and_group_number;

                                // Set the ID for this file as well
                                switch (mimetype)
                                {
                                    case "image/tiff":
                                        thisFile.ID = "TIF" + page_and_group_number;
                                        break;
                                    case "text/plain":
                                        thisFile.ID = "TXT" + page_and_group_number;
                                        break;
                                    case "image/jpeg":
                                        if (thisFile.System_Name.ToLower().IndexOf("thm.jp") > 0)
                                        {
                                            thisFile.ID = "THUMB" + page_and_group_number;
                                            mimetype = mimetype + "-thumbnails";
                                        }
                                        else
                                            thisFile.ID = "JPEG" + page_and_group_number;
                                        break;
                                    case "image/gif":
                                        if (thisFile.System_Name.ToLower().IndexOf("thm.gif") > 0)
                                        {
                                            thisFile.ID = "THUMB" + page_and_group_number;
                                            mimetype = mimetype + "-thumbnails";
                                        }
                                        else
                                            thisFile.ID = "GIF" + page_and_group_number;
                                        break;
                                    case "image/jp2":
                                        thisFile.ID = "JP2" + page_and_group_number;
                                        break;
                                    default:
                                        if (fileExtension.Length > 0)
                                        {
                                            thisFile.ID = fileExtension + page_and_group_number;
                                        }
                                        else
                                        {
                                            thisFile.ID = "NOEXT" + page_and_group_number;
                                        }
                                        break;
                                }

                                // Ensure this fileid is really unique.  It may not be if there are multiple
                                // files of the same mime-type in the same page.  (such as 0001.jpg and 0001.qc.jpg)
                                if (fileids_used.Contains(thisFile.ID))
                                {
                                    int count = 2;
                                    while (fileids_used.Contains(thisFile.ID + "." + count))
                                        count++;
                                    thisFile.ID = thisFile.ID + "." + count;
                                }

                                // Save this file id
                                fileids_used.Add(thisFile.ID);

                                // Also ensure we know there are page image files
                                HasFiles = true;
                            }
                        }

                        // Prepare for the next page
                        page_and_group_number++;
                    }
                    else
                    {
                        // Page has no files, so it should be skipped when written
                        pageNode.ID = "SKIP";
                    }
                }
                else
                {
                    // This node is a DIVISION (non-page)
                    thisNode.ID = DivIdPrefix + division_number;
                    division_number++;
                }
            }
        }

        /// <summary> Collect all the divisions, files, and files by mime type </summary>
        /// <param name="AllDivisions"></param>
        /// <param name="AllFiles"></param>
        /// <param name="MimeHash"></param>
        public void Collect_Divs_And_Files(List<abstract_TreeNode> AllDivisions, List<SobekCM_File_Info> AllFiles, Dictionary<string, List<SobekCM_File_Info>> MimeHash)
        {
            List<abstract_TreeNode> physicalDivisions = Divisions_PreOrder;

            foreach (abstract_TreeNode thisNode in physicalDivisions)
            {
                // Add this to the list of all nodes
                AllDivisions.Add(thisNode);

                // Was this a PAGE or a DIVISION node?
                if (thisNode.Page)
                {
                    // Get this page node
                    Page_TreeNode pageNode = (Page_TreeNode)thisNode;

                    // Only do anything if there are actually any files here
                    if ((pageNode.Files != null) && (pageNode.Files.Count > 0))
                    {
                        // Step through any files under this page
                        foreach (SobekCM_File_Info thisFile in pageNode.Files)
                        {
                            // If no file name or ID, skip this
                            if ((String.IsNullOrEmpty(thisFile.System_Name)) || (String.IsNullOrEmpty(thisFile.ID)))                               
                                continue;

                            // Get this file extension and MIME type
                            string fileExtension = thisFile.File_Extension;
                            string mimetype = thisFile.MIME_Type(thisFile.File_Extension);

                            if (thisFile.ID.IndexOf("THUMB") == 0 )
                                mimetype = mimetype + "-thumbnails";

                            // Also add to the list of files
                            AllFiles.Add(thisFile);

                            // If this is a new MIME type, add it, else just save this file in the MIME hash
                            if (!MimeHash.ContainsKey(mimetype))
                            {
                                List<SobekCM_File_Info> newList = new List<SobekCM_File_Info> { thisFile };
                                MimeHash[mimetype] = newList;
                            }
                            else
                            {
                                MimeHash[mimetype].Add(thisFile);
                            }
                        }
                    }
                }
            }
        }

        /// <summary> Write the structure map for this division tree </summary>
        /// <param name="Output_Stream"></param>
        /// <param name="MainTitle"></param>
        /// <param name="ID"></param>
        /// <param name="Type"></param>
        /// <param name="DmdSecIds"></param>
        /// <param name="AmdSecIds"></param>
        /// <param name="OuterDivisions"></param>
        public void Write_METS(TextWriter Output_Stream, string MainTitle, string ID, string Type, string DmdSecIds, string AmdSecIds, List<Outer_Division_Info> OuterDivisions)
        {
            Dictionary<abstract_TreeNode, int> pages_to_appearances = new Dictionary<abstract_TreeNode, int>();

            Output_Stream.WriteLine("<METS:structMap ID=\"" + ID + "\" TYPE=\"" + Type + "\">");

            // Add any outer divisions here
            if ((OuterDivisions != null ) && (OuterDivisions.Count > 0 ))
            {
                foreach (Outer_Division_Info outerDiv in OuterDivisions)
                {
                    Output_Stream.Write("<METS:div");
                    if (DmdSecIds.Length > 0)
                    {
                        Output_Stream.Write(" DMDID=\"" + DmdSecIds + "\"");
                    }
                    if (AmdSecIds.Length > 0)
                    {
                        Output_Stream.Write(" ADMID=\"" + AmdSecIds + "\"");
                    }
                    if (outerDiv.Label.Length > 0)
                    {
                        Output_Stream.Write(" LABEL=\"" + Convert_String_To_XML_Safe_Static(outerDiv.Label) + "\"");
                    }
                    if (outerDiv.OrderLabel > 0)
                    {
                        Output_Stream.Write(" ORDERLABEL=\"" + outerDiv.OrderLabel + "\"");
                    }
                    if (outerDiv.Type.Length > 0)
                    {
                        Output_Stream.Write(" TYPE=\"" + Convert_String_To_XML_Safe_Static(outerDiv.Type) + "\"");
                    }
                    Output_Stream.WriteLine(">");
                }
            }
            else
            {
                // Start the main division information
                Output_Stream.Write("<METS:div");
                if (DmdSecIds.Length > 0)
                {
                    Output_Stream.Write(" DMDID=\"" + DmdSecIds + "\"");
                }
                if (AmdSecIds.Length > 0)
                {
                    Output_Stream.Write(" ADMID=\"" + AmdSecIds + "\"");
                }

                // Add the title, if one was provided
                if (MainTitle.Length > 0)
                {
                    Output_Stream.Write(" LABEL=\"" + Convert_String_To_XML_Safe_Static(MainTitle) + "\"");
                }

                // Finish out this first, main division tag
                Output_Stream.WriteLine(" ORDER=\"0\" TYPE=\"main\">");
            }

            // Add all the divisions recursively
            int order = 1;
            foreach (abstract_TreeNode thisRoot in Roots)
            {
                recursively_add_div_info(thisRoot, Output_Stream, pages_to_appearances, order++);
            }

            // Close any outer divisions here
            if (( OuterDivisions != null ) && ( OuterDivisions.Count > 0))
            {
                for (int index = 0; index < OuterDivisions.Count; index++)
                {
                    Output_Stream.WriteLine("</METS:div>");
                }
            }
            else
            {
                // Close out the main division tag
                Output_Stream.WriteLine("</METS:div>");
            }

            // Close out this structure map portion
            Output_Stream.WriteLine("</METS:structMap>");
        }


        private void recursively_add_div_info(abstract_TreeNode ThisNode, TextWriter Results, Dictionary<abstract_TreeNode, int> PagesToAppearances, int Order)
        {
            // Add the div information for this node first
            if (ThisNode.Page)
            {
                // If the ID of this page is SKIP, then just return and do nothing here
                if (ThisNode.ID == "SKIP")
                    return;

                if (PagesToAppearances.ContainsKey(ThisNode))
                {
                    PagesToAppearances[ThisNode] = PagesToAppearances[ThisNode] + 1;
                    Results.Write("<METS:div ID=\"" + ThisNode.ID + "_repeat" + PagesToAppearances[ThisNode] + "\"");
                }
                else
                {
                    PagesToAppearances[ThisNode] = 1;
                    Results.Write("<METS:div ID=\"" + ThisNode.ID + "\"");
                }
            }
            else
            {
                Results.Write("<METS:div ID=\"" + ThisNode.ID + "\"");
            }

            // Add links to dmd secs and amd secs
            if (!String.IsNullOrEmpty(ThisNode.DMDID))
            {
                Results.Write(" DMDID=\"" + ThisNode.DMDID + "\"");
            }
            if (!String.IsNullOrEmpty(ThisNode.ADMID))
            {
                Results.Write(" ADMID=\"" + ThisNode.ADMID + "\"");
            }

            // Add the label, if there is one
            if ((ThisNode.Label.Length > 0) && (ThisNode.Label != ThisNode.Type))
                Results.Write(" LABEL=\"" + Convert_String_To_XML_Safe(ThisNode.Label) + "\"");

            // Finish the start div label for this division
            Results.WriteLine(" ORDER=\"" + Order + "\" TYPE=\"" + ThisNode.Type + "\">");

            // If this is a page, add all the files, otherwise call this method recursively
            if (ThisNode.Page)
            {
                // Add each file
                Page_TreeNode thisPage = (Page_TreeNode)ThisNode;
                foreach (SobekCM_File_Info thisFile in thisPage.Files)
                {
                    // Add the file pointer informatino
                    if (thisFile.ID.Length > 0)
                        Results.WriteLine("<METS:fptr FILEID=\"" + thisFile.ID + "\" />");
                }
            }
            else
            {
                // Call this method for each subdivision
                int inner_order = 1;
                Division_TreeNode thisDivision = (Division_TreeNode)ThisNode;
                foreach (abstract_TreeNode thisSubDivision in thisDivision.Nodes)
                {
                    recursively_add_div_info(thisSubDivision, Results, PagesToAppearances, inner_order++);
                }
            }

            // Close out this division
            Results.WriteLine("</METS:div>");
        }

        #endregion
    }
}