#region Using directives

using System.IO;
using System.Xml;

#endregion

namespace SobekCM.Resource_Object.Utilities
{
    /// <summary> Builds XmlReaders for parsing externally-sourced metadata content, defaulting to
    /// prohibiting DOCTYPE/external-entity resolution (XXE hardening, and avoids unexplained parsing
    /// delays from resolving external references) unless a specific format's configuration explicitly
    /// opts in -- see the various Allow_DOCTYPE_In_* settings on Metadata_Configuration. </summary>
    public static class Safe_Xml_Reader_Factory
    {
        /// <summary> Builds an XmlReader over the given stream, using the legacy DOCTYPE-permissive
        /// XmlTextReader only if Allow_Doctype is TRUE; otherwise the modern XmlReader.Create factory,
        /// which prohibits DOCTYPE by default. </summary>
        public static XmlReader Create(Stream Input_Stream, bool Allow_Doctype)
        {
            return Allow_Doctype ? new XmlTextReader(Input_Stream) : XmlReader.Create(Input_Stream);
        }

        /// <summary> Builds an XmlReader over the given text reader (e.g. a StringReader), using the
        /// legacy DOCTYPE-permissive XmlTextReader only if Allow_Doctype is TRUE; otherwise the modern
        /// XmlReader.Create factory, which prohibits DOCTYPE by default. </summary>
        public static XmlReader Create(TextReader Input_Reader, bool Allow_Doctype)
        {
            return Allow_Doctype ? new XmlTextReader(Input_Reader) : XmlReader.Create(Input_Reader);
        }

        /// <summary> Builds an XmlReader over the file at the given path, using the legacy
        /// DOCTYPE-permissive XmlTextReader only if Allow_Doctype is TRUE; otherwise the modern
        /// XmlReader.Create factory, which prohibits DOCTYPE by default. </summary>
        public static XmlReader Create(string FilePath, bool Allow_Doctype)
        {
            return Allow_Doctype ? new XmlTextReader(FilePath) : XmlReader.Create(FilePath);
        }

        /// <summary> Builds an XmlReader that allows DOCTYPE declarations (including internal DTD subset
        /// entities defined within the document itself) but prohibits external entity/DTD resolution --
        /// XmlResolver = null means the parser has no mechanism to fetch anything external, so
        /// SYSTEM/PUBLIC references become inert rather than triggering network/filesystem access, while
        /// internal-subset entities continue to work normally. For formats that legitimately rely on
        /// DOCTYPE (EAD, TEI/XSLT, OAI-PMH harvesting) where fully prohibiting it would break real, needed
        /// documents, but unrestricted external resolution is still not wanted. </summary>
        public static XmlReader Create_Doctype_Permissive(Stream Input_Stream)
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, XmlResolver = null };
            return XmlReader.Create(Input_Stream, settings);
        }

        /// <summary> Builds an XmlReader that allows DOCTYPE declarations but prohibits external
        /// entity/DTD resolution -- see the Stream overload's remarks for the rationale. </summary>
        public static XmlReader Create_Doctype_Permissive(TextReader Input_Reader)
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, XmlResolver = null };
            return XmlReader.Create(Input_Reader, settings);
        }

        /// <summary> Builds an XmlReader that allows DOCTYPE declarations but prohibits external
        /// entity/DTD resolution -- see the Stream overload's remarks for the rationale. </summary>
        public static XmlReader Create_Doctype_Permissive(string FilePath)
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, XmlResolver = null };
            return XmlReader.Create(FilePath, settings);
        }
    }
}
