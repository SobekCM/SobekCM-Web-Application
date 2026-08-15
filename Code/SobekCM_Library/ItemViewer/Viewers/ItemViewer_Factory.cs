#region Using directives

using SobekCM.Core.BriefItem;
using SobekCM.Core.UI_Configuration.Viewers;
using SobekCM.Library.ItemViewer.Viewers;
using SobekCM.Library.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#endregion

namespace SobekCM.Library.ItemViewer
{
    /// <summary> Static class is a factory that generates and returns the requested item viewer object 
    /// which implements the <see cref="SobekCM.Library.ItemViewer.Viewers.iItemViewer"/> interface.</summary>
    public static class ItemViewer_Factory
    {
        private static Dictionary<string, iItemViewerPrototyper> viewerCodeToItemViewerPrototyper;
        private static Dictionary<string, iItemViewerPrototyper> viewTypeToItemViewerPrototyper;
        private static List<ItemSubViewerConfig> mgmtViewerConfigs;
        private static readonly object configLock = new object();

        /// <summary> Configure the viewers for a single brief item to match this user interface settings </summary>
        /// <param name="BriefItem"> Brief item to adjust selected viewers </param>
        public static void Configure_Brief_Item_Viewers(BriefItemInfo BriefItem)
        {
            // Ensure the necessary dictionaries are built
            configureItemViewers();

            // If the brief item already has the UI built, skip this
            if ((BriefItem.UI != null) && (BriefItem.UI.Viewers_By_Priority != null) && (BriefItem.UI.Viewers_Menu_Order != null))
                return;

            // BriefItem is frequently a shared, cached instance (see CachedDataManager) -- lock on it so
            // concurrent requests for the same freshly-cached item can't both slip past the guard above
            // and race to mutate BriefItem.UI's collections (and, transitively, other lazily-built
            // collections elsewhere on the item, like BriefItem_Web.fileExtensionLookupDictionary, which
            // Include_Viewer below can reach into) at the same time. No other code locks on a BriefItemInfo
            // instance, so this can't deadlock against anything else.
            lock (BriefItem)
            {
                // Another thread may have already finished building this while this thread waited for the lock
                if ((BriefItem.UI != null) && (BriefItem.UI.Viewers_By_Priority != null) && (BriefItem.UI.Viewers_Menu_Order != null))
                    return;

                // Snapshot the current published collections into locals -- these fields are only ever
                // replaced wholesale (never mutated in place), so a local reference stays valid and fully
                // populated for the rest of this method even if another thread calls Clear() concurrently
                Dictionary<string, iItemViewerPrototyper> viewTypeLookup = viewTypeToItemViewerPrototyper;
                List<ItemSubViewerConfig> mgmtConfigs = mgmtViewerConfigs;

                // Now, add the UI object (if null) and set the values
                if (BriefItem.UI == null) BriefItem.UI = new BriefItem_UI();
                if (BriefItem.UI.Viewers_By_Priority == null) BriefItem.UI.Viewers_By_Priority = new List<string>();
                if (BriefItem.UI.Viewers_Menu_Order == null) BriefItem.UI.Viewers_Menu_Order = new List<string>();

                // Use a sorted list to build the menu order
                var menuOrderSort = new SortedDictionary<float, string>();

                // Step through each viewer included from the database
                foreach (BriefItem_BehaviorViewer viewer in BriefItem.Behaviors.Viewers)
                {
                    // If this is an EXCLUDE viewer, just skip it here
                    if (viewer.Excluded)
                        continue;

                    // Verify a match in the UI configuration for the actual item viewers
                    if (!viewTypeLookup.ContainsKey(viewer.ViewerType))
                        continue;

                    // Get the item prototype object
                    iItemViewerPrototyper protoTyper = viewTypeLookup[viewer.ViewerType];

                    // Verify this prototyper is not NULL and believes it should be added
                    if ((protoTyper != null) && (protoTyper.Include_Viewer(BriefItem)))
                    {
                        // Add this view to the ordained views
                        BriefItem.UI.Viewers_By_Priority.Add(viewer.ViewerType);

                        // Check for collisions on the menu order
                        float menuOrder = viewer.MenuOrder;
                        while (menuOrderSort.ContainsKey(menuOrder))
                            menuOrder = menuOrder + .001f;

                        // Also add this to the menu order sorted list
                        menuOrderSort[menuOrder] = viewer.ViewerType;

                        // Also add this to the dictionary for lookup
                        BriefItem.UI.Add_Viewer_Code(protoTyper.ViewerCode, protoTyper.ViewerType);
                    }
                }

                // Also, add each of the management style viewers, that are not generally stored in the database
                // but we will still double check it wasn't already added
                foreach (ItemSubViewerConfig viewerConfig in mgmtConfigs)
                {
                    // Verify a match in the UI configuration for the actual item viewers
                    if (!viewTypeLookup.ContainsKey(viewerConfig.ViewerType))
                        continue;

                    // Is it ALREADY added though? (may be added with EXCLUDE set to true)
                    if (BriefItem.UI.Includes_Viewer_Type(viewerConfig.ViewerType))
                        continue;

                    // Get the item prototype object
                    iItemViewerPrototyper protoTyper = viewTypeLookup[viewerConfig.ViewerType];

                    // Verify this prototyper is not NULL and believes it should be added
                    if ((protoTyper != null) && (protoTyper.Include_Viewer(BriefItem)))
                    {
                        // Add this view to the ordained views
                        BriefItem.UI.Viewers_By_Priority.Add(viewerConfig.ViewerType);

                        // Check for collisions on the menu order
                        float menuOrder = viewerConfig.ManagementOrder;
                        while (menuOrderSort.ContainsKey(menuOrder))
                            menuOrder = menuOrder + .001f;

                        // Also add this to the menu order sorted list
                        menuOrderSort[menuOrder] = viewerConfig.ViewerType;

                        // Also add this to the dictionary for lookup
                        BriefItem.UI.Add_Viewer_Code(protoTyper.ViewerCode, protoTyper.ViewerType);
                    }
                }

                // Add the viewers back in menu order to the menu order portion
                foreach (float thisKey in menuOrderSort.Keys)
                {
                    BriefItem.UI.Viewers_Menu_Order.Add(menuOrderSort[thisKey]);
                }
            }
        }


