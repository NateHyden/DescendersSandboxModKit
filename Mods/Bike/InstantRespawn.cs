using System.Reflection;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class InstantRespawn
    {
        public static bool Enabled { get; private set; } = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("[InstantRespawn] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void OnBail()
        {
            if (!Enabled) return;
            try
            {
                PlayerManager pm = Object.FindObjectOfType<PlayerManager>();
                if ((object)pm == null)
                {
                    ModLog.Warn("[InstantRespawn] PlayerManager not found.");
                    return;
                }

                MethodInfo getPii = typeof(PlayerManager).GetMethod(
                    "GetPlayerImpact", BindingFlags.Public | BindingFlags.Instance);
                if ((object)getPii == null)
                {
                    ModLog.Warn("[InstantRespawn] GetPlayerImpact not found.");
                    return;
                }

                object pii = getPii.Invoke(pm, null);
                if ((object)pii == null)
                {
                    ModLog.Warn("[InstantRespawn] PlayerImpact null.");
                    return;
                }

                MethodInfo respawn = pii.GetType().GetMethod(
                    "RespawnOnTrack",
                    BindingFlags.Public | BindingFlags.Instance,
                    null, new System.Type[] { typeof(bool) }, null);
                if ((object)respawn == null)
                {
                    ModLog.Warn("[InstantRespawn] RespawnOnTrack not found.");
                    return;
                }

                respawn.Invoke(pii, new object[] { true });
                ModLog.Debug("[InstantRespawn] Respawned.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[InstantRespawn] OnBail: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "InstantRespawn");
            }
        }

        public static void Reset()
        {
            Enabled = false;
        }
    }
}

