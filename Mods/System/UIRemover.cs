using MelonLoader;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.Mods
{
    // UIRemover — hides all game HUD canvases while keeping input working.
    //
    // Uses canvas.enabled = false instead of gameObject.SetActive(false).
    // This stops rendering but leaves the EventSystem alive so UI clicks still work.
    // Also disables GraphicRaycaster to stop hidden canvases consuming clicks.
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
                    if ((object)cv == null) continue;
                    if (!cv.isRootCanvas) continue;
                    if (!cv.enabled) continue;

                    // Skip our mod menu canvas
                    if ((object)UI.MenuWindow.RootCanvasGroup != null)
                    {
                        Canvas modCanvas = UI.MenuWindow.RootCanvasGroup
                            .gameObject.GetComponentInParent<Canvas>();
                        if ((object)modCanvas != null && modCanvas == cv) continue;
                    }

                    // Disable canvas rendering (leaves GameObject + EventSystem alive)
                    cv.enabled = false;

                    // Also disable GraphicRaycaster so hidden UI can't intercept clicks
                    var gr = cv.GetComponent<GraphicRaycaster>();
                    bool grWasOn = (object)gr != null && gr.enabled;
                    if ((object)gr != null) gr.enabled = false;

                    _hidden.Add(new HiddenCanvas
                    {
                        canvas = cv,
                        raycaster = gr,
                        wasRaycasterEnabled = grWasOn
                    });
                }
                Enabled = true;
                MelonLogger.Msg("[UIRemover] ON — hidden " + _hidden.Count + " canvas(es).");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[UIRemover] Apply: " + ex.Message);
            }
        }

        private static void Restore()
        {
            for (int i = 0; i < _hidden.Count; i++)
            {
                try
                {
                    var h = _hidden[i];
                    if ((object)h.canvas != null) h.canvas.enabled = true;
                    if ((object)h.raycaster != null) h.raycaster.enabled = h.wasRaycasterEnabled;
                }
                catch { }
            }
            _hidden.Clear();
            Enabled = false;
            MelonLogger.Msg("[UIRemover] OFF — game HUD restored.");
        }

        public static void ClearCache()
        {
            // Canvas components are destroyed with the scene — just clear the list.
            _hidden.Clear();
            // Keep Enabled — snapshot will re-apply on the new scene.
        }

        public static void Reset()
        {
            if (Enabled) Restore();
            Enabled = false;
        }
    }
}