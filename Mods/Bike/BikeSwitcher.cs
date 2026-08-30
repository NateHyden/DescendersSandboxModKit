using System;
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
            try
            {
                try { if (TrickSetSwap.Enabled) TrickSetSwap.Disable(); }
                catch (Exception tssEx) { ModLog.Warn("BikeSwitcher: TrickSetSwap.Disable failed: " + tssEx.Message); }

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
}

