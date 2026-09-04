using MelonLoader;
using DescendersModMenu;
using DescendersModMenu.Mods;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class LocationsPage
    {
        private static Text _mapLabel;
        private static Text _saveStatusLabel;
        private static readonly Button[] _saveButtons = new Button[SavedLocations.SlotCount];
        private static Text[] _nameTexts = new Text[SavedLocations.SlotCount];
        private static Transform _allSpotsHost;
        private static readonly System.Collections.Generic.List<GameObject> _allSpotRows
            = new System.Collections.Generic.List<GameObject>();
        private static int _renamingSlot = -1;
        private static string _renameBuffer = "";
        private static string _allSearchBuffer = "";
        private static bool _allSearchFocused = false;
        private static Text _allSearchInputText;
        private static RectTransform _allSearchBoxRect;
        private static Transform _perkHost;
        private static ScrollRect _perkScroll;
        private static readonly System.Collections.Generic.Dictionary<string, Image> _perkTracks
            = new System.Collections.Generic.Dictionary<string, Image>();
        private static readonly System.Collections.Generic.Dictionary<string, RectTransform> _perkKnobs
            = new System.Collections.Generic.Dictionary<string, RectTransform>();

        public static bool IsAnyActive => false;
        public static bool IsRenaming => _renamingSlot >= 0;

        private static void BuildCrewPerkPanel(Transform parent)
        {
            LeftUnderlinedHeader(parent, "CrewPerkHdr", "CREW PERKS", SegmentTotalWidth,
                UIHelpers.Accent, true, 11, FontStyle.Bold);
            LeftInfoLine(parent, "CrewPerkInfo",
                "Toggle perks below. They apply when you use GO+ in the spot list.",
                SegmentTotalWidth);

            var panel = UIHelpers.Panel("CrewPerkPanel", parent, UIHelpers.RowBg, UIHelpers.RowSp);
            var ple = panel.AddComponent<LayoutElement>();
            ple.preferredWidth = SegmentTotalWidth;
            ple.minWidth = SegmentTotalWidth;
            ple.preferredHeight = 128f;
            ple.minHeight = 128f;
            ple.flexibleWidth = 0;

            var scrollObj = UIHelpers.Obj("CrewPerkScroll", panel.transform);
            UIHelpers.Fill(UIHelpers.RT(scrollObj));
            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;
            scroll.inertia = false;

            var vp = UIHelpers.Obj("CrewPerkVp", scrollObj.transform);
            UIHelpers.Fill(UIHelpers.RT(vp));
            vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            vp.AddComponent<Mask>().showMaskGraphic = true;
            scroll.viewport = UIHelpers.RT(vp);

            var content = UIHelpers.Obj("CrewPerkContent", vp.transform);
            var crt = UIHelpers.RT(content);
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.sizeDelta = new Vector2(0, 0);
            scroll.content = crt;
            UIHelpers.AddScrollbar(scroll);
            _perkScroll = scroll;
            content.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2((SegmentTotalWidth - 32f) / 2f, 22f);
            grid.spacing = new Vector2(4f, 2f);
            grid.padding = new RectOffset(6, 14, 4, 4);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperLeft;

            _perkHost = content.transform;
            _perkTracks.Clear();
            _perkKnobs.Clear();
            RebuildCrewPerkToggles();
        }

        private static void RebuildCrewPerkToggles()
        {
            if ((object)_perkHost == null) return;
            CrewPerkManager.RefreshCatalog();

            for (int i = _perkHost.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(_perkHost.GetChild(i).gameObject);
            _perkTracks.Clear();
            _perkKnobs.Clear();

            var catalog = CrewPerkManager.Catalog;
            if (catalog.Count == 0)
            {
                var empty = UIHelpers.Obj("CrewPerkEmpty", _perkHost);
                var ele = empty.AddComponent<LayoutElement>();
                ele.preferredWidth = SegmentTotalWidth - 16f;
                ele.preferredHeight = 20f;
                UIHelpers.Txt("CrewPerkEmptyT", empty.transform,
                    "Open freeride menu once to load perk list.", 9,
                    FontStyle.Italic, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                return;
            }

            for (int i = 0; i < catalog.Count; i++)
            {
                CrewPerkManager.PerkEntry entry = catalog[i];
                string id = entry.Id;
                var row = UIHelpers.Obj("Perk_" + i, _perkHost);
                var hlg = row.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 4;
                hlg.padding = new RectOffset(2, 2, 0, 0);
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childForceExpandWidth = false;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;

                Image track;
                RectTransform knob;
                UIHelpers.Toggle(row.transform, "Pt", () =>
                {
                    CrewPerkManager.Toggle(id);
                    RefreshCrewPerkToggleVisuals();
                }, out track, out knob);
                _perkTracks[id] = track;
                _perkKnobs[id] = knob;

                string label = entry.Label;
                if (label.Length > 28)
                    label = label.Substring(0, 27) + "\u2026";
                var lbl = UIHelpers.Txt("PerkLbl" + i, row.transform, label, 9,
                    FontStyle.Normal, TextAnchor.MiddleLeft, UIHelpers.TextLight);
                lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
                lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            }

            RefreshCrewPerkToggleVisuals();
            if ((object)_perkScroll != null && (object)_perkHost != null)
                UIHelpers.AddScrollForwarders(_perkHost);
        }

        private static void RefreshCrewPerkToggleVisuals()
        {
            foreach (var kv in _perkTracks)
            {
                RectTransform knob;
                if (!_perkKnobs.TryGetValue(kv.Key, out knob)) continue;
                UIHelpers.SetToggle(kv.Value, knob, CrewPerkManager.IsSelected(kv.Key));
            }
        }

        private const float AllListGoWidth = 40f;
        private const float AllListGoPerkWidth = 42f;
        private const float AllListRemoveWidth = 44f;
        private const float ColSplitGap = 8f;
        private const float ColDividerWidth = 1f;
        private const float SegmentColWidth = 340f;
        private const float SegmentTotalWidth = SegmentColWidth * 2f + ColSplitGap + ColDividerWidth;

        private static void BuildCurrentSpotsHeader(Transform parent, float width, out Text mapLabel)
        {
            var wrap = UIHelpers.Obj("SpotsHdrWrap", parent);
            var wle = wrap.AddComponent<LayoutElement>();
            wle.preferredWidth = width;
            wle.minWidth = width;
            wle.flexibleWidth = 0;
            wle.flexibleHeight = 0;

            wrap.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            var vlg = wrap.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 0, 0);
            vlg.spacing = 2;
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            string mapLine = SavedLocations.GetMapTitleLine();
            mapLabel = UIHelpers.Txt("SpotsHdrT", wrap.transform,
                "Current spots: " + mapLine, 11, FontStyle.Bold,
                TextAnchor.MiddleLeft, UIHelpers.Accent);
            mapLabel.supportRichText = false;
            mapLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            mapLabel.verticalOverflow = VerticalWrapMode.Truncate;
            var tle = mapLabel.gameObject.AddComponent<LayoutElement>();
            tle.flexibleWidth = 1;
            tle.minHeight = 16;
            mapLabel.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var ul = UIHelpers.Panel("SpotsHdrUl", wrap.transform, UIHelpers.Accent, UIHelpers.RowSp);
            ul.GetComponent<Image>().raycastTarget = false;
            var ule = ul.AddComponent<LayoutElement>();
            ule.preferredHeight = 1;
            ule.minHeight = 1;
            ule.flexibleWidth = 1;
        }

        private static void CenterUnderlinedHeader(Transform parent, string objName, string title,
            float width, Color color, bool uppercase, int fontSize, FontStyle style)
        {
            var wrap = UIHelpers.Obj(objName + "Wrap", parent);
            var wle = wrap.AddComponent<LayoutElement>();
            wle.preferredWidth = width;
            wle.minWidth = width;
            wle.flexibleWidth = 0;
            wle.preferredHeight = uppercase ? 20f : 24f;
            wle.minHeight = uppercase ? 20f : 24f;
            wle.flexibleHeight = 0;

            var vlg = wrap.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 0, 0);
            vlg.spacing = 2;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            string shown = title ?? "";
            if (uppercase)
                shown = shown.ToUpper();
            var t = UIHelpers.Txt(objName + "T", wrap.transform, shown, fontSize, style,
                TextAnchor.MiddleCenter, color);
            t.supportRichText = false;
            t.alignment = TextAnchor.MiddleCenter;
            var tle = t.gameObject.AddComponent<LayoutElement>();
            tle.preferredHeight = fontSize + 4;
            tle.flexibleWidth = 1;

            var ul = UIHelpers.Panel(objName + "Ul", wrap.transform, color, UIHelpers.RowSp);
            ul.GetComponent<Image>().raycastTarget = false;
            var ule = ul.AddComponent<LayoutElement>();
            ule.preferredHeight = 1;
            ule.minHeight = 1;
            ule.flexibleWidth = 1;
        }

        private static void LeftUnderlinedHeader(Transform parent, string objName, string title,
            float width, Color color, bool uppercase, int fontSize, FontStyle style)
        {
            var wrap = UIHelpers.Obj(objName + "Wrap", parent);
            var wle = wrap.AddComponent<LayoutElement>();
            wle.preferredWidth = width;
            wle.minWidth = width;
            wle.flexibleWidth = 0;
            wle.preferredHeight = uppercase ? 20f : 24f;
            wle.minHeight = uppercase ? 20f : 24f;
            wle.flexibleHeight = 0;

            var vlg = wrap.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 0, 0);
            vlg.spacing = 2;
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            string shown = title ?? "";
            if (uppercase)
                shown = shown.ToUpper();
            var t = UIHelpers.Txt(objName + "T", wrap.transform, shown, fontSize, style,
                TextAnchor.MiddleLeft, color);
            t.supportRichText = false;
            var tle = t.gameObject.AddComponent<LayoutElement>();
            tle.preferredHeight = fontSize + 4;
            tle.flexibleWidth = 1;

            var ul = UIHelpers.Panel(objName + "Ul", wrap.transform, color, UIHelpers.RowSp);
            ul.GetComponent<Image>().raycastTarget = false;
            var ule = ul.AddComponent<LayoutElement>();
            ule.preferredHeight = 1;
            ule.minHeight = 1;
            ule.flexibleWidth = 1;
        }

        private static void LeftInfoLine(Transform parent, string objName, string txt, float width)
        {
            var box = UIHelpers.Obj(objName + "Wrap", parent);
            var ble = box.AddComponent<LayoutElement>();
            ble.preferredWidth = width;
            ble.minWidth = width;
            ble.flexibleWidth = 0;
            ble.flexibleHeight = 0;
            box.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            var vlg = box.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.padding = new RectOffset(6, 6, 2, 2);

            var t = UIHelpers.Txt(objName + "T", box.transform, txt ?? "", 10,
                FontStyle.Italic, TextAnchor.UpperLeft, Color.white);
            t.supportRichText = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = 16;
            t.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void CenteredInfoBox(Transform parent, string txt, float width)
        {
            var box = UIHelpers.Obj("InfWrap", parent);
            var ble = box.AddComponent<LayoutElement>();
            ble.preferredWidth = width;
            ble.minWidth = width;
            ble.flexibleWidth = 0;
            ble.flexibleHeight = 0;

            box.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            var vlg = box.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            var t = UIHelpers.Txt("Inf", box.transform, txt ?? "", 10,
                FontStyle.Italic, TextAnchor.MiddleCenter, Color.white);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = 16;
            t.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void CenteredTextHost(Transform parent, string objName, float width,
            out GameObject host, out Text label, int fontSize, FontStyle style, Color color)
        {
            host = UIHelpers.Obj(objName, parent);
            var hostLe = host.AddComponent<LayoutElement>();
            hostLe.preferredWidth = width;
            hostLe.minWidth = width;
            hostLe.flexibleWidth = 0;
            hostLe.minHeight = 18;

            host.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            var vlg = host.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            label = UIHelpers.Txt(objName + "T", host.transform, "—", fontSize, style,
                TextAnchor.MiddleCenter, color);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            var lblLe = label.gameObject.AddComponent<LayoutElement>();
            lblLe.flexibleWidth = 1;
            lblLe.minHeight = 18;
            label.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
        }

        private static string CompactRowLabel(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            const int max = 26;
            if (text.Length <= max) return text;
            return text.Substring(0, max - 1) + "\u2026";
        }

        private static GameObject CompactCenteredRow(Transform parent, string suffix, float width, float height)
        {
            var row = UIHelpers.Panel("Ctr" + suffix, parent, UIHelpers.RowBg, UIHelpers.RowSp);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleHeight = 0;
            le.flexibleWidth = 0;
            le.preferredWidth = width;
            le.minWidth = width;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5;
            hlg.padding = new RectOffset(5, 5, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            var bd = UIHelpers.Panel("Bd", row.transform, UIHelpers.RowBorder, UIHelpers.RowSp);
            bd.GetComponent<Image>().raycastTarget = false;
            UIHelpers.Fill(UIHelpers.RT(bd));
            bd.AddComponent<LayoutElement>().ignoreLayout = true;

            return row;
        }

        private static GameObject AllSpotsCompactRow(Transform parent, string suffix, float width)
        {
            return CompactCenteredRow(parent, suffix, width, 28f);
        }

        private static Text AddCenteredListLabel(Transform row, string objName, string text,
            int fontSize, FontStyle style, Color color)
        {
            var nmBtn = UIHelpers.Obj(objName, row);
            var nmLe = nmBtn.AddComponent<LayoutElement>();
            nmLe.flexibleWidth = 1;
            nmLe.preferredHeight = 26;
            nmLe.minHeight = 26;
            Text txt = UIHelpers.Txt(objName + "T", nmBtn.transform, text ?? "",
                fontSize, style, TextAnchor.MiddleCenter, color);
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            UIHelpers.Fill(UIHelpers.RT(txt.gameObject));
            return txt;
        }

        private static void WideDivider(Transform parent)
        {
            var dv = UIHelpers.Panel("AllDv", parent, UIHelpers.RowBorder, UIHelpers.RowSp);
            var le = dv.AddComponent<LayoutElement>();
            le.preferredWidth = SegmentColWidth;
            le.minWidth = SegmentColWidth;
            le.flexibleWidth = 0;
            le.preferredHeight = 1;
            le.minHeight = 1;
        }

        private static Transform CreateSegmentColumn(Transform parent, string suffix, float width,
            float spacing)
        {
            var col = UIHelpers.Obj("Seg" + suffix, parent);
            var le = col.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;
            le.flexibleWidth = 0;
            col.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            var vlg = col.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            return col.transform;
        }

        private static void AddColumnDivider(Transform parent)
        {
            var dv = UIHelpers.Panel("ColDv", parent, UIHelpers.RowBorder, UIHelpers.RowSp);
            var le = dv.AddComponent<LayoutElement>();
            le.preferredWidth = ColDividerWidth;
            le.minWidth = ColDividerWidth;
            le.flexibleWidth = 0;
            le.flexibleHeight = 0;
            le.preferredHeight = 1;
            le.minHeight = 1;
        }

        private static void BuildAllSavedSearchRow(Transform parent)
        {
            var searchRow = UIHelpers.Panel("AllSrRow", parent, UIHelpers.RowBg, UIHelpers.RowSp);
            var srLe = searchRow.AddComponent<LayoutElement>();
            srLe.preferredWidth = SegmentColWidth;
            srLe.minWidth = SegmentColWidth;
            srLe.flexibleWidth = 0;
            srLe.preferredHeight = 34;
            srLe.minHeight = 34;

            var bd = UIHelpers.Panel("Bd", searchRow.transform, UIHelpers.RowBorder, UIHelpers.RowSp);
            bd.GetComponent<Image>().raycastTarget = false;
            UIHelpers.Fill(UIHelpers.RT(bd));
            bd.AddComponent<LayoutElement>().ignoreLayout = true;

            var sHlg = searchRow.AddComponent<HorizontalLayoutGroup>();
            sHlg.padding = new RectOffset(6, 6, 4, 4);
            sHlg.spacing = 6;
            sHlg.childAlignment = TextAnchor.MiddleLeft;
            sHlg.childForceExpandWidth = false;
            sHlg.childForceExpandHeight = true;
            sHlg.childControlWidth = true;
            sHlg.childControlHeight = true;

            var searchBg = UIHelpers.Obj("AllSrBg", searchRow.transform);
            searchBg.AddComponent<Image>().color = UIHelpers.WinOuter;
            var sbgLe = searchBg.AddComponent<LayoutElement>();
            sbgLe.flexibleWidth = 1;
            sbgLe.minHeight = 26;
            sbgLe.preferredHeight = 26;
            var sbgHlg = searchBg.AddComponent<HorizontalLayoutGroup>();
            sbgHlg.padding = new RectOffset(8, 8, 0, 0);
            sbgHlg.childAlignment = TextAnchor.MiddleLeft;
            sbgHlg.childForceExpandWidth = true;
            sbgHlg.childForceExpandHeight = true;

            string placeholder = "Search bike parks & maps...";
            string shown = string.IsNullOrEmpty(_allSearchBuffer)
                ? (_allSearchFocused ? UIHelpers.WithCaret(placeholder, true) : placeholder)
                : (_allSearchFocused ? UIHelpers.WithCaret(_allSearchBuffer, true) : _allSearchBuffer);
            _allSearchInputText = UIHelpers.Txt("AllSrIT", searchBg.transform, shown, 11,
                FontStyle.Normal, TextAnchor.MiddleLeft,
                string.IsNullOrEmpty(_allSearchBuffer) ? Color.white : UIHelpers.TextLight);
            _allSearchInputText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _allSearchInputText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            _allSearchBoxRect = UIHelpers.RT(searchBg);
            var focusBtn = searchBg.AddComponent<Button>();
            focusBtn.targetGraphic = searchBg.GetComponent<Image>();
            focusBtn.onClick.AddListener(() => { _allSearchFocused = true; });

            if (!string.IsNullOrEmpty(_allSearchBuffer))
            {
                UIHelpers.SmallBtn(searchRow.transform, "\u2716", () =>
                {
                    _allSearchBuffer = "";
                    _allSearchFocused = false;
                    RebuildAllSpotsList();
                    RefreshAllSearchInputText();
                });
            }
        }

        private static void RefreshAllSearchInputText()
        {
            if (!_allSearchInputText) return;
            string placeholder = "Search bike parks & maps...";
            if (_allSearchFocused)
            {
                if (_allSearchBuffer.Length > 0)
                {
                    _allSearchInputText.text = UIHelpers.WithCaret(_allSearchBuffer, true);
                    _allSearchInputText.color = UIHelpers.TextLight;
                }
                else
                {
                    _allSearchInputText.text = UIHelpers.WithCaret(placeholder, true);
                    _allSearchInputText.color = Color.white;
                }
            }
            else if (string.IsNullOrEmpty(_allSearchBuffer))
            {
                _allSearchInputText.text = placeholder;
                _allSearchInputText.color = Color.white;
            }
            else
            {
                _allSearchInputText.text = _allSearchBuffer;
                _allSearchInputText.color = UIHelpers.TextLight;
            }
        }

        private static bool GroupMatchesSearch(string mapTitle, string mapKey,
            System.Collections.Generic.List<SavedLocations.SavedSpotRef> group,
            string query, bool sectionGroup)
        {
            if (string.IsNullOrEmpty(query)) return true;
            string q = query.ToLowerInvariant();
            if ((mapTitle ?? "").ToLowerInvariant().Contains(q)) return true;
            if ((mapKey ?? "").ToLowerInvariant().Contains(q)) return true;
            for (int i = 0; i < group.Count; i++)
            {
                SavedLocations.SavedSpotRef spot = group[i];
                string label = sectionGroup
                    ? SavedLocations.GetFreerideListRowLabel(spot.MapLabel, spot.SpotName, spot.Slot)
                    : spot.SpotName;
                if ((label ?? "").ToLowerInvariant().Contains(q)) return true;
            }
            return false;
        }

        private static bool SpotMatchesSearch(SavedLocations.SavedSpotRef spot, string rowLabel, string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            string q = query.ToLowerInvariant();
            return (rowLabel ?? "").ToLowerInvariant().Contains(q)
                || (spot.SpotName ?? "").ToLowerInvariant().Contains(q)
                || (spot.MapLabel ?? "").ToLowerInvariant().Contains(q);
        }

        private static void TickAllSearch()
        {
            if (!_allSearchInputText) return;

            if (_allSearchFocused && Input.GetMouseButtonDown(0))
            {
                if ((object)_allSearchBoxRect != null
                    && !RectTransformUtility.RectangleContainsScreenPoint(
                        _allSearchBoxRect, Input.mousePosition, null))
                    _allSearchFocused = false;
            }

            if (!_allSearchFocused)
            {
                RefreshAllSearchInputText();
                return;
            }

            bool changed = false;
            foreach (char ch in Input.inputString)
            {
                if (ch == '\b')
                {
                    if (_allSearchBuffer.Length > 0)
                    {
                        _allSearchBuffer = _allSearchBuffer.Substring(0, _allSearchBuffer.Length - 1);
                        changed = true;
                    }
                }
                else if (ch == '\n' || ch == '\r' || ch == '\x1b')
                    _allSearchFocused = false;
                else if (_allSearchBuffer.Length < 40)
                {
                    _allSearchBuffer += ch;
                    changed = true;
                }
            }

            if (MenuInputGuard.GetKeyDown(KeyCode.Escape))
                _allSearchFocused = false;

            RefreshAllSearchInputText();
            if (changed) RebuildAllSpotsList();
        }

        private static void AddGoButtonSpacer(Transform row)
        {
            var spacer = UIHelpers.Obj("GoSp", row.transform);
            var le = spacer.AddComponent<LayoutElement>();
            float w = AllListGoWidth + AllListGoPerkWidth;
            le.preferredWidth = w;
            le.minWidth = w;
            le.flexibleWidth = 0;
        }

        private static Transform CreateAllSpotsHost(Transform parent)
        {
            var host = UIHelpers.Obj("AllSpotsHost", parent);
            var le = host.AddComponent<LayoutElement>();
            le.preferredWidth = SegmentColWidth;
            le.minWidth = SegmentColWidth;
            le.flexibleWidth = 0;
            le.flexibleHeight = 0;
            le.minHeight = 24;
            host.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            var vlg = host.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = UIHelpers.RowGap;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            return host.transform;
        }

        private static void AddAllListSpotRow(Transform parent, string suffix, int rowIdx,
            string rowLabel, bool mapCanGo, string spotMapKey, int slot)
        {
            var row = AllSpotsCompactRow(parent, suffix, SegmentColWidth);
            _allSpotRows.Add(row);

            AddCenteredListLabel(row.transform, "AllSpotLbl" + rowIdx,
                CompactRowLabel(rowLabel), 10, FontStyle.Bold, Color.white);

            if (mapCanGo)
            {
                UIHelpers.ActionBtnGreen(row.transform, "GO",
                    () => SavedLocations.GoToSavedSpot(spotMapKey, slot), (int)AllListGoWidth);
                UIHelpers.ActionBtnGreen(row.transform, "GO+",
                    () => SavedLocations.GoToSavedSpot(spotMapKey, slot, true),
                    (int)AllListGoPerkWidth);
            }
            else
            {
                AddGoButtonSpacer(row.transform);
            }

            UIHelpers.ActionBtnRed(row.transform, "DEL",
                () =>
                {
                    SavedLocations.DeleteSavedSpot(spotMapKey, slot);
                    RebuildAllSpotsList();
                    RefreshRows();
                },
                (int)AllListRemoveWidth);
        }

        private static void AddMapGroupHeader(Transform parent, string objName, string title,
            string availabilityHint, bool firstInList)
        {
            if (!firstInList)
                WideDivider(parent);

            Color titleColor = string.IsNullOrEmpty(availabilityHint)
                ? UIHelpers.Accent : UIHelpers.TextDim;
            LeftUnderlinedHeader(parent, objName, title, SegmentColWidth,
                titleColor, false, 10, FontStyle.Bold);

            if (!string.IsNullOrEmpty(availabilityHint))
                LeftInfoLine(parent, objName + "Hint", availabilityHint, SegmentColWidth);
        }

        private static void ClearAllSpotsHost(Transform host)
        {
            if ((object)host == null) return;
            for (int i = host.childCount - 1; i >= 0; i--)
            {
                GameObject child = host.GetChild(i).gameObject;
                if (child)
                    Object.DestroyImmediate(child);
            }
        }

        private static void ClearAllSpotsList()
        {
            ClearAllSpotsHost(_allSpotsHost);
            _allSpotRows.Clear();
        }

        public static void CreatePage(Transform parent)
        {
            try
            {
                var root = UIHelpers.Obj("P27R", parent);
                UIHelpers.Fill(UIHelpers.RT(root));

                var scrollObj = UIHelpers.Obj("Scroll", root.transform);
                UIHelpers.Fill(UIHelpers.RT(scrollObj));
                var scrollRect = scrollObj.AddComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 25f;
                scrollRect.inertia = false;

                var vp = UIHelpers.Obj("VP", scrollObj.transform);
                UIHelpers.Fill(UIHelpers.RT(vp));
                vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                vp.AddComponent<Mask>().showMaskGraphic = true;
                scrollRect.viewport = UIHelpers.RT(vp);

                var pg = UIHelpers.Obj("Content", vp.transform);
                var crt = UIHelpers.RT(pg);
                crt.anchorMin = new Vector2(0, 1);
                crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1);
                crt.sizeDelta = new Vector2(0, 0);
                scrollRect.content = crt;
                UIHelpers.AddScrollbar(scrollRect);
                pg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var vlg = pg.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = false;
                vlg.childForceExpandHeight = false;

                Transform c = pg.transform;

                CenterUnderlinedHeader(c, "SavedLocHdr", "Spot Book",
                    SegmentTotalWidth, UIHelpers.Accent, false, 12, FontStyle.Bold);
                Transform savedLocHdrWrap = c.Find("SavedLocHdrWrap");
                if ((object)savedLocHdrWrap != null)
                    FavouritesManager.RegisterStarButton(
                        "SavedLocations",
                        UIHelpers.StarBtnAbs(savedLocHdrWrap, "SavedLocations",
                            () => FavouritesManager.Toggle("SavedLocations")));

                BuildCrewPerkPanel(c);

                var splitHost = UIHelpers.Obj("LocSplit", c);
                var splitLe = splitHost.AddComponent<LayoutElement>();
                splitLe.preferredWidth = SegmentTotalWidth;
                splitLe.minWidth = SegmentTotalWidth;
                splitLe.flexibleWidth = 0;
                splitHost.AddComponent<ContentSizeFitter>().verticalFit =
                    ContentSizeFitter.FitMode.PreferredSize;
                var splitHlg = splitHost.AddComponent<HorizontalLayoutGroup>();
                splitHlg.spacing = ColSplitGap;
                splitHlg.childAlignment = TextAnchor.UpperCenter;
                splitHlg.childForceExpandWidth = false;
                splitHlg.childForceExpandHeight = false;
                splitHlg.childControlWidth = true;
                splitHlg.childControlHeight = true;

                Transform spotsSeg = CreateSegmentColumn(splitHost.transform, "Spots",
                    SegmentColWidth, 2f);
                AddColumnDivider(splitHost.transform);
                Transform allSeg = CreateSegmentColumn(splitHost.transform, "All",
                    SegmentColWidth, UIHelpers.RowGap);

                BuildCurrentSpotsHeader(spotsSeg, SegmentColWidth, out _mapLabel);

                GameObject saveStsHost;
                CenteredTextHost(spotsSeg, "SaveStsHost", SegmentColWidth, out saveStsHost,
                    out _saveStatusLabel, 10, FontStyle.Italic, UIHelpers.TextDim);
                saveStsHost.gameObject.SetActive(false);

                LeftInfoLine(spotsSeg, "SpotsInfo",
                    "Click a name to rename. SAVE stores your current position.",
                    SegmentColWidth);

                var spotsHost = UIHelpers.Obj("SpotsHost", spotsSeg);
                var spotsHostLe = spotsHost.AddComponent<LayoutElement>();
                spotsHostLe.preferredWidth = SegmentColWidth;
                spotsHostLe.minWidth = SegmentColWidth;
                spotsHostLe.flexibleWidth = 0;
                spotsHostLe.minHeight = 24;
                spotsHost.AddComponent<ContentSizeFitter>().verticalFit =
                    ContentSizeFitter.FitMode.PreferredSize;
                var spotsVlg = spotsHost.AddComponent<VerticalLayoutGroup>();
                spotsVlg.spacing = UIHelpers.RowGap;
                spotsVlg.childAlignment = TextAnchor.UpperCenter;
                spotsVlg.childForceExpandWidth = false;
                spotsVlg.childForceExpandHeight = false;

                for (int li = 0; li < SavedLocations.SlotCount; li++)
                {
                    int idx = li;
                    var locRow = CompactCenteredRow(spotsHost.transform, "Loc" + li, SegmentColWidth, 30f);

                    var nmBtn = UIHelpers.Obj("LocNm" + li, locRow.transform);
                    var nmLe = nmBtn.AddComponent<LayoutElement>();
                    nmLe.flexibleWidth = 1;
                    nmLe.preferredHeight = 26;
                    _nameTexts[li] = UIHelpers.Txt("LocNt" + li, nmBtn.transform,
                        SavedLocations.GetName(li), 10, FontStyle.Bold,
                        TextAnchor.MiddleCenter, Color.white);
                    UIHelpers.Fill(UIHelpers.RT(_nameTexts[li].gameObject));
                    var nmClick = nmBtn.AddComponent<Button>();
                    nmClick.targetGraphic = nmBtn.AddComponent<Image>();
                    nmClick.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                    nmClick.onClick.AddListener(() => StartRename(idx));

                    UIHelpers.ActionBtn(locRow.transform, "SAVE",
                        () =>
                        {
                            CommitRenameIfActive(idx);
                            SavedLocations.Save(idx);
                            RefreshAll();
                        }, 44);
                    Transform saveBtnT = locRow.transform.Find("SAVEB");
                    if ((object)saveBtnT != null)
                        _saveButtons[li] = saveBtnT.GetComponent<Button>();
                    UIHelpers.ActionBtnGreen(locRow.transform, "GO",
                        () => SavedLocations.Teleport(idx), 40);
                    UIHelpers.ActionBtnRed(locRow.transform, "DEL",
                        () => { SavedLocations.Delete(idx); RefreshAll(); }, 40);
                }

                LeftUnderlinedHeader(allSeg, "AllHdr", "ALL SPOTS", SegmentColWidth,
                    UIHelpers.Accent, true, 11, FontStyle.Bold);
                LeftInfoLine(allSeg, "AllInfo",
                    "Grouped by map. GO loads the map. GO+ loads with checked crew perks.",
                    SegmentColWidth);

                BuildAllSavedSearchRow(allSeg);
                _allSpotsHost = CreateAllSpotsHost(allSeg);

                Text favMapLbl = null;
                Text[] favNameTexts = null;

                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "SavedLocations",
                    DisplayName = "Spot Book",
                    TabBadge = "SPOTS",
                    BuildControls = (p) =>
                    {
                        BuildSavedLocationsFavourites(p, out favMapLbl, out favNameTexts);

                        FavouritesManager.RegisterRefresh("SavedLocations", () =>
                        {
                            if (favMapLbl)
                            {
                                favMapLbl.text = CurrentSpotsTitleLine();
                                favMapLbl.color = SavedLocations.CanSaveOnCurrentMap
                                    ? UIHelpers.Accent : UIHelpers.TextDim;
                            }
                            if (favNameTexts == null) return;
                            for (int i = 0; i < favNameTexts.Length; i++)
                            {
                                if (!favNameTexts[i]) continue;
                                favNameTexts[i].text = SavedLocations.GetName(i);
                                favNameTexts[i].color = Color.white;
                            }
                        });
                    },
                    IsActive = () => SavedLocations.IsAnyActive
                });

                UIHelpers.AddScrollForwarders(c);
                RefreshAll();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("LocationsPage: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "LocationsPage");
            }
        }

        private static int AllSavedGroupSortRank(string groupKey)
        {
            if (groupKey == SavedLocations.ModIoSectionKey) return 1;
            if (groupKey == SavedLocations.FreerideSectionKey) return 2;
            return 0;
        }

        private static string DefaultSpotName(int slot)
        {
            return "Spot " + (slot + 1);
        }

        private static string RenameStartText(int slot)
        {
            if (SavedLocations.HasSlot(slot))
                return SavedLocations.GetName(slot);
            string label = SavedLocations.GetName(slot);
            if (label != "Empty")
                return label;
            return DefaultSpotName(slot);
        }

        private static void CommitSpotName(int slot, string name)
        {
            if (string.IsNullOrEmpty(name))
                name = DefaultSpotName(slot);
            SavedLocations.SetName(slot, name);
            RebuildAllSpotsList();
        }

        private static void CommitRenameIfActive(int slot)
        {
            if (_renamingSlot != slot) return;
            CommitSpotName(slot, _renameBuffer);
            _renamingSlot = -1;
            _renameBuffer = "";
        }

        private static void StartRename(int slot)
        {
            _allSearchFocused = false;
            if (_renamingSlot >= 0 && _renamingSlot != slot && _nameTexts[_renamingSlot])
                _nameTexts[_renamingSlot].color = Color.white;
            _renamingSlot = slot;
            _renameBuffer = RenameStartText(slot);
            if (_nameTexts[slot])
                _nameTexts[slot].color = Color.white;
        }

        public static void CancelRename()
        {
            if (_renamingSlot >= 0 && _nameTexts[_renamingSlot])
                _nameTexts[_renamingSlot].color = Color.white;
            _renamingSlot = -1;
            _renameBuffer = "";
            RefreshRows();
        }

        private static void TickRename()
        {
            foreach (char ch in Input.inputString)
            {
                if (ch == '\b')
                {
                    if (_renameBuffer.Length > 0)
                        _renameBuffer = _renameBuffer.Substring(0, _renameBuffer.Length - 1);
                }
                else if (ch == '\n' || ch == '\r')
                {
                    int slot = _renamingSlot;
                    CommitSpotName(slot, _renameBuffer);
                    _renamingSlot = -1;
                    _renameBuffer = "";
                    RefreshAll();
                    return;
                }
                else if (ch == '\x1b')
                {
                    _renamingSlot = -1;
                    _renameBuffer = "";
                    RefreshRows();
                    return;
                }
                else if (_renameBuffer.Length < 32)
                    _renameBuffer += ch;
            }

            if (MenuInputGuard.GetKeyDown(KeyCode.Escape))
            {
                CancelRename();
                return;
            }

            int ri = _renamingSlot;
            if (ri >= 0 && ri < _nameTexts.Length && _nameTexts[ri])
                _nameTexts[ri].text = UIHelpers.WithCaret(_renameBuffer, true);
        }

        private static void RefreshRows()
        {
            SavedLocations.MapSaveStatus saveSt = SavedLocations.GetCurrentMapSaveStatus();

            if (_mapLabel)
            {
                _mapLabel.text = "Current spots: " + SavedLocations.GetMapTitleLine();
                _mapLabel.color = saveSt.CanSave ? UIHelpers.Accent : UIHelpers.TextDim;
            }

            if (_saveStatusLabel)
            {
                bool show = !saveSt.CanSave && !string.IsNullOrEmpty(saveSt.Message);
                Transform saveStsHost = _saveStatusLabel.transform.parent;
                if (saveStsHost)
                    saveStsHost.gameObject.SetActive(show);
                if (show)
                    _saveStatusLabel.text = saveSt.Message;
            }

            for (int i = 0; i < SavedLocations.SlotCount; i++)
            {
                if (_saveButtons[i])
                {
                    _saveButtons[i].interactable = saveSt.CanSave;
                    var img = _saveButtons[i].GetComponent<Image>();
                    if ((object)img != null)
                        img.color = saveSt.CanSave
                            ? UIHelpers.ActionBtnBg
                            : new Color(UIHelpers.ActionBtnBg.r, UIHelpers.ActionBtnBg.g,
                                UIHelpers.ActionBtnBg.b, 0.35f);
                }
            }

            for (int i = 0; i < SavedLocations.SlotCount; i++)
            {
                if (_renamingSlot == i) continue;
                if (_nameTexts[i])
                {
                    _nameTexts[i].text = SavedLocations.GetName(i);
                    _nameTexts[i].color = Color.white;
                }
            }
        }

        private static string CurrentSpotsTitleLine()
        {
            return "Current spots: " + SavedLocations.GetMapTitleLine();
        }

        private static void BuildSavedLocationsFavourites(Transform p,
            out Text mapLabel, out Text[] nameTexts)
        {
            nameTexts = new Text[SavedLocations.SlotCount];

            var hdrRow = FavsPage.CompactStatRow("", p);
            mapLabel = UIHelpers.Txt("FavLocMap", hdrRow.transform, CurrentSpotsTitleLine(),
                11, FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.Accent);
            var favMapLblLe = mapLabel.gameObject.AddComponent<LayoutElement>();
            favMapLblLe.flexibleWidth = 1;
            favMapLblLe.preferredHeight = FavsPage.CompactRowH;

            const int colCount = 2;
            int perCol = (SavedLocations.SlotCount + colCount - 1) / colCount;

            var split = UIHelpers.Obj("FavLocSplit", p);
            var splitLe = split.AddComponent<LayoutElement>();
            splitLe.flexibleWidth = 1;
            split.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            var splitHlg = split.AddComponent<HorizontalLayoutGroup>();
            splitHlg.spacing = 8;
            splitHlg.childAlignment = TextAnchor.UpperCenter;
            splitHlg.childForceExpandWidth = true;
            splitHlg.childForceExpandHeight = false;
            splitHlg.childControlWidth = true;
            splitHlg.childControlHeight = true;

            for (int col = 0; col < colCount; col++)
            {
                var colPane = UIHelpers.Obj("FavCol" + col, split.transform);
                var cle = colPane.AddComponent<LayoutElement>();
                cle.flexibleWidth = 1;
                cle.minWidth = 0;
                colPane.AddComponent<ContentSizeFitter>().verticalFit =
                    ContentSizeFitter.FitMode.PreferredSize;
                var cvlg = colPane.AddComponent<VerticalLayoutGroup>();
                cvlg.spacing = 2f;
                cvlg.childAlignment = TextAnchor.UpperCenter;
                cvlg.childForceExpandWidth = true;
                cvlg.childForceExpandHeight = false;
                cvlg.childControlWidth = true;
                cvlg.childControlHeight = true;

                int start = col * perCol;
                int end = start + perCol;
                if (end > SavedLocations.SlotCount)
                    end = SavedLocations.SlotCount;
                for (int fi = start; fi < end; fi++)
                {
                    int slot = fi;
                    var row = FavsPage.CompactStatRow("", colPane.transform);
                    nameTexts[fi] = UIHelpers.Txt("FavLocN" + fi, row.transform,
                        SavedLocations.GetName(fi), 10, FontStyle.Bold,
                        TextAnchor.MiddleLeft, Color.white);
                    var nle = nameTexts[fi].gameObject.AddComponent<LayoutElement>();
                    nle.flexibleWidth = 1;
                    nle.preferredHeight = FavsPage.CompactRowH;
                    UIHelpers.ActionBtnGreen(row.transform, "GO",
                        () => SavedLocations.Teleport(slot), 36);
                    UIHelpers.ActionBtnRed(row.transform, "DEL",
                        () => { SavedLocations.Delete(slot); RefreshAll(); }, 36);
                }
            }
        }

        public static void RefreshAll()
        {
            SavedLocations.RefreshCurrentMap();
            RefreshRows();
            RebuildAllSpotsList();
            RefreshCrewPerkToggleVisuals();
            try { FavouritesManager.InvokeRefresh(); } catch { }
        }

        private static void RebuildAllSpotsList()
        {
            if ((object)_allSpotsHost == null) return;

            ClearAllSpotsList();

            SavedLocations.SavedSpotRef[] spots = SavedLocations.GetAllSavedSpots();
            if (spots.Length == 0)
            {
                var emptyRow = AllSpotsCompactRow(_allSpotsHost, "Empty", SegmentColWidth);
                _allSpotRows.Add(emptyRow);
                AddCenteredListLabel(emptyRow.transform, "NoAllSpots",
                    "No saved spots yet — use SAVE above.", 10,
                    FontStyle.Italic, UIHelpers.TextDim);
                return;
            }

            var groupOrder = new System.Collections.Generic.List<string>();
            var groups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SavedLocations.SavedSpotRef>>();
            for (int i = 0; i < spots.Length; i++)
            {
                SavedLocations.SavedSpotRef spot = spots[i];
                string groupKey = SavedLocations.GetCanonicalGroupKey(spot.MapKey ?? "");
                System.Collections.Generic.List<SavedLocations.SavedSpotRef> list;
                if (!groups.TryGetValue(groupKey, out list))
                {
                    list = new System.Collections.Generic.List<SavedLocations.SavedSpotRef>();
                    groups[groupKey] = list;
                    groupOrder.Add(groupKey);
                }
                list.Add(spot);
            }

            groupOrder.Sort((a, b) =>
            {
                int ra = AllSavedGroupSortRank(a);
                int rb = AllSavedGroupSortRank(b);
                if (ra != rb) return ra.CompareTo(rb);
                return string.Compare(a, b, System.StringComparison.OrdinalIgnoreCase);
            });

            string searchQuery = (_allSearchBuffer ?? "").Trim();
            bool firstShown = true;
            int shownGroups = 0;

            for (int g = 0; g < groupOrder.Count; g++)
            {
                string mapKey = groupOrder[g];
                System.Collections.Generic.List<SavedLocations.SavedSpotRef> group = groups[mapKey];
                if (group.Count == 0) continue;

                string mapTitle = SavedLocations.GetMapLabelForKey(mapKey);
                string availHint = SavedLocations.GetMapAvailabilityLine(mapKey);
                bool mapCanGo = SavedLocations.GetMapGoStatus(mapKey).CanGo;
                bool freerideGroup = mapKey == SavedLocations.FreerideSectionKey;
                bool modioGroup = mapKey == SavedLocations.ModIoSectionKey;
                bool sectionGroup = freerideGroup || modioGroup;

                if (!GroupMatchesSearch(mapTitle, mapKey, group, searchQuery, sectionGroup))
                    continue;

                bool mapTitleMatch = string.IsNullOrEmpty(searchQuery);
                if (!mapTitleMatch)
                {
                    string q = searchQuery.ToLowerInvariant();
                    mapTitleMatch = (mapTitle ?? "").ToLowerInvariant().Contains(q)
                        || (mapKey ?? "").ToLowerInvariant().Contains(q);
                }

                AddMapGroupHeader(_allSpotsHost, "AllMapHdr" + g, mapTitle, availHint, firstShown);
                firstShown = false;
                shownGroups++;

                for (int s = 0; s < group.Count; s++)
                {
                    SavedLocations.SavedSpotRef spot = group[s];
                    string spotMapKey = spot.MapKey;
                    int slot = spot.Slot;

                    string rowLabel;
                    if (sectionGroup)
                        rowLabel = SavedLocations.GetFreerideListRowLabel(
                            spot.MapLabel, spot.SpotName, slot);
                    else
                        rowLabel = spot.SpotName;

                    if (!mapTitleMatch && !SpotMatchesSearch(spot, rowLabel, searchQuery))
                        continue;

                    AddAllListSpotRow(_allSpotsHost, "Spot" + g + "_" + s, g * 100 + s,
                        rowLabel, mapCanGo, spotMapKey, slot);
                }
            }

            if (shownGroups == 0 && !string.IsNullOrEmpty(searchQuery))
            {
                var noMatchRow = AllSpotsCompactRow(_allSpotsHost, "NoMatch", SegmentColWidth);
                _allSpotRows.Add(noMatchRow);
                AddCenteredListLabel(noMatchRow.transform, "NoAllMatch",
                    "No maps match your search.", 10, FontStyle.Italic, UIHelpers.TextDim);
            }
        }

        public static void Tick()
        {
            if (_renamingSlot >= 0)
            {
                TickRename();
                return;
            }
            TickAllSearch();
        }

        public static void ClearUiRefs()
        {
            _mapLabel = null;
            _saveStatusLabel = null;
            for (int i = 0; i < _saveButtons.Length; i++)
                _saveButtons[i] = null;
            _allSpotsHost = null;
            _allSpotRows.Clear();
            _nameTexts = new Text[SavedLocations.SlotCount];
            _renamingSlot = -1;
            _renameBuffer = "";
            _allSearchBuffer = "";
            _allSearchFocused = false;
            _allSearchInputText = null;
            _allSearchBoxRect = null;
            _perkHost = null;
            _perkScroll = null;
            _perkTracks.Clear();
            _perkKnobs.Clear();
        }
    }
}
