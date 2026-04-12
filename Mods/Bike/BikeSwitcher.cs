using System;
using System.Reflection;
using MelonLoader;
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
                // If TrickSetSwap is on, restore the current bike's original gestures
                // BEFORE switching — otherwise the modified array would stay applied to
                // the bike type we're leaving and re-appear next time we switch back to it.
                try { if (TrickSetSwap.Enabled) TrickSetSwap.Disable(); }
                catch (Exception tssEx) { MelonLogger.Warning("BikeSwitcher: TrickSetSwap.Disable failed: " + tssEx.Message); }

                GameData gameData = UnityEngine.Object.FindObjectOfType<GameData>();
                if (object.ReferenceEquals(gameData, null))
                {
                    MelonLogger.Warning("BikeSwitcher: GameData not found.");
                    return;
                }

                PlayerInfoImpact player = GetPlayerImpact();
                if (object.ReferenceEquals(player, null))
                {
                    MelonLogger.Warning("BikeSwitcher: PlayerInfoImpact not found.");
                    return;
                }

                GameObject playerObject = null;

                // First try the actual field used by PlayerInfoImpact
                FieldInfo playerObjectField = player.GetType().GetField("W\u0082oQHKm", Flags);
                if (!object.ReferenceEquals(playerObjectField, null))
                {
                    playerObject = playerObjectField.GetValue(player) as GameObject;
                }

                // Fallback: find the live player object by name
                if (object.ReferenceEquals(playerObject, null))
                {
                    playerObject = GameObject.Find("Player_Human");
                }

                if (object.ReferenceEquals(playerObject, null))
                {
                    MelonLogger.Warning("BikeSwitcher: Player_Human GameObject not found.");
                    return;
                }

                PlayerCustomization customization = playerObject.GetComponent<PlayerCustomization>();
                if (object.ReferenceEquals(customization, null))
                    customization = playerObject.GetComponentInChildren<PlayerCustomization>(true);

                if (object.ReferenceEquals(customization, null))
                {
                    MelonLogger.Warning("BikeSwitcher: PlayerCustomization not found on Player_Human.");
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
                    MelonLogger.Warning("BikeSwitcher: BikeType[] field not found on GameData.");
                    return;
                }

                BikeType[] bikes = bikeArrayField.GetValue(gameData) as BikeType[];
                if (object.ReferenceEquals(bikes, null) || bikes.Length == 0)
                {
                    MelonLogger.Warning("BikeSwitcher: bike array is null or empty.");
                    return;
                }

                if (index < 0)
                    index = bikes.Length - 1;

                if (index >= bikes.Length)
                    index = 0;

                BikeType selectedBike = bikes[index];
                if (object.ReferenceEquals(selectedBike, null))
                {
                    MelonLogger.Warning("BikeSwitcher: selected bike is null.");
                    return;
                }

                // Change actual bike type through the game method
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
                    MelonLogger.Warning("BikeSwitcher: SetBikeTypeFromNum method not found.");
                }

                // Force-write the BikeType field too, just in case
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
                            MelonLogger.Msg("BikeSwitcher: forced BikeType field -> " + field.Name);
                            break;
                        }
                    }
                }

                // Persist only the preferred bike index for future loads
                SetPreferredBikeIndex(index);

                // LIVE refresh only - do NOT load or save outfit here
                MethodInfo refreshBikeMeshMethod = customization.GetType().GetMethod(
                    "RefreshBikeMesh",
                    Flags
                );

                if (!object.ReferenceEquals(refreshBikeMeshMethod, null))
                {
                    refreshBikeMeshMethod.Invoke(customization, null);
                    MelonLogger.Msg("BikeSwitcher: RefreshBikeMesh called.");
                }
                else
                {
                    MelonLogger.Warning("BikeSwitcher: RefreshBikeMesh method not found.");
                }

                // Debug: check whether a Bike slot item actually exists
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

                            MelonLogger.Msg("BikeSwitcher: Bike slot instance = " +
                                (object.ReferenceEquals(bikeItemInstance, null) ? "NULL" : "FOUND"));
                        }
                    }
                }

                string bikeName = selectedBike.name;
                MelonLogger.Msg("BikeSwitcher: switched to index " + index + " (" + bikeName + ")");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("BikeSwitcher.SetBike failed: " + ex);
            }
        }

        private static PlayerInfoImpact GetPlayerImpact()
        {
            try
            {
                PlayerManager playerManager = UnityEngine.Object.FindObjectOfType<PlayerManager>();
                if (object.ReferenceEquals(playerManager, null))
                {
                    MelonLogger.Warning("BikeSwitcher: PlayerManager not found.");
                    return null;
                }

                MethodInfo getPlayerImpactMethod = playerManager.GetType().GetMethod(
                    "GetPlayerImpact",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (object.ReferenceEquals(getPlayerImpactMethod, null))
                {
                    MelonLogger.Warning("BikeSwitcher: GetPlayerImpact method not found.");
                    return null;
                }

                return getPlayerImpactMethod.Invoke(playerManager, null) as PlayerInfoImpact;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("BikeSwitcher.GetPlayerImpact failed: " + ex);
                return null;
            }
        }

        private static PrefsManager GetPrefsManager()
        {
            PrefsManager prefs = UnityEngine.Object.FindObjectOfType<PrefsManager>();
            if (object.ReferenceEquals(prefs, null))
                MelonLogger.Warning("BikeSwitcher: PrefsManager not found.");

            return prefs;
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
                    MelonLogger.Warning("BikeSwitcher: PrefsManager.GetInt not found.");
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
                return 0;
            }
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
                    MelonLogger.Warning("BikeSwitcher: PrefsManager.SetInt not found.");
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
            }
        }
    }
}