using System.Reflection;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class Suspension
    {
        public static int TravelLevel { get; private set; } = 5;
        private static FieldInfo _travelField = null;
        private static float _travelDefault = -1f;

        public static int StiffnessLevel { get; private set; } = 5;
        private static FieldInfo _stiffField = null;
        private static float _stiffDefault = -1f;

        public static int DampingLevel { get; private set; } = 5;
        private static FieldInfo _dampField = null;
        private static float _dampDefault = -1f;

        private static float Mult(int level) { return level * 0.2f; }

        /// <summary>UI: stock Level 5 = 0%.</summary>
        public static string PercentDisplay(int level)
        {
            return DialDisplay.OffsetPercent(level, 5, 1, 10);
        }

        // ── Travel ────────────────────────────────────────────────────────
        public static void TravelIncrease()
        {
            if (TravelLevel >= 10) return;
            bool was = TravelLevel != 5;
            TravelLevel++;
            ApplyTravel();
            ModLog.Dial("Travel", was, TravelLevel != 5);
        }
        public static void TravelDecrease()
        {
            if (TravelLevel <= 1) return;
            bool was = TravelLevel != 5;
            TravelLevel--;
            ApplyTravel();
            ModLog.Dial("Travel", was, TravelLevel != 5);
        }
        public static void SetTravelLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > 10) level = 10;
            bool was = TravelLevel != 5;
            TravelLevel = level;
            ApplyTravel();
            ModLog.Dial("Travel", was, TravelLevel != 5);
        }

        public static void ApplyTravel()
        {
            try
            {
                Wheel[] wheels = GetWheels();
                if (wheels == null) return;
                for (int i = 0; i < wheels.Length; i++)
                {
                    if ((object)_travelField == null)
                        _travelField = wheels[i].GetType().GetField("xL\u007BgJGT",
                            BindingFlags.Public | BindingFlags.Instance);
                    if ((object)_travelField == null) { ModLog.Warn("[Suspension] Travel field not found."); return; }
                    if (_travelDefault < 0f)
                        _travelDefault = (float)_travelField.GetValue(wheels[i]);
                    _travelField.SetValue(wheels[i], _travelDefault * Mult(TravelLevel));
                }
                ModLog.Debug("[Suspension] Travel level " + TravelLevel);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Suspension] ApplyTravel: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Suspension"); }
        }

        // ── Stiffness ─────────────────────────────────────────────────────
        public static void StiffnessIncrease()
        {
            if (StiffnessLevel >= 10) return;
            bool was = StiffnessLevel != 5;
            StiffnessLevel++;
            ApplyStiffness();
            ModLog.Dial("Stiffness", was, StiffnessLevel != 5);
        }
        public static void StiffnessDecrease()
        {
            if (StiffnessLevel <= 1) return;
            bool was = StiffnessLevel != 5;
            StiffnessLevel--;
            ApplyStiffness();
            ModLog.Dial("Stiffness", was, StiffnessLevel != 5);
        }
        public static void SetStiffnessLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > 10) level = 10;
            bool was = StiffnessLevel != 5;
            StiffnessLevel = level;
            ApplyStiffness();
            ModLog.Dial("Stiffness", was, StiffnessLevel != 5);
        }

        public static void ApplyStiffness()
        {
            try
            {
                Wheel[] wheels = GetWheels();
                if (wheels == null) return;
                for (int i = 0; i < wheels.Length; i++)
                {
                    if ((object)_stiffField == null)
                        _stiffField = wheels[i].GetType().GetField("p\u007EmkyX\u007B",
                            BindingFlags.Public | BindingFlags.Instance);
                    if ((object)_stiffField == null) { ModLog.Warn("[Suspension] Stiffness field not found."); return; }
                    if (_stiffDefault < 0f)
                        _stiffDefault = (float)_stiffField.GetValue(wheels[i]);
                    _stiffField.SetValue(wheels[i], _stiffDefault * Mult(StiffnessLevel));
                }
                ModLog.Debug("[Suspension] Stiffness level " + StiffnessLevel);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Suspension] ApplyStiffness: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Suspension"); }
        }

        // ── Damping ───────────────────────────────────────────────────────
        public static void DampingIncrease()
        {
            if (DampingLevel >= 10) return;
            bool was = DampingLevel != 5;
            DampingLevel++;
            ApplyDamping();
            ModLog.Dial("Damping", was, DampingLevel != 5);
        }
        public static void DampingDecrease()
        {
            if (DampingLevel <= 1) return;
            bool was = DampingLevel != 5;
            DampingLevel--;
            ApplyDamping();
            ModLog.Dial("Damping", was, DampingLevel != 5);
        }
        public static void SetDampingLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > 10) level = 10;
            bool was = DampingLevel != 5;
            DampingLevel = level;
            ApplyDamping();
            ModLog.Dial("Damping", was, DampingLevel != 5);
        }

        public static void ApplyDamping()
        {
            try
            {
                Wheel[] wheels = GetWheels();
                if (wheels == null) return;
                for (int i = 0; i < wheels.Length; i++)
                {
                    if ((object)_dampField == null)
                        _dampField = wheels[i].GetType().GetField("YrKDSPL",
                            BindingFlags.Public | BindingFlags.Instance);
                    if ((object)_dampField == null) { ModLog.Warn("[Suspension] Damping field not found."); return; }
                    if (_dampDefault < 0f)
                        _dampDefault = (float)_dampField.GetValue(wheels[i]);
                    _dampField.SetValue(wheels[i], _dampDefault * Mult(DampingLevel));
                }
                ModLog.Debug("[Suspension] Damping level " + DampingLevel);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Suspension] ApplyDamping: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Suspension"); }
        }

        private static Wheel[] GetWheels()
        {
            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) { ModLog.Debug("[Suspension] Player_Human not found."); return null; }
            Wheel[] wheels = player.GetComponentsInChildren<Wheel>();
            if (wheels == null || wheels.Length == 0) { ModLog.Debug("[Suspension] No Wheel components found."); return null; }
            return wheels;
        }
    }
}

