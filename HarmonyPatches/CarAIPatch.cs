using System.Runtime.CompilerServices;
using ColossalFramework;
using HarmonyLib;
using MoreTransferReasons;
using MoreTransferReasons.AI;
using RoadsideCare.AI;
using RoadsideCare.Managers;
using RoadsideCare.Utils;
using UnityEngine;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
    public static class CarAIPatch
    {
        const float FRAMES_PER_UNIT = 3.2f;

        [HarmonyPatch(typeof(CarAI), "PathfindFailure")]
        [HarmonyPostfix]
        public static void PathfindFailure(ushort vehicleID, ref Vehicle data)
        {
            if (VehicleNeedsManager.VehicleNeedsExist(vehicleID))
            {
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
                var targetBuilding = vehicleNeeds.OriginalTargetBuilding;

                if (data.Info.m_vehicleAI is CargoTruckAI cargoTruckAI && (data.m_targetBuilding != 0))
                {
                    cargoTruckAI.SetTarget(vehicleID, ref data, targetBuilding);
                }
                else if (data.Info.m_vehicleAI is PassengerCarAI passengerCarAI && (data.m_targetBuilding != 0))
                {
                    passengerCarAI.SetTarget(vehicleID, ref data, targetBuilding);
                }
            }
        }

        [HarmonyPatch(typeof(CarAI), "StartPathFind",
           [typeof(ushort), typeof(Vehicle), typeof(Vector3), typeof(Vector3), typeof(bool), typeof(bool), typeof(bool)],
           [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyReversePatch]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool BaseCarAIStartPathFind(CarAI instance, ushort vehicleID, ref Vehicle vehicleData, Vector3 startPos, Vector3 endPos, bool startBothWays, bool endBothWays, bool undergroundTarget)
        {
            return false;
        }

        public static void ArriveAtTarget(CarAI instance, ushort vehicleID, ref Vehicle data)
        {
            var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
            if (vehicleNeeds.IsGoingToRefuel || vehicleNeeds.IsGoingToHandWash)
            {
                var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];
                var distance = Vector3.Distance(data.GetLastFramePosition(), building.m_position);
                if (distance < 80f && building.Info.GetAI() is GasStationAI || building.Info.GetAI() is GasPumpAI || building.Info.GetAI() is VehicleWashBuildingAI)
                {
                    VehicleNeedsManager.SetServiceTimer(vehicleID, 0); // Reset service timer
                    data.m_blockCounter = 0;
                    data.m_flags |= Vehicle.Flags.Stopped;
                    data.m_flags |= Vehicle.Flags.WaitingPath;

                    if (vehicleNeeds.IsGoingToRefuel)
                    {
                        float fuelingInSeconds = 0;

                        if(data.Info.GetAI() is PassengerCarAI)
                        {
                            fuelingInSeconds = RoadsideCareSettings.PassengerCarFuelingTimeInSeconds;
                        }
                        if (data.Info.GetAI() is ExtendedCargoTruckAI)
                        {
                            fuelingInSeconds = RoadsideCareSettings.CargoTruckFuelingTimeInSeconds;
                        }

                        var fuel_steps = 4 * fuelingInSeconds;

                        float initialFuel = vehicleNeeds.FuelAmount;

                        // Calculates the total fuel that still needs to be added to reach full capacity
                        float fuelNeeded = vehicleNeeds.FuelCapacity - initialFuel;

                        float delta = fuelNeeded / fuel_steps;

                        VehicleNeedsManager.SetFuelPerFrame(vehicleID, delta); // Set the fuel per frame to be added during refueling

                        VehicleNeedsManager.SetIsRefuelingMode(vehicleID); // sets IsGoingToRefuel to false and IsRefueling to true
                    }
                    if (vehicleNeeds.IsGoingToHandWash)
                    {
                        float handWashInSeconds = 0;

                        if (data.Info.GetAI() is PassengerCarAI)
                        {
                            handWashInSeconds = RoadsideCareSettings.PassengerCarHandWashTimeInSeconds;
                        }
                        if (data.Info.GetAI() is ExtendedCargoTruckAI)
                        {
                            handWashInSeconds = RoadsideCareSettings.CargoTruckHandWashTimeInSeconds;
                        }

                        var handWash_steps = 4 * handWashInSeconds;

                        // Calculates the total dirt that needs to be removed
                        float dirtToRemove = vehicleNeeds.DirtPercentage;

                        float delta = dirtToRemove / handWash_steps;

                        VehicleNeedsManager.SetDirtPerFrame(vehicleID, delta); // Set the dirt per frame to be removed during hand wash

                        VehicleNeedsManager.SetIsAtHandWashMode(vehicleID); // sets IsGoingToHandWash to false and IsAtHandWash to true
                    }
                }
            }
            if (vehicleNeeds.IsAtTunnelWash)
            {
                data.m_blockCounter = 0;
                data.m_flags |= Vehicle.Flags.Stopped;
                data.m_flags |= Vehicle.Flags.WaitingPath;
                VehicleNeedsManager.SetIsAtTunnelWashExitMode(vehicleID);
            }
        }

        public static void TakingCareOfVehicle(CarAI instance, ushort vehicleID, ref Vehicle data)
        {
            var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);

            var serviceTimer = vehicleNeeds.ServiceTimer + 1; // Increment the service timer

            VehicleNeedsManager.SetServiceTimer(vehicleID, serviceTimer);

            bool TunnelWashComplete = false;

            if (vehicleNeeds.IsRefueling)
            {
                var newAmount = vehicleNeeds.FuelAmount + vehicleNeeds.FuelPerFrame;

                VehicleNeedsManager.SetFuelAmount(vehicleID, newAmount);

                if(serviceTimer >= 4 && serviceTimer % 4 == 0)
                {
                    var fuelAmount = 4 * vehicleNeeds.FuelPerFrame;
                    // Update the gas station's fuel buffer
                    ref var building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];
                    ModifyGasStationFuelAmount(vehicleID, ref data, ref building, (int)fuelAmount);
                }
            }

            if (vehicleNeeds.IsAtHandWash)
            {
                var newAmount = vehicleNeeds.DirtPercentage - vehicleNeeds.DirtPerFrame;

                // Update the manager with the new dirt percentage.
                VehicleNeedsManager.SetDirtPercentage(vehicleID, newAmount);
            }

            if (vehicleNeeds.IsGoingToTunnelWash)
            {
                ref var building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];

                float distance = Vector3.Distance(data.GetLastFramePosition(), building.m_position);

                if (distance <= 80f)
                {
                    byte b = data.m_pathPositionIndex;

                    PathManager.instance.m_pathUnits.m_buffer[data.m_path].GetPosition(b >> 1, out var position);

                    ref var segment = ref Singleton<NetManager>.instance.m_segments.m_buffer[position.m_segment];

                    if (segment.Info.GetAI() is VehicleWashLaneAI)
                    {
                        VehicleNeedsManager.SetIsAtTunnelWashMode(vehicleID);

                        float totalWashingFrames = segment.m_averageLength * FRAMES_PER_UNIT;

                        // The dirt to remove is calculated once and stored
                        float dirtToRemovePerFrame = vehicleNeeds.DirtPercentage / totalWashingFrames;

                        VehicleNeedsManager.SetDirtPerFrame(vehicleID, dirtToRemovePerFrame);
                    }
                }
            }

            if (vehicleNeeds.IsAtTunnelWash)
            {
                if (vehicleNeeds.IsAtTunnelWashExit)
                {
                    data.m_flags |= Vehicle.Flags.Stopped;
                    data.m_flags |= Vehicle.Flags.WaitingPath;
                    data.m_blockCounter = 0;
                }

                float dirtToRemoveThisFrame = vehicleNeeds.DirtPerFrame;

                // This is the check that will prevent the number from going negative
                float dirtLevel = Mathf.Max(0, vehicleNeeds.DirtPercentage - dirtToRemoveThisFrame);

                VehicleNeedsManager.SetDirtPercentage(vehicleID, dirtLevel);

                if(dirtLevel <= 0)
                {
                    TunnelWashComplete = true;
                }
            }

            data.m_flags |= Vehicle.Flags.Stopped;
            data.m_flags |= Vehicle.Flags.WaitingPath;
            data.m_blockCounter = 0;

            vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);

            var FuelingComplete = vehicleNeeds.IsRefueling && vehicleNeeds.FuelAmount >= vehicleNeeds.FuelCapacity;
            var HandWashComplete = vehicleNeeds.IsAtHandWash && vehicleNeeds.DirtPercentage <= 0;

            if (FuelingComplete || HandWashComplete || TunnelWashComplete)
            {
                VehicleNeedsManager.ClearAtLocationMode(vehicleID);
                var targetBuilding = vehicleNeeds.OriginalTargetBuilding;

                data.m_flags &= ~Vehicle.Flags.Stopped;
                data.m_flags &= ~Vehicle.Flags.WaitingPath;
                VehicleNeedsManager.SetOriginalTargetBuilding(vehicleID, 0);
                VehicleNeedsManager.SetServiceTimer(vehicleID, 0);

                if (vehicleNeeds.IsRefueling)
                {
                    // Ensure fuel is exactly at capacity after refuel is complete
                    VehicleNeedsManager.SetFuelAmount(vehicleID, vehicleNeeds.FuelCapacity);
                }

                if (vehicleNeeds.IsAtHandWash || vehicleNeeds.IsAtTunnelWash)
                {
                    // Ensure dirt is exactly at 0 after cleaning is complete
                    VehicleNeedsManager.SetDirtPercentage(vehicleID, 0);
                }

                if (data.Info.GetAI() is PassengerCarAI)
                {
                    var citizenId = instance.GetOwnerID(vehicleID, ref data).Citizen;
                    ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];
                    var citizenInstance = Singleton<CitizenManager>.instance.m_instances.m_buffer[citizen.m_instance];
                    var humanAI = citizen.GetCitizenInfo(citizenId).GetAI() as HumanAI;
                    humanAI.StartMoving(citizenId, ref citizen, citizenInstance.m_targetBuilding, targetBuilding);
                }
                else if (data.Info.GetAI() is ExtendedCargoTruckAI extendedCargoTruckAI)
                {
                    extendedCargoTruckAI.SetTarget(vehicleID, ref data, targetBuilding);
                }

            }
        }

        private static void ModifyGasStationFuelAmount(ushort vehicleID, ref Vehicle data, ref Building building, int fuelAmount)
        {
            bool iElectricPassengerCar = data.Info.GetAI() is PassengerCarAI && data.Info.m_class.m_subService != ItemClass.SubService.ResidentialLow;
            bool iElectricCargoTruck = data.Info.GetAI() is ExtendedCargoTruckAI extendedCargoTruckAI && extendedCargoTruckAI.m_isElectric;

            if (!iElectricPassengerCar && iElectricCargoTruck)
            {
                if (building.Info.GetAI() is GasPumpAI gasPumpAI)
                {
                    gasPumpAI.ExtendedModifyMaterialBuffer(data.m_targetBuilding, ref building, ExtendedTransferManager.TransferReason.VehicleFuel, ref fuelAmount);
                }
                if (building.Info.GetAI() is GasStationAI gasStationAI)
                {
                    gasStationAI.ExtendedModifyMaterialBuffer(data.m_targetBuilding, ref building, ExtendedTransferManager.TransferReason.VehicleFuel, ref fuelAmount);
                }
            }
            Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.PublicIncome, 20, ItemClass.Service.Vehicles, ItemClass.SubService.None, ItemClass.Level.Level2);
        }
    }
}
