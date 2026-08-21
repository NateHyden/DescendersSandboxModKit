using System.Collections.Generic;
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
        public const int MaxLevel = 20;
        private const float StockShake = 30f;

        // Level 1 ≈ gentle boost, Level 20 = very strong. Stock (off) is 30.
        public static float ShakeValue
        {
            get { return StockShake * (1f + Level * 0.75f); }
        }

        public static string DisplayValue
        {
            get { return Mathf.RoundToInt((Level - 1) / (float)(MaxLevel - 1) * 100f) + "%"; }
        }

        private struct ShakeDefaults
        {
            public float CameraShake;
            public float ImpactShake;
        }

        private static readonly Dictionary<int, ShakeDefaults> _defaults =
            new Dictionary<int, ShakeDefaults>();

        private static float _nextApply;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Apply(true);
            else Restore();
            ModLog.Feedback("[CameraShake] -> " + (Enabled
                ? "ON " + DisplayValue + " (shake=" + ShakeValue.ToString("F0") + ")"
                : "OFF"));
        }

        public static void SetLevel(int v)
        {
            Level = System.Math.Max(MinLevel, System.Math.Min(MaxLevel, v));
            if (Enabled) Apply(false);
        }

        public static void Increase()
        {
            if (Level >= MaxLevel) return;
            Level++;
            if (Enabled) Apply(false);
        }

        public static void Decrease()
        {
            if (Level <= MinLevel) return;
            Level--;
            if (Enabled) Apply(false);
        }

        public static void Reset()
        {
            if (Enabled) Restore();
            Enabled = false;
            Level = 5;
            _defaults.Clear();
            _nextApply = 0f;
        }

        /// <summary>Keep values stuck after camera / angle swaps.</summary>
        public static void Tick()
        {
            if (!Enabled) return;
            if (Time.unscaledTime < _nextApply) return;
            _nextApply = Time.unscaledTime + 0.5f;
            Apply(false);
        }

        private static void Apply(bool captureMissing)
        {
            try
            {
                CameraAngle[] angles = FindAngles();
                if (angles == null || angles.Length == 0)
                {
                    ModLog.Warn("[CameraShake] No CameraAngle found.");
                    return;
                }

                float shake = ShakeValue;
                int count = 0;
                for (int i = 0; i < angles.Length; i++)
                {
                    CameraAngle ca = angles[i];
                    if (!UnityNull.Alive(ca)) continue;

                    int id = ca.GetInstanceID();
                    if (captureMissing && !_defaults.ContainsKey(id))
                    {
                        _defaults[id] = new ShakeDefaults
                        {
                            CameraShake = ca.cameraShake,
                            ImpactShake = ca.impactCameraShake
                        };
                    }

                    ca.cameraShake = shake;
                    ca.impactCameraShake = shake;
                    count++;
                }

                // Also poke the live BikeCamera current angle (in case Find missed it).
                ApplyLiveBikeCameras(shake);

                if (captureMissing)
                    ModLog.Debug("[CameraShake] Applied shake=" + shake + " to " + count + " angle(s).");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[CameraShake] Apply: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CameraShake");
            }
        }

        private static void Restore()
        {
            try
            {
                CameraAngle[] angles = FindAngles();
                for (int i = 0; i < angles.Length; i++)
                {
                    CameraAngle ca = angles[i];
                    if (!UnityNull.Alive(ca)) continue;
                    int id = ca.GetInstanceID();
                    ShakeDefaults d;
                    if (_defaults.TryGetValue(id, out d))
                    {
                        ca.cameraShake = d.CameraShake;
                        ca.impactCameraShake = d.ImpactShake;
                    }
                    else
                    {
                        ca.cameraShake = StockShake;
                        ca.impactCameraShake = StockShake;
                    }
                }
                ApplyLiveBikeCameras(StockShake);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[CameraShake] Restore: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CameraShake");
            }
        }

        private static CameraAngle[] FindAngles()
        {
            // ScriptableObjects: FindObjectsOfTypeAll catches loaded angle assets.
            CameraAngle[] all = Resources.FindObjectsOfTypeAll<CameraAngle>();
            if ((object)all != null && all.Length > 0) return all;
            all = Object.FindObjectsOfType<CameraAngle>();
            return all != null ? all : new CameraAngle[0];
        }

        private static FieldInfo _caFld;

        private static void ApplyLiveBikeCameras(float shake)
        {
            BikeCamera[] cameras = Object.FindObjectsOfType<BikeCamera>();
            if (cameras == null || cameras.Length == 0) return;

            if ((object)_caFld == null)
            {
                FieldInfo[] fields = typeof(BikeCamera).GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int f = 0; f < fields.Length; f++)
                {
                    if (string.Equals(fields[f].FieldType.Name, "CameraAngle",
                        System.StringComparison.Ordinal))
                    {
                        _caFld = fields[f];
                        break;
                    }
                }
            }
            if ((object)_caFld == null) return;

            for (int i = 0; i < cameras.Length; i++)
            {
                if (!UnityNull.Alive(cameras[i])) continue;
                CameraAngle ca = _caFld.GetValue(cameras[i]) as CameraAngle;
                if (!UnityNull.Alive(ca)) continue;
                ca.cameraShake = shake;
                ca.impactCameraShake = shake;
            }
        }
    }
}
