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
        private const float BasePadX = 8f;
        private const float BasePadY = 4f;
        private const float BaseGap = 5f;
        private const int RoundR = 10;

        private static readonly Color BgCol = new Color(0.055f, 0.063f, 0.078f, 0.88f);
        private static readonly Color BorderCol = new Color(0.25f, 0.55f, 1f, 0.45f);
        private static readonly Color LabelCol = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color ValueCol = new Color(0.25f, 0.65f, 1f, 1f);

        private static GUIStyle _labelStyle;
        private static GUIStyle _valueStyle;
        private static GUIStyle _roundStyle;
        private static int _styleLabelFs;
        private static int _styleValueFs;

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

            int labelFs = Mathf.Max(9, Mathf.RoundToInt(10f * s));
            int valueFs = Mathf.Max(11, Mathf.RoundToInt(13f * s));

            int count = 0;
            var users = ModDetection.ModUsers;
            if ((object)users != null) count = users.Count;

            string label = "SANDBOX USERS:";
            string value = count.ToString();

            EnsureStyles(labelFs, valueFs);

            float labelW = _labelStyle.CalcSize(new GUIContent(label)).x;
            float valueW = _valueStyle.CalcSize(new GUIContent(value)).x;
            float panelW = padX + labelW + gap + valueW + padX;
            float panelH = padY * 2f + Mathf.Max(labelFs, valueFs) + 2f * s;

            float x = Screen.width - panelW - marginX;
            float y = marginY;
            if (SessionHUD.Enabled && SessionHUD.LastDrawnHeight > 0f)
                y = marginY + SessionHUD.LastDrawnHeight + gap;

            float b = Mathf.Max(1f, s);
            GUI.color = BorderCol;
            GUI.Box(new Rect(x - b, y - b, panelW + b * 2f, panelH + b * 2f), GUIContent.none, _roundStyle);
            GUI.color = BgCol;
            GUI.Box(new Rect(x, y, panelW, panelH), GUIContent.none, _roundStyle);
            GUI.color = Color.white;

            float textY = y;
            GUI.Label(new Rect(x + padX, textY, labelW + 2f, panelH), label, _labelStyle);
            GUI.Label(new Rect(x + padX + labelW + gap, textY, valueW + 2f, panelH), value, _valueStyle);
        }

        private static void EnsureStyles(int labelFs, int valueFs)
        {
            if ((object)_roundStyle == null)
            {
                _roundStyle = new GUIStyle
                {
                    border = new RectOffset(RoundR, RoundR, RoundR, RoundR),
                    normal = { background = UIHelpers.RoundTex(64, 64, RoundR, Color.white) }
                };
            }
            if ((object)_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Overflow,
                    normal = { textColor = LabelCol }
                };
                _valueStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Overflow,
                    normal = { textColor = ValueCol }
                };
            }
            if (_styleLabelFs != labelFs) { _labelStyle.fontSize = labelFs; _styleLabelFs = labelFs; }
            if (_styleValueFs != valueFs) { _valueStyle.fontSize = valueFs; _styleValueFs = valueFs; }
        }
    }
}
