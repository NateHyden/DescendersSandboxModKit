using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class NoBail
    {
        public static bool Enabled { get; private set; } = false;

        private static PlayerInfoImpact _cached = null;

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
            MelonLogger.Msg("No Bail -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            Apply();
        }

        // Called from OnUpdate � only does real work when toggled, not every frame
        public static void Apply()
        {
            try
            {
                if ((object)_cached == null)
                {
                    GameObject playerInfoObject = GameObject.Find("PlayerInfo_Human");
                    if ((object)playerInfoObject == null) return;
                    _cached = playerInfoObject.GetComponent<PlayerInfoImpact>();
                }
                if ((object)_cached == null) return;
                _cached.Nobail(Enabled);
            }
            catch (System.Exception ex) { MelonLogger.Error("NoBail.Apply: " + ex.Message); }
        }

        public static void ClearCache() { _cached = null; }

        // Patches Vehicle.Reset(bool) — fires after every respawn (bail or manual)
        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo resetMethod = typeof(Vehicle).GetMethod("Reset",
                    BindingFlags.Public | BindingFlags.Instance,
                    null, new System.Type[] { typeof(bool) }, null);

                if ((object)resetMethod == null)
                {
                    MelonLogger.Warning("[NoBail] Vehicle.Reset(bool) not found.");
                    return;
                }

                MethodInfo postfix = typeof(NoBail_Patch).GetMethod("Postfix",
                    BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(resetMethod, postfix: new HarmonyMethod(postfix));
                MelonLogger.Msg("[NoBail] Patched Vehicle.Reset(bool).");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[NoBail] ApplyPatch: " + ex.Message);
            }
        }
    }

    public static class NoBail_Patch
    {
        public static void Postfix(Vehicle __instance)
        {
            if (!NoBail.Enabled) return;
            if ((object)__instance == null) return;

            try
            {
                if (!string.Equals(__instance.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                // Clear cache so Apply() re-finds PlayerInfoImpact fresh after respawn
                NoBail.ClearCache();
                NoBail.Apply();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[NoBail] Postfix: " + ex.Message);
            }
        }
    }
}