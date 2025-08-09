using System.Collections.Generic;

namespace RoadsideCare.Managers
{
    public static class GasStationManager
    {
        private static Dictionary<ushort, GasStationStruct> GasStationBuildings;

        public struct GasStationStruct
        {
            public int FuelAmount;
            public List<ushort> FuelLanes;
        }

        public static void Init()
        {
            GasStationBuildings ??= [];
        }

        public static void Deinit()
        {
            GasStationBuildings = [];
        }

        public static Dictionary<ushort, GasStationStruct> GetGasStationBuildings() => GasStationBuildings;

        public static GasStationStruct GetGasStationBuilding(ushort buildingId) => GasStationBuildings.TryGetValue(buildingId, out var gasStationBuilding) ? gasStationBuilding : default;

        public static bool GasStationBuildingExist(ushort buildingId) => GasStationBuildings.ContainsKey(buildingId);

        public static GasStationStruct CreateGasStationBuilding(ushort buildingId, int fuelAmount, List<ushort> fuelLanes)
        {
            var gasStationStruct = new GasStationStruct
            {
                FuelAmount = fuelAmount,
                FuelLanes = fuelLanes
            };

            GasStationBuildings.Add(buildingId, gasStationStruct);

            return gasStationStruct;
        }

        public static void RemoveGasStation(ushort buildingId)
        {
            if (GasStationBuildings.TryGetValue(buildingId, out var _))
            {
                GasStationBuildings.Remove(buildingId);
            }
        }


        public static void SetFuelAmount(ushort buildingId, int fuelAmount)
        {
            if (GasStationBuildings.TryGetValue(buildingId, out var gasStationStruct))
            {
                gasStationStruct.FuelAmount = fuelAmount;
                GasStationBuildings[buildingId] = gasStationStruct;
            }
        }

        public static void SetFuelLanes(ushort buildingId, List<ushort> fuelLanes)
        {
            if (GasStationBuildings.TryGetValue(buildingId, out var gasStationStruct))
            {
                gasStationStruct.FuelLanes = fuelLanes;
                GasStationBuildings[buildingId] = gasStationStruct;
            }
        }

    }
}
