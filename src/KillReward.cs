using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// When units bought with Field Command destroy something (vehicles, ships, buildings,
    /// aircraft, missiles), the player gets a share of the money a player would get for that kill.
    /// Uses the game's own damage credit and its own reward formula
    /// (FactionHQ.ReportKillAction: killReward * share * sqrt(value), minus player tax).
    /// </summary>
    internal static class KillReward
    {
        /// <summary>persistent IDs of units bought "for me"</summary>
        public static readonly HashSet<PersistentID> Mine = new HashSet<PersistentID>();

        /// <summary>persistent IDs of units bought "for the faction"</summary>
        public static readonly HashSet<PersistentID> ForFaction = new HashSet<PersistentID>();

        public static Action<string> Toast;

        public static float SessionTotal;

        public static void Remember(Unit u, bool forFaction)
        {
            if (u == null || u is Aircraft) return; // KAR/BOTE you drive yourself are already paid by the game
            try
            {
                var id = u.persistentID;
                if (id == PersistentID.None) return;
                (forFaction ? ForFaction : Mine).Add(id);
            }
            catch
            {
            }
        }

        public static void Reset()
        {
            Mine.Clear();
            ForFaction.Clear();
            SessionTotal = 0f;
        }

        public static bool Counts(PersistentID id)
        {
            if (Mine.Contains(id)) return true;
            return Plugin.RewardFromFactionUnits.Value && ForFaction.Contains(id);
        }

        // pending toast aggregation (missile salvos kill many things in one second)
        private static float pendingMoney;
        private static int pendingKills;
        private static string lastName;
        private static float flushAt;

        public static void Tick()
        {
            if (pendingKills > 0 && Time.unscaledTime >= flushAt)
            {
                string what = pendingKills == 1 ? lastName : L.T($"{pendingKills} targets", $"{pendingKills} целей");
                Toast?.Invoke(L.T($"+{GameApi.Money(pendingMoney)} — your units destroyed: {what}", $"+{GameApi.Money(pendingMoney)} — твои юниты уничтожили: {what}"));
                pendingKills = 0;
                pendingMoney = 0f;
            }
        }

        public static void OnKilled(Unit victim)
        {
            if (Plugin.KillRewardPercent.Value <= 0f && Stratagems.RewardPercent.Value <= 0f) return;
            if (!GameApi.IsServer() || !GameApi.TryGetLocal(out var player, out var hq)) return;
            if (victim == null || victim.definition == null) return;
            if (victim.NetworkHQ == hq) return; // friendly fire pays nothing

            var credit = Traverse.Create(victim).Field("damageCredit").GetValue<Dictionary<PersistentID, float>>();
            if (credit == null || credit.Count == 0) return;

            float total = 0f;
            foreach (var kv in credit) total += kv.Value;
            if (total <= 0f) return;

            float factor = 0f;
            foreach (var kv in credit)
            {
                float share = kv.Value / total;
                if (share < 0.01f || !Counts(kv.Key)) continue;
                factor += share;
            }
            // damage with no owner (our stratagem munitions) inside a fresh strike zone
            float strikeFactor = 0f;
            if (Stratagems.RewardPercent.Value > 0f && credit.TryGetValue(PersistentID.None, out float none) && none / total >= 0.01f
                && Stratagems.InRecentStrike(victim.GlobalPosition()))
            {
                strikeFactor = none / total;
            }
            if (factor <= 0f && strikeFactor <= 0f) return;

            float value = victim.definition.value;
            try
            {
                value += victim.GetAmmoValue().Current;
            }
            catch
            {
            }
            if (value <= 0f) return;

            // exactly what a player pilot would get for the same share of the kill...
            float playerReward = hq.killReward * factor * Mathf.Sqrt(value) * (1f - hq.playerTaxRate);
            // ...times the configured percentage
            float baseReward = hq.killReward * Mathf.Sqrt(value) * (1f - hq.playerTaxRate);
            float money = baseReward * factor * Mathf.Clamp(Plugin.KillRewardPercent.Value, 0f, 500f) / 100f
                        + baseReward * strikeFactor * Mathf.Clamp(Stratagems.RewardPercent.Value, 0f, 100f) / 100f;
            if (money <= 0.0001f) return;

            player.AddAllocation(money);
            SessionTotal += money;
            pendingMoney += money;
            pendingKills++;
            lastName = victim is Missile ? L.T("missile", "ракета") : (victim.definition.unitName ?? victim.name);
            flushAt = Time.unscaledTime + 1.5f;
            Plugin.Log.LogInfo($"[Reward] {lastName}: share {factor:P0}, +{money:0.00}M");
        }
    }

    [HarmonyPatch(typeof(Unit), nameof(Unit.ReportKilled))]
    internal static class Unit_ReportKilled_Patch
    {
        private static void Postfix(Unit __instance)
        {
            try
            {
                KillReward.OnKilled(__instance);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[Reward] " + e.Message);
            }
        }
    }
}
