using System;
using System.Collections.Generic;
using System.Linq;
using RoadsideCare.AI;
using RoadsideCare.Managers;
using UnityEngine;

namespace RoadsideCare.Serializer
{
    public class GasStationManagerSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iGAS_STATION_MANAGER_DATA_VERSION = 1;

        public static void SaveData(FastList<byte> Data)
        {
            var GasStationBuildings = GasStationManager.GetGasStationBuildings();

            // Write out metadata
            StorageData.WriteUInt16(iGAS_STATION_MANAGER_DATA_VERSION, Data);
            Debug.Log("iGAS_STATION_MANAGER_DATA_VERSION: " + iGAS_STATION_MANAGER_DATA_VERSION);

            StorageData.WriteInt32(GasStationBuildings.Count, Data);
            Debug.Log("GasStationManager.GasStationBuildings.Count: " + GasStationBuildings.Count);

            // Write out each buffer settings
            foreach (var kvp in GasStationBuildings)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                StorageData.WriteUInt16(kvp.Key, Data);

                StorageData.WriteInt32(kvp.Value.FuelAmount, Data);
                StorageData.WriteUShortList(kvp.Value.FuelPoints, Data);

                Debug.Log($"GasStationManager SaveData: Building {kvp.Key} FuelAmount {kvp.Value.FuelAmount} FuelPoints {string.Join(",", [.. kvp.Value.FuelPoints.Select(x => x.ToString())])}");

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }

        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iGasStationManagerVersion = StorageData.ReadUInt16(Data, ref iIndex);

                Debug.Log("RoadsideCare GasStation - Global: " + iGlobalVersion + " BufferVersion: " + iGasStationManagerVersion + " DataLength: " + Data.Length + " Index: " + iIndex);

                GasStationManager.Init();

                int GasStationBuildings_Count = StorageData.ReadInt32(Data, ref iIndex);

                for (int i = 0; i < GasStationBuildings_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iGasStationManagerVersion, Data, ref iIndex);

                    ushort buildingId = StorageData.ReadUInt16(Data, ref iIndex);

                    int fuelAmount = StorageData.ReadInt32(Data, ref iIndex);

                    List<ushort> fuelPoints = StorageData.ReadUShortList(Data, ref iIndex);

                    Debug.Log($"GasStationManager LoadData: Building {buildingId} fuelAmount {fuelAmount} fuelPoints {string.Join(",", [.. fuelPoints.Select(x => x.ToString())])}");

                    if (!GasStationManager.GasStationBuildingExist(buildingId))
                    {
                        GasStationManager.CreateGasStationBuilding(buildingId, fuelAmount, fuelPoints);
                    }
                
                    CheckEndTuple($"Buffer({i})", iGasStationManagerVersion, Data, ref iIndex);
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
                    throw new Exception($"GasStation Buffer start tuple not found at: {sTupleLocation}");
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
                    throw new Exception($"GasStation Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
    
}
