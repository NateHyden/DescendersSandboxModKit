using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Bypass the game's fakie pedal gate in VehicleController.FixedUpdate:
    /// accel *= InverseLerp(-10, -5, groundSpaceVelocity.z)
    /// </summary>
    public static class PedalWhileReverse
    {
        public static bool Enabled { get; private set; } = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("[PedalWhileReverse] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo fixedUpdate = typeof(VehicleController).GetMethod(
                    "FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)fixedUpdate == null)
                { ModLog.Warn("[PedalWhileReverse] VehicleController.FixedUpdate not found."); return; }

                harmony.Patch(fixedUpdate,
                    prefix: new HarmonyMethod(typeof(PedalWhileReverse_Patch).GetMethod("Prefix")),
                    postfix: new HarmonyMethod(typeof(PedalWhileReverse_Patch).GetMethod("Postfix")));
                ModLog.Debug("[PedalWhileReverse] Patched VehicleController.FixedUpdate.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[PedalWhileReverse] ApplyPatch: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "PedalWhileReverse");
            }
        }

        public static void Reset() { Enabled = false; }
    }

    public static class PedalWhileReverse_Patch
    {
        private static MethodInfo _getAccelInput = null;
        private static MethodInfo _getGroundVel = null;
        private static MethodInfo _getOnGround = null;
        private static MethodInfo _getAccel = null;
        private static MethodInfo _setAccel = null;
        private static FieldInfo _vehicleField = null;
        private static bool _searched = false;

        private static float _savedAccelInput;

        // Game starts cutting pedal below -5 ground-space Z (full cut at -10).
        private const float ReverseGateZ = -5f;

        public static void Prefix(VehicleController __instance)
        {
            _savedAccelInput = 0f;
            if (!PedalWhileReverse.Enabled) return;
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
            if (!PedalWhileReverse.Enabled) return;
            if (_savedAccelInput < 0.05f) return;
            if (!UnityNull.Alive(__instance)) return;

            try
            {
                EnsureMethods();
                if ((object)_vehicleField == null || (object)_getGroundVel == null
                    || (object)_getAccel == null || (object)_setAccel == null) return;

                Vehicle vehicle = _vehicleField.GetValue(__instance) as Vehicle;
                if (!UnityNull.Alive(vehicle)) return;
                if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                if ((object)_getOnGround != null)
                {
                    bool onGround = (bool)_getOnGround.Invoke(vehicle, null);
                    if (!onGround) return;
                }

                Vector3 gsv = (Vector3)_getGroundVel.Invoke(vehicle, null);
                if (gsv.z >= ReverseGateZ) return;

                float curAccel = (float)_getAccel.Invoke(vehicle, null);
                if (curAccel < _savedAccelInput - 0.01f)
                {
                    _setAccel.Invoke(vehicle, new object[] { _savedAccelInput });
                    ModLog.Debug("[PedalWhileReverse] Restored accel=" + _savedAccelInput
                        + " vz=" + gsv.z.ToString("F1"));
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[PedalWhileReverse] Postfix: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "PedalWhileReverse");
            }
        }

        private static void EnsureMethods()
        {
            if (_searched) return;
            _searched = true;

            _getAccelInput = typeof(VehicleController).GetMethod("get_accelInput",
                BindingFlags.Public | BindingFlags.Instance);
            if ((object)_getAccelInput == null)
                ModLog.Warn("[PedalWhileReverse] get_accelInput not found on VehicleController.");

            FieldInfo[] fields = typeof(VehicleController).GetFields(
                BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i].FieldType.Name, "Vehicle", System.StringComparison.Ordinal))
                { _vehicleField = fields[i]; break; }
            }
            if ((object)_vehicleField == null)
                ModLog.Warn("[PedalWhileReverse] Vehicle field not found on VehicleController.");

            _getGroundVel = typeof(Vehicle).GetMethod("get_groundSpaceVelocity",
                BindingFlags.Public | BindingFlags.Instance);
            _getOnGround = typeof(Vehicle).GetMethod("get_onGround",
                BindingFlags.Public | BindingFlags.Instance);
            _getAccel = typeof(Vehicle).GetMethod("get_inputAcceleration",
                BindingFlags.Public | BindingFlags.Instance);
            _setAccel = typeof(Vehicle).GetMethod("set_inputAcceleration",
                BindingFlags.Public | BindingFlags.Instance);

            if ((object)_getGroundVel == null)
                ModLog.Warn("[PedalWhileReverse] get_groundSpaceVelocity not found on Vehicle.");
            if ((object)_getAccel == null || (object)_setAccel == null)
                ModLog.Warn("[PedalWhileReverse] get/set_inputAcceleration not found on Vehicle.");
        }
    }
}
