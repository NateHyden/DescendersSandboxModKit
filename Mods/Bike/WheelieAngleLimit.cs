using MelonLoader;
using UnityEngine;
using System.Reflection;
using HarmonyLib;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    public static class WheelieAngleLimit
    {
        public static bool Enabled { get; private set; } = false;
        public static int Level { get; private set; } = 5;

        public static float AngleLimit { get { return Mathf.Lerp(20f, 85f, (Level - 1) / 9f); } }
        public static string DisplayValue { get { return Mathf.RoundToInt(AngleLimit) + "\u00b0"; } }

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("[WheelieAngleLimit] -> " + (Enabled ? "ON (" + DisplayValue + ")" : "OFF"));
        }

        public static void Increase() { if (Level < 10) Level++; }
        public static void Decrease() { if (Level > 1) Level--; }
        public static void SetLevel(int level) { Level = Mathf.Clamp(level, 1, 10); }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo target = typeof(Vehicle).GetMethod("FixedUpdate",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)target == null)
                { ModLog.Warn("[WheelieAngleLimit] Vehicle.FixedUpdate not found."); return; }
                MethodInfo postfix = typeof(WheelieAngleLimit_Patch).GetMethod("Postfix",
                    BindingFlags.Public | BindingFlags.Static);
                harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                ModLog.Debug("[WheelieAngleLimit] Patched Vehicle.FixedUpdate.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[WheelieAngleLimit] ApplyPatch: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "WheelieAngleLimit"); }
        }

        public static void Reset() { Enabled = false; WheelieAngleLimit_Patch.ResetGrace(); }
    }

    public static class WheelieAngleLimit_Patch
    {
        private static PropertyInfo _rbProp = null;
        private static PropertyInfo _wheelGroundedProp = null;
        private static Wheel _frontWheel = null;
        private static Wheel _rearWheel = null;
        private static bool _cached = false;

        private static float _graceTimer = 0f;
        private const float GraceDuration = 0.6f;

        public static float CurrentPitch = 0f;

        public static void ResetGrace() { _graceTimer = 0f; }

        public static void Postfix(Vehicle __instance)
        {
            if (!UnityNull.Alive(__instance)) return;
            if (!string.Equals(__instance.gameObject.name, "Player_Human",
                System.StringComparison.Ordinal)) return;

            try
            {
                CurrentPitch = Mathf.Asin(Mathf.Clamp(__instance.transform.forward.y, -1f, 1f))
                               * Mathf.Rad2Deg;

                if (!WheelieAngleLimit.Enabled) return;

                if (((object)_frontWheel != null && !UnityNull.Alive(_frontWheel))
                    || ((object)_rearWheel != null && !UnityNull.Alive(_rearWheel)))
                {
                    _frontWheel = null;
                    _rearWheel = null;
                    _cached = false;
                }
                if (!_cached) CacheRefs(__instance);

                Rigidbody rb = null;
                if ((object)_rbProp != null)
                    rb = _rbProp.GetValue(__instance, null) as Rigidbody;
                if (!UnityNull.Alive(rb)) return;

                bool frontGrounded = IsGrounded(_frontWheel);
                bool rearGrounded = IsGrounded(_rearWheel);

                bool inWheelieState = rearGrounded && !frontGrounded;

                if (inWheelieState)
                    _graceTimer = GraceDuration;
                else if (_graceTimer > 0f)
                    _graceTimer -= Time.fixedDeltaTime;

                if (!inWheelieState && _graceTimer <= 0f) return;

                if (CurrentPitch > WheelieAngleLimit.AngleLimit)
                {
                    Vector3 rightAxis = __instance.transform.right;
                    float pitchSpin = Vector3.Dot(rb.angularVelocity, rightAxis);
                    if (pitchSpin < 0f)
                        rb.angularVelocity -= rightAxis * pitchSpin;
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[WheelieAngleLimit] Postfix: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "WheelieAngleLimit");
            }
        }

        private static bool IsGrounded(Wheel w)
        {
            if (!UnityNull.Alive(w) || (object)_wheelGroundedProp == null) return false;
            try { return (bool)_wheelGroundedProp.GetValue(w, null); }
            catch { return false; }
        }

        private static void CacheRefs(Vehicle v)
        {
            if (!UnityNull.Alive(v)) { _cached = false; return; }
            _cached = true;

            PropertyInfo[] vProps = typeof(Vehicle).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < vProps.Length; i++)
            {
                if (vProps[i].CanRead && vProps[i].PropertyType.Equals(typeof(Rigidbody)))
                { _rbProp = vProps[i]; break; }
            }

            PropertyInfo[] wProps = typeof(Wheel).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < wProps.Length; i++)
            {
                if (wProps[i].CanRead && wProps[i].CanWrite &&
                    wProps[i].PropertyType.Equals(typeof(bool)) &&
                    wProps[i].Name.StartsWith("TDEX"))
                { _wheelGroundedProp = wProps[i]; break; }
            }

            ModLog.Debug("[WheelieAngleLimit] RB=" + ((object)_rbProp != null ? _rbProp.Name : "NULL")
                + " Grounded=" + ((object)_wheelGroundedProp != null ? _wheelGroundedProp.Name : "NULL"));

            Wheel[] wheels = v.GetComponentsInChildren<Wheel>();
            if ((object)wheels != null && wheels.Length >= 2)
            {
                if (!UnityNull.Alive(wheels[0]) || !UnityNull.Alive(wheels[1]))
                {
                    _cached = false;
                    return;
                }
                if (wheels[0].transform.localPosition.z >= wheels[1].transform.localPosition.z)
                { _frontWheel = wheels[0]; _rearWheel = wheels[1]; }
                else
                { _frontWheel = wheels[1]; _rearWheel = wheels[0]; }
                ModLog.Debug("[WheelieAngleLimit] Front=" + _frontWheel.gameObject.name
                    + " Rear=" + _rearWheel.gameObject.name);
            }
            else
            {
                ModLog.Warn("[WheelieAngleLimit] Wheel count=" + (wheels != null ? wheels.Length : 0));
            }
        }
    }
}

