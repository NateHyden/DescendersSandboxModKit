using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class IceMode
    {
        public static bool Enabled { get; private set; } = false;

        // Saved angularDrag for restore
        private static float _savedAngularDrag = -1f;
        private static bool _defaultsSaved = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled)
                ApplyToRigidbody();
            else
                RestoreRigidbody();
            MelonLogger.Msg("[IceMode] -> " + (Enabled ? "ON" : "OFF"));
        }

        private static void ApplyToRigidbody()
        {
            try
            {
                Rigidbody rb = GetRigidbody();
                if ((object)rb == null) return;
                if (!_defaultsSaved)
                {
                    _savedAngularDrag = rb.angularDrag;
                    _defaultsSaved = true;
                    MelonLogger.Msg("[IceMode] Saved angularDrag=" + _savedAngularDrag);
                }
                rb.angularDrag = 0f;
            }
            catch (System.Exception ex) { MelonLogger.Error("[IceMode] ApplyToRigidbody: " + ex.Message); }
        }

        private static void RestoreRigidbody()
        {
            try
            {
                if (!_defaultsSaved) return;
                Rigidbody rb = GetRigidbody();
                if ((object)rb == null) return;
                rb.angularDrag = _savedAngularDrag;
                MelonLogger.Msg("[IceMode] Restored angularDrag=" + _savedAngularDrag);
            }
            catch (System.Exception ex) { MelonLogger.Error("[IceMode] RestoreRigidbody: " + ex.Message); }
        }

        private static Rigidbody GetRigidbody()
        {
            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) { MelonLogger.Warning("[IceMode] Player_Human not found."); return null; }
            return player.GetComponent<Rigidbody>();
        }

        public static void Reset()
        {
            if (Enabled) RestoreRigidbody();
            Enabled = false;
            _defaultsSaved = false;
            _savedAngularDrag = -1f;
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                // Patch Wheel.FixedUpdate — zero rollFriction every frame
                MethodInfo wheelFU = typeof(Wheel).GetMethod(
                    "FixedUpdate", BindingFlags.Public | BindingFlags.Instance);

                if ((object)wheelFU != null)
                {
                    harmony.Patch(wheelFU, postfix: new HarmonyMethod(
                        typeof(IceMode_WheelPatch).GetMethod("Postfix", BindingFlags.Public | BindingFlags.Static)));
                    MelonLogger.Msg("[IceMode] Patched Wheel.FixedUpdate.");
                }
                else
                    MelonLogger.Warning("[IceMode] Wheel.FixedUpdate not found.");

                // Patch Vehicle.FixedUpdate — zero averaged grip (eSXpeQc) every frame
                MethodInfo vehicleFU = typeof(Vehicle).GetMethod(
                    "FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)vehicleFU != null)
                {
                    harmony.Patch(vehicleFU, postfix: new HarmonyMethod(
                        typeof(IceMode_VehiclePatch).GetMethod("Postfix", BindingFlags.Public | BindingFlags.Static)));
                    MelonLogger.Msg("[IceMode] Patched Vehicle.FixedUpdate.");
                }
                else
                    MelonLogger.Warning("[IceMode] Vehicle.FixedUpdate not found.");

                DiagnosticsManager.Report("IceMode", true);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[IceMode] ApplyPatch: " + ex.Message);
                DiagnosticsManager.Report("IceMode", false, ex.Message);
            }
        }
    }

    public static class IceMode_WheelPatch
    {
        // WbmnXfG = rollFriction property on Wheel
        private static PropertyInfo _rollFrictionProp = null;

        public static void Postfix(Wheel __instance)
        {
            if (!IceMode.Enabled) return;
            if ((object)__instance == null) return;

            try
            {
                // Only local player wheels
                Transform t = __instance.transform;
                if ((object)t == null || (object)t.parent == null) return;
                if (!string.Equals(t.parent.name, "Player_Human", System.StringComparison.Ordinal)) return;

                if ((object)_rollFrictionProp == null)
                    _rollFrictionProp = typeof(Wheel).GetProperty(
                        "WbmnXfG", BindingFlags.Public | BindingFlags.Instance);

                if ((object)_rollFrictionProp != null)
                    _rollFrictionProp.SetValue(__instance, 0.0f, null);
            }
            catch { }
        }
    }

    public static class IceMode_VehiclePatch
    {
        // njDpmV = actual ground grip (public property on Vehicle)
        // This is what directly drives forward + lateral velocity corrections each frame.
        // eSXpeQc gets overwritten inside FixedUpdate before our postfix, so it does nothing.
        // njDpmV is SET by sub-methods then READ to apply forces -- zero it in postfix.
        private static PropertyInfo _groundGripProp = null;

        public static void Postfix(Vehicle __instance)
        {
            if (!IceMode.Enabled) return;
            if ((object)__instance == null) return;

            try
            {
                if (!string.Equals(__instance.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                if ((object)_groundGripProp == null)
                {
                    _groundGripProp = typeof(Vehicle).GetProperty(
                        "njDpmV", BindingFlags.Public | BindingFlags.Instance);
                    if ((object)_groundGripProp != null)
                        MelonLogger.Msg("[IceMode] Found ground grip prop: njDpmV");
                    else
                        MelonLogger.Warning("[IceMode] Could not find ground grip prop: njDpmV");
                }

                if ((object)_groundGripProp != null)
                    _groundGripProp.SetValue(__instance, 0.0f, null);
            }
            catch { }
        }
    }
}