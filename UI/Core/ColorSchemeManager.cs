using System;
using System.IO;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.UI
{
    [Serializable]
    public class ColorSchemeSaveData
    {
        public int SchemeIndex = 0;
    }

    public struct ColorScheme
    {
        public string Name;
        public Color Accent;
        public Color Secondary;
        // How strongly backgrounds/borders/buttons lean toward Accent.
        // 1.0 = normal reskin. Low values (e.g. Blackout) keep everything
        // near-neutral so the theme stays genuinely dark/monochrome
        // instead of the whole menu brightening to match a light accent.
        public float BgBlendMultiplier;

        public ColorScheme(string name, Color accent, Color secondary, float bgBlendMultiplier = 1f)
        {
            Name = name; Accent = accent; Secondary = secondary; BgBlendMultiplier = bgBlendMultiplier;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  ColorSchemeManager — preset palettes, saved to file.
    //  Index 0 (Purple) restores the exact original hand-tuned UITheme
    //  constants. Every other preset derives its full background/border/
    //  accent family from a neutral base blended toward that scheme's
    //  accent colour, so picking a scheme reskins the whole menu, not
    //  just the buttons.
    // ════════════════════════════════════════════════════════════════════
    public static class ColorSchemeManager
    {
        public static readonly ColorScheme[] Presets = new ColorScheme[]
        {
            new ColorScheme("Purple",      new Color(0.749f, 0.373f, 1.000f), new Color(0.749f, 0.373f, 1.000f)),
            new ColorScheme("Arboreal",    new Color(0.129f, 0.780f, 0.310f), new Color(0.129f, 0.780f, 0.310f)),
            new ColorScheme("Kinetic",     new Color(0.200f, 0.650f, 1.000f), new Color(0.200f, 0.650f, 1.000f)),
            new ColorScheme("Sunset",      new Color(1.000f, 0.500f, 0.150f), new Color(1.000f, 0.500f, 0.150f)),
            new ColorScheme("Enemy",       new Color(1.000f, 0.220f, 0.280f), new Color(1.000f, 0.220f, 0.280f)),
            new ColorScheme("Ice Cyan",    new Color(0.300f, 0.950f, 0.900f), new Color(0.300f, 0.950f, 0.900f)),
            new ColorScheme("Gold",        new Color(1.000f, 0.800f, 0.250f), new Color(1.000f, 0.800f, 0.250f)),
            new ColorScheme("Vaporwave",   new Color(1.000f, 0.200f, 0.800f), new Color(1.000f, 0.200f, 0.800f)),
            new ColorScheme("Radioactive", new Color(0.780f, 1.000f, 0.000f), new Color(0.780f, 1.000f, 0.000f)),
            new ColorScheme("Nightshade",  new Color(0.430f, 0.200f, 1.000f), new Color(0.430f, 0.200f, 1.000f)),
            new ColorScheme("Inferno",     new Color(1.000f, 0.235f, 0.078f), new Color(1.000f, 0.235f, 0.078f)),
            new ColorScheme("Frostbite",   new Color(0.400f, 0.560f, 0.780f), new Color(0.400f, 0.560f, 0.780f)),
            new ColorScheme("Blackout",    new Color(0.850f, 0.850f, 0.870f), new Color(0.850f, 0.850f, 0.870f), 0.15f),
        };

        public static int CurrentIndex { get; private set; } = 0;

        private static readonly string SaveFolder = Path.Combine(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData"),
            "DescendersModMenu");
        private const string SaveFileName = "ColorScheme.json";

        // ── Original hand-tuned Purple values — restored exactly for
        // index 0 so the shipped default never drifts from these. ──────
        private static readonly Color OrigBgOuter = new Color(0.047f, 0.040f, 0.068f, 0.98f);
        private static readonly Color OrigBgContent = new Color(0.075f, 0.062f, 0.108f, 1.00f);
        private static readonly Color OrigBgHeader = new Color(0.052f, 0.040f, 0.080f, 1.00f);
        private static readonly Color OrigBgRow = new Color(0.108f, 0.090f, 0.145f, 1.00f);
        private static readonly Color OrigBorderWin = new Color(0.155f, 0.130f, 0.200f, 1.00f);
        private static readonly Color OrigBorderRow = new Color(0.145f, 0.120f, 0.195f, 1.00f);
        private static readonly Color OrigNavGlow = new Color(0.065f, 0.040f, 0.100f, 1.00f);
        private static readonly Color OrigAccent = new Color(0.749f, 0.373f, 1.000f, 1.00f);
        private static readonly Color OrigAccentDim = new Color(0.135f, 0.095f, 0.175f, 1.00f);
        private static readonly Color OrigAccentBorder = new Color(0.228f, 0.135f, 0.295f, 1.00f);
        private static readonly Color OrigNavActiveBg = new Color(0.120f, 0.078f, 0.178f, 1.00f);
        private static readonly Color OrigBtnBg = new Color(0.240f, 0.195f, 0.320f, 1.00f);
        private static readonly Color OrigBtnBorder = new Color(0.340f, 0.280f, 0.440f, 1.00f);

        // ── Clean synthetic neutral bases used to derive every OTHER
        // scheme's background family. Deliberately near-black so the
        // menu stays readable no matter how bright the accent is. ──────
        private static readonly Color NeutralBgOuter = new Color(0.020f, 0.018f, 0.024f, 0.98f);
        private static readonly Color NeutralBgContent = new Color(0.035f, 0.032f, 0.042f, 1.00f);
        private static readonly Color NeutralBgHeader = new Color(0.024f, 0.020f, 0.032f, 1.00f);
        private static readonly Color NeutralBgRow = new Color(0.050f, 0.045f, 0.058f, 1.00f);
        private static readonly Color NeutralBorderWin = new Color(0.060f, 0.052f, 0.070f, 1.00f);
        private static readonly Color NeutralBorderRow = new Color(0.055f, 0.048f, 0.065f, 1.00f);
        private static readonly Color NeutralNavGlow = new Color(0.015f, 0.012f, 0.020f, 1.00f);
        private static readonly Color NeutralBtnBg = new Color(0.065f, 0.058f, 0.075f, 1.00f);
        private static readonly Color NeutralBtnBorder = new Color(0.095f, 0.085f, 0.108f, 1.00f);

        // Blend ratios: how strongly each field leans toward the scheme
        // accent vs. the neutral base above.
        private const float BgBlend = 0.06f;
        private const float HeaderBlend = 0.05f;
        private const float RowBlend = 0.08f;
        private const float BorderWinBlend = 0.18f;
        private const float BorderRowBlend = 0.15f;
        private const float NavGlowBlend = 0.10f;
        private const float BtnBlend = 0.20f;
        private const float BtnBorderBlend = 0.30f;
        private const float DimBlend = 0.09f;
        private const float AccentBorderBlend = 0.22f;
        private const float NavBlend = 0.07f;

        // ── Apply a preset by index ──────────────────────────────────
        public static void Apply(int index, bool rebuildMenu)
        {
            if (index < 0 || index >= Presets.Length)
            {
                MelonLogger.Warning("[ColorSchemeManager] Apply: index " + index + " out of range, using 0.");
                index = 0;
            }
            CurrentIndex = index;
            ColorScheme s = Presets[index];

            try
            {
                if (index == 0)
                {
                    // Purple — restore exact original constants, no drift.
                    UITheme.BgOuter = OrigBgOuter;
                    UITheme.BgContent = OrigBgContent;
                    UITheme.BgHeader = OrigBgHeader;
                    UITheme.BgSidebar = OrigBgHeader;
                    UITheme.BgRow = OrigBgRow;
                    UITheme.BorderWin = OrigBorderWin;
                    UITheme.BorderRow = OrigBorderRow;
                    UITheme.NavGlow = OrigNavGlow;
                    UITheme.Accent = OrigAccent;
                    UITheme.AccentDim = OrigAccentDim;
                    UITheme.AccentBorder = OrigAccentBorder;
                    UITheme.Secondary = OrigAccent;
                    UITheme.SecondaryDim = OrigAccentDim;
                    UITheme.SecondaryBorder = OrigAccentBorder;
                    UITheme.NavActiveBg = OrigNavActiveBg;
                    UITheme.NavActiveText = OrigAccent;
                    UITheme.ToggleKnobOn = OrigAccent;
                    UITheme.SliderFill = OrigAccent;
                    UITheme.BtnActionBg = OrigBtnBg;
                    UITheme.BtnActionBorder = OrigBtnBorder;
                    UITheme.BtnPrimaryBg = OrigBtnBg;
                    UITheme.BtnPrimaryBorder = OrigBtnBorder;
                }
                else
                {
                    float m = s.BgBlendMultiplier;
                    UITheme.BgOuter = Color.Lerp(NeutralBgOuter, s.Accent, BgBlend * m);
                    UITheme.BgContent = Color.Lerp(NeutralBgContent, s.Accent, BgBlend * m);
                    UITheme.BgHeader = Color.Lerp(NeutralBgHeader, s.Accent, HeaderBlend * m);
                    UITheme.BgSidebar = UITheme.BgHeader;
                    UITheme.BgRow = Color.Lerp(NeutralBgRow, s.Accent, RowBlend * m);
                    UITheme.BorderWin = Color.Lerp(NeutralBorderWin, s.Accent, BorderWinBlend * m);
                    UITheme.BorderRow = Color.Lerp(NeutralBorderRow, s.Accent, BorderRowBlend * m);
                    UITheme.NavGlow = Color.Lerp(NeutralNavGlow, s.Accent, NavGlowBlend * m);
                    UITheme.BtnActionBg = Color.Lerp(NeutralBtnBg, s.Accent, BtnBlend * m);
                    UITheme.BtnActionBorder = Color.Lerp(NeutralBtnBorder, s.Accent, BtnBorderBlend * m);
                    UITheme.BtnPrimaryBg = UITheme.BtnActionBg;
                    UITheme.BtnPrimaryBorder = UITheme.BtnActionBorder;

                    UITheme.Accent = s.Accent;
                    UITheme.AccentDim = Color.Lerp(UITheme.BgContent, s.Accent, DimBlend);
                    UITheme.AccentBorder = Color.Lerp(UITheme.BgContent, s.Accent, AccentBorderBlend);
                    UITheme.Secondary = s.Secondary;
                    UITheme.SecondaryDim = Color.Lerp(UITheme.BgContent, s.Secondary, DimBlend);
                    UITheme.SecondaryBorder = Color.Lerp(UITheme.BgContent, s.Secondary, AccentBorderBlend);
                    UITheme.NavActiveBg = Color.Lerp(UITheme.BgContent, s.Accent, NavBlend);
                    UITheme.NavActiveText = s.Accent;
                    UITheme.ToggleKnobOn = s.Accent;
                    UITheme.SliderFill = s.Accent;
                }

                MelonLogger.Msg("[ColorSchemeManager] Applied '" + s.Name + "' (index " + index + ").");
            }
            catch (Exception ex) { MelonLogger.Error("[ColorSchemeManager] Apply: " + ex.Message); }

            if (rebuildMenu)
            {
                try { MenuUI.RebuildMenu(); }
                catch (Exception ex) { MelonLogger.Error("[ColorSchemeManager] RebuildMenu: " + ex.Message); }
            }
        }

        // ── Called from a swatch button — applies, rebuilds the open
        // menu immediately (reopening on the Colour Scheme page so you
        // can keep clicking through options without renavigating), and
        // persists the choice to file. ───────────────────────────────
        public static void SelectScheme(int index)
        {
            MenuWindow.PendingPage = 3;      // Info/Customise page
            InfoPage.PendingSubTab = 2;      // Customise sub-tab
            Apply(index, true);
            Save();
        }

        public static void Save()
        {
            try
            {
                if (!Directory.Exists(SaveFolder)) Directory.CreateDirectory(SaveFolder);
                var data = new ColorSchemeSaveData { SchemeIndex = CurrentIndex };
                File.WriteAllText(Path.Combine(SaveFolder, SaveFileName), JsonUtility.ToJson(data, true));
                MelonLogger.Msg("[ColorSchemeManager] Saved scheme index " + CurrentIndex + ".");
            }
            catch (Exception ex) { MelonLogger.Error("[ColorSchemeManager] Save: " + ex.Message); }
        }

        // ── Called once at startup, before the menu is ever built ──────
        public static void LoadAndApply()
        {
            int index = 0;
            try
            {
                string path = Path.Combine(SaveFolder, SaveFileName);
                if (File.Exists(path))
                {
                    var data = JsonUtility.FromJson<ColorSchemeSaveData>(File.ReadAllText(path));
                    if (data != null) index = data.SchemeIndex;
                    MelonLogger.Msg("[ColorSchemeManager] Loaded scheme index " + index + " from file.");
                }
                else
                {
                    MelonLogger.Msg("[ColorSchemeManager] No colour scheme file — using default (Purple).");
                }
            }
            catch (Exception ex) { MelonLogger.Error("[ColorSchemeManager] LoadAndApply read: " + ex.Message); }

            Apply(index, false);
        }
    }
}
