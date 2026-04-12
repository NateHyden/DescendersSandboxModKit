using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Scales tyre grip by simulating inflation pressure.
    // Level 1  = flat tyre  -> less grip (1.6x rollFriction)
    // Level 5  = stock      -> no change (1.0x) -- zero allocation, early exit
    // Level 10 = rock hard  -> low grip  (0.2x)
    //
    // GC optimised: default rollFriction captured once per session (single GetValue).
    // CachedMultiplier pre-computed on level change. Early exit at Level 5 (1.0x = no work).
    public static class TyrePressure
    {
        public static bool Enabled { get; private set; } = false;

        private static int _level = 5;
        public static int Level => _level;

        // Pre-computed multiplier updated by SetLevel/Toggle -- not recomputed per frame
        private static float _cachedMultiplier = 1.0f;
        public static float CachedMultiplier => _cachedMultiplier;

        private static readonly string[] PressureLabels =
        {
            "Flat", "Flat", "Soft", "Soft", "Stock",
            "Stock", "Firm", "Firm", "Hard", "Hard"
        };

        public static string PressureLabel =>
            (_level >= 1 && _level <= 10) ? PressureLabels[_level - 1] : "Stock";

        // Not called per-frame -- only on level change or log output
        public static float GripMultiplier
        {
            get
            {
                if (_level <= 5)
                    return Mathf.Lerp(1.6f, 1.0f, (_level - 1) / 4f);
                else
                    return Mathf.Lerp(1.0f, 0.2f, (_level - 5) / 5f);
            }
        }

        private static void UpdateCache()
        {
            _cachedMultiplier = GripMultiplier;
            TyrePressure_WheelPatch.InvalidateDefault();
        }

        public static void Toggle()
        {
            Enabled = !Enabled;
            UpdateCache();
            MelonLogger.Msg("[TyrePressure] -> " + (Enabled ? "ON" : "OFF")
                + " level=" + _level + " grip=" + _cachedMultiplier.ToString("F2") + "x");
        }

        public static void SetLevel(int level)
        {
            _level = Mathf.Clamp(level, 1, 10);
            UpdateCache();
            MelonLogger.Msg("[TyrePressure] Level=" + _level
                + " (" + PressureLabel + ") grip=" + _cachedMultiplier.ToString("F2") + "x");
        }

        public static void Increase() { if (_level < 10) SetLevel(_level + 1); }
        public static void Decrease() { if (_level > 1) SetLevel(_level - 1); }

        public static void Reset()
        {
            Enabled = false;
            _level = 5;
            _cachedMultiplier = 1.0f;
            TyrePressure_WheelPatch.InvalidateDefault();
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo wheelFU = typeof(Wheel).GetMethod(
                    "FixedUpdate", BindingFlags.Public | BindingFlags.Instance);

                if ((object)wheelFU != null)
                {
                    harmony.Patch(wheelFU, postfix: new HarmonyMethod(
                        typeof(TyrePressure_WheelPatch).GetMethod(
                            "Postfix", BindingFlags.Public | BindingFlags.Static)));
                    MelonLogger.Msg("[TyrePressure] Patched Wheel.FixedUpdate.");
                }
                else
                    MelonLogger.Warning("[TyrePressure] Wheel.FixedUpdate not found.");

                DiagnosticsManager.Report("TyrePressure", true);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[TyrePressure] ApplyPatch: " + ex.Message);
                DiagnosticsManager.Report("TyrePressure", false, ex.Message);
            }
        }
    }

    public static class TyrePressure_WheelPatch
    {
        private static PropertyInfo _rollFrictionProp = null;
        private static bool _searched = false;

        // Default rollFriction captured once per session -- replaces recurring GetValue
        private static float _defaultFriction = -1f;

        public static void InvalidateDefault() { _defaultFriction = -1f; }

        public static void Postfix(Wheel __instance)
        {
            if (!TyrePressure.Enabled) return;

            // Level 5 = 1.0x multiplier = no change -- early exit, zero allocations
            float mult = TyrePressure.CachedMultiplier;
            if (mult == 1.0f) return;

            if ((object)__instance == null) return;

            try
            {
                Transform t = __instance.transform;
                if ((object)t == null || (object)t.parent == null) return;
                if (!string.Equals(t.parent.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                // Find property -- runs once per session
                if ((object)_rollFrictionProp == null && !_searched)
                {
                    _searched = true;
                    _rollFrictionProp = typeof(Wheel).GetProperty(
                        "WbmnXfG", BindingFlags.Public | BindingFlags.Instance);

                    if ((object)_rollFrictionProp != null)
                    {
                        MelonLogger.Msg("[TyrePressure] Prop WbmnXfG found.");
                    }
                    else
                    {
                        MelonLogger.Warning("[TyrePressure] Prop WbmnXfG not found -- dumping Wheel float props:");
                        var props = typeof(Wheel).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var p in props)
                            if (p.PropertyType.Equals(typeof(float)))
                                MelonLogger.Msg("[TyrePressure]   float prop: " + p.Name);
                        return;
                    }
                }

                if ((object)_rollFrictionProp == null) return;

                // Capture default rollFriction once -- single GetValue call per session
                if (_defaultFriction < 0f)
                {
                    _defaultFriction = (float)_rollFrictionProp.GetValue(__instance, null);
                    MelonLogger.Msg("[TyrePressure] Default rollFriction=" + _defaultFriction.ToString("F4"));
                }

                _rollFrictionProp.SetValue(__instance, _defaultFriction * mult, null);
            }
            catch { }
        }
    }
}