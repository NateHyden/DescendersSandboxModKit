using MelonLoader;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;

namespace DescendersModMenu.Mods
{
    public static class GhostReplay
    {
        public static bool Enabled { get; private set; } = false;
        public static bool IsRecording { get; private set; } = false;
        public static bool IsPlaying { get; private set; } = false;

        // Settings
        public static float GhostAlpha = 0.45f;
        public static Color GhostColor = new Color(0f, 0.8f, 1f, 0.45f);
        public static int RecordEvery = 2;
        public static int MaxFrames = 18000;

        // Public stats for UI
        public static int RecordedFrames { get { return _currentRun.Count; } }
        public static int SavedFrames { get { return _savedRun.Count; } }
        public static float RunTime { get; private set; } = 0f;
        public static float SavedRunTime { get; private set; } = 0f;

        private enum GhostState
        {
            Off,
            WaitingForMarker,   // press B to set spawn
            WaitingForMove,     // spawn set — waiting for movement
            Recording           // actively recording + ghost playing
        }

        private static GhostState _state = GhostState.Off;

        private struct GhostFrame
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public float Time;
        }

        private static readonly List<GhostFrame> _currentRun = new List<GhostFrame>(); // live recording
        private static readonly List<GhostFrame> _savedRun = new List<GhostFrame>(); // F4 saved ghost

        private static GameObject _ghostObj = null;
        private static int _frameCount = 0;
        private static int _playbackIdx = 0;
        private static float _playbackTime = 0f;
        private static Vector3 _spawnPos = Vector3.zero;
        private static float _graceEnd = 0f;
        private const float GracePeriod = 0.05f; // minimal - just blocks same-frame double fire

