using MelonLoader;
using UnityEngine;
using System.Reflection;

namespace DescendersModMenu.Mods
{
    public static class GraphicsSettings
    {
        public static bool BloomEnabled { get; private set; } = true;
        public static bool AmbientOccEnabled { get; private set; } = true;
        public static bool VignetteEnabled { get; private set; } = true;
        public static bool DepthOfFieldEnabled { get; private set; } = false;
        public static bool ChromaticAbEnabled { get; private set; } = true;

        public static string[] QualityNames = { "Low", "Medium", "High", "Ultra" };

        // Quality level recorded on first scene init — before any mod touches it
        private static int _defaultQuality = -1;

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

        private static MonoBehaviour _ppb = null;
        private static object _profile = null;

        // Cached field refs on profile — all public fields
        private static FieldInfo _bloomField = null;
        private static FieldInfo _aoField = null;
        private static FieldInfo _vigField = null;
        private static FieldInfo _dofField = null;
        private static FieldInfo _cabField = null;

        // enabled property on PostProcessingModel base
        private static PropertyInfo _enabledProp = null;

        private static bool EnsureRefs()
        {
            if ((object)_ppb == null)
            {
                MonoBehaviour[] all = Object.FindObjectsOfType<MonoBehaviour>();
                for (int i = 0; i < all.Length; i++)
                {
                    if (string.Equals(all[i].GetType().Name, "PostProcessingBehaviour",
                        System.StringComparison.Ordinal))
                    { _ppb = all[i]; break; }
                }
                if ((object)_ppb == null) { MelonLogger.Warning("[Graphics] PostProcessingBehaviour not found."); return false; }
                MelonLogger.Msg("[Graphics] Found PostProcessingBehaviour.");
            }

            if ((object)_profile == null)
            {
                // Profile is public field RzjbfkQ on PostProcessingBehaviour
                FieldInfo f = _ppb.GetType().GetField("RzjbfkQ",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)f != null)
                    _profile = f.GetValue(_ppb);

                if ((object)_profile == null)
                { MelonLogger.Warning("[Graphics] PostProcessingProfile (RzjbfkQ) not found."); return false; }

                // All models are public fields on the profile
                System.Type pt = _profile.GetType();
                _bloomField = pt.GetField("bloom", BindingFlags.Public | BindingFlags.Instance);
                _aoField = pt.GetField("ambientOcclusion", BindingFlags.Public | BindingFlags.Instance);
                _vigField = pt.GetField("vignette", BindingFlags.Public | BindingFlags.Instance);
                _dofField = pt.GetField("depthOfField", BindingFlags.Public | BindingFlags.Instance);
                _cabField = pt.GetField("chromaticAberration", BindingFlags.Public | BindingFlags.Instance);

                MelonLogger.Msg("[Graphics] Profile found. Bloom=" + ((object)_bloomField != null)
                    + " AO=" + ((object)_aoField != null)
                    + " Vig=" + ((object)_vigField != null)
                    + " DOF=" + ((object)_dofField != null)
                    + " CAB=" + ((object)_cabField != null));
            }
            return true;
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
                // Walk up base types to find enabled if not found directly
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
                else
                    MelonLogger.Warning("[Graphics] 'enabled' property not found on model.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] SetEnabled: " + ex.Message); }
        }

        public static void ToggleBloom()
        {
            BloomEnabled = !BloomEnabled;
            if (!EnsureRefs()) return;
            SetEnabled(_bloomField, BloomEnabled);
            MelonLogger.Msg("[Graphics] Bloom -> " + BloomEnabled);
        }

        public static void ToggleAO()
        {
            AmbientOccEnabled = !AmbientOccEnabled;
            if (!EnsureRefs()) return;
            SetEnabled(_aoField, AmbientOccEnabled);
            MelonLogger.Msg("[Graphics] AO -> " + AmbientOccEnabled);
        }

        public static void ToggleVignette()
        {
            VignetteEnabled = !VignetteEnabled;
            if (!EnsureRefs()) return;
            SetEnabled(_vigField, VignetteEnabled);
            MelonLogger.Msg("[Graphics] Vignette -> " + VignetteEnabled);
        }

        public static void ToggleDOF()
        {
            DepthOfFieldEnabled = !DepthOfFieldEnabled;
            if (!EnsureRefs()) return;
            SetEnabled(_dofField, DepthOfFieldEnabled);
            MelonLogger.Msg("[Graphics] DOF -> " + DepthOfFieldEnabled);
        }

        public static void ToggleChromatic()
        {
            ChromaticAbEnabled = !ChromaticAbEnabled;
            if (!EnsureRefs()) return;
            SetEnabled(_cabField, ChromaticAbEnabled);
            MelonLogger.Msg("[Graphics] ChromaticAb -> " + ChromaticAbEnabled);
        }

        public static void SetQuality(int level)
        {
            try
            {
                QualitySettings.SetQualityLevel(level, true);

                // Force visible quality changes since SetQualityLevel alone
                // often has no effect mid-game in Unity 2017
                switch (level)
                {
                    case 0: // Low
                        QualitySettings.shadowDistance = 40f;
                        QualitySettings.shadowCascades = 0;
                        QualitySettings.masterTextureLimit = 2;
                        QualitySettings.lodBias = 0.3f;
                        QualitySettings.maximumLODLevel = 2;
                        QualitySettings.pixelLightCount = 0;
                        break;
                    case 1: // Medium
                        QualitySettings.shadowDistance = 80f;
                        QualitySettings.shadowCascades = 2;
                        QualitySettings.masterTextureLimit = 1;
                        QualitySettings.lodBias = 0.7f;
                        QualitySettings.maximumLODLevel = 1;
                        QualitySettings.pixelLightCount = 2;
                        break;
                    case 2: // High
                        QualitySettings.shadowDistance = 150f;
                        QualitySettings.shadowCascades = 4;
                        QualitySettings.masterTextureLimit = 0;
                        QualitySettings.lodBias = 1.0f;
                        QualitySettings.maximumLODLevel = 0;
                        QualitySettings.pixelLightCount = 4;
                        break;
                    case 3: // Ultra
                        QualitySettings.shadowDistance = 300f;
                        QualitySettings.shadowCascades = 4;
                        QualitySettings.masterTextureLimit = 0;
                        QualitySettings.lodBias = 2.0f;
                        QualitySettings.maximumLODLevel = 0;
                        QualitySettings.pixelLightCount = 8;
                        break;
                }
                MelonLogger.Msg("[Graphics] Quality -> " + QualityNames[level]);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Graphics] SetQuality: " + ex.Message); }
        }

        public static int GetCurrentQuality()
        {
            return QualitySettings.GetQualityLevel();
        }

        public static void Reset()
        {
            _ppb = null;
            _profile = null;
            _bloomField = null;
            _aoField = null;
            _vigField = null;
            _dofField = null;
            _cabField = null;
            _enabledProp = null;
            BloomEnabled = true;
            AmbientOccEnabled = true;
            VignetteEnabled = true;
            DepthOfFieldEnabled = false;
            ChromaticAbEnabled = true;
        }
    }
}