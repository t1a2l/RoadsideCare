using System.Reflection;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using UnityEngine;

namespace RoadsideCare.AI
{
    public class CustomPathFindAI : CargoTruckAI
    {
        private delegate bool StartPathFindCargoTruckAIDelegate(CargoTruckAI instance, ushort vehicleID, ref Vehicle vehicleData, Vector3 startPos, Vector3 endPos, bool startBothWays, bool endBothWays, bool undergroundTarget);
        private static readonly StartPathFindCargoTruckAIDelegate StartPathFindCargoTruckAI = AccessTools.MethodDelegate<StartPathFindCargoTruckAIDelegate>(typeof(CargoTruckAI).GetMethod("StartPathFind", BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(Vector3), typeof(Vector3), typeof(bool), typeof(bool), typeof(bool)], null), null, false);

        public static bool CustomStartPathFind(ushort vehicleID, ref Vehicle vehicleData)
        {
            var m_info = vehicleData.Info;
            if ((vehicleData.m_flags & Vehicle.Flags.WaitingTarget) != 0)
            {
                return true;
            }
            if ((vehicleData.m_flags & Vehicle.Flags.GoingBack) != 0)
            {
                if (vehicleData.m_sourceBuilding != 0)
                {
                    BuildingManager instance = Singleton<BuildingManager>.instance;
                    BuildingInfo info = instance.m_buildings.m_buffer[vehicleData.m_sourceBuilding].Info;
                    Randomizer randomizer = new(vehicleID);
                    info.m_buildingAI.CalculateUnspawnPosition(vehicleData.m_sourceBuilding, ref instance.m_buildings.m_buffer[vehicleData.m_sourceBuilding], ref randomizer, m_info, out Vector3 a, out Vector3 target);
                    return StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, target, true, true, false);
                }
            }
            else if (vehicleData.m_targetBuilding != 0)
            {
                BuildingManager instance2 = Singleton<BuildingManager>.instance;
                BuildingInfo info2 = instance2.m_buildings.m_buffer[vehicleData.m_targetBuilding].Info;
                Randomizer randomizer2 = new(vehicleID);
                info2.m_buildingAI.CalculateUnspawnPosition(vehicleData.m_targetBuilding, ref instance2.m_buildings.m_buffer[vehicleData.m_targetBuilding], ref randomizer2, m_info, out Vector3 b, out Vector3 target2);
                return StartPathFindCargoTruckAI(Singleton<CargoTruckAI>.instance, vehicleID, ref vehicleData, vehicleData.m_targetPos3, target2, true, true, false);
            }
            return false;
        }
    }
}
