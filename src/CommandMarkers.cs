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

        private static void DrawOne(Camera cam, Unit u, Color color, float sizeMul)
        {
            if (u == null || u.disabled || u.definition == null) return;
            try
            {
                float h = Mathf.Max(u.definition.height, 2f);
                Vector3 world = u.transform.position + Vector3.up * (h + 4f);
                Vector3 sp = cam.WorldToScreenPoint(world);
                if (sp.z <= 0f || sp.x < -40f || sp.y < -40f || sp.x > Screen.width + 40f || sp.y > Screen.height + 40f) return;
                float dist = sp.z;
                // big up close, never smaller than a readable symbol far away
                float size = Mathf.Clamp(900f / Mathf.Max(dist, 1f) * 10f, 16f, 34f) * sizeMul;
                var c = new Vector2(sp.x, Screen.height - sp.y);
                var r = new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size);

                Color old = GUI.color;
                // dark plate so the symbol reads over sky and terrain
                GUI.color = new Color(0f, 0f, 0f, 0.45f);
                GUI.DrawTexture(new Rect(r.x - 3f, r.y - 3f, r.width + 6f, r.height + 6f), Texture2D.whiteTexture);
                GUI.color = color;
                GUI.DrawTexture(new Rect(r.x - 3f, r.yMax + 3f, r.width + 6f, 2f), Texture2D.whiteTexture);

                var sprite = u.definition.mapIcon;
                if (sprite != null && sprite.texture != null)
                {
                    var tr = sprite.textureRect;
                    var tex = sprite.texture;
                    var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
                    GUI.DrawTextureWithTexCoords(r, tex, uv, true);
                }
                else
                {
                    GUI.DrawTexture(new Rect(c.x - 4f, c.y - 4f, 8f, 8f), Texture2D.whiteTexture);
                }
                GUI.color = old;
            }
            catch
            {
            }
        }
    }
}
