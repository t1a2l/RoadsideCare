using ColossalFramework;
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

                ushort buildingId = 0;

                var sourceBuildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_sourceBuilding].Info.GetAI();
                var targetBuildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding].Info.GetAI();

                if ((data.m_flags & Vehicle.Flags.GoingBack) != 0 && (sourceBuildingAI is GasStationAI || sourceBuildingAI is GasPumpAI || sourceBuildingAI is VehicleWashBuildingAI || sourceBuildingAI is RepairStationAI))
                {
                    buildingId = data.m_sourceBuilding;
                }
                if ((data.m_flags & Vehicle.Flags.GoingBack) == 0 &&  (targetBuildingAI is GasStationAI || targetBuildingAI is GasPumpAI || targetBuildingAI is VehicleWashBuildingAI || targetBuildingAI is RepairStationAI))
                {
                    buildingId = data.m_targetBuilding;
                }
                if(buildingId == 0)
                {
                    return;
                }

                if (VehicleNeedsManager.IsGoingToRefuel(vehicleID))
                {
                    target.Building = buildingId;
                    __result = "Driving to gas station ";
                }
                else if (VehicleNeedsManager.IsRefueling(vehicleID))
                {
                    target.Building = buildingId;
                    __result = "Fueling vehicle at gas station ";
                }
                else if (VehicleNeedsManager.IsGoingToHandWash(vehicleID) || VehicleNeedsManager.IsGoingToTunnelWash(vehicleID))
                {
                    target.Building = buildingId;
                    __result = "Driving to truck wash ";
                }
                else if (VehicleNeedsManager.IsAtHandWash(vehicleID) || VehicleNeedsManager.IsAtTunnelWash(vehicleID))
                {
                    target.Building = buildingId;
                    __result = "Washing vehicle at truck wash ";
                }
                else if (VehicleNeedsManager.IsGoingToGetRepaired(vehicleID))
                {
                    target.Building = buildingId;
                    __result = "Driving to mechanic ";
                }
                else if (VehicleNeedsManager.IsBeingRepaired(vehicleID))
                {
                    target.Building = buildingId;
                    __result = "Repairing vehicle at mechanic ";
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

            if(data.m_custom == 0)
            {
                return true;
            }

            var buildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetBuilding].Info.GetAI();

            if ((buildingAI is GasStationAI || buildingAI is GasPumpAI) && data.m_transferType >= 200 && data.m_transferType != 255)
            {
                return true;
            }

            if (buildingAI is not GasStationAI && buildingAI is not GasPumpAI && buildingAI is not VehicleWashBuildingAI && buildingAI is not RepairStationAI)
            {
                return true; // Only allow setting target to gas station, gas pump, car wash or mechanic
            }

            if (VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                ushort original_targetBuilding;
                if ((data.m_flags & Vehicle.Flags.GoingBack) != 0)
                {
                    original_targetBuilding = data.m_sourceBuilding;
                    data.m_sourceBuilding = targetBuilding;
                }
                else
                {
                    original_targetBuilding = data.m_targetBuilding;
                    data.m_targetBuilding = targetBuilding;
                }
                VehicleNeedsManager.SetOriginalTargetBuilding(vehicleID, original_targetBuilding);

                var pathToRoadsideCareBuilding = CustomPathFindAI.CustomStartPathFind(vehicleID, ref data);
                if (!pathToRoadsideCareBuilding)
                {
                    VehicleNeedsManager.ClearAtLocationMode(vehicleID);
                    VehicleNeedsManager.ClearGoingToMode(vehicleID);
                    data.m_custom = 0;
                    if ((data.m_flags & Vehicle.Flags.GoingBack) != 0)
                    {
                        data.m_sourceBuilding = original_targetBuilding;
                        __instance.SetTarget(vehicleID, ref data, 0);
                    }
                    else
                    {
                        __instance.SetTarget(vehicleID, ref data, original_targetBuilding);
                    }  
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
                if (VehicleNeedsManager.IsGoingToRefuel(vehicleID) || VehicleNeedsManager.IsGoingToHandWash(vehicleID) || VehicleNeedsManager.IsAtTunnelWash(vehicleID))
                {
                    HandleRoadSideCareManager.ArriveAtTarget(__instance, vehicleID, ref data);
                    __result = false;
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(ExtendedCargoTruckAI), "ArriveAtSource")]
        [HarmonyPrefix]
        public static bool ArriveAtSource(ExtendedCargoTruckAI __instance, ushort vehicleID, ref Vehicle data, ref bool __result)
        {
            if (VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                if (VehicleNeedsManager.IsGoingToRefuel(vehicleID) || VehicleNeedsManager.IsGoingToHandWash(vehicleID) || VehicleNeedsManager.IsAtTunnelWash(vehicleID))
                {
                    HandleRoadSideCareManager.ArriveAtTarget(__instance, vehicleID, ref data);
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
                if (VehicleNeedsManager.IsRefueling(vehicleID) || VehicleNeedsManager.IsAtHandWash(vehicleID) || VehicleNeedsManager.IsGoingToTunnelWash(vehicleID) || VehicleNeedsManager.IsAtTunnelWash(vehicleID))
                {
                    HandleRoadSideCareManager.TakingCareOfVehicle(__instance, vehicleID, ref data);
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
                    data.m_custom = (ushort)material;
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
                bool isOnWayToCareCenter = VehicleNeedsManager.IsGoingToRefuel(vehicleID) || VehicleNeedsManager.IsGoingToHandWash(vehicleID) || VehicleNeedsManager.IsGoingToTunnelWash(vehicleID) || VehicleNeedsManager.IsGoingToGetRepaired(vehicleID);
                bool isBeingCaredFor = VehicleNeedsManager.IsRefueling(vehicleID) || VehicleNeedsManager.IsAtHandWash(vehicleID) || VehicleNeedsManager.IsAtTunnelWash(vehicleID) || VehicleNeedsManager.IsBeingRepaired(vehicleID);
                if (isOnWayToCareCenter || isBeingCaredFor)
                {
                    return false;
                }
            }
            return true;
        }

    }
}
