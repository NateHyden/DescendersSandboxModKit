using System.Collections.Generic;
using UnityEngine;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    // On-screen toast for ModChat messages. Each toast lasts DisplaySeconds,
    // stacks from the top-centre. Toggle from the Chat page (default ON).
    public static class ChatHUD
    {
        public static bool Enabled { get; private set; } = true;

        private const float DisplaySeconds = 5f;
        private const int MaxVisible = 5;
        private const float BaseRes = 1080f;

        private static readonly Color BgCol = new Color(0.055f, 0.063f, 0.078f, 0.88f);
        private static readonly Color BorderCol = new Color(0.25f, 0.55f, 1f, 0.35f);
        private static readonly Color AccentSelf = new Color(0.25f, 0.65f, 1f, 1f);
        private static readonly Color AccentOther = new Color(1f, 0.55f, 0.15f, 1f);
        private static readonly Color NameCol = new Color(1f, 1f, 1f, 0.92f);
        private static readonly Color TextCol = new Color(1f, 1f, 1f, 0.82f);
        private static readonly Color IconCol = new Color(0.68f, 1f, 0.18f, 1f);

        private class Toast
        {
            public string PlayerName;
            public string Text;
            public bool IsSelf;
            public float ExpireAt;
        }

        private static readonly List<Toast> _toasts = new List<Toast>();
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

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("[ChatHUD] On-screen chat " + (Enabled ? "ON" : "OFF"));
            if (!Enabled) _toasts.Clear();
        }

        public static void Reset()
        {
            // Keep Enabled across scenes — only drop in-flight toasts.
            _toasts.Clear();
        }

        // Called from ModChat whenever a message is added (send or receive).
        public static void Notify(ModChat.ChatMessage msg)
        {
            if ((object)msg == null || !Enabled) return;

            _toasts.Add(new Toast
            {
                PlayerName = string.IsNullOrEmpty(msg.PlayerName) ? "Unknown" : msg.PlayerName,
                Text = string.IsNullOrEmpty(msg.Text) ? "" : msg.Text,
                IsSelf = msg.IsSelf,
                ExpireAt = Time.unscaledTime + DisplaySeconds
            });

            while (_toasts.Count > MaxVisible)
                _toasts.RemoveAt(0);
        }

        public static void Draw()
        {
            if (!Enabled) return;

            float now = Time.unscaledTime;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                if (now >= _toasts[i].ExpireAt)
                    _toasts.RemoveAt(i);
            }
            if (_toasts.Count == 0) return;

            float s = Screen.height / BaseRes;
            float panelW = 420f * s;
            float rowH = 36f * s;
            float gap = 6f * s;
            float pad = 10f * s;
            float iconW = 28f * s;
            float marginY = 48f * s;
            float x = (Screen.width - panelW) * 0.5f;
            float y = marginY;

            int iconFs = Mathf.Max(12, Mathf.RoundToInt(16f * s));
            int nameFs = Mathf.Max(11, Mathf.RoundToInt(13f * s));
            int msgFs = Mathf.Max(10, Mathf.RoundToInt(12f * s));

            for (int i = 0; i < _toasts.Count; i++)
            {
                Toast t = _toasts[i];
                float remaining = t.ExpireAt - now;
                float alpha = remaining < 0.75f ? Mathf.Clamp01(remaining / 0.75f) : 1f;

                Color bg = BgCol; bg.a *= alpha;
                Color border = BorderCol; border.a *= alpha;
                Color accent = t.IsSelf ? AccentSelf : AccentOther; accent.a *= alpha;
                Color icon = IconCol; icon.a *= alpha;
                Color nameC = NameCol; nameC.a *= alpha;
                Color textC = TextCol; textC.a *= alpha;

                DrawRect(x, y, panelW, rowH, bg);
                float b = Mathf.Max(1f, s);
                DrawRect(x, y, panelW, b, border);
                DrawRect(x, y + rowH - b, panelW, b, border);
                DrawRect(x, y, b, rowH, border);
                DrawRect(x + panelW - b, y, b, rowH, border);
                DrawRect(x, y, Mathf.Max(2f, 3f * s), rowH, accent);

                // Message symbol (envelope) — left of the name.
                var iconStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = iconFs,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = icon }
                };
                GUI.Label(new Rect(x + pad, y, iconW, rowH), "\u2709", iconStyle);

                float textX = x + pad + iconW + 4f * s;
                float textW = panelW - (textX - x) - pad;

                var nameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = nameFs,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft,
                    clipping = TextClipping.Clip,
                    normal = { textColor = accent }
                };
                var msgStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = msgFs,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.UpperLeft,
                    clipping = TextClipping.Clip,
                    normal = { textColor = textC }
                };

                float half = rowH * 0.5f;
                GUI.Label(new Rect(textX, y + 2f * s, textW, half), t.PlayerName, nameStyle);
                GUI.Label(new Rect(textX, y + half - 2f * s, textW, half), t.Text, msgStyle);

                y += rowH + gap;
            }
        }

        private static void DrawRect(float rx, float ry, float rw, float rh, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(rx, ry, rw, rh), Tex);
            GUI.color = Color.white;
        }
    }
}
