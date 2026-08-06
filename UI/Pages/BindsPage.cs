using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class BindsPage
    {
        private static int _listeningSlot = -1;
        private static int _conflictSlot  = -1;
        private static int _conflictWith  = -1;
        private static KeyCode _conflictKey = KeyCode.None;
        private static bool _listeningMenuOpen = false;

        public static bool IsListening { get { return _listeningSlot >= 0 || _conflictSlot >= 0 || _listeningMenuOpen; } }
        public static bool IsAnyActive { get { return false; } }

        private static Transform _contentRoot;
        private static ScrollRect _scrollRect;

        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
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
            catch (Exception ex) { MelonLogger.Error("[BindsPage] CreatePage: " + ex); }
            return pg;
        }

        public static void Rebuild()
        {
            if ((object)_contentRoot == null) return;
            try
            {
                // Preserve scroll position across state-change rebuilds
                float savedScroll = (object)_scrollRect != null ? _scrollRect.verticalNormalizedPosition : 1f;

                for (int i = _contentRoot.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(_contentRoot.GetChild(i).gameObject);

                UIHelpers.SectionHeader("MENU ACCESS", _contentRoot);
                BuildMenuOpenRow();
                UIHelpers.Divider(_contentRoot);

                UIHelpers.SectionHeader("KEY BINDINGS", _contentRoot);

                var hint = UIHelpers.StatRow("", _contentRoot);
                var hintTxt = UIHelpers.Txt("Hint", hint.transform,
                    "Click BIND then press any key.  ESC cancels.",
                    9, FontStyle.Normal, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                hintTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                UIHelpers.Divider(_contentRoot);

                int count = KeyBindManager.Count;
                for (int i = 0; i < count; i++)
                {
                    int capturedSlot = i;
                    KeyCode current = KeyBindManager.GetKeyCode(i);
                    bool isListening = (_listeningSlot == i);
                    bool isConflict  = (_conflictSlot  == i);

                    if (isListening)       BuildListeningRow(KeyBindManager.ModLabels[i], capturedSlot);
                    else if (isConflict)   BuildConflictRow(KeyBindManager.ModLabels[i], capturedSlot);
                    else                   BuildNormalRow(KeyBindManager.ModLabels[i], capturedSlot, current);
                }

                UIHelpers.AddScrollForwarders(_contentRoot);
                var crtRT = UIHelpers.RT(_contentRoot.gameObject);
                LayoutRebuilder.ForceRebuildLayoutImmediate(crtRT);
                Canvas.ForceUpdateCanvases();
                if ((object)_scrollRect != null) _scrollRect.verticalNormalizedPosition = savedScroll;
            }
            catch (Exception ex) { MelonLogger.Error("[BindsPage] Rebuild: " + ex.Message); }
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
                    _listeningMenuOpen = true;
                    Rebuild();
                }, 40);
            }
        }

        private static void BuildNormalRow(string label, int slot, KeyCode current)
        {
            var row = UIHelpers.StatRow(label, _contentRoot);
            bool hasBind = current != KeyCode.None;
            string keyName = hasBind ? current.ToString() : "\u2014";
            Color keyCol = hasBind ? UIHelpers.Accent : UIHelpers.TextDim;
            var keyTxt = UIHelpers.Txt("KN_" + slot, row.transform, keyName,
                11, hasBind ? FontStyle.Bold : FontStyle.Normal, TextAnchor.MiddleRight, keyCol);
            keyTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 96;

            UIHelpers.ActionBtn(row.transform, "BIND", () =>
            {
                _listeningMenuOpen = false;
                _listeningSlot = slot; _conflictSlot = -1; _conflictWith = -1; _conflictKey = KeyCode.None;
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
            var promptTxt = UIHelpers.Txt("LP_" + slot, row.transform, "Press any key\u2026",
                11, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
            promptTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 96;
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
                KeyBindManager.SetKey(_conflictSlot, _conflictKey);
                KeyBindManager.SaveBindings();
                KeyBindManager.SkipNextCheck();
                ClearConflict(); Rebuild();
            }, 48);
            UIHelpers.ActionBtnOrange(row.transform, "CANCEL", () => { ClearConflict(); Rebuild(); }, 52);
        }

        public static void CheckController()
        {
            if (!_listeningMenuOpen) return;
            int code;
            if (!KeyBindManager.AnyControllerPressed(out code)) return;

            KeyBindManager.SetMenuOpenCode(code);
            KeyBindManager.SaveBindings();
            KeyBindManager.SkipMenuOpenCheck();
            _listeningMenuOpen = false;
            Rebuild();
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
                MelonLogger.Msg("[BindsPage] F6 is reserved for menu open — choose a different key.");
                return;
            }
            ev.Use();

            int conflictSlot = KeyBindManager.FindConflict(k, _listeningSlot);
            if (conflictSlot >= 0)
            {
                _conflictSlot = _listeningSlot; _conflictWith = conflictSlot; _conflictKey = k;
                _listeningSlot = -1; Rebuild();
            }
            else
            {
                KeyBindManager.SetKey(_listeningSlot, k);
                KeyBindManager.SaveBindings();
                KeyBindManager.SkipNextCheck();
                _listeningSlot = -1; Rebuild();
            }
        }

        private static void CancelListen() { _listeningSlot = -1; ClearConflict(); Rebuild(); }
        private static void ClearConflict() { _conflictSlot = -1; _conflictWith = -1; _conflictKey = KeyCode.None; }
    }
}
