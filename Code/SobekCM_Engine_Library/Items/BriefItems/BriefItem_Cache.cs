#region Using directives

using ProtoBuf;
using ProtoBuf.Meta;
using Saxon.Hej.lib;
using SobekCM.Core.BriefItem;
using SobekCM.Core.FileSystems;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Resource_Object.Configuration;
using SobekCM.Tools;
using System;
using System.IO;
using System.Xml.Serialization;

#endregion

namespace SobekCM.Engine_Library.Items.BriefItems
{
    /// <summary> Reads and writes the on-disk protobuf cache of a <see cref="BriefItemInfo"/> for a
    /// digital resource, sitting alongside that resource's other files </summary>
    /// <remarks> Only applies to the default mapping set -- callers requesting a non-default mapping
    /// set should not use this cache, since a cached file always reflects the default set's shape </remarks>
    public static class BriefItem_Cache
    {
        /// <summary> Every <c>[ProtoContract]</c> type under <c>SobekCM.Core.BriefItem</c> -- the full
        /// object graph reachable from a <see cref="BriefItemInfo"/> </summary>
        private static readonly Type[] AllBriefItemTypes = {
            typeof(BriefItemInfo), typeof(BriefItem_BehaviorViewer), typeof(BriefItem_Behaviors),
            typeof(BriefItem_CitationResponse), typeof(BriefItem_Coordinate_Circle), typeof(BriefItem_Coordinate_Line),
            typeof(BriefItem_Coordinate_Point), typeof(BriefItem_Coordinate_Polygon), typeof(BriefItem_DescTermValue),
            typeof(BriefItem_DescriptiveTerm), typeof(BriefItem_ExtensionData), typeof(BriefItem_File),
            typeof(BriefItem_FileGrouping), typeof(BriefItem_GeoSpatial), typeof(BriefItem_Namespace),
            typeof(BriefItem_Related_Titles), typeof(BriefItem_TocElement), typeof(BriefItem_UI),
            typeof(BriefItem_UserGroupRestrictions), typeof(BriefItem_UserTag), typeof(BriefItem_Web),
            typeof(BriefItem_Wordmark)
        };

        /// <summary> Registers every <see cref="BriefItemInfo"/>-related type with protobuf-net's default
        /// model and compiles the whole model in place, so (de)serializing a cached brief item -- including
        /// every nested object, not just the outer type -- uses compiled IL instead of the slower
        /// interpreted/reflection-based path. </summary>
        /// <remarks> Call exactly once, early during application startup -- compiling is a one-time, fairly
        /// expensive cost that only pays for itself across many later (de)serializations, so it should never
        /// run per-request. Registering every type explicitly (rather than relying on a warmup round-trip of
        /// one sample item) is deliberate: <c>RuntimeTypeModel.Add</c> builds a type's schema from its
        /// <c>[ProtoMember]</c> attributes via static reflection over the type definition, not from instance
        /// data, so this is deterministic regardless of what any particular item happens to contain (e.g. an
        /// item with no coordinates would never otherwise touch <see cref="BriefItem_GeoSpatial"/> /
        /// <see cref="BriefItem_Coordinate_Point"/>, silently leaving those uncompiled for a later item that
        /// does have them). Measured ~20-40% faster deserialization versus compiling only
        /// <see cref="BriefItemInfo"/> itself, which left every nested type it references on the slower path. </remarks>
        public static void CompileProtobufModel()
        {
            foreach (Type briefItemType in AllBriefItemTypes)
                RuntimeTypeModel.Default.Add(briefItemType);

            RuntimeTypeModel.Default.CompileInPlace();
        }

        /// <summary> Attempts to read a still-valid cached <see cref="BriefItemInfo"/> for a digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier for the digital resource </param>
        /// <param name="VID"> Volume identifier for the digital resource (or "00000" for a title-level lookup) </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        /// <param name="CachedItem"> [OUT] The cached brief item, if a valid cache was found </param>
        /// <returns> TRUE if a valid (present and not stale) cache file was found and successfully read </returns>
        public static bool TryReadCache(string BibID, string VID, Custom_Tracer Tracer, out BriefItemInfo CachedItem)
        {
            CachedItem = null;
            string cacheFile = SobekFileSystem.Resource_Network_Uri(BibID, VID, ResourceObjectSettings.Metadata_Cache_FileName);

            try
            {
                if (!File.Exists(cacheFile))
                    return false;

                if (File.GetLastWriteTime(cacheFile) < Engine_ApplicationCache_Gateway.Settings.Resources.Metadata_Invalidation_Date)
                {
                    Tracer.Add_Trace("BriefItem_Cache.TryReadCache", "Cache file is older than the metadata invalidation date; ignoring");
                    return false;
                }

                using (var stream = File.OpenRead(cacheFile))
                {
                    CachedItem = Serializer.Deserialize<BriefItemInfo>(stream);
                }

                Tracer.Add_Trace("BriefItem_Cache.TryReadCache", "Loaded brief item from " + ResourceObjectSettings.Metadata_Cache_FileName);
                return CachedItem != null;
            }
            catch (Exception ee)
            {
                // Corrupt, partial, or locked cache file -- treat as a miss, not a hard failure.  The
                // caller will rebuild from METS and this method's write counterpart will overwrite it.
                Tracer.Add_Trace("BriefItem_Cache.TryReadCache", "Error reading cache file: " + ee.Message);
                CachedItem = null;
                return false;
            }
        }