        private static void configureItemViewers()
        {
            // If already configured, do nothing
            if ((viewTypeToItemViewerPrototyper != null) && (viewTypeToItemViewerPrototyper.Count > 0) &&
                (viewerCodeToItemViewerPrototyper != null) && (viewerCodeToItemViewerPrototyper.Count > 0) &&
                (mgmtViewerConfigs != null))
                return;

            lock (configLock)
            {
                // Another thread may have already finished building this while this thread was
                // waiting for the lock -- without this re-check, two concurrent first-callers would
                // both fall through and write into the same static dictionaries at once, corrupting
                // them (Dictionary<TKey,TValue> isn't thread-safe for concurrent writes)
                if ((viewTypeToItemViewerPrototyper != null) && (viewTypeToItemViewerPrototyper.Count > 0) &&
                    (viewerCodeToItemViewerPrototyper != null) && (viewerCodeToItemViewerPrototyper.Count > 0) &&
                    (mgmtViewerConfigs != null))
                    return;

                // Build into local collections and publish them with a single reference assignment
                // each at the end, so concurrent readers on other threads (which read the static
                // fields without a lock) never observe a partially-built dictionary
                var newViewerCodeToItemViewerPrototyper = new Dictionary<string, iItemViewerPrototyper>(StringComparer.OrdinalIgnoreCase);
                var newViewTypeToItemViewerPrototyper = new Dictionary<string, iItemViewerPrototyper>(StringComparer.OrdinalIgnoreCase);

                // Temporary sorter
                var mgmtOrder = new SortedDictionary<float, ItemSubViewerConfig>();

                // Step through all the potential item viewers prototypes in the dictionary
                foreach (ItemSubViewerConfig thisViewerConfig in UI_ApplicationCache_Gateway.Configuration.UI.WriterViewers.Items.Viewers)
                {
                    // If this is not enabled, skip it
                    if (!thisViewerConfig.Enabled)
                        continue;

                    // Get the code and viewer type
                    string code = thisViewerConfig.ViewerCode;
                    string type = thisViewerConfig.ViewerType;

                    // Build the prototyper
                    iItemViewerPrototyper prototyper = configurePrototyper(thisViewerConfig.Assembly, thisViewerConfig.Class);

                    // If this failed to create a prototyper move on
                    if (prototyper == null)
                        continue;

                    // Add any other configuration here
                    if (!String.IsNullOrEmpty(thisViewerConfig.ViewerCode))
                        prototyper.ViewerCode = thisViewerConfig.ViewerCode;
                    if ((thisViewerConfig.PageExtensions != null) && (thisViewerConfig.PageExtensions.Length > 0))
                        prototyper.FileExtensions = thisViewerConfig.PageExtensions;
                    else if ((thisViewerConfig.FileExtensions != null) && (thisViewerConfig.FileExtensions.Length > 0))
                        prototyper.FileExtensions = thisViewerConfig.FileExtensions;

                    // Add this to the dictionaries
                    newViewTypeToItemViewerPrototyper[type] = prototyper;
                    newViewerCodeToItemViewerPrototyper[prototyper.ViewerCode] = prototyper;

                    // Was this a management viewer?
                    if (thisViewerConfig.ManagementViewer)
                    {
                        // Get the order and ensure there are no collisions
                        float orderValue = thisViewerConfig.ManagementOrder;
                        while (mgmtOrder.ContainsKey(orderValue))
                            orderValue += .001f;

                        // Add this to the sort list
                        mgmtOrder[orderValue] = thisViewerConfig;
                    }
                }

                // Publish the newly-built collections
                viewTypeToItemViewerPrototyper = newViewTypeToItemViewerPrototyper;
                viewerCodeToItemViewerPrototyper = newViewerCodeToItemViewerPrototyper;
                mgmtViewerConfigs = mgmtOrder.Values.ToList();
            }
        }

