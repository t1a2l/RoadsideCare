using ColossalFramework;
using ColossalFramework.UI;
using RoadsideCare.AI;
using RoadsideCare.Managers;
using UnityEngine;

namespace RoadsideCare.UI
{
    public class SegmentSelectionTool : DefaultTool
    {
        // UI thread to simulation thread communication.
        private readonly object _simulationLock = new();
        private ushort _segmentID = 0;
        private static ushort _buildingID = 0;
        private static bool _isWashLane = false;

        /// <summary>
        /// Gets the active tool instance.
        /// </summary>
        public static SegmentSelectionTool Instance => ToolsModifierControl.toolController?.gameObject?.GetComponent<SegmentSelectionTool>();

        /// <summary>
        /// Gets a value indicating whether the tool is currently active (true) or inactive (false).
        /// </summary>
        public static bool IsActiveTool => Instance != null && ToolsModifierControl.toolController.CurrentTool == Instance;

        /// <summary>
        /// Called by the game every simulation step.
        /// Used to perform any zone manipulations from the simulation thread.
        /// </summary>
        public override void SimulationStep()
        {
            base.SimulationStep();

            // Thread locking.
            lock (_simulationLock)
            {
                // Check to see if there's any valid segment.
                if (_segmentID != 0 && _buildingID != 0)
                {
                    if (GasStationManager.GasStationBuildingExist(_buildingID))
                    {
                        var gasStation = GasStationManager.GetGasStationBuilding(_buildingID);

                        if (gasStation.FuelPoints.Contains(_segmentID))
                        {
                            // Remove fuel point.
                            gasStation.FuelPoints.Remove(_segmentID);
                            Debug.Log($"[RoadsideCare] Removed fuel point segment {_segmentID} from gas station building {_buildingID}.");
                        }
                        else
                        {
                            // Add fuel point.
                            gasStation.FuelPoints.Add(_segmentID);
                            Debug.Log($"[RoadsideCare] Added fuel point segment {_segmentID} to gas station building {_buildingID}.");
                        }
                    }

                    if (VehicleWashBuildingManager.VehicleWashBuildingExist(_buildingID))
                    {
                        var vehicleWash = VehicleWashBuildingManager.GetVehicleWashBuilding(_buildingID);

                        if(_isWashLane)
                        {
                            if (vehicleWash.VehicleWashLanes.Contains(_segmentID))
                            {
                                // Remove wash lane.
                                vehicleWash.VehicleWashLanes.Remove(_segmentID);
                                Debug.Log($"[RoadsideCare] Removed wash lane segment {_segmentID} from wash vehicle building {_buildingID}.");
                            }
                            else
                            {
                                // Add wash lane.
                                vehicleWash.VehicleWashLanes.Add(_segmentID);
                                Debug.Log($"[RoadsideCare] Added wash lane segment {_segmentID} to wash vehicle building {_buildingID}.");
                            }
                        }
                        else
                        {
                            if (vehicleWash.VehicleWashPoints.Contains(_segmentID))
                            {
                                // Remove wash point.
                                vehicleWash.VehicleWashPoints.Remove(_segmentID);
                                Debug.Log($"[RoadsideCare] Removed wash point segment {_segmentID} from wash vehicle building {_buildingID}.");
                            }
                            else
                            {
                                // Add wash point.
                                vehicleWash.VehicleWashPoints.Add(_segmentID);
                                Debug.Log($"[RoadsideCare] Added wash point segment {_segmentID} to wash vehicle building {_buildingID}.");
                            }
                        }



                    }
                }

                // Clear segment reference to indicate that work is donw.
                _segmentID = 0;
                
            }
        }

        /// <summary>
        /// Toggles the current tool to/from the zoning tool.
        /// </summary>
        internal static void ToggleTool(ushort buildingID, bool isWashLane = false)
        {
            // Activate zoning tool if it isn't already; if already active, deactivate it by selecting the previously active tool instead.
            if (!IsActiveTool)
            {
                // Activate tool.
                ToolsModifierControl.toolController.CurrentTool = Instance;
                _buildingID = buildingID;
                _isWashLane = isWashLane;
            }
            else
            {
                // Activate default tool.
                ToolsModifierControl.SetTool<DefaultTool>();
                _buildingID = 0;
                _isWashLane = false;
            }
        }

        /// <summary>
        /// Tool GUI event processing.
        /// Called by game every GUI update.
        /// </summary>
        /// <param name="e">Event.</param>
        protected override void OnToolGUI(Event e)
        {
            // Check for escape key.
            if (e.type == EventType.keyDown && e.keyCode == KeyCode.Escape)
            {
                // Escape key pressed - revert to default tool.
                e.Use();
                ToolsModifierControl.SetTool<DefaultTool>();
            }

            // Don't do anything if mouse is inside UI or if there are any errors other than failed raycast.
            if (m_toolController.IsInsideUI || (m_selectErrors != ToolErrors.None && m_selectErrors != ToolErrors.RaycastFailed))
            {
                return;
            }

             // Try to get a hovered network instance.
            ushort segmentID = m_hoverInstance.NetSegment;
            if (segmentID != 0 && _buildingID != 0)
            {
                var segment = Singleton<NetManager>.instance.m_segments.m_buffer[segmentID];

                var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[_buildingID];

                bool isFuelPoint = false;
                bool isVehicleWashPoint = false;
                bool isVehicleWashLane = false;

                if (building.Info.m_buildingAI is GasPumpAI)
                {
                    isFuelPoint = segment.Info.m_netAI is FuelPointAI || segment.Info.m_netAI is FuelPointSmallAI || segment.Info.m_netAI is FuelPointLargeAI;
                }
                else if(building.Info.m_buildingAI is VehicleWashBuildingAI)
                {
                   isVehicleWashPoint = segment.Info.m_netAI is VehicleWashPointAI || segment.Info.m_netAI is VehicleWashPointSmallAI || segment.Info.m_netAI is VehicleWashPointLargeAI;
                   isVehicleWashLane = segment.Info.m_netAI is VehicleWashLaneAI || segment.Info.m_netAI is VehicleWashLaneSmallAI || segment.Info.m_netAI is VehicleWashLaneLargeAI;
                }

                if (isFuelPoint || isVehicleWashPoint || isVehicleWashLane)
                {
                    // Check for mousedown events.
                    if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 1))
                    {
                        // Got one; use the event.
                        UIInput.MouseUsed();

                        // Need to update zoning via simulation thread - set the fields for SimulationStep to pick up.
                        lock (_simulationLock)
                        {
                            _segmentID = segmentID;
                        }
                    }
                }
            }
        }
    }
}
