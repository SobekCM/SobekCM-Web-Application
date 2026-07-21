#region Using directives

using SobekCM.Core.Aggregations;
using SobekCM.Core.Navigation;
using SobekCM.Core.Skins;
using SobekCM.Library.UI;
using System;
using System.IO;

#endregion

namespace SobekCM.Library.HTML
{
    /// <summary> Class is a helper class used for writing the main collection, interface, or institution banner for HTML responses </summary>
    public static class Banner_Helper
    {
        /// <summary> Adds the banner to the response stream from either the html web skin
        /// or from the current item aggreagtion object, depending on flags in the web skin object </summary>
        /// <param name="Output"> Stream to which to write the HTML for the banner </param>
        /// <param name="Banner_Division_Name"> Name for the wrapper division around the banner </param>
        /// <param name="Hierarchy_Object"> Current item aggregation object to display </param>
        /// <param name="HTML_Skin"> HTML Web skin which controls the overall appearance of this digital library </param>
        /// <param name="Web_Page_Title"> Web page title to add behind the banner image </param>
        /// <param name="CurrentMode"> Mode / navigation information for the current request</param>
        /// <remarks> This is called by several html subwriters that otherwise tell this class to suppress writing the banner </remarks>
        public static void Add_Banner(TextWriter Output, string Banner_Division_Name, string Web_Page_Title, Navigation_Object CurrentMode, Web_Skin_Object HTML_Skin, Item_Aggregation Hierarchy_Object)
        {
            Output.WriteLine("<!-- Banner_Helper.Add_Banner - Write the main collection, interface, or institution banner -->");

            if ((HTML_Skin != null) && (HTML_Skin.Override_Banner.HasValue) && (HTML_Skin.Override_Banner.Value))
            {
                if (!String.IsNullOrEmpty(HTML_Skin.Banner_HTML))
                    Output.WriteLine(HTML_Skin.Banner_HTML);
            }
            else
            {
                string url_options = UrlWriterHelper.URL_Options(CurrentMode);
                if (url_options.Length > 0)
                    url_options = "?" + url_options;

                if ((Hierarchy_Object != null) && (Hierarchy_Object.Code != "all"))
                {
                    Output.WriteLine("<section id=\"sbkAhs_BannerDiv\" role=\"banner\" title=\"" + Hierarchy_Object.ShortName + "\">");
                    Output.WriteLine("  <h1 class=\"hidden-element\">" + Web_Page_Title + "</h1>");
                    Output.WriteLine("  <a alt=\"" + Hierarchy_Object.ShortName + "\" href=\"" + CurrentMode.Base_URL + Hierarchy_Object.Code + url_options + "\"><img id=\"mainBanner\" src=\"" + CurrentMode.Base_URL + Hierarchy_Object.Get_Banner_Image(HTML_Skin) + "\"  alt=\"" + Hierarchy_Object.ShortName + "\" /></a>");
                    Output.WriteLine("</section>");
                }
                else
                {
                    if ((Hierarchy_Object != null) && (Hierarchy_Object.Get_Banner_Image(HTML_Skin).Length > 0))
                    {
                        Output.WriteLine("<section id=\"sbkAhs_BannerDiv\" role=\"banner\" title=\"" + Hierarchy_Object.ShortName + "\">");
                        Output.WriteLine("  <h1 class=\"hidden-element\">" + Web_Page_Title + "</h1>");
                        Output.WriteLine("  <a alt=\"" + Hierarchy_Object.ShortName + "\" href=\"" + CurrentMode.Base_URL + url_options + "\"><img id=\"mainBanner\" src=\"" + CurrentMode.Base_URL + Hierarchy_Object.Get_Banner_Image(HTML_Skin) + "\"  alt=\"" + Hierarchy_Object.ShortName + "\" /></a>");
                        Output.WriteLine("</section>");
                    }
                    else
                    {
                        string skin_url = CurrentMode.Base_Design_URL + "skins/" + CurrentMode.Skin + "/";
                        Output.WriteLine("<section id=\"sbkAhs_BannerDiv\" role=\"banner\"><h1 class=\"hidden-element\">" + Web_Page_Title + "</h1><a href=\"" + CurrentMode.Base_URL + url_options + "\"><img id=\"mainBanner\" src=\"" + skin_url + "default.jpg\" alt=\"\" /></a></section>");
                    }
                }
            }

            Output.WriteLine();
        }
    }
}
