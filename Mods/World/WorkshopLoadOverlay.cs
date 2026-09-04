using DescendersModMenu.UI;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.Mods
{
    /// <summary>Full-screen "please wait" while a mod.io map is loading from Spot Book GO.</summary>
    public static class WorkshopLoadOverlay
    {
        private static GameObject _root;
        private static Text _title;
        private static Text _detail;
        private static float _pulse;

        public static void Show(string mapLabel)
        {
            Hide();

            string title = "Please wait";
            string detail = "Loading mod.io map…";
            if (!string.IsNullOrEmpty(mapLabel))
                detail = "Loading \"" + mapLabel + "\"…";

            _root = new GameObject("DescendersSandbox_WorkshopLoadWait");
            Object.DontDestroyOnLoad(_root);

            RectTransform rootRt = _root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 31500;

            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _root.AddComponent<GraphicRaycaster>();

            GameObject dim = new GameObject("Dim");
            dim.transform.SetParent(_root.transform, false);
            RectTransform dimRt = dim.AddComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            Image dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.55f);
            dimImg.raycastTarget = true;

            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(_root.transform, false);
            RectTransform panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(520f, 120f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.09f, 0.12f, 0.94f);

            Font font = UIHelpers.GetFont();

            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            RectTransform titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.55f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(16f, 0f);
            titleRt.offsetMax = new Vector2(-16f, -8f);
            _title = titleGo.AddComponent<Text>();
            _title.font = font;
            _title.fontSize = 22;
            _title.fontStyle = FontStyle.Bold;
            _title.alignment = TextAnchor.MiddleCenter;
            _title.color = new Color(0.96f, 0.95f, 0.99f, 0.98f);
            _title.text = title;

            GameObject detailGo = new GameObject("Detail");
            detailGo.transform.SetParent(panel.transform, false);
            RectTransform detailRt = detailGo.AddComponent<RectTransform>();
            detailRt.anchorMin = new Vector2(0f, 0f);
            detailRt.anchorMax = new Vector2(1f, 0.55f);
            detailRt.offsetMin = new Vector2(16f, 8f);
            detailRt.offsetMax = new Vector2(-16f, 0f);
            _detail = detailGo.AddComponent<Text>();
            _detail.font = font;
            _detail.fontSize = 16;
            _detail.alignment = TextAnchor.MiddleCenter;
            _detail.color = UITheme.Accent;
            _detail.text = detail;
        }

        public static void Tick()
        {
            if ((object)_root == null || _root == null || (object)_detail == null)
                return;
            _pulse += Time.unscaledDeltaTime * 2f;
            float a = 0.65f + 0.35f * (0.5f + 0.5f * Mathf.Sin(_pulse));
            Color c = UITheme.Accent;
            c.a = a;
            _detail.color = c;
        }

        public static void Hide()
        {
            if ((object)_root != null && _root != null)
                Object.Destroy(_root);
            _root = null;
            _title = null;
            _detail = null;
            _pulse = 0f;
        }

        /// <summary>Brief on-screen message (e.g. map not installed).</summary>
        public static void ShowMessage(string title, string detail, float autoHideSeconds = 3.5f)
        {
            Show(null);
            if ((object)_title != null)
                _title.text = string.IsNullOrEmpty(title) ? "Notice" : title;
            if ((object)_detail != null)
            {
                _detail.text = detail ?? "";
                _detail.color = new Color(0.96f, 0.55f, 0.45f, 0.98f);
            }
            if (autoHideSeconds > 0f)
                MelonLoader.MelonCoroutines.Start(AutoHideRoutine(autoHideSeconds));
        }

        private static System.Collections.IEnumerator AutoHideRoutine(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Hide();
        }
    }
}
