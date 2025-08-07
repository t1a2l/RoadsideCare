using System.Collections.Generic;

namespace RoadsideCare.Managers
{
    public static class VehicleNeedsManager
    {
        private static Dictionary<ushort, VehicleNeedsStruct> VehiclesNeeds;

        private static Dictionary<ushort, ParkedVehicleNeedsStruct> ParkedVehiclesNeeds;

        public struct VehicleNeedsStruct
        {
            public ushort OriginalTargetBuilding;

            // fuel related
            public float FuelAmount;
            public float FuelCapacity;
            public bool IsRefueling;
            public bool IsGoingToRefuel;

            // dirt related
            public float DirtPercentage;
            public bool IsBeingWashed;
            public bool IsGoingToGetWashed;

            // wear related
            public float WearPercentage;
            public bool IsBeingRepaired;
            public bool IsGoingToGetRepaired;

            // car issues
            public bool IsBroken;
            public bool IsOutOfFuel;
        }

        public struct ParkedVehicleNeedsStruct
        {
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

        public static Dictionary<ushort, VehicleNeedsStruct> GetVehicleNeeds() => VehiclesNeeds;

        public static Dictionary<ushort, ParkedVehicleNeedsStruct> GetParkedVehicleNeeds() => ParkedVehiclesNeeds;

        public static VehicleNeedsStruct GetVehicleNeeds(ushort vehicleId) => VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeeds) ? vehicleNeeds : default;

        public static ParkedVehicleNeedsStruct GetParkedVehicleNeeds(ushort parkedVehicleId) => ParkedVehiclesNeeds.TryGetValue(parkedVehicleId, out var parkedVehicleNeeds) ? parkedVehicleNeeds : default;

        public static bool VehicleNeedsExist(ushort vehicleId) => VehiclesNeeds.ContainsKey(vehicleId);

        public static bool ParkedVehicleNeedsExist(ushort parkedVehicleId) => ParkedVehiclesNeeds.ContainsKey(parkedVehicleId);

        public static VehicleNeedsStruct CreateVehicleNeeds(ushort vehicleId, ushort originalTargetBuilding, float fuelAmount, float fuelCapacity, 
            float currentDirtPercentage, float currentWearPercenatge, bool isRefueling = false, bool isGoingToRefuel = false, bool isBeingWashed = false, 
            bool isGoingToGetWashed = false, bool isBeingRepaired = false, bool isGoingToGetRepaired = false, bool isBroken = false, bool isOutOfFuel = false)
        {
            var vehicleNeedsStruct = new VehicleNeedsStruct
            {
                FuelAmount = fuelAmount,
                FuelCapacity = fuelCapacity,
                OriginalTargetBuilding = originalTargetBuilding,
                IsRefueling = isRefueling,
                IsGoingToRefuel = isGoingToRefuel,
                DirtPercentage = currentDirtPercentage,
                IsBeingWashed = isBeingWashed,
                IsGoingToGetWashed = isGoingToGetWashed,
                WearPercentage = currentWearPercenatge,
                IsBeingRepaired = isBeingRepaired,
                IsGoingToGetRepaired = isGoingToGetRepaired,
                IsBroken = isBroken,
                IsOutOfFuel = isOutOfFuel
            };

            VehiclesNeeds.Add(vehicleId, vehicleNeedsStruct);

            return vehicleNeedsStruct;
        }

        public static ParkedVehicleNeedsStruct CreateParkedVehicleNeeds(ushort parkedVehicleId, float fuelAmount, float fuelCapacity, 
            float currentDirtPercentage, float currentWearPercenatge, bool isBroken = false, bool isOutOfFuel = false)
        {
            var parkedVehicleNeedsStruct = new ParkedVehicleNeedsStruct
            {
                FuelAmount = fuelAmount,
                FuelCapacity = fuelCapacity,
                DirtPercentage = currentDirtPercentage,
                WearPercentage = currentWearPercenatge,
                IsBroken = isBroken,
                IsOutOfFuel = isOutOfFuel
            };

            ParkedVehiclesNeeds.Add(parkedVehicleId, parkedVehicleNeedsStruct);

            return parkedVehicleNeedsStruct;
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

        public static void SetIsGoingToGetWashedMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsGoingToGetWashed = true;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

        public static void SetIsBeingWashedMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsBeingWashed = true;
                vehicleNeedsStruct.IsGoingToGetWashed = false;
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

        // clear going to building

        public static void SetNoneCareMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeedsStruct))
            {
                vehicleNeedsStruct.IsRefueling = false;
                vehicleNeedsStruct.IsBeingWashed = false;
                vehicleNeedsStruct.IsBeingRepaired = false;
                VehiclesNeeds[vehicleId] = vehicleNeedsStruct;
            }
        }

    }
}
