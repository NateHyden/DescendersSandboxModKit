using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using System.Reflection;

namespace DescendersModMenu.Mods
{
    public static class OutfitPresets
    {
        public const int SlotCount = 5;

        private static int[][] _presetIds = new int[SlotCount][];

        private static PlayerCustomization _pc = null;
        private static CustomizationManager _custMgr = null;
        private static MethodInfo _equipOutfit = null;
        private static MethodInfo _getOutfit = null;
        private static MethodInfo _saveOutfit = null;
        private static MethodInfo _getItemInSlot = null;
        private static object _bikeTypeSlotVal = null;

        private static MelonPreferences_Category _cat;
        private static MelonPreferences_Entry<string>[] _entries
            = new MelonPreferences_Entry<string>[SlotCount];
        private static MelonPreferences_Entry<string>[] _nameEntries
            = new MelonPreferences_Entry<string>[SlotCount];
        private static MelonPreferences_Entry<int> _activeSlotEntry;
        private static MelonPreferences_Entry<int>[] _bikeIndexEntries
            = new MelonPreferences_Entry<int>[SlotCount];
        private static string[] _presetNames = new string[SlotCount];
        private static int[] _presetBikeIndex = new int[SlotCount];

        private static int _activeSlot = -1;

        public static void Init()
        {
            _cat = MelonPreferences.CreateCategory("OutfitPresets", "Outfit Presets");
            _activeSlotEntry = _cat.CreateEntry<int>("ActiveSlot", -1,
                "Last loaded outfit preset slot (-1 = none)");
            _activeSlot = _activeSlotEntry.Value;

            for (int i = 0; i < SlotCount; i++)
            {
                _entries[i] = _cat.CreateEntry<string>("Preset" + (i + 1), "",
                    "Preset " + (i + 1) + " item IDs");
                _nameEntries[i] = _cat.CreateEntry<string>("Preset" + (i + 1) + "Name",
                    "Preset " + (i + 1), "Preset " + (i + 1) + " display name");
                _bikeIndexEntries[i] = _cat.CreateEntry<int>("Preset" + (i + 1) + "BikeIndex", -1,
                    "Preset " + (i + 1) + " preferred bike index");
                string val = _entries[i].Value;
                if (!string.IsNullOrEmpty(val))
                    _presetIds[i] = ParseIds(val);
                _presetNames[i] = _nameEntries[i].Value;
                _presetBikeIndex[i] = _bikeIndexEntries[i].Value;
            }

            ModLog.Debug("[OutfitPresets] Loaded from preferences. ActiveSlot=" + _activeSlot);
        }

        // ── Accessors ─────────────────────────────────────────────────────
        public static bool HasPreset(int slot) => slot >= 0 && slot < SlotCount
            && _presetIds[slot] != null && _presetIds[slot].Length > 0;

        // ── Name accessors ────────────────────────────────────────────────
        public static string GetName(int slot)
        {
            if (slot < 0 || slot >= SlotCount) return "";
            return _presetNames[slot];
        }

        public static void SetName(int slot, string name)
        {
            if (slot < 0 || slot >= SlotCount) return;
            _presetNames[slot] = name;
            if (_nameEntries[slot] != null)
            {
                _nameEntries[slot].Value = name;
                MelonPreferences.Save();
            }
        }

        // ── Delete a preset ───────────────────────────────────────────────
        public static void Delete(int slot)
        {
            if (slot < 0 || slot >= SlotCount) return;
            _presetIds[slot] = null;
            _presetNames[slot] = "Preset " + (slot + 1);
            _presetBikeIndex[slot] = -1;
            if (_entries[slot] != null) _entries[slot].Value = "";
            if (_nameEntries[slot] != null) _nameEntries[slot].Value = "Preset " + (slot + 1);
            if (_bikeIndexEntries[slot] != null) _bikeIndexEntries[slot].Value = -1;
            if (_activeSlot == slot)
                SetActiveSlot(-1);
            MelonPreferences.Save();
            ModLog.Debug("[OutfitPresets] Deleted slot " + slot);
        }

        public static void OnSceneUnloaded()
        {
            _pc = null;
        }

        public static void OnSceneInitialized()
        {
            if (_activeSlot < 0 || !HasPreset(_activeSlot)) return;
            MelonCoroutines.Start(SyncPreferredBikeAfterMapRoutine(_activeSlot));
        }

        /// <summary>
        /// Hands/feet-only after a bad Lux quit save — re-apply a known outfit preset.
        /// </summary>
        public static bool TryRepairStrippedOutfit()
        {
            return ForceReapplyActiveOutfit();
        }

