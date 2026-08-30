using System;
using System.Diagnostics;
using System.Threading;
using MelonLoader;

namespace DescendersModMenu.Mods
{
    public static class UpdateChecker
    {
        private const string ReleasesApiUrl =
            "https://api.github.com/repos/NateHyden/Descenders-Sandbox/releases/latest";
        public const string ReleasesPageUrl =
            "https://github.com/NateHyden/Descenders-Sandbox/releases";

        public static bool CheckComplete { get; private set; } = false;
        public static bool UpdateAvailable { get; private set; } = false;
        public static string LatestVersion { get; private set; } = "";
        public static string DownloadUrl { get; private set; } = "";
        public static string CurrentVersion => BuildInfo.Version;

        public static void CheckAsync()
        {
            try
            {
                Thread thread = new Thread(DoCheck);
                thread.IsBackground = true;
                thread.Start();
                ModLog.Debug("[UpdateChecker] Background check started.");
            }
            catch (Exception ex)
            {
                ModLog.Warn("[UpdateChecker] Failed to start: " + ex.Message);
                CheckComplete = true;
            }
        }

        private static void DoCheck()
        {
            try
            {
                string psCmd = "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; "
                    + "(Invoke-WebRequest -Uri '" + ReleasesApiUrl + "' -UseBasicParsing "
                    + "-Headers @{'User-Agent'='DescendersToolKit'}).Content";

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "-NoProfile -NonInteractive -Command \"" + psCmd + "\"";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                Process proc = Process.Start(psi);
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(10000);

                if (string.IsNullOrEmpty(output))
                {
                    ModLog.Debug("[UpdateChecker] Empty response.");
                    CheckComplete = true;
                    return;
                }

                string tag = ExtractJsonValue(output, "tag_name");
                string url = ExtractJsonValue(output, "html_url");

                if (string.IsNullOrEmpty(tag))
                {
                    ModLog.Debug("[UpdateChecker] Could not parse release tag.");
                    CheckComplete = true;
                    return;
                }

                string remoteClean = tag.TrimStart('v', 'V');
                string localClean = CurrentVersion.TrimStart('v', 'V');

                LatestVersion = remoteClean;
                DownloadUrl = url ?? "";

                if (IsNewer(remoteClean, localClean))
                {
                    UpdateAvailable = true;
                    string line = "[UpdateChecker] OUTDATED - V" + localClean
                        + " -----> V" + remoteClean + ", VISIT NEXUS OR GITHUB FOR THE LATEST VERSION";
                    // Repeat so it stands out in the Melon console.
                    MelonLogger.Msg(ConsoleColor.Red, line);
                    Thread.Sleep(200);
                    MelonLogger.Msg(ConsoleColor.Red, line);
                    Thread.Sleep(200);
                    MelonLogger.Msg(ConsoleColor.Red, line);
                }
                else
                {
                    MelonLogger.Msg(ConsoleColor.Green, "[UpdateChecker] UP TO DATE v" + localClean);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("[UpdateChecker] Check failed: " + ex.Message);
            }
            CheckComplete = true;
        }

        private static bool IsNewer(string remote, string local)
        {
            try
            {
                string[] rParts = remote.Split('.');
                string[] lParts = local.Split('.');
                int len = Math.Max(rParts.Length, lParts.Length);
                for (int i = 0; i < len; i++)
                {
                    int r = i < rParts.Length ? ParseInt(rParts[i]) : 0;
                    int l = i < lParts.Length ? ParseInt(lParts[i]) : 0;
                    if (r > l) return true;
                    if (r < l) return false;
                }
            }
            catch { }
            return false;
        }

        private static int ParseInt(string s)
        {
            int result = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c >= '0' && c <= '9')
                    result = result * 10 + (c - '0');
                else
                    break;
            }
            return result;
        }

        private static string ExtractJsonValue(string json, string key)
        {
            string search = "\"" + key + "\"";
            int keyIdx = json.IndexOf(search, StringComparison.Ordinal);
            if (keyIdx < 0) return null;

            int colonIdx = json.IndexOf(':', keyIdx + search.Length);
            if (colonIdx < 0) return null;

            int quoteStart = json.IndexOf('"', colonIdx + 1);
            if (quoteStart < 0) return null;

            int quoteEnd = json.IndexOf('"', quoteStart + 1);
            if (quoteEnd < 0) return null;

            return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
        }
    }
}

