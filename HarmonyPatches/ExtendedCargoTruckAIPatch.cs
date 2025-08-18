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
        const float FRAMES_PER_UNIT = 3.2f;

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
                    VehicleNeedsManager.ClearAtLocationMode(vehicleID);
                    VehicleNeedsManager.ClearGoingToMode(vehicleID);
                    __instance.SetTarget(vehicleID, ref data, vehicleNeeds.OriginalTargetBuilding);
                    data.Unspawn(vehicleID);
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
                if (vehicleNeeds.IsGoingToRefuel || vehicleNeeds.IsGoingToHandWash)
                {
                    var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];
                    var distance = Vector3.Distance(data.GetLastFramePosition(), building.m_position);
                    if (distance < 80f && building.Info.GetAI() is GasStationAI || building.Info.GetAI() is GasPumpAI || building.Info.GetAI() is VehicleWashBuildingAI)
                    {
                        data.m_custom = 0;
                        data.m_blockCounter = 0;
                        data.m_flags |= Vehicle.Flags.Stopped;
                        data.m_flags |= Vehicle.Flags.WaitingPath;

                        if (vehicleNeeds.IsGoingToRefuel)
                        {
                            float fuelPerFrame = (vehicleNeeds.FuelCapacity - vehicleNeeds.FuelAmount) / 20; // RefuelingDurationInFrames = 20 turn to option for fuel timing
                            VehicleNeedsManager.SetFuelPerFrame(vehicleID, fuelPerFrame);
                            VehicleNeedsManager.SetIsRefuelingMode(vehicleID); // sets IsGoingToRefuel to false and IsRefueling to true
                        }
                        if (vehicleNeeds.IsGoingToHandWash)
                        {
                            float dirtPerFrame = (100 - vehicleNeeds.DirtPercentage) / 20; // DirtRemovalDurationInFrames = 20 turn to option for washing timing
                            VehicleNeedsManager.SetDirtPerFrame(vehicleID, dirtPerFrame);
                            VehicleNeedsManager.SetIsAtHandWashMode(vehicleID); // sets IsGoingToHandWash to false and IsAtHandWash to true
                        }
                        __result = false;
                        return false;
                    }
                }
                if (vehicleNeeds.IsAtTunnelWash)
                {
                    data.m_blockCounter = 0;
                    data.m_flags |= Vehicle.Flags.Stopped;
                    data.m_flags |= Vehicle.Flags.WaitingPath;
                    VehicleNeedsManager.SetIsAtTunnelWashExitMode(vehicleID);
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
                if (vehicleNeeds.IsRefueling || vehicleNeeds.IsAtHandWash)
                {
                    int durationInFrames = 20; // turn to option for fuel timing
                    TakingCareOfVehicle(__instance, vehicleID, ref data, durationInFrames);

                    if (vehicleNeeds.IsRefueling)
                    {
                        ref var building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];
                        var newFuelAmount = vehicleNeeds.FuelAmount + vehicleNeeds.FuelPerFrame;
                        VehicleNeedsManager.SetFuelAmount(vehicleID, newFuelAmount); // add fuel to car 
                        FuelVehicle(vehicleID, ref data, ref building, (int)newFuelAmount); // remove fuel for gas station or gas pump 
                    }
                }

                if (vehicleNeeds.IsGoingToTunnelWash)
                {
                    ref var building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];

                    float distance = Vector3.Distance(data.GetLastFramePosition(), building.m_position);

                    if (distance <= 80f)
                    {
                        byte b = data.m_pathPositionIndex;

                        PathManager.instance.m_pathUnits.m_buffer[data.m_path].GetPosition(b >> 1, out var position);

                        ref var segment = ref Singleton<NetManager>.instance.m_segments.m_buffer[position.m_segment];

                        if (segment.Info.GetAI() is VehicleWashLaneAI)
                        {
                            VehicleNeedsManager.SetIsAtTunnelWashMode(vehicleID);

                            float totalWashingFrames = segment.m_averageLength * FRAMES_PER_UNIT;

                            // The dirt to remove is calculated once and stored
                            float dirtToRemovePerFrame = vehicleNeeds.DirtPercentage / totalWashingFrames;

                            VehicleNeedsManager.SetDirtPerFrame(vehicleID, dirtToRemovePerFrame);
                        }
                    }
                }

                if (vehicleNeeds.IsAtTunnelWash)
                {
                    ref var building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];

                    float distance = Vector3.Distance(data.GetLastFramePosition(), building.m_position);

                    if (distance > 80f)
                    {
                        VehicleNeedsManager.ClearAtLocationMode(vehicleID);
                        return;
                    }

                    if (vehicleNeeds.IsAtTunnelWashExit)
                    {
                        data.m_flags |= Vehicle.Flags.Stopped;
                        data.m_flags |= Vehicle.Flags.WaitingPath;
                        data.m_blockCounter = 0;
                    }

                    float dirtToRemoveThisFrame = vehicleNeeds.DirtPerFrame;

                    // This is the check that will prevent the number from going negative
                    float dirtLevel = Mathf.Max(0, vehicleNeeds.DirtPercentage - dirtToRemoveThisFrame);

                    VehicleNeedsManager.SetDirtPercentage(vehicleID, dirtLevel);

                    if (dirtLevel <= 0)
                    {
                        VehicleNeedsManager.ClearAtLocationMode(vehicleID);
                        var targetBuilding = vehicleNeeds.OriginalTargetBuilding;

                        data.m_flags &= ~Vehicle.Flags.WaitingPath;
                        VehicleNeedsManager.SetOriginalTargetBuilding(vehicleID, 0);

                        var citizenId = __instance.GetOwnerID(vehicleID, ref data).Citizen;
                        ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];
                        var citizenInstance = Singleton<CitizenManager>.instance.m_instances.m_buffer[citizen.m_instance];
                        var humanAI = citizen.GetCitizenInfo(citizenId).GetAI() as HumanAI;
                        humanAI.StartMoving(citizenId, ref citizen, citizenInstance.m_targetBuilding, targetBuilding);
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
                    material == ExtendedTransferManager.TransferReason.VehicleLargeWash || material == ExtendedTransferManager.TransferReason.VehicleLargeMinorRepair || 
                    material == ExtendedTransferManager.TransferReason.VehicleLargeMajorRepair)
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

        private static void FuelVehicle(ushort vehicleID, ref Vehicle data, ref Building building, int neededFuel)
        {
            ExtendedCargoTruckAI extendedCargoTruckAI = data.Info.GetAI() as ExtendedCargoTruckAI;
            if (extendedCargoTruckAI != null && !extendedCargoTruckAI.m_isElectric)
            {
                if (building.Info.GetAI() is GasPumpAI gasPumpAI)
                {
                    gasPumpAI.ExtendedModifyMaterialBuffer(data.m_targetBuilding, ref building, ExtendedTransferManager.TransferReason.VehicleFuel, ref neededFuel);
                }
                if (building.Info.GetAI() is GasStationAI gasStationAI)
                {
                    gasStationAI.ExtendedModifyMaterialBuffer(data.m_targetBuilding, ref building, ExtendedTransferManager.TransferReason.VehicleFuel, ref neededFuel);
                }
            }
            Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.PublicIncome, 20, ItemClass.Service.Vehicles, ItemClass.SubService.None, ItemClass.Level.Level2);
        }

        private static void TakingCareOfVehicle(ExtendedCargoTruckAI instance, ushort vehicleID, ref Vehicle data, int durationInFrames)
        {
            var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
            data.m_custom++;
            data.m_flags |= Vehicle.Flags.Stopped;
            data.m_flags |= Vehicle.Flags.WaitingPath;
            data.m_blockCounter = 0;

            if (data.m_custom >= durationInFrames)
            {
                VehicleNeedsManager.ClearAtLocationMode(vehicleID);
                var targetBuilding = vehicleNeeds.OriginalTargetBuilding;

                data.m_flags &= ~Vehicle.Flags.Stopped;
                data.m_flags &= ~Vehicle.Flags.WaitingPath;
                VehicleNeedsManager.SetOriginalTargetBuilding(vehicleID, 0);
                data.m_custom = 0;

                instance.SetTarget(vehicleID, ref data, targetBuilding);
            }
        }

    }
}
