using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class SessionTrackers
    {
        // ── Session Timer ─────────────────────────────────────────────────
        private static float _sessionStartTime = -1f;

        public static float SessionTime
        {
            get
            {
                if (_sessionStartTime < 0f) return 0f;
                return Time.unscaledTime - _sessionStartTime;
            }
        }

        public static string SessionTimeDisplay
        {
            get
            {
                float t = SessionTime;
                if (t <= 0f) return "--:--";
                int mins = (int)(t / 60f);
                int secs = (int)(t % 60f);
                return mins.ToString("D2") + ":" + secs.ToString("D2");
            }
        }

        // ── Bail Counter ──────────────────────────────────────────────────
        public static int BailCount { get; private set; } = 0;
        private static float _lastBailTime = -999f;
        private const float BailCooldown = 1.0f; // ignore duplicate Bail() calls within 1 second

        public static string BailCountDisplay
        {
            get { return BailCount.ToString(); }
        }

        // ── Checkpoint Counter ────────────────────────────────────────
        public static int CheckpointCount { get; private set; } = 0;
        private static int _lastCpIndex = -1;

        // ]w\u0082Jbz} is an AUTO-PROPERTY on VehicleEvents, NOT a field.
        // Must use GetProperties(), not GetFields().
        private static System.Reflection.PropertyInfo _cpIndexProp = null;
        private static bool _cpPropSearchDone = false;

        private static VehicleEvents _cachedVE = null;

        public static string CheckpointCountDisplay
        {
            get { return CheckpointCount.ToString(); }
        }

        // Poll VehicleEvents.]w\u0082Jbz} (auto-property, furthest checkpoint index) each frame.
        // When the value increases a checkpoint was crossed.
        public static void CheckpointTick()
        {
            try
            {
                // ── 1. Ensure we have a live VehicleEvents instance ──────────
                if ((object)_cachedVE == null || !(bool)(UnityEngine.Object)_cachedVE)
                {
                    _cachedVE = UnityEngine.Object.FindObjectOfType<VehicleEvents>();
                    if ((object)_cachedVE == null) return;
                    _cpIndexProp = null;
                    _cpPropSearchDone = false;
                }

                // ── 2. Find the ]w\u0082Jbz} PROPERTY (not a field!) ─────────
                if ((object)_cpIndexProp == null && !_cpPropSearchDone)
                {
                    _cpPropSearchDone = true;
                    var props = typeof(VehicleEvents).GetProperties(
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    for (int p = 0; p < props.Length; p++)
                    {
                        if (!props[p].CanRead) continue;
                        if (!string.Equals(props[p].PropertyType.Name, "Int32",
                            System.StringComparison.Ordinal)) continue;
                        string n = props[p].Name;
                        // ]w\u0082Jbz} — ']' = 93, 'w' = 119
                        if (n.Length >= 2 && n[0] == ']' && n[1] == 'w')
                        {
                            _cpIndexProp = props[p];
                            break;
                        }
                    }
                }

                if ((object)_cpIndexProp == null) return;

                // ── 3. Read value and detect crossings ───────────────────────
                int idx = (int)_cpIndexProp.GetValue(_cachedVE, null);

                if (_lastCpIndex < 0)
                {
                    _lastCpIndex = idx;
                    return;
                }

                if (idx > _lastCpIndex)
                {
                    CheckpointCount += 1;
                }
                else if (idx == 0 && _lastCpIndex > 0)
                {
                    // Respawn reset — don't decrement count
                }

                _lastCpIndex = idx;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[CP] Exception: " + ex.Message);
                _cachedVE = null;
                _cpIndexProp = null;
                _cpPropSearchDone = false;
            }
        }

        public static void OnCheckpointDetected()
        {
            CheckpointCount++;
            MelonLogger.Msg("[SessionTrackers] Checkpoint #" + CheckpointCount);
        }

        // Called by Harmony postfix on Cyclist.Bail()
        public static void OnBailDetected()
        {
            // Game calls Bail() twice per wipeout from two VehicleController code paths
            // Debounce so we only count once per actual crash
            float now = Time.unscaledTime;
            if (now - _lastBailTime < BailCooldown) return;
            _lastBailTime = now;
            BailCount++;
            MelonLogger.Msg("[SessionTrackers] Bail #" + BailCount);

            // Capture impact speed for BikeDamage before respawn resets velocity
            float impactSpeed = 0f;
            if ((object)_cachedRb != null)
                impactSpeed = _cachedRb.velocity.magnitude;

            SlowMoOnBail.OnBail();
            InstantRespawn.OnBail();
            BikeDamage.OnBail(BailCount, impactSpeed);
        }

        // ── Longest Airtime ───────────────────────────────────────────────
        public static float LongestAirtime { get; private set; } = 0f;
        private static float _currentAirtimeStart = -1f;
        private static bool _wasOnGround = true;

        // ── G-Force Tracker ──────────────────────────────────────────────
        public static float CurrentGForce { get; private set; } = 0f;
        public static float PeakGForce { get; private set; } = 0f;
        private static Vector3 _lastVelocity = Vector3.zero;
        private static bool _hasLastVelocity = false;

        // Cached for ground check — same property NoSpeedCap uses
        private static PropertyInfo _onGroundProp = null;
        private static bool _groundPropCached = false;

        // Cached refs
        private static GameObject _cachedPlayer = null;
        private static Vehicle _cachedVehicle = null;
        private static Rigidbody _cachedRb = null;  // cached — eliminates GetComponent every frame

        public static string AirtimeDisplay
        {
            get { return LongestAirtime > 0.1f ? LongestAirtime.ToString("F1") + "s" : "--"; }
        }

        public static string GForceDisplay
        {
            get { return CurrentGForce.ToString("F1") + "G"; }
        }

        public static string PeakGForceDisplay
        {
            get { return PeakGForce > 0.1f ? PeakGForce.ToString("F1") + "G" : "--"; }
        }

        // ── Tick (call from OnUpdate) ─────────────────────────────────────
        public static void Tick()
        {
            try
            {
                // Start session timer on first tick
                if (_sessionStartTime < 0f)
                    _sessionStartTime = Time.unscaledTime;

                // Re-find player if cache is stale
                if ((object)_cachedPlayer == null || !_cachedPlayer.activeInHierarchy)
                {
                    _cachedPlayer = GameObject.Find("Player_Human");
                    _cachedVehicle = null;
                    _cachedRb = null;  // invalidate Rigidbody cache with player
                }
                if ((object)_cachedPlayer == null) return;

                if ((object)_cachedVehicle == null)
                    _cachedVehicle = _cachedPlayer.GetComponent<Vehicle>();
                if ((object)_cachedVehicle == null) return;

                // ── Airtime tracking ─────────────────────────────────────
                bool onGround = true;

                if (!_groundPropCached)
                {
                    _groundPropCached = true;
                    // Find the onGround bool property — starts with 'T' (TDEX{ib)
                    PropertyInfo[] props = _cachedVehicle.GetType().GetProperties(
                        BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (!props[i].CanRead) continue;
                        if (!string.Equals(props[i].PropertyType.Name, "Boolean",
                            System.StringComparison.Ordinal)) continue;
                        if (props[i].Name.StartsWith("T"))
                        {
                            _onGroundProp = props[i];
                            MelonLogger.Msg("[SessionTrackers] Found onGround prop: " + props[i].Name);
                            break;
                        }
                    }
                }

                if ((object)_onGroundProp != null)
                {
                    object val = _onGroundProp.GetValue(_cachedVehicle, null);
                    if (val is bool) onGround = (bool)val;
                }

                if (!onGround)
                {
                    // In the air
                    if (_wasOnGround)
                    {
                        // Just left ground
                        _currentAirtimeStart = Time.unscaledTime;
                    }
                    else if (_currentAirtimeStart > 0f)
                    {
                        float airtime = Time.unscaledTime - _currentAirtimeStart;
                        if (airtime > LongestAirtime)
                            LongestAirtime = airtime;
                    }
                }
                else
                {
                    // On ground — finalize any airtime
                    if (!_wasOnGround && _currentAirtimeStart > 0f)
                    {
                        float airtime = Time.unscaledTime - _currentAirtimeStart;
                        if (airtime > LongestAirtime)
                            LongestAirtime = airtime;
                    }
                    _currentAirtimeStart = -1f;
                }

                _wasOnGround = onGround;

                // ── G-Force tracking ──────────────────────────────────────
                if ((object)_cachedRb == null)
                    _cachedRb = _cachedPlayer.GetComponentInChildren<Rigidbody>();
                if ((object)_cachedRb != null)
                {
                    Vector3 currentVelocity = _cachedRb.velocity;
                    if (_hasLastVelocity)
                    {
                        float dt = Time.fixedDeltaTime;
                        if (dt > 0.0001f)
                        {
                            Vector3 accel = (currentVelocity - _lastVelocity) / dt;
                            CurrentGForce = accel.magnitude / 9.81f;
                            if (CurrentGForce > PeakGForce)
                                PeakGForce = CurrentGForce;
                        }
                    }
                    _lastVelocity = currentVelocity;
                    _hasLastVelocity = true;
                }
            }
            catch { }
        }

        // ── Reset ─────────────────────────────────────────────────────────
        public static void Reset()
        {
            _sessionStartTime = -1f;
            BailCount = 0;
            CheckpointCount = 0;
            _lastCpIndex = -1;
            _cachedVE = null;
            _cpIndexProp = null;
            _cpPropSearchDone = false;
            _lastBailTime = -999f;
            LongestAirtime = 0f;
            _currentAirtimeStart = -1f;
            _wasOnGround = true;
            _cachedPlayer = null;
            _cachedVehicle = null;
            _cachedRb = null;
            _groundPropCached = false;
            _onGroundProp = null;
            CurrentGForce = 0f;
            PeakGForce = 0f;
            _lastVelocity = Vector3.zero;
            _hasLastVelocity = false;
        }

        public static void ResetBails() { BailCount = 0; _lastBailTime = -999f; }
        public static void ResetCheckpoints() { CheckpointCount = 0; _lastCpIndex = -1; }
        public static void ResetAirtime() { LongestAirtime = 0f; _currentAirtimeStart = -1f; }
        public static void ResetGForce() { PeakGForce = 0f; CurrentGForce = 0f; }

        // ── Harmony Patch for bail detection ──────────────────────────────
        // Patches Cyclist.Bail() — the actual crash/bail method
        // NOT Vehicle.Reset(bool) which is manual reset
        public static void ApplyBailPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                // Cyclist.Bail() is public and unobfuscated
                MethodInfo bailMethod = typeof(Cyclist).GetMethod("Bail",
                    BindingFlags.Public | BindingFlags.Instance);

                if ((object)bailMethod == null)
                {
                    MelonLogger.Warning("[SessionTrackers] Cyclist.Bail() not found.");
                    return;
                }

                MethodInfo postfix = typeof(BailDetector_Patch).GetMethod("Postfix",
                    BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(bailMethod, postfix: new HarmonyMethod(postfix));
                MelonLogger.Msg("[SessionTrackers] Patched Cyclist.Bail() for bail detection.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SessionTrackers] ApplyBailPatch: " + ex.Message);
            }
        }
        public static void ApplyCheckpointPatch(HarmonyLib.Harmony harmony)
        {
            // Checkpoint counting uses polling via Tick() — no Harmony patch needed.
        }
    }

    // Harmony postfix on VehicleEvents.E\u0081\u007EPyeF(int)
    // Fires only when a checkpoint is genuinely hit (after all index guards).
    // Checkpoint counting is done via polling in SessionTrackers.Tick()
    public static class CheckpointDetector_Patch
    {
        public static void Postfix() { }
    }

    // Harmony postfix on Cyclist.Bail()
    // Fires when the player actually crashes/wipes out
    public static class BailDetector_Patch
    {
        public static void Postfix(Cyclist __instance)
        {
            if ((object)__instance == null) return;

            // Only count local player bails
            if (!string.Equals(__instance.gameObject.name, "Player_Human",
                System.StringComparison.Ordinal)) return;

            SessionTrackers.OnBailDetected();
        }
    }
}