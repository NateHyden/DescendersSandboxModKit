using MelonLoader;
using UnityEngine;
using System.Collections.Generic;

namespace DescendersModMenu.Mods
{
    public static class BoulderDodgeMode
    {
        // ── Public state ──────────────────────────────────────────────
        public static bool Enabled { get; private set; } = false;

        // ── Settings ──────────────────────────────────────────────────
        private static readonly float[]  IntervalValues  = { 3f, 5f, 8f, 12f, 20f, 30f };
        private static readonly string[] IntervalLabels  = { "3s", "5s", "8s", "12s", "20s", "30s" };
        public  static int IntervalIndex = 2; // default 8s

        private static readonly float[]  SizeValues  = { 1f, 2f, 3f, 5f, 8f, 12f };
        private static readonly string[] SizeLabels  = { "Tiny", "Small", "Medium", "Large", "Huge", "Massive" };
        public  static int SizeIndex = 2; // default Medium

        private static readonly float[]  ForwardValues  = { 15f, 20f, 25f, 30f, 40f, 50f };
        private static readonly string[] ForwardLabels  = { "15m", "20m", "25m", "30m", "40m", "50m" };
        public  static int ForwardIndex = 1; // default 20m

        // Display helpers
        public static string IntervalDisplay => IntervalLabels[IntervalIndex];
        public static string SizeDisplay     => SizeLabels[SizeIndex];
        public static string ForwardDisplay  => ForwardLabels[ForwardIndex];

        // ── Constants ─────────────────────────────────────────────────
        private const float SpawnHeight     = 20f;   // metres above target point
        private const float SpawnJitter     = 2f;    // random XZ spread on spawn
        private const float ExtraGravity    = 60f;   // downforce while falling (m/s²)
        private const float LockVelThresh   = 0.6f;  // velocity below this → start lock timer
        private const float LockConfirmTime = 0.4f;  // must stay below thresh this long to lock
        private const float MinFallTime     = 1.2f;  // ignore velocity until this many seconds after spawn
        private const float ForceLockAfter  = 8f;    // force-lock regardless after this many seconds
        private const float CleanupDist     = 200f;  // despawn when this far from player
        private const int   HardCap         = 25;    // absolute max boulders

        // ── Internal ──────────────────────────────────────────────────
        private static float _spawnTimer = 0f;

        private class BoulderEntry
        {
            public GameObject Go;
            public Rigidbody  Rb;
            public float      Age;
            public float      LowVelTimer;
            public bool       Locked;
        }

        private static readonly List<BoulderEntry> _boulders = new List<BoulderEntry>();

        // ── Toggle / Reset ────────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled)
            {
                _spawnTimer = IntervalValues[IntervalIndex]; // spawn immediately on first tick
                MelonLogger.Msg("[BoulderDodge] ON");
            }
            else
            {
                ClearAll();
                MelonLogger.Msg("[BoulderDodge] OFF");
            }
        }

        public static void Reset()
        {
            Enabled = false;
            ClearAll();
        }

        // ── Selectors ─────────────────────────────────────────────────
        public static void PrevInterval() { if (IntervalIndex > 0) IntervalIndex--; }
        public static void NextInterval() { if (IntervalIndex < IntervalValues.Length - 1) IntervalIndex++; }
        public static void PrevSize()     { if (SizeIndex > 0) SizeIndex--; }
        public static void NextSize()     { if (SizeIndex < SizeValues.Length - 1) SizeIndex++; }
        public static void PrevForward()  { if (ForwardIndex > 0) ForwardIndex--; }
        public static void NextForward()  { if (ForwardIndex < ForwardValues.Length - 1) ForwardIndex++; }

        // ── Tick (OnUpdate) ───────────────────────────────────────────
        public static void Tick()
        {
            if (!Enabled) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= IntervalValues[IntervalIndex])
            {
                _spawnTimer = 0f;
                TrySpawn();
            }

            // Distance-based cleanup + null sweep
            GameObject player = GameObject.Find("Player_Human");
            Vector3 playerPos = (object)player != null
                ? player.transform.position : Vector3.zero;

