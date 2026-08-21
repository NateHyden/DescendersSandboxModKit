using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class WheelSize
    {
        public static int Level = 10;
        public static int Mode = 0;
        public static bool IsIndividualMode = false;
        public static int FrontLevel = 10;
        public static int RearLevel = 10;

        /// <summary>True when either combined or individual size is off stock (0%).</summary>
        public static bool IsModified
        {
            get
            {
                if (IsIndividualMode)
                    return FrontLevel != 10 || RearLevel != 10;
                return Level != 10;
            }
        }

        // Kept for save/stats compatibility — same meaning as IsModified.
        public static bool IsEnabled
        {
            get { return IsModified; }
            set { }
        }

        public static string PercentDisplay(int level)
        {
            return DialDisplay.OffsetPercent(level, 10, 1, 20);
        }

        public static readonly float[] ScaleLevels = {
            0.10f, 0.15f, 0.20f, 0.25f, 0.35f, 0.50f, 0.65f, 0.75f, 0.90f, 1.00f,
            1.20f, 1.50f, 1.80f, 2.20f, 2.60f, 3.00f, 3.50f, 4.00f, 5.00f, 6.00f
        };
        private static readonly float[] LegacyScales = { 1.0f, 0.25f, 0.5f, 1.5f, 3.0f };
        private static readonly string[] LegacyLabels = { "Default", "Tiny", "Small", "Large", "Huge" };

        private static System.Reflection.FieldInfo _wheelRadiusField = null;
        private static float _defaultRadiusFront = -1f;
        private static float _defaultRadiusBack = -1f;
        private static System.Reflection.FieldInfo _backBoneField = null;
        private static System.Reflection.FieldInfo _frontBoneField = null;
        private static Transform _cachedFrontBone = null;
        private static Transform _cachedBackBone = null;

        public static void Tick()
        {
            try
            {
                if (!IsModified) return;

                if (IsIndividualMode)
                {
                    float fs = ScaleLevels[FrontLevel - 1];
                    float rs = ScaleLevels[RearLevel - 1];
                    if (UnityNull.Alive(_cachedFrontBone)) _cachedFrontBone.localScale = new Vector3(fs, fs, fs);
                    else _cachedFrontBone = null;
                    if (UnityNull.Alive(_cachedBackBone)) _cachedBackBone.localScale = new Vector3(rs, rs, rs);
                    else _cachedBackBone = null;
                }
                else
                {
                    float scale = ScaleLevels[Level - 1];
                    if (UnityNull.Alive(_cachedFrontBone)) _cachedFrontBone.localScale = new Vector3(scale, scale, scale);
                    else _cachedFrontBone = null;
                    if (UnityNull.Alive(_cachedBackBone)) _cachedBackBone.localScale = new Vector3(scale, scale, scale);
                    else _cachedBackBone = null;
                }
            }
            catch { }
        }

        public static void Increase()
        {
            if (Level >= 20) return;
            bool was = IsModified;
            Level++;
            ApplyCombined();
            ModLog.Dial("Wheel Size", was, IsModified);
        }

        public static void Decrease()
        {
            if (Level <= 1) return;
            bool was = IsModified;
            Level--;
            ApplyCombined();
            ModLog.Dial("Wheel Size", was, IsModified);
        }

        public static void IncreaseFront()
        {
            if (FrontLevel >= 20) return;
            bool wasCombined = IsModified;
            bool wasFront = FrontLevel != 10;
            FrontLevel++;
            ApplyIndividual();
            ModLog.Dial("Front Wheel Size", wasFront, FrontLevel != 10);
            ModLog.Dial("Wheel Size", wasCombined, IsModified);
        }

        public static void DecreaseFront()
        {
            if (FrontLevel <= 1) return;
            bool wasCombined = IsModified;
            bool wasFront = FrontLevel != 10;
            FrontLevel--;
            ApplyIndividual();
            ModLog.Dial("Front Wheel Size", wasFront, FrontLevel != 10);
            ModLog.Dial("Wheel Size", wasCombined, IsModified);
        }

        public static void IncreaseRear()
        {
            if (RearLevel >= 20) return;
            bool wasCombined = IsModified;
            bool wasRear = RearLevel != 10;
            RearLevel++;
            ApplyIndividual();
            ModLog.Dial("Rear Wheel Size", wasRear, RearLevel != 10);
            ModLog.Dial("Wheel Size", wasCombined, IsModified);
        }

        public static void DecreaseRear()
        {
            if (RearLevel <= 1) return;
            bool wasCombined = IsModified;
            bool wasRear = RearLevel != 10;
            RearLevel--;
            ApplyIndividual();
            ModLog.Dial("Rear Wheel Size", wasRear, RearLevel != 10);
            ModLog.Dial("Wheel Size", wasCombined, IsModified);
        }

        public static void ApplyLevel(int level)
        {
            bool was = IsModified;
            Level = Mathf.Clamp(level, 1, 20);
            ApplyCombined();
            ModLog.Dial("Wheel Size", was, IsModified);
        }

        private static void ApplyCombined()
        {
            IsIndividualMode = false;
            FrontLevel = Level;
            RearLevel = Level;
            ApplyScaleDirectly(ScaleLevels[Level - 1]);
            if (Level == 10)
                ClearBoneCache();
        }

        private static void ApplyIndividual()
        {
            IsIndividualMode = true;
            if (FrontLevel == 10 && RearLevel == 10)
            {
                Level = 10;
                IsIndividualMode = false;
                ApplyScaleDirectly(1f);
                ClearBoneCache();
                return;
            }
            ApplyIndividualLevels();
        }

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
                            if ((object)fb != null)
                            {
                                float fs = ScaleLevels[FrontLevel - 1];
                                fb.localScale = new Vector3(fs, fs, fs);
                                _cachedFrontBone = fb;
                            }
                        }
                        if ((object)_backBoneField != null)
                        {
                            Transform bb = _backBoneField.GetValue(bikeAnim) as Transform;
                            if ((object)bb != null)
                            {
                                float rs = ScaleLevels[RearLevel - 1];
                                bb.localScale = new Vector3(rs, rs, rs);
                                _cachedBackBone = bb;
                            }
                        }
                    }
                }
                ApplyRadii(ScaleLevels[FrontLevel - 1], ScaleLevels[RearLevel - 1]);
                ModLog.Debug("[WheelSize] Individual F=" + FrontLevel + " R=" + RearLevel);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[WheelSize] ApplyIndividualLevels: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "WheelSize");
            }
        }

        public static void ApplyFromSave(bool enabled, int level, int legacyMode)
        {
            if (level != 10 && level >= 1 && level <= 20)
                ApplyLevel(level);
            else if (legacyMode != 0)
                ApplyLegacy(true, legacyMode);
            else
                Reset();
        }

        public static void ApplyIndividualFromSave(int frontLevel, int rearLevel)
        {
            FrontLevel = Mathf.Clamp(frontLevel, 1, 20);
            RearLevel = Mathf.Clamp(rearLevel, 1, 20);
            if (FrontLevel == 10 && RearLevel == 10)
            {
                Reset();
                return;
            }
            IsIndividualMode = true;
            Level = 10;
            ApplyIndividualLevels();
            ModLog.Debug("[WheelSize] IndividualFromSave F=" + FrontLevel + " R=" + RearLevel);
        }

        public static void ApplyLegacy(bool enabled, int mode)
        {
            Mode = mode;
            if (mode != 0) SetLegacyMode(mode);
            else Reset();
        }

        public static void Reset()
        {
            bool was = IsModified;
            try { ApplyScaleDirectly(1f); } catch { }
            Mode = 0;
            Level = 10;
            FrontLevel = 10;
            RearLevel = 10;
            IsIndividualMode = false;
            ClearBoneCache();
            _wheelRadiusField = null;
            _defaultRadiusFront = -1f;
            _defaultRadiusBack = -1f;
            ModLog.Dial("Wheel Size", was, false);
            ModLog.Dial("Front Wheel Size", was, false);
            ModLog.Dial("Rear Wheel Size", was, false);
        }

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
                        if ((object)_backBoneField != null)
                        {
                            Transform bb = _backBoneField.GetValue(bikeAnim) as Transform;
                            if ((object)bb != null)
                            {
                                bb.localScale = new Vector3(scale, scale, scale);
                                _cachedBackBone = bb;
                            }
                        }
                        if ((object)_frontBoneField != null)
                        {
                            Transform fb = _frontBoneField.GetValue(bikeAnim) as Transform;
                            if ((object)fb != null)
                            {
                                fb.localScale = new Vector3(scale, scale, scale);
                                _cachedFrontBone = fb;
                            }
                        }
                    }
                }
                ApplyRadii(scale, scale);
                ModLog.Debug("[WheelSize] Level=" + Level + " scale=" + scale);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[WheelSize] ApplyScaleDirectly: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "WheelSize");
            }
        }

        private static void ApplyRadii(float frontScale, float rearScale)
        {
            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) return;
            Wheel[] wheels = player.GetComponentsInChildren<Wheel>();
            if (wheels == null) return;

            for (int i = 0; i < wheels.Length; i++)
            {
                if ((object)_wheelRadiusField == null)
                    _wheelRadiusField = wheels[i].GetType().GetField("HqsqNkJ",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)_wheelRadiusField == null) break;

                bool isFront = string.Equals(wheels[i].gameObject.name, "wheel_front",
                    System.StringComparison.Ordinal);
                float current = (float)_wheelRadiusField.GetValue(wheels[i]);

                if (isFront)
                {
                    if (_defaultRadiusFront < 0f)
                        _defaultRadiusFront = EstimateDefaultRadius(current, frontScale);
                    if (_defaultRadiusFront > 0f)
                        _wheelRadiusField.SetValue(wheels[i], _defaultRadiusFront * frontScale);
                }
                else
                {
                    if (_defaultRadiusBack < 0f)
                        _defaultRadiusBack = EstimateDefaultRadius(current, rearScale);
                    if (_defaultRadiusBack > 0f)
                        _wheelRadiusField.SetValue(wheels[i], _defaultRadiusBack * rearScale);
                }
            }
        }

        /// <summary>
        /// If we never saw stock radius, reverse out the current scale so we
        /// don't lock in an already-scaled value as "default" (causes floating bike).
        /// </summary>
        private static float EstimateDefaultRadius(float current, float appliedScale)
        {
            if (appliedScale > 0.001f)
                return current / appliedScale;
            return current;
        }

        private static void SetLegacyMode(int mode)
        {
            try
            {
                float scale = LegacyScales[mode];
                Level = 10;
                for (int i = 0; i < ScaleLevels.Length; i++)
                {
                    if (Mathf.Abs(ScaleLevels[i] - scale) < 0.01f)
                    {
                        Level = i + 1;
                        break;
                    }
                }
                ApplyCombined();
                Mode = mode;
                ModLog.Debug("[WheelSize] Legacy -> " + LegacyLabels[mode]);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[WheelSize] SetLegacyMode: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "WheelSize");
            }
        }

        private static void ClearBoneCache()
        {
            _cachedFrontBone = null;
            _cachedBackBone = null;
            _backBoneField = null;
            _frontBoneField = null;
        }

        private static void CacheBoneFields(BikeAnimation anim)
        {
            if ((object)_backBoneField == null)
                _backBoneField = typeof(BikeAnimation).GetField("YLzyVuM",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if ((object)_frontBoneField == null)
                _frontBoneField = typeof(BikeAnimation).GetField("RCNLpue",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        }
    }
}
