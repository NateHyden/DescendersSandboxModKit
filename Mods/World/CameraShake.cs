using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class CameraShake
    {
        public static bool Enabled { get; private set; } = false;
        public static int Level { get; private set; } = 5;
        private const int MinLevel = 1;
        private const int MaxLevel = 10;
        private const float DefaultShake = 30f;

        // Level 5 = 30f (exact default), Level 10 = 120f (4x), Level 1 ≈ 10f (subtle)
        // shake = 30f * 2^((level-5)/2.5f)
        public static float ShakeValue => DefaultShake * Mathf.Pow(2f, (Level - 5) / 2.5f);
        public static string DisplayValue => Level.ToString();

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(Enabled ? ShakeValue : DefaultShake);
            MelonLogger.Msg("[CameraShake] " + (Enabled
                ? "ON level=" + Level + " shake=" + ShakeValue
                : "OFF restored default=" + DefaultShake));
        }

        public static void SetLevel(int v)
        {
            Level = System.Math.Max(1, System.Math.Min(10, v));
            if (Enabled) Apply(ShakeValue);
        }

        public static void Increase()
        {
            if (Level >= MaxLevel) return;
            Level++;
            if (Enabled) Apply(ShakeValue);
        }

        public static void Decrease()
        {
            if (Level <= MinLevel) return;
            Level--;
            if (Enabled) Apply(ShakeValue);
        }

        // Reset state only — no Apply() because scene objects are destroyed
        // during unload. ResetStats handles restoration via Toggle() which
        // calls Apply() when scene objects exist.
        public static void Reset()
        {
            Enabled = false;
            Level = 5;
        }

        private static System.Reflection.FieldInfo _caFld = null;

        private static void Apply(float shake)
        {
            try
            {
                BikeCamera[] cameras = Object.FindObjectsOfType<BikeCamera>();
                if (cameras == null || cameras.Length == 0)
                { MelonLogger.Warning("[CameraShake] No BikeCamera found."); return; }

                // Resolve CameraAngle field once via reflection — field name contains
                // control chars that C# source cannot express as an identifier.
                // Scan for the public CameraAngle field on BikeCamera.
                if ((object)_caFld == null)
                {
                    var fields = typeof(BikeCamera).GetFields(
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    for (int f = 0; f < fields.Length; f++)
                    {
                        if (string.Equals(fields[f].FieldType.Name, "CameraAngle",
                            System.StringComparison.Ordinal))
                        { _caFld = fields[f]; break; }
                    }
                }
                if ((object)_caFld == null)
                { MelonLogger.Warning("[CameraShake] CameraAngle field not found."); return; }

                int count = 0;
                for (int i = 0; i < cameras.Length; i++)
                {
                    CameraAngle ca = _caFld.GetValue(cameras[i]) as CameraAngle;
                    if ((object)ca == null) continue;
                    ca.cameraShake = shake;
                    ca.impactCameraShake = shake;
                    count++;
                }
                MelonLogger.Msg("[CameraShake] Applied shake=" + shake + " to " + count + " CameraAngle(s).");
            }
            catch (System.Exception ex) { MelonLogger.Error("[CameraShake] Apply: " + ex.Message); }
        }
    }
}