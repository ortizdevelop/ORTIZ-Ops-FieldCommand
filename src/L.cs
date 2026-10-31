using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace FieldCommand
{
    public enum UiLanguage
    {
        English,
        Russian
    }

    /// <summary>
    /// Read by BepInEx Configuration Manager (F1) through reflection: lets the settings window show
    /// Russian section and setting names while the config keys themselves stay English.
    /// </summary>
#pragma warning disable 0649
    internal sealed class ConfigurationManagerAttributes
    {
        public string Category;
        public string DispName;
        public int? Order;
    }
#pragma warning restore 0649

    /// <summary>Interface language: English by default, Russian on demand.</summary>
    internal static class L
    {
        internal static ConfigEntry<UiLanguage> Language;

        public static bool Ru => Language != null && Language.Value == UiLanguage.Russian;

        /// <summary>Pick the English or the Russian text for the current language.</summary>
        public static string T(string en, string ru) => Ru ? ru : en;

        public static void BindLanguage(ConfigFile cfg)
        {
            Language = cfg.Bind("0. Language", "Interface language", UiLanguage.English,
                new ConfigDescription("Language of the mod's windows and messages (switches immediately). Setting names in this F1 window change after a game restart.\n" +
                                      "Язык окон и сообщений мода (меняется сразу). Названия настроек в окне F1 меняются после перезапуска игры.",
                    null, new ConfigurationManagerAttributes { Order = 1000 }));
        }

        private static Dictionary<ConfigDefinition, string> Orphans(ConfigFile cfg)
        {
            try
            {
                var p = typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                return p?.GetValue(cfg) as Dictionary<ConfigDefinition, string>;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Binds a setting under an English key. The F1 window shows the Russian names when the language is Russian.
        /// A value saved earlier under the old Russian key is carried over once.
        /// </summary>
        public static ConfigEntry<T> Bind<T>(ConfigFile cfg, string secEn, string secRu, string keyEn, string keyRu, T def,
            string descEn, string descRu, AcceptableValueBase range = null)
        {
            var attrs = new ConfigurationManagerAttributes();
            if (Ru)
            {
                attrs.Category = secRu;
                attrs.DispName = keyRu;
            }
            var entry = cfg.Bind(secEn, keyEn, def, new ConfigDescription(Ru ? descRu : descEn, range, attrs));
            try
            {
                var orphans = Orphans(cfg);
                var old = new ConfigDefinition(secRu, keyRu);
                if (orphans != null && orphans.TryGetValue(old, out var saved))
                {
                    entry.SetSerializedValue(saved);
                    orphans.Remove(old);
                    cfg.Save();
                }
            }
            catch
            {
                // migration is best-effort
            }
            return entry;
        }
    }
}
