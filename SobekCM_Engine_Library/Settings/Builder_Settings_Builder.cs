﻿#region Using directives

using System;
using System.Collections.Generic;
using System.Data;
using SobekCM.Core.Builder;
using SobekCM.Core.Settings;

#endregion

namespace SobekCM.Engine_Library.Settings
{
    /// <summary> Creates the builder settings, which holds data that is used by the builder </summary>
    public class Builder_Settings_Builder
    {
        /// <summary> Refreshes the specified builder settings object, from the information pulled from the database </summary>
        /// <param name="SettingsObject"> Current builer settings object to refresh </param>
        /// <param name="SobekCM_Settings"> Dataset of all the builder settings, from the instance database </param>
        /// <param name="IncludeModuleDescriptions"> Flag indicates if the module descriptions should be included for human readability </param>
        /// <param name="DataTableOffset"> If some previous tables exist, and should be skipped, set this to a non-zero value</param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool Refresh(Builder_Settings SettingsObject, DataSet SobekCM_Settings, bool IncludeModuleDescriptions, int DataTableOffset = 0)
        {
            SettingsObject.Clear();
            try
            {
                Dictionary<int, List<Builder_Source_Folder>> folder_to_set_dictionary = new Dictionary<int, List<Builder_Source_Folder>>();
                Dictionary<int, List<Builder_Module_Setting>> setid_to_modules = new Dictionary<int, List<Builder_Module_Setting>>();

                Set_Builder_Folders(SettingsObject, SobekCM_Settings.Tables[DataTableOffset], folder_to_set_dictionary);

                Set_NonScheduled_Modules(SettingsObject, SobekCM_Settings.Tables[1 + DataTableOffset], setid_to_modules, IncludeModuleDescriptions);

                Set_Scheduled_Modules(SettingsObject, SobekCM_Settings.Tables[2 + DataTableOffset], IncludeModuleDescriptions);

                // Link the folders to the builder module sets
                foreach (KeyValuePair<int, List<Builder_Module_Setting>> module in setid_to_modules)
                {
                    if (folder_to_set_dictionary.ContainsKey(module.Key))
                    {
                        List<Builder_Source_Folder> folders = folder_to_set_dictionary[module.Key];
                        foreach (Builder_Source_Folder thisFolder in folders)
                        {
                            thisFolder.Builder_Module_Set.Builder_Modules = module.Value;
                        }
                    }
                }

                return true;
            }
            catch 
            {
                return false;
            }
        }

        private static void Set_NonScheduled_Modules(Builder_Settings SettingsObject, DataTable BuilderFoldersTable, Dictionary<int, List<Builder_Module_Setting>> SetidToModules, bool IncludeModuleDescriptions )
        {
            DataColumn assemblyColumn = BuilderFoldersTable.Columns["Assembly"];
            DataColumn classColumn = BuilderFoldersTable.Columns["Class"];
            DataColumn descColumn = BuilderFoldersTable.Columns["ModuleDesc"];
            DataColumn args1Column = BuilderFoldersTable.Columns["Argument1"];
            DataColumn args2Column = BuilderFoldersTable.Columns["Argument2"];
            DataColumn args3Column = BuilderFoldersTable.Columns["Argument3"];
            DataColumn setIdColumn = BuilderFoldersTable.Columns["ModuleSetID"];
            DataColumn typeAbbrevColumn = BuilderFoldersTable.Columns["TypeAbbrev"];


            Dictionary<int,List<Builder_Module_Setting>> folderSettings = new Dictionary<int, List<Builder_Module_Setting>>();
            foreach (DataRow thisRow in BuilderFoldersTable.Rows)
            {
                string type = thisRow[typeAbbrevColumn].ToString().ToUpper();

                Builder_Module_Setting newSetting = new Builder_Module_Setting
                {
                    Class = thisRow[classColumn].ToString()
                };
                if (thisRow[assemblyColumn] != DBNull.Value)
                    newSetting.Assembly = thisRow[assemblyColumn].ToString();
                if (thisRow[args1Column] != DBNull.Value)
                    newSetting.Argument1 = thisRow[args1Column].ToString();
                if (thisRow[args2Column] != DBNull.Value)
                    newSetting.Argument2 = thisRow[args2Column].ToString();
                if (thisRow[args3Column] != DBNull.Value)
                    newSetting.Argument3 = thisRow[args3Column].ToString();

                if (IncludeModuleDescriptions)
                {
                    if (thisRow[descColumn] != DBNull.Value)
                        newSetting.Description = thisRow[descColumn].ToString();
                }

                switch (type)
                {
                    case "PRE":
                        SettingsObject.PreProcessModulesSettings.Add(newSetting);
                        break;

                    case "POST":
                        SettingsObject.PostProcessModulesSettings.Add(newSetting);
                        break;

                    case "NEW":
                        SettingsObject.ItemProcessModulesSettings.Add(newSetting);
                        break;

                    case "DELT":
                        SettingsObject.ItemDeleteModulesSettings.Add(newSetting);
                        break;

                    case "FOLD":
                        int setId = Int32.Parse(thisRow[setIdColumn].ToString());
                        if (SetidToModules.ContainsKey(setId))
                        {
                            SetidToModules[setId].Add(newSetting);
                        }
                        else
                        {
                            SetidToModules[setId] = new List<Builder_Module_Setting> {newSetting};
                        }
                        break;
                }
            }
        }

