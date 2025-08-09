using System;
using System.Collections.Generic;
using System.Text;
using ColossalFramework;
using ColossalFramework.DataBinding;
using ColossalFramework.Math;
using MoreTransferReasons;
using RoadsideCare.Managers;
using UnityEngine;

namespace RoadsideCare.AI
{
    public class GasPumpAI : PlayerBuildingAI, IExtendedBuildingAI
    {
        [CustomizableProperty("Uneducated Workers", "Workers", 0)]
        public int m_workPlaceCount0 = 2;

        [CustomizableProperty("Noise Accumulation", "Pollution")]
        public int m_noiseAccumulation = 50;

        [CustomizableProperty("Noise Radius", "Pollution")]
        public float m_noiseRadius = 50f;

        [CustomizableProperty("Fuel Capacity")]
        public int m_fuelCapacity = 50000;

        [CustomizableProperty("Battery Recharge")]
        public bool m_allowBatteryRecharge = true;

        readonly ExtendedTransferManager.TransferReason m_incomingResource = ExtendedTransferManager.TransferReason.PetroleumProducts;

        readonly ExtendedTransferManager.TransferReason m_outgoingResource1 = ExtendedTransferManager.TransferReason.VehicleFuel;

        readonly ExtendedTransferManager.TransferReason m_outgoingResource2 = ExtendedTransferManager.TransferReason.VehicleFuelElectric;

        public override Color GetColor(ushort buildingID, ref Building data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode)
        {
            switch (infoMode)
            {
                case InfoManager.InfoMode.NoisePollution:
                    {
                        int noiseAccumulation = m_noiseAccumulation;
                        return GetNoisePollutionColor(noiseAccumulation);
                    }
                case InfoManager.InfoMode.Tourism:
                    switch (Singleton<InfoManager>.instance.CurrentSubMode)
                    {
                        case InfoManager.SubInfoMode.Default:
                            if (data.m_tempExport != 0 || data.m_finalExport != 0)
                            {
                                return GetTourismColor(Mathf.Max(data.m_tempExport, data.m_finalExport));
                            }
                            return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                        case InfoManager.SubInfoMode.WaterPower:
                            return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                        default:
                            return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                    }
                case InfoManager.InfoMode.Connections:
                    if (ShowConsumption(buildingID, ref data))
                    {
                        if (Singleton<InfoManager>.instance.CurrentSubMode == InfoManager.SubInfoMode.Default)
                        {
                            if (m_incomingResource != ExtendedTransferManager.TransferReason.None && (data.m_tempImport != 0 || data.m_finalImport != 0))
                            {
                                return Singleton<ExtendedTransferManager>.instance.m_properties.m_resourceColors[(int)m_incomingResource];
                            }
                            return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                        }
                        return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                    }
                    return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                default:
                    return base.GetColor(buildingID, ref data, infoMode, subInfoMode);
            }
        }

        public override int GetResourceRate(ushort buildingID, ref Building data, ImmaterialResourceManager.Resource resource)
        {
            if (resource == ImmaterialResourceManager.Resource.NoisePollution)
            {
                return m_noiseAccumulation;
            }
            return base.GetResourceRate(buildingID, ref data, resource);
        }

        public override void GetPlacementInfoMode(out InfoManager.InfoMode mode, out InfoManager.SubInfoMode subMode, float elevation)
        {
            if (m_incomingResource == ExtendedTransferManager.TransferReason.PetroleumProducts)
            {
                mode = InfoManager.InfoMode.Connections;
                subMode = InfoManager.SubInfoMode.None;
            }
            else
            {
                base.GetPlacementInfoMode(out mode, out subMode, elevation);
            }
        }

