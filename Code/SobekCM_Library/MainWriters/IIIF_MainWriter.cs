#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.BriefItem;
using SobekCM.Core.Client;
using SobekCM.Core.FileSystems;
using SobekCM.Core.Navigation;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

#endregion

namespace SobekCM.Library.MainWriters
{
    /// <summary> Main writer serves IIIF (International Image Interoperability Framework) requests,
    /// such as Image API info.json/image requests and Presentation API manifests </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractMainWriter"/>. </remarks>
    public class IIIF_MainWriter : abstractMainWriter
    {
        /// <summary> Descriptive terms which are internal/administrative rather than descriptive of the
        /// resource itself, and so should never appear in a public-facing IIIF manifest's metadata </summary>
        private static readonly HashSet<string> Skipped_Metadata_Terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Last Modified",
            "Last Type",
            "Last User",
            "System Folder",
            "Aggregations",
            "Aggregation"
        };

        /// <summary> Constructor for a new instance of the IIIF_MainWriter class </summary>
        /// <param name="Context"> Context for this individual HTTP request </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public IIIF_MainWriter(HttpContext Context, RequestCache RequestSpecificValues) : base(Context, RequestSpecificValues)
        {
            Context.Response.ContentType = "application/json";
        }

        /// <summary> Gets the enumeration of the type of main writer </summary>
        /// <value> This property always returns the enumerational value <see cref="Writer_Type_Enum.IIIF"/>. </value>
        public override Writer_Type_Enum Writer_Type { get { return Writer_Type_Enum.IIIF; } }

        /// <summary> Perform all the work of adding text directly to the response stream back to the web user </summary>
        /// <param name="Output"> Stream to which to write the text for this main writer </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Body(TextWriter Output, Custom_Tracer Tracer)
        {
            switch (RequestSpecificValues.Current_Mode.ViewerCode)
            {
                case "manifest":
                    display_iiif_info(Output, -1);
                    break;

                case "canvas":
                    string possible_sequence = RequestSpecificValues.Current_Mode.SubPage.ToString();
                    if ( !int.TryParse(possible_sequence, out int sequence) || sequence < 1 )
                    {
                        write_json_error(Output, "Bad Request", 400, "Canvas identifier missing or malformed");
                        return;
                    }
                    display_iiif_info(Output, 1);
                    break;

                case "image":
                    write_json_error(Output, "Not Implemented", 501, "Image API is not yet implemented for IIIF");
                    return;

                default:
                    write_json_error(Output, "Bad Request", 400, "Invalid request");
                    return;
            }
        }

