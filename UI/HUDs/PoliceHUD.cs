using UnityEngine;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    public static class PoliceHUD
    {
        private static bool _stylesBuilt = false;
        private static GUIStyle _lightStyle = null;
        private static GUIStyle _caughtStyle = null;
        private static GUIStyle _infoStyle = null;
        private static GUIStyle _promptStyle = null;
        private static GUIStyle _burstStyle = null;
        private static Texture2D _redTex = null;
        private static Texture2D _blueTex = null;
        private static Texture2D _darkTex = null;
        private static Texture2D _warnTex = null;

        private static float _hudFlash = 0f;
        private static bool _hudIsRed = true;
        private static float _caughtPulse = 0f;

        private static void BuildStyles()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            _redTex = MakeTex(new Color(0.85f, 0.04f, 0.04f, 0.90f));
            _blueTex = MakeTex(new Color(0.05f, 0.15f, 0.90f, 0.90f));
            _darkTex = MakeTex(new Color(0f, 0f, 0f, 0.60f));
            _warnTex = MakeTex(new Color(0.7f, 0.05f, 0.05f, 0.85f));

            _lightStyle = new GUIStyle();
            _lightStyle.alignment = TextAnchor.MiddleCenter;
            _lightStyle.fontStyle = FontStyle.Bold;
            _lightStyle.normal.textColor = Color.white;

            _caughtStyle = new GUIStyle();
            _caughtStyle.alignment = TextAnchor.MiddleCenter;
            _caughtStyle.fontStyle = FontStyle.Bold;
            _caughtStyle.normal.textColor = Color.white;

            _infoStyle = new GUIStyle();
            _infoStyle.alignment = TextAnchor.MiddleCenter;
            _infoStyle.fontStyle = FontStyle.Bold;
            _infoStyle.normal.textColor = Color.white;
            _infoStyle.normal.background = _darkTex;
            _infoStyle.padding = new RectOffset(12, 12, 6, 6);

            _promptStyle = new GUIStyle();
            _promptStyle.alignment = TextAnchor.MiddleCenter;
            _promptStyle.fontStyle = FontStyle.Bold;
            _promptStyle.normal.textColor = Color.white;
            _promptStyle.normal.background = _warnTex;
            _promptStyle.padding = new RectOffset(18, 18, 10, 10);

            _burstStyle = new GUIStyle();
            _burstStyle.alignment = TextAnchor.MiddleCenter;
            _burstStyle.fontStyle = FontStyle.Bold;
            _burstStyle.normal.textColor = new Color(1f, 0.8f, 0f, 1f);
            _burstStyle.normal.background = _darkTex;
            _burstStyle.padding = new RectOffset(10, 10, 4, 4);
        }

        private static Texture2D MakeTex(Color col)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }

        public static void Draw()
        {
            if (!PoliceChaseMode.Enabled) return;
            BuildStyles();

            float sw = Screen.width;
            float sh = Screen.height;
            float dt = Time.deltaTime;

            // ── Flash timer ────────────────────────────────────────────
            _hudFlash -= dt;
            if (_hudFlash <= 0f) { _hudFlash = 0.35f; _hudIsRed = !_hudIsRed; }

            // ── Police light panels — alternate ON/OFF, one at a time ──
            float panelW = sw * 0.13f;
            float panelH = sh * 0.11f;
            float margin = sw * 0.015f;
            float panelY = sh * 0.02f;

            // Only one panel visible at a time — they alternate
            if (_hudIsRed)
            {
                _lightStyle.fontSize = Mathf.RoundToInt(sh * 0.022f);
                _lightStyle.normal.background = _redTex;
                GUI.Label(new Rect(margin, panelY, panelW, panelH),
                    "POLICE\nCHASE", _lightStyle);
            }
            else
            {
                _lightStyle.fontSize = Mathf.RoundToInt(sh * 0.022f);
                _lightStyle.normal.background = _blueTex;
                GUI.Label(new Rect(sw - margin - panelW, panelY, panelW, panelH),
                    "POLICE\nCHASE", _lightStyle);
            }

            // ── Info strip — distance + caught count ───────────────────
            float dist = PoliceChaseMode.PursuerDistance;
            string distTxt = dist >= 0f ? dist.ToString("F0") + "m" : "?m";
            string stateTxt = PoliceChaseMode.IsBursting ? "  ⚡ BURSTING" : "";
            string infoTxt = PoliceChaseMode.DifficultyName
                + "   Caught: " + PoliceChaseMode.CaughtCount
                + "   Pursuer: " + distTxt + stateTxt;

            _infoStyle.fontSize = Mathf.RoundToInt(sh * 0.016f);
            GUIContent infoContent = new GUIContent(infoTxt);
            Vector2 infoSize = _infoStyle.CalcSize(infoContent);
            float infoY = panelY + panelH + sh * 0.008f;
            GUI.Label(new Rect(sw * 0.5f - infoSize.x * 0.5f, infoY,
                infoSize.x, infoSize.y), infoTxt, _infoStyle);

            // ── Countdown before chase starts ──────────────────────────
            if (PoliceChaseMode.IsCountingDown)
            {
                int secs = Mathf.CeilToInt(PoliceChaseMode.CountdownRemaining);
                _caughtStyle.fontSize = Mathf.RoundToInt(sh * 0.12f);
                _caughtStyle.normal.textColor = new Color(1f, 1f, 1f, 1f);
                float cW = sw * 0.4f;
                float cH = sh * 0.25f;
                GUI.Label(new Rect(sw * 0.5f - cW * 0.5f, sh * 0.35f, cW, cH),
                    secs.ToString(), _caughtStyle);

                _infoStyle.fontSize = Mathf.RoundToInt(sh * 0.022f);
                string readyTxt = "Get ready!";
                GUIContent rc = new GUIContent(readyTxt);
                Vector2 rs = _infoStyle.CalcSize(rc);
                GUI.Label(new Rect(sw * 0.5f - rs.x * 0.5f, sh * 0.35f + sh * 0.15f,
                    rs.x, rs.y), readyTxt, _infoStyle);
                return; // don't draw other HUD elements during countdown
            }

            // ── CAUGHT flash ───────────────────────────────────────────
            if (PoliceChaseMode.IsCaught)
            {
                _caughtPulse += dt * 6f;
                float alpha = Mathf.Abs(Mathf.Sin(_caughtPulse));
                _caughtStyle.normal.textColor = new Color(1f, 0.1f, 0.1f, alpha);
                _caughtStyle.fontSize = Mathf.RoundToInt(sh * 0.09f);
                float cW = sw * 0.6f;
                float cH = sh * 0.2f;
                GUI.Label(new Rect(sw * 0.5f - cW * 0.5f, sh * 0.38f, cW, cH),
                    "CAUGHT!", _caughtStyle);
            }
            else
            {
                _caughtPulse = 0f;
            }

            // ── F5 reset prompt — shown when waiting for manual reset ──
            if (PoliceChaseMode.WaitingForReset)
            {
                _promptStyle.fontSize = Mathf.RoundToInt(sh * 0.022f);
                string promptTxt = "Press F5 to reset pursuer";
                GUIContent pc = new GUIContent(promptTxt);
                Vector2 ps = _promptStyle.CalcSize(pc);
                // Sits just below the CAUGHT text / centre screen
                float pY = sh * 0.6f;
                GUI.Label(new Rect(sw * 0.5f - ps.x * 0.5f, pY, ps.x, ps.y),
                    promptTxt, _promptStyle);
            }
            // ── Epilepsy warning — bottom of screen ────────────────────
            if ((object)_infoStyle != null)
            {
                _infoStyle.fontSize = Mathf.RoundToInt(sh * 0.013f);
                string warnTxt = "⚠ Flashing Lights";
                GUIContent wc = new GUIContent(warnTxt);
                Vector2 ws = _infoStyle.CalcSize(wc);
                GUI.Label(new Rect(sw * 0.5f - ws.x * 0.5f,
                    sh - ws.y - sh * 0.015f, ws.x, ws.y),
                    warnTxt, _infoStyle);
            }
        }
    }
}