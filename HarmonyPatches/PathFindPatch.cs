using HarmonyLib;
using RoadsideCare.AI;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
    public static class PathFindPatch
    {
        [HarmonyPatch(typeof(PathFind), "CalculateLaneSpeed")]
        [HarmonyPostfix]
        public static void CalculateLaneSpeed(byte startOffset, byte endOffset, ref NetSegment segment, NetInfo.Lane laneInfo, ref float __result)
        {
            bool isFuelPoint = segment.Info.m_netAI is FuelPointAI || segment.Info.m_netAI is FuelPointSmallAI || segment.Info.m_netAI is FuelPointLargeAI;
            bool isVehicleWashPoint = segment.Info.m_netAI is VehicleWashPointAI || segment.Info.m_netAI is VehicleWashPointSmallAI || segment.Info.m_netAI is VehicleWashPointLargeAI;
            bool isVehicleWashLane = segment.Info.m_netAI is VehicleWashLaneAI || segment.Info.m_netAI is VehicleWashLaneSmallAI || segment.Info.m_netAI is VehicleWashLaneLargeAI;
            if (isFuelPoint || isVehicleWashPoint || isVehicleWashLane)
            {
                __result /= 100;
            }
        }
    }
}
