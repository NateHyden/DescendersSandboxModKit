using MelonLoader;
using UnityEngine;
using System.Reflection;

namespace DescendersModMenu.Mods
{
    public static class TrickAttackMode
    {
        public enum State { Off, WaitingForStart, Running, Success, Fail }
        public static State CurrentState { get; private set; } = State.Off;

        public static int TargetScore { get; private set; } = 5000;
        public static int TimeLimitSecs { get; private set; } = 60;

        private static readonly int[] TimeLimits = { 30, 60, 90, 120 };
        private static int _timeLimitIndex = 1;

        public static void NextTimeLimit()
        {
            _timeLimitIndex = (_timeLimitIndex + 1) % TimeLimits.Length;
            TimeLimitSecs = TimeLimits[_timeLimitIndex];
        }
        public static void PrevTimeLimit()
        {
            _timeLimitIndex = (_timeLimitIndex - 1 + TimeLimits.Length) % TimeLimits.Length;
            TimeLimitSecs = TimeLimits[_timeLimitIndex];
        }
        public static string TimeLimitDisplay
        {
            get
            {
                if (TimeLimitSecs < 60) return TimeLimitSecs + "s";
                int m = TimeLimitSecs / 60; int s = TimeLimitSecs % 60;
                return s == 0 ? m + "m" : m + "m " + s + "s";
            }
        }

        public static float TimeRemaining { get; private set; } = 0f;
        public static int ScoreGained { get; private set; } = 0;

        // combo.score resets to 0 on each landing (committed) and each bail (lost).
        // We accumulate committed combos ourselves and add the live combo on top.
        private static int _snapshotScore = 0; // pre-run combo offset at LS click
        private static int _accumulated = 0; // committed trick score during this run
        private static int _prevRawCombo = 0; // last frame's raw combo (detects drops)
        private static int _bailCountAtRun = 0;
        private static int _bailCountFrame = 0;
        private static float _resultTimer = 0f;
        private const float ResultDuration = 4f;

        // Reflection cache
        private static VehicleTricks _tricks = null;
        private static FieldInfo _comboFld = null;
        private static FieldInfo _scoreFld = null;

        // ── Public API ────────────────────────────────────────────────
        public static void SetTarget(int score)
        {
            TargetScore = Mathf.Max(1, score);
            MelonLogger.Msg("[TrickAttack] Target=" + TargetScore);
        }

        public static void Toggle()
        {
            if (CurrentState == State.Off)
            {
                CurrentState = State.WaitingForStart;
                ScoreGained = 0;
                _accumulated = 0;
                _snapshotScore = 0;
                _prevRawCombo = 0;
            }
            else
            {
                CurrentState = State.Off;
                ScoreGained = 0;
                TimeRemaining = 0f;
            }
        }

        private static void ResetRun()
        {
            CurrentState = State.WaitingForStart;
            _accumulated = 0;
            _snapshotScore = 0;
            _prevRawCombo = 0;
            ScoreGained = 0;
            TimeRemaining = 0f;
            MelonLogger.Msg("[TrickAttack] Run cancelled — waiting for left stick.");
        }

