using SobekCM.Core.Items;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SobekCM.Library.ItemViewer.Viewers
{
    /// <summary> The volumes of a serial arranged by date, for the calendar view of all issues </summary>
    /// <remarks> Newspapers get their year / month / day hierarchy (and its numeric index values) generated from the
    /// date issued, but older hierarchies were often typed in by hand, so the level text is tried too </remarks>
    public class Issue_Calendar
    {
        /// <summary> Share of the volumes that must have a usable date for the calendar to be offered at all </summary>
        private const double MIN_DATED_SHARE = 0.8;

        private readonly Dictionary<DateTime, List<Item_Hierarchy_Details>> issuesByDate = new Dictionary<DateTime, List<Item_Hierarchy_Details>>();

        /// <summary> Years that have at least one issue, in order </summary>
        public List<int> Years { get; private set; }

        /// <summary> Volumes with no usable date, listed below the calendar </summary>
        public List<Item_Hierarchy_Details> Undated { get; private set; }

        /// <summary> TRUE if enough of the volumes are dated for a calendar to make sense </summary>
        public bool Suitable { get; private set; }

        /// <summary> Arranges these volumes by date </summary>
        /// <param name="Volumes"> The volumes the current user can see, in their existing order </param>
        public Issue_Calendar(IEnumerable<Item_Hierarchy_Details> Volumes)
        {
            Undated = new List<Item_Hierarchy_Details>();
            int total = 0;
            foreach (Item_Hierarchy_Details volume in Volumes)
            {
                total++;
                if (Try_Get_Date(volume, out DateTime date))
                {
                    if (!issuesByDate.TryGetValue(date, out List<Item_Hierarchy_Details> sameDay))
                        issuesByDate[date] = sameDay = new List<Item_Hierarchy_Details>();
                    sameDay.Add(volume);
                }
                else
                {
                    Undated.Add(volume);
                }
            }

            Years = issuesByDate.Keys.Select(D => D.Year).Distinct().OrderBy(Y => Y).ToList();
            Suitable = (issuesByDate.Count > 0) && (total > 1) && (total - Undated.Count >= total * MIN_DATED_SHARE);
        }

        /// <summary> The issues on one day (more than one when there are several editions), or NULL if none </summary>
        public List<Item_Hierarchy_Details> Issues_On(DateTime Date)
        {
            return issuesByDate.TryGetValue(Date.Date, out List<Item_Hierarchy_Details> issues) ? issues : null;
        }

        /// <summary> TRUE if any issue falls in this month </summary>
        public bool Has_Issues_In(int Year, int Month)
        {
            return issuesByDate.Keys.Any(D => (D.Year == Year) && (D.Month == Month));
        }

        /// <summary> Gets the date of one volume, from its hierarchy </summary>
        /// <returns> TRUE if the volume's hierarchy is a real year, month, and day </returns>
        public static bool Try_Get_Date(Item_Hierarchy_Details Volume, out DateTime Date)
        {
            Date = DateTime.MinValue;

            // The index values, as generated from the date issued
            if ((Volume.Level1_Index.HasValue) && (Volume.Level2_Index.HasValue) && (Volume.Level3_Index.HasValue) &&
                (Valid_Date(Volume.Level1_Index.Value, Volume.Level2_Index.Value, Volume.Level3_Index.Value, out Date)))
            {
                return true;
            }

            // Otherwise the level text: a year, then a month name or number, then a day
            return (Int32.TryParse(Volume.Level1_Text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int year)) &&
                   (Try_Parse_Month(Volume.Level2_Text, out int month)) &&
                   (Int32.TryParse(Volume.Level3_Text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int day)) &&
                   (Valid_Date(year, month, day, out Date));
        }

        private static bool Valid_Date(int Year, int Month, int Day, out DateTime Date)
        {
            Date = DateTime.MinValue;
            if ((Year < 1500) || (Year > 2100) || (Month < 1) || (Month > 12) || (Day < 1) || (Day > DateTime.DaysInMonth(Year, Month)))
                return false;

            Date = new DateTime(Year, Month, Day);
            return true;
        }

        private static bool Try_Parse_Month(string Text, out int Month)
        {
            Month = 0;
            if (String.IsNullOrWhiteSpace(Text))
                return false;

            string month = Text.Trim().TrimEnd('.');
            if (Int32.TryParse(month, NumberStyles.Integer, CultureInfo.InvariantCulture, out Month))
                return true;

            // Month names are written in English by the hierarchy generator
            DateTimeFormatInfo english = CultureInfo.InvariantCulture.DateTimeFormat;
            for (int i = 0; i < 12; i++)
            {
                if ((String.Equals(month, english.MonthNames[i], StringComparison.OrdinalIgnoreCase)) ||
                    (String.Equals(month, english.AbbreviatedMonthNames[i], StringComparison.OrdinalIgnoreCase)))
                {
                    Month = i + 1;
                    return true;
                }
            }
            return false;
        }
    }
}
