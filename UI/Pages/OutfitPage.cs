using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using UnityEngine.UI;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    public static class OutfitPage
    {
        private static Text[] _nameTexts = new Text[OutfitPresets.SlotCount];
        private static Text[] _statusTexts = new Text[OutfitPresets.SlotCount];
        private static Button[] _loadBtns = new Button[OutfitPresets.SlotCount];
        private static Button[] _deleteBtns = new Button[OutfitPresets.SlotCount];

        private static int _renamingSlot = -1;
        private static string _renameBuffer = "";
        private const float CompactRowH = 28f;
        private const float CompactBtnH = 22f;

        private static object _stateBeforeShed = null;

        public static void CreatePage(Transform parent)
        {
            try
            {
                var pg = UIHelpers.Obj("P11R", parent);
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
                crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 0);
                sr.content = crt;
                content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

                var c = content.transform;

                UIHelpers.SectionHeader("OUTFIT PRESETS", c);

                for (int i = 0; i < OutfitPresets.SlotCount; i++)
                {
                    int idx = i;

                    var row = UIHelpers.StatRow("", c);
                    SlimRow(row, CompactRowH);
                    StripEmptyStatLabel(row);

                    // Balance right-side controls so the name sits in the visual centre.
                    float sideW = 40f + 8f + 48f + 8f + 48f + 8f + 40f;
                    var leftPad = UIHelpers.Obj("NmPad" + i, row.transform);
                    leftPad.AddComponent<LayoutElement>().preferredWidth = sideW;
                    leftPad.transform.SetAsFirstSibling();

                    var nmObj = UIHelpers.Obj("NmBtn" + i, row.transform);
                    var nmImg = nmObj.AddComponent<Image>();
                    nmImg.color = new Color(0, 0, 0, 0);
                    var nmLe = nmObj.AddComponent<LayoutElement>();
                    nmLe.flexibleWidth = 1; nmLe.preferredHeight = CompactRowH;
                    var nmBtn = nmObj.AddComponent<Button>();
                    var nmCb = nmBtn.colors;
                    nmCb.normalColor = Color.white; nmCb.highlightedColor = UIHelpers.AccentDim;
                    nmCb.pressedColor = UIHelpers.Accent; nmCb.colorMultiplier = 1;
                    nmBtn.colors = nmCb;
                    nmBtn.onClick.AddListener(() => { StartRename(idx); });

                    _nameTexts[i] = UIHelpers.Txt("NmTxt" + i, nmObj.transform,
                        OutfitPresets.GetName(i), 11, FontStyle.Bold,
                        TextAnchor.MiddleCenter, UIHelpers.TextLight);
                    UIHelpers.Fill(UIHelpers.RT(_nameTexts[i].gameObject));
                    _nameTexts[i].raycastTarget = false;

                    _statusTexts[i] = UIHelpers.Txt("St" + i, row.transform,
                        "EMPTY", 9, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                    _statusTexts[i].gameObject.AddComponent<LayoutElement>().preferredWidth = 40;

                    var svBtn = UIHelpers.Btn("SvB" + i, row.transform, "SAVE",
                        new Vector2(48, CompactBtnH), 11,
                        () => { OutfitPresets.Save(idx); RefreshAll(); },
                        UIHelpers.NeonBlue, Color.black);
                    var svLe = svBtn.gameObject.AddComponent<LayoutElement>();
                    svLe.preferredWidth = 48; svLe.preferredHeight = CompactBtnH;
                    svLe.minHeight = CompactBtnH;

                    _loadBtns[i] = UIHelpers.Btn("LdB" + i, row.transform, "LOAD",
                        new Vector2(48, CompactBtnH), 11,
                        () => { OutfitPresets.Load(idx); RefreshAll(); },
                        UIHelpers.NeonBlue, Color.black);
                    var ldLe = _loadBtns[i].gameObject.AddComponent<LayoutElement>();
                    ldLe.preferredWidth = 48; ldLe.preferredHeight = CompactBtnH;
                    ldLe.minHeight = CompactBtnH;

                    _deleteBtns[i] = UIHelpers.Btn("DlB" + i, row.transform, "DEL",
                        new Vector2(40, CompactBtnH), 11,
                        () => { OutfitPresets.Delete(idx); RefreshAll(); },
                        UIHelpers.Orange, Color.black);
                    var dlLe = _deleteBtns[i].gameObject.AddComponent<LayoutElement>();
                    dlLe.preferredWidth = 40; dlLe.preferredHeight = CompactBtnH;
                    dlLe.minHeight = CompactBtnH;
                }

                UIHelpers.Divider(c);

                UIHelpers.InfoBox(c, "Click a preset name to rename, then type + Enter", Color.white);

                UIHelpers.Divider(c);

                UIHelpers.SectionHeader("QUICK ACTIONS", c);

                var actRow = UIHelpers.StatRow("", c);
                SlimRow(actRow, CompactRowH);
                StripEmptyStatLabel(actRow);

                var shedBtn = UIHelpers.Btn("ShedBtn", actRow.transform, "GO TO SHED",
                    new Vector2(110, CompactBtnH), 11,
                    () => { GoToShed(); },
                    UIHelpers.NeonBlue, Color.black);
                var shedLe = shedBtn.gameObject.AddComponent<LayoutElement>();
                shedLe.preferredWidth = 110; shedLe.preferredHeight = CompactBtnH;
                shedLe.minHeight = CompactBtnH;

                var leaveBtn = UIHelpers.Btn("LeaveBtn", actRow.transform, "LEAVE SHED",
                    new Vector2(110, CompactBtnH), 11,
                    () => { LeaveShed(); },
                    UIHelpers.Orange, Color.black);
                var leaveLe = leaveBtn.gameObject.AddComponent<LayoutElement>();
                leaveLe.preferredWidth = 110; leaveLe.preferredHeight = CompactBtnH;
                leaveLe.minHeight = CompactBtnH;

                UIHelpers.Divider(c);

                UIHelpers.SectionHeader("RIDER CUSTOMISATION", c);
                UIHelpers.InfoBox(c, "Cycle through looks until you find the one you want. The game won't show what's currently equipped.");

                Text skinColorVal = null, hairColorVal = null, hairTypeVal = null,
                     beardColorVal = null, beardTypeVal = null, bodyTypeVal = null;

                skinColorVal = BuildRiderStepper(c, "Skin Colour", RiderCustomiser.SkinColorLevel,
                    () => { RiderCustomiser.DecreaseSkinColor(); if (skinColorVal) skinColorVal.text = RiderCustomiser.SkinColorLevel.ToString(); },
                    () => { RiderCustomiser.IncreaseSkinColor(); if (skinColorVal) skinColorVal.text = RiderCustomiser.SkinColorLevel.ToString(); });
                hairColorVal = BuildRiderStepper(c, "Hair Colour", RiderCustomiser.HairColorLevel,
                    () => { RiderCustomiser.DecreaseHairColor(); if (hairColorVal) hairColorVal.text = RiderCustomiser.HairColorLevel.ToString(); },
                    () => { RiderCustomiser.IncreaseHairColor(); if (hairColorVal) hairColorVal.text = RiderCustomiser.HairColorLevel.ToString(); });
                hairTypeVal = BuildRiderStepper(c, "Hair Type", RiderCustomiser.HairTypeLevel,
                    () => { RiderCustomiser.DecreaseHairType(); if (hairTypeVal) hairTypeVal.text = RiderCustomiser.HairTypeLevel.ToString(); },
                    () => { RiderCustomiser.IncreaseHairType(); if (hairTypeVal) hairTypeVal.text = RiderCustomiser.HairTypeLevel.ToString(); });
                beardColorVal = BuildRiderStepper(c, "Beard Colour", RiderCustomiser.BeardColorLevel,
                    () => { RiderCustomiser.DecreaseBeardColor(); if (beardColorVal) beardColorVal.text = RiderCustomiser.BeardColorLevel.ToString(); },
                    () => { RiderCustomiser.IncreaseBeardColor(); if (beardColorVal) beardColorVal.text = RiderCustomiser.BeardColorLevel.ToString(); });
                beardTypeVal = BuildRiderStepper(c, "Beard Type", RiderCustomiser.BeardTypeLevel,
                    () => { RiderCustomiser.DecreaseBeardType(); if (beardTypeVal) beardTypeVal.text = RiderCustomiser.BeardTypeLevel.ToString(); },
                    () => { RiderCustomiser.IncreaseBeardType(); if (beardTypeVal) beardTypeVal.text = RiderCustomiser.BeardTypeLevel.ToString(); });
                bodyTypeVal = BuildRiderStepper(c, "Body Type", RiderCustomiser.BodyTypeLevel,
                    () => { RiderCustomiser.DecreaseBodyType(); if (bodyTypeVal) bodyTypeVal.text = RiderCustomiser.BodyTypeLevel.ToString(); },
                    () => { RiderCustomiser.IncreaseBodyType(); if (bodyTypeVal) bodyTypeVal.text = RiderCustomiser.BodyTypeLevel.ToString(); });

                var riderResetRow = UIHelpers.StatRow("", c);
                SlimRow(riderResetRow, CompactRowH);
                UIHelpers.ActionBtnOrange(riderResetRow.transform, "Reset All", () =>
                {
                    RiderCustomiser.ResetAll();
                    if (skinColorVal) skinColorVal.text = RiderCustomiser.SkinColorLevel.ToString();
                    if (hairColorVal) hairColorVal.text = RiderCustomiser.HairColorLevel.ToString();
                    if (hairTypeVal) hairTypeVal.text = RiderCustomiser.HairTypeLevel.ToString();
                    if (beardColorVal) beardColorVal.text = RiderCustomiser.BeardColorLevel.ToString();
                    if (beardTypeVal) beardTypeVal.text = RiderCustomiser.BeardTypeLevel.ToString();
                    if (bodyTypeVal) bodyTypeVal.text = RiderCustomiser.BodyTypeLevel.ToString();
                }, 100);

                Transform opHdr = c.Find("OUTFIT PRESETSH");
                if ((object)opHdr != null)
                    FavouritesManager.RegisterStarButton("OutfitPresets", UIHelpers.StarBtnAbs(opHdr, "OutfitPresets", () => FavouritesManager.Toggle("OutfitPresets")));
                Transform qaHdr = c.Find("QUICK ACTIONSH");
                if ((object)qaHdr != null)
                    FavouritesManager.RegisterStarButton("OutfitActions", UIHelpers.StarBtnAbs(qaHdr, "OutfitActions", () => FavouritesManager.Toggle("OutfitActions")));
                Transform rcHdr = c.Find("RIDER CUSTOMISATIONH");
                if ((object)rcHdr != null)
                    FavouritesManager.RegisterStarButton("RiderCustomisation", UIHelpers.StarBtnAbs(rcHdr, "RiderCustomisation", () => FavouritesManager.Toggle("RiderCustomisation")));

                FavouritesManager.Register(new ModFavEntry {
                    Id = "OutfitPresets", DisplayName = "Outfit Presets", TabBadge = "OUTFIT",
                    BuildControls = (p) => {
                        for (int s = 0; s < OutfitPresets.SlotCount; s++)
                        {
                            int idx = s;
                            var row = UIHelpers.StatRow(OutfitPresets.GetName(s), p);
                            UIHelpers.ActionBtn(row.transform, "SAVE", () => { OutfitPresets.Save(idx); }, 50);
                            UIHelpers.ActionBtn(row.transform, "LOAD", () => { OutfitPresets.Load(idx); }, 50);
                        }
                    },
                    IsActive = () => false
                });
                FavouritesManager.Register(new ModFavEntry {
                    Id = "OutfitActions", DisplayName = "Shed Actions", TabBadge = "OUTFIT",
                    BuildControls = (p) => {
                        var row = UIHelpers.StatRow("Shed", p);
                        UIHelpers.ActionBtn(row.transform, "Go To Shed", () => GoToShed(), 80);
                        UIHelpers.ActionBtnOrange(row.transform, "Leave Shed", () => LeaveShed(), 80);
                    },
                    IsActive = () => false
                });
                FavouritesManager.Register(new ModFavEntry {
                    Id = "RiderCustomisation", DisplayName = "Rider Customisation", TabBadge = "OUTFIT",
                    BuildControls = (p) => {
                        Text fSkin = null, fHairC = null, fHairT = null, fBeardC = null, fBeardT = null, fBody = null;
                        fSkin = BuildRiderStepper(p, "Skin Colour", RiderCustomiser.SkinColorLevel,
                            () => { RiderCustomiser.DecreaseSkinColor(); if (fSkin) fSkin.text = RiderCustomiser.SkinColorLevel.ToString(); },
                            () => { RiderCustomiser.IncreaseSkinColor(); if (fSkin) fSkin.text = RiderCustomiser.SkinColorLevel.ToString(); });
                        fHairC = BuildRiderStepper(p, "Hair Colour", RiderCustomiser.HairColorLevel,
                            () => { RiderCustomiser.DecreaseHairColor(); if (fHairC) fHairC.text = RiderCustomiser.HairColorLevel.ToString(); },
                            () => { RiderCustomiser.IncreaseHairColor(); if (fHairC) fHairC.text = RiderCustomiser.HairColorLevel.ToString(); });
                        fHairT = BuildRiderStepper(p, "Hair Type", RiderCustomiser.HairTypeLevel,
                            () => { RiderCustomiser.DecreaseHairType(); if (fHairT) fHairT.text = RiderCustomiser.HairTypeLevel.ToString(); },
                            () => { RiderCustomiser.IncreaseHairType(); if (fHairT) fHairT.text = RiderCustomiser.HairTypeLevel.ToString(); });
                        fBeardC = BuildRiderStepper(p, "Beard Colour", RiderCustomiser.BeardColorLevel,
                            () => { RiderCustomiser.DecreaseBeardColor(); if (fBeardC) fBeardC.text = RiderCustomiser.BeardColorLevel.ToString(); },
                            () => { RiderCustomiser.IncreaseBeardColor(); if (fBeardC) fBeardC.text = RiderCustomiser.BeardColorLevel.ToString(); });
                        fBeardT = BuildRiderStepper(p, "Beard Type", RiderCustomiser.BeardTypeLevel,
                            () => { RiderCustomiser.DecreaseBeardType(); if (fBeardT) fBeardT.text = RiderCustomiser.BeardTypeLevel.ToString(); },
                            () => { RiderCustomiser.IncreaseBeardType(); if (fBeardT) fBeardT.text = RiderCustomiser.BeardTypeLevel.ToString(); });
                        fBody = BuildRiderStepper(p, "Body Type", RiderCustomiser.BodyTypeLevel,
                            () => { RiderCustomiser.DecreaseBodyType(); if (fBody) fBody.text = RiderCustomiser.BodyTypeLevel.ToString(); },
                            () => { RiderCustomiser.IncreaseBodyType(); if (fBody) fBody.text = RiderCustomiser.BodyTypeLevel.ToString(); });
                        var row = UIHelpers.StatRow("", p);
                        UIHelpers.ActionBtnOrange(row.transform, "Reset All", () =>
                        {
                            RiderCustomiser.ResetAll();
                            if (fSkin) fSkin.text = RiderCustomiser.SkinColorLevel.ToString();
                            if (fHairC) fHairC.text = RiderCustomiser.HairColorLevel.ToString();
                            if (fHairT) fHairT.text = RiderCustomiser.HairTypeLevel.ToString();
                            if (fBeardC) fBeardC.text = RiderCustomiser.BeardColorLevel.ToString();
                            if (fBeardT) fBeardT.text = RiderCustomiser.BeardTypeLevel.ToString();
                            if (fBody) fBody.text = RiderCustomiser.BodyTypeLevel.ToString();
                        }, 100);
                    },
                    IsActive = () => false
                });

                UIHelpers.AddScrollForwarders(c);
                RefreshAll();
            }
            catch (System.Exception ex) { MelonLogger.Error("OutfitPage: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "OutfitPage"); }
        }

        private static Text BuildRiderStepper(Transform parent, string label,
            int currentLevel, UnityEngine.Events.UnityAction onMinus, UnityEngine.Events.UnityAction onPlus)
        {
            var row = UIHelpers.StatRow(label, parent);
            SlimRow(row, CompactRowH);
            UIHelpers.SmallBtn(row.transform, "\u25C0", onMinus);
            var val = UIHelpers.Txt("RCV_" + label, row.transform, currentLevel.ToString(), 12,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            val.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            UIHelpers.SmallBtn(row.transform, "\u25B6", onPlus);
            return val;
        }

        private static void SlimRow(GameObject row, float h)
        {
            if ((object)row == null) return;
            LayoutElement le = row.GetComponent<LayoutElement>();
            if ((object)le == null) return;
            le.preferredHeight = h;
            le.minHeight = h;
        }

        public static void GoToShed()
        {
            try
            {
                StateMachine sm = GameObject.FindObjectOfType<StateMachine>();
                if ((object)sm == null) { ModLog.Warn("[Page11] StateMachine not found."); return; }

                var curStateProp = typeof(StateMachine).GetProperty("\u005EtrLeIp",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)curStateProp != null)
                    _stateBeforeShed = curStateProp.GetValue(sm, null);

                var pushState = typeof(StateMachine).GetMethod("PushState",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)pushState == null) return;
                var vtType = pushState.GetParameters()[0].ParameterType;
                pushState.Invoke(sm, new object[] { System.Enum.Parse(vtType, "Customization") });
                ModLog.Debug("[Page11] Going to shed. Was in: " + _stateBeforeShed);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Page11] GoToShed: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "OutfitPage"); }
        }

        public static void LeaveShed()
        {
            try
            {
                StateMachine sm = GameObject.FindObjectOfType<StateMachine>();
                if ((object)sm == null) { ModLog.Warn("[Page11] StateMachine not found."); return; }

                if (_stateBeforeShed != null)
                {
                    var popBackTo = typeof(StateMachine).GetMethod("PopStateBackTo",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if ((object)popBackTo != null)
                    {
                        popBackTo.Invoke(sm, new object[] { _stateBeforeShed });
                        ModLog.Debug("[Page11] Returning to: " + _stateBeforeShed);
                        _stateBeforeShed = null;
                        return;
                    }
                }

                var popState = typeof(StateMachine).GetMethod("PopState",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                    null, new System.Type[0], null);
                if ((object)popState != null) popState.Invoke(sm, null);
                ModLog.Debug("[Page11] PopState fallback.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[Page11] LeaveShed: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "OutfitPage"); }
        }

        public static bool IsRenaming => _renamingSlot >= 0;

        public static void CancelRename()
        {
            if (_renamingSlot >= 0 && _nameTexts[_renamingSlot] != null)
                _nameTexts[_renamingSlot].color = UIHelpers.TextLight;
            _renamingSlot = -1;
            _renameBuffer = "";
        }

        private static void StripEmptyStatLabel(GameObject row)
        {
            if ((object)row == null) return;
            Transform t = row.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform ch = t.GetChild(i);
                if ((object)ch == null) continue;
                Text txt = ch.GetComponent<Text>();
                if ((object)txt == null) continue;
                if (!string.IsNullOrEmpty(txt.text)) continue;
                LayoutElement le = ch.GetComponent<LayoutElement>();
                if ((object)le != null && le.flexibleWidth > 0f)
                {
                    UnityEngine.Object.DestroyImmediate(ch.gameObject);
                    return;
                }
            }
        }

        private static void StartRename(int slot)
        {
            if (_renamingSlot >= 0 && _renamingSlot != slot)
                if (_nameTexts[_renamingSlot] != null)
                    _nameTexts[_renamingSlot].color = UIHelpers.TextLight;
            _renamingSlot = slot;
            _renameBuffer = OutfitPresets.GetName(slot);
            if (_nameTexts[slot]) _nameTexts[slot].color = UIHelpers.Accent;
        }

        public static void Tick()
        {
            if (_renamingSlot >= 0) { TickRename(); return; }
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
                    if (_renameBuffer.Length == 0) _renameBuffer = "Preset " + (_renamingSlot + 1);
                    OutfitPresets.SetName(_renamingSlot, _renameBuffer);
                    _renamingSlot = -1; _renameBuffer = "";
                    RefreshAll(); return;
                }
                else if (ch == '\x1b')
                {
                    _renamingSlot = -1; _renameBuffer = "";
                    RefreshAll(); return;
                }
                else if (_renameBuffer.Length < 24) _renameBuffer += ch;
            }

            if (_renamingSlot >= 0)
            {
                if (_nameTexts[_renamingSlot])
                    _nameTexts[_renamingSlot].text = UIHelpers.WithCaret(_renameBuffer, true);
            }
        }

        public static void RefreshAll()
        {
            for (int i = 0; i < OutfitPresets.SlotCount; i++)
            {
                bool has = OutfitPresets.HasPreset(i);
                if (_statusTexts[i])
                {
                    _statusTexts[i].text = has ? "SAVED" : "EMPTY";
                    _statusTexts[i].color = has ? UIHelpers.OnColor : UIHelpers.OffColor;
                }
                if (_nameTexts[i] && _renamingSlot != i)
                {
                    _nameTexts[i].text = OutfitPresets.GetName(i);
                    _nameTexts[i].color = UIHelpers.TextLight;
                }
                if ((object)_loadBtns[i] != null) _loadBtns[i].interactable = has;
                if ((object)_deleteBtns[i] != null) _deleteBtns[i].interactable = has;
            }
        }
    }
}

