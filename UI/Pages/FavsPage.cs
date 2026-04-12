using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class FavsPage
    {
        private static Transform _contentRoot;
        private static ScrollRect _scrollRect;
        private static bool _dirty = false;

        public static bool IsAnyActive => FavouritesManager.IsAnyActive;

        /// <summary>Mark for rebuild on next tab show — avoids lag spike during star toggle.</summary>
        public static void MarkDirty()
        {
            // If Favourites tab is currently visible, rebuild now (for un-star UX)
            if ((object)_contentRoot != null)
            {
                Transform t = _contentRoot;
                bool visible = true;
                while ((object)t != null)
                {
                    if (!t.gameObject.activeSelf) { visible = false; break; }
                    t = t.parent;
                }
                if (visible) { Rebuild(); return; }
            }
            _dirty = true;
        }

        /// <summary>Called when Favourites tab becomes visible. Rebuilds if needed.</summary>
        public static void CheckDirty()
        {
            if (_dirty) { Rebuild(); _dirty = false; }
        }

        // ── CreatePage ────────────────────────────────────────────────
        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("PFavR", parent);
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
                crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 0);
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
            catch (Exception ex) { MelonLogger.Error("[FavsPage] CreatePage: " + ex); }
            return pg;
        }

        // ── Rebuild — destroys and recreates all favourite entries ────
        public static void Rebuild()
        {
            if ((object)_contentRoot == null) return;

            FavouritesManager.ClearRefreshCallbacks();

            // Destroy all children
            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(_contentRoot.GetChild(i).gameObject);

            var favIds = FavouritesManager.GetAll();

            if (favIds.Count == 0)
            {
                BuildEmptyState(_contentRoot);
                return;
            }

            // ── Remove All button ─────────────────────────────────────
            var clearRow = UIHelpers.StatRow("", _contentRoot);
            UIHelpers.ActionBtnOrange(clearRow.transform, "\u2716  Remove All Favourites", () =>
            {
                FavouritesManager.ClearAll();
            }, 186);
            UIHelpers.Divider(_contentRoot);

            bool first = true;
            foreach (var id in favIds)
            {
                ModFavEntry entry;
                if (!FavouritesManager.TryGetEntry(id, out entry))
                {
                    MelonLogger.Msg("[Favs] Skipping unknown ID: " + id);
                    continue;
                }

                if (!first) UIHelpers.Divider(_contentRoot);
                first = false;

                // ── Entry header: tab badge + mod name + remove star ──
                var hdr = UIHelpers.Obj("FH_" + id, _contentRoot);
                var hle = hdr.AddComponent<LayoutElement>();
                hle.preferredHeight = 24; hle.minHeight = 24;
                var hhlg = hdr.AddComponent<HorizontalLayoutGroup>();
                hhlg.spacing = 6;
                hhlg.padding = new RectOffset(4, 4, 0, 0);
                hhlg.childAlignment = TextAnchor.MiddleLeft;
                hhlg.childForceExpandWidth = false; hhlg.childForceExpandHeight = false;

                // Tab badge
                var badge = UIHelpers.Panel("Badge", hdr.transform, new Color(0, 0, 0, 0));
                var bt = UIHelpers.Txt("BT", badge.transform, entry.TabBadge, 9,
                    FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                UIHelpers.Fill(UIHelpers.RT(bt.gameObject));
                badge.AddComponent<LayoutElement>().preferredWidth = 50;

                // Mod name
                var nameT = UIHelpers.Txt("FN", hdr.transform, entry.DisplayName, 10,
                    FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextMid);
                var nle = nameT.gameObject.AddComponent<LayoutElement>();
                nle.flexibleWidth = 1; nle.preferredHeight = 24;

                // Remove star — uses the real ID for colour, not registered globally
                string capturedId = id;
                var removeStar = UIHelpers.StarBtn(hdr.transform, capturedId,
                    () => { FavouritesManager.Toggle(capturedId); });

                // ── Build the mod's actual controls ───────────────────
                try { entry.BuildControls(_contentRoot); }
                catch (Exception ex)
                {
                    MelonLogger.Warning("[Favs] BuildControls(" + id + "): " + ex.Message);
                }
            }

            UIHelpers.AddScrollForwarders(_contentRoot);

            // Force layout recalculation so scrollbar detects content height
            var crtRT = UIHelpers.RT(_contentRoot.gameObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(crtRT);
            Canvas.ForceUpdateCanvases();
            if ((object)_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 1f; // scroll to top
        }

        // ── RefreshFavourites — updates displayed values ─────────────
        public static void RefreshFavourites()
        {
            FavouritesManager.InvokeRefresh();
        }

        // ── Empty state ───────────────────────────────────────────────
        private static void BuildEmptyState(Transform parent)
        {
            var box = UIHelpers.Obj("EmptyBox", parent);
            var ble = box.AddComponent<LayoutElement>();
            ble.preferredHeight = 200;
            var vlg = box.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(20, 20, 40, 20);

            // Spacer
            var sp1 = UIHelpers.Obj("Sp1", box.transform);
            sp1.AddComponent<LayoutElement>().preferredHeight = 20;

            // Big star
            var starT = UIHelpers.Txt("BStar", box.transform, "\u2605", 32,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            starT.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;

            // "No favourites yet"
            var msg = UIHelpers.Txt("EMsg", box.transform, "No favourites yet", 13,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextDim);
            msg.gameObject.AddComponent<LayoutElement>().preferredHeight = 20;

            // Hint
            var hint = UIHelpers.Txt("EHint", box.transform,
                "Star any mod from its tab to add it here", 10,
                FontStyle.Normal, TextAnchor.MiddleCenter, UIHelpers.TextDim);
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;
        }

        // ══════════════════════════════════════════════════════════════
        //  BUILDER HELPERS — used by page factories
        // ══════════════════════════════════════════════════════════════

        /// <summary>Simple toggle row (e.g. Cut Brakes, No Bail)</summary>
        public static void BuildSimpleToggle(Transform parent, string id, string label,
            FavBoolGetter getState, FavAction doToggle, FavAction refreshPage)
        {
            bool initOn = getState();
            var row = UIHelpers.StatRow(label, parent);
            var val = UIHelpers.Txt("FT_" + id, row.transform, initOn ? "ON" : "OFF", 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, initOn ? UIHelpers.OnColor : UIHelpers.OffColor);
            val.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            Image track; RectTransform knob;
            UIHelpers.Toggle(row.transform, "FTg_" + id, () =>
            {
                doToggle();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
                FavouritesManager.RefreshAllStars();
            }, out track, out knob);
            UIHelpers.SetToggle(track, knob, initOn);
            UIHelpers.SetRowActive(row, initOn);

            FavouritesManager.RegisterRefresh(id, () =>
            {
                bool on = getState();
                if (val) { val.text = on ? "ON" : "OFF"; val.color = on ? UIHelpers.OnColor : UIHelpers.OffColor; }
                UIHelpers.SetToggle(track, knob, on);
                UIHelpers.SetRowActive(row, on);
            });
        }

        /// <summary>Toggle + level slider row (e.g. Wide Tyres, Acceleration)</summary>
        public static void BuildToggleSlider(Transform parent, string id, string label,
            FavBoolGetter getState, FavAction doToggle,
            FavIntGetter getLevel, FavAction onIncrease, FavAction onDecrease,
            int maxLevel, FavFloatGetter getBarPct, FavAction refreshPage,
            FavStringGetter getDisplayVal = null)
        {
            bool initOn = getState();
            var row = UIHelpers.StatRow(label, parent);
            var togVal = UIHelpers.Txt("FTV_" + id, row.transform, initOn ? "ON" : "OFF", 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, initOn ? UIHelpers.OnColor : UIHelpers.OffColor);
            togVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            Image track; RectTransform knob;
            UIHelpers.Toggle(row.transform, "FTg_" + id, () =>
            {
                doToggle();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
                FavouritesManager.RefreshAllStars();
            }, out track, out knob);
            UIHelpers.SetToggle(track, knob, initOn);
            var bar = UIHelpers.MakeBar("FB_" + id, row.transform, getBarPct());
            var lvlVal = UIHelpers.Txt("FLV_" + id, row.transform,
                getDisplayVal != null ? getDisplayVal() : getLevel().ToString(), 12,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
            lvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 24;
            UIHelpers.SetRowActive(row, initOn);
            UIHelpers.SmallBtn(row.transform, "-", () =>
            {
                onDecrease();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
                FavouritesManager.RefreshAllStars();
            });
            UIHelpers.SmallBtn(row.transform, "+", () =>
            {
                onIncrease();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
                FavouritesManager.RefreshAllStars();
            });

            FavouritesManager.RegisterRefresh(id, () =>
            {
                bool on = getState();
                if (togVal) { togVal.text = on ? "ON" : "OFF"; togVal.color = on ? UIHelpers.OnColor : UIHelpers.OffColor; }
                UIHelpers.SetToggle(track, knob, on);
                UIHelpers.SetBar(bar, getBarPct());
                if (lvlVal) lvlVal.text = getDisplayVal != null ? getDisplayVal() : getLevel().ToString();
                UIHelpers.SetRowActive(row, on);
            });
        }

        /// <summary>Slider-only row (e.g. Gravity, Time of Day)</summary>
        public static void BuildSliderOnly(Transform parent, string id, string label,
            FavIntGetter getLevel, FavAction onIncrease, FavAction onDecrease,
            FavFloatGetter getBarPct, FavAction refreshPage,
            FavStringGetter getDisplayVal = null, FavBoolGetter isNonDefault = null)
        {
            var row = UIHelpers.StatRow(label, parent);
            var bar = UIHelpers.MakeBar("FB_" + id, row.transform, getBarPct());
            var lvlVal = UIHelpers.Txt("FLV_" + id, row.transform,
                getDisplayVal != null ? getDisplayVal() : getLevel().ToString(), 12,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
            lvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 24;
            UIHelpers.SmallBtn(row.transform, "-", () =>
            {
                onDecrease();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
            });
            UIHelpers.SmallBtn(row.transform, "+", () =>
            {
                onIncrease();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
            });

            FavouritesManager.RegisterRefresh(id, () =>
            {
                UIHelpers.SetBar(bar, getBarPct());
                if (lvlVal) lvlVal.text = getDisplayVal != null ? getDisplayVal() : getLevel().ToString();
                if (isNonDefault != null) UIHelpers.SetRowActive(row, isNonDefault());
            });
        }

        /// <summary>Stepper row (e.g. Bike Size, Player Size)</summary>
        public static void BuildStepper(Transform parent, string id, string label,
            FavIntGetter getLevel, FavAction onMinus, FavAction onPlus,
            int min, int max, FavAction refreshPage, int defaultLevel = 10)
        {
            var row = UIHelpers.StatRow(label, parent);
            var minus = UIHelpers.SmallBtn(row.transform, "\u25C0", () =>
            {
                onMinus();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
            });
            var lvlVal = UIHelpers.Txt("FSt_" + id, row.transform,
                getLevel().ToString(), 13, FontStyle.Bold,
                TextAnchor.MiddleCenter, UIHelpers.Accent);
            lvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 32;
            var plus = UIHelpers.SmallBtn(row.transform, "\u25B6", () =>
            {
                onPlus();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
            });

            FavouritesManager.RegisterRefresh(id, () =>
            {
                int lv = getLevel();
                if (lvlVal) lvlVal.text = lv.ToString();
                if ((object)minus != null) minus.interactable = lv > min;
                if ((object)plus != null) plus.interactable = lv < max;
                UIHelpers.SetRowActive(row, lv != defaultLevel);
            });
        }

        /// <summary>Three-slider section (Suspension)</summary>
        public static void BuildTripleSlider(Transform parent, string id,
            string label1, FavIntGetter get1, FavAction inc1, FavAction dec1,
            string label2, FavIntGetter get2, FavAction inc2, FavAction dec2,
            string label3, FavIntGetter get3, FavAction inc3, FavAction dec3,
            FavFloatGetter pct1, FavFloatGetter pct2, FavFloatGetter pct3,
            FavAction refreshPage, int defaultLevel = 5)
        {
            // Row 1
            var r1 = UIHelpers.StatRow(label1, parent);
            var b1 = UIHelpers.MakeBar("FB1_" + id, r1.transform, pct1());
            var v1 = UIHelpers.Txt("FV1_" + id, r1.transform, get1().ToString(), 12,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
            v1.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
            UIHelpers.SmallBtn(r1.transform, "-", () => { dec1(); if (refreshPage != null) refreshPage(); RefreshFavourites(); });
            UIHelpers.SmallBtn(r1.transform, "+", () => { inc1(); if (refreshPage != null) refreshPage(); RefreshFavourites(); });

            // Row 2
            var r2 = UIHelpers.StatRow(label2, parent);
            var b2 = UIHelpers.MakeBar("FB2_" + id, r2.transform, pct2());
            var v2 = UIHelpers.Txt("FV2_" + id, r2.transform, get2().ToString(), 12,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
            v2.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
            UIHelpers.SmallBtn(r2.transform, "-", () => { dec2(); if (refreshPage != null) refreshPage(); RefreshFavourites(); });
            UIHelpers.SmallBtn(r2.transform, "+", () => { inc2(); if (refreshPage != null) refreshPage(); RefreshFavourites(); });

            // Row 3
            var r3 = UIHelpers.StatRow(label3, parent);
            var b3 = UIHelpers.MakeBar("FB3_" + id, r3.transform, pct3());
            var v3 = UIHelpers.Txt("FV3_" + id, r3.transform, get3().ToString(), 12,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
            v3.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
            UIHelpers.SmallBtn(r3.transform, "-", () => { dec3(); if (refreshPage != null) refreshPage(); RefreshFavourites(); });
            UIHelpers.SmallBtn(r3.transform, "+", () => { inc3(); if (refreshPage != null) refreshPage(); RefreshFavourites(); });

            FavouritesManager.RegisterRefresh(id, () =>
            {
                UIHelpers.SetBar(b1, pct1()); if (v1) v1.text = get1().ToString();
                UIHelpers.SetBar(b2, pct2()); if (v2) v2.text = get2().ToString();
                UIHelpers.SetBar(b3, pct3()); if (v3) v3.text = get3().ToString();
                bool active = get1() != defaultLevel || get2() != defaultLevel || get3() != defaultLevel;
                UIHelpers.SetRowActive(r1, active);
                UIHelpers.SetRowActive(r2, active);
                UIHelpers.SetRowActive(r3, active);
            });
        }

        /// <summary>Toggle + stepper (e.g. Wheel Size)</summary>
        public static void BuildToggleStepper(Transform parent, string id, string label,
            FavBoolGetter getState, FavAction doToggle,
            FavIntGetter getLevel, FavAction onMinus, FavAction onPlus,
            int min, int max, FavAction refreshPage, int defaultLevel = 10)
        {
            bool initOn = getState();
            var row = UIHelpers.StatRow(label, parent);
            var togVal = UIHelpers.Txt("FTV_" + id, row.transform, initOn ? "ON" : "OFF", 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, initOn ? UIHelpers.OnColor : UIHelpers.OffColor);
            togVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            Image track; RectTransform knob;
            UIHelpers.Toggle(row.transform, "FTg_" + id, () =>
            {
                doToggle();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
                FavouritesManager.RefreshAllStars();
            }, out track, out knob);
            UIHelpers.SetToggle(track, knob, initOn);
            UIHelpers.SetRowActive(row, initOn);
            var minus = UIHelpers.SmallBtn(row.transform, "\u25C0", () =>
            {
                onMinus();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
            });
            var lvlVal = UIHelpers.Txt("FSt_" + id, row.transform,
                getLevel().ToString(), 13, FontStyle.Bold,
                TextAnchor.MiddleCenter, UIHelpers.Accent);
            lvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 32;
            var plus = UIHelpers.SmallBtn(row.transform, "\u25B6", () =>
            {
                onPlus();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
            });

            FavouritesManager.RegisterRefresh(id, () =>
            {
                bool on = getState();
                if (togVal) { togVal.text = on ? "ON" : "OFF"; togVal.color = on ? UIHelpers.OnColor : UIHelpers.OffColor; }
                UIHelpers.SetToggle(track, knob, on);
                int lv = getLevel();
                if (lvlVal) lvlVal.text = lv.ToString();
                UIHelpers.SetRowActive(row, on);
            });
        }

        /// <summary>Toggle + intensity stepper (Torch)</summary>
        public static void BuildToggleIntensityStepper(Transform parent, string id, string label,
            FavBoolGetter getState, FavAction doToggle,
            FavStringGetter getDisplay, FavAction onMinus, FavAction onPlus,
            FavAction refreshPage)
        {
            bool initOn = getState();
            var row = UIHelpers.StatRow(label, parent);
            var togVal = UIHelpers.Txt("FTV_" + id, row.transform, initOn ? "ON" : "OFF", 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, initOn ? UIHelpers.OnColor : UIHelpers.OffColor);
            togVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            Image track; RectTransform knob;
            UIHelpers.Toggle(row.transform, "FTg_" + id, () =>
            {
                doToggle();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
                FavouritesManager.RefreshAllStars();
            }, out track, out knob);
            UIHelpers.SetToggle(track, knob, initOn);
            UIHelpers.SetRowActive(row, initOn);
            var intLbl = UIHelpers.Txt("FInt_" + id, row.transform,
                getDisplay(), 11, FontStyle.Normal,
                TextAnchor.MiddleCenter, UIHelpers.TextMid);
            intLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 52;
            UIHelpers.SmallBtn(row.transform, "-", () =>
            {
                onMinus();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
            });
            UIHelpers.SmallBtn(row.transform, "+", () =>
            {
                onPlus();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
            });

            FavouritesManager.RegisterRefresh(id, () =>
            {
                bool on = getState();
                if (togVal) { togVal.text = on ? "ON" : "OFF"; togVal.color = on ? UIHelpers.OnColor : UIHelpers.OffColor; }
                UIHelpers.SetToggle(track, knob, on);
                if (intLbl) intLbl.text = getDisplay();
                UIHelpers.SetRowActive(row, on);
            });
        }
    }
}