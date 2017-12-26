#region Using directives

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Core.Search;
using SobekCM.Core.UI_Configuration.Viewers;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Database;
using SobekCM.Library.Email;
using SobekCM.Library.ResultsViewer;
using SobekCM.Library.UI;
using SobekCM.Tools;

#endregion

namespace SobekCM.Library.HTML
{
	/// <summary> Accepts a result set of titles and items and renders the correct page of results in the result view the user has requested </summary>
	/// <remarks> This class extends the <see cref="abstractHtmlSubwriter"/> abstract class. </remarks>
	public class PagedResults_HtmlSubwriter : abstractHtmlSubwriter
	{
		private const int MINIMIZED_FACET_COUNT = 10;
		private const int MAXIMIZED_FACET_COUNT = 100;
		private const int RESULTS_PER_PAGE = 20;

		private string leftButtons;
		private string rightButtons;
		private readonly string facetInformation;
		private iResultsViewer resultWriter;
		private string sortOptions;
		private int term_counter;

        private readonly Item_Aggregation hierarchyObject;
        private readonly Search_Results_Statistics resultsStatistics;
        private readonly List<iSearch_Title_Result> pagedResults;

	    /// <summary> Constructor for a new instance of the paged_result_html_subwriter class </summary>
	    /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
	    /// <param name="ResultsStats"> Statistics about the results to display including the facets </param>
        /// <param name="PagedResults"> Actual pages of results </param>
	    public PagedResults_HtmlSubwriter(RequestCache RequestSpecificValues, Search_Results_Statistics ResultsStats, List<iSearch_Title_Result> PagedResults) : base(RequestSpecificValues) 
		{
            // Save the search results info
            resultsStatistics = ResultsStats;
		    pagedResults = PagedResults;

            // Check that the current aggregation code is valid
            if (!UI_ApplicationCache_Gateway.Aggregations.isValidCode(RequestSpecificValues.Current_Mode.Aggregation))
            {
                // Is there a "forward value"
                if (UI_ApplicationCache_Gateway.Collection_Aliases.ContainsKey(RequestSpecificValues.Current_Mode.Aggregation))
                {
                    RequestSpecificValues.Current_Mode.Aggregation = UI_ApplicationCache_Gateway.Collection_Aliases[RequestSpecificValues.Current_Mode.Aggregation];
                }
            }

            // Use the method in the base class to actually pull the entire hierarchy
            if (!Get_Collection(RequestSpecificValues.Current_Mode, RequestSpecificValues.Tracer, out hierarchyObject))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Error;
                return;
            }

			Browse_Title = String.Empty;
			sortOptions = String.Empty;
			leftButtons = String.Empty;
			rightButtons = String.Empty;
			Showing_Text = String.Empty;
			Include_Bookshelf_View = false;
			Outer_Form_Name = String.Empty;
			Folder_Owner_Name = String.Empty;
			Folder_Owner_Email = String.Empty;
			term_counter = 0;

			// Try to get the facet configuration information
			facetInformation = "00000000";
			if (HttpContext.Current.Request.Form["facet"] != null)
				facetInformation = HttpContext.Current.Request.Form["facet"].PadRight(7, '0');

			if ( true ) // if (currentMode.isPostBack)
			{
				// Pull the standard values
				NameValueCollection form = HttpContext.Current.Request.Form;

			
				if (form["item_action"] != null)
				{
					string action = form["item_action"].ToLower().Trim();
					string url_description = form["url_description"].Trim();
					
					if (action == "email")
					{
						string address = form["email_address"].Replace(";", ",").Trim();
						string comments = form["email_comments"].Trim();
						string format = form["email_format"].Trim().ToUpper();
						string list_type = form["list_type"].Trim();

						if (address.Length > 0)
						{
							// Determine the email format
							bool is_html_format = format != "TEXT";

							// CC: the user, unless they are already on the list
							string cc_list = RequestSpecificValues.Current_User.Email;
							if (address.ToUpper().IndexOf(RequestSpecificValues.Current_User.Email.ToUpper()) >= 0)
								cc_list = String.Empty;

							// Send the email
							string any_error = URL_Email_Helper.Send_Email(address, cc_list, comments, RequestSpecificValues.Current_User.Full_Name, RequestSpecificValues.Current_Mode.Instance_Abbreviation, is_html_format, HttpContext.Current.Items["Original_URL"].ToString(), url_description, list_type, RequestSpecificValues.Current_User.UserID);
							HttpContext.Current.Session.Add("ON_LOAD_MESSAGE", any_error.Length > 0 ? any_error : "Your email has been sent");

							RequestSpecificValues.Current_Mode.isPostBack = true;

							// Do this to force a return trip (cirumnavigate cacheing)
							string original_url = HttpContext.Current.Items["Original_URL"].ToString();
							if ( original_url.IndexOf("?") < 0 )
								HttpContext.Current.Response.Redirect(original_url + "?p=" + DateTime.Now.Millisecond, false);
							else
								HttpContext.Current.Response.Redirect(original_url + "&p=" + DateTime.Now.Millisecond, false);

							HttpContext.Current.ApplicationInstance.CompleteRequest();
                            RequestSpecificValues.Current_Mode.Request_Completed = true;
							return;
						}
					}

					if (action == "save_search")
					{
						string usernotes = HttpContext.Current.Request.Form["add_notes"].Trim();
						bool open_searches = HttpContext.Current.Request.Form["open_searches"] != null;

						string original_url = HttpContext.Current.Items["Original_URL"].ToString();
                        if (SobekCM_Database.Save_User_Search(RequestSpecificValues.Current_User.UserID, original_url, url_description, 0, usernotes, RequestSpecificValues.Tracer) != -1000)
						{
							if (open_searches)
							{
								HttpContext.Current.Session.Add("ON_LOAD_WINDOW", "?m=lms");
							}

							HttpContext.Current.Session.Add("ON_LOAD_MESSAGE", "Search has been saved to your saved searches.");
						}
						else
						{
							HttpContext.Current.Session.Add("ON_LOAD_MESSAGE", "ERROR encountered while saving!");
						}

						// Do this to force a return trip (cirumnavigate cacheing)
						RequestSpecificValues.Current_Mode.isPostBack = true;
						if (original_url.IndexOf("?") > 0)
						{
							HttpContext.Current.Response.Redirect(original_url + "&p=" + DateTime.Now.Millisecond, false);
						}
						else
						{
							HttpContext.Current.Response.Redirect(original_url + "?p=" + DateTime.Now.Millisecond, false);
						}
						HttpContext.Current.ApplicationInstance.CompleteRequest();
                        RequestSpecificValues.Current_Mode.Request_Completed = true;
					}
				}
			}
		}

        #region Properties



        /// <summary> If the results dataset should be displayed in the context of an outer form (such as in
        /// the case that this is part of the mySobek bookshelf functionality) then the form name should go here.  If 
        /// no outer form name is provided, this will create its own sort form  </summary>
        public string Outer_Form_Name { get; set; }

        /// <summary> Flag indicates if the bookshelf view should be included in the list of possible views  </summary>
        public bool Include_Bookshelf_View { get; set; }

        /// <summary> Name of the owner of this folder </summary>
        public string Folder_Owner_Name { private get; set; }

        /// <summary> Email of the owner of this folder </summary>
        public string Folder_Owner_Email { private get; set; }

        /// <summary> Text which indicates which values of the current result or browse are being shown</summary>
        public string Showing_Text { get; private set; }

        /// <summary> Title for the current view, which is used rather than the search explanation </summary>
        public string Browse_Title { get; set; }
        
        #endregion

