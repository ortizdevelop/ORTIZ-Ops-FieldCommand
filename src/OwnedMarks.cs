using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// Units the player bought "for me" get their own colour on the map and on the 3D HUD markers,
    /// so they stand out from the rest of the faction. The colour is configurable (default: interface orange).
    /// </summary>
    internal static class OwnedMarks
    {
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<Color> MyColor;

        public static readonly HashSet<Unit> Mine = new HashSet<Unit>();
        private static readonly HashSet<Unit> scratch = new HashSet<Unit>();
        private static float nextTick;
        private static Color lastColor;
        private static bool lastEnabled;

        private static AccessTools.FieldRef<CombatHUD, Dictionary<Unit, HUDUnitMarker>> markerLookup;
        private static System.Reflection.MethodInfo setFactionColor, updateMarkerColor;
        private static bool reflected;

        private static void Reflect()
        {
            if (reflected) return;
            reflected = true;
            markerLookup = SafeRef<CombatHUD, Dictionary<Unit, HUDUnitMarker>>("markerLookup");
            try
            {
                setFactionColor = AccessTools.Method(typeof(HUDUnitMarker), "SetFactionColor");
                updateMarkerColor = AccessTools.Method(typeof(HUDUnitMarker), "UpdateColor");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[Marks] " + e.Message);
            }
        }

        public static void Bind(ConfigFile cfg)
        {
            Enabled = cfg.Bind("0. Interface", "Highlight my units", true,
                new ConfigDescription("Units bought for yourself get their own colour on the map and on the HUD markers.\nЮниты, купленные себе, выделяются своим цветом на карте и на метках в 3D.",
                    null, new ConfigurationManagerAttributes { Order = 800 }));
            MyColor = cfg.Bind("0. Interface", "My units colour", new Color(1f, 0.478f, 0.239f, 1f),
                new ConfigDescription("Colour of the units bought for yourself (default: interface orange).\nЦвет купленных себе юнитов (по умолчанию — оранжевый, как в интерфейсе).",
                    null, new ConfigurationManagerAttributes { Order = 790 }));
        }

        public static bool Active => Enabled != null && Enabled.Value;

        public static Color Normal
        {
            get
            {
                var c = MyColor?.Value ?? new Color(1f, 0.478f, 0.239f, 1f);
                c.a = 1f;
                return c;
            }
        }

        public static Color Selected => Color.Lerp(Normal, Color.white, 0.45f);

        public static bool IsMine(Unit u) => u != null && Mine.Contains(u);

        public static void Reset()
        {
            Mine.Clear();
            nextTick = 0f;
        }

        /// <summary>Keeps the set in sync with the purchased list and recolours icons/markers that changed.</summary>
        public static void Tick(List<Unit> purchased)
        {
            if (Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + 0.5f;
            bool settingsChanged = Enabled.Value != lastEnabled || MyColor.Value != lastColor;
            lastEnabled = Enabled.Value;
            lastColor = MyColor.Value;

            scratch.Clear();
            foreach (var u in purchased)
            {
                if (u != null && !u.disabled) scratch.Add(u);
            }
            var added = scratch.Where(u => !Mine.Contains(u)).ToList();
            var removed = Mine.Where(u => !scratch.Contains(u)).ToList();
            foreach (var u in removed) Mine.Remove(u);
            foreach (var u in added) Mine.Add(u);

            IEnumerable<Unit> refresh = settingsChanged ? Mine.Concat(removed) : added.Concat(removed);
            foreach (var u in refresh.ToList()) Refresh(u);
        }

        private static void Refresh(Unit u)
        {
            if (u == null) return;
            Reflect();
            try
            {
                var map = SceneSingleton<DynamicMap>.i;
                if (map != null && map.TryGetIcon(u, out var icon) && icon != null) icon.UpdateColor();
            }
            catch
            {
            }
            try
            {
                var hud = SceneSingleton<CombatHUD>.i;
                if (hud == null || markerLookup == null) return;
                var dict = markerLookup(hud);
                if (dict != null && dict.TryGetValue(u, out var marker) && marker != null)
                {
                    setFactionColor?.Invoke(marker, null);
                    updateMarkerColor?.Invoke(marker, null);
                }
            }
            catch
            {
            }
        }

        private static AccessTools.FieldRef<TObj, TField> SafeRef<TObj, TField>(string name)
        {
            try
            {
                return AccessTools.FieldRefAccess<TObj, TField>(name);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[Marks] " + name + ": " + e.Message);
                return null;
            }
        }
    }

    [HarmonyPatch(typeof(UnitMapIcon), "GetColor")]
    internal static class UnitMapIcon_GetColor_Patch
    {
        private static AccessTools.FieldRef<MapIcon, bool> selectedRef;

        private static void Postfix(UnitMapIcon __instance, ref Color __result)
        {
            try
            {
                if (!OwnedMarks.Active || !OwnedMarks.IsMine(__instance.unit)) return;
                if (selectedRef == null) selectedRef = AccessTools.FieldRefAccess<MapIcon, bool>("isSelected");
                __result = selectedRef(__instance) ? OwnedMarks.Selected : OwnedMarks.Normal;
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(HUDUnitMarker), "SetFactionColor")]
    internal static class HUDUnitMarker_SetFactionColor_Patch
    {
        private static AccessTools.FieldRef<HUDUnitMarker, Color> colorRef;

        private static void Postfix(HUDUnitMarker __instance)
        {
            try
            {
                if (!OwnedMarks.Active || !OwnedMarks.IsMine(__instance.unit)) return;
                if (colorRef == null) colorRef = AccessTools.FieldRefAccess<HUDUnitMarker, Color>("color");
                colorRef(__instance) = OwnedMarks.Normal;
            }
            catch
            {
            }
        }
    }
}

namespace FieldCommand
{
    /// <summary>Ground Control (RTS) draws its own unit markers in the commander view — paint ours there too.</summary>
    [HarmonyPatch]
    internal static class RtsMarker_Update_Patch
    {
        private static System.Type viewType;
        private static AccessTools.FieldRef<object, Unit> unitRef;
        private static AccessTools.FieldRef<object, UnityEngine.UI.Image> imageRef;
        private static AccessTools.FieldRef<object, bool> depotRef;

        private static bool Prepare()
        {
            viewType = AccessTools.TypeByName("GroundControlRts.CommanderMarkerView");
            return viewType != null && AccessTools.Method(viewType, "Update") != null;
        }

        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(viewType, "Update");
        }

        private static void Postfix(object __instance, bool selected)
        {
            try
            {
                if (!OwnedMarks.Active) return;
                if (unitRef == null)
                {
                    unitRef = AccessTools.FieldRefAccess<Unit>(viewType, "unit");
                    imageRef = AccessTools.FieldRefAccess<UnityEngine.UI.Image>(viewType, "image");
                    depotRef = AccessTools.FieldRefAccess<bool>(viewType, "depotMarker");
                }
                var unit = unitRef(__instance);
                if (!OwnedMarks.IsMine(unit) || depotRef(__instance)) return;
                var image = imageRef(__instance);
                if (image == null || !image.enabled) return;
                Color c = selected ? OwnedMarks.Selected : OwnedMarks.Normal;
                c.a = image.color.a;
                image.color = c;
            }
            catch
            {
            }
        }
    }
}
