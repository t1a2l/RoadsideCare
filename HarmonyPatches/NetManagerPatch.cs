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
            UpdateRoadCareBuildingsNearNewSegment(segment);
        }

        [HarmonyPatch(typeof(NetManager), "ReleaseSegment")]
        [HarmonyPostfix]
        public static void ReleaseSegment(ushort segment, bool keepNodes)
        {
            UpdateRoadCareBuildingsNearRemovedSegment(segment);
        }

        private static void UpdateRoadCareBuildingsNearNewSegment(ushort segmentID)
        {
            var segment = NetManager.instance.m_segments.m_buffer[segmentID];

            if(segment.Info.m_netAI is not FuelPointAI && segment.Info.m_netAI is not FuelPointSmallAI && segment.Info.m_netAI is not FuelPointLargeAI && 
                segment.Info.m_netAI is not VehicleWashLaneAI && segment.Info.m_netAI is not VehicleWashLaneSmallAI && segment.Info.m_netAI is not VehicleWashLaneLargeAI &&
                segment.Info.m_netAI is not VehicleWashPointAI && segment.Info.m_netAI is not VehicleWashPointSmallAI && segment.Info.m_netAI is not VehicleWashPointLargeAI)
            {
                return;
            }

            for (ushort buildingID = 0; buildingID < BuildingManager.MAX_BUILDING_COUNT; buildingID++)
            {
                ref Building b = ref BuildingManager.instance.m_buildings.m_buffer[buildingID];
                if ((b.m_flags & Building.Flags.Created) == 0)
                    continue;

                if (b.Info.m_buildingAI is GasPumpAI && GasStationManager.GasStationBuildingExist(buildingID) && 
                    (segment.Info.m_netAI is FuelPointAI || segment.Info.m_netAI is FuelPointSmallAI || segment.Info.m_netAI is FuelPointLargeAI)
                    && IsSegmentFullyWithinRadius(segmentID, buildingID, 60f))
                {
                    var gasStation = GasStationManager.GetGasStationBuilding(buildingID);
                    gasStation.FuelPoints.Add(segmentID);
                    GasStationManager.SetFuelPoints(buildingID, gasStation.FuelPoints);
                    break;
                }
                else if (b.Info.m_buildingAI is VehicleWashBuildingAI && VehicleWashBuildingManager.VehicleWashBuildingExist(buildingID))
                {
                    if ((segment.Info.m_netAI is VehicleWashLaneAI || segment.Info.m_netAI is VehicleWashLaneSmallAI || segment.Info.m_netAI is VehicleWashLaneLargeAI) 
                        && IsSegmentFullyWithinRadius(segmentID, buildingID, 60f))
                    {
                        var vehicleWashBuilding = VehicleWashBuildingManager.GetVehicleWashBuilding(buildingID);
                        vehicleWashBuilding.VehicleWashLanes.Add(segmentID);
                        VehicleWashBuildingManager.SetVehicleWashLanes(buildingID, vehicleWashBuilding.VehicleWashLanes);
                        break;
                    }
                    else if ((segment.Info.m_netAI is VehicleWashPointAI || segment.Info.m_netAI is VehicleWashPointSmallAI || segment.Info.m_netAI is VehicleWashPointLargeAI) 
                        && IsSegmentFullyWithinRadius(segmentID, buildingID, 60f))
                    {
                        var vehicleWashBuilding = VehicleWashBuildingManager.GetVehicleWashBuilding(buildingID);
                        vehicleWashBuilding.VehicleWashPoints.Add(segmentID);
                        VehicleWashBuildingManager.SetVehicleWashPoints(buildingID, vehicleWashBuilding.VehicleWashPoints);
                        break;
                    }
                }
            }
        }

        private static void UpdateRoadCareBuildingsNearRemovedSegment(ushort segmentID)
        {
            var segment = NetManager.instance.m_segments.m_buffer[segmentID];

            if (segment.Info.m_netAI is not FuelPointAI && segment.Info.m_netAI is not FuelPointSmallAI && segment.Info.m_netAI is not FuelPointLargeAI &&
                segment.Info.m_netAI is not VehicleWashLaneAI && segment.Info.m_netAI is not VehicleWashLaneSmallAI && segment.Info.m_netAI is not VehicleWashLaneLargeAI &&
                segment.Info.m_netAI is not VehicleWashPointAI && segment.Info.m_netAI is not VehicleWashPointSmallAI && segment.Info.m_netAI is not VehicleWashPointLargeAI)
            {
                return;
            }

            if ((segment.Info.m_netAI is FuelPointAI || segment.Info.m_netAI is FuelPointSmallAI || segment.Info.m_netAI is FuelPointLargeAI) 
                && !GasStationManager.SegmentIdBelongsToAGasStation(segmentID))
            {
                return;
            }

            if ((segment.Info.m_netAI is VehicleWashLaneAI || segment.Info.m_netAI is VehicleWashLaneSmallAI || segment.Info.m_netAI is VehicleWashLaneLargeAI 
                || segment.Info.m_netAI is VehicleWashPointAI || segment.Info.m_netAI is VehicleWashPointSmallAI || segment.Info.m_netAI is VehicleWashPointLargeAI) 
                && !VehicleWashBuildingManager.SegmentIdBelongsToAVehicleWashBuilding(segmentID))
            {
                return;
            }

            for (ushort buildingID = 0; buildingID < BuildingManager.MAX_BUILDING_COUNT; buildingID++)
            {
                ref Building b = ref BuildingManager.instance.m_buildings.m_buffer[buildingID];
                if ((b.m_flags & Building.Flags.Created) == 0)
                    continue;

                if (b.Info.m_buildingAI is GasPumpAI && GasStationManager.GasStationBuildingExist(buildingID) 
                    && (segment.Info.m_netAI is FuelPointAI || segment.Info.m_netAI is FuelPointSmallAI || segment.Info.m_netAI is FuelPointLargeAI))
                {
                    var gasStation = GasStationManager.GetGasStationBuilding(buildingID);
                    gasStation.FuelPoints.Remove(segmentID);
                    GasStationManager.SetFuelPoints(buildingID, gasStation.FuelPoints);
                    break;
                }
                else if (b.Info.m_buildingAI is VehicleWashBuildingAI && VehicleWashBuildingManager.VehicleWashBuildingExist(buildingID))
                {
                    if (segment.Info.m_netAI is VehicleWashLaneAI || segment.Info.m_netAI is VehicleWashLaneSmallAI || segment.Info.m_netAI is VehicleWashLaneLargeAI)
                    {
                        var vehicleWashBuilding = VehicleWashBuildingManager.GetVehicleWashBuilding(buildingID);
                        vehicleWashBuilding.VehicleWashLanes.Remove(segmentID);
                        VehicleWashBuildingManager.SetVehicleWashLanes(buildingID, vehicleWashBuilding.VehicleWashLanes);
                        break;
                    }
                    else if (segment.Info.m_netAI is VehicleWashPointAI || segment.Info.m_netAI is VehicleWashPointSmallAI || segment.Info.m_netAI is VehicleWashPointLargeAI)
                    {
                        var vehicleWashBuilding = VehicleWashBuildingManager.GetVehicleWashBuilding(buildingID);
                        vehicleWashBuilding.VehicleWashPoints.Remove(segmentID);
                        VehicleWashBuildingManager.SetVehicleWashPoints(buildingID, vehicleWashBuilding.VehicleWashPoints);
                        break;
                    }
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