        public override string GetDebugString(ushort buildingID, ref Building data)
        {
            string text = base.GetDebugString(buildingID, ref data);
            ExtendedTransferManager.TransferReason incomingResource = m_incomingResource;
            ExtendedTransferManager.TransferReason outgoingResource1 = m_outgoingResource1;
            ExtendedTransferManager.TransferReason outgoingResource2 = m_outgoingResource2;
            if (incomingResource != ExtendedTransferManager.TransferReason.None)
            {
                int count = 0;
                int cargo = 0;
                int capacity = 0;
                int outside = 0;
                ExtendedVehicleManager.CalculateGuestVehicles(buildingID, ref data, incomingResource, ref count, ref cargo, ref capacity, ref outside);
                text = StringUtils.SafeFormat("{0}\n{1}: {2} (+{3})", text, incomingResource.ToString(), data.m_customBuffer1, cargo);
            }
            if (outgoingResource1 != ExtendedTransferManager.TransferReason.None)
            {
                int count = 0;
                int cargo = 0;
                int capacity = 0;
                int outside = 0;
                ExtendedVehicleManager.CalculateGuestVehicles(buildingID, ref data, outgoingResource1, ref count, ref cargo, ref capacity, ref outside);
                text = StringUtils.SafeFormat("{0}\n{1}: {2} (+{3})", text, outgoingResource1.ToString(), data.m_customBuffer1, cargo);
            }
            if (m_allowBatteryRecharge && outgoingResource2 != ExtendedTransferManager.TransferReason.None)
            {
                int count = 0;
                int cargo = 0;
                int capacity = 0;
                int outside = 0;
                ExtendedVehicleManager.CalculateGuestVehicles(buildingID, ref data, outgoingResource2, ref count, ref cargo, ref capacity, ref outside);
                text = StringUtils.SafeFormat("{0}\n{1}: {2}", text, outgoingResource2.ToString(), cargo);
            }
            return text;
        }

        public override void CreateBuilding(ushort buildingID, ref Building data)
        {
            base.CreateBuilding(buildingID, ref data);
            int workCount = m_workPlaceCount0;
            Singleton<CitizenManager>.instance.CreateUnits(out data.m_citizenUnits, ref Singleton<SimulationManager>.instance.m_randomizer, buildingID, 0, 0, workCount, 0, 0, 0);
            GasStationManager.CreateGasStationBuilding(buildingID, 0, []);
        }

        public override void ReleaseBuilding(ushort buildingID, ref Building data)
        {
            base.ReleaseBuilding(buildingID, ref data);
            GasStationManager.RemoveGasStation(buildingID);
        }

        public override void BuildingLoaded(ushort buildingID, ref Building data, uint version)
        {
            base.BuildingLoaded(buildingID, ref data, version);
            EnsureCitizenUnits(buildingID, ref data);
            if (!GasStationManager.GasStationBuildingExist(buildingID))
            {
                GasStationManager.CreateGasStationBuilding(buildingID, 0, []);
            }
        }

        public override void EndRelocating(ushort buildingID, ref Building data)
        {
            base.EndRelocating(buildingID, ref data);
            EnsureCitizenUnits(buildingID, ref data);
        }

        private void EnsureCitizenUnits(ushort buildingID, ref Building data)
        {
            int workCount = m_workPlaceCount0;
            EnsureCitizenUnits(buildingID, ref data, 0, workCount, 0, 0);
        }

        protected override void ManualActivation(ushort buildingID, ref Building buildingData)
        {
            if (m_noiseAccumulation != 0)
            {
                Singleton<NotificationManager>.instance.AddWaveEvent(buildingData.m_position, NotificationEvent.Type.Happy, ImmaterialResourceManager.Resource.NoisePollution, m_noiseAccumulation, m_noiseRadius);
            }
        }

        protected override void ManualDeactivation(ushort buildingID, ref Building buildingData)
        {
            if ((buildingData.m_flags & Building.Flags.Collapsed) != 0)
            {
                Singleton<NotificationManager>.instance.AddWaveEvent(buildingData.m_position, NotificationEvent.Type.Sad, ImmaterialResourceManager.Resource.Abandonment, -buildingData.Width * buildingData.Length, 64f);
                return;
            }
            if (m_noiseAccumulation != 0)
            {
                Singleton<NotificationManager>.instance.AddWaveEvent(buildingData.m_position, NotificationEvent.Type.Sad, ImmaterialResourceManager.Resource.NoisePollution, -m_noiseAccumulation, m_noiseRadius);
            }
        }

        public void ExtendedStartTransfer(ushort buildingID, ref Building data, ExtendedTransferManager.TransferReason material, ExtendedTransferManager.Offer offer)
        {

        }

