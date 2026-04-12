using MelonLoader;
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

        // MelonPreferences category
        private static MelonPreferences_Category _cat;
        private static MelonPreferences_Entry<string>[] _entries
            = new MelonPreferences_Entry<string>[SlotCount];
        private static MelonPreferences_Entry<string>[] _nameEntries
            = new MelonPreferences_Entry<string>[SlotCount];
        private static string[] _presetNames = new string[SlotCount];

        // ── Init — call once from OnInitializeMelon ───────────────────────
        public static void Init()
        {
            _cat = MelonPreferences.CreateCategory("OutfitPresets", "Outfit Presets");
            for (int i = 0; i < SlotCount; i++)
            {
                _entries[i] = _cat.CreateEntry<string>("Preset" + (i + 1), "",
                    "Preset " + (i + 1) + " item IDs");
                _nameEntries[i] = _cat.CreateEntry<string>("Preset" + (i + 1) + "Name",
                    "Preset " + (i + 1), "Preset " + (i + 1) + " display name");
                string val = _entries[i].Value;
                if (!string.IsNullOrEmpty(val))
                    _presetIds[i] = ParseIds(val);
                _presetNames[i] = _nameEntries[i].Value;
            }
            MelonLogger.Msg("[OutfitPresets] Loaded from preferences.");
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
            if (_entries[slot] != null) _entries[slot].Value = "";
            if (_nameEntries[slot] != null) _nameEntries[slot].Value = "Preset " + (slot + 1);
            MelonPreferences.Save();
            MelonLogger.Msg("[OutfitPresets] Deleted slot " + slot);
        }

        // ── Internal ──────────────────────────────────────────────────────
        private static bool EnsureRefs()
        {
            if ((object)_pc == null)
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) { MelonLogger.Warning("[OutfitPresets] Player_Human not found."); return false; }
                _pc = player.GetComponent<PlayerCustomization>();
                if ((object)_pc == null) { MelonLogger.Warning("[OutfitPresets] PlayerCustomization not found."); return false; }
            }
            if ((object)_equipOutfit == null)
                _equipOutfit = typeof(PlayerCustomization).GetMethod("EquipOutfit",
                    BindingFlags.Public | BindingFlags.Instance);
            if ((object)_getOutfit == null)
                _getOutfit = typeof(PlayerCustomization).GetMethod("GetEquippedOutfit",
                    BindingFlags.Public | BindingFlags.Instance);
            return (object)_equipOutfit != null && (object)_getOutfit != null;
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

        // ── Save ──────────────────────────────────────────────────────────
        public static bool Save(int slot)
        {
            if (slot < 0 || slot >= SlotCount) return false;
            if (!EnsureRefs()) return false;
            try
            {
                CustomizationItem[] equipped = _getOutfit.Invoke(_pc, null) as CustomizationItem[];
                if (equipped == null || equipped.Length == 0)
                { MelonLogger.Warning("[OutfitPresets] No equipped items."); return false; }

                _presetIds[slot] = new int[equipped.Length];
                for (int i = 0; i < equipped.Length; i++)
                    _presetIds[slot][i] = equipped[i].itemID;

                // Persist to disk
                if (_entries[slot] != null)
                {
                    _entries[slot].Value = IdsToString(_presetIds[slot]);
                    if (_nameEntries[slot] != null)
                        _nameEntries[slot].Value = _presetNames[slot];
                    MelonPreferences.Save();
                }

                MelonLogger.Msg("[OutfitPresets] Saved slot " + slot + " (" + equipped.Length + " items) to disk.");
                return true;
            }
            catch (System.Exception ex) { MelonLogger.Error("[OutfitPresets] Save: " + ex.Message); return false; }
        }

        // ── Load ──────────────────────────────────────────────────────────
        public static bool Load(int slot)
        {
            if (!HasPreset(slot)) { MelonLogger.Warning("[OutfitPresets] Slot " + slot + " empty."); return false; }
            if (!EnsureRefs()) return false;
            try
            {
                if ((object)_custMgr == null)
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
                if (found == 0) { MelonLogger.Warning("[OutfitPresets] No items resolved."); return false; }

                CustomizationItem[] toEquip = new CustomizationItem[found];
                System.Array.Copy(items, toEquip, found);
                _equipOutfit.Invoke(_pc, new object[] { toEquip, false });
                MelonLogger.Msg("[OutfitPresets] Loaded slot " + slot + " (" + found + " items).");
                return true;
            }
            catch (System.Exception ex) { MelonLogger.Error("[OutfitPresets] Load: " + ex.Message); return false; }
        }

        public static void Reset()
        {
            _pc = null;
            _custMgr = null;
            _equipOutfit = null;
            _getOutfit = null;
        }
    }
}