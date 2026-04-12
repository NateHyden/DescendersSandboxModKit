using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class CutBrakes
    {
        public static bool Enabled { get; private set; } = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            MelonLogger.Msg("[CutBrakes] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo fixedUpdate = typeof(VehicleController).GetMethod(
                    "FixedUpdate",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)fixedUpdate == null)
                { MelonLogger.Warning("[CutBrakes] VehicleController.FixedUpdate not found."); return; }

                MethodInfo postfix = typeof(CutBrakes_Patch).GetMethod(
                    "Postfix", BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(postfix));
                MelonLogger.Msg("[CutBrakes] Patched VehicleController.FixedUpdate.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[CutBrakes] ApplyPatch: " + ex.Message); }
        }

        public static void Reset()
        {
            Enabled = false;
        }
    }

    public static class CutBrakes_Patch
    {
        // Cached field: NYsPlot on Vehicle is the brake input
        private static FieldInfo _vehicleField = null;

        public static void Postfix(VehicleController __instance)
        {
            if (!CutBrakes.Enabled) return;
            if ((object)__instance == null) return;

            try
            {
                // Get the Vehicle reference (CDVkgio field on VehicleController)
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
                    { MelonLogger.Warning("[CutBrakes] Vehicle field not found."); return; }
                }

                Vehicle vehicle = _vehicleField.GetValue(__instance) as Vehicle;
                if ((object)vehicle == null) return;

                // Only affect local player
                if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                // NYsPlot is public property on Vehicle - set directly
                vehicle.NYsPlot = 0f;
            }
            catch (System.Exception ex) { MelonLogger.Error("[CutBrakes] Postfix: " + ex.Message); }
        }
    }
}