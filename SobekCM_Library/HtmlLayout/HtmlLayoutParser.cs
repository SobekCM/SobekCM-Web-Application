using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SobekCM.Library.HtmlLayout
{
    /// <summary> Static class is used to read the HTML layout template objects and
    /// create the individual token portions of the layout </summary>
    public static class HtmlLayoutParser
    {
        /// <summary> Parse the HTML layout template file and return a fully built
        /// HTML layout information object </summary>
        /// <param name="SourceFile"> Source HTML segment file to parse </param>
        /// <returns> Fully built HTML layout obbject </returns>
        public static HtmlLayoutInfo Parse(string SourceFile)
        {
            string template_contents = String.Empty;
            try
            {
                // Get the template contents
                StreamReader reader = new StreamReader(SourceFile);
                template_contents = reader.ReadToEnd();
                reader.Close();
            }
            catch (Exception ee)
            {
                // Error reading the template, create a dummy here
                HtmlLayoutInfo errorLayout = new HtmlLayoutInfo();
                HtmlLayoutSection errorSection = new HtmlLayoutSection
                {
                    Name = "Error",
                    Type = HtmlLayoutSectionTypeEnum.Static_HTML,
                    HTML = "Exception reading the source file ( " + SourceFile + " ) : " + ee.Message
                };
                errorLayout.Sections.Add(errorSection);
                return errorLayout;
            }

            int static_section_count = 0;

            // Create the return object
            HtmlLayoutInfo returnObj = new HtmlLayoutInfo();
            int current_index = 0;
            int total_length = template_contents.Length;
            while (current_index < total_length)
            {
                // Look for the next section
                int next_section_index = template_contents.IndexOf("[%SECTION:", current_index);

                // If none found, the rest is static HTML
                if (next_section_index < 0)
                {
                    static_section_count++;

                    HtmlLayoutSection staticSection = new HtmlLayoutSection
                    {
                        Name = "Static" + static_section_count,
                        Type = HtmlLayoutSectionTypeEnum.Static_HTML,
                        HTML = template_contents.Substring(current_index)
                    };
                    returnObj.Sections.Add(staticSection);
                    current_index = total_length;
                }
                else // Found a section directive
                {
                    // Is there a static section to grab?
                    if (next_section_index != current_index)
                    {
                        static_section_count++;

                        HtmlLayoutSection staticSection = new HtmlLayoutSection
                        {
                            Name = "Static" + static_section_count,
                            Type = HtmlLayoutSectionTypeEnum.Static_HTML,
                            HTML = template_contents.Substring(current_index, next_section_index - current_index )
                        };
                        returnObj.Sections.Add(staticSection);
                    }

                    // Get the section name
                    int section_tag_end = template_contents.IndexOf("%]", next_section_index);
                    if (section_tag_end < 0)
                    {
                        static_section_count++;

                        HtmlLayoutSection staticSection = new HtmlLayoutSection
                        {
                            Name = "Static" + static_section_count,
                            Type = HtmlLayoutSectionTypeEnum.Static_HTML,
                            HTML = "Unable to find end of directive tag in the HTML template file"
                        };
                        returnObj.Sections.Add(staticSection);
                        current_index = total_length;
                    }
                    else
                    {
                        string section_name = template_contents.Substring(next_section_index + 10, section_tag_end - next_section_index - 10);
                        if (section_name == "VIEWER")
                        {
                            HtmlLayoutSection viewerSection = new HtmlLayoutSection
                            {
                                Name = section_name,
                                Type = HtmlLayoutSectionTypeEnum.Viewer_Section
                            };
                            returnObj.Sections.Add(viewerSection);
                        }
                        else
                        {
                            HtmlLayoutSection dynamicSection = new HtmlLayoutSection
                            {
                                Name = section_name,
                                Type = HtmlLayoutSectionTypeEnum.Dynamic_Section
                            };
                            returnObj.Sections.Add(dynamicSection);
                        }
                        current_index = section_tag_end + 2;
                    }
                }
            }

            return returnObj;
        }
    }
}
