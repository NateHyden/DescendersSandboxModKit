using UnityEngine;

namespace DescendersModMenu.UI
{
    public static class SurvivalHUD
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
            if (!Mods.SurvivalMode.Enabled) return;

            float sw = Screen.width;
            float sh = Screen.height;

            // ── Game Over overlay ─────────────────────────────────────
            if (Mods.SurvivalMode.IsGameOver)
            {
                // Dark background
                DrawRect(0, 0, sw, sh, new Color(0f, 0f, 0f, 0.75f));

                // "LS Click to Reset" at the top — large and clear
                var promptStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 26,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }
                };
                GUI.Label(new Rect(0, 24, sw, 48), "LS Click to Reset", promptStyle);

                // "GAME OVER"
                var goStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 72,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.95f, 0.15f, 0.15f, 1f) }
                };
                GUI.Label(new Rect(0, sh * 0.28f, sw, 100), "GAME OVER", goStyle);

                // Stats
                var statsStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                float statsY = sh * 0.5f;
                string timeStr = FormatTime(Mods.SurvivalMode.TimeAlive);
                GUI.Label(new Rect(0, statsY, sw, 36), "Time Survived:  " + timeStr, statsStyle);
                GUI.Label(new Rect(0, statsY + 42, sw, 36), "Bails Taken:    " + Mods.SurvivalMode.BailsTaken, statsStyle);
                GUI.Label(new Rect(0, statsY + 84, sw, 36), "Tricks Landed:  " + Mods.SurvivalMode.TricksLanded, statsStyle);

                return; // don't draw the health bar when game over
            }

            // ── Health bar ────────────────────────────────────────────
            float pct = Mods.SurvivalMode.HP / Mods.SurvivalMode.MaxHP;
            float barW = sw * 0.22f;
            float barH = 16f;
            float barX = 20f;
            float barY = sh - barH - 20f; // bottom-left

            // Bar background
            DrawRect(barX - 2, barY - 2, barW + 4, barH + 4, new Color(0f, 0f, 0f, 0.7f));

            // Bar fill — green → amber → red based on health
            Color barColor = pct > 0.6f
                ? Color.Lerp(new Color(1f, 0.7f, 0f), new Color(0.1f, 0.9f, 0.1f), (pct - 0.6f) / 0.4f)
                : Color.Lerp(new Color(0.9f, 0.1f, 0.1f), new Color(1f, 0.7f, 0f), pct / 0.6f);

            // Pulse alpha when critical
            float alpha = 1f;
            if (pct < 0.2f)
                alpha = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 5f));

            barColor.a = alpha;
            DrawRect(barX, barY, barW * pct, barH, barColor);

            // HP label
            var hpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            string hpText = Mathf.CeilToInt(Mods.SurvivalMode.HP) + " HP";
            GUI.Label(new Rect(barX, barY - 20, 100, 20), hpText, hpStyle);
        }

        private static void DrawRect(float x, float y, float w, float h, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(x, y, w, h), Tex);
            GUI.color = Color.white;
        }

        private static string FormatTime(float seconds)
        {
            int m = (int)(seconds / 60f);
            int s = (int)(seconds % 60f);
            return m.ToString("D2") + ":" + s.ToString("D2");
        }
    }
}