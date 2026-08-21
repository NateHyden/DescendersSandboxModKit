using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    public static class QuickBrake
    {
        public static bool Enabled { get; private set; } = false;
        public static int Level { get; private set; } = 1;
        public const int MaxLevel = 3;

        // L1/L2 = mild assist only; L3 (MAX) = hard stop.
        public static float GetMultiplier()
        {
            if (Level <= 1) return 1.01f;
            if (Level == 2) return 1.02f;
            return 1.15f;
        }

        public static float GetDrag()
        {
            if (Level <= 1) return 1.5f;
            if (Level == 2) return 3.5f;
            return 200f;
        }

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("[QuickBrake] -> " + (Enabled ? "ON (level " + Level + ")" : "OFF"));
        }

        public static void Increase() { if (Level < MaxLevel) Level++; }
        public static void Decrease() { if (Level > 1) Level--; }
        public static void SetLevel(int v) { Level = System.Math.Max(1, System.Math.Min(MaxLevel, v)); }

        /// <summary>
        /// Load from bike-stats save. Clamp/migrate older higher caps down to 1–3.
        /// </summary>
        public static void SetLevelFromSave(int v)
        {
            if (v <= 0) v = 1;
            // Old default (5) / anything above new max → sensible mapping.
            if (v == 5) v = 1;
            else if (v > MaxLevel) v = MaxLevel;
            SetLevel(v);
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo fixedUpdate = typeof(VehicleController).GetMethod(
                    "FixedUpdate",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)fixedUpdate == null)
                { ModLog.Warn("[QuickBrake] VehicleController.FixedUpdate not found."); return; }

                MethodInfo postfix = typeof(QuickBrake_Patch).GetMethod(
                    "Postfix", BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(postfix));
                ModLog.Debug("[QuickBrake] Patched VehicleController.FixedUpdate.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[QuickBrake] ApplyPatch: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "QuickBrake"); }
        }

        public static void Reset() { Enabled = false; }
    }

    public static class QuickBrake_Patch
    {
        private static FieldInfo _vehicleField = null;
        private static Rigidbody _rb = null;
        private static float _origDrag = 0f;

        public static void Postfix(VehicleController __instance)
        {
            if (!QuickBrake.Enabled) return;
            if ((object)__instance == null) return;
            try
            {
                if ((object)_vehicleField == null)
                {
                    FieldInfo[] fields = __instance.GetType().GetFields(
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (string.Equals(fields[i].FieldType.Name, "Vehicle",
                            System.StringComparison.Ordinal))
                        { _vehicleField = fields[i]; break; }
                    }
                    if ((object)_vehicleField == null) return;
                }

                Vehicle vehicle = _vehicleField.GetValue(__instance) as Vehicle;
                if (!UnityNull.Alive(vehicle)) return;

                if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                if (vehicle.NYsPlot <= 0f)
                {
                    if (UnityNull.Alive(_rb)) _rb.drag = _origDrag;
                    return;
                }

                if (!UnityNull.Alive(_rb))
                {
                    _rb = null;
                    PropertyInfo[] props = typeof(Vehicle).GetProperties(
                        BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (props[i].CanRead && string.Equals(props[i].PropertyType.Name,
                            "Rigidbody", System.StringComparison.Ordinal))
                        { _rb = props[i].GetValue(vehicle, null) as Rigidbody; break; }
                    }
                    if (UnityNull.Alive(_rb)) _origDrag = _rb.drag;
                }

                vehicle.NYsPlot = Mathf.Clamp(vehicle.NYsPlot * QuickBrake.GetMultiplier(), 0f, 1f);

                if (!UnityNull.Alive(_rb)) return;

                if (QuickBrake.Level >= QuickBrake.MaxLevel)
                {
                    _rb.velocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                    _rb.drag = 200f;
                }
                else
                {
                    _rb.drag = _origDrag + QuickBrake.GetDrag();
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[QuickBrake] Postfix: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "QuickBrake");
                _vehicleField = null; _rb = null;
            }
        }

        public static void ClearCache()
        {
            try { if (UnityNull.Alive(_rb)) _rb.drag = _origDrag; } catch { }
            _vehicleField = null;
            _rb = null;
            _origDrag = 0f;
        }
    }
}

