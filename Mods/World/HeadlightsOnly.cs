using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // HeadlightsOnly — makes the world pitch black except for the bike torch.
    //
    // On enable:
    //   - Saves RenderSettings ambient colour + intensity, sets both to zero
    //   - Finds all Directional lights (the sun) and disables them
    //   - Auto-enables BikeTorch at max intensity if not already on
    //
    // On disable:
    //   - Restores ambient settings
    //   - Re-enables saved directional lights
    //   - Turns BikeTorch off if it wasn't on before
    //
    // Lights are re-cached and re-disabled each scene load (ClearCache).
    // Does not touch Point or Spot lights — preserves any scene decoration lights.
    public static class HeadlightsOnly
    {
        public static bool Enabled { get; private set; } = false;

        // ── Ambient restore ───────────────────────────────────────────
        private static Color _savedAmbientColor = Color.black;
        private static Color _savedAmbientSky = Color.black;
        private static Color _savedAmbientEquator = Color.black;
        private static Color _savedAmbientGround = Color.black;
        private static float _savedAmbientIntensity = 1f;
        private static float _savedReflectionIntensity = 1f;
        private static bool _ambientSaved = false;

        // ── Directional light cache ───────────────────────────────────
        private static Light[] _dirLights = null;
        private static bool[] _dirLightWasEnabled = null;
        private static bool _lightsCached = false;

        // ── BikeTorch auto-on state ───────────────────────────────────
        private static bool _torchWasEnabled = false;

        // ── Time of day ───────────────────────────────────────────────
        private static int _savedTodLevel = 5;

        // ── Public API ────────────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled)
                Apply();
            else
                Restore();
            MelonLogger.Msg("[HeadlightsOnly] -> " + (Enabled ? "ON" : "OFF"));
        }

        private static void Apply()
        {
            try
            {
                // Save + zero all ambient channels
                if (!_ambientSaved)
                {
                    _savedAmbientColor = RenderSettings.ambientLight;
                    _savedAmbientSky = RenderSettings.ambientSkyColor;
                    _savedAmbientEquator = RenderSettings.ambientEquatorColor;
                    _savedAmbientGround = RenderSettings.ambientGroundColor;
                    _savedAmbientIntensity = RenderSettings.ambientIntensity;
                    _savedReflectionIntensity = RenderSettings.reflectionIntensity;
                    _ambientSaved = true;
                    MelonLogger.Msg("[HeadlightsOnly] Ambient saved. intensity="
                        + _savedAmbientIntensity + " reflection=" + _savedReflectionIntensity);
                }
                RenderSettings.ambientLight = Color.black;
                RenderSettings.ambientSkyColor = Color.black;
                RenderSettings.ambientEquatorColor = Color.black;
                RenderSettings.ambientGroundColor = Color.black;
                RenderSettings.ambientIntensity = 0f;
                RenderSettings.reflectionIntensity = 0f;

                // Force time of day to Twilight (darkest) — level 9 = 20.5h
                _savedTodLevel = TimeOfDay.Level;
                TimeOfDay.SetLevel(9);

                // Find and disable all directional lights (sun)
                if (!_lightsCached)
                    CacheLights();

                if (_dirLights != null)
                {
                    for (int i = 0; i < _dirLights.Length; i++)
                    {
                        if ((object)_dirLights[i] != null)
                            _dirLights[i].enabled = false;
                    }
                    MelonLogger.Msg("[HeadlightsOnly] Disabled " + _dirLights.Length + " directional light(s).");
                }

                // Auto-enable BikeTorch at max intensity
                _torchWasEnabled = BikeTorch.Enabled;
                if (!BikeTorch.Enabled)
                {
                    // Set to max intensity before enabling
                    BikeTorch.IntensityIndex = 4;
                    BikeTorch.Toggle();
                }
                else
                {
                    // Already on — boost to max
                    BikeTorch.IntensityIndex = 4;
                    BikeTorch.Apply();
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[HeadlightsOnly] Apply: " + ex.Message); }
        }

        private static void Restore()
        {
            try
            {
                // Restore all ambient channels
                if (_ambientSaved)
                {
                    RenderSettings.ambientLight = _savedAmbientColor;
                    RenderSettings.ambientSkyColor = _savedAmbientSky;
                    RenderSettings.ambientEquatorColor = _savedAmbientEquator;
                    RenderSettings.ambientGroundColor = _savedAmbientGround;
                    RenderSettings.ambientIntensity = _savedAmbientIntensity;
                    RenderSettings.reflectionIntensity = _savedReflectionIntensity;
                    _ambientSaved = false;
                    MelonLogger.Msg("[HeadlightsOnly] Ambient restored.");
                }

                // Restore time of day
                TimeOfDay.SetLevel(_savedTodLevel);

                // Re-enable directional lights
                if (_dirLights != null && _dirLightWasEnabled != null)
                {
                    for (int i = 0; i < _dirLights.Length; i++)
                    {
                        if ((object)_dirLights[i] != null)
                            _dirLights[i].enabled = _dirLightWasEnabled[i];
                    }
                    MelonLogger.Msg("[HeadlightsOnly] Directional lights restored.");
                }

                // Turn torch off if we auto-enabled it
                if (!_torchWasEnabled && BikeTorch.Enabled)
                    BikeTorch.Toggle();
            }
            catch (System.Exception ex) { MelonLogger.Error("[HeadlightsOnly] Restore: " + ex.Message); }
        }

        private static void CacheLights()
        {
            _lightsCached = true;
            try
            {
                Light[] all = Object.FindObjectsOfType<Light>();
                int count = 0;
                for (int i = 0; i < all.Length; i++)
                    if (all[i].type == LightType.Directional) count++;

                _dirLights = new Light[count];
                _dirLightWasEnabled = new bool[count];
                int idx = 0;
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].type == LightType.Directional)
                    {
                        _dirLights[idx] = all[i];
                        _dirLightWasEnabled[idx] = all[i].enabled;
                        idx++;
                    }
                }
                MelonLogger.Msg("[HeadlightsOnly] Cached " + count + " directional light(s).");
            }
            catch (System.Exception ex) { MelonLogger.Error("[HeadlightsOnly] CacheLights: " + ex.Message); }
        }

        // Called on scene unload — restore and clear caches
        public static void ClearCache()
        {
            if (Enabled) Restore();
            _dirLights = null;
            _dirLightWasEnabled = null;
            _lightsCached = false;
            _ambientSaved = false;
        }

        public static void Reset()
        {
            if (Enabled) { Restore(); }
            Enabled = false;
            ClearCache();
        }
    }
}