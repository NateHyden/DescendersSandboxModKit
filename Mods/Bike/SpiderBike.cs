using MelonLoader;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    // Magnet tyres: gravity and bike-up follow the surface so you can ride
    // walls, roofs and ceilings. Hop turns the magnet off for one jump.
    public static class SpiderBike
    {
        public static bool Enabled { get; private set; } = false;

        private const float ProbeDistance = 3.2f;
        private const float StickDistance = 1.6f;
        private const float ContactDistance = 0.7f;
        private const float AlignRate = 16f;
        private const float StickMul = 5.5f;
        private const float HopStart = 3.2f;
        private const float HopHeight = 1.6f;

        private static Rigidbody _rb;
        private static Transform _playerRoot;
        private static PlayerInfoImpact _impact;
        private static bool _savedGravity;
        private static Vector3 _lastUp = Vector3.up;
        private static bool _hopping;
        private static bool _wasPlanted;
        private static float _hopReadyAt;
        private static readonly RaycastHit[] _hits = new RaycastHit[16];

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Enable();
            else Disable();
            ModLog.Feedback("[SpiderBike] -> " + (Enabled ? "ON" : "OFF"));
        }

        private static void Enable()
        {
            _lastUp = Vector3.up;
            _hopping = false;
            _wasPlanted = false;
            _hopReadyAt = 0f;
            CacheBody();
            if (_rb) _rb.useGravity = false;
            ApplyNoBail(true);
        }

        private static void Disable()
        {
            try
            {
                if (_rb) _rb.useGravity = _savedGravity;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SpiderBike] Disable: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "SpiderBike");
            }
            ApplyNoBail(NoBail.Enabled);
            _rb = null;
            _playerRoot = null;
            _impact = null;
        }

        public static void FixedTick()
        {
            if (!Enabled) return;

            try
            {
                CacheBody();
                if (!_rb) return;
                if (_rb.useGravity) _rb.useGravity = false;
                ApplyNoBail(true);

                Vector3 desiredUp;
                float hitDist;
                bool onSurface = ProbeSurface(out desiredUp, out hitDist);
                if (onSurface) _lastUp = desiredUp;
                else desiredUp = _lastUp;

                float g = Physics.gravity.magnitude;
                if (g < 0.5f) g = 17.5f;

                bool contacting = onSurface && hitDist < ContactDistance;
                bool inStickRange = onSurface && hitDist < StickDistance;
                float away = Vector3.Dot(_rb.velocity, desiredUp);

                if (!_hopping && _wasPlanted && away > HopStart && Time.time >= _hopReadyAt)
                {
                    _hopping = true;
                    BoostHop(desiredUp, away, g);
                    away = Vector3.Dot(_rb.velocity, desiredUp);
                }

                // Only kill the bounce OFF the surface. Never kill speed INTO it —
                // that is the magnet, and wiping it is why walls started slipping.
                if (!_hopping && contacting && !_wasPlanted && away > 0.4f)
                    _rb.velocity -= desiredUp * away;
                if (_hopping && contacting && away <= 0f)
                {
                    _hopping = false;
                    _hopReadyAt = Time.time + 0.15f;
                }

                if (!_hopping)
                    AlignUp(desiredUp);

                _rb.AddForce(-desiredUp * g, ForceMode.Acceleration);
                if (inStickRange && !_hopping)
                    _rb.AddForce(-desiredUp * (g * StickMul), ForceMode.Acceleration);

                _wasPlanted = contacting && !_hopping;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SpiderBike] FixedTick: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "SpiderBike");
            }
        }

        public static void Reset()
        {
            if (Enabled)
            {
                Enabled = false;
                Disable();
            }
        }

        private static void BoostHop(Vector3 up, float away, float g)
        {
            float need = Mathf.Sqrt(2f * g * HopHeight);
            if (away >= need) return;
            _rb.velocity += up * (need - away);
        }

        private static void CacheBody()
        {
            if (_rb) return;
            GameObject player = GameObject.Find("Player_Human");
            if (!player) return;
            _playerRoot = player.transform;
            _rb = player.GetComponentInChildren<Rigidbody>();
            if (_rb) _savedGravity = _rb.useGravity;
        }

        private static bool ProbeSurface(out Vector3 normal, out float distance)
        {
            normal = _lastUp;
            distance = ProbeDistance;
            if (!_rb) return false;

            Transform t = _rb.transform;
            Vector3 origin = _rb.position + t.up * 0.25f;
            Vector3 bestN = Vector3.zero;
            float best = ProbeDistance;
            bool found = false;

            // Prefer the surface under the tyres. Extra rays only win if
            // they're clearly closer — averaging walls+floor made you slip.
            TryHit(origin, -t.up, ref bestN, ref best, ref found);
            TryHit(origin + t.forward * 0.8f, -t.up, ref bestN, ref best, ref found);
            if (!_hopping)
            {
                TryHit(origin, t.forward, ref bestN, ref best, ref found);
                TryHit(origin, -t.forward, ref bestN, ref best, ref found);
                TryHit(origin, t.right, ref bestN, ref best, ref found);
                TryHit(origin, -t.right, ref bestN, ref best, ref found);
                TryHit(origin, t.up, ref bestN, ref best, ref found);
            }

            if (!found) return false;
            normal = bestN.normalized;
            distance = best;
            return normal.sqrMagnitude > 0.01f;
        }

        private static void TryHit(Vector3 origin, Vector3 dir, ref Vector3 bestN, ref float best, ref bool found)
        {
            if (dir.sqrMagnitude < 0.01f) return;
            int count = Physics.RaycastNonAlloc(origin, dir, _hits, ProbeDistance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (!hit.transform) continue;
                if (_playerRoot && hit.transform.root == _playerRoot.root) continue;
                if (hit.distance >= best) continue;
                best = hit.distance;
                bestN = hit.normal;
                found = true;
            }
        }

        private static void AlignUp(Vector3 desiredUp)
        {
            Vector3 current = _rb.transform.up;
            float ang = Vector3.Angle(current, desiredUp);
            if (ang < 0.8f) return;

            float t = Mathf.Clamp01(AlignRate * Time.fixedDeltaTime);
            if (ang > 40f) t = Mathf.Clamp01(t * 2f);
            Vector3 newUp = Vector3.Slerp(current, desiredUp, t);
            if (newUp.sqrMagnitude < 0.0001f) return;
            _rb.MoveRotation(Quaternion.FromToRotation(current, newUp) * _rb.rotation);
        }

        private static void ApplyNoBail(bool on)
        {
            try
            {
                if (!_impact)
                {
                    GameObject go = GameObject.Find("PlayerInfo_Human");
                    if (go) _impact = go.GetComponent<PlayerInfoImpact>();
                }
                if (_impact) _impact.Nobail(on);
            }
            catch { }
        }
    }
}
