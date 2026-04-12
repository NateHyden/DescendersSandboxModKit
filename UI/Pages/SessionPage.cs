using DescendersModMenu.Mods;
using MelonLoader;
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

        public static bool IsAnyActive =>
            SpeedrunTimer.Enabled || SessionHUD.Enabled;

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

                // ── HUD TOGGLE ────────────────────────────────────────
                UIHelpers.SectionHeader("ON-SCREEN HUD", c);
                var hudr = UIHelpers.StatRow("Show HUD", c);
                _hudTogVal = UIHelpers.Txt("HdV", hudr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _hudTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(hudr.transform, "HdT", () =>
                {
                    SessionHUD.Enabled = !SessionHUD.Enabled;
                    RefreshAll();
                }, out _hudTrack, out _hudKnob);
                UIHelpers.InfoBox(c, "Displays session stats in the top-right corner while riding.");

                UIHelpers.Divider(c);

                // ── SESSION header with Reset All ─────────────────────
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
                    TopSpeed.Reset();
                    SessionTrackers.ResetBails();
                    SessionTrackers.ResetCheckpoints();
                    SessionTrackers.ResetAirtime();
                    SessionTrackers.ResetGForce();
                    RefreshAll();
                }, 68);

                // ── Rows ──────────────────────────────────────────────
                var str = UIHelpers.StatRow("Session Timer", c);
                _sessionTimeVal = UIHelpers.Txt("StV", str.transform, SessionTrackers.SessionTimeDisplay,
                    12, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
                _sessionTimeVal.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                var tsr = UIHelpers.StatRow("Top Speed", c);
                _topSpeedVal = UIHelpers.Txt("TSV", tsr.transform, TopSpeed.DisplayValue,
                    12, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
                _topSpeedVal.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                UIHelpers.ActionBtn(tsr.transform, "Reset", () => { TopSpeed.Reset(); RefreshAll(); }, 52);

                var srtr = UIHelpers.StatRow("Speedrun Timer", c);
                _srtVal = UIHelpers.Txt("SrV", srtr.transform, "OFF", 11, FontStyle.Bold,
                    TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _srtVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(srtr.transform, "SrT", () => { SpeedrunTimer.Toggle(); RefreshAll(); },
                    out _srtTrack, out _srtKnob);
                UIHelpers.ActionBtn(srtr.transform, "Reset", () => { SpeedrunTimer.ResetTime(); }, 52);
                UIHelpers.InfoBox(c, "Requires Speedrun Timer ON in Settings > Gameplay.");

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

                // ── STAR BUTTONS (Favourites) ──────────────────────────
                FavouritesManager.RegisterStarButton("ShowHUD", UIHelpers.StarBtn(hudr.transform, "ShowHUD", () => FavouritesManager.Toggle("ShowHUD")));
                FavouritesManager.RegisterStarButton("SpeedrunTimer", UIHelpers.StarBtn(srtr.transform, "SpeedrunTimer", () => FavouritesManager.Toggle("SpeedrunTimer")));

                FavouritesManager.Register(new ModFavEntry {
                    Id = "ShowHUD", DisplayName = "Show HUD", TabBadge = "SESSION",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "ShowHUD", "Show HUD",
                        () => SessionHUD.Enabled, () => { SessionHUD.Enabled = !SessionHUD.Enabled; }, () => RefreshAll()),
                    IsActive = () => SessionHUD.Enabled
                });
                FavouritesManager.Register(new ModFavEntry {
                    Id = "SpeedrunTimer", DisplayName = "Speedrun Timer", TabBadge = "SESSION",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "SpeedrunTimer", "Speedrun Timer",
                        () => SpeedrunTimer.Enabled, () => SpeedrunTimer.Toggle(), () => RefreshAll()),
                    IsActive = () => SpeedrunTimer.Enabled
                });

                UIHelpers.AddScrollForwarders(c);
            }
            catch (System.Exception ex) { MelonLogger.Error("SessionPage.CreatePage: " + ex.Message); return null; }
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
