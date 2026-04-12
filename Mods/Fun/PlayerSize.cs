using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class PlayerSize
    {
        public static float CurrentScale = 1f;
        public static int Level = 10;

        private static readonly float[] Scales = {
            0.10f, 0.15f, 0.20f, 0.30f, 0.40f, 0.55f, 0.70f, 0.85f, 0.92f, 1.00f,
            1.20f, 1.50f, 1.80f, 2.20f, 2.60f, 3.00f, 3.50f, 4.00f, 5.00f, 6.00f
        };

        private static Vector3 _defaultScale = Vector3.one;
        private static bool _captured = false;

        public static bool IsModified => Level != 10;

        public static void CaptureDefaults()
        {
            _captured = false;
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) return;
                Transform cy = player.transform.Find("Cyclist");
                if ((object)cy != null) { _defaultScale = cy.localScale; _captured = true; }
            }
            catch { }
        }

        public static void ApplyLevel(int level)
        {
            Level = Mathf.Clamp(level, 1, 20);
            if (Level == 10) ResetToDefault();
            else Apply(Scales[Level - 1]);
        }

        public static void Apply(float scale)
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) return;
                Transform cyclist = player.transform.Find("Cyclist");
                if ((object)cyclist == null) return;
                if (!_captured) { _defaultScale = cyclist.localScale; _captured = true; }
                cyclist.localScale = new Vector3(scale, scale, scale);
                CurrentScale = scale;
            }
            catch (System.Exception ex) { MelonLogger.Error("[PlayerSize] Apply: " + ex.Message); }
        }

        public static void ResetToDefault()
        {
            try
            {
                CurrentScale = 1f;
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) return;
                Transform cyclist = player.transform.Find("Cyclist");
                if ((object)cyclist != null) cyclist.localScale = _defaultScale;
            }
            catch (System.Exception ex) { MelonLogger.Error("[PlayerSize] ResetToDefault: " + ex.Message); }
        }
    }
}
