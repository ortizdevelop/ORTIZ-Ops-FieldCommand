using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NuclearOption.SavedMission;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// Works out which faction (BDF / PALA) each unit type belongs to.
    /// The game has no "faction" field on unit definitions, so we learn it from
    /// how the game itself uses each unit: faction convoy lists, all built-in
    /// missions, the player's own missions and the mission being played.
    /// </summary>
    internal static class FactionResolver
    {
        public const string Shared = "*";

        // type key -> faction name -> weight
        private static Dictionary<string, Dictionary<string, float>> missionVotes;
        private static readonly Dictionary<UnitDefinition, string> keyOf = new Dictionary<UnitDefinition, string>();

        public static string KeyOf(UnitDefinition def)
        {
            if (def == null) return null;
            if (keyOf.Count == 0 || !keyOf.ContainsKey(def))
            {
                keyOf.Clear();
                foreach (var kv in Encyclopedia.Lookup)
                {
                    if (kv.Value != null && !keyOf.ContainsKey(kv.Value))
                    {
                        keyOf[kv.Value] = kv.Key;
                    }
                }
            }
            return keyOf.TryGetValue(def, out var k) ? k : (string.IsNullOrEmpty(def.jsonKey) ? def.name : def.jsonKey);
        }

        public static string FactionNameOf(FactionHQ hq)
        {
            try
            {
                return hq?.faction?.factionName ?? "";
            }
            catch
            {
                return "";
            }
        }

        public static string TagOf(string factionName)
        {
            if (factionName == Shared) return "общая";
            if (string.IsNullOrEmpty(factionName)) return "?";
            foreach (var f in FactionRegistry.factions)
            {
                if (f != null && f.factionName == factionName)
                {
                    return string.IsNullOrEmpty(f.factionTag) ? f.factionName : f.factionTag;
                }
            }
            return factionName;
        }

        private static void Vote(Dictionary<string, Dictionary<string, float>> votes, string key, string faction, float w)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(faction)) return;
            if (!votes.TryGetValue(key, out var d))
            {
                d = new Dictionary<string, float>();
                votes[key] = d;
            }
            d.TryGetValue(faction, out float cur);
            d[faction] = cur + w;
        }

        // ---------- mission scanning (done once per game launch) ----------

        private static void EnsureMissionVotes()
        {
            if (missionVotes != null) return;
            missionVotes = new Dictionary<string, Dictionary<string, float>>();
            int files = 0;
            foreach (string path in new[] { "Missions", "Tutorials", "DefaultMission" })
            {
                try
                {
                    foreach (var ta in Resources.LoadAll<TextAsset>(path))
                    {
                        if (ta != null && ScanJson(ta.text, missionVotes, 1f)) files++;
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning($"Mission resources '{path}' not readable: {e.Message}");
                }
            }
            try
            {
                string dir = MissionGroup.UserGroup.UserMissionDirectory;
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    foreach (var f in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories).Take(400))
                    {
                        try
                        {
                            if (new FileInfo(f).Length < 20_000_000 && ScanJson(File.ReadAllText(f), missionVotes, 1f)) files++;
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("User missions not readable: " + e.Message);
            }
            Plugin.Log.LogInfo($"Faction data: scanned {files} missions, {missionVotes.Count} unit types seen");
        }

        private static bool ScanJson(string json, Dictionary<string, Dictionary<string, float>> votes, float w)
        {
            if (string.IsNullOrEmpty(json)) return false;
            string trimmed = json.TrimStart();
            if (trimmed.Length == 0 || trimmed[0] != '{') return false;
            JToken root;
            try
            {
                root = JToken.Parse(json);
            }
            catch
            {
                return false;
            }
            var stack = new Stack<JToken>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                if (t is JObject o)
                {
                    var type = o["type"] as JValue;
                    var fac = o["faction"] as JValue;
                    if (type != null && fac != null && type.Type == JTokenType.String && fac.Type == JTokenType.String)
                    {
                        Vote(votes, (string)type, (string)fac, w);
                    }
                    foreach (var p in o.Properties())
                    {
                        if (p.Value is JContainer) stack.Push(p.Value);
                    }
                }
                else if (t is JArray a)
                {
                    foreach (var c in a)
                    {
                        if (c is JContainer) stack.Push(c);
                    }
                }
            }
            return true;
        }

        // ---------- resolution ----------

        private static Dictionary<string, string> ParseOverrides()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string s = Plugin.FactionOverrides.Value ?? "";
            foreach (var part in s.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int i = part.IndexOf(':');
                if (i <= 0) i = part.IndexOf('=');
                if (i <= 0) continue;
                string key = part.Substring(0, i).Trim();
                string val = part.Substring(i + 1).Trim();
                if (key.Length > 0 && val.Length > 0) map[key] = val;
            }
            return map;
        }

        private static string ResolveOverrideValue(string val)
        {
            if (val.Equals("ALL", StringComparison.OrdinalIgnoreCase) || val == "*" || val.Equals("общая", StringComparison.OrdinalIgnoreCase))
            {
                return Shared;
            }
            foreach (var f in FactionRegistry.factions)
            {
                if (f == null) continue;
                if (string.Equals(f.factionTag, val, StringComparison.OrdinalIgnoreCase) || string.Equals(f.factionName, val, StringComparison.OrdinalIgnoreCase))
                {
                    return f.factionName;
                }
            }
            return val;
        }

        /// <summary>Returns faction name, Shared, or null when unknown.</summary>
        public static Dictionary<UnitDefinition, string> Resolve(IEnumerable<UnitDefinition> defs)
        {
            EnsureMissionVotes();
            var votes = new Dictionary<string, Dictionary<string, float>>();
            foreach (var kv in missionVotes)
            {
                votes[kv.Key] = new Dictionary<string, float>(kv.Value);
            }

            // the faction's own convoy list is the strongest hint
            foreach (var f in FactionRegistry.factions)
            {
                if (f == null) continue;
                try
                {
                    foreach (var g in f.GetConvoyGroups())
                    {
                        foreach (var c in g.Constituents)
                        {
                            Vote(votes, KeyOf(c.Type), f.factionName, 25f);
                        }
                    }
                }
                catch
                {
                }
            }

            // what the current mission placed for each side
            try
            {
                var m = MissionManager.CurrentMission;
                if (m != null)
                {
                    var all = new List<SavedUnit>();
                    MissionManager.GetAllSavedUnitsNonAlloc(m, all, true);
                    foreach (var su in all)
                    {
                        if (su != null) Vote(votes, su.type, su.faction, 3f);
                    }
                }
            }
            catch
            {
            }

            var overrides = ParseOverrides();
            float share = Mathf.Clamp(Plugin.FactionShareThreshold.Value, 0.5f, 1f);
            var result = new Dictionary<UnitDefinition, string>();
            foreach (var def in defs)
            {
                string key = KeyOf(def);
                if (key != null && (overrides.TryGetValue(key, out var ov) || overrides.TryGetValue(def.unitName ?? "", out ov)))
                {
                    result[def] = ResolveOverrideValue(ov);
                    continue;
                }
                if (key == null || !votes.TryGetValue(key, out var d) || d.Count == 0)
                {
                    result[def] = null;
                    continue;
                }
                float total = d.Values.Sum();
                var top = d.OrderByDescending(x => x.Value).First();
                result[def] = top.Value / total >= share ? top.Key : Shared;
            }
            return result;
        }
    }
}
