using MelonLoader;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    public static class CenterOfMass
    {
        public const float Step = 0.1f;
        public const float Min = -5.0f;
        public const float Max = 5.0f;

        public static float OffsetLR { get; private set; } = 0f;
        public static float OffsetFB { get; private set; } = 0f;
        public static float OffsetUD { get; private set; } = 0f;

        private static Rigidbody _rb = null;

        // ── Display strings ──────────────────────────────────────────
        public static string DisplayLR { get { return FormatVal(OffsetLR); } }
        public static string DisplayFB { get { return FormatVal(OffsetFB); } }
        public static string DisplayUD { get { return FormatVal(OffsetUD); } }

        private static string FormatVal(float v)
        {
            string s = v.ToString("F1");
            return v > 0f ? "+" + s : s;
        }

        public static float BarLR { get { return (OffsetLR - Min) / (Max - Min); } }
        public static float BarFB { get { return (OffsetFB - Min) / (Max - Min); } }
        public static float BarUD { get { return (OffsetUD - Min) / (Max - Min); } }

        // ── Rigidbody cache ──────────────────────────────────────────
        private static bool EnsureRb()
        {
            if (UnityNull.Alive(_rb)) return true;
            _rb = null;
            GameObject player = GameObject.Find("Player_Human");
            if (!UnityNull.Alive(player)) return false;
            _rb = player.GetComponentInChildren<Rigidbody>();
            return UnityNull.Alive(_rb);
        }

        // ── Apply ────────────────────────────────────────────────────
        private static void Apply()
        {
            if (!EnsureRb()) return;
            _rb.centerOfMass = new Vector3(OffsetLR, OffsetUD, OffsetFB);
        }

        public static void FixedTick()
        {
            if (OffsetLR == 0f && OffsetUD == 0f && OffsetFB == 0f) return;
            Apply();
        }

        // ── Setters ──────────────────────────────────────────────────
        public static void SetLR(float v)
        {
            OffsetLR = Mathf.Round(Mathf.Clamp(v, Min, Max) * 10f) / 10f;
            Apply();
        }

        public static void SetFB(float v)
        {
            OffsetFB = Mathf.Round(Mathf.Clamp(v, Min, Max) * 10f) / 10f;
            Apply();
        }

        public static void SetUD(float v)
        {
            OffsetUD = Mathf.Round(Mathf.Clamp(v, Min, Max) * 10f) / 10f;
            Apply();
        }

        public static void IncreaseLR() { SetLR(OffsetLR + Step); }
        public static void DecreaseLR() { SetLR(OffsetLR - Step); }
        public static void IncreaseFB() { SetFB(OffsetFB + Step); }
        public static void DecreaseFB() { SetFB(OffsetFB - Step); }
        public static void IncreaseUD() { SetUD(OffsetUD + Step); }
        public static void DecreaseUD() { SetUD(OffsetUD - Step); }

        // ── Per-axis reset ───────────────────────────────────────────
        public static void ResetLR() { SetLR(0f); }
        public static void ResetFB() { SetFB(0f); }
        public static void ResetUD() { SetUD(0f); }

        public static void Reset()
        {
            OffsetLR = 0f;
            OffsetFB = 0f;
            OffsetUD = 0f;
            try { if (UnityNull.Alive(_rb)) _rb.ResetCenterOfMass(); } catch { }
            _rb = null;
        }
    }
}