        // ── F3 Toggle ─────────────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled)
            {
                _state = GhostState.WaitingForMarker;
                _currentRun.Clear();
                IsRecording = false;
                IsPlaying = false;
                RunTime = 0f;
                BuildGhost();
                MelonLogger.Msg("[GhostReplay] ON — step 1: ride to start and press B.");
            }
            else
            {
                _state = GhostState.Off;
                IsRecording = false;
                IsPlaying = false;
                _currentRun.Clear();
                DestroyGhost();
                MelonLogger.Msg("[GhostReplay] OFF.");
            }
        }

        // ── F4 Save run ────────────────────────────────────────────────
        public static void SaveRun()
        {
            if (!Enabled || !IsRecording || _currentRun.Count < 30)
            {
                MelonLogger.Msg("[GhostReplay] Nothing to save (frames=" + _currentRun.Count + ").");
                return;
            }
            DoSave();
        }

        private static void DoSave()
        {
            _savedRun.Clear();
            _savedRun.AddRange(_currentRun);
            SavedRunTime = RunTime;
            // Rebuild ghost so it reflects current outfit
            BuildGhost();
            MelonLogger.Msg("[GhostReplay] SAVED " + _savedRun.Count + " frames, " + SavedRunTime.ToString("F1") + "s.");
        }

        private static float _lastRespawnTime = -999f;
        private const float RespawnCooldown = 2.0f;

        // ── On B pressed (respawn patch) — only restarts recording ────
        public static void OnRespawn()
        {
            if (!Enabled) return;

            float now = Time.realtimeSinceStartup;
            if (now - _lastRespawnTime < RespawnCooldown)
            {
                MelonLogger.Msg("[GhostReplay] Respawn ignored (double-fire).");
                return;
            }
            _lastRespawnTime = now;

            if (_state == GhostState.WaitingForMove || _state == GhostState.Recording)
            {
                // Auto-save first run if nothing saved yet and run is meaningful
                if (_savedRun.Count == 0 && _currentRun.Count >= 30)
                {
                    MelonLogger.Msg("[GhostReplay] Auto-saving first run (" + _currentRun.Count + " frames).");
                    DoSave();
                }

                // Restart recording — never change spawn point
                _currentRun.Clear();
                RunTime = 0f;
                _frameCount = 0;
                IsRecording = false;
                IsPlaying = false;
                _graceEnd = now + GracePeriod;
                _state = GhostState.WaitingForMove;
                MelonLogger.Msg("[GhostReplay] Reset — keeping spawn at " + _spawnPos);

                if ((object)_ghostObj != null && _savedRun.Count > 0)
                {
                    _ghostObj.transform.position = _savedRun[0].Position;
                    _ghostObj.transform.rotation = _savedRun[0].Rotation;
                    _ghostObj.SetActive(true);
                }
            }
            // WaitingForMarker — B does nothing for ghost replay, game handles respawn
        }

        // ── LS click — set spawn marker ───────────────────────────────
        public static void SetSpawnMarker()
        {
            if (!Enabled) return;
            if (_state == GhostState.WaitingForMarker || _state == GhostState.WaitingForMove || _state == GhostState.Recording)
            {
                GameObject p = GameObject.Find("Player_Human");
                _spawnPos = (object)p != null ? p.transform.position : Vector3.zero;
                _graceEnd = Time.realtimeSinceStartup + GracePeriod;
                _state = GhostState.WaitingForMove;
                IsRecording = false;
                IsPlaying = false;
                _currentRun.Clear();
                RunTime = 0f;
                _frameCount = 0;
                MelonLogger.Msg("[GhostReplay] LS — spawn set at " + _spawnPos + " waiting for move.");

                if ((object)_ghostObj != null && _savedRun.Count > 0)
                {
                    _ghostObj.transform.position = _savedRun[0].Position;
                    _ghostObj.transform.rotation = _savedRun[0].Rotation;
                    _ghostObj.SetActive(true);
                }
            }
        }

        // ── Main tick ──────────────────────────────────────────────────
        private static float _lastLogTime = 0f;
        public static void Tick()
        {
            if (!Enabled) return;

            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) return;
            Vector3 playerPos = player.transform.position;

            if (Time.realtimeSinceStartup - _lastLogTime > 2f)
            {
                _lastLogTime = Time.realtimeSinceStartup;
                MelonLogger.Msg("[GhostReplay] State=" + _state
                    + " frames=" + _currentRun.Count
                    + " saved=" + _savedRun.Count
                    + " dist=" + Vector3.Distance(playerPos, _spawnPos).ToString("F2")
                    + " ghost=" + ((object)_ghostObj != null && _ghostObj.activeSelf));
            }

            switch (_state)
            {
                case GhostState.WaitingForMarker:
                    break;

                case GhostState.WaitingForMove:
                    // During grace period keep updating spawn ref to actual settled position
                    if (Time.realtimeSinceStartup < _graceEnd)
                    {
                        _spawnPos = playerPos;
                        return;
                    }

                    // Pin ghost to start of saved run every frame while waiting
                    if ((object)_ghostObj != null && _savedRun.Count > 0)
                    {
                        _ghostObj.transform.position = _savedRun[0].Position;
                        _ghostObj.transform.rotation = _savedRun[0].Rotation;
                        _ghostObj.SetActive(true);
                    }

                    // Detect movement
                    if (Vector3.Distance(playerPos, _spawnPos) > 0.02f)
                    {
                        _state = GhostState.Recording;
                        IsRecording = true;
                        _frameCount = 0;
                        RunTime = 0f;
                        _currentRun.Clear();

                        // Start ghost playback the same frame
                        if (_savedRun.Count > 1)
                        {
                            _playbackIdx = 0;
                            _playbackTime = 0f;
                            IsPlaying = true;
                            if ((object)_ghostObj != null) _ghostObj.SetActive(true);
                            MelonLogger.Msg("[GhostReplay] MOVING — recording + ghost started simultaneously.");
                        }
                        else
                        {
                            MelonLogger.Msg("[GhostReplay] MOVING — recording only (no saved run yet, press F4 to save).");
                        }
                    }
                    break;

                case GhostState.Recording:
                    // Record current run
                    RunTime += Time.deltaTime;
                    _frameCount++;
                    if (_frameCount % RecordEvery == 0)
                    {
                        if (_currentRun.Count >= MaxFrames) _currentRun.RemoveAt(0);
                        _currentRun.Add(new GhostFrame
                        {
                            Position = playerPos,
                            Rotation = player.transform.rotation,
                            Time = RunTime
                        });
                    }

                    // Advance ghost playback
                    if (IsPlaying && (object)_ghostObj != null && _ghostObj.activeSelf)
                        AdvancePlayback();
                    break;
            }
        }

        private static void AdvancePlayback()
        {
            _playbackTime += Time.deltaTime;

            while (_playbackIdx < _savedRun.Count - 1
                && _savedRun[_playbackIdx + 1].Time <= _playbackTime)
                _playbackIdx++;

            if (_playbackIdx >= _savedRun.Count - 1)
            {
                // Hold at final position
                _ghostObj.transform.position = _savedRun[_savedRun.Count - 1].Position;
                _ghostObj.transform.rotation = _savedRun[_savedRun.Count - 1].Rotation;
                IsPlaying = false;
                return;
            }

            var a = _savedRun[_playbackIdx];
            var b = _savedRun[_playbackIdx + 1];
            float span = b.Time - a.Time;
            float t = span > 0f ? (_playbackTime - a.Time) / span : 1f;
            _ghostObj.transform.position = Vector3.Lerp(a.Position, b.Position, Mathf.Clamp01(t));
            _ghostObj.transform.rotation = Quaternion.Slerp(a.Rotation, b.Rotation, Mathf.Clamp01(t));
        }

        // ── Ghost visibility helpers ───────────────────────────────────
        public static string GetStateLabel()
        {
            switch (_state)
            {
                case GhostState.WaitingForMarker: return "STEP 1: RIDE TO START";
                case GhostState.WaitingForMove: return "STEP 2: WAITING FOR MOVE";
                case GhostState.Recording: return "RECORDING";
                default: return "OFF";
            }
        }

        public static bool HasSavedRun { get { return _savedRun.Count > 1; } }

        // ── Ghost object ──────────────────────────────────────────────
        private static void BuildGhost()
        {
            DestroyGhost();
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) { MelonLogger.Warning("[GhostReplay] No player."); return; }

                _ghostObj = new GameObject("GhostRider");

                Transform cyclist = player.transform.Find("Cyclist");
                if ((object)(UnityEngine.Object)cyclist != null)
                {
                    var c = GameObject.Instantiate(cyclist.gameObject);
                    c.transform.SetParent(_ghostObj.transform, false);
                    StripAndTint(c);
                }

                Transform bike = player.transform.Find("BikeModel");
                if ((object)(UnityEngine.Object)bike != null)
                {
                    var b = GameObject.Instantiate(bike.gameObject);
                    b.transform.SetParent(_ghostObj.transform, false);
                    StripAndTint(b);
                }

                _ghostObj.SetActive(false);
                MelonLogger.Msg("[GhostReplay] Ghost built.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[GhostReplay] BuildGhost: " + ex.Message); }
        }

        private static void StripAndTint(GameObject go)
        {
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>()) GameObject.Destroy(rb);
            foreach (var col in go.GetComponentsInChildren<Collider>()) GameObject.Destroy(col);
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
            foreach (var ani in go.GetComponentsInChildren<Animator>()) ani.enabled = false;

            // Build ghost material — additive blending glows through objects like a real ghost
            var shader = Shader.Find("Particles/Additive");
            bool useAdd = (object)(UnityEngine.Object)shader != null;
            if (!useAdd) { shader = Shader.Find("Unlit/Transparent"); useAdd = (object)(UnityEngine.Object)shader != null; }
            if (!useAdd) shader = Shader.Find("Standard");

            Material ghostMat;
            if (useAdd)
            {
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                ghostMat = new Material(shader);
                ghostMat.mainTexture = tex;
                // Low intensity cyan — additive so it glows without being solid
                ghostMat.color = new Color(0f, 0.5f, 0.7f, 0.2f);
            }
            else
            {
                ghostMat = new Material(shader);
                ghostMat.color = new Color(0f, 0.8f, 1f, 1f);
            }

            // Bake each SkinnedMeshRenderer into a static MeshFilter/MeshRenderer
            // This freezes the current animated pose permanently — no more T-pose or ragdoll
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                try
                {
                    var bakedMesh = new Mesh();
                    smr.BakeMesh(bakedMesh);

                    var go2 = smr.gameObject;
                    smr.enabled = false; // hide original skinned mesh

                    // Add static mesh components with baked pose
                    var mf = go2.AddComponent<MeshFilter>();
                    var mr = go2.AddComponent<MeshRenderer>();
                    mf.mesh = bakedMesh;

                    var mats = new Material[smr.sharedMaterials.Length];
                    for (int m = 0; m < mats.Length; m++) mats[m] = ghostMat;
                    mr.materials = mats;
                }
                catch (System.Exception ex)
                { MelonLogger.Warning("[GhostReplay] BakeMesh failed: " + ex.Message); smr.enabled = false; }
            }

            // Regular MeshRenderers (bike frame, wheels)
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
            {
                if (!mr.enabled) continue; // skip baked meshes we just disabled
                var mats = mr.materials;
                for (int m = 0; m < mats.Length; m++) mats[m] = ghostMat;
                mr.materials = mats;
            }
        }

        private static void DestroyGhost()
        {
            if ((object)_ghostObj != null) { GameObject.Destroy(_ghostObj); _ghostObj = null; }
            IsPlaying = false;
        }

        // ── Scene hooks ───────────────────────────────────────────────
        public static void OnSceneLoaded()
        {
            if (!Enabled) return;
            _state = GhostState.WaitingForMarker;
            IsRecording = false;
            IsPlaying = false;
            _currentRun.Clear();
        }

        public static void OnSceneInitialized()
        {
            if (!Enabled) return;
            BuildGhost();
        }

        public static void ClearSavedRun()
        {
            _savedRun.Clear();
            SavedRunTime = 0f;
            IsPlaying = false;
            if ((object)_ghostObj != null) _ghostObj.SetActive(false);
            MelonLogger.Msg("[GhostReplay] Saved run cleared.");
        }

        public static void Reset()
        {
            Enabled = false;
            _state = GhostState.Off;
            IsRecording = false;
            DestroyGhost();
            _currentRun.Clear();
        }

        // ── Harmony patches ───────────────────────────────────────────
        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                var postfix = typeof(GhostRespawn_Patch).GetMethod("Postfix",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                var m1 = typeof(PlayerInfoImpact).GetMethod("RespawnAtStartLine",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)m1 != null)
                    harmony.Patch(m1, postfix: new HarmonyLib.HarmonyMethod(postfix));

                var m2 = typeof(PlayerInfoImpact).GetMethod("RespawnOnTrack",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)m2 != null)
                    harmony.Patch(m2, postfix: new HarmonyLib.HarmonyMethod(postfix));

                MelonLogger.Msg("[GhostReplay] Patched respawn methods.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[GhostReplay] ApplyPatch: " + ex.Message); }
        }
    }

    public static class GhostRespawn_Patch
    {
        public static void Postfix() { GhostReplay.OnRespawn(); }
    }
}