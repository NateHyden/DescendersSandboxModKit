using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Hard cap on bike speed in km/h (same formula as TopSpeed / speedo).
    /// </summary>
    public static class SpeedLimiter
    {
        public static bool Enabled { get; private set; }
        public static float LimitKmh { get; private set; } = 80f;

        private const float MinKmh = 5f;
        private const float MaxKmh = 500f;

        private static GameObject _cachedPlayer;
        private static Rigidbody _cachedRb;

        public static string DisplayLimit
        {
            get { return Mathf.RoundToInt(LimitKmh).ToString(); }
        }

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("[Speed Limiter] -> " + (Enabled ? "ON (" + DisplayLimit + " km/h)" : "OFF"));
        }

        public static void SetEnabled(bool on)
        {
            Enabled = on;
        }

        public static void SetLimitKmh(float kmh)
        {
            if (kmh < MinKmh) kmh = MinKmh;
            if (kmh > MaxKmh) kmh = MaxKmh;
            LimitKmh = kmh;
            if (Enabled)
                ModLog.Feedback("[Speed Limiter] Cap -> " + DisplayLimit + " km/h");
        }

        public static bool TrySetLimitFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            float v;
            if (!float.TryParse(text.Trim(), out v)) return false;
            SetLimitKmh(v);
            return true;
        }

        public static void ClearCache()
        {
            _cachedPlayer = null;
            _cachedRb = null;
        }

        public static void Reset()
        {
            Enabled = false;
            LimitKmh = 80f;
            ClearCache();
        }

        public static void Tick()
        {
            if (!Enabled) return;
            try
            {
                if (!UnityNull.Alive(_cachedPlayer) || !_cachedPlayer.activeInHierarchy)
                {
                    _cachedPlayer = GameObject.Find("Player_Human");
                    _cachedRb = null;
                }
                if (!UnityNull.Alive(_cachedPlayer)) return;

                if (!UnityNull.Alive(_cachedRb))
                    _cachedRb = _cachedPlayer.GetComponent<Rigidbody>();
                if (!UnityNull.Alive(_cachedRb)) return;

                float gravMag = Physics.gravity.magnitude;
                if (gravMag < 0.01f) gravMag = 17.5f;

                // Invert TopSpeed display: km/h = |v| * 3.6 / g * 9.81
                float maxMag = LimitKmh * gravMag / (3.6f * 9.81f);
                if (maxMag < 0.01f) return;

                Vector3 vel = _cachedRb.velocity;
                float mag = vel.magnitude;
                if (mag > maxMag)
                    _cachedRb.velocity = vel * (maxMag / mag);
            }
            catch { }
        }
    }
}
