#region Using directives

using SobekCM.Resource_Object.Bib_Info;
using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace SobekCM.Resource_Object.Behaviors
{
    /// <summary> Stores the serial hierarchy information associated with this resource </summary>
    /// <remarks> Object created by Mark V Sullivan (2006) for University of Florida's Digital Library Center.</remarks>
    [Serializable]
    public class Serial_Info
    {
        private static readonly string[] MonthNames =
        {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };

        private SortedList<int, Single_Serial_Hierarchy> hierarchy;

        /// <summary> Constructor creates a new instance of the Serial_Info class </summary>
        public Serial_Info()
        {
            // Do nothing
        }

        /// <summary> Gets the number of hierarchies in this serial hierarchy </summary>
        public int Count
        {
            get
            {
                return hierarchy == null ? 0 : hierarchy.Count;
            }
        }

        /// <summary> Address a single hierarchy from this collection, by index </summary>
        /// <exception cref="Exception"> Throws a <see cref="Exception"/> if the hierarchy requested does not exist. </exception>
        public Single_Serial_Hierarchy this[int Index]
        {
            get
            {
                // Check that this node exists exists
                if ((hierarchy == null) || (Index >= hierarchy.Count) || (Index < 0))
                    throw new Exception("Requested serial hierarchy #" + Index + " and this serial hierarchy does not exist.");

                // Return the requested hierarchy file
                return hierarchy.Values[Index];
            }
        }

        /// <summary> Clear all the existing hierarchical information </summary>
        public void Clear()
        {
            if (hierarchy != null)
                hierarchy.Clear();
        }

        /// <summary> Add a new level to the serial hierarchy associated with this resource </summary>
        /// <param name="Level">Serial hierarchy level</param>
        /// <param name="Order">Order for this to display along with other items in the same level</param>
        /// <param name="Display">Text to display for this hierarchical level</param>
        public void Add_Hierarchy(int Level, int Order, string Display)
        {
            if (hierarchy == null)
                hierarchy = new SortedList<int, Single_Serial_Hierarchy>();

            if (!hierarchy.ContainsKey(Level))
            {
                hierarchy.Add(Level, new Single_Serial_Hierarchy(Order, Display));
            }
        }

        /// <summary> For a newspaper item, ensures the serial hierarchy reflects the item's date issued </summary>
        /// <param name="Item"> Item to inspect, and possibly update -- <see cref="SobekCM_Item.Bib_Info"/>'s Origin_Info.Date_Issued
        /// should already hold the CURRENT (possibly just-edited) date issued value </param>
        /// <param name="Previous_Date_Issued"> The date issued value prior to this save, if this call is happening as part of
        /// editing an existing item's metadata; pass NULL (the default) for a brand-new item, or any other case where there is no
        /// prior date issued to compare against </param>
        /// <returns> TRUE if the serial hierarchy was populated or regenerated, FALSE if left unchanged </returns>
        /// <remarks> This is the single place newspaper serial hierarchy generation lives, since it must happen consistently from
        /// several call sites: the builder (<see cref="SobekCM.Builder_Library"/>), submitting a new item online
        /// (<see cref="SobekCM.Library.MySobekViewer.New_Group_And_Item_MySobekViewer"/> and Group_Add_Volume_MySobekViewer), and
        /// editing an existing item's metadata (<see cref="SobekCM.Engine_Library.Items.SobekCM_Item_Updater"/>).
        /// <br /><br />
        /// Non-newspaper items are left entirely alone. For a newspaper item with no existing hierarchy, a fresh year/month/day
        /// hierarchy is generated from the current date issued. For a newspaper item that already has a hierarchy, it is only
        /// regenerated when <paramref name="Previous_Date_Issued"/> is supplied, differs from the current date issued, and the
        /// existing hierarchy exactly matches what would have been generated from that previous date -- meaning it was
        /// auto-generated rather than hand-edited. A hand-edited (or otherwise independently set) hierarchy is never touched. </remarks>
        public static bool Synchronize_Newspaper_Hierarchy_With_Date_Issued(SobekCM_Item Item, string Previous_Date_Issued = null)
        {
            if (Item == null)
                return false;

            if (Item.Bib_Info.SobekCM_Type != TypeOfResource_SobekCM_Enum.Newspaper)
                return false;

            Serial_Info serialInfo = Item.Behaviors.Serial_Info;
            string currentDateIssued = Item.Bib_Info.Origin_Info.Date_Issued;

            if (serialInfo.Count == 0)
                return Apply_Date_To_Hierarchy(serialInfo, currentDateIssued);

            // There is already a hierarchy -- only regenerate it if we know the previous date issued, it actually
            // changed, and the existing hierarchy matches what would have been generated from that previous date
            // (meaning it was auto-generated, not hand-edited)
            if (String.IsNullOrEmpty(Previous_Date_Issued))
                return false;

            if (String.Equals(Previous_Date_Issued, currentDateIssued, StringComparison.Ordinal))
                return false;

            if (!Matches_Date(serialInfo, Previous_Date_Issued))
                return false;

            serialInfo.Clear();
            return Apply_Date_To_Hierarchy(serialInfo, currentDateIssued);
        }

        /// <summary> Attempts to break a date issued value down into the year/month-name/day pieces used to build a newspaper's
        /// three-level serial hierarchy </summary>
        private static bool Try_Parse_Hierarchy_Date(string DateIssued, out int Year, out int Month, out string MonthName, out int Day)
        {
            Year = 0;
            Month = 0;
            MonthName = null;
            Day = 0;

            if (String.IsNullOrEmpty(DateIssued))
                return false;

            DateTime asDateTime;
            if (!DateTime.TryParse(DateIssued, out asDateTime))
                return false;

            Year = asDateTime.Year;
            Month = asDateTime.Month;
            MonthName = MonthNames[Month - 1];
            Day = asDateTime.Day;
            return true;
        }

        /// <summary> Builds a year/month-name/day serial hierarchy ( levels 1/2/3 ) from a date issued value </summary>
        /// <returns> TRUE if the date issued value could be parsed and the hierarchy applied, otherwise FALSE </returns>
        private static bool Apply_Date_To_Hierarchy(Serial_Info Hierarchy, string DateIssued)
        {
            int year, month, day;
            string monthName;
            if (!Try_Parse_Hierarchy_Date(DateIssued, out year, out month, out monthName, out day))
                return false;

            Hierarchy.Add_Hierarchy(1, year, year.ToString());
            Hierarchy.Add_Hierarchy(2, month, monthName);
            Hierarchy.Add_Hierarchy(3, day, day.ToString());
            return true;
        }

        /// <summary> Determines whether an existing serial hierarchy is exactly what would have been generated from a given
        /// date issued value, i.e. whether it was auto-generated from that date rather than hand-edited </summary>
        private static bool Matches_Date(Serial_Info Hierarchy, string DateIssued)
        {
            int year, month, day;
            string monthName;
            if (!Try_Parse_Hierarchy_Date(DateIssued, out year, out month, out monthName, out day))
                return false;

            if (Hierarchy.Count != 3)
                return false;

            return (Hierarchy[0].Order == year) && (Hierarchy[0].Display == year.ToString())
                && (Hierarchy[1].Order == month) && (Hierarchy[1].Display == monthName)
                && (Hierarchy[2].Order == day) && (Hierarchy[2].Display == day.ToString());
        }

        /// <summary> Adds the METS formatted XML string for the serial hierarchy information </summary>
        /// <param name="SobekcmNamespace">METS extension schema namespace to use</param>
        /// <param name="Results">Results stream to write the METS-encoded serial information </param>
        internal void Add_METS(string SobekcmNamespace, TextWriter Results)
        {
            if ((hierarchy == null) || (hierarchy.Count == 0))
            {
                return;
            }

            Results.Write("<" + SobekcmNamespace + ":serial>\r\n");
            for (int i = 1; i <= Count; i++)
            {
                Results.Write(hierarchy.Values[i - 1].toMETS(i, SobekcmNamespace) + "\r\n");
            }
            Results.Write("</" + SobekcmNamespace + ":serial>\r\n");
        }

        #region Nested type: Single_Serial_Hierarchy

        /// <summary> Single bit of serial hierarchy data to be associated with this resource </summary>
        /// <remarks> Object created by Mark V Sullivan (2006) for University of Florida's Digital Library Center.</remarks>
        [Serializable]
        public class Single_Serial_Hierarchy : XML_Writing_Base_Type
        {
            private string display;
            private int order;

            /// <summary> Constructor creates a new instance of the Single_Serial_Hierarchy class </summary>
            /// <param name="Order">Order for this to display along with other items in the same level</param>
            /// <param name="Display">Text to display for this hierarchical level</param>
            public Single_Serial_Hierarchy(int Order, string Display)
            {
                order = Order;
                display = Display;
            }

            /// <summary> Gets and sets the order for this to display along with other items in the same level </summary>
            public int Order
            {
                get { return order; }
                set { order = value; }
            }

            /// <summary> Gets or sets the text to display for this hierarchical level </summary>
            public string Display
            {
                get { return display; }
                set { display = value; }
            }

            internal string Display_XML
            {
                get { return Convert_String_To_XML_Safe(display); }
            }

            /// <summary> Returns this single serial hierarchy in METS formatted XML </summary>
            /// <param name="level">Serial hierarchy level for this element</param>
            /// <param name="myNamespace">METS extension schema namespace to use</param>
            /// <returns>METS formatted XML for this single serial hierarchical data</returns>
            internal string toMETS(int level, string myNamespace)
            {
                return "<" + myNamespace + ":SerialHierarchy level=\"" + level + "\" order=\"" + order + "\">" + Convert_String_To_XML_Safe(display) + "</" + myNamespace + ":SerialHierarchy>";
            }
        }

        #endregion
    }
}