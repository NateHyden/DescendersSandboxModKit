using System.Collections;
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
        private static bool _closing;
        private static CursorLockMode prevLock;
        private static bool prevVis;
        private static bool _reopenAfterMapChange;
        private static object _animRoutine;
        private static bool _prebuildStarted;

        private const float AnimInDur = 0.18f;
        private const float AnimOutDur = 0.14f;
        private const float PopScale = 0.82f;

        public static bool IsOpen => menuVisible;
        public static bool Locked { get; private set; }

        public static void SetLocked(bool locked)
        {
            Locked = locked;
            if (locked && menuVisible)
            {
                StopAnim();
                _closing = false;
                menuVisible = false;
                _reopenAfterMapChange = false;
                if (menuCanvas != null) menuCanvas.SetActive(false);
                SnapToIdleTransform();
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

            // Closing anim in progress → reopen; otherwise toggle.
            if (menuVisible && !_closing)
                SetVisible(false);
            else
                SetVisible(true);
        }

        /// <summary>
        /// Map hop destroys the non-DDOL menu canvas. Remember the tab and reopen
        /// once the next playable session is ready (not on loading screens).
        /// </summary>
        public static void OnSceneUnloaded()
        {
            try
            {
                bool menuWasOpen = menuVisible && !Locked;
                if (menuWasOpen)
                {
                    _reopenAfterMapChange = true;
                    MenuWindow.PendingPage = MenuWindow.CurrentPage;
                }

                StopAnim();
                _closing = false;
                menuVisible = false;
                _prebuildStarted = false;
                if (menuCanvas != null)
                {
                    try { MenuWindow.StopPageWarm(); } catch { }
                    Object.Destroy(menuCanvas);
                    menuCanvas = null;
                }
                if (menuWasOpen && !_reopenAfterMapChange)
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
        /// Build the inactive menu canvas while riding so the first F6 does not hitch.
        /// Starts once Player_Human exists, after a short delay so spawn/load stays smooth.
        /// </summary>
        public static void TryPrebuild()
        {
            if (Locked || _prebuildStarted || menuCanvas != null) return;
            if ((object)GameObject.Find("Player_Human") == null) return;
            _prebuildStarted = true;
            MelonCoroutines.Start(PrebuildRoutine());
        }

        private static IEnumerator PrebuildRoutine()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            if (Locked || menuCanvas != null) yield break;
            try
            {
                EnsureCanvas();
                ModLog.Debug("[MenuUI] Prebuilt menu canvas in background");
            }
            catch (System.Exception ex)
            {
                _prebuildStarted = false;
                MelonLogger.Error("[MenuUI] TryPrebuild: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "MenuUI.TryPrebuild");
            }
        }

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
                bool wasVisible = menuVisible && !_closing;
                int page = MenuWindow.CurrentPage;
                StopAnim();
                _closing = false;
                try { MenuWindow.StopPageWarm(); } catch { }
                if (menuCanvas != null) { Object.DestroyImmediate(menuCanvas); menuCanvas = null; }
                if (wasVisible) MenuWindow.PendingPage = page;
                menuCanvas = MenuWindow.CreateMenu();
                if (menuCanvas == null) return;
                menuCanvas.SetActive(wasVisible);
                menuVisible = wasVisible;
                SnapToIdleTransform();
                if (wasVisible)
                {
                    prevLock = Cursor.lockState;
                    prevVis = Cursor.visible;
                    EnforceUnlockedCursor();
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
            StopAnim();

            if (visible)
            {
                _closing = false;
                menuVisible = true;
                if (menuCanvas != null) menuCanvas.SetActive(true);
                prevLock = Cursor.lockState;
                prevVis = Cursor.visible;
                EnforceUnlockedCursor();
                _animRoutine = MelonCoroutines.Start(AnimIn());
            }
            else
            {
                if (!menuVisible || menuCanvas == null)
                {
                    menuVisible = false;
                    _closing = false;
                    RestoreCursor();
                    return;
                }
                _closing = true;
                _animRoutine = MelonCoroutines.Start(AnimOut());
            }
        }

        private static void StopAnim()
        {
            if (_animRoutine == null) return;
            try { MelonCoroutines.Stop(_animRoutine); } catch { }
            _animRoutine = null;
        }

        private static void SnapToIdleTransform()
        {
            float s = MenuCustomiser.CurrentScale;
            float a = MenuCustomiser.CurrentOpacity;
            if (MenuWindow.RootRT != null)
                MenuWindow.RootRT.localScale = new Vector3(s, s, 1f);
            if (MenuWindow.RootCanvasGroup != null)
            {
                MenuWindow.RootCanvasGroup.alpha = a;
                MenuWindow.RootCanvasGroup.blocksRaycasts = true;
                MenuWindow.RootCanvasGroup.interactable = true;
            }
        }

        private static IEnumerator AnimIn()
        {
            var rt = MenuWindow.RootRT;
            var cg = MenuWindow.RootCanvasGroup;
            float targetS = MenuCustomiser.CurrentScale;
            float targetA = MenuCustomiser.CurrentOpacity;
            float startS = targetS * PopScale;

            if (rt != null) rt.localScale = new Vector3(startS, startS, 1f);
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }

            float t = 0f;
            while (t < AnimInDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / AnimInDur);
                float e = 1f - (1f - k) * (1f - k) * (1f - k);
                float s = Mathf.Lerp(startS, targetS, e);
                if (rt != null) rt.localScale = new Vector3(s, s, 1f);
                if (cg != null) cg.alpha = Mathf.Lerp(0f, targetA, e);
                yield return null;
            }

            SnapToIdleTransform();
            _animRoutine = null;
        }

        private static IEnumerator AnimOut()
        {
            var rt = MenuWindow.RootRT;
            var cg = MenuWindow.RootCanvasGroup;
            float startS = rt != null ? rt.localScale.x : MenuCustomiser.CurrentScale;
            float startA = cg != null ? cg.alpha : MenuCustomiser.CurrentOpacity;
            float endS = MenuCustomiser.CurrentScale * PopScale;

            if (cg != null)
            {
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }

            float t = 0f;
            while (t < AnimOutDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / AnimOutDur);
                float e = k * k;
                float s = Mathf.Lerp(startS, endS, e);
                if (rt != null) rt.localScale = new Vector3(s, s, 1f);
                if (cg != null) cg.alpha = Mathf.Lerp(startA, 0f, e);
                yield return null;
            }

            menuVisible = false;
            _closing = false;
            if (menuCanvas != null) menuCanvas.SetActive(false);
            SnapToIdleTransform();
            RestoreCursor();
            _animRoutine = null;
        }

        private static void EnforceUnlockedCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
