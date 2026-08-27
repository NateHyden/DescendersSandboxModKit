using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using System.Reflection;

namespace DescendersModMenu.Mods
{
    public static class EarthquakeMode
    {
        public static bool Enabled { get; private set; } = false;

        public static int IntensityLevel { get; private set; } = 5;
        public static int DurationLevel { get; private set; } = 5;
        public static int FrequencyLevel { get; private set; } = 5;

        public static int FrequencyMode { get; private set; } = 0;

        private const int MinLevel = 1;
        private const int MaxLevel = 10;
        private const float ForeshadowLead = 1.6f;

        // ── Derived values ────────────────────────────────────────────
        private static float ImpulseForce =>
            Mathf.Lerp(10f, 100f, (IntensityLevel - 1) / 9f);

        private static float TimedInterval =>
            Mathf.Lerp(8f, 1f, (FrequencyLevel - 1) / 9f);

        private static float EventDuration =>
            Mathf.Lerp(1f, 10f, (DurationLevel - 1) / 9f);

        private const float ImpulseCadence = 0.25f;

        private static float ShakeAmount =>
            Mathf.Lerp(80f, 400f, (IntensityLevel - 1) / 9f);

        private const float DefaultShake = 30f;
        private static float ForeshadowShake =>
            Mathf.Lerp(40f, 120f, (IntensityLevel - 1) / 9f);

        // ── State ─────────────────────────────────────────────────────
        private static float _quakeRemaining = 0f;
        private static float _intervalTimer = 0f;
        private static float _impulseTimer = 0f;
        private static bool _foreshadowed = false;

        private static Rigidbody _rb = null;
        private static FieldInfo _caFld = null;

        // ── HUD accessors ─────────────────────────────────────────────
        public static bool IsQuaking
        {
            get { return Enabled && (FrequencyMode == 2 || _quakeRemaining > 0f); }
        }

        public static bool IsForeshadowing
        {
            get
            {
                return Enabled && FrequencyMode != 2 && _quakeRemaining <= 0f
                    && _intervalTimer > 0f && _intervalTimer <= ForeshadowLead;
            }
        }

        public static float NextQuakeIn
        {
            get
            {
                if (!Enabled) return -1f;
                if (FrequencyMode == 2) return 0f;
                if (_quakeRemaining > 0f) return 0f;
                return Mathf.Max(0f, _intervalTimer);
            }
        }

        public static float QuakeRemaining
        {
            get { return FrequencyMode == 2 ? -1f : Mathf.Max(0f, _quakeRemaining); }
        }

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
                _foreshadowed = false;
                ApplyCameraShake(DefaultShake);
            }
            else
            {
                _intervalTimer = FrequencyMode == 1
                    ? Random.Range(2f, 8f)
                    : TimedInterval;
                _foreshadowed = false;
            }
            ModLog.Debug("[EarthquakeMode] " + (Enabled ? "ON" : "OFF"));
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
                    _foreshadowed = false;
                    if (FrequencyMode == 1)
                        _intervalTimer = Random.Range(3f, 30f);
                    else
                        _intervalTimer = TimedInterval;
                    ApplyCameraShake(DefaultShake);
                    ModLog.Debug("[EarthquakeMode] Event ended. Next in "
                        + _intervalTimer.ToString("F1") + "s");
                }
            }
            else
            {
                _intervalTimer -= dt;

                if (_intervalTimer <= ForeshadowLead && _intervalTimer > 0f)
                {
                    ApplyCameraShake(ForeshadowShake);
                    if (!_foreshadowed)
                    {
                        _foreshadowed = true;
                        ModLog.Debug("[EarthquakeMode] Foreshadow...");
                    }
                }

                if (_intervalTimer <= 0f)
                {
                    _quakeRemaining = EventDuration;
                    _impulseTimer = 0f;
                    _foreshadowed = false;
                    ModLog.Debug("[EarthquakeMode] Event started! dur="
                        + EventDuration.ToString("F1") + "s force="
                        + ImpulseForce.ToString("F1"));
                }
            }
        }

        private static void FireImpulse()
        {
            if (!UnityNull.Alive(_rb))
            {
                _rb = null;
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) return;
                _rb = player.GetComponentInChildren<Rigidbody>();
            }
            if (!UnityNull.Alive(_rb)) return;

            float f = ImpulseForce;
            // Mostly lateral — keep you on the ground more than yeeting airborne.
            Vector3 impulse = new Vector3(
                Random.Range(-f, f),
                Random.Range(-f * 0.02f, f * 0.02f),
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
            _foreshadowed = false;
            _rb = null;
            _caFld = null;
            ApplyCameraShake(DefaultShake);
        }
    }
}
