using UnityEngine;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    public static class EarthquakeHUD
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
            if (!EarthquakeMode.Enabled) return;

            float sw = Screen.width;
            float sh = Screen.height;

            string line;
            Color accent;
            if (EarthquakeMode.FrequencyMode == 2)
            {
                line = "EARTHQUAKE  CONSTANT";
                accent = new Color(1f, 0.55f, 0.15f, 1f);
            }
            else if (EarthquakeMode.IsQuaking)
            {
                float rem = EarthquakeMode.QuakeRemaining;
                line = rem >= 0f
                    ? "QUAKE  " + rem.ToString("F1") + "s"
                    : "QUAKE";
                accent = new Color(1f, 0.25f, 0.1f, 1f);
            }
            else if (EarthquakeMode.IsForeshadowing)
            {
                float next = EarthquakeMode.NextQuakeIn;
                float pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 8f));
                line = "TREMOR  " + next.ToString("F1") + "s";
                accent = new Color(1f, 0.75f, 0.2f, pulse);
            }
            else
            {
                float next = EarthquakeMode.NextQuakeIn;
                line = "NEXT QUAKE  " + next.ToString("F0") + "s";
                accent = new Color(0.7f, 0.75f, 0.85f, 1f);
            }

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
            if (AvalancheMode.Enabled && AvalancheMode.ShowTimer)
                y = sh * 0.08f;

            GUI.color = new Color(0.05f, 0.07f, 0.1f, 0.72f);
            GUI.DrawTexture(new Rect(x, y, w, h), Tex);
            GUI.color = accent;
            GUI.DrawTexture(new Rect(x, y, 3f, h), Tex);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, w, h), line, style);
        }
    }
}
