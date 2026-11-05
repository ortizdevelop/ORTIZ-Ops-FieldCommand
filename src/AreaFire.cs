using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// "Fire only on order" and "fire at area".
    /// Hold fire: the unit's turrets / fire control pick no targets of their own; they shoot only at an ordered
    /// target (focus fire, missile defence) or an ordered area.
    /// Area fire: guns are aimed with a ballistic solution at random points of the circle and fired by us;
    /// launchers fire too, and the spawned rockets / guided shells / ballistic missiles get the point as their
    /// known position, so their own guidance flies them there — no enemy needed.
    /// </summary>
    internal static class AreaFire
    {
        public sealed class Order
        {
            public GlobalPosition Center;
            public float Radius;
            public float Until; // unscaled time, 0 = no limit
            public readonly Dictionary<WeaponStation, Slot> Slots = new Dictionary<WeaponStation, Slot>();
            public FireControl Fc;
            public bool DeployRequested;
            public float FireFrom; // after the launchers are raised
            public int Shots;
        }

        public sealed class Slot
        {
            public WeaponStation Ws;
            public Turret Turret;
            public bool Gun;
            public GlobalPosition Aim;
            public float LastFired;
            public Vector3 AimDir;
            public float NextSolve;
            public bool Solvable = true;
            public float OnTargetSince = -1f;
        }

        public static readonly Dictionary<Unit, Order> Orders = new Dictionary<Unit, Order>();
        public static readonly HashSet<Unit> HoldFire = new HashSet<Unit>();

        /// <summary>The unit must not pick targets by itself.</summary>
        public static bool Blocks(Unit u) => u != null && (HoldFire.Contains(u) || Orders.ContainsKey(u));

        // ---------- reflection ----------
        private static readonly AccessTools.FieldRef<Turret, Unit> attachedRef = AccessTools.FieldRefAccess<Turret, Unit>("attachedUnit");
        private static readonly AccessTools.FieldRef<Turret, Unit> targetRef = AccessTools.FieldRefAccess<Turret, Unit>("target");
        private static readonly AccessTools.FieldRef<Turret, WeaponStation> stationRef = AccessTools.FieldRefAccess<Turret, WeaponStation>("currentWeaponStation");
        private static readonly AccessTools.FieldRef<Turret, WeaponStation[]> stationsRef = AccessTools.FieldRefAccess<Turret, WeaponStation[]>("weaponStations");
        private static readonly AccessTools.FieldRef<Turret, bool> onTargetRef = AccessTools.FieldRefAccess<Turret, bool>("onTarget");
        private static readonly AccessTools.FieldRef<Turret, bool> noAimRef = AccessTools.FieldRefAccess<Turret, bool>("firesWithoutAiming");
        private static readonly AccessTools.FieldRef<Turret, bool> disabledRef = AccessTools.FieldRefAccess<Turret, bool>("disabled");
        private static readonly MethodInfo aimTurret = AccessTools.Method(typeof(Turret), "AimTurret", new[] { typeof(Vector3) });
        private static readonly MethodInfo onTargetDisabled = AccessTools.Method(typeof(Turret), "Turret_OnTargetDisabled");
        private static readonly AccessTools.FieldRef<FireControl, bool> fcDeployedRef = AccessTools.FieldRefAccess<FireControl, bool>("deployed");
        private static readonly AccessTools.FieldRef<FireControl, List<WeaponStation>> fcStationsRef = AccessTools.FieldRefAccess<FireControl, List<WeaponStation>>("subscribedWeaponStations");
        private static readonly AccessTools.FieldRef<Gun, MissileDefinition> guidedRef = SafeRef<Gun, MissileDefinition>("guidedProjectile");
        private static readonly AccessTools.FieldRef<MissileSeeker, Missile> seekerMissileRef = AccessTools.FieldRefAccess<MissileSeeker, Missile>("missile");

        private static AccessTools.FieldRef<T, F> SafeRef<T, F>(string name)
        {
            try { return AccessTools.FieldRefAccess<T, F>(name); }
            catch { return null; }
        }

        // seekers that fly to a "known position" even without a target unit
        private static readonly string[] areaSeekers = { "InertialSeekerShell", "OpticalSeekerShell", "BallisticMissileGuidance" };

        private static readonly Dictionary<MissileDefinition, bool> seekerOk = new Dictionary<MissileDefinition, bool>();

        private static bool MissileCanArea(MissileDefinition md)
        {
            if (md == null || md.unitPrefab == null) return false;
            if (seekerOk.TryGetValue(md, out var ok)) return ok;
            ok = false;
            try
            {
                var s = md.unitPrefab.GetComponentInChildren<MissileSeeker>(true);
                ok = s != null && areaSeekers.Contains(s.GetType().Name);
            }
            catch
            {
            }
            seekerOk[md] = ok;
            return ok;
        }

        // ---------- which weapons can shell an area ----------

        private static bool SurfaceWeapon(WeaponStation ws)
        {
            var wi = ws?.WeaponInfo;
            if (wi == null || ws.Ammo <= 0 || wi.nuclear || wi.cargo || wi.troops || wi.jammer || wi.sling || ws.Weapons == null || ws.Weapons.Count == 0) return false;
            if (wi.effectiveness.antiSurface <= 0f) return false;
            return wi.effectiveness.antiAir <= wi.effectiveness.antiSurface;
        }

        /// <summary>Usable station kind: 1 = gun, 2 = launcher, 0 = no.</summary>
        private static int Kind(WeaponStation ws)
        {
            if (!SurfaceWeapon(ws)) return 0;
            var w = ws.Weapons[0];
            if (w is Gun g)
            {
                var gp = guidedRef != null ? guidedRef(g) : null;
                if (gp != null && !MissileCanArea(gp)) return 0;
                return 1;
            }
            if (w is MissileLauncher ml) return MissileCanArea(ml.missile) ? 2 : 0;
            return 0;
        }

        /// <summary>Range band of the unit's area-capable weapons.</summary>
        public static bool Reach(Unit u, out float min, out float max)
        {
            min = float.MaxValue;
            max = 0f;
            if (u?.weaponStations == null) return false;
            foreach (var ws in u.weaponStations)
            {
                if (Kind(ws) == 0) continue;
                var r = ws.WeaponInfo.targetRequirements;
                max = Mathf.Max(max, r.maxRange);
                min = Mathf.Min(min, r.minRange);
            }
            if (max <= 0f) { min = 0f; return false; }
            return true;
        }

        public static bool CanReach(Unit u, GlobalPosition p)
        {
            if (!Reach(u, out float min, out float max)) return false;
            float d = HorizontalDistance(u.GlobalPosition(), p);
            return d <= max && d >= min;
        }

        private static float HorizontalDistance(GlobalPosition a, GlobalPosition b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // ---------- orders ----------

        /// <summary>Returns false when the unit has no weapon that can shell this point.</summary>
        public static bool Give(Unit u, GlobalPosition center, float radius)
        {
            if (u == null || u.disabled) return false;
            Cancel(u);
            var o = new Order { Center = center, Radius = radius, Until = Plugin.AreaDuration.Value > 0f ? Time.unscaledTime + Plugin.AreaDuration.Value : 0f };
            var turrets = u.GetComponentsInChildren<Turret>(true);
            foreach (var ws in u.weaponStations)
            {
                int k = Kind(ws);
                if (k == 0) continue;
                var r = ws.WeaponInfo.targetRequirements;
                float d = HorizontalDistance(u.GlobalPosition(), center);
                if (d > r.maxRange || d < r.minRange) continue;
                Turret t = null;
                foreach (var tu in turrets)
                {
                    var arr = stationsRef(tu);
                    if (arr != null && arr.Contains(ws)) { t = tu; break; }
                }
                if (k == 1 && t == null) continue; // a fixed gun can't be pointed
                o.Slots[ws] = new Slot { Ws = ws, Turret = t, Gun = k == 1, Aim = Pick(center, radius), LastFired = ws.LastFiredTime };
            }
            if (o.Slots.Count == 0) return false;
            o.Fc = u.GetComponent<FireControl>();
            try
            {
                foreach (var sl in o.Slots.Values)
                {
                    string seeker = "";
                    if (!sl.Gun && sl.Ws.Weapons[0] is MissileLauncher ml && ml.missile?.unitPrefab != null) seeker = ml.missile.unitPrefab.GetComponentInChildren<MissileSeeker>(true)?.GetType().Name ?? "none";
                    Plugin.Log.LogInfo($"[AreaFire] {u.definition?.unitName}: {sl.Ws.WeaponInfo?.weaponName} — {(sl.Gun ? "gun" : "launcher " + seeker)}, turret {(sl.Turret != null ? (noAimRef(sl.Turret) ? "fires without aiming" : "aimed") : "none")}, fire control {(o.Fc != null && fcStationsRef(o.Fc) != null && fcStationsRef(o.Fc).Contains(sl.Ws))}");
                }
            }
            catch
            {
            }
            Orders[u] = o;
            FocusFire.Clear(u);
            ClearTargets(u);
            return true;
        }

        public static void Cancel(Unit u)
        {
            if (u == null || !Orders.TryGetValue(u, out var o)) return;
            Orders.Remove(u);
            try
            {
                if (o.DeployRequested && u is GroundVehicle gv && !u.disabled) gv.RpcDeployFireControl(false);
            }
            catch
            {
            }
        }

        public static void SetHold(Unit u, bool on)
        {
            if (u == null) return;
            if (on)
            {
                HoldFire.Add(u);
                if (!FocusFire.Orders.ContainsKey(u) && !Orders.ContainsKey(u)) ClearTargets(u);
            }
            else
            {
                HoldFire.Remove(u);
                FocusFire.Nudge(u);
            }
        }

        private static GlobalPosition Pick(GlobalPosition c, float r)
        {
            Vector2 d = UnityEngine.Random.insideUnitCircle * r;
            var p = new GlobalPosition(c.x + d.x, c.y, c.z + d.y);
            try
            {
                Vector3 local = p.ToLocalPosition();
                if (PathfindingAgent.RaycastTerrain(local, out var hit)) p.y = Mathf.Max(hit.point.ToGlobalPosition().y, 0f);
            }
            catch
            {
            }
            return p;
        }

        // ---------- turrets: drop targets ----------

        public static void ClearTurret(Turret t)
        {
            try
            {
                var u = attachedRef(t);
                var old = targetRef(t);
                if (u == null || old == null) return;
                if (onTargetDisabled != null) old.onDisableUnit -= (Action<Unit>)Delegate.CreateDelegate(typeof(Action<Unit>), t, onTargetDisabled);
                if (u.NetworkHQ != null && u.NetworkHQ.trackingDatabase.TryGetValue(old.persistentID, out var ti)) ti.attackers--;
                targetRef(t) = null;
                var ws = stationRef(t);
                if (ws != null) u.RpcSetStationTargets(ws.Number, new[] { PersistentID.None });
                t.enabled = false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[HoldFire] " + e.Message);
            }
        }

        public static void ClearTargets(Unit u)
        {
            try
            {
                foreach (var t in u.GetComponentsInChildren<Turret>(true)) ClearTurret(t);
            }
            catch
            {
            }
        }

        public static void Cleanup()
        {
            HoldFire.RemoveWhere(u => u == null || u.disabled);
            foreach (var u in Orders.Keys.Where(k => k == null || k.disabled).ToList()) Orders.Remove(u);
        }

        // ---------- per physics step ----------

        public static void FixedTick()
        {
            if (Orders.Count == 0) return;
            foreach (var kv in Orders.ToList())
            {
                var u = kv.Key;
                var o = kv.Value;
                try
                {
                    if (u == null || u.disabled) { Orders.Remove(u); continue; }
                    if (o.Until > 0f && Time.unscaledTime > o.Until) { Cancel(u); continue; }
                    if (!u.LocalSim) continue;
                    bool anyAmmo = false;
                    foreach (var s in o.Slots.Values)
                    {
                        if (s.Ws.Ammo > 0) anyAmmo = true;
                        Step(u, o, s);
                    }
                    if (!anyAmmo) Cancel(u);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("[AreaFire] " + e.Message);
                    Orders.Remove(u);
                }
            }
        }

        private static void Step(Unit u, Order o, Slot s)
        {
            var ws = s.Ws;
            if (ws.Ammo <= 0) return;

            // a new shot happened → next random point of the circle
            if (ws.LastFiredTime != s.LastFired)
            {
                s.LastFired = ws.LastFiredTime;
                s.Aim = Pick(o.Center, o.Radius);
                s.NextSolve = 0f;
            }

            // launchers on a fire-control unit (MLRS etc.) must be raised first
            bool fcLauncher = !s.Gun && o.Fc != null && fcStationsRef(o.Fc) != null && fcStationsRef(o.Fc).Contains(ws);
            if (fcLauncher)
            {
                if (!o.DeployRequested)
                {
                    o.DeployRequested = true;
                    if (u is GroundVehicle gv) gv.RpcDeployFireControl(true);
                    else o.Fc.DeployOrStowLaunchers(true);
                    o.FireFrom = float.MaxValue;
                }
                if (o.FireFrom == float.MaxValue)
                {
                    if (fcDeployedRef(o.Fc)) o.FireFrom = Time.timeSinceLevelLoad + 2f;
                    else return;
                }
                if (Time.timeSinceLevelLoad < o.FireFrom) return;
            }

            bool aimed = true;
            var t = s.Turret;
            if (t != null && !disabledRef(t) && !noAimRef(t))
            {
                if (t.enabled) t.enabled = false; // we steer it ourselves
                stationRef(t) = ws;
                Vector3 muzzle = ws.Weapons[0].transform.position;
                if (Time.timeSinceLevelLoad >= s.NextSolve)
                {
                    s.NextSolve = Time.timeSinceLevelLoad + 0.5f;
                    s.AimDir = GameAim(t, ws.WeaponInfo, muzzle, s.Aim);
                    s.Solvable = s.AimDir.sqrMagnitude > 1e-6f;
                }
                if (!s.Solvable) return;
                aimTurret.Invoke(t, new object[] { s.AimDir });
                if (s.Gun)
                {
                    aimed = onTargetRef(t);
                }
                else
                {
                    // launchers only need to face the right way (the game ignores their elevation error too)
                    Vector3 f = Vector3.ProjectOnPlane(t.transform.forward, Vector3.up);
                    Vector3 w = Vector3.ProjectOnPlane(s.AimDir, Vector3.up);
                    aimed = f.sqrMagnitude < 1e-4f || w.sqrMagnitude < 1e-4f || Vector3.Angle(f, w) < 3f;
                }
            }
            // like the game: only after the barrel has stayed on the aim for a moment
            if (!aimed)
            {
                s.OnTargetSince = -1f;
                return;
            }
            if (s.OnTargetSince < 0f) s.OnTargetSince = Time.timeSinceLevelLoad;
            if (t != null && !noAimRef(t) && Time.timeSinceLevelLoad - s.OnTargetSince < 0.6f) return;
            if (!ws.Ready()) return;
            ws.Fire(u, null);
            o.Shots++;
        }

        private static readonly AccessTools.FieldRef<Turret, AimSolver> solverRef = SafeRef<Turret, AimSolver>("aimSolver");
        private static readonly AccessTools.FieldRef<AimSolver, bool> artilleryRef = SafeRef<AimSolver, bool>("artillery");
        private static readonly AccessTools.FieldRef<AimSolver, float> artDragRef = SafeRef<AimSolver, float>("artilleryDragCoef");

        /// <summary>The game's own aim formula (AimSolver.GetAimVector) for a fixed point on the ground.</summary>
        private static Vector3 GameAim(Turret t, WeaponInfo wi, Vector3 muzzle, GlobalPosition aim)
        {
            GlobalPosition from = muzzle.ToGlobalPosition();
            Vector3 d = aim - from;
            float range = d.magnitude;
            if (range < 1f) return Vector3.zero;
            if (wi.missile) return d.normalized;
            float maxSpeed = wi.GetMaxSpeed();
            if (maxSpeed <= 1f) return d.normalized;
            bool artillery = false;
            float dragCoef = 1f;
            try
            {
                var solver = solverRef != null ? solverRef(t) : null;
                if (solver != null)
                {
                    if (artilleryRef != null) artillery = artilleryRef(solver);
                    if (artDragRef != null) dragCoef = artDragRef(solver);
                }
            }
            catch
            {
            }
            float k = range / Mathf.Max(wi.targetRequirements.maxRange, 1f);
            if (artillery && k > 0.2f)
            {
                float eff = Mathf.Lerp(range * (1f + 0.5f * dragCoef), range * (1f + 1.5f * dragCoef), k);
                Vector3 flat = new Vector3(d.x, 0f, d.z);
                if (Mathf.Abs(d.y) / range < 0.1f)
                {
                    float ang = 0.5f * Mathf.Asin(Mathf.Min(eff * 9.81f / (maxSpeed * maxSpeed), 1f));
                    return (flat.normalized + Vector3.up * Mathf.Tan(ang)).normalized;
                }
            }
            float time = range / (maxSpeed * 0.9f);
            Vector3 v = d + time * time * 4.905f * (wi.gravMult > 0f ? wi.gravMult : 1f) * Vector3.up;
            try
            {
                // the game corrects the simple lead with a trajectory simulation
                Kinematics.TrajectorySim(false, wi, v.normalized * (wi.muzzleVelocity > 0f ? wi.muzzleVelocity : maxSpeed), from, aim, Vector3.zero, Vector3.zero, 0.1f, out var miss, out _);
                if (miss.magnitude < range) v -= miss;
            }
            catch
            {
            }
            return v.normalized;
        }

        // ---------- missiles fired by an area order ----------

        public static readonly Dictionary<Missile, GlobalPosition> Missiles = new Dictionary<Missile, GlobalPosition>();

        public static void OnMissileSpawned(Missile m, Unit target, Unit owner)
        {
            if (m == null || target != null || owner == null || !Orders.TryGetValue(owner, out var o)) return;
            if (Missiles.Count > 400) foreach (var k in Missiles.Keys.Where(k => k == null || k.disabled).ToList()) Missiles.Remove(k);
            Missiles[m] = Pick(o.Center, o.Radius);
        }

        private static readonly Dictionary<Type, FieldInfo[]> seekerFields = new Dictionary<Type, FieldInfo[]>();

        public static void OnSeekerInitialized(MissileSeeker seeker)
        {
            try
            {
                var m = seekerMissileRef(seeker);
                if (m == null || !Missiles.TryGetValue(m, out var p)) return;
                var type = seeker.GetType();
                if (!seekerFields.TryGetValue(type, out var f))
                {
                    f = new[] { AccessTools.Field(type, "knownPos"), AccessTools.Field(type, "useDatalink"), AccessTools.Field(type, "useLaser"), AccessTools.Field(type, "aimPos") };
                    seekerFields[type] = f;
                }
                f[0]?.SetValue(seeker, p);
                f[1]?.SetValue(seeker, false);
                f[2]?.SetValue(seeker, false);
                f[3]?.SetValue(seeker, p);
                m.SetAimpoint(p, Vector3.zero);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[AreaFire] seeker: " + e.Message);
            }
        }

        /// <summary>Called from Plugin.Awake: postfix on every seeker's Initialize.</summary>
        public static void PatchSeekers(Harmony h)
        {
            var post = new HarmonyMethod(typeof(AreaFire), nameof(SeekerInitPostfix));
            foreach (var name in areaSeekers)
            {
                try
                {
                    var type = AccessTools.TypeByName(name);
                    var mi = type != null ? AccessTools.DeclaredMethod(type, "Initialize") : null;
                    if (mi != null) h.Patch(mi, postfix: post);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("[AreaFire] patch " + name + ": " + e.Message);
                }
            }
        }

        private static void SeekerInitPostfix(MissileSeeker __instance)
        {
            if (Missiles.Count > 0) OnSeekerInitialized(__instance);
        }
    }

    [HarmonyPatch]
    internal static class Spawner_SpawnMissile_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return AccessTools.GetDeclaredMethods(typeof(Spawner)).Where(m => m.Name == "SpawnMissile" && m.GetParameters().Length == 6);
        }

        private static void Postfix(Missile __result, Unit target, Unit owner)
        {
            try
            {
                if (AreaFire.Orders.Count > 0) AreaFire.OnMissileSpawned(__result, target, owner);
            }
            catch
            {
            }
        }
    }

    /// <summary>Launch angle for a gun to put a shell on a point (low trajectory), with the game's drag model.</summary>
    internal static class Ballistic
    {
        public static bool Solve(WeaponInfo wi, GlobalPosition from, GlobalPosition to, out Vector3 dir)
        {
            dir = Vector3.forward;
            float v0 = wi.muzzleVelocity > 0f ? wi.muzzleVelocity : wi.GetMaxSpeed();
            if (v0 <= 1f) return false;
            Vector3 d = to - from;
            Vector3 flat = new Vector3(d.x, 0f, d.z);
            float range = flat.magnitude;
            if (range < 1f) return false;
            Vector3 hdir = flat / range;
            float lo = -15f * Mathf.Deg2Rad, hi = 45f * Mathf.Deg2Rad;
            float fHi = HeightAt(wi, v0, hi, range) - d.y;
            if (float.IsNaN(fHi) || fHi < 0f) return false; // can't reach even at 45°
            for (int i = 0; i < 22; i++)
            {
                float mid = (lo + hi) * 0.5f;
                float f = HeightAt(wi, v0, mid, range) - d.y;
                if (float.IsNaN(f) || f < 0f) lo = mid;
                else hi = mid;
            }
            float a = hi;
            dir = (hdir * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)).normalized;
            return true;
        }

        /// <summary>Height of the shell when it has travelled 'range' horizontally (NaN if it never gets there).</summary>
        private static float HeightAt(WeaponInfo wi, float v0, float angle, float range)
        {
            float vx = v0 * Mathf.Cos(angle), vy = v0 * Mathf.Sin(angle);
            float x = 0f, y = 0f;
            const float dt = 0.05f;
            float drag = wi.dragCoef / Mathf.Max(wi.muzzleVelocity, 1f);
            float g = 9.81f * (wi.gravMult > 0f ? wi.gravMult : 1f);
            for (int i = 0; i < 6000; i++)
            {
                float sp = Mathf.Sqrt(vx * vx + vy * vy);
                float ax = -drag * sp * vx;
                float ay = -g - drag * sp * vy;
                float nx = x + vx * dt, ny = y + vy * dt;
                if (nx >= range)
                {
                    float k = (range - x) / Mathf.Max(nx - x, 1e-4f);
                    return y + (ny - y) * k;
                }
                x = nx;
                y = ny;
                vx += ax * dt;
                vy += ay * dt;
                if (vx <= 0.5f || y < -20000f) return float.NaN;
            }
            return float.NaN;
        }
    }
}
