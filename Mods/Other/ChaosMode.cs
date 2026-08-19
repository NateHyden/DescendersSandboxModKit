using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class ChaosMode
    {
        public static bool Enabled { get; private set; } = false;

        private static readonly string[] PoolNames = { "Ice Mode", "Mirror Mode", "Drunk Mode", "Reverse Steering" };
        private const int PoolCount = 4;
        private static readonly bool[] _snapshot = new bool[PoolCount];
        private static bool _hasSnapshot = false;

        private static float _nextFlipTime = 0f;
        private const float MinInterval = 4f;
        private const float MaxInterval = 9f;

        public static string LastFlipDisplay { get; private set; } = "--";

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled)
            {
                for (int i = 0; i < PoolCount; i++) _snapshot[i] = GetPoolEnabled(i);
                _hasSnapshot = true;
                ScheduleNext();
                ModLog.Debug("[ChaosMode] ON — snapshotted " + PoolCount + " mods.");
            }
            else
            {
                RestoreAll();
                ModLog.Debug("[ChaosMode] OFF — restored original states.");
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

            int idx = Random.Range(0, PoolCount);
            try
            {
                TogglePool(idx);
                LastFlipDisplay = PoolNames[idx] + " " + (GetPoolEnabled(idx) ? "ON" : "OFF");
                ModLog.Debug("[ChaosMode] Flipped " + LastFlipDisplay);
            }
            catch (System.Exception ex) { MelonLogger.Error("[ChaosMode] Flip " + PoolNames[idx] + ": " + ex.Message);  Telemetry.ReportErrorAsync(ex, "ChaosMode"); }
            ScheduleNext();
        }

        private static bool GetPoolEnabled(int i)
        {
            switch (i)
            {
                case 0: return IceMode.Enabled;
                case 1: return MirrorMode.Enabled;
                case 2: return DrunkMode.Enabled;
                case 3: return ReverseSteering.Enabled;
                default: return false;
            }
        }

        private static void TogglePool(int i)
        {
            switch (i)
            {
                case 0: IceMode.Toggle(); break;
                case 1: MirrorMode.Toggle(); break;
                case 2: DrunkMode.Toggle(); break;
                case 3: ReverseSteering.Toggle(); break;
            }
        }

        private static void RestoreAll()
        {
            if (!_hasSnapshot) return;
            for (int i = 0; i < PoolCount; i++)
            {
                try { if (GetPoolEnabled(i) != _snapshot[i]) TogglePool(i); }
                catch (System.Exception ex) { MelonLogger.Error("[ChaosMode] Restore " + PoolNames[i] + ": " + ex.Message);  Telemetry.ReportErrorAsync(ex, "ChaosMode"); }
            }
            _hasSnapshot = false;
        }

        public static void Reset()
        {
            if (Enabled) RestoreAll();
            Enabled = false;
            LastFlipDisplay = "--";
        }
    }
}

