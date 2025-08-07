using System;
using RoadsideCare.Managers;
using RoadsideCare.Utils;
using CitiesHarmony.API;
using ICities;
using UnityEngine;

namespace RoadsideCare
{
    public class Mod : LoadingExtensionBase, IUserMod
    {
        string IUserMod.Name => "Roadside Care";

        string IUserMod.Description => "Track individual vehicles' needs and strategically place gas stations, car washes, and repair shops as roadside care for vehicles.";

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => PatchUtil.PatchAll());
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) PatchUtil.UnpatchAll();
        }

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            try
            {
                VehicleNeedsManager.Init();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                VehicleNeedsManager.Deinit();
            }
        }

        public override void OnReleased()
        {
            base.OnReleased();
            try
            {
                VehicleNeedsManager.Deinit();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

        
    }


    public class CleanupStaleParkedVehicle : ThreadingExtensionBase
    {
        private int cleanupCounter = 0;
        private const int cleanupInterval = 600; // Run cleanup every 10 seconds (600 frames)

        public override void OnBeforeSimulationFrame()
        {
            // Your main mod logic here...

            // Increment and check the counter
            cleanupCounter++;
            if (cleanupCounter >= cleanupInterval)
            {
                VehicleNeedsManager.CleanupStaleParkedVehicleNeeds();
                cleanupCounter = 0; // Reset the counter
            }
        }
    }
}
