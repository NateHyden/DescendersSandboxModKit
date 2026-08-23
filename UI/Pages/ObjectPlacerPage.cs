using DescendersModMenu.Mods;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class ObjectPlacerPage
    {
        private static Transform _listRoot = null;
        private static Text _placeVal;
        private static Image _placeTrack;
        private static RectTransform _placeKnob;
        private static Text _autoVal;
        private static Image _autoTrack;
        private static RectTransform _autoKnob;
        private static Text _statusVal;
        private static Image _moveBar, _rotBar, _liftBar, _camBar, _scaleBar;
        private static Text _moveVal, _rotVal, _liftVal, _camVal, _scaleVal;
        private static Text _renameText;
        private static int _renamingIndex = -1;
        private static string _renameBuffer = "";

        public static bool IsAnyActive => ObjectPlacer.IsAnyActive;
        public static bool IsRenaming => _renamingIndex >= 0;

        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("P24R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));

                var scrollObj = UIHelpers.Obj("Scroll", pg.transform);
                UIHelpers.Fill(UIHelpers.RT(scrollObj));
                var scrollRect = scrollObj.AddComponent<ScrollRect>();
                scrollRect.horizontal = false; scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 25f;
                scrollRect.inertia = false;

                var vp = UIHelpers.Obj("VP", scrollObj.transform);
                UIHelpers.Fill(UIHelpers.RT(vp));
                vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                vp.AddComponent<Mask>().showMaskGraphic = true;
                scrollRect.viewport = UIHelpers.RT(vp);

                var content = UIHelpers.Obj("Content", vp.transform);
                var crt = UIHelpers.RT(content);
                crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1);
                crt.sizeDelta = new Vector2(0, 0);
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

                _listRoot = content.transform;

                FavouritesManager.Register(new ModFavEntry
                {
                    Id = "ObjectPlacer",
                    DisplayName = "Object Placer",
                    TabBadge = "PLACE",
                    BuildControls = (p) => FavsPage.BuildSimpleToggle(p, "ObjectPlacer", "Object Placer",
                        () => ObjectPlacer.Enabled, () => { ObjectPlacer.Toggle(); }, () => RefreshAll()),
                    IsActive = () => ObjectPlacer.IsAnyActive
                });

                RebuildList();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("ObjectPlacerPage.CreatePage: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "ObjectPlacerPage");
                return null;
            }
            return pg;
        }

        public static void RebuildList()
        {
            if (!_listRoot) { _listRoot = null; return; }
            _renameText = null;
            try
            {
                while (_listRoot.childCount > 0)
                    GameObject.DestroyImmediate(_listRoot.GetChild(0).gameObject);

                var c = _listRoot;

                var rst = UIHelpers.BareBtnRow(c);
                UIHelpers.ActionBtnOrange(rst.transform, "↺  Reset Placed", () => { ObjectPlacer.ClearAll(); RefreshAll(); }, 140);

                UIHelpers.SectionHeader("PLACE", c);

                var pr = UIHelpers.StatRow("Place Objects", c);
                _placeVal = UIHelpers.Txt("OpV", pr.transform, ObjectPlacer.Enabled ? "ON" : "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter,
                    ObjectPlacer.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor);
                _placeVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(pr.transform, "OpT", () =>
                {
                    ObjectPlacer.Toggle();
                    RefreshAll();
                }, out _placeTrack, out _placeKnob);
                FavouritesManager.RegisterStarButton("ObjectPlacer",
                    UIHelpers.StarBtn(pr.transform, "ObjectPlacer", () => FavouritesManager.Toggle("ObjectPlacer")));

                var ar = UIHelpers.StatRow("Autoclose Menu on B/Esc", c);
                _autoVal = UIHelpers.Txt("OpAV", ar.transform, ObjectPlacer.AutoCloseMenu ? "ON" : "OFF", 11,
                    FontStyle.Bold, TextAnchor.MiddleCenter,
                    ObjectPlacer.AutoCloseMenu ? UIHelpers.OnColor : UIHelpers.OffColor);
                _autoVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(ar.transform, "OpAT", () =>
                {
                    ObjectPlacer.ToggleAutoCloseMenu();
                    RefreshHeader();
                }, out _autoTrack, out _autoKnob);

                var camr = UIHelpers.StatRow("Camera Distance", c);
                _camBar = UIHelpers.MakeBar("CmB", camr.transform, (ObjectPlacer.CamDistanceLevel - 1) / 9f);
                _camVal = UIHelpers.Txt("CmV", camr.transform, ObjectPlacer.CamDistanceDisplay, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _camVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 40;
                UIHelpers.SmallBtn(camr.transform, "-", () => { ObjectPlacer.BumpCamDistance(-1); RefreshHeader(); });
                UIHelpers.SmallBtn(camr.transform, "+", () => { ObjectPlacer.BumpCamDistance(1); RefreshHeader(); });

                var scR = UIHelpers.StatRow("Mesh Scale", c);
                _scaleBar = UIHelpers.MakeBar("ScB", scR.transform, (ObjectPlacer.MeshScaleLevel - 1) / 9f);
                _scaleVal = UIHelpers.Txt("ScV", scR.transform, ObjectPlacer.MeshScaleDisplay, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _scaleVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 40;
                UIHelpers.SmallBtn(scR.transform, "-", () => { ObjectPlacer.BumpMeshScale(-1); RefreshHeader(); });
                UIHelpers.SmallBtn(scR.transform, "+", () => { ObjectPlacer.BumpMeshScale(1); RefreshHeader(); });

                var lib = UIHelpers.StatRow("Library", c);
                UIHelpers.ActionBtn(lib.transform, "Scan Map", () => { ObjectPlacer.ScanMap(); RebuildList(); }, 70);
                UIHelpers.ActionBtnOrange(lib.transform, "Forget", () => { ObjectPlacer.ClearHarvested(); RebuildList(); }, 56);

                _statusVal = UIHelpers.Txt("OpSt", c, StatusLine(), 10,
                    FontStyle.Italic, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                _statusVal.gameObject.AddComponent<LayoutElement>().preferredHeight = 18;

                UIHelpers.InfoBox(c, "Scan a Bike Park, star objects you like, then place them. Favourites save across maps. Mesh Scale is session-only (resets each map). Stick moves / place with A / exit with B.", Color.white);

                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("FAVOURITES", c);
                int favs = ObjectPlacer.FavCount;
                if (favs == 0)
                {
                    UIHelpers.InfoBox(c, "No favourites yet — tap the star on an object below to pin it here.");
                }
                else
                {
                    UIHelpers.InfoBox(c,
                        "Click a favourite name to rename · Enter to save · Esc to cancel",
                        Color.white);

                    int totalFavScan = ObjectPlacer.CatalogCount;
                    for (int i = 0; i < totalFavScan; i++)
                    {
                        if (ObjectPlacer.IsFavAt(i))
                            AddObjectRow(c, i, true);
                    }
                }

                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("SPEED", c);

                var mr = UIHelpers.StatRow("Move", c);
                _moveBar = UIHelpers.MakeBar("MvB", mr.transform, (ObjectPlacer.MoveSpeedLevel - 1) / 9f);
                _moveVal = UIHelpers.Txt("MvV", mr.transform, ObjectPlacer.MoveSpeedDisplay, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _moveVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                UIHelpers.SmallBtn(mr.transform, "-", () => { ObjectPlacer.BumpMove(-1); RefreshHeader(); });
                UIHelpers.SmallBtn(mr.transform, "+", () => { ObjectPlacer.BumpMove(1); RefreshHeader(); });

                var rr = UIHelpers.StatRow("Rotate", c);
                _rotBar = UIHelpers.MakeBar("RtB", rr.transform, (ObjectPlacer.RotateSpeedLevel - 1) / 9f);
                _rotVal = UIHelpers.Txt("RtV", rr.transform, ObjectPlacer.RotateSpeedDisplay, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _rotVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                UIHelpers.SmallBtn(rr.transform, "-", () => { ObjectPlacer.BumpRotate(-1); RefreshHeader(); });
                UIHelpers.SmallBtn(rr.transform, "+", () => { ObjectPlacer.BumpRotate(1); RefreshHeader(); });

                var lr = UIHelpers.StatRow("Lift", c);
                _liftBar = UIHelpers.MakeBar("LfB", lr.transform, (ObjectPlacer.LiftSpeedLevel - 1) / 9f);
                _liftVal = UIHelpers.Txt("LfV", lr.transform, ObjectPlacer.LiftSpeedDisplay, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter, UIHelpers.TextMid);
                _liftVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 36;
                UIHelpers.SmallBtn(lr.transform, "-", () => { ObjectPlacer.BumpLift(-1); RefreshHeader(); });
                UIHelpers.SmallBtn(lr.transform, "+", () => { ObjectPlacer.BumpLift(1); RefreshHeader(); });

                UIHelpers.Divider(c);
                int mapCount = 0;
                int total = ObjectPlacer.CatalogCount;
                for (int i = 0; i < total; i++)
                    if (ObjectPlacer.IsHarvestedAt(i)) mapCount++;

                UIHelpers.SectionHeader("MAP OBJECTS  (" + mapCount + ")", c);
                if (mapCount == 0)
                {
                    UIHelpers.InfoBox(c, "Nothing scanned yet. Load a Bike Park and hit Scan Map.");
                }
                else
                {
                    for (int i = 0; i < total; i++)
                    {
                        if (ObjectPlacer.IsHarvestedAt(i))
                            AddObjectRow(c, i, false);
                    }
                }

                UIHelpers.AddScrollForwarders(c);
                RefreshHeader();
            }
            catch (System.Exception ex)
            {
                if (ex is MissingReferenceException || ex is System.NullReferenceException)
                {
                    ClearUiRefs();
                    return;
                }
                MelonLogger.Error("ObjectPlacerPage.RebuildList: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "ObjectPlacerPage");
            }
        }

        private static void AddObjectRow(Transform c, int index, bool fromFavBox)
        {
            int captured = index;
            bool selected = ObjectPlacer.SelectedIndex == index;
            bool fav = ObjectPlacer.IsFavAt(index);
            string label = ObjectPlacer.GetNameAt(index);
            if (!fromFavBox && ObjectPlacer.IsHarvestedAt(index)) label = label + "  · map";

            var row = UIHelpers.StatRow("", c);
            StripEmptyStatLabel(row);

            if (fromFavBox)
            {
                // Match star + Use width on the left so the name centres in the full row.
                float sideW = 22f + 8f + 44f;
                var leftPad = UIHelpers.Obj("FavPad" + index, row.transform);
                leftPad.AddComponent<LayoutElement>().preferredWidth = sideW;
                leftPad.transform.SetAsFirstSibling();

                var nmObj = UIHelpers.Obj("FavNm" + index, row.transform);
                var nmImg = nmObj.AddComponent<Image>();
                nmImg.color = new Color(0, 0, 0, 0);
                var nmLe = nmObj.AddComponent<LayoutElement>();
                nmLe.flexibleWidth = 1;
                nmLe.preferredHeight = UIHelpers.RowH;
                var nmBtn = nmObj.AddComponent<Button>();
                var nmCb = nmBtn.colors;
                nmCb.normalColor = Color.white;
                nmCb.highlightedColor = UIHelpers.AccentDim;
                nmCb.pressedColor = UIHelpers.Accent;
                nmCb.colorMultiplier = 1;
                nmBtn.colors = nmCb;
                nmBtn.onClick.AddListener(() => StartRename(captured));

                var nmTxt = UIHelpers.Txt("FavNmT" + index, nmObj.transform, label, 12,
                    FontStyle.Bold, TextAnchor.MiddleCenter,
                    (_renamingIndex == captured) ? UIHelpers.Accent : UIHelpers.TextLight);
                UIHelpers.Fill(UIHelpers.RT(nmTxt.gameObject));
                nmTxt.raycastTarget = false;
                if (_renamingIndex == captured)
                {
                    _renameText = nmTxt;
                    nmTxt.text = UIHelpers.WithCaret(_renameBuffer, true);
                }
            }
            else
            {
                var labelTxt = UIHelpers.Txt("ObjL" + index, row.transform, label, 12,
                    FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextLight);
                var lle = labelTxt.gameObject.AddComponent<LayoutElement>();
                lle.flexibleWidth = 1;
                lle.preferredHeight = UIHelpers.RowH;
            }

            var star = UIHelpers.Btn("Fav" + (fromFavBox ? "F" : "G") + index, row.transform, "\u2605",
                new Vector2(22, 22), 13,
                () =>
                {
                    if (IsRenaming) CancelRename();
                    ObjectPlacer.ToggleFav(captured);
                    RebuildList();
                },
                new Color(0, 0, 0, 0),
                fav ? UIHelpers.Accent : UIHelpers.TextDim);
            var sle = star.gameObject.AddComponent<LayoutElement>();
            sle.preferredWidth = 22; sle.preferredHeight = 22;
            sle.minWidth = 22; sle.minHeight = 22;

            UIHelpers.ActionBtn(row.transform, selected ? "✓" : "Use", () =>
            {
                if (IsRenaming) CancelRename();
                ObjectPlacer.SetObject(captured);
                RebuildList();
            }, 44);
        }

        public static void CancelRename()
        {
            _renamingIndex = -1;
            _renameBuffer = "";
            _renameText = null;
        }

        private static void StripEmptyStatLabel(GameObject row)
        {
            if ((object)row == null) return;
            Transform t = row.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform ch = t.GetChild(i);
                if ((object)ch == null) continue;
                Text txt = ch.GetComponent<Text>();
                if ((object)txt == null) continue;
                if (!string.IsNullOrEmpty(txt.text)) continue;
                LayoutElement le = ch.GetComponent<LayoutElement>();
                if ((object)le != null && le.flexibleWidth > 0f)
                {
                    UnityEngine.Object.DestroyImmediate(ch.gameObject);
                    return;
                }
            }
        }

        private static void StartRename(int index)
        {
            if (!ObjectPlacer.IsFavAt(index)) return;
            _renamingIndex = index;
            _renameBuffer = ObjectPlacer.GetNameAt(index);
            RebuildList();
        }

        public static void Tick()
        {
            if (_renamingIndex < 0) return;
            TickRename();
        }

        private static void TickRename()
        {
            foreach (char ch in Input.inputString)
            {
                if (ch == '\b')
                {
                    if (_renameBuffer.Length > 0)
                        _renameBuffer = _renameBuffer.Substring(0, _renameBuffer.Length - 1);
                }
                else if (ch == '\n' || ch == '\r')
                {
                    int idx = _renamingIndex;
                    string name = _renameBuffer;
                    CancelRename();
                    if (name.Length > 0)
                        ObjectPlacer.RenameFavAt(idx, name);
                    RebuildList();
                    return;
                }
                else if (ch == '\x1b')
                {
                    CancelRename();
                    RebuildList();
                    return;
                }
                else if (_renameBuffer.Length < 32)
                {
                    _renameBuffer += ch;
                }
            }

            if (MenuInputGuard.GetKeyDown(KeyCode.Escape))
            {
                CancelRename();
                RebuildList();
                return;
            }

            if (_renameText) _renameText.text = UIHelpers.WithCaret(_renameBuffer, true);
        }

        private static string StatusLine()
        {
            return ObjectPlacer.PlacedCount + " placed  ·  " + ObjectPlacer.HarvestedCount + " from maps  ·  " + ObjectPlacer.SelectedName;
        }

        private static void RefreshHeader()
        {
            if (_placeVal)
            {
                _placeVal.text = ObjectPlacer.Enabled ? "ON" : "OFF";
                _placeVal.color = ObjectPlacer.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor;
            }
            UIHelpers.SetToggle(_placeTrack, _placeKnob, ObjectPlacer.Enabled);
            if (_autoVal)
            {
                _autoVal.text = ObjectPlacer.AutoCloseMenu ? "ON" : "OFF";
                _autoVal.color = ObjectPlacer.AutoCloseMenu ? UIHelpers.OnColor : UIHelpers.OffColor;
            }
            UIHelpers.SetToggle(_autoTrack, _autoKnob, ObjectPlacer.AutoCloseMenu);
            if (_statusVal) _statusVal.text = StatusLine();

            UIHelpers.SetBar(_moveBar, (ObjectPlacer.MoveSpeedLevel - 1) / 9f);
            UIHelpers.SetBar(_rotBar, (ObjectPlacer.RotateSpeedLevel - 1) / 9f);
            UIHelpers.SetBar(_liftBar, (ObjectPlacer.LiftSpeedLevel - 1) / 9f);
            UIHelpers.SetBar(_camBar, (ObjectPlacer.CamDistanceLevel - 1) / 9f);
            UIHelpers.SetBar(_scaleBar, (ObjectPlacer.MeshScaleLevel - 1) / 9f);
            if (_moveVal) _moveVal.text = ObjectPlacer.MoveSpeedDisplay;
            if (_rotVal) _rotVal.text = ObjectPlacer.RotateSpeedDisplay;
            if (_liftVal) _liftVal.text = ObjectPlacer.LiftSpeedDisplay;
            if (_camVal) _camVal.text = ObjectPlacer.CamDistanceDisplay;
            if (_scaleVal) _scaleVal.text = ObjectPlacer.MeshScaleDisplay;
        }

        public static void RefreshAll()
        {
            if (!_listRoot) return;
            RefreshHeader();
        }

        public static void ClearUiRefs()
        {
            CancelRename();
            _listRoot = null;
            _placeVal = null;
            _placeTrack = null;
            _placeKnob = null;
            _autoVal = null;
            _autoTrack = null;
            _autoKnob = null;
            _statusVal = null;
            _moveBar = null;
            _rotBar = null;
            _liftBar = null;
            _camBar = null;
            _scaleBar = null;
            _moveVal = null;
            _rotVal = null;
            _liftVal = null;
            _camVal = null;
            _scaleVal = null;
        }
    }
}
