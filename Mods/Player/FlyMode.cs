using MelonLoader;
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

        private static Vehicle _vehicle = null;
        private static Rigidbody _rb = null;
        private static Transform _playerTrans = null;
        private static VehicleController _vc = null;

        private static FieldInfo _physField = null; // bYxcVhv on Vehicle / subclass
        private static MethodInfo _toggleCtrl = null; // ToggleControl

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
            MelonLogger.Msg("[FlyMode] -> " + (Enabled ? "ON" : "OFF"));
        }

        private static void Enable()
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null)
                {
                    MelonLogger.Warning("[FlyMode] Player_Human not found.");
                    Enabled = false; return;
                }

                _playerTrans = player.transform;
                _vehicle = player.GetComponent<Vehicle>();
                _vc = player.GetComponent<VehicleController>();

                // Rigidbody lives on a child — use GetComponentInChildren
                _rb = player.GetComponentInChildren<Rigidbody>();

                if ((object)_vehicle == null)
                {
                    MelonLogger.Warning("[FlyMode] Vehicle not found.");
                    Enabled = false; return;
                }

                if ((object)_rb == null)
                {
                    MelonLogger.Warning("[FlyMode] Rigidbody not found.");
                    Enabled = false; return;
                }

                // Find physField on actual runtime type (may be a subclass of Vehicle)
                if ((object)_physField == null)
                {
                    _physField = _vehicle.GetType().GetField("bYxcVhv",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if ((object)_physField == null) // fallback to declared type
                        _physField = typeof(Vehicle).GetField("bYxcVhv",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if ((object)_toggleCtrl == null)
                    _toggleCtrl = typeof(VehicleController).GetMethod("ToggleControl",
                        BindingFlags.Public | BindingFlags.Instance);

                // Kill physics on the rigidbody
                _savedKinematic = _rb.isKinematic;
                _savedGravity = _rb.useGravity;
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.useGravity = false;
                _rb.isKinematic = true;

                // Disable vehicle physics simulation
                if ((object)_physField != null)
                    _physField.SetValue(_vehicle, false);

                // Disable controller input
                if ((object)_vc != null && (object)_toggleCtrl != null)
                    _toggleCtrl.Invoke(_vc, new object[] { false, false });

                // Suppress bail
                _savedNoBail = NoBail.Enabled;
                NoBail.SetEnabled(true);

                // Seed rotation from current orientation
                _yaw = _playerTrans.eulerAngles.y;
                _pitch = 0f;

                MelonLogger.Msg("[FlyMode] Ready. rb=" + ((object)_rb != null)
                    + " physField=" + ((object)_physField != null)
                    + " toggle=" + ((object)_toggleCtrl != null));
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[FlyMode] Enable: " + ex.Message);
                Enabled = false;
            }
        }

        private static void Disable()
        {
            try
            {
                // Re-enable vehicle physics
                if ((object)_vehicle != null && (object)_physField != null)
                    _physField.SetValue(_vehicle, true);

                if ((object)_rb != null)
                {
                    _rb.isKinematic = _savedKinematic;
                    _rb.useGravity = _savedGravity;
                    _rb.velocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }

                // Re-enable controller input
                if ((object)_vc != null && (object)_toggleCtrl != null)
                    _toggleCtrl.Invoke(_vc, new object[] { true, true });

                // Restore bail behaviour
                NoBail.SetEnabled(_savedNoBail);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[FlyMode] Disable: " + ex.Message);
            }

            _vehicle = null;
            _rb = null;
            _playerTrans = null;
            _vc = null;
        }

        public static void Tick()
        {
            if (!Enabled) return;
            if ((object)_vehicle == null || (object)_rb == null) return;

            try
            {
                // Keep vehicle physics suppressed every frame
                if ((object)_physField != null)
                    _physField.SetValue(_vehicle, false);

                // Also keep kinematic in case something restores it
                if (!_rb.isKinematic)
                {
                    _rb.velocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                    _rb.isKinematic = true;
                }

                InControl.InputDevice dev = InControl.InputManager.ActiveDevice;

                // Right stick — yaw and pitch
                float rsX = (float)dev.RightStick.X;
                float rsY = (float)dev.RightStick.Y;
                if (Mathf.Abs(rsX) > 0.1f) _yaw += rsX * LookSpeed * Time.deltaTime;
                if (Mathf.Abs(rsY) > 0.1f) _pitch -= rsY * LookSpeed * Time.deltaTime;
                _pitch = Mathf.Clamp(_pitch, -89f, 89f);

                Quaternion newRot = Quaternion.Euler(_pitch, _yaw, 0f);

                // Move player root via rb.MovePosition (works correctly on kinematic)
                Vector3 move = Vector3.zero;
                float v = (float)dev.LeftStick.Y;
                float h = (float)dev.LeftStick.X;
                move += newRot * Vector3.forward * v * MoveSpeed * Time.deltaTime;
                move += newRot * Vector3.right * h * MoveSpeed * Time.deltaTime;

                float up = (float)dev.RightTrigger;
                float down = (float)dev.LeftTrigger;
                if (Input.GetKey(KeyCode.Space)) up = 1f;
                if (Input.GetKey(KeyCode.LeftShift)) down = 1f;
                move += Vector3.up * (up - down) * ClimbSpeed * Time.deltaTime;

                // Move the root transform — all children (including kinematic rb) follow
                _playerTrans.position += move;
                _playerTrans.rotation = newRot;

                // Keep camera locked behind the bike
                Camera cam = Camera.main;
                if ((object)cam != null)
                {
                    Vector3 offset = new Vector3(0f, 1.5f, -4.5f);
                    cam.transform.position = _playerTrans.position + newRot * offset;
                    cam.transform.rotation = newRot;
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[FlyMode] Tick: " + ex.Message);
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