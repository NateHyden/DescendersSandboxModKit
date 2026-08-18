using System.Reflection;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Bounces the bike off the ground on landing, proportional to impact
    // speed, using the same restitution ("bounciness fraction of impact
    // speed comes back") model as a real ball — which is also why it
    // needs no separate "fade out" logic: a harder fall produces a bigger
    // rebound automatically, and each successive bounce is naturally
    // smaller than the last since gravity has less height to reaccelerate
    // it before the next landing. The fade is a free consequence of
    // applying the same restitution ratio every time, not something
    // tracked separately.
    //
    // Ground detection and landing-edge detection reuse the exact same
    // proven pattern SessionTrackers already uses for airtime tracking —
    // same reflection-found onGround property on Vehicle (the one
    // NoSpeedCap also uses), same "was airborne, now grounded" edge check.
    //
    // Known interaction: a hard bounce can register as a crash to the
    // game's own bail/Landing Impact threshold before we get a chance to
    // apply the rebound. Documented in the UI info box rather than papered
    // over — plays best with a raised Landing Impact threshold or No Bail.
    public static class BouncyBike
    {
        public static bool Enabled = false;
        public static int BouncinessLevel { get; private set; } = 5;

        // Below this downward impact speed (m/s), don't bounce at all —
        // otherwise every tiny bump would launch you.
        private const float MinImpactSpeed = 4f;

        // Level 1 = 0.15 (barely bounces), Level 10 = 0.95 (superball).
        // Fraction of impact speed returned as rebound speed.
        private static float Restitution => 0.1f + (BouncinessLevel - 1) * (0.85f / 9f);

        private static PropertyInfo _onGroundProp = null;
        private static bool _groundPropCached = false;
        private static bool _wasOnGround = true;

        private static GameObject _cachedPlayer = null;
        private static Vehicle _cachedVehicle = null;
        private static Rigidbody _cachedRb = null;

        // Tracked every airborne frame so the TRUE impact velocity is
        // available the instant landing is detected — the game's own
        // collision response zeroes Rigidbody.velocity.y out almost
        // immediately on contact, so reading it only on the landing frame
        // itself would be too late.
        private static float _lastVerticalVelocity = 0f;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (!Enabled) ClearRuntimeState();
            ModLog.Feedback("[BouncyBike] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void IncreaseLevel() { if (BouncinessLevel < 10) BouncinessLevel++; }
        public static void DecreaseLevel() { if (BouncinessLevel > 1) BouncinessLevel--; }
        public static void SetLevel(int level) { BouncinessLevel = Mathf.Clamp(level, 1, 10); }

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

                // Same onGround property SessionTrackers/NoSpeedCap already
                // found and use — a bool property on Vehicle whose obfuscated
                // name happens to start with 'T'. Cached once, not re-searched.
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
                    // Airborne — keep the last vertical velocity fresh so it's
                    // ready the instant we detect landing next frame.
                    _lastVerticalVelocity = _cachedRb.velocity.y;
                }
                else if (!_wasOnGround)
                {
                    // Landed this exact frame.
                    float impactSpeed = -_lastVerticalVelocity; // positive = was falling
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
