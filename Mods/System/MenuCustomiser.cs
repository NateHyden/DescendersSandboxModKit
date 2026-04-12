using System;
using System.IO;
using DescendersModMenu.UI;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class MenuCustomiser
    {
        // ── Serialisable data ─────────────────────────────────────────
        [Serializable]
        private class MenuLayoutData
        {
            public int PositionPreset = 0;
            public int ScaleLevel = 3;
            public int OpacityLevel = 8;
        }

        // ── Save path ─────────────────────────────────────────────────
        private static readonly string SaveFolder =
            Path.Combine(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData"),
                "DescendersModMenu"
            );

        private static readonly string SaveFile =
            Path.Combine(SaveFolder, "MenuLayout.json");

        // ── State ─────────────────────────────────────────────────────
        // 0 = Centre, 1 = Top Left, 2 = Top Right
        public static int PositionPreset = 0;

        public static readonly string[] PositionLabels = { "Centre", "Top Left", "Top Right" };

        private static readonly float[] ScaleValues = { 0.7f, 0.8f, 0.9f, 1.0f, 1.1f, 1.2f };
        private static readonly string[] ScaleLabels = { "70%", "80%", "90%", "100%", "110%", "120%" };
        public static int ScaleLevel = 3; // default 100%

        private static readonly float[] OpacityValues = { 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f };
        private static readonly string[] OpacityLabels = { "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" };
        public static int OpacityLevel = 8; // default 100%

        // ── Display ───────────────────────────────────────────────────
        public static string ScaleDisplay => ScaleLabels[ScaleLevel];
        public static string OpacityDisplay => OpacityLabels[OpacityLevel];
        public static float CurrentOpacity => OpacityValues[OpacityLevel];
        public static bool ShowSavedIndicator => Time.realtimeSinceStartup - _savedTime < 5f;

        private static float _savedTime = -999f;

        // ── Setters (auto-save on every change) ───────────────────────
        public static void SetPosition(int preset)
        {
            PositionPreset = preset;
            Apply();
            SaveToFile();
        }

        public static void PrevScale()
        {
            if (ScaleLevel > 0) { ScaleLevel--; Apply(); SaveToFile(); }
        }

        public static void NextScale()
        {
            if (ScaleLevel < ScaleValues.Length - 1) { ScaleLevel++; Apply(); SaveToFile(); }
        }

        public static void PrevOpacity()
        {
            if (OpacityLevel > 0) { OpacityLevel--; Apply(); SaveToFile(); }
        }

        public static void NextOpacity()
        {
            if (OpacityLevel < OpacityValues.Length - 1) { OpacityLevel++; Apply(); SaveToFile(); }
        }

        // ── Reset to defaults ─────────────────────────────────────────
        public static void Reset()
        {
            PositionPreset = 0;
            ScaleLevel = 3;
            OpacityLevel = 8;
            Apply();
            SaveToFile();
        }

        // ── Apply ─────────────────────────────────────────────────────
        public static void Apply()
        {
            var rt = MenuWindow.RootRT;
            if ((object)rt == null) return;

            switch (PositionPreset)
            {
                case 0: // Centre
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    break;
                case 1: // Top Left
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(10f, -10f);
                    break;
                case 2: // Top Right
                    rt.anchorMin = new Vector2(1f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-10f, -10f);
                    break;
            }

            float s = ScaleValues[ScaleLevel];
            rt.localScale = new Vector3(s, s, 1f);

            var cg = MenuWindow.RootCanvasGroup;
            if ((object)cg != null)
                cg.alpha = OpacityValues[OpacityLevel];
        }

        // ── File I/O ──────────────────────────────────────────────────
        public static void LoadFromFile()
        {
            try
            {
                if (!File.Exists(SaveFile))
                {
                    MelonLogger.Msg("[MenuCustomiser] No layout file found, using defaults.");
                    Apply();
                    return;
                }

                string json = File.ReadAllText(SaveFile);
                var data = JsonUtility.FromJson<MenuLayoutData>(json);

                if ((object)data == null)
                {
                    MelonLogger.Warning("[MenuCustomiser] Layout file corrupt, using defaults.");
                    Apply();
                    return;
                }

                PositionPreset = Mathf.Clamp(data.PositionPreset, 0, PositionLabels.Length - 1);
                ScaleLevel = Mathf.Clamp(data.ScaleLevel, 0, ScaleValues.Length - 1);
                OpacityLevel = Mathf.Clamp(data.OpacityLevel, 0, OpacityValues.Length - 1);

                Apply();
                MelonLogger.Msg("[MenuCustomiser] Layout loaded.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[MenuCustomiser] LoadFromFile: " + ex.Message);
                Apply();
            }
        }

        public static void SaveToFile()
        {
            try
            {
                if (!Directory.Exists(SaveFolder))
                    Directory.CreateDirectory(SaveFolder);

                var data = new MenuLayoutData
                {
                    PositionPreset = PositionPreset,
                    ScaleLevel = ScaleLevel,
                    OpacityLevel = OpacityLevel,
                };

                File.WriteAllText(SaveFile, JsonUtility.ToJson(data, true));
                _savedTime = Time.realtimeSinceStartup;
                MelonLogger.Msg("[MenuCustomiser] Layout saved.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[MenuCustomiser] SaveToFile: " + ex.Message);
            }
        }
    }
}