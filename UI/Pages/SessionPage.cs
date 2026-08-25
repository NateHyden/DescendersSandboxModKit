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
        private static Text _specTogVal, _specTargetVal, _specDistVal;
        private static Image _specTrack;
        private static RectTransform _specKnob;
        private static Text _blackDeathVal;
        private static Image _blackDeathTrack;
        private static RectTransform _blackDeathKnob;
        private static Text _tmLabelVal;
        private static UnityEngine.UI.Button _tmMinus, _tmPlus;
        private static Text _jumpStatusTxt;
        private static Text _respawnStartStatusTxt;

        public static bool IsAnyActive =>
            SpeedrunTimer.Enabled || SpectateMode.Enabled ||
            BlackDeath.Enabled || TrickMultiplier.Enabled;

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

                UIHelpers.SectionHeader("BAIL", c);
                var bdr = UIHelpers.StatRow("Black Death", c);
                _blackDeathVal = UIHelpers.Txt("BdV", bdr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _blackDeathVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(bdr.transform, "BdT", () => { BlackDeath.Toggle(); MenuWindow.RefreshAll(); }, out _blackDeathTrack, out _blackDeathKnob);
                UIHelpers.InfoBox(c, "Screen goes black when you crash. Press B or respawn to come back.");

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
                UIHelpers.InfoBox(c, "Follow another player with the camera. Multiplayer only. Locks your controls while on.");

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
                UIHelpers.InfoBox(c, "Needs the game's Speedrun Timer turned on in Settings > Gameplay.");

                var tmr = UIHelpers.StatRow("Trick Multiplier", c);
                _tmMinus = UIHelpers.SmallBtn(tmr.transform, "\u25C0", () => { TrickMultiplier.Decrease(); MenuWindow.RefreshAll(); });
                _tmLabelVal = UIHelpers.Txt("TmLV", tmr.transform, TrickMultiplier.LevelDisplay, 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _tmLabelVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 64;
                _tmPlus = UIHelpers.SmallBtn(tmr.transform, "\u25B6", () => { TrickMultiplier.Increase(); MenuWindow.RefreshAll(); });
                UIHelpers.InfoBox(c, "Lets your combo multiplier go higher than the normal x3.");

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
                    string err;
                    if (!SessionCommands.TryJumpToFinish(out err))
                    {
                        if (_jumpStatusTxt)
                        {
                            _jumpStatusTxt.text = string.IsNullOrEmpty(err) ? "Failed" : err;
                            _jumpStatusTxt.color = UIHelpers.Orange;
                        }
                        return;
                    }
                    if (_jumpStatusTxt) _jumpStatusTxt.text = "";
                }, 60);
                FavouritesManager.RegisterStarButton("JumpToFinish", UIHelpers.StarBtn(jr.transform, "JumpToFinish", () => FavouritesManager.Toggle("JumpToFinish")));

                var respawnStartRow = UIHelpers.StatRow("Respawn at Start", c);
                _respawnStartStatusTxt = UIHelpers.Txt("RspStartStatus", respawnStartRow.transform, "",
                    9, FontStyle.Italic, TextAnchor.MiddleRight, UIHelpers.TextDim);
                _respawnStartStatusTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 130;
                UIHelpers.ActionBtnOrange(respawnStartRow.transform, "Go", () =>
                {
                    try
                    {
                        PlayerManager pm = UnityEngine.Object.FindObjectOfType<PlayerManager>();
                        if ((object)pm == null)
                        {
                            if (_respawnStartStatusTxt) { _respawnStartStatusTxt.text = "Not in a session"; _respawnStartStatusTxt.color = UIHelpers.Orange; }
                            return;
                        }
                        var getPii = typeof(PlayerManager).GetMethod("GetPlayerImpact",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if ((object)getPii == null) return;
                        object pii = getPii.Invoke(pm, null);
                        if ((object)pii == null) return;
                        var respawn = pii.GetType().GetMethod("RespawnAtStartLine",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                            null, new System.Type[] { typeof(bool) }, null);
                        if ((object)respawn == null) return;
                        respawn.Invoke(pii, new object[] { true });
                        if (_respawnStartStatusTxt) _respawnStartStatusTxt.text = "";
                    }
                    catch (System.Exception ex)
                    {
                        MelonLogger.Error("[RespawnAtStart]: " + ex.Message);
                        Telemetry.ReportErrorAsync(ex, "SessionPage");
                        if (_respawnStartStatusTxt) { _respawnStartStatusTxt.text = "Failed - see log"; _respawnStartStatusTxt.color = UIHelpers.Orange; }
                    }
                }, 60);
                FavouritesManager.RegisterStarButton("RespawnAtStart", UIHelpers.StarBtn(respawnStartRow.transform, "RespawnAtStart", () => FavouritesManager.Toggle("RespawnAtStart")));

                var skipRow = UIHelpers.StatRow("Skip Song", c);
                UIHelpers.ActionBtn(skipRow.transform, "Skip", () =>
                {
                    try { DevCommandsGameplay.SkipSong(); }
                    catch (System.Exception ex) { MelonLogger.Error("[SkipSong]: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SessionPage"); }
                }, 60);
                FavouritesManager.RegisterStarButton("SkipSong", UIHelpers.StarBtn(skipRow.transform, "SkipSong", () => FavouritesManager.Toggle("SkipSong")));

                FavouritesManager.RegisterStarButton("SpeedrunTimer", UIHelpers.StarBtn(srtr.transform, "SpeedrunTimer", () => FavouritesManager.Toggle("SpeedrunTimer")));
                FavouritesManager.RegisterStarButton("SpectateMode", UIHelpers.StarBtn(specR.transform, "SpectateMode", () => FavouritesManager.Toggle("SpectateMode")));
                FavouritesManager.RegisterStarButton("BlackDeath", UIHelpers.StarBtn(bdr.transform, "BlackDeath", () => FavouritesManager.Toggle("BlackDeath")));
                FavouritesManager.RegisterStarButton("TrickMultiplier", UIHelpers.StarBtn(tmr.transform, "TrickMultiplier", () => FavouritesManager.Toggle("TrickMultiplier")));

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
                    Id = "BlackDeath", DisplayName = "Black Death", TabBadge = "SESSION",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "BlackDeath", "Black Death",
                        () => BlackDeath.Enabled, () => BlackDeath.Toggle(), () => MenuWindow.RefreshAll()),
                    IsActive = () => BlackDeath.Enabled
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
                            string err;
                            SessionCommands.TryJumpToFinish(out err);
                        }, 60);
                    },
                    IsActive = () => false
                });
                FavouritesManager.Register(new ModFavEntry {
                    Id = "RespawnAtStart",
                    DisplayName = "Respawn at Start",
                    TabBadge = "SESSION",
                    BuildControls = (p) =>
                    {
                        var row = FavsPage.CompactStatRow("Respawn at Start", p);
                        UIHelpers.ActionBtnOrange(row.transform, "Go", () =>
                        {
                            try
                            {
                                PlayerManager pm = UnityEngine.Object.FindObjectOfType<PlayerManager>();
                                if ((object)pm == null) return;
                                var getPii = typeof(PlayerManager).GetMethod("GetPlayerImpact",
                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                if ((object)getPii == null) return;
                                object pii = getPii.Invoke(pm, null);
                                if ((object)pii == null) return;
                                var respawn = pii.GetType().GetMethod("RespawnAtStartLine",
                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                                    null, new System.Type[] { typeof(bool) }, null);
                                if ((object)respawn == null) return;
                                respawn.Invoke(pii, new object[] { true });
                            }
                            catch (System.Exception ex) { MelonLogger.Error("[RespawnAtStart]: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SessionPage"); }
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

            bool bdth = BlackDeath.Enabled;
            if (_blackDeathVal) { _blackDeathVal.text = bdth ? "ON" : "OFF"; _blackDeathVal.color = bdth ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_blackDeathTrack, _blackDeathKnob, bdth);

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
            _specTogVal = null;
            _specTargetVal = null;
            _specDistVal = null;
            _specTrack = null;
            _specKnob = null;
            _blackDeathVal = null;
            _blackDeathTrack = null;
            _blackDeathKnob = null;
            _tmLabelVal = null;
            _tmMinus = null;
            _tmPlus = null;
            _jumpStatusTxt = null;
            _respawnStartStatusTxt = null;
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

