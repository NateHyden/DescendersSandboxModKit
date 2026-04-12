using MelonLoader;
using UnityEngine;
using System.Reflection;

namespace DescendersModMenu.Mods
{
    public static class EarthquakeMode
    {
        public static bool Enabled { get; private set; } = false;

        // Intensity 1-10: force per impulse
        public static int IntensityLevel { get; private set; } = 5;
        // Duration 1-10: seconds each quake event lasts
        public static int DurationLevel { get; private set; } = 5;
        // Frequency 1-10: only used in Timed mode
        public static int FrequencyLevel { get; private set; } = 5;

        // 0 = Timed, 1 = Random, 2 = Constant
        public static int FrequencyMode { get; private set; } = 0;

        private const int MinLevel = 1;
        private const int MaxLevel = 10;

        // ── Derived values ────────────────────────────────────────────
        // Level 1 = 10f, Level 5 = 40f, Level 10 = 100f
        private static float ImpulseForce =>
            Mathf.Lerp(10f, 100f, (IntensityLevel - 1) / 9f);

        // Timed interval: Level 1 = 8s, Level 10 = 1s
        private static float TimedInterval =>
            Mathf.Lerp(8f, 1f, (FrequencyLevel - 1) / 9f);

        // Duration: Level 1 = 1s, Level 10 = 10s
        private static float EventDuration =>
            Mathf.Lerp(1f, 10f, (DurationLevel - 1) / 9f);

        // Impulse cadence within an event
        private const float ImpulseCadence = 0.25f;

        // Camera shake — significantly stronger: Level 1 = 80f, Level 10 = 400f
        private static float ShakeAmount =>
            Mathf.Lerp(80f, 400f, (IntensityLevel - 1) / 9f);

        private const float DefaultShake = 30f;

        // ── State ─────────────────────────────────────────────────────
        private static float _quakeRemaining = 0f;
        private static float _intervalTimer = 0f;
        private static float _impulseTimer = 0f;

        private static Rigidbody _rb = null;
        private static FieldInfo _caFld = null;

        // ── Display ───────────────────────────────────────────────────
        public static string IntensityDisplay => IntensityLevel.ToString();
        public static string FrequencyDisplay => FrequencyLevel.ToString();
        public static string DurationDisplay => DurationLevel.ToString();
        public static string FrequencyModeName
        {
            get
            {
                if (FrequencyMode == 1) return "Random";
                if (FrequencyMode == 2) return "Constant";
                return "Timed";
            }
        }

        // ── Toggle ────────────────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            if (!Enabled)
            {
                _quakeRemaining = 0f;
                _intervalTimer = 0f;
                _impulseTimer = 0f;
                ApplyCameraShake(DefaultShake);
            }
            MelonLogger.Msg("[EarthquakeMode] " + (Enabled ? "ON" : "OFF"));
        }

        public static void SetFrequencyMode(int mode) { FrequencyMode = mode; }

        // ── Level adjusters ───────────────────────────────────────────
        public static void IncreaseIntensity() { if (IntensityLevel < MaxLevel) IntensityLevel++; }
        public static void DecreaseIntensity() { if (IntensityLevel > MinLevel) IntensityLevel--; }
        public static void IncreaseFrequency() { if (FrequencyLevel < MaxLevel) FrequencyLevel++; }
        public static void DecreaseFrequency() { if (FrequencyLevel > MinLevel) FrequencyLevel--; }
        public static void IncreaseDuration() { if (DurationLevel < MaxLevel) DurationLevel++; }
        public static void DecreaseDuration() { if (DurationLevel > MinLevel) DurationLevel--; }

        // ── FixedTick ─────────────────────────────────────────────────
        public static void FixedTick()
        {
            if (!Enabled) return;

            float dt = Time.fixedDeltaTime;

            // Constant mode — always quaking, skip interval logic
            if (FrequencyMode == 2)
            {
                ApplyCameraShake(ShakeAmount);
                _impulseTimer -= dt;
                if (_impulseTimer <= 0f)
                {
                    _impulseTimer = ImpulseCadence;
                    FireImpulse();
                }
                return;
            }

            if (_quakeRemaining > 0f)
            {
                // Inside a quake event
                _quakeRemaining -= dt;
                _impulseTimer -= dt;
                ApplyCameraShake(ShakeAmount);

                if (_impulseTimer <= 0f)
                {
                    _impulseTimer = ImpulseCadence;
                    FireImpulse();
                }

                if (_quakeRemaining <= 0f)
                {
                    _quakeRemaining = 0f;
                    // Pick next interval based on mode
                    if (FrequencyMode == 1)
                        _intervalTimer = Random.Range(0f, 30f);
                    else
                        _intervalTimer = TimedInterval;
                    ApplyCameraShake(DefaultShake);
                    MelonLogger.Msg("[EarthquakeMode] Event ended. Next in "
                        + _intervalTimer.ToString("F1") + "s");
                }
            }
            else
            {
                // Waiting for next event
                _intervalTimer -= dt;
                if (_intervalTimer <= 0f)
                {
                    _quakeRemaining = EventDuration;
                    _impulseTimer = 0f;
                    MelonLogger.Msg("[EarthquakeMode] Event started! dur="
                        + EventDuration.ToString("F1") + "s force="
                        + ImpulseForce.ToString("F1"));
                }
            }
        }

        private static void FireImpulse()
        {
            if ((object)_rb == null)
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) return;
                _rb = player.GetComponentInChildren<Rigidbody>();
            }
            if ((object)_rb == null) return;

            float f = ImpulseForce;
            // Horizontal shaking only — tiny Y so the player stays on the bike
            Vector3 impulse = new Vector3(
                Random.Range(-f, f),
                Random.Range(-f * 0.05f, f * 0.05f),
                Random.Range(-f, f)
            );
            _rb.AddForce(impulse, ForceMode.Impulse);
        }

        private static void ApplyCameraShake(float shake)
        {
            try
            {
                BikeCamera[] cameras = UnityEngine.Object.FindObjectsOfType<BikeCamera>();
                if (cameras == null || cameras.Length == 0) return;

                if ((object)_caFld == null)
                {
                    FieldInfo[] fields = typeof(BikeCamera).GetFields(
                        BindingFlags.Public | BindingFlags.Instance);
                    for (int f = 0; f < fields.Length; f++)
                    {
                        if (string.Equals(fields[f].FieldType.Name, "CameraAngle",
                            System.StringComparison.Ordinal))
                        { _caFld = fields[f]; break; }
                    }
                }
                if ((object)_caFld == null) return;

                for (int i = 0; i < cameras.Length; i++)
                {
                    CameraAngle ca = _caFld.GetValue(cameras[i]) as CameraAngle;
                    if ((object)ca == null) continue;
                    ca.cameraShake = shake;
                    ca.impactCameraShake = shake;
                }
            }
            catch { }
        }

        public static void Reset()
        {
            Enabled = false;
            IntensityLevel = 5;
            FrequencyLevel = 5;
            DurationLevel = 5;
            FrequencyMode = 0;
            _quakeRemaining = 0f;
            _intervalTimer = 0f;
            _impulseTimer = 0f;
            _rb = null;
            _caFld = null;
            ApplyCameraShake(DefaultShake);
        }
    }
}