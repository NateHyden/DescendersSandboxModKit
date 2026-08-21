using MelonLoader;
using DescendersModMenu;
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
                Telemetry.ReportErrorAsync(ex, "MapPage");
            }
        }

        public static void RebuildList()
        {
            if (!_listRoot) { _listRoot = null; return; }
            try
            {
                while (_listRoot.childCount > 0)
                    GameObject.DestroyImmediate(_listRoot.GetChild(0).gameObject);

                // ── LOAD FROM SEED ──────────────────────────────
                UIHelpers.SectionHeader("LOAD FROM SEED", _listRoot);

                var curSeedRow = UIHelpers.StatRow("Current Map Seed", _listRoot);
                _currentSeedText = UIHelpers.Txt("CurSeed", curSeedRow.transform, "—",
                    11, FontStyle.Bold, TextAnchor.MiddleRight, UIHelpers.TextMid);
                _currentSeedText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                RefreshCurrentSeed();

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

                _seedCursor = UIHelpers.Txt("SdCur", seedBg.transform, "●",
                    10, FontStyle.Normal, TextAnchor.MiddleCenter, UIHelpers.OnColor);
                _seedCursor.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var scRT = UIHelpers.RT(_seedCursor.gameObject);
                scRT.anchorMin = new Vector2(1, 0); scRT.anchorMax = new Vector2(1, 1);
                scRT.pivot = new Vector2(1, 0.5f);
                scRT.sizeDelta = new Vector2(14, 0);
                scRT.anchoredPosition = new Vector2(-6, 0);
                _seedCursor.gameObject.SetActive(false);

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
                        ModLog.Debug("[MapChanger] Loading seed: " + s);
                        MapChanger.LoadFromSeed(s);
                    }
                }, 52);

                UIHelpers.InfoBox(_listRoot, "Share this number so friends get the same world. Paste a seed and hit Load to ride it.");

                UIHelpers.Divider(_listRoot);

                UIHelpers.SectionHeader("MAP CHANGER", _listRoot);

                var statusRow = UIHelpers.StatRow("Maps", _listRoot);
                _statusText = UIHelpers.Txt("Status", statusRow.transform,
                    MapChanger.HasBikeParks
                        ? "Base + Bike Parks"
                        : "Open Freeride to scan parks",
                    11, FontStyle.Normal, TextAnchor.MiddleRight,
                    MapChanger.HasBikeParks ? UIHelpers.OnColor : UIHelpers.TextDim);
                _statusText.gameObject.AddComponent<LayoutElement>().preferredWidth = 150;

                UIHelpers.Divider(_listRoot);

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

                UIHelpers.SectionHeader("BASE GAME MAPS", _listRoot);
                for (int i = 0; i < MapChanger.Count; i++)
                {
                    try
                    {
                        var entry = MapChanger.GetEntry(i);
                        if (!entry.IsBikePark)
                            BuildMapRow(i);
                    }
                    catch (System.Exception exRow)
                    {
                        LogRowFailure(i, "base map", exRow);
                    }
                }

                if (MapChanger.HasBikeParks)
                {
                    UIHelpers.Divider(_listRoot);
                    UIHelpers.SectionHeader("BIKE PARKS & FREERIDE", _listRoot);
                    for (int i = 0; i < MapChanger.Count; i++)
                    {
                        try
                        {
                            var entry = MapChanger.GetEntry(i);
                            if (entry.IsBikePark)
                                BuildMapRow(i);
                        }
                        catch (System.Exception exRow)
                        {
                            LogRowFailure(i, "bike park", exRow);
                        }
                    }
                }

                UIHelpers.AddScrollForwarders(_listRoot);
            }
            catch (System.Exception ex)
            {
                if (ex is MissingReferenceException || ex is System.NullReferenceException)
                {
                    ClearUiRefs();
                    return;
                }
                LogFullException("MapPage.RebuildList", ex);
            }
        }

        private static void LogFullException(string context, System.Exception ex)
        {
            string msg = string.IsNullOrEmpty(ex.Message) ? "(empty)" : ex.Message;
            MelonLogger.Error("[" + context + "] " + ex.GetType().FullName + ": " + msg);
            Telemetry.ReportErrorAsync(new System.Exception("[" + context + "] " + ex.GetType().FullName + ": " + msg), "MapPage");

            string trace = ex.StackTrace;
            MelonLogger.Error("[" + context + "] StackTrace: " + (string.IsNullOrEmpty(trace) ? "(none)" : trace));
            Telemetry.ReportErrorAsync(new System.Exception("[" + context + "] StackTrace: " + (string.IsNullOrEmpty(trace) ? "(none)" : trace)), "MapPage");

            System.Exception inner = ex.InnerException;
            int depth = 0;
            while (inner != null && depth < 5)
            {
                string innerMsg = string.IsNullOrEmpty(inner.Message) ? "(empty)" : inner.Message;
                MelonLogger.Error("[" + context + "] InnerException[" + depth + "]: " + inner.GetType().FullName + ": " + innerMsg);
                Telemetry.ReportErrorAsync(new System.Exception("[" + context + "] InnerException[" + depth + "]: " + inner.GetType().FullName + ": " + innerMsg), "MapPage");
                if (!string.IsNullOrEmpty(inner.StackTrace))
                    MelonLogger.Error("[" + context + "] InnerException[" + depth + "] StackTrace: " + inner.StackTrace);
                    Telemetry.ReportErrorAsync(new System.Exception("[" + context + "] InnerException[" + depth + "] StackTrace: " + inner.StackTrace), "MapPage");
                inner = inner.InnerException;
                depth++;
            }
        }

        private static void LogRowFailure(int index, string kind, System.Exception ex)
        {
            string name = "?";
            try { name = MapChanger.GetName(index); } catch { }
            MelonLogger.Error("MapPage.RebuildList: row " + index + " (" + kind + ", name=\"" + name + "\") failed to build - continuing with the rest of the list.");
            Telemetry.ReportErrorAsync(new System.Exception("MapPage.RebuildList: row " + index + " (" + kind + ", name=\"" + name + "\") failed to build - continuing with the rest of the list."), "MapPage");
            LogFullException("MapPage.RebuildList row " + index, ex);
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
            if (!_seedInputText) return;

            if (_seedFocused && Input.GetMouseButtonDown(0))
            {
                if (_seedBoxRect
                    && !RectTransformUtility.RectangleContainsScreenPoint(_seedBoxRect, Input.mousePosition, null))
                    _seedFocused = false;
            }

            if (_seedCursor)
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
                        if (_seedInputText) { _seedInputText.text = "Enter seed number..."; _seedInputText.color = UIHelpers.TextDim; }
                        ModLog.Debug("[MapChanger] Loading seed via Enter: " + s);
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

        public static void ClearUiRefs()
        {
            _listRoot = null;
            _statusText = null;
            _seedInputText = null;
            _seedCursor = null;
            _seedBoxRect = null;
            _currentSeedText = null;
            _seedFocused = false;
            _seedBuffer = "";
        }

        private static void RefreshCurrentSeed()
        {
            if (!_currentSeedText) return;

            string liveSeed = MapChanger.GetCurrentLevelSeed();
            if (!string.IsNullOrEmpty(liveSeed))
            {
                _currentSeedText.text = liveSeed;
                _currentSeedText.color = UIHelpers.Accent;
            }
            else
            {
                _currentSeedText.text = "- not in a session";
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

