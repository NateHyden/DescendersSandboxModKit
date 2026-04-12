using MelonLoader;
using System.IO;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // ScreenshotMode
    //
    // Two captures in consecutive frames (not same frame — Unity drops duplicates):
    //   Frame A: 2x supersampled main shot  -> Screenshots/screenshot_NNN.png
    //   Frame B: 1x preview shot            -> DescendersModMenu/preview_last.png
    //
    // The 2x shot is saved to the numbered library. The 1x preview (native res)
    // is polled and loaded as the tab preview — small file, no lag spike.
    //
    // State machine:
    //   0 = idle
    //   1 = hide all UI, wait 2 frames
    //   2 = capture 2x main, advance to state 5, wait 1 frame
    //   5 = capture 1x preview (separate frame), wait 15 frames
    //   3 = restore all UI, begin poll for preview file
    //   4 = poll every 6 frames until preview file exists (up to 30 polls)
    public static class ScreenshotMode
    {
        public static bool Enabled { get; private set; } = false;

        // ── Preview ───────────────────────────────────────────────────
        public static Texture2D PreviewTexture { get; private set; } = null;
        public static string LastPath { get; private set; } = "";
        public static string LastFilename =>
            LastPath.Length > 0 ? Path.GetFileName(LastPath) : "—";
        public static bool IsCapturing => _state != 0;

        public static string SaveFolder =>
            Path.Combine(Path.Combine(Application.persistentDataPath,
                "DescendersModMenu"), "Screenshots");

        private static string PreviewPath =>
            Path.Combine(Path.Combine(Application.persistentDataPath,
                "DescendersModMenu"), "preview_last.png");

        // ── Counter ───────────────────────────────────────────────────
        private static int _shotCounter = 0; // 0-based, next shot = (counter%100)+1

        // ── State machine ─────────────────────────────────────────────
        private static int _state = 0;
        private static int _wait = 0;
        private static string _pending = ""; // main shot path (for display)
        private static bool _menuWasOpen = false;
        private static bool _btnHeld = false;
        private static int _previewPolls = 0;
        private static int _enableGrace = 0; // skip detection for N frames after enabling

        private static readonly System.Collections.Generic.List<Canvas> _hiddenCanvases
            = new System.Collections.Generic.List<Canvas>();

        // ── InControl cache ───────────────────────────────────────────
        private static System.Reflection.PropertyInfo _activeDeviceProp = null;
        private static System.Reflection.PropertyInfo _dpadUpProp = null;
        private static System.Reflection.PropertyInfo _wasPressedProp = null;
        private static bool _incontrolSearched = false;

        // ── ScreenCapture type cache ──────────────────────────────────
        private static System.Reflection.MethodInfo _captureMethod = null;
        private static bool _captureSearched = false;

        // ── Public API ────────────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) _enableGrace = 10; // ignore input for 10 frames after enabling
            MelonLogger.Msg("[ScreenshotMode] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void TriggerScreenshot()
        {
            if (_state != 0) return;
            _state = 1; _wait = 1;
            MelonLogger.Msg("[ScreenshotMode] Triggered.");
        }

        public static void Tick()
        {
            if (!Enabled) return;

            if (_state == 0)
            {
                if (_enableGrace > 0) { _enableGrace--; return; }

                bool dpadUp = InControlDPadUp();
                bool f11 = false;
                try { f11 = Input.GetKeyDown(KeyCode.F11); } catch { }
                if ((dpadUp || f11) && !_btnHeld) { _btnHeld = true; TriggerScreenshot(); }
                if (!dpadUp && !f11) _btnHeld = false;
                return;
            }

            if (_wait > 0) { _wait--; return; }

            switch (_state)
            {
                case 1:
                    // Hide mod menu
                    _menuWasOpen = UI.MenuUI.IsOpen;
                    if (_menuWasOpen) UI.MenuUI.ToggleMenu();

                    // Hide all game canvases
                    _hiddenCanvases.Clear();
                    try
                    {
                        Canvas[] all = Object.FindObjectsOfType<Canvas>();
                        for (int i = 0; i < all.Length; i++)
                        {
                            var cv = all[i];
                            if ((object)cv == null || !cv.isRootCanvas || !cv.gameObject.activeSelf) continue;
                            var cg = cv.GetComponent<CanvasGroup>();
                            if ((object)cg != null && (object)cg == UI.MenuWindow.RootCanvasGroup) continue;
                            cv.gameObject.SetActive(false);
                            _hiddenCanvases.Add(cv);
                        }
                        MelonLogger.Msg("[ScreenshotMode] Hidden " + _hiddenCanvases.Count + " canvas(es).");
                    }
                    catch (System.Exception ex) { MelonLogger.Error("[Screenshot] HideCanvases: " + ex.Message); }

                    _state = 2; _wait = 2;
                    break;

                case 2:
                    // Frame A — capture 2x main shot
                    _shotCounter = (_shotCounter % 100) + 1;
                    _pending = Path.Combine(SaveFolder,
                        "screenshot_" + _shotCounter.ToString("D3") + ".png");
                    EnsureFolder();
                    // Delete old preview so poll waits for new write
                    try { if (File.Exists(PreviewPath)) File.Delete(PreviewPath); } catch { }
                    Capture(_pending, 2);
                    _state = 5; _wait = 1; // wait ONE frame before preview capture
                    break;

                case 5:
                    // Frame B — capture 1x preview in a separate frame
                    Capture(PreviewPath, 1);
                    MelonLogger.Msg("[ScreenshotMode] Preview capture queued.");
                    _state = 3; _wait = 15; // give both files time to start writing
                    break;

                case 3:
                    // Restore all UI
                    for (int i = 0; i < _hiddenCanvases.Count; i++)
                        try { if ((object)_hiddenCanvases[i] != null) _hiddenCanvases[i].gameObject.SetActive(true); } catch { }
                    _hiddenCanvases.Clear();
                    if (_menuWasOpen) UI.MenuUI.ToggleMenu();

                    LastPath = _pending;
                    _previewPolls = 0;
                    _state = 4; _wait = 6;
                    try { UI.ScreenshotPage.RefreshAll(); } catch { }
                    break;

                case 4:
                    // Poll for 1x preview file (small file, writes fast)
                    _previewPolls++;
                    if (File.Exists(PreviewPath))
                    {
                        MelonLogger.Msg("[ScreenshotMode] Preview ready after " + _previewPolls + " poll(s).");
                        LoadPreview(PreviewPath);
                        _state = 0;
                        try { UI.ScreenshotPage.RefreshAll(); } catch { }
                    }
                    else if (_previewPolls >= 30)
                    {
                        MelonLogger.Warning("[ScreenshotMode] Preview timed out. Use Reload Preview.");
                        _state = 0;
                    }
                    else { _wait = 6; }
                    break;
            }
        }

        private static void Capture(string path, int superSize)
        {
            try
            {
                if (!_captureSearched)
                {
                    _captureSearched = true;
                    System.Type scType = null;
                    foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        scType = asm.GetType("UnityEngine.ScreenCapture");
                        if ((object)scType != null) break;
                    }
                    if ((object)scType != null)
                        _captureMethod = scType.GetMethod("CaptureScreenshot",
                            new System.Type[] { typeof(string), typeof(int) });
                }

                if ((object)_captureMethod != null)
                {
                    _captureMethod.Invoke(null, new object[] { path, superSize });
                    MelonLogger.Msg("[ScreenshotMode] Capture " + superSize + "x -> " + path);
                }
                else MelonLogger.Error("[ScreenshotMode] CaptureScreenshot method not found.");
            }
            catch (System.Reflection.TargetInvocationException tex)
            {
                var inner = tex.InnerException ?? tex;
                MelonLogger.Error("[ScreenshotMode] Capture: " + inner.Message);
            }
            catch (System.Exception ex) { MelonLogger.Error("[ScreenshotMode] Capture: " + ex.Message); }
        }

        private static void EnsureFolder()
        {
            try { if (!Directory.Exists(SaveFolder)) Directory.CreateDirectory(SaveFolder); }
            catch (System.Exception ex) { MelonLogger.Error("[ScreenshotMode] EnsureFolder: " + ex.Message); }
        }

        private static void LoadPreview(string path)
        {
            try
            {
                if (!File.Exists(path)) { MelonLogger.Warning("[ScreenshotMode] Preview not found."); return; }
                byte[] bytes = File.ReadAllBytes(path);
                MelonLogger.Msg("[ScreenshotMode] Preview bytes: " + bytes.Length);
                if (bytes == null || bytes.Length == 0) { MelonLogger.Warning("[ScreenshotMode] Preview empty."); return; }

                if ((object)PreviewTexture != null) Object.Destroy(PreviewTexture);
                var tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    PreviewTexture = tex;
                    MelonLogger.Msg("[ScreenshotMode] Preview loaded: " + tex.width + "x" + tex.height);
                }
                else { MelonLogger.Error("[ScreenshotMode] LoadImage failed."); Object.Destroy(tex); }
            }
            catch (System.Exception ex) { MelonLogger.Error("[ScreenshotMode] LoadPreview: " + ex.Message); }
        }

        private static bool InControlDPadUp()
        {
            try
            {
                if (!_incontrolSearched)
                {
                    _incontrolSearched = true;
                    System.Type mgrType = null;
                    foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        mgrType = asm.GetType("InControl.InputManager");
                        if ((object)mgrType != null) break;
                    }
                    if ((object)mgrType == null) { MelonLogger.Warning("[ScreenshotMode] InControl not found."); return false; }
                    _activeDeviceProp = mgrType.GetProperty("ActiveDevice",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                }
                if ((object)_activeDeviceProp == null) return false;
                var device = _activeDeviceProp.GetValue(null, null);
                if ((object)device == null) return false;
                if ((object)_dpadUpProp == null) _dpadUpProp = device.GetType().GetProperty("DPadUp");
                if ((object)_dpadUpProp == null) return false;
                var dpadUp = _dpadUpProp.GetValue(device, null);
                if ((object)dpadUp == null) return false;
                if ((object)_wasPressedProp == null) _wasPressedProp = dpadUp.GetType().GetProperty("WasPressed");
                if ((object)_wasPressedProp == null) return false;
                return (bool)_wasPressedProp.GetValue(dpadUp, null);
            }
            catch { return false; }
        }

        public static void Reset()
        {
            Enabled = false; _state = 0; _wait = 0;
            _btnHeld = false; _previewPolls = 0; _enableGrace = 0;
            _hiddenCanvases.Clear();
        }

        public static void ForceReloadPreview()
        {
            if (LastPath.Length > 0)
            {
                MelonLogger.Msg("[ScreenshotMode] ForceReload preview.");
                LoadPreview(PreviewPath);
            }
        }
    }
}