using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// ModTool keeps ModSearchDirectory entries after a mod.io unsubscribe deletes the
    /// folder. BackgroundRefresh then throws DirectoryNotFoundException. Guard the scan
    /// so missing dirs are treated as empty instead of spamming the Melon log.
    /// </summary>
    internal static class ModSearchDirectoryGuard
    {
        private static bool _applied;
        private static PropertyInfo _pathProp;
        private static readonly string[] EmptyPaths = new string[0];

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            if (_applied || harmony == null) return;

            try
            {
                Type searchType = FindModSearchDirectoryType();
                if ((object)searchType == null)
                {
                    // ModTool may load later — EnsurePatched will retry quietly.
                    return;
                }

                _pathProp = searchType.GetProperty("path",
                    BindingFlags.Public | BindingFlags.Instance);
                MethodInfo getPaths = searchType.GetMethod("GetModInfoPaths",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo doRefresh = searchType.GetMethod("DoRefresh",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)getPaths != null)
                {
                    harmony.Patch(getPaths,
                        prefix: new HarmonyMethod(typeof(ModSearchDirectoryGuard)
                            .GetMethod(nameof(GetModInfoPathsPrefix),
                                BindingFlags.Public | BindingFlags.Static)));
                }

                if ((object)doRefresh != null)
                {
                    harmony.Patch(doRefresh,
                        finalizer: new HarmonyMethod(typeof(ModSearchDirectoryGuard)
                            .GetMethod(nameof(DoRefreshFinalizer),
                                BindingFlags.Public | BindingFlags.Static)));
                }

                _applied = (object)getPaths != null || (object)doRefresh != null;
                if (_applied)
                    ModLog.Debug("[ModSearchGuard] Patched ModSearchDirectory refresh.");
            }
            catch (MissingMethodException ex)
            {
                // Old Unity Mono missing newer BCL APIs — don't spam EnsurePatched retries.
                _applied = true;
                MelonLogger.Error("[ModSearchGuard] ApplyPatch: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "ModSearchDirectoryGuard");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[ModSearchGuard] ApplyPatch: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "ModSearchDirectoryGuard");
            }
        }

        public static void EnsurePatched(HarmonyLib.Harmony harmony)
        {
            if (!_applied)
                ApplyPatch(harmony);
        }

        public static bool GetModInfoPathsPrefix(object __instance, ref string[] __result)
        {
            try
            {
                if ((object)__instance == null) return true;
                if ((object)_pathProp == null) return true;

                string path = _pathProp.GetValue(__instance, null) as string;
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    __result = EmptyPaths;
                    return false;
                }
            }
            catch
            {
                __result = EmptyPaths;
                return false;
            }
            return true;
        }

        public static Exception DoRefreshFinalizer(Exception __exception)
        {
            if (__exception is DirectoryNotFoundException)
                return null;
            return __exception;
        }

        private static Type FindModSearchDirectoryType()
        {
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                try
                {
                    if (!string.Equals(asms[i].GetName().Name, "ModTool", StringComparison.Ordinal))
                        continue;
                    return asms[i].GetType("ModTool.ModSearchDirectory");
                }
                catch { }
            }

            try
            {
                string dataPath = UnityEngine.Application.dataPath;
                string gameRoot = Path.GetDirectoryName(dataPath) ?? "";
                string managed = Path.Combine(Path.Combine(Path.Combine(gameRoot, "Descenders_Data"), "Managed"), "ModTool.dll");
                if (!File.Exists(managed))
                    managed = Path.Combine(Path.Combine(dataPath, "Managed"), "ModTool.dll");
                if (File.Exists(managed))
                {
                    Assembly asm = Assembly.LoadFrom(managed);
                    return asm.GetType("ModTool.ModSearchDirectory");
                }
            }
            catch { }

            return null;
        }
    }
}
