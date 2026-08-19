using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using MelonLoader;

namespace DescendersModMenu
{
    internal static class Telemetry
    {
        private static bool _hasPinged;

        private static bool _steamClientChecked;
        private static object _steamClientInstance;
        private static Type _steamClientType;

        // ── Preferences ────────────────────────────────────────────────
        private static MelonPreferences_Category _prefCategory;
        private static MelonPreferences_Entry<bool> _prefEnabled;
        private static MelonPreferences_Entry<string> _prefWebhookUrl;
        private static MelonPreferences_Entry<bool> _prefHeaderHintDismissed;

        private static string WebhookUrl => _prefWebhookUrl != null ? _prefWebhookUrl.Value : "";

        private static void EnsurePrefs()
        {
            if (_prefCategory != null) return;
            try
            {
                _prefCategory = MelonPreferences.CreateCategory("DescendersSandbox_Telemetry");
                _prefEnabled = _prefCategory.CreateEntry("EnableTelemetry", false, "Send diagnostics to Dev");
                string builtIn = GetBuiltInWebhookUrl();
                _prefWebhookUrl = _prefCategory.CreateEntry("WebhookUrl", builtIn, "Discord webhook URL (leave blank to disable)");
                _prefHeaderHintDismissed = _prefCategory.CreateEntry("HeaderHintDismissed", false, "Dismissed the header telemetry hint");

                if (string.IsNullOrEmpty(_prefWebhookUrl.Value) && !string.IsNullOrEmpty(builtIn))
                {
                    _prefWebhookUrl.Value = builtIn;
                    MelonPreferences.Save();
                }
            }
            catch { }
        }

        private static string GetBuiltInWebhookUrl()
        {
            try
            {
                Type t = Type.GetType("DescendersModMenu.TelemetryConfig");
                if ((object)t == null) return "";
                FieldInfo f = t.GetField("WebhookUrl", BindingFlags.Public | BindingFlags.Static);
                if ((object)f == null) return "";
                object val = f.GetValue(null);
                return val != null ? val.ToString() : "";
            }
            catch { return ""; }
        }

        public static bool Enabled
        {
            get { EnsurePrefs(); return _prefEnabled != null && _prefEnabled.Value; }
        }

        public static bool HeaderHintDismissed
        {
            get { EnsurePrefs(); return _prefHeaderHintDismissed != null && _prefHeaderHintDismissed.Value; }
        }

        public static void DismissHeaderHint()
        {
            EnsurePrefs();
            if (_prefHeaderHintDismissed == null) return;
            _prefHeaderHintDismissed.Value = true;
            MelonPreferences.Save();
        }

        public static void Toggle()
        {
            EnsurePrefs();
            if (_prefEnabled == null) return;
            _prefEnabled.Value = !_prefEnabled.Value;
            MelonPreferences.Save();

            if (_prefEnabled.Value && !string.IsNullOrEmpty(WebhookUrl))
            {
                try
                {
                    Thread t = new Thread(DoPost);
                    t.IsBackground = true;
                    t.Start();
                }
                catch { }
            }
        }

        // ── Entry point — load ping ───────────────────────────────────
        public static void PingAsync()
        {
            EnsurePrefs();
            if (!Enabled) return;
            if (_hasPinged) return;
            if (string.IsNullOrEmpty(WebhookUrl)) return;
            _hasPinged = true;

            try
            {
                Thread t = new Thread(DoPost);
                t.IsBackground = true;
                t.Start();
            }
            catch { }
        }

        private static readonly HashSet<string> _reportedErrors = new HashSet<string>();
        private static readonly HashSet<string> _reportedWarnings = new HashSet<string>();

        public static void ReportErrorAsync(Exception ex, string activeMod)
        {
            EnsurePrefs();
            if (!Enabled) return;
            if (string.IsNullOrEmpty(WebhookUrl)) return;
            if (ex == null) return;

            string dedupKey = (activeMod ?? "unknown") + "|" + ex.GetType().FullName;
            if (_reportedErrors.Contains(dedupKey)) return;
            _reportedErrors.Add(dedupKey);

            try
            {
                string exType = ex.GetType().FullName;
                string exMsg = Sanitise(ex.Message, 200);
                string stack = (ex.StackTrace ?? "").Replace("\r\n", " | ").Replace("\n", " | ");
                stack = ScrubPath(stack);
                stack = Sanitise(stack, 700);
                string mod = Sanitise(activeMod ?? "unknown", 40);
                string platform = GetPlatform();
                string mlVer = GetMelonLoaderVersion();
                string mods = Sanitise(GetLoadedMods(), 350);
                string rawName = GetPhotonLocalPlayerName();
                if (string.IsNullOrEmpty(rawName)) rawName = GetSteamName();
                string playerName = Sanitise(rawName, 32);

                Thread t = new Thread(() => DoPostError(exType, exMsg, stack, mod, platform, mlVer, mods, playerName));
                t.IsBackground = true;
                t.Start();
            }
            catch { }
        }

