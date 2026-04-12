using DescendersModMenu.Mods;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class ScreenshotPage
    {
        private static Text _toggleVal;
        private static Image _toggleTrack;
        private static RectTransform _toggleKnob;
        private static Text _filenameVal;
        private static RawImage _preview;
        private static GameObject _previewObj;
        private static UnityEngine.UI.AspectRatioFitter _previewFitter;
        private static UnityEngine.UI.Button _takeBtn;

        public static bool IsAnyActive => ScreenshotMode.Enabled;

        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("P18R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));

                var scrollObj = UIHelpers.Obj("Scroll", pg.transform);
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

                var content = UIHelpers.Obj("Content", vp.transform);
                var crt = UIHelpers.RT(content);
                crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 0);
                scrollRect.content = crt;
                UIHelpers.AddScrollbar(scrollRect);

                var csf = content.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                var c = content.transform;

                // ENABLE
                UIHelpers.SectionHeader("SCREENSHOT MODE", c);
                var toggleRow = UIHelpers.StatRow("Enabled", c);
                _toggleVal = UIHelpers.Txt("SsV", toggleRow.transform, "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _toggleVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(toggleRow.transform, "SsT",
                    () => { ScreenshotMode.Toggle(); RefreshAll(); },
                    out _toggleTrack, out _toggleKnob);
                FavouritesManager.RegisterStarButton("ScreenshotMode",
                    UIHelpers.StarBtn(toggleRow.transform, "ScreenshotMode",
                        () => FavouritesManager.Toggle("ScreenshotMode")));

                var takeRow = UIHelpers.StatRow("", c);
                UIHelpers.ActionBtnOrange(takeRow.transform, "Take Screenshot Now",
                    () => ScreenshotMode.TriggerScreenshot(), 180);
                _takeBtn = takeRow.GetComponentInChildren<UnityEngine.UI.Button>();
                var pauseNote = UIHelpers.Txt("PauseNote", takeRow.transform,
                    "[!] screen pauses briefly [!]", 9, FontStyle.Bold,
                    TextAnchor.MiddleRight, Color.white);
                pauseNote.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                // HOW TO USE
                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("HOW TO USE", c);
                UIHelpers.InfoBox(c,
                    "1. Enable Screenshot Mode using the toggle above." +
                    "\n2. Press DPad Up on your Xbox controller, or F11 on keyboard." +
                    "\n3. The menu disappears for a clean shot, then returns automatically." +
                    "\n4. Screenshots are saved at 2x your display resolution.");
                UIHelpers.InfoBox(c, "Save location:\n" + ScreenshotMode.SaveFolder);
                UIHelpers.InfoBox(c,
                    "Resolution: 2x your current display (1080p -> 4K)." +
                    "\nFilenames: screenshot_001.png to screenshot_100.png." +
                    "\nSlot 101 overwrites slot 001 and so on.");

                // LAST SCREENSHOT
                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("LAST SCREENSHOT", c);

                var nameRow = UIHelpers.StatRow("File", c);
                _filenameVal = UIHelpers.Txt("SsFN", nameRow.transform,
                    ScreenshotMode.LastFilename, 10, FontStyle.Normal,
                    TextAnchor.MiddleLeft, UIHelpers.TextDim);
                _filenameVal.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                _filenameVal.horizontalOverflow = HorizontalWrapMode.Overflow;

                // Preview image panel
                _previewObj = UIHelpers.Obj("PreviewFrame", c);
                var previewLE = _previewObj.AddComponent<LayoutElement>();
                previewLE.preferredHeight = 120; previewLE.flexibleWidth = 1;
                _previewObj.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 1f);

                var rawGO = UIHelpers.Obj("Preview", _previewObj.transform);
                var rawRT = UIHelpers.RT(rawGO);
                rawRT.anchorMin = new Vector2(0.5f, 0.5f);
                rawRT.anchorMax = new Vector2(0.5f, 0.5f);
                rawRT.pivot = new Vector2(0.5f, 0.5f);
                rawRT.sizeDelta = new Vector2(0, 112); // initial height, fitter controls width
                _preview = rawGO.AddComponent<RawImage>();
                _preview.color = new Color(1, 1, 1, 0.08f);
                _previewFitter = rawGO.AddComponent<UnityEngine.UI.AspectRatioFitter>();
                _previewFitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.HeightControlsWidth;
                _previewFitter.aspectRatio = 16f / 9f; // default until real texture loads

                var reloadRow = UIHelpers.StatRow("", c);
                UIHelpers.ActionBtn(reloadRow.transform, "Reload Preview", () => {
                    if (ScreenshotMode.LastPath.Length > 0)
                    {
                        ScreenshotMode.ForceReloadPreview();
                        RefreshAll();
                    }
                }, 120);
                UIHelpers.InfoBox(c, "Preview updates automatically after each screenshot. Use Reload Preview if it doesn't appear.");
                UIHelpers.AddScrollForwarders(c);

                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "ScreenshotMode",
                    DisplayName = "Screenshot Mode",
                    TabBadge = "TOOLS",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "ScreenshotMode", "Screenshot Mode",
                        () => ScreenshotMode.Enabled, () => ScreenshotMode.Toggle(), () => RefreshAll()),
                    IsActive = () => ScreenshotMode.Enabled
                });

                RefreshAll();
            }
            catch (System.Exception ex) { MelonLogger.Error("ScreenshotPage.CreatePage: " + ex.Message); return null; }
            return pg;
        }

        public static void RefreshAll()
        {
            bool on = ScreenshotMode.Enabled;
            if (_toggleVal) { _toggleVal.text = on ? "ON" : "OFF"; _toggleVal.color = on ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_toggleTrack, _toggleKnob, on);
            if ((object)_takeBtn != null) _takeBtn.interactable = on;
            if (_filenameVal) _filenameVal.text = ScreenshotMode.LastFilename;
            if ((object)_preview != null)
            {
                var tex = ScreenshotMode.PreviewTexture;
                if ((object)tex != null)
                {
                    _preview.texture = tex;
                    _preview.color = Color.white;
                    // Update aspect ratio to match actual screenshot dimensions
                    if ((object)_previewFitter != null && tex.height > 0)
                        _previewFitter.aspectRatio = (float)tex.width / tex.height;
                }
                else
                {
                    _preview.texture = null;
                    _preview.color = new Color(1, 1, 1, 0.08f);
                }
            }
        }
    }
}