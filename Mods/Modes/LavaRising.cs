using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    // Modes mini-game: teleport to the lowest sampled ground, countdown,
    // then race a rising lava plane to the map's highest sampled point.
    //
    // Kill check is height-based (player Y vs lava Y) — no collider.
    // Rise rate is derived from this map's measured height range so
    // Easy/Normal/Hard/Insane feel similar on short parks and long worlds.
    // Countdown + rise use unscaled time so Slow Motion doesn't stall lava.
    public static class LavaRising
    {
        public static bool Enabled { get; private set; }
        public static bool PausedByAllMods { get; private set; }

        // 1=Easy 2=Normal 3=Hard 4=Insane. 0 from old JSON → treated as Normal.
        public static int DifficultyLevel { get; private set; } = 2;

        public static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard", "Insane" };
        private static readonly float[] ClimbSeconds = { 140f, 90f, 60f, 32f };

        public enum Phase
        {
            Off,
            Countdown,
            Rising,
            Caught,
            Won
        }

        public static Phase CurrentPhase { get; private set; } = Phase.Off;
        public static float CountdownRemaining { get; private set; }
        public static float ClimbTime { get; private set; }
        public static float LastWinTime { get; private set; }
        public static int Attempts { get; private set; }
        public static float LavaY { get { return _lavaY; } }
        public static float MinY { get { return _minY; } }
        public static float MaxY { get { return _maxY; } }
        public static float RiseRate { get { return _riseRate; } }
        public static bool LavaArmed { get { return _lavaArmed; } }
        public static float CurrentMeters { get; private set; }
        public static float RecordMeters { get { return GetRecord(CurrentMapKey()); } }
        public static bool HasSummit { get { return _hasSummit; } }

        public static string DifficultyName
        {
            get
            {
                int i = DifficultyLevel - 1;
                if (i < 0 || i >= DifficultyNames.Length) return "Normal";
                return DifficultyNames[i];
            }
        }

        public static string ClimbTimeDisplay
        {
            get
            {
                int i = DifficultyLevel - 1;
                if (i < 0 || i >= ClimbSeconds.Length) i = 1;
                return ((int)ClimbSeconds[i]).ToString() + "s";
            }
        }

        private const float CountdownSeconds = 5f;
        private const float GoHoldSeconds = 0.45f;
        private const float CaughtHoldSeconds = 2.5f;
        private const float SummitHoldSeconds = 2.5f;
        private const float SpawnLift = 1.5f;
        private const float KillMargin = 0.75f;
        private const float WinMargin = 3.0f;
        // Lava sits this far under the lowest scanned ground so it starts
        // in the void, not in the valley you're standing in.
        private const float LavaBelowMap = 120f;
        // Climb this many metres above spawn before lava starts moving.
        private const float MinArmClimb = 10f;
        // When lava arms, snap the plane up to this far below the player so
        // you see it within seconds (not a minute from the void floor).
        private const float LavaArmBelowPlayer = 22f;
        private const float KillGraceSeconds = 1.5f;
        private const int TerrainGrid = 48;
        private const int TerrainInset = 8; // skip outer ~17% — that's the map rim / void
        private const int RaycastGrid = 16;
        private const float LavaThickness = 0.45f;

        private static float _minY;
        private static float _maxY;
        private static float _floorY;
        private static float _minX;
        private static float _maxX;
        private static float _minZ;
        private static float _maxZ;
        private static Vector3 _spawnPos;
        private static Vector3 _summitPos;
        private static bool _hasSummit;
        private static float _lavaY;
        private static float _baseRate;
        private static float _riseRate;
        private static float _caughtTimer;
        private static float _killGrace;
        private static bool _lavaArmed;
        private static GameObject _lavaGo;
        private static Material _lavaMat;
        private static Texture2D _lavaTex;
        private static Vector2 _uvOffset;

        private struct ScanPt
        {
            public float x, y, z;
            public bool inland;
        }

        private static readonly List<ScanPt> _scan = new List<ScanPt>(2048);
        private static readonly List<string> _recMaps = new List<string>();
        private static readonly List<float> _recMeters = new List<float>();
        private static bool _recordsLoaded;
        private static readonly string RecordsFolder =
            Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData"), "DescendersModMenu");
        private static readonly string RecordsFile =
            Path.Combine(RecordsFolder, "LavaRisingRecords.txt");

        public static void SetDifficulty(int level)
        {
            if (level < 1) level = 1;
            if (level > 4) level = 4;
            DifficultyLevel = level;
        }

        public static void CycleDifficulty(int dir)
        {
            int next = DifficultyLevel + dir;
            if (next < 1) next = 4;
            if (next > 4) next = 1;
            SetDifficulty(next);
            ModLog.Feedback("[LavaRising] Difficulty -> " + DifficultyName + " (" + ClimbTimeDisplay + ")");
        }

        public static void Toggle()
        {
            if (Enabled) Stop(false);
            else Start();
        }

        // All Mods OFF — freeze the run without tearing down map scan / progress.
        public static void PauseForAllMods()
        {
            if (!Enabled || PausedByAllMods) return;
            PausedByAllMods = true;
            if (UnityNull.Alive(_lavaGo)) _lavaGo.SetActive(false);
        }

        // All Mods ON — resume the same attempt from where it was paused.
        public static void ResumeFromAllMods()
        {
            if (!Enabled || !PausedByAllMods) return;
            PausedByAllMods = false;
            if (!UnityNull.Alive(_lavaGo)
                && CurrentPhase != Phase.Off
                && CurrentPhase != Phase.Won
                && CurrentPhase != Phase.Caught)
            {
                CreateLava();
                PlaceLava();
            }
            if (UnityNull.Alive(_lavaGo)) _lavaGo.SetActive(true);
        }

        private static void Start()
        {
            try
            {
                DisableConflicts();

                if (!ScanMap())
                {
                    MelonLogger.Error("[LavaRising] Height scan failed — aborting.");
                    Telemetry.ReportErrorAsync(new System.Exception("LavaRising height scan failed"), "LavaRising");
                    return;
                }

                GameObject player = GameObject.Find("Player_Human");
                if (!UnityNull.Alive(player))
                {
                    MelonLogger.Error("[LavaRising] Player_Human not found — aborting.");
                    Telemetry.ReportErrorAsync(new System.Exception("LavaRising Player_Human missing"), "LavaRising");
                    return;
                }

                if (!TeleportToSpawn())
                {
                    MelonLogger.Error("[LavaRising] Teleport to spawn failed — aborting.");
                    return;
                }

                CreateLava();
                BeginAttempt();
                Enabled = true;
                ModLog.Feedback("[LavaRising] ON — " + DifficultyName
                    + "  range=" + (_maxY - _minY).ToString("F0") + "m"
                    + "  rate=" + _baseRate.ToString("F2") + "m/s (unscaled)"
                    + "  lavaY=" + _lavaY.ToString("F0")
                    + "  (" + (_spawnPos.y - _lavaY).ToString("F0") + "m below spawn)");
                ModLog.Debug("[LavaRising] Spawn=" + _spawnPos
                    + " minY=" + _minY.ToString("F1")
                    + " maxY=" + _maxY.ToString("F1")
                    + " xz=(" + _minX.ToString("F0") + ".." + _maxX.ToString("F0")
                    + ", " + _minZ.ToString("F0") + ".." + _maxZ.ToString("F0") + ")");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[LavaRising] Start: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "LavaRising");
                Stop(false);
            }
        }

        private static void BeginAttempt()
        {
            Attempts++;
            CurrentPhase = Phase.Countdown;
            CountdownRemaining = CountdownSeconds;
            ClimbTime = 0f;
            _caughtTimer = 0f;
            _killGrace = 0f;
            _lavaArmed = false;
            CurrentMeters = 0f;
            RecalcRate();
            _lavaY = LavaStartY();
            PlaceLava();
        }

        private static void Stop(bool won)
        {
            Enabled = false;
            PausedByAllMods = false;
            CurrentPhase = won ? Phase.Won : Phase.Off;
            CountdownRemaining = 0f;
            DestroyLava();
            if (!won)
            {
                ClimbTime = 0f;
                Attempts = 0;
            }
        }

        public static void Reset()
        {
            ClearCache();
        }

        public static void ClearCache()
        {
            Stop(false);
            CurrentPhase = Phase.Off;
            _minY = 0f;
            _maxY = 0f;
            _floorY = 0f;
            _minX = 0f;
            _maxX = 0f;
            _minZ = 0f;
            _maxZ = 0f;
            _spawnPos = Vector3.zero;
            _summitPos = Vector3.zero;
            _hasSummit = false;
            _lavaY = 0f;
            _baseRate = 0f;
            _riseRate = 0f;
            _killGrace = 0f;
            _lavaArmed = false;
        }

        // ── Tick (unscaled: countdown, lava rise, UV) ─────────────────
        public static void Tick()
        {
            if (!Enabled || PausedByAllMods) return;

            float dt = Time.unscaledDeltaTime;
            if (dt < 0f) dt = 0f;
            if (dt > 0.1f) dt = 0.1f;

            if (CurrentPhase == Phase.Countdown)
            {
                HoldAtSpawn();
                CountdownRemaining -= dt;
                if (CountdownRemaining <= -GoHoldSeconds)
                {
                    CountdownRemaining = 0f;
                    CurrentPhase = Phase.Rising;
                    ClimbTime = 0f;
                    _lavaArmed = false;
                    _killGrace = KillGraceSeconds;
                    ModLog.Debug("[LavaRising] GO — climb " + MinArmClimb.ToString("F0")
                        + "m above spawn to start lava (unscaled)");
                }
                return;
            }

            if (CurrentPhase == Phase.Caught)
            {
                _caughtTimer -= dt;
                if (_caughtTimer <= 0f)
                    Stop(false);
                return;
            }

            if (CurrentPhase == Phase.Won)
            {
                _caughtTimer -= dt;
                ScrollLava(dt);
                if (_caughtTimer <= 0f)
                    Stop(false);
                return;
            }

            if (CurrentPhase != Phase.Rising) return;

            if (!_lavaArmed)
            {
                GameObject p = GameObject.Find("Player_Human");
                float py = UnityNull.Alive(p) ? p.transform.position.y : _spawnPos.y;
                NoteHeight(py);
                if (py < _spawnPos.y + MinArmClimb)
                {
                    PlaceLava();
                    ScrollLava(dt);
                    return;
                }
                _lavaArmed = true;
                _killGrace = KillGraceSeconds;
                float snapY = py - LavaArmBelowPlayer;
                float minSnap = _spawnPos.y - 35f;
                if (snapY < minSnap) snapY = minSnap;
                if (_lavaY < snapY) _lavaY = snapY;
                PlaceLava();
                ModLog.Debug("[LavaRising] Lava armed at Y=" + _lavaY.ToString("F0")
                    + " (climbed " + (py - _spawnPos.y).ToString("F0") + "m)");
            }

            ClimbTime += dt;
            if (_killGrace > 0f)
            {
                _killGrace -= dt;
                if (_killGrace < 0f) _killGrace = 0f;
            }
            RecalcRate();
            _lavaY += _riseRate * dt;
            if (_lavaY > _maxY + 4f) _lavaY = _maxY + 4f;
            PlaceLava();
            ScrollLava(dt);
        }

        // ── FixedTick: kill / win against physics-timed player pose ───
        public static void FixedTick()
        {
            if (!Enabled || PausedByAllMods) return;
            if (CurrentPhase == Phase.Countdown)
            {
                HoldAtSpawn();
                return;
            }
            if (CurrentPhase != Phase.Rising) return;

            GameObject player = GameObject.Find("Player_Human");
            if (!UnityNull.Alive(player)) return;

            float py = player.transform.position.y;
            NoteHeight(py);

            if (py >= WinY())
            {
                CurrentPhase = Phase.Won;
                LastWinTime = ClimbTime;
                _caughtTimer = SummitHoldSeconds;
                ModLog.Feedback("[LavaRising] SUMMIT  " + FormatTime(LastWinTime)
                    + "  attempts=" + Attempts
                    + "  " + CurrentMeters.ToString("F0") + "m");
                TryBeatRecord(CurrentMeters);
                return;
            }

            if (!_lavaArmed || _killGrace > 0f) return;

            if (py <= _lavaY + KillMargin)
                OnCaught();
        }

        private static void OnCaught()
        {
            if (CurrentPhase != Phase.Rising) return;
            CurrentPhase = Phase.Caught;
            _caughtTimer = CaughtHoldSeconds;
            GameObject player = GameObject.Find("Player_Human");
            if (UnityNull.Alive(player))
            {
                try
                {
                    Vehicle v = player.GetComponent<Vehicle>();
                    if (UnityNull.Alive(v))
                    {
                        MethodInfo setVel = typeof(Vehicle).GetMethod("SetVelocity",
                            BindingFlags.Public | BindingFlags.Instance);
                        if ((object)setVel != null)
                            setVel.Invoke(v, new object[] { Vector3.up * 12f });
                    }
                }
                catch (System.Exception ex)
                {
                    ModLog.Warn("[LavaRising] Caught impulse: " + ex.Message);
                }
            }
            ModLog.Debug("[LavaRising] Caught at Y=" + (UnityNull.Alive(player) ? player.transform.position.y.ToString("F1") : "?")
                + " lavaY=" + _lavaY.ToString("F1")
                + " t=" + ClimbTime.ToString("F1") + "s"
                + " meters=" + CurrentMeters.ToString("F0"));
            TryBeatRecord(CurrentMeters);
        }

        private static void RecalcRate()
        {
            float range = _maxY - _minY;
            if (range < 8f) range = 8f;
            int i = DifficultyLevel - 1;
            if (i < 0 || i >= ClimbSeconds.Length) i = 1;
            _baseRate = range / ClimbSeconds[i];
            float mul = 1f;
            if (ClimbTime > 100f) mul = 1.35f;
            else if (ClimbTime > 55f) mul = 1.15f;
            _riseRate = _baseRate * mul;
        }

        private static float LavaStartY()
        {
            float floor = _floorY;
            if (floor > _minY) floor = _minY;
            return floor - LavaBelowMap;
        }

        public static float ArmClimbHeight()
        {
            return MinArmClimb;
        }

        public static string CurrentMapDisplay
        {
            get { return FormatMapName(CurrentMapKey()); }
        }

        public static string FormatMeters(float m)
        {
            if (m < 0.5f) return "0m";
            return ((int)m).ToString() + "m";
        }

        private static void DisableConflicts()
        {
            try { if (FlyMode.Enabled) FlyMode.Toggle(); } catch { }
            try { if (HoverMode.Enabled) HoverMode.Toggle(); } catch { }
            try { if (SpectateMode.Enabled) SpectateMode.Toggle(); } catch { }
            try { if (AvalancheMode.Enabled) AvalancheMode.Reset(); } catch { }
            try { if (EarthquakeMode.Enabled) EarthquakeMode.Reset(); } catch { }
            try { if (PoliceChaseMode.Enabled) PoliceChaseMode.Reset(); } catch { }
            try { if (TrickAttackMode.CurrentState != TrickAttackMode.State.Off) TrickAttackMode.Reset(); } catch { }
            try { if (BoulderDodgeMode.Enabled) BoulderDodgeMode.Reset(); } catch { }
            try { if (SurvivalMode.Enabled) SurvivalMode.Reset(); } catch { }
            try { if (ObjectPlacer.Enabled) ObjectPlacer.Toggle(); } catch { }
        }

        // ── Height scan ───────────────────────────────────────────────
        private static bool ScanMap()
        {
            _scan.Clear();
            _minY = float.MaxValue;
            _maxY = float.MinValue;
            _floorY = float.MaxValue;
            _minX = float.MaxValue;
            _maxX = float.MinValue;
            _minZ = float.MaxValue;
            _maxZ = float.MinValue;
            _spawnPos = Vector3.zero;

            Terrain[] terrains = UnityEngine.Object.FindObjectsOfType<Terrain>();
            if ((object)terrains != null)
            {
                for (int t = 0; t < terrains.Length; t++)
                {
                    Terrain ter = terrains[t];
                    if (!UnityNull.Alive(ter) || !UnityNull.Alive(ter.terrainData)) continue;
                    ScanTerrain(ter);
                }
            }

            if (_scan.Count == 0)
            {
                ModLog.Warn("[LavaRising] No Terrain components — raycast grid fallback.");
                ScanRaycastGrid();
            }

            if (_scan.Count == 0)
            {
                MelonLogger.Error("[LavaRising] Empty height scan.");
                return false;
            }

            for (int i = 0; i < _scan.Count; i++)
            {
                ScanPt p = _scan[i];
                if (p.y < _floorY) _floorY = p.y;
                if (p.y > _maxY) _maxY = p.y;
                if (p.x < _minX) _minX = p.x;
                if (p.x > _maxX) _maxX = p.x;
                if (p.z < _minZ) _minZ = p.z;
                if (p.z > _maxZ) _maxZ = p.z;
            }

            if (!PickInlandSpawn())
            {
                MelonLogger.Error("[LavaRising] No inland spawn. floorY=" + _floorY
                    + " maxY=" + _maxY + " samples=" + _scan.Count);
                return false;
            }

            if (!PickSummit())
            {
                MelonLogger.Error("[LavaRising] No summit point found.");
                return false;
            }

            _minY = _spawnPos.y - SpawnLift;

            float range = _maxY - _minY;
            if (range < 5f)
                ModLog.Warn("[LavaRising] Very small height range (" + range.ToString("F1")
                    + "m) — map may be flat or the scan hit scenery.");

            if (_spawnPos.y >= _maxY - 3f)
            {
                MelonLogger.Error("[LavaRising] Spawn is too close to the peak (spawnY="
                    + _spawnPos.y.ToString("F1") + " maxY=" + _maxY.ToString("F1")
                    + ") — map may be too flat.");
                return false;
            }

            ModLog.Debug("[LavaRising] Inland spawn=" + _spawnPos
                + " summit=" + _summitPos
                + " floorY=" + _floorY.ToString("F1")
                + " playMin=" + _minY.ToString("F1")
                + " maxY=" + _maxY.ToString("F1"));
            return true;
        }

        private static bool PickSummit()
        {
            _hasSummit = false;
            float band = Mathf.Max(6f, (_maxY - _minY) * 0.06f);
            float cutoff = _maxY - band;
            float cx = (_minX + _maxX) * 0.5f;
            float cz = (_minZ + _maxZ) * 0.5f;
            float bestY = float.MinValue;
            float bestDist = float.MaxValue;
            int best = -1;

            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < _scan.Count; i++)
                {
                    ScanPt p = _scan[i];
                    if (pass == 0 && !p.inland) continue;
                    if (p.y < cutoff) continue;
                    float dx = p.x - cx;
                    float dz = p.z - cz;
                    float dist = dx * dx + dz * dz;
                    if (p.y > bestY + 0.05f
                        || (Mathf.Abs(p.y - bestY) <= 0.05f && dist < bestDist))
                    {
                        bestY = p.y;
                        bestDist = dist;
                        best = i;
                    }
                }
                if (best >= 0) break;
                cutoff = _maxY - band * 3f;
            }

            if (best < 0)
            {
                bestY = float.MinValue;
                for (int i = 0; i < _scan.Count; i++)
                {
                    ScanPt p = _scan[i];
                    if (!p.inland) continue;
                    if (p.y <= bestY) continue;
                    bestY = p.y;
                    best = i;
                }
            }

            if (best < 0) return false;

            ScanPt s = _scan[best];
            _summitPos = SnapToGround(s.x, s.y, s.z);
            if (_summitPos.y < bestY - 5f) _summitPos.y = bestY;
            _hasSummit = true;
            return true;
        }

        // Vertical metres still to climb before the win height (counts down as you go up).
        public static bool TryGetSummitRemaining(out float metersRemaining)
        {
            metersRemaining = 0f;
            if (!_hasSummit || !Enabled) return false;
            if (CurrentPhase == Phase.Off || CurrentPhase == Phase.Caught || CurrentPhase == Phase.Won)
                return false;

            GameObject player = GameObject.Find("Player_Human");
            if (!UnityNull.Alive(player)) return false;

            metersRemaining = WinY() - player.transform.position.y;
            if (metersRemaining < 0f) metersRemaining = 0f;
            return true;
        }

        private static void ScanTerrain(Terrain ter)
        {
            TerrainData data = ter.terrainData;
            Vector3 origin = ter.transform.position;
            Vector3 size = data.size;

            for (int ix = 0; ix < TerrainGrid; ix++)
            {
                float nx = TerrainGrid == 1 ? 0.5f : (float)ix / (TerrainGrid - 1);
                for (int iz = 0; iz < TerrainGrid; iz++)
                {
                    float nz = TerrainGrid == 1 ? 0.5f : (float)iz / (TerrainGrid - 1);
                    float h = data.GetInterpolatedHeight(nx, nz) + origin.y;
                    float wx = origin.x + nx * size.x;
                    float wz = origin.z + nz * size.z;
                    ScanPt pt;
                    pt.x = wx;
                    pt.y = h;
                    pt.z = wz;
                    pt.inland = ix >= TerrainInset && ix < TerrainGrid - TerrainInset
                             && iz >= TerrainInset && iz < TerrainGrid - TerrainInset;
                    _scan.Add(pt);
                }
            }
        }

        private static void ScanRaycastGrid()
        {
            GameObject player = GameObject.Find("Player_Human");
            Vector3 centre = UnityNull.Alive(player) ? player.transform.position : Vector3.zero;
            float span = 400f;
            int inset = 2;

            for (int ix = 0; ix < RaycastGrid; ix++)
            {
                float nx = RaycastGrid == 1 ? 0.5f : (float)ix / (RaycastGrid - 1);
                for (int iz = 0; iz < RaycastGrid; iz++)
                {
                    float nz = RaycastGrid == 1 ? 0.5f : (float)iz / (RaycastGrid - 1);
                    float wx = centre.x + (nx - 0.5f) * span;
                    float wz = centre.z + (nz - 0.5f) * span;
                    RaycastHit hit;
                    Vector3 from = new Vector3(wx, centre.y + 800f, wz);
                    if (!Physics.Raycast(from, Vector3.down, out hit, 2000f)) continue;
                    ScanPt pt;
                    pt.x = hit.point.x;
                    pt.y = hit.point.y;
                    pt.z = hit.point.z;
                    pt.inland = ix >= inset && ix < RaycastGrid - inset
                             && iz >= inset && iz < RaycastGrid - inset;
                    _scan.Add(pt);
                }
            }
        }

        private static bool PickInlandSpawn()
        {
            float inlandMin = float.MaxValue;
            int inlandCount = 0;
            for (int i = 0; i < _scan.Count; i++)
            {
                if (!_scan[i].inland) continue;
                inlandCount++;
                if (_scan[i].y < inlandMin) inlandMin = _scan[i].y;
            }
            if (inlandCount == 0)
            {
                ModLog.Warn("[LavaRising] No inset samples — using full scan.");
                for (int i = 0; i < _scan.Count; i++)
                {
                    ScanPt q = _scan[i];
                    q.inland = true;
                    _scan[i] = q;
                    if (q.y < inlandMin) inlandMin = q.y;
                }
            }

            float[] bands = { 12f, 30f, 60f, 99999f };
            for (int b = 0; b < bands.Length; b++)
            {
                float band = bands[b];
                float bestY = float.MaxValue;
                int best = -1;
                for (int i = 0; i < _scan.Count; i++)
                {
                    ScanPt p = _scan[i];
                    if (!p.inland) continue;
                    if (p.y > inlandMin + band) continue;
                    if (p.y >= bestY) continue;
                    if (!IsShelf(p.x, p.y, p.z)) continue;
                    bestY = p.y;
                    best = i;
                }
                if (best >= 0)
                {
                    ScanPt p = _scan[best];
                    Vector3 grounded = SnapToGround(p.x, p.y, p.z);
                    grounded = NudgeInland(grounded);
                    _spawnPos = grounded + Vector3.up * SpawnLift;
                    ModLog.Debug("[LavaRising] Spawn band=" + band.ToString("F0")
                        + "m above inlandMin " + inlandMin.ToString("F1"));
                    return true;
                }
            }

            int fallback = -1;
            float fy = float.MaxValue;
            for (int i = 0; i < _scan.Count; i++)
            {
                if (!_scan[i].inland) continue;
                if (_scan[i].y >= fy) continue;
                fy = _scan[i].y;
                fallback = i;
            }
            if (fallback < 0) return false;
            ScanPt raw = _scan[fallback];
            Vector3 pos = SnapToGround(raw.x, raw.y, raw.z);
            pos = NudgeInland(pos);
            _spawnPos = pos + Vector3.up * SpawnLift;
            ModLog.Warn("[LavaRising] Spawn fell back to lowest inland without a shelf check.");
            return true;
        }

        private static bool IsShelf(float x, float y, float z)
        {
            RaycastHit hit;
            Vector3 from = new Vector3(x, y + 40f, z);
            if (!Physics.Raycast(from, Vector3.down, out hit, 90f)) return false;
            if (Mathf.Abs(hit.point.y - y) > 8f) return false;

            int good = 0;
            for (int d = 0; d < 8; d++)
            {
                float ang = d * 45f * Mathf.Deg2Rad;
                float px = x + Mathf.Cos(ang) * 14f;
                float pz = z + Mathf.Sin(ang) * 14f;
                Vector3 probe = new Vector3(px, y + 40f, pz);
                RaycastHit nHit;
                if (!Physics.Raycast(probe, Vector3.down, out nHit, 100f)) continue;
                float drop = y - nHit.point.y;
                if (drop < 12f && drop > -8f) good++;
            }
            return good >= 6;
        }

        private static Vector3 SnapToGround(float x, float y, float z)
        {
            RaycastHit hit;
            Vector3 from = new Vector3(x, y + 80f, z);
            if (Physics.Raycast(from, Vector3.down, out hit, 200f))
                return hit.point;
            return new Vector3(x, y, z);
        }

        private static Vector3 NudgeInland(Vector3 spawn)
        {
            if (IsShelf(spawn.x, spawn.y, spawn.z)) return spawn;

            float cx = (_minX + _maxX) * 0.5f;
            float cz = (_minZ + _maxZ) * 0.5f;
            Vector3 dir = new Vector3(cx - spawn.x, 0f, cz - spawn.z);
            if (dir.sqrMagnitude < 1f) return spawn;
            dir.Normalize();

            Vector3 best = spawn;
            for (int s = 1; s <= 16; s++)
            {
                Vector3 p = spawn + dir * (10f * s);
                Vector3 g = SnapToGround(p.x, spawn.y, p.z);
                if (g.y < spawn.y - 40f) continue;
                if (IsShelf(g.x, g.y, g.z))
                {
                    ModLog.Debug("[LavaRising] Nudged spawn inland " + (10f * s).ToString("F0") + "m toward centre.");
                    return g;
                }
                best = g;
            }
            return best;
        }

        private static float WinY()
        {
            float y = _maxY - WinMargin;
            if (y <= _spawnPos.y + 4f) y = _maxY;
            return y;
        }

        // ── Teleport (same sequence as TeleportToCheckpoint.TeleportByIndex) ──
        private static bool TeleportToSpawn()
        {
            try
            {
                GameObject local = GameObject.Find("Player_Human");
                if (!UnityNull.Alive(local))
                {
                    ModLog.Warn("[LavaRising] Teleport: Player_Human not found.");
                    return false;
                }

                Vector3 dest = _spawnPos;
                local.transform.position = dest;

                Vehicle vehicle = local.GetComponent<Vehicle>();
                if (UnityNull.Alive(vehicle))
                {
                    Rigidbody rb = vehicle.GetComponent<Rigidbody>();
                    if (!UnityNull.Alive(rb)) rb = vehicle.GetComponentInChildren<Rigidbody>();
                    if (UnityNull.Alive(rb))
                    {
                        rb.position = dest;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    try
                    {
                        MethodInfo resetMethod = vehicle.GetType().GetMethod("Reset",
                            BindingFlags.Public | BindingFlags.Instance, null,
                            new System.Type[] { typeof(bool) }, null);
                        if ((object)resetMethod != null)
                            resetMethod.Invoke(vehicle, new object[] { false });
                    }
                    catch (System.Exception ex)
                    {
                        ModLog.Warn("[LavaRising] vehicle.Reset: " + ex.Message);
                    }
                }
                return true;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[LavaRising] TeleportToSpawn: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "LavaRising");
                return false;
            }
        }

        private static void HoldAtSpawn()
        {
            GameObject local = GameObject.Find("Player_Human");
            if (!UnityNull.Alive(local)) return;
            local.transform.position = _spawnPos;
            Vehicle vehicle = local.GetComponent<Vehicle>();
            if (!UnityNull.Alive(vehicle)) return;
            Rigidbody rb = vehicle.GetComponent<Rigidbody>();
            if (!UnityNull.Alive(rb)) rb = vehicle.GetComponentInChildren<Rigidbody>();
            if (!UnityNull.Alive(rb)) return;
            rb.position = _spawnPos;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // ── Lava visual ───────────────────────────────────────────────
        private static void CreateLava()
        {
            DestroyLava();

            float pad = 180f;
            float sx = Mathf.Max(40f, (_maxX - _minX) + pad * 2f);
            float sz = Mathf.Max(40f, (_maxZ - _minZ) + pad * 2f);
            float cx = (_minX + _maxX) * 0.5f;
            float cz = (_minZ + _maxZ) * 0.5f;
            if (sx < 40f) { sx = 400f; cx = _spawnPos.x; }
            if (sz < 40f) { sz = 400f; cz = _spawnPos.z; }

            _lavaGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _lavaGo.name = "SandboxLavaPlane";
            _lavaGo.transform.localScale = new Vector3(sx, LavaThickness, sz);
            Collider col = _lavaGo.GetComponent<Collider>();
            if (UnityNull.Alive(col)) UnityEngine.Object.Destroy(col);

            Shader sh = Shader.Find("Standard");
            if ((object)sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");
            if ((object)sh == null) sh = Shader.Find("Diffuse");
            if ((object)sh == null) sh = Shader.Find("Unlit/Color");
            MeshRenderer mr = _lavaGo.GetComponent<MeshRenderer>();
            if ((object)sh == null)
            {
                MelonLogger.Error("[LavaRising] No usable shader for lava plane — using primitive default.");
            }
            else
            {
                _lavaMat = new Material(sh);
                Color lava = new Color(1f, 0.22f, 0.04f, 1f);
                _lavaMat.color = lava;
                _lavaMat.EnableKeyword("_EMISSION");
                _lavaMat.SetColor("_EmissionColor", lava * 2.2f);
                if ((object)_lavaTex == null) _lavaTex = MakeLavaTex();
                _lavaMat.mainTexture = _lavaTex;
                if (UnityNull.Alive(mr)) mr.material = _lavaMat;
            }

            _lavaY = LavaStartY();
            _lavaGo.transform.position = new Vector3(cx, _lavaY, cz);
            _uvOffset = Vector2.zero;
        }

        private static void PlaceLava()
        {
            if (!UnityNull.Alive(_lavaGo)) return;
            Vector3 p = _lavaGo.transform.position;
            p.y = _lavaY;
            _lavaGo.transform.position = p;
        }

        private static void ScrollLava(float dt)
        {
            if (!UnityNull.Alive(_lavaMat)) return;
            _uvOffset.x += dt * 0.08f;
            _uvOffset.y += dt * 0.03f;
            _lavaMat.SetTextureOffset("_MainTex", _uvOffset);
        }

        private static Texture2D MakeLavaTex()
        {
            int n = 64;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear;
            Color dark = new Color(0.45f, 0.03f, 0.01f, 1f);
            Color hot = new Color(1f, 0.62f, 0.08f, 1f);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float v = 0.45f + 0.55f * Mathf.PerlinNoise(x * 0.14f, y * 0.14f);
                    t.SetPixel(x, y, Color.Lerp(dark, hot, v));
                }
            }
            t.Apply();
            return t;
        }

        private static void DestroyLava()
        {
            if (UnityNull.Alive(_lavaGo)) UnityEngine.Object.Destroy(_lavaGo);
            _lavaGo = null;
            _lavaMat = null;
            if ((object)_lavaTex != null)
            {
                UnityEngine.Object.Destroy(_lavaTex);
                _lavaTex = null;
            }
        }

        public static string FormatTime(float t)
        {
            if (t < 0f) t = 0f;
            int m = (int)(t / 60f);
            int s = (int)(t % 60f);
            int cs = (int)((t - (int)t) * 10f);
            return m + ":" + s.ToString("D2") + "." + cs;
        }

        // ── Height records (per map) ──────────────────────────────────
        private static void NoteHeight(float py)
        {
            float m = py - _spawnPos.y;
            if (m < 0f) m = 0f;
            if (m > CurrentMeters) CurrentMeters = m;
        }

        private static string CurrentMapKey()
        {
            string name = "";
            try
            {
                Scene sc = SceneManager.GetActiveScene();
                name = sc.name;
            }
            catch { }
            if (string.IsNullOrEmpty(name) || name == "EmptyScene") return "";
            try
            {
                string seed = MapChanger.GetCurrentLevelSeed();
                if (!string.IsNullOrEmpty(seed)) name = name + " [" + seed + "]";
            }
            catch { }
            return name;
        }

        private static string FormatMapName(string key)
        {
            if (string.IsNullOrEmpty(key)) return "—";
            return key.Replace('_', ' ');
        }

        private static float GetRecord(string key)
        {
            EnsureRecordsLoaded();
            if (string.IsNullOrEmpty(key)) return 0f;
            for (int i = 0; i < _recMaps.Count; i++)
                if (_recMaps[i] == key) return _recMeters[i];
            return 0f;
        }

        private static void TryBeatRecord(float meters)
        {
            if (meters < 1f) return;
            string key = CurrentMapKey();
            if (string.IsNullOrEmpty(key)) return;
            EnsureRecordsLoaded();
            int found = -1;
            for (int i = 0; i < _recMaps.Count; i++)
            {
                if (_recMaps[i] == key) { found = i; break; }
            }
            if (found >= 0)
            {
                if (meters <= _recMeters[found] + 0.25f) return;
                _recMeters[found] = meters;
            }
            else
            {
                _recMaps.Add(key);
                _recMeters.Add(meters);
            }
            ModLog.Feedback("[LavaRising] Record  " + FormatMeters(meters)
                + "  on  " + FormatMapName(key));
            SaveRecordsNow();
        }

        public static string ExportRecords()
        {
            EnsureRecordsLoaded();
            if (_recMaps.Count == 0) return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < _recMaps.Count; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(_recMaps[i].Replace('|', ' ').Replace('=', '-'));
                sb.Append('=');
                sb.Append(_recMeters[i].ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        public static void ImportRecords(string raw)
        {
            _recMaps.Clear();
            _recMeters.Clear();
            _recordsLoaded = true;
            if (string.IsNullOrEmpty(raw)) return;
            string[] parts = raw.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i];
                int eq = p.LastIndexOf('=');
                if (eq <= 0 || eq >= p.Length - 1) continue;
                string map = p.Substring(0, eq);
                float m;
                if (!float.TryParse(p.Substring(eq + 1),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out m)) continue;
                if (m < 0f) continue;
                _recMaps.Add(map);
                _recMeters.Add(m);
            }
        }

        public static void ClearRecords()
        {
            _recMaps.Clear();
            _recMeters.Clear();
            _recordsLoaded = true;
            try
            {
                if (File.Exists(RecordsFile)) File.Delete(RecordsFile);
            }
            catch (Exception ex)
            {
                ModLog.Warn("[LavaRising] ClearRecords: " + ex.Message);
            }
        }

        private static void EnsureRecordsLoaded()
        {
            if (_recordsLoaded) return;
            _recordsLoaded = true;
            try
            {
                if (!File.Exists(RecordsFile)) return;
                ImportRecords(File.ReadAllText(RecordsFile));
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[LavaRising] Load records: " + ex.Message);
            }
        }

        private static void SaveRecordsNow()
        {
            try
            {
                if (!Directory.Exists(RecordsFolder)) Directory.CreateDirectory(RecordsFolder);
                File.WriteAllText(RecordsFile, ExportRecords());
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[LavaRising] Save records: " + ex.Message);
            }
        }
    }
}