        public static void ReportWarningAsync(string message, string activeMod)
        {
            EnsurePrefs();
            if (!Enabled) return;
            if (string.IsNullOrEmpty(WebhookUrl)) return;
            if (string.IsNullOrEmpty(message)) return;

            string dedupKey = (activeMod ?? "unknown") + "|" + message;
            if (_reportedWarnings.Contains(dedupKey)) return;
            _reportedWarnings.Add(dedupKey);

            try
            {
                string msg = Sanitise(message, 400);
                string mod = Sanitise(activeMod ?? "unknown", 40);
                string platform = GetPlatform();
                string mlVer = GetMelonLoaderVersion();
                string mods = Sanitise(GetLoadedMods(), 350);
                string rawName = GetPhotonLocalPlayerName();
                if (string.IsNullOrEmpty(rawName)) rawName = GetSteamName();
                string playerName = Sanitise(rawName, 32);

                Thread t = new Thread(() => DoPostWarning(msg, mod, platform, mlVer, mods, playerName));
                t.IsBackground = true;
                t.Start();
            }
            catch { }
        }

        private static bool _hasSentFeedback = false;
        private static int _lastFeedbackSendTick;
        private const int FeedbackCooldownMs = 15000;

        public enum FeedbackSendState { Idle, Sending, Success, Failed }
        private static FeedbackSendState _feedbackState = FeedbackSendState.Idle;
        public static FeedbackSendState GetFeedbackState() => _feedbackState;

        public static bool CanSendFeedback()
        {
            EnsurePrefs();
            if (string.IsNullOrEmpty(WebhookUrl)) return false;
            if (!_hasSentFeedback) return true;
            return unchecked(Environment.TickCount - _lastFeedbackSendTick) >= FeedbackCooldownMs;
        }

        public static void SendFeedbackAsync(string category, string message)
        {
            EnsurePrefs();
            if (string.IsNullOrEmpty(WebhookUrl)) return;
            if (string.IsNullOrEmpty(message)) return;
            if (!CanSendFeedback()) return;
            _hasSentFeedback = true;
            _lastFeedbackSendTick = Environment.TickCount;
            _feedbackState = FeedbackSendState.Sending;

            try
            {
                string cat = Sanitise(category, 30);
                string msg = Sanitise(message, 900);
                string version = Sanitise(BuildInfo.Version, 16);
                string platform = GetPlatform();
                string rawName = GetPhotonLocalPlayerName();
                if (string.IsNullOrEmpty(rawName)) rawName = GetSteamName();
                string playerName = Sanitise(rawName, 32);

                int color = string.Equals(category, "Bug Report", StringComparison.Ordinal) ? 15158332
                    : string.Equals(category, "Feature Request", StringComparison.Ordinal) ? 3066993
                    : 10181046;

                string json =
                    "{\"embeds\":[{" +
                        "\"color\":" + color + "," +
                        "\"title\":\"Descenders Sandbox — " + cat + "\"," +
                        "\"fields\":[" +
                            "{\"name\":\"Message\",\"value\":\"" + msg + "\",\"inline\":false}," +
                            "{\"name\":\"Player\",\"value\":\"" + playerName + "\",\"inline\":true}," +
                            "{\"name\":\"Platform\",\"value\":\"" + platform + "\",\"inline\":true}," +
                            "{\"name\":\"Version\",\"value\":\"" + version + "\",\"inline\":true}" +
                        "]" +
                    "}]}";

                Thread t = new Thread(() =>
                {
                    bool ok = PostJson(json);
                    _feedbackState = ok ? FeedbackSendState.Success : FeedbackSendState.Failed;
                });
                t.IsBackground = true;
                t.Start();
            }
            catch { _feedbackState = FeedbackSendState.Failed; }
        }

