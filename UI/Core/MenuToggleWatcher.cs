using MelonLoader;
using DescendersModMenu;
using DescendersModMenu.Mods;
using System;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.UI
{
    /// <summary>
    /// Always-on menu toggle from the controller bind. Lives on a DontDestroyOnLoad
    /// object so it keeps running whether the menu canvas is active or not, and uses
    /// LateUpdate so it runs after InControl has committed pad state for the frame.
    /// </summary>
    public class MenuToggleWatcher : MonoBehaviour
    {
        private static MenuToggleWatcher _instance;
        private static FieldInfo _thisStateField;
        private static FieldInfo _stateField;
        private bool _heldLast;
        private int _toggleFrame = -1;

        public static void Ensure()
        {
            if ((object)_instance != null) return;
            try
            {
                var go = new GameObject("DescendersSandbox_MenuToggleWatcher");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<MenuToggleWatcher>();
                ModLog.Debug("[MenuToggleWatcher] Started.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[MenuToggleWatcher] Ensure: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "MenuToggleWatcher");
            }
        }

        void LateUpdate()
        {
            try
            {
                if (BindsPage.IsListening) { _heldLast = false; return; }
                if (_toggleFrame == Time.frameCount) return;

                int code = KeyBindManager.GetMenuOpenCode();
                if (code == 0) { _heldLast = false; return; }

                bool held = ReadHeld(code);
                if (held && !_heldLast)
                {
                    _toggleFrame = Time.frameCount;
                    MenuUI.ToggleMenu();
                }
                _heldLast = held;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[MenuToggleWatcher] LateUpdate: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "MenuToggleWatcher");
            }
        }

        private static bool ReadHeld(int code)
        {
            var control = KeyBindManager.GetMenuOpenControl();
            if ((object)control == null) return false;

            // Prefer private thisState — immune to any Harmony property prefixes.
            try
            {
                if ((object)_thisStateField == null)
                {
                    _thisStateField = typeof(InControl.OneAxisInputControl).GetField(
                        "thisState",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                }
                if ((object)_thisStateField != null)
                {
                    object boxed = _thisStateField.GetValue(control);
                    if ((object)boxed != null)
                    {
                        if ((object)_stateField == null)
                            _stateField = boxed.GetType().GetField("State");
                        if ((object)_stateField != null)
                            return (bool)_stateField.GetValue(boxed);
                    }
                }
            }
            catch { }

            return control.IsPressed;
        }
    }
}
