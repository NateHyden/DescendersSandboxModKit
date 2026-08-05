using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.UI
{
    public static class MenuUI
    {
        private static GameObject menuCanvas;
        private static bool menuVisible;
        private static CursorLockMode prevLock;
        private static bool prevVis;

        public static bool IsOpen => menuVisible;

        public static void ToggleMenu()
        {
            if (menuCanvas == null) menuCanvas = MenuWindow.CreateMenu();
            menuVisible = !menuVisible;
            menuCanvas.SetActive(menuVisible);
            if (menuVisible)
            {
                // No forced page here on purpose — MenuWindow's "cur" page
                // (and each page's own internal sub-tab state, e.g. Info/
                // Customize's System/Hotkeys/Customize/Career selection)
                // already persists naturally across simple show/hide,
                // since the menu GameObject is only ever built once and
                // just toggled active/inactive. So a normal F6/dpad open
                // reopens wherever you left off. Only the colour-scheme-
                // select flow (via RebuildMenu, a full destroy+rebuild)
                // explicitly overrides this to land on the Customize page.
                prevLock = Cursor.lockState; prevVis = Cursor.visible;
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                if (MenuWindow.RootCanvasGroup != null) MenuWindow.RootCanvasGroup.alpha = Mods.MenuCustomiser.CurrentOpacity;
            }
            else RestoreCursor();
        }

        public static void RestoreCursor()
        {
            Cursor.lockState = prevLock; Cursor.visible = prevVis;
        }

        // ── Destroy and recreate the menu GameObject so a colour scheme
        // change (or anything else that only takes effect at build time)
        // is reflected immediately without restarting the game. ─────────
        public static void RebuildMenu()
        {
            try
            {
                bool wasVisible = menuVisible;
                if ((object)menuCanvas != null) { UnityEngine.Object.DestroyImmediate(menuCanvas); menuCanvas = null; }
                menuCanvas = MenuWindow.CreateMenu();
                menuCanvas.SetActive(wasVisible);
                if (wasVisible && MenuWindow.RootCanvasGroup != null)
                    MenuWindow.RootCanvasGroup.alpha = Mods.MenuCustomiser.CurrentOpacity;
                MelonLogger.Msg("[MenuUI] RebuildMenu complete. wasVisible=" + wasVisible);
            }
            catch (System.Exception ex) { MelonLogger.Error("[MenuUI] RebuildMenu: " + ex.Message); }
        }
    }
}