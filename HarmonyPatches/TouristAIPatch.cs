using ColossalFramework;
using HarmonyLib;
using MoreTransferReasons;
using MoreTransferReasons.AI;
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

        [HarmonyPatch(typeof(ExtenedTouristAI), "ExtendedStartTransfer")]
        [HarmonyPrefix]
        public static bool ExtendedStartTransfer(ExtenedTouristAI __instance, uint citizenID, ref Citizen data, ExtendedTransferManager.TransferReason material, ExtendedTransferManager.Offer offer)
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
                case ExtendedTransferManager.TransferReason.VehicleFuel:
                case ExtendedTransferManager.TransferReason.VehicleFuelElectric:
                case ExtendedTransferManager.TransferReason.VehicleSmallWash:
                case ExtendedTransferManager.TransferReason.VehicleSmallMinorRepair:
                case ExtendedTransferManager.TransferReason.VehicleSmallMajorRepair:
                    data.m_flags &= ~Citizen.Flags.Evacuating;
                    __instance.StartMoving(citizenID, ref data, source_building, offer.Building);
                    return false;
            }
            return true;
        }

    }
}
