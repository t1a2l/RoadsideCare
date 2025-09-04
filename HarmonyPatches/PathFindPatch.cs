using System.Reflection;
using ColossalFramework;
using HarmonyLib;
using RoadsideCare.AI;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
    public static class PathFindPatch
    {
        [HarmonyPatch(typeof(PathFind), "CalculateLaneSpeed")]
        [HarmonyPostfix]
        public static void CalculateLaneSpeed(PathFind __instance, byte startOffset, byte endOffset, ref NetSegment segment, NetInfo.Lane laneInfo, ref float __result)
        {
            bool isFuelPoint = segment.Info.m_netAI is FuelPointAI || segment.Info.m_netAI is FuelPointSmallAI || segment.Info.m_netAI is FuelPointLargeAI;
            bool isVehicleWashPoint = segment.Info.m_netAI is VehicleWashPointAI || segment.Info.m_netAI is VehicleWashPointSmallAI || segment.Info.m_netAI is VehicleWashPointLargeAI;
            bool isVehicleWashLane = segment.Info.m_netAI is VehicleWashLaneAI || segment.Info.m_netAI is VehicleWashLaneSmallAI || segment.Info.m_netAI is VehicleWashLaneLargeAI;

            if (isFuelPoint || isVehicleWashPoint || isVehicleWashLane)
            {
                uint m_startLaneA = (uint)typeof(PathFind).GetField("m_startLaneA", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                uint m_startLaneB = (uint)typeof(PathFind).GetField("m_startLaneB", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                uint m_endLaneA = (uint)typeof(PathFind).GetField("m_endLaneA", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                uint m_endLaneB = (uint)typeof(PathFind).GetField("m_startLaneA", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

                ushort segmentId = Singleton<NetManager>.instance.m_lanes.m_buffer[segment.m_lanes].m_segment;

                bool sameSegment = false;

                if (m_startLaneA != 0 && Singleton<NetManager>.instance.m_lanes.m_buffer[m_startLaneA].m_segment == segmentId)
                {
                    sameSegment = true;
                }

                if (!sameSegment && m_startLaneB != 0 && Singleton<NetManager>.instance.m_lanes.m_buffer[m_startLaneB].m_segment == segmentId)
                {
                    sameSegment = true;
                }

                if (!sameSegment && m_endLaneA != 0 && Singleton<NetManager>.instance.m_lanes.m_buffer[m_endLaneA].m_segment == segmentId)
                {
                    sameSegment = true;
                }

                if (!sameSegment && m_endLaneB != 0 && Singleton<NetManager>.instance.m_lanes.m_buffer[m_endLaneB].m_segment == segmentId)
                {
                    sameSegment = true;
                }

                if(!sameSegment)
                {
                    // not a start or end lane for this pathfinding - make it very slow to avoid using it
                    __result /= 100;
                }
            }
        }
    }
}
