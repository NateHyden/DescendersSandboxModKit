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
        // ── Wheel Size ────────────────────────────────────────────────
        private static Text _wheelSizeLvlVal;
        private static UnityEngine.UI.Button _wheelSizeMinus2, _wheelSizePlus2;
        private static Text _frontWheelLvlVal;
        private static Text _rearWheelLvlVal;
        private static GameObject _frontWheelRow, _rearWheelRow;
        private static Image _invisBikeTrack; private static RectTransform _invisBikeKnob;
        private static Text _invisBikeVal;

        // ── Wide Tyres ────────────────────────────────────────────────
        private static Text _wideTyresLvlVal; private static Image _wideTyresBar;
        private static UnityEngine.UI.Button _wideTyresMinus, _wideTyresPlus;

        // ── Spider Bike ───────────────────────────────────────────────
        private static Image _spiderTrack; private static RectTransform _spiderKnob;
        private static Text _spiderVal;
        private static GameObject _spiderRow;

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

        private static GameObject _invisBikeRow, _wheelSizeRow, _wideTyresRow;
        private static GameObject _revSteerRow, _cutBrakesRow, _torchRow, _torchDiscoRow;

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
            WheelSize.IsModified ||
            InvisibleBike.Enabled || BikeSize.IsModified ||
            WideTyres.IsModified ||
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
            WideTyres.Reset();
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
                UIHelpers.InfoBox(pg8, "Ride on walls and ceilings. You won't fall off from the tilt while this is on.");

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
                _travelVal = UIHelpers.Txt("TvV", tr.transform, Suspension.PercentDisplay(Suspension.TravelLevel), 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _travelVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
                UIHelpers.SmallBtn(tr.transform, "-", () => { Suspension.TravelDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(tr.transform, "+", () => { Suspension.TravelIncrease(); RefreshAll(); });

                var sr = UIHelpers.StatRow("Stiffness", pg8);
                _stiffBar = UIHelpers.MakeBar("StB", sr.transform, (Suspension.StiffnessLevel - 1) / 9f);
                _stiffVal = UIHelpers.Txt("StV", sr.transform, Suspension.PercentDisplay(Suspension.StiffnessLevel), 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _stiffVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
                UIHelpers.SmallBtn(sr.transform, "-", () => { Suspension.StiffnessDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(sr.transform, "+", () => { Suspension.StiffnessIncrease(); RefreshAll(); });

                var dr = UIHelpers.StatRow("Damping", pg8);
                _dampBar = UIHelpers.MakeBar("DpB", dr.transform, (Suspension.DampingLevel - 1) / 9f);
                _dampVal = UIHelpers.Txt("DpV", dr.transform, Suspension.PercentDisplay(Suspension.DampingLevel), 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _dampVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
                UIHelpers.SmallBtn(dr.transform, "-", () => { Suspension.DampingDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(dr.transform, "+", () => { Suspension.DampingIncrease(); RefreshAll(); });

                UIHelpers.InfoBox(pg8, "0% is normal. Lower = softer / less travel, higher = firmer / more travel.");

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
                UIHelpers.InfoBox(pg8, "Bike bounces on landing. 1 = tiny bounce, 10 = superball. Pair with No Bail if hard bounces crash you.");

                UIHelpers.Divider(pg8);

                // ── BIKE SIZE ─────────────────────────────────────────
                UIHelpers.SectionHeader("BIKE SIZE", pg8);

                var szr = UIHelpers.StatRow("Size", pg8);
                _bikeSizeMinus = UIHelpers.SmallBtn(szr.transform, "◀", () =>
                {
                    if (BikeSize.Level > 1) { BikeSize.Decrease(); RefreshAll(); }
                });
                _bikeSizeLvlVal = UIHelpers.Txt("BsL", szr.transform, BikeSize.PercentDisplay, 13,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _bikeSizeLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                _bikeSizePlus = UIHelpers.SmallBtn(szr.transform, "▶", () =>
                {
                    if (BikeSize.Level < 20) { BikeSize.Increase(); RefreshAll(); }
                });

                UIHelpers.Divider(pg8);

                UIHelpers.InfoBox(pg8, "0% is normal size. Negative = smaller, positive = bigger.");

                UIHelpers.Divider(pg8);

                // ── BIKE PARTS ────────────────────────────────────────
                UIHelpers.SectionHeader("BIKE PARTS", pg8);

                _invisBikeRow = UIHelpers.StatRow("Invisible Bike", pg8);
                var ibr = _invisBikeRow;
                _invisBikeVal = UIHelpers.Txt("IbV", ibr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _invisBikeVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(ibr.transform, "IbT", () =>
                {
                    InvisibleBike.Toggle();
                    RefreshAll();
                }, out _invisBikeTrack, out _invisBikeKnob);

                _wheelSizeRow = UIHelpers.StatRow("Wheel Size", pg8);
                var gwr = _wheelSizeRow;
                _wheelSizeMinus2 = UIHelpers.SmallBtn(gwr.transform, "◀", () =>
                {
                    WheelSize.Decrease();
                    RefreshAll();
                });
                _wheelSizeLvlVal = UIHelpers.Txt("WsL", gwr.transform, WheelSize.PercentDisplay(WheelSize.Level), 13,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _wheelSizeLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                _wheelSizePlus2 = UIHelpers.SmallBtn(gwr.transform, "▶", () =>
                {
                    WheelSize.Increase();
                    RefreshAll();
                });

                UIHelpers.InfoBox(pg8, "0% is normal. Return to 0% to undo. Negative = smaller, positive = bigger. Or set front and rear below.");

                _frontWheelRow = UIHelpers.StatRow("Front Wheel Size", pg8);
                UIHelpers.SmallBtn(_frontWheelRow.transform, "◀", () =>
                {
                    WheelSize.DecreaseFront();
                    RefreshAll();
                });
                _frontWheelLvlVal = UIHelpers.Txt("FwL", _frontWheelRow.transform, WheelSize.PercentDisplay(WheelSize.FrontLevel), 13,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _frontWheelLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                UIHelpers.SmallBtn(_frontWheelRow.transform, "▶", () =>
                {
                    WheelSize.IncreaseFront();
                    RefreshAll();
                });

                _rearWheelRow = UIHelpers.StatRow("Rear Wheel Size", pg8);
                UIHelpers.SmallBtn(_rearWheelRow.transform, "◀", () =>
                {
                    WheelSize.DecreaseRear();
                    RefreshAll();
                });
                _rearWheelLvlVal = UIHelpers.Txt("RwL", _rearWheelRow.transform, WheelSize.PercentDisplay(WheelSize.RearLevel), 13,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _rearWheelLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                UIHelpers.SmallBtn(_rearWheelRow.transform, "▶", () =>
                {
                    WheelSize.IncreaseRear();
                    RefreshAll();
                });

                _wideTyresRow = UIHelpers.StatRow("Wide Tyres", pg8);
                var wtr = _wideTyresRow;
                _wideTyresBar = UIHelpers.MakeBar("WtB", wtr.transform, (WideTyres.Level - 1) / 19f);
                _wideTyresLvlVal = UIHelpers.Txt("WtL", wtr.transform, WideTyres.PercentDisplay, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _wideTyresLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                _wideTyresMinus = UIHelpers.SmallBtn(wtr.transform, "-", () => { WideTyres.Decrease(); RefreshAll(); });
                _wideTyresPlus = UIHelpers.SmallBtn(wtr.transform, "+", () => { WideTyres.Increase(); RefreshAll(); });
                UIHelpers.InfoBox(pg8, "0% is normal tyre width. Return to 0% to undo. Negative = skinnier, positive = wider.");

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
                UIHelpers.InfoBox(pg8, "Crashes make the bike pull sideways. Hard hits can knock the rear wheel off. Press RESET to fix.");

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
                UIHelpers.InfoBox(pg8, "Steering and lean feel delayed. Higher = more lag.");

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
                UIHelpers.InfoBox(pg8, "Hard braking overheats the brakes until they fade or fail. Ease off to cool down. Watch the corner meter.");

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
                        () => RefreshAll(), 5,
                        () => Suspension.PercentDisplay(Suspension.TravelLevel),
                        () => Suspension.PercentDisplay(Suspension.StiffnessLevel),
                        () => Suspension.PercentDisplay(Suspension.DampingLevel)),
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
                        1, BouncyBike.MaxLevel, () => RefreshAll(), 5),
                    IsActive = () => BouncyBike.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "BikeSize",
                    DisplayName = "Bike Size",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildStepper(p, "BikeSize", "Bike Size",
                        () => BikeSize.Level,
                        () => BikeSize.Decrease(),
                        () => BikeSize.Increase(),
                        1, 20, () => RefreshAll(), 10, () => BikeSize.PercentDisplay),
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
                    BuildControls = (p) => FavsPage.BuildStepper(p, "WheelSize", "Wheel Size",
                        () => WheelSize.Level,
                        () => WheelSize.Decrease(),
                        () => WheelSize.Increase(),
                        1, 20, () => RefreshAll(), 10, () => WheelSize.PercentDisplay(WheelSize.Level)),
                    IsActive = () => WheelSize.IsModified && !WheelSize.IsIndividualMode
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "FrontWheelSize",
                    DisplayName = "Front Wheel Size",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildStepper(p, "FrontWheelSize", "Front Wheel Size",
                        () => WheelSize.FrontLevel,
                        () => WheelSize.DecreaseFront(),
                        () => WheelSize.IncreaseFront(),
                        1, 20, () => RefreshAll(), 10, () => WheelSize.PercentDisplay(WheelSize.FrontLevel)),
                    IsActive = () => WheelSize.IsIndividualMode && WheelSize.FrontLevel != 10
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "RearWheelSize",
                    DisplayName = "Rear Wheel Size",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildStepper(p, "RearWheelSize", "Rear Wheel Size",
                        () => WheelSize.RearLevel,
                        () => WheelSize.DecreaseRear(),
                        () => WheelSize.IncreaseRear(),
                        1, 20, () => RefreshAll(), 10, () => WheelSize.PercentDisplay(WheelSize.RearLevel)),
                    IsActive = () => WheelSize.IsIndividualMode && WheelSize.RearLevel != 10
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "WideTyres",
                    DisplayName = "Wide Tyres",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildSliderOnly(p, "WideTyres", "Wide Tyres",
                        () => WideTyres.Level, () => WideTyres.Increase(), () => WideTyres.Decrease(),
                        () => (WideTyres.Level - 1) / 19f, () => RefreshAll(),
                        () => WideTyres.PercentDisplay, () => WideTyres.IsModified),
                    IsActive = () => WideTyres.IsModified
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
            WideTyres.Reset();
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
            WheelSize.Reset();
            WideTyres.Reset();
        }

        // ── RefreshAll ────────────────────────────────────────────────
        public static void RefreshAll()
        {
            if (!UnityNull.Alive(_travelVal) && !UnityNull.Alive(_spiderVal)) return;

            if (_travelVal) _travelVal.text = Suspension.PercentDisplay(Suspension.TravelLevel);
            if (_stiffVal) _stiffVal.text = Suspension.PercentDisplay(Suspension.StiffnessLevel);
            if (_dampVal) _dampVal.text = Suspension.PercentDisplay(Suspension.DampingLevel);
            UIHelpers.SetBar(_travelBar, (Suspension.TravelLevel - 1) / 9f);
            UIHelpers.SetBar(_stiffBar, (Suspension.StiffnessLevel - 1) / 9f);
            UIHelpers.SetBar(_dampBar, (Suspension.DampingLevel - 1) / 9f);

            bool spOn = SpiderBike.Enabled;
            if (_spiderVal) { _spiderVal.text = spOn ? "ON" : "OFF"; _spiderVal.color = spOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_spiderTrack, _spiderKnob, spOn);

            bool bbOn = BouncyBike.Enabled;
            if (_bbVal) { _bbVal.text = bbOn ? "ON" : "OFF"; _bbVal.color = bbOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_bbTrack, _bbKnob, bbOn);
            if (_bbLvlVal) _bbLvlVal.text = BouncyBike.BouncinessLevel.ToString();
            UIHelpers.SetInteractable(_bbMinus2, BouncyBike.BouncinessLevel > 1);
            UIHelpers.SetInteractable(_bbPlus2, BouncyBike.BouncinessLevel < BouncyBike.MaxLevel);

            if (_bikeSizeLvlVal) _bikeSizeLvlVal.text = BikeSize.PercentDisplay;
            UIHelpers.SetInteractable(_bikeSizeMinus, BikeSize.Level > 1);
            UIHelpers.SetInteractable(_bikeSizePlus, BikeSize.Level < 20);

            if (_wheelSizeLvlVal) _wheelSizeLvlVal.text = WheelSize.PercentDisplay(WheelSize.Level);
            UIHelpers.SetInteractable(_wheelSizeMinus2, WheelSize.Level > 1);
            UIHelpers.SetInteractable(_wheelSizePlus2, WheelSize.Level < 20);

            if (_frontWheelLvlVal) _frontWheelLvlVal.text = WheelSize.PercentDisplay(WheelSize.FrontLevel);
            if (_rearWheelLvlVal) _rearWheelLvlVal.text = WheelSize.PercentDisplay(WheelSize.RearLevel);
            UIHelpers.SetRowActive(_frontWheelRow, WheelSize.IsIndividualMode && WheelSize.FrontLevel != 10);
            UIHelpers.SetRowActive(_rearWheelRow, WheelSize.IsIndividualMode && WheelSize.RearLevel != 10);
            UIHelpers.SetRowActive(_wheelSizeRow, WheelSize.IsModified && !WheelSize.IsIndividualMode);

            if (_invisBikeVal) { _invisBikeVal.text = InvisibleBike.Enabled ? "ON" : "OFF"; _invisBikeVal.color = InvisibleBike.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_invisBikeTrack, _invisBikeKnob, InvisibleBike.Enabled);

            if (_wideTyresLvlVal) _wideTyresLvlVal.text = WideTyres.PercentDisplay;
            UIHelpers.SetBar(_wideTyresBar, (WideTyres.Level - 1) / 19f);
            UIHelpers.SetInteractable(_wideTyresMinus, WideTyres.Level > 1);
            UIHelpers.SetInteractable(_wideTyresPlus, WideTyres.Level < 20);
            UIHelpers.SetRowActive(_wideTyresRow, WideTyres.IsModified);

            bool tpOn = TyrePressure.Enabled;
            if (_tyrePressureVal) { _tyrePressureVal.text = tpOn ? "ON" : "OFF"; _tyrePressureVal.color = tpOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_tyrePressureTrack, _tyrePressureKnob, tpOn);
            UIHelpers.SetRowActive(_tyrePressureLvlRow, tpOn);
            if (_tyrePressureLvlVal) _tyrePressureLvlVal.text = TyrePressure.Level.ToString();
            if (_tyrePressureLabelVal) _tyrePressureLabelVal.text = TyrePressure.PressureLabel;
            UIHelpers.SetInteractable(_tyrePressureMinus, tpOn && TyrePressure.Level > 1);
            UIHelpers.SetInteractable(_tyrePressurePlus, tpOn && TyrePressure.Level < 10);

            bool bdOn = BikeDamage.Enabled;
            if (_bikeDamageVal) { _bikeDamageVal.text = bdOn ? "ON" : "OFF"; _bikeDamageVal.color = bdOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_bikeDamageTrack, _bikeDamageKnob, bdOn);

            bool revOn = ReverseSteering.Enabled;
            if (_revSteerVal) { _revSteerVal.text = revOn ? "ON" : "OFF"; _revSteerVal.color = revOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_revSteerTrack, _revSteerKnob, revOn);

            bool rubOn = RubberBandSteering.Enabled;
            if (_rubberVal) { _rubberVal.text = rubOn ? "ON" : "OFF"; _rubberVal.color = rubOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_rubberTrack, _rubberKnob, rubOn);
            if (_rubberLvlVal) _rubberLvlVal.text = RubberBandSteering.LevelDisplay;

            bool cbOn = CutBrakes.Enabled;
            if (_cutBrakesVal) { _cutBrakesVal.text = cbOn ? "ON" : "OFF"; _cutBrakesVal.color = cbOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_cutBrakesTrack, _cutBrakesKnob, cbOn);

            bool torch = BikeTorch.Enabled;
            if (_torchVal) { _torchVal.text = torch ? "ON" : "OFF"; _torchVal.color = torch ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_torchTrack, _torchKnob, torch);
            if (_torchIntLbl) _torchIntLbl.text = BikeTorch.IntensityDisplay;

            bool discoTorch = BikeTorch.DiscoEnabled;
            if (_torchDiscoVal) { _torchDiscoVal.text = discoTorch ? "ON" : "OFF"; _torchDiscoVal.color = discoTorch ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_torchDiscoTrack, _torchDiscoKnob, discoTorch);

            bool shOn = SuspensionHUD.Enabled;
            if (_shVal) { _shVal.text = shOn ? "ON" : "OFF"; _shVal.color = shOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_shTrack, _shKnob, shOn);

            bool bfOn = BrakeFade.Enabled;
            if (_bfVal) { _bfVal.text = bfOn ? "ON" : "OFF"; _bfVal.color = bfOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_bfTrack, _bfKnob, bfOn);

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
            _wideTyresLvlVal = null; _wideTyresBar = null;
            _wideTyresMinus = _wideTyresPlus = null;
            _wheelSizeLvlVal = null;
            _wheelSizeMinus2 = _wheelSizePlus2 = null;
            _wideTyresMinus = _wideTyresPlus = null;
            _spiderTrack = null; _spiderKnob = null; _spiderVal = null; _spiderRow = null;
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
            _invisBikeRow = _wheelSizeRow = _wideTyresRow = null;
            _revSteerRow = _cutBrakesRow = _torchRow = _torchDiscoRow = null;
            _shRow = null; _shVal = null; _shTrack = null; _shKnob = null;
            _bfRow = null; _bfVal = null; _bfTrack = null; _bfKnob = null;
            _bbLabelVal = null; _bbMinus = _bbPlus = null; _bbRow = null;
        }
    }
}

