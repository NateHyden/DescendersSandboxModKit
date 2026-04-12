using MelonLoader;
using UnityEngine;
using System.Collections.Generic;

namespace DescendersModMenu.Mods
{
    public static class AvalancheMode
    {
        public static bool Enabled { get; private set; } = false;

        // ── Settings ──────────────────────────────────────────────────
        public static float SpawnInterval = 4f;
        public static int MaxHazards = 3;
        public static float HazardLifetime = 60f;
        public static float HazardSize = 2.0f;
        public static float AttractionForce = 5f;   // None=0 / Slight=2 / Medium=5 / Strong=10
        public static float MinSpawnDist = 8f;
        public static float ExtraGravity = 18f;
        public static float SpawnDistance = 15f;
        public static float SpawnRadius = 7f;
        public static float SpawnHeight = 20f;
        public static float ForwardImpulse = 10f;
        public static float DespawnDist = 200f;
        public static bool InstantFail = false;
        public static bool DifficultyScale = false;
        public static bool UseBox = false;
        public static bool ShowTimer = true;

        // Survival
        public static float SurvivalTime = 0f;

        // Internal
        private static float _spawnTimer = 0f;
        private static float _modeTimer = 0f;

        private class HazardEntry
        {
            public GameObject Go;
            public Rigidbody Rb;
            public float Age;
        }

        private static readonly List<HazardEntry> _hazards = new List<HazardEntry>();

        // ── Toggle ────────────────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled)
            {
                _spawnTimer = SpawnInterval; // spawn on first tick
                _modeTimer = 0f;
                SurvivalTime = 0f;
                MelonLogger.Msg("[Avalanche] ON");
            }
            else
            {
                ClearAll();
                MelonLogger.Msg("[Avalanche] OFF");
            }
        }

        // ── Update ────────────────────────────────────────────────────
        public static void Tick()
        {
            if (!Enabled) return;

            _modeTimer += Time.deltaTime;
            SurvivalTime += Time.deltaTime;
            _spawnTimer += Time.deltaTime;

            // Difficulty scaling
            float interval = SpawnInterval;
            if (DifficultyScale)
            {
                if (_modeTimer > 120f) interval = Mathf.Max(1f, SpawnInterval * 0.4f);
                else if (_modeTimer > 60f) interval = Mathf.Max(1f, SpawnInterval * 0.6f);
                else if (_modeTimer > 30f) interval = Mathf.Max(1f, SpawnInterval * 0.8f);
            }

            if (_spawnTimer >= interval) { _spawnTimer = 0f; TrySpawn(); }

            // Despawn + hit detection
            GameObject player = GameObject.Find("Player_Human");
            Vector3 playerPos = (object)player != null ? player.transform.position : Vector3.zero;

            for (int i = _hazards.Count - 1; i >= 0; i--)
            {
                var h = _hazards[i];
                if ((object)h.Go == null) { _hazards.RemoveAt(i); continue; }

                h.Age += Time.deltaTime;

                bool tooOld = h.Age > HazardLifetime;
                bool tooFar = false;
                bool stuck = false;

                if ((object)player != null)
                {
                    float dist = Vector3.Distance(h.Go.transform.position, playerPos);
                    tooFar = dist > DespawnDist;

                    // Hit detection — slightly generous radius
                    if (InstantFail && dist < (HazardSize * 0.5f) + 1.5f)
                    {
                        MelonLogger.Msg("[Avalanche] Hit! Bailing.");
                        TriggerBail(player);
                    }
                }

                // Mark stuck only after 12s with very low velocity
                if ((object)h.Rb != null)
                    stuck = h.Age > 12f && h.Rb.velocity.magnitude < 0.5f;

                if (tooOld || tooFar || stuck)
                {
                    MelonLogger.Msg("[Avalanche] Despawn: tooOld=" + tooOld
                        + " tooFar=" + tooFar + " stuck=" + stuck
                        + " age=" + h.Age.ToString("F1")
                        + " vel=" + ((object)h.Rb != null ? h.Rb.velocity.magnitude.ToString("F1") : "?")
                        + " dist=" + ((object)player != null ? Vector3.Distance(h.Go.transform.position, playerPos).ToString("F1") : "?"));
                    GameObject.Destroy(h.Go);
                    _hazards.RemoveAt(i);
                }
            }
        }

        // ── FixedUpdate ───────────────────────────────────────────────
        public static void FixedTick()
        {
            if (!Enabled) return;

            GameObject player = GameObject.Find("Player_Human");
            Vector3 playerPos = (object)player != null ? player.transform.position : Vector3.zero;

            for (int i = 0; i < _hazards.Count; i++)
            {
                var h = _hazards[i];
                if ((object)h.Go == null || (object)h.Rb == null) continue;

                // Extra downforce — always
                h.Rb.AddForce(Vector3.down * ExtraGravity, ForceMode.Acceleration);

                // Attraction — full 3D direction toward player, never lift above player height
                if ((object)player != null && AttractionForce > 0f)
                {
                    Vector3 toPlayer = playerPos - h.Go.transform.position;

                    // Only clamp Y if ball is already below player — don't fight gravity on downslopes
                    if (h.Go.transform.position.y < playerPos.y)
                        toPlayer.y = 0f;

                    if (toPlayer.sqrMagnitude > 0.5f)
                        h.Rb.AddForce(toPlayer.normalized * AttractionForce * 4f, ForceMode.Acceleration);
                }
            }
        }

