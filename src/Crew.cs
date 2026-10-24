using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// Getting in / out of KAR and BOTE units.
    ///
    /// Moving the player's "seat" between live units by hand (v0.3.0) did not work,
    /// so this uses only paths the game itself uses and that are proven to work:
    ///  * get out  = the game's own "end sortie" (ReturnToInventory), then an identical
    ///               empty copy is put back on the same spot;
    ///  * get in   = the empty unit is removed and the same unit is spawned at the same
    ///               spot with the player inside (exactly like buying it).
    /// Position, heading, modules, fuel and livery are kept. Damage and spent ammo are reset.
    /// </summary>
    internal class Crew : MonoBehaviour
    {
        public static Crew I;

        /// <summary>Unique names of our units that have no crew (AI patch keeps them parked).</summary>
        public static readonly HashSet<string> Unmanned = new HashSet<string>();

        public List<Unit> Purchased;
        public Action<string> Toast;
        public bool Busy { get; private set; }

        private static int counter;

        private struct Snapshot
        {
            public AircraftDefinition Def;
            public GlobalPosition Pos;
            public Quaternion Rot;
            public Loadout Loadout;
            public float Fuel;
            public LiveryKey Livery;
            public FactionHQ Hq;
        }

        private void Awake()
        {
            I = this;
        }

        private static void Log(string s) => Plugin.Log.LogInfo("[Crew] " + s);

        public static bool IsPlayable(Unit u)
        {
            return u is Aircraft a && a.definition is AircraftDefinition ad && Classifier.DetectPlayable(ad) != Classifier.PlayableKind.None;
        }

        public static Aircraft MyVehicle()
        {
            if (!GameApi.TryGetLocal(out var p, out _)) return null;
            var a = p.Aircraft;
            return a != null && !a.disabled ? a : null;
        }

        private static Snapshot Take(Aircraft a)
        {
            var s = new Snapshot
            {
                Def = (AircraftDefinition)a.definition,
                Pos = a.transform.GlobalPosition(),
                Rot = a.transform.rotation,
                Hq = a.NetworkHQ,
                Livery = default(LiveryKey)
            };
            try
            {
                s.Livery = a.NetworkLiveryKey;
            }
            catch
            {
            }
            var l = new Loadout();
            if (a.loadout?.weapons != null) l.weapons.AddRange(a.loadout.weapons);
            s.Loadout = l;
            float f = 1f;
            try
            {
                f = a.GetFuelLevel();
            }
            catch
            {
            }
            s.Fuel = f > 0.001f ? Mathf.Clamp01(f) : 1f; // KAR units often have no tanks at all
            return s;
        }

        private static string NewName(AircraftDefinition def)
        {
            string n;
            do
            {
                n = def.unitPrefab.name + "_FCc" + (++counter);
            }
            while (UnitRegistry.customIDLookup.ContainsKey(n));
            return n;
        }

        private Aircraft SpawnFrom(Snapshot s, Player player)
        {
            var spawner = NetworkSceneSingleton<Spawner>.i;
            string name = NewName(s.Def);
            if (player == null) Unmanned.Add(name);
            Vector3 lift = Vector3.up * 0.3f; // avoid spawning a hair inside the ground
            var a = spawner.SpawnAircraft(player, s.Def.unitPrefab, s.Loadout, s.Fuel, s.Livery, s.Pos + lift, s.Rot, Vector3.zero, null, s.Hq, name, 1f, 0.5f);
            if (a != null)
            {
                try
                {
                    UnitRegistry.RegisterCustomID(name, a);
                }
                catch
                {
                }
                Purchased?.Add(a);
            }
            return a;
        }

        // ---------------- public actions ----------------

        public bool CanLeave(out string why)
        {
            why = null;
            var a = MyVehicle();
            if (a == null) { why = "Ты сейчас ни в чём не сидишь"; return false; }
            if (!IsPlayable(a)) { why = "Выйти можно только из техники KAR/BOTE"; return false; }
            if (a.speed > 3f) { why = "Сначала остановись"; return false; }
            if (Busy) { why = "Подожди, идёт пересадка"; return false; }
            return true;
        }

        public void Leave()
        {
            if (!CanLeave(out var why)) { Toast?.Invoke(why); return; }
            StartCoroutine(LeaveRoutine(null));
        }

        public void Enter(Aircraft target)
        {
            if (Busy) { Toast?.Invoke("Подожди, идёт пересадка"); return; }
            if (target == null || target.disabled) { Toast?.Invoke("Техника уничтожена"); return; }
            if (target.Player != null) { Toast?.Invoke("В ней уже кто-то есть"); return; }
            var mine = MyVehicle();
            if (mine == target) return;
            if (mine != null)
            {
                if (!IsPlayable(mine)) { Toast?.Invoke("Сначала выйди из текущей техники обычным способом"); return; }
                if (mine.speed > 3f) { Toast?.Invoke("Сначала остановись"); return; }
                StartCoroutine(LeaveRoutine(target)); // get out of the current one, then into the target
                return;
            }
            StartCoroutine(EnterRoutine(target));
        }

        private static void DetachPlayer(Aircraft a)
        {
            if (GameApi.TryGetLocal(out var player, out _))
            {
                player.RemoveAircraft(a);
            }
            Log("leave: player detached");
            try
            {
                SceneSingleton<CombatHUD>.i?.RemoveAircraft();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[Crew] HUD remove: " + e.Message);
            }
            try
            {
                var status = AccessTools.Field(typeof(Aircraft), "statusDisplay")?.GetValue(a) as Component;
                if (status != null) Destroy(status.gameObject);
            }
            catch
            {
            }
            try
            {
                var hurt = SceneSingleton<GameplayUI>.i?.hurt;
                if (hurt != null)
                {
                    hurt.color = new Color(0f, 0f, 0f, 0f);
                    hurt.gameObject.SetActive(false);
                }
            }
            catch
            {
            }
            // 2) put the free camera behind and above the vehicle, looking at it
            try
            {
                var cam = SceneSingleton<CameraStateManager>.i;
                if (cam != null)
                {
                    Vector3 p = a.transform.position;
                    Vector3 back = a.transform.forward;
                    back.y = 0f;
                    if (back.sqrMagnitude < 0.01f) back = Vector3.forward;
                    back.Normalize();
                    float dist = Mathf.Max(a.definition.length, 6f) * 2.5f;
                    Vector3 eye = p - back * dist + Vector3.up * dist * 0.5f;
                    cam.SetCameraPosition(eye.ToGlobalPosition(), Quaternion.LookRotation(p - eye));
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[Crew] camera reset: " + e.Message);
            }
            Log("leave: camera moved to free view");
        }

        private static IEnumerator FixCameraAfterEnter(Aircraft mine)
        {
            yield return new WaitForSecondsRealtime(1f);
            try
            {
                var cam = SceneSingleton<CameraStateManager>.i;
                if (mine != null && cam != null && cam.followingUnit != mine)
                {
                    Log("enter: camera was not on the vehicle — attaching");
                    cam.SetFollowingUnit(mine);
                }
                var hurt = SceneSingleton<GameplayUI>.i?.hurt;
                if (hurt != null) hurt.gameObject.SetActive(false);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[Crew] camera attach: " + e.Message);
            }
        }

        // ---------------- routines ----------------

        private static IEnumerator RemoveLater(Aircraft a, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (a != null) Destroy(a.gameObject);
        }

        private IEnumerator LeaveRoutine(Aircraft thenEnter)
        {
            Busy = true;
            Aircraft a = MyVehicle();
            Snapshot s = default;
            bool ok = false;
            try
            {
                s = Take(a);
                // same as the game's end-of-sortie (Aircraft.ReturnToInventory) but without
                // its refunds: no weapon money back and no free airframe in the inventory
                // 1) detach the player first, exactly like the game does when the crew bails out
                //    (otherwise the camera keeps following the removed unit -> white screen)
                DetachPlayer(a);
                Log($"leave {a.name}: Returned + disabled");
                a.NetworkunitState = Unit.UnitState.Returned;
                a.Networkdisabled = true;
                StartCoroutine(RemoveLater(a, 2f));
                ok = true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[Crew] leave failed at disable: " + e);
                Toast?.Invoke("Выйти не удалось: " + (e.InnerException ?? e).Message);
            }
            if (!ok)
            {
                Busy = false;
                yield break;
            }

            // the game removes the old object after 2 s — put the copy only when the spot is free
            float t = 0f;
            while (a != null && t < 4f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;

            Aircraft copy = null;
            try
            {
                Log("leave: spawning empty copy");
                copy = SpawnFrom(s, null);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[Crew] leave failed at copy spawn: " + e);
            }
            if (copy == null)
            {
                Toast?.Invoke("Ты вышел, но технику вернуть на место не удалось (см. LogOutput.log)");
                Busy = false;
                yield break;
            }

            if (thenEnter == null)
            {
                Toast?.Invoke("Ты вышел. Техника стоит на месте — «Сесть» в F6 → «Мои юниты»");
                Busy = false;
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.5f);
            Busy = false;
            yield return EnterRoutine(thenEnter);
        }

        private IEnumerator EnterRoutine(Aircraft target)
        {
            Busy = true;
            if (!GameApi.TryGetLocal(out var player, out _))
            {
                Busy = false;
                yield break;
            }
            Snapshot s = default;
            bool ok = false;
            try
            {
                s = Take(target);
                Log($"enter {target.name}: removing empty unit");
                Unmanned.Remove(target.UniqueName ?? "");
                Purchased?.Remove(target);
                target.NetworkunitState = Unit.UnitState.Returned;
                target.Networkdisabled = true; // lets map icons / registries clean up like a normal removal
                ok = true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[Crew] enter failed at remove: " + e);
                Toast?.Invoke("Сесть не удалось: " + (e.InnerException ?? e).Message);
            }
            if (!ok)
            {
                Busy = false;
                yield break;
            }
            yield return null;
            if (target != null) Destroy(target.gameObject);
            yield return null;
            yield return null;

            Aircraft mine = null;
            try
            {
                Log("enter: spawning with player");
                mine = SpawnFrom(s, player);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[Crew] enter failed at spawn: " + e);
            }
            if (mine == null)
            {
                // put it back empty so nothing is lost
                try
                {
                    SpawnFrom(s, null);
                }
                catch
                {
                }
                Toast?.Invoke("Сесть не удалось — техника возвращена на место (см. LogOutput.log)");
            }
            else
            {
                Toast?.Invoke("Ты в " + (s.Def.unitName ?? s.Def.name));
                StartCoroutine(FixCameraAfterEnter(mine));
            }
            Busy = false;
        }
    }

    /// <summary>Empty KAR/BOTE units bought with Field Command just stand and wait (the game AI is built for planes).</summary>
    [HarmonyPatch(typeof(Pilot), "SetStartingAiState")]
    internal static class Pilot_SetStartingAiState_Patch
    {
        private static bool Prefix(Pilot __instance)
        {
            try
            {
                var a = __instance.aircraft;
                if (a == null || string.IsNullOrEmpty(a.UniqueName) || !Crew.Unmanned.Contains(a.UniqueName)) return true;
                __instance.SwitchState(__instance.parkedState);
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}
