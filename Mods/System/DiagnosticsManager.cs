using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    public class ModStatus
    {
        public string Name;
        public bool OK;
        public string Error;

        public ModStatus(string name, bool ok, string error = "")
        {
            Name = name;
            OK = ok;
            Error = error;
        }
    }

    public static class DiagnosticsManager
    {
        private static readonly List<ModStatus> _statuses = new List<ModStatus>();
        private const string BuiltForUnity = "2017.4.40f1";
        private static string _logPath;

        public static List<ModStatus> Statuses { get { return _statuses; } }

        public static void Report(string name, bool ok, string error = "")
        {
            _statuses.Add(new ModStatus(name, ok, error));
            // Successes are batched into LogStartupSummary — only failures
            // hit the MelonLoader console so startup isn't a wall of OK lines.
            if (!ok)
            {
                MelonLogger.Warning("[Diagnostics] " + name + ": FAILED - " + error);
                WriteToFile(name, error, "");
            }
        }

        public static void LogStartupSummary()
        {
            // Quiet success path — only surface startup failures in MelonLoader.
            if (FailCount > 0)
            {
                MelonLogger.Warning("[Diagnostics] Startup: " + FailCount + " failed, "
                    + OKCount + " OK  |  Unity " + UnityVersion
                    + (UnityVersionMatch ? "" : " (built for " + BuiltForUnity + ")"));
                for (int i = 0; i < _statuses.Count; i++)
                {
                    if (!_statuses[i].OK)
                        MelonLogger.Warning("[Diagnostics]   " + _statuses[i].Name
                            + ": " + _statuses[i].Error);
                }
            }
            else
            {
                ModLog.Debug("[Diagnostics] Startup: " + OKCount + " OK, 0 failed  |  Unity "
                    + UnityVersion);
            }
        }

        public static void LogError(string modName, Exception ex)
        {
            string msg = (object)ex != null ? ex.Message : "Unknown error";
            string stack = (object)ex != null ? ex.StackTrace : "";
            MelonLogger.Error("[" + modName + "] " + msg);
            WriteToFile(modName, msg, stack);
            if ((object)ex != null)
                Telemetry.ReportErrorAsync(ex, modName);
        }

        public static void LogError(string modName, string error)
        {
            MelonLogger.Error("[" + modName + "] " + error);
            WriteToFile(modName, error, "");
            Telemetry.ReportErrorAsync(new Exception(error), modName);
        }

        private static void WriteToFile(string modName, string error, string stackTrace)
        {
            try
            {
                if ((object)_logPath == null)
                {
                    string dir = Path.Combine("UserData", "DescendersModMenu");
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    _logPath = Path.Combine(dir, "ErrorLog.txt");
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string entry = "[" + timestamp + "] " + modName + " FAILED\n"
                    + "  " + error + "\n";
                if (!string.IsNullOrEmpty(stackTrace))
                    entry += "  " + stackTrace.Replace("\n", "\n  ") + "\n";
                entry += "\n";

                File.AppendAllText(_logPath, entry);
            }
            catch { }
        }

        public static string UnityVersion
        {
            get
            {
                try { return Application.unityVersion; }
                catch { return "Unknown"; }
            }
        }

        public static string MelonLoaderVersion
        {
            get
            {
                try { return MelonLoader.BuildInfo.Version; }
                catch { return "Unknown"; }
            }
        }

        public static string BuiltForVersion { get { return BuiltForUnity; } }

        public static bool UnityVersionMatch
        {
            get { return string.Equals(UnityVersion, BuiltForUnity, StringComparison.Ordinal); }
        }

        public static int OKCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _statuses.Count; i++)
                    if (_statuses[i].OK) n++;
                return n;
            }
        }

        public static int FailCount
        {
            get { return _statuses.Count - OKCount; }
        }
    }
}