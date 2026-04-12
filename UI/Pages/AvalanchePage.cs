using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    public static class AvalanchePage
    {
        // Toggle refs
        private static Image _enableTrack; private static RectTransform _enableKnob;
        private static Image _failTrack; private static RectTransform _failKnob;
        private static Image _diffTrack; private static RectTransform _diffKnob;
        private static Image _timerTrack; private static RectTransform _timerKnob;

        // Live labels
        private static Text _timerText = null;
        private static Text _activeText = null;

        public static void CreatePage(Transform parent)
        {
            try
            {
                var pg = UIHelpers.Obj("P13R", parent);
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
                UIHelpers.SectionHeader("AVALANCHE MODE", c);

                // Enable toggle
                var enableRow = UIHelpers.StatRow("Enable", c);
                UIHelpers.Toggle(enableRow.transform, "AvalEnable",
                    () => { AvalancheMode.Toggle(); RefreshAll(); },
                    out _enableTrack, out _enableKnob);

                // Survival + active count rows
                var timerRow = UIHelpers.StatRow("Survival Time", c);
                _timerText = UIHelpers.Txt("ATm", timerRow.transform,
                    "0:00", 12, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.Accent);
                _timerText.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;

                var activeRow = UIHelpers.StatRow("Active Hazards", c);
                _activeText = UIHelpers.Txt("AAc", activeRow.transform,
                    "0 / 3", 12, FontStyle.Normal, TextAnchor.MiddleRight, UIHelpers.TextMid);
                _activeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;

                UIHelpers.Divider(c);

                // ── SPAWN SETTINGS ────────────────────────────────────
                UIHelpers.SectionHeader("SPAWN SETTINGS", c);

                MakeCycleRow(c, "Max Hazards",
                    new string[] { "1", "2", "3", "5", "8" },
                    new float[] { 1, 2, 3, 5, 8 }, 2,
                    v => AvalancheMode.MaxHazards = (int)v);

                MakeCycleRow(c, "Spawn Interval",
                    new string[] { "2s Fast", "4s Normal", "6s Slow", "8s V.Slow" },
                    new float[] { 2f, 4f, 6f, 8f }, 1,
                    v => AvalancheMode.SpawnInterval = v);

                MakeCycleRow(c, "Hazard Size",
                    new string[] { "1m Small", "2m Medium", "3m Large", "5m Massive" },
                    new float[] { 1f, 2f, 3f, 5f }, 1,
                    v => AvalancheMode.HazardSize = v);

                MakeCycleRow(c, "Spawn Distance",
                    new string[] { "10m Near", "15m Med", "25m Far" },
                    new float[] { 10f, 15f, 25f }, 1,
                    v => AvalancheMode.SpawnDistance = v);

                MakeCycleRow(c, "Spawn Radius",
                    new string[] { "4m Tight", "7m Normal", "12m Wide" },
                    new float[] { 4f, 7f, 12f }, 1,
                    v => AvalancheMode.SpawnRadius = v);

                MakeCycleRow(c, "Spawn Height",
                    new string[] { "8m Low", "20m Normal", "35m High" },
                    new float[] { 8f, 20f, 35f }, 1,
                    v => AvalancheMode.SpawnHeight = v);

                UIHelpers.Divider(c);

                // ── PHYSICS ───────────────────────────────────────────
                UIHelpers.SectionHeader("PHYSICS SETTINGS", c);

                MakeCycleRow(c, "Extra Gravity",
                    new string[] { "8 Low", "18 Medium", "30 High", "50 Extreme" },
                    new float[] { 8f, 18f, 30f, 50f }, 1,
                    v => AvalancheMode.ExtraGravity = v);

                MakeCycleRow(c, "Forward Push",
                    new string[] { "None", "6 Light", "10 Medium", "16 Heavy" },
                    new float[] { 0f, 6f, 10f, 16f }, 2,
                    v => AvalancheMode.ForwardImpulse = v);

                // Shape toggle row
                var shapeRow = UIHelpers.StatRow("Shape", c);
                var shapeLbl = UIHelpers.Txt("ShpLbl", shapeRow.transform,
                    "SPHERE", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
                shapeLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 70;
                var shapeBtn = UIHelpers.Btn("ShpBtn", shapeRow.transform, "TOGGLE",
                    new Vector2(80, 30), 11,
                    () => {
                        AvalancheMode.UseBox = !AvalancheMode.UseBox;
                        shapeLbl.text = AvalancheMode.UseBox ? "BOX" : "SPHERE";
                        shapeLbl.color = AvalancheMode.UseBox ? UIHelpers.Orange : UIHelpers.Accent;
                    }, UIHelpers.BtnBg, UIHelpers.TextLight);
                var shapeLe = shapeBtn.gameObject.AddComponent<LayoutElement>();
                shapeLe.preferredWidth = 80; shapeLe.minWidth = 80;
                shapeLe.preferredHeight = 30; shapeLe.minHeight = 30;

                UIHelpers.Divider(c);

                // ── GAMEPLAY ──────────────────────────────────────────
                UIHelpers.SectionHeader("GAMEPLAY", c);

                var failRow = UIHelpers.StatRow("Instant Fail on Hit", c);
                UIHelpers.Toggle(failRow.transform, "InstFail",
                    () => { AvalancheMode.InstantFail = !AvalancheMode.InstantFail; RefreshAll(); },
                    out _failTrack, out _failKnob);

                var diffRow = UIHelpers.StatRow("Difficulty Scaling", c);
                UIHelpers.Toggle(diffRow.transform, "DiffScale",
                    () => { AvalancheMode.DifficultyScale = !AvalancheMode.DifficultyScale; RefreshAll(); },
                    out _diffTrack, out _diffKnob);

                var tmrRow = UIHelpers.StatRow("Show Timer", c);
                UIHelpers.Toggle(tmrRow.transform, "ShowTmr",
                    () => { AvalancheMode.ShowTimer = !AvalancheMode.ShowTimer; RefreshAll(); },
                    out _timerTrack, out _timerKnob);

                UIHelpers.Divider(c);

                // Clear button
                var clearRow = UIHelpers.StatRow("", c);
                var clearBtn = UIHelpers.Btn("ClrBtn", clearRow.transform, "CLEAR ALL HAZARDS",
                    new Vector2(180, 32), 12,
                    () => { AvalancheMode.ClearAll(); RefreshAll(); },
                    UIHelpers.Orange, Color.black);
                var clearLe = clearBtn.gameObject.AddComponent<LayoutElement>();
                clearLe.preferredWidth = 180; clearLe.minWidth = 180;
                clearLe.preferredHeight = 32; clearLe.minHeight = 32;

                UIHelpers.AddScrollForwarders(c);
                RefreshAll();
            }
            catch (System.Exception ex) { MelonLogger.Error("AvalanchePage: " + ex.Message); }
        }

        // ── Cycle row: Label  [<]  Value  [>] ─────────────────────────
        private static void MakeCycleRow(Transform parent, string label,
            string[] opts, float[] vals, int defIdx, System.Action<float> onChange)
        {
            int[] idx = new int[] { defIdx };
            var row = UIHelpers.StatRow(label, parent);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childForceExpandWidth = false;

            // < button
            var prev = UIHelpers.Btn("Prev", row.transform, "<",
                new Vector2(28, 28), 14, () => { },
                UIHelpers.BtnBg, UIHelpers.TextLight);
            var prevLe = prev.gameObject.AddComponent<LayoutElement>();
            prevLe.preferredWidth = 28; prevLe.minWidth = 28;
            prevLe.preferredHeight = 28; prevLe.minHeight = 28;

            // Value label
            var val = UIHelpers.Txt("Val", row.transform, opts[defIdx],
                11, FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.Accent);
            var valLe = val.gameObject.AddComponent<LayoutElement>();
            valLe.preferredWidth = 100; valLe.minWidth = 80;

            // > button
            var next = UIHelpers.Btn("Next", row.transform, ">",
                new Vector2(28, 28), 14, () => { },
                UIHelpers.BtnBg, UIHelpers.TextLight);
            var nextLe = next.gameObject.AddComponent<LayoutElement>();
            nextLe.preferredWidth = 28; nextLe.minWidth = 28;
            nextLe.preferredHeight = 28; nextLe.minHeight = 28;

            // Wire up after all refs are captured
            prev.onClick.AddListener(() => {
                idx[0] = (idx[0] - 1 + opts.Length) % opts.Length;
                val.text = opts[idx[0]];
                onChange(vals[idx[0]]);
            });
            next.onClick.AddListener(() => {
                idx[0] = (idx[0] + 1) % opts.Length;
                val.text = opts[idx[0]];
                onChange(vals[idx[0]]);
            });
        }

        // ── Tick ──────────────────────────────────────────────────────
        public static void Tick()
        {
            if ((object)_activeText != null)
                _activeText.text = AvalancheMode.ActiveCount + " / " + AvalancheMode.MaxHazards;

            if ((object)_timerText != null)
            {
                if (AvalancheMode.Enabled && AvalancheMode.ShowTimer)
                {
                    int m = (int)(AvalancheMode.SurvivalTime / 60f);
                    int s = (int)(AvalancheMode.SurvivalTime % 60f);
                    _timerText.text = m + ":" + s.ToString("D2");
                }
                else
                {
                    _timerText.text = "0:00";
                }
            }
        }

        public static void RefreshAll()
        {
            UIHelpers.SetToggle(_enableTrack, _enableKnob, AvalancheMode.Enabled);
            UIHelpers.SetToggle(_failTrack, _failKnob, AvalancheMode.InstantFail);
            UIHelpers.SetToggle(_diffTrack, _diffKnob, AvalancheMode.DifficultyScale);
            UIHelpers.SetToggle(_timerTrack, _timerKnob, AvalancheMode.ShowTimer);
        }
    }
}