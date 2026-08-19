using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class Fog
    {
        public static bool Enabled = false;
        private static float _savedDensity = -1f;
        private static bool _savedState = true;

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(!Enabled);
            ModLog.Feedback("[Fog] -> " + (Enabled ? "ON" : "OFF"));
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
                }
                else
                {
                    RenderSettings.fog = _savedState;
                    RenderSettings.fogDensity = _savedDensity >= 0f ? _savedDensity : 0.01f;
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[Fog] Apply: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Fog"); }
        }

        public static void Reset()
        {
            if (Enabled) { Enabled = false; Apply(true); }
        }
    }
}