        private static iItemViewerPrototyper configurePrototyper(string assembly, string className)
        {
            // Was an assembly indicated
            if (String.IsNullOrEmpty(assembly))
            {

                // Return a standard class
                switch (className)
                {
                    //case "SobekCM.Library.ItemViewer.Viewers.GIF_ItemViewer_Prototyper":
                    //    return new GIF_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Archives_ItemViewer_Prototyper":
                        return new Archives_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Audio_ItemViewer_Prototyper":
                        return new Audio_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Citation_MARC_ItemViewer_Prototyper":
                        return new Citation_MARC_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Citation_Standard_ItemViewer_Prototyper":
                        return new Citation_Standard_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Directory_ItemViewer_Prototyper":
                        return new Directory_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Downloads_ItemViewer_Prototyper":
                        return new Downloads_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.EmbeddedVideo_ItemViewer_Prototyper":
                        return new EmbeddedVideo_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Flash_ItemViewer_Prototyper":
                        return new Flash_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.GnuBooks_PageTurner_ItemViewer_Prototyper":
                        return new GnuBooks_PageTurner_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Google_Map_ItemViewer_Prototyper":
                        return new Google_Map_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Google_Coordinate_Entry_ItemViewer_Prototyper":
                        return new Google_Coordinate_Entry_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.HTML_ItemViewer_Prototyper":
                        return new HTML_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.JPEG_ItemViewer_Prototyper":
                        return new JPEG_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.JPEG2000_ItemViewer_Prototyper":
                        return new JPEG2000_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.ManageMenu_ItemViewer_Prototyper":
                        return new ManageMenu_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Metadata_Links_ItemViewer_Prototyper":
                        return new Metadata_Links_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Milestones_ItemViewer_Prototyper":
                        return new Milestones_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.MultiVolumes_ItemViewer_Prototyper":
                        return new MultiVolumes_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.OpenTextbook_ItemViewer_Prototyper":
                        return new OpenTextbook_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.PDF_ItemViewer_Prototyper":
                        return new PDF_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.QC_ItemViewer_Prototyper":
                        return new QC_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Related_Images_ItemViewer_Prototyper":
                        return new Related_Images_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.TEI_ItemViewer_Prototyper":
                        return new TEI_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Text_ItemViewer_Prototyper":
                        return new Text_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Text_Search_ItemViewer_Prototyper":
                        return new Text_Search_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.TrackingSheet_ItemViewer_Prototyper":
                        return new TrackingSheet_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Tracking_ItemViewer_Prototyper":
                        return new Tracking_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Usage_Stats_ItemViewer_Prototyper":
                        return new Usage_Stats_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.Video_ItemViewer_Prototyper":
                        return new Video_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.UF_Archives_ItemViewer_Prototyper":
                        return new UF_Archives_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.UF_Media_ItemViewer_Prototyper":
                        return new UF_Media_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.HTML_WebSite_ItemViewer_Prototyper":
                        return new HTML_WebSite_ItemViewer_Prototyper();

                    case "SobekCM.Library.ItemViewer.Viewers.SearchEngineIndexing_ItemViewer_Prototyper":
                        return new SearchEngineIndexing_ItemViewer_Prototyper();
                }

                // If it made it here, there is no assembly, but it is an unexpected type.  
                // Just create it from the same assembly then
                try
                {
                    Assembly dllAssembly = Assembly.GetCallingAssembly();
                    Type prototyperType = dllAssembly.GetType(className);
                    iItemViewerPrototyper returnObj = (iItemViewerPrototyper)Activator.CreateInstance(prototyperType);
                    return returnObj;
                }
                catch (Exception)
                {
                    // Not sure exactly what to do here, honestly
                    return null;
                }
            }


            // An assembly was indicated
            try
            {
                // Try to find the file/path for this assembly then
                Assembly dllAssembly = null;
                string assemblyFilePath = UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Assembly(assembly);
                if (assemblyFilePath != null)
                {
                    dllAssembly = Assembly.LoadFrom(assemblyFilePath);
                }
                Type prototyperType = dllAssembly.GetType(className);
                iItemViewerPrototyper returnObj = (iItemViewerPrototyper)Activator.CreateInstance(prototyperType);
                return returnObj;
            }
            catch (Exception ee)
            {
                // Not sure exactly what to do here, honestly
                if (ee.Message.Length > 0)
                    return null;
                return null;
            }
        }

