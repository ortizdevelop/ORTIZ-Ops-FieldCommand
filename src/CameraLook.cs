using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// In command mode the free camera's "Free Look" (normally RMB) follows the middle mouse button,
    /// so RMB is free for move / attack orders.
    /// </summary>
    [HarmonyPatch]
    internal static class FreeLook_Patch
    {
        public static bool CommandMode;

        private static bool Prepare() => TargetMethod() != null;

        private static MethodBase TargetMethod()
        {
            var t = AccessTools.TypeByName("Rewired.Player");
            if (t == null) return null;
            var m = AccessTools.Method(t, "GetButton", new[] { typeof(string) });
            return m != null && !m.IsAbstract ? m : null;
        }

        private static void Postfix(string actionName, ref bool __result)
        {
            if (!CommandMode || actionName != "Free Look") return;
            __result = Input.GetMouseButton(2);
        }
    }
}
