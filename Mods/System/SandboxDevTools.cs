using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using DescendersModMenu;
using DescendersModMenu.UI;

namespace DescendersModMenu.Mods
{
    /// <summary>Hidden developer diagnostics helpers (gated by DevLock).</summary>
    public static class SandboxDevTools
    {
        private const int RoomLogMax = 24;
        private const int FindResultMax = 40;

        private static readonly List<string> _roomLog = new List<string>();
        private static bool _roomKnown;
        private static bool _wasInRoom;
        private static string _lastRoom = "";
        private static int _lastPlayerCount = -1;
        private static string _lastScene = "";

        private static float _fps;
        private static float _fpsAccum;
        private static int _fpsFrames;
        private static float _fpsWindowStart;
        private static Texture2D _perfTex;

        public static bool PerfOverlayEnabled { get; private set; }
        public static string LastActionResult { get; set; }
        public static string LastProbeResult { get; private set; }

        public static bool NameSpoofEnabled { get; private set; }
        public static string SpoofedName { get; private set; } = "";
        public static string OriginalName { get; private set; } = "";

        private static float _nextNameReapply;
        private static PropertyInfo _nickProp;
        private static PropertyInfo _localPlayerProp;
        private static Type _photonNetType;
        private static readonly string PhotonNetName = "upVWa\u0084E";
        private static readonly string LocalPlayerName = "gQ\u0060\u0083tus";
        private static readonly string NickNameName = "DiQND\u0080L";

        public static IList<string> RoomLog { get { return _roomLog; } }

        public static void TogglePerfOverlay()
        {
            PerfOverlayEnabled = !PerfOverlayEnabled;
            LastActionResult = "Perf overlay " + (PerfOverlayEnabled ? "ON" : "OFF");
            ModLog.Feedback("[DevTools] " + LastActionResult);
        }

        public static void Tick()
        {
            if (PerfOverlayEnabled) TickPerf();
            if (DevLock.IsUnlocked) TickRoomLog();
            TickNameSpoof();
        }

        public static bool ApplyNameSpoof(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                LastActionResult = "Enter a name first";
                return false;
            }
            name = name.Trim();
            if (name.Length > 32) name = name.Substring(0, 32);

            try
            {
                if (string.IsNullOrEmpty(OriginalName))
                    OriginalName = ReadPhotonNick();

                if (!SetPhotonNick(name))
                {
                    LastActionResult = "Could not set Photon nick (not in room?)";
                    return false;
                }

                NameSpoofEnabled = true;
                SpoofedName = name;
                _nextNameReapply = Time.unscaledTime + 1.5f;
                LastActionResult = "Name spoof -> \"" + name + "\"";
                ModLog.Feedback("[DevTools] " + LastActionResult);
                return true;
            }
            catch (Exception ex)
            {
                LastActionResult = "Name spoof failed: " + ex.Message;
                MelonLogger.Error("[DevTools] NameSpoof: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "DevTools.NameSpoof");
                return false;
            }
        }

        public static void ClearNameSpoof()
        {
            try
            {
                NameSpoofEnabled = false;
                string restore = !string.IsNullOrEmpty(OriginalName) ? OriginalName : "";
                SpoofedName = "";
                if (!string.IsNullOrEmpty(restore))
                    SetPhotonNick(restore);
                OriginalName = "";
                LastActionResult = string.IsNullOrEmpty(restore)
                    ? "Name spoof cleared"
                    : "Name restored -> \"" + restore + "\"";
                ModLog.Feedback("[DevTools] " + LastActionResult);
            }
            catch (Exception ex)
            {
                LastActionResult = "Clear spoof failed: " + ex.Message;
                MelonLogger.Error("[DevTools] ClearNameSpoof: " + ex.Message);
            }
        }

        private static void TickNameSpoof()
        {
            if (!NameSpoofEnabled || string.IsNullOrEmpty(SpoofedName)) return;
            float now = Time.unscaledTime;
            if (now < _nextNameReapply) return;
            _nextNameReapply = now + 2f;
            try
            {
                string cur = ReadPhotonNick();
                if (!string.Equals(cur, SpoofedName, StringComparison.Ordinal))
                    SetPhotonNick(SpoofedName);
            }
            catch { }
        }

