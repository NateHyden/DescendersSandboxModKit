using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class TeleportToPlayer
    {
        public class PlayerEntry
        {
            public string Name;
            public GameObject Root;

            public PlayerEntry(string name, GameObject root)
            {
                Name = name;
                Root = root;
            }
        }

        private static readonly string IoTpiSFieldName = "\u0080ioTpiS";
        private static readonly string PlayerNameField = "laxjiuc";

        private static FieldInfo _ioTpiSField = null;
        private static FieldInfo _nameField = null;

        public static List<PlayerEntry> ScanForPlayers()
        {
            List<PlayerEntry> results = new List<PlayerEntry>();

            try
            {
                Vehicle[] vehicles = UnityEngine.Object.FindObjectsOfType<Vehicle>();

                for (int i = 0; i < vehicles.Length; i++)
                {
                    Vehicle v = vehicles[i];
                    if ((object)v == null) continue;

                    GameObject root = v.gameObject;
                    if ((object)root == null || !root.activeInHierarchy) continue;

                    if (string.Equals(root.name, "Player_Human", StringComparison.Ordinal))
                        continue;

                    string name = GetPlayerName(v, i);
                    results.Add(new PlayerEntry(name, root));
                }

                MelonLogger.Msg("[TeleportToPlayer] Found " + results.Count + " player(s).");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[TeleportToPlayer] ScanForPlayers failed: " + ex.Message);
            }

            return results;
        }

        public static bool TeleportTo(PlayerEntry entry)
        {
            if ((object)entry == null)
            {
                MelonLogger.Warning("[TeleportToPlayer] Entry is null.");
                return false;
            }

            try
            {
                // Re-scan fresh so we always get the current live position
                // Match by name to find the right player
                List<PlayerEntry> fresh = ScanForPlayers();
                PlayerEntry target = null;

                for (int i = 0; i < fresh.Count; i++)
                {
                    if (string.Equals(fresh[i].Name, entry.Name, StringComparison.Ordinal))
                    {
                        target = fresh[i];
                        break;
                    }
                }

                // Fall back to the cached root if name match fails
                if ((object)target == null)
                {
                    MelonLogger.Warning("[TeleportToPlayer] Could not re-find " + entry.Name + " - using cached position.");
                    target = entry;
                }

                if ((object)target.Root == null)
                {
                    MelonLogger.Warning("[TeleportToPlayer] Target root is null.");
                    return false;
                }

                Vector3 dest = target.Root.transform.position + Vector3.up * 1.5f;

                GameObject local = GameObject.Find("Player_Human");
                if ((object)local == null)
                {
                    MelonLogger.Warning("[TeleportToPlayer] Player_Human not found.");
                    return false;
                }

                Vehicle vehicle = local.GetComponent<Vehicle>();
                if ((object)vehicle != null)
                {
                    // Move the transform first
                    local.transform.position = dest;

                    // Zero out the rigidbody so physics doesn't launch us
                    Rigidbody rb = vehicle.GetComponent<Rigidbody>();
                    if ((object)rb == null)
                        rb = vehicle.GetComponentInChildren<Rigidbody>();

                    if ((object)rb != null)
                    {
                        rb.position = dest;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    // Call Vehicle.Reset(true) - same as the game uses for respawning
                    // This clears all physics state, tricks, bail flags cleanly
                    try
                    {
                        System.Reflection.MethodInfo resetMethod = vehicle.GetType().GetMethod(
                            "Reset",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                            null,
                            new System.Type[] { typeof(bool) },
                            null
                        );
                        if ((object)resetMethod != null)
                            resetMethod.Invoke(vehicle, new object[] { false });
                    }
                    catch { }

                    MelonLogger.Msg("[TeleportToPlayer] Teleported to " + target.Name + " at " + dest);
                    return true;
                }

                local.transform.position = dest;
                MelonLogger.Msg("[TeleportToPlayer] Teleported to " + target.Name + " (transform) at " + dest);
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[TeleportToPlayer] TeleportTo failed: " + ex.Message);
                return false;
            }
        }

        private static string GetPlayerName(Vehicle vehicle, int fallbackIndex)
        {
            try
            {
                if ((object)_ioTpiSField == null)
                {
                    FieldInfo[] fields = vehicle.GetType().GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.FlattenHierarchy
                    );
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (string.Equals(fields[i].Name, IoTpiSFieldName, StringComparison.Ordinal))
                        {
                            _ioTpiSField = fields[i];
                            break;
                        }
                    }
                }

                if ((object)_ioTpiSField == null) return "Player " + (fallbackIndex + 1);

                object playerInfo = _ioTpiSField.GetValue(vehicle);
                if ((object)playerInfo == null) return "Player " + (fallbackIndex + 1);

                if ((object)_nameField == null)
                {
                    System.Type t = playerInfo.GetType();
                    while ((object)t != null)
                    {
                        FieldInfo[] fields = t.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.DeclaredOnly
                        );
                        bool found = false;
                        for (int i = 0; i < fields.Length; i++)
                        {
                            if (string.Equals(fields[i].Name, PlayerNameField, StringComparison.Ordinal))
                            {
                                _nameField = fields[i];
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                        t = t.BaseType;
                    }
                }

                if ((object)_nameField == null) return "Player " + (fallbackIndex + 1);

                object photonPlayer = _nameField.GetValue(playerInfo);
                if ((object)photonPlayer == null) return "Player " + (fallbackIndex + 1);

                string name = photonPlayer.ToString();
                if (!string.IsNullOrEmpty(name)) return name;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[TeleportToPlayer] GetPlayerName failed: " + ex.Message);
            }

            return "Player " + (fallbackIndex + 1);
        }
    }
}