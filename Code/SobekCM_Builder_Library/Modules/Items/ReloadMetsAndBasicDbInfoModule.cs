#region Using directives

using System;
using System.Data;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Items;
using SobekCM.Resource_Object.Metadata_Modules;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module reloads the basic behavior information from the database into the
    /// digital resource, such as collections and thumbnails </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class ReloadMetsAndBasicDbInfoModule : abstractSubmissionPackageModule
    {
        /// <summary> Reloads the basic behavior information from the database into the
        /// digital resource, such as collections and thumbnails  </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("ReloadMetsAndBasicDbInfoModule.DoWork");

            // Load the METS file
            if (!Resource.Load_METS())
            {
                OnError("Error reading most recent METS file from " + Resource.BibID + ":" + Resource.VID, Resource.BibID + ":" + Resource.VID, String.Empty, Resource.BuilderLogId);
                return false;
            }

            // Add thumbnail, aggregation, viewer, group, and dark/access information from the database --
            // this is the SAME fuller database call and finalize logic the web/engine item-loading path uses
            // (SobekCM_METS_Based_ItemBuilder.Build_Item -> Finish_Building_Item), rather than the older,
            // narrower Add_Minimum_Builder_Information call, so a reprocessed item's data (and, downstream,
            // its cache.protobuf) matches what the web would build fresh from METS + database.
            if (!Resource.NewPackage)
            {
                DataSet itemDetails = Engine_Database.Get_Item_Details(Resource.BibID, Resource.VID, Tracer);
                if (itemDetails != null)
                {
                    bool multipleVolumesExist = (itemDetails.Tables.Count > 2) && (itemDetails.Tables[2].Rows.Count > 0) && (Convert.ToInt32(itemDetails.Tables[2].Rows[0]["Total_Volumes"]) > 1);
                    new SobekCM_METS_Based_ItemBuilder().Finish_Building_Item(Resource.Metadata, itemDetails, multipleVolumesExist, Tracer);
                }
                else
                {
                    Tracer?.Add_Trace("ReloadMetsAndBasicDbInfoModule.DoWork", "Unable to pull item details from the database for " + Resource.BibID + ":" + Resource.VID, Custom_Trace_Type_Enum.Error);
                }
            }
            else
            {
                // Check for any access/restriction/embargo date in the RightsMD section
                RightsMD_Info rightsInfo = Resource.Metadata.Get_Metadata_Module(GlobalVar.PALMM_RIGHTSMD_METADATA_MODULE_KEY) as RightsMD_Info;
                if ((rightsInfo != null) && (rightsInfo.hasData))
                {
                    switch (rightsInfo.Access_Code)
                    {
                        case RightsMD_Info.AccessCode_Enum.Campus:
                            // Was there an embargo date?
                            if (rightsInfo.Has_Embargo_End)
                            {
                                if (DateTime.Compare(DateTime.Now, rightsInfo.Embargo_End) < 0)
                                {
                                    Resource.Metadata.Behaviors.IP_Restriction_Membership = 1;
                                }
                            }
                            else
                            {
                                Resource.Metadata.Behaviors.IP_Restriction_Membership = 1;
                            }
                            break;

                        case RightsMD_Info.AccessCode_Enum.Private:
                            // Was there an embargo date?
                            if (rightsInfo.Has_Embargo_End)
                            {
                                if (DateTime.Compare(DateTime.Now, rightsInfo.Embargo_End) < 0)
                                {
                                    Resource.Metadata.Behaviors.Dark_Flag = true;
                                }
                            }
                            else
                            {
                                Resource.Metadata.Behaviors.Dark_Flag = true;
                            }
                            break;
                    }
                }
            }

            return true;
        }
    }
}
