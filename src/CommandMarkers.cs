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
        private static void DrawOne(Camera cam, Unit u, Color color, float sizeMul)
        {
            if (u == null || u.disabled || u.definition == null) return;
            try
            {
                Vector3 world = u.transform.position;
                Vector3 camPos = cam.transform.position;
                if (Vector3.Dot(world - camPos, cam.transform.forward) <= 0f) return;
                Vector3 sp = cam.WorldToScreenPoint(world);
                if (sp.z <= 0f || sp.x < 0f || sp.y < 0f || sp.x > Screen.width || sp.y > Screen.height) return;

                var sprite = u.definition.friendlyIcon;
                if (sprite == null && GameAssets.i != null) sprite = GameAssets.i.targetUnitSpriteFriendly;
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

                var tex = sprite.texture;
                var tr = sprite.textureRect;
                var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
                Color old = GUI.color;
                GUI.color = new Color(color.r, color.g, color.b, 0.95f);
                GUI.DrawTextureWithTexCoords(r, tex, uv, true);
                GUI.color = old;
            }
            catch
            {
            }
        }
    }
}
