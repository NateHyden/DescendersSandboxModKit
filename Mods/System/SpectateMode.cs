using System.Reflection;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Detaches the camera from your own bike and chase-cams another
    // connected player instead. Deliberately NOT the game's own native
    // spectate system (State_Spectate/UI_Spectate) — that's wired tightly
    // into StateMachine + MultiManager + SessionManager multiplayer
    // session/room flow, and forcing it from outside that flow risks
    // softlocking a session. This reuses two patterns already proven
    // elsewhere in this project instead: FlyMode's freeze-your-own-
    // physics-and-drive-the-camera-by-hand approach, and
    // TeleportToPlayer's player scan/name resolution.
    //
    // v1 ran its own dual-layer Lerp/Slerp smoothing on top of the target's
    // position every frame — pure added latency, since this game already
    // writes remote-player transform.position directly from its own
    // network sync each tick, not through Rigidbody physics. v2 tried
    // forcing Rigidbody.interpolation on to help, which made only the
    // spectated target choppy while every other rider stayed smooth —
    // interpolation is built for physics-step motion, and conflicts with
    // a script overwriting transform.position from outside that step.
    // Settled state: track the target's transform directly, no lerp, no
    // Rigidbody tampering. See the field comments below for the full story.
    //
    // Only useful in multiplayer — solo there's no one else to spectate.
    public static class SpectateMode
    {
        public static bool Enabled { get; private set; } = false;

        private static List<TeleportToPlayer.PlayerEntry> _targets = new List<TeleportToPlayer.PlayerEntry>();
        private static int _targetIndex = -1;

        public static string CurrentTargetName =>
            (_targetIndex >= 0 && _targetIndex < _targets.Count) ? _targets[_targetIndex].Name : "--";
        public static string StatusDisplay =>
            !Enabled ? "OFF" : (_targets.Count == 0 ? "No players found" : CurrentTargetName);
        public static int TargetCount => _targets.Count;

        // ── Chase-cam distance ────────────────────────────────────────
        public static float Distance { get; private set; } = 6f;
        public const float MinDistance = 3f, MaxDistance = 15f, DistanceStep = 1f;
        public static void IncreaseDistance() { Distance = Mathf.Min(MaxDistance, Distance + DistanceStep); }
        public static void DecreaseDistance() { Distance = Mathf.Max(MinDistance, Distance - DistanceStep); }
        public static void SetDistance(float v) { Distance = Mathf.Clamp(v, MinDistance, MaxDistance); }
        private const float Height = 2.5f;

        private static GameObject _localPlayer;
        private static Vehicle _localVehicle;
        private static VehicleController _localVc;
        private static Rigidbody _localRb;

        // bYxcVhv = Vehicle's own-physics-simulation flag (same field
        // FlyMode already found and uses for the identical purpose).
        private static FieldInfo _physField;
        private static MethodInfo _toggleCtrl;

        private static bool _savedKinematic, _savedGravity, _savedNoBail;

        // Last used forward direction — kept only so the very rare frame
        // where a target's forward briefly reads as zero-length doesn't
        // snap the camera to somewhere nonsensical; not a smoothing pass.
        private static Vector3 _lastForward = Vector3.forward;

        // v2 forced Rigidbody.interpolation = Interpolate on the target,
        // assuming that's what smooths a physics-driven body between fixed
        // timesteps (same reasoning as your own bike). Testing found the
        // opposite: after this was added, ONLY the spectated target went
        // choppy — every other rider on screen, left with the game's own
        // default (None), stayed smooth. That's the signature of causing
        // a problem, not fixing one. The decompile explains why: this
        // game moves remote players by directly writing transform.position
        // in a custom script, not through real Rigidbody physics forces.
        // Interpolation smooths motion that originates inside the physics
        // step; forcing it onto a body whose position is being overwritten
        // from outside that step conflicts with it instead of helping.
        // Removed entirely rather than reintroduced with caveats.

        private static Transform _camOriginalParent;
        private static bool _camDetached = false;

        private static bool _subscribed = false;
        private static void EnsureSubscribed()
        {
            if (_subscribed) return;
            // Camera.onPreRender fires after every LateUpdate() in the
            // scene for that frame, right before the camera renders —
            // guaranteed last, so our write always sticks.
            Camera.onPreRender += OnPreRenderCamera;
            _subscribed = true;
        }

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Enable();
            else Disable();
            MelonLogger.Msg("[SpectateMode] -> " + (Enabled ? "ON" : "OFF"));
        }

        private static void Enable()
        {
            try
            {
                EnsureSubscribed();

                _localPlayer = GameObject.Find("Player_Human");
                if ((object)_localPlayer == null)
                { MelonLogger.Warning("[SpectateMode] Player_Human not found."); Enabled = false; return; }

                _localVehicle = _localPlayer.GetComponent<Vehicle>();
                _localVc = _localPlayer.GetComponent<VehicleController>();
                _localRb = _localPlayer.GetComponentInChildren<Rigidbody>();

                if ((object)_localVehicle == null || (object)_localRb == null)
                { MelonLogger.Warning("[SpectateMode] Vehicle/Rigidbody not found."); Enabled = false; return; }

                if ((object)_physField == null)
                {
                    _physField = _localVehicle.GetType().GetField("bYxcVhv",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if ((object)_physField == null)
                        _physField = typeof(Vehicle).GetField("bYxcVhv",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if ((object)_toggleCtrl == null)
                    _toggleCtrl = typeof(VehicleController).GetMethod("ToggleControl",
                        BindingFlags.Public | BindingFlags.Instance);

                _savedKinematic = _localRb.isKinematic;
                _savedGravity = _localRb.useGravity;
                _localRb.velocity = Vector3.zero;
                _localRb.angularVelocity = Vector3.zero;
                _localRb.useGravity = false;
                _localRb.isKinematic = true;

                if ((object)_physField != null) _physField.SetValue(_localVehicle, false);
                if ((object)_localVc != null && (object)_toggleCtrl != null)
                    _toggleCtrl.Invoke(_localVc, new object[] { false, false });

                _savedNoBail = NoBail.Enabled;
                NoBail.SetEnabled(true);

                RefreshTargets();
                if (_targets.Count > 0) SetTarget(0);

                MelonLogger.Msg("[SpectateMode] Enabled. " + _targets.Count + " target(s) found.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SpectateMode] Enable: " + ex.Message);
                Enabled = false;
            }
        }

        private static void Disable()
        {
            try
            {
                if ((object)_localVehicle != null && (object)_physField != null)
                    _physField.SetValue(_localVehicle, true);

                if ((object)_localRb != null)
                {
                    _localRb.isKinematic = _savedKinematic;
                    _localRb.useGravity = _savedGravity;
                    _localRb.velocity = Vector3.zero;
                    _localRb.angularVelocity = Vector3.zero;
                }

                if ((object)_localVc != null && (object)_toggleCtrl != null)
                    _toggleCtrl.Invoke(_localVc, new object[] { true, true });

                NoBail.SetEnabled(_savedNoBail);
            }
            catch (System.Exception ex) { MelonLogger.Error("[SpectateMode] Disable: " + ex.Message); }

            try
            {
                if (_camDetached && (object)_camOriginalParent != null)
                {
                    Camera cam = Camera.main;
                    if ((object)cam != null) cam.transform.SetParent(_camOriginalParent, true);
                }
            }
            catch (System.Exception ex) { MelonLogger.Warning("[SpectateMode] Re-attach camera: " + ex.Message); }
            _camDetached = false;
            _camOriginalParent = null;

            _localPlayer = null; _localVehicle = null; _localVc = null; _localRb = null;
            _targets.Clear();
            _targetIndex = -1;
        }

        public static void RefreshTargets()
        {
            _targets = TeleportToPlayer.ScanForPlayers();
            if (_targetIndex >= _targets.Count) _targetIndex = _targets.Count - 1;
        }

        // Sets which player we're watching. Every place that changes
        // _targetIndex goes through here.
        private static void SetTarget(int newIndex)
        {
            _targetIndex = newIndex;
            _lastForward = Vector3.forward;
        }

        public static void Next()
        {
            if (!Enabled) return;
            RefreshTargets();
            if (_targets.Count == 0) { SetTarget(-1); return; }
            SetTarget((_targetIndex + 1 + _targets.Count) % _targets.Count);
            MelonLogger.Msg("[SpectateMode] -> " + CurrentTargetName);
        }

        public static void Previous()
        {
            if (!Enabled) return;
            RefreshTargets();
            if (_targets.Count == 0) { SetTarget(-1); return; }
            SetTarget((_targetIndex - 1 + _targets.Count) % _targets.Count);
            MelonLogger.Msg("[SpectateMode] -> " + CurrentTargetName);
        }

        // Called from OnUpdate every frame while Enabled — just keeps our
        // own bike frozen. Camera-follow work is in OnPreRenderCamera
        // (below), which fires on Unity's Camera.onPreRender event.
        public static void Tick()
        {
            if (!Enabled) return;

            try
            {
                if ((object)_localVehicle != null && (object)_physField != null)
                    _physField.SetValue(_localVehicle, false);
                if ((object)_localRb != null && !_localRb.isKinematic)
                {
                    _localRb.velocity = Vector3.zero;
                    _localRb.angularVelocity = Vector3.zero;
                    _localRb.isKinematic = true;
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SpectateMode] Tick: " + ex.Message);
                Enabled = false;
                Disable();
            }
        }

        public static void LateTick() { /* camera work is in OnPreRenderCamera */ }

        private static void OnPreRenderCamera(Camera cam)
        {
            if (!Enabled) return;
            if ((object)cam == null || (object)Camera.main == null || cam != Camera.main) return;

            try
            {
                if (_targetIndex < 0 || _targetIndex >= _targets.Count)
                {
                    RefreshTargets();
                    if (_targets.Count > 0) SetTarget(0);
                    else return;
                }

                GameObject targetRoot = _targets[_targetIndex].Root;
                // Unity's own == here, deliberately NOT the (object) cast
                // used everywhere else in this codebase — (object) only
                // catches "never assigned", not "destroyed but the C#
                // reference still exists" (Unity's fake-null pattern).
                // A spectated player disconnecting mid-session is exactly
                // that case, and it crashed here without this.
                if (targetRoot == null) { RefreshTargets(); return; }

                if (!_camDetached)
                {
                    _camOriginalParent = cam.transform.parent;
                    if ((object)_camOriginalParent != null) cam.transform.SetParent(null, true);
                    _camDetached = true;
                }

                Vector3 targetPos = targetRoot.transform.position;

                // Flatten to the horizontal plane — pitch/roll from
                // balancing in place shouldn't move the camera at all,
                // only genuine yaw (turning) should.
                Vector3 forward = targetRoot.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.0001f) forward = _lastForward;
                else forward.Normalize();
                _lastForward = forward;

                // Direct, no lerp — see the field comment above for why
                // adding our own smoothing on top only made things worse.
                cam.transform.position = targetPos - forward * Distance + Vector3.up * Height;
                cam.transform.LookAt(targetPos + Vector3.up, Vector3.up);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SpectateMode] OnPreRenderCamera: " + ex.Message);
                Enabled = false;
                Disable();
            }
        }

        // Scene unload destroys every cached ref here (local player, all
        // spectate targets) — session-scoped by design, so this just turns
        // off cleanly rather than trying to reapply post-transition (same
        // convention as ChaosMode/RubberBandSteering, not FlyMode).
        public static void ClearCache()
        {
            _targets.Clear();
            _targetIndex = -1;
        }

        public static void Reset()
        {
            if (Enabled) { Enabled = false; Disable(); }
        }
    }
}
