using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Enabled = true means fog is REMOVED
    public static class Fog
    {
        public static bool Enabled = false;
        private static float _savedDensity = -1f;
        private static bool _savedState = true;

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(!Enabled); // true = fog on, false = fog off
        }

        public static void Apply(bool fogOn)
        {
            try
            {
                if (!fogOn)
                {
                    if (_savedDensity < 0f)
                    {
                        _savedDensity = RenderSettings.fogDensity;
                        _savedState = RenderSettings.fog;
                    }
                    RenderSettings.fog = false;
                    RenderSettings.fogDensity = 0f;
                    MelonLogger.Msg("[Fog] Disabled");
                }
                else
                {
                    RenderSettings.fog = _savedState;
                    RenderSettings.fogDensity = _savedDensity >= 0f ? _savedDensity : 0.01f;
                    MelonLogger.Msg("[Fog] Restored density: " + RenderSettings.fogDensity);
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[Fog] Apply: " + ex.Message); }
        }

        public static void Reset()
        {
            if (Enabled) { Enabled = false; Apply(true); }
        }
    }
}
