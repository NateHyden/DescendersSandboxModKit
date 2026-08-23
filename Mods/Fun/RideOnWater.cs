using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Stops DeathVolume kill zones (water, voids, etc.) from auto-bailing,
    /// and turns their trigger colliders solid so you can ride on them.
    /// </summary>
    public static class RideOnWater
    {
        public static bool Enabled { get; private set; }

        private const float RescanInterval = 2f;

        private static readonly Dictionary<int, bool> _savedIsTrigger =
            new Dictionary<int, bool>();

        private static float _nextRescanTime;

        public static void Toggle()
        {
            SetEnabled(!Enabled);
            ModLog.Feedback("[RideOnWater] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void SetEnabled(bool enabled)
        {
            if (Enabled == enabled) return;
            Enabled = enabled;
            if (Enabled)
            {
                ApplySolids();
                _nextRescanTime = Time.unscaledTime + RescanInterval;
            }
            else
            {
                RestoreSolids();
            }
        }

        public static void Reset()
        {
            if (Enabled)
                SetEnabled(false);
            else
                _savedIsTrigger.Clear();
        }

        public static void OnSceneInitialized()
        {
            _savedIsTrigger.Clear();
            if (!Enabled) return;
            ApplySolids();
            _nextRescanTime = Time.unscaledTime + RescanInterval;
        }

        public static void OnSceneUnloaded()
        {
            // Objects are destroyed with the scene; drop handles without restore.
            _savedIsTrigger.Clear();
        }

        public static void Tick()
        {
            if (!Enabled) return;
            if (Time.unscaledTime < _nextRescanTime) return;
            _nextRescanTime = Time.unscaledTime + RescanInterval;
            ApplySolids();
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo enter = typeof(DeathVolume).GetMethod(
                    "OnTriggerEnter",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new System.Type[] { typeof(Collider) },
                    null);

                if ((object)enter == null)
                {
                    ModLog.Warn("[RideOnWater] DeathVolume.OnTriggerEnter not found.");
                    DiagnosticsManager.Report("RideOnWater", false, "OnTriggerEnter missing");
                    return;
                }

                MethodInfo prefix = typeof(RideOnWater_Patch).GetMethod(
                    "Prefix", BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(enter, prefix: new HarmonyMethod(prefix));
                ModLog.Debug("[RideOnWater] Patched DeathVolume.OnTriggerEnter.");
                DiagnosticsManager.Report("RideOnWater", true);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[RideOnWater] ApplyPatch: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "RideOnWater");
                DiagnosticsManager.Report("RideOnWater", false, ex.Message);
            }
        }

        private static void ApplySolids()
        {
            try
            {
                DeathVolume[] volumes = Object.FindObjectsOfType<DeathVolume>();
                for (int i = 0; i < volumes.Length; i++)
                {
                    DeathVolume vol = volumes[i];
                    if (!UnityNull.Alive(vol)) continue;

                    Collider[] cols = vol.GetComponentsInChildren<Collider>(true);
                    for (int c = 0; c < cols.Length; c++)
                    {
                        Collider col = cols[c];
                        if (!UnityNull.Alive(col)) continue;

                        int id = col.GetInstanceID();
                        if (!_savedIsTrigger.ContainsKey(id))
                            _savedIsTrigger[id] = col.isTrigger;

                        if (col.isTrigger)
                            col.isTrigger = false;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[RideOnWater] ApplySolids: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "RideOnWater");
            }
        }

        private static void RestoreSolids()
        {
            try
            {
                DeathVolume[] volumes = Object.FindObjectsOfType<DeathVolume>();
                for (int i = 0; i < volumes.Length; i++)
                {
                    DeathVolume vol = volumes[i];
                    if (!UnityNull.Alive(vol)) continue;

                    Collider[] cols = vol.GetComponentsInChildren<Collider>(true);
                    for (int c = 0; c < cols.Length; c++)
                    {
                        Collider col = cols[c];
                        if (!UnityNull.Alive(col)) continue;

                        int id = col.GetInstanceID();
                        bool wasTrigger;
                        if (_savedIsTrigger.TryGetValue(id, out wasTrigger))
                            col.isTrigger = wasTrigger;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[RideOnWater] RestoreSolids: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "RideOnWater");
            }
            finally
            {
                _savedIsTrigger.Clear();
            }
        }
    }

    public static class RideOnWater_Patch
    {
        public static bool Prefix()
        {
            return !RideOnWater.Enabled;
        }
    }
}
