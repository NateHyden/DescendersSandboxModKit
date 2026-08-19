using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    public static class RubberBandSteering
    {
        public static bool Enabled { get; private set; } = false;
        public static int Level { get; private set; } = 5;

        private static float DelayForLevel(int level) { return level * 0.05f; }
        public static string LevelDisplay => (DelayForLevel(Level) * 1000f).ToString("0") + "ms";

        public static void Toggle()
        {
            Enabled = !Enabled;
            _buffer.Clear();
            ModLog.Feedback("[RubberBandSteering] -> " + (Enabled ? "ON " + LevelDisplay : "OFF"));
        }

        public static void Increase()
        {
            if (Level < 10) { Level++; ModLog.Feedback("[RubberBandSteering] Level -> " + Level + " (" + LevelDisplay + ")"); }
        }
        public static void Decrease()
        {
            if (Level > 1) { Level--; ModLog.Feedback("[RubberBandSteering] Level -> " + Level + " (" + LevelDisplay + ")"); }
        }
        public static void SetLevel(int v) { Level = Mathf.Clamp(v, 1, 10); }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo original = typeof(VehicleController).GetMethod(
                    "FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)original == null)
                {
                    ModLog.Warn("[RubberBandSteering] VehicleController.FixedUpdate not found.");
                    return;
                }

                MethodInfo postfix = typeof(RubberBandSteering_Patch).GetMethod(
                    "Postfix", BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
                ModLog.Debug("[RubberBandSteering] Patched VehicleController.FixedUpdate.");
                DiagnosticsManager.Report("RubberBandSteering", true);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[RubberBandSteering] ApplyPatch: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "RubberBandSteering");
                DiagnosticsManager.Report("RubberBandSteering", false, ex.Message);
            }
        }

        public static void ClearCache() { _buffer.Clear(); }

        public static void Reset()
        {
            Enabled = false;
            _buffer.Clear();
        }

        private struct Sample { public float t; public float steer; public float lean; }
        private static readonly List<Sample> _buffer = new List<Sample>(64);
        private const float MaxHistorySeconds = 0.7f;

        internal static void PushAndApply(Vehicle vehicle, PropertyInfo steerProp, PropertyInfo leanProp)
        {
            float now = Time.time;
            float steer = (object)steerProp != null ? (float)steerProp.GetValue(vehicle, null) : 0f;
            float lean = (object)leanProp != null ? (float)leanProp.GetValue(vehicle, null) : 0f;

            _buffer.Add(new Sample { t = now, steer = steer, lean = lean });

            int trim = 0;
            while (trim < _buffer.Count && _buffer[trim].t < now - MaxHistorySeconds) trim++;
            if (trim > 0) _buffer.RemoveRange(0, trim);

            float target = now - DelayForLevel(Level);

            Sample prev = _buffer[0];
            Sample outSample = prev;
            bool bracketed = false;
            for (int i = 0; i < _buffer.Count; i++)
            {
                if (_buffer[i].t <= target) { prev = _buffer[i]; continue; }
                Sample next = _buffer[i];
                float span = next.t - prev.t;
                float frac = span > 0.0001f ? (target - prev.t) / span : 0f;
                outSample.steer = Mathf.Lerp(prev.steer, next.steer, frac);
                outSample.lean = Mathf.Lerp(prev.lean, next.lean, frac);
                bracketed = true;
                break;
            }
            if (!bracketed) outSample = prev;

            if ((object)steerProp != null) steerProp.SetValue(vehicle, outSample.steer, null);
            if ((object)leanProp != null) leanProp.SetValue(vehicle, outSample.lean, null);
        }
    }

    public static class RubberBandSteering_Patch
    {
        private static FieldInfo _vehicleField = null;

        private static PropertyInfo _steerProp = null;
        private static PropertyInfo _leanProp = null;
        private static readonly string SteerPropName = "swebLyg";
        private static readonly string LeanPropName = "c\u007Bv\u007DlhG";

        public static void Postfix(VehicleController __instance)
        {
            if (!RubberBandSteering.Enabled) return;
            if (!UnityNull.Alive(__instance)) return;

            try
            {
                if ((object)_vehicleField == null)
                {
                    FieldInfo[] fields = typeof(VehicleController).GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (string.Equals(fields[i].FieldType.Name, "Vehicle",
                            System.StringComparison.Ordinal))
                        {
                            _vehicleField = fields[i];
                            ModLog.Debug("[RubberBandSteering] Found Vehicle field: " + fields[i].Name);
                            break;
                        }
                    }
                    if ((object)_vehicleField == null)
                    {
                        ModLog.Warn("[RubberBandSteering] Could not find Vehicle field on VehicleController.");
                        return;
                    }
                }

                Vehicle vehicle = _vehicleField.GetValue(__instance) as Vehicle;
                if (!UnityNull.Alive(vehicle)) return;

                if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                if ((object)_steerProp == null)
                {
                    _steerProp = typeof(Vehicle).GetProperty(SteerPropName, BindingFlags.Public | BindingFlags.Instance);
                    if ((object)_steerProp != null)
                        ModLog.Debug("[RubberBandSteering] Found steer property: " + SteerPropName);
                    else
                        ModLog.Warn("[RubberBandSteering] Could not find steer property: " + SteerPropName);
                }
                if ((object)_leanProp == null)
                {
                    _leanProp = typeof(Vehicle).GetProperty(LeanPropName, BindingFlags.Public | BindingFlags.Instance);
                    if ((object)_leanProp != null)
                        ModLog.Debug("[RubberBandSteering] Found lean property: " + LeanPropName);
                    else
                        ModLog.Warn("[RubberBandSteering] Could not find lean property: " + LeanPropName);
                }
                if ((object)_steerProp == null && (object)_leanProp == null) return;

                RubberBandSteering.PushAndApply(vehicle, _steerProp, _leanProp);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[RubberBandSteering] Postfix error: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "RubberBandSteering");
            }
        }
    }
}

