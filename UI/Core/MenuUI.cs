using MelonLoader;
using DescendersModMenu;
using DescendersModMenu.Mods;
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
        public static bool Locked { get; private set; }

        public static void SetLocked(bool locked)
        {
            Locked = locked;
            if (locked && menuVisible)
            {
                menuVisible = false;
                if ((object)menuCanvas != null) menuCanvas.SetActive(false);
                RestoreCursor();
            }
        }

        public static void ToggleMenu()
        {
            if (Locked)
            {
                ModLog.Feedback("Menu disabled in Multiplayer Race Mode");
                return;
            }
            if (menuCanvas == null) menuCanvas = MenuWindow.CreateMenu();
            menuVisible = !menuVisible;
            menuCanvas.SetActive(menuVisible);
            if (menuVisible)
            {
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
                ModLog.Debug("[MenuUI] RebuildMenu complete. wasVisible=" + wasVisible);
            }
            catch (System.Exception ex) { MelonLogger.Error("[MenuUI] RebuildMenu: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "MenuUI"); }
        }
    }
}

