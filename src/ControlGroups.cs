using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// Command mode hot keys: Ctrl+1..0 — remember the selection as a group, 1..0 — select it
    /// (Shift — add, twice quickly — move the camera to it), Ctrl+C — quick buy of the selected unit's type.
    /// </summary>
    internal partial class FieldCommandController
    {
        private readonly Dictionary<int, List<Unit>> groups = new Dictionary<int, List<Unit>>();
        private int lastGroupKey = -1;
        private float lastGroupTime;

        private static readonly KeyCode[] GroupKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
        };

        /// <summary>Group number as shown to the player (1..9, 0).</summary>
        private static string GroupName(int i) => i == 9 ? "0" : (i + 1).ToString();

        private static bool Ctrl => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        private static bool Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        private List<Unit> GroupUnits(int i)
        {
            if (!groups.TryGetValue(i, out var g)) return new List<Unit>();
            g.RemoveAll(u => !Selectable(u) || !purchased.Contains(u));
            return g;
        }

        private void TickCommandHotkeys()
        {
            if (!commandMode || mode != PlaceMode.None || areaTargeting || GUIUtility.keyboardControl != 0) return;
            if (!Plugin.ControlGroups.Value) return;

            for (int i = 0; i < GroupKeys.Length; i++)
            {
                if (!Input.GetKeyDown(GroupKeys[i]) && !Input.GetKeyDown(KeyCode.Keypad1 + (i == 9 ? -1 : i))) continue;
                if (Ctrl)
                {
                    var sel = OrderSelection().Where(u => Selectable(u) && purchased.Contains(u)).ToList();
                    if (sel.Count == 0)
                    {
                        groups.Remove(i);
                        Toast(L.T($"Group {GroupName(i)} cleared", $"Группа {GroupName(i)} очищена"), 2f);
                    }
                    else
                    {
                        // a unit belongs to one group at a time
                        foreach (var kv in groups) if (kv.Key != i) kv.Value.RemoveAll(sel.Contains);
                        groups[i] = sel;
                        Toast(L.T($"Group {GroupName(i)}: {sel.Count} units (press {GroupName(i)} to select)", $"Группа {GroupName(i)}: {sel.Count} юн. (нажми {GroupName(i)}, чтобы выделить)"), 2.5f);
                    }
                    return;
                }
                SelectGroup(i, Shift);
                return;
            }

            if (Ctrl && Input.GetKeyDown(KeyCode.C)) QuickBuy();
        }

        private void SelectGroup(int i, bool add)
        {
            var g = GroupUnits(i);
            if (g.Count == 0)
            {
                Toast(L.T($"Group {GroupName(i)} is empty — select units and press Ctrl+{GroupName(i)}", $"Группа {GroupName(i)} пуста — выдели юнитов и нажми Ctrl+{GroupName(i)}"), 2.5f);
                return;
            }
            bool twice = lastGroupKey == i && Time.unscaledTime - lastGroupTime < 0.4f;
            lastGroupKey = i;
            lastGroupTime = Time.unscaledTime;
            var list = add ? cmdSel.Union(g).ToList() : g.ToList();
            SetSelection(list);
            if (twice) FocusCameraOn(g);
            else Toast(L.T($"Group {GroupName(i)}: {g.Count}", $"Группа {GroupName(i)}: {g.Count}"), 1.2f);
        }

        /// <summary>Replaces the selection in the 3D view and on the map.</summary>
        private void SetSelection(List<Unit> list)
        {
            cmdSel.Clear();
            cmdSel.AddRange(list.Where(Selectable));
            try
            {
                var map = SceneSingleton<DynamicMap>.i;
                if (map != null && DynamicMap.mapMaximized)
                {
                    map.DeselectAllIcons();
                    foreach (var u in cmdSel) map.SelectIcon(u);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[Groups] map selection: " + e.Message);
            }
        }

        private static readonly System.Reflection.MethodInfo jumpCamera = AccessTools.Method(typeof(DynamicMap), "JumpCameraTo");

        private void FocusCameraOn(List<Unit> g)
        {
            try
            {
                if (GameManager.GetLocalAircraft(out _)) return; // flying: the camera is yours
                Vector3 c = Vector3.zero;
                foreach (var u in g) c += u.transform.position;
                c /= g.Count;
                var map = SceneSingleton<DynamicMap>.i;
                if (map == null || jumpCamera == null) return;
                TrySwitchToFreeCamera();
                jumpCamera.Invoke(map, new object[] { c.ToGlobalPosition() });
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[Groups] camera: " + e.Message);
            }
        }

        /// <summary>Ctrl+C: buy one more of the selected unit's type and place it at once (camera, or the map when it is open).</summary>
        private void QuickBuy()
        {
            var sel = OrderSelection().Where(u => u != null && u.definition != null).ToList();
            if (sel.Count == 0)
            {
                Toast(L.T("Quick buy: select one of your units first", "Быстрая покупка: сначала выдели своего юнита"), 2.5f);
                return;
            }
            var def = sel[0].definition;
            if (sel.Any(u => u.definition != def))
            {
                Toast(L.T("Quick buy: select units of one type", "Быстрая покупка: выдели юнитов одного типа"), 2.5f);
                return;
            }
            if (catalog == null || catalog.Count == 0) RebuildCatalog();
            var entry = catalog?.FirstOrDefault(e => e.Definition == def && !e.Playable)
                        ?? catalog?.FirstOrDefault(e => e.Definition != null && e.Definition.unitName == def.unitName && !e.Playable);
            if (entry == null)
            {
                Toast(L.T($"Quick buy: {def.unitName} is not on sale", $"Быстрая покупка: {def.unitName} не продаётся"), 3f);
                return;
            }
            if (!entry.AllowedForMe)
            {
                Toast(L.T($"Quick buy: {entry.Name} belongs to another faction", $"Быстрая покупка: {entry.Name} — чужая техника"), 3f);
                return;
            }
            if (GameApi.GetFunds() < entry.Price)
            {
                Toast(L.T($"Quick buy: not enough money for {entry.Name} ({GameApi.Money(entry.Price)})", $"Быстрая покупка: не хватает денег на {entry.Name} ({GameApi.Money(entry.Price)})"), 3f);
                return;
            }
            buyForFaction = false;
            BeginPlacement(entry, DynamicMap.mapMaximized ? PlaceMode.Map : PlaceMode.Camera);
            Toast(L.T($"Quick buy: {entry.Name} — {GameApi.Money(entry.Price)}. LMB — place, {Plugin.HoldFireKey.Value} — fire mode, {Plugin.CancelKey.Value} — cancel",
                      $"Быстрая покупка: {entry.Name} — {GameApi.Money(entry.Price)}. ЛКМ — поставить, {Plugin.HoldFireKey.Value} — режим огня, {Plugin.CancelKey.Value} — отмена"), 5f);
        }

        /// <summary>Group number of a unit, or -1.</summary>
        private int GroupOf(Unit u)
        {
            foreach (var kv in groups) if (kv.Value.Contains(u)) return kv.Key;
            return -1;
        }

        /// <summary>Clickable group chips for the top bar.</summary>
        private void DrawGroupChips()
        {
            bool anyGroup = false;
            for (int i = 0; i < 10; i++)
            {
                var g = GroupUnits(i);
                if (g.Count == 0) continue;
                if (!anyGroup)
                {
                    GUILayout.Space(10);
                    anyGroup = true;
                }
                bool on = g.All(cmdSel.Contains);
                if (GUILayout.Button($"{GroupName(i)}·{g.Count}", on ? Theme.SegOn : Theme.Seg, GUILayout.Width(52))) SelectGroup(i, Shift);
            }
        }

        private void ResetGroups()
        {
            groups.Clear();
            lastGroupKey = -1;
        }
    }
}
