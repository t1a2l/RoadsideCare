using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using UnityEngine;

namespace RoadsideCare.Managers
{
    public static class VehicleNeedsManager
    {
        private static Dictionary<ushort, VehicleNeedsStruct> VehiclesNeeds;

        private static Dictionary<ushort, ParkedVehicleNeedsStruct> ParkedVehiclesNeeds;

        // Reuse this list to avoid allocations during cleanup
        private static readonly List<ushort> _tempStaleKeys = [];

        public struct VehicleNeedsStruct
        {
            public ushort OriginalTargetBuilding;
            public uint OwnerId;
            public float ServiceTimer;

            // fuel related
            public float FuelAmount;
            public float FuelCapacity;
            public float FuelPerFrame;

            // dirt related
            public float DirtPercentage;
            public float DirtPerFrame;

            // wear related
            public float WearPercentage;
            public float WearPerFrame;

            // Tunnel wash data
            public uint LastFrameIndex;
            public float TunnelWashSegmentLength;
            public float TunnelWashSegmentMaxSpeed;
            public float TunnelWashDistanceTraveled;
            public float TunnelWashDirtStartPercentage;
            public Vector3 TunnelWashStartPosition;
            public byte TunnelWashEntryOffset;
            public byte TunnelWashPreviousOffset;
            public ushort TunnelWashStartNode;
            public ushort TunnelWashEndNode;

            public VehicleStateFlags StateFlags;
        }

        [Flags]
        public enum VehicleStateFlags : uint
        {
            None = 0,
            IsRefueling = 1 << 0,
            IsGoingToRefuel = 1 << 1,
            IsAtTunnelWash = 1 << 2,
            IsAtTunnelWashExit = 1 << 3,
            IsGoingToTunnelWash = 1 << 4,
            IsAtHandWash = 1 << 5,
            IsGoingToHandWash = 1 << 6,
            IsBeingRepaired = 1 << 7,
            IsGoingToGetRepaired = 1 << 8,
            IsBroken = 1 << 9,
            IsOutOfFuel = 1 << 10,
            TunnelWashIsForwardDirection = 1 << 11,
            TunnelWashDirectionDetected = 1 << 12
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

            // cleanup frame
            public uint FrameIndex;

            // Pack the few boolean flags
            public ParkedVehicleStateFlags StateFlags;
        }

        [Flags]
        public enum ParkedVehicleStateFlags : uint
        {
            None = 0,
            IsBroken = 1 << 0,
            IsOutOfFuel = 1 << 1
        }

        public static void Init()
        {
            VehiclesNeeds ??= [];
            ParkedVehiclesNeeds ??= [];
        }

        public static void Deinit()
        {
            VehiclesNeeds?.Clear();
            ParkedVehiclesNeeds?.Clear();
            _tempStaleKeys.Clear();
        }

        public static Dictionary<ushort, VehicleNeedsStruct> GetVehiclesNeeds() => VehiclesNeeds;

        public static Dictionary<ushort, ParkedVehicleNeedsStruct> GetParkedVehiclesNeeds() => ParkedVehiclesNeeds;

        public static VehicleNeedsStruct GetVehicleNeeds(ushort vehicleId) => VehiclesNeeds.TryGetValue(vehicleId, out var vehicleNeeds) ? vehicleNeeds : default;

        public static ParkedVehicleNeedsStruct GetParkedVehicleNeeds(ushort parkedVehicleId) => ParkedVehiclesNeeds.TryGetValue(parkedVehicleId, out var parkedVehicleNeeds) ? parkedVehicleNeeds : default;

        public static bool VehicleNeedsExist(ushort vehicleId) => VehiclesNeeds.ContainsKey(vehicleId);

        public static bool ParkedVehicleNeedsExist(ushort parkedVehicleId) => ParkedVehiclesNeeds.ContainsKey(parkedVehicleId);

