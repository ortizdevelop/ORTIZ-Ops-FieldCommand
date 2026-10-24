using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// Focus fire: a unit's own fire control is told which target to shoot.
    /// The unit does NOT drive anywhere — it fires as soon as the target is inside
    /// the range of one of its weapons (artillery hits from far away, SAMs intercept
    /// missiles). If none of its weapons can reach or hurt the target, the unit keeps
    /// its normal behaviour (self-defence) until the target comes into range.
    /// </summary>
    internal static class FocusFire
    {
        /// <summary>attacker -> ordered target</summary>
        public static readonly Dictionary<Unit, Unit> Orders = new Dictionary<Unit, Unit>();

        /// <summary>units that automatically pick incoming enemy missiles (ballistic/nuclear first)</summary>
        public static readonly HashSet<Unit> MissileDefense = new HashSet<Unit>();

        private static readonly Dictionary<Unit, Unit> autoMissile = new Dictionary<Unit, Unit>();

        public static bool TryGet(Unit attacker, out Unit target)
        {
            target = null;
            if (attacker == null) return false;
            if (Orders.TryGetValue(attacker, out target) && Valid(attacker, target)) return true;
            if (autoMissile.TryGetValue(attacker, out target) && Valid(attacker, target)) return true;
            target = null;
            return false;
        }

        private static bool Valid(Unit attacker, Unit t)
        {
            return t != null && !t.disabled && t.NetworkHQ != null && t.NetworkHQ != attacker.NetworkHQ;
        }

        public static void Clear(Unit u)
        {
            Orders.Remove(u);
        }

        public static void Cleanup()
        {
            foreach (var k in Orders.Where(kv => kv.Key == null || kv.Key.disabled || !Valid(kv.Key, kv.Value)).Select(kv => kv.Key).ToList())
            {
                Orders.Remove(k);
            }
            MissileDefense.RemoveWhere(u => u == null || u.disabled);
            foreach (var k in autoMissile.Keys.Where(k => k == null || k.disabled || !MissileDefense.Contains(k)).ToList())
            {
                autoMissile.Remove(k);
            }
        }

        public static TrackingInfo Track(Unit attacker, Unit t)
        {
            try
            {
                return attacker.NetworkHQ?.GetTrackingData(t.persistentID);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Best station of this list that can reach and hurt the target, or null.</summary>
        public static WeaponStation BestStation(IEnumerable<WeaponStation> stations, Unit attacker, Unit target, TrackingInfo tracking, float dist, float armorOptimism)
        {
            WeaponStation best = null;
            float bestScore = 0f;
            foreach (var ws in stations)
            {
                if (ws == null || ws.WeaponInfo == null || ws.Ammo <= 0) continue;
                var req = ws.WeaponInfo.targetRequirements;
                if (dist > req.maxRange || dist < req.minRange) continue;
                float score;
                try
                {
                    score = CombatAI.AnalyzeTarget(ws, attacker, tracking, armorOptimism, dist, 1f).opportunity;
                }
                catch
                {
                    continue;
                }
                if (ws.Reloading) score *= 0.5f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = ws;
                }
            }
            return best;
        }

        /// <summary>Max reach of a unit's weapons (for UI / missile defence).</summary>
        public static float MaxRange(Unit u)
        {
            float r = 0f;
            foreach (var ws in u.weaponStations)
            {
                if (ws?.WeaponInfo != null && ws.Ammo > 0) r = Mathf.Max(r, ws.WeaponInfo.targetRequirements.maxRange);
            }
            return r;
        }

        public static bool CanEngageNow(Unit u, Unit t)
        {
            var tr = Track(u, t);
            if (tr == null) return false;
            float d = Vector3.Distance(u.transform.position, t.transform.position);
            return BestStation(u.weaponStations, u, t, tr, d, 0f) != null;
        }

        // ---------- missile defence: pick the most dangerous incoming missile ----------

        public static void UpdateMissileDefense()
        {
            if (MissileDefense.Count == 0) return;
            if (!GameApi.TryGetLocal(out _, out var hq)) return;
            var missiles = new List<Missile>();
            foreach (var u in UnitRegistry.allUnits)
            {
                if (u is Missile m && !m.disabled && m.NetworkHQ != null && m.NetworkHQ != hq)
                {
                    bool tracked = false;
                    try
                    {
                        tracked = hq.IsTargetBeingTracked(m);
                    }
                    catch
                    {
                    }
                    if (tracked) missiles.Add(m);
                }
            }
            foreach (var d in MissileDefense)
            {
                if (d == null || d.disabled) continue;
                Unit pick = null;
                int bestRank = -1;
                float bestDist = float.MaxValue;
                float reach = MaxRange(d);
                foreach (var m in missiles)
                {
                    float dist = Vector3.Distance(d.transform.position, m.transform.position);
                    if (dist > reach * 1.05f) continue;
                    var tr = Track(d, m);
                    if (tr == null || BestStation(d.weaponStations, d, m, tr, dist, 0f) == null) continue;
                    int rank = 0;
                    try
                    {
                        var info = m.GetWeaponInfo();
                        if (info != null && info.strategic) rank = 3;
                        else if (info != null && info.nuclear) rank = 2;
                    }
                    catch
                    {
                    }
                    if (m.targetID == d.persistentID) rank = Math.Max(rank, 1);
                    if (rank > bestRank || (rank == bestRank && dist < bestDist))
                    {
                        bestRank = rank;
                        bestDist = dist;
                        pick = m;
                    }
                }
                if (pick != null) autoMissile[d] = pick;
                else autoMissile.Remove(d);
            }
        }

        /// <summary>Make all turrets of a focused unit re-evaluate now (instead of waiting for their own timer).</summary>
        public static void Nudge(Unit u)
        {
            if (u == null || u.disabled) return;
            try
            {
                foreach (var t in u.GetComponentsInChildren<Turret>())
                {
                    Turret_ChooseTarget_Patch.TryForce(t, false);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[Focus] nudge: " + e.Message);
            }
        }
    }

    // ===================== turrets that pick their own targets =====================

    [HarmonyPatch(typeof(Turret), "ChooseTarget")]
    internal static class Turret_ChooseTarget_Patch
    {
        private static readonly AccessTools.FieldRef<Turret, Unit> attachedUnitRef = AccessTools.FieldRefAccess<Turret, Unit>("attachedUnit");
        private static readonly AccessTools.FieldRef<Turret, Unit> targetRef = AccessTools.FieldRefAccess<Turret, Unit>("target");
        private static readonly AccessTools.FieldRef<Turret, WeaponStation> stationRef = AccessTools.FieldRefAccess<Turret, WeaponStation>("currentWeaponStation");
        private static readonly AccessTools.FieldRef<Turret, WeaponStation[]> stationsRef = AccessTools.FieldRefAccess<Turret, WeaponStation[]>("weaponStations");
        private static readonly AccessTools.FieldRef<Turret, FiringCone[]> conesRef = AccessTools.FieldRefAccess<Turret, FiringCone[]>("firingCones");
        private static readonly AccessTools.FieldRef<Turret, List<Unit>> potentialRef = AccessTools.FieldRefAccess<Turret, List<Unit>>("potentialTargets");
        private static readonly AccessTools.FieldRef<Turret, float> lastAssessRef = AccessTools.FieldRefAccess<Turret, float>("lastTargetAssessment");
        private static readonly AccessTools.FieldRef<Turret, float> optimismRef = AccessTools.FieldRefAccess<Turret, float>("armorTierOptimism");
        private static readonly AccessTools.FieldRef<Turret, bool> disabledRef = AccessTools.FieldRefAccess<Turret, bool>("disabled");
        private static readonly AccessTools.FieldRef<Turret, bool> manualRef = AccessTools.FieldRefAccess<Turret, bool>("manual");
        private static readonly System.Reflection.MethodInfo onTargetDisabled = AccessTools.Method(typeof(Turret), "Turret_OnTargetDisabled");

        private static Action<Unit> Handler(Turret t)
        {
            return onTargetDisabled == null ? null : (Action<Unit>)Delegate.CreateDelegate(typeof(Action<Unit>), t, onTargetDisabled);
        }

        private static bool Prefix(Turret __instance, bool clearAfterSearch)
        {
            try
            {
                return !TryForce(__instance, clearAfterSearch);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[Focus] turret: " + e.Message);
                return true;
            }
        }

        /// <summary>Returns true when the ordered target was assigned (original method must be skipped).</summary>
        public static bool TryForce(Turret turret, bool clearAfterSearch)
        {
            Unit attached = attachedUnitRef(turret);
            if (attached == null || attached.disabled || disabledRef(turret) || manualRef(turret)) return false;
            if (attached is Aircraft) return false; // player-flown KAR/BOTE: leave alone
            if (!FocusFire.TryGet(attached, out var t)) return false;
            var tracking = FocusFire.Track(attached, t);
            if (tracking == null) return false; // nobody on our side sees it
            Vector3 v = t.transform.position - turret.transform.position;
            var cones = conesRef(turret);
            if (cones != null && cones.Length > 0 && !FiringConeChecker.VectorWithinFiringCones(cones, v, out _)) return false;
            var ws = FocusFire.BestStation(stationsRef(turret) ?? new WeaponStation[0], attached, t, tracking, v.magnitude, optimismRef(turret));
            if (ws == null) return false;

            Unit old = targetRef(turret);
            WeaponStation oldWs = stationRef(turret);
            lastAssessRef(turret) = Time.timeSinceLevelLoad;
            if (old != t)
            {
                var h = Handler(turret);
                var db = attached.NetworkHQ.trackingDatabase;
                if (old != null)
                {
                    if (h != null) old.onDisableUnit -= h;
                    if (db.TryGetValue(old.persistentID, out var oi)) oi.attackers--;
                }
                if (h != null) t.onDisableUnit += h;
                tracking.attackers++;
            }
            targetRef(turret) = t;
            stationRef(turret) = ws;
            if (old != t || oldWs != ws)
            {
                attached.RpcSetStationTargets(ws.Number, new[] { t.persistentID });
            }
            if (clearAfterSearch) potentialRef(turret)?.Clear();
            return true;
        }
    }

    // ===================== radar-guided turret groups (SAM sites, CIWS on ships) =====================

    [HarmonyPatch(typeof(FireControl), "DistributeTurretTargets")]
    internal static class FireControl_Distribute_Patch
    {
        private static bool Prefix(FireControl __instance)
        {
            try
            {
                var attached = Traverse.Create(__instance).Field("attachedUnit").GetValue<Unit>();
                if (attached == null || attached.disabled || !FocusFire.TryGet(attached, out var t)) return true;
                var ws = Traverse.Create(__instance).Field("weaponStation").GetValue<WeaponStation>();
                var turrets = Traverse.Create(__instance).Field("turrets").GetValue<List<Turret>>();
                var tracking = FocusFire.Track(attached, t);
                if (ws == null || turrets == null || tracking == null) return true;
                float d = Vector3.Distance(attached.transform.position, t.transform.position);
                if (FocusFire.BestStation(new[] { ws }, attached, t, tracking, d, 0f) == null) return true;
                int n = 0;
                foreach (var tu in turrets)
                {
                    if (tu != null && tu.HasAmmo())
                    {
                        tu.SetTargetFromController(t);
                        n++;
                    }
                }
                return n == 0;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[Focus] fire control: " + e.Message);
                return true;
            }
        }
    }

    // ===================== salvo launchers (artillery, rocket and missile batteries) =====================

    [HarmonyPatch(typeof(FireControl), "HQTargetAssessment")]
    internal static class FireControl_Salvo_Patch
    {
        private static Type fcTargetType;

        private static bool Prefix(FireControl __instance)
        {
            try
            {
                var tr = Traverse.Create(__instance);
                var attached = tr.Field("attachedUnit").GetValue<Unit>();
                if (attached == null || attached.disabled || attached.NetworkHQ == null || !FocusFire.TryGet(attached, out var t)) return true;
                var stations = tr.Field("subscribedWeaponStations").GetValue<List<WeaponStation>>();
                if (stations == null || stations.Count == 0) return true;
                var tracking = FocusFire.Track(attached, t);
                if (tracking == null) return true;
                float d = Vector3.Distance(attached.transform.position, t.transform.position);
                var ws = stations[0];
                if (FocusFire.BestStation(new[] { ws }, attached, t, tracking, d, 0f) == null) return true;

                // don't waste a second salvo on a ground target that already has our missiles inbound
                var inbound = tr.Field("currentMissiles").GetValue<List<Missile>>();
                if (!(t is Missile) && inbound != null && inbound.Any(m => m != null && m.targetID == t.persistentID)) return false;
                if (tr.Field("planningSalvo").GetValue<bool>()) return false;

                fcTargetType = fcTargetType ?? AccessTools.Inner(typeof(FireControl), "FireControlTarget");
                var ot = CombatAI.AnalyzeTarget(ws, attached, tracking, 0f, d, 1f);
                var target = Activator.CreateInstance(fcTargetType, ot, tracking, ws);
                var salvo = tr.Field("salvoTargets").GetValue() as IList;
                salvo.Clear();
                salvo.Add(target);
                tr.Method("PlanSalvo").GetValue();
                return false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[Focus] salvo: " + e.Message);
                return true;
            }
        }
    }
}
