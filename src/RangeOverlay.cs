using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// On the big map: firing range of the selected units (surface and air reach, dead zone),
    /// and when the cursor is on an enemy — who of the selection can hit it right now.
    /// </summary>
    internal static class RangeOverlay
    {
        public static ConfigEntry<bool> Enabled;
        private static readonly Color AirColor = new Color(0.24f, 0.84f, 1f, 1f);
        private static readonly Color Ok = new Color(0.45f, 1f, 0.45f, 1f);
        private static readonly Color Bad = new Color(1f, 0.35f, 0.3f, 1f);
        private static Texture2D disc;
        private static readonly Dictionary<long, KeyValuePair<bool, float>> engageCache = new Dictionary<long, KeyValuePair<bool, float>>();

        private static bool CanEngage(Unit u, Unit t)
        {
            long key = ((long)u.GetInstanceID() << 32) ^ (uint)t.GetInstanceID();
            if (engageCache.TryGetValue(key, out var e) && Time.unscaledTime - e.Value < 0.3f) return e.Key;
            bool ok = false;
            try { ok = FocusFire.CanEngageNow(u, t); } catch { }
            if (engageCache.Count > 512) engageCache.Clear();
            engageCache[key] = new KeyValuePair<bool, float>(ok, Time.unscaledTime);
            return ok;
        }

        public static void Bind(ConfigFile cfg)
        {
            Enabled = cfg.Bind("0. Interface", "Weapon range on map", true,
                new ConfigDescription("Show the firing range of the selected units on the map and whether they can hit the enemy under the cursor.\n" +
                                      "Показывать на карте дальность огня выделенных юнитов и могут ли они поразить врага под курсором.",
                    null, new ConfigurationManagerAttributes { Order = 760 }));
        }

        public struct Reach
        {
            public float Surface, SurfaceMin, Air;
        }

        public static Reach Of(Unit u)
        {
            var r = new Reach { SurfaceMin = float.MaxValue };
            if (u?.weaponStations == null) return r;
            foreach (var ws in u.weaponStations)
            {
                var wi = ws?.WeaponInfo;
                if (wi == null || ws.Ammo <= 0 || wi.cargo || wi.troops || wi.jammer) continue;
                var req = wi.targetRequirements;
                if (wi.effectiveness.antiSurface > 0f)
                {
                    r.Surface = Mathf.Max(r.Surface, req.maxRange);
                    r.SurfaceMin = Mathf.Min(r.SurfaceMin, req.minRange);
                }
                if (wi.effectiveness.antiAir > 0f) r.Air = Mathf.Max(r.Air, req.maxRange);
            }
            if (r.SurfaceMin == float.MaxValue) r.SurfaceMin = 0f;
            return r;
        }

        /// <summary>Screen (GUI) point of a world position on the maximized map.</summary>
        public static bool ToGui(DynamicMap map, GlobalPosition p, out Vector2 gui, out float pxPerM)
        {
            gui = default;
            pxPerM = 0f;
            float s = map.mapImage.transform.lossyScale.x;
            if (s <= 0f || map.mapDimension <= 0f) return false;
            pxPerM = 900f * s / map.mapDimension;
            Vector3 o = map.mapImage.transform.position;
            gui = new Vector2(o.x + p.x * pxPerM, Screen.height - (o.y + p.z * pxPerM));
            return true;
        }

        public static void Draw(DynamicMap map, List<Unit> sel, Unit hover)
        {
            if (Event.current.type != EventType.Repaint || map == null || sel.Count == 0) return;
            var shown = sel.Take(16).ToList();
            foreach (var u in shown)
            {
                if (!ToGui(map, u.GlobalPosition(), out var c, out var k)) continue;
                var r = Of(u);
                Color col = OwnedMarks.Normal;
                if (r.Surface > 0f)
                {
                    Disc(c, r.Surface * k, new Color(col.r, col.g, col.b, 0.07f));
                    Circle(c, r.Surface * k, new Color(col.r, col.g, col.b, 0.9f), 2f, false);
                    if (r.SurfaceMin > 50f) Circle(c, r.SurfaceMin * k, new Color(col.r, col.g, col.b, 0.7f), 1.5f, true);
                }
                if (r.Air > 0f && Mathf.Abs(r.Air - r.Surface) > 50f) Circle(c, r.Air * k, new Color(AirColor.r, AirColor.g, AirColor.b, 0.85f), 1.5f, true);
                if (shown.Count <= 4)
                {
                    string t = (r.Surface > 0f ? Km(r.Surface) : "") + (r.Air > 0f && Mathf.Abs(r.Air - r.Surface) > 50f ? L.T("  air ", "  возд. ") + Km(r.Air) : "");
                    if (r.Surface <= 0f && r.Air <= 0f) t = L.T("no weapons / no ammo", "нет оружия / боезапаса");
                    float rr = Mathf.Max(r.Surface, r.Air) * k;
                    Label(new Vector2(c.x - 70f, c.y - rr - 22f), t, Color.white);
                }
                if (FocusFire.Orders.TryGetValue(u, out var ft) && ft != null && !ft.disabled && ft != hover && ToGui(map, ft.GlobalPosition(), out var tp, out _))
                {
                    Dashed(c, tp, CanEngage(u, ft) ? Ok : new Color(1f, 0.75f, 0.3f, 1f), 2f);
                }
            }

            if (hover == null || !ToGui(map, hover.GlobalPosition(), out var hp, out _)) return;
            int can = 0;
            float bestGap = float.MaxValue;
            foreach (var u in shown)
            {
                if (!ToGui(map, u.GlobalPosition(), out var c, out _)) continue;
                bool ok = CanEngage(u, hover);
                if (ok) can++;
                else
                {
                    var r = Of(u);
                    float reach = hover is Aircraft || hover is Missile ? r.Air : r.Surface;
                    float d = Vector3.Distance(u.transform.position, hover.transform.position);
                    if (reach > 0f) bestGap = Mathf.Min(bestGap, d - reach);
                }
                Line(c, hp, ok ? Ok : Bad, 2f);
            }
            Circle(hp, 14f, can > 0 ? Ok : Bad, 2f, false);
            string name = hover is Missile ? L.T("missile", "ракета") : (hover.definition?.unitName ?? hover.name);
            string msg = can == shown.Count
                ? L.T($"{name}: IN RANGE — all {can} will fire", $"{name}: В ЗОНЕ — огонь откроют все ({can})")
                : can > 0
                    ? L.T($"{name}: in range for {can} of {shown.Count}", $"{name}: достают {can} из {shown.Count}")
                    : L.T($"{name}: OUT OF RANGE", $"{name}: ВНЕ ДОСЯГАЕМОСТИ") + (bestGap < float.MaxValue && bestGap > 0f ? L.T($" — {Km(bestGap)} short", $" — не хватает {Km(bestGap)}") : L.T(" — no suitable weapon", " — нет подходящего оружия"));
            Label(new Vector2(hp.x + 18f, hp.y - 30f), msg, can == shown.Count ? Ok : can > 0 ? new Color(1f, 0.85f, 0.4f) : Bad);
        }

        public static string Km(float m) => m >= 1000f ? (m / 1000f).ToString("0.#") + L.T(" km", " км") : m.ToString("0") + L.T(" m", " м");

        public static void Label(Vector2 p, string text, Color col)
        {
            var st = Theme.Small;
            var size = st.CalcSize(new GUIContent(text));
            var r = new Rect(p.x, p.y, size.x + 12f, 22f);
            Color old = GUI.color;
            GUI.color = new Color(0.07f, 0.07f, 0.08f, 0.85f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = col;
            GUI.DrawTexture(new Rect(r.x, r.y, 2f, r.height), Texture2D.whiteTexture);
            GUI.color = old;
            var c = st.normal.textColor;
            st.normal.textColor = col;
            GUI.Label(new Rect(r.x + 7f, r.y + 2f, size.x + 4f, 20f), text, st);
            st.normal.textColor = c;
        }

        public static void Line(Vector2 a, Vector2 b, Color col, float w)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.5f) return;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Matrix4x4 m = GUI.matrix;
            Color old = GUI.color;
            GUI.color = col;
            GUIUtility.RotateAroundPivot(ang, a);
            GUI.DrawTexture(new Rect(a.x, a.y - w * 0.5f, len, w), Texture2D.whiteTexture);
            GUI.matrix = m;
            GUI.color = old;
        }

        public static void Dashed(Vector2 a, Vector2 b, Color col, float w)
        {
            float len = Vector2.Distance(a, b);
            int n = Mathf.Clamp((int)(len / 14f), 1, 200);
            for (int i = 0; i < n; i += 2) Line(Vector2.Lerp(a, b, i / (float)n), Vector2.Lerp(a, b, (i + 1) / (float)n), col, w);
        }

        public static void Circle(Vector2 c, float r, Color col, float w, bool dashed)
        {
            if (r < 2f || r > 20000f) return;
            int n = Mathf.Clamp((int)(r * 0.5f), 24, 160);
            if (dashed && n % 2 == 1) n++;
            Vector2 prev = c + new Vector2(r, 0f);
            for (int i = 1; i <= n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                Vector2 p = c + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                if (!dashed || i % 2 == 0) Line(prev, p, col, w);
                prev = p;
            }
        }

        public static void Disc(Vector2 c, float r, Color col)
        {
            if (r < 2f || r > 20000f) return;
            if (disc == null)
            {
                const int n = 256;
                disc = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                var px = new Color32[n * n];
                float h = n * 0.5f;
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - h) * (x + 0.5f - h) + (y + 0.5f - h) * (y + 0.5f - h));
                    byte a = (byte)(Mathf.Clamp01(h - d) * 255f);
                    px[y * n + x] = new Color32(255, 255, 255, a);
                }
                disc.SetPixels32(px);
                disc.Apply();
            }
            Color old = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), disc);
            GUI.color = old;
        }
    }
}

