using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DescendersModMenu;
using DescendersModMenu.BikeStats;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Loads mod.io / workshop maps by matching a saved scene key to ModTool.Mod and
    /// invoking the same StartMod entry point the freeride workshop UI uses.
    /// </summary>
    public static class ModWorkshopLoader
    {
        private static System.Type _modManagerType = null;
        private static System.Type _modType = null;
        private static System.Type _modSceneType = null;
        private static PropertyInfo _modManagerInstanceProp = null;
        private static PropertyInfo _modsProp = null;
        private static PropertyInfo _sceneNamesProp = null;
        private static PropertyInfo _scenesProp = null;
        private static PropertyInfo _modSceneNameProp = null;
        private static MethodInfo _uiStartMod = null;
        private static MethodInfo _startNewSessionModMethod = null;
        private static System.Type _gameModifierListType = null;
        private static bool _sessionModStartResolved = false;
        private static PropertyInfo _gameDataActiveModProp = null;
        private static FieldInfo _gameDataActiveModFld = null;
        private static PropertyInfo _gameDataModManagerProp = null;
        private static FieldInfo _modManagerModsFld = null;
        private static MethodInfo _refreshModsMethod = null;
        private static MethodInfo _addSearchDirMethod = null;
        private static MethodInfo _uiRefreshMethod = null;
        private static FieldInfo _workshopModListFld = null;
        private static PropertyInfo _gameDataModsListProp = null;
        private static FieldInfo _gameDataModsListFld = null;
        private static MethodInfo _modLoadMethod = null;
        private static MethodInfo _modLoadAsyncMethod = null;
        private static PropertyInfo _modLoadStateProp = null;
        private static PropertyInfo _modCanLoadProp = null;
        private static PropertyInfo _modLoadProgressProp = null;
        private static MethodInfo _modUnloadMethod = null;
        private static MethodInfo _dispatcherProcessQueueMethod = null;
        private static MethodInfo _freerideStateStartModMethod = null;
        private static readonly List<object> _catalogCache = new List<object>();
        private static bool _catalogBuilt = false;
        private static bool _startModRoutineRunning = false;
        private static MethodInfo _uiUnpackingMod = null;
        private static MethodInfo _uiHoverMod = null;
        private static PropertyInfo _gameDataLoadedModProp = null;
        private static MethodInfo _initializeFreerideModsMethod = null;
        private static MethodInfo _initModManagerMethod = null;
        private static PropertyInfo _modEnabledProp = null;
        private static bool _searchPathsDone = false;
        private static object _cachedModManager = null;
        private static int _forceSandboxModSessionDepth = 0;
        private static bool _forceWorkshopAssetReload = false;

        public static void BeginForceSandboxModSession()
        {
            _forceSandboxModSessionDepth++;
        }

        public static void EndForceSandboxModSession()
        {
            if (_forceSandboxModSessionDepth > 0)
                _forceSandboxModSessionDepth--;
        }

        public static bool IsForceSandboxModSession()
        {
            return _forceSandboxModSessionDepth > 0;
        }

        /// <summary>Active mod.io mod from GameData — no ModManager mod-list scan (safe during workshop rides).</summary>
        public static bool TryGetActiveWorkshopModInfo(out string displayName, out string path)
        {
            displayName = null;
            path = null;
            try
            {
                if (!Resolve()) return false;
                object mod = TryGetGameDataLoadedMod();
                if (mod == null)
                    mod = TryGetActiveModFromGameData();
                if (mod == null) return false;
                displayName = ReadModDisplayName(mod);
                path = ReadModPath(mod);
                return !string.IsNullOrEmpty(displayName);
            }
            catch { return false; }
        }

        public static bool TryGetWorkshopPathForBookmark(MapChanger.MapBookmark bm, out string path)
        {
            path = "";
            try
            {
                if (!bm.Valid || !Resolve()) return false;
                object mod = FindModForBookmark(bm);
                if (mod == null) return false;
                path = ReadModPath(mod);
                return !string.IsNullOrEmpty(path);
            }
            catch { return false; }
        }

        public static object FindModForBookmarkPublic(MapChanger.MapBookmark bm)
        {
            return FindModForBookmark(bm);
        }

        public static string ReadModPathPublic(object mod)
        {
            return ReadModPath(mod);
        }

        /// <summary>Mod folder on disk (never a .info file path).</summary>
        public static string NormalizeWorkshopPathForSave(string path)
        {
            string folder = GetWorkshopFolderPath(path);
            return !string.IsNullOrEmpty(folder) ? folder : path;
        }

        public static void WriteDiagnostics(System.Action<string> appendLine)
        {
            if (appendLine == null) return;
            try
            {
                appendLine("=== MOD.IO / WORKSHOP DIAGNOSTICS ===");
                appendLine("Active scene: " + MapChanger.GetCurrentSceneName());
                appendLine("InWorkshopLevel: " + MapChanger.InWorkshopLevel());
                appendLine("persistentDataPath: " + Application.persistentDataPath);

                string activeName;
                string activePath;
                if (TryGetActiveWorkshopModInfo(out activeName, out activePath))
                {
                    appendLine("GameData active mod: " + activeName);
                    appendLine("GameData active path: " + activePath);
                }
                else
                    appendLine("GameData active mod: (none)");

                if (!Resolve())
                {
                    appendLine("ModTool.Resolve: FAILED");
                    return;
                }

                object mgr = GetModManager();
                appendLine("ModManager: " + (mgr != null ? mgr.ToString() : "(null)"));
                if (mgr != null && (object)_modsProp != null)
                {
                    object dir = _modManagerType.GetProperty("defaultSearchDirectory")
                        .GetValue(mgr, null);
                    appendLine("defaultSearchDirectory: " + (dir != null ? dir.ToString() : ""));
                }

                List<object> mods = new List<object>();
                CollectMods(mods);
                appendLine("Catalog mod count: " + mods.Count);
                for (int i = 0; i < mods.Count && i < 40; i++)
                {
                    object mod = mods[i];
                    if (mod == null) continue;
                    appendLine("  [" + i + "] " + ReadModDisplayName(mod)
                        + " | path=" + ReadModPath(mod)
                        + " | scenes=" + ReadModSceneNames(mod));
                }
                if (mods.Count > 40)
                    appendLine("  ... (" + (mods.Count - 40) + " more)");
            }
            catch (System.Exception ex)
            {
                appendLine("Mod diagnostics error: " + ex.Message);
            }
        }

        private static string ReadModSceneNames(object mod)
        {
            if (mod == null || (object)_sceneNamesProp == null) return "";
            try
            {
                object namesObj = _sceneNamesProp.GetValue(mod, null);
                if (namesObj is IList names)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int i = 0; i < names.Count && i < 6; i++)
                    {
                        if (i > 0) sb.Append(',');
                        object n = names[i];
                        if (n != null) sb.Append(n.ToString());
                    }
                    return sb.ToString();
                }
            }
            catch { }
            return "";
        }

        public static bool TryLoadByDisplayName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName) || !Resolve()) return false;
            object mod = FindModForDisplayName(displayName);
            if (mod == null) return false;
            return TryStartMod(mod, displayName);
        }

        public static bool TryLoadByScenePart(string scenePart)
        {
            if (string.IsNullOrEmpty(scenePart) || !Resolve()) return false;
            object mod = FindModForScenePart(scenePart);
            if (mod == null) return false;
            return TryStartMod(mod, ReadModDisplayName(mod));
        }

        public static bool TryLoadByPath(string workshopPath)
        {
            if (string.IsNullOrEmpty(workshopPath) || !Resolve()) return false;
            return TryDeferredWorkshopLoad(null, workshopPath, null);
        }

        public static bool IsBookmarkSubscribed(MapChanger.MapBookmark bm)
        {
            return FindModForBookmark(bm) != null;
        }

        public static bool TryLoadBookmark(MapChanger.MapBookmark bm)
        {
            if (!bm.Valid) return false;
            object mod = FindModForBookmark(bm);
            if (mod != null)
                return TryStartMod(mod, ReadModDisplayName(mod));

            string scenePart = null;
            if (!string.IsNullOrEmpty(bm.SceneName))
                scenePart = MapChanger.SanitizeStoragePartPublic(bm.SceneName);
            return TryDeferredWorkshopLoad(bm.DisplayLabel, null, scenePart);
        }

        /// <summary>Starts load even when mod is not in cache yet — runs disk catalog scan in coroutine.</summary>
        public static bool IsWorkshopLoadInProgress()
        {
            return _startModRoutineRunning;
        }

        public static bool TryDeferredWorkshopLoad(
            string displayName, string workshopPath, string scenePart)
        {
            if (!Resolve()) return false;

            string modName = ResolveWorkshopModLabel(displayName, workshopPath, scenePart);

            if (!IsWorkshopMapInstalledLocally(displayName, workshopPath, scenePart))
            {
                ReportWorkshopMapNotFound(modName);
                return false;
            }

            ModLog.Feedback("[SavedLoc] Loading mod.io map \"" + modName + "\"...");
            MelonCoroutines.Start(StartModRoutineWrapper(null, modName, workshopPath, scenePart));
            return true;
        }

        public static bool TryLoadSceneMap(string sceneStorageKey)
        {
            if (string.IsNullOrEmpty(sceneStorageKey)
                || !sceneStorageKey.StartsWith("scene_", System.StringComparison.Ordinal))
                return false;

            if (!Resolve())
            {
                ModLog.Feedback("[SavedLoc] Workshop loader not available.");
                return false;
            }

            if (!EnsureModsAccess())
            {
                ModLog.Feedback("[SavedLoc] Workshop loader not available.");
                return false;
            }

            object mod = FindModForScenePart(sceneStorageKey.Substring(6));
            if (mod == null)
            {
                string label = MapChanger.SceneToDisplayLabel(sceneStorageKey.Substring(6));
                ReportWorkshopMapNotFound(label);
                return false;
            }

            return TryStartMod(mod, ReadModDisplayName(mod));
        }

        /// <summary>Mod display name for a live scene (e.g. "Mega Ramp" instead of "modio map megaramp").</summary>
        public static bool TryGetDisplayNameForSceneName(string sceneName, out string displayName)
        {
            displayName = null;
            if (string.IsNullOrEmpty(sceneName)) return false;
            string safe = MapChanger.SanitizeStoragePartPublic(sceneName);
            if (string.IsNullOrEmpty(safe)) return false;
            return TryGetDisplayNameForSceneKey("scene_" + safe, out displayName);
        }

        /// <summary>Mod display name for a saved scene_* storage key.</summary>
        public static bool TryGetDisplayNameForSceneKey(string sceneStorageKey, out string displayName)
        {
            displayName = null;
            if (string.IsNullOrEmpty(sceneStorageKey)
                || !sceneStorageKey.StartsWith("scene_", System.StringComparison.Ordinal))
                return false;
            if (!Resolve()) return false;

            object mod = FindModForScenePart(sceneStorageKey.Substring(6));
            if (mod == null) return false;

            displayName = ReadModDisplayName(mod);
            return !string.IsNullOrEmpty(displayName);
        }

        /// <summary>True when a subscribed mod.io map matching the scene key is on disk.</summary>
        public static bool IsSceneMapSubscribed(string sceneStorageKey)
        {
            if (string.IsNullOrEmpty(sceneStorageKey)
                || !sceneStorageKey.StartsWith("scene_", System.StringComparison.Ordinal))
                return false;
            if (MapChanger.InWorkshopLevel())
                return true;
            if (!Resolve()) return false;
            EnsureCatalogBuilt(false);
            return FindModForScenePartCached(sceneStorageKey.Substring(6)) != null
                || FindModForDisplayNameCached(
                    MapChanger.SceneToDisplayLabel(sceneStorageKey.Substring(6))) != null;
        }

        /// <summary>False when saved install path is gone or mod cannot be found on disk.</summary>
        public static bool IsWorkshopMapInstalledLocally(
            string displayName, string workshopPath, string scenePart)
        {
            if (!Resolve()) return false;

            if (IsSavedWorkshopInstallPresent(workshopPath))
                return true;

            string modName = ResolveWorkshopModLabel(displayName, workshopPath, scenePart);

            if (ResolveModWithoutCtor(null, modName, workshopPath, scenePart) != null)
                return true;

            EnsureCatalogBuilt(false);
            if (ResolveModWithoutCtor(null, modName, workshopPath, scenePart) != null)
                return true;

            if (!string.IsNullOrEmpty(scenePart))
            {
                object sceneMod = FindModForScenePartCached(scenePart);
                if (sceneMod != null) return true;
            }

            if (!string.IsNullOrEmpty(modName))
            {
                object nameMod = FindModForDisplayNameCached(modName);
                if (nameMod != null) return true;
            }

            if (!string.IsNullOrEmpty(scenePart) && IsSceneMapSubscribed("scene_" + scenePart))
                return true;

            if (IsSavedWorkshopInstallMissing(workshopPath))
                return false;

            return false;
        }

        public static void ReportWorkshopMapNotFound(string modLabel)
        {
            string label = string.IsNullOrEmpty(modLabel) ? "This mod.io map" : "\"" + modLabel + "\"";
            string detail = label + " is not installed.\nSubscribe in Mod.io or reinstall from the workshop.";
            ModLog.Feedback("[SavedLoc] Map not found - " + label + " is not installed locally.");
            WorkshopLoadOverlay.ShowMessage("Map not found", detail);
            SavedLocations.CancelPendingTeleport();
        }

        public static string ResolveWorkshopModLabelPublic(
            string displayName, string workshopPath, string scenePart)
        {
            return ResolveWorkshopModLabel(displayName, workshopPath, scenePart);
        }

        private static string ResolveWorkshopModLabel(
            string displayName, string workshopPath, string scenePart)
        {
            string modName = displayName;
            if (string.IsNullOrEmpty(modName) && !string.IsNullOrEmpty(workshopPath))
            {
                string folder = GetWorkshopFolderPath(workshopPath);
                if (!string.IsNullOrEmpty(folder))
                    modName = System.IO.Path.GetFileName(folder);
            }
            if (string.IsNullOrEmpty(modName) && !string.IsNullOrEmpty(scenePart))
                modName = MapChanger.SceneToDisplayLabel(scenePart);
            if (string.IsNullOrEmpty(modName))
                modName = "Workshop map";
            return modName;
        }

        private static bool IsSavedWorkshopInstallPresent(string workshopPath)
        {
            if (string.IsNullOrEmpty(workshopPath)) return false;

            try
            {
                if (workshopPath.EndsWith(".info", System.StringComparison.OrdinalIgnoreCase)
                    && System.IO.File.Exists(workshopPath))
                    return true;

                if (System.IO.Directory.Exists(workshopPath))
                    return true;

                if (System.IO.File.Exists(workshopPath))
                    return true;

                string folder = GetWorkshopFolderPath(workshopPath);
                if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
                    return true;

                string info = GetWorkshopInfoPath(workshopPath);
                if (!string.IsNullOrEmpty(info) && System.IO.File.Exists(info))
                    return true;
            }
            catch { }

            return false;
        }

        private static bool IsSavedWorkshopInstallMissing(string workshopPath)
        {
            if (string.IsNullOrEmpty(workshopPath)) return false;

            try
            {
                string norm = workshopPath.Replace('\\', '/');
                bool installPath = norm.IndexOf("_installedMods", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || norm.IndexOf("/modio", System.StringComparison.OrdinalIgnoreCase) >= 0;

                if (!installPath) return false;

                if (workshopPath.EndsWith(".info", System.StringComparison.OrdinalIgnoreCase))
                    return !System.IO.File.Exists(workshopPath);

                if (System.IO.Directory.Exists(workshopPath))
                    return false;

                if (System.IO.File.Exists(workshopPath))
                    return false;

                string folder = GetWorkshopFolderPath(workshopPath);
                if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
                    return false;

                string info = GetWorkshopInfoPath(workshopPath);
                if (!string.IsNullOrEmpty(info) && System.IO.File.Exists(info))
                    return false;

                return true;
            }
            catch { return false; }
        }

        private static bool TryStartMod(object mod, string modName)
        {
            if (mod == null) return false;
            if (string.IsNullOrEmpty(modName))
                modName = ReadModDisplayName(mod);
            ModLog.Feedback("[SavedLoc] Loading mod.io map \"" + modName + "\"...");
            MelonCoroutines.Start(StartModRoutineWrapper(mod, modName, ReadModPath(mod), null));
            return true;
        }

        private static IEnumerator StartModRoutineWrapper(
            object mod, string modName, string workshopPath, string scenePart)
        {
            if (_startModRoutineRunning)
            {
                ModLog.Feedback("[SavedLoc] Mod.io load already in progress.");
                yield break;
            }

            _startModRoutineRunning = true;
            _forceWorkshopAssetReload = false;
            WorkshopLoadOverlay.Show(modName);
            yield return StartModRoutine(mod, modName, workshopPath, scenePart);
            WorkshopLoadOverlay.Hide();
            _forceWorkshopAssetReload = false;
            _startModRoutineRunning = false;
            SavedLocations.NotifyWorkshopLoadComplete();
        }

        private static IEnumerator StartModRoutine(
            object mod, string modName, string workshopPath, string scenePart)
        {
            yield return null;

            ModLog.Feedback("[SavedLoc] Finding mod.io map \"" + modName + "\"...");
            TryInitializeGameDataMods();
            yield return null;

            if (!string.IsNullOrEmpty(workshopPath))
                RefreshCatalogForLoad(workshopPath);

            IEnumerator catalogWait = WaitForModCatalogLight(modName, workshopPath, scenePart);
            while (catalogWait != null && catalogWait.MoveNext())
                yield return catalogWait.Current;

            object resolved = mod;
            if (resolved == null)
                resolved = ResolveModWithoutCtor(null, modName, workshopPath, scenePart);

            if (resolved == null)
            {
                for (int retry = 0; retry < 60; retry++)
                {
                    resolved = ResolveModWithoutCtor(null, modName, workshopPath, scenePart);
                    if (resolved != null)
                        break;
                    yield return null;
                }
            }

            if (resolved == null && !string.IsNullOrEmpty(workshopPath))
                resolved = FindModByPath(workshopPath);

            if (resolved == null && !string.IsNullOrEmpty(workshopPath))
            {
                ModLog.Feedback("[SavedLoc] Reading mod files for \"" + modName + "\"...");
                yield return null;
                resolved = TryCreateModFromWorkshopPath(workshopPath);
            }

            if (resolved == null)
            {
                ReportWorkshopMapNotFound(modName);
                yield break;
            }

            mod = resolved;
            if (string.IsNullOrEmpty(modName))
                modName = ReadModDisplayName(mod);

            TryEnableMod(mod);
            yield return null;

            object startMod = ResolveModForStartMod(mod, modName, workshopPath);
            if (startMod == null)
                startMod = mod;

            if (FindModInGameDataListOnly(modName, workshopPath, startMod) == null)
                RegisterModInGameData(startMod);

            mod = ResolveModForStartMod(startMod, modName, workshopPath) ?? startMod;
            TryEnableMod(mod);
            _forceWorkshopAssetReload = ShouldForceWorkshopAssetReload(modName, workshopPath, mod);

            object listMod = FindModInGameDataListOnly(modName, workshopPath, mod);
            if (listMod != null && IsModResourceLoaded(listMod))
                mod = listMod;

            bool resourcesReady = IsModResourceLoaded(mod);

            ModLog.Feedback(resourcesReady
                ? "[SavedLoc] Preparing mod.io map \"" + modName + "\"..."
                : "[SavedLoc] Unpacking mod.io map \"" + modName + "\"...");

            if (!resourcesReady)
            {
                object prevLoaded = TryGetGameDataLoadedMod();
                if (prevLoaded != null && !IsSameModEntry(mod, prevLoaded))
                    UnloadPreviousLoadedMod();

                UnloadMatchingModsInGameData(modName, workshopPath, mod);
                if (!IsModResourceLoaded(mod))
                    TryUnloadMod(mod);
                yield return null;
                yield return null;
            }

            IEnumerator unpackWait = WaitForModUnpackRoutine(mod, modName, workshopPath);
            while (unpackWait != null && unpackWait.MoveNext())
                yield return unpackWait.Current;

            mod = ResolveModForStartMod(mod, modName, workshopPath) ?? mod;
            listMod = FindModInGameDataListOnly(modName, workshopPath, mod);
            if (listMod != null && IsModResourceLoaded(listMod))
                mod = listMod;

            if (!IsModResourceLoaded(mod))
            {
                LogModLoadDiagnostics(mod, modName);
                ModLog.Warn("[ModWorkshopLoader] Mod unpack/load slow for \"" + modName + "\"");
                ModLog.Feedback("[SavedLoc] Could not unpack mod.io map \"" + modName + "\".");
                SavedLocations.CancelPendingTeleport();
                yield break;
            }

            ModLog.Debug("[ModWorkshopLoader] Mod unpacked \"" + modName + "\"");
            TrySetGameDataLoadedMod(mod);

            ModLog.Feedback("[SavedLoc] Starting mod.io map \"" + modName + "\"...");
            yield return null;

            MapChanger.CloseCurrentSessionPublic();
            MapChanger.SuppressInactivityWarningPublic();
            yield return null;
            yield return null;
            yield return null;

            mod = ResolveModForStartMod(mod, modName, workshopPath) ?? mod;
            PinModToGameData(mod);

            BeginForceSandboxModSession();
            bool workshopSessionStarted = false;
            try
            {
                IEnumerator beginSession = TryBeginWorkshopSessionRoutine(
                    mod, modName, workshopPath, started => workshopSessionStarted = started);
                while (beginSession != null && beginSession.MoveNext())
                    yield return beginSession.Current;

                PinModToGameData(mod);

                if (workshopSessionStarted)
                {
                    bool retriedSubscene = false;
                    for (int verify = 0; verify < 1800; verify++)
                    {
                        PumpModDispatcher();
                        MapChanger.ApplySandboxWorkshopRidePublic();
                        PinModToGameData(mod);

                        if (!IsModWorkshopSubsceneLoaded(mod) && verify > 240 && !retriedSubscene)
                        {
                            retriedSubscene = true;
                            ModLog.Debug("[ModWorkshopLoader] Mod subscene missing — re-pinning mod and "
                                + "retrying session for \"" + modName + "\"");
                            mod = ResolveModForStartMod(mod, modName, workshopPath) ?? mod;
                            PinModToGameData(mod);
                            TryInvokeWorkshopUi(mod);
                            yield return null;
                            yield return null;
                            yield return null;
                            if (!IsModWorkshopSubsceneLoaded(mod))
                                TryStartModDirect(mod);
                            verify = 0;
                            continue;
                        }

                        if (verify >= 60 && IsWorkshopSessionReady(mod))
                        {
                            MapChanger.SuppressInactivityWarningPublic();
                            ModLog.Feedback("[SavedLoc] Mod.io map loaded.");
                            yield break;
                        }
                        yield return null;
                    }
                }
            }
            finally
            {
                EndForceSandboxModSession();
            }

            if (IsModWorkshopSubsceneLoaded(mod)
                || IsWorkshopModActive(modName, workshopPath)
                || MapChanger.IsWorkshopRidePlayableForLoadComplete())
            {
                ModLog.Feedback("[SavedLoc] Mod.io map loaded (ride still settling).");
                SavedLocations.NotifyWorkshopLoadComplete();
                yield break;
            }

            if (MapChanger.InWorkshopLevel())
            {
                ModLog.Feedback("[SavedLoc] Mod.io map loading — press A to spawn if needed.");
                SavedLocations.NotifyWorkshopLoadComplete();
                yield break;
            }

            ModLog.Feedback("[SavedLoc] Could not start mod.io map \"" + modName + "\".");
            SavedLocations.CancelPendingTeleport();
        }

        private static IEnumerator TryBeginWorkshopSessionRoutine(
            object mod, string modName, string workshopPath, System.Action<bool> onStarted)
        {
            bool started = false;
            if (mod == null)
            {
                if (onStarted != null) onStarted(false);
                yield break;
            }

            IEnumerator ensureState = EnsureWorkshopStateReadyRoutine();
            while (ensureState != null && ensureState.MoveNext())
                yield return ensureState.Current;

            for (int attempt = 0; attempt < 4 && !started; attempt++)
            {
                if (attempt > 0)
                {
                    ModLog.Debug("[ModWorkshopLoader] Retrying workshop session start ("
                        + attempt + ") for \"" + modName + "\"");
                    for (int w = 0; w < 30; w++)
                        yield return null;
                    if (StatsManager.IsInMenuContext())
                        StateNavigator.PushGameState(
                            StateNavigator.State_FreerideWorkshop, "Workshop");
                    for (int w = 0; w < 30; w++)
                        yield return null;
                }

                if (StatsManager.IsInMenuContext() && TryInvokeFreerideStateStart(mod))
                {
                    started = true;
                    break;
                }

                if (TryStartModDirect(mod))
                {
                    started = true;
                    break;
                }

                if (!StatsManager.IsInMenuContext() && TryInvokeFreerideStateStart(mod))
                {
                    started = true;
                    break;
                }
            }

            if (!started)
            {
                object uiMod = ResolveModForStartMod(mod, modName, workshopPath) ?? mod;
                if (GetWorkshopUi() != null && TryInvokeWorkshopUi(uiMod))
                {
                    ModLog.Debug("[ModWorkshopLoader] Workshop UI StartMod for \"" + modName + "\"");
                    started = true;
                }
            }

            if (onStarted != null)
                onStarted(started);
        }

        private static bool IsWorkshopModActive(string modName, string workshopPath)
        {
            object loaded = TryGetGameDataLoadedMod();
            if (loaded != null)
            {
                string loadedPath = ReadModPath(loaded);
                if (!string.IsNullOrEmpty(workshopPath)
                    && ModPathsMatch(workshopPath, loadedPath))
                    return true;
                string loadedName = ReadModInfoName(loaded);
                if (!string.IsNullOrEmpty(modName)
                    && WorkshopNamesMatch(modName, loadedName))
                    return true;
            }

            string activeName;
            string activePath;
            if (!TryGetActiveWorkshopModInfo(out activeName, out activePath))
                return false;
            if (!string.IsNullOrEmpty(workshopPath)
                && ModPathsMatch(workshopPath, activePath))
                return true;
            if (!string.IsNullOrEmpty(modName)
                && WorkshopNamesMatch(modName, activeName))
                return true;
            return false;
        }

        private static void PinModToGameData(object mod)
        {
            if (mod == null) return;
            TrySetGameDataLoadedMod(mod);
            try
            {
                System.Type gdType = FindGameType("GameData");
                if ((object)gdType == null) return;
                object gd = GetGameDataSingleton(gdType);
                if (gd == null) return;

                PropertyInfo[] props = gdType.GetProperties(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < props.Length; i++)
                {
                    PropertyInfo p = props[i];
                    if (!TypesMatch(p.PropertyType, _modType) || !p.CanWrite) continue;
                    p.SetValue(gd, mod, null);
                    if ((object)_gameDataActiveModProp == null)
                        _gameDataActiveModProp = p;
                }

                FieldInfo[] fields = gdType.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo f = fields[i];
                    if (!TypesMatch(f.FieldType, _modType)) continue;
                    f.SetValue(gd, mod);
                    if ((object)_gameDataActiveModFld == null)
                        _gameDataActiveModFld = f;
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[ModWorkshopLoader] PinModToGameData: " + ex.Message);
            }
        }

        private static bool IsModWorkshopSubsceneLoaded(object mod)
        {
            if (mod == null) return false;
            try
            {
                int count = SceneManager.sceneCount;
                for (int i = 0; i < count; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (!scene.IsValid() || !scene.isLoaded) continue;
                    string sceneName = scene.name;
                    if (string.IsNullOrEmpty(sceneName) || sceneName == "modscene")
                        continue;
                    if (ModSceneNameMatchesMod(mod, sceneName))
                        return true;
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[ModWorkshopLoader] IsModWorkshopSubsceneLoaded: " + ex.Message);
            }
            return false;
        }

        private static bool ModSceneNameMatchesMod(object mod, string sceneName)
        {
            if (mod == null || string.IsNullOrEmpty(sceneName)) return false;

            string catalogScenes = ReadModSceneNames(mod);
            if (!string.IsNullOrEmpty(catalogScenes))
            {
                string[] parts = catalogScenes.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    string part = (parts[i] ?? "").Trim();
                    if (part.Length == 0) continue;
                    if (sceneName.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            string modName = ReadModInfoName(mod);
            if (!string.IsNullOrEmpty(modName)
                && sceneName.IndexOf(modName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        private static IEnumerator WaitForModCatalogLight(
            string modName, string workshopPath, string scenePart = null)
        {
            AddSearchDirectoriesForPath(workshopPath);
            EnsureCatalogBuilt(false);
            if (FindModForLoad(modName, workshopPath, scenePart) != null)
                yield break;

            for (int i = 0; i < 45; i++)
            {
                if (FindModForLoad(modName, workshopPath, scenePart) != null)
                    yield break;
                yield return null;
            }

            RefreshDiskCatalog();
            InvalidateCatalog();
            EnsureCatalogBuilt(true);

            for (int i = 0; i < 30; i++)
            {
                if (FindModForLoad(modName, workshopPath, scenePart) != null)
                    yield break;
                yield return null;
            }
        }

        private static void TryInitializeGameDataMods()
        {
            try
            {
                System.Type gdType = FindGameType("GameData");
                if ((object)gdType == null) return;
                object gd = GetGameDataSingleton(gdType);
                if (gd == null) return;

                if ((object)_initializeFreerideModsMethod == null)
                    _initializeFreerideModsMethod = gdType.GetMethod(
                        "InitializeFreerideMods",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)_initModManagerMethod == null)
                    _initModManagerMethod = gdType.GetMethod(
                        "InitModManager",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)_initializeFreerideModsMethod != null)
                    _initializeFreerideModsMethod.Invoke(gd, null);
                if ((object)_initModManagerMethod != null)
                    _initModManagerMethod.Invoke(gd, null);
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[ModWorkshopLoader] InitializeFreerideMods: " + ex.Message);
            }
        }

        private static object FindModInGameDataListOnly(
            string modName, string workshopPath, object hintMod)
        {
            object listObj = TryGetGameDataModsList();
            if (listObj is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    object entry = list[i];
                    if (entry == null) continue;
                    if (hintMod != null && ModsMatchForStart(entry, hintMod))
                        return entry;
                    if (!string.IsNullOrEmpty(workshopPath)
                        && ModPathsMatch(workshopPath, ReadModPath(entry)))
                        return entry;
                    if (!string.IsNullOrEmpty(modName)
                        && WorkshopNamesMatch(
                            NormalizeModName(modName),
                            NormalizeModName(ReadModDisplayName(entry))))
                        return entry;
                }
            }

            return null;
        }

        private static object FindModInGameDataList(
            string modName, string workshopPath, object hintMod)
        {
            object fromList = FindModInGameDataListOnly(modName, workshopPath, hintMod);
            if (fromList != null)
                return fromList;

            object catalog = FindModForLoad(modName, workshopPath, null);
            if (catalog != null)
            {
                object again = FindModInGameDataListOnly(modName, workshopPath, catalog);
                if (again != null) return again;
                return catalog;
            }

            return null;
        }

        private static void RegisterModInGameData(object mod)
        {
            if (mod == null) return;
            try
            {
                object listObj = TryGetGameDataModsList();
                if (listObj is IList list)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        object entry = list[i];
                        if (entry != null && ModsMatchForStart(entry, mod))
                            return;
                    }
                    list.Add(mod);
                    ModLog.Debug("[ModWorkshopLoader] Registered mod in GameData.mods: "
                        + ReadModDisplayName(mod));
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[ModWorkshopLoader] RegisterModInGameData: " + ex.Message);
            }
        }

        private static bool ModsMatchForStart(object a, object b)
        {
            if (a == null || b == null) return false;
            string nameA = ReadModInfoName(a);
            string nameB = ReadModInfoName(b);
            if (string.IsNullOrEmpty(nameA) || string.IsNullOrEmpty(nameB))
                return false;
            if (!string.Equals(nameA, nameB, System.StringComparison.OrdinalIgnoreCase))
                return false;

            string verA = ReadModVersion(a);
            string verB = ReadModVersion(b);
            if (string.IsNullOrEmpty(verA) || string.IsNullOrEmpty(verB))
                return true;
            return string.Equals(verA, verB, System.StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadModInfoName(object mod)
        {
            if (mod == null || !Resolve()) return "";
            try
            {
                PropertyInfo infoProp = _modType.GetProperty("modInfo");
                if ((object)infoProp == null) return ReadModDisplayName(mod);
                object info = infoProp.GetValue(mod, null);
                if (info == null) return ReadModDisplayName(mod);
                PropertyInfo nameProp = info.GetType().GetProperty("name");
                if ((object)nameProp != null)
                {
                    object n = nameProp.GetValue(info, null);
                    if (n != null && !string.IsNullOrEmpty(n.ToString()))
                        return n.ToString();
                }
            }
            catch { }
            return ReadModDisplayName(mod);
        }

        private static string ReadModVersion(object mod)
        {
            if (mod == null || !Resolve()) return "";
            try
            {
                PropertyInfo infoProp = _modType.GetProperty("modInfo");
                if ((object)infoProp == null) return "";
                object info = infoProp.GetValue(mod, null);
                if (info == null) return "";
                PropertyInfo verProp = info.GetType().GetProperty("version");
                if ((object)verProp != null)
                {
                    object v = verProp.GetValue(info, null);
                    return v != null ? v.ToString() : "";
                }
            }
            catch { }
            return "";
        }

        private static void TrySetGameDataLoadedMod(object mod)
        {
            if (mod == null) return;
            try
            {
                System.Type gdType = FindGameType("GameData");
                if ((object)gdType == null) return;
                object gd = GetGameDataSingleton(gdType);
                if (gd == null) return;

                if ((object)_gameDataLoadedModProp == null)
                    _gameDataLoadedModProp = gdType.GetProperty("loadedMod");

                if ((object)_gameDataLoadedModProp != null && _gameDataLoadedModProp.CanWrite)
                    _gameDataLoadedModProp.SetValue(gd, mod, null);
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[ModWorkshopLoader] set_loadedMod: " + ex.Message);
            }
        }

        private static object TryGetGameDataLoadedMod()
        {
            try
            {
                System.Type gdType = FindGameType("GameData");
                if ((object)gdType == null) return null;
                object gd = GetGameDataSingleton(gdType);
                if (gd == null) return null;

                if ((object)_gameDataLoadedModProp == null)
                    _gameDataLoadedModProp = gdType.GetProperty("loadedMod");

                if ((object)_gameDataLoadedModProp == null) return null;
                return _gameDataLoadedModProp.GetValue(gd, null);
            }
            catch { return null; }
        }

        private static void ClearGameDataLoadedMod()
        {
            try
            {
                System.Type gdType = FindGameType("GameData");
                if ((object)gdType == null) return;
                object gd = GetGameDataSingleton(gdType);
                if (gd == null) return;

                if ((object)_gameDataLoadedModProp == null)
                    _gameDataLoadedModProp = gdType.GetProperty("loadedMod");

                if ((object)_gameDataLoadedModProp != null && _gameDataLoadedModProp.CanWrite)
                    _gameDataLoadedModProp.SetValue(gd, null, null);
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[ModWorkshopLoader] clear loadedMod: " + ex.Message);
            }
        }

        private static void TryUnloadMod(object mod)
        {
            if (mod == null || !Resolve()) return;
            try
            {
                if ((object)_modUnloadMethod == null)
                    _modUnloadMethod = _modType.GetMethod("Unload");

                if ((object)_modUnloadMethod != null)
                    _modUnloadMethod.Invoke(mod, null);
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[ModWorkshopLoader] Unload mod: " + ex.Message);
            }
        }

        private static bool ShouldForceWorkshopAssetReload(
            string modName, string workshopPath, object mod)
        {
            if (mod == null) return true;
            object check = ResolveModForStartMod(mod, modName, workshopPath) ?? mod;
            if (IsModResourceLoaded(check))
                return false;

            object listMod = FindModInGameDataListOnly(modName, workshopPath, check);
            if (listMod != null && IsModResourceLoaded(listMod))
                return false;

            object prev = TryGetGameDataLoadedMod();
            if (prev != null && IsSameModEntry(check, prev) && IsModResourceLoaded(prev))
                return false;

            return true;
        }

        private static IEnumerator WaitForModUnpackRoutine(
            object mod, string modName, string workshopPath)
        {
            if (mod == null) yield break;

            if (IsModResourceLoaded(mod))
                yield break;

            if (NeedsModAssetReload(mod))
            {
                IEnumerator load = ForceModLoadRoutine(mod);
                while (load != null && load.MoveNext())
                    yield return load.Current;
            }

            bool retried = false;
            for (int i = 0; i < 1800; i++)
            {
                PumpModDispatcher();
                if (i % 30 == 0)
                    TryInvokeUnpackingMod();

                object resolved = ResolveModForStartMod(mod, modName, workshopPath) ?? mod;
                if (IsModResourceLoaded(resolved))
                    yield break;

                object listMod = FindModInGameDataListOnly(modName, workshopPath, resolved);
                if (listMod != null && IsModResourceLoaded(listMod))
                    yield break;

                if (i == 900 && !retried)
                {
                    retried = true;
                    ModLog.Debug("[ModWorkshopLoader] Mod unpack slow — retrying Load for \""
                        + modName + "\"");
                    object retryMod = listMod ?? resolved;
                    TryUnloadMod(retryMod);
                    yield return null;
                    yield return null;
                    try
                    {
                        if ((object)_modLoadAsyncMethod != null)
                            _modLoadAsyncMethod.Invoke(retryMod, null);
                        else if ((object)_modLoadMethod != null)
                            _modLoadMethod.Invoke(retryMod, null);
                        TryInvokeUnpackingMod();
                    }
                    catch (System.Exception ex)
                    {
                        ModLog.Debug("[ModWorkshopLoader] Mod unpack retry: " + ex.Message);
                    }
                }

                yield return null;
            }
        }

        private static void UnloadMatchingModsInGameData(
            string modName, string workshopPath, object skipMod)
        {
            if (!Resolve()) return;
            object listObj = TryGetGameDataModsList();
            if (!(listObj is IList list)) return;

            for (int i = 0; i < list.Count; i++)
            {
                object entry = list[i];
                if (entry == null) continue;
                if (skipMod != null && System.Object.ReferenceEquals(entry, skipMod))
                    continue;

                bool match = false;
                if (!string.IsNullOrEmpty(workshopPath)
                    && ModPathsMatch(workshopPath, ReadModPath(entry)))
                    match = true;
                else if (!string.IsNullOrEmpty(modName)
                    && WorkshopNamesMatch(modName, ReadModInfoName(entry)))
                    match = true;

                if (match)
                    TryUnloadMod(entry);
            }
        }

        private static bool NeedsModAssetReload(object mod)
        {
            if (mod == null) return true;
            if (IsModResourceLoaded(mod))
                return false;
            if (_forceWorkshopAssetReload)
                return true;

            object loaded = TryGetGameDataLoadedMod();
            if (loaded == null || !IsSameModEntry(mod, loaded))
                return true;

            if (!MapChanger.InWorkshopLevel())
                return true;

            return !IsModResourceLoaded(mod);
        }

        private static bool IsSameModEntry(object a, object b)
        {
            if (a == null || b == null) return false;
            if (System.Object.ReferenceEquals(a, b)) return true;
            string pathA = ReadModPath(a);
            string pathB = ReadModPath(b);
            if (!string.IsNullOrEmpty(pathA) && !string.IsNullOrEmpty(pathB))
                return ModPathsMatch(pathA, pathB);
            string nameA = ReadModInfoName(a);
            string nameB = ReadModInfoName(b);
            return !string.IsNullOrEmpty(nameA)
                && string.Equals(nameA, nameB, System.StringComparison.OrdinalIgnoreCase);
        }

        private static object ResolveModForStartMod(
            object hintMod, string modName, string workshopPath)
        {
            string wantName = ReadModInfoName(hintMod);
            string wantVer = ReadModVersion(hintMod);
            if (string.IsNullOrEmpty(wantName) && !string.IsNullOrEmpty(modName))
                wantName = modName;

            object listObj = TryGetGameDataModsList();
            if (listObj is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    object entry = list[i];
                    if (entry == null) continue;

                    if (!string.IsNullOrEmpty(wantName))
                    {
                        string entryName = ReadModInfoName(entry);
                        if (!string.Equals(entryName, wantName, System.StringComparison.OrdinalIgnoreCase))
                            continue;

                        string entryVer = ReadModVersion(entry);
                        if (!string.IsNullOrEmpty(wantVer) && !string.IsNullOrEmpty(entryVer)
                            && !string.Equals(entryVer, wantVer, System.StringComparison.OrdinalIgnoreCase))
                            continue;

                        return entry;
                    }

                    if (!string.IsNullOrEmpty(workshopPath)
                        && ModPathsMatch(workshopPath, ReadModPath(entry)))
                        return entry;
                }

                if (!string.IsNullOrEmpty(wantName))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        object entry = list[i];
                        if (entry == null) continue;
                        if (WorkshopNamesMatch(wantName, ReadModInfoName(entry)))
                            return entry;
                    }
                }
            }

            return FindModInGameDataList(modName, workshopPath, hintMod);
        }

        private static void UnloadPreviousLoadedMod()
        {
            try
            {
                object prev = TryGetGameDataLoadedMod();
                if (prev != null && Resolve())
                {
                    if ((object)_modUnloadMethod == null)
                        _modUnloadMethod = _modType.GetMethod("Unload");

                    if ((object)_modUnloadMethod != null)
                        _modUnloadMethod.Invoke(prev, null);
                }

                ClearGameDataLoadedMod();
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[ModWorkshopLoader] Unload previous mod: " + ex.Message);
            }
        }

        private static IEnumerator ForceModLoadRoutine(object mod)
        {
            if (mod == null || !Resolve()) yield break;

            if (!NeedsModAssetReload(mod))
                yield break;

            TryUnloadMod(mod);
            yield return null;

            try
            {
                if ((object)_modLoadAsyncMethod != null)
                    _modLoadAsyncMethod.Invoke(mod, null);
                else if ((object)_modLoadMethod != null)
                    _modLoadMethod.Invoke(mod, null);

                TryInvokeUnpackingMod();
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[ModWorkshopLoader] Mod.LoadAsync: " + ex.Message);
            }

            for (int i = 0; i < 1200; i++)
            {
                PumpModDispatcher();
                if (i % 30 == 0)
                    TryInvokeUnpackingMod();
                if (IsModResourceLoaded(mod))
                    yield break;
                yield return null;
            }

            if (!IsModResourceLoaded(mod) && (object)_modLoadMethod != null)
            {
                try
                {
                    _modLoadMethod.Invoke(mod, null);
                    PumpModDispatcher();
                }
                catch (System.Exception ex)
                {
                    ModLog.Debug("[ModWorkshopLoader] Mod.Load sync fallback: " + ex.Message);
                }
            }
        }

        private static void PumpModDispatcher()
        {
            try
            {
                if ((object)_dispatcherProcessQueueMethod == null)
                {
                    foreach (Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (asm.GetName().Name != "ModTool") continue;
                        System.Type dispType = asm.GetType("ModTool.Dispatcher");
                        if ((object)dispType == null) continue;
                        _dispatcherProcessQueueMethod = dispType.GetMethod(
                            "ProcessQueue",
                            BindingFlags.Public | BindingFlags.Static);
                        if ((object)_dispatcherProcessQueueMethod != null)
                            break;
                    }
                }

                if ((object)_dispatcherProcessQueueMethod != null)
                    _dispatcherProcessQueueMethod.Invoke(null, null);
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[ModWorkshopLoader] PumpModDispatcher: " + ex.Message);
            }
        }

        private static bool TryInvokeFreerideStateStart(object mod)
        {
            if (mod == null || !Resolve() || !ResolveSessionModStart()) return false;

            try
            {
                System.Type stateType = FindGameType("State_FreerideWorkshop");
                if ((object)stateType == null) return false;

                if ((object)_freerideStateStartModMethod == null)
                {
                    MethodInfo named = stateType.GetMethod(
                        "lfmgFFR",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if ((object)named != null)
                    {
                        ParameterInfo[] np = named.GetParameters();
                        if (np.Length == 2 && TypesMatch(np[0].ParameterType, _modType)
                            && TypesMatch(np[1].ParameterType, _gameModifierListType))
                            _freerideStateStartModMethod = named;
                    }

                    if ((object)_freerideStateStartModMethod == null)
                    {
                        MethodInfo[] methods = stateType.GetMethods(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        for (int i = 0; i < methods.Length; i++)
                        {
                            MethodInfo m = methods[i];
                            ParameterInfo[] p = m.GetParameters();
                            if (p.Length != 2) continue;
                            if (!TypesMatch(p[0].ParameterType, _modType)) continue;
                            if (!TypesMatch(p[1].ParameterType, _gameModifierListType)) continue;
                            _freerideStateStartModMethod = m;
                            break;
                        }
                    }
                }

                if ((object)_freerideStateStartModMethod == null) return false;

                object state = FindObjectOfTypeSafe(stateType);
                if (state == null) return false;

                var stateMb = state as MonoBehaviour;
                if (stateMb == null || !stateMb.isActiveAndEnabled)
                    return false;

                MapChanger.ApplySandboxWorkshopRidePublic();

                object modifierList = BuildSessionModifierListForStart();
                PinModToGameData(mod);
                _freerideStateStartModMethod.Invoke(state, new object[] { mod, modifierList });
                MapChanger.ApplySandboxWorkshopRidePublic();
                PinModToGameData(mod);
                return true;
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[ModWorkshopLoader] Freeride state start: " + ex.Message);
                return false;
            }
        }

        private static bool IsModResourceLoaded(object mod)
        {
            if (mod == null || !Resolve()) return false;
            try
            {
                if ((object)_modLoadStateProp != null)
                {
                    object state = _modLoadStateProp.GetValue(mod, null);
                    if (state != null)
                    {
                        int stateInt = System.Convert.ToInt32(state);
                        if (stateInt == 2)
                            return true;

                        string stateName = state.ToString();
                        if (!string.IsNullOrEmpty(stateName)
                            && stateName.IndexOf("Loaded", System.StringComparison.OrdinalIgnoreCase) >= 0
                            && stateName.IndexOf("Unloaded", System.StringComparison.OrdinalIgnoreCase) < 0
                            && stateName.IndexOf("Loading", System.StringComparison.OrdinalIgnoreCase) < 0)
                            return true;
                    }
                }

                if ((object)_modLoadProgressProp != null)
                {
                    object progress = _modLoadProgressProp.GetValue(mod, null);
                    if (progress is float f && f >= 0.99f)
                        return true;
                }
            }
            catch { }
            return false;
        }

        private static void LogModLoadDiagnostics(object mod, string modName)
        {
            if (mod == null || !Resolve()) return;
            try
            {
                string state = "?";
                string progress = "?";
                string canLoad = "?";
                if ((object)_modLoadStateProp != null)
                    state = _modLoadStateProp.GetValue(mod, null)?.ToString() ?? "?";
                if ((object)_modLoadProgressProp != null)
                    progress = _modLoadProgressProp.GetValue(mod, null)?.ToString() ?? "?";
                if ((object)_modCanLoadProp != null)
                    canLoad = _modCanLoadProp.GetValue(mod, null)?.ToString() ?? "?";
                ModLog.Debug("[ModWorkshopLoader] Load state for \"" + modName
                    + "\": state=" + state + " progress=" + progress
                    + " canLoad=" + canLoad + " path=" + ReadModPath(mod));
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[ModWorkshopLoader] Load diagnostics: " + ex.Message);
            }
        }

        private static IEnumerator EnsureWorkshopStateReadyRoutine()
        {
            System.Type stateType = FindGameType("State_FreerideWorkshop");

            for (int i = 0; i < 300; i++)
            {
                bool needPush = GetWorkshopUi() == null;
                if (!needPush && (object)stateType != null)
                {
                    object state = FindObjectOfTypeSafe(stateType);
                    var mb = state as MonoBehaviour;
                    if (mb == null || !mb.isActiveAndEnabled)
                        needPush = true;
                }

                if (needPush && i % 45 == 0)
                    StateNavigator.PushGameState(StateNavigator.State_FreerideWorkshop, "Workshop");

                if (GetWorkshopUi() != null && IsFreerideWorkshopStateActive(stateType))
                    yield break;

                yield return null;
            }
        }

        private static bool IsFreerideWorkshopStateActive(System.Type stateType)
        {
            if ((object)stateType == null)
                return true;

            object state = FindObjectOfTypeSafe(stateType);
            var mb = state as MonoBehaviour;
            return mb != null && mb.isActiveAndEnabled;
        }

        private static void TryHoverMod(object mod)
        {
            if (mod == null) return;
            try
            {
                System.Type uiType = FindGameType("UI_FreerideWorkshop");
                if ((object)uiType == null) return;

                if ((object)_uiHoverMod == null
                    || !System.Object.ReferenceEquals(_uiHoverMod.DeclaringType, uiType))
                    _uiHoverMod = uiType.GetMethod(
                        "HoveringOverMod",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)_uiHoverMod == null) return;

                object ui = GetWorkshopUi();
                if (ui != null)
                    _uiHoverMod.Invoke(ui, new object[] { mod });
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[ModWorkshopLoader] HoveringOverMod: " + ex.Message);
            }
        }

        private static bool TryInvokeUnpackingMod()
        {
            try
            {
                System.Type uiType = FindGameType("UI_FreerideWorkshop");
                if ((object)uiType == null) return false;

                if ((object)_uiUnpackingMod == null
                    || !System.Object.ReferenceEquals(_uiUnpackingMod.DeclaringType, uiType))
                    _uiUnpackingMod = uiType.GetMethod(
                        "UnpackingMod",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)_uiUnpackingMod == null) return false;

                object ui = GetWorkshopUi();
                if (ui == null) return false;

                _uiUnpackingMod.Invoke(ui, new object[] { true, true });
                return true;
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[ModWorkshopLoader] UnpackingMod: " + ex.Message);
                return false;
            }
        }

        private static void TryEnableMod(object mod)
        {
            if (mod == null || !Resolve()) return;
            try
            {
                if ((object)_modEnabledProp == null)
                    _modEnabledProp = _modType.GetProperty("isEnabled");
                if ((object)_modEnabledProp != null && _modEnabledProp.CanWrite)
                    _modEnabledProp.SetValue(mod, true, null);
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[ModWorkshopLoader] Enable mod: " + ex.Message);
            }
        }

        private static object BuildSessionModifierListForStart()
        {
            IList crewMods = CrewPerkManager.BuildSessionModifierList();
            object modifierList = System.Activator.CreateInstance(_gameModifierListType);
            if (crewMods != null && modifierList is IList il)
            {
                for (int i = 0; i < crewMods.Count; i++)
                    il.Add(crewMods[i]);
            }
            return modifierList;
        }

        private static bool IsWorkshopSessionReady(object mod)
        {
            if (MapChanger.IsWorkshopRidePlayableForLoadComplete())
                return true;

            if (mod == null || !IsModWorkshopSubsceneLoaded(mod))
                return false;

            if (!MapChanger.IsSessionStarted())
                return false;

            if (MapChanger.IsGeneratingRideState())
                return false;

            return MapChanger.IsRideStateInGame()
                || MapChanger.IsWorkshopRidePlayableForLoadComplete();
        }

        /// <summary>Fallback when workshop state is unavailable — mirrors lfmgFFR session calls.</summary>
        private static bool TryStartModDirect(object mod)
        {
            if (mod == null || !Resolve() || !ResolveSessionModStart()) return false;

            try
            {
                object smInstance = GetSessionManagerSingleton();
                if (smInstance == null) return false;

                MapChanger.ApplySandboxWorkshopRidePublic();
                PinModToGameData(mod);

                object modifierList = BuildSessionModifierListForStart();
                _startNewSessionModMethod.Invoke(smInstance, new object[] { mod, modifierList });
                MapChanger.ApplySandboxWorkshopRidePublic();
                PinModToGameData(mod);
                MapChanger.PushGeneratingStatePublic();
                return MapChanger.IsSessionStarted()
                    || MapChanger.IsGeneratingRideState();
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[ModWorkshopLoader] Direct mod load: " + ex.Message);
                return false;
            }
        }

        private static void RefreshCatalogForLoad(string workshopPath)
        {
            AddSearchDirectoriesForPath(workshopPath);
            RefreshDiskCatalog();
            InvalidateCatalog();
            EnsureCatalogBuilt(true);
        }

        private static object GetWorkshopUi()
        {
            System.Type uiType = FindGameType("UI_FreerideWorkshop");
            if ((object)uiType == null) return null;

            object ui = FindObjectOfTypeSafe(uiType);
            if (ui != null) return ui;

            System.Type stateType = FindGameType("State_FreerideWorkshop");
            if ((object)stateType == null) return null;

            object state = FindObjectOfTypeSafe(stateType);
            if (state == null) return null;

            FieldInfo[] fields = stateType.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                if (TypesMatch(fields[i].FieldType, uiType))
                {
                    object val = fields[i].GetValue(state);
                    if (val != null) return val;
                }
            }

            return null;
        }

        private static object ResolveModWithoutCtor(
            object modHint, string modName, string workshopPath, string scenePart)
        {
            if (modHint != null)
                return modHint;

            object listMod = FindModInGameDataListOnly(modName, workshopPath, null);
            if (listMod != null)
                return listMod;

            object mgrMod = FindModInManagerCatalog(modName, workshopPath, scenePart);
            if (mgrMod != null)
                return mgrMod;

            EnsureCatalogFromManagerOnly();

            if (!string.IsNullOrEmpty(scenePart))
            {
                object byScene = FindModForScenePartCached(scenePart);
                if (byScene != null)
                    return byScene;
            }
            if (!string.IsNullOrEmpty(modName))
            {
                object byName = FindModForDisplayNameCached(modName);
                if (byName != null)
                    return byName;
            }

            return null;
        }

        private static void EnsureCatalogFromManagerOnly()
        {
            if (_catalogBuilt || !Resolve()) return;

            System.Collections.Generic.HashSet<object> seen
                = new System.Collections.Generic.HashSet<object>();
            _catalogCache.Clear();

            void add(object m)
            {
                if (m == null || seen.Contains(m)) return;
                seen.Add(m);
                _catalogCache.Add(m);
            }

            object mgr = GetModManagerFromSingleton();
            if (mgr == null)
                mgr = GetModManagerFromGameData();
            if (mgr != null)
            {
                if ((object)_modsProp != null)
                    AddModsFromList(_modsProp.GetValue(mgr, null), add);
                if ((object)_modManagerModsFld != null)
                    AddModsFromList(_modManagerModsFld.GetValue(mgr), add);
            }

            AddModsFromList(TryGetGameDataModsList(), add);
            _catalogBuilt = true;
        }

        private static object FindModInManagerCatalog(
            string modName, string workshopPath, string scenePart)
        {
            if (!Resolve()) return null;

            object mgr = GetModManagerFromSingleton();
            if (mgr == null)
                mgr = GetModManagerFromGameData();
            if (mgr == null) return null;

            List<object> mods = new List<object>();
            if ((object)_modsProp != null)
                AddModsFromList(_modsProp.GetValue(mgr, null), mods.Add);
            if ((object)_modManagerModsFld != null)
                AddModsFromList(_modManagerModsFld.GetValue(mgr), mods.Add);

            for (int i = 0; i < mods.Count; i++)
            {
                object mod = mods[i];
                if (mod == null) continue;
                if (!string.IsNullOrEmpty(workshopPath)
                    && ModPathsMatch(workshopPath, ReadModPath(mod)))
                    return mod;
            }

            for (int i = 0; i < mods.Count; i++)
            {
                object mod = mods[i];
                if (mod == null) continue;
                if (!string.IsNullOrEmpty(scenePart) && ModMatchesScenePart(mod, scenePart))
                    return mod;
            }

            if (!string.IsNullOrEmpty(modName))
            {
                string want = NormalizeModName(modName);
                for (int i = 0; i < mods.Count; i++)
                {
                    object mod = mods[i];
                    if (mod == null) continue;
                    if (WorkshopNamesMatch(want, NormalizeModName(ReadModDisplayName(mod))))
                        return mod;
                }
            }

            return null;
        }

        private static object ResolveModDirect(
            object modHint, string modName, string workshopPath, string scenePart)
        {
            object resolved = ResolveModWithoutCtor(modHint, modName, workshopPath, scenePart);
            if (resolved != null)
                return resolved;

            if (!string.IsNullOrEmpty(workshopPath))
                return TryCreateModFromWorkshopPath(workshopPath);

            return null;
        }

        private static object ResolveModForLoad(
            object modHint, string modName, string workshopPath, string scenePart)
        {
            object found = FindModForLoad(modName, workshopPath, scenePart);
            if (found != null) return found;
            if (modHint != null) return modHint;
            if (!string.IsNullOrEmpty(workshopPath))
                return TryCreateModFromWorkshopPath(workshopPath);
            return null;
        }

        private static object FindModForLoad(string modName, string workshopPath, string scenePart = null)
        {
            if (!string.IsNullOrEmpty(workshopPath))
            {
                object byPath = FindModByPath(workshopPath);
                if (byPath != null) return byPath;
            }
            if (!string.IsNullOrEmpty(modName))
            {
                object byName = FindModForDisplayName(modName);
                if (byName != null) return byName;
            }
            if (!string.IsNullOrEmpty(scenePart))
            {
                object byScene = FindModForScenePart(scenePart);
                if (byScene != null) return byScene;
            }
            if (!string.IsNullOrEmpty(workshopPath))
            {
                object direct = TryCreateModFromWorkshopPath(workshopPath);
                if (direct != null) return direct;
            }
            return null;
        }

        private static IEnumerator WaitForModCatalog(
            string modName, string workshopPath, string scenePart = null)
        {
            AddSearchDirectoriesForPath(workshopPath);
            EnsureCatalogBuilt(false);
            if (FindModForLoad(modName, workshopPath, scenePart) != null)
                yield break;

            RefreshDiskCatalog();
            InvalidateCatalog();
            EnsureCatalogBuilt(true);
            TryInvokeWorkshopRefresh();

            if (FindModForLoad(modName, workshopPath, scenePart) != null)
                yield break;

            for (int i = 0; i < 90; i++)
            {
                if (FindModForLoad(modName, workshopPath, scenePart) != null)
                    yield break;

                if (i == 20)
                {
                    if (StatsManager.IsInMenuContext())
                        StateNavigator.PushGameState(
                            StateNavigator.State_FreerideWorkshop, "Workshop");
                    TryInvokeWorkshopRefresh();
                    InvalidateCatalog();
                    EnsureCatalogBuilt(true);
                }

                if (i == 50)
                {
                    RefreshDiskCatalog();
                    InvalidateCatalog();
                    EnsureCatalogBuilt(true);
                }

                yield return null;
            }

            int count = _catalogCache.Count;
            ModLog.Debug("[ModWorkshopLoader] Catalog miss for \"" + modName
                + "\" path=" + (workshopPath ?? "")
                + " scene=" + (scenePart ?? "")
                + " mods=" + count);
        }

        private static void InvalidateCatalog()
        {
            _catalogBuilt = false;
            _catalogCache.Clear();
            _cachedModManager = null;
        }

        public static void InvalidateCatalogPublic()
        {
            InvalidateCatalog();
        }

        private static void RefreshDiskCatalog()
        {
            try
            {
                EnsureModSearchPathsOnce();
                object mgr = GetModManager();
                if (mgr != null && (object)_refreshModsMethod != null)
                    _refreshModsMethod.Invoke(mgr, null);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[ModWorkshopLoader] RefreshSearchDirectories: " + ex.Message);
            }
        }

        private static void AddSearchDirectoriesForPath(string workshopPath)
        {
            // AddSearchDirectory triggers ModSearchDirectory.Refresh (full disk scan) — never on GO path.
        }

        private static void TryAddSearchDirectory(object mgr, string dir)
        {
            if (mgr == null || string.IsNullOrEmpty(dir)
                || (object)_addSearchDirMethod == null
                || !System.IO.Directory.Exists(dir))
                return;

            try
            {
                _addSearchDirMethod.Invoke(mgr, new object[] { dir });
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                string detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ModLog.Warn("[ModWorkshopLoader] AddSearchDirectory(" + dir + "): " + detail);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[ModWorkshopLoader] AddSearchDirectory(" + dir + "): " + ex.Message);
            }
        }

        private static string FindInstalledModsRoot(string pathHint)
        {
            if (string.IsNullOrEmpty(pathHint)) return null;
            string norm = pathHint.Replace('\\', '/');
            int idx = norm.IndexOf("_installedMods", System.StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            string root = pathHint.Substring(0, idx + "_installedMods".Length);
            return System.IO.Directory.Exists(root) ? root : null;
        }

        private static object TryCreateModFromWorkshopPath(string workshopPath)
        {
            if (string.IsNullOrEmpty(workshopPath) || !Resolve()) return null;

            string infoPath = GetWorkshopInfoPath(workshopPath);
            string folderPath = GetWorkshopFolderPath(workshopPath);

            if (!string.IsNullOrEmpty(infoPath))
            {
                object mod = TryCreateModInstance(infoPath);
                if (mod != null) return mod;
            }

            if (!string.IsNullOrEmpty(folderPath) && folderPath != infoPath)
            {
                object mod = TryCreateModInstance(folderPath);
                if (mod != null) return mod;
            }

            return null;
        }

        private static object TryCreateModInstance(string path)
        {
            if (string.IsNullOrEmpty(path) || !Resolve()) return null;
            try
            {
                return System.Activator.CreateInstance(_modType, new object[] { path });
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[ModWorkshopLoader] Mod ctor failed for " + path + ": " + ex.Message);
                return null;
            }
        }

        private static string GetWorkshopFolderPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            try
            {
                if (path.EndsWith(".info", System.StringComparison.OrdinalIgnoreCase))
                    return System.IO.Path.GetDirectoryName(path);

                if (System.IO.Directory.Exists(path))
                    return path;

                if (System.IO.File.Exists(path))
                    return System.IO.Path.GetDirectoryName(path);
            }
            catch { }

            return null;
        }

        private static string GetWorkshopInfoPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            try
            {
                if (path.EndsWith(".info", System.StringComparison.OrdinalIgnoreCase)
                    && System.IO.File.Exists(path))
                    return path;

                string folder = GetWorkshopFolderPath(path);
                if (string.IsNullOrEmpty(folder)) return null;

                string folderName = System.IO.Path.GetFileName(folder);
                if (!string.IsNullOrEmpty(folderName))
                {
                    string candidate = System.IO.Path.Combine(folder, folderName + ".info");
                    if (System.IO.File.Exists(candidate))
                        return candidate;
                }

                string[] infos = System.IO.Directory.GetFiles(folder, "*.info");
                if (infos != null && infos.Length > 0)
                    return infos[0];
            }
            catch { }

            return null;
        }

        private static bool TryStartViaSessionManager(object mod)
        {
            return TryStartModDirect(mod);
        }

        private static bool TryInvokeWorkshopRefresh()
        {
            try
            {
                System.Type uiType = FindGameType("UI_FreerideWorkshop");
                if ((object)uiType == null) return false;

                if ((object)_uiRefreshMethod == null
                    || !System.Object.ReferenceEquals(_uiRefreshMethod.DeclaringType, uiType))
                    _uiRefreshMethod = uiType.GetMethod(
                        "Refresh", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)_uiRefreshMethod == null) return false;

                object ui = GetWorkshopUi();
                if (ui == null) return false;

                _uiRefreshMethod.Invoke(ui, null);
                return true;
            }
            catch { return false; }
        }

        private static void EnsureModSearchPathsOnce()
        {
            if (_searchPathsDone) return;
            try
            {
                object mgr = GetModManager();
                if (mgr == null || (object)_addSearchDirMethod == null) return;

                string persistent = Application.persistentDataPath;
                if (!string.IsNullOrEmpty(persistent))
                    TryAddSearchDirectory(mgr, persistent);

                string localLow = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrEmpty(localLow))
                {
                    string rageLow = System.IO.Path.Combine(
                        System.IO.Path.Combine(
                            System.IO.Path.Combine(localLow, "Low"), "RageSquid"),
                        "Descenders");
                    AddModioInstallRoots(mgr, rageLow);
                }

                PropertyInfo dirProp = _modManagerType.GetProperty("defaultSearchDirectory");
                if ((object)dirProp != null)
                {
                    object dir = dirProp.GetValue(mgr, null);
                    if (dir != null && !string.IsNullOrEmpty(dir.ToString()))
                        TryAddSearchDirectory(mgr, dir.ToString());
                }
                _searchPathsDone = true;
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[ModWorkshopLoader] AddSearchDirectory: " + ex.Message);
            }
        }

        private static void AddModioInstallRoots(object mgr, string rageLow)
        {
            if (mgr == null || string.IsNullOrEmpty(rageLow)) return;
            TryAddSearchDirectory(mgr, rageLow);

            try
            {
                if (!System.IO.Directory.Exists(rageLow)) return;
                string[] modioDirs = System.IO.Directory.GetDirectories(rageLow, "modio-*");
                for (int i = 0; i < modioDirs.Length; i++)
                {
                    string installed = System.IO.Path.Combine(modioDirs[i], "_installedMods");
                    TryAddSearchDirectory(mgr, installed);
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[ModWorkshopLoader] modio roots: " + ex.Message);
            }
        }

        private static bool TryInvokeWorkshopUi(object mod)
        {
            if (mod == null) return false;

            try
            {
                System.Type uiType = FindGameType("UI_FreerideWorkshop");
                if ((object)uiType != null)
                {
                    if ((object)_uiStartMod == null
                        || !System.Object.ReferenceEquals(_uiStartMod.DeclaringType, uiType))
                    {
                        _uiStartMod = uiType.GetMethod(
                            "StartMod",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    }

                    if ((object)_uiStartMod != null)
                    {
                        object uiObj = GetWorkshopUi();
                if ((object)uiObj != null)
                {
                    PinModToGameData(mod);
                    _uiStartMod.Invoke(uiObj, new object[] { mod });
                    PinModToGameData(mod);
                    return true;
                }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[ModWorkshopLoader] StartMod: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "ModWorkshopLoader");
            }

            return false;
        }

        private static object FindModForBookmark(MapChanger.MapBookmark bm)
        {
            if (!bm.Valid || !Resolve()) return null;
            EnsureCatalogBuilt(false);

            if (!string.IsNullOrEmpty(bm.SceneName))
            {
                string safe = MapChanger.SanitizeStoragePartPublic(bm.SceneName);
                if (!string.IsNullOrEmpty(safe))
                {
                    object mod = FindModForScenePart(safe);
                    if (mod != null) return mod;
                }
                object modByScene = FindModForScenePart(bm.SceneName);
                if (modByScene != null) return modByScene;
            }

            if (!string.IsNullOrEmpty(bm.DisplayLabel))
            {
                object mod = FindModForDisplayName(bm.DisplayLabel);
                if (mod != null) return mod;
            }

            return null;
        }

        private static object FindModByPath(string workshopPath)
        {
            if (string.IsNullOrEmpty(workshopPath) || !Resolve()) return null;

            List<object> mods = new List<object>();
            CollectMods(mods);
            for (int i = 0; i < mods.Count; i++)
            {
                object mod = mods[i];
                if (mod == null) continue;
                string path = ReadModPath(mod);
                if (string.IsNullOrEmpty(path)) continue;
                if (ModPathsMatch(workshopPath, path))
                    return mod;
            }

            return TryCreateModFromWorkshopPath(workshopPath);
        }

        private static bool ModPathsMatch(string savedPath, string catalogPath)
        {
            if (string.IsNullOrEmpty(savedPath) || string.IsNullOrEmpty(catalogPath))
                return false;

            string want = NormalizeModPath(savedPath);
            string norm = NormalizeModPath(catalogPath);
            if (want == norm || norm.EndsWith(want, System.StringComparison.Ordinal)
                || want.EndsWith(norm, System.StringComparison.Ordinal))
                return true;

            string wantFolder = NormalizeModPath(GetWorkshopFolderPath(savedPath));
            string catFolder = NormalizeModPath(GetWorkshopFolderPath(catalogPath));
            if (!string.IsNullOrEmpty(wantFolder) && !string.IsNullOrEmpty(catFolder)
                && wantFolder == catFolder)
                return true;

            string wantInfo = NormalizeModPath(GetWorkshopInfoPath(savedPath));
            string catInfo = NormalizeModPath(GetWorkshopInfoPath(catalogPath));
            return !string.IsNullOrEmpty(wantInfo) && !string.IsNullOrEmpty(catInfo)
                && wantInfo == catInfo;
        }

        private static string ReadModPath(object mod)
        {
            if (mod == null || !Resolve()) return "";
            try
            {
                PropertyInfo infoProp = _modType.GetProperty("modInfo");
                if ((object)infoProp == null) return "";
                object info = infoProp.GetValue(mod, null);
                if (info == null) return "";
                PropertyInfo pathProp = info.GetType().GetProperty("path");
                if ((object)pathProp == null) return "";
                object path = pathProp.GetValue(info, null);
                return path != null ? path.ToString() : "";
            }
            catch { return ""; }
        }

        private static string NormalizeModPath(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            return raw.Replace('\\', '/').Trim().ToLowerInvariant();
        }

        private static bool EnsureModsAccess()
        {
            return Resolve();
        }

        private static void EnsureCatalogBuilt(bool includeUiList)
        {
            if (_catalogBuilt) return;
            if (!Resolve()) return;

            System.Collections.Generic.HashSet<object> seen = new System.Collections.Generic.HashSet<object>();
            _catalogCache.Clear();

            void add(object mod)
            {
                if (mod == null || seen.Contains(mod)) return;
                seen.Add(mod);
                _catalogCache.Add(mod);
            }

            object mgr = GetModManager();
            if (mgr != null)
            {
                if ((object)_modsProp != null)
                    AddModsFromList(_modsProp.GetValue(mgr, null), add);
                if ((object)_modManagerModsFld != null)
                    AddModsFromList(_modManagerModsFld.GetValue(mgr), add);
            }

            AddModsFromList(TryGetGameDataModsList(), add);

            object active = TryGetActiveModFromGameData();
            if (active != null)
                add(active);

            if (includeUiList)
                CollectModsFromWorkshopUi(add);

            _catalogBuilt = true;
        }

        private static void CollectMods(List<object> sink)
        {
            if (sink == null) return;
            EnsureCatalogBuilt(false);
            for (int i = 0; i < _catalogCache.Count; i++)
                sink.Add(_catalogCache[i]);
        }

        private static object FindModForScenePartCached(string scenePart)
        {
            for (int i = 0; i < _catalogCache.Count; i++)
            {
                object mod = _catalogCache[i];
                if (mod != null && ModMatchesScenePart(mod, scenePart))
                    return mod;
            }
            return null;
        }

        private static object FindModForDisplayNameCached(string displayLabel)
        {
            if (string.IsNullOrEmpty(displayLabel)) return null;
            string want = NormalizeModName(displayLabel);
            if (string.IsNullOrEmpty(want)) return null;
            for (int i = 0; i < _catalogCache.Count; i++)
            {
                object mod = _catalogCache[i];
                if (mod == null) continue;
                string name = ReadModDisplayName(mod);
                if (string.IsNullOrEmpty(name)) continue;
                if (WorkshopNamesMatch(want, NormalizeModName(name)))
                    return mod;
            }
            return null;
        }

        private static void CollectModsFromWorkshopUi(System.Action<object> add)
        {
            try
            {
                System.Type uiType = FindGameType("UI_FreerideWorkshop");
                if ((object)uiType == null) return;

                object ui = FindObjectOfTypeSafe(uiType);
                if (ui == null) return;

                if ((object)_workshopModListFld == null)
                {
                    FieldInfo[] fields = uiType.GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    System.Type listType = typeof(List<>).MakeGenericType(_modType);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (TypesMatch(fields[i].FieldType, listType))
                        {
                            _workshopModListFld = fields[i];
                            break;
                        }
                    }
                }

                if ((object)_workshopModListFld != null)
                    AddModsFromList(_workshopModListFld.GetValue(ui), add);
            }
            catch { }
        }

        private static void AddModsFromList(object listObj, System.Action<object> add)
        {
            if (listObj is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                    add(list[i]);
                return;
            }

            if (listObj is System.Collections.IEnumerable en)
            {
                foreach (object item in en)
                    add(item);
            }
        }

        private static object TryGetGameDataModsList()
        {
            try
            {
                System.Type gdType = FindGameType("GameData");
                if ((object)gdType == null) return null;
                object gd = GetGameDataSingleton(gdType);
                if (gd == null) return null;

                if ((object)_gameDataModsListProp == null && (object)_gameDataModsListFld == null)
                {
                    System.Type listType = typeof(List<>).MakeGenericType(_modType);
                    PropertyInfo[] props = gdType.GetProperties(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (TypesMatch(props[i].PropertyType, listType))
                        {
                            _gameDataModsListProp = props[i];
                            break;
                        }
                    }
                    if ((object)_gameDataModsListProp == null)
                    {
                        FieldInfo[] fields = gdType.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        for (int i = 0; i < fields.Length; i++)
                        {
                            if (TypesMatch(fields[i].FieldType, listType))
                            {
                                _gameDataModsListFld = fields[i];
                                break;
                            }
                        }
                    }
                }

                if ((object)_gameDataModsListProp != null)
                    return _gameDataModsListProp.GetValue(gd, null);
                if ((object)_gameDataModsListFld != null)
                    return _gameDataModsListFld.GetValue(gd);
            }
            catch { }
            return null;
        }

        private static IEnumerator EnsureModLoadedRoutine(object mod)
        {
            if (mod == null) yield break;
            IEnumerator load = ForceModLoadRoutine(mod);
            while (load != null && load.MoveNext())
                yield return load.Current;
        }

        private static bool ShouldLoadModAssets(object mod)
        {
            try
            {
                if ((object)_modCanLoadProp != null)
                {
                    object can = _modCanLoadProp.GetValue(mod, null);
                    if (can is bool && !(bool)can)
                        return false;
                }

                if ((object)_modLoadStateProp != null)
                {
                    object state = _modLoadStateProp.GetValue(mod, null);
                    if (state != null)
                    {
                        string stateName = state.ToString();
                        if (stateName.IndexOf("Loaded", System.StringComparison.OrdinalIgnoreCase) >= 0
                            && stateName.IndexOf("Unloaded", System.StringComparison.OrdinalIgnoreCase) < 0)
                            return false;
                    }
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[ModWorkshopLoader] Mod load state: " + ex.Message);
            }
            return true;
        }

        private static object FindObjectOfTypeSafe(System.Type targetType)
        {
            if ((object)targetType == null) return null;
            try
            {
                MethodInfo findMethod = typeof(UnityEngine.Object).GetMethod(
                    "FindObjectOfType",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new System.Type[] { typeof(System.Type) },
                    null);
                if ((object)findMethod != null)
                    return findMethod.Invoke(null, new object[] { targetType });
            }
            catch { }
            return null;
        }

        private static object FindModForDisplayName(string displayLabel)
        {
            if (string.IsNullOrEmpty(displayLabel) || !Resolve()) return null;
            EnsureCatalogBuilt(false);
            return FindModForDisplayNameCached(displayLabel);
        }

        private static bool ModNamesMatch(string want, string candidate)
        {
            return WorkshopNamesMatch(want, candidate);
        }

        private static bool WorkshopNamesMatch(string want, string candidate)
        {
            if (string.IsNullOrEmpty(want) || string.IsNullOrEmpty(candidate))
                return false;
            if (want == candidate) return true;
            if (want.Length >= 4 && candidate.Contains(want)) return true;
            if (candidate.Length >= 4 && want.Contains(candidate)) return true;
            if (want.Length >= 4 && candidate.StartsWith(want, System.StringComparison.Ordinal)) return true;
            if (candidate.Length >= 4 && want.StartsWith(candidate, System.StringComparison.Ordinal)) return true;
            return false;
        }

        private static string NormalizeModName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        private static bool ResolveSessionModStart()
        {
            if (_sessionModStartResolved)
                return (object)_startNewSessionModMethod != null;

            _sessionModStartResolved = true;
            if (!Resolve()) return false;

            try
            {
                System.Type gmType = FindGameType("GameModifier");
                if ((object)gmType == null) return false;

                _gameModifierListType = typeof(List<>).MakeGenericType(gmType);
                MethodInfo[] methods = typeof(SessionManager).GetMethods(
                    BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo m = methods[i];
                    if (m.Name != "StartNewSession") continue;
                    ParameterInfo[] p = m.GetParameters();
                    if (p.Length != 2) continue;
                    if (!TypesMatch(p[0].ParameterType, _modType)) continue;
                    if (!TypesMatch(p[1].ParameterType, _gameModifierListType)) continue;
                    _startNewSessionModMethod = m;
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[ModWorkshopLoader] ResolveSessionModStart: " + ex.Message);
            }

            return false;
        }

        private static object FindModForScenePart(string scenePart)
        {
            if (!Resolve()) return null;
            EnsureCatalogBuilt(false);
            return FindModForScenePartCached(scenePart);
        }

        private static bool ModMatchesScenePart(object mod, string scenePart)
        {
            string want = NormalizeScenePart(scenePart);
            if (string.IsNullOrEmpty(want)) return false;

            if ((object)_sceneNamesProp == null) return false;

            object namesObj = _sceneNamesProp.GetValue(mod, null);
            if (namesObj is IList names)
            {
                for (int i = 0; i < names.Count; i++)
                {
                    object n = names[i];
                    if (n == null) continue;
                    if (ScenePartsMatch(want, NormalizeScenePart(n.ToString())))
                        return true;
                }
            }

            object scenesObj = (object)_scenesProp != null ? _scenesProp.GetValue(mod, null) : null;
            if (scenesObj is IList scenes && (object)_modSceneNameProp != null)
            {
                for (int i = 0; i < scenes.Count; i++)
                {
                    object scene = scenes[i];
                    if (scene == null) continue;
                    object nameObj = _modSceneNameProp.GetValue(scene, null);
                    if (nameObj == null) continue;
                    if (ScenePartsMatch(want, NormalizeScenePart(nameObj.ToString())))
                        return true;
                }
            }

            return false;
        }

        private static string ReadModDisplayName(object mod)
        {
            try
            {
                PropertyInfo nameProp = _modType.GetProperty("name");
                if ((object)nameProp != null)
                {
                    object n = nameProp.GetValue(mod, null);
                    if (n != null && !string.IsNullOrEmpty(n.ToString()))
                        return n.ToString();
                }

                PropertyInfo infoProp = _modType.GetProperty("modInfo");
                if ((object)infoProp != null)
                {
                    object info = infoProp.GetValue(mod, null);
                    if (info != null)
                    {
                        PropertyInfo infoNameProp = info.GetType().GetProperty("name");
                        if ((object)infoNameProp != null)
                        {
                            object n = infoNameProp.GetValue(info, null);
                            if (n != null && !string.IsNullOrEmpty(n.ToString()))
                                return n.ToString();
                        }

                        FieldInfo infoNameFld = info.GetType().GetField("name");
                        if ((object)infoNameFld != null)
                        {
                            object n = infoNameFld.GetValue(info);
                            if (n != null && !string.IsNullOrEmpty(n.ToString()))
                                return n.ToString();
                        }
                    }
                }
            }
            catch { }

            return "Workshop map";
        }

        private static object GetSessionManagerSingleton()
        {
            try
            {
                System.Type smType = typeof(SessionManager);
                FieldInfo directField = smType.GetField(
                    "[~qsVD|",
                    BindingFlags.Public | BindingFlags.Static);
                if ((object)directField != null)
                {
                    object val = directField.GetValue(null);
                    if ((object)val != null) return val;
                }

                System.Type singletonOpen = typeof(ModWorkshopLoader).Assembly.GetType("Singleton`1");
                if ((object)singletonOpen == null)
                {
                    foreach (Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        singletonOpen = asm.GetType("Singleton`1");
                        if ((object)singletonOpen != null) break;
                    }
                }
                if ((object)singletonOpen == null) return null;

                System.Type closed = singletonOpen.MakeGenericType(smType);
                PropertyInfo[] props = closed.GetProperties(
                    BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < props.Length; i++)
                {
                    PropertyInfo p = props[i];
                    if (TypesMatch(p.PropertyType, smType))
                        return p.GetValue(null, null);
                }
                FieldInfo[] fields = closed.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo f = fields[i];
                    if (TypesMatch(f.FieldType, smType))
                        return f.GetValue(null);
                }
            }
            catch { }
            return null;
        }

        private static object GetModManager()
        {
            if ((object)_modManagerType == null) return null;
            if (_cachedModManager != null) return _cachedModManager;

            object mgr = GetModManagerFromGameData();
            if (mgr != null)
            {
                _cachedModManager = mgr;
                return mgr;
            }

            mgr = GetModManagerFromSingleton();
            if (mgr != null)
            {
                _cachedModManager = mgr;
                return mgr;
            }

            if (MapChanger.InWorkshopLevel()) return null;

            mgr = FindObjectOfTypeSafe(_modManagerType);
            if (mgr != null)
                _cachedModManager = mgr;
            return mgr;
        }

        private static object GetModManagerFromGameData()
        {
            try
            {
                System.Type gdType = FindGameType("GameData");
                if ((object)gdType == null) return null;
                object gd = GetGameDataSingleton(gdType);
                if (gd == null) return null;

                if ((object)_gameDataModManagerProp == null)
                {
                    PropertyInfo[] props = gdType.GetProperties(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (TypesMatch(props[i].PropertyType, _modManagerType))
                        {
                            _gameDataModManagerProp = props[i];
                            break;
                        }
                    }
                }

                if ((object)_gameDataModManagerProp != null)
                    return _gameDataModManagerProp.GetValue(gd, null);
            }
            catch { }
            return null;
        }

        private static object TryGetActiveModFromGameData()
        {
            try
            {
                System.Type gdType = FindGameType("GameData");
                if ((object)gdType == null) return null;
                object gd = GetGameDataSingleton(gdType);
                if (gd == null) return null;

                if ((object)_gameDataActiveModProp == null && (object)_gameDataActiveModFld == null)
                {
                    PropertyInfo[] props = gdType.GetProperties(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < props.Length; i++)
                    {
                        PropertyInfo p = props[i];
                        if (TypesMatch(p.PropertyType, _modType))
                        {
                            _gameDataActiveModProp = p;
                            break;
                        }
                    }
                    if ((object)_gameDataActiveModProp == null)
                    {
                        FieldInfo[] fields = gdType.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        for (int i = 0; i < fields.Length; i++)
                        {
                            FieldInfo f = fields[i];
                            if (TypesMatch(f.FieldType, _modType))
                            {
                                _gameDataActiveModFld = f;
                                break;
                            }
                        }
                    }
                }

                if ((object)_gameDataActiveModProp != null)
                    return _gameDataActiveModProp.GetValue(gd, null);
                if ((object)_gameDataActiveModFld != null)
                    return _gameDataActiveModFld.GetValue(gd);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[ModWorkshopLoader] Active mod from GameData: " + ex.Message);
            }
            return null;
        }

        private static object GetGameDataSingleton(System.Type gdType)
        {
            try
            {
                FieldInfo directField = gdType.GetField(
                    "[~qsVD|",
                    BindingFlags.Public | BindingFlags.Static);
                if ((object)directField != null)
                {
                    object val = directField.GetValue(null);
                    if ((object)val != null) return val;
                }

                System.Type singletonOpen = typeof(ModWorkshopLoader).Assembly.GetType("Singleton`1");
                if ((object)singletonOpen == null)
                {
                    foreach (Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        singletonOpen = asm.GetType("Singleton`1");
                        if ((object)singletonOpen != null) break;
                    }
                }
                if ((object)singletonOpen == null) return null;

                System.Type closed = singletonOpen.MakeGenericType(gdType);
                PropertyInfo[] props = closed.GetProperties(
                    BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < props.Length; i++)
                {
                    PropertyInfo p = props[i];
                    if (TypesMatch(p.PropertyType, gdType))
                        return p.GetValue(null, null);
                }
                FieldInfo[] fields = closed.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo f = fields[i];
                    if (TypesMatch(f.FieldType, gdType))
                        return f.GetValue(null);
                }
            }
            catch { }
            return null;
        }

        private static object GetModManagerFromSingleton()
        {
            try
            {
                System.Type singletonOpen = typeof(ModWorkshopLoader).Assembly.GetType("ModTool.UnitySingleton`1");
                if ((object)singletonOpen == null)
                {
                    foreach (Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        singletonOpen = asm.GetType("ModTool.UnitySingleton`1");
                        if ((object)singletonOpen != null) break;
                    }
                }

                if ((object)singletonOpen == null) return null;

                System.Type closed = singletonOpen.MakeGenericType(_modManagerType);
                if ((object)_modManagerInstanceProp == null)
                    _modManagerInstanceProp = closed.GetProperty(
                        "instance", BindingFlags.Public | BindingFlags.Static);

                if ((object)_modManagerInstanceProp == null) return null;
                return _modManagerInstanceProp.GetValue(null, null);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[ModWorkshopLoader] GetModManager: " + ex.Message);
                return null;
            }
        }

        private static bool TypesMatch(System.Type a, System.Type b)
        {
            if ((object)a == null || (object)b == null) return false;
            if (System.Object.ReferenceEquals(a, b)) return true;
            return string.Equals(a.FullName, b.FullName, System.StringComparison.Ordinal);
        }

        private static bool ScenePartsMatch(string want, string candidate)
        {
            if (string.IsNullOrEmpty(want) || string.IsNullOrEmpty(candidate))
                return false;
            if (string.Equals(want, candidate, System.StringComparison.OrdinalIgnoreCase))
                return true;

            string wantLabel = MapChanger.SceneToDisplayLabel(want).Replace(" ", "");
            string candLabel = MapChanger.SceneToDisplayLabel(candidate).Replace(" ", "");
            return string.Equals(
                NormalizeScenePart(wantLabel),
                NormalizeScenePart(candLabel),
                System.StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeScenePart(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string safe = MapChanger.SanitizeStoragePartPublic(raw);
            if (!string.IsNullOrEmpty(safe)) return safe;

            System.Text.StringBuilder sb = new System.Text.StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        private static bool Resolve()
        {
            if ((object)_modManagerType != null && (object)_modsProp != null)
                return true;

            try
            {
                Assembly modToolAsm = null;
                foreach (Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "ModTool")
                    {
                        modToolAsm = asm;
                        break;
                    }
                }

                if ((object)modToolAsm == null) return false;

                _modManagerType = modToolAsm.GetType("ModTool.ModManager");
                _modType = modToolAsm.GetType("ModTool.Mod");
                _modSceneType = modToolAsm.GetType("ModTool.ModScene");
                if ((object)_modManagerType == null || (object)_modType == null)
                    return false;

                _modsProp = _modManagerType.GetProperty("mods");
                _modManagerModsFld = _modManagerType.GetField(
                    "_mods", BindingFlags.NonPublic | BindingFlags.Instance);
                _refreshModsMethod = _modManagerType.GetMethod("RefreshSearchDirectories");
                _addSearchDirMethod = _modManagerType.GetMethod("AddSearchDirectory");
                _sceneNamesProp = _modType.GetProperty("sceneNames");
                _scenesProp = _modType.GetProperty("scenes");
                _modLoadMethod = _modType.GetMethod("Load");
                _modLoadAsyncMethod = _modType.GetMethod("LoadAsync");
                _modLoadStateProp = _modType.GetProperty("loadState");
                _modCanLoadProp = _modType.GetProperty("canLoad");
                _modLoadProgressProp = _modType.GetProperty("loadProgress");
                _modUnloadMethod = _modType.GetMethod("Unload");
                if ((object)_modSceneType != null)
                    _modSceneNameProp = _modSceneType.GetProperty("name");

                return (object)_modsProp != null && (object)_sceneNamesProp != null;
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[ModWorkshopLoader] Resolve: " + ex.Message);
                return false;
            }
        }

        private static System.Type FindGameType(string name)
        {
            foreach (Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                System.Type t = null;
                try { t = asm.GetType(name); }
                catch { }
                if ((object)t != null) return t;
            }
            return null;
        }
    }
}
