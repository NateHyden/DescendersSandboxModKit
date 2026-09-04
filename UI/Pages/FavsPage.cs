using System;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using UnityEngine.UI;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    public static class FavsPage
    {
        private static Transform _contentRoot;
        private static ScrollRect _scrollRect;
        private static bool _dirty = false;

        public static bool IsAnyActive => FavouritesManager.IsAnyActive;

        /// <summary>Mark for rebuild — the actual rebuild is deferred to Tick()/
        /// CheckDirty() rather than happening here, since MarkDirty() runs
        /// synchronously from inside FavouritesManager.Toggle(), which itself
        /// runs from inside a star Button's own onClick. Calling Rebuild()
        /// (DestroyImmediate on every row) at that point would destroy the
        /// very GameObject whose click is still mid-invocation — a real
        /// Unity footgun, not just theoretical.</summary>
        public static void MarkDirty()
        {
            _dirty = true;
        }

        /// <summary>Called every frame from ModEntry.OnUpdate. If dirty AND the
        /// Favourites tab is currently visible, rebuilds — one frame later
        /// than the click that caused it, not mid-click, so it still feels
        /// instant without the DestroyImmediate risk above.</summary>
        public static void Tick()
        {
            if (!_dirty) return;
            try
            {
                if (!_contentRoot) { ClearUiRefs(); return; }
                Transform t = _contentRoot;
                bool visible = true;
                while (t)
                {
                    if (!t.gameObject.activeSelf) { visible = false; break; }
                    t = t.parent;
                }
                if (visible) { Rebuild(); _dirty = false; }
            }
            catch (MissingReferenceException)
            {
                ClearUiRefs();
                _dirty = false;
            }
            catch (NullReferenceException)
            {
                ClearUiRefs();
                _dirty = false;
            }
        }

        public static void ClearUiRefs()
        {
            _contentRoot = null;
            _scrollRect = null;
        }

        /// <summary>Called when Favourites tab becomes visible via tab-switch. Rebuilds if needed.</summary>
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
                vlg.spacing = 2f;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 4, 4);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

                _contentRoot = content.transform;

                _dirty = true;
            }
            catch (Exception ex) { MelonLogger.Error("[FavsPage] CreatePage: " + ex);  Telemetry.ReportErrorAsync(ex, "FavsPage"); }
            return pg;
        }

        public static void Rebuild()
        {
            if ((object)_contentRoot == null) return;

            FavouritesManager.ClearRefreshCallbacks();

            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(_contentRoot.GetChild(i).gameObject);

            var favIds = FavouritesManager.GetAll();

            if (favIds.Count == 0)
            {
                BuildEmptyState(_contentRoot);
                return;
            }

            // ── Remove All button ─────────────────────────────────────
            var clearRow = UIHelpers.BareBtnRow(_contentRoot, CompactRowH);
            UIHelpers.ActionBtnOrange(clearRow.transform, "\u2716  Remove All Favourites", () =>
            {
                FavouritesManager.ClearAll();
            }, 186);

            foreach (var id in favIds)
            {
                ModFavEntry entry;
                if (!FavouritesManager.TryGetEntry(id, out entry))
                {
                    ModLog.Debug("[Favs] Skipping unknown ID: " + id);
                    continue;
                }

                var card = UIHelpers.Obj("Fav_" + id, _contentRoot);
                var cardLe = card.AddComponent<LayoutElement>();
                cardLe.flexibleWidth = 1;
                var cardV = card.AddComponent<VerticalLayoutGroup>();
                cardV.spacing = 1f;
                cardV.padding = new RectOffset(0, 0, 0, 0);
                cardV.childAlignment = TextAnchor.UpperCenter;
                cardV.childForceExpandWidth = true;
                cardV.childForceExpandHeight = false;
                cardV.childControlWidth = true;
                cardV.childControlHeight = true;

                var hdr = UIHelpers.Obj("FH_" + id, card.transform);
                var hle = hdr.AddComponent<LayoutElement>();
                hle.preferredHeight = 14; hle.minHeight = 14;
                var hhlg = hdr.AddComponent<HorizontalLayoutGroup>();
                hhlg.spacing = 4;
                hhlg.padding = new RectOffset(2, 2, 0, 0);
                hhlg.childAlignment = TextAnchor.MiddleLeft;
                hhlg.childForceExpandWidth = false; hhlg.childForceExpandHeight = false;

                var badge = UIHelpers.Panel("Badge", hdr.transform, new Color(0, 0, 0, 0));
                var bt = UIHelpers.Txt("BT", badge.transform, entry.TabBadge, 7,
                    FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                UIHelpers.Fill(UIHelpers.RT(bt.gameObject));
                badge.AddComponent<LayoutElement>().preferredWidth = 40;

                var nameT = UIHelpers.Txt("FN", hdr.transform, entry.DisplayName, 8,
                    FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextMid);
                var nle = nameT.gameObject.AddComponent<LayoutElement>();
                nle.flexibleWidth = 1; nle.preferredHeight = 14;

                string capturedId = id;
                UIHelpers.StarBtn(hdr.transform, capturedId,
                    () => { FavouritesManager.Toggle(capturedId); });

                try { entry.BuildControls(card.transform); }
                catch (Exception ex)
                {
                    ModLog.Warn("[Favs] BuildControls(" + id + "): " + ex.Message);
                }
            }

            UIHelpers.AddScrollForwarders(_contentRoot);

            var crtRT = UIHelpers.RT(_contentRoot.gameObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(crtRT);
            Canvas.ForceUpdateCanvases();
            if ((object)_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 1f;

            FavouritesManager.InvokeRefresh();
        }

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

            var sp1 = UIHelpers.Obj("Sp1", box.transform);
            sp1.AddComponent<LayoutElement>().preferredHeight = 20;

            var starT = UIHelpers.Txt("BStar", box.transform, "\u2605", 32,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            starT.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;

            var msg = UIHelpers.Txt("EMsg", box.transform, "No favourites yet", 13,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextDim);
            msg.gameObject.AddComponent<LayoutElement>().preferredHeight = 20;

            var hint = UIHelpers.Txt("EHint", box.transform,
                "Star any mod from its tab to add it here", 10,
                FontStyle.Normal, TextAnchor.MiddleCenter, UIHelpers.TextDim);
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;
        }

        public const float CompactRowH = 24f;

        public static GameObject CompactStatRow(string label, Transform p)
        {
            var row = UIHelpers.Panel(label + "R", p, UIHelpers.RowBg, UIHelpers.RowSp);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = CompactRowH; le.minHeight = CompactRowH; le.flexibleHeight = 0;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5; hlg.padding = new RectOffset(5, 5, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            var bd = UIHelpers.Panel("Bd", row.transform, UIHelpers.RowBorder, UIHelpers.RowSp);
            bd.GetComponent<Image>().raycastTarget = false;
            UIHelpers.Fill(UIHelpers.RT(bd));
            bd.AddComponent<LayoutElement>().ignoreLayout = true;

            if (!string.IsNullOrEmpty(label))
            {
                var t = UIHelpers.Txt(label + "L", row.transform, label, 10, FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextLight);
                var tle = t.gameObject.AddComponent<LayoutElement>();
                tle.flexibleWidth = 1; tle.preferredHeight = CompactRowH; tle.minHeight = CompactRowH;
            }
            return row;
        }

        // ══════════════════════════════════════════════════════════════
        // ══════════════════════════════════════════════════════════════

        /// <summary>Simple toggle row (e.g. Cut Brakes, No Bail)</summary>
        public static void BuildSimpleToggle(Transform parent, string id, string label,
            FavBoolGetter getState, FavAction doToggle, FavAction refreshPage)
        {
            bool initOn = getState();
            var row = CompactStatRow(label, parent);
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

        /// <summary>One-shot action row - no toggle, no persisted state (e.g. Complete Missions, Level Reset).
        /// getResult can return a short status string shown after the button fires; pass null to skip it.</summary>
        public static void BuildActionButton(Transform parent, string id, string label, string btnLabel,
            FavAction doAction, FavAction refreshPage, FavStringGetter getResult = null)
        {
            var row = CompactStatRow(label, parent);
            Text resultTxt = null;
            if (getResult != null)
            {
                resultTxt = UIHelpers.Txt("FAR_" + id, row.transform, "", 10,
                    FontStyle.Normal, TextAnchor.MiddleRight, UIHelpers.TextDim);
                resultTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            }
            UIHelpers.ActionBtnOrange(row.transform, btnLabel, () =>
            {
                doAction();
                if (resultTxt != null && getResult != null) resultTxt.text = getResult();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
            }, 100);
        }

        /// <summary>Toggle-only row (no slider) for simple on/off mods.</summary>
        public static void BuildToggleOnly(Transform parent, string id, string label,
            FavBoolGetter getState, FavAction doToggle)
        {
            bool initOn = getState();
            var row = CompactStatRow(label, parent);
            var togVal = UIHelpers.Txt("FTV_" + id, row.transform, initOn ? "ON" : "OFF", 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, initOn ? UIHelpers.OnColor : UIHelpers.OffColor);
            togVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            Image track; RectTransform knob;
            UIHelpers.Toggle(row.transform, "FTg_" + id, () =>
            {
                doToggle();
                RefreshFavourites();
                FavouritesManager.RefreshAllStars();
            }, out track, out knob);
            UIHelpers.SetToggle(track, knob, initOn);
        }

        /// <summary>Toggle + level slider row (e.g. Wide Tyres, Acceleration)</summary>
        public static void BuildToggleSlider(Transform parent, string id, string label,
            FavBoolGetter getState, FavAction doToggle,
            FavIntGetter getLevel, FavAction onIncrease, FavAction onDecrease,
            int maxLevel, FavFloatGetter getBarPct, FavAction refreshPage,
            FavStringGetter getDisplayVal = null)
        {
            bool initOn = getState();
            var row = CompactStatRow(label, parent);
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
            var row = CompactStatRow(label, parent);
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
            int min, int max, FavAction refreshPage, int defaultLevel = 10,
            FavStringGetter getDisplayVal = null)
        {
            var row = CompactStatRow(label, parent);
            var minus = UIHelpers.SmallBtn(row.transform, "\u25C0", () =>
            {
                onMinus();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
            });
            var lvlVal = UIHelpers.Txt("FSt_" + id, row.transform,
                getDisplayVal != null ? getDisplayVal() : getLevel().ToString(), 13, FontStyle.Bold,
                TextAnchor.MiddleCenter, UIHelpers.Accent);
            lvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
            var plus = UIHelpers.SmallBtn(row.transform, "\u25B6", () =>
            {
                onPlus();
                if (refreshPage != null) refreshPage();
                RefreshFavourites();
            });

            FavouritesManager.RegisterRefresh(id, () =>
            {
                int lv = getLevel();
                if (lvlVal) lvlVal.text = getDisplayVal != null ? getDisplayVal() : lv.ToString();
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
            FavAction refreshPage, int defaultLevel = 5,
            FavStringGetter disp1 = null, FavStringGetter disp2 = null, FavStringGetter disp3 = null)
        {
            var r1 = CompactStatRow(label1, parent);
            var b1 = UIHelpers.MakeBar("FB1_" + id, r1.transform, pct1());
            var v1 = UIHelpers.Txt("FV1_" + id, r1.transform,
                disp1 != null ? disp1() : get1().ToString(), 12,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            v1.gameObject.AddComponent<LayoutElement>().preferredWidth = disp1 != null ? 44 : 18;
            UIHelpers.SmallBtn(r1.transform, "-", () => { dec1(); if (refreshPage != null) refreshPage(); RefreshFavourites(); });
            UIHelpers.SmallBtn(r1.transform, "+", () => { inc1(); if (refreshPage != null) refreshPage(); RefreshFavourites(); });

            var r2 = CompactStatRow(label2, parent);
            var b2 = UIHelpers.MakeBar("FB2_" + id, r2.transform, pct2());
            var v2 = UIHelpers.Txt("FV2_" + id, r2.transform,
                disp2 != null ? disp2() : get2().ToString(), 12,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            v2.gameObject.AddComponent<LayoutElement>().preferredWidth = disp2 != null ? 44 : 18;
            UIHelpers.SmallBtn(r2.transform, "-", () => { dec2(); if (refreshPage != null) refreshPage(); RefreshFavourites(); });
            UIHelpers.SmallBtn(r2.transform, "+", () => { inc2(); if (refreshPage != null) refreshPage(); RefreshFavourites(); });

            var r3 = CompactStatRow(label3, parent);
            var b3 = UIHelpers.MakeBar("FB3_" + id, r3.transform, pct3());
            var v3 = UIHelpers.Txt("FV3_" + id, r3.transform,
                disp3 != null ? disp3() : get3().ToString(), 12,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            v3.gameObject.AddComponent<LayoutElement>().preferredWidth = disp3 != null ? 44 : 18;
            UIHelpers.SmallBtn(r3.transform, "-", () => { dec3(); if (refreshPage != null) refreshPage(); RefreshFavourites(); });
            UIHelpers.SmallBtn(r3.transform, "+", () => { inc3(); if (refreshPage != null) refreshPage(); RefreshFavourites(); });

            FavouritesManager.RegisterRefresh(id, () =>
            {
                UIHelpers.SetBar(b1, pct1()); if (v1) v1.text = disp1 != null ? disp1() : get1().ToString();
                UIHelpers.SetBar(b2, pct2()); if (v2) v2.text = disp2 != null ? disp2() : get2().ToString();
                UIHelpers.SetBar(b3, pct3()); if (v3) v3.text = disp3 != null ? disp3() : get3().ToString();
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
            int min, int max, FavAction refreshPage, int defaultLevel = 10,
            FavStringGetter getDisplayVal = null)
        {
            bool initOn = getState();
            var row = CompactStatRow(label, parent);
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
                getDisplayVal != null ? getDisplayVal() : getLevel().ToString(), 13, FontStyle.Bold,
                TextAnchor.MiddleCenter, UIHelpers.Accent);
            lvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
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
                if (lvlVal) lvlVal.text = getDisplayVal != null ? getDisplayVal() : lv.ToString();
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
            var row = CompactStatRow(label, parent);
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

