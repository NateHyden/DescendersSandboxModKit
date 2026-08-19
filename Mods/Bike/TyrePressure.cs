using HarmonyLib;
using MelonLoader;
using DescendersModMenu;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class TyrePressure
    {
        public static bool Enabled { get; private set; } = false;

        private static int _level = 5;
        public static int Level => _level;

        private static float _cachedMultiplier = 1.0f;
        public static float CachedMultiplier => _cachedMultiplier;

        private static readonly string[] PressureLabels =
        {
            "Flat", "Flat", "Soft", "Soft", "Stock",
            "Stock", "Firm", "Firm", "Hard", "Hard"
        };

        public static string PressureLabel =>
            (_level >= 1 && _level <= 10) ? PressureLabels[_level - 1] : "Stock";

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
            ModLog.Feedback("[TyrePressure] -> " + (Enabled ? "ON" : "OFF")
                + " level=" + _level + " grip=" + _cachedMultiplier.ToString("F2") + "x");
        }

        public static void SetLevel(int level)
        {
            _level = Mathf.Clamp(level, 1, 10);
            UpdateCache();
            ModLog.Debug("[TyrePressure] Level=" + _level
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
                    ModLog.Debug("[TyrePressure] Patched Wheel.FixedUpdate.");
                }
                else
                    ModLog.Warn("[TyrePressure] Wheel.FixedUpdate not found.");

                DiagnosticsManager.Report("TyrePressure", true);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[TyrePressure] ApplyPatch: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "TyrePressure");
                DiagnosticsManager.Report("TyrePressure", false, ex.Message);
            }
        }
    }

    public static class TyrePressure_WheelPatch
    {
        private static PropertyInfo _rollFrictionProp = null;
        private static bool _searched = false;

        private static float _defaultFriction = -1f;

        public static void InvalidateDefault() { _defaultFriction = -1f; }

        public static void Postfix(Wheel __instance)
        {
            if (!TyrePressure.Enabled) return;

            float mult = TyrePressure.CachedMultiplier;
            if (mult == 1.0f) return;

            if (!UnityNull.Alive(__instance)) return;

            try
            {
                Transform t = __instance.transform;
                if (!UnityNull.Alive(t) || !UnityNull.Alive(t.parent)) return;
                if (!string.Equals(t.parent.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                if ((object)_rollFrictionProp == null && !_searched)
                {
                    _searched = true;
                    _rollFrictionProp = typeof(Wheel).GetProperty(
                        "WbmnXfG", BindingFlags.Public | BindingFlags.Instance);

                    if ((object)_rollFrictionProp != null)
                    {
                        ModLog.Debug("[TyrePressure] Prop WbmnXfG found.");
                    }
                    else
                    {
                        ModLog.Warn("[TyrePressure] Prop WbmnXfG not found -- dumping Wheel float props:");
                        var props = typeof(Wheel).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var p in props)
                            if (p.PropertyType.Equals(typeof(float)))
                                ModLog.Debug("[TyrePressure]   float prop: " + p.Name);
                        return;
                    }
                }

                if ((object)_rollFrictionProp == null) return;

                if (_defaultFriction < 0f)
                {
                    _defaultFriction = (float)_rollFrictionProp.GetValue(__instance, null);
                    ModLog.Debug("[TyrePressure] Default rollFriction=" + _defaultFriction.ToString("F4"));
                }

                _rollFrictionProp.SetValue(__instance, _defaultFriction * mult, null);
            }
            catch { }
        }
    }
}
