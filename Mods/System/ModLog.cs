using System;
using MelonLoader;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    // Startup/init chatter goes through Debug (off by default).
    // Important lines use MelonLogger directly: start, update check, sandbox loaded, errors.
    public static class ModLog
    {
        /// <summary>Set true to restore the old verbose MelonLoader console.</summary>
        public static bool Verbose;

        /// <summary>True while AutoLoad/reapply applies saved toggles — hides "X -> ON" spam.</summary>
        public static bool SuppressUserFeedback;

        public static void Debug(string message)
        {
            if (Verbose) MelonLogger.Msg(message);
        }

        public static void Feedback(string message)
        {
            if (!SuppressUserFeedback) MelonLogger.Msg(message);
        }

        /// <summary>Log an error and (if telemetry is on) report it to Discord.</summary>
        public static void Error(Exception ex, string activeMod)
        {
            if (ex == null) return;
            string tag = string.IsNullOrEmpty(activeMod) ? "Sandbox" : activeMod;
            MelonLogger.Error("[" + tag + "] " + ex.Message);
            Telemetry.ReportErrorAsync(ex, tag);
        }

        /// <summary>Log a custom error line and report the exception to Discord when telemetry is on.</summary>
        public static void Error(string message, Exception ex, string activeMod)
        {
            MelonLogger.Error(message);
            if (ex != null)
                Telemetry.ReportErrorAsync(ex, string.IsNullOrEmpty(activeMod) ? "Sandbox" : activeMod);
        }
    }
}
