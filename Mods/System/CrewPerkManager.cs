using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using DescendersModMenu.BikeStats;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Freeride crew / GameModifier perks — same assets the vanilla crew pick uses.
    /// Selected perks are passed into workshop session start and re-applied on arrival
    /// so top-left banners show via PlayerInfoImpact.AddGameModifier.
    /// </summary>
    public static class CrewPerkManager
    {
        public struct PerkEntry
        {
            public string Id;
            public string Label;
            public GameModifier Modifier;
        }

        private static readonly Dictionary<string, string> KnownPerkLabels = new Dictionary<string, string>
        {
            { "SPEEDWOBBLES", "Speed Wobbles" },
            { "WHEELIEBALANCE", "Wheelie Balance" },
            { "AIRCORRECTION", "Air Correction" },
            { "FAKIEBALANCE", "Fakie Balance" },
            { "PUMPSTRENGTH", "Pump Strength" },
            { "TWEAKSPEED", "Tweak Speed" },
            { "OFFROADFRICTION", "Offroad Friction" },
            { "LESSCURVES", "Less Curves" },
            { "MORECHECKPOINTS", "More Checkpoints" },
            { "PREVENTMODIFIERS", "Prevent Modifiers" },
            { "SHOWCOMPASS", "Show Compass" },
            { "SPINSPEED", "Spin Speed" },
            { "LESSOBSTACLES", "Less Obstacles" },
            { "MORETEAMNODES", "More Team Nodes" },
            { "SCOUTNODES", "Scout Nodes" },
            { "SMOOTHERCURVES", "Smoother Curves" },
            { "NOSPEEDWOBBLES", "No Speed Wobbles" },
            { "NOBAIL", "No Bail" },
            { "BOSSES", "Bosses" },
            { "CURVEBOSSES", "Curve Bosses" },
            { "STEEPBOSSES", "Steep Bosses" },
            { "JUMPS", "Jumps" },
            { "BIGJUMPS", "Big Jumps" },
            { "SMALLJUMPS", "Small Jumps" },
            { "STEEP", "Steep" },
            { "FLAT", "Flat" },
            { "CURVY", "Curvy" },
            { "STRAIGHT", "Straight" },
            { "NARROW", "Narrow" },
            { "WIDE", "Wide" },
            { "ROUGH", "Rough" },
            { "SMOOTH", "Smooth" },
            { "FAST", "Fast" },
            { "SLOW", "Slow" },
            { "HIGH", "High" },
            { "LOW", "Low" },
            { "EXTRASTUNTS", "Extra Stunts" },
            { "HEAVYBAILTHRESHOLD", "Heavy Bail Threshold" },
            { "BROADERPATH", "Broader Path" },
            { "EXTRASTEEPNESS", "Extra Steepness" },
            { "LANDINGIMPACT", "Landing Impact" },
            { "BUNNYHOP", "Bunny Hop" },
            { "MORECURVES", "More Curves" },
            { "MOREOBSTACLES", "More Obstacles" },
            { "LESSSTEEPNESS", "Less Steepness" },
            { "LESSCHECKPOINTS", "Less Checkpoints" },
            { "NARROWPATH", "Narrow Path" },
            { "WIDEPATH", "Wide Path" },
        };

        private static readonly string[] AllCapsWordParts = new string[]
        {
            "CHECKPOINTS", "MODIFIERS", "OBSTACLES", "TEAMNODES", "THRESHOLD",
            "STEEPNESS", "CORRECTION", "FRICTION", "STRENGTH", "BALANCE",
            "WOBBLES", "SMOOTHER", "LANDING", "IMPACT", "BROADER", "BUNNY",
            "EXTRA", "HEAVY", "STUNTS", "CURVES", "COMPASS", "PREVENT",
            "SCOUT", "NODES", "SPEED", "SPIN", "WHEELIE", "OFFROAD", "FAKIE",
            "PUMP", "TWEAK", "SHOW", "TEAM", "LESS", "MORE", "PATH", "BAIL",
            "HOP", "NO", "AIR"
        };

        private static List<PerkEntry> _catalog = new List<PerkEntry>();
        private static readonly HashSet<string> _selected = new HashSet<string>();
        private static FieldInfo _modArrayField;
        private static bool _armedForNextTravel = false;
        private static bool _catalogReady = false;
        private static bool _activeFromGoPlus = false;

        private static FieldInfo _playerModListField;
        private static FieldInfo _uiGenIconContainerField;
        private static FieldInfo _shieldTemplateField;
        private static FieldInfo _hudRootField;
        private static MethodInfo _modShieldColorMethod;

        private static string SelectionPath
        {
            get
            {
                string dir = Path.Combine(
                    Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "UserData"),
                    "DescendersModMenu");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return Path.Combine(dir, "CrewPerkSelection.json");
            }
        }

        public static List<PerkEntry> Catalog => _catalog;
        public static bool IsArmedForNextTravel => _armedForNextTravel;
        public static int SelectedCount => _selected.Count;

        public static void Init()
        {
            LoadSelection();
        }

        public static void RefreshCatalog()
        {
            _catalog.Clear();
            _catalogReady = false;

            try
            {
                GameData gameData = Object.FindObjectOfType<GameData>();
                if ((object)gameData == null) return;

                if ((object)_modArrayField == null)
                    _modArrayField = gameData.GetType().GetField("\u0081jU\u0080h\u0084c",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)_modArrayField == null) return;

                GameModifier[] mods = _modArrayField.GetValue(gameData) as GameModifier[];
                if ((object)mods == null) return;

                var seen = new HashSet<string>();
                for (int i = 0; i < mods.Length; i++)
                {
                    GameModifier mod = mods[i];
                    if ((object)mod == null || string.IsNullOrEmpty(mod.name)) continue;
                    string id = mod.name.Trim();
                    if (seen.Contains(id)) continue;
                    seen.Add(id);

                    _catalog.Add(new PerkEntry
                    {
                        Id = id,
                        Label = FormatPerkLabel(id, mod),
                        Modifier = mod
                    });
                }

                _catalog.Sort((a, b) => string.Compare(a.Label, b.Label,
                    System.StringComparison.OrdinalIgnoreCase));
                _catalogReady = _catalog.Count > 0;
                ModLog.Debug("[CrewPerks] Catalog refreshed: " + _catalog.Count + " perks");
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[CrewPerks] RefreshCatalog: " + ex.Message);
            }
        }

        public static bool IsSelected(string id)
        {
            return !string.IsNullOrEmpty(id) && _selected.Contains(id);
        }

        public static void SetSelected(string id, bool on)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (on)
                _selected.Add(id);
            else
                _selected.Remove(id);
            SaveSelection();
        }

        public static void Toggle(string id)
        {
            SetSelected(id, !IsSelected(id));
        }

        public static void ArmForNextTravel()
        {
            _armedForNextTravel = true;
        }

        public static void Disarm()
        {
            _armedForNextTravel = false;
        }

        public static bool IsActiveFromGoPlus => _activeFromGoPlus;

        /// <summary>Remove GO+ perks when leaving a map (regular GO / new travel without GO+).</summary>
        public static void ClearGoPlusPerks()
        {
            if (!_activeFromGoPlus) return;

            try
            {
                PlayerManager pm = Object.FindObjectOfType<PlayerManager>();
                if ((object)pm != null)
                {
                    PlayerInfoImpact pi = pm.GetPlayerImpact();
                    List<GameModifier> list = (object)pi != null ? GetPlayerModifierList(pi) : null;
                    if (list != null)
                        list.Clear();
                }

                _activeFromGoPlus = false;
                RefreshCrewHud();
                try { GameModifierMods.ReconcileSandboxDialModifiers(); } catch { }
                ModLog.Debug("[CrewPerks] Cleared GO+ perks.");
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[CrewPerks] ClearGoPlusPerks: " + ex.Message);
            }
        }

        private static void MarkGoPlusActive()
        {
            if (_selected.Count == 0) return;
            _activeFromGoPlus = true;
        }

        /// <summary>Modifier list for SessionManager.StartNewSession / lfmgFFR when GO+.</summary>
        public static IList BuildSessionModifierList()
        {
            if (!_armedForNextTravel) return null;

            var list = new List<GameModifier>();
            for (int i = 0; i < _catalog.Count; i++)
            {
                PerkEntry e = _catalog[i];
                if (!IsSelected(e.Id) || (object)e.Modifier == null) continue;
                list.Add(e.Modifier);
            }
            return list;
        }

        /// <summary>Merge armed crew perks into a seed/session modifier list for StartNewSession.</summary>
        public static List<GameModifier> MergeIntoSessionModifiers(object seedModifiers)
        {
            var merged = new List<GameModifier>();
            if (seedModifiers is List<GameModifier>)
            {
                List<GameModifier> seedList = (List<GameModifier>)seedModifiers;
                for (int i = 0; i < seedList.Count; i++)
                    merged.Add(seedList[i]);
            }
            else if (seedModifiers is IList)
            {
                IList il = (IList)seedModifiers;
                for (int i = 0; i < il.Count; i++)
                {
                    if (il[i] is GameModifier)
                        merged.Add((GameModifier)il[i]);
                }
            }

            if (!_armedForNextTravel)
                return merged;

            IList crew = BuildSessionModifierList();
            if (crew == null) return merged;
            for (int i = 0; i < crew.Count; i++)
            {
                GameModifier gm = crew[i] as GameModifier;
                if ((object)gm == null) continue;
                if (!merged.Contains(gm))
                    merged.Add(gm);
            }
            return merged;
        }

        public static void ApplyToLocalPlayer()
        {
            if (_selected.Count == 0) return;

            try
            {
                PlayerManager pm = Object.FindObjectOfType<PlayerManager>();
                if ((object)pm == null) return;
                PlayerInfoImpact pi = pm.GetPlayerImpact();
                if ((object)pi == null) return;

                if (PlayerAlreadyHasSelectedPerks(pi))
                {
                    MarkGoPlusActive();
                    return;
                }

                GameData gameData = Object.FindObjectOfType<GameData>();
                MethodInfo addMod = typeof(PlayerInfoImpact).GetMethod(
                    "AddGameModifier",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new System.Type[] { typeof(GameModifier) },
                    null);
                MethodInfo unlockMod = (object)gameData != null
                    ? typeof(GameData).GetMethod(
                        "UnlockModifierForFreeride",
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        new System.Type[] { typeof(GameModifier) },
                        null)
                    : null;
                MethodInfo getModByName = (object)gameData != null
                    ? typeof(GameData).GetMethod(
                        "GetModFromString",
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        new System.Type[] { typeof(string) },
                        null)
                    : null;

                int applied = 0;
                for (int i = 0; i < _catalog.Count; i++)
                {
                    PerkEntry e = _catalog[i];
                    if (!IsSelected(e.Id)) continue;

                    GameModifier mod = e.Modifier;
                    if ((object)mod == null && (object)getModByName != null)
                        mod = getModByName.Invoke(gameData, new object[] { e.Id }) as GameModifier;
                    if ((object)mod == null) continue;

                    if ((object)unlockMod != null)
                        unlockMod.Invoke(gameData, new object[] { mod });
                    if ((object)addMod != null)
                        addMod.Invoke(pi, new object[] { mod });

                    applied++;
                }

                if (applied > 0)
                {
                    ModLog.Feedback("[SavedLoc] Applied " + applied + " crew perk(s).");
                    MarkGoPlusActive();
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[CrewPerks] ApplyToLocalPlayer: " + ex.Message);
            }
        }

        /// <summary>Redraw top-left modifier shield icons (in-ride HUD, not generating overlay).</summary>
        public static void RefreshCrewHud()
        {
            if (!IsRideHudSafe())
                return;

            try
            {
                PlayerManager pm = Object.FindObjectOfType<PlayerManager>();
                if ((object)pm == null) return;
                PlayerInfoImpact pi = pm.GetPlayerImpact();
                if ((object)pi == null) return;

                RefreshInRideModifierHud(pi);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[CrewPerks] RefreshCrewHud: " + ex.Message);
            }
        }

        private static bool IsRideHudSafe()
        {
            if (MapChanger.IsGeneratingRideState())
                return false;
            return MapChanger.IsRideStateInGame() || StatsManager.IsRidingHudActive();
        }

        private static MethodInfo _currentModsInitMethod;

        private static void RefreshInRideModifierHud(PlayerInfoImpact pi)
        {
            UI_CurrentMods hud = Object.FindObjectOfType<UI_CurrentMods>();
            if ((object)hud == null)
            {
                UI_CurrentMods[] all = Resources.FindObjectsOfTypeAll<UI_CurrentMods>();
                for (int i = 0; i < all.Length; i++)
                {
                    if ((object)all[i] == null) continue;
                    if (!all[i].gameObject.scene.IsValid()) continue;
                    hud = all[i];
                    break;
                }
            }
            if ((object)hud == null) return;

            if ((object)_currentModsInitMethod == null)
                _currentModsInitMethod = typeof(UI_CurrentMods).GetMethod(
                    "Initialize",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new System.Type[]
                    {
                        typeof(PlayerInfoImpact),
                        typeof(int),
                        typeof(int),
                        typeof(bool)
                    },
                    null);
            if ((object)_currentModsInitMethod == null) return;

            int sessionType = 0;
            MapChanger.TryGetCurrentSessionTypeInt(out sessionType);
            int world = 0;
            MapChanger.MapBookmark rideBm;
            if (MapChanger.TryCaptureRideMapIdentity(out rideBm))
                world = rideBm.WorldInt;

            if (!hud.gameObject.activeInHierarchy)
                hud.gameObject.SetActive(true);

            _currentModsInitMethod.Invoke(hud, new object[] { pi, world, sessionType, false });
        }

        private static bool PlayerAlreadyHasSelectedPerks(PlayerInfoImpact pi)
        {
            List<GameModifier> list = GetPlayerModifierList(pi);
            if (list == null || list.Count == 0) return false;

            int need = 0;
            int found = 0;
            for (int i = 0; i < _catalog.Count; i++)
            {
                PerkEntry e = _catalog[i];
                if (!IsSelected(e.Id)) continue;
                need++;
                for (int j = 0; j < list.Count; j++)
                {
                    GameModifier gm = list[j];
                    if ((object)gm != null && gm.name == e.Id)
                    {
                        found++;
                        break;
                    }
                }
            }
            return need > 0 && found >= need;
        }

        private static UI_Generating FindGeneratingUi()
        {
            UI_Generating ui = Object.FindObjectOfType<UI_Generating>();
            if ((object)ui != null) return ui;

            UI_Generating[] all = Resources.FindObjectsOfTypeAll<UI_Generating>();
            for (int i = 0; i < all.Length; i++)
            {
                if ((object)all[i] == null) continue;
                if (!all[i].gameObject.scene.IsValid()) continue;
                return all[i];
            }
            return null;
        }

        private static void HideCrewHudRoot(UI_Generating ui)
        {
            EnsureHudFields(ui);
            if ((object)_hudRootField != null)
            {
                GameObject root = _hudRootField.GetValue(ui) as GameObject;
                if ((object)root != null)
                    root.SetActive(false);
            }
        }

        private static void EnsureHudFields(UI_Generating ui)
        {
            if ((object)_hudRootField == null)
                _hudRootField = typeof(UI_Generating).GetField(
                    "gGPwMoT", BindingFlags.Public | BindingFlags.Instance);
            if ((object)_uiGenIconContainerField == null)
                _uiGenIconContainerField = typeof(UI_Generating).GetField(
                    "\u007CK\u0081zS\u0082\u0081",
                    BindingFlags.Public | BindingFlags.Instance);
            if ((object)_shieldTemplateField == null)
                _shieldTemplateField = typeof(UI_Generating).GetField(
                    "RogG\u007EEw", BindingFlags.Public | BindingFlags.Instance);
            if ((object)_modShieldColorMethod == null)
            {
                MethodInfo[] gmMethods = typeof(GameModifier).GetMethods(
                    BindingFlags.Public | BindingFlags.Instance);
                for (int m = 0; m < gmMethods.Length; m++)
                {
                    if (gmMethods[m].Name == "grbR\u005EaT"
                        && gmMethods[m].GetParameters().Length == 0)
                    {
                        _modShieldColorMethod = gmMethods[m];
                        break;
                    }
                }
            }
        }

        private static void BuildModifierShields(UI_Generating ui, List<GameModifier> mods)
        {
            EnsureHudFields(ui);
            if ((object)_shieldTemplateField == null || (object)_uiGenIconContainerField == null)
                return;

            GameObject template = _shieldTemplateField.GetValue(ui) as GameObject;
            RectTransform container = _uiGenIconContainerField.GetValue(ui) as RectTransform;
            if ((object)template == null || (object)container == null) return;

            for (int i = 0; i < mods.Count; i++)
            {
                GameModifier mod = mods[i];
                if ((object)mod == null) continue;

                GameObject shield = Object.Instantiate(template, container);
                RectTransform rt = shield.GetComponent<RectTransform>();
                if ((object)rt != null)
                    rt.anchoredPosition = new Vector2(i * 45f, 0f);

                TrySetShieldVisuals(shield.transform, mod);
                shield.SetActive(true);
                ActivateTransformChain(shield.transform);
            }
        }

        private static void TrySetShieldVisuals(Transform shield, GameModifier mod)
        {
            if ((object)shield == null || (object)mod == null) return;

            try
            {
                if (shield.childCount > 1)
                {
                    Transform badge = shield.GetChild(1);
                    if (badge.childCount > 0)
                    {
                        Image icon = badge.GetChild(0).GetComponent<Image>();
                        if ((object)icon != null && (object)mod.icon != null)
                            icon.sprite = mod.icon;
                    }
                    Image badgeBg = badge.GetComponent<Image>();
                    if ((object)badgeBg != null && (object)_modShieldColorMethod != null)
                    {
                        object color = _modShieldColorMethod.Invoke(mod, null);
                        if (color is Color)
                            badgeBg.color = (Color)color;
                    }
                }
            }
            catch { }

            try
            {
                if (shield.childCount > 0)
                {
                    Transform alt = shield.GetChild(0);
                    if (alt.childCount > 1)
                    {
                        Image icon = alt.GetChild(1).GetComponent<Image>();
                        if ((object)icon != null && (object)mod.icon != null)
                            icon.sprite = mod.icon;
                    }
                }
            }
            catch { }
        }

        private static void ActivateCrewHudRoot(UI_Generating ui)
        {
            EnsureHudFields(ui);
            if ((object)_hudRootField != null)
            {
                GameObject root = _hudRootField.GetValue(ui) as GameObject;
                if ((object)root != null)
                    root.SetActive(true);
            }

            RectTransform iconRow = (object)_uiGenIconContainerField != null
                ? _uiGenIconContainerField.GetValue(ui) as RectTransform : null;
            if ((object)iconRow != null && (object)iconRow.parent != null
                && (object)iconRow.parent.parent != null)
                iconRow.parent.parent.gameObject.SetActive(true);
        }

        private static void ApplyCrewPortraits(UI_Generating ui, List<GameModifier> mods)
        {
            if (mods == null || mods.Count == 0) return;

            GameData gameData = Object.FindObjectOfType<GameData>();
            if ((object)gameData == null) return;

            FieldInfo facesField = typeof(UI_Generating).GetField(
                "kGNiOnd", BindingFlags.Public | BindingFlags.Instance);
            if ((object)facesField == null) return;
            Image[] faces = facesField.GetValue(ui) as Image[];
            if (faces == null || faces.Length == 0) return;

            MethodInfo getSprite = null;
            MethodInfo[] gdMethods = typeof(GameData).GetMethods(
                BindingFlags.Public | BindingFlags.Instance);
            for (int m = 0; m < gdMethods.Length; m++)
            {
                if (gdMethods[m].Name != "GetCrewMemberSprite") continue;
                ParameterInfo[] parms = gdMethods[m].GetParameters();
                if (parms.Length == 2
                    && string.Equals(parms[1].ParameterType.Name, "Boolean",
                        System.StringComparison.Ordinal))
                {
                    getSprite = gdMethods[m];
                    break;
                }
            }

            var seenClasses = new HashSet<int>();
            var classValues = new List<object>();
            for (int i = 0; i < mods.Count; i++)
            {
                GameModifier mod = mods[i];
                if ((object)mod == null) continue;
                int cls = System.Convert.ToInt32(mod.modClass);
                // Fa~Qg_u.Any — no portrait slot
                if (cls == 3) continue;
                if (seenClasses.Contains(cls)) continue;
                seenClasses.Add(cls);
                classValues.Add(mod.modClass);
            }

            for (int i = 0; i < faces.Length; i++)
            {
                if ((object)faces[i] != null)
                    faces[i].gameObject.SetActive(false);
            }

            int portrait = 0;
            for (int i = 0; i < classValues.Count && portrait < faces.Length && portrait <= 2; i++)
            {
                Image face = faces[portrait];
                if ((object)face == null) continue;

                Sprite sprite = null;
                if ((object)getSprite != null)
                    sprite = getSprite.Invoke(gameData, new object[] { classValues[i], true }) as Sprite;

                if ((object)sprite != null)
                    face.sprite = sprite;

                Color c = face.color;
                face.color = new Color(c.r, c.g, c.b, 1f);
                ActivateTransformChain(face.transform);
                face.gameObject.SetActive(true);
                portrait++;
            }
        }

        private static void ActivateTransformChain(Transform t)
        {
            while ((object)t != null)
            {
                if (!t.gameObject.activeSelf)
                    t.gameObject.SetActive(true);
                t = t.parent;
            }
        }

        public static void ScheduleCrewHudRefresh()
        {
            MelonCoroutines.Start(CrewHudRefreshRoutine());
        }

        private static System.Collections.IEnumerator CrewHudRefreshRoutine()
        {
            for (int i = 0; i < 80; i++)
            {
                if (IsRideHudSafe())
                    break;
                yield return new WaitForSeconds(0.25f);
            }
            if (!IsRideHudSafe())
                yield break;

            RefreshCrewHud();
            yield return new WaitForSeconds(0.5f);
            RefreshCrewHud();
        }

        private static List<GameModifier> GetPlayerModifierList(PlayerInfoImpact pi)
        {
            if ((object)_playerModListField == null)
            {
                FieldInfo[] fields = typeof(PlayerInfoImpact).GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i].FieldType.Equals(typeof(List<GameModifier>)))
                    {
                        _playerModListField = fields[i];
                        break;
                    }
                }
            }
            if ((object)_playerModListField == null) return null;
            return _playerModListField.GetValue(pi) as List<GameModifier>;
        }

        private static void ClearModIconChildren(UI_Generating ui)
        {
            if ((object)_uiGenIconContainerField == null)
                _uiGenIconContainerField = typeof(UI_Generating).GetField(
                    "\u007CK\u0081zS\u0082\u0081",
                    BindingFlags.Public | BindingFlags.Instance);

            if ((object)_uiGenIconContainerField == null) return;
            RectTransform container = _uiGenIconContainerField.GetValue(ui) as RectTransform;
            if ((object)container == null) return;

            for (int i = container.childCount - 1; i >= 0; i--)
                Object.Destroy(container.GetChild(i).gameObject);
        }

        public static void FinishTravelPerks()
        {
            MelonCoroutines.Start(FinishTravelPerksRoutine());
        }

        private static System.Collections.IEnumerator FinishTravelPerksRoutine()
        {
            if (!_armedForNextTravel)
                yield break;

            for (int i = 0; i < 120; i++)
            {
                if (!MapChanger.IsGeneratingRideState())
                    break;
                yield return new WaitForSecondsRealtime(0.25f);
            }

            if (_selected.Count > 0)
                ApplyToLocalPlayer();

            MarkGoPlusActive();
            Disarm();
            ScheduleCrewHudRefresh();
        }

        private static string FormatPerkLabel(string id, GameModifier mod)
        {
            if (string.IsNullOrEmpty(id)) return "";

            string key = NormalizePerkId(id);
            string known;
            if (!string.IsNullOrEmpty(key) && KnownPerkLabels.TryGetValue(key, out known))
                return known;

            string fromAsset = TryGetModifierUiName(mod);
            if (!string.IsNullOrEmpty(fromAsset))
            {
                // Asset strings are often jammed ("Extrastunts") — pretty-format those.
                if (fromAsset.IndexOf(' ') >= 0)
                    return fromAsset;

                string assetKey = NormalizePerkId(fromAsset);
                if (!string.IsNullOrEmpty(assetKey) && KnownPerkLabels.TryGetValue(assetKey, out known))
                    return known;
                return TitleCaseWords(SplitAllCapsId(assetKey));
            }

            return TitleCaseWords(SplitAllCapsId(key));
        }

        private static string NormalizePerkId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            var sb = new System.Text.StringBuilder(id.Length);
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (c == '_' || c == '-' || c == ' ') continue;
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString();
        }

        private static string TryGetModifierUiName(GameModifier mod)
        {
            if ((object)mod == null) return null;
            try
            {
                FieldInfo[] fields = mod.GetType().GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (!fields[i].FieldType.Equals(typeof(string))) continue;
                    string fieldName = fields[i].Name;
                    if (fieldName == "name" || fieldName == "m_Name") continue;
                    string val = fields[i].GetValue(mod) as string;
                    if (string.IsNullOrEmpty(val)) continue;
                    val = val.Trim();
                    if (val.Length < 2 || val == mod.name) continue;
                    if (val.IndexOf(' ') >= 0 || val != val.ToUpperInvariant())
                        return val;
                }
            }
            catch { }
            return null;
        }

        private static string SplitAllCapsId(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;

            bool allUpper = true;
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (char.IsLetter(c) && char.IsLower(c))
                {
                    allUpper = false;
                    break;
                }
            }

            if (!allUpper)
                return SplitCamelCase(id);

            var words = new List<string>();
            SplitAllCapsWords(id, words);
            if (words.Count == 0)
                return id;
            return string.Join(" ", words.ToArray());
        }

        private static void SplitAllCapsWords(string remaining, List<string> words)
        {
            if (string.IsNullOrEmpty(remaining)) return;

            int bestLen = 0;
            for (int i = 0; i < AllCapsWordParts.Length; i++)
            {
                string word = AllCapsWordParts[i];
                if (remaining.Length < word.Length) continue;
                if (string.Equals(remaining.Substring(0, word.Length), word,
                        System.StringComparison.OrdinalIgnoreCase)
                    && word.Length > bestLen)
                    bestLen = word.Length;
            }

            if (bestLen > 0)
            {
                words.Add(remaining.Substring(0, bestLen));
                SplitAllCapsWords(remaining.Substring(bestLen), words);
                return;
            }

            words.Add(remaining);
        }

        private static string SplitCamelCase(string id)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (c == '_' || c == '-')
                {
                    sb.Append(' ');
                    continue;
                }
                if (i > 0 && char.IsUpper(c) && char.IsLower(id[i - 1]))
                    sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        private static string TitleCaseWords(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string[] parts = text.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                string p = parts[i];
                if (p.Length == 0) continue;
                if (p.Length == 1)
                    sb.Append(char.ToUpperInvariant(p[0]));
                else
                    sb.Append(char.ToUpperInvariant(p[0])).Append(p.Substring(1).ToLowerInvariant());
            }
            return sb.ToString();
        }

        private static string FormatPerkLabel(string id)
        {
            return FormatPerkLabel(id, null);
        }

        private static void LoadSelection()
        {
            _selected.Clear();
            try
            {
                string path = SelectionPath;
                if (!File.Exists(path)) return;
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json)) return;

                // Minimal JSON array of strings: ["NOBAIL","..."]
                json = json.Trim();
                if (json.Length < 2 || json[0] != '[') return;
                string inner = json.Substring(1, json.Length - 2);
                string[] parts = inner.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    string p = parts[i].Trim().Trim('"');
                    if (!string.IsNullOrEmpty(p))
                        _selected.Add(p);
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[CrewPerks] LoadSelection: " + ex.Message);
            }
        }

        private static void SaveSelection()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.Append('[');
                bool first = true;
                foreach (string id in _selected)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append('"').Append(id.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
                }
                sb.Append(']');
                File.WriteAllText(SelectionPath, sb.ToString());
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[CrewPerks] SaveSelection: " + ex.Message);
            }
        }
    }
}
