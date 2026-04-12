using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class SlowMotion
    {
        public static bool Enabled { get; private set; } = false;

        // Level 1-9: maps to 0.1x-0.9x timescale. Never >= 1.0 (that would be normal or fast).
        // Default level 5 = 0.5x (original behaviour preserved)
        public static int Level { get; private set; } = 5;
        public static string DisplayValue { get { return (Level * 0.1f).ToString("F1") + "x"; } }

        private static float SlowScale { get { return Level * 0.1f; } }

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
            MelonLogger.Msg("SlowMotion -> " + (Enabled ? "ON (" + DisplayValue + ")" : "OFF"));
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
                // The game has a TimeScaleManager that lerps Time.timeScale to its
                // own target every frame — setting Time.timeScale directly gets
                // overwritten instantly. We must set it via SetTimeScale instead.
                var mgr = Object.FindObjectOfType<TimeScaleManager>();
                if ((object)mgr != null)
                {
                    mgr.SetTimeScale(scale, true);
                }
                else
                {
                    // Fallback if manager not found yet
                    Time.timeScale = scale;
                    Time.fixedDeltaTime = 0.02f * scale;
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("SlowMotion.SetScale: " + ex.Message); }

        }
    }
}