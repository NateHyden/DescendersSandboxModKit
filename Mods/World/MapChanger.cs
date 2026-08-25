using DescendersModMenu;
using MelonLoader;
using UnityEngine;
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
        private static MethodInfo _startNewSession = null;
        private static FieldInfo _sessionDataFld = null;
        private static FieldInfo _currentLevelFld = null;
        private static MethodInfo _pushState = null;
        private static object _vtGenerating = null;
        private static object _vtSandbox = null;

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
            }
            return System.Globalization.CultureInfo.CurrentCulture
                .TextInfo.ToTitleCase(key.ToLowerInvariant());
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
                    new object[] { (World)map.WorldInt, _sandboxValue, -1, null });

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
                    if (p.Length != 4) continue;
                    if (!string.Equals(p[0].ParameterType.FullName,
                            typeof(World).FullName, System.StringComparison.Ordinal)) continue;
                    if (!p[1].ParameterType.IsEnum) continue;
                    if (!string.Equals(p[2].ParameterType.FullName,
                            typeof(int).FullName, System.StringComparison.Ordinal)) continue;
                    if (System.Enum.IsDefined(p[1].ParameterType, "Sandbox"))
                    {
                        _startNewSession = m;
                        _rDRSType = p[1].ParameterType;
                        _sandboxValue = System.Enum.Parse(_rDRSType, "Sandbox");
                        break;
                    }
                }
                if ((object)_startNewSession == null)
                {
                    MelonLogger.Error("[MapChanger] StartNewSession(World,sessionType) not found.");
                    Telemetry.ReportErrorAsync(new System.Exception("[MapChanger] StartNewSession(World,sessionType) not found."), "MapChanger");
                    return false;
                }

                _sessionDataFld = typeof(SessionManager).GetField(
                    "ESVMoz",
                    BindingFlags.Public | BindingFlags.Instance);

                if ((object)_sessionDataFld != null)
                {
                    _currentLevelFld = _sessionDataFld.FieldType.GetField(
                        "vebfkn",
                        BindingFlags.Public | BindingFlags.Instance);
                }

                if ((object)_sessionDataFld == null || (object)_currentLevelFld == null)
                {
                    MelonLogger.Error("[MapChanger] Session data fields not found.");
                    Telemetry.ReportErrorAsync(new System.Exception("[MapChanger] Session data fields not found."), "MapChanger");
                    return false;
                }

                _pushState = typeof(StateMachine).GetMethod("PushState",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)_pushState != null)
                {
                    var vtType = _pushState.GetParameters()[0].ParameterType;
                    _vtGenerating = System.Enum.Parse(vtType, "Generating");
                    if (System.Enum.IsDefined(vtType, "Sandbox"))
                        _vtSandbox = System.Enum.Parse(vtType, "Sandbox");
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

        public static void OnSceneInitialized() { CacheCurrentLevelSeed(); }

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
                        if (r is int) return (int)r;
                        if ((object)r != null) return System.Convert.ToInt32(r);
                    }
                }
            }
            catch { }
            return 0;
        }

        // ── Read live session seed ────────────────────────────────────
        private static MethodInfo _seedGetterMethod = null;
        private static string _cachedSeedString = "";

        public static void CacheCurrentLevelSeed()
        {
            _cachedSeedString = "";
            _seedGetterMethod = null;
            try
            {
                SessionManager sm = UnityEngine.Object.FindObjectOfType<SessionManager>();
                if ((object)sm == null) return;

                if ((object)_sessionDataFld == null || (object)_currentLevelFld == null)
                    ResolveReflection();
                if ((object)_sessionDataFld == null || (object)_currentLevelFld == null) return;

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

        /// <summary>
        /// Load a freeride world from a seed string. Returns false if the seed
        /// is invalid / unknown (caller can show UI feedback).
        /// </summary>
        public static bool LoadFromSeed(string seed)
        {
            try
            {
                if (!ResolveReflection()) return false;

                long seedNum;
                string[] parts = seed.Split('-');
                if (!long.TryParse(parts[0].Trim(), out seedNum))
                {
                    ModLog.Debug("[MapChanger] LoadFromSeed: not a number: \"" + seed + "\"");
                    return false;
                }

                object levelInfo = _fmDOWdg.Invoke(null, new object[] { seedNum });
                if ((object)levelInfo == null)
                {
                    ModLog.Debug("[MapChanger] LoadFromSeed: unknown seed=" + seedNum);
                    return false;
                }

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

                try
                {
                    object pip = GetSingleton(typeof(PlayerManager));
                    if ((object)pip != null)
                    {
                        var getAllImpact = typeof(PlayerManager).GetMethod("GetAllPlayersImpact",
                            BindingFlags.Public | BindingFlags.Instance);
                        if ((object)getAllImpact != null)
                        {
                            var players = getAllImpact.Invoke(pip, null) as System.Array;
                            if ((object)players != null)
                            {
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
                        }
                    }
                }
                catch { }

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
                ModLog.Debug("[MapChanger] LoadFromSeed: \"" + seed + "\" world=" + world + " (Sandbox/freeride)");
                return true;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[MapChanger] LoadFromSeed: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "MapChanger");
                return false;
            }
        }

    }
}