        public static void Tick()
        {
            if (CurrentState == State.Off) return;
            float dt = Time.deltaTime;

            if (CurrentState == State.WaitingForStart)
            {
                if (Input.GetKeyDown(KeyCode.JoystickButton8))
                {
                    _snapshotScore = ReadRawComboScore();
                    _accumulated = 0;
                    _prevRawCombo = _snapshotScore;
                    _bailCountAtRun = SessionTrackers.BailCount;
                    _bailCountFrame = _bailCountAtRun;
                    TimeRemaining = TimeLimitSecs;
                    ScoreGained = 0;
                    CurrentState = State.Running;
                    MelonLogger.Msg("[TrickAttack] GO! target=" + TargetScore
                        + " time=" + TimeLimitSecs + "s snapshot=" + _snapshotScore);
                }
                return;
            }

            if (CurrentState == State.Running)
            {
                if (Input.GetKeyDown(KeyCode.JoystickButton8))
                {
                    ResetRun();
                    return;
                }

                TimeRemaining -= dt;

                int currentBails = SessionTrackers.BailCount;
                int rawCombo = ReadRawComboScore();
                int liveScore = Mathf.Max(0, rawCombo - _snapshotScore);

                if (currentBails > _bailCountFrame)
                {
                    // BAIL — lose current combo, clear everything
                    MelonLogger.Msg("[TrickAttack] Bail — clearing. lostCombo="
                        + liveScore);
                    _accumulated = 0;
                    _snapshotScore = 0;
                    _prevRawCombo = 0;
                    _bailCountFrame = currentBails;
                    liveScore = 0;
                }
                else if (rawCombo < _prevRawCombo - 20 && _prevRawCombo > 0)
                {
                    // Combo dropped significantly without a bail = LANDED
                    // Bank whatever was live before the drop
                    int banked = Mathf.Max(0, _prevRawCombo - _snapshotScore);
                    _accumulated += banked;
                    _snapshotScore = 0; // new baseline after landing
                    MelonLogger.Msg("[TrickAttack] Landed! banked=" + banked
                        + " total=" + _accumulated);
                }

                _prevRawCombo = rawCombo;
                ScoreGained = _accumulated + liveScore;

                if (TimeRemaining <= 0f)
                {
                    TimeRemaining = 0f;
                    // Use final accumulated + whatever live combo exists at buzzer
                    ScoreGained = _accumulated + liveScore;
                    CurrentState = ScoreGained >= TargetScore ? State.Success : State.Fail;
                    _resultTimer = ResultDuration;
                    MelonLogger.Msg("[TrickAttack] " + CurrentState
                        + " accumulated=" + _accumulated
                        + " live=" + liveScore
                        + " total=" + ScoreGained
                        + " target=" + TargetScore);
                }
                return;
            }

            if (CurrentState == State.Success || CurrentState == State.Fail)
            {
                _resultTimer -= dt;
                if (_resultTimer <= 0f) CurrentState = State.WaitingForStart;
            }
        }

        // ── Raw combo.score (no multiplier applied) ───────────────────
        private static int ReadRawComboScore()
        {
            try
            {
                if ((object)_tricks == null)
                {
                    GameObject player = GameObject.Find("Player_Human");
                    if ((object)player == null) return 0;
                    _tricks = player.GetComponentInChildren<VehicleTricks>();
                    if ((object)_tricks != null)
                        MelonLogger.Msg("[TrickAttack] VehicleTricks found.");
                }
                if ((object)_tricks == null) return 0;

                if ((object)_comboFld == null)
                {
                    FieldInfo[] fields = typeof(VehicleTricks).GetFields(
                        BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (string.Equals(fields[i].FieldType.Name, "Combo",
                            System.StringComparison.Ordinal))
                        { _comboFld = fields[i]; break; }
                    }
                }
                if ((object)_comboFld == null) return 0;

                object combo = _comboFld.GetValue(_tricks);
                if ((object)combo == null) return 0;

                if ((object)_scoreFld == null)
                {
                    FieldInfo[] fields = combo.GetType().GetFields(
                        BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (string.Equals(fields[i].Name, "score",
                            System.StringComparison.Ordinal))
                        { _scoreFld = fields[i]; break; }
                    }
                }
                if ((object)_scoreFld == null) return 0;

                return ReadObscuredInt(_scoreFld.GetValue(combo));
            }
            catch { return 0; }
        }

        private static int ReadObscuredInt(object val)
        {
            if ((object)val == null) return 0;
            // Try op_Implicit (ObscuredInt → int)
            try
            {
                MethodInfo op = val.GetType().GetMethod("op_Implicit",
                    BindingFlags.Static | BindingFlags.Public);
                if ((object)op != null)
                    return (int)op.Invoke(null, new object[] { val });
            }
            catch { }
            int result;
            return int.TryParse(val.ToString(), out result) ? result : 0;
        }

        public static void Reset()
        {
            CurrentState = State.Off;
            ScoreGained = 0;
            TimeRemaining = 0f;
            _resultTimer = 0f;
            _accumulated = 0;
            _snapshotScore = 0;
            _prevRawCombo = 0;
            _bailCountFrame = 0;
            _tricks = null;
            _comboFld = null;
            _scoreFld = null;
        }
    }
}