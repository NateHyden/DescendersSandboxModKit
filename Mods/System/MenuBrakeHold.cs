using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using DescendersModMenu.UI;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// While the mod menu is open, gently decelerate the local bike to a stop,
    /// then pin it so it does not roll away on slopes. Avoids the emergency-stop
    /// bail that full brake + huge drag caused at speed.
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

        public static void ClearCache()
        {
            MenuBrakeHold_Patch.ClearCache();
        }
    }

    public static class MenuBrakeHold_Patch
    {
        private static FieldInfo _vehicleField;
        private static Rigidbody _rb;
        private static float _savedDrag;
        private static bool _appliedHold;

        private static VehicleController _localVc;
        private static float _nextLocalFind;

        // Soft stop: ~2s from a fast run, no slam that tips you off.
        private const float Decel = 14f;          // m/s² linear
        private const float AngDecel = 8f;       // rad/s²
        private const float PinSpeedSq = 0.36f;  // ~0.6 m/s — then freeze
        private const float LightBrake = 0.35f;  // visual brake, not full lock

        private static bool IsLocalController(VehicleController vc)
        {
            if (!UnityNull.Alive(_localVc))
            {
                float now = Time.unscaledTime;
                if (now < _nextLocalFind) return false;
                _nextLocalFind = now + 1f;
                GameObject go = GameObject.Find("Player_Human");
                _localVc = UnityNull.Alive(go) ? go.GetComponent<VehicleController>() : null;
                if (!UnityNull.Alive(_localVc) && UnityNull.Alive(go))
                    _localVc = go.GetComponentInChildren<VehicleController>();
            }
            return (object)vc == (object)_localVc;
        }

        public static void Postfix(VehicleController __instance)
        {
            if (!MenuUI.IsOpen)
            {
                if (_appliedHold) RestoreDrag();
                return;
            }
            if (FlyMode.Enabled || SpectateMode.Enabled) return;
            if ((object)__instance == null) return;
            if (!IsLocalController(__instance)) return;

            try
            {
                Vehicle vehicle = GetVehicle(__instance);
                if (!UnityNull.Alive(vehicle)) return;
                if (!EnsureRigidbody(vehicle)) return;

                if (!_appliedHold)
                {
                    _savedDrag = _rb.drag;
                    _appliedHold = true;
                }

                float dt = Time.fixedDeltaTime;
                if (dt < 0.0001f) dt = 0.02f;

                Vector3 vel = _rb.velocity;
                float speedSq = vel.sqrMagnitude;

                if (speedSq <= PinSpeedSq)
                {
                    // Fully stopped — pin so slopes don't take you away.
                    vehicle.NYsPlot = 1f;
                    _rb.velocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                    _rb.drag = Mathf.Max(_savedDrag, 40f);
                    return;
                }

                // Soft decelerate toward a hold (no full brake / no drag spike).
                vehicle.NYsPlot = LightBrake;

                float speed = Mathf.Sqrt(speedSq);
                float newSpeed = Mathf.MoveTowards(speed, 0f, Decel * dt);
                if (newSpeed <= 0.001f)
                    _rb.velocity = Vector3.zero;
                else
                    _rb.velocity = vel * (newSpeed / speed);

                _rb.angularVelocity = Vector3.MoveTowards(
                    _rb.angularVelocity, Vector3.zero, AngDecel * dt);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[MenuBrakeHold] Postfix: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "MenuBrakeHold");
                _vehicleField = null;
                _rb = null;
                _localVc = null;
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

            return UnityNull.Alive(_rb);
        }

        public static void ClearCache()
        {
            RestoreDrag();
            _vehicleField = null;
            _rb = null;
            _localVc = null;
            _savedDrag = 0f;
        }
    }
}
