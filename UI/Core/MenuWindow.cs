using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using DescendersModMenu.BikeStats;
using DescendersModMenu.Mods;
using DescendersModMenu;

namespace DescendersModMenu.UI
{
    public static class MenuWindow
    {
        // ── Page 1 fields ─────────────────────────────────────────────
        private static Text accelVal, accelTogVal;
        private static Image accelBar, accelTrack;
        private static RectTransform accelKnob;
        private static Text msVal, msTogVal;
        private static Image msBar, msTrack;
        private static RectTransform msKnob;
        private static Text landTogVal;
        private static Image landTrack;
        private static RectTransform landKnob;
        private static Text landVal;
        private static Image landBar;
        private static Text bailVal;
        private static Image bailTrack;
        private static RectTransform bailKnob;
        private static Text autoBalVal, autoBalStrVal; private static Image autoBalBar;
        private static Image autoBalTrack; private static RectTransform autoBalKnob;
        private static Text _brakeTogVal = null;
        private static Image _brakeTrack = null;
        private static RectTransform _brakeKnob = null;
        private static Image _brakeLevelBar = null;
        private static Text _brakeLevelVal = null;
        private static Text nswVal;
        private static Image nswTrack;
        private static RectTransform nswKnob;
        private static Image capBg, capBdr; private static Text capTxt;
        // ── Pages ─────────────────────────────────────────────────────
        private static GameObject pg1, pg2, pg3, pg6, pg7, pg8, pg9, pg10, pg11, pg12, pg13, pg14, pg15, pg16, pg17, pg18, pg19, pg20, pg21, pg22, pg23, pg24, pg25, pg26;
        private static int cur = 1;

        public static int PendingPage = -1;

        private static readonly int[] PageOrder = { 17, 20, 19, 3, 25, 1, 24, 23, 16, 6, 8, 10, 7, 9, 11, 12, 13, 14, 15, 2, 18, 21, 22, 26 };
        private static readonly string[] NavLabels = { "\u2605 Favourites", "Search", "Key Binds", "Info", "Customise", "General", "Object Placer", "Xbox Workshop", "Session", "Move", "Bike", "Graphics", "World", "Fun", "Outfit", "Chat", "Modes", "Ghost Replay", "Maps", "Find", "Screenshot", "Other", "Perks", "Career" };
        private static readonly string[] GroupLabels = { null, null, null, null, null, "SEP", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };

        private static Image[] _navBars = new Image[24];
        private static Text[] _navTxts = new Text[24];
        private static Image[] _navBgs = new Image[24];
        private static Image[] _activeDots = new Image[24];
        private static UnityEngine.UI.Image _infoTabDot;
        private static GameObject _chatUnreadBadge;
        private static Text _chatUnreadTxt;
        private static int _lastUnreadShown = -1;

        public static CanvasGroup RootCanvasGroup { get; private set; }
        public static RectTransform RootRT { get; private set; }
        private static Text _updateStatusText;

        // ── Header button flash ───────────────────────────────────────
        private static Image _hdrSaveImg, _hdrLoadImg, _hdrResetImg;
        private static Image _hdrFlashImg = null;
        private static float _hdrFlashTimer = 0f;
        private static Text _telemetryTxt;
        private static Image _telSwitchTrack;
        private static RectTransform _telSwitchKnob;
        private static Text _allModsTxt;
        private static Image _allModsTrack;
        private static RectTransform _allModsKnob;
        private static ScrollRect _sibScroll;
        private static CanvasGroup _sibMoreHint;
        private static CanvasGroup _sibMoreHintTop;

        // ─────────────────────────────────────────────────────────────
        public static GameObject CreateMenu()
        {
            try
            {
                if (UIHelpers.GetFont() == null) { MelonLogger.Error("Font null"); Telemetry.ReportErrorAsync(new System.Exception("Font null"), "MenuWindow"); return null; }
                cur = (PendingPage >= 0) ? PendingPage : 1;
                PendingPage = -1;
                FavouritesManager.ClearStarButtons();

                var cv = new GameObject("DescendersMenu");
                var c = cv.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 999;
                var cs = cv.AddComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
                cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                cs.matchWidthOrHeight = 0.5f;
                cv.AddComponent<GraphicRaycaster>();
                var raycaster = cv.GetComponent<GraphicRaycaster>();
                cv.AddComponent<GamepadCursor>().Init(raycaster, c);

                var root = UIHelpers.Obj("Root", cv.transform);
                UIHelpers.Pin(UIHelpers.RT(root), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                    Vector2.zero, new Vector2(UIHelpers.WinW, UIHelpers.WinH));
                RootCanvasGroup = root.AddComponent<CanvasGroup>();
                RootRT = UIHelpers.RT(root);

                var frame = UIHelpers.Panel("Frame", root.transform, UIHelpers.Accent, UIHelpers.WinSp);
                frame.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                var fRT = UIHelpers.RT(frame);
                fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
                fRT.offsetMin = new Vector2(-3, -3); fRT.offsetMax = new Vector2(3, 3);
                frame.AddComponent<LayoutElement>().ignoreLayout = true;

                var win = UIHelpers.Panel("Win", root.transform, UIHelpers.WinPanel, UIHelpers.WinSp);
                UIHelpers.Fill(UIHelpers.RT(win));
                win.AddComponent<Mask>().showMaskGraphic = true;
                var hdr = UIHelpers.Panel("Hdr", win.transform, UIHelpers.HeaderBg);
                var hrt = UIHelpers.RT(hdr);
                hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1);
                hrt.pivot = new Vector2(.5f, 1);
                hrt.sizeDelta = new Vector2(0, UIHelpers.HeaderH);
                hrt.anchoredPosition = Vector2.zero;
                hdr.AddComponent<WindowDragHandler>();


                const float titleTop = -16f;
                const float titleRowH = 22f;

                var title = UIHelpers.Txt("T", hdr.transform, "DESCENDERS", 18, FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextLight);
                var trt = UIHelpers.RT(title.gameObject);
                trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1);
                trt.sizeDelta = new Vector2(138, titleRowH);
                trt.anchoredPosition = new Vector2(16, titleTop);

                var sub = UIHelpers.Txt("Sub", hdr.transform, "SANDBOX", 18, FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.Accent);
                var subrt = UIHelpers.RT(sub.gameObject);
                subrt.anchorMin = new Vector2(0, 1); subrt.anchorMax = new Vector2(0, 1);
                subrt.pivot = new Vector2(0, 1);
                subrt.sizeDelta = new Vector2(110, titleRowH);
                subrt.anchoredPosition = new Vector2(155, titleTop);

                var slash = UIHelpers.Panel("HSlash", hdr.transform, UIHelpers.Accent);
                var slrt = UIHelpers.RT(slash);
                slrt.anchorMin = new Vector2(0, 1); slrt.anchorMax = new Vector2(0, 1);
                slrt.pivot = new Vector2(0, 0.5f); slrt.sizeDelta = new Vector2(2, 20);
                slrt.anchoredPosition = new Vector2(275, titleTop - titleRowH * 0.5f);

