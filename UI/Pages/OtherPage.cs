using DescendersModMenu.Mods;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class OtherPage
    {
        // Trail Painter
        private static Text _trailVal; private static Image _trailTrack; private static RectTransform _trailKnob;
        private static Text _trailColourVal;
        // Confetti
        private static Text _confettiVal; private static Image _confettiTrack; private static RectTransform _confettiKnob;
        // Big Head Mode
        private static Text _headVal; private static Image _headTrack; private static RectTransform _headKnob;
        private static Text _headLvlVal;
        // Chaos Mode
        private static Text _chaosVal; private static Image _chaosTrack; private static RectTransform _chaosKnob;
        private static Text _chaosLastVal;
        // Random Bike Switch
        private static Text _rbsVal; private static Image _rbsTrack; private static RectTransform _rbsKnob;
        // Random Mutator
        private static Text _mutatorVal; private static Image _mutatorTrack; private static RectTransform _mutatorKnob;
        private static Text _mutatorLastVal;

        public static bool IsAnyActive =>
            TrailPainter.Enabled || ConfettiOnTrick.Enabled || BigHeadMode.Enabled ||
            ChaosMode.Enabled || RandomBikeSwitch.Enabled || RandomMutatorOnCheckpoint.Enabled;

        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("P21R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));

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

                var c = content.transform;

                // ── RESET TAB ─────────────────────────────────────────
                var rstRow = UIHelpers.BareBtnRow(c);
                UIHelpers.ActionBtnOrange(rstRow.transform, "\u21BA  Reset Tab to Defaults", () => { GlobalReset(); RefreshAll(); }, 186);

                UIHelpers.SectionHeader("VISUAL CHAOS", c);

                var trR = UIHelpers.StatRow("Trail Painter", c);
                _trailVal = UIHelpers.Txt("TrlV", trR.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _trailVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(trR.transform, "TrlT", () => { TrailPainter.Toggle(); RefreshAll(); }, out _trailTrack, out _trailKnob);
                _trailColourVal = UIHelpers.Txt("TrlCV", trR.transform, TrailPainter.ColourNames[TrailPainter.ColourIndex], 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _trailColourVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 50;
                UIHelpers.ActionBtn(trR.transform, "Cycle Colour", () => { TrailPainter.CycleColour(); RefreshAll(); }, 84);
                FavouritesManager.RegisterStarButton("TrailPainter", UIHelpers.StarBtn(trR.transform, "TrailPainter", () => FavouritesManager.Toggle("TrailPainter")));

                var cfR = UIHelpers.StatRow("Confetti on Trick Landing", c);
                _confettiVal = UIHelpers.Txt("CfV", cfR.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _confettiVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(cfR.transform, "CfT", () => { ConfettiOnTrick.Toggle(); RefreshAll(); }, out _confettiTrack, out _confettiKnob);
                FavouritesManager.RegisterStarButton("ConfettiOnTrick", UIHelpers.StarBtn(cfR.transform, "ConfettiOnTrick", () => FavouritesManager.Toggle("ConfettiOnTrick")));

                var ahR = UIHelpers.StatRow("Airhorn", c);
                UIHelpers.ActionBtn(ahR.transform, "HONK", () => { Airhorn.Honk(); }, 72);
                FavouritesManager.RegisterStarButton("Airhorn", UIHelpers.StarBtn(ahR.transform, "Airhorn", () => FavouritesManager.Toggle("Airhorn")));

                var hdR = UIHelpers.StatRow("Big Head Mode", c);
                _headVal = UIHelpers.Txt("HdV", hdR.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _headVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(hdR.transform, "HdT", () => { BigHeadMode.Toggle(); RefreshAll(); }, out _headTrack, out _headKnob);
                _headLvlVal = UIHelpers.Txt("HdLV", hdR.transform, BigHeadMode.LevelDisplay, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _headLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 40;
                UIHelpers.SmallBtn(hdR.transform, "-", () => { BigHeadMode.Decrease(); RefreshAll(); });
                UIHelpers.SmallBtn(hdR.transform, "+", () => { BigHeadMode.Increase(); RefreshAll(); });
                FavouritesManager.RegisterStarButton("BigHeadMode", UIHelpers.StarBtn(hdR.transform, "BigHeadMode", () => FavouritesManager.Toggle("BigHeadMode")));

                UIHelpers.Divider(c);

                UIHelpers.SectionHeader("CHAOS", c);

                var chR = UIHelpers.StatRow("Chaos Mode", c);
                _chaosVal = UIHelpers.Txt("ChV", chR.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _chaosVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(chR.transform, "ChT", () => { ChaosMode.Toggle(); RefreshAll(); }, out _chaosTrack, out _chaosKnob);
                FavouritesManager.RegisterStarButton("ChaosMode", UIHelpers.StarBtn(chR.transform, "ChaosMode", () => FavouritesManager.Toggle("ChaosMode")));
                UIHelpers.InfoBox(c, "Randomly flips Ice/Mirror/Drunk/Reverse Steering every few seconds. Everything reverts to how it was when you turn this off.");
                var chLastR = UIHelpers.StatRow("Last flip", c);
                _chaosLastVal = UIHelpers.Txt("ChLV", chLastR.transform, ChaosMode.LastFlipDisplay, 11,
                    FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.TextDim);
                _chaosLastVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 160;

                var rbsR = UIHelpers.StatRow("Random Bike Switch", c);
                _rbsVal = UIHelpers.Txt("RbsV", rbsR.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _rbsVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(rbsR.transform, "RbsT", () => { RandomBikeSwitch.Toggle(); RefreshAll(); }, out _rbsTrack, out _rbsKnob);
                FavouritesManager.RegisterStarButton("RandomBikeSwitch", UIHelpers.StarBtn(rbsR.transform, "RandomBikeSwitch", () => FavouritesManager.Toggle("RandomBikeSwitch")));
                UIHelpers.InfoBox(c, "Automatically switches to a different bike every few seconds using the same switching logic as the Bike tab.");

                var mutR = UIHelpers.StatRow("Random Mutator on Checkpoint", c);
                _mutatorVal = UIHelpers.Txt("MuV", mutR.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _mutatorVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(mutR.transform, "MuT", () => { RandomMutatorOnCheckpoint.Toggle(); RefreshAll(); }, out _mutatorTrack, out _mutatorKnob);
                FavouritesManager.RegisterStarButton("RandomMutatorOnCheckpoint", UIHelpers.StarBtn(mutR.transform, "RandomMutatorOnCheckpoint", () => FavouritesManager.Toggle("RandomMutatorOnCheckpoint")));
                var mutLastR = UIHelpers.StatRow("Last mutation", c);
                _mutatorLastVal = UIHelpers.Txt("MuLV", mutLastR.transform, RandomMutatorOnCheckpoint.LastMutationDisplay, 11,
                    FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.TextDim);
                _mutatorLastVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 160;

                UIHelpers.Divider(c);

                // ── Favourites/Search registry ──────────────────────────
                // RegisterStarButton (above) only wires the star icon on
                // THIS page's own rows. Register(ModFavEntry) is the
                // separate full registry the Favourites tab AND the Search
                // tab both actually read from — missing this is why these
                // mods weren't showing up in search.
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "TrailPainter",
                    DisplayName = "Trail Painter",
                    TabBadge = "OTHER",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "TrailPainter", "Trail Painter",
                        () => TrailPainter.Enabled, () => { TrailPainter.Toggle(); }, () => RefreshAll()),
                    IsActive = () => TrailPainter.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "ConfettiOnTrick",
                    DisplayName = "Confetti on Trick Landing",
                    TabBadge = "OTHER",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "ConfettiOnTrick", "Confetti on Trick Landing",
                        () => ConfettiOnTrick.Enabled, () => { ConfettiOnTrick.Toggle(); }, () => RefreshAll()),
                    IsActive = () => ConfettiOnTrick.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "Airhorn",
                    DisplayName = "Airhorn",
                    TabBadge = "OTHER",
                    BuildControls = (p) =>
                    {
                        var row = FavsPage.CompactStatRow("Airhorn", p);
                        UIHelpers.ActionBtn(row.transform, "HONK", () => { Airhorn.Honk(); }, 72);
                    },
                    IsActive = () => false // one-shot action, no persistent state
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "BigHeadMode",
                    DisplayName = "Big Head Mode",
                    TabBadge = "OTHER",
                    BuildControls = (p) => FavsPage.BuildToggleStepper(p, "BigHeadMode", "Big Head Mode",
                        () => BigHeadMode.Enabled, () => { BigHeadMode.Toggle(); },
                        () => BigHeadMode.Level, () => BigHeadMode.Decrease(), () => BigHeadMode.Increase(),
                        1, 20, () => RefreshAll(), 15),
                    IsActive = () => BigHeadMode.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "ChaosMode",
                    DisplayName = "Chaos Mode",
                    TabBadge = "OTHER",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "ChaosMode", "Chaos Mode",
                        () => ChaosMode.Enabled, () => { ChaosMode.Toggle(); }, () => RefreshAll()),
                    IsActive = () => ChaosMode.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "RandomBikeSwitch",
                    DisplayName = "Random Bike Switch",
                    TabBadge = "OTHER",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "RandomBikeSwitch", "Random Bike Switch",
                        () => RandomBikeSwitch.Enabled, () => { RandomBikeSwitch.Toggle(); }, () => RefreshAll()),
                    IsActive = () => RandomBikeSwitch.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "RandomMutatorOnCheckpoint",
                    DisplayName = "Random Mutator on Checkpoint",
                    TabBadge = "OTHER",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "RandomMutatorOnCheckpoint", "Random Mutator on Checkpoint",
                        () => RandomMutatorOnCheckpoint.Enabled, () => { RandomMutatorOnCheckpoint.Toggle(); }, () => RefreshAll()),
                    IsActive = () => RandomMutatorOnCheckpoint.Enabled
                });

                RefreshAll();
                UIHelpers.AddScrollForwarders(c);
            }
            catch (System.Exception ex) { MelonLogger.Error("OtherPage.CreatePage: " + ex.Message); Telemetry.ReportErrorAsync(ex, "OtherPage"); return null; }
            return pg;
        }

        public static void GlobalReset()
        {
            TrailPainter.Reset();
            ConfettiOnTrick.Reset();
            // Airhorn has no persistent state to reset — it's a one-shot
            // action (like SuperLaunch/TeleportCheckpoint), not a toggle.
            BigHeadMode.Reset();
            ChaosMode.Reset();
            RandomBikeSwitch.Reset();
            RandomMutatorOnCheckpoint.Reset();
        }

        public static void RefreshAll()
        {
            bool trOn = TrailPainter.Enabled;
            if (_trailVal) { _trailVal.text = trOn ? "ON" : "OFF"; _trailVal.color = trOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_trailTrack, _trailKnob, trOn);
            if (_trailColourVal) _trailColourVal.text = TrailPainter.ColourNames[TrailPainter.ColourIndex];

            bool cfOn = ConfettiOnTrick.Enabled;
            if (_confettiVal) { _confettiVal.text = cfOn ? "ON" : "OFF"; _confettiVal.color = cfOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_confettiTrack, _confettiKnob, cfOn);

            bool hdOn = BigHeadMode.Enabled;
            if (_headVal) { _headVal.text = hdOn ? "ON" : "OFF"; _headVal.color = hdOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_headTrack, _headKnob, hdOn);
            if (_headLvlVal) _headLvlVal.text = BigHeadMode.LevelDisplay;

            bool chOn = ChaosMode.Enabled;
            if (_chaosVal) { _chaosVal.text = chOn ? "ON" : "OFF"; _chaosVal.color = chOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_chaosTrack, _chaosKnob, chOn);
            if (_chaosLastVal) _chaosLastVal.text = ChaosMode.LastFlipDisplay;

            bool rbsOn = RandomBikeSwitch.Enabled;
            if (_rbsVal) { _rbsVal.text = rbsOn ? "ON" : "OFF"; _rbsVal.color = rbsOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_rbsTrack, _rbsKnob, rbsOn);

            bool muOn = RandomMutatorOnCheckpoint.Enabled;
            if (_mutatorVal) { _mutatorVal.text = muOn ? "ON" : "OFF"; _mutatorVal.color = muOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_mutatorTrack, _mutatorKnob, muOn);
            if (_mutatorLastVal) _mutatorLastVal.text = RandomMutatorOnCheckpoint.LastMutationDisplay;
        }
    }
}
