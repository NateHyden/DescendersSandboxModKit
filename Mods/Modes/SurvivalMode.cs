using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class SurvivalMode
    {
        // ── Public state ──────────────────────────────────────────────
        public static bool Enabled { get; private set; } = false;
        public static bool IsGameOver { get; private set; } = false;
        public static float HP { get; private set; } = 100f;
        public const float MaxHP = 100f;

        // ── Settings ──────────────────────────────────────────────────
        private static readonly int[] BailPenaltyValues = { 10, 20, 30, 40 };
        private static readonly string[] BailPenaltyLabels = { "10 HP", "20 HP", "30 HP", "40 HP" };
        public static int BailPenaltyIndex = 1;
        public static string BailPenaltyDisplay => BailPenaltyLabels[BailPenaltyIndex];

        private static readonly float[] BleedValues = { 0f, 2f, 5f, 10f };
        private static readonly string[] BleedLabels = { "None", "Slow", "Medium", "Fast" };
        public static int BleedIndex = 1;
        public static string BleedDisplay => BleedLabels[BleedIndex];

        private static readonly int[] HealValues = { 5, 10, 20 };
        private static readonly string[] HealLabels = { "5 HP", "10 HP", "20 HP" };
        public static int HealIndex = 1;
        public static string HealDisplay => HealLabels[HealIndex];

        private const float SmallJumpTime = 0.4f;
        private const float BigJumpTime = 1.2f;

        // ── Selectors ─────────────────────────────────────────────────
        public static void PrevBailPenalty() { if (BailPenaltyIndex > 0) BailPenaltyIndex--; }
        public static void NextBailPenalty() { if (BailPenaltyIndex < BailPenaltyValues.Length - 1) BailPenaltyIndex++; }
        public static void PrevBleed() { if (BleedIndex > 0) BleedIndex--; }
        public static void NextBleed() { if (BleedIndex < BleedValues.Length - 1) BleedIndex++; }
        public static void PrevHeal() { if (HealIndex > 0) HealIndex--; }
        public static void NextHeal() { if (HealIndex < HealValues.Length - 1) HealIndex++; }

        // ── Stats ─────────────────────────────────────────────────────
        public static float TimeAlive { get; private set; } = 0f;
        public static int BailsTaken { get; private set; } = 0;
        public static int TricksLanded { get; private set; } = 0;

        // ── Internal ──────────────────────────────────────────────────
        private static int _prevBailCount = 0;
        private static bool _wasAirborne = false;
        private static float _airtimeAccum = 0f;
        private static float _prevVelY = 0f;

        private static Rigidbody _rb = null;
        private static float _bailBleedPause = 0f;
        private const float IdleSpeedThreshold = 2.5f;

        // ── Toggle / Reset ────────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) { ResetRun(); ModLog.Debug("[Survival] ON"); }
            else { IsGameOver = false; ModLog.Debug("[Survival] OFF"); }
        }

        public static void ResetRun()
        {
            HP = MaxHP;
            IsGameOver = false;
            TimeAlive = 0f;
            BailsTaken = 0;
            TricksLanded = 0;
            _prevBailCount = SessionTrackers.BailCount;
            _wasAirborne = false;
            _airtimeAccum = 0f;
            _prevVelY = 0f;
            _bailBleedPause = 0f;
            ModLog.Debug("[Survival] Run reset.");
        }

        public static void Reset()
        {
            Enabled = false;
            IsGameOver = false;
            HP = MaxHP;
            _rb = null;
        }

        // ── Tick ──────────────────────────────────────────────────────
        public static void Tick()
        {
            if (!Enabled || IsGameOver) return;

            TimeAlive += Time.deltaTime;

            // ── Bail detection ────────────────────────────────────────
            int currentBails = SessionTrackers.BailCount;
            bool bailedThisFrame = currentBails > _prevBailCount;
            if (bailedThisFrame)
            {
                int newBails = currentBails - _prevBailCount;
                HP -= BailPenaltyValues[BailPenaltyIndex] * newBails;
                BailsTaken += newBails;
                _bailBleedPause = 2f;
            }
            _prevBailCount = currentBails;

            // ── Speed bleed (only while nearly stopped) ───────────────
            float bleedRate = BleedValues[BleedIndex];
            if (bleedRate > 0f && _bailBleedPause <= 0f)
            {
                float hSpeed = GetHorizontalSpeed();
                if (hSpeed < IdleSpeedThreshold)
                    HP -= bleedRate * Time.deltaTime;
            }
            if (_bailBleedPause > 0f)
                _bailBleedPause -= Time.deltaTime;

            float velY = GetVerticalVelocity();

            bool falling = velY < -2f;
            bool grounded = velY > -0.5f && _prevVelY < -0.5f;

            if (falling || (_wasAirborne && !grounded))
            {
                _airtimeAccum += Time.deltaTime;
                _wasAirborne = true;
            }

            if (_wasAirborne && grounded && !bailedThisFrame)
            {
                if (_airtimeAccum >= BigJumpTime)
                {
                    int heal = HealValues[HealIndex];
                    HP = Mathf.Min(MaxHP, HP + heal);
                    TricksLanded++;
                    ModLog.Debug("[Survival] Big landing +" + heal + " HP (air=" + _airtimeAccum.ToString("F2") + "s)");
                }
                else if (_airtimeAccum >= SmallJumpTime)
                {
                    HP = Mathf.Min(MaxHP, HP + 5);
                    TricksLanded++;
                    ModLog.Debug("[Survival] Small landing +5 HP (air=" + _airtimeAccum.ToString("F2") + "s)");
                }
                _wasAirborne = false;
                _airtimeAccum = 0f;
            }

            if (bailedThisFrame)
            {
                _wasAirborne = false;
                _airtimeAccum = 0f;
            }

            _prevVelY = velY;

            // ── Game over ─────────────────────────────────────────────
            if (HP <= 0f)
            {
                HP = 0f;
                IsGameOver = true;
                ModLog.Debug("[Survival] GAME OVER. " + TimeAlive.ToString("F1")
                    + "s  Bails:" + BailsTaken + "  Tricks:" + TricksLanded);
            }
        }

        // ── Velocity helpers ──────────────────────────────────────────
        private static float GetHorizontalSpeed()
        {
            try
            {
                EnsureRb();
                if (!UnityNull.Alive(_rb)) return 0f;
                Vector3 v = _rb.velocity;
                return new Vector2(v.x, v.z).magnitude;
            }
            catch { _rb = null; return 0f; }
        }

        private static float GetVerticalVelocity()
        {
            try
            {
                EnsureRb();
                if (!UnityNull.Alive(_rb)) return 0f;
                return _rb.velocity.y;
            }
            catch { _rb = null; return 0f; }
        }

        private static void EnsureRb()
        {
            if (UnityNull.Alive(_rb)) return;
            _rb = null;
            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) return;
            _rb = player.GetComponentInChildren<Rigidbody>();
        }
    }
}

