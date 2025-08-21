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

        public static void TakingCareOfVehicle(CarAI instance, ushort vehicleID, ref Vehicle data)
        {
            var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);

            // Get the time passed since the last frame.
            float deltaTime = SimulationManager.instance.m_simulationTimeDelta;

            // Add the elapsed time to the service timer
            float serviceTimer = vehicleNeeds.ServiceTimer + deltaTime;

            // Update the manager with the new service timer.
            VehicleNeedsManager.SetServiceTimer(vehicleID, serviceTimer);

            bool TunnelWashComplete = false;

            if (vehicleNeeds.IsRefueling)
            {
                // Store the fuel amount when the service began to act as a constant starting point
                // A temporary solution without changing the struct
                float initialFuel = vehicleNeeds.FuelAmount;

                // Calculates the total fuel that still needs to be added to reach full capacity
                float fuelNeeded = vehicleNeeds.FuelCapacity - initialFuel;

                // Calculate the target fuel amount based on the percentage of time that has passed,
                // starting from the initial fuel amount.
                float targetFuelAmount = initialFuel + (serviceTimer / RoadsideCareSettings.PassengerCarFuelingTimeInSeconds) * fuelNeeded;

                // Update the vehicle's fuel amount, capping it at the maximum capacity.
                vehicleNeeds.FuelAmount = Mathf.Min(vehicleNeeds.FuelCapacity, targetFuelAmount);

                // Update the manager with the new fuel amount.
                VehicleNeedsManager.SetFuelAmount(vehicleID, vehicleNeeds.FuelAmount);

                // Calculates the fuel refill rate in units of "fuel per second".
                float fuelPerSecond = fuelNeeded / RoadsideCareSettings.PassengerCarFuelingTimeInSeconds;

                // represents the value of the ServiceTimer at the start of the current frame
                float nextServiceTimer = serviceTimer - deltaTime;

                // Total fuel amount that the vehicle had at the beginning of the current frame
                // This calculation now uses the initial fuel amount and the amount added up to the previous frame
                float frameTotalFuelAmount = initialFuel + fuelPerSecond * nextServiceTimer;

                // Calculate the amount of fuel to remove from the gas station buffer this frame.
                float fuelToRemoveThisFrame = targetFuelAmount - (serviceTimer > 0 ? frameTotalFuelAmount : 0);

                // Update the gas station's fuel buffer
                ref var building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];
                ModifyGasStationFuelAmount(vehicleID, ref data, ref building, (int)fuelToRemoveThisFrame);
            }

            if (vehicleNeeds.IsAtHandWash)
            {
                // Calculates the total dirt that needs to be removed
                float initialDirt = vehicleNeeds.DirtPercentage;

                // Calculates the total dirt that still needs to be removed to reach a clean state
                float dirtToRemove = 100 - initialDirt;

                // Calculate the target dirt percentage based on the percentage of time remaining.
                float targetDirtPercentage = dirtToRemove - (vehicleNeeds.ServiceTimer / RoadsideCareSettings.PassengerCarHandWashTimeInSeconds) * dirtToRemove;

                // Update the dirt level, ensuring it doesn't go below zero.
                float dirt_level = Mathf.Max(0, targetDirtPercentage);

                // Update the manager with the new dirt percentage.
                VehicleNeedsManager.SetDirtPercentage(vehicleID, dirt_level);
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

            var FuelingComplete = vehicleNeeds.IsRefueling && serviceTimer >= RoadsideCareSettings.PassengerCarFuelingTimeInSeconds;
            var HandWashComplete = vehicleNeeds.IsAtHandWash && serviceTimer >= RoadsideCareSettings.PassengerCarHandWashTimeInSeconds;

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

                if(data.Info.GetAI() is PassengerCarAI)
                {
                    var citizenId = instance.GetOwnerID(vehicleID, ref data).Citizen;
                    ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];
                    var citizenInstance = Singleton<CitizenManager>.instance.m_instances.m_buffer[citizen.m_instance];
                    var humanAI = citizen.GetCitizenInfo(citizenId).GetAI() as HumanAI;
                    humanAI.StartMoving(citizenId, ref citizen, citizenInstance.m_targetBuilding, targetBuilding);
                }

                if (data.Info.GetAI() is ExtendedCargoTruckAI extendedCargoTruckAI)
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
