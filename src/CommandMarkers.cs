using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace FieldCommand
{
    public enum MarkerMode
    {
        CommandMode,
        Always,
        Off
    }

    /// <summary>
    /// RTS-style markers in the 3D view: the unit's own map symbol above every unit you bought,
    /// in your colour (faction-bought units in a softer colour). Shown in command mode (purchase menu
    /// open or placing) by default.
    /// </summary>
    internal static class CommandMarkers
    {
        public static ConfigEntry<MarkerMode> Mode;
        public static ConfigEntry<bool> FactionToo;
        private static readonly Color FactionColor = new Color(0.43f, 0.91f, 0.72f, 1f);

        public static void Bind(ConfigFile cfg)
        {
            Mode = cfg.Bind("0. Interface", "Markers above my units", MarkerMode.CommandMode,
                new ConfigDescription("Unit symbols above your purchased units in the 3D view: CommandMode — while the purchase menu is open or you are placing, Always, Off.\n" +
                                      "Значки над купленными юнитами в 3D: CommandMode — пока открыто меню покупки или идёт расстановка, Always — всегда, Off — выключено.",
                    null, new ConfigurationManagerAttributes { Order = 780 }));
            FactionToo = cfg.Bind("0. Interface", "Markers above faction-bought units", true,
                new ConfigDescription("Also mark the units bought for the faction (in a softer green).\nПомечать и юнитов, купленных для фракции (мягким зелёным).",
                    null, new ConfigurationManagerAttributes { Order = 770 }));
        }

        // Some unit icons are stored as a light glyph on an opaque black square (the game draws them additively).
        // We make a transparent copy of each icon once: alpha = brightness, colour = white-normalised glyph.
        private static readonly Dictionary<Sprite, Texture2D> cleanIcons = new Dictionary<Sprite, Texture2D>();
        private static readonly HashSet<Sprite> pending = new HashSet<Sprite>();

        private static Texture2D Clean(Sprite s)
        {
            if (cleanIcons.TryGetValue(s, out var t)) return t;
            pending.Add(s);
            return null;
        }

        /// <summary>Called from Update (outside of OnGUI) — render targets can be used safely here.</summary>
        public static void ProcessPending()
        {
            if (pending.Count == 0) return;
            foreach (var s in pending)
            {
                Texture2D t = null;
                try
                {
                    t = MakeClean(s);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("[Markers] icon " + (s != null ? s.name : "?") + ": " + e.Message);
                }
                if (s != null) cleanIcons[s] = t;
            }
            pending.Clear();
        }

        private static Texture2D MakeClean(Sprite s)
        {
            if (s == null || s.texture == null) return null;
            var tex = s.texture;
            // sprite bounds in texture pixels (works for packed and tight sprites)
            Vector2 mn = new Vector2(float.MaxValue, float.MaxValue), mx = new Vector2(float.MinValue, float.MinValue);
            foreach (var uv in s.uv)
            {
                mn = Vector2.Min(mn, uv);
                mx = Vector2.Max(mx, uv);
            }
            int x0 = Mathf.Clamp(Mathf.FloorToInt(mn.x * tex.width), 0, tex.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(mn.y * tex.height), 0, tex.height - 1);
            int w = Mathf.Clamp(Mathf.CeilToInt(mx.x * tex.width) - x0, 1, tex.width - x0);
            int h = Mathf.Clamp(Mathf.CeilToInt(mx.y * tex.height) - y0, 1, tex.height - y0);

            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            var prev = RenderTexture.active;
            var outT = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            try
            {
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;
                outT.ReadPixels(new Rect(x0, y0, w, h), 0, 0, false);
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }
            var px = outT.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                var p = px[i];
                int m = Math.Max(p.r, Math.Max(p.g, p.b));
                int a = p.a * m / 255;
                if (m > 0)
                {
                    p.r = (byte)(p.r * 255 / m);
                    p.g = (byte)(p.g * 255 / m);
                    p.b = (byte)(p.b * 255 / m);
                }
                p.a = (byte)a;
                px[i] = p;
            }
            outT.SetPixels32(px);
            outT.Apply(false, false);
            return outT;
        }

        public static bool ShouldDraw(bool commandMode)
        {
            if (Mode == null || Mode.Value == MarkerMode.Off) return false;
            if (DynamicMap.mapMaximized) return false; // the map shows its own icons
            return Mode.Value == MarkerMode.Always || commandMode;
        }

        public static void Draw(Camera cam, List<Unit> mine, List<Unit> faction)
        {
            if (cam == null || Event.current.type != EventType.Repaint) return;
            foreach (var u in mine) DrawOne(cam, u, OwnedMarks.Normal, 1f);
            if (FactionToo.Value)
            {
                foreach (var u in faction) DrawOne(cam, u, FactionColor, 0.85f);
            }
        }

        /// <summary>Same look as the game's HUD and Ground Control RTS: the unit's own outlined "friendly" icon at the unit, tinted.</summary>
        private static readonly Color EnemyColor = new Color(1f, 0.27f, 0.22f, 1f);

        /// <summary>Enemies our side has spotted: the game's hostile icon at their known position, in red.</summary>
        public static void DrawEnemies(Camera cam)
        {
            if (cam == null || Event.current.type != EventType.Repaint || !GameApi.TryGetLocal(out _, out var hq) || hq == null) return;
            foreach (var u in UnitRegistry.allUnits)
            {
                try
                {
                    if (u == null || u.disabled || u.definition == null || u.NetworkHQ == null || u.NetworkHQ == hq) continue;
                    if (u is Missile || u is Building && !(u.GetComponentInChildren<Turret>() != null)) continue;
                    if (!hq.TryGetKnownPosition(u, out var known)) continue;
                    if (!hq.IsTargetBeingTracked(u)) continue;
                    DrawOne(cam, u, EnemyColor, 0.9f, known.ToLocalPosition(), true);
                }
                catch
                {
                }
            }
        }

        private static void DrawOne(Camera cam, Unit u, Color color, float sizeMul)
        {
            if (u == null) return;
            DrawOne(cam, u, color, sizeMul, u.transform.position, false);
        }

        private static void DrawOne(Camera cam, Unit u, Color color, float sizeMul, Vector3 world, bool hostile)
        {
            if (u == null || u.disabled || u.definition == null) return;
            try
            {
                Vector3 camPos = cam.transform.position;
                if (Vector3.Dot(world - camPos, cam.transform.forward) <= 0f) return;
                Vector3 sp = cam.WorldToScreenPoint(world);
                if (sp.z <= 0f || sp.x < 0f || sp.y < 0f || sp.x > Screen.width || sp.y > Screen.height) return;

                var sprite = hostile ? u.definition.hostileIcon : u.definition.friendlyIcon;
                if (sprite == null && GameAssets.i != null) sprite = hostile ? GameAssets.i.targetUnitSprite : GameAssets.i.targetUnitSpriteFriendly;
                if (sprite == null || sprite.texture == null) return;

                // the game shrinks markers with distance (like RTS: 1 → 0.45 between ~12 and 37 km)
                float t = Mathf.Clamp01(Vector3.Distance(camPos, world) * 4E-05f - 0.5f);
                float scale = Mathf.Lerp(1f, 0.45f, t);
                float iconSize = u.definition.iconSize > 0f ? u.definition.iconSize : 1f;
                float hmd = PlayerSettings.hmdIconSize > 0f ? PlayerSettings.hmdIconSize : 1f;
                var sr = sprite.rect;
                // HUD marker size = iconSize × "HMD icon size" setting (30 by default) × distance scale, on a 1080p-referenced canvas
                float h = Mathf.Max(iconSize * hmd * scale * (Screen.height / 1080f) * sizeMul, 10f);
                float w = h * (sr.width / Mathf.Max(sr.height, 1f));
                var c = new Vector2(sp.x, Screen.height - sp.y);
                var r = new Rect(c.x - w * 0.5f, c.y - h * 0.5f, w, h);

                var clean = Clean(sprite);
                if (clean == null) return;
                Color old = GUI.color;
                GUI.color = new Color(color.r, color.g, color.b, 0.95f);
                GUI.DrawTexture(r, clean, ScaleMode.StretchToFill, true);
                GUI.color = old;
            }
            catch
            {
            }
        }
    }
}
