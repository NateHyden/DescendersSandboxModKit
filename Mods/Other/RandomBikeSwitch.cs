using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Automatically cycles to a different bike every few seconds, purely
    // by calling BikeSwitcher.NextBike() a random number of times per tick
    // (1-4) — reuses BikeSwitcher's existing index wraparound entirely, no
    // need to know the total bike count. Snapshots the bike you were on
    // when enabled and restores it exactly on disable.
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
                MelonLogger.Msg("[RandomBikeSwitch] ON — snapshotted bike index " + _snapshotIndex + ".");
            }
            else
            {
                RestoreSnapshot();
                MelonLogger.Msg("[RandomBikeSwitch] OFF — restored original bike.");
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
                int hops = Random.Range(1, 5); // 1-4 — feels random without needing the total bike count
                for (int i = 0; i < hops; i++) BikeSwitcher.NextBike();
                ModLog.Feedback("[RandomBikeSwitch] Switched (" + hops + " hop(s)) -> index " + BikeSwitcher.CurrentBikeIndex);
            }
            catch (System.Exception ex) { MelonLogger.Error("[RandomBikeSwitch] Tick: " + ex.Message); }
            ScheduleNext();
        }

        private static void RestoreSnapshot()
        {
            if (!_hasSnapshot) return;
            try { BikeSwitcher.SetBike(_snapshotIndex); }
            catch (System.Exception ex) { MelonLogger.Error("[RandomBikeSwitch] RestoreSnapshot: " + ex.Message); }
            _hasSnapshot = false;
        }

        public static void Reset()
        {
            if (Enabled) RestoreSnapshot();
            Enabled = false;
        }
    }
}
