using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Cycles between Storm, Fog, Moon and Normal (nothing) every so often
    // instead of a single fixed weather pick. Reuses SkyColours' existing
    // storm system and MoonMode's low-gravity effect; Fog is its own tiny
    // self-contained density override — Fog.cs only ever REMOVES fog
    // (no "make it thicker" mode), so that piece is implemented directly
    // against RenderSettings here instead of going through Fog.cs.
    //
    // Same snapshot/restore shape as ChaosMode/RandomBikeSwitch: not
    // hooked into ModEntry's scene-transition system (it holds no cached
    // per-scene object refs, only a bool/float override state), so it
    // just keeps ticking across scene loads like its siblings do.
    public static class RandomWeatherRoulette
    {
        public enum WeatherState { Normal, Storm, Fog, Moon }

        public static bool Enabled { get; private set; } = false;
        public static WeatherState CurrentState { get; private set; } = WeatherState.Normal;
        public static string LastFlipDisplay { get; private set; } = "--";

        private const float MinInterval = 12f;
        private const float MaxInterval = 25f;
        private static float _nextFlipTime = 0f;

        // Baseline captured on enable, restored on disable — same shape
        // as ChaosMode's snapshot/restore for the mods it borrows.
        private static bool _snapStorm = false;
        private static bool _snapMoon = false;
        private static bool _hasSnapshot = false;

        // Our own fog override — Fog.cs is a removal-only toggle, so a
        // "make it foggy" state needs its own density save/restore.
        private const float HeavyFogDensity = 0.06f;
        private static float _savedFogDensity = -1f;
        private static bool _savedFogState = true;
        private static bool _fogOverrideActive = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled)
            {
                _snapStorm = SkyColours.StormEnabled;
                _snapMoon = MoonMode.IsActive;
                _hasSnapshot = true;
                // Start from a clean Normal state — whatever was already
                // on gets folded back in when the snapshot is restored.
                if (SkyColours.StormEnabled) SkyColours.ToggleStorm();
                if (MoonMode.IsActive) MoonMode.Toggle();
                CurrentState = WeatherState.Normal;
                ScheduleNext();
                ModLog.Debug("[RandomWeatherRoulette] ON — snapshotted Storm=" + _snapStorm + " Moon=" + _snapMoon);
            }
            else
            {
                ApplyState(WeatherState.Normal); // turn off whatever's currently running
                RestoreSnapshot();
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
            if (Time.unscaledTime < _nextFlipTime) return;

            WeatherState next;
            do { next = (WeatherState)Random.Range(0, 4); } while (next == CurrentState);

            try
            {
                ApplyState(next);
                LastFlipDisplay = next.ToString();
                ModLog.Feedback("[RandomWeatherRoulette] -> " + next);
            }
            catch (System.Exception ex) { MelonLogger.Error("[RandomWeatherRoulette] Tick: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "RandomWeatherRoulette"); }
            ScheduleNext();
        }

        private static void ApplyState(WeatherState next)
        {
            // Turn off whatever the current state was switching FROM.
            switch (CurrentState)
            {
                case WeatherState.Storm: if (SkyColours.StormEnabled) SkyColours.ToggleStorm(); break;
                case WeatherState.Fog: RestoreFog(); break;
                case WeatherState.Moon: if (MoonMode.IsActive) MoonMode.Toggle(); break;
            }

            // Turn on whatever we're switching TO.
            switch (next)
            {
                case WeatherState.Storm: if (!SkyColours.StormEnabled) SkyColours.ToggleStorm(); break;
                case WeatherState.Fog: ApplyFog(); break;
                case WeatherState.Moon: if (!MoonMode.IsActive) MoonMode.Toggle(); break;
            }

            CurrentState = next;
        }

        private static void ApplyFog()
        {
            try
            {
                if (!_fogOverrideActive)
                {
                    _savedFogDensity = RenderSettings.fogDensity;
                    _savedFogState = RenderSettings.fog;
                    _fogOverrideActive = true;
                }
                RenderSettings.fog = true;
                RenderSettings.fogDensity = HeavyFogDensity;
            }
            catch (System.Exception ex) { MelonLogger.Error("[RandomWeatherRoulette] ApplyFog: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "RandomWeatherRoulette"); }
        }

        private static void RestoreFog()
        {
            if (!_fogOverrideActive) return;
            try
            {
                RenderSettings.fog = _savedFogState;
                RenderSettings.fogDensity = _savedFogDensity;
            }
            catch (System.Exception ex) { MelonLogger.Error("[RandomWeatherRoulette] RestoreFog: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "RandomWeatherRoulette"); }
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
            catch (System.Exception ex) { MelonLogger.Error("[RandomWeatherRoulette] RestoreSnapshot: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "RandomWeatherRoulette"); }
            _hasSnapshot = false;
        }

        public static void Reset()
        {
            if (Enabled) { ApplyState(WeatherState.Normal); RestoreSnapshot(); }
            Enabled = false;
            CurrentState = WeatherState.Normal;
            LastFlipDisplay = "--";
            _fogOverrideActive = false;
        }
    }
}
