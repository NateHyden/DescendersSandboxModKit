using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // HoverMode — floats the bike a selectable height above (or below) the
    // terrain surface, following ground contour via a fresh raycast every
    // physics tick, reached through a spring-damper force rather than a hard
    // position snap - that's what gives the soft, lagging "magic carpet" feel
    // instead of a rigid float.
    //
    // A pure visual-offset version (leave physics on real ground, just render
    // the bike/rider/camera floating above it) was tried and reverted - moving
    // BikeModel's transform directly caused a launch even with the camera
    // offset removed, meaning that transform isn't safely decoupled from
    // physics in this game's implementation. This spring+direct-drive version
    // is the one confirmed stable in testing.
    public static class HoverMode
    {
        public static bool Enabled { get; private set; } = false;

        // ── Height (meters from raycast-detected ground, signed) ─────────
        public const float MinHeight = -5f;
        public const float MaxHeight = 20f;
        public const float HeightStep = 0.5f;
        public static float HoverHeight { get; private set; } = 3f;

        public static void IncreaseHeight() { HoverHeight = Mathf.Min(MaxHeight, HoverHeight + HeightStep); }
        public static void DecreaseHeight() { HoverHeight = Mathf.Max(MinHeight, HoverHeight - HeightStep); }
        public static void SetHeight(float v) { HoverHeight = Mathf.Clamp(v, MinHeight, MaxHeight); }
        public static string DisplayHeight { get { return HoverHeight.ToString("0.0") + "m"; } }

        // ── Spring-damper tuning ───────────────────────────────────────
        public static float Stiffness = 40f;
        public static float Damping = 12f;

        // ── Horizontal drive (hovercraft-style) ───────────────────────
        // The game's own forward/steer physics is wheel-traction based - it
        // only pushes the bike when wheels have real ground contact to grip
        // against. With wheels floating in mid-air that traction never exists,
        // so this drives translation/turning directly instead.
        public static float MoveSpeed = 25f;
        public static float TurnSpeed = 110f; // degrees/sec
        private static float _yaw;

        private static Transform _playerTrans;
        private static Vehicle _vehicle;
        private static Rigidbody _rb;
        private static bool _savedGravity;

        private static readonly RaycastHit[] _hitBuffer = new RaycastHit[16];

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Enable();
            else Disable();
            MelonLogger.Msg("[HoverMode] -> " + (Enabled ? "ON" : "OFF") + " height=" + DisplayHeight);
        }

        private static void Enable()
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) { MelonLogger.Warning("[HoverMode] Player_Human not found."); Enabled = false; return; }

                _playerTrans = player.transform;
                _vehicle = player.GetComponent<Vehicle>();
                _rb = player.GetComponentInChildren<Rigidbody>();

                if ((object)_vehicle == null || (object)_rb == null)
                {
                    MelonLogger.Warning("[HoverMode] Vehicle/Rigidbody not found.");
                    Enabled = false; return;
                }

                _savedGravity = _rb.useGravity;
                _rb.useGravity = false;
                _yaw = _playerTrans.eulerAngles.y;

                MelonLogger.Msg("[HoverMode] Enabled. rb=" + _rb.name);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[HoverMode] Enable: " + ex.Message);
                Enabled = false;
            }
        }

        private static void Disable()
        {
            try
            {
                if ((object)_rb != null) _rb.useGravity = _savedGravity;
            }
            catch (System.Exception ex) { MelonLogger.Error("[HoverMode] Disable: " + ex.Message); }

            _vehicle = null;
            _rb = null;
            _playerTrans = null;
        }

        public static void FixedTick()
        {
            if (!Enabled) return;
            if ((object)_rb == null || (object)_playerTrans == null) return;

            try
            {
                if (!FindGroundHeight(out float groundY)) return;

                float targetY = groundY + HoverHeight;
                float currentY = _rb.position.y;
                float error = targetY - currentY;
                float velY = _rb.velocity.y;

                float accel = error * Stiffness - velY * Damping;
                _rb.AddForce(Vector3.up * accel, ForceMode.Acceleration);

                DriveTick();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[HoverMode] FixedTick: " + ex.Message);
                Enabled = false;
                Disable();
            }
        }

        private static void DriveTick()
        {
            float turnInput = 0f, moveInput = 0f;
            try
            {
                InControl.InputDevice dev = InControl.InputManager.ActiveDevice;
                turnInput = (float)dev.LeftStick.X;
                moveInput = (float)dev.LeftStick.Y;
            }
            catch { }
            if (Input.GetKey(KeyCode.W)) moveInput = 1f;
            else if (Input.GetKey(KeyCode.S)) moveInput = -1f;
            if (Input.GetKey(KeyCode.D)) turnInput = 1f;
            else if (Input.GetKey(KeyCode.A)) turnInput = -1f;

            _yaw += turnInput * TurnSpeed * Time.fixedDeltaTime;
            Quaternion targetRot = Quaternion.Euler(0f, _yaw, 0f);
            _rb.MoveRotation(targetRot);
            _rb.angularVelocity = Vector3.zero;

            Vector3 forwardDir = targetRot * Vector3.forward;
            Vector3 horizontalVel = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
            Vector3 desiredHorizontal = forwardDir * moveInput * MoveSpeed;
            Vector3 correction = (desiredHorizontal - horizontalVel) * 5f;
            _rb.AddForce(new Vector3(correction.x, 0f, correction.z), ForceMode.Acceleration);
        }

        private static bool FindGroundHeight(out float groundY)
        {
            groundY = 0f;
            Vector3 origin = _rb.position + Vector3.up * 2f;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, _hitBuffer, 200f, ~0, QueryTriggerInteraction.Ignore);
            if (count <= 0) return false;

            float bestDist = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hitBuffer[i];
                if ((object)hit.transform == null) continue;
                if (hit.transform.root == _playerTrans.root) continue;
                if (hit.distance < bestDist) { bestDist = hit.distance; groundY = hit.point.y; found = true; }
            }
            return found;
        }

        public static void Reset()
        {
            if (Enabled) { Enabled = false; Disable(); }
        }
    }
}
