using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    public static class CutBrakes
    {
        public static bool Enabled { get; private set; } = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("[CutBrakes] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo fixedUpdate = typeof(VehicleController).GetMethod(
                    "FixedUpdate",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)fixedUpdate == null)
                { ModLog.Warn("[CutBrakes] VehicleController.FixedUpdate not found."); return; }

                MethodInfo postfix = typeof(CutBrakes_Patch).GetMethod(
                    "Postfix", BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(postfix));
                ModLog.Debug("[CutBrakes] Patched VehicleController.FixedUpdate.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[CutBrakes] ApplyPatch: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "CutBrakes"); }
        }

        public static void Reset()
        {
            Enabled = false;
        }
    }

    public static class CutBrakes_Patch
    {
        private static FieldInfo _vehicleField = null;

        public static void Postfix(VehicleController __instance)
        {
            if (!CutBrakes.Enabled) return;
            if (!UnityNull.Alive(__instance)) return;

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
                    if ((object)_vehicleField == null)
                    { ModLog.Warn("[CutBrakes] Vehicle field not found."); return; }
                }

                Vehicle vehicle = _vehicleField.GetValue(__instance) as Vehicle;
                if (!UnityNull.Alive(vehicle)) return;

                if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                vehicle.NYsPlot = 0f;
            }
            catch (System.Exception ex) { MelonLogger.Error("[CutBrakes] Postfix: " + ex.Message); Telemetry.ReportErrorAsync(ex, "CutBrakes"); }
        }
    }
}
