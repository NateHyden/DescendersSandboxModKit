using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    public static class GhostPage
    {
        private static Image _enableTrack; private static RectTransform _enableKnob;
        private static Text _statusText = null;
        private static Text _recTimeText = null;
        private static Text _savedTimeText = null;
        private static GameObject _savedPanel = null;

        public static void CreatePage(Transform parent)
        {
            try
            {
                var pg = UIHelpers.Obj("P14R", parent);
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

                var c = content.transform;

                // ── HEADER ────────────────────────────────────────────
                UIHelpers.SectionHeader("GHOST REPLAY", c);

                // Enable toggle
                var enableRow = UIHelpers.StatRow("Enable  (F3)", c);
                UIHelpers.Toggle(enableRow.transform, "GhostEnable",
                    () => { GhostReplay.Toggle(); RefreshAll(); },
                    out _enableTrack, out _enableKnob);

                UIHelpers.Divider(c);

                // ── HOW TO USE ────────────────────────────────────────
                UIHelpers.SectionHeader("HOW TO USE", c);

                AddInstruction(c, "1", "Double click RS to enable Ghost Replay");
                AddInstruction(c, "2", "Ride to where you want your run to start");
                AddInstruction(c, "3", "Set Your Spawn Point - Then Click Left Stick");
                AddInstruction(c, "4", "Move — recording begins automatically");
                AddInstruction(c, "5", "Ride your run — first reset auto-saves it as the ghost");
                AddInstruction(c, "6", "Click RS to manually save a better run as the ghost");
                AddInstruction(c, "7", "Reset with B — ghost rides your saved run alongside you");

                UIHelpers.Divider(c);

                // ── STATUS ────────────────────────────────────────────
                UIHelpers.SectionHeader("STATUS", c);

                var stateRow = UIHelpers.StatRow("State", c);
                _statusText = UIHelpers.Txt("GhSt", stateRow.transform,
                    "OFF", 11, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.OffColor);
                _statusText.gameObject.AddComponent<LayoutElement>().preferredWidth = 140;

                var recRow = UIHelpers.StatRow("Current Run", c);
                _recTimeText = UIHelpers.Txt("GhRt", recRow.transform,
                    "0:00", 11, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
                _recTimeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;

                // Saved run panel — shows when a run is saved
                _savedPanel = UIHelpers.Obj("SavedPanel", c);
                var spVlg = _savedPanel.AddComponent<VerticalLayoutGroup>();
                spVlg.spacing = UIHelpers.RowGap;
                spVlg.childForceExpandWidth = true; spVlg.childForceExpandHeight = false;
                _savedPanel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                UIHelpers.SectionHeader("SAVED GHOST RUN", _savedPanel.transform);

                var savedRow = UIHelpers.StatRow("Ghost Run Length", _savedPanel.transform);
                _savedTimeText = UIHelpers.Txt("GhSv", savedRow.transform,
                    "--:--", 11, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.OnColor);
                _savedTimeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;

                UIHelpers.Divider(c);

                // ── CONTROLS ─────────────────────────────────────────
                UIHelpers.SectionHeader("CONTROLS", c);

                AddKeyHint(c, "F3 / RS×2", "Toggle Ghost Replay on / off");
                AddKeyHint(c, "LS click", "Record Run");
                AddKeyHint(c, "F4 / RS×1", "Save current run as ghost");
                AddKeyHint(c, "B", "Reset to spawn");

                UIHelpers.Divider(c);

                // Clear button
                var clearRow = UIHelpers.StatRow("", c);
                var clearBtn = UIHelpers.Btn("ClrBtn", clearRow.transform, "CLEAR SAVED RUN",
                    new Vector2(160, 32), 12,
                    () => { GhostReplay.ClearSavedRun(); RefreshAll(); },
                    UIHelpers.Orange, Color.black);
                var clrLe = clearBtn.gameObject.AddComponent<LayoutElement>();
                clrLe.preferredWidth = 160; clrLe.minWidth = 160;
                clrLe.preferredHeight = 32; clrLe.minHeight = 32;

                // ── STAR BUTTON (Favourites) ──────────────────────────
                FavouritesManager.RegisterStarButton("GhostReplay", UIHelpers.StarBtn(enableRow.transform, "GhostReplay", () => FavouritesManager.Toggle("GhostReplay")));
                FavouritesManager.Register(new ModFavEntry {
                    Id = "GhostReplay", DisplayName = "Ghost Replay", TabBadge = "TOOLS",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "GhostReplay", "Ghost Replay",
                        () => GhostReplay.Enabled, () => GhostReplay.Toggle(), () => RefreshAll()),
                    IsActive = () => GhostReplay.Enabled
                });

                UIHelpers.AddScrollForwarders(c);
                RefreshAll();
            }
            catch (System.Exception ex) { MelonLogger.Error("GhostPage: " + ex.Message); }
        }

        private static void AddInstruction(Transform parent, string num, string text)
        {
            var row = UIHelpers.Obj("Instr" + num, parent);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8; hlg.padding = new RectOffset(4, 4, 2, 2);
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            row.AddComponent<LayoutElement>().minHeight = 22;

            var numTxt = UIHelpers.Txt("N", row.transform, num + ".",
                11, FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.Accent);
            numTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;

            var bodyTxt = UIHelpers.Txt("T", row.transform, text,
                10, FontStyle.Normal, TextAnchor.MiddleLeft, UIHelpers.TextMid);
            var bLe = bodyTxt.gameObject.AddComponent<LayoutElement>();
            bLe.flexibleWidth = 1;
            bodyTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private static void AddKeyHint(Transform parent, string key, string desc)
        {
            var row = UIHelpers.StatRow(desc, parent);
            var keyTxt = UIHelpers.Txt("K", row.transform, key,
                11, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.NeonBlue);
            keyTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 30;
        }

        public static void Tick()
        {
            if ((object)_statusText == null) return;

            string label = GhostReplay.GetStateLabel();
            Color col = UIHelpers.OffColor;
            if (GhostReplay.Enabled)
            {
                if (label == "RECORDING") col = UIHelpers.Orange;
                else if (label == "STEP 2: WAITING FOR MOVE") col = UIHelpers.Accent;
                else if (label == "STEP 1: RIDE TO START") col = UIHelpers.NeonBlue;
            }
            _statusText.text = label;
            _statusText.color = col;

            if (_recTimeText)
                _recTimeText.text = GhostReplay.IsRecording ? FormatTime(GhostReplay.RunTime) : "0:00";

            if (_savedTimeText)
                _savedTimeText.text = GhostReplay.HasSavedRun
                    ? FormatTime(GhostReplay.SavedRunTime) : "--:--";

            if (_savedPanel)
                _savedPanel.SetActive(GhostReplay.HasSavedRun);
        }

        private static string FormatTime(float t)
        {
            int m = (int)(t / 60f);
            int s = (int)(t % 60f);
            int ms = (int)((t % 1f) * 10f);
            return m + ":" + s.ToString("D2") + "." + ms;
        }

        public static void RefreshAll()
        {
            UIHelpers.SetToggle(_enableTrack, _enableKnob, GhostReplay.Enabled);
        }
    }
}