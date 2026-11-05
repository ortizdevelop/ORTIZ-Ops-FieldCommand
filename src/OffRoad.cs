using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// Player orders to your own purchased ground units are driven straight across country instead of
    /// following the road network (hold Alt while ordering to use the roads).
    /// </summary>
    internal static class OffRoad
    {
        /// <summary>unit → destination of the last player order that should be driven straight</summary>
        public static readonly Dictionary<Unit, GlobalPosition> Direct = new Dictionary<Unit, GlobalPosition>();

        public static bool UseRoadsNow => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
    }

    [HarmonyPatch(typeof(GroundVehicle), "UnitCommand_ProcessSetDestination")]
    internal static class GroundVehicle_ProcessSetDestination_Patch
    {
        private static void Prefix(GroundVehicle __instance, ref UnitCommand.Command command)
        {
            try
            {
                if (!command.FromPlayer || !Plugin.DriveOffRoad.Value || OffRoad.UseRoadsNow || !OwnedMarks.IsMine(__instance))
                {
                    OffRoad.Direct.Remove(__instance);
                    return;
                }
                OffRoad.Direct[__instance] = command.position;
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(PathfindingAgent), "Pathfind")]
    internal static class PathfindingAgent_Pathfind_Patch
    {
        private static readonly AccessTools.FieldRef<PathfindingAgent, Unit> unitRef = AccessTools.FieldRefAccess<PathfindingAgent, Unit>("unit");
        private static readonly AccessTools.FieldRef<PathfindingAgent, List<GlobalPosition>> waypointsRef = AccessTools.FieldRefAccess<PathfindingAgent, List<GlobalPosition>>("waypoints");
        private static readonly AccessTools.FieldRef<PathfindingAgent, GlobalPosition> targetRef = AccessTools.FieldRefAccess<PathfindingAgent, GlobalPosition>("targetPos");
        private static readonly AccessTools.FieldRef<PathfindingAgent, Transform> movingRef = AccessTools.FieldRefAccess<PathfindingAgent, Transform>("movingTarget");
        private static readonly System.Reflection.FieldInfo nodesField = AccessTools.Field(typeof(PathfindingAgent), "nodes");

        private static bool Prefix(PathfindingAgent __instance, GlobalPosition targetPos)
        {
            try
            {
                var u = unitRef(__instance);
                if (u == null || !(u is GroundVehicle) || !OffRoad.Direct.TryGetValue(u, out var dest)) return true;
                float dx = dest.x - targetPos.x, dz = dest.z - targetPos.z;
                if (dx * dx + dz * dz > 80f * 80f) return true; // a different destination (AI, depot…) — normal pathing
                var cur = targetRef(__instance);
                float cx = cur.x - targetPos.x, cz = cur.z - targetPos.z;
                var wps = waypointsRef(__instance);
                if (cx * cx + cz * cz < 100f && wps != null && wps.Count > 0) return false; // same target, already set
                movingRef(__instance) = null;
                (nodesField?.GetValue(__instance) as System.Collections.IList)?.Clear();
                wps?.Clear();
                var t = targetPos;
                if (PathfindingAgent.RaycastTerrain(t, out var hit)) t = hit.point.ToGlobalPosition();
                targetRef(__instance) = t;
                wps?.Add(t); // straight across country
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}