        // ── Spawn ─────────────────────────────────────────────────────
        private static void TrySpawn()
        {
            if (_hazards.Count >= MaxHazards) return;

            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) return;

            Vector3 playerPos = player.transform.position;

            // Don't spawn if any existing hazard is still very close to player
            for (int i = 0; i < _hazards.Count; i++)
            {
                if ((object)_hazards[i].Go == null) continue;
                if (Vector3.Distance(_hazards[i].Go.transform.position, playerPos) < MinSpawnDist)
                {
                    MelonLogger.Msg("[Avalanche] Skipping spawn — hazard still close.");
                    return;
                }
            }

            // Full 360° circle around player at SpawnDistance
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(SpawnDistance * 0.8f, SpawnDistance);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Vector3 spawnXZ = playerPos + offset;

            float terrainY = GetTerrainHeight(spawnXZ);
            Vector3 spawnPos = new Vector3(spawnXZ.x, terrainY + SpawnHeight, spawnXZ.z);

            // Create object
            GameObject hazard = UseBox
                ? GameObject.CreatePrimitive(PrimitiveType.Cube)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hazard.name = "AvalancheHazard";
            hazard.transform.position = spawnPos;
            hazard.transform.localScale = Vector3.one * HazardSize;

            // Colour — rocky grey-brown tint
            var rend = hazard.GetComponent<Renderer>();
            if ((object)rend != null)
                rend.material.color = new Color(
                    Random.Range(0.30f, 0.50f),
                    Random.Range(0.25f, 0.40f),
                    Random.Range(0.20f, 0.35f));

            // Zero-friction physics material
            var col = hazard.GetComponent<Collider>();
            if ((object)col != null)
            {
                var mat = new PhysicMaterial("AvalancheMat");
                mat.staticFriction = 0f;
                mat.dynamicFriction = 0f;
                mat.frictionCombine = PhysicMaterialCombine.Minimum;
                mat.bounciness = 0f;
                mat.bounceCombine = PhysicMaterialCombine.Minimum;
                col.material = mat;
            }

            // Rigidbody
            var rb = hazard.AddComponent<Rigidbody>();
            rb.mass = 80f;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Push toward player direction, always slightly downhill
            Vector3 toward = (playerPos - spawnPos).normalized;
            toward.y = Mathf.Min(toward.y, -0.1f);
            rb.AddForce(toward * ForwardImpulse, ForceMode.Impulse);

            _hazards.Add(new HazardEntry { Go = hazard, Rb = rb, Age = 0f });
            MelonLogger.Msg("[Avalanche] Spawned. Active=" + _hazards.Count
                + " pos=" + spawnPos + " terrainY=" + terrainY);
        }

        // ── Terrain height ────────────────────────────────────────────
        private static float GetTerrainHeight(Vector3 worldPos)
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
                if (h > 1f) return h; // only trust it if non-zero
            }

            // Raycast fallback — cast from high above
            RaycastHit hit;
            Vector3 castFrom = new Vector3(worldPos.x, worldPos.y + 500f, worldPos.z);
            if (Physics.Raycast(castFrom, Vector3.down, out hit, 1000f))
            {
                MelonLogger.Msg("[Avalanche] Raycast terrainY=" + hit.point.y);
                return hit.point.y;
            }

            // Last resort — use player's current Y so ball spawns above them
            GameObject player = GameObject.Find("Player_Human");
            float playerY = (object)player != null ? player.transform.position.y : 0f;
            MelonLogger.Msg("[Avalanche] Using player Y as terrain: " + playerY);
            return playerY;
        }

        // ── Bail ──────────────────────────────────────────────────────
        private static void TriggerBail(GameObject player)
        {
            try
            {
                Vehicle v = player.GetComponent<Vehicle>();
                if ((object)v == null) return;
                var setVel = typeof(Vehicle).GetMethod("SetVelocity",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)setVel != null)
                    setVel.Invoke(v, new object[] { Vector3.up * 15f });
            }
            catch (System.Exception ex)
            { MelonLogger.Warning("[Avalanche] TriggerBail: " + ex.Message); }
        }

        // ── Clear ─────────────────────────────────────────────────────
        public static void ClearAll()
        {
            for (int i = 0; i < _hazards.Count; i++)
                if ((object)_hazards[i].Go != null)
                    GameObject.Destroy(_hazards[i].Go);
            _hazards.Clear();
            _spawnTimer = 0f;
        }

        public static void Reset()
        {
            Enabled = false;
            ClearAll();
        }

        public static int ActiveCount { get { return _hazards.Count; } }
    }
}