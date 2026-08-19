using System.Reflection;
using HarmonyLib;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class NoSpeedCap
    {
        public static bool Enabled { get; private set; } = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("NoSpeedCap -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void SetEnabled(bool enabled)
        {
            Enabled = enabled;
        }

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
                    ModLog.Warn("[NoSpeedCap] Could not find VehicleController.FixedUpdate.");
                    return;
                }

                MethodInfo postfix = typeof(NoSpeedCap_VCPatch).GetMethod(
                    "Postfix", BindingFlags.Public | BindingFlags.Static
                );

                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(postfix));
                ModLog.Debug("[NoSpeedCap] Patched VehicleController.FixedUpdate.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[NoSpeedCap] VC Patch failed: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "NoSpeedCap");
            }
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                System.Type vehicleType = typeof(Vehicle);
                MethodInfo[] methods = vehicleType.GetMethods(
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                MethodInfo target = null;
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo m = methods[i];
                    if (m.GetParameters().Length != 0) continue;
                    if (!m.ReturnType.Equals(typeof(void))) continue;
                    if (!m.Name.StartsWith("E")) continue;
                    if (m.Name == "enabled") continue;
                    if (m.IsSpecialName) continue;


                    if (m.Name.Length == 7 && m.Name.EndsWith("Kza"))
                    {
                        target = m;
                        break;
                    }
                }

                if ((object)target == null)
                {
                    ModLog.Warn("[NoSpeedCap] Could not find E{Kza method.");
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
                ModLog.Debug("[NoSpeedCap] Patched successfully.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[NoSpeedCap] Patch failed: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "NoSpeedCap");
            }
        }
    }

    public static class NoSpeedCap_VCPatch
    {
        private static FieldInfo _vehicleField = null;
        private static PropertyInfo _tiltProp = null;
        private static PropertyInfo _inputAccProp = null;
        private static bool _vcFieldsCached = false;

        public static void Postfix(VehicleController __instance)
        {
            if (!NoSpeedCap.Enabled) return;
            if (!UnityNull.Alive(__instance)) return;

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
            if (!UnityNull.Alive(vehicle)) return;

            if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                System.StringComparison.Ordinal)) return;

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
            if (tiltInput <= 0.01f) return;

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
        private static FieldInfo _rbField = null;
        private static bool _fieldsCached = false;

        private static bool _active = false;
        private static Vector3 _savedVelocity;

        public static void Prefix(Vehicle __instance)
        {
            _active = false;
            if (!NoSpeedCap.Enabled) return;
            if (!UnityNull.Alive(__instance)) return;
            if (!string.Equals(__instance.gameObject.name, "Player_Human",
                System.StringComparison.Ordinal)) return;

            EnsureFields(__instance);

            Rigidbody rb = null;
            if ((object)_rbField != null)
                rb = _rbField.GetValue(__instance) as Rigidbody;
            if (!UnityNull.Alive(rb)) return;

            _savedVelocity = rb.velocity;
            rb.velocity = Vector3.zero;
            _active = true;
        }

        public static void Postfix(Vehicle __instance)
        {
            if (!_active) return;
            _active = false;

            Rigidbody rb = null;
            if ((object)_rbField != null)
                rb = _rbField.GetValue(__instance) as Rigidbody;
            if (!UnityNull.Alive(rb)) return;

            Vector3 accelDelta = rb.velocity;
            rb.velocity = _savedVelocity + accelDelta;
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