        public void ExtendedGetMaterialAmount(ushort buildingID, ref Building data, ExtendedTransferManager.TransferReason material, out int amount, out int max)
        {
            var gasStation = GasStationManager.GetGasStationBuilding(buildingID);
            amount = gasStation.FuelAmount;
            max = m_fuelCapacity;
        }

        public void ExtendedModifyMaterialBuffer(ushort buildingID, ref Building data, ExtendedTransferManager.TransferReason material, ref int amountDelta)
        {
            if (GasStationManager.GasStationBuildingExist(buildingID))
            {
                var gasStation = GasStationManager.GetGasStationBuilding(buildingID);
                if (material == m_incomingResource)
                {
                    amountDelta = Mathf.Clamp(amountDelta, 0, m_fuelCapacity - gasStation.FuelAmount);
                    gasStation.FuelAmount += (ushort)amountDelta;
                }
                if (material == m_outgoingResource1)
                {
                    amountDelta = Mathf.Clamp(amountDelta, 0, gasStation.FuelAmount);
                    gasStation.FuelAmount -= (ushort)amountDelta;
                }
                GasStationManager.SetFuelAmount(buildingID, gasStation.FuelAmount);
            }
        }

        public override void BuildingDeactivated(ushort buildingID, ref Building data)
        {
            ExtendedTransferManager.Offer offer = default;
            offer.Building = buildingID;
            if (m_incomingResource != ExtendedTransferManager.TransferReason.None)
            {
                Singleton<ExtendedTransferManager>.instance.RemoveIncomingOffer(m_incomingResource, offer);
            }
            if (m_outgoingResource1 != ExtendedTransferManager.TransferReason.None)
            {
                Singleton<ExtendedTransferManager>.instance.RemoveOutgoingOffer(m_outgoingResource1, offer);
            }
            if (m_allowBatteryRecharge && m_outgoingResource2 != ExtendedTransferManager.TransferReason.None)
            {
                Singleton<ExtendedTransferManager>.instance.RemoveOutgoingOffer(m_outgoingResource2, offer);
            }
            base.BuildingDeactivated(buildingID, ref data);
        }

        public override void SimulationStep(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
        {
            base.SimulationStep(buildingID, ref buildingData, ref frameData);
        }

        protected override bool CanEvacuate()
        {
            return m_workPlaceCount0 != 0;
        }

        protected override void HandleWorkAndVisitPlaces(ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveWorkerCount, ref int totalWorkerCount, ref int workPlaceCount, ref int aliveVisitorCount, ref int totalVisitorCount, ref int visitPlaceCount)
        {
            workPlaceCount += m_workPlaceCount0;
            GetWorkBehaviour(buildingID, ref buildingData, ref behaviour, ref aliveWorkerCount, ref totalWorkerCount);
            HandleWorkPlaces(buildingID, ref buildingData, m_workPlaceCount0, 0, 0, 0, ref behaviour, aliveWorkerCount, totalWorkerCount);
        }

        protected override void ProduceGoods(ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData behaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveVisitorCount, int totalVisitorCount, int visitPlaceCount)
        {
            base.ProduceGoods(buildingID, ref buildingData, ref frameData, productionRate, finalProductionRate, ref behaviour, aliveWorkerCount, totalWorkerCount, workPlaceCount, aliveVisitorCount, totalVisitorCount, visitPlaceCount);
            if (finalProductionRate != 0 && GasStationManager.GasStationBuildingExist(buildingID))
            {
                if (m_noiseAccumulation != 0)
                {
                    Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.NoisePollution, m_noiseAccumulation, buildingData.m_position, m_noiseRadius);
                }
                HandleDead(buildingID, ref buildingData, ref behaviour, totalWorkerCount);
                var gasStation = GasStationManager.GetGasStationBuilding(buildingID);
                int missingFuel = m_fuelCapacity - gasStation.FuelAmount;
                if (buildingData.m_fireIntensity == 0)
                {
                    if (missingFuel > m_fuelCapacity * 0.8)
                    {
                        ExtendedTransferManager.Offer offer = default;
                        offer.Building = buildingID;
                        offer.Position = buildingData.m_position;
                        offer.Amount = missingFuel;
                        offer.Active = false;
                        Singleton<ExtendedTransferManager>.instance.AddIncomingOffer(m_incomingResource, offer);
                    }

                    if (gasStation.FuelAmount > m_fuelCapacity * 0.1)
                    {
                        ExtendedTransferManager.Offer offer = default;
                        offer.Building = buildingID;
                        offer.Position = buildingData.m_position;
                        offer.Amount = 1;
                        offer.Active = false;
                        Singleton<ExtendedTransferManager>.instance.AddIncomingOffer(m_outgoingResource1, offer);
                    }
                }

                if (buildingData.m_electricityBuffer > 0)
                {
                    ExtendedTransferManager.Offer offer = default;
                    offer.Building = buildingID;
                    offer.Position = buildingData.m_position;
                    offer.Amount = 1;
                    offer.Active = false;
                    Singleton<ExtendedTransferManager>.instance.AddIncomingOffer(m_outgoingResource2, offer);
                }
                RefreshFuelLanes(buildingID);
            }
        }

