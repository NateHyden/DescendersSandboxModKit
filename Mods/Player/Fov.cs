using MelonLoader;
using DescendersModMenu;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class FOV
    {
        public static bool Enabled { get; private set; } = false;
        public static int Level { get; private set; } = 5;
        public const int MaxLevel = 20;

        // Game clamps around 139 — going past that breaks restore / feels zoomed when toggled off.
        private const float MinFov = 45f;
        private const float MaxFov = 139f;
        private const float FallbackFov = 85f;

        private static float GetFOV()
        {
            return Mathf.Lerp(MinFov, MaxFov, (Level - 1) / (float)(MaxLevel - 1));
        }
        public static string DisplayValue { get { return ((int)GetFOV()).ToString(); } }

        private static BikeCamera[] _bikeCams = null;
        private static FieldInfo _caField = null;
        private static FieldInfo _unityCamField = null;
        private static MethodInfo _snapCamMethod = null;
        private static FieldInfo _inGameField = null;
        private static bool _refsCached = false;

        // Keyed by CameraAngle instance ID so EnsureCameras rebuilds can't wipe defaults.
        private static readonly Dictionary<int, float> _defaults = new Dictionary<int, float>();

        private static float SanitizeDefault(float fov)
        {
            if (float.IsNaN(fov) || float.IsInfinity(fov)) return FallbackFov;
            if (fov < MinFov || fov > MaxFov) return FallbackFov;
            return fov;
        }

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Apply();
            else Restore();
            ModLog.Feedback("[FOV] -> " + (Enabled ? "ON (" + DisplayValue + ")" : "OFF"));
        }

        public static void Increase() { if (Level < MaxLevel) Level++; if (Enabled) Apply(); }
        public static void Decrease() { if (Level > 1) Level--; if (Enabled) Apply(); }

        public static void SetLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > MaxLevel) level = MaxLevel;
            Level = level;
            if (Enabled) Apply();
        }

        public static void Apply()
        {
            if (!Enabled) return;
            try
            {
                float target = GetFOV();
                ApplyToAllAngles(target, captureDefaults: true);
                SyncLiveCameras(target, snap: false);
            }
            catch (System.Exception ex) { MelonLogger.Error("[FOV] Apply: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Fov"); }
        }

        private static void Restore()
        {
            try
            {
                // Restore every in-game angle to its captured stock FOV (or fallback).
                EnsureRefs();
                CameraAngle[] angles = FindInGameAngles();
                for (int i = 0; i < angles.Length; i++)
                {
                    CameraAngle ca = angles[i];
                    if (!UnityNull.Alive(ca)) continue;
                    int id = ca.GetInstanceID();
                    float restore = FallbackFov;
                    float saved;
                    if (_defaults.TryGetValue(id, out saved) && saved > 0f)
                        restore = SanitizeDefault(saved);
                    ca.targetFOV = restore;
                }

                // First-person keeps Unity Camera.fieldOfView until a cam switch;
                // push the live lens back and snap so we don't need to cycle views.
                float live = FallbackFov;
                BikeCamera active = FindActiveBikeCamera();
                if (UnityNull.Alive(active) && (object)_caField != null)
                {
                    CameraAngle activeCa = _caField.GetValue(active) as CameraAngle;
                    if (UnityNull.Alive(activeCa))
                        live = activeCa.targetFOV;
                }
                SyncLiveCameras(live, snap: true);
                ModLog.Debug("[FOV] Restored default FOV (live=" + live + ").");
            }
            catch (System.Exception ex) { MelonLogger.Error("[FOV] Restore: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Fov"); }
        }

        public static void ClearCache()
        {
            _bikeCams = null;
            _caField = null;
            _unityCamField = null;
            _snapCamMethod = null;
            _inGameField = null;
            _refsCached = false;
            _defaults.Clear();
        }

        public static void Reset()
        {
            if (Enabled) Restore();
            Enabled = false;
            ClearCache();
        }

        private static void ApplyToAllAngles(float target, bool captureDefaults)
        {
            EnsureRefs();
            CameraAngle[] angles = FindInGameAngles();
            for (int i = 0; i < angles.Length; i++)
            {
                CameraAngle ca = angles[i];
                if (!UnityNull.Alive(ca)) continue;
                int id = ca.GetInstanceID();
                if (captureDefaults && !_defaults.ContainsKey(id))
                {
                    float stock = SanitizeDefault(ca.targetFOV);
                    _defaults[id] = stock;
                    ModLog.Debug("[FOV] Captured default id=" + id + " fov=" + stock
                        + (string.IsNullOrEmpty(ca.displayName) ? "" : " (" + ca.displayName + ")"));
                }
                ca.targetFOV = target;
            }
        }

        private static CameraAngle[] FindInGameAngles()
        {
            CameraAngle[] all = UnityEngine.Object.FindObjectsOfType<CameraAngle>();
            if ((object)all == null || all.Length == 0) return new CameraAngle[0];

            // Prefer inGame==true when the field is available; otherwise keep all.
            if ((object)_inGameField == null)
                return all;

            List<CameraAngle> list = new List<CameraAngle>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                CameraAngle ca = all[i];
                if (!UnityNull.Alive(ca)) continue;
                try
                {
                    object v = _inGameField.GetValue(ca);
                    if ((object)v != null
                        && string.Equals(v.GetType().FullName, "System.Boolean", StringComparison.Ordinal)
                        && !(bool)v)
                        continue;
                }
                catch { }
                list.Add(ca);
            }
            return list.Count > 0 ? list.ToArray() : all;
        }

        private static void SyncLiveCameras(float fov, bool snap)
        {
            EnsureBikeCameras();
            if ((object)_bikeCams == null) return;

            for (int i = 0; i < _bikeCams.Length; i++)
            {
                BikeCamera bc = _bikeCams[i];
                if (!UnityNull.Alive(bc)) continue;

                Camera cam = GetUnityCamera(bc);
                if (UnityNull.Alive(cam))
                    cam.fieldOfView = fov;

                if (snap && (object)_snapCamMethod != null)
                {
                    try { _snapCamMethod.Invoke(bc, new object[] { true }); }
                    catch { }
                }
            }
        }

        private static BikeCamera FindActiveBikeCamera()
        {
            EnsureBikeCameras();
            if ((object)_bikeCams == null) return null;
            for (int i = 0; i < _bikeCams.Length; i++)
            {
                BikeCamera bc = _bikeCams[i];
                if (!UnityNull.Alive(bc)) continue;
                if (!bc.isActiveAndEnabled) continue;
                Camera cam = GetUnityCamera(bc);
                if (UnityNull.Alive(cam) && cam.enabled) return bc;
            }
            for (int i = 0; i < _bikeCams.Length; i++)
            {
                if (UnityNull.Alive(_bikeCams[i])) return _bikeCams[i];
            }
            return null;
        }

        private static Camera GetUnityCamera(BikeCamera bc)
        {
            if ((object)_unityCamField != null)
            {
                try
                {
                    Camera c = _unityCamField.GetValue(bc) as Camera;
                    if (UnityNull.Alive(c)) return c;
                }
                catch { }
            }
            Camera onGo = bc.GetComponent<Camera>();
            if (UnityNull.Alive(onGo)) return onGo;
            return bc.GetComponentInChildren<Camera>();
        }

        private static void EnsureRefs()
        {
            if (_refsCached) return;
            _refsCached = true;

            _inGameField = typeof(CameraAngle).GetField("inGame",
                BindingFlags.Public | BindingFlags.Instance);

            FieldInfo[] fields = typeof(BikeCamera).GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                if ((object)_caField == null
                    && string.Equals(fields[i].FieldType.Name, "CameraAngle", StringComparison.Ordinal))
                    _caField = fields[i];
                if ((object)_unityCamField == null
                    && string.Equals(fields[i].FieldType.Name, "Camera", StringComparison.Ordinal))
                    _unityCamField = fields[i];
            }

            _snapCamMethod = typeof(BikeCamera).GetMethod("SnapCamera",
                BindingFlags.Public | BindingFlags.Instance, null,
                new System.Type[] { typeof(bool) }, null);

            if ((object)_caField != null)
                ModLog.Debug("[FOV] CameraAngle field: " + _caField.Name);
            else
                ModLog.Warn("[FOV] CameraAngle field not found on BikeCamera.");
        }

        private static void EnsureBikeCameras()
        {
            bool rebuild = (object)_bikeCams == null || _bikeCams.Length == 0;
            if (!rebuild)
            {
                for (int i = 0; i < _bikeCams.Length; i++)
                {
                    if (!UnityNull.Alive(_bikeCams[i]))
                    {
                        rebuild = true;
                        break;
                    }
                }
            }
            if (rebuild)
                _bikeCams = UnityEngine.Object.FindObjectsOfType<BikeCamera>();
            EnsureRefs();
        }
    }
}
