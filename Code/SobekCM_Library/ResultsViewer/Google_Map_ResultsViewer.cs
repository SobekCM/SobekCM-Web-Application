#region Using directives

using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Core.Search;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

#endregion

namespace SobekCM.Library.ResultsViewer
{
    /// <summary> Results viewer shows the results which have spatial information in Google maps.  </summary>
    /// <remarks> This class extends the abstract class <see cref="abstract_ResultsViewer"/> and implements the 
    /// <see cref="iResultsViewer" /> interface. </remarks>
    public class Google_Map_ResultsViewer : abstract_ResultsViewer
    {
        private int currentResultCount;

        /// <summary> Layout for this view, written once at the top of the results </summary>
        /// <remarks> Kept here rather than in sobekcm.css, which every site loads from the CDN, so a change to this view
        /// ships with it (the same as News_HtmlHelper's banner). The class names are this view's own (sbkMrv_), apart
        /// from each result's metadata list, which reuses the brief view's sbkBrv_SingleResultDescList so a result
        /// reads the same in both views. A point map is bigger than an area map, since it carries several numbered
        /// markers; both shrink to the width of a narrow screen and stay square. </remarks>
        private const string STYLES =
            "<style>\n" +
            "  .sbkMrv_Results { max-width: 1000px; margin: 10px auto; padding: 0 10px; text-align: left; }\n" +
            "  .sbkMrv_Group { display: flex; flex-wrap: wrap; gap: 20px; align-items: flex-start; padding: 15px 0; border-top: 1px solid #cccccc; }\n" +
            "  .sbkMrv_Group:last-child { border-bottom: 1px solid #cccccc; }\n" +
            "  .sbkMrv_Map { flex: 0 0 auto; width: 100%; }\n" +
            "  .sbkMrv_PointGroup .sbkMrv_Map { max-width: 450px; }\n" +
            "  .sbkMrv_AreaGroup .sbkMrv_Map { max-width: 250px; }\n" +
            "  .sbkMrv_MapCanvas { width: 100%; aspect-ratio: 1 / 1; }\n" +
            "  .sbkMrv_Titles { flex: 1 1 300px; min-width: 0; }\n" +
            "  .sbkMrv_GroupNote { color: gray; font-style: italic; text-align: center; margin-bottom: 10px; }\n" +
            "  .sbkMrv_Title { display: flex; gap: 10px; align-items: flex-start; }\n" +
            "  .sbkMrv_Title + .sbkMrv_Title, .sbkMrv_Title + .sbkMrv_GroupNote { margin-top: 12px; padding-top: 12px; border-top: 1px solid #e7e7e7; }\n" +
            "  .sbkMrv_Marker { flex: 0 0 30px; }\n" +
            "  .sbkMrv_Desc { flex: 1 1 auto; min-width: 0; }\n" +
            "</style>";

        private StringBuilder mapScriptHtml;
        private int polyCount;

        /// <summary> Constructor for a new instance of the Google_Map_ResultsViewer class </summary>
        public Google_Map_ResultsViewer() : base()
        {
            // Do nothing
        }

        ///// <summary> Gets the total number of results to display </summary>
        ///// <value> Since results are displayed by geographic information, this returns the number of distinct coordinates in the result set </value>
        //public override int Total_Results
        //{
        //    get
        //    {
        //        return resultTable.Spatial_Info_Count;
        //    }
        //}

        /// <summary> Adds the controls for this result viewer to the place holder on the main form </summary>
        /// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form into which the the bulk of the result viewer's output is displayed</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Sorted tree with the results in hierarchical structure with volumes and issues under the titles and sorted by serial hierarchy </returns>
        public override void Add_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("Map_ResultsWriter.Add_HTML", "Rendering results in map view");

            // If results are null, or no results, return empty string
            if ((PagedResults == null) || (ResultsStats == null) || (ResultsStats.Total_Items <= 0))
                return;

            // Get the text search redirect stem and (writer-adjusted) base url 
            string textRedirectStem = Text_Redirect_Stem;
            string base_url = RequestSpecificValues.Current_Mode.Base_URL;
            if (RequestSpecificValues.Current_Mode.Writer_Type == Writer_Codes.HTML_LoggedIn)
                base_url = RequestSpecificValues.Current_Mode.Base_URL + "l/";

