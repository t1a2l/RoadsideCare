using ColossalFramework;
using ColossalFramework.UI;
using RoadsideCare.Utils;
using RoadsideCare.AI;
using RoadsideCare.Managers;
using UnityEngine;

namespace RoadsideCare.UI
{
    public class SegmentSelectionTool : DefaultTool
    {
        // UI thread to simulation thread communication.
        private readonly object _simulationLock = new();
        private ushort _currentSegmentID = 0;
        private static ushort _currentBuilding = 0;
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
        /// Sets the building currently selected by the info panel.
        /// </summary>
        internal ushort CurrentBuilding { set => _currentBuilding = value; }

        /// <summary>
        /// Gets a value indicating whether terrain is ignored by the tool (always returns true, i.e. terrain is ignored by the tool).
        /// </summary>
        /// <returns>True.</returns>
        public override bool GetTerrainIgnore() => true;

        /// <summary>
        /// Gets which net nodes are ignored by the tool (always returns all, i.e. none are selectable by the tool).
        /// </summary>
        /// <returns>NetNode.Flags.All.</returns>
        public override NetNode.Flags GetNodeIgnoreFlags() => NetNode.Flags.All;

        /// <summary>
        /// Gets which buildings are ignored by the tool (always returns all, i.e. none are selectable by the tool).
        /// </summary>
        /// <returns>Building.Flags.All.</returns>
        public override Building.Flags GetBuildingIgnoreFlags() => Building.Flags.All;

        /// <summary>
        /// Gets which trees are ignored by the tool (always returns all, i.e. none are selectable by the tool).
        /// </summary>
        /// <returns>TreeInstance.Flags.All.</returns>
        public override global::TreeInstance.Flags GetTreeIgnoreFlags() => global::TreeInstance.Flags.All;

        /// <summary>
        /// Gets which props are ignored by the tool (always returns all, i.e. none are selectable by the tool).
        /// </summary>
        /// <returns>PropInstance.Flags.All.</returns>
        public override PropInstance.Flags GetPropIgnoreFlags() => PropInstance.Flags.All;

        /// <summary>
        /// Gets which parked vehicles are ignored by the tool (always returns all, i.e. none are selectable by the tool).
        /// </summary>
        /// <returns>VehicleParked.Flags.All.</returns>
        public override VehicleParked.Flags GetParkedVehicleIgnoreFlags() => VehicleParked.Flags.All;

        /// <summary>
        /// Gets which citizens are ignored by the tool (always returns all, i.e. none are selectable by the tool).
        /// </summary>
        /// <returns>CitizenInstance.Flags.All.</returns>
        public override CitizenInstance.Flags GetCitizenIgnoreFlags() => CitizenInstance.Flags.All;

        /// <summary>
        /// Gets which transport lines are ignored by the tool (always returns all, i.e. none are selectable by the tool).
        /// </summary>
        /// <returns>TransportLine.Flags.All.</returns>
        public override TransportLine.Flags GetTransportIgnoreFlags() => TransportLine.Flags.All;

        /// <summary>
        /// Gets a value indicating which transport types are supported by the tool (always returns zero, i.e. no transport type is supported by the tool).
        /// </summary>
        /// <returns>Zero.</returns>
        public override int GetTransportTypes() => 0;

        /// <summary>
        /// Gets which districts are ignored by the tool (always returns all, i.e. none are selectable by the tool).
        /// </summary>
        /// <returns>District.Flags.All.</returns>
        public override District.Flags GetDistrictIgnoreFlags() => District.Flags.All;

        /// <summary>
        /// Gets which parks are ignored by the tool (always returns all, i.e. none are selectable by the tool).
        /// </summary>
        /// <returns>DistrictPark.Flags.All.</returns>
        public override DistrictPark.Flags GetParkIgnoreFlags() => DistrictPark.Flags.All;

        /// <summary>
        /// Gets which disasters are ignored by the tool (always returns all, i.e. none are selectable by the tool).
        /// </summary>
        /// <returns>DisasterData.Flags.All.</returns>
        public override DisasterData.Flags GetDisasterIgnoreFlags() => DisasterData.Flags.All;

        /// <summary>
        /// Gets which network segments are ignored by the tool (always returns none, i.e. all are selectable by the tool).
        /// </summary>
        /// <param name="nameOnly">Always set to false.</param>
        /// <returns>NetSegment.Flags.None.</returns>
        public override NetSegment.Flags GetSegmentIgnoreFlags(out bool nameOnly)
        {
            nameOnly = false;
            return NetSegment.Flags.None;
        }

