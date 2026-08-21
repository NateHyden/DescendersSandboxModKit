using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using System.Reflection;

namespace DescendersModMenu.Mods
{
    public static class GraphicsSettings
    {
        // ── Post processing ───────────────────────────────────────────
        public static bool BloomEnabled { get; private set; } = true;
        public static bool AmbientOccEnabled { get; private set; } = true;
        public static bool VignetteEnabled { get; private set; } = true;
        public static bool DepthOfFieldEnabled { get; private set; } = false;
        public static bool ChromaticAbEnabled { get; private set; } = true;
        public static bool EyeAdaptEnabled { get; private set; } = true;
        public static bool ColorGradingEnabled { get; private set; } = true;
        public static bool MotionBlurEnabled { get; private set; } = false;
        public static bool SsrEnabled { get; private set; } = true;
        public static bool PpFogEnabled { get; private set; } = true;
        public static bool GrainEnabled { get; private set; } = true;

        // ── Render ────────────────────────────────────────────────────
        public static bool ShadowsEnabled { get; private set; } = true;
        public static bool SoftParticlesEnabled { get; private set; } = true;
        public static int AntiAliasingLevel { get; private set; } = -1;
        public static readonly string[] AaLabels = { "Off", "2x", "4x", "8x" };
        public static readonly string[] ShadowResLabels = { "Low", "Med", "High", "Ultra" };
        public static readonly string[] AnisoLabels = { "Off", "On", "Force" };
        public static readonly string[] CascadeLabels = { "0", "2", "4" };
        public static readonly string[] TexLabels = { "Full", "1/2", "1/4", "1/8" };
        public static readonly string[] VSyncLabels = { "Off", "On", "½ Rate" };
        public static readonly int[] FpsCaps = { 0, 30, 60, 120, 144, 240 };

        public static string[] QualityNames = { "Low", "Medium", "High", "Ultra" };

        private static int _defaultQuality = -1;
        private static int _shadowDistLevel = 3; // 0..5 maps to distances
        private static readonly float[] ShadowDistances = { 40f, 80f, 150f, 300f, 450f, 600f };
        private static int _lodBiasLevel = 2; // 0..5
        private static readonly float[] LodBiases = { 0.5f, 1f, 1.5f, 2f, 3f, 4f };
        private static int _pixelLightLevel = 4; // 0..8 used as count
        private static int _fpsCapIndex = 0;

        private static MonoBehaviour _ppb = null;
        private static object _profile = null;

        private static FieldInfo _bloomField, _aoField, _vigField, _dofField, _cabField;
        private static FieldInfo _eyeField, _cgField, _mbField, _ssrField, _fogField, _grainField;
        private static PropertyInfo _enabledProp = null;

        public static void CaptureDefaultQuality()
        {
            if (_defaultQuality < 0)
                _defaultQuality = QualitySettings.GetQualityLevel();
        }

        public static void RestoreDefaultQuality()
        {
            if (_defaultQuality >= 0)
                SetQuality(_defaultQuality);
        }

        private static bool EnsureRefs()
        {
            if (!UnityNull.Alive(_ppb))
            {
                MonoBehaviour[] all = Object.FindObjectsOfType<MonoBehaviour>();
                for (int i = 0; i < all.Length; i++)
                {
                    if (string.Equals(all[i].GetType().Name, "PostProcessingBehaviour",
                        System.StringComparison.Ordinal))
                    { _ppb = all[i]; break; }
                }
                if (!UnityNull.Alive(_ppb))
                {
                    ModLog.Warn("[Graphics] PostProcessingBehaviour not found.");
                    return false;
                }
            }

            if ((object)_profile == null)
            {
                FieldInfo f = _ppb.GetType().GetField("RzjbfkQ",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)f != null)
                    _profile = f.GetValue(_ppb);
                if ((object)_profile == null)
                {
                    ModLog.Warn("[Graphics] PostProcessingProfile not found.");
                    return false;
                }

                System.Type pt = _profile.GetType();
                _bloomField = pt.GetField("bloom", BindingFlags.Public | BindingFlags.Instance);
                _aoField = pt.GetField("ambientOcclusion", BindingFlags.Public | BindingFlags.Instance);
                _vigField = pt.GetField("vignette", BindingFlags.Public | BindingFlags.Instance);
                _dofField = pt.GetField("depthOfField", BindingFlags.Public | BindingFlags.Instance);
                _cabField = pt.GetField("chromaticAberration", BindingFlags.Public | BindingFlags.Instance);
                _eyeField = FindProfileField(pt, "eyeAdaptation", "autoExposure");
                _cgField = FindProfileField(pt, "colorGrading");
                _mbField = FindProfileField(pt, "motionBlur");
                _ssrField = FindProfileField(pt, "screenSpaceReflection", "screenSpaceReflections");
                _fogField = FindProfileField(pt, "fog");
                _grainField = FindProfileField(pt, "grain");
            }
            return true;
        }

