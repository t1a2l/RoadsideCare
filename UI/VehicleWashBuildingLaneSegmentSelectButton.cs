using ColossalFramework.UI;
using RoadsideCare.Utils;
using UnityEngine;

namespace RoadsideCare.UI
{
    public class VehicleWashBuildingLaneSegmentSelectButton : RoadCarePanelButton
    {

        public override void Start()
        {
            base.Start();
            name = "VehicleWashBuildingLaneSegmentSelect";
            UISprite internalSprite = AddUIComponent<UISprite>();
            internalSprite.atlas = TextureUtils.GetAtlas(Mod.m_atlasName);
            internalSprite.spriteName = "WashLane";
            internalSprite.relativePosition = new Vector3(0, 0);
            internalSprite.width = 40f;
            internalSprite.height = 40f;
        }

    }
}
