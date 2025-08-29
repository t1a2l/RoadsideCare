using System;
using System.Text;
using ColossalFramework;
using ColossalFramework.DataBinding;
using ColossalFramework.Math;
using MoreTransferReasons;
using RoadsideCare.Managers;
using UnityEngine;

namespace RoadsideCare.AI
{
    public class GasStationAI : PlayerBuildingAI, IExtendedBuildingAI
    {
        [CustomizableProperty("Uneducated Workers", "Workers", 0)]
        public int m_workPlaceCount0 = 5;

        [CustomizableProperty("Educated Workers", "Workers", 1)]
        public int m_workPlaceCount1 = 5;

        [CustomizableProperty("Noise Accumulation", "Pollution")]
        public int m_noiseAccumulation = 50;

        [CustomizableProperty("Noise Radius", "Pollution")]
        public float m_noiseRadius = 100f;

        [CustomizableProperty("Visit place count", "Visitors", 3)]
        public int m_visitPlaceCount = 10;

        [CustomizableProperty("Fuel Capacity")]
        public int m_fuelCapacity = 50000;

        [CustomizableProperty("Goods Capacity")]
        public int m_goodsCapacity = 20000;

        [CustomizableProperty("Battery Recharge")]
        public bool m_allowBatteryRecharge = true;

        public TransferManager.TransferReason m_incomingResource1 = TransferManager.TransferReason.Goods;

        public readonly ExtendedTransferManager.TransferReason m_incomingResource2 = ExtendedTransferManager.TransferReason.PetroleumProducts;

        public readonly ExtendedTransferManager.TransferReason m_outgoingResource1 = ExtendedTransferManager.TransferReason.VehicleFuel;

