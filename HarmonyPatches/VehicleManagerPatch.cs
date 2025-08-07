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

    }
}
