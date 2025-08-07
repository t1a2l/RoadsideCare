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
        string IUserMod.Description => "Track individual vehicles' needs and strategically place gas stations, car washes, and mechanic shops.";

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
}
