using ColossalFramework;
using HarmonyLib;
using MoreTransferReasons;
using MoreTransferReasons.AI;
using RoadsideCare.Managers;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
    public static class VehicleAIPatch
    {
        [HarmonyPatch(typeof(VehicleAI), "CreateVehicle")]
        [HarmonyPostfix]
        public static void CreateVehicle(VehicleAI __instance, ushort vehicleID, ref Vehicle data)
        {
            CreateNeedsForVehicle(__instance, vehicleID, ref data);
        }

        [HarmonyPatch(typeof(VehicleAI), "SimulationStep",
            [typeof(ushort), typeof(Vehicle), typeof(Vehicle.Frame), typeof(ushort), typeof(Vehicle), typeof(int)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static void SimulationStep(VehicleAI __instance, ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
        {
            if (VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
                if (__instance is ExtendedCargoTruckAI || __instance is PassengerCarAI)
                {
                    bool isOnWayToCareCenter = vehicleNeeds.IsGoingToRefuel || vehicleNeeds.IsGoingToHandWash || vehicleNeeds.IsGoingToTunnelWash || vehicleNeeds.IsGoingToGetRepaired;
                    bool isBeingCaredFor = vehicleNeeds.IsRefueling || vehicleNeeds.IsAtHandWash || vehicleNeeds.IsAtTunnelWash || vehicleNeeds.IsBeingRepaired;

                    if (!isBeingCaredFor && !isOnWayToCareCenter)
                    {
                        VehicleNeedsManager.SetOriginalTargetBuilding(vehicleID, 0);
                        if (__instance is PassengerCarAI)
                        {
                            AskForRoadsideCarePassengerCar(__instance, vehicleID, ref vehicleData);
                        }
                        else
                        {
                            AskForRoadsideCare(vehicleID, ref vehicleData);
                        }
                    }
                }
                if (vehicleNeeds.FuelAmount > 0 && !vehicleNeeds.IsRefueling && (vehicleData.m_flags & Vehicle.Flags.Stopped) == 0)
                {
                    var newAmount = vehicleNeeds.FuelAmount - 0.01f;
                    VehicleNeedsManager.SetFuelAmount(vehicleID, newAmount);
                }
                if (vehicleNeeds.DirtPercentage < 100 && !(vehicleNeeds.IsAtHandWash || vehicleNeeds.IsAtTunnelWash))
                {
                    var newDirtPercentage = vehicleNeeds.DirtPercentage + 0.01f;
                    VehicleNeedsManager.SetDirtPercentage(vehicleID, newDirtPercentage);
                }
                //if (vehicleNeeds.WearPercentage < 100 && !vehicleNeeds.IsBeingRepaired)
                //{
                //    var newWearPercentage = vehicleNeeds.WearPercentage + 0.01f;
                //    VehicleNeedsManager.SetWearPercentage(vehicleID, newWearPercentage);
                //}

                //bool shouldBreakDownUnExpected = Singleton<SimulationManager>.instance.m_randomizer.Int32(10000U) == 0;

                //if (shouldBreakDownUnExpected)
                //{
                //    VehicleNeedsManager.SetIsBrokenMode(vehicleID);
                //}
            }
        }

        public static void CreateNeedsForVehicle(VehicleAI instance, ushort vehicleID, ref Vehicle data)
        {
            float passengerCarFuelCapacity = 60f;
            float truckFuelCapacity = 80f;

            if (instance is PassengerCarAI && !VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                int randomFuelAmount = Singleton<SimulationManager>.instance.m_randomizer.Int32(30, 60);
                int randomDirtiness = Singleton<SimulationManager>.instance.m_randomizer.Int32(0, 40);
                int randomWear = Singleton<SimulationManager>.instance.m_randomizer.Int32(0, 40);
                VehicleNeedsManager.CreateVehicleNeeds(vehicleID, 0, 0, 0, randomFuelAmount, passengerCarFuelCapacity, randomDirtiness, randomWear);
            }
            if (instance is ExtendedCargoTruckAI && !VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                int randomFuelAmount = Singleton<SimulationManager>.instance.m_randomizer.Int32(50, 80);
                int randomDirtiness = Singleton<SimulationManager>.instance.m_randomizer.Int32(0, 40);
                int randomWear = Singleton<SimulationManager>.instance.m_randomizer.Int32(0, 40);
                VehicleNeedsManager.CreateVehicleNeeds(vehicleID, 0, 0, 0, randomFuelAmount, truckFuelCapacity, randomDirtiness, randomWear);
            }
        }

        private static void AskForRoadsideCare(ushort vehicleID, ref Vehicle data)
        {
            var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
            float fuelPercent = vehicleNeeds.FuelAmount / vehicleNeeds.FuelCapacity;
            bool shouldFuel = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U) == 0;
            if ((fuelPercent > 0.2 && fuelPercent < 0.8 && shouldFuel) || fuelPercent <= 0.2)
            {
                ExtendedTransferManager.Offer offer = default;
                offer.Vehicle = vehicleID;
                offer.Position = data.GetLastFramePosition();
                offer.Amount = 1;
                offer.Active = true;
                if (data.Info.m_vehicleAI is ExtendedCargoTruckAI extendedCargoTruckAI && extendedCargoTruckAI.m_isElectric)
                {
                    Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.VehicleFuelElectric, offer);
                }
                else
                {
                    Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.VehicleFuel, offer);
                }
                return;
            }

            bool shouldWash = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U) == 0;

            if (shouldWash || vehicleNeeds.DirtPercentage >= 80)
            {
                ExtendedTransferManager.Offer offer = default;
                offer.Vehicle = vehicleID;
                offer.Position = data.GetLastFramePosition();
                offer.Amount = 1;
                offer.Active = true;
                Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.VehicleWash, offer);
                return;
            }

            //bool shouldReapir = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U) == 0;

            //if (shouldReapir || vehicleNeeds.WearPercentage >= 80)
            //{
            //    ExtendedTransferManager.Offer offer = default;
            //    offer.Vehicle = vehicleID;
            //    offer.Position = data.GetLastFramePosition();
            //    offer.Amount = 1;
            //    offer.Active = true;
            //    ExtendedTransferManager.TransferReason transferReason;
            //    bool isMajorRepair = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U) == 0;
            //    if (isMajorRepair)
            //    {
            //        transferReason = ExtendedTransferManager.TransferReason.VehicleLargeMajorRepair;
            //    }
            //    else
            //    {
            //        transferReason = ExtendedTransferManager.TransferReason.VehicleLargeMinorRepair;
            //    }
            //    Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(transferReason, offer);
            //}
        }

        private static void AskForRoadsideCarePassengerCar(VehicleAI instance, ushort vehicleID, ref Vehicle data)
        {
            var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
            float fuelPercent = vehicleNeeds.FuelAmount / vehicleNeeds.FuelCapacity;
            bool shouldFuel = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U) == 0;
            if ((fuelPercent > 0.2 && fuelPercent < 0.8 && shouldFuel) || fuelPercent <= 0.2)
            {
                ExtendedTransferManager.Offer offer = default;
                offer.Citizen = instance.GetOwnerID(vehicleID, ref data).Citizen;
                offer.Position = data.GetLastFramePosition();
                offer.Amount = 1;
                offer.Active = true;
                bool isElectric = data.Info.m_class.m_subService != ItemClass.SubService.ResidentialLow;
                if (isElectric)
                {
                    Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.VehicleFuelElectric, offer);
                }
                else
                {
                    Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.VehicleFuel, offer);
                }
                return;
            }

            bool shouldWash = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U) == 0;

            if ((shouldWash && vehicleNeeds.DirtPercentage > 20) || vehicleNeeds.DirtPercentage >= 80)
            {
                ExtendedTransferManager.Offer offer = default;
                offer.Citizen = instance.GetOwnerID(vehicleID, ref data).Citizen;
                offer.Position = data.GetLastFramePosition();
                offer.Amount = 1;
                offer.Active = true;
                Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(ExtendedTransferManager.TransferReason.VehicleWash, offer);
                return;
            }

            //bool shouldReapir = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U) == 0;

            //if (shouldReapir || vehicleNeeds.WearPercentage >= 90)
            //{
            //    ExtendedTransferManager.Offer offer = default;
            //    offer.Citizen = instance.GetOwnerID(vehicleID, ref data).Citizen;
            //    offer.Position = data.GetLastFramePosition();
            //    offer.Amount = 1;
            //    offer.Active = true;
            //    ExtendedTransferManager.TransferReason transferReason;
            //    bool isMajorRepair = Singleton<SimulationManager>.instance.m_randomizer.Int32(100U) == 0;
            //    if (isMajorRepair)
            //    {
            //        transferReason = ExtendedTransferManager.TransferReason.VehicleSmallMajorRepair;
            //    }
            //    else
            //    {
            //        transferReason = ExtendedTransferManager.TransferReason.VehicleSmallMinorRepair;
            //    }
            //    Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(transferReason, offer);
            //}
        }

    }
}
