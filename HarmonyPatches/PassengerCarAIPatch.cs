using System;
using ColossalFramework;
using ColossalFramework.Globalization;
using HarmonyLib;
using MoreTransferReasons;
using RoadsideCare.AI;
using RoadsideCare.Managers;
using UnityEngine;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
    public static class PassengerCarAIPatch
    {
        const float FRAMES_PER_UNIT = 3.2f;

        [HarmonyPatch(typeof(PassengerCarAI), "CanLeave")]
        [HarmonyPrefix]
        public static bool CanLeave(PassengerCarAI __instance, ushort vehicleID, ref Vehicle vehicleData, ref bool __result)
        {
            if (vehicleData.m_sourceBuilding == 0)
            {
                return true;
            }

            if (VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
                if (vehicleNeeds.IsRefueling || vehicleNeeds.IsAtHandWash || vehicleNeeds.IsAtTunnelWash || vehicleNeeds.IsBeingRepaired)
                {
                    __result = false;
                    return false;
                }
            }

            return true;
        }

        [HarmonyPatch(typeof(PassengerCarAI), "ParkVehicle")]
        [HarmonyPostfix]
        public static void ParkVehiclePostfix(PassengerCarAI __instance, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint nextPath, int nextPositionIndex, ref byte segmentOffset)
        {
            var citizenId = __instance.GetOwnerID(vehicleID, ref vehicleData).Citizen;
            ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];

            if (citizen.m_parkedVehicle != 0 && VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
                VehicleNeedsManager.CreateParkedVehicleNeeds(citizen.m_parkedVehicle, citizenId, vehicleNeeds.FuelAmount, vehicleNeeds.FuelCapacity, vehicleNeeds.DirtPercentage, 
                    vehicleNeeds.WearPercentage, vehicleNeeds.IsBroken, vehicleNeeds.IsOutOfFuel);
                VehicleNeedsManager.RemoveVehicleNeeds(vehicleID);
            }
        }

        [HarmonyPatch(typeof(PassengerCarAI), "GetLocalizedStatus", [typeof(ushort), typeof(Vehicle), typeof(InstanceID)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref])]
        [HarmonyPostfix]
        public static void GetLocalizedStatus(PassengerCarAI __instance, ushort vehicleID, ref Vehicle data, ref InstanceID target, ref string __result)
        {
            var citizenId = __instance.GetOwnerID(vehicleID, ref data).Citizen;
            ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];
            var citizenInstance = Singleton<CitizenManager>.instance.m_instances.m_buffer[citizen.m_instance];
            if (VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
                if (vehicleNeeds.IsGoingToRefuel)
                {
                    target.Building = citizenInstance.m_targetBuilding;
                    __result = "Driving to gas station ";
                }
                else if (vehicleNeeds.IsRefueling)
                {
                    target.Building = citizenInstance.m_targetBuilding;
                    __result = "Fueling vehicle at gas station ";
                }
                else if (vehicleNeeds.IsGoingToHandWash || vehicleNeeds.IsGoingToTunnelWash)
                {
                    target.Building = citizenInstance.m_targetBuilding;
                    __result = "Driving to car wash ";
                }
                else if (vehicleNeeds.IsAtHandWash || vehicleNeeds.IsAtTunnelWash)
                {
                    target.Building = citizenInstance.m_targetBuilding;
                    __result = "Washing vehicle at car wash ";
                }
                else if (vehicleNeeds.IsGoingToGetRepaired)
                {
                    target.Building = citizenInstance.m_targetBuilding;
                    __result = "Driving to mechanic ";
                }
                else if (vehicleNeeds.IsBeingRepaired)
                {
                    target.Building = citizenInstance.m_targetBuilding;
                    __result = "Repairing vehicle at mechanic ";
                }
                if (citizenInstance.m_targetBuilding == vehicleNeeds.OriginalTargetBuilding)
                {
                    target.Building = citizenInstance.m_targetBuilding;
                    __result += " and " + Locale.Get("VEHICLE_STATUS_GOINGTO");
                }
            }
        }

        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(PassengerCarAI), "SetTarget")]
        [HarmonyPrefix]
        public static bool SetTarget(ref PassengerCarAI __instance, ushort vehicleID, ref Vehicle data, ushort targetBuilding)
        {
            if(VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                var citizenId = GetOwnerID(vehicleID, ref data).Citizen;
                if (citizenId != 0)
                {
                    var citizenInstanceId = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId].m_instance;
                    if (citizenInstanceId != 0)
                    {
                        ref var citizenInstance = ref Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId];

                        data.m_targetBuilding = citizenInstance.m_targetBuilding;

                        var buildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenInstance.m_targetBuilding].Info.GetAI();
                        if (buildingAI is not GasStationAI && buildingAI is not GasPumpAI && buildingAI is not VehicleWashBuildingAI && buildingAI is not RepairStationAI)
                        {
                            return true; // Only allow setting target to gas station, gas pump, car wash or mechanic
                        }
                        var pathToRoadsideCareBuilding = CustomPathFindAI.CustomStartPathFind(vehicleID, ref data);
                        var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
                        if (!pathToRoadsideCareBuilding)
                        {
                            data.m_targetBuilding = 0;
                            VehicleNeedsManager.ClearAtLocationMode(vehicleID);
                            VehicleNeedsManager.ClearGoingToMode(vehicleID);
                            citizenInstance.m_targetBuilding = vehicleNeeds.OriginalTargetBuilding;
                            __instance.SetTarget(vehicleID, ref data, vehicleNeeds.OriginalTargetBuilding);
                            data.Unspawn(vehicleID);
                        }
                        return false;
                    }
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(PassengerCarAI), "ArriveAtTarget")]
        [HarmonyPrefix]
        public static bool ArriveAtTarget(PassengerCarAI __instance, ushort vehicleID, ref Vehicle data, ref bool __result)
        {
            if (VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
                if (vehicleNeeds.IsGoingToRefuel || vehicleNeeds.IsGoingToHandWash)
                {
                    var citizenId = __instance.GetOwnerID(vehicleID, ref data).Citizen;
                    var citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];
                    var citizenInstance = Singleton<CitizenManager>.instance.m_instances.m_buffer[citizen.m_instance];
                    var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[citizenInstance.m_targetBuilding];

                    var distance = Vector3.Distance(data.GetLastFramePosition(), building.m_position);
                    if(distance < 80f && building.Info.GetAI() is GasStationAI || building.Info.GetAI() is GasPumpAI || building.Info.GetAI() is VehicleWashBuildingAI)
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

        [HarmonyPatch(typeof(PassengerCarAI), "SimulationStep", [typeof(ushort), typeof(Vehicle), typeof(Vector3)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal])]
        [HarmonyPostfix]
        public static void SimulationStep(PassengerCarAI __instance, ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
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

                    if(vehicleNeeds.IsAtTunnelWashExit)
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

        private static void FuelVehicle(ushort vehicleID, ref Vehicle data, ref Building building, int neededFuel)
        {
            bool isElectric = data.Info.m_class.m_subService != ItemClass.SubService.ResidentialLow;
            if (!isElectric)
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

        private static InstanceID GetOwnerID(ushort vehicleID, ref Vehicle vehicleData)
        {
            InstanceID result = default;
            ushort driverInstance = GetDriverInstance(vehicleID, ref vehicleData);
            if (driverInstance != 0)
            {
                result.Citizen = Singleton<CitizenManager>.instance.m_instances.m_buffer[driverInstance].m_citizen;
            }
            return result;
        }

        private static ushort GetDriverInstance(ushort vehicleID, ref Vehicle data)
        {
            CitizenManager instance = Singleton<CitizenManager>.instance;
            uint num = data.m_citizenUnits;
            int num2 = 0;
            while (num != 0)
            {
                uint nextUnit = instance.m_units.m_buffer[num].m_nextUnit;
                for (int i = 0; i < 5; i++)
                {
                    uint citizen = instance.m_units.m_buffer[num].GetCitizen(i);
                    if (citizen != 0)
                    {
                        ushort instance2 = instance.m_citizens.m_buffer[citizen].m_instance;
                        if (instance2 != 0)
                        {
                            return instance2;
                        }
                    }
                }
                num = nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            return 0;
        }

        private static void TakingCareOfVehicle(PassengerCarAI instance, ushort vehicleID, ref Vehicle data, int durationInFrames)
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

                var citizenId = instance.GetOwnerID(vehicleID, ref data).Citizen;
                ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];
                var citizenInstance = Singleton<CitizenManager>.instance.m_instances.m_buffer[citizen.m_instance];
                var humanAI = citizen.GetCitizenInfo(citizenId).GetAI() as HumanAI;
                humanAI.StartMoving(citizenId, ref citizen, citizenInstance.m_targetBuilding, targetBuilding);
            }
        }
    }
}