        /// <summary> Write any additional values within the HTML Head of the
        /// final served page </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> By default this does nothing, but can be overwritten by all the individual html subwriters </remarks>
        public void Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer)
        {
            if (resultWriter != null)
            {
                resultWriter.Write_Within_HTML_Head(Output, Tracer);
            }
        }

        /// <summary> Creates the specific results viewer according the user's preferences in the current request mode </summary>
		private void create_resultwriter()
		{
			if ( resultsStatistics.Total_Items == 0)
			{
                resultWriter = new No_Results_ResultsViewer();
			    resultWriter.PagedResults = pagedResults;
			    resultWriter.ResultsStats = resultsStatistics;
			    resultWriter.RequestSpecificValues = RequestSpecificValues;
				return;
			}

			// If this is default, determine the type from the aggregation (currently) or user
			if ( String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Result_Display_Type))
			{
				if ( !String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Coordinates))
					RequestSpecificValues.Current_Mode.Result_Display_Type = "map";
				else
				{
					string user_view = "default";
					if (HttpContext.Current.Session["User_Default_View"] != null)
						user_view = HttpContext.Current.Session["User_Default_View"].ToString();
					RequestSpecificValues.Current_Mode.Result_Display_Type = hierarchyObject.Default_Result_View;
				    if ((!String.IsNullOrEmpty(user_view)) && (!String.Equals(user_view, "default", StringComparison.OrdinalIgnoreCase)))
				    {
				        // Is this a valid viewer?
				        if ((UI_ApplicationCache_Gateway.Configuration.UI.WriterViewers.Results.GetViewerByCode(user_view) != null) && ( hierarchyObject.Result_Views.Contains(user_view)))
				        {
				            RequestSpecificValues.Current_Mode.Result_Display_Type = user_view;
				        }
				    }
				}
			}

	        // Get the results viewer via the factory class
            resultWriter = ResultsViewer_Factory.Get_Results_Viewer(RequestSpecificValues.Current_Mode.Result_Display_Type, RequestSpecificValues, resultsStatistics, pagedResults);
			
			// Populate the sort list and sort the result set
			sortOptions = String.Empty;
			StringBuilder sort_options_builder = new StringBuilder(1000);
			if ((resultWriter.Sortable) && (!RequestSpecificValues.Current_Mode.Is_Robot))
			{
				// Add the special sorts for browses
				if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation) // browse info only
				{
					if (RequestSpecificValues.Current_Mode.Info_Browse_Mode.ToUpper().IndexOf("NEW") >= 0)
					{
						if (RequestSpecificValues.Current_Mode.Sort == 0)
						{
							sort_options_builder.Append("      <option value=\"" + 0 + "\" selected=\"selected\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Date Added", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
						}
						else
						{
							sort_options_builder.Append("      <option value=\"" + 0 + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Date Added", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
						}

						if (RequestSpecificValues.Current_Mode.Sort == 1)
						{
							sort_options_builder.Append("      <option value=\"" + 1 + "\" selected=\"selected\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Title", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
						}
						else
						{
							sort_options_builder.Append("      <option value=\"" + 1 + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Title", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
						}
					}
					else
					{
						if ((RequestSpecificValues.Current_Mode.Sort == 0) || (RequestSpecificValues.Current_Mode.Sort == 1))
						{
							sort_options_builder.Append("      <option value=\"" + 0 + "\" selected=\"selected\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Title", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
						}
						else
						{
							sort_options_builder.Append("      <option value=\"" + 0 + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Title", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
						}
					}
				}

				// Add the special sorts for searches
				if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Results)
				{
					if (RequestSpecificValues.Current_Mode.Sort == 0)
					{
						sort_options_builder.Append("      <option value=\"" + 0 + "\" selected=\"selected\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Rank", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
					}
					else
					{
						sort_options_builder.Append("      <option value=\"" + 0 + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Rank", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
					}

					if (RequestSpecificValues.Current_Mode.Sort == 1)
					{
						sort_options_builder.Append("      <option value=\"" + 1 + "\" selected=\"selected\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Title", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
					}
					else
					{
						sort_options_builder.Append("      <option value=\"" + 1 + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Title", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
					}
				}

				// Add the bibid sorts if this is an internal user
                if ((RequestSpecificValues.Current_User != null) && (RequestSpecificValues.Current_User.LoggedOn) && (RequestSpecificValues.Current_User.Is_Internal_User))
				{
					if (RequestSpecificValues.Current_Mode.Sort == 2)
					{
						sort_options_builder.Append("      <option value=\"" + 2 + "\" selected=\"selected\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("BibID Ascending", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
					}
					else
					{
						sort_options_builder.Append("      <option value=\"" + 2 + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("BibID Ascending", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
					}

					if (RequestSpecificValues.Current_Mode.Sort == 3)
					{
						sort_options_builder.Append("      <option value=\"" + 3 + "\" selected=\"selected\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("BibID Descending", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
					}
					else
					{
						sort_options_builder.Append("      <option value=\"" + 3 + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("BibID Descending", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
					}
				}

				// Add the publication date sorts
				if (RequestSpecificValues.Current_Mode.Sort == 10)
				{
					sort_options_builder.Append("      <option value=\"" + 10 + "\" selected=\"selected\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Date Ascending", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
				}
				else
				{
					sort_options_builder.Append("      <option value=\"" + 10 + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Date Ascending", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
				}

				if (RequestSpecificValues.Current_Mode.Sort == 11)
				{
					sort_options_builder.Append("      <option value=\"" + 11 + "\" selected=\"selected\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Date Descending", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
				}
				else
				{
					sort_options_builder.Append("      <option value=\"" + 11 + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Date Descending", RequestSpecificValues.Current_Mode.Language) + "</option>" + Environment.NewLine );
				}

				sortOptions = sort_options_builder.ToString();
			}
		}

		/// <summary> Adds controls to the main navigational page </summary>
		/// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form, widely used throughout the application</param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
		/// <returns> Sorted tree with the results in hierarchical structure with volumes and issues under the titles and sorted by serial hierarchy </returns>
		public void Add_Controls(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
		{
			Tracer.Add_Trace("paged_result_html_subwriter.Add_Controls", "Adding controls for the result set");

			// If the results have facets, this should be rendered in a table with the facets to the left
            if ((resultsStatistics.Has_Facet_Info) && (resultsStatistics.Total_Items > 1) && (!String.Equals(RequestSpecificValues.Current_Mode.Result_Display_Type, "export", StringComparison.OrdinalIgnoreCase)) && (!String.Equals(RequestSpecificValues.Current_Mode.Result_Display_Type, "map", StringComparison.OrdinalIgnoreCase)))
			{
				// Start this table, write the facets, and start the next TD section for the results
				Literal startFacetTable = new Literal { Text = string.Format("<table id=\"sbkPrsw_ResultsOuterTable\">" + Environment.NewLine + "<tr style=\"vertical-align:top;\">" + Environment.NewLine + "<td id=\"sbkPrsw_FacetOuterColumn\">" + Environment.NewLine + "{0}" + Environment.NewLine + "</td>" + Environment.NewLine + "<td>" + Environment.NewLine, Add_Facet_Information(Tracer)) };
				MainPlaceHolder.Controls.Add(startFacetTable);
			}
			else
			{
				Literal startFacetTable = new Literal { Text = "<table style=\"width:100%;\">" + Environment.NewLine + "<tr style=\"vertical-align:top;\">" + Environment.NewLine + "<td style=\"text-align:center\">" + Environment.NewLine };
				MainPlaceHolder.Controls.Add(startFacetTable);
			}

			// Make sure the result writer has been created
			if (resultWriter == null)
				create_resultwriter();
			Debug.Assert(resultWriter != null, "resultWriter != null");
			if (resultWriter == null)
				return;


			if (resultsStatistics.Total_Items == 0)
			{
				resultWriter.Add_HTML(MainPlaceHolder, Tracer);
				return;
			}

            Literal startingLiteral = new Literal { Text = (String.Equals(RequestSpecificValues.Current_Mode.Result_Display_Type, "map", StringComparison.OrdinalIgnoreCase)) ? "</div>" + Environment.NewLine + "<div class=\"sbkPrsw_ResultsPanel\" id=\"main-content\" role=\"main\">" + Environment.NewLine : "<div class=\"sbkPrsw_ResultsPanel\" id=\"main-content\" role=\"main\" itemscope itemtype=\"http:schema.org/SearchResultsPage\">" + Environment.NewLine };
			MainPlaceHolder.Controls.Add(startingLiteral);

			resultWriter.Add_HTML(MainPlaceHolder, Tracer );

            Literal endingLiteral = new Literal { Text = (String.Equals(RequestSpecificValues.Current_Mode.Result_Display_Type, "map", StringComparison.OrdinalIgnoreCase))  ? "</div>" + Environment.NewLine + "<div id=\"pagecontainer_resumed\">" + Environment.NewLine : "</div>" + Environment.NewLine };
			MainPlaceHolder.Controls.Add(endingLiteral);

			// If the results have facets, end the result table
			Literal endResultTable = new Literal {Text = "</td>" + Environment.NewLine + "</tr>" + Environment.NewLine + "</table>" + Environment.NewLine };
			MainPlaceHolder.Controls.Add(endResultTable);
		}

		/// <summary> Writes the final output to close this result view, including the results page navigation buttons </summary>
		/// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
		public override void Write_Final_HTML(TextWriter Output, Custom_Tracer Tracer)
		{
			Tracer.Add_Trace("paged_result_html_subwriter.Write_Final_Html", "Rendering HTML ( finish the main viewer section )");

			if ( resultsStatistics.Total_Items > 0 )
			{
				Output.WriteLine("<div class=\"sbkPrsw_ResultsNavBar\">");
				Output.Write(leftButtons);
				Output.WriteLine("  " + Showing_Text);
				Output.Write(rightButtons);
				Output.WriteLine("</div>");
				Output.WriteLine("<br />");
				Output.WriteLine();
			}
		}

		/// <summary> Writes the HTML generated to browse the list of titles/itemsr  directly to the response stream </summary>
		/// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
		/// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
		/// <returns> TRUE -- Value indicating if html writer should finish the page immediately after this, or if there are other controls or routines which need to be called first </returns>
		public override bool Write_HTML(TextWriter Output, Custom_Tracer Tracer)
		{
			Tracer.Add_Trace("paged_result_html_subwriter.Write_HTML", "Rendering HTML");

                string sort_by = "Sort By";
                string showing_range_text = "{0} - {1} of {2} matching titles";
                string showing_coord_range_text = "{0} - {1} of {2} matching coordinates";

                if (RequestSpecificValues.Current_Mode.Aggregation == "aerials")
                {
                    showing_coord_range_text = "{0} - {1} of {2} matching flights";
                }

                if (RequestSpecificValues.Current_Mode.Language == Web_Language_Enum.Spanish)
                {
                    sort_by = "Organizar";
                    showing_range_text = "{0} - {1} de {2} t�tulos correspondientes";
                }

                if (RequestSpecificValues.Current_Mode.Language == Web_Language_Enum.French)
                {
                    sort_by = "Limiter";
                    showing_range_text = "{0} - {1} de {2} titres correspondants";
                }

                if ( String.Equals(RequestSpecificValues.Current_Mode.Result_Display_Type, "map", StringComparison.OrdinalIgnoreCase ))
                    showing_range_text = showing_coord_range_text;

                Display_Mode_Enum initialMode = RequestSpecificValues.Current_Mode.Mode;

                Tracer.Add_Trace("paged_result_html_subwriter.Write_HTML", "Building appropriate ResultsWriter");

                RequestSpecificValues.Current_Mode.Mode = initialMode;
                if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Search)
                    RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Results;

                // If no results, display different information here
                if ((RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Results) && (resultsStatistics.Total_Items == 0))
                {
                    Output.WriteLine("<div class=\"sbkPrsw_DescPanel\" style=\"margin-top:10px\">");
                    Show_Search_Info(Output, true);
                    Output.WriteLine("</div>");
                    Output.WriteLine("<div class=\"sbkPrsw_ResultsNavBar\">&nbsp;</div>");
                    return true;
                }

                // Make sure the result writer has been created
                if (resultWriter == null)
                    create_resultwriter();
                Debug.Assert(resultWriter != null, "resultWriter != null");
                if (resultWriter == null)
                    return false;

                // Determine which rows are being displayed
                int current_page_last = RequestSpecificValues.Current_Mode.Page.HasValue ? RequestSpecificValues.Current_Mode.Page.Value : 1;
                int lastRow = current_page_last * RESULTS_PER_PAGE;
                int startRow = lastRow - 19;

                // Start the form for this, unless we are already in an appropriate form
                if (Outer_Form_Name.Length == 0)
                {
                    string post_url = HttpUtility.HtmlEncode(HttpContext.Current.Items["Original_URL"].ToString());
                    Output.WriteLine("<form name=\"sort_form\" id=\"addedForm\" method=\"post\" action=\"" + post_url + "\" >");
                }

                // Get the name of this
                string currentName = "browse";
                string currentTitle = "Browse";
                if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Results)
                {
                    currentName = "search";
                    currentTitle = "Search";
                }
                if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Public_Folder)
                {
                    currentName = "public bookshelf";
                    currentTitle = "Public Bookshelf";
                }

                // Load the HTML that can be used to customize the search/results bar
                string html_source = String.Empty;
                string fileToRead = HttpContext.Current.Server.MapPath("default/fragments/search_browse_bar.html");
                if (File.Exists(fileToRead))
                {
                    html_source = File.ReadAllText(fileToRead);
                }

                // Get the value for the <%SORTER%> directive (to sort the results)
                string SORTER = String.Empty;
                if ((resultWriter.Sortable) && (!RequestSpecificValues.Current_Mode.Is_Robot) && (RequestSpecificValues.Current_Mode.Mode != Display_Mode_Enum.My_Sobek) && (RequestSpecificValues.Current_Mode.Mode != Display_Mode_Enum.Public_Folder))
                {
                    StringBuilder sorterBuilder = new StringBuilder("  <div class=\"sbkPrsw_ResultsSort\">");
                    short current_order = RequestSpecificValues.Current_Mode.Sort.HasValue ? RequestSpecificValues.Current_Mode.Sort.Value : ((short) 0 );
                    RequestSpecificValues.Current_Mode.Sort = 0;
                    string url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
                    RequestSpecificValues.Current_Mode.Sort = current_order;
                    sorterBuilder.AppendLine("    <label for=\"sorter_input\">" + sort_by + "</label>: &nbsp;");
                    sorterBuilder.AppendLine("    <select name=\"sorter_input\" onchange=\"sort_results('" + url.Replace("&", "&amp;") + "')\" id=\"sorter_input\" class=\"sbkPrsw_SorterDropDown\">");
                    sorterBuilder.AppendLine(sortOptions);
                    sorterBuilder.AppendLine("    </select>");
                    sorterBuilder.AppendLine("  </div>");
                    SORTER = sorterBuilder.ToString();
                }

                // Get the value for the <%DESCRIPTION%> directive (to explain current display)
                string DESCRIPTION = String.Empty;
                string summation;
                if ((RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation) || (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Public_Folder) || ((RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.My_Sobek) && (RequestSpecificValues.Current_Mode.My_Sobek_Type == My_Sobek_Type_Enum.Folder_Management))) // browse info only for aggregation
                {
                    if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Public_Folder)
                    {
                        DESCRIPTION = "<h1>&quot;" + UI_ApplicationCache_Gateway.Translation.Get_Translation(Browse_Title, RequestSpecificValues.Current_Mode.Language) + "&quot;</h1>" + Environment.NewLine + "  <span class=\"sbkPrsw_PublicFolderAuthor\">This is a publicly shared bookshelf of <a href=\"mailto:" + Folder_Owner_Email + "\">" + Folder_Owner_Name + "</a>.</span>";

                        summation = UI_ApplicationCache_Gateway.Translation.Get_Translation(Browse_Title, RequestSpecificValues.Current_Mode.Language) + " (publicly shared folder)";
                    }
                    else
                    {
                        DESCRIPTION = "<h1>" + UI_ApplicationCache_Gateway.Translation.Get_Translation(Browse_Title, RequestSpecificValues.Current_Mode.Language) + "</h1>";
                        summation = UI_ApplicationCache_Gateway.Translation.Get_Translation(Browse_Title, RequestSpecificValues.Current_Mode.Language) + " browse in " + hierarchyObject.Name;
                    }
                }
                else
                {
                    StringBuilder descriptionBuilder = new StringBuilder();
                    descriptionBuilder.Append("<div class=\"sbkPrsw_ResultsExplanation\">");
                    StringBuilder searchInfoBuilder = new StringBuilder();
                    StringWriter writer = new StringWriter(searchInfoBuilder);
                    Show_Search_Info(writer, UI_ApplicationCache_Gateway.Settings.Search.Include_Result_Count_In_Search_Text);
                    summation = remove_html_tags(searchInfoBuilder.ToString()).Replace("\"", "").Replace("'", "").Replace("\n", "").Replace("\r", "").Replace("&", "%26");
                    descriptionBuilder.Append(searchInfoBuilder);
                    descriptionBuilder.Append("</div>");
                    DESCRIPTION = descriptionBuilder.ToString();
                }


                // Get the value for the <%DESCRIPTION%> directive (to explain current display)
                //ushort current_page = currentMode.Page;
                string SHOWING = String.Empty;
                if ( String.Equals( RequestSpecificValues.Current_Mode.Result_Display_Type, "export", StringComparison.OrdinalIgnoreCase))
                {
                    SHOWING = resultsStatistics.Total_Items.ToString();
                }
                else
                {
                    SHOWING = String.Format(showing_range_text, startRow, Math.Min(lastRow, resultsStatistics.Total_Titles), resultWriter.Total_Results);
                    if (startRow == lastRow)
                    {
                        SHOWING = Showing_Text.Replace(startRow + " - " + startRow, startRow + " ");
                    }
                }

                // Get the values for the <%LEFTBUTTONS%> and <%RIGHTBUTTONS%>
                string LEFT_BUTTONS = String.Empty;
                string RIGHT_BUTTONS = String.Empty;
                string first_page = "First Page";
                string previous_page = "Previous Page";
                string next_page = "Next Page";
                string last_page = "Last Page";
                string first_page_text = "First";
                string previous_page_text = "Previous";
                string next_page_text = "Next";
                string last_page_text = "Last";

                if (RequestSpecificValues.Current_Mode.Language == Web_Language_Enum.Spanish)
                {
                    first_page = "Primera P�gina";
                    previous_page = "P�gina Anterior";
                    next_page = "P�gina Siguiente";
                    last_page = "�ltima P�gina";
                    first_page_text = "Primero";
                    previous_page_text = "Anterior";
                    next_page_text = "Proximo";
                    last_page_text = "�ltimo";
                }

                if (RequestSpecificValues.Current_Mode.Language == Web_Language_Enum.French)
                {
                    first_page = "Premi�re Page";
                    previous_page = "Page Pr�c�dente";
                    next_page = "Page Suivante";
                    last_page = "Derni�re Page";
                    first_page_text = "Premi�re";
                    previous_page_text = "Pr�c�dente";
                    next_page_text = "Suivante";
                    last_page_text = "Derniere";
                }

                // Make sure the result writer has been created
                if (resultWriter == null)
                    create_resultwriter();
                if (resultWriter != null)
                {
                    Debug.Assert(resultWriter != null, "resultWriter != null");

                    if (RESULTS_PER_PAGE < resultWriter.Total_Results)
                    {
                        ushort current_page = RequestSpecificValues.Current_Mode.Page.HasValue ? RequestSpecificValues.Current_Mode.Page.Value : ((ushort) 1);
                        StringBuilder buttons_builder = new StringBuilder(1000);

                        // Should the previous and first buttons be enabled?
                        if (current_page > 1)
                        {
                            buttons_builder.Append("<div class=\"sbkPrsw_LeftButtons\">");
                            RequestSpecificValues.Current_Mode.Page = 1;
                            buttons_builder.Append("<button title=\"" + first_page + "\" class=\"sbkPrsw_RoundButton\" onclick=\"window.location='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("&", "&amp;") + "'; return false;\"><img src=\"" + Static_Resources_Gateway.Button_First_Arrow_Png + "\" class=\"roundbutton_img_left\" alt=\"\" />" + first_page_text + "</button>&nbsp;");
                            RequestSpecificValues.Current_Mode.Page = (ushort)(current_page - 1);
                            buttons_builder.Append("<button title=\"" + previous_page + "\" class=\"sbkPrsw_RoundButton\" onclick=\"window.location='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("&", "&amp;") + "'; return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"roundbutton_img_left\" alt=\"\" />" + previous_page_text + "</button>");
                            buttons_builder.Append("</div>");
                            LEFT_BUTTONS = buttons_builder.ToString();
                            buttons_builder.Clear();
                        }
                        else
                        {
                            LEFT_BUTTONS = "<div class=\"sbkPrsw_NoLeftButtons\">&nbsp;</div>";
                        }


                        // Should the next and last buttons be enabled?
                        if ((current_page * RESULTS_PER_PAGE) < resultWriter.Total_Results)
                        {
                            buttons_builder.Append("<div class=\"sbkPrsw_RightButtons\">");
                            RequestSpecificValues.Current_Mode.Page = (ushort)(current_page + 1);
                            buttons_builder.Append("<button title=\"" + next_page + "\" class=\"sbkPrsw_RoundButton\" onclick=\"window.location='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("&", "&amp;") + "'; return false;\">" + next_page_text + "<img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"roundbutton_img_right\" alt=\"\" /></button>&nbsp;");
                            RequestSpecificValues.Current_Mode.Page = (ushort)(resultWriter.Total_Results / RESULTS_PER_PAGE);
                            if (resultWriter.Total_Results % RESULTS_PER_PAGE > 0)
                                RequestSpecificValues.Current_Mode.Page = (ushort)(RequestSpecificValues.Current_Mode.Page + 1);
                            buttons_builder.Append("<button title=\"" + last_page + "\" class=\"sbkPrsw_RoundButton\" onclick=\"window.location='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("&", "&amp;") + "'; return false;\">" + last_page_text + "<img src=\"" + Static_Resources_Gateway.Button_Last_Arrow_Png + "\" class=\"roundbutton_img_right\" alt=\"\" /></button>");
                            buttons_builder.Append("</div>");
                            RIGHT_BUTTONS = buttons_builder.ToString();
                        }
                        else
                        {
                            RIGHT_BUTTONS = "<div class=\"sbkPrsw_NoRightButtons\">&nbsp;</div>";
                        }

                        RequestSpecificValues.Current_Mode.Page = current_page;
                    }
                }

                // Empty strings for now
                //string brief_view = "BRIEF VIEW";
                //string map_view = "MAP VIEW";
                //string table_view = "TABLE VIEW";
                //string thumbnail_view = "THUMBNAIL VIEW";
                //if (RequestSpecificValues.Current_Mode.Language == Web_Language_Enum.Spanish)
                //{
                //    map_view = "VISTA MAPA";
                //    brief_view = "VISTA BREVE";
                //    table_view = "VISTA TABLERA";
                //    thumbnail_view = "VISTA MINIATURA";
                //}
                //if (RequestSpecificValues.Current_Mode.Language == Web_Language_Enum.French)
                //{
                //    map_view = "MODE CARTE";
                //    brief_view = "MODE SIMPLE";
                //    table_view = "MODE DE TABLE";
                //    thumbnail_view = "MODE IMAGETTE";
                //}
                string resultView = RequestSpecificValues.Current_Mode.Result_Display_Type;
                StringBuilder iconBuilder = new StringBuilder(1000);
                iconBuilder.AppendLine();
                iconBuilder.AppendLine("    <div class=\"sbkPrsw_ViewIconButtons\">");


                // There SHOULD be results views here
                if (hierarchyObject.Result_Views != null)
                {
                    // Step through all enabled viewers
                    foreach (string resultWriterType in hierarchyObject.Result_Views)
                    {
                        // Get the corresponding config
                        ResultsSubViewerConfig resultConfig = UI_ApplicationCache_Gateway.Configuration.UI.WriterViewers.Results.GetViewerByType(resultWriterType);

                        // Must exist and be enabled here
                        if ((resultConfig == null) || (!resultConfig.Enabled)) continue;

                        // Get the label and description
                        string view_label = UI_ApplicationCache_Gateway.Translation.Get_Translation(resultConfig.Label, RequestSpecificValues.Current_Mode.Language);
                        string view_hover = UI_ApplicationCache_Gateway.Translation.Get_Translation(resultConfig.Description, RequestSpecificValues.Current_Mode.Language);
                        string view_icon = resultConfig.Icon;

                        // Some special icon cases here
                        switch (view_icon)
                        {
                            case "[Brief_Blue_Img]":
                                view_icon = UI_ApplicationCache_Gateway.Configuration.UI.StaticResources.Brief_Blue_Img;
                                break;

                            case "[Geo_Blue_Png]":
                                view_icon = UI_ApplicationCache_Gateway.Configuration.UI.StaticResources.Geo_Blue_Png;
                                break;

                            case "[Table_Blue_Png]":
                                view_icon = UI_ApplicationCache_Gateway.Configuration.UI.StaticResources.Table_Blue_Png;
                                break;

                            case "[Thumb_Blue_Png]":
                                view_icon = UI_ApplicationCache_Gateway.Configuration.UI.StaticResources.Thumb_Blue_Png;
                                break;
                        }

                        // For now, only show the map if there was a coordinate search
                        if ((String.Equals("map", resultConfig.ViewerCode, StringComparison.OrdinalIgnoreCase)) && (String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Coordinates)))
                            continue;

                        // is this the current view?
                        if (String.Equals(resultView, resultConfig.ViewerCode, StringComparison.OrdinalIgnoreCase))
                        {
                            iconBuilder.AppendLine("      <img src=\"" + view_icon + "\" alt=\"" + view_label + "\" class=\"sbkPrsw_ViewIconButtonCurrent\"/>");
                        }
                        else
                        {
                            RequestSpecificValues.Current_Mode.Result_Display_Type = resultConfig.ViewerCode;
                            iconBuilder.AppendLine("      <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("&", "&amp;") + "\" title=\"" + view_label + "\"><img src=\"" + view_icon + "\" alt=\"" + view_label + "\" class=\"sbkPrsw_ViewIconButton\"/></a>");
                        }
                    }
                }

                RequestSpecificValues.Current_Mode.Result_Display_Type = resultView;
                iconBuilder.AppendLine("    </div>");
                string VIEWICONS = iconBuilder.ToString();
                string NEWSEARCH = String.Empty;
                string ADDFILTER = String.Empty;

                // Start the division for the sort and then description and buttons, etc..
                switch (RequestSpecificValues.Current_Mode.Mode)
                {
                    case Display_Mode_Enum.Public_Folder:
                        Output.WriteLine("<div class=\"sbkPrsw_DescPanel sbkPrsw_FolderDescPanel\">");
                        break;

                    case Display_Mode_Enum.Aggregation:  // browse info only
                        Output.WriteLine("<div class=\"sbkPrsw_DescPanel sbkPrsw_BrowseDescPanel\">");
                        break;

                    default:
                        Output.WriteLine("<div class=\"sbkPrsw_DescPanel sbkPrsw_ResultsDescPanel\">");
                        break;
                }

                // Now, write this 
                Output.WriteLine(html_source.Replace("<%DESCRIPTION%>", DESCRIPTION).Replace("<%NEWSEARCH%>", NEWSEARCH).Replace("<%ADDFILTER%>", ADDFILTER).Replace("<%VIEWICONS%>", VIEWICONS).Replace("<%LEFTBUTTONS%>", LEFT_BUTTONS).Replace("<%SHOWING%>", SHOWING).Replace("<%RIGHTBUTTONS%>", RIGHT_BUTTONS).Replace("<%SORTER%>", SORTER));

                // End this division
                Output.WriteLine("</div>");
                Output.WriteLine();

                // Configure the way to remove search terms
                if ((UI_ApplicationCache_Gateway.Settings.Search.Can_Remove_Single_Term) && (term_counter > 0))
                {
                    Output.WriteLine("<script>");
                    for (int i = 1; i <= term_counter; i++)
                    {
                        Output.WriteLine("  init_search_term('searchterm" + i + "', 'removesearchterm" + i + "');");
                    }
                    Output.WriteLine("</script>");
                    Output.WriteLine();
                }

                // Save the buttons for later, to be used at the bottom of the page
                leftButtons = LEFT_BUTTONS;
                rightButtons = RIGHT_BUTTONS;

                // Determine the number of columns for text areas, depending on browser
                int actual_cols = 50;
                if ((!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Browser_Type)) && (RequestSpecificValues.Current_Mode.Browser_Type.ToUpper().IndexOf("FIREFOX") >= 0))
                    actual_cols = 45;

                // Add the hidden field
                Output.WriteLine("<!-- Hidden field is used for postbacks to indicate what to save and reset -->");
                if (RequestSpecificValues.Current_Mode.Mode != Display_Mode_Enum.My_Sobek)
                {
                    Output.WriteLine("<input type=\"hidden\" id=\"item_action\" name=\"item_action\" value=\"\" />");
                }
                Output.WriteLine("<input type=\"hidden\" id=\"url_description\" name=\"url_description\" value=\"" + HttpUtility.HtmlEncode(summation) + "\" />");
                Output.WriteLine("<input type=\"hidden\" id=\"list_type\" name=\"list_type\" value=\"" + currentName + "\" />");
                Output.WriteLine();

                // Add the scripts needed
                if (RequestSpecificValues.Current_Mode.Mode != Display_Mode_Enum.My_Sobek)
                {
                    Output.WriteLine("<script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Ui_1_10_3_Custom_Js + "\"></script>");
                    Output.WriteLine();
                }

                #region Email form

                if (RequestSpecificValues.Current_User != null)
                {
                    Output.WriteLine("<!-- Email form -->");
                    Output.WriteLine("<div class=\"form_email sbk_PopupForm\" id=\"form_email\" style=\"display:none;\">");
                    Output.WriteLine("  <div class=\"sbk_PopupTitle\"><table style=\"width:100%\"><tr><td style=\"text-align:left\">Send this " + currentTitle + " to a Friend</td><td style=\"text-align:right\"> <a href=\"#template\" alt=\"CLOSE\" onclick=\"email_form_close()\">X</a> &nbsp; </td></tr></table></div>");
                    Output.WriteLine("  <br />");
                    Output.WriteLine("  <fieldset><legend>Enter the email information below &nbsp; </legend>");
                    Output.WriteLine("    <br />");
                    Output.WriteLine("    <table class=\"sbk_PopupTable\">");


                    // Add email address line
                    Output.Write("      <tr><td style=\"width:80px\"><label for=\"email_address\">To:</label></td>");
                    Output.WriteLine("<td><input class=\"email_input sbk_Focusable\" name=\"email_address\" id=\"email_address\" type=\"text\" value=\"" + RequestSpecificValues.Current_User.Email + "\" /></td></tr>");

                    // Add comments area
                    Output.Write("      <tr style=\"vertical-align:top\"><td><br /><label for=\"email_comments\">Comments:</label></td>");
                    Output.WriteLine("<td><textarea rows=\"6\" cols=\"" + actual_cols + "\" name=\"email_comments\" id=\"email_comments\" class=\"email_textarea sbk_Focusable\" ></textarea></td></tr>");

                    // Add format area
                    Output.Write("      <tr style=\"vertical-align:top\"><td>Format:</td>");
                    Output.Write("<td><input type=\"radio\" name=\"email_format\" id=\"email_format_html\" value=\"html\" checked=\"checked\" /> <label for=\"email_format_html\">HTML</label> &nbsp; &nbsp; ");
                    Output.WriteLine("<input type=\"radio\" name=\"email_format\" id=\"email_format_text\" value=\"text\" /> <label for=\"email_format_text\">Plain Text</label></td></tr>");


                    Output.WriteLine("    </table>");
                    Output.WriteLine("    <br />");
                    Output.WriteLine("  </fieldset><br />");
                    Output.WriteLine("  <div style=\"text-align:center; font-size:1.3em;\">");
                    Output.WriteLine("    <button title=\"Send\" class=\"roundbutton\" onclick=\"return email_form_close();\"> CANCEL </button> &nbsp; &nbsp; ");
                    Output.WriteLine("    <button title=\"Send\" class=\"roundbutton\" type=\"submit\"> SEND </button>");
                    Output.WriteLine("  </div><br />");
                    Output.WriteLine("</div>");
                    Output.WriteLine();

                }

                #endregion

                if (RequestSpecificValues.Current_Mode.Mode != Display_Mode_Enum.My_Sobek)
                {

                    #region Save search/browse form

                    if (RequestSpecificValues.Current_User != null)
                    {
                        Output.WriteLine("<!-- Save search/browse -->");
                        Output.WriteLine("<div class=\"add_popup_div\" id=\"save_search_form\" style=\"display:none;\">");
                        Output.WriteLine("  <div class=\"popup_title\"><table width=\"100%\"><tr><td align=\"left\">A<span class=\"smaller\">DD THIS</span> " + currentTitle + "<span class=\"smaller\"> TO YOUR</span> S<span class=\"smaller\">AVED</span> S<span class=\"smaller\">EARCHES</span></td><td align=\"right\"> <a href=\"#template\" alt=\"CLOSE\" onclick=\"save_search_form_close()\">X</a> &nbsp; </td></tr></table></div>");
                        Output.WriteLine("  <br />");
                        Output.WriteLine("  <fieldset><legend>Enter notes for this " + currentName + " &nbsp; </legend>");
                        Output.WriteLine("    <br />");
                        Output.WriteLine("    <table class=\"popup_table\">");

                        // Add comments area
                        Output.Write("      <tr align=\"left\" valign=\"top\"><td><br /><label for=\"add_notes\">Description:</label></td>");
                        Output.WriteLine("<td><textarea rows=\"8\" cols=\"" + actual_cols + "\" name=\"add_notes\" id=\"add_notes\" class=\"add_notes_textarea\" onfocus=\"javascript:textbox_enter('add_notes','add_notes_textarea_focused')\" onblur=\"javascript:textbox_leave('add_notes','add_notes_textarea')\">" + summation + "</textarea></td></tr>");
                        Output.WriteLine("      <tr align=\"left\" valign=\"top\"><td>&nbsp;</td><td><input type=\"checkbox\" id=\"open_searches\" name=\"open_searches\" value=\"open\" /> <label for=\"open_searches\">Open saved searches in new window</label></td></tr>");
                        Output.WriteLine("    </table>");
                        Output.WriteLine("    <br />");
                        Output.WriteLine("  </fieldset><br />");
                        Output.WriteLine("  <center><a href=\"\" onclick=\"return save_search_form_close();\"><img border=\"0\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.Current_Mode.Base_Skin_Or_Skin + "/buttons/cancel_button_g.gif\" alt=\"CLOSE\" /></a> &nbsp; &nbsp; <input type=\"image\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.Current_Mode.Base_Skin_Or_Skin + "/buttons/save_button_g.gif\" value=\"Submit\" alt=\"Submit\"></center><br />");
                        Output.WriteLine("</div>");
                        Output.WriteLine();
                    }

                    #endregion

                    #region Share form

                    // Calculate the title and url
                    string title = HttpUtility.HtmlEncode(summation);
                    string share_url = HttpContext.Current.Items["Original_URL"].ToString().Replace("&", "%26").Replace("?", "%3F").Replace("http://", "").Replace("=", "%3D").Replace("\"", "&quot;");

                    Output.WriteLine("<!-- Share form -->");
                    Output.WriteLine("<div class=\"share_popup_div\" id=\"share_form\" style=\"display:none;\">");

                    Output.WriteLine("<a href=\"http://www.facebook.com/share.php?u=" + share_url + "&amp;t=" + title + "\" target=\"FACEBOOK_WINDOW\" onmouseover=\"facebook_share.src='" + Static_Resources_Gateway.Facebook_Share_H_Gif + "'\" onfocus=\"facebook_share.src='" + Static_Resources_Gateway.Facebook_Share_H_Gif + "'\" onmouseout=\"facebook_share.src='" + Static_Resources_Gateway.Facebook_Share_Gif + "'\" onblur=\"facebook_share.src='" + Static_Resources_Gateway.Facebook_Share_Gif + "'\" onclick=\"\"><img class=\"ResultSavePrintButtons\" border=\"0px\" id=\"facebook_share\" name=\"facebook_share\" src=\"" + Static_Resources_Gateway.Facebook_Share_Gif + "\" alt=\"FACEBOOK\" /></a>");
                    Output.WriteLine("<a href=\"http://buzz.yahoo.com/buzz?targetUrl=" + share_url + "&amp;headline=" + title + "\" target=\"YAHOOBUZZ_WINDOW\" onmouseover=\"yahoobuzz_share.src='" + Static_Resources_Gateway.Yahoobuzz_Share_H_Gif + "'\" onfocus=\"yahoobuzz_share.src='" + Static_Resources_Gateway.Yahoobuzz_Share_H_Gif + "'\" onmouseout=\"yahoobuzz_share.src='" + Static_Resources_Gateway.Yahoobuzz_Share_Gif + "'\" onblur=\"yahoobuzz_share.src='" + Static_Resources_Gateway.Yahoobuzz_Share_Gif + "'\" onclick=\"\"><img class=\"ResultSavePrintButtons\" border=\"0px\" id=\"yahoobuzz_share\" name=\"yahoobuzz_share\" src=\"" + Static_Resources_Gateway.Yahoobuzz_Share_Gif + "\" alt=\"YAHOO BUZZ\" /></a>");
                    Output.WriteLine("<br />");

                    Output.WriteLine("<a href=\"http://twitter.com/home?status=Currently reading " + share_url + "\" target=\"TWITTER_WINDOW\" onmouseover=\"twitter_share.src='" + Static_Resources_Gateway.Twitter_Share_H_Gif + "'\" onfocus=\"twitter_share.src='" + Static_Resources_Gateway.Twitter_Share_H_Gif + "'\" onmouseout=\"twitter_share.src='" + Static_Resources_Gateway.Twitter_Share_Gif + "'\" onblur=\"twitter_share.src='" + Static_Resources_Gateway.Twitter_Share_Gif + "'\" onclick=\"\"><img class=\"ResultSavePrintButtons\" border=\"0px\" id=\"twitter_share\" name=\"twitter_share\" src=\"" + Static_Resources_Gateway.Twitter_Share_Gif + "\" alt=\"TWITTER\" /></a>");
                    Output.WriteLine("<a href=\"http://www.google.com/bookmarks/mark?op=add&amp;bkmk=" + share_url + "&amp;title=" + title + "\" target=\"GOOGLE_WINDOW\" onmouseover=\"google_share.src='" + Static_Resources_Gateway.Google_Share_H_Gif + "'\" onfocus=\"google_share.src='" + Static_Resources_Gateway.Google_Share_H_Gif + "'\" onmouseout=\"google_share.src='" + Static_Resources_Gateway.Google_Share_Gif + "'\" onblur=\"google_share.src='" + Static_Resources_Gateway.Google_Share_Gif + "'\" onclick=\"\"><img class=\"ResultSavePrintButtons\" border=\"0px\" id=\"google_share\" name=\"google_share\" src=\"" + Static_Resources_Gateway.Google_Share_Gif + "\" alt=\"GOOGLE SHARE\" /></a>");
                    Output.WriteLine("<br />");

                    Output.WriteLine("<a href=\"http://www.stumbleupon.com/submit?url=" + share_url + "&amp;title=" + title + "\" target=\"STUMBLEUPON_WINDOW\" onmouseover=\"stumbleupon_share.src='" + Static_Resources_Gateway.Stumbleupon_Share_H_Gif + "'\" onfocus=\"stumbleupon_share.src='" + Static_Resources_Gateway.Stumbleupon_Share_H_Gif + "'\" onmouseout=\"stumbleupon_share.src='" + Static_Resources_Gateway.Stumbleupon_Share_Gif + "'\" onblur=\"stumbleupon_share.src='" + Static_Resources_Gateway.Stumbleupon_Share_Gif + "'\" onclick=\"\"><img class=\"ResultSavePrintButtons\" border=\"0px\" id=\"stumbleupon_share\" name=\"stumbleupon_share\" src=\"" + Static_Resources_Gateway.Stumbleupon_Share_Gif + "\" alt=\"STUMBLEUPON\" /></a>");
                    Output.WriteLine("<a href=\"http://myweb.yahoo.com/myresults/bookmarklet?t=" + title + "&amp;u=" + share_url + "\" target=\"YAHOO_WINDOW\" onmouseover=\"yahoo_share.src='" + Static_Resources_Gateway.Yahoo_Share_H_Gif + "'\" onfocus=\"yahoo_share.src='" + Static_Resources_Gateway.Yahoo_Share_H_Gif + "'\" onmouseout=\"yahoo_share.src='" + Static_Resources_Gateway.Yahoo_Share_Gif + "'\" onblur=\"yahoo_share.src='" + Static_Resources_Gateway.Yahoo_Share_Gif + "'\" onclick=\"\"><img class=\"ResultSavePrintButtons\" border=\"0px\" id=\"yahoo_share\" name=\"yahoo_share\" src=\"" + Static_Resources_Gateway.Yahoo_Share_Gif + "\" alt=\"YAHOO SHARE\" /></a>");
                    Output.WriteLine("<br />");

                    Output.WriteLine("<a href=\"http://digg.com/submit?phase=2&amp;url=" + share_url + "&amp;title=" + title + "\" target=\"DIGG_WINDOW\" onmouseover=\"digg_share.src='" + Static_Resources_Gateway.Digg_Share_H_Gif + "'\" onfocus=\"digg_share.src='" + Static_Resources_Gateway.Digg_Share_H_Gif + "'\" onmouseout=\"digg_share.src='" + Static_Resources_Gateway.Digg_Share_Gif + "'\" onblur=\"digg_share.src='" + Static_Resources_Gateway.Digg_Share_Gif + "'\" onclick=\"\"><img class=\"ResultSavePrintButtons\" border=\"0px\" id=\"digg_share\" name=\"digg_share\" src=\"" + Static_Resources_Gateway.Digg_Share_Gif + "\" alt=\"DIGG\" /></a>");
                    Output.WriteLine("<a onmouseover=\"favorites_share.src='" + Static_Resources_Gateway.Favorites_Share_H_Gif + "'\" onfocus=\"favorites_share.src='" + Static_Resources_Gateway.Favorites_Share_H_Gif + "'\" onmouseout=\"favorites_share.src='" + Static_Resources_Gateway.Favorites_Share_Gif + "'\" onblur=\"favorites_share.src='" + Static_Resources_Gateway.Favorites_Share_Gif + "'\" onclick=\"javascript:add_to_favorites();\"><img class=\"ResultSavePrintButtons\" border=\"0px\" id=\"favorites_share\" name=\"favorites_share\" src=\"" + Static_Resources_Gateway.Favorites_Share_Gif + "\" alt=\"MY FAVORITES\" /></a>");
                    Output.WriteLine("<br />");

                    Output.WriteLine("</div>");
                    Output.WriteLine();

                    #endregion

                }

                if (Outer_Form_Name.Length == 0)
                    Output.WriteLine("</form>");


			return true;
		}

        private static string remove_html_tags(string MixedHtmlText )
        {
            StringBuilder builder = new StringBuilder(MixedHtmlText.Length);
            bool inTag = false;
            char lastChar = '_';
            foreach (char thisChar in MixedHtmlText)
            {
                switch( thisChar )
                {
                    case '>':
                        inTag = false;
                        break;

                    case '<':
                        inTag = true;
                        break;

                    case ' ':
                        if ((!inTag) && ( lastChar != ' ' ))
                        {
                            builder.Append(' ');
                            lastChar = ' ';
                        }
                        break;

                    default:
                        if (!inTag)
                        {
                            builder.Append(thisChar);
                            lastChar = thisChar;
                        }
                        break;
                }
            }

            return builder.ToString();
        }

		/// <summary> Renders the text about this search (i.e., &quot;Your search for ... resulted in ...&quot; )
		/// directly to the output stream </summary>
		/// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
        /// <param name="IncludeResultCount"> Flag tells whether to include the number of results in the text </param>
		protected void Show_Search_Info(TextWriter Output, bool IncludeResultCount )
		{
			string and_language = "and ";
			string or_language = "or ";
			string and_not_language = "not ";
			string no_matches_language = "resulted in no matching records.";
			string one_match_language = "resulted in one matching record.";
			string multiple_records_language = "resulted in {0} matching records.";
			string one_item_language = "resulted in one item in ";
			string multiple_items_language = "resulted in {0} items in ";
			string one_title_language = "one title.";
			string multiple_titles_language = " titles.";

			string between_two_dates = "between {0} and {1} ";
			string on_one_date = "in {0} ";

			// Set special language for aerials
			if (RequestSpecificValues.Current_Mode.Aggregation == "aerials")
			{
				no_matches_language = "resulted in no matching flights.";
				one_match_language = "resulted in one matching flight.";
				multiple_records_language = "resulted in {0} matching flights.";
				one_item_language = "resulted in one flight in ";
				multiple_items_language = "resulted in {0} flights in ";
				one_title_language = "one county.";
				multiple_titles_language = " counties.";
			}
			
			switch (RequestSpecificValues.Current_Mode.Language)
			{
				case Web_Language_Enum.French:
					Output.Write("Votre recherche de <i>" + hierarchyObject.Name + "</i> en ");
					and_language = "et ";
					or_language = "ou ";
					and_not_language = "non ";

					no_matches_language = "aucun des documents correspondants.";
					one_match_language = ", correpsonde � 1 document.";
					multiple_records_language = ", correpsonde � {0} documents";
					one_item_language = ", correpsonde � 1 document en ";
					multiple_items_language = ", correpsonde � {0} documents en ";
					one_title_language = "1 titre.";
					multiple_titles_language = " titres.";
					break;

				case Web_Language_Enum.Spanish:
					Output.Write("Su b�squeda de <i>" + hierarchyObject.Name + "</i> en ");
					and_language = "y ";
					or_language = "o ";
					and_not_language = "no ";

					no_matches_language = "no dio lugar a los objetos.";
					one_match_language = ", result� en 1 objeto.";
					multiple_records_language = ", result� en {0} objetos.";
					one_item_language = ", result� en 1 objeto en ";
					multiple_items_language = ", result� en {0} objetos en ";
					one_title_language = "1 t�tulo.";
					multiple_titles_language = " t�tulos.";
					break;

				default:
					if ((RequestSpecificValues.Current_Mode.Search_Type == Search_Type_Enum.Map)||(RequestSpecificValues.Current_Mode.Search_Type == Search_Type_Enum.Map_Beta))
						Output.Write("Your geographic search of <i>" + hierarchyObject.Name + "</i> ");
					else
						Output.Write("Your search of <i>" + hierarchyObject.Name + "</i> for ");
					break;
			}

			// Split the parts
			if ((RequestSpecificValues.Current_Mode.Search_Type != Search_Type_Enum.Map)||(RequestSpecificValues.Current_Mode.Search_Type != Search_Type_Enum.Map_Beta))
			{
			    List<string> terms = new List<string>();
				List<string> fields = new List<string>();

				// Split the terms correctly
				SobekCM_Assistant.Split_Clean_Search_Terms_Fields(RequestSpecificValues.Current_Mode.Search_String, RequestSpecificValues.Current_Mode.Search_Fields, RequestSpecificValues.Current_Mode.Search_Type, terms, fields, UI_ApplicationCache_Gateway.Search_Stop_Words, RequestSpecificValues.Current_Mode.Search_Precision, ',');

				try
				{
					// Create this differently depending on whether users can remove a search term from their current search
					if (UI_ApplicationCache_Gateway.Settings.Search.Can_Remove_Single_Term)
					{
						string current_search_string = RequestSpecificValues.Current_Mode.Search_String;
						string current_search_field = RequestSpecificValues.Current_Mode.Search_Fields;
						Display_Mode_Enum current_display_mode = RequestSpecificValues.Current_Mode.Mode;
						Aggregation_Type_Enum current_aggr_mode = RequestSpecificValues.Current_Mode.Aggregation_Type;
						string current_info_browse_mode = RequestSpecificValues.Current_Mode.Info_Browse_Mode;

						StringBuilder fieldsBuilder = new StringBuilder();
						StringBuilder termsBuilder = new StringBuilder();

						term_counter = 0;
						for (int i = 0; i < Math.Min(terms.Count, fields.Count); i++)
						{
							if ((terms[i].Length > 0) && (fields[i].Length > 0))
							{
								Output.WriteLine();
								Output.Write("        ");

								// Remove the leading + sign
								if (fields[i][0] == '+')
									fields[i] = fields[i].Substring(1);
								if (fields[i][0] == ' ')
									fields[i] = fields[i].Substring(1);

								// Add the 'AND' value
								if (i > 0)
								{
									if (fields[i][0] == '=')
									{
										Output.Write(or_language);
									    fields[i] = fields[i].Substring(1);
									}
									else
									{
										Output.Write(and_language);
									}
								}

								//// This explanataion need to be capped
								//if (length_of_explanation >= 160)
								//{
								//	Output.Write("... ");
								//	break;
								//}

								term_counter++;
								Output.Write("<div id=\"searchterm" + term_counter + "\" class=\"sbkPrsw_SearchTerm\">");


                                // Special code for MIMETYPE of NOT NONE
							    string write_value;
                                if ((String.Compare(terms[i], "NONE", StringComparison.OrdinalIgnoreCase) == 0) && (String.Compare(fields[i], "-MI", StringComparison.OrdinalIgnoreCase) == 0))
							    {
							        write_value = "items with files ";
							        Output.Write("items with files ");
							    }
							    else
							    {
							        // Add the term
							        if (terms[i].Contains(" "))
							        {
                                        Output.Write("\"" + HttpUtility.HtmlEncode(terms[i].Replace("''''", "'").Replace("''", "'")).Replace("%2c", ",").Replace("+", " ") + "\" ");
							        }
							        else
							        {
                                        Output.Write("'" + HttpUtility.HtmlEncode(terms[i].Replace("''''", "'").Replace("''", "'")).Replace("%2c", ",").Replace("+", " ") + "' ");
							        }

							        // Does the field start with a negative?
							        if (fields[i][0] == '-')
							        {
							            Output.Write(and_not_language);
							            fields[i] = fields[i].Substring(1);
							        }

							        write_value = Search_Label_from_Sobek_Code(fields[i]).ToLower() + " ";
							        Output.Write(write_value);
							    }

							    // Determine URL of this search without this one term
								if (terms.Count > 1)
								{
									termsBuilder.Clear();
									fieldsBuilder.Clear();

									// Add all fields, EXCEPT the one to be skipped
									for (int j = 0; j < Math.Min(terms.Count, fields.Count); j++)
									{
										if (j != i)
										{
											if (termsBuilder.Length > 0)
												termsBuilder.Append(",");
                                            if ( terms[j].IndexOf(" ") > 0 )
    											termsBuilder.Append("\"" + terms[j] + "\"");
                                            else
                                                termsBuilder.Append(terms[j]);

											if (fieldsBuilder.Length > 0)
												fieldsBuilder.Append(",");
											fieldsBuilder.Append(fields[j]);
										}
									}
									RequestSpecificValues.Current_Mode.Search_String = termsBuilder.ToString();
									RequestSpecificValues.Current_Mode.Search_Fields = fieldsBuilder.ToString();
								}
								else
								{
									if (hierarchyObject.Views_And_Searches.Contains(Item_Aggregation_Views_Searches_Enum.All_New_Items))
									{
										RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
										RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Browse_Info;
										RequestSpecificValues.Current_Mode.Info_Browse_Mode = "all";
									}
									else
									{
										RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
										RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
									}
								}


								Output.WriteLine("<a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\" title=\"Click to remove this search term\"><img src=\"" + Static_Resources_Gateway.Removeicon_Gif + "\" id=\"removesearchterm" + term_counter + "\" class=\"sbkPrsw_RemoveSearchTerm\" /></a></div>");
							}
						}

						RequestSpecificValues.Current_Mode.Search_String = current_search_string;
						RequestSpecificValues.Current_Mode.Search_Fields = current_search_field;
						RequestSpecificValues.Current_Mode.Mode = current_display_mode;
						RequestSpecificValues.Current_Mode.Aggregation_Type = current_aggr_mode;
						RequestSpecificValues.Current_Mode.Info_Browse_Mode = current_info_browse_mode;
					}
					else
					{

						for (int i = 0; (i < terms.Count) && (i < fields.Count); i++)
						{
							if ((terms[i].Length > 0) && (fields[i].Length > 0))
							{
								// Remove the leading + sign
								if (fields[i][0] == '+')
									fields[i] = fields[i].Substring(1);
								if (fields[i][0] == ' ')
									fields[i] = fields[i].Substring(1);

								// Add the 'AND' value
								if (i > 0)
								{
									if (fields[i][0] == '=')
									{
										Output.Write(or_language);
									    fields[i] = fields[i].Substring(1);
									}
									else
									{
										Output.Write(and_language);
									}
								}

								//// This explanataion need to be capped
								//if (length_of_explanation >= 160)
								//{
								//	Output.Write("... ");
								//	break;
								//}

								// Add the term
								if (terms[i].Contains(" "))
								{
									Output.Write("\"" + HttpUtility.UrlEncode(terms[i].Replace("''''", "'").Replace("''", "'")) + "\" ");
								}
								else
								{
									Output.Write("'" + HttpUtility.UrlEncode(terms[i].Replace("''''", "'").Replace("''", "'")) + "' ");
								}

								// Does the field start with a negative?
								if (fields[i][0] == '-')
								{
									Output.Write(and_not_language);
								    fields[i] = fields[i].Substring(1);
								}

								string write_value = Search_Label_from_Sobek_Code(fields[i]).ToLower() + " ";
								Output.Write(write_value);
							}
						}

					}
				}
				catch
				{
					Output.Write("UNRECOGNIZED SEARCH ");
				}
			}

			// Add the year date range text here as well
			if (RequestSpecificValues.Current_Mode.DateRange_Year1 >= 0)
			{
				if (RequestSpecificValues.Current_Mode.DateRange_Year2 >= 0)
				{
					Output.Write(between_two_dates, RequestSpecificValues.Current_Mode.DateRange_Year1, RequestSpecificValues.Current_Mode.DateRange_Year2 );
				}
				else
				{
					Output.Write(on_one_date, RequestSpecificValues.Current_Mode.DateRange_Year1);
				}
			}

            if (!IncludeResultCount)
                return;

            if ((resultsStatistics == null) || (resultsStatistics.Total_Titles == 0))
			{
				Output.WriteLine(no_matches_language );
			}
			else
			{
				if (resultsStatistics.Total_Titles == resultsStatistics.Total_Items)
				{
					Output.WriteLine(resultsStatistics.Total_Titles == 1 ? one_match_language : String.Format(multiple_records_language, resultsStatistics.Total_Titles));
				}
				else
				{
					Output.Write(resultsStatistics.Total_Items == 1 ? one_item_language : String.Format(multiple_items_language, resultsStatistics.Total_Items.ToString()));

					if (resultsStatistics.Total_Titles == 1)
					{
						Output.WriteLine(one_title_language);
					}
					else
					{
						Output.WriteLine(resultsStatistics.Total_Titles + multiple_titles_language);
					}
				}
			}
		}

		private string Search_Label_from_Sobek_Code(string Code)
		{
			string in_language = "in ";
			if (RequestSpecificValues.Current_Mode.Language == Web_Language_Enum.French)
			{
				in_language = "en ";
			}
			if (RequestSpecificValues.Current_Mode.Language == Web_Language_Enum.Spanish)
			{
				in_language = "en ";
			}

			if (Code == "ZZ")
				return UI_ApplicationCache_Gateway.Translation.Get_Translation("anywhere", RequestSpecificValues.Current_Mode.Language);

			Metadata_Search_Field field = UI_ApplicationCache_Gateway.Settings.Metadata_Search_Field_By_Code(Code);
			return (field != null) ? in_language + UI_ApplicationCache_Gateway.Translation.Get_Translation(field.Display_Term, RequestSpecificValues.Current_Mode.Language) : in_language + HttpUtility.UrlEncode(Code);
		}

		#region Methods to create the facets on the left side of the results

	    /// <summary> Returns the facets for this result/browse as HTML to be added into the form </summary>
	    /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
	    /// <returns> Formatted facet information in HTML table format </returns>
	    protected string Add_Facet_Information(Custom_Tracer Tracer)
	    {

	        StringBuilder builder = new StringBuilder();


	        string collection = UI_ApplicationCache_Gateway.Translation.Get_Translation("Collection", RequestSpecificValues.Current_Mode.Language);
	        string show_more = UI_ApplicationCache_Gateway.Translation.Get_Translation("Show More", RequestSpecificValues.Current_Mode.Language);
	        string show_less = UI_ApplicationCache_Gateway.Translation.Get_Translation("Show Less", RequestSpecificValues.Current_Mode.Language);
	        string sort_by_frequency = UI_ApplicationCache_Gateway.Translation.Get_Translation("Sort these facets by frequency", RequestSpecificValues.Current_Mode.Language);
	        string sort_alphabetically = UI_ApplicationCache_Gateway.Translation.Get_Translation("Sort these facets alphabetically", RequestSpecificValues.Current_Mode.Language);

	        builder.AppendLine("<input type=\"hidden\" id=\"facet\" name=\"facet\" value=\"" + HttpUtility.HtmlEncode(facetInformation) + "\" />");

	        builder.AppendLine("<script type=\"text/javascript\">");
	        builder.AppendLine("  //<![CDATA[");
	        builder.AppendLine("    function add_facet(code, new_value) {");

	        string url = String.Empty;
	        string aggregation_url = String.Empty;

	        if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation) // browse info only
	        {
	            Display_Mode_Enum displayMode = RequestSpecificValues.Current_Mode.Mode;
	            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Results;
	            RequestSpecificValues.Current_Mode.Search_Type = Search_Type_Enum.Advanced;
	            RequestSpecificValues.Current_Mode.Search_Fields = "<%CODE%>";
	            RequestSpecificValues.Current_Mode.Search_String = "<%VALUE%>";
	            ushort? page = RequestSpecificValues.Current_Mode.Page;
	            RequestSpecificValues.Current_Mode.Page = 1;
	            url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("%3c%25", "<%").Replace("%25%3e", "%>").Replace("<%VALUE%>", "\"<%VALUE%>\"").Replace("/exact/", "/results/");

	            RequestSpecificValues.Current_Mode.Mode = displayMode;
	            RequestSpecificValues.Current_Mode.Page = page;
	            RequestSpecificValues.Current_Mode.Search_Fields = String.Empty;
	            RequestSpecificValues.Current_Mode.Search_String = String.Empty;

	            if ((RequestSpecificValues.Current_Mode.Aggregation.Length == 0) || (RequestSpecificValues.Current_Mode.Aggregation == "all"))
	            {
	                RequestSpecificValues.Current_Mode.Aggregation = "<%AGGREGATION%>";
	                aggregation_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
	                RequestSpecificValues.Current_Mode.Aggregation = String.Empty;
	            }
	        }
	        else
	        {
	            if ((RequestSpecificValues.Current_Mode.Aggregation.Length == 0) || (RequestSpecificValues.Current_Mode.Aggregation == "all"))
	            {
	                RequestSpecificValues.Current_Mode.Aggregation = "<%AGGREGATION%>";
	                aggregation_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
	                RequestSpecificValues.Current_Mode.Aggregation = String.Empty;
	            }

	            if (RequestSpecificValues.Current_Mode.Search_Type == Search_Type_Enum.Advanced)
	            {
	                string orig_field = RequestSpecificValues.Current_Mode.Search_Fields;
	                string orig_terms = RequestSpecificValues.Current_Mode.Search_String;
	                RequestSpecificValues.Current_Mode.Search_Fields = RequestSpecificValues.Current_Mode.Search_Fields + ",<%CODE%>";
	                RequestSpecificValues.Current_Mode.Search_String = RequestSpecificValues.Current_Mode.Search_String + ",<%VALUE%>";
	                ushort? page = RequestSpecificValues.Current_Mode.Page;
	                RequestSpecificValues.Current_Mode.Page = 1;
	                url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("%3c%25", "<%").Replace("%25%3e", "%>").Replace("<%VALUE%>", "\"<%VALUE%>\"").Replace("/exact/", "/results/");
	                RequestSpecificValues.Current_Mode.Page = page;
	                RequestSpecificValues.Current_Mode.Search_Fields = orig_field;
	                RequestSpecificValues.Current_Mode.Search_String = orig_terms;
	            }
	            if (RequestSpecificValues.Current_Mode.Search_Type == Search_Type_Enum.Basic)
	            {
	                List<string> output_terms = new List<string>();
	                List<string> output_fields = new List<string>();
	                SobekCM_Assistant.Split_Clean_Search_Terms_Fields(RequestSpecificValues.Current_Mode.Search_String, RequestSpecificValues.Current_Mode.Search_Fields, RequestSpecificValues.Current_Mode.Search_Type, output_terms, output_fields, UI_ApplicationCache_Gateway.Search_Stop_Words, RequestSpecificValues.Current_Mode.Search_Precision, ',');

	                string original_search = RequestSpecificValues.Current_Mode.Search_String;
	                RequestSpecificValues.Current_Mode.Search_Type = Search_Type_Enum.Advanced;
	                StringBuilder term_builder = new StringBuilder();
	                foreach (string thisTerm in output_terms)
	                {
	                    if (term_builder.Length > 0)
	                        term_builder.Append(",");
	                    if (thisTerm.IndexOf(" ") > 0)
	                        term_builder.Append("\"" + thisTerm + "\"");
	                    else
	                        term_builder.Append(thisTerm);
	                }
	                StringBuilder field_builder = new StringBuilder();
	                foreach (string thisField in output_fields)
	                {
	                    if (field_builder.Length > 0)
	                        field_builder.Append(",");
	                    field_builder.Append(thisField);
	                }
	                RequestSpecificValues.Current_Mode.Search_Fields = field_builder.ToString();
	                RequestSpecificValues.Current_Mode.Search_String = term_builder.ToString();
	                ushort? page = RequestSpecificValues.Current_Mode.Page;
                    RequestSpecificValues.Current_Mode.Page = 1;

	                RequestSpecificValues.Current_Mode.Search_Fields = RequestSpecificValues.Current_Mode.Search_Fields + ",<%CODE%>";
	                RequestSpecificValues.Current_Mode.Search_String = RequestSpecificValues.Current_Mode.Search_String + ",<%VALUE%>";
	                url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("%3c%25", "<%").Replace("%25%3e", "%>").Replace("<%VALUE%>", "\"<%VALUE%>\"");

	                RequestSpecificValues.Current_Mode.Search_Type = Search_Type_Enum.Basic;
	                RequestSpecificValues.Current_Mode.Search_Fields = String.Empty;
	                RequestSpecificValues.Current_Mode.Search_String = original_search;
	                RequestSpecificValues.Current_Mode.Page = page;
	            }
	        }
	        builder.AppendLine("      var stem_url = '" + url + "';");
	        builder.AppendLine("      var new_url = stem_url.replace('<%CODE%>', code).replace('<%VALUE%>', new_value);");
	        builder.AppendLine("      window.location.href = new_url;");
	        builder.AppendLine("      return false;");
	        builder.AppendLine("    }");
	        builder.AppendLine("  //]]>");
	        builder.AppendLine("</script>");
	        builder.AppendLine();

	        builder.AppendLine("<nav class=\"sbkPrsw_FacetColumn\" role=\"complementary\" aria-label=\"Facets\">");
	        builder.AppendLine("<div class=\"sbkPrsw_FacetColumnTitle\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Narrow Results By", RequestSpecificValues.Current_Mode.Language) + ":</div>");


	        // Add the aggregation information first
	        if (((RequestSpecificValues.Current_Mode.Aggregation.Length == 0) || (RequestSpecificValues.Current_Mode.Aggregation == "all")) && (resultsStatistics.Aggregation_Facets_Count > 0))
	        {
	            string title = collection;
	            const int FACET_INDEX = 0;
	            int facet_count = 0;
	            int total_facets_to_show = MINIMIZED_FACET_COUNT;
	            char other_sort_type = '2';
	            char other_show_type = '1';
	            if ((facetInformation[FACET_INDEX] == '1') || (facetInformation[FACET_INDEX] == '3'))
	            {
	                total_facets_to_show = MAXIMIZED_FACET_COUNT;
	            }

	            string resort_image = "2_to_1.gif";
	            string sort_instructions = sort_by_frequency;
	            switch (facetInformation[FACET_INDEX])
	            {
	                case '0':
	                    other_sort_type = '2';
	                    other_show_type = '1';
	                    sort_instructions = sort_alphabetically;
	                    break;

	                case '1':
	                    other_sort_type = '3';
	                    other_show_type = '0';
	                    sort_instructions = sort_alphabetically;
	                    break;

	                case '2':
	                    other_sort_type = '0';
	                    other_show_type = '3';
	                    resort_image = "a_to_z.gif";
	                    break;

	                case '3':
	                    other_sort_type = '1';
	                    other_show_type = '2';
	                    resort_image = "a_to_z.gif";
	                    break;
	            }

	            builder.AppendLine("<div class=\"sbkPrsw_FacetBoxTitle\">" + title + "</div>");
	            builder.AppendLine("<div class=\"sbkPrsw_FacetBox\">");
	            if (resultsStatistics.Aggregation_Facets.Count > 1)
	                builder.AppendLine("<div class=\"sbkPrsw_FacetReorder\"><a href=\"\" onclick=\"return set_facet(" + FACET_INDEX + ",'" + other_sort_type + "');\" title=\"" + sort_instructions + "\"><img src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.Current_Mode.Base_Skin_Or_Skin + "/buttons/" + resort_image + "\" alt=\"Resort " + title + "\" /></a></div>");
	            if ((facetInformation[FACET_INDEX] == '2') || (facetInformation[FACET_INDEX] == '3'))
	            {
	                SortedList<string, string> order_facets = new SortedList<string, string>();
	                while ((facet_count < total_facets_to_show) && (facet_count < resultsStatistics.Aggregation_Facets.Count))
	                {
	                    if (resultsStatistics.Aggregation_Facets[facet_count].Code.ToLower() != "iuf")
	                    {
	                        order_facets[resultsStatistics.Aggregation_Facets[facet_count].Facet.ToUpper()] = "<a href=\"" + aggregation_url.Replace("<%AGGREGATION%>", resultsStatistics.Aggregation_Facets[facet_count].Code.ToLower()) + "\">" + resultsStatistics.Aggregation_Facets[facet_count].Facet + "</a> ( " + resultsStatistics.Aggregation_Facets[facet_count].Frequency + " ) <br />";
	                    }
	                    facet_count++;
	                }
	                foreach (string html in order_facets.Values)
	                {
	                    builder.AppendLine(html);
	                }
	            }
	            else
	            {
	                while ((facet_count < total_facets_to_show) && (facet_count < resultsStatistics.Aggregation_Facets.Count))
	                {
	                    if (resultsStatistics.Aggregation_Facets[facet_count].Code.ToLower() != "iuf")
	                    {
	                        builder.AppendLine("<a href=\"" + aggregation_url.Replace("<%AGGREGATION%>", resultsStatistics.Aggregation_Facets[facet_count].Code.ToLower()) + "\">" + resultsStatistics.Aggregation_Facets[facet_count].Facet + "</a> ( " + resultsStatistics.Aggregation_Facets[facet_count].Frequency + " ) <br />");
	                    }
	                    facet_count++;
	                }
	            }
	            if (facet_count > MINIMIZED_FACET_COUNT)
	            {
	                builder.AppendLine("<div class=\"sbkPrsw_ShowHideFacets\"><a href=\"\" onclick=\"return set_facet(" + FACET_INDEX + ",'" + other_show_type + "');\">&lt;&lt; " + show_less + " &nbsp; &nbsp;</a></div>");
	            }
	            else
	            {
	                if (facet_count < resultsStatistics.Aggregation_Facets.Count)
	                {
	                    builder.AppendLine("<div class=\"sbkPrsw_ShowHideFacets\"><a href=\"\" onclick=\"return set_facet(" + FACET_INDEX + ",'" + other_show_type + "');\">" + show_more + " &gt;&gt; &nbsp;</a></div>");
	                }
	            }
	            builder.AppendLine("</div>");
	        }

	        // Add the facet information 
	        if ((resultsStatistics.Facet_Collections != null) && (resultsStatistics.Facet_Collections.Count > 0))
	        {
	            int facetIndex = 1;
	            foreach (Search_Facet_Collection theseFacets in resultsStatistics.Facet_Collections)
	            {
	                if ((theseFacets.MetadataTypeID > 0) && (theseFacets.Count > 0))
	                {
	                    Metadata_Search_Field field = UI_ApplicationCache_Gateway.Settings.Metadata_Search_Field_By_ID(theseFacets.MetadataTypeID);
	                    if (field != null)
	                    {
	                        Add_Single_Facet(builder, UI_ApplicationCache_Gateway.Translation.Get_Translation(field.Facet_Term, RequestSpecificValues.Current_Mode.Language), field.Web_Code, show_less, show_more, facetIndex, sort_by_frequency, sort_alphabetically, theseFacets.Facets);
	                    }
	                }

	                facetIndex++;
	            }
	        }

	        builder.AppendLine("</nav>");

	        return builder.ToString();

	    }

	    private void Add_Single_Facet(StringBuilder Builder, string Title, string SearchCode, string ShowLess, string ShowMore, int FacetIndex, string SortByFrequency, string SortAlphabetically, List<Search_Facet> Collection)
		{
			int facet_count = 0;
			int total_facets_to_show = MINIMIZED_FACET_COUNT;
			char other_sort_type = '2';
			char other_show_type = '1';
			if ((facetInformation[FacetIndex - 1] == '1') || ( facetInformation[FacetIndex - 1 ] == '3' ))
			{
				total_facets_to_show = MAXIMIZED_FACET_COUNT;
			}

			string resort_image = "2_to_1.gif";
			string sort_instructions = SortByFrequency;
			switch ( facetInformation[FacetIndex - 1])
			{
				case '0':
					other_sort_type = '2';
					other_show_type = '1';
					sort_instructions = SortAlphabetically;
					break;

				case '1':
					other_sort_type = '3';
					other_show_type = '0';
					sort_instructions = SortAlphabetically;
					break;

				case '2':
					other_sort_type = '0';
					other_show_type = '3';
					resort_image = "a_to_z.gif";
					break;

				case '3':
					other_sort_type = '1';
					other_show_type = '2';
					resort_image = "a_to_z.gif";
					break;
			}

			Builder.AppendLine("<div class=\"sbkPrsw_FacetBoxTitle\">" + Title + "</div>");
			Builder.AppendLine("<div class=\"sbkPrsw_FacetBox\">");
			if (Collection.Count > 1)
			{
				Builder.AppendLine("<div class=\"sbkPrsw_FacetReorder\"><a href=\"\" onclick=\"return set_facet(" + (FacetIndex - 1) + ",'" + other_sort_type + "');\" title=\"" + sort_instructions + "\"><img src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.Current_Mode.Base_Skin_Or_Skin + "/buttons/" + resort_image + "\" alt=\"Resort " + Title + "\" /></a></div>");
			}
			if ((facetInformation[FacetIndex - 1] == '2') || (facetInformation[FacetIndex - 1] == '3'))
			{
				SortedList<string, string> order_facets = new SortedList<string, string>();
				while ((facet_count < total_facets_to_show) && (facet_count < Collection.Count))
				{
                    order_facets[Collection[facet_count].Facet.ToUpper()] = "<a href=\"\" onclick=\"return add_facet('" + SearchCode + "','" + HttpUtility.HtmlEncode(Collection[facet_count].Facet.Replace("&", "")).Replace("'", "\\'").Replace(",", "").Replace("&", "") + "');\">" + Collection[facet_count].Facet.Replace("&", "&amp;").Replace("&amp;amp;", "&amp;") + "</a> ( " + Collection[facet_count].Frequency + " ) <br />";
					facet_count++;
				}
				foreach (string html in order_facets.Values)
				{
					Builder.AppendLine(html);
				}
			}
			else
			{
				while ((facet_count < total_facets_to_show) && (facet_count < Collection.Count))
				{
					Builder.AppendLine("<a href=\"\" onclick=\"return add_facet('" + SearchCode + "','" + HttpUtility.HtmlEncode(Collection[facet_count].Facet.Replace("&", "")).Replace("'", "\\'").Replace(",", "").Replace("&", "") + "');\">" + Collection[facet_count].Facet.Replace("&", "&amp;" ).Replace("&amp;amp;","&amp;") + "</a> ( " + Collection[facet_count].Frequency + " ) <br />");
					facet_count++;
				}
			}
			if (facet_count > MINIMIZED_FACET_COUNT)
			{
				Builder.AppendLine("<div class=\"sbkPrsw_ShowHideFacets\"><a href=\"\" onclick=\"return set_facet(" + (FacetIndex - 1) + ",'" + other_show_type + "');\">&lt;&lt; " + ShowLess + " &nbsp; &nbsp;</a></div>");
			}
			else
			{
				if (facet_count < Collection.Count)
				{
					Builder.AppendLine("<div class=\"sbkPrsw_ShowHideFacets\"><a href=\"\" onclick=\"return set_facet(" + (FacetIndex - 1) + ",'" + other_show_type + "');\">" + ShowMore + " &gt;&gt; &nbsp;</a></div>");
				}
			}
			Builder.AppendLine("</div>");
		}

        private void Add_Single_Facet_Callback(StringBuilder Builder, string Title, string SearchCode, string ShowLess, string ShowMore, int FacetIndex, string SortByFrequency, string SortAlphabetically, List<Search_Facet> Collection)
        {
            int facet_count = 0;
            int total_facets_to_show = MINIMIZED_FACET_COUNT;
            char other_sort_type = '2';
            char other_show_type = '1';
            if ((facetInformation[FacetIndex - 1] == '1') || (facetInformation[FacetIndex - 1] == '3'))
            {
                total_facets_to_show = MAXIMIZED_FACET_COUNT;
            }

            string resort_image = "2_to_1.gif";
            string sort_instructions = SortByFrequency;
            switch (facetInformation[FacetIndex - 1])
            {
                case '0':
                    other_sort_type = '2';
                    other_show_type = '1';
                    sort_instructions = SortAlphabetically;
                    break;

                case '1':
                    other_sort_type = '3';
                    other_show_type = '0';
                    sort_instructions = SortAlphabetically;
                    break;

                case '2':
                    other_sort_type = '0';
                    other_show_type = '3';
                    resort_image = "a_to_z.gif";
                    break;

                case '3':
                    other_sort_type = '1';
                    other_show_type = '2';
                    resort_image = "a_to_z.gif";
                    break;
            }

            Builder.AppendLine("<div class=\"sbkPrsw_FacetBoxTitle\">" + Title + "</div>");
            Builder.AppendLine("<div class=\"sbkPrsw_FacetBox\"><ul>");
            if (Collection.Count > 1)
            {
                Builder.AppendLine("<div class=\"sbkPrsw_FacetReorder\"><a onclick=\"set_facet_callback(" + (FacetIndex - 1) + ",'" + other_sort_type + "');\" title=\"" + sort_instructions + "\"><img src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.Current_Mode.Base_Skin_Or_Skin + "/buttons/" + resort_image + "\" alt=\"RESORT\" /></a></div>");
            }
            if ((facetInformation[FacetIndex - 1] == '2') || (facetInformation[FacetIndex - 1] == '3'))
            {
                SortedList<string, string> order_facets = new SortedList<string, string>();
                while ((facet_count < total_facets_to_show) && (facet_count < Collection.Count))
                {
                    order_facets[Collection[facet_count].Facet.ToUpper()] = "<li><a onclick=\"add_facet_callback('" + SearchCode + "','" + HttpUtility.HtmlEncode(Collection[facet_count].Facet.Replace("&", "")).Replace("'", "\\'").Replace(",", "").Replace("&", "") + "');\">" + Collection[facet_count].Facet.Replace("&", "&amp;") + "</a> ( " + Collection[facet_count].Frequency + " ) </li>";
                    facet_count++;
                }
                foreach (string html in order_facets.Values)
                {
                    Builder.AppendLine(html);
                }
            }
            else
            {
                while ((facet_count < total_facets_to_show) && (facet_count < Collection.Count))
                {
                    Builder.AppendLine("<li><a onclick=\"add_facet_callback('" + SearchCode + "','" + HttpUtility.HtmlEncode(Collection[facet_count].Facet.Replace("&", "")).Replace("'", "\\'").Replace(",", "").Replace("&", "") + "');\">" + Collection[facet_count].Facet.Replace("&", "&amp;") + "</a> ( " + Collection[facet_count].Frequency + " )</li>");
                    facet_count++;
                }
            }
            if (facet_count > MINIMIZED_FACET_COUNT)
            {
                Builder.AppendLine("<div class=\"sbkPrsw_ShowHideFacets loadmoreResults\"><a onclick=\"set_facet_callback(" + (FacetIndex - 1) + ",'" + other_show_type + "');\">&lt;&lt; " + ShowLess + " &nbsp; &nbsp;</a></div>");
            }
            else
            {
                if (facet_count < Collection.Count)
                {
                    Builder.AppendLine("<div class=\"sbkPrsw_ShowHideFacets loadmoreResults\"><a onclick=\"set_facet_callback(" + (FacetIndex - 1) + ",'" + other_show_type + "');\">" + ShowMore + " &gt;&gt; &nbsp;</a></div>");
                }
            }
            Builder.AppendLine("</ul></div>");
        }
		
        #endregion
	}
}
