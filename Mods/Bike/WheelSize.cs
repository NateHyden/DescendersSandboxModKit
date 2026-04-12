using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class WheelSize
    {
        // ── State ─────────────────────────────────────────────────────
        public static bool IsEnabled = false;
        public static int Level = 10;
        public static int Mode = 0; // legacy 5-mode, kept for compat
        public static bool IsIndividualMode = false;
        public static int FrontLevel = 10;
        public static int RearLevel = 10;

        // ── Scale tables ──────────────────────────────────────────────
        public static readonly float[] ScaleLevels = {
            0.10f, 0.15f, 0.20f, 0.25f, 0.35f, 0.50f, 0.65f, 0.75f, 0.90f, 1.00f,
            1.20f, 1.50f, 1.80f, 2.20f, 2.60f, 3.00f, 3.50f, 4.00f, 5.00f, 6.00f
        };
        private static readonly float[] LegacyScales = { 1.0f, 0.25f, 0.5f, 1.5f, 3.0f };
        private static readonly string[] LegacyLabels = { "Default", "Tiny", "Small", "Large", "Huge" };

        // ── Reflection cache ──────────────────────────────────────────
        private static System.Reflection.FieldInfo _wheelRadiusField = null;
        private static float _defaultRadiusFront = -1f;
        private static float _defaultRadiusBack = -1f;
        private static System.Reflection.FieldInfo _backBoneField = null;
        private static System.Reflection.FieldInfo _frontBoneField = null;
        private static Transform _cachedFrontBone = null;
        private static Transform _cachedBackBone = null;

        // ── Tick (OnLateUpdate) ───────────────────────────────────────
        public static void Tick()
        {
            try
            {
                if (IsIndividualMode)
                {
                    float fs = ScaleLevels[FrontLevel - 1];
                    float rs = ScaleLevels[RearLevel - 1];
                    if ((object)_cachedFrontBone != null) _cachedFrontBone.localScale = new Vector3(fs, fs, fs);
                    if ((object)_cachedBackBone != null) _cachedBackBone.localScale = new Vector3(rs, rs, rs);
                }
                else if (IsEnabled && Level != 10)
                {
                    float scale = ScaleLevels[Level - 1];
                    if ((object)_cachedFrontBone != null) _cachedFrontBone.localScale = new Vector3(scale, scale, scale);
                    if ((object)_cachedBackBone != null) _cachedBackBone.localScale = new Vector3(scale, scale, scale);
                }
            }
            catch { }
        }

        // ── Apply level (both wheels) ─────────────────────────────────
        public static void ApplyLevel(int level)
        {
            Level = Mathf.Clamp(level, 1, 20);
            if (Level == 10) SetLegacyMode(0);
            else ApplyScaleDirectly(ScaleLevels[Level - 1]);
        }

        // ── Apply individual wheel levels ─────────────────────────────
        public static void ApplyIndividualLevels()
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) return;

                Transform bikeModel = player.transform.Find("BikeModel");
                if ((object)bikeModel != null)
                {
                    BikeAnimation bikeAnim = bikeModel.GetComponent<BikeAnimation>();
                    if ((object)bikeAnim != null)
                    {
                        CacheBoneFields(bikeAnim);
                        if ((object)_frontBoneField != null)
                        {
                            Transform fb = _frontBoneField.GetValue(bikeAnim) as Transform;
                            if ((object)fb != null) { float fs = ScaleLevels[FrontLevel - 1]; fb.localScale = new Vector3(fs, fs, fs); _cachedFrontBone = fb; }
                        }
                        if ((object)_backBoneField != null)
                        {
                            Transform bb = _backBoneField.GetValue(bikeAnim) as Transform;
                            if ((object)bb != null) { float rs = ScaleLevels[RearLevel - 1]; bb.localScale = new Vector3(rs, rs, rs); _cachedBackBone = bb; }
                        }
                    }
                }
                Wheel[] wheels = player.GetComponentsInChildren<Wheel>();
                if (wheels != null)
                {
                    for (int i = 0; i < wheels.Length; i++)
                    {
                        if ((object)_wheelRadiusField == null)
                            _wheelRadiusField = wheels[i].GetType().GetField("HqsqNkJ", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if ((object)_wheelRadiusField == null) break;
                        bool isFront = string.Equals(wheels[i].gameObject.name, "wheel_front", System.StringComparison.Ordinal);
                        if (isFront && _defaultRadiusFront < 0f) _defaultRadiusFront = (float)_wheelRadiusField.GetValue(wheels[i]);
                        if (!isFront && _defaultRadiusBack < 0f) _defaultRadiusBack = (float)_wheelRadiusField.GetValue(wheels[i]);
                        float def = isFront ? _defaultRadiusFront : _defaultRadiusBack;
                        float lv = isFront ? FrontLevel : RearLevel;
                        float sc = ScaleLevels[(int)lv - 1];
                        if (def > 0f) _wheelRadiusField.SetValue(wheels[i], def * sc);
                    }
                }
                MelonLogger.Msg("[WheelSize] Individual F=" + FrontLevel + " R=" + RearLevel);
            }
            catch (System.Exception ex) { MelonLogger.Error("[WheelSize] ApplyIndividualLevels: " + ex.Message); }
        }

        // ── Save/load helpers ─────────────────────────────────────────
        public static void ApplyFromSave(bool enabled, int level, int legacyMode)
        {
            IsEnabled = enabled;
            if (level != 10 && level >= 1 && level <= 20) ApplyLevel(level);
            else if (legacyMode != 0) ApplyLegacy(enabled, legacyMode);
            else SetLegacyMode(0);
        }

        public static void ApplyIndividualFromSave(int frontLevel, int rearLevel)
        {
            FrontLevel = Mathf.Clamp(frontLevel, 1, 20);
            RearLevel = Mathf.Clamp(rearLevel, 1, 20);
            IsIndividualMode = true;
            IsEnabled = false;
            ApplyIndividualLevels();
            MelonLogger.Msg("[WheelSize] IndividualFromSave F=" + FrontLevel + " R=" + RearLevel);
        }

        public static void ApplyLegacy(bool enabled, int mode)
        {
            int oldMode = Mode;
            IsEnabled = enabled;
            Mode = mode;
            if (enabled && mode != 0) SetLegacyMode(mode);
            else if (!enabled && oldMode != 0) SetLegacyMode(0);
        }

        // ── Reset ─────────────────────────────────────────────────────
        public static void Reset()
        {
            if (Mode != 0) try { SetLegacyMode(0); } catch { }
            if (IsEnabled || IsIndividualMode) try { ApplyScaleDirectly(1f); } catch { }
            Mode = 0; Level = 10; IsEnabled = false;
            FrontLevel = 10; RearLevel = 10; IsIndividualMode = false;
            _cachedFrontBone = null; _cachedBackBone = null;
            _backBoneField = null; _frontBoneField = null;
            _wheelRadiusField = null;
            _defaultRadiusFront = -1f; _defaultRadiusBack = -1f;
        }

        // ── Internal ──────────────────────────────────────────────────
        public static void ApplyScaleDirectly(float scale)
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) return;
                Transform bikeModel = player.transform.Find("BikeModel");
                if ((object)bikeModel != null)
                {
                    BikeAnimation bikeAnim = bikeModel.GetComponent<BikeAnimation>();
                    if ((object)bikeAnim != null)
                    {
                        CacheBoneFields(bikeAnim);
                        if ((object)_backBoneField != null) { Transform bb = _backBoneField.GetValue(bikeAnim) as Transform; if ((object)bb != null) { bb.localScale = new Vector3(scale, scale, scale); _cachedBackBone = bb; } }
                        if ((object)_frontBoneField != null) { Transform fb = _frontBoneField.GetValue(bikeAnim) as Transform; if ((object)fb != null) { fb.localScale = new Vector3(scale, scale, scale); _cachedFrontBone = fb; } }
                    }
                }
                Wheel[] wheels = player.GetComponentsInChildren<Wheel>();
                if (wheels != null)
                {
                    for (int i = 0; i < wheels.Length; i++)
                    {
                        if ((object)_wheelRadiusField == null)
                            _wheelRadiusField = wheels[i].GetType().GetField("HqsqNkJ", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if ((object)_wheelRadiusField == null) break;
                        bool isFront = string.Equals(wheels[i].gameObject.name, "wheel_front", System.StringComparison.Ordinal);
                        if (isFront && _defaultRadiusFront < 0f) _defaultRadiusFront = (float)_wheelRadiusField.GetValue(wheels[i]);
                        else if (!isFront && _defaultRadiusBack < 0f) _defaultRadiusBack = (float)_wheelRadiusField.GetValue(wheels[i]);
                        float def = isFront ? _defaultRadiusFront : _defaultRadiusBack;
                        if (def > 0f) _wheelRadiusField.SetValue(wheels[i], def * scale);
                    }
                }
                MelonLogger.Msg("[WheelSize] Level=" + Level + " scale=" + scale);
            }
            catch (System.Exception ex) { MelonLogger.Error("[WheelSize] ApplyScaleDirectly: " + ex.Message); }
        }

        private static void SetLegacyMode(int mode)
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) return;
                float scale = LegacyScales[mode];
                Transform bikeModel = player.transform.Find("BikeModel");
                if ((object)bikeModel != null)
                {
                    BikeAnimation bikeAnim = bikeModel.GetComponent<BikeAnimation>();
                    if ((object)bikeAnim != null)
                    {
                        CacheBoneFields(bikeAnim);
                        if ((object)_backBoneField != null) { Transform bb = _backBoneField.GetValue(bikeAnim) as Transform; if ((object)bb != null) { bb.localScale = new Vector3(scale, scale, scale); _cachedBackBone = bb; } }
                        if ((object)_frontBoneField != null) { Transform fb = _frontBoneField.GetValue(bikeAnim) as Transform; if ((object)fb != null) { fb.localScale = new Vector3(scale, scale, scale); _cachedFrontBone = fb; } }
                    }
                }
                Wheel[] wheels = player.GetComponentsInChildren<Wheel>();
                if (wheels != null)
                {
                    for (int i = 0; i < wheels.Length; i++)
                    {
                        if ((object)_wheelRadiusField == null)
                            _wheelRadiusField = wheels[i].GetType().GetField("HqsqNkJ", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if ((object)_wheelRadiusField == null) break;
                        bool isFront = string.Equals(wheels[i].gameObject.name, "wheel_front", System.StringComparison.Ordinal);
                        if (isFront && _defaultRadiusFront < 0f) _defaultRadiusFront = (float)_wheelRadiusField.GetValue(wheels[i]);
                        else if (!isFront && _defaultRadiusBack < 0f) _defaultRadiusBack = (float)_wheelRadiusField.GetValue(wheels[i]);
                        float def = isFront ? _defaultRadiusFront : _defaultRadiusBack;
                        if (def > 0f) _wheelRadiusField.SetValue(wheels[i], def * scale);
                    }
                }
                Mode = mode;
                if (mode == 0) { _cachedFrontBone = null; _cachedBackBone = null; }
                MelonLogger.Msg("[WheelSize] Legacy -> " + LegacyLabels[mode]);
            }
            catch (System.Exception ex) { MelonLogger.Error("[WheelSize] SetLegacyMode: " + ex.Message); }
        }

        private static void CacheBoneFields(BikeAnimation anim)
        {
            if ((object)_backBoneField == null)
                _backBoneField = typeof(BikeAnimation).GetField("YLzyVuM", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if ((object)_frontBoneField == null)
                _frontBoneField = typeof(BikeAnimation).GetField("RCNLpue", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        }
    }
}
