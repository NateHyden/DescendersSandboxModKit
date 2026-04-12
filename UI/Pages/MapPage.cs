using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    public static class MapPage
    {
        private static Transform _listRoot = null;
        private static Text _statusText = null;

        // ── Seed input ────────────────────────────────────────────
        private static string _seedBuffer = "";
        private static bool _seedFocused = false;
        public static bool IsSeedFocused => _seedFocused;
        private static Text _seedInputText = null;
        private static Text _seedCursor = null;
        private static Text _currentSeedText = null;
        private static RectTransform _seedBoxRect = null;

        public static void CreatePage(Transform parent)
        {
            try
            {
                var pg = UIHelpers.Obj("P15R", parent);
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
                content.AddComponent<ContentSizeFitter>().verticalFit =
                    ContentSizeFitter.FitMode.PreferredSize;
                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset(
                    (int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                _listRoot = content.transform;

                RebuildList();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("MapPage.CreatePage: " + ex.Message);
            }
        }

        public static void RebuildList()
        {
            if ((object)_listRoot == null) return;
            try
            {
                // Clear everything
                while (_listRoot.childCount > 0)
                    GameObject.DestroyImmediate(_listRoot.GetChild(0).gameObject);

                // ── LOAD FROM SEED ──────────────────────────────
                UIHelpers.SectionHeader("LOAD FROM SEED", _listRoot);

                // Current seed display
                var curSeedRow = UIHelpers.StatRow("Current Map Seed", _listRoot);
                _currentSeedText = UIHelpers.Txt("CurSeed", curSeedRow.transform, "—",
                    11, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.TextMid);
                _currentSeedText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                RefreshCurrentSeed();

                // Seed input row
                var seedInputRow = UIHelpers.Obj("SeedInputRow", _listRoot);
                seedInputRow.AddComponent<Image>().color = UIHelpers.RowBg;
                var sirLe = seedInputRow.AddComponent<LayoutElement>();
                sirLe.preferredHeight = 36; sirLe.minHeight = 36;
                var sirHlg = seedInputRow.AddComponent<HorizontalLayoutGroup>();
                sirHlg.padding = new RectOffset(8, 8, 4, 4);
                sirHlg.spacing = 6; sirHlg.childAlignment = TextAnchor.MiddleLeft;
                sirHlg.childForceExpandHeight = true; sirHlg.childForceExpandWidth = false;

                var seedBg = UIHelpers.Obj("SdBg", seedInputRow.transform);
                seedBg.AddComponent<Image>().color = UIHelpers.WinOuter;
                var sbgLe = seedBg.AddComponent<LayoutElement>();
                sbgLe.flexibleWidth = 1; sbgLe.minHeight = 26; sbgLe.preferredHeight = 26;
                var sbgHlg = seedBg.AddComponent<HorizontalLayoutGroup>();
                sbgHlg.padding = new RectOffset(8, 8, 0, 0);
                sbgHlg.childAlignment = TextAnchor.MiddleLeft;
                sbgHlg.childForceExpandWidth = true; sbgHlg.childForceExpandHeight = true;

                _seedInputText = UIHelpers.Txt("SdIT", seedBg.transform, "Enter seed number...",
                    11, FontStyle.Normal, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                _seedInputText.horizontalOverflow = HorizontalWrapMode.Overflow;
                _seedInputText.verticalOverflow = VerticalWrapMode.Truncate;
                _seedInputText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                // Flashing cursor
                _seedCursor = UIHelpers.Txt("SdCur", seedBg.transform, "●",
                    10, FontStyle.Normal, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _seedCursor.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var scRT = UIHelpers.RT(_seedCursor.gameObject);
                scRT.anchorMin = new Vector2(1, 0); scRT.anchorMax = new Vector2(1, 1);
                scRT.pivot = new Vector2(1, 0.5f);
                scRT.sizeDelta = new Vector2(14, 0);
                scRT.anchoredPosition = new Vector2(-6, 0);
                _seedCursor.gameObject.SetActive(false);

                // Click to focus
                _seedBoxRect = UIHelpers.RT(seedBg);
                var seedFocusBtn = seedBg.AddComponent<UnityEngine.UI.Button>();
                seedFocusBtn.targetGraphic = seedBg.GetComponent<Image>();
                seedFocusBtn.onClick.AddListener(() => { _seedFocused = true; });

                UIHelpers.ActionBtn(seedInputRow.transform, "Load", () =>
                {
                    string s = _seedBuffer.Trim();
                    if (!string.IsNullOrEmpty(s))
                    {
                        _seedBuffer = "";
                        _seedFocused = false;
                        if ((object)_seedInputText != null) { _seedInputText.text = "Enter seed number..."; _seedInputText.color = UIHelpers.TextDim; }
                        MelonLogger.Msg("[MapChanger] Loading seed: " + s);
                        MapChanger.LoadFromSeed(s);
                    }
                }, 52);

                UIHelpers.InfoBox(_listRoot, "Session Seed: share this number so friends can ride the same world. Paste any seed and press Load for freeride.");

                UIHelpers.Divider(_listRoot);

                UIHelpers.SectionHeader("MAP CHANGER", _listRoot);

                // Status row
                var statusRow = UIHelpers.StatRow("Maps", _listRoot);
                _statusText = UIHelpers.Txt("Status", statusRow.transform,
                    MapChanger.HasBikeParks
                        ? "Base + Bike Parks"
                        : "Open Freeride to scan parks",
                    11, FontStyle.Normal, TextAnchor.MiddleRight,
                    MapChanger.HasBikeParks ? UIHelpers.OnColor : UIHelpers.TextDim);
                _statusText.gameObject.AddComponent<LayoutElement>().preferredWidth = 150;

                UIHelpers.Divider(_listRoot);

                // Hint if bike parks not yet scanned
                if (!MapChanger.HasBikeParks)
                {
                    var hintRow = UIHelpers.Obj("HintRow", _listRoot);
                    hintRow.AddComponent<LayoutElement>().minHeight = 28;
                    var htxt = UIHelpers.Txt("HintTxt", hintRow.transform,
                        "Go to  Ride \u2192 Bike Parks  once to load all parks into this list",
                        10, FontStyle.Normal, TextAnchor.MiddleCenter, UIHelpers.TextDim);
                    htxt.horizontalOverflow = HorizontalWrapMode.Wrap;
                    UIHelpers.Fill(UIHelpers.RT(htxt.gameObject));
                    UIHelpers.Divider(_listRoot);
                }

                // Base worlds section
                UIHelpers.SectionHeader("BASE GAME MAPS", _listRoot);
                for (int i = 0; i < MapChanger.Count; i++)
                {
                    var entry = MapChanger.GetEntry(i);
                    if (!entry.IsBikePark)
                        BuildMapRow(i);
                }

                // Bike parks section — only if found
                if (MapChanger.HasBikeParks)
                {
                    UIHelpers.Divider(_listRoot);
                    UIHelpers.SectionHeader("BIKE PARKS & FREERIDE", _listRoot);
                    for (int i = 0; i < MapChanger.Count; i++)
                    {
                        var entry = MapChanger.GetEntry(i);
                        if (entry.IsBikePark)
                            BuildMapRow(i);
                    }
                }

                UIHelpers.AddScrollForwarders(_listRoot);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("MapPage.RebuildList: " + ex.Message);
            }
        }

        private static void BuildMapRow(int i)
        {
            int idx = i;
            var row = UIHelpers.StatRow(MapChanger.GetName(i), _listRoot);

            var goBtn = UIHelpers.Btn("Go" + i, row.transform, "GO",
                new Vector2(52, 30), 12,
                () =>
                {
                    SetStatus("LOADING " + MapChanger.GetName(idx) + "...", UIHelpers.Orange);
                    MapChanger.GoToMap(idx);
                },
                UIHelpers.Orange, Color.black);

            var le = goBtn.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 52; le.minWidth = 52;
            le.preferredHeight = 30; le.minHeight = 30;
        }

        private static void SetStatus(string msg, Color col)
        {
            if ((object)_statusText == null) return;
            _statusText.text = msg;
            _statusText.color = col;
        }

        public static void SeedTick()
        {
            if ((object)_seedInputText == null) return;

            // Click-away to unfocus
            if (_seedFocused && Input.GetMouseButtonDown(0))
            {
                if ((object)_seedBoxRect != null)
                {
                    Vector2 mp = Input.mousePosition;
                    if (!RectTransformUtility.RectangleContainsScreenPoint(_seedBoxRect, mp, null))
                        _seedFocused = false;
                }
            }

            // Cursor pulse
            if ((object)_seedCursor != null)
            {
                _seedCursor.gameObject.SetActive(_seedFocused);
                if (_seedFocused)
                {
                    float alpha = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
                    Color col = UIHelpers.OnColor;
                    col.a = alpha;
                    _seedCursor.color = col;
                }
            }

            // Update current seed display each frame
            RefreshCurrentSeed();

            if (!_seedFocused) return;

            foreach (char ch in Input.inputString)
            {
                if (ch == '\b') { if (_seedBuffer.Length > 0) _seedBuffer = _seedBuffer.Substring(0, _seedBuffer.Length - 1); }
                else if (ch == '\n' || ch == '\r')
                {
                    string s = _seedBuffer.Trim();
                    if (!string.IsNullOrEmpty(s))
                    {
                        _seedBuffer = "";
                        _seedFocused = false;
                        if ((object)_seedInputText != null) { _seedInputText.text = "Enter seed number..."; _seedInputText.color = UIHelpers.TextDim; }
                        MelonLogger.Msg("[MapChanger] Loading seed via Enter: " + s);
                        MapChanger.LoadFromSeed(s);
                    }
                    return;
                }
                else if (ch == (char)27) { _seedFocused = false; return; }
                else if (_seedBuffer.Length < 20) _seedBuffer += ch;
            }

            if (_seedBuffer.Length > 0)
            {
                _seedInputText.text = _seedBuffer;
                _seedInputText.color = UIHelpers.TextLight;
            }
            else
            {
                _seedInputText.text = "Enter seed number...";
                _seedInputText.color = UIHelpers.TextDim;
            }
        }

        private static void RefreshCurrentSeed()
        {
            if ((object)_currentSeedText == null) return;

            // Try to read the live session seed first — works regardless of how
            // the map was loaded (normal session, friend's world, or mod load)
            string liveSeed = MapChanger.GetCurrentLevelSeed();
            if (!string.IsNullOrEmpty(liveSeed))
            {
                _currentSeedText.text = liveSeed;
                _currentSeedText.color = UIHelpers.Accent;
            }
            else
            {
                _currentSeedText.text = "— not in a session";
                _currentSeedText.color = UIHelpers.TextDim;
            }
        }

        public static void RefreshAll()
        {
            MapChanger.BuildMapList();
            RebuildList();
        }
    }
}