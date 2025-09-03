using ColossalFramework;
using HarmonyLib;
using RoadsideCare.Managers;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
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

        [HarmonyPatch(typeof(VehicleManager), "AddToGrid", [typeof(ushort), typeof(VehicleParked)], [ArgumentType.Normal, ArgumentType.Ref])]
        [HarmonyPostfix]
        public static void AddToGrid(ushort parked, ref VehicleParked data)
        {
            CreateNeedsForParkedVehicle(parked, ref data);
        }

        public static void CreateNeedsForParkedVehicle(ushort parkedID, ref VehicleParked data)
        {
            if (!VehicleNeedsManager.ParkedVehicleNeedsExist(parkedID))
            {
                int randomFuelAmount = Singleton<SimulationManager>.instance.m_randomizer.Int32(30, 60);
                int randomDirtiness = Singleton<SimulationManager>.instance.m_randomizer.Int32(0, 40);
                int randomWear = Singleton<SimulationManager>.instance.m_randomizer.Int32(0, 40);
                VehicleNeedsManager.CreateParkedVehicleNeeds(parkedID, data.m_ownerCitizen, randomFuelAmount, 60f, randomDirtiness, randomWear);
            }
        }

    }
}