            // Start the HTML.  The layout is plain CSS classes (below) rather than fixed-width tables, so the
            // maps and their lists wrap onto one column on a narrow screen, and a skin can restyle them.
            var builder = new StringBuilder();
            builder.AppendLine(STYLES);
            builder.AppendLine("<section class=\"sbkMrv_Results\">");

            // Set some values prior to stepping through all the coordinates to display
            polyCount = 1;
            currentResultCount = 0;
            int map_number = 1;
            string coords = String.Empty;

            // Start to create the first map html
            mapScriptHtml = new StringBuilder();

            mapScriptHtml.AppendLine("<script async defer src=\"https://maps.googleapis.com/maps/api/js?key=" + UI_ApplicationCache_Gateway.Settings.System.Google_Map_API_Key + "&callback=initMap\" type=\"text/javascript\"></script>");

            // Google calls initMap (the callback named above) once the API has loaded, which builds every map on
            // the page.  This function used to be called load(), which nothing called, so no map ever drew.
            mapScriptHtml.AppendLine("<script type=\"text/javascript\">");
            mapScriptHtml.AppendLine("  //<![CDATA[");
            mapScriptHtml.AppendLine("  function initMap() {");

            var titles_for_current_map = new List<iSearch_Title_Result>();


            // Step through and add each item to the result set
            // All of the item rows to be displayed together are collected first, and then 
            // the information and google map are rendered.
            foreach (iSearch_Title_Result titleResult in PagedResults)
            {
                // If this new spatial does not match the last spatial, need to close out the last coordiante
                // and render all the HTML and map script information
                // This happens for each area, although points are lumped together
                string thisCoords = spatial_coordinates(titleResult);
                if ((titles_for_current_map.Count > 0) && (thisCoords != coords))
                {
                    if ((thisCoords.Length == 0) || (coords.Length == 0) || (thisCoords[0] == 'A') || (thisCoords[0] != coords[0]))
                    {
                        // Write the information
                        Add_Item_Info_And_Map(textRedirectStem, base_url, map_number, titles_for_current_map, Output, builder);

                        // Get ready for the next item
                        map_number++;
                        titles_for_current_map.Clear();
                    }
                }

                // Add this title for the current (possibly new) map
                titles_for_current_map.Add(titleResult);

                // Just making sure the coordinate and bib id really reflect this last item
                // before going to the next item
                coords = thisCoords;
            }

            // Again, check for left over collected item rows
            if (titles_for_current_map.Count > 0)
            {
                // Write the information
                Add_Item_Info_And_Map(textRedirectStem, base_url, map_number, titles_for_current_map, Output, builder);
            }

            // Close out the results
            builder.AppendLine("</section>");

            // End the map script
            mapScriptHtml.AppendLine("  }");
            mapScriptHtml.AppendLine("  //]]>");
            mapScriptHtml.AppendLine("</script>");

            // Write to output
            Output.Write(Restore_Role_Markup(builder.ToString()) + mapScriptHtml.ToString());
        }