        /// <summary>
        /// Re-equip ActiveSlot (or first filled preset) and SaveOutfit so the game
        /// stop spawning a stripped rider. Safe to call after SetBike rebuilds.
        /// </summary>
        public static bool ForceReapplyActiveOutfit()
        {
            int slot = -1;
            if (_activeSlot >= 0 && HasPreset(_activeSlot))
                slot = _activeSlot;
            else
            {
                for (int i = 0; i < SlotCount; i++)
                {
                    if (HasPreset(i))
                    {
                        slot = i;
                        break;
                    }
                }
            }
            if (slot < 0) return false;

            MelonCoroutines.Start(RepairStrippedOutfitRoutine(slot));
            return true;
        }

        private static System.Collections.IEnumerator RepairStrippedOutfitRoutine(int slot)
        {
            bool applied = false;
            for (int attempt = 0; attempt < 120; attempt++)
            {
                // Bike first inside ApplyPresetItems, then clothes, then SaveOutfit.
                if (ApplyPresetItems(slot, true, true, true, true))
                {
                    applied = true;
                    ModLog.Feedback("[OutfitPresets] Restored \"" + GetName(slot) + "\" outfit.");
                    break;
                }
                yield return null;
            }

            if (!applied)
            {
                ModLog.Warn("[OutfitPresets] Could not restore slot " + slot
                    + " — press LOAD on an outfit preset.");
                yield break;
            }

            // One more SaveOutfit after meshes settle.
            for (int w = 0; w < 30; w++)
                yield return null;
            try { PersistLoadedOutfit(); }
            catch { }
        }

        private static System.Collections.IEnumerator SyncPreferredBikeAfterMapRoutine(int slot)
        {
            // Only refresh PREFERREDBIKE from the last-loaded preset.
            // Do NOT SetBike / EquipOutfit here — that was yanking players onto an old
            // preset bike after they had changed gear (e.g. Lux) or the game's own save.
            for (int i = 0; i < 180; i++)
            {
                if (!UnityNull.Alive(GameObject.Find("Player_Human")))
                {
                    yield return null;
                    continue;
                }
                break;
            }

            for (int w = 0; w < 45; w++)
                yield return null;

            int index = ResolveBikeIndexFromPreset(slot);
            if (index < 0) yield break;

            try
            {
                BikeSwitcher.SetPreferredBikeIndexOnly(index);
                ModLog.Debug("[OutfitPresets] Map sync preferred-only bike index " + index
                    + " from active slot " + slot);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[OutfitPresets] Map bike sync: " + ex.Message);
            }
        }

        private static int ResolveBikeIndexFromPreset(int slot)
        {
            if (!HasPreset(slot)) return -1;

            // Prefer the index captured when the preset was saved (reliable).
            if (_presetBikeIndex[slot] >= 0)
                return _presetBikeIndex[slot];

            try
            {
                if (!UnityNull.Alive(_custMgr))
                    _custMgr = GameObject.FindObjectOfType<CustomizationManager>();
                if ((object)_custMgr == null) return -1;

                int[] ids = _presetIds[slot];
                for (int i = 0; i < ids.Length; i++)
                {
                    CustomizationItem item = _custMgr.GetItemFromID(ids[i]);
                    if (!IsBikeTypeSlotItem(item)) continue;
                    int index = BikeSwitcher.FindBikeIndexForCustomizationItem(item);
                    if (index >= 0)
                    {
                        _presetBikeIndex[slot] = index;
                        if (_bikeIndexEntries[slot] != null)
                        {
                            _bikeIndexEntries[slot].Value = index;
                            MelonPreferences.Save();
                        }
                        return index;
                    }
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("[OutfitPresets] ResolveBikeIndexFromPreset: " + ex.Message);
            }
            return -1;
        }

        // ── Internal ──────────────────────────────────────────────────────
        private static bool EnsureRefs(bool silent = false)
        {
            if (!UnityNull.Alive(_pc))
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null)
                {
                    if (!silent)
                        ModLog.Warn("[OutfitPresets] Player_Human not found.");
                    return false;
                }
                _pc = player.GetComponent<PlayerCustomization>();
                if (!UnityNull.Alive(_pc))
                {
                    if (!silent)
                        ModLog.Warn("[OutfitPresets] PlayerCustomization not found.");
                    return false;
                }
            }
            if ((object)_equipOutfit == null)
                _equipOutfit = typeof(PlayerCustomization).GetMethod("EquipOutfit",
                    BindingFlags.Public | BindingFlags.Instance);
            if ((object)_getOutfit == null)
                _getOutfit = typeof(PlayerCustomization).GetMethod("GetEquippedOutfit",
                    BindingFlags.Public | BindingFlags.Instance);
            if ((object)_saveOutfit == null)
                _saveOutfit = typeof(PlayerCustomization).GetMethod("SaveOutfit",
                    BindingFlags.Public | BindingFlags.Instance);
            if ((object)_getItemInSlot == null)
                _getItemInSlot = typeof(PlayerCustomization).GetMethod("GetItemInSlot",
                    BindingFlags.Public | BindingFlags.Instance);
            if ((object)_bikeTypeSlotVal == null)
                _bikeTypeSlotVal = ResolveBikeTypeSlotValue();
            return (object)_equipOutfit != null && (object)_getOutfit != null;
        }

