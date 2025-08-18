using System.Collections.Generic;
using System.Linq;
using ColossalFramework;

namespace RoadsideCare.Managers
{
    public static class VehicleNeedsManager
    {
        private static Dictionary<ushort, VehicleNeedsStruct> VehiclesNeeds;

        private static Dictionary<ushort, ParkedVehicleNeedsStruct> ParkedVehiclesNeeds;

        public struct VehicleNeedsStruct
        {
            public ushort OriginalTargetBuilding;
            public uint OwnerId;

            // fuel related
            public float FuelAmount;
            public float FuelCapacity;
            public float FuelPerFrame;
            public bool IsRefueling;
            public bool IsGoingToRefuel;

            // dirt related
            public float DirtPercentage;
            public float DirtPerFrame;
            public bool IsAtTunnelWash;
            public bool IsAtTunnelWashExit;
            public bool IsGoingToTunnelWash;
            public bool IsAtHandWash;
            public bool IsGoingToHandWash;

            // wear related
            public float WearPercentage;
            public float WearPerFrame;
            public bool IsBeingRepaired;
            public bool IsGoingToGetRepaired;

            // car issues
            public bool IsBroken;
            public bool IsOutOfFuel;
        }

        public struct ParkedVehicleNeedsStruct
        {
            public uint OwnerId;

            // fuel related
            public float FuelAmount;
            public float FuelCapacity;

            // dirt related
            public float DirtPercentage;

            // wear related
            public float WearPercentage;

            // car issues
            public bool IsBroken;
            public bool IsOutOfFuel;

            // cleanup frame
            public uint FrameIndex;
        }

        public static void Init()
        {
            VehiclesNeeds ??= [];
            ParkedVehiclesNeeds ??= [];
        }

        public static void Deinit()
        {
            VehiclesNeeds = [];
            ParkedVehiclesNeeds = [];
        }

        public static Dictionary<ushort, VehicleNeedsStruct> GetVehiclesNeeds() => VehiclesNeeds;

        public static Dictionary<ushort, ParkedVehicleNeedsStruct> GetParkedVehiclesNeeds() => ParkedVehiclesNeeds;

        public static VehicleNeedsStruct GetVehicleNeeds(ushort vehicleId) => VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeeds) ? vehicleNeeds : default;

        public static ParkedVehicleNeedsStruct GetParkedVehicleNeeds(ushort parkedVehicleId) => ParkedVehiclesNeeds.TryGetValue(parkedVehicleId, out var parkedVehicleNeeds) ? parkedVehicleNeeds : default;

        public static bool VehicleNeedsExist(ushort vehicleId) => VehiclesNeeds.ContainsKey(vehicleId);

        public static bool ParkedVehicleNeedsExist(ushort parkedVehicleId) => ParkedVehiclesNeeds.ContainsKey(parkedVehicleId);

        public static VehicleNeedsStruct CreateVehicleNeeds(ushort vehicleId, ushort originalTargetBuilding, uint ownerId, float fuelAmount, float fuelCapacity,
            float dirtPercentage, float wearPercenatge, float fuelPerFrame = 0, float dirtPerFrame = 0, float wearPerFrame = 0, bool isRefueling = false, 
            bool isGoingToRefuel = false, bool isAtTunnelWash = false, bool isAtTunnelWashExit = false, bool isGoingToTunnelWash = false, bool isAtHandWash = false, 
            bool isGoingToHandWash = false, bool isBeingRepaired = false, bool isGoingToGetRepaired = false, bool isBroken = false, bool isOutOfFuel = false)
        {
            var vehicleNeedsStruct = new VehicleNeedsStruct
            {
                OriginalTargetBuilding = originalTargetBuilding,
                OwnerId = ownerId,
                FuelAmount = fuelAmount,
                FuelCapacity = fuelCapacity,
                FuelPerFrame = fuelPerFrame,
                IsRefueling = isRefueling,
                IsGoingToRefuel = isGoingToRefuel,
                DirtPercentage = dirtPercentage,
                DirtPerFrame = dirtPerFrame,
                IsAtTunnelWash = isAtTunnelWash,
                IsAtTunnelWashExit = isAtTunnelWashExit,
                IsGoingToTunnelWash = isGoingToTunnelWash,
                IsAtHandWash = isAtHandWash,
                IsGoingToHandWash = isGoingToHandWash,
                WearPercentage = wearPercenatge,
                WearPerFrame = wearPerFrame,
                IsBeingRepaired = isBeingRepaired,
                IsGoingToGetRepaired = isGoingToGetRepaired,
                IsBroken = isBroken,
                IsOutOfFuel = isOutOfFuel
            };

            VehiclesNeeds.Add(vehicleId, vehicleNeedsStruct);

            return vehicleNeedsStruct;
        }

        public static ParkedVehicleNeedsStruct CreateParkedVehicleNeeds(ushort parkedVehicleId, uint ownerId, float fuelAmount, float fuelCapacity, 
            float dirtPercentage, float wearPercenatge, bool isBroken = false, bool isOutOfFuel = false)
        {
            var parkedVehicleNeedsStruct = new ParkedVehicleNeedsStruct
            {
                OwnerId = ownerId,
                FuelAmount = fuelAmount,
                FuelCapacity = fuelCapacity,
                DirtPercentage = dirtPercentage,
                WearPercentage = wearPercenatge,
                IsBroken = isBroken,
                IsOutOfFuel = isOutOfFuel,
                FrameIndex = SimulationManager.instance.m_currentFrameIndex
            };

            ParkedVehiclesNeeds.Add(parkedVehicleId, parkedVehicleNeedsStruct);

            return parkedVehicleNeedsStruct;
        }

