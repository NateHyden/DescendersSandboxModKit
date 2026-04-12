using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class AutoBalance
    {
        public static bool Enabled { get; private set; } = false;

        public static int StrengthLevel { get; private set; } = 5;
        private static readonly float[] StrengthValues =
        {
            1f, 2f, 3f, 5f, 7f, 10f, 14f, 18f, 23f, 30f
        };
        public static float Strength { get { return StrengthValues[StrengthLevel - 1]; } }

        public static void Toggle()
        {
            Enabled = !Enabled;
            MelonLogger.Msg("[AutoBalance] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void StrengthIncrease()
        {
            if (StrengthLevel < 10) { StrengthLevel++; MelonLogger.Msg("[AutoBalance] Strength -> " + StrengthLevel); }
        }

        public static void StrengthDecrease()
        {
            if (StrengthLevel > 1) { StrengthLevel--; MelonLogger.Msg("[AutoBalance] Strength -> " + StrengthLevel); }
        }

        public static void SetStrengthLevel(int v) { StrengthLevel = System.Math.Max(1, System.Math.Min(10, v)); }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo fixedUpdate = typeof(Vehicle).GetMethod(
                    "FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)fixedUpdate == null)
                {
                    MelonLogger.Warning("[AutoBalance] Vehicle.FixedUpdate not found.");
                    DiagnosticsManager.Report("AutoBalance", false, "Vehicle.FixedUpdate not found");
                    return;
                }

                MethodInfo postfix = typeof(AutoBalance_Patch).GetMethod(
                    "Postfix", BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(postfix));
                MelonLogger.Msg("[AutoBalance] Patched Vehicle.FixedUpdate.");
                DiagnosticsManager.Report("AutoBalance", true);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[AutoBalance] ApplyPatch: " + ex.Message);
                DiagnosticsManager.Report("AutoBalance", false, ex.Message);
            }
        }

        public static void Reset()
        {
            Enabled = false;
            StrengthLevel = 5;
        }
    }

    public static class AutoBalance_Patch
    {
        public static void Postfix(Vehicle __instance)
        {
            if (!AutoBalance.Enabled) return;
            if ((object)__instance == null) return;

            try
            {
                if (!string.Equals(__instance.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                Rigidbody rb = __instance.GetComponent<Rigidbody>();
                if ((object)rb == null) return;

                // Roll-only correction using world-space vectors — no Euler angles,
                // so no gimbal lock during backflips/frontflips.
                //
                // "No roll" means the bike's local RIGHT axis is horizontal (no Y component).
                // We find the corrective rotation needed to push right.y toward 0,
                // rotating around the bike's FORWARD axis only.
                //
                // This leaves pitch (forward/back tilt) completely untouched.

                Transform t = __instance.transform;
                Vector3 right = t.right;    // bike's local X in world space
                Vector3 forward = t.forward;  // bike's local Z in world space

                // The component of 'right' that we want to eliminate is its Y part.
                // Project 'right' onto the horizontal plane, then find the rotation
                // from current right to that horizontal right — around forward axis only.
                Vector3 rightFlat = new Vector3(right.x, 0f, right.z);
                if (rightFlat.sqrMagnitude < 0.001f) return; // near-vertical forward, skip

                rightFlat = rightFlat.normalized;

                // Angle between current right and flat right, signed around forward axis
                float rollAngle = Vector3.SignedAngle(right, rightFlat, forward);

                // Apply corrective rotation — slerp a fraction of the roll error per frame
                float correction = rollAngle * AutoBalance.Strength * Time.fixedDeltaTime;
                Quaternion corrective = Quaternion.AngleAxis(correction, forward);
                rb.MoveRotation(corrective * rb.rotation);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[AutoBalance] Postfix: " + ex.Message);
            }
        }
    }
}