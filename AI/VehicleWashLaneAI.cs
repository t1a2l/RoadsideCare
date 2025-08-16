using ColossalFramework;
using MoreTransferReasons.AI;
using RoadsideCare.Managers;

namespace RoadsideCare.AI
{
    public class VehicleWashLaneAI : RoadAI
    {
        public override void CreateSegment(ushort segmentID, ref NetSegment data)
        {
            base.CreateSegment(segmentID, ref data);
            NetInfo.Lane[] lanes = data.Info.m_lanes;
            lanes[0].m_speedLimit = 0.1f;
        }

        public override void SegmentLoaded(ushort segmentID, ref NetSegment data, uint version)
        {
            base.SegmentLoaded(segmentID, ref data, version);
            NetInfo.Lane[] lanes = data.Info.m_lanes;
            lanes[0].m_speedLimit = 0.1f;
        }

        public override void SimulationStep(ushort segmentID, ref NetSegment data)
        {
            base.SimulationStep(segmentID, ref data);

            ProcessVehiclesOnSegment(segmentID, ref data);
        }

        private void ProcessVehiclesOnSegment(ushort segmentID, ref NetSegment data)
        {
            VehicleManager vehicleManager = VehicleManager.instance;

            for (int i = 0; i < vehicleManager.m_vehicles.m_size; i++)
            {
                ushort vehicleID = (ushort)i;

                if (vehicleID == 0) continue;

                var vehicleNeeds = VehicleNeedsManager.GetVehicleNeeds(vehicleID);

                if (vehicleNeeds.IsBeingWashed)
                {
                    ref Vehicle vehicle = ref vehicleManager.m_vehicles.m_buffer[vehicleID];

                    // Ensure the vehicle is active and on our segment
                    if ((vehicle.m_flags & Vehicle.Flags.Created) != 0 && vehicle.m_path != 0)
                    {
                        PathUnit.Position pathPos = PathManager.instance.m_pathUnits.m_buffer[vehicle.m_path].m_position00;

                        if (pathPos.m_segment == segmentID)
                        {
                            // Apply your washing logic here
                            // For example, reduce the dirt level
                            var dirt_level = vehicleNeeds.DirtPercentage - 0.5f;

                            // Ensure dirt doesn't go below zero
                            if (dirt_level < 0)
                            {
                                dirt_level = 0;
                            }

                            VehicleNeedsManager.SetDirtPercentage(vehicleID, dirt_level);

                            if(dirt_level <= 0)
                            {
                                VehicleNeedsManager.SetNoneCareMode(vehicleID);
                                var targetBuilding = vehicleNeeds.OriginalTargetBuilding;

                                vehicle.m_flags &= ~Vehicle.Flags.WaitingPath;
                                VehicleNeedsManager.SetOriginalTargetBuilding(vehicleID, 0);

                                if(vehicle.Info.GetAI() is PassengerCarAI passengerCarAI)
                                {
                                    var citizenId = passengerCarAI.GetOwnerID(vehicleID, ref vehicle).Citizen;
                                    ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];
                                    var citizenInstance = Singleton<CitizenManager>.instance.m_instances.m_buffer[citizen.m_instance];
                                    var humanAI = citizen.GetCitizenInfo(citizenId).GetAI() as HumanAI;
                                    humanAI.StartMoving(citizenId, ref citizen, citizenInstance.m_targetBuilding, targetBuilding);
                                }

                                if (vehicle.Info.GetAI() is ExtendedCargoTruckAI extendedCargoTruckAI)
                                {
                                    extendedCargoTruckAI.SetTarget(vehicleID, ref vehicle, targetBuilding);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