        public static void ReportInitFailuresAsync(List<string> failures)
        {
            EnsurePrefs();
            if (!Enabled) return;
            if (string.IsNullOrEmpty(WebhookUrl)) return;
            if (failures == null || failures.Count == 0) return;

            try
            {
                const int max = 20;
                List<string> safe = new List<string>();
                for (int i = 0; i < failures.Count && i < max; i++)
                    safe.Add(Sanitise(failures[i], 150));
                string extra = failures.Count > max ? " (+" + (failures.Count - max) + " more)" : "";
                string platform = GetPlatform();
                string mlVer = GetMelonLoaderVersion();
                string mods = Sanitise(GetLoadedMods(), 300);
                string rawName = GetPhotonLocalPlayerName();
                if (string.IsNullOrEmpty(rawName)) rawName = GetSteamName();
                string playerName = Sanitise(rawName, 32);

                Thread t = new Thread(() => DoPostInitFailures(safe, extra, platform, mlVer, mods, playerName));
                t.IsBackground = true;
                t.Start();
            }
            catch { }
        }

        private static void DoPost()
        {
            try
            {
                WaitForSteamReady();

                string version    = Sanitise(BuildInfo.Version, 16);
                string rawName = GetPhotonLocalPlayerName();
                if (string.IsNullOrEmpty(rawName)) rawName = GetSteamName();
                string playerName = Sanitise(rawName, 32);
                string platform   = Sanitise(GetPlatform(), 16);
                string mlVer      = Sanitise(GetMelonLoaderVersion(), 16);
                string mods       = Sanitise(GetLoadedMods(), 350);

                string json =
                    "{\"embeds\":[{" +
                        "\"color\":16753920," +
                        "\"title\":\"Descenders Sandbox — load\"," +
                        "\"fields\":[" +
                            "{\"name\":\"Version\",\"value\":\"" + version + "\",\"inline\":true}," +
                            "{\"name\":\"Platform\",\"value\":\"" + platform + "\",\"inline\":true}," +
                            "{\"name\":\"MelonLoader\",\"value\":\"" + mlVer + "\",\"inline\":true}," +
                            "{\"name\":\"Player\", \"value\":\"" + playerName + "\",\"inline\":true}," +
                            "{\"name\":\"Loaded Mods\",\"value\":\"" + mods + "\",\"inline\":false}" +
                        "]" +
                    "}]}";

                PostJson(json);
            }
            catch { }
        }

        private static void DoPostError(string exType, string exMsg, string stack,
            string mod, string platform, string mlVer, string mods, string playerName)
        {
            try
            {
                if (string.IsNullOrEmpty(playerName) || string.Equals(playerName, "unknown", StringComparison.Ordinal))
                {
                    WaitForSteamReady();
                    string rawName = GetPhotonLocalPlayerName();
                    if (string.IsNullOrEmpty(rawName)) rawName = GetSteamName();
                    playerName = Sanitise(rawName, 32);
                }

                string json =
                    "{\"embeds\":[{" +
                        "\"color\":16711680," +
                        "\"title\":\"Descenders Sandbox — error\"," +
                        "\"fields\":[" +
                            "{\"name\":\"Exception\",\"value\":\"" + Sanitise(exType, 100) + "\",\"inline\":false}," +
                            "{\"name\":\"Message\",\"value\":\"" + exMsg + "\",\"inline\":false}," +
                            "{\"name\":\"Player\",\"value\":\"" + playerName + "\",\"inline\":true}," +
                            "{\"name\":\"Active Mod\",\"value\":\"" + mod + "\",\"inline\":true}," +
                            "{\"name\":\"Platform\",\"value\":\"" + Sanitise(platform, 16) + "\",\"inline\":true}," +
                            "{\"name\":\"MelonLoader\",\"value\":\"" + Sanitise(mlVer, 16) + "\",\"inline\":true}," +
                            "{\"name\":\"Loaded Mods\",\"value\":\"" + mods + "\",\"inline\":false}," +
                            "{\"name\":\"Stack\",\"value\":\"" + stack + "\",\"inline\":false}" +
                        "]" +
                    "}]}";

                PostJson(json);
            }
            catch { }
        }

