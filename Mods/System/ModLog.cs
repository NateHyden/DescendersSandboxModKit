using MelonLoader;

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
    }
}