        private void Add_Item_Info_And_Map(string TextRedirectStem, string BaseURL, int MapNumber, List<iSearch_Title_Result> TitlesForCurrentMap, TextWriter Output, StringBuilder Builder)
        {
            // Step through each collection of items by bib id for this coordinate and see if this is a collection of points
            bool point_collection_map = false;
            bool polygon_map = false;
            string firstCoords = spatial_coordinates(TitlesForCurrentMap[0]);
            if (firstCoords.Length > 0)
            {
                if (firstCoords[0] == 'P')
                {
                    point_collection_map = true;
                }
                else
                {
                    polygon_map = true;
                }
            }

            // Start this group: its map (unless it has no coordinates), then its list of titles
            string group_class = point_collection_map ? "sbkMrv_PointGroup" : (polygon_map ? "sbkMrv_AreaGroup" : "sbkMrv_NoCoordinatesGroup");
            Builder.AppendLine("\t<section class=\"sbkMrv_Group " + group_class + "\">");
            if ((point_collection_map) || (polygon_map))
            {
                Builder.AppendLine("\t\t<div class=\"sbkMrv_Map\"><div id=\"map" + MapNumber + "\" class=\"sbkMrv_MapCanvas\"></div></div>");
            }
            Builder.AppendLine("\t\t<div class=\"sbkMrv_Titles\">");

            // Put a note here about the number of matches sharing this area, or having no coordinates at all
            if ((!point_collection_map) && (TitlesForCurrentMap.Count > 1))
            {
                string shared = polygon_map ? "share the same coordinate information" : "have no coordinate information";
                int total_items = TitlesForCurrentMap.Sum(TitleInMap => TitleInMap.Item_Count);
                if (total_items != TitlesForCurrentMap.Count)
                {
                    Builder.AppendLine("\t\t\t<div class=\"sbkMrv_GroupNote\">The following " + total_items + " matches in " + TitlesForCurrentMap.Count + " sets " + shared + "</div>");
                }
                else
                {
                    Builder.AppendLine("\t\t\t<div class=\"sbkMrv_GroupNote\">The following " + total_items + " matches " + shared + "</div>");
                }
            }

            // Now, add all the individual item information for each bib id in this map
            int items_per_this_map = 0;
            string last_link = String.Empty;
            int polygons_added_to_this_map = 0;
            int coordinates_per_this_map = 1;
            string coords = String.Empty;
            foreach (iSearch_Title_Result titleResult in TitlesForCurrentMap)
            {
                // Always get the first item for things like the main link and thumbnail
                iSearch_Item_Result firstItemResult = titleResult.Get_Item(0);
                string internal_link = BaseURL + titleResult.BibID + "/" + firstItemResult.VID + TextRedirectStem;

                // Increment by the number of items in this collection of items
                items_per_this_map += 1;

                // Save this link, just in case it is the only area in this map (clicking the area then opens it)
                if ((!point_collection_map) && (titleResult.Item_Count == 1))
                    last_link = internal_link;

                // On a point map, the first title at each point gets that point's numbered marker, and a note when
                // the titles right after it are at the same point
                string marker_html = String.Empty;
                if (point_collection_map)
                {
                    if (spatial_coordinates(titleResult) != coords)
                    {
                        // Look ahead to see if multiple items have the same coordinate
                        int index = TitlesForCurrentMap.IndexOf(titleResult);
                        int matching_titles_for_this_point = 1;
                        while ((index >= 0) && ((index + 1) < TitlesForCurrentMap.Count))
                        {
                            if (spatial_coordinates(TitlesForCurrentMap[index + 1]) == spatial_coordinates(titleResult))
                            {
                                matching_titles_for_this_point++;
                            }
                            else
                            {
                                break;
                            }
                            index++;
                        }
                        if (matching_titles_for_this_point > 1)
                        {
                            Builder.AppendLine("\t\t\t<div class=\"sbkMrv_GroupNote\">The following " + matching_titles_for_this_point + " titles have the same coordinate point</div>");
                        }

                        marker_html = "<img src=\"" + icon_by_number(coordinates_per_this_map) + "\" alt=\"Map marker " + coordinates_per_this_map + "\" />";
                        coords = spatial_coordinates(titleResult);
                        coordinates_per_this_map++;
                    }
                }

                // Write this title: its marker column (point maps only), then the same description the brief view shows
                Builder.AppendLine("\t\t\t<div class=\"sbkMrv_Title\">");
                if (point_collection_map)
                {
                    Builder.AppendLine("\t\t\t\t<div class=\"sbkMrv_Marker\">" + marker_html + "</div>");
                }
                Builder.AppendLine("\t\t\t\t<div class=\"sbkMrv_Desc\">");
                Append_Result_Description(Builder, titleResult, internal_link, "\t\t\t\t\t");

                // Draw the tree of all matching issues, for a title with several items.  The tree is written straight
                // to the output, so everything collected so far has to be written first.
                if (titleResult.Item_Count > 1)
                {
                    Output.Write(Restore_Role_Markup(Builder.ToString()));
                    Builder.Remove(0, Builder.Length);

                    Add_Issue_Tree(Output, titleResult, currentResultCount, TextRedirectStem, BaseURL);
                }

                Builder.AppendLine("\t\t\t\t</div>");
                Builder.AppendLine("\t\t\t</div>");

                // Increment the row counter
                currentResultCount++;
            }

            // End this group
            Builder.AppendLine("\t\t</div>");
            Builder.AppendLine("\t</section>");

            if ((point_collection_map) || (polygon_map))
            {
                // Clear the last latitude and longitude information
                double max_lat = -90;
                double max_long = -180;
                double min_lat = 90;
                double min_long = 180;

                // Now, start to add the map javascript information to the building javascript
                mapScriptHtml.AppendLine();
                mapScriptHtml.AppendLine("    var map" + MapNumber + "_center = new google.maps.LatLng(<%CENTERINFO" + MapNumber + "%>);");
                mapScriptHtml.AppendLine("    var map" + MapNumber + "_options = { zoom: <%ZOOMINFO" + MapNumber + "%>, center: map" + MapNumber + "_center, mapTypeId: google.maps.MapTypeId.ROADMAP, mapTypeControl: false, streetViewControl: false };");
                mapScriptHtml.AppendLine("    var map" + MapNumber + " = new google.maps.Map(document.getElementById(\"map" + MapNumber + "\"), map" + MapNumber + "_options);");

                // Step through each coordinate/title collection for this map
                int point_index = 1;
                coords = "A";
                foreach (iSearch_Title_Result items_per_bib in TitlesForCurrentMap)
                {
                    // Add this coordinate information to the 
                    if (spatial_coordinates(items_per_bib).Length > 0)
                    {
                        string[] coords_splitter = spatial_coordinates(items_per_bib).Split("|,".ToCharArray());

                        // If this was a point, add this point
                        if (spatial_coordinates(items_per_bib)[0] == 'P')
                        {
                            if (spatial_coordinates(items_per_bib) != coords)
                            {
                                coords = spatial_coordinates(items_per_bib);

                                // Add the marker to the map script
                                mapScriptHtml.AppendLine("    var marker" + MapNumber + "_" + point_index + " = new google.maps.Marker({ position: new google.maps.LatLng(" + coords_splitter[1] + ", " + coords_splitter[2] + "), map: map" + MapNumber + ", icon: \"" + icon_by_number(point_index) + "\" });");
                                point_index++;

                                // Check the new boundaries
                                check_boundaries(coords_splitter[1], coords_splitter[2], ref max_lat, ref max_long, ref min_lat, ref min_long);
                            }
                        }
                        else
                        {
                            if (spatial_coordinates(items_per_bib) != coords)
                            {
                                coords = spatial_coordinates(items_per_bib);
                                if (coords_splitter.Length == 5)
                                {
                                    mapScriptHtml.AppendLine("    var polygon" + polyCount + "_outline = [ new google.maps.LatLng(" + coords_splitter[1] + "," + coords_splitter[2] + "), new google.maps.LatLng(" + coords_splitter[1] + "," + coords_splitter[4] + "), new google.maps.LatLng(" + coords_splitter[3] + "," + coords_splitter[4] + "),  new google.maps.LatLng(" + coords_splitter[3] + "," + coords_splitter[2] + "), new google.maps.LatLng(" + coords_splitter[1] + "," + coords_splitter[2] + ")];");
                                    mapScriptHtml.AppendLine("    var polygon" + polyCount + " = new google.maps.Polygon({ paths: polygon" + polyCount + "_outline, strokeColor: \"#f33f00\", strokeOpacity: 1, strokeWeight: 5, fillColor: \"#ff0000\", fillOpacity: 0.2 });");
                                    mapScriptHtml.AppendLine("    polygon" + polyCount + ".setMap(map" + MapNumber + ");");

                                    check_boundaries(coords_splitter[1], coords_splitter[2], ref max_lat, ref max_long, ref min_lat, ref min_long);
                                    check_boundaries(coords_splitter[3], coords_splitter[4], ref max_lat, ref max_long, ref min_lat, ref min_long);
                                }
                                else
                                {
                                    bool first = true;

                                    mapScriptHtml.Append("    var polygon" + polyCount + "_outline = [ ");

                                    int point = 1;
                                    while ((point + 2) <= coords_splitter.Length)
                                    {
                                        if (!first)
                                            mapScriptHtml.Append(",");
                                        else
                                            first = false;

                                        mapScriptHtml.Append("new google.maps.LatLng(" + coords_splitter[point] + ", " + coords_splitter[point + 1] + ")");
                                        check_boundaries(coords_splitter[point], coords_splitter[point + 1], ref max_lat, ref max_long, ref min_lat, ref min_long);

                                        point += 2;
                                    }
                                    mapScriptHtml.AppendLine("];");
                                    mapScriptHtml.AppendLine("    var polygon" + polyCount + " = new google.maps.Polygon({ paths: polygon" + polyCount + "_outline, strokeColor: \"#f33f00\", strokeOpacity: 1, strokeWeight: 5, fillColor: \"#ff0000\", fillOpacity: 0.2 });");
                                    mapScriptHtml.AppendLine("    polygon" + polyCount + ".setMap(map" + MapNumber + ");");
                                }

                                // Finish the last polygon by adding the link, if there should be one
                                if ((items_per_this_map == 1) && (last_link.Length > 0))
                                {
                                    mapScriptHtml.AppendLine("    google.maps.event.addListener(polygon" + polyCount + ", 'click', function redirect" + polyCount + "() { window.location.href = \"" + last_link + "\"; }); ");
                                }
                                polyCount++;
                                polygons_added_to_this_map++;
                            }
                        }
                    }
                }


                try
                {
                    // Compute the center and zoom of the last map
                    double mid_lat = (max_lat + min_lat) / 2;
                    double mid_long = (max_long + min_long) / 2;
                    int zoom = compute_zoom(max_lat, max_long, min_lat, min_long);
                    if (coords[0] == 'A')
                        zoom--;
                    if ((polygons_added_to_this_map == 0) && (point_index <= 1))
                        zoom = 6;

                    mapScriptHtml.Replace("<%CENTERINFO" + MapNumber + "%>", mid_lat + ", " + mid_long);
                    mapScriptHtml.Replace("<%ZOOMINFO" + MapNumber + "%>", zoom.ToString());
                }
                catch
                {
                    mapScriptHtml.Replace("<%CENTERINFO" + MapNumber + "%>", "0, 0");
                    mapScriptHtml.Replace("<%ZOOMINFO" + MapNumber + "%>", "8");
                }
            }
        }