        /// <summary> Clears the dictionaries of item viewer prototypes, used when the cache
        /// is reset either manually or automatically </summary>
        /// <returns></returns>
        public static void Clear()
        {
            // Null out the references (rather than calling .Clear() on the existing dictionaries)
            // so any thread already holding a snapshot of the old reference keeps a valid, fully
            // populated dictionary instead of having it emptied out from under an in-progress read
            lock (configLock)
            {
                viewerCodeToItemViewerPrototyper = null;
                viewTypeToItemViewerPrototyper = null;
                mgmtViewerConfigs = null;
            }
        }

        /// <summary> Gets the viewer code (used in URLs and such) for a specific view type,
        /// or NULL if the current instance doesn't support that viewer type </summary>
        /// <param name="ViewType"> Standard type of the viewer to find </param>
        /// <returns> Viewer code (used in URLs and such) for a specific view type,
        /// or NULL if the current instance doesn't support that viewer type </returns>
        public static string ViewCode_From_ViewType(string ViewType)
        {
            // Ensure the necessary dictionaries are built
            configureItemViewers();

            Dictionary<string, iItemViewerPrototyper> viewTypeLookup = viewTypeToItemViewerPrototyper;

            // If this not in the dictionary, return NULL
            if (!viewTypeLookup.ContainsKey(ViewType))
                return null;

            // return match
            return viewTypeLookup[ViewType].ViewerCode;
        }

