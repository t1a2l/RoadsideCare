using HarmonyLib;
using RoadsideCare.Managers;

namespace RoadsideCare.HarmonyPatches
{
    public static class VehicleManagerPatch
    {
        [HarmonyPatch(typeof(VehicleManager), "ReleaseVehicle")]
        [HarmonyPrefix]
        public static void ReleaseVehicle(ushort vehicle)
        {
            if (VehicleNeedsManager.VehicleNeedsExist(vehicle))
            {
                VehicleNeedsManager.RemoveVehicleNeeds(vehicle);
            }
        }

        [HarmonyPatch(typeof(VehicleManager), "ReleaseParkedVehicle")]
        [HarmonyPrefix]
        public static void ReleaseParkedVehicle(ushort parked)
        {
            if (VehicleNeedsManager.ParkedVehicleNeedsExist(parked))
            {
                VehicleNeedsManager.RemoveParkedVehicleNeeds(parked);
            }
        }

    }
}