        private static bool ResolvePhotonNick()
        {
            try
            {
                if ((object)_nickProp != null && (object)_localPlayerProp != null) return true;

                Assembly asm = null;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    if (string.Equals(assemblies[i].GetName().Name, "Assembly-CSharp", StringComparison.Ordinal))
                    { asm = assemblies[i]; break; }
                }
                if ((object)asm == null) return false;

                if ((object)_photonNetType == null)
                {
                    Type[] types = asm.GetTypes();
                    for (int i = 0; i < types.Length; i++)
                    {
                        if (string.Equals(types[i].Name, PhotonNetName, StringComparison.Ordinal))
                        { _photonNetType = types[i]; break; }
                    }
                }
                if ((object)_photonNetType == null) return false;

                if ((object)_localPlayerProp == null)
                    _localPlayerProp = _photonNetType.GetProperty(LocalPlayerName, BindingFlags.Public | BindingFlags.Static);
                if ((object)_localPlayerProp == null) return false;

                object local = _localPlayerProp.GetValue(null, null);
                if ((object)local == null) return false;

                if ((object)_nickProp == null)
                    _nickProp = local.GetType().GetProperty(NickNameName, BindingFlags.Public | BindingFlags.Instance);

                return (object)_nickProp != null && _nickProp.CanWrite;
            }
            catch { return false; }
        }

        private static string ReadPhotonNick()
        {
            try
            {
                if (!ResolvePhotonNick()) return "";
                object local = _localPlayerProp.GetValue(null, null);
                if ((object)local == null) return "";
                object n = _nickProp.GetValue(local, null);
                return n != null ? n.ToString() : "";
            }
            catch { return ""; }
        }

        private static bool SetPhotonNick(string name)
        {
            if (!ResolvePhotonNick()) return false;
            object local = _localPlayerProp.GetValue(null, null);
            if ((object)local == null) return false;
            _nickProp.SetValue(local, name, null);
            return true;
        }

        public static void OnSceneChanged(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (string.Equals(sceneName, _lastScene, StringComparison.Ordinal)) return;
            _lastScene = sceneName;
            PushLog("map -> " + sceneName);
        }

