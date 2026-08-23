using System;
using MelonLoader;
using DescendersModMenu;
using DescendersModMenu.Mods;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class BindsPage
    {
        private static int _listeningSlot = -1;
        private static int _conflictSlot  = -1;
        private static int _conflictWith  = -1;
        private static int _conflictCode  = 0;
        private static KeyCode _conflictKey = KeyCode.None;
        private static bool _listeningMenuOpen = false;

        private static string _queryBuffer = "";
        private static bool _queryFocused = false;
        private static Text _queryInputText;
        private static RectTransform _queryBoxRect;

        public static bool IsListening { get { return _listeningSlot >= 0 || _conflictSlot >= 0 || _listeningMenuOpen; } }
        public static bool IsQueryFocused => _queryFocused;
        public static bool IsAnyActive { get { return false; } }

        private static Transform _contentRoot;
        private static ScrollRect _scrollRect;
        private static bool _hasBuiltOnce = false;

        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            _hasBuiltOnce = false;
            try
            {
                pg = UIHelpers.Obj("PBinds", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));

                var scrollObj = UIHelpers.Obj("Scroll", pg.transform);
                UIHelpers.Fill(UIHelpers.RT(scrollObj));
                _scrollRect = scrollObj.AddComponent<ScrollRect>();
                _scrollRect.horizontal = false; _scrollRect.vertical = true;
                _scrollRect.movementType = ScrollRect.MovementType.Clamped;
                _scrollRect.scrollSensitivity = 25f; _scrollRect.inertia = false;

                var vp = UIHelpers.Obj("VP", scrollObj.transform);
                UIHelpers.Fill(UIHelpers.RT(vp));
                vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                vp.AddComponent<Mask>().showMaskGraphic = true;
                _scrollRect.viewport = UIHelpers.RT(vp);

                var content = UIHelpers.Obj("Content", vp.transform);
                var crt = UIHelpers.RT(content);
                crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = Vector2.zero;
                _scrollRect.content = crt;
                UIHelpers.AddScrollbar(_scrollRect);

                content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

                _contentRoot = content.transform;
                Rebuild();
            }
            catch (Exception ex) { MelonLogger.Error("[BindsPage] CreatePage: " + ex);  Telemetry.ReportErrorAsync(ex, "BindsPage"); }
            return pg;
        }

        public static void Rebuild()
        {
            if ((object)_contentRoot == null) return;
            try
            {
                float savedScroll = (!_hasBuiltOnce || (object)_scrollRect == null)
                    ? 1f : _scrollRect.verticalNormalizedPosition;
                _hasBuiltOnce = true;
                _queryInputText = null;
                _queryBoxRect = null;

                for (int i = _contentRoot.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(_contentRoot.GetChild(i).gameObject);

                UIHelpers.SectionHeader("MENU ACCESS", _contentRoot);
                BuildMenuOpenRow();
                UIHelpers.Divider(_contentRoot);

                UIHelpers.SectionHeader("KEY BINDINGS", _contentRoot);
                BuildSearchRow();

                var hint = UIHelpers.StatRow("", _contentRoot);
                var hintTxt = UIHelpers.Txt("Hint", hint.transform,
                    "Click BIND then press a keyboard key or controller button.  ESC cancels.",
                    9, FontStyle.Normal, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                hintTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                UIHelpers.Divider(_contentRoot);

                string q = _queryBuffer.Trim();
                int shown = 0;
                int count = KeyBindManager.Count;
                for (int i = 0; i < count; i++)
                {
                    string label = KeyBindManager.ModLabels[i];
                    if (q.Length > 0
                        && label.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0
                        && KeyBindManager.ModIds[i].IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    int capturedSlot = i;
                    bool isListening = (_listeningSlot == i);
                    bool isConflict  = (_conflictSlot  == i);

                    if (isListening)       BuildListeningRow(label, capturedSlot);
                    else if (isConflict)   BuildConflictRow(label, capturedSlot);
                    else                   BuildNormalRow(label, capturedSlot);
                    shown++;
                }

                if (shown == 0)
                {
                    UIHelpers.InfoBox(_contentRoot,
                        string.IsNullOrEmpty(q) ? "No bindable mods." : "No binds match \"" + q + "\".",
                        Color.white);
                }

                UIHelpers.AddScrollForwarders(_contentRoot);
                var crtRT = UIHelpers.RT(_contentRoot.gameObject);
                LayoutRebuilder.ForceRebuildLayoutImmediate(crtRT);
                Canvas.ForceUpdateCanvases();
                if ((object)_scrollRect != null) _scrollRect.verticalNormalizedPosition = savedScroll;
            }
            catch (Exception ex) { MelonLogger.Error("[BindsPage] Rebuild: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "BindsPage"); }
        }

        private static void BuildSearchRow()
        {
            var searchRow = UIHelpers.Obj("SrRow", _contentRoot);
            searchRow.AddComponent<LayoutElement>().preferredHeight = 28;
            var sHlg = searchRow.AddComponent<HorizontalLayoutGroup>();
            sHlg.spacing = 6;
            sHlg.childAlignment = TextAnchor.MiddleCenter;
            sHlg.childForceExpandWidth = false;
            sHlg.childForceExpandHeight = true;
            sHlg.childControlWidth = true;
            sHlg.childControlHeight = true;

            var searchBg = UIHelpers.Obj("SrBg", searchRow.transform);
            searchBg.AddComponent<Image>().color = UIHelpers.WinOuter;
            var sbgLe = searchBg.AddComponent<LayoutElement>();
            sbgLe.flexibleWidth = 1; sbgLe.minHeight = 26; sbgLe.preferredHeight = 26;
            var sbgHlg = searchBg.AddComponent<HorizontalLayoutGroup>();
            sbgHlg.padding = new RectOffset(8, 8, 0, 0);
            sbgHlg.childAlignment = TextAnchor.MiddleLeft;
            sbgHlg.childForceExpandWidth = true;
            sbgHlg.childForceExpandHeight = true;

            string shown = string.IsNullOrEmpty(_queryBuffer)
                ? (_queryFocused ? UIHelpers.WithCaret("Search binds...", true) : "Search binds...")
                : (_queryFocused ? UIHelpers.WithCaret(_queryBuffer, true) : _queryBuffer);
            _queryInputText = UIHelpers.Txt("SrIT", searchBg.transform, shown, 11,
                FontStyle.Normal, TextAnchor.MiddleLeft,
                string.IsNullOrEmpty(_queryBuffer) ? UIHelpers.TextDim : UIHelpers.TextLight);
            _queryInputText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _queryInputText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            _queryBoxRect = UIHelpers.RT(searchBg);
            var focusBtn = searchBg.AddComponent<Button>();
            focusBtn.targetGraphic = searchBg.GetComponent<Image>();
            focusBtn.onClick.AddListener(() => { _queryFocused = true; });

            if (!string.IsNullOrEmpty(_queryBuffer))
            {
                UIHelpers.SmallBtn(searchRow.transform, "\u2716", () =>
                {
                    _queryBuffer = "";
                    _queryFocused = false;
                    Rebuild();
                });
            }
        }

        private static void BuildMenuOpenRow()
        {
            var row = UIHelpers.StatRow("Open Menu (Controller)", _contentRoot);
            if (_listeningMenuOpen)
            {
                UIHelpers.SetRowActive(row, true);
                var promptTxt = UIHelpers.Txt("MOP", row.transform, "Press any controller button\u2026",
                    11, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
                promptTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 160;
                UIHelpers.ActionBtn(row.transform, "ESC", () => { _listeningMenuOpen = false; Rebuild(); }, 40);
            }
            else
            {
                int code = KeyBindManager.GetMenuOpenCode();
                string name = KeyBindManager.ControllerName(code);
                var keyTxt = UIHelpers.Txt("MOK", row.transform, name,
                    11, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
                keyTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 120;
                UIHelpers.ActionBtn(row.transform, "BIND", () =>
                {
                    _listeningSlot = -1; ClearConflict();
                    _queryFocused = false;
                    _listeningMenuOpen = true;
                    Rebuild();
                }, 40);
            }
        }

        private static void BuildNormalRow(string label, int slot)
        {
            var row = UIHelpers.StatRow(label, _contentRoot);
            bool hasBind = KeyBindManager.HasBind(slot);
            string keyName = KeyBindManager.GetBindDisplay(slot);
            Color keyCol = hasBind ? UIHelpers.Accent : UIHelpers.TextDim;
            var keyTxt = UIHelpers.Txt("KN_" + slot, row.transform, keyName,
                11, hasBind ? FontStyle.Bold : FontStyle.Normal, TextAnchor.MiddleRight, keyCol);
            keyTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 110;

            UIHelpers.ActionBtn(row.transform, "BIND", () =>
            {
                _listeningMenuOpen = false;
                _queryFocused = false;
                _listeningSlot = slot; _conflictSlot = -1; _conflictWith = -1;
                _conflictKey = KeyCode.None; _conflictCode = 0;
                Rebuild();
            }, 40);

            if (hasBind)
            {
                UIHelpers.ActionBtnOrange(row.transform, "\u2715", () =>
                {
                    KeyBindManager.ClearKey(slot);
                    KeyBindManager.SaveBindings();
                    Rebuild();
                }, 24);
            }
            else
            {
                var spacer = UIHelpers.Obj("Sp_" + slot, row.transform);
                spacer.AddComponent<LayoutElement>().preferredWidth = 24;
            }
        }

        private static void BuildListeningRow(string label, int slot)
        {
            var row = UIHelpers.StatRow(label, _contentRoot);
            UIHelpers.SetRowActive(row, true);
            var promptTxt = UIHelpers.Txt("LP_" + slot, row.transform, "Key or controller\u2026",
                11, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
            promptTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 110;
            UIHelpers.ActionBtn(row.transform, "ESC", () => { CancelListen(); }, 40);
            var spacer = UIHelpers.Obj("SpL_" + slot, row.transform);
            spacer.AddComponent<LayoutElement>().preferredWidth = 24;
        }

        private static void BuildConflictRow(string label, int slot)
        {
            var row = UIHelpers.StatRow(label, _contentRoot);
            UIHelpers.SetRowActive(row, true);
            string otherLabel = (_conflictWith >= 0 && _conflictWith < KeyBindManager.Count)
                ? KeyBindManager.ModLabels[_conflictWith] : "another mod";
            var warnTxt = UIHelpers.Txt("CW_" + slot, row.transform, "Used by: " + otherLabel,
                9, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Orange);
            warnTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 120;
            UIHelpers.ActionBtn(row.transform, "STEAL", () =>
            {
                KeyBindManager.ClearKey(_conflictWith);
                if (_conflictCode < 0) KeyBindManager.SetCode(_conflictSlot, _conflictCode);
                else KeyBindManager.SetKey(_conflictSlot, _conflictKey);
                KeyBindManager.SaveBindings();
                KeyBindManager.SkipNextCheck();
                ClearConflict(); Rebuild();
            }, 48);
            UIHelpers.ActionBtnOrange(row.transform, "CANCEL", () => { ClearConflict(); Rebuild(); }, 52);
        }

        public static void Tick()
        {
            if ((object)_queryInputText == null) return;

            if (_queryFocused && Input.GetMouseButtonDown(0))
            {
                if ((object)_queryBoxRect != null
                    && !RectTransformUtility.RectangleContainsScreenPoint(_queryBoxRect, Input.mousePosition, null))
                    _queryFocused = false;
            }

            if (!_queryFocused)
            {
                if (!string.IsNullOrEmpty(_queryBuffer) && _queryInputText)
                {
                    _queryInputText.text = _queryBuffer;
                    _queryInputText.color = UIHelpers.TextLight;
                }
                return;
            }

            bool changed = false;
            foreach (char ch in Input.inputString)
            {
                if (ch == '\b')
                {
                    if (_queryBuffer.Length > 0) { _queryBuffer = _queryBuffer.Substring(0, _queryBuffer.Length - 1); changed = true; }
                }
                else if (ch == '\n' || ch == '\r' || ch == (char)27) { _queryFocused = false; }
                else if (_queryBuffer.Length < 40) { _queryBuffer += ch; changed = true; }
            }

            if (_queryInputText)
            {
                if (_queryBuffer.Length > 0)
                {
                    _queryInputText.text = UIHelpers.WithCaret(_queryBuffer, true);
                    _queryInputText.color = UIHelpers.TextLight;
                }
                else
                {
                    _queryInputText.text = UIHelpers.WithCaret("Search binds...", true);
                    _queryInputText.color = UIHelpers.TextDim;
                }
            }

            if (changed) Rebuild();
        }

        public static void CheckController()
        {
            int code;
            if (!KeyBindManager.AnyControllerPressed(out code)) return;

            if (_listeningMenuOpen)
            {
                KeyBindManager.SetMenuOpenCode(code);
                KeyBindManager.SaveBindings();
                KeyBindManager.SkipMenuOpenCheck();
                _listeningMenuOpen = false;
                Rebuild();
                return;
            }

            if (_listeningSlot < 0) return;

            // Don't bind the same button used to open the menu.
            if (code == KeyBindManager.GetMenuOpenCode())
            {
                ModLog.Feedback("[Binds] That button opens the menu — pick another.");
                return;
            }

            ApplyBindCode(code);
        }

        public static void OnGUI()
        {
            if (_listeningMenuOpen)
            {
                var mEv = Event.current;
                if (mEv != null && mEv.type == EventType.KeyDown && mEv.keyCode == KeyCode.Escape)
                {
                    mEv.Use(); _listeningMenuOpen = false; Rebuild();
                }
                return;
            }
            if (_listeningSlot < 0) return;
            var ev = Event.current;
            if (ev == null || ev.type != EventType.KeyDown) return;
            KeyCode k = ev.keyCode;
            if (k == KeyCode.None) return;

            if (k == KeyCode.Escape) { ev.Use(); CancelListen(); return; }
            if (k == KeyCode.F6)
            {
                ev.Use();
                ModLog.Debug("[BindsPage] F6 is reserved for menu open — choose a different key.");
                return;
            }
            ev.Use();
            ApplyBindCode((int)k);
        }

        private static void ApplyBindCode(int code)
        {
            int conflictSlot = KeyBindManager.FindConflict(code, _listeningSlot);
            if (conflictSlot >= 0)
            {
                _conflictSlot = _listeningSlot;
                _conflictWith = conflictSlot;
                _conflictCode = code;
                _conflictKey = code < 0 ? KeyCode.None : (KeyCode)code;
                _listeningSlot = -1;
                Rebuild();
                return;
            }

            KeyBindManager.SetCode(_listeningSlot, code);
            KeyBindManager.SaveBindings();
            KeyBindManager.SkipNextCheck();
            _listeningSlot = -1;
            Rebuild();
        }

        private static void CancelListen() { _listeningSlot = -1; ClearConflict(); Rebuild(); }
        private static void ClearConflict()
        {
            _conflictSlot = -1; _conflictWith = -1;
            _conflictKey = KeyCode.None; _conflictCode = 0;
        }
    }
}
