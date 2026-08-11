using System;
using MelonLoader;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    // Feedback = user-facing ON/OFF lines in Melon Logger.
    // Debug = silent unless Verbose.
    // Warn / Error always hit Melon Logger and Discord (when telemetry is on).
    public static class ModLog
    {
        public static bool Verbose;

        public static bool SuppressUserFeedback;

        public static void Debug(string message)
        {
            if (Verbose) MelonLogger.Msg(message);
        }

        public static void Feedback(string message)
        {
            if (!SuppressUserFeedback) MelonLogger.Msg(message);
        }

        public static void Warn(string message)
        {
            MelonLogger.Warning(message);
            Telemetry.ReportWarningAsync(message, TagFrom(message));
        }

        public static void Warn(string message, Exception ex)
        {
            string line = ex != null ? message + " " + ex.Message : message;
            MelonLogger.Warning(line);
            if (ex != null) Telemetry.ReportErrorAsync(ex, TagFrom(message));
            else Telemetry.ReportWarningAsync(line, TagFrom(message));
        }

        public static void Error(Exception ex, string activeMod)
        {
            if (ex == null) return;
            string tag = string.IsNullOrEmpty(activeMod) ? "Sandbox" : activeMod;
            MelonLogger.Error("[" + tag + "] " + ex.Message);
            Telemetry.ReportErrorAsync(ex, tag);
        }

        public static void Error(string message, Exception ex, string activeMod)
        {
            MelonLogger.Error(message);
            if (ex != null)
                Telemetry.ReportErrorAsync(ex, string.IsNullOrEmpty(activeMod) ? "Sandbox" : activeMod);
            else
                Telemetry.ReportWarningAsync(message, string.IsNullOrEmpty(activeMod) ? "Sandbox" : activeMod);
        }

        private static string TagFrom(string message)
        {
            if (string.IsNullOrEmpty(message) || message[0] != '[') return "Sandbox";
            int end = message.IndexOf(']');
            if (end <= 1) return "Sandbox";
            return message.Substring(1, end - 1);
        }
    }
}
