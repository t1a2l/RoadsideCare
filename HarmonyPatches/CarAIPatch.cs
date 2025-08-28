using System;
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
                data.m_flags |= Vehicle.Flags.Stopped;
                data.m_flags |= Vehicle.Flags.WaitingPath;
                data.m_blockCounter = 0;

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
                data.m_flags |= Vehicle.Flags.Stopped;
                data.m_flags |= Vehicle.Flags.WaitingPath;
                data.m_blockCounter = 0;

                var newAmount = vehicleNeeds.DirtPercentage - vehicleNeeds.DirtPerFrame;

                // Update the manager with the new dirt percentage.
                VehicleNeedsManager.SetDirtPercentage(vehicleID, newAmount);
            }

            if (vehicleNeeds.IsGoingToTunnelWash)
            {
                ref var building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_targetBuilding];

                float distance = Vector3.Distance(data.GetLastFramePosition(), building.m_position);

                byte b = data.m_pathPositionIndex;

                PathManager.instance.m_pathUnits.m_buffer[data.m_path].GetPosition(b >> 1, out var position);

                ref var segment = ref Singleton<NetManager>.instance.m_segments.m_buffer[position.m_segment];

                var isVehicleWashSegment = segment.Info.GetAI() is VehicleWashLaneAI || segment.Info.GetAI() is VehicleWashLaneSmallAI || segment.Info.GetAI() is VehicleWashLaneLargeAI;

                if (isVehicleWashSegment && distance < 80f)
                {
                    Vector3 segmentStartPos = NetManager.instance.m_nodes.m_buffer[segment.m_startNode].m_position;
                    Vector3 segmentEndPos = NetManager.instance.m_nodes.m_buffer[segment.m_endNode].m_position;

                    Vector3 actualVehiclePos = data.GetLastFramePosition();

                    // Check which end the vehicle is closer to
                    float distanceToStart = Vector3.Distance(actualVehiclePos, segmentStartPos);
                    float distanceToEnd = Vector3.Distance(actualVehiclePos, segmentEndPos);

                    Debug.Log($"distanceToStart: {distanceToStart}");
                    Debug.Log($"distanceToEnd: {distanceToEnd}");

                    Vector3 entryPoint;
                    float proximityThreshold = 10f;

                    if (distanceToStart < distanceToEnd)
                    {
                        // Vehicle approaching from start node
                        if (distanceToStart < proximityThreshold)
                        {
                            entryPoint = segmentStartPos;
                            Debug.Log("Vehicle entering from START node");
                        }
                        else return; // Too far from entry
                    }
                    else
                    {
                        // Vehicle approaching from end node  
                        if (distanceToEnd < proximityThreshold)
                        {
                            entryPoint = segmentEndPos;
                            Debug.Log("Vehicle entering from END node");
                        }
                        else return; // Too far from entry
                    }

                    float actualSegmentLength = Vector3.Distance(segmentStartPos, segmentEndPos);

                    Debug.Log($"SegmentLength: {actualSegmentLength}");
                    Debug.Log($"DirtStartPercentage: {vehicleNeeds.DirtPercentage}");
                    Debug.Log($"StartPosition: {entryPoint}");
                    Debug.Log($"EntryOffset: {position.m_offset}");

                    VehicleNeedsManager.SetIsAtTunnelWashMode(vehicleID);

                    VehicleNeedsManager.SetTunnelWashSegmentLength(vehicleID, actualSegmentLength);

                    VehicleNeedsManager.SetTunnelWashDirtStartPercentage(vehicleID, vehicleNeeds.DirtPercentage);

                    VehicleNeedsManager.SetTunnelWashStartPosition(vehicleID, entryPoint);

                    VehicleNeedsManager.SetTunnelWashEntryOffset(vehicleID, position.m_offset);

                    VehicleNeedsManager.SetTunnelWashPreviousOffset(vehicleID, position.m_offset);

                    VehicleNeedsManager.SetTunnelWashDirectionDetected(vehicleID, false);

                    VehicleNeedsManager.SetTunnelWashStartNode(vehicleID, segment.m_startNode);

                    VehicleNeedsManager.SetTunnelWashEndNode(vehicleID, segment.m_endNode);

                    return;
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

                Debug.Log($"vehicleID: {vehicleID}");

                byte b = data.m_pathPositionIndex;

                PathManager.instance.m_pathUnits.m_buffer[data.m_path].GetPosition(b >> 1, out var position);

                var laneId = PathManager.GetLaneID(position);

                CalculateSegmentPosition(instance, vehicleID, ref data, position, laneId, data.m_lastPathOffset, out var currentPosition, out var _, out var maxSpeed);

                var currentOffset = data.m_lastPathOffset;

                // Detect direction on first movement
                if (!vehicleNeeds.TunnelWashDirectionDetected && data.m_lastPathOffset != vehicleNeeds.TunnelWashPreviousOffset)
                {
                    DetectDirection(vehicleID, data.m_lastPathOffset);
                    VehicleNeedsManager.SetTunnelWashStartPosition(vehicleID, currentPosition);
                    Debug.Log($"Direction detected, resetting entry position to: {currentPosition}");
                }

                vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);

                // Calculate progress (0.0 to 1.0)
                float progressRatio = CalculateProgress(vehicleID, currentPosition, currentOffset);

                // Reduce dirt based on progress
                var currentDirt = vehicleNeeds.TunnelWashDirtStartPercentage * (1f - progressRatio);

                Debug.Log($"Offset: {currentOffset}, Progress: {progressRatio:F2}, Dirt: {currentDirt:F1}");

                VehicleNeedsManager.SetTunnelWashPreviousOffset(vehicleID, currentOffset);

                VehicleNeedsManager.SetDirtPercentage(vehicleID, currentDirt);

                if (progressRatio >= 1.0f || currentDirt <= 0.01f)
                {
                    TunnelWashComplete = true;
                }
            }

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

        private static void CalculateSegmentPosition(VehicleAI v_instance, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position position, uint laneID, byte offset, out Vector3 pos, out Vector3 dir, out float maxSpeed)
        {
            NetManager instance = Singleton<NetManager>.instance;
            instance.m_lanes.m_buffer[laneID].CalculatePositionAndDirection((float)(int)offset * 0.003921569f, out pos, out dir);
            NetInfo info = instance.m_segments.m_buffer[position.m_segment].Info;
            if (info.m_lanes != null && info.m_lanes.Length > position.m_lane)
            {
                maxSpeed = CalculateTargetSpeed(v_instance, vehicleID, ref vehicleData, info, position.m_lane, instance.m_lanes.m_buffer[laneID].m_curve);
            }
            else
            {
                maxSpeed = CalculateTargetSpeed(v_instance, vehicleID, ref vehicleData, 1f, 0f);
            }
        }

        private static float CalculateTargetSpeed(VehicleAI instance, ushort vehicleID, ref Vehicle data, NetInfo info, uint lane, float curve)
        {
            float num = ((lane >= info.m_lanes.Length) ? 1f : info.m_lanes[lane].m_speedLimit);
            if (num > 0.4f && (instance.vehicleCategory & VehicleInfo.VehicleCategory.RoadTransport) != 0 && !info.m_netAI.IsHighway() && !info.m_netAI.IsTunnel() && !info.IsPedestrianZoneOrPublicTransportRoad())
            {
                Vector3 lastFramePosition = data.GetLastFramePosition();
                byte park = Singleton<DistrictManager>.instance.GetPark(lastFramePosition);
                if (park != 0 && (Singleton<DistrictManager>.instance.m_parks.m_buffer[park].m_parkPolicies & DistrictPolicies.Park.SlowDriving) != DistrictPolicies.Park.None)
                {
                    num = 0.4f;
                }
            }
            return CalculateTargetSpeed(instance, vehicleID, ref data, num, curve);
        }

        private static float CalculateTargetSpeed(VehicleAI instance, ushort vehicleID, ref Vehicle data, float speedLimit, float curve)
        {
            float a = 1000f / (1f + curve * 1000f / instance.m_info.m_turning) + 2f;
            float b = 8f * speedLimit;
            return Mathf.Min(Mathf.Min(a, b), instance.m_info.m_maxSpeed);
        }

        private static void DetectDirection(ushort vehicleID, byte currentOffset)
        {
            var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);
            bool isForwardDirection = vehicleNeeds.TunnelWashIsForwardDirection;

            // Check if offset wrapped around (0 to 255 or 255 to 0)
            int offsetDifference = currentOffset - vehicleNeeds.TunnelWashPreviousOffset;

            // Handle wraparound cases
            if (offsetDifference > 127)
            {
                // Wrapped from high to low (e.g., 254 -> 2), moving backward
                isForwardDirection = false;
                Debug.Log("Direction: BACKWARD (offset decreasing with wraparound)");
            }
            else if (offsetDifference < -127)
            {
                // Wrapped from low to high (e.g., 2 -> 254), moving forward
                isForwardDirection = true;
                Debug.Log("Direction: FORWARD (offset increasing with wraparound)");
            }
            else if (offsetDifference > 0)
            {
                // Normal forward movement
                isForwardDirection = true;
                Debug.Log("Direction: FORWARD (offset increasing)");
            }
            else if (offsetDifference < 0)
            {
                // Normal backward movement
                isForwardDirection = false;
                Debug.Log("Direction: BACKWARD (offset decreasing)");
            }

            VehicleNeedsManager.SetTunnelWashIsForwardDirection(vehicleID, isForwardDirection);

            VehicleNeedsManager.SetTunnelWashDirectionDetected(vehicleID, true);
        }

        private static float CalculateProgress(ushort vehicleID, Vector3 currentPosition, byte currentOffset)
        {
            var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);

            Debug.Log($"currentPosition: {currentPosition}");

            // Calculate how far the vehicle should travel (from entry to opposite end)
            Vector3 segmentStartPos = NetManager.instance.m_nodes.m_buffer[vehicleNeeds.TunnelWashStartNode].m_position;
            Vector3 segmentEndPos = NetManager.instance.m_nodes.m_buffer[vehicleNeeds.TunnelWashEndNode].m_position;

            Debug.Log($"TunnelWashStartNode: {vehicleNeeds.TunnelWashStartNode}");
            Debug.Log($"TunnelWashEndNode: {vehicleNeeds.TunnelWashEndNode}");
            Debug.Log($"segmentStartPos: {segmentStartPos}");
            Debug.Log($"segmentEndPos: {segmentEndPos}");

            // Determine which direction vehicle is traveling
            Vector3 targetEndPoint;
            if (Vector3.Distance(vehicleNeeds.TunnelWashStartPosition, segmentStartPos) < Vector3.Distance(vehicleNeeds.TunnelWashStartPosition, segmentEndPos))
            {
                // Entered from start, traveling toward end
                targetEndPoint = segmentEndPos;
            }
            else
            {
                // Entered from end, traveling toward start  
                targetEndPoint = segmentStartPos;
            }

            Debug.Log($"distanceFromStart: {Vector3.Distance(vehicleNeeds.TunnelWashStartPosition, segmentStartPos)}");
            Debug.Log($"distanceFromEnd: {Vector3.Distance(vehicleNeeds.TunnelWashStartPosition, segmentEndPos)}");
            Debug.Log($"targetEndPoint: {targetEndPoint}");

            // Calculate progress based on distance from entry toward target
            float distanceFromEntry = Vector3.Distance(vehicleNeeds.TunnelWashStartPosition, currentPosition);
            float totalDistanceToTravel = Vector3.Distance(vehicleNeeds.TunnelWashStartPosition, targetEndPoint);

            float distanceProgress = distanceFromEntry / totalDistanceToTravel;

            // Use offset only as debug info
            float offsetProgress = 0f;
            if (vehicleNeeds.TunnelWashDirectionDetected)
            {
                offsetProgress = CalculateOffsetProgress(vehicleID, currentOffset);
            }

            Console.WriteLine($"Distance from entry: {distanceFromEntry:F2}m, Total to travel: {totalDistanceToTravel:F2}m, Progress: {distanceProgress:F2}, Offset Progress: {offsetProgress:F2}");

            return Math.Min(distanceProgress, 1.0f);
        }

        private static float CalculateOffsetProgress(ushort vehicleID, byte currentOffset)
        {
            var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);

            // Calculate the total distance the vehicle should travel in this direction
            float totalDistance;
            float currentDistance;

            if (vehicleNeeds.TunnelWashIsForwardDirection)
            {
                // Forward: calculate how far we can go from entry point
                // Handle wraparound case
                if (currentOffset < vehicleNeeds.TunnelWashEntryOffset)
                {
                    // Wrapped around (e.g., entry=250, current=10)
                    totalDistance = (255 - vehicleNeeds.TunnelWashEntryOffset) + currentOffset;
                    currentDistance = totalDistance; // Already traveled the full distance
                }
                else
                {
                    // Normal case
                    totalDistance = 255 - vehicleNeeds.TunnelWashEntryOffset;
                    currentDistance = currentOffset - vehicleNeeds.TunnelWashEntryOffset;
                }
            }
            else
            {
                // Backward: calculate how far we can go from entry point
                // Handle wraparound case
                if (currentOffset > vehicleNeeds.TunnelWashEntryOffset)
                {
                    // Wrapped around (e.g., entry=10, current=250)
                    totalDistance = vehicleNeeds.TunnelWashEntryOffset + (255 - currentOffset);
                    currentDistance = totalDistance; // Already traveled the full distance
                }
                else
                {
                    // Normal case
                    totalDistance = vehicleNeeds.TunnelWashEntryOffset;
                    currentDistance = vehicleNeeds.TunnelWashEntryOffset - currentOffset;
                }
            }

            if (totalDistance <= 0) return 1.0f;

            float progress = currentDistance / totalDistance;
            return Math.Min(Math.Max(0f, progress), 1.0f);
        }
    }
}
