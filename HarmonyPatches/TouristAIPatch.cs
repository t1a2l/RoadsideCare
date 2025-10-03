using ColossalFramework;
using HarmonyLib;
using MoreTransferReasons;
using RoadsideCare.AI;
using RoadsideCare.Managers;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
    public static class TouristAIPatch
    {
        public static ushort Chosen_Building = 0;

        [HarmonyPatch(typeof(TouristAI), "SetTarget")]
        [HarmonyPrefix]
        public static void SetTarget(TouristAI __instance, ushort instanceID, ref CitizenInstance data, ushort targetIndex, bool targetIsNode)
        {
            var vehicleId = Singleton<CitizenManager>.instance.m_citizens.m_buffer[data.m_citizen].m_vehicle;
            var buildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetIndex].Info.GetAI();
            if (vehicleId != 0 && VehicleNeedsManager.VehicleNeedsExist(vehicleId))
            {
                if (buildingAI is GasStationAI || buildingAI is GasPumpAI || buildingAI is VehicleWashBuildingAI || buildingAI is RepairStationAI)
                {
                    var targetBuilding = data.m_targetBuilding != 0 ? data.m_targetBuilding : targetIndex;
                    VehicleNeedsManager.SetOriginalTargetBuilding(vehicleId, targetBuilding);
                }
            }
        }

        [HarmonyPatch(typeof(TouristAI), "StartTransfer")]
        [HarmonyPrefix]
        public static bool StartTransfer(TouristAI __instance, uint citizenID, ref Citizen data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            if (data.m_flags == Citizen.Flags.None || data.Dead || data.Sick)
            {
                return true;
            }
            ushort source_building = 0;
            switch (data.CurrentLocation)
            {
                case Citizen.Location.Home:
                    source_building = data.m_homeBuilding;
                    break;

                case Citizen.Location.Work:
                    source_building = data.m_workBuilding;
                    break;

                case Citizen.Location.Visit:
                    source_building = data.m_visitBuilding;
                    break;
            }
            switch (material)
            {
                case ExtendedTransferManager.VehicleFuel:
                case ExtendedTransferManager.VehicleFuelElectric:
                case ExtendedTransferManager.VehicleWash:
                case ExtendedTransferManager.VehicleMinorRepair:
                case ExtendedTransferManager.VehicleMajorRepair:
                    data.m_flags &= ~Citizen.Flags.Evacuating;
                    Singleton<VehicleManager>.instance.m_vehicles.m_buffer[data.m_vehicle].m_custom = (ushort)material;
                    __instance.StartMoving(citizenID, ref data, source_building, offer.Building);
                    return false;
            }
            return true;
        }

    }
}
