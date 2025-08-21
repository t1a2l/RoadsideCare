using System;
using ColossalFramework;
using UnityEngine;

namespace RoadsideCare.Utils
{
    public class RoadsideCareSettings
    {
        public const string settingsFileName = "RoadsideCare_Settings";

        public static SavedFloat PassengerCarFuelingTimeInSeconds = new("PassengerCarFuelingTimeInSeconds", settingsFileName, 10, true);

        public static SavedFloat CargoTruckFuelingTimeInSeconds = new("CargoTruckFuelingTimeInSeconds", settingsFileName, 10, true);

        public static SavedFloat PassengerCarHandWashTimeInSeconds = new("PassengerCarHandWashTimeInSeconds", settingsFileName, 10, true);

        public static SavedFloat CargoTruckHandWashTimeInSeconds = new("CargoTruckHandWashTimeInSeconds", settingsFileName, 10, true);

        public static void Init()
        {
            try
            {
                // Creating setting file
                if (GameSettings.FindSettingsFileByName(settingsFileName) == null)
                {
                    GameSettings.AddSettingsFile([new SettingsFile() { fileName = settingsFileName }]);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Could not load/create the setting file.");
                Debug.LogException(e);
            }
        }
    }
}
