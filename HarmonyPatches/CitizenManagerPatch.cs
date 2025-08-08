using HarmonyLib;
using RoadsideCare.Managers;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
    public static class CitizenManagerPatch
    {
        [HarmonyPatch(typeof(CitizenManager), "ReleaseCitizenImplementation")]
        [HarmonyPrefix]
        public static void ReleaseCitizenImplementation(uint citizen, ref Citizen data)
        {
            if(data.m_vehicle != 0 && VehicleNeedsManager.VehicleNeedsExist(data.m_vehicle))
            {
                VehicleNeedsManager.RemoveVehicleNeeds(data.m_vehicle);
            }
            if (data.m_parkedVehicle != 0 && VehicleNeedsManager.ParkedVehicleNeedsExist(data.m_parkedVehicle))
            {
                VehicleNeedsManager.RemoveParkedVehicleNeeds(data.m_parkedVehicle);
            }
        }
    }
}
