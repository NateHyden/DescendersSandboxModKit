using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class ReverseSteering
    {
        public static bool Enabled { get; private set; } = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            MelonLogger.Msg("[ReverseSteering] -> " + (Enabled ? "ON" : "OFF"));
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
                    MelonLogger.Warning("[ReverseSteering] VehicleController.FixedUpdate not found.");
                    return;
                }

                MethodInfo postfix = typeof(ReverseSteering_Patch).GetMethod(
                    "Postfix", BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(postfix));
                MelonLogger.Msg("[ReverseSteering] Patched VehicleController.FixedUpdate.");
                DiagnosticsManager.Report("ReverseSteering", true);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[ReverseSteering] ApplyPatch: " + ex.Message);
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
        // CDVkgio = Vehicle field on VehicleController
        private static FieldInfo _vehicleField = null;

        // swebLyg = steering input (public property on Vehicle)
        private static PropertyInfo _steerProp = null;

        // c{v}lhG = lean input (public property on Vehicle)
        private static PropertyInfo _leanProp = null;

        private static readonly string SteerPropName = "swebLyg";
        private static readonly string LeanPropName = "c\u007Bv\u007DlhG";

        public static void Postfix(VehicleController __instance)
        {
            if (!ReverseSteering.Enabled) return;
            if ((object)__instance == null) return;

            try
            {
                // Cache the Vehicle field on VehicleController
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
                            MelonLogger.Msg("[ReverseSteering] Found Vehicle field: " + fields[i].Name);
                            break;
                        }
                    }

                    if ((object)_vehicleField == null)
                    {
                        MelonLogger.Warning("[ReverseSteering] Could not find Vehicle field on VehicleController.");
                        return;
                    }
                }

                Vehicle vehicle = _vehicleField.GetValue(__instance) as Vehicle;
                if ((object)vehicle == null) return;

                // Only affect local player
                if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                // Cache steer property (swebLyg) - it's a public property, not a field
                if ((object)_steerProp == null)
                {
                    _steerProp = typeof(Vehicle).GetProperty(
                        SteerPropName,
                        BindingFlags.Public | BindingFlags.Instance);

                    if ((object)_steerProp != null)
                        MelonLogger.Msg("[ReverseSteering] Found steer property: " + SteerPropName);
                    else
                        MelonLogger.Warning("[ReverseSteering] Could not find steer property: " + SteerPropName);
                }

                // Cache lean property (c{v}lhG) - also a public property
                if ((object)_leanProp == null)
                {
                    _leanProp = typeof(Vehicle).GetProperty(
                        LeanPropName,
                        BindingFlags.Public | BindingFlags.Instance);

                    if ((object)_leanProp != null)
                        MelonLogger.Msg("[ReverseSteering] Found lean property: " + LeanPropName);
                    else
                        MelonLogger.Warning("[ReverseSteering] Could not find lean property: " + LeanPropName);
                }

                // Negate steer input
                if ((object)_steerProp != null)
                {
                    float steer = (float)_steerProp.GetValue(vehicle, null);
                    _steerProp.SetValue(vehicle, -steer, null);
                }

                // Negate lean input
                if ((object)_leanProp != null)
                {
                    float lean = (float)_leanProp.GetValue(vehicle, null);
                    _leanProp.SetValue(vehicle, -lean, null);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[ReverseSteering] Postfix error: " + ex.Message);
            }
        }
    }
}