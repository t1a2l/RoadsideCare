using ColossalFramework.Math;
using HarmonyLib;
using RoadsideCare.AI;
using UnityEngine;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
    public static class NetManagerPatch
    {
        [HarmonyPatch(typeof(NetManager), "CreateSegment",
           [typeof(ushort), typeof(Randomizer), typeof(NetInfo), typeof(ushort), typeof(ushort), typeof(Vector3), typeof(Vector3), typeof(uint), typeof(uint), typeof(uint)],
           [ArgumentType.Out, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPostfix]
        public static void CreateSegment(ref ushort segment, ref Randomizer randomizer, NetInfo info, ushort startNode, ushort endNode, Vector3 startDirection, Vector3 endDirection, uint buildIndex, uint modifiedIndex, bool invert)
        {
            UpdateStationsNearSegment(segment, true);
        }

        [HarmonyPatch(typeof(NetManager), "ReleaseSegment")]
        [HarmonyPostfix]
        public static void ReleaseSegment(ushort segment, bool keepNodes)
        {
            UpdateStationsNearSegment(segment, false);
        }

        private static void UpdateStationsNearSegment(ushort segmentID, bool isNew)
        {
            var segment = NetManager.instance.m_segments.m_buffer[segmentID];

            Vector3 segPos = segment.m_bounds.center;

            for (ushort buildingID = 0; buildingID < BuildingManager.MAX_BUILDING_COUNT; buildingID++)
            {
                ref Building b = ref BuildingManager.instance.m_buildings.m_buffer[buildingID];
                if ((b.m_flags & Building.Flags.Created) == 0)
                    continue;

                if (b.Info.m_buildingAI is GasPumpAI gasPumpAI && Vector3.Distance(segPos, b.m_position) <= 30f && segment.Info.m_netAI is FuelLaneAI)
                {
                    if (isNew)
                    {
                        if (!gasPumpAI.m_fuelLanes.Contains(segmentID))
                            gasPumpAI.m_fuelLanes.Add(segmentID);
                    }
                    else
                    {
                        gasPumpAI.m_fuelLanes.Remove(segmentID);
                    }
                }
            }
        }
    }
}
