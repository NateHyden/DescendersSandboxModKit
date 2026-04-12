using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class SlowMoOnBail
    {
        public static bool Enabled { get; private set; } = false;

        private const float SlowScale = 0.25f;    // 25% speed during bail
        private const float Duration = 3.0f;      // real-time seconds of slow-mo
        private const float RampDuration = 1.0f;   // real-time seconds to ramp back to normal

        private static bool _active = false;       // in slow-mo from a bail
        private static float _endTime = -1f;       // when slow-mo phase ends → ramp begins
        private static bool _ramping = false;       // ramping back to normal speed
        private static float _rampStart = -1f;     // real-time when ramp began
        private static float _rampFromScale = SlowScale; // scale at ramp start

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (!Enabled) CancelImmediate();
            MelonLogger.Msg("[SlowMoOnBail] -> " + (Enabled ? "ON" : "OFF"));
        }

        // Called by SessionTrackers.OnBailDetected()
        public static void OnBail()
        {
            if (!Enabled) return;
            _active = true;
            _ramping = false;
            _endTime = Time.realtimeSinceStartup + Duration;
            SetScale(SlowScale);
            MelonLogger.Msg("[SlowMoOnBail] Bail detected — slow-mo for " + Duration + "s");
        }

        // Called when the player resets/respawns — begin smooth ramp to normal
        public static void OnRespawn()
        {
            if (!_active && !_ramping) return;
            StartRamp();
            MelonLogger.Msg("[SlowMoOnBail] Respawn — ramping to normal over " + RampDuration + "s");
        }

        // Called every frame from OnUpdate
        public static void Tick()
        {
            // ── Ramp phase: smoothly lerp back to 1.0 ─────────────────
            if (_ramping)
            {
                float elapsed = Time.realtimeSinceStartup - _rampStart;
                float t = Mathf.Clamp01(elapsed / RampDuration);
                // Ease-out curve for a natural feel
                float eased = 1f - (1f - t) * (1f - t);
                float scale = Mathf.Lerp(_rampFromScale, 1f, eased);
                SetScale(scale);
                if (t >= 1f)
                {
                    _ramping = false;
                    SetScale(1f);
                    MelonLogger.Msg("[SlowMoOnBail] Restored normal speed.");
                }
                return;
            }

            // ── Slow-mo phase: wait for timer to expire ───────────────
            if (!_active) return;
            if (Time.realtimeSinceStartup >= _endTime)
            {
                StartRamp();
                MelonLogger.Msg("[SlowMoOnBail] Timer expired — ramping to normal over " + RampDuration + "s");
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

        // Hard cancel — used when toggling OFF or resetting
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
            catch (System.Exception ex) { MelonLogger.Error("[SlowMoOnBail] SetScale: " + ex.Message); }
        }

        public static void Reset()
        {
            Enabled = false;
            CancelImmediate();
        }

        // ── Harmony patch — hooks into player respawn to cancel slow-mo ──
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

                MelonLogger.Msg("[SlowMoOnBail] Patched respawn methods.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[SlowMoOnBail] ApplyPatch: " + ex.Message); }
        }
    }

    public static class SlowMoOnBailRespawn_Patch
    {
        public static void Postfix() { SlowMoOnBail.OnRespawn(); }
    }
}