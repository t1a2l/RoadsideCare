using System;
using ColossalFramework;
using ColossalFramework.Globalization;
using HarmonyLib;
using RoadsideCare.AI;
using RoadsideCare.Managers;
using UnityEngine;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
    public static class PassengerCarAIPatch
    {
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
                if (VehicleNeedsManager.IsRefueling(vehicleID) || VehicleNeedsManager.IsAtHandWash(vehicleID) || VehicleNeedsManager.IsAtTunnelWash(vehicleID) || VehicleNeedsManager.IsBeingRepaired(vehicleID))
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
                    vehicleNeeds.WearPercentage);
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
                if (VehicleNeedsManager.IsGoingToRefuel(vehicleID))
                {
                    target.Building = citizenInstance.m_targetBuilding;
                    __result = "Driving to gas station ";
                }
                else if (VehicleNeedsManager.IsRefueling(vehicleID))
                {
                    target.Building = citizenInstance.m_targetBuilding;
                    __result = "Fueling vehicle at gas station ";
                }
                else if (VehicleNeedsManager.IsGoingToHandWash(vehicleID) || VehicleNeedsManager.IsGoingToTunnelWash(vehicleID))
                {
                    target.Building = citizenInstance.m_targetBuilding;
                    __result = "Driving to car wash ";
                }
                else if (VehicleNeedsManager.IsAtHandWash(vehicleID) || VehicleNeedsManager.IsAtTunnelWash(vehicleID))
                {
                    target.Building = citizenInstance.m_targetBuilding;
                    __result = "Washing vehicle at car wash ";
                }
                else if (VehicleNeedsManager.IsGoingToGetRepaired(vehicleID))
                {
                    target.Building = citizenInstance.m_targetBuilding;
                    __result = "Driving to mechanic ";
                }
                else if (VehicleNeedsManager.IsBeingRepaired(vehicleID))
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
            if (data.m_custom == 0)
            {
                return true;
            }
            if (VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                var citizenId = GetOwnerID(vehicleID, ref data).Citizen;
                if (citizenId != 0)
                {
                    var citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];

                    var citizenInstanceId = citizen.m_instance;
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
                            VehicleNeedsManager.ClearAtLocationMode(vehicleID);
                            VehicleNeedsManager.ClearGoingToMode(vehicleID);
                            citizenInstance.m_targetBuilding = vehicleNeeds.OriginalTargetBuilding;
                            var humanAI = citizen.GetCitizenInfo(citizenId).GetAI() as HumanAI;
                            humanAI.SetTarget(citizenInstanceId, ref citizenInstance, vehicleNeeds.OriginalTargetBuilding);
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
                if (VehicleNeedsManager.IsGoingToRefuel(vehicleID) || VehicleNeedsManager.IsGoingToHandWash(vehicleID) || VehicleNeedsManager.IsAtTunnelWash(vehicleID))
                {
                    HandleRoadSideCareManager.ArriveAtRoadCareBuilding(vehicleID, ref data);
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
                if (VehicleNeedsManager.IsRefueling(vehicleID) || VehicleNeedsManager.IsAtHandWash(vehicleID) || VehicleNeedsManager.IsGoingToTunnelWash(vehicleID) || VehicleNeedsManager.IsAtTunnelWash(vehicleID))
                {
                    HandleRoadSideCareManager.TakingCareOfVehicle(__instance, vehicleID, ref data);
                }
            }
        }

        [HarmonyPatch(typeof(PassengerCarAI), "UpdateParkedVehicle")]
        [HarmonyPostfix]
        public static void UpdateParkedVehicle(ushort parkedID, ref VehicleParked parkedData)
        {
            if (VehicleNeedsManager.ParkedVehicleNeedsExist(parkedID))
            {
                var vehicleNeeds = VehicleNeedsManager.GetParkedVehicleNeeds(parkedID);
                if (vehicleNeeds.DirtPercentage < 100)
                {
                    var newDirtPercentage = vehicleNeeds.DirtPercentage + 0.01f;
                    VehicleNeedsManager.SetParkedDirtPercentage(parkedID, newDirtPercentage);
                }
            }
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

    }
}
