using ColossalFramework.UI;
using UnityEngine;

namespace RoadsideCare.UI
{
    public class RoadCarePanelButton : UIButton
    {
        /// <summary>
        /// Gets the button instance.
        /// </summary>
        public static RoadCarePanelButton Instance { get; private set; }

        /// <summary>
        /// Sets a value indicating whether the tool is active.
        /// </summary>
        public static bool ToolActive
        {
            set
            {
                // Null check - tool may be created before button is initialised.
                if (Instance != null)
                {
                    if (value)
                    {
                        Instance.normalFgSprite = "hovered";
                    }
                    else
                    {
                        Instance.normalFgSprite = "normal";
                    }
                }
            }
        }

        public override void Start()
        {
            normalBgSprite = "ToolbarIconGroup1Nomarl";
            hoveredBgSprite = "ToolbarIconGroup1Hovered";
            focusedBgSprite = "ToolbarIconGroup1Focused";
            pressedBgSprite = "ToolbarIconGroup1Pressed";
            playAudioEvents = true;
            width = 40f;
            height = 40f;
            size = new Vector2(40f, 40f);
            eventClick += delegate (UIComponent component, UIMouseEventParameter eventParam)
            {
                SegmentSelectionTool.ToggleTool();
            };
        }
    }
}
