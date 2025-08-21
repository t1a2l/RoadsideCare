using ColossalFramework.UI;
using UnityEngine;

namespace RoadsideCare.UI
{
    public static class UILayout
    {
        public static Vector2 PositionRightOf(UIComponent uIComponent, float margin = 8f, float verticalOffset = 0f)
        {
            return new Vector2(uIComponent.relativePosition.x + uIComponent.width + margin, uIComponent.relativePosition.y + verticalOffset);
        }

        public static Vector2 PositionUnder(UIComponent uIComponent, float margin = 8f, float horizontalOffset = 0f)
        {
            return new Vector2(uIComponent.relativePosition.x + horizontalOffset, uIComponent.relativePosition.y + uIComponent.height + margin);
        }
    }
}
