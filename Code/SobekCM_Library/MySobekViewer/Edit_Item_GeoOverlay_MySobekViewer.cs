#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.AdminViewer;
using SobekCM.Library.HTML;
using SobekCM.Library.Localization;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Divisions;
using SobekCM.Resource_Object.Metadata_Modules.GeoSpatial;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Lets an editor georeference each page image: lay it over the map, line it up, and save either
    /// the image's (rotated) outline or a hand-drawn polygon as that page's footprint </summary>
    public class Edit_Item_GeoOverlay_MySobekViewer : abstract_MySobekViewer
    {
        private const string TRACE = "Edit_Item_GeoOverlay_MySobekViewer";
        private const string MODE_RECTANGLE = "rectangle";
        private const string MODE_CUSTOM = "custom";

        private readonly SobekCM_Item currentItem;
        private readonly string message;
        private readonly bool messageIsError;

        /// <summary> Constructor for a new instance of the Edit_Item_GeoOverlay_MySobekViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="Context"> HTTP context for the current request </param>
        public Edit_Item_GeoOverlay_MySobekViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace(TRACE + ".Constructor", String.Empty);

            currentItem = GeoSpatial_Edit_Helper.Load_Editable_Item(RequestSpecificValues, Context, TRACE + ".Constructor");
            if (currentItem == null)
                return;

            string language = RequestSpecificValues.Current_Mode.Language;

            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                string action = Context.Request.Form["action"];
                if (action == "cancel")
                {
                    GeoSpatial_Edit_Helper.Exit_To_Item(currentItem, RequestSpecificValues, Context);
                    return;
                }

                if (action == "save")
                {
                    var payload = GeoSpatial_Edit_Helper.Parse_Payload<Overlay_Payload>(Context.Request.Form["geo_payload"]);
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
        private bool Apply_Changes(Overlay_Payload Payload)
        {
            if (Payload?.Changes == null)
                return false;

            List<Page_TreeNode> pages = GeoSpatial_Edit_Helper.Get_Pages(currentItem);
            foreach (Overlay_Change change in Payload.Changes)
            {
                if ((change == null) || (change.Index < 1) || (change.Index > pages.Count))
                    return false;
                if (change.Clear)
                    continue;

                if ((change.Mode != MODE_RECTANGLE) && (change.Mode != MODE_CUSTOM))
                    return false;
                if ((Double.IsNaN(change.Rotation)) || (Double.IsInfinity(change.Rotation)))
                    return false;
                if ((change.Points == null) || (change.Points.Count > GeoSpatial_Edit_Helper.MAX_POLYGON_POINTS))
                    return false;
                // A very wide image outline carries extra points along its edges, so it can be more than four
                if ((change.Mode == MODE_RECTANGLE) ? change.Points.Count < 4 : change.Points.Count < 3)
                    return false;
                if (change.Points.Any(P => (P == null) || (P.Length != 2) || (!GeoSpatial_Edit_Helper.Valid_Coordinate(P[0], P[1]))))
                    return false;
                if ((change.Image != null) && ((change.Image.Count != 4) || (change.Image.Any(P => (P == null) || (P.Length != 2) || (!GeoSpatial_Edit_Helper.Valid_Coordinate(P[0], P[1]))))))
                    return false;
            }

            string language = RequestSpecificValues.Current_Mode.Language;
            foreach (Overlay_Change change in Payload.Changes)
            {
                Page_TreeNode page = pages[change.Index - 1];
                GeoSpatial_Information geo = GeoSpatial_Edit_Helper.Get_Geo(page, !change.Clear);
                if (geo == null)
                    continue;

                // Only replace this editor's footprint polygon, leaving any page points and legacy POIs alone
                foreach (Coordinate_Polygon existing in geo.Polygons.Where(P => P.FeatureType != "poi").ToList())
                    geo.Clear_Specific_Polygon(existing);

                if (change.Clear)
                    continue;

                double rotation = change.Rotation % 360;
                if (rotation < 0)
                    rotation += 360;

                var polygon = new Coordinate_Polygon
                {
                    Label = GeoSpatial_Edit_Helper.Page_Label(page, change.Index, language),
                    FeatureType = GeoSpatial_Edit_Helper.MAIN_FEATURE_TYPE,
                    PolygonType = change.Mode,
                    Rotation = Math.Round(rotation, 2),
                    Page_Sequence = (ushort)change.Index
                };
                foreach (double[] point in change.Points)
                    polygon.Add_Edge_Point(Math.Round(point[0], 7), Math.Round(point[1], 7));
                polygon.Recalculate_Bounding_Box();
                geo.Add_Polygon(polygon);

                // Where the whole page image sits (TL, TR, BR, BL), so it can be drawn back over the map
                if (change.Image != null)
                {
                    var extent = new Coordinate_Polygon
                    {
                        Label = polygon.Label,
                        FeatureType = GeoSpatial_Information.IMAGE_EXTENT_FEATURE_TYPE,
                        PolygonType = MODE_RECTANGLE,
                        Rotation = polygon.Rotation,
                        Page_Sequence = (ushort)change.Index
                    };
                    foreach (double[] point in change.Image)
                        extent.Add_Edge_Point(Math.Round(point[0], 7), Math.Round(point[1], 7));
                    extent.Recalculate_Bounding_Box();
                    geo.Add_Polygon(extent);
                }
            }

            return true;
        }

        /// <summary> Returns the page's current footprint polygon, if it has one </summary>
        private static Coordinate_Polygon Existing_Polygon(Page_TreeNode Page)
        {
            GeoSpatial_Information geo = GeoSpatial_Edit_Helper.Get_Geo(Page, false);
            if (geo?.Polygons == null)
                return null;

            return geo.Polygons.FirstOrDefault(P => (P.FeatureType != "poi") && (P.FeatureType != GeoSpatial_Information.IMAGE_EXTENT_FEATURE_TYPE) && (P.PolygonType != "hidden") && (P.Edge_Points_Count >= 2));
        }

        /// <summary> Returns the page's saved image extent (its four corners), if it has one </summary>
        private static Coordinate_Polygon Existing_Image_Extent(Page_TreeNode Page)
        {
            GeoSpatial_Information geo = GeoSpatial_Edit_Helper.Get_Geo(Page, false);
            return geo?.Polygons?.FirstOrDefault(P => (P.FeatureType == GeoSpatial_Information.IMAGE_EXTENT_FEATURE_TYPE) && (P.Edge_Points_Count == 4));
        }

        /// <summary> A point to center the map on when a page has no footprint yet: the item's own location
        /// point, or any existing page footprint </summary>
        private object Default_Center(List<Page_TreeNode> Pages)
        {
            GeoSpatial_Information itemGeo = GeoSpatial_Edit_Helper.Get_Geo(currentItem, false);
            Coordinate_Point itemPoint = itemGeo?.Points?.FirstOrDefault(P => P.FeatureType != "poi");
            if (itemPoint != null)
                return new { lat = itemPoint.Latitude, lng = itemPoint.Longitude, zoom = 12 };

            foreach (Page_TreeNode page in Pages)
            {
                Coordinate_Polygon polygon = Existing_Polygon(page);
                if (polygon != null)
                    return new { lat = polygon.Edge_Points.Average(P => P.Latitude), lng = polygon.Edge_Points.Average(P => P.Longitude), zoom = 12 };
            }

            return null;
        }

        /// <summary> Title for the page that displays this viewer </summary>
        public override string Web_Title => Localization_Gateway.GeoSpatial_Edit.Overlay_Page_Title(RequestSpecificValues.Current_Mode.Language);

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
            GeoSpatial_Edit_Helper.Write_Head(Output, Static_Resources_Gateway.Sobekcm_Geo_Overlay_Js, "SobekGeoOverlay.init");
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

            // Only pages with a page image can be overlaid, but each keeps its real page sequence
            var tiles = new List<GeoSpatial_Edit_Helper.Ribbon_Tile>();
            var pageData = new List<object>();
            for (int i = 0; i < pages.Count; i++)
            {
                string image = GeoSpatial_Edit_Helper.Find_Page_Jpeg(pages[i], false);
                if (image == null)
                    continue;

                string label = GeoSpatial_Edit_Helper.Page_Label(pages[i], i + 1, language);
                Coordinate_Polygon polygon = Existing_Polygon(pages[i]);
                Coordinate_Polygon extent = polygon == null ? null : Existing_Image_Extent(pages[i]);
                tiles.Add(new GeoSpatial_Edit_Helper.Ribbon_Tile { Label = label, ThumbnailUrl = GeoSpatial_Edit_Helper.File_Url(currentItem, GeoSpatial_Edit_Helper.Find_Page_Jpeg(pages[i], true)), HasGeo = polygon != null });
                pageData.Add(new
                {
                    seq = i + 1,
                    label,
                    image = GeoSpatial_Edit_Helper.File_Url(currentItem, image),
                    polygon = polygon == null ? null : new
                    {
                        mode = polygon.PolygonType == MODE_CUSTOM ? MODE_CUSTOM : MODE_RECTANGLE,
                        rotation = polygon.Rotation,
                        points = polygon.Edge_Points.Select(P => new[] { P.Latitude, P.Longitude }).ToList()
                    },
                    extent = extent?.Edge_Points.Select(P => new[] { P.Latitude, P.Longitude }).ToList()
                });
            }

            Write_Item_Type_Top(Output, currentItem);

            Output.WriteLine("<div class=\"sbkGeo_Editor\" id=\"sbkGeo_Editor\">");
            Output.WriteLine("  <h2>" + Localization_Gateway.GeoSpatial_Edit.Overlay_Page_Title(language) + "</h2>");

            if (tiles.Count == 0)
            {
                Output.WriteLine("  <p class=\"sbkGeo_Instructions\">" + Localization_Gateway.GeoSpatial_Edit.No_Pages(language) + "</p>");
                Output.WriteLine("</div>");
                return;
            }

            Output.WriteLine("  <p class=\"sbkGeo_Instructions\">" + Localization_Gateway.GeoSpatial_Edit.Overlay_Instructions(language) + "</p>");
            GeoSpatial_Edit_Helper.Write_Message(Output, message, messageIsError);

            GeoSpatial_Edit_Helper.Write_Ribbon(Output, tiles, true, language);

            Write_ItemNavForm_Opening(Output);
            Output.WriteLine("  <input type=\"hidden\" id=\"sbkGeo_Action\" name=\"action\" value=\"\" />");
            Output.WriteLine("  <input type=\"hidden\" id=\"sbkGeo_Payload\" name=\"geo_payload\" value=\"\" />");

            Output.WriteLine("  <div class=\"sbkGeo_Toolbar\">");
            Output.WriteLine("    <span class=\"sbkGeo_Current\" id=\"sbkGeo_Current\"></span>");
            Output.WriteLine("    <input type=\"text\" class=\"sbkGeo_Search\" id=\"sbkGeo_Search\" placeholder=\"" + WebUtility.HtmlEncode(Localization_Gateway.GeoSpatial_Edit.Search_Placeholder(language)) + "\" />");
            Output.WriteLine("    <button type=\"button\" class=\"sbkGeo_Button\" id=\"sbkGeo_SearchButton\">" + Localization_Gateway.GeoSpatial_Edit.Search_Button(language) + "</button>");
            Output.WriteLine("    <span class=\"sbkGeo_ToolbarSpacer\"></span>");
            Output.WriteLine("    <button type=\"button\" class=\"sbkPiu_RoundButton\" id=\"sbkGeo_Cancel\">" + Localization_Gateway.Buttons.Exit(language) + "</button>");
            Output.WriteLine("    <button type=\"button\" class=\"sbkPiu_RoundButton\" id=\"sbkGeo_Save\">" + Localization_Gateway.Buttons.Save(language) + "</button>");
            Output.WriteLine("  </div>");

            Output.WriteLine("  <div class=\"sbkGeo_Toolbar sbkGeo_OverlayTools\">");
            Output.WriteLine("    <button type=\"button\" class=\"sbkGeo_Button\" id=\"sbkGeo_CenterImage\">" + Localization_Gateway.GeoSpatial_Edit.Center_Image(language) + "</button>");
            Output.WriteLine("    <button type=\"button\" class=\"sbkGeo_Button\" id=\"sbkGeo_ToggleImage\">" + Localization_Gateway.GeoSpatial_Edit.Toggle_Image(language) + "</button>");
            Output.WriteLine("    <label class=\"sbkGeo_Field\">" + Localization_Gateway.GeoSpatial_Edit.Transparency_Label(language) + " <input type=\"range\" id=\"sbkGeo_Transparency\" min=\"0\" max=\"90\" step=\"5\" value=\"40\" /></label>");
            Output.WriteLine("    <label class=\"sbkGeo_Field\">" + Localization_Gateway.GeoSpatial_Edit.Rotation_Label(language) + " <input type=\"number\" id=\"sbkGeo_Rotation\" min=\"0\" max=\"359.9\" step=\"0.5\" value=\"0\" />&deg;</label>");
            Output.WriteLine("    <label class=\"sbkGeo_Field\"><input type=\"checkbox\" id=\"sbkGeo_KeepProportions\" checked=\"checked\" /> " + Localization_Gateway.GeoSpatial_Edit.Keep_Proportions(language) + "</label>");
            Output.WriteLine("    <span class=\"sbkGeo_ToolbarDivider\"></span>");
            Output.WriteLine("    <button type=\"button\" class=\"sbkGeo_Button\" id=\"sbkGeo_UsePerimeter\">" + Localization_Gateway.GeoSpatial_Edit.Use_Perimeter(language) + "</button>");
            Output.WriteLine("    <button type=\"button\" class=\"sbkGeo_Button\" id=\"sbkGeo_DrawPolygon\">" + Localization_Gateway.GeoSpatial_Edit.Draw_Polygon(language) + "</button>");
            Output.WriteLine("    <button type=\"button\" class=\"sbkGeo_Button\" id=\"sbkGeo_DrawRectangle\">" + Localization_Gateway.GeoSpatial_Edit.Draw_Rectangle(language) + "</button>");
            Output.WriteLine("    <button type=\"button\" class=\"sbkGeo_Button\" id=\"sbkGeo_FinishPolygon\" hidden>" + Localization_Gateway.GeoSpatial_Edit.Finish_Polygon(language) + "</button>");
            Output.WriteLine("    <button type=\"button\" class=\"sbkGeo_Button\" id=\"sbkGeo_ClearPolygon\">" + Localization_Gateway.GeoSpatial_Edit.Clear_Polygon(language) + "</button>");
            Output.WriteLine("    <span class=\"sbkGeo_Hint\" id=\"sbkGeo_DrawHint\" hidden>" + Localization_Gateway.GeoSpatial_Edit.Draw_Hint(language) + "</span>");
            Output.WriteLine("    <span class=\"sbkGeo_Hint\" id=\"sbkGeo_RectangleHint\" hidden>" + Localization_Gateway.GeoSpatial_Edit.Rectangle_Hint(language) + "</span>");
            Output.WriteLine("  </div>");

            Output.WriteLine("  <div class=\"sbkGeo_Map\" id=\"sbkGeo_Map\"></div>");
            Write_ItemNavForm_Closing(Output);

            GeoSpatial_Edit_Helper.Write_Json_Block(Output, "sbkGeo_Data", new
            {
                pages = pageData,
                center = Default_Center(pages),
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

        /// <summary> Posted editor payload: only the pages the user actually changed </summary>
        private class Overlay_Payload
        {
            public List<Overlay_Change> Changes { get; set; }
        }

        /// <summary> One changed page, by page sequence </summary>
        private class Overlay_Change
        {
            public int Index { get; set; }
            public bool Clear { get; set; }
            public string Mode { get; set; }
            public double Rotation { get; set; }
            public List<double[]> Points { get; set; }

            /// <summary> The page image's four corners (TL, TR, BR, BL), when the image has been placed </summary>
            public List<double[]> Image { get; set; }
        }
    }
}
