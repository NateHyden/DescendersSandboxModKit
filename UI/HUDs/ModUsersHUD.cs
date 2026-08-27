using DescendersModMenu.Mods;
using UnityEngine;

namespace DescendersModMenu.UI
{
    public static class ModUsersHUD
    {
        public static bool Enabled { get; private set; } = false;

        private const float BaseRes = 1080f;
        private const float BaseMarginX = 18f;
        private const float BaseMarginY = 18f;
        private const float BasePadX = 12f;
        private const float BasePadY = 8f;
        private const float BaseGap = 8f;

        private static readonly Color BgCol = new Color(0.055f, 0.063f, 0.078f, 0.88f);
        private static readonly Color BorderCol = new Color(0.25f, 0.55f, 1f, 0.35f);
        private static readonly Color AccentCol = new Color(0.25f, 0.65f, 1f, 1f);
        private static readonly Color LabelCol = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color ValueCol = new Color(0.25f, 0.65f, 1f, 1f);

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
            ModLog.Feedback("[ModUsersHUD] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void Draw()
        {
            if (!Enabled) return;

            float s = Screen.height / BaseRes;
            float marginX = BaseMarginX * s;
            float marginY = BaseMarginY * s;
            float padX = BasePadX * s;
            float padY = BasePadY * s;
            float gap = BaseGap * s;

            int labelFs = Mathf.Max(10, Mathf.RoundToInt(11f * s));
            int valueFs = Mathf.Max(12, Mathf.RoundToInt(14f * s));

            int count = 0;
            var users = ModDetection.ModUsers;
            if ((object)users != null) count = users.Count;

            string label = "SANDBOX USERS";
            string value = count.ToString();

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
                normal = { textColor = ValueCol }
            };

            float labelW = labelStyle.CalcSize(new GUIContent(label)).x;
            float valueW = valueStyle.CalcSize(new GUIContent(value)).x;
            float panelW = padX + labelW + gap + valueW + padX;
            float panelH = padY * 2f + Mathf.Max(labelFs, valueFs) + 4f * s;

            float x = Screen.width - panelW - marginX;
            float y = marginY;
            if (SessionHUD.Enabled && SessionHUD.LastDrawnHeight > 0f)
                y = marginY + SessionHUD.LastDrawnHeight + gap;

            DrawRect(x, y, panelW, panelH, BgCol);
            float b = Mathf.Max(1f, s);
            DrawRect(x, y, panelW, b, BorderCol);
            DrawRect(x, y + panelH - b, panelW, b, BorderCol);
            DrawRect(x, y, b, panelH, BorderCol);
            DrawRect(x + panelW - b, y, b, panelH, BorderCol);
            DrawRect(x, y, Mathf.Max(2f, 3f * s), panelH, AccentCol);

            float textX = x + padX + 2f * s;
            float textW = panelW - padX * 2f - 2f * s;
            GUI.Label(new Rect(textX, y, textW * 0.7f, panelH), label, labelStyle);
            GUI.Label(new Rect(textX, y, textW, panelH), value, valueStyle);
        }

        private static void DrawRect(float rx, float ry, float rw, float rh, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(rx, ry, rw, rh), Tex);
            GUI.color = Color.white;
        }
    }
}
