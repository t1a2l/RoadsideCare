using RoadsideCare.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadsideCare.Serializer
{
    public class VehicleWashBuildingManagerSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iVEHICLE_WASH_BUILDING_MANAGER_DATA_VERSION = 1;

        public static void SaveData(FastList<byte> Data)
        {
            var VehicleWashBuildings = VehicleWashBuildingManager.GetVehicleWashBuildings();

            // Write out metadata
            StorageData.WriteUInt16(iVEHICLE_WASH_BUILDING_MANAGER_DATA_VERSION, Data);
            Debug.Log("iVEHICLE_WASH_BUILDING_MANAGER_DATA_VERSION: " + iVEHICLE_WASH_BUILDING_MANAGER_DATA_VERSION);

            StorageData.WriteInt32(VehicleWashBuildings.Count, Data);
            Debug.Log("VehicleWashBuildingManager.VehicleWashBuildings.Count: " + VehicleWashBuildings.Count);

            // Write out each buffer settings
            foreach (var kvp in VehicleWashBuildings)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                StorageData.WriteUInt16(kvp.Key, Data);

                StorageData.WriteUShortList(kvp.Value.VehicleWashLanes, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }

        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iVehicleWashBuildingManagerVersion = StorageData.ReadUInt16(Data, ref iIndex);

                Debug.Log("RoadsideCare VehicleWashBuilding - Global: " + iGlobalVersion + " BufferVersion: " + iVehicleWashBuildingManagerVersion + " DataLength: " + Data.Length + " Index: " + iIndex);

                VehicleWashBuildingManager.Init();

                int VehicleWashBuildings_Count = StorageData.ReadInt32(Data, ref iIndex);

                for (int i = 0; i < VehicleWashBuildings_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iVehicleWashBuildingManagerVersion, Data, ref iIndex);

                    ushort buildingId = StorageData.ReadUInt16(Data, ref iIndex);

                    List<ushort> vehicleWashLanes = StorageData.ReadUShortList(Data, ref iIndex);

                    if (!VehicleWashBuildingManager.VehicleWashBuildingExist(buildingId))
                    {
                        VehicleWashBuildingManager.CreateVehicleWashBuilding(buildingId, vehicleWashLanes);
                    }
                
                    CheckEndTuple($"Buffer({i})", iVehicleWashBuildingManagerVersion, Data, ref iIndex);
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
                    throw new Exception($"VehicleWashBuilding Buffer start tuple not found at: {sTupleLocation}");
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
                    throw new Exception($"VehicleWashBuilding Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
    
}
