using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class SlowMotion
    {
        public static bool Enabled { get; private set; } = false;

        public static int Level { get; private set; } = 5;
        public static string DisplayValue { get { return (Level * 0.1f).ToString("F1") + "x"; } }

        private static float SlowScale { get { return Level * 0.1f; } }

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
            ModLog.Feedback("SlowMotion -> " + (Enabled ? "ON (" + DisplayValue + ")" : "OFF"));
        }

        public static void Increase()
        {
            if (Level >= 9) return;
            Level++;
            if (Enabled) Apply();
        }

        public static void Decrease()
        {
            if (Level <= 1) return;
            Level--;
            if (Enabled) Apply();
        }

        public static void SetLevel(int level)
        {
            Level = UnityEngine.Mathf.Clamp(level, 1, 9);
            if (Enabled) Apply();
        }

        public static void Apply()
        {
            SetScale(Enabled ? SlowScale : 1f);
        }

        public static void Reset()
        {
            Enabled = false;
            SetScale(1f);
        }

        private static void SetScale(float scale)
        {

            try
            {
                var mgr = Object.FindObjectOfType<TimeScaleManager>();
                if ((object)mgr != null)
                {
                    mgr.SetTimeScale(scale, true);
                }
                else
                {
                    Time.timeScale = scale;
                    Time.fixedDeltaTime = 0.02f * scale;
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("SlowMotion.SetScale: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "SlowMotion"); }

        }
    }
}

