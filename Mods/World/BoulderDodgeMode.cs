using MelonLoader;
using DescendersModMenu;
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
        public  static int IntervalIndex = 2;

        private static readonly float[]  SizeValues  = { 1f, 2f, 3f, 5f, 8f, 12f };
        private static readonly string[] SizeLabels  = { "Tiny", "Small", "Medium", "Large", "Huge", "Massive" };
        public  static int SizeIndex = 2;

        private static readonly float[]  ForwardValues  = { 15f, 20f, 25f, 30f, 40f, 50f };
        private static readonly string[] ForwardLabels  = { "15m", "20m", "25m", "30m", "40m", "50m" };
        public  static int ForwardIndex = 1;

        public static string IntervalDisplay => IntervalLabels[IntervalIndex];
        public static string SizeDisplay     => SizeLabels[SizeIndex];
        public static string ForwardDisplay  => ForwardLabels[ForwardIndex];

        // ── Constants ─────────────────────────────────────────────────
        private const float SpawnHeight     = 10f;
        private const float SpawnJitter     = 2f;
        private const float ExtraGravity    = 220f;
        private const float LockVelThresh   = 0.6f;
        private const float LockConfirmTime = 0.25f;
        private const float MinFallTime     = 0.35f;
        private const float ForceLockAfter  = 3.5f;
        private const float CleanupDist     = 200f;
        private const int   HardCap         = 25;
        private const float WarnLeadTime    = 0.55f;

        // ── Internal ──────────────────────────────────────────────────
        private static float _spawnTimer = 0f;
        private static float _warnTimer = -1f;
        private static Vector3 _warnXZ = Vector3.zero;
        private static GameObject _warnMarker = null;

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
                _spawnTimer = IntervalValues[IntervalIndex];
                _warnTimer = -1f;
                ModLog.Debug("[BoulderDodge] ON");
            }
            else
            {
                ClearAll();
                ModLog.Debug("[BoulderDodge] OFF");
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
            float interval = IntervalValues[IntervalIndex];

            // Place warning shortly before the drop.
            if (_warnTimer < 0f && _spawnTimer >= interval - WarnLeadTime && _boulders.Count < HardCap)
            {
                if (TryPickImpactPoint(out _warnXZ))
                {
                    _warnTimer = WarnLeadTime;
                    ShowWarning(_warnXZ);
                }
            }

            if (_warnTimer >= 0f)
            {
                _warnTimer -= Time.deltaTime;
                PulseWarning();
                if (_warnTimer <= 0f)
                {
                    _warnTimer = -1f;
                    _spawnTimer = 0f;
                    SpawnAt(_warnXZ);
                    ClearWarning();
                }
            }
            else if (_spawnTimer >= interval)
            {
                _spawnTimer = 0f;
                TrySpawn();
            }

            GameObject player = GameObject.Find("Player_Human");
            Vector3 playerPos = UnityNull.Alive(player)
                ? player.transform.position : Vector3.zero;

            for (int i = _boulders.Count - 1; i >= 0; i--)
            {
                var b = _boulders[i];
                if (!UnityNull.Alive(b.Go)) { _boulders.RemoveAt(i); continue; }

                b.Age += Time.deltaTime;

                if (UnityNull.Alive(player))
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

        public static void FixedTick()
        {
            if (!Enabled) return;

            for (int i = 0; i < _boulders.Count; i++)
            {
                var b = _boulders[i];
                if (!UnityNull.Alive(b.Go) || !UnityNull.Alive(b.Rb)) continue;
                if (b.Locked) continue;

                b.Rb.AddForce(Vector3.down * ExtraGravity, ForceMode.Acceleration);

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
                        b.LowVelTimer = 0f;
                    }

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
                ModLog.Debug("[BoulderDodge] Hard cap reached (" + HardCap + "), skipping spawn.");
                return;
            }

            Vector3 xz;
            if (!TryPickImpactPoint(out xz)) return;
            SpawnAt(xz);
        }

        private static bool TryPickImpactPoint(out Vector3 xz)
        {
            xz = Vector3.zero;
            GameObject player = GameObject.Find("Player_Human");
            if (!UnityNull.Alive(player)) return false;

            Vector3 playerPos = player.transform.position;

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

            float forwardDist = ForwardValues[ForwardIndex];
            xz = playerPos + predictedDir * forwardDist;
            xz.x += Random.Range(-SpawnJitter, SpawnJitter);
            xz.z += Random.Range(-SpawnJitter, SpawnJitter);
            xz.y = 0f;
            return true;
        }

        private static void SpawnAt(Vector3 targetXZ)
        {
            if (_boulders.Count >= HardCap) return;

            float groundY = GetGroundHeight(targetXZ);
            Vector3 spawnPos = new Vector3(targetXZ.x, groundY + SpawnHeight, targetXZ.z);

            GameObject boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boulder.name = "BoulderDodgeRock";

            float size = SizeValues[SizeIndex];
            boulder.transform.position = spawnPos;
            // Slightly irregular so they read less like bowling balls.
            boulder.transform.localScale = new Vector3(
                size * Random.Range(0.9f, 1.15f),
                size * Random.Range(0.85f, 1.1f),
                size * Random.Range(0.9f, 1.15f));

            var rend = boulder.GetComponent<Renderer>();
            if ((object)rend != null)
                rend.material.color = new Color(
                    Random.Range(0.28f, 0.42f),
                    Random.Range(0.24f, 0.36f),
                    Random.Range(0.18f, 0.30f));

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

            var boulderRb = boulder.AddComponent<Rigidbody>();
            boulderRb.mass                  = 500f;
            boulderRb.drag                  = 0.2f;
            boulderRb.angularDrag           = 5f;
            boulderRb.useGravity            = true;
            boulderRb.interpolation         = RigidbodyInterpolation.Interpolate;
            boulderRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            boulderRb.velocity = Vector3.down * 35f;
            boulderRb.angularVelocity = new Vector3(
                Random.Range(-2f, 2f), Random.Range(-1f, 1f), Random.Range(-2f, 2f));

            _boulders.Add(new BoulderEntry
            {
                Go          = boulder,
                Rb          = boulderRb,
                Age         = 0f,
                LowVelTimer = 0f,
                Locked      = false,
            });
            ModLog.Debug("[BoulderDodge] Spawned at " + spawnPos
                + " size=" + size
                + " active=" + _boulders.Count);
        }

        private static void ShowWarning(Vector3 xz)
        {
            ClearWarning();
            float groundY = GetGroundHeight(xz);
            float size = SizeValues[SizeIndex];

            _warnMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _warnMarker.name = "BoulderDodgeWarn";
            Object.Destroy(_warnMarker.GetComponent<Collider>());
            _warnMarker.transform.position = new Vector3(xz.x, groundY + 0.05f, xz.z);
            _warnMarker.transform.localScale = new Vector3(size * 1.1f, 0.04f, size * 1.1f);

            var rend = _warnMarker.GetComponent<Renderer>();
            if ((object)rend != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(1f, 0.25f, 0.05f, 0.55f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.2f, 0f) * 0.6f);
                rend.material = mat;
            }
        }

        private static void PulseWarning()
        {
            if (!UnityNull.Alive(_warnMarker)) return;
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 12f);
            float size = SizeValues[SizeIndex] * pulse;
            Vector3 p = _warnMarker.transform.position;
            _warnMarker.transform.localScale = new Vector3(size * 1.1f, 0.04f, size * 1.1f);
            _warnMarker.transform.position = p;
        }

        private static void ClearWarning()
        {
            if ((object)_warnMarker != null)
                Object.Destroy(_warnMarker);
            _warnMarker = null;
        }

        // ── Lock boulder in place ─────────────────────────────────────
        private static void LockBoulder(BoulderEntry b)
        {
            if (!UnityNull.Alive(b.Rb)) return;
            b.Locked          = true;
            b.Rb.velocity        = Vector3.zero;
            b.Rb.angularVelocity = Vector3.zero;
            b.Rb.isKinematic     = true;
            ModLog.Debug("[BoulderDodge] Boulder locked at age=" + b.Age.ToString("F1"));
        }

        // ── Ground height ─────────────────────────────────────────────
        private static float GetGroundHeight(Vector3 worldPos)
        {
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

            RaycastHit hit;
            Vector3 castFrom = new Vector3(worldPos.x, worldPos.y + 500f, worldPos.z);
            if (Physics.Raycast(castFrom, Vector3.down, out hit, 1000f))
                return hit.point.y;

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
            _warnTimer = -1f;
            ClearWarning();
        }

        public static int ActiveCount => _boulders.Count;
    }
}