        /// <summary> Gets the standard viewer type from a viewer code,
        /// or NULL if the current instance doesn't support that viewer code </summary>
        /// <param name="ViewerCode"> Viewer code to look for viewer </param>
        /// <returns> Viewer type from a viewer code,
        /// or NULL if the current instance doesn't support that viewer code </returns>
        public static string ViewType_From_ViewerCode(string ViewerCode)
        {
            // Ensure the necessary dictionaries are built
            configureItemViewers();

            Dictionary<string, iItemViewerPrototyper> viewerCodeLookup = viewerCodeToItemViewerPrototyper;

            // If this not in the dictionary, return NULL
            if (!viewerCodeLookup.ContainsKey(ViewerCode))
                return null;

            // return match
            return viewerCodeLookup[ViewerCode].ViewerType;
        }

        /// <summary> Accepts a viewer code (string) from the digital resource object and returns
        /// the appropriate item viewer object which extends the <see cref="SobekCM.Library.ItemViewer.Viewers.iItemViewerPrototyper"/>
        /// class for rendering items to the web via HTML.</summary>
        /// <param name="ViewerCode"> Viewer code to retrieve </param>
        /// <returns> Genereated item viewer class for rendering the particular view of a digital resource
        /// via HTML. </returns>
        public static iItemViewerPrototyper Get_Viewer_By_ViewerCode(string ViewerCode)
        {
            // Ensure the necessary dictionaries are built
            configureItemViewers();

            Dictionary<string, iItemViewerPrototyper> viewerCodeLookup = viewerCodeToItemViewerPrototyper;

            // Get this viewer, by viewer code
            if (viewerCodeLookup.ContainsKey(ViewerCode))
                return viewerCodeLookup[ViewerCode];

            return null;
        }


        /// <summary> Accepts a view type (string) from the digital resource object and returns
        /// the appropriate item viewer object which extends the <see cref="SobekCM.Library.ItemViewer.Viewers.iItemViewerPrototyper"/>
        /// class for rendering items to the web via HTML.</summary>
        /// <param name="ViewType"> Viewer code to retrieve </param>
        /// <returns> Genereated item viewer class for rendering the particular view of a digital resource
        /// via HTML. </returns>
        public static iItemViewerPrototyper Get_Viewer_By_ViewType(string ViewType)
        {
            // Ensure the necessary dictionaries are built
            configureItemViewers();

            Dictionary<string, iItemViewerPrototyper> viewTypeLookup = viewTypeToItemViewerPrototyper;

            // Get this viewer, by viewer code
            if (viewTypeLookup.ContainsKey(ViewType))
                return viewTypeLookup[ViewType];

            return null;
        }

        /// <summary> Gets the appropriate item viewer prototyper ( <see cref="SobekCM.Library.ItemViewer.Viewers.iItemViewerPrototyper"/> )
        /// class for an individual item, based on the requested viewer code </summary>
        /// <param name="CurrentItem">The current item.</param>
        /// <param name="ViewerCode">The viewer code.</param>
        /// <returns> Item viewer prototyper class, which can then be used to create the viewer, if applicable </returns>
        public static iItemViewerPrototyper Get_Item_Viewer(BriefItemInfo CurrentItem, string ViewerCode)
        {
            // Get the viewer type from the item
            string validType = CurrentItem.UI.Get_Viewer_Type(ViewerCode);

            // Now, return this prototype 
            return Get_Viewer_By_ViewType(validType);
        }
    }
}
