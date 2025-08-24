using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using MoreTransferReasons.AI;
using RoadsideCare.Managers;
using UnityEngine;

namespace RoadsideCare.AI
{
    public class CustomPathFindAI : CargoTruckAI
    {
        private delegate bool StartPathFindCargoTruckAIDelegate(CargoTruckAI instance, ushort vehicleID, ref Vehicle vehicleData, Vector3 startPos, Vector3 endPos, bool startBothWays, bool endBothWays, bool undergroundTarget);
        private static readonly StartPathFindCargoTruckAIDelegate StartPathFindCargoTruckAI = AccessTools.MethodDelegate<StartPathFindCargoTruckAIDelegate>(typeof(CargoTruckAI).GetMethod("StartPathFind", BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(Vector3), typeof(Vector3), typeof(bool), typeof(bool), typeof(bool)], null), null, false);

        public static bool CustomStartPathFind(ushort vehicleID, ref Vehicle vehicleData)
        {
            var m_info = vehicleData.Info;
            if ((vehicleData.m_flags & Vehicle.Flags.WaitingTarget) != 0)
            {
                return true;
            }
            if ((vehicleData.m_flags & Vehicle.Flags.GoingBack) != 0)
            {
                if (vehicleData.m_sourceBuilding != 0)
                {
                    BuildingManager instance = Singleton<BuildingManager>.instance;
                    BuildingInfo info = instance.m_buildings.m_buffer[vehicleData.m_sourceBuilding].Info;

                    if (info.GetAI() is GasStationAI && GasStationManager.GasStationBuildingExist(vehicleData.m_targetBuilding))
                    {
                        Randomizer randomizer2 = new(vehicleID);
                        info.m_buildingAI.CalculateUnspawnPosition(vehicleData.m_targetBuilding, ref instance.m_buildings.m_buffer[vehicleData.m_targetBuilding], ref randomizer2, m_info, out Vector3 b, out Vector3 target2);
                        return StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, target2, true, true, false);
                    }
                    if (info.GetAI() is GasPumpAI && GasStationManager.GasStationBuildingExist(vehicleData.m_targetBuilding))
                    {
                        if (TryFindRandomGasPumpPoint(vehicleID, ref vehicleData, vehicleData.m_targetBuilding, out Vector3 fuelPointTargetPos))
                        {
                            return StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, fuelPointTargetPos, true, true, false);
                        }
                    }
                    if (info.GetAI() is VehicleWashBuildingAI && VehicleWashBuildingManager.VehicleWashBuildingExist(vehicleData.m_targetBuilding))
                    {
                        if (TryFindRandomVehicleWashPoint(vehicleID, ref vehicleData, vehicleData.m_targetBuilding, out Vector3 vehicleWashPointTargetPos))
                        {
                            var result = StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, vehicleWashPointTargetPos, true, true, false);
                            if (result)
                            {
                                return result;
                            }
                            else
                            {
                                if (TryFindRandomVehicleTunnelWash(vehicleID, ref vehicleData, vehicleData.m_targetBuilding, out Vector3 vehicleWashLaneTargetPos))
                                {
                                    return StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, vehicleWashLaneTargetPos, true, true, false);
                                }
                                return false;
                            }
                        }
                        else
                        {
                            if (TryFindRandomVehicleTunnelWash(vehicleID, ref vehicleData, vehicleData.m_targetBuilding, out Vector3 vehicleWashLaneTargetPos))
                            {
                                return StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, vehicleWashLaneTargetPos, true, true, false);
                            }
                            return false;
                        }
                    }
                }
            }
            else if (vehicleData.m_targetBuilding != 0)
            {
                BuildingManager instance2 = Singleton<BuildingManager>.instance;
                BuildingInfo info2 = instance2.m_buildings.m_buffer[vehicleData.m_targetBuilding].Info;

                if (info2.GetAI() is GasStationAI && GasStationManager.GasStationBuildingExist(vehicleData.m_targetBuilding))
                {
                    Randomizer randomizer2 = new(vehicleID);
                    info2.m_buildingAI.CalculateUnspawnPosition(vehicleData.m_targetBuilding, ref instance2.m_buildings.m_buffer[vehicleData.m_targetBuilding], ref randomizer2, m_info, out Vector3 b, out Vector3 target2);
                    return StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, target2, true, true, false);
                }
                if (info2.GetAI() is GasPumpAI && GasStationManager.GasStationBuildingExist(vehicleData.m_targetBuilding))
                {
                    if(TryFindRandomGasPumpPoint(vehicleID, ref vehicleData, vehicleData.m_targetBuilding, out Vector3 fuelPointTargetPos))
                    {
                        return StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, fuelPointTargetPos, true, true, false);
                    }
                }
                if (info2.GetAI() is VehicleWashBuildingAI && VehicleWashBuildingManager.VehicleWashBuildingExist(vehicleData.m_targetBuilding))
                {
                    if (TryFindRandomVehicleWashPoint(vehicleID, ref vehicleData, vehicleData.m_targetBuilding, out Vector3 vehicleWashPointTargetPos))
                    {
                        var result = StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, vehicleWashPointTargetPos, true, true, false);
                        if (result)
                        {
                            return result;
                        }
                        else
                        {
                            if (TryFindRandomVehicleTunnelWash(vehicleID, ref vehicleData, vehicleData.m_targetBuilding, out Vector3 vehicleWashLaneTargetPos))
                            {
                                return StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, vehicleWashLaneTargetPos, true, true, false);
                            }
                            return false;
                        }
                    }
                    else
                    {
                        if (TryFindRandomVehicleTunnelWash(vehicleID, ref vehicleData, vehicleData.m_targetBuilding, out Vector3 vehicleWashLaneTargetPos))
                        {
                            return StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, vehicleWashLaneTargetPos, true, true, false);
                        }
                        return false;
                    }
                }
            }
            return false;
        }

        private static bool TryFindRandomGasPumpPoint(ushort vehicleID, ref Vehicle vehicleData, ushort buildingID, out Vector3 targetPos)
        {
            targetPos = Vector3.zero;
            var gasStation = GasStationManager.GetGasStationBuilding(buildingID);

            if (gasStation.FuelPoints == null || gasStation.FuelPoints.Count == 0)
            {
                return false;
            }

            var fuelPoints = new List<ushort>();

            foreach (ushort point in gasStation.FuelPoints)
            {
                var netAI = NetManager.instance.m_segments.m_buffer[point].Info.m_netAI;
                if (vehicleData.Info.GetAI() is PassengerCarAI && (netAI is FuelPointSmallAI || netAI is FuelPointAI))
                {
                    fuelPoints.Add(point);
                }
                else if (vehicleData.Info.GetAI() is ExtendedCargoTruckAI && (netAI is FuelPointLargeAI || netAI is FuelPointAI))
                {
                    fuelPoints.Add(point);
                }
            }

            if (fuelPoints.Count == 0)
            {
                return false;
            }

            // Pick a random segment from the fuel points
            ushort segmentId = fuelPoints[Random.Range(0, fuelPoints.Count)];
            var laneId = NetManager.instance.m_segments.m_buffer[segmentId].m_lanes; // single lane index 0

            if (laneId == 0) return false;

            // Get lane position for pathfinding target
            targetPos = NetManager.instance.m_lanes.m_buffer[laneId].CalculatePosition(0.5f); // middle of lane
            VehicleNeedsManager.SetIsGoingToRefuelMode(vehicleID);
            return true;
        }

        private static bool TryFindRandomVehicleWashPoint(ushort vehicleID, ref Vehicle vehicleData, ushort buildingID, out Vector3 targetPos)
        {
            targetPos = Vector3.zero;
            var vehicleWashBuilding = VehicleWashBuildingManager.GetVehicleWashBuilding(buildingID);

            if (vehicleWashBuilding.VehicleWashPoints == null || vehicleWashBuilding.VehicleWashPoints.Count == 0)
            {
                return false;
            }

            var vehicleWashPoints = new List<ushort>();

            foreach (ushort point in vehicleWashBuilding.VehicleWashPoints)
            {
                var netAI = NetManager.instance.m_segments.m_buffer[point].Info.m_netAI;
                if (vehicleData.Info.GetAI() is PassengerCarAI && (netAI is VehicleWashPointSmallAI || netAI is VehicleWashPointAI))
                {
                    vehicleWashPoints.Add(point);
                }
                else if (vehicleData.Info.GetAI() is ExtendedCargoTruckAI && (netAI is VehicleWashPointLargeAI || netAI is VehicleWashPointAI))
                {
                    vehicleWashPoints.Add(point);
                }
            }

            if (vehicleWashPoints.Count == 0)
            {
                return false;
            }

            // Pick a random segment from the vehicle wash points
            ushort segmentId = vehicleWashPoints[Random.Range(0, vehicleWashPoints.Count)];
            var laneId = NetManager.instance.m_segments.m_buffer[segmentId].m_lanes; // single lane index 0

            if (laneId == 0) return false;

            // Get lane position for pathfinding target
            targetPos = NetManager.instance.m_lanes.m_buffer[laneId].CalculatePosition(0.5f); // middle of lane
            VehicleNeedsManager.SetIsGoingToHandWashMode(vehicleID);
            return true;
        }

        private static bool TryFindRandomVehicleTunnelWash(ushort vehicleID, ref Vehicle vehicleData, ushort buildingID, out Vector3 targetPos)
        {
            targetPos = Vector3.zero;
            var vehicleWashBuilding = VehicleWashBuildingManager.GetVehicleWashBuilding(buildingID);

            if (vehicleWashBuilding.VehicleWashLanes == null || vehicleWashBuilding.VehicleWashLanes.Count == 0)
            {
                return false;
            }

            var vehicleWashLanes = new List<ushort>();

            foreach (ushort point in vehicleWashBuilding.VehicleWashPoints)
            {
                var netAI = NetManager.instance.m_segments.m_buffer[point].Info.m_netAI;
                if (vehicleData.Info.GetAI() is PassengerCarAI && (netAI is VehicleWashLaneSmallAI || netAI is VehicleWashLaneAI))
                {
                    vehicleWashLanes.Add(point);
                }
                else if (vehicleData.Info.GetAI() is ExtendedCargoTruckAI && (netAI is VehicleWashLaneLargeAI || netAI is VehicleWashLaneAI))
                {
                    vehicleWashLanes.Add(point);
                }
            }

            if (vehicleWashLanes.Count == 0)
            {
                return false;
            }

            // Pick a random segment from the vehicle wash lanes
            ushort segmentId = vehicleWashLanes[Random.Range(0, vehicleWashLanes.Count)];
            var laneId = NetManager.instance.m_segments.m_buffer[segmentId].m_lanes; // single lane index 0

            if (laneId == 0) return false;

            // Get lane position for pathfinding target
            targetPos = NetManager.instance.m_lanes.m_buffer[laneId].CalculatePosition(1f); // end of lane
            VehicleNeedsManager.SetIsGoingToHandWashMode(vehicleID);
            return true;
        }

    }
}
