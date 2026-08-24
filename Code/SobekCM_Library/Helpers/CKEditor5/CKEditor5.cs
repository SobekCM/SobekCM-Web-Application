#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Helpers.CKEditor;
using SobekCM.Library.UI;
using System;
using System.IO;
using System.Text;

#endregion

namespace SobekCM.Library.Helpers.CKEditor5
{
    /// <summary> Class is used to write the HTML to allow users to edit web (HTML) content using
    /// CKEditor 5, for side-by-side comparison against the existing CKEditor 4 helper </summary>
    /// <remarks> This is an evaluation build, not yet wired for production use.  Known gaps versus
    /// the CKEditor 4 helper: no non-English UI language support yet.  The custom 'slideshow' plugin
    /// was not ported, since it is believed to be unused.  CKEditor 5's free source-editing view has
    /// no syntax coloring of its own (that is a paid 'Enhanced Source Code Editing' add-on) - CodeMirror
    /// is wired on top of its plain textarea here, the same trick the CKEditor 4 helper uses via its
    /// own 'codemirror' plugin.  Image upload reuses the same HtmlEditFileHandler.ashx endpoint and
    /// CKEditor_Security_Token as the CKEditor 4 helper - see Program.cs's HtmlEdit_Upload_Handler,
    /// which branches its response format depending on which editor is asking. </remarks>
    public class CKEditor5
    {
        /// <summary> Constructor for a new instance of the CKEditor5 class </summary>
        public CKEditor5()
        {
            Start_In_Source_Mode = false;
        }

