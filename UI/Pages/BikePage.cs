using DescendersModMenu.Mods;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class BikePage
    {
        // ── Suspension ────────────────────────────────────────────────
        private static Text _travelVal, _stiffVal, _dampVal;
        private static Image _travelBar, _stiffBar, _dampBar;

        // ── Bouncy Bike ───────────────────────────────────────────────
        private static GameObject _bbRow2;
        private static Text _bbVal, _bbLvlVal;
        private static Image _bbTrack; private static RectTransform _bbKnob;
        private static UnityEngine.UI.Button _bbMinus2, _bbPlus2;


        private static Text _bikeSizeLvlVal;
        private static UnityEngine.UI.Button _bikeSizeMinus, _bikeSizePlus;
        private static Text _wheelSizeLvlVal;
        private static UnityEngine.UI.Button _wheelSizeMinus2, _wheelSizePlus2;

        // ── Individual front/rear wheel size ─────────────────────────
        private static Text _frontWheelLvlVal;
        private static Text _rearWheelLvlVal;
        // Note: front/rear stepper buttons not stored — no interactable control needed
        private static GameObject _frontWheelRow, _rearWheelRow;
        private static Image _invisBikeTrack; private static RectTransform _invisBikeKnob;
        private static Text _invisBikeVal;

        // ── Wheel Size ────────────────────────────────────────────────
        private static Image _wheelSizeTrack;
        private static RectTransform _wheelSizeKnob;
        private static Text _wheelSizeTogVal;

        // ── Wide Tyres ────────────────────────────────────────────────
        private static Image _wideTyresTrack; private static RectTransform _wideTyresKnob;
        private static Text _wideTyresVal, _wideTyresLvlVal; private static Image _wideTyresBar;
        private static UnityEngine.UI.Button _wideTyresMinus, _wideTyresPlus;

        // ── Spider Bike ───────────────────────────────────────────────
        private static Image _spiderTrack; private static RectTransform _spiderKnob;
        private static Text _spiderVal;
        private static GameObject _spiderRow;

        // ── Sticky Tyres ──────────────────────────────────────────────
        private static Image _stickyTrack; private static RectTransform _stickyKnob;
        private static Text _stickyVal;

        // ── Tyre Pressure ─────────────────────────────────────────────
        private static Image _tyrePressureTrack; private static RectTransform _tyrePressureKnob;
        private static Text _tyrePressureVal;
        private static Text _tyrePressureLvlVal;
        private static Text _tyrePressureLabelVal;
        private static UnityEngine.UI.Button _tyrePressureMinus, _tyrePressurePlus;
        private static GameObject _tyrePressureRow, _tyrePressureLvlRow;

        // ── Bike Damage ───────────────────────────────────────────────
        private static GameObject _bikeDamageRow;
        private static Text _bikeDamageVal;
        private static Image _bikeDamageTrack;
        private static RectTransform _bikeDamageKnob;

        // ── Reverse Steering ──────────────────────────────────────────
        private static Image _revSteerTrack; private static RectTransform _revSteerKnob;
        private static Text _revSteerVal;

        // ── Rubber Band Steering ──────────────────────────────────────
        private static Image _rubberTrack; private static RectTransform _rubberKnob;
        private static Text _rubberVal, _rubberLvlVal;

        // ── Bike switcher / Trick Set Swap ────────────────────────────
        private static Text _bikeVal;
        private static Text _tssSrcVal, _tssTogVal;
        private static Image _tssTrack;
        private static RectTransform _tssKnob;

        // ── Cut Brakes ────────────────────────────────────────────────
        private static Image _cutBrakesTrack; private static RectTransform _cutBrakesKnob;
        private static Text _cutBrakesVal;

        // ── Torch ─────────────────────────────────────────────────────
        private static Text _torchVal, _torchIntLbl, _torchDiscoVal;
        private static Image _torchTrack, _torchDiscoTrack;
        private static RectTransform _torchKnob, _torchDiscoKnob;

        // ── Row GO refs for highlight ─────────────────────────────────
        private static GameObject _invisBikeRow, _wheelSizeRow, _wideTyresRow, _stickyRow;
        private static GameObject _revSteerRow, _cutBrakesRow, _torchRow, _torchDiscoRow;

        // ── Suspension HUD (Telemetry) ────────────────────────────────
        private static GameObject _shRow;
        private static Text _shVal;
        private static Image _shTrack; private static RectTransform _shKnob;

        // ── Brake Fade (Telemetry) ────────────────────────────────────
        private static GameObject _bfRow;
        private static Text _bfVal;
        private static Image _bfTrack; private static RectTransform _bfKnob;
        // ── Brake Balance ─────────────────────────────────────────────
        private static Text _bbLabelVal;
        private static UnityEngine.UI.Button _bbMinus, _bbPlus;
        private static GameObject _bbRow;

        public static void CaptureSceneDefaults()
        {
            BikeSize.CaptureDefaults();
        }

        public static bool IsAnyActive =>
            SpiderBike.Enabled ||
            Suspension.TravelLevel != 5 || Suspension.StiffnessLevel != 5 || Suspension.DampingLevel != 5 ||
            BouncyBike.Enabled ||
            WheelSize.IsEnabled ||
            (WheelSize.IsIndividualMode && (WheelSize.FrontLevel != 10 || WheelSize.RearLevel != 10)) ||
            InvisibleBike.Enabled || BikeSize.IsModified ||
            WideTyres.Enabled || StickyTyres.Enabled ||
            ReverseSteering.Enabled || RubberBandSteering.Enabled || CutBrakes.Enabled ||
            BikeTorch.Enabled || BikeTorch.DiscoEnabled || SuspensionHUD.Enabled || BrakeFade.Enabled ||
            BrakeFade.BalanceLevel != 6 ||
            TyrePressure.Enabled || BikeDamage.Enabled || TrickSetSwap.Enabled;

        public static void GlobalReset()
        {
            if (InvisibleBike.Enabled) InvisibleBike.SetEnabled(false);
            if (SpiderBike.Enabled) SpiderBike.Toggle();
            BouncyBike.Reset();
            WheelSize.Reset();
            BikeSize.ResetToDefault();
            BikeSize.Level = 10;
            if (TrickSetSwap.Enabled) TrickSetSwap.Disable();
            RubberBandSteering.Reset();
        }

        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("P8R", parent);
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

                content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                var pg8 = content.transform;

                // ── RESET TAB ─────────────────────────────────────────
                var rstRow = UIHelpers.BareBtnRow(pg8);
                UIHelpers.ActionBtnOrange(rstRow.transform, "↺  Reset Tab to Defaults", () => { ResetBikeTab(); RefreshAll(); }, 186);

                _spiderRow = UIHelpers.StatRow("Spider Bike", pg8);
                _spiderVal = UIHelpers.Txt("SpV", _spiderRow.transform, "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _spiderVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(_spiderRow.transform, "SpT", () => { SpiderBike.Toggle(); RefreshAll(); },
                    out _spiderTrack, out _spiderKnob);
                UIHelpers.InfoBox(pg8, "Stick to walls, roofs and any surface. Ride like the floor is wherever your tyres are. Stops you bailing from the tilt while it's on.");

                UIHelpers.SectionHeader("BIKE TYPE", pg8);

                var br = UIHelpers.StatRow("Bike", pg8);
                UIHelpers.SmallBtn(br.transform, "\u25C0", () => { BikeSwitcher.PreviousBike(); RefreshAll(); });
                _bikeVal = UIHelpers.Txt("BV", br.transform, "Enduro", 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _bikeVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 80;
                UIHelpers.SmallBtn(br.transform, "\u25B6", () => { BikeSwitcher.NextBike(); RefreshAll(); });

                var tssSrc = UIHelpers.StatRow("Trick Source", pg8);
                UIHelpers.SmallBtn(tssSrc.transform, "\u25C0", () => { TrickSetSwap.PrevSource(); RefreshAll(); });
                _tssSrcVal = UIHelpers.Txt("TSV", tssSrc.transform, TrickSetSwap.CurrentSourceName, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _tssSrcVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 80;
                UIHelpers.SmallBtn(tssSrc.transform, "\u25B6", () => { TrickSetSwap.NextSource(); RefreshAll(); });

                var tssR = UIHelpers.StatRow("Trick Set Swap", pg8);
                _tssTogVal = UIHelpers.Txt("TSTV", tssR.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _tssTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(tssR.transform, "TST", () => { TrickSetSwap.Toggle(); RefreshAll(); }, out _tssTrack, out _tssKnob);

                UIHelpers.Divider(pg8);

                UIHelpers.SectionHeader("SUSPENSION", pg8);

                var tr = UIHelpers.StatRow("Travel", pg8);
                _travelBar = UIHelpers.MakeBar("TvB", tr.transform, (Suspension.TravelLevel - 1) / 9f);
                _travelVal = UIHelpers.Txt("TvV", tr.transform, Suspension.TravelLevel.ToString(), 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _travelVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
                UIHelpers.SmallBtn(tr.transform, "-", () => { Suspension.TravelDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(tr.transform, "+", () => { Suspension.TravelIncrease(); RefreshAll(); });

                var sr = UIHelpers.StatRow("Stiffness", pg8);
                _stiffBar = UIHelpers.MakeBar("StB", sr.transform, (Suspension.StiffnessLevel - 1) / 9f);
                _stiffVal = UIHelpers.Txt("StV", sr.transform, Suspension.StiffnessLevel.ToString(), 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _stiffVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
                UIHelpers.SmallBtn(sr.transform, "-", () => { Suspension.StiffnessDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(sr.transform, "+", () => { Suspension.StiffnessIncrease(); RefreshAll(); });

                var dr = UIHelpers.StatRow("Damping", pg8);
                _dampBar = UIHelpers.MakeBar("DpB", dr.transform, (Suspension.DampingLevel - 1) / 9f);
                _dampVal = UIHelpers.Txt("DpV", dr.transform, Suspension.DampingLevel.ToString(), 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _dampVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
                UIHelpers.SmallBtn(dr.transform, "-", () => { Suspension.DampingDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(dr.transform, "+", () => { Suspension.DampingIncrease(); RefreshAll(); });

                UIHelpers.InfoBox(pg8, "Level 5 = default. Travel = how much the fork/shock moves. Stiffness = spring resistance. Damping = how fast it settles.");

                _bbRow2 = UIHelpers.StatRow("Bouncy Bike", pg8);
                var bb2 = _bbRow2;
                _bbVal = UIHelpers.Txt("BbV2", bb2.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _bbVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(bb2.transform, "BbT2", () => { BouncyBike.Toggle(); RefreshAll(); }, out _bbTrack, out _bbKnob);
                _bbMinus2 = UIHelpers.SmallBtn(bb2.transform, "-", () => { BouncyBike.DecreaseLevel(); RefreshAll(); });
                _bbLvlVal = UIHelpers.Txt("BbL2", bb2.transform, BouncyBike.BouncinessLevel.ToString(), 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _bbLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
                _bbPlus2 = UIHelpers.SmallBtn(bb2.transform, "+", () => { BouncyBike.IncreaseLevel(); RefreshAll(); });
                UIHelpers.InfoBox(pg8, "Bounces the bike off the ground on landing — bigger falls bounce higher, and it naturally fades out over a few bounces just like a real ball. Level 1 = barely bounces, 10 = superball. Hard bounces can trigger the game's own crash detection — pairs well with a higher Landing Impact threshold or No Bail.");

                UIHelpers.Divider(pg8);

                // ── BIKE SIZE ─────────────────────────────────────────
                UIHelpers.SectionHeader("BIKE SIZE", pg8);

                var szr = UIHelpers.StatRow("Size", pg8);
                _bikeSizeMinus = UIHelpers.SmallBtn(szr.transform, "◀", () =>
                {
                    if (BikeSize.Level > 1) { BikeSize.Level--; BikeSize.ApplyLevel(BikeSize.Level); RefreshAll(); }
                });
                _bikeSizeLvlVal = UIHelpers.Txt("BsL", szr.transform, BikeSize.Level.ToString(), 13,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _bikeSizeLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 32;
                _bikeSizePlus = UIHelpers.SmallBtn(szr.transform, "▶", () =>
                {
                    if (BikeSize.Level < 20) { BikeSize.Level++; BikeSize.ApplyLevel(BikeSize.Level); RefreshAll(); }
                });

                UIHelpers.Divider(pg8);

                // Add info box for bike size
                UIHelpers.InfoBox(pg8, "10 = default size. Lower numbers shrink the bike, higher numbers grow it.");

                UIHelpers.Divider(pg8);

                // ── BIKE PARTS ────────────────────────────────────────
                UIHelpers.SectionHeader("BIKE PARTS", pg8);

                // Invisible Bike
                _invisBikeRow = UIHelpers.StatRow("Invisible Bike", pg8);
                var ibr = _invisBikeRow;
                _invisBikeVal = UIHelpers.Txt("IbV", ibr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _invisBikeVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(ibr.transform, "IbT", () =>
                {
                    InvisibleBike.Toggle();
                    RefreshAll();
                }, out _invisBikeTrack, out _invisBikeKnob);

                // Wheel Size
                _wheelSizeRow = UIHelpers.StatRow("Wheel Size", pg8);
                var gwr = _wheelSizeRow;
                _wheelSizeTogVal = UIHelpers.Txt("WsTV", gwr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _wheelSizeTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(gwr.transform, "WsT", () =>
                {
                    WheelSize.Toggle();
                    RefreshAll();
                }, out _wheelSizeTrack, out _wheelSizeKnob);
                _wheelSizeMinus2 = UIHelpers.SmallBtn(gwr.transform, "◀", () =>
                {
                    if (WheelSize.Level > 1) { WheelSize.Level--; if (WheelSize.IsEnabled) WheelSize.ApplyLevel(WheelSize.Level); RefreshAll(); }
                });
                _wheelSizeLvlVal = UIHelpers.Txt("WsL", gwr.transform, WheelSize.Level.ToString(), 13,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _wheelSizeLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 32;
                _wheelSizePlus2 = UIHelpers.SmallBtn(gwr.transform, "▶", () =>
                {
                    if (WheelSize.Level < 20) { WheelSize.Level++; if (WheelSize.IsEnabled) WheelSize.ApplyLevel(WheelSize.Level); RefreshAll(); }
                });

                UIHelpers.InfoBox(pg8, "10 = default size. Enable the toggle first, then use ◀ ▶ to adjust. Or set front/rear individually below.");

                // Front Wheel Size
                _frontWheelRow = UIHelpers.StatRow("Front Wheel Size", pg8);
                UIHelpers.SmallBtn(_frontWheelRow.transform, "◀", () =>
                {
                    if (WheelSize.FrontLevel > 1)
                    {
                        WheelSize.FrontLevel--;
                        WheelSize.IsIndividualMode = true;
                        if (WheelSize.IsEnabled) { WheelSize.IsEnabled = false; }
                        WheelSize.ApplyIndividualLevels();
                        RefreshAll();
                    }
                });
                _frontWheelLvlVal = UIHelpers.Txt("FwL", _frontWheelRow.transform, WheelSize.FrontLevel.ToString(), 13,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _frontWheelLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 32;
                UIHelpers.SmallBtn(_frontWheelRow.transform, "▶", () =>
                {
                    if (WheelSize.FrontLevel < 20)
                    {
                        WheelSize.FrontLevel++;
                        WheelSize.IsIndividualMode = true;
                        if (WheelSize.IsEnabled) { WheelSize.IsEnabled = false; }
                        WheelSize.ApplyIndividualLevels();
                        RefreshAll();
                    }
                });

                // Rear Wheel Size
                _rearWheelRow = UIHelpers.StatRow("Rear Wheel Size", pg8);
                UIHelpers.SmallBtn(_rearWheelRow.transform, "◀", () =>
                {
                    if (WheelSize.RearLevel > 1)
                    {
                        WheelSize.RearLevel--;
                        WheelSize.IsIndividualMode = true;
                        if (WheelSize.IsEnabled) { WheelSize.IsEnabled = false; }
                        WheelSize.ApplyIndividualLevels();
                        RefreshAll();
                    }
                });
                _rearWheelLvlVal = UIHelpers.Txt("RwL", _rearWheelRow.transform, WheelSize.RearLevel.ToString(), 13,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _rearWheelLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 32;
                UIHelpers.SmallBtn(_rearWheelRow.transform, "▶", () =>
                {
                    if (WheelSize.RearLevel < 20)
                    {
                        WheelSize.RearLevel++;
                        WheelSize.IsIndividualMode = true;
                        if (WheelSize.IsEnabled) { WheelSize.IsEnabled = false; }
                        WheelSize.ApplyIndividualLevels();
                        RefreshAll();
                    }
                });

                // Wide Tyres
                _wideTyresRow = UIHelpers.StatRow("Wide Tyres", pg8);
                var wtr = _wideTyresRow;
                _wideTyresVal = UIHelpers.Txt("WtV", wtr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _wideTyresVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(wtr.transform, "WtT", () => { WideTyres.Toggle(); RefreshAll(); }, out _wideTyresTrack, out _wideTyresKnob);
                _wideTyresBar = UIHelpers.MakeBar("WtB", wtr.transform, (WideTyres.Level - 1) / 19f);
                _wideTyresLvlVal = UIHelpers.Txt("WtL", wtr.transform, WideTyres.Level.ToString(), 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _wideTyresLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
                _wideTyresMinus = UIHelpers.SmallBtn(wtr.transform, "-", () => { WideTyres.Decrease(); RefreshAll(); });
                _wideTyresPlus = UIHelpers.SmallBtn(wtr.transform, "+", () => { WideTyres.Increase(); RefreshAll(); });

                // Sticky Tyres
                _stickyRow = UIHelpers.StatRow("Sticky Tyres", pg8);
                var str2 = _stickyRow;
                _stickyVal = UIHelpers.Txt("StV", str2.transform, StickyTyres.Enabled ? "ON" : "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, StickyTyres.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor);
                _stickyVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(str2.transform, "StT", () => { StickyTyres.Toggle(); RefreshAll(); }, out _stickyTrack, out _stickyKnob);

                // Tyre Pressure
                _tyrePressureRow = UIHelpers.StatRow("Tyre Pressure", pg8);
                var tpr = _tyrePressureRow;
                _tyrePressureVal = UIHelpers.Txt("TpV", tpr.transform, "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _tyrePressureVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(tpr.transform, "TpT", () => { TyrePressure.Toggle(); RefreshAll(); },
                    out _tyrePressureTrack, out _tyrePressureKnob);

                _tyrePressureLvlRow = UIHelpers.StatRow("Pressure", pg8);
                var tplr = _tyrePressureLvlRow;
                _tyrePressureMinus = UIHelpers.SmallBtn(tplr.transform, "\u25C0", () =>
                {
                    TyrePressure.Decrease(); RefreshAll();
                });
                _tyrePressureLvlVal = UIHelpers.Txt("TpLv", tplr.transform,
                    TyrePressure.Level.ToString(), 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _tyrePressureLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 22;
                _tyrePressureLabelVal = UIHelpers.Txt("TpLbl", tplr.transform,
                    TyrePressure.PressureLabel, 11,
                    FontStyle.Normal, TextAnchor.MiddleLeft, UIHelpers.TextMid);
                _tyrePressureLabelVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 40;
                _tyrePressurePlus = UIHelpers.SmallBtn(tplr.transform, "\u25B6", () =>
                {
                    TyrePressure.Increase(); RefreshAll();
                });

                _bikeDamageRow = UIHelpers.StatRow("Bike Damage", pg8);
                var bdr = _bikeDamageRow;
                _bikeDamageVal = UIHelpers.Txt("BdV", bdr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _bikeDamageVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(bdr.transform, "BdT", () => { BikeDamage.Toggle(); RefreshAll(); }, out _bikeDamageTrack, out _bikeDamageKnob);
                var resetBtn = UIHelpers.SmallBtn(bdr.transform, "RESET", () => { BikeDamage.ManualReset(); });
                resetBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 48;
                UIHelpers.InfoBox(pg8, "Crashes make the bike drift left/right — counter-steer to correct. Hard impacts (>43km/h) remove the rear wheel so the back drags on the floor. Press RESET to fix mid-session.");

                UIHelpers.Divider(pg8);

                // ── CONTROLS ──────────────────────────────────────────
                UIHelpers.SectionHeader("CONTROLS", pg8);

                _revSteerRow = UIHelpers.StatRow("Reverse Steering", pg8);
                var rsr = _revSteerRow;
                _revSteerVal = UIHelpers.Txt("RsV", rsr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _revSteerVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(rsr.transform, "RsT", () => { ReverseSteering.Toggle(); RefreshAll(); }, out _revSteerTrack, out _revSteerKnob);

                var rubR = UIHelpers.StatRow("Rubber Band Steering", pg8);
                _rubberVal = UIHelpers.Txt("RbV", rubR.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _rubberVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(rubR.transform, "RbT", () => { RubberBandSteering.Toggle(); RefreshAll(); }, out _rubberTrack, out _rubberKnob);
                _rubberLvlVal = UIHelpers.Txt("RbLV", rubR.transform, RubberBandSteering.LevelDisplay, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _rubberLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
                UIHelpers.SmallBtn(rubR.transform, "-", () => { RubberBandSteering.Decrease(); RefreshAll(); });
                UIHelpers.SmallBtn(rubR.transform, "+", () => { RubberBandSteering.Increase(); RefreshAll(); });
                UIHelpers.InfoBox(pg8, "Delays your steering and lean input so every move lands late. Level 1 = 50ms, Level 10 = 500ms.");

                _cutBrakesRow = UIHelpers.StatRow("Cut Brakes", pg8);
                var cbr = _cutBrakesRow;
                _cutBrakesVal = UIHelpers.Txt("CbV", cbr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _cutBrakesVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(cbr.transform, "CbT", () => { CutBrakes.Toggle(); RefreshAll(); }, out _cutBrakesTrack, out _cutBrakesKnob);

                UIHelpers.Divider(pg8);

                // ── TORCH ─────────────────────────────────────────────
                UIHelpers.SectionHeader("TORCH", pg8);

                _torchRow = UIHelpers.StatRow("Headlight", pg8);
                var tchr = _torchRow;
                _torchVal = UIHelpers.Txt("TchV", tchr.transform, "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _torchVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(tchr.transform, "TchT",
                    () => { BikeTorch.Toggle(); RefreshAll(); },
                    out _torchTrack, out _torchKnob);

                var tcir = UIHelpers.StatRow("Intensity", pg8);
                UIHelpers.SmallBtn(tcir.transform, "\u25C0",
                    () => { BikeTorch.PrevIntensity(); RefreshAll(); });
                _torchIntLbl = UIHelpers.Txt("TchIV", tcir.transform,
                    BikeTorch.IntensityDisplay, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _torchIntLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 56;
                UIHelpers.SmallBtn(tcir.transform, "\u25B6",
                    () => { BikeTorch.NextIntensity(); RefreshAll(); });

                _torchDiscoRow = UIHelpers.StatRow("Disco Torch", pg8);
                var tdr = _torchDiscoRow;
                _torchDiscoVal = UIHelpers.Txt("TchDV", tdr.transform, "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _torchDiscoVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(tdr.transform, "TchDT",
                    () => { BikeTorch.ToggleDisco(); RefreshAll(); },
                    out _torchDiscoTrack, out _torchDiscoKnob);

                UIHelpers.InfoBox(pg8,
                    "Headlight: enables the bike spotlight (or creates one if missing).\n" +
                    "Disco Torch: cycles the beam through neon colours. Warning: flashing lights.");

                UIHelpers.Divider(pg8);

                // ── TELEMETRY ─────────────────────────────────────────
                UIHelpers.SectionHeader("TELEMETRY", pg8);

                _shRow = UIHelpers.StatRow("Suspension HUD", pg8);
                var shr = _shRow;
                _shVal = UIHelpers.Txt("ShV", shr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _shVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(shr.transform, "ShT", () => { SuspensionHUD.Toggle(); RefreshAll(); }, out _shTrack, out _shKnob);

                _bfRow = UIHelpers.StatRow("Brake Fade", pg8);
                var bfr = _bfRow;
                _bfVal = UIHelpers.Txt("BfV", bfr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _bfVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(bfr.transform, "BfT", () => { BrakeFade.Toggle(); RefreshAll(); }, out _bfTrack, out _bfKnob);

                _bbRow = UIHelpers.StatRow("Brake Balance", pg8);
                var bbr = _bbRow;
                _bbMinus = UIHelpers.SmallBtn(bbr.transform, "\u25C0", () => { BrakeFade.DecreaseBalance(); RefreshAll(); });
                _bbLabelVal = UIHelpers.Txt("BbLV", bbr.transform,
                    BrakeFade.BalanceDisplay, 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _bbLabelVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 64;
                _bbPlus = UIHelpers.SmallBtn(bbr.transform, "\u25B6", () => { BrakeFade.IncreaseBalance(); RefreshAll(); });
                UIHelpers.InfoBox(pg8, "Your brake discs overheat from hard braking. Brakes weaken above 150°C and fail completely at 300°C. Let go to cool down — going fast cools them quicker. Watch the top-right HUD.");

                // ── STAR BUTTONS (Favourites) ──────────────────────────
                Transform suspHdr = pg8.Find("SUSPENSIONH");
                if ((object)suspHdr != null)
                    FavouritesManager.RegisterStarButton("Suspension", UIHelpers.StarBtnAbs(suspHdr, "Suspension", () => FavouritesManager.Toggle("Suspension")));
                FavouritesManager.RegisterStarButton("SpiderBike", UIHelpers.StarBtn(_spiderRow.transform, "SpiderBike", () => FavouritesManager.Toggle("SpiderBike")));
                FavouritesManager.RegisterStarButton("BikeSwitcher", UIHelpers.StarBtn(br.transform, "BikeSwitcher", () => FavouritesManager.Toggle("BikeSwitcher")));
                FavouritesManager.RegisterStarButton("BikeSize", UIHelpers.StarBtn(szr.transform, "BikeSize", () => FavouritesManager.Toggle("BikeSize")));
                FavouritesManager.RegisterStarButton("BouncyBike", UIHelpers.StarBtn(_bbRow2.transform, "BouncyBike", () => FavouritesManager.Toggle("BouncyBike")));
                FavouritesManager.RegisterStarButton("InvisibleBike", UIHelpers.StarBtn(_invisBikeRow.transform, "InvisibleBike", () => FavouritesManager.Toggle("InvisibleBike")));
                FavouritesManager.RegisterStarButton("WheelSize", UIHelpers.StarBtn(_wheelSizeRow.transform, "WheelSize", () => FavouritesManager.Toggle("WheelSize")));
                FavouritesManager.RegisterStarButton("FrontWheelSize", UIHelpers.StarBtn(_frontWheelRow.transform, "FrontWheelSize", () => FavouritesManager.Toggle("FrontWheelSize")));
                FavouritesManager.RegisterStarButton("RearWheelSize", UIHelpers.StarBtn(_rearWheelRow.transform, "RearWheelSize", () => FavouritesManager.Toggle("RearWheelSize")));
                FavouritesManager.RegisterStarButton("WideTyres", UIHelpers.StarBtn(_wideTyresRow.transform, "WideTyres", () => FavouritesManager.Toggle("WideTyres")));
                FavouritesManager.RegisterStarButton("StickyTyres", UIHelpers.StarBtn(_stickyRow.transform, "StickyTyres", () => FavouritesManager.Toggle("StickyTyres")));
                FavouritesManager.RegisterStarButton("TyrePressure", UIHelpers.StarBtn(_tyrePressureRow.transform, "TyrePressure", () => FavouritesManager.Toggle("TyrePressure")));
                FavouritesManager.RegisterStarButton("BikeDamage", UIHelpers.StarBtn(_bikeDamageRow.transform, "BikeDamage", () => FavouritesManager.Toggle("BikeDamage")));
                FavouritesManager.RegisterStarButton("ReverseSteering", UIHelpers.StarBtn(_revSteerRow.transform, "ReverseSteering", () => FavouritesManager.Toggle("ReverseSteering")));
                FavouritesManager.RegisterStarButton("RubberBandSteering", UIHelpers.StarBtn(rubR.transform, "RubberBandSteering", () => FavouritesManager.Toggle("RubberBandSteering")));
                FavouritesManager.RegisterStarButton("CutBrakes", UIHelpers.StarBtn(_cutBrakesRow.transform, "CutBrakes", () => FavouritesManager.Toggle("CutBrakes")));
                FavouritesManager.RegisterStarButton("BikeTorch", UIHelpers.StarBtn(_torchRow.transform, "BikeTorch", () => FavouritesManager.Toggle("BikeTorch")));
                FavouritesManager.RegisterStarButton("DiscoTorch", UIHelpers.StarBtn(_torchDiscoRow.transform, "DiscoTorch", () => FavouritesManager.Toggle("DiscoTorch")));
                FavouritesManager.RegisterStarButton("SuspensionHUD", UIHelpers.StarBtn(_shRow.transform, "SuspensionHUD", () => FavouritesManager.Toggle("SuspensionHUD")));
                FavouritesManager.RegisterStarButton("BrakeFade", UIHelpers.StarBtn(_bfRow.transform, "BrakeFade", () => FavouritesManager.Toggle("BrakeFade")));
                FavouritesManager.RegisterStarButton("BrakeBalance", UIHelpers.StarBtn(_bbRow.transform, "BrakeBalance", () => FavouritesManager.Toggle("BrakeBalance")));

                // ── FACTORY REGISTRATIONS (Bike tab mods) ──────────────
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "SpiderBike",
                    DisplayName = "Spider Bike",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "SpiderBike", "Spider Bike",
                        () => SpiderBike.Enabled, () => SpiderBike.Toggle(), () => RefreshAll()),
                    IsActive = () => SpiderBike.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "BikeSwitcher",
                    DisplayName = "Bike",
                    TabBadge = "BIKE",
                    BuildControls = (fp) => {
                        var row = FavsPage.CompactStatRow("Bike", fp);
                        UIHelpers.SmallBtn(row.transform, "\u25C0", () => { BikeSwitcher.PreviousBike(); RefreshAll(); FavsPage.RefreshFavourites(); });
                        var bv = UIHelpers.Txt("FBV", row.transform, "Enduro", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                        bv.gameObject.AddComponent<LayoutElement>().preferredWidth = 72;
                        UIHelpers.SmallBtn(row.transform, "\u25B6", () => { BikeSwitcher.NextBike(); RefreshAll(); FavsPage.RefreshFavourites(); });

                        var tsRow = FavsPage.CompactStatRow("Trick Source", fp);
                        UIHelpers.SmallBtn(tsRow.transform, "\u25C0", () => { TrickSetSwap.PrevSource(); RefreshAll(); FavsPage.RefreshFavourites(); });
                        var tsv = UIHelpers.Txt("FTSV", tsRow.transform, TrickSetSwap.CurrentSourceName, 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                        tsv.gameObject.AddComponent<LayoutElement>().preferredWidth = 72;
                        UIHelpers.SmallBtn(tsRow.transform, "\u25B6", () => { TrickSetSwap.NextSource(); RefreshAll(); FavsPage.RefreshFavourites(); });

                        var tssRow = FavsPage.CompactStatRow("Trick Set Swap", fp);
                        var tssVal = UIHelpers.Txt("FTSSV", tssRow.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                        tssVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                        Image fTssTrack; RectTransform fTssKnob;
                        UIHelpers.Toggle(tssRow.transform, "FTg_TrickSetSwap",
                            () => { TrickSetSwap.Toggle(); RefreshAll(); FavsPage.RefreshFavourites(); },
                            out fTssTrack, out fTssKnob);

                        FavouritesManager.RegisterRefresh("BikeSwitcher", () => {
                            if (bv)
                            {
                                switch (BikeSwitcher.CurrentBikeIndex)
                                {
                                    case 0: bv.text = "Enduro"; break;
                                    case 1: bv.text = "Downhill"; break;
                                    case 2: bv.text = "Hardtail"; break;
                                    case 3: bv.text = "BRNZL Enduro"; break;
                                    default: bv.text = "Unknown"; break;
                                }
                            }
                            if (tsv) tsv.text = TrickSetSwap.CurrentSourceName;
                            bool tssOn = TrickSetSwap.Enabled;
                            if (tssVal) { tssVal.text = tssOn ? "ON" : "OFF"; tssVal.color = tssOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
                            UIHelpers.SetToggle(fTssTrack, fTssKnob, tssOn);
                        });
                    },
                    IsActive = () => TrickSetSwap.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "Suspension",
                    DisplayName = "Suspension",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildTripleSlider(p, "Suspension",
                        "Travel", () => Suspension.TravelLevel, () => Suspension.TravelIncrease(), () => Suspension.TravelDecrease(),
                        "Stiffness", () => Suspension.StiffnessLevel, () => Suspension.StiffnessIncrease(), () => Suspension.StiffnessDecrease(),
                        "Damping", () => Suspension.DampingLevel, () => Suspension.DampingIncrease(), () => Suspension.DampingDecrease(),
                        () => (Suspension.TravelLevel - 1) / 9f, () => (Suspension.StiffnessLevel - 1) / 9f, () => (Suspension.DampingLevel - 1) / 9f,
                        () => RefreshAll(), 5),
                    IsActive = () => Suspension.TravelLevel != 5 || Suspension.StiffnessLevel != 5 || Suspension.DampingLevel != 5
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "BouncyBike",
                    DisplayName = "Bouncy Bike",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildToggleStepper(p, "BouncyBike", "Bouncy Bike",
                        () => BouncyBike.Enabled,
                        () => BouncyBike.Toggle(),
                        () => BouncyBike.BouncinessLevel,
                        () => BouncyBike.DecreaseLevel(),
                        () => BouncyBike.IncreaseLevel(),
                        1, 10, () => RefreshAll(), 5),
                    IsActive = () => BouncyBike.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "BikeSize",
                    DisplayName = "Bike Size",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildStepper(p, "BikeSize", "Bike Size",
                        () => BikeSize.Level,
                        () => { if (BikeSize.Level > 1) { BikeSize.Level--; BikeSize.ApplyLevel(BikeSize.Level); } },
                        () => { if (BikeSize.Level < 20) { BikeSize.Level++; BikeSize.ApplyLevel(BikeSize.Level); } },
                        1, 20, () => RefreshAll(), 10),
                    IsActive = () => BikeSize.Level != 10
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "InvisibleBike",
                    DisplayName = "Invisible Bike",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "InvisibleBike", "Invisible Bike",
                        () => InvisibleBike.Enabled, () => { InvisibleBike.Toggle(); }, () => RefreshAll()),
                    IsActive = () => InvisibleBike.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "WheelSize",
                    DisplayName = "Wheel Size",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildToggleStepper(p, "WheelSize", "Wheel Size",
                        () => WheelSize.IsEnabled,
                        () => { WheelSize.Toggle(); },
                        () => WheelSize.Level,
                        () => { if (WheelSize.Level > 1) { WheelSize.Level--; if (WheelSize.IsEnabled) WheelSize.ApplyLevel(WheelSize.Level); } },
                        () => { if (WheelSize.Level < 20) { WheelSize.Level++; if (WheelSize.IsEnabled) WheelSize.ApplyLevel(WheelSize.Level); } },
                        1, 20, () => RefreshAll(), 10),
                    IsActive = () => WheelSize.IsEnabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "FrontWheelSize",
                    DisplayName = "Front Wheel Size",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildStepper(p, "FrontWheelSize", "Front Wheel Size",
                        () => WheelSize.FrontLevel,
                        () => { if (WheelSize.FrontLevel > 1) { WheelSize.FrontLevel--; WheelSize.IsIndividualMode = true; if (WheelSize.IsEnabled) WheelSize.IsEnabled = false; WheelSize.ApplyIndividualLevels(); } },
                        () => { if (WheelSize.FrontLevel < 20) { WheelSize.FrontLevel++; WheelSize.IsIndividualMode = true; if (WheelSize.IsEnabled) WheelSize.IsEnabled = false; WheelSize.ApplyIndividualLevels(); } },
                        1, 20, () => RefreshAll(), 10),
                    IsActive = () => WheelSize.IsIndividualMode && WheelSize.FrontLevel != 10
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "RearWheelSize",
                    DisplayName = "Rear Wheel Size",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildStepper(p, "RearWheelSize", "Rear Wheel Size",
                        () => WheelSize.RearLevel,
                        () => { if (WheelSize.RearLevel > 1) { WheelSize.RearLevel--; WheelSize.IsIndividualMode = true; if (WheelSize.IsEnabled) WheelSize.IsEnabled = false; WheelSize.ApplyIndividualLevels(); } },
                        () => { if (WheelSize.RearLevel < 20) { WheelSize.RearLevel++; WheelSize.IsIndividualMode = true; if (WheelSize.IsEnabled) WheelSize.IsEnabled = false; WheelSize.ApplyIndividualLevels(); } },
                        1, 20, () => RefreshAll(), 10),
                    IsActive = () => WheelSize.IsIndividualMode && WheelSize.RearLevel != 10
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "WideTyres",
                    DisplayName = "Wide Tyres",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildToggleSlider(p, "WideTyres", "Wide Tyres",
                        () => WideTyres.Enabled, () => WideTyres.Toggle(),
                        () => WideTyres.Level, () => WideTyres.Increase(), () => WideTyres.Decrease(),
                        20, () => (WideTyres.Level - 1) / 19f, () => RefreshAll()),
                    IsActive = () => WideTyres.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "StickyTyres",
                    DisplayName = "Sticky Tyres",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "StickyTyres", "Sticky Tyres",
                        () => StickyTyres.Enabled, () => StickyTyres.Toggle(), () => RefreshAll()),
                    IsActive = () => StickyTyres.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "TyrePressure",
                    DisplayName = "Tyre Pressure",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildToggleStepper(p, "TyrePressure", "Tyre Pressure",
                        () => TyrePressure.Enabled,
                        () => TyrePressure.Toggle(),
                        () => TyrePressure.Level,
                        () => TyrePressure.Decrease(),
                        () => TyrePressure.Increase(),
                        1, 10, () => RefreshAll(), 5),
                    IsActive = () => TyrePressure.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "BikeDamage",
                    DisplayName = "Bike Damage",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "BikeDamage", "Bike Damage",
                        () => BikeDamage.Enabled, () => BikeDamage.Toggle(), () => RefreshAll()),
                    IsActive = () => BikeDamage.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "ReverseSteering",
                    DisplayName = "Reverse Steering",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "ReverseSteering", "Reverse Steering",
                        () => ReverseSteering.Enabled, () => ReverseSteering.Toggle(), () => RefreshAll()),
                    IsActive = () => ReverseSteering.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "RubberBandSteering",
                    DisplayName = "Rubber Band Steering",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildToggleStepper(p, "RubberBandSteering", "Rubber Band Steering",
                        () => RubberBandSteering.Enabled, () => RubberBandSteering.Toggle(),
                        () => RubberBandSteering.Level, () => RubberBandSteering.Decrease(), () => RubberBandSteering.Increase(),
                        1, 10, () => RefreshAll(), 5),
                    IsActive = () => RubberBandSteering.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "CutBrakes",
                    DisplayName = "Cut Brakes",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "CutBrakes", "Cut Brakes",
                        () => CutBrakes.Enabled, () => CutBrakes.Toggle(), () => RefreshAll()),
                    IsActive = () => CutBrakes.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "BikeTorch",
                    DisplayName = "Bike Torch",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildToggleIntensityStepper(p, "BikeTorch", "Headlight",
                        () => BikeTorch.Enabled, () => BikeTorch.Toggle(),
                        () => BikeTorch.IntensityDisplay, () => BikeTorch.PrevIntensity(), () => BikeTorch.NextIntensity(),
                        () => RefreshAll()),
                    IsActive = () => BikeTorch.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "DiscoTorch",
                    DisplayName = "Disco Torch",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "DiscoTorch", "Disco Torch",
                        () => BikeTorch.DiscoEnabled, () => BikeTorch.ToggleDisco(), () => RefreshAll()),
                    IsActive = () => BikeTorch.DiscoEnabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "SuspensionHUD",
                    DisplayName = "Suspension HUD",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "SuspensionHUD", "Suspension HUD",
                        () => SuspensionHUD.Enabled, () => SuspensionHUD.Toggle(), () => RefreshAll()),
                    IsActive = () => SuspensionHUD.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "BrakeFade",
                    DisplayName = "Brake Fade",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "BrakeFade", "Brake Fade",
                        () => BrakeFade.Enabled, () => BrakeFade.Toggle(), () => RefreshAll()),
                    IsActive = () => BrakeFade.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "BrakeBalance",
                    DisplayName = "Brake Balance",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildStepper(p, "BrakeBalance", "Brake Balance",
                        () => BrakeFade.BalanceLevel,
                        () => BrakeFade.DecreaseBalance(),
                        () => BrakeFade.IncreaseBalance(),
                        1, 11, () => RefreshAll(), 6),
                    IsActive = () => BrakeFade.BalanceLevel != 6
                });

                RefreshAll();
                UIHelpers.AddScrollForwarders(pg8);
            }
            catch (System.Exception ex) { MelonLogger.Error("BikePage.CreatePage: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BikePage"); return null; }
            return pg;
        }
















        // ── Reset Tab ─────────────────────────────────────────────────
        private static void ResetBikeTab()
        {
            Suspension.SetTravelLevel(5);
            Suspension.SetStiffnessLevel(5);
            Suspension.SetDampingLevel(5);
            BouncyBike.Reset();
            if (SpiderBike.Enabled) SpiderBike.Toggle();
            if (InvisibleBike.Enabled) InvisibleBike.SetEnabled(false);
            WheelSize.Reset();
            if (WideTyres.Enabled) WideTyres.Toggle();
            WideTyres.SetLevel(5);
            if (StickyTyres.Enabled) StickyTyres.Toggle();
            if (TyrePressure.Enabled) TyrePressure.Toggle();
            TyrePressure.SetLevel(5);
            if (BikeDamage.Enabled) BikeDamage.Toggle();
            if (ReverseSteering.Enabled) ReverseSteering.Toggle();
            RubberBandSteering.Reset();
            if (TrickSetSwap.Enabled) TrickSetSwap.Disable();
            if (CutBrakes.Enabled) CutBrakes.Toggle();
            if (BikeTorch.DiscoEnabled) BikeTorch.ToggleDisco();
            if (BikeTorch.Enabled) BikeTorch.Toggle();
            if (SuspensionHUD.Enabled) SuspensionHUD.Toggle();
            if (BrakeFade.Enabled) BrakeFade.Toggle();
            BrakeFade.SetBalanceLevel(6);
            BikeSize.Level = 10;
            BikeSize.ResetToDefault();
            WheelSize.Level = 10;
            WheelSize.Reset();
        }

        // ── RefreshAll ────────────────────────────────────────────────
        public static void RefreshAll()
        {
            // Scene unload clears UI refs; skip until CreatePage runs again
            if ((object)_travelVal == null && (object)_spiderVal == null) return;

            // Suspension
            if (_travelVal) _travelVal.text = Suspension.TravelLevel.ToString();
            if (_stiffVal) _stiffVal.text = Suspension.StiffnessLevel.ToString();
            if (_dampVal) _dampVal.text = Suspension.DampingLevel.ToString();
            UIHelpers.SetBar(_travelBar, (Suspension.TravelLevel - 1) / 9f);
            UIHelpers.SetBar(_stiffBar, (Suspension.StiffnessLevel - 1) / 9f);
            UIHelpers.SetBar(_dampBar, (Suspension.DampingLevel - 1) / 9f);

            // Spider Bike
            bool spOn = SpiderBike.Enabled;
            if (_spiderVal) { _spiderVal.text = spOn ? "ON" : "OFF"; _spiderVal.color = spOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_spiderTrack, _spiderKnob, spOn);

            // Bouncy Bike
            bool bbOn = BouncyBike.Enabled;
            if (_bbVal) { _bbVal.text = bbOn ? "ON" : "OFF"; _bbVal.color = bbOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_bbTrack, _bbKnob, bbOn);
            if (_bbLvlVal) _bbLvlVal.text = BouncyBike.BouncinessLevel.ToString();
            UIHelpers.SetInteractable(_bbMinus2, BouncyBike.BouncinessLevel > 1);
            UIHelpers.SetInteractable(_bbPlus2, BouncyBike.BouncinessLevel < 10);

            // Bike Size level
            if (_bikeSizeLvlVal) _bikeSizeLvlVal.text = BikeSize.Level.ToString();
            UIHelpers.SetInteractable(_bikeSizeMinus, BikeSize.Level > 1);
            UIHelpers.SetInteractable(_bikeSizePlus, BikeSize.Level < 20);

            // Wheel Size level — both-wheels disabled while individual mode is active
            if (_wheelSizeLvlVal) _wheelSizeLvlVal.text = WheelSize.Level.ToString();
            bool bothActive = WheelSize.IsEnabled && !WheelSize.IsIndividualMode;
            UIHelpers.SetInteractable(_wheelSizeMinus2, bothActive && WheelSize.Level > 1);
            UIHelpers.SetInteractable(_wheelSizePlus2, bothActive && WheelSize.Level < 20);

            // Individual wheel levels — hide the extra rows unless they're actually off default
            if (_frontWheelLvlVal) _frontWheelLvlVal.text = WheelSize.FrontLevel.ToString();
            if (_rearWheelLvlVal) _rearWheelLvlVal.text = WheelSize.RearLevel.ToString();
            UIHelpers.SetRowActive(_frontWheelRow, WheelSize.IsIndividualMode && WheelSize.FrontLevel != 10);
            UIHelpers.SetRowActive(_rearWheelRow, WheelSize.IsIndividualMode && WheelSize.RearLevel != 10);

            // Invisible Bike
            if (_invisBikeVal) { _invisBikeVal.text = InvisibleBike.Enabled ? "ON" : "OFF"; _invisBikeVal.color = InvisibleBike.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_invisBikeTrack, _invisBikeKnob, InvisibleBike.Enabled);

            // Wheel Size
            if (_wheelSizeTogVal) { _wheelSizeTogVal.text = WheelSize.IsEnabled ? "ON" : "OFF"; _wheelSizeTogVal.color = WheelSize.IsEnabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_wheelSizeTrack, _wheelSizeKnob, WheelSize.IsEnabled);

            // Wide Tyres
            bool wtOn = WideTyres.Enabled;
            if (_wideTyresVal) { _wideTyresVal.text = wtOn ? "ON" : "OFF"; _wideTyresVal.color = wtOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_wideTyresTrack, _wideTyresKnob, wtOn);
            if (_wideTyresLvlVal) _wideTyresLvlVal.text = WideTyres.Level.ToString();
            UIHelpers.SetBar(_wideTyresBar, (WideTyres.Level - 1) / 19f);
            UIHelpers.SetInteractable(_wideTyresMinus, wtOn);
            UIHelpers.SetInteractable(_wideTyresPlus, wtOn);

            // Sticky Tyres
            bool stOn = StickyTyres.Enabled;
            if (_stickyVal) { _stickyVal.text = stOn ? "ON" : "OFF"; _stickyVal.color = stOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_stickyTrack, _stickyKnob, stOn);

            // Tyre Pressure — keep the extra Pressure row hidden until the toggle is on
            bool tpOn = TyrePressure.Enabled;
            if (_tyrePressureVal) { _tyrePressureVal.text = tpOn ? "ON" : "OFF"; _tyrePressureVal.color = tpOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_tyrePressureTrack, _tyrePressureKnob, tpOn);
            UIHelpers.SetRowActive(_tyrePressureLvlRow, tpOn);
            if (_tyrePressureLvlVal) _tyrePressureLvlVal.text = TyrePressure.Level.ToString();
            if (_tyrePressureLabelVal) _tyrePressureLabelVal.text = TyrePressure.PressureLabel;
            UIHelpers.SetInteractable(_tyrePressureMinus, tpOn && TyrePressure.Level > 1);
            UIHelpers.SetInteractable(_tyrePressurePlus, tpOn && TyrePressure.Level < 10);

            // Bike Damage
            bool bdOn = BikeDamage.Enabled;
            if (_bikeDamageVal) { _bikeDamageVal.text = bdOn ? "ON" : "OFF"; _bikeDamageVal.color = bdOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_bikeDamageTrack, _bikeDamageKnob, bdOn);

            // Reverse Steering
            bool revOn = ReverseSteering.Enabled;
            if (_revSteerVal) { _revSteerVal.text = revOn ? "ON" : "OFF"; _revSteerVal.color = revOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_revSteerTrack, _revSteerKnob, revOn);

            bool rubOn = RubberBandSteering.Enabled;
            if (_rubberVal) { _rubberVal.text = rubOn ? "ON" : "OFF"; _rubberVal.color = rubOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_rubberTrack, _rubberKnob, rubOn);
            if (_rubberLvlVal) _rubberLvlVal.text = RubberBandSteering.LevelDisplay;

            // Cut Brakes
            bool cbOn = CutBrakes.Enabled;
            if (_cutBrakesVal) { _cutBrakesVal.text = cbOn ? "ON" : "OFF"; _cutBrakesVal.color = cbOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_cutBrakesTrack, _cutBrakesKnob, cbOn);

            // Torch
            bool torch = BikeTorch.Enabled;
            if (_torchVal) { _torchVal.text = torch ? "ON" : "OFF"; _torchVal.color = torch ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_torchTrack, _torchKnob, torch);
            if (_torchIntLbl) _torchIntLbl.text = BikeTorch.IntensityDisplay;

            bool discoTorch = BikeTorch.DiscoEnabled;
            if (_torchDiscoVal) { _torchDiscoVal.text = discoTorch ? "ON" : "OFF"; _torchDiscoVal.color = discoTorch ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_torchDiscoTrack, _torchDiscoKnob, discoTorch);

            // Suspension HUD
            bool shOn = SuspensionHUD.Enabled;
            if (_shVal) { _shVal.text = shOn ? "ON" : "OFF"; _shVal.color = shOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_shTrack, _shKnob, shOn);

            // Brake Fade
            bool bfOn = BrakeFade.Enabled;
            if (_bfVal) { _bfVal.text = bfOn ? "ON" : "OFF"; _bfVal.color = bfOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_bfTrack, _bfKnob, bfOn);

            // Brake Balance
            if (_bbLabelVal) _bbLabelVal.text = BrakeFade.BalanceDisplay;
            UIHelpers.SetInteractable(_bbMinus, BrakeFade.BalanceLevel > 1);
            UIHelpers.SetInteractable(_bbPlus, BrakeFade.BalanceLevel < 11);

            if (_bikeVal)
            {
                switch (BikeSwitcher.CurrentBikeIndex)
                {
                    case 0: _bikeVal.text = "Enduro"; break;
                    case 1: _bikeVal.text = "Downhill"; break;
                    case 2: _bikeVal.text = "Hardtail"; break;
                    case 3: _bikeVal.text = "BRNZL Enduro"; break;
                    default: _bikeVal.text = "Unknown"; break;
                }
            }
            if (_tssSrcVal) _tssSrcVal.text = TrickSetSwap.CurrentSourceName;
            bool tssOn = TrickSetSwap.Enabled;
            if (_tssTogVal) { _tssTogVal.text = tssOn ? "ON" : "OFF"; _tssTogVal.color = tssOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_tssTrack, _tssKnob, tssOn);
        }

        public static void ClearUiRefs()
        {
            _travelVal = _stiffVal = _dampVal = null;
            _travelBar = _stiffBar = _dampBar = null;
            _bbRow2 = null; _bbVal = _bbLvlVal = null; _bbTrack = null; _bbKnob = null;
            _bbMinus2 = _bbPlus2 = null;
            _bikeSizeLvlVal = null; _bikeSizeMinus = _bikeSizePlus = null;
            _wheelSizeLvlVal = null; _wheelSizeMinus2 = _wheelSizePlus2 = null;
            _frontWheelLvlVal = _rearWheelLvlVal = null;
            _frontWheelRow = _rearWheelRow = null;
            _invisBikeTrack = null; _invisBikeKnob = null; _invisBikeVal = null;
            _wheelSizeTrack = null; _wheelSizeKnob = null; _wheelSizeTogVal = null;
            _wideTyresTrack = null; _wideTyresKnob = null;
            _wideTyresVal = _wideTyresLvlVal = null; _wideTyresBar = null;
            _wideTyresMinus = _wideTyresPlus = null;
            _spiderTrack = null; _spiderKnob = null; _spiderVal = null; _spiderRow = null;
            _stickyTrack = null; _stickyKnob = null; _stickyVal = null;
            _tyrePressureTrack = null; _tyrePressureKnob = null;
            _tyrePressureVal = _tyrePressureLvlVal = _tyrePressureLabelVal = null;
            _tyrePressureMinus = _tyrePressurePlus = null;
            _tyrePressureRow = _tyrePressureLvlRow = null;
            _bikeDamageRow = null; _bikeDamageVal = null; _bikeDamageTrack = null; _bikeDamageKnob = null;
            _revSteerTrack = null; _revSteerKnob = null; _revSteerVal = null;
            _rubberTrack = null; _rubberKnob = null; _rubberVal = _rubberLvlVal = null;
            _bikeVal = null; _tssSrcVal = _tssTogVal = null; _tssTrack = null; _tssKnob = null;
            _cutBrakesTrack = null; _cutBrakesKnob = null; _cutBrakesVal = null;
            _torchVal = _torchIntLbl = _torchDiscoVal = null;
            _torchTrack = _torchDiscoTrack = null; _torchKnob = _torchDiscoKnob = null;
            _invisBikeRow = _wheelSizeRow = _wideTyresRow = _stickyRow = null;
            _revSteerRow = _cutBrakesRow = _torchRow = _torchDiscoRow = null;
            _shRow = null; _shVal = null; _shTrack = null; _shKnob = null;
            _bfRow = null; _bfVal = null; _bfTrack = null; _bfKnob = null;
            _bbLabelVal = null; _bbMinus = _bbPlus = null; _bbRow = null;
        }
    }
}