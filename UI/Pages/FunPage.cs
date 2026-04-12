using DescendersModMenu.Mods;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class FunPage
    {
        private static Text _playerSizeLvlVal;
        private static UnityEngine.UI.Button _playerSizeMinus, _playerSizePlus;
        private static Image _invisTrack; private static RectTransform _invisKnob;
        private static Text _invisVal;

        // ── Camera Shake ─────────────────────────────────────────────
        private static Text _shakeVal, _shakeTogVal;
        private static Image _shakeBar, _shakeTrack;
        private static RectTransform _shakeKnob;

        // ── Drunk / Fly / Mirror ──────────────────────────────────────
        private static Image _drunkTrack; private static RectTransform _drunkKnob; private static Text _drunkVal;
        private static Image _flyTrack; private static RectTransform _flyKnob; private static Text _flyVal;
        private static Image _mirrorTrack; private static RectTransform _mirrorKnob; private static Text _mirrorVal;

        // ── Moon Mode UI refs ─────────────────────────────────────────
        private static Image _moonBg, _moonBdr;
        private static Text _moonTxt;

        // ── Row GO refs for highlight ─────────────────────────────────
        private static GameObject _invisPlayerRow, _mirrorRow, _flyRow, _drunkRow;


        public static void CaptureSceneDefaults()
        {
            PlayerSize.CaptureDefaults();
        }

        public static bool IsAnyActive =>
            InvisiblePlayer.Enabled || MoonMode.IsActive || PlayerSize.IsModified ||
            MirrorMode.Enabled || FlyMode.Enabled || DrunkMode.Enabled ||
            CameraShake.Enabled;

        public static void GlobalReset()
        {
            if (InvisiblePlayer.Enabled) InvisiblePlayer.SetEnabled(false);
            if (MoonMode.IsActive) MoonMode.Toggle();
            PlayerSize.ApplyLevel(10);
        }

        // ─────────────────────────────────────────────────────────────
        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("P9R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));

                // ScrollRect wrapper
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
                UIHelpers.AddScrollbar(sr);
                content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

                var pg9 = content.transform;

                // ── RESET TAB ─────────────────────────────────────────
                var rstRow = UIHelpers.StatRow("", pg9);
                UIHelpers.ActionBtnOrange(rstRow.transform, "↺  Reset Tab to Defaults", () => { GlobalReset(); RefreshAll(); }, 186);
                UIHelpers.SectionHeader("PLAYER SIZE", pg9);
                var psr = UIHelpers.StatRow("Size", pg9);
                _playerSizeMinus = UIHelpers.SmallBtn(psr.transform, "◀", () =>
                {
                    if (PlayerSize.Level > 1) { PlayerSize.Level--; PlayerSize.ApplyLevel(PlayerSize.Level); RefreshAll(); }
                });
                _playerSizeLvlVal = UIHelpers.Txt("PsL", psr.transform, PlayerSize.Level.ToString(), 13,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _playerSizeLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 32;
                _playerSizePlus = UIHelpers.SmallBtn(psr.transform, "▶", () =>
                {
                    if (PlayerSize.Level < 20) { PlayerSize.Level++; PlayerSize.ApplyLevel(PlayerSize.Level); RefreshAll(); }
                });
                UIHelpers.InfoBox(pg9, "10 = default size. Lower numbers shrink the player, higher numbers grow them.");

                UIHelpers.Divider(pg9);

                // ── PRESETS ───────────────────────────────────────────
                UIHelpers.SectionHeader("PRESETS", pg9);

                // Moon Mode compound row
                var mmo = UIHelpers.Panel("MMR", pg9, UIHelpers.RowBg, UIHelpers.RowSp);
                mmo.AddComponent<LayoutElement>().minHeight = UIHelpers.RowH + 38;
                var mmbd = UIHelpers.Panel("MMBd", mmo.transform, UIHelpers.RowBorder, UIHelpers.RowSp);
                mmbd.GetComponent<Image>().raycastTarget = false; UIHelpers.Fill(UIHelpers.RT(mmbd));
                mmbd.AddComponent<LayoutElement>().ignoreLayout = true;
                var mmvlg = mmo.AddComponent<VerticalLayoutGroup>();
                mmvlg.spacing = 4; mmvlg.padding = new RectOffset((int)UIHelpers.RowPad, (int)UIHelpers.RowPad, 6, 8);
                mmvlg.childAlignment = TextAnchor.UpperCenter;
                mmvlg.childForceExpandWidth = true; mmvlg.childForceExpandHeight = false;
                var mmtop = UIHelpers.Obj("MMTop", mmo.transform);
                mmtop.AddComponent<LayoutElement>().preferredHeight = 28;
                var mmhlg = mmtop.AddComponent<HorizontalLayoutGroup>();
                mmhlg.spacing = 8; mmhlg.childAlignment = TextAnchor.MiddleCenter;
                mmhlg.childForceExpandWidth = false; mmhlg.childForceExpandHeight = false;
                var mml = UIHelpers.Txt("MML", mmtop.transform, "Moon Mode", 12, FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextLight);
                mml.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                var mmBtn = UIHelpers.Obj("MMBtn", mmo.transform);
                _moonBg = mmBtn.AddComponent<Image>(); _moonBg.sprite = UIHelpers.BtnSp;
                _moonBg.type = Image.Type.Sliced; _moonBg.color = UIHelpers.NeonBlue;
                var mbtn = mmBtn.AddComponent<Button>();
                mbtn.onClick.AddListener(() => { MoonMode.Toggle(); RefreshAll(); });
                var mcb = mbtn.colors;
                mcb.normalColor = Color.white; mcb.highlightedColor = new Color(1, 1, 1, 1.15f);
                mcb.pressedColor = new Color(.7f, .7f, .7f, 1); mcb.colorMultiplier = 1; mcb.fadeDuration = .08f;
                mbtn.colors = mcb;
                mmBtn.AddComponent<LayoutElement>().preferredHeight = 30;
                var mbdr = UIHelpers.Panel("MBdr", mmBtn.transform, UIHelpers.NeonBlue, UIHelpers.BtnSp);
                _moonBdr = mbdr.GetComponent<Image>(); _moonBdr.raycastTarget = false;
                UIHelpers.Fill(UIHelpers.RT(mbdr));
                _moonTxt = UIHelpers.Txt("MT", mmBtn.transform, "ACTIVATE MOON MODE", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0, 0, 0, 1));
                _moonTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                UIHelpers.Fill(UIHelpers.RT(_moonTxt.gameObject));

                UIHelpers.Divider(pg9);

                // ── EFFECTS ───────────────────────────────────────────
                UIHelpers.SectionHeader("EFFECTS", pg9);

                _mirrorRow = UIHelpers.StatRow("Mirror Mode", pg9);
                var mmr = _mirrorRow;
                _mirrorVal = UIHelpers.Txt("MmV", mmr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _mirrorVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(mmr.transform, "MmT", () => { MirrorMode.Toggle(); RefreshAll(); }, out _mirrorTrack, out _mirrorKnob);

                _flyRow = UIHelpers.StatRow("Fly Mode", pg9);
                var flyr = _flyRow;
                _flyVal = UIHelpers.Txt("FlV", flyr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _flyVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(flyr.transform, "FlT", () => { FlyMode.Toggle(); RefreshAll(); }, out _flyTrack, out _flyKnob);

                _drunkRow = UIHelpers.StatRow("Drunk Mode", pg9);
                var drnkr = _drunkRow;
                _drunkVal = UIHelpers.Txt("DrV", drnkr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _drunkVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(drnkr.transform, "DrT", () => { DrunkMode.Toggle(); RefreshAll(); }, out _drunkTrack, out _drunkKnob);

                UIHelpers.Divider(pg9);

                // ── CAMERA ────────────────────────────────────────────
                UIHelpers.SectionHeader("CAMERA", pg9);

                var csr = UIHelpers.StatRow("Camera Shake", pg9);
                _shakeBar = UIHelpers.MakeBar("ShB", csr.transform, (CameraShake.Level - 1) / 9f);
                _shakeVal = UIHelpers.Txt("ShV", csr.transform, CameraShake.DisplayValue, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _shakeVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
                _shakeTogVal = UIHelpers.Txt("ShTV", csr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _shakeTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                Image shakeTrack; RectTransform shakeKnob;
                UIHelpers.Toggle(csr.transform, "ShT", () => { CameraShake.Toggle(); RefreshAll(); }, out shakeTrack, out shakeKnob);
                UIHelpers.SmallBtn(csr.transform, "-", () => { CameraShake.Decrease(); RefreshAll(); });
                UIHelpers.SmallBtn(csr.transform, "+", () => { CameraShake.Increase(); RefreshAll(); });
                _shakeTrack = shakeTrack; _shakeKnob = shakeKnob;
                UIHelpers.InfoBox(pg9, "Level 5 = default. Amplifies camera shake at speed. Level 10 = 4x default.");

                UIHelpers.Divider(pg9);

                // ── PLAYER ────────────────────────────────────────────
                UIHelpers.SectionHeader("PLAYER", pg9);

                _invisPlayerRow = UIHelpers.StatRow("Invisible Player", pg9);
                var ir = _invisPlayerRow;
                _invisVal = UIHelpers.Txt("InV", ir.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _invisVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(ir.transform, "InT", () =>
                {
                    InvisiblePlayer.Toggle();
                    RefreshAll();
                }, out _invisTrack, out _invisKnob);

                // ── STAR BUTTONS (Favourites) ──────────────────────────
                FavouritesManager.RegisterStarButton("DrunkMode", UIHelpers.StarBtn(_drunkRow.transform, "DrunkMode", () => FavouritesManager.Toggle("DrunkMode")));
                FavouritesManager.RegisterStarButton("FlyMode", UIHelpers.StarBtn(_flyRow.transform, "FlyMode", () => FavouritesManager.Toggle("FlyMode")));
                FavouritesManager.RegisterStarButton("MirrorMode", UIHelpers.StarBtn(_mirrorRow.transform, "MirrorMode", () => FavouritesManager.Toggle("MirrorMode")));
                FavouritesManager.RegisterStarButton("CameraShake", UIHelpers.StarBtn(csr.transform, "CameraShake", () => FavouritesManager.Toggle("CameraShake")));
                FavouritesManager.RegisterStarButton("PlayerSize", UIHelpers.StarBtn(psr.transform, "PlayerSize", () => FavouritesManager.Toggle("PlayerSize")));
                FavouritesManager.RegisterStarButton("InvisiblePlayer", UIHelpers.StarBtn(_invisPlayerRow.transform, "InvisiblePlayer", () => FavouritesManager.Toggle("InvisiblePlayer")));
                FavouritesManager.RegisterStarButton("MoonMode", UIHelpers.StarBtnAbs(mmtop.transform, "MoonMode", () => FavouritesManager.Toggle("MoonMode")));

                // ── FACTORY REGISTRATIONS (Fun tab mods) ───────────────
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "DrunkMode",
                    DisplayName = "Drunk Mode",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "DrunkMode", "Drunk Mode",
                        () => DrunkMode.Enabled, () => DrunkMode.Toggle(), () => RefreshAll()),
                    IsActive = () => DrunkMode.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "FlyMode",
                    DisplayName = "Fly Mode",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "FlyMode", "Fly Mode",
                        () => FlyMode.Enabled, () => FlyMode.Toggle(), () => RefreshAll()),
                    IsActive = () => FlyMode.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "MirrorMode",
                    DisplayName = "Mirror Mode",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "MirrorMode", "Mirror Mode",
                        () => MirrorMode.Enabled, () => MirrorMode.Toggle(), () => RefreshAll()),
                    IsActive = () => MirrorMode.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "CameraShake",
                    DisplayName = "Camera Shake",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildToggleSlider(p, "CameraShake", "Camera Shake",
                        () => CameraShake.Enabled, () => CameraShake.Toggle(),
                        () => CameraShake.Level, () => CameraShake.Increase(), () => CameraShake.Decrease(),
                        10, () => (CameraShake.Level - 1) / 9f, () => RefreshAll(),
                        () => CameraShake.DisplayValue),
                    IsActive = () => CameraShake.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "PlayerSize",
                    DisplayName = "Player Size",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildStepper(p, "PlayerSize", "Player Size",
                        () => PlayerSize.Level,
                        () => { if (PlayerSize.Level > 1) { PlayerSize.Level--; PlayerSize.ApplyLevel(PlayerSize.Level); } },
                        () => { if (PlayerSize.Level < 20) { PlayerSize.Level++; PlayerSize.ApplyLevel(PlayerSize.Level); } },
                        1, 20, () => RefreshAll(), 10),
                    IsActive = () => PlayerSize.Level != 10
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "InvisiblePlayer",
                    DisplayName = "Invisible Player",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "InvisiblePlayer", "Invisible Player",
                        () => InvisiblePlayer.Enabled, () => { InvisiblePlayer.Toggle(); }, () => RefreshAll()),
                    IsActive = () => InvisiblePlayer.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "MoonMode",
                    DisplayName = "Moon Mode",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "MoonMode", "Moon Mode",
                        () => MoonMode.IsActive, () => MoonMode.Toggle(), () => RefreshAll()),
                    IsActive = () => MoonMode.IsActive
                });

                RefreshAll();
                UIHelpers.AddScrollForwarders(pg9);
            }
            catch (System.Exception ex) { MelonLogger.Error("FunPage.CreatePage: " + ex.Message); return null; }
            return pg;
        }


        // ── RefreshAll ────────────────────────────────────────────────
        public static void RefreshAll()
        {
            // Player size level
            if (_playerSizeLvlVal) _playerSizeLvlVal.text = PlayerSize.Level.ToString();
            if ((object)_playerSizeMinus != null) _playerSizeMinus.interactable = PlayerSize.Level > 1;
            if ((object)_playerSizePlus != null) _playerSizePlus.interactable = PlayerSize.Level < 20;

            if (_invisVal) { _invisVal.text = InvisiblePlayer.Enabled ? "ON" : "OFF"; _invisVal.color = InvisiblePlayer.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_invisTrack, _invisKnob, InvisiblePlayer.Enabled);
            UIHelpers.SetRowActive(_invisPlayerRow, InvisiblePlayer.Enabled);

            // Moon Mode
            if (_moonTxt) { _moonTxt.text = MoonMode.IsActive ? "MOON MODE ACTIVE" : "ACTIVATE MOON MODE"; _moonTxt.color = new Color(0, 0, 0, 1); }
            if (_moonBg) _moonBg.color = MoonMode.IsActive ? UIHelpers.OnColor : UIHelpers.NeonBlue;
            if (_moonBdr) _moonBdr.color = MoonMode.IsActive ? UIHelpers.OnColor : UIHelpers.NeonBlue;

            bool mmOn = MirrorMode.Enabled;
            if (_mirrorVal) { _mirrorVal.text = mmOn ? "ON" : "OFF"; _mirrorVal.color = mmOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_mirrorTrack, _mirrorKnob, mmOn);
            UIHelpers.SetRowActive(_mirrorRow, mmOn);

            bool flyOn = FlyMode.Enabled;
            if (_flyVal) { _flyVal.text = flyOn ? "ON" : "OFF"; _flyVal.color = flyOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_flyTrack, _flyKnob, flyOn);
            UIHelpers.SetRowActive(_flyRow, flyOn);

            bool drunkOn = DrunkMode.Enabled;
            if (_drunkVal) { _drunkVal.text = drunkOn ? "ON" : "OFF"; _drunkVal.color = drunkOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_drunkTrack, _drunkKnob, drunkOn);
            UIHelpers.SetRowActive(_drunkRow, drunkOn);

            // Camera Shake
            bool shOn = CameraShake.Enabled;
            if (_shakeTogVal) { _shakeTogVal.text = shOn ? "ON" : "OFF"; _shakeTogVal.color = shOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_shakeTrack, _shakeKnob, shOn);
            if (_shakeVal) _shakeVal.text = CameraShake.DisplayValue;
            UIHelpers.SetBar(_shakeBar, (CameraShake.Level - 1) / 9f);

        }
    }
}