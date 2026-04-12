using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class Gravity
    {
        // Level 1 = very light (-2), Level 5 = default (-17.5), Level 10 = heavy (-40)
        private static readonly float[] Levels = {
            -2f, -5f, -8f, -12f, -17.5f, -22f, -27f, -31f, -35f, -40f
        };

        public static int Level { get; private set; } = 5;

        public static string DisplayValue { get { return Levels[Level - 1].ToString("F1"); } }

        public static void Increase() { if (Level < 10) { Level++; Apply(); } }
        public static void Decrease() { if (Level > 1) { Level--; Apply(); } }
        public static void SetLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > 10) level = 10;
            Level = level;
            Apply();
        }

        public static void Apply()
        {
            try
            {
                Physics.gravity = new Vector3(0f, Levels[Level - 1], 0f);
                MelonLogger.Msg("[Gravity] Set to " + Levels[Level - 1]);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Gravity] Apply: " + ex.Message); }
        }

        public static void Reset()
        {
            Level = 5;
            Apply();
        }
    }
}