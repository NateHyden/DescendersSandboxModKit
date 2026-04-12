using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class PoliceChaseMode
    {
        public static bool Enabled { get; private set; } = false;

        // ── Difficulty ────────────────────────────────────────────────
        // 0=Easy  1=Medium  2=Hard
        public static int Difficulty { get; private set; } = 1;

        // Speed ratio relative to player speed per difficulty
        // Easy = manageable, Medium = faster than player, Hard = relentless
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

        // Stuck detection
        private static float _stuckTimer = 0f;
        private static float _progressTimer = 0f;
        private static float _lastDistToPlayer = 999f;

        // Countdown before chase starts
        public static bool IsCountingDown { get; private set; } = false;
        public static float CountdownRemaining { get; private set; } = 0f;
        private const float CountdownDuration = 5f;

        private static float _caughtTimer = 0f;
        private const float CaughtDuration = 2.5f;

        // ── Burst system ──────────────────────────────────────────────
        private static float _burstTimer = 0f;
        private static float _burstCooldown = 0f;

        // ── Crash system ──────────────────────────────────────────────
        // If pursuer speed drops below this fraction of target, it "crashed"

        // ── Pursuer ball ──────────────────────────────────────────────
        private static GameObject _ball = null;
        private static Rigidbody _ballRb = null;
        private static Material _ballMat = null;
        private static float _flashTimer = 0f;
        private static bool _flashRed = true;

        private static readonly Color ColRed = new Color(1f, 0.04f, 0.04f, 1f);
        private static readonly Color ColBlue = new Color(0.08f, 0.2f, 1f, 1f);

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
                if (!Enabled || (object)_ball == null || (object)_player == null)
                    return -1f;
                return Vector3.Distance(_player.transform.position,
                                        _ball.transform.position);
            }
        }

        public static float PlayerSpeedMs
        {
            get
            {
                if ((object)_playerRb == null) return 0f;
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
                IsCountingDown = true;
                CountdownRemaining = CountdownDuration;
                _hasLastPos = false;
                _burstCooldown = Random.Range(5f, 12f);
                SpawnBall();
            }
            else
            {
                IsCountingDown = false;
                DestroyBall();
                _player = null;
                _playerRb = null;
                _cyclist = null;
            }
            MelonLogger.Msg("[PoliceChase] " + (Enabled ? "ON - counting down" : "OFF")
                + " difficulty=" + DifficultyName);
        }

        public static void SetDifficulty(int d)
        {
            Difficulty = Mathf.Clamp(d, 0, 2);
            MelonLogger.Msg("[PoliceChase] Difficulty -> " + DifficultyName);
        }

        public static void ManualReset()
        {
            if (!Enabled) return;
            WaitingForReset = false;
            _bailCooldown = 0f;
            _stuckTimer = 0f;
            _progressTimer = 0f;
            _lastDistToPlayer = 999f;
            ResetBallBehindPlayer();
        }

        // ── Tick — OnUpdate ───────────────────────────────────────────
        public static void Tick()
        {
            if (!Enabled) return;

            float dt = Time.deltaTime;

            // F5 reset
            if (WaitingForReset && Input.GetKeyDown(KeyCode.F5))
                ManualReset();

            // Find/cache player
            if ((object)_player == null)
            {
                _player = GameObject.Find("Player_Human");
                if ((object)_player != null)
                {
                    _playerRb = _player.GetComponentInChildren<Rigidbody>();
                    // Find Cyclist on Player_Human reliably
                    Cyclist[] cyclists = UnityEngine.Object.FindObjectsOfType<Cyclist>();
                    for (int i = 0; i < cyclists.Length; i++)
                    {
                        if (string.Equals(cyclists[i].gameObject.name, "Player_Human",
                            System.StringComparison.Ordinal))
                        { _cyclist = cyclists[i]; break; }
                    }
                }
                _hasLastPos = false;
            }
            if ((object)_player == null) return;

            // Respawn detection
            Vector3 pos = _player.transform.position;
            if (_hasLastPos && Vector3.Distance(pos, _lastPlayerPos) > 20f)
            {
                ResetBallBehindPlayer();
                MelonLogger.Msg("[PoliceChase] Respawn detected — pursuer repositioned.");
            }
            _lastPlayerPos = pos;
            _hasLastPos = true;

            if (_bailCooldown > 0f) _bailCooldown -= dt;

            // Countdown before chase starts
            if (IsCountingDown)
            {
                CountdownRemaining -= dt;
                if (CountdownRemaining <= 0f)
                {
                    IsCountingDown = false;
                    MelonLogger.Msg("[PoliceChase] GO!");
                }
                return; // ball doesn't move during countdown
            }

            // CAUGHT timer
            if (IsCaught)
            {
                _caughtTimer -= dt;
                if (_caughtTimer <= 0f) IsCaught = false;
            }

            if ((object)_ball == null) { SpawnBall(); return; }

            // Flash ball
            _flashTimer -= dt;
            if (_flashTimer <= 0f)
            {
                _flashTimer = 0.35f;
                _flashRed = !_flashRed;
                if ((object)_ballMat != null)
                {
                    Color c = _flashRed ? ColRed : ColBlue;
                    _ballMat.color = c;
                    _ballMat.SetColor("_EmissionColor", c * 0.8f);
                }
            }

            // Catch check
            if (!WaitingForReset && _bailCooldown <= 0f
                && Vector3.Distance(pos, _ball.transform.position) <= ActiveCatchDist)
            {
                TriggerCaught();
            }
        }

        // ── FixedTick — physics drive ─────────────────────────────────
        public static void FixedTick()
        {
            if (!Enabled || WaitingForReset || IsCountingDown) return;
            if ((object)_ball == null || (object)_player == null) return;

            float dt = Time.fixedDeltaTime;

            // ── Burst timer ────────────────────────────────────────────
            float burstMultiplier = 1f;
            if (Difficulty > 0) // Easy has no bursts
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
                        MelonLogger.Msg("[PoliceChase] Burst!");
                    }
                }
            }

            // ── Target speed tied to player speed ─────────────────────
            float playerSpeed = (object)_playerRb != null
                ? _playerRb.velocity.magnitude : 10f;
            float targetSpeed = Mathf.Clamp(
                playerSpeed * ActiveSpeedRatio * burstMultiplier,
                ActiveMinSpeed, ActiveMaxSpeed);

            // ── Steer toward player with obstacle avoidance ───────────
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

                // Only push Y upward if the ball is in a hole (trapped flag set by radar)
                float yVel = _ballRb.velocity.y;
                if (_inHole)
                    yVel = Mathf.Max(yVel, targetSpeed * 0.5f);

                _ballRb.velocity = new Vector3(newVel.x, yVel, newVel.z);
            }

            // ── Progress-based stuck detection ─────────────────────────
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

                // At 2s: fire a large escape jump toward the player before giving up
                if (_stuckTimer >= 2f && _stuckTimer < 3f)
                {
                    _ballRb.velocity = Vector3.zero;
                    Vector3 escapeDir = (toPlayer.normalized + Vector3.up * 1.8f).normalized;
                    _ballRb.AddForce(escapeDir * 35f, ForceMode.Impulse);
                    MelonLogger.Msg("[PoliceChase] Escape jump fired.");
                }

                // At 3s: still stuck — respawn from above
                if (_stuckTimer >= 3f)
                {
                    _stuckTimer = 0f;
                    Vector3 respawnPos = _player.transform.position
                        - _player.transform.forward * 70f
                        + Vector3.up * 50f;
                    _ball.transform.position = respawnPos;
                    _ballRb.velocity = Vector3.zero;
                    _ballRb.angularVelocity = Vector3.zero;
                    MelonLogger.Msg("[PoliceChase] Genuinely stuck — respawned above player.");
                }
                _lastDistToPlayer = dist;
            }
        }

        private static bool _inHole = false; // set by radar when all horizontal paths blocked

        // ── Predictive radar steering ─────────────────────────────────
        private static Vector3 GetSteeringDirection(Vector3 primaryDir)
        {
            Vector3 ballPos = _ball.transform.position + Vector3.up * 0.5f;
            float lookDist = 15f;

            // Work entirely in the horizontal plane — no upBias on normal candidates
            Vector3 flat = new Vector3(primaryDir.x, 0f, primaryDir.z).normalized;
            Vector3 right = new Vector3(flat.z, 0f, -flat.x);

            float[] angles = { 0f, -20f, 20f, -40f, 40f, -60f, 60f, -80f, 80f };
            float bestScore = float.MinValue;
            Vector3 bestDir = flat;
            int blocked = 0;

            for (int i = 0; i < angles.Length; i++)
            {
                float rad = angles[i] * Mathf.Deg2Rad;
                // Pure horizontal candidate — no upward bias
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

            // Hole detection: if most horizontal directions are blocked but above is clear,
            // the ball is in a concave trap — set the _inHole flag so we push Y upward
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

            // Force bail
            if ((object)_cyclist != null)
            {
                try { _cyclist.Bail(); }
                catch (System.Exception ex)
                { MelonLogger.Error("[PoliceChase] Bail failed: " + ex.Message); }
            }

            // Zero out player rigidbody velocity for a hard stop feel
            if ((object)_playerRb != null)
                _playerRb.velocity = Vector3.zero;

            MelonLogger.Msg("[PoliceChase] CAUGHT! Total=" + CaughtCount);
        }

        // ── Ball spawning ─────────────────────────────────────────────
        private static void SpawnBall()
        {
            if ((object)_ball != null) return;

            _ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _ball.name = "PolicePursuer";
            _ball.transform.localScale = new Vector3(2f, 2f, 2f);

            // Keep collider — makes it interact with terrain naturally
            // Set layer to Default so it hits terrain but not the player bike
            // (player bike is typically on a different layer)
            var col = _ball.GetComponent<SphereCollider>();
            if ((object)col != null)
            {
                col.isTrigger = false; // real collision with terrain = natural slowdown
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
            _ballRb.useGravity = true;   // gravity ON — follows terrain slope
            _ballRb.drag = 1.5f;   // natural deceleration
            _ballRb.angularDrag = 0.8f;
            _ballRb.constraints = RigidbodyConstraints.None; // can roll freely

            _ballMat = new Material(Shader.Find("Standard"));
            _ballMat.color = ColRed;
            _ballMat.EnableKeyword("_EMISSION");
            _ballMat.SetColor("_EmissionColor", ColRed * 0.8f);
            var mr = _ball.GetComponent<MeshRenderer>();
            if ((object)mr != null) mr.material = _ballMat;

            _flashRed = true;
            _flashTimer = 0f;
            IsBursting = false;
            _burstCooldown = Random.Range(5f, 12f);

            ResetBallBehindPlayer();
        }

        private static void ResetBallBehindPlayer()
        {
            if ((object)_ball == null) return;
            if ((object)_player == null)
                _player = GameObject.Find("Player_Human");
            if ((object)_player == null) return;

            // Spawn ABOVE and BEHIND the player — gravity drops it onto terrain.
            // Spawning directly behind using transform.forward risks embedding in slope.
            Vector3 spawnPos = _player.transform.position
                + Vector3.up * 50f                                // drop from 50m above
                - _player.transform.forward * SpawnDistance;      // behind the player
            _ball.transform.position = spawnPos;
            if ((object)_ballRb != null)
            {
                _ballRb.velocity = Vector3.zero;
                _ballRb.angularVelocity = Vector3.zero;
            }
        }

        private static void DestroyBall()
        {
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