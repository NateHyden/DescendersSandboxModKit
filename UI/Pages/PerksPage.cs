using System;
using System.Collections.Generic;
using System.Text;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using UnityEngine.UI;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    public static class PerksPage
    {
        public static bool IsAnyActive { get { return false; } }

        private static Transform _contentRoot;
        private static ScrollRect _scrollRect;
        private static Text _resultTxt;

        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("PPerks", parent);
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
            catch (Exception ex) { MelonLogger.Error("[PerksPage] CreatePage: " + ex);  Telemetry.ReportErrorAsync(ex, "PerksPage"); }
            return pg;
        }

        private static bool _everBuilt = false;

        public static void Rebuild()
        {
            if ((object)_contentRoot == null) return;
            try
            {
                float savedScroll = 1f;
                if (_everBuilt && (object)_scrollRect != null) savedScroll = _scrollRect.verticalNormalizedPosition;
                _everBuilt = true;

                for (int i = _contentRoot.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(_contentRoot.GetChild(i).gameObject);

                var crewHdr = UIHelpers.Obj("CrewPerksH", _contentRoot);
                var crewHdrLe = crewHdr.AddComponent<LayoutElement>();
                crewHdrLe.preferredHeight = 28; crewHdrLe.minHeight = 28; crewHdrLe.flexibleHeight = 0;

                var crewHint = UIHelpers.Txt("CrewHint", crewHdr.transform,
                    "Click a badge to grant or remove it. Be loaded into Career or a Bike Park.",
                    9, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
                UIHelpers.Fill(UIHelpers.RT(crewHint.gameObject));
                crewHint.raycastTarget = false;
                crewHint.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                var crewBar = UIHelpers.Panel("Bar", crewHdr.transform, UIHelpers.Accent);
                var crewBarRt = UIHelpers.RT(crewBar);
                crewBarRt.anchorMin = new Vector2(0f, 0.5f); crewBarRt.anchorMax = new Vector2(0f, 0.5f);
                crewBarRt.pivot = new Vector2(0f, 0.5f);
                crewBarRt.sizeDelta = new Vector2(3f, 14f);
                crewBarRt.anchoredPosition = Vector2.zero;
                crewBar.AddComponent<LayoutElement>().ignoreLayout = true;

                var crewTitle = UIHelpers.Txt("CrewT", crewHdr.transform, "CREW MEMBER PERKS", 11,
                    FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.Accent);
                var crewTitleRt = UIHelpers.RT(crewTitle.gameObject);
                crewTitleRt.anchorMin = new Vector2(0f, 0f); crewTitleRt.anchorMax = new Vector2(0f, 1f);
                crewTitleRt.pivot = new Vector2(0f, 0.5f);
                crewTitleRt.sizeDelta = new Vector2(160f, 0f);
                crewTitleRt.anchoredPosition = new Vector2(10f, 0f);
                crewTitle.raycastTarget = false;
                crewTitle.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                var clearRow = UIHelpers.StatRow("Clear All Active Perks", _contentRoot);
                UIHelpers.ActionBtnOrange(clearRow.transform, "Clear All", () =>
                {
                    PerkMenu.ClearAllPerks();
                    Rebuild();
                }, 90);

                var resultRow = UIHelpers.StatRow("Last Result", _contentRoot);
                _resultTxt = UIHelpers.Txt("PkResult", resultRow.transform, PerkMenu.LastResult,
                    11, FontStyle.Normal, TextAnchor.MiddleRight, UIHelpers.Accent);
                _resultTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                UIHelpers.Divider(_contentRoot);

                GameModifier[] perks = PerkMenu.AllPerks;
                if (perks == null || perks.Length == 0)
                {
                    var emptyRow = UIHelpers.StatRow("", _contentRoot);
                    var emptyTxt = UIHelpers.Txt("Empty", emptyRow.transform,
                        "No perks found - open this tab while loaded into a session, then reopen the menu.",
                        10, FontStyle.Normal, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                    emptyTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                }
                else
                {
                    UIHelpers.SectionHeader("ACTIVE PERKS", _contentRoot);
                    int activeCount = 0;
                    for (int i = 0; i < perks.Length; i++)
                    {
                        if ((object)perks[i] == null || !PerkMenu.HasPerk(perks[i])) continue;
                        activeCount++;
                        BuildActiveSummaryRow(perks[i]);
                    }
                    if (activeCount == 0)
                    {
                        var noneRow = UIHelpers.StatRow("", _contentRoot);
                        var noneTxt = UIHelpers.Txt("NoneActive", noneRow.transform, "None active.",
                            10, FontStyle.Normal, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                        noneTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                    }

                    UIHelpers.Divider(_contentRoot);

                    // ── All perks grid ───────────────────────────────────
                    UIHelpers.SectionHeader("ALL PERKS", _contentRoot);
                    BuildPerkGrid(perks);
                }

                UIHelpers.AddScrollForwarders(_contentRoot);
                var crtRT = UIHelpers.RT(_contentRoot.gameObject);
                LayoutRebuilder.ForceRebuildLayoutImmediate(crtRT);
                Canvas.ForceUpdateCanvases();
                if ((object)_scrollRect != null)
                {
                    _scrollRect.verticalNormalizedPosition = savedScroll;
                    if ((object)_scrollRect.content != null)
                    {
                        Vector2 pos = _scrollRect.content.anchoredPosition;
                        pos.y = (1f - savedScroll) * Mathf.Max(0f,
                            _scrollRect.content.rect.height - ((RectTransform)_scrollRect.transform).rect.height);
                        _scrollRect.content.anchoredPosition = pos;
                    }
                }
            }
            catch (Exception ex) { MelonLogger.Error("[PerksPage] Rebuild: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "PerksPage"); }
        }

        private static void BuildActiveSummaryRow(GameModifier perk)
        {
            string label = PerkMenu.DisplayName(perk);
            var row = UIHelpers.StatRow(label, _contentRoot);

            if ((object)perk.icon != null)
            {
                var iconObj = UIHelpers.Obj("PkAIcon", row.transform);
                var iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = perk.icon;
                iconImg.type = Image.Type.Simple;
                iconImg.preserveAspect = true;
                var iconLe = iconObj.AddComponent<LayoutElement>();
                iconLe.preferredWidth = 18; iconLe.preferredHeight = 18; iconLe.flexibleWidth = 0;
                iconObj.transform.SetSiblingIndex(1);
            }

            UIHelpers.ActionBtnOrange(row.transform, "Remove", () =>
            {
                PerkMenu.Remove(perk);
                Rebuild();
            }, 60);
        }

        private const float CellW = 96f, CellH = 128f, BadgeH = 84f;

        private static void BuildPerkGrid(GameModifier[] perks)
        {
            var gridObj = UIHelpers.Obj("PerkGrid", _contentRoot);
            var glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(CellW, CellH);
            glg.spacing = new Vector2(8, 10);
            glg.padding = new RectOffset(4, 4, 4, 4);
            glg.childAlignment = TextAnchor.UpperCenter;
            var gridLe = gridObj.AddComponent<LayoutElement>();
            int perRow = 4;
            int rows = Mathf.CeilToInt(perks.Length / (float)perRow);
            gridLe.preferredHeight = rows * (CellH + glg.spacing.y);
            gridLe.flexibleWidth = 1;

            for (int i = 0; i < perks.Length; i++)
                BuildPerkTile(gridObj.transform, perks[i]);
        }

        private static void BuildPerkTile(Transform parent, GameModifier perk)
        {
            if ((object)perk == null) return;
            bool active = PerkMenu.HasPerk(perk);
            string label = PerkMenu.DisplayName(perk);

            var cell = UIHelpers.Obj("PkCell", parent);
            var cvlg = cell.AddComponent<VerticalLayoutGroup>();
            cvlg.spacing = 2;
            cvlg.childAlignment = TextAnchor.UpperCenter;
            cvlg.childForceExpandWidth = true; cvlg.childForceExpandHeight = false;

            // ── Badge (clickable) ────────────────────────────────────
            var badge = UIHelpers.Obj("PkBadge", cell.transform);
            var badgeLe = badge.AddComponent<LayoutElement>();
            badgeLe.preferredHeight = BadgeH; badgeLe.minHeight = BadgeH; badgeLe.flexibleWidth = 0;
            var badgeImg = badge.AddComponent<Image>();
            Sprite badgeSprite = PerkMenu.GetBadgeSprite(perk);
            if ((object)badgeSprite != null) { badgeImg.sprite = badgeSprite; badgeImg.type = Image.Type.Simple; }
            else badgeImg.color = new Color(0, 0, 0, 0);
            badgeImg.color = active
                ? Color.white
                : new Color(0.5f, 0.5f, 0.5f, 1f);

            var badgeBtn = badge.AddComponent<Button>();
            badgeBtn.targetGraphic = badgeImg;
            badgeBtn.onClick.AddListener(() =>
            {
                if (PerkMenu.HasPerk(perk)) PerkMenu.Remove(perk);
                else PerkMenu.Grant(perk);
                Rebuild();
            });

            if ((object)perk.icon != null)
            {
                var medallion = UIHelpers.Obj("PkMedallion", badge.transform);
                var medRT = UIHelpers.RT(medallion);
                medRT.anchorMin = new Vector2(0.50f, 0.03f); medRT.anchorMax = new Vector2(0.98f, 0.51f);
                medRT.offsetMin = Vector2.zero; medRT.offsetMax = Vector2.zero;
                var medImg = medallion.AddComponent<Image>();
                medImg.sprite = UIHelpers.RoundSprite(160, 80, Color.white);
                medImg.color = CategoryColor(perk);
                medImg.raycastTarget = false;

                var iconObj = UIHelpers.Obj("PkTileIcon", medallion.transform);
                var iconRT = UIHelpers.RT(iconObj);
                iconRT.anchorMin = new Vector2(0.02f, 0.02f); iconRT.anchorMax = new Vector2(0.98f, 0.98f);
                iconRT.offsetMin = Vector2.zero; iconRT.offsetMax = Vector2.zero;
                var iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = perk.icon;
                iconImg.type = Image.Type.Simple;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            if (active)
            {
                var ring = UIHelpers.Obj("PkActiveRing", badge.transform);
                UIHelpers.Fill(UIHelpers.RT(ring));
                var ringImg = ring.AddComponent<Image>();
                ringImg.sprite = null;
                ringImg.color = new Color(0, 0, 0, 0);
                ringImg.raycastTarget = false;
                var ringOutline = ring.AddComponent<UnityEngine.UI.Outline>();
                ringOutline.effectColor = UIHelpers.OnColor;
                ringOutline.effectDistance = new Vector2(2, 2);
            }

            // ── Label ────────────────────────────────────────────────
            var labelTxt = UIHelpers.Txt("PkLbl", cell.transform, label,
                8, FontStyle.Bold, TextAnchor.UpperCenter, active ? UIHelpers.Accent : UIHelpers.TextDim);
            labelTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            var labelLe = labelTxt.gameObject.AddComponent<LayoutElement>();
            labelLe.flexibleHeight = 1; labelLe.flexibleWidth = 0;
        }

        private static string DescribePerk(GameModifier perk)
        {
            try
            {
                Modifier[] mods = perk.modifiers;
                if (mods == null || mods.Length == 0) return null;

                var sb = new StringBuilder();
                for (int i = 0; i < mods.Length; i++)
                {
                    if ((object)mods[i] == null) continue;
                    if (sb.Length > 0) sb.Append(", ");
                    object typeObj = mods[i].modifierType;
                    string sign = mods[i].percentageValue >= 0 ? "+" : "";
                    sb.Append(typeObj).Append(" ").Append(sign).Append(mods[i].percentageValue).Append("%");
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                ModLog.Warn("[PerksPage] DescribePerk: " + ex.Message);
                return null;
            }
        }

        private static Color CategoryColor(GameModifier perk)
        {
            try
            {
                string cls = perk.modClass.ToString();
                if (string.Equals(cls, "PlayerPhysics")) return new Color(0.20f, 0.95f, 0.20f);
                if (string.Equals(cls, "LevelGeneration")) return new Color(1.00f, 0.85f, 0.00f);
                if (string.Equals(cls, "Utility")) return new Color(0.15f, 0.55f, 1.00f);
            }
            catch { }
            return new Color(0.75f, 0.75f, 0.75f);
        }

        private static void RefreshResult()
        {
            if (_resultTxt) _resultTxt.text = PerkMenu.LastResult;
        }
    }
}

