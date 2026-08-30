using MelonLoader;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
  public static class LuxGlowPresets
  {
    public const int SlotCount = 3;

    private static MelonPreferences_Category _cat;
    private static MelonPreferences_Entry<string>[] _entries
        = new MelonPreferences_Entry<string>[SlotCount];
    private static MelonPreferences_Entry<string>[] _nameEntries
        = new MelonPreferences_Entry<string>[SlotCount];
    private static string[] _names = new string[SlotCount];

    public static void Init()
    {
      _cat = MelonPreferences.CreateCategory("LuxGlowPresets", "Lux Glow Presets");
      for (int i = 0; i < SlotCount; i++)
      {
        _entries[i] = _cat.CreateEntry<string>("LuxPreset" + (i + 1), "",
            "Lux glow preset " + (i + 1));
        _nameEntries[i] = _cat.CreateEntry<string>("LuxPreset" + (i + 1) + "Name",
            "Glow " + (i + 1), "Lux glow preset " + (i + 1) + " name");
        _names[i] = _nameEntries[i].Value;
      }
      ModLog.Debug("[LuxGlowPresets] Loaded from preferences.");
    }

    public static bool HasPreset(int slot)
    {
      return slot >= 0 && slot < SlotCount
          && !string.IsNullOrEmpty(_entries[slot].Value);
    }

    public static string GetName(int slot)
    {
      if (slot < 0 || slot >= SlotCount) return "";
      return _names[slot];
    }

    public static void SetName(int slot, string name)
    {
      if (slot < 0 || slot >= SlotCount) return;
      _names[slot] = name;
      if (_nameEntries[slot] != null)
      {
        _nameEntries[slot].Value = name;
        MelonPreferences.Save();
      }
    }

    public static void Delete(int slot)
    {
      if (slot < 0 || slot >= SlotCount) return;
      _names[slot] = "Glow " + (slot + 1);
      if (_entries[slot] != null) _entries[slot].Value = "";
      if (_nameEntries[slot] != null) _nameEntries[slot].Value = _names[slot];
      MelonPreferences.Save();
      ModLog.Debug("[LuxGlowPresets] Deleted slot " + slot);
    }

    public static bool Save(int slot)
    {
      if (slot < 0 || slot >= SlotCount) return false;
      try
      {
        string data = LuxGlowTint.ExportPresetString();
        _entries[slot].Value = data;
        if (_nameEntries[slot] != null)
          _nameEntries[slot].Value = _names[slot];
        MelonPreferences.Save();
        ModLog.Feedback("[LuxGlow] Saved preset " + (slot + 1) + ".");
        ModLog.Debug("[LuxGlowPresets] Saved slot " + slot + ": " + data);
        return true;
      }
      catch (System.Exception ex)
      {
        MelonLogger.Error("[LuxGlowPresets] Save: " + ex.Message);
        Telemetry.ReportErrorAsync(ex, "LuxGlowPresets");
        return false;
      }
    }

    public static bool Load(int slot)
    {
      if (!HasPreset(slot))
      {
        ModLog.Feedback("[LuxGlow] Preset " + (slot + 1) + " is empty.");
        return false;
      }
      try
      {
        bool ok = LuxGlowTint.ImportPresetString(_entries[slot].Value);
        if (ok)
          ModLog.Feedback("[LuxGlow] Loaded preset " + (slot + 1) + ".");
        else
          ModLog.Feedback("[LuxGlow] Could not load preset " + (slot + 1) + ".");
        return ok;
      }
      catch (System.Exception ex)
      {
        MelonLogger.Error("[LuxGlowPresets] Load: " + ex.Message);
        Telemetry.ReportErrorAsync(ex, "LuxGlowPresets");
        return false;
      }
    }
  }
}
