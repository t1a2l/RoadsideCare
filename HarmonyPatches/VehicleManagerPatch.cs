using System.Diagnostics;
using System.Reflection;
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
            // Get the full stack trace.
            var stackTrace = new StackTrace();

            // The stack frame at index 0 is this Prefix method.
            // The stack frame at index 1 is the original MyTargetMethod.
            // The stack frame at index 2 is the method that called MyTargetMethod.
            var callingFrame = stackTrace.GetFrame(2);

            if (callingFrame != null)
            {
                // Get the method information from the stack frame.
                MethodBase callingMethod = callingFrame.GetMethod();
                string callingMethodName = callingMethod.Name;

                if (callingMethodName == "AfterDeserialize")
                {
                    CreateNeedsForParkedVehicle(parked, ref data);
                }
            }
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
