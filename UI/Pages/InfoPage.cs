using DescendersModMenu.Mods;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class InfoPage
    {
        // ── Sub-tab state ─────────────────────────────────────────────
        private static int _activeTab = 0; // 0=System 1=Hotkeys 2=Customise

        private static readonly string[] TabLabels = { "System", "Hotkeys", "Customise" };

        // Sub-tab bar buttons
        private static Image[] _tabBgs = new Image[3];
        private static Text[] _tabTxts = new Text[3];

        // Page root GameObjects
        private static GameObject _pgSystem;
        private static GameObject _pgHotkeys;
        private static GameObject _pgCustomise;

        // Customise tab refs
        private static Text _custPosLbl;
        private static Text _custScaleLbl;
        private static Text _custOpacityLbl;
        private static GameObject _custSavedRow;
        // System tab
        private static Text _unityVersionTxt;
        private static Text _steamPlayerTxt;
        private static Text _unityMatchTxt;
        private static Text _mlVersionTxt;

        // ── CreatePage ────────────────────────────────────────────────
        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                // Root — fills the content slot
                pg = UIHelpers.Obj("P3R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));
                var rootVlg = pg.AddComponent<VerticalLayoutGroup>();
                rootVlg.spacing = 0;
                rootVlg.padding = new RectOffset(0, 0, 0, 0);
                rootVlg.childAlignment = TextAnchor.UpperLeft;
                rootVlg.childForceExpandWidth = true;
                rootVlg.childForceExpandHeight = false;

                // ── Sub-tab bar ───────────────────────────────────────
                var tabBar = UIHelpers.Obj("TabBar", pg.transform);
                tabBar.AddComponent<Image>().color = UIHelpers.WinOuter;
                var tbLE = tabBar.AddComponent<LayoutElement>();
                tbLE.preferredHeight = 38; tbLE.minHeight = 38; tbLE.flexibleHeight = 0;
                var tbHlg = tabBar.AddComponent<HorizontalLayoutGroup>();
                tbHlg.spacing = 1;
                tbHlg.padding = new RectOffset(8, 8, 0, 0);
                tbHlg.childAlignment = TextAnchor.LowerLeft;
                tbHlg.childForceExpandWidth = false;
                tbHlg.childForceExpandHeight = false;

                for (int i = 0; i < TabLabels.Length; i++)
                {
                    int idx = i;
                    var tab = UIHelpers.Obj("Tab" + i, tabBar.transform);
                    var tabImg = tab.AddComponent<Image>();
                    tabImg.color = new Color(0, 0, 0, 0);
                    _tabBgs[i] = tabImg;
                    var tabLE = tab.AddComponent<LayoutElement>();
                    tabLE.preferredHeight = 30; tabLE.minHeight = 30;
                    tabLE.flexibleHeight = 0; tabLE.flexibleWidth = 0;

                    var tabHlg = tab.AddComponent<HorizontalLayoutGroup>();
                    tabHlg.padding = new RectOffset(12, 12, 0, 0);
                    tabHlg.childAlignment = TextAnchor.MiddleCenter;
                    tabHlg.childForceExpandWidth = false;
                    tabHlg.childForceExpandHeight = true;

                    var tabTxt = UIHelpers.Txt("T" + i, tab.transform, TabLabels[i], 11,
                        FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextDim);
                    _tabTxts[i] = tabTxt;

                    var btn = tab.AddComponent<Button>();
                    btn.targetGraphic = tabImg;
                    var bc = btn.colors;
                    bc.normalColor = Color.white;
                    bc.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
                    bc.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                    bc.colorMultiplier = 1; btn.colors = bc;
                    btn.onClick.AddListener(() => SwitchTab(idx));
                }

                // ── Content area ──────────────────────────────────────
                var contentArea = UIHelpers.Obj("Content", pg.transform);
                var caLE = contentArea.AddComponent<LayoutElement>();
                caLE.flexibleHeight = 1; caLE.flexibleWidth = 1;
                UIHelpers.Fill(UIHelpers.RT(contentArea));

                // ── System page ───────────────────────────────────────
                _pgSystem = UIHelpers.Obj("PgSystem", contentArea.transform);
                UIHelpers.Fill(UIHelpers.RT(_pgSystem));
                BuildSystemPage(_pgSystem.transform);

                // ── Hotkeys page ──────────────────────────────────────
                _pgHotkeys = UIHelpers.Obj("PgHotkeys", contentArea.transform);
                UIHelpers.Fill(UIHelpers.RT(_pgHotkeys));
                BuildHotkeysPage(_pgHotkeys.transform);

                // ── Credits page ──────────────────────────────────────
                // ── Customise page ────────────────────────────────────
                _pgCustomise = UIHelpers.Obj("PgCustomise", contentArea.transform);
                UIHelpers.Fill(UIHelpers.RT(_pgCustomise));
                BuildCustomisePage(_pgCustomise.transform);
                SwitchTab(0);
                Refresh();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("InfoPage.CreatePage: " + ex.Message);
                return null;
            }
            return pg;
        }

        // ── Tab switching ─────────────────────────────────────────────
        private static void SwitchTab(int idx)
        {
            _activeTab = idx;
            if ((object)_pgSystem != null) _pgSystem.SetActive(idx == 0);
            if ((object)_pgHotkeys != null) _pgHotkeys.SetActive(idx == 1);
            if ((object)_pgCustomise != null) _pgCustomise.SetActive(idx == 2);

            for (int i = 0; i < TabLabels.Length; i++)
            {
                bool active = i == idx;
                if ((object)_tabBgs[i] != null)
                    _tabBgs[i].color = active ? UIHelpers.RowBg : new Color(0, 0, 0, 0);
                if ((object)_tabTxts[i] != null)
                    _tabTxts[i].color = active ? UIHelpers.Accent : UIHelpers.TextDim;
            }

            // Refresh status data when switching to its tab
        }

        // ── System page ───────────────────────────────────────────────
        private static void BuildSystemPage(Transform p)
        {
            var vlg = UIHelpers.Obj("SysVlg", p);
            UIHelpers.Fill(UIHelpers.RT(vlg));
            var v = vlg.AddComponent<VerticalLayoutGroup>();
            v.spacing = UIHelpers.RowGap;
            v.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            UIHelpers.SectionHeader("ENGINE", vlg.transform);
            _unityVersionTxt = MakeInfoRow("Unity Version", vlg.transform);
            _unityMatchTxt = MakeInfoRow("Version Match", vlg.transform);
            _mlVersionTxt = MakeInfoRow("MelonLoader", vlg.transform);
            MakeInfoRow2("Scripting Backend", "Mono", vlg.transform);
            MakeInfoRow2("Build Target", ".NET 4.7.2", vlg.transform);

            UIHelpers.Divider(vlg.transform);
            UIHelpers.SectionHeader("SANDBOX", vlg.transform);
            MakeInfoRow2("Version", BuildInfo.Version, vlg.transform, UIHelpers.Accent);
            MakeInfoRow2("Output DLL", "DescendersSandbox.dll", vlg.transform);
            MakeInfoRow2("Author", "NateHyden", vlg.transform);

            UIHelpers.Divider(vlg.transform);
            UIHelpers.SectionHeader("COMMUNITY", vlg.transform);
            _steamPlayerTxt = MakeInfoRow("Steam Players Online", vlg.transform);

            UIHelpers.Divider(vlg.transform);
            UIHelpers.SectionHeader("DIAGNOSTICS", vlg.transform);
            var dumpRow = UIHelpers.StatRow("Scene Dump", vlg.transform);
            UIHelpers.ActionBtn(dumpRow.transform, "Dump Now", () =>
            {
                SceneDumper.DumpCurrentScene();
            }, 90);
            UIHelpers.InfoBox(vlg.transform, "Writes forensic dump files next to the game folder. Same as pressing # in-game - use this if that hotkey doesn't register on your setup.");
        }

        // ── Hotkeys page ──────────────────────────────────────────────
        private static void BuildHotkeysPage(Transform p)
        {
            var vlg = UIHelpers.Obj("HkVlg", p);
            UIHelpers.Fill(UIHelpers.RT(vlg));
            var v = vlg.AddComponent<VerticalLayoutGroup>();
            v.spacing = UIHelpers.RowGap;
            v.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            UIHelpers.SectionHeader("MENU", vlg.transform);
            UIHelpers.HotkeyRow(vlg.transform, "Toggle mod menu", "F6");

            UIHelpers.SectionHeader("GAMEPLAY", vlg.transform);
            UIHelpers.HotkeyRow(vlg.transform, "Toggle slow motion", "F2");
            UIHelpers.HotkeyRow(vlg.transform, "Ghost Replay — toggle", "F3 / RS Dbl Click");
            UIHelpers.HotkeyRow(vlg.transform, "Ghost Replay — save run", "F4 / RS Click");
            UIHelpers.HotkeyRow(vlg.transform, "Ghost Replay — set spawn", "LS Click");

        }

        // ── Credits page ──────────────────────────────────────────────

        // ── Customise page ────────────────────────────────────────────
        // ── Scanner page ──────────────────────────────────────────────

        private static void BuildCustomisePage(Transform p)
        {
            var vlg = UIHelpers.Obj("CustVlg", p);
            UIHelpers.Fill(UIHelpers.RT(vlg));
            var v = vlg.AddComponent<VerticalLayoutGroup>();
            v.spacing = UIHelpers.RowGap;
            v.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            var c = vlg.transform;

            // ── How it works ──────────────────────────────────────────
            UIHelpers.SectionHeader("HOW IT WORKS", c);
            UIHelpers.InfoBox(c,
                "Your layout saves automatically whenever you make a change.");
            UIHelpers.InfoBox(c,
                "It loads back every time you launch the game — no manual save needed.");
            UIHelpers.InfoBox(c,
                "Use Reset to go back to the default position, scale and opacity.");

            UIHelpers.Divider(c);

            // ── Position ──────────────────────────────────────────────
            UIHelpers.SectionHeader("POSITION", c);

            var posRow = UIHelpers.StatRow("Position", c);
            UIHelpers.ActionBtn(posRow.transform, "Centre",
                () => { Mods.MenuCustomiser.SetPosition(0); RefreshCustomise(); }, 52);
            UIHelpers.ActionBtn(posRow.transform, "Top Left",
                () => { Mods.MenuCustomiser.SetPosition(1); RefreshCustomise(); }, 58);
            UIHelpers.ActionBtn(posRow.transform, "Top Right",
                () => { Mods.MenuCustomiser.SetPosition(2); RefreshCustomise(); }, 60);
            _custPosLbl = UIHelpers.Txt("CustPosV", posRow.transform,
                Mods.MenuCustomiser.PositionLabels[Mods.MenuCustomiser.PositionPreset],
                11, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
            _custPosLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;

            UIHelpers.Divider(c);

            // ── Scale ─────────────────────────────────────────────────
            UIHelpers.SectionHeader("SCALE", c);

            var scaleRow = UIHelpers.StatRow("Scale", c);
            UIHelpers.SmallBtn(scaleRow.transform, "\u25C0",
                () => { Mods.MenuCustomiser.PrevScale(); RefreshCustomise(); });
            _custScaleLbl = UIHelpers.Txt("CustScaleV", scaleRow.transform,
                Mods.MenuCustomiser.ScaleDisplay,
                12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            _custScaleLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 52;
            UIHelpers.SmallBtn(scaleRow.transform, "\u25B6",
                () => { Mods.MenuCustomiser.NextScale(); RefreshCustomise(); });

            UIHelpers.Divider(c);

            // ── Opacity ───────────────────────────────────────────────
            UIHelpers.SectionHeader("OPACITY", c);

            var opacityRow = UIHelpers.StatRow("Opacity", c);
            UIHelpers.SmallBtn(opacityRow.transform, "\u25C0",
                () => { Mods.MenuCustomiser.PrevOpacity(); RefreshCustomise(); });
            _custOpacityLbl = UIHelpers.Txt("CustOpacityV", opacityRow.transform,
                Mods.MenuCustomiser.OpacityDisplay,
                12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            _custOpacityLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 52;
            UIHelpers.SmallBtn(opacityRow.transform, "\u25B6",
                () => { Mods.MenuCustomiser.NextOpacity(); RefreshCustomise(); });

            UIHelpers.InfoBox(c, "Below 50% opacity the menu becomes hard to read.");

            UIHelpers.Divider(c);

            // ── Save / Reset buttons ──────────────────────────────────
            var btnRow = UIHelpers.StatRow("", c);
            UIHelpers.ActionBtn(btnRow.transform, "Save Now",
                () => { Mods.MenuCustomiser.SaveToFile(); }, 72);
            UIHelpers.ActionBtn(btnRow.transform, "Reset to Defaults",
                () => { Mods.MenuCustomiser.Reset(); RefreshCustomise(); }, 120);

            // ── Saved indicator (hidden until save fires) ─────────────
            _custSavedRow = UIHelpers.Obj("SavedIndicator", c);
            var siLE = _custSavedRow.AddComponent<LayoutElement>();
            siLE.preferredHeight = 22; siLE.minHeight = 22;
            var siHlg = _custSavedRow.AddComponent<HorizontalLayoutGroup>();
            siHlg.childAlignment = TextAnchor.MiddleCenter;
            siHlg.childForceExpandWidth = false;
            siHlg.childForceExpandHeight = false;
            siHlg.spacing = 6;

            var dot = UIHelpers.Txt("SavedDot", _custSavedRow.transform,
                "\u25CF", 10, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
            dot.gameObject.AddComponent<LayoutElement>().preferredWidth = 12;

            var savedLbl = UIHelpers.Txt("SavedLbl", _custSavedRow.transform,
                "Layout saved", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
            savedLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 80;

            _custSavedRow.SetActive(false);

            UIHelpers.AddScrollForwarders(c);
            RefreshCustomise();
        }

        private static void RefreshCustomise()
        {
            if (_custPosLbl)
                _custPosLbl.text = Mods.MenuCustomiser.PositionLabels[Mods.MenuCustomiser.PositionPreset];
            if (_custScaleLbl)
                _custScaleLbl.text = Mods.MenuCustomiser.ScaleDisplay;
            if (_custOpacityLbl)
                _custOpacityLbl.text = Mods.MenuCustomiser.OpacityDisplay;
        }

        // ── Helpers ───────────────────────────────────────────────────
        private static Text MakeInfoRow(string label, Transform parent)
        {
            var row = UIHelpers.Panel(label + "R", parent, UIHelpers.RowBg, UIHelpers.RowSp);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 28; le.minHeight = 28; le.flexibleHeight = 0;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset((int)UIHelpers.RowPad, (int)UIHelpers.RowPad, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            var bd = UIHelpers.Panel("Bd", row.transform, UIHelpers.RowBorder, UIHelpers.RowSp);
            bd.GetComponent<Image>().raycastTarget = false;
            UIHelpers.Fill(UIHelpers.RT(bd));
            bd.AddComponent<LayoutElement>().ignoreLayout = true;
            var lbl = UIHelpers.Txt(label + "L", row.transform, label, 11,
                FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextLight);
            lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var val = UIHelpers.Txt(label + "V", row.transform, "...", 11,
                FontStyle.Normal, TextAnchor.MiddleRight, UIHelpers.TextMid);
            val.gameObject.AddComponent<LayoutElement>().preferredWidth = 200;
            return val;
        }

        private static void MakeInfoRow2(string label, string value, Transform parent,
            Color? valueColor = null)
        {
            var row = UIHelpers.Panel(label + "R", parent, UIHelpers.RowBg, UIHelpers.RowSp);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 28; le.minHeight = 28; le.flexibleHeight = 0;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset((int)UIHelpers.RowPad, (int)UIHelpers.RowPad, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            var bd = UIHelpers.Panel("Bd", row.transform, UIHelpers.RowBorder, UIHelpers.RowSp);
            bd.GetComponent<Image>().raycastTarget = false;
            UIHelpers.Fill(UIHelpers.RT(bd));
            bd.AddComponent<LayoutElement>().ignoreLayout = true;
            var lbl = UIHelpers.Txt(label + "L", row.transform, label, 11,
                FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextLight);
            lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var val = UIHelpers.Txt(label + "V", row.transform, value, 11,
                FontStyle.Normal, TextAnchor.MiddleRight, valueColor ?? UIHelpers.TextMid);
            val.gameObject.AddComponent<LayoutElement>().preferredWidth = 200;
        }

        private static void MakeLinkRow(string label, string url, Transform parent)
        {
            var row = UIHelpers.Panel(label + "R", parent, UIHelpers.RowBg, UIHelpers.RowSp);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 28; le.minHeight = 28; le.flexibleHeight = 0;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset((int)UIHelpers.RowPad, (int)UIHelpers.RowPad, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            var bd = UIHelpers.Panel("Bd", row.transform, UIHelpers.RowBorder, UIHelpers.RowSp);
            bd.GetComponent<Image>().raycastTarget = false;
            UIHelpers.Fill(UIHelpers.RT(bd));
            bd.AddComponent<LayoutElement>().ignoreLayout = true;
            var lbl = UIHelpers.Txt(label + "L", row.transform, label, 11,
                FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextLight);
            lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var val = UIHelpers.Txt(label + "V", row.transform, url, 11,
                FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.NeonBlue);
            val.gameObject.AddComponent<LayoutElement>().preferredWidth = 280;
        }

        // ── Tick ──────────────────────────────────────────────────────
        public static void Tick()
        {
            if ((object)_custSavedRow != null)
                _custSavedRow.SetActive(Mods.MenuCustomiser.ShowSavedIndicator);

            // Refresh system tab once steam fetch completes
            if (_steamPlayerTxt && Mods.SteamPlayerCount.FetchComplete
                && _steamPlayerTxt.text == "...")
                Refresh();
        }

        // ── Refresh / Rebuild ─────────────────────────────────────────
        public static void Refresh()
        {
            try
            {
                // System tab values
                if (_unityVersionTxt) _unityVersionTxt.text = DiagnosticsManager.UnityVersion;
                if (_mlVersionTxt) _mlVersionTxt.text = DiagnosticsManager.MelonLoaderVersion;
                bool match = DiagnosticsManager.UnityVersionMatch;
                if (_unityMatchTxt)
                {
                    _unityMatchTxt.text = match
                        ? "OK \u2014 matches build target"
                        : "Mismatch! Built for " + DiagnosticsManager.BuiltForVersion;
                    _unityMatchTxt.color = match ? UIHelpers.OnColor : UIHelpers.OffColor;
                }

                // Steam player count — updates once fetch completes
                if (_steamPlayerTxt)
                {
                    _steamPlayerTxt.text = Mods.SteamPlayerCount.DisplayValue;
                    _steamPlayerTxt.color = Mods.SteamPlayerCount.FetchFailed
                        ? UIHelpers.OffColor : UIHelpers.Accent;
                }

            }
            catch (System.Exception ex) { MelonLogger.Error("InfoPage.Refresh: " + ex.Message); }
        }

    }
}