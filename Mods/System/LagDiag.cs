using System;
using System.Text;
using MelonLoader;
using UnityEngine;
using DescendersModMenu;
using DescendersModMenu.UI;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Interval sampler for progressive lobby lag.
    /// Always starts OFF each game launch (opt-in). Once ON, stays ON for this
    /// session until toggled off — including while the menu is closed.
    /// </summary>
    public static class LagDiag
    {
        private static MelonPreferences_Category _cat;
        private static MelonPreferences_Entry<float> _intervalEntry;
        private static MelonPreferences_Entry<bool> _heavyEntry;

        private static bool _sessionEnabled;

        private static float _nextSample;
        private static float _fpsAccum;
        private static int _fpsFrames;
        private static float _windowStart;

        private static long _prevGcBytes;
        private static int _prevPhotonPlayers = -1;
        private static int _joinLeaveEvents;

        // Cheap always-on counters (ints only). Reset each sample when logging.
        public static int ModDetectTagAttempts;
        public static int ModDetectTagSuccess;
        public static int ModDetectScans;
        public static int ModChatEvents;
        public static int ModChatMessages;
        public static int LuxMaterialsAccess;
        public static int ChatPageTicks;
        public static int EspPageTicks;
        public static int GhostPageTicks;

        public static bool Enabled
        {
            get { return _sessionEnabled; }
        }

        public static bool Heavy
        {
            get { return _heavyEntry != null && _heavyEntry.Value; }
        }

        public static float IntervalSec
        {
            get
            {
                if ((object)_intervalEntry == null) return 5f;
                float v = _intervalEntry.Value;
                if (v < 2f) return 2f;
                if (v > 60f) return 60f;
                return v;
            }
        }

        public static string LastSampleLine { get; private set; } = "";

        public static void Init()
        {
            try
            {
                _cat = MelonPreferences.CreateCategory("DescendersSandbox_LagDiag", "Lag Diagnostics");
                // Legacy "Enabled" entry — force false so an old prefs file cannot leave it ON.
                var legacyEnabled = _cat.CreateEntry("Enabled", false,
                    "Deprecated: LagDiag is session-only (always starts OFF)");
                legacyEnabled.Value = false;

                _intervalEntry = _cat.CreateEntry("IntervalSec", 5f,
                    "Seconds between samples (2–60)");
                _heavyEntry = _cat.CreateEntry("HeavyVehicleCount", false,
                    "Also FindObjectsOfType Vehicle count each sample (slight hitch)");
                // Never leave Heavy stuck ON from an old prefs file — it allocates every sample.
                _heavyEntry.Value = false;

                _sessionEnabled = false;
                _windowStart = Time.unscaledTime;
                _nextSample = Time.unscaledTime + IntervalSec;
                ModLog.Debug("[LagDiag] Init session-off (opt-in)");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[LagDiag] Init: " + ex.Message);
            }
        }

        public static void Toggle()
        {
            if ((object)_intervalEntry == null) Init();
            _sessionEnabled = !_sessionEnabled;
            if (_sessionEnabled)
            {
                ResetWindowCounters();
                _nextSample = Time.unscaledTime + IntervalSec;
                ModLog.Feedback("[LagDiag] ON - samples every " + IntervalSec.ToString("0")
                    + "s in Melon log (stays on until you turn it off; resets off next launch)");
            }
            else
            {
                ModLog.Feedback("[LagDiag] OFF");
            }
        }

        public static void ToggleHeavy()
        {
            if ((object)_heavyEntry == null) Init();
            if ((object)_heavyEntry == null) return;
            _heavyEntry.Value = !_heavyEntry.Value;
            MelonPreferences.Save();
            ModLog.Feedback("[LagDiag] Heavy vehicle count -> " + (_heavyEntry.Value ? "ON" : "OFF"));
        }

        public static void Tick()
        {
            if (!_sessionEnabled) return;

            float now = Time.unscaledTime;
            _fpsAccum += Time.unscaledDeltaTime;
            _fpsFrames++;

            // Join/leave counted only on sample (PlayerListCount is reflection).
            if (now < _nextSample) return;
            _nextSample = now + IntervalSec;

            int players = 0;
            try { players = ModChat.PlayerListCount; }
            catch { }
            if (_prevPhotonPlayers >= 0 && players != _prevPhotonPlayers)
                _joinLeaveEvents++;
            _prevPhotonPlayers = players;

            SampleAndLog();
        }

        public static void SampleNow()
        {
            if ((object)_intervalEntry == null) Init();
            if (!_sessionEnabled)
            {
                _sessionEnabled = true;
                ResetWindowCounters();
                _nextSample = Time.unscaledTime + IntervalSec;
            }
            SampleAndLog();
            ModLog.Feedback("[LagDiag] Sample logged.");
        }

        private static void SampleAndLog()
        {
            try
            {
                float elapsed = Time.unscaledTime - _windowStart;
                if (elapsed < 0.001f) elapsed = 0.001f;
                float fps = _fpsFrames / elapsed;
                float ms = fps > 0.01f ? 1000f / fps : 0f;

                long gc = GC.GetTotalMemory(false);
                long gcDelta = _prevGcBytes > 0 ? (gc - _prevGcBytes) : 0;
                _prevGcBytes = gc;

                bool inRoom = false;
                int players = 0;
                string room = "";
                try
                {
                    inRoom = ModChat.InRoom;
                    players = ModChat.PlayerListCount;
                    room = ModChat.RoomName ?? "";
                }
                catch { }

                int modUsers = 0;
                try
                {
                    if (ModDetection.ModUsers != null)
                        modUsers = ModDetection.ModUsers.Count;
                }
                catch { }

                int vehicles = -1;
                int controllers = -1;
                if (Heavy)
                {
                    try
                    {
                        vehicles = UnityEngine.Object.FindObjectsOfType<Vehicle>().Length;
                        controllers = UnityEngine.Object.FindObjectsOfType<VehicleController>().Length;
                    }
                    catch { }
                }

                bool luxOn = false;
                try { luxOn = LuxGlowTint.AnyEnabled; }
                catch { }

                bool espOn = false;
                try { espOn = ESP.Enabled; }
                catch { }

                bool menuOpen = false;
                try { menuOpen = MenuUI.IsOpen; }
                catch { }

                StringBuilder sb = new StringBuilder(256);
                sb.Append("[LagDiag] fps=");
                sb.Append(fps.ToString("0.0"));
                sb.Append(" ms=");
                sb.Append(ms.ToString("0.0"));
                sb.Append(" gcMB=");
                sb.Append((gc / (1024f * 1024f)).ToString("0.0"));
                sb.Append(" dGcKB=");
                sb.Append((gcDelta / 1024f).ToString("0"));
                sb.Append(" room=");
                sb.Append(inRoom ? "Y" : "N");
                sb.Append(" players=");
                sb.Append(players);
                sb.Append(" joins=");
                sb.Append(_joinLeaveEvents);
                sb.Append(" modUsers=");
                sb.Append(modUsers);
                sb.Append(" tagA=");
                sb.Append(ModDetectTagAttempts);
                sb.Append(" tagOk=");
                sb.Append(ModDetectTagSuccess);
                sb.Append(" scans=");
                sb.Append(ModDetectScans);
                sb.Append(" chatEv=");
                sb.Append(ModChatEvents);
                sb.Append(" chatMsg=");
                sb.Append(ModChatMessages);
                sb.Append(" luxMat=");
                sb.Append(LuxMaterialsAccess);
                sb.Append(" chatTick=");
                sb.Append(ChatPageTicks);
                sb.Append(" espTick=");
                sb.Append(EspPageTicks);
                sb.Append(" ghostTick=");
                sb.Append(GhostPageTicks);
                sb.Append(" lux=");
                sb.Append(luxOn ? "Y" : "N");
                sb.Append(" esp=");
                sb.Append(espOn ? "Y" : "N");
                sb.Append(" menu=");
                sb.Append(menuOpen ? "Y" : "N");
                if (vehicles >= 0)
                {
                    sb.Append(" vehicles=");
                    sb.Append(vehicles);
                    sb.Append(" vc=");
                    sb.Append(controllers);
                }
                if (!string.IsNullOrEmpty(room))
                {
                    sb.Append(" roomName=");
                    sb.Append(room.Length > 24 ? room.Substring(0, 24) : room);
                }

                LastSampleLine = sb.ToString();
                MelonLogger.Msg(LastSampleLine);

                ResetWindowCounters();
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[LagDiag] Sample: " + ex.Message);
            }
        }

        private static void ResetWindowCounters()
        {
            _fpsAccum = 0f;
            _fpsFrames = 0;
            _windowStart = Time.unscaledTime;
            _joinLeaveEvents = 0;
            ModDetectTagAttempts = 0;
            ModDetectTagSuccess = 0;
            ModDetectScans = 0;
            ModChatEvents = 0;
            ModChatMessages = 0;
            LuxMaterialsAccess = 0;
            ChatPageTicks = 0;
            EspPageTicks = 0;
            GhostPageTicks = 0;
        }
    }
}
