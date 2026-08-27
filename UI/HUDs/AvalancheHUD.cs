using UnityEngine;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    public static class AvalancheHUD
    {
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

        public static void Draw()
        {
            if (!AvalancheMode.Enabled || !AvalancheMode.ShowTimer) return;

            float sw = Screen.width;
            float sh = Screen.height;

            int m = (int)(AvalancheMode.SurvivalTime / 60f);
            int s = (int)(AvalancheMode.SurvivalTime % 60f);
            string line = "AVALANCHE  "
                + m.ToString("D2") + ":" + s.ToString("D2")
                + "   Rocks: " + AvalancheMode.ActiveCount;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(12, Mathf.RoundToInt(sh * 0.018f)),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            Vector2 size = style.CalcSize(new GUIContent(line));
            float padX = 14f;
            float padY = 8f;
            float w = size.x + padX * 2f;
            float h = size.y + padY * 2f;
            float x = sw * 0.5f - w * 0.5f;
            float y = sh * 0.03f;

            GUI.color = new Color(0.05f, 0.07f, 0.1f, 0.72f);
            GUI.DrawTexture(new Rect(x, y, w, h), Tex);
            GUI.color = new Color(0.55f, 0.75f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, 3f, h), Tex);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, w, h), line, style);
        }
    }
}
