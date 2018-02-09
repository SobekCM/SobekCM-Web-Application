﻿using System;
using System.Collections.Generic;

namespace SobekCM.Core.Results
{
    [Serializable]
    public class v5_Solr_Title_Result : iSearch_Title_Result
    {
        private Search_Result_Item_Tree itemTree;

        /// <summary> Constructor for a new instance of the v5_Solr_Title_Result class </summary>
        public v5_Solr_Title_Result()
        {
            Items = new List<v5_Solr_Item_Result>();
        }

        /// <summary> List of items within this title </summary>
        public List<v5_Solr_Item_Result> Items { get; set; }

        /// <summary> Row number of this title within a larger page of results from the database </summary>
        public int RowNumber { get; set; }

        #region Basic properties the implement the iSearch_Title_Result interface

        /// <summary> Bibliographic identifier (BibID) for this title result </summary>
        public string BibID { get; set; }

        /// <summary> Group title for this title result </summary>
        public string GroupTitle { get; set; }

        /// <summary> Local OPAC cataloging number for this title result </summary>
        public long OPAC_Number { get; set; }

        /// <summary> OCLC cataloging number for this title result </summary>
        public long OCLC_Number { get; set; }

        /// <summary> Group-wide thumbnail for this title result </summary>
        public string GroupThumbnail { get; set; }

        /// <summary> Material type for this title result </summary>
        public string MaterialType { get; set; }

        /// <summary> Type of the primary alternate identifier for this resource ( i.e. 'Accession Number', etc.. )</summary>
        public string Primary_Identifier_Type { get; set; }

        /// <summary> Primary alternate identifier for this resource</summary>
        public string Primary_Identifier { get; set; }

        /// <summary> Spatial coverage for this title result in terms of coordinates for map display </summary>
        public string Spatial_Coordinates { get; set; }

        /// <summary> User notes for this title result, if it is in a bookshelf </summary>
        public string UserNotes { get; set; }

        /// <summary> Highlighted snippet of text from this document </summary>
        public string Snippet { get; set; }

        /// <summary> Metadata values to display for this item title result </summary>
        public string[] Metadata_Display_Values { get; set; }

        #endregion

        #region iSearch_Title_Result Members

        /// <summary> Gets the number of items contained within this title result </summary>
        public int Item_Count
        {
            get { return Items.Count; }
        }

        /// <summary> Gets the item indicated by the provided index </summary>
        /// <param name="Index"> Index of the item requested </param>
        /// <returns> Item result requested, or NULL </returns>
        public iSearch_Item_Result Get_Item(int Index)
        {
            return Items[Index];
        }

        /// <summary> Gets the item tree view used for showing all the items under this title in a tree type html display </summary>
        /// <remarks> This includes intermediary nodes, while the item list only includes the actual items </remarks>
        public Search_Result_Item_Tree Item_Tree
        {
            get
            {
                return itemTree;
            }
        }

