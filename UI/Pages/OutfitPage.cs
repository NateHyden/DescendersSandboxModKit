using MelonLoader;
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
        private static Text _renameHint = null;

        // Capture state before going to shed so we can return to it
        private static object _stateBeforeShed = null;

        public static void CreatePage(Transform parent)
        {
            try
            {
                var pg = UIHelpers.Obj("P11R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));

                // ScrollRect
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

                    // Name button — Panel with Image so it can be clicked
                    var nmObj = UIHelpers.Obj("NmBtn" + i, row.transform);
                    var nmImg = nmObj.AddComponent<Image>();
                    nmImg.color = new Color(0, 0, 0, 0); // transparent — just for raycast
                    var nmLe = nmObj.AddComponent<LayoutElement>();
                    nmLe.flexibleWidth = 1; nmLe.preferredHeight = UIHelpers.RowH;
                    var nmBtn = nmObj.AddComponent<Button>();
                    var nmCb = nmBtn.colors;
                    nmCb.normalColor = Color.white; nmCb.highlightedColor = UIHelpers.AccentDim;
                    nmCb.pressedColor = UIHelpers.Accent; nmCb.colorMultiplier = 1;
                    nmBtn.colors = nmCb;
                    nmBtn.onClick.AddListener(() => { StartRename(idx); });

                    // Text inside the name button
                    _nameTexts[i] = UIHelpers.Txt("NmTxt" + i, nmObj.transform,
                        OutfitPresets.GetName(i), 12, FontStyle.Bold,
                        TextAnchor.MiddleLeft, UIHelpers.TextLight);
                    UIHelpers.Fill(UIHelpers.RT(_nameTexts[i].gameObject));
                    _nameTexts[i].raycastTarget = false;

                    // Status
                    _statusTexts[i] = UIHelpers.Txt("St" + i, row.transform,
                        "EMPTY", 10, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                    _statusTexts[i].gameObject.AddComponent<LayoutElement>().preferredWidth = 48;

                    // Save
                    var svBtn = UIHelpers.Btn("SvB" + i, row.transform, "SAVE",
                        new Vector2(60, 30), 12,
                        () => { OutfitPresets.Save(idx); RefreshAll(); },
                        UIHelpers.NeonBlue, Color.black);
                    var svLe = svBtn.gameObject.AddComponent<LayoutElement>();
                    svLe.preferredWidth = 60; svLe.preferredHeight = 30;

                    // Load
                    _loadBtns[i] = UIHelpers.Btn("LdB" + i, row.transform, "LOAD",
                        new Vector2(60, 30), 12,
                        () => { OutfitPresets.Load(idx); RefreshAll(); },
                        UIHelpers.NeonBlue, Color.black);
                    var ldLe = _loadBtns[i].gameObject.AddComponent<LayoutElement>();
                    ldLe.preferredWidth = 60; ldLe.preferredHeight = 30;

                    // Delete
                    _deleteBtns[i] = UIHelpers.Btn("DlB" + i, row.transform, "DEL",
                        new Vector2(46, 30), 12,
                        () => { OutfitPresets.Delete(idx); RefreshAll(); },
                        UIHelpers.Orange, Color.black);
                    var dlLe = _deleteBtns[i].gameObject.AddComponent<LayoutElement>();
                    dlLe.preferredWidth = 46; dlLe.preferredHeight = 30;
                }

                UIHelpers.Divider(c);

                // Rename hint
                var hintRow = UIHelpers.StatRow("", c);
                _renameHint = UIHelpers.Txt("RenHint", hintRow.transform,
                    "Click a preset name to rename, then type + Enter",
                    10, FontStyle.Italic, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                _renameHint.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                UIHelpers.Divider(c);

                // Quick Actions
                UIHelpers.SectionHeader("QUICK ACTIONS", c);

                var actRow = UIHelpers.StatRow("", c);
                actRow.GetComponent<LayoutElement>().preferredHeight = 52;
                actRow.GetComponent<LayoutElement>().minHeight = 52;

                // GO TO SHED
                var shedBtn = UIHelpers.Btn("ShedBtn", actRow.transform, "GO TO SHED",
                    new Vector2(140, 44), 13,
                    () => { GoToShed(); },
                    UIHelpers.NeonBlue, Color.black);
                var shedLe = shedBtn.gameObject.AddComponent<LayoutElement>();
                shedLe.preferredWidth = 140; shedLe.preferredHeight = 44; shedLe.minHeight = 44;

                // LEAVE SHED
                var leaveBtn = UIHelpers.Btn("LeaveBtn", actRow.transform, "LEAVE SHED",
                    new Vector2(140, 44), 13,
                    () => { LeaveShed(); },
                    UIHelpers.Orange, Color.black);
                var leaveLe = leaveBtn.gameObject.AddComponent<LayoutElement>();
                leaveLe.preferredWidth = 140; leaveLe.preferredHeight = 44; leaveLe.minHeight = 44;

                // ── STAR BUTTONS (Favourites) ──────────────────────────
                Transform opHdr = c.Find("OUTFIT PRESETSH");
                if ((object)opHdr != null)
                    FavouritesManager.RegisterStarButton("OutfitPresets", UIHelpers.StarBtnAbs(opHdr, "OutfitPresets", () => FavouritesManager.Toggle("OutfitPresets")));
                Transform qaHdr = c.Find("QUICK ACTIONSH");
                if ((object)qaHdr != null)
                    FavouritesManager.RegisterStarButton("OutfitActions", UIHelpers.StarBtnAbs(qaHdr, "OutfitActions", () => FavouritesManager.Toggle("OutfitActions")));

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

                UIHelpers.AddScrollForwarders(c);
                RefreshAll();
            }
            catch (System.Exception ex) { MelonLogger.Error("OutfitPage: " + ex.Message); }
        }

        private static void GoToShed()
        {
            try
            {
                StateMachine sm = GameObject.FindObjectOfType<StateMachine>();
                if ((object)sm == null) { MelonLogger.Warning("[Page11] StateMachine not found."); return; }

                // Capture current state before going to shed
                var curStateProp = typeof(StateMachine).GetProperty("\u005EtrLeIp",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)curStateProp != null)
                    _stateBeforeShed = curStateProp.GetValue(sm, null);

                var pushState = typeof(StateMachine).GetMethod("PushState",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)pushState == null) return;
                var vtType = pushState.GetParameters()[0].ParameterType;
                pushState.Invoke(sm, new object[] { System.Enum.Parse(vtType, "Customization") });
                MelonLogger.Msg("[Page11] Going to shed. Was in: " + _stateBeforeShed);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Page11] GoToShed: " + ex.Message); }
        }

        private static void LeaveShed()
        {
            try
            {
                StateMachine sm = GameObject.FindObjectOfType<StateMachine>();
                if ((object)sm == null) { MelonLogger.Warning("[Page11] StateMachine not found."); return; }

                if (_stateBeforeShed != null)
                {
                    // Return to the state we were in before going to the shed
                    var popBackTo = typeof(StateMachine).GetMethod("PopStateBackTo",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if ((object)popBackTo != null)
                    {
                        popBackTo.Invoke(sm, new object[] { _stateBeforeShed });
                        MelonLogger.Msg("[Page11] Returning to: " + _stateBeforeShed);
                        _stateBeforeShed = null;
                        return;
                    }
                }

                // Fallback — just pop current state
                var popState = typeof(StateMachine).GetMethod("PopState",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                    null, new System.Type[0], null);
                if ((object)popState != null) popState.Invoke(sm, null);
                MelonLogger.Msg("[Page11] PopState fallback.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[Page11] LeaveShed: " + ex.Message); }
        }

        public static bool IsRenaming => _renamingSlot >= 0;

        public static void CancelRename()
        {
            if (_renamingSlot >= 0 && _nameTexts[_renamingSlot] != null)
                _nameTexts[_renamingSlot].color = UIHelpers.TextLight;
            _renamingSlot = -1;
            _renameBuffer = "";
        }

        private static void StartRename(int slot)
        {
            // Reset previous slot colour before switching
            if (_renamingSlot >= 0 && _renamingSlot != slot)
                if (_nameTexts[_renamingSlot] != null)
                    _nameTexts[_renamingSlot].color = UIHelpers.TextLight;
            _renamingSlot = slot;
            _renameBuffer = OutfitPresets.GetName(slot);
            if (_renameHint) _renameHint.text = "Type new name then press Enter to confirm  ·  Esc to cancel";
            if (_nameTexts[slot]) _nameTexts[slot].color = UIHelpers.Accent;
        }

        public static void Tick()
        {
            if (_renamingSlot < 0) return;
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
                if (_nameTexts[_renamingSlot]) _nameTexts[_renamingSlot].text = _renameBuffer;
                if (_renameHint) _renameHint.text = "Renaming: " + _renameBuffer + "  ·  Enter to confirm  Esc to cancel";
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
            if (_renamingSlot < 0 && _renameHint)
                _renameHint.text = "Click a preset name to rename, then type + Enter";
        }
    }
}