        /// <summary> Gets the spatial coordinate string for a single title result, returning an empty
        /// string if this title result has no coordinate information </summary>
        /// <param name="TitleResult"> Title result from which to get the spatial coordinates </param>
        /// <returns> Spatial coordinate string, or an empty string </returns>
        /// <remarks> Not every search system populates the spatial coordinates on the title results, so
        /// this is NULL for results which were never assigned any coordinate information </remarks>
        private static string spatial_coordinates(iSearch_Title_Result TitleResult)
        {
            return TitleResult.Spatial_Coordinates ?? String.Empty;
        }

        private static int compute_zoom(double MaxLat, double MaxLong, double MinLat, double MinLong)
        {
            try
            {
                double miles_lat = 69.167 * (MaxLat - MinLat);
                double miles_long = 69.167 * (MaxLong - MinLong);
                double miles = Math.Max(miles_lat, miles_long);
                if (miles < 0.5)
                    return 16;
                if ((miles < 1) && (miles >= 0.5))
                    return 15;
                if ((miles < 2) && (miles >= 1))
                    return 14;
                if ((miles < 3) && (miles >= 2))
                    return 13;
                if ((miles < 7) && (miles >= 3))
                    return 12;
                if ((miles < 15) && (miles >= 7))
                    return 11;
                if ((miles < 30) && (miles >= 15))
                    return 10;
                if ((miles < 60) && (miles >= 30))
                    return 9;
                if ((miles < 120) && (miles >= 60))
                    return 8;
                if ((miles < 240) && (miles >= 120))
                    return 7;
                if ((miles < 480) && (miles >= 240))
                    return 6;
                if ((miles < 960) && (miles >= 480))
                    return 5;
                if ((miles < 2000) && (miles >= 960))
                    return 4;
            }
            catch
            {
                return 2;
            }

            return 2;
        }