        /// <summary> Writes (or overwrites) the cached <see cref="BriefItemInfo"/> for a digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier for the digital resource </param>
        /// <param name="VID"> Volume identifier for the digital resource (or "00000" for a title-level lookup) </param>
        /// <param name="Item"> Freshly built brief item to cache </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        public static void WriteCache(string BibID, string VID, BriefItemInfo Item, Custom_Tracer Tracer)
        {
            string cacheFile = SobekFileSystem.Resource_Network_Uri(BibID, VID, ResourceObjectSettings.Metadata_Cache_FileName);

            try
            {
                using (var stream = File.Create(cacheFile))
                {
                    Serializer.Serialize(stream, Item);
                }

                Tracer.Add_Trace("BriefItem_Cache.WriteCache", "Wrote brief item to " + ResourceObjectSettings.Metadata_Cache_FileName);

               // string debugXmlFile = Path.Combine("\\\\sobek-frontend\\Files\\demo\\imports", BibID + "_" + VID + "_cache_web.xml");
               // WriteCacheXmlDebug(debugXmlFile, Item, Tracer);
            }
            catch (Exception ee)
            {
                // Best-effort -- same "not critical, self-corrects" philosophy as SobekCM_Item.Delete_Metadata_Cache.
                // A concurrent writer or locked file just means this request doesn't warm the cache; the next
                // request tries again.
                Tracer.Add_Trace("BriefItem_Cache.WriteCache", "Error writing cache file (non-critical): " + ee.Message);
            }
        }

        /// <summary> Writes a human-readable XML copy of a <see cref="BriefItemInfo"/>, for debugging what
        /// actually ended up mapped into the object -- e.g. comparing it against the source METS when the
        /// protobuf cache seems to be missing data </summary>
        /// <param name="OutputFile"> Full file path to write the XML to </param>
        /// <param name="Item"> Freshly built brief item to dump </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        /// <remarks> Uses <see cref="XmlSerializer"/>, not protobuf-net -- <see cref="BriefItemInfo"/> and its
        /// nested types already carry <c>[XmlRoot]</c>/<c>[XmlAttribute]</c>/<c>[XmlElement]</c> attributes for
        /// exactly this (the same mechanism <c>EndpointBase.Serialize</c> uses for XML-format engine responses),
        /// so no extra plumbing is needed to get a readable shape rather than a generic dump. Temporary/debug-only
        /// -- not wired into any real code path; call this alongside <see cref="WriteCache"/> where needed while
        /// tracking down what's missing from the protobuf cache, then remove the call once done. </remarks>
        public static void WriteCacheXmlDebug(string OutputFile, BriefItemInfo Item, Custom_Tracer Tracer)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(BriefItemInfo));
                using (var stream = File.Create(OutputFile))
                {
                    serializer.Serialize(stream, Item);
                }

                Tracer.Add_Trace("BriefItem_Cache.WriteCacheXmlDebug", "Wrote XML debug copy of brief item to " + OutputFile);
            }
            catch (Exception ee)
            {
                // Debugging aid only -- never let a failure here affect the real (protobuf) cache write
                Tracer.Add_Trace("BriefItem_Cache.WriteCacheXmlDebug", "Error writing XML debug file (non-critical): " + ee.Message);
            }
        }

        /// <summary> Deletes the cached <see cref="BriefItemInfo"/> protobuf file for a digital resource, if one exists </summary>
        /// <param name="BibID"> Bibliographic identifier for the digital resource </param>
        /// <param name="VID"> Volume identifier for the digital resource (or "00000" for a title-level lookup) </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        /// <remarks> Used when something outside a normal metadata save changes whether a cached item's data is
        /// still accurate -- e.g., <see cref="BriefItem_Web.Siblings"/> crossing the 1/multi-volume boundary
        /// because a sibling item was added to or deleted from the same title -- so the affected item's cache is
        /// rebuilt fresh on its next request. Best-effort, like <see cref="WriteCache"/>: a missing or locked
        /// file is not an error. </remarks>
        public static void DeleteCache(string BibID, string VID, Custom_Tracer Tracer)
        {
            string cacheFile = SobekFileSystem.Resource_Network_Uri(BibID, VID, ResourceObjectSettings.Metadata_Cache_FileName);

            try
            {
                if (File.Exists(cacheFile))
                {
                    File.Delete(cacheFile);
                    Tracer.Add_Trace("BriefItem_Cache.DeleteCache", "Deleted brief item cache file " + ResourceObjectSettings.Metadata_Cache_FileName);
                }
            }
            catch (Exception ee)
            {
                Tracer.Add_Trace("BriefItem_Cache.DeleteCache", "Error deleting cache file (non-critical): " + ee.Message);
            }
        }
    }
}
