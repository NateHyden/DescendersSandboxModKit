using System;
using System.Diagnostics;
using System.Threading;
using MelonLoader;

namespace DescendersModMenu.Mods
{
    // Fetches the current Descenders player count from the Steam API.
    // No API key required. Runs on a background thread, same pattern as UpdateChecker.
    public static class SteamPlayerCount
    {
        private const string Url =
            "https://api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers/v1/?appid=681280";

        public static bool   FetchComplete  { get; private set; } = false;
        public static bool   FetchFailed    { get; private set; } = false;
        public static int    PlayerCount    { get; private set; } = 0;
        public static string DisplayValue   { get; private set; } = "...";

        public static void FetchAsync()
        {
            try
            {
                Thread t = new Thread(DoFetch);
                t.IsBackground = true;
                t.Start();
                MelonLogger.Msg("[SteamPlayerCount] Fetch started.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[SteamPlayerCount] Failed to start thread: " + ex.Message);
                DisplayValue  = "unavailable";
                FetchFailed   = true;
                FetchComplete = true;
            }
        }

        private static void DoFetch()
        {
            try
            {
                string psCmd = "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; "
                    + "(Invoke-WebRequest -Uri '" + Url + "' -UseBasicParsing).Content";

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName               = "powershell.exe";
                psi.Arguments              = "-NoProfile -NonInteractive -Command \"" + psCmd + "\"";
                psi.UseShellExecute        = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError  = true;
                psi.CreateNoWindow         = true;

                Process proc   = Process.Start(psi);
                string  output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(10000);

                if (string.IsNullOrEmpty(output))
                {
                    MelonLogger.Warning("[SteamPlayerCount] Empty response.");
                    DisplayValue  = "unavailable";
                    FetchFailed   = true;
                    FetchComplete = true;
                    return;
                }

                // Response: {"response":{"player_count":1234,"result":1}}
                string raw = ExtractJsonInt(output, "player_count");
                int count;
                if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, out count))
                {
                    PlayerCount   = count;
                    DisplayValue  = count.ToString("N0"); // e.g. "1,234"
                    MelonLogger.Msg("[SteamPlayerCount] " + DisplayValue + " players online.");
                }
                else
                {
                    MelonLogger.Warning("[SteamPlayerCount] Could not parse player_count. Raw: " + output);
                    DisplayValue = "unavailable";
                    FetchFailed  = true;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[SteamPlayerCount] Fetch failed: " + ex.Message);
                DisplayValue = "unavailable";
                FetchFailed  = true;
            }
            FetchComplete = true;
        }

        // Extracts the value of an integer JSON field by key name
        private static string ExtractJsonInt(string json, string key)
        {
            string search = "\"" + key + "\"";
            int keyIdx = json.IndexOf(search, StringComparison.Ordinal);
            if (keyIdx < 0) return null;
            int colonIdx = json.IndexOf(':', keyIdx + search.Length);
            if (colonIdx < 0) return null;
            // Skip whitespace after colon
            int start = colonIdx + 1;
            while (start < json.Length && (json[start] == ' ' || json[start] == '\t')) start++;
            // Read digits
            int end = start;
            while (end < json.Length && json[end] >= '0' && json[end] <= '9') end++;
            if (end == start) return null;
            return json.Substring(start, end - start);
        }
    }
}
