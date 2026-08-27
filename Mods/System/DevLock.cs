using UnityEngine;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Session-only unlock for the System page internal panel.
    /// Trigger: tap header "Created by NateHyden" enough times within a short window.
    /// </summary>
    public static class DevLock
    {
        private const int TapsRequired = 10;
        private const float TapWindow = 2.4f;

        public static bool IsUnlocked { get; private set; }

        private static int _tapCount;
        private static float _lastTapTime = -999f;

        public static void RegisterTap()
        {
            if (IsUnlocked) return;

            float now = Time.realtimeSinceStartup;
            if (now - _lastTapTime > TapWindow) _tapCount = 0;
            _lastTapTime = now;
            _tapCount++;

            if (_tapCount >= TapsRequired)
            {
                IsUnlocked = true;
                _tapCount = 0;
            }
        }

        public static void Lock()
        {
            IsUnlocked = false;
            _tapCount = 0;
        }
    }
}