namespace FieldCommand
{
    /// <summary>Area-fire circles: on the map and projected onto the ground in the 3D view.</summary>
    internal static class AreaOverlay
    {
        public static readonly Color AreaColor = new Color(1f, 0.32f, 0.22f, 1f);

        public static void DrawMap(DynamicMap map, bool preview, GlobalPosition previewPoint, float previewRadius, List<Unit> previewUnits)
        {
            if (map == null) return;
            foreach (var kv in AreaFire.Orders)
            {
                var u = kv.Key;
                var o = kv.Value;
                if (u == null || !RangeOverlay.ToGui(map, o.Center, out var c, out var k)) continue;
                RangeOverlay.Disc(c, o.Radius * k, new Color(AreaColor.r, AreaColor.g, AreaColor.b, 0.12f));
                RangeOverlay.Circle(c, Mathf.Max(o.Radius * k, 5f), AreaColor, 2f, false);
                if (RangeOverlay.ToGui(map, u.GlobalPosition(), out var up, out _)) RangeOverlay.Dashed(up, c, new Color(AreaColor.r, AreaColor.g, AreaColor.b, 0.7f), 1.5f);
            }
            if (!preview || !RangeOverlay.ToGui(map, previewPoint, out var pc, out var pk)) return;
            int can = 0;
            foreach (var u in previewUnits)
            {
                if (u == null || !RangeOverlay.ToGui(map, u.GlobalPosition(), out var up, out _)) continue;
                bool ok = AreaFire.CanReach(u, previewPoint);
                if (ok) can++;
                RangeOverlay.Line(up, pc, ok ? new Color(0.45f, 1f, 0.45f, 0.8f) : new Color(1f, 0.35f, 0.3f, 0.6f), 1.5f);
            }
            Color col = can > 0 ? AreaColor : new Color(0.6f, 0.6f, 0.6f, 1f);
            RangeOverlay.Disc(pc, previewRadius * pk, new Color(col.r, col.g, col.b, 0.15f));
            RangeOverlay.Circle(pc, Mathf.Max(previewRadius * pk, 6f), col, 2f, true);
            RangeOverlay.Label(new Vector2(pc.x + 16f, pc.y + 10f),
                L.T($"AREA R={previewRadius:0} m — reach: {can} of {previewUnits.Count}", $"ПЛОЩАДЬ R={previewRadius:0} м — достают: {can} из {previewUnits.Count}"),
                can > 0 ? new Color(1f, 0.85f, 0.6f) : new Color(1f, 0.5f, 0.45f));
        }

