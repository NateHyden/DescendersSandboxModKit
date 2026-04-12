using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class QuickBrake
    {
        public static bool Enabled { get; private set; } = false;
        public static int Level { get; private set; } = 5;

        // Old L3 values (1.3x multiplier, 6f drag) are now the L10 ceiling.
        // L1 is very gentle (1.03x, 0 drag), scaling smoothly up to L9.
        // L10 = instant (zero velocity).
        //
        // Multiplier: L1=1.03, L9=1.27, L10=instant
        // Extra drag: L1=0, L9=5.3f, L10=instant
        public static float GetMultiplier() { return Level < 10 ? 1.03f + (Level - 1) * 0.027f : 1.3f; }
        public static float GetDrag() { return Level < 10 ? (Level - 1) * 0.67f : 200f; }

        public static void Toggle()
        {
            Enabled = !Enabled;
            MelonLogger.Msg("[QuickBrake] -> " + (Enabled ? "ON (level " + Level + ")" : "OFF"));
        }

        public static void Increase() { if (Level < 10) Level++; }
        public static void Decrease() { if (Level > 1) Level--; }
        public static void SetLevel(int v) { Level = System.Math.Max(1, System.Math.Min(10, v)); }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo fixedUpdate = typeof(VehicleController).GetMethod(
                    "FixedUpdate",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)fixedUpdate == null)
                { MelonLogger.Warning("[QuickBrake] VehicleController.FixedUpdate not found."); return; }

                MethodInfo postfix = typeof(QuickBrake_Patch).GetMethod(
                    "Postfix", BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(postfix));
                MelonLogger.Msg("[QuickBrake] Patched VehicleController.FixedUpdate.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[QuickBrake] ApplyPatch: " + ex.Message); }
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
                // Resolve Vehicle field once
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
                if ((object)vehicle == null) return;

                if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                // Not braking — restore drag and do nothing
                if (vehicle.NYsPlot <= 0f)
                {
                    if ((object)_rb != null) _rb.drag = _origDrag;
                    return;
                }

                // Resolve Rigidbody once
                if ((object)_rb == null)
                {
                    PropertyInfo[] props = typeof(Vehicle).GetProperties(
                        BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (props[i].CanRead && string.Equals(props[i].PropertyType.Name,
                            "Rigidbody", System.StringComparison.Ordinal))
                        { _rb = props[i].GetValue(vehicle, null) as Rigidbody; break; }
                    }
                    if ((object)_rb != null) _origDrag = _rb.drag;
                }

                // Scale brake force by level — L1 barely touches it, L9 maxes it
                vehicle.NYsPlot = Mathf.Clamp(vehicle.NYsPlot * QuickBrake.GetMultiplier(), 0f, 1f);

                if ((object)_rb == null) return;

                if (QuickBrake.Level >= 10)
                {
                    // Instant — zero everything
                    _rb.velocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                    _rb.drag = 200f;
                }
                else
                {
                    // Extra drag scales with level — 0 at L1, 27f at L9
                    _rb.drag = _origDrag + QuickBrake.GetDrag();
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[QuickBrake] Postfix: " + ex.Message);
                _vehicleField = null; _rb = null;
            }
        }

        public static void ClearCache()
        {
            try { if ((object)_rb != null) _rb.drag = _origDrag; } catch { }
            _vehicleField = null;
            _rb = null;
            _origDrag = 0f;
        }
    }
}