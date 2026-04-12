using DescendersModMenu.Mods;
using MelonLoader;
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
        private static Text _qualityVal;
        private static Text _uiRemoverVal;
        private static Image _uiRemoverTrack; private static RectTransform _uiRemoverKnob;

        public static bool IsAnyActive =>
            !GraphicsSettings.BloomEnabled || !GraphicsSettings.AmbientOccEnabled ||
            !GraphicsSettings.VignetteEnabled || GraphicsSettings.DepthOfFieldEnabled ||
            !GraphicsSettings.ChromaticAbEnabled || UIRemover.Enabled;

        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("P10R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));
                var vlg = pg.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                // ── Post Processing ───────────────────────────────────────
                UIHelpers.SectionHeader("POST PROCESSING", pg.transform);

                var br = UIHelpers.StatRow("Bloom", pg.transform);
                _bloomVal = UIHelpers.Txt("BlV", br.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _bloomVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(br.transform, "BlT", () => { GraphicsSettings.ToggleBloom(); RefreshAll(); }, out _bloomTrack, out _bloomKnob);

                var aor = UIHelpers.StatRow("Ambient Occlusion", pg.transform);
                _aoVal = UIHelpers.Txt("AoV", aor.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _aoVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(aor.transform, "AoT", () => { GraphicsSettings.ToggleAO(); RefreshAll(); }, out _aoTrack, out _aoKnob);

                var vigr = UIHelpers.StatRow("Vignette", pg.transform);
                _vigVal = UIHelpers.Txt("VgV", vigr.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _vigVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(vigr.transform, "VgT", () => { GraphicsSettings.ToggleVignette(); RefreshAll(); }, out _vigTrack, out _vigKnob);

                var dofr = UIHelpers.StatRow("Depth of Field", pg.transform);
                _dofVal = UIHelpers.Txt("DfV", dofr.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _dofVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(dofr.transform, "DfT", () => { GraphicsSettings.ToggleDOF(); RefreshAll(); }, out _dofTrack, out _dofKnob);

                var cabr = UIHelpers.StatRow("Chromatic Aberration", pg.transform);
                _cabVal = UIHelpers.Txt("CaV", cabr.transform, "ON", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _cabVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(cabr.transform, "CaT", () => { GraphicsSettings.ToggleChromatic(); RefreshAll(); }, out _cabTrack, out _cabKnob);

                UIHelpers.Divider(pg.transform);

                // ── Quality ───────────────────────────────────────────────
                UIHelpers.SectionHeader("QUALITY", pg.transform);

                var qr = UIHelpers.StatRow("Preset", pg.transform);
                UIHelpers.ActionBtn(qr.transform, "Low", () => { GraphicsSettings.SetQuality(0); RefreshAll(); }, 44);
                UIHelpers.ActionBtn(qr.transform, "Medium", () => { GraphicsSettings.SetQuality(1); RefreshAll(); }, 56);
                UIHelpers.ActionBtn(qr.transform, "High", () => { GraphicsSettings.SetQuality(2); RefreshAll(); }, 44);
                UIHelpers.ActionBtn(qr.transform, "Ultra", () => { GraphicsSettings.SetQuality(3); RefreshAll(); }, 44);
                UIHelpers.ActionBtn(qr.transform, "Default", () => { GraphicsSettings.RestoreDefaultQuality(); RefreshAll(); }, 54);

                var qvr = UIHelpers.StatRow("Current", pg.transform);
                _qualityVal = UIHelpers.Txt("QlV", qvr.transform, "—", 11, FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.Accent);
                _qualityVal.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                UIHelpers.Divider(pg.transform);

                // ── GAME HUD ──────────────────────────────────────────────
                UIHelpers.SectionHeader("GAME HUD", pg.transform);

                var uir = UIHelpers.StatRow("Hide Game HUD", pg.transform);
                _uiRemoverVal = UIHelpers.Txt("UiRV", uir.transform, "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.OffColor);
                _uiRemoverVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(uir.transform, "UiRT",
                    () => { UIRemover.Toggle(); RefreshAll(); },
                    out _uiRemoverTrack, out _uiRemoverKnob);
                FavouritesManager.RegisterStarButton("UIRemover",
                    UIHelpers.StarBtn(uir.transform, "UIRemover",
                        () => FavouritesManager.Toggle("UIRemover")));

                UIHelpers.InfoBox(pg.transform,
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

                UIHelpers.Divider(pg.transform);

                UIHelpers.InfoBox(pg.transform, "Post processing changes take effect immediately. Quality changes may cause a brief stutter.");

                RefreshAll();
            }
            catch (System.Exception ex) { MelonLogger.Error("GraphicsPage.CreatePage: " + ex.Message); return null; }
            return pg;
        }

        public static void RefreshAll()
        {
            bool bloom = GraphicsSettings.BloomEnabled;
            if (_bloomVal) { _bloomVal.text = bloom ? "ON" : "OFF"; _bloomVal.color = bloom ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_bloomTrack, _bloomKnob, bloom);

            bool ao = GraphicsSettings.AmbientOccEnabled;
            if (_aoVal) { _aoVal.text = ao ? "ON" : "OFF"; _aoVal.color = ao ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_aoTrack, _aoKnob, ao);

            bool vig = GraphicsSettings.VignetteEnabled;
            if (_vigVal) { _vigVal.text = vig ? "ON" : "OFF"; _vigVal.color = vig ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_vigTrack, _vigKnob, vig);

            bool dof = GraphicsSettings.DepthOfFieldEnabled;
            if (_dofVal) { _dofVal.text = dof ? "ON" : "OFF"; _dofVal.color = dof ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_dofTrack, _dofKnob, dof);

            bool cab = GraphicsSettings.ChromaticAbEnabled;
            if (_cabVal) { _cabVal.text = cab ? "ON" : "OFF"; _cabVal.color = cab ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_cabTrack, _cabKnob, cab);

            if (_qualityVal)
            {
                int q = GraphicsSettings.GetCurrentQuality();
                string[] names = { "Low", "Medium", "High", "Ultra" };
                _qualityVal.text = (q >= 0 && q < names.Length) ? names[q] : QualitySettings.names[q];
            }

            bool uiRem = UIRemover.Enabled;
            if (_uiRemoverVal) { _uiRemoverVal.text = uiRem ? "ON" : "OFF"; _uiRemoverVal.color = uiRem ? UIHelpers.OnColor : UIHelpers.OffColor; }
            UIHelpers.SetToggle(_uiRemoverTrack, _uiRemoverKnob, uiRem);
        }
    }
}