        /// <summary>
        /// Sets vehicle ingore flags to ignore all vehicles.
        /// </summary>
        /// <returns>Vehicle flags ignoring all vehicles.</returns>
        public override Vehicle.Flags GetVehicleIgnoreFlags() =>
            Vehicle.Flags.LeftHandDrive
            | Vehicle.Flags.Created
            | Vehicle.Flags.Deleted
            | Vehicle.Flags.Spawned
            | Vehicle.Flags.Inverted
            | Vehicle.Flags.TransferToTarget
            | Vehicle.Flags.TransferToSource
            | Vehicle.Flags.Emergency1
            | Vehicle.Flags.Emergency2
            | Vehicle.Flags.WaitingPath
            | Vehicle.Flags.Stopped
            | Vehicle.Flags.Leaving
            | Vehicle.Flags.Arriving
            | Vehicle.Flags.Reversed
            | Vehicle.Flags.TakingOff
            | Vehicle.Flags.Flying
            | Vehicle.Flags.Landing
            | Vehicle.Flags.WaitingSpace
            | Vehicle.Flags.WaitingCargo
            | Vehicle.Flags.GoingBack
            | Vehicle.Flags.WaitingTarget
            | Vehicle.Flags.Importing
            | Vehicle.Flags.Exporting
            | Vehicle.Flags.Parking
            | Vehicle.Flags.CustomName
            | Vehicle.Flags.OnGravel
            | Vehicle.Flags.WaitingLoading
            | Vehicle.Flags.Congestion
            | Vehicle.Flags.DummyTraffic
            | Vehicle.Flags.Underground
            | Vehicle.Flags.Transition
            | Vehicle.Flags.InsideBuilding;

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
                if (_currentSegmentID != 0 && _currentBuilding != 0)
                {
                    if (GasStationManager.GasStationBuildingExist(_currentBuilding))
                    {
                        var gasStation = GasStationManager.GetGasStationBuilding(_currentBuilding);

                        if (gasStation.FuelPoints.Contains(_currentSegmentID))
                        {
                            // Remove fuel point.
                            gasStation.FuelPoints.Remove(_currentSegmentID);
                            Debug.Log($"[RoadsideCare] Removed fuel point segment {_currentSegmentID} from gas station building {_currentBuilding}.");
                        }
                        else
                        {
                            // Add fuel point.
                            gasStation.FuelPoints.Add(_currentSegmentID);
                            Debug.Log($"[RoadsideCare] Added fuel point segment {_currentSegmentID} to gas station building {_currentBuilding}.");
                        }
                    }

                    if (VehicleWashBuildingManager.VehicleWashBuildingExist(_currentBuilding))
                    {
                        var vehicleWash = VehicleWashBuildingManager.GetVehicleWashBuilding(_currentBuilding);




                        if(_isWashLane)
                        {
                            if (vehicleWash.VehicleWashLanes.Contains(_currentSegmentID))
                            {
                                // Remove wash lane.
                                vehicleWash.VehicleWashLanes.Remove(_currentSegmentID);
                                Debug.Log($"[RoadsideCare] Removed wash lane segment {_currentSegmentID} from wash vehicle building {_currentBuilding}.");
                            }
                            else
                            {
                                // Add wash lane.
                                vehicleWash.VehicleWashLanes.Add(_currentSegmentID);
                                Debug.Log($"[RoadsideCare] Added wash lane segment {_currentSegmentID} to wash vehicle building {_currentBuilding}.");
                            }
                        }
                        else
                        {
                            if (vehicleWash.VehicleWashPoints.Contains(_currentSegmentID))
                            {
                                // Remove wash point.
                                vehicleWash.VehicleWashPoints.Remove(_currentSegmentID);
                                Debug.Log($"[RoadsideCare] Removed wash point segment {_currentSegmentID} from wash vehicle building {_currentBuilding}.");
                            }
                            else
                            {
                                // Add wash point.
                                vehicleWash.VehicleWashPoints.Add(_currentSegmentID);
                                Debug.Log($"[RoadsideCare] Added wash point segment {_currentSegmentID} to wash vehicle building { _currentBuilding}.");
                            }
                        }
                    }
                }

                // Clear segment reference to indicate that work is donw.
                _currentSegmentID = 0;
                
            }
        }

        /// <summary>
        /// Toggles the current tool to/from the zoning tool.
        /// </summary>
        internal static void ToggleTool()
        {
            // Activate zoning tool if it isn't already; if already active, deactivate it by selecting the previously active tool instead.
            if (!IsActiveTool)
            {
                // Activate tool.
                ToolsModifierControl.toolController.CurrentTool = Instance;
            }
            else
            {
                // Activate default tool.
                ToolsModifierControl.SetTool<DefaultTool>();
            }
        }

        /// <summary>
        /// Initialise the tool.
        /// Called by unity when the tool is created.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // Set default cursor.
            m_cursor = TextureUtils.LoadCursor("ZoningAdjusterCursor.png");
        }

        /// <summary>
        /// Unity late update handling.
        /// Called by game every late update.
        /// </summary>
        protected override void OnToolLateUpdate()
        {
            base.OnToolLateUpdate();

            // Force the info mode to none.
            ForceInfoMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.None);
        }

        /// <summary>
        /// Called by game when tool is enabled.
        /// </summary>
        protected override void OnEnable()
        {
            // Call base even before loaded checks to properly initialize tool.
            base.OnEnable();

            Debug.Log("RoadsideCare tool enabled");

            // Set button state to indicate tool is active.
            RoadCarePanelButton.ToolActive = true;
        }

        /// <summary>
        /// Called by game when tool is disabled.
        /// </summary>
        protected override void OnDisable()
        {
            Debug.Log("RoadsideCare tool disabled");

            base.OnDisable();

            // Set panel button state to indicate tool no longer active.
            RoadCarePanelButton.ToolActive = false;
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
            if (segmentID != 0 && _currentBuilding != 0)
            {
                var segment = Singleton<NetManager>.instance.m_segments.m_buffer[segmentID];

                var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[_currentBuilding];

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
                    if(isVehicleWashLane)
                    {
                        _isWashLane = true;
                    }
                    else
                    {
                        _isWashLane = false;
                    }
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
                            _currentSegmentID = segmentID;
                        }
                    }
                }
            }
        }

        
    }
}
