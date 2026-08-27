using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using DescendersModMenu;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    public static class LavaRisingPage
    {
        private static Text _togVal;
        private static Image _track;
        private static RectTransform _knob;
        private static Text _diffLbl;
        private static Text _statusLbl;
        private static Text _mapLbl;
        private static Text _runLbl;
        private static Text _bestLbl;

        public static void CreatePage(Transform parent)
        {
            try
            {
                var pg = UIHelpers.Obj("PLava", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));

                var scrollObj = UIHelpers.Obj("Scroll", pg.transform);
                UIHelpers.Fill(UIHelpers.RT(scrollObj));
                var sr = scrollObj.AddComponent<ScrollRect>();
                sr.horizontal = false; sr.vertical = true;
                sr.movementType = ScrollRect.MovementType.Clamped;
                sr.scrollSensitivity = 25f; sr.inertia = false;

                var vp = UIHelpers.Obj("VP", scrollObj.transform);
                UIHelpers.Fill(UIHelpers.RT(vp));
                vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                vp.AddComponent<Mask>().showMaskGraphic = true;
                sr.viewport = UIHelpers.RT(vp);

                var content = UIHelpers.Obj("Content", vp.transform);
                var crt = UIHelpers.RT(content);
                crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = Vector2.zero;
                sr.content = crt;
                UIHelpers.AddScrollbar(sr);
                content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

                var c = content.transform;

                UIHelpers.SectionHeader("THE FLOOR IS LAVA", c);
                UIHelpers.InfoBoxBullets(c,
                    "Teleports you to a low spot. Climb 10m and lava rises under you — keep going up.",
                    "Getting caught or reaching the top ends the run. Harder difficulties climb faster.");

                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("CONTROLS", c);

                var enRow = UIHelpers.StatRow("Enable", c);
                FavouritesManager.RegisterStarButton("LavaRising",
                    UIHelpers.StarBtn(enRow.transform, "LavaRising", () => FavouritesManager.Toggle("LavaRising")));
                _togVal = UIHelpers.Txt("LrTV", enRow.transform, "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _togVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(enRow.transform, "LrT",
                    () => { LavaRising.Toggle(); RefreshAll(); },
                    out _track, out _knob);

                var stRow = UIHelpers.StatRow("Status", c);
                _statusLbl = UIHelpers.Txt("LrSt", stRow.transform, "Off", 11,
                    FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.TextMid);
                _statusLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 160;

                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("DIFFICULTY", c);
                UIHelpers.InfoBox(c, "How fast the lava climbs. Easy is slowest, Insane is fastest.");

                var dRow = UIHelpers.StatRow("Climb Time", c);
                UIHelpers.SmallBtn(dRow.transform, "\u25C0", () => { LavaRising.CycleDifficulty(-1); RefreshAll(); });
                _diffLbl = UIHelpers.Txt("LrDv", dRow.transform, LavaRising.DifficultyName + "  " + LavaRising.ClimbTimeDisplay,
                    12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _diffLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 140;
                UIHelpers.SmallBtn(dRow.transform, "\u25B6", () => { LavaRising.CycleDifficulty(1); RefreshAll(); });

                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("HEIGHT RECORD", c);
                UIHelpers.InfoBox(c, "Best height gained from spawn on this map. New records save automatically.");

                var mapRow = UIHelpers.StatRow("Map", c);
                _mapLbl = UIHelpers.Txt("LrMap", mapRow.transform, LavaRising.CurrentMapDisplay,
                    11, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.TextMid);
                _mapLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 200;
                _mapLbl.horizontalOverflow = HorizontalWrapMode.Overflow;

                var runRow = UIHelpers.StatRow("This run", c);
                _runLbl = UIHelpers.Txt("LrRun", runRow.transform, "0m",
                    12, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
                _runLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 80;

                var bestRow = UIHelpers.StatRow("Record", c);
                _bestLbl = UIHelpers.Txt("LrBest", bestRow.transform, LavaRising.FormatMeters(LavaRising.RecordMeters),
                    12, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.OnColor);
                _bestLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 80;

                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "LavaRising",
                    DisplayName = "The floor is LAVA",
                    TabBadge = "MODES",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "LavaRising", "The floor is LAVA",
                        () => LavaRising.Enabled, () => LavaRising.Toggle(), () => RefreshAll()),
                    IsActive = () => LavaRising.Enabled
                });

                UIHelpers.AddScrollForwarders(c);
                RefreshAll();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("LavaRisingPage: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "LavaRisingPage");
            }
        }

        public static void Tick()
        {
            if (UnityNull.Alive(_mapLbl))
                _mapLbl.text = LavaRising.CurrentMapDisplay;
            if (UnityNull.Alive(_runLbl))
            {
                _runLbl.text = LavaRising.FormatMeters(LavaRising.CurrentMeters);
                _runLbl.color = LavaRising.Enabled ? UIHelpers.Accent : UIHelpers.TextDim;
            }
            if (UnityNull.Alive(_bestLbl))
                _bestLbl.text = LavaRising.FormatMeters(LavaRising.RecordMeters);

            if (!UnityNull.Alive(_statusLbl)) return;
            if (!LavaRising.Enabled
                && LavaRising.CurrentPhase != LavaRising.Phase.Won
                && LavaRising.CurrentPhase != LavaRising.Phase.Caught)
            {
                _statusLbl.text = "Off";
                _statusLbl.color = UIHelpers.TextDim;
                return;
            }
            switch (LavaRising.CurrentPhase)
            {
                case LavaRising.Phase.Countdown:
                    _statusLbl.text = "Countdown  " + Mathf.CeilToInt(LavaRising.CountdownRemaining);
                    _statusLbl.color = UIHelpers.Orange;
                    break;
                case LavaRising.Phase.Rising:
                    _statusLbl.text = LavaRising.FormatTime(LavaRising.ClimbTime)
                        + "  " + LavaRising.FormatMeters(LavaRising.CurrentMeters)
                        + "  att " + LavaRising.Attempts;
                    _statusLbl.color = UIHelpers.Accent;
                    break;
                case LavaRising.Phase.Caught:
                    _statusLbl.text = "Caught";
                    _statusLbl.color = UIHelpers.OffColor;
                    break;
                case LavaRising.Phase.Won:
                    _statusLbl.text = "Summit  " + LavaRising.FormatTime(LavaRising.LastWinTime);
                    _statusLbl.color = UIHelpers.OnColor;
                    break;
                default:
                    _statusLbl.text = "Off";
                    _statusLbl.color = UIHelpers.TextDim;
                    break;
            }
        }

        public static void RefreshAll()
        {
            bool on = LavaRising.Enabled;
            if (_togVal)
            {
                _togVal.text = on ? "ON" : "OFF";
                _togVal.color = on ? UIHelpers.OnColor : UIHelpers.OffColor;
            }
            UIHelpers.SetToggle(_track, _knob, on);
            if (_diffLbl)
                _diffLbl.text = LavaRising.DifficultyName + "  " + LavaRising.ClimbTimeDisplay;
            if (UnityNull.Alive(_mapLbl))
                _mapLbl.text = LavaRising.CurrentMapDisplay;
            if (UnityNull.Alive(_bestLbl))
                _bestLbl.text = LavaRising.FormatMeters(LavaRising.RecordMeters);
        }

        public static void ClearUiRefs()
        {
            _togVal = null;
            _track = null;
            _knob = null;
            _diffLbl = null;
            _statusLbl = null;
            _mapLbl = null;
            _runLbl = null;
            _bestLbl = null;
        }
    }
}
