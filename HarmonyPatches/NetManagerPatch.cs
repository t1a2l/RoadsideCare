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
           [typeof(ushort), typeof(Randomizer), typeof(NetInfo), typeof(ushort), typeof(ushort), typeof(Vector3), typeof(Vector3), typeof(uint), typeof(uint), typeof(bool)],
           [ArgumentType.Out, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPostfix]
        public static void CreateSegment(ref ushort segment, ref Randomizer randomizer, NetInfo info, TreeInfo treeInfo, ushort startNode, ushort endNode, Vector3 startDirection, Vector3 endDirection, uint buildIndex, uint modifiedIndex, bool invert)
        {
            UpdateStationsNearSegment(segment, true);
        }

        [HarmonyPatch(typeof(NetManager), "ReleaseSegment")]
        [HarmonyPostfix]
        public static void ReleaseSegment(ushort segment, bool keepNodes)
        {
            UpdateStationsNearSegment(segment, false);
        }

        [HarmonyPatch(typeof(NetManager), "UpdateSegment", [typeof(ushort), typeof(ushort), typeof(int)], [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPostfix]
        public static void UpdateSegment(ushort segment, ushort fromNode, int level)
        {
            UpdateStationsNearSegment(segment, true);
        }

        private static void UpdateStationsNearSegment(ushort segmentID, bool isNew)
        {
            var segment = NetManager.instance.m_segments.m_buffer[segmentID];

            if(segment.Info.m_netAI is not FuelLaneAI)
            {
                return;
            }

            if (GasStationManager.SegmentIdBelongsToAGasStation(segmentID))
            {
                return;
            }

            Vector3 segPos = segment.m_bounds.center;

            for (ushort buildingID = 0; buildingID < BuildingManager.MAX_BUILDING_COUNT; buildingID++)
            {
                ref Building b = ref BuildingManager.instance.m_buildings.m_buffer[buildingID];
                if ((b.m_flags & Building.Flags.Created) == 0)
                    continue;

                if (b.Info.m_buildingAI is GasPumpAI && Vector3.Distance(segPos, b.m_position) <= 30f && GasStationManager.GasStationBuildingExist(buildingID))
                {
                    var gasStation = GasStationManager.GetGasStationBuilding(buildingID);
                    if (isNew)
                    {
                        if (!gasStation.FuelLanes.Contains(segmentID))
                        {
                            gasStation.FuelLanes.Add(segmentID);
                        }    
                    }
                    else
                    {
                        gasStation.FuelLanes.Remove(segmentID);
                    }
                    GasStationManager.SetFuelLanes(buildingID, gasStation.FuelLanes);
                }
            }
        }
    }
}
