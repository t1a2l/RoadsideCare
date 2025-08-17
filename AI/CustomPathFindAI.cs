using System.Reflection;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
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
                    if (TryGetRandomLaneTarget(vehicleData.m_sourceBuilding, out Vector3 targetPos))
                    {
                        // Use fuel lane position as path target
                        return StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, targetPos, true, true, false);
                    }
                    else
                    {
                        BuildingManager instance = Singleton<BuildingManager>.instance;
                        BuildingInfo info = instance.m_buildings.m_buffer[vehicleData.m_sourceBuilding].Info;
                        Randomizer randomizer = new(vehicleID);
                        info.m_buildingAI.CalculateUnspawnPosition(vehicleData.m_sourceBuilding, ref instance.m_buildings.m_buffer[vehicleData.m_sourceBuilding], ref randomizer, m_info, out Vector3 a, out Vector3 target);
                        return StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, target, true, true, false);
                    }   
                }
            }
            else if (vehicleData.m_targetBuilding != 0)
            {
                if (TryGetRandomLaneTarget(vehicleData.m_targetBuilding, out Vector3 targetPos))
                {
                    // Use random lane position as path target
                    return StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, targetPos, true, true, false);
                }
                else
                {
                    BuildingManager instance2 = Singleton<BuildingManager>.instance;
                    BuildingInfo info2 = instance2.m_buildings.m_buffer[vehicleData.m_targetBuilding].Info;
                    Randomizer randomizer2 = new(vehicleID);
                    info2.m_buildingAI.CalculateUnspawnPosition(vehicleData.m_targetBuilding, ref instance2.m_buildings.m_buffer[vehicleData.m_targetBuilding], ref randomizer2, m_info, out Vector3 b, out Vector3 target2);
                    return StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, target2, true, true, false);
                } 
            }
            return false;
        }

        public static bool TryGetRandomLaneTarget(ushort buildingID, out Vector3 targetPos)
        {
            targetPos = Vector3.zero;
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingID];
            if ((building.m_flags & Building.Flags.Created) == 0) return false;

            if(building.Info.m_buildingAI is GasPumpAI && GasStationManager.GasStationBuildingExist(buildingID))
            {
                var gasStation = GasStationManager.GetGasStationBuilding(buildingID);

                if (gasStation.FuelLanes == null || gasStation.FuelLanes.Count == 0)
                {
                    return false;
                }

                // Pick a random segment from the fuel lanes
                ushort segmentId = gasStation.FuelLanes[Random.Range(0, gasStation.FuelLanes.Count)];
                var laneId = NetManager.instance.m_segments.m_buffer[segmentId].m_lanes; // single lane index 0

                if (laneId == 0) return false;

                // Get lane position for pathfinding target
                targetPos = NetManager.instance.m_lanes.m_buffer[laneId].CalculatePosition(0.5f); // middle of lane
                return true;
            }
            else if (building.Info.m_buildingAI is VehicleWashBuildingAI && VehicleWashBuildingManager.VehicleWashBuildingExist(buildingID))
            {
                var vehicleWashBuilding = VehicleWashBuildingManager.GetVehicleWashBuilding(buildingID);

                if (vehicleWashBuilding.VehicleWashLanes == null || vehicleWashBuilding.VehicleWashLanes.Count == 0)
                {
                    return false;
                }

                // Pick a random segment from the fuel lanes
                ushort segmentId = vehicleWashBuilding.VehicleWashLanes[Random.Range(0, vehicleWashBuilding.VehicleWashLanes.Count)];
                var laneId = NetManager.instance.m_segments.m_buffer[segmentId].m_lanes; // single lane index 0

                if (laneId == 0) return false;

                // Get lane position for pathfinding target
                targetPos = NetManager.instance.m_lanes.m_buffer[laneId].CalculatePosition(1f); // end of lane
                return true;
            }
            return false;
        }
    }
}
