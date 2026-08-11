using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Spectate chase-cam for Descenders multiplayer remotes.
    //
    // Decompile (VehicleNetworking / VehicleReplay / lKRMtV):
    //   Remotes are replay-driven. Full pose keyframes land every
    //   lKRMtV.Q_tLUUr (= 100) fixed frames via VehicleReplay.\u007Fg\u0084zUF\u0083 —
    //   a hard transform/velocity write. Buffer skips (ZbiDa^I) also teleport.
    //   Those snaps are the ~1–2s jolt; the camera was already smooth.
    //
    // Fix while spectating that remote:
    //   1) Harmony on keyframe appliers + buffer seeks: SoftDamp heal from
    //      pre-snap pose.
    //   2) Skip Vehicle.hgIcHdS on the spectated bike (Cubxx calls this after
    //      hard-setting pose — full local reset, irregular timing).
    //   3) Velocity-relative discontinuity heal + min heal duration as backup.
    //   4) Mild ZbiDa^I nudge; draw-only override while healing.
    public static class SpectateMode
    {
        public static bool Enabled { get; private set; } = false;

        private static readonly List<PlayerInfoImpact> _targets = new List<PlayerInfoImpact>();
        private static int _targetIndex = -1;
        private static string _activeName = "";
        private static Transform _targetTrans;
        private static Rigidbody _targetRb;
        private static PlayerInfoImpact _localImpact;
        private static PlayerManager _pm;

        private static FieldInfo _nameField;
        private static readonly string NameField = "a\u005EsXf\u0083Y";

        public static string CurrentTargetName =>
            !string.IsNullOrEmpty(_activeName) ? _activeName : "--";
        public static string StatusDisplay =>
            !Enabled ? "OFF" : (_targets.Count == 0 ? "No players found" : CurrentTargetName);
        public static int TargetCount => _targets.Count;

        public static float Distance { get; private set; } = 6f;
        public const float MinDistance = 3f, MaxDistance = 15f, DistanceStep = 1f;
        private const float Height = 2.5f;
        private const float YawSmoothTime = 0.25f;
        private const float HealSmoothTime = 0.28f;
        private const float MinHealSeconds = 0.22f;
        private const float DiscontinuityMeters = 0.4f;
        private const float SnapJumpMeters = 12f;
        private const float HealDoneMeters = 0.06f;

        private const string BufferLimitField = "ZbiDa\u005EI";
        private const int RaisedBufferLimit = 90;
        private static FieldInfo _bufferLimitField;
        private static int _savedBufferLimit = 50;
        private static bool _bufferRaised;

        public static void IncreaseDistance() { Distance = Mathf.Min(MaxDistance, Distance + DistanceStep); }
        public static void DecreaseDistance() { Distance = Mathf.Max(MinDistance, Distance - DistanceStep); }
        public static void SetDistance(float v) { Distance = Mathf.Clamp(v, MinDistance, MaxDistance); }

        private static VehicleController _localVc;
        private static MethodInfo _toggleCtrl;
        private static bool _savedNoBail;

        private static readonly List<BikeCamera> _disabledBikeCams = new List<BikeCamera>();
        private static Transform _camOriginalParent;
        private static bool _camDetached;

        private static float _orbitYaw;
        private static float _yawVel;
        private static Vector3 _smoothPos;
        private static Vector3 _posVel;
        private static Quaternion _smoothRot = Quaternion.identity;
        private static bool _haveSmooth;
        private static bool _healing;
        private static float _healUntil;
        private static Vector3 _prevRawPos;
        private static Quaternion _prevRawRot = Quaternion.identity;
        private static bool _havePrevRaw;
        private static float _prevFrameJump;
        private static Vector3 _prevSmoothPos;
        private static bool _havePrevSmooth;

        private static bool _poseOverridden;
        private static Vector3 _rawPos;
        private static Quaternion _rawRot = Quaternion.identity;
        private static Vector3 _rawVel;
        private static Vector3 _rawAngVel;
        private static bool _rawHadRb;

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            int patched = 0;

            // Keyframe appliers — each isolated so one failure doesn't kill the rest.
            MethodInfo keyPrefix = typeof(SpectateMode_KeyframePatch).GetMethod(
                "Prefix", BindingFlags.Public | BindingFlags.Static);
            string[] keyNames = { "\u007Fg\u0084zUF\u0083", "NH\u007CIuw\u0081" };
            for (int i = 0; i < keyNames.Length; i++)
            {
                try
                {
                    MethodInfo m = typeof(VehicleReplay).GetMethod(
                        keyNames[i], BindingFlags.NonPublic | BindingFlags.Instance);
                    if ((object)m == null)
                    {
                        // Steam names — Xbox/Game Pass CSharp often differs. Not a lobby issue.
                        ModLog.Debug("[SpectateMode] Keyframe method missing: " + i);
                        continue;
                    }
                    harmony.Patch(m, prefix: new HarmonyMethod(keyPrefix));
                    patched++;
                }
                catch (System.Exception ex)
                {
                    ModLog.Warn("[SpectateMode] Keyframe patch " + i + " failed: " + ex.Message);
                }
            }

            // Do NOT patch rjcGHqt — it's a [SpecialName] one-liner setter and
            // Harmony emits "IL Compile Error" on it. Buffer seeks are covered by
            // velocity-relative discontinuity healing in onPreRender.

            // Cubxx hard-sets call Vehicle.hgIcHdS(bool) afterward.
            try
            {
                MethodInfo resetLike = typeof(Vehicle).GetMethod(
                    "hgIcHdS", BindingFlags.Public | BindingFlags.Instance,
                    null, new System.Type[] { typeof(bool) }, null);
                if ((object)resetLike == null)
                    ModLog.Debug("[SpectateMode] hgIcHdS(bool) not found.");
                else
                {
                    MethodInfo hgPrefix = typeof(SpectateMode_HgPatch).GetMethod(
                        "Prefix", BindingFlags.Public | BindingFlags.Static);
                    harmony.Patch(resetLike, prefix: new HarmonyMethod(hgPrefix));
                    patched++;
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[SpectateMode] hgIcHdS patch failed: " + ex.Message);
            }

            if (patched == 0)
                ModLog.Warn("[SpectateMode] No spectate patches applied.");
            else
                ModLog.Debug("[SpectateMode] Applied " + patched + " spectate patch(es).");
        }

        public static bool IsWatchingReplay(VehicleReplay replay)
        {
            if (!Enabled || (object)replay == null) return false;
            if ((object)_targetTrans == null || _targetTrans == null) return false;
            return (object)replay.transform == (object)_targetTrans;
        }

        public static bool IsWatchingVehicle(Vehicle vehicle)
        {
            if (!Enabled || (object)vehicle == null) return false;
            if ((object)_targetTrans == null || _targetTrans == null) return false;
            return (object)vehicle.transform == (object)_targetTrans;
        }

        /// <summary>
        /// Harmony: before keyframe hard-set — anchor display at pre-snap pose.
        /// </summary>
        public static void OnKeyframeAboutToApply(VehicleReplay replay)
        {
            if (!IsWatchingReplay(replay)) return;
            try
            {
                Transform t = replay.transform;
                if ((object)t == null || t == null) return;
                BeginHealFromPose(t.position, t.rotation, reanchor: !_healing);
            }
            catch { }
        }

        /// <summary>
        /// Harmony: Cubxx already moved the transform; keep current display pose
        /// and skip the destructive hgIcHdS reset while we ease toward raw.
        /// Still zeroes velocity so the bike doesn't keep pre-teleport momentum.
        /// </summary>
        public static void OnNetworkResetAboutToRun(Vehicle vehicle)
        {
            if (!IsWatchingVehicle(vehicle)) return;
            if (_havePrevRaw)
                BeginHealFromPose(_prevRawPos, _prevRawRot, reanchor: true);
            else if (_haveSmooth)
                BeginHealFromPose(_smoothPos, _smoothRot, reanchor: false);
            else
                BeginHealFromPose(vehicle.transform.position, vehicle.transform.rotation, reanchor: false);

            try
            {
                Rigidbody rb = _targetRb;
                if ((object)rb == null || rb == null)
                    rb = vehicle.GetComponent<Rigidbody>();
                if ((object)rb != null && rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
            catch { }
        }

        private static void BeginHealFromPose(Vector3 pos, Quaternion rot, bool reanchor)
        {
            if (reanchor || !_haveSmooth)
            {
                _smoothPos = pos;
                _smoothRot = rot;
                _posVel = Vector3.zero;
            }
            _healing = true;
            _haveSmooth = true;
            _healUntil = Time.unscaledTime + MinHealSeconds;
        }

        private static bool _subscribed;
        private static void EnsureSubscribed()
        {
            if (_subscribed) return;
            Camera.onPreRender += OnPreRenderCamera;
            Camera.onPostRender += OnPostRenderCamera;
            _subscribed = true;
        }

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Enable();
            else Disable();
            ModLog.Feedback("[SpectateMode] -> " + (Enabled ? "ON" : "OFF"));
        }

        private static void Enable()
        {
            try
            {
                EnsureSubscribed();

                GameObject local = GameObject.Find("Player_Human");
                if ((object)local == null)
                { ModLog.Warn("[SpectateMode] Player_Human not found."); Enabled = false; return; }

                _localVc = local.GetComponent<VehicleController>();
                _pm = Object.FindObjectOfType<PlayerManager>();
                if ((object)_pm == null)
                { ModLog.Warn("[SpectateMode] PlayerManager missing."); Enabled = false; return; }

                _localImpact = _pm.GetPlayerImpact();
                if ((object)_localImpact == null)
                    _localImpact = _pm.GetPlayer() as PlayerInfoImpact;

                if ((object)_toggleCtrl == null)
                    _toggleCtrl = typeof(VehicleController).GetMethod("ToggleControl",
                        BindingFlags.Public | BindingFlags.Instance);

                if ((object)_localVc != null && (object)_toggleCtrl != null)
                    _toggleCtrl.Invoke(_localVc, new object[] { false, false });

                _savedNoBail = NoBail.Enabled;
                NoBail.SetEnabled(true);

                DisableBikeCameras();
                RaiseReplayBufferLimit();

                _haveSmooth = false;
                _havePrevSmooth = false;
                _havePrevRaw = false;
                _healing = false;
                _healUntil = 0f;
                _prevFrameJump = 0f;
                _poseOverridden = false;
                _posVel = Vector3.zero;
                _yawVel = 0f;

                RebuildTargetList();
                if (_targets.Count > 0)
                    WatchIndex(0);
                else
                    ModLog.Warn("[SpectateMode] No remote players to spectate.");

                ModLog.Debug("[SpectateMode] Enabled. " + _targets.Count + " target(s).");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SpectateMode] Enable: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "SpectateMode");
                Enabled = false;
            }
        }

        private static void Disable()
        {
            try
            {
                if (_camDetached)
                {
                    Camera cam = Camera.main;
                    if ((object)cam != null && (object)_camOriginalParent != null)
                        cam.transform.SetParent(_camOriginalParent, true);
                }
            }
            catch { }
            _camDetached = false;
            _camOriginalParent = null;

            try
            {
                if ((object)_localVc != null && (object)_toggleCtrl != null)
                    _toggleCtrl.Invoke(_localVc, new object[] { true, true });
                NoBail.SetEnabled(_savedNoBail);
            }
            catch (System.Exception ex) { MelonLogger.Error("[SpectateMode] Disable: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "SpectateMode"); }

            RestorePoseOverride();
            RestoreBikeCameras();
            RestoreReplayBufferLimit();

            try
            {
                if ((object)_localImpact != null)
                {
                    CameraManager cm = Object.FindObjectOfType<CameraManager>();
                    if ((object)cm != null) cm.SetCameraTarget(_localImpact, false);
                }
            }
            catch { }

            _localVc = null;
            _localImpact = null;
            _activeName = "";
            _targetTrans = null;
            _targetRb = null;
            _pm = null;
            _haveSmooth = false;
            _havePrevSmooth = false;
            _havePrevRaw = false;
            _healing = false;
            _healUntil = 0f;
            _prevFrameJump = 0f;
            _targets.Clear();
            _targetIndex = -1;
        }

        private static FieldInfo GetBufferLimitField()
        {
            if ((object)_bufferLimitField == null)
            {
                _bufferLimitField = typeof(VehicleNetworking).GetField(
                    BufferLimitField, BindingFlags.Public | BindingFlags.Static);
            }
            return _bufferLimitField;
        }

        private static void RaiseReplayBufferLimit()
        {
            try
            {
                FieldInfo f = GetBufferLimitField();
                if ((object)f == null)
                {
                    ModLog.Warn("[SpectateMode] Buffer limit field not found");
                    _bufferRaised = false;
                    return;
                }
                _savedBufferLimit = (int)f.GetValue(null);
                f.SetValue(null, RaisedBufferLimit);
                _bufferRaised = true;
                ModLog.Feedback("[SpectateMode] Replay buffer limit " + _savedBufferLimit + " -> " + RaisedBufferLimit);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[SpectateMode] Could not raise buffer limit: " + ex.Message);
                _bufferRaised = false;
            }
        }

        private static void RestoreReplayBufferLimit()
        {
            if (!_bufferRaised) return;
            try
            {
                FieldInfo f = GetBufferLimitField();
                if ((object)f != null) f.SetValue(null, _savedBufferLimit);
            }
            catch { }
            _bufferRaised = false;
        }

        public static void RefreshTargets() { RebuildTargetList(); }

        private static void RebuildTargetList()
        {
            _targets.Clear();
            try
            {
                if ((object)_pm == null) _pm = Object.FindObjectOfType<PlayerManager>();
                if ((object)_pm == null) return;

                if ((object)_localImpact == null)
                {
                    _localImpact = _pm.GetPlayerImpact();
                    if ((object)_localImpact == null)
                        _localImpact = _pm.GetPlayer() as PlayerInfoImpact;
                }

                PlayerInfo[] all = _pm.GetAllPlayers();
                for (int i = 0; i < all.Length; i++)
                {
                    PlayerInfoImpact pip = all[i] as PlayerInfoImpact;
                    if ((object)pip == null) continue;
                    if ((object)_localImpact != null && (object)pip == (object)_localImpact) continue;
                    if ((object)pip.bIvwNah == null) continue;
                    _targets.Add(pip);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SpectateMode] RebuildTargetList: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "SpectateMode");
            }
        }

        private static void WatchIndex(int index)
        {
            if (index < 0 || index >= _targets.Count) return;
            PlayerInfoImpact pip = _targets[index];
            _targetIndex = index;
            _activeName = GetName(pip);
            CacheTargetTransform(pip);
            _haveSmooth = false;
            _havePrevSmooth = false;
            _havePrevRaw = false;
            _healing = false;
            _healUntil = 0f;
            _prevFrameJump = 0f;
            _poseOverridden = false;
            _posVel = Vector3.zero;
            _yawVel = 0f;
            if ((object)_targetTrans != null && _targetTrans != null)
            {
                Vector3 f = _targetTrans.forward;
                f.y = 0f;
                if (f.sqrMagnitude > 0.0001f)
                    _orbitYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                _smoothPos = _targetTrans.position;
                _smoothRot = _targetTrans.rotation;
                _haveSmooth = true;
                _prevSmoothPos = _smoothPos;
                _havePrevSmooth = true;
                _prevRawPos = _smoothPos;
                _prevRawRot = _smoothRot;
                _havePrevRaw = true;
            }
        }

        private static void CacheTargetTransform(PlayerInfoImpact pip)
        {
            _targetTrans = null;
            _targetRb = null;
            if ((object)pip == null) return;
            try
            {
                if ((object)pip.bIvwNah != null)
                {
                    Vehicle v = pip.bIvwNah;
                    _targetTrans = v.transform;
                    _targetRb = v.GetComponent<Rigidbody>();
                    if ((object)_targetRb == null)
                        _targetRb = v.GetComponentInChildren<Rigidbody>();
                }
            }
            catch { }
        }

        private static void EnsureTargetTransform()
        {
            if ((object)_targetTrans != null && _targetTrans != null) return;
            if (string.IsNullOrEmpty(_activeName)) return;

            RebuildTargetList();
            int idx = IndexOfName(_activeName);
            if (idx >= 0)
            {
                _targetIndex = idx;
                CacheTargetTransform(_targets[idx]);
            }
            else if (_targets.Count > 0)
            {
                WatchIndex(0);
            }
        }

        public static void Next()
        {
            if (!Enabled) return;
            string keep = _activeName;
            RebuildTargetList();
            if (_targets.Count == 0) { _targetIndex = -1; _activeName = ""; _targetTrans = null; _targetRb = null; return; }
            int cur = IndexOfName(keep);
            if (cur < 0) cur = _targetIndex;
            if (cur < 0) cur = 0;
            WatchIndex((cur + 1) % _targets.Count);
            ModLog.Feedback("[SpectateMode] -> " + CurrentTargetName);
        }

        public static void Previous()
        {
            if (!Enabled) return;
            string keep = _activeName;
            RebuildTargetList();
            if (_targets.Count == 0) { _targetIndex = -1; _activeName = ""; _targetTrans = null; _targetRb = null; return; }
            int cur = IndexOfName(keep);
            if (cur < 0) cur = _targetIndex;
            if (cur < 0) cur = 0;
            WatchIndex((cur - 1 + _targets.Count) % _targets.Count);
            ModLog.Feedback("[SpectateMode] -> " + CurrentTargetName);
        }

        private static int IndexOfName(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;
            for (int i = 0; i < _targets.Count; i++)
            {
                if (string.Equals(GetName(_targets[i]), name, System.StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        public static void Tick()
        {
            if (!Enabled) return;
            for (int i = 0; i < _disabledBikeCams.Count; i++)
            {
                BikeCamera bc = _disabledBikeCams[i];
                if (bc != null && bc.enabled) bc.enabled = false;
            }
        }

        public static void LateTick() { }

        private static void OnPreRenderCamera(Camera cam)
        {
            if (!Enabled) return;
            if ((object)cam == null || (object)Camera.main == null || cam != Camera.main) return;

            // Never leave a stale override if we early-out.
            RestorePoseOverride();

            try
            {
                EnsureTargetTransform();
                if ((object)_targetTrans == null || _targetTrans == null) return;

                Vector3 rawPos = _targetTrans.position;
                Quaternion rawRot = _targetTrans.rotation;

                if (!_haveSmooth)
                {
                    _smoothPos = rawPos;
                    _smoothRot = rawRot;
                    _haveSmooth = true;
                    _healing = false;
                    _healUntil = 0f;
                    _posVel = Vector3.zero;
                    _prevRawPos = rawPos;
                    _prevRawRot = rawRot;
                    _havePrevRaw = true;
                    _prevFrameJump = 0f;
                }

                float frameJump = _havePrevRaw ? (rawPos - _prevRawPos).magnitude : 0f;
                float error = (rawPos - _smoothPos).magnitude;
                float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
                // Unexpected jump vs recent motion / speed — catches Cubxx hard-sets
                // that don't go through the keyframe Harmony hooks.
                float speed = (object)_targetRb != null && _targetRb != null
                    ? _targetRb.velocity.magnitude
                    : (_prevFrameJump / dt);
                float expectedMax = Mathf.Max(DiscontinuityMeters, speed * dt * 3.5f + 0.2f);

                if (error >= SnapJumpMeters || frameJump >= SnapJumpMeters)
                {
                    _smoothPos = rawPos;
                    _smoothRot = rawRot;
                    _posVel = Vector3.zero;
                    _healing = false;
                    _healUntil = 0f;
                }
                else if (frameJump >= expectedMax)
                {
                    BeginHealFromPose(_prevRawPos, _prevRawRot, reanchor: !_healing);
                }

                if (_healing)
                {
                    _smoothPos = Vector3.SmoothDamp(_smoothPos, rawPos, ref _posVel, HealSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
                    _smoothRot = Quaternion.Slerp(_smoothRot, rawRot, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
                    bool minTimeDone = Time.unscaledTime >= _healUntil;
                    if (minTimeDone && (rawPos - _smoothPos).magnitude <= HealDoneMeters)
                    {
                        _smoothPos = rawPos;
                        _smoothRot = rawRot;
                        _posVel = Vector3.zero;
                        _healing = false;
                    }
                }
                else
                {
                    _smoothPos = rawPos;
                    _smoothRot = rawRot;
                    _posVel = Vector3.zero;
                }

                _prevFrameJump = frameJump;
                _prevRawPos = rawPos;
                _prevRawRot = rawRot;
                _havePrevRaw = true;

                // Draw-only override: mesh matches camera for this frame, then
                // onPostRender puts the real replay pose/velocity back.
                _rawPos = rawPos;
                _rawRot = rawRot;
                _rawHadRb = (object)_targetRb != null && _targetRb != null;
                if (_rawHadRb)
                {
                    _rawVel = _targetRb.velocity;
                    _rawAngVel = _targetRb.angularVelocity;
                }
                _targetTrans.SetPositionAndRotation(_smoothPos, _smoothRot);
                if (_rawHadRb)
                {
                    _targetRb.position = _smoothPos;
                    _targetRb.rotation = _smoothRot;
                }
                _poseOverridden = true;

                float targetYaw = _orbitYaw;
                if (_havePrevSmooth)
                {
                    Vector3 move = _smoothPos - _prevSmoothPos;
                    move.y = 0f;
                    if (move.sqrMagnitude > 0.00005f)
                        targetYaw = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
                    else
                    {
                        Vector3 f = _smoothRot * Vector3.forward;
                        f.y = 0f;
                        if (f.sqrMagnitude > 0.0001f)
                            targetYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                    }
                }
                _prevSmoothPos = _smoothPos;
                _havePrevSmooth = true;
                _orbitYaw = Mathf.SmoothDampAngle(_orbitYaw, targetYaw, ref _yawVel, YawSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
                Vector3 forward = Quaternion.Euler(0f, _orbitYaw, 0f) * Vector3.forward;

                if (!_camDetached)
                {
                    _camOriginalParent = cam.transform.parent;
                    if ((object)_camOriginalParent != null)
                        cam.transform.SetParent(null, true);
                    _camDetached = true;
                }

                Vector3 lookAt = _smoothPos + Vector3.up;
                cam.transform.position = _smoothPos - forward * Distance + Vector3.up * Height;
                Vector3 to = lookAt - cam.transform.position;
                if (to.sqrMagnitude > 0.0001f)
                    cam.transform.rotation = Quaternion.LookRotation(to, Vector3.up);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SpectateMode] OnPreRender: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "SpectateMode");
                Enabled = false;
                Disable();
            }
        }

        private static void OnPostRenderCamera(Camera cam)
        {
            if ((object)cam == null || (object)Camera.main == null || cam != Camera.main) return;
            RestorePoseOverride();
        }

        private static void RestorePoseOverride()
        {
            if (!_poseOverridden) return;
            _poseOverridden = false;
            try
            {
                if ((object)_targetTrans == null || _targetTrans == null) return;
                _targetTrans.SetPositionAndRotation(_rawPos, _rawRot);
                if (_rawHadRb && (object)_targetRb != null && _targetRb != null)
                {
                    _targetRb.position = _rawPos;
                    _targetRb.rotation = _rawRot;
                    _targetRb.velocity = _rawVel;
                    _targetRb.angularVelocity = _rawAngVel;
                }
            }
            catch { }
        }

        private static void DisableBikeCameras()
        {
            RestoreBikeCameras();
            try
            {
                BikeCamera[] cams = Object.FindObjectsOfType<BikeCamera>();
                for (int i = 0; i < cams.Length; i++)
                {
                    if ((object)cams[i] == null || !cams[i].enabled) continue;
                    cams[i].enabled = false;
                    _disabledBikeCams.Add(cams[i]);
                }
            }
            catch (System.Exception ex) { ModLog.Warn("[SpectateMode] DisableBikeCameras: " + ex.Message); }
        }

        private static void RestoreBikeCameras()
        {
            for (int i = 0; i < _disabledBikeCams.Count; i++)
            {
                try
                {
                    if (_disabledBikeCams[i] != null)
                        _disabledBikeCams[i].enabled = true;
                }
                catch { }
            }
            _disabledBikeCams.Clear();
        }

        private static string GetName(PlayerInfoImpact pip)
        {
            if ((object)pip == null) return "Player";
            try
            {
                if ((object)_nameField == null)
                {
                    System.Type t = typeof(PlayerInfo);
                    while ((object)t != null)
                    {
                        FieldInfo f = t.GetField(NameField,
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.DeclaredOnly);
                        if ((object)f != null) { _nameField = f; break; }
                        t = t.BaseType;
                    }
                }
                if ((object)_nameField != null)
                {
                    object v = _nameField.GetValue(pip);
                    if (v is string s && !string.IsNullOrEmpty(s)) return s;
                }
            }
            catch { }
            return "Player";
        }

        public static void ClearCache()
        {
            _targets.Clear();
            _targetIndex = -1;
            _activeName = "";
            _targetTrans = null;
            _targetRb = null;
        }

        public static void Reset()
        {
            if (Enabled) { Enabled = false; Disable(); }
        }
    }

    public static class SpectateMode_KeyframePatch
    {
        public static void Prefix(VehicleReplay __instance)
        {
            SpectateMode.OnKeyframeAboutToApply(__instance);
        }
    }

    public static class SpectateMode_HgPatch
    {
        // Skip the destructive post-Cubxx reset on the spectated bike; ease instead.
        public static bool Prefix(Vehicle __instance, bool CjKxewL)
        {
            if (!SpectateMode.IsWatchingVehicle(__instance)) return true;
            SpectateMode.OnNetworkResetAboutToRun(__instance);
            return false;
        }
    }
}
