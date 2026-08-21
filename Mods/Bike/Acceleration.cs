using System.Reflection;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class Acceleration
    {
        public static bool Enabled { get; private set; } = false;
        public static int Level { get; private set; } = 1;

        private static readonly string AccelFieldName = "cPkCE^\u0081";
        private static float _originalValue = -1f;
        private static FieldInfo _field = null;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Apply();
            else Restore();
            ModLog.Feedback("[Acceleration] -> " + (Enabled ? "ON (level " + Level + ")" : "OFF"));
        }

        public const int MaxLevel = 20;

        public static void Increase() { if (Level < MaxLevel) Level++; if (Enabled) Apply(); }
        public static void Decrease() { if (Level > 1) Level--; if (Enabled) Apply(); }

        public static void SetLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > MaxLevel) level = MaxLevel;
            Level = level;
            if (Enabled) Apply();
        }

        public static void Apply()
        {
            if (!Enabled) return;
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) return;
                Vehicle vehicle = player.GetComponent<Vehicle>();
                if ((object)vehicle == null) return;

                if ((object)_field == null)
                    _field = vehicle.GetType().GetField(AccelFieldName,
                        BindingFlags.Public | BindingFlags.Instance);
                if ((object)_field == null) { ModLog.Warn("[Acceleration] Field not found."); return; }

                if (_originalValue < 0f)
                {
                    object val = _field.GetValue(vehicle);
                    if (val is float f) _originalValue = f;
                    else return;
                }

                float multiplier = 1f + (Level - 1) * 0.5f;
                _field.SetValue(vehicle, _originalValue * multiplier);
                ModLog.Feedback("[Acceleration] Level " + Level + " -> " + (_originalValue * multiplier));
            }
            catch (System.Exception ex) { MelonLogger.Error("[Acceleration] Apply: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Acceleration"); }
        }

        public static void Tick()
        {
            if (!Enabled) return;
            try
            {
                GameObject player = PlayerCache.PlayerHuman;
                if ((object)player == null) return;
                Vehicle vehicle = player.GetComponent<Vehicle>();
                if ((object)vehicle == null) return;
                if ((object)_field == null || _originalValue < 0f) return;

                float multiplier = 1f + (Level - 1) * 0.5f;
                float target = _originalValue * multiplier;
                float current = (float)_field.GetValue(vehicle);
                if (Mathf.Abs(current - target) > 0.01f)
                    _field.SetValue(vehicle, target);
            }
            catch { }
        }

        private static void Restore()
        {
            try
            {
                if (_originalValue < 0f) return;
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) return;
                Vehicle vehicle = player.GetComponent<Vehicle>();
                if ((object)vehicle == null) return;
                if ((object)_field == null) return;
                _field.SetValue(vehicle, _originalValue);
                ModLog.Debug("[Acceleration] Restored default: " + _originalValue);
            }
            catch { }
        }

        public static void Reset()
        {
            if (Enabled) Restore();
            Enabled = false;
            _originalValue = -1f;
            _field = null;
        }
    }
}
