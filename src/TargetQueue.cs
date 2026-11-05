using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// Ctrl+RMB on enemies (3D view or map): a target list. Each selected unit fires one salvo at the first target,
    /// then moves on to the next one, and so on. Dead targets are skipped; when the list ends the unit goes back to
    /// its fire mode (free / on order).
    /// </summary>
    internal partial class FieldCommandController
    {
        private sealed class TargetList
        {
            public readonly List<Unit> Targets = new List<Unit>();
            public Unit Current;
            public float Baseline;      // last shot time of the unit when the current target was given
            public float FirstShot = -1f;
            public float LastShot;
            public float NoEngageSince = -1f;
            public int Done;
        }

        private readonly Dictionary<Unit, TargetList> targetLists = new Dictionary<Unit, TargetList>();

        private static bool Alive(Unit t) => t != null && !t.disabled;

        private static float LastShotTime(Unit u)
        {
            float t = 0f;
            try
            {
                foreach (var ws in u.weaponStations)
                {
                    if (ws != null && ws.LastFiredTime > t) t = ws.LastFiredTime;
                }
            }
            catch
            {
            }
            return t;
        }

        /// <summary>Adds a target to the end of the list of every unit in the selection.</summary>
        private void QueueTarget(List<Unit> sel, Unit enemy)
        {
            sel = sel.Where(u => u != null && !u.disabled).ToList();
            if (sel.Count == 0 || !Alive(enemy)) return;
            int n = 0;
            foreach (var u in sel)
            {
                if (!targetLists.TryGetValue(u, out var tl))
                {
                    tl = new TargetList();
                    targetLists[u] = tl;
                    attackOrders.Remove(u);
                    AreaFire.Cancel(u);
                }
                if (tl.Current != enemy && !tl.Targets.Contains(enemy)) tl.Targets.Add(enemy);
                if (tl.Current == null) NextTarget(u, tl);
                n = Mathf.Max(n, tl.Targets.Count + (tl.Current != null ? 1 : 0));
            }
            string tname = enemy.definition?.unitName ?? enemy.name;
            Toast(L.T($"Target #{n}: {tname} — {sel.Count} units fire one salvo at each target in turn (Ctrl+RMB — add more)",
                      $"Цель №{n}: {tname} — {sel.Count} юн. дадут по залпу по каждой цели по очереди (Ctrl+ПКМ — добавить ещё)"), 3f);
        }

        private void NextTarget(Unit u, TargetList tl)
        {
            tl.Current = null;
            while (tl.Targets.Count > 0)
            {
                var t = tl.Targets[0];
                tl.Targets.RemoveAt(0);
                if (!Alive(t)) continue;
                tl.Current = t;
                tl.Baseline = LastShotTime(u);
                tl.FirstShot = -1f;
                tl.NoEngageSince = -1f;
                FocusFire.Orders[u] = t;
                FocusFire.Nudge(u);
                return;
            }
            // the list is over: back to the unit's own fire mode
            FocusFire.Clear(u);
            targetLists.Remove(u);
        }

        private float nextQueueTick;

        private void TickTargetLists()
        {
            if (targetLists.Count == 0 || Time.unscaledTime < nextQueueTick) return;
            nextQueueTick = Time.unscaledTime + 0.1f;
            float now = Time.timeSinceLevelLoad;
            foreach (var kv in targetLists.ToList())
            {
                var u = kv.Key;
                var tl = kv.Value;
                if (u == null || u.disabled)
                {
                    targetLists.Remove(u);
                    continue;
                }
                if (!Alive(tl.Current))
                {
                    NextTarget(u, tl);
                    continue;
                }
                // another order (K, RMB attack, stop, cease fire) replaced ours → drop the list
                if (!FocusFire.Orders.TryGetValue(u, out var o) || o != tl.Current)
                {
                    targetLists.Remove(u);
                    continue;
                }
                float shot = LastShotTime(u);
                if (shot > tl.Baseline + 0.01f)
                {
                    if (tl.FirstShot < 0f) tl.FirstShot = shot;
                    tl.LastShot = shot;
                }
                if (tl.FirstShot >= 0f)
                {
                    // a salvo: shots one after another; it is over after a short pause (or a few seconds of fire)
                    if (now - tl.LastShot > 1.2f || now - tl.FirstShot > 5f)
                    {
                        tl.Done++;
                        NextTarget(u, tl);
                    }
                    continue;
                }
                // can't shoot at it (out of reach, not tracked, no fitting ammo) → skip after a short wait
                if (!FocusFire.CanEngageNow(u, tl.Current))
                {
                    if (tl.NoEngageSince < 0f) tl.NoEngageSince = now;
                    else if (now - tl.NoEngageSince > 3f) NextTarget(u, tl);
                }
                else
                {
                    tl.NoEngageSince = -1f;
                    FocusFire.Nudge(u);
                }
            }
        }

        private void ClearTargetList(Unit u) => targetLists.Remove(u);

        /// <summary>Ordered targets (first come first) of the given units, for the numbers on screen.</summary>
        private List<Unit> QueuedTargets(IEnumerable<Unit> units)
        {
            var res = new List<Unit>();
            foreach (var u in units)
            {
                if (u == null || !targetLists.TryGetValue(u, out var tl)) continue;
                if (Alive(tl.Current) && !res.Contains(tl.Current)) res.Add(tl.Current);
                foreach (var t in tl.Targets) if (Alive(t) && !res.Contains(t)) res.Add(t);
            }
            return res;
        }

        private static readonly Color QueueColor = new Color(1f, 0.55f, 0.2f, 1f);

        private void DrawTargetLists3D(Camera cam)
        {
            var units = cmdSel.Count > 0 ? (IEnumerable<Unit>)cmdSel : targetLists.Keys;
            var list = QueuedTargets(units);
            Vector2 prev = default;
            bool hasPrev = false;
            for (int i = 0; i < list.Count; i++)
            {
                Vector3 sp = cam.WorldToScreenPoint(list[i].transform.position);
                if (sp.z <= 0f) { hasPrev = false; continue; }
                Vector2 g = new Vector2(sp.x, Screen.height - sp.y);
                if (hasPrev) RangeOverlay.Dashed(prev, g, new Color(QueueColor.r, QueueColor.g, QueueColor.b, 0.6f), 1.5f);
                RangeOverlay.Circle(g, 16f, QueueColor, 2f, false);
                RangeOverlay.Label(g + new Vector2(14f, -24f), (i + 1).ToString(), QueueColor);
                prev = g;
                hasPrev = true;
            }
        }

        private void DrawTargetListsOnMap(DynamicMap map)
        {
            var units = cmdSel.Count > 0 ? (IEnumerable<Unit>)cmdSel : targetLists.Keys;
            var list = QueuedTargets(units);
            Vector2 prev = default;
            bool hasPrev = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (!RangeOverlay.ToGui(map, list[i].GlobalPosition(), out var g, out _)) { hasPrev = false; continue; }
                if (hasPrev) RangeOverlay.Dashed(prev, g, new Color(QueueColor.r, QueueColor.g, QueueColor.b, 0.6f), 1.5f);
                RangeOverlay.Circle(g, 11f, QueueColor, 2f, false);
                RangeOverlay.Label(g + new Vector2(10f, -20f), (i + 1).ToString(), QueueColor);
                prev = g;
                hasPrev = true;
            }
        }
    }

    /// <summary>Ctrl+RMB on the map is our target list — keep the game from also sending the selected units there.</summary>
    [HarmonyPatch(typeof(DynamicMap), "MapControls")]
    internal static class MapCtrlRmb_Patch
    {
        private static void Prefix(DynamicMap __instance, out List<MapIcon> __state)
        {
            __state = null;
            try
            {
                if (DynamicMap.mapMaximized && Input.GetMouseButtonDown(1)
                    && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    && __instance.selectedIcons != null && __instance.selectedIcons.Count > 0)
                {
                    __state = __instance.selectedIcons;
                    __instance.selectedIcons = new List<MapIcon>();
                }
            }
            catch
            {
                __state = null;
            }
        }

        private static void Postfix(DynamicMap __instance, List<MapIcon> __state)
        {
            if (__state != null) __instance.selectedIcons = __state;
        }
    }
}