        /// <summary> Add the necessary style, import map, and module script sections, with
        /// all the options specified here, directly to the streamwriter </summary>
        /// <param name="Output"> Writer to write to the stream </param>
        /// <param name="Include_Script_Reference"> Flag indicates if the CKEditor 5 stylesheet, import
        /// map, and CodeMirror assets should be added to the output stream here.  An import map can only
        /// appear once per page, so when multiple CKEditor5 instances share a page, only the first
        /// should pass TRUE </param>
        public void Add_To_Stream(TextWriter Output, bool Include_Script_Reference)
        {
            // All these resolve through the static resources gateway (CDN/local per the active config),
            // same as every other vendored library in the app - see StaticResources_Configuration.cs
            string ckJs = Static_Resources_Gateway.Ckeditor5_Js;
            string ckRoot = ckJs.Substring(0, ckJs.LastIndexOf('/') + 1);
            string cmRoot = Static_Resources_Gateway.Codemirror_Prefix;
            string beautifyJs = Static_Resources_Gateway.Jsbeautify_Js;
            string beautifyRoot = beautifyJs.Substring(0, beautifyJs.LastIndexOf('/') + 1);

            if (Include_Script_Reference)
            {
                Output.WriteLine("  <link rel=\"stylesheet\" href=\"" + Static_Resources_Gateway.Ckeditor5_Css + "\" />");
                Output.WriteLine("  <link rel=\"stylesheet\" href=\"" + Static_Resources_Gateway.Ckeditor5_Content_Css + "\" />");
                Output.WriteLine("  <script type=\"importmap\">");
                Output.WriteLine("  {");
                Output.WriteLine("    \"imports\": {");
                Output.WriteLine("      \"ckeditor5\": \"" + ckJs + "\",");
                Output.WriteLine("      \"ckeditor5/\": \"" + ckRoot + "\"");
                Output.WriteLine("    }");
                Output.WriteLine("  }");
                Output.WriteLine("  </script>");

                // CodeMirror provides the syntax coloring the free CKEditor 5 source-editing view lacks
                Output.WriteLine("  <link rel=\"stylesheet\" href=\"" + Static_Resources_Gateway.Codemirror_Css + "\" />");
                Output.WriteLine("  <style>.ck-source-editing-area .CodeMirror { height: 500px; border: 1px solid #ccced1; }</style>");
                Output.WriteLine("  <script src=\"" + Static_Resources_Gateway.Codemirror_Js + "\"></script>");
                Output.WriteLine("  <script src=\"" + cmRoot + "mode/xml/xml.js\"></script>");
                Output.WriteLine("  <script src=\"" + cmRoot + "mode/javascript/javascript.js\"></script>");
                Output.WriteLine("  <script src=\"" + cmRoot + "mode/css/css.js\"></script>");
                Output.WriteLine("  <script src=\"" + cmRoot + "mode/htmlmixed/htmlmixed.js\"></script>");

                // js-beautify reformats the compact HTML CKEditor 5's getData() produces (one long line,
                // no indentation) into readable, indented markup before it's shown in the colored source
                // view - the same beautify-on-source-view behavior the CKEditor 4 helper's own vendored
                // 'codemirror' plugin provides via its own bundled copy of this same library.
                Output.WriteLine("  <script src=\"" + beautifyJs + "\"></script>");
                Output.WriteLine("  <script src=\"" + Static_Resources_Gateway.Jsbeautify_Css_Js + "\"></script>");
                Output.WriteLine("  <script src=\"" + Static_Resources_Gateway.Jsbeautify_Html_Js + "\"></script>");
            }

            string js_editor_id = TextAreaID.ToLower() + "_ck5";

            // Wire up image upload, if a path/URL was configured - reuses the same security token and
            // server endpoint as the CKEditor 4 helper (see Program.cs's HtmlEdit_Upload_Handler).  This
            // one endpoint lives on the app server itself, not the CDN, so it stays BaseUrl-relative.
            bool uploadConfigured = (!String.IsNullOrEmpty(UploadPath)) && (!String.IsNullOrEmpty(UploadURL)) && (Context != null);
            string uploadUrl = String.Empty;
            if (uploadConfigured)
            {
                var newToken = new CKEditor_Security_Token(UploadPath, UploadURL);
                string token = newToken.ThisGuid.ToString();
                Context.SessionObject()["#CKEDITOR::" + token] = newToken;
                string baseUrl = String.IsNullOrEmpty(BaseUrl) ? "/" : BaseUrl;
                uploadUrl = baseUrl + "HtmlEditFileHandler.ashx?token=" + token;
            }

            Output.WriteLine("  <script type=\"module\">");
            Output.WriteLine("    import {");
            Output.WriteLine("      ClassicEditor, Essentials, Paragraph, Heading,");
            Output.WriteLine("      Bold, Italic, Underline, Strikethrough, RemoveFormat,");
            Output.WriteLine("      Link, List, BlockQuote, HorizontalLine, Alignment, Indent, IndentBlock,");
            Output.WriteLine("      Table, TableToolbar, TableColumnResize,");
            Output.WriteLine("      Image, ImageToolbar, ImageCaption, ImageStyle, ImageResize, ImageUpload, SimpleUploadAdapter,");
            Output.WriteLine("      SourceEditing, PasteFromOffice, SpecialCharacters, SpecialCharactersEssentials,");
            Output.WriteLine("      GeneralHtmlSupport, AccessibilityHelp, Autoformat");
            Output.WriteLine("    } from 'ckeditor5';");
            Output.WriteLine();
            Output.WriteLine("    ClassicEditor.create(document.querySelector('#" + TextAreaID + "'), {");
            Output.WriteLine("      licenseKey: 'GPL',");
            Output.WriteLine("      plugins: [");
            Output.WriteLine("        Essentials, Paragraph, Heading,");
            Output.WriteLine("        Bold, Italic, Underline, Strikethrough, RemoveFormat,");
            Output.WriteLine("        Link, List, BlockQuote, HorizontalLine, Alignment, Indent, IndentBlock,");
            Output.WriteLine("        Table, TableToolbar, TableColumnResize,");
            Output.WriteLine("        Image, ImageToolbar, ImageCaption, ImageStyle, ImageResize, ImageUpload, SimpleUploadAdapter,");
            Output.WriteLine("        SourceEditing, PasteFromOffice, SpecialCharacters, SpecialCharactersEssentials,");
            Output.WriteLine("        GeneralHtmlSupport, AccessibilityHelp, Autoformat");
            Output.WriteLine("      ],");
            if (uploadConfigured)
            {
                Output.WriteLine("      simpleUpload: {");
                Output.WriteLine("        uploadUrl: '" + uploadUrl + "'");
                Output.WriteLine("      },");
            }
            Output.WriteLine("      toolbar: [");
            Output.WriteLine("        'undo', 'redo', '|',");
            Output.WriteLine("        'heading', '|',");
            Output.WriteLine("        'bold', 'italic', 'underline', 'strikethrough', 'removeFormat', '|',");
            Output.WriteLine("        'link', 'blockQuote', 'insertTable', 'horizontalLine', '|',");
            Output.WriteLine("        'bulletedList', 'numberedList', 'outdent', 'indent', 'alignment', '|',");
            Output.WriteLine("        'insertImage', 'specialCharacters', '|',");
            Output.WriteLine("        'sourceEditing', 'accessibilityHelp'");
            Output.WriteLine("      ],");
            Output.WriteLine("      image: {");
            Output.WriteLine("        toolbar: [ 'imageStyle:inline', 'imageStyle:block', 'imageStyle:side', '|', 'toggleImageCaption', 'imageTextAlternative', 'resizeImage' ]");
            Output.WriteLine("      },");
            Output.WriteLine("      table: {");
            Output.WriteLine("        contentToolbar: [ 'tableColumn', 'tableRow', 'mergeTableCells' ]");
            Output.WriteLine("      },");
            Output.WriteLine("      htmlSupport: {");
            Output.WriteLine("        allow: [ { name: /.*/, attributes: true, classes: true, styles: true } ]");
            Output.WriteLine("      }");
            Output.WriteLine("    })");
            Output.WriteLine("    .then(editor => {");
            Output.WriteLine("      window." + js_editor_id + " = editor;");
            Output.WriteLine();
            Output.WriteLine("      // ckeditor5-content.css hard-codes its own baseline typography (font, size,");
            Output.WriteLine("      // line-height, color) as :root custom properties, which otherwise override the");
            Output.WriteLine("      // page's own styles inside the editable area.  Read what the surrounding page");
            Output.WriteLine("      // actually uses and set it as an inline style on the editable element itself -");
            Output.WriteLine("      // inline styles beat the :root-level rule regardless of load order, and this");
            Output.WriteLine("      // adapts to whatever skin/CSS is in effect rather than a hard-coded guess.");
            Output.WriteLine("      const editableElement = editor.ui.view.editable.element;");
            Output.WriteLine("      const ambientContainer = editableElement.closest('.ck-editor').parentElement;");
            Output.WriteLine("      const applyFontFix = () => {");
            Output.WriteLine("        const pageFont = getComputedStyle(ambientContainer);");
            Output.WriteLine("        editableElement.style.setProperty('--ck-content-font-family', pageFont.fontFamily);");
            Output.WriteLine("        editableElement.style.setProperty('--ck-content-font-size', pageFont.fontSize);");
            Output.WriteLine("        editableElement.style.setProperty('--ck-content-line-height', pageFont.lineHeight);");
            Output.WriteLine("        editableElement.style.setProperty('--ck-content-font-color', pageFont.color);");
            Output.WriteLine("      };");
            Output.WriteLine("      applyFontFix();");
            Output.WriteLine();
            Output.WriteLine("      // CKEditor 5 rewrites this element's own inline style attribute as part of its");
            Output.WriteLine("      // normal operation (e.g. on focus), clobbering the fix above.  Watch for that");
            Output.WriteLine("      // and reapply immediately rather than chasing the specific event that causes it.");
            Output.WriteLine("      new MutationObserver(() => {");
            Output.WriteLine("        if (!editableElement.style.getPropertyValue('--ck-content-font-family')) {");
            Output.WriteLine("          applyFontFix();");
            Output.WriteLine("        }");
            Output.WriteLine("      }).observe(editableElement, { attributes: true, attributeFilter: ['style'] });");
            Output.WriteLine();
            Output.WriteLine("      const sourceEditing = editor.plugins.get('SourceEditing');");
            Output.WriteLine("      const cmInstances = [];");
            Output.WriteLine();
            Output.WriteLine("      // Wrap the plain source-editing textarea(s) with CodeMirror for syntax coloring.");
            Output.WriteLine("      // Registered 'low' priority so it runs AFTER CKEditor's own listener has already");
            Output.WriteLine("      // created the textarea for this change.");
            Output.WriteLine("      sourceEditing.on('change:isSourceEditingMode', (evt, name, value) => {");
            Output.WriteLine("        if (!value) return;");
            Output.WriteLine("        editor.ui.view.element.querySelectorAll('.ck-source-editing-area textarea').forEach(textarea => {");
            Output.WriteLine("          if (textarea.dataset.ck5CmAttached) return;");
            Output.WriteLine("          textarea.dataset.ck5CmAttached = 'true';");
            Output.WriteLine("          // getData() produces compact, unindented HTML - reformat it before CodeMirror reads it in");
            Output.WriteLine("          textarea.value = html_beautify(textarea.value, { indent_size: 2 });");
            Output.WriteLine("          const cm = CodeMirror.fromTextArea(textarea, { mode: 'htmlmixed', lineNumbers: true, lineWrapping: true, theme: 'default' });");
            Output.WriteLine("          cm.on('change', () => { cm.save(); textarea.dispatchEvent(new Event('input')); });");
            Output.WriteLine("          cmInstances.push(cm);");
            Output.WriteLine("        });");
            Output.WriteLine("      }, { priority: 'low' });");
            Output.WriteLine();
            Output.WriteLine("      // Tear the CodeMirror wrapper back down BEFORE CKEditor's own ('normal' priority)");
            Output.WriteLine("      // listener removes the underlying textarea from the DOM.");
            Output.WriteLine("      sourceEditing.on('change:isSourceEditingMode', (evt, name, value) => {");
            Output.WriteLine("        if (value) return;");
            Output.WriteLine("        while (cmInstances.length) {");
            Output.WriteLine("          const cm = cmInstances.pop();");
            Output.WriteLine("          cm.save();");
            Output.WriteLine("          cm.toTextArea();");
            Output.WriteLine("        }");
            Output.WriteLine("      }, { priority: 'high' });");
            Output.WriteLine();

            if (Start_In_Source_Mode)
            {
                Output.WriteLine("      sourceEditing.isSourceEditingMode = true;");
            }

            Output.WriteLine("    })");
            Output.WriteLine("    .catch(error => { console.error('CKEditor 5 failed to initialize:', error); });");
            Output.WriteLine("  </script>");
        }

