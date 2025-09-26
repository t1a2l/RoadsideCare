using HarmonyLib;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
    public static class TransferManagerPatch
    {
        [HarmonyPatch(typeof(TransferManager), "GetFrameReason")]
        [HarmonyPrefix]
        public static bool GetFrameReason(TransferManager __instance, int frameIndex, ref TransferManager.TransferReason __result)
        {
            if(frameIndex == 74 || frameIndex == 158 || frameIndex == 160 || frameIndex == 162 || frameIndex == 164 || frameIndex == 166 || frameIndex == 168 || frameIndex == 170)
            {
                if(frameIndex == 74)
                {
                    __result = Mod.PetroleumProducts;
                }
                else if(frameIndex == 158)
                {
                    __result = Mod.VehicleFuel;
                }
                else if (frameIndex == 160)
                {
                    __result = Mod.VehicleFuelElectric;
                }
                else if (frameIndex == 162)
                {
                    __result = Mod.VehicleWash;
                }
                else if (frameIndex == 164)
                {
                    __result = Mod.VehicleMinorRepair;
                }
                else if (frameIndex == 166)
                {
                    __result = Mod.VehicleMajorRepair;
                }
                else if (frameIndex == 168)
                {
                    __result = Mod.VehicleOutOfFuel;
                }
                else
                {
                    __result = Mod.VehicleBrokenDown;
                }
                return false;
            }
            return true;
        }
    }
}
