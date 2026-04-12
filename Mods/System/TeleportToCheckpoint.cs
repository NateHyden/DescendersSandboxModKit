using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class TeleportToCheckpoint
    {
        public static void Teleport()
        {
            try
            {
                // Use RespawnOnTrack(true) — works in all modes including freeride.
                // The game calls this on bail — it matches checkpoint by terrain index,
                // so no race route is needed.
                PlayerManager pm = UnityEngine.Object.FindObjectOfType<PlayerManager>();
                if ((object)pm == null) { MelonLogger.Warning("[TeleportCP] PlayerManager not found."); return; }

                var getPii = typeof(PlayerManager).GetMethod("GetPlayerImpact",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)getPii == null) { MelonLogger.Warning("[TeleportCP] GetPlayerImpact not found."); return; }

                object pii = getPii.Invoke(pm, null);
                if ((object)pii == null) { MelonLogger.Warning("[TeleportCP] PlayerImpact null."); return; }

                var respawn = pii.GetType().GetMethod("RespawnOnTrack",
                    BindingFlags.Public | BindingFlags.Instance, null,
                    new System.Type[] { typeof(bool) }, null);
                if ((object)respawn == null) { MelonLogger.Warning("[TeleportCP] RespawnOnTrack not found."); return; }

                respawn.Invoke(pii, new object[] { true }); // true = skip CanRespawn check
                MelonLogger.Msg("[TeleportCP] RespawnOnTrack called.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[TeleportCP] Teleport: " + ex.Message); }
        }

        // ── Checkpoint count ─────────────────────────────────────────
        public static int CheckpointCount
        {
            get
            {
                try
                {
                    var fld = typeof(Checkpoint).GetField(
                        "b]sfXb",
                        BindingFlags.Public | BindingFlags.Static);
                    if ((object)fld == null) return 0;
                    var list = fld.GetValue(null) as System.Collections.Generic.List<Checkpoint>;
                    return (object)list != null ? list.Count : 0;
                }
                catch { return 0; }
            }
        }

        // ── Teleport to checkpoint by index ──────────────────────────
        public static void TeleportByIndex(int index)
        {
            try
            {
                var fld = typeof(Checkpoint).GetField(
                    "b]sfXb",
                    BindingFlags.Public | BindingFlags.Static);
                if ((object)fld == null) { MelonLogger.Warning("[TeleportCP] Checkpoint list field not found."); return; }
                var list = fld.GetValue(null) as System.Collections.Generic.List<Checkpoint>;
                if ((object)list == null || list.Count == 0) { MelonLogger.Warning("[TeleportCP] No checkpoints in list."); return; }
                index = UnityEngine.Mathf.Clamp(index, 0, list.Count - 1);
                Checkpoint cp = list[index];
                if ((object)cp == null) { MelonLogger.Warning("[TeleportCP] Checkpoint at index " + index + " is null."); return; }

                GameObject local = GameObject.Find("Player_Human");
                if ((object)local == null) { MelonLogger.Warning("[TeleportCP] Player_Human not found."); return; }

                Vehicle vehicle = local.GetComponent<Vehicle>();
                Vector3 dest = cp.transform.position + cp.transform.up * 1.5f;
                local.transform.position = dest;

                if ((object)vehicle != null)
                {
                    Rigidbody rb = vehicle.GetComponent<Rigidbody>();
                    if ((object)rb == null) rb = vehicle.GetComponentInChildren<Rigidbody>();
                    if ((object)rb != null)
                    {
                        rb.position = dest;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    try
                    {
                        MethodInfo resetMethod = vehicle.GetType().GetMethod("Reset",
                            BindingFlags.Public | BindingFlags.Instance, null,
                            new System.Type[] { typeof(bool) }, null);
                        if ((object)resetMethod != null)
                            resetMethod.Invoke(vehicle, new object[] { false });
                    }
                    catch { }
                }
                MelonLogger.Msg("[TeleportCP] Teleported to checkpoint #" + index + " at " + dest);
            }
            catch (System.Exception ex) { MelonLogger.Error("[TeleportCP] TeleportByIndex: " + ex.Message); }
        }
    }
}