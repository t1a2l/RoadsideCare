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

                // Fuel related
                StorageData.WriteFloat(kvp.Value.FuelAmount, Data);
                StorageData.WriteFloat(kvp.Value.FuelCapacity, Data);
                StorageData.WriteFloat(kvp.Value.FuelPerFrame, Data);
                StorageData.WriteBool(kvp.Value.IsRefueling, Data);
                StorageData.WriteBool(kvp.Value.IsGoingToRefuel, Data);

                // Dirt related
                StorageData.WriteFloat(kvp.Value.DirtPercentage, Data);
                StorageData.WriteBool(kvp.Value.IsGoingToGetWashed, Data);
                StorageData.WriteBool(kvp.Value.IsBeingWashed, Data);

                // Wear related
                StorageData.WriteFloat(kvp.Value.WearPercentage, Data);
                StorageData.WriteBool(kvp.Value.IsGoingToGetRepaired, Data);
                StorageData.WriteBool(kvp.Value.IsBeingRepaired, Data);

                // Vehicle issues
                StorageData.WriteBool(kvp.Value.IsBroken, Data);
                StorageData.WriteBool(kvp.Value.IsOutOfFuel, Data);

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

                // Vehicle issues
                StorageData.WriteBool(kvp.Value.IsBroken, Data);
                StorageData.WriteBool(kvp.Value.IsOutOfFuel, Data);

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

                    // Fuel related
                    float fuelAmount = StorageData.ReadFloat(Data, ref iIndex);

                    float fuelCapacity = StorageData.ReadFloat(Data, ref iIndex);

                    float fuelPerFrame = StorageData.ReadFloat(Data, ref iIndex);

                    bool isRefueling = StorageData.ReadBool(Data, ref iIndex);

                    bool isGoingToRefuel = StorageData.ReadBool(Data, ref iIndex);

                    // Dirt related
                    float dirtPercentage = StorageData.ReadFloat(Data, ref iIndex);

                    bool isGoingToGetWashed = StorageData.ReadBool(Data, ref iIndex);

                    bool isBeingWashed = StorageData.ReadBool(Data, ref iIndex);

                    // Wear related
                    float wearPercentage = StorageData.ReadFloat(Data, ref iIndex);

                    bool isGoingToGetRepaired = StorageData.ReadBool(Data, ref iIndex);

                    bool isBeingRepaired = StorageData.ReadBool(Data, ref iIndex);

                    // Vehicle issues
                    bool isBroken = StorageData.ReadBool(Data, ref iIndex);

                    bool isOutOfFuel = StorageData.ReadBool(Data, ref iIndex);

                    if(!VehicleNeedsManager.VehicleNeedsExist(vehicleId))
                    {
                        VehicleNeedsManager.CreateVehicleNeeds(vehicleId, originalTargetBuilding, ownerId, fuelAmount, fuelCapacity, dirtPercentage,
                            wearPercentage, fuelPerFrame, isRefueling, isGoingToRefuel, isGoingToGetWashed, isBeingWashed, isGoingToGetRepaired, isBeingRepaired,
                            isBroken, isOutOfFuel);
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

                    // Vehicle issues
                    bool isBroken = StorageData.ReadBool(Data, ref iIndex);

                    bool isOutOfFuel = StorageData.ReadBool(Data, ref iIndex);

                    if (!VehicleNeedsManager.ParkedVehicleNeedsExist(parkedVehicleId))
                    {
                        VehicleNeedsManager.CreateParkedVehicleNeeds(parkedVehicleId, ownerId, fuelAmount, fuelCapacity, dirtPercentage, wearPercentage, isBroken, isOutOfFuel);
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
