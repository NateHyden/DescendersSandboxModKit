using UnityEngine;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    public static class LavaRisingHUD
    {
        private static bool _stylesBuilt;
        private static GUIStyle _countStyle;
        private static GUIStyle _goStyle;
        private static GUIStyle _infoStyle;
        private static GUIStyle _caughtStyle;
        private static GUIStyle _winStyle;
        private static GUIStyle _flashStyle;
        private static GUIStyle _compassLabelStyle;
        private static GUIStyle _compassDistStyle;
        private static Texture2D _darkTex;
        private static Texture2D _caughtTex;
        private static Texture2D _winTex;
        private static Texture2D _compassBgTex;

        private static void BuildStyles()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            _darkTex = MakeTex(new Color(0f, 0f, 0f, 0.55f));
            _caughtTex = MakeTex(new Color(0.55f, 0.04f, 0.02f, 0.88f));
            _winTex = MakeTex(new Color(0.05f, 0.35f, 0.08f, 0.88f));
            _compassBgTex = MakeTex(new Color(0.06f, 0.08f, 0.1f, 0.82f));

            _countStyle = new GUIStyle();
            _countStyle.alignment = TextAnchor.MiddleCenter;
            _countStyle.fontStyle = FontStyle.Bold;
            _countStyle.normal.textColor = new Color(1f, 0.35f, 0.08f, 1f);

            _goStyle = new GUIStyle();
            _goStyle.alignment = TextAnchor.MiddleCenter;
            _goStyle.fontStyle = FontStyle.Bold;
            _goStyle.normal.textColor = new Color(1f, 0.95f, 0.2f, 1f);

            _infoStyle = new GUIStyle();
            _infoStyle.alignment = TextAnchor.MiddleCenter;
            _infoStyle.fontStyle = FontStyle.Bold;
            _infoStyle.normal.textColor = Color.white;
            _infoStyle.padding = new RectOffset(14, 14, 6, 6);

            _caughtStyle = new GUIStyle();
            _caughtStyle.alignment = TextAnchor.MiddleCenter;
            _caughtStyle.fontStyle = FontStyle.Bold;
            _caughtStyle.normal.textColor = Color.white;
            _caughtStyle.padding = new RectOffset(28, 28, 16, 16);

            _winStyle = new GUIStyle();
            _winStyle.alignment = TextAnchor.MiddleCenter;
            _winStyle.fontStyle = FontStyle.Bold;
            _winStyle.normal.textColor = Color.white;
            _winStyle.padding = new RectOffset(28, 28, 16, 16);

            _flashStyle = new GUIStyle();
            _flashStyle.alignment = TextAnchor.MiddleCenter;
            _flashStyle.fontStyle = FontStyle.Bold;
            _flashStyle.normal.textColor = new Color(1f, 0.12f, 0.08f, 1f);

            _compassLabelStyle = new GUIStyle();
            _compassLabelStyle.alignment = TextAnchor.MiddleCenter;
            _compassLabelStyle.fontStyle = FontStyle.Bold;
            _compassLabelStyle.normal.textColor = new Color(0.55f, 1f, 0.65f, 1f);
            _compassLabelStyle.wordWrap = true;
            _compassLabelStyle.padding = new RectOffset(4, 4, 2, 2);

            _compassDistStyle = new GUIStyle();
            _compassDistStyle.alignment = TextAnchor.MiddleCenter;
            _compassDistStyle.fontStyle = FontStyle.Bold;
            _compassDistStyle.normal.textColor = Color.white;
            _compassDistStyle.padding = new RectOffset(4, 4, 2, 2);
        }

        private static Texture2D MakeTex(Color col)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }

        private static void DrawSummitRemaining(float sw, float sh)
        {
            if (!LavaRising.HasSummit) return;

            float dist;
            if (!LavaRising.TryGetSummitRemaining(out dist)) return;

            const string title = "SUMMIT";
            string distTxt = Mathf.CeilToInt(dist).ToString() + "m up";

            _compassLabelStyle.fontSize = Mathf.RoundToInt(sh * 0.015f);
            _compassDistStyle.fontSize = Mathf.RoundToInt(sh * 0.022f);

            float padX = 14f;
            float padY = 10f;
            float rowGap = 8f;

            Vector2 titleSize = _compassLabelStyle.CalcSize(new GUIContent(title));
            Vector2 distSize = _compassDistStyle.CalcSize(new GUIContent(distTxt));

            float panelW = Mathf.Max(titleSize.x, distSize.x) + padX * 2f;
            panelW = Mathf.Clamp(panelW, 200f, sw * 0.28f);
            float panelH = padY + titleSize.y + rowGap + distSize.y + padY;
            float margin = Mathf.Clamp(sw * 0.02f, 14f, 36f);
            float x = sw - panelW - margin;
            float y = margin + sh * 0.06f;

            GUI.DrawTexture(new Rect(x, y, panelW, panelH), _compassBgTex);

            float textW = panelW - padX * 2f;
            float titleY = y + padY;
            GUI.Label(new Rect(x + padX, titleY, textW, titleSize.y), title, _compassLabelStyle);

            float distY = titleY + titleSize.y + rowGap;
            GUI.Label(new Rect(x + padX, distY, textW, distSize.y), distTxt, _compassDistStyle);
        }

        private static void DrawCenteredBox(string text, GUIStyle style, Texture2D bg, float y, float minWFrac)
        {
            float sw = Screen.width;
            float sh = Screen.height;
            float fs = style.fontSize;
            if (fs < 8) fs = 24;
            string[] lines = text.Split('\n');
            int longest = 1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > longest) longest = lines[i].Length;
            }
            float w = Mathf.Max(sw * minWFrac, longest * fs * 0.85f + 96f);
            float h = lines.Length * fs * 1.45f + 48f;
            Rect r = new Rect((sw - w) * 0.5f, y, w, h);
            if ((object)bg != null) GUI.DrawTexture(r, bg);
            GUI.Label(r, text, style);
        }

        public static void Draw()
        {
            if (!LavaRising.Enabled
                && LavaRising.CurrentPhase != LavaRising.Phase.Won
                && LavaRising.CurrentPhase != LavaRising.Phase.Caught) return;
            BuildStyles();

            float sw = Screen.width;
            float sh = Screen.height;

            LavaRising.Phase phase = LavaRising.CurrentPhase;

            if (phase == LavaRising.Phase.Countdown || phase == LavaRising.Phase.Rising)
                DrawSummitRemaining(sw, sh);

            if (phase == LavaRising.Phase.Countdown)
            {
                int n = Mathf.CeilToInt(LavaRising.CountdownRemaining);
                if (n < 1) n = 1;
                int fs = Mathf.RoundToInt(sh * 0.18f);
                _countStyle.fontSize = fs;
                _goStyle.fontSize = fs;
                bool go = LavaRising.CountdownRemaining <= 0f;
                GUI.Label(new Rect(0, sh * 0.28f, sw, sh * 0.28f),
                    go ? "GO" : n.ToString(),
                    go ? _goStyle : _countStyle);
            }
            else if (phase == LavaRising.Phase.Rising)
            {
                if (LavaRising.LavaArmed)
                {
                    _flashStyle.fontSize = Mathf.RoundToInt(sh * 0.055f);
                    bool on = (Time.unscaledTime % 0.9f) < 0.5f;
                    if (on)
                        GUI.Label(new Rect(0f, 10f, sw, sh * 0.08f), "LAVA RISING", _flashStyle);

                    _infoStyle.fontSize = Mathf.RoundToInt(sh * 0.026f);
                    string line = LavaRising.FormatTime(LavaRising.ClimbTime)
                        + "   " + LavaRising.FormatMeters(LavaRising.CurrentMeters)
                        + "   " + LavaRising.DifficultyName
                        + "   +" + LavaRising.RiseRate.ToString("F1") + " m/s";
                    DrawCenteredBox(line, _infoStyle, _darkTex, sh * 0.09f, 0.42f);
                }
                else
                {
                    _infoStyle.fontSize = Mathf.RoundToInt(sh * 0.028f);
                    string line = "CLIMB  "
                        + LavaRising.FormatMeters(LavaRising.CurrentMeters)
                        + "  /  "
                        + LavaRising.FormatMeters(LavaRising.ArmClimbHeight())
                        + "  TO START";
                    DrawCenteredBox(line, _infoStyle, _darkTex, 18f, 0.48f);
                }
            }
            else if (phase == LavaRising.Phase.Caught)
            {
                _caughtStyle.fontSize = Mathf.RoundToInt(sh * 0.07f);
                DrawCenteredBox("CAUGHT", _caughtStyle, _caughtTex, sh * 0.32f, 0.36f);
            }
            else if (phase == LavaRising.Phase.Won)
            {
                _winStyle.fontSize = Mathf.RoundToInt(sh * 0.055f);
                DrawCenteredBox(
                    "SUMMIT\n" + LavaRising.FormatTime(LavaRising.LastWinTime)
                    + "   " + LavaRising.FormatMeters(LavaRising.CurrentMeters),
                    _winStyle, _winTex, sh * 0.28f, 0.48f);
            }
        }
    }
}
