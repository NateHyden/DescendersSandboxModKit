using MelonLoader;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    public static class SlowMoOnBail
    {
        public static bool Enabled { get; private set; } = false;

        private const float SlowScale = 0.25f;
        private const float Duration = 3.0f;
        private const float RampDuration = 1.0f;

        private static bool _active = false;
        private static float _endTime = -1f;
        private static bool _ramping = false;
        private static float _rampStart = -1f;
        private static float _rampFromScale = SlowScale;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (!Enabled) CancelImmediate();
            ModLog.Feedback("[SlowMoOnBail] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void OnBail()
        {
            if (!Enabled) return;
            _active = true;
            _ramping = false;
            _endTime = Time.realtimeSinceStartup + Duration;
            SetScale(SlowScale);
            ModLog.Debug("[SlowMoOnBail] Bail detected — slow-mo for " + Duration + "s");
        }

        public static void OnRespawn()
        {
            if (!_active && !_ramping) return;
            StartRamp();
            ModLog.Debug("[SlowMoOnBail] Respawn — ramping to normal over " + RampDuration + "s");
        }

        public static void Tick()
        {
            if (_ramping)
            {
                float elapsed = Time.realtimeSinceStartup - _rampStart;
                float t = Mathf.Clamp01(elapsed / RampDuration);
                float eased = 1f - (1f - t) * (1f - t);
                float scale = Mathf.Lerp(_rampFromScale, 1f, eased);
                SetScale(scale);
                if (t >= 1f)
                {
                    _ramping = false;
                    SetScale(1f);
                    ModLog.Debug("[SlowMoOnBail] Restored normal speed.");
                }
                return;
            }

            if (!_active) return;
            if (Time.realtimeSinceStartup >= _endTime)
            {
                StartRamp();
                ModLog.Debug("[SlowMoOnBail] Timer expired — ramping to normal over " + RampDuration + "s");
            }
        }

        private static void StartRamp()
        {
            _active = false;
            _endTime = -1f;
            _ramping = true;
            _rampStart = Time.realtimeSinceStartup;
            _rampFromScale = Time.timeScale > 0.01f ? Time.timeScale : SlowScale;
        }

        private static void CancelImmediate()
        {
            _active = false;
            _ramping = false;
            _endTime = -1f;
            _rampStart = -1f;
            SetScale(1f);
        }

        private static void SetScale(float scale)
        {
            try
            {
                TimeScaleManager mgr = Object.FindObjectOfType<TimeScaleManager>();
                if ((object)mgr != null)
                    mgr.SetTimeScale(scale, true);
                else
                {
                    Time.timeScale = scale;
                    Time.fixedDeltaTime = 0.02f * scale;
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[SlowMoOnBail] SetScale: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SlowMoOnBail"); }
        }

        public static void Reset()
        {
            Enabled = false;
            CancelImmediate();
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                var postfix = typeof(SlowMoOnBailRespawn_Patch).GetMethod("Postfix",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                var m1 = typeof(PlayerInfoImpact).GetMethod("RespawnAtStartLine",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)m1 != null)
                    harmony.Patch(m1, postfix: new HarmonyLib.HarmonyMethod(postfix));

                var m2 = typeof(PlayerInfoImpact).GetMethod("RespawnOnTrack",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)m2 != null)
                    harmony.Patch(m2, postfix: new HarmonyLib.HarmonyMethod(postfix));

                ModLog.Debug("[SlowMoOnBail] Patched respawn methods.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[SlowMoOnBail] ApplyPatch: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "SlowMoOnBail"); }
        }
    }

    public static class SlowMoOnBailRespawn_Patch
    {
        public static void Postfix()
        {
            SlowMoOnBail.OnRespawn();
            BlackDeath.OnRespawn();
        }
    }
}

