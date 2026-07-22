using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using MelonLoader;

namespace DescendersModMenu
{
    // Fires a one-shot Discord webhook on mod load so you can see global installs.
    // Replace WEBHOOK_URL with your Discord channel webhook URL.
    // Disclosed in mod description — no opt-in toggle needed for anonymous ping.
    internal static class Telemetry
    {
        private const string WebhookUrl = "YOUR_WEBHOOK_URL_HERE";

        private static bool _hasPinged;

        // ── Entry point ─────────────────────────────────────────────────
        public static void PingAsync()
        {
            if (_hasPinged) return;
            if (string.Equals(WebhookUrl, "YOUR_WEBHOOK_URL_HERE", StringComparison.Ordinal))
            {
                MelonLogger.Warning("[Telemetry] Webhook URL not set — skipping ping.");
                return;
            }
            _hasPinged = true;

            try
            {
                Thread t = new Thread(DoPost);
                t.IsBackground = true;
                t.Start();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Telemetry] Failed to start thread: " + ex.Message);
            }
        }

        // ── Background thread ────────────────────────────────────────────
        private static void DoPost()
        {
            try
            {
                string version    = BuildInfo.Version;
                string playerName = GetSteamName();

                // Sanitise for JSON and PowerShell single-quoted strings
                playerName = Sanitise(playerName, 32);
                version    = Sanitise(version,    16);

                // Build Discord embed JSON (no external JSON lib needed)
                string json =
                    "{\"embeds\":[{" +
                        "\"color\":16753920," +
                        "\"title\":\"Descenders Sandbox — load\"," +
                        "\"fields\":[" +
                            "{\"name\":\"Version\",\"value\":\"" + version    + "\",\"inline\":true}," +
                            "{\"name\":\"Player\", \"value\":\"" + playerName + "\",\"inline\":true}" +
                        "]" +
                    "}]}";

                // Escape single quotes for PowerShell single-quoted string ('' = literal ')
                string safeJson = json.Replace("'", "''");

                string psCmd =
                    "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; " +
                    "Invoke-WebRequest -Uri '" + WebhookUrl + "' " +
                        "-Method Post " +
                        "-ContentType 'application/json' " +
                        "-Body '" + safeJson + "' " +
                        "-UseBasicParsing | Out-Null";

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName               = "powershell.exe";
                psi.Arguments              = "-NoProfile -NonInteractive -Command \"" + psCmd + "\"";
                psi.UseShellExecute        = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError  = true;
                psi.CreateNoWindow         = true;

                Process proc = Process.Start(psi);
                proc.WaitForExit(15000);

                string err = proc.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(err))
                    MelonLogger.Warning("[Telemetry] Post error: " + err.Trim());
                else
                    MelonLogger.Msg("[Telemetry] Ping sent — v" + version + " | " + playerName);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Telemetry] DoPost failed: " + ex.Message);
            }
        }

        // ── Try to get the Steam persona name via Steamworks reflection ──
        // Falls back to Windows username if Steamworks is not loaded.
        private static string GetSteamName()
        {
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Type sf = assemblies[i].GetType("Steamworks.SteamFriends");
                    if ((object)sf == null) continue;

                    MethodInfo m = sf.GetMethod(
                        "GetPersonaName",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        Type.EmptyTypes,
                        null);

                    if ((object)m == null) continue;

                    object result = m.Invoke(null, null);
                    if ((object)result != null)
                    {
                        string name = result.ToString();
                        if (!string.IsNullOrEmpty(name))
                        {
                            MelonLogger.Msg("[Telemetry] Steam name: " + name);
                            return name;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Telemetry] Steam name lookup failed: " + ex.Message);
            }

            // Fallback — Windows username. Not ideal but better than nothing.
            try { return Environment.UserName; }
            catch { return "unknown"; }
        }

        // ── Strip characters that would break JSON or the PS command ─────
        private static string Sanitise(string input, int maxLen)
        {
            if (string.IsNullOrEmpty(input)) return "unknown";
            System.Text.StringBuilder sb = new System.Text.StringBuilder(input.Length);
            for (int i = 0; i < input.Length && sb.Length < maxLen; i++)
            {
                char c = input[i];
                // Allow printable ASCII except chars that could break JSON/PS
                if (c >= 32 && c < 127 && c != '"' && c != '\\' && c != '`')
                    sb.Append(c);
            }
            return sb.Length > 0 ? sb.ToString() : "unknown";
        }
    }
}
