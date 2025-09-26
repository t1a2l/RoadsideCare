using HarmonyLib;
using RoadsideCare.UI;

namespace RoadsideCare.HarmonyPatches
{
    [HarmonyPatch(typeof(GameKeyShortcuts), "Escape")]
    public static class Escape
    {
        /// <summary>
        /// Harmony prefix patch to cancel the segment selection tool when it's active and the escape key is pressed.
        /// </summary>
        /// <returns>True (continue on to game method) if the segment selection tool isn't already active, false (pre-empt game method) otherwise.</returns>
        public static bool Prefix()
        {
            // Is the zoning tool active?
            if (SegmentSelectionTool.IsActiveTool)
            {
                // Yes; deactivate the tool and return false (pre-empt original method).
                SegmentSelectionTool.ToggleTool();
                return false;
            }

            // Tool not active - don't do anything, just go on to game code.
            return true;
        }
    }
}