        /// <summary> Writes the IIIF info.json / manifest / image response directly to the output stream </summary>
        /// <param name="Output"> Stream to which to write the IIIF response </param>
        protected internal void display_iiif_info(TextWriter Output, int sequence)
        {
            string bibid = RequestSpecificValues.Current_Mode.BibID;
            string vid = RequestSpecificValues.Current_Mode.VID;

            if ((String.IsNullOrEmpty(bibid)) || ( String.IsNullOrEmpty(vid)) || (bibid.Length != 10) || (vid.Length != 5 ))
            {
                write_json_error(Output, "Bad Request", 400, "Manifest identifier missing or malformed");
                return;
            }

            int status_code = 0;
            BriefItemInfo currentItem = null;

            try
            {
                currentItem = SobekEngineClient.Items.Get_Item_Brief(bibid, vid, true, RequestSpecificValues.Tracer, out status_code);
            }
            catch (Exception ee)
            {
                if (ee.Message?.IndexOf("404") == 0)
                {
                    write_json_error(Output, "Not Found", 404, "Manifest does not exist");
                }
                else
                {
                    write_json_error(Output, "Internal Server Error", 500, "Error attempting to retrieve manifest");
                }
                return;
            }

            // Finally, if no error but it is NULL, return
            if (currentItem == null)
            {
                write_json_error(Output, "Not Found", 404, "Manifest does not exist");
                return;
            }

            if (sequence < 0)
            {
                JsonObject manifest = Build_Manifest(currentItem, bibid, vid);
                Output.Write(manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                string manifestId = UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + "iiif/manifest/" + bibid + "/" + vid;
                JsonObject canvas = Build_Canvas(sequence, currentItem, manifestId);
                if ( canvas == null )
                {
                    write_json_error(Output, "Not Found", 404, "Canvas does not exist for that manifest");
                    return;
                }
                Output.Write(canvas.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        /// <summary> Builds a minimal IIIF Presentation API 3.0 manifest for the given item, with one
        /// Canvas per image-bearing page </summary>
        /// <remarks> This is a very basic manifest -- it does not yet cover ranges/structure (table of
        /// contents), rights/license statements, provider/homepage metadata, or the IIIF Image API
        /// service on each image body (which would allow region/size/rotation requests against the
        /// image, rather than just linking the full-size JPEG directly) </remarks>
        private JsonObject Build_Manifest(BriefItemInfo Item, string BibID, string VID)
        {
            string manifestId = UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + "iiif/manifest/" + BibID + "/" + VID;

            var canvases = new JsonArray();
            if (Item.Images != null)
            {
                for ( int sequence = 1; sequence <= Item.Images.Count ; sequence++ )
                {
                    var canvasJson = Build_Canvas(sequence, Item, manifestId);
                        if (canvasJson != null)
                            canvases.Add(canvasJson);
                }
            }

            // Create the metadata array
            var metadataArray = new JsonArray();
            foreach ( var desc in Item.Description )
            {
                if (Skipped_Metadata_Terms.Contains(desc.Term))
                    continue;

                if ((desc.Values == null) || (desc.Values.Count == 0))
                    continue;

                // Group the values by language, since a single term's values may mix languages
                // (or have none specified, in which case IIIF's "none" key is used)
                var valueMap = new JsonObject();
                foreach (var descValue in desc.Values)
                {
                    string lang = String.IsNullOrEmpty(descValue.Language) ? "none" : descValue.Language;

                    if (!valueMap.TryGetPropertyValue(lang, out var langValues))
                    {
                        langValues = new JsonArray();
                        valueMap[lang] = langValues;
                    }

                    ((JsonArray) langValues).Add(descValue.Value);
                }

                metadataArray.Add(new JsonObject()
                {
                    ["label"] = new JsonObject { ["en"] = new JsonArray(desc.Term) },
                    ["value"] = valueMap
                });
            }

            // Create the seeAlso json array
            var seeAlsoArray = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = SobekFileSystem.Resource_Web_Uri(Item, Item.BibID + "_" + Item.VID + ".mets.xml"),
                    ["type"] = "Dataset",
                    ["format"] = "application/mets+xml",
                    ["label"] = new JsonObject { ["en"] = "METS" }
                },
                new JsonObject
                {
                    ["id"] = SobekFileSystem.Resource_Web_Uri(Item, "marc.xml"),
                    ["type"] = "Dataset",
                    ["format"] = "application/marcxml+xml",
                    ["label"] = new JsonObject { ["en"] = "MARCXML" }
                }
            };

            return new JsonObject
            {
                ["@context"] = "http://iiif.io/api/presentation/3/context.json",
                ["id"] = manifestId,
                ["type"] = "Manifest",
                ["label"] = new JsonObject { ["none"] = new JsonArray(String.IsNullOrEmpty(Item.Title) ? BibID : Item.Title) },
                ["metadata"] = metadataArray,
                ["services"] = new JsonObject
                {
                    ["context"] = "http://iiif.io/api/search/0/context.json",
                    ["id"] = manifestId + "/search",
                    ["profile"] = "http://iiif.io/api/search/0/search"
                },
                ["seeAlso"] = seeAlsoArray,
                ["items"] = canvases
            };
        }

        private JsonObject Build_Canvas(int sequence, BriefItemInfo Item, string manifestId)
        {
            if (Item.Images == null) return null;
            if (sequence < 1) return null;
            if (sequence > Item.Images.Count) return null;

            var page = Item.Images[sequence - 1];

            BriefItem_File imageFile = Get_Preferred_Image_File(page);
            if (imageFile != null)
            {
                string canvasId = manifestId + "/canvas/" + sequence;
                string imageUrl = Item.Web.Source_URL + "/" + imageFile.Name;
                int width = imageFile.Width ?? 0;
                int height = imageFile.Height ?? 0;
                string pageLabel = String.IsNullOrEmpty(page.Label) ? "Page " + sequence : page.Label;

                return new JsonObject
                {
                    ["id"] = canvasId,
                    ["type"] = "Canvas",
                    ["label"] = new JsonObject { ["none"] = new JsonArray(pageLabel) },
                    ["width"] = width,
                    ["height"] = height,
                    ["items"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["id"] = canvasId + "/painting-page",
                                    ["type"] = "AnnotationPage",
                                    ["items"] = new JsonArray
                                    {
                                        new JsonObject
                                        {
                                            ["id"] = canvasId + "/painting-anno",
                                            ["type"] = "Annotation",
                                            ["motivation"] = "painting",
                                            ["target"] = canvasId,
                                            ["body"] = new JsonObject
                                            {
                                                ["id"] = imageUrl,
                                                ["type"] = "Image",
                                                ["format"] = imageFile.MIME_Type,
                                                ["width"] = width,
                                                ["height"] = height
                                            }
                                        }
                                    }
                                }
                            }
                };
            }

            return null;
        }

        /// <summary> Picks the file within a page's file grouping to use as the Canvas's painted image </summary>
        /// <remarks> Prefers the JPEG derivative, matching the file selection logic used elsewhere for
        /// on-screen page display (e.g. Json_MainWriter) </remarks>
        private static BriefItem_File Get_Preferred_Image_File(BriefItem_FileGrouping Page)
        {
            if ((Page.Files == null) || (Page.Files.Count == 0))
                return null;

            foreach (BriefItem_File file in Page.Files)
            {
                string extension = file.File_Extension.TrimStart('.').ToUpperInvariant();
                if ((extension == "JPG") || (extension == "JPEG"))
                    return file;
            }

            return null;
        }

        private void write_json_error(TextWriter Output, string title, int code, string detail)
        {
            Context.Response.StatusCode = code;

            Output.WriteLine("{");
            Output.WriteLine("  \"type\": \"about:blank\",");
            Output.WriteLine($"  \"title\": \"{title}\",");
            Output.WriteLine($"  \"status\": {code},");
            Output.WriteLine($"  \"detail\": \"{detail}\"");
            Output.WriteLine("}");
        }
    }
}
