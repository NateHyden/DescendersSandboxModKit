using MelonLoader;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    // Disco Mode — party-strobe sky + ambient through the same TOD shader
    // globals SkyColours already owns. Snap-cycles neon colours every few
    // frames so the world flashes club lights while you ride.
    public static class DiscoMode
    {
        public static bool Enabled { get; private set; } = false;

        // Level 1 = slow party, Level 10 = hard strobe
        public static int SpeedLevel { get; private set; } = 5;
        public static string SpeedDisplay => "x" + SpeedLevel;

        private static readonly Color[] Neon =
        {
            new Color(1.00f, 0.05f, 0.55f, 1f), // hot pink
            new Color(0.10f, 0.35f, 1.00f, 1f), // electric blue
            new Color(0.20f, 1.00f, 0.20f, 1f), // lime
            new Color(1.00f, 0.90f, 0.05f, 1f), // yellow
            new Color(1.00f, 0.35f, 0.05f, 1f), // orange
            new Color(0.75f, 0.05f, 1.00f, 1f), // purple
            new Color(0.05f, 1.00f, 0.95f, 1f), // cyan
            new Color(1.00f, 0.05f, 0.10f, 1f), // red
        };

        private static int _index = 0;
        private static float _nextFlip = 0f;
        private static bool _idsLoaded = false;
        private static int _idSunSky, _idMoonSky, _idFog, _idAmbient;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled)
            {
                _index = 0;
                _nextFlip = 0f;
                ApplyColours();
            }
            else if (SkyColours.CurrentPreset != 0)
            {
                SkyColours.ApplyColours();
            }
            else
            {
                SkyColours.RestoreDefault();
            }
            ModLog.Feedback("[DiscoMode] -> " + (Enabled ? "ON " + SpeedDisplay : "OFF"));
        }

        public static void IncreaseSpeed()
        {
            if (SpeedLevel < 10) SpeedLevel++;
            ModLog.Feedback("[DiscoMode] Speed -> " + SpeedDisplay);
        }

        public static void DecreaseSpeed()
        {
            if (SpeedLevel > 1) SpeedLevel--;
            ModLog.Feedback("[DiscoMode] Speed -> " + SpeedDisplay);
        }

        public static void SetSpeed(int level)
        {
            SpeedLevel = Mathf.Clamp(level, 1, 10);
        }

        public static void Reset()
        {
            if (Enabled) Toggle();
            SpeedLevel = 5;
            _index = 0;
            _nextFlip = 0f;
        }

        // Advance colour index. Colours are applied from TOD_Sky LateUpdate
        // so the game can't overwrite them the same frame.
        public static void Tick()
        {
            if (!Enabled) return;
            float now = Time.unscaledTime;
            if (now < _nextFlip) return;
            _index = (_index + 1) % Neon.Length;
            // Level 1 ≈ 0.28s, Level 10 ≈ 0.04s
            float interval = Mathf.Lerp(0.28f, 0.04f, (SpeedLevel - 1) / 9f);
            _nextFlip = now + interval;
        }

        public static void ApplyColours()
        {
            if (!Enabled) return;
            try
            {
                EnsureIds();
                Color c = Neon[_index];
                // Slightly darker moon/fog so the sun sky is the punch colour
                Color moon = c * 0.35f; moon.a = 1f;
                Color fog = Color.Lerp(c, Color.black, 0.35f); fog.a = 1f;
                Color amb = Color.Lerp(c, Color.white, 0.25f); amb.a = 1f;

                Shader.SetGlobalColor(_idSunSky, c);
                Shader.SetGlobalColor(_idMoonSky, moon);
                Shader.SetGlobalColor(_idFog, fog);
                Shader.SetGlobalColor(_idAmbient, amb);
                RenderSettings.ambientLight = amb;
                RenderSettings.ambientSkyColor = amb;
                RenderSettings.fogColor = fog;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[DiscoMode] ApplyColours: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "DiscoMode");
            }
        }

        private static void EnsureIds()
        {
            if (_idsLoaded) return;
            _idSunSky = Shader.PropertyToID("TOD_SunSkyColor");
            _idMoonSky = Shader.PropertyToID("TOD_MoonSkyColor");
            _idFog = Shader.PropertyToID("TOD_FogColor");
            _idAmbient = Shader.PropertyToID("TOD_AmbientColor");
            _idsLoaded = true;
        }
    }
}
