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
        private static bool _reopenAfterMapChange;

        public static bool IsOpen => menuVisible;
        public static bool Locked { get; private set; }

        public static void SetLocked(bool locked)
        {
            Locked = locked;
            if (locked && menuVisible)
            {
                menuVisible = false;
                _reopenAfterMapChange = false;
                if (menuCanvas != null) menuCanvas.SetActive(false);
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
            EnsureCanvas();
            if (menuCanvas == null) return;
            SetVisible(!menuVisible);
        }

        /// <summary>
        /// Map hop destroys the non-DDOL menu canvas. Remember the tab and reopen
        /// once the next playable session is ready (not on loading screens).
        /// </summary>
        public static void OnSceneUnloaded()
        {
            try
            {
                if (menuVisible && !Locked)
                {
                    _reopenAfterMapChange = true;
                    MenuWindow.PendingPage = MenuWindow.CurrentPage;
                }
                // Do not clear _reopenAfterMapChange here — intermediate loading
                // unloads run with menuVisible=false and would wipe the flag.

                menuVisible = false;
                if (menuCanvas != null)
                {
                    Object.Destroy(menuCanvas);
                    menuCanvas = null;
                }
                // Leave cursor alone during a pending reopen; game/load UI owns it.
                if (!_reopenAfterMapChange)
                    RestoreCursor();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[MenuUI] OnSceneUnloaded: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "MenuUI");
            }
        }

        /// <summary>
        /// Call from Update once Player_Human exists so we skip loading scenes.
        /// </summary>
        public static void TryReopenAfterMapChange()
        {
            if (!_reopenAfterMapChange) return;
            if (Locked)
            {
                _reopenAfterMapChange = false;
                MenuWindow.PendingPage = -1;
                return;
            }

            try
            {
                if ((object)GameObject.Find("Player_Human") == null)
                    return;

                _reopenAfterMapChange = false;
                EnsureCanvas();
                if (menuCanvas == null) return;
                SetVisible(true);
                EnforceUnlockedCursor();
                ModLog.Debug("[MenuUI] Reopened after map change on page " + MenuWindow.CurrentPage);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[MenuUI] TryReopenAfterMapChange: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "MenuUI");
            }
        }

        /// <summary>
        /// Gameplay often re-locks the cursor after a scene load; keep it free while open
        /// so mouse clicks work again after a map hop with the menu restored.
        /// </summary>
        public static void Tick()
        {
            if (!menuVisible) return;
            EnforceUnlockedCursor();
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
                int page = MenuWindow.CurrentPage;
                if (menuCanvas != null) { Object.DestroyImmediate(menuCanvas); menuCanvas = null; }
                if (wasVisible) MenuWindow.PendingPage = page;
                menuCanvas = MenuWindow.CreateMenu();
                if (menuCanvas == null) return;
                menuCanvas.SetActive(wasVisible);
                menuVisible = wasVisible;
                if (wasVisible)
                {
                    prevLock = Cursor.lockState;
                    prevVis = Cursor.visible;
                    EnforceUnlockedCursor();
                    if (MenuWindow.RootCanvasGroup != null)
                        MenuWindow.RootCanvasGroup.alpha = Mods.MenuCustomiser.CurrentOpacity;
                }
                ModLog.Debug("[MenuUI] RebuildMenu complete. wasVisible=" + wasVisible);
            }
            catch (System.Exception ex) { MelonLogger.Error("[MenuUI] RebuildMenu: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "MenuUI"); }
        }

        private static void EnsureCanvas()
        {
            if (menuCanvas != null) return;
            menuCanvas = MenuWindow.CreateMenu();
        }

        private static void SetVisible(bool visible)
        {
            menuVisible = visible;
            if (menuCanvas != null) menuCanvas.SetActive(visible);
            if (visible)
            {
                // Remember whatever the game had (ride = locked, native menus = free)
                // so closing Sandbox doesn't break Esc / main-menu mouse.
                prevLock = Cursor.lockState;
                prevVis = Cursor.visible;
                EnforceUnlockedCursor();
                if (MenuWindow.RootCanvasGroup != null)
                    MenuWindow.RootCanvasGroup.alpha = Mods.MenuCustomiser.CurrentOpacity;
            }
            else RestoreCursor();
        }

        private static void EnforceUnlockedCursor()
        {
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible)
                Cursor.visible = true;
        }
    }
}
