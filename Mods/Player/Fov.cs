using MelonLoader;
using DescendersModMenu;
using System;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class FOV
    {
        public static bool Enabled { get; private set; } = false;
        public static int Level { get; private set; } = 5;

        private static float GetFOV() { return 45f + (Level - 1) * 9.4f; }
        public static string DisplayValue { get { return ((int)GetFOV()).ToString(); } }

        private static BikeCamera[] _cameras = null;
        private static FieldInfo _caField = null;
        private static bool _fieldScan = false;

        private static float[] _defaults = null;

        // ── Public API ────────────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Apply();
            else Restore();
            ModLog.Feedback("[FOV] -> " + (Enabled ? "ON (" + DisplayValue + ")" : "OFF"));
        }

        public static void Increase() { if (Level < 10) Level++; if (Enabled) Apply(); }
        public static void Decrease() { if (Level > 1) Level--; if (Enabled) Apply(); }

        public static void SetLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > 10) level = 10;
            Level = level;
            if (Enabled) Apply();
        }

        public static void Apply()
        {
            if (!Enabled) return;
            try
            {
                EnsureCameras();
                if ((object)_cameras == null || (object)_caField == null) return;

                float target = GetFOV();
                for (int i = 0; i < _cameras.Length; i++)
                {
                    if (!UnityNull.Alive(_cameras[i])) continue;
                    CameraAngle ca = _caField.GetValue(_cameras[i]) as CameraAngle;
                    if (!UnityNull.Alive(ca)) continue;

                    if (_defaults[i] < 0f)
                    {
                        _defaults[i] = ca.targetFOV;
                        ModLog.Debug("[FOV] Captured default for camera " + i
                            + ": " + _defaults[i]);
                    }

                    ca.targetFOV = target;
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[FOV] Apply: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Fov"); }
        }

        private static void Restore()
        {
            try
            {
                EnsureCameras();
                if ((object)_cameras == null || (object)_caField == null) return;
                for (int i = 0; i < _cameras.Length; i++)
                {
                    if (!UnityNull.Alive(_cameras[i])) continue;
                    CameraAngle ca = _caField.GetValue(_cameras[i]) as CameraAngle;
                    if (!UnityNull.Alive(ca)) continue;
                    ca.targetFOV = (_defaults != null && _defaults[i] > 0f)
                        ? _defaults[i]
                        : 85f;
                }
                ModLog.Debug("[FOV] Restored default FOV.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[FOV] Restore: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Fov"); }
        }

        public static void ClearCache()
        {
            _cameras = null;
            _caField = null;
            _fieldScan = false;
            _defaults = null;
        }

        public static void Reset()
        {
            if (Enabled) Restore();
            Enabled = false;
            ClearCache();
        }

        // ── Internals ─────────────────────────────────────────────────
        private static void EnsureCameras()
        {
            bool rebuild = (object)_cameras == null || _cameras.Length == 0;
            if (!rebuild)
            {
                for (int i = 0; i < _cameras.Length; i++)
                {
                    if (!UnityNull.Alive(_cameras[i]))
                    {
                        rebuild = true;
                        break;
                    }
                }
            }

            if (rebuild)
            {
                _cameras = UnityEngine.Object.FindObjectsOfType<BikeCamera>();
                _defaults = new float[_cameras.Length];
                for (int i = 0; i < _defaults.Length; i++) _defaults[i] = -1f;
                _fieldScan = false;
            }

            if (!_fieldScan && _cameras.Length > 0)
            {
                BikeCamera probe = null;
                for (int i = 0; i < _cameras.Length; i++)
                {
                    if (UnityNull.Alive(_cameras[i]))
                    {
                        probe = _cameras[i];
                        break;
                    }
                }
                if (!UnityNull.Alive(probe)) return;
                _fieldScan = true;
                FieldInfo[] fields = probe.GetType().GetFields(
                    BindingFlags.Public | BindingFlags.Instance);
                for (int j = 0; j < fields.Length; j++)
                {
                    if (!string.Equals(fields[j].FieldType.Name, "CameraAngle",
                        StringComparison.Ordinal)) continue;
                    _caField = fields[j];
                    ModLog.Debug("[FOV] Found CameraAngle field: " + fields[j].Name);
                    break;
                }
                if ((object)_caField == null)
                    ModLog.Warn("[FOV] CameraAngle field not found on BikeCamera.");
            }
        }
    }
}