        /// <summary> Builds the tree of items under this title, for multiple item titles </summary>
        public void Build_Item_Tree(string ResultsIndex)
        {
            // Create the tree
            itemTree = new Search_Result_Item_Tree();

            // Add a root node
            Search_Result_Item_TreeNode myRootNode = itemTree.Add_Root_Node("ROOT", String.Empty, BibID);

            // Is this a newspaper type (handles display slightly differently)
            string materialType = MaterialType ?? String.Empty;
            bool newspaper = materialType.ToUpper().IndexOf("NEWSPAPER") >= 0;

            // Placeholders for the day that we begin having four levels of serial hierarchy
            string thischildLevel4Text = String.Empty;
            const int THISCHILD_LEVEL4_INDEX = -1;

            // Add each seperate child row to the Items_Within_Title set
            Dictionary<string, Search_Result_Item_TreeNode> level1Nodes = new Dictionary<string, Search_Result_Item_TreeNode>();
            Dictionary<string, Search_Result_Item_TreeNode> level2Nodes = new Dictionary<string, Search_Result_Item_TreeNode>();
            Dictionary<string, Search_Result_Item_TreeNode> level3Nodes = new Dictionary<string, Search_Result_Item_TreeNode>();
            foreach (iSearch_Item_Result thisChild in Items)
            {
                // Determine the final link for this item
                string itemLink = BibID + "/" + thisChild.VID;

                string level1Text = thisChild.Level1_Text;
                string level2Text = thisChild.Level2_Text;
                string level3Text = thisChild.Level3_Text;
                string level4Text = thischildLevel4Text;

                // The logic to add all the nodes for this item.  This is large in an attempt to make it
                // frighteningly quick
                if (level4Text.Length > 0)
                {
                    if (level3Nodes.ContainsKey(level1Text + "_" + level2Text + "_" + level3Text))
                    {
                        level3Nodes[level1Text + "_" + level2Text + "_" + level3Text].Add_Child_Node(level4Text, itemLink, THISCHILD_LEVEL4_INDEX);
                    }
                    else
                    {
                        if (level2Nodes.ContainsKey(level1Text + "_" + level2Text))
                        {
                            level3Nodes[level1Text + "_" + level2Text + "_" + level3Text] = level2Nodes[level1Text + "_" + level2Text].Add_Child_Node(level3Text, String.Empty, thisChild.Level3_Index);
                        }
                        else
                        {
                            if (level1Nodes.ContainsKey(level1Text))
                            {
                                level2Nodes[level1Text + "_" + level2Text] = level1Nodes[level1Text].Add_Child_Node(level2Text, String.Empty, thisChild.Level2_Index);
                            }
                            else
                            {
                                level1Nodes[level1Text] = myRootNode.Add_Child_Node(level1Text, String.Empty, 0);
                                level2Nodes[level1Text + "_" + level2Text] = level1Nodes[level1Text].Add_Child_Node(level2Text, String.Empty, thisChild.Level2_Index);
                            }
                            level3Nodes[level1Text + "_" + level2Text + "_" + level3Text] = level2Nodes[level1Text + "_" + level2Text].Add_Child_Node(level3Text, String.Empty, thisChild.Level3_Index);
                        }
                        level3Nodes[level1Text + "_" + level2Text + "_" + level3Text].Add_Child_Node(level4Text, itemLink, THISCHILD_LEVEL4_INDEX);
                    }
                }
                else
                {
                    if (level3Text.Length > 0)
                    {
                        if ((newspaper) && (level3Text.IndexOf(level2Text) < 0) && (level3Text.IndexOf(level1Text) < 0))
                        {
                            string date = level2Text + " " + level3Text + ", " + level1Text;
                            if (level2Nodes.ContainsKey(level1Text + "_" + level2Text))
                            {
                                level3Nodes[level1Text + "_" + level2Text + "_" + level3Text] = level2Nodes[level1Text + "_" + level2Text].Add_Child_Node(date, itemLink, thisChild.Level3_Index);
                            }
                            else
                            {
                                if (level1Nodes.ContainsKey(level1Text))
                                {
                                    level2Nodes[level1Text + "_" + level2Text] = level1Nodes[level1Text].Add_Child_Node(level2Text, String.Empty, thisChild.Level2_Index);
                                }
                                else
                                {
                                    level1Nodes[level1Text] = myRootNode.Add_Child_Node(level1Text, String.Empty, thisChild.Level1_Index);
                                    level2Nodes[level1Text + "_" + level2Text] = level1Nodes[level1Text].Add_Child_Node(level2Text, String.Empty, thisChild.Level2_Index);
                                }
                                level3Nodes[level1Text + "_" + level2Text + "_" + level3Text] = level2Nodes[level1Text + "_" + level2Text].Add_Child_Node(date, itemLink, thisChild.Level3_Index);
                            }
                        }
                        else
                        {
                            if (level2Nodes.ContainsKey(level1Text + "_" + level2Text))
                            {
                                level3Nodes[level1Text + "_" + level2Text + "_" + level3Text] = level2Nodes[level1Text + "_" + level2Text].Add_Child_Node(level3Text, itemLink, thisChild.Level3_Index);
                            }
                            else
                            {
                                if (level1Nodes.ContainsKey(level1Text))
                                {
                                    level2Nodes[level1Text + "_" + level2Text] = level1Nodes[level1Text].Add_Child_Node(level2Text, String.Empty, thisChild.Level2_Index);
                                }
                                else
                                {
                                    level1Nodes[level1Text] = myRootNode.Add_Child_Node(level1Text, String.Empty, thisChild.Level1_Index);
                                    level2Nodes[level1Text + "_" + level2Text] = level1Nodes[level1Text].Add_Child_Node(level2Text, String.Empty, thisChild.Level2_Index);
                                }
                                level3Nodes[level1Text + "_" + level2Text + "_" + level3Text] = level2Nodes[level1Text + "_" + level2Text].Add_Child_Node(level3Text, itemLink, thisChild.Level3_Index);
                            }
                        }
                    }
                    else
                    {
                        if (level2Text.Length > 0)
                        {
                            if (level1Nodes.ContainsKey(level1Text))
                            {
                                level2Nodes[level1Text + "_" + level2Text] = level1Nodes[level1Text].Add_Child_Node(level2Text, itemLink, thisChild.Level2_Index);
                            }
                            else
                            {
                                level1Nodes[level1Text] = myRootNode.Add_Child_Node(level1Text, String.Empty, thisChild.Level1_Index);
                                level2Nodes[level1Text + "_" + level2Text] = level1Nodes[level1Text].Add_Child_Node(level2Text, itemLink, thisChild.Level2_Index);
                            }
                        }
                        else
                        {
                            if (level1Text.Length > 0)
                            {
                                level1Nodes[level1Text] = myRootNode.Add_Child_Node(level1Text, itemLink, thisChild.Level1_Index);
                            }
                            else
                            {
                                level1Nodes[thisChild.Title] = myRootNode.Add_Child_Node(thisChild.Title, itemLink, 0);
                            }
                        }
                    }
                }
            }

            // Set the hash values, for lookup by id
            itemTree.Set_Values();
        }

        #endregion

        /// <summary> Adds information about a child item under this title result </summary>
        /// <param name="Item_Result"> Display and identification information for a single item within this title </param>
        internal void Add_Item_Result(v5_Solr_Item_Result Item_Result)
        {
            Items.Add(Item_Result);
        }
    }
}
