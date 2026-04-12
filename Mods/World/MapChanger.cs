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
            public int CustomSeed; // -1 = base world
            public int WorldInt;
            public bool IsBikePark;
        }

        private static readonly List<MapEntry> _maps = new List<MapEntry>();
        public static int Count => _maps.Count;
        public static string GetName(int i) => _maps[i].Name;
        public static MapEntry GetEntry(int i) => _maps[i];
        public static bool HasBikeParks { get; private set; } = false;

        // Base game worlds
        private static readonly string[] _baseNames =
            { "Highlands","Forest","Canyon","Peaks","Hell","Desert","Jungle","Favela","Glaciers","Ridges" };
        private static readonly int[] _baseWorlds =
            { 1, 2, 3, 4, 5, 6, 7, 8, 9, 11 };

        // ── Reflection cache ──────────────────────────────────────────
        private static System.Type _wiWlGzType = null; // \u0081wiWlGz
        private static MethodInfo _fmDOWdg = null; // FmDOWdg(long)
        private static System.Type _rDRSType = null; // r}RS (session type enum)
        private static object _sandboxValue = null; // r}RS.Sandbox
        private static MethodInfo _startNewSession = null; // StartNewSession(World, r}RS)
        private static FieldInfo _sessionDataFld = null; // 83ESVMoz on SessionManager
        private static FieldInfo _currentLevelFld = null; // vebf81kn on session data
        private static MethodInfo _pushState = null; // StateMachine.PushState(Vt)
        private static object _vtGenerating = null; // Vt.Generating
        private static object _vtSandbox = null; // Vt.Sandbox

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
                object gd = GetSingleton(typeof(GameData));
                if ((object)gd == null) { MelonLogger.Warning("[MapChanger] No GameData."); return; }

                int found = 0;

                // Scan FqVmLOT (BonusLevelInfo[]) — the definitive bike park list
                System.Array fqv = GetPublicField<System.Array>(gd, "FqVmLOT");
                if ((object)fqv != null && fqv.Length > 0)
                {
                    foreach (var b in fqv)
                    {
                        if ((object)b == null) continue;
                        string name = GetPublicField<string>(b, "levelName");
                        int seed = GetPublicField<int>(b, "customSeed");
                        object we = GetPublicField<object>(b, "world");
                        int world = we != null ? (int)we : 0;
                        if (string.IsNullOrEmpty(name) || seed == 0) continue;
                        // Pretty-print the internal key e.g. BIKEOUTV2 → Bike Out V2
                        _maps.Add(new MapEntry
                        {
                            Name = PrettyName(name),
                            CustomSeed = seed,
                            WorldInt = world,
                            IsBikePark = true
                        });
                        found++;
                    }
                }

                HasBikeParks = found > 0;
                MelonLogger.Msg("[MapChanger] " + found + " bike parks + 10 base worlds = "
                    + _maps.Count + " total.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[MapChanger] BuildMapList: " + ex.Message); }
        }

        // Convert internal ALL_CAPS keys to Title Case display names
        // e.g. BIKEOUTV2 → Bike Out V2,  MTPALUMBO → Mt. Palumbo
        private static string PrettyName(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            // Known remaps
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
            // Fallback: title-case the raw key
            return System.Globalization.CultureInfo.CurrentCulture
                .TextInfo.ToTitleCase(key.ToLowerInvariant());
        }

        // ── Deferred load ─────────────────────────────────────────────
        private static int _pendingLoad = -1;
        private static float _loadTimer = 0f;
        private static int _scoreToRestore = 0;
        private static float _suppressTimer = 0f; // keeps Last Stand suppressed after seed load
        public static string LastLoadedSeed { get; private set; } = "";
        private static float _restoreTimer = 0f;

        public static void GoToMap(int index)
        {
            if (index < 0 || index >= _maps.Count) return;
            MelonLogger.Msg("[MapChanger] Queuing: " + _maps[index].Name);
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
                        MelonLogger.Msg("[MapChanger] Restored " + _scoreToRestore + " REP.");
                    }
                    catch (System.Exception ex) { MelonLogger.Error("[MapChanger] RestoreScore: " + ex.Message); }
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
                    // Try LoadLevel with world only (no -1 level suffix).
                    // "5-1" = career level 1 of Hell. "5" alone may load freeride.
                    // Also try LoadLevelFromSeed with the world index string.
                    SuppressInactivityWarning();
                    string worldStr = map.WorldInt.ToString();
                    MelonLogger.Msg("[MapChanger] Base world load attempt: worldStr=" + worldStr);
                    // Attempt 1: LoadLevel with just world index (no career suffix)
                    DevCommandsGameplay.LoadLevel(worldStr);
                    MelonLogger.Msg("[MapChanger] Base world: LoadLevel(" + worldStr + ") called");
                    return;
                }

                // ── Bike park load ────────────────────────────────────
                // Replicates BikeParkSelectButton.OnClick():
                // 1. SessionManager.StartNewSession(world, Sandbox)
                // 2. session.currentLevel = wiWlGz.FmDOWdg((long)customSeed)
                // 3. StateMachine.PushState(Vt.Generating)

                MelonLogger.Msg("[MapChanger] Bike park: " + map.Name
                    + " seed=" + map.CustomSeed + " world=" + map.WorldInt);

                // Resolve reflection cache
                if (!ResolveReflection()) return;

                // Get singleton instances
                object smInstance = GetSingleton(typeof(SessionManager));
                object stInstance = GetSingleton(typeof(StateMachine));
                if ((object)smInstance == null || (object)stInstance == null)
                {
                    MelonLogger.Error("[MapChanger] SessionManager or StateMachine null.");
                    return;
                }

                SuppressInactivityWarning();

                // BikeParkSelectButton always sets HCq84\u0083xy = -1 before loading.
                // Without this, ResetPlayer (called by StartNewSession) sets obpDRQw=0
                // because the session is Sandbox, making IsLastStand() true and
                // triggering the "LAST STAND" banner animation.
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

                // 1. StartNewSession(world, Sandbox, -1, null) — must pass all 4 params,
                //    reflection doesn't honour C# default parameter values
                _startNewSession.Invoke(smInstance,
                    new object[] { (World)map.WorldInt, _sandboxValue, -1, null });

                // 2. Get session data object (83ESVMoz field)
                object sessionData = _sessionDataFld.GetValue(smInstance);
                if ((object)sessionData == null)
                {
                    MelonLogger.Error("[MapChanger] Session data null after StartNewSession.");
                    return;
                }

                // 3. Create level info: wiWlGz.FmDOWdg((long)customSeed)
                object levelInfo = _fmDOWdg.Invoke(null, new object[] { (long)map.CustomSeed });
                if ((object)levelInfo == null)
                {
                    MelonLogger.Error("[MapChanger] FmDOWdg returned null for seed=" + map.CustomSeed);
                    return;
                }

                // 4. Set session.currentLevel = levelInfo
                _currentLevelFld.SetValue(sessionData, levelInfo);

                // 5. StateMachine.PushState(Vt.Generating)
                _pushState.Invoke(stInstance, new object[] { _vtGenerating });

                MelonLogger.Msg("[MapChanger] Bike park load dispatched.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[MapChanger] ExecuteLoad: " + ex.Message); }
        }

        private static void SuppressInactivityWarning()
        {
            // The "LAST STAND" banner fires because the inactivity timer (murgZZE)
            // keeps running during the gap between StartNewSession and the Generating
            // state activating. Fix: reset murgZZE = 0 and kVhi84yF = now so the
            // elapsed-time condition (murgZZE > gd82zUG5D * 0.75) is never met.
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

                // Also hide it immediately in case it's already visible
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
            if ((object)_fmDOWdg != null) return true; // already cached

            try
            {
                // Find \u0081wiWlGz type (level info class)
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!string.Equals(asm.GetName().Name, "Assembly-CSharp",
                        System.StringComparison.Ordinal)) continue;
                    _wiWlGzType = asm.GetType("\u0081wiWlGz");
                    if ((object)_wiWlGzType == null)
                    {
                        // Try all types to find it by its known static method
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
                    return false;
                }

                _fmDOWdg = _wiWlGzType.GetMethod("Fm\u007DOWd\u0060",
                    BindingFlags.Public | BindingFlags.Static,
                    null, new System.Type[] { typeof(long) }, null);

                // Find StartNewSession(World, r}RS, int, List<GameModifier>) on SessionManager
                // Derive session-type enum from parameter — avoids picking Vt (also has Sandbox)
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
                    return false;
                }

                // Session data field on SessionManager
                _sessionDataFld = typeof(SessionManager).GetField(
                    "ESVMoz",
                    BindingFlags.Public | BindingFlags.Instance);

                // Current level field on session data type
                if ((object)_sessionDataFld != null)
                {
                    _currentLevelFld = _sessionDataFld.FieldType.GetField(
                        "vebfkn",
                        BindingFlags.Public | BindingFlags.Instance);
                }

                if ((object)_sessionDataFld == null || (object)_currentLevelFld == null)
                {
                    MelonLogger.Error("[MapChanger] Session data fields not found.");
                    return false;
                }

                // Vt.Generating — the state machine enum value
                // PushState(Vt) on StateMachine
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
                    return false;
                }

                MelonLogger.Msg("[MapChanger] Reflection resolved OK. "
                    + "wiWlGz=" + _wiWlGzType.Name
                    + " sessionType=" + _rDRSType.Name);
                return true;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[MapChanger] ResolveReflection: " + ex.Message);
                return false;
            }
        }

        public static void OnSceneInitialized() { CacheCurrentLevelSeed(); }

        // ── Harmony patch — fires when UI_FreerideBikeParks.OnEnable runs ──
        // OnEnable reads GameData.Mg^xbWZ directly, so by the time it fires
        // the bike park bundle data is guaranteed to be populated.
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
                    MelonLogger.Warning("[MapChanger] UI_FreerideBikeParks.OnEnable not found.");
                    return;
                }
                var postfix = typeof(MapChanger).GetMethod(
                    "Patch_FreerideBikeParksRefresh",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                harmony.Patch(target, postfix: new HarmonyLib.HarmonyMethod(postfix));
                MelonLogger.Msg("[MapChanger] UI_FreerideBikeParks.OnEnable patch applied.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning("[MapChanger] Patch failed: " + ex.Message);
            }
        }

        // Called by Harmony after State_FreerideBikeParks.Refresh()
        public static void Patch_FreerideBikeParksRefresh()
        {
            if (HasBikeParks) return; // already scanned — no need to repeat
            MelonLogger.Msg("[MapChanger] Freeride screen opened — scanning bike parks...");
            BuildMapList();
            try { MapPage.RebuildList(); } catch { }
        }

        // ── Generic helpers ───────────────────────────────────────────
        private static object GetSingleton(System.Type targetType)
        {
            try
            {
                // First: check for direct public static field [~qsVD| on the type itself
                // (GameData, SessionManager etc. use this pattern)
                var directField = targetType.GetField(
                    "[~qsVD|",
                    BindingFlags.Public | BindingFlags.Static);
                if ((object)directField != null)
                {
                    object val = directField.GetValue(null);
                    if ((object)val != null) return val;
                }

                // Fallback: Singleton<T> wrapper
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
        // Reads SessionManager.\u0083ESVMoz.vebf\u0081kn.digk\u0084\u007FK() — the actual seed
        // used to generate the current map, regardless of how it was loaded.
        private static MethodInfo _seedGetterMethod = null;
        private static string _cachedSeedString = "";

        // Called once from ModEntry.OnSceneWasInitialized — reads and caches the
        // seed for the newly loaded map. Zero per-frame cost.
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

                // Find digk\u0084\u007FK — the only public no-arg method returning long
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
                    MelonLogger.Msg("[MapChanger] Cached map seed: " + _cachedSeedString);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[MapChanger] CacheCurrentLevelSeed: " + ex.Message);
            }
        }

        public static string GetCurrentLevelSeed() { return _cachedSeedString; }
        // Uses the exact same path as bike park loading — proven to work
        // without Last Stand. Parses seed string -> FmDOWdg -> StartNewSession(World, Sandbox)
        public static void LoadFromSeed(string seed)
        {
            try
            {
                if (!ResolveReflection()) return;

                // Parse seed string to long (take first segment before any dash)
                long seedNum;
                string[] parts = seed.Split('-');
                if (!long.TryParse(parts[0].Trim(), out seedNum))
                { MelonLogger.Error("[MapChanger] LoadFromSeed: could not parse \"" + seed + "\" as a number."); return; }

                // Convert seed to level info via FmDOWdg
                object levelInfo = _fmDOWdg.Invoke(null, new object[] { seedNum });
                if ((object)levelInfo == null)
                { MelonLogger.Error("[MapChanger] LoadFromSeed: FmDOWdg returned null for seed=" + seedNum); return; }

                // Get the World from levelInfo (g^ErFwSM public field)
                System.Reflection.FieldInfo worldFld = levelInfo.GetType().GetField(
                    "g\u005ErFwSM", BindingFlags.Public | BindingFlags.Instance);
                if ((object)worldFld == null)
                { MelonLogger.Error("[MapChanger] LoadFromSeed: g^ErFwSM field not found."); return; }
                object world = worldFld.GetValue(levelInfo);

                object smInstance = GetSingleton(typeof(SessionManager));
                object stInstance = GetSingleton(typeof(StateMachine));
                if ((object)smInstance == null || (object)stInstance == null)
                { MelonLogger.Error("[MapChanger] LoadFromSeed: SessionManager or StateMachine null."); return; }

                _suppressTimer = 5f;
                SuppressInactivityWarning();

                // Set HCqxy = -1 on ALL players before StartNewSession.
                // StartNewSession loops GetAllPlayersImpact() and calls ResetPlayer() on each.
                // ResetPlayer: if session is Sandbox, sets obpDRQw = HCqxy.
                // HCqxy defaults to 0 -> obpDRQw=0 -> IsLastStand()=true -> LAST STAND banner.
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

                // StartNewSession(World, Sandbox, -1, null) — identical to bike park path
                _startNewSession.Invoke(smInstance,
                    new object[] { (World)world, _sandboxValue, -1, null });

                // Set session currentLevel to the seed-derived level info
                object sessionData = _sessionDataFld.GetValue(smInstance);
                if ((object)sessionData == null)
                { MelonLogger.Error("[MapChanger] LoadFromSeed: session data null after StartNewSession."); return; }
                _currentLevelFld.SetValue(sessionData, levelInfo);

                SuppressInactivityWarning();

                // Push Generating state to start the world load
                _pushState.Invoke(stInstance, new object[] { _vtGenerating });

                LastLoadedSeed = seed;
                MelonLogger.Msg("[MapChanger] LoadFromSeed: \"" + seed + "\" world=" + world + " (Sandbox/freeride)");
            }
            catch (System.Exception ex)
            { MelonLogger.Error("[MapChanger] LoadFromSeed: " + ex.Message); }
        }

    }
}