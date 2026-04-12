using MelonLoader;
using UnityEngine;
using System.Reflection;
using HarmonyLib;

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
            MelonLogger.Msg("[WheelieAngleLimit] -> " + (Enabled ? "ON (" + DisplayValue + ")" : "OFF"));
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
                { MelonLogger.Warning("[WheelieAngleLimit] Vehicle.FixedUpdate not found."); return; }
                MethodInfo postfix = typeof(WheelieAngleLimit_Patch).GetMethod("Postfix",
                    BindingFlags.Public | BindingFlags.Static);
                harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                MelonLogger.Msg("[WheelieAngleLimit] Patched Vehicle.FixedUpdate.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[WheelieAngleLimit] ApplyPatch: " + ex.Message); }
        }

        public static void Reset() { Enabled = false; WheelieAngleLimit_Patch.ResetGrace(); }
    }

    public static class WheelieAngleLimit_Patch
    {
        private static PropertyInfo _rbProp = null;
        private static PropertyInfo _wheelGroundedProp = null; // TDEX{ib via reflection
        private static Wheel _frontWheel = null;
        private static Wheel _rearWheel = null;
        private static bool _cached = false;

        // Grace period — keeps the limiter active for a short window after the rear
        // wheel leaves the ground (e.g. hitting a bump mid-wheelie). Without this,
        // the limiter disengages the instant rear lifts and accumulated rotational
        // momentum spins the player off.
        private static float _graceTimer = 0f;
        private const float GraceDuration = 0.6f;

        // Live pitch in degrees (positive = nose up). Published every FixedUpdate
        // by the Postfix below — read by WheelieHUD regardless of limiter Enabled state.
        public static float CurrentPitch = 0f;

        public static void ResetGrace() { _graceTimer = 0f; }

        public static void Postfix(Vehicle __instance)
        {
            if ((object)__instance == null) return;
            if (!string.Equals(__instance.gameObject.name, "Player_Human",
                System.StringComparison.Ordinal)) return;

            try
            {
                // Always publish pitch so WheelieHUD can read it every frame,
                // regardless of whether the angle-limiter itself is enabled.
                CurrentPitch = Mathf.Asin(Mathf.Clamp(__instance.transform.forward.y, -1f, 1f))
                               * Mathf.Rad2Deg;

                // Limiter logic only runs when the toggle is on
                if (!WheelieAngleLimit.Enabled) return;

                if (!_cached) CacheRefs(__instance);

                Rigidbody rb = null;
                if ((object)_rbProp != null)
                    rb = _rbProp.GetValue(__instance, null) as Rigidbody;
                if ((object)rb == null) return;

                // Read grounded state via reflection (TDEX{ib has { in name — can't use directly)
                bool frontGrounded = IsGrounded(_frontWheel);
                bool rearGrounded = IsGrounded(_rearWheel);

                // Wheelie state: rear down, front up
                bool inWheelieState = rearGrounded && !frontGrounded;

                // Refresh grace timer while actively wheelie-ing; otherwise tick down
                if (inWheelieState)
                    _graceTimer = GraceDuration;
                else if (_graceTimer > 0f)
                    _graceTimer -= Time.fixedDeltaTime;

                // Limiter remains active during the grace window so a bump-launch
                // mid-wheelie doesn't drop the clamp and let the bike over-rotate
                if (!inWheelieState && _graceTimer <= 0f) return;

                if (CurrentPitch > WheelieAngleLimit.AngleLimit)
                {
                    // In Unity, rotating around +right with NEGATIVE angular velocity = nose goes UP
                    // (right-hand rule: +right rotation pushes nose DOWN)
                    // So we strip the negative component to prevent further nose-up rotation
                    Vector3 rightAxis = __instance.transform.right;
                    float pitchSpin = Vector3.Dot(rb.angularVelocity, rightAxis);
                    if (pitchSpin < 0f)
                        rb.angularVelocity -= rightAxis * pitchSpin;
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[WheelieAngleLimit] Postfix: " + ex.Message);
            }
        }

        private static bool IsGrounded(Wheel w)
        {
            if ((object)w == null || (object)_wheelGroundedProp == null) return false;
            try { return (bool)_wheelGroundedProp.GetValue(w, null); }
            catch { return false; }
        }

        private static void CacheRefs(Vehicle v)
        {
            _cached = true;

            // Rigidbody property on Vehicle
            PropertyInfo[] vProps = typeof(Vehicle).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < vProps.Length; i++)
            {
                if (vProps[i].CanRead && vProps[i].PropertyType.Equals(typeof(Rigidbody)))
                { _rbProp = vProps[i]; break; }
            }

            // TDEX{ib on Wheel — find by name prefix "TDEX" with get+set
            PropertyInfo[] wProps = typeof(Wheel).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < wProps.Length; i++)
            {
                if (wProps[i].CanRead && wProps[i].CanWrite &&
                    wProps[i].PropertyType.Equals(typeof(bool)) &&
                    wProps[i].Name.StartsWith("TDEX"))
                { _wheelGroundedProp = wProps[i]; break; }
            }

            MelonLogger.Msg("[WheelieAngleLimit] RB=" + ((object)_rbProp != null ? _rbProp.Name : "NULL")
                + " Grounded=" + ((object)_wheelGroundedProp != null ? _wheelGroundedProp.Name : "NULL"));

            // Front wheel = higher local z
            Wheel[] wheels = v.GetComponentsInChildren<Wheel>();
            if ((object)wheels != null && wheels.Length >= 2)
            {
                if (wheels[0].transform.localPosition.z >= wheels[1].transform.localPosition.z)
                { _frontWheel = wheels[0]; _rearWheel = wheels[1]; }
                else
                { _frontWheel = wheels[1]; _rearWheel = wheels[0]; }
                MelonLogger.Msg("[WheelieAngleLimit] Front=" + _frontWheel.gameObject.name
                    + " Rear=" + _rearWheel.gameObject.name);
            }
            else
            {
                MelonLogger.Warning("[WheelieAngleLimit] Wheel count=" + (wheels != null ? wheels.Length : 0));
            }
        }
    }
}