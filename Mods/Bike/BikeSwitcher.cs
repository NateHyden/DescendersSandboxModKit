using System;
using System.Collections;
using System.Reflection;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class BikeSwitcher
    {
        private const BindingFlags Flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public static int CurrentBikeIndex
        {
            get { return GetPreferredBikeIndex(); }
        }

        public static void NextBike()
        {
            SetBike(CurrentBikeIndex + 1);
        }

        public static void PreviousBike()
        {
            SetBike(CurrentBikeIndex - 1);
        }

        public static void SetBike(int index)
        {
            // Trick Set Swap patches the current BikeType, so it has to be lifted off the outgoing
            // bike first and re-applied to the new one - otherwise every bike change turned it off.
            bool resumeTrickSwap = false;
            try { resumeTrickSwap = TrickSetSwap.SuspendForBikeChange(); }
            catch (Exception tssEx) { ModLog.Warn("BikeSwitcher: TrickSetSwap.SuspendForBikeChange failed: " + tssEx.Message); }

            try
            {
                SetBikeCore(index);
            }
            finally
            {
                if (resumeTrickSwap)
                {
                    try { TrickSetSwap.ResumeAfterBikeChange(); }
                    catch (Exception tssEx) { ModLog.Warn("BikeSwitcher: TrickSetSwap.ResumeAfterBikeChange failed: " + tssEx.Message); }
                }
            }
        }

        private static void SetBikeCore(int index)
        {
            try
            {
                GameData gameData = UnityEngine.Object.FindObjectOfType<GameData>();
                if (object.ReferenceEquals(gameData, null))
                {
                    ModLog.Debug("BikeSwitcher: GameData not found.");
                    return;
                }

                PlayerInfoImpact player = GetPlayerImpact();
                if (object.ReferenceEquals(player, null))
                {
                    ModLog.Debug("BikeSwitcher: PlayerInfoImpact not found.");
                    return;
                }

                GameObject playerObject = null;

                FieldInfo playerObjectField = player.GetType().GetField("W\u0082oQHKm", Flags);
                if (!object.ReferenceEquals(playerObjectField, null))
                {
                    playerObject = playerObjectField.GetValue(player) as GameObject;
                }

                if (object.ReferenceEquals(playerObject, null))
                {
                    playerObject = GameObject.Find("Player_Human");
                }

                if (object.ReferenceEquals(playerObject, null))
                {
                    ModLog.Debug("BikeSwitcher: Player_Human GameObject not found.");
                    return;
                }

                PlayerCustomization customization = playerObject.GetComponent<PlayerCustomization>();
                if (object.ReferenceEquals(customization, null))
                    customization = playerObject.GetComponentInChildren<PlayerCustomization>(true);

                if (object.ReferenceEquals(customization, null))
                {
                    ModLog.Warn("BikeSwitcher: PlayerCustomization not found on Player_Human.");
                    return;
                }

                FieldInfo bikeArrayField = null;
                FieldInfo[] gameDataFields = gameData.GetType().GetFields(Flags);

                for (int i = 0; i < gameDataFields.Length; i++)
                {
                    FieldInfo field = gameDataFields[i];

                    if (!field.FieldType.IsArray)
                        continue;

                    Type elementType = field.FieldType.GetElementType();
                    if (!object.ReferenceEquals(elementType, null) &&
                        string.Equals(elementType.Name, "BikeType", StringComparison.Ordinal))
                    {
                        bikeArrayField = field;
                        break;
                    }
                }

                if (object.ReferenceEquals(bikeArrayField, null))
                {
                    ModLog.Warn("BikeSwitcher: BikeType[] field not found on GameData.");
                    return;
                }

                BikeType[] bikes = bikeArrayField.GetValue(gameData) as BikeType[];
                if (object.ReferenceEquals(bikes, null) || bikes.Length == 0)
                {
                    ModLog.Warn("BikeSwitcher: bike array is null or empty.");
                    return;
                }

                if (index < 0)
                    index = bikes.Length - 1;

                if (index >= bikes.Length)
                    index = 0;

                BikeType selectedBike = bikes[index];
                if (object.ReferenceEquals(selectedBike, null))
                {
                    ModLog.Warn("BikeSwitcher: selected bike is null.");
                    return;
                }

                MethodInfo setBikeTypeMethod = player.GetType().GetMethod(
                    "SetBikeTypeFromNum",
                    Flags
                );

                if (!object.ReferenceEquals(setBikeTypeMethod, null))
                {
                    setBikeTypeMethod.Invoke(player, new object[] { index });
                }
                else
                {
                    ModLog.Warn("BikeSwitcher: SetBikeTypeFromNum method not found.");
                }

                FieldInfo[] playerFields = player.GetType().GetFields(Flags);
                for (int i = 0; i < playerFields.Length; i++)
                {
                    FieldInfo field = playerFields[i];

                    if (string.Equals(field.FieldType.Name, "BikeType", StringComparison.Ordinal))
                    {
                        if (string.Equals(field.Name, "dzQf\u0082nw", StringComparison.Ordinal) ||
                            string.Equals(field.Name, "<dzQf\u0082nw>k__BackingField", StringComparison.Ordinal))
                        {
                            field.SetValue(player, selectedBike);
                            ModLog.Debug("BikeSwitcher: forced BikeType field -> " + field.Name);
                            break;
                        }
                    }
                }

                SetPreferredBikeIndex(index);

                // Remember the bike the user actually picked so a respawn (B key) can restore it.
                _userBikeIndex = index;
                ModLog.Debug("[BikeSwitcher] User bike set -> index " + index + " (" + selectedBike.name + ")");

                MethodInfo refreshBikeMeshMethod = customization.GetType().GetMethod(
                    "RefreshBikeMesh",
                    Flags
                );

                if (!object.ReferenceEquals(refreshBikeMeshMethod, null))
                {
                    refreshBikeMeshMethod.Invoke(customization, null);
                    ModLog.Debug("BikeSwitcher: RefreshBikeMesh called.");
                }
                else
                {
                    ModLog.Warn("BikeSwitcher: RefreshBikeMesh method not found.");
                }

                MethodInfo getItemInstanceInSlotMethod = customization.GetType().GetMethod(
                    "GetItemInstanceInSlot",
                    Flags
                );

                if (!object.ReferenceEquals(getItemInstanceInSlotMethod, null))
                {
                    Type[] nestedTypes = customization.GetType().Assembly.GetTypes();
                    Type slotEnumType = null;

                    for (int i = 0; i < nestedTypes.Length; i++)
                    {
                        if (string.Equals(nestedTypes[i].Name, "mFWXh}~", StringComparison.Ordinal))
                        {
                            slotEnumType = nestedTypes[i];
                            break;
                        }
                    }

                    if (!object.ReferenceEquals(slotEnumType, null))
                    {
                        Array enumValues = Enum.GetValues(slotEnumType);
                        object bikeSlotValue = null;

                        for (int i = 0; i < enumValues.Length; i++)
                        {
                            object value = enumValues.GetValue(i);
                            if (string.Equals(value.ToString(), "Bike", StringComparison.Ordinal))
                            {
                                bikeSlotValue = value;
                                break;
                            }
                        }

                        if (!object.ReferenceEquals(bikeSlotValue, null))
                        {
                            object bikeItemInstance = getItemInstanceInSlotMethod.Invoke(
                                customization,
                                new object[] { bikeSlotValue }
                            );

                            ModLog.Debug("BikeSwitcher: Bike slot instance = " +
                                (object.ReferenceEquals(bikeItemInstance, null) ? "NULL" : "FOUND"));
                        }
                    }
                }

                string bikeName = selectedBike.name;
                ModLog.Debug("BikeSwitcher: switched to index " + index + " (" + bikeName + ")");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("BikeSwitcher.SetBike failed: " + ex);
                Telemetry.ReportErrorAsync(ex, "BikeSwitcher");
            }
        }

        // ── Respawn persistence (B key / reset to checkpoint) ─────────────────
        // The game rebuilds the rider on respawn and can fall back to its own default bike,
        // so the bike picked in the Bike tab was lost every time the player pressed B.
        // _userBikeIndex is the last bike chosen through SetBike / EnsureBikeApplied.
        // -1 means the Sandbox never switched the bike, so respawns are left untouched.
        private static int _userBikeIndex = -1;
        private static int _respawnGen = 0;
        private static bool _reapplying = false;

        // Frames after the respawn call at which the rider's bike is checked. The game can
        // rebuild the bike a few frames after RespawnOnTrack returns, so check more than once.
        private static readonly int[] RespawnCheckFrames = { 3, 15, 45 };

        public static int UserBikeIndex
        {
            get { return _userBikeIndex; }
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo startLine = typeof(PlayerInfoImpact).GetMethod(
                    "RespawnAtStartLine", BindingFlags.Public | BindingFlags.Instance);
                MethodInfo onTrack = typeof(PlayerInfoImpact).GetMethod(
                    "RespawnOnTrack", BindingFlags.Public | BindingFlags.Instance);

                if ((object)startLine != null)
                    harmony.Patch(startLine, postfix: new HarmonyLib.HarmonyMethod(
                        typeof(BikeSwitcherRespawn_Patch).GetMethod("PostfixStartLine",
                            BindingFlags.Public | BindingFlags.Static)));
                else
                    MelonLogger.Warning("[BikeSwitcher] RespawnAtStartLine not found — bike will not be kept on start-line respawn.");

                if ((object)onTrack != null)
                    harmony.Patch(onTrack, postfix: new HarmonyLib.HarmonyMethod(
                        typeof(BikeSwitcherRespawn_Patch).GetMethod("PostfixOnTrack",
                            BindingFlags.Public | BindingFlags.Static)));
                else
                    MelonLogger.Warning("[BikeSwitcher] RespawnOnTrack not found — bike will not be kept on respawn.");

                ModLog.Debug("[BikeSwitcher] Respawn bike-keep patch applied (startLine="
                    + ((object)startLine != null) + ", onTrack=" + ((object)onTrack != null) + ").");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[BikeSwitcher] ApplyPatch: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "BikeSwitcher");
            }
        }

        public static void OnPlayerRespawned(string source)
        {
            try
            {
                if (_reapplying) return;

                if (_userBikeIndex < 0)
                {
                    ModLog.Debug("[BikeSwitcher] Respawn via " + source + " — no user-selected bike, leaving as is.");
                    return;
                }

                int gen = ++_respawnGen;
                MelonCoroutines.Start(ReapplyBikeAfterRespawn(_userBikeIndex, gen, source));
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[BikeSwitcher] OnPlayerRespawned: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "BikeSwitcher");
            }
        }

        private static IEnumerator ReapplyBikeAfterRespawn(int wanted, int gen, string source)
        {
            ModLog.Debug("[BikeSwitcher] Respawn via " + source + " — keeping user bike index " + wanted
                + " (PREFERREDBIKE=" + GetPreferredBikeIndex() + ", TrickSetSwap=" + TrickSetSwap.Enabled + ")");

            bool reapplied = false;
            int waited = 0;

            for (int c = 0; c < RespawnCheckFrames.Length; c++)
            {
                while (waited < RespawnCheckFrames[c])
                {
                    waited++;
                    yield return null;
                }

                // A newer respawn supersedes this one.
                if (gen != _respawnGen) yield break;

                try
                {
                    if (CheckAndReapply(wanted, source, RespawnCheckFrames[c], reapplied))
                        reapplied = true;
                }
                catch (Exception ex)
                {
                    MelonLogger.Error("[BikeSwitcher] Respawn check @" + RespawnCheckFrames[c] + "f: " + ex);
                    Telemetry.ReportErrorAsync(ex, "BikeSwitcher");
                }
            }
        }

        /// <summary>Returns true if it had to call SetBike to put the bike back.</summary>
        private static bool CheckAndReapply(int wanted, string source, int frame, bool alreadyReapplied)
        {
            string tag = "[BikeSwitcher] Respawn(" + source + ") @" + frame + "f: ";

            PlayerInfoImpact player = GetPlayerImpact();
            if (object.ReferenceEquals(player, null))
            {
                ModLog.Debug(tag + "PlayerInfoImpact not ready yet.");
                return false;
            }

            int preferred = GetPreferredBikeIndex();
            BikeType primary = GetPrimaryBikeType(player);
            int actual = FindBikeIndex(primary);
            string actualName = (object)primary != null ? primary.name : "NULL";

            if (actual == wanted)
            {
                ModLog.Debug(tag + "OK — rider is on wanted bike " + wanted + " (" + actualName
                    + "), PREFERREDBIKE=" + preferred + ".");
                if (preferred != wanted)
                {
                    SetPreferredBikeIndex(wanted);
                    ModLog.Debug(tag + "PREFERREDBIKE was " + preferred + " — restored to " + wanted + ".");
                }
                return false;
            }

            if (actual < 0 && alreadyReapplied)
            {
                ModLog.Debug(tag + "current bike can't be resolved (" + actualName
                    + "); already re-applied this respawn, not repeating.");
                return false;
            }

            ModLog.Debug(tag + "MISMATCH — rider on index " + actual + " (" + actualName + "), wanted "
                + wanted + ", PREFERREDBIKE=" + preferred + ". Re-applying.");

            // SetBike suspends and re-applies Trick Set Swap itself, so it follows the restored bike.
            _reapplying = true;
            try { SetBike(wanted); }
            finally { _reapplying = false; }

            BikeType after = GetPrimaryBikeType(player);
            ModLog.Debug(tag + "after re-apply: rider on index " + FindBikeIndex(after) + " ("
                + ((object)after != null ? after.name : "NULL") + "), PREFERREDBIKE=" + GetPreferredBikeIndex() + ".");
            return true;
        }

        /// <summary>
        /// The BikeType field SetBike forces; falls back to the first non-null BikeType field.
        /// </summary>
        private static BikeType GetPrimaryBikeType(PlayerInfoImpact player)
        {
            FieldInfo[] fields = player.GetType().GetFields(Flags);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (!string.Equals(field.FieldType.Name, "BikeType", StringComparison.Ordinal))
                    continue;

                if (string.Equals(field.Name, "dzQf\u0082nw", StringComparison.Ordinal) ||
                    string.Equals(field.Name, "<dzQf\u0082nw>k__BackingField", StringComparison.Ordinal))
                {
                    BikeType bt = field.GetValue(player) as BikeType;
                    if ((object)bt != null)
                        return bt;
                }
            }

            return GetCurrentBikeType();
        }

        public static BikeType[] GetAllBikeTypes()
        {
            try
            {
                GameData gameData = UnityEngine.Object.FindObjectOfType<GameData>();
                if (object.ReferenceEquals(gameData, null))
                    return null;

                FieldInfo[] gameDataFields = gameData.GetType().GetFields(Flags);
                for (int i = 0; i < gameDataFields.Length; i++)
                {
                    FieldInfo field = gameDataFields[i];
                    if (!field.FieldType.IsArray)
                        continue;

                    Type elementType = field.FieldType.GetElementType();
                    if (!object.ReferenceEquals(elementType, null) &&
                        string.Equals(elementType.Name, "BikeType", StringComparison.Ordinal))
                        return field.GetValue(gameData) as BikeType[];
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("BikeSwitcher.GetAllBikeTypes failed: " + ex);
                Telemetry.ReportErrorAsync(ex, "BikeSwitcher");
            }

            return null;
        }

        public static BikeType GetCurrentBikeType()
        {
            try
            {
                PlayerInfoImpact player = GetPlayerImpact();
                if (object.ReferenceEquals(player, null))
                    return null;

                FieldInfo[] playerFields = player.GetType().GetFields(Flags);
                for (int i = 0; i < playerFields.Length; i++)
                {
                    FieldInfo field = playerFields[i];
                    if (!string.Equals(field.FieldType.Name, "BikeType", StringComparison.Ordinal))
                        continue;

                    BikeType bt = field.GetValue(player) as BikeType;
                    if (!object.ReferenceEquals(bt, null))
                        return bt;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("BikeSwitcher.GetCurrentBikeType failed: " + ex);
                Telemetry.ReportErrorAsync(ex, "BikeSwitcher");
            }

            return null;
        }

        public static int FindBikeIndex(BikeType bike)
        {
            if (object.ReferenceEquals(bike, null))
                return -1;

            BikeType[] bikes = GetAllBikeTypes();
            if (object.ReferenceEquals(bikes, null) || bikes.Length == 0)
                return -1;

            for (int i = 0; i < bikes.Length; i++)
            {
                if (object.ReferenceEquals(bikes[i], bike))
                    return i;
            }

            string bikeName = bike.name ?? "";
            for (int i = 0; i < bikes.Length; i++)
            {
                BikeType candidate = bikes[i];
                if (object.ReferenceEquals(candidate, null))
                    continue;

                if (string.Equals(candidate.name, bikeName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        public static int FindBikeIndexForCustomizationItem(CustomizationItem item)
        {
            if (object.ReferenceEquals(item, null))
                return -1;

            BikeType[] bikes = GetAllBikeTypes();
            if (object.ReferenceEquals(bikes, null) || bikes.Length == 0)
                return -1;

            string displayName = item.displayName ?? "";
            for (int i = 0; i < bikes.Length; i++)
            {
                BikeType candidate = bikes[i];
                if (object.ReferenceEquals(candidate, null))
                    continue;

                string bikeName = candidate.name ?? "";
                if (string.Equals(displayName, bikeName, StringComparison.OrdinalIgnoreCase))
                    return i;

                if (bikeName.Length > 2 &&
                    displayName.IndexOf(bikeName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }

            return -1;
        }

        private static PlayerInfoImpact GetPlayerImpact()
        {
            try
            {
                PlayerManager playerManager = UnityEngine.Object.FindObjectOfType<PlayerManager>();
                if (object.ReferenceEquals(playerManager, null))
                {
                    ModLog.Debug("BikeSwitcher: PlayerManager not found.");
                    return null;
                }

                MethodInfo getPlayerImpactMethod = playerManager.GetType().GetMethod(
                    "GetPlayerImpact",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (object.ReferenceEquals(getPlayerImpactMethod, null))
                {
                    ModLog.Warn("BikeSwitcher: GetPlayerImpact method not found.");
                    return null;
                }

                return getPlayerImpactMethod.Invoke(playerManager, null) as PlayerInfoImpact;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("BikeSwitcher.GetPlayerImpact failed: " + ex);
                Telemetry.ReportErrorAsync(ex, "BikeSwitcher");
                return null;
            }
        }

        private static PrefsManager GetPrefsManager()
        {
            // Early spawn / menu hops often call preferred-bike helpers before PrefsManager
            // exists — not a real failure; callers no-op and DeferredEnsureSavedBike retries.
            return UnityEngine.Object.FindObjectOfType<PrefsManager>();
        }

        private static int GetPreferredBikeIndex()
        {
            try
            {
                PrefsManager prefs = GetPrefsManager();
                if (object.ReferenceEquals(prefs, null))
                    return 0;

                MethodInfo getIntMethod = prefs.GetType().GetMethod(
                    "GetInt",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (object.ReferenceEquals(getIntMethod, null))
                {
                    ModLog.Warn("BikeSwitcher: PrefsManager.GetInt not found.");
                    return 0;
                }

                object result = getIntMethod.Invoke(prefs, new object[] { "PREFERREDBIKE", 0 });
                if (result is int)
                    return (int)result;

                return 0;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("BikeSwitcher.GetPreferredBikeIndex failed: " + ex);
                Telemetry.ReportErrorAsync(ex, "BikeSwitcher");
                return 0;
            }
        }

        /// <summary>
        /// Updates PREFERREDBIKE without switching bike type or touching TrickSetSwap.
        /// </summary>
        public static void SetPreferredBikeIndexOnly(int index)
        {
            SetPreferredBikeIndex(index);
        }

        /// <summary>
        /// Sets preferred bike and switches the spawned rider if they are on a different bike.
        /// Safe to call after Player_Human exists (autoload / map hop).
        /// </summary>
        public static void EnsureBikeApplied(int index)
        {
            if (index < 0) return;
            // Even when we are already on this bike (so SetBike is skipped), it is still the
            // bike the user wants kept across respawns.
            _userBikeIndex = index;
            SetPreferredBikeIndex(index);
            int actual = FindBikeIndex(GetCurrentBikeType());
            if (actual >= 0 && actual == index)
            {
                ModLog.Debug("BikeSwitcher: already on bike index " + index);
                return;
            }
            SetBike(index);
        }

        private static void SetPreferredBikeIndex(int index)
        {
            try
            {
                PrefsManager prefs = GetPrefsManager();
                if (object.ReferenceEquals(prefs, null))
                    return;

                MethodInfo setIntMethod = prefs.GetType().GetMethod(
                    "SetInt",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (object.ReferenceEquals(setIntMethod, null))
                {
                    ModLog.Warn("BikeSwitcher: PrefsManager.SetInt not found.");
                    return;
                }

                setIntMethod.Invoke(prefs, new object[] { "PREFERREDBIKE", index });

                MethodInfo saveMethod = prefs.GetType().GetMethod(
                    "Save",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (!object.ReferenceEquals(saveMethod, null))
                    saveMethod.Invoke(prefs, null);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("BikeSwitcher.SetPreferredBikeIndex failed: " + ex);
                Telemetry.ReportErrorAsync(ex, "BikeSwitcher");
            }
        }
    }

    /// <summary>
    /// Harmony postfixes for the game's respawn methods (B key / reset to checkpoint / start line).
    /// Same pattern as GhostRespawn_Patch / SlowMoOnBailRespawn_Patch.
    /// </summary>
    public static class BikeSwitcherRespawn_Patch
    {
        public static void PostfixStartLine() { BikeSwitcher.OnPlayerRespawned("RespawnAtStartLine"); }
        public static void PostfixOnTrack() { BikeSwitcher.OnPlayerRespawned("RespawnOnTrack"); }
    }
}
