using System.Collections.Generic;
using System.Linq;

namespace RoadsideCare.Managers
{
    public static class GasStationManager
    {
        private static Dictionary<ushort, GasStationStruct> GasStationBuildings;

        public struct GasStationStruct
        {
            public int FuelAmount;
            public List<ushort> FuelPoints;
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

        public static GasStationStruct CreateGasStationBuilding(ushort buildingId, int fuelAmount, List<ushort> fuelPoints)
        {
            var gasStationStruct = new GasStationStruct
            {
                FuelAmount = fuelAmount,
                FuelPoints = fuelPoints
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

        public static void SetFuelPoints(ushort buildingId, List<ushort> fuelPoints)
        {
            if (GasStationBuildings.TryGetValue(buildingId, out var gasStationStruct))
            {
                gasStationStruct.FuelPoints = fuelPoints;
                GasStationBuildings[buildingId] = gasStationStruct;
            }
        }

        public static bool SegmentIdBelongsToAGasStation(ushort segmentID)
        {
            return GasStationBuildings.Any(s => s.Value.FuelPoints.Any(v => v == segmentID));
        }

        public static ushort GetSegmentIdGasStation(ushort segmentID)
        {
            return GasStationBuildings.FirstOrDefault(s => s.Value.FuelPoints.Any(v => v == segmentID)).Key;
        }
    }
}
