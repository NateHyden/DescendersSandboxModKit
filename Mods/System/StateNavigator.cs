using System;
using System.Reflection;
using MelonLoader;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Forces the native menu StateMachine to push a given game-menu state,
    /// bypassing whatever UI element (button) would normally trigger it.
    ///
    /// Why this exists: on the Xbox/PC Game Pass build, "Workshop" and
    /// "BikeParks" never show up as menu options. Diffed Assembly-CSharp.dll
    /// between the Steam and Xbox builds (2026-08-10) - the IL for
    /// UI_MainMenu.Button_Workshop(), State_FreerideWorkshop, UI_FreerideWorkshop,
    /// ToggleModIO, and the full mod.io.UnityPlugin.dll / ModTool.dll managed
    /// stack are BYTE-IDENTICAL between builds. Nothing is stripped. The only
    /// difference found was a 5KB native "DescendantsMod.dll" (unrelated - a
    /// stray sample plugin, not part of the mod.io pipeline) missing from the
    /// Xbox Plugins folder. So the button itself is being hidden at the
    /// scene/asset level (almost certainly a UIPlatformEnabled component on
    /// the button or its parent, gating on Application.platform) - that's
    /// data baked into the scene, not something in the DLL we can patch.
    ///
    /// Instead of fighting that gate, this calls the exact same entry point
    /// the native button calls: StateMachine.PushState(GameState).
    ///
    /// Confirmed (Steam build, 2026-08-10, current version - re-verify with
    /// AssemblyScanner after any game update):
    ///   - Type "StateMachine" : Singleton`1&lt;StateMachine&gt;   (NOT obfuscated)
    ///   - Singleton`1&lt;T&gt; exposes a static getter method "get_SP"        (NOT obfuscated -
    ///     the PropertyDefinition's own Name is obfuscated, but the compiler-
    ///     generated accessor method keeps its original "get_SP" name, so we
    ///     look it up by METHOD name, not PropertyInfo name)
    ///   - StateMachine.PushState(TEnum state)                          (NOT obfuscated)
    ///   - The enum type itself is obfuscated, but its members are stable:
    ///       FreerideWorkshop  = 52
    ///       FreerideBikeParks = 53
    ///       FreerideModSelect = 36
    ///
    /// Everything below is still done via reflection with full logging (never
    /// hardcoded direct calls) per project convention, since obfuscated names
    /// shift on game updates even though these three happen not to be obfuscated.
    /// </summary>
    public static class StateNavigator
    {
        public const int State_FreerideWorkshop = 52;
        public const int State_FreerideBikeParks = 53;
        public const int State_FreerideModSelect = 36;

        public static bool PushGameState(int stateValue, string label)
        {
            try
            {
                ModLog.Debug("[StateNavigator] Attempting to push state \"" + label + "\" (" + stateValue + ")");

                Type stateMachineType = FindType("StateMachine");
                if ((object)stateMachineType == null)
                {
                    MelonLogger.Error("[StateNavigator] StateMachine type not found in any loaded assembly.");
                    return false;
                }
                ModLog.Debug("[StateNavigator] Found StateMachine type: " + stateMachineType.AssemblyQualifiedName);

                Type baseType = stateMachineType.BaseType; // expected: Singleton<StateMachine>
                if ((object)baseType == null)
                {
                    MelonLogger.Error("[StateNavigator] StateMachine.BaseType is null - can't reach Singleton<T>.");
                    return false;
                }
                ModLog.Debug("[StateNavigator] StateMachine base type: " + baseType.FullName);

                MethodInfo getSP = baseType.GetMethod("get_SP", BindingFlags.Public | BindingFlags.Static);
                if ((object)getSP == null)
                {
                    // Fallback: dump every static getter on the base type so the
                    // right one can be identified by hand if "get_SP" ever changes.
                    MelonLogger.Error("[StateNavigator] get_SP not found on " + baseType.Name + ". Dumping static methods:");
                    foreach (var m in baseType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                        ModLog.Debug("[StateNavigator]   candidate: " + m.ReturnType.Name + " " + m.Name + "()");
                    return false;
                }

                object smInstance = getSP.Invoke(null, null);
                if ((object)smInstance == null)
                {
                    MelonLogger.Error("[StateNavigator] StateMachine.SP returned null (menu system may not be initialized yet - try again once you're in the main menu).");
                    return false;
                }
                ModLog.Debug("[StateNavigator] StateMachine instance acquired.");

                MethodInfo pushState = stateMachineType.GetMethod("PushState", BindingFlags.Public | BindingFlags.Instance);
                if ((object)pushState == null)
                {
                    MelonLogger.Error("[StateNavigator] PushState method not found on StateMachine. Dumping instance methods:");
                    foreach (var m in stateMachineType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                        ModLog.Debug("[StateNavigator]   candidate: " + m.ReturnType.Name + " " + m.Name + "(" + m.GetParameters().Length + " args)");
                    return false;
                }

                ParameterInfo[] pars = pushState.GetParameters();
                if (pars.Length != 1)
                {
                    MelonLogger.Error("[StateNavigator] PushState has unexpected parameter count: " + pars.Length);
                    return false;
                }

                Type enumType = pars[0].ParameterType;
                ModLog.Debug("[StateNavigator] PushState enum parameter type: " + enumType.FullName);

                if (!enumType.IsEnum)
                {
                    MelonLogger.Error("[StateNavigator] PushState's parameter is not an enum (" + enumType.FullName + ") - game structure changed, needs re-verification.");
                    return false;
                }

                if (!Enum.IsDefined(enumType, stateValue))
                {
                    MelonLogger.Error("[StateNavigator] Value " + stateValue + " is not defined on enum " + enumType.Name + ". Dumping valid values:");
                    foreach (var name in Enum.GetNames(enumType))
                        ModLog.Debug("[StateNavigator]   " + name + " = " + (int)Enum.Parse(enumType, name));
                    return false;
                }

                object stateArg = Enum.ToObject(enumType, stateValue);
                ModLog.Debug("[StateNavigator] Invoking PushState(" + stateArg + ")");
                pushState.Invoke(smInstance, new object[] { stateArg });

                ModLog.Debug("[StateNavigator] PushState call completed without exception for \"" + label + "\".");
                return true;
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException ?? tie;
                MelonLogger.Error("[StateNavigator] PushState threw during invoke for \"" + label + "\": " + inner.GetType().FullName + ": " + inner.Message);
                MelonLogger.Error("[StateNavigator] StackTrace: " + inner.StackTrace);
                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[StateNavigator] Unexpected exception pushing \"" + label + "\": " + ex.GetType().FullName + ": " + ex.Message);
                MelonLogger.Error("[StateNavigator] StackTrace: " + ex.StackTrace);
                return false;
            }
        }

        private static Type FindType(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = null;
                try { t = asm.GetType(name); }
                catch { /* some assemblies throw on partial-load types; ignore and keep scanning */ }
                if ((object)t != null) return t;
            }
            return null;
        }
    }
}
