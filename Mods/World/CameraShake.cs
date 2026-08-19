using System.Reflection;
using MelonLoader;
using DescendersModMenu;
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

        public static float ShakeValue => DefaultShake * Mathf.Pow(2f, (Level - 5) / 2.5f);
        public static string DisplayValue => Level.ToString();

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(Enabled ? ShakeValue : DefaultShake);
            ModLog.Debug("[CameraShake] " + (Enabled
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
                { ModLog.Warn("[CameraShake] No BikeCamera found."); return; }

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
                { ModLog.Warn("[CameraShake] CameraAngle field not found."); return; }

                int count = 0;
                for (int i = 0; i < cameras.Length; i++)
                {
                    CameraAngle ca = _caFld.GetValue(cameras[i]) as CameraAngle;
                    if ((object)ca == null) continue;
                    ca.cameraShake = shake;
                    ca.impactCameraShake = shake;
                    count++;
                }
                ModLog.Debug("[CameraShake] Applied shake=" + shake + " to " + count + " CameraAngle(s).");
            }
            catch (System.Exception ex) { MelonLogger.Error("[CameraShake] Apply: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "CameraShake"); }
        }
    }
}

