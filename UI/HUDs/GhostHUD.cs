using UnityEngine;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    /// <summary>
    /// Resolution-independent on-screen overlay for Ghost Replay.
    /// Uses Unity's immediate-mode GUI — always correct regardless of resolution.
    /// Call GhostHUD.Draw() from your MelonMod.OnGUI() override.
    /// </summary>
    public static class GhostHUD
    {
        // ── Styles — built once ───────────────────────────────────────
        private static GUIStyle _stateStyle   = null;
        private static GUIStyle _infoStyle    = null;
        private static GUIStyle _hintStyle    = null;
        private static Texture2D _bgTex       = null;
        private static bool _stylesBuilt      = false;

        private static void BuildStyles()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            // Background pill
            _bgTex = new Texture2D(1, 1);
            _bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
            _bgTex.Apply();

            // State text — large, centred, bold
            _stateStyle = new GUIStyle();
            _stateStyle.fontSize  = Mathf.RoundToInt(Screen.height * 0.026f); // ~2.6% of height
            _stateStyle.fontStyle = FontStyle.Bold;
            _stateStyle.alignment = TextAnchor.MiddleCenter;
            _stateStyle.normal.textColor = Color.white;
            _stateStyle.normal.background = _bgTex;
            _stateStyle.padding = new RectOffset(14, 14, 6, 6);

            // Info text — smaller, centred
            _infoStyle = new GUIStyle();
            _infoStyle.fontSize  = Mathf.RoundToInt(Screen.height * 0.018f);
            _infoStyle.fontStyle = FontStyle.Normal;
            _infoStyle.alignment = TextAnchor.MiddleCenter;
            _infoStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            _infoStyle.normal.background = _bgTex;
            _infoStyle.padding = new RectOffset(14, 14, 4, 4);

            // Hint text — small, left-aligned, top-left instructions
            _hintStyle = new GUIStyle();
            _hintStyle.fontSize  = Mathf.RoundToInt(Screen.height * 0.015f);
            _hintStyle.fontStyle = FontStyle.Normal;
            _hintStyle.alignment = TextAnchor.UpperLeft;
            _hintStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            _hintStyle.normal.background = _bgTex;
            _hintStyle.padding = new RectOffset(10, 10, 8, 8);
            _hintStyle.wordWrap = true;
        }

        // ── Main draw call ────────────────────────────────────────────
        public static void Draw()
        {
            if (!GhostReplay.Enabled) return;
            BuildStyles();

            float sw = Screen.width;
            float sh = Screen.height;

            // ── TOP CENTRE — current state ────────────────────────────
            string stateText;
            Color  stateColor;
            GetStateDisplay(out stateText, out stateColor);

            _stateStyle.normal.textColor = stateColor;

            // Measure text width so pill is exactly the right size
            GUIContent stateContent = new GUIContent(stateText);
            Vector2 stateSize = _stateStyle.CalcSize(stateContent);

            float centreX  = sw * 0.5f;
            float stateW   = Mathf.Max(stateSize.x, sw * 0.18f);
            float stateH   = stateSize.y;
            float stateY   = sh * 0.04f;

            Rect stateRect = new Rect(centreX - stateW * 0.5f, stateY, stateW, stateH);
            GUI.Label(stateRect, stateText, _stateStyle);

            // ── BELOW STATE — sub-info line ───────────────────────────
            string subText = GetSubInfo();
            if (!string.IsNullOrEmpty(subText))
            {
                GUIContent subContent = new GUIContent(subText);
                Vector2 subSize = _infoStyle.CalcSize(subContent);
                float subW = Mathf.Max(subSize.x, stateW);
                Rect subRect = new Rect(centreX - subW * 0.5f,
                    stateY + stateH + sh * 0.005f, subW, subSize.y);
                GUI.Label(subRect, subText, _infoStyle);
            }

            // ── TOP LEFT — contextual instructions ────────────────────
            string hintText = GetHintText();
            if (!string.IsNullOrEmpty(hintText))
            {
                float hintX = sw * 0.015f;
                float hintY = sh * 0.04f;
                float hintW = sw * 0.22f;
                GUIContent hintContent = new GUIContent(hintText);
                float hintH = _hintStyle.CalcHeight(hintContent, hintW);
                GUI.Label(new Rect(hintX, hintY, hintW, hintH), hintText, _hintStyle);
            }
        }

        // ── State display ─────────────────────────────────────────────
        private static void GetStateDisplay(out string text, out Color col)
        {
            if (!GhostReplay.Enabled) { text = ""; col = Color.white; return; }

            switch (GhostReplay.GetStateLabel())
            {
                case "STEP 1: RIDE TO START":
                    text = "GHOST REPLAY — PRESS LS TO SET START";
                    col  = new Color(0f, 0.8f, 1f);
                    break;
                case "STEP 2: WAITING FOR MOVE":
                    text = "START RIDING TO BEGIN RECORDING";
                    col  = new Color(0.8f, 0.8f, 0f);
                    break;
                case "RECORDING":
                    text = "● REC";
                    col  = new Color(1f, 0.25f, 0.1f);
                    break;
                default:
                    text = "GHOST REPLAY";
                    col  = Color.white;
                    break;
            }
        }

        // ── Sub-info below state ──────────────────────────────────────
        private static string GetSubInfo()
        {
            if (GhostReplay.GetStateLabel() == "RECORDING")
            {
                string frames = GhostReplay.RecordedFrames.ToString();
                string time   = FormatTime(GhostReplay.RunTime);
                string saved  = GhostReplay.HasSavedRun
                    ? "  |  Ghost: " + FormatTime(GhostReplay.SavedRunTime)
                    : "  |  No ghost saved — RS click to save";
                return time + "  (" + frames + " frames)" + saved;
            }
            if (GhostReplay.GetStateLabel() == "STEP 2: WAITING FOR MOVE" && GhostReplay.HasSavedRun)
                return "Ghost ready  |  Ghost run: " + FormatTime(GhostReplay.SavedRunTime);
            return "";
        }

        // ── Hint text top-left ────────────────────────────────────────
        private static string GetHintText()
        {
            switch (GhostReplay.GetStateLabel())
            {
                case "STEP 1: RIDE TO START":
                    return "Ride to your start point\nthen click LEFT STICK";
                case "STEP 2: WAITING FOR MOVE":
                    return "Push forward to\nstart recording";
                case "RECORDING":
                    return GhostReplay.HasSavedRun
                        ? "RS click  →  Save as ghost\nB  →  Reset & replay ghost"
                        : "RS click  →  Save run as ghost\n(first reset auto-saves too)";
                default:
                    return "";
            }
        }

        private static string FormatTime(float t)
        {
            int m  = (int)(t / 60f);
            int s  = (int)(t % 60f);
            int ms = (int)((t % 1f) * 10f);
            return m + ":" + s.ToString("D2") + "." + ms;
        }
    }
}
