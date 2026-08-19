using MelonLoader;
using UnityEngine;
using HarmonyLib;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    public static class DrunkMode
    {
        public static bool Enabled { get; private set; } = false;

        private static float _time = 0f;
        private static float _fovTime = 0f;
        private static float _steerTime = 0f;
        private static float _camRollTime = 0f;

        private static float _baseFOV = 60f;
        private static Camera _cam = null;
        private static float _lastRoll = 0f;
        private static float _smoothRoll = 0f;
        private static Quaternion _cleanRot = Quaternion.identity;
        private static CameraAngle _cachedAngle = null;

        private static System.Reflection.PropertyInfo _bodyLeanProp = null;

        private static bool _subscribed = false;
        private static void EnsureSubscribed()
        {
            if (_subscribed) return;
            Camera.onPreRender += OnPreRenderCamera;
            _subscribed = true;
        }

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled)
            {
                EnsureSubscribed();
                _time = 0f;
                _fovTime = 0f;
                _steerTime = 0f;
                _camRollTime = 0f;
                _cam = Camera.main;
                if (UnityNull.Alive(_cam)) _baseFOV = _cam.fieldOfView;
                _cachedAngle = null;
                BikeCamera[] cams = GameObject.FindObjectsOfType<BikeCamera>();
                for (int i = 0; i < cams.Length; i++)
                {
                    System.Reflection.FieldInfo[] fields = cams[i].GetType().GetFields(
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    for (int j = 0; j < fields.Length; j++)
                    {
                        if (!string.Equals(fields[j].FieldType.Name, "CameraAngle",
                            System.StringComparison.Ordinal)) continue;
                        CameraAngle ca = fields[j].GetValue(cams[i]) as CameraAngle;
                        if (UnityNull.Alive(ca)) { _baseFOV = ca.targetFOV; _cachedAngle = ca; }
                        break;
                    }
                }
                ModLog.Feedback("[DrunkMode] ON");
            }
            else
            {
                if (UnityNull.Alive(_cam)) _cam.fieldOfView = _baseFOV;
                _cachedAngle = null;
                ModLog.Feedback("[DrunkMode] OFF");
            }
        }

        public static void Tick()
        {
            if (!Enabled) return;
            _time += Time.deltaTime;
            _steerTime += Time.deltaTime * 0.7f;
        }

        public static void LateTick()
        {
            if (!Enabled) return;
            _fovTime += Time.deltaTime * 0.4f;
            _camRollTime += Time.deltaTime * 0.18f;
        }

        private static void OnPreRenderCamera(Camera cam)
        {
            if (!Enabled) return;
            if (!UnityNull.Alive(cam) || !UnityNull.Alive(Camera.main) || cam != Camera.main) return;

            _cam = cam;

            // ── FOV breathing ──────────────────────────────────────────────
            float fovWobble = Mathf.Sin(_fovTime * Mathf.PI * 2f) * 10f
                            + Mathf.Sin(_fovTime * Mathf.PI * 3.3f) * 5f;
            if (UnityNull.Alive(_cachedAngle))
                _cachedAngle.targetFOV = _baseFOV + fovWobble;
            _cam.fieldOfView = _baseFOV + fovWobble;

            // ── Camera roll ───────────────────────────────────────────────
            float roll = Mathf.Sin(_camRollTime * Mathf.PI * 2f) * 14f
                       + Mathf.Sin(_camRollTime * Mathf.PI * 1.7f) * 6f;
            _smoothRoll = Mathf.Lerp(_smoothRoll, roll, Time.deltaTime * 3f);
            _cleanRot = _cam.transform.rotation * Quaternion.Inverse(Quaternion.Euler(0f, 0f, _lastRoll));
            _cam.transform.rotation = _cleanRot * Quaternion.Euler(0f, 0f, _smoothRoll);
            _lastRoll = _smoothRoll;
        }

        public static void ApplySteeringWobble(Vehicle vehicle)
        {
            if (!Enabled || !UnityNull.Alive(vehicle)) return;

            _steerTime += Time.fixedDeltaTime * 0.7f;

            float wobble = Mathf.Sin(_steerTime * Mathf.PI * 2f) * 0.7f
                         + Mathf.Sin(_steerTime * Mathf.PI * 5.1f) * 0.25f
                         + Mathf.Sin(_steerTime * Mathf.PI * 2.3f) * 0.15f;

            vehicle.swebLyg += wobble;
            if ((object)_bodyLeanProp == null)
                _bodyLeanProp = typeof(Vehicle).GetProperty("zsEdyM\u007D",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if ((object)_bodyLeanProp != null)
            {
                float cur = (float)_bodyLeanProp.GetValue(vehicle, null);
                _bodyLeanProp.SetValue(vehicle, cur + wobble * 0.4f, null);
            }
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            var original = typeof(Vehicle).GetMethod("FixedUpdate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if ((object)original == null) { ModLog.Warn("[DrunkMode] Vehicle.FixedUpdate not found"); return; }
            var postfix = typeof(DrunkMode_Patch).GetMethod("Postfix");
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            ModLog.Debug("[DrunkMode] Patched Vehicle.FixedUpdate");
        }

        public static void Reset()
        {
            if (Enabled)
            {
                if (UnityNull.Alive(_cam)) _cam.fieldOfView = _baseFOV;
                if (UnityNull.Alive(_cam)) _cam.transform.rotation *= Quaternion.Euler(0f, 0f, -_lastRoll);
                Enabled = false;
            }
            _lastRoll = 0f;
            _smoothRoll = 0f;
            _cam = null;
        }
    }

    public static class DrunkMode_Patch
    {
        public static void Postfix(Vehicle __instance)
        {
            DrunkMode.ApplySteeringWobble(__instance);
        }
    }
}

