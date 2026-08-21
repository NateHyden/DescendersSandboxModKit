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
        private static Image _moveBar, _rotBar, _liftBar, _camBar;
        private static Text _moveVal, _rotVal, _liftVal, _camVal;

        public static bool IsAnyActive => ObjectPlacer.IsAnyActive;

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

                var lib = UIHelpers.StatRow("Library", c);
                UIHelpers.ActionBtn(lib.transform, "Scan Map", () => { ObjectPlacer.ScanMap(); RebuildList(); }, 70);
                UIHelpers.ActionBtnOrange(lib.transform, "Forget", () => { ObjectPlacer.ClearHarvested(); RebuildList(); }, 56);

                _statusVal = UIHelpers.Txt("OpSt", c, StatusLine(), 10,
                    FontStyle.Italic, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                _statusVal.gameObject.AddComponent<LayoutElement>().preferredHeight = 18;

                UIHelpers.InfoBox(c, "Scan a Bike Park, star objects you like, then place them. Favourites save across maps. Stick moves / place with A / exit with B.", Color.white);

                UIHelpers.Divider(c);
                UIHelpers.SectionHeader("FAVOURITES", c);
                int favs = ObjectPlacer.FavCount;
                if (favs == 0)
                {
                    UIHelpers.InfoBox(c, "No favourites yet — tap the star on an object below to pin it here.");
                }
                else
                {
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
            if (ObjectPlacer.IsHarvestedAt(index)) label = label + "  · map";

            var row = UIHelpers.StatRow(label, c);
            var star = UIHelpers.Btn("Fav" + (fromFavBox ? "F" : "G") + index, row.transform, "\u2605",
                new Vector2(22, 22), 13,
                () => { ObjectPlacer.ToggleFav(captured); RebuildList(); },
                new Color(0, 0, 0, 0),
                fav ? UIHelpers.Accent : UIHelpers.TextDim);
            var sle = star.gameObject.AddComponent<LayoutElement>();
            sle.preferredWidth = 22; sle.preferredHeight = 22;
            sle.minWidth = 22; sle.minHeight = 22;

            var tag = UIHelpers.Txt("Sel" + (fromFavBox ? "F" : "G") + index, row.transform,
                selected ? "USING" : "Use", 10,
                FontStyle.Bold, TextAnchor.MiddleCenter,
                selected ? UIHelpers.OnColor : UIHelpers.TextDim);
            tag.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
            UIHelpers.ActionBtn(row.transform, selected ? "✓" : "Use", () =>
            {
                ObjectPlacer.SetObject(captured);
                RebuildList();
            }, 44);
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
            if (_moveVal) _moveVal.text = ObjectPlacer.MoveSpeedDisplay;
            if (_rotVal) _rotVal.text = ObjectPlacer.RotateSpeedDisplay;
            if (_liftVal) _liftVal.text = ObjectPlacer.LiftSpeedDisplay;
            if (_camVal) _camVal.text = ObjectPlacer.CamDistanceDisplay;
        }

        public static void RefreshAll()
        {
            if (!_listRoot) return;
            RefreshHeader();
        }

        public static void ClearUiRefs()
        {
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
            _moveVal = null;
            _rotVal = null;
            _liftVal = null;
            _camVal = null;
        }
    }
}