        public static void CleanupStaleParkedVehicleNeeds()
        {
            var staleKeys = new List<ushort>();
            var currentTime = SimulationManager.instance.m_currentFrameIndex;
            var staleThreshold = 300; // E.g., 300 frames, roughly 5 seconds

            foreach (var entry in ParkedVehiclesNeeds)
            {
                var citizenId = entry.Value.OwnerId;

                var NoParkedVehicle = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId].m_parkedVehicle == 0;

                if(NoParkedVehicle)
                {
                    // Check if the entry is too old based on frame index
                    if (currentTime - entry.Value.FrameIndex > staleThreshold)
                    {
                        staleKeys.Add(entry.Key);
                        continue; // Move to the next entry
                    }

                    // Check if the citizen still exists
                    if (CitizenManager.instance.m_citizens.m_buffer[entry.Key].m_instance == 0)
                    {
                        staleKeys.Add(entry.Key);
                    }
                }      
            }

            // Remove the stale entries
            foreach (var key in staleKeys)
            {
                RemoveParkedVehicleNeeds(key);
            }
        }

        public static void RemoveVehicleNeeds(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var _))
            {
                VehiclesNeeds.Remove(vehicleId);
            }
        }

        public static void RemoveParkedVehicleNeeds(ushort parkedVehicleId)
        {
            if (ParkedVehiclesNeeds.TryGetValue(parkedVehicleId, out var _))
            {
                ParkedVehiclesNeeds.Remove(parkedVehicleId);
            }
        }

        public static KeyValuePair<ushort, VehicleNeedsStruct> FindVehicleWithNoOwner(ushort vehicleId)
        {
            return VehiclesNeeds.FirstOrDefault(item => item.Key == vehicleId && item.Value.OwnerId == 0);
        }

        public static KeyValuePair<ushort, ParkedVehicleNeedsStruct> FindParkedVehicleOwner(uint ownerId)
        {
            return ParkedVehiclesNeeds.FirstOrDefault(item => item.Value.OwnerId == ownerId);
        }

        public static void SetNewVehicleNeeds(ushort vehicleId, VehicleNeedsStruct vehicleNeeds)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var _))
            {
                VehiclesNeeds[vehicleId] = vehicleNeeds;
            }
        }

        // Original target building related methods

        public static void SetOriginalTargetBuilding(ushort vehicleId, ushort originalTargetBuilding)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.OriginalTargetBuilding = originalTargetBuilding;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        // Fuel related methods

        public static void SetFuelAmount(ushort vehicleId, float fuelAmount)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.FuelAmount = fuelAmount;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetFuelPerFrame(ushort vehicleId, float fuelPerFrame)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.FuelPerFrame = fuelPerFrame;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetIsGoingToRefuelMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsGoingToRefuel = true;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetIsRefuelingMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsRefueling = true;
                vehicleNeedsStruct.IsGoingToRefuel = false;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetIsOutOfFuelMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsOutOfFuel = true;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void ClearIsOutOfFuelMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsOutOfFuel = false;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        // Dirt related methods

        public static void SetDirtPercentage(ushort vehicleId, float dirtPercentage)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.DirtPercentage = dirtPercentage;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetDirtPerFrame(ushort vehicleId, float dirtPerFrame)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.DirtPerFrame = dirtPerFrame;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetIsAtTunnelWashMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsAtTunnelWash = true;
                vehicleNeedsStruct.IsGoingToTunnelWash = false;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetIsAtTunnelWashExitMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsAtTunnelWashExit = true;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetIsGoingToTunnelWashMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsGoingToTunnelWash = true;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetIsAtHandWashMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsAtHandWash = true;
                vehicleNeedsStruct.IsGoingToHandWash = false;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetIsGoingToHandWashMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsGoingToHandWash = true;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        // Wear related methods

        public static void SetWearPercentage(ushort vehicleId, float wearPercentage)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.WearPercentage = wearPercentage;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetWearPerFrame(ushort vehicleId, float wearPerFrame)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.WearPerFrame = wearPerFrame;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetIsGoingToGetRepairedMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsGoingToGetRepaired = true;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetIsBeingRepairedMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsBeingRepaired = true;
                vehicleNeedsStruct.IsGoingToGetRepaired = false;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetIsBrokenMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsBroken = true;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void ClearIsBrokenMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsBroken = false;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        // clear going to building or at building modes

        public static void ClearAtLocationMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsRefueling = false;
                vehicleNeedsStruct.IsAtTunnelWash = false;
                vehicleNeedsStruct.IsAtTunnelWashExit = false;
                vehicleNeedsStruct.IsAtHandWash = false;
                vehicleNeedsStruct.IsBeingRepaired = false;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void ClearGoingToMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsGoingToRefuel = false;
                vehicleNeedsStruct.IsGoingToTunnelWash = false;
                vehicleNeedsStruct.IsGoingToHandWash = false;
                vehicleNeedsStruct.IsGoingToGetRepaired = false;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

    }
}
