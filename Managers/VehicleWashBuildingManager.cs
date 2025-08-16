using System.Collections.Generic;
using System.Linq;

namespace RoadsideCare.Managers
{
    public static class VehicleWashBuildingManager
    {
        private static Dictionary<ushort, VehicleWashStruct> VehicleWashBuildings;

        public struct VehicleWashStruct
        {
            public List<ushort> VehicleWashLanes;
        }

        public static void Init()
        {
            VehicleWashBuildings ??= [];
        }

        public static void Deinit()
        {
            VehicleWashBuildings = [];
        }

        public static Dictionary<ushort, VehicleWashStruct> GetVehicleWashBuildings() => VehicleWashBuildings;

        public static VehicleWashStruct GetVehicleWashBuilding(ushort buildingId) => VehicleWashBuildings.TryGetValue(buildingId, out var vehicleWashBuilding) ? vehicleWashBuilding : default;

        public static bool VehicleWashBuildingExist(ushort buildingId) => VehicleWashBuildings.ContainsKey(buildingId);

        public static VehicleWashStruct CreateVehicleWashBuilding(ushort buildingId, List<ushort> vehicleWashLanes)
        {
            var vehicleWashStruct = new VehicleWashStruct
            {
                VehicleWashLanes = vehicleWashLanes
            };

            VehicleWashBuildings.Add(buildingId, vehicleWashStruct);

            return vehicleWashStruct;
        }

        public static void RemoveVehicleWashBuilding(ushort buildingId)
        {
            if (VehicleWashBuildings.TryGetValue(buildingId, out var _))
            {
                VehicleWashBuildings.Remove(buildingId);
            }
        }

        public static void SetVehicleWashLanes(ushort buildingId, List<ushort> vehicleWashLanes)
        {
            if (VehicleWashBuildings.TryGetValue(buildingId, out var vehicleWashStruct))
            {
                vehicleWashStruct.VehicleWashLanes = vehicleWashLanes;
                VehicleWashBuildings[buildingId] = vehicleWashStruct;
            }
        }

        public static bool SegmentIdBelongsToAVehicleWashBuilding(ushort segmentID)
        {
            return VehicleWashBuildings.Values.Any(s => s.VehicleWashLanes.Any(v => v == segmentID));
        }

    }
}
