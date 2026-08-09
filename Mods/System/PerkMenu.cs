using MelonLoader;
using DescendersModMenu;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // PerkMenu — instant-grant for the game's own crew member perk system.
    //
    // Crew members are GameModifier ScriptableObject assets - confirmed by reading
    // GameModifier.cs (clean type name, public "modifiers" array of Modifier
    // {modifierType, percentageValue} pairs) and PlayerInfoImpact.cs, where the
    // real crew-member-pick screen grants one by calling the clean, public,
    // unobfuscated PlayerInfoImpact.AddGameModifier(GameModifier). This drives
    // that exact same method, so a perk applies identically to earning it
    // normally - no faking the effect by combining unrelated toggle mods.
    //
    // GameData exposes the full roster the game draws random choices from as a
    // public GameModifier[] field (obfuscated name - found by TYPE, not a
    // hardcoded name, per project convention, so this survives re-obfuscation).
    public static class PerkMenu
    {
        public static string LastResult { get; private set; } = "";

        private static GameModifier[] _allPerks;
        private static FieldInfo _rosterField;
        private static FieldInfo _activeListField;

        public static GameModifier[] AllPerks
        {
            get
            {
                if (_allPerks == null) LoadRoster();
                return _allPerks ?? new GameModifier[0];
            }
        }

        private static void LoadRoster()
        {
            try
            {
                GameData gd = Object.FindObjectOfType<GameData>();
                if ((object)gd == null)
                {
                    MelonLogger.Warning("[PerkMenu] GameData not found in scene.");
                    return;
                }

                if ((object)_rosterField == null)
                    _rosterField = FindFieldByType(typeof(GameData), typeof(GameModifier[]),
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)_rosterField == null)
                {
                    MelonLogger.Warning("[PerkMenu] No GameModifier[] field found on GameData.");
                    return;
                }

                _allPerks = _rosterField.GetValue(gd) as GameModifier[];
                MelonLogger.Msg("[PerkMenu] Loaded " + (_allPerks != null ? _allPerks.Length : 0)
                    + " perk(s) from GameData." + _rosterField.Name);
            }
            catch (System.Exception ex) { MelonLogger.Error("[PerkMenu] LoadRoster: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "PerkMenu"); }
        }

        // Category badge art (colored shield/circle per Green/Blue/Yellow class) -
        // GetCrewMemberSprite is clean, unobfuscated, and is exactly what
        // CrewMemberCard's own real Initialize() method uses for the background
        // behind the (monochrome-mask) perk icon.
        private static GameData _gameData;

        private static GameData GetGameData()
        {
            if ((object)_gameData == null) _gameData = Object.FindObjectOfType<GameData>();
            return _gameData;
        }

        public static Sprite GetBadgeSprite(GameModifier perk)
        {
            if ((object)perk == null) return null;
            try
            {
                GameData gd = GetGameData();
                if ((object)gd == null) return null;
                return gd.GetCrewMemberSprite(perk.modClass);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning("[PerkMenu] GetBadgeSprite: " + ex.Message);
                return null;
            }
        }

        public static void ForceReload() { _allPerks = null; _gameData = null; LoadRoster(); }

        // perk.name is a localization KEY. LocalizationManager.GetLocalizedText(key, sheet)
        // forwards to VpjVZ[_0080.ESPn{xs(key, sheet) - confirmed by reading that class
        // directly. IMPORTANT: the second parameter is a SHEET NAME, not a fallback
        // string (misread this initially - a miss just returns "#!#key#!#" or empty,
        // it never returns the fallback unless key/sheet themselves are empty strings).
        // Sheet titles are read from the real LocalizationSettings asset (clean,
        // unobfuscated "sheetTitles" field) and tried in turn since there's no single
        // "the" sheet - crew member names could live in any of them.
        private static string[] _sheetTitles;
        private static bool _sheetTitlesSearched;

        private static string[] GetSheetTitles()
        {
            if (!_sheetTitlesSearched)
            {
                _sheetTitlesSearched = true;
                try
                {
                    LocalizationSettings settings = Resources.Load<LocalizationSettings>("Languages/LocalizationSettings");
                    if ((object)settings != null && settings.sheetTitles != null)
                        _sheetTitles = settings.sheetTitles;
                    else MelonLogger.Warning("[PerkMenu] GetSheetTitles: LocalizationSettings asset or sheetTitles was null.");
                }
                catch (System.Exception ex) { MelonLogger.Warning("[PerkMenu] GetSheetTitles: " + ex.Message); }
            }
            return _sheetTitles ?? new string[0];
        }

        public static string DisplayName(GameModifier perk)
        {
            if ((object)perk == null) return "(unknown)";
            string key = perk.name;
            if (string.IsNullOrEmpty(key)) return "(unnamed)";
            try
            {
                LocalizationManager lm = GetSingletonInstance<LocalizationManager>();
                if ((object)lm == null)
                {
                    MelonLogger.Warning("[PerkMenu] DisplayName: LocalizationManager singleton not resolved.");
                    return key;
                }

                string[] sheets = GetSheetTitles();
                for (int i = 0; i < sheets.Length; i++)
                {
                    string resolved = lm.GetLocalizedText(key, sheets[i]);
                    if (string.IsNullOrEmpty(resolved)) continue;
                    if (resolved.Length > 0 && resolved[0] == '#') continue; // "#!#key#!#" miss marker
                    return resolved;
                }
                return key;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning("[PerkMenu] DisplayName: " + ex.Message);
                return key;
            }
        }

        // Generic Singleton<T> accessor - the property itself carries an
        // obfuscated name (same string across every closed generic instance),
        // so it's found by return type rather than a literal identifier.
        private static T GetSingletonInstance<T>() where T : UnityEngine.MonoBehaviour
        {
            try
            {
                System.Type closed = typeof(Singleton<>).MakeGenericType(typeof(T));
                PropertyInfo[] props = closed.GetProperties(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < props.Length; i++)
                {
                    if (props[i].PropertyType.Equals(typeof(T)))
                        return props[i].GetValue(null, null) as T;
                }
            }
            catch (System.Exception ex) { MelonLogger.Warning("[PerkMenu] GetSingletonInstance<" + typeof(T).Name + ">: " + ex.Message); }
            return null;
        }

        private static PlayerInfoImpact FindLocalPlayer()
        {
            PlayerInfoImpact[] all = Object.FindObjectsOfType<PlayerInfoImpact>();
            MethodInfo isHuman = typeof(PlayerInfoImpact).GetMethod("IsHumanControlled",
                BindingFlags.Public | BindingFlags.Instance);
            if ((object)isHuman == null) return all.Length > 0 ? all[0] : null;
            for (int i = 0; i < all.Length; i++)
            {
                try { if ((bool)isHuman.Invoke(all[i], null)) return all[i]; }
                catch { }
            }
            return null;
        }

        public static void Grant(GameModifier perk)
        {
            if ((object)perk == null) { LastResult = "Perk is null"; return; }
            PlayerInfoImpact pii = FindLocalPlayer();
            if ((object)pii == null)
            {
                LastResult = "Not in a session";
                MelonLogger.Warning("[PerkMenu] Grant: local PlayerInfoImpact not found - not loaded into a Career/Bike Park session?");
                return;
            }

            try
            {
                pii.AddGameModifier(perk);
                LastResult = "Granted: " + perk.name;
                MelonLogger.Msg("[PerkMenu] Granted \"" + perk.name + "\".");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[PerkMenu] Grant: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "PerkMenu");
                LastResult = "Error - see log";
            }
        }

        public static void Remove(GameModifier perk)
        {
            if ((object)perk == null) { LastResult = "Perk is null"; return; }
            PlayerInfoImpact pii = FindLocalPlayer();
            if ((object)pii == null) { LastResult = "Not in a session"; return; }

            try
            {
                List<GameModifier> list = GetActiveList(pii);
                if (list == null) { LastResult = "Active perk list not found"; return; }

                bool removed = list.Remove(perk);
                LastResult = removed ? "Removed: " + perk.name : "Wasn't active: " + perk.name;
                MelonLogger.Msg("[PerkMenu] " + LastResult);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[PerkMenu] Remove: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "PerkMenu");
                LastResult = "Error - see log";
            }
        }

        public static bool HasPerk(GameModifier perk)
        {
            if ((object)perk == null) return false;
            PlayerInfoImpact pii = FindLocalPlayer();
            if ((object)pii == null) return false;

            try
            {
                List<GameModifier> list = GetActiveList(pii);
                return list != null && list.Contains(perk);
            }
            catch { return false; }
        }

        public static void ClearAllPerks()
        {
            PlayerInfoImpact pii = FindLocalPlayer();
            if ((object)pii == null) { LastResult = "Not in a session"; return; }

            try
            {
                List<GameModifier> list = GetActiveList(pii);
                if (list == null) { LastResult = "Active perk list not found"; return; }

                int count = list.Count;
                list.Clear();
                LastResult = "Cleared " + count + " perk(s)";
                MelonLogger.Msg("[PerkMenu] Cleared " + count + " perk(s).");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[PerkMenu] ClearAllPerks: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "PerkMenu");
                LastResult = "Error - see log";
            }
        }

        private static List<GameModifier> GetActiveList(PlayerInfoImpact pii)
        {
            if ((object)_activeListField == null)
                _activeListField = FindFieldByType(typeof(PlayerInfoImpact), typeof(List<GameModifier>),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if ((object)_activeListField == null) return null;
            return _activeListField.GetValue(pii) as List<GameModifier>;
        }

        private static FieldInfo FindFieldByType(System.Type onType, System.Type wantType, BindingFlags flags)
        {
            // NOTE: .Equals(), never == or != on Type objects - Type's operator
            // overloads compile to Type.op_Equality/op_Inequality, and this build's
            // mscorlib.dll is missing those (documented, recurring gotcha in this
            // project - see How_to_fix_after_update.md).
            FieldInfo[] fields = onType.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
                if (fields[i].FieldType.Equals(wantType)) return fields[i];
            return null;
        }
    }
}
