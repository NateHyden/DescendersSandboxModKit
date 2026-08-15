using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Cycles Storm / Fog / Moon / Normal. Fog must set Linear distances —
    // Descenders often uses FogMode.Linear where fogDensity alone does nothing
    // (that made Fog look like a no-op while Moon was the only obvious flip).
    public static class RandomWeatherRoulette
    {
        public enum WeatherState { Normal, Storm, Fog, Moon }

        public static bool Enabled { get; private set; } = false;
        public static WeatherState CurrentState { get; private set; } = WeatherState.Normal;
        public static string LastFlipDisplay { get; private set; } = "--";

        private const float MinInterval = 10f;
        private const float MaxInterval = 18f;
        private static float _nextFlipTime = 0f;

        private static bool _snapStorm = false;
        private static bool _snapMoon = false;
        private static bool _hasSnapshot = false;

        // Fog override (Fog.cs only removes fog — we thicken it here).
        private static bool _fogOverrideActive = false;
        private static bool _savedFogEnabled = false;
        private static FogMode _savedFogMode = FogMode.ExponentialSquared;
        private static float _savedFogDensity = 0.01f;
        private static float _savedFogStart = 0f;
        private static float _savedFogEnd = 300f;
        private static Color _savedFogColor = Color.gray;

        private const float HeavyFogStart = 2f;
        private const float HeavyFogEnd = 45f;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled)
            {
                _snapStorm = SkyColours.StormEnabled;
                _snapMoon = MoonMode.IsActive;
                _hasSnapshot = true;
                if (SkyColours.StormEnabled) SkyColours.ToggleStorm();
                if (MoonMode.IsActive) MoonMode.Toggle();
                CurrentState = WeatherState.Normal;
                LastFlipDisplay = "Normal";
                // Flip immediately so the feature is obvious — old code waited
                // 12–25s before the first change.
                FlipNow(preferNonNormal: true);
                ScheduleNext();
                ModLog.Debug("[RandomWeatherRoulette] ON — snapshotted Storm=" + _snapStorm + " Moon=" + _snapMoon);
            }
            else
            {
                ApplyState(WeatherState.Normal);
                RestoreSnapshot();
                LastFlipDisplay = "--";
                ModLog.Debug("[RandomWeatherRoulette] OFF — restored original weather.");
            }
        }

        private static void ScheduleNext()
        {
            _nextFlipTime = Time.unscaledTime + Random.Range(MinInterval, MaxInterval);
        }

        public static void Tick()
        {
            if (!Enabled) return;

            // Game/scene code can overwrite RenderSettings — reassert fog.
            if (CurrentState == WeatherState.Fog)
                ApplyFog(force: true);

            if (Time.unscaledTime < _nextFlipTime) return;
            FlipNow(preferNonNormal: false);
            ScheduleNext();
        }

        private static void FlipNow(bool preferNonNormal)
        {
            WeatherState next;
            int guard = 0;
            do
            {
                next = (WeatherState)Random.Range(0, 4);
                guard++;
            }
            while ((next == CurrentState || (preferNonNormal && next == WeatherState.Normal)) && guard < 12);

            try
            {
                ApplyState(next);
                LastFlipDisplay = next.ToString();
                ModLog.Feedback("[RandomWeatherRoulette] -> " + next);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[RandomWeatherRoulette] Flip: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "RandomWeatherRoulette");
            }
        }

        private static void ApplyState(WeatherState next)
        {
            switch (CurrentState)
            {
                case WeatherState.Storm: if (SkyColours.StormEnabled) SkyColours.ToggleStorm(); break;
                case WeatherState.Fog: RestoreFog(); break;
                case WeatherState.Moon: if (MoonMode.IsActive) MoonMode.Toggle(); break;
            }

            switch (next)
            {
                case WeatherState.Storm: if (!SkyColours.StormEnabled) SkyColours.ToggleStorm(); break;
                case WeatherState.Fog: ApplyFog(force: false); break;
                case WeatherState.Moon: if (!MoonMode.IsActive) MoonMode.Toggle(); break;
            }

            CurrentState = next;
        }

        private static void ApplyFog(bool force)
        {
            try
            {
                if (!_fogOverrideActive)
                {
                    _savedFogEnabled = RenderSettings.fog;
                    _savedFogMode = RenderSettings.fogMode;
                    _savedFogDensity = RenderSettings.fogDensity;
                    _savedFogStart = RenderSettings.fogStartDistance;
                    _savedFogEnd = RenderSettings.fogEndDistance;
                    _savedFogColor = RenderSettings.fogColor;
                    _fogOverrideActive = true;
                }
                else if (!force && CurrentState == WeatherState.Fog)
                    return;

                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = HeavyFogStart;
                RenderSettings.fogEndDistance = HeavyFogEnd;
                RenderSettings.fogDensity = 0.08f;
                RenderSettings.fogColor = new Color(0.55f, 0.62f, 0.70f, 1f);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[RandomWeatherRoulette] ApplyFog: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "RandomWeatherRoulette");
            }
        }

        private static void RestoreFog()
        {
            if (!_fogOverrideActive) return;
            try
            {
                RenderSettings.fog = _savedFogEnabled;
                RenderSettings.fogMode = _savedFogMode;
                RenderSettings.fogDensity = _savedFogDensity;
                RenderSettings.fogStartDistance = _savedFogStart;
                RenderSettings.fogEndDistance = _savedFogEnd;
                RenderSettings.fogColor = _savedFogColor;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[RandomWeatherRoulette] RestoreFog: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "RandomWeatherRoulette");
            }
            _fogOverrideActive = false;
        }

        private static void RestoreSnapshot()
        {
            if (!_hasSnapshot) return;
            try
            {
                if (_snapStorm && !SkyColours.StormEnabled) SkyColours.ToggleStorm();
                if (_snapMoon && !MoonMode.IsActive) MoonMode.Toggle();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[RandomWeatherRoulette] RestoreSnapshot: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "RandomWeatherRoulette");
            }
            _hasSnapshot = false;
        }

        public static void Reset()
        {
            if (Enabled)
            {
                ApplyState(WeatherState.Normal);
                RestoreSnapshot();
            }
            Enabled = false;
            CurrentState = WeatherState.Normal;
            LastFlipDisplay = "--";
            if (_fogOverrideActive) RestoreFog();
        }
    }
}
