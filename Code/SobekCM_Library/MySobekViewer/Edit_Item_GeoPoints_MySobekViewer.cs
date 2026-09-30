#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.AdminViewer;
using SobekCM.Library.HTML;
using SobekCM.Library.Localization;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Divisions;
using SobekCM.Resource_Object.Metadata_Modules;
using SobekCM.Resource_Object.Metadata_Modules.GeoSpatial;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Lets an editor place one location point for the whole item, and/or one point per page </summary>
    public class Edit_Item_GeoPoints_MySobekViewer : abstract_MySobekViewer
    {
        private const string TRACE = "Edit_Item_GeoPoints_MySobekViewer";
        private const string HIDE_HELP_SETTING = "Edit_Item_GeoPoints_MySobekViewer:Hide Help";

        private readonly SobekCM_Item currentItem;
        private readonly string message;
        private readonly bool messageIsError;

        /// <summary> Constructor for a new instance of the Edit_Item_GeoPoints_MySobekViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="Context"> HTTP context for the current request </param>
        public Edit_Item_GeoPoints_MySobekViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace(TRACE + ".Constructor", String.Empty);

            currentItem = GeoSpatial_Edit_Helper.Load_Editable_Item(RequestSpecificValues, Context, TRACE + ".Constructor");
            if (currentItem == null)
                return;

            string language = RequestSpecificValues.Current_Mode.Language;

            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                // The help dialog's "don't show this again" box, posted in the background so the editor stays put
                if (GeoSpatial_Edit_Helper.Handle_Help_Preference(RequestSpecificValues, Context, HIDE_HELP_SETTING))
                    return;

                string action = Context.Request.Form["action"];
                if (action == "cancel")
                {
                    GeoSpatial_Edit_Helper.Exit_To_Item(currentItem, RequestSpecificValues, Context);
                    return;
                }

                if (action == "save")
                {
                    var payload = GeoSpatial_Edit_Helper.Parse_Payload<Points_Payload>(Context.Request.Form["geo_payload"]);
                    var result = Apply_Changes(payload) ? GeoSpatial_Edit_Helper.Save_Item_Geo(currentItem, RequestSpecificValues, TRACE + ".Constructor") : GeoSpatial_Edit_Helper.Save_Result.Failed;
                    if (result != GeoSpatial_Edit_Helper.Save_Result.Failed)
                    {
                        string saved = result == GeoSpatial_Edit_Helper.Save_Result.Saved ? "1" : "2";
                        Context.Response.Redirect(UrlWriterHelper.Add_Query_Param(UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode), "saved", saved));
                        return;
                    }

                    message = Localization_Gateway.GeoSpatial_Edit.Save_Error(language);
                    messageIsError = true;
                }
            }
            else if (Context.Request.Query["saved"] == "1")
            {
                message = Localization_Gateway.GeoSpatial_Edit.Save_Success(language);
            }
            else if (Context.Request.Query["saved"] == "2")
            {
                message = Localization_Gateway.GeoSpatial_Edit.Save_Index_Warning(language);
                messageIsError = true;
            }
        }

        /// <summary> Validates every posted change first, then applies them all, so a bad payload changes nothing </summary>
        private bool Apply_Changes(Points_Payload Payload)
        {
            if (Payload?.Changes == null)
                return false;

            List<Page_TreeNode> pages = GeoSpatial_Edit_Helper.Get_Pages(currentItem);
            foreach (Point_Change change in Payload.Changes)
            {
                if ((change == null) || (change.Index < 0) || (change.Index > pages.Count))
                    return false;
                if ((!change.Clear) && (!GeoSpatial_Edit_Helper.Valid_Coordinate(change.Lat, change.Lng)))
                    return false;
            }

            string language = RequestSpecificValues.Current_Mode.Language;
            foreach (Point_Change change in Payload.Changes)
            {
                iMetadataDescribable node = change.Index == 0 ? currentItem : pages[change.Index - 1];
                GeoSpatial_Information geo = GeoSpatial_Edit_Helper.Get_Geo(node, !change.Clear);
                if (geo == null)
                    continue;

                geo.Clear_NonPOIPoints();
                if (change.Clear)
                    continue;

                string label = change.Index == 0 ? currentItem.Bib_Info.Main_Title.Title : GeoSpatial_Edit_Helper.Page_Label(pages[change.Index - 1], change.Index, language);
                geo.Add_Point(new Coordinate_Point(Math.Round(change.Lat, 7), Math.Round(change.Lng, 7), label, GeoSpatial_Edit_Helper.MAIN_FEATURE_TYPE));
            }

            return true;
        }

        /// <summary> Returns the item or page's current (non-POI) point, if it has one </summary>
        private static Coordinate_Point Existing_Point(iMetadataDescribable Node)
        {
            GeoSpatial_Information geo = GeoSpatial_Edit_Helper.Get_Geo(Node, false);
            if (geo?.Points == null)
                return null;

            foreach (Coordinate_Point point in geo.Points)
            {
                if (point.FeatureType != "poi")
                    return point;
            }
            return null;
        }

        /// <summary> Title for the page that displays this viewer </summary>
        public override string Web_Title => Localization_Gateway.GeoSpatial_Edit.Points_Page_Title(RequestSpecificValues.Current_Mode.Language);

        /// <summary> This viewer writes its own navigation </summary>
        public override MySobek_Admin_Included_Navigation_Enum Standard_Navigation_Type => MySobek_Admin_Included_Navigation_Enum.NONE;

        /// <summary> Wide layout container </summary>
        public override string Container_CssClass => "sbkGeo_ContainerInner";

        /// <summary> Mimic the item viewer, without the banner or footer </summary>
        public override List<HtmlSubwriter_Behaviors_Enum> Viewer_Behaviors => new List<HtmlSubwriter_Behaviors_Enum>
        {
            HtmlSubwriter_Behaviors_Enum.MySobek_Subwriter_Mimic_Item_Subwriter,
            HtmlSubwriter_Behaviors_Enum.Suppress_Banner,
            HtmlSubwriter_Behaviors_Enum.Suppress_Footer
        };

        /// <summary> Adds the editor's stylesheets and scripts, plus the Google Maps loader </summary>
        public override bool Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer)
        {
            GeoSpatial_Edit_Helper.Write_Head(Output, Static_Resources_Gateway.Sobekcm_Geo_Points_Js, "SobekGeoPoints.init");
            return true;
        }

        /// <summary> Writes the ribbon, toolbar, and map </summary>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace(TRACE + ".Write_HTML", String.Empty);
            if (currentItem == null)
                return;

            string language = RequestSpecificValues.Current_Mode.Language;
            List<Page_TreeNode> pages = GeoSpatial_Edit_Helper.Get_Pages(currentItem);

            // Index 0 is the whole item; index N is page sequence N
            var tiles = new List<GeoSpatial_Edit_Helper.Ribbon_Tile>();
            var nodes = new List<object>();

            Coordinate_Point itemPoint = Existing_Point(currentItem);
            string itemThumbnail = String.IsNullOrEmpty(currentItem.Behaviors.Main_Thumbnail) ? null : GeoSpatial_Edit_Helper.File_Url(currentItem, currentItem.Behaviors.Main_Thumbnail);
            tiles.Add(new GeoSpatial_Edit_Helper.Ribbon_Tile { Label = Localization_Gateway.GeoSpatial_Edit.Whole_Item(language), ThumbnailUrl = itemThumbnail, HasGeo = itemPoint != null });
            nodes.Add(Node_Data(Localization_Gateway.GeoSpatial_Edit.Whole_Item(language), itemPoint));

            for (int i = 0; i < pages.Count; i++)
            {
                string label = GeoSpatial_Edit_Helper.Page_Label(pages[i], i + 1, language);
                Coordinate_Point pagePoint = Existing_Point(pages[i]);
                tiles.Add(new GeoSpatial_Edit_Helper.Ribbon_Tile { Label = label, ThumbnailUrl = GeoSpatial_Edit_Helper.File_Url(currentItem, GeoSpatial_Edit_Helper.Find_Page_Jpeg(pages[i], true)), HasGeo = pagePoint != null });
                nodes.Add(Node_Data(label, pagePoint));
            }

            Write_Item_Type_Top(Output, currentItem);

            Output.WriteLine("<div class=\"sbkGeo_Editor\" id=\"sbkGeo_Editor\">");

            // The instructions live in a help dialog rather than above the strip, leaving more room for the map
            Write_Help_Dialog(Output, language);
            GeoSpatial_Edit_Helper.Write_Message(Output, message, messageIsError);

            GeoSpatial_Edit_Helper.Write_Ribbon(Output, tiles, false, language);

            Write_ItemNavForm_Opening(Output);
            Output.WriteLine("  <input type=\"hidden\" id=\"sbkGeo_Action\" name=\"action\" value=\"\" />");
            Output.WriteLine("  <input type=\"hidden\" id=\"sbkGeo_Payload\" name=\"geo_payload\" value=\"\" />");

            Output.WriteLine("  <div class=\"sbkGeo_Toolbar\">");
            Output.WriteLine("    <span class=\"sbkGeo_Current\" id=\"sbkGeo_Current\"></span>");
            Output.WriteLine("    <input type=\"text\" class=\"sbkGeo_Search\" id=\"sbkGeo_Search\" placeholder=\"" + System.Net.WebUtility.HtmlEncode(Localization_Gateway.GeoSpatial_Edit.Search_Placeholder(language)) + "\" />");
            Output.WriteLine("    <button type=\"button\" class=\"sbkGeo_Button\" id=\"sbkGeo_SearchButton\">" + Localization_Gateway.GeoSpatial_Edit.Search_Button(language) + "</button>");
            Output.WriteLine("    <button type=\"button\" class=\"sbkGeo_Button\" id=\"sbkGeo_ClearPoint\">" + Localization_Gateway.GeoSpatial_Edit.Clear_Point(language) + "</button>");
            Output.WriteLine("    <span class=\"sbkGeo_ToolbarSpacer\"></span>");
            Output.WriteLine("    " + GeoSpatial_Edit_Helper.Help_Button(language));
            Output.WriteLine("    <button type=\"button\" class=\"sbkPiu_RoundButton\" id=\"sbkGeo_Cancel\">" + Localization_Gateway.Buttons.Exit(language) + "</button>");
            Output.WriteLine("    <button type=\"button\" class=\"sbkPiu_RoundButton\" id=\"sbkGeo_Save\">" + Localization_Gateway.Buttons.Save(language) + "</button>");
            Output.WriteLine("  </div>");
            Output.WriteLine("  <div class=\"sbkGeo_Map\" id=\"sbkGeo_Map\"></div>");
            Write_ItemNavForm_Closing(Output);

            GeoSpatial_Edit_Helper.Write_Json_Block(Output, "sbkGeo_Data", new
            {
                nodes,
                helpHidden = GeoSpatial_Edit_Helper.Help_Hidden(RequestSpecificValues, HIDE_HELP_SETTING),
                strings = new
                {
                    searchNotFound = Localization_Gateway.GeoSpatial_Edit.Search_Not_Found(language),
                    cancel = Localization_Gateway.Buttons.Cancel(language),
                    exit = Localization_Gateway.Buttons.Exit(language),
                    mapUnavailable = Localization_Gateway.GeoSpatial_Edit.Map_Unavailable(language)
                }
            });
            Output.WriteLine("</div>");
        }

        /// <summary> Writes the help dialog, which opens on its own until the user asks not to see it again </summary>
        private static void Write_Help_Dialog(TextWriter Output, string Language)
        {
            // Button names in the help text come from the buttons' own labels, so they always match the screen
            string Label(Func<string, string> Phrase) => GeoSpatial_Edit_Helper.Help_Label(Phrase(Language));

            GeoSpatial_Edit_Helper.Write_Help_Dialog(Output, Localization_Gateway.GeoSpatial_Edit.Points_Page_Title(Language), new[]
            {
                Localization_Gateway.GeoSpatial_Edit.Points_Help_Select(Language),
                String.Format(Localization_Gateway.GeoSpatial_Edit.Points_Help_Find(Language), Label(Localization_Gateway.GeoSpatial_Edit.Search_Button)),
                Localization_Gateway.GeoSpatial_Edit.Points_Help_Place(Language),
                Localization_Gateway.GeoSpatial_Edit.Points_Help_Other(Language),
                String.Format(Localization_Gateway.GeoSpatial_Edit.Points_Help_Clear(Language), Label(Localization_Gateway.GeoSpatial_Edit.Clear_Point)),
                String.Format(Localization_Gateway.GeoSpatial_Edit.Help_Save(Language), Label(Localization_Gateway.Buttons.Save))
            }, Language);
        }

        private static object Node_Data(string Label, Coordinate_Point Point)
        {
            if (Point == null)
                return new { label = Label };
            return new { label = Label, lat = Point.Latitude, lng = Point.Longitude };
        }

        /// <summary> Posted editor payload: only the nodes the user actually changed </summary>
        private class Points_Payload
        {
            public List<Point_Change> Changes { get; set; }
        }

        /// <summary> One changed node: index 0 is the whole item, index N is page sequence N </summary>
        private class Point_Change
        {
            public int Index { get; set; }
            public bool Clear { get; set; }
            public double Lat { get; set; }
            public double Lng { get; set; }
        }
    }
}
