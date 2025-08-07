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
                    __result = "Driving to fuel at ";
                }
                else if (vehicleNeeds.IsRefueling)
                {
                    target.Building = data.m_targetBuilding;
                    __result = "Fueling vehicle at ";
                }
                else if (vehicleNeeds.IsGoingToGetWashed)
                {
                    target.Building = data.m_targetBuilding;
                    __result = "Driving to wash vehicle at ";
                }
                else if (vehicleNeeds.IsBeingWashed)
                {
                    target.Building = data.m_targetBuilding;
                    __result = "Washing vehicle at ";
                }
                else if (vehicleNeeds.IsGoingToGetRepaired)
                {
                    target.Building = data.m_targetBuilding;
                    __result = "Driving to repair vehicle at ";
                }
                else if (vehicleNeeds.IsBeingWashed)
                {
                    target.Building = data.m_targetBuilding;
                    __result = "Repairing vehicle at ";
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

            if ((buildingAI is GasStationAI || buildingAI is GasPumpAI) && VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
                if (vehicleNeeds.OriginalTargetBuilding == 0 && data.m_targetBuilding != 0)
                {
                    VehicleNeedsManager.SetOriginalTargetBuilding(vehicleID, data.m_targetBuilding);
                }
                data.m_targetBuilding = targetBuilding;
                var pathToRoadsideCareBuilding = CustomPathFindAI.CustomStartPathFind(vehicleID, ref data);
                if (!pathToRoadsideCareBuilding)
                {
                    data.m_targetBuilding = vehicleNeeds.OriginalTargetBuilding;
                    VehicleNeedsManager.SetNoneCareMode(vehicleID);
                    __instance.SetTarget(vehicleID, ref data, vehicleNeeds.OriginalTargetBuilding);
                    data.Unspawn(vehicleID);
                }
                else
                {
                    if (buildingAI is GasStationAI || buildingAI is GasPumpAI)
                    {
                        // Only set going to refuel if path was found
                        VehicleNeedsManager.SetIsGoingToRefuelMode(vehicleID);
                    }
                    else if (buildingAI is VehicleWashAI)
                    {
                        // Only set going to wash vehicle if path was found
                        VehicleNeedsManager.SetIsGoingToGetWashedMode(vehicleID);
                    }
                    else if (buildingAI is RepairStationAI)
                    {
                        // Only set going to fix vehicle if path was found
                        VehicleNeedsManager.SetIsGoingToGetRepairedMode(vehicleID);
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
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
                if (vehicleNeeds.IsGoingToRefuel)
                {
                    var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];

                    if ((building.Info.GetAI() is GasStationAI || building.Info.GetAI() is GasPumpAI) && Vector3.Distance(data.GetLastFramePosition(), building.m_position) < 80f)
                    {
                        data.m_custom = 0;
                        data.m_blockCounter = 0;
                        data.m_flags |= Vehicle.Flags.Stopped;
                        data.m_flags |= Vehicle.Flags.WaitingPath;
                        VehicleNeedsManager.SetIsRefuelingMode(vehicleID);
                        __result = false;
                        return false;
                    }
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
                if (vehicleNeeds.IsRefueling)
                {
                    data.m_custom++;
                    data.m_flags |= Vehicle.Flags.Stopped;
                    data.m_flags |= Vehicle.Flags.WaitingPath;
                    ref var building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];
                    var gasStationAI = building.Info.GetAI() as GasStationAI;
                    int RefuelingDurationInFrames = 10;
                    if (data.m_custom >= RefuelingDurationInFrames)
                    {
                        var neededFuel = (int)vehicleNeeds.FuelCapacity;
                        VehicleNeedsManager.SetFuelAmount(vehicleID, neededFuel);
                        FuelVehicle(vehicleID, ref data, gasStationAI, ref building, neededFuel);

                        VehicleNeedsManager.SetNoneCareMode(vehicleID);
                        var targetBuilding = vehicleNeeds.OriginalTargetBuilding;

                        data.m_flags &= ~Vehicle.Flags.Stopped;
                        data.m_flags &= ~Vehicle.Flags.WaitingPath;
                        VehicleNeedsManager.SetOriginalTargetBuilding(vehicleID, 0);
                        data.m_custom = 0;

                        __instance.SetTarget(vehicleID, ref data, targetBuilding);
                    }
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
                bool isOnWayToCareCenter = vehicleNeeds.IsGoingToRefuel || vehicleNeeds.IsGoingToGetWashed || vehicleNeeds.IsGoingToGetRepaired;
                bool isBeingCaredFor = vehicleNeeds.IsRefueling || vehicleNeeds.IsBeingWashed || vehicleNeeds.IsBeingRepaired;
                if (isOnWayToCareCenter || isBeingCaredFor)
                {
                    return false;
                }
            }
            return true;
        }

        private static void FuelVehicle(ushort vehicleID, ref Vehicle data, GasStationAI gasStationAI, ref Building building, int neededFuel)
        {
            ExtendedCargoTruckAI extendedCargoTruckAI = data.Info.GetAI() as ExtendedCargoTruckAI;
            if (extendedCargoTruckAI != null && !extendedCargoTruckAI.m_isElectric)
            {
                gasStationAI.ExtendedModifyMaterialBuffer(data.m_targetBuilding, ref building, ExtendedTransferManager.TransferReason.VehicleFuel, ref neededFuel);
            }
            Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.PublicIncome, 20, ItemClass.Service.Vehicles, ItemClass.SubService.None, ItemClass.Level.Level2);
        }

    }
}
