using DescendersModMenu.Mods;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using DescendersModMenu;

namespace DescendersModMenu.UI
{
    public static class InfoPage
    {
        // ── Sub-tab state ─────────────────────────────────────────────
        private static int _activeTab = 0;

        private static readonly string[] TabLabels = { "System", "Hotkeys", "Version Update Check" };

        private static Image[] _tabBgs = new Image[3];
        private static Text[] _tabTxts = new Text[3];

        private static GameObject _pgSystem;
        private static GameObject _pgHotkeys;
        private static GameObject _pgCustomise;
        private static GameObject _pgCareer;
        private static GameObject _pgVersion;
        private static Text _verInstalledTxt;
        private static Text _verLatestTxt;
        private static Text _verStatusTxt;

        private static Text _custPosLbl;
        private static Text _custScaleLbl;
        private static Text _custOpacityLbl;
        private static GameObject _custSavedRow;

        public static int PendingSubTab = -1;
        private static Text _unityVersionTxt;
        private static Text _steamPlayerTxt;
        private static Text _unityMatchTxt;
        private static Text _mlVersionTxt;
        private static Text _careerResultTxt;
        private static Text _telemetryStatusTxt;

        private static int _feedbackCategory = 2;
        private static readonly string[] _feedbackCatNames = { "Bug Report", "Feature Request", "Feedback" };
        private static Image[] _feedbackCatBgs = new Image[3];
        private static InputField _feedbackInput;
        private static Text _feedbackStatusTxt;
        private static Text _sponsorVal;
        private static Text _repVal;
        private static UnityEngine.UI.Button _repMinus, _repPlus;
        private static Text _repMultVal;
        private static Text _inGameRepVal;
        private static UnityEngine.UI.Button _inGameRepMinus, _inGameRepPlus;
        private static Text _inGameRepMultVal;
        private static GameObject _devDiagContent;

        // ── Set-exact-rep inputs (text field + Set button) ──────────────
        private static Text _repSetInputText, _repSetCursor;
        private static RectTransform _repSetBoxRect;
        private static bool _repSetFocused;
        private static string _repSetBuffer = "";

        private static Text _inGameRepSetInputText, _inGameRepSetCursor;
        private static RectTransform _inGameRepSetBoxRect;
        private static bool _inGameRepSetFocused;
        private static string _inGameRepSetBuffer = "";

        // ── CreatePage ────────────────────────────────────────────────
        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("P3R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));
                var rootVlg = pg.AddComponent<VerticalLayoutGroup>();
                rootVlg.spacing = 0;
                rootVlg.padding = new RectOffset(0, 0, 0, 0);
                rootVlg.childAlignment = TextAnchor.UpperLeft;
                rootVlg.childForceExpandWidth = true;
                rootVlg.childForceExpandHeight = false;

                // ── Sub-tab bar ───────────────────────────────────────
                var tabBar = UIHelpers.Obj("TabBar", pg.transform);
                tabBar.AddComponent<Image>().color = UIHelpers.WinOuter;
                var tbLE = tabBar.AddComponent<LayoutElement>();
                tbLE.preferredHeight = 38; tbLE.minHeight = 38; tbLE.flexibleHeight = 0;
                var tbHlg = tabBar.AddComponent<HorizontalLayoutGroup>();
                tbHlg.spacing = 1;
                tbHlg.padding = new RectOffset(8, 8, 0, 0);
                tbHlg.childAlignment = TextAnchor.LowerLeft;
                tbHlg.childForceExpandWidth = false;
                tbHlg.childForceExpandHeight = false;

                for (int i = 0; i < TabLabels.Length; i++)
                {
                    int idx = i;
                    var tab = UIHelpers.Obj("Tab" + i, tabBar.transform);
                    var tabImg = tab.AddComponent<Image>();
                    tabImg.color = new Color(0, 0, 0, 0);
                    _tabBgs[i] = tabImg;
                    var tabLE = tab.AddComponent<LayoutElement>();
                    tabLE.preferredHeight = 30; tabLE.minHeight = 30;
                    tabLE.flexibleHeight = 0; tabLE.flexibleWidth = 0;

                    var tabHlg = tab.AddComponent<HorizontalLayoutGroup>();
                    tabHlg.padding = new RectOffset(12, 12, 0, 0);
                    tabHlg.childAlignment = TextAnchor.MiddleCenter;
                    tabHlg.childForceExpandWidth = false;
                    tabHlg.childForceExpandHeight = true;

                    var tabTxt = UIHelpers.Txt("T" + i, tab.transform, TabLabels[i], 11,
                        FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextDim);
                    _tabTxts[i] = tabTxt;

                    var btn = tab.AddComponent<Button>();
                    btn.targetGraphic = tabImg;
                    var bc = btn.colors;
                    bc.normalColor = Color.white;
                    bc.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
                    bc.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                    bc.colorMultiplier = 1; btn.colors = bc;
                    btn.onClick.AddListener(() => SwitchTab(idx));
                }

                // ── Content area ──────────────────────────────────────
                var contentArea = UIHelpers.Obj("Content", pg.transform);
                var caLE = contentArea.AddComponent<LayoutElement>();
                caLE.flexibleHeight = 1; caLE.flexibleWidth = 1;
                UIHelpers.Fill(UIHelpers.RT(contentArea));

                // ── System page ───────────────────────────────────────
                _pgSystem = UIHelpers.Obj("PgSystem", contentArea.transform);
                UIHelpers.Fill(UIHelpers.RT(_pgSystem));
                BuildSystemPage(_pgSystem.transform);

                // ── Hotkeys page ──────────────────────────────────────
                _pgHotkeys = UIHelpers.Obj("PgHotkeys", contentArea.transform);
                UIHelpers.Fill(UIHelpers.RT(_pgHotkeys));
                BuildHotkeysPage(_pgHotkeys.transform);

                _pgVersion = UIHelpers.Obj("PgVersion", contentArea.transform);
                UIHelpers.Fill(UIHelpers.RT(_pgVersion));
                BuildVersionPage(_pgVersion.transform);

                SwitchTab(0);
                PendingSubTab = -1;
                Refresh();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("InfoPage.CreatePage: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "InfoPage");
                return null;
            }
            return pg;
        }

        public static GameObject CreateCustomisePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("P25R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));
                _pgCustomise = pg;
                BuildCustomisePage(pg.transform);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("InfoPage.CreateCustomisePage: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "InfoPage");
                return null;
            }
            return pg;
        }

        private static GameObject _careerGate;
        private static GameObject _careerContentHost;
        private static bool _careerUnlockedThisVisit;

        public static GameObject CreateCareerPage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("P26R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));
                _pgCareer = pg;
                _careerUnlockedThisVisit = false;

                _careerContentHost = UIHelpers.Obj("CareerContent", pg.transform);
                UIHelpers.Fill(UIHelpers.RT(_careerContentHost));
                BuildCareerPage(_careerContentHost.transform);
                _careerContentHost.SetActive(false);

                _careerGate = BuildCareerConfirmGate(pg.transform);
                _careerGate.SetActive(true);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("InfoPage.CreateCareerPage: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "InfoPage");
                return null;
            }
            return pg;
        }

        public static void RefreshCustomisePage() { RefreshCustomise(); }
        public static void RefreshCareerPage() { RefreshCareerResult(); }

        public static void OnCareerTabOpened()
        {
            if (_careerUnlockedThisVisit)
            {
                if (_careerGate) _careerGate.SetActive(false);
                if (_careerContentHost) _careerContentHost.SetActive(true);
                RefreshCareerResult();
                return;
            }
            if (_careerGate) _careerGate.SetActive(true);
            if (_careerContentHost) _careerContentHost.SetActive(false);
        }

        public static void OnCareerTabClosed()
        {
            _careerUnlockedThisVisit = false;
            if (_careerGate) _careerGate.SetActive(true);
            if (_careerContentHost) _careerContentHost.SetActive(false);
        }

        private static GameObject BuildCareerConfirmGate(Transform parent)
        {
            var gate = UIHelpers.Obj("CareerGate", parent);
            UIHelpers.Fill(UIHelpers.RT(gate));
            var bg = gate.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.08f, 0.97f);
            bg.raycastTarget = true;

            var card = UIHelpers.Panel("GateCard", gate.transform, UIHelpers.RowBg, UIHelpers.RowSp);
            var cardRt = UIHelpers.RT(card);
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(420, 160);

            var cardBdr = UIHelpers.Panel("GateBdr", card.transform, UIHelpers.AccentBdr, UIHelpers.RowSp);
            cardBdr.GetComponent<Image>().raycastTarget = false;
            UIHelpers.Fill(UIHelpers.RT(cardBdr));
            cardBdr.AddComponent<LayoutElement>().ignoreLayout = true;

