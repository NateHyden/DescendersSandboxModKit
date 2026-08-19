using System;
using System.Collections.Generic;
using MelonLoader;
using DescendersModMenu;
using DescendersModMenu.Mods;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    /// <summary>
    /// Search tab - filters FavouritesManager's full registry (every mod that's ever been
    /// registered via a page's CreatePage, not just favourited ones) by name as you type,
    /// and renders each match using its own BuildControls - the exact same function the
    /// Favourites tab uses - so activating/adjusting a mod here does the real thing, not a copy.
    /// </summary>
    public static class SearchPage
    {
        private static Transform _listRoot;
        private static string _queryBuffer = "";
        private static bool _queryFocused = false;
        public static bool IsQueryFocused => _queryFocused;
        private static Text _queryInputText;
        private static Text _queryCursor;
        private static RectTransform _queryBoxRect;

        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("PSearchR", parent);
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

                _listRoot = content.transform;

                RebuildList();
            }
            catch (Exception ex) { MelonLogger.Error("SearchPage.CreatePage: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "SearchPage"); }
            return pg;
        }

        public static void RebuildList()
        {
            if ((object)_listRoot == null) return;
            try
            {
                while (_listRoot.childCount > 0)
                    GameObject.DestroyImmediate(_listRoot.GetChild(0).gameObject);

                var searchRow = UIHelpers.Obj("SearchInputRow", _listRoot);
                searchRow.AddComponent<Image>().color = UIHelpers.RowBg;
                var srLe = searchRow.AddComponent<LayoutElement>();
                srLe.preferredHeight = 36; srLe.minHeight = 36;
                var srHlg = searchRow.AddComponent<HorizontalLayoutGroup>();
                srHlg.padding = new RectOffset(8, 8, 4, 4);
                srHlg.spacing = 6; srHlg.childAlignment = TextAnchor.MiddleLeft;
                srHlg.childForceExpandHeight = true; srHlg.childForceExpandWidth = false;

                var searchBg = UIHelpers.Obj("SrBg", searchRow.transform);
                searchBg.AddComponent<Image>().color = UIHelpers.WinOuter;
                var sbgLe = searchBg.AddComponent<LayoutElement>();
                sbgLe.flexibleWidth = 1; sbgLe.minHeight = 26; sbgLe.preferredHeight = 26;
                var sbgHlg = searchBg.AddComponent<HorizontalLayoutGroup>();
                sbgHlg.padding = new RectOffset(8, 8, 0, 0);
                sbgHlg.childAlignment = TextAnchor.MiddleLeft;
                sbgHlg.childForceExpandWidth = true; sbgHlg.childForceExpandHeight = true;

                _queryInputText = UIHelpers.Txt("SrIT", searchBg.transform,
                    string.IsNullOrEmpty(_queryBuffer) ? "Type to search mods..." : _queryBuffer, 11,
                    FontStyle.Normal, TextAnchor.MiddleLeft,
                    string.IsNullOrEmpty(_queryBuffer) ? UIHelpers.TextDim : UIHelpers.TextLight);
                _queryInputText.horizontalOverflow = HorizontalWrapMode.Overflow;
                _queryInputText.verticalOverflow = VerticalWrapMode.Truncate;
                _queryInputText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                _queryCursor = UIHelpers.Txt("SrCur", searchBg.transform, "\u25CF",
                    10, FontStyle.Normal, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _queryCursor.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var scRT = UIHelpers.RT(_queryCursor.gameObject);
                scRT.anchorMin = new Vector2(1, 0); scRT.anchorMax = new Vector2(1, 1);
                scRT.pivot = new Vector2(1, 0.5f);
                scRT.sizeDelta = new Vector2(14, 0);
                scRT.anchoredPosition = new Vector2(-6, 0);
                _queryCursor.gameObject.SetActive(false);

                _queryBoxRect = UIHelpers.RT(searchBg);
                var searchFocusBtn = searchBg.AddComponent<UnityEngine.UI.Button>();
                searchFocusBtn.targetGraphic = searchBg.GetComponent<Image>();
                searchFocusBtn.onClick.AddListener(() => { _queryFocused = true; });

                if (!string.IsNullOrEmpty(_queryBuffer))
                {
                    UIHelpers.SmallBtn(searchRow.transform, "\u2716", () =>
                    {
                        _queryBuffer = "";
                        RebuildList();
                    });
                }

                UIHelpers.Divider(_listRoot);

                string q = _queryBuffer.Trim();

                if (string.IsNullOrEmpty(q))
                {
                    var hintRow = UIHelpers.Obj("HintRow", _listRoot);
                    hintRow.AddComponent<LayoutElement>().minHeight = 40;
                    var htxt = UIHelpers.Txt("HintTxt", hintRow.transform,
                        "Start typing to find any mod by name", 11,
                        FontStyle.Normal, TextAnchor.MiddleCenter, UIHelpers.TextDim);
                    UIHelpers.Fill(UIHelpers.RT(htxt.gameObject));
                    UIHelpers.AddScrollForwarders(_listRoot);
                    return;
                }

                List<string> ids = FavouritesManager.GetAllRegisteredIds();
                var matches = new List<ModFavEntry>();
                for (int i = 0; i < ids.Count; i++)
                {
                    ModFavEntry entry;
                    if (!FavouritesManager.TryGetEntry(ids[i], out entry)) continue;
                    if (entry.DisplayName != null &&
                        entry.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                        matches.Add(entry);
                }
                matches.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));

                if (matches.Count == 0)
                {
                    var noneRow = UIHelpers.Obj("NoneRow", _listRoot);
                    noneRow.AddComponent<LayoutElement>().minHeight = 40;
                    var ntxt = UIHelpers.Txt("NoneTxt", noneRow.transform,
                        "No mods match \"" + q + "\"", 11,
                        FontStyle.Normal, TextAnchor.MiddleCenter, UIHelpers.TextDim);
                    UIHelpers.Fill(UIHelpers.RT(ntxt.gameObject));
                    UIHelpers.AddScrollForwarders(_listRoot);
                    return;
                }

                bool first = true;
                for (int i = 0; i < matches.Count; i++)
                {
                    ModFavEntry entry = matches[i];
                    if (!first) UIHelpers.Divider(_listRoot);
                    first = false;

                    var hdr = UIHelpers.Obj("SH_" + entry.Id, _listRoot);
                    var hle = hdr.AddComponent<LayoutElement>();
                    hle.preferredHeight = 24; hle.minHeight = 24;
                    var hhlg = hdr.AddComponent<HorizontalLayoutGroup>();
                    hhlg.spacing = 6;
                    hhlg.padding = new RectOffset(4, 4, 0, 0);
                    hhlg.childAlignment = TextAnchor.MiddleLeft;
                    hhlg.childForceExpandWidth = false; hhlg.childForceExpandHeight = false;

                    var badge = UIHelpers.Panel("Badge", hdr.transform, new Color(0, 0, 0, 0));
                    var bt = UIHelpers.Txt("BT", badge.transform, entry.TabBadge, 9,
                        FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                    UIHelpers.Fill(UIHelpers.RT(bt.gameObject));
                    badge.AddComponent<LayoutElement>().preferredWidth = 50;

                    var nameT = UIHelpers.Txt("SN", hdr.transform, entry.DisplayName, 10,
                        FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextMid);
                    var nle = nameT.gameObject.AddComponent<LayoutElement>();
                    nle.flexibleWidth = 1; nle.preferredHeight = 24;

                    string capturedId = entry.Id;
                    var star = UIHelpers.StarBtn(hdr.transform, capturedId,
                        () => { FavouritesManager.Toggle(capturedId); FavouritesManager.RefreshAllStars(); });
                    FavouritesManager.RegisterStarButton(capturedId, star);

                    try { entry.BuildControls(_listRoot); }
                    catch (Exception ex)
                    {
                        ModLog.Warn("[Search] BuildControls(" + entry.Id + "): " + ex.Message);
                    }
                }

                UIHelpers.AddScrollForwarders(_listRoot);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("SearchPage.RebuildList: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "SearchPage");
            }
        }

        public static void SearchTick()
        {
            if ((object)_queryInputText == null) return;

            if (_queryFocused && Input.GetMouseButtonDown(0))
            {
                if ((object)_queryBoxRect != null)
                {
                    Vector2 mp = Input.mousePosition;
                    if (!RectTransformUtility.RectangleContainsScreenPoint(_queryBoxRect, mp, null))
                        _queryFocused = false;
                }
            }

            if ((object)_queryCursor != null)
            {
                _queryCursor.gameObject.SetActive(_queryFocused);
                if (_queryFocused)
                {
                    float alpha = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
                    Color col = UIHelpers.OnColor;
                    col.a = alpha;
                    _queryCursor.color = col;
                }
            }

            if (!_queryFocused) return;

            bool changed = false;
            foreach (char ch in Input.inputString)
            {
                if (ch == '\b')
                {
                    if (_queryBuffer.Length > 0) { _queryBuffer = _queryBuffer.Substring(0, _queryBuffer.Length - 1); changed = true; }
                }
                else if (ch == '\n' || ch == '\r') { _queryFocused = false; }
                else if (ch == (char)27) { _queryFocused = false; }
                else if (_queryBuffer.Length < 40) { _queryBuffer += ch; changed = true; }
            }

            if (changed) RebuildList();
        }
    }
}

