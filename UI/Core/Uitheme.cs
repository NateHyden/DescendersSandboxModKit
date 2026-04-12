using UnityEngine;

namespace DescendersModMenu.UI
{
    // ════════════════════════════════════════════════════════════════════
    //  UITheme — single file that owns every visual decision in the UI.
    //
    //  To retheme the entire menu, change values here only.
    //  UIHelpers references these tokens. Pages reference UIHelpers.
    //  Nothing else needs touching.
    //
    //  Colour notation: new Color(R, G, B, A) — all values 0.0–1.0
    // ════════════════════════════════════════════════════════════════════
    public static class UITheme
    {
        // ── Layout ───────────────────────────────────────────────────
        public const float WinW = 800f;
        public const float WinH = 660f;
        public const float SidebarW = 130f;
        public const float HeaderH = 58f;
        public const float RowH = 36f;
        public const float RowGap = 3f;
        public const float RowPad = 12f;
        public const float ContentPad = 4f;

        // Sprite corner radii (pixels on 64×64 source texture)
        public const int RadiusRow = 8;    // stat rows
        public const int RadiusBtn = 8;    // buttons
        public const int RadiusWin = 12;   // window corners
        public const int RadiusNav = 12;   // nav pill

        // ── Window backgrounds ───────────────────────────────────────
        // The window outer rim (seen through transparency at edges)
        public static readonly Color BgOuter = new Color(0.047f, 0.040f, 0.068f, 0.98f);
        // Main content area background
        public static readonly Color BgContent = new Color(0.075f, 0.062f, 0.108f, 1.00f);
        // Header strip background (slightly darker than content)
        public static readonly Color BgHeader = new Color(0.052f, 0.040f, 0.080f, 1.00f);
        // Left sidebar background
        public static readonly Color BgSidebar = new Color(0.052f, 0.040f, 0.080f, 1.00f);
        // Individual stat rows
        public static readonly Color BgRow = new Color(0.108f, 0.090f, 0.145f, 1.00f);

        // ── Borders ──────────────────────────────────────────────────
        public static readonly Color BorderWin = new Color(0.155f, 0.130f, 0.200f, 1.00f);
        public static readonly Color BorderRow = new Color(0.145f, 0.120f, 0.195f, 1.00f);

        // ── Accent colour (primary brand / active state) ─────────────
        // Change Accent + AccentDim + AccentBorder to re-colour everything at once.
        public static readonly Color Accent = new Color(0.749f, 0.373f, 1.000f, 1.00f); // #BF5FFF purple
        public static readonly Color AccentDim = new Color(0.135f, 0.095f, 0.175f, 1.00f); // pre-blended on BgContent
        public static readonly Color AccentBorder = new Color(0.228f, 0.135f, 0.295f, 1.00f); // pre-blended border

        // ── Secondary colour (Moon button, link text, chat self-msgs) ─
        // Kept separate so it can differ from Accent if desired.
        public static readonly Color Secondary = new Color(0.749f, 0.373f, 1.000f, 1.00f);
        public static readonly Color SecondaryDim = new Color(0.135f, 0.095f, 0.175f, 1.00f);
        public static readonly Color SecondaryBorder = new Color(0.228f, 0.135f, 0.295f, 1.00f);

        // ── Nav sidebar ──────────────────────────────────────────────
        public static readonly Color NavActiveBg = new Color(0.120f, 0.078f, 0.178f, 1.00f); // pill fill
        public static readonly Color NavActiveText = new Color(0.749f, 0.373f, 1.000f, 1.00f); // same as Accent
        public static readonly Color NavInactiveText = new Color(0.530f, 0.500f, 0.580f, 1.00f);
        public static readonly Color NavGlow = new Color(0.065f, 0.040f, 0.100f, 1.00f); // bar glow panel