        private static object ResolveBikeTypeSlotValue()
        {
            try
            {
                System.Type[] nestedTypes = typeof(PlayerCustomization).Assembly.GetTypes();
                System.Type slotEnumType = null;
                for (int i = 0; i < nestedTypes.Length; i++)
                {
                    if (string.Equals(nestedTypes[i].Name, "mFWXh}~", System.StringComparison.Ordinal))
                    {
                        slotEnumType = nestedTypes[i];
                        break;
                    }
                }

                if ((object)slotEnumType == null)
                    return null;

                System.Array enumValues = System.Enum.GetValues(slotEnumType);
                for (int i = 0; i < enumValues.Length; i++)
                {
                    object value = enumValues.GetValue(i);
                    if (string.Equals(value.ToString(), "BikeType", System.StringComparison.Ordinal))
                        return value;
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[OutfitPresets] ResolveBikeTypeSlotValue: " + ex.Message);
            }

            return null;
        }

        private static void SetActiveSlot(int slot)
        {
            _activeSlot = slot;
            if (_activeSlotEntry != null)
            {
                _activeSlotEntry.Value = slot;
                MelonPreferences.Save();
            }
        }

        private static string IdsToString(int[] ids)
        {
            string[] parts = new string[ids.Length];
            for (int i = 0; i < ids.Length; i++)
                parts[i] = ids[i].ToString();
            return string.Join(",", parts);
        }

        private static int[] ParseIds(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            string[] parts = s.Split(',');
            int[] ids = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                if (!int.TryParse(parts[i].Trim(), out ids[i])) return null;
            return ids;
        }

        private static bool IsBikeTypeSlotItem(CustomizationItem item)
        {
            return (object)item != null
                && string.Equals(item.slot.ToString(), "BikeType", System.StringComparison.Ordinal);
        }

        private static int FindBikeIndexInItems(CustomizationItem[] equippedItems)
        {
            if (equippedItems == null) return -1;
            for (int i = 0; i < equippedItems.Length; i++)
            {
                CustomizationItem item = equippedItems[i];
                if (!IsBikeTypeSlotItem(item)) continue;
                int index = BikeSwitcher.FindBikeIndexForCustomizationItem(item);
                if (index >= 0) return index;
            }
            return -1;
        }

        private static void SyncPreferredBike(CustomizationItem[] equippedItems)
        {
            int index = FindBikeIndexInItems(equippedItems);

            if (index < 0 && EnsureRefs()
                && (object)_getItemInSlot != null
                && (object)_bikeTypeSlotVal != null)
            {
                try
                {
                    CustomizationItem inSlot = _getItemInSlot.Invoke(
                        _pc, new object[] { _bikeTypeSlotVal }) as CustomizationItem;
                    if ((object)inSlot != null)
                        index = BikeSwitcher.FindBikeIndexForCustomizationItem(inSlot);
                }
                catch (System.Exception ex)
                {
                    ModLog.Warn("[OutfitPresets] GetItemInSlot BikeType: " + ex.Message);
                }
            }

            if (index < 0)
                index = BikeSwitcher.FindBikeIndex(BikeSwitcher.GetCurrentBikeType());

            if (index >= 0)
            {
                // Preferred only — never SetBike after EquipOutfit (wipes clothes).
                BikeSwitcher.SetPreferredBikeIndexOnly(index);
                ModLog.Debug("[OutfitPresets] Synced preferred bike index " + index);
            }
            else
                ModLog.Warn("[OutfitPresets] Could not resolve bike type for preferred bike sync.");
        }

        private static void PersistLoadedOutfit()
        {
            if (!EnsureRefs() || (object)_saveOutfit == null)
                return;

            try
            {
                _saveOutfit.Invoke(_pc, null);
                ModLog.Debug("[OutfitPresets] SaveOutfit called.");
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("[OutfitPresets] SaveOutfit: " + ex.Message);
            }
        }

