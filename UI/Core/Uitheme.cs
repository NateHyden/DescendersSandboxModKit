using UnityEngine;

namespace DescendersModMenu.UI
{
    // ════════════════════════════════════════════════════════════════════
    // ════════════════════════════════════════════════════════════════════
    public static class UITheme
    {
        // ── Layout ───────────────────────────────────────────────────
        public const float WinW = 880f;
        public const float WinH = 660f;
        public const float SidebarW = 130f;
        public const float HeaderH = 84f;
        public const float RowH = 36f;
        public const float RowGap = 3f;
        public const float RowPad = 12f;
        public const float ContentPad = 4f;

        public const int RadiusRow = 8;
        public const int RadiusBtn = 8;
        public const int RadiusWin = 12;
        public const int RadiusNav = 12;

        // ── Window backgrounds ───────────────────────────────────────
        public static Color BgOuter = new Color(0.047f, 0.040f, 0.068f, 0.98f);
        public static Color BgContent = new Color(0.075f, 0.062f, 0.108f, 1.00f);
        public static Color BgHeader = new Color(0.052f, 0.040f, 0.080f, 1.00f);
        public static Color BgSidebar = new Color(0.052f, 0.040f, 0.080f, 1.00f);
        public static Color BgRow = new Color(0.108f, 0.090f, 0.145f, 1.00f);

        // ── Borders ──────────────────────────────────────────────────
        public static Color BorderWin = new Color(0.155f, 0.130f, 0.200f, 1.00f);
        public static Color BorderRow = new Color(0.145f, 0.120f, 0.195f, 1.00f);

        public static Color Accent = new Color(0.749f, 0.373f, 1.000f, 1.00f);
        public static Color AccentDim = new Color(0.135f, 0.095f, 0.175f, 1.00f);
        public static Color AccentBorder = new Color(0.228f, 0.135f, 0.295f, 1.00f);

        public static Color Secondary = new Color(0.749f, 0.373f, 1.000f, 1.00f);
        public static Color SecondaryDim = new Color(0.135f, 0.095f, 0.175f, 1.00f);
        public static Color SecondaryBorder = new Color(0.228f, 0.135f, 0.295f, 1.00f);

        // ── Nav sidebar ──────────────────────────────────────────────
        public static Color NavActiveBg = new Color(0.120f, 0.078f, 0.178f, 1.00f);
        public static Color NavActiveText = new Color(0.749f, 0.373f, 1.000f, 1.00f);
        public static readonly Color NavInactiveText = new Color(1.000f, 1.000f, 1.000f, 1.00f);
        public static Color NavGlow = new Color(0.065f, 0.040f, 0.100f, 1.00f);

        public static Color BtnActionBg = new Color(0.240f, 0.195f, 0.320f, 1.00f);
        public static readonly Color BtnActionText = new Color(0.900f, 0.880f, 0.960f, 1.00f);
        public static Color BtnActionBorder = new Color(0.340f, 0.280f, 0.440f, 1.00f);

        public static Color BtnPrimaryBg = new Color(0.240f, 0.195f, 0.320f, 1.00f);
        public static readonly Color BtnPrimaryText = new Color(0.900f, 0.880f, 0.960f, 1.00f);
        public static Color BtnPrimaryBorder = new Color(0.340f, 0.280f, 0.440f, 1.00f);

        public static readonly Color BtnWarnBg = new Color(0.140f, 0.080f, 0.048f, 1.00f);
        public static readonly Color BtnWarnText = new Color(1.000f, 0.267f, 0.000f, 1.00f);
        public static readonly Color BtnWarnBorder = new Color(0.280f, 0.110f, 0.050f, 1.00f);

        // ── Text ─────────────────────────────────────────────────────
        public static readonly Color TextHeading = new Color(0.900f, 0.880f, 0.950f, 1.00f);
        public static readonly Color TextBody = new Color(0.530f, 0.510f, 0.580f, 1.00f);
        public static readonly Color TextDim = new Color(0.320f, 0.305f, 0.360f, 1.00f);
        public static readonly Color TextOnBtn = new Color(0.000f, 0.000f, 0.000f, 1.00f);

        // ── State indicators ─────────────────────────────────────────
        public static readonly Color StateOn = new Color(0.000f, 1.000f, 0.533f, 1.00f);
        public static readonly Color StateOnBg = new Color(0.063f, 0.145f, 0.098f, 1.00f);
        public static readonly Color StateOnBdr = new Color(0.055f, 0.310f, 0.192f, 1.00f);
        public static readonly Color StateOff = new Color(1.000f, 0.133f, 0.267f, 1.00f);
        public static readonly Color StateOffBg = new Color(0.130f, 0.063f, 0.067f, 1.00f);
        public static readonly Color StateOffBdr = new Color(0.255f, 0.075f, 0.098f, 1.00f);

        // ── Warning / orange ─────────────────────────────────────────
        public static readonly Color Warning = new Color(1.000f, 0.267f, 0.000f, 1.00f);
        public static readonly Color WarningDim = new Color(0.140f, 0.075f, 0.047f, 1.00f);
        public static readonly Color WarningBdr = new Color(0.280f, 0.110f, 0.050f, 1.00f);

        // ── Toggle switch ─────────────────────────────────────────────
        public static readonly Color ToggleTrackOn = new Color(0.152f, 0.102f, 0.215f, 1.00f);
        public static readonly Color ToggleTrackOff = new Color(0.135f, 0.110f, 0.178f, 1.00f);
        public static Color ToggleKnobOn = new Color(0.749f, 0.373f, 1.000f, 1.00f);
        public static readonly Color ToggleKnobOff = new Color(0.310f, 0.295f, 0.360f, 1.00f);

        // ── Slider / bar ─────────────────────────────────────────────
        public static Color SliderFill = new Color(0.749f, 0.373f, 1.000f, 1.00f);
        public static readonly Color SliderBg = new Color(0.135f, 0.110f, 0.178f, 1.00f);
    }
}

