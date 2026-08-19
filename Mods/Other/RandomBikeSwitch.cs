using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class RandomBikeSwitch
    {
        public static bool Enabled { get; private set; } = false;

        private const float MinInterval = 4f;
        private const float MaxInterval = 9f;
        private static float _nextSwitchTime = 0f;

        private static int _snapshotIndex = 0;
        private static bool _hasSnapshot = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled)
            {
                _snapshotIndex = BikeSwitcher.CurrentBikeIndex;
                _hasSnapshot = true;
                ScheduleNext();
                ModLog.Debug("[RandomBikeSwitch] ON — snapshotted bike index " + _snapshotIndex + ".");
            }
            else
            {
                RestoreSnapshot();
                ModLog.Debug("[RandomBikeSwitch] OFF — restored original bike.");
            }
        }

        private static void ScheduleNext()
        {
            _nextSwitchTime = Time.unscaledTime + Random.Range(MinInterval, MaxInterval);
        }

        public static void Tick()
        {
            if (!Enabled) return;
            if (Time.unscaledTime < _nextSwitchTime) return;
            try
            {
                int hops = Random.Range(1, 5);
                for (int i = 0; i < hops; i++) BikeSwitcher.NextBike();
                ModLog.Feedback("[RandomBikeSwitch] Switched (" + hops + " hop(s)) -> index " + BikeSwitcher.CurrentBikeIndex);
            }
            catch (System.Exception ex) { MelonLogger.Error("[RandomBikeSwitch] Tick: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "RandomBikeSwitch"); }
            ScheduleNext();
        }

        private static void RestoreSnapshot()
        {
            if (!_hasSnapshot) return;
            try { BikeSwitcher.SetBike(_snapshotIndex); }
            catch (System.Exception ex) { MelonLogger.Error("[RandomBikeSwitch] RestoreSnapshot: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "RandomBikeSwitch"); }
            _hasSnapshot = false;
        }

        public static void Reset()
        {
            if (Enabled) RestoreSnapshot();
            Enabled = false;
        }
    }
}

