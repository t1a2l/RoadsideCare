using System;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
using RoadsideCare.AI;
using RoadsideCare.Managers;
using UnityEngine;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch]
    public static class WorldInfoPanelPatches
    {
        [HarmonyPatch(typeof(CitizenVehicleWorldInfoPanel), "UpdateBindings")]
        [HarmonyPostfix]
        public static void CitizenVehicleUpdateBindings(CitizenVehicleWorldInfoPanel __instance, ref InstanceID ___m_InstanceID)
        {
            var Type = __instance.Find<UILabel>("Type");
            var panel = __instance.Find<UIPanel>("(Library) CitizenVehicleWorldInfoPanel");
            panel?.height = 290;
            if (Type == null)
            {
                return;
            }

            ushort vehicleId = 0;
            ushort parkedVehicleId = 0;
            VehicleInfo vehicleInfo = null;
            float fuelValue = 0;
            float dirtValue = 0;
            //float wearValue = 0;

            // bool isBroken = false;
            bool isOutOfFuel = false;

            if (___m_InstanceID.Type == InstanceType.Vehicle && ___m_InstanceID.Vehicle != 0 && VehicleNeedsManager.VehicleNeedsExist(___m_InstanceID.Vehicle))
            {
                vehicleId = ___m_InstanceID.Vehicle;
                vehicleInfo = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[___m_InstanceID.Vehicle].Info;
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleId);
                fuelValue = vehicleNeeds.FuelAmount / vehicleNeeds.FuelCapacity;
                dirtValue = vehicleNeeds.DirtPercentage / 100;
                // wearValue = vehicleNeeds.WearPercentage / 100;
                // isBroken = (vehicleNeeds.StateFlags & VehicleNeedsManager.VehicleStateFlags.IsBroken) != 0;
                isOutOfFuel = (vehicleNeeds.StateFlags & VehicleNeedsManager.VehicleStateFlags.IsOutOfFuel) != 0;
            }
            else if (___m_InstanceID.Type == InstanceType.ParkedVehicle && ___m_InstanceID.ParkedVehicle != 0 && VehicleNeedsManager.ParkedVehicleNeedsExist(___m_InstanceID.ParkedVehicle))
            {
                parkedVehicleId = ___m_InstanceID.ParkedVehicle;
                vehicleInfo = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[___m_InstanceID.ParkedVehicle].Info;
                var parkedVehicleNeeds = VehicleNeedsManager.GetParkedVehicleNeeds(parkedVehicleId);
                fuelValue = parkedVehicleNeeds.FuelAmount / parkedVehicleNeeds.FuelCapacity;
                dirtValue = parkedVehicleNeeds.DirtPercentage / 100;
                // wearValue = parkedVehicleNeeds.WearPercentage / 100;
                // isBroken = (parkedVehicleNeeds.StateFlags & VehicleNeedsManager.ParkedVehicleStateFlags.IsBroken) != 0;
                isOutOfFuel = (parkedVehicleNeeds.StateFlags & VehicleNeedsManager.ParkedVehicleStateFlags.IsOutOfFuel) != 0;
            }

            if ((vehicleId != 0 || parkedVehicleId != 0) && vehicleInfo != null)
            {
                bool isElectric = vehicleInfo.m_class.m_subService != ItemClass.SubService.ResidentialLow;
                Type.text += Environment.NewLine;
                Type.parent.height = 35;
                if (isElectric)
                {
                    Type.text += "Battery Percent:  " + fuelValue.ToString("#0%");
                }
                else
                {
                    Type.text += "Fuel Percent:  " + fuelValue.ToString("#0%");
                }
                Type.text += Environment.NewLine;
                Type.text += " Dirt Percent:  " + dirtValue.ToString("#0%");
                //Type.text += Environment.NewLine;
                //Type.text += " Wear Percent:  " + wearValue.ToString("#0%");

                //if(isBroken)
                //{
                //    Type.text += Environment.NewLine;
                //    Type.text += " Broke Down  ";
                //}

                if (isOutOfFuel)
                {
                    Type.text += Environment.NewLine;
                    Type.text += " Out Of Fuel  ";
                }

            }

        }

        [HarmonyPatch(typeof(CityServiceVehicleWorldInfoPanel), "UpdateBindings")]
        [HarmonyPostfix]
        public static void CityServiceVehicleUpdateBindings(CityServiceVehicleWorldInfoPanel __instance, ref InstanceID ___m_InstanceID)
        {
            var Type = __instance.Find<UILabel>("Type");
            if (Type == null)
            {
                return;
            }
            if (___m_InstanceID.Vehicle != 0 && VehicleNeedsManager.VehicleNeedsExist(___m_InstanceID.Vehicle))
            {
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(___m_InstanceID.Vehicle);
                Type.parent.height = 35;
                Type.text += Environment.NewLine;
                float fuelValue = vehicleNeeds.FuelAmount / vehicleNeeds.FuelCapacity;
                float dirtValue = vehicleNeeds.DirtPercentage / 100;
                //float wearValue = vehicleNeeds.WearPercentage / 100;

                Type.text += "Fuel Percent:  " + fuelValue.ToString("#0%");
                Type.text += Environment.NewLine;
                Type.text += " Dirt Percent:  " + dirtValue.ToString("#0%");
                //Type.text += Environment.NewLine;
                //Type.text += " Wear Percent:  " + wearValue.ToString("#0%");

                //if ((vehicleNeeds.StateFlags & VehicleNeedsManager.VehicleStateFlags.IsBroken) != 0)
                //{
                //    Type.text += Environment.NewLine;
                //    Type.text += " Broke Down  ";
                //}

                if ((vehicleNeeds.StateFlags & VehicleNeedsManager.VehicleStateFlags.IsOutOfFuel) != 0)
                {
                    Type.text += Environment.NewLine;
                    Type.text += " Out Of Fuel  ";
                }

                var panel = __instance.Find<UIPanel>("(Library) CityServiceVehicleWorldInfoPanel");
                panel?.height = 190;
            }
        }

        private static bool buildingSelected = false;
        private static Vector3 buildingPosition = Vector3.zero;
        private static List<ushort> buildingSegmentList = [];
        private static List<ushort> buildingSegmentList2 = [];
        private static string[] buildingTypeArr;
        private static string[] buildingTypeArr2;
        private static Color[] buildingColorArr;
        private static Color[] buildingColorArr2;

        [HarmonyPatch(typeof(CityServiceWorldInfoPanel), "OnSetTarget")]
        [HarmonyPostfix]
        public static void OnSetTarget(CityServiceWorldInfoPanel __instance, ref InstanceID ___m_InstanceID)
        {
            ushort buildingId = ___m_InstanceID.Building;
            if (GasStationManager.GasStationBuildingExist(buildingId))
            {
                var gasStation = GasStationManager.GetGasStationBuilding(buildingId);
                var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId];
                var typeArr = new string[] { "FuelPointAI", "FuelPointLargeAI", "FuelPointSmallAI" };
                var colorArr = new Color[] { Color.cyan, Color.blue };
                FindSegmentsAroundBuilding(building.m_position, gasStation.FuelPoints, typeArr, colorArr, 40f, true);
                buildingSelected = true;
                buildingPosition = building.m_position;
                buildingSegmentList = gasStation.FuelPoints;
                buildingTypeArr = typeArr;
                buildingColorArr = colorArr;
            }
            else if (VehicleWashBuildingManager.VehicleWashBuildingExist(buildingId))
            {
                var vehicleWash = VehicleWashBuildingManager.GetVehicleWashBuilding(buildingId);
                var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId];
                var typeArr = new string[] { "VehicleWashPointAI", "VehicleWashPointLargeAI", "VehicleWashPointSmallAI" };
                var typeArr2 = new string[] { "VehicleWashLaneAI", "VehicleWashLaneLargeAI", "VehicleWashLaneSmallAI" };
                var colorArr = new Color[] { Color.white, Color.yellow };
                var colorArr2 = new Color[] { Color.magenta, Color.green };
                FindSegmentsAroundBuilding(building.m_position, vehicleWash.VehicleWashPoints, typeArr, colorArr, 40f, true);
                FindSegmentsAroundBuilding(building.m_position, vehicleWash.VehicleWashLanes, typeArr2, colorArr2, 40f, true);
                buildingSelected = true;
                buildingPosition = building.m_position;
                buildingSegmentList = vehicleWash.VehicleWashPoints;
                buildingSegmentList2 = vehicleWash.VehicleWashLanes;
                buildingTypeArr = typeArr;
                buildingTypeArr2 = typeArr2;
                buildingColorArr = colorArr;
                buildingColorArr2 = colorArr2;
            }
            else
            {
                if (buildingSelected && buildingPosition != Vector3.zero)
                {
                    if (buildingSegmentList.Count > 0)
                    {
                        FindSegmentsAroundBuilding(buildingPosition, buildingSegmentList, buildingTypeArr, buildingColorArr, 40f, false);
                        buildingSegmentList = [];
                    }
                    if (buildingSegmentList2.Count > 0)
                    {
                        FindSegmentsAroundBuilding(buildingPosition, buildingSegmentList2, buildingTypeArr2, buildingColorArr2, 40f, false);
                        buildingSegmentList = [];
                    }
                    buildingSelected = false;
                    buildingPosition = Vector3.zero;
                    buildingTypeArr = null;
                    buildingTypeArr2 = null;
                    buildingColorArr = null;
                    buildingColorArr2 = null;
                }
            }
        }

        private const int MaxBuildingGridIndex = BuildingManager.BUILDINGGRID_RESOLUTION - 1;
        private const int BuildingGridMiddle = BuildingManager.BUILDINGGRID_RESOLUTION / 2;

        public static void FindSegmentsAroundBuilding(Vector3 position, List<ushort> list, string[] typeArr, Color[] colorArr, float maxDistance, bool highlight)
        {
            if (position == Vector3.zero)
            {
                return;
            }

            string typeNormal = typeArr[0];
            string typeLarge = typeArr[1];
            string typeSmall = typeArr[2];

            Color belongs_color = colorArr[0];
            Color available_color = colorArr[1];

            int gridXFrom = Mathf.Max((int)((position.x - maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), 0);
            int gridZFrom = Mathf.Max((int)((position.z - maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), 0);
            int gridXTo = Mathf.Min((int)((position.x + maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), MaxBuildingGridIndex);
            int gridZTo = Mathf.Min((int)((position.z + maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), MaxBuildingGridIndex);

            float sqrMaxDistance = maxDistance * maxDistance;
            for (int z = gridZFrom; z <= gridZTo; ++z)
            {
                for (int x = gridXFrom; x <= gridXTo; ++x)
                {
                    ushort segmentId = NetManager.instance.m_segmentGrid[z * BuildingManager.BUILDINGGRID_RESOLUTION + x];
                    uint counter = 0;
                    while (segmentId != 0)
                    {
                        ref var segment = ref NetManager.instance.m_segments.m_buffer[segmentId];
                        if (segment.Info.GetType().Name.Equals(typeNormal) || segment.Info.GetType().Name.Equals(typeLarge) || segment.Info.GetType().Name.Equals(typeSmall))
                        {
                            if(highlight)
                            {
                                if (list.Contains(segmentId))
                                {
                                    UpdateSegemntColor(segmentId, belongs_color);
                                }
                                else
                                {
                                    float sqrDistance = Vector3.SqrMagnitude(position - segment.m_middlePosition);
                                    if (sqrDistance < sqrMaxDistance)
                                    {
                                        UpdateSegemntColor(segmentId, available_color);
                                    }
                                    else
                                    {
                                        UpdateSegemntColor(segmentId, Singleton<InfoManager>.instance.m_properties.m_neutralColor);
                                    }
                                }
                            }
                            else
                            {
                                UpdateSegemntColor(segmentId, Singleton<InfoManager>.instance.m_properties.m_neutralColor);
                            }
                        }
                        segmentId = segment.m_nextGridSegment;
                        if (++counter >= NetManager.MAX_SEGMENT_COUNT)
                        {
                            break;
                        }
                    }
                }
            }
            return;
        }

        public static void UpdateSegemntColor(ushort segmentId, Color color)
        {
            RenderManager.GetColorLocation((uint)(49152 + segmentId), out var x, out var y);
            RenderManager.instance.m_objectColorMap.SetPixel(x, y, color);
            RenderManager.instance.m_objectColorMap.Apply(updateMipmaps: false);
        }
    }
}
