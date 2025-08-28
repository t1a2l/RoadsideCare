using System;
using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
using RoadsideCare.Managers;

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

            bool isBroken = false;
            bool isOutOfFuel = false;

            if (___m_InstanceID.Type == InstanceType.Vehicle && ___m_InstanceID.Vehicle != 0 && VehicleNeedsManager.VehicleNeedsExist(___m_InstanceID.Vehicle))
            {
                vehicleId = ___m_InstanceID.Vehicle;
                vehicleInfo = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[___m_InstanceID.Vehicle].Info;
                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleId);
                fuelValue = vehicleNeeds.FuelAmount / vehicleNeeds.FuelCapacity;
                dirtValue = vehicleNeeds.DirtPercentage / 100;
                //wearValue = vehicleNeeds.WearPercentage / 100;
                isBroken = (vehicleNeeds.StateFlags & VehicleNeedsManager.VehicleStateFlags.IsBroken) != 0;
                isOutOfFuel = (vehicleNeeds.StateFlags & VehicleNeedsManager.VehicleStateFlags.IsOutOfFuel) != 0;
            }
            else if (___m_InstanceID.Type == InstanceType.ParkedVehicle && ___m_InstanceID.ParkedVehicle != 0 && VehicleNeedsManager.ParkedVehicleNeedsExist(___m_InstanceID.ParkedVehicle))
            {
                parkedVehicleId = ___m_InstanceID.ParkedVehicle;
                vehicleInfo = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[___m_InstanceID.ParkedVehicle].Info;
                var parkedVehicleNeeds = VehicleNeedsManager.GetParkedVehicleNeeds(vehicleId);
                fuelValue = parkedVehicleNeeds.FuelAmount / parkedVehicleNeeds.FuelCapacity;
                dirtValue = parkedVehicleNeeds.DirtPercentage / 100;
                //wearValue = parkedVehicleNeeds.WearPercentage / 100;
                isBroken = (parkedVehicleNeeds.StateFlags & VehicleNeedsManager.ParkedVehicleStateFlags.IsBroken) != 0;
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
    }
}
