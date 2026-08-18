using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using DescendersModMenu; // Telemetry

namespace DescendersModMenu.Mods
{
    // Ice Grip: zero wheel roll friction + ground grip so the bike slides on
    // contact. Do NOT zero Rigidbody.angularDrag — that made mid-air spins /
    // flips integrate forever and runaway to ridiculous speeds.
    public static class IceMode
    {
        public static bool Enabled { get; private set; } = false;

        // Soft air safety: if angular speed somehow spikes while ice is on,
        // clamp it. Does not fight normal trick spins under this cap.
        private const float MaxAirAngularSpeed = 14f; // rad/s ≈ 800 deg/s

        private static PropertyInfo _vehicleGroundedProp = null;

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("[IceMode] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void Reset()
        {
            Enabled = false;
            _vehicleGroundedProp = null;
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo wheelFU = typeof(Wheel).GetMethod(
                    "FixedUpdate", BindingFlags.Public | BindingFlags.Instance);

                if ((object)wheelFU != null)
                {
                    harmony.Patch(wheelFU, postfix: new HarmonyMethod(
                        typeof(IceMode_WheelPatch).GetMethod("Postfix", BindingFlags.Public | BindingFlags.Static)));
                    ModLog.Debug("[IceMode] Patched Wheel.FixedUpdate.");
                }
                else
                    ModLog.Warn("[IceMode] Wheel.FixedUpdate not found.");

                MethodInfo vehicleFU = typeof(Vehicle).GetMethod(
                    "FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)vehicleFU != null)
                {
                    harmony.Patch(vehicleFU, postfix: new HarmonyMethod(
                        typeof(IceMode_VehiclePatch).GetMethod("Postfix", BindingFlags.Public | BindingFlags.Static)));
                    ModLog.Debug("[IceMode] Patched Vehicle.FixedUpdate.");
                }
                else
                    ModLog.Warn("[IceMode] Vehicle.FixedUpdate not found.");

                DiagnosticsManager.Report("IceMode", true);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[IceMode] ApplyPatch: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "IceMode");
                DiagnosticsManager.Report("IceMode", false, ex.Message);
            }
        }

        internal static bool IsVehicleAirborne(Vehicle vehicle)
        {
            try
            {
                if ((object)_vehicleGroundedProp == null)
                {
                    PropertyInfo[] props = typeof(Vehicle).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (!props[i].CanRead) continue;
                        if (props[i].PropertyType.Equals(typeof(bool)) && props[i].Name.StartsWith("T"))
                        {
                            _vehicleGroundedProp = props[i];
                            break;
                        }
                    }
                }
                if ((object)_vehicleGroundedProp == null) return false;
                return !(bool)_vehicleGroundedProp.GetValue(vehicle, null);
            }
            catch { return false; }
        }

        internal static void ClampAirSpin(Vehicle vehicle)
        {
            if (!UnityNull.Alive(vehicle)) return;
            if (!IsVehicleAirborne(vehicle)) return;
            try
            {
                Rigidbody rb = vehicle.GetComponent<Rigidbody>();
                if (!UnityNull.Alive(rb)) return;
                float mag = rb.angularVelocity.magnitude;
                if (mag > MaxAirAngularSpeed)
                    rb.angularVelocity = rb.angularVelocity * (MaxAirAngularSpeed / mag);
            }
            catch { }
        }
    }

    public static class IceMode_WheelPatch
    {
        // WbmnXfG = rollFriction property on Wheel
        private static PropertyInfo _rollFrictionProp = null;

        public static void Postfix(Wheel __instance)
        {
            if (!IceMode.Enabled) return;
            if (!UnityNull.Alive(__instance)) return;

            try
            {
                Transform t = __instance.transform;
                if (!UnityNull.Alive(t) || !UnityNull.Alive(t.parent)) return;
                if (!string.Equals(t.parent.name, "Player_Human", System.StringComparison.Ordinal)) return;

                if ((object)_rollFrictionProp == null)
                    _rollFrictionProp = typeof(Wheel).GetProperty(
                        "WbmnXfG", BindingFlags.Public | BindingFlags.Instance);

                if ((object)_rollFrictionProp != null)
                    _rollFrictionProp.SetValue(__instance, 0.0f, null);
            }
            catch { }
        }
    }

    public static class IceMode_VehiclePatch
    {
        // n\u0080jDpmV = actual ground grip (public property on Vehicle)
        // eSXpeQc gets overwritten inside FixedUpdate before our postfix.
        private static PropertyInfo _groundGripProp = null;

        public static void Postfix(Vehicle __instance)
        {
            if (!IceMode.Enabled) return;
            if (!UnityNull.Alive(__instance)) return;

            try
            {
                if (!string.Equals(__instance.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                if ((object)_groundGripProp == null)
                {
                    // Prefer exact unicode name; fall back to scan (mojibake-safe).
                    _groundGripProp = typeof(Vehicle).GetProperty(
                        "n\u0080jDpmV", BindingFlags.Public | BindingFlags.Instance);
                    if ((object)_groundGripProp == null)
                    {
                        PropertyInfo[] props = typeof(Vehicle).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        for (int i = 0; i < props.Length; i++)
                        {
                            if (!props[i].CanWrite) continue;
                            if (!props[i].PropertyType.Equals(typeof(float))) continue;
                            string n = props[i].Name;
                            if (n != null && n.Length >= 6 && n[0] == 'n' && n.IndexOf("jDpm") >= 0)
                            { _groundGripProp = props[i]; break; }
                        }
                    }
                    if ((object)_groundGripProp != null)
                        ModLog.Debug("[IceMode] Found ground grip prop: " + _groundGripProp.Name);
                    else
                        ModLog.Warn("[IceMode] Could not find ground grip prop.");
                }

                if ((object)_groundGripProp != null)
                    _groundGripProp.SetValue(__instance, 0.0f, null);

                IceMode.ClampAirSpin(__instance);
            }
            catch { }
        }
    }
}
