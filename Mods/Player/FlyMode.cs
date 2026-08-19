using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using System.Reflection;

namespace DescendersModMenu.Mods
{
    public static class FlyMode
    {
        public static bool Enabled { get; private set; } = false;

        public static float MoveSpeed = 30f;
        public static float ClimbSpeed = 20f;
        public static float LookSpeed = 90f;

        public const float MinMoveSpeed = 5f;
        public const float MaxMoveSpeed = 80f;
        public const float MoveSpeedStep = 5f;

        public const float MinClimbSpeed = 5f;
        public const float MaxClimbSpeed = 60f;
        public const float ClimbSpeedStep = 5f;

        public static void IncreaseMoveSpeed() { MoveSpeed = Mathf.Min(MaxMoveSpeed, MoveSpeed + MoveSpeedStep); }
        public static void DecreaseMoveSpeed() { MoveSpeed = Mathf.Max(MinMoveSpeed, MoveSpeed - MoveSpeedStep); }
        public static void SetMoveSpeed(float v) { MoveSpeed = Mathf.Clamp(v, MinMoveSpeed, MaxMoveSpeed); }

        public static void IncreaseClimbSpeed() { ClimbSpeed = Mathf.Min(MaxClimbSpeed, ClimbSpeed + ClimbSpeedStep); }
        public static void DecreaseClimbSpeed() { ClimbSpeed = Mathf.Max(MinClimbSpeed, ClimbSpeed - ClimbSpeedStep); }
        public static void SetClimbSpeed(float v) { ClimbSpeed = Mathf.Clamp(v, MinClimbSpeed, MaxClimbSpeed); }

        private static Vehicle _vehicle = null;
        private static Rigidbody _rb = null;
        private static Transform _playerTrans = null;
        private static VehicleController _vc = null;

        private static FieldInfo _physField = null;
        private static MethodInfo _toggleCtrl = null;

        private static bool _savedKinematic = false;
        private static bool _savedGravity = false;
        private static bool _savedNoBail = false;
        private static float _yaw = 0f;
        private static float _pitch = 0f;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Enable();
            else Disable();
            ModLog.Feedback("[FlyMode] -> " + (Enabled ? "ON" : "OFF"));
        }

        private static void Enable()
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if (!UnityNull.Alive(player))
                {
                    ModLog.Warn("[FlyMode] Player_Human not found.");
                    Enabled = false; return;
                }

                _playerTrans = player.transform;
                _vehicle = player.GetComponent<Vehicle>();
                _vc = player.GetComponent<VehicleController>();

                _rb = player.GetComponentInChildren<Rigidbody>();

                if (!UnityNull.Alive(_vehicle))
                {
                    ModLog.Warn("[FlyMode] Vehicle not found.");
                    Enabled = false; return;
                }

                if (!UnityNull.Alive(_rb))
                {
                    ModLog.Warn("[FlyMode] Rigidbody not found.");
                    Enabled = false; return;
                }

                if ((object)_physField == null)
                {
                    _physField = _vehicle.GetType().GetField("bYxcVhv",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if ((object)_physField == null)
                        _physField = typeof(Vehicle).GetField("bYxcVhv",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if ((object)_toggleCtrl == null)
                    _toggleCtrl = typeof(VehicleController).GetMethod("ToggleControl",
                        BindingFlags.Public | BindingFlags.Instance);

                _savedKinematic = _rb.isKinematic;
                _savedGravity = _rb.useGravity;
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.useGravity = false;
                _rb.isKinematic = true;

                if ((object)_physField != null)
                    _physField.SetValue(_vehicle, false);

                if ((object)_vc != null && (object)_toggleCtrl != null)
                    _toggleCtrl.Invoke(_vc, new object[] { false, false });

                _savedNoBail = NoBail.Enabled;
                NoBail.SetEnabled(true);

                _yaw = _playerTrans.eulerAngles.y;
                _pitch = 0f;

                ModLog.Debug("[FlyMode] Ready. rb=" + ((object)_rb != null)
                    + " physField=" + ((object)_physField != null)
                    + " toggle=" + ((object)_toggleCtrl != null));
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[FlyMode] Enable: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "FlyMode");
                Enabled = false;
            }
        }

        private static void Disable()
        {
            try
            {
                if (UnityNull.Alive(_vehicle) && (object)_physField != null)
                    _physField.SetValue(_vehicle, true);

                if (UnityNull.Alive(_rb))
                {
                    _rb.isKinematic = _savedKinematic;
                    _rb.useGravity = _savedGravity;
                    _rb.velocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }

                if (UnityNull.Alive(_vc) && (object)_toggleCtrl != null)
                    _toggleCtrl.Invoke(_vc, new object[] { true, true });

                NoBail.SetEnabled(_savedNoBail);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[FlyMode] Disable: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "FlyMode");
            }

            _vehicle = null;
            _rb = null;
            _playerTrans = null;
            _vc = null;
        }

        public static void Tick()
        {
            if (!Enabled) return;
            if (!UnityNull.Alive(_vehicle) || !UnityNull.Alive(_rb) || !UnityNull.Alive(_playerTrans))
            {
                Enabled = false;
                Disable();
                return;
            }

            try
            {
                if ((object)_physField != null)
                    _physField.SetValue(_vehicle, false);

                if (!_rb.isKinematic)
                {
                    _rb.velocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                    _rb.isKinematic = true;
                }

                InControl.InputDevice dev = InControl.InputManager.ActiveDevice;

                float rsX = (float)dev.RightStick.X;
                float rsY = (float)dev.RightStick.Y;
                if (Mathf.Abs(rsX) > 0.1f) _yaw += rsX * LookSpeed * Time.deltaTime;
                if (Mathf.Abs(rsY) > 0.1f) _pitch -= rsY * LookSpeed * Time.deltaTime;
                _pitch = Mathf.Clamp(_pitch, -89f, 89f);

                Quaternion newRot = Quaternion.Euler(_pitch, _yaw, 0f);

                Vector3 move = Vector3.zero;
                float v = (float)dev.LeftStick.Y;
                float h = (float)dev.LeftStick.X;
                if (Input.GetKey(KeyCode.W)) v = 1f;
                else if (Input.GetKey(KeyCode.S)) v = -1f;
                if (Input.GetKey(KeyCode.D)) h = 1f;
                else if (Input.GetKey(KeyCode.A)) h = -1f;
                move += newRot * Vector3.forward * v * MoveSpeed * Time.deltaTime;
                move += newRot * Vector3.right * h * MoveSpeed * Time.deltaTime;

                float up = (float)dev.RightTrigger;
                float down = (float)dev.LeftTrigger;
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) up = 1f;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) down = 1f;
                move += Vector3.up * (up - down) * ClimbSpeed * Time.deltaTime;

                _playerTrans.position += move;
                _playerTrans.rotation = newRot;

                Camera cam = Camera.main;
                if (UnityNull.Alive(cam))
                {
                    Vector3 offset = new Vector3(0f, 1.5f, -4.5f);
                    cam.transform.position = _playerTrans.position + newRot * offset;
                    cam.transform.rotation = newRot;
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[FlyMode] Tick: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "FlyMode");
                Enabled = false;
                Disable();
            }
        }

        public static void Reset()
        {
            if (Enabled) { Enabled = false; Disable(); }
            _physField = null;
            _toggleCtrl = null;
        }
    }
}

