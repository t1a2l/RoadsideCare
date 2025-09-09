using ColossalFramework;
using ColossalFramework.UI;
using RoadsideCare.AI;
using RoadsideCare.Managers;
using RoadsideCare.Utils;
using UnityEngine;

namespace RoadsideCare.UI
{
    public class GasStationSegmentSelectButton : UIButton
    {
        private UIPanel playerBuildingInfo;
        private ushort BuildingID = 0;

        public override void Start()
        {
            normalBgSprite = "ToolbarIconGroup1Nomarl";
            hoveredBgSprite = "ToolbarIconGroup1Hovered";
            focusedBgSprite = "ToolbarIconGroup1Focused";
            pressedBgSprite = "ToolbarIconGroup1Pressed";
            playAudioEvents = true;
            name = "GasStationSegmentSelect";
            UISprite internalSprite = AddUIComponent<UISprite>();
            internalSprite.atlas = TextureUtils.GetAtlas(Mod.m_atlasName);
            internalSprite.spriteName = "FuelPoint";
            internalSprite.relativePosition = new Vector3(0, 0);
            internalSprite.width = 40f;
            internalSprite.height = 40f;
            width = 40f;
            height = 40f;
            size = new Vector2(40f, 40f);
            eventClick += delegate (UIComponent component, UIMouseEventParameter eventParam)
            {
                SegmentSelectionTool.ToggleTool(BuildingID);
            };
            playerBuildingInfo = UIView.Find<UIPanel>("(Library) CityServiceWorldInfoPanel");
            if (playerBuildingInfo == null)
            {
                Debug.Log("UIPanel not found (update broke the mod!): (Library) CityServiceWorldInfoPanel\nAvailable panels are:\n");
            }
        }

        public override void Update()
        {
            var BuildingId = WorldInfoPanel.GetCurrentInstanceID().Building;

            if (BuildingId != 0)
            {
                var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[BuildingId];
                if (building.Info.GetAI() is GasPumpAI && GasStationManager.GasStationBuildingExist(BuildingId))
                {
                    BuildingID = BuildingId;
                    relativePosition = new Vector3(playerBuildingInfo.size.x - width - 90, playerBuildingInfo.size.y - height);
                    Show();
                }
                else
                {
                    Hide();
                }
            }

            base.Update();
        }
    }
}
