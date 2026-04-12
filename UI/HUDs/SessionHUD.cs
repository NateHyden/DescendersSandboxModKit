using UnityEngine;

namespace DescendersModMenu.UI
{
    public static class SessionHUD
    {
        public static bool Enabled = false;

        private static Texture2D _tex;
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
        private static readonly Color AccentCol = new Color(0.678f, 1.000f, 0.184f, 1.000f); // neon lime
        private static readonly Color BgCol = new Color(0.055f, 0.063f, 0.055f, 0.850f);
        private static readonly Color BorderCol = new Color(0.678f, 1.000f, 0.184f, 0.220f);
        private static readonly Color LabelCol = new Color(1.000f, 1.000f, 1.000f, 0.420f);
        private static readonly Color ValueCol = new Color(0.678f, 1.000f, 0.184f, 1.000f);
        private static readonly Color ValueWhite = new Color(1.000f, 1.000f, 1.000f, 0.870f);
        private static readonly Color DividerCol = new Color(1.000f, 1.000f, 1.000f, 0.060f);

        // ── Layout — all values are at 1080p base, scaled at runtime ──
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
            if (!Enabled) return;

            float sh = Screen.height;
            float sw = Screen.width;
            float s = sh / BaseRes;   // scale factor

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
                + rowH          // Time
                + rowH          // Top Speed
                + divGap + divH + divGap
                + rowH          // Bails
                + rowH          // Checkpoints
                + divGap + divH + divGap
                + rowH          // Airtime
                + rowH          // G-Force
                + rowH          // Peak G
                + pad * 0.5f;

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

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = titleFs,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = AccentCol }
            };
            GUI.Label(new Rect(x + accentBarW + pad * 0.6f, y, panelW, titleH), "SESSION", titleStyle);

            // ── Rows ──────────────────────────────────────────────────
            float cy = y + titleH + pad * 0.4f;

            cy = DrawRow(x, cy, panelW, pad, rowH, labelFs, valueFs, "TIME", Mods.SessionTrackers.SessionTimeDisplay, false);
            cy = DrawRow(x, cy, panelW, pad, rowH, labelFs, valueFs, "TOP SPEED", Mods.TopSpeed.DisplayValue, true);
            cy = DrawDivider(x, cy, panelW, pad, divH, divGap);
            cy = DrawRow(x, cy, panelW, pad, rowH, labelFs, valueFs, "BAILS", Mods.SessionTrackers.BailCountDisplay, false);
            cy = DrawRow(x, cy, panelW, pad, rowH, labelFs, valueFs, "CHECKPOINTS", Mods.SessionTrackers.CheckpointCountDisplay, false);
            cy = DrawDivider(x, cy, panelW, pad, divH, divGap);
            cy = DrawRow(x, cy, panelW, pad, rowH, labelFs, valueFs, "AIRTIME", Mods.SessionTrackers.AirtimeDisplay, true);
            cy = DrawRow(x, cy, panelW, pad, rowH, labelFs, valueFs, "G-FORCE", Mods.SessionTrackers.GForceDisplay, false);
            cy = DrawRow(x, cy, panelW, pad, rowH, labelFs, valueFs, "PEAK G", Mods.SessionTrackers.PeakGForceDisplay, true);
        }

        // ── Helpers ───────────────────────────────────────────────────
        private static float DrawRow(float px, float py, float pw, float pad, float rowH,
            int labelFs, int valueFs, string label, string value, bool accent)
        {
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = labelFs,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = LabelCol }
            };
            var valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = valueFs,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = accent ? ValueCol : ValueWhite }
            };

            float innerW = pw - pad * 2f;
            GUI.Label(new Rect(px + pad, py, innerW * 0.55f, rowH), label, labelStyle);
            GUI.Label(new Rect(px + pad + innerW * 0.45f, py, innerW * 0.55f, rowH), value, valueStyle);

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