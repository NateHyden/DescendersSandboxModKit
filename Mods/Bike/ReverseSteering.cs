using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    public static class ReverseSteering
    {
        public static bool Enabled { get; private set; } = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("[ReverseSteering] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo fixedUpdate = typeof(VehicleController).GetMethod(
                    "FixedUpdate",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)fixedUpdate == null)
                {
                    ModLog.Warn("[ReverseSteering] VehicleController.FixedUpdate not found.");
                    return;
                }

                MethodInfo postfix = typeof(ReverseSteering_Patch).GetMethod(
                    "Postfix", BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(postfix));
                ModLog.Debug("[ReverseSteering] Patched VehicleController.FixedUpdate.");
                DiagnosticsManager.Report("ReverseSteering", true);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[ReverseSteering] ApplyPatch: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "ReverseSteering");
                DiagnosticsManager.Report("ReverseSteering", false, ex.Message);
            }
        }

        public static void Reset()
        {
            Enabled = false;
        }
    }

    public static class ReverseSteering_Patch
    {
        private static FieldInfo _vehicleField = null;

        private static PropertyInfo _steerProp = null;

        private static PropertyInfo _leanProp = null;

        private static readonly string SteerPropName = "swebLyg";
        private static readonly string LeanPropName = "c\u007Bv\u007DlhG";

        public static void Postfix(VehicleController __instance)
        {
            if (!ReverseSteering.Enabled) return;
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
                            ModLog.Debug("[ReverseSteering] Found Vehicle field: " + fields[i].Name);
                            break;
                        }
                    }

                    if ((object)_vehicleField == null)
                    {
                        ModLog.Warn("[ReverseSteering] Could not find Vehicle field on VehicleController.");
                        return;
                    }
                }

                Vehicle vehicle = _vehicleField.GetValue(__instance) as Vehicle;
                if (!UnityNull.Alive(vehicle)) return;

                if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                if ((object)_steerProp == null)
                {
                    _steerProp = typeof(Vehicle).GetProperty(
                        SteerPropName,
                        BindingFlags.Public | BindingFlags.Instance);

                    if ((object)_steerProp != null)
                        ModLog.Debug("[ReverseSteering] Found steer property: " + SteerPropName);
                    else
                        ModLog.Warn("[ReverseSteering] Could not find steer property: " + SteerPropName);
                }

                if ((object)_leanProp == null)
                {
                    _leanProp = typeof(Vehicle).GetProperty(
                        LeanPropName,
                        BindingFlags.Public | BindingFlags.Instance);

                    if ((object)_leanProp != null)
                        ModLog.Debug("[ReverseSteering] Found lean property: " + LeanPropName);
                    else
                        ModLog.Warn("[ReverseSteering] Could not find lean property: " + LeanPropName);
                }

                if ((object)_steerProp != null)
                {
                    float steer = (float)_steerProp.GetValue(vehicle, null);
                    _steerProp.SetValue(vehicle, -steer, null);
                }

                if ((object)_leanProp != null)
                {
                    float lean = (float)_leanProp.GetValue(vehicle, null);
                    _leanProp.SetValue(vehicle, -lean, null);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[ReverseSteering] Postfix error: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "ReverseSteering");
            }
        }
    }
}
