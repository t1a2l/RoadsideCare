using ColossalFramework;
using HarmonyLib;
using RoadsideCare.Managers;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
    public static class CitizenPatch
    {
        [HarmonyPatch(typeof(Citizen), "SetVehicle")]
        [HarmonyPrefix]
        public static void SetVehicle(Citizen __instance, uint citizenID, ushort vehicleID, uint unitID)
        {
            if(citizenID != 0 && vehicleID != 0 && __instance.m_vehicle == 0)
            {
                var isPassengerCar = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleID].Info.GetAI() is PassengerCarAI;
                var citizenAI = __instance.GetCitizenInfo(citizenID).m_citizenAI;
                // tourist or resident that has a parked car and but no driving car and is going to set the current vehicle as their own car

                if ((citizenAI is ResidentAI || citizenAI is TouristAI) && isPassengerCar)
                {
                    // get parked vehicle of the owner
                    var parkedVehicleNeedsPair = VehicleNeedsManager.FindParkedVehicleOwner(citizenID);

                    // check it is the owner vehicle and a needs data exist
                    if (parkedVehicleNeedsPair.Value.OwnerId == citizenID && VehicleNeedsManager.VehicleNeedsExist(vehicleID))
                    {
                        // find the vehicle to be set and check it has no owner
                        var vehicleNeedsPair = VehicleNeedsManager.FindVehicleWithNoOwner(vehicleID);
                        if(vehicleNeedsPair.Key == vehicleID && vehicleNeedsPair.Value.OwnerId == 0)
                        {
                            var vehicleNeedsStruct = new VehicleNeedsManager.VehicleNeedsStruct
                            {
                                OriginalTargetBuilding = 0,
                                OwnerId = citizenID,
                                FuelAmount = parkedVehicleNeedsPair.Value.FuelAmount,
                                FuelCapacity = parkedVehicleNeedsPair.Value.FuelCapacity,
                                IsRefueling = false,
                                IsGoingToRefuel = false,
                                DirtPercentage = parkedVehicleNeedsPair.Value.DirtPercentage,
                                IsBeingWashed = false,
                                IsGoingToGetWashed = false,
                                WearPercentage = parkedVehicleNeedsPair.Value.WearPercentage,
                                IsBeingRepaired = false,
                                IsGoingToGetRepaired = false,
                                IsBroken = false,
                                IsOutOfFuel = false
                            };

                            VehicleNeedsManager.SetNewVehicleNeeds(vehicleID, vehicleNeedsStruct);

                            VehicleNeedsManager.RemoveParkedVehicleNeeds(parkedVehicleNeedsPair.Key);
                        } 
                    }
                }
            }
        }
    }
}
