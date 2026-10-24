using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NuclearOption.SavedMission;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// Loadout picker for KAR/BOTE units: one choice per hardpoint set, respecting
    /// the game's own availability rules (faction restrictions, nukes, precluding sets)
    /// and KAR's module sockets (a module only fits if its socket exists on the vehicle
    /// or on another chosen module).
    /// </summary>
    internal class LoadoutDraft
    {
        public readonly AircraftDefinition Definition;
        public readonly HardpointSet[] Sets;
        public readonly List<WeaponMount>[] Options; // index 0 is always "empty"
        public readonly int[] Selected;
        private readonly HashSet<string> vehicleSockets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly bool isKar;

        public LoadoutDraft(AircraftDefinition def)
        {
            Definition = def;
            isKar = (def.jsonKey ?? "").StartsWith("kar_", StringComparison.OrdinalIgnoreCase);
            WeaponManager wm = null;
            var ac = def.unitPrefab.GetComponent<Aircraft>();
            if (ac != null) wm = ac.weaponManager;
            if (wm == null) wm = def.unitPrefab.GetComponentInChildren<WeaponManager>(true);
            Sets = wm?.hardpointSets ?? new HardpointSet[0];
            Options = new List<WeaponMount>[Sets.Length];
            Selected = new int[Sets.Length];

            foreach (var t in def.unitPrefab.GetComponentsInChildren<Transform>(true))
            {
                vehicleSockets.Add(t.name);
            }

            GameApi.TryGetLocal(out var player, out var hq);
            Airbase airbase = null;
            try
            {
                airbase = hq?.GetNearestAirbase(def.unitPrefab.transform.position);
            }
            catch
            {
            }
            var tmp = new List<WeaponMount>();
            for (int i = 0; i < Sets.Length; i++)
            {
                var list = new List<WeaponMount> { null };
                try
                {
                    WeaponChecker.GetAvailableWeaponsNonAlloc(player, Sets[i], airbase, hq, false, tmp);
                    list.AddRange(tmp.Where(m => m != null).Distinct());
                }
                catch
                {
                    list.AddRange(Sets[i].weaponOptions.Where(m => m != null && WeaponChecker.MountAllowedHQ(m, hq)).Distinct());
                }
                Options[i] = list;
            }
            // sensible default: first weapon of every set that fits
            for (int i = 0; i < Sets.Length; i++)
            {
                for (int k = 1; k < Options[i].Count; k++)
                {
                    if (IsAvailable(i, k))
                    {
                        Selected[i] = k;
                        break;
                    }
                }
            }
            Revalidate();
        }

        public WeaponMount Current(int set) => Options[set][Selected[set]];

        public static string MountName(WeaponMount m)
        {
            if (m == null) return "— пусто —";
            if (!string.IsNullOrEmpty(m.mountName)) return m.mountName;
            if (m.info != null && !string.IsNullOrEmpty(m.info.weaponName)) return m.info.weaponName;
            return m.name;
        }

        public string SetName(int i)
        {
            var n = Sets[i].name;
            return string.IsNullOrEmpty(n) ? "Слот " + (i + 1) : n;
        }

        public float MountCost(int set, WeaponMount m)
        {
            if (m == null) return 0f;
            float per = m.emptyCost + (m.info != null ? m.info.costPerRound * m.ammo : 0f);
            return per * Mathf.Max(Sets[set].hardpoints.Count, 1);
        }

        public float TotalCost()
        {
            float c = 0f;
            for (int i = 0; i < Sets.Length; i++) c += MountCost(i, Current(i));
            return c;
        }

        // ---- rules ----

        private bool BlockedByPrecluding(int set)
        {
            var pre = Sets[set].precludingHardpointSets;
            if (pre == null) return false;
            foreach (byte j in pre)
            {
                if (j < Selected.Length && j != set && Current(j) != null) return true;
            }
            return false;
        }

        private static FieldInfo plugSocketField, plugRequiredField;

        private bool SocketsSatisfied(int set, WeaponMount m)
        {
            if (m == null || m.prefab == null) return true;
            Component[] plugs;
            try
            {
                plugs = m.prefab.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null && c.GetType().Name == "WeaponPlug").ToArray();
            }
            catch
            {
                return true;
            }
            if (plugs.Length == 0) return true;
            var have = new HashSet<string>(vehicleSockets, StringComparer.OrdinalIgnoreCase);
            for (int j = 0; j < Sets.Length; j++)
            {
                if (j == set) continue;
                var other = Current(j);
                if (other?.prefab == null) continue;
                foreach (var t in other.prefab.GetComponentsInChildren<Transform>(true)) have.Add(t.name);
            }
            foreach (var plug in plugs)
            {
                var t = plug.GetType();
                plugSocketField = plugSocketField ?? t.GetField("TargetSocketName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                plugRequiredField = plugRequiredField ?? t.GetField("required", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                string socket = plugSocketField?.GetValue(plug) as string;
                bool required = plugRequiredField == null || (bool)plugRequiredField.GetValue(plug);
                if (!required || string.IsNullOrWhiteSpace(socket)) continue;
                if (!have.Any(n => n.StartsWith(socket, StringComparison.OrdinalIgnoreCase))) return false;
            }
            return true;
        }

        public bool IsAvailable(int set, int option)
        {
            var m = Options[set][option];
            if (m == null) return true;
            if (BlockedByPrecluding(set)) return false;
            return SocketsSatisfied(set, m);
        }

        public void Step(int set, int dir)
        {
            int n = Options[set].Count;
            if (n <= 1) return;
            int k = Selected[set];
            for (int tries = 0; tries < n; tries++)
            {
                k = (k + dir + n) % n;
                if (IsAvailable(set, k)) break;
            }
            bool changed = k != Selected[set];
            Selected[set] = k;
            if (changed && isKar && set == 0)
            {
                // KAR: changing the main module resets the add-on modules (same as KAR's own menu)
                for (int i = 1; i < Selected.Length; i++) Selected[i] = 0;
            }
            Revalidate();
        }

        private void Revalidate()
        {
            for (int pass = 0; pass < 3; pass++)
            {
                bool any = false;
                for (int i = 0; i < Sets.Length; i++)
                {
                    if (Selected[i] != 0 && !IsAvailable(i, Selected[i]))
                    {
                        Selected[i] = 0;
                        any = true;
                    }
                }
                if (!any) break;
            }
        }

        public Loadout Build()
        {
            var l = new Loadout();
            for (int i = 0; i < Sets.Length; i++) l.weapons.Add(Current(i));
            return l;
        }
    }
}