        private static void check_boundaries(string Latitude, string Longitude, ref double MaxLat, ref double MaxLong, ref double MinLat, ref double MinLong)
        {
            double point_lat = Convert.ToDouble(Latitude);
            double point_long = Convert.ToDouble(Longitude);
            if (point_long > MaxLong)
                MaxLong = point_long;
            if (point_long < MinLong)
                MinLong = point_long;
            if (point_lat > MaxLat)
                MaxLat = point_lat;
            if (point_lat < MinLat)
                MinLat = point_lat;
        }

        private static string icon_by_number(int IconNumber)
        {
            switch (IconNumber)
            {
                case 1:
                    return "http://www.google.com/mapfiles/markerA.png";

                case 2:
                    return "http://www.google.com/mapfiles/markerB.png";

                case 3:
                    return "http://www.google.com/mapfiles/markerC.png";

                case 4:
                    return "http://www.google.com/mapfiles/markerD.png";

                case 5:
                    return "http://www.google.com/mapfiles/markerE.png";

                case 6:
                    return "http://www.google.com/mapfiles/markerF.png";

                case 7:
                    return "http://www.google.com/mapfiles/markerG.png";

                case 8:
                    return "http://www.google.com/mapfiles/markerH.png";

                case 9:
                    return "http://www.google.com/mapfiles/markerI.png";

                case 10:
                    return "http://www.google.com/mapfiles/markerJ.png";

                default:
                    return "http://www.google.com/mapfiles/markerA.png";
            }
        }

