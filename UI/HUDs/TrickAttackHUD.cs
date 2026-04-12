using UnityEngine;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    public static class TrickAttackHUD
    {
        private static bool _built = false;
        private static GUIStyle _timerStyle = null;
        private static GUIStyle _scoreStyle = null;
        private static GUIStyle _targetStyle = null;
        private static GUIStyle _resultStyle = null;
        private static GUIStyle _hintStyle = null;
        private static Texture2D _darkTex = null;
        private static Texture2D _greenTex = null;
        private static Texture2D _redTex = null;

        private static void Build()
        {
            if (_built) return;
            _built = true;

            _darkTex = MakeTex(new Color(0f, 0f, 0f, 0.65f));
            _greenTex = MakeTex(new Color(0.05f, 0.7f, 0.05f, 0.88f));
            _redTex = MakeTex(new Color(0.8f, 0.05f, 0.05f, 0.88f));

            _timerStyle = new GUIStyle();
            _timerStyle.fontStyle = FontStyle.Bold;
            _timerStyle.alignment = TextAnchor.MiddleCenter;
            _timerStyle.normal.textColor = Color.white;
            _timerStyle.normal.background = _darkTex;
            _timerStyle.padding = new RectOffset(14, 14, 8, 8);

            _scoreStyle = new GUIStyle(_timerStyle);
            _targetStyle = new GUIStyle(_timerStyle);

            _resultStyle = new GUIStyle();
            _resultStyle.fontStyle = FontStyle.Bold;
            _resultStyle.alignment = TextAnchor.MiddleCenter;
            _resultStyle.normal.textColor = Color.white;
            _resultStyle.padding = new RectOffset(30, 30, 14, 14);

            _hintStyle = new GUIStyle(_timerStyle);
            _hintStyle.fontStyle = FontStyle.Normal;
            _hintStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        }

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        public static void Draw()
        {
            var state = TrickAttackMode.CurrentState;
            if (state == TrickAttackMode.State.Off) return;
            Build();

            float sw = Screen.width;
            float sh = Screen.height;
            float panelH = sh * 0.07f;
            float panelY = sh * 0.02f;
            float panelW = sw * 0.14f;

            // ── Top-left: timer ───────────────────────────────────────
            int secs = Mathf.CeilToInt(TrickAttackMode.TimeRemaining);
            string timerTxt = state == TrickAttackMode.State.Running
                ? FormatTime(TrickAttackMode.TimeRemaining)
                : "—:——";

            // Timer turns red in last 10 seconds
            bool urgent = state == TrickAttackMode.State.Running && secs <= 10;
            _timerStyle.normal.textColor = urgent ? new Color(1f, 0.25f, 0.1f) : Color.white;
            _timerStyle.fontSize = Mathf.RoundToInt(sh * 0.028f);
            GUI.Label(new Rect(sw * 0.015f, panelY, panelW, panelH), timerTxt, _timerStyle);

            // ── Top-centre: score gained ──────────────────────────────
            string scoreTxt = TrickAttackMode.ScoreGained.ToString("N0");
            _scoreStyle.fontSize = Mathf.RoundToInt(sh * 0.028f);
            float scoreW = sw * 0.18f;
            _scoreStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(sw * 0.5f - scoreW * 0.5f, panelY, scoreW, panelH),
                scoreTxt, _scoreStyle);

            // ── Top-right: target score ───────────────────────────────
            string targetTxt = TrickAttackMode.TargetScore.ToString("N0");
            _targetStyle.fontSize = Mathf.RoundToInt(sh * 0.022f);
            _targetStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            GUI.Label(new Rect(sw - sw * 0.015f - panelW, panelY, panelW, panelH),
                targetTxt, _targetStyle);

            // ── Waiting hint ──────────────────────────────────────────
            if (state == TrickAttackMode.State.WaitingForStart)
            {
                _hintStyle.fontSize = Mathf.RoundToInt(sh * 0.018f);
                string hint = "Ride to your spot, then\nclick LEFT STICK to start";
                GUIContent hc = new GUIContent(hint);
                float hintW = sw * 0.25f;
                float hintH = _hintStyle.CalcHeight(hc, hintW);
                GUI.Label(new Rect(sw * 0.5f - hintW * 0.5f, panelY + panelH + sh * 0.01f,
                    hintW, hintH), hint, _hintStyle);
            }

            // ── SUCCESS / FAIL result ─────────────────────────────────
            if (state == TrickAttackMode.State.Success || state == TrickAttackMode.State.Fail)
            {
                bool success = state == TrickAttackMode.State.Success;
                string txt = success ? "SUCCESS!" : "FAIL";
                string sub = success
                    ? TrickAttackMode.ScoreGained.ToString("N0") + " points scored"
                    : TrickAttackMode.ScoreGained.ToString("N0") + " / "
                      + TrickAttackMode.TargetScore.ToString("N0") + " points";

                _resultStyle.normal.background = success ? _greenTex : _redTex;
                _resultStyle.fontSize = Mathf.RoundToInt(sh * 0.075f);
                float rW = sw * 0.5f;
                float rH = sh * 0.18f;
                GUI.Label(new Rect(sw * 0.5f - rW * 0.5f, sh * 0.35f, rW, rH),
                    txt, _resultStyle);

                _resultStyle.fontSize = Mathf.RoundToInt(sh * 0.024f);
                float sW = sw * 0.4f;
                float sH = sh * 0.07f;
                GUI.Label(new Rect(sw * 0.5f - sW * 0.5f, sh * 0.35f + rH + sh * 0.01f,
                    sW, sH), sub, _resultStyle);
            }
        }

        private static string FormatTime(float t)
        {
            int m = (int)(t / 60f);
            int s = (int)(t % 60f);
            return m + ":" + s.ToString("D2");
        }
    }
}