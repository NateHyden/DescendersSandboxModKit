using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using MelonLoader;

namespace DescendersModMenu.Mods
{
    internal static class ESP
    {
        public static bool Enabled { get; private set; }
        public static bool ShowDistance { get; private set; } = true;
        public static bool ShowTracers { get; private set; } = true;

        private static GUIStyle _labelStyle;
        private static Texture2D _lineTexture;

        private static readonly List<ESPTarget> _targets = new List<ESPTarget>();
        private static float _lastRefreshTime = -999f;
        private const float RefreshInterval = 1.0f;
        private const float HeadHeightOffset = 1.8f;

        // \u0080ioTpiS - the leading byte is U+0080 (confirmed from raw dump bytes)
        private static readonly string IoTpiSFieldName = "\u0080ioTpiS";
        // laxjiuc - clean readable field name, holds a Photon Player whose ToString() = player name
        private static readonly string PlayerNameFieldName = "laxjiuc";

        private static FieldInfo _ioTpiSField = null;
        private static FieldInfo _playerNameField = null;

        private class ESPTarget
        {
            public Transform Root;
            public string DisplayName;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public static void Toggle()
        {
            Enabled = !Enabled;
            MelonLogger.Msg("ESP -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void ToggleDistance()
        {
            ShowDistance = !ShowDistance;
            MelonLogger.Msg("ESP Distance -> " + (ShowDistance ? "ON" : "OFF"));
        }

        public static void ToggleTracers()
        {
            ShowTracers = !ShowTracers;
            MelonLogger.Msg("ESP Tracers -> " + (ShowTracers ? "ON" : "OFF"));
        }

        public static void RefreshNow()
        {
            RefreshTargets();
            _lastRefreshTime = Time.unscaledTime;
            MelonLogger.Msg("ESP targets refreshed: " + _targets.Count + " player(s).");
        }

        // ── Rendering ─────────────────────────────────────────────────────────

        public static void OnGUI()
        {
            if (!Enabled) return;

            Camera cam = Camera.main;
            if ((object)cam == null) return;

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

            if (Time.unscaledTime - _lastRefreshTime >= RefreshInterval)
            {
                RefreshTargets();
                _lastRefreshTime = Time.unscaledTime;
            }

            GameObject localPlayer = GameObject.Find("Player_Human");
            Vector3 localPos = (object)localPlayer != null
                ? localPlayer.transform.position
                : Vector3.zero;

            for (int i = 0; i < _targets.Count; i++)
            {
                try
                {
                    ESPTarget target = _targets[i];
                    if ((object)target == null) continue;

                    Vector3 rootPos;
                    try { rootPos = target.Root.position; }
                    catch { continue; }

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
                    if ((object)v == null) continue;

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
                MelonLogger.Warning("ESP.RefreshTargets failed: " + ex.Message);
            }
        }

        // Chain: Vehicle --(ioTpiS field, has U+0080 prefix)--> PlayerInfoImpact
        //        PlayerInfoImpact --(laxjiuc field)--> Photon Player object
        //        Photon Player .ToString() --> "PlayerName"
        private static string GetPlayerName(Vehicle vehicle, int fallbackIndex)
        {
            try
            {
                // Find and cache the \u0080ioTpiS field on Vehicle
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
                            MelonLogger.Msg("[ESP] Found ioTpiS field: " + fields[i].Name.Length + " chars");
                            break;
                        }
                    }
                }

                if ((object)_ioTpiSField == null) return "Player " + (fallbackIndex + 1);

                object playerInfoImpact = _ioTpiSField.GetValue(vehicle);
                if ((object)playerInfoImpact == null) return "Player " + (fallbackIndex + 1);

                // Find and cache laxjiuc on PlayerInfoImpact (or its base classes)
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
                                MelonLogger.Msg("[ESP] Found laxjiuc on " + t.Name);
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

                // Photon Player.ToString() returns the NickName directly
                string name = photonPlayer.ToString();
                if (!string.IsNullOrEmpty(name)) return name;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[ESP] GetPlayerName failed: " + ex.Message);
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