        // Helper methods for flag operations - makes the code cleaner
        public static bool HasFlag(ushort vehicleId, VehicleStateFlags flag)
        {
            return VehiclesNeeds.TryGetValue(vehicleId, out var needs) && (needs.StateFlags & flag) != 0;
        }

        public static void SetFlag(ushort vehicleId, VehicleStateFlags flag, bool value = true)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                if (value)
                    needs.StateFlags |= flag;
                else
                    needs.StateFlags &= ~flag;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        // Convenient property accessors using flags
        public static bool IsRefueling(ushort vehicleId) => HasFlag(vehicleId, VehicleStateFlags.IsRefueling);
        public static bool IsGoingToRefuel(ushort vehicleId) => HasFlag(vehicleId, VehicleStateFlags.IsGoingToRefuel);
        public static bool IsAtTunnelWash(ushort vehicleId) => HasFlag(vehicleId, VehicleStateFlags.IsAtTunnelWash);
        public static bool IsAtTunnelWashExit(ushort vehicleId) => HasFlag(vehicleId, VehicleStateFlags.IsAtTunnelWashExit);
        public static bool IsGoingToTunnelWash(ushort vehicleId) => HasFlag(vehicleId, VehicleStateFlags.IsGoingToTunnelWash);
        public static bool IsAtHandWash(ushort vehicleId) => HasFlag(vehicleId, VehicleStateFlags.IsAtHandWash);
        public static bool IsGoingToHandWash(ushort vehicleId) => HasFlag(vehicleId, VehicleStateFlags.IsGoingToHandWash);
        public static bool TunnelWashIsForwardDirection(ushort vehicleId) => HasFlag(vehicleId, VehicleStateFlags.TunnelWashIsForwardDirection);
        public static bool TunnelWashDirectionDetected(ushort vehicleId) => HasFlag(vehicleId, VehicleStateFlags.TunnelWashDirectionDetected);
        public static bool IsBeingRepaired(ushort vehicleId) => HasFlag(vehicleId, VehicleStateFlags.IsBeingRepaired);
        public static bool IsGoingToGetRepaired(ushort vehicleId) => HasFlag(vehicleId, VehicleStateFlags.IsGoingToGetRepaired);

        public static VehicleNeedsStruct CreateVehicleNeeds(ushort vehicleId, ushort originalTargetBuilding, uint ownerId, float serviceTimer, float fuelAmount, 
            float fuelCapacity, float dirtPercentage, float wearPercenatge)
        {
            var vehicleNeedsStruct = new VehicleNeedsStruct
            {
                OriginalTargetBuilding = originalTargetBuilding,
                OwnerId = ownerId,
                ServiceTimer = serviceTimer,
                FuelAmount = fuelAmount,
                FuelCapacity = fuelCapacity,
                DirtPercentage = dirtPercentage,
                WearPercentage = wearPercenatge
            };

            VehiclesNeeds.Add(vehicleId, vehicleNeedsStruct);

            return vehicleNeedsStruct;
        }

        public static ParkedVehicleNeedsStruct CreateParkedVehicleNeeds(ushort parkedVehicleId, uint ownerId, float fuelAmount, float fuelCapacity, 
            float dirtPercentage, float wearPercenatge, bool isBroken = false, bool isOutOfFuel = false)
        {
            var stateFlags = ParkedVehicleStateFlags.None;
            if (isBroken) stateFlags |= ParkedVehicleStateFlags.IsBroken;
            if (isOutOfFuel) stateFlags |= ParkedVehicleStateFlags.IsOutOfFuel;

            var parkedVehicleNeedsStruct = new ParkedVehicleNeedsStruct
            {
                OwnerId = ownerId,
                FuelAmount = fuelAmount,
                FuelCapacity = fuelCapacity,
                DirtPercentage = dirtPercentage,
                WearPercentage = wearPercenatge,
                FrameIndex = SimulationManager.instance.m_currentFrameIndex,
                StateFlags = stateFlags
            };

            ParkedVehiclesNeeds.Add(parkedVehicleId, parkedVehicleNeedsStruct);

            return parkedVehicleNeedsStruct;
        }

