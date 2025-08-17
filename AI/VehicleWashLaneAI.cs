namespace RoadsideCare.AI
{
    public class VehicleWashLaneAI : RoadAI
    {
        public override void CreateSegment(ushort segmentID, ref NetSegment data)
        {
            base.CreateSegment(segmentID, ref data);
            NetInfo.Lane[] lanes = data.Info.m_lanes;
            lanes[0].m_speedLimit = 0.01f;
        }

        public override void SegmentLoaded(ushort segmentID, ref NetSegment data, uint version)
        {
            base.SegmentLoaded(segmentID, ref data, version);
            NetInfo.Lane[] lanes = data.Info.m_lanes;
            lanes[0].m_speedLimit = 0.01f;
        }

        public static float GetMaxSpeed(ushort segmentID, ref NetSegment data)
        {
            NetInfo.Lane[] lanes = data.Info.m_lanes;
            return lanes[0].m_speedLimit;
        }
    }
}