        private static void Set_Scheduled_Modules(Builder_Settings SettingsObject, DataTable BuilderFoldersTable, bool IncludeModuleDescriptions)
        {
            DataColumn assemblyColumn = BuilderFoldersTable.Columns["Assembly"];
            DataColumn classColumn = BuilderFoldersTable.Columns["Class"];
            DataColumn descColumn = BuilderFoldersTable.Columns["ModuleDesc"];
            DataColumn args1Column = BuilderFoldersTable.Columns["Argument1"];
            DataColumn args2Column = BuilderFoldersTable.Columns["Argument2"];
            DataColumn args3Column = BuilderFoldersTable.Columns["Argument3"];
            DataColumn daysOfWeekColumn = BuilderFoldersTable.Columns["DaysOfWeek"];
            DataColumn timeOfDayColumn = BuilderFoldersTable.Columns["TimesOfDay"];
            DataColumn lastRunColumn = BuilderFoldersTable.Columns["LastRun"];

            Dictionary<string, Builder_Schedulable_Module_Setting> alreadyBuilt = new Dictionary<string, Builder_Schedulable_Module_Setting>();

            foreach (DataRow thisRow in BuilderFoldersTable.Rows)
            {
                string className = thisRow[classColumn].ToString();
                string assemblyName = thisRow[assemblyColumn].ToString();
                string key = assemblyName + ":" + className;

                var newModule = new Builder_Schedulable_Module_Setting();
                if (alreadyBuilt.ContainsKey(key))
                {
                    newModule = alreadyBuilt[key];
                }
                else
                {
                    newModule.Class = className;
                    newModule.Assembly = assemblyName;

                    if (thisRow[assemblyColumn] != DBNull.Value)
                        newModule.Assembly = thisRow[assemblyColumn].ToString();
                    if (thisRow[args1Column] != DBNull.Value)
                        newModule.Argument1 = thisRow[args1Column].ToString();
                    if (thisRow[args2Column] != DBNull.Value)
                        newModule.Argument2 = thisRow[args2Column].ToString();
                    if (thisRow[args3Column] != DBNull.Value)
                        newModule.Argument3 = thisRow[args3Column].ToString();

                    if (IncludeModuleDescriptions)
                    {
                        if (thisRow[descColumn] != DBNull.Value)
                            newModule.Description = thisRow[descColumn].ToString();
                    }

                    SettingsObject.ScheduledModulesSettings.Add(newModule);
                    alreadyBuilt[key] = newModule;
                }

                // Get this schedule
                Builder_Module_Schedule schedule = new Builder_Module_Schedule();
                schedule.DaysOfWeek = thisRow[daysOfWeekColumn].ToString();
                schedule.TimeOfDay = thisRow[timeOfDayColumn].ToString();
                if (thisRow[lastRunColumn] != DBNull.Value)
                    schedule.LastRun = Convert.ToDateTime(thisRow[lastRunColumn]);

                newModule.Schedules.Add(schedule);
            }
        }


        private static void Set_Builder_Folders(Builder_Settings SettingsObject, DataTable BuilderFoldersTable, Dictionary<int, List<Builder_Source_Folder>> FolderToSetDictionary)
        {
            SettingsObject.IncomingFolders.Clear();
            foreach (DataRow thisRow in BuilderFoldersTable.Rows)
            {
                Builder_Source_Folder newFolder = new Builder_Source_Folder
                {
                    IncomingFolderID = Convert.ToInt32(thisRow["IncomingFolderId"]),
                    Folder_Name = thisRow["FolderName"].ToString(),
                    Inbound_Folder = thisRow["NetworkFolder"].ToString(),
                    Failures_Folder = thisRow["ErrorFolder"].ToString(),
                    Processing_Folder = thisRow["ProcessingFolder"].ToString(),
                    Perform_Checksum = Convert.ToBoolean(thisRow["Perform_Checksum_Validation"]),
                    Archive_TIFFs = Convert.ToBoolean(thisRow["Archive_TIFF"]),
                    Archive_All_Files = Convert.ToBoolean(thisRow["Archive_All_Files"]),
                    Allow_Deletes = Convert.ToBoolean(thisRow["Allow_Deletes"]),
                    Allow_Folders_No_Metadata = Convert.ToBoolean(thisRow["Allow_Folders_No_Metadata"]),
                    Allow_Metadata_Updates = Convert.ToBoolean(thisRow["Allow_Metadata_Updates"]),
                    BibID_Roots_Restrictions = thisRow["BibID_Roots_Restrictions"].ToString(), 
                    Builder_Module_Set = {SetID = Convert.ToInt32(thisRow["ModuleSetID"]), SetName = thisRow["SetName"].ToString()}
                };


                if (( thisRow["ModuleSetID"] != null) && ( thisRow["ModuleSetID"].ToString().Length > 0 ))
                {
                    int id = Int32.Parse(thisRow["ModuleSetID"].ToString());
                    if (FolderToSetDictionary.ContainsKey(id))
                        FolderToSetDictionary[id].Add(newFolder);
                    else
                    {
                        FolderToSetDictionary[id] = new List<Builder_Source_Folder> {newFolder};
                    }
                }

                SettingsObject.IncomingFolders.Add(newFolder);
            }
        }
    }
}