            for (int i = _boulders.Count - 1; i >= 0; i--)
            {
                var b = _boulders[i];
                if ((object)b.Go == null) { _boulders.RemoveAt(i); continue; }

                b.Age += Time.deltaTime;

                if ((object)player != null)
                {
                    float dist = Vector3.Distance(b.Go.transform.position, playerPos);
                    if (dist > CleanupDist)
                    {
                        GameObject.Destroy(b.Go);
                        _boulders.RemoveAt(i);
                    }
                }
            }
        }

        // ── FixedTick (OnFixedUpdate) ─────────────────────────────────
        public static void FixedTick()
        {
            if (!Enabled) return;

            for (int i = 0; i < _boulders.Count; i++)
            {
                var b = _boulders[i];
                if ((object)b.Go == null || (object)b.Rb == null) continue;
                if (b.Locked) continue;

                // Extra gravity while falling
                b.Rb.AddForce(Vector3.down * ExtraGravity, ForceMode.Acceleration);

                // Landing lock logic — only check after minimum fall time
                if (b.Age > MinFallTime)
                {
                    if (b.Rb.velocity.magnitude < LockVelThresh)
                    {
                        b.LowVelTimer += Time.fixedDeltaTime;
                        if (b.LowVelTimer >= LockConfirmTime || b.Age >= ForceLockAfter)
                            LockBoulder(b);
                    }
                    else
                    {
                        b.LowVelTimer = 0f; // reset if picks up speed again (e.g. rolling off ledge)
                    }

                    // Force lock failsafe
                    if (b.Age >= ForceLockAfter && !b.Locked)
                        LockBoulder(b);
                }
            }
        }

        // ── Spawn ─────────────────────────────────────────────────────
        private static void TrySpawn()
        {
            if (_boulders.Count >= HardCap)
            {
                MelonLogger.Msg("[BoulderDodge] Hard cap reached (" + HardCap + "), skipping spawn.");
                return;
            }

            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) return;

            Vector3 playerPos = player.transform.position;

            // ── Direction prediction ──────────────────────────────────
            // Primary: horizontal velocity from Rigidbody
            // Fallback: bike transform.forward if moving too slowly
            Vector3 predictedDir = player.transform.forward;
            predictedDir.y = 0f;
            if (predictedDir.sqrMagnitude < 0.01f) predictedDir = Vector3.forward;
            predictedDir.Normalize();

            Rigidbody rb = player.GetComponentInChildren<Rigidbody>();
            if ((object)rb != null)
            {
                Vector3 hVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                if (hVel.magnitude > 2f)
                    predictedDir = hVel.normalized;
            }

            // ── Spawn position ────────────────────────────────────────
            float forwardDist = ForwardValues[ForwardIndex];
            Vector3 targetXZ = playerPos + predictedDir * forwardDist;

            // Small random jitter so it's not laser-precise
            targetXZ.x += Random.Range(-SpawnJitter, SpawnJitter);
            targetXZ.z += Random.Range(-SpawnJitter, SpawnJitter);

            float groundY = GetGroundHeight(targetXZ);
            Vector3 spawnPos = new Vector3(targetXZ.x, groundY + SpawnHeight, targetXZ.z);

            // ── Create boulder ────────────────────────────────────────
            GameObject boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boulder.name = "BoulderDodgeRock";

            float size = SizeValues[SizeIndex];
            boulder.transform.position = spawnPos;
            boulder.transform.localScale = Vector3.one * size;

            // Rocky grey-brown colour with slight variation
            var rend = boulder.GetComponent<Renderer>();
            if ((object)rend != null)
                rend.material.color = new Color(
                    Random.Range(0.28f, 0.42f),
                    Random.Range(0.24f, 0.36f),
                    Random.Range(0.18f, 0.30f));

            // High-friction, no bounce physics material — grips terrain on landing
            var col = boulder.GetComponent<Collider>();
            if ((object)col != null)
            {
                var mat = new PhysicMaterial("BoulderMat");
                mat.staticFriction  = 1f;
                mat.dynamicFriction = 1f;
                mat.frictionCombine = PhysicMaterialCombine.Maximum;
                mat.bounciness      = 0f;
                mat.bounceCombine   = PhysicMaterialCombine.Minimum;
                col.material = mat;
            }

            // Heavy rigidbody — mass ensures it won't get nudged by the bike
            var boulderRb = boulder.AddComponent<Rigidbody>();
            boulderRb.mass                  = 500f;
            boulderRb.drag                  = 0.2f;
            boulderRb.angularDrag           = 5f;
            boulderRb.useGravity            = true;
            boulderRb.interpolation         = RigidbodyInterpolation.Interpolate;
            boulderRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var entry = new BoulderEntry
            {
                Go          = boulder,
                Rb          = boulderRb,
                Age         = 0f,
                LowVelTimer = 0f,
                Locked      = false,
            };

            _boulders.Add(entry);
            MelonLogger.Msg("[BoulderDodge] Spawned at " + spawnPos
                + " dir=" + predictedDir
                + " size=" + size
                + " active=" + _boulders.Count);
        }

        // ── Lock boulder in place ─────────────────────────────────────
        private static void LockBoulder(BoulderEntry b)
        {
            b.Locked          = true;
            b.Rb.velocity        = Vector3.zero;
            b.Rb.angularVelocity = Vector3.zero;
            b.Rb.isKinematic     = true;
            MelonLogger.Msg("[BoulderDodge] Boulder locked at age=" + b.Age.ToString("F1"));
        }

        // ── Ground height ─────────────────────────────────────────────
        private static float GetGroundHeight(Vector3 worldPos)
        {
            // Try Unity Terrain first
            Terrain terrain = Terrain.activeTerrain;
            if ((object)(UnityEngine.Object)terrain != null
                && (object)(UnityEngine.Object)terrain.terrainData != null)
            {
                Vector3 rel = worldPos - terrain.transform.position;
                float nx = Mathf.InverseLerp(0f, terrain.terrainData.size.x, rel.x);
                float nz = Mathf.InverseLerp(0f, terrain.terrainData.size.z, rel.z);
                float h = terrain.terrainData.GetInterpolatedHeight(nx, nz)
                          + terrain.transform.position.y;
                if (h > 1f) return h;
            }

            // Raycast fallback — cast from high above
            RaycastHit hit;
            Vector3 castFrom = new Vector3(worldPos.x, worldPos.y + 500f, worldPos.z);
            if (Physics.Raycast(castFrom, Vector3.down, out hit, 1000f))
                return hit.point.y;

            // Last resort
            GameObject player = GameObject.Find("Player_Human");
            return (object)player != null ? player.transform.position.y : 0f;
        }

        // ── Clear ─────────────────────────────────────────────────────
        private static void ClearAll()
        {
            for (int i = 0; i < _boulders.Count; i++)
                if ((object)_boulders[i].Go != null)
                    GameObject.Destroy(_boulders[i].Go);
            _boulders.Clear();
            _spawnTimer = 0f;
        }

        public static int ActiveCount => _boulders.Count;
    }
}
