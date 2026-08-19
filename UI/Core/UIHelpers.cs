using DescendersModMenu.Mods;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class UIHelpers
    {
        // ════════════════════════════════════════════════════
        // ════════════════════════════════════════════════════

        public static Color WinOuter => UITheme.BgOuter;
        public static Color WinPanel => UITheme.BgContent;
        public static Color WinBorder => UITheme.BorderWin;
        public static Color HeaderBg => UITheme.BgHeader;
        public static Color SidebarBg => UITheme.BgSidebar;
        public static Color NavActive => UITheme.NavActiveBg;
        public static Color RowBg => UITheme.BgRow;
        public static Color RowBorder => UITheme.BorderRow;
        public static Color BtnBg => UITheme.BtnActionBg;

        public static Color Accent => UITheme.Accent;
        public static Color AccentDim => UITheme.AccentDim;
        public static Color AccentBdr => UITheme.AccentBorder;

        public static Color NeonBlue => UITheme.Secondary;

        public static Color Orange => UITheme.Warning;
        public static Color OrangeDim => UITheme.WarningDim;
        public static Color OrangeBdr => UITheme.WarningBdr;
        public static Color ActionBtnBg => UITheme.BtnActionBg;

        public static Color TextLight => UITheme.TextHeading;
        public static Color TextMid => UITheme.TextBody;
        public static Color TextDim => UITheme.TextDim;
        public static Color BtnText => UITheme.BtnActionText;

        public static Color OnColor => UITheme.StateOn;
        public static Color OnBg => UITheme.StateOnBg;
        public static Color OnBdr => UITheme.StateOnBdr;
        public static Color OffColor => UITheme.StateOff;
        public static Color RedDim => UITheme.StateOffBg;
        public static Color RedBdr => UITheme.StateOffBdr;

        public static Color TogOffTrack => UITheme.ToggleTrackOff;
        public static Color TogOnTrack => UITheme.ToggleTrackOn;
        public static Color TogKnobOn => UITheme.ToggleKnobOn;
        public static Color TogKnobOff => UITheme.ToggleKnobOff;

        public static Color BarBg => UITheme.SliderBg;
        public static Color BarFill => UITheme.SliderFill;

        // ── Layout ──────────────────────────────────────────────────────────
        public static float WinW => UITheme.WinW;
        public static float WinH => UITheme.WinH;
        public static float SidebarW => UITheme.SidebarW;
        public static float HeaderH => UITheme.HeaderH;
        public const float TabH = 36f;
        public static float RowH => UITheme.RowH;
        public static float RowGap => UITheme.RowGap;
        public static float RowPad => UITheme.RowPad;
        public static float ContentPad => UITheme.ContentPad;
        public const float BottomH = 46f;

        // ── Font ────────────────────────────────────────────────────────────
        private static Font _font;
        public static Font GetFont()
        {
            if (_font == null) _font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            return _font;
        }

        public static Texture2D RoundTex(int w, int h, int r, Color fill)
        {
            var tex = new Texture2D(w, h, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;
            var clr = new Color(0, 0, 0, 0);
            var px = new Color[w * h];
            for (int y2 = 0; y2 < h; y2++)
                for (int x = 0; x < w; x++)
                {
                    float dx = 0, dy = 0;
                    if (x < r && y2 < r) { dx = r - x; dy = r - y2; }
                    else if (x > w - r - 1 && y2 < r) { dx = x - (w - r - 1); dy = r - y2; }
                    else if (x < r && y2 > h - r - 1) { dx = r - x; dy = y2 - (h - r - 1); }
                    else if (x > w - r - 1 && y2 > h - r - 1) { dx = x - (w - r - 1); dy = y2 - (h - r - 1); }
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > r + 0.5f) px[y2 * w + x] = clr;
                    else if (d > r - 0.5f) px[y2 * w + x] = new Color(fill.r, fill.g, fill.b, fill.a * (1f - (d - (r - 0.5f))));
                    else px[y2 * w + x] = fill;
                }
            tex.SetPixels(px); tex.Apply();
            return tex;
        }

        public static Sprite RoundSprite(int sz, int r, Color fill)
        {
            var tex = RoundTex(sz, sz, r, fill);
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(.5f, .5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        private static Sprite _rowSp, _btnSp, _winSp, _togSp, _knobSp, _barSp, _dotSp;
        public static Sprite RowSp { get { if (_rowSp == null) _rowSp = RoundSprite(128, 8, Color.white); return _rowSp; } }
        public static Sprite BtnSp { get { if (_btnSp == null) _btnSp = RoundSprite(128, 10, Color.white); return _btnSp; } }
        public static Sprite WinSp { get { if (_winSp == null) _winSp = RoundSprite(128, 20, Color.white); return _winSp; } }
        private static Sprite _navSp;
        public static Sprite NavSp { get { if (_navSp == null) _navSp = RoundSprite(128, 12, Color.white); return _navSp; } }
        public static Texture2D FrameTex(int sz, int r, int thick)
        {
            var t = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            var pix = new Color32[sz * sz];
            Color32 col = new Color32(255, 255, 255, 255);
            Color32 emp = new Color32(0, 0, 0, 0);
            float hw = (sz - 1) / 2f, hh = (sz - 1) / 2f;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float dx = Mathf.Abs(x - hw), dy = Mathf.Abs(y - hh);
                    bool inO = dx <= hw && dy <= hh &&
                        (dx <= hw - r || dy <= hh - r ||
                         (dx - (hw - r)) * (dx - (hw - r)) + (dy - (hh - r)) * (dy - (hh - r)) <= (float)r * r);
                    float ir = Mathf.Max(0, r - thick);
                    float ihw = hw - thick, ihh = hh - thick;
                    bool inI = ihw > 0 && ihh > 0 && dx <= ihw && dy <= ihh &&
                        (dx <= ihw - ir || dy <= ihh - ir ||
                         (dx - (ihw - ir)) * (dx - (ihw - ir)) + (dy - (ihh - ir)) * (dy - (ihh - ir)) <= ir * ir);
                    pix[y * sz + x] = (inO && !inI) ? col : emp;
                }
            t.SetPixels32(pix); t.Apply(); return t;
        }
        private static Sprite _frameSp;
        public static Sprite FrameSp
        {
            get
            {
                if (_frameSp == null)
                {
                    const int sz = 256, r = 40, thick = 4;
                    _frameSp = Sprite.Create(FrameTex(sz, r, thick),
                        new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100, 0,
                        SpriteMeshType.FullRect, new Vector4(r, r, r, r));
                }
                return _frameSp;
            }
        }


        public static Sprite TogSp { get { if (_togSp == null) _togSp = RoundSprite(128, 11, Color.white); return _togSp; } }
        public static Sprite KnobSp { get { if (_knobSp == null) _knobSp = RoundSprite(64, 32, Color.white); return _knobSp; } }
        public static Sprite BarSp { get { if (_barSp == null) _barSp = RoundSprite(64, 3, Color.white); return _barSp; } }
        public static Sprite DotSp
        {
            get
            {
                if (_dotSp == null)
                {
                    var tex = RoundTex(16, 16, 8, Color.white);
                    _dotSp = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 100f);
                }
                return _dotSp;
            }
        }

        // ── Core helpers ─────────────────────────────────────────────────────
        public static GameObject Obj(string n, Transform p)
        {
            var g = new GameObject(n, typeof(RectTransform));
            g.transform.SetParent(p, false);
            return g;
        }

        public static RectTransform RT(GameObject g) { return g.GetComponent<RectTransform>(); }

        public static void Fill(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        public static void Pin(RectTransform rt, Vector2 a, Vector2 pv, Vector2 pos, Vector2 sz)
        {
            rt.anchorMin = a; rt.anchorMax = a; rt.pivot = pv;
            rt.anchoredPosition = pos; rt.sizeDelta = sz;
        }

        public static GameObject Panel(string n, Transform p, Color c, Sprite sp = null)
        {
            var g = Obj(n, p);
            var i = g.AddComponent<Image>(); i.color = c;
            if (sp) { i.sprite = sp; i.type = Image.Type.Sliced; }
            return g;
        }

        public static Text Txt(string n, Transform p, string txt, int sz, FontStyle fs, TextAnchor a, Color c)
        {
            var g = Obj(n, p);
            var t = g.AddComponent<Text>();
            t.font = GetFont(); t.text = txt; t.fontSize = sz; t.fontStyle = fs;
            t.alignment = a; t.color = c;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Btn(string n, Transform p, string lbl, Vector2 sz, int fs,
            UnityEngine.Events.UnityAction clk, Color? bg = null, Color? tc = null)
        {
            var g = Obj(n, p);
            var im = g.AddComponent<Image>();
            im.sprite = BtnSp; im.type = Image.Type.Sliced; im.color = bg ?? BtnBg;
            var b = g.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = Color.white; cb.highlightedColor = new Color(1, 1, 1, 1.15f);
            cb.pressedColor = new Color(.7f, .7f, .7f, 1);
            cb.colorMultiplier = 1; cb.fadeDuration = .08f;
            b.colors = cb;
            RT(g).sizeDelta = sz;
            b.onClick.AddListener(clk);
            var t = Txt("L", g.transform, lbl, fs, FontStyle.Bold, TextAnchor.MiddleCenter, tc ?? BtnText);
            Fill(RT(t.gameObject));
            return b;
        }

        public static Button SmallBtn(Transform p, string lbl, UnityEngine.Events.UnityAction clk)
        {
            var b = Btn(lbl + "B", p, lbl, new Vector2(24, 24), 13, clk, UITheme.BtnActionBg, UITheme.BtnActionText);
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 24; le.preferredHeight = 24;
            le.minWidth = 24; le.minHeight = 24; le.flexibleHeight = 0;
            return b;
        }

        public static void ActionBtn(Transform p, string lbl, UnityEngine.Events.UnityAction clk, float w = 72)
        {
            var b = Btn(lbl + "B", p, lbl, new Vector2(w, 26), 11, clk, UITheme.BtnActionBg, UITheme.BtnActionText);
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w; le.preferredHeight = 26;
            le.minWidth = w; le.minHeight = 26; le.flexibleHeight = 0;
        }

        public static void ActionBtnOrange(Transform p, string lbl, UnityEngine.Events.UnityAction clk, float w = 72)
        {
            var b = Btn(lbl + "B", p, lbl, new Vector2(w, 26), 11, clk, UITheme.BtnActionBg, UITheme.BtnActionText);
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w; le.preferredHeight = 26;
            le.minWidth = w; le.minHeight = 26; le.flexibleHeight = 0;
        }

        public static Image MakeBar(string n, Transform p, float pct)
        {
            var w = Obj(n, p);
            var wi = w.AddComponent<Image>();
            wi.sprite = BarSp; wi.type = Image.Type.Sliced;
            wi.color = Color.clear; wi.raycastTarget = false;
            var le = w.AddComponent<LayoutElement>();
            le.preferredWidth = 70; le.preferredHeight = 4;
            le.minWidth = 70; le.minHeight = 4; le.flexibleHeight = 0;
            le.flexibleWidth = 0;

            var f = Obj("F", w.transform);
            var fi = f.AddComponent<Image>();
            fi.sprite = BarSp; fi.type = Image.Type.Sliced; fi.color = BarFill;
            var frt = RT(f);
            frt.anchorMin = new Vector2(0, 0.5f); frt.anchorMax = new Vector2(0, 0.5f);
            frt.pivot = new Vector2(0, 0.5f);
            frt.sizeDelta = new Vector2(70f * Mathf.Clamp01(pct), 4);
            frt.anchoredPosition = Vector2.zero;
            return fi;
        }

        public static void SetBar(Image fi, float pct)
        {
            if (fi) RT(fi.gameObject).sizeDelta = new Vector2(70f * Mathf.Clamp01(pct), 4);
        }

        public static GameObject BareBtnRow(Transform p, float height = -1f)
        {
            var row = Obj("BareBtnRow", p);
            var le = row.AddComponent<LayoutElement>();
            float h = height > 0f ? height : RowH;
            le.preferredHeight = h; le.minHeight = h; le.flexibleHeight = 0;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            return row;
        }

        public static GameObject StatRow(string label, Transform p)
        {
            var row = Panel(label + "R", p, RowBg, RowSp);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = RowH; le.minHeight = RowH;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8; hlg.padding = new RectOffset((int)RowPad, (int)RowPad, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            var bd = Panel("Bd", row.transform, RowBorder, RowSp);
            bd.GetComponent<Image>().raycastTarget = false;
            Fill(RT(bd));
            bd.AddComponent<LayoutElement>().ignoreLayout = true;

            var t = Txt(label + "L", row.transform, label, 12, FontStyle.Bold, TextAnchor.MiddleLeft, TextLight);
            var tle = t.gameObject.AddComponent<LayoutElement>();
            tle.flexibleWidth = 1; tle.preferredHeight = RowH;
            return row;
        }

        // ── Active row highlight ──────────────────────────────────────
        public static void SetRowActive(GameObject row, bool active)
        {
            if ((object)row == null) return;
            var img = row.GetComponent<Image>();
            if (img) img.color = active ? NavActive : RowBg;
        }

        public static Button StarBtn(Transform parent, string id, UnityEngine.Events.UnityAction onClick)
        {
            var b = Btn("Star_" + id, parent, "\u2605", new Vector2(22, 22), 13, onClick,
                new Color(0, 0, 0, 0),
                FavouritesManager.IsFavourited(id) ? Accent : TextDim);
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 22; le.preferredHeight = 22;
            le.minWidth = 22; le.minHeight = 22;
            return b;
        }

        public static void SetStarActive(Button btn, bool active)
        {
            if (!btn) return;
            var t = btn.GetComponentInChildren<Text>();
            if (t) t.color = active ? Accent : TextDim;
            var img = btn.GetComponent<Image>();
            if (img) img.color = new Color(0, 0, 0, 0);
        }

        public static Button StarBtnAbs(Transform parent, string id, UnityEngine.Events.UnityAction onClick)
        {
            var g = Obj("Star_" + id, parent);
            var im = g.AddComponent<Image>();
            im.color = new Color(0, 0, 0, 0); im.raycastTarget = true;
            var b = g.AddComponent<Button>();
            b.onClick.AddListener(onClick);
            var cb = b.colors;
            cb.normalColor = Color.white; cb.highlightedColor = new Color(1, 1, 1, 1.15f);
            cb.pressedColor = new Color(.7f, .7f, .7f, 1); cb.colorMultiplier = 1; b.colors = cb;
            var t = Txt("ST", g.transform, "\u2605", 13, FontStyle.Bold, TextAnchor.MiddleCenter,
                FavouritesManager.IsFavourited(id) ? Accent : TextDim);
            Fill(RT(t.gameObject));
            var rt = RT(g);
            rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(22, 22);
            rt.anchoredPosition = new Vector2(-6, 0);
            g.AddComponent<LayoutElement>().ignoreLayout = true;
            return b;
        }

        public static void Toggle(Transform p, string n, UnityEngine.Events.UnityAction clk,
            out Image track, out RectTransform knob)
        {
            var g = Obj(n, p);
            track = g.AddComponent<Image>();
            track.sprite = TogSp; track.type = Image.Type.Sliced;
            track.color = TogOffTrack;

            var tbdr = Panel("TBdr", g.transform, RowBorder, TogSp);
            tbdr.GetComponent<Image>().raycastTarget = false;
            Fill(RT(tbdr));
            tbdr.AddComponent<LayoutElement>().ignoreLayout = true;

            var b = g.AddComponent<Button>(); b.onClick.AddListener(clk);
            var cb = b.colors;
            cb.normalColor = Color.white; cb.highlightedColor = Color.white;
            cb.pressedColor = Color.white; cb.colorMultiplier = 1;
            b.colors = cb;

            var le = g.AddComponent<LayoutElement>();
            le.preferredWidth = 44; le.preferredHeight = 24;
            le.minWidth = 44; le.minHeight = 24; le.flexibleHeight = 0;

            var k = Obj("K", g.transform);
            var ki = k.AddComponent<Image>();
            ki.sprite = KnobSp; ki.type = Image.Type.Sliced;
            ki.color = TogKnobOff;
            ki.raycastTarget = false;

            knob = RT(k);
            knob.anchorMin = new Vector2(0, 0.5f); knob.anchorMax = new Vector2(0, 0.5f);
            knob.pivot = new Vector2(0, 0.5f);
            knob.sizeDelta = new Vector2(14, 14);
            knob.anchoredPosition = new Vector2(4, 0);
        }

        public static void SetToggle(Image track, RectTransform knob, bool on)
        {
            if (track)
            {
                track.color = on ? TogOnTrack : TogOffTrack;
                Transform tbdr = track.transform.Find("TBdr");
                if (tbdr != null)
                {
                    var tbdrImg = tbdr.GetComponent<Image>();
                    if (tbdrImg) tbdrImg.color = on
                        ? AccentBdr
                        : RowBorder;
                }
            }
            if (knob)
            {
                knob.anchoredPosition = on ? new Vector2(26, 0) : new Vector2(4, 0);
                var knobImg = knob.GetComponent<Image>();
                if (knobImg) knobImg.color = on ? TogKnobOn : TogKnobOff;
            }
        }

        public static void SetInteractable(UnityEngine.UI.Button btn, bool on)
        {
            if (btn) btn.interactable = on;
        }

        public static void SectionHeader(string title, Transform p)
        {
            var row = Obj(title + "H", p);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 28; le.minHeight = 28; le.flexibleHeight = 0;

            var bar = Panel("Bar", row.transform, Accent);
            var brt = RT(bar);
            brt.anchorMin = new Vector2(0, 0.5f); brt.anchorMax = new Vector2(0, 0.5f);
            brt.pivot = new Vector2(0, 0.5f); brt.sizeDelta = new Vector2(3, 14);
            brt.anchoredPosition = Vector2.zero;

            var t = Txt(title + "T", row.transform, title.ToUpper(), 11,
                FontStyle.Bold, TextAnchor.MiddleLeft, Accent);
            var trt = RT(t.gameObject);
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10, 0); trt.offsetMax = Vector2.zero;
        }

        public static void SectionHeaderButton(string title, Transform p, UnityEngine.Events.UnityAction onClick)
        {
            var row = Obj(title + "H", p);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 28; le.minHeight = 28; le.flexibleHeight = 0;

            var img = row.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            var btn = row.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var bar = Panel("Bar", row.transform, Accent);
            var brt = RT(bar);
            brt.anchorMin = new Vector2(0, 0.5f); brt.anchorMax = new Vector2(0, 0.5f);
            brt.pivot = new Vector2(0, 0.5f); brt.sizeDelta = new Vector2(3, 14);
            brt.anchoredPosition = Vector2.zero;

            var t = Txt(title + "T", row.transform, title.ToUpper(), 11,
                FontStyle.Bold, TextAnchor.MiddleLeft, Accent);
            var trt = RT(t.gameObject);
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10, 0); trt.offsetMax = Vector2.zero;
        }

        public static void Divider(Transform p)
        {
            Panel("Dv", p, RowBorder)
                .AddComponent<LayoutElement>().preferredHeight = 1;
        }

        public static void InfoBox(Transform p, string txt) => InfoBox(p, txt, TextDim);

        public static void InfoBox(Transform p, string txt, Color textColor)
        {
            var bx = Panel("Inf", p, RowBg, RowSp);
            bx.AddComponent<LayoutElement>().preferredHeight = 34;

            var bd = Panel("Bd", bx.transform, RowBorder, RowSp);
            bd.GetComponent<Image>().raycastTarget = false; Fill(RT(bd));
            bd.AddComponent<LayoutElement>().ignoreLayout = true;

            var lbar = Panel("LBar", bx.transform, TextDim);
            var lbRT = RT(lbar);
            lbRT.anchorMin = Vector2.zero; lbRT.anchorMax = new Vector2(0, 1);
            lbRT.pivot = new Vector2(0, 0.5f);
            lbRT.sizeDelta = new Vector2(2, 0); lbRT.offsetMin = new Vector2(0, 4);
            lbRT.offsetMax = new Vector2(2, -4);
            lbar.AddComponent<LayoutElement>().ignoreLayout = true;

            var t = Txt("IT", bx.transform, txt, 10, FontStyle.Italic, TextAnchor.MiddleLeft, textColor);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            Fill(RT(t.gameObject), 14, 12, 4, 4);
        }

        public static void HotkeyRow(Transform p, string desc, string key)
        {
            var row = Panel("HK" + key, p, RowBg, RowSp);
            row.AddComponent<LayoutElement>().preferredHeight = 30;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8; hlg.padding = new RectOffset(14, 14, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            var bd = Panel("Bd", row.transform, RowBorder, RowSp);
            bd.GetComponent<Image>().raycastTarget = false; Fill(RT(bd));
            bd.AddComponent<LayoutElement>().ignoreLayout = true;

            var dt = Txt("D", row.transform, desc, 11, FontStyle.Normal, TextAnchor.MiddleLeft, TextMid);
            var dle = dt.gameObject.AddComponent<LayoutElement>();
            dle.flexibleWidth = 1; dle.preferredHeight = 30;

            var badge = Panel("KB", row.transform, AccentDim, BtnSp);
            var ble = badge.AddComponent<LayoutElement>();
            float badgeW = Mathf.Max(38f, key.Length * 8f + 16f);
            ble.preferredWidth = badgeW; ble.minWidth = badgeW; ble.preferredHeight = 20; ble.flexibleHeight = 0;

            var bbdr = Panel("BBdr", badge.transform, AccentBdr, BtnSp);
            bbdr.GetComponent<Image>().raycastTarget = false;
            Fill(RT(bbdr));
            bbdr.AddComponent<LayoutElement>().ignoreLayout = true;

            var kt = Txt("K", badge.transform, key, 11, FontStyle.Bold, TextAnchor.MiddleCenter, Accent);
            Fill(RT(kt.gameObject));
        }

        private static Sprite CreateCircleSprite(int worldDiameter)
        {
            int scale = 8;
            int px = worldDiameter * scale;
            var tex = new Texture2D(px, px, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;
            float r = px * 0.5f;
            float cx = r, cy = r;
            for (int y = 0; y < px; y++)
                for (int x = 0; x < px; x++)
                {
                    float dx = x - cx + 0.5f, dy = y - cy + 0.5f;
                    float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, px, px),
                new Vector2(0.5f, 0.5f), (float)scale);
        }

        public static void AddScrollbar(ScrollRect sr)
        {
            try
            {
                float s = Screen.height / 1080f;
                float trackW = Mathf.Max(4f, 6f * s);
                float inset = Mathf.Max(1f, 1f * s);

                // ── Track ─────────────────────────────────────────────────────
                var trackGO = Obj("SBTrack", sr.transform);
                var trackRT = RT(trackGO);
                trackRT.anchorMin = new Vector2(1f, 0f);
                trackRT.anchorMax = new Vector2(1f, 1f);
                trackRT.pivot = new Vector2(1f, 0.5f);
                trackRT.sizeDelta = new Vector2(trackW, 0f);
                trackRT.anchoredPosition = Vector2.zero;
                var trackImg = trackGO.AddComponent<Image>();
                trackImg.color = new Color(UITheme.BgSidebar.r, UITheme.BgSidebar.g, UITheme.BgSidebar.b, 0.0f);
                trackImg.raycastTarget = true;
                trackGO.AddComponent<ScrollbarEventBlocker>();

                var handleGO = Obj("SBHandle", trackGO.transform);
                var handleRT = RT(handleGO);
                handleRT.anchorMin = new Vector2(0f, 1f);
                handleRT.anchorMax = new Vector2(1f, 1f);
                handleRT.pivot = new Vector2(0.5f, 1f);
                handleRT.offsetMin = new Vector2(inset, -40f);
                handleRT.offsetMax = new Vector2(-inset, 0f);

                int capD = Mathf.Max(2, Mathf.RoundToInt(trackW - inset * 2f));
                var circleSp = CreateCircleSprite(capD);

                var topGO = Obj("SBTop", handleGO.transform);
                var topRT = RT(topGO);
                topRT.anchorMin = new Vector2(0f, 1f);
                topRT.anchorMax = new Vector2(1f, 1f);
                topRT.pivot = new Vector2(0.5f, 1f);
                topRT.sizeDelta = new Vector2(0f, capD);
                topRT.anchoredPosition = Vector2.zero;
                var topImg = topGO.AddComponent<Image>();
                topImg.sprite = circleSp;
                topImg.color = UITheme.Accent;
                topImg.raycastTarget = true;

                var botGO = Obj("SBBot", handleGO.transform);
                var botRT = RT(botGO);
                botRT.anchorMin = new Vector2(0f, 0f);
                botRT.anchorMax = new Vector2(1f, 0f);
                botRT.pivot = new Vector2(0.5f, 0f);
                botRT.sizeDelta = new Vector2(0f, capD);
                botRT.anchoredPosition = Vector2.zero;
                var botImg = botGO.AddComponent<Image>();
                botImg.sprite = circleSp;
                botImg.color = UITheme.Accent;
                botImg.raycastTarget = true;

                var midGO = Obj("SBMid", handleGO.transform);
                var midRT = RT(midGO);
                midRT.anchorMin = Vector2.zero;
                midRT.anchorMax = Vector2.one;
                midRT.offsetMin = new Vector2(0f, capD / 2f);
                midRT.offsetMax = new Vector2(0f, -capD / 2f);
                var midImg = midGO.AddComponent<Image>();
                midImg.color = UITheme.Accent;
                midImg.raycastTarget = true;

                var msb = handleGO.AddComponent<ManualScrollbar>();
                msb.Init(sr, trackRT, handleRT, inset);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[UIHelpers] AddScrollbar: " + ex.Message);
            }
        }

        public static void AddScrollForwarders(Transform root)
        {
            if ((object)root == null) return;
            ScrollRect parentSR = root.GetComponentInParent<ScrollRect>();
            if ((object)parentSR == null) return;
            AddForwardersRecursive(root, parentSR);
        }

        private static void AddForwardersRecursive(Transform t, ScrollRect sr)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                var graphic = child.GetComponent<UnityEngine.UI.Graphic>();
                if ((object)graphic != null && graphic.raycastTarget)
                {
                    var fwd = child.GetComponent<ScrollForwarder>();
                    if ((object)fwd == null)
                    {
                        fwd = child.gameObject.AddComponent<ScrollForwarder>();
                        fwd.target = sr;
                    }
                }
                AddForwardersRecursive(child, sr);
            }
        }
    }

    public class ScrollForwarder : MonoBehaviour, UnityEngine.EventSystems.IScrollHandler
    {
        public ScrollRect target;

        public void OnScroll(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if ((object)target != null)
                target.OnScroll(eventData);
        }
    }

    public class ScrollbarEventBlocker : MonoBehaviour,
        UnityEngine.EventSystems.IPointerDownHandler
    {
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) { }
    }

    public class ManualScrollbar : MonoBehaviour,
        UnityEngine.EventSystems.IPointerDownHandler,
        UnityEngine.EventSystems.IBeginDragHandler,
        UnityEngine.EventSystems.IDragHandler,
        UnityEngine.EventSystems.IEndDragHandler
    {
        private ScrollRect _sr;
        private RectTransform _trackRT;
        private RectTransform _handleRT;
        private float _inset;
        private bool _dragging;
        private float _dragStartLocalY;
        private float _dragStartNormPos;
        private const float MinHandleH = 24f;

        public void Init(ScrollRect sr, RectTransform trackRT, RectTransform handleRT, float inset)
        {
            _sr = sr;
            _trackRT = trackRT;
            _handleRT = handleRT;
            _inset = inset;
        }

        private void LateUpdate()
        {
            if ((object)_sr == null || (object)_trackRT == null || (object)_handleRT == null) return;
            if ((object)_sr.viewport == null || (object)_sr.content == null) return;
            try
            {
                float viewH = _sr.viewport.rect.height;
                float contentH = _sr.content.rect.height;
                if (contentH <= 0f) return;

                float trackH = _trackRT.rect.height;
                float ratio = Mathf.Clamp01(viewH / contentH);
                float handleH = Mathf.Max(MinHandleH, trackH * ratio);
                float maxOff = trackH - handleH;

                float norm = Mathf.Clamp01(_sr.verticalNormalizedPosition);
                float topOff = (1f - norm) * maxOff;

                _handleRT.anchorMin = new Vector2(0f, 1f);
                _handleRT.anchorMax = new Vector2(1f, 1f);
                _handleRT.pivot = new Vector2(0.5f, 1f);
                _handleRT.offsetMin = new Vector2(_inset, -topOff - handleH);
                _handleRT.offsetMax = new Vector2(-_inset, -topOff);

                gameObject.SetActive(ratio < 0.999f);
            }
            catch { }
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) { }

        public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData e)
        {
            if ((object)_sr == null || (object)_trackRT == null) return;
            _dragging = true;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _trackRT, e.position, e.pressEventCamera, out local);
            _dragStartLocalY = local.y;
            _dragStartNormPos = _sr.verticalNormalizedPosition;
        }

        public void OnDrag(UnityEngine.EventSystems.PointerEventData e)
        {
            if (!_dragging || (object)_sr == null || (object)_trackRT == null) return;
            try
            {
                float viewH = _sr.viewport.rect.height;
                float contentH = _sr.content.rect.height;
                if (contentH <= viewH) return;

                float trackH = _trackRT.rect.height;
                float ratio = viewH / contentH;
                float handleH = Mathf.Max(MinHandleH, trackH * ratio);
                float maxOff = trackH - handleH;
                if (maxOff <= 0f) return;

                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _trackRT, e.position, e.pressEventCamera, out local);

                float deltaY = local.y - _dragStartLocalY;
                float normDelta = deltaY / maxOff;
                _sr.verticalNormalizedPosition = Mathf.Clamp01(_dragStartNormPos + normDelta);
            }
            catch { }
        }

        public void OnEndDrag(UnityEngine.EventSystems.PointerEventData e) { _dragging = false; }
    }
}

