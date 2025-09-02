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

        private const ushort iVEHICLE_NEEDS_MANAGER_DATA_VERSION = 1;

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

                // Tunnel wash related
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
            Debug.Log("VehicleNeedsManager.ParkedVehiclesNeeds.Count: " + ParkedVehiclesNeeds.Count);

            foreach (var kvp in ParkedVehiclesNeeds)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt16(kvp.Key, Data);

                // Owner related
                StorageData.WriteUInt32(kvp.Value.OwnerId, Data);

                // Fuel related
                StorageData.WriteFloat(kvp.Value.FuelAmount, Data);
                StorageData.WriteFloat(kvp.Value.FuelCapacity, Data);

                // Dirt related
                StorageData.WriteFloat(kvp.Value.DirtPercentage, Data);

                // Wear related
                StorageData.WriteFloat(kvp.Value.WearPercentage, Data);

                // Frame index related
                StorageData.WriteUInt32(kvp.Value.FrameIndex, Data);

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
                Debug.Log("VehicleNeedsManager.VehiclesNeeds.Count: " + VehiclesFuel_Count);

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

                    // Dirt related
                    float dirtPercentage = StorageData.ReadFloat(Data, ref iIndex);
                    float dirtPerFrame = StorageData.ReadFloat(Data, ref iIndex);

                    // Tunnel wash related
                    uint lastFrameIndex = StorageData.ReadUInt32(Data, ref iIndex); ;
                    float tunnelWashSegmentLength = StorageData.ReadFloat(Data, ref iIndex);
                    float tunnelWashSegmentMaxSpeed = StorageData.ReadFloat(Data, ref iIndex);
                    float tunnelWashDistanceTraveled = StorageData.ReadFloat(Data, ref iIndex);
                    float tunnelWashDirtStartPercentage = StorageData.ReadFloat(Data, ref iIndex);
                    float tunnelWashStartPosition_x = StorageData.ReadFloat(Data, ref iIndex);
                    float tunnelWashStartPosition_y = StorageData.ReadFloat(Data, ref iIndex);
                    float tunnelWashStartPosition_z = StorageData.ReadFloat(Data, ref iIndex);
                    byte tunnelWashEntryOffset = StorageData.ReadByte(Data, ref iIndex);
                    byte tunnelWashPreviousOffset = StorageData.ReadByte(Data, ref iIndex);
                    ushort tunnelWashStartNode = StorageData.ReadUInt16(Data, ref iIndex);
                    ushort tunnelWashEndNode = StorageData.ReadUInt16(Data, ref iIndex);

                    // Flags related
                    Vector3 tunnelWashStartPosition = new(tunnelWashStartPosition_x, tunnelWashStartPosition_y, tunnelWashStartPosition_z);

                    // Wear related
                    float wearPercentage = StorageData.ReadFloat(Data, ref iIndex);
                    float wearPerFrame = StorageData.ReadFloat(Data, ref iIndex);

                    VehicleNeedsManager.VehicleStateFlags stateFlags = (VehicleNeedsManager.VehicleStateFlags)StorageData.ReadUInt32(Data, ref iIndex);
 
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
                Debug.Log("VehicleNeedsManager.ParkedVehiclesNeeds.Count: " + ParkedVehiclesFuel_Count);

                for (int i = 0; i < ParkedVehiclesFuel_Count; i++)
                {
                    CheckParkedStartTuple($"Buffer({i})", iVehicleNeedsManagerVersion, Data, ref iIndex);

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

                    uint frameIndex = StorageData.ReadUInt32(Data, ref iIndex);

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

                    CheckParkedEndTuple($"Buffer({i})", iVehicleNeedsManagerVersion, Data, ref iIndex);
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

        private static void CheckParkedStartTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleStart = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleStart != uiTUPLE_START)
                {
                    throw new Exception($"ParkedVehiclesNeeds Buffer start tuple not found at: {sTupleLocation}");
                }
            }
        }

        private static void CheckParkedEndTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleEnd = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleEnd != uiTUPLE_END)
                {
                    throw new Exception($"ParkedVehiclesNeeds Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
    
}
