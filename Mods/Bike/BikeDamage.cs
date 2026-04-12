using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // BikeDamage — progressive damage from crashes.
    //
    // STEERING OFFSET: adds a constant bias to swebLyg (Vehicle steering input property)
    //   every VehicleController.FixedUpdate — same property ReverseSteering uses.
    //   This is true fake steering input: the bike pulls in one direction,
    //   player must counter-steer to go straight. Does NOT affect air physics.
    //   Only active when Vehicle.IsGrounded (via the onGround bool property).
    //   Accumulates in one direction — each crash makes it worse.
    //
    // REAR WHEEL GONE: hard bail impact (>= 12 m/s) shrinks rear wheel.
    //   Bone scale → 0.01 (visual) AND HqsqNkJ physics radius → 0.01 (lowers rear).
    //   Both reapplied every frame to fight physics/animation resets.
    //
    // Hard landings also increase steering offset (detected via velocity drop).
    public static class BikeDamage
    {
        public static bool Enabled { get; private set; } = false;

        // ── Steering offset ───────────────────────────────────────────
        // Added to swebLyg each FixedUpdate when grounded.
        // Positive = pulls right, negative = pulls left. Fixed direction per session.
        private static float _steerOffset = 0f;
        private static float _offsetDir = 1f;   // set once, never flips

        // ── Wheel state ───────────────────────────────────────────────
        private static bool _rearWheelGone = false;
        private static int _hardBailCount = 0;  // bails with impact >= threshold
        private static int _hardLandingCount = 0;  // hard landings from FixedTick
        private const int HardBailsToRemove = 3;  // wheel gone after 3 hard bails
        private const int HardLandsToRemove = 5;  // OR 5 hard landings
        private const float WheelRemoveThreshold = 12f; // m/s impact = counts as hard bail

        // ── Hard landing detection ────────────────────────────────────
        private static float _lastSpeed = 0f;
        private static float _impactCooldown = 0f;
        private const float HardLandingDrop = 4f;
        private const float ImpactCooldownSecs = 0.5f;

        // ── Physics wheel radius ──────────────────────────────────────
        private static Wheel _rearWheel = null;
        private static FieldInfo _radiusField = null;
        private static float _defaultRearRadius = -1f;
        private static bool _wheelSearched = false;

        // ── Visual bones ──────────────────────────────────────────────
        private static Transform _rearWheelBone = null;
        private static Transform _steerBone = null;
        // Captured BEFORE any offset — restoring this keeps bars visually straight
        private static Quaternion _steerBoneNeutral = Quaternion.identity;
        private static bool _steerNeutralCaptured = false;
        private static bool _boneCacheSearched = false;

        // ── Rear wheel roll friction (slippery when gone) ─────────────
        private static PropertyInfo _rollFrictionProp = null;

        // ── Rigidbody ─────────────────────────────────────────────────
        private static Rigidbody _cachedRb = null;

        // ── Public API ────────────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            if (!Enabled)
                ResetDamage();
            else
                _offsetDir = (UnityEngine.Random.value > 0.5f) ? 1f : -1f;
            MelonLogger.Msg("[BikeDamage] -> " + (Enabled ? "ON dir=" + _offsetDir : "OFF"));
        }

        // Called by SessionTrackers.OnBailDetected()
        public static void OnBail(int bailCount, float impactSpeed)
        {
            if (!Enabled) return;

            // Accumulate offset in fixed direction
            float add = Mathf.Lerp(0.02f, 0.08f, Mathf.Clamp01(impactSpeed / 25f));
            _steerOffset += add * _offsetDir;
            _steerOffset = Mathf.Clamp(_steerOffset, -1f, 1f);

            // Count hard bails toward wheel removal
            if (impactSpeed >= WheelRemoveThreshold && !_rearWheelGone)
            {
                _hardBailCount++;
                if (_hardBailCount >= HardBailsToRemove)
                {
                    _rearWheelGone = true;
                    MelonLogger.Msg("[BikeDamage] Rear wheel removed after " + _hardBailCount + " hard bails!");
                }
            }

            MelonLogger.Msg("[BikeDamage] Bail #" + bailCount
                + " impact=" + impactSpeed.ToString("F1")
                + " hardBails=" + _hardBailCount + "/" + HardBailsToRemove
                + " steerOffset=" + _steerOffset.ToString("F3")
                + " rearGone=" + _rearWheelGone);
        }

        // Called from OnFixedUpdate — speed tracking + wheel physics reapply
        public static void FixedTick()
        {
            if (!Enabled) return;
            try
            {
                if ((object)_cachedRb == null)
                {
                    GameObject player = GameObject.Find("Player_Human");
                    if ((object)player == null) return;
                    _cachedRb = player.GetComponentInChildren<Rigidbody>();
                    if ((object)_cachedRb == null) return;
                }

                float currentSpeed = _cachedRb.velocity.magnitude;

                // Capture steer bone neutral rotation BEFORE any offset accumulates
                if (!_steerNeutralCaptured && _steerOffset == 0f)
                {
                    try
                    {
                        GameObject player2 = GameObject.Find("Player_Human");
                        if ((object)player2 != null)
                        {
                            Transform bm = player2.transform.Find("BikeModel");
                            if ((object)bm != null)
                            {
                                Transform sb = bm.Find("root_Jnt/Frame_Jnt/steer_Jnt");
                                if ((object)sb != null)
                                {
                                    _steerBone = sb;
                                    _steerBoneNeutral = sb.localRotation;
                                    _steerNeutralCaptured = true;
                                    MelonLogger.Msg("[BikeDamage] steer_Jnt neutral captured: "
                                        + _steerBoneNeutral.eulerAngles.ToString("F2"));
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Hard landing detection
                if (_impactCooldown > 0f) _impactCooldown -= Time.fixedDeltaTime;
                float drop = _lastSpeed - currentSpeed;
                if (drop >= HardLandingDrop && _impactCooldown <= 0f)
                {
                    float add = Mathf.Lerp(0.01f, 0.04f, Mathf.Clamp01(drop / 15f));
                    _steerOffset += add * _offsetDir;
                    _steerOffset = Mathf.Clamp(_steerOffset, -1f, 1f);
                    _impactCooldown = ImpactCooldownSecs;

                    // Count hard landings toward wheel removal
                    if (!_rearWheelGone)
                    {
                        _hardLandingCount++;
                        if (_hardLandingCount >= HardLandsToRemove)
                        {
                            _rearWheelGone = true;
                            MelonLogger.Msg("[BikeDamage] Rear wheel removed after "
                                + _hardLandingCount + " hard landings!");
                        }
                    }
                    MelonLogger.Msg("[BikeDamage] Hard landing drop=" + drop.ToString("F1")
                        + " hardLandings=" + _hardLandingCount + "/" + HardLandsToRemove
                        + " steerOffset=" + _steerOffset.ToString("F3"));
                }
                _lastSpeed = currentSpeed;

                // Reapply wheel physics radius + kill friction (fights physics reset)
                if (_rearWheelGone)
                {
                    if (!_wheelSearched) FindRearWheel();
                    if ((object)_rearWheel != null && (object)_radiusField != null && _defaultRearRadius > 0f)
                        _radiusField.SetValue(_rearWheel, _defaultRearRadius * 0.01f);

                    // Make rear wheel slippery — near-zero roll friction
                    if ((object)_rearWheel != null)
                    {
                        if ((object)_rollFrictionProp == null)
                            _rollFrictionProp = typeof(Wheel).GetProperty(
                                "WbmnXfG", BindingFlags.Public | BindingFlags.Instance);
                        if ((object)_rollFrictionProp != null)
                            _rollFrictionProp.SetValue(_rearWheel, 0.0f, null);
                    }
                }
            }
            catch { }
        }

        // Called from OnLateUpdate
        public static void Tick()
        {
            if (!Enabled) return;
            bool needsSteer = _steerNeutralCaptured && _steerOffset != 0f;
            bool needsWheel = _rearWheelGone;
            if (!needsSteer && !needsWheel) return;
            try
            {
                // Restore handlebar visual to neutral rotation (bars appear straight)
                // steer_Jnt was cached in FixedTick before any offset was applied
                if (needsSteer && (object)_steerBone != null)
                    _steerBone.localRotation = _steerBoneNeutral;

                // Rear wheel bone scale — shrink visually (fights Animation reset)
                if (needsWheel)
                {
                    if (!_boneCacheSearched)
                    {
                        _boneCacheSearched = true;
                        GameObject player = GameObject.Find("Player_Human");
                        if ((object)player == null) return;
                        Transform bikeModel = player.transform.Find("BikeModel");
                        if ((object)bikeModel == null) return;

                        BikeAnimation bikeAnim = bikeModel.GetComponent<BikeAnimation>();
                        if ((object)bikeAnim != null)
                        {
                            FieldInfo[] fields = bikeAnim.GetType().GetFields(
                                BindingFlags.Public | BindingFlags.Instance);
                            for (int i = 0; i < fields.Length; i++)
                            {
                                if (!string.Equals(fields[i].FieldType.Name, "Transform",
                                    System.StringComparison.Ordinal)) continue;
                                Transform t = fields[i].GetValue(bikeAnim) as Transform;
                                if ((object)t == null) continue;
                                if (string.Equals(t.name, "backWheel_Jnt",
                                    System.StringComparison.Ordinal))
                                { _rearWheelBone = t; break; }
                            }
                        }
                        else
                        {
                            _rearWheelBone = bikeModel.Find(
                                "root_Jnt/Frame_Jnt/backWheelRotator_Jnt/BackWheelShockAbsorber_Jnt/backWheel_Jnt");
                        }
                        MelonLogger.Msg("[BikeDamage] Rear bone: "
                            + ((object)_rearWheelBone != null ? "OK" : "MISSING"));
                    }

                    if ((object)_rearWheelBone != null)
                        _rearWheelBone.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                }
            }
            catch { }
        }

        // Exposed to BikeDamage_SteerPatch — the steering offset to apply this frame
        public static float SteerOffset => _steerOffset;

        private static void FindRearWheel()
        {
            _wheelSearched = true;
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) return;
                Wheel[] wheels = player.GetComponentsInChildren<Wheel>();
                if (wheels == null) return;
                for (int i = 0; i < wheels.Length; i++)
                {
                    if ((object)wheels[i] == null) continue;
                    if (string.Equals(wheels[i].gameObject.name, "wheel_front",
                        System.StringComparison.Ordinal)) continue;

                    _rearWheel = wheels[i];
                    if ((object)_radiusField == null)
                        _radiusField = wheels[i].GetType().GetField("HqsqNkJ",
                            BindingFlags.Public | BindingFlags.Instance);
                    if ((object)_radiusField != null && _defaultRearRadius < 0f)
                        _defaultRearRadius = (float)_radiusField.GetValue(wheels[i]);
                    MelonLogger.Msg("[BikeDamage] Rear Wheel found. defaultRadius="
                        + _defaultRearRadius.ToString("F4"));
                    break;
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[BikeDamage] FindRearWheel: " + ex.Message); }
        }

        public static void ManualReset()
        {
            try
            {
                if ((object)_rearWheel != null && (object)_radiusField != null && _defaultRearRadius > 0f)
                    _radiusField.SetValue(_rearWheel, _defaultRearRadius);
                // Restore roll friction (default ~1.0)
                if ((object)_rearWheel != null && (object)_rollFrictionProp != null)
                    _rollFrictionProp.SetValue(_rearWheel, 1.0f, null);
                if ((object)_rearWheelBone != null)
                    _rearWheelBone.localScale = Vector3.one;
            }
            catch { }
            ResetDamage();
            MelonLogger.Msg("[BikeDamage] Manual reset — damage cleared.");
        }

        private static void ResetDamage()
        {
            _steerOffset = 0f;
            _rearWheelGone = false;
            _hardBailCount = 0;
            _hardLandingCount = 0;
            _lastSpeed = 0f;
            _impactCooldown = 0f;
        }

        public static void Reset()
        {
            Enabled = false;
            ResetDamage();
            _offsetDir = 1f; // reset direction on full mod reset
            ClearBoneCache();
        }

        public static void ClearBoneCache()
        {
            _cachedRb = null;
            _rearWheelBone = null;
            _steerBone = null;
            _steerNeutralCaptured = false;
            _boneCacheSearched = false;
            _rearWheel = null;
            _radiusField = null;
            _rollFrictionProp = null;
            _defaultRearRadius = -1f;
            _wheelSearched = false;
            _lastSpeed = 0f;
            _impactCooldown = 0f;
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo fixedUpdate = typeof(VehicleController).GetMethod(
                    "FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)fixedUpdate == null)
                { MelonLogger.Warning("[BikeDamage] VehicleController.FixedUpdate not found."); return; }

                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(
                    typeof(BikeDamage_SteerPatch).GetMethod(
                        "Postfix", BindingFlags.Public | BindingFlags.Static)));
                MelonLogger.Msg("[BikeDamage] Patched VehicleController.FixedUpdate (steering offset).");
            }
            catch (System.Exception ex) { MelonLogger.Error("[BikeDamage] ApplyPatch: " + ex.Message); }
        }
    }

    // Harmony postfix — runs after VehicleController.FixedUpdate each physics frame.
    // Adds a small steering offset to swebLyg (steering input on Vehicle).
    // Only fires when grounded so airtime tricks are unaffected.
    public static class BikeDamage_SteerPatch
    {
        private static FieldInfo _vehicleField = null;
        private static PropertyInfo _steerProp = null;
        private static PropertyInfo _groundProp = null;
        private static bool _groundPropSearched = false;

        public static void Postfix(VehicleController __instance)
        {
            if (!BikeDamage.Enabled) return;
            if (BikeDamage.SteerOffset == 0f) return;
            if ((object)__instance == null) return;

            try
            {
                // Cache Vehicle field on VehicleController
                if ((object)_vehicleField == null)
                {
                    FieldInfo[] fields = typeof(VehicleController).GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (string.Equals(fields[i].FieldType.Name, "Vehicle",
                            System.StringComparison.Ordinal))
                        { _vehicleField = fields[i]; break; }
                    }
                    if ((object)_vehicleField == null) return;
                }

                Vehicle vehicle = _vehicleField.GetValue(__instance) as Vehicle;
                if ((object)vehicle == null) return;
                if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                // Cache steer property (swebLyg)
                if ((object)_steerProp == null)
                    _steerProp = typeof(Vehicle).GetProperty(
                        "swebLyg", BindingFlags.Public | BindingFlags.Instance);
                if ((object)_steerProp == null) return;

                // Cache ground bool property (starts with 'T' — same as SessionTrackers)
                if (!_groundPropSearched)
                {
                    _groundPropSearched = true;
                    PropertyInfo[] props = typeof(Vehicle).GetProperties(
                        BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (!props[i].CanRead) continue;
                        if (!string.Equals(props[i].PropertyType.Name, "Boolean",
                            System.StringComparison.Ordinal)) continue;
                        if (props[i].Name.StartsWith("T"))
                        { _groundProp = props[i]; break; }
                    }
                }

                // Only apply when grounded
                if ((object)_groundProp != null)
                {
                    object grounded = _groundProp.GetValue(vehicle, null);
                    if (grounded is bool && !(bool)grounded) return;
                }

                // Add steering offset — player must steer against it to go straight
                float current = (float)_steerProp.GetValue(vehicle, null);
                float modified = Mathf.Clamp(current + BikeDamage.SteerOffset, -1f, 1f);
                _steerProp.SetValue(vehicle, modified, null);
            }
            catch { }
        }
    }
}