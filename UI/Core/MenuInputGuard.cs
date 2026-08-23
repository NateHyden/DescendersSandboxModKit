using System;
using HarmonyLib;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.UI
{
    /// <summary>
    /// While the mod menu is open, suppress keyboard and bike sticks/triggers so
    /// browsing the menu does not steer or pedal the rider.
    /// D-Pad and face buttons are left alone so the menu-open bind can close the menu.
    /// </summary>
    public static class MenuInputGuard
    {
        private static int _bypassDepth;

        public static bool ShouldBlockGameInput
        {
            get { return MenuUI.IsOpen && _bypassDepth <= 0 && !ForceAllowInControl; }
        }

        public static bool ForceAllowInControl;

        public static bool ShouldBlockKeyboard { get { return ShouldBlockGameInput; } }

        public static void BeginBypass() { _bypassDepth++; }
        public static void EndBypass()
        {
            if (_bypassDepth > 0) _bypassDepth--;
        }

        public static bool GetKeyDown(KeyCode key)
        {
            BeginBypass();
            try { return Input.GetKeyDown(key); }
            finally { EndBypass(); }
        }

        public static bool GetKey(KeyCode key)
        {
            BeginBypass();
            try { return Input.GetKey(key); }
            finally { EndBypass(); }
        }

        public static bool GetKeyUp(KeyCode key)
        {
            BeginBypass();
            try { return Input.GetKeyUp(key); }
            finally { EndBypass(); }
        }

        internal static bool IsMouseKey(KeyCode key)
        {
            return key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6;
        }

        internal static bool AllowKey(KeyCode key)
        {
            if (!ShouldBlockGameInput) return true;
            if (key == KeyCode.None) return true;
            return IsMouseKey(key);
        }

        internal static bool IsUiScrollAxis(string axisName)
        {
            return !string.IsNullOrEmpty(axisName)
                && string.Equals(axisName, "Mouse ScrollWheel", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Only mute accel/brake triggers — never the menu-open bind, even if it is a trigger.</summary>
        internal static bool ShouldMuteTrigger(InControl.OneAxisInputControl control)
        {
            if (!ShouldBlockGameInput) return false;
            if ((object)control == null) return false;
            try
            {
                var menuCtrl = KeyBindManager.GetMenuOpenControl();
                if ((object)menuCtrl != null && ReferenceEquals(control, menuCtrl))
                    return false;

                var dev = InControl.InputManager.ActiveDevice;
                if ((object)dev == null) return false;
                return ReferenceEquals(control, dev.LeftTrigger)
                    || ReferenceEquals(control, dev.RightTrigger);
            }
            catch { return false; }
        }

        /// <summary>
        /// Mute only Left/Right sticks. Never mute DPad — InControl often drives
        /// DPadDown from DPad.Y, and zeroing that made the menu-open bind fail while open.
        /// </summary>
        internal static bool ShouldMuteStick(InControl.TwoAxisInputControl stick)
        {
            if (!ShouldBlockGameInput) return false;
            if ((object)stick == null) return false;
            try
            {
                var dev = InControl.InputManager.ActiveDevice;
                if ((object)dev == null) return false;
                return ReferenceEquals(stick, dev.LeftStick)
                    || ReferenceEquals(stick, dev.RightStick);
            }
            catch { return false; }
        }
    }

    [HarmonyPatch(typeof(Input), "GetKey", new Type[] { typeof(KeyCode) })]
    internal static class MenuInputGuard_GetKey
    {
        private static bool Prefix(KeyCode key, ref bool __result)
        {
            if (MenuInputGuard.AllowKey(key)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetKey", new Type[] { typeof(string) })]
    internal static class MenuInputGuard_GetKeyString
    {
        private static bool Prefix(string name, ref bool __result)
        {
            if (!MenuInputGuard.ShouldBlockGameInput) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetKeyDown", new Type[] { typeof(KeyCode) })]
    internal static class MenuInputGuard_GetKeyDown
    {
        private static bool Prefix(KeyCode key, ref bool __result)
        {
            if (MenuInputGuard.AllowKey(key)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetKeyDown", new Type[] { typeof(string) })]
    internal static class MenuInputGuard_GetKeyDownString
    {
        private static bool Prefix(string name, ref bool __result)
        {
            if (!MenuInputGuard.ShouldBlockGameInput) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetKeyUp", new Type[] { typeof(KeyCode) })]
    internal static class MenuInputGuard_GetKeyUp
    {
        private static bool Prefix(KeyCode key, ref bool __result)
        {
            if (MenuInputGuard.AllowKey(key)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetKeyUp", new Type[] { typeof(string) })]
    internal static class MenuInputGuard_GetKeyUpString
    {
        private static bool Prefix(string name, ref bool __result)
        {
            if (!MenuInputGuard.ShouldBlockGameInput) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetAxis", new Type[] { typeof(string) })]
    internal static class MenuInputGuard_GetAxis
    {
        private static bool Prefix(string axisName, ref float __result)
        {
            if (!MenuInputGuard.ShouldBlockGameInput) return true;
            if (MenuInputGuard.IsUiScrollAxis(axisName)) return true;
            __result = 0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetAxisRaw", new Type[] { typeof(string) })]
    internal static class MenuInputGuard_GetAxisRaw
    {
        private static bool Prefix(string axisName, ref float __result)
        {
            if (!MenuInputGuard.ShouldBlockGameInput) return true;
            if (MenuInputGuard.IsUiScrollAxis(axisName)) return true;
            __result = 0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetButton", new Type[] { typeof(string) })]
    internal static class MenuInputGuard_GetButton
    {
        private static bool Prefix(string buttonName, ref bool __result)
        {
            if (!MenuInputGuard.ShouldBlockGameInput) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetButtonDown", new Type[] { typeof(string) })]
    internal static class MenuInputGuard_GetButtonDown
    {
        private static bool Prefix(string buttonName, ref bool __result)
        {
            if (!MenuInputGuard.ShouldBlockGameInput) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetButtonUp", new Type[] { typeof(string) })]
    internal static class MenuInputGuard_GetButtonUp
    {
        private static bool Prefix(string buttonName, ref bool __result)
        {
            if (!MenuInputGuard.ShouldBlockGameInput) return true;
            __result = false;
            return false;
        }
    }

    // Triggers only (bike accel / brake)
    [HarmonyPatch(typeof(InControl.OneAxisInputControl), "get_Value")]
    internal static class MenuInputGuard_InControl_Value
    {
        private static bool Prefix(InControl.OneAxisInputControl __instance, ref float __result)
        {
            if (!MenuInputGuard.ShouldMuteTrigger(__instance)) return true;
            __result = 0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(InControl.OneAxisInputControl), "get_RawValue")]
    internal static class MenuInputGuard_InControl_RawValue
    {
        private static bool Prefix(InControl.OneAxisInputControl __instance, ref float __result)
        {
            if (!MenuInputGuard.ShouldMuteTrigger(__instance)) return true;
            __result = 0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(InControl.OneAxisInputControl), "get_IsPressed")]
    internal static class MenuInputGuard_InControl_IsPressed
    {
        private static bool Prefix(InControl.OneAxisInputControl __instance, ref bool __result)
        {
            if (!MenuInputGuard.ShouldMuteTrigger(__instance)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(InControl.OneAxisInputControl), "get_WasPressed")]
    internal static class MenuInputGuard_InControl_WasPressed
    {
        private static bool Prefix(InControl.OneAxisInputControl __instance, ref bool __result)
        {
            if (!MenuInputGuard.ShouldMuteTrigger(__instance)) return true;
            __result = false;
            return false;
        }
    }

    // Left / Right sticks only (never DPad — menu-open bind is D-Pad Down by default)
    [HarmonyPatch(typeof(InControl.TwoAxisInputControl), "get_X")]
    internal static class MenuInputGuard_InControl_StickX
    {
        private static bool Prefix(InControl.TwoAxisInputControl __instance, ref float __result)
        {
            if (!MenuInputGuard.ShouldMuteStick(__instance)) return true;
            __result = 0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(InControl.TwoAxisInputControl), "get_Y")]
    internal static class MenuInputGuard_InControl_StickY
    {
        private static bool Prefix(InControl.TwoAxisInputControl __instance, ref float __result)
        {
            if (!MenuInputGuard.ShouldMuteStick(__instance)) return true;
            __result = 0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(InControl.TwoAxisInputControl), "get_Value")]
    internal static class MenuInputGuard_InControl_StickValue
    {
        private static bool Prefix(InControl.TwoAxisInputControl __instance, ref Vector2 __result)
        {
            if (!MenuInputGuard.ShouldMuteStick(__instance)) return true;
            __result = Vector2.zero;
            return false;
        }
    }

    [HarmonyPatch(typeof(InControl.TwoAxisInputControl), "get_Vector")]
    internal static class MenuInputGuard_InControl_StickVector
    {
        private static bool Prefix(InControl.TwoAxisInputControl __instance, ref Vector2 __result)
        {
            if (!MenuInputGuard.ShouldMuteStick(__instance)) return true;
            __result = Vector2.zero;
            return false;
        }
    }
}
