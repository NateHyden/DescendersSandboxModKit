using DescendersModMenu;
using DescendersModMenu.BikeStats;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Collections.Generic;
using DescendersModMenu.UI;

namespace DescendersModMenu.Mods
{
    public static class MapChanger
    {
        // ── Map entry ─────────────────────────────────────────────────
        public struct MapEntry
        {
            public string Name;
            public int CustomSeed;
            public int WorldInt;
            public bool IsBikePark;
        }

        private static readonly List<MapEntry> _maps = new List<MapEntry>();
        public static int Count => _maps.Count;
        public static string GetName(int i) => _maps[i].Name;
        public static MapEntry GetEntry(int i) => _maps[i];
        public static bool HasBikeParks { get; private set; } = false;

        private static readonly string[] _baseNames =
            { "Highlands","Forest","Canyon","Peaks","Hell","Desert","Jungle","Favela","Glaciers","Ridges" };
        private static readonly int[] _baseWorlds =
            { 1, 2, 3, 4, 5, 6, 7, 8, 9, 11 };

        // ── Reflection cache ──────────────────────────────────────────
        private static System.Type _wiWlGzType = null;
        private static MethodInfo _fmDOWdg = null;
        private static System.Type _rDRSType = null;
        private static object _sandboxValue = null;
        private static MethodInfo _closeCurrentSessionMethod = null;
        private static MethodInfo _writeCurrentLevelSeedMethod = null;
        private static FieldInfo _sessionTypeFld = null;
        private static MethodInfo _startNewSession = null;
        private static MethodInfo _startNewSessionString = null;
        private static MethodInfo _loadLevelFromSeedMethod = null;
        private static MethodInfo _sessionSeedMethod = null;
        private static MethodInfo _setSeedMethod = null;
        private static MethodInfo _getCurrentLevelFullSeedMethod = null;
        private static MethodInfo _getSessionSeedIntMethod = null;
        private static MethodInfo _parseSeedMethod = null;
        private static FieldInfo _sessionDataFld = null;
        private static FieldInfo _sessionDataSessionTypeFld = null;
        private static FieldInfo _currentLevelFld = null;
        private static MethodInfo _pushState = null;
        private static object _vtGenerating = null;
        private static object _vtSandbox = null;
        private static object _vtInGame = null;
        private static MethodInfo _sandboxInfHealthMethod = null;
        private static MethodInfo _isLastStandMethod = null;
        private static MethodInfo _getAllPlayersImpactMethod = null;
        private static PropertyInfo _currentStateProp = null;
        private static MethodInfo _currentStateGetter = null;

        // ── Build map list ────────────────────────────────────────────
        public static void BuildMapList()
        {
            _maps.Clear();
            HasBikeParks = false;

            for (int i = 0; i < _baseNames.Length; i++)
                _maps.Add(new MapEntry
                {
                    Name = _baseNames[i],
                    CustomSeed = -1,
                    WorldInt = _baseWorlds[i],
                    IsBikePark = false
                });

            try
            {
                var seenSeeds = new HashSet<int>();
                int found = 0;

                object gd = GetSingleton(typeof(GameData));
                if ((object)gd != null)
                {
                    // Prefer every BonusLevelInfo[] on GameData (FqVmLOT + any twin arrays).
                    FieldInfo[] fields = gd.GetType().GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int fi = 0; fi < fields.Length; fi++)
                    {
                        FieldInfo f = fields[fi];
                        if (!f.FieldType.IsArray) continue;
                        if (!string.Equals(f.FieldType.GetElementType().Name, "BonusLevelInfo",
                            System.StringComparison.Ordinal)) continue;

                        System.Array arr = f.GetValue(gd) as System.Array;
                        if ((object)arr == null) continue;
                        found += AddBonusLevels(arr, seenSeeds);
                    }

                    // BikeParkCategory[].bikeParkList — same data the Freeride UI uses.
                    for (int fi = 0; fi < fields.Length; fi++)
                    {
                        FieldInfo f = fields[fi];
                        if (!f.FieldType.IsArray) continue;
                        if (!string.Equals(f.FieldType.GetElementType().Name, "BikeParkCategory",
                            System.StringComparison.Ordinal)) continue;

                        System.Array cats = f.GetValue(gd) as System.Array;
                        if ((object)cats == null) continue;
                        for (int c = 0; c < cats.Length; c++)
                        {
                            object cat = cats.GetValue(c);
                            if ((object)cat == null) continue;
                            object listObj = GetPublicField<object>(cat, "bikeParkList");
                            var list = listObj as System.Collections.IList;
                            if ((object)list == null) continue;
                            for (int j = 0; j < list.Count; j++)
                            {
                                if (TryAddBonusLevel(list[j], seenSeeds))
                                    found++;
                            }
                        }
                    }
                }

                // Anything already loaded as a ScriptableObject (covers late-loaded parks).
                BonusLevelInfo[] all = Resources.FindObjectsOfTypeAll<BonusLevelInfo>();
                if ((object)all != null)
                    found += AddBonusLevels(all, seenSeeds);

                HasBikeParks = found > 0;
                ModLog.Debug("[MapChanger] " + found + " bike parks + 10 base worlds = "
                    + _maps.Count + " total.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[MapChanger] BuildMapList: " + ex.Message); Telemetry.ReportErrorAsync(ex, "MapChanger"); }
        }