        /// <summary> Returns the HTML to add the necessary style, import map, and module script
        /// sections to enable CKEditor 5 with these options </summary>
        /// <returns> HTML as a string </returns>
        public string HTML_To_Write()
        {
            var builder = new StringBuilder(500);
            TextWriter writer = new StringWriter(builder);
            Add_To_Stream(writer, true);
            writer.Close();
            return builder.ToString();
        }

        /// <summary> HTTP context for the current request (required for session token storage,
        /// only needed if image upload is being wired in via UploadPath/UploadURL) </summary>
        public HttpContext Context { get; set; }

        /// <summary> Base URL for the system </summary>
        public string BaseUrl { get; set; }

        /// <summary> ID of the existing text area where the HTML to edit resides </summary>
        public string TextAreaID { get; set; }

        /// <summary> Path where uploaded images should be saved.  Must be set, along with
        /// <see cref="UploadURL"/> and <see cref="Context"/>, to enable image upload </summary>
        public string UploadPath { get; set; }

        /// <summary> URL where uploaded images are ultimately served from, to return the
        /// uploaded image's URL back to the editor </summary>
        public string UploadURL { get; set; }

        /// <summary> Flag indicates if it should start with the source-editing view active </summary>
        public bool Start_In_Source_Mode { get; set; }
    }
}