        /// <summary> for some reason I cannot put this in the beta??? </summary>
        /// <param name="sendData">The send data.</param>
        /// <returns></returns>
        public object Process_MapSearch_Callback(string sendData)
        {
            //blank tracer
            var Tracer = new Custom_Tracer();

            #region Process SendData

            //get rid of excess string 
            sendData = sendData.Replace("{\"sendData\": \"", "").Replace("{\"sendData\":\"", "");

            //validate
            if (sendData.Length == 0)
                return "";

            //get the length of incoming message
            int index1 = sendData.LastIndexOf("~", StringComparison.Ordinal);

            //split into each action message
            string[] allActions = sendData.Substring(0, index1).Split('~');

            //hold action type handle

            //go through each item to action and check for ovelrays and item only not pois (ORDER does matter because these will be actiond to db before pois are actiond)
            for (int i = 0; i < allActions.Length; i++)
            {
                //get the length of action message
                int index2 = allActions[i].LastIndexOf("|");
                //split into action elements
                string[] ar = allActions[i].Substring(0, index2).Split('|');
                //determine the action type handle (position 0 in array)
                string actionTypeHandle = ar[0];
                //determine the action type (position 1 in array)
                string actionType = ar[1];
                //based on actionType, parse into objects
                if (actionTypeHandle == "search")
                {
                    //split aggregation incoming subset into an array
                    string[] aggregationList = ar[2].Replace("##", "|").Split('|');
                    //handle action based on type
                    switch (actionType)
                    {
                        case "aggregation":
                            Google_Map_ResultsViewer_Beta.Perform_Aggregation_Search(aggregationList, Tracer, Context);
                            break;
                        case "bounds":
                            //HttpContext.Current.Session["MapSearchResultsKey"] = ar[6];
                            Google_Map_ResultsViewer_Beta.Perform_Coordinate_Bounds_Search(Convert.ToDouble(ar[2]), Convert.ToDouble(ar[3]), Convert.ToDouble(ar[4]), Convert.ToDouble(ar[5]), Context);
                            break;
                        case "filter":
                            //split filterlist incoming subset into an array
                            string[] filterList = ar[3].Replace("###", "|").Split('|');
                            Google_Map_ResultsViewer_Beta.Perform_Filter_Search(filterList);
                            break;
                        case "dateTime":
                            Google_Map_ResultsViewer_Beta.Perform_DateTime_Range_Search(Convert.ToDateTime(ar[3]), Convert.ToDateTime(ar[4]));
                            break;
                        case "coordinate":
                            Google_Map_ResultsViewer_Beta.Perform_Coordinate_Bounds_Search(Convert.ToDouble(ar[2]), Convert.ToDouble(ar[3]), Convert.ToDouble(ar[4]), Convert.ToDouble(ar[5]), Context);
                            //Map_ResultsViewer_Beta.Perform_Coordinate_Bounds_Search(aggregationList, Convert.ToDouble(ar[3]), Convert.ToDouble(ar[4]), Convert.ToDouble(ar[5]), Convert.ToDouble(ar[6]), Tracer);
                            break;
                        case "complete":
                            string[] filterList2 = ar[3].Replace("###", "|").Split('|'); //split filterlist incoming subset into an array
                            Google_Map_ResultsViewer_Beta.Perform_Complete_Search(aggregationList, filterList2, Convert.ToDateTime(ar[4]), Convert.ToDateTime(ar[5]), Convert.ToDouble(ar[6]), Convert.ToDouble(ar[7]), Convert.ToDouble(ar[8]), Convert.ToDouble(ar[9]), Tracer);
                            break;
                    }
                }
            }

            #endregion

            //return HttpContext.Current.Session["SearchResultsJSON"].ToString();
            return Context.Items[RequestCache_Keys.DisplaySearchResults];
        }

    }
}