        private static int AddBonusLevels(System.Array arr, HashSet<int> seenSeeds)
        {
            int found = 0;
            if ((object)arr == null) return 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (TryAddBonusLevel(arr.GetValue(i), seenSeeds))
                    found++;
            }
            return found;
        }

        private static bool TryAddBonusLevel(object b, HashSet<int> seenSeeds)
        {
            if ((object)b == null) return false;
            string name = GetPublicField<string>(b, "levelName");
            int seed = GetPublicField<int>(b, "customSeed");
            object we = GetPublicField<object>(b, "world");
            int world = we != null ? (int)we : 0;
            if (string.IsNullOrEmpty(name) || seed == 0) return false;
            if (seenSeeds != null && !seenSeeds.Add(seed)) return false;

            _maps.Add(new MapEntry
            {
                Name = PrettyName(name),
                CustomSeed = seed,
                WorldInt = world,
                IsBikePark = true
            });
            return true;
        }

        private static string PrettyName(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            switch (key)
            {
                case "BIKEOUT": return "Bike Out";
                case "BIKEOUTV2": return "Bike Out 2";
                case "BIKEOUTV3": return "Bike Out 3";
                case "BIKEOUTV4": return "Bike Out 4";
                case "BIKEOUTV5": return "Bike Out 5";
                case "MTPALUMBO": return "Mt. Palumbo";
                case "MTROSIE": return "Mt. Rosie";
                case "CONSTRUCTIONSITE": return "Construction Site";
                case "KIDSROOMIMAGINATE": return "Kids Room";
                case "MEGARAMP": return "Mega Ramp";
                case "STMP": return "STMP Line";
                case "STOKER": return "Stoker Bike Park";
                case "VUURBERG": return "Vuurberg";
                case "CAMBRIA": return "Cambria";
                case "DYFI": return "Dyfi Valley";
                case "ALODALAKES": return "Aloda Lakes";
                case "ISLANDCAKEWALK": return "Island Cakewalk";
                case "LOSTCAUSECAVES": return "Lost Cause Caves";
                case "BCBIKEPARK": return "BC Bike Park";
                case "REDRAVENCANYON": return "Red Raven Canyon";
                case "BIGAIRCOMPOUND": return "Big Air Compound";
                case "VISIONLINE": return "Vision Line";
                case "SNOWSHOE": return "Snowshoe";
                case "JUMPCITY": return "Jump City";
                case "ROSERIDGE": return "Rose Ridge";
                case "MEGAPARK": return "Mega Park";
                case "DRYLANDS": return "Dry Lands";
                case "SANCTUARY": return "Sanctuary";
                case "KUSHMUCK": return "Kushmuck";
                case "GRASSHOPPER": return "Grasshopper";
                case "SNOWMAN": return "Snowman";
                case "BOGOTA": return "Bogota";
                case "ISLAND": return "Island";
                case "POLDER": return "Polder";
                case "RANCH": return "Ranch";
                case "MOON": return "Moon";
                case "SABA": return "Saba";
                case "SLOPE": return "Slope";
                case "UTAH": return "Utah";
                case "IDO": return "IDO Bike Park";
                case "RIOT": return "Ragesquid Riot";
                case "RAGESQUIDRIOT": return "Ragesquid Riot";
            }
            return System.Globalization.CultureInfo.CurrentCulture
                .TextInfo.ToTitleCase(key.ToLowerInvariant());
        }

        private static bool IsBaseWorldDisplayName(string label)
        {
            if (string.IsNullOrEmpty(label)) return false;
            string norm = NormalizeMapNameForMatch(label);
            for (int i = 0; i < _baseNames.Length; i++)
            {
                if (NormalizeMapNameForMatch(_baseNames[i]) == norm)
                    return true;
            }
            return false;
        }

        public static bool IsBaseWorldNamePublic(string label) => IsBaseWorldDisplayName(label);

        /// <summary>
        /// Bike parks reuse career biomes (worldInt=Highlands etc). Prefer scene/level/seed
        /// names so "Ragesquid Riot" is never stored as "Highlands".
        /// </summary>
        public static string ResolveRideDisplayLabel(string sceneName, string levelName, int customSeed)
        {
            if (customSeed != 0)
            {
                if (_maps.Count == 0)
                    BuildMapList();
                string fromSeed = FindMapNameForSeed(customSeed);
                if (!string.IsNullOrEmpty(fromSeed))
                    return fromSeed;
            }

            if (!string.IsNullOrEmpty(levelName))
            {
                string fromLevel = PrettyName(levelName);
                if (!string.IsNullOrEmpty(fromLevel) && !IsBaseWorldDisplayName(fromLevel))
                    return fromLevel;
            }

            string fromScene = ResolveLabelFromScene(sceneName);
            if (!string.IsNullOrEmpty(fromScene))
                return fromScene;

            if (!string.IsNullOrEmpty(levelName))
                return PrettyName(levelName);

            return null;
        }

        private static string ResolveLabelFromScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;

            string prettyScene = PrettySceneLabel(sceneName);
            string titled = System.Globalization.CultureInfo.CurrentCulture
                .TextInfo.ToTitleCase(prettyScene.ToLowerInvariant());

            if (_maps.Count == 0)
                BuildMapList();

            int idx;
            if (TryFindMapIndexByName(titled, out idx) || TryFindMapIndexByName(prettyScene, out idx))
                return _maps[idx].Name;

            // Compact letters/digits → PrettyName (ragesquid_riot → RAGESQUIDRIOT / RIOT)
            System.Text.StringBuilder compact = new System.Text.StringBuilder();
            for (int i = 0; i < sceneName.Length; i++)
            {
                char c = sceneName[i];
                if (char.IsLetterOrDigit(c))
                    compact.Append(char.ToUpperInvariant(c));
            }
            string compactKey = compact.ToString();
            if (!string.IsNullOrEmpty(compactKey))
            {
                string mapped = PrettyName(compactKey);
                if (!string.IsNullOrEmpty(mapped)
                    && !string.Equals(mapped, titled, System.StringComparison.OrdinalIgnoreCase)
                    && NormalizeMapNameForMatch(mapped) != NormalizeMapNameForMatch(compactKey))
                    return mapped;
                if (TryFindMapIndexByName(mapped, out idx))
                    return _maps[idx].Name;
            }

            string[] parts = sceneName.Split(new char[] { '_', '-', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                string last = parts[parts.Length - 1].ToUpperInvariant();
                string mappedLast = PrettyName(last);
                if (!string.IsNullOrEmpty(mappedLast)
                    && NormalizeMapNameForMatch(mappedLast) != NormalizeMapNameForMatch(last)
                    && !IsBaseWorldDisplayName(mappedLast))
                    return mappedLast;
                if (TryFindMapIndexByName(mappedLast, out idx))
                    return _maps[idx].Name;
            }

            if (!string.IsNullOrEmpty(titled) && !IsBaseWorldDisplayName(titled))
                return titled;
            return null;
        }

        // ── Deferred load ─────────────────────────────────────────────
        private static int _pendingLoad = -1;
        private static float _loadTimer = 0f;
        private static int _scoreToRestore = 0;
        private static float _suppressTimer = 0f;
        public static string LastLoadedSeed { get; private set; } = "";
        private static float _restoreTimer = 0f;

        public static void GoToMap(int index)
        {
            if (index < 0 || index >= _maps.Count) return;
            ModLog.Debug("[MapChanger] Queuing: " + _maps[index].Name);
            _pendingLoad = index;
            _loadTimer = 0.1f;
        }

        public static void Tick()
        {
            if (_pendingLoad >= 0)
            {
                _loadTimer -= Time.deltaTime;
                if (_loadTimer <= 0f)
                {
                    int idx = _pendingLoad;
                    _pendingLoad = -1;
                    ExecuteLoad(idx);
                }
            }
            if (_restoreTimer > 0f)
            {
                _restoreTimer -= Time.deltaTime;
                if (_restoreTimer <= 0f && _scoreToRestore > 0)
                {
                    try
                    {
                        DevCommandsGameplay.AddScore(_scoreToRestore);
                        ModLog.Debug("[MapChanger] Restored " + _scoreToRestore + " REP.");
                    }
                    catch (System.Exception ex) { MelonLogger.Error("[MapChanger] RestoreScore: " + ex.Message); Telemetry.ReportErrorAsync(ex, "MapChanger"); }
                    _scoreToRestore = 0;
                }
            }
            if (_suppressTimer > 0f)
            {
                _suppressTimer -= Time.deltaTime;
                SuppressInactivityWarning();
            }
        }

        private static void ExecuteLoad(int index)
        {
            try
            {
                MapEntry map = _maps[index];
                int saved = ReadCurrentScore();
                if (saved > 0) { _scoreToRestore = saved; _restoreTimer = 2.5f; }

                if (!map.IsBikePark)
                {
                    SuppressInactivityWarning();
                    string worldStr = map.WorldInt.ToString();
                    ModLog.Debug("[MapChanger] Base world load attempt: worldStr=" + worldStr);
                    DevCommandsGameplay.LoadLevel(worldStr);
                    ModLog.Debug("[MapChanger] Base world: LoadLevel(" + worldStr + ") called");
                    return;
                }

                // ── Bike park load ────────────────────────────────────

                ModLog.Debug("[MapChanger] Bike park: " + map.Name
                    + " seed=" + map.CustomSeed + " world=" + map.WorldInt);

                if (!ResolveReflection()) return;

                object smInstance = GetSingleton(typeof(SessionManager));
                object stInstance = GetSingleton(typeof(StateMachine));
                if ((object)smInstance == null || (object)stInstance == null)
                {
                    MelonLogger.Error("[MapChanger] SessionManager or StateMachine null.");
                    Telemetry.ReportErrorAsync(new System.Exception("[MapChanger] SessionManager or StateMachine null."), "MapChanger");
                    return;
                }

                SuppressInactivityWarning();

                object modifiers = null;
                if (CrewPerkManager.IsArmedForNextTravel)
                {
                    try
                    {
                        object gameData = GetSingleton(typeof(GameData));
                        if ((object)gameData != null)
                        {
                            MethodInfo getMods = typeof(GameData).GetMethod(
                                "GetModifiersFromSeed", BindingFlags.Public | BindingFlags.Instance);
                            if ((object)getMods != null)
                            {
                                modifiers = getMods.Invoke(gameData,
                                    new object[] { (long)map.CustomSeed, 1 });
                                modifiers = CrewPerkManager.MergeIntoSessionModifiers(modifiers);
                            }
                        }
                    }
                    catch { }
                }

                try
                {
                    object pip = GetSingleton(typeof(PlayerManager));
                    if ((object)pip != null)
                    {
                        var getPlayer = typeof(PlayerManager).GetMethod("GetPlayer",
                            BindingFlags.Public | BindingFlags.Instance);
                        if ((object)getPlayer != null)
                        {
                            object player = getPlayer.Invoke(pip, null);
                            if ((object)player != null)
                            {
                                var hcqFld = player.GetType().GetField(
                                    "HCqxy",
                                    BindingFlags.Public | BindingFlags.Instance);
                                if ((object)hcqFld != null)
                                    hcqFld.SetValue(player, -1);
                            }
                        }
                    }
                }
                catch { }

                _startNewSession.Invoke(smInstance,
                    new object[] { (World)map.WorldInt, _sandboxValue, -1, modifiers });

                object sessionData = _sessionDataFld.GetValue(smInstance);
                if ((object)sessionData == null)
                {
                    MelonLogger.Error("[MapChanger] Session data null after StartNewSession.");
                    Telemetry.ReportErrorAsync(new System.Exception("[MapChanger] Session data null after StartNewSession."), "MapChanger");
                    return;
                }

                object levelInfo = _fmDOWdg.Invoke(null, new object[] { (long)map.CustomSeed });
                if ((object)levelInfo == null)
                {
                    MelonLogger.Error("[MapChanger] FmDOWdg returned null for seed=" + map.CustomSeed);
                    Telemetry.ReportErrorAsync(new System.Exception("[MapChanger] FmDOWdg returned null for seed=" + map.CustomSeed), "MapChanger");
                    return;
                }

                _currentLevelFld.SetValue(sessionData, levelInfo);

                _pushState.Invoke(stInstance, new object[] { _vtGenerating });

                ModLog.Debug("[MapChanger] Bike park load dispatched.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[MapChanger] ExecuteLoad: " + ex.Message); Telemetry.ReportErrorAsync(ex, "MapChanger"); }
        }

        private static void SuppressInactivityWarning()
        {
            try
            {
                object mm = GetSingleton(typeof(MultiManager));
                if ((object)mm == null) return;

                var murgZZE = typeof(MultiManager).GetField(
                    "murgZZE",
                    BindingFlags.Public | BindingFlags.Instance);
                var kVhi84yF = typeof(MultiManager).GetField(
                    "kVhiyF",
                    BindingFlags.Public | BindingFlags.Instance);

                if ((object)murgZZE != null) murgZZE.SetValue(mm, 0f);
                if ((object)kVhi84yF != null) kVhi84yF.SetValue(mm, Time.unscaledTime);

                var f = typeof(PermaGUI).GetField(
                    "\u005B\u007EqsVD\u007C", BindingFlags.Public | BindingFlags.Static);
                if ((object)f == null) return;
                object pg = f.GetValue(null);
                if ((object)pg == null) return;
                var m = pg.GetType().GetMethod("ShowInactivityWarning",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)m != null) m.Invoke(pg, new object[] { false });
            }
            catch { }
        }

        private static bool ResolveReflection()
        {
            if ((object)_fmDOWdg != null) return true;

            try
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!string.Equals(asm.GetName().Name, "Assembly-CSharp",
                        System.StringComparison.Ordinal)) continue;
                    _wiWlGzType = asm.GetType("\u0081wiWlGz");
                    if ((object)_wiWlGzType == null)
                    {
                        foreach (var t in asm.GetTypes())
                        {
                            var m = t.GetMethod("Fm\u007DOWd\u0060",
                                BindingFlags.Public | BindingFlags.Static,
                                null, new System.Type[] { typeof(long) }, null);
                            if ((object)m != null) { _wiWlGzType = t; break; }
                        }
                    }
                    break;
                }
                if ((object)_wiWlGzType == null)
                {
                    MelonLogger.Error("[MapChanger] wiWlGz type not found.");
                    Telemetry.ReportErrorAsync(new System.Exception("[MapChanger] wiWlGz type not found."), "MapChanger");
                    return false;
                }

                _fmDOWdg = _wiWlGzType.GetMethod("Fm\u007DOWd\u0060",
                    BindingFlags.Public | BindingFlags.Static,
                    null, new System.Type[] { typeof(long) }, null);

                foreach (var m in typeof(SessionManager).GetMethods(
                    BindingFlags.Public | BindingFlags.Instance))
                {
                    if (m.Name != "StartNewSession") continue;
                    var p = m.GetParameters();
                    if (p.Length == 4
                        && string.Equals(p[0].ParameterType.FullName,
                            typeof(World).FullName, System.StringComparison.Ordinal)
                        && p[1].ParameterType.IsEnum
                        && string.Equals(p[2].ParameterType.FullName,
                            typeof(int).FullName, System.StringComparison.Ordinal))
                    {
                        if (System.Enum.IsDefined(p[1].ParameterType, "Sandbox"))
                        {
                            _startNewSession = m;
                            _rDRSType = p[1].ParameterType;
                            _sandboxValue = System.Enum.Parse(_rDRSType, "Sandbox");
                        }
                    }
                    else if (p.Length == 2
                        && string.Equals(p[0].ParameterType.FullName,
                            typeof(string).FullName, System.StringComparison.Ordinal)
                        && p[1].ParameterType.IsEnum)
                    {
                        if ((object)_rDRSType == null)
                            _rDRSType = p[1].ParameterType;
                        if (_sandboxValue == null && System.Enum.IsDefined(p[1].ParameterType, "Sandbox"))
                            _sandboxValue = System.Enum.Parse(p[1].ParameterType, "Sandbox");
                        _startNewSessionString = m;
                    }
                }
                if ((object)_startNewSession == null)
                {
                    MelonLogger.Error("[MapChanger] StartNewSession(World,sessionType) not found.");
                    Telemetry.ReportErrorAsync(new System.Exception("[MapChanger] StartNewSession(World,sessionType) not found."), "MapChanger");
                    return false;
                }

                _loadLevelFromSeedMethod = typeof(DevCommandsGameplay).GetMethod(
                    "LoadLevelFromSeed", BindingFlags.Public | BindingFlags.Static);
                _sessionSeedMethod = typeof(DevCommandsGameplay).GetMethod(
                    "SessionSeed", BindingFlags.Public | BindingFlags.Static);
                _setSeedMethod = typeof(DevCommandsGameplay).GetMethod(
                    "SetSeed", BindingFlags.Public | BindingFlags.Static);

                _getCurrentLevelFullSeedMethod = typeof(SessionManager).GetMethod(
                    "GetCurrentLevelFullSeed",
                    BindingFlags.Public | BindingFlags.Instance);

                _parseSeedMethod = typeof(SessionManager).GetMethod(
                    "ParseSeed", BindingFlags.Public | BindingFlags.Static);

                _sessionDataFld = typeof(SessionManager).GetField(
                    "ESVMoz",
                    BindingFlags.Public | BindingFlags.Instance);

                if ((object)_sessionDataFld != null)
                {
                    _currentLevelFld = _sessionDataFld.FieldType.GetField(
                        "vebfkn",
                        BindingFlags.Public | BindingFlags.Instance);

                    if ((object)_rDRSType != null)
                    {
                        FieldInfo[] sdFields = _sessionDataFld.FieldType.GetFields(
                            BindingFlags.Public | BindingFlags.Instance);
                        for (int sdi = 0; sdi < sdFields.Length; sdi++)
                        {
                            if (string.Equals(sdFields[sdi].FieldType.FullName, _rDRSType.FullName,
                                System.StringComparison.Ordinal))
                            {
                                _sessionDataSessionTypeFld = sdFields[sdi];
                                break;
                            }
                        }
                    }
                }

                if ((object)_sessionDataFld == null || (object)_currentLevelFld == null)
                {
                    MelonLogger.Error("[MapChanger] Session data fields not found.");
                    Telemetry.ReportErrorAsync(new System.Exception("[MapChanger] Session data fields not found."), "MapChanger");
                    return false;
                }

                _closeCurrentSessionMethod = typeof(SessionManager).GetMethod(
                    "CloseCurrentSession", BindingFlags.Public | BindingFlags.Instance);
                _writeCurrentLevelSeedMethod = typeof(SessionManager).GetMethod(
                    "WriteCurrentLevelSeed", BindingFlags.Public | BindingFlags.Instance);

                if ((object)_rDRSType != null)
                {
                    foreach (FieldInfo f in typeof(SessionManager).GetFields(
                        BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (string.Equals(f.FieldType.FullName, _rDRSType.FullName,
                            System.StringComparison.Ordinal))
                        {
                            _sessionTypeFld = f;
                            break;
                        }
                    }
                }

                _pushState = typeof(StateMachine).GetMethod("PushState",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)_pushState != null)
                {
                    var vtType = _pushState.GetParameters()[0].ParameterType;
                    _vtGenerating = System.Enum.Parse(vtType, "Generating");
                    if (System.Enum.IsDefined(vtType, "Sandbox"))
                        _vtSandbox = System.Enum.Parse(vtType, "Sandbox");
                    if (System.Enum.IsDefined(vtType, "InGame"))
                        _vtInGame = System.Enum.Parse(vtType, "InGame");
                }
                if ((object)_pushState == null || (object)_vtGenerating == null)
                {
                    MelonLogger.Error("[MapChanger] PushState or Vt.Generating not found.");
                    Telemetry.ReportErrorAsync(new System.Exception("[MapChanger] PushState or Vt.Generating not found."), "MapChanger");
                    return false;
                }

                ModLog.Debug("[MapChanger] Reflection resolved OK. "
                    + "wiWlGz=" + _wiWlGzType.Name
                    + " sessionType=" + _rDRSType.Name);
                return true;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[MapChanger] ResolveReflection: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "MapChanger");
                return false;
            }
        }

        public static void OnSceneInitialized(string sceneName)
        {
            if (!string.IsNullOrEmpty(sceneName))
                _cachedSceneName = sceneName;
            else
                _cachedSceneName = "";
            CacheCurrentLevelSeed();
        }

        public static string GetCurrentSceneName()
        {
            if (!string.IsNullOrEmpty(_cachedSceneName))
                return _cachedSceneName;
            try
            {
                Scene scene = SceneManager.GetActiveScene();
                if (scene.IsValid() && !string.IsNullOrEmpty(scene.name))
                    return scene.name;
            }
            catch { }
            return "";
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                var target = typeof(UI_FreerideBikeParks).GetMethod(
                    "OnEnable",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if ((object)target == null)
                {
                    ModLog.Warn("[MapChanger] UI_FreerideBikeParks.OnEnable not found.");
                    return;
                }
                var postfix = typeof(MapChanger).GetMethod(
                    "Patch_FreerideBikeParksRefresh",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                harmony.Patch(target, postfix: new HarmonyLib.HarmonyMethod(postfix));
                ModLog.Debug("[MapChanger] UI_FreerideBikeParks.OnEnable patch applied.");

                MethodInfo startModSession = null;
                MethodInfo[] smMethods = typeof(SessionManager).GetMethods(
                    BindingFlags.Public | BindingFlags.Instance);
                for (int smi = 0; smi < smMethods.Length; smi++)
                {
                    MethodInfo smMethod = smMethods[smi];
                    if (smMethod.Name != "StartNewSession") continue;
                    ParameterInfo[] smParams = smMethod.GetParameters();
                    if (smParams.Length != 2) continue;
                    if (!smParams[1].ParameterType.IsGenericType) continue;
                    System.Type[] genArgs = smParams[1].ParameterType.GetGenericArguments();
                    if (genArgs == null || genArgs.Length != 1) continue;
                    string modParam = smParams[0].ParameterType.FullName ?? "";
                    if (modParam.IndexOf("ModTool.", System.StringComparison.Ordinal) < 0) continue;
                    startModSession = smMethod;
                    break;
                }

                if ((object)startModSession != null)
                {
                    var modSessionPostfix = typeof(MapChanger).GetMethod(
                        "Patch_StartNewSessionModPostfix",
                        BindingFlags.Public | BindingFlags.Static);
                    harmony.Patch(startModSession,
                        postfix: new HarmonyLib.HarmonyMethod(modSessionPostfix));
                    ModLog.Debug("[MapChanger] SessionManager.StartNewSession(Mod) patch applied.");
                }
                else
                    ModLog.Warn("[MapChanger] StartNewSession(Mod) not found for Harmony patch.");
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[MapChanger] Patch failed: " + ex.Message);
            }
        }

        public static void Patch_FreerideBikeParksRefresh()
        {
            if (HasBikeParks) return;
            ModLog.Debug("[MapChanger] Freeride screen opened — scanning bike parks...");
            BuildMapList();
            try { MapPage.RebuildList(); } catch { }
        }

        /// <summary>After game workshop StartNewSession(Mod) — force sandbox freeride, not Last Stand.</summary>
        public static void Patch_StartNewSessionModPostfix()
        {
            if (!ModWorkshopLoader.IsForceSandboxModSession()) return;
            ApplySandboxWorkshopRidePublic();
        }

        // ── Generic helpers ───────────────────────────────────────────
        private static object GetSingleton(System.Type targetType)
        {
            try
            {
                var directField = targetType.GetField(
                    "[~qsVD|",
                    BindingFlags.Public | BindingFlags.Static);
                if ((object)directField != null)
                {
                    object val = directField.GetValue(null);
                    if ((object)val != null) return val;
                }

                var singletonType = typeof(Singleton<>).MakeGenericType(targetType);
                foreach (var p in singletonType.GetProperties(
                    BindingFlags.Public | BindingFlags.Static))
                {
                    if (string.Equals(p.PropertyType.FullName,
                        targetType.FullName, System.StringComparison.Ordinal))
                        return p.GetValue(null, null);
                }
                foreach (var f in singletonType.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (string.Equals(f.FieldType.FullName,
                        targetType.FullName, System.StringComparison.Ordinal))
                        return f.GetValue(null);
                }
            }
            catch { }
            return null;
        }

        private static T GetPublicField<T>(object obj, string name)
        {
            try
            {
                var f = obj.GetType().GetField(name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)f == null) return default(T);
                object v = f.GetValue(obj);
                if ((object)v == null) return default(T);
                return (T)v;
            }
            catch { return default(T); }
        }

        private static int ReadCurrentScore()
        {
            try
            {
                PlayerManager pm = GameObject.FindObjectOfType<PlayerManager>();
                if ((object)pm == null) return 0;
                PlayerInfoImpact pip = pm.GetPlayer() as PlayerInfoImpact;
                if ((object)pip == null) return 0;
                foreach (var f in typeof(PlayerInfoImpact).GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    object s = f.GetValue(pip);
                    if ((object)s == null) continue;
                    foreach (var sf in s.GetType().GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (!sf.Name.Contains("LgqK")) continue;
                        object ob = sf.GetValue(s);
                        if ((object)ob == null) continue;
                        MethodInfo dec = ob.GetType().GetMethod("DZlraRf",
                            BindingFlags.Public | BindingFlags.Static,
                            null, new System.Type[] { ob.GetType() }, null);
                        if ((object)dec == null) continue;
                        object r = dec.Invoke(null, new object[] { ob });
                        if (r != null)
                        {
                            try { return (int)r; }
                            catch { return System.Convert.ToInt32(r); }
                        }
                    }
                }
            }
            catch { }
            return 0;
        }

        // ── Read live session seed ────────────────────────────────────
        private static MethodInfo _seedGetterMethod = null;
        private static string _cachedSeedString = "";
        private static string _cachedSceneName = "";

        public static void CacheCurrentLevelSeed()
        {
            _cachedSeedString = "";
            _seedGetterMethod = null;
            try
            {
                SessionManager sm = UnityEngine.Object.FindObjectOfType<SessionManager>();
                if ((object)sm == null) return;

                if ((object)_getSessionSeedIntMethod == null)
                    _getSessionSeedIntMethod = typeof(SessionManager).GetMethod(
                        "GetSessionSeed", BindingFlags.Public | BindingFlags.Instance);
                if ((object)_getCurrentLevelFullSeedMethod == null)
                    _getCurrentLevelFullSeedMethod = typeof(SessionManager).GetMethod(
                        "GetCurrentLevelFullSeed",
                        BindingFlags.Public | BindingFlags.Instance);

                int trailSeed = 0;
                if ((object)_getSessionSeedIntMethod != null)
                {
                    object trail = _getSessionSeedIntMethod.Invoke(sm, null);
                    if (trail != null)
                    {
                        try { trailSeed = System.Convert.ToInt32(trail); }
                        catch { }
                    }
                }

                string fullStr = null;
                if ((object)_getCurrentLevelFullSeedMethod != null)
                {
                    object full = _getCurrentLevelFullSeedMethod.Invoke(sm, null);
                    fullStr = full as string;
                }

                if (!string.IsNullOrEmpty(fullStr) && fullStr != "0")
                {
                    _cachedSeedString = fullStr.Trim();
                    ModLog.Debug("[MapChanger] Cached full level seed: " + _cachedSeedString);
                    return;
                }

                if (trailSeed != 0)
                {
                    _cachedSeedString = trailSeed.ToString();
                    ModLog.Debug("[MapChanger] Cached session trail: " + _cachedSeedString);
                    return;
                }

                if ((object)_sessionDataFld == null || (object)_currentLevelFld == null)
                {
                    if ((object)_sessionDataFld == null)
                        _sessionDataFld = typeof(SessionManager).GetField(
                            "ESVMoz", BindingFlags.Public | BindingFlags.Instance);
                    if ((object)_sessionDataFld != null && (object)_currentLevelFld == null)
                        _currentLevelFld = _sessionDataFld.FieldType.GetField(
                            "vebfkn", BindingFlags.Public | BindingFlags.Instance);
                }

                object sessionData = _sessionDataFld.GetValue(sm);
                if ((object)sessionData == null) return;

                object levelInfo = _currentLevelFld.GetValue(sessionData);
                if ((object)levelInfo == null) return;

                MethodInfo[] methods = levelInfo.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < methods.Length; i++)
                {
                    if (!string.Equals(methods[i].ReturnType.Name, "Int64",
                        System.StringComparison.Ordinal)) continue;
                    if (methods[i].GetParameters().Length != 0) continue;
                    _seedGetterMethod = methods[i];
                    break;
                }
                if ((object)_seedGetterMethod == null) return;

                object seed = _seedGetterMethod.Invoke(levelInfo, null);
                if ((object)seed != null)
                {
                    _cachedSeedString = seed.ToString();
                    ModLog.Debug("[MapChanger] Cached map seed: " + _cachedSeedString);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[MapChanger] CacheCurrentLevelSeed: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "MapChanger");
            }
        }

        public static string GetCurrentLevelSeed() { return _cachedSeedString; }

        public static bool TryGetSessionSeedString(out string seedStr)
        {
            if (string.IsNullOrEmpty(_cachedSeedString))
                CacheCurrentLevelSeed();
            seedStr = _cachedSeedString?.Trim();
            return !string.IsNullOrEmpty(seedStr);
        }

        private static bool TryParseFullSeedLong(string seed, out long seedLong)
        {
            seedLong = 0L;
            if (string.IsNullOrEmpty(seed)) return false;
            string part = seed.Split('-')[0].Trim();
            return long.TryParse(part, out seedLong) && seedLong != 0L;
        }

        private static bool DispatchStartNewSessionFromSeedString(string seed, int sessionTypeInt)
        {
            if ((object)_startNewSessionString == null)
                return false;

            object sessionType = ResolveSessionTypeValue(sessionTypeInt);
            if (sessionType == null)
                return false;

            object smInstance = GetSingleton(typeof(SessionManager));
            if ((object)smInstance == null)
            {
                MelonLogger.Error("[MapChanger] SessionManager null.");
                return false;
            }

            try
            {
                TryCloseCurrentSession(smInstance);
                _startNewSessionString.Invoke(smInstance, new object[] { seed, sessionType });
                if (!TryPushGeneratingState())
                {
                    ModLog.Debug("[MapChanger] StartNewSession(string): PushState failed.");
                    return false;
                }
                SuppressInactivityWarning();
                LastLoadedSeed = seed;
                ModLog.Debug("[MapChanger] StartNewSession(string) seed=\"" + seed
                    + "\" type=" + sessionTypeInt);
                return true;
            }
            catch (System.Exception ex)
            {
                string detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ModLog.Debug("[MapChanger] StartNewSession(string,\"" + seed + "\"): " + detail);
                return false;
            }
        }

        private static void TryCloseCurrentSession(object smInstance)
        {
            if ((object)smInstance == null) return;
            if ((object)_closeCurrentSessionMethod == null)
                _closeCurrentSessionMethod = typeof(SessionManager).GetMethod(
                    "CloseCurrentSession", BindingFlags.Public | BindingFlags.Instance);
            if ((object)_closeCurrentSessionMethod == null) return;
            try { _closeCurrentSessionMethod.Invoke(smInstance, null); }
            catch { }
        }

        public static bool TryGetCurrentSessionTypeInt(out int sessionTypeInt)
        {
            sessionTypeInt = 0;
            try
            {
                if (!ResolveReflection()) return false;
                object smInstance = GetSingleton(typeof(SessionManager));
                if ((object)smInstance == null || (object)_sessionTypeFld == null) return false;
                object val = _sessionTypeFld.GetValue(smInstance);
                if (val == null) return false;
                sessionTypeInt = System.Convert.ToInt32(val);
                return sessionTypeInt != 0;
            }
            catch { return false; }
        }

        public static bool TryGetCurrentSessionDataSessionTypeInt(out int sessionTypeInt)
        {
            sessionTypeInt = 0;
            try
            {
                if (!ResolveReflection()) return false;
                object smInstance = GetSingleton(typeof(SessionManager));
                if ((object)smInstance == null || (object)_sessionDataFld == null
                    || (object)_sessionDataSessionTypeFld == null)
                    return false;
                object sessionData = _sessionDataFld.GetValue(smInstance);
                if ((object)sessionData == null) return false;
                object val = _sessionDataSessionTypeFld.GetValue(sessionData);
                if (val == null) return false;
                sessionTypeInt = System.Convert.ToInt32(val);
                return sessionTypeInt != 0;
            }
            catch { return false; }
        }

        private static object ResolveSessionTypeValue(int sessionTypeInt)
        {
            if ((object)_rDRSType == null) return _sandboxValue;
            if (sessionTypeInt > 0 && System.Enum.IsDefined(_rDRSType, sessionTypeInt))
                return System.Enum.ToObject(_rDRSType, sessionTypeInt);
            return _sandboxValue;
        }

        /// <summary>Free ride creation uses Sandbox — StandardSession is career (lives).</summary>
        private static int GetFreerideSandboxSessionTypeInt()
        {
            if ((object)_sandboxValue != null)
                return System.Convert.ToInt32(_sandboxValue);
            return 5;
        }

        public static int GetFreerideSandboxSessionTypeIntPublic()
        {
            if (!ResolveReflection())
                return 5;
            return GetFreerideSandboxSessionTypeInt();
        }

        /// <summary>
        /// Workshop mod StartNewSession(Mod) sets session data to Sandbox but can leave
        /// SessionManager on StandardSession (career / Last Stand). Force both to Sandbox.
        /// </summary>
        public static void ApplyFreerideSandboxSessionTypePublic()
        {
            try
            {
                if (!ResolveReflection()) return;
                object sandbox = GetFreerideSandboxSessionType();
                if (sandbox == null) return;

                object smInstance = GetSingleton(typeof(SessionManager));
                if ((object)smInstance == null) return;

                if ((object)_sessionTypeFld != null)
                    _sessionTypeFld.SetValue(smInstance, sandbox);

                if ((object)_sessionDataFld != null)
                {
                    object sessionData = _sessionDataFld.GetValue(smInstance);
                    if ((object)sessionData != null && (object)_sessionDataSessionTypeFld != null)
                        _sessionDataSessionTypeFld.SetValue(sessionData, sandbox);
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[MapChanger] ApplyFreerideSandboxSessionType: " + ex.Message);
            }
        }

        /// <summary>Sandbox health + no Last Stand lives after mod.io session start.</summary>
        public static void ApplySandboxPlayerHealthPublic()
        {
            try
            {
                if ((object)_getAllPlayersImpactMethod == null)
                {
                    _getAllPlayersImpactMethod = typeof(PlayerManager).GetMethod(
                        "GetAllPlayersImpact", BindingFlags.Public | BindingFlags.Instance);
                }
                if ((object)_sandboxInfHealthMethod == null)
                {
                    _sandboxInfHealthMethod = typeof(PlayerInfoImpact).GetMethod(
                        "SandboxInfHealth",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                object playerMgr = GetSingleton(typeof(PlayerManager));
                if ((object)playerMgr == null || (object)_getAllPlayersImpactMethod == null)
                    return;

                object players = _getAllPlayersImpactMethod.Invoke(playerMgr, null);
                if (players is System.Array arr)
                {
                    System.Reflection.FieldInfo livesFld = null;
                    for (int i = 0; i < arr.Length; i++)
                    {
                        object impact = arr.GetValue(i);
                        if (impact == null) continue;
                        if ((object)livesFld == null)
                            livesFld = impact.GetType().GetField(
                                "HCqxy", BindingFlags.Public | BindingFlags.Instance);
                        if ((object)livesFld != null)
                            livesFld.SetValue(impact, -1);
                        if ((object)_sandboxInfHealthMethod != null)
                            _sandboxInfHealthMethod.Invoke(impact, null);
                    }
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[MapChanger] ApplySandboxPlayerHealth: " + ex.Message);
            }
        }

        /// <summary>Session type + player state for mod.io GO (not career Last Stand).</summary>
        public static void ApplySandboxWorkshopRidePublic()
        {
            ApplyFreerideSandboxSessionTypePublic();
            ApplySandboxPlayerHealthPublic();
        }

        public static bool TryIsPlayerLastStand(out bool lastStand)
        {
            lastStand = false;
            try
            {
                if ((object)_isLastStandMethod == null)
                {
                    _isLastStandMethod = typeof(PlayerInfoImpact).GetMethod(
                        "IsLastStand",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }
                if ((object)_isLastStandMethod == null) return false;

                object playerMgr = GetSingleton(typeof(PlayerManager));
                if ((object)playerMgr == null) return false;
                MethodInfo getImpact = typeof(PlayerManager).GetMethod(
                    "GetPlayerImpact", BindingFlags.Public | BindingFlags.Instance);
                if ((object)getImpact == null) return false;

                object impact = getImpact.Invoke(playerMgr, null);
                if (impact == null) return false;
                object result = _isLastStandMethod.Invoke(impact, null);
                if (result is bool b)
                {
                    lastStand = b;
                    return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>Workshop map loaded in sandbox freeride — not StandardSession / Last Stand.</summary>
        public static bool IsSandboxWorkshopRideReady()
        {
            if (!InWorkshopLevel()) return false;

            int sandboxType = GetFreerideSandboxSessionTypeIntPublic();
            int managerType;
            if (!TryGetCurrentSessionTypeInt(out managerType) || managerType != sandboxType)
                return false;

            int dataType;
            if (!TryGetCurrentSessionDataSessionTypeInt(out dataType) || dataType != sandboxType)
                return false;

            bool lastStand;
            if (TryIsPlayerLastStand(out lastStand) && lastStand)
                return false;

            return true;
        }

        /// <summary>Session flags + InGame + terrain colliders — safe to teleport.</summary>
        public static bool IsWorkshopRidePlayable()
        {
            if (!IsSandboxWorkshopRideReady())
                return false;
            if (!IsSessionStarted())
                return false;
            if (!IsRideStateInGame())
                return false;
            if (!IsWorkshopWorldGeometryReady())
                return false;
            return true;
        }

        /// <summary>Map generation finished at spawn — OK to hide load overlay.</summary>
        public static bool IsWorkshopRidePlayableForLoadComplete()
        {
            if (!IsSandboxWorkshopRideReady())
                return false;
            if (!IsSessionStarted())
                return false;
            if (IsGeneratingRideState())
                return false;
            if (!IsRideStateInGame())
                return false;

            Vector3 riderPos;
            if (!TryGetRiderWorldPosition(out riderPos))
                return false;
            return IsGroundColliderNear(riderPos);
        }

        public static bool IsRideStateInGame()
        {
            int stateInt;
            if (!TryGetCurrentRideStateInt(out stateInt))
                return false;

            if ((object)_vtInGame != null)
                return stateInt == System.Convert.ToInt32(_vtInGame);
            return stateInt == 6;
        }

        public static bool IsGeneratingRideState()
        {
            int stateInt;
            if (!TryGetCurrentRideStateInt(out stateInt))
                return InWorkshopLevel();

            if ((object)_vtGenerating != null)
                return stateInt == System.Convert.ToInt32(_vtGenerating);
            return stateInt == 4;
        }

        /// <summary>Workshop terrain exists near rider or pending GO destination.</summary>
        public static bool IsWorkshopWorldGeometryReady()
        {
            if (!InWorkshopLevel())
                return false;

            Vector3 riderPos;
            if (TryGetRiderWorldPosition(out riderPos) && IsGroundColliderNear(riderPos))
                return true;

            Vector3 pending;
            if (SavedLocations.TryGetPendingGoWorldPosition(out pending)
                && IsGroundColliderNear(pending))
                return true;

            return false;
        }

        private static bool TryGetRiderWorldPosition(out Vector3 pos)
        {
            pos = Vector3.zero;
            try
            {
                GameObject local = GameObject.Find("Player_Human");
                if (!UnityNull.Alive(local)) return false;
                pos = local.transform.position;
                return true;
            }
            catch { return false; }
        }

        private static bool TryGetCurrentRideStateInt(out int stateInt)
        {
            stateInt = 0;
            try
            {
                if (!ResolveReflection()) return false;
                object stInstance = GetSingleton(typeof(StateMachine));
                if ((object)stInstance == null) return false;

                if ((object)_currentStateProp == null && (object)_currentStateGetter == null)
                {
                    _currentStateProp = typeof(StateMachine).GetProperty(
                        "currentState", BindingFlags.Public | BindingFlags.Instance);
                    if ((object)_currentStateProp == null)
                        _currentStateGetter = typeof(StateMachine).GetMethod(
                            "get_currentState",
                            BindingFlags.Public | BindingFlags.Instance);
                }

                object val = null;
                if ((object)_currentStateProp != null)
                    val = _currentStateProp.GetValue(stInstance, null);
                else if ((object)_currentStateGetter != null)
                    val = _currentStateGetter.Invoke(stInstance, null);

                if (val == null) return false;
                stateInt = System.Convert.ToInt32(val);
                return true;
            }
            catch { return false; }
        }

        /// <summary>True when terrain colliders exist near a saved spot (not brown void).</summary>
        public static bool IsGroundColliderNear(Vector3 pos, float probeUp = 80f, float probeDown = 600f)
        {
            try
            {
                Vector3 origin = pos + Vector3.up * probeUp;
                RaycastHit hit;
                if (Physics.Raycast(origin, Vector3.down, out hit, probeUp + probeDown))
                {
                    if ((object)hit.collider == null) return false;
                    float dy = Mathf.Abs(hit.point.y - pos.y);
                    if (dy > 120f) return false;
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static object GetFreerideSandboxSessionType()
        {
            if ((object)_sandboxValue != null)
                return _sandboxValue;
            if ((object)_rDRSType != null && System.Enum.IsDefined(_rDRSType, 5))
                return System.Enum.ToObject(_rDRSType, 5);
            return null;
        }

        private static bool TryParseGameSeed(string seed, out int partA, out int partB)
        {
            partA = 0;
            partB = 0;
            if (string.IsNullOrEmpty(seed)) return false;

            if ((object)_parseSeedMethod == null)
                _parseSeedMethod = typeof(SessionManager).GetMethod(
                    "ParseSeed", BindingFlags.Public | BindingFlags.Static);
            if ((object)_parseSeedMethod == null) return false;

            try
            {
                object[] args = new object[] { seed.Trim(), 0L, 0L };
                _parseSeedMethod.Invoke(null, args);
                long a = System.Convert.ToInt64(args[1]);
                long b = System.Convert.ToInt64(args[2]);
                if (a <= 0L) return false;
                partA = (int)a;
                partB = (int)b;
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Game share strings are levelHash or levelHash-modifierSeed (not world-trail).
        /// Repair legacy saves that stored world-trail composites from earlier mod builds.
        /// </summary>
        private static string NormalizeFreerideLoadSeed(string storedSeed, int fallbackWorld)
        {
            if (string.IsNullOrEmpty(storedSeed)) return "";
            string trimmed = storedSeed.Trim();

            int partA, partB;
            if (!TryParseGameSeed(trimmed, out partA, out partB) || partB == 0)
                return trimmed;

            // Legitimate share string: large hash + modifier (e.g. 2980323-12345).
            if (partA > 11) return trimmed;

            // Mis-save world-trail (e.g. 3-2980323) — load the trail/hash portion only.
            if (partA >= 1 && partA <= 11)
            {
                if (fallbackWorld > 0 && fallbackWorld != partA)
                    ModLog.Debug("[MapChanger] Repairing world-trail seed \"" + trimmed
                        + "\" -> \"" + partB + "\"");
                return partB.ToString();
            }

            return trimmed;
        }

        public static bool FreerideSeedsMatch(string saved, string live, int worldInt)
        {
            if (string.IsNullOrEmpty(saved) || string.IsNullOrEmpty(live)) return false;
            if (string.Equals(saved.Trim(), live.Trim(), System.StringComparison.Ordinal))
                return true;
            string normSaved = NormalizeFreerideLoadSeed(saved, worldInt);
            string normLive = NormalizeFreerideLoadSeed(live, worldInt);
            return string.Equals(normSaved, normLive, System.StringComparison.Ordinal);
        }

        private static bool IsLikelyFreerideShareSeed(string seed)
        {
            if (string.IsNullOrEmpty(seed)) return false;
            string trimmed = seed.Trim();
            if (trimmed.Length > 24) return false;

            string[] parts = trimmed.Split('-');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (part.Length == 0) return false;
                long n;
                if (!long.TryParse(part, out n) || n == 0L) return false;
            }
            return true;
        }

        private static bool DispatchFreerideSeedManual(string loadSeed, int sandboxTypeInt)
        {
            object smInstance = GetSingleton(typeof(SessionManager));
            if ((object)smInstance == null || (object)_fmDOWdg == null || (object)_startNewSession == null)
                return false;

            int hashInt, modInt;
            if (!TryParseGameSeed(loadSeed, out hashInt, out modInt) || hashInt <= 0)
                return false;

            object sessionType = GetFreerideSandboxSessionType();
            if (sessionType == null) return false;

            try
            {
                TryCloseCurrentSession(smInstance);

                object levelInfo = _fmDOWdg.Invoke(null, new object[] { (long)hashInt });
                if ((object)levelInfo == null)
                {
                    ModLog.Debug("[MapChanger] FmDOWdg null for hash " + hashInt);
                    return false;
                }

                object modifiers = null;
                try
                {
                    object gameData = GetSingleton(typeof(GameData));
                    if ((object)gameData != null)
                    {
                        MethodInfo getMods = typeof(GameData).GetMethod(
                            "GetModifiersFromSeed", BindingFlags.Public | BindingFlags.Instance);
                        if ((object)getMods != null)
                        {
                            object playerMgr = GetSingleton(typeof(PlayerManager));
                            object playerImpact = null;
                            if ((object)playerMgr != null)
                            {
                                MethodInfo getImpact = typeof(PlayerManager).GetMethod(
                                    "GetPlayerImpact", BindingFlags.Public | BindingFlags.Instance);
                                if ((object)getImpact != null)
                                    playerImpact = getImpact.Invoke(playerMgr, null);
                            }

                            // Fa~Qg\u0081u.LevelGeneration = 1 in vanilla builds.
                            modifiers = getMods.Invoke(gameData, new object[] { (long)modInt, 1 });
                            modifiers = CrewPerkManager.MergeIntoSessionModifiers(modifiers);
                        }
                    }
                }
                catch { }

                int worldInt = ReadWorldInt(levelInfo);
                if (worldInt <= 0) return false;

                _startNewSession.Invoke(smInstance,
                    new object[] { (World)worldInt, sessionType, -1, modifiers });

                object sessionData = _sessionDataFld.GetValue(smInstance);
                if ((object)sessionData == null) return false;
                _currentLevelFld.SetValue(sessionData, levelInfo);

                if (!TryPushGeneratingState()) return false;
                SuppressInactivityWarning();
                LastLoadedSeed = loadSeed;
                ModLog.Debug("[MapChanger] Freeride manual hash=" + hashInt + " mod=" + modInt);
                return true;
            }
            catch (System.Exception ex)
            {
                string detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ModLog.Debug("[MapChanger] Freeride manual load: " + detail);
                return false;
            }
        }

        private static bool DispatchStartNewSessionFromSeedString(string seed)
        {
            return DispatchStartNewSessionFromSeedString(seed, 0);
        }

        public struct MapLocationContext
        {
            public bool Supported;
            public string StorageKey;
            public string DisplayLabel;
        }

        /// <summary>
        /// Stable key for per-map saved locations. Prefer seed; else world name; else Unity scene name
        /// (mod.io / workshop maps).
        /// </summary>
        public static bool TryGetMapLocationContext(out MapLocationContext ctx)
        {
            ctx = new MapLocationContext();

            string scene = GetCurrentSceneName();
            // Sandbox mod menu open is OK — player is still on the map underneath.
            if (IsOnOverworldWorld()) return false;
            if (IsBlockedRideScene(scene)) return false;
            if (IsHubSceneName(scene)) return false;

            bool ridingHud = StatsManager.IsRidingHudActive();
            bool sessionStarted = IsSessionStarted();
            bool inFixedLevel = InBikeParkLevel() || InWorkshopLevel();

            object levelInfo = TryGetCurrentLevelInfo();
            if ((object)levelInfo != null)
            {
                int customSeed = GetPublicField<int>(levelInfo, "customSeed");
                string levelName = GetPublicField<string>(levelInfo, "levelName");
                string resolved = ResolveRideDisplayLabel(scene, levelName, customSeed);
                if (customSeed != 0)
                {
                    string label = !string.IsNullOrEmpty(resolved)
                        ? resolved
                        : (!string.IsNullOrEmpty(levelName)
                            ? PrettyName(levelName) : FindMapNameForSeed(customSeed));
                    if (string.IsNullOrEmpty(label))
                        label = "Seed " + customSeed;
                    ctx.Supported = true;
                    ctx.StorageKey = "seed_" + customSeed;
                    ctx.DisplayLabel = label;
                    return true;
                }

                if (!string.IsNullOrEmpty(resolved) && !IsBaseWorldDisplayName(resolved))
                {
                    ctx.Supported = true;
                    ctx.StorageKey = "name_" + resolved;
                    ctx.DisplayLabel = resolved;
                    return true;
                }

                int worldInt = ReadWorldInt(levelInfo);
                bool namedPark = InBikeParkLevel() || InWorkshopLevel()
                    || (!string.IsNullOrEmpty(resolved) && !IsBaseWorldDisplayName(resolved));
                if (worldInt > 0 && !namedPark && (ridingHud || sessionStarted || inFixedLevel))
                {
                    string baseName = GetBaseWorldName(worldInt);
                    if (!string.IsNullOrEmpty(baseName))
                    {
                        ctx.Supported = true;
                        ctx.StorageKey = "name_" + baseName;
                        ctx.DisplayLabel = baseName;
                        return true;
                    }
                }
            }

            if (ridingHud || sessionStarted || inFixedLevel)
            {
                if (string.IsNullOrEmpty(_cachedSeedString))
                    CacheCurrentLevelSeed();

                string seedStr = _cachedSeedString;
                if (!string.IsNullOrEmpty(seedStr)
                    && long.TryParse(seedStr, out long seedLong)
                    && seedLong != 0L)
                {
                    string label = FindMapNameForSeed((int)seedLong);
                    if (string.IsNullOrEmpty(label))
                        label = "Seed " + seedStr;
                    ctx.Supported = true;
                    ctx.StorageKey = "seed_" + seedStr;
                    ctx.DisplayLabel = label;
                    return true;
                }
            }

            if (ridingHud || sessionStarted || inFixedLevel)
                return TryGetSceneLocationContext(scene, out ctx);

            return false;
        }

        /// <summary>Hub, menu, sandbox UI, or stale career session — not an active ride map.</summary>
        public static bool IsNonRideMapContext()
        {
            return IsNonRideMapContext(GetCurrentSceneName());
        }

        public static bool IsNonRideMapContext(string scene)
        {
            // Sandbox mod menu open is OK — player is still on the map underneath.
            if (StatsManager.IsInMenuContext()) return true;
            if (IsOnOverworldWorld()) return true;
            if (IsBlockedRideScene(scene)) return true;
            if (IsHubSceneName(scene)) return true;
            if (!StatsManager.IsRidingHudActive() && !IsSessionStarted()
                && !InBikeParkLevel() && !InWorkshopLevel())
                return true;
            return false;
        }

        public static bool InBikeParkLevel()
        {
            try
            {
                SessionManager sm = UnityEngine.Object.FindObjectOfType<SessionManager>();
                if ((object)sm == null) return false;
                MethodInfo m = typeof(SessionManager).GetMethod(
                    "InBikeParkLevel", BindingFlags.Public | BindingFlags.Instance);
                if ((object)m == null) return false;
                object result = m.Invoke(sm, null);
                if (result != null)
                {
                    try { return (bool)result; }
                    catch { }
                }
                return false;
            }
            catch { return false; }
        }

        public static bool IsHubSceneName(string scene)
        {
            if (string.IsNullOrEmpty(scene)) return false;
            string lower = scene.ToLowerInvariant();
            return lower.Contains("overworld") || lower.Contains("hub")
                || lower.Contains("home") || lower.Contains("shed")
                || lower.Contains("customization") || lower.Contains("menu")
                || lower.Contains("camp") || lower.Contains("lodge");
        }

        public static bool InWorkshopLevel()
        {
            try
            {
                SessionManager sm = UnityEngine.Object.FindObjectOfType<SessionManager>();
                if ((object)sm == null) return false;
                MethodInfo m = typeof(SessionManager).GetMethod(
                    "InWorkshopLevel", BindingFlags.Public | BindingFlags.Instance);
                if ((object)m == null) return false;
                object result = m.Invoke(sm, null);
                if (result != null)
                {
                    try { return (bool)result; }
                    catch { }
                }
                return false;
            }
            catch { return false; }
        }

        /// <summary>Bike-park / bonus level seed from level info (not session procedural seed).</summary>
        public static bool TryGetLevelCustomSeed(out int seed)
        {
            seed = 0;
            object levelInfo = TryGetCurrentLevelInfo();
            if ((object)levelInfo == null) return false;
            int customSeed = GetPublicField<int>(levelInfo, "customSeed");
            if (customSeed == 0) return false;
            seed = customSeed;
            return true;
        }

        /// <summary>True when on a random freeride world (no fixed bike-park seed).</summary>
        public static bool IsProceduralFreerideMap()
        {
            if (TryGetLevelCustomSeed(out int bikeParkSeed) && bikeParkSeed > 0)
                return false;

            object levelInfo = TryGetCurrentLevelInfo();
            if ((object)levelInfo != null)
            {
                int worldInt = ReadWorldInt(levelInfo);
                if (worldInt > 0)
                {
                    for (int i = 0; i < _baseWorlds.Length; i++)
                    {
                        if (_baseWorlds[i] == worldInt)
                            return true;
                    }
                }
            }

            MapLocationContext ctx;
            if (TryGetMapLocationContext(out ctx) && ctx.Supported)
            {
                if (ctx.StorageKey.StartsWith("name_", System.StringComparison.Ordinal))
                    return true;
                if (ctx.StorageKey.StartsWith("scene_", System.StringComparison.Ordinal))
                    return false;
                if (TryParseSeedFromStorageKey(ctx.StorageKey, out int sk) && sk > 0)
                {
                    if (_maps.Count == 0)
                        BuildMapList();
                    for (int i = 0; i < _maps.Count; i++)
                    {
                        if (_maps[i].CustomSeed == sk && _maps[i].IsBikePark)
                            return false;
                    }
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetSceneLocationContext(string sceneName, out MapLocationContext ctx)
        {
            ctx = new MapLocationContext();
            if (IsBlockedRideScene(sceneName))
                return false;

            string safe = SanitizeStoragePart(sceneName);
            if (string.IsNullOrEmpty(safe))
                return false;

            ctx.Supported = true;
            ctx.StorageKey = "scene_" + safe;
            ctx.DisplayLabel = PrettySceneLabel(sceneName);
            return true;
        }

        private static bool IsBlockedRideScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return true;
            string lower = sceneName.ToLowerInvariant();
            if (lower.Contains("dontdestroy")) return true;
            if (lower == "bootstrap" || lower == "main" || lower.Contains("menu")) return true;
            if (lower.Contains("loading") || lower.Contains("generating")) return true;
            if (lower == "modscene") return true;
            if (lower.Contains("customization") || lower.Contains("shed")) return true;
            if (lower.Contains("overworld")) return true;
            if (lower == "splash" || lower.Contains("splash")) return true;
            return false;
        }

        public static bool IsBlockedRideScenePublic(string sceneName) => IsBlockedRideScene(sceneName);

        /// <summary>Bootstrap, loading, splash — not a playable loaded scene.</summary>
        public static bool IsSystemScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return true;
            string lower = sceneName.ToLowerInvariant();
            if (lower.Contains("dontdestroy")) return true;
            if (lower == "bootstrap" || lower == "main") return true;
            if (lower.Contains("loading") || lower.Contains("generating")) return true;
            if (lower == "modscene") return true;
            if (lower == "splash" || lower.Contains("splash")) return true;
            return false;
        }

        public static string SanitizeStoragePartPublic(string raw) => SanitizeStoragePart(raw);

        private static string SanitizeStoragePart(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length && sb.Length < 48; i++)
            {
                char c = raw[i];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                    sb.Append(c);
                else if (c == '_' || c == '-' || c == '.' || c == ' ')
                    sb.Append('_');
            }
            string s = sb.ToString().Trim('_');
            while (s.Contains("__"))
                s = s.Replace("__", "_");
            return s;
        }

        private static string PrettySceneLabel(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return sceneName;
            string label = sceneName;
            if (label.StartsWith("Scene_", System.StringComparison.OrdinalIgnoreCase))
                label = label.Substring(6);
            label = label.Replace('_', ' ').Replace('-', ' ').Trim();
            return string.IsNullOrEmpty(label) ? sceneName : label;
        }

        public static string SceneToDisplayLabel(string sceneName) => PrettySceneLabel(sceneName);

        private static bool WorkshopSceneNameMatchesLabel(string sceneName, string label)
        {
            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(label)) return false;
            System.Text.StringBuilder sbScene = new System.Text.StringBuilder(sceneName.Length);
            System.Text.StringBuilder sbLabel = new System.Text.StringBuilder(label.Length);
            for (int i = 0; i < sceneName.Length; i++)
            {
                char c = sceneName[i];
                if (char.IsLetterOrDigit(c))
                    sbScene.Append(char.ToLowerInvariant(c));
            }
            for (int i = 0; i < label.Length; i++)
            {
                char c = label[i];
                if (char.IsLetterOrDigit(c))
                    sbLabel.Append(char.ToLowerInvariant(c));
            }
            string ns = sbScene.ToString();
            string nl = sbLabel.ToString();
            if (string.IsNullOrEmpty(ns) || string.IsNullOrEmpty(nl)) return false;
            return ns.StartsWith(nl, System.StringComparison.Ordinal)
                || nl.StartsWith(ns, System.StringComparison.Ordinal);
        }

        private static object TryGetCurrentLevelInfo()
        {
            try
            {
                if (!ResolveReflection()) return null;
                SessionManager sm = UnityEngine.Object.FindObjectOfType<SessionManager>();
                if ((object)sm == null) return null;
                object sessionData = _sessionDataFld.GetValue(sm);
                if ((object)sessionData == null) return null;
                return _currentLevelFld.GetValue(sessionData);
            }
            catch { return null; }
        }

        private static int ReadWorldInt(object levelInfo)
        {
            if ((object)levelInfo == null) return 0;
            object worldObj = GetPublicField<object>(levelInfo, "g\u005ErFwSM");
            if ((object)worldObj == null) return 0;
            try { return (int)worldObj; }
            catch
            {
                try { return (int)(World)worldObj; }
                catch
                {
                    try { return System.Convert.ToInt32(worldObj); }
                    catch { return 0; }
                }
            }
        }

        private static string GetBaseWorldName(int worldInt)
        {
            for (int i = 0; i < _baseWorlds.Length; i++)
            {
                if (_baseWorlds[i] == worldInt)
                    return _baseNames[i];
            }
            return null;
        }

        public static string GetWorldDisplayName(int worldInt)
        {
            string n = GetBaseWorldName(worldInt);
            return !string.IsNullOrEmpty(n) ? n : "World " + worldInt;
        }

        public static bool TryGetWorldIntForName(string worldName, out int worldInt)
        {
            worldInt = 0;
            if (string.IsNullOrEmpty(worldName)) return false;
            string needle = worldName.Trim();
            for (int i = 0; i < _baseNames.Length; i++)
            {
                if (string.Equals(_baseNames[i], needle, System.StringComparison.OrdinalIgnoreCase))
                {
                    worldInt = _baseWorlds[i];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Capture ride map identity for saved spots — works while the sandbox menu is open.
        /// </summary>
        public static bool TryCaptureRideMapIdentity(out MapBookmark bm)
        {
            bm = new MapBookmark();
            if (StatsManager.IsInMenuContext()) return false;

            string scene = GetCurrentSceneName();
            if (IsSystemScene(scene)) return false;

            CacheCurrentLevelSeed();
            bm.Valid = true;
            bm.SceneName = scene;

            if (InWorkshopLevel())
            {
                string modLabel;
                string modPath;
                if (ModWorkshopLoader.TryGetActiveWorkshopModInfo(out modLabel, out modPath)
                    && !string.IsNullOrEmpty(modLabel))
                {
                    bm.Kind = MapBookmarkKind.SceneOnly;
                    bm.DisplayLabel = modLabel;
                    bm.CustomSeed = 0;
                    return true;
                }

                object workshopLevelInfo = TryGetCurrentLevelInfo();
                string workshopLabel = null;
                if ((object)workshopLevelInfo != null)
                {
                    string workshopLevelName = GetPublicField<string>(workshopLevelInfo, "levelName");
                    if (!string.IsNullOrEmpty(workshopLevelName))
                        workshopLabel = PrettyName(workshopLevelName);
                }

                // Prefer full Unity scene id (e.g. "Folk Valley-Okb") over shortened levelName ("Folk Valley").
                if (!string.IsNullOrEmpty(scene)
                    && (string.IsNullOrEmpty(workshopLabel)
                        || scene.Length > workshopLabel.Length
                        || WorkshopSceneNameMatchesLabel(scene, workshopLabel)))
                    workshopLabel = scene;

                if (string.IsNullOrEmpty(workshopLabel))
                    workshopLabel = PrettySceneLabel(scene);

                bm.Kind = MapBookmarkKind.SceneOnly;
                bm.DisplayLabel = workshopLabel;
                bm.CustomSeed = 0;
                return true;
            }

            object levelInfo = TryGetCurrentLevelInfo();
            int customSeed = 0;
            string levelName = null;
            int worldInt = 0;
            if ((object)levelInfo != null)
            {
                customSeed = GetPublicField<int>(levelInfo, "customSeed");
                levelName = GetPublicField<string>(levelInfo, "levelName");
                worldInt = ReadWorldInt(levelInfo);
                bm.WorldInt = worldInt;
            }

            string sceneParkLabel = ResolveLabelFromScene(scene);
            bool namedParkScene = InBikeParkLevel() || InWorkshopLevel()
                || (!string.IsNullOrEmpty(sceneParkLabel) && !IsBaseWorldDisplayName(sceneParkLabel));

            if (customSeed == 0)
            {
                string seedStr = GetCurrentLevelSeed();
                if (!string.IsNullOrEmpty(seedStr)
                    && long.TryParse(seedStr, out long seedLong) && seedLong != 0L)
                {
                    int candidate = (int)seedLong;
                    if (IsKnownMapSeed(candidate))
                        customSeed = candidate;
                }
            }

            string resolved = ResolveRideDisplayLabel(scene, levelName, customSeed);
            if (!string.IsNullOrEmpty(resolved) && !IsBaseWorldDisplayName(resolved))
            {
                bm.DisplayLabel = resolved;
                if (customSeed != 0)
                {
                    bm.CustomSeed = customSeed;
                    bm.Kind = MapBookmarkKind.SeedWorld;
                }
                else
                {
                    FillBookmarkSeedFromLevel(ref bm);
                    if (bm.CustomSeed == 0
                        && TryFindSeedForMapName(resolved, out int parkSeed) && parkSeed != 0)
                        bm.CustomSeed = parkSeed;
                    bm.Kind = bm.CustomSeed != 0
                        ? MapBookmarkKind.SeedWorld
                        : MapBookmarkKind.SceneOnly;
                }
                return true;
            }

            // Procedural free ride (career world + session seed, not a fixed bike park).
            if (customSeed == 0 && worldInt > 0 && !namedParkScene)
            {
                string sessionStr;
                if (TryGetSessionSeedString(out sessionStr))
                {
                    string baseName = GetBaseWorldName(worldInt);
                    bm.Kind = MapBookmarkKind.FreeRideSeed;
                    bm.WorldInt = worldInt;
                    bm.SessionSeed = sessionStr;
                    bm.DisplayLabel = !string.IsNullOrEmpty(baseName) ? baseName : ("World " + worldInt);
                    bm.SessionTypeInt = GetFreerideSandboxSessionTypeInt();
                    return true;
                }
            }

            if (customSeed != 0)
            {
                bm.Kind = MapBookmarkKind.SeedWorld;
                bm.CustomSeed = customSeed;
                bm.DisplayLabel = !string.IsNullOrEmpty(resolved)
                    ? resolved
                    : (!string.IsNullOrEmpty(levelName) ? PrettyName(levelName) : ("Seed " + customSeed));
                return true;
            }

            // Generic career world load (no session seed captured yet).
            if (worldInt > 0 && !namedParkScene)
            {
                string baseName = GetBaseWorldName(worldInt);
                if (!string.IsNullOrEmpty(baseName))
                {
                    bm.Kind = MapBookmarkKind.BaseWorld;
                    bm.DisplayLabel = baseName;
                    FillBookmarkSeedFromLevel(ref bm);
                    return true;
                }
            }

            bm.Kind = MapBookmarkKind.SceneOnly;
            bm.DisplayLabel = !string.IsNullOrEmpty(resolved)
                ? resolved
                : PrettySceneLabel(scene);
            FillBookmarkSeedFromLevel(ref bm);
            if (bm.CustomSeed == 0
                && TryFindSeedForMapName(bm.DisplayLabel, out int sceneParkSeed) && sceneParkSeed != 0)
                bm.CustomSeed = sceneParkSeed;
            if (bm.CustomSeed != 0)
                bm.Kind = MapBookmarkKind.SeedWorld;
            return true;
        }

        /// <summary>True when seed exists in the built-in / bike-park map list (not a stray session seed).</summary>
        public static bool IsKnownMapSeed(int seed)
        {
            if (seed == 0) return false;
            if (_maps.Count == 0)
                BuildMapList();
            for (int i = 0; i < _maps.Count; i++)
            {
                if (_maps[i].CustomSeed == seed)
                    return true;
            }
            return false;
        }

        /// <summary>True when the game can resolve this bike-park seed to level info (FmDOWdg).</summary>
        public static bool CanResolveLevelSeed(int seed)
        {
            if (seed == 0) return false;
            try
            {
                if (!ResolveReflection()) return false;
                object levelInfo = _fmDOWdg.Invoke(null, new object[] { (long)seed });
                return (object)levelInfo != null;
            }
            catch { return false; }
        }

        /// <summary>Find a map list index by display name (e.g. "Mt. Palumbo").</summary>
        public static bool TryFindMapIndexByName(string displayLabel, out int index)
        {
            index = -1;
            if (string.IsNullOrEmpty(displayLabel)) return false;
            if (_maps.Count == 0)
                BuildMapList();

            string normNeedle = NormalizeMapNameForMatch(displayLabel);
            if (string.IsNullOrEmpty(normNeedle)) return false;

            for (int i = 0; i < _maps.Count; i++)
            {
                if (string.Equals(_maps[i].Name, displayLabel.Trim(), System.StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    return true;
                }
                if (NormalizeMapNameForMatch(_maps[i].Name) == normNeedle)
                {
                    index = i;
                    return true;
                }
            }
            return false;
        }

        public enum MapBookmarkKind
        {
            None = 0,
            BaseWorld = 1,
            SeedWorld = 2,
            SceneOnly = 3,
            FreeRideSeed = 4
        }

        public struct MapBookmark
        {
            public bool Valid;
            public MapBookmarkKind Kind;
            public string DisplayLabel;
            public int CustomSeed;
            public int WorldInt;
            public string SceneName;
            /// <summary>Free-ride trail session seed (e.g. 78706).</summary>
            public string SessionSeed;
            /// <summary>rDRSxW session type captured at save (Sandbox=5, etc.).</summary>
            public int SessionTypeInt;
        }

        public static bool TryCaptureCurrentBookmark(out MapBookmark bm)
        {
            bm = new MapBookmark();
            MapLocationContext ctx;
            if (!TryGetMapLocationContext(out ctx) || !ctx.Supported)
                return false;

            bm.Valid = true;
            bm.DisplayLabel = ctx.DisplayLabel;

            if (ctx.StorageKey.StartsWith("scene_", System.StringComparison.Ordinal))
            {
                bm.Kind = MapBookmarkKind.SceneOnly;
                bm.SceneName = GetCurrentSceneName();
                FillBookmarkSeedFromLevel(ref bm);
                return true;
            }

            object levelInfo = TryGetCurrentLevelInfo();
            if ((object)levelInfo != null)
            {
                int customSeed = GetPublicField<int>(levelInfo, "customSeed");
                int worldInt = ReadWorldInt(levelInfo);
                bm.WorldInt = worldInt;

                if (customSeed != 0)
                {
                    bm.Kind = MapBookmarkKind.SeedWorld;
                    bm.CustomSeed = customSeed;
                    return true;
                }

                if (worldInt > 0 && ctx.StorageKey.StartsWith("name_", System.StringComparison.Ordinal))
                {
                    bm.Kind = MapBookmarkKind.BaseWorld;
                    FillBookmarkSeedFromLevel(ref bm);
                    return true;
                }
            }

            if (TryParseSeedFromStorageKey(ctx.StorageKey, out int keySeed))
            {
                bm.Kind = MapBookmarkKind.SeedWorld;
                bm.CustomSeed = keySeed;
                FillBookmarkSeedFromLevel(ref bm);
                return true;
            }

            FillBookmarkSeedFromLevel(ref bm);
            if (bm.CustomSeed != 0)
            {
                bm.Kind = MapBookmarkKind.SeedWorld;
                return true;
            }

            if (bm.WorldInt > 0)
            {
                bm.Kind = MapBookmarkKind.BaseWorld;
                return true;
            }

            return bm.Valid;
        }

        private static void FillBookmarkSeedFromLevel(ref MapBookmark bm)
        {
            try
            {
                object levelInfo = TryGetCurrentLevelInfo();
                if ((object)levelInfo != null)
                {
                    int customSeed = GetPublicField<int>(levelInfo, "customSeed");
                    if (customSeed != 0)
                        bm.CustomSeed = customSeed;
                    int worldInt = ReadWorldInt(levelInfo);
                    if (worldInt > 0)
                        bm.WorldInt = worldInt;
                }

                if (bm.CustomSeed == 0)
                {
                    string seedStr = GetCurrentLevelSeed();
                    if (!string.IsNullOrEmpty(seedStr)
                        && long.TryParse(seedStr, out long seedLong)
                        && seedLong != 0L)
                    {
                        int candidate = (int)seedLong;
                        if (IsKnownMapSeed(candidate))
                            bm.CustomSeed = candidate;
                    }
                }
            }
            catch { }
        }

        public static bool LoadBookmark(MapBookmark bm)
        {
            if (!bm.Valid) return false;

            try
            {
                // Bike-park seeds always win over stale BaseWorld bookmarks (e.g. Bike Out saved with world int).
                if (bm.CustomSeed != 0 && IsBikeParkSeed(bm.CustomSeed))
                    bm.Kind = MapBookmarkKind.SeedWorld;

                if (bm.Kind == MapBookmarkKind.FreeRideSeed)
                {
                    string seed = bm.SessionSeed;
                    if (string.IsNullOrEmpty(seed) && bm.CustomSeed != 0)
                        seed = bm.CustomSeed.ToString();
                    if (!string.IsNullOrEmpty(seed) && LoadFreeRideSeed(seed, bm.WorldInt, bm.SessionTypeInt))
                    {
                        ModLog.Feedback("[Map] Loading free ride sandbox trail " + seed);
                        return true;
                    }
                    if (!IsLikelyFreerideTrailSeed(seed))
                        ModLog.Feedback("[Map] Old full-seed save — re-save spot in free ride.");
                    else
                        ModLog.Feedback("[Map] Could not load free ride seed.");
                    return false;
                }

                if (bm.Kind == MapBookmarkKind.SeedWorld && bm.CustomSeed != 0)
                {
                    BuildMapList();
                    for (int i = 0; i < _maps.Count; i++)
                    {
                        if (_maps[i].CustomSeed == bm.CustomSeed)
                        {
                            GoToMap(i);
                            ModLog.Feedback("[Map] Loading " + _maps[i].Name);
                            return true;
                        }
                    }

                    if (CanResolveLevelSeed(bm.CustomSeed) && LoadFromSeed(bm.CustomSeed.ToString()))
                    {
                        ModLog.Feedback("[Map] Loading seed " + bm.CustomSeed);
                        return true;
                    }

                    int byName;
                    if (TryFindMapIndexByName(bm.DisplayLabel, out byName))
                    {
                        GoToMap(byName);
                        ModLog.Feedback("[Map] Loading " + _maps[byName].Name);
                        return true;
                    }

                    ModLog.Feedback("[Map] Could not load seed " + bm.CustomSeed);
                    return false;
                }

                if (bm.Kind == MapBookmarkKind.BaseWorld && bm.WorldInt > 0)
                {
                    string label = GetWorldDisplayName(bm.WorldInt);
                    DevCommandsGameplay.LoadLevel(bm.WorldInt.ToString());
                    ModLog.Feedback("[Map] Loading " + label);
                    return true;
                }

                if (bm.Kind == MapBookmarkKind.SceneOnly)
                {
                    if (!string.IsNullOrEmpty(bm.DisplayLabel)
                        && ModWorkshopLoader.TryLoadByDisplayName(bm.DisplayLabel))
                        return true;

                    if (ModWorkshopLoader.IsBookmarkSubscribed(bm))
                    {
                        if (ModWorkshopLoader.TryLoadBookmark(bm))
                            return true;

                        string workshopLabel = !string.IsNullOrEmpty(bm.DisplayLabel)
                            ? bm.DisplayLabel : PrettySceneLabel(bm.SceneName);
                        ModLog.Feedback("[Map] Could not load mod.io map \"" + workshopLabel + "\".");
                        return false;
                    }

                    BuildMapList();
                    {
                        string seedLabel = !string.IsNullOrEmpty(bm.DisplayLabel)
                            ? bm.DisplayLabel : PrettySceneLabel(bm.SceneName);
                        if (TryFindSeedForMapName(seedLabel, out int sk) && sk != 0)
                            bm.CustomSeed = sk;
                        else if (!string.IsNullOrEmpty(bm.SceneName)
                            && TryFindSeedForMapName(PrettySceneLabel(bm.SceneName), out sk) && sk != 0)
                            bm.CustomSeed = sk;
                    }

                    if (bm.CustomSeed != 0)
                    {
                        bm.Kind = MapBookmarkKind.SeedWorld;
                        for (int i = 0; i < _maps.Count; i++)
                        {
                            if (_maps[i].CustomSeed == bm.CustomSeed)
                            {
                                GoToMap(i);
                                ModLog.Feedback("[Map] Loading " + _maps[i].Name);
                                return true;
                            }
                        }
                        if (LoadFromSeed(bm.CustomSeed.ToString()))
                        {
                            ModLog.Feedback("[Map] Loading seed " + bm.CustomSeed);
                            return true;
                        }
                    }

                    string label = !string.IsNullOrEmpty(bm.DisplayLabel)
                        ? bm.DisplayLabel : PrettySceneLabel(bm.SceneName);
                    ModLog.Feedback("[Map] Could not load map \"" + label + "\".");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                string detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MelonLogger.Error("[MapChanger] LoadBookmark: " + detail);
                Telemetry.ReportErrorAsync(ex, "MapChanger.LoadBookmark");
            }

            return false;
        }

        private static string FindMapNameForSeed(int seed)
        {
            if (seed == 0) return null;
            for (int i = 0; i < _maps.Count; i++)
            {
                if (_maps[i].CustomSeed == seed)
                    return _maps[i].Name;
            }
            return null;
        }

        public static string FindMapNameForSeedPublic(int seed)
        {
            if (_maps.Count == 0)
                BuildMapList();
            return FindMapNameForSeed(seed);
        }

        /// <summary>Resolve bike-park / bonus seed from a map display name (e.g. "mt palumbo" → 26305).</summary>
        public static bool IsBikeParkSeed(int seed)
        {
            if (seed <= 0) return false;
            if (_maps.Count == 0)
                BuildMapList();
            for (int i = 0; i < _maps.Count; i++)
            {
                if (_maps[i].IsBikePark && _maps[i].CustomSeed == seed)
                    return true;
            }
            return false;
        }

        public static bool TryFindSeedForMapName(string mapName, out int seed)
        {
            seed = 0;
            if (string.IsNullOrEmpty(mapName)) return false;
            if (_maps.Count == 0)
                BuildMapList();

            string normNeedle = NormalizeMapNameForMatch(mapName);
            if (string.IsNullOrEmpty(normNeedle)) return false;

            int fallback = 0;
            for (int i = 0; i < _maps.Count; i++)
            {
                if (_maps[i].CustomSeed <= 0) continue;
                if (string.Equals(_maps[i].Name, mapName.Trim(), System.StringComparison.OrdinalIgnoreCase))
                {
                    seed = _maps[i].CustomSeed;
                    return true;
                }
                string normEntry = NormalizeMapNameForMatch(_maps[i].Name);
                if (normEntry == normNeedle)
                {
                    if (CanResolveLevelSeed(_maps[i].CustomSeed))
                    {
                        seed = _maps[i].CustomSeed;
                        return true;
                    }
                    if (fallback == 0)
                        fallback = _maps[i].CustomSeed;
                }
            }

            for (int i = 0; i < _maps.Count; i++)
            {
                if (_maps[i].CustomSeed <= 0) continue;
                string normEntry = NormalizeMapNameForMatch(_maps[i].Name);
                if (normEntry.Length < 4 || normNeedle.Length < 4) continue;
                if (normEntry.Contains(normNeedle) || normNeedle.Contains(normEntry))
                {
                    if (CanResolveLevelSeed(_maps[i].CustomSeed))
                    {
                        seed = _maps[i].CustomSeed;
                        return true;
                    }
                    if (fallback == 0)
                        fallback = _maps[i].CustomSeed;
                }
            }

            if (fallback != 0)
            {
                seed = fallback;
                return true;
            }

            return false;
        }

        private static string NormalizeMapNameForMatch(string raw)
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

        public static bool TryParseSeedFromStorageKey(string storageKey, out int seed)
        {
            seed = 0;
            if (string.IsNullOrEmpty(storageKey) || !storageKey.StartsWith("seed_", System.StringComparison.Ordinal))
                return false;
            return int.TryParse(storageKey.Substring(5), out seed) && seed != 0;
        }

        private static bool IsLikelyFreerideTrailSeed(string seed)
        {
            return IsLikelyFreerideShareSeed(seed);
        }

        /// <summary>Load a procedural / build-it-yourself free-ride map from its saved seed.</summary>
        public static bool LoadFreeRideSeed(string sessionSeed, int worldInt, int sessionTypeInt)
        {
            if (string.IsNullOrEmpty(sessionSeed)) return false;
            if (!ResolveReflection()) return false;
            return DispatchFreerideTrailLoad(sessionSeed.Trim(), worldInt, sessionTypeInt);
        }

        private static bool DispatchFreerideTrailLoad(
            string seedLabel, int fallbackWorld, int sessionTypeInt)
        {
            if (string.IsNullOrEmpty(seedLabel)) return false;
            if (!ResolveReflection()) return false;

            string loadSeed = NormalizeFreerideLoadSeed(seedLabel, fallbackWorld);
            if (string.IsNullOrEmpty(loadSeed)) return false;

            _suppressTimer = 5f;
            SuppressInactivityWarning();
            ResetPlayersForMapLoad();

            int sandboxTypeInt = GetFreerideSandboxSessionTypeInt();

            if (DispatchStartNewSessionFromSeedString(loadSeed, sandboxTypeInt))
            {
                ModLog.Debug("[MapChanger] Freeride load \"" + loadSeed + "\"");
                return true;
            }

            if (DispatchFreerideSeedManual(loadSeed, sandboxTypeInt))
                return true;

            ModLog.Debug("[MapChanger] Freeride load failed for \"" + loadSeed + "\"");
            return false;
        }

        private static bool DispatchSandboxWorldWithSeed(int worldInt, int seedInt, string seedLabel)
        {
            return DispatchFreerideTrailLoad(seedLabel, worldInt, 0);
        }

        public static bool PushGeneratingStatePublic()
        {
            return TryPushGeneratingState();
        }

        public static void CloseCurrentSessionPublic()
        {
            object smInstance = GetSingleton(typeof(SessionManager));
            TryCloseCurrentSession(smInstance);
        }

        public static void SuppressInactivityWarningPublic()
        {
            SuppressInactivityWarning();
        }

        private static bool TryPushGeneratingState()
        {
            try
            {
                if (!ResolveReflection()) return false;
                object stInstance = GetSingleton(typeof(StateMachine));
                if ((object)stInstance == null || (object)_pushState == null || (object)_vtGenerating == null)
                    return false;
                _pushState.Invoke(stInstance, new object[] { _vtGenerating });
                return true;
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[MapChanger] TryPushGeneratingState: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Load a freeride world from a seed string. Returns false if the seed
        /// is invalid / unknown (caller can show UI feedback).
        /// </summary>
        public static bool LoadFromSeed(string seed)
        {
            try
            {
                if (string.IsNullOrEmpty(seed) || string.IsNullOrEmpty(seed.Trim())) return false;
                string trimmed = seed.Trim();
                if (!ResolveReflection()) return false;

                long seedNum;
                string[] parts = trimmed.Split('-');
                bool hasNumeric = long.TryParse(parts[0].Trim(), out seedNum) && seedNum != 0L;

                if (IsLikelyFreerideShareSeed(trimmed))
                {
                    _suppressTimer = 5f;
                    SuppressInactivityWarning();
                    ResetPlayersForMapLoad();
                    if (DispatchFreerideTrailLoad(trimmed, 0, 0))
                    {
                        ModLog.Debug("[MapChanger] LoadFromSeed: freeride \"" + trimmed + "\"");
                        return true;
                    }
                }

                object levelInfo = null;
                if (hasNumeric)
                    levelInfo = _fmDOWdg.Invoke(null, new object[] { seedNum });

                if ((object)levelInfo != null)
                    return DispatchLevelInfoLoad(trimmed, levelInfo);

                _suppressTimer = 5f;
                SuppressInactivityWarning();
                ResetPlayersForMapLoad();

                if (DispatchStartNewSessionFromSeedString(trimmed, GetFreerideSandboxSessionTypeInt()))
                {
                    ModLog.Debug("[MapChanger] LoadFromSeed: StartNewSession \"" + trimmed + "\"");
                    return true;
                }

                if (TryDispatchDevLoadLevelFromSeed(trimmed))
                {
                    ModLog.Debug("[MapChanger] LoadFromSeed: dev \"" + trimmed + "\"");
                    return true;
                }

                if (IsLikelyFreerideShareSeed(trimmed)
                    && DispatchFreerideTrailLoad(trimmed, 0, 0))
                {
                    ModLog.Debug("[MapChanger] LoadFromSeed: freeride retry \"" + trimmed + "\"");
                    return true;
                }

                if (!hasNumeric)
                {
                    ModLog.Debug("[MapChanger] LoadFromSeed: not a number: \"" + trimmed + "\"");
                    return false;
                }

                ModLog.Debug("[MapChanger] LoadFromSeed: unknown seed=" + seedNum);
                return false;
            }
            catch (System.Exception ex)
            {
                string detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MelonLogger.Error("[MapChanger] LoadFromSeed: " + detail);
                Telemetry.ReportErrorAsync(ex, "MapChanger");
                return false;
            }
        }

        private static bool TryGuessWorldForSeed(string seed, out int worldInt)
        {
            worldInt = 0;
            object levelInfo = TryGetCurrentLevelInfo();
            if ((object)levelInfo != null)
            {
                worldInt = ReadWorldInt(levelInfo);
                if (worldInt > 0) return true;
            }
            worldInt = 1;
            return true;
        }

        private static bool TryDispatchDevLoadLevelFromSeed(string seed)
        {
            return DispatchStartNewSessionFromSeedString(
                seed, GetFreerideSandboxSessionTypeInt());
        }

        private static void ResetPlayersForMapLoad()
        {
            try
            {
                object pip = GetSingleton(typeof(PlayerManager));
                if ((object)pip == null) return;
                var getAllImpact = typeof(PlayerManager).GetMethod("GetAllPlayersImpact",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)getAllImpact == null) return;
                var players = getAllImpact.Invoke(pip, null) as System.Array;
                if ((object)players == null) return;
                System.Reflection.FieldInfo hcqFld = null;
                foreach (object player in players)
                {
                    if ((object)player == null) continue;
                    if ((object)hcqFld == null)
                        hcqFld = player.GetType().GetField(
                            "HCqxy",
                            BindingFlags.Public | BindingFlags.Instance);
                    if ((object)hcqFld != null)
                        hcqFld.SetValue(player, -1);
                }
            }
            catch { }
        }

        private static bool DispatchLevelInfoLoad(string seed, object levelInfo)
        {
            System.Reflection.FieldInfo worldFld = levelInfo.GetType().GetField(
                "g\u005ErFwSM", BindingFlags.Public | BindingFlags.Instance);
            if ((object)worldFld == null)
            {
                MelonLogger.Error("[MapChanger] LoadFromSeed: g^ErFwSM field not found.");
                Telemetry.ReportErrorAsync(new System.Exception("[MapChanger] LoadFromSeed: g^ErFwSM field not found."), "MapChanger");
                return false;
            }
            object world = worldFld.GetValue(levelInfo);

            object smInstance = GetSingleton(typeof(SessionManager));
            object stInstance = GetSingleton(typeof(StateMachine));
            if ((object)smInstance == null || (object)stInstance == null)
            {
                MelonLogger.Error("[MapChanger] LoadFromSeed: SessionManager or StateMachine null.");
                Telemetry.ReportErrorAsync(new System.Exception("[MapChanger] LoadFromSeed: SessionManager or StateMachine null."), "MapChanger");
                return false;
            }

            _suppressTimer = 5f;
            SuppressInactivityWarning();
            ResetPlayersForMapLoad();

            _startNewSession.Invoke(smInstance,
                new object[] { (World)world, _sandboxValue, -1, null });

            object sessionData = _sessionDataFld.GetValue(smInstance);
            if ((object)sessionData == null)
            {
                MelonLogger.Error("[MapChanger] LoadFromSeed: session data null after StartNewSession.");
                Telemetry.ReportErrorAsync(new System.Exception("[MapChanger] LoadFromSeed: session data null after StartNewSession."), "MapChanger");
                return false;
            }
            _currentLevelFld.SetValue(sessionData, levelInfo);

            SuppressInactivityWarning();
            _pushState.Invoke(stInstance, new object[] { _vtGenerating });

            LastLoadedSeed = seed;
            ModLog.Debug("[MapChanger] LoadFromSeed: \"" + seed + "\" world=" + world + " (level info)");
            return true;
        }

        public static bool IsOnOverworldWorld()
        {
            try
            {
                SessionManager sm = UnityEngine.Object.FindObjectOfType<SessionManager>();
                if ((object)sm == null) return false;
                MethodInfo getWorld = typeof(SessionManager).GetMethod(
                    "GetWorld", BindingFlags.Public | BindingFlags.Instance);
                if ((object)getWorld == null) return false;
                object w = getWorld.Invoke(sm, null);
                if (w != null)
                {
                    try { return (World)w == World.Overworld; }
                    catch { return System.Convert.ToInt32(w) == (int)World.Overworld; }
                }
                return false;
            }
            catch { return false; }
        }

        private static MethodInfo _sessionStartedMethod = null;

        /// <summary>True when SessionManager reports an active ride (not spawn preview).</summary>
        public static bool IsSessionStarted()
        {
            try
            {
                SessionManager sm = UnityEngine.Object.FindObjectOfType<SessionManager>();
                if ((object)sm == null) return false;
                if ((object)_sessionStartedMethod == null)
                {
                    _sessionStartedMethod = typeof(SessionManager).GetMethod(
                        "SessionStarted",
                        BindingFlags.Public | BindingFlags.Instance);
                }
                if ((object)_sessionStartedMethod == null) return false;
                object result = _sessionStartedMethod.Invoke(sm, null);
                if (result != null)
                {
                    try { return (bool)result; }
                    catch { }
                }
                return false;
            }
            catch { return false; }
        }

        /// <summary>Deprecated — forcing Sandbox state spammed crew/mod UI and tanked FPS.</summary>
        public static bool TryContinueSandboxSpawn()
        {
            return IsSessionStarted() && StatsManager.IsRidingHudActive();
        }

    }
}

