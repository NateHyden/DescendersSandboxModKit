using DescendersModMenu.Mods;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class GraphicsPage
    {
        private static Image _bloomTrack; private static RectTransform _bloomKnob; private static Text _bloomVal;
        private static Image _aoTrack; private static RectTransform _aoKnob; private static Text _aoVal;
        private static Image _vigTrack; private static RectTransform _vigKnob; private static Text _vigVal;
        private static Image _dofTrack; private static RectTransform _dofKnob; private static Text _dofVal;
        private static Image _cabTrack; private static RectTransform _cabKnob; private static Text _cabVal;
        private static Image _eyeTrack; private static RectTransform _eyeKnob; private static Text _eyeVal;
        private static Image _cgTrack; private static RectTransform _cgKnob; private static Text _cgVal;
        private static Image _mbTrack; private static RectTransform _mbKnob; private static Text _mbVal;
        private static Image _ssrTrack; private static RectTransform _ssrKnob; private static Text _ssrVal;
        private static Image _ppFogTrack; private static RectTransform _ppFogKnob; private static Text _ppFogVal;
        private static Image _grainTrack; private static RectTransform _grainKnob; private static Text _grainVal;

        private static Image _shadowTrack; private static RectTransform _shadowKnob; private static Text _shadowVal;
        private static Image _softPTrack; private static RectTransform _softPKnob; private static Text _softPVal;
        private static Text _aaVal;
        private static Text _shadowDistVal;
        private static Text _shadowResVal;
        private static Text _cascadesVal;
        private static Text _texVal;
        private static Text _anisoVal;
        private static Text _lodVal;
        private static Text _pixelVal;
        private static Text _vsyncVal;
        private static Text _fpsVal;
        private static Text _qualityVal;
        private static Text _uiRemoverVal;
        private static Image _uiRemoverTrack; private static RectTransform _uiRemoverKnob;

        public static bool IsAnyActive
        {
            get
            {
                return !GraphicsSettings.BloomEnabled || !GraphicsSettings.AmbientOccEnabled ||
                    !GraphicsSettings.VignetteEnabled || GraphicsSettings.DepthOfFieldEnabled ||
                    !GraphicsSettings.ChromaticAbEnabled || !GraphicsSettings.ShadowsEnabled ||
                    !GraphicsSettings.SoftParticlesEnabled || GraphicsSettings.AntiAliasingLevel >= 0 ||
                    GraphicsSettings.MotionBlurEnabled || UIRemover.Enabled;
            }
        }

        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                GraphicsSettings.SyncLevelsFromQuality();

                pg = UIHelpers.Obj("P10R", parent);
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
                content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                Transform root = content.transform;

                UIHelpers.SectionHeader("PRESETS", root);
                var pr = UIHelpers.StatRow("Apply", root);
                UIHelpers.ActionBtn(pr.transform, "Max Detail", () => { GraphicsSettings.ApplyMaxDetail(); RefreshAll(); }, 72);
                UIHelpers.ActionBtn(pr.transform, "Ultra", () => { GraphicsSettings.SetQuality(3); RefreshAll(); }, 44);
                UIHelpers.ActionBtn(pr.transform, "Default", () => { GraphicsSettings.RestoreDefaultQuality(); RefreshAll(); }, 54);
                UIHelpers.InfoBox(root, "Turns every detail setting up as high as it will go.");

                UIHelpers.Divider(root);
                UIHelpers.SectionHeader("POST PROCESSING", root);

                var br = UIHelpers.StatRow("Bloom", root);
                _bloomVal = UIHelpers.Txt("BlV", br.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _bloomVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(br.transform, "BlT", () => { GraphicsSettings.ToggleBloom(); RefreshAll(); }, out _bloomTrack, out _bloomKnob);

                var aor = UIHelpers.StatRow("Ambient Occlusion", root);
                _aoVal = UIHelpers.Txt("AoV", aor.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _aoVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(aor.transform, "AoT", () => { GraphicsSettings.ToggleAO(); RefreshAll(); }, out _aoTrack, out _aoKnob);

                var vigr = UIHelpers.StatRow("Vignette", root);
                _vigVal = UIHelpers.Txt("VgV", vigr.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _vigVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(vigr.transform, "VgT", () => { GraphicsSettings.ToggleVignette(); RefreshAll(); }, out _vigTrack, out _vigKnob);

                var dofr = UIHelpers.StatRow("Depth of Field", root);
                _dofVal = UIHelpers.Txt("DfV", dofr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _dofVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(dofr.transform, "DfT", () => { GraphicsSettings.ToggleDOF(); RefreshAll(); }, out _dofTrack, out _dofKnob);

                var cabr = UIHelpers.StatRow("Chromatic Aberration", root);
                _cabVal = UIHelpers.Txt("CaV", cabr.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _cabVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(cabr.transform, "CaT", () => { GraphicsSettings.ToggleChromatic(); RefreshAll(); }, out _cabTrack, out _cabKnob);

                var eyer = UIHelpers.StatRow("Eye Adaptation", root);
                _eyeVal = UIHelpers.Txt("EyV", eyer.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _eyeVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(eyer.transform, "EyT", () => { GraphicsSettings.ToggleEyeAdapt(); RefreshAll(); }, out _eyeTrack, out _eyeKnob);

                var cgr = UIHelpers.StatRow("Color Grading", root);
                _cgVal = UIHelpers.Txt("CgV", cgr.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _cgVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(cgr.transform, "CgT", () => { GraphicsSettings.ToggleColorGrading(); RefreshAll(); }, out _cgTrack, out _cgKnob);

                var mbr = UIHelpers.StatRow("Motion Blur", root);
                _mbVal = UIHelpers.Txt("MbV", mbr.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _mbVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(mbr.transform, "MbT", () => { GraphicsSettings.ToggleMotionBlur(); RefreshAll(); }, out _mbTrack, out _mbKnob);

                var ssrr = UIHelpers.StatRow("SSR", root);
                _ssrVal = UIHelpers.Txt("SrV", ssrr.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _ssrVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(ssrr.transform, "SrT", () => { GraphicsSettings.ToggleSsr(); RefreshAll(); }, out _ssrTrack, out _ssrKnob);

                var ppf = UIHelpers.StatRow("PP Fog", root);
                _ppFogVal = UIHelpers.Txt("PfV", ppf.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _ppFogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(ppf.transform, "PfT", () => { GraphicsSettings.TogglePpFog(); RefreshAll(); }, out _ppFogTrack, out _ppFogKnob);

                var grn = UIHelpers.StatRow("Grain", root);
                _grainVal = UIHelpers.Txt("GnV", grn.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _grainVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(grn.transform, "GnT", () => { GraphicsSettings.ToggleGrain(); RefreshAll(); }, out _grainTrack, out _grainKnob);

                UIHelpers.InfoBox(root, "Some effects only work if the map supports them.");

                UIHelpers.Divider(root);
                UIHelpers.SectionHeader("RENDER", root);

                var shR = UIHelpers.StatRow("Shadows", root);
                _shadowVal = UIHelpers.Txt("ShV", shR.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _shadowVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(shR.transform, "ShT", () => { GraphicsSettings.ToggleShadows(); RefreshAll(); }, out _shadowTrack, out _shadowKnob);

                var spR = UIHelpers.StatRow("Soft Particles", root);
                _softPVal = UIHelpers.Txt("SpV", spR.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _softPVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(spR.transform, "SpT", () => { GraphicsSettings.ToggleSoftParticles(); RefreshAll(); }, out _softPTrack, out _softPKnob);

                var aaR = UIHelpers.StatRow("Anti-Aliasing", root);
                _aaVal = UIHelpers.Txt("AaV", aaR.transform, GraphicsSettings.CurrentAaDisplay, 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _aaVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                UIHelpers.ActionBtn(aaR.transform, "Cycle", () => { GraphicsSettings.CycleAntiAliasing(); RefreshAll(); }, 48);

                var sdR = UIHelpers.StatRow("Shadow Distance", root);
                _shadowDistVal = UIHelpers.Txt("SdV", sdR.transform, GraphicsSettings.ShadowDistDisplay, 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _shadowDistVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
                UIHelpers.SmallBtn(sdR.transform, "-", () => { GraphicsSettings.ShadowDistDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(sdR.transform, "+", () => { GraphicsSettings.ShadowDistIncrease(); RefreshAll(); });

                var srR = UIHelpers.StatRow("Shadow Resolution", root);
                _shadowResVal = UIHelpers.Txt("SresV", srR.transform, GraphicsSettings.ShadowResDisplay, 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _shadowResVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
                UIHelpers.ActionBtn(srR.transform, "Cycle", () => { GraphicsSettings.CycleShadowResolution(); RefreshAll(); }, 48);

                var scR = UIHelpers.StatRow("Shadow Cascades", root);
                _cascadesVal = UIHelpers.Txt("ScV", scR.transform, GraphicsSettings.CascadesDisplay, 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _cascadesVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                UIHelpers.ActionBtn(scR.transform, "Cycle", () => { GraphicsSettings.CycleShadowCascades(); RefreshAll(); }, 48);

                UIHelpers.Divider(root);
                UIHelpers.SectionHeader("DETAIL", root);

                var txR = UIHelpers.StatRow("Textures", root);
                _texVal = UIHelpers.Txt("TxV", txR.transform, GraphicsSettings.TextureDisplay, 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _texVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                UIHelpers.ActionBtn(txR.transform, "Cycle", () => { GraphicsSettings.CycleTextureQuality(); RefreshAll(); }, 48);

                var anR = UIHelpers.StatRow("Anisotropic", root);
                _anisoVal = UIHelpers.Txt("AnV", anR.transform, GraphicsSettings.AnisoDisplay, 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _anisoVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
                UIHelpers.ActionBtn(anR.transform, "Cycle", () => { GraphicsSettings.CycleAnisotropic(); RefreshAll(); }, 48);

                var lodR = UIHelpers.StatRow("LOD Bias", root);
                _lodVal = UIHelpers.Txt("LdV", lodR.transform, GraphicsSettings.LodBiasDisplay, 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _lodVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                UIHelpers.SmallBtn(lodR.transform, "-", () => { GraphicsSettings.LodBiasDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(lodR.transform, "+", () => { GraphicsSettings.LodBiasIncrease(); RefreshAll(); });

                var plR = UIHelpers.StatRow("Pixel Lights", root);
                _pixelVal = UIHelpers.Txt("PlV", plR.transform, GraphicsSettings.PixelLightsDisplay, 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _pixelVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.SmallBtn(plR.transform, "-", () => { GraphicsSettings.PixelLightsDecrease(); RefreshAll(); });
                UIHelpers.SmallBtn(plR.transform, "+", () => { GraphicsSettings.PixelLightsIncrease(); RefreshAll(); });

                var vsR = UIHelpers.StatRow("VSync", root);
                _vsyncVal = UIHelpers.Txt("VsV", vsR.transform, GraphicsSettings.VSyncDisplay, 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _vsyncVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
                UIHelpers.ActionBtn(vsR.transform, "Cycle", () => { GraphicsSettings.CycleVSync(); RefreshAll(); }, 48);

                var fpR = UIHelpers.StatRow("FPS Cap", root);
                _fpsVal = UIHelpers.Txt("FpV", fpR.transform, GraphicsSettings.FpsCapDisplay, 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                _fpsVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 52;
                UIHelpers.ActionBtn(fpR.transform, "Cycle", () => { GraphicsSettings.CycleFpsCap(); RefreshAll(); }, 48);

                UIHelpers.InfoBox(root, "Full textures look sharpest. Higher LOD keeps detail visible farther away.");

                UIHelpers.Divider(root);
                UIHelpers.SectionHeader("QUALITY", root);

                var qr = UIHelpers.StatRow("Preset", root);
                UIHelpers.ActionBtn(qr.transform, "Low", () => { GraphicsSettings.SetQuality(0); RefreshAll(); }, 44);
                UIHelpers.ActionBtn(qr.transform, "Medium", () => { GraphicsSettings.SetQuality(1); RefreshAll(); }, 56);
                UIHelpers.ActionBtn(qr.transform, "High", () => { GraphicsSettings.SetQuality(2); RefreshAll(); }, 44);
                UIHelpers.ActionBtn(qr.transform, "Ultra", () => { GraphicsSettings.SetQuality(3); RefreshAll(); }, 44);

                var qvr = UIHelpers.StatRow("Current", root);
                _qualityVal = UIHelpers.Txt("QlV", qvr.transform, "-", 11, FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.Accent);
                _qualityVal.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                UIHelpers.Divider(root);
                UIHelpers.SectionHeader("GAME HUD", root);

                var uir = UIHelpers.StatRow("Hide Game HUD", root);
                _uiRemoverVal = UIHelpers.Txt("UiRV", uir.transform, "OFF", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _uiRemoverVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(uir.transform, "UiRT", () => { UIRemover.Toggle(); RefreshAll(); }, out _uiRemoverTrack, out _uiRemoverKnob);
                FavouritesManager.RegisterStarButton("UIRemover",
                    UIHelpers.StarBtn(uir.transform, "UIRemover", () => FavouritesManager.Toggle("UIRemover")));

                UIHelpers.InfoBox(root,
                    "Hides all game HUD elements (trick feed, score, etc.).\n" +
                    "The mod menu remains visible so you can toggle it back off.");

                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "UIRemover",
                    DisplayName = "Hide Game HUD",
                    TabBadge = "GFX",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "UIRemover", "Hide Game HUD",
                        () => UIRemover.Enabled, () => UIRemover.Toggle(), () => RefreshAll()),
                    IsActive = () => UIRemover.Enabled
                });

                UIHelpers.Divider(root);
                UIHelpers.InfoBox(root, "Effects apply straight away. Quality changes may hitch for a second.");

                RefreshAll();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("GraphicsPage.CreatePage: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "GraphicsPage");
                return null;
            }
            return pg;
        }

        private static void SetToggleUi(Text val, Image track, RectTransform knob, bool on)
        {
            if ((object)val != null) { val.text = on ? "ON" : "OFF"; val.color = on ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(track, knob, on);
        }

        public static void RefreshAll()
        {
            SetToggleUi(_bloomVal, _bloomTrack, _bloomKnob, GraphicsSettings.BloomEnabled);
            SetToggleUi(_aoVal, _aoTrack, _aoKnob, GraphicsSettings.AmbientOccEnabled);
            SetToggleUi(_vigVal, _vigTrack, _vigKnob, GraphicsSettings.VignetteEnabled);
            SetToggleUi(_dofVal, _dofTrack, _dofKnob, GraphicsSettings.DepthOfFieldEnabled);
            SetToggleUi(_cabVal, _cabTrack, _cabKnob, GraphicsSettings.ChromaticAbEnabled);
            SetToggleUi(_eyeVal, _eyeTrack, _eyeKnob, GraphicsSettings.EyeAdaptEnabled);
            SetToggleUi(_cgVal, _cgTrack, _cgKnob, GraphicsSettings.ColorGradingEnabled);
            SetToggleUi(_mbVal, _mbTrack, _mbKnob, GraphicsSettings.MotionBlurEnabled);
            SetToggleUi(_ssrVal, _ssrTrack, _ssrKnob, GraphicsSettings.SsrEnabled);
            SetToggleUi(_ppFogVal, _ppFogTrack, _ppFogKnob, GraphicsSettings.PpFogEnabled);
            SetToggleUi(_grainVal, _grainTrack, _grainKnob, GraphicsSettings.GrainEnabled);
            SetToggleUi(_shadowVal, _shadowTrack, _shadowKnob, GraphicsSettings.ShadowsEnabled);
            SetToggleUi(_softPVal, _softPTrack, _softPKnob, GraphicsSettings.SoftParticlesEnabled);

            if ((object)_aaVal != null) _aaVal.text = GraphicsSettings.CurrentAaDisplay;
            if ((object)_shadowDistVal != null) _shadowDistVal.text = GraphicsSettings.ShadowDistDisplay;
            if ((object)_shadowResVal != null) _shadowResVal.text = GraphicsSettings.ShadowResDisplay;
            if ((object)_cascadesVal != null) _cascadesVal.text = GraphicsSettings.CascadesDisplay;
            if ((object)_texVal != null) _texVal.text = GraphicsSettings.TextureDisplay;
            if ((object)_anisoVal != null) _anisoVal.text = GraphicsSettings.AnisoDisplay;
            if ((object)_lodVal != null) _lodVal.text = GraphicsSettings.LodBiasDisplay;
            if ((object)_pixelVal != null) _pixelVal.text = GraphicsSettings.PixelLightsDisplay;
            if ((object)_vsyncVal != null) _vsyncVal.text = GraphicsSettings.VSyncDisplay;
            if ((object)_fpsVal != null) _fpsVal.text = GraphicsSettings.FpsCapDisplay;

            if ((object)_qualityVal != null)
            {
                int q = GraphicsSettings.GetCurrentQuality();
                if (q == 0) _qualityVal.text = "Low";
                else if (q == 1) _qualityVal.text = "Medium";
                else if (q == 2) _qualityVal.text = "High";
                else if (q == 3) _qualityVal.text = "Ultra";
                else _qualityVal.text = q.ToString();
            }

            SetToggleUi(_uiRemoverVal, _uiRemoverTrack, _uiRemoverKnob, UIRemover.Enabled);
        }
    }
}
