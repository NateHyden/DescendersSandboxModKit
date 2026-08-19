using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    public static class PedalWhileTweak
    {
        public static bool Enabled { get; private set; } = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("[PedalWhileTweak] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo fixedUpdate = typeof(VehicleController).GetMethod(
                    "FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)fixedUpdate == null)
                { ModLog.Warn("[PedalWhileTweak] VehicleController.FixedUpdate not found."); return; }

                harmony.Patch(fixedUpdate,
                    prefix: new HarmonyMethod(typeof(PedalWhileTweak_Patch).GetMethod("Prefix")),
                    postfix: new HarmonyMethod(typeof(PedalWhileTweak_Patch).GetMethod("Postfix")));
                ModLog.Debug("[PedalWhileTweak] Patched VehicleController.FixedUpdate.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[PedalWhileTweak] ApplyPatch: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "PedalWhileTweak");
            }
        }

        public static void Reset() { Enabled = false; }
    }

    public static class PedalWhileTweak_Patch
    {
        // get_accelInput on VehicleController — the raw left trigger value read this frame
        private static MethodInfo _getAccelInput = null;
        private static bool _searched = false;

        // get/set inputTweaking on Vehicle — right stick deflection
        private static MethodInfo _getTweaking = null;
        // get/set inputAcceleration on Vehicle — pedal value passed to physics
        private static MethodInfo _getAccel = null;
        private static MethodInfo _setAccel = null;

        private static FieldInfo _vehicleField = null;

        private static float _savedAccelInput;

        public static void Prefix(VehicleController __instance)
        {
            _savedAccelInput = 0f;
            if (!PedalWhileTweak.Enabled) return;
            try
            {
                EnsureMethods();
                if ((object)_getAccelInput == null) return;
                _savedAccelInput = (float)_getAccelInput.Invoke(__instance, null);
            }
            catch { }
        }

        public static void Postfix(VehicleController __instance)
        {
            if (!PedalWhileTweak.Enabled) return;
            if (_savedAccelInput < 0.05f) return;
            if (!UnityNull.Alive(__instance)) return;

            try
            {
                EnsureMethods();
                if ((object)_vehicleField == null || (object)_getTweaking == null
                    || (object)_getAccel == null || (object)_setAccel == null) return;

                Vehicle vehicle = _vehicleField.GetValue(__instance) as Vehicle;
                if (!UnityNull.Alive(vehicle)) return;
                if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                float tweakMag = Mathf.Abs((float)_getTweaking.Invoke(vehicle, null));
                if (tweakMag < 0.05f) return;

                float curAccel = (float)_getAccel.Invoke(vehicle, null);
                if (curAccel < 0.01f)
                {
                    _setAccel.Invoke(vehicle, new object[] { _savedAccelInput });
                    ModLog.Debug("[PedalWhileTweak] Restored accel=" + _savedAccelInput + " tweak=" + tweakMag);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[PedalWhileTweak] Postfix: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "PedalWhileTweak");
            }
        }

        private static void EnsureMethods()
        {
            if (_searched) return;
            _searched = true;

            _getAccelInput = typeof(VehicleController).GetMethod("get_accelInput",
                BindingFlags.Public | BindingFlags.Instance);
            if ((object)_getAccelInput == null)
                ModLog.Warn("[PedalWhileTweak] get_accelInput not found on VehicleController.");
            else
                ModLog.Debug("[PedalWhileTweak] Found get_accelInput.");

            FieldInfo[] fields = typeof(VehicleController).GetFields(
                BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i].FieldType.Name, "Vehicle", System.StringComparison.Ordinal))
                { _vehicleField = fields[i]; break; }
            }
            if ((object)_vehicleField == null)
                ModLog.Warn("[PedalWhileTweak] Vehicle field not found on VehicleController.");

            _getTweaking = typeof(Vehicle).GetMethod("get_inputTweaking",
                BindingFlags.Public | BindingFlags.Instance);
            _getAccel = typeof(Vehicle).GetMethod("get_inputAcceleration",
                BindingFlags.Public | BindingFlags.Instance);
            _setAccel = typeof(Vehicle).GetMethod("set_inputAcceleration",
                BindingFlags.Public | BindingFlags.Instance);

            if ((object)_getTweaking == null)
                ModLog.Warn("[PedalWhileTweak] get_inputTweaking not found on Vehicle.");
            else
                ModLog.Debug("[PedalWhileTweak] Found get_inputTweaking.");

            if ((object)_getAccel == null || (object)_setAccel == null)
                ModLog.Warn("[PedalWhileTweak] get/set_inputAcceleration not found on Vehicle.");
            else
                ModLog.Debug("[PedalWhileTweak] Found get/set_inputAcceleration.");
        }
    }
}