        private static void DoPostWarning(string message, string mod,
            string platform, string mlVer, string mods, string playerName)
        {
            try
            {
                if (string.IsNullOrEmpty(playerName) || string.Equals(playerName, "unknown", StringComparison.Ordinal))
                {
                    WaitForSteamReady();
                    string rawName = GetPhotonLocalPlayerName();
                    if (string.IsNullOrEmpty(rawName)) rawName = GetSteamName();
                    playerName = Sanitise(rawName, 32);
                }

                string json =
                    "{\"embeds\":[{" +
                        "\"color\":16744448," +
                        "\"title\":\"Descenders Sandbox — warning\"," +
                        "\"fields\":[" +
                            "{\"name\":\"Message\",\"value\":\"" + message + "\",\"inline\":false}," +
                            "{\"name\":\"Player\",\"value\":\"" + playerName + "\",\"inline\":true}," +
                            "{\"name\":\"Active Mod\",\"value\":\"" + mod + "\",\"inline\":true}," +
                            "{\"name\":\"Platform\",\"value\":\"" + Sanitise(platform, 16) + "\",\"inline\":true}," +
                            "{\"name\":\"MelonLoader\",\"value\":\"" + Sanitise(mlVer, 16) + "\",\"inline\":true}," +
                            "{\"name\":\"Loaded Mods\",\"value\":\"" + mods + "\",\"inline\":false}" +
                        "]" +
                    "}]}";

                PostJson(json);
            }
            catch { }
        }

        private static void DoPostInitFailures(List<string> failures, string extra,
            string platform, string mlVer, string mods, string playerName)
        {
            try
            {
                if (string.IsNullOrEmpty(playerName) || string.Equals(playerName, "unknown", StringComparison.Ordinal))
                {
                    WaitForSteamReady();
                    string rawName = GetPhotonLocalPlayerName();
                    if (string.IsNullOrEmpty(rawName)) rawName = GetSteamName();
                    playerName = Sanitise(rawName, 32);
                }

                List<string> fields = new List<string>();
                fields.Add("{\"name\":\"Player\",\"value\":\"" + playerName + "\",\"inline\":true}");
                fields.Add("{\"name\":\"Platform\",\"value\":\"" + Sanitise(platform, 16) + "\",\"inline\":true}");
                fields.Add("{\"name\":\"MelonLoader\",\"value\":\"" + Sanitise(mlVer, 16) + "\",\"inline\":true}");
                fields.Add("{\"name\":\"Failed Count\",\"value\":\"" + Sanitise(failures.Count + extra, 20) + "\",\"inline\":true}");
                for (int i = 0; i < failures.Count; i++)
                    fields.Add("{\"name\":\"Init Failure " + (i + 1) + "\",\"value\":\"" + failures[i] + "\",\"inline\":false}");
                fields.Add("{\"name\":\"Loaded Mods\",\"value\":\"" + mods + "\",\"inline\":false}");

                string json =
                    "{\"embeds\":[{" +
                        "\"color\":16711680," +
                        "\"title\":\"Descenders Sandbox — init failures\"," +
                        "\"fields\":[" + string.Join(",", fields.ToArray()) + "]" +
                    "}]}";

                PostJson(json);
            }
            catch { }
        }

        // ── Shared webhook POST ────────────────────────────────────────
        private static bool PostJson(string json)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "curl.exe";
                psi.Arguments = "-s -S -X POST -H \"Content-Type: application/json\" --data-binary @- \"" + WebhookUrl + "\"";
                psi.UseShellExecute = false;
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                Process proc = Process.Start(psi);

                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
                using (var stdin = proc.StandardInput.BaseStream)
                {
                    stdin.Write(data, 0, data.Length);
                }

                proc.WaitForExit(15000);

                if (proc.ExitCode != 0) return false;
                string stdout = proc.StandardOutput.ReadToEnd();
                return string.IsNullOrEmpty(stdout == null ? stdout : stdout.Trim());
            }
            catch { return false; }
        }

        // ── Platform detection ─────────────────────────────────────────
        private static string GetPlatform()
        {
            if (IsWindowsAppsInstall()) return "Xbox/Game Pass";

            try
            {
                object client = GetSteamClientInstance();
                if (client != null && (object)_steamClientType != null)
                {
                    PropertyInfo isValidProp = _steamClientType.GetProperty("IsValid", BindingFlags.Public | BindingFlags.Instance);
                    if ((object)isValidProp != null)
                    {
                        object valid = isValidProp.GetValue(client, null);
                        if (valid is bool && (bool)valid) return "Steam";
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        private static bool? _isWindowsAppsInstall;
        private static bool IsWindowsAppsInstall()
        {
            if (_isWindowsAppsInstall.HasValue) return _isWindowsAppsInstall.Value;
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory ?? "";
                _isWindowsAppsInstall = basePath.IndexOf("WindowsApps", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { _isWindowsAppsInstall = false; }
            return _isWindowsAppsInstall.Value;
        }

        private static object GetSteamClientInstance()
        {
            if (_steamClientChecked) return _steamClientInstance;
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Type t = assemblies[i].GetType("Facepunch.Steamworks.Client");
                    if ((object)t == null) continue;

                    PropertyInfo instanceProp = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    if ((object)instanceProp == null) continue;

                    object instance = instanceProp.GetValue(null, null);
                    if ((object)instance == null) continue;

                    _steamClientType = t;
                    _steamClientInstance = instance;
                    _steamClientChecked = true;
                    return instance;
                }
            }
            catch { }
            return null;
        }

        private static void WaitForSteamReady()
        {
            if (IsWindowsAppsInstall()) return;

            const int maxAttempts = 15;
            const int delayMs = 1000;
            for (int i = 0; i < maxAttempts; i++)
            {
                object client = GetSteamClientInstance();
                if (client != null && (object)_steamClientType != null)
                {
                    PropertyInfo isValidProp = _steamClientType.GetProperty("IsValid", BindingFlags.Public | BindingFlags.Instance);
                    PropertyInfo usernameProp = _steamClientType.GetProperty("Username", BindingFlags.Public | BindingFlags.Instance);
                    if ((object)isValidProp != null && (object)usernameProp != null)
                    {
                        object valid = isValidProp.GetValue(client, null);
                        object nameObj = usernameProp.GetValue(client, null);
                        string name = nameObj != null ? nameObj.ToString() : null;
                        if (valid is bool && (bool)valid && !string.IsNullOrEmpty(name)) return;
                    }
                }
                Thread.Sleep(delayMs);
            }
        }

        private static string GetMelonLoaderVersion()
        {
            try { return MelonLoader.BuildInfo.Version; }
            catch { return "unknown"; }
        }

        private static string GetLoadedMods()
        {
            try
            {
                List<string> names = new List<string>();
                var registered = MelonBase.RegisteredMelons;
                if (registered != null)
                {
                    foreach (var melon in registered)
                    {
                        try
                        {
                            if ((object)melon == null) continue;
                            string n = melon.Info.Name;
                            if (!string.IsNullOrEmpty(n)) names.Add(n);
                        }
                        catch { }
                    }
                }
                return names.Count > 0 ? string.Join(", ", names.ToArray()) : "none";
            }
            catch { return "unknown"; }
        }

        private static string GetPhotonLocalPlayerName()
        {
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    if (!string.Equals(assemblies[i].GetName().Name, "Assembly-CSharp", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Type pnType = assemblies[i].GetType("upVWa\u0084E");
                    if ((object)pnType == null) continue;

                    PropertyInfo localPlayerProp = pnType.GetProperty("gQ\u0060\u0083tus", BindingFlags.Public | BindingFlags.Static);
                    if ((object)localPlayerProp == null) return null;

                    object localPlayer = localPlayerProp.GetValue(null, null);
                    if ((object)localPlayer == null) return null;

                    Type playerType = localPlayer.GetType();
                    PropertyInfo nickProp = playerType.GetProperty("DiQND\u0080L", BindingFlags.Public | BindingFlags.Instance);
                    if ((object)nickProp == null) return null;

                    object nickObj = nickProp.GetValue(localPlayer, null);
                    string nick = nickObj != null ? nickObj.ToString() : null;
                    return string.IsNullOrEmpty(nick) ? null : nick;
                }
            }
            catch { }
            return null;
        }

        private static string GetSteamName()
        {
            try
            {
                object client = GetSteamClientInstance();
                if (client != null && (object)_steamClientType != null)
                {
                    PropertyInfo usernameProp = _steamClientType.GetProperty("Username", BindingFlags.Public | BindingFlags.Instance);
                    if ((object)usernameProp != null)
                    {
                        object nameObj = usernameProp.GetValue(client, null);
                        if ((object)nameObj != null)
                        {
                            string name = nameObj.ToString();
                            if (!string.IsNullOrEmpty(name)) return name;
                        }
                    }
                }
            }
            catch { }
            return "unknown";
        }

        private static string ScrubPath(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            try
            {
                string user = Environment.UserName;
                if (!string.IsNullOrEmpty(user))
                    s = s.Replace(user, "<user>");
            }
            catch { }
            return s;
        }

        private static string Sanitise(string input, int maxLen)
        {
            if (string.IsNullOrEmpty(input)) return "unknown";
            System.Text.StringBuilder sb = new System.Text.StringBuilder(input.Length);
            for (int i = 0; i < input.Length && sb.Length < maxLen; i++)
            {
                char c = input[i];
                if (c >= 32 && c < 127 && c != '"' && c != '\\' && c != '`')
                    sb.Append(c);
            }
            return sb.Length > 0 ? sb.ToString() : "unknown";
        }
    }
}

