using System.Reflection;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Squash modes on Cyclist + BikeModel — Bounce (sine) and Jelly (landing physics)
    /// are mutually exclusive.
    /// </summary>
    public static class CartoonSquash
    {
        /// <summary>Sine-wave bounce mode.</summary>
        public static bool Enabled = false;
        /// <summary>Landing/bump jelly physics. Cannot be on with Enabled.</summary>
        public static bool JellyMode = false;

        public static bool IsActive => Enabled || JellyMode;

        public static int SpeedLevel = 10;
        public static int AmountLevel = 10;

        public const int MinLevel = 1;
        public const int MaxLevel = 20;

        private static Transform _cyclist;
        private static Transform _bike;
        private static Vector3 _cyclistDefault = Vector3.one;
        private static Vector3 _bikeDefault = Vector3.one;
        private static bool _captured;

        private static float _jellyY = 1f;
        private static float _jellyVel = 0f;
        // Delayed width jiggle — lags behind height for a gelatin wobble.
        private static float _jellyXz = 1f;
        private static float _jellyXzVel = 0f;

        private static GameObject _cachedPlayer;
        private static Vehicle _cachedVehicle;
        private static Rigidbody _cachedRb;
        private static PropertyInfo _onGroundProp;
        private static bool _groundPropCached;
        private static bool _wasOnGround = true;
        private static float _lastAirVerticalVel;
        private static float _prevVelY;
        private static bool _hadPrevVel;

        private const float MinImpactSpeed = 1.5f;
        private const float MaxYMul = 2.1f;
        private const float MinYMul = 0.22f;

        public static float SpeedHz => 0.5f + (SpeedLevel - 1) * (3.5f / 19f);
        public static float Amount => 0.12f + (AmountLevel - 1) * (0.33f / 19f);
        public static string SpeedDisplay => SpeedHz.ToString("0.0") + " Hz";

        public static void CaptureDefaults()
        {
            _captured = false;
            _cyclist = null;
            _bike = null;
            TryCapture();
        }

        public static void Toggle()
        {
            SetEnabled(!Enabled);
        }

        public static void ToggleJelly()
        {
            SetJellyEnabled(!JellyMode);
        }

        public static void SetEnabled(bool on)
        {
            if (on)
            {
                if (JellyMode)
                {
                    JellyMode = false;
                    ResetJellyState();
                }
                if (Enabled) return;
                Enabled = true;
                ModLog.Feedback("[CartoonSquash] Bounce -> ON");
                return;
            }

            if (!Enabled) return;
            Enabled = false;
            if (!JellyMode)
            {
                RestoreBaseScales();
                ClearCache();
            }
            ModLog.Feedback("[CartoonSquash] Bounce -> OFF");
        }

        public static void SetJellyEnabled(bool on)
        {
            if (on)
            {
                if (Enabled)
                {
                    Enabled = false;
                }
                if (JellyMode) return;
                JellyMode = true;
                ResetJellyState();
                ModLog.Feedback("[CartoonSquash] Jelly -> ON");
                return;
            }

            if (!JellyMode) return;
            JellyMode = false;
            ResetJellyState();
            if (!Enabled)
            {
                RestoreBaseScales();
                ClearCache();
            }
            ModLog.Feedback("[CartoonSquash] Jelly -> OFF");
        }

        public static void IncreaseSpeed()
        {
            if (SpeedLevel < MaxLevel) SpeedLevel++;
        }

        public static void DecreaseSpeed()
        {
            if (SpeedLevel > MinLevel) SpeedLevel--;
        }

        public static void IncreaseAmount()
        {
            if (AmountLevel < MaxLevel) AmountLevel++;
        }

        public static void DecreaseAmount()
        {
            if (AmountLevel > MinLevel) AmountLevel--;
        }

        public static void Tick()
        {
            if (!IsActive) return;
            try
            {
                if (!EnsureTargets()) return;

                float yMul;
                float xzMul;
                if (JellyMode)
                    TickJelly(out yMul, out xzMul);
                else
                {
                    float wave = Mathf.Sin(Time.time * SpeedHz * Mathf.PI * 2f);
                    yMul = 1f + Amount * wave;
                    xzMul = 1f / yMul;
                }

                ApplySquash(_cyclist, _cyclistDefault, PlayerSize.CurrentScale, yMul, xzMul);
                ApplySquash(_bike, _bikeDefault, BikeSize.CurrentScale, yMul, xzMul);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[CartoonSquash] Tick: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CartoonSquash");
            }
        }

        private static void TickJelly(out float yMul, out float xzMul)
        {
            float dt = Time.deltaTime;
            if (dt < 0.0001f) dt = 0.016f;
            if (dt > 0.05f) dt = 0.05f;

            EnsurePhysicsRefs();

            float velY = 0f;
            bool hasRb = UnityNull.Alive(_cachedRb);
            if (hasRb) velY = _cachedRb.velocity.y;

            bool onGround = ResolveOnGround(velY);

            bool velocityLanding = false;
            float impactFromVel = 0f;
            if (_hadPrevVel && _prevVelY < -MinImpactSpeed && velY > _prevVelY + 1.2f)
            {
                impactFromVel = -_prevVelY;
                velocityLanding = true;
            }

            if (!onGround)
            {
                _lastAirVerticalVel = velY;
                if (velY < -0.6f)
                {
                    float fallStretch = Mathf.Clamp01((-velY - 0.6f) / 10f) * Amount * 1.35f;
                    float targetAir = 1f + fallStretch;
                    _jellyY = Mathf.Lerp(_jellyY, targetAir, 1f - Mathf.Exp(-6f * dt));
                    _jellyVel *= Mathf.Exp(-3.5f * dt);
                }
            }

            bool groundLanding = onGround && !_wasOnGround;
            if (groundLanding || velocityLanding)
            {
                float impactSpeed = groundLanding ? -_lastAirVerticalVel : impactFromVel;
                if (impactSpeed < impactFromVel) impactSpeed = impactFromVel;
                if (impactSpeed < 0f) impactSpeed = 0f;
                if (impactSpeed > MinImpactSpeed)
                {
                    float intensity = Mathf.Clamp01((impactSpeed - MinImpactSpeed) / 12f);
                    float compress = intensity * (0.5f + Amount * 1.55f);
                    _jellyY = Mathf.Min(_jellyY, 1f - compress);
                    _jellyVel = -intensity * (14f + Amount * 28f);
                    _jellyXz = Mathf.Max(_jellyXz, 1f + compress * 0.9f);
                    _jellyXzVel += intensity * (6f + Amount * 10f);
                }
            }
            else if (onGround && hasRb && _hadPrevVel)
            {
                float accelY = (velY - _prevVelY) / dt;
                if (accelY < -12f)
                {
                    float bump = Mathf.Clamp01((-accelY - 12f) / 45f) * Amount;
                    _jellyY -= bump * 0.55f;
                    _jellyVel -= bump * 16f;
                    _jellyXzVel += bump * 8f;
                }
                if (Mathf.Abs(velY) > 0.8f)
                {
                    float ride = Mathf.Clamp(velY * 0.012f * Amount, -0.08f, 0.08f);
                    _jellyVel += ride * 20f * dt;
                }
            }

            _wasOnGround = onGround;
            _prevVelY = velY;
            _hadPrevVel = hasRb;

            // Soft underdamped spring — long jelly wobble.
            float stiffness = 5.5f + SpeedHz * 14f;
            float damping = 0.85f + SpeedHz * 0.55f;
            float force = (1f - _jellyY) * stiffness - _jellyVel * damping;
            _jellyVel += force * dt;
            _jellyY += _jellyVel * dt;

            if (_jellyY < MinYMul) { _jellyY = MinYMul; if (_jellyVel < 0f) _jellyVel *= -0.35f; }
            if (_jellyY > MaxYMul) { _jellyY = MaxYMul; if (_jellyVel > 0f) _jellyVel *= -0.35f; }

            // Width lags behind height for gooey out-of-phase jiggle.
            float targetXz = 1f / Mathf.Max(0.2f, _jellyY);
            float xzStiff = 3.5f + SpeedHz * 9f;
            float xzDamp = 0.7f + SpeedHz * 0.4f;
            float xzForce = (targetXz - _jellyXz) * xzStiff - _jellyXzVel * xzDamp;
            _jellyXzVel += xzForce * dt;
            _jellyXz += _jellyXzVel * dt;

            if (_jellyXz < 0.45f) { _jellyXz = 0.45f; if (_jellyXzVel < 0f) _jellyXzVel *= -0.3f; }
            if (_jellyXz > 2.6f) { _jellyXz = 2.6f; if (_jellyXzVel > 0f) _jellyXzVel *= -0.3f; }

            if (Mathf.Abs(_jellyY - 1f) < 0.004f && Mathf.Abs(_jellyVel) < 0.04f
                && Mathf.Abs(_jellyXz - 1f) < 0.004f && Mathf.Abs(_jellyXzVel) < 0.04f)
            {
                _jellyY = 1f;
                _jellyVel = 0f;
                _jellyXz = 1f;
                _jellyXzVel = 0f;
            }

            yMul = _jellyY;
            xzMul = _jellyXz;
        }

        private static bool ResolveOnGround(float velY)
        {
            if ((object)_onGroundProp != null && UnityNull.Alive(_cachedVehicle))
            {
                try
                {
                    object val = _onGroundProp.GetValue(_cachedVehicle, null);
                    if (val is bool) return (bool)val;
                }
                catch { }
            }
            // Fallback: treat near-zero vertical speed as grounded.
            return Mathf.Abs(velY) < 1.25f;
        }

        private static void EnsurePhysicsRefs()
        {
            if (!UnityNull.Alive(_cachedPlayer) || !_cachedPlayer.activeInHierarchy)
            {
                _cachedPlayer = GameObject.Find("Player_Human");
                _cachedVehicle = null;
                _cachedRb = null;
                _onGroundProp = null;
                _groundPropCached = false;
            }
            if (!UnityNull.Alive(_cachedPlayer)) return;

            if (!UnityNull.Alive(_cachedVehicle))
                _cachedVehicle = _cachedPlayer.GetComponent<Vehicle>();
            if (!UnityNull.Alive(_cachedRb))
                _cachedRb = _cachedPlayer.GetComponentInChildren<Rigidbody>();

            if (!_groundPropCached && UnityNull.Alive(_cachedVehicle))
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
        }

        private static void ApplySquash(Transform target, Vector3 defaultScale, float sizeScale, float yMul, float xzMul)
        {
            if (!UnityNull.Alive(target)) return;
            float s = defaultScale.x * sizeScale;
            target.localScale = new Vector3(s * xzMul, s * yMul, s * xzMul);
        }

        private static bool EnsureTargets()
        {
            if (!UnityNull.Alive(_cyclist) || !UnityNull.Alive(_bike))
            {
                _cyclist = null;
                _bike = null;
                if (!TryCapture()) return false;
            }
            return UnityNull.Alive(_cyclist) && UnityNull.Alive(_bike);
        }

        private static bool TryCapture()
        {
            GameObject player = GameObject.Find("Player_Human");
            if (!UnityNull.Alive(player)) return false;

            Transform cyclist = player.transform.Find("Cyclist");
            Transform bike = player.transform.Find("BikeModel");
            if (!UnityNull.Alive(cyclist) || !UnityNull.Alive(bike)) return false;

            if (!_captured)
            {
                _cyclistDefault = cyclist.localScale;
                _bikeDefault = bike.localScale;
                _captured = true;
            }

            _cyclist = cyclist;
            _bike = bike;
            return true;
        }

        private static void RestoreBaseScales()
        {
            try
            {
                BikeSize.ApplyLevel(BikeSize.Level);
                PlayerSize.ApplyLevel(PlayerSize.Level);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[CartoonSquash] RestoreBaseScales: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CartoonSquash");
            }
        }

        private static void ResetJellyState()
        {
            _jellyY = 1f;
            _jellyVel = 0f;
            _jellyXz = 1f;
            _jellyXzVel = 0f;
            _wasOnGround = true;
            _lastAirVerticalVel = 0f;
            _prevVelY = 0f;
            _hadPrevVel = false;
        }

        private static void ClearCache()
        {
            _cyclist = null;
            _bike = null;
            _captured = false;
            _cachedPlayer = null;
            _cachedVehicle = null;
            _cachedRb = null;
            _onGroundProp = null;
            _groundPropCached = false;
            ResetJellyState();
        }

        public static void Reset()
        {
            Enabled = false;
            JellyMode = false;
            RestoreBaseScales();
            ClearCache();
        }

        public static void GlobalReset()
        {
            SpeedLevel = 10;
            AmountLevel = 10;
            Reset();
        }
    }
}
