using DescendersModMenu.Mods;
using UnityEngine;

namespace DescendersModMenu.UI
{
    public static class SessionHUD
    {
        public static bool Enabled = false;

        /// <summary>Height of the last drawn panel (0 if not drawn this frame).</summary>
        public static float LastDrawnHeight { get; private set; }

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("[SessionHUD] -> " + (Enabled ? "ON" : "OFF"));
        }

        private static Texture2D _tex;
        private static GUIStyle _titleStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _valueStyle;
        private static GUIStyle _valueAccentStyle;
        private static int _styleTitleFs;
        private static int _styleLabelFs;
        private static int _styleValueFs;

        private static Texture2D Tex
        {
            get
            {
                if ((object)_tex == null)
                {
                    _tex = new Texture2D(1, 1);
                    _tex.SetPixel(0, 0, Color.white);
                    _tex.Apply();
                }
                return _tex;
            }
        }

        // ── Colours ───────────────────────────────────────────────────
        private static readonly Color AccentCol = new Color(0.678f, 1.000f, 0.184f, 1.000f);
        private static readonly Color BgCol = new Color(0.055f, 0.063f, 0.055f, 0.850f);
        private static readonly Color BorderCol = new Color(0.678f, 1.000f, 0.184f, 0.220f);
        private static readonly Color LabelCol = new Color(1.000f, 1.000f, 1.000f, 0.420f);
        private static readonly Color ValueCol = new Color(0.678f, 1.000f, 0.184f, 1.000f);
        private static readonly Color ValueWhite = new Color(1.000f, 1.000f, 1.000f, 0.870f);
        private static readonly Color DividerCol = new Color(1.000f, 1.000f, 1.000f, 0.060f);

        private const float BasePanelW = 220f;
        private const float BasePad = 12f;
        private const float BaseRowH = 22f;
        private const float BaseTitleH = 26f;
        private const float BaseDivH = 1f;
        private const float BaseDivGap = 4f;
        private const float BaseMarginX = 18f;
        private const float BaseMarginY = 18f;
        private const float BaseRes = 1080f;

        public static void Draw()
        {
            LastDrawnHeight = 0f;
            if (!Enabled) return;

            float sh = Screen.height;
            float sw = Screen.width;
            float s = sh / BaseRes;

            float panelW = BasePanelW * s;
            float pad = BasePad * s;
            float rowH = BaseRowH * s;
            float titleH = BaseTitleH * s;
            float divH = Mathf.Max(1f, BaseDivH * s);
            float divGap = BaseDivGap * s;
            float marginX = BaseMarginX * s;
            float marginY = BaseMarginY * s;

            int labelFs = Mathf.RoundToInt(10f * s);
            int valueFs = Mathf.RoundToInt(12f * s);
            int titleFs = Mathf.RoundToInt(10f * s);

            float totalH = titleH + pad * 0.5f
                + rowH
                + rowH
                + divGap + divH + divGap
                + rowH
                + rowH
                + divGap + divH + divGap
                + rowH
                + rowH
                + rowH
                + pad * 0.5f;

            LastDrawnHeight = totalH;

            float x = sw - panelW - marginX;
            float y = marginY;

            // ── Panel background ──────────────────────────────────────
            DrawRect(x, y, panelW, totalH, BgCol);

            // ── Border ────────────────────────────────────────────────
            float b = Mathf.Max(1f, s);
            DrawRect(x, y, panelW, b, BorderCol);
            DrawRect(x, y + totalH - b, panelW, b, BorderCol);
            DrawRect(x, y, b, totalH, BorderCol);
            DrawRect(x + panelW - b, y, b, totalH, BorderCol);

            // ── Title bar ─────────────────────────────────────────────
            float accentBarW = Mathf.Max(2f, 3f * s);
            DrawRect(x, y, accentBarW, titleH, AccentCol);
            DrawRect(x, y + titleH, panelW, b, new Color(0.678f, 1f, 0.184f, 0.15f));

            EnsureStyles(titleFs, labelFs, valueFs);
            GUI.Label(new Rect(x + accentBarW + pad * 0.6f, y, panelW, titleH), "SESSION", _titleStyle);

            // ── Rows ──────────────────────────────────────────────────
            float cy = y + titleH + pad * 0.4f;

            cy = DrawRow(x, cy, panelW, pad, rowH, "TIME", Mods.SessionTrackers.SessionTimeDisplay, false);
            cy = DrawRow(x, cy, panelW, pad, rowH, "TOP SPEED", Mods.TopSpeed.DisplayValue, true);
            cy = DrawDivider(x, cy, panelW, pad, divH, divGap);
            cy = DrawRow(x, cy, panelW, pad, rowH, "BAILS", Mods.SessionTrackers.BailCountDisplay, false);
            cy = DrawRow(x, cy, panelW, pad, rowH, "CHECKPOINTS", Mods.SessionTrackers.CheckpointCountDisplay, false);
            cy = DrawDivider(x, cy, panelW, pad, divH, divGap);
            cy = DrawRow(x, cy, panelW, pad, rowH, "AIRTIME", Mods.SessionTrackers.AirtimeDisplay, true);
            cy = DrawRow(x, cy, panelW, pad, rowH, "G-FORCE", Mods.SessionTrackers.GForceDisplay, false);
            cy = DrawRow(x, cy, panelW, pad, rowH, "PEAK G", Mods.SessionTrackers.PeakGForceDisplay, true);
        }

        private static void EnsureStyles(int titleFs, int labelFs, int valueFs)
        {
            if ((object)_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = AccentCol }
                };
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = LabelCol }
                };
                _valueStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = ValueWhite }
                };
                _valueAccentStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = ValueCol }
                };
            }
            if (_styleTitleFs != titleFs) { _titleStyle.fontSize = titleFs; _styleTitleFs = titleFs; }
            if (_styleLabelFs != labelFs) { _labelStyle.fontSize = labelFs; _styleLabelFs = labelFs; }
            if (_styleValueFs != valueFs)
            {
                _valueStyle.fontSize = valueFs;
                _valueAccentStyle.fontSize = valueFs;
                _styleValueFs = valueFs;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────
        private static float DrawRow(float px, float py, float pw, float pad, float rowH,
            string label, string value, bool accent)
        {
            float innerW = pw - pad * 2f;
            GUI.Label(new Rect(px + pad, py, innerW * 0.55f, rowH), label, _labelStyle);
            GUI.Label(new Rect(px + pad + innerW * 0.45f, py, innerW * 0.55f, rowH), value,
                accent ? _valueAccentStyle : _valueStyle);

            return py + rowH;
        }

        private static float DrawDivider(float px, float py, float pw, float pad, float divH, float divGap)
        {
            float cy = py + divGap;
            DrawRect(px + pad, cy, pw - pad * 2f, divH, DividerCol);
            return cy + divH + divGap;
        }

        private static void DrawRect(float rx, float ry, float rw, float rh, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(rx, ry, rw, rh), Tex);
            GUI.color = Color.white;
        }
    }
}