            var title = UIHelpers.Txt("GateTitle", card.transform,
                "Are you sure you want to stop the grind?",
                14, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextLight);
            var titleRt = UIHelpers.RT(title.gameObject);
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.sizeDelta = new Vector2(-24, 48);
            titleRt.anchoredPosition = new Vector2(0, -18);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.verticalOverflow = VerticalWrapMode.Overflow;

            var hint = UIHelpers.Txt("GateHint", card.transform,
                "Career tools can wipe or max progression. Yes opens the menu. No takes you back to General.",
                11, FontStyle.Italic, TextAnchor.MiddleCenter, Color.white);
            var hintRt = UIHelpers.RT(hint.gameObject);
            hintRt.anchorMin = new Vector2(0, 1);
            hintRt.anchorMax = new Vector2(1, 1);
            hintRt.pivot = new Vector2(0.5f, 1);
            hintRt.sizeDelta = new Vector2(-28, 40);
            hintRt.anchoredPosition = new Vector2(0, -68);
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;

            var btnRow = UIHelpers.Obj("GateBtns", card.transform);
            var btnRt = UIHelpers.RT(btnRow);
            btnRt.anchorMin = new Vector2(0.5f, 0);
            btnRt.anchorMax = new Vector2(0.5f, 0);
            btnRt.pivot = new Vector2(0.5f, 0);
            btnRt.sizeDelta = new Vector2(280, 32);
            btnRt.anchoredPosition = new Vector2(0, 18);
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            UIHelpers.ActionBtn(btnRow.transform, "Yes", () =>
            {
                _careerUnlockedThisVisit = true;
                if (_careerGate) _careerGate.SetActive(false);
                if (_careerContentHost) _careerContentHost.SetActive(true);
                RefreshCareerResult();
            }, 100);

            UIHelpers.ActionBtnOrange(btnRow.transform, "No", () =>
            {
                _careerUnlockedThisVisit = false;
                if (_careerGate) _careerGate.SetActive(true);
                if (_careerContentHost) _careerContentHost.SetActive(false);
                MenuWindow.GoToPage(1);
            }, 100);

            return gate;
        }

        // ── Tab switching ─────────────────────────────────────────────
        private static void SwitchTab(int idx)
        {
            _activeTab = idx;
            if ((object)_pgSystem != null) _pgSystem.SetActive(idx == 0);
            if ((object)_pgHotkeys != null) _pgHotkeys.SetActive(idx == 1);
            if ((object)_pgVersion != null) _pgVersion.SetActive(idx == 2);
            if (idx == 2) Refresh();

            for (int i = 0; i < TabLabels.Length; i++)
            {
                bool active = i == idx;
                if ((object)_tabBgs[i] != null)
                    _tabBgs[i].color = active ? UIHelpers.RowBg : new Color(0, 0, 0, 0);
                if ((object)_tabTxts[i] != null)
                    _tabTxts[i].color = active ? UIHelpers.Accent : UIHelpers.TextDim;
            }
        }

        // ── System page ───────────────────────────────────────────────
        private static void BuildSystemPage(Transform p)
        {
            var scrollObj = UIHelpers.Obj("SysScroll", p);
            UIHelpers.Fill(UIHelpers.RT(scrollObj));
            var sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 25f; sr.inertia = false;

            var vp = UIHelpers.Obj("SysVP", scrollObj.transform);
            UIHelpers.Fill(UIHelpers.RT(vp));
            vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            vp.AddComponent<Mask>().showMaskGraphic = true;
            sr.viewport = UIHelpers.RT(vp);

            var vlg = UIHelpers.Obj("SysVlg", vp.transform);
            var crt = UIHelpers.RT(vlg);
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 0);
            sr.content = crt;
            vlg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var v = vlg.AddComponent<VerticalLayoutGroup>();
            v.spacing = UIHelpers.RowGap;
            v.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            UIHelpers.SectionHeader("ENGINE", vlg.transform);
            _unityVersionTxt = MakeInfoRow("Unity Version", vlg.transform);
            _unityMatchTxt = MakeInfoRow("Version Match", vlg.transform);
            _mlVersionTxt = MakeInfoRow("MelonLoader", vlg.transform);
            MakeInfoRow2("Scripting Backend", "Mono", vlg.transform);
            MakeInfoRow2("Build Target", ".NET 4.7.2", vlg.transform);

            UIHelpers.Divider(vlg.transform);
            UIHelpers.SectionHeader("SANDBOX", vlg.transform);
            MakeInfoRow2("Version", BuildInfo.Version, vlg.transform, UIHelpers.Accent);
            MakeInfoRow2("Output DLL", "DescendersSandbox.dll", vlg.transform);
            MakeInfoRow2("Created By", "NateHyden", vlg.transform);

            UIHelpers.Divider(vlg.transform);
            UIHelpers.SectionHeader("COMMUNITY", vlg.transform);
            _steamPlayerTxt = MakeInfoRow("Steam Players Online", vlg.transform);

            // ── Telemetry ────────────────────────────────────────────
            UIHelpers.Divider(vlg.transform);
            UIHelpers.SectionHeader("TELEMETRY", vlg.transform);
            _telemetryStatusTxt = MakeInfoRow("Status", vlg.transform);
            UIHelpers.InfoBox(vlg.transform, "Helps me find bugs faster. Sends a small report to Discord if something goes wrong.", Color.white);
            UIHelpers.InfoBox(vlg.transform, "Sends mod version, platform, MelonLoader version, loaded mods, and error details if something breaks.", Color.white);

