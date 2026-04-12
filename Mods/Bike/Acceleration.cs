using System.Reflection;
using MelonLoader;
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
            MelonLogger.Msg("[Acceleration] -> " + (Enabled ? "ON (level " + Level + ")" : "OFF"));
        }

        public static void Increase() { if (Level < 10) Level++; if (Enabled) Apply(); }
        public static void Decrease() { if (Level > 1) Level--; if (Enabled) Apply(); }

        public static void SetLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > 10) level = 10;
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
                if ((object)_field == null) { MelonLogger.Warning("[Acceleration] Field not found."); return; }

                if (_originalValue < 0f)
                {
                    object val = _field.GetValue(vehicle);
                    if (val is float f) _originalValue = f;
                    else return;
                }

                float multiplier = 1f + (Level - 1) * 0.5f;
                _field.SetValue(vehicle, _originalValue * multiplier);
                MelonLogger.Msg("[Acceleration] Level " + Level + " -> " + (_originalValue * multiplier));
            }
            catch (System.Exception ex) { MelonLogger.Error("[Acceleration] Apply: " + ex.Message); }
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
                MelonLogger.Msg("[Acceleration] Restored default: " + _originalValue);
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