        public static void RetagLocal()
        {
            try
            {
                ModDetection.ResetTag();
                ModDetection.TagLocalPlayer();
                ModDetection.Scan();
                LastActionResult = "Retagged local player (v" + ModDetection.ModVersion + ")";
                ModLog.Feedback("[DevTools] " + LastActionResult);
            }
            catch (Exception ex)
            {
                LastActionResult = "Retag failed: " + ex.Message;
                MelonLogger.Error("[DevTools] Retag: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "DevTools.Retag");
            }
        }

        public static void ForceScan()
        {
            try
            {
                ModDetection.Scan();
                int n = ModDetection.ModUsers != null ? ModDetection.ModUsers.Count : 0;
                LastActionResult = "Scan complete — " + n + " mod user(s)";
                ModLog.Feedback("[DevTools] " + LastActionResult);
            }
            catch (Exception ex)
            {
                LastActionResult = "Scan failed: " + ex.Message;
                MelonLogger.Error("[DevTools] Scan: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "DevTools.Scan");
            }
        }

        public static string DumpPlayerProps()
        {
            string dump = ModDetection.FormatAllPlayerPropsDump();
            MelonLogger.Msg("[DevTools] Player props dump:\n" + dump);
            LastActionResult = "Props dump written to MelonLoader log";
            ModLog.Feedback("[DevTools] " + LastActionResult);
            return dump;
        }

        public static string ProbeChat()
        {
            string result = ModChat.ProbeRaiseEvent();
            LastProbeResult = result;
            LastActionResult = result;
            ModLog.Feedback("[DevTools] Probe: " + result);
            return result;
        }

        public static void ForceGc()
        {
            try
            {
                long before = GC.GetTotalMemory(false);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                long after = GC.GetTotalMemory(true);
                LastActionResult = "GC done — " + FormatBytes(before) + " -> " + FormatBytes(after);
                ModLog.Feedback("[DevTools] " + LastActionResult);
            }
            catch (Exception ex)
            {
                LastActionResult = "GC failed: " + ex.Message;
                MelonLogger.Error("[DevTools] GC: " + ex.Message);
            }
        }

        public static void UnloadUnused()
        {
            try
            {
                AsyncOperation op = Resources.UnloadUnusedAssets();
                LastActionResult = (object)op != null
                    ? "UnloadUnusedAssets started"
                    : "UnloadUnusedAssets returned null";
                ModLog.Feedback("[DevTools] " + LastActionResult);
            }
            catch (Exception ex)
            {
                LastActionResult = "Unload failed: " + ex.Message;
                MelonLogger.Error("[DevTools] Unload: " + ex.Message);
            }
        }

        public static string GetSceneInfo()
        {
            try
            {
                Scene active = SceneManager.GetActiveScene();
                string name = active.IsValid() ? active.name : "?";
                int roots = active.IsValid() ? active.rootCount : 0;
                int ddol = CountDdolRoots();
                return name + "  |  roots " + roots + "  |  DDOL " + ddol;
            }
            catch (Exception ex)
            {
                return "err: " + ex.Message;
            }
        }

        public static string GetLoadedModsText()
        {
            try
            {
                var registered = MelonBase.RegisteredMelons;
                if (registered == null) return "(none)";
                StringBuilder sb = new StringBuilder();
                int i = 0;
                foreach (var melon in registered)
                {
                    if ((object)melon == null) continue;
                    if (i > 0) sb.Append('\n');
                    string n = melon.Info != null ? melon.Info.Name : "?";
                    string v = melon.Info != null ? melon.Info.Version : "?";
                    sb.Append(n).Append("  [v").Append(v).Append(']');
                    i++;
                }
                return i > 0 ? sb.ToString() : "(none)";
            }
            catch (Exception ex)
            {
                return "err: " + ex.Message;
            }
        }

        public static string GetPatchStatusText()
        {
            try
            {
                var list = DiagnosticsManager.Statuses;
                if (list == null || list.Count == 0) return "(no reports yet)";
                StringBuilder sb = new StringBuilder();
                int fails = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    ModStatus s = list[i];
                    if ((object)s == null) continue;
                    if (!s.OK)
                    {
                        fails++;
                        if (sb.Length > 0) sb.Append('\n');
                        sb.Append("FAIL  ").Append(s.Name);
                        if (!string.IsNullOrEmpty(s.Error))
                            sb.Append(" — ").Append(s.Error);
                    }
                }
                if (fails == 0)
                    return "All OK (" + DiagnosticsManager.OKCount + " reported)";
                return fails + " failed / " + list.Count + " total\n" + sb.ToString();
            }
            catch (Exception ex)
            {
                return "err: " + ex.Message;
            }
        }

        public static string BuildSessionSnapshot()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Descenders Sandbox snapshot");
            sb.AppendLine("time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("mod: v" + BuildInfo.Version);
            sb.AppendLine("unity: " + DiagnosticsManager.UnityVersion);
            sb.AppendLine("melon: " + DiagnosticsManager.MelonLoaderVersion);
            sb.AppendLine("scene: " + GetSceneInfo());
            sb.AppendLine("photon: " + ModChat.ConnectionStateLabel
                + " | inRoom=" + ModChat.InRoom
                + " | offline=" + ModChat.OfflineMode);
            sb.AppendLine("room: " + (string.IsNullOrEmpty(ModChat.RoomName) ? "(none)" : ModChat.RoomName));
            sb.AppendLine("players: " + ModChat.PlayerListCount);
            sb.AppendLine("local: " + ModChat.LocalPlayerName);
            int mods = ModDetection.ModUsers != null ? ModDetection.ModUsers.Count : 0;
            sb.AppendLine("modUsers: " + mods);
            if (ModDetection.ModUsers != null)
            {
                for (int i = 0; i < ModDetection.ModUsers.Count; i++)
                {
                    var u = ModDetection.ModUsers[i];
                    if ((object)u == null) continue;
                    sb.AppendLine("  - " + u.Name + " [v" + u.Version + "]");
                }
            }
            sb.AppendLine("patches: OK=" + DiagnosticsManager.OKCount
                + " FAIL=" + DiagnosticsManager.FailCount);
            sb.AppendLine("fps: " + _fps.ToString("0.0"));
            sb.AppendLine("timeScale: " + Time.timeScale.ToString("0.###"));
            sb.AppendLine("loadedMods:");
            sb.AppendLine(GetLoadedModsText());
            return sb.ToString();
        }

        public static void CopySessionSnapshot()
        {
            try
            {
                string snap = BuildSessionSnapshot();
                GUIUtility.systemCopyBuffer = snap;
                MelonLogger.Msg("[DevTools] Session snapshot:\n" + snap);
                LastActionResult = "Snapshot copied to clipboard (+ log)";
                ModLog.Feedback("[DevTools] " + LastActionResult);
            }
            catch (Exception ex)
            {
                LastActionResult = "Copy failed: " + ex.Message;
                MelonLogger.Error("[DevTools] Snapshot: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "DevTools.Snapshot");
            }
        }

        public static string FindComponents(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                LastActionResult = "Enter a type name to search";
                return LastActionResult;
            }

            try
            {
                string q = query.Trim();
                UnityEngine.Object[] all = Resources.FindObjectsOfTypeAll(typeof(Component));
                StringBuilder sb = new StringBuilder();
                int hits = 0;
                for (int i = 0; i < all.Length; i++)
                {
                    Component c = all[i] as Component;
                    if ((object)c == null) continue;
                    Type t = c.GetType();
                    string tn = t.Name;
                    string fn = t.FullName;
                    if (tn.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0
                        && ((object)fn == null || fn.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0))
                        continue;

                    hits++;
                    if (hits <= FindResultMax)
                    {
                        if (sb.Length > 0) sb.Append('\n');
                        string path = c.gameObject != null ? GetPath(c.gameObject) : "?";
                        sb.Append(tn).Append("  @  ").Append(path);
                    }
                }

                if (hits == 0)
                {
                    LastActionResult = "No matches for \"" + q + "\"";
                    return LastActionResult;
                }

                string header = hits + " match(es) for \"" + q + "\"";
                if (hits > FindResultMax)
                    header += " (showing " + FindResultMax + ")";
                LastActionResult = header;
                return header + "\n" + sb.ToString();
            }
            catch (Exception ex)
            {
                LastActionResult = "Find failed: " + ex.Message;
                MelonLogger.Error("[DevTools] Find: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "DevTools.Find");
                return LastActionResult;
            }
        }

        public static string FormatRoomLog()
        {
            if (_roomLog.Count == 0) return "(no events yet)";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _roomLog.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(_roomLog[i]);
            }
            return sb.ToString();
        }

        public static string PerfPanelText()
        {
            return _fps.ToString("0.0") + " fps   "
                + (1000f / Mathf.Max(_fps, 0.01f)).ToString("0.0") + " ms   "
                + "scale " + Time.timeScale.ToString("0.##") + "   "
                + "mem " + FormatBytes(GC.GetTotalMemory(false));
        }

        public static void DrawPerfOverlay()
        {
            if (!PerfOverlayEnabled) return;

            float s = Screen.height / 1080f;
            float pad = 10f * s;
            float w = 280f * s;
            float h = 28f * s;
            float x = Screen.width - w - 18f * s;
            float y = 18f * s;
            if (SessionHUD.Enabled && SessionHUD.LastDrawnHeight > 0f)
                y += SessionHUD.LastDrawnHeight + 8f * s;
            if (ModUsersHUD.Enabled)
                y += 36f * s;

            if ((object)_perfTex == null)
            {
                _perfTex = new Texture2D(1, 1);
                _perfTex.SetPixel(0, 0, Color.white);
                _perfTex.Apply();
            }

            Color bg = new Color(0.05f, 0.06f, 0.08f, 0.88f);
            Color accent = new Color(0.25f, 0.65f, 1f, 1f);
            GUI.color = bg;
            GUI.DrawTexture(new Rect(x, y, w, h), _perfTex);
            GUI.color = accent;
            GUI.DrawTexture(new Rect(x, y, 3f * s, h), _perfTex);
            GUI.color = Color.white;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(10, Mathf.RoundToInt(12f * s)),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = accent }
            };
            GUI.Label(new Rect(x + pad, y, w - pad * 2f, h), PerfPanelText(), style);
        }

        private static void TickPerf()
        {
            float now = Time.unscaledTime;
            if (_fpsWindowStart <= 0f) _fpsWindowStart = now;
            _fpsAccum += Time.unscaledDeltaTime;
            _fpsFrames++;
            if (now - _fpsWindowStart >= 0.5f)
            {
                _fps = _fpsFrames / Mathf.Max(_fpsAccum, 0.0001f);
                _fpsAccum = 0f;
                _fpsFrames = 0;
                _fpsWindowStart = now;
            }
        }

        private static float _nextRoomLogCheck;
        private const float RoomLogInterval = 0.5f;

        private static void TickRoomLog()
        {
            // RoomName allocates; do not poll every frame while DevLock is unlocked.
            float now = Time.unscaledTime;
            if (now < _nextRoomLogCheck) return;
            _nextRoomLogCheck = now + RoomLogInterval;

            bool inRoom = ModChat.InRoom;
            string room = ModChat.RoomName ?? "";
            int count = ModChat.PlayerListCount;

            if (!_roomKnown)
            {
                _roomKnown = true;
                _wasInRoom = inRoom;
                _lastRoom = room;
                _lastPlayerCount = count;
                if (inRoom)
                    PushLog("in room \"" + room + "\" (" + count + "p)");
                return;
            }

            if (inRoom && !_wasInRoom)
                PushLog("joined \"" + room + "\" (" + count + "p)");
            else if (!inRoom && _wasInRoom)
                PushLog("left \"" + _lastRoom + "\"");
            else if (inRoom && !string.Equals(room, _lastRoom, StringComparison.Ordinal))
                PushLog("room \"" + _lastRoom + "\" -> \"" + room + "\"");

            if (inRoom && count != _lastPlayerCount && _lastPlayerCount >= 0)
                PushLog("players " + _lastPlayerCount + " -> " + count);

            _wasInRoom = inRoom;
            _lastRoom = room;
            _lastPlayerCount = count;
        }

        private static void PushLog(string line)
        {
            string entry = DateTime.Now.ToString("HH:mm:ss") + "  " + line;
            _roomLog.Add(entry);
            while (_roomLog.Count > RoomLogMax)
                _roomLog.RemoveAt(0);
            ModLog.Debug("[DevTools] " + entry);
        }

        private static int CountDdolRoots()
        {
            GameObject temp = null;
            try
            {
                temp = new GameObject("DevTools_DDOLProbe");
                UnityEngine.Object.DontDestroyOnLoad(temp);
                Scene ddol = temp.scene;
                return ddol.IsValid() ? Mathf.Max(0, ddol.rootCount - 1) : 0;
            }
            catch { return -1; }
            finally
            {
                if ((object)temp != null)
                    UnityEngine.Object.Destroy(temp);
            }
        }

        private static string GetPath(GameObject go)
        {
            if ((object)go == null) return "?";
            string path = go.name;
            Transform t = go.transform.parent;
            int guard = 0;
            while ((object)t != null && guard++ < 32)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            double kb = bytes / 1024.0;
            if (kb < 1024) return kb.ToString("0.0") + " KB";
            return (kb / 1024.0).ToString("0.00") + " MB";
        }
    }
}