            var telOffRow = UIHelpers.Obj("TelOffNotice", vlg.transform);
            var telOffLe = telOffRow.AddComponent<LayoutElement>();
            telOffLe.preferredHeight = 48; telOffLe.minHeight = 48; telOffLe.flexibleHeight = 0;
            var telOffTxt = UIHelpers.Txt("TelOffTxt", telOffRow.transform,
                "TURN IT ON AND OFF ANYTIME WITH THE TOGGLE AT THE TOP - THIS IS THE BEST WAY TO SUPPORT BUG REPORTING IF YOU RUN INTO THEM",
                13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            telOffTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            telOffTxt.verticalOverflow = VerticalWrapMode.Overflow;
            UIHelpers.Fill(UIHelpers.RT(telOffTxt.gameObject), 4, 4, 0, 0);

            UIHelpers.InfoBox(vlg.transform, "Tip: click the small X next to the header hint to hide it for good.", Color.white);

            UIHelpers.Divider(vlg.transform);
            UIHelpers.SectionHeader("FEEDBACK", vlg.transform);
            UIHelpers.InfoBox(vlg.transform, "Report a bug, request a feature, or send feedback to Discord.", Color.white);

            var catRow = UIHelpers.Obj("FeedbackCatRow", vlg.transform);
            var catLe = catRow.AddComponent<LayoutElement>();
            catLe.preferredHeight = 28; catLe.minHeight = 28; catLe.flexibleHeight = 0;
            var catHlg = catRow.AddComponent<HorizontalLayoutGroup>();
            catHlg.spacing = 6; catHlg.childForceExpandWidth = true; catHlg.childForceExpandHeight = true;

            for (int c = 0; c < 3; c++)
            {
                int idx = c;
                var catBtnGo = UIHelpers.Obj("Cat" + c, catRow.transform);
                var catImg = catBtnGo.AddComponent<Image>();
                catImg.sprite = UIHelpers.BtnSp; catImg.type = Image.Type.Sliced;
                _feedbackCatBgs[c] = catImg;
                var catBtn = catBtnGo.AddComponent<Button>();
                catBtn.targetGraphic = catImg;
                catBtn.onClick.AddListener(() => { _feedbackCategory = idx; RefreshFeedbackCategoryButtons(); });
                var catTxt = UIHelpers.Txt("T", catBtnGo.transform, _feedbackCatNames[c], 10,
                    FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                UIHelpers.Fill(UIHelpers.RT(catTxt.gameObject));
            }

            var inputGo = UIHelpers.Obj("FeedbackInputBox", vlg.transform);
            var inputImg = inputGo.AddComponent<Image>();
            inputImg.sprite = UIHelpers.RowSp; inputImg.type = Image.Type.Sliced; inputImg.color = UIHelpers.RowBg;
            var inputLe = inputGo.AddComponent<LayoutElement>();
            inputLe.preferredHeight = 70; inputLe.minHeight = 70; inputLe.flexibleHeight = 0;

            var inputBd = UIHelpers.Panel("Bd", inputGo.transform, UIHelpers.RowBorder, UIHelpers.RowSp);
            inputBd.GetComponent<Image>().raycastTarget = false;
            UIHelpers.Fill(UIHelpers.RT(inputBd));
            inputBd.AddComponent<LayoutElement>().ignoreLayout = true;

            _feedbackInput = inputGo.AddComponent<InputField>();
            _feedbackInput.lineType = InputField.LineType.MultiLineNewline;
            _feedbackInput.characterLimit = 500;

            var inputTextGo = UIHelpers.Obj("Text", inputGo.transform);
            var inputTextComp = inputTextGo.AddComponent<Text>();
            inputTextComp.font = UIHelpers.GetFont();
            inputTextComp.fontSize = 12;
            inputTextComp.color = Color.white;
            inputTextComp.supportRichText = false;
            inputTextComp.alignment = TextAnchor.UpperLeft;
            inputTextComp.horizontalOverflow = HorizontalWrapMode.Wrap;
            inputTextComp.verticalOverflow = VerticalWrapMode.Overflow;
            UIHelpers.Fill(UIHelpers.RT(inputTextGo), 20, 8, 6, 6);
            _feedbackInput.textComponent = inputTextComp;

            var placeholderGo = UIHelpers.Obj("Placeholder", inputGo.transform);
            var placeholderComp = placeholderGo.AddComponent<Text>();
            placeholderComp.font = UIHelpers.GetFont();
            placeholderComp.fontSize = 12;
            placeholderComp.fontStyle = FontStyle.Italic;
            placeholderComp.color = UIHelpers.TextDim;
            placeholderComp.text = "Type your message here...";
            placeholderComp.alignment = TextAnchor.UpperLeft;
            placeholderComp.horizontalOverflow = HorizontalWrapMode.Wrap;
            placeholderComp.verticalOverflow = VerticalWrapMode.Overflow;
            UIHelpers.Fill(UIHelpers.RT(placeholderGo), 20, 8, 6, 6);
            _feedbackInput.placeholder = placeholderComp;

            _feedbackInput.customCaretColor = true;
            _feedbackInput.caretColor = Color.white;
            _feedbackInput.caretWidth = 2;
            _feedbackInput.caretBlinkRate = 0.85f;
            _feedbackInput.selectionColor = new Color(UIHelpers.Accent.r, UIHelpers.Accent.g, UIHelpers.Accent.b, 0.4f);

            var caretDotGo = UIHelpers.Obj("CaretBlink", inputGo.transform);
            var caretDotImg = caretDotGo.AddComponent<Image>();
            caretDotImg.sprite = UIHelpers.DotSp;
            caretDotImg.color = UIHelpers.OnColor;
            caretDotImg.raycastTarget = false;
            caretDotImg.enabled = false;
            var caretDotRt = UIHelpers.RT(caretDotGo);
            caretDotRt.anchorMin = new Vector2(0, 1); caretDotRt.anchorMax = new Vector2(0, 1);
            caretDotRt.pivot = new Vector2(0, 1);
            caretDotRt.sizeDelta = new Vector2(8, 8);
            caretDotRt.anchoredPosition = new Vector2(6, -10);
            caretDotGo.AddComponent<LayoutElement>().ignoreLayout = true;

            var sendRow = UIHelpers.Obj("FeedbackSendRow", vlg.transform);
            var sendLe = sendRow.AddComponent<LayoutElement>();
            sendLe.preferredHeight = 30; sendLe.minHeight = 30; sendLe.flexibleHeight = 0;
            var sendHlg = sendRow.AddComponent<HorizontalLayoutGroup>();
            sendHlg.spacing = 8; sendHlg.childAlignment = TextAnchor.MiddleLeft;
            sendHlg.childForceExpandWidth = false; sendHlg.childForceExpandHeight = true;

            var sendBtn = UIHelpers.Btn("SendBtn", sendRow.transform, "SEND", new Vector2(80, 28), 11,
                OnFeedbackSendClicked, UIHelpers.Accent, Color.black);
            var sendBtnLe = sendBtn.gameObject.AddComponent<LayoutElement>();
            sendBtnLe.preferredWidth = 80; sendBtnLe.preferredHeight = 28; sendBtnLe.flexibleHeight = 0;

            _feedbackStatusTxt = UIHelpers.Txt("FeedbackStatus", sendRow.transform, "", 10,
                FontStyle.Italic, TextAnchor.MiddleLeft, UIHelpers.TextDim);
            var statusLe = _feedbackStatusTxt.gameObject.AddComponent<LayoutElement>();
            statusLe.flexibleWidth = 1; statusLe.preferredHeight = 28;

            var updater = sendRow.gameObject.AddComponent<FeedbackPanelUpdater>();
            updater.InputField = _feedbackInput;
            updater.CaretDot = caretDotImg;
            updater.StatusText = _feedbackStatusTxt;

            RefreshFeedbackCategoryButtons();

            UIHelpers.Divider(vlg.transform);
            UIHelpers.SectionHeaderButton("DEVELOPER DIAGNOSTICS", vlg.transform, HandleDevDiagTap);

            _devDiagContent = UIHelpers.Obj("DevDiagContent", vlg.transform);
            var dcLe = _devDiagContent.AddComponent<LayoutElement>();
            dcLe.flexibleHeight = 0;
            var dcVlg = _devDiagContent.AddComponent<VerticalLayoutGroup>();
            dcVlg.spacing = UIHelpers.RowGap;
            dcVlg.childAlignment = TextAnchor.UpperCenter;
            dcVlg.childForceExpandWidth = true; dcVlg.childForceExpandHeight = false;
            var dcFitter = _devDiagContent.AddComponent<ContentSizeFitter>();
            dcFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Transform ddc = _devDiagContent.transform;

            var dumpRow = UIHelpers.StatRow("Scene Dump", ddc);
            UIHelpers.ActionBtn(dumpRow.transform, "Dump Now", () =>
            {
                SceneDumper.DumpCurrentScene();
            }, 90);
            UIHelpers.InfoBox(ddc, "Saves debug dump files next to the game. Same as pressing # in-game.");

            var bikeUnlockDumpRow = UIHelpers.StatRow("Bike Unlock Status", ddc);
            UIHelpers.ActionBtn(bikeUnlockDumpRow.transform, "Dump Now", () =>
            {
                CareerReset.DumpBikeUnlockStatus();
                RefreshCareerResult();
            }, 90);
            UIHelpers.InfoBox(ddc, "Logs unlock status for every bike and gear item. Check MelonLoader/Latest.log after clicking.");

            _devDiagContent.SetActive(DevLock.IsUnlocked);

            UIHelpers.AddScrollbar(sr);
            UIHelpers.AddScrollForwarders(vlg.transform);
        }

        private static void BuildCareerPage(Transform p)
        {
            var scrollObj = UIHelpers.Obj("CareerScroll", p);
            UIHelpers.Fill(UIHelpers.RT(scrollObj));
            var sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 25f; sr.inertia = false;

            var vp = UIHelpers.Obj("CareerVP", scrollObj.transform);
            UIHelpers.Fill(UIHelpers.RT(vp));
            vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            vp.AddComponent<Mask>().showMaskGraphic = true;
            sr.viewport = UIHelpers.RT(vp);

            var vlg = UIHelpers.Obj("CareerVlg", vp.transform);
            var crt = UIHelpers.RT(vlg);
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 0);
            sr.content = crt;
            vlg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var v = vlg.AddComponent<VerticalLayoutGroup>();
            v.spacing = UIHelpers.RowGap;
            v.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            UIHelpers.SectionHeader("CAREER PROGRESSION", vlg.transform);
            UIHelpers.InfoBox(vlg.transform, "This can't be undone.");

            var completeRow = UIHelpers.StatRow("Complete Missions", vlg.transform);
            UIHelpers.ActionBtnOrange(completeRow.transform, "Complete All", () =>
            {
                CareerReset.CompleteAllMissions();
                RefreshCareerResult();
            }, 100);

            var levelRow = UIHelpers.StatRow("Level Reset", vlg.transform);
            UIHelpers.ActionBtnOrange(levelRow.transform, "Wipe Progress", () =>
            {
                CareerReset.ResetLevelProgress();
                RefreshCareerResult();
            }, 100);

            var sponsorRow = UIHelpers.StatRow("Sponsor Reset", vlg.transform);
            UIHelpers.ActionBtnOrange(sponsorRow.transform, "Reset Sponsor", () =>
            {
                CareerReset.ResetSponsorProgress();
                RefreshCareerResult();
            }, 100);

            var maxTierRow = UIHelpers.StatRow("Max Sponsor Level", vlg.transform);
            UIHelpers.ActionBtnOrange(maxTierRow.transform, "Max Level", () =>
            {
                CareerReset.MaxSponsorLevel();
                RefreshCareerResult();
            }, 100);

            // ── Switch Sponsor ────────────────────────────────────────
            var switchSponsorRow = UIHelpers.StatRow("Current Sponsor", vlg.transform);
            UIHelpers.SmallBtn(switchSponsorRow.transform, "\u25C0", () =>
            {
                CareerReset.PreviousSponsor();
                RefreshCareerResult();
            });
            _sponsorVal = UIHelpers.Txt("SponsorV", switchSponsorRow.transform, CareerReset.CurrentSponsorName, 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            _sponsorVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 130;
            UIHelpers.SmallBtn(switchSponsorRow.transform, "\u25B6", () =>
            {
                CareerReset.NextSponsor();
                RefreshCareerResult();
            });
            UIHelpers.InfoBox(vlg.transform, "Cycles through every sponsor team.");

            var unlockAllRow = UIHelpers.StatRow("Unlock All (Bikes + Gear)", vlg.transform);
            UIHelpers.ActionBtnOrange(unlockAllRow.transform, "Unlock All", () =>
            {
                CareerReset.UnlockAllOn();
                RefreshCareerResult();
            }, 90);
            UIHelpers.ActionBtnOrange(unlockAllRow.transform, "Lock All", () =>
            {
                CareerReset.UnlockAllOff();
                RefreshCareerResult();
            }, 90);
            UIHelpers.InfoBox(vlg.transform, "Unlocks or locks every bike and gear item straight away.");

            // ── Adjust Rep (+/-) ──────────────────────────────────────
            var repRow = UIHelpers.StatRow("Adjust Total Rep", vlg.transform);
            _repMinus = UIHelpers.SmallBtn(repRow.transform, "\u25C0", () =>
            {
                CareerReset.AdjustRep(-CareerReset.RepStepAmount);
                RefreshCareerResult();
            });
            _repVal = UIHelpers.Txt("RepV", repRow.transform, CareerReset.LiveRepValue.ToString(), 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            _repVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 90;
            _repPlus = UIHelpers.SmallBtn(repRow.transform, "\u25B6", () =>
            {
                CareerReset.AdjustRep(CareerReset.RepStepAmount);
                RefreshCareerResult();
            });
            UIHelpers.SmallBtn(repRow.transform, "-", () =>
            {
                CareerReset.DecreaseRepMultiplier();
                RefreshCareerResult();
            });
            _repMultVal = UIHelpers.Txt("RepMultV", repRow.transform, "x" + CareerReset.RepMultiplierLevel, 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
            _repMultVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            UIHelpers.SmallBtn(repRow.transform, "+", () =>
            {
                CareerReset.IncreaseRepMultiplier();
                RefreshCareerResult();
            });
            UIHelpers.InfoBox(vlg.transform, "Adds or removes reputation. Use the multiplier for bigger jumps. Changes your total / lifetime rep.");

            // ── Set Total Rep to an exact typed value ───────────────────
            var repSetRow = UIHelpers.Obj("RepSetRow", vlg.transform);
            repSetRow.AddComponent<Image>().color = UIHelpers.RowBg;
            var rsrLe = repSetRow.AddComponent<LayoutElement>();
            rsrLe.preferredHeight = 36; rsrLe.minHeight = 36;
            var rsrHlg = repSetRow.AddComponent<HorizontalLayoutGroup>();
            rsrHlg.padding = new RectOffset(8, 8, 4, 4);
            rsrHlg.spacing = 6; rsrHlg.childAlignment = TextAnchor.MiddleLeft;
            rsrHlg.childForceExpandHeight = true; rsrHlg.childForceExpandWidth = false;

            var repSetBg = UIHelpers.Obj("RepSetBg", repSetRow.transform);
            repSetBg.AddComponent<Image>().color = UIHelpers.WinOuter;
            var rsbgLe = repSetBg.AddComponent<LayoutElement>();
            rsbgLe.flexibleWidth = 1; rsbgLe.minHeight = 26; rsbgLe.preferredHeight = 26;
            var rsbgHlg = repSetBg.AddComponent<HorizontalLayoutGroup>();
            rsbgHlg.padding = new RectOffset(8, 8, 0, 0);
            rsbgHlg.childAlignment = TextAnchor.MiddleLeft;
            rsbgHlg.childForceExpandWidth = true; rsbgHlg.childForceExpandHeight = true;

            _repSetInputText = UIHelpers.Txt("RepSetIT", repSetBg.transform, "Type exact rep value...",
                11, FontStyle.Normal, TextAnchor.MiddleLeft, UIHelpers.TextDim);
            _repSetInputText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _repSetInputText.verticalOverflow = VerticalWrapMode.Truncate;
            _repSetInputText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            _repSetCursor = UIHelpers.Txt("RepSetCur", repSetBg.transform, "|",
                12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            _repSetCursor.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var rscRT = UIHelpers.RT(_repSetCursor.gameObject);
            rscRT.anchorMin = new Vector2(1, 0); rscRT.anchorMax = new Vector2(1, 1);
            rscRT.pivot = new Vector2(1, 0.5f);
            rscRT.sizeDelta = new Vector2(10, 0);
            rscRT.anchoredPosition = new Vector2(-6, 0);
            _repSetCursor.gameObject.SetActive(false);

            _repSetBoxRect = UIHelpers.RT(repSetBg);
            var repSetFocusBtn = repSetBg.AddComponent<UnityEngine.UI.Button>();
            repSetFocusBtn.targetGraphic = repSetBg.GetComponent<Image>();
            repSetFocusBtn.onClick.AddListener(() => { _repSetFocused = true; });

            UIHelpers.ActionBtn(repSetRow.transform, "Set", () =>
            {
                TrySetRepFromBox();
            }, 52);
            UIHelpers.InfoBox(vlg.transform, "Type a number and hit Set (or Enter) to jump straight to that total rep value.");
            UIHelpers.InfoBox(vlg.transform, "Game limitation: a big jump upward may stop counting genuine rep earned afterwards. Prefer small changes close to your real total.", UIHelpers.Orange);

            var inGameRepRow = UIHelpers.StatRow("Adjust In-Game Rep", vlg.transform);
            _inGameRepMinus = UIHelpers.SmallBtn(inGameRepRow.transform, "\u25C0", () =>
            {
                CareerReset.AdjustInGameRep(-CareerReset.InGameRepStepAmount);
                RefreshCareerResult();
            });
            _inGameRepVal = UIHelpers.Txt("InGameRepV", inGameRepRow.transform, CareerReset.CurrentInGameRep.ToString(), 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            _inGameRepVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 90;
            _inGameRepPlus = UIHelpers.SmallBtn(inGameRepRow.transform, "\u25B6", () =>
            {
                CareerReset.AdjustInGameRep(CareerReset.InGameRepStepAmount);
                RefreshCareerResult();
            });
            UIHelpers.SmallBtn(inGameRepRow.transform, "-", () =>
            {
                CareerReset.DecreaseInGameRepMultiplier();
                RefreshCareerResult();
            });
            _inGameRepMultVal = UIHelpers.Txt("InGameRepMultV", inGameRepRow.transform, "x" + CareerReset.InGameRepMultiplierLevel, 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
            _inGameRepMultVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            UIHelpers.SmallBtn(inGameRepRow.transform, "+", () =>
            {
                CareerReset.IncreaseInGameRepMultiplier();
                RefreshCareerResult();
            });
            UIHelpers.InfoBox(vlg.transform, "Adds or removes this session's combo rep only. Resets each new session.");

            // ── Set In-Game Rep to an exact typed value ──────────────────
            var inGameRepSetRow = UIHelpers.Obj("InGameRepSetRow", vlg.transform);
            inGameRepSetRow.AddComponent<Image>().color = UIHelpers.RowBg;
            var igrsrLe = inGameRepSetRow.AddComponent<LayoutElement>();
            igrsrLe.preferredHeight = 36; igrsrLe.minHeight = 36;
            var igrsrHlg = inGameRepSetRow.AddComponent<HorizontalLayoutGroup>();
            igrsrHlg.padding = new RectOffset(8, 8, 4, 4);
            igrsrHlg.spacing = 6; igrsrHlg.childAlignment = TextAnchor.MiddleLeft;
            igrsrHlg.childForceExpandHeight = true; igrsrHlg.childForceExpandWidth = false;

            var inGameRepSetBg = UIHelpers.Obj("InGameRepSetBg", inGameRepSetRow.transform);
            inGameRepSetBg.AddComponent<Image>().color = UIHelpers.WinOuter;
            var igrsbgLe = inGameRepSetBg.AddComponent<LayoutElement>();
            igrsbgLe.flexibleWidth = 1; igrsbgLe.minHeight = 26; igrsbgLe.preferredHeight = 26;
            var igrsbgHlg = inGameRepSetBg.AddComponent<HorizontalLayoutGroup>();
            igrsbgHlg.padding = new RectOffset(8, 8, 0, 0);
            igrsbgHlg.childAlignment = TextAnchor.MiddleLeft;
            igrsbgHlg.childForceExpandWidth = true; igrsbgHlg.childForceExpandHeight = true;

            _inGameRepSetInputText = UIHelpers.Txt("IGRepSetIT", inGameRepSetBg.transform, "Type exact in-game rep value...",
                11, FontStyle.Normal, TextAnchor.MiddleLeft, UIHelpers.TextDim);
            _inGameRepSetInputText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _inGameRepSetInputText.verticalOverflow = VerticalWrapMode.Truncate;
            _inGameRepSetInputText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            _inGameRepSetCursor = UIHelpers.Txt("IGRepSetCur", inGameRepSetBg.transform, "|",
                12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            _inGameRepSetCursor.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var igrscRT = UIHelpers.RT(_inGameRepSetCursor.gameObject);
            igrscRT.anchorMin = new Vector2(1, 0); igrscRT.anchorMax = new Vector2(1, 1);
            igrscRT.pivot = new Vector2(1, 0.5f);
            igrscRT.sizeDelta = new Vector2(10, 0);
            igrscRT.anchoredPosition = new Vector2(-6, 0);
            _inGameRepSetCursor.gameObject.SetActive(false);

            _inGameRepSetBoxRect = UIHelpers.RT(inGameRepSetBg);
            var inGameRepSetFocusBtn = inGameRepSetBg.AddComponent<UnityEngine.UI.Button>();
            inGameRepSetFocusBtn.targetGraphic = inGameRepSetBg.GetComponent<Image>();
            inGameRepSetFocusBtn.onClick.AddListener(() => { _inGameRepSetFocused = true; });

            UIHelpers.ActionBtn(inGameRepSetRow.transform, "Set", () =>
            {
                TrySetInGameRepFromBox();
            }, 52);
            UIHelpers.InfoBox(vlg.transform, "Type a number and hit Set (or Enter) to jump straight to that in-game rep value.");
            UIHelpers.InfoBox(vlg.transform, "Game limitation: a big jump upward may stop counting genuine rep earned afterwards. Prefer small changes close to your real total.", UIHelpers.Orange);

            var resultRow = UIHelpers.StatRow("Last Result", vlg.transform);
            _careerResultTxt = UIHelpers.Txt("CRResult", resultRow.transform, CareerReset.LastResult,
                11, FontStyle.Normal, TextAnchor.MiddleRight, UIHelpers.Accent);
            _careerResultTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            FavouritesManager.RegisterStarButton("CompleteMissions", UIHelpers.StarBtn(completeRow.transform, "CompleteMissions", () => FavouritesManager.Toggle("CompleteMissions")));
            FavouritesManager.RegisterStarButton("LevelReset", UIHelpers.StarBtn(levelRow.transform, "LevelReset", () => FavouritesManager.Toggle("LevelReset")));
            FavouritesManager.RegisterStarButton("SponsorReset", UIHelpers.StarBtn(sponsorRow.transform, "SponsorReset", () => FavouritesManager.Toggle("SponsorReset")));
            FavouritesManager.RegisterStarButton("MaxSponsorLevel", UIHelpers.StarBtn(maxTierRow.transform, "MaxSponsorLevel", () => FavouritesManager.Toggle("MaxSponsorLevel")));
            FavouritesManager.RegisterStarButton("SwitchSponsor", UIHelpers.StarBtn(switchSponsorRow.transform, "SwitchSponsor", () => FavouritesManager.Toggle("SwitchSponsor")));
            FavouritesManager.RegisterStarButton("UnlockAll", UIHelpers.StarBtn(unlockAllRow.transform, "UnlockAll", () => FavouritesManager.Toggle("UnlockAll")));
            FavouritesManager.RegisterStarButton("AdjustRep", UIHelpers.StarBtn(repRow.transform, "AdjustRep", () => FavouritesManager.Toggle("AdjustRep")));
            FavouritesManager.RegisterStarButton("AdjustInGameRep", UIHelpers.StarBtn(inGameRepRow.transform, "AdjustInGameRep", () => FavouritesManager.Toggle("AdjustInGameRep")));

            FavouritesManager.Register(new ModFavEntry
            {
                Id = "SwitchSponsor",
                DisplayName = "Current Sponsor",
                TabBadge = "INFO",
                BuildControls = (fp) =>
                {
                    var row = FavsPage.CompactStatRow("Current Sponsor", fp);
                    UIHelpers.SmallBtn(row.transform, "\u25C0", () =>
                    {
                        CareerReset.PreviousSponsor();
                        RefreshCareerResult();
                        FavsPage.RefreshFavourites();
                    });
                    var val = UIHelpers.Txt("FSt_SwitchSponsor", row.transform, CareerReset.CurrentSponsorName,
                        13, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                    val.gameObject.AddComponent<LayoutElement>().preferredWidth = 110;
                    UIHelpers.SmallBtn(row.transform, "\u25B6", () =>
                    {
                        CareerReset.NextSponsor();
                        RefreshCareerResult();
                        FavsPage.RefreshFavourites();
                    });
                },
                IsActive = () => false
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "UnlockAll",
                DisplayName = "Unlock All (Bikes + Gear)",
                TabBadge = "INFO",
                BuildControls = (fp) => FavsPage.BuildSimpleToggle(fp, "UnlockAll", "Unlock All",
                    () => CareerReset.UnlockAllEnabled, () => { CareerReset.ToggleUnlockAll(); RefreshCareerResult(); }, () => RefreshCareerResult()),
                IsActive = () => CareerReset.UnlockAllEnabled
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "AdjustRep",
                DisplayName = "Adjust Total Rep",
                TabBadge = "INFO",
                BuildControls = (fp) => FavsPage.BuildStepper(fp, "AdjustRep", "Adjust Total Rep",
                    () => CareerReset.LiveRepValue,
                    () => { CareerReset.AdjustRep(-CareerReset.RepStepAmount); RefreshCareerResult(); },
                    () => { CareerReset.AdjustRep(CareerReset.RepStepAmount); RefreshCareerResult(); },
                    int.MinValue, int.MaxValue, () => RefreshCareerResult(), 0),
                IsActive = () => false
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "AdjustInGameRep",
                DisplayName = "Adjust In-Game Rep",
                TabBadge = "INFO",
                BuildControls = (fp) => FavsPage.BuildStepper(fp, "AdjustInGameRep", "Adjust In-Game Rep",
                    () => CareerReset.CurrentInGameRep,
                    () => { CareerReset.AdjustInGameRep(-CareerReset.InGameRepStepAmount); RefreshCareerResult(); },
                    () => { CareerReset.AdjustInGameRep(CareerReset.InGameRepStepAmount); RefreshCareerResult(); },
                    int.MinValue, int.MaxValue, () => RefreshCareerResult(), 0),
                IsActive = () => false
            });

            FavouritesManager.Register(new ModFavEntry
            {
                Id = "CompleteMissions",
                DisplayName = "Complete All Missions",
                TabBadge = "INFO",
                BuildControls = (fp) => FavsPage.BuildActionButton(fp, "CompleteMissions", "Complete All Missions",
                    "Complete All", () => CareerReset.CompleteAllMissions(), null, () => CareerReset.LastResult),
                IsActive = () => false
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "LevelReset",
                DisplayName = "Level Reset",
                TabBadge = "INFO",
                BuildControls = (fp) => FavsPage.BuildActionButton(fp, "LevelReset", "Level Reset",
                    "Wipe Progress", () => CareerReset.ResetLevelProgress(), null, () => CareerReset.LastResult),
                IsActive = () => false
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "SponsorReset",
                DisplayName = "Sponsor Reset",
                TabBadge = "INFO",
                BuildControls = (fp) => FavsPage.BuildActionButton(fp, "SponsorReset", "Sponsor Reset",
                    "Reset Sponsor", () => CareerReset.ResetSponsorProgress(), null, () => CareerReset.LastResult),
                IsActive = () => false
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "MaxSponsorLevel",
                DisplayName = "Max Sponsor Level",
                TabBadge = "INFO",
                BuildControls = (fp) => FavsPage.BuildActionButton(fp, "MaxSponsorLevel", "Max Sponsor Level",
                    "Max Level", () => CareerReset.MaxSponsorLevel(), null, () => CareerReset.LastResult),
                IsActive = () => false
            });

            UIHelpers.AddScrollbar(sr);
            UIHelpers.AddScrollForwarders(vlg.transform);
            RefreshCareerResult();
        }

        // ── Feedback panel handlers ────────────────────────────────────
        private static void RefreshFeedbackCategoryButtons()
        {
            for (int i = 0; i < 3; i++)
            {
                if (!_feedbackCatBgs[i]) continue;
                _feedbackCatBgs[i].color = (i == _feedbackCategory) ? UIHelpers.Accent : UIHelpers.RowBg;
                var txt = _feedbackCatBgs[i].GetComponentInChildren<Text>();
                if (txt) txt.color = (i == _feedbackCategory) ? Color.black : UIHelpers.TextMid;
            }
        }

        private static void OnFeedbackSendClicked()
        {
            if (!_feedbackInput) return;
            string msg = _feedbackInput.text != null ? _feedbackInput.text.Trim() : "";
            if (string.IsNullOrEmpty(msg))
            {
                if (_feedbackStatusTxt) { _feedbackStatusTxt.text = "Type something first!"; _feedbackStatusTxt.color = UIHelpers.OffColor; }
                return;
            }
            if (!Telemetry.CanSendFeedback())
            {
                if (_feedbackStatusTxt) { _feedbackStatusTxt.text = "Please wait a moment before sending again."; _feedbackStatusTxt.color = UIHelpers.OffColor; }
                return;
            }
            Telemetry.SendFeedbackAsync(_feedbackCatNames[_feedbackCategory], msg);
            _feedbackInput.text = "";
            if (_feedbackStatusTxt) { _feedbackStatusTxt.text = "Sending..."; _feedbackStatusTxt.color = UIHelpers.TextDim; }
        }

        private static void HandleDevDiagTap()
        {
            DevLock.RegisterTap();
            if (_devDiagContent) _devDiagContent.SetActive(DevLock.IsUnlocked);
        }

        private static void RefreshCareerResult()
        {
            if ((object)_careerResultTxt != null && _careerResultTxt)
                _careerResultTxt.text = CareerReset.LastResult;

            if (_sponsorVal) _sponsorVal.text = CareerReset.CurrentSponsorName;

            if (_repVal) _repVal.text = CareerReset.LiveRepValue.ToString();
            if (_repMultVal) _repMultVal.text = "x" + CareerReset.RepMultiplierLevel;
            if (_inGameRepVal) _inGameRepVal.text = CareerReset.CurrentInGameRep.ToString();
            if (_inGameRepMultVal) _inGameRepMultVal.text = "x" + CareerReset.InGameRepMultiplierLevel;
        }

        // ── Hotkeys page ──────────────────────────────────────────────
        private static void BuildHotkeysPage(Transform p)
        {
            var vlg = UIHelpers.Obj("HkVlg", p);
            UIHelpers.Fill(UIHelpers.RT(vlg));
            var v = vlg.AddComponent<VerticalLayoutGroup>();
            v.spacing = UIHelpers.RowGap;
            v.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            UIHelpers.SectionHeader("MENU", vlg.transform);
            UIHelpers.HotkeyRow(vlg.transform, "Toggle mod menu", "F6");

            UIHelpers.SectionHeader("GAMEPLAY", vlg.transform);
            UIHelpers.HotkeyRow(vlg.transform, "Toggle slow motion", "F2");
            UIHelpers.HotkeyRow(vlg.transform, "Ghost Replay — toggle", "F3 / RS Dbl Click");
            UIHelpers.HotkeyRow(vlg.transform, "Ghost Replay — save run", "F4 / RS Click");
            UIHelpers.HotkeyRow(vlg.transform, "Ghost Replay — set spawn", "LS Click");

        }

        private static void BuildVersionPage(Transform p)
        {
            var vlg = UIHelpers.Obj("VerVlg", p);
            UIHelpers.Fill(UIHelpers.RT(vlg));
            var v = vlg.AddComponent<VerticalLayoutGroup>();
            v.spacing = UIHelpers.RowGap;
            v.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            UIHelpers.SectionHeader("VERSION UPDATE CHECK", vlg.transform);
            _verInstalledTxt = MakeInfoRow("Installed", vlg.transform);
            _verLatestTxt = MakeInfoRow("Latest available", vlg.transform);
            _verStatusTxt = MakeInfoRow("Status", vlg.transform);

            UIHelpers.Divider(vlg.transform);
            UIHelpers.SectionHeader("DOWNLOAD", vlg.transform);
            UIHelpers.InfoBox(vlg.transform, "Grab the latest build on GitHub or Nexus.");

            var linkRow = UIHelpers.StatRow("Releases", vlg.transform);
            UIHelpers.ActionBtn(linkRow.transform, "Open GitHub", () =>
            {
                try { Application.OpenURL(UpdateChecker.ReleasesPageUrl); }
                catch (System.Exception ex)
                {
                    MelonLogger.Error("[InfoPage] OpenURL: " + ex.Message);
                    Telemetry.ReportErrorAsync(ex, "InfoPage");
                }
            }, 110);

            var urlTxt = UIHelpers.Txt("RelUrl", linkRow.transform, UpdateChecker.ReleasesPageUrl,
                10, FontStyle.Normal, TextAnchor.MiddleRight, UIHelpers.NeonBlue);
            urlTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            RefreshVersion();
        }

        private static void RefreshVersion()
        {
            if (_verInstalledTxt)
                _verInstalledTxt.text = "v" + BuildInfo.Version;

            if (_verLatestTxt)
            {
                if (!UpdateChecker.CheckComplete)
                    _verLatestTxt.text = "Checking...";
                else if (string.IsNullOrEmpty(UpdateChecker.LatestVersion))
                    _verLatestTxt.text = "Could not check";
                else
                    _verLatestTxt.text = "v" + UpdateChecker.LatestVersion;
                _verLatestTxt.color = UIHelpers.Accent;
            }

            if (_verStatusTxt)
            {
                if (!UpdateChecker.CheckComplete)
                {
                    _verStatusTxt.text = "Checking...";
                    _verStatusTxt.color = UIHelpers.TextDim;
                }
                else if (string.IsNullOrEmpty(UpdateChecker.LatestVersion))
                {
                    _verStatusTxt.text = "Could not check";
                    _verStatusTxt.color = UIHelpers.OffColor;
                }
                else if (UpdateChecker.UpdateAvailable)
                {
                    _verStatusTxt.text = "OUTDATED";
                    _verStatusTxt.color = UIHelpers.OffColor;
                }
                else
                {
                    _verStatusTxt.text = "UP TO DATE";
                    _verStatusTxt.color = UIHelpers.OnColor;
                }
            }
        }

        // ── Credits page ──────────────────────────────────────────────

        // ── Customise page ────────────────────────────────────────────
        // ── Scanner page ──────────────────────────────────────────────

        private static void BuildCustomisePage(Transform p)
        {
            var vlg = UIHelpers.Obj("CustVlg", p);
            UIHelpers.Fill(UIHelpers.RT(vlg));
            var v = vlg.AddComponent<VerticalLayoutGroup>();
            v.spacing = UIHelpers.RowGap;
            v.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            var c = vlg.transform;

            // ── Position ──────────────────────────────────────────────
            UIHelpers.SectionHeader("POSITION", c);

            var posRow = UIHelpers.StatRow("Position", c);
            UIHelpers.ActionBtn(posRow.transform, "Centre",
                () => { Mods.MenuCustomiser.SetPosition(0); RefreshCustomise(); }, 52);
            UIHelpers.ActionBtn(posRow.transform, "Top Left",
                () => { Mods.MenuCustomiser.SetPosition(1); RefreshCustomise(); }, 58);
            UIHelpers.ActionBtn(posRow.transform, "Top Right",
                () => { Mods.MenuCustomiser.SetPosition(2); RefreshCustomise(); }, 60);
            _custPosLbl = UIHelpers.Txt("CustPosV", posRow.transform,
                Mods.MenuCustomiser.PositionLabels[Mods.MenuCustomiser.PositionPreset],
                11, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
            _custPosLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;

            UIHelpers.Divider(c);

            // ── Scale ─────────────────────────────────────────────────
            UIHelpers.SectionHeader("SCALE", c);

            var scaleRow = UIHelpers.StatRow("Scale", c);
            UIHelpers.SmallBtn(scaleRow.transform, "\u25C0",
                () => { Mods.MenuCustomiser.PrevScale(); RefreshCustomise(); });
            _custScaleLbl = UIHelpers.Txt("CustScaleV", scaleRow.transform,
                Mods.MenuCustomiser.ScaleDisplay,
                12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            _custScaleLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 52;
            UIHelpers.SmallBtn(scaleRow.transform, "\u25B6",
                () => { Mods.MenuCustomiser.NextScale(); RefreshCustomise(); });

            UIHelpers.Divider(c);

            // ── Opacity ───────────────────────────────────────────────
            UIHelpers.SectionHeader("OPACITY", c);

            var opacityRow = UIHelpers.StatRow("Opacity", c);
            UIHelpers.SmallBtn(opacityRow.transform, "\u25C0",
                () => { Mods.MenuCustomiser.PrevOpacity(); RefreshCustomise(); });
            _custOpacityLbl = UIHelpers.Txt("CustOpacityV", opacityRow.transform,
                Mods.MenuCustomiser.OpacityDisplay,
                12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            _custOpacityLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 52;
            UIHelpers.SmallBtn(opacityRow.transform, "\u25B6",
                () => { Mods.MenuCustomiser.NextOpacity(); RefreshCustomise(); });

            UIHelpers.InfoBox(c, "Below 50% opacity the menu gets hard to read.");

            UIHelpers.Divider(c);

            // ── Colour Scheme ────────────────────────────────────────
            UIHelpers.SectionHeader("COLOUR SCHEME", c);
            UIHelpers.InfoBox(c, "Pick an accent colour. Saves straight away.");
            BuildSchemeSwatches(c);

            UIHelpers.Divider(c);

            // ── Save / Reset buttons ──────────────────────────────────
            var btnRow = UIHelpers.StatRow("", c);
            UIHelpers.ActionBtn(btnRow.transform, "Save Now",
                () => { Mods.MenuCustomiser.SaveToFile(); }, 72);
            UIHelpers.ActionBtn(btnRow.transform, "Reset to Defaults",
                () => { Mods.MenuCustomiser.Reset(); RefreshCustomise(); }, 120);

            _custSavedRow = UIHelpers.Obj("SavedIndicator", c);
            var siLE = _custSavedRow.AddComponent<LayoutElement>();
            siLE.preferredHeight = 22; siLE.minHeight = 22;
            var siHlg = _custSavedRow.AddComponent<HorizontalLayoutGroup>();
            siHlg.childAlignment = TextAnchor.MiddleCenter;
            siHlg.childForceExpandWidth = false;
            siHlg.childForceExpandHeight = false;
            siHlg.spacing = 6;

            var dot = UIHelpers.Txt("SavedDot", _custSavedRow.transform,
                "\u25CF", 10, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
            dot.gameObject.AddComponent<LayoutElement>().preferredWidth = 12;

            var savedLbl = UIHelpers.Txt("SavedLbl", _custSavedRow.transform,
                "Layout saved", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
            savedLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 80;

            _custSavedRow.SetActive(false);

            UIHelpers.AddScrollForwarders(c);
            RefreshCustomise();
        }

        // ── Colour scheme swatches ───────────────────────────────────
        private static void BuildSchemeSwatches(Transform c)
        {
            var schemes = ColorSchemeManager.Presets;
            const int perRow = 4;
            int i = 0;
            while (i < schemes.Length)
            {
                var row = UIHelpers.Obj("SchemeRow" + i, c);
                var rowLe = row.AddComponent<LayoutElement>();
                rowLe.preferredHeight = 30; rowLe.minHeight = 30; rowLe.flexibleHeight = 0;
                var hlg = row.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 6;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true;

                int rowCount = Mathf.Min(perRow, schemes.Length - i);
                for (int j = 0; j < rowCount; j++)
                {
                    int idx = i + j;
                    var s = schemes[idx];
                    string label = (idx == ColorSchemeManager.CurrentIndex ? "\u2713 " : "") + s.Name;
                    var swatch = UIHelpers.Btn("Scheme" + idx, row.transform, label,
                        new Vector2(148, 28), 11,
                        () => { ColorSchemeManager.SelectScheme(idx); },
                        s.Accent, UITheme.TextOnBtn);
                    var swLe = swatch.gameObject.AddComponent<LayoutElement>();
                    swLe.preferredWidth = 148; swLe.preferredHeight = 28;
                    swLe.minWidth = 148; swLe.minHeight = 28; swLe.flexibleHeight = 0;
                }
                i += perRow;
            }
        }

        private static void RefreshCustomise()
        {
            if (_custPosLbl)
                _custPosLbl.text = Mods.MenuCustomiser.PositionLabels[Mods.MenuCustomiser.PositionPreset];
            if (_custScaleLbl)
                _custScaleLbl.text = Mods.MenuCustomiser.ScaleDisplay;
            if (_custOpacityLbl)
                _custOpacityLbl.text = Mods.MenuCustomiser.OpacityDisplay;
        }

        // ── Helpers ───────────────────────────────────────────────────
        private static Text MakeInfoRow(string label, Transform parent)
        {
            var row = UIHelpers.Panel(label + "R", parent, UIHelpers.RowBg, UIHelpers.RowSp);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 28; le.minHeight = 28; le.flexibleHeight = 0;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset((int)UIHelpers.RowPad, (int)UIHelpers.RowPad, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            var bd = UIHelpers.Panel("Bd", row.transform, UIHelpers.RowBorder, UIHelpers.RowSp);
            bd.GetComponent<Image>().raycastTarget = false;
            UIHelpers.Fill(UIHelpers.RT(bd));
            bd.AddComponent<LayoutElement>().ignoreLayout = true;
            var lbl = UIHelpers.Txt(label + "L", row.transform, label, 11,
                FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextLight);
            lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var val = UIHelpers.Txt(label + "V", row.transform, "...", 11,
                FontStyle.Normal, TextAnchor.MiddleRight, UIHelpers.TextMid);
            val.gameObject.AddComponent<LayoutElement>().preferredWidth = 200;
            return val;
        }

        private static void MakeInfoRow2(string label, string value, Transform parent,
            Color? valueColor = null)
        {
            var row = UIHelpers.Panel(label + "R", parent, UIHelpers.RowBg, UIHelpers.RowSp);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 28; le.minHeight = 28; le.flexibleHeight = 0;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset((int)UIHelpers.RowPad, (int)UIHelpers.RowPad, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            var bd = UIHelpers.Panel("Bd", row.transform, UIHelpers.RowBorder, UIHelpers.RowSp);
            bd.GetComponent<Image>().raycastTarget = false;
            UIHelpers.Fill(UIHelpers.RT(bd));
            bd.AddComponent<LayoutElement>().ignoreLayout = true;
            var lbl = UIHelpers.Txt(label + "L", row.transform, label, 11,
                FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextLight);
            lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var val = UIHelpers.Txt(label + "V", row.transform, value, 11,
                FontStyle.Normal, TextAnchor.MiddleRight, valueColor ?? UIHelpers.TextMid);
            val.gameObject.AddComponent<LayoutElement>().preferredWidth = 200;
        }

        private static void MakeLinkRow(string label, string url, Transform parent)
        {
            var row = UIHelpers.Panel(label + "R", parent, UIHelpers.RowBg, UIHelpers.RowSp);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 28; le.minHeight = 28; le.flexibleHeight = 0;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset((int)UIHelpers.RowPad, (int)UIHelpers.RowPad, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            var bd = UIHelpers.Panel("Bd", row.transform, UIHelpers.RowBorder, UIHelpers.RowSp);
            bd.GetComponent<Image>().raycastTarget = false;
            UIHelpers.Fill(UIHelpers.RT(bd));
            bd.AddComponent<LayoutElement>().ignoreLayout = true;
            var lbl = UIHelpers.Txt(label + "L", row.transform, label, 11,
                FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextLight);
            lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var val = UIHelpers.Txt(label + "V", row.transform, url, 11,
                FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.NeonBlue);
            val.gameObject.AddComponent<LayoutElement>().preferredWidth = 280;
        }

        // ── Tick ──────────────────────────────────────────────────────
        public static void Tick()
        {
            if (_custSavedRow)
                _custSavedRow.SetActive(Mods.MenuCustomiser.ShowSavedIndicator);

            if (_steamPlayerTxt && Mods.SteamPlayerCount.FetchComplete
                && _steamPlayerTxt.text == "...")
                Refresh();

            if (_verLatestTxt && UpdateChecker.CheckComplete
                && (_verLatestTxt.text == "Checking..." || _verLatestTxt.text == "..."))
                RefreshVersion();

            RepSetTick();
            InGameRepSetTick();
        }

        // ── Set-exact-rep digit input handling (mirrors MapPage.SeedTick, digits only) ──
        private static void RepSetTick()
        {
            if (!_repSetInputText) return;

            if (_repSetFocused && Input.GetMouseButtonDown(0))
            {
                if (_repSetBoxRect
                    && !RectTransformUtility.RectangleContainsScreenPoint(_repSetBoxRect, Input.mousePosition, null))
                    _repSetFocused = false;
            }

            if (_repSetCursor) _repSetCursor.gameObject.SetActive(false);

            if (!_repSetFocused) return;

            foreach (char ch in Input.inputString)
            {
                if (ch == '\b') { if (_repSetBuffer.Length > 0) _repSetBuffer = _repSetBuffer.Substring(0, _repSetBuffer.Length - 1); }
                else if (ch == '\n' || ch == '\r') { TrySetRepFromBox(); return; }
                else if (ch == (char)27) { _repSetFocused = false; return; }
                else if (char.IsDigit(ch) && _repSetBuffer.Length < 10) _repSetBuffer += ch;
            }

            if (_repSetBuffer.Length > 0)
            {
                _repSetInputText.text = UIHelpers.WithCaret(_repSetBuffer, true);
                _repSetInputText.color = UIHelpers.TextLight;
            }
            else
            {
                _repSetInputText.text = UIHelpers.WithCaret("Type exact rep value...", true);
                _repSetInputText.color = UIHelpers.TextDim;
            }
        }

        private static void TrySetRepFromBox()
        {
            string s = _repSetBuffer.Trim();
            _repSetBuffer = "";
            _repSetFocused = false;
            if ((object)_repSetInputText != null)
            {
                _repSetInputText.text = "Type exact rep value...";
                _repSetInputText.color = UIHelpers.TextDim;
            }
            if (string.IsNullOrEmpty(s)) return;

            int target;
            if (!int.TryParse(s, out target)) return;

            ModLog.Debug("[CareerReset] Setting total rep to: " + target);
            CareerReset.SetRep(target);
            RefreshCareerResult();
        }

        private static void InGameRepSetTick()
        {
            if (!_inGameRepSetInputText) return;

            if (_inGameRepSetFocused && Input.GetMouseButtonDown(0))
            {
                if (_inGameRepSetBoxRect
                    && !RectTransformUtility.RectangleContainsScreenPoint(_inGameRepSetBoxRect, Input.mousePosition, null))
                    _inGameRepSetFocused = false;
            }

            if (_inGameRepSetCursor) _inGameRepSetCursor.gameObject.SetActive(false);

            if (!_inGameRepSetFocused) return;

            foreach (char ch in Input.inputString)
            {
                if (ch == '\b') { if (_inGameRepSetBuffer.Length > 0) _inGameRepSetBuffer = _inGameRepSetBuffer.Substring(0, _inGameRepSetBuffer.Length - 1); }
                else if (ch == '\n' || ch == '\r') { TrySetInGameRepFromBox(); return; }
                else if (ch == (char)27) { _inGameRepSetFocused = false; return; }
                else if (char.IsDigit(ch) && _inGameRepSetBuffer.Length < 10) _inGameRepSetBuffer += ch;
            }

            if (_inGameRepSetBuffer.Length > 0)
            {
                _inGameRepSetInputText.text = UIHelpers.WithCaret(_inGameRepSetBuffer, true);
                _inGameRepSetInputText.color = UIHelpers.TextLight;
            }
            else
            {
                _inGameRepSetInputText.text = UIHelpers.WithCaret("Type exact in-game rep value...", true);
                _inGameRepSetInputText.color = UIHelpers.TextDim;
            }
        }

        private static void TrySetInGameRepFromBox()
        {
            string s = _inGameRepSetBuffer.Trim();
            _inGameRepSetBuffer = "";
            _inGameRepSetFocused = false;
            if ((object)_inGameRepSetInputText != null)
            {
                _inGameRepSetInputText.text = "Type exact in-game rep value...";
                _inGameRepSetInputText.color = UIHelpers.TextDim;
            }
            if (string.IsNullOrEmpty(s)) return;

            int target;
            if (!int.TryParse(s, out target)) return;

            ModLog.Debug("[CareerReset] Setting in-game rep to: " + target);
            bool ok = CareerReset.SetInGameRep(target);
            if (!ok && (object)_inGameRepSetInputText != null)
            {
                _inGameRepSetInputText.text = "Not in a session";
                _inGameRepSetInputText.color = UIHelpers.Orange;
            }
            RefreshCareerResult();
        }

        public static void ClearUiRefs()
        {
            _custSavedRow = null;
            _steamPlayerTxt = null;
            _unityVersionTxt = null;
            _mlVersionTxt = null;
            _unityMatchTxt = null;
            _telemetryStatusTxt = null;
            _verInstalledTxt = null;
            _verLatestTxt = null;
            _verStatusTxt = null;
            _pgVersion = null;
            _repSetInputText = null; _repSetCursor = null; _repSetBoxRect = null;
            _repSetFocused = false; _repSetBuffer = "";
            _inGameRepSetInputText = null; _inGameRepSetCursor = null; _inGameRepSetBoxRect = null;
            _inGameRepSetFocused = false; _inGameRepSetBuffer = "";
        }

        // ── Refresh / Rebuild ─────────────────────────────────────────
        public static void Refresh()
        {
            try
            {
                if (_unityVersionTxt) _unityVersionTxt.text = DiagnosticsManager.UnityVersion;
                if (_mlVersionTxt) _mlVersionTxt.text = DiagnosticsManager.MelonLoaderVersion;
                bool match = DiagnosticsManager.UnityVersionMatch;
                if (_unityMatchTxt)
                {
                    _unityMatchTxt.text = match
                        ? "OK \u2014 matches build target"
                        : "Mismatch! Built for " + DiagnosticsManager.BuiltForVersion;
                    _unityMatchTxt.color = match ? UIHelpers.OnColor : UIHelpers.OffColor;
                }

                if (_steamPlayerTxt)
                {
                    _steamPlayerTxt.text = Mods.SteamPlayerCount.DisplayValue;
                    _steamPlayerTxt.color = Mods.SteamPlayerCount.FetchFailed
                        ? UIHelpers.OffColor : UIHelpers.Accent;
                }

                if (_telemetryStatusTxt)
                {
                    bool telOn = Telemetry.Enabled;
                    _telemetryStatusTxt.text = telOn ? "ON" : "OFF";
                    _telemetryStatusTxt.color = telOn ? UIHelpers.OnColor : UIHelpers.OffColor;
                }

                RefreshVersion();

                if (_feedbackStatusTxt)
                {
                    switch (Telemetry.GetFeedbackState())
                    {
                        case Telemetry.FeedbackSendState.Success:
                            _feedbackStatusTxt.text = "Sent - thank you!";
                            _feedbackStatusTxt.color = UIHelpers.OnColor;
                            break;
                        case Telemetry.FeedbackSendState.Failed:
                            _feedbackStatusTxt.text = "Failed to send - please try again.";
                            _feedbackStatusTxt.color = UIHelpers.OffColor;
                            break;
                    }
                }

            }
            catch (System.Exception ex) { MelonLogger.Error("InfoPage.Refresh: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "InfoPage"); }
        }

    }

    public class FeedbackPanelUpdater : MonoBehaviour
    {
        public InputField InputField;
        public Image CaretDot;
        public Text StatusText;

        private float _blinkTimer;
        private const float BlinkInterval = 0.5f;
        private Telemetry.FeedbackSendState _lastSeenState = Telemetry.FeedbackSendState.Idle;

        private void Update()
        {
            if ((object)InputField != null && (object)CaretDot != null)
            {
                if (InputField.isFocused)
                {
                    _blinkTimer += Time.deltaTime;
                    if (_blinkTimer >= BlinkInterval)
                    {
                        _blinkTimer -= BlinkInterval;
                        CaretDot.enabled = !CaretDot.enabled;
                    }
                }
                else if (CaretDot.enabled || _blinkTimer != 0f)
                {
                    CaretDot.enabled = false;
                    _blinkTimer = 0f;
                }
            }

            if ((object)StatusText != null)
            {
                var state = Telemetry.GetFeedbackState();
                if (state != _lastSeenState)
                {
                    _lastSeenState = state;
                    if (state == Telemetry.FeedbackSendState.Success)
                    {
                        StatusText.text = "Sent - thank you!";
                        StatusText.color = UIHelpers.OnColor;
                    }
                    else if (state == Telemetry.FeedbackSendState.Failed)
                    {
                        StatusText.text = "Failed to send - please try again.";
                        StatusText.color = UIHelpers.OffColor;
                    }
                }
            }
        }
    }
}

