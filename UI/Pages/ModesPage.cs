using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    public static class ModesPage
    {
        private static int _activeTab = 0;
        private static readonly List<GameObject> _tabPages = new List<GameObject>();
        private static readonly List<Text> _tabTxts = new List<Text>();
        private static readonly List<Image> _tabBgs = new List<Image>();

        // Earthquake UI refs
        private static Text _eqIntVal, _eqFreqVal, _eqDurVal, _eqTogVal, _eqModeLbl;
        private static Image _eqIntBar, _eqFreqBar, _eqDurBar, _eqTrack;
        private static RectTransform _eqKnob;
        private static GameObject _eqFreqRow;

        // Trick Attack UI refs
        private static Text _taTogVal, _taTargetInput, _taTimeLbl;
        private static Image _taTrack;
        private static RectTransform _taKnob;
        private static string _taBuffer = "";
        private static Text _pcTogVal, _pcStatusTxt;
        private static Image _pcTrack;
        private static RectTransform _pcKnob;

        public static bool IsAnyActive =>
            AvalancheMode.Enabled || EarthquakeMode.Enabled ||
            PoliceChaseMode.Enabled || TrickAttackMode.CurrentState != TrickAttackMode.State.Off ||
            BoulderDodgeMode.Enabled || SurvivalMode.Enabled;

        public static void CreatePage(Transform parent)
        {
            try
            {
                _activeTab = 0;
                _tabPages.Clear();
                _tabTxts.Clear();
                _tabBgs.Clear();

                var pg = UIHelpers.Obj("PModes", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));
                var rootVlg = pg.AddComponent<VerticalLayoutGroup>();
                rootVlg.spacing = 0;
                rootVlg.padding = new RectOffset(0, 0, 0, 0);
                rootVlg.childAlignment = TextAnchor.UpperLeft;
                rootVlg.childForceExpandWidth = true;
                rootVlg.childForceExpandHeight = false;

                var tabBar = UIHelpers.Obj("ModeTabBar", pg.transform);
                tabBar.AddComponent<Image>().color = UIHelpers.WinOuter;
                var tbLE = tabBar.AddComponent<LayoutElement>();
                tbLE.preferredHeight = 38; tbLE.minHeight = 38; tbLE.flexibleHeight = 0;
                var tbHlg = tabBar.AddComponent<HorizontalLayoutGroup>();
                tbHlg.spacing = 1;
                tbHlg.padding = new RectOffset(8, 8, 0, 0);
                tbHlg.childAlignment = TextAnchor.LowerLeft;
                tbHlg.childForceExpandWidth = false;
                tbHlg.childForceExpandHeight = false;

                var contentArea = UIHelpers.Obj("ModeContent", pg.transform);
                var caLE = contentArea.AddComponent<LayoutElement>();
                caLE.flexibleHeight = 1; caLE.flexibleWidth = 1;
                UIHelpers.Fill(UIHelpers.RT(contentArea));

                // ── Sub-tabs ──────────────────────────────────────────
                AddSubTab(tabBar.transform, contentArea.transform, "Avalanche",
                    t => AvalanchePage.CreatePage(t));
                AddSubTab(tabBar.transform, contentArea.transform, "Earthquake",
                    t => BuildEarthquakePage(t));
                AddSubTab(tabBar.transform, contentArea.transform, "Police Chase",
                    t => BuildPoliceChasePage(t));
                AddSubTab(tabBar.transform, contentArea.transform, "Trick Attack",
                    t => BuildTrickAttackPage(t));
                AddSubTab(tabBar.transform, contentArea.transform, "Boulder Dodge",
                    t => BuildBoulderDodgePage(t));
                AddSubTab(tabBar.transform, contentArea.transform, "Survival",
                    t => BuildSurvivalPage(t));

                // ── "EXPERIMENTAL" badge — pushed to the right ────────
                var spacer = UIHelpers.Obj("TabSpacer", tabBar.transform);
                spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

                var expBadge = UIHelpers.Panel("ExpBadge", tabBar.transform, UIHelpers.OrangeDim, UIHelpers.BtnSp);
                var expBadgeLE = expBadge.AddComponent<LayoutElement>();
                expBadgeLE.preferredWidth = 96; expBadgeLE.minWidth = 96;
                expBadgeLE.preferredHeight = 22; expBadgeLE.minHeight = 22;
                expBadgeLE.flexibleHeight = 0; expBadgeLE.flexibleWidth = 0;
                var expBdr = UIHelpers.Panel("ExpBdr", expBadge.transform, UIHelpers.OrangeBdr, UIHelpers.BtnSp);
                expBdr.GetComponent<Image>().raycastTarget = false;
                UIHelpers.Fill(UIHelpers.RT(expBdr));
                expBdr.AddComponent<LayoutElement>().ignoreLayout = true;
                var expTxt = UIHelpers.Txt("ExpTxt", expBadge.transform,
                    "EXPERIMENTAL", 9, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Orange);
                UIHelpers.Fill(UIHelpers.RT(expTxt.gameObject));

                SwitchTab(0);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("ModesPage.CreatePage: " + ex.Message);
            }
        }

        // ── Earthquake page ───────────────────────────────────────────
        private static void BuildEarthquakePage(Transform parent)
        {
            try
            {
                var pg = UIHelpers.Obj("PEQ", parent);
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
                content.AddComponent<ContentSizeFitter>().verticalFit =
                    ContentSizeFitter.FitMode.PreferredSize;
                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

                var c = content.transform;

                UIHelpers.SectionHeader("EARTHQUAKE MODE", c);

                // Description
                UIHelpers.InfoBox(c,
                    "Simulates an earthquake while you ride. Quake events strike at intervals " +
                    "and last for the chosen duration. During each event rapid random physics " +
                    "impulses throw your bike sideways and forward/back.");
                UIHelpers.InfoBox(c,
                    "The camera shakes for the full duration of each quake. " +
                    "Intensity = force per hit.  Frequency = time between events.  " +
                    "Duration = how long each quake lasts.");

                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("CONTROLS", c);

                // Enable toggle
                var enRow = UIHelpers.StatRow("Enable", c);
                _eqTogVal = UIHelpers.Txt("EqTV", enRow.transform, "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _eqTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(enRow.transform, "EqT",
                    () => { EarthquakeMode.Toggle(); RefreshEarthquake(); },
                    out _eqTrack, out _eqKnob);

                // Intensity
                var intRow = UIHelpers.StatRow("Intensity", c);
                _eqIntBar = UIHelpers.MakeBar("EqIB", intRow.transform,
                    (EarthquakeMode.IntensityLevel - 1) / 9f);
                _eqIntVal = UIHelpers.Txt("EqIV", intRow.transform,
                    EarthquakeMode.IntensityDisplay, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _eqIntVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
                UIHelpers.SmallBtn(intRow.transform, "-",
                    () => { EarthquakeMode.DecreaseIntensity(); RefreshEarthquake(); });
                UIHelpers.SmallBtn(intRow.transform, "+",
                    () => { EarthquakeMode.IncreaseIntensity(); RefreshEarthquake(); });

                // Frequency Mode buttons
                UIHelpers.SectionHeader("FREQUENCY MODE", c);
                var modeRow = UIHelpers.StatRow("Mode", c);
                _eqModeLbl = UIHelpers.Txt("EqML", modeRow.transform,
                    EarthquakeMode.FrequencyModeName, 11,
                    FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.Accent);
                _eqModeLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;
                UIHelpers.ActionBtn(modeRow.transform, "Timed", () => { EarthquakeMode.SetFrequencyMode(0); RefreshEarthquake(); }, 52);
                UIHelpers.ActionBtn(modeRow.transform, "Random", () => { EarthquakeMode.SetFrequencyMode(1); RefreshEarthquake(); }, 52);
                UIHelpers.ActionBtn(modeRow.transform, "Constant", () => { EarthquakeMode.SetFrequencyMode(2); RefreshEarthquake(); }, 64);

                // Frequency slider — only relevant in Timed mode
                _eqFreqRow = UIHelpers.StatRow("Frequency", c);
                _eqFreqBar = UIHelpers.MakeBar("EqFB", _eqFreqRow.transform,
                    (EarthquakeMode.FrequencyLevel - 1) / 9f);
                _eqFreqVal = UIHelpers.Txt("EqFV", _eqFreqRow.transform,
                    EarthquakeMode.FrequencyDisplay, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _eqFreqVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
                UIHelpers.SmallBtn(_eqFreqRow.transform, "-",
                    () => { EarthquakeMode.DecreaseFrequency(); RefreshEarthquake(); });
                UIHelpers.SmallBtn(_eqFreqRow.transform, "+",
                    () => { EarthquakeMode.IncreaseFrequency(); RefreshEarthquake(); });

                UIHelpers.InfoBox(c,
                    "Timed = quakes on a fixed schedule. " +
                    "Random = surprise quakes, up to 30 seconds apart. " +
                    "Constant = never stops shaking.");

                // Duration
                var durRow = UIHelpers.StatRow("Quake Duration", c);
                _eqDurBar = UIHelpers.MakeBar("EqDB", durRow.transform,
                    (EarthquakeMode.DurationLevel - 1) / 9f);
                _eqDurVal = UIHelpers.Txt("EqDV", durRow.transform,
                    EarthquakeMode.DurationDisplay, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _eqDurVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
                UIHelpers.SmallBtn(durRow.transform, "-",
                    () => { EarthquakeMode.DecreaseDuration(); RefreshEarthquake(); });
                UIHelpers.SmallBtn(durRow.transform, "+",
                    () => { EarthquakeMode.IncreaseDuration(); RefreshEarthquake(); });

                UIHelpers.InfoBox(c, "Level 1 duration = 1 second. Level 10 = 10 seconds of pure chaos.");

                UIHelpers.AddScrollForwarders(c);
                RefreshEarthquake();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("ModesPage.BuildEarthquakePage: " + ex.Message);
            }
        }

        private static void RefreshEarthquake()
        {
            bool on = EarthquakeMode.Enabled;
            if (_eqTogVal) { _eqTogVal.text = on ? "ON" : "OFF"; _eqTogVal.color = on ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_eqTrack, _eqKnob, on);
            if (_eqIntVal) _eqIntVal.text = EarthquakeMode.IntensityDisplay;
            if (_eqFreqVal) _eqFreqVal.text = EarthquakeMode.FrequencyDisplay;
            if (_eqDurVal) _eqDurVal.text = EarthquakeMode.DurationDisplay;
            if (_eqModeLbl) _eqModeLbl.text = EarthquakeMode.FrequencyModeName;
            UIHelpers.SetBar(_eqIntBar, (EarthquakeMode.IntensityLevel - 1) / 9f);
            UIHelpers.SetBar(_eqFreqBar, (EarthquakeMode.FrequencyLevel - 1) / 9f);
            UIHelpers.SetBar(_eqDurBar, (EarthquakeMode.DurationLevel - 1) / 9f);
            // Only show frequency slider in Timed mode
            if ((object)_eqFreqRow != null)
                _eqFreqRow.SetActive(EarthquakeMode.FrequencyMode == 0);
        }

        // ── Police Chase page ─────────────────────────────────────────
        private static void BuildPoliceChasePage(Transform parent)
        {
            try
            {
                var pg = UIHelpers.Obj("PPC", parent);
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
                content.AddComponent<ContentSizeFitter>().verticalFit =
                    ContentSizeFitter.FitMode.PreferredSize;
                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

                var c = content.transform;

                UIHelpers.SectionHeader("POLICE CHASE MODE", c);

                UIHelpers.InfoBox(c,
                    "A pursuer spawns behind you and chases you down the mountain.");
                UIHelpers.InfoBox(c,
                    "If it gets within 5 metres you bail and the CAUGHT counter goes up. " +
                    "The pursuer resets behind you and the chase begins again.");
                UIHelpers.InfoBox(c,
                    "Watch for the flashing red/blue ball — that's the pursuer. " +
                    "Police lights on screen show the mode is active. " +
                    "The ball has real physics — it slows on corners and can crash into terrain.");

                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("DIFFICULTY", c);

                var diffRow = UIHelpers.StatRow("Mode", c);
                UIHelpers.ActionBtn(diffRow.transform, "Easy",
                    () => { PoliceChaseMode.SetDifficulty(0); RefreshPoliceChase(); }, 44);
                UIHelpers.ActionBtn(diffRow.transform, "Medium",
                    () => { PoliceChaseMode.SetDifficulty(1); RefreshPoliceChase(); }, 56);
                UIHelpers.ActionBtn(diffRow.transform, "Hard",
                    () => { PoliceChaseMode.SetDifficulty(2); RefreshPoliceChase(); }, 44);

                UIHelpers.InfoBox(c,
                    "Easy = 80% of your speed, no bursts. " +
                    "Medium = 95% speed with bursts. " +
                    "Hard = 110% speed, frequent bursts. It will catch you on straights.");

                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("CONTROLS", c);

                var enRow = UIHelpers.StatRow("Enable", c);
                _pcTogVal = UIHelpers.Txt("PcTV", enRow.transform, "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _pcTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(enRow.transform, "PcT",
                    () => { PoliceChaseMode.Toggle(); RefreshPoliceChase(); },
                    out _pcTrack, out _pcKnob);

                // Countdown / status row
                var statusRow = UIHelpers.StatRow("Status", c);
                _pcStatusTxt = UIHelpers.Txt("PcSt", statusRow.transform, "—", 11,
                    FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                _pcStatusTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                UIHelpers.Divider(c);
                UIHelpers.InfoBox(c,
                    "⚠ Photosensitivity Warning: This mode displays rapidly flashing " +
                    "red and blue lights. Do not use if you are sensitive to flashing lights.");

                UIHelpers.AddScrollForwarders(c);
                RefreshPoliceChase();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("ModesPage.BuildPoliceChasePage: " + ex.Message);
            }
        }

        private static void RefreshPoliceChase()
        {
            bool on = PoliceChaseMode.Enabled;
            if (_pcTogVal) { _pcTogVal.text = on ? "ON" : "OFF"; _pcTogVal.color = on ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_pcTrack, _pcKnob, on);
        }

        // ── Trick Attack page ─────────────────────────────────────────
        private static void BuildTrickAttackPage(Transform parent)
        {
            try
            {
                var pg = UIHelpers.Obj("PTA", parent);
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
                content.AddComponent<ContentSizeFitter>().verticalFit =
                    ContentSizeFitter.FitMode.PreferredSize;
                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad,
                    (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

                var c = content.transform;

                UIHelpers.SectionHeader("TRICK ATTACK MODE", c);
                UIHelpers.InfoBox(c,
                    "Set a score target, ride to your chosen spot, then click " +
                    "LEFT STICK to start the timer. Score as many trick points " +
                    "as you can before time runs out.");
                UIHelpers.InfoBox(c,
                    "Hit your target before time hits zero = SUCCESS. Miss it = FAIL. " +
                    "Timer and score are shown on screen while the run is active.");

                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("SETUP", c);

                // ── Score target input — exact MapPage seed input pattern ──
                var inputRow = UIHelpers.Obj("TaInputRow", c);
                inputRow.AddComponent<Image>().color = UIHelpers.RowBg;
                var irLE = inputRow.AddComponent<LayoutElement>();
                irLE.preferredHeight = 36; irLE.minHeight = 36;
                var irHlg = inputRow.AddComponent<HorizontalLayoutGroup>();
                irHlg.padding = new RectOffset(8, 8, 4, 4);
                irHlg.spacing = 6;
                irHlg.childAlignment = TextAnchor.MiddleLeft;
                irHlg.childForceExpandHeight = true;
                irHlg.childForceExpandWidth = false;

                var inputBg = UIHelpers.Obj("TaIBg", inputRow.transform);
                inputBg.AddComponent<Image>().color = UIHelpers.WinOuter;
                var ibgLE = inputBg.AddComponent<LayoutElement>();
                ibgLE.flexibleWidth = 1; ibgLE.minHeight = 26; ibgLE.preferredHeight = 26;
                var ibgHlg = inputBg.AddComponent<HorizontalLayoutGroup>();
                ibgHlg.padding = new RectOffset(8, 8, 0, 0);
                ibgHlg.childAlignment = TextAnchor.MiddleLeft;
                ibgHlg.childForceExpandWidth = true;
                ibgHlg.childForceExpandHeight = true;

                _taTargetInput = UIHelpers.Txt("TaIT", inputBg.transform,
                    TrickAttackMode.TargetScore.ToString(), 11,
                    FontStyle.Normal, TextAnchor.MiddleLeft, UIHelpers.TextLight);
                _taTargetInput.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                // Button on the background panel so clicking it focuses the input
                _taInputRect = UIHelpers.RT(inputBg);
                var focusBtn = inputBg.AddComponent<Button>();
                focusBtn.targetGraphic = inputBg.GetComponent<Image>();
                focusBtn.onClick.AddListener(() => { _taFocused = true; });

                UIHelpers.ActionBtn(inputRow.transform, "Set", () =>
                {
                    int v;
                    if (int.TryParse(_taBuffer.Trim(), out v) && v > 0)
                    {
                        TrickAttackMode.SetTarget(v);
                        _taBuffer = "";
                        _taFocused = false;
                        if (_taTargetInput) { _taTargetInput.text = v.ToString(); _taTargetInput.color = UIHelpers.TextLight; }
                    }
                }, 34);

                UIHelpers.InfoBox(c, "Click the box, type your target score, press Set or Enter.");

                // Time limit selector — same style as BikeSwitcher ◀ label ▶
                var timeRow = UIHelpers.StatRow("Time Limit", c);
                UIHelpers.SmallBtn(timeRow.transform, "\u25C0",
                    () => { TrickAttackMode.PrevTimeLimit(); RefreshTrickAttack(); });
                _taTimeLbl = UIHelpers.Txt("TaTimeV", timeRow.transform,
                    TrickAttackMode.TimeLimitDisplay, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _taTimeLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;
                UIHelpers.SmallBtn(timeRow.transform, "\u25B6",
                    () => { TrickAttackMode.NextTimeLimit(); RefreshTrickAttack(); });

                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("CONTROLS", c);

                var enRow = UIHelpers.StatRow("Enable", c);
                _taTogVal = UIHelpers.Txt("TaTV", enRow.transform, "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _taTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(enRow.transform, "TaT",
                    () => { TrickAttackMode.Toggle(); RefreshTrickAttack(); },
                    out _taTrack, out _taKnob);

                UIHelpers.AddScrollForwarders(c);
                RefreshTrickAttack();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("ModesPage.BuildTrickAttackPage: " + ex.Message);
            }
        }

        private static bool _taFocused = false;
        public static bool IsTAInputFocused => _taFocused;
        private static RectTransform _taInputRect = null;

        // Boulder Dodge UI refs
        private static Text _bdTogVal, _bdIntervalLbl, _bdSizeLbl, _bdForwardLbl, _bdCountLbl;
        private static Image _bdTrack;
        private static RectTransform _bdKnob;

        // Survival UI refs
        private static Text _svTogVal, _svBailLbl, _svBleedLbl, _svHealLbl, _svHPLbl;
        private static Image _svTrack;
        private static RectTransform _svKnob;

        public static void TickTrickAttackInput()
        {
            if (_taTargetInput == null) return;

            // Click away to unfocus
            if (_taFocused && Input.GetMouseButtonDown(0))
            {
                Vector2 mp = Input.mousePosition;
                if ((object)_taInputRect != null &&
                    !RectTransformUtility.RectangleContainsScreenPoint(_taInputRect, mp, null))
                    _taFocused = false;
            }

            if (!_taFocused) return;

            foreach (char ch in Input.inputString)
            {
                if (ch == '\b')
                {
                    if (_taBuffer.Length > 0)
                        _taBuffer = _taBuffer.Substring(0, _taBuffer.Length - 1);
                }
                else if (ch == '\n' || ch == '\r')
                {
                    int v;
                    if (int.TryParse(_taBuffer.Trim(), out v) && v > 0)
                    {
                        TrickAttackMode.SetTarget(v);
                        if (_taTargetInput) _taTargetInput.text = v.ToString();
                    }
                    _taBuffer = "";
                    _taFocused = false;
                    return;
                }
                else if (ch >= '0' && ch <= '9' && _taBuffer.Length < 9)
                    _taBuffer += ch;
            }

            if (_taTargetInput)
            {
                _taTargetInput.text = _taBuffer.Length > 0
                    ? _taBuffer : TrickAttackMode.TargetScore.ToString();
                _taTargetInput.color = _taBuffer.Length > 0
                    ? UIHelpers.Accent : UIHelpers.TextDim;
            }
        }

        private static void RefreshTrickAttack()
        {
            bool on = TrickAttackMode.CurrentState != TrickAttackMode.State.Off;
            if (_taTogVal) { _taTogVal.text = on ? "ON" : "OFF"; _taTogVal.color = on ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_taTrack, _taKnob, on);
            if (_taTimeLbl) _taTimeLbl.text = TrickAttackMode.TimeLimitDisplay;
            if (_taTargetInput && _taBuffer.Length == 0)
                _taTargetInput.text = TrickAttackMode.TargetScore.ToString();
        }

        // ── Boulder Dodge page ────────────────────────────────────────
        private static void BuildBoulderDodgePage(Transform parent)
        {
            try
            {
                var pg = UIHelpers.Obj("PBD", parent);
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

                UIHelpers.SectionHeader("BOULDER DODGE MODE", c);
                UIHelpers.InfoBox(c, "Boulders drop from the sky directly into your path while you ride.");
                UIHelpers.InfoBox(c, "The mode tracks your speed and direction to predict where to drop each rock. Boulders lock into the ground on landing and won't move.");
                UIHelpers.InfoBox(c, "Rocks disappear once you are 200 metres away from them.");
                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("SETTINGS", c);

                var intRow = UIHelpers.StatRow("Interval", c);
                UIHelpers.SmallBtn(intRow.transform, "\u25C0", () => { BoulderDodgeMode.PrevInterval(); RefreshBoulderDodge(); });
                _bdIntervalLbl = UIHelpers.Txt("BdIntV", intRow.transform, BoulderDodgeMode.IntervalDisplay, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _bdIntervalLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
                UIHelpers.SmallBtn(intRow.transform, "\u25B6", () => { BoulderDodgeMode.NextInterval(); RefreshBoulderDodge(); });
                UIHelpers.InfoBox(c, "How long between each boulder drop.");

                var sizeRow = UIHelpers.StatRow("Size", c);
                UIHelpers.SmallBtn(sizeRow.transform, "\u25C0", () => { BoulderDodgeMode.PrevSize(); RefreshBoulderDodge(); });
                _bdSizeLbl = UIHelpers.Txt("BdSzV", sizeRow.transform, BoulderDodgeMode.SizeDisplay, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _bdSizeLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;
                UIHelpers.SmallBtn(sizeRow.transform, "\u25B6", () => { BoulderDodgeMode.NextSize(); RefreshBoulderDodge(); });

                var fwdRow = UIHelpers.StatRow("Drop Distance", c);
                UIHelpers.SmallBtn(fwdRow.transform, "\u25C0", () => { BoulderDodgeMode.PrevForward(); RefreshBoulderDodge(); });
                _bdForwardLbl = UIHelpers.Txt("BdFwdV", fwdRow.transform, BoulderDodgeMode.ForwardDisplay, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _bdForwardLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
                UIHelpers.SmallBtn(fwdRow.transform, "\u25B6", () => { BoulderDodgeMode.NextForward(); RefreshBoulderDodge(); });
                UIHelpers.InfoBox(c, "How far ahead of you the boulder targets.");

                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("CONTROLS", c);
                var enRow = UIHelpers.StatRow("Enable", c);
                _bdTogVal = UIHelpers.Txt("BdTV", enRow.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _bdTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(enRow.transform, "BdT", () => { BoulderDodgeMode.Toggle(); RefreshBoulderDodge(); }, out _bdTrack, out _bdKnob);

                var countRow = UIHelpers.StatRow("Active Boulders", c);
                _bdCountLbl = UIHelpers.Txt("BdCnt", countRow.transform, "0", 11, FontStyle.Normal, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                _bdCountLbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                UIHelpers.AddScrollForwarders(c);
                RefreshBoulderDodge();
            }
            catch (System.Exception ex) { MelonLogger.Error("ModesPage.BuildBoulderDodgePage: " + ex.Message); }
        }

        private static void RefreshBoulderDodge()
        {
            bool on = BoulderDodgeMode.Enabled;
            if (_bdTogVal) { _bdTogVal.text = on ? "ON" : "OFF"; _bdTogVal.color = on ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_bdTrack, _bdKnob, on);
            if (_bdIntervalLbl) _bdIntervalLbl.text = BoulderDodgeMode.IntervalDisplay;
            if (_bdSizeLbl) _bdSizeLbl.text = BoulderDodgeMode.SizeDisplay;
            if (_bdForwardLbl) _bdForwardLbl.text = BoulderDodgeMode.ForwardDisplay;
        }

        // ── Survival page ─────────────────────────────────────────────
        private static void BuildSurvivalPage(Transform parent)
        {
            try
            {
                var pg = UIHelpers.Obj("PSV", parent);
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

                UIHelpers.SectionHeader("SURVIVAL MODE", c);
                UIHelpers.InfoBox(c, "You have 100 HP. Every bail costs health. Standing still drains it slowly. Land tricks to earn it back.");
                UIHelpers.InfoBox(c, "Hit zero and it's GAME OVER. Press D-Pad Up to reset and go again.");

                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("SETTINGS", c);

                // Bail penalty
                var bailRow = UIHelpers.StatRow("Bail Penalty", c);
                UIHelpers.SmallBtn(bailRow.transform, "\u25C0", () => { SurvivalMode.PrevBailPenalty(); RefreshSurvival(); });
                _svBailLbl = UIHelpers.Txt("SvBailV", bailRow.transform, SurvivalMode.BailPenaltyDisplay, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _svBailLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 52;
                UIHelpers.SmallBtn(bailRow.transform, "\u25B6", () => { SurvivalMode.NextBailPenalty(); RefreshSurvival(); });

                // Bleed rate
                var bleedRow = UIHelpers.StatRow("Speed Bleed", c);
                UIHelpers.SmallBtn(bleedRow.transform, "\u25C0", () => { SurvivalMode.PrevBleed(); RefreshSurvival(); });
                _svBleedLbl = UIHelpers.Txt("SvBleedV", bleedRow.transform, SurvivalMode.BleedDisplay, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _svBleedLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 52;
                UIHelpers.SmallBtn(bleedRow.transform, "\u25B6", () => { SurvivalMode.NextBleed(); RefreshSurvival(); });
                UIHelpers.InfoBox(c, "HP lost per second when you're nearly stationary. Set to None to disable.");

                // Trick heal
                var healRow = UIHelpers.StatRow("Trick Heal", c);
                UIHelpers.SmallBtn(healRow.transform, "\u25C0", () => { SurvivalMode.PrevHeal(); RefreshSurvival(); });
                _svHealLbl = UIHelpers.Txt("SvHealV", healRow.transform, SurvivalMode.HealDisplay, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _svHealLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 52;
                UIHelpers.SmallBtn(healRow.transform, "\u25B6", () => { SurvivalMode.NextHeal(); RefreshSurvival(); });
                UIHelpers.InfoBox(c, "HP gained each time you land a trick combo.");

                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("CONTROLS", c);

                var enRow = UIHelpers.StatRow("Enable", c);
                _svTogVal = UIHelpers.Txt("SvTV", enRow.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _svTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(enRow.transform, "SvT", () => { SurvivalMode.Toggle(); RefreshSurvival(); }, out _svTrack, out _svKnob);

                // Live HP display
                var hpRow = UIHelpers.StatRow("Health", c);
                _svHPLbl = UIHelpers.Txt("SvHP", hpRow.transform, "—", 12, FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                _svHPLbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                UIHelpers.InfoBox(c, "D-Pad Up resets the run after a Game Over.");

                UIHelpers.AddScrollForwarders(c);
                RefreshSurvival();
            }
            catch (System.Exception ex) { MelonLogger.Error("ModesPage.BuildSurvivalPage: " + ex.Message); }
        }

        private static void RefreshSurvival()
        {
            bool on = SurvivalMode.Enabled;
            if (_svTogVal) { _svTogVal.text = on ? "ON" : "OFF"; _svTogVal.color = on ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_svTrack, _svKnob, on);
            if (_svBailLbl) _svBailLbl.text = SurvivalMode.BailPenaltyDisplay;
            if (_svBleedLbl) _svBleedLbl.text = SurvivalMode.BleedDisplay;
            if (_svHealLbl) _svHealLbl.text = SurvivalMode.HealDisplay;
        }

        // ── Sub-tab infrastructure ────────────────────────────────────
        private static void AddSubTab(Transform tabBar, Transform contentArea,
            string label, System.Action<Transform> buildContent)
        {
            int index = _tabPages.Count;

            var tab = UIHelpers.Obj("ModeTab_" + label, tabBar);
            var tabImg = tab.AddComponent<Image>();
            tabImg.color = new Color(0, 0, 0, 0);
            _tabBgs.Add(tabImg);

            var tabLE = tab.AddComponent<LayoutElement>();
            tabLE.preferredHeight = 30; tabLE.minHeight = 30;
            tabLE.flexibleHeight = 0; tabLE.flexibleWidth = 0;

            var tabHlg = tab.AddComponent<HorizontalLayoutGroup>();
            tabHlg.padding = new RectOffset(12, 12, 0, 0);
            tabHlg.childAlignment = TextAnchor.MiddleCenter;
            tabHlg.childForceExpandWidth = false;
            tabHlg.childForceExpandHeight = true;

            var tabTxt = UIHelpers.Txt("T_" + label, tab.transform, label, 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextDim);
            _tabTxts.Add(tabTxt);

            var btn = tab.AddComponent<Button>();
            btn.targetGraphic = tabImg;
            var bc = btn.colors;
            bc.normalColor = Color.white;
            bc.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            bc.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            bc.colorMultiplier = 1f;
            btn.colors = bc;
            int captured = index;
            btn.onClick.AddListener(() => SwitchTab(captured));

            var page = UIHelpers.Obj("ModePage_" + label, contentArea);
            UIHelpers.Fill(UIHelpers.RT(page));
            _tabPages.Add(page);

            buildContent(page.transform);
        }

        private static void SwitchTab(int idx)
        {
            _activeTab = idx;
            for (int i = 0; i < _tabPages.Count; i++)
            {
                bool on = (i == idx);
                if ((object)_tabPages[i] != null) _tabPages[i].SetActive(on);
                if ((object)_tabBgs[i] != null) _tabBgs[i].color = on ? UIHelpers.RowBg : new Color(0, 0, 0, 0);
                if ((object)_tabTxts[i] != null) _tabTxts[i].color = on ? UIHelpers.Accent : UIHelpers.TextDim;
            }
        }

        public static void Tick()
        {
            if (_activeTab == 0) AvalanchePage.Tick();
            if (_activeTab == 2) TickPoliceChasePage();
            if (_activeTab == 3) TickTrickAttackInput();
            if (_activeTab == 4) TickBoulderDodgePage();
            if (_activeTab == 5) TickSurvivalPage();
        }

        private static void TickBoulderDodgePage()
        {
            if (_bdCountLbl == null) return;
            int count = BoulderDodgeMode.ActiveCount;
            _bdCountLbl.text = count.ToString();
            _bdCountLbl.color = count > 0 ? UIHelpers.Accent : UIHelpers.TextDim;
        }

        private static void TickSurvivalPage()
        {
            if (_svHPLbl == null) return;
            if (SurvivalMode.IsGameOver)
            {
                _svHPLbl.text = "GAME OVER";
                _svHPLbl.color = UIHelpers.OffColor;
            }
            else if (SurvivalMode.Enabled)
            {
                _svHPLbl.text = Mathf.CeilToInt(SurvivalMode.HP) + " HP";
                float pct = SurvivalMode.HP / SurvivalMode.MaxHP;
                _svHPLbl.color = pct > 0.6f ? UIHelpers.OnColor
                               : pct > 0.3f ? new Color(1f, 0.7f, 0f)
                               : UIHelpers.OffColor;
            }
            else
            {
                _svHPLbl.text = "—";
                _svHPLbl.color = UIHelpers.TextDim;
            }
        }

        private static void TickPoliceChasePage()
        {
            if (_pcStatusTxt == null) return;
            if (PoliceChaseMode.IsCountingDown)
            {
                int secs = Mathf.CeilToInt(PoliceChaseMode.CountdownRemaining);
                _pcStatusTxt.text = "Starting in " + secs + "...";
                _pcStatusTxt.color = UIHelpers.OnColor;
            }
            else if (PoliceChaseMode.Enabled)
            {
                _pcStatusTxt.text = "Active — " + PoliceChaseMode.CaughtCount + " caught";
                _pcStatusTxt.color = UIHelpers.Accent;
            }
            else
            {
                _pcStatusTxt.text = "—";
                _pcStatusTxt.color = UIHelpers.TextDim;
            }
        }

        public static void RefreshAll()
        {
            if (_activeTab == 0) AvalanchePage.RefreshAll();
            RefreshEarthquake();
            RefreshPoliceChase();
        }
    }
}