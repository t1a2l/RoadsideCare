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
            public List<ushort> VehicleWashPoints;
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

        public static VehicleWashStruct CreateVehicleWashBuilding(ushort buildingId, List<ushort> vehicleWashLanes, List<ushort> vehicleWashPoints)
        {
            var vehicleWashStruct = new VehicleWashStruct
            {
                VehicleWashLanes = vehicleWashLanes,
                VehicleWashPoints = vehicleWashPoints
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

        public static void SetVehicleWashPoints(ushort buildingId, List<ushort> vehicleWashPoints)
        {
            if (VehicleWashBuildings.TryGetValue(buildingId, out var vehicleWashStruct))
            {
                vehicleWashStruct.VehicleWashPoints = vehicleWashPoints;
                VehicleWashBuildings[buildingId] = vehicleWashStruct;
            }
        }

        public static bool SegmentIdBelongsToAVehicleWashBuildingWithPoints(ushort segmentID)
        {
            return VehicleWashBuildings.Any(s => s.Value.VehicleWashPoints.Any(v => v == segmentID));
        }

        public static bool SegmentIdBelongsToAVehicleWashBuildingWithLanes(ushort segmentID)
        {
            return VehicleWashBuildings.Any(s => s.Value.VehicleWashLanes.Any(v => v == segmentID));
        }

        public static ushort GetSegmentIdVehicleWashBuildingByPoint(ushort segmentID)
        {
            return VehicleWashBuildings.FirstOrDefault(s => s.Value.VehicleWashPoints.Any(v => v == segmentID)).Key;
        }

        public static ushort GetSegmentIdVehicleWashBuildingByLane(ushort segmentID)
        {
            return VehicleWashBuildings.FirstOrDefault(s => s.Value.VehicleWashLanes.Any(v => v == segmentID)).Key;
        }
    }
}
