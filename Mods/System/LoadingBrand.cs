using System.Reflection;
using DescendersModMenu.UI;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Brand on the cold-boot loading splash only (studio logo strip).
    /// Never shown again after that first load finishes (map changes, etc.).
    /// </summary>
    public static class LoadingBrand
    {
        private const string BrandLine1 = "DESCENDERS";
        private const string BrandLine2 = "PHYSICS SANDBOX";
        private static string BrandCredit { get { return "By " + BuildInfo.Author; } }

        private static GameObject _root;
        private static RectTransform _panel;
        private static Text _line2;
        private static float _pulse;
        private static float _scanTimer;
        private static bool _wantVisible;
        private static bool _disabled;
        private static bool _sawBootSplash;
        private static bool _bootFinished; // first splash done — stay off for the rest of the session
        private static Font _font;
        private static System.Type _tmpType;
        private static PropertyInfo _tmpTextProp;
        private static bool _tmpLookupDone;

        public static void Tick()
        {
            if (_disabled || _bootFinished) return;

            try
            {
                _scanTimer += Time.unscaledDeltaTime;
                if (_scanTimer >= 0.15f)
                {
                    _scanTimer = 0f;
                    _wantVisible = ShouldShow();
                }

                if (_wantVisible)
                {
                    _sawBootSplash = true;
                    EnsureUi();
                    if ((object)_root != null && !_root.activeSelf)
                        _root.SetActive(true);
                    DockBesideSplashLogos();
                    Pulse();
                }
                else
                {
                    if (_sawBootSplash || IsPastBoot())
                        FinishBootBrand();
                    else if ((object)_root != null && _root.activeSelf)
                        _root.SetActive(false);
                }
            }
            catch (System.Exception ex)
            {
                _disabled = true;
                MelonLogger.Error("[LoadingBrand] disabled: " + ex.Message);
            }
        }

        private static void FinishBootBrand()
        {
            _bootFinished = true;
            _wantVisible = false;
            Destroy();
        }

        private static bool ShouldShow()
        {
            if (_bootFinished) return false;
            if (IsPastBoot())
            {
                FinishBootBrand();
                return false;
            }

            return IsLoadingSplashVisible();
        }

        /// <summary>True once we're clearly past the cold-boot splash.</summary>
        private static bool IsPastBoot()
        {
            return IsInGameplay();
        }

        private static bool IsInGameplay()
        {
            GameObject player = GameObject.Find("Player_Human");
            if ((object)player != null && player != null && player.activeInHierarchy)
                return true;

            if (HasActiveUiType("UI_Speedometer")) return true;
            if (HasActiveUiType("UI_InGame")) return true;
            if (HasLabelContaining("km/h")) return true;
            if (HasLabelContaining("mph")) return true;
            return false;
        }

        private static bool IsLoadingSplashVisible()
        {
            Text[] texts = Resources.FindObjectsOfTypeAll<Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                Text t = texts[i];
                if ((object)t == null || t == null) continue;
                if (!t.gameObject.activeInHierarchy) continue;
                if (IsOurs(t.transform)) continue;
                if (IsLoadingLabel(t.text)) return true;
            }

            if (AnyTmpLoadingLabel()) return true;

            if (HasActiveUiType("UI_Loading")) return true;
            if (HasActiveUiType("UI_Preloader")) return true;

            return false;
        }

        private static bool AnyTmpLoadingLabel()
        {
            EnsureTmpReflection();
            if ((object)_tmpType == null || (object)_tmpTextProp == null) return false;

            Object[] objs = Resources.FindObjectsOfTypeAll(_tmpType);
            for (int i = 0; i < objs.Length; i++)
            {
                Object o = objs[i];
                if ((object)o == null || o == null) continue;
                Behaviour b = o as Behaviour;
                if ((object)b == null || b == null) continue;
                if (!b.gameObject.activeInHierarchy) continue;
                if (IsOurs(b.transform)) continue;

                object val = null;
                try { val = _tmpTextProp.GetValue(o, null); } catch { continue; }
                string s = val as string;
                if (IsLoadingLabel(s)) return true;
            }
            return false;
        }

        private static void EnsureTmpReflection()
        {
            if (_tmpLookupDone) return;
            _tmpLookupDone = true;
            try
            {
                _tmpType = FindType("TMPro.TextMeshProUGUI");
                if ((object)_tmpType != null)
                    _tmpTextProp = _tmpType.GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
            }
            catch
            {
                _tmpType = null;
                _tmpTextProp = null;
            }
        }

        private static bool IsLoadingLabel(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            s = s.Trim();
            if (s.Length == 0) return false;

            if (string.Equals(s, "Loading...", System.StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "Loading…", System.StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "Loading", System.StringComparison.OrdinalIgnoreCase)) return true;
            if (s.StartsWith("Loading", System.StringComparison.OrdinalIgnoreCase)) return true;
            if (IsPercentLabel(s)) return true;
            return false;
        }

        private static bool IsPercentLabel(string s)
        {
            if (s.Length < 2 || s[s.Length - 1] != '%') return false;
            int i = 0;
            while (i < s.Length - 1 && s[i] == ' ') i++;
            int digits = 0;
            while (i < s.Length - 1 && s[i] >= '0' && s[i] <= '9')
            {
                digits++;
                i++;
                if (digits > 3) return false;
            }
            while (i < s.Length - 1 && s[i] == ' ') i++;
            return digits > 0 && i == s.Length - 1;
        }

        private static bool IsOurs(Transform t)
        {
            if ((object)_root == null || _root == null) return false;
            return t.root == _root.transform;
        }

        private static bool HasLabelContaining(string needle)
        {
            Text[] texts = Resources.FindObjectsOfTypeAll<Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                Text t = texts[i];
                if ((object)t == null || t == null) continue;
                if (!t.gameObject.activeInHierarchy) continue;
                if (IsOurs(t.transform)) continue;
                if (!string.IsNullOrEmpty(t.text)
                    && t.text.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            EnsureTmpReflection();
            if ((object)_tmpType != null && (object)_tmpTextProp != null)
            {
                Object[] objs = Resources.FindObjectsOfTypeAll(_tmpType);
                for (int i = 0; i < objs.Length; i++)
                {
                    Object o = objs[i];
                    if ((object)o == null || o == null) continue;
                    Behaviour b = o as Behaviour;
                    if ((object)b == null || b == null) continue;
                    if (!b.gameObject.activeInHierarchy) continue;
                    if (IsOurs(b.transform)) continue;
                    object val = null;
                    try { val = _tmpTextProp.GetValue(o, null); } catch { continue; }
                    string s = val as string;
                    if (!string.IsNullOrEmpty(s)
                        && s.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            return false;
        }

        private static bool HasActiveUiType(string typeName)
        {
            System.Type t = FindType(typeName);
            if ((object)t == null) return false;
            Object[] objs = Object.FindObjectsOfType(t);
            for (int i = 0; i < objs.Length; i++)
            {
                Behaviour b = objs[i] as Behaviour;
                if ((object)b != null && b != null && b.isActiveAndEnabled)
                    return true;
            }
            return false;
        }

        private static System.Type FindType(string typeName)
        {
            Assembly[] asms = System.AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                try
                {
                    System.Type t = asms[i].GetType(typeName);
                    if ((object)t != null) return t;
                }
                catch { }
            }
            return null;
        }

        private static void EnsureUi()
        {
            if ((object)_root != null && _root != null) return;

            _font = ResolveCoolFont();

            _root = new GameObject("DescendersSandbox_LoadingBrand");
            Object.DontDestroyOnLoad(_root);

            RectTransform rootRt = _root.GetComponent<RectTransform>();
            if ((object)rootRt == null)
                rootRt = _root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootRt.pivot = new Vector2(0.5f, 0.5f);

            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _root.AddComponent<GraphicRaycaster>().enabled = false;

            GameObject panel = new GameObject("Brand");
            panel.transform.SetParent(_root.transform, false);
            _panel = panel.AddComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.5f, 0f);
            _panel.anchorMax = new Vector2(0.5f, 0f);
            _panel.pivot = new Vector2(0f, 0.5f);
            _panel.anchoredPosition = new Vector2(420f, 36f);
            _panel.sizeDelta = new Vector2(420f, 44f);

            MakeLine(panel.transform, "L1s", BrandLine1, 13, new Color(0f, 0f, 0f, 0.7f), new Vector2(1.5f, 12f));
            MakeLine(panel.transform, "L2s", BrandLine2, 10, new Color(0f, 0f, 0f, 0.7f), new Vector2(1.5f, -1f));
            MakeLine(panel.transform, "L3s", BrandCredit, 9, new Color(0f, 0f, 0f, 0.65f), new Vector2(1.5f, -14f));
            MakeLine(panel.transform, "L1", BrandLine1, 13, new Color(0.96f, 0.95f, 0.99f, 0.98f), new Vector2(0f, 13.5f));
            _line2 = MakeLine(panel.transform, "L2", BrandLine2, 10, UITheme.Accent, new Vector2(0f, 0.5f));
            MakeLine(panel.transform, "L3", BrandCredit, 9, new Color(0.78f, 0.80f, 0.86f, 0.92f), new Vector2(0f, -12.5f));

            DockBesideSplashLogos();
            ModLog.Debug("[LoadingBrand] Overlay ready.");
        }

        /// <summary>
        /// Clear the entire splash logo row (cube + wordmark are often separate Images),
        /// then sit just to the right in world space.
        /// </summary>
        private static void DockBesideSplashLogos()
        {
            if ((object)_panel == null || _panel == null) return;
            if ((object)_root == null || _root == null) return;

            RectTransform seed = FindSeedSplashLogo();
            if ((object)seed == null || seed == null)
                return;

            Transform row = seed.parent;
            if ((object)row == null || row == null)
                row = seed;

            float maxRight = float.MinValue;
            Vector3 alignRightMid = Vector3.zero;
            Vector3 widthVec = Vector3.right;
            bool any = false;

            // Include the seed and all Image siblings in the same row.
            CollectLogoEdge(seed, ref maxRight, ref alignRightMid, ref widthVec, ref any);
            if ((object)row != null)
            {
                for (int i = 0; i < row.childCount; i++)
                {
                    Transform child = row.GetChild(i);
                    if ((object)child == null) continue;
                    Image img = child.GetComponent<Image>();
                    if ((object)img == null || img == null) continue;
                    if (!img.gameObject.activeInHierarchy) continue;
                    if (!IsLogoSized(img.rectTransform)) continue;
                    CollectLogoEdge(img.rectTransform, ref maxRight, ref alignRightMid, ref widthVec, ref any);
                }
            }

            // Also sweep named splash logos in case wordmark lives outside the row parent.
            Image[] images = Resources.FindObjectsOfTypeAll<Image>();
            for (int i = 0; i < images.Length; i++)
            {
                Image img = images[i];
                if ((object)img == null || img == null) continue;
                if (!img.gameObject.activeInHierarchy) continue;
                if (IsOurs(img.transform)) continue;
                string n = img.gameObject.name;
                string sn = (object)img.sprite != null && img.sprite != null ? img.sprite.name : null;
                if (!NameLooksLikeSplashLogo(n) && !NameLooksLikeSplashLogo(sn))
                    continue;
                if (!IsLogoSized(img.rectTransform)) continue;
                CollectLogoEdge(img.rectTransform, ref maxRight, ref alignRightMid, ref widthVec, ref any);
            }

            if (!any)
                return;

            // ~8% of logo width — similar to spacing between fmod/unity, scales with res.
            float gap = widthVec.magnitude;
            if (gap < 1f) gap = 10f;
            else gap = Mathf.Clamp(gap * 0.08f, 6f, 16f);

            Vector3 dir = widthVec.sqrMagnitude > 0.0001f ? widthVec.normalized : Vector3.right;
            Vector3 target = alignRightMid + dir * gap;

            _panel.pivot = new Vector2(0f, 0.5f);
            _panel.position = target;
            // Keep facing/scale sane under our overlay canvas.
            _panel.localRotation = Quaternion.identity;
            if (_panel.localScale.x < 0.01f)
                _panel.localScale = Vector3.one;
        }

        private static void CollectLogoEdge(
            RectTransform rt,
            ref float maxRight,
            ref Vector3 alignRightMid,
            ref Vector3 widthVec,
            ref bool any)
        {
            if ((object)rt == null || rt == null) return;

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            // Prefer screen X so different canvases still compare.
            Camera cam = GetCanvasCamera(rt);
            float right = RectTransformUtility.WorldToScreenPoint(cam, corners[3]).x;
            if (right < maxRight)
                return;

            maxRight = right;
            alignRightMid = (corners[2] + corners[3]) * 0.5f;
            widthVec = corners[3] - corners[0];
            any = true;
        }

        private static Camera GetCanvasCamera(RectTransform rt)
        {
            Canvas c = rt.GetComponentInParent<Canvas>();
            if ((object)c == null || c == null) return null;
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return c.worldCamera;
        }

        private static bool IsLogoSized(RectTransform rt)
        {
            if ((object)rt == null || rt == null) return false;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Camera cam = GetCanvasCamera(rt);
            Vector2 bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 tl = RectTransformUtility.WorldToScreenPoint(cam, corners[1]);
            Vector2 br = RectTransformUtility.WorldToScreenPoint(cam, corners[3]);
            float screenH = Screen.height > 0 ? Screen.height : 1080f;
            float height = tl.y - bl.y;
            float width = br.x - bl.x;
            if (bl.y > screenH * 0.5f) return false;
            if (height < 8f || height > 180f) return false;
            if (width < 16f || width > 560f) return false;
            return true;
        }

        private static RectTransform FindSeedSplashLogo()
        {
            Image[] images = Resources.FindObjectsOfTypeAll<Image>();
            RectTransform best = null;
            float bestRight = float.MinValue;

            for (int i = 0; i < images.Length; i++)
            {
                Image img = images[i];
                if ((object)img == null || img == null) continue;
                if (!img.gameObject.activeInHierarchy) continue;
                if (IsOurs(img.transform)) continue;

                string n = img.gameObject.name;
                string sn = (object)img.sprite != null && img.sprite != null ? img.sprite.name : null;
                // Prefer Unity / fmod as seeds for the strip.
                bool named = NameLooksLikeSplashLogo(n) || NameLooksLikeSplashLogo(sn);
                if (!named) continue;
                if (!IsLogoSized(img.rectTransform)) continue;

                Vector3[] corners = new Vector3[4];
                img.rectTransform.GetWorldCorners(corners);
                float right = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(img.rectTransform), corners[3]).x;
                if (right > bestRight)
                {
                    bestRight = right;
                    best = img.rectTransform;
                }
            }
            return best;
        }

        private static bool NameLooksLikeSplashLogo(string n)
        {
            if (string.IsNullOrEmpty(n)) return false;
            string lower = n.ToLowerInvariant();
            if (lower.Contains("button") || lower.Contains("toggle") || lower.Contains("slider"))
                return false;
            if (lower.Contains("unity")) return true;
            if (lower.Contains("fmod")) return true;
            if (lower.Contains("ragesquid") || lower.Contains("rage_squid") || lower.Contains("rage squid")) return true;
            if (lower.Contains("robots")) return true;
            if (lower.Contains("codeglue")) return true;
            if (lower.Contains("powerup") || lower.Contains("power_up") || lower.Contains("power up")) return true;
            return false;
        }

        private static Text MakeLine(Transform parent, string name, string text, int size, Color color, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(360f, 16f);

            Text t = go.AddComponent<Text>();
            t.font = _font != null ? _font : UIHelpers.GetFont();
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static Font ResolveCoolFont()
        {
            Font best = null;
            int bestScore = -1;
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            for (int i = 0; i < fonts.Length; i++)
            {
                Font f = fonts[i];
                if ((object)f == null || f == null) continue;
                string n = f.name;
                if (string.IsNullOrEmpty(n)) continue;
                int score = 0;
                string lower = n.ToLowerInvariant();
                if (lower.Contains("arial")) score -= 5;
                if (lower.Contains("legacy")) score -= 3;
                if (lower.Contains("impact")) score += 40;
                if (lower.Contains("oswald")) score += 35;
                if (lower.Contains("bebas")) score += 35;
                if (lower.Contains("montserrat")) score += 30;
                if (lower.Contains("bold")) score += 10;
                if (lower.Contains("display") || lower.Contains("title")) score += 12;
                if (lower.Contains("lexico") || lower.Contains("descender")) score += 25;
                if (score > bestScore) { bestScore = score; best = f; }
            }
            if ((object)best != null) return best;
            return UIHelpers.GetFont();
        }

        private static void Pulse()
        {
            if ((object)_line2 == null || _line2 == null) return;
            _pulse += Time.unscaledDeltaTime * 2.2f;
            float a = 0.72f + 0.28f * (0.5f + 0.5f * Mathf.Sin(_pulse));
            Color c = UITheme.Accent;
            c.a = a;
            _line2.color = c;
        }

        public static void Destroy()
        {
            if ((object)_root != null && _root != null)
                Object.Destroy(_root);
            _root = null;
            _panel = null;
            _line2 = null;
            _font = null;
        }
    }
}
