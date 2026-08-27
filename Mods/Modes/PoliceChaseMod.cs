using MelonLoader;
using DescendersModMenu;
using DescendersModMenu.BikeStats;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class PoliceChaseMode
    {
        public static bool Enabled { get; private set; } = false;

        // ── Difficulty ────────────────────────────────────────────────
        public static int Difficulty { get; private set; } = 1;

        private static readonly float[] SpeedRatio = { 0.85f, 1.25f, 1.60f };
        private static readonly float[] CatchDist = { 4f, 5f, 6f };
        private static readonly float[] BurstMult = { 1.10f, 1.40f, 1.80f };
        private static readonly float[] BurstChance = { 0.0f, 5f, 3f };
        private static readonly float[] MinSpeeds = { 15f, 24f, 32f };
        private static readonly float[] MaxSpeeds = { 35f, 55f, 75f };

        private static float ActiveSpeedRatio => SpeedRatio[Difficulty];
        private static float ActiveCatchDist => CatchDist[Difficulty];
        private static float ActiveBurstMult => BurstMult[Difficulty];
        private static float ActiveBurstCooldown => BurstChance[Difficulty];
        private static float ActiveMinSpeed => MinSpeeds[Difficulty];
        private static float ActiveMaxSpeed => MaxSpeeds[Difficulty];

        // ── Stats ─────────────────────────────────────────────────────
        public static int CaughtCount { get; private set; } = 0;
        public static bool IsCaught { get; private set; } = false;
        public static bool WaitingForReset { get; private set; } = false;
        public static bool IsBursting { get; private set; } = false;

        /// <summary>Seconds survived this chase (not counting countdown / wait).</summary>
        public static float CurrentRunTime { get; private set; } = 0f;
        public static float BestTimeEasy { get; private set; } = 0f;
        public static float BestTimeMedium { get; private set; } = 0f;
        public static float BestTimeHard { get; private set; } = 0f;

        public static float BestTimeForDifficulty
        {
            get
            {
                if (Difficulty == 0) return BestTimeEasy;
                if (Difficulty == 2) return BestTimeHard;
                return BestTimeMedium;
            }
        }

        private static float _stuckTimer = 0f;
        private static float _progressTimer = 0f;
        private static float _lastDistToPlayer = 999f;

        public static bool IsCountingDown { get; private set; } = false;
        public static float CountdownRemaining { get; private set; } = 0f;
        private const float CountdownDuration = 5f;

        private static float _caughtTimer = 0f;
        private const float CaughtDuration = 2.5f;

        // ── Burst system ──────────────────────────────────────────────
        private static float _burstTimer = 0f;
        private static float _burstCooldown = 0f;

        // ── Crash system ──────────────────────────────────────────────

        // ── Pursuer ball ──────────────────────────────────────────────
        private static GameObject _ball = null;
        private static Rigidbody _ballRb = null;
        private static Material _ballMat = null;
        private static float _flashTimer = 0f;
        private static bool _flashRed = true;
        private static AudioSource _sirenSrc = null;
        private static AudioClip _sirenClip = null;

        private static readonly Color ColRed = new Color(1f, 0f, 0f, 1f);
        private static readonly Color ColBlue = new Color(0f, 0.25f, 1f, 1f);

        // ── Player cache ──────────────────────────────────────────────
        private static GameObject _player = null;
        private static Rigidbody _playerRb = null;
        private static Cyclist _cyclist = null;
        private static Vector3 _lastPlayerPos = Vector3.zero;
        private static bool _hasLastPos = false;
        private static float _bailCooldown = 0f;
        private const float BailCooldownDur = 1.5f;
        private const float SpawnDistance = 35f;

        // ── Accessors for HUD ─────────────────────────────────────────
        public static float PursuerDistance
        {
            get
            {
                if (!Enabled || !UnityNull.Alive(_ball) || !UnityNull.Alive(_player))
                    return -1f;
                return Vector3.Distance(_player.transform.position,
                                        _ball.transform.position);
            }
        }

        public static float PlayerSpeedMs
        {
            get
            {
                if (!UnityNull.Alive(_playerRb)) return 0f;
                return _playerRb.velocity.magnitude;
            }
        }

        public static string DifficultyName
        {
            get
            {
                if (Difficulty == 0) return "Easy";
                if (Difficulty == 2) return "Hard";
                return "Medium";
            }
        }

        // ── Public API ────────────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled)
            {
                CaughtCount = 0;
                IsCaught = false;
                WaitingForReset = false;
                CurrentRunTime = 0f;
                IsCountingDown = true;
                CountdownRemaining = CountdownDuration;
                _hasLastPos = false;
                _burstCooldown = Random.Range(5f, 12f);
                SpawnBall();
            }
            else
            {
                IsCountingDown = false;
                StopSiren();
                DestroyBall();
                _player = null;
                _playerRb = null;
                _cyclist = null;
            }
            ModLog.Debug("[PoliceChase] " + (Enabled ? "ON - counting down" : "OFF")
                + " difficulty=" + DifficultyName);
        }

        public static void SetDifficulty(int d)
        {
            Difficulty = Mathf.Clamp(d, 0, 2);
            ModLog.Feedback("[PoliceChase] Difficulty -> " + DifficultyName);
        }

        public static void ManualReset()
        {
            if (!Enabled) return;
            WaitingForReset = false;
            IsCaught = false;
            CurrentRunTime = 0f;
            _bailCooldown = 0f;
            _stuckTimer = 0f;
            _progressTimer = 0f;
            _lastDistToPlayer = 999f;
            IsCountingDown = true;
            CountdownRemaining = CountdownDuration;
            ResetBallBehindPlayer();
            EnsureSiren();
        }

        public static void ApplyBestTimes(float easy, float medium, float hard)
        {
            BestTimeEasy = Mathf.Max(0f, easy);
            BestTimeMedium = Mathf.Max(0f, medium);
            BestTimeHard = Mathf.Max(0f, hard);
        }

        public static string FormatTime(float seconds)
        {
            if (seconds <= 0.01f) return "--:--.--";
            int m = (int)(seconds / 60f);
            float s = seconds - m * 60f;
            return m.ToString("D2") + ":" + s.ToString("00.00");
        }

        // ── Tick — OnUpdate ───────────────────────────────────────────
        public static void Tick()
        {
            if (!Enabled) return;

            float dt = Time.deltaTime;

            if (WaitingForReset)
            {
                bool stickReset = false;
                try { stickReset = Input.GetKeyDown(KeyCode.JoystickButton8); } catch { }
                if (Input.GetKeyDown(KeyCode.F5) || stickReset)
                    ManualReset();
            }
            else if (!IsCountingDown && !IsCaught)
            {
                CurrentRunTime += dt;
            }

            if (!UnityNull.Alive(_player))
            {
                _player = GameObject.Find("Player_Human");
                _playerRb = null;
                _cyclist = null;
                if (UnityNull.Alive(_player))
                {
                    _playerRb = _player.GetComponentInChildren<Rigidbody>();
                    Cyclist[] cyclists = UnityEngine.Object.FindObjectsOfType<Cyclist>();
                    for (int i = 0; i < cyclists.Length; i++)
                    {
                        if (string.Equals(cyclists[i].gameObject.name, "Player_Human",
                            System.StringComparison.Ordinal))
                        { _cyclist = cyclists[i]; break; }
                    }
                }
                else
                    _player = null;
                _hasLastPos = false;
            }
            if (!UnityNull.Alive(_player)) return;

            Vector3 pos = _player.transform.position;
            if (_hasLastPos && Vector3.Distance(pos, _lastPlayerPos) > 20f)
            {
                ResetBallBehindPlayer();
                ModLog.Debug("[PoliceChase] Respawn detected — pursuer repositioned.");
            }
            _lastPlayerPos = pos;
            _hasLastPos = true;

            if (_bailCooldown > 0f) _bailCooldown -= dt;

            if (IsCountingDown)
            {
                CountdownRemaining -= dt;
                if (CountdownRemaining <= 0f)
                {
                    IsCountingDown = false;
                    CurrentRunTime = 0f;
                    ModLog.Debug("[PoliceChase] GO!");
                }
                UpdateSiren(999f);
                return;
            }

            if (IsCaught)
            {
                _caughtTimer -= dt;
                if (_caughtTimer <= 0f) IsCaught = false;
            }

            if (!UnityNull.Alive(_ball))
            {
                _ball = null;
                _ballRb = null;
                SpawnBall();
                return;
            }

            _flashTimer -= dt;
            if (_flashTimer <= 0f)
            {
                _flashTimer = 0.28f;
                _flashRed = !_flashRed;
                ApplyBallColor(_flashRed ? ColRed : ColBlue);
            }

            float distToPlayer = Vector3.Distance(pos, _ball.transform.position);
            UpdateSiren(WaitingForReset ? 999f : distToPlayer);

            if (!WaitingForReset && _bailCooldown <= 0f
                && distToPlayer <= ActiveCatchDist)
            {
                TriggerCaught();
            }
        }

        public static void FixedTick()
        {
            if (!Enabled || WaitingForReset || IsCountingDown) return;
            if (!UnityNull.Alive(_ball) || !UnityNull.Alive(_ballRb))
            {
                _ball = null;
                _ballRb = null;
                return;
            }
            if (!UnityNull.Alive(_player)) return;

            float dt = Time.fixedDeltaTime;

            // ── Burst timer ────────────────────────────────────────────
            float burstMultiplier = 1f;
            if (Difficulty > 0)
            {
                if (IsBursting)
                {
                    _burstTimer -= dt;
                    burstMultiplier = ActiveBurstMult;
                    if (_burstTimer <= 0f)
                    {
                        IsBursting = false;
                        _burstCooldown = Random.Range(ActiveBurstCooldown,
                                                      ActiveBurstCooldown * 2f);
                    }
                }
                else
                {
                    _burstCooldown -= dt;
                    if (_burstCooldown <= 0f)
                    {
                        IsBursting = true;
                        _burstTimer = Random.Range(2f, 4f);
                        ModLog.Debug("[PoliceChase] Burst!");
                    }
                }
            }

            float playerSpeed = UnityNull.Alive(_playerRb)
                ? _playerRb.velocity.magnitude : 10f;
            float targetSpeed = Mathf.Clamp(
                playerSpeed * ActiveSpeedRatio * burstMultiplier,
                ActiveMinSpeed, ActiveMaxSpeed);

            Vector3 toPlayer = _player.transform.position - _ball.transform.position;
            float dist = toPlayer.magnitude;

            if (dist > 0.5f)
            {
                Vector3 steerDir = GetSteeringDirection(toPlayer.normalized);
                Vector3 desiredVel = steerDir * targetSpeed;
                float accel = 70f;
                Vector3 curVel = _ballRb.velocity;
                Vector3 newVel = Vector3.MoveTowards(
                    new Vector3(curVel.x, curVel.y, curVel.z),
                    desiredVel, accel * Time.fixedDeltaTime);

                float yVel = _ballRb.velocity.y;
                if (_inHole)
                    yVel = Mathf.Max(yVel, targetSpeed * 0.5f);

                _ballRb.velocity = new Vector3(newVel.x, yVel, newVel.z);
            }

            _progressTimer += Time.fixedDeltaTime;
            if (_progressTimer >= 1f)
            {
                _progressTimer = 0f;
                float gained = _lastDistToPlayer - dist;
                float xzSpeed = new Vector2(_ballRb.velocity.x, _ballRb.velocity.z).magnitude;
                bool stuckNow = gained < 1f && xzSpeed < 2f;

                if (stuckNow && dist > ActiveCatchDist * 2f)
                    _stuckTimer += 1f;
                else
                    _stuckTimer = 0f;

                if (_stuckTimer >= 2f && _stuckTimer < 3f)
                {
                    _ballRb.velocity = Vector3.zero;
                    Vector3 escapeDir = (toPlayer.normalized + Vector3.up * 1.8f).normalized;
                    _ballRb.AddForce(escapeDir * 35f, ForceMode.Impulse);
                    ModLog.Debug("[PoliceChase] Escape jump fired.");
                }

                if (_stuckTimer >= 3f)
                {
                    _stuckTimer = 0f;
                    Vector3 respawnPos = _player.transform.position
                        - _player.transform.forward * 70f
                        + Vector3.up * 50f;
                    _ball.transform.position = respawnPos;
                    _ballRb.velocity = Vector3.zero;
                    _ballRb.angularVelocity = Vector3.zero;
                    ModLog.Debug("[PoliceChase] Genuinely stuck — respawned above player.");
                }
                _lastDistToPlayer = dist;
            }
        }

        private static bool _inHole = false;

        private static Vector3 GetSteeringDirection(Vector3 primaryDir)
        {
            Vector3 ballPos = _ball.transform.position + Vector3.up * 0.5f;
            float lookDist = 15f;

            Vector3 flat = new Vector3(primaryDir.x, 0f, primaryDir.z).normalized;
            Vector3 right = new Vector3(flat.z, 0f, -flat.x);

            float[] angles = { 0f, -20f, 20f, -40f, 40f, -60f, 60f, -80f, 80f };
            float bestScore = float.MinValue;
            Vector3 bestDir = flat;
            int blocked = 0;

            for (int i = 0; i < angles.Length; i++)
            {
                float rad = angles[i] * Mathf.Deg2Rad;
                Vector3 candidate = (flat * Mathf.Cos(rad)
                                   + right * Mathf.Sin(rad)).normalized;

                RaycastHit h;
                float clearance = Physics.Raycast(ballPos, candidate, out h, lookDist)
                    ? h.distance : lookDist;

                if (clearance < lookDist * 0.5f) blocked++;

                float alignment = Vector3.Dot(candidate, flat);
                float score = clearance * 3f + alignment * 1f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = candidate;
                }
            }

            bool aboveClear = !Physics.Raycast(ballPos, Vector3.up, lookDist * 0.5f);
            _inHole = blocked >= 6 && aboveClear;

            return bestDir;
        }

        // ── Caught ────────────────────────────────────────────────────
        private static void TriggerCaught()
        {
            CaughtCount++;
            IsCaught = true;
            _caughtTimer = CaughtDuration;
            _bailCooldown = BailCooldownDur;
            WaitingForReset = true;

            float run = CurrentRunTime;
            bool newBest = false;
            if (Difficulty == 0 && run > BestTimeEasy) { BestTimeEasy = run; newBest = true; }
            else if (Difficulty == 2 && run > BestTimeHard) { BestTimeHard = run; newBest = true; }
            else if (Difficulty == 1 && run > BestTimeMedium) { BestTimeMedium = run; newBest = true; }

            if (newBest)
            {
                ModLog.Feedback("[PoliceChase] New best (" + DifficultyName + "): " + FormatTime(run));
                PersistBestTimes();
            }
            else
                ModLog.Feedback("[PoliceChase] Caught at " + FormatTime(run)
                    + " — F5 / LS Click to restart");

            if (UnityNull.Alive(_cyclist))
            {
                try { _cyclist.Bail(); }
                catch (System.Exception ex)
                { MelonLogger.Error("[PoliceChase] Bail failed: " + ex.Message); Telemetry.ReportErrorAsync(ex, "PoliceChaseMod"); }
            }

            if (UnityNull.Alive(_playerRb))
                _playerRb.velocity = Vector3.zero;

            UpdateSiren(999f);
        }

        private static void ApplyBallColor(Color c)
        {
            if ((object)_ballMat == null) return;
            _ballMat.color = c;
            _ballMat.SetColor("_Color", c);
            _ballMat.SetColor("_EmissionColor", c * 2.2f);
            _ballMat.EnableKeyword("_EMISSION");
        }

        private static void EnsureSiren()
        {
            try
            {
                if (!UnityNull.Alive(_ball)) return;
                if ((object)_sirenClip == null) _sirenClip = BuildSirenClip();
                if (!UnityNull.Alive(_sirenSrc))
                {
                    _sirenSrc = _ball.GetComponent<AudioSource>();
                    if ((object)_sirenSrc == null)
                        _sirenSrc = _ball.AddComponent<AudioSource>();
                }
                _sirenSrc.clip = _sirenClip;
                _sirenSrc.loop = true;
                _sirenSrc.playOnAwake = false;
                _sirenSrc.spatialBlend = 0f;
                _sirenSrc.volume = 0f;
                if (!_sirenSrc.isPlaying) _sirenSrc.Play();
            }
            catch (System.Exception ex)
            { ModLog.Warn("[PoliceChase] Siren: " + ex.Message); }
        }

        private static void UpdateSiren(float dist)
        {
            try
            {
                EnsureSiren();
                if (!UnityNull.Alive(_sirenSrc)) return;
                if (WaitingForReset || IsCountingDown || dist > 80f)
                {
                    _sirenSrc.volume = Mathf.MoveTowards(_sirenSrc.volume, 0f, Time.deltaTime * 1.5f);
                    return;
                }
                // Louder as it closes — ~0 at 70m, full near catch range.
                float t = 1f - Mathf.Clamp01((dist - ActiveCatchDist) / 65f);
                float target = Mathf.Lerp(0.08f, 0.85f, t * t);
                _sirenSrc.volume = Mathf.MoveTowards(_sirenSrc.volume, target, Time.deltaTime * 2f);
            }
            catch { }
        }

        private static void StopSiren()
        {
            try
            {
                if (UnityNull.Alive(_sirenSrc))
                {
                    _sirenSrc.Stop();
                    _sirenSrc.volume = 0f;
                }
            }
            catch { }
            _sirenSrc = null;
        }

        private static AudioClip BuildSirenClip()
        {
            const int sampleRate = 22050;
            const float duration = 1.2f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                // Two-tone wail (hi-lo) like a basic siren.
                float phase = (t % 1.2f) / 1.2f;
                float freq = phase < 0.5f
                    ? Mathf.Lerp(680f, 920f, phase * 2f)
                    : Mathf.Lerp(920f, 680f, (phase - 0.5f) * 2f);
                float env = 0.55f;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.45f;
            }
            AudioClip clip = AudioClip.Create("PoliceSiren", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void PersistBestTimes()
        {
            try
            {
                StatsManager.PersistPoliceBestTimes(
                    BestTimeEasy, BestTimeMedium, BestTimeHard);
            }
            catch (System.Exception ex)
            { ModLog.Warn("[PoliceChase] PersistBestTimes: " + ex.Message); }
        }

        // ── Ball spawning ─────────────────────────────────────────────
        private static void SpawnBall()
        {
            if (UnityNull.Alive(_ball)) return;
            _ball = null;
            _ballRb = null;
            _sirenSrc = null;

            _ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _ball.name = "PolicePursuer";
            _ball.transform.localScale = new Vector3(2f, 2f, 2f);

            var col = _ball.GetComponent<SphereCollider>();
            if ((object)col != null)
            {
                col.isTrigger = false;
                col.material = new PhysicMaterial
                {
                    dynamicFriction = 0.4f,
                    staticFriction = 0.4f,
                    bounciness = 0.2f,
                    frictionCombine = PhysicMaterialCombine.Average,
                    bounceCombine = PhysicMaterialCombine.Minimum
                };
            }

            _ballRb = _ball.AddComponent<Rigidbody>();
            _ballRb.useGravity = true;
            _ballRb.drag = 1.5f;
            _ballRb.angularDrag = 0.8f;
            _ballRb.constraints = RigidbodyConstraints.None;

            Shader sh = Shader.Find("Unlit/Color");
            if ((object)sh == null) sh = Shader.Find("Standard");
            _ballMat = new Material(sh);
            ApplyBallColor(ColRed);
            var mr = _ball.GetComponent<MeshRenderer>();
            if ((object)mr != null) mr.material = _ballMat;

            _flashRed = true;
            _flashTimer = 0f;
            IsBursting = false;
            _burstCooldown = Random.Range(5f, 12f);

            ResetBallBehindPlayer();
            EnsureSiren();
        }

        private static void ResetBallBehindPlayer()
        {
            if (!UnityNull.Alive(_ball)) return;
            if (!UnityNull.Alive(_player))
                _player = GameObject.Find("Player_Human");
            if (!UnityNull.Alive(_player)) return;

            Vector3 spawnPos = _player.transform.position
                + Vector3.up * 50f
                - _player.transform.forward * SpawnDistance;
            _ball.transform.position = spawnPos;
            if (UnityNull.Alive(_ballRb))
            {
                _ballRb.velocity = Vector3.zero;
                _ballRb.angularVelocity = Vector3.zero;
            }
        }

        private static void DestroyBall()
        {
            StopSiren();
            if ((object)_ball != null)
            {
                UnityEngine.Object.Destroy(_ball);
                _ball = null;
                _ballRb = null;
                _ballMat = null;
            }
        }

        public static void Reset()
        {
            Enabled = false;
            CaughtCount = 0;
            IsCaught = false;
            WaitingForReset = false;
            IsBursting = false;
            IsCountingDown = false;
            CountdownRemaining = 0f;
            CurrentRunTime = 0f;
            _caughtTimer = 0f;
            _flashTimer = 0f;
            _bailCooldown = 0f;
            _burstTimer = 0f;
            _burstCooldown = 0f;
            _stuckTimer = 0f;
            _progressTimer = 0f;
            _lastDistToPlayer = 999f;
            _hasLastPos = false;
            _player = null;
            _playerRb = null;
            _cyclist = null;
            DestroyBall();
        }
    }
}

