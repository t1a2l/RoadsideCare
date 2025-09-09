using System;
using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using RoadsideCare.AI;
using RoadsideCare.HarmonyPatches;
using RoadsideCare.Managers;
using RoadsideCare.UI;
using RoadsideCare.Utils;
using UnityEngine;

namespace RoadsideCare
{
    public class Mod : LoadingExtensionBase, IUserMod
    {
        string IUserMod.Name => "Roadside Care";

        string IUserMod.Description => "Track individual vehicles' needs and strategically place gas stations, car washes, and repair shops as roadside care for vehicles.";

        public static bool IsEnabled = false;
        public static LoadMode CurrentLoadMode;
        public static bool isGuiRunning = false;
        public static GasStationSegmentSelectButton GSButton;
        public static VehicleWashBuildingLaneSegmentSelectButton VWPButton;
        public static VehicleWashBuildingPointSegmentSelectButton VWLButton;
        public static string m_atlasName = "RoadsideCareAtlas.png";
        public static bool m_atlasLoaded;
        private const float LeftMargin = 24f;

        public void OnEnabled()
        {
            RoadsideCareSettings.Init();
            HarmonyHelper.DoOnHarmonyReady(() => PatchUtil.PatchAll());
            IsEnabled = true;
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) PatchUtil.UnpatchAll();
            IsEnabled = false;
        }

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            try
            {
                VehicleNeedsManager.Init();
                GasStationManager.Init();
                VehicleWashBuildingManager.Init();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                VehicleNeedsManager.Deinit();
                GasStationManager.Deinit();
                VehicleWashBuildingManager.Deinit();
            }
        }

        public override void OnReleased()
        {
            base.OnReleased();
            try
            {
                VehicleNeedsManager.Deinit();
                GasStationManager.Deinit();
                VehicleWashBuildingManager.Deinit();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            switch (mode)
            {
                case LoadMode.LoadGame:
                case LoadMode.NewGame:
                case LoadMode.LoadScenario:
                case LoadMode.NewGameFromScenario:
                    break;

                default:
                    return;
            }

            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
            {
                SetupGui();
            }

            var buildings = Singleton<BuildingManager>.instance.m_buildings;

            for (ushort buildingId = 0; buildingId < buildings.m_size; buildingId++)
            {
                var building = buildings.m_buffer[buildingId];
                if ((building.m_flags & Building.Flags.Created) != 0)
                {
                    if (building.Info.GetAI() is GasStationAI || building.Info.GetAI() is GasPumpAI)
                    {
                        if (!GasStationManager.GasStationBuildingExist(buildingId))
                        {
                            GasStationManager.CreateGasStationBuilding(buildingId, 0, []);
                        }
                    }
                    else if (building.Info.GetAI() is VehicleWashBuildingAI)
                    {
                        if (!VehicleWashBuildingManager.VehicleWashBuildingExist(buildingId))
                        {
                            VehicleWashBuildingManager.CreateVehicleWashBuilding(buildingId, [], []);
                        }
                    }
                }
            }

            var vehicles = Singleton<VehicleManager>.instance.m_vehicles;

            for (ushort vehicleId = 0; vehicleId < vehicles.m_size; vehicleId++)
            {
                ref var vehicle = ref vehicles.m_buffer[vehicleId];
                if ((vehicle.m_flags & Vehicle.Flags.Created) != 0)
                {
                    if (!VehicleNeedsManager.VehicleNeedsExist(vehicleId))
                    {
                        VehicleAIPatch.CreateNeedsForVehicle(vehicle.Info.m_vehicleAI, vehicleId, ref vehicle);
                    }
                }
            }
        }

        public override void OnLevelUnloading()
        {
            if (CurrentLoadMode == LoadMode.LoadGame || CurrentLoadMode == LoadMode.NewGame)
            {
                if (IsEnabled && isGuiRunning)
                {
                    RemoveGui();
                }
            }
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelper Fueling_Time = helper.AddGroup("Fueling Time") as UIHelper;

            var PassengerCarFuelingTimeInSeconds = UISliders.AddPlainSliderWithValue((UIComponent)Fueling_Time.self, LeftMargin, 0, "Passenger Car Fueling Time In Seconds", 1, 20, 1, RoadsideCareSettings.PassengerCarFuelingTimeInSeconds.value);

            PassengerCarFuelingTimeInSeconds.eventValueChanged += (c, value) =>
            {
                RoadsideCareSettings.PassengerCarFuelingTimeInSeconds.value = value;
            };

            var CargoTruckFuelingTimeInSeconds = UISliders.AddPlainSliderWithValue((UIComponent)Fueling_Time.self, LeftMargin, 0, "Cargo Truck Fueling Time In Seconds", 1, 20, 1, RoadsideCareSettings.CargoTruckFuelingTimeInSeconds.value);

            CargoTruckFuelingTimeInSeconds.eventValueChanged += (c, value) =>
            {
                RoadsideCareSettings.CargoTruckFuelingTimeInSeconds.value = value;
            };

            UIHelper HandWash_Time = helper.AddGroup("Hand Wash Time") as UIHelper;

            var PassengerCarHandWashTimeInSeconds = UISliders.AddPlainSliderWithValue((UIComponent)HandWash_Time.self, LeftMargin, 0, "Passenger Car Hand Wash Time In Seconds", 1, 20, 1, RoadsideCareSettings.PassengerCarHandWashTimeInSeconds.value);

            PassengerCarHandWashTimeInSeconds.eventValueChanged += (c, value) =>
            {
                RoadsideCareSettings.PassengerCarHandWashTimeInSeconds.value = value;
            };

            var CargoTruckHandWashTimeInSeconds = UISliders.AddPlainSliderWithValue((UIComponent)HandWash_Time.self, LeftMargin, 0, "Cargo Truck Hand Wash Time In Seconds", 1, 20, 1, RoadsideCareSettings.CargoTruckHandWashTimeInSeconds.value);

            CargoTruckHandWashTimeInSeconds.eventValueChanged += (c, value) =>
            {
                RoadsideCareSettings.CargoTruckHandWashTimeInSeconds.value = value;
            };

        }

        private static void LoadSprites()
        {
            var SpriteNames = new string[] {
                "FuelPoint",
                "HandWash",
                "WashLane"
            };

            if (TextureUtils.GetAtlas(m_atlasName) == null)
            {
                m_atlasLoaded = TextureUtils.InitialiseAtlas(m_atlasName);
                for (int i = 0; i < SpriteNames.Length; i++)
                {
                    TextureUtils.AddSpriteToAtlas(new Rect(111 * i, 1, 111, 102), SpriteNames[i], m_atlasName);
                }
            }
        }

        public static void SetupGui()
        {
            LoadSprites();
            if (m_atlasLoaded)
            {
                SetupPlayerBuildingButton();
                isGuiRunning = true;
            }
            
        }

        public static void RemoveGui()
        {
            isGuiRunning = false;
            if (GSButton != null)
            {
                UnityEngine.Object.Destroy(GSButton);
                GSButton = null;
            }
            if (VWLButton != null)
            {
                UnityEngine.Object.Destroy(VWLButton);
                VWLButton = null;
            }
            if (VWPButton != null)
            {
                UnityEngine.Object.Destroy(VWPButton);
                VWPButton = null;
            }
        }

        public static void SetupPlayerBuildingButton()
        {
            var playerbuildingInfo = UIView.Find<UIPanel>("(Library) CityServiceWorldInfoPanel");
            if (GSButton == null)
            {
                GSButton = (playerbuildingInfo.AddUIComponent(typeof(GasStationSegmentSelectButton)) as GasStationSegmentSelectButton);
            }
            if (VWLButton == null)
            {
                VWLButton = (playerbuildingInfo.AddUIComponent(typeof(VehicleWashBuildingPointSegmentSelectButton)) as VehicleWashBuildingPointSegmentSelectButton);
            }
            if (VWPButton == null)
            {
                VWPButton = (playerbuildingInfo.AddUIComponent(typeof(VehicleWashBuildingLaneSegmentSelectButton)) as VehicleWashBuildingLaneSegmentSelectButton);
            }

            GSButton.width = 30f;
            GSButton.height = 40f;
            GSButton.relativePosition = new Vector3(playerbuildingInfo.size.x - GSButton.width - 90, playerbuildingInfo.size.y - GSButton.height);
            GSButton.Hide();

            VWLButton.width = 30f;
            VWLButton.height = 40f;
            VWLButton.relativePosition = new Vector3(playerbuildingInfo.size.x - GSButton.width - VWLButton.width - 90, playerbuildingInfo.size.y - GSButton.height - VWLButton.height);
            VWLButton.Hide();

            VWPButton.width = 30f;
            VWPButton.height = 40f;
            VWPButton.relativePosition = new Vector3(playerbuildingInfo.size.x - GSButton.width - VWLButton.width - VWPButton.width - 90, playerbuildingInfo.size.y - GSButton.height - VWLButton.height - VWPButton.width);
            VWPButton.Hide();
        }

    }


    public class CleanupStaleParkedVehicle : ThreadingExtensionBase
    {
        private int cleanupCounter = 0;
        private const int cleanupInterval = 600; // Run cleanup every 10 seconds (600 frames)

        public override void OnBeforeSimulationFrame()
        {
            // Your main mod logic here...

            // Increment and check the counter
            cleanupCounter++;
            if (cleanupCounter >= cleanupInterval)
            {
                VehicleNeedsManager.CleanupStaleParkedVehicleNeeds();
                cleanupCounter = 0; // Reset the counter
            }
        }
    }


}
