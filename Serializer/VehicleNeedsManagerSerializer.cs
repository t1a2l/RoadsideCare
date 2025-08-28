using RoadsideCare.Managers;
using System;
using UnityEngine;

namespace RoadsideCare.Serializer
{
    public class VehicleNeedsManagerSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iVEHICLE_NEEDS_MANAGER_DATA_VERSION = 2;

        public static void SaveData(FastList<byte> Data)
        {
            var VehiclesNeeds = VehicleNeedsManager.GetVehiclesNeeds();

            var ParkedVehiclesNeeds = VehicleNeedsManager.GetParkedVehiclesNeeds();

            // Write out metadata
            StorageData.WriteUInt16(iVEHICLE_NEEDS_MANAGER_DATA_VERSION, Data);
            Debug.Log("iVEHICLE_NEEDS_MANAGER_DATA_VERSION: " + iVEHICLE_NEEDS_MANAGER_DATA_VERSION);

            StorageData.WriteInt32(VehiclesNeeds.Count, Data);
            Debug.Log("VehicleNeedsManager.VehiclesNeeds.Count: " + VehiclesNeeds.Count);

            // Write out each buffer settings
            foreach (var kvp in VehiclesNeeds)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt16(kvp.Key, Data);

                // Original target building related
                StorageData.WriteUInt16(kvp.Value.OriginalTargetBuilding, Data);

                // Owner related
                StorageData.WriteUInt32(kvp.Value.OwnerId, Data);

                // Service Timer related
                StorageData.WriteFloat(kvp.Value.ServiceTimer, Data);

                // Fuel related
                StorageData.WriteFloat(kvp.Value.FuelAmount, Data);
                StorageData.WriteFloat(kvp.Value.FuelCapacity, Data);
                StorageData.WriteFloat(kvp.Value.FuelPerFrame, Data);

                // Dirt related
                StorageData.WriteFloat(kvp.Value.DirtPercentage, Data);
                StorageData.WriteFloat(kvp.Value.DirtPerFrame, Data);
                StorageData.WriteUInt32(kvp.Value.LastFrameIndex, Data);
                StorageData.WriteFloat(kvp.Value.TunnelWashSegmentLength, Data);
                StorageData.WriteFloat(kvp.Value.TunnelWashSegmentMaxSpeed, Data);
                StorageData.WriteFloat(kvp.Value.TunnelWashDistanceTraveled, Data);
                StorageData.WriteFloat(kvp.Value.TunnelWashDirtStartPercentage, Data);
                StorageData.WriteFloat(kvp.Value.TunnelWashStartPosition.x, Data);
                StorageData.WriteFloat(kvp.Value.TunnelWashStartPosition.y, Data);
                StorageData.WriteFloat(kvp.Value.TunnelWashStartPosition.z, Data);
                StorageData.WriteByte(kvp.Value.TunnelWashEntryOffset, Data);
                StorageData.WriteByte(kvp.Value.TunnelWashPreviousOffset, Data);
                StorageData.WriteBool(kvp.Value.TunnelWashIsForwardDirection, Data);
                StorageData.WriteBool(kvp.Value.TunnelWashDirectionDetected, Data);
                StorageData.WriteUInt16(kvp.Value.TunnelWashStartNode, Data);
                StorageData.WriteUInt16(kvp.Value.TunnelWashEndNode, Data);

                // Wear related
                StorageData.WriteFloat(kvp.Value.WearPercentage, Data);
                StorageData.WriteFloat(kvp.Value.WearPerFrame, Data);

                // NEW: Write the packed flags as a single UInt32
                StorageData.WriteUInt32((uint)kvp.Value.StateFlags, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }

            StorageData.WriteInt32(ParkedVehiclesNeeds.Count, Data);

            foreach (var kvp in ParkedVehiclesNeeds)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt16(kvp.Key, Data);

                // Fuel related
                StorageData.WriteFloat(kvp.Value.FuelAmount, Data);
                StorageData.WriteFloat(kvp.Value.FuelCapacity, Data);

                // Dirt related
                StorageData.WriteFloat(kvp.Value.DirtPercentage, Data);

                // Wear related
                StorageData.WriteFloat(kvp.Value.WearPercentage, Data);

                // NEW: Write the packed flags as a single UInt32
                StorageData.WriteUInt32((uint)kvp.Value.StateFlags, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iVehicleNeedsManagerVersion = StorageData.ReadUInt16(Data, ref iIndex);

                Debug.Log("RoadsideCare VehiclesNeeds - Global: " + iGlobalVersion + " BufferVersion: " + iVehicleNeedsManagerVersion + " DataLength: " + Data.Length + " Index: " + iIndex);
            
                VehicleNeedsManager.Init();

                int VehiclesFuel_Count = StorageData.ReadInt32(Data, ref iIndex);

                for (int i = 0; i < VehiclesFuel_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iVehicleNeedsManagerVersion, Data, ref iIndex);

                    ushort vehicleId = StorageData.ReadUInt16(Data, ref iIndex);

                    // Original target building related
                    ushort originalTargetBuilding = StorageData.ReadUInt16(Data, ref iIndex);

                    // Owner related
                    uint ownerId = StorageData.ReadUInt32(Data, ref iIndex);

                    // Service Timer related
                    float serviceTimer = StorageData.ReadFloat(Data, ref iIndex);

                    // Fuel related
                    float fuelAmount = StorageData.ReadFloat(Data, ref iIndex);
                    float fuelCapacity = StorageData.ReadFloat(Data, ref iIndex);
                    float fuelPerFrame = StorageData.ReadFloat(Data, ref iIndex);

                    // Initialize boolean flags -will be overridden by flags if version >= 3
                    bool isRefueling = false;
                    bool isGoingToRefuel = false;
                    bool isAtTunnelWash = false;
                    bool isAtTunnelWashExit = false;
                    bool isGoingToTunnelWash = false;
                    bool isAtHandWash = false;
                    bool isGoingToHandWash = false;
                    bool tunnelWashIsForwardDirection = true;
                    bool tunnelWashDirectionDetected = false;
                    bool isGoingToGetRepaired = false;
                    bool isBeingRepaired = false;
                    bool isBroken = false;
                    bool isOutOfFuel = false;

                    // Dirt related
                    float dirtPercentage = StorageData.ReadFloat(Data, ref iIndex);
                    float dirtPerFrame = StorageData.ReadFloat(Data, ref iIndex);

                    uint lastFrameIndex = 0;
                    float tunnelWashSegmentLength = 1;
                    float tunnelWashSegmentMaxSpeed = 1;
                    float tunnelWashDistanceTraveled = 1;
                    float tunnelWashDirtStartPercentage = 1;
                    float tunnelWashStartPosition_x = 0;
                    float tunnelWashStartPosition_y = 0;
                    float tunnelWashStartPosition_z = 0;
                    byte tunnelWashEntryOffset = 0;
                    byte tunnelWashPreviousOffset = 0;
                    ushort tunnelWashStartNode = 0;
                    ushort tunnelWashEndNode = 0;

                    if (iVehicleNeedsManagerVersion >= 2)
                    {
                        lastFrameIndex = StorageData.ReadUInt32(Data, ref iIndex);

                        tunnelWashSegmentLength = StorageData.ReadFloat(Data, ref iIndex);

                        tunnelWashSegmentMaxSpeed = StorageData.ReadFloat(Data, ref iIndex);

                        tunnelWashDistanceTraveled = StorageData.ReadFloat(Data, ref iIndex);

                        tunnelWashDirtStartPercentage = StorageData.ReadFloat(Data, ref iIndex);

                        tunnelWashStartPosition_x = StorageData.ReadFloat(Data, ref iIndex);

                        tunnelWashStartPosition_y = StorageData.ReadFloat(Data, ref iIndex);

                        tunnelWashStartPosition_z = StorageData.ReadFloat(Data, ref iIndex);

                        tunnelWashEntryOffset = StorageData.ReadByte(Data, ref iIndex);

                        tunnelWashPreviousOffset = StorageData.ReadByte(Data, ref iIndex);

                        tunnelWashStartNode = StorageData.ReadUInt16(Data, ref iIndex);

                        tunnelWashEndNode = StorageData.ReadUInt16(Data, ref iIndex);
                    }

                    Vector3 tunnelWashStartPosition = new(tunnelWashStartPosition_x, tunnelWashStartPosition_y, tunnelWashStartPosition_z);

                    // Wear related
                    float wearPercentage = StorageData.ReadFloat(Data, ref iIndex);
                    float wearPerFrame = StorageData.ReadFloat(Data, ref iIndex);

                    // NEW: Read packed flags for version 3+
                    VehicleNeedsManager.VehicleStateFlags stateFlags = VehicleNeedsManager.VehicleStateFlags.None;
                    if (iVehicleNeedsManagerVersion >= 3)
                    {
                        stateFlags = (VehicleNeedsManager.VehicleStateFlags)StorageData.ReadUInt32(Data, ref iIndex);
                    }
                    else
                    {
                        // Convert individual booleans to flags (for legacy compatibility)
                        if (isRefueling) stateFlags |= VehicleNeedsManager.VehicleStateFlags.IsRefueling;
                        if (isGoingToRefuel) stateFlags |= VehicleNeedsManager.VehicleStateFlags.IsGoingToRefuel;
                        if (isAtTunnelWash) stateFlags |= VehicleNeedsManager.VehicleStateFlags.IsAtTunnelWash;
                        if (isAtTunnelWashExit) stateFlags |= VehicleNeedsManager.VehicleStateFlags.IsAtTunnelWashExit;
                        if (isGoingToTunnelWash) stateFlags |= VehicleNeedsManager.VehicleStateFlags.IsGoingToTunnelWash;
                        if (isAtHandWash) stateFlags |= VehicleNeedsManager.VehicleStateFlags.IsAtHandWash;
                        if (isGoingToHandWash) stateFlags |= VehicleNeedsManager.VehicleStateFlags.IsGoingToHandWash;
                        if (isBeingRepaired) stateFlags |= VehicleNeedsManager.VehicleStateFlags.IsBeingRepaired;
                        if (isGoingToGetRepaired) stateFlags |= VehicleNeedsManager.VehicleStateFlags.IsGoingToGetRepaired;
                        if (isBroken) stateFlags |= VehicleNeedsManager.VehicleStateFlags.IsBroken;
                        if (isOutOfFuel) stateFlags |= VehicleNeedsManager.VehicleStateFlags.IsOutOfFuel;
                        if (tunnelWashIsForwardDirection) stateFlags |= VehicleNeedsManager.VehicleStateFlags.TunnelWashIsForwardDirection;
                        if (tunnelWashDirectionDetected) stateFlags |= VehicleNeedsManager.VehicleStateFlags.TunnelWashDirectionDetected;
                    }

                    if (!VehicleNeedsManager.VehicleNeedsExist(vehicleId))
                    {
                        // Create the vehicle needs using the new method signature
                        var vehicleNeeds = new VehicleNeedsManager.VehicleNeedsStruct
                        {
                            OriginalTargetBuilding = originalTargetBuilding,
                            OwnerId = ownerId,
                            ServiceTimer = serviceTimer,
                            FuelAmount = fuelAmount,
                            FuelCapacity = fuelCapacity,
                            FuelPerFrame = fuelPerFrame,
                            DirtPercentage = dirtPercentage,
                            DirtPerFrame = dirtPerFrame,
                            LastFrameIndex = lastFrameIndex,
                            TunnelWashSegmentLength = tunnelWashSegmentLength,
                            TunnelWashSegmentMaxSpeed = tunnelWashSegmentMaxSpeed,
                            TunnelWashDistanceTraveled = tunnelWashDistanceTraveled,
                            TunnelWashDirtStartPercentage = tunnelWashDirtStartPercentage,
                            TunnelWashStartPosition = tunnelWashStartPosition,
                            TunnelWashEntryOffset = tunnelWashEntryOffset,
                            TunnelWashPreviousOffset = tunnelWashPreviousOffset,
                            TunnelWashStartNode = tunnelWashStartNode,
                            TunnelWashEndNode = tunnelWashEndNode,
                            WearPercentage = wearPercentage,
                            WearPerFrame = wearPerFrame,
                            StateFlags = stateFlags
                        };

                        // Add to the manager
                        VehicleNeedsManager.GetVehiclesNeeds()[vehicleId] = vehicleNeeds;
                    }

                    CheckEndTuple($"Buffer({i})", iVehicleNeedsManagerVersion, Data, ref iIndex);
                }

                int ParkedVehiclesFuel_Count = StorageData.ReadInt32(Data, ref iIndex);

                for (int i = 0; i < ParkedVehiclesFuel_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iVehicleNeedsManagerVersion, Data, ref iIndex);

                    ushort parkedVehicleId = StorageData.ReadUInt16(Data, ref iIndex);

                    // Owner related
                    uint ownerId = StorageData.ReadUInt32(Data, ref iIndex);

                    // Fuel related
                    float fuelAmount = StorageData.ReadFloat(Data, ref iIndex);

                    float fuelCapacity = StorageData.ReadFloat(Data, ref iIndex);

                    // Dirt related
                    float dirtPercentage = StorageData.ReadFloat(Data, ref iIndex);

                    // Wear related
                    float wearPercentage = StorageData.ReadFloat(Data, ref iIndex);

                    uint frameIndex = 0;
                    if (iVehicleNeedsManagerVersion >= 2)
                    {
                        frameIndex = StorageData.ReadUInt32(Data, ref iIndex);
                    }

                    VehicleNeedsManager.ParkedVehicleStateFlags stateFlags = (VehicleNeedsManager.ParkedVehicleStateFlags)StorageData.ReadUInt32(Data, ref iIndex);

                    if (!VehicleNeedsManager.ParkedVehicleNeedsExist(parkedVehicleId))
                    {
                        var parkedVehicleNeeds = new VehicleNeedsManager.ParkedVehicleNeedsStruct
                        {
                            OwnerId = ownerId,
                            FuelAmount = fuelAmount,
                            FuelCapacity = fuelCapacity,
                            DirtPercentage = dirtPercentage,
                            WearPercentage = wearPercentage,
                            FrameIndex = frameIndex,
                            StateFlags = stateFlags
                        };

                        // Add to the manager
                        VehicleNeedsManager.GetParkedVehiclesNeeds()[parkedVehicleId] = parkedVehicleNeeds;
                    }

                    CheckEndTuple($"Buffer({i})", iVehicleNeedsManagerVersion, Data, ref iIndex);
                }
            }
        }

        private static void CheckStartTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleStart = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleStart != uiTUPLE_START)
                {
                    throw new Exception($"VehiclesNeeds Buffer start tuple not found at: {sTupleLocation}");
                }
            }
        }

        private static void CheckEndTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleEnd = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleEnd != uiTUPLE_END)
                {
                    throw new Exception($"VehiclesNeeds Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
    
}
