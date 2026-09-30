#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Client;
using SobekCM.Core.FileSystems;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Solr;
using SobekCM.Library.Localization;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Divisions;
using SobekCM.Resource_Object.Metadata_Modules;
using SobekCM.Resource_Object.Metadata_Modules.GeoSpatial;
using SobekCM.Tools;
using SobekCM_Resource_Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Shared plumbing for the two geospatial editing mySobek viewers: loading and permission-checking
    /// the item, rendering the page ribbon, and saving the edited item back through SobekFileSystem </summary>
    internal static class GeoSpatial_Edit_Helper
    {
        /// <summary> Feature type written on the points and polygons these editors manage, so any legacy
        /// "poi" features from the old map editor are left untouched </summary>
        internal const string MAIN_FEATURE_TYPE = "main";

        /// <summary> Most edge points accepted for one polygon </summary>
        internal const int MAX_POLYGON_POINTS = 500;

        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            MaxDepth = 8
        };

        /// <summary> Loads the requested item and confirms the current user may edit it </summary>
        /// <returns> The full item, or NULL if the request was redirected or turned into an error </returns>
        internal static SobekCM_Item Load_Editable_Item(RequestCache RequestSpecificValues, HttpContext Context, string TraceSource)
        {
            Custom_Tracer tracer = RequestSpecificValues.Tracer;
            Navigation_Object currentMode = RequestSpecificValues.Current_Mode;

            if ((RequestSpecificValues.Current_User == null) || (!RequestSpecificValues.Current_User.LoggedOn))
            {
                currentMode.Mode = Display_Mode_Enum.Aggregation;
                currentMode.Aggregation = String.Empty;
                UrlWriterHelper.Redirect(currentMode, Context);
                return null;
            }

            if ((String.IsNullOrEmpty(currentMode.BibID)) || (String.IsNullOrEmpty(currentMode.VID)))
            {
                tracer.Add_Trace(TraceSource, "BibID or VID was not provided!");
                currentMode.Mode = Display_Mode_Enum.Error;
                currentMode.Error_Message = "Invalid Request : BibID/VID missing in geospatial edit request";
                return null;
            }

            tracer.Add_Trace(TraceSource, "Try to pull this sobek complete item");
            SobekCM_Item item = SobekEngineClient.Items.Get_Sobek_Item(currentMode.BibID, currentMode.VID, RequestSpecificValues.Current_User.UserID, tracer);
            if (item == null)
            {
                tracer.Add_Trace(TraceSource, "Unable to build complete item");
                currentMode.Mode = Display_Mode_Enum.Error;
                currentMode.Error_Message = "Invalid Request : Unable to build complete item";
                return null;
            }

            if (!RequestSpecificValues.Current_User.Can_Edit_This_Item(item.BibID, item.Bib_Info.SobekCM_Type_String, item.Bib_Info.Source.Code, item.Bib_Info.HoldingCode, item.Behaviors.Aggregation_Code_List))
            {
                currentMode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(currentMode, Context);
                return null;
            }

            return item;
        }

        /// <summary> Leaves the editor for the item itself: its map view when it has any coordinates,
        /// otherwise its default view </summary>
        internal static void Exit_To_Item(SobekCM_Item Item, RequestCache RequestSpecificValues, HttpContext Context)
        {
            Navigation_Object currentMode = RequestSpecificValues.Current_Mode;
            currentMode.Mode = Display_Mode_Enum.Item_Display;
            currentMode.Page = null;
            currentMode.ViewerCode = Has_Coordinates(Item) ? "map" : null;
            UrlWriterHelper.Redirect(currentMode, Context);
        }

        /// <summary> TRUE if the item, or any of its pages, has any coordinate information </summary>
        private static bool Has_Coordinates(SobekCM_Item Item)
        {
            if (Get_Geo(Item, false)?.hasData == true)
                return true;
            foreach (Page_TreeNode page in Get_Pages(Item))
            {
                if (Get_Geo(page, false)?.hasData == true)
                    return true;
            }
            return false;
        }

        /// <summary> All the pages of the item, in the same order (and so the same 1-based sequence) that
        /// GeoSpatial_BriefItemMapper uses to tie page polygons back to their page </summary>
        internal static List<Page_TreeNode> Get_Pages(SobekCM_Item Item)
        {
            var pages = new List<Page_TreeNode>();
            foreach (abstract_TreeNode node in Item.Divisions.Physical_Tree.Pages_PreOrder)
            {
                if (node is Page_TreeNode page)
                    pages.Add(page);
            }
            return pages;
        }

        /// <summary> Display label for a page, falling back to "Page N" </summary>
        internal static string Page_Label(Page_TreeNode Page, int Sequence, string Language)
        {
            if (!String.IsNullOrWhiteSpace(Page.Label))
                return Page.Label;
            return String.Format(Localization_Gateway.GeoSpatial_Edit.Page_Format(Language), Sequence);
        }

        /// <summary> Finds the page's JPEG, either the thumbnail or the full-size (non-thumbnail) image </summary>
        internal static string Find_Page_Jpeg(Page_TreeNode Page, bool Thumbnail)
        {
            string fallback = null;
            foreach (SobekCM_File_Info file in Page.Files)
            {
                string name = file.System_Name;
                if (String.IsNullOrEmpty(name) || !name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".qc.jpg", StringComparison.OrdinalIgnoreCase))
                    continue;

                bool isThumb = name.EndsWith("thm.jpg", StringComparison.OrdinalIgnoreCase);
                if (isThumb == Thumbnail)
                    return name;
                fallback ??= name;
            }

            // A missing thumbnail can fall back to the full image, but never the other way around
            return Thumbnail ? fallback : null;
        }

        /// <summary> Web URL for a file in the item's folder. These are lazy-loaded or swapped in by script,
        /// so they use the long-lived signed URL lifetime. </summary>
        internal static string File_Url(SobekCM_Item Item, string FileName)
        {
            if (String.IsNullOrEmpty(FileName))
                return null;

            bool isRestricted = (Item.Behaviors.IP_Restriction_Membership > 0) || Item.Behaviors.Dark_Flag;
            return SobekFileSystem.Resource_Web_Uri(Item.BibID, Item.VID, FileName, false, isRestricted, Signed_Url_Lifetime_Enum.Continuous);
        }

        /// <summary> Gets the geospatial module attached to an item or page, optionally creating it </summary>
        internal static GeoSpatial_Information Get_Geo(iMetadataDescribable Node, bool Create)
        {
            var geo = Node.Get_Metadata_Module(GlobalVar.GEOSPATIAL_METADATA_MODULE_KEY) as GeoSpatial_Information;
            if ((geo == null) && (Create))
            {
                geo = new GeoSpatial_Information();
                Node.Add_Metadata_Module(GlobalVar.GEOSPATIAL_METADATA_MODULE_KEY, geo);
            }
            return geo;
        }

        /// <summary> Deserializes the posted editor payload, returning NULL if it is missing or malformed </summary>
        internal static T Parse_Payload<T>(string Json) where T : class
        {
            if (String.IsNullOrWhiteSpace(Json) || Json.Length > 2_000_000)
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(Json, jsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        internal static bool Valid_Coordinate(double Latitude, double Longitude)
        {
            return (!Double.IsNaN(Latitude)) && (!Double.IsNaN(Longitude)) &&
                   (Latitude >= -90) && (Latitude <= 90) && (Longitude >= -180) && (Longitude <= 180);
        }

        /// <summary> Writes data for the page script as a JSON block. The default encoder escapes &lt;, &gt;
        /// and &amp;, so labels can't break out of the script element. </summary>
        internal static void Write_Json_Block(TextWriter Output, string Id, object Data)
        {
            Output.WriteLine("<script type=\"application/json\" id=\"" + Id + "\">" + JsonSerializer.Serialize(Data) + "</script>");
        }

        /// <summary> Writes the head content shared by both editors, followed by the tool's own script and the
        /// Google Maps loader, which calls back into the tool once the API is ready </summary>
        internal static void Write_Head(TextWriter Output, string ToolScriptUrl, string MapsCallback)
        {
            Output.WriteLine("  <meta name=\"robots\" content=\"noindex, nofollow\" />");
            Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Mysobek_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");
            Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Item_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");
            Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Geo_Edit_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");
            Output.WriteLine("  <script src=\"" + Static_Resources_Gateway.Sobekcm_Geo_Ribbon_Js + "\" type=\"text/javascript\"></script>");
            Output.WriteLine("  <script src=\"" + ToolScriptUrl + "\" type=\"text/javascript\"></script>");

            string key = UI_ApplicationCache_Gateway.Settings.System.Google_Map_API_Key;
            if (!String.IsNullOrEmpty(key))
                Output.WriteLine("  <script src=\"https://maps.googleapis.com/maps/api/js?key=" + WebUtility.UrlEncode(key) + "&loading=async&callback=" + MapsCallback + "\" async defer></script>");
        }

        /// <summary> One thumbnail in the page ribbon </summary>
        internal class Ribbon_Tile
        {
            internal string Label;
            internal string ThumbnailUrl;
            internal bool HasGeo;
        }

        /// <summary> Writes the scrolling thumbnail ribbon across the top of the editor </summary>
        /// <param name="Globe"> TRUE to use the globe status icon (overlay editor), FALSE for the map pin (points editor) </param>
        internal static void Write_Ribbon(TextWriter Output, List<Ribbon_Tile> Tiles, bool Globe, string Language)
        {
            string statusTitle = Globe ? Localization_Gateway.GeoSpatial_Edit.Has_Polygon_Title(Language) : Localization_Gateway.GeoSpatial_Edit.Has_Point_Title(Language);
            string statusIcon = Globe ? GLOBE_SVG : PIN_SVG;
            string unsavedTitle = Localization_Gateway.GeoSpatial_Edit.Unsaved_Title(Language);

            Output.WriteLine("<div class=\"sbkGeo_Ribbon\" id=\"sbkGeo_Ribbon\">");
            Output.WriteLine("  <button type=\"button\" class=\"sbkGeo_RibbonArrow\" id=\"sbkGeo_RibbonLeft\" title=\"" + Attr(Localization_Gateway.GeoSpatial_Edit.Scroll_Left(Language)) + "\" aria-label=\"" + Attr(Localization_Gateway.GeoSpatial_Edit.Scroll_Left(Language)) + "\">&#10094;</button>");
            Output.WriteLine("  <div class=\"sbkGeo_RibbonViewport\" id=\"sbkGeo_RibbonViewport\">");
            Output.WriteLine("    <div class=\"sbkGeo_RibbonTrack\" role=\"listbox\">");

            for (int i = 0; i < Tiles.Count; i++)
            {
                Ribbon_Tile tile = Tiles[i];
                string label = Attr(tile.Label);
                Output.WriteLine("      <button type=\"button\" role=\"option\" class=\"sbkGeo_Tile" + (tile.HasGeo ? " sbkGeo_HasGeo" : "") + "\" data-index=\"" + i + "\" title=\"" + label + "\">");
                if (!String.IsNullOrEmpty(tile.ThumbnailUrl))
                    Output.WriteLine("        <span class=\"sbkGeo_TileImg\"><img src=\"" + Attr(tile.ThumbnailUrl) + "\" alt=\"\" loading=\"lazy\" /></span>");
                else
                    Output.WriteLine("        <span class=\"sbkGeo_TileImg sbkGeo_TileNoImg\"></span>");
                Output.WriteLine("        <span class=\"sbkGeo_TileStatus\" title=\"" + Attr(statusTitle) + "\">" + statusIcon + "</span>");
                Output.WriteLine("        <span class=\"sbkGeo_TileDirty\" title=\"" + Attr(unsavedTitle) + "\"></span>");
                Output.WriteLine("        <span class=\"sbkGeo_TileLabel\">" + WebUtility.HtmlEncode(tile.Label) + "</span>");
                Output.WriteLine("      </button>");
            }

            Output.WriteLine("    </div>");
            Output.WriteLine("  </div>");
            Output.WriteLine("  <button type=\"button\" class=\"sbkGeo_RibbonArrow\" id=\"sbkGeo_RibbonRight\" title=\"" + Attr(Localization_Gateway.GeoSpatial_Edit.Scroll_Right(Language)) + "\" aria-label=\"" + Attr(Localization_Gateway.GeoSpatial_Edit.Scroll_Right(Language)) + "\">&#10095;</button>");
            Output.WriteLine("</div>");
        }

        /// <summary> Form action the help dialog posts, in the background, when its "don't show this again" box changes </summary>
        internal const string HELP_PREFERENCE_ACTION = "help_pref";

        /// <summary> Wraps a button's label for use in the help text, so the help always names buttons as they appear on screen </summary>
        internal static string Help_Label(string Label) => "<b>" + Label + "</b>";

        /// <summary> Writes an editor's help dialog, which the ribbon script opens on its own until the user asks
        /// not to see it again </summary>
        /// <param name="Output"> Stream to write to </param>
        /// <param name="Title"> Dialog title </param>
        /// <param name="Items"> Each help point, already formatted (see <see cref="Help_Label"/>) </param>
        /// <param name="Language"> Language for the dialog's own text </param>
        internal static void Write_Help_Dialog(TextWriter Output, string Title, IEnumerable<string> Items, string Language)
        {
            Output.WriteLine("  <dialog class=\"sbkGeo_Help\" id=\"sbkGeo_Help\" aria-labelledby=\"sbkGeo_HelpTitle\">");
            Output.WriteLine("    <h2 id=\"sbkGeo_HelpTitle\">" + Title + "</h2>");
            Output.WriteLine("    <ul>");
            foreach (string item in Items)
                Output.WriteLine("      <li>" + item + "</li>");
            Output.WriteLine("    </ul>");
            Output.WriteLine("    <div class=\"sbkGeo_HelpFooter\">");
            Output.WriteLine("      <label><input type=\"checkbox\" id=\"sbkGeo_HelpHide\" /> " + Localization_Gateway.GeoSpatial_Edit.Help_Dont_Show(Language) + "</label>");
            Output.WriteLine("      <button type=\"button\" class=\"sbkPiu_RoundButton\" id=\"sbkGeo_HelpOk\">" + Localization_Gateway.GeoSpatial_Edit.Help_OK(Language) + "</button>");
            Output.WriteLine("    </div>");
            Output.WriteLine("  </dialog>");
        }

        /// <summary> Returns the toolbar button that reopens the help dialog </summary>
        internal static string Help_Button(string Language) =>
            "<button type=\"button\" class=\"sbkGeo_Button\" id=\"sbkGeo_HelpButton\">" + Localization_Gateway.HeaderFooter.Help(Language) + "</button>";

        /// <summary> TRUE if the current user has asked not to see this editor's help dialog on opening </summary>
        internal static bool Help_Hidden(RequestCache RequestSpecificValues, string SettingKey) =>
            RequestSpecificValues.Current_User?.Get_Setting(SettingKey, false) == true;

        /// <summary> Handles the help dialog's background post, saving whether the user wants to see it on opening.
        /// Returns TRUE if this was that post, in which case the request is complete. </summary>
        internal static bool Handle_Help_Preference(RequestCache RequestSpecificValues, HttpContext Context, string SettingKey)
        {
            if (Context.Request.Form["action"] != HELP_PREFERENCE_ACTION)
                return false;

            RequestSpecificValues.Current_Mode.Request_Completed = true;

            User_Object user = RequestSpecificValues.Current_User;
            if (user == null)
                return true;

            string value = Context.Request.Form["help_hidden"] == "true" ? "true" : "false";
            if (user.Get_Setting(SettingKey, "false") == value)
                return true;

            user.Add_Setting(SettingKey, value);
            Engine_Database.Set_User_Setting(user.UserID, SettingKey, value);

            // Current_User is deserialized from the session on every request, so write it back too
            CachedDataManager_UserCacheServices.Save_To_Session(Context.Session, user);
            return true;
        }

        /// <summary> Writes the success or error message shown above the editor after a save </summary>
        internal static void Write_Message(TextWriter Output, string Message, bool IsError)
        {
            if (String.IsNullOrEmpty(Message))
                return;
            Output.WriteLine("<div class=\"sbkGeo_Message" + (IsError ? " sbkGeo_MessageError" : "") + "\" role=\"status\">" + WebUtility.HtmlEncode(Message) + "</div>");
        }

        /// <summary> Result of saving an edited item </summary>
        internal enum Save_Result
        {
            Failed,
            Saved,

            /// <summary> Saved to the METS and database, but the search index could not be updated </summary>
            Saved_Index_Failed
        }

        /// <summary> Saves an item whose geospatial modules were just edited: backs up the current METS, writes
        /// the new METS through SobekFileSystem (so it reaches GCS under Hybrid/Full), updates the database
        /// footprint, reindexes the item in Solr right away, and clears every cached copy of the item </summary>
        internal static Save_Result Save_Item_Geo(SobekCM_Item Item, RequestCache RequestSpecificValues, string TraceSource)
        {
            Custom_Tracer tracer = RequestSpecificValues.Tracer;
            string metsFileName = Item.BibID + "_" + Item.VID + ".mets.xml";
            string stagingFile = null;

            try
            {
                tracer.Add_Trace(TraceSource, "Backing up the current METS");
                try
                {
                    Mets_Backup_Helper.Backup_Current_Mets(Item.BibID, Item.VID, metsFileName, UI_ApplicationCache_Gateway.Settings.Resources.Backup_Files_Folder_Name);
                }
                catch (Exception ee)
                {
                    // A failed backup shouldn't block the edit itself
                    tracer.Add_Trace(TraceSource, "Unable to back up the METS: " + ee.Message, Custom_Trace_Type_Enum.Error);
                }

                tracer.Add_Trace(TraceSource, "Writing the new METS");
                string stagingDirectory = UI_ApplicationCache_Gateway.Settings.User_InProcess_Directory(RequestSpecificValues.Current_User, "geoedit");
                if (!Directory.Exists(stagingDirectory))
                    Directory.CreateDirectory(stagingDirectory);
                stagingFile = Path.Combine(stagingDirectory, metsFileName);
                Item.Save_METS(stagingFile);
                SobekFileSystem.CopyFileIn(stagingFile, Item.BibID, Item.VID, metsFileName);

                tracer.Add_Trace(TraceSource, "Saving to the database");
                SobekCM_Item_Database.Save_Digital_Resource(Item, Database_Save_Options());

                Item.Delete_Metadata_Cache();

                // Reindex now, so a map search finds the new location without waiting for the builder
                Save_Result result = Save_Result.Saved;
                string documentIndex = UI_ApplicationCache_Gateway.Settings.Servers.Document_Solr_Index_URL;
                if (!String.IsNullOrEmpty(documentIndex))
                {
                    try
                    {
                        tracer.Add_Trace(TraceSource, "Updating the search index");
                        Solr_Controller.Update_Index_After_Metadata_Change(documentIndex, UI_ApplicationCache_Gateway.Settings.Servers.Page_Solr_Index_URL, Item, UI_ApplicationCache_Gateway.Settings.System.Solr_Atomic_Updates_Enabled);
                    }
                    catch (Exception ee)
                    {
                        tracer.Add_Trace(TraceSource, "Unable to update the search index: " + ee.Message, Custom_Trace_Type_Enum.Error);
                        result = Save_Result.Saved_Index_Failed;
                    }
                }

                return result;
            }
            catch (Exception ee)
            {
                tracer.Add_Trace(TraceSource, "Save failed: " + ee.Message, Custom_Trace_Type_Enum.Error);
                return Save_Result.Failed;
            }
            finally
            {
                if ((stagingFile != null) && (File.Exists(stagingFile)))
                {
                    try { File.Delete(stagingFile); } catch { }
                }

                // The cached item was edited in place, so drop it whether or not the save succeeded
                CachedDataManager.Items.Remove_Digital_Resource_Object(Item.BibID, Item.VID, tracer);
                SobekEngineClient.Items.Clear_Item_Cache(Item.BibID, Item.VID, tracer);
            }
        }

        /// <summary> Same database save options File_Management_MySobekViewer passes </summary>
        private static Dictionary<string, object> Database_Save_Options()
        {
            var options = new Dictionary<string, object>();
            if (UI_ApplicationCache_Gateway.Settings.MarcGeneration != null)
            {
                options["MarcXML_File_ReaderWriter:MARC Cataloging Source Code"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Cataloging_Source_Code;
                options["MarcXML_File_ReaderWriter:MARC Location Code"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Location_Code;
                options["MarcXML_File_ReaderWriter:MARC Reproduction Agency"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Reproduction_Agency;
                options["MarcXML_File_ReaderWriter:MARC Reproduction Place"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Reproduction_Place;
                options["MarcXML_File_ReaderWriter:MARC XSLT File"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.XSLT_File;
            }
            options["MarcXML_File_ReaderWriter:System Name"] = UI_ApplicationCache_Gateway.Settings.System.System_Name;
            options["MarcXML_File_ReaderWriter:System Abbreviation"] = UI_ApplicationCache_Gateway.Settings.System.System_Code;
            return options;
        }

        private static string Attr(string Value)
        {
            return WebUtility.HtmlEncode(Value ?? String.Empty);
        }

        private const string PIN_SVG = "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" aria-hidden=\"true\"><path fill=\"#d93025\" stroke=\"#fff\" stroke-width=\"1.5\" d=\"M12 2C8.1 2 5 5.1 5 9c0 5.2 7 13 7 13s7-7.8 7-13c0-3.9-3.1-7-7-7z\"/><circle cx=\"12\" cy=\"9\" r=\"2.6\" fill=\"#fff\"/></svg>";

        private const string GLOBE_SVG = "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" aria-hidden=\"true\"><circle cx=\"12\" cy=\"12\" r=\"10\" fill=\"#1a73e8\" stroke=\"#fff\" stroke-width=\"1.5\"/><path fill=\"none\" stroke=\"#fff\" stroke-width=\"1.3\" d=\"M2.5 12h19M12 2.2c2.8 2.7 4.2 6 4.2 9.8s-1.4 7.1-4.2 9.8M12 2.2C9.2 4.9 7.8 8.2 7.8 12s1.4 7.1 4.2 9.8\"/></svg>";
    }
}