        public readonly ExtendedTransferManager.TransferReason m_outgoingResource2 = ExtendedTransferManager.TransferReason.VehicleFuelElectric;

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
                            DistrictManager instance = Singleton<DistrictManager>.instance;
                            byte district = instance.GetDistrict(data.m_position);
                            DistrictPolicies.CityPlanning cityPlanningPolicies = instance.m_districts.m_buffer[district].m_cityPlanningPolicies;
                            DistrictPolicies.Taxation taxationPolicies = instance.m_districts.m_buffer[district].m_taxationPolicies;
                            int taxRate = GetTaxRate(buildingID, ref data, taxationPolicies);
                            GetAccumulation(new Randomizer(buildingID), data.m_adults * 100, taxRate, cityPlanningPolicies, taxationPolicies, out var _, out var attractiveness);
                            if (attractiveness != 0)
                            {
                                return CommonBuildingAI.GetAttractivenessColor(attractiveness);
                            }
                            return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                        default:
                            return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                    }
                case InfoManager.InfoMode.Connections:
                    if (ShowConsumption(buildingID, ref data))
                    {
                        if (Singleton<InfoManager>.instance.CurrentSubMode == InfoManager.SubInfoMode.Default)
                        {
                            if (m_incomingResource1 != TransferManager.TransferReason.None && (data.m_tempImport != 0 || data.m_finalImport != 0))
                            {
                                return Singleton<TransferManager>.instance.m_properties.m_resourceColors[(int)m_incomingResource1];
                            }
                            if (DistrictPark.IsPedestrianReason(m_incomingResource1, out var index))
                            {
                                byte park = Singleton<DistrictManager>.instance.GetPark(data.m_position);
                                if (park != 0 && Singleton<DistrictManager>.instance.m_parks.m_buffer[park].IsPedestrianZone && (Singleton<DistrictManager>.instance.m_parks.m_buffer[park].m_tempImport[index] != 0 || Singleton<DistrictManager>.instance.m_parks.m_buffer[park].m_finalImport[index] != 0))
                                {
                                    return Singleton<TransferManager>.instance.m_properties.m_resourceColors[(int)m_incomingResource1];
                                }
                            }
                            if (m_incomingResource2 != ExtendedTransferManager.TransferReason.None && (data.m_tempImport != 0 || data.m_finalImport != 0))
                            {
                                return Singleton<ExtendedTransferManager>.instance.m_properties.m_resourceColors[(int)m_incomingResource2];
                            }
                            return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                        }
                        return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                    }
                    return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                case InfoManager.InfoMode.Financial:
                    if (subInfoMode == InfoManager.SubInfoMode.WaterPower)
                    {
                        Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(ImmaterialResourceManager.Resource.CashCollecting, data.m_position, out var local);
                        return Color.Lerp(Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_negativeColorB, Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_targetColor, Mathf.Clamp01((float)local * 0.01f));
                    }
                    return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                case InfoManager.InfoMode.Hotel:
                    if (subInfoMode == InfoManager.SubInfoMode.WaterPower)
                    {
                        return Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_negativeColor;
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

        public override int GetResourceRate(ushort buildingID, ref Building data, EconomyManager.Resource resource)
        {
            if (resource == EconomyManager.Resource.PrivateIncome)
            {
                int width = data.Width;
                int length = data.Length;
                int num = width * length;
                int num2 = (100 * num + 99) / 100;
                return num2 * 100;
            }
            return base.GetResourceRate(buildingID, ref data, resource);
        }

        public override void GetPlacementInfoMode(out InfoManager.InfoMode mode, out InfoManager.SubInfoMode subMode, float elevation)
        {
            if (m_incomingResource2 == ExtendedTransferManager.TransferReason.PetroleumProducts)
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
            TransferManager.TransferReason incomingResource1 = m_incomingResource1;
            TransferManager.TransferReason outgoingTransferReason = GetOutgoingTransferReason(buildingID);
            ExtendedTransferManager.TransferReason incomingResource2 = m_incomingResource2;
            ExtendedTransferManager.TransferReason outgoingResource1 = m_outgoingResource1;
            ExtendedTransferManager.TransferReason outgoingResource2 = m_outgoingResource2;
            if (incomingResource1 != TransferManager.TransferReason.None)
            {
                int count = 0;
                int cargo = 0;
                int capacity = 0;
                int outside = 0;
                if (incomingResource1 == TransferManager.TransferReason.Goods || incomingResource1 == TransferManager.TransferReason.Food)
                {
                    CalculateGuestVehicles(buildingID, ref data, incomingResource1, TransferManager.TransferReason.LuxuryProducts, ref count, ref cargo, ref capacity, ref outside);
                }
                else
                {
                    CalculateGuestVehicles(buildingID, ref data, incomingResource1, ref count, ref cargo, ref capacity, ref outside);
                }
                Citizen.BehaviourData behaviour = default(Citizen.BehaviourData);
                int aliveCount = 0;
                int totalCount = 0;
                GetVisitBehaviour(buildingID, ref data, ref behaviour, ref aliveCount, ref totalCount);
                int num = m_visitPlaceCount;
                int a = Mathf.Min(num * 500, 65535);
                int num2 = Mathf.Max(a, MaxIncomingLoadSize() * 4);
                text = StringUtils.SafeFormat("{0}\n{1}: {2} (+{3}) of {4}", text, incomingResource1.ToString(), GetGoodsAmount(ref data), cargo, num2);
            }
            if (outgoingTransferReason != TransferManager.TransferReason.None)
            {
                text = StringUtils.SafeFormat("{0}\n{1}: {2}", text, outgoingTransferReason.ToString(), data.m_customBuffer2);
            }
            if (incomingResource1 != TransferManager.TransferReason.None)
            {
                int count = 0;
                int cargo = 0;
                int capacity = 0;
                int outside = 0;
                CalculateGuestVehicles(buildingID, ref data, incomingResource1, ref count, ref cargo, ref capacity, ref outside);
                text = StringUtils.SafeFormat("{0}\n{1}: {2} (+{3})", text, incomingResource1.ToString(), data.m_customBuffer1, cargo);
            }
            if (incomingResource2 != ExtendedTransferManager.TransferReason.None)
            {
                int count = 0;
                int cargo = 0;
                int capacity = 0;
                int outside = 0;
                ExtendedVehicleManager.CalculateGuestVehicles(buildingID, ref data, incomingResource2, ref count, ref cargo, ref capacity, ref outside);
                text = StringUtils.SafeFormat("{0}\n{1}: {2} (+{3})", text, incomingResource2.ToString(), data.m_customBuffer1, cargo);
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
            return StringUtils.SafeFormat("{0}\nMoney: {1}/{2}", text, data.m_cashBuffer / 10, GetCashCapacity(buildingID, ref data) / 10); ;
        }

        protected virtual ushort GetGoodsAmount(ref Building data)
        {
            return data.m_customBuffer1;
        }

        protected virtual void SetGoodsAmount(ref Building data, ushort amount)
        {
            data.m_customBuffer1 = amount;
        }

        public override void CreateBuilding(ushort buildingID, ref Building data)
        {
            base.CreateBuilding(buildingID, ref data);
            int workCount = m_workPlaceCount0 + m_workPlaceCount1;
            Singleton<CitizenManager>.instance.CreateUnits(out data.m_citizenUnits, ref Singleton<SimulationManager>.instance.m_randomizer, buildingID, 0, 0, workCount, m_visitPlaceCount, 0, 0);
            int num = MaxIncomingLoadSize();
            int num3 = Mathf.Min(Mathf.Max(m_visitPlaceCount * 500, num * 4), 65535);
            data.m_customBuffer1 = (ushort)Singleton<SimulationManager>.instance.m_randomizer.Int32(num3 - num, num3);
            data.m_cashBuffer = GetCashCapacity(buildingID, ref data) >> 1;
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
            int num = MaxIncomingLoadSize();
            int num2 = m_visitPlaceCount;
            int num3 = Mathf.Min(Mathf.Max(num2 * 500, num * 2), 65535);
            int num4 = Mathf.Min(Mathf.Max(num2 * 500, num * 4), 65535);
            data.m_customBuffer1 = (ushort)(data.m_customBuffer1 + num4 - num3);
            data.m_cashBuffer = GetCashCapacity(buildingID, ref data) >> 1;
        }

        public override void EndRelocating(ushort buildingID, ref Building data)
        {
            base.EndRelocating(buildingID, ref data);
            EnsureCitizenUnits(buildingID, ref data);
            int num = MaxIncomingLoadSize();
            int num2 = m_visitPlaceCount;
            int num3 = Mathf.Min(Mathf.Max(num2 * 500, num * 2), 65535);
            int num4 = Mathf.Min(Mathf.Max(num2 * 500, num * 4), 65535);
            data.m_customBuffer1 = (ushort)(data.m_customBuffer1 + num4 - num3);
            data.m_cashBuffer = GetCashCapacity(buildingID, ref data) >> 1;
        }

        private void EnsureCitizenUnits(ushort buildingID, ref Building data)
        {
            int workCount = m_workPlaceCount0 + m_workPlaceCount1;
            EnsureCitizenUnits(buildingID, ref data, 0, workCount, m_visitPlaceCount, 0);
        }

        protected override void ManualActivation(ushort buildingID, ref Building buildingData)
        {
            if (m_noiseAccumulation != 0)
            {
                Singleton<NotificationManager>.instance.AddWaveEvent(buildingData.m_position, NotificationEvent.Type.Happy, ImmaterialResourceManager.Resource.NoisePollution, m_noiseAccumulation, m_noiseRadius);
            }
            Vector3 position = buildingData.m_position;
            position.y += m_info.m_size.y;
            Singleton<NotificationManager>.instance.AddEvent(NotificationEvent.Type.GainTaxes, position, 1.5f);
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
            Vector3 position = buildingData.m_position;
            position.y += m_info.m_size.y;
            Singleton<NotificationManager>.instance.AddEvent(NotificationEvent.Type.LoseTaxes, position, 1.5f);
        }

        private int GetGoodsCapacity(ushort buildingID, ref Building data)
        {
            int num = MaxIncomingLoadSize();
            int num2 = m_visitPlaceCount;
            return Mathf.Min(Mathf.Max(num2 * 500, num * 4), 65535);
        }

        private int GetCashCapacity(ushort buildingID, ref Building data)
        {
            return GetGoodsCapacity(buildingID, ref data) * 4;
        }

        public override void ModifyMaterialBuffer(ushort buildingID, ref Building data, TransferManager.TransferReason material, ref int amountDelta)
        {
            switch (material)
            {
                case TransferManager.TransferReason.Shopping:
                case TransferManager.TransferReason.ShoppingB:
                case TransferManager.TransferReason.ShoppingC:
                case TransferManager.TransferReason.ShoppingD:
                case TransferManager.TransferReason.ShoppingE:
                case TransferManager.TransferReason.ShoppingF:
                case TransferManager.TransferReason.ShoppingG:
                case TransferManager.TransferReason.ShoppingH:
                    {
                        int customBuffer = data.m_customBuffer2;
                        amountDelta = Mathf.Clamp(amountDelta, -customBuffer, 0);
                        data.m_customBuffer2 = (ushort)(customBuffer + amountDelta);
                        data.m_outgoingProblemTimer = 0;
                        byte park = Singleton<DistrictManager>.instance.GetPark(data.m_position);
                        if (park != 0 && Singleton<DistrictManager>.instance.m_parks.m_buffer[park].IsPedestrianZone)
                        {
                            Singleton<DistrictManager>.instance.m_parks.m_buffer[park].m_tempGoodsSold -= (uint)amountDelta;
                        }
                        int cashCapacity = GetCashCapacity(buildingID, ref data);
                        data.m_cashBuffer = Math.Min(data.m_cashBuffer - amountDelta, cashCapacity);
                        return;
                    }
                case TransferManager.TransferReason.Cash:
                    {
                        int cashBuffer = data.m_cashBuffer;
                        amountDelta = Mathf.Clamp(amountDelta, -cashBuffer, 0);
                        data.m_cashBuffer = cashBuffer + amountDelta;
                        return;
                    }
            }
            if (material == m_incomingResource1 || ((m_incomingResource1 == TransferManager.TransferReason.Goods || m_incomingResource1 == TransferManager.TransferReason.Food) && material == TransferManager.TransferReason.LuxuryProducts))
            {
                int width = data.Width;
                int length = data.Length;
                int num = MaxIncomingLoadSize();
                int num2 = m_visitPlaceCount;
                int num3 = Mathf.Min(Mathf.Max(num2 * 500, num * 4), 65535);
                int goodsAmount = GetGoodsAmount(ref data);
                amountDelta = Mathf.Clamp(amountDelta, 0, num3 - goodsAmount);
                SetGoodsAmount(ref data, (ushort)(goodsAmount + amountDelta));
            }
            else
            {
                base.ModifyMaterialBuffer(buildingID, ref data, material, ref amountDelta);
            }
        }

        public override void GetMaterialAmount(ushort buildingID, ref Building data, TransferManager.TransferReason material, out int amount, out int max)
        {
            switch (material)
            {
                case TransferManager.TransferReason.Shopping:
                case TransferManager.TransferReason.ShoppingB:
                case TransferManager.TransferReason.ShoppingC:
                case TransferManager.TransferReason.ShoppingD:
                case TransferManager.TransferReason.ShoppingE:
                case TransferManager.TransferReason.ShoppingF:
                case TransferManager.TransferReason.ShoppingG:
                case TransferManager.TransferReason.ShoppingH:
                    amount = data.m_customBuffer2;
                    max = 0;
                    return;
                case TransferManager.TransferReason.Cash:
                    amount = data.m_cashBuffer;
                    max = GetCashCapacity(buildingID, ref data);
                    return;
            }
            if (material == m_incomingResource1 || ((m_incomingResource1 == TransferManager.TransferReason.Goods || m_incomingResource1 == TransferManager.TransferReason.Food) && material == TransferManager.TransferReason.LuxuryProducts))
            {
                int width = data.Width;
                int length = data.Length;
                int num = MaxIncomingLoadSize();
                int num2 = m_visitPlaceCount;
                amount = GetGoodsAmount(ref data);
                max = Mathf.Min(Mathf.Max(num2 * 500, num * 4), 65535);
            }
            else
            {
                base.GetMaterialAmount(buildingID, ref data, material, out amount, out max);
            }
        }

        public override void VisitorEnter(ushort buildingID, ref Building data, uint citizen)
        {
            int amountDelta = -100;
            ModifyMaterialBuffer(buildingID, ref data, TransferManager.TransferReason.Shopping, ref amountDelta);
            base.VisitorEnter(buildingID, ref data, citizen);
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
            if(GasStationManager.GasStationBuildingExist(buildingID))
            {
                var gasStation = GasStationManager.GetGasStationBuilding(buildingID);
                if (material == m_incomingResource2)
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
            if (m_incomingResource1 != TransferManager.TransferReason.None)
            {
                TransferManager.TransferOffer offer = new()
                {
                    Building = buildingID
                };
                Singleton<TransferManager>.instance.RemoveIncomingOffer(m_incomingResource1, offer);
                if (m_incomingResource1 == TransferManager.TransferReason.Goods || m_incomingResource1 == TransferManager.TransferReason.Food)
                {
                    Singleton<TransferManager>.instance.RemoveIncomingOffer(TransferManager.TransferReason.LuxuryProducts, offer);
                }
            }
            TransferManager.TransferReason outgoingTransferReason = GetOutgoingTransferReason(buildingID);
            if (outgoingTransferReason != TransferManager.TransferReason.None)
            {
                TransferManager.TransferOffer offer = new()
                {
                    Building = buildingID
                };
                Singleton<TransferManager>.instance.RemoveOutgoingOffer(outgoingTransferReason, offer);
            }
            ExtendedTransferManager.Offer offer2 = default;
            offer2.Building = buildingID;
            if (m_incomingResource2 != ExtendedTransferManager.TransferReason.None)
            {
                Singleton<ExtendedTransferManager>.instance.RemoveIncomingOffer(m_incomingResource2, offer2);
            }
            if (m_outgoingResource1 != ExtendedTransferManager.TransferReason.None)
            {
                Singleton<ExtendedTransferManager>.instance.RemoveOutgoingOffer(m_outgoingResource1, offer2);
            }
            if (m_allowBatteryRecharge && m_outgoingResource2 != ExtendedTransferManager.TransferReason.None)
            {
                Singleton<ExtendedTransferManager>.instance.RemoveOutgoingOffer(m_outgoingResource2, offer2);
            }
            base.BuildingDeactivated(buildingID, ref data);
        }

        public override void SimulationStep(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
        {
            base.SimulationStep(buildingID, ref buildingData, ref frameData);
            SimulationManager instance = Singleton<SimulationManager>.instance;
            uint num = (instance.m_currentFrameIndex & 0xF00) >> 8;
            if (num == 15)
            {
                buildingData.m_finalImport = buildingData.m_tempImport;
                buildingData.m_finalExport = buildingData.m_tempExport;
                buildingData.m_tempImport = 0;
                buildingData.m_tempExport = 0;
            }
        }

        protected override bool CanEvacuate()
        {
            return m_workPlaceCount0 != 0 || m_workPlaceCount1 != 0;
        }

        protected override void HandleWorkAndVisitPlaces(ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveWorkerCount, ref int totalWorkerCount, ref int workPlaceCount, ref int aliveVisitorCount, ref int totalVisitorCount, ref int visitPlaceCount)
        {
            workPlaceCount += m_workPlaceCount0 + m_workPlaceCount1;
            GetWorkBehaviour(buildingID, ref buildingData, ref behaviour, ref aliveWorkerCount, ref totalWorkerCount);
            HandleWorkPlaces(buildingID, ref buildingData, m_workPlaceCount0, m_workPlaceCount1, 0, 0, ref behaviour, aliveWorkerCount, totalWorkerCount);
            visitPlaceCount += m_visitPlaceCount;
            GetVisitBehaviour(buildingID, ref buildingData, ref behaviour, ref aliveVisitorCount, ref totalVisitorCount);
        }

        protected override void ProduceGoods(ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData behaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveVisitorCount, int totalVisitorCount, int visitPlaceCount)
        {
            base.ProduceGoods(buildingID, ref buildingData, ref frameData, productionRate, finalProductionRate, ref behaviour, aliveWorkerCount, totalWorkerCount, workPlaceCount, aliveVisitorCount, totalVisitorCount, visitPlaceCount);
            Notification.ProblemStruct problemStruct = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem1.NoCustomers | Notification.Problem1.NoGoods);
            if (finalProductionRate != 0)
            {
                DistrictManager instance = Singleton<DistrictManager>.instance;
                byte park = instance.GetPark(buildingData.m_position);
                if (m_noiseAccumulation != 0)
                {
                    Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.NoisePollution, m_noiseAccumulation, buildingData.m_position, m_noiseRadius);
                }
                HandleDead(buildingID, ref buildingData, ref behaviour, totalWorkerCount + totalVisitorCount);
                int goodsCapacity = m_goodsCapacity;
                TransferManager.TransferReason outgoingTransferReason = GetOutgoingTransferReason(buildingID);
                if (productionRate != 0)
                {
                    int num16 = goodsCapacity;
                    if (m_incomingResource1 != TransferManager.TransferReason.None)
                    {
                        num16 = Mathf.Min(num16, buildingData.m_customBuffer1);
                    }
                    if (outgoingTransferReason != TransferManager.TransferReason.None)
                    {
                        num16 = Mathf.Min(num16, goodsCapacity - buildingData.m_customBuffer2);
                    }
                    productionRate = Mathf.Max(0, Mathf.Min(productionRate, (num16 * 200 + goodsCapacity - 1) / goodsCapacity));
                    int num17 = (visitPlaceCount * productionRate + 9) / 10;
                    if (Singleton<SimulationManager>.instance.m_isNightTime)
                    {
                        num17 = num17 + 1 >> 1;
                    }
                    num17 = Mathf.Max(0, Mathf.Min(num17, num16));
                    if (m_incomingResource1 != TransferManager.TransferReason.None)
                    {
                        buildingData.m_customBuffer1 -= (ushort)num17;
                    }
                    if (outgoingTransferReason != TransferManager.TransferReason.None)
                    {
                        buildingData.m_customBuffer2 += (ushort)num17;
                    }
                    productionRate = (num17 + 9) / 10;
                }
                int count = 0;
                int cargo = 0;
                int capacity = 0;
                int outside = 0;
                if (m_incomingResource1 != TransferManager.TransferReason.None)
                {
                    CalculateGuestVehicles(buildingID, ref buildingData, m_incomingResource1, ref count, ref cargo, ref capacity, ref outside);
                    buildingData.m_tempImport = (byte)Mathf.Clamp(outside, buildingData.m_tempImport, 255);
                }
                buildingData.m_tempExport = (byte)Mathf.Clamp(behaviour.m_touristCount, buildingData.m_tempExport, 255);
                buildingData.m_adults = (byte)productionRate;
                int num18 = visitPlaceCount * 500;
                if (buildingData.m_customBuffer2 > goodsCapacity - (num18 >> 1) && aliveVisitorCount <= visitPlaceCount >> 1)
                {
                    buildingData.m_outgoingProblemTimer = (byte)Mathf.Min(255, buildingData.m_outgoingProblemTimer + 1);
                    if (buildingData.m_outgoingProblemTimer >= 192)
                    {
                        problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.NoCustomers | Notification.Problem1.MajorProblem);
                    }
                    else if (buildingData.m_outgoingProblemTimer >= 128)
                    {
                        problemStruct = Notification.AddProblems(problemStruct, Notification.Problem1.NoCustomers);
                    }
                }
                else
                {
                    buildingData.m_outgoingProblemTimer = 0;
                }
                if (buildingData.m_customBuffer1 == 0 && m_incomingResource1 != TransferManager.TransferReason.None)
                {
                    buildingData.m_incomingProblemTimer = (byte)Mathf.Min(255, buildingData.m_incomingProblemTimer + 1);
                    problemStruct = ((buildingData.m_incomingProblemTimer >= 64) ? Notification.AddProblems(problemStruct, Notification.Problem1.MajorProblem | Notification.Problem1.NoGoods) : Notification.AddProblems(problemStruct, Notification.Problem1.NoGoods));
                }
                else
                {
                    buildingData.m_incomingProblemTimer = 0;
                }
                if (buildingData.m_fireIntensity == 0 && m_incomingResource1 != TransferManager.TransferReason.None)
                {
                    int num19 = goodsCapacity - buildingData.m_customBuffer1 - capacity;
                    int num20 = MaxIncomingLoadSize();
                    num19 -= num20 >> 1;
                    if (num19 >= 0)
                    {
                        TransferManager.TransferOffer offer = new()
                        {
                            Priority = num19 * 8 / num20,
                            Building = buildingID,
                            Position = buildingData.m_position,
                            Amount = 1,
                            Active = false
                        };
                        Singleton<TransferManager>.instance.AddIncomingOffer(m_incomingResource1, offer);
                    }
                }
                if (buildingData.m_fireIntensity == 0 && outgoingTransferReason != TransferManager.TransferReason.None)
                {
                    int num21 = buildingData.m_customBuffer2 - aliveVisitorCount * 100;
                    int num22 = Mathf.Max(0, visitPlaceCount - totalVisitorCount);
                    if (num21 >= 100 && num22 > 0)
                    {
                        TransferManager.TransferOffer offer2 = new TransferManager.TransferOffer
                        {
                            Priority = Mathf.Max(1, num21 * 8 / goodsCapacity),
                            Building = buildingID,
                            Position = buildingData.m_position,
                            Amount = Mathf.Min(num21 / 100, num22),
                            Active = false
                        };
                        Singleton<TransferManager>.instance.AddOutgoingOffer(outgoingTransferReason, offer2);
                    }
                }
                if (GasStationManager.GasStationBuildingExist(buildingID))
                {
                    var gasStation = GasStationManager.GetGasStationBuilding(buildingID);
                    int missingFuel = m_fuelCapacity - gasStation.FuelAmount;

                    Debug.Log($"GasStationAI: Building {buildingID} has {gasStation.FuelAmount} liters of fuel out of {m_fuelCapacity}, missing {missingFuel} liters.");

                    if (buildingData.m_fireIntensity == 0)
                    {
                        if (missingFuel > m_fuelCapacity * 0.8)
                        {
                            ExtendedTransferManager.Offer offer = default;
                            offer.Building = buildingID;
                            offer.Position = buildingData.m_position;
                            offer.Amount = missingFuel;
                            offer.Active = false;
                            Singleton<ExtendedTransferManager>.instance.AddIncomingOffer(m_incomingResource2, offer);
                        }

                        if (gasStation.FuelAmount > m_fuelCapacity * 0.1)
                        {
                            ExtendedTransferManager.Offer offer = default;
                            offer.Building = buildingID;
                            offer.Position = buildingData.m_position;
                            offer.Amount = 1;
                            offer.Active = false;
                            Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(m_outgoingResource1, offer);
                        }
                    }

                    if (buildingData.m_electricityBuffer > 0)
                    {
                        ExtendedTransferManager.Offer offer = default;
                        offer.Building = buildingID;
                        offer.Position = buildingData.m_position;
                        offer.Amount = 1;
                        offer.Active = false;
                        Singleton<ExtendedTransferManager>.instance.AddOutgoingOffer(m_outgoingResource2, offer);
                    }
                }
            }
            buildingData.m_problems = problemStruct; 
        }

        private int GetTaxRate(ushort buildingID, ref Building buildingData, DistrictPolicies.Taxation taxationPolicies)
        {
            return Singleton<EconomyManager>.instance.GetTaxRate(m_info.m_class.m_service, m_info.m_class.m_subService, (ItemClass.Level)buildingData.m_level, taxationPolicies);
        }

        private int MaxIncomingLoadSize()
        {
            return 4000;
        }

        protected TransferManager.TransferReason GetOutgoingTransferReason(ushort buildingID)
        {
            Randomizer randomizer = new(buildingID);
            return randomizer.Int32(8u) switch
            {
                0 => TransferManager.TransferReason.Shopping,
                1 => TransferManager.TransferReason.ShoppingB,
                2 => TransferManager.TransferReason.ShoppingC,
                3 => TransferManager.TransferReason.ShoppingD,
                4 => TransferManager.TransferReason.ShoppingE,
                5 => TransferManager.TransferReason.ShoppingF,
                6 => TransferManager.TransferReason.ShoppingG,
                7 => TransferManager.TransferReason.ShoppingH,
                _ => TransferManager.TransferReason.Shopping,
            };
        }

        public override string GetLocalizedTooltip()
        {
            string text = LocaleFormatter.FormatGeneric("AIINFO_WATER_CONSUMPTION", GetWaterConsumption() * 16) + Environment.NewLine + LocaleFormatter.FormatGeneric("AIINFO_ELECTRICITY_CONSUMPTION", GetElectricityConsumption() * 16);
            string text2 = LocaleFormatter.FormatGeneric("AIINFO_WORKPLACES_ACCUMULATION", (m_workPlaceCount0 + m_workPlaceCount1).ToString());
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

        private void GetAccumulation(Randomizer r, int productionRate, int taxRate, DistrictPolicies.CityPlanning cityPlanningPolicies, DistrictPolicies.Taxation taxationPolicies, out int entertainment, out int attractiveness)
        {
            entertainment = 1;
            attractiveness = 1;
            if (entertainment != 0)
            {
                entertainment = (productionRate * entertainment + r.Int32(100u)) / 100;
            }
            if (attractiveness != 0)
            {
                attractiveness = (productionRate * attractiveness + r.Int32(100u)) / 100;
            }
            attractiveness = UniqueFacultyAI.IncreaseByBonus(UniqueFacultyAI.FacultyBonus.Tourism, attractiveness);
            entertainment = UniqueFacultyAI.IncreaseByBonus(UniqueFacultyAI.FacultyBonus.Tourism, entertainment);
        }

        public override bool RequireRoadAccess()
        {
            return true;
        }

        public override void CountWorkPlaces(out int workPlaceCount0, out int workPlaceCount1, out int workPlaceCount2, out int workPlaceCount3)
        {
            workPlaceCount0 = m_workPlaceCount0;
            workPlaceCount1 = m_workPlaceCount1;
            workPlaceCount2 = 0;
            workPlaceCount3 = 0;
        }

        public override void CalculateUnspawnPosition(ushort buildingID, ref Building data, ref Randomizer randomizer, VehicleInfo info, out Vector3 position, out Vector3 target)
        {
            position = data.CalculateSidewalkPosition(0f, 2f);
            target = position;
        }

    }
}
