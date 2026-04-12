using MelonLoader;
using System;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class FOV
    {
        public static bool Enabled { get; private set; } = false;
        public static int Level { get; private set; } = 5;

        // Level 1=45, Level 5=85 (game default), Level 10=130
        private static float GetFOV() { return 45f + (Level - 1) * 9.4f; }
        public static string DisplayValue { get { return ((int)GetFOV()).ToString(); } }

        // Only cache BikeCamera[] and FieldInfo — both persist across camera switches.
        // CameraAngle instances are NOT cached: CameraManager.SetCameraAngle() calls
        // Object.Instantiate<CameraAngle>() on every mode switch, invalidating any
        // cached reference. We fetch the live instance fresh every Apply() call.
        private static BikeCamera[] _cameras = null;
        private static FieldInfo _caField = null;   // BikeCamera field of type CameraAngle
        private static bool _fieldScan = false;

        // Per-camera default targetFOV captured before first modification.
        // Index matches _cameras[i]. Stays valid because BikeCamera objects persist.
        private static float[] _defaults = null;

        // ── Public API ────────────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Apply();
            else Restore();
            MelonLogger.Msg("[FOV] -> " + (Enabled ? "ON (" + DisplayValue + ")" : "OFF"));
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

        // Called from OnLateUpdate every frame
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
                    if ((object)_cameras[i] == null) continue;
                    // Fresh fetch every call — survives camera mode switches
                    CameraAngle ca = _caField.GetValue(_cameras[i]) as CameraAngle;
                    if ((object)ca == null) continue;

                    // Capture default on first encounter (before we overwrite it)
                    if (_defaults[i] < 0f)
                    {
                        _defaults[i] = ca.targetFOV;
                        MelonLogger.Msg("[FOV] Captured default for camera " + i
                            + ": " + _defaults[i]);
                    }

                    ca.targetFOV = target;
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[FOV] Apply: " + ex.Message); }
        }

        // Called when toggled OFF — restores each camera to its captured default.
        // Fetches live CameraAngle fresh so it works even after a camera switch.
        private static void Restore()
        {
            try
            {
                if ((object)_cameras == null || (object)_caField == null) return;
                for (int i = 0; i < _cameras.Length; i++)
                {
                    if ((object)_cameras[i] == null) continue;
                    CameraAngle ca = _caField.GetValue(_cameras[i]) as CameraAngle;
                    if ((object)ca == null) continue;
                    // Use captured default; fall back to 85f if not yet captured
                    ca.targetFOV = (_defaults != null && _defaults[i] > 0f)
                        ? _defaults[i]
                        : 85f;
                }
                MelonLogger.Msg("[FOV] Restored default FOV.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[FOV] Restore: " + ex.Message); }
        }

        // Scene unload / reset
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
            // Rebuild camera list if empty
            if ((object)_cameras == null || _cameras.Length == 0)
            {
                _cameras = UnityEngine.Object.FindObjectsOfType<BikeCamera>();
                _defaults = new float[_cameras.Length];
                for (int i = 0; i < _defaults.Length; i++) _defaults[i] = -1f;
                _fieldScan = false; // re-scan field on new camera set
            }

            // Find the CameraAngle FieldInfo once per camera set
            if (!_fieldScan && _cameras.Length > 0)
            {
                _fieldScan = true;
                FieldInfo[] fields = _cameras[0].GetType().GetFields(
                    BindingFlags.Public | BindingFlags.Instance);
                for (int j = 0; j < fields.Length; j++)
                {
                    if (!string.Equals(fields[j].FieldType.Name, "CameraAngle",
                        StringComparison.Ordinal)) continue;
                    _caField = fields[j];
                    MelonLogger.Msg("[FOV] Found CameraAngle field: " + fields[j].Name);
                    break;
                }
                if ((object)_caField == null)
                    MelonLogger.Warning("[FOV] CameraAngle field not found on BikeCamera.");
            }
        }
    }
}