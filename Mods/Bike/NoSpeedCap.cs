using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class NoSpeedCap
    {
        public static bool Enabled { get; private set; } = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            MelonLogger.Msg("NoSpeedCap -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void SetEnabled(bool enabled)
        {
            Enabled = enabled;
        }

        // Also patch VehicleController.FixedUpdate which zeros j[fCiJt when speed > 55
        public static void ApplyVCPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                System.Type vcType = typeof(VehicleController);
                MethodInfo fixedUpdate = vcType.GetMethod(
                    "FixedUpdate",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if ((object)fixedUpdate == null)
                {
                    MelonLogger.Warning("[NoSpeedCap] Could not find VehicleController.FixedUpdate.");
                    return;
                }

                MethodInfo postfix = typeof(NoSpeedCap_VCPatch).GetMethod(
                    "Postfix", BindingFlags.Public | BindingFlags.Static
                );

                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(postfix));
                MelonLogger.Msg("[NoSpeedCap] Patched VehicleController.FixedUpdate.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[NoSpeedCap] VC Patch failed: " + ex.Message);
            }
        }

        // Manual patch since E{Kza has unprintable chars in its name
        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                // Find E{Kza by scanning all private methods on Vehicle
                // It's the one that takes no params and contains the speed cap logic
                System.Type vehicleType = typeof(Vehicle);
                MethodInfo[] methods = vehicleType.GetMethods(
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                MethodInfo target = null;
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo m = methods[i];
                    // E{Kza starts with 'E' and has no parameters
                    if (m.GetParameters().Length != 0) continue;
                    if (!m.ReturnType.Equals(typeof(void))) continue;
                    if (!m.Name.StartsWith("E")) continue;
                    // It's not a Unity event method
                    if (m.Name == "enabled") continue;
                    // Check it's not a property getter
                    if (m.IsSpecialName) continue;


                    // The real E{Kza has 3 chars before 'Kza': E + 2 non-ascii
                    if (m.Name.Length == 7 && m.Name.EndsWith("Kza"))
                    {
                        target = m;
                        break;
                    }
                }

                if ((object)target == null)
                {
                    MelonLogger.Warning("[NoSpeedCap] Could not find E{Kza method.");
                    return;
                }

                MethodInfo prefix = typeof(NoSpeedCap_EKzaPatch).GetMethod(
                    "Prefix",
                    BindingFlags.Public | BindingFlags.Static
                );
                MethodInfo postfix = typeof(NoSpeedCap_EKzaPatch).GetMethod(
                    "Postfix",
                    BindingFlags.Public | BindingFlags.Static
                );

                harmony.Patch(target,
                    prefix: new HarmonyMethod(prefix),
                    postfix: new HarmonyMethod(postfix));
                MelonLogger.Msg("[NoSpeedCap] Patched successfully.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[NoSpeedCap] Patch failed: " + ex.Message);
            }
        }
    }

    // Postfix on VehicleController.FixedUpdate
    // Restores j[fCiJt (inputAcceleration) after the game zeros it at speed > 55
    public static class NoSpeedCap_VCPatch
    {
        private static FieldInfo _vehicleField = null; // CDVkgio - the Vehicle ref
        private static PropertyInfo _tiltProp = null; // dyEyUZ - raw tilt input
        private static PropertyInfo _inputAccProp = null; // j[fCiJt - inputAcceleration
        private static bool _vcFieldsCached = false;

        public static void Postfix(VehicleController __instance)
        {
            if (!NoSpeedCap.Enabled) return;
            if ((object)__instance == null) return;

            // Cache fields
            if ((object)_vehicleField == null)
            {
                FieldInfo[] fields = __instance.GetType().GetFields(
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                for (int i = 0; i < fields.Length; i++)
                {
                    if (string.Equals(fields[i].FieldType.Name, "Vehicle",
                        System.StringComparison.Ordinal))
                    {
                        _vehicleField = fields[i];
                        break;
                    }
                }
            }

            if ((object)_vehicleField == null) return;

            Vehicle vehicle = _vehicleField.GetValue(__instance) as Vehicle;
            if ((object)vehicle == null) return;

            // Only for local player
            if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                System.StringComparison.Ordinal)) return;

            // Cache tilt and inputAccel props once
            if (!_vcFieldsCached)
            {
                _vcFieldsCached = true;
                PropertyInfo[] vcProps = __instance.GetType().GetProperties(
                    BindingFlags.Public | BindingFlags.Instance
                );
                for (int i = 0; i < vcProps.Length; i++)
                {
                    if (!vcProps[i].CanRead) continue;
                    if (!string.Equals(vcProps[i].PropertyType.Name, "Single",
                        System.StringComparison.Ordinal)) continue;
                    if ((object)_tiltProp == null &&
                        vcProps[i].Name.StartsWith("d") && vcProps[i].Name.Length > 4)
                    {
                        _tiltProp = vcProps[i];
                    }
                }
            }

            float tiltInput = 0f;
            if ((object)_tiltProp != null)
                tiltInput = (float)_tiltProp.GetValue(__instance, null);
            if (tiltInput <= 0.01f) return; // not pedaling, leave j[fCiJt at 0

            // Player is pedaling but game zeroed input because GetVelocity() > 55
            // Find j[fCiJt and restore it to the tilt value so speed control works normally
            if ((object)_inputAccProp == null)
            {
                PropertyInfo[] allProps = vehicle.GetType().GetProperties(
                    BindingFlags.Public | BindingFlags.Instance
                );
                for (int i = 0; i < allProps.Length; i++)
                {
                    if (!allProps[i].CanWrite || !allProps[i].CanRead) continue;
                    if (!string.Equals(allProps[i].PropertyType.Name, "Single",
                        System.StringComparison.Ordinal)) continue;
                    if (!allProps[i].Name.StartsWith("j")) continue;
                    _inputAccProp = allProps[i];
                    break;
                }
            }

            if ((object)_inputAccProp != null)
            {
                float current = (float)_inputAccProp.GetValue(vehicle, null);
                if (Mathf.Approximately(current, 0f) && vehicle.GetVelocity() > 55f)
                    _inputAccProp.SetValue(vehicle, tiltInput, null);
            }
        }
    }

    public static class NoSpeedCap_EKzaPatch
    {
        // Cache fields via reflection
        private static FieldInfo _rbField = null; // Rigidbody
        private static bool _fieldsCached = false;

        // State passed from Prefix to Postfix
        private static bool _active = false;
        private static Vector3 _savedVelocity;

        // ── Prefix: zero the velocity so E{Kza's speed cap (num2) sees speed=0
        //    and applies FULL acceleration. Then Postfix restores real velocity.
        public static void Prefix(Vehicle __instance)
        {
            _active = false;
            if (!NoSpeedCap.Enabled) return;
            if ((object)__instance == null) return;
            if (!string.Equals(__instance.gameObject.name, "Player_Human",
                System.StringComparison.Ordinal)) return;

            EnsureFields(__instance);

            Rigidbody rb = null;
            if ((object)_rbField != null)
                rb = _rbField.GetValue(__instance) as Rigidbody;
            if ((object)rb == null) return;

            // Save real velocity and zero it so E{Kza thinks speed=0
            _savedVelocity = rb.velocity;
            rb.velocity = Vector3.zero;
            _active = true;
        }

        // ── Postfix: E{Kza ran with velocity=0, so it applied full uncapped
        //    acceleration to "zero". Now velocity = just the acceleration delta.
        //    Add that delta to the real velocity we saved.
        public static void Postfix(Vehicle __instance)
        {
            if (!_active) return;
            _active = false;

            Rigidbody rb = null;
            if ((object)_rbField != null)
                rb = _rbField.GetValue(__instance) as Rigidbody;
            if ((object)rb == null) return;

            Vector3 accelDelta = rb.velocity; // what E{Kza added to "zero"
            rb.velocity = _savedVelocity + accelDelta; // real velocity + uncapped accel
        }

        private static void EnsureFields(Vehicle v)
        {
            if (_fieldsCached) return;
            _fieldsCached = true;

            FieldInfo[] fields = v.GetType().GetFields(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.FlattenHierarchy
            );
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i].FieldType.Name, "Rigidbody",
                    System.StringComparison.Ordinal) &&
                    fields[i].Name.IndexOf("BackingField",
                    System.StringComparison.Ordinal) >= 0)
                {
                    _rbField = fields[i];
                    break;
                }
            }
        }
    }
}