        public static void Draw3D(Camera cam, bool preview, GlobalPosition previewPoint, float previewRadius, int canReach, int total)
        {
            if (cam == null) return;
            foreach (var o in AreaFire.Orders.Values) Ring(cam, o.Center, o.Radius, AreaColor, false);
            if (!preview) return;
            Color col = canReach > 0 ? AreaColor : new Color(0.7f, 0.7f, 0.7f, 1f);
            Ring(cam, previewPoint, previewRadius, col, true);
            Vector3 sp = cam.WorldToScreenPoint(previewPoint.ToLocalPosition());
            if (sp.z > 0f)
                RangeOverlay.Label(new Vector2(sp.x + 16f, Screen.height - sp.y + 10f),
                    L.T($"AREA R={previewRadius:0} m — reach: {canReach} of {total}", $"ПЛОЩАДЬ R={previewRadius:0} м — достают: {canReach} из {total}"),
                    canReach > 0 ? new Color(1f, 0.85f, 0.6f) : new Color(1f, 0.5f, 0.45f));
        }

        public static void Ring(Camera cam, GlobalPosition c, float r, Color col, bool dashed)
        {
            Vector3 center = c.ToLocalPosition();
            const int n = 48;
            Vector3? prev = null;
            for (int i = 0; i <= n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                Vector3 w = center + new Vector3(Mathf.Cos(a) * r, 2f, Mathf.Sin(a) * r);
                Vector3 s = cam.WorldToScreenPoint(w);
                Vector3? cur = s.z > 0f ? new Vector3(s.x, Screen.height - s.y, 0f) : (Vector3?)null;
                if (prev.HasValue && cur.HasValue && (!dashed || i % 2 == 0)) RangeOverlay.Line(prev.Value, cur.Value, col, 2f);
                prev = cur;
            }
            Vector3 cs = cam.WorldToScreenPoint(center);
            if (cs.z > 0f)
            {
                var p = new Vector2(cs.x, Screen.height - cs.y);
                RangeOverlay.Line(p + new Vector2(-6f, 0f), p + new Vector2(6f, 0f), col, 2f);
                RangeOverlay.Line(p + new Vector2(0f, -6f), p + new Vector2(0f, 6f), col, 2f);
            }
        }
    }
}
