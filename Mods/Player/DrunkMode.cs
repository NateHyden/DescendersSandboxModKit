using MelonLoader;
using UnityEngine;
using HarmonyLib;

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
        private static CameraAngle _cachedAngle = null; // cached once on enable

        // Reflection for zsEdyM} (body lean) — brace in name breaks direct access
        private static System.Reflection.PropertyInfo _bodyLeanProp = null;

        // Camera.onPreRender is a static Unity event guaranteed to fire
        // AFTER every LateUpdate() in the scene for that frame, right
        // before the camera actually renders. LateTick below used to run
        // from MelonLoader's OnLateUpdate hook on the assumption that ran
        // after the game's own BikeCamera.LateUpdate() — that ordering
        // between a mod hook and a native Unity component's own LateUpdate
        // was never actually guaranteed, so some frames this code won the
        // race and some frames BikeCamera won, alternating winners frame
        // to frame — that's exactly what the reported jitter was.
        // onPreRender removes the race: nothing can run after it.
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
                if ((object)_cam != null) _baseFOV = _cam.fieldOfView;
                // Cache CameraAngle once on enable — reused every LateTick
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
                        if ((object)ca != null) { _baseFOV = ca.targetFOV; _cachedAngle = ca; }
                        break;
                    }
                }
                ModLog.Feedback("[DrunkMode] ON");
            }
            else
            {
                // Restore FOV
                if ((object)_cam != null) _cam.fieldOfView = _baseFOV;
                _cachedAngle = null;
                ModLog.Feedback("[DrunkMode] OFF");
            }
        }

        // Called from OnUpdate — steering wobble only
        public static void Tick()
        {
            if (!Enabled) return;
            _time += Time.deltaTime;
            _steerTime += Time.deltaTime * 0.7f;
        }

        // Called from OnLateUpdate — accumulates timers only now. Actual
        // camera writes moved to OnPreRenderCamera (see EnsureSubscribed).
        public static void LateTick()
        {
            if (!Enabled) return;
            _fovTime += Time.deltaTime * 0.4f;
            _camRollTime += Time.deltaTime * 0.18f;
        }

        private static void OnPreRenderCamera(Camera cam)
        {
            if (!Enabled) return;
            if ((object)cam == null || (object)Camera.main == null || cam != Camera.main) return;

            _cam = cam;

            // ── FOV breathing ──────────────────────────────────────────────
            float fovWobble = Mathf.Sin(_fovTime * Mathf.PI * 2f) * 10f
                            + Mathf.Sin(_fovTime * Mathf.PI * 3.3f) * 5f;
            // Write BOTH: targetFOV in case anything else legitimately reads
            // it, but fieldOfView directly too — writing only targetFOV left
            // the actual visible zoom entirely dependent on whatever/whenever
            // BikeCamera itself chooses to consume that field, which we don't
            // control or know the timing of. Direct write makes us the
            // final, guaranteed authority on the rendered value, exactly
            // like the camera roll write below already was.
            if ((object)_cachedAngle != null)
                _cachedAngle.targetFOV = _baseFOV + fovWobble;
            _cam.fieldOfView = _baseFOV + fovWobble;

            // ── Camera roll ───────────────────────────────────────────────
            float roll = Mathf.Sin(_camRollTime * Mathf.PI * 2f) * 14f
                       + Mathf.Sin(_camRollTime * Mathf.PI * 1.7f) * 6f;
            // Lerp toward target roll for smooth transitions — no more snapping
            _smoothRoll = Mathf.Lerp(_smoothRoll, roll, Time.deltaTime * 3f);
            _cleanRot = _cam.transform.rotation * Quaternion.Inverse(Quaternion.Euler(0f, 0f, _lastRoll));
            _cam.transform.rotation = _cleanRot * Quaternion.Euler(0f, 0f, _smoothRoll);
            _lastRoll = _smoothRoll;
        }

        // Called from Harmony postfix on Vehicle.FixedUpdate — adds steering wobble
        public static void ApplySteeringWobble(Vehicle vehicle)
        {
            if (!Enabled || (object)vehicle == null) return;

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
            if ((object)original == null) { MelonLogger.Warning("[DrunkMode] Vehicle.FixedUpdate not found"); return; }
            var postfix = typeof(DrunkMode_Patch).GetMethod("Postfix");
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            ModLog.Debug("[DrunkMode] Patched Vehicle.FixedUpdate");
        }

        public static void Reset()
        {
            if (Enabled)
            {
                if ((object)_cam != null) _cam.fieldOfView = _baseFOV;
                if ((object)_cam != null) _cam.transform.rotation *= Quaternion.Euler(0f, 0f, -_lastRoll);
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