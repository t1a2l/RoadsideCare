using ColossalFramework;
using ColossalFramework.Globalization;
using HarmonyLib;
using MoreTransferReasons;
using MoreTransferReasons.AI;
using RoadsideCare.AI;
using RoadsideCare.Managers;
using UnityEngine;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
    public static class ExtendedCargoTruckAIPatch
    {
        [HarmonyPatch(typeof(ExtendedCargoTruckAI), "GetLocalizedStatus")]
        [HarmonyPostfix]
        public static void GetLocalizedStatus(ExtendedCargoTruckAI __instance, ushort vehicleID, ref Vehicle data, ref InstanceID target, ref string __result)
        {
            if (VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
                if (vehicleNeeds.IsGoingToRefuel)
                {
                    target.Building = data.m_targetBuilding;
                    __result = "Driving to gas station ";
                }
                else if (vehicleNeeds.IsRefueling)
                {
                    target.Building = data.m_targetBuilding;
                    __result = "Fueling vehicle at gas station ";
                }
                else if (vehicleNeeds.IsGoingToHandWash || vehicleNeeds.IsGoingToTunnelWash)
                {
                    target.Building = data.m_targetBuilding;
                    __result = "Driving to car wash ";
                }
                else if (vehicleNeeds.IsAtHandWash || vehicleNeeds.IsAtTunnelWash)
                {
                    target.Building = data.m_targetBuilding;
                    __result = "Washing vehicle at car wash ";
                }
                else if (vehicleNeeds.IsGoingToGetRepaired)
                {
                    target.Building = data.m_targetBuilding;
                    __result = "Driving to mechanic ";
                }
                else if (vehicleNeeds.IsBeingRepaired)
                {
                    target.Building = data.m_targetBuilding;
                    __result = "Repairing vehicle at mechanic ";
                }
                if (data.m_targetBuilding == vehicleNeeds.OriginalTargetBuilding)
                {
                    target.Building = data.m_targetBuilding;
                    __result += " and " + Locale.Get("VEHICLE_STATUS_GOINGTO");
                }
            }
        }

        [HarmonyPatch(typeof(ExtendedCargoTruckAI), "SetTarget")]
        [HarmonyPrefix]
        public static bool SetTarget(ExtendedCargoTruckAI __instance, ushort vehicleID, ref Vehicle data, ushort targetBuilding)
        {
            if(targetBuilding == 0)
            {
                return true;
            }

            var buildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetBuilding].Info.GetAI();

            if (buildingAI is not GasStationAI && buildingAI is not GasPumpAI && buildingAI is not VehicleWashBuildingAI && buildingAI is not RepairStationAI)
            {
                return true; // Only allow setting target to gas station, gas pump, car wash or mechanic
            }

            if (VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                var original_targetBuilding = data.m_targetBuilding;

                VehicleNeedsManager.SetOriginalTargetBuilding(vehicleID, original_targetBuilding);

                data.m_targetBuilding = targetBuilding;

                var pathToRoadsideCareBuilding = CustomPathFindAI.CustomStartPathFind(vehicleID, ref data);
                if (!pathToRoadsideCareBuilding)
                {
                    VehicleNeedsManager.ClearAtLocationMode(vehicleID);
                    VehicleNeedsManager.ClearGoingToMode(vehicleID);
                    __instance.SetTarget(vehicleID, ref data, original_targetBuilding);
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(ExtendedCargoTruckAI), "ArriveAtTarget")]
        [HarmonyPrefix]
        public static bool ArriveAtTarget(ExtendedCargoTruckAI __instance, ushort vehicleID, ref Vehicle data, ref bool __result)
        {
            if (VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
                if (vehicleNeeds.IsGoingToRefuel || vehicleNeeds.IsGoingToHandWash || vehicleNeeds.IsAtTunnelWash)
                {
                    CarAIPatch.ArriveAtTarget(__instance, vehicleID, ref data);
                    __result = false;
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(ExtendedCargoTruckAI), "SimulationStep")]
        [HarmonyPostfix]
        public static void SimulationStep(ExtendedCargoTruckAI __instance, ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
        {
            if (VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);

                if (vehicleNeeds.IsRefueling || vehicleNeeds.IsAtHandWash || vehicleNeeds.IsGoingToTunnelWash || vehicleNeeds.IsAtTunnelWash)
                {
                    CarAIPatch.TakingCareOfVehicle(__instance, vehicleID, ref data);
                }
            }
        }

        [HarmonyPatch(typeof(ExtendedCargoTruckAI), "ExtendedStartTransfer")]
        [HarmonyPrefix]
        public static bool ExtendedStartTransfer(ExtendedCargoTruckAI __instance, ushort vehicleID, ref Vehicle data, ExtendedTransferManager.TransferReason material, ExtendedTransferManager.Offer offer)
        {
            if (VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                if(material == ExtendedTransferManager.TransferReason.VehicleFuel || material == ExtendedTransferManager.TransferReason.VehicleFuelElectric ||
                    material == ExtendedTransferManager.TransferReason.VehicleWash || material == ExtendedTransferManager.TransferReason.VehicleMinorRepair || 
                    material == ExtendedTransferManager.TransferReason.VehicleMajorRepair)
                {
                    __instance.SetTarget(vehicleID, ref data, offer.Building);
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(CargoTruckAI), "UpdateBuildingTargetPositions")]
        [HarmonyPrefix]
        public static bool UpdateBuildingTargetPositions(ExtendedCargoTruckAI __instance, ushort vehicleID, ref Vehicle vehicleData, Vector3 refPos, ushort leaderID, ref Vehicle leaderData, ref int index, float minSqrDistance)
        {
            if (VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
                bool isOnWayToCareCenter = vehicleNeeds.IsGoingToRefuel || vehicleNeeds.IsGoingToHandWash || vehicleNeeds.IsGoingToTunnelWash || vehicleNeeds.IsGoingToGetRepaired;
                bool isBeingCaredFor = vehicleNeeds.IsRefueling || vehicleNeeds.IsAtHandWash || vehicleNeeds.IsAtTunnelWash || vehicleNeeds.IsBeingRepaired;
                if (isOnWayToCareCenter || isBeingCaredFor)
                {
                    return false;
                }
            }
            return true;
        }

    }
}
