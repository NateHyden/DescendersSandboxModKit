using DescendersModMenu.Mods;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class BikePage
    {
        // ── Suspension ────────────────────────────────────────────────
        private static Text _travelVal, _stiffVal, _dampVal;
        private static Image _travelBar, _stiffBar, _dampBar;


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

        // ── Ice Mode ──────────────────────────────────────────────────
        private static Image _iceModeTrack; private static RectTransform _iceModeKnob;
        private static Text _iceModeVal;

        // ── Cut Brakes ────────────────────────────────────────────────
        private static Image _cutBrakesTrack; private static RectTransform _cutBrakesKnob;
        private static Text _cutBrakesVal;

        // ── Torch ─────────────────────────────────────────────────────
        private static Text _torchVal, _torchIntLbl;
        private static Image _torchTrack;
        private static RectTransform _torchKnob;

        // ── Row GO refs for highlight ─────────────────────────────────
        private static GameObject _invisBikeRow, _wheelSizeRow, _wideTyresRow, _stickyRow;
        private static GameObject _revSteerRow, _iceModeRow, _cutBrakesRow, _torchRow;

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
            Suspension.TravelLevel != 5 || Suspension.StiffnessLevel != 5 || Suspension.DampingLevel != 5 ||
            WheelSize.IsEnabled || WheelSize.IsIndividualMode || InvisibleBike.Enabled || BikeSize.IsModified ||
            WideTyres.Enabled || StickyTyres.Enabled ||
            ReverseSteering.Enabled || IceMode.Enabled || CutBrakes.Enabled ||
            BikeTorch.Enabled || SuspensionHUD.Enabled || BrakeFade.Enabled ||
            TyrePressure.Enabled || BikeDamage.Enabled;

        public static void GlobalReset()
        {
            if (InvisibleBike.Enabled) InvisibleBike.SetEnabled(false);
            WheelSize.Reset();
            BikeSize.ResetToDefault();
            BikeSize.Level = 10;
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
                var rstRow = UIHelpers.StatRow("", pg8);
                UIHelpers.ActionBtnOrange(rstRow.transform, "↺  Reset Tab to Defaults", () => { ResetBikeTab(); RefreshAll(); }, 186);
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
                    WheelSize.IsEnabled = !WheelSize.IsEnabled;
                    if (WheelSize.IsEnabled)
                    {
                        WheelSize.IsIndividualMode = false; // both-wheels takes over
                        WheelSize.ApplyLevel(WheelSize.Level);
                    }
                    else WheelSize.Reset();
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

                _iceModeRow = UIHelpers.StatRow("Ice Grip", pg8);
                var imr = _iceModeRow;
                _iceModeVal = UIHelpers.Txt("ImV", imr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _iceModeVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(imr.transform, "ImT", () => { IceMode.Toggle(); RefreshAll(); }, out _iceModeTrack, out _iceModeKnob);
                UIHelpers.InfoBox(pg8, "Removes tyre grip entirely. For an opposite experience to Sticky Tyres.");

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

                UIHelpers.InfoBox(pg8,
                    "Enables the bike's headlight. If the game has no built-in light, " +
                    "a spotlight is added to the front of the bike.");

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
                FavouritesManager.RegisterStarButton("BikeSize", UIHelpers.StarBtn(szr.transform, "BikeSize", () => FavouritesManager.Toggle("BikeSize")));
                FavouritesManager.RegisterStarButton("InvisibleBike", UIHelpers.StarBtn(_invisBikeRow.transform, "InvisibleBike", () => FavouritesManager.Toggle("InvisibleBike")));
                FavouritesManager.RegisterStarButton("WheelSize", UIHelpers.StarBtn(_wheelSizeRow.transform, "WheelSize", () => FavouritesManager.Toggle("WheelSize")));
                FavouritesManager.RegisterStarButton("FrontWheelSize", UIHelpers.StarBtn(_frontWheelRow.transform, "FrontWheelSize", () => FavouritesManager.Toggle("FrontWheelSize")));
                FavouritesManager.RegisterStarButton("RearWheelSize", UIHelpers.StarBtn(_rearWheelRow.transform, "RearWheelSize", () => FavouritesManager.Toggle("RearWheelSize")));
                FavouritesManager.RegisterStarButton("WideTyres", UIHelpers.StarBtn(_wideTyresRow.transform, "WideTyres", () => FavouritesManager.Toggle("WideTyres")));
                FavouritesManager.RegisterStarButton("StickyTyres", UIHelpers.StarBtn(_stickyRow.transform, "StickyTyres", () => FavouritesManager.Toggle("StickyTyres")));
                FavouritesManager.RegisterStarButton("TyrePressure", UIHelpers.StarBtn(_tyrePressureRow.transform, "TyrePressure", () => FavouritesManager.Toggle("TyrePressure")));
                FavouritesManager.RegisterStarButton("BikeDamage", UIHelpers.StarBtn(_bikeDamageRow.transform, "BikeDamage", () => FavouritesManager.Toggle("BikeDamage")));
                FavouritesManager.RegisterStarButton("ReverseSteering", UIHelpers.StarBtn(_revSteerRow.transform, "ReverseSteering", () => FavouritesManager.Toggle("ReverseSteering")));
                FavouritesManager.RegisterStarButton("IceGrip", UIHelpers.StarBtn(_iceModeRow.transform, "IceGrip", () => FavouritesManager.Toggle("IceGrip")));
                FavouritesManager.RegisterStarButton("CutBrakes", UIHelpers.StarBtn(_cutBrakesRow.transform, "CutBrakes", () => FavouritesManager.Toggle("CutBrakes")));
                FavouritesManager.RegisterStarButton("BikeTorch", UIHelpers.StarBtn(_torchRow.transform, "BikeTorch", () => FavouritesManager.Toggle("BikeTorch")));
                FavouritesManager.RegisterStarButton("SuspensionHUD", UIHelpers.StarBtn(_shRow.transform, "SuspensionHUD", () => FavouritesManager.Toggle("SuspensionHUD")));
                FavouritesManager.RegisterStarButton("BrakeFade", UIHelpers.StarBtn(_bfRow.transform, "BrakeFade", () => FavouritesManager.Toggle("BrakeFade")));
                FavouritesManager.RegisterStarButton("BrakeBalance", UIHelpers.StarBtn(_bbRow.transform, "BrakeBalance", () => FavouritesManager.Toggle("BrakeBalance")));

                // ── FACTORY REGISTRATIONS (Bike tab mods) ──────────────
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
                        () => { WheelSize.IsEnabled = !WheelSize.IsEnabled; if (WheelSize.IsEnabled) { WheelSize.IsIndividualMode = false; WheelSize.ApplyLevel(WheelSize.Level); } else WheelSize.Reset(); },
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
                    Id = "IceGrip",
                    DisplayName = "Ice Grip",
                    TabBadge = "BIKE",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "IceGrip", "Ice Grip",
                        () => IceMode.Enabled, () => IceMode.Toggle(), () => RefreshAll()),
                    IsActive = () => IceMode.Enabled
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
            catch (System.Exception ex) { MelonLogger.Error("BikePage.CreatePage: " + ex.Message); return null; }
            return pg;
        }
















        // ── Reset Tab ─────────────────────────────────────────────────
        private static void ResetBikeTab()
        {
            Suspension.SetTravelLevel(5);
            Suspension.SetStiffnessLevel(5);
            Suspension.SetDampingLevel(5);
            if (InvisibleBike.Enabled) InvisibleBike.SetEnabled(false);
            WheelSize.Reset();
            if (WideTyres.Enabled) WideTyres.Toggle();
            WideTyres.SetLevel(5);
            if (StickyTyres.Enabled) StickyTyres.Toggle();
            if (TyrePressure.Enabled) TyrePressure.Toggle();
            TyrePressure.SetLevel(5);
            if (BikeDamage.Enabled) BikeDamage.Toggle();
            if (ReverseSteering.Enabled) ReverseSteering.Toggle();
            if (IceMode.Enabled) IceMode.Toggle();
            if (CutBrakes.Enabled) CutBrakes.Toggle();
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
            // Suspension
            if (_travelVal) _travelVal.text = Suspension.TravelLevel.ToString();
            if (_stiffVal) _stiffVal.text = Suspension.StiffnessLevel.ToString();
            if (_dampVal) _dampVal.text = Suspension.DampingLevel.ToString();
            UIHelpers.SetBar(_travelBar, (Suspension.TravelLevel - 1) / 9f);
            UIHelpers.SetBar(_stiffBar, (Suspension.StiffnessLevel - 1) / 9f);
            UIHelpers.SetBar(_dampBar, (Suspension.DampingLevel - 1) / 9f);

            // Bike Size level
            if (_bikeSizeLvlVal) _bikeSizeLvlVal.text = BikeSize.Level.ToString();
            if ((object)_bikeSizeMinus != null) _bikeSizeMinus.interactable = BikeSize.Level > 1;
            if ((object)_bikeSizePlus != null) _bikeSizePlus.interactable = BikeSize.Level < 20;

            // Wheel Size level — both-wheels disabled while individual mode is active
            if (_wheelSizeLvlVal) _wheelSizeLvlVal.text = WheelSize.Level.ToString();
            bool bothActive = WheelSize.IsEnabled && !WheelSize.IsIndividualMode;
            if ((object)_wheelSizeMinus2 != null) _wheelSizeMinus2.interactable = bothActive && WheelSize.Level > 1;
            if ((object)_wheelSizePlus2 != null) _wheelSizePlus2.interactable = bothActive && WheelSize.Level < 20;

            // Individual wheel levels
            if (_frontWheelLvlVal) _frontWheelLvlVal.text = WheelSize.FrontLevel.ToString();
            if (_rearWheelLvlVal) _rearWheelLvlVal.text = WheelSize.RearLevel.ToString();
            UIHelpers.SetRowActive(_frontWheelRow, WheelSize.IsIndividualMode && WheelSize.FrontLevel != 10);
            UIHelpers.SetRowActive(_rearWheelRow, WheelSize.IsIndividualMode && WheelSize.RearLevel != 10);

            // Invisible Bike
            if (_invisBikeVal) { _invisBikeVal.text = InvisibleBike.Enabled ? "ON" : "OFF"; _invisBikeVal.color = InvisibleBike.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_invisBikeTrack, _invisBikeKnob, InvisibleBike.Enabled);
            UIHelpers.SetRowActive(_invisBikeRow, InvisibleBike.Enabled);

            // Wheel Size
            if (_wheelSizeTogVal) { _wheelSizeTogVal.text = WheelSize.IsEnabled ? "ON" : "OFF"; _wheelSizeTogVal.color = WheelSize.IsEnabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_wheelSizeTrack, _wheelSizeKnob, WheelSize.IsEnabled);
            UIHelpers.SetRowActive(_wheelSizeRow, WheelSize.IsEnabled);

            // Wide Tyres
            bool wtOn = WideTyres.Enabled;
            if (_wideTyresVal) { _wideTyresVal.text = wtOn ? "ON" : "OFF"; _wideTyresVal.color = wtOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_wideTyresTrack, _wideTyresKnob, wtOn);
            if (_wideTyresLvlVal) _wideTyresLvlVal.text = WideTyres.Level.ToString();
            UIHelpers.SetBar(_wideTyresBar, (WideTyres.Level - 1) / 19f);
            if ((object)_wideTyresMinus != null) _wideTyresMinus.interactable = wtOn;
            if ((object)_wideTyresPlus != null) _wideTyresPlus.interactable = wtOn;
            UIHelpers.SetRowActive(_wideTyresRow, wtOn);

            // Sticky Tyres
            bool stOn = StickyTyres.Enabled;
            if (_stickyVal) { _stickyVal.text = stOn ? "ON" : "OFF"; _stickyVal.color = stOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_stickyTrack, _stickyKnob, stOn);
            UIHelpers.SetRowActive(_stickyRow, stOn);

            // Tyre Pressure
            bool tpOn = TyrePressure.Enabled;
            if (_tyrePressureVal) { _tyrePressureVal.text = tpOn ? "ON" : "OFF"; _tyrePressureVal.color = tpOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_tyrePressureTrack, _tyrePressureKnob, tpOn);
            UIHelpers.SetRowActive(_tyrePressureRow, tpOn);
            UIHelpers.SetRowActive(_tyrePressureLvlRow, tpOn);
            if (_tyrePressureLvlVal) _tyrePressureLvlVal.text = TyrePressure.Level.ToString();
            if (_tyrePressureLabelVal) _tyrePressureLabelVal.text = TyrePressure.PressureLabel;
            if ((object)_tyrePressureMinus != null) _tyrePressureMinus.interactable = tpOn && TyrePressure.Level > 1;
            if ((object)_tyrePressurePlus != null) _tyrePressurePlus.interactable = tpOn && TyrePressure.Level < 10;

            // Bike Damage
            bool bdOn = BikeDamage.Enabled;
            if (_bikeDamageVal) { _bikeDamageVal.text = bdOn ? "ON" : "OFF"; _bikeDamageVal.color = bdOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_bikeDamageTrack, _bikeDamageKnob, bdOn);

            // Reverse Steering
            bool revOn = ReverseSteering.Enabled;
            if (_revSteerVal) { _revSteerVal.text = revOn ? "ON" : "OFF"; _revSteerVal.color = revOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_revSteerTrack, _revSteerKnob, revOn);
            UIHelpers.SetRowActive(_revSteerRow, revOn);

            // Ice Grip
            bool imOn = IceMode.Enabled;
            if (_iceModeVal) { _iceModeVal.text = imOn ? "ON" : "OFF"; _iceModeVal.color = imOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_iceModeTrack, _iceModeKnob, imOn);
            UIHelpers.SetRowActive(_iceModeRow, imOn);

            // Cut Brakes
            bool cbOn = CutBrakes.Enabled;
            if (_cutBrakesVal) { _cutBrakesVal.text = cbOn ? "ON" : "OFF"; _cutBrakesVal.color = cbOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_cutBrakesTrack, _cutBrakesKnob, cbOn);
            UIHelpers.SetRowActive(_cutBrakesRow, cbOn);

            // Torch
            bool torch = BikeTorch.Enabled;
            if (_torchVal) { _torchVal.text = torch ? "ON" : "OFF"; _torchVal.color = torch ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_torchTrack, _torchKnob, torch);
            UIHelpers.SetRowActive(_torchRow, torch);
            if (_torchIntLbl) _torchIntLbl.text = BikeTorch.IntensityDisplay;

            // Suspension HUD
            bool shOn = SuspensionHUD.Enabled;
            if (_shVal) { _shVal.text = shOn ? "ON" : "OFF"; _shVal.color = shOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_shTrack, _shKnob, shOn);
            UIHelpers.SetRowActive(_shRow, shOn);

            // Brake Fade
            bool bfOn = BrakeFade.Enabled;
            if (_bfVal) { _bfVal.text = bfOn ? "ON" : "OFF"; _bfVal.color = bfOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_bfTrack, _bfKnob, bfOn);
            UIHelpers.SetRowActive(_bfRow, bfOn);

            // Brake Balance
            UIHelpers.SetRowActive(_bbRow, true);
            if (_bbLabelVal) _bbLabelVal.text = BrakeFade.BalanceDisplay;
            if ((object)_bbMinus != null) _bbMinus.interactable = BrakeFade.BalanceLevel > 1;
            if ((object)_bbPlus != null) _bbPlus.interactable = BrakeFade.BalanceLevel < 11;
        }
    }
}