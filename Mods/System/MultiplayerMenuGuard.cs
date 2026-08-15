using System;
using System.Reflection;
using MelonLoader;
using DescendersModMenu;
using DescendersModMenu.UI;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Locks the Sandbox menu while MultiManager reports Multiplayer Race Mode.
    // Detected via game API (InMPRaceMode / GetMultiPlayerSessionType) — no mod
    // needed on other players. Lobby / freeride / career multiplayer stay unlocked.
    public static class MultiplayerMenuGuard
    {
        public static bool InRaceMode { get; private set; }

        private static float _nextCheck;
        private const float CheckInterval = 0.5f;

        private static MultiManager _mm;
        private static float _nextMmFind;
        private static MethodInfo _inMpRaceMode;
        private static MethodInfo _getSessionType;
        private static bool _resolved;
        private static bool _resolveFailed;
        private static bool _loggedResolve;

        public static void Reset()
        {
            if (InRaceMode)
                LeaveRaceMode();
            _mm = null;
            _nextCheck = 0f;
            _nextMmFind = 0f;
        }

        public static void Tick()
        {
            float now = Time.unscaledTime;
            if (now < _nextCheck) return;
            _nextCheck = now + CheckInterval;

            bool race = DetectRaceMode();
            if (race == InRaceMode) return;

            if (race) EnterRaceMode();
            else LeaveRaceMode();
        }

        private static void EnterRaceMode()
        {
            InRaceMode = true;
            MelonLogger.Msg("[MultiplayerMenuGuard] Joined multiplayer race - Menu disabled");
            MenuUI.SetLocked(true);
        }

        private static void LeaveRaceMode()
        {
            InRaceMode = false;
            MelonLogger.Msg("[MultiplayerMenuGuard] Left multiplayer race - Menu enabled");
            MenuUI.SetLocked(false);
        }

        private static bool DetectRaceMode()
        {
            try
            {
                EnsureResolved();
                if (_resolveFailed) return false;

                if ((object)_mm == null || _mm == null)
                {
                    float now = Time.unscaledTime;
                    if (now < _nextMmFind) return false;
                    _nextMmFind = now + 2f;
                    _mm = UnityEngine.Object.FindObjectOfType<MultiManager>();
                    if ((object)_mm == null) return false;
                }

                // Prefer InMPRaceMode when present
                if ((object)_inMpRaceMode != null)
                {
                    object r = _inMpRaceMode.Invoke(_mm, null);
                    if (r is bool && (bool)r) return true;
                }

                // Fallback: session type name contains RaceMode
                if ((object)_getSessionType != null)
                {
                    object session = _getSessionType.Invoke(_mm, null);
                    if ((object)session != null)
                    {
                        string name = session.ToString();
                        if (!string.IsNullOrEmpty(name)
                            && name.IndexOf("RaceMode", StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[MultiplayerMenuGuard] Detect: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "MultiplayerMenuGuard");
                return false;
            }
        }

        private static void EnsureResolved()
        {
            if (_resolved || _resolveFailed) return;
            _resolved = true;
            try
            {
                Type t = typeof(MultiManager);
                _inMpRaceMode = t.GetMethod("InMPRaceMode", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                if ((object)_inMpRaceMode == null)
                    _inMpRaceMode = t.GetMethod("InMPRaceMode", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                _getSessionType = t.GetMethod("GetMultiPlayerSessionType", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                if ((object)_getSessionType == null)
                    _getSessionType = t.GetMethod("GetMultiPlayerSessionType", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                // Property fallback for InMPRaceMode
                if ((object)_inMpRaceMode == null)
                {
                    PropertyInfo p = t.GetProperty("InMPRaceMode", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                    if ((object)p != null) _inMpRaceMode = p.GetGetMethod(true);
                }

                if ((object)_inMpRaceMode == null && (object)_getSessionType == null)
                {
                    _resolveFailed = true;
                    MelonLogger.Warning("[MultiplayerMenuGuard] MultiManager race APIs not found — menu lock inactive.");
                    return;
                }

                if (!_loggedResolve)
                {
                    _loggedResolve = true;
                    ModLog.Debug("[MultiplayerMenuGuard] Resolved InMPRaceMode="
                        + ((object)_inMpRaceMode != null)
                        + " GetMultiPlayerSessionType=" + ((object)_getSessionType != null));
                }
            }
            catch (Exception ex)
            {
                _resolveFailed = true;
                MelonLogger.Error("[MultiplayerMenuGuard] Resolve: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "MultiplayerMenuGuard");
            }
        }
    }
}
