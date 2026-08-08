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
            ModLog.Feedback("[Acceleration] -> " + (Enabled ? "ON (level " + Level + ")" : "OFF"));
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
                ModLog.Feedback("[Acceleration] Level " + Level + " -> " + (_originalValue * multiplier));
            }
            catch (System.Exception ex) { MelonLogger.Error("[Acceleration] Apply: " + ex.Message); }
        }

        // Confirmed via scene dump 2026-08-04: the game's own bike-stat init runs
        // AFTER our apply-once reapply on scene load and silently overwrites this
        // field back to the raw default (dump showed 14, our log said we'd set 21 -
        // no exception, no error, just clobbered). A single apply-on-Player_Human-
        // found isn't enough to win that race reliably. Re-enforce every frame
        // instead, same pattern as WideTyres.Tick()/BikeDamage.Tick() elsewhere in
        // this project. Called from OnLateUpdate so it runs after the game's own
        // Update-phase logic for that frame.
        public static void Tick()
        {
            if (!Enabled) return;
            try
            {
                GameObject player = PlayerCache.PlayerHuman;
                if ((object)player == null) return;
                Vehicle vehicle = player.GetComponent<Vehicle>();
                if ((object)vehicle == null) return;
                if ((object)_field == null || _originalValue < 0f) return; // Apply() handles first-time setup

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
