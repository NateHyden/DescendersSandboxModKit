using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Tap-to-unlock gate for the "DEVELOPER DIAGNOSTICS" section (career
    /// progression / rep / unlock-all tools). Same idea as Android's hidden
    /// developer options: tap the section header several times within a short
    /// window to reveal it.
    ///
    /// Why tap-to-unlock and not a typed password: this codebase has no
    /// text-input system anywhere - no InputField, no on-screen keyboard - it's
    /// a controller-first UI (DPad navigation, gamepad binds throughout). A real
    /// password-entry screen would mean building a virtual keyboard from
    /// scratch just for this one feature.
    ///
    /// Session-only: relocks every app restart, does not persist to disk.
    /// </summary>
    public static class DevLock
    {
        private const int TapsRequired = 7;
        private const float TapWindow = 3f; // seconds allowed between taps before the streak resets

        public static bool IsUnlocked { get; private set; } = false;

        private static int _tapCount = 0;
        private static float _lastTapTime = -999f;

        public static int TapsRemaining
        {
            get { return IsUnlocked ? 0 : Mathf.Max(0, TapsRequired - _tapCount); }
        }

        public static void RegisterTap()
        {
            if (IsUnlocked) return;

            float now = Time.realtimeSinceStartup;
            if (now - _lastTapTime > TapWindow) _tapCount = 0; // streak expired, restart count
            _lastTapTime = now;
            _tapCount++;

            ModLog.Debug("[DevLock] Tap " + _tapCount + "/" + TapsRequired);

            if (_tapCount >= TapsRequired)
            {
                IsUnlocked = true;
                ModLog.Debug("[DevLock] Developer Diagnostics unlocked.");
            }
        }

        public static void Lock()
        {
            IsUnlocked = false;
            _tapCount = 0;
        }
    }
}
