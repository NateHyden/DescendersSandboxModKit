using MelonLoader;
using DescendersModMenu;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.Mods
{
    public static class UIRemover
    {
        public static bool Enabled { get; private set; } = false;

        private struct HiddenCanvas
        {
            public Canvas canvas;
            public GraphicRaycaster raycaster;
            public bool wasRaycasterEnabled;
        }

        private static readonly List<HiddenCanvas> _hidden = new List<HiddenCanvas>();

        public static void Toggle()
        {
            if (Enabled) Restore();
            else Apply();
        }

        private static void Apply()
        {
            _hidden.Clear();
            try
            {
                Canvas[] all = Object.FindObjectsOfType<Canvas>();
                for (int i = 0; i < all.Length; i++)
                {
                    var cv = all[i];
                    if (!UnityNull.Alive(cv)) continue;
                    if (!cv.isRootCanvas) continue;
                    if (!cv.enabled) continue;

                    if (UnityNull.Alive(UI.MenuWindow.RootCanvasGroup))
                    {
                        Canvas modCanvas = UI.MenuWindow.RootCanvasGroup
                            .gameObject.GetComponentInParent<Canvas>();
                        if (UnityNull.Alive(modCanvas) && modCanvas == cv) continue;
                    }

                    cv.enabled = false;

                    var gr = cv.GetComponent<GraphicRaycaster>();
                    bool grWasOn = UnityNull.Alive(gr) && gr.enabled;
                    if (UnityNull.Alive(gr)) gr.enabled = false;

                    _hidden.Add(new HiddenCanvas
                    {
                        canvas = cv,
                        raycaster = gr,
                        wasRaycasterEnabled = grWasOn
                    });
                }
                Enabled = true;
                ModLog.Debug("[UIRemover] ON — hidden " + _hidden.Count + " canvas(es).");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[UIRemover] Apply: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "UIRemover");
            }
        }

        private static void Restore()
        {
            for (int i = 0; i < _hidden.Count; i++)
            {
                try
                {
                    var h = _hidden[i];
                    if (UnityNull.Alive(h.canvas)) h.canvas.enabled = true;
                    if (UnityNull.Alive(h.raycaster)) h.raycaster.enabled = h.wasRaycasterEnabled;
                }
                catch { }
            }
            _hidden.Clear();
            Enabled = false;
            ModLog.Debug("[UIRemover] OFF — game HUD restored.");
        }

        public static void ClearCache()
        {
            _hidden.Clear();
        }

        public static void Reset()
        {
            if (Enabled) Restore();
            Enabled = false;
        }
    }
}

