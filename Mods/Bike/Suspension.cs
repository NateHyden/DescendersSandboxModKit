using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class Suspension
    {
        // ── Suspension Travel (xL\u007BgJGT on Wheel, default 0.5) ─────────────
        public static int TravelLevel { get; private set; } = 5;
        private static FieldInfo _travelField = null;
        private static float _travelDefault = -1f;

        // ── Spring Stiffness (p\u007EmkyX\u007B on Wheel, default 50) ──────────────
        public static int StiffnessLevel { get; private set; } = 5;
        private static FieldInfo _stiffField = null;
        private static float _stiffDefault = -1f;

        // ── Spring Damping (YrKDSPL on Wheel, default 5) ─────────────────
        public static int DampingLevel { get; private set; } = 5;
        private static FieldInfo _dampField = null;
        private static float _dampDefault = -1f;

        // Level 5 = default (1x), 1 = 0.2x, 10 = 2x
        private static float Mult(int level) { return level * 0.2f; }

        // ── Travel ────────────────────────────────────────────────────────
        public static void TravelIncrease() { if (TravelLevel < 10) { TravelLevel++; ApplyTravel(); } }
        public static void TravelDecrease() { if (TravelLevel > 1) { TravelLevel--; ApplyTravel(); } }
        public static void SetTravelLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > 10) level = 10;
            TravelLevel = level;
            ApplyTravel();
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
                    if ((object)_travelField == null) { MelonLogger.Warning("[Suspension] Travel field not found."); return; }
                    if (_travelDefault < 0f)
                        _travelDefault = (float)_travelField.GetValue(wheels[i]);
                    _travelField.SetValue(wheels[i], _travelDefault * Mult(TravelLevel));
                }
                MelonLogger.Msg("[Suspension] Travel level " + TravelLevel);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Suspension] ApplyTravel: " + ex.Message); }
        }

        // ── Stiffness ─────────────────────────────────────────────────────
        public static void StiffnessIncrease() { if (StiffnessLevel < 10) { StiffnessLevel++; ApplyStiffness(); } }
        public static void StiffnessDecrease() { if (StiffnessLevel > 1) { StiffnessLevel--; ApplyStiffness(); } }
        public static void SetStiffnessLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > 10) level = 10;
            StiffnessLevel = level;
            ApplyStiffness();
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
                    if ((object)_stiffField == null) { MelonLogger.Warning("[Suspension] Stiffness field not found."); return; }
                    if (_stiffDefault < 0f)
                        _stiffDefault = (float)_stiffField.GetValue(wheels[i]);
                    _stiffField.SetValue(wheels[i], _stiffDefault * Mult(StiffnessLevel));
                }
                MelonLogger.Msg("[Suspension] Stiffness level " + StiffnessLevel);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Suspension] ApplyStiffness: " + ex.Message); }
        }

        // ── Damping ───────────────────────────────────────────────────────
        public static void DampingIncrease() { if (DampingLevel < 10) { DampingLevel++; ApplyDamping(); } }
        public static void DampingDecrease() { if (DampingLevel > 1) { DampingLevel--; ApplyDamping(); } }
        public static void SetDampingLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > 10) level = 10;
            DampingLevel = level;
            ApplyDamping();
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
                    if ((object)_dampField == null) { MelonLogger.Warning("[Suspension] Damping field not found."); return; }
                    if (_dampDefault < 0f)
                        _dampDefault = (float)_dampField.GetValue(wheels[i]);
                    _dampField.SetValue(wheels[i], _dampDefault * Mult(DampingLevel));
                }
                MelonLogger.Msg("[Suspension] Damping level " + DampingLevel);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Suspension] ApplyDamping: " + ex.Message); }
        }

        private static Wheel[] GetWheels()
        {
            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) { MelonLogger.Warning("[Suspension] Player_Human not found."); return null; }
            Wheel[] wheels = player.GetComponentsInChildren<Wheel>();
            if (wheels == null || wheels.Length == 0) { MelonLogger.Warning("[Suspension] No Wheel components found."); return null; }
            return wheels;
        }
    }
}