        private static bool ApplyPresetItems(int slot, bool setActive, bool silent = false,
            bool syncBike = true, bool persist = true)
        {
            if (!HasPreset(slot)) return false;
            if (!EnsureRefs(silent)) return false;

            try
            {
                if (!UnityNull.Alive(_custMgr))
                    _custMgr = GameObject.FindObjectOfType<CustomizationManager>();

                int[] ids = _presetIds[slot];
                CustomizationItem[] items = new CustomizationItem[ids.Length];
                int found = 0;
                for (int i = 0; i < ids.Length; i++)
                {
                    if ((object)_custMgr == null) break;
                    CustomizationItem item = _custMgr.GetItemFromID(ids[i]);
                    if ((object)(UnityEngine.Object)item != null)
                    { items[found] = item; found++; }
                }
                if (found == 0)
                {
                    if (!silent)
                        ModLog.Warn("[OutfitPresets] No items resolved.");
                    return false;
                }
                if (found != ids.Length)
                {
                    ModLog.Debug("[OutfitPresets] Catalog incomplete (" + found + "/" + ids.Length + "), waiting.");
                    return false;
                }

                CustomizationItem[] toEquip = new CustomizationItem[found];
                System.Array.Copy(items, toEquip, found);

                // Switch bike BEFORE equipping clothes — SetBike after EquipOutfit
                // rebuilds the rider and drops the preset outfit.
                if (syncBike)
                {
                    int bikeIdx = FindBikeIndexInItems(toEquip);
                    if (bikeIdx >= 0)
                    {
                        int actual = BikeSwitcher.FindBikeIndex(BikeSwitcher.GetCurrentBikeType());
                        if (actual != bikeIdx)
                            BikeSwitcher.SetBike(bikeIdx);
                        else
                            BikeSwitcher.SetPreferredBikeIndexOnly(bikeIdx);
                    }
                }

                _equipOutfit.Invoke(_pc, new object[] { toEquip, false });

                if (syncBike)
                    SyncPreferredBike(toEquip);
                if (persist)
                    PersistLoadedOutfit();

                if (setActive)
                    SetActiveSlot(slot);

                ModLog.Debug("[OutfitPresets] Applied slot " + slot + " (" + found + " items).");
                return true;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[OutfitPresets] Apply: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "OutfitPresets");
                return false;
            }
        }

        // ── Save ──────────────────────────────────────────────────────────
        public static bool Save(int slot)
        {
            if (slot < 0 || slot >= SlotCount) return false;
            if (LuxGlowTint.AnyEnabled)
            {
                ModLog.Feedback("[OutfitPresets] Turn off Lux glow before saving an outfit.");
                return false;
            }
            if (!EnsureRefs()) return false;
            try
            {
                CustomizationItem[] equipped = _getOutfit.Invoke(_pc, null) as CustomizationItem[];
                if (equipped == null || equipped.Length == 0)
                { ModLog.Warn("[OutfitPresets] No equipped items."); return false; }

                _presetIds[slot] = new int[equipped.Length];
                for (int i = 0; i < equipped.Length; i++)
                    _presetIds[slot][i] = equipped[i].itemID;

                int bikeIdx = BikeSwitcher.FindBikeIndex(BikeSwitcher.GetCurrentBikeType());
                if (bikeIdx < 0)
                    bikeIdx = FindBikeIndexInItems(equipped);
                _presetBikeIndex[slot] = bikeIdx;

                if (_entries[slot] != null)
                {
                    _entries[slot].Value = IdsToString(_presetIds[slot]);
                    if (_nameEntries[slot] != null)
                        _nameEntries[slot].Value = _presetNames[slot];
                    if (_bikeIndexEntries[slot] != null)
                        _bikeIndexEntries[slot].Value = bikeIdx;
                    MelonPreferences.Save();
                }

                ModLog.Debug("[OutfitPresets] Saved slot " + slot + " (" + equipped.Length
                    + " items, bikeIndex=" + bikeIdx + ") to disk.");
                return true;
            }
            catch (System.Exception ex) { MelonLogger.Error("[OutfitPresets] Save: " + ex.Message); Telemetry.ReportErrorAsync(ex, "OutfitPresets"); return false; }
        }

        // ── Load ──────────────────────────────────────────────────────────
        public static bool Load(int slot)
        {
            if (!HasPreset(slot)) { ModLog.Warn("[OutfitPresets] Slot " + slot + " empty."); return false; }
            return ApplyPresetItems(slot, true);
        }

        public static void Reset()
        {
            _pc = null;
            _custMgr = null;
            // Keep method refs — only player/manager are scene-bound.
            _equipOutfit = null;
            _getOutfit = null;
            _saveOutfit = null;
            _getItemInSlot = null;
            _bikeTypeSlotVal = null;
        }
    }
}
