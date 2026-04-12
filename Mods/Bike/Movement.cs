using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class Movement
    {
        // ── Rotation Speed ────────────────────────────────────────────
        public static bool SpinEnabled   { get; private set; } = false;
        public static int  SpinLevel     { get; private set; } = 5;
        private static FieldInfo _spinField   = null;
        private static float     _spinDefault = -1f;

        // ── Hop Force ─────────────────────────────────────────────────
        public static bool HopEnabled    { get; private set; } = false;
        public static int  HopLevel      { get; private set; } = 5;
        private static FieldInfo _hopField    = null;
        private static float     _hopDefault  = -1f;

        // ── Wheelie Force ─────────────────────────────────────────────
        public static bool WheelieEnabled  { get; private set; } = false;
        public static int  WheelieLevel    { get; private set; } = 5;
        private static FieldInfo _wheelieField   = null;
        private static float     _wheelieDefault = -1f;

        // ── Lean Strength ─────────────────────────────────────────────
        public static bool LeanEnabled   { get; private set; } = false;
        public static int  LeanLevel     { get; private set; } = 5;
        private static FieldInfo _leanField   = null;
        private static float     _leanDefault = -1f;

        // Level 5 = 1× default. Range 1–10, step 0.2 → 0.4× to 2.8×
        private static float Mult(int level) { return 0.2f + (level - 1) * 0.3f; }

        // ── Rotation Speed ────────────────────────────────────────────
        public static void ToggleSpin()
        {
            SpinEnabled = !SpinEnabled;
            if (SpinEnabled) ApplySpin(); else RestoreSpin();
            MelonLogger.Msg("[Movement] Spin -> " + (SpinEnabled ? "ON" : "OFF"));
        }
        public static void SpinIncrease() { if (SpinLevel < 10) { SpinLevel++; if (SpinEnabled) ApplySpin(); } }
        public static void SpinDecrease() { if (SpinLevel > 1)  { SpinLevel--; if (SpinEnabled) ApplySpin(); } }
        public static void SetSpinLevel(int v) { SpinLevel = System.Math.Max(1, System.Math.Min(10, v)); if (SpinEnabled) ApplySpin(); }

        public static void ApplySpin()
        {
            if (!SpinEnabled) return;
            try
            {
                Cyclist c = GetCyclist();
                if ((object)c == null) return;
                if ((object)_spinField == null)
                    _spinField = FindField(c, "\u005EE\u005DVx\u0081\u0082");
                if ((object)_spinField == null) { MelonLogger.Warning("[Movement] Spin field not found."); return; }
                if (_spinDefault < 0f) _spinDefault = (float)_spinField.GetValue(c);
                _spinField.SetValue(c, _spinDefault * Mult(SpinLevel));
                MelonLogger.Msg("[Movement] Spin -> " + SpinLevel + " (" + (_spinDefault * Mult(SpinLevel)) + ")");
            }
            catch (System.Exception ex) { MelonLogger.Error("[Movement] ApplySpin: " + ex.Message); }
        }

        private static void RestoreSpin()
        {
            try
            {
                if (_spinDefault < 0f) return;
                Cyclist c = GetCyclist();
                if ((object)c == null || (object)_spinField == null) return;
                _spinField.SetValue(c, _spinDefault);
            }
            catch { }
        }

        // ── Hop Force ─────────────────────────────────────────────────
        public static void ToggleHop()
        {
            HopEnabled = !HopEnabled;
            if (HopEnabled) ApplyHop(); else RestoreHop();
            MelonLogger.Msg("[Movement] Hop -> " + (HopEnabled ? "ON" : "OFF"));
        }
        public static void HopIncrease() { if (HopLevel < 10) { HopLevel++; if (HopEnabled) ApplyHop(); } }
        public static void HopDecrease() { if (HopLevel > 1)  { HopLevel--; if (HopEnabled) ApplyHop(); } }
        public static void SetHopLevel(int v) { HopLevel = System.Math.Max(1, System.Math.Min(10, v)); if (HopEnabled) ApplyHop(); }

        public static void ApplyHop()
        {
            if (!HopEnabled) return;
            try
            {
                Cyclist c = GetCyclist();
                if ((object)c == null) return;
                if ((object)_hopField == null)
                    _hopField = FindField(c, "kV\u005B\u0083SnO");
                if ((object)_hopField == null) { MelonLogger.Warning("[Movement] Hop field not found."); return; }
                if (_hopDefault < 0f) _hopDefault = (float)_hopField.GetValue(c);
                _hopField.SetValue(c, _hopDefault * Mult(HopLevel));
                MelonLogger.Msg("[Movement] Hop -> " + HopLevel + " (" + (_hopDefault * Mult(HopLevel)) + ")");
            }
            catch (System.Exception ex) { MelonLogger.Error("[Movement] ApplyHop: " + ex.Message); }
        }

        private static void RestoreHop()
        {
            try
            {
                if (_hopDefault < 0f) return;
                Cyclist c = GetCyclist();
                if ((object)c == null || (object)_hopField == null) return;
                _hopField.SetValue(c, _hopDefault);
            }
            catch { }
        }

        // ── Wheelie Force ─────────────────────────────────────────────
        public static void ToggleWheelie()
        {
            WheelieEnabled = !WheelieEnabled;
            if (WheelieEnabled) ApplyWheelie(); else RestoreWheelie();
            MelonLogger.Msg("[Movement] Wheelie -> " + (WheelieEnabled ? "ON" : "OFF"));
        }
        public static void WheelieIncrease() { if (WheelieLevel < 10) { WheelieLevel++; if (WheelieEnabled) ApplyWheelie(); } }
        public static void WheelieDecrease() { if (WheelieLevel > 1)  { WheelieLevel--; if (WheelieEnabled) ApplyWheelie(); } }
        public static void SetWheelieLevel(int v) { WheelieLevel = System.Math.Max(1, System.Math.Min(10, v)); if (WheelieEnabled) ApplyWheelie(); }

        public static void ApplyWheelie()
        {
            if (!WheelieEnabled) return;
            try
            {
                Cyclist c = GetCyclist();
                if ((object)c == null) return;
                if ((object)_wheelieField == null)
                    _wheelieField = FindField(c, "\u007D\u0060\u0082zWt\u007C");
                if ((object)_wheelieField == null) { MelonLogger.Warning("[Movement] Wheelie field not found."); return; }
                if (_wheelieDefault < 0f) _wheelieDefault = (float)_wheelieField.GetValue(c);
                _wheelieField.SetValue(c, _wheelieDefault * Mult(WheelieLevel));
                MelonLogger.Msg("[Movement] Wheelie -> " + WheelieLevel + " (" + (_wheelieDefault * Mult(WheelieLevel)) + ")");
            }
            catch (System.Exception ex) { MelonLogger.Error("[Movement] ApplyWheelie: " + ex.Message); }
        }

        private static void RestoreWheelie()
        {
            try
            {
                if (_wheelieDefault < 0f) return;
                Cyclist c = GetCyclist();
                if ((object)c == null || (object)_wheelieField == null) return;
                _wheelieField.SetValue(c, _wheelieDefault);
            }
            catch { }
        }

        // ── Lean Strength ─────────────────────────────────────────────
        public static void ToggleLean()
        {
            LeanEnabled = !LeanEnabled;
            if (LeanEnabled) ApplyLean(); else RestoreLean();
            MelonLogger.Msg("[Movement] Lean -> " + (LeanEnabled ? "ON" : "OFF"));
        }
        public static void LeanIncrease() { if (LeanLevel < 10) { LeanLevel++; if (LeanEnabled) ApplyLean(); } }
        public static void LeanDecrease() { if (LeanLevel > 1)  { LeanLevel--; if (LeanEnabled) ApplyLean(); } }
        public static void SetLeanLevel(int v) { LeanLevel = System.Math.Max(1, System.Math.Min(10, v)); if (LeanEnabled) ApplyLean(); }

        public static void ApplyLean()
        {
            if (!LeanEnabled) return;
            try
            {
                Cyclist c = GetCyclist();
                if ((object)c == null) return;
                if ((object)_leanField == null)
                    _leanField = FindField(c, "Pl\u007Fvrb\u0082");
                if ((object)_leanField == null) { MelonLogger.Warning("[Movement] Lean field not found."); return; }
                if (_leanDefault < 0f) _leanDefault = (float)_leanField.GetValue(c);
                _leanField.SetValue(c, _leanDefault * Mult(LeanLevel));
                MelonLogger.Msg("[Movement] Lean -> " + LeanLevel + " (" + (_leanDefault * Mult(LeanLevel)) + ")");
            }
            catch (System.Exception ex) { MelonLogger.Error("[Movement] ApplyLean: " + ex.Message); }
        }

        private static void RestoreLean()
        {
            try
            {
                if (_leanDefault < 0f) return;
                Cyclist c = GetCyclist();
                if ((object)c == null || (object)_leanField == null) return;
                _leanField.SetValue(c, _leanDefault);
            }
            catch { }
        }

        // ── Scene reset ───────────────────────────────────────────────
        public static void Reset()
        {
            // Restore all before clearing — no player on next scene so do in-place
            SpinEnabled = false;   _spinDefault = -1f;    _spinField = null;
            HopEnabled = false;    _hopDefault = -1f;     _hopField = null;
            WheelieEnabled = false; _wheelieDefault = -1f; _wheelieField = null;
            LeanEnabled = false;   _leanDefault = -1f;    _leanField = null;
        }

        // ── Helpers ───────────────────────────────────────────────────
        private static Cyclist GetCyclist()
        {
            GameObject local = GameObject.Find("Player_Human");
            if ((object)local == null) return null;
            return local.GetComponentInChildren<Cyclist>();
        }

        private static FieldInfo FindField(Cyclist c, string name)
        {
            FieldInfo f = c.GetType().GetField(name,
                BindingFlags.Public | BindingFlags.Instance);
            if ((object)f != null)
                MelonLogger.Msg("[Movement] Found field: " + name);
            else
                MelonLogger.Warning("[Movement] Field not found: " + name);
            return f;
        }
    }
}
