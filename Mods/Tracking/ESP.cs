using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using MelonLoader;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    internal static class ESP
    {
        public static bool Enabled { get; private set; }
        public static bool ShowDistance { get; private set; } = true;
        public static bool ShowTracers { get; private set; } = true;
        public static bool ShowWorldObjects { get; private set; }

        private static GUIStyle _labelStyle;
        private static Texture2D _lineTexture;

        private static readonly List<ESPTarget> _targets = new List<ESPTarget>();
        private static readonly List<WorldTarget> _worldTargets = new List<WorldTarget>();

        private static float _lastPlayerRefreshTime = -999f;
        private const float PlayerRefreshInterval = 3f;
        private static Vector3 _cachedLocalPos;
        private static GameObject _cachedLocalPlayer;

        private const int WorldScanStepCount = 21;
        private static int _worldScanStep;
        private static bool _worldScanRunning;
        private static bool _pendingWorldScan;

        private const float HeadHeightOffset = 1.8f;

        private static readonly string IoTpiSFieldName = "\u0080ioTpiS";
        private static readonly string PlayerNameFieldName = "laxjiuc";

        private static FieldInfo _ioTpiSField = null;
        private static FieldInfo _playerNameField = null;

        private class ESPTarget
        {
            public Transform Root;
            public string DisplayName;
        }

        private class WorldTarget
        {
            public Transform Root;
            public string DisplayName;
            public Color Color;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("ESP -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void ToggleDistance()
        {
            ShowDistance = !ShowDistance;
            ModLog.Feedback("ESP Distance -> " + (ShowDistance ? "ON" : "OFF"));
        }

        public static void ToggleTracers()
        {
            ShowTracers = !ShowTracers;
            ModLog.Feedback("ESP Tracers -> " + (ShowTracers ? "ON" : "OFF"));
        }

        public static void ToggleWorldObjects()
        {
            ShowWorldObjects = !ShowWorldObjects;
            ModLog.Feedback("ESP World Objects -> " + (ShowWorldObjects ? "ON" : "OFF"));
            if (ShowWorldObjects) StartWorldScan();
            else StopWorldScan();
        }

        public static void RefreshNow()
        {
            RefreshTargets();
            StartWorldScan();
            _lastPlayerRefreshTime = Time.unscaledTime;
            ModLog.Debug("ESP targets refreshed: " + _targets.Count + " player(s), "
                + _worldTargets.Count + " world object(s) (world scan in progress="
                + _worldScanRunning + ").");
        }

        public static void Tick()
        {
            if (!Enabled && !ShowWorldObjects && !_worldScanRunning && !_pendingWorldScan)
                return;

            if (!UnityNull.Alive(_cachedLocalPlayer) || !_cachedLocalPlayer.activeInHierarchy)
                _cachedLocalPlayer = GameObject.Find("Player_Human");
            if (UnityNull.Alive(_cachedLocalPlayer))
                _cachedLocalPos = _cachedLocalPlayer.transform.position;

            if (_pendingWorldScan && UnityNull.Alive(_cachedLocalPlayer))
            {
                _pendingWorldScan = false;
                if (ShowWorldObjects) StartWorldScan();
            }

            if (_worldScanRunning)
                StepWorldScan();

            if (Enabled && Time.unscaledTime - _lastPlayerRefreshTime >= PlayerRefreshInterval)
            {
                RefreshTargets();
                _lastPlayerRefreshTime = Time.unscaledTime;
            }
        }

        public static void ClearCache()
        {
            _targets.Clear();
            _worldTargets.Clear();
            StopWorldScan();
            _cachedLocalPlayer = null;
            _lastPlayerRefreshTime = -999f;
            if (ShowWorldObjects) _pendingWorldScan = true;
        }

        // ── Rendering ─────────────────────────────────────────────────────────

        public static void OnGUI()
        {
            if (!Enabled && !ShowWorldObjects) return;

            Camera cam = Camera.main;
            if (!UnityNull.Alive(cam)) return;

            if ((object)_labelStyle == null)
            {
                try
                {
                    _labelStyle = new GUIStyle();
                    _labelStyle.fontSize = 16;
                    _labelStyle.fontStyle = FontStyle.Bold;
                    _labelStyle.alignment = TextAnchor.MiddleCenter;
                    _labelStyle.normal.textColor = Color.white;
                    _labelStyle.wordWrap = false;
                }
                catch { return; }
            }

            if ((object)_lineTexture == null)
            {
                try
                {
                    _lineTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    _lineTexture.SetPixel(0, 0, Color.white);
                    _lineTexture.Apply();
                }
                catch { }
            }

            Vector3 localPos = _cachedLocalPos;

            for (int i = 0; Enabled && i < _targets.Count; i++)
            {
                try
                {
                    ESPTarget target = _targets[i];
                    if ((object)target == null || !UnityNull.Alive(target.Root))
                    {
                        _targets.RemoveAt(i);
                        i--;
                        continue;
                    }

                    Vector3 rootPos = target.Root.position;

                    Vector3 worldPos = rootPos + Vector3.up * HeadHeightOffset;
                    Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

                    if (screenPos.z <= 0f) continue;

                    float screenX = screenPos.x;
                    float screenY = Screen.height - screenPos.y;

                    float dist = Vector3.Distance(localPos, rootPos);

                    string label = ShowDistance
                        ? target.DisplayName + "  [" + dist.ToString("0") + "m]"
                        : target.DisplayName;

                    float labelWidth = label.Length * 9f;
                    float labelHeight = 20f;

                    Rect labelRect = new Rect(
                        screenX - labelWidth * 0.5f,
                        screenY - 22f,
                        labelWidth,
                        labelHeight
                    );

                    GUI.Label(labelRect, label, _labelStyle);

                    if (ShowTracers && (object)_lineTexture != null)
                    {
                        Vector2 start = new Vector2(Screen.width * 0.5f, Screen.height - 40f);
                        Vector2 end = new Vector2(screenX, screenY);
                        DrawLine(start, end, 1.5f, Color.cyan);
                    }
                }
                catch { }
            }

            for (int i = 0; ShowWorldObjects && i < _worldTargets.Count; i++)
            {
                try
                {
                    WorldTarget target = _worldTargets[i];
                    if ((object)target == null || !UnityNull.Alive(target.Root))
                    {
                        _worldTargets.RemoveAt(i);
                        i--;
                        continue;
                    }

                    Vector3 rootPos = target.Root.position;

                    Vector3 screenPos = cam.WorldToScreenPoint(rootPos);
                    if (screenPos.z <= 0f) continue;

                    float screenX = screenPos.x;
                    float screenY = Screen.height - screenPos.y;

                    float dist = Vector3.Distance(localPos, rootPos);
                    string label = ShowDistance
                        ? target.DisplayName + "  [" + dist.ToString("0") + "m]"
                        : target.DisplayName;

                    float labelWidth = label.Length * 9f;
                    float labelHeight = 20f;

                    Color prevColor = _labelStyle.normal.textColor;
                    _labelStyle.normal.textColor = target.Color;
                    GUI.Label(new Rect(screenX - labelWidth * 0.5f, screenY - 22f, labelWidth, labelHeight),
                        label, _labelStyle);
                    _labelStyle.normal.textColor = prevColor;

                    if (ShowTracers && (object)_lineTexture != null)
                    {
                        Vector2 start = new Vector2(Screen.width * 0.5f, Screen.height - 40f);
                        Vector2 end = new Vector2(screenX, screenY);
                        DrawLine(start, end, 1.5f, target.Color);
                    }
                }
                catch { }
            }
        }

        // ── Target scanning ───────────────────────────────────────────────────

        private static void RefreshTargets()
        {
            _targets.Clear();

            try
            {
                Vehicle[] vehicles = UnityEngine.Object.FindObjectsOfType<Vehicle>();

                for (int i = 0; i < vehicles.Length; i++)
                {
                    Vehicle v = vehicles[i];
                    if (!UnityNull.Alive(v)) continue;

                    GameObject root = v.gameObject;
                    if ((object)root == null || !root.activeInHierarchy) continue;

                    if (string.Equals(root.name, "Player_Human", StringComparison.Ordinal))
                        continue;

                    string name = GetPlayerName(v, i);

                    _targets.Add(new ESPTarget
                    {
                        Root = root.transform,
                        DisplayName = name
                    });
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("ESP.RefreshTargets failed: " + ex.Message);
            }
        }

        private static void StartWorldScan()
        {
            _worldTargets.Clear();
            _worldScanStep = 0;
            _worldScanRunning = true;
        }

        private static void StopWorldScan()
        {
            _worldScanRunning = false;
            _worldScanStep = 0;
            _pendingWorldScan = false;
        }

        private static void StepWorldScan()
        {
            if (!_worldScanRunning) return;
            if (_worldScanStep >= WorldScanStepCount)
            {
                _worldScanRunning = false;
                return;
            }

            try { RunWorldScanStep(_worldScanStep); }
            catch (Exception ex) { ModLog.Warn("[ESP] World scan step " + _worldScanStep + ": " + ex.Message); }

            _worldScanStep++;
            if (_worldScanStep >= WorldScanStepCount)
                _worldScanRunning = false;
        }

        private static void RunWorldScanStep(int step)
        {
            Color gold = new Color(1f, 0.85f, 0.2f);
            Color cyan = new Color(0.3f, 0.9f, 1f);
            Color orange = new Color(1f, 0.4f, 0.1f);
            Color purple = new Color(0.6f, 0.4f, 1f);
            Color green = new Color(0.3f, 1f, 0.4f);
            Color red = new Color(1f, 0.15f, 0.15f);
            Color lightBlue = new Color(0.6f, 0.9f, 1f);

            switch (step)
            {
                case 0: AddWorldObjects<ScavengerHuntItem>("Scavenger Item", gold); break;
                case 1: AddWorldObjects<Collectible>("Collectible", gold); break;
                case 2: AddWorldObjects<PickupItem>("Pickup", gold); break;
                case 3: AddWorldObjects<ShortcutTrigger>("Shortcut", cyan); break;
                case 4: AddWorldObjects<Boost>("Boost Pad", orange); break;
                case 5: AddWorldObjects<Catapult>("Catapult", orange); break;
                case 6: AddWorldObjects<SpecialJumpTrigger>("Special Jump", orange); break;
                case 7: AddWorldObjects<ForceVolume>("Force Volume", purple); break;
                case 8: AddWorldObjects<BounceVolume>("Bounce Volume", purple); break;
                case 9: AddWorldObjects<WheelieVolume>("Wheelie Zone", purple); break;
                case 10: AddWorldObjects<Nobailvolume>("No-Bail Zone", green); break;
                case 11: AddWorldObjects<Checkpoint>("Checkpoint", green); break;
                case 12: AddWorldObjects<RouteCheckpoint>("Route Checkpoint", green); break;
                case 13: AddWorldObjects<StartLine>("Start Line", green); break;
                case 14: AddWorldObjects<FinishLine>("Finish Line", green); break;
                case 15: AddWorldObjects<AirBagVolume>("Air Bag", green); break;
                case 16: AddWorldObjects<Gap>("Gap", green); break;
                case 17: AddWorldObjects<Portal>("Portal", cyan); break;
                case 18: AddWorldObjects<DeathVolume>("Death Volume", red); break;
                case 19: AddWorldObjects<IceVolume>("Ice Patch", lightBlue); break;
                case 20: AddWorldObjects<HighFrictionVolume>("Sticky Ground", lightBlue); break;
            }
        }

        private static void RefreshWorldTargets()
        {
            StartWorldScan();
        }

        private static void AddWorldObjects<T>(string label, Color color) where T : Component
        {
            try
            {
                T[] objs = UnityEngine.Object.FindObjectsOfType<T>();
                for (int i = 0; i < objs.Length; i++)
                {
                    if ((object)objs[i] == null) continue;
                    GameObject go = objs[i].gameObject;
                    if ((object)go == null || !go.activeInHierarchy) continue;

                    _worldTargets.Add(new WorldTarget
                    {
                        Root = go.transform,
                        DisplayName = label,
                        Color = color
                    });
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("[ESP] AddWorldObjects<" + typeof(T).Name + "> failed: " + ex.Message);
            }
        }

        private static string GetPlayerName(Vehicle vehicle, int fallbackIndex)
        {
            try
            {
                if ((object)_ioTpiSField == null)
                {
                    FieldInfo[] fields = vehicle.GetType().GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.FlattenHierarchy
                    );

                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (string.Equals(fields[i].Name, IoTpiSFieldName, StringComparison.Ordinal))
                        {
                            _ioTpiSField = fields[i];
                            ModLog.Debug("[ESP] Found ioTpiS field: " + fields[i].Name.Length + " chars");
                            break;
                        }
                    }
                }

                if ((object)_ioTpiSField == null) return "Player " + (fallbackIndex + 1);

                object playerInfoImpact = _ioTpiSField.GetValue(vehicle);
                if ((object)playerInfoImpact == null) return "Player " + (fallbackIndex + 1);

                if ((object)_playerNameField == null)
                {
                    System.Type t = playerInfoImpact.GetType();
                    while ((object)t != null)
                    {
                        FieldInfo[] fields = t.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.DeclaredOnly
                        );

                        bool found = false;
                        for (int i = 0; i < fields.Length; i++)
                        {
                            if (string.Equals(fields[i].Name, PlayerNameFieldName, StringComparison.Ordinal))
                            {
                                _playerNameField = fields[i];
                                ModLog.Debug("[ESP] Found laxjiuc on " + t.Name);
                                found = true;
                                break;
                            }
                        }

                        if (found) break;
                        t = t.BaseType;
                    }
                }

                if ((object)_playerNameField == null) return "Player " + (fallbackIndex + 1);

                object photonPlayer = _playerNameField.GetValue(playerInfoImpact);
                if ((object)photonPlayer == null) return "Player " + (fallbackIndex + 1);

                string name = photonPlayer.ToString();
                if (!string.IsNullOrEmpty(name)) return name;
            }
            catch (Exception ex)
            {
                ModLog.Warn("[ESP] GetPlayerName failed: " + ex.Message);
            }

            return "Player " + (fallbackIndex + 1);
        }

        // ── Draw Line ─────────────────────────────────────────────────────────

        private static void DrawLine(Vector2 start, Vector2 end, float width, Color color)
        {
            try
            {
                Matrix4x4 oldMatrix = GUI.matrix;
                Color oldColor = GUI.color;

                Vector2 delta = end - start;
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                float length = delta.magnitude;

                GUI.color = color;
                GUIUtility.RotateAroundPivot(angle, start);
                GUI.DrawTexture(new Rect(start.x, start.y, length, width), _lineTexture);
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
            }
            catch { }
        }
    }
}

