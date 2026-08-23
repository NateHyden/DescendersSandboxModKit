using DescendersModMenu.Mods;
using MelonLoader;
using DescendersModMenu;
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

        // ── Fly Mode speed steppers ────────────────────────────────────
        private static Text _flyMoveVal, _flyClimbVal;
        private static UnityEngine.UI.Button _flyMoveMinus, _flyMovePlus, _flyClimbMinus, _flyClimbPlus;

        // ── Moon Mode UI refs ─────────────────────────────────────────
        private static Image _moonBg, _moonBdr;
        private static Text _moonTxt;

        private static GameObject _invisPlayerRow, _mirrorRow, _flyRow, _drunkRow;
        private static GameObject _cartoonSquashRow;
        private static Text _cartoonSquashVal, _cartoonSpeedVal, _cartoonAmountVal, _cartoonJellyVal;
        private static Image _cartoonSquashTrack, _cartoonJellyTrack;
        private static RectTransform _cartoonSquashKnob, _cartoonJellyKnob;
        private static UnityEngine.UI.Button _cartoonSpeedMinus, _cartoonSpeedPlus;
        private static UnityEngine.UI.Button _cartoonAmountMinus, _cartoonAmountPlus;


        public static void CaptureSceneDefaults()
        {
            PlayerSize.CaptureDefaults();
            CartoonSquash.CaptureDefaults();
        }

        private static Text _hoverVal, _hoverHeightVal;
        private static Image _hoverTrack;
        private static RectTransform _hoverKnob;

        private static Text _rideWaterVal;
        private static Image _rideWaterTrack;
        private static RectTransform _rideWaterKnob;

        public static bool IsAnyActive =>
            InvisiblePlayer.Enabled || MoonMode.IsActive || PlayerSize.IsModified ||
            MirrorMode.Enabled || FlyMode.Enabled || DrunkMode.Enabled ||
            CameraShake.Enabled || HoverMode.Enabled || CartoonSquash.IsActive ||
            RideOnWater.Enabled;

        public static void GlobalReset()
        {
            if (InvisiblePlayer.Enabled) InvisiblePlayer.SetEnabled(false);
            if (MoonMode.IsActive) MoonMode.Toggle();
            if (HoverMode.Enabled) HoverMode.Toggle();
            HoverMode.SetHeight(3f);
            if (RideOnWater.Enabled) RideOnWater.SetEnabled(false);
            PlayerSize.ApplyLevel(10);
            if (MirrorMode.Enabled) MirrorMode.Toggle();
            if (FlyMode.Enabled) FlyMode.Toggle();
            FlyMode.SetMoveSpeed(30f);
            FlyMode.SetClimbSpeed(20f);
            if (DrunkMode.Enabled) DrunkMode.Toggle();
            if (CameraShake.Enabled) CameraShake.Toggle();
            CameraShake.SetLevel(5);
            CartoonSquash.GlobalReset();
        }

        // ─────────────────────────────────────────────────────────────
        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("P9R", parent);
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
                UIHelpers.AddScrollbar(sr);
                content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

                var pg9 = content.transform;

                // ── RESET TAB ─────────────────────────────────────────
                var rstRow = UIHelpers.BareBtnRow(pg9);
                UIHelpers.ActionBtnOrange(rstRow.transform, "↺  Reset Tab to Defaults", () => { GlobalReset(); RefreshAll(); }, 186);
                UIHelpers.SectionHeader("PLAYER SIZE", pg9);
                var psr = UIHelpers.StatRow("Size", pg9);
                _playerSizeMinus = UIHelpers.SmallBtn(psr.transform, "◀", () =>
                {
                    if (PlayerSize.Level > 1) { PlayerSize.Decrease(); RefreshAll(); }
                });
                _playerSizeLvlVal = UIHelpers.Txt("PsL", psr.transform, PlayerSize.Level.ToString(), 13,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _playerSizeLvlVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 32;
                _playerSizePlus = UIHelpers.SmallBtn(psr.transform, "▶", () =>
                {
                    if (PlayerSize.Level < 20) { PlayerSize.Increase(); RefreshAll(); }
                });
                UIHelpers.InfoBox(pg9, "10 is normal. Lower = smaller rider, higher = bigger.");

                UIHelpers.SectionHeader("CARTOON SQUASH", pg9);
                _cartoonSquashRow = UIHelpers.StatRow("Squash & Stretch", pg9);
                _cartoonSquashVal = UIHelpers.Txt("CsV", _cartoonSquashRow.transform, "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _cartoonSquashVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(_cartoonSquashRow.transform, "CsT",
                    () => { CartoonSquash.Toggle(); RefreshAll(); },
                    out _cartoonSquashTrack, out _cartoonSquashKnob);

                var csSpeedRow = UIHelpers.StatRow("Bounce Speed", pg9);
                _cartoonSpeedMinus = UIHelpers.SmallBtn(csSpeedRow.transform, "-",
                    () => { CartoonSquash.DecreaseSpeed(); RefreshAll(); });
                _cartoonSpeedVal = UIHelpers.Txt("CsSpd", csSpeedRow.transform, CartoonSquash.SpeedDisplay, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _cartoonSpeedVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 52;
                _cartoonSpeedPlus = UIHelpers.SmallBtn(csSpeedRow.transform, "+",
                    () => { CartoonSquash.IncreaseSpeed(); RefreshAll(); });

                var csAmtRow = UIHelpers.StatRow("Squash Amount", pg9);
                _cartoonAmountMinus = UIHelpers.SmallBtn(csAmtRow.transform, "-",
                    () => { CartoonSquash.DecreaseAmount(); RefreshAll(); });
                _cartoonAmountVal = UIHelpers.Txt("CsAmt", csAmtRow.transform, CartoonSquash.AmountLevel.ToString(), 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _cartoonAmountVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 32;
                _cartoonAmountPlus = UIHelpers.SmallBtn(csAmtRow.transform, "+",
                    () => { CartoonSquash.IncreaseAmount(); RefreshAll(); });

                var csJellyRow = UIHelpers.StatRow("Jelly Mode", pg9);
                _cartoonJellyVal = UIHelpers.Txt("CsJelV", csJellyRow.transform, "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _cartoonJellyVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(csJellyRow.transform, "CsJelT",
                    () => { CartoonSquash.ToggleJelly(); RefreshAll(); },
                    out _cartoonJellyTrack, out _cartoonJellyKnob);

                UIHelpers.InfoBox(pg9,
                    "Pick one: Bounce = constant squash loop. Jelly = soft body on landings (harder landings squash more). They can't both be on.");

                UIHelpers.Divider(pg9);

                // ── PRESETS ───────────────────────────────────────────
                UIHelpers.SectionHeader("PRESETS", pg9);

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

                var flyMoveRow = UIHelpers.StatRow("Side-to-Side Speed (Fly Mode)", pg9);
                _flyMoveMinus = UIHelpers.SmallBtn(flyMoveRow.transform, "-", () => { FlyMode.DecreaseMoveSpeed(); RefreshAll(); });
                _flyMoveVal = UIHelpers.Txt("FlMV", flyMoveRow.transform, FlyMode.MoveSpeed.ToString("0"), 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _flyMoveVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                _flyMovePlus = UIHelpers.SmallBtn(flyMoveRow.transform, "+", () => { FlyMode.IncreaseMoveSpeed(); RefreshAll(); });

                var flyClimbRow = UIHelpers.StatRow("Up/Down Speed (Fly Mode)", pg9);
                _flyClimbMinus = UIHelpers.SmallBtn(flyClimbRow.transform, "-", () => { FlyMode.DecreaseClimbSpeed(); RefreshAll(); });
                _flyClimbVal = UIHelpers.Txt("FlCV", flyClimbRow.transform, FlyMode.ClimbSpeed.ToString("0"), 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _flyClimbVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                _flyClimbPlus = UIHelpers.SmallBtn(flyClimbRow.transform, "+", () => { FlyMode.IncreaseClimbSpeed(); RefreshAll(); });

                _drunkRow = UIHelpers.StatRow("Drunk Mode", pg9);
                var drnkr = _drunkRow;
                _drunkVal = UIHelpers.Txt("DrV", drnkr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _drunkVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(drnkr.transform, "DrT", () => { DrunkMode.Toggle(); RefreshAll(); }, out _drunkTrack, out _drunkKnob);

                var hoverRow = UIHelpers.StatRow("Hover Mode", pg9);
                _hoverVal = UIHelpers.Txt("HovV", hoverRow.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _hoverVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(hoverRow.transform, "HovT", () => { HoverMode.Toggle(); RefreshAll(); }, out _hoverTrack, out _hoverKnob);
                _hoverHeightVal = UIHelpers.Txt("HovHV", hoverRow.transform, HoverMode.DisplayHeight, 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _hoverHeightVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
                UIHelpers.SmallBtn(hoverRow.transform, "-", () => { HoverMode.DecreaseHeight(); RefreshAll(); });
                UIHelpers.SmallBtn(hoverRow.transform, "+", () => { HoverMode.IncreaseHeight(); RefreshAll(); });
                UIHelpers.InfoBox(pg9, "How high you float above the ground. Negative sinks you under it.");

                var rideWaterRow = UIHelpers.StatRow("Ride On Water", pg9);
                _rideWaterVal = UIHelpers.Txt("RoWV", rideWaterRow.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _rideWaterVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(rideWaterRow.transform, "RoWT", () => { RideOnWater.Toggle(); RefreshAll(); }, out _rideWaterTrack, out _rideWaterKnob);
                UIHelpers.InfoBox(pg9, "Stops water (and other kill volumes) from auto-bailing, and makes them solid so you can ride on them.");

                UIHelpers.Divider(pg9);

                // ── CAMERA ────────────────────────────────────────────
                UIHelpers.SectionHeader("CAMERA", pg9);

                var csr = UIHelpers.StatRow("Camera Shake", pg9);
                _shakeBar = UIHelpers.MakeBar("ShB", csr.transform, (CameraShake.Level - 1) / (float)(CameraShake.MaxLevel - 1));
                _shakeVal = UIHelpers.Txt("ShV", csr.transform, CameraShake.DisplayValue, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _shakeVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                _shakeTogVal = UIHelpers.Txt("ShTV", csr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _shakeTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                Image shakeTrack; RectTransform shakeKnob;
                UIHelpers.Toggle(csr.transform, "ShT", () => { CameraShake.Toggle(); RefreshAll(); }, out shakeTrack, out shakeKnob);
                UIHelpers.SmallBtn(csr.transform, "-", () => { CameraShake.Decrease(); RefreshAll(); });
                UIHelpers.SmallBtn(csr.transform, "+", () => { CameraShake.Increase(); RefreshAll(); });
                _shakeTrack = shakeTrack; _shakeKnob = shakeKnob;
                UIHelpers.InfoBox(pg9, "Extra camera shake when going fast. Turn it on, then turn the dial up.");

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

                FavouritesManager.RegisterStarButton("DrunkMode", UIHelpers.StarBtn(_drunkRow.transform, "DrunkMode", () => FavouritesManager.Toggle("DrunkMode")));
                FavouritesManager.RegisterStarButton("FlyMode", UIHelpers.StarBtn(_flyRow.transform, "FlyMode", () => FavouritesManager.Toggle("FlyMode")));
                FavouritesManager.RegisterStarButton("FlyMoveSpeed", UIHelpers.StarBtn(flyMoveRow.transform, "FlyMoveSpeed", () => FavouritesManager.Toggle("FlyMoveSpeed")));
                FavouritesManager.RegisterStarButton("FlyClimbSpeed", UIHelpers.StarBtn(flyClimbRow.transform, "FlyClimbSpeed", () => FavouritesManager.Toggle("FlyClimbSpeed")));
                FavouritesManager.RegisterStarButton("MirrorMode", UIHelpers.StarBtn(_mirrorRow.transform, "MirrorMode", () => FavouritesManager.Toggle("MirrorMode")));
                FavouritesManager.RegisterStarButton("HoverMode", UIHelpers.StarBtn(hoverRow.transform, "HoverMode", () => FavouritesManager.Toggle("HoverMode")));
                FavouritesManager.RegisterStarButton("RideOnWater", UIHelpers.StarBtn(rideWaterRow.transform, "RideOnWater", () => FavouritesManager.Toggle("RideOnWater")));
                FavouritesManager.RegisterStarButton("CameraShake", UIHelpers.StarBtn(csr.transform, "CameraShake", () => FavouritesManager.Toggle("CameraShake")));
                FavouritesManager.RegisterStarButton("PlayerSize", UIHelpers.StarBtn(psr.transform, "PlayerSize", () => FavouritesManager.Toggle("PlayerSize")));
                FavouritesManager.RegisterStarButton("CartoonSquash", UIHelpers.StarBtn(_cartoonSquashRow.transform, "CartoonSquash", () => FavouritesManager.Toggle("CartoonSquash")));
                FavouritesManager.RegisterStarButton("CartoonSquashSpeed", UIHelpers.StarBtn(csSpeedRow.transform, "CartoonSquashSpeed", () => FavouritesManager.Toggle("CartoonSquashSpeed")));
                FavouritesManager.RegisterStarButton("CartoonSquashAmount", UIHelpers.StarBtn(csAmtRow.transform, "CartoonSquashAmount", () => FavouritesManager.Toggle("CartoonSquashAmount")));
                FavouritesManager.RegisterStarButton("CartoonSquashJelly", UIHelpers.StarBtn(csJellyRow.transform, "CartoonSquashJelly", () => FavouritesManager.Toggle("CartoonSquashJelly")));
                FavouritesManager.RegisterStarButton("InvisiblePlayer", UIHelpers.StarBtn(_invisPlayerRow.transform, "InvisiblePlayer", () => FavouritesManager.Toggle("InvisiblePlayer")));
                FavouritesManager.RegisterStarButton("MoonMode", UIHelpers.StarBtnAbs(mmtop.transform, "MoonMode", () => FavouritesManager.Toggle("MoonMode")));

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
                    Id = "FlyMoveSpeed",
                    DisplayName = "Fly: Side-to-Side Speed",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildStepper(p, "FlyMoveSpeed", "Side-to-Side Speed",
                        () => (int)FlyMode.MoveSpeed,
                        () => FlyMode.DecreaseMoveSpeed(),
                        () => FlyMode.IncreaseMoveSpeed(),
                        (int)FlyMode.MinMoveSpeed, (int)FlyMode.MaxMoveSpeed, () => RefreshAll(), 30),
                    IsActive = () => FlyMode.MoveSpeed != 30f
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "FlyClimbSpeed",
                    DisplayName = "Fly: Up/Down Speed",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildStepper(p, "FlyClimbSpeed", "Up/Down Speed",
                        () => (int)FlyMode.ClimbSpeed,
                        () => FlyMode.DecreaseClimbSpeed(),
                        () => FlyMode.IncreaseClimbSpeed(),
                        (int)FlyMode.MinClimbSpeed, (int)FlyMode.MaxClimbSpeed, () => RefreshAll(), 20),
                    IsActive = () => FlyMode.ClimbSpeed != 20f
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
                    Id = "HoverMode",
                    DisplayName = "Hover Mode",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildToggleIntensityStepper(p, "HoverMode", "Hover Mode",
                        () => HoverMode.Enabled, () => HoverMode.Toggle(),
                        () => HoverMode.DisplayHeight, () => HoverMode.DecreaseHeight(), () => HoverMode.IncreaseHeight(),
                        () => MenuWindow.RefreshAll()),
                    IsActive = () => HoverMode.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "RideOnWater",
                    DisplayName = "Ride On Water",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "RideOnWater", "Ride On Water",
                        () => RideOnWater.Enabled, () => RideOnWater.Toggle(), () => RefreshAll()),
                    IsActive = () => RideOnWater.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "CameraShake",
                    DisplayName = "Camera Shake",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildToggleSlider(p, "CameraShake", "Camera Shake",
                        () => CameraShake.Enabled, () => CameraShake.Toggle(),
                        () => CameraShake.Level, () => CameraShake.Increase(), () => CameraShake.Decrease(),
                        CameraShake.MaxLevel, () => (CameraShake.Level - 1) / (float)(CameraShake.MaxLevel - 1), () => RefreshAll(),
                        () => CameraShake.DisplayValue),
                    IsActive = () => CameraShake.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "CartoonSquash",
                    DisplayName = "Cartoon Squash",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "CartoonSquash", "Squash & Stretch",
                        () => CartoonSquash.Enabled, () => CartoonSquash.Toggle(), () => RefreshAll()),
                    IsActive = () => CartoonSquash.Enabled
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "CartoonSquashSpeed",
                    DisplayName = "Cartoon Squash: Speed",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildStepper(p, "CartoonSquashSpeed", "Bounce Speed",
                        () => CartoonSquash.SpeedLevel,
                        () => CartoonSquash.DecreaseSpeed(),
                        () => CartoonSquash.IncreaseSpeed(),
                        CartoonSquash.MinLevel, CartoonSquash.MaxLevel, () => RefreshAll(), 10,
                        () => CartoonSquash.SpeedDisplay),
                    IsActive = () => CartoonSquash.SpeedLevel != 10
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "CartoonSquashAmount",
                    DisplayName = "Cartoon Squash: Amount",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildStepper(p, "CartoonSquashAmount", "Squash Amount",
                        () => CartoonSquash.AmountLevel,
                        () => CartoonSquash.DecreaseAmount(),
                        () => CartoonSquash.IncreaseAmount(),
                        CartoonSquash.MinLevel, CartoonSquash.MaxLevel, () => RefreshAll(), 10),
                    IsActive = () => CartoonSquash.AmountLevel != 10
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "CartoonSquashJelly",
                    DisplayName = "Cartoon Squash: Jelly",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "CartoonSquashJelly", "Jelly Mode",
                        () => CartoonSquash.JellyMode, () => CartoonSquash.ToggleJelly(), () => RefreshAll()),
                    IsActive = () => CartoonSquash.JellyMode
                });
                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "PlayerSize",
                    DisplayName = "Player Size",
                    TabBadge = "FUN",
                    BuildControls = (p) => FavsPage.BuildStepper(p, "PlayerSize", "Player Size",
                        () => PlayerSize.Level,
                        () => PlayerSize.Decrease(),
                        () => PlayerSize.Increase(),
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
            catch (System.Exception ex) { MelonLogger.Error("FunPage.CreatePage: " + ex.Message); Telemetry.ReportErrorAsync(ex, "FunPage"); return null; }
            return pg;
        }


        // ── RefreshAll ────────────────────────────────────────────────
        public static void RefreshAll()
        {
            if (_playerSizeLvlVal) _playerSizeLvlVal.text = PlayerSize.Level.ToString();
            if ((object)_playerSizeMinus != null && _playerSizeMinus) _playerSizeMinus.interactable = PlayerSize.Level > 1;
            if ((object)_playerSizePlus != null && _playerSizePlus) _playerSizePlus.interactable = PlayerSize.Level < 20;

            bool csOn = CartoonSquash.Enabled;
            if (_cartoonSquashVal)
            {
                _cartoonSquashVal.text = csOn ? "ON" : "OFF";
                _cartoonSquashVal.color = csOn ? UIHelpers.OnColor : UIHelpers.OffColor;
            }
            if (_cartoonSquashTrack != null && _cartoonSquashKnob != null)
                UIHelpers.SetToggle(_cartoonSquashTrack, _cartoonSquashKnob, csOn);
            if (_cartoonSpeedVal) _cartoonSpeedVal.text = CartoonSquash.SpeedDisplay;
            if ((object)_cartoonSpeedMinus != null && _cartoonSpeedMinus)
                _cartoonSpeedMinus.interactable = CartoonSquash.SpeedLevel > CartoonSquash.MinLevel;
            if ((object)_cartoonSpeedPlus != null && _cartoonSpeedPlus)
                _cartoonSpeedPlus.interactable = CartoonSquash.SpeedLevel < CartoonSquash.MaxLevel;
            if (_cartoonAmountVal) _cartoonAmountVal.text = CartoonSquash.AmountLevel.ToString();
            if ((object)_cartoonAmountMinus != null && _cartoonAmountMinus)
                _cartoonAmountMinus.interactable = CartoonSquash.AmountLevel > CartoonSquash.MinLevel;
            if ((object)_cartoonAmountPlus != null && _cartoonAmountPlus)
                _cartoonAmountPlus.interactable = CartoonSquash.AmountLevel < CartoonSquash.MaxLevel;

            bool jellyOn = CartoonSquash.JellyMode;
            if (_cartoonJellyVal)
            {
                _cartoonJellyVal.text = jellyOn ? "ON" : "OFF";
                _cartoonJellyVal.color = jellyOn ? UIHelpers.OnColor : UIHelpers.OffColor;
            }
            if (_cartoonJellyTrack != null && _cartoonJellyKnob != null)
                UIHelpers.SetToggle(_cartoonJellyTrack, _cartoonJellyKnob, jellyOn);

            if (_invisVal) { _invisVal.text = InvisiblePlayer.Enabled ? "ON" : "OFF"; _invisVal.color = InvisiblePlayer.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_invisTrack, _invisKnob, InvisiblePlayer.Enabled);

            if (_moonTxt) { _moonTxt.text = MoonMode.IsActive ? "MOON MODE ACTIVE" : "ACTIVATE MOON MODE"; _moonTxt.color = new Color(0, 0, 0, 1); }
            if (_moonBg) _moonBg.color = MoonMode.IsActive ? UIHelpers.OnColor : UIHelpers.NeonBlue;
            if (_moonBdr) _moonBdr.color = MoonMode.IsActive ? UIHelpers.OnColor : UIHelpers.NeonBlue;

            bool mmOn = MirrorMode.Enabled;
            if (_mirrorVal) { _mirrorVal.text = mmOn ? "ON" : "OFF"; _mirrorVal.color = mmOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_mirrorTrack, _mirrorKnob, mmOn);

            bool flyOn = FlyMode.Enabled;
            if (_flyVal) { _flyVal.text = flyOn ? "ON" : "OFF"; _flyVal.color = flyOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_flyTrack, _flyKnob, flyOn);

            if (_flyMoveVal) _flyMoveVal.text = FlyMode.MoveSpeed.ToString("0");
            if ((object)_flyMoveMinus != null && _flyMoveMinus) _flyMoveMinus.interactable = FlyMode.MoveSpeed > FlyMode.MinMoveSpeed;
            if ((object)_flyMovePlus != null && _flyMovePlus) _flyMovePlus.interactable = FlyMode.MoveSpeed < FlyMode.MaxMoveSpeed;

            if (_flyClimbVal) _flyClimbVal.text = FlyMode.ClimbSpeed.ToString("0");
            if ((object)_flyClimbMinus != null && _flyClimbMinus) _flyClimbMinus.interactable = FlyMode.ClimbSpeed > FlyMode.MinClimbSpeed;
            if ((object)_flyClimbPlus != null && _flyClimbPlus) _flyClimbPlus.interactable = FlyMode.ClimbSpeed < FlyMode.MaxClimbSpeed;

            bool drunkOn = DrunkMode.Enabled;
            if (_drunkVal) { _drunkVal.text = drunkOn ? "ON" : "OFF"; _drunkVal.color = drunkOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_drunkTrack, _drunkKnob, drunkOn);

            bool hover = HoverMode.Enabled;
            if (_hoverVal) { _hoverVal.text = hover ? "ON" : "OFF"; _hoverVal.color = hover ? UIHelpers.OnColor : UIHelpers.OffColor; }
            if (_hoverHeightVal) _hoverHeightVal.text = HoverMode.DisplayHeight;
            UIHelpers.SetToggle(_hoverTrack, _hoverKnob, hover);

            bool rideWater = RideOnWater.Enabled;
            if (_rideWaterVal) { _rideWaterVal.text = rideWater ? "ON" : "OFF"; _rideWaterVal.color = rideWater ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_rideWaterTrack, _rideWaterKnob, rideWater);

            bool shOn = CameraShake.Enabled;
            if (_shakeTogVal) { _shakeTogVal.text = shOn ? "ON" : "OFF"; _shakeTogVal.color = shOn ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_shakeTrack, _shakeKnob, shOn);
            if (_shakeVal) _shakeVal.text = CameraShake.DisplayValue;
            UIHelpers.SetBar(_shakeBar, (CameraShake.Level - 1) / (float)(CameraShake.MaxLevel - 1));

        }
    }
}