        private static FieldInfo FindProfileField(System.Type pt, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo f = pt.GetField(names[i], BindingFlags.Public | BindingFlags.Instance);
                if ((object)f != null) return f;
            }
            FieldInfo[] all = pt.GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < all.Length; i++)
            {
                for (int j = 0; j < names.Length; j++)
                {
                    if (string.Equals(all[i].Name, names[j], System.StringComparison.OrdinalIgnoreCase))
                        return all[i];
                }
            }
            return null;
        }

        private static void SetEnabled(FieldInfo modelField, bool enabled)
        {
            if ((object)modelField == null || (object)_profile == null) return;
            try
            {
                object model = modelField.GetValue(_profile);
                if ((object)model == null) return;
                if ((object)_enabledProp == null)
                    _enabledProp = model.GetType().GetProperty("enabled",
                        BindingFlags.Public | BindingFlags.Instance);
                if ((object)_enabledProp == null)
                {
                    System.Type t = model.GetType().BaseType;
                    while ((object)t != null && (object)_enabledProp == null)
                    {
                        _enabledProp = t.GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance);
                        t = t.BaseType;
                    }
                }
                if ((object)_enabledProp != null)
                    _enabledProp.SetValue(model, enabled, null);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[Graphics] SetEnabled: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "Graphics");
            }
        }

        public static void ToggleBloom()
        {
            BloomEnabled = !BloomEnabled;
            if (!EnsureRefs()) return;
            ApplyPp("Bloom", _bloomField, BloomEnabled);
        }
        public static void ToggleAO()
        {
            AmbientOccEnabled = !AmbientOccEnabled;
            if (!EnsureRefs()) return;
            ApplyPp("AO", _aoField, AmbientOccEnabled);
        }
        public static void ToggleVignette()
        {
            VignetteEnabled = !VignetteEnabled;
            if (!EnsureRefs()) return;
            ApplyPp("Vignette", _vigField, VignetteEnabled);
        }
        public static void ToggleDOF()
        {
            DepthOfFieldEnabled = !DepthOfFieldEnabled;
            if (!EnsureRefs()) return;
            ApplyPp("DOF", _dofField, DepthOfFieldEnabled);
        }
        public static void ToggleChromatic()
        {
            ChromaticAbEnabled = !ChromaticAbEnabled;
            if (!EnsureRefs()) return;
            ApplyPp("Chromatic", _cabField, ChromaticAbEnabled);
        }
        public static void ToggleEyeAdapt()
        {
            EyeAdaptEnabled = !EyeAdaptEnabled;
            if (!EnsureRefs()) return;
            ApplyPp("Eye Adapt", _eyeField, EyeAdaptEnabled);
        }
        public static void ToggleColorGrading()
        {
            ColorGradingEnabled = !ColorGradingEnabled;
            if (!EnsureRefs()) return;
            ApplyPp("Color Grading", _cgField, ColorGradingEnabled);
        }
        public static void ToggleMotionBlur()
        {
            MotionBlurEnabled = !MotionBlurEnabled;
            if (!EnsureRefs()) return;
            ApplyPp("Motion Blur", _mbField, MotionBlurEnabled);
        }
        public static void ToggleSsr()
        {
            SsrEnabled = !SsrEnabled;
            if (!EnsureRefs()) return;
            ApplyPp("SSR", _ssrField, SsrEnabled);
        }
        public static void TogglePpFog()
        {
            PpFogEnabled = !PpFogEnabled;
            if (!EnsureRefs()) return;
            ApplyPp("PP Fog", _fogField, PpFogEnabled);
        }
        public static void ToggleGrain()
        {
            GrainEnabled = !GrainEnabled;
            if (!EnsureRefs()) return;
            ApplyPp("Grain", _grainField, GrainEnabled);
        }

        private static void ApplyPp(string label, FieldInfo field, bool enabled)
        {
            if ((object)field == null)
            {
                ModLog.Warn("[Graphics] " + label + " missing from profile — toggle stored only.");
                ModLog.Feedback("[Graphics] " + label + " -> " + (enabled ? "ON" : "OFF") + " (n/a)");
                return;
            }
            SetEnabled(field, enabled);
            ModLog.Feedback("[Graphics] " + label + " -> " + (enabled ? "ON" : "OFF"));
        }

        public static void ToggleShadows()
        {
            ShadowsEnabled = !ShadowsEnabled;
            try
            {
                QualitySettings.shadows = ShadowsEnabled ? ShadowQuality.All : ShadowQuality.Disable;
                ModLog.Feedback("[Graphics] Shadows -> " + (ShadowsEnabled ? "ON" : "OFF"));
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] Shadows: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Graphics"); }
        }

        public static void ToggleSoftParticles()
        {
            SoftParticlesEnabled = !SoftParticlesEnabled;
            try
            {
                QualitySettings.softParticles = SoftParticlesEnabled;
                ModLog.Feedback("[Graphics] SoftParticles -> " + (SoftParticlesEnabled ? "ON" : "OFF"));
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] SoftParticles: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Graphics"); }
        }

        public static void CycleAntiAliasing()
        {
            int cur = AntiAliasingLevel < 0 ? QualitySettings.antiAliasing : AntiAliasingLevel;
            int next;
            if (cur <= 0) next = 2;
            else if (cur <= 2) next = 4;
            else if (cur <= 4) next = 8;
            else next = 0;
            AntiAliasingLevel = next;
            try
            {
                QualitySettings.antiAliasing = next;
                ModLog.Feedback("[Graphics] AA -> " + AaLabel(next));
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] AA: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Graphics"); }
        }

        public static string AaLabel(int level)
        {
            if (level <= 0) return AaLabels[0];
            if (level <= 2) return AaLabels[1];
            if (level <= 4) return AaLabels[2];
            return AaLabels[3];
        }

        public static string CurrentAaDisplay
        {
            get
            {
                int v = AntiAliasingLevel >= 0 ? AntiAliasingLevel : QualitySettings.antiAliasing;
                return AaLabel(v);
            }
        }

        // ── Shadow distance ───────────────────────────────────────────
        public static string ShadowDistDisplay
        {
            get { return ((int)QualitySettings.shadowDistance).ToString() + "m"; }
        }

        public static void ShadowDistIncrease()
        {
            if (_shadowDistLevel < ShadowDistances.Length - 1) _shadowDistLevel++;
            ApplyShadowDist();
        }

        public static void ShadowDistDecrease()
        {
            if (_shadowDistLevel > 0) _shadowDistLevel--;
            ApplyShadowDist();
        }

        private static void ApplyShadowDist()
        {
            try
            {
                QualitySettings.shadowDistance = ShadowDistances[_shadowDistLevel];
                ModLog.Feedback("[Graphics] ShadowDist -> " + ShadowDistDisplay);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] ShadowDist: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Graphics"); }
        }

        // ── Shadow resolution ─────────────────────────────────────────
        public static string ShadowResDisplay
        {
            get
            {
                try
                {
                    int i = (int)QualitySettings.shadowResolution;
                    if (i >= 0 && i < ShadowResLabels.Length) return ShadowResLabels[i];
                }
                catch { }
                return "—";
            }
        }

        public static void CycleShadowResolution()
        {
            try
            {
                int cur = (int)QualitySettings.shadowResolution;
                int next = (cur + 1) % 4;
                QualitySettings.shadowResolution = (ShadowResolution)next;
                ModLog.Feedback("[Graphics] ShadowRes -> " + ShadowResLabels[next]);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] ShadowRes: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Graphics"); }
        }

        // ── Shadow cascades ───────────────────────────────────────────
        public static string CascadesDisplay
        {
            get
            {
                int c = QualitySettings.shadowCascades;
                if (c <= 0) return CascadeLabels[0];
                if (c <= 2) return CascadeLabels[1];
                return CascadeLabels[2];
            }
        }

        public static void CycleShadowCascades()
        {
            try
            {
                int c = QualitySettings.shadowCascades;
                int next;
                if (c <= 0) next = 2;
                else if (c <= 2) next = 4;
                else next = 0;
                QualitySettings.shadowCascades = next;
                ModLog.Feedback("[Graphics] Cascades -> " + next);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] Cascades: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Graphics"); }
        }

        // ── Texture quality ───────────────────────────────────────────
        public static string TextureDisplay
        {
            get
            {
                int lim = QualitySettings.masterTextureLimit;
                if (lim < 0) lim = 0;
                if (lim > 3) lim = 3;
                return TexLabels[lim];
            }
        }

        public static void CycleTextureQuality()
        {
            try
            {
                // Cycle Full → 1/2 → 1/4 → 1/8 → Full (0 is best)
                int lim = QualitySettings.masterTextureLimit;
                lim = (lim + 1) % 4;
                QualitySettings.masterTextureLimit = lim;
                ModLog.Feedback("[Graphics] Textures -> " + TexLabels[lim]);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] Textures: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Graphics"); }
        }

        public static void SetTextureFull()
        {
            try
            {
                QualitySettings.masterTextureLimit = 0;
                ModLog.Feedback("[Graphics] Textures -> Full");
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] Textures: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Graphics"); }
        }

        // ── Anisotropic ───────────────────────────────────────────────
        public static string AnisoDisplay
        {
            get
            {
                try
                {
                    int i = (int)QualitySettings.anisotropicFiltering;
                    if (i >= 0 && i < AnisoLabels.Length) return AnisoLabels[i];
                }
                catch { }
                return "—";
            }
        }

        public static void CycleAnisotropic()
        {
            try
            {
                int cur = (int)QualitySettings.anisotropicFiltering;
                int next = (cur + 1) % 3;
                QualitySettings.anisotropicFiltering = (AnisotropicFiltering)next;
                ModLog.Feedback("[Graphics] Aniso -> " + AnisoLabels[next]);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] Aniso: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Graphics"); }
        }

        // ── LOD bias ──────────────────────────────────────────────────
        public static string LodBiasDisplay
        {
            get { return QualitySettings.lodBias.ToString("F1"); }
        }

        public static void LodBiasIncrease()
        {
            if (_lodBiasLevel < LodBiases.Length - 1) _lodBiasLevel++;
            ApplyLodBias();
        }

        public static void LodBiasDecrease()
        {
            if (_lodBiasLevel > 0) _lodBiasLevel--;
            ApplyLodBias();
        }

        private static void ApplyLodBias()
        {
            try
            {
                QualitySettings.lodBias = LodBiases[_lodBiasLevel];
                QualitySettings.maximumLODLevel = 0;
                ModLog.Feedback("[Graphics] LOD Bias -> " + LodBiasDisplay);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] LOD: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Graphics"); }
        }

        // ── Pixel lights ──────────────────────────────────────────────
        public static string PixelLightsDisplay
        {
            get { return QualitySettings.pixelLightCount.ToString(); }
        }

        public static void PixelLightsIncrease()
        {
            if (_pixelLightLevel < 8) _pixelLightLevel++;
            ApplyPixelLights();
        }

        public static void PixelLightsDecrease()
        {
            if (_pixelLightLevel > 0) _pixelLightLevel--;
            ApplyPixelLights();
        }

        private static void ApplyPixelLights()
        {
            try
            {
                QualitySettings.pixelLightCount = _pixelLightLevel;
                ModLog.Feedback("[Graphics] PixelLights -> " + _pixelLightLevel);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] PixelLights: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Graphics"); }
        }

        // ── VSync ─────────────────────────────────────────────────────
        public static string VSyncDisplay
        {
            get
            {
                int v = QualitySettings.vSyncCount;
                if (v <= 0) return VSyncLabels[0];
                if (v == 1) return VSyncLabels[1];
                return VSyncLabels[2];
            }
        }

        public static void CycleVSync()
        {
            try
            {
                int v = QualitySettings.vSyncCount;
                int next;
                if (v <= 0) next = 1;
                else if (v == 1) next = 2;
                else next = 0;
                QualitySettings.vSyncCount = next;
                ModLog.Feedback("[Graphics] VSync -> " + VSyncDisplay);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] VSync: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Graphics"); }
        }

        // ── Target FPS ────────────────────────────────────────────────
        public static string FpsCapDisplay
        {
            get
            {
                int cap = Application.targetFrameRate;
                if (cap <= 0) return "Unlimited";
                return cap.ToString();
            }
        }

        public static void CycleFpsCap()
        {
            _fpsCapIndex = (_fpsCapIndex + 1) % FpsCaps.Length;
            try
            {
                Application.targetFrameRate = FpsCaps[_fpsCapIndex];
                ModLog.Feedback("[Graphics] FPS Cap -> " + FpsCapDisplay);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] FPS Cap: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Graphics"); }
        }

        // ── Quality presets ───────────────────────────────────────────
        public static void SetQuality(int level)
        {
            try
            {
                QualitySettings.SetQualityLevel(level, true);

                switch (level)
                {
                    case 0:
                        QualitySettings.shadowDistance = 40f;
                        QualitySettings.shadowCascades = 0;
                        QualitySettings.masterTextureLimit = 2;
                        QualitySettings.lodBias = 0.3f;
                        QualitySettings.maximumLODLevel = 2;
                        QualitySettings.pixelLightCount = 0;
                        _shadowDistLevel = 0;
                        _lodBiasLevel = 0;
                        _pixelLightLevel = 0;
                        break;
                    case 1:
                        QualitySettings.shadowDistance = 80f;
                        QualitySettings.shadowCascades = 2;
                        QualitySettings.masterTextureLimit = 1;
                        QualitySettings.lodBias = 0.7f;
                        QualitySettings.maximumLODLevel = 1;
                        QualitySettings.pixelLightCount = 2;
                        _shadowDistLevel = 1;
                        _lodBiasLevel = 0;
                        _pixelLightLevel = 2;
                        break;
                    case 2:
                        QualitySettings.shadowDistance = 150f;
                        QualitySettings.shadowCascades = 4;
                        QualitySettings.masterTextureLimit = 0;
                        QualitySettings.lodBias = 1.0f;
                        QualitySettings.maximumLODLevel = 0;
                        QualitySettings.pixelLightCount = 4;
                        _shadowDistLevel = 2;
                        _lodBiasLevel = 1;
                        _pixelLightLevel = 4;
                        break;
                    case 3:
                        QualitySettings.shadowDistance = 300f;
                        QualitySettings.shadowCascades = 4;
                        QualitySettings.masterTextureLimit = 0;
                        QualitySettings.lodBias = 2.0f;
                        QualitySettings.maximumLODLevel = 0;
                        QualitySettings.pixelLightCount = 8;
                        _shadowDistLevel = 3;
                        _lodBiasLevel = 3;
                        _pixelLightLevel = 8;
                        break;
                }
                ModLog.Feedback("[Graphics] Quality -> " + QualityNames[level]);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] SetQuality: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Graphics"); }
        }

        public static int GetCurrentQuality()
        {
            return QualitySettings.GetQualityLevel();
        }

        /// <summary>Push every quality knob as high as Unity will go.</summary>
        public static void ApplyMaxDetail()
        {
            try
            {
                SetQuality(3);

                QualitySettings.antiAliasing = 8;
                AntiAliasingLevel = 8;

                ShadowsEnabled = true;
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                QualitySettings.shadowCascades = 4;
                _shadowDistLevel = ShadowDistances.Length - 1;
                QualitySettings.shadowDistance = ShadowDistances[_shadowDistLevel];

                SoftParticlesEnabled = true;
                QualitySettings.softParticles = true;

                QualitySettings.masterTextureLimit = 0;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

                _lodBiasLevel = LodBiases.Length - 1;
                QualitySettings.lodBias = LodBiases[_lodBiasLevel];
                QualitySettings.maximumLODLevel = 0;

                _pixelLightLevel = 8;
                QualitySettings.pixelLightCount = 8;

                // Enable detail-friendly post FX (leave DOF/motion blur off — they soften clarity)
                if (EnsureRefs())
                {
                    BloomEnabled = true; SetEnabled(_bloomField, true);
                    AmbientOccEnabled = true; SetEnabled(_aoField, true);
                    VignetteEnabled = true; SetEnabled(_vigField, true);
                    ChromaticAbEnabled = true; SetEnabled(_cabField, true);
                    EyeAdaptEnabled = true; SetEnabled(_eyeField, true);
                    ColorGradingEnabled = true; SetEnabled(_cgField, true);
                    SsrEnabled = true; SetEnabled(_ssrField, true);
                    PpFogEnabled = true; SetEnabled(_fogField, true);
                    GrainEnabled = true; SetEnabled(_grainField, true);
                    DepthOfFieldEnabled = false; SetEnabled(_dofField, false);
                    MotionBlurEnabled = false; SetEnabled(_mbField, false);
                }

                ModLog.Feedback("[Graphics] MAX DETAIL applied.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[Graphics] MaxDetail: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "Graphics");
            }
        }

        public static void SyncLevelsFromQuality()
        {
            try
            {
                float sd = QualitySettings.shadowDistance;
                _shadowDistLevel = 0;
                for (int i = 0; i < ShadowDistances.Length; i++)
                {
                    if (sd + 0.1f >= ShadowDistances[i]) _shadowDistLevel = i;
                }

                float lb = QualitySettings.lodBias;
                _lodBiasLevel = 0;
                for (int i = 0; i < LodBiases.Length; i++)
                {
                    if (lb + 0.05f >= LodBiases[i]) _lodBiasLevel = i;
                }

                _pixelLightLevel = Mathf.Clamp(QualitySettings.pixelLightCount, 0, 8);

                int fps = Application.targetFrameRate;
                _fpsCapIndex = 0;
                for (int i = 0; i < FpsCaps.Length; i++)
                {
                    if (FpsCaps[i] == fps) { _fpsCapIndex = i; break; }
                }
            }
            catch { }
        }

        public static void Reset()
        {
            _ppb = null;
            _profile = null;
            _bloomField = _aoField = _vigField = _dofField = _cabField = null;
            _eyeField = _cgField = _mbField = _ssrField = _fogField = _grainField = null;
            _enabledProp = null;
            BloomEnabled = true;
            AmbientOccEnabled = true;
            VignetteEnabled = true;
            DepthOfFieldEnabled = false;
            ChromaticAbEnabled = true;
            EyeAdaptEnabled = true;
            ColorGradingEnabled = true;
            MotionBlurEnabled = false;
            SsrEnabled = true;
            PpFogEnabled = true;
            GrainEnabled = true;
            ShadowsEnabled = true;
            SoftParticlesEnabled = true;
            AntiAliasingLevel = -1;
        }
    }
}
