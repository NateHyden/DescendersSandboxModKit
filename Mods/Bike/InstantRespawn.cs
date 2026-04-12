using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Calls RespawnOnTrack(true) immediately on bail — skips the countdown entirely.
    // Triggered via SessionTrackers.OnBailDetected() (already debounced 1s).
    // Uses the same PlayerManager.GetPlayerImpact() pattern as TeleportToCheckpoint.
    public static class InstantRespawn
    {
        public static bool Enabled { get; private set; } = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            MelonLogger.Msg("[InstantRespawn] -> " + (Enabled ? "ON" : "OFF"));
        }

        // Called by SessionTrackers.OnBailDetected()
        public static void OnBail()
        {
            if (!Enabled) return;
            try
            {
                PlayerManager pm = Object.FindObjectOfType<PlayerManager>();
                if ((object)pm == null)
                {
                    MelonLogger.Warning("[InstantRespawn] PlayerManager not found.");
                    return;
                }

                MethodInfo getPii = typeof(PlayerManager).GetMethod(
                    "GetPlayerImpact", BindingFlags.Public | BindingFlags.Instance);
                if ((object)getPii == null)
                {
                    MelonLogger.Warning("[InstantRespawn] GetPlayerImpact not found.");
                    return;
                }

                object pii = getPii.Invoke(pm, null);
                if ((object)pii == null)
                {
                    MelonLogger.Warning("[InstantRespawn] PlayerImpact null.");
                    return;
                }

                MethodInfo respawn = pii.GetType().GetMethod(
                    "RespawnOnTrack",
                    BindingFlags.Public | BindingFlags.Instance,
                    null, new System.Type[] { typeof(bool) }, null);
                if ((object)respawn == null)
                {
                    MelonLogger.Warning("[InstantRespawn] RespawnOnTrack not found.");
                    return;
                }

                respawn.Invoke(pii, new object[] { true });
                MelonLogger.Msg("[InstantRespawn] Respawned.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[InstantRespawn] OnBail: " + ex.Message);
            }
        }

        public static void Reset()
        {
            Enabled = false;
        }
    }
}