        // ── Buttons — ACTION (-, +, ◀, ▶, small icon buttons) ───────
        // These are the small functional buttons. Neutral, not accent-coloured.
        public static readonly Color BtnActionBg = new Color(0.240f, 0.195f, 0.320f, 1.00f);  // clearly lighter than BgRow
        public static readonly Color BtnActionText = new Color(0.900f, 0.880f, 0.960f, 1.00f);  // bright on dark btn
        public static readonly Color BtnActionBorder = new Color(0.340f, 0.280f, 0.440f, 1.00f);  // visible border

        // ── Buttons — PRIMARY (Save/Load/Reset style) ────────────────
        // Larger labelled buttons. Slightly more prominent than action.
        public static readonly Color BtnPrimaryBg = new Color(0.240f, 0.195f, 0.320f, 1.00f);
        public static readonly Color BtnPrimaryText = new Color(0.900f, 0.880f, 0.960f, 1.00f);
        public static readonly Color BtnPrimaryBorder = new Color(0.340f, 0.280f, 0.440f, 1.00f);

        // ── Buttons — DESTRUCTIVE / WARNING (orange accent buttons) ──
        public static readonly Color BtnWarnBg = new Color(0.140f, 0.080f, 0.048f, 1.00f);
        public static readonly Color BtnWarnText = new Color(1.000f, 0.267f, 0.000f, 1.00f);
        public static readonly Color BtnWarnBorder = new Color(0.280f, 0.110f, 0.050f, 1.00f);

        // ── Text ─────────────────────────────────────────────────────
        public static readonly Color TextHeading = new Color(0.900f, 0.880f, 0.950f, 1.00f); // row labels, bold text
        public static readonly Color TextBody = new Color(0.530f, 0.510f, 0.580f, 1.00f); // secondary labels
        public static readonly Color TextDim = new Color(0.320f, 0.305f, 0.360f, 1.00f); // hints, placeholders
        public static readonly Color TextOnBtn = new Color(0.000f, 0.000f, 0.000f, 1.00f); // text on accent-coloured surface

        // ── State indicators ─────────────────────────────────────────
        public static readonly Color StateOn = new Color(0.000f, 1.000f, 0.533f, 1.00f); // green ON
        public static readonly Color StateOnBg = new Color(0.063f, 0.145f, 0.098f, 1.00f); // green ON bg (row tint)
        public static readonly Color StateOnBdr = new Color(0.055f, 0.310f, 0.192f, 1.00f); // green ON border
        public static readonly Color StateOff = new Color(1.000f, 0.133f, 0.267f, 1.00f); // red OFF
        public static readonly Color StateOffBg = new Color(0.130f, 0.063f, 0.067f, 1.00f); // red OFF bg (unused but available)
        public static readonly Color StateOffBdr = new Color(0.255f, 0.075f, 0.098f, 1.00f); // red OFF border

        // ── Warning / orange ─────────────────────────────────────────
        public static readonly Color Warning = new Color(1.000f, 0.267f, 0.000f, 1.00f);
        public static readonly Color WarningDim = new Color(0.140f, 0.075f, 0.047f, 1.00f);
        public static readonly Color WarningBdr = new Color(0.280f, 0.110f, 0.050f, 1.00f);

        // ── Toggle switch ─────────────────────────────────────────────
        public static readonly Color ToggleTrackOn = new Color(0.152f, 0.102f, 0.215f, 1.00f);
        public static readonly Color ToggleTrackOff = new Color(0.135f, 0.110f, 0.178f, 1.00f);
        public static readonly Color ToggleKnobOn = new Color(0.749f, 0.373f, 1.000f, 1.00f); // matches Accent
        public static readonly Color ToggleKnobOff = new Color(0.310f, 0.295f, 0.360f, 1.00f);

        // ── Slider / bar ─────────────────────────────────────────────
        public static readonly Color SliderFill = new Color(0.749f, 0.373f, 1.000f, 1.00f); // matches Accent
        public static readonly Color SliderBg = new Color(0.135f, 0.110f, 0.178f, 1.00f);
    }
}