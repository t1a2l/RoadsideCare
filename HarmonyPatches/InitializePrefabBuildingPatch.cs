using System;
using HarmonyLib;
using RoadsideCare.AI;
using RoadsideCare.Utils;
using UnityEngine;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch(typeof(BuildingInfo), "InitializePrefab")]
    public static class InitializePrefabBuildingPatch
    {
        public static void Prefix(BuildingInfo __instance)
        {
            try
            {
                if(__instance.m_placementStyle == ItemClass.Placement.Manual)
                {
                    if ((__instance.name.ToLower().Contains("gas station") || __instance.name.ToLower().Contains("gasstation")) && __instance.GetAI() is not GasStationAI)
                    {
                        var oldAI = __instance.GetComponent<PrefabAI>();
                        UnityEngine.Object.DestroyImmediate(oldAI);
                        var newAI = (PrefabAI)__instance.gameObject.AddComponent<GasStationAI>();
                        PrefabUtil.TryCopyAttributes(oldAI, newAI, false);
                    }
                    else if ((__instance.name.ToLower().Contains("gaspumps") || __instance.name.ToLower().Contains("gas pumps")) && __instance.GetAI() is not GasPumpAI)
                    {
                        var oldAI = __instance.GetComponent<PrefabAI>();
                        UnityEngine.Object.DestroyImmediate(oldAI);
                        var newAI = (PrefabAI)__instance.gameObject.AddComponent<GasPumpAI>();
                        PrefabUtil.TryCopyAttributes(oldAI, newAI, false);
                    }
                    else if ((__instance.name.ToLower().Contains("carwash") || __instance.name.ToLower().Contains("car wash")) && __instance.GetAI() is not VehicleWashBuildingAI)
                    {
                        var oldAI = __instance.GetComponent<PrefabAI>();
                        UnityEngine.Object.DestroyImmediate(oldAI);
                        var newAI = (PrefabAI)__instance.gameObject.AddComponent<VehicleWashBuildingAI>();
                        PrefabUtil.TryCopyAttributes(oldAI, newAI, false);
                    }
                    //else if ((__instance.name.ToLower().Contains("repairshop") || __instance.name.ToLower().Contains("repair shop")) && __instance.GetAI() is not RepairStationAI)
                    //{
                    //    var oldAI = __instance.GetComponent<PrefabAI>();
                    //    UnityEngine.Object.DestroyImmediate(oldAI);
                    //    var newAI = (PrefabAI)__instance.gameObject.AddComponent<RepairStationAI>();
                    //    PrefabUtil.TryCopyAttributes(oldAI, newAI, false);
                    //
                    //    __instance.m_placementStyle = ItemClass.Placement.Manual;
                    //}
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

    }
}
