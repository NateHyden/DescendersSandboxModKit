using DescendersModMenu.Mods;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class SessionPage
    {
        private static Text _sessionTimeVal;
        private static Text _topSpeedVal;
        private static Text _bailCountVal;
        private static Text _checkpointCountVal;
        private static Text _airtimeVal;
        private static Text _gforceVal;
        private static Text _peakGforceVal;
        private static Text _srtVal;
        private static Image _srtTrack;
        private static RectTransform _srtKnob;
        private static Text _hudTogVal;
        private static Image _hudTrack;
        private static RectTransform _hudKnob;
        private static Text _specTogVal, _specTargetVal, _specDistVal;
        private static Image _specTrack;
        private static RectTransform _specKnob;
        private static Text _fovVal, _fovTogVal;
        private static Image _fovBar, _fovTrack;
        private static RectTransform _fovKnob;
        private static Text _slowVal, _slowSpeedVal;
        private static Image _slowSpeedBar, _slowTrack;
        private static RectTransform _slowKnob;
        private static Text _smobVal;
        private static Image _smobTrack;
        private static RectTransform _smobKnob;
        private static Text _blackDeathVal;
        private static Image _blackDeathTrack;
        private static RectTransform _blackDeathKnob;
        private static Text _compassVal;
        private static Image _compassTrack;
        private static RectTransform _compassKnob;
        private static Text _tmLabelVal;
        private static UnityEngine.UI.Button _tmMinus, _tmPlus;
        private static Text _jumpStatusTxt;

        public static bool IsAnyActive =>
            SpeedrunTimer.Enabled || SessionHUD.Enabled || SpectateMode.Enabled ||
            FOV.Enabled || SlowMotion.Enabled || SlowMoOnBail.Enabled ||
            BlackDeath.Enabled || CompassAlwaysOn.Enabled || TrickMultiplier.Enabled;

        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("P16R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));

                var scrollObj = UIHelpers.Obj("Scroll", pg.transform);
                UIHelpers.Fill(UIHelpers.RT(scrollObj));
                var scrollRect = scrollObj.AddComponent<ScrollRect>();
                scrollRect.horizontal = false; scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 25f; scrollRect.inertia = false;

                var vp = UIHelpers.Obj("VP", scrollObj.transform);
                UIHelpers.Fill(UIHelpers.RT(vp));
                vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                vp.AddComponent<Mask>().showMaskGraphic = true;
                scrollRect.viewport = UIHelpers.RT(vp);

                var content = UIHelpers.Obj("Content", vp.transform);
                var crt = UIHelpers.RT(content);
                crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 0);
                scrollRect.content = crt;
                content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

                var c = content.transform;

                UIHelpers.SectionHeader("ON-SCREEN HUD", c);
                var hudr = UIHelpers.StatRow("Show HUD", c);
                _hudTogVal = UIHelpers.Txt("HdV", hudr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _hudTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(hudr.transform, "HdT", () =>
                {
                    SessionHUD.Toggle();
                    RefreshAll();
                }, out _hudTrack, out _hudKnob);
                UIHelpers.InfoBox(c, "Displays session stats in the top-right corner while riding.");

                var compassRow = UIHelpers.StatRow("Show Compass", c);
                _compassVal = UIHelpers.Txt("CmpV", compassRow.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _compassVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(compassRow.transform, "CmpT", () => { CompassAlwaysOn.Toggle(); MenuWindow.RefreshAll(); }, out _compassTrack, out _compassKnob);
                UIHelpers.InfoBox(c, "Only points to something in Bike Park or Career.");

                UIHelpers.Divider(c);

                UIHelpers.SectionHeader("CAMERA & BAIL", c);
                var fr = UIHelpers.StatRow("FOV", c);
                _fovTogVal = UIHelpers.Txt("FTV", fr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _fovTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(fr.transform, "FT", () => { FOV.Toggle(); MenuWindow.RefreshAll(); }, out _fovTrack, out _fovKnob);
                _fovBar = UIHelpers.MakeBar("FB", fr.transform, (FOV.Level - 1) / 9f);
                _fovVal = UIHelpers.Txt("FV", fr.transform, FOV.DisplayValue, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _fovVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 26;
                UIHelpers.SmallBtn(fr.transform, "-", () => { FOV.Decrease(); MenuWindow.RefreshAll(); });
                UIHelpers.SmallBtn(fr.transform, "+", () => { FOV.Increase(); MenuWindow.RefreshAll(); });

                var smr = UIHelpers.StatRow("Slow Motion", c);
                _slowVal = UIHelpers.Txt("SMV", smr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _slowVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(smr.transform, "SMT", () => { SlowMotion.Toggle(); MenuWindow.RefreshAll(); }, out _slowTrack, out _slowKnob);
                _slowSpeedBar = UIHelpers.MakeBar("SmSB", smr.transform, (SlowMotion.Level - 1) / 8f);
                _slowSpeedVal = UIHelpers.Txt("SmSV", smr.transform, SlowMotion.DisplayValue, 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _slowSpeedVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.SmallBtn(smr.transform, "-", () => { SlowMotion.Decrease(); MenuWindow.RefreshAll(); });
                UIHelpers.SmallBtn(smr.transform, "+", () => { SlowMotion.Increase(); MenuWindow.RefreshAll(); });
                var smHint = UIHelpers.Txt("SMH", smr.transform, "F2", 10, FontStyle.Normal, TextAnchor.MiddleRight, UIHelpers.TextDim);
                smHint.gameObject.AddComponent<LayoutElement>().preferredWidth = 22;

                var smobr = UIHelpers.StatRow("Slow Mo on Bail", c);
                _smobVal = UIHelpers.Txt("SbV", smobr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _smobVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(smobr.transform, "SbT", () => { SlowMoOnBail.Toggle(); MenuWindow.RefreshAll(); }, out _smobTrack, out _smobKnob);

                var bdr = UIHelpers.StatRow("Black Death", c);
                _blackDeathVal = UIHelpers.Txt("BdV", bdr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _blackDeathVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(bdr.transform, "BdT", () => { BlackDeath.Toggle(); MenuWindow.RefreshAll(); }, out _blackDeathTrack, out _blackDeathKnob);
                UIHelpers.InfoBox(c, "Screen goes black when you bail. Press B / respawn to come back.");

                UIHelpers.Divider(c);

                UIHelpers.SectionHeader("SPECTATE MODE", c);
                var specR = UIHelpers.StatRow("Spectate", c);
                _specTogVal = UIHelpers.Txt("SpcTV", specR.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _specTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(specR.transform, "SpcT", () => { SpectateMode.Toggle(); RefreshAll(); }, out _specTrack, out _specKnob);

                var specTargetR = UIHelpers.StatRow("Watching", c);
                _specTargetVal = UIHelpers.Txt("SpcTgV", specTargetR.transform, SpectateMode.StatusDisplay, 12,
                    FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
                _specTargetVal.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                UIHelpers.SmallBtn(specTargetR.transform, "<", () => { SpectateMode.Previous(); RefreshAll(); });
                UIHelpers.SmallBtn(specTargetR.transform, ">", () => { SpectateMode.Next(); RefreshAll(); });

                var specDistR = UIHelpers.StatRow("Camera Distance", c);
                _specDistVal = UIHelpers.Txt("SpcDV", specDistR.transform, SpectateMode.Distance.ToString("0") + "m", 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _specDistVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                UIHelpers.SmallBtn(specDistR.transform, "-", () => { SpectateMode.DecreaseDistance(); RefreshAll(); });
                UIHelpers.SmallBtn(specDistR.transform, "+", () => { SpectateMode.IncreaseDistance(); RefreshAll(); });
                UIHelpers.InfoBox(c, "Chase-cams another connected player (transform follow â€” doesn't touch their physics). Multiplayer only. Locks your controls while active.");

                UIHelpers.Divider(c);

                var sessionHdr = UIHelpers.Obj("SessionHdr", c);
                var shLE = sessionHdr.AddComponent<LayoutElement>();
                shLE.preferredHeight = 28; shLE.minHeight = 28; shLE.flexibleHeight = 0;
                var shHlg = sessionHdr.AddComponent<HorizontalLayoutGroup>();
                shHlg.spacing = 8;
                shHlg.padding = new RectOffset(0, 8, 0, 0);
                shHlg.childAlignment = TextAnchor.MiddleLeft;
                shHlg.childForceExpandWidth = false;
                shHlg.childForceExpandHeight = true;
                var shBar = UIHelpers.Panel("SHBar", sessionHdr.transform, UIHelpers.Accent);
                shBar.AddComponent<LayoutElement>().preferredWidth = 3;
                var shTxt = UIHelpers.Txt("SHT", sessionHdr.transform, "SESSION", 11,
                    FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.Accent);
                shTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                UIHelpers.ActionBtn(sessionHdr.transform, "Reset All", () =>
                {
                    TopSpeed.ResetSession();
                    SessionTrackers.ResetBails();
                    SessionTrackers.ResetCheckpoints();
                    SessionTrackers.ResetAirtime();
                    SessionTrackers.ResetGForce();
                    RefreshAll();
                }, 68);

                var str = UIHelpers.StatRow("Session Timer", c);
                _sessionTimeVal = UIHelpers.Txt("StV", str.transform, SessionTrackers.SessionTimeDisplay,
                    12, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
                _sessionTimeVal.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                var tsr = UIHelpers.StatRow("Top Speed", c);
                _topSpeedVal = UIHelpers.Txt("TSV", tsr.transform, TopSpeed.DisplayValue,
                    12, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
                _topSpeedVal.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                UIHelpers.ActionBtn(tsr.transform, "Reset", () => { TopSpeed.ResetSession(); RefreshAll(); }, 52);

                var srtr = UIHelpers.StatRow("Speedrun Timer", c);
                _srtVal = UIHelpers.Txt("SrV", srtr.transform, "OFF", 11, FontStyle.Bold,
                    TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _srtVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(srtr.transform, "SrT", () => { SpeedrunTimer.Toggle(); RefreshAll(); },
                    out _srtTrack, out _srtKnob);
                UIHelpers.ActionBtn(srtr.transform, "Reset", () => { SpeedrunTimer.ResetTime(); RefreshAll(); }, 52);
                UIHelpers.InfoBox(c, "Requires Speedrun Timer ON in Settings > Gameplay.");

                var tmr = UIHelpers.StatRow("Trick Multiplier", c);
                _tmMinus = UIHelpers.SmallBtn(tmr.transform, "\u25C0", () => { TrickMultiplier.Decrease(); MenuWindow.RefreshAll(); });
                _tmLabelVal = UIHelpers.Txt("TmLV", tmr.transform, TrickMultiplier.LevelDisplay, 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _tmLabelVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 64;
                _tmPlus = UIHelpers.SmallBtn(tmr.transform, "\u25B6", () => { TrickMultiplier.Increase(); MenuWindow.RefreshAll(); });
                UIHelpers.InfoBox(c, "Raises the max combo multiplier cap above the game's default x3.");

                var bcr = UIHelpers.StatRow("Bails", c);
                _bailCountVal = UIHelpers.Txt("BcV", bcr.transform, SessionTrackers.BailCountDisplay,
                    12, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
                _bailCountVal.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                UIHelpers.ActionBtn(bcr.transform, "Reset", () => { SessionTrackers.ResetBails(); RefreshAll(); }, 52);

                var cpcr = UIHelpers.StatRow("Checkpoints", c);
                _checkpointCountVal = UIHelpers.Txt("CpCV", cpcr.transform,
                    SessionTrackers.CheckpointCountDisplay, 12, FontStyle.Bold,
                    TextAnchor.MiddleRight, UIHelpers.Accent);
                _checkpointCountVal.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                UIHelpers.ActionBtn(cpcr.transform, "Reset", () => { SessionTrackers.ResetCheckpoints(); RefreshAll(); });

                var atr = UIHelpers.StatRow("Longest Airtime", c);
                _airtimeVal = UIHelpers.Txt("AtV", atr.transform, SessionTrackers.AirtimeDisplay,
                    12, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
                _airtimeVal.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                UIHelpers.ActionBtn(atr.transform, "Reset", () => { SessionTrackers.ResetAirtime(); RefreshAll(); }, 52);

                var gfr = UIHelpers.StatRow("G-Force", c);
                _gforceVal = UIHelpers.Txt("GfV", gfr.transform, SessionTrackers.GForceDisplay,
                    12, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
                _gforceVal.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                var pgfr = UIHelpers.StatRow("Peak G-Force", c);
                _peakGforceVal = UIHelpers.Txt("PgV", pgfr.transform, SessionTrackers.PeakGForceDisplay,
                    12, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
                _peakGforceVal.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                UIHelpers.ActionBtn(pgfr.transform, "Reset", () => { SessionTrackers.ResetGForce(); RefreshAll(); }, 52);

                UIHelpers.Divider(c);

                UIHelpers.SectionHeader("RUN", c);

                var jr = UIHelpers.StatRow("Jump to Finish", c);
                _jumpStatusTxt = UIHelpers.Txt("JumpStatus", jr.transform, "",
                    9, FontStyle.Italic, TextAnchor.MiddleRight, UIHelpers.TextDim);
                _jumpStatusTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 130;
                UIHelpers.ActionBtnOrange(jr.transform, "Jump", () =>
                {
                    try
                    {
                        var fl = FinishLine.GetAFinishLine();
                        if (!UnityNull.Alive(fl))
                        {
                            ModLog.Debug("[JumpToFinish] No FinishLine found on this level - nothing to jump to.");
                            if (_jumpStatusTxt) { _jumpStatusTxt.text = "No finish line here"; _jumpStatusTxt.color = UIHelpers.Orange; }
                            return;
                        }

                        DevCommandsGameplay.JumpToFinish();
                        if (_jumpStatusTxt) _jumpStatusTxt.text = "";
                    }
                    catch (System.Exception ex)
                    {
                        MelonLogger.Error("[JumpToFinish]: " + ex.Message);
                        Telemetry.ReportErrorAsync(ex, "SessionPage");
                        if (_jumpStatusTxt) { _jumpStatusTxt.text = "Failed - see log"; _jumpStatusTxt.color = UIHelpers.Orange; }
                    }
                }, 60);
                FavouritesManager.RegisterStarButton("JumpToFinish", UIHelpers.StarBtn(jr.transform, "JumpToFinish", () => FavouritesManager.Toggle("JumpToFinish")));

                var skipRow = UIHelpers.StatRow("Skip Song", c);
                UIHelpers.ActionBtn(skipRow.transform, "Skip", () =>
                {
                    try { DevCommandsGameplay.SkipSong(); }
                    catch (System.Exception ex) { MelonLogger.Error("[SkipSong]: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SessionPage"); }
                }, 60);
                FavouritesManager.RegisterStarButton("SkipSong", UIHelpers.StarBtn(skipRow.transform, "SkipSong", () => FavouritesManager.Toggle("SkipSong")));

                FavouritesManager.RegisterStarButton("ShowHUD", UIHelpers.StarBtn(hudr.transform, "ShowHUD", () => FavouritesManager.Toggle("ShowHUD")));
                FavouritesManager.RegisterStarButton("SpeedrunTimer", UIHelpers.StarBtn(srtr.transform, "SpeedrunTimer", () => FavouritesManager.Toggle("SpeedrunTimer")));
                FavouritesManager.RegisterStarButton("SpectateMode", UIHelpers.StarBtn(specR.transform, "SpectateMode", () => FavouritesManager.Toggle("SpectateMode")));
                FavouritesManager.RegisterStarButton("FOV", UIHelpers.StarBtn(fr.transform, "FOV", () => FavouritesManager.Toggle("FOV")));
                FavouritesManager.RegisterStarButton("SlowMotion", UIHelpers.StarBtn(smr.transform, "SlowMotion", () => FavouritesManager.Toggle("SlowMotion")));
                FavouritesManager.RegisterStarButton("SlowMoOnBail", UIHelpers.StarBtn(smobr.transform, "SlowMoOnBail", () => FavouritesManager.Toggle("SlowMoOnBail")));
                FavouritesManager.RegisterStarButton("BlackDeath", UIHelpers.StarBtn(bdr.transform, "BlackDeath", () => FavouritesManager.Toggle("BlackDeath")));
                FavouritesManager.RegisterStarButton("CompassAlwaysOn", UIHelpers.StarBtn(compassRow.transform, "CompassAlwaysOn", () => FavouritesManager.Toggle("CompassAlwaysOn")));
                FavouritesManager.RegisterStarButton("TrickMultiplier", UIHelpers.StarBtn(tmr.transform, "TrickMultiplier", () => FavouritesManager.Toggle("TrickMultiplier")));

                FavouritesManager.Register(new ModFavEntry {
                    Id = "ShowHUD", DisplayName = "Show HUD", TabBadge = "SESSION",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "ShowHUD", "Show HUD",
                        () => SessionHUD.Enabled, () => { SessionHUD.Toggle(); }, () => RefreshAll()),
                    IsActive = () => SessionHUD.Enabled
                });
                FavouritesManager.Register(new ModFavEntry {
                    Id = "SpeedrunTimer", DisplayName = "Speedrun Timer", TabBadge = "SESSION",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "SpeedrunTimer", "Speedrun Timer",
                        () => SpeedrunTimer.Enabled, () => SpeedrunTimer.Toggle(), () => RefreshAll()),
                    IsActive = () => SpeedrunTimer.Enabled
                });
                FavouritesManager.Register(new ModFavEntry {
                    Id = "SpectateMode", DisplayName = "Spectate Mode", TabBadge = "SESSION",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "SpectateMode", "Spectate Mode",
                        () => SpectateMode.Enabled, () => SpectateMode.Toggle(), () => RefreshAll()),
                    IsActive = () => SpectateMode.Enabled
                });
                FavouritesManager.Register(new ModFavEntry {
                    Id = "FOV", DisplayName = "FOV", TabBadge = "SESSION",
                    BuildControls = (p) => FavsPage.BuildToggleSlider(p, "FOV", "FOV",
                        () => FOV.Enabled, () => FOV.Toggle(),
                        () => FOV.Level, () => FOV.Increase(), () => FOV.Decrease(),
                        10, () => (FOV.Level - 1) / 9f, () => MenuWindow.RefreshAll(),
                        () => FOV.DisplayValue),
                    IsActive = () => FOV.Enabled
                });
                FavouritesManager.Register(new ModFavEntry {
                    Id = "SlowMotion", DisplayName = "Slow Motion", TabBadge = "SESSION",
                    BuildControls = (p) => FavsPage.BuildToggleSlider(p, "SlowMotion", "Slow Motion",
                        () => SlowMotion.Enabled, () => SlowMotion.Toggle(),
                        () => SlowMotion.Level, () => SlowMotion.Increase(), () => SlowMotion.Decrease(),
                        9, () => (SlowMotion.Level - 1) / 8f, () => MenuWindow.RefreshAll(),
                        () => SlowMotion.DisplayValue),
                    IsActive = () => SlowMotion.Enabled
                });
                FavouritesManager.Register(new ModFavEntry {
                    Id = "SlowMoOnBail", DisplayName = "Slow Mo On Bail", TabBadge = "SESSION",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "SlowMoOnBail", "Slow Mo On Bail",
                        () => SlowMoOnBail.Enabled, () => SlowMoOnBail.Toggle(), () => MenuWindow.RefreshAll()),
                    IsActive = () => SlowMoOnBail.Enabled
                });
                FavouritesManager.Register(new ModFavEntry {
                    Id = "BlackDeath", DisplayName = "Black Death", TabBadge = "SESSION",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "BlackDeath", "Black Death",
                        () => BlackDeath.Enabled, () => BlackDeath.Toggle(), () => MenuWindow.RefreshAll()),
                    IsActive = () => BlackDeath.Enabled
                });
                FavouritesManager.Register(new ModFavEntry {
                    Id = "CompassAlwaysOn", DisplayName = "Show Compass", TabBadge = "SESSION",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "CompassAlwaysOn", "Show Compass",
                        () => CompassAlwaysOn.Enabled, () => CompassAlwaysOn.Toggle(), () => MenuWindow.RefreshAll()),
                    IsActive = () => CompassAlwaysOn.Enabled
                });
                FavouritesManager.Register(new ModFavEntry {
                    Id = "TrickMultiplier", DisplayName = "Trick Multiplier", TabBadge = "SESSION",
                    BuildControls = (p) => FavsPage.BuildStepper(p, "TrickMultiplier", "Trick Multiplier",
                        () => TrickMultiplier.Level,
                        () => TrickMultiplier.Decrease(),
                        () => TrickMultiplier.Increase(),
                        0, 3, () => MenuWindow.RefreshAll(), 0),
                    IsActive = () => TrickMultiplier.Enabled
                });
                FavouritesManager.Register(new ModFavEntry {
                    Id = "JumpToFinish",
                    DisplayName = "Jump to Finish",
                    TabBadge = "SESSION",
                    BuildControls = (p) =>
                    {
                        var row = FavsPage.CompactStatRow("Jump to Finish", p);
                        UIHelpers.ActionBtnOrange(row.transform, "Jump", () =>
                        {
                            try
                            {
                                var fl = FinishLine.GetAFinishLine();
                                if (!UnityNull.Alive(fl)) return;
                                DevCommandsGameplay.JumpToFinish();
                            }
                            catch (System.Exception ex) { MelonLogger.Error("[JumpToFinish]: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SessionPage"); }
                        }, 60);
                    },
                    IsActive = () => false
                });
                FavouritesManager.Register(new ModFavEntry {
                    Id = "SkipSong",
                    DisplayName = "Skip Song",
                    TabBadge = "SESSION",
                    BuildControls = (p) =>
                    {
                        var row = FavsPage.CompactStatRow("Skip Song", p);
                        UIHelpers.ActionBtn(row.transform, "Skip", () =>
                        {
                            try { DevCommandsGameplay.SkipSong(); }
                            catch (System.Exception ex) { MelonLogger.Error("[SkipSong]: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SessionPage"); }
                        }, 60);
                    },
                    IsActive = () => false
                });

                UIHelpers.AddScrollForwarders(c);
            }
            catch (System.Exception ex) { MelonLogger.Error("SessionPage.CreatePage: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SessionPage"); return null; }
            return pg;
        }

        public static void RefreshAll()
        {
            if (_hudTogVal) { _hudTogVal.text = SessionHUD.Enabled ? "ON" : "OFF"; _hudTogVal.color = SessionHUD.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_hudTrack, _hudKnob, SessionHUD.Enabled);

            if (_topSpeedVal) _topSpeedVal.text = TopSpeed.DisplayValue;
            if (_sessionTimeVal) _sessionTimeVal.text = SessionTrackers.SessionTimeDisplay;
            if (_bailCountVal) _bailCountVal.text = SessionTrackers.BailCountDisplay;
            if (_checkpointCountVal) _checkpointCountVal.text = SessionTrackers.CheckpointCountDisplay;
            if (_airtimeVal) _airtimeVal.text = SessionTrackers.AirtimeDisplay;
            if (_gforceVal) _gforceVal.text = SessionTrackers.GForceDisplay;
            if (_peakGforceVal) _peakGforceVal.text = SessionTrackers.PeakGForceDisplay;

            bool srt = SpeedrunTimer.Enabled;
            if (_srtVal) { _srtVal.text = srt ? "ON" : "OFF"; _srtVal.color = srt ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_srtTrack, _srtKnob, srt);

            bool spec = SpectateMode.Enabled;
            if (_specTogVal) { _specTogVal.text = spec ? "ON" : "OFF"; _specTogVal.color = spec ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_specTrack, _specKnob, spec);
            if (_specTargetVal) _specTargetVal.text = SpectateMode.StatusDisplay;
            if (_specDistVal) _specDistVal.text = SpectateMode.Distance.ToString("0") + "m";

            bool fovOn = FOV.Enabled;
            if (_fovTogVal) { _fovTogVal.text = fovOn ? "ON" : "OFF"; _fovTogVal.color = fovOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_fovTrack, _fovKnob, fovOn);
            if (_fovVal) _fovVal.text = FOV.DisplayValue;
            UIHelpers.SetBar(_fovBar, (FOV.Level - 1) / 9f);

            bool slow = SlowMotion.Enabled;
            if (_slowVal) { _slowVal.text = slow ? "ON" : "OFF"; _slowVal.color = slow ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_slowTrack, _slowKnob, slow);
            if (_slowSpeedVal) _slowSpeedVal.text = SlowMotion.DisplayValue;
            UIHelpers.SetBar(_slowSpeedBar, (SlowMotion.Level - 1) / 8f);

            bool smob = SlowMoOnBail.Enabled;
            if (_smobVal) { _smobVal.text = smob ? "ON" : "OFF"; _smobVal.color = smob ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_smobTrack, _smobKnob, smob);

            bool bdth = BlackDeath.Enabled;
            if (_blackDeathVal) { _blackDeathVal.text = bdth ? "ON" : "OFF"; _blackDeathVal.color = bdth ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_blackDeathTrack, _blackDeathKnob, bdth);

            bool compass = CompassAlwaysOn.Enabled;
            if (_compassVal) { _compassVal.text = compass ? "ON" : "OFF"; _compassVal.color = compass ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_compassTrack, _compassKnob, compass);

            if (_tmLabelVal) _tmLabelVal.text = TrickMultiplier.LevelDisplay;
            if ((object)_tmMinus != null && _tmMinus) _tmMinus.interactable = TrickMultiplier.Level > 0;
            if ((object)_tmPlus != null && _tmPlus) _tmPlus.interactable = TrickMultiplier.Level < 3;
        }

        public static void ClearUiRefs()
        {
            _sessionTimeVal = null;
            _topSpeedVal = null;
            _bailCountVal = null;
            _checkpointCountVal = null;
            _airtimeVal = null;
            _gforceVal = null;
            _peakGforceVal = null;
            _srtVal = null;
            _srtTrack = null;
            _srtKnob = null;
            _hudTogVal = null;
            _hudTrack = null;
            _hudKnob = null;
            _specTogVal = null;
            _specTargetVal = null;
            _specDistVal = null;
            _specTrack = null;
            _specKnob = null;
            _fovVal = null;
            _fovTogVal = null;
            _fovBar = null;
            _fovTrack = null;
            _fovKnob = null;
            _slowVal = null;
            _slowSpeedVal = null;
            _slowSpeedBar = null;
            _slowTrack = null;
            _slowKnob = null;
            _smobVal = null;
            _smobTrack = null;
            _smobKnob = null;
            _blackDeathVal = null;
            _blackDeathTrack = null;
            _blackDeathKnob = null;
            _compassVal = null;
            _compassTrack = null;
            _compassKnob = null;
            _tmLabelVal = null;
            _tmMinus = null;
            _tmPlus = null;
            _jumpStatusTxt = null;
        }

        public static void TickLive()
        {
            if (_topSpeedVal) _topSpeedVal.text = TopSpeed.DisplayValue;
            if (_sessionTimeVal) _sessionTimeVal.text = SessionTrackers.SessionTimeDisplay;
            if (_bailCountVal) _bailCountVal.text = SessionTrackers.BailCountDisplay;
            if (_checkpointCountVal) _checkpointCountVal.text = SessionTrackers.CheckpointCountDisplay;
            if (_airtimeVal) _airtimeVal.text = SessionTrackers.AirtimeDisplay;
            if (_gforceVal) _gforceVal.text = SessionTrackers.GForceDisplay;
            if (_peakGforceVal) _peakGforceVal.text = SessionTrackers.PeakGForceDisplay;
        }
    }
}

