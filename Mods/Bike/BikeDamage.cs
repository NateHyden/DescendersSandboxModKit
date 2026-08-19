using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    public static class BikeDamage
    {
        public static bool Enabled { get; private set; } = false;

        // ── Steering offset ───────────────────────────────────────────
        private static float _steerOffset = 0f;
        private static float _offsetDir = 1f;

        // ── Wheel state ───────────────────────────────────────────────
        private static bool _rearWheelGone = false;
        private static int _hardBailCount = 0;
        private static int _hardLandingCount = 0;
        private const int HardBailsToRemove = 3;
        private const int HardLandsToRemove = 5;
        private const float WheelRemoveThreshold = 12f;

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
        private static Quaternion _steerBoneNeutral = Quaternion.identity;
        private static bool _steerNeutralCaptured = false;
        private static bool _boneCacheSearched = false;

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
            ModLog.Feedback("[BikeDamage] -> " + (Enabled ? "ON dir=" + _offsetDir : "OFF"));
        }

        public static void OnBail(int bailCount, float impactSpeed)
        {
            if (!Enabled) return;

            float add = Mathf.Lerp(0.02f, 0.08f, Mathf.Clamp01(impactSpeed / 25f));
            _steerOffset += add * _offsetDir;
            _steerOffset = Mathf.Clamp(_steerOffset, -1f, 1f);

            if (impactSpeed >= WheelRemoveThreshold && !_rearWheelGone)
            {
                _hardBailCount++;
                if (_hardBailCount >= HardBailsToRemove)
                {
                    _rearWheelGone = true;
                    ModLog.Debug("[BikeDamage] Rear wheel removed after " + _hardBailCount + " hard bails!");
                }
            }

            ModLog.Debug("[BikeDamage] Bail #" + bailCount
                + " impact=" + impactSpeed.ToString("F1")
                + " hardBails=" + _hardBailCount + "/" + HardBailsToRemove
                + " steerOffset=" + _steerOffset.ToString("F3")
                + " rearGone=" + _rearWheelGone);
        }

        public static void FixedTick()
        {
            if (!Enabled) return;
            try
            {
                if (!UnityNull.Alive(_cachedRb))
                {
                    _cachedRb = null;
                    GameObject player = GameObject.Find("Player_Human");
                    if (!UnityNull.Alive(player)) return;
                    _cachedRb = player.GetComponentInChildren<Rigidbody>();
                    if (!UnityNull.Alive(_cachedRb)) return;
                }

                float currentSpeed = _cachedRb.velocity.magnitude;

                if (!_steerNeutralCaptured && _steerOffset == 0f)
                {
                    try
                    {
                        GameObject player2 = GameObject.Find("Player_Human");
                        if (UnityNull.Alive(player2))
                        {
                            Transform bm = player2.transform.Find("BikeModel");
                            if (UnityNull.Alive(bm))
                            {
                                Transform sb = bm.Find("root_Jnt/Frame_Jnt/steer_Jnt");
                                if (UnityNull.Alive(sb))
                                {
                                    _steerBone = sb;
                                    _steerBoneNeutral = sb.localRotation;
                                    _steerNeutralCaptured = true;
                                    ModLog.Debug("[BikeDamage] steer_Jnt neutral captured: "
                                        + _steerBoneNeutral.eulerAngles.ToString("F2"));
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (_impactCooldown > 0f) _impactCooldown -= Time.fixedDeltaTime;
                float drop = _lastSpeed - currentSpeed;
                if (drop >= HardLandingDrop && _impactCooldown <= 0f)
                {
                    float add = Mathf.Lerp(0.01f, 0.04f, Mathf.Clamp01(drop / 15f));
                    _steerOffset += add * _offsetDir;
                    _steerOffset = Mathf.Clamp(_steerOffset, -1f, 1f);
                    _impactCooldown = ImpactCooldownSecs;

                    if (!_rearWheelGone)
                    {
                        _hardLandingCount++;
                        if (_hardLandingCount >= HardLandsToRemove)
                        {
                            _rearWheelGone = true;
                            ModLog.Debug("[BikeDamage] Rear wheel removed after "
                                + _hardLandingCount + " hard landings!");
                        }
                    }
                    ModLog.Debug("[BikeDamage] Hard landing drop=" + drop.ToString("F1")
                        + " hardLandings=" + _hardLandingCount + "/" + HardLandsToRemove
                        + " steerOffset=" + _steerOffset.ToString("F3"));
                }
                _lastSpeed = currentSpeed;

                if (_rearWheelGone)
                {
                    if ((object)_rearWheel != null && !UnityNull.Alive(_rearWheel))
                    {
                        _rearWheel = null;
                        _wheelSearched = false;
                    }
                    if (!_wheelSearched) FindRearWheel();
                    if (UnityNull.Alive(_rearWheel) && (object)_radiusField != null && _defaultRearRadius > 0f)
                        _radiusField.SetValue(_rearWheel, _defaultRearRadius * 0.01f);

                    if (UnityNull.Alive(_rearWheel))
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

        public static void Tick()
        {
            if (!Enabled) return;
            bool needsSteer = _steerNeutralCaptured && _steerOffset != 0f;
            bool needsWheel = _rearWheelGone;
            if (!needsSteer && !needsWheel) return;
            try
            {
                if (needsSteer && UnityNull.Alive(_steerBone))
                    _steerBone.localRotation = _steerBoneNeutral;

                if (needsWheel)
                {
                    if ((object)_rearWheelBone != null && !UnityNull.Alive(_rearWheelBone))
                    {
                        _rearWheelBone = null;
                        _boneCacheSearched = false;
                    }
                    if (!_boneCacheSearched)
                    {
                        GameObject player = GameObject.Find("Player_Human");
                        if (!UnityNull.Alive(player)) return;
                        _boneCacheSearched = true;
                        Transform bikeModel = player.transform.Find("BikeModel");
                        if (!UnityNull.Alive(bikeModel)) return;

                        BikeAnimation bikeAnim = bikeModel.GetComponent<BikeAnimation>();
                        if (UnityNull.Alive(bikeAnim))
                        {
                            FieldInfo[] fields = bikeAnim.GetType().GetFields(
                                BindingFlags.Public | BindingFlags.Instance);
                            for (int i = 0; i < fields.Length; i++)
                            {
                                if (!string.Equals(fields[i].FieldType.Name, "Transform",
                                    System.StringComparison.Ordinal)) continue;
                                Transform t = fields[i].GetValue(bikeAnim) as Transform;
                                if (!UnityNull.Alive(t)) continue;
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
                        ModLog.Debug("[BikeDamage] Rear bone: "
                            + (UnityNull.Alive(_rearWheelBone) ? "OK" : "MISSING"));
                    }

                    if (UnityNull.Alive(_rearWheelBone))
                        _rearWheelBone.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                }
            }
            catch { }
        }

        public static float SteerOffset => _steerOffset;

        private static void FindRearWheel()
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if (!UnityNull.Alive(player)) return;
                _wheelSearched = true;
                Wheel[] wheels = player.GetComponentsInChildren<Wheel>();
                if (wheels == null) return;
                for (int i = 0; i < wheels.Length; i++)
                {
                    if (!UnityNull.Alive(wheels[i])) continue;
                    if (string.Equals(wheels[i].gameObject.name, "wheel_front",
                        System.StringComparison.Ordinal)) continue;

                    _rearWheel = wheels[i];
                    if ((object)_radiusField == null)
                        _radiusField = wheels[i].GetType().GetField("HqsqNkJ",
                            BindingFlags.Public | BindingFlags.Instance);
                    if ((object)_radiusField != null && _defaultRearRadius < 0f)
                        _defaultRearRadius = (float)_radiusField.GetValue(wheels[i]);
                    ModLog.Debug("[BikeDamage] Rear Wheel found. defaultRadius="
                        + _defaultRearRadius.ToString("F4"));
                    break;
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[BikeDamage] FindRearWheel: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BikeDamage"); }
        }

        public static void ManualReset()
        {
            try
            {
                if (UnityNull.Alive(_rearWheel) && (object)_radiusField != null && _defaultRearRadius > 0f)
                    _radiusField.SetValue(_rearWheel, _defaultRearRadius);
                if (UnityNull.Alive(_rearWheel) && (object)_rollFrictionProp != null)
                    _rollFrictionProp.SetValue(_rearWheel, 1.0f, null);
                if (UnityNull.Alive(_rearWheelBone))
                    _rearWheelBone.localScale = Vector3.one;
            }
            catch { }
            ResetDamage();
            ModLog.Debug("[BikeDamage] Manual reset — damage cleared.");
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
            _offsetDir = 1f;
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
                { ModLog.Warn("[BikeDamage] VehicleController.FixedUpdate not found."); return; }

                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(
                    typeof(BikeDamage_SteerPatch).GetMethod(
                        "Postfix", BindingFlags.Public | BindingFlags.Static)));
                ModLog.Debug("[BikeDamage] Patched VehicleController.FixedUpdate (steering offset).");
            }
            catch (System.Exception ex) { MelonLogger.Error("[BikeDamage] ApplyPatch: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "BikeDamage"); }
        }
    }

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
            if (!UnityNull.Alive(__instance)) return;

            try
            {
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
                if (!UnityNull.Alive(vehicle)) return;
                if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                if ((object)_steerProp == null)
                    _steerProp = typeof(Vehicle).GetProperty(
                        "swebLyg", BindingFlags.Public | BindingFlags.Instance);
                if ((object)_steerProp == null) return;

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

                if ((object)_groundProp != null)
                {
                    object grounded = _groundProp.GetValue(vehicle, null);
                    if (grounded is bool && !(bool)grounded) return;
                }

                float current = (float)_steerProp.GetValue(vehicle, null);
                float modified = Mathf.Clamp(current + BikeDamage.SteerOffset, -1f, 1f);
                _steerProp.SetValue(vehicle, modified, null);
            }
            catch { }
        }
    }
}

