using DescendersModMenu.Mods;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class EspPage
    {
        private static Text espVal, distVal, tracVal;
        private static Image espTrk, distTrk, tracTrk;
        private static RectTransform espKnb, distKnb, tracKnb;
        private static Text worldVal;
        private static Image worldTrk;
        private static RectTransform worldKnb;
        private static Text _modUsersText;
        private static Text _modUsersHudVal;
        private static Image _modUsersHudTrk;
        private static RectTransform _modUsersHudKnb;
        private static int _cpIndex = 0;
        private static Text _cpIndexText = null;

        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                var root = UIHelpers.Obj("P2R", parent);
                UIHelpers.Fill(UIHelpers.RT(root));

                var scrollObj = UIHelpers.Obj("Scroll", root.transform);
                UIHelpers.Fill(UIHelpers.RT(scrollObj));
                var scrollRect = scrollObj.AddComponent<ScrollRect>();
                scrollRect.horizontal = false; scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 25f; scrollRect.inertia = false;

                var vp = UIHelpers.Obj("VP", scrollObj.transform);
                UIHelpers.Fill(UIHelpers.RT(vp));
                vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                vp.AddComponent<Mask>().showMaskGraphic = true;
                scrollRect.viewport = UIHelpers.RT(vp);

                pg = UIHelpers.Obj("Content", vp.transform);
                var crt = UIHelpers.RT(pg);
                crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 0);
                scrollRect.content = crt;
                pg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var vlg = pg.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

                UIHelpers.SectionHeader("VISUAL PLAYER FINDER", pg.transform);

                var er = UIHelpers.StatRow("Visual Player Finder", pg.transform);
                espVal = UIHelpers.Txt("EV", er.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                var evle = espVal.gameObject.AddComponent<LayoutElement>(); evle.preferredWidth = 28; evle.preferredHeight = 18; evle.flexibleHeight = 0;
                UIHelpers.Toggle(er.transform, "ET", () => { ESP.Toggle(); RefreshTexts(); }, out espTrk, out espKnb);

                var dr = UIHelpers.StatRow("Distance", pg.transform);
                distVal = UIHelpers.Txt("DV", dr.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                var dvle = distVal.gameObject.AddComponent<LayoutElement>(); dvle.preferredWidth = 28; dvle.preferredHeight = 18; dvle.flexibleHeight = 0;
                UIHelpers.Toggle(dr.transform, "DT", () => { ESP.ToggleDistance(); RefreshTexts(); }, out distTrk, out distKnb);

                var tr = UIHelpers.StatRow("Tracers", pg.transform);
                tracVal = UIHelpers.Txt("TV", tr.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                var tvle = tracVal.gameObject.AddComponent<LayoutElement>(); tvle.preferredWidth = 28; tvle.preferredHeight = 18; tvle.flexibleHeight = 0;
                UIHelpers.Toggle(tr.transform, "TT", () => { ESP.ToggleTracers(); RefreshTexts(); }, out tracTrk, out tracKnb);

                var rr = UIHelpers.StatRow("Refresh", pg.transform);
                UIHelpers.ActionBtn(rr.transform, "Refresh", () => { ESP.RefreshNow(); RefreshTexts(); });

                UIHelpers.Divider(pg.transform);
                UIHelpers.SectionHeader("WORLD OBJECT FINDER", pg.transform);
                UIHelpers.InfoBox(pg.transform, "Shows collectibles, shortcuts, boosts, hazards and checkpoints. Colour-coded by type.");

                var wr = UIHelpers.StatRow("World Object Finder", pg.transform);
                worldVal = UIHelpers.Txt("WV", wr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                var wvle = worldVal.gameObject.AddComponent<LayoutElement>(); wvle.preferredWidth = 28; wvle.preferredHeight = 18; wvle.flexibleHeight = 0;
                UIHelpers.Toggle(wr.transform, "WT", () => { ESP.ToggleWorldObjects(); RefreshTexts(); }, out worldTrk, out worldKnb);

                var wrr = UIHelpers.StatRow("Refresh Objects", pg.transform);
                UIHelpers.ActionBtn(wrr.transform, "Refresh", () => { ESP.RefreshNow(); RefreshTexts(); });

                UIHelpers.Divider(pg.transform);
                UIHelpers.SectionHeader("TELEPORT", pg.transform);

                var tpr = UIHelpers.StatRow("Player", pg.transform);
                UIHelpers.SmallBtn(tpr.transform, "\u25C0", () => TeleportUI.PreviousPlayer());

                var nb = UIHelpers.Panel("NB", tpr.transform, UIHelpers.RowBg, UIHelpers.BtnSp);
                var nle = nb.AddComponent<LayoutElement>(); nle.flexibleWidth = 1; nle.preferredHeight = 26; nle.flexibleHeight = 0; nle.minWidth = 180;
                var nbd = UIHelpers.Panel("NBd", nb.transform, UIHelpers.RowBorder, UIHelpers.BtnSp);
                nbd.GetComponent<Image>().raycastTarget = false; UIHelpers.Fill(UIHelpers.RT(nbd));
                TeleportUI.PlayerNameText = UIHelpers.Txt("TPN", nb.transform,
                    "No players \u2014 scan first", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                TeleportUI.PlayerNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                TeleportUI.PlayerNameText.verticalOverflow = VerticalWrapMode.Truncate;
                UIHelpers.Fill(UIHelpers.RT(TeleportUI.PlayerNameText.gameObject), 4, 4, 0, 0);

                UIHelpers.SmallBtn(tpr.transform, "\u25B6", () => TeleportUI.NextPlayer());
                UIHelpers.ActionBtnOrange(tpr.transform, "Teleport", () => TeleportUI.TeleportToSelected(), 76);

                var sr = UIHelpers.StatRow("Find Players", pg.transform);
                UIHelpers.ActionBtn(sr.transform, "Scan", () => TeleportUI.Scan());

                UIHelpers.Divider(pg.transform);
                UIHelpers.SectionHeader("CHECKPOINT", pg.transform);

                var cpr = UIHelpers.StatRow("Last Checkpoint", pg.transform);
                UIHelpers.ActionBtnOrange(cpr.transform, "Teleport", () =>
                {
                    try { TeleportToCheckpoint.Teleport(); }
                    catch (System.Exception ex) { MelonLogger.Error("[TeleportCP]: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "EspPage"); }
                }, 76);

                // ── Teleport by index ─────────────────────────────
                var cpir = UIHelpers.StatRow("By Index", pg.transform);
                UIHelpers.SmallBtn(cpir.transform, "◀", () =>
                {
                    int count = TeleportToCheckpoint.CheckpointCount;
                    if (count > 0) { _cpIndex = (_cpIndex - 1 + count) % count; RefreshCpIndex(); }
                });

                var cpiBg = UIHelpers.Obj("CpIBg", cpir.transform);
                cpiBg.AddComponent<Image>().color = UIHelpers.WinOuter;
                var cpiBgLe = cpiBg.AddComponent<LayoutElement>();
                cpiBgLe.preferredWidth = 52; cpiBgLe.minWidth = 52; cpiBgLe.preferredHeight = 26;
                _cpIndexText = UIHelpers.Txt("CpIT", cpiBg.transform, "0 / 0",
                    11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextLight);
                UIHelpers.Fill(UIHelpers.RT(_cpIndexText.gameObject));

                UIHelpers.SmallBtn(cpir.transform, "▶", () =>
                {
                    int count = TeleportToCheckpoint.CheckpointCount;
                    if (count > 0) { _cpIndex = (_cpIndex + 1) % count; RefreshCpIndex(); }
                });
                UIHelpers.ActionBtnOrange(cpir.transform, "Teleport", () =>
                {
                    int count = TeleportToCheckpoint.CheckpointCount;
                    if (count == 0) { ModLog.Warn("[TeleportCP] No checkpoints."); return; }
                    _cpIndex = UnityEngine.Mathf.Clamp(_cpIndex, 0, count - 1);
                    TeleportToCheckpoint.TeleportByIndex(_cpIndex);
                }, 76);

                UIHelpers.Divider(pg.transform);
                UIHelpers.SectionHeader("SANDBOX USERS", pg.transform);

                var mdr = UIHelpers.StatRow("Detect Sandbox Users", pg.transform);
                UIHelpers.ActionBtn(mdr.transform, "Scan", () => { ModDetection.Scan(); RefreshModUsers(); }, 52);

                var muHud = UIHelpers.StatRow("Sandbox Users HUD", pg.transform);
                _modUsersHudVal = UIHelpers.Txt("MUHV", muHud.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                var muhvLe = _modUsersHudVal.gameObject.AddComponent<LayoutElement>(); muhvLe.preferredWidth = 28; muhvLe.preferredHeight = 18; muhvLe.flexibleHeight = 0;
                UIHelpers.Toggle(muHud.transform, "MUHT", () => { ModUsersHUD.Toggle(); RefreshTexts(); }, out _modUsersHudTrk, out _modUsersHudKnb);
                UIHelpers.InfoBox(pg.transform, "Top-right counter of Sandbox users in the lobby. Updates automatically.");

                _modUsersText = UIHelpers.Txt("MUT", pg.transform, "Scanning lobby for Sandbox users...", 11,
                    FontStyle.Normal, TextAnchor.UpperLeft, UIHelpers.TextMid);
                _modUsersText.horizontalOverflow = HorizontalWrapMode.Wrap;
                _modUsersText.verticalOverflow = VerticalWrapMode.Truncate;
                var mutle = _modUsersText.gameObject.AddComponent<LayoutElement>();
                mutle.preferredHeight = 60; mutle.flexibleWidth = 1;

                FavouritesManager.RegisterStarButton("ESP", UIHelpers.StarBtn(er.transform, "ESP", () => FavouritesManager.Toggle("ESP")));
                FavouritesManager.RegisterStarButton("ESPDistance", UIHelpers.StarBtn(dr.transform, "ESPDistance", () => FavouritesManager.Toggle("ESPDistance")));
                FavouritesManager.RegisterStarButton("ESPTracers", UIHelpers.StarBtn(tr.transform, "ESPTracers", () => FavouritesManager.Toggle("ESPTracers")));
                FavouritesManager.RegisterStarButton("ESPWorldObjects", UIHelpers.StarBtn(wr.transform, "ESPWorldObjects", () => FavouritesManager.Toggle("ESPWorldObjects")));

                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "ESP",
                    DisplayName = "ESP",
                    TabBadge = "SYSTEM",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "ESP", "ESP",
                        () => ESP.Enabled, () => ESP.Toggle(), () => RefreshTexts()),
                    IsActive = () => ESP.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "ESPDistance",
                    DisplayName = "Distance",
                    TabBadge = "SYSTEM",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "ESPDistance", "Distance",
                        () => ESP.ShowDistance, () => ESP.ToggleDistance(), () => RefreshTexts()),
                    IsActive = () => !ESP.ShowDistance
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "ESPTracers",
                    DisplayName = "Tracers",
                    TabBadge = "SYSTEM",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "ESPTracers", "Tracers",
                        () => ESP.ShowTracers, () => ESP.ToggleTracers(), () => RefreshTexts()),
                    IsActive = () => !ESP.ShowTracers
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "ESPWorldObjects",
                    DisplayName = "World Object Finder",
                    TabBadge = "SYSTEM",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "ESPWorldObjects", "World Object Finder",
                        () => ESP.ShowWorldObjects, () => ESP.ToggleWorldObjects(), () => RefreshTexts()),
                    IsActive = () => ESP.ShowWorldObjects
                });

                RefreshTexts();

                UIHelpers.AddScrollbar(scrollRect);
                UIHelpers.AddScrollForwarders(pg.transform);
            }
            catch (System.Exception ex) { MelonLogger.Error("EspPage.CreatePage: " + ex.Message); Telemetry.ReportErrorAsync(ex, "EspPage"); return null; }
            return pg;
        }

        public static void RefreshTexts()
        {
            Upd(espVal, espTrk, espKnb, ESP.Enabled);
            Upd(distVal, distTrk, distKnb, ESP.ShowDistance);
            Upd(tracVal, tracTrk, tracKnb, ESP.ShowTracers);
            Upd(worldVal, worldTrk, worldKnb, ESP.ShowWorldObjects);
            Upd(_modUsersHudVal, _modUsersHudTrk, _modUsersHudKnb, ModUsersHUD.Enabled);
            RefreshCpIndex();
            RefreshModUsers();
        }

        public static void Tick()
        {
            if (!_modUsersText) return;
            RefreshModUsers();
        }

        public static void ClearUiRefs()
        {
            espVal = distVal = tracVal = worldVal = _modUsersHudVal = null;
            espTrk = distTrk = tracTrk = worldTrk = _modUsersHudTrk = null;
            espKnb = distKnb = tracKnb = worldKnb = _modUsersHudKnb = null;
            _modUsersText = null;
            _cpIndexText = null;
        }

        private static void RefreshModUsers()
        {
            if (!_modUsersText) return;
            var users = ModDetection.ModUsers;
            if (users.Count == 0)
            {
                _modUsersText.text = "No Sandbox users found in lobby";
                _modUsersText.color = UIHelpers.TextDim;
                return;
            }
            _modUsersText.color = UIHelpers.Accent;
            string txt = "";
            for (int i = 0; i < users.Count; i++)
            {
                if (i > 0) txt += "\n";
                txt += users[i].Name + "  [v" + users[i].Version + "]";
            }
            _modUsersText.text = txt;
        }

        private static void RefreshCpIndex()
        {
            if (!UnityNull.Alive(_cpIndexText)) return;
            int count = TeleportToCheckpoint.CheckpointCount;
            if (count == 0)
            { _cpIndexText.text = "No CPs"; _cpIndexText.color = UIHelpers.TextDim; return; }
            _cpIndex = UnityEngine.Mathf.Clamp(_cpIndex, 0, count - 1);
            _cpIndexText.text = (_cpIndex + 1) + " / " + count;
            _cpIndexText.color = UIHelpers.TextLight;
        }

        private static void Upd(Text l, Image t, RectTransform k, bool on)
        {
            if (l) { l.text = on ? "ON" : "OFF"; l.color = on ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(t, k, on);
        }
    }

    public static class TeleportUI
    {
        public static Text PlayerNameText;
        private static int _i;
        private static System.Collections.Generic.List<TeleportToPlayer.PlayerEntry> _pl
            = new System.Collections.Generic.List<TeleportToPlayer.PlayerEntry>();

        public static void Scan()
        { _pl = TeleportToPlayer.ScanForPlayers(); _i = 0; UL(); ModLog.Debug("[TP] Scanned: " + _pl.Count); }
        public static void NextPlayer()
        { if (_pl.Count == 0) return; _i = (_i + 1) % _pl.Count; UL(); }
        public static void PreviousPlayer()
        { if (_pl.Count == 0) return; _i = (_i - 1 + _pl.Count) % _pl.Count; UL(); }
        public static void TeleportToSelected()
        {
            if (_pl.Count == 0) { ModLog.Warn("[TP] No players."); UL("Scan first!"); return; }
            var e = _pl[_i]; bool ok = TeleportToPlayer.TeleportTo(e);
            UL(ok ? "Teleported to " + e.Name + "!" : "Failed");
        }
        private static void UL(string ov = null)
        {
            if (!UnityNull.Alive(PlayerNameText)) return;
            if (ov != null) { PlayerNameText.text = ov; return; }
            if (_pl.Count == 0) { PlayerNameText.text = "No players \u2014 press Scan"; return; }
            var e = _pl[_i]; PlayerNameText.text = "(" + (_i + 1) + "/" + _pl.Count + ") " + e.Name;
        }
    }
}

