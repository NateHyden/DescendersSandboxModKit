using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using MelonLoader;

namespace DescendersModMenu
{
    // Fires a one-shot Discord webhook on mod load, and (opt-in) posts
    // unhandled-exception reports, so Nate can see installs and diagnose
    // issues without needing the user's Latest.log.
    //
    // Gated behind MelonPreferences "EnableTelemetry" (default: false —
    // opt-in, never forced on for a fresh install).
    // The UI toggle lives in the menu header — see MenuWindow.cs.
    //
    // Silent by design — no MelonLogger output at all, success or
    // failure. If this needs debugging again, logging can be re-added
    // temporarily; it's deliberately absent from the shipped behaviour.
    internal static class Telemetry
    {
        // Not hardcoded — the repo is public. Set the real URL once in
        // UserData/MelonPreferences.cfg under [DescendersSandbox_Telemetry]
        // WebhookUrl = "..."  (created automatically on first run, empty).
        private static bool _hasPinged;

        // ── Steam client reflection cache ──────────────────────────────
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

        // Header hint ("Please read telemetry page in Info/Customize") —
        // dismissible, saved so it stays gone once the player's read it.
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

            // Just turned ON mid-session — fire a ping now, EVERY time,
            // not just once per session. This deliberately bypasses
            // PingAsync()'s _hasPinged guard (that guard exists only to
            // stop the automatic launch-time ping firing twice — it was
            // wrongly also blocking this manual path, since the automatic
            // ping normally fires once telemetry's already on). This is
            // the main way someone reports a bug in practice: they hit
            // something wrong, flip the switch, and this snapshot is what
            // reaches Discord — should work no matter how many times they
            // toggle it during a session.
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

        // ── Entry point — error/crash report ──────────────────────────
        // De-duped per (mod, exception type) per session — a bug that
        // throws every frame only pings once, not hundreds of times.
        //
        // No lock here (deliberately, not an oversight): every call site
        // is a Harmony Postfix/Prefix on Update/FixedUpdate/LateUpdate/
        // OnGUI, which Unity only ever calls from the main thread — so
        // this genuinely never runs concurrently. A `lock` statement was
        // here originally, compiled down to Monitor.Enter(object, ref
        // bool), which doesn't exist in this Mono build's stripped
        // mscorlib — same class of bug as the Type.op_Inequality issue
        // earlier tonight. That one `lock` silently broke ALL 23 wiring
        // sites from the telemetry sweep, not just this one, since they
        // all funnel through this shared method.
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

        // ── Entry point — user-submitted feedback / bug report / feature request ──
        // Deliberately NOT gated by Enabled (the passive-telemetry toggle) —
        // this is an active choice the user made by typing a message and
        // hitting Send, not passive diagnostic collection. Still needs a
        // webhook configured, and has its own light cooldown so mashing the
        // button can't spam the channel. Environment.TickCount (not
        // UnityEngine.Time) — this file has no UnityEngine dependency
        // anywhere else and there's no reason to add one just for a timer.
        //
        // _hasSentFeedback, NOT a sentinel tick value: the previous version
        // used int.MinValue to mean "never sent", but TickCount - int.MinValue
        // overflows a 32-bit int and wraps to a NEGATIVE number — which
        // always failed the >= cooldown check. That silently broke the
        // very first Send click every session, permanently (nothing after
        // it can succeed either, since the broken tick value never gets
        // replaced). A plain bool sidesteps the overflow entirely.
        private static bool _hasSentFeedback = false;
        private static int _lastFeedbackSendTick;
        private const int FeedbackCooldownMs = 15000;

        // Polled by InfoPage.Refresh() so the UI can show real delivery
        // status instead of an optimistic "Sent!" the instant Send is
        // clicked — the actual POST happens on a background thread, so
        // this is the hand-off point back to the main thread.
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

                // Colour-code by category: red=bug, green=feature, purple=general
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

        // ── Entry point — batched init-failure report ─────────────────
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

        // ── Background thread — load ping ─────────────────────────────
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

        // ── Background thread — error report ──────────────────────────
        private static void DoPostError(string exType, string exMsg, string stack,
            string mod, string platform, string mlVer, string mods, string playerName)
        {
            try
            {
                // Refresh name on the worker if the caller only had "unknown"
                // (e.g. error fired before Photon/Steam nick was ready).
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

        // ── Background thread — batched init-failure report ────────────
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
        // curl.exe (external process), not in-process HttpWebRequest —
        // direct networking from inside the Mono-hosted game process gets
        // refused; spawning curl reaches Discord fine via Windows' own
        // Schannel TLS stack. JSON body is piped via stdin, never on the
        // command line, so there's nothing to escape.
        //
        // Returns success/failure — not logged anywhere (still silent by
        // design for the passive paths: ping/error/init-failure callers
        // just discard it), but the feedback-send path needs a REAL
        // result to show the user, not just an optimistic "Sent!" the
        // instant they click — so this checks curl's exit code and
        // whether Discord returned an error body.
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
                // Discord returns an empty body (204) on success; anything
                // else is Discord's own error JSON.
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

        // ── Shared Steam client lookup ───────────────────────────────────
        // Only caches on SUCCESS — a failed attempt must be retryable.
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

        // ── Bounded retry — short safety margin, not the primary wait ──
        // Fired once Player_Human exists (see ModEntry.cs), so Steam
        // should already be ready in the common case.
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

        // ── Get the local player's display name via Photon ───────────────
        // Photon's NetworkingPeer wrapper (obfuscated name "upVWa\u0084E",
        // confirmed by its Photon SDK version constant "1.94" and by
        // PlayerInfoImpact.cs already calling SetCustomProperties through
        // the exact same static "gQ\u0060\u0083tus" property, which Photon
        // only permits on the local player) exposes a static LocalPlayer
        // equivalent, whose Player object (obfuscated "Mn\u0081\u0084vL\u007F")
        // has a NickName-equivalent property (obfuscated "DiQND\u0080L").
        // Works identically on Steam and Xbox, unlike the Steam-only lookup.
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

        // ── Get the local player's Steam name via Facepunch.Steamworks ──
        // No Windows-username fallback — reports "unknown" instead of
        // leaking the PC account name (e.g. on Xbox/Game Pass).
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

        // ── Strip local username from paths/stack traces before posting ──
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

        // ── Strip characters that would break JSON or the PS command ─────
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