                var verBadge = UIHelpers.Panel("VBadge", hdr.transform, UIHelpers.AccentDim, UIHelpers.BtnSp);
                var vbrt = UIHelpers.RT(verBadge);
                vbrt.anchorMin = new Vector2(0, 1); vbrt.anchorMax = new Vector2(0, 1);
                vbrt.pivot = new Vector2(0, 0.5f); vbrt.sizeDelta = new Vector2(58f, 20f);
                vbrt.anchoredPosition = new Vector2(288, titleTop - titleRowH * 0.5f);
                var vbBdr = UIHelpers.Panel("VBBdr", verBadge.transform, UIHelpers.AccentBdr, UIHelpers.BtnSp);
                vbBdr.GetComponent<Image>().raycastTarget = false; UIHelpers.Fill(UIHelpers.RT(vbBdr));
                vbBdr.AddComponent<LayoutElement>().ignoreLayout = true;
                var verTxt = UIHelpers.Txt("VT", verBadge.transform, "v" + BuildInfo.Version, 10, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                UIHelpers.Fill(UIHelpers.RT(verTxt.gameObject));

                var allLbl = UIHelpers.Txt("AllLbl", hdr.transform, "All Mods", 9, FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextMid);
                var allLblRt = UIHelpers.RT(allLbl.gameObject);
                allLblRt.anchorMin = new Vector2(0, 0.5f); allLblRt.anchorMax = new Vector2(0, 0.5f);
                allLblRt.pivot = new Vector2(0, 0.5f);
                allLblRt.sizeDelta = new Vector2(54, 16);
                allLblRt.anchoredPosition = new Vector2(16, -26);
                allLbl.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                _allModsTxt = UIHelpers.Txt("AllState", hdr.transform, "ON", 9, FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.OnColor);
                var allStateRt = UIHelpers.RT(_allModsTxt.gameObject);
                allStateRt.anchorMin = new Vector2(0, 0.5f); allStateRt.anchorMax = new Vector2(0, 0.5f);
                allStateRt.pivot = new Vector2(0, 0.5f);
                allStateRt.sizeDelta = new Vector2(26, 16);
                allStateRt.anchoredPosition = new Vector2(70, -26);
                _allModsTxt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                BuildHeaderToggle(hdr.transform, "AllSwitch", new Vector2(98, -27),
                    new Vector2(0, 0.5f), () => { AllModsSwitch.Toggle(); },
                    out _allModsTrack, out _allModsKnob);
                RefreshAllModsSwitch();

                var byTxt = UIHelpers.Txt("By", hdr.transform, "Created by NateHyden", 9, FontStyle.Normal, TextAnchor.UpperRight, UIHelpers.TextMid);
                var byrt = UIHelpers.RT(byTxt.gameObject);
                byrt.anchorMin = new Vector2(1, 1); byrt.anchorMax = new Vector2(1, 1);
                byrt.pivot = new Vector2(1, 1);
                byrt.sizeDelta = new Vector2(280, 14);
                byrt.anchoredPosition = new Vector2(-8, -3);
                byrt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                var usTxt = UIHelpers.Txt("UST", hdr.transform,
                    "checking...", 10, FontStyle.Bold, TextAnchor.UpperRight, UIHelpers.TextDim);
                _updateStatusText = usTxt;
                var usrt = UIHelpers.RT(usTxt.gameObject);
                usrt.anchorMin = new Vector2(1, 1); usrt.anchorMax = new Vector2(1, 1);
                usrt.pivot = new Vector2(1, 1);
                usrt.sizeDelta = new Vector2(280, 14);
                usrt.anchoredPosition = new Vector2(-8, -17);
                usTxt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                var telLbl = UIHelpers.Txt("TelLbl", hdr.transform, "Telemetry", 9, FontStyle.Bold, TextAnchor.UpperRight, UIHelpers.TextMid);
                var telLblRt = UIHelpers.RT(telLbl.gameObject);
                telLblRt.anchorMin = new Vector2(1, 1); telLblRt.anchorMax = new Vector2(1, 1);
                telLblRt.pivot = new Vector2(1, 1);
                telLblRt.sizeDelta = new Vector2(60, 16);
                telLblRt.anchoredPosition = new Vector2(-92, -35);
                telLbl.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                _telemetryTxt = UIHelpers.Txt("TelState", hdr.transform, "ON", 9, FontStyle.Bold, TextAnchor.UpperRight, UIHelpers.OnColor);
                var telStateRt = UIHelpers.RT(_telemetryTxt.gameObject);
                telStateRt.anchorMin = new Vector2(1, 1); telStateRt.anchorMax = new Vector2(1, 1);
                telStateRt.pivot = new Vector2(1, 1);
                telStateRt.sizeDelta = new Vector2(26, 16);
                telStateRt.anchoredPosition = new Vector2(-56, -35);
                _telemetryTxt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                BuildHeaderToggle(hdr.transform, "TelSwitch", new Vector2(-8, -32),
                    new Vector2(1, 1), () => { Telemetry.Toggle(); RefreshTelemetryLabel(); },
                    out _telSwitchTrack, out _telSwitchKnob);
                RefreshTelemetryLabel();

                if (!Telemetry.HeaderHintDismissed)
                {
                    var telRow = UIHelpers.Obj("TelExplRow", hdr.transform);
                    var telRowRt = UIHelpers.RT(telRow);
                    telRowRt.anchorMin = new Vector2(1, 1); telRowRt.anchorMax = new Vector2(1, 1);
                    telRowRt.pivot = new Vector2(1, 1);
                    telRowRt.anchoredPosition = new Vector2(-8, -51);
                    telRowRt.sizeDelta = new Vector2(0, 20);
                    telRow.AddComponent<LayoutElement>().ignoreLayout = true;
                    var telRowHlg = telRow.AddComponent<HorizontalLayoutGroup>();
                    telRowHlg.spacing = 6;
                    telRowHlg.childAlignment = TextAnchor.MiddleRight;
                    telRowHlg.childForceExpandWidth = false; telRowHlg.childForceExpandHeight = false;
                    telRowHlg.childControlWidth = true; telRowHlg.childControlHeight = true;
                    var telRowFitter = telRow.AddComponent<ContentSizeFitter>();
                    telRowFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

                    var telDismissGo = UIHelpers.Obj("TelDismiss", telRow.transform);
                    var telDismissImg = telDismissGo.AddComponent<Image>();
                    telDismissImg.sprite = UIHelpers.BtnSp; telDismissImg.type = Image.Type.Sliced;
                    telDismissImg.color = UIHelpers.RowBg;
                    var telDismissLe = telDismissGo.AddComponent<LayoutElement>();
                    telDismissLe.preferredWidth = 16; telDismissLe.preferredHeight = 16;
                    telDismissLe.minWidth = 16; telDismissLe.minHeight = 16;
                    var telDismissBtn = telDismissGo.AddComponent<Button>();
                    telDismissBtn.targetGraphic = telDismissImg;
                    var telDismissTxt = UIHelpers.Txt("X", telDismissGo.transform, "\u2715", 9,
                        FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                    UIHelpers.Fill(UIHelpers.RT(telDismissTxt.gameObject));

                    var telExpl = UIHelpers.Obj("TelExpl", telRow.transform);
                    var telExplText = telExpl.AddComponent<Text>();
                    telExplText.font = UIHelpers.GetFont();
                    telExplText.fontSize = 9; telExplText.fontStyle = FontStyle.Normal;
                    telExplText.alignment = TextAnchor.MiddleLeft;
                    telExplText.color = Color.white;
                    telExplText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    telExplText.verticalOverflow = VerticalWrapMode.Overflow;
                    telExplText.raycastTarget = false;
                    telExplText.text = "Please read telemetry page in Info";
                    var telExplLe = telExpl.AddComponent<LayoutElement>();
                    telExplLe.preferredWidth = telExplText.preferredWidth;
                    telExplLe.preferredHeight = 20;

                    telDismissBtn.onClick.AddListener(() =>
                    {
                        Telemetry.DismissHeaderHint();
                        UnityEngine.Object.Destroy(telRow);
                    });
                }

                _hdrSaveImg = HeaderBtn(hdr.transform, "SAVE", 383f, () => { StatsManager.SaveStats(); FlashHeader(_hdrSaveImg); });
                _hdrLoadImg = HeaderBtn(hdr.transform, "LOAD", 443f, () => { StatsManager.LoadStats(); RefreshAll(); FlashHeader(_hdrLoadImg); });
                _hdrResetImg = HeaderBtn(hdr.transform, "RESET", 503f, () => { StatsManager.ResetStats(); RefreshAll(); FlashHeader(_hdrResetImg); });

                var slrHint = UIHelpers.Txt("SLRHint", hdr.transform,
                    "(Saves, Loads and resets active mods)",
                    8, FontStyle.Normal, TextAnchor.UpperCenter, Color.white);
                var slrHintRt = UIHelpers.RT(slrHint.gameObject);
                slrHintRt.anchorMin = new Vector2(0, 1); slrHintRt.anchorMax = new Vector2(0, 1);
                slrHintRt.pivot = new Vector2(0.5f, 1);
                slrHintRt.sizeDelta = new Vector2(180, 16);
                slrHintRt.anchoredPosition = new Vector2(469f, -42f);
                slrHint.horizontalOverflow = HorizontalWrapMode.Overflow;
                slrHint.verticalOverflow = VerticalWrapMode.Overflow;
                slrHint.raycastTarget = false;
                slrHint.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                var body = UIHelpers.Obj("Body", win.transform);
                var bodyRT = UIHelpers.RT(body);
                bodyRT.anchorMin = Vector2.zero; bodyRT.anchorMax = Vector2.one;
                bodyRT.offsetMin = new Vector2(0, 0); bodyRT.offsetMax = new Vector2(0, -UIHelpers.HeaderH);

                var sidebar = UIHelpers.Panel("Sidebar", body.transform, UIHelpers.SidebarBg);
                var sibRT = UIHelpers.RT(sidebar);
                sibRT.anchorMin = Vector2.zero; sibRT.anchorMax = new Vector2(0, 1);
                sibRT.offsetMin = Vector2.zero; sibRT.offsetMax = new Vector2(UIHelpers.SidebarW, 0);
                var sibBorder = UIHelpers.Panel("SibBorder", sidebar.transform, UIHelpers.WinBorder);
                var sbrt2 = UIHelpers.RT(sibBorder);
                sbrt2.anchorMin = new Vector2(1, 0); sbrt2.anchorMax = new Vector2(1, 1);
                sbrt2.pivot = new Vector2(1, 0.5f); sbrt2.sizeDelta = new Vector2(1, 0);
                sbrt2.anchoredPosition = Vector2.zero;
                sibBorder.AddComponent<LayoutElement>().ignoreLayout = true;

                var sibScroll = UIHelpers.Obj("SibScroll", sidebar.transform);
                UIHelpers.Fill(UIHelpers.RT(sibScroll));
                var sibSR = sibScroll.AddComponent<ScrollRect>();
                sibSR.horizontal = false; sibSR.vertical = true;
                sibSR.movementType = ScrollRect.MovementType.Clamped;
                sibSR.scrollSensitivity = 20f; sibSR.inertia = false;

                var sibVP = UIHelpers.Obj("SibVP", sibScroll.transform);
                UIHelpers.Fill(UIHelpers.RT(sibVP));
                sibVP.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                sibVP.AddComponent<Mask>().showMaskGraphic = true;
                sibSR.viewport = UIHelpers.RT(sibVP);

                var sibContent = UIHelpers.Obj("SibContent", sibVP.transform);
                var sibCRT = UIHelpers.RT(sibContent);
                sibCRT.anchorMin = new Vector2(0, 1); sibCRT.anchorMax = new Vector2(1, 1);
                sibCRT.pivot = new Vector2(0.5f, 1); sibCRT.sizeDelta = new Vector2(0, 0);
                sibSR.content = sibCRT;
                UIHelpers.AddScrollbar(sibSR);
                _sibScroll = sibSR;

                var moreHint = UIHelpers.Obj("MoreHint", sidebar.transform);
                var moreRt = UIHelpers.RT(moreHint);
                moreRt.anchorMin = new Vector2(0, 0); moreRt.anchorMax = new Vector2(1, 0);
                moreRt.pivot = new Vector2(0.5f, 0);
                moreRt.sizeDelta = new Vector2(0, 26);
                moreRt.anchoredPosition = Vector2.zero;
                moreHint.AddComponent<LayoutElement>().ignoreLayout = true;
                var moreFade = UIHelpers.Panel("Fade", moreHint.transform, new Color(0.04f, 0.05f, 0.08f, 0.82f));
                UIHelpers.Fill(UIHelpers.RT(moreFade));
                moreFade.GetComponent<Image>().raycastTarget = false;
                var moreTxt = UIHelpers.Txt("Arr", moreHint.transform, "\u25BC", 13, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                UIHelpers.Fill(UIHelpers.RT(moreTxt.gameObject));
                moreTxt.raycastTarget = false;
                _sibMoreHint = moreHint.AddComponent<CanvasGroup>();
                _sibMoreHint.alpha = 0f;
                _sibMoreHint.blocksRaycasts = false;
                _sibMoreHint.interactable = false;

                var moreHintTop = UIHelpers.Obj("MoreHintTop", sidebar.transform);
                var moreTopRt = UIHelpers.RT(moreHintTop);
                moreTopRt.anchorMin = new Vector2(0, 1); moreTopRt.anchorMax = new Vector2(1, 1);
                moreTopRt.pivot = new Vector2(0.5f, 1);
                moreTopRt.sizeDelta = new Vector2(0, 26);
                moreTopRt.anchoredPosition = Vector2.zero;
                moreHintTop.AddComponent<LayoutElement>().ignoreLayout = true;
                var moreTopFade = UIHelpers.Panel("Fade", moreHintTop.transform, new Color(0.04f, 0.05f, 0.08f, 0.82f));
                UIHelpers.Fill(UIHelpers.RT(moreTopFade));
                moreTopFade.GetComponent<Image>().raycastTarget = false;
                var moreTopTxt = UIHelpers.Txt("Arr", moreHintTop.transform, "\u25B2", 13, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                UIHelpers.Fill(UIHelpers.RT(moreTopTxt.gameObject));
                moreTopTxt.raycastTarget = false;
                _sibMoreHintTop = moreHintTop.AddComponent<CanvasGroup>();
                _sibMoreHintTop.alpha = 0f;
                _sibMoreHintTop.blocksRaycasts = false;
                _sibMoreHintTop.interactable = false;

                sibContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var sVlg = sibContent.AddComponent<VerticalLayoutGroup>();
                sVlg.spacing = 1; sVlg.padding = new RectOffset(4, 4, 2, 6);
                sVlg.childAlignment = TextAnchor.UpperCenter;
                sVlg.childForceExpandWidth = true; sVlg.childForceExpandHeight = false;

                for (int i = 0; i < PageOrder.Length; i++)
                {
                    // ── Group separator ────────────────────────────────
                    if (GroupLabels[i] != null)
                    {
                        var sep = UIHelpers.Obj("Sep" + i, sibContent.transform);
                        var sepLe = sep.AddComponent<LayoutElement>();
                        sepLe.preferredHeight = 18; sepLe.minHeight = 18; sepLe.flexibleHeight = 0;
                        var sepLine = UIHelpers.Panel("SepLine", sep.transform, UIHelpers.RowBorder);
                        var slRT = UIHelpers.RT(sepLine);
                        slRT.anchorMin = new Vector2(0.1f, 0.5f); slRT.anchorMax = new Vector2(0.9f, 0.5f);
                        slRT.pivot = new Vector2(0.5f, 0.5f); slRT.sizeDelta = new Vector2(0, 1);
                        slRT.anchoredPosition = Vector2.zero;
                        sepLine.AddComponent<LayoutElement>().ignoreLayout = true;
                    }

                    int navIdx = i;
                    int pageNum = PageOrder[i];
                    var item = UIHelpers.Obj("Nav" + i, sibContent.transform);
                    var ile = item.AddComponent<LayoutElement>();
                    ile.preferredHeight = 28; ile.minHeight = 28; ile.flexibleHeight = 0;
                    var bg = UIHelpers.Panel("Bg", item.transform, new Color(0, 0, 0, 0), UIHelpers.NavSp);
                    UIHelpers.Fill(UIHelpers.RT(bg)); _navBgs[i] = bg.GetComponent<Image>();
                    var barGlow = UIHelpers.Panel("BarGlow", item.transform, UITheme.NavGlow);
                    var bgRT2 = UIHelpers.RT(barGlow);
                    bgRT2.anchorMin = Vector2.zero; bgRT2.anchorMax = new Vector2(0, 1);
                    bgRT2.pivot = new Vector2(0, .5f); bgRT2.offsetMin = Vector2.zero; bgRT2.offsetMax = new Vector2(6, 0);
                    barGlow.GetComponent<Image>().enabled = false;
                    var bar = UIHelpers.Panel("Bar", item.transform, new Color(0, 0, 0, 0));
                    var barRT = UIHelpers.RT(bar);
                    barRT.anchorMin = Vector2.zero; barRT.anchorMax = new Vector2(0, 1);
                    barRT.pivot = new Vector2(0, .5f); barRT.offsetMin = Vector2.zero; barRT.offsetMax = new Vector2(3, 0);
                    _navBars[i] = bar.GetComponent<Image>();
                    var lbl = UIHelpers.Txt("L", item.transform, NavLabels[i], 11, FontStyle.Bold, TextAnchor.MiddleLeft, UITheme.NavInactiveText);
                    var lblRT = UIHelpers.RT(lbl.gameObject);
                    lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
                    lblRT.offsetMin = new Vector2(18, 0); lblRT.offsetMax = Vector2.zero;
                    _navTxts[i] = lbl;

                    var dotObj = UIHelpers.Obj("ActiveDot", item.transform);
                    var dotImg = dotObj.AddComponent<Image>();
                    dotImg.sprite = UIHelpers.DotSp;
                    dotImg.type = Image.Type.Simple;
                    dotImg.color = UIHelpers.OnColor;
                    dotImg.enabled = false;
                    _activeDots[i] = dotImg;
                    var drt = UIHelpers.RT(dotObj);
                    drt.anchorMin = new Vector2(1f, 0.5f); drt.anchorMax = new Vector2(1f, 0.5f);
                    drt.pivot = new Vector2(1f, 0.5f);
                    drt.sizeDelta = new Vector2(6, 6);
                    drt.anchoredPosition = new Vector2(-8, 0);
                    dotObj.AddComponent<LayoutElement>().ignoreLayout = true;

                    if (pageNum == 3)
                    {
                        var infoDotObj = UIHelpers.Obj("InfoDot", item.transform);
                        var infoDotImg = infoDotObj.AddComponent<UnityEngine.UI.Image>();
                        infoDotImg.sprite = UIHelpers.DotSp; infoDotImg.type = UnityEngine.UI.Image.Type.Simple; infoDotImg.color = UIHelpers.OnColor;
                        _infoTabDot = infoDotImg;
                        var idrt = UIHelpers.RT(infoDotObj);
                        idrt.anchorMin = new Vector2(1f, 0.5f); idrt.anchorMax = new Vector2(1f, 0.5f);
                        idrt.pivot = new Vector2(1f, 0.5f); idrt.sizeDelta = new Vector2(7, 7); idrt.anchoredPosition = new Vector2(-10, 0);
                        infoDotObj.AddComponent<LayoutElement>().ignoreLayout = true;
                    }
                    if (pageNum == 12)
                    {
                        var badge = UIHelpers.Panel("ChatUnread", item.transform, UIHelpers.Orange, UIHelpers.BtnSp);
                        _chatUnreadBadge = badge;
                        var brt = UIHelpers.RT(badge);
                        brt.anchorMin = new Vector2(1f, 0.5f); brt.anchorMax = new Vector2(1f, 0.5f);
                        brt.pivot = new Vector2(1f, 0.5f);
                        brt.sizeDelta = new Vector2(18, 14);
                        brt.anchoredPosition = new Vector2(-6, 0);
                        badge.AddComponent<LayoutElement>().ignoreLayout = true;
                        _chatUnreadTxt = UIHelpers.Txt("CU", badge.transform, "0", 9,
                            FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                        UIHelpers.Fill(UIHelpers.RT(_chatUnreadTxt.gameObject));
                        badge.SetActive(false);
                    }
                    var btn = item.AddComponent<Button>();
                    btn.onClick.AddListener(() => Switch(PageOrder[navIdx]));
                    btn.targetGraphic = bg.GetComponent<Image>();
                    var bcol = btn.colors;
                    bcol.normalColor = Color.white; bcol.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1);
                    bcol.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1); bcol.colorMultiplier = 1; btn.colors = bcol;
                }

                var cont = UIHelpers.Obj("Cnt", body.transform);
                var crt = UIHelpers.RT(cont);
                crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
                crt.offsetMin = new Vector2(UIHelpers.SidebarW, 0); crt.offsetMax = Vector2.zero;

                pg1 = UIHelpers.Obj("P1", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg1)); BuildPage1(pg1.transform);
                pg2 = UIHelpers.Obj("P2", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg2));
                try { EspPage.CreatePage(pg2.transform); }
                catch (System.Exception espEx) { MelonLogger.Error("CreateMenu: EspPage failed - " + espEx);  Telemetry.ReportErrorAsync(espEx, "MenuWindow"); }
                pg3 = UIHelpers.Obj("P3", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg3));
                try { InfoPage.CreatePage(pg3.transform); }
                catch (System.Exception infoEx) { MelonLogger.Error("CreateMenu: InfoPage failed - " + infoEx);  Telemetry.ReportErrorAsync(infoEx, "MenuWindow"); }
                pg25 = UIHelpers.Obj("P25", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg25));
                try { InfoPage.CreateCustomisePage(pg25.transform); }
                catch (System.Exception custEx) { MelonLogger.Error("CreateMenu: CustomisePage failed - " + custEx); Telemetry.ReportErrorAsync(custEx, "MenuWindow"); }
                pg26 = UIHelpers.Obj("P26", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg26));
                try { InfoPage.CreateCareerPage(pg26.transform); }
                catch (System.Exception carEx) { MelonLogger.Error("CreateMenu: CareerPage failed - " + carEx); Telemetry.ReportErrorAsync(carEx, "MenuWindow"); }
                pg6 = UIHelpers.Obj("P6", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg6)); MovePage.CreatePage(pg6.transform);
                pg7 = UIHelpers.Obj("P7", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg7)); WorldPage.CreatePage(pg7.transform);
                pg8 = UIHelpers.Obj("P8", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg8)); BikePage.CreatePage(pg8.transform);
                pg9 = UIHelpers.Obj("P9", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg9)); FunPage.CreatePage(pg9.transform);
                pg10 = UIHelpers.Obj("P10", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg10)); GraphicsPage.CreatePage(pg10.transform);
                pg11 = UIHelpers.Obj("P11", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg11));
                try { OutfitPage.CreatePage(pg11.transform); }
                catch (System.Exception outfitEx) { MelonLogger.Error("CreateMenu: OutfitPage failed - " + outfitEx);  Telemetry.ReportErrorAsync(outfitEx, "MenuWindow"); }
                pg12 = UIHelpers.Obj("P12", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg12)); ChatPage.CreatePage(pg12.transform);
                pg13 = UIHelpers.Obj("P13", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg13)); ModesPage.CreatePage(pg13.transform);
                pg14 = UIHelpers.Obj("P14", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg14)); GhostPage.CreatePage(pg14.transform);
                pg15 = UIHelpers.Obj("P15", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg15)); MapPage.CreatePage(pg15.transform);

                pg16 = UIHelpers.Obj("P16", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg16)); SessionPage.CreatePage(pg16.transform);

                pg17 = UIHelpers.Obj("P17", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg17)); FavsPage.CreatePage(pg17.transform);
                pg20 = UIHelpers.Obj("P20", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg20)); SearchPage.CreatePage(pg20.transform);
                pg18 = UIHelpers.Obj("P18", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg18)); ScreenshotPage.CreatePage(pg18.transform);
                pg19 = UIHelpers.Obj("P19", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg19)); BindsPage.CreatePage(pg19.transform);
                pg21 = UIHelpers.Obj("P21", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg21)); OtherPage.CreatePage(pg21.transform);
                pg22 = UIHelpers.Obj("P22", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg22)); PerksPage.CreatePage(pg22.transform);
                pg23 = UIHelpers.Obj("P23", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg23)); XboxWorkshopPage.CreatePage(pg23.transform);
                pg24 = UIHelpers.Obj("P24", cont.transform); UIHelpers.Fill(UIHelpers.RT(pg24)); ObjectPlacerPage.CreatePage(pg24.transform);

                RefreshAll(); RefreshTabs();
                Mods.MenuCustomiser.LoadFromFile();
                cv.SetActive(false);
                return cv;
            }
            catch (System.Exception ex) { MelonLogger.Error("CreateMenu: " + ex); Telemetry.ReportErrorAsync(ex, "MenuWindow"); return null; }
        }

        // ── Page 1 (General) ──────────────────────────────────────────
        private static void BuildPage1(Transform p)
        {
            var scrollObj = UIHelpers.Obj("Scroll", p);
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
            crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 0);
            sr.content = crt;
            UIHelpers.AddScrollbar(sr);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = UIHelpers.RowGap;
            vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            var pg = content.transform;

            UIHelpers.SectionHeader("BIKE PHYSICS", pg);

            var ar = UIHelpers.StatRow("Acceleration", pg);
            accelTogVal = UIHelpers.Txt("ATV", ar.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
            accelTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            UIHelpers.Toggle(ar.transform, "AT", () => { Acceleration.Toggle(); RefreshAll(); }, out accelTrack, out accelKnob);
            accelBar = UIHelpers.MakeBar("AB", ar.transform, (Acceleration.Level - 1) / 9f);
            accelVal = UIHelpers.Txt("AV", ar.transform, Acceleration.Level.ToString(), 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
            accelVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
            UIHelpers.SmallBtn(ar.transform, "-", () => { Acceleration.Decrease(); RefreshAll(); });
            UIHelpers.SmallBtn(ar.transform, "+", () => { Acceleration.Increase(); RefreshAll(); });

            var mso = UIHelpers.Panel("MSR", pg, UIHelpers.RowBg, UIHelpers.RowSp);
            mso.AddComponent<LayoutElement>().minHeight = UIHelpers.RowH + 38;
            var mbd = UIHelpers.Panel("MBd", mso.transform, UIHelpers.RowBorder, UIHelpers.RowSp);
            mbd.GetComponent<Image>().raycastTarget = false; UIHelpers.Fill(UIHelpers.RT(mbd));
            mbd.AddComponent<LayoutElement>().ignoreLayout = true;
            var mvlg = mso.AddComponent<VerticalLayoutGroup>();
            mvlg.spacing = 4; mvlg.padding = new RectOffset((int)UIHelpers.RowPad, (int)UIHelpers.RowPad, 6, 8);
            mvlg.childAlignment = TextAnchor.UpperCenter;
            mvlg.childForceExpandWidth = true; mvlg.childForceExpandHeight = false;

            var mst = UIHelpers.Obj("MST", mso.transform);
            mst.AddComponent<LayoutElement>().preferredHeight = 28;
            var mhlg = mst.AddComponent<HorizontalLayoutGroup>();
            mhlg.spacing = 8; mhlg.childAlignment = TextAnchor.MiddleCenter;
            mhlg.childForceExpandWidth = false; mhlg.childForceExpandHeight = false;

            var msll = UIHelpers.Txt("MSL", mst.transform, "Max Speed", 12, FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextLight)
                .gameObject.AddComponent<LayoutElement>();
            msll.flexibleWidth = 1; msll.preferredHeight = 28;

            msTogVal = UIHelpers.Txt("MSTV", mst.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
            msTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            UIHelpers.Toggle(mst.transform, "MST2", () => { MaxSpeedMultiplier.Toggle(); RefreshAll(); }, out msTrack, out msKnob);
            msBar = UIHelpers.MakeBar("MSB", mst.transform, (MaxSpeedMultiplier.Level - 1) / 9f);
            msVal = UIHelpers.Txt("MSV", mst.transform, MaxSpeedMultiplier.Level.ToString(), 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
            msVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
            UIHelpers.SmallBtn(mst.transform, "-", () => { MaxSpeedMultiplier.Decrease(); RefreshAll(); });
            UIHelpers.SmallBtn(mst.transform, "+", () => { MaxSpeedMultiplier.Increase(); RefreshAll(); });

            var cap = UIHelpers.Obj("Cap", mso.transform);
            capBg = cap.AddComponent<Image>(); capBg.sprite = UIHelpers.BtnSp;
            capBg.type = Image.Type.Sliced; capBg.color = UIHelpers.NeonBlue;
            var cbtn = cap.AddComponent<Button>();
            cbtn.onClick.AddListener(() => { NoSpeedCap.Toggle(); RefreshAll(); });
            var ccb = cbtn.colors; ccb.normalColor = Color.white; ccb.highlightedColor = new Color(1, 1, 1, 1.15f);
            ccb.pressedColor = new Color(.7f, .7f, .7f, 1); ccb.colorMultiplier = 1; ccb.fadeDuration = .08f;
            cbtn.colors = ccb;
            cap.AddComponent<LayoutElement>().preferredHeight = 30;
            var cbd = UIHelpers.Panel("CBd", cap.transform, UIHelpers.NeonBlue, UIHelpers.BtnSp);
            capBdr = cbd.GetComponent<Image>(); capBdr.raycastTarget = false; UIHelpers.Fill(UIHelpers.RT(cbd));
            capTxt = UIHelpers.Txt("CT", cap.transform, "REMOVE SPEED CAP", 11, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0, 0, 0, 1));
            capTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIHelpers.Fill(UIHelpers.RT(capTxt.gameObject));

            // ── Landing Impact ────────────────────────────────────────
            var lr = UIHelpers.StatRow("Landing Impact", pg);
            landTogVal = UIHelpers.Txt("LTV", lr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
            landTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            UIHelpers.Toggle(lr.transform, "LT", () => { LandingImpact.Toggle(); RefreshAll(); }, out landTrack, out landKnob);
            landBar = UIHelpers.MakeBar("LB", lr.transform, (LandingImpact.Level - 1) / 9f);
            landVal = UIHelpers.Txt("LV", lr.transform, LandingImpact.DisplayValue, 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
            landVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            UIHelpers.SmallBtn(lr.transform, "-", () => { LandingImpact.Decrease(); RefreshAll(); });
            UIHelpers.SmallBtn(lr.transform, "+", () => { LandingImpact.Increase(); RefreshAll(); });
            UIHelpers.InfoBox(pg, "Raises the impact speed required to bail. Level 200 = almost impossible to fall off.");

            // ── No Bail ───────────────────────────────────────────────
            var nr = UIHelpers.StatRow("No Bail", pg);
            bailVal = UIHelpers.Txt("NV", nr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
            bailVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            UIHelpers.Toggle(nr.transform, "NT", () => { NoBail.Toggle(); RefreshAll(); }, out bailTrack, out bailKnob);

            // ── Auto Balance ──────────────────────────────────────────
            var abTogRow = UIHelpers.StatRow("Auto Balance", pg);
            autoBalVal = UIHelpers.Txt("ABV", abTogRow.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
            autoBalVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            UIHelpers.Toggle(abTogRow.transform, "ABT", () => { AutoBalance.Toggle(); RefreshAll(); }, out autoBalTrack, out autoBalKnob);
            autoBalBar = UIHelpers.MakeBar("ABB", abTogRow.transform, (AutoBalance.StrengthLevel - 1) / 9f);
            autoBalStrVal = UIHelpers.Txt("ABS", abTogRow.transform, AutoBalance.StrengthLevel.ToString(), 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
            autoBalStrVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
            UIHelpers.SmallBtn(abTogRow.transform, "-", () => { AutoBalance.StrengthDecrease(); RefreshAll(); });
            UIHelpers.SmallBtn(abTogRow.transform, "+", () => { AutoBalance.StrengthIncrease(); RefreshAll(); });

            // ── No Speed Wobbles ──────────────────────────────────────
            var nswr = UIHelpers.StatRow("No Speed Wobbles", pg);
            nswVal = UIHelpers.Txt("NwV", nswr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
            nswVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            UIHelpers.Toggle(nswr.transform, "NwT", () => { GameModifierMods.NoSpeedWobblesToggle(); RefreshAll(); }, out nswTrack, out nswKnob);

            // ── QUICK ACTIONS ─────────────────────────────────────────
            UIHelpers.Divider(pg);
            UIHelpers.SectionHeader("QUICK ACTIONS", pg);
            // ── Brake toggle row ─────────────────────────────────────
            var brakeRow = UIHelpers.StatRow("Quick Brake", pg);
            _brakeTogVal = UIHelpers.Txt("BkTV", brakeRow.transform, "OFF", 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
            _brakeTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            UIHelpers.Toggle(brakeRow.transform, "BkT", () =>
            {
                QuickBrake.Toggle();
                RefreshAll();
            }, out _brakeTrack, out _brakeKnob);
            _brakeLevelBar = UIHelpers.MakeBar("BkB", brakeRow.transform, (QuickBrake.Level - 1) / 9f);
            _brakeLevelVal = UIHelpers.Txt("BkLV", brakeRow.transform,
                QuickBrake.Level == 10 ? "MAX" : QuickBrake.Level.ToString(), 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
            _brakeLevelVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            UIHelpers.SmallBtn(brakeRow.transform, "-", () =>
            { QuickBrake.Decrease(); RefreshAll(); });
            UIHelpers.SmallBtn(brakeRow.transform, "+", () =>
            { QuickBrake.Increase(); RefreshAll(); });
            UIHelpers.InfoBox(pg, "Level 1-9: fast drag deceleration. Level 10 (MAX): truly instant stop.");
            // ── Launch button row ─────────────────────────────────────
            var qar = UIHelpers.StatRow("Actions", pg);
            UIHelpers.ActionBtn(qar.transform, "Launch", () =>
            {
                try
                {
                    GameObject player = GameObject.Find("Player_Human");
                    if ((object)player == null) return;
                    Vehicle v = player.GetComponent<Vehicle>();
                    if ((object)v == null) return;
                    System.Reflection.MethodInfo setVel = typeof(Vehicle).GetMethod(
                        "SetVelocity",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if ((object)setVel == null) return;
                    Vector3 launchVec = player.transform.forward * 80f + Vector3.up * 20f;
                    setVel.Invoke(v, new object[] { launchVec });
                }
                catch (System.Exception ex) { MelonLogger.Error("[SuperLaunch] " + ex.Message);  Telemetry.ReportErrorAsync(ex, "MenuWindow"); }
            }, 60);
            UIHelpers.InfoBox(pg, "Launch: fires you forward at high speed.");

            FavouritesManager.RegisterStarButton("Acceleration", UIHelpers.StarBtn(ar.transform, "Acceleration", () => FavouritesManager.Toggle("Acceleration")));
            FavouritesManager.RegisterStarButton("MaxSpeed", UIHelpers.StarBtn(mst.transform, "MaxSpeed", () => FavouritesManager.Toggle("MaxSpeed")));
            FavouritesManager.RegisterStarButton("NoSpeedCap", UIHelpers.StarBtnAbs(cap.transform, "NoSpeedCap", () => FavouritesManager.Toggle("NoSpeedCap")));
            FavouritesManager.RegisterStarButton("LandingImpact", UIHelpers.StarBtn(lr.transform, "LandingImpact", () => FavouritesManager.Toggle("LandingImpact")));
            FavouritesManager.RegisterStarButton("NoBail", UIHelpers.StarBtn(nr.transform, "NoBail", () => FavouritesManager.Toggle("NoBail")));
            FavouritesManager.RegisterStarButton("AutoBalance", UIHelpers.StarBtn(abTogRow.transform, "AutoBalance", () => FavouritesManager.Toggle("AutoBalance")));
            FavouritesManager.RegisterStarButton("NoSpeedWobbles", UIHelpers.StarBtn(nswr.transform, "NoSpeedWobbles", () => FavouritesManager.Toggle("NoSpeedWobbles")));
            FavouritesManager.RegisterStarButton("QuickBrake", UIHelpers.StarBtn(brakeRow.transform, "QuickBrake", () => FavouritesManager.Toggle("QuickBrake")));
            FavouritesManager.RegisterStarButton("Launch", UIHelpers.StarBtn(qar.transform, "Launch", () => FavouritesManager.Toggle("Launch")));

            FavouritesManager.Register(new ModFavEntry
            {
                Id = "Acceleration",
                DisplayName = "Acceleration",
                TabBadge = "GENERAL",
                BuildControls = (fp) => FavsPage.BuildToggleSlider(fp, "Acceleration", "Acceleration",
                    () => Mods.Acceleration.Enabled, () => Mods.Acceleration.Toggle(),
                    () => Mods.Acceleration.Level, () => Mods.Acceleration.Increase(), () => Mods.Acceleration.Decrease(),
                    10, () => (Mods.Acceleration.Level - 1) / 9f, () => RefreshAll()),
                IsActive = () => Mods.Acceleration.Enabled
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "MaxSpeed",
                DisplayName = "Max Speed",
                TabBadge = "GENERAL",
                BuildControls = (fp) => FavsPage.BuildToggleSlider(fp, "MaxSpeed", "Max Speed",
                    () => Mods.MaxSpeedMultiplier.Enabled, () => Mods.MaxSpeedMultiplier.Toggle(),
                    () => Mods.MaxSpeedMultiplier.Level, () => Mods.MaxSpeedMultiplier.Increase(), () => Mods.MaxSpeedMultiplier.Decrease(),
                    10, () => (Mods.MaxSpeedMultiplier.Level - 1) / 9f, () => RefreshAll()),
                IsActive = () => Mods.MaxSpeedMultiplier.Enabled
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "NoSpeedCap",
                DisplayName = "No Speed Cap",
                TabBadge = "GENERAL",
                BuildControls = (fp) => FavsPage.BuildSimpleToggle(fp, "NoSpeedCap", "No Speed Cap",
                    () => Mods.NoSpeedCap.Enabled, () => Mods.NoSpeedCap.Toggle(), () => RefreshAll()),
                IsActive = () => Mods.NoSpeedCap.Enabled
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "LandingImpact",
                DisplayName = "Landing Impact",
                TabBadge = "GENERAL",
                BuildControls = (fp) => FavsPage.BuildToggleSlider(fp, "LandingImpact", "Landing Impact",
                    () => Mods.LandingImpact.Enabled, () => Mods.LandingImpact.Toggle(),
                    () => Mods.LandingImpact.Level, () => Mods.LandingImpact.Increase(), () => Mods.LandingImpact.Decrease(),
                    10, () => (Mods.LandingImpact.Level - 1) / 9f, () => RefreshAll(),
                    () => Mods.LandingImpact.DisplayValue),
                IsActive = () => Mods.LandingImpact.Enabled
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "NoBail",
                DisplayName = "No Bail",
                TabBadge = "GENERAL",
                BuildControls = (fp) => FavsPage.BuildSimpleToggle(fp, "NoBail", "No Bail",
                    () => Mods.NoBail.Enabled, () => Mods.NoBail.Toggle(), () => RefreshAll()),
                IsActive = () => Mods.NoBail.Enabled
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "AutoBalance",
                DisplayName = "Auto Balance",
                TabBadge = "GENERAL",
                BuildControls = (fp) => FavsPage.BuildToggleSlider(fp, "AutoBalance", "Auto Balance",
                    () => Mods.AutoBalance.Enabled, () => Mods.AutoBalance.Toggle(),
                    () => Mods.AutoBalance.StrengthLevel, () => Mods.AutoBalance.StrengthIncrease(), () => Mods.AutoBalance.StrengthDecrease(),
                    10, () => (Mods.AutoBalance.StrengthLevel - 1) / 9f, () => RefreshAll()),
                IsActive = () => Mods.AutoBalance.Enabled
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "NoSpeedWobbles",
                DisplayName = "No Speed Wobbles",
                TabBadge = "GENERAL",
                BuildControls = (fp) => FavsPage.BuildSimpleToggle(fp, "NoSpeedWobbles", "No Speed Wobbles",
                    () => Mods.GameModifierMods.NoSpeedWobblesEnabled, () => Mods.GameModifierMods.NoSpeedWobblesToggle(), () => RefreshAll()),
                IsActive = () => Mods.GameModifierMods.NoSpeedWobblesEnabled
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "QuickBrake",
                DisplayName = "Quick Brake",
                TabBadge = "GENERAL",
                BuildControls = (fp) => FavsPage.BuildToggleSlider(fp, "QuickBrake", "Quick Brake",
                    () => Mods.QuickBrake.Enabled, () => Mods.QuickBrake.Toggle(),
                    () => Mods.QuickBrake.Level, () => Mods.QuickBrake.Increase(), () => Mods.QuickBrake.Decrease(),
                    10, () => (Mods.QuickBrake.Level - 1) / 9f, () => RefreshAll()),
                IsActive = () => Mods.QuickBrake.Enabled
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "Launch",
                DisplayName = "Super Launch",
                TabBadge = "GENERAL",
                BuildControls = (fp) => {
                    var row = UIHelpers.StatRow("Actions", fp);
                    UIHelpers.ActionBtn(row.transform, "Launch", () => {
                        try
                        {
                            GameObject player = GameObject.Find("Player_Human");
                            if ((object)player == null) return;
                            Vehicle v = player.GetComponent<Vehicle>();
                            if ((object)v == null) return;
                            var setVel = typeof(Vehicle).GetMethod("SetVelocity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            if ((object)setVel == null) return;
                            setVel.Invoke(v, new object[] { player.transform.forward * 80f + Vector3.up * 20f });
                        }
                        catch (System.Exception ex) { MelonLogger.Error("[SuperLaunch] " + ex.Message);  Telemetry.ReportErrorAsync(ex, "MenuWindow"); }
                    }, 60);
                },
                IsActive = () => false
            });
            UIHelpers.AddScrollForwarders(content.transform);
        }

        private static Image HeaderBtn(Transform hdr, string lbl, float x, UnityEngine.Events.UnityAction clk)
        {
            var g = UIHelpers.Obj(lbl + "HB", hdr);
            var rt = UIHelpers.RT(g);
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(52, 22);
            rt.anchoredPosition = new Vector2(x, -27f);
            var im = g.AddComponent<Image>(); im.sprite = UIHelpers.BtnSp;
            im.type = Image.Type.Sliced; im.color = UIHelpers.NeonBlue;
            var btn = g.AddComponent<Button>(); btn.onClick.AddListener(clk);
            var bc = btn.colors;
            bc.normalColor = Color.white; bc.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            bc.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f); bc.colorMultiplier = 1; btn.colors = bc;
            var t = UIHelpers.Txt(lbl + "HBT", g.transform, lbl, 9, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Color(0f, 0f, 0f, 1f));
            UIHelpers.Fill(UIHelpers.RT(t.gameObject));
            return im;
        }

        private static void BuildHeaderToggle(Transform hdr, string name, Vector2 pos, Vector2 corner,
            UnityEngine.Events.UnityAction clk, out Image track, out RectTransform knob)
        {
            var g = UIHelpers.Obj(name, hdr);
            var rt = UIHelpers.RT(g);
            rt.anchorMin = corner; rt.anchorMax = corner;
            rt.pivot = corner;
            rt.sizeDelta = new Vector2(34, 16);
            rt.anchoredPosition = pos;
            g.AddComponent<LayoutElement>().ignoreLayout = true;

            track = g.AddComponent<Image>();
            track.sprite = UIHelpers.TogSp; track.type = Image.Type.Sliced;
            track.color = UIHelpers.TogOffTrack;

            var tbdr = UIHelpers.Panel("TBdr", g.transform, UIHelpers.RowBorder, UIHelpers.TogSp);
            tbdr.GetComponent<Image>().raycastTarget = false;
            UIHelpers.Fill(UIHelpers.RT(tbdr));
            tbdr.AddComponent<LayoutElement>().ignoreLayout = true;

            var b = g.AddComponent<Button>();
            b.onClick.AddListener(clk);
            b.targetGraphic = track;
            var cb = b.colors;
            cb.normalColor = Color.white; cb.highlightedColor = Color.white;
            cb.pressedColor = Color.white; cb.colorMultiplier = 1;
            b.colors = cb;

            var k = UIHelpers.Obj("K", g.transform);
            var ki = k.AddComponent<Image>();
            ki.sprite = UIHelpers.KnobSp; ki.type = Image.Type.Sliced;
            ki.color = UIHelpers.TogKnobOff;
            ki.raycastTarget = false;
            knob = UIHelpers.RT(k);
            knob.anchorMin = new Vector2(0, 0.5f); knob.anchorMax = new Vector2(0, 0.5f);
            knob.pivot = new Vector2(0, 0.5f);
            knob.sizeDelta = new Vector2(10, 10);
            knob.anchoredPosition = new Vector2(3, 0);
        }

        private static void SetHeaderToggle(Image track, RectTransform knob, bool on)
        {
            if (track) track.color = on ? UIHelpers.TogOnTrack : UIHelpers.TogOffTrack;
            Transform tbdr = (object)track != null ? track.transform.Find("TBdr") : null;
            if ((object)tbdr != null)
            {
                var tbdrImg = tbdr.GetComponent<Image>();
                if (tbdrImg) tbdrImg.color = on ? UIHelpers.AccentBdr : UIHelpers.RowBorder;
            }
            if (knob)
            {
                knob.anchoredPosition = new Vector2(on ? 21f : 3f, 0f);
                var ki = knob.GetComponent<Image>();
                if (ki) ki.color = on ? UIHelpers.TogKnobOn : UIHelpers.TogKnobOff;
            }
        }

        public static void RefreshAllModsSwitch()
        {
            if (!_allModsTxt) return;
            bool on = AllModsSwitch.Enabled;
            _allModsTxt.text = on ? "ON" : "OFF";
            _allModsTxt.color = on ? UIHelpers.OnColor : UIHelpers.OffColor;
            SetHeaderToggle(_allModsTrack, _allModsKnob, on);
        }

        private static void RefreshTelemetryLabel()
        {
            if (!_telemetryTxt) return;
            bool on = Telemetry.Enabled;
            _telemetryTxt.text = on ? "ON" : "OFF";
            _telemetryTxt.color = on ? UIHelpers.OnColor : UIHelpers.OffColor;
            SetHeaderToggle(_telSwitchTrack, _telSwitchKnob, on);
        }

        private static void BotBtn(string lbl, Transform p, Color bg, UnityEngine.Events.UnityAction clk)
        {
            var g = UIHelpers.Obj(lbl + "B", p);
            var im = g.AddComponent<Image>(); im.sprite = UIHelpers.BtnSp; im.type = Image.Type.Sliced; im.color = bg;
            var b = g.AddComponent<Button>(); b.onClick.AddListener(clk);
            var cb = b.colors; cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1); cb.colorMultiplier = 1; b.colors = cb;
            var t = UIHelpers.Txt("L", g.transform, lbl.ToUpper(), 12, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0, 0, 0, 1));
            UIHelpers.Fill(UIHelpers.RT(t.gameObject));
        }

        // ── Navigation ────────────────────────────────────────────────
        private static bool IsPageActive(int pageNum)
        {
            try
            {
                switch (pageNum)
                {
                    case 1:
                        return Mods.Acceleration.Enabled || Mods.MaxSpeedMultiplier.Enabled ||
                                    Mods.NoSpeedCap.Enabled || Mods.LandingImpact.Enabled ||
                                    Mods.QuickBrake.Enabled || Mods.NoBail.Enabled ||
                                    Mods.AutoBalance.Enabled ||
                                    Mods.GameModifierMods.NoSpeedWobblesEnabled;
                    case 6: return MovePage.IsAnyActive;
                    case 7: return WorldPage.IsAnyActive;
                    case 8: return BikePage.IsAnyActive;
                    case 9: return FunPage.IsAnyActive;
                    case 10: return GraphicsPage.IsAnyActive;
                    case 13: return ModesPage.IsAnyActive;
                    case 14: return Mods.GhostReplay.Enabled;
                    case 16: return SessionPage.IsAnyActive;
                    case 17: return FavsPage.IsAnyActive;
                    case 18: return ScreenshotPage.IsAnyActive;
                    case 19: return BindsPage.IsAnyActive;
                    case 20: return false;
                    case 21: return OtherPage.IsAnyActive;
                    case 22: return false;
                    case 23: return false;
                    case 24: return ObjectPlacerPage.IsAnyActive;
                    default: return false;
                }
            }
            catch { return false; }
        }

        private static int _lastCur = -1;

        private static void Switch(int pg)
        {
            cur = pg;
            if (pg == 12) ModChat.MarkAsRead();
            RefreshTabs();
            UpdateChatUnreadBadge(true);
        }

        public static void GoToPage(int pg) { Switch(pg); }

        public static bool IsChatOpen
        {
            get { return MenuUI.IsOpen && cur == 12; }
        }

        private static void RefreshTabs()
        {
            if (_lastCur == 26 && cur != 26)
                InfoPage.OnCareerTabClosed();

            if (pg1) pg1.SetActive(cur == 1); if (pg2) pg2.SetActive(cur == 2);
            if (pg3) pg3.SetActive(cur == 3); if (pg6) pg6.SetActive(cur == 6);
            if (pg7) pg7.SetActive(cur == 7); if (pg8) pg8.SetActive(cur == 8);
            if (pg9) pg9.SetActive(cur == 9); if (pg10) pg10.SetActive(cur == 10);
            if (pg11) pg11.SetActive(cur == 11);
            if (cur != 11) OutfitPage.CancelRename(); if (pg12) pg12.SetActive(cur == 12);
            if (pg13) pg13.SetActive(cur == 13); if (pg14) pg14.SetActive(cur == 14);
            if (pg15) pg15.SetActive(cur == 15);
            if (pg16) pg16.SetActive(cur == 16);
            if (pg17) pg17.SetActive(cur == 17);
            if (cur == 17) FavsPage.CheckDirty();
            if (pg20) pg20.SetActive(cur == 20);
            if (pg18) pg18.SetActive(cur == 18);
            if (pg19) pg19.SetActive(cur == 19);
            if (pg21) pg21.SetActive(cur == 21);
            if (pg22) pg22.SetActive(cur == 22);
            if (pg23) pg23.SetActive(cur == 23);
            if (pg24) pg24.SetActive(cur == 24);
            if (cur == 24) ObjectPlacerPage.RefreshAll();
            if (pg25) pg25.SetActive(cur == 25);
            if (cur == 25) InfoPage.RefreshCustomisePage();
            if (pg26) pg26.SetActive(cur == 26);
            if (cur == 26)
            {
                InfoPage.OnCareerTabOpened();
                InfoPage.RefreshCareerPage();
            }

            _lastCur = cur;

            for (int i = 0; i < PageOrder.Length; i++)
            {
                bool on = PageOrder[i] == cur;
                bool active = IsPageActive(PageOrder[i]);
                if (_navBars[i]) _navBars[i].color = new Color(0, 0, 0, 0);
                if (_navTxts[i]) _navTxts[i].color = on ? UIHelpers.Accent : UITheme.NavInactiveText;
                if (_navBgs[i]) _navBgs[i].color = on ? UIHelpers.NavActive : new Color(0, 0, 0, 0);
                if (_activeDots[i]) _activeDots[i].enabled = active && !on;
            }
            if (cur == 2) EspPage.RefreshTexts();
            if (cur == 3) InfoPage.Refresh();
            if (cur == 22) { Mods.PerkMenu.ForceReload(); PerksPage.Rebuild(); }
            if (_infoTabDot) _infoTabDot.color = DiagnosticsManager.FailCount > 0 ? UIHelpers.OffColor : UIHelpers.OnColor;
            UpdateChatUnreadBadge(true);
        }

        private static void UpdateChatUnreadBadge(bool force)
        {
            if (!_chatUnreadBadge) return;
            int n = ModChat.UnreadCount;
            if (!force && n == _lastUnreadShown) return;
            _lastUnreadShown = n;
            if (n <= 0)
            {
                _chatUnreadBadge.SetActive(false);
                return;
            }
            _chatUnreadBadge.SetActive(true);
            if (_chatUnreadTxt)
                _chatUnreadTxt.text = n > 99 ? "99+" : n.ToString();
            var rt = UIHelpers.RT(_chatUnreadBadge);
            rt.sizeDelta = new Vector2(n > 9 ? 22f : 18f, 14f);
        }

        // ── RefreshAll ────────────────────────────────────────────────
        public static void RefreshAll()
        {
            // ── Acceleration ──────────────────────────────────────────
            bool acOn = Acceleration.Enabled;
            if (accelTogVal) { accelTogVal.text = acOn ? "ON" : "OFF"; accelTogVal.color = acOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(accelTrack, accelKnob, acOn);
            if (accelVal) accelVal.text = Acceleration.Level.ToString();
            UIHelpers.SetBar(accelBar, (Acceleration.Level - 1) / 9f);

            // ── Max Speed ─────────────────────────────────────────────
            bool msOn = MaxSpeedMultiplier.Enabled;
            if (msTogVal) { msTogVal.text = msOn ? "ON" : "OFF"; msTogVal.color = msOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(msTrack, msKnob, msOn);
            if (msVal) msVal.text = MaxSpeedMultiplier.Level.ToString();
            UIHelpers.SetBar(msBar, (MaxSpeedMultiplier.Level - 1) / 9f);

            // ── No Speed Cap ──────────────────────────────────────────
            bool cap2 = NoSpeedCap.Enabled;
            if (capTxt) { capTxt.text = cap2 ? "SPEED CAP REMOVED" : "REMOVE SPEED CAP"; capTxt.color = new Color(0, 0, 0, 1); }
            if (capBg) capBg.color = cap2 ? UIHelpers.OnColor : UIHelpers.NeonBlue;
            if (capBdr) capBdr.color = cap2 ? UIHelpers.OnColor : UIHelpers.NeonBlue;

            // ── Landing Impact ────────────────────────────────────────
            bool liOn = LandingImpact.Enabled;
            if (landTogVal) { landTogVal.text = liOn ? "ON" : "OFF"; landTogVal.color = liOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(landTrack, landKnob, liOn);
            if (landVal) landVal.text = LandingImpact.DisplayValue;
            UIHelpers.SetBar(landBar, (LandingImpact.Level - 1) / 9f);

            // ── Quick Brake ───────────────────────────────────────────
            bool qbOn = QuickBrake.Enabled;
            if (_brakeTogVal) { _brakeTogVal.text = qbOn ? "ON" : "OFF"; _brakeTogVal.color = qbOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_brakeTrack, _brakeKnob, qbOn);
            if (_brakeLevelBar) UIHelpers.SetBar(_brakeLevelBar, (QuickBrake.Level - 1) / 9f);
            if (_brakeLevelVal) { _brakeLevelVal.text = QuickBrake.Level == 10 ? "MAX" : QuickBrake.Level.ToString(); }

            // ── No Bail ───────────────────────────────────────────────
            bool bail = NoBail.Enabled;
            if (bailVal) { bailVal.text = bail ? "ON" : "OFF"; bailVal.color = bail ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(bailTrack, bailKnob, bail);

            // ── Auto Balance ──────────────────────────────────────────
            bool ab = AutoBalance.Enabled;
            if (autoBalVal) { autoBalVal.text = ab ? "ON" : "OFF"; autoBalVal.color = ab ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(autoBalTrack, autoBalKnob, ab);
            if (autoBalStrVal) autoBalStrVal.text = AutoBalance.StrengthLevel.ToString();
            UIHelpers.SetBar(autoBalBar, (AutoBalance.StrengthLevel - 1) / 9f);

            // ── No Speed Wobbles ──────────────────────────────────────
            bool nsw = GameModifierMods.NoSpeedWobblesEnabled;
            if (nswVal) { nswVal.text = nsw ? "ON" : "OFF"; nswVal.color = nsw ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(nswTrack, nswKnob, nsw);

            SessionPage.RefreshAll();
            try { BikePage.RefreshAll(); } catch (System.Exception ex) { MelonLogger.Error("BikePage.RefreshAll: " + ex); Telemetry.ReportErrorAsync(ex, "BikePage.RefreshAll"); }
            try { MovePage.RefreshAll(); } catch (System.Exception ex) { MelonLogger.Error("MovePage.RefreshAll: " + ex); Telemetry.ReportErrorAsync(ex, "MovePage.RefreshAll"); }
            try { FunPage.RefreshAll(); } catch (System.Exception ex) { MelonLogger.Error("FunPage.RefreshAll: " + ex); Telemetry.ReportErrorAsync(ex, "FunPage.RefreshAll"); }
            try { WorldPage.RefreshAll(); } catch (System.Exception ex) { MelonLogger.Error("WorldPage.RefreshAll: " + ex); Telemetry.ReportErrorAsync(ex, "WorldPage.RefreshAll"); }
            try { OtherPage.RefreshAll(); } catch (System.Exception ex) { MelonLogger.Error("OtherPage.RefreshAll: " + ex); Telemetry.ReportErrorAsync(ex, "OtherPage.RefreshAll"); }
            try { ModesPage.RefreshAll(); } catch (System.Exception ex) { MelonLogger.Error("ModesPage.RefreshAll: " + ex); Telemetry.ReportErrorAsync(ex, "ModesPage.RefreshAll"); }

            // ── Favourites sync ───────────────────────────────────────
            try { FavsPage.RefreshFavourites(); } catch (System.Exception ex) { MelonLogger.Error("FavsPage.RefreshFavourites: " + ex); Telemetry.ReportErrorAsync(ex, "FavsPage.RefreshFavourites"); }
            try { FavouritesManager.RefreshAllStars(); } catch (System.Exception ex) { MelonLogger.Error("FavouritesManager.RefreshAllStars: " + ex); Telemetry.ReportErrorAsync(ex, "FavouritesManager.RefreshAllStars"); }
            try { RefreshTabs(); } catch { }
        }

        private static void FlashHeader(Image img)
        {
            if ((object)img == null) return;
            if ((object)_hdrFlashImg != null && (object)_hdrFlashImg != (object)img)
                _hdrFlashImg.color = UIHelpers.NeonBlue;
            _hdrFlashImg = img;
            _hdrFlashTimer = 1.5f;
            img.color = UIHelpers.OnColor;
        }

        public static void TickLive()
        {
            if (MenuUI.IsOpen) SessionPage.TickLive();

            if (!UnityNull.Alive(_updateStatusText))
            {
                _updateStatusText = null;
            }
            else if (!UpdateChecker.CheckComplete)
            {
                _updateStatusText.text = "checking for updates...";
                _updateStatusText.color = UIHelpers.TextDim;
                _updateStatusText.fontStyle = FontStyle.Normal;
            }
            else if (UpdateChecker.UpdateAvailable)
            {
                _updateStatusText.text = "\u25B2 v" + UpdateChecker.LatestVersion + " available";
                _updateStatusText.color = new UnityEngine.Color(1f, 0.20f, 0.20f, 1f);
                _updateStatusText.fontStyle = FontStyle.Bold;
            }
            else
            {
                _updateStatusText.text = "\u2713 v" + BuildInfo.Version + " up to date";
                _updateStatusText.color = UIHelpers.OnColor;
                _updateStatusText.fontStyle = FontStyle.Normal;
            }

            if (_hdrFlashTimer > 0f)
            {
                _hdrFlashTimer -= UnityEngine.Time.deltaTime;
                if (_hdrFlashTimer <= 0f && UnityNull.Alive(_hdrFlashImg))
                {
                    _hdrFlashImg.color = UIHelpers.NeonBlue;
                    _hdrFlashImg = null;
                }
                else if (_hdrFlashTimer <= 0f)
                    _hdrFlashImg = null;
            }

            if (MenuUI.IsOpen) UpdateSidebarMoreHint();
            if (MenuUI.IsOpen) UpdateChatUnreadBadge(false);
        }

        private static void UpdateSidebarMoreHint()
        {
            if (!_sibScroll)
            {
                _sibMoreHint = null;
                _sibMoreHintTop = null;
                _sibScroll = null;
                return;
            }

            try
            {
                bool showBottom = false;
                bool showTop = false;
                RectTransform content = _sibScroll.content;
                RectTransform vp = _sibScroll.viewport;
                if (content && vp)
                {
                    float extra = content.rect.height - vp.rect.height;
                    bool overflow = extra > 10f;
                    float v = _sibScroll.verticalNormalizedPosition;
                    bool atTop = v >= 0.96f;
                    bool atBottom = v <= 0.04f;
                    showBottom = overflow && atTop;
                    showTop = overflow && atBottom;
                }

                float step = Time.unscaledDeltaTime * 8f;
                if (_sibMoreHint)
                    _sibMoreHint.alpha = Mathf.MoveTowards(_sibMoreHint.alpha, showBottom ? 1f : 0f, step);
                if (_sibMoreHintTop)
                    _sibMoreHintTop.alpha = Mathf.MoveTowards(_sibMoreHintTop.alpha, showTop ? 1f : 0f, step);
            }
            catch (System.Exception)
            {
                _sibMoreHint = null;
                _sibMoreHintTop = null;
                _sibScroll = null;
            }
        }
    }
}