        // Optimized cleanup - reuse list to avoid allocations
        public static void CleanupStaleParkedVehicleNeeds()
        {
            _tempStaleKeys.Clear(); // Reuse the list
            var currentTime = SimulationManager.instance.m_currentFrameIndex;
            var staleThreshold = 300; // E.g., 300 frames, roughly 5 seconds

            foreach (var entry in ParkedVehiclesNeeds)
            {
                var citizenId = entry.Value.OwnerId;

                if(citizenId == 0)
                {
                    _tempStaleKeys.Add(entry.Key);
                    continue; // Move to the next entry
                }

                var NoParkedVehicle = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId].m_parkedVehicle == 0;

                if(NoParkedVehicle)
                {
                    // Check if the entry is too old based on frame index
                    if (currentTime - entry.Value.FrameIndex > staleThreshold)
                    {
                        _tempStaleKeys.Add(entry.Key);
                        continue; // Move to the next entry
                    }

                    // Check if the citizen still exists
                    if (CitizenManager.instance.m_citizens.m_buffer[citizenId].m_instance == 0)
                    {
                        _tempStaleKeys.Add(entry.Key);
                    }
                }      
            }

            // Remove the stale entries
            foreach (var key in _tempStaleKeys)
            {
                RemoveParkedVehicleNeeds(key);
            }
        }

        public static void RemoveVehicleNeeds(ushort vehicleId)
        {
            VehiclesNeeds.Remove(vehicleId);
        }

        public static void RemoveParkedVehicleNeeds(ushort parkedVehicleId)
        {
            ParkedVehiclesNeeds.Remove(parkedVehicleId);
        }

