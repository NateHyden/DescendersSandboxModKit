using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class WheelieHUD
    {
        public static bool Enabled { get; private set; } = false;

        // ── Layout constants (base 1080p) ─────────────────────────────
        private const float PanelW = 160f;
        private const float PanelH = 160f;
        private const float MarginRight = 30f;
        private const float MarginTop = 30f;
        private const float Padding = 10f;
        private const float HeaderH = 14f;
        private const float ReadoutH = 16f;

        // BrakeFade conflict: if BrakeFade HUD is enabled, sit below it.
        // BrakeFade panel is 76px tall + we want a 12px gap.
        private const float BrakeFadeStackH = 76f + 12f;

        // ── ViewBox (matches the v9 mockup) ───────────────────────────
        private const float VB_W = 200f;
        private const float VB_H = 160f;

        // Bike pivot (rear wheel centre)
        private const float PivotVBx = 95f;
        private const float PivotVBy = 115f;

        // Arc parameters — quarter circle, centre at pivot, radius 85
        // Spans 0° (right horizontal) → 90° (straight up) measured CCW
        private const float ArcRadius = 85f;
        private const int ArcSegments = 24;

        // ── Colours ───────────────────────────────────────────────────
        private static readonly Color PanelBg = new Color(14f / 255f, 17f / 255f, 21f / 255f, 0.92f);
        private static readonly Color BorderDim = new Color(42f / 255f, 47f / 255f, 54f / 255f, 1f);
        private static readonly Color BorderRed = new Color(1f, 0.133f, 0.267f, 1f);
        private static readonly Color ArcTrackC = new Color(31f / 255f, 35f / 255f, 41f / 255f, 1f);
        private static readonly Color HeaderLime = new Color(0.8f, 1f, 0f, 1f);
        private static readonly Color HeaderRed = new Color(1f, 0.133f, 0.267f, 1f);
        private static readonly Color GreenZone = new Color(0f, 1f, 0.498f, 1f);
        private static readonly Color YellowZone = new Color(1f, 0.831f, 0f, 1f);
        private static readonly Color RedZone = new Color(1f, 0.133f, 0.267f, 1f);

        // ── Cached resources ──────────────────────────────────────────
        private static Texture2D _whiteTex = null;
        private static Texture2D _ringTex = null;
        private static GUIStyle _headerStyle = null;
        private static GUIStyle _readoutStyle = null;
        private static Vector2[] _arcPointsVB = null;

        private static Texture2D WhiteTex
        {
            get
            {
                if ((object)_whiteTex == null)
                {
                    _whiteTex = new Texture2D(1, 1);
                    _whiteTex.SetPixel(0, 0, Color.white);
                    _whiteTex.Apply();
                }
                return _whiteTex;
            }
        }

        // Anti-aliased ring (outlined circle) used for the wheels
        private static Texture2D RingTex
        {
            get
            {
                if ((object)_ringTex == null)
                {
                    int sz = 64;
                    _ringTex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                    _ringTex.wrapMode = TextureWrapMode.Clamp;
                    float r = sz / 2f - 1f;
                    float thick = 5f;
                    Vector2 c = new Vector2(sz / 2f, sz / 2f);
                    for (int y = 0; y < sz; y++)
                    {
                        for (int x = 0; x < sz; x++)
                        {
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                            float a;
                            if (d > r) a = Mathf.Clamp01(1f - (d - r));
                            else if (d > r - thick) a = 1f;
                            else if (d > r - thick - 1f) a = Mathf.Clamp01(1f - ((r - thick) - d));
                            else a = 0f;
                            _ringTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                        }
                    }
                    _ringTex.Apply();
                }
                return _ringTex;
            }
        }

        // ─────────────────────────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            MelonLogger.Msg("[WheelieHUD] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void Reset()
        {
            Enabled = false;
        }

        // ─────────────────────────────────────────────────────────────
        public static void OnGUI()
        {
            if (!Enabled) return;

            float s = Screen.height / 1080f;

            // Read pitch from WheelieAngleLimit_Patch (always populated by the Harmony postfix)
            float currentPitch = WheelieAngleLimit_Patch.CurrentPitch;
            if (currentPitch < 0f) currentPitch = 0f;     // only show nose-up

            float limit = WheelieAngleLimit.AngleLimit;
            float fillRatio = limit > 0f ? Mathf.Clamp01(currentPitch / limit) : 0f;

            // Zone colour
            Color zoneColor;
            if (fillRatio < 0.6f) zoneColor = GreenZone;
            else if (fillRatio < 0.9f) zoneColor = YellowZone;
            else zoneColor = RedZone;
            bool critical = fillRatio >= 0.9f;

            // Pulsing alpha for critical border
            float pulse = 1f;
            if (critical) pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 8f);

            // ── Panel position ────────────────────────────────────────
            float pw = PanelW * s;
            float ph = PanelH * s;
            float px = Screen.width - pw - MarginRight * s;
            float py = MarginTop * s;
            // Stack below BrakeFade HUD if it's also visible (BrakeFade owns the top slot)
            if (BrakeFade.Enabled) py += BrakeFadeStackH * s;

            // ── Panel background + border ─────────────────────────────
            DrawRect(px, py, pw, ph, PanelBg);
            float b = Mathf.Max(1f, s);
            Color borderC = critical
                ? new Color(BorderRed.r, BorderRed.g, BorderRed.b, pulse)
                : BorderDim;
            DrawBorder(px, py, pw, ph, b, borderC);

            // ── Header text ───────────────────────────────────────────
            if (_headerStyle == null) _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(10f * s));
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.alignment = TextAnchor.MiddleCenter;
            _headerStyle.normal.textColor = critical ? HeaderRed : HeaderLime;
            GUI.Label(new Rect(px, py + Padding * s, pw, HeaderH * s), "WHEELIE", _headerStyle);

            // ── Drawing area for arc + bike ───────────────────────────
            float drawX = px + Padding * s;
            float drawY = py + (Padding + HeaderH + 2f) * s;
            float drawW = pw - 2f * Padding * s;
            float drawH = ph - (2f * Padding + HeaderH + ReadoutH + 4f) * s;

            // Uniform scale to fit the viewbox without distortion
            float scl = Mathf.Min(drawW / VB_W, drawH / VB_H);
            float effW = VB_W * scl;
            float effH = VB_H * scl;
            float offX = drawX + (drawW - effW) * 0.5f;
            float offY = drawY + (drawH - effH) * 0.5f;

            // ── Pre-compute arc points in viewbox space (lazy, once) ──
            if (_arcPointsVB == null)
            {
                _arcPointsVB = new Vector2[ArcSegments + 1];
                for (int i = 0; i <= ArcSegments; i++)
                {
                    float t = i / (float)ArcSegments;
                    float theta = t * 90f * Mathf.Deg2Rad;
                    _arcPointsVB[i] = new Vector2(
                        PivotVBx + ArcRadius * Mathf.Cos(theta),
                        PivotVBy - ArcRadius * Mathf.Sin(theta));
                }
            }

            // Convert to screen space for this frame
            Vector2[] arcScreen = new Vector2[ArcSegments + 1];
            for (int i = 0; i <= ArcSegments; i++)
                arcScreen[i] = VBToScreen(_arcPointsVB[i].x, _arcPointsVB[i].y, offX, offY, scl);

            // ── Arc backdrop ──────────────────────────────────────────
            float arcThick = 4f * s;
            for (int i = 1; i <= ArcSegments; i++)
                DrawLine(arcScreen[i - 1], arcScreen[i], arcThick, ArcTrackC);

            // ── Arc fill (proportional to currentPitch / limit) ───────
            int fillSegs = Mathf.RoundToInt(ArcSegments * fillRatio);
            for (int i = 1; i <= fillSegs; i++)
                DrawLine(arcScreen[i - 1], arcScreen[i], arcThick, zoneColor);

            // ── Bike (manually rotate each point to avoid nested GUI.matrix issues) ─
            Vector2 pivot = VBToScreen(PivotVBx, PivotVBy, offX, offY, scl);
            // SVG/OnGUI both use Y-down, so a CCW visual rotation = negative angle
            float rad = -currentPitch * Mathf.Deg2Rad;
            float cosA = Mathf.Cos(rad);
            float sinA = Mathf.Sin(rad);

            float t1 = Mathf.Max(1f, 2.5f * s);
            float t2 = Mathf.Max(1f, 2f * s);
            float t3 = Mathf.Max(1f, 1.5f * s);

            // Pre-rotate every bike vertex once so all lines stay connected
            Vector2 pRearAxle = RotateAround(95, 115, pivot, cosA, sinA, offX, offY, scl);
            Vector2 pBB = RotateAround(114, 115, pivot, cosA, sinA, offX, offY, scl);
            Vector2 pSeatTop = RotateAround(109, 92, pivot, cosA, sinA, offX, offY, scl);
            Vector2 pHeadTop = RotateAround(139, 92, pivot, cosA, sinA, offX, offY, scl);
            Vector2 pFrontAxle = RotateAround(150, 115, pivot, cosA, sinA, offX, offY, scl);
            Vector2 pSadL = RotateAround(102, 89, pivot, cosA, sinA, offX, offY, scl);
            Vector2 pSadR = RotateAround(114, 89, pivot, cosA, sinA, offX, offY, scl);
            Vector2 pPostTop = RotateAround(108, 89, pivot, cosA, sinA, offX, offY, scl);
            Vector2 pStemTop = RotateAround(135, 84, pivot, cosA, sinA, offX, offY, scl);
            Vector2 pGripL = RotateAround(129, 84, pivot, cosA, sinA, offX, offY, scl);
            Vector2 pGripR = RotateAround(141, 84, pivot, cosA, sinA, offX, offY, scl);

            // Frame tubes
            DrawLine(pRearAxle, pBB, t1, zoneColor); // chainstay
            DrawLine(pBB, pSeatTop, t1, zoneColor); // seat tube
            DrawLine(pBB, pHeadTop, t1, zoneColor); // down tube
            DrawLine(pSeatTop, pHeadTop, t1, zoneColor); // top tube
            DrawLine(pSeatTop, pRearAxle, t1, zoneColor); // seat stay
            DrawLine(pHeadTop, pFrontAxle, t1, zoneColor); // fork
            // Saddle
            DrawLine(pSadL, pSadR, t1, zoneColor); // saddle bar
            DrawLine(pPostTop, pSeatTop, t3, zoneColor); // seat post
            // Handlebar
            DrawLine(pHeadTop, pStemTop, t2, zoneColor); // stem
            DrawLine(pGripL, pGripR, t1, zoneColor); // grips
            // Wheels (circles — orientation-invariant, just need rotated centres)
            DrawCircle(pRearAxle, 11f * scl, zoneColor);
            DrawCircle(pFrontAxle, 11f * scl, zoneColor);

            // ── Numeric readout ───────────────────────────────────────
            if (_readoutStyle == null) _readoutStyle = new GUIStyle(GUI.skin.label);
            _readoutStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(13f * s));
            _readoutStyle.fontStyle = FontStyle.Bold;
            _readoutStyle.alignment = TextAnchor.MiddleCenter;
            _readoutStyle.normal.textColor = zoneColor;

            float readoutY = py + ph - (Padding + ReadoutH) * s;
            string readoutText = Mathf.RoundToInt(currentPitch) + "\u00b0";
            GUI.Label(new Rect(px, readoutY, pw, ReadoutH * s), readoutText, _readoutStyle);
        }

        // ── Helpers ───────────────────────────────────────────────────
        private static Vector2 VBToScreen(float vbx, float vby, float offX, float offY, float scl)
        {
            return new Vector2(offX + vbx * scl, offY + vby * scl);
        }

        // Rotate a viewbox-space point around the bike pivot (in screen space) and return
        // the resulting screen coordinate. Used to pre-compute every bike vertex per frame
        // so all the line segments stay connected when the bike pitches up.
        private static Vector2 RotateAround(float vbx, float vby, Vector2 pivot,
            float cosA, float sinA, float offX, float offY, float scl)
        {
            float rx = offX + vbx * scl;
            float ry = offY + vby * scl;
            float dx = rx - pivot.x;
            float dy = ry - pivot.y;
            return new Vector2(
                pivot.x + dx * cosA - dy * sinA,
                pivot.y + dx * sinA + dy * cosA);
        }

        private static void DrawLine(Vector2 p1, Vector2 p2, float thickness, Color color)
        {
            Vector2 delta = p2 - p1;
            float length = delta.magnitude;
            if (length < 0.001f) return;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, p1);
            GUI.color = color;
            GUI.DrawTexture(new Rect(p1.x, p1.y - thickness * 0.5f, length, thickness), WhiteTex);
            GUI.color = Color.white;
            GUI.matrix = saved;
        }

        private static void DrawCircle(Vector2 center, float radius, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), RingTex);
            GUI.color = Color.white;
        }

        private static void DrawRect(float rx, float ry, float rw, float rh, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(rx, ry, rw, rh), WhiteTex);
            GUI.color = Color.white;
        }

        private static void DrawBorder(float rx, float ry, float rw, float rh, float b, Color c)
        {
            DrawRect(rx, ry, rw, b, c);
            DrawRect(rx, ry + rh - b, rw, b, c);
            DrawRect(rx, ry, b, rh, c);
            DrawRect(rx + rw - b, ry, b, rh, c);
        }
    }
}