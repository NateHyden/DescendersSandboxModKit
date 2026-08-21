using System.Reflection;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class BouncyBike
    {
        public static bool Enabled = false;
        public static int BouncinessLevel { get; private set; } = 5;
        public const int MaxLevel = 20;

        private const float MinImpactSpeed = 4f;

        private static float Restitution => 0.1f + (BouncinessLevel - 1) * (0.9f / (MaxLevel - 1));

        private static PropertyInfo _onGroundProp = null;
        private static bool _groundPropCached = false;
        private static bool _wasOnGround = true;

        private static GameObject _cachedPlayer = null;
        private static Vehicle _cachedVehicle = null;
        private static Rigidbody _cachedRb = null;

        private static float _lastVerticalVelocity = 0f;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (!Enabled) ClearRuntimeState();
            ModLog.Feedback("[BouncyBike] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void IncreaseLevel() { if (BouncinessLevel < MaxLevel) BouncinessLevel++; }
        public static void DecreaseLevel() { if (BouncinessLevel > 1) BouncinessLevel--; }
        public static void SetLevel(int level) { BouncinessLevel = Mathf.Clamp(level, 1, MaxLevel); }

        public static void Tick()
        {
            if (!Enabled) return;
            try
            {
                if (!UnityNull.Alive(_cachedPlayer) || !_cachedPlayer.activeInHierarchy)
                {
                    _cachedPlayer = GameObject.Find("Player_Human");
                    _cachedVehicle = null;
                    _cachedRb = null;
                }
                if (!UnityNull.Alive(_cachedPlayer)) return;

                if (!UnityNull.Alive(_cachedVehicle))
                    _cachedVehicle = _cachedPlayer.GetComponent<Vehicle>();
                if (!UnityNull.Alive(_cachedVehicle)) return;

                if (!UnityNull.Alive(_cachedRb))
                    _cachedRb = _cachedPlayer.GetComponentInChildren<Rigidbody>();
                if (!UnityNull.Alive(_cachedRb)) return;

                if (!_groundPropCached)
                {
                    _groundPropCached = true;
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
                            break;
                        }
                    }
                }

                bool onGround = true;
                if ((object)_onGroundProp != null)
                {
                    object val = _onGroundProp.GetValue(_cachedVehicle, null);
                    if (val is bool) onGround = (bool)val;
                }

                if (!onGround)
                {
                    _lastVerticalVelocity = _cachedRb.velocity.y;
                }
                else if (!_wasOnGround)
                {
                    float impactSpeed = -_lastVerticalVelocity;
                    if (impactSpeed > MinImpactSpeed)
                    {
                        Vector3 v = _cachedRb.velocity;
                        v.y = impactSpeed * Restitution;
                        _cachedRb.velocity = v;
                    }
                }

                _wasOnGround = onGround;
            }
            catch (System.Exception ex) { MelonLogger.Error("[BouncyBike] Tick: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "BouncyBike"); }
        }

        private static void ClearRuntimeState()
        {
            _wasOnGround = true;
            _lastVerticalVelocity = 0f;
        }

        public static void ClearCache()
        {
            _cachedPlayer = null; _cachedVehicle = null; _cachedRb = null;
            _onGroundProp = null; _groundPropCached = false;
            ClearRuntimeState();
        }

        public static void Reset()
        {
            Enabled = false;
            BouncinessLevel = 5;
            ClearCache();
        }
    }
}

