using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using DescendersModMenu.UI;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Holds the brake while the mod menu is open so the bike does not roll away
    /// on slopes (keyboard, mouse, or controller menu open).
    /// </summary>
    public static class MenuBrakeHold
    {
        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo fixedUpdate = typeof(VehicleController).GetMethod(
                    "FixedUpdate",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)fixedUpdate == null)
                {
                    ModLog.Warn("[MenuBrakeHold] VehicleController.FixedUpdate not found.");
                    return;
                }

                MethodInfo postfix = typeof(MenuBrakeHold_Patch).GetMethod(
                    "Postfix", BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(postfix));
                ModLog.Debug("[MenuBrakeHold] Patched VehicleController.FixedUpdate.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[MenuBrakeHold] ApplyPatch: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "MenuBrakeHold");
            }
        }
    }

    public static class MenuBrakeHold_Patch
    {
        private static FieldInfo _vehicleField;
        private static Rigidbody _rb;
        private static float _savedDrag;
        private static bool _appliedHold;

        public static void Postfix(VehicleController __instance)
        {
            if (!MenuUI.IsOpen)
            {
                RestoreDrag();
                return;
            }
            if (FlyMode.Enabled || SpectateMode.Enabled) return;
            if ((object)__instance == null) return;

            try
            {
                Vehicle vehicle = GetVehicle(__instance);
                if (!UnityNull.Alive(vehicle)) return;
                if (!string.Equals(vehicle.gameObject.name, "Player_Human", System.StringComparison.Ordinal))
                    return;

                vehicle.NYsPlot = 1f;

                if (!EnsureRigidbody(vehicle)) return;

                if (!_appliedHold)
                {
                    _savedDrag = _rb.drag;
                    _appliedHold = true;
                }

                if (_rb.velocity.sqrMagnitude <= 0.25f)
                {
                    _rb.velocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }
                else
                {
                    _rb.drag = Mathf.Max(_savedDrag, 80f);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[MenuBrakeHold] Postfix: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "MenuBrakeHold");
                _vehicleField = null;
                _rb = null;
                _appliedHold = false;
            }
        }

        private static void RestoreDrag()
        {
            if (!_appliedHold) return;
            try
            {
                if (UnityNull.Alive(_rb)) _rb.drag = _savedDrag;
            }
            catch { }
            _appliedHold = false;
        }

        private static Vehicle GetVehicle(VehicleController vc)
        {
            if ((object)_vehicleField == null)
            {
                FieldInfo[] fields = vc.GetType().GetFields(
                    BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (string.Equals(fields[i].FieldType.Name, "Vehicle", System.StringComparison.Ordinal))
                    {
                        _vehicleField = fields[i];
                        break;
                    }
                }
                if ((object)_vehicleField == null) return null;
            }

            return _vehicleField.GetValue(vc) as Vehicle;
        }

        private static bool EnsureRigidbody(Vehicle vehicle)
        {
            if (UnityNull.Alive(_rb)) return true;

            _rb = null;
            PropertyInfo[] props = typeof(Vehicle).GetProperties(
                BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < props.Length; i++)
            {
                if (props[i].CanRead && string.Equals(props[i].PropertyType.Name, "Rigidbody", System.StringComparison.Ordinal))
                {
                    _rb = props[i].GetValue(vehicle, null) as Rigidbody;
                    break;
                }
            }

            if (UnityNull.Alive(_rb))
                return true;

            return false;
        }

        public static void ClearCache()
        {
            RestoreDrag();
            _vehicleField = null;
            _rb = null;
            _savedDrag = 0f;
        }
    }
}
