using ColossalFramework.Math;
using HarmonyLib;
using RoadsideCare.AI;
using RoadsideCare.Managers;
using UnityEngine;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
    public static class NetManagerPatch
    {
        [HarmonyPatch(typeof(NetManager), "CreateSegment",
           [typeof(ushort), typeof(Randomizer), typeof(NetInfo), typeof(TreeInfo), typeof(ushort), typeof(ushort), typeof(Vector3), typeof(Vector3), typeof(uint), typeof(uint), typeof(bool)],
           [ArgumentType.Out, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPostfix]
        public static void CreateSegment(ref ushort segment, ref Randomizer randomizer, NetInfo info, TreeInfo treeInfo, ushort startNode, ushort endNode, Vector3 startDirection, Vector3 endDirection, uint buildIndex, uint modifiedIndex, bool invert)
        {
            CreateOrUpdateSegmentToARoadCareBuilding(segment);
        }

        [HarmonyPatch(typeof(NetManager), "UpdateSegment",
           [typeof(ushort), typeof(ushort), typeof(int)],
           [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPostfix]
        public static void UpdateSegment(ushort segment, ushort fromNode, int level)
        {
            CreateOrUpdateSegmentToARoadCareBuilding(segment);
        }

        [HarmonyPatch(typeof(NetManager), "ReleaseSegment")]
        [HarmonyPostfix]
        public static void ReleaseSegment(ushort segment, bool keepNodes)
        {
            RemoveSegmentFromRoadCareBuilding(segment);
        }

        private static void CreateOrUpdateSegmentToARoadCareBuilding(ushort segmentID)
        {
            var segment = NetManager.instance.m_segments.m_buffer[segmentID];

            if (segment.Info.m_netAI is FuelPointAI || segment.Info.m_netAI is FuelPointSmallAI || segment.Info.m_netAI is FuelPointLargeAI)
            {
                var gasStationBuildings = GasStationManager.GetGasStationBuildings();

                foreach (ushort buildingID in gasStationBuildings.Keys)
                {
                    if (IsSegmentFullyWithinRadius(segmentID, buildingID, 20f))
                    {
                        var gasStation = GasStationManager.GetGasStationBuilding(buildingID);
                        if (!gasStation.FuelPoints.Contains(segmentID))
                        {
                            gasStation.FuelPoints.Add(segmentID);
                            GasStationManager.SetFuelPoints(buildingID, gasStation.FuelPoints);
                            break;
                        }
                    }
                }
                return;
            }

            if (segment.Info.m_netAI is VehicleWashLaneAI || segment.Info.m_netAI is VehicleWashLaneSmallAI || segment.Info.m_netAI is VehicleWashLaneLargeAI)
            {
                var vehicleWashBuildings = VehicleWashBuildingManager.GetVehicleWashBuildings();

                foreach (ushort buildingID in vehicleWashBuildings.Keys)
                {
                    if (IsSegmentFullyWithinRadius(segmentID, buildingID, 20f))
                    {
                        var vehicleWashBuilding = VehicleWashBuildingManager.GetVehicleWashBuilding(buildingID);
                        if (!vehicleWashBuilding.VehicleWashLanes.Contains(segmentID))
                        {
                            vehicleWashBuilding.VehicleWashLanes.Add(segmentID);
                            VehicleWashBuildingManager.SetVehicleWashLanes(buildingID, vehicleWashBuilding.VehicleWashLanes);
                            break;
                        }  
                    }
                }
                return;
            }

            if (segment.Info.m_netAI is VehicleWashPointAI || segment.Info.m_netAI is VehicleWashPointSmallAI || segment.Info.m_netAI is VehicleWashPointLargeAI)
            {
                var vehicleWashBuildings = VehicleWashBuildingManager.GetVehicleWashBuildings();

                foreach (ushort buildingID in vehicleWashBuildings.Keys)
                {
                    if (IsSegmentFullyWithinRadius(segmentID, buildingID, 20f))
                    {
                        var vehicleWashBuilding = VehicleWashBuildingManager.GetVehicleWashBuilding(buildingID);
                        if (!vehicleWashBuilding.VehicleWashPoints.Contains(segmentID))
                        {
                            vehicleWashBuilding.VehicleWashPoints.Add(segmentID);
                            VehicleWashBuildingManager.SetVehicleWashPoints(buildingID, vehicleWashBuilding.VehicleWashPoints);
                            break;
                        }  
                    }
                }
                return;
            }
        }

        private static void RemoveSegmentFromRoadCareBuilding(ushort segmentID)
        {
            var segment = NetManager.instance.m_segments.m_buffer[segmentID];

            if (segment.Info.m_netAI is FuelPointAI || segment.Info.m_netAI is FuelPointSmallAI || segment.Info.m_netAI is FuelPointLargeAI)
            {
                if(GasStationManager.SegmentIdBelongsToAGasStation(segmentID))
                {
                    var buildingID = GasStationManager.GetSegmentIdGasStation(segmentID);
                    var gasStation = GasStationManager.GetGasStationBuilding(buildingID);
                    gasStation.FuelPoints.Remove(segmentID);
                    GasStationManager.SetFuelPoints(buildingID, gasStation.FuelPoints);
                }
            }

            if (segment.Info.m_netAI is VehicleWashLaneAI || segment.Info.m_netAI is VehicleWashLaneSmallAI || segment.Info.m_netAI is VehicleWashLaneLargeAI)
            {
                if (VehicleWashBuildingManager.SegmentIdBelongsToAVehicleWashBuildingWithLanes(segmentID))
                {
                    var buildingID = VehicleWashBuildingManager.GetSegmentIdVehicleWashBuildingByLane(segmentID);
                    var vehicleWashBuilding = VehicleWashBuildingManager.GetVehicleWashBuilding(buildingID);
                    vehicleWashBuilding.VehicleWashLanes.Remove(segmentID);
                    VehicleWashBuildingManager.SetVehicleWashLanes(buildingID, vehicleWashBuilding.VehicleWashLanes);
                }
            }

            if (segment.Info.m_netAI is VehicleWashPointAI || segment.Info.m_netAI is VehicleWashPointSmallAI || segment.Info.m_netAI is VehicleWashPointLargeAI) 
            {
                if (VehicleWashBuildingManager.SegmentIdBelongsToAVehicleWashBuildingWithPoints(segmentID))
                {
                    var buildingID = VehicleWashBuildingManager.GetSegmentIdVehicleWashBuildingByPoint(segmentID);
                    var vehicleWashBuilding = VehicleWashBuildingManager.GetVehicleWashBuilding(buildingID);
                    vehicleWashBuilding.VehicleWashPoints.Remove(segmentID);
                    VehicleWashBuildingManager.SetVehicleWashPoints(buildingID, vehicleWashBuilding.VehicleWashPoints);
                }
            }
        }

        private static bool IsSegmentFullyWithinRadius(ushort segmentId, ushort buildingId, float radius)
        {
            Vector3 buildingPos = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_position;

            // Get Bezier curve of the segment
            Bezier3 bezier = GetSegmentBezier(segmentId);

            // Number of samples along the curve
            int samples = 8; // increase for more accuracy
            float radiusSq = radius * radius;

            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector3 pos = bezier.Position(t);

                // If any point is outside radius, the segment is not fully inside
                if ((pos - buildingPos).sqrMagnitude > radiusSq)
                    return false;
            }

            return true;
        }

        private static Bezier3 GetSegmentBezier(ushort segmentId)
        {
            ref NetSegment segment = ref NetManager.instance.m_segments.m_buffer[segmentId];

            Vector3 startPos = NetManager.instance.m_nodes.m_buffer[segment.m_startNode].m_position;
            Vector3 endPos = NetManager.instance.m_nodes.m_buffer[segment.m_endNode].m_position;

            Vector3 startDir = segment.m_startDirection;
            Vector3 endDir = segment.m_endDirection;

            NetSegment.CalculateMiddlePoints(
                startPos, startDir,
                endPos, endDir,
                smoothStart: false, // use true if you want smoothing
                smoothEnd: false,
                out Vector3 middlePos1,
                out Vector3 middlePos2
            );

            Bezier3 bezier;
            bezier.a = startPos;
            bezier.b = middlePos1;
            bezier.c = middlePos2;
            bezier.d = endPos;

            return bezier;
        }
    }
}
