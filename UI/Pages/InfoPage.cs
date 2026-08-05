using DescendersModMenu.Mods;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class InfoPage
    {
        // ── Sub-tab state ─────────────────────────────────────────────
        private static int _activeTab = 0; // 0=System 1=Hotkeys 2=Customise 3=Career

        private static readonly string[] TabLabels = { "System", "Hotkeys", "Customize", "Career" };

        // Sub-tab bar buttons
        private static Image[] _tabBgs = new Image[4];
        private static Text[] _tabTxts = new Text[4];

        // Page root GameObjects
        private static GameObject _pgSystem;
        private static GameObject _pgHotkeys;
        private static GameObject _pgCustomise;
        private static GameObject _pgCareer;

        // Customise tab refs
        private static Text _custPosLbl;
        private static Text _custScaleLbl;
        private static Text _custOpacityLbl;
        private static GameObject _custSavedRow;

        // Set before RebuildMenu() to reopen the Info/Customise page on a specific
        // sub-tab (2 = Customise) instead of the default first sub-tab. Consumed on use.
        public static int PendingSubTab = -1;
        // System tab
        private static Text _unityVersionTxt;
        private static Text _steamPlayerTxt;
        private static Text _unityMatchTxt;
        private static Text _mlVersionTxt;
        private static Text _careerResultTxt;
        private static Text _sponsorVal;
        private static Text _repVal;
        private static UnityEngine.UI.Button _repMinus, _repPlus;
        private static Text _repMultVal;
        private static Text _inGameRepVal;
        private static UnityEngine.UI.Button _inGameRepMinus, _inGameRepPlus;
        private static Text _inGameRepMultVal;
        private static GameObject _devDiagContent;
        private static GameObject _devDiagLockedRow;
        private static Text _devDiagLockedTxt;

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

                // ── Career page ───────────────────────────────────────
                _pgCareer = UIHelpers.Obj("PgCareer", contentArea.transform);
                UIHelpers.Fill(UIHelpers.RT(_pgCareer));
                BuildCareerPage(_pgCareer.transform);

                SwitchTab(PendingSubTab >= 0 ? PendingSubTab : 0);
                PendingSubTab = -1;
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
            if ((object)_pgCareer != null) _pgCareer.SetActive(idx == 3);

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
            // Scrollable - this tab grew past one screen's worth of content once
            // Career Progression moved in, so it needs the same ScrollRect setup every
            // other scrollable page in this project already uses.
            var scrollObj = UIHelpers.Obj("SysScroll", p);
            UIHelpers.Fill(UIHelpers.RT(scrollObj));
            var sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 25f; sr.inertia = false;

            var vp = UIHelpers.Obj("SysVP", scrollObj.transform);
            UIHelpers.Fill(UIHelpers.RT(vp));
            vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            vp.AddComponent<Mask>().showMaskGraphic = true;
            sr.viewport = UIHelpers.RT(vp);

            var vlg = UIHelpers.Obj("SysVlg", vp.transform);
            var crt = UIHelpers.RT(vlg);
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 0);
            sr.content = crt;
            vlg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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

            // ── Diagnostics - gated behind DevLock (tap header 7x within 3s).
            // Content lives in its own container so unlocking just toggles
            // visibility instead of needing a full page rebuild. Neither row here
            // has a Favourites entry, so there's no bypass-via-Favourites concern
            // like the career-progression tools would have had.
            UIHelpers.Divider(vlg.transform);
            UIHelpers.SectionHeaderButton("DEVELOPER DIAGNOSTICS", vlg.transform, HandleDevDiagTap);

            _devDiagLockedRow = UIHelpers.Obj("DevDiagLocked", vlg.transform);
            var dlLe = _devDiagLockedRow.AddComponent<LayoutElement>();
            dlLe.preferredHeight = 36; dlLe.minHeight = 36; dlLe.flexibleHeight = 0;
            _devDiagLockedTxt = UIHelpers.Txt("DevDiagLockedTxt", _devDiagLockedRow.transform,
                "Locked - tap the header above " + DevLock.TapsRemaining + " more time(s) to unlock.",
                11, FontStyle.Italic, TextAnchor.MiddleCenter, UIHelpers.OffColor);
            UIHelpers.Fill(UIHelpers.RT(_devDiagLockedTxt.gameObject));

            _devDiagContent = UIHelpers.Obj("DevDiagContent", vlg.transform);
            var dcLe = _devDiagContent.AddComponent<LayoutElement>();
            dcLe.flexibleHeight = 0;
            var dcVlg = _devDiagContent.AddComponent<VerticalLayoutGroup>();
            dcVlg.spacing = UIHelpers.RowGap;
            dcVlg.childAlignment = TextAnchor.UpperCenter;
            dcVlg.childForceExpandWidth = true; dcVlg.childForceExpandHeight = false;
            var dcFitter = _devDiagContent.AddComponent<ContentSizeFitter>();
            dcFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Transform ddc = _devDiagContent.transform;

            var dumpRow = UIHelpers.StatRow("Scene Dump", ddc);
            UIHelpers.ActionBtn(dumpRow.transform, "Dump Now", () =>
            {
                SceneDumper.DumpCurrentScene();
            }, 90);
            UIHelpers.InfoBox(ddc, "Writes forensic dump files next to the game folder. Same as pressing # in-game - use this if that hotkey doesn't register on your setup.");

            var bikeUnlockDumpRow = UIHelpers.StatRow("Bike Unlock Status", ddc);
            UIHelpers.ActionBtn(bikeUnlockDumpRow.transform, "Dump Now", () =>
            {
                CareerReset.DumpBikeUnlockStatus();
                RefreshCareerResult();
            }, 90);
            UIHelpers.InfoBox(ddc, "Logs every Bike/BikeType customization item with its live IsItemUnlocked() result - check MelonLoader/Latest.log for the [CareerReset] lines after clicking.");

            _devDiagLockedRow.SetActive(!DevLock.IsUnlocked);
            _devDiagContent.SetActive(DevLock.IsUnlocked);

            UIHelpers.AddScrollbar(sr);
            UIHelpers.AddScrollForwarders(vlg.transform);
        }

        // ── Career page (its own tab, next to Customise) ────────────────
        private static void BuildCareerPage(Transform p)
        {
            var scrollObj = UIHelpers.Obj("CareerScroll", p);
            UIHelpers.Fill(UIHelpers.RT(scrollObj));
            var sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 25f; sr.inertia = false;

            var vp = UIHelpers.Obj("CareerVP", scrollObj.transform);
            UIHelpers.Fill(UIHelpers.RT(vp));
            vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            vp.AddComponent<Mask>().showMaskGraphic = true;
            sr.viewport = UIHelpers.RT(vp);

            var vlg = UIHelpers.Obj("CareerVlg", vp.transform);
            var crt = UIHelpers.RT(vlg);
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 0);
            sr.content = crt;
            vlg.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var v = vlg.AddComponent<VerticalLayoutGroup>();
            v.spacing = UIHelpers.RowGap;
            v.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            UIHelpers.SectionHeader("CAREER PROGRESSION", vlg.transform);
            UIHelpers.InfoBox(vlg.transform, "Irreversible, no confirmation step.");

            var completeRow = UIHelpers.StatRow("Complete Missions", vlg.transform);
            UIHelpers.ActionBtnOrange(completeRow.transform, "Complete All", () =>
            {
                CareerReset.CompleteAllMissions();
                RefreshCareerResult();
            }, 100);

            var levelRow = UIHelpers.StatRow("Level Reset", vlg.transform);
            UIHelpers.ActionBtnOrange(levelRow.transform, "Wipe Progress", () =>
            {
                CareerReset.ResetLevelProgress();
                RefreshCareerResult();
            }, 100);

            var sponsorRow = UIHelpers.StatRow("Sponsor Reset", vlg.transform);
            UIHelpers.ActionBtnOrange(sponsorRow.transform, "Reset Sponsor", () =>
            {
                CareerReset.ResetSponsorProgress();
                RefreshCareerResult();
            }, 100);

            var maxTierRow = UIHelpers.StatRow("Max Sponsor Level", vlg.transform);
            UIHelpers.ActionBtnOrange(maxTierRow.transform, "Max Level", () =>
            {
                CareerReset.MaxSponsorLevel();
                RefreshCareerResult();
            }, 100);

            // ── Switch Sponsor ────────────────────────────────────────
            var switchSponsorRow = UIHelpers.StatRow("Current Sponsor", vlg.transform);
            UIHelpers.SmallBtn(switchSponsorRow.transform, "\u25C0", () =>
            {
                CareerReset.PreviousSponsor();
                RefreshCareerResult();
            });
            _sponsorVal = UIHelpers.Txt("SponsorV", switchSponsorRow.transform, CareerReset.CurrentSponsorName, 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            _sponsorVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 130;
            UIHelpers.SmallBtn(switchSponsorRow.transform, "\u25B6", () =>
            {
                CareerReset.NextSponsor();
                RefreshCareerResult();
            });
            UIHelpers.InfoBox(vlg.transform, "Cycles through every sponsor team. Changes which sponsor's branding/menus/tier progress you're currently signed to.");

            // ── Unlock All (bikes + gear) - two plain instant buttons, no
            // status text, no confirm step. Default OFF each session (the flag
            // itself already starts false / resets via PrefsManager as before).
            var unlockAllRow = UIHelpers.StatRow("Unlock All (Bikes + Gear)", vlg.transform);
            UIHelpers.ActionBtnOrange(unlockAllRow.transform, "Unlock All", () =>
            {
                CareerReset.UnlockAllOn();
                RefreshCareerResult();
            }, 90);
            UIHelpers.ActionBtnOrange(unlockAllRow.transform, "Lock All", () =>
            {
                CareerReset.UnlockAllOff();
                RefreshCareerResult();
            }, 90);
            UIHelpers.InfoBox(vlg.transform, "Unlocks or locks every bike and gear item immediately - no confirmation step.");

            // ── Adjust Rep (+/-) ──────────────────────────────────────
            var repRow = UIHelpers.StatRow("Adjust Total Rep", vlg.transform);
            _repMinus = UIHelpers.SmallBtn(repRow.transform, "\u25C0", () =>
            {
                CareerReset.AdjustRep(-CareerReset.RepStepAmount);
                RefreshCareerResult();
            });
            _repVal = UIHelpers.Txt("RepV", repRow.transform, CareerReset.LiveRepValue.ToString(), 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            _repVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 90;
            _repPlus = UIHelpers.SmallBtn(repRow.transform, "\u25B6", () =>
            {
                CareerReset.AdjustRep(CareerReset.RepStepAmount);
                RefreshCareerResult();
            });
            UIHelpers.SmallBtn(repRow.transform, "-", () =>
            {
                CareerReset.DecreaseRepMultiplier();
                RefreshCareerResult();
            });
            _repMultVal = UIHelpers.Txt("RepMultV", repRow.transform, "x" + CareerReset.RepMultiplierLevel, 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
            _repMultVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            UIHelpers.SmallBtn(repRow.transform, "+", () =>
            {
                CareerReset.IncreaseRepMultiplier();
                RefreshCareerResult();
            });
            UIHelpers.InfoBox(vlg.transform, "+/- rep per click, scaled by the x1-x10 multiplier on the right. Updates TOTALREP (sponsor tiers) and the persistent lifetime rep total (the one submitted to Steam).");

            var inGameRepRow = UIHelpers.StatRow("Adjust In-Game Rep", vlg.transform);
            _inGameRepMinus = UIHelpers.SmallBtn(inGameRepRow.transform, "\u25C0", () =>
            {
                CareerReset.AdjustInGameRep(-CareerReset.InGameRepStepAmount);
                RefreshCareerResult();
            });
            _inGameRepVal = UIHelpers.Txt("InGameRepV", inGameRepRow.transform, CareerReset.CurrentInGameRep.ToString(), 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            _inGameRepVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 90;
            _inGameRepPlus = UIHelpers.SmallBtn(inGameRepRow.transform, "\u25B6", () =>
            {
                CareerReset.AdjustInGameRep(CareerReset.InGameRepStepAmount);
                RefreshCareerResult();
            });
            UIHelpers.SmallBtn(inGameRepRow.transform, "-", () =>
            {
                CareerReset.DecreaseInGameRepMultiplier();
                RefreshCareerResult();
            });
            _inGameRepMultVal = UIHelpers.Txt("InGameRepMultV", inGameRepRow.transform, "x" + CareerReset.InGameRepMultiplierLevel, 11,
                FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
            _inGameRepMultVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
            UIHelpers.SmallBtn(inGameRepRow.transform, "+", () =>
            {
                CareerReset.IncreaseInGameRepMultiplier();
                RefreshCareerResult();
            });
            UIHelpers.InfoBox(vlg.transform, "+/- rep per click, scaled by the x1-x10 multiplier on the right. Adjusts the current session's combo-score rep counter only - resets to 0 at the start of each session, separate from your Total Rep above.");

            var resultRow = UIHelpers.StatRow("Last Result", vlg.transform);
            _careerResultTxt = UIHelpers.Txt("CRResult", resultRow.transform, CareerReset.LastResult,
                11, FontStyle.Normal, TextAnchor.MiddleRight, UIHelpers.Accent);
            _careerResultTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            FavouritesManager.RegisterStarButton("CompleteMissions", UIHelpers.StarBtn(completeRow.transform, "CompleteMissions", () => FavouritesManager.Toggle("CompleteMissions")));
            FavouritesManager.RegisterStarButton("LevelReset", UIHelpers.StarBtn(levelRow.transform, "LevelReset", () => FavouritesManager.Toggle("LevelReset")));
            FavouritesManager.RegisterStarButton("SponsorReset", UIHelpers.StarBtn(sponsorRow.transform, "SponsorReset", () => FavouritesManager.Toggle("SponsorReset")));
            FavouritesManager.RegisterStarButton("MaxSponsorLevel", UIHelpers.StarBtn(maxTierRow.transform, "MaxSponsorLevel", () => FavouritesManager.Toggle("MaxSponsorLevel")));
            FavouritesManager.RegisterStarButton("SwitchSponsor", UIHelpers.StarBtn(switchSponsorRow.transform, "SwitchSponsor", () => FavouritesManager.Toggle("SwitchSponsor")));
            FavouritesManager.RegisterStarButton("UnlockAll", UIHelpers.StarBtn(unlockAllRow.transform, "UnlockAll", () => FavouritesManager.Toggle("UnlockAll")));
            FavouritesManager.RegisterStarButton("AdjustRep", UIHelpers.StarBtn(repRow.transform, "AdjustRep", () => FavouritesManager.Toggle("AdjustRep")));
            FavouritesManager.RegisterStarButton("AdjustInGameRep", UIHelpers.StarBtn(inGameRepRow.transform, "AdjustInGameRep", () => FavouritesManager.Toggle("AdjustInGameRep")));

            FavouritesManager.Register(new ModFavEntry
            {
                Id = "SwitchSponsor",
                DisplayName = "Current Sponsor",
                TabBadge = "INFO",
                BuildControls = (fp) =>
                {
                    var row = UIHelpers.StatRow("Current Sponsor", fp);
                    UIHelpers.SmallBtn(row.transform, "\u25C0", () =>
                    {
                        CareerReset.PreviousSponsor();
                        RefreshCareerResult();
                        FavsPage.RefreshFavourites();
                    });
                    var val = UIHelpers.Txt("FSt_SwitchSponsor", row.transform, CareerReset.CurrentSponsorName,
                        13, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                    val.gameObject.AddComponent<LayoutElement>().preferredWidth = 110;
                    UIHelpers.SmallBtn(row.transform, "\u25B6", () =>
                    {
                        CareerReset.NextSponsor();
                        RefreshCareerResult();
                        FavsPage.RefreshFavourites();
                    });
                },
                IsActive = () => false
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "UnlockAll",
                DisplayName = "Unlock All (Bikes + Gear)",
                TabBadge = "INFO",
                BuildControls = (fp) => FavsPage.BuildSimpleToggle(fp, "UnlockAll", "Unlock All",
                    () => CareerReset.UnlockAllEnabled, () => { CareerReset.ToggleUnlockAll(); RefreshCareerResult(); }, () => RefreshCareerResult()),
                IsActive = () => CareerReset.UnlockAllEnabled
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "AdjustRep",
                DisplayName = "Adjust Total Rep",
                TabBadge = "INFO",
                BuildControls = (fp) => FavsPage.BuildStepper(fp, "AdjustRep", "Adjust Total Rep",
                    () => CareerReset.LiveRepValue,
                    () => { CareerReset.AdjustRep(-CareerReset.RepStepAmount); RefreshCareerResult(); },
                    () => { CareerReset.AdjustRep(CareerReset.RepStepAmount); RefreshCareerResult(); },
                    int.MinValue, int.MaxValue, () => RefreshCareerResult(), 0),
                IsActive = () => false
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "AdjustInGameRep",
                DisplayName = "Adjust In-Game Rep",
                TabBadge = "INFO",
                BuildControls = (fp) => FavsPage.BuildStepper(fp, "AdjustInGameRep", "Adjust In-Game Rep",
                    () => CareerReset.CurrentInGameRep,
                    () => { CareerReset.AdjustInGameRep(-CareerReset.InGameRepStepAmount); RefreshCareerResult(); },
                    () => { CareerReset.AdjustInGameRep(CareerReset.InGameRepStepAmount); RefreshCareerResult(); },
                    int.MinValue, int.MaxValue, () => RefreshCareerResult(), 0),
                IsActive = () => false
            });

            FavouritesManager.Register(new ModFavEntry
            {
                Id = "CompleteMissions",
                DisplayName = "Complete All Missions",
                TabBadge = "INFO",
                BuildControls = (fp) => FavsPage.BuildActionButton(fp, "CompleteMissions", "Complete All Missions",
                    "Complete All", () => CareerReset.CompleteAllMissions(), null, () => CareerReset.LastResult),
                IsActive = () => false
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "LevelReset",
                DisplayName = "Level Reset",
                TabBadge = "INFO",
                BuildControls = (fp) => FavsPage.BuildActionButton(fp, "LevelReset", "Level Reset",
                    "Wipe Progress", () => CareerReset.ResetLevelProgress(), null, () => CareerReset.LastResult),
                IsActive = () => false
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "SponsorReset",
                DisplayName = "Sponsor Reset",
                TabBadge = "INFO",
                BuildControls = (fp) => FavsPage.BuildActionButton(fp, "SponsorReset", "Sponsor Reset",
                    "Reset Sponsor", () => CareerReset.ResetSponsorProgress(), null, () => CareerReset.LastResult),
                IsActive = () => false
            });
            FavouritesManager.Register(new ModFavEntry
            {
                Id = "MaxSponsorLevel",
                DisplayName = "Max Sponsor Level",
                TabBadge = "INFO",
                BuildControls = (fp) => FavsPage.BuildActionButton(fp, "MaxSponsorLevel", "Max Sponsor Level",
                    "Max Level", () => CareerReset.MaxSponsorLevel(), null, () => CareerReset.LastResult),
                IsActive = () => false
            });

            UIHelpers.AddScrollbar(sr);
            UIHelpers.AddScrollForwarders(vlg.transform);
            RefreshCareerResult();
        }

        // ── Dev Diagnostics tap-to-unlock (gates Scene Dump / Bike Unlock Status) ──
        private static void HandleDevDiagTap()
        {
            DevLock.RegisterTap();
            if (_devDiagLockedTxt)
                _devDiagLockedTxt.text = DevLock.IsUnlocked
                    ? "Unlocked."
                    : "Locked - tap the header above " + DevLock.TapsRemaining + " more time(s) to unlock.";

            if (_devDiagLockedRow) _devDiagLockedRow.SetActive(!DevLock.IsUnlocked);
            if (_devDiagContent) _devDiagContent.SetActive(DevLock.IsUnlocked);
        }

        private static void RefreshCareerResult()
        {
            if ((object)_careerResultTxt != null) _careerResultTxt.text = CareerReset.LastResult;

            if (_sponsorVal) _sponsorVal.text = CareerReset.CurrentSponsorName;

            if (_repVal) _repVal.text = CareerReset.LiveRepValue.ToString();
            if (_repMultVal) _repMultVal.text = "x" + CareerReset.RepMultiplierLevel;
            if (_inGameRepVal) _inGameRepVal.text = CareerReset.CurrentInGameRep.ToString();
            if (_inGameRepMultVal) _inGameRepMultVal.text = "x" + CareerReset.InGameRepMultiplierLevel;
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

            // ── Colour Scheme ────────────────────────────────────────
            UIHelpers.SectionHeader("COLOUR SCHEME", c);
            UIHelpers.InfoBox(c, "Pick an accent colour. Applies instantly and saves automatically.");
            BuildSchemeSwatches(c);

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

        // ── Colour scheme swatches ───────────────────────────────────
        // Rebuilt fresh every time this page is built (which happens on
        // every RebuildMenu call), so it always reflects the live
        // ColorSchemeManager.CurrentIndex — no separate refresh needed.
        private static void BuildSchemeSwatches(Transform c)
        {
            var schemes = ColorSchemeManager.Presets;
            const int perRow = 4;
            int i = 0;
            while (i < schemes.Length)
            {
                var row = UIHelpers.Obj("SchemeRow" + i, c);
                var rowLe = row.AddComponent<LayoutElement>();
                rowLe.preferredHeight = 30; rowLe.minHeight = 30; rowLe.flexibleHeight = 0;
                var hlg = row.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 6;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true;

                int rowCount = Mathf.Min(perRow, schemes.Length - i);
                for (int j = 0; j < rowCount; j++)
                {
                    int idx = i + j; // captured per-iteration, safe for the lambda below
                    var s = schemes[idx];
                    string label = (idx == ColorSchemeManager.CurrentIndex ? "\u2713 " : "") + s.Name;
                    var swatch = UIHelpers.Btn("Scheme" + idx, row.transform, label,
                        new Vector2(148, 28), 11,
                        () => { ColorSchemeManager.SelectScheme(idx); },
                        s.Accent, UITheme.TextOnBtn);
                    var swLe = swatch.gameObject.AddComponent<LayoutElement>();
                    swLe.preferredWidth = 148; swLe.preferredHeight = 28;
                    swLe.minWidth = 148; swLe.minHeight = 28; swLe.flexibleHeight = 0;
                }
                i += perRow;
            }
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