        public override string GetLocalizedTooltip()
        {
            string text = LocaleFormatter.FormatGeneric("AIINFO_WATER_CONSUMPTION", GetWaterConsumption() * 16) + Environment.NewLine + LocaleFormatter.FormatGeneric("AIINFO_ELECTRICITY_CONSUMPTION", GetElectricityConsumption() * 16);
            string text2 = LocaleFormatter.FormatGeneric("AIINFO_WORKPLACES_ACCUMULATION", (m_workPlaceCount0).ToString());
            return TooltipHelper.Append(base.GetLocalizedTooltip(), TooltipHelper.Format(LocaleFormatter.Info1, text, LocaleFormatter.Info2, string.Empty, LocaleFormatter.WorkplaceCount, text2));
        }

        public override string GetLocalizedStats(ushort buildingID, ref Building data)
        {
            StringBuilder stringBuilder = new();
            if (GasStationManager.GasStationBuildingExist(buildingID))
            {
                var gasStation = GasStationManager.GetGasStationBuilding(buildingID);
                stringBuilder.Append(string.Format("Fuel Liters Avaliable: {0} of {1}", gasStation.FuelAmount, m_fuelCapacity));
                stringBuilder.Append(Environment.NewLine);
            }
            return stringBuilder.ToString();
        }

        public override void GetPollutionAccumulation(out int ground, out int noise)
        {
            ground = 0;
            noise = m_noiseAccumulation;
        }

        public override bool RequireRoadAccess()
        {
            return true;
        }

        public override void CountWorkPlaces(out int workPlaceCount0, out int workPlaceCount1, out int workPlaceCount2, out int workPlaceCount3)
        {
            workPlaceCount0 = m_workPlaceCount0;
            workPlaceCount1 = 0;
            workPlaceCount2 = 0;
            workPlaceCount3 = 0;
        }

        public override void CalculateUnspawnPosition(ushort buildingID, ref Building data, ref Randomizer randomizer, VehicleInfo info, out Vector3 position, out Vector3 target)
        {
            position = data.CalculateSidewalkPosition(0f, 2f);
            target = position;
        }

        public void RefreshFuelLanes(ushort buildingID)
        {
            if(GasStationManager.GasStationBuildingExist(buildingID))
            {
                var gasStation = GasStationManager.GetGasStationBuilding(buildingID);
                var toRemove = new List<ushort>();
                foreach (var segmentId in gasStation.FuelLanes)
                {
                    if (segmentId == 0 || !NetManager.instance.m_segments.m_buffer[segmentId].m_flags.IsFlagSet(NetSegment.Flags.Created))
                    {
                        toRemove.Add(segmentId);
                        continue;
                    }

                    ushort infoIndex = NetManager.instance.m_segments.m_buffer[segmentId].m_infoIndex;
                    NetInfo info = PrefabCollection<NetInfo>.GetPrefab(infoIndex);
                    if (info.m_netAI is not FuelLaneAI)
                    {
                        toRemove.Add(segmentId);
                    }
                }

                foreach (var seg in toRemove)
                {
                    gasStation.FuelLanes.Remove(seg);
                }

                GasStationManager.SetFuelLanes(buildingID, gasStation.FuelLanes);
            }
        }

    }
}
