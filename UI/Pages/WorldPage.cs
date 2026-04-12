using DescendersModMenu.Mods;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class WorldPage
    {
        // ── Sky Colours ───────────────────────────────────────────────────
        private static Image _stormTrack; private static RectTransform _stormKnob;
        private static Text _stormVal, _skyPresetVal;

        private static Text _gravityVal;
        private static Image _gravityBar;
        private static Text _todVal;
        private static Image _todBar;
        private static Text _treesVal;
        private static Image _treesTrack;
        private static RectTransform _treesKnob;
        private static Text _musicVal;
        private static Image _musicTrack;
        private static RectTransform _musicKnob;
        private static Text _fogVal;
        private static Image _fogTrack;
        private static RectTransform _fogKnob;





        // Turbo Wind UI refs
        private static Image _windTrack; private static RectTransform _windKnob;
        private static Text _windVal;

        // Exploding Props UI refs
        private static Image _explodeTrack; private static RectTransform _explodeKnob;
        private static Text _explodeVal;

        // Headlights Only
        private static GameObject _hlRow;
        private static Text _hlVal;
        private static Image _hlTrack;
        private static RectTransform _hlKnob;

        public static bool IsAnyActive =>
            SkyColours.CurrentPreset != 0 || SkyColours.StormEnabled ||
            Gravity.Level != 5 ||
            Trees.Enabled || Music.Enabled || Fog.Enabled ||
            TurboWind.Enabled || ExplodingProps.Enabled || HeadlightsOnly.Enabled;

        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("P7R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));

                // ScrollRect wrapper — same pattern as FunPage
                var scrollObj = UIHelpers.Obj("Scroll", pg.transform);
                UIHelpers.Fill(UIHelpers.RT(scrollObj));
                var scrollRect = scrollObj.AddComponent<ScrollRect>();
                scrollRect.horizontal = false; scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 25f;
                scrollRect.inertia = false;

                var vp = UIHelpers.Obj("VP", scrollObj.transform);
                UIHelpers.Fill(UIHelpers.RT(vp));
                vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                vp.AddComponent<Mask>().showMaskGraphic = true;
                scrollRect.viewport = UIHelpers.RT(vp);

                var content = UIHelpers.Obj("Content", vp.transform);
                var crt = UIHelpers.RT(content);
                crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1);
                crt.sizeDelta = new Vector2(0, 0);
                scrollRect.content = crt;
                UIHelpers.AddScrollbar(scrollRect);

                var csf = content.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                // All rows go into scrollable content
                var pg7 = content.transform;

                // ── RESET TAB ─────────────────────────────────────────
                var rstRow = UIHelpers.StatRow("", pg7);
                UIHelpers.ActionBtnOrange(rstRow.transform, "↺  Reset Tab to Defaults", () => { ResetWorldTab(); RefreshAll(); }, 186);
                UIHelpers.SectionHeader("SKY", pg7);

                var skpr = UIHelpers.StatRow("Colour", pg7);
                _skyPresetVal = UIHelpers.Txt("SkV", skpr.transform, SkyColours.PresetNames[0], 11,
                    FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.Accent);
                _skyPresetVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;
                UIHelpers.ActionBtn(skpr.transform, "Default", () => { SkyColours.RestoreDefault(); RefreshAll(); }, 50);
                UIHelpers.ActionBtn(skpr.transform, "Blood Red", () => { SkyColours.ApplyPreset(1); RefreshAll(); }, 58);
                UIHelpers.ActionBtn(skpr.transform, "Alien", () => { SkyColours.ApplyPreset(2); RefreshAll(); }, 40);
                UIHelpers.ActionBtn(skpr.transform, "Synthwave", () => { SkyColours.ApplyPreset(3); RefreshAll(); }, 60);
                UIHelpers.ActionBtn(skpr.transform, "Midnight", () => { SkyColours.ApplyPreset(4); RefreshAll(); }, 55);
                UIHelpers.ActionBtn(skpr.transform, "Toxic", () => { SkyColours.ApplyPreset(5); RefreshAll(); }, 40);

                var stmr = UIHelpers.StatRow("Storm", pg7);
                _stormVal = UIHelpers.Txt("StmV", stmr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _stormVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(stmr.transform, "StmT", () => { SkyColours.ToggleStorm(); RefreshAll(); }, out _stormTrack, out _stormKnob);


                UIHelpers.Divider(pg7);

                // ── Physics ───────────────────────────────────────────────
                UIHelpers.SectionHeader("PHYSICS", pg7);

                var gr = UIHelpers.StatRow("Gravity", pg7);
                _gravityBar = UIHelpers.MakeBar("GrB", gr.transform, (Gravity.Level - 1) / 9f);
                _gravityVal = UIHelpers.Txt("GrV", gr.transform, Gravity.DisplayValue, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _gravityVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                UIHelpers.SmallBtn(gr.transform, "-", () => { Gravity.Decrease(); RefreshAll(); });
                UIHelpers.SmallBtn(gr.transform, "+", () => { Gravity.Increase(); RefreshAll(); });
                UIHelpers.Txt("GrHint", gr.transform, "def:-17.5", 9, FontStyle.Italic, TextAnchor.MiddleRight, UIHelpers.TextDim)
                    .gameObject.AddComponent<LayoutElement>().preferredWidth = 52;

                UIHelpers.Divider(pg7);

                // ── Environment ───────────────────────────────────────────
                UIHelpers.SectionHeader("ENVIRONMENT", pg7);

                var tr = UIHelpers.StatRow("Time of Day", pg7);
                _todBar = UIHelpers.MakeBar("TdB", tr.transform, (TimeOfDay.Level - 1) / 9f);
                _todVal = UIHelpers.Txt("TdV", tr.transform, TimeOfDay.DisplayValue, 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _todVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 70;
                UIHelpers.SmallBtn(tr.transform, "-", () => { TimeOfDay.Decrease(); RefreshAll(); });
                UIHelpers.SmallBtn(tr.transform, "+", () => { TimeOfDay.Increase(); RefreshAll(); });

                var ter = UIHelpers.StatRow("Trees & Foliage", pg7);
                _treesVal = UIHelpers.Txt("TrV", ter.transform, "ON", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _treesVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(ter.transform, "TrT", () =>
                {
                    Trees.Toggle();
                    RefreshAll();
                }, out _treesTrack, out _treesKnob);

                var mur = UIHelpers.StatRow("Music", pg7);
                _musicVal = UIHelpers.Txt("MuV", mur.transform, "ON", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _musicVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(mur.transform, "MuT", () =>
                {
                    Music.Toggle();
                    RefreshAll();
                }, out _musicTrack, out _musicKnob);

                var fogr = UIHelpers.StatRow("Fog", pg7);
                _fogVal = UIHelpers.Txt("FgV", fogr.transform, "ON", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _fogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(fogr.transform, "FgT", () =>
                {
                    Fog.Toggle();
                    RefreshAll();
                }, out _fogTrack, out _fogKnob);

                _hlRow = UIHelpers.StatRow("Headlights Only", pg7);
                _hlVal = UIHelpers.Txt("HlV", _hlRow.transform, "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _hlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(_hlRow.transform, "HlT", () =>
                {
                    HeadlightsOnly.Toggle();
                    RefreshAll();
                }, out _hlTrack, out _hlKnob);
                UIHelpers.InfoBox(pg7, "Kills all ambient and directional lighting. Only your headlight illuminates the trail. BikeTorch is auto-enabled at max intensity.");

                var wr = UIHelpers.StatRow("Turbo Wind", pg7);
                _windVal = UIHelpers.Txt("WnV", wr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _windVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(wr.transform, "WnT", () =>
                {
                    TurboWind.Toggle();
                    RefreshAll();
                }, out _windTrack, out _windKnob);

                var er = UIHelpers.StatRow("No Mistakes", pg7);
                _explodeVal = UIHelpers.Txt("ExV", er.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _explodeVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                var erHint = UIHelpers.Txt("ExH", er.transform, "launch on impact", 9, FontStyle.Italic, TextAnchor.MiddleRight, UIHelpers.TextDim);
                erHint.gameObject.AddComponent<LayoutElement>().preferredWidth = 90;
                UIHelpers.Toggle(er.transform, "ExT", () =>
                {
                    ExplodingProps.Toggle();
                    RefreshAll();
                }, out _explodeTrack, out _explodeKnob);

                UIHelpers.Divider(pg7);
                UIHelpers.Divider(pg7);

                // ── Level Tools ───────────────────────────────────────────
                UIHelpers.SectionHeader("LEVEL", pg7);

                var jr = UIHelpers.StatRow("Jump to Finish", pg7);
                UIHelpers.ActionBtnOrange(jr.transform, "Jump", () =>
                {
                    try { DevCommandsGameplay.JumpToFinish(); }
                    catch (System.Exception ex) { MelonLogger.Error("[JumpToFinish]: " + ex.Message); }
                }, 60);

                var sr = UIHelpers.StatRow("Skip Song", pg7);
                UIHelpers.ActionBtn(sr.transform, "Skip", () =>
                {
                    try { DevCommandsGameplay.SkipSong(); }
                    catch (System.Exception ex) { MelonLogger.Error("[SkipSong]: " + ex.Message); }
                }, 60);

                // ── STAR BUTTONS (Favourites) ──────────────────────────
                FavouritesManager.RegisterStarButton("Gravity", UIHelpers.StarBtn(gr.transform, "Gravity", () => FavouritesManager.Toggle("Gravity")));
                FavouritesManager.RegisterStarButton("TimeOfDay", UIHelpers.StarBtn(tr.transform, "TimeOfDay", () => FavouritesManager.Toggle("TimeOfDay")));
                FavouritesManager.RegisterStarButton("TurboWind", UIHelpers.StarBtn(wr.transform, "TurboWind", () => FavouritesManager.Toggle("TurboWind")));
                FavouritesManager.RegisterStarButton("ExplodingProps", UIHelpers.StarBtn(er.transform, "ExplodingProps", () => FavouritesManager.Toggle("ExplodingProps")));
                FavouritesManager.RegisterStarButton("Storm", UIHelpers.StarBtn(stmr.transform, "Storm", () => FavouritesManager.Toggle("Storm")));
                FavouritesManager.RegisterStarButton("Trees", UIHelpers.StarBtn(ter.transform, "Trees", () => FavouritesManager.Toggle("Trees")));
                FavouritesManager.RegisterStarButton("Music", UIHelpers.StarBtn(mur.transform, "Music", () => FavouritesManager.Toggle("Music")));
                FavouritesManager.RegisterStarButton("Fog", UIHelpers.StarBtn(fogr.transform, "Fog", () => FavouritesManager.Toggle("Fog")));
                FavouritesManager.RegisterStarButton("HeadlightsOnly", UIHelpers.StarBtn(_hlRow.transform, "HeadlightsOnly", () => FavouritesManager.Toggle("HeadlightsOnly")));
                FavouritesManager.RegisterStarButton("SkyColour", UIHelpers.StarBtn(skpr.transform, "SkyColour", () => FavouritesManager.Toggle("SkyColour")));

                // ── FACTORY REGISTRATIONS (World tab mods) ─────────────
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "Gravity",
                    DisplayName = "Gravity",
                    TabBadge = "WORLD",
                    BuildControls = (p) => FavsPage.BuildSliderOnly(p, "Gravity", "Gravity",
                        () => Gravity.Level, () => Gravity.Increase(), () => Gravity.Decrease(),
                        () => (Gravity.Level - 1) / 9f, () => RefreshAll(),
                        () => Gravity.DisplayValue, () => Gravity.Level != 5),
                    IsActive = () => Gravity.Level != 5
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "TimeOfDay",
                    DisplayName = "Time of Day",
                    TabBadge = "WORLD",
                    BuildControls = (p) => FavsPage.BuildSliderOnly(p, "TimeOfDay", "Time of Day",
                        () => TimeOfDay.Level, () => TimeOfDay.Increase(), () => TimeOfDay.Decrease(),
                        () => (TimeOfDay.Level - 1) / 9f, () => RefreshAll(),
                        () => TimeOfDay.DisplayValue, null),
                    IsActive = () => false
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "ExplodingProps",
                    DisplayName = "Exploding Props",
                    TabBadge = "WORLD",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "ExplodingProps", "Exploding Props",
                        () => ExplodingProps.Enabled, () => { ExplodingProps.Toggle(); }, () => RefreshAll()),
                    IsActive = () => ExplodingProps.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "TurboWind",
                    DisplayName = "Turbo Wind",
                    TabBadge = "WORLD",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "TurboWind", "Turbo Wind",
                        () => TurboWind.Enabled, () => { TurboWind.Toggle(); }, () => RefreshAll()),
                    IsActive = () => TurboWind.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "Storm",
                    DisplayName = "Storm",
                    TabBadge = "WORLD",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "Storm", "Storm",
                        () => SkyColours.StormEnabled, () => SkyColours.ToggleStorm(), () => RefreshAll()),
                    IsActive = () => SkyColours.StormEnabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "Trees",
                    DisplayName = "Trees & Foliage",
                    TabBadge = "WORLD",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "Trees", "Trees & Foliage",
                        () => Trees.Enabled, () => { Trees.Toggle(); }, () => RefreshAll()),
                    IsActive = () => Trees.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "Music",
                    DisplayName = "Music",
                    TabBadge = "WORLD",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "Music", "Music",
                        () => Music.Enabled, () => { Music.Toggle(); }, () => RefreshAll()),
                    IsActive = () => Music.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "Fog",
                    DisplayName = "Fog",
                    TabBadge = "WORLD",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "Fog", "Fog",
                        () => Fog.Enabled, () => { Fog.Toggle(); }, () => RefreshAll()),
                    IsActive = () => Fog.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "HeadlightsOnly",
                    DisplayName = "Headlights Only",
                    TabBadge = "WORLD",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "HeadlightsOnly", "Headlights Only",
                        () => HeadlightsOnly.Enabled, () => HeadlightsOnly.Toggle(), () => RefreshAll()),
                    IsActive = () => HeadlightsOnly.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "SkyColour",
                    DisplayName = "Sky Colour",
                    TabBadge = "WORLD",
                    BuildControls = (p) => {
                        var row = UIHelpers.StatRow("Colour", p);
                        UIHelpers.ActionBtn(row.transform, "Default", () => { SkyColours.RestoreDefault(); RefreshAll(); FavsPage.RefreshFavourites(); }, 50);
                        UIHelpers.ActionBtn(row.transform, "Blood Red", () => { SkyColours.ApplyPreset(1); RefreshAll(); FavsPage.RefreshFavourites(); }, 58);
                        UIHelpers.ActionBtn(row.transform, "Alien", () => { SkyColours.ApplyPreset(2); RefreshAll(); FavsPage.RefreshFavourites(); }, 40);
                        UIHelpers.ActionBtn(row.transform, "Synthwave", () => { SkyColours.ApplyPreset(3); RefreshAll(); FavsPage.RefreshFavourites(); }, 60);
                        UIHelpers.ActionBtn(row.transform, "Midnight", () => { SkyColours.ApplyPreset(4); RefreshAll(); FavsPage.RefreshFavourites(); }, 55);
                        UIHelpers.ActionBtn(row.transform, "Toxic", () => { SkyColours.ApplyPreset(5); RefreshAll(); FavsPage.RefreshFavourites(); }, 40);
                    },
                    IsActive = () => SkyColours.CurrentPreset != 0
                });

                RefreshAll();
                UIHelpers.AddScrollForwarders(pg7);
            }
            catch (System.Exception ex) { MelonLogger.Error("WorldPage.CreatePage: " + ex.Message); return null; }
            return pg;
        }





        private static void ResetWorldTab()
        {
            SkyColours.RestoreDefault();
            if (SkyColours.StormEnabled) SkyColours.ToggleStorm();
            SkyColours.SetRainIntensityLevel(5);
            Gravity.SetLevel(5);
            TimeOfDay.ResetToSceneDefault();
            GlobalReset();
        }

        public static void GlobalReset()
        {
            TurboWind.Reset();
            Trees.Reset();
            Music.Reset();
            Fog.Reset();
            if (HeadlightsOnly.Enabled) HeadlightsOnly.Toggle();
        }

        public static void RefreshAll()
        {
            if (_gravityVal) _gravityVal.text = Gravity.DisplayValue;
            UIHelpers.SetBar(_gravityBar, (Gravity.Level - 1) / 9f);
            if (_todVal) _todVal.text = TimeOfDay.DisplayValue;
            UIHelpers.SetBar(_todBar, (TimeOfDay.Level - 1) / 9f);

            if (_treesVal) { _treesVal.text = !Trees.Enabled ? "ON" : "OFF"; _treesVal.color = !Trees.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_treesTrack, _treesKnob, !Trees.Enabled);

            if (_musicVal) { _musicVal.text = !Music.Enabled ? "ON" : "OFF"; _musicVal.color = !Music.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_musicTrack, _musicKnob, !Music.Enabled);

            if (_fogVal) { _fogVal.text = !Fog.Enabled ? "ON" : "OFF"; _fogVal.color = !Fog.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_fogTrack, _fogKnob, !Fog.Enabled);

            bool hlOn = HeadlightsOnly.Enabled;
            if (_hlVal) { _hlVal.text = hlOn ? "ON" : "OFF"; _hlVal.color = hlOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_hlTrack, _hlKnob, hlOn);

            if (_windVal) { _windVal.text = TurboWind.Enabled ? "ON" : "OFF"; _windVal.color = TurboWind.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_windTrack, _windKnob, TurboWind.Enabled);

            if (_explodeVal) { _explodeVal.text = ExplodingProps.Enabled ? "ON" : "OFF"; _explodeVal.color = ExplodingProps.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_explodeTrack, _explodeKnob, ExplodingProps.Enabled);

            if (_skyPresetVal) _skyPresetVal.text = SkyColours.PresetNames[SkyColours.CurrentPreset];
            bool storm = SkyColours.StormEnabled;
            if (_stormVal) { _stormVal.text = storm ? "ON" : "OFF"; _stormVal.color = storm ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_stormTrack, _stormKnob, storm);
        }
    }
}