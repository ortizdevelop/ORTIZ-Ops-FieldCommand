using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// Command mode: shells and missiles in flight, drawn over the 3D view and the map —
    /// yours in your colour, allies faint, enemies red. Small-arms fire is left out.
    /// </summary>
    internal static class Projectiles
    {
        private static readonly List<BulletSim> sims = new List<BulletSim>();

        private static readonly AccessTools.FieldRef<BulletSim, List<BulletSim.Bullet>> bulletsRef = AccessTools.FieldRefAccess<BulletSim, List<BulletSim.Bullet>>("bullets");
        private static readonly AccessTools.FieldRef<BulletSim, Unit> ownerRef = AccessTools.FieldRefAccess<BulletSim, Unit>("owner");
        private static readonly AccessTools.FieldRef<BulletSim, WeaponInfo> infoRef = AccessTools.FieldRefAccess<BulletSim, WeaponInfo>("weaponInfo");

        public static readonly Color Enemy = new Color(1f, 0.3f, 0.25f, 1f);
        private static readonly Color Ally = new Color(0.85f, 0.92f, 1f, 0.55f);

        public static void Register(BulletSim s)
        {
            if (s != null) sims.Add(s);
        }

        public static void Reset() => sims.Clear();

        /// <summary>Shells worth showing: explosive rounds and slow-firing guns (cannons, artillery), not machine guns.</summary>
        private static bool Big(WeaponInfo wi, BulletSim.Bullet b)
        {
            if (wi == null) return false;
            return wi.fireInterval >= 0.15f || (wi.blastDamage > 0f && wi.fireInterval >= 0.06f);
        }

        private delegate bool ToScreen(GlobalPosition p, out Vector2 gui);

        private static Color ColorFor(Unit owner, FactionHQ hq, HashSet<Unit> mine, out bool show)
        {
            show = true;
            if (owner != null && mine.Contains(owner)) return OwnedMarks.Normal;
            var oh = owner != null ? owner.NetworkHQ : null;
            if (oh == null) { show = false; return Ally; }
            return oh == hq ? Ally : Enemy;
        }

        /// <summary>Draws all projectiles. trailSeconds — how long a tail to show (screen space follows the projection).</summary>
        private static void DrawAll(ToScreen toScreen, HashSet<Unit> mine, float trailSeconds, float dot)
        {
            if (!GameApi.TryGetLocal(out _, out var hq) || hq == null) return;
            int budget = 700;
            sims.RemoveAll(s => s == null);
            foreach (var s in sims)
            {
                List<BulletSim.Bullet> list;
                WeaponInfo wi;
                Unit owner;
                try
                {
                    list = bulletsRef(s);
                    wi = infoRef(s);
                    owner = ownerRef(s);
                }
                catch
                {
                    continue;
                }
                if (list == null || list.Count == 0) continue;
                var col = ColorFor(owner, hq, mine, out bool show);
                if (!show) continue;
                for (int i = 0; i < list.Count && budget > 0; i++)
                {
                    var b = list[i];
                    if (b == null || !b.active || !Big(wi, b)) continue;
                    if (!toScreen(b.position, out var g)) continue;
                    budget--;
                    if (toScreen(b.position - b.velocity * trailSeconds, out var tail))
                        RangeOverlay.Line(tail, g, new Color(col.r, col.g, col.b, col.a * 0.45f), 1.5f);
                    RangeOverlay.Disc(g, dot, col);
                }
            }
            // missiles (and guided shells / rockets the game spawns as missiles)
            foreach (var u in UnitRegistry.allUnits)
            {
                if (budget <= 0) break;
                if (!(u is Missile m) || m.disabled) continue;
                Unit owner = null;
                try { owner = m.owner; } catch { }
                var col = ColorFor(owner, hq, mine, out bool show);
                if (!show && m.NetworkHQ != null) { col = m.NetworkHQ == hq ? Ally : Enemy; show = true; }
                if (!show) continue;
                var gp = m.GlobalPosition();
                if (!toScreen(gp, out var g)) continue;
                budget--;
                Vector3 vel = m.transform.forward * 250f;
                try { var rb = m.rb; if (rb != null && rb.velocity.sqrMagnitude > 1f) vel = rb.velocity; } catch { }
                if (toScreen(gp - vel * trailSeconds * 1.5f, out var tail))
                    RangeOverlay.Line(tail, g, new Color(col.r, col.g, col.b, col.a * 0.6f), 2f);
                Diamond(g, dot + 3.5f, col);
            }
        }

        private static void Diamond(Vector2 c, float r, Color col)
        {
            Vector2 up = new Vector2(0, -r), rt = new Vector2(r, 0);
            RangeOverlay.Line(c + up, c + rt, col, 2f);
            RangeOverlay.Line(c + rt, c - up, col, 2f);
            RangeOverlay.Line(c - up, c - rt, col, 2f);
            RangeOverlay.Line(c - rt, c + up, col, 2f);
            RangeOverlay.Disc(c, 2.5f, col);
        }

        public static void Draw3D(Camera cam, HashSet<Unit> mine)
        {
            if (cam == null || Event.current.type != EventType.Repaint) return;
            Vector3 camPos = cam.transform.position, fwd = cam.transform.forward;
            DrawAll((GlobalPosition p, out Vector2 gui) =>
            {
                gui = default;
                Vector3 w = p.ToLocalPosition();
                if (Vector3.Dot(w - camPos, fwd) <= 0f) return false;
                Vector3 sp = cam.WorldToScreenPoint(w);
                if (sp.z <= 0f) return false;
                gui = new Vector2(sp.x, Screen.height - sp.y);
                return gui.x > -50f && gui.y > -50f && gui.x < Screen.width + 50f && gui.y < Screen.height + 50f;
            }, mine, 0.12f, 3f);
        }

        public static void DrawMap(HashSet<Unit> mine)
        {
            if (Event.current.type != EventType.Repaint) return;
            var map = SceneSingleton<DynamicMap>.i;
            if (map == null) return;
            DrawAll((GlobalPosition p, out Vector2 gui) => RangeOverlay.ToGui(map, p, out gui, out _), mine, 3f, 2.5f);
        }
    }

    [HarmonyPatch(typeof(BulletSim), nameof(BulletSim.Create))]
    internal static class BulletSim_Create_Patch
    {
        private static void Postfix(BulletSim __result)
        {
            try
            {
                Projectiles.Register(__result);
            }
            catch
            {
            }
        }
    }
}