        public static KeyValuePair<ushort, VehicleNeedsStruct> FindVehicleWithNoOwner(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs) && needs.OwnerId == 0)
            {
                return new KeyValuePair<ushort, VehicleNeedsStruct>(vehicleId, needs);
            }
            return default;
        }

        public static KeyValuePair<ushort, ParkedVehicleNeedsStruct> FindParkedVehicleOwner(uint ownerId)
        {
            return ParkedVehiclesNeeds.FirstOrDefault(item => item.Value.OwnerId == ownerId);
        }

        public static void SetNewVehicleNeeds(ushort vehicleId, VehicleNeedsStruct vehicleNeeds)
        {
            VehiclesNeeds[vehicleId] = vehicleNeeds;
        }

        // Original target building related methods

        public static void SetOriginalTargetBuilding(ushort vehicleId, ushort originalTargetBuilding)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.OriginalTargetBuilding = originalTargetBuilding;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetServiceTimer(ushort vehicleId, float serviceTimer)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.ServiceTimer = serviceTimer;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        // Fuel related methods

        public static void SetFuelAmount(ushort vehicleId, float fuelAmount)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.FuelAmount = fuelAmount;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetFuelPerFrame(ushort vehicleId, float fuelPerFrame)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.FuelPerFrame = fuelPerFrame;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetIsGoingToRefuelMode(ushort vehicleId)
        {
            SetFlag(vehicleId, VehicleStateFlags.IsGoingToRefuel, true);
        }

        public static void SetIsRefuelingMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.StateFlags |= VehicleStateFlags.IsRefueling;
                needs.StateFlags &= ~VehicleStateFlags.IsGoingToRefuel;
                VehiclesNeeds[vehicleId] = needs;
            }
        }


        // Dirt related methods

        public static void SetDirtPercentage(ushort vehicleId, float dirtPercentage)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.DirtPercentage = dirtPercentage;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetParkedDirtPercentage(ushort parkedId, float dirtPercentage)
        {
            if (ParkedVehiclesNeeds.TryGetValue(parkedId, out var parkedNeeds))
            {
                parkedNeeds.DirtPercentage = dirtPercentage;
                ParkedVehiclesNeeds[parkedId] = parkedNeeds;
            }
        }

        public static void SetDirtPerFrame(ushort vehicleId, float dirtPerFrame)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.DirtPerFrame = dirtPerFrame;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetIsAtTunnelWashMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.StateFlags |= VehicleStateFlags.IsAtTunnelWash;
                needs.StateFlags &= ~VehicleStateFlags.IsGoingToTunnelWash;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetIsAtTunnelWashExitMode(ushort vehicleId)
        {
            SetFlag(vehicleId, VehicleStateFlags.IsAtTunnelWashExit, true);
        }

        public static void SetIsGoingToTunnelWashMode(ushort vehicleId)
        {
            SetFlag(vehicleId, VehicleStateFlags.IsGoingToTunnelWash, true);
        }

        public static void SetIsAtHandWashMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.StateFlags |= VehicleStateFlags.IsAtHandWash;
                needs.StateFlags &= ~VehicleStateFlags.IsGoingToHandWash;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetIsGoingToHandWashMode(ushort vehicleId)
        {
            SetFlag(vehicleId, VehicleStateFlags.IsGoingToHandWash, true);
        }

        public static void SetTunnelWashIsForwardDirection(ushort vehicleId, bool isForward)
        {
            SetFlag(vehicleId, VehicleStateFlags.TunnelWashIsForwardDirection, isForward);
        }

        public static void SetTunnelWashDirectionDetected(ushort vehicleId, bool detected)
        {
            SetFlag(vehicleId, VehicleStateFlags.TunnelWashDirectionDetected, detected);
        }

        // Tunnel wash property setters - keeping the same API
        public static void SetTunnelWashSegmentLength(ushort vehicleId, float length)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.TunnelWashSegmentLength = length;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetTunnelWashStartPosition(ushort vehicleId, Vector3 position)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.TunnelWashStartPosition = position;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetTunnelWashEntryOffset(ushort vehicleId, byte offset)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.TunnelWashEntryOffset = offset;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetTunnelWashPreviousOffset(ushort vehicleId, byte offset)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.TunnelWashPreviousOffset = offset;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetTunnelWashDirtStartPercentage(ushort vehicleId, float percentage)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.TunnelWashDirtStartPercentage = percentage;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetTunnelWashStartNode(ushort vehicleId, ushort startNode)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.TunnelWashStartNode = startNode;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetTunnelWashEndNode(ushort vehicleId, ushort endNode)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.TunnelWashEndNode = endNode;
                VehiclesNeeds[vehicleId] = needs;
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
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.StateFlags |= VehicleStateFlags.IsGoingToGetRepaired;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetIsBeingRepairedMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.StateFlags |= VehicleStateFlags.IsBeingRepaired;
                needs.StateFlags &= ~VehicleStateFlags.IsGoingToGetRepaired;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void SetIsBrokenMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.StateFlags |= VehicleStateFlags.IsBroken;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void ClearIsBrokenMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                needs.StateFlags &= ~VehicleStateFlags.IsBroken;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        // clear going to building or at building modes

        public static void ClearAtLocationMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                const VehicleStateFlags atLocationFlags =
                    VehicleStateFlags.IsRefueling |
                    VehicleStateFlags.IsAtTunnelWash |
                    VehicleStateFlags.IsAtTunnelWashExit |
                    VehicleStateFlags.IsAtHandWash |
                    VehicleStateFlags.IsBeingRepaired;

                needs.StateFlags &= ~atLocationFlags;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

        public static void ClearGoingToMode(ushort vehicleId)
        {
            if (VehiclesNeeds.TryGetValue(vehicleId, out var needs))
            {
                const VehicleStateFlags goingToFlags =
                    VehicleStateFlags.IsGoingToRefuel |
                    VehicleStateFlags.IsGoingToTunnelWash |
                    VehicleStateFlags.IsGoingToHandWash |
                    VehicleStateFlags.IsGoingToGetRepaired;

                needs.StateFlags &= ~goingToFlags;
                VehiclesNeeds[vehicleId] = needs;
            }
        }

    }
}
