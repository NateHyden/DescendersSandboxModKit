using MelonLoader;
using DescendersModMenu;
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
        public static float AttractionForce = 8f;
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

        public static float SurvivalTime = 0f;

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
                _spawnTimer = SpawnInterval;
                _modeTimer = 0f;
                SurvivalTime = 0f;
                ModLog.Debug("[Avalanche] ON");
            }
            else
            {
                ClearAll();
                ModLog.Debug("[Avalanche] OFF");
            }
        }

        // ── Update ────────────────────────────────────────────────────
        public static void Tick()
        {
            if (!Enabled) return;

            _modeTimer += Time.deltaTime;
            SurvivalTime += Time.deltaTime;
            _spawnTimer += Time.deltaTime;

            float interval = SpawnInterval;
            if (DifficultyScale)
            {
                if (_modeTimer > 120f) interval = Mathf.Max(1f, SpawnInterval * 0.4f);
                else if (_modeTimer > 60f) interval = Mathf.Max(1f, SpawnInterval * 0.6f);
                else if (_modeTimer > 30f) interval = Mathf.Max(1f, SpawnInterval * 0.8f);
            }

            if (_spawnTimer >= interval) { _spawnTimer = 0f; TrySpawn(); }

            GameObject player = GameObject.Find("Player_Human");
            Vector3 playerPos = UnityNull.Alive(player) ? player.transform.position : Vector3.zero;
            Cyclist cyclist = null;
            if (UnityNull.Alive(player) && InstantFail)
                cyclist = player.GetComponent<Cyclist>();

            for (int i = _hazards.Count - 1; i >= 0; i--)
            {
                var h = _hazards[i];
                if (!UnityNull.Alive(h.Go)) { _hazards.RemoveAt(i); continue; }

                h.Age += Time.deltaTime;

                bool tooOld = h.Age > HazardLifetime;
                bool tooFar = false;
                bool stuck = false;

                if (UnityNull.Alive(player))
                {
                    float dist = Vector3.Distance(h.Go.transform.position, playerPos);
                    tooFar = dist > DespawnDist;

                    if (InstantFail && dist < (HazardSize * 0.5f) + 1.5f)
                    {
                        ModLog.Debug("[Avalanche] Hit! Bailing.");
                        TriggerBail(player, cyclist);
                    }
                }

                if (UnityNull.Alive(h.Rb))
                    stuck = h.Age > 12f && h.Rb.velocity.magnitude < 0.5f;

                if (tooOld || tooFar || stuck)
                {
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
            Vector3 playerPos = UnityNull.Alive(player) ? player.transform.position : Vector3.zero;

            for (int i = 0; i < _hazards.Count; i++)
            {
                var h = _hazards[i];
                if (!UnityNull.Alive(h.Go) || !UnityNull.Alive(h.Rb)) continue;

                h.Rb.AddForce(Vector3.down * ExtraGravity, ForceMode.Acceleration);

                // Chase rider — pull hard horizontally into their path.
                if (UnityNull.Alive(player) && AttractionForce > 0f)
                {
                    Vector3 toPlayer = playerPos - h.Go.transform.position;
                    toPlayer.y = 0f;
                    if (toPlayer.sqrMagnitude > 0.5f)
                        h.Rb.AddForce(toPlayer.normalized * AttractionForce * 5f, ForceMode.Acceleration);
                }
            }
        }

        // ── Spawn (ahead on predicted path — fall into trajectory) ────
        private static void TrySpawn()
        {
            if (_hazards.Count >= MaxHazards) return;

            GameObject player = GameObject.Find("Player_Human");
            if (!UnityNull.Alive(player)) return;

            Vector3 playerPos = player.transform.position;

            for (int i = 0; i < _hazards.Count; i++)
            {
                if (!UnityNull.Alive(_hazards[i].Go)) continue;
                if (Vector3.Distance(_hazards[i].Go.transform.position, playerPos) < MinSpawnDist)
                {
                    ModLog.Debug("[Avalanche] Skipping spawn — hazard still close.");
                    return;
                }
            }

            // Predict travel direction from velocity, fall back to facing.
            Vector3 travel = player.transform.forward;
            travel.y = 0f;
            Rigidbody prb = player.GetComponentInChildren<Rigidbody>();
            float speed = 8f;
            if ((object)prb != null)
            {
                Vector3 hVel = new Vector3(prb.velocity.x, 0f, prb.velocity.z);
                speed = Mathf.Max(6f, hVel.magnitude);
                if (hVel.sqrMagnitude > 4f)
                    travel = hVel.normalized;
            }
            if (travel.sqrMagnitude < 0.01f) travel = Vector3.forward;
            travel.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, travel).normalized;

            // Aim where the rider will be in ~1–1.6s, with lateral scatter.
            float lookAhead = Mathf.Clamp(SpawnDistance, 10f, 28f);
            float timeHint = lookAhead / Mathf.Max(speed, 6f);
            timeHint = Mathf.Clamp(timeHint, 0.8f, 1.8f);
            float lat = Random.Range(-SpawnRadius, SpawnRadius);

            Vector3 impactXZ = playerPos + travel * (speed * timeHint) + right * lat;
            // Mix: mostly ahead to dodge, occasional behind-chase spawn.
            bool chaseFromBehind = Random.value < 0.28f;
            if (chaseFromBehind)
            {
                impactXZ = playerPos - travel * Random.Range(SpawnDistance * 0.6f, SpawnDistance)
                    + right * Random.Range(-SpawnRadius * 0.5f, SpawnRadius * 0.5f);
            }

            float terrainY = GetTerrainHeight(impactXZ);
            float dropH = chaseFromBehind ? SpawnHeight : Mathf.Max(8f, SpawnHeight * 0.7f);
            Vector3 spawnPos = new Vector3(impactXZ.x, terrainY + dropH, impactXZ.z);

            GameObject hazard = UseBox
                ? GameObject.CreatePrimitive(PrimitiveType.Cube)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hazard.name = "AvalancheHazard";
            hazard.transform.position = spawnPos;
            hazard.transform.localScale = Vector3.one * HazardSize;

            var rend = hazard.GetComponent<Renderer>();
            if ((object)rend != null)
                rend.material.color = new Color(
                    Random.Range(0.55f, 0.75f),
                    Random.Range(0.55f, 0.75f),
                    Random.Range(0.60f, 0.80f));

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

            var rb = hazard.AddComponent<Rigidbody>();
            rb.mass = 80f;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Drop hard onto the intercept + shove toward the rider's path.
            Vector3 downSlam = Vector3.down * (ForwardImpulse * 1.4f);
            Vector3 towardRider = playerPos - spawnPos;
            towardRider.y = 0f;
            if (towardRider.sqrMagnitude > 0.01f)
                towardRider = towardRider.normalized * ForwardImpulse;
            else
                towardRider = -travel * ForwardImpulse;
            rb.AddForce(downSlam + towardRider, ForceMode.Impulse);

            _hazards.Add(new HazardEntry { Go = hazard, Rb = rb, Age = 0f });
            ModLog.Debug("[Avalanche] Spawned "
                + (chaseFromBehind ? "chase" : "ahead")
                + " active=" + _hazards.Count
                + " lookAhead=" + timeHint.ToString("F1") + "s");
        }

        // ── Terrain height ────────────────────────────────────────────
        private static float GetTerrainHeight(Vector3 worldPos)
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
            float playerY = (object)player != null ? player.transform.position.y : 0f;
            return playerY;
        }

        // ── Bail ──────────────────────────────────────────────────────
        private static void TriggerBail(GameObject player, Cyclist cyclist)
        {
            try
            {
                if ((object)cyclist != null)
                {
                    cyclist.Bail();
                    return;
                }
                Cyclist c = player.GetComponent<Cyclist>();
                if ((object)c != null) { c.Bail(); return; }

                Vehicle v = player.GetComponent<Vehicle>();
                if ((object)v == null) return;
                var setVel = typeof(Vehicle).GetMethod("SetVelocity",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)setVel != null)
                    setVel.Invoke(v, new object[] { Vector3.up * 15f });
            }
            catch (System.Exception ex)
            { ModLog.Warn("[Avalanche] TriggerBail: " + ex.Message); }
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
