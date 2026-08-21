using DescendersModMenu.Mods;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class MovePage
    {
        // ── Movement row fields ───────────────────────────────────────
        private static Text spinVal, wheelieVal, leanVal;
        private static Image spinBar, wheelieBar, leanBar;
        private static Text spinTogVal, wheelieTogVal, leanTogVal;
        private static Image spinTrack, wheelieTrack, leanTrack;
        private static RectTransform spinKnob, wheelieKnob, leanKnob;

        private static Text wbVal, psVal;
        private static Image wbBar, psBar;
        private static Text _wbTogVal;
        private static Image _wbTrack;
        private static RectTransform _wbKnob;

        private static Text _gmWbVal, _gmTsVal;
        private static Image _gmWbBar, _gmTsBar;

        private static Text _pwtTogVal;
        private static Image _pwtTrack;
        private static RectTransform _pwtKnob;

        // ── Wheelie HUD row fields ────────────────────────────────────
        private static GameObject _whRow;
        private static Text _whTogVal;
        private static Image _whTrack;
        private static RectTransform _whKnob;

        // ── Near Miss Sensitivity ─────────────────────────────────────
        private static Text _nmVal, _nmTogVal;
        private static Image _nmBar, _nmTrack;
        private static RectTransform _nmKnob;

        // ── Center of Mass ────────────────────────────────────────────
        private static Text _comLRVal, _comFBVal, _comUDVal;
        private static Image _comLRBar, _comFBBar, _comUDBar;

        public static bool IsAnyActive =>
            Movement.SpinEnabled ||
            Movement.WheelieEnabled || Movement.LeanEnabled ||
            WheelieAngleLimit.Enabled ||
            WheelieHUD.Enabled ||
            GameModifierMods.PumpStrengthLevel != 5 ||
            GameModifierMods.WheelieBalanceLevel != 5 ||
            GameModifierMods.TweakSpeedLevel != 5 ||
            PedalWhileTweak.Enabled ||
            NearMissSensitivity.Enabled ||
            CenterOfMass.OffsetLR != 0f || CenterOfMass.OffsetFB != 0f || CenterOfMass.OffsetUD != 0f;

        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("P6R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));

                // ── ScrollRect wrapper ────────────────────────────────
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
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

                var pg6 = content.transform;

                // ── RESET TAB ─────────────────────────────────────────
                var rstRow = UIHelpers.BareBtnRow(pg6);
                UIHelpers.ActionBtnOrange(rstRow.transform, "↺  Reset Tab to Defaults", () => { ResetMoveTab(); RefreshAll(); }, 186);
                UIHelpers.SectionHeader("MOVEMENT", pg6);

                var sr = UIHelpers.StatRow("Rotation Speed", pg6);
                spinTogVal = UIHelpers.Txt("SpTV", sr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                spinTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(sr.transform, "SpT", () => { Movement.ToggleSpin(); RefreshAll(); }, out spinTrack, out spinKnob);
                spinBar = UIHelpers.MakeBar("SpB", sr.transform, (Movement.SpinLevel - 1) / (float)(Movement.MaxLevel - 1));
                spinVal = UIHelpers.Txt("SpV", sr.transform, Movement.SpinLevel.ToString(), 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                spinVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
                UIHelpers.SmallBtn(sr.transform, "-", () => { Movement.SpinDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(sr.transform, "+", () => { Movement.SpinIncrease(); RefreshAll(); });

                var wr = UIHelpers.StatRow("Wheelie Force", pg6);
                wheelieTogVal = UIHelpers.Txt("WlTV", wr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                wheelieTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(wr.transform, "WlT", () => { Movement.ToggleWheelie(); RefreshAll(); }, out wheelieTrack, out wheelieKnob);
                wheelieBar = UIHelpers.MakeBar("WlB", wr.transform, (Movement.WheelieLevel - 1) / (float)(Movement.MaxLevel - 1));
                wheelieVal = UIHelpers.Txt("WlV", wr.transform, Movement.WheelieLevel.ToString(), 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                wheelieVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
                UIHelpers.SmallBtn(wr.transform, "-", () => { Movement.WheelieDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(wr.transform, "+", () => { Movement.WheelieIncrease(); RefreshAll(); });

                var lr = UIHelpers.StatRow("Lean Strength", pg6);
                leanTogVal = UIHelpers.Txt("LnTV", lr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                leanTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(lr.transform, "LnT", () => { Movement.ToggleLean(); RefreshAll(); }, out leanTrack, out leanKnob);
                leanBar = UIHelpers.MakeBar("LnB", lr.transform, (Movement.LeanLevel - 1) / (float)(Movement.MaxLevel - 1));
                leanVal = UIHelpers.Txt("LnV", lr.transform, Movement.LeanLevel.ToString(), 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                leanVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
                UIHelpers.SmallBtn(lr.transform, "-", () => { Movement.LeanDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(lr.transform, "+", () => { Movement.LeanIncrease(); RefreshAll(); });

                UIHelpers.Divider(pg6);

                // ── BALANCE & PHYSICS ─────────────────────────────────
                UIHelpers.SectionHeader("BALANCE & PHYSICS", pg6);

                var wbr = UIHelpers.StatRow("Wheelie Angle Limit", pg6);
                wbBar = UIHelpers.MakeBar("WbB", wbr.transform, (WheelieAngleLimit.Level - 1) / 9f);
                wbVal = UIHelpers.Txt("WbV", wbr.transform, WheelieAngleLimit.DisplayValue, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                wbVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                _wbTogVal = UIHelpers.Txt("WbTV", wbr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _wbTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                Image wbTrack; RectTransform wbKnob;
                UIHelpers.Toggle(wbr.transform, "WbT", () => { WheelieAngleLimit.Toggle(); RefreshAll(); }, out wbTrack, out wbKnob);
                UIHelpers.SmallBtn(wbr.transform, "-", () => { WheelieAngleLimit.Decrease(); RefreshAll(); });
                UIHelpers.SmallBtn(wbr.transform, "+", () => { WheelieAngleLimit.Increase(); RefreshAll(); });
                _wbTrack = wbTrack; _wbKnob = wbKnob;
                UIHelpers.InfoBox(pg6, "Stops your wheelie looping too far. Lower = you tip over sooner.");

                var whr = UIHelpers.StatRow("Wheelie HUD", pg6);
                _whRow = whr;
                _whTogVal = UIHelpers.Txt("WhV", whr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _whTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                Image whTrack; RectTransform whKnob;
                UIHelpers.Toggle(whr.transform, "WhT", () => { WheelieHUD.Toggle(); RefreshAll(); }, out whTrack, out whKnob);
                _whTrack = whTrack; _whKnob = whKnob;
                UIHelpers.InfoBox(pg6, "Shows a wheelie meter in the corner. Goes red as you get close to tipping.");

                var fbr = UIHelpers.StatRow("Pump Strength", pg6);
                psBar = UIHelpers.MakeBar("PsB", fbr.transform, (GameModifierMods.PumpStrengthLevel - 1) / 9f);
                psVal = UIHelpers.Txt("PsV", fbr.transform, GameModifierMods.DeltaDisplay(GameModifierMods.PumpStrengthLevel), 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                psVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
                UIHelpers.SmallBtn(fbr.transform, "-", () => { GameModifierMods.PumpStrengthDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(fbr.transform, "+", () => { GameModifierMods.PumpStrengthIncrease(); RefreshAll(); });
                UIHelpers.InfoBox(pg6, "How hard you pump for speed. 0% is normal. Higher = more speed from pumps.");

                var gmWbR = UIHelpers.StatRow("Wheelie Balance", pg6);
                _gmWbBar = UIHelpers.MakeBar("GmWbB", gmWbR.transform, (GameModifierMods.WheelieBalanceLevel - 1) / 9f);
                _gmWbVal = UIHelpers.Txt("GmWbV", gmWbR.transform, GameModifierMods.DeltaDisplay(GameModifierMods.WheelieBalanceLevel), 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _gmWbVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
                UIHelpers.SmallBtn(gmWbR.transform, "-", () => { GameModifierMods.WheelieBalanceDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(gmWbR.transform, "+", () => { GameModifierMods.WheelieBalanceIncrease(); RefreshAll(); });
                UIHelpers.InfoBox(pg6, "How easy wheelies are to hold. 0% is normal. Higher = more help balancing.");

                var gmTsR = UIHelpers.StatRow("Tweak Speed", pg6);
                _gmTsBar = UIHelpers.MakeBar("GmTsB", gmTsR.transform, (GameModifierMods.TweakSpeedLevel - 1) / 9f);
                _gmTsVal = UIHelpers.Txt("GmTsV", gmTsR.transform, GameModifierMods.DeltaDisplay(GameModifierMods.TweakSpeedLevel), 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _gmTsVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
                UIHelpers.SmallBtn(gmTsR.transform, "-", () => { GameModifierMods.TweakSpeedDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(gmTsR.transform, "+", () => { GameModifierMods.TweakSpeedIncrease(); RefreshAll(); });
                UIHelpers.InfoBox(pg6, "How fast you can twist the bike mid-trick. 0% is normal.");

                var pwtR = UIHelpers.StatRow("Pedal While Tweak", pg6);
                _pwtTogVal = UIHelpers.Txt("PwtTV", pwtR.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _pwtTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                Image pwtTrack; RectTransform pwtKnob;
                UIHelpers.Toggle(pwtR.transform, "PwtT", () => { PedalWhileTweak.Toggle(); RefreshAll(); }, out pwtTrack, out pwtKnob);
                _pwtTrack = pwtTrack; _pwtKnob = pwtKnob;
                UIHelpers.InfoBox(pg6, "Lets you keep pedalling while you tweak with the stick.");
                FavouritesManager.RegisterStarButton("PedalWhileTweak", UIHelpers.StarBtn(pwtR.transform, "PedalWhileTweak", () => FavouritesManager.Toggle("PedalWhileTweak")));

                var nmr = UIHelpers.StatRow("Near Miss Sensitivity", pg6);
                _nmBar = UIHelpers.MakeBar("NmB", nmr.transform, (NearMissSensitivity.Level - 1) / 9f);
                _nmVal = UIHelpers.Txt("NmV", nmr.transform, NearMissSensitivity.DisplayValue, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _nmVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
                _nmTogVal = UIHelpers.Txt("NmTV", nmr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _nmTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                Image nmTrack; RectTransform nmKnob;
                UIHelpers.Toggle(nmr.transform, "NmT", () => { NearMissSensitivity.Toggle(); RefreshAll(); }, out nmTrack, out nmKnob);
                UIHelpers.SmallBtn(nmr.transform, "-", () => { NearMissSensitivity.Decrease(); RefreshAll(); });
                UIHelpers.SmallBtn(nmr.transform, "+", () => { NearMissSensitivity.Increase(); RefreshAll(); });
                _nmTrack = nmTrack; _nmKnob = nmKnob;
                UIHelpers.InfoBox(pg6, "How close you need to get for a near miss. Higher = easier near misses.");

                UIHelpers.Divider(pg6);

                // ── CENTER OF MASS ─────────────────────────────────────
                UIHelpers.SectionHeader("CENTER OF MASS", pg6);

                var comLRr = UIHelpers.StatRow("Left / Right", pg6);
                _comLRBar = UIHelpers.MakeBar("CLrB", comLRr.transform, CenterOfMass.BarLR);
                _comLRVal = UIHelpers.Txt("CLrV", comLRr.transform, CenterOfMass.DisplayLR, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _comLRVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                UIHelpers.ActionBtn(comLRr.transform, "0", () => { CenterOfMass.ResetLR(); RefreshAll(); }, 22);
                UIHelpers.SmallBtn(comLRr.transform, "-", () => { CenterOfMass.DecreaseLR(); RefreshAll(); });
                UIHelpers.SmallBtn(comLRr.transform, "+", () => { CenterOfMass.IncreaseLR(); RefreshAll(); });

                var comFBr = UIHelpers.StatRow("Forward / Back", pg6);
                _comFBBar = UIHelpers.MakeBar("CFbB", comFBr.transform, CenterOfMass.BarFB);
                _comFBVal = UIHelpers.Txt("CFbV", comFBr.transform, CenterOfMass.DisplayFB, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _comFBVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                UIHelpers.ActionBtn(comFBr.transform, "0", () => { CenterOfMass.ResetFB(); RefreshAll(); }, 22);
                UIHelpers.SmallBtn(comFBr.transform, "-", () => { CenterOfMass.DecreaseFB(); RefreshAll(); });
                UIHelpers.SmallBtn(comFBr.transform, "+", () => { CenterOfMass.IncreaseFB(); RefreshAll(); });

                var comUDr = UIHelpers.StatRow("Up / Down", pg6);
                _comUDBar = UIHelpers.MakeBar("CUdB", comUDr.transform, CenterOfMass.BarUD);
                _comUDVal = UIHelpers.Txt("CUdV", comUDr.transform, CenterOfMass.DisplayUD, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _comUDVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                UIHelpers.ActionBtn(comUDr.transform, "0", () => { CenterOfMass.ResetUD(); RefreshAll(); }, 22);
                UIHelpers.SmallBtn(comUDr.transform, "-", () => { CenterOfMass.DecreaseUD(); RefreshAll(); });
                UIHelpers.SmallBtn(comUDr.transform, "+", () => { CenterOfMass.IncreaseUD(); RefreshAll(); });

                UIHelpers.InfoBox(pg6, "Moves where the bike balances. Start at 0. Press 0 on an axis to reset it.");

                FavouritesManager.RegisterStarButton("Spin", UIHelpers.StarBtn(sr.transform, "Spin", () => FavouritesManager.Toggle("Spin")));
                FavouritesManager.RegisterStarButton("Wheelie", UIHelpers.StarBtn(wr.transform, "Wheelie", () => FavouritesManager.Toggle("Wheelie")));
                FavouritesManager.RegisterStarButton("Lean", UIHelpers.StarBtn(lr.transform, "Lean", () => FavouritesManager.Toggle("Lean")));
                FavouritesManager.RegisterStarButton("WheelieAngle", UIHelpers.StarBtn(wbr.transform, "WheelieAngle", () => FavouritesManager.Toggle("WheelieAngle")));
                FavouritesManager.RegisterStarButton("WheelieHUD", UIHelpers.StarBtn(whr.transform, "WheelieHUD", () => FavouritesManager.Toggle("WheelieHUD")));
                FavouritesManager.RegisterStarButton("PumpStrength", UIHelpers.StarBtn(fbr.transform, "PumpStrength", () => FavouritesManager.Toggle("PumpStrength")));
                FavouritesManager.RegisterStarButton("GmWheelieBalance", UIHelpers.StarBtn(gmWbR.transform, "GmWheelieBalance", () => FavouritesManager.Toggle("GmWheelieBalance")));
                FavouritesManager.RegisterStarButton("TweakSpeed", UIHelpers.StarBtn(gmTsR.transform, "TweakSpeed", () => FavouritesManager.Toggle("TweakSpeed")));
                FavouritesManager.RegisterStarButton("NearMiss", UIHelpers.StarBtn(nmr.transform, "NearMiss", () => FavouritesManager.Toggle("NearMiss")));
                Transform comHdr = pg6.Find("CENTER OF MASSH");
                if ((object)comHdr != null)
                    FavouritesManager.RegisterStarButton("CenterOfMass", UIHelpers.StarBtnAbs(comHdr, "CenterOfMass", () => FavouritesManager.Toggle("CenterOfMass")));

                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "Spin",
                    DisplayName = "Rotation Speed",
                    TabBadge = "MOVE",
                    BuildControls = (p) => FavsPage.BuildToggleSlider(p, "Spin", "Rotation Speed",
                        () => Movement.SpinEnabled, () => Movement.ToggleSpin(),
                        () => Movement.SpinLevel, () => Movement.SpinIncrease(), () => Movement.SpinDecrease(),
                        Movement.MaxLevel, () => (Movement.SpinLevel - 1) / (float)(Movement.MaxLevel - 1), () => RefreshAll()),
                    IsActive = () => Movement.SpinEnabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "Wheelie",
                    DisplayName = "Wheelie Force",
                    TabBadge = "MOVE",
                    BuildControls = (p) => FavsPage.BuildToggleSlider(p, "Wheelie", "Wheelie Force",
                        () => Movement.WheelieEnabled, () => Movement.ToggleWheelie(),
                        () => Movement.WheelieLevel, () => Movement.WheelieIncrease(), () => Movement.WheelieDecrease(),
                        Movement.MaxLevel, () => (Movement.WheelieLevel - 1) / (float)(Movement.MaxLevel - 1), () => RefreshAll()),
                    IsActive = () => Movement.WheelieEnabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "Lean",
                    DisplayName = "Lean Strength",
                    TabBadge = "MOVE",
                    BuildControls = (p) => FavsPage.BuildToggleSlider(p, "Lean", "Lean Strength",
                        () => Movement.LeanEnabled, () => Movement.ToggleLean(),
                        () => Movement.LeanLevel, () => Movement.LeanIncrease(), () => Movement.LeanDecrease(),
                        Movement.MaxLevel, () => (Movement.LeanLevel - 1) / (float)(Movement.MaxLevel - 1), () => RefreshAll()),
                    IsActive = () => Movement.LeanEnabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "WheelieAngle",
                    DisplayName = "Wheelie Angle Limit",
                    TabBadge = "MOVE",
                    BuildControls = (p) => FavsPage.BuildToggleSlider(p, "WheelieAngle", "Wheelie Angle Limit",
                        () => WheelieAngleLimit.Enabled, () => WheelieAngleLimit.Toggle(),
                        () => WheelieAngleLimit.Level, () => WheelieAngleLimit.Increase(), () => WheelieAngleLimit.Decrease(),
                        10, () => (WheelieAngleLimit.Level - 1) / 9f, () => RefreshAll(),
                        () => WheelieAngleLimit.DisplayValue),
                    IsActive = () => WheelieAngleLimit.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "WheelieHUD",
                    DisplayName = "Wheelie HUD",
                    TabBadge = "MOVE",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "WheelieHUD", "Wheelie HUD",
                        () => WheelieHUD.Enabled, () => WheelieHUD.Toggle(), () => RefreshAll()),
                    IsActive = () => WheelieHUD.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "PumpStrength",
                    DisplayName = "Pump Strength",
                    TabBadge = "MOVE",
                    BuildControls = (p) => FavsPage.BuildSliderOnly(p, "PumpStrength", "Pump Strength",
                        () => GameModifierMods.PumpStrengthLevel, () => GameModifierMods.PumpStrengthIncrease(), () => GameModifierMods.PumpStrengthDecrease(),
                        () => (GameModifierMods.PumpStrengthLevel - 1) / 9f, () => RefreshAll(),
                        () => GameModifierMods.DeltaDisplay(GameModifierMods.PumpStrengthLevel), () => GameModifierMods.PumpStrengthLevel != 5),
                    IsActive = () => GameModifierMods.PumpStrengthLevel != 5
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "GmWheelieBalance",
                    DisplayName = "Wheelie Balance",
                    TabBadge = "MOVE",
                    BuildControls = (p) => FavsPage.BuildSliderOnly(p, "GmWheelieBalance", "Wheelie Balance",
                        () => GameModifierMods.WheelieBalanceLevel, () => GameModifierMods.WheelieBalanceIncrease(), () => GameModifierMods.WheelieBalanceDecrease(),
                        () => (GameModifierMods.WheelieBalanceLevel - 1) / 9f, () => RefreshAll(),
                        () => GameModifierMods.DeltaDisplay(GameModifierMods.WheelieBalanceLevel), () => GameModifierMods.WheelieBalanceLevel != 5),
                    IsActive = () => GameModifierMods.WheelieBalanceLevel != 5
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "TweakSpeed",
                    DisplayName = "Tweak Speed",
                    TabBadge = "MOVE",
                    BuildControls = (p) => FavsPage.BuildSliderOnly(p, "TweakSpeed", "Tweak Speed",
                        () => GameModifierMods.TweakSpeedLevel, () => GameModifierMods.TweakSpeedIncrease(), () => GameModifierMods.TweakSpeedDecrease(),
                        () => (GameModifierMods.TweakSpeedLevel - 1) / 9f, () => RefreshAll(),
                        () => GameModifierMods.DeltaDisplay(GameModifierMods.TweakSpeedLevel), () => GameModifierMods.TweakSpeedLevel != 5),
                    IsActive = () => GameModifierMods.TweakSpeedLevel != 5
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "PedalWhileTweak",
                    DisplayName = "Pedal While Tweak",
                    TabBadge = "MOVE",
                    BuildControls = (p) => FavsPage.BuildToggleOnly(p, "PedalWhileTweak", "Pedal While Tweak",
                        () => PedalWhileTweak.Enabled, () => { PedalWhileTweak.Toggle(); RefreshAll(); }),
                    IsActive = () => PedalWhileTweak.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "NearMiss",
                    DisplayName = "Near Miss Sensitivity",
                    TabBadge = "MOVE",
                    BuildControls = (p) => FavsPage.BuildToggleSlider(p, "NearMiss", "Near Miss Sensitivity",
                        () => NearMissSensitivity.Enabled, () => NearMissSensitivity.Toggle(),
                        () => NearMissSensitivity.Level, () => NearMissSensitivity.Increase(), () => NearMissSensitivity.Decrease(),
                        10, () => (NearMissSensitivity.Level - 1) / 9f, () => RefreshAll(),
                        () => NearMissSensitivity.DisplayValue),
                    IsActive = () => NearMissSensitivity.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "CenterOfMass",
                    DisplayName = "Center of Mass",
                    TabBadge = "MOVE",
                    BuildControls = (p) => {
                        var r1 = FavsPage.CompactStatRow("Left / Right", p);
                        var b1 = UIHelpers.MakeBar("CLr", r1.transform, CenterOfMass.BarLR);
                        var v1 = UIHelpers.Txt("CLrV", r1.transform, CenterOfMass.DisplayLR, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                        v1.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                        UIHelpers.ActionBtn(r1.transform, "0", () => { CenterOfMass.ResetLR(); MovePage.RefreshAll(); FavsPage.RefreshFavourites(); }, 22);
                        UIHelpers.SmallBtn(r1.transform, "-", () => { CenterOfMass.DecreaseLR(); MovePage.RefreshAll(); FavsPage.RefreshFavourites(); });
                        UIHelpers.SmallBtn(r1.transform, "+", () => { CenterOfMass.IncreaseLR(); MovePage.RefreshAll(); FavsPage.RefreshFavourites(); });
                        var r2 = FavsPage.CompactStatRow("Forward / Back", p);
                        var b2 = UIHelpers.MakeBar("CFb", r2.transform, CenterOfMass.BarFB);
                        var v2 = UIHelpers.Txt("CFbV", r2.transform, CenterOfMass.DisplayFB, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                        v2.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                        UIHelpers.ActionBtn(r2.transform, "0", () => { CenterOfMass.ResetFB(); MovePage.RefreshAll(); FavsPage.RefreshFavourites(); }, 22);
                        UIHelpers.SmallBtn(r2.transform, "-", () => { CenterOfMass.DecreaseFB(); MovePage.RefreshAll(); FavsPage.RefreshFavourites(); });
                        UIHelpers.SmallBtn(r2.transform, "+", () => { CenterOfMass.IncreaseFB(); MovePage.RefreshAll(); FavsPage.RefreshFavourites(); });
                        var r3 = FavsPage.CompactStatRow("Up / Down", p);
                        var b3 = UIHelpers.MakeBar("CUd", r3.transform, CenterOfMass.BarUD);
                        var v3 = UIHelpers.Txt("CUdV", r3.transform, CenterOfMass.DisplayUD, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                        v3.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                        UIHelpers.ActionBtn(r3.transform, "0", () => { CenterOfMass.ResetUD(); MovePage.RefreshAll(); FavsPage.RefreshFavourites(); }, 22);
                        UIHelpers.SmallBtn(r3.transform, "-", () => { CenterOfMass.DecreaseUD(); MovePage.RefreshAll(); FavsPage.RefreshFavourites(); });
                        UIHelpers.SmallBtn(r3.transform, "+", () => { CenterOfMass.IncreaseUD(); MovePage.RefreshAll(); FavsPage.RefreshFavourites(); });
                        FavouritesManager.RegisterRefresh("CenterOfMass", () => {
                            UIHelpers.SetBar(b1, CenterOfMass.BarLR); if (v1) v1.text = CenterOfMass.DisplayLR;
                            UIHelpers.SetBar(b2, CenterOfMass.BarFB); if (v2) v2.text = CenterOfMass.DisplayFB;
                            UIHelpers.SetBar(b3, CenterOfMass.BarUD); if (v3) v3.text = CenterOfMass.DisplayUD;
                        });
                    },
                    IsActive = () => CenterOfMass.OffsetLR != 0f || CenterOfMass.OffsetFB != 0f || CenterOfMass.OffsetUD != 0f
                });

                UIHelpers.AddScrollForwarders(pg6);
            }
            catch (System.Exception ex) { MelonLogger.Error("MovePage.CreatePage: " + ex.Message); Telemetry.ReportErrorAsync(ex, "MovePage"); return null; }
            return pg;
        }

        private static void ResetMoveTab()
        {
            if (Movement.SpinEnabled) Movement.ToggleSpin();
            if (Movement.WheelieEnabled) Movement.ToggleWheelie();
            if (Movement.LeanEnabled) Movement.ToggleLean();
            Movement.SetSpinLevel(1);
            Movement.SetWheelieLevel(1); Movement.SetLeanLevel(1);
            if (WheelieAngleLimit.Enabled) WheelieAngleLimit.Toggle();
            WheelieAngleLimit.SetLevel(5);
            if (WheelieHUD.Enabled) WheelieHUD.Toggle();
            GameModifierMods.SetPumpStrengthLevel(5);
            GameModifierMods.SetWheelieBalanceLevel(5);
            GameModifierMods.SetTweakSpeedLevel(5);
            if (NearMissSensitivity.Enabled) NearMissSensitivity.Toggle();
            NearMissSensitivity.SetLevel(5);
            CenterOfMass.SetLR(0f); CenterOfMass.SetFB(0f); CenterOfMass.SetUD(0f);
        }

        public static void RefreshAll()
        {
            // ── Rotation Speed ────────────────────────────────────────
            bool spOn = Movement.SpinEnabled;
            if (spinTogVal) { spinTogVal.text = spOn ? "ON" : "OFF"; spinTogVal.color = spOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(spinTrack, spinKnob, spOn);
            if (spinVal) spinVal.text = Movement.SpinLevel.ToString();
            UIHelpers.SetBar(spinBar, (Movement.SpinLevel - 1) / (float)(Movement.MaxLevel - 1));

            // ── Wheelie Force ─────────────────────────────────────────
            bool wlOn = Movement.WheelieEnabled;
            if (wheelieTogVal) { wheelieTogVal.text = wlOn ? "ON" : "OFF"; wheelieTogVal.color = wlOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(wheelieTrack, wheelieKnob, wlOn);
            if (wheelieVal) wheelieVal.text = Movement.WheelieLevel.ToString();
            UIHelpers.SetBar(wheelieBar, (Movement.WheelieLevel - 1) / (float)(Movement.MaxLevel - 1));

            // ── Lean Strength ─────────────────────────────────────────
            bool lnOn = Movement.LeanEnabled;
            if (leanTogVal) { leanTogVal.text = lnOn ? "ON" : "OFF"; leanTogVal.color = lnOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(leanTrack, leanKnob, lnOn);
            if (leanVal) leanVal.text = Movement.LeanLevel.ToString();
            UIHelpers.SetBar(leanBar, (Movement.LeanLevel - 1) / (float)(Movement.MaxLevel - 1));

            // ── Wheelie Angle Limit ───────────────────────────────────
            if (wbVal) wbVal.text = WheelieAngleLimit.DisplayValue;
            if (_wbTogVal) { _wbTogVal.text = WheelieAngleLimit.Enabled ? "ON" : "OFF"; _wbTogVal.color = WheelieAngleLimit.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_wbTrack, _wbKnob, WheelieAngleLimit.Enabled);
            UIHelpers.SetBar(wbBar, (WheelieAngleLimit.Level - 1) / 9f);

            // ── Wheelie HUD ───────────────────────────────────────────
            if (_whTogVal) { _whTogVal.text = WheelieHUD.Enabled ? "ON" : "OFF"; _whTogVal.color = WheelieHUD.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_whTrack, _whKnob, WheelieHUD.Enabled);

            // ── Pump Strength ─────────────────────────────────────────
            if (psVal) psVal.text = GameModifierMods.DeltaDisplay(GameModifierMods.PumpStrengthLevel);
            UIHelpers.SetBar(psBar, (GameModifierMods.PumpStrengthLevel - 1) / 9f);

            if (_gmWbVal) _gmWbVal.text = GameModifierMods.DeltaDisplay(GameModifierMods.WheelieBalanceLevel);
            UIHelpers.SetBar(_gmWbBar, (GameModifierMods.WheelieBalanceLevel - 1) / 9f);

            if (_gmTsVal) _gmTsVal.text = GameModifierMods.DeltaDisplay(GameModifierMods.TweakSpeedLevel);
            UIHelpers.SetBar(_gmTsBar, (GameModifierMods.TweakSpeedLevel - 1) / 9f);

            bool pwtOn = PedalWhileTweak.Enabled;
            if (_pwtTogVal) { _pwtTogVal.text = pwtOn ? "ON" : "OFF"; _pwtTogVal.color = pwtOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_pwtTrack, _pwtKnob, pwtOn);

            // ── Center of Mass ────────────────────────────────────────
            if (_comLRVal) _comLRVal.text = CenterOfMass.DisplayLR;
            if (_comFBVal) _comFBVal.text = CenterOfMass.DisplayFB;
            if (_comUDVal) _comUDVal.text = CenterOfMass.DisplayUD;
            UIHelpers.SetBar(_comLRBar, CenterOfMass.BarLR);
            UIHelpers.SetBar(_comFBBar, CenterOfMass.BarFB);
            UIHelpers.SetBar(_comUDBar, CenterOfMass.BarUD);

            bool nmOn = NearMissSensitivity.Enabled;
            if (_nmTogVal) { _nmTogVal.text = nmOn ? "ON" : "OFF"; _nmTogVal.color = nmOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_nmTrack, _nmKnob, nmOn);
            if (_nmVal) _nmVal.text = NearMissSensitivity.DisplayValue;
            UIHelpers.SetBar(_nmBar, (NearMissSensitivity.Level - 1) / 9f);
        }
    }
}

