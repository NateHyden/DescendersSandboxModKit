using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class MaxSpeedMultiplier
    {
        public static bool Enabled { get; private set; } = false;
        public static int Level { get; private set; } = 1;

        // ei[frnu = drag coefficient, default 0.06
        // Lower drag = higher top speed
        private static readonly float DefaultDrag = 0.06f;
        private static FieldInfo _field = null;
        private static bool _fieldCached = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Apply();
            else Restore();
            MelonLogger.Msg("[MaxSpeed] -> " + (Enabled ? "ON (level " + Level + ")" : "OFF"));
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

        private static FieldInfo FindField(Vehicle vehicle)
        {
            if (_fieldCached) return _field;
            _fieldCached = true;
            FieldInfo[] fields = vehicle.GetType().GetFields(
                BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                if (!string.Equals(fields[i].FieldType.Name, "Single",
                    System.StringComparison.Ordinal)) continue;
                object val = fields[i].GetValue(vehicle);
                if ((object)val == null) continue;
                float f = (float)val;
                // Drag field is ~0.06 — scan range 0.001 to 0.12
                if (f >= 0.001f && f <= 0.12f)
                {
                    _field = fields[i];
                    MelonLogger.Msg("[MaxSpeed] Found drag field: " + fields[i].Name + " = " + f);
                    return _field;
                }
            }
            MelonLogger.Warning("[MaxSpeed] Drag field not found.");
            return null;
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
                FieldInfo field = FindField(vehicle);
                if ((object)field == null) return;
                // Higher level = lower drag = higher top speed
                float multiplier = 1f + (Level - 1) * 0.5f;
                float newDrag = DefaultDrag / multiplier;
                field.SetValue(vehicle, newDrag);
                MelonLogger.Msg("[MaxSpeed] Level " + Level + " drag -> " + newDrag);
            }
            catch (System.Exception ex) { MelonLogger.Error("[MaxSpeed] Apply: " + ex.Message); }
        }

        private static void Restore()
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) return;
                Vehicle vehicle = player.GetComponent<Vehicle>();
                if ((object)vehicle == null) return;
                if ((object)_field == null) return;
                _field.SetValue(vehicle, DefaultDrag);
                MelonLogger.Msg("[MaxSpeed] Restored default drag: " + DefaultDrag);
            }
            catch { }
        }

        public static void Reset()
        {
            if (Enabled) Restore();
            Enabled = false;
            _field = null;
            _fieldCached = false;
        }
    }
}
