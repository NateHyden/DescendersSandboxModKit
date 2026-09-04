using MelonLoader;
using UnityEngine;
using DescendersModMenu;
using DescendersModMenu.BikeStats;
using DescendersModMenu.UI;
using System.IO;

namespace DescendersModMenu.Mods
{
  /// <summary>
  /// Saved ride spots — JSON file only. Save captures map identity + position; GO loads map if needed then teleports.
  /// </summary>
  public static class SavedLocations
  {
    public const int SlotCount = 10;
    public const string FreerideSectionKey = "__freeride_section__";
    public const string ModIoSectionKey = "__modio_section__";

    public struct SavedSpotRef
    {
      public string MapKey;
      public string MapLabel;
      public int Slot;
      public string SpotName;
      public bool MapCanGo;
      public string MapAvailabilityHint;
    }

    public struct MapGoStatus
    {
      public bool CanGo;
      public string Hint;
    }

    public struct MapSaveStatus
    {
      public bool CanSave;
      public string Message;
    }

    private struct SlotData
    {
      public bool Has;
      public string Name;
      public float X;
      public float Y;
      public float Z;
      public float RotY;
    }

    [System.Serializable]
    private class SpotFileEntry
    {
      public string mapKey;
      public string mapLabel;
      public int slot;
      public string name;
      public float x;
      public float y;
      public float z;
      public float rotY;
      public int kind;
      public int customSeed;
      public int worldInt;
      public int sessionTypeInt;
      public string sceneName;
      public string sessionSeed;
      public string workshopPath;
    }

    private static readonly SlotData[] _slots = new SlotData[SlotCount];
    private static readonly System.Collections.Generic.List<SpotFileEntry> _fileSpots
        = new System.Collections.Generic.List<SpotFileEntry>();

    private static bool _supported;
    private static string _mapKey = "";
    private static string _mapLabel = "";
    private static MapChanger.MapBookmark _currentMap;

    private static SpotFileEntry _pendingSpot;
    private static bool _pendingGoRunning;
    private static string _pendingMapKey = "";
    private static int _pendingSlot = -1;

    private static bool _holdPositionActive;
    private static Vector3 _holdDest;
    private static float _holdRotY;
    private static float _holdUntil;

    private static MelonPreferences_Category _prefsCat;

    private static string FilePath
    {
      get
      {
        string dir = Path.Combine(
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "UserData"),
            "DescendersModMenu");
        if (!Directory.Exists(dir))
          Directory.CreateDirectory(dir);
        return Path.Combine(dir, "SavedLocations.json");
      }
    }

    public static void Init()
    {
      for (int i = 0; i < SlotCount; i++)
        _slots[i].Name = "Spot " + (i + 1);

      LoadFileFromDisk();
      int loadedFromFile = _fileSpots.Count;
      bool mergedFromPrefs = MergeFromMelonPrefs();
      for (int i = 0; i < _fileSpots.Count; i++)
        EnsureSpotMetadata(_fileSpots[i], false);
      DeduplicateFileSpots();
      // Persist repaired labels, but never rewrite if load somehow lost spots.
      if (_fileSpots.Count >= loadedFromFile)
        SaveFileToDisk();
      ModLog.Debug("[SavedLocations] Init file=" + FilePath + " spots=" + _fileSpots.Count
          + " (loaded=" + loadedFromFile + ")");
    }

    public static void OnSceneInitialized()
    {
      ModWorkshopLoader.InvalidateCatalogPublic();
      RefreshCurrentMap();
      MelonCoroutines.Start(DelayedRefreshRoutine());
      if (!ModWorkshopLoader.IsWorkshopLoadInProgress())
        StartDeferredTeleportIfPending();
    }

    public static void OnSceneUnloaded() => SaveFileToDisk();

    public static void ForceSave() => SaveFileToDisk();

    public static void Tick()
    {
      if (_holdPositionActive)
      {
        if (Time.unscaledTime < _holdUntil)
          ApplyRidePosition(_holdDest, _holdRotY);
        else
          _holdPositionActive = false;
      }

      if (HasPendingGo() && !_pendingGoRunning && !ModWorkshopLoader.IsWorkshopLoadInProgress())
        StartDeferredTeleportIfPending();
    }

    private static System.Collections.IEnumerator DelayedRefreshRoutine()
    {
      for (int i = 0; i < 6; i++)
      {
        yield return new WaitForSeconds(0.5f);
        RefreshCurrentMap();
      }
    }

    public static void RefreshCurrentMap()
    {
      MapChanger.MapBookmark bm;
      if (!TryCaptureBookmarkForSpots(out bm))
      {
        _supported = false;
        _mapKey = "";
        _mapLabel = "";
        _currentMap = new MapChanger.MapBookmark();
        ClearMemorySlots();
        return;
      }

      _supported = true;
      _currentMap = bm;
      _mapKey = MapKeyFromBookmark(bm);
      _mapLabel = bm.DisplayLabel ?? "";
      LoadSlotsForCurrentMap();
      tryCompletePendingGo();
    }

    public static bool CanUse => _supported;
    public static string MapLabel => _mapLabel;
    public static bool HasAnyOnCurrentMap()
    {
      for (int i = 0; i < SlotCount; i++)
        if (_slots[i].Has) return true;
      return false;
    }

    public static bool IsAnyActive => HasAnyOnCurrentMap();

    public static MapSaveStatus GetCurrentMapSaveStatus()
    {
      if (StatsManager.IsInMenuContext())
        return new MapSaveStatus
        {
          CanSave = false,
          Message = "Can't save here — start a ride first."
        };

      string scene = MapChanger.GetCurrentSceneName();
      if (MapChanger.IsSystemScene(scene))
        return new MapSaveStatus
        {
          CanSave = false,
          Message = "Can't save here — still loading."
        };

      if (!UnityNull.Alive(GameObject.Find("Player_Human")))
        return new MapSaveStatus
        {
          CanSave = false,
          Message = "Can't save here — no rider found."
        };

      MapChanger.MapBookmark bm;
      if (!TryCaptureBookmarkForSpots(out bm))
        return new MapSaveStatus
        {
          CanSave = false,
          Message = "Can't read this scene."
        };

      return new MapSaveStatus { CanSave = true, Message = "" };
    }

    public static bool CanSaveOnCurrentMap => GetCurrentMapSaveStatus().CanSave;

    public static string GetMapTitleLine()
    {
      MapChanger.MapBookmark bm;
      if (MapChanger.TryCaptureRideMapIdentity(out bm) && !string.IsNullOrEmpty(bm.DisplayLabel))
      {
        if (bm.Kind == MapChanger.MapBookmarkKind.FreeRideSeed
            && !string.IsNullOrEmpty(bm.SessionSeed))
          return PrettyLabel(bm.DisplayLabel) + " - " + CompactSeedForDisplay(bm.SessionSeed);
        return PrettyLabel(bm.DisplayLabel);
      }

      string scene = MapChanger.GetCurrentSceneName();
      if (!MapChanger.IsSystemScene(scene))
        return PrettyLabel(MapChanger.SceneToDisplayLabel(scene));

      if (_supported && !string.IsNullOrEmpty(_mapLabel))
        return PrettyLabel(_mapLabel);
      return "—";
    }

    public static MapGoStatus GetMapGoStatus(string mapKey)
    {
      MapGoStatus st = new MapGoStatus { CanGo = true, Hint = "" };
      if (string.IsNullOrEmpty(mapKey))
      {
        st.CanGo = false;
        st.Hint = "Unknown map";
        return st;
      }

      if (mapKey.StartsWith("scene_", System.StringComparison.Ordinal))
      {
        if (MapChanger.InWorkshopLevel())
          return st;

        SpotFileEntry fe = FindSpotByMapKey(mapKey);
        if (fe != null)
        {
          if (!string.IsNullOrEmpty(fe.workshopPath) || !string.IsNullOrEmpty(fe.mapLabel))
            return st;
          if (fe.kind == (int)MapChanger.MapBookmarkKind.SceneOnly && fe.customSeed == 0)
            return st;
          if (!string.IsNullOrEmpty(fe.sceneName))
            return st;
        }

        int sceneSeed;
        if (TryResolveSeedForSceneKey(mapKey, out sceneSeed) && sceneSeed != 0)
          return st;

        st.CanGo = false;
        st.Hint = "Unavailable — subscribe in Mod.io";
        return st;
      }
      return st;
    }

    public static string GetMapAvailabilityLine(string mapKey)
    {
      MapGoStatus st = GetMapGoStatus(mapKey);
      return st.CanGo ? "" : st.Hint;
    }

    public static string GetMapLabelForKey(string mapKey)
    {
      if (mapKey == FreerideSectionKey)
        return "Free Ride";
      if (mapKey == ModIoSectionKey)
        return "Mod.io Maps";
      SpotFileEntry fe = FindSpotByMapKey(mapKey);
      if (fe != null && !string.IsNullOrEmpty(fe.mapLabel))
        return PrettyLabel(fe.mapLabel);
      return PrettyLabel(ResolveLabelFromMapKey(mapKey));
    }

    public static string GetCanonicalGroupKey(string mapKey)
    {
      if (IsModIoMapKey(mapKey))
        return ModIoSectionKey;
      if (!string.IsNullOrEmpty(mapKey) && mapKey.StartsWith("freeride_", System.StringComparison.Ordinal))
        return FreerideSectionKey;
      SpotFileEntry fe = FindSpotByMapKey(mapKey);
      if (fe != null && IsModIoSpot(fe))
        return ModIoSectionKey;
      if (fe != null)
        return MapKeyFromSpot(fe);
      return mapKey ?? "";
    }

    public static bool IsModIoMapKey(string mapKey)
    {
      if (string.IsNullOrEmpty(mapKey)) return false;
      if (mapKey == ModIoSectionKey) return true;
      if (mapKey.StartsWith("scene_", System.StringComparison.Ordinal))
      {
        SpotFileEntry fe = FindSpotByMapKey(mapKey);
        if (fe != null && LooksLikeWorkshopSpot(fe))
          return true;
      }
      return false;
    }

    private static bool IsModIoSpot(SpotFileEntry fe)
    {
      return LooksLikeWorkshopSpot(fe);
    }

    private static bool LooksLikeWorkshopSpot(SpotFileEntry fe)
    {
      if (fe == null) return false;
      if (!string.IsNullOrEmpty(fe.workshopPath)) return true;

      if (IsFreerideMapKey(fe.mapKey))
        return false;
      if (fe.kind == (int)MapChanger.MapBookmarkKind.FreeRideSeed)
        return false;
      if (fe.customSeed != 0)
        return false;

      if (!string.IsNullOrEmpty(fe.mapLabel))
      {
        if (fe.mapLabel.IndexOf("Seed", System.StringComparison.OrdinalIgnoreCase) >= 0)
          return false;
        int seed;
        if (MapChanger.TryFindSeedForMapName(fe.mapLabel, out seed) && seed != 0)
          return false;
      }

      if (fe.kind != (int)MapChanger.MapBookmarkKind.SceneOnly)
        return false;

      if (!string.IsNullOrEmpty(fe.sceneName) && LooksLikeWorkshopSceneName(fe.sceneName))
        return true;

      if (fe.mapKey != null && fe.mapKey.StartsWith("scene_", System.StringComparison.Ordinal))
      {
        string part = fe.mapKey.Substring(6).Replace('_', ' ');
        return LooksLikeWorkshopSceneName(part);
      }

      return false;
    }

    public static bool IsFreerideMapKey(string mapKey)
    {
      return !string.IsNullOrEmpty(mapKey)
          && (mapKey == FreerideSectionKey || mapKey.StartsWith("freeride_", System.StringComparison.Ordinal));
    }

    public static bool HasSlot(int slot)
    {
      return slot >= 0 && slot < SlotCount && _slots[slot].Has;
    }

    public static string GetName(int slot)
    {
      if (slot < 0 || slot >= SlotCount) return "";
      if (_slots[slot].Has)
        return _slots[slot].Name;
      string staged = _slots[slot].Name;
      string defaultName = "Spot " + (slot + 1);
      if (!string.IsNullOrEmpty(staged) && staged != "Empty" && staged != defaultName)
        return staged;
      return "Empty";
    }

    public static bool Save(int slot)
    {
      if (slot < 0 || slot >= SlotCount) return false;

      MapSaveStatus saveSt = GetCurrentMapSaveStatus();
      if (!saveSt.CanSave)
      {
        ModLog.Feedback("[SavedLoc] " + saveSt.Message);
        return false;
      }

      MapChanger.MapBookmark bm;
      if (!TryCaptureBookmarkForSpots(out bm))
      {
        ModLog.Feedback("[SavedLoc] Could not read map info.");
        return false;
      }

      GameObject local = GameObject.Find("Player_Human");
      if (!UnityNull.Alive(local))
      {
        ModLog.Warn("[SavedLoc] Player_Human not found.");
        return false;
      }

      Vector3 pos;
      float ry;
      if (!TryGetPlayerRidePosition(local, out pos, out ry))
      {
        ModLog.Warn("[SavedLoc] Could not read rider position.");
        return false;
      }

      _supported = true;
      _currentMap = bm;
      _mapKey = MapKeyFromBookmark(bm);
      _mapLabel = bm.DisplayLabel ?? "";

      _slots[slot].Has = true;
      _slots[slot].X = pos.x;
      _slots[slot].Y = pos.y;
      _slots[slot].Z = pos.z;
      _slots[slot].RotY = ry;
      if (string.IsNullOrEmpty(_slots[slot].Name) || _slots[slot].Name == "Empty")
        _slots[slot].Name = "Spot " + (slot + 1);

      UpsertFileSpot(slot, bm, pos, ry);
      DeduplicateFileSpots();
      SaveFileToDisk();
      LoadSlotsForCurrentMap();

      ModLog.Feedback("[SavedLoc] Saved \"" + _slots[slot].Name + "\" on " + PrettyLabel(_mapLabel)
          + (bm.Kind == MapChanger.MapBookmarkKind.FreeRideSeed && !string.IsNullOrEmpty(bm.SessionSeed)
              ? " (seed " + bm.SessionSeed + ")"
              : " (seed " + bm.CustomSeed + ")"));
      ModLog.Debug("[SavedLoc] Pos " + pos.x.ToString("F1") + ", " + pos.y.ToString("F1")
          + ", " + pos.z.ToString("F1"));
      try { FavsPage.RefreshFavourites(); } catch { }
      try { FavouritesManager.InvokeRefresh(); } catch { }
      return true;
    }

    public static bool Teleport(int slot, bool withPerks = false)
    {
      SpotFileEntry spot = FindSpotForCurrentMap(slot);
      if (spot == null)
      {
        ModLog.Feedback("[SavedLoc] Slot " + (slot + 1) + " is empty on this map.");
        return false;
      }

      if (!withPerks)
        CrewPerkManager.ClearGoPlusPerks();

      if (IsSameMap(BookmarkFromSpot(spot), _currentMap))
      {
        if (TeleportSpot(spot))
        {
          if (withPerks)
            CrewPerkManager.ApplyToLocalPlayer();
          return true;
        }
        if (withPerks)
        {
          CrewPerkManager.ClearGoPlusPerks();
          CrewPerkManager.ArmForNextTravel();
        }
        SetPendingGo(spot);
        StartDeferredTeleportIfPending();
        return true;
      }

      GoToSavedSpot(MapKeyFromSpot(spot), slot, withPerks);
      return true;
    }

    public static void GoToSavedSpot(string mapKey, int slot, bool withPerks = false)
    {
      if (withPerks)
      {
        CrewPerkManager.ClearGoPlusPerks();
        CrewPerkManager.ArmForNextTravel();
      }
      else
      {
        CrewPerkManager.ClearGoPlusPerks();
        CrewPerkManager.Disarm();
      }

      SpotFileEntry spot = ResolveFreshSpot(FindSpot(mapKey, slot));
      if (spot == null)
      {
        ModLog.Feedback("[SavedLoc] Spot no longer saved.");
        return;
      }

      MapChanger.MapBookmark saved = BookmarkFromSpot(spot);
      MapChanger.MapBookmark live;
      MapChanger.TryCaptureRideMapIdentity(out live);

      if (IsSameMap(saved, live))
      {
        ClearPendingGo();
        if (TeleportSpot(spot))
        {
          ModLog.Feedback("[SavedLoc] Teleported to saved spot.");
          if (withPerks)
            CrewPerkManager.FinishTravelPerks();
        }
        else
        {
          SetPendingGo(spot);
          StartDeferredTeleportIfPending();
        }
        return;
      }

      if (TryLoadWorkshopSpot(spot))
      {
        SetPendingGo(spot);
        ModLog.Feedback("[SavedLoc] Loading map for saved spot...");
        StartDeferredTeleportIfPending();
        return;
      }

      if (LooksLikeWorkshopSpot(spot))
        return;

      if (!TryLoadMap(saved))
      {
        ModLog.Feedback("[SavedLoc] Could not load that map.");
        return;
      }

      SetPendingGo(spot);
      ModLog.Feedback("[SavedLoc] Loading map for saved spot...");
      StartDeferredTeleportIfPending();
    }

    private static bool TryLoadWorkshopSpot(SpotFileEntry spot)
    {
      if (spot == null || !LooksLikeWorkshopSpot(spot))
        return false;

      string scenePart = null;
      if (!string.IsNullOrEmpty(spot.sceneName))
        scenePart = MapChanger.SanitizeStoragePartPublic(spot.sceneName);

      return ModWorkshopLoader.TryDeferredWorkshopLoad(
          spot.mapLabel, spot.workshopPath, scenePart);
    }

    public static void Delete(int slot)
    {
      if (slot < 0 || slot >= SlotCount) return;
      _slots[slot].Has = false;
      _slots[slot].Name = "Spot " + (slot + 1);
      RemoveFileSpot(_mapKey, slot);
      SaveFileToDisk();
      ModLog.Feedback("[SavedLoc] Deleted slot " + (slot + 1));
      try { FavsPage.RefreshFavourites(); } catch { }
      try { FavouritesManager.InvokeRefresh(); } catch { }
    }

    public static void DeleteSavedSpot(string mapKey, int slot)
    {
      if (slot < 0 || slot >= SlotCount || string.IsNullOrEmpty(mapKey)) return;

      SpotFileEntry spot = FindSpot(mapKey, slot);
      if (spot == null)
      {
        ModLog.Feedback("[SavedLoc] Spot no longer saved.");
        return;
      }

      RemoveFileSpotByKey(mapKey, slot);

      if (HasPendingGo() && _pendingMapKey == mapKey && _pendingSlot == slot)
        ClearPendingGo();

      if (string.Equals(_mapKey, mapKey, System.StringComparison.Ordinal)
          || string.Equals(MapKeyFromSpot(spot), mapKey, System.StringComparison.Ordinal))
      {
        _slots[slot].Has = false;
        _slots[slot].Name = "Spot " + (slot + 1);
      }

      SaveFileToDisk();
      ModLog.Feedback("[SavedLoc] Removed \"" + (spot.name ?? "spot") + "\".");
      try { FavsPage.RefreshFavourites(); } catch { }
      try { FavouritesManager.InvokeRefresh(); } catch { }
    }

    public static bool IsDefaultSpotName(string name, int slot)
    {
      if (string.IsNullOrEmpty(name)) return true;
      if (name == "Empty") return true;
      return name == "Spot " + (slot + 1);
    }

    public static void SetName(int slot, string name)
    {
      if (slot < 0 || slot >= SlotCount) return;
      if (string.IsNullOrEmpty(name)) return;
      string cleaned = name.Trim();
      if (cleaned.Length > 32) cleaned = cleaned.Substring(0, 32);
      _slots[slot].Name = cleaned;

      bool saved = ApplySpotNameToFile(slot, cleaned);
      if (saved)
        SaveFileToDisk();
      try { FavsPage.RefreshFavourites(); } catch { }
      try { FavouritesManager.InvokeRefresh(); } catch { }
    }

    public static SavedSpotRef[] GetAllSavedSpots()
    {
      var list = new System.Collections.Generic.List<SavedSpotRef>();
      System.Collections.Generic.List<SpotFileEntry> collapsed = CollapseSpotEntries(_fileSpots);

      for (int i = 0; i < collapsed.Count; i++)
      {
        SpotFileEntry fe = collapsed[i];
        if (fe == null) continue;
        string mapKey = MapKeyFromSpot(fe);

        string spotName = fe.name;
        if (string.IsNullOrEmpty(spotName))
          spotName = "Spot " + (fe.slot + 1);

        list.Add(new SavedSpotRef
        {
          MapKey = mapKey,
          MapLabel = PrettyLabel(fe.mapLabel),
          Slot = fe.slot,
          SpotName = spotName,
          MapCanGo = GetMapGoStatus(mapKey).CanGo,
          MapAvailabilityHint = GetMapAvailabilityLine(mapKey)
        });
      }

      list.Sort((a, b) =>
      {
        int c = string.Compare(a.MapLabel, b.MapLabel, System.StringComparison.OrdinalIgnoreCase);
        if (c != 0) return c;
        c = string.Compare(a.SpotName, b.SpotName, System.StringComparison.OrdinalIgnoreCase);
        if (c != 0) return c;
        return a.Slot.CompareTo(b.Slot);
      });
      return list.ToArray();
    }

    // ── Map identity ───────────────────────────────────────────────

    private static bool TryCaptureBookmarkForSpots(out MapChanger.MapBookmark bm)
    {
      bm = new MapChanger.MapBookmark();
      if (MapChanger.TryCaptureRideMapIdentity(out bm))
      {
        NormalizeBookmarkForStorage(ref bm);
        if (bm.Valid && (bm.CustomSeed != 0
            || bm.Kind == MapChanger.MapBookmarkKind.FreeRideSeed
            || bm.Kind == MapChanger.MapBookmarkKind.SceneOnly
            || !string.IsNullOrEmpty(bm.SceneName)))
          return true;
      }

      string scene = MapChanger.GetCurrentSceneName();
      if (MapChanger.IsSystemScene(scene))
        return false;

      int parkSeed;
      if (MapChanger.TryGetLevelCustomSeed(out parkSeed) && parkSeed != 0)
      {
        bm.Valid = true;
        bm.CustomSeed = parkSeed;
        bm.Kind = MapChanger.MapBookmarkKind.SeedWorld;
        bm.SceneName = scene;
        bm.DisplayLabel = MapChanger.FindMapNameForSeedPublic(parkSeed);
        NormalizeBookmarkForStorage(ref bm);
        return bm.CustomSeed != 0;
      }

      return false;
    }

    private static void NormalizeBookmarkForStorage(ref MapChanger.MapBookmark bm)
    {
      if (!bm.Valid) return;

      if (bm.Kind == MapChanger.MapBookmarkKind.FreeRideSeed)
      {
        MapChanger.CacheCurrentLevelSeed();
        if (MapChanger.TryGetSessionSeedString(out string liveSession))
          bm.SessionSeed = liveSession;
        bm.SessionTypeInt = MapChanger.GetFreerideSandboxSessionTypeIntPublic();
        if (bm.WorldInt > 0 && string.IsNullOrEmpty(bm.DisplayLabel))
          bm.DisplayLabel = MapChanger.GetWorldDisplayName(bm.WorldInt);
        return;
      }

      if (!string.IsNullOrEmpty(bm.SceneName))
      {
        if (MapChanger.InWorkshopLevel() && !string.IsNullOrEmpty(bm.DisplayLabel))
        {
          bm.Kind = MapChanger.MapBookmarkKind.SceneOnly;
          bm.CustomSeed = 0;
          return;
        }
      }

      // Prefer official park name from seed / scene — never keep a biome label like "Highlands"
      // when the actual scene is a named park (e.g. ragesquid_riot).
      if (bm.CustomSeed != 0)
      {
        string fromSeed = MapChanger.FindMapNameForSeedPublic(bm.CustomSeed);
        if (!string.IsNullOrEmpty(fromSeed))
        {
          bm.DisplayLabel = fromSeed;
          bm.Kind = MapChanger.MapBookmarkKind.SeedWorld;
          return;
        }
      }

      string fromScene = MapChanger.ResolveRideDisplayLabel(bm.SceneName, null, bm.CustomSeed);
      if (!string.IsNullOrEmpty(fromScene)
          && (string.IsNullOrEmpty(bm.DisplayLabel)
              || MapChanger.IsBaseWorldNamePublic(bm.DisplayLabel)))
      {
        bm.DisplayLabel = fromScene;
      }

      if (!string.IsNullOrEmpty(bm.DisplayLabel)
          && !MapChanger.IsBaseWorldNamePublic(bm.DisplayLabel))
      {
        int byName;
        if (MapChanger.TryFindSeedForMapName(bm.DisplayLabel, out byName) && byName != 0)
        {
          bm.CustomSeed = byName;
          bm.Kind = MapChanger.MapBookmarkKind.SeedWorld;
          bm.DisplayLabel = MapChanger.FindMapNameForSeedPublic(byName) ?? bm.DisplayLabel;
          return;
        }
      }

      int liveSeed;
      if (MapChanger.TryGetLevelCustomSeed(out liveSeed) && liveSeed != 0
          && MapChanger.CanResolveLevelSeed(liveSeed))
      {
        bm.CustomSeed = liveSeed;
        bm.Kind = MapChanger.MapBookmarkKind.SeedWorld;
        string liveName = MapChanger.FindMapNameForSeedPublic(liveSeed);
        if (!string.IsNullOrEmpty(liveName))
          bm.DisplayLabel = liveName;
        else if (string.IsNullOrEmpty(bm.DisplayLabel))
          bm.DisplayLabel = "Seed " + liveSeed;
        return;
      }

      PromoteBookmarkToSeed(ref bm);

      if (bm.CustomSeed != 0)
      {
        string fromSeed = MapChanger.FindMapNameForSeedPublic(bm.CustomSeed);
        if (!string.IsNullOrEmpty(fromSeed))
          bm.DisplayLabel = fromSeed;
      }
    }

    private static bool IsSameMap(MapChanger.MapBookmark a, MapChanger.MapBookmark b)
    {
      if (!a.Valid || !b.Valid) return false;

      if (a.Kind == MapChanger.MapBookmarkKind.FreeRideSeed
          || b.Kind == MapChanger.MapBookmarkKind.FreeRideSeed)
      {
        return a.Kind == b.Kind
            && a.WorldInt > 0 && a.WorldInt == b.WorldInt
            && !string.IsNullOrEmpty(a.SessionSeed)
            && a.SessionSeed == b.SessionSeed;
      }

      MapChanger.MapBookmark aa = a;
      MapChanger.MapBookmark bb = b;
      PromoteBookmarkToSeed(ref aa);
      PromoteBookmarkToSeed(ref bb);

      if (aa.CustomSeed != 0 || bb.CustomSeed != 0)
      {
        if (aa.CustomSeed != 0 && bb.CustomSeed != 0)
          return aa.CustomSeed == bb.CustomSeed;
        return false;
      }

      if (aa.WorldInt > 0 && bb.WorldInt > 0 && aa.WorldInt == bb.WorldInt)
        return true;

      if (aa.Kind == MapChanger.MapBookmarkKind.SceneOnly
          && bb.Kind == MapChanger.MapBookmarkKind.SceneOnly)
      {
        string workshopSceneA = SanitizeScene(aa.SceneName);
        string workshopSceneB = SanitizeScene(bb.SceneName);
        if (!string.IsNullOrEmpty(workshopSceneA) && workshopSceneA == workshopSceneB)
          return true;
      }

      if (!string.IsNullOrEmpty(aa.DisplayLabel) && !string.IsNullOrEmpty(bb.DisplayLabel))
      {
        if (NormalizeLabel(aa.DisplayLabel) == NormalizeLabel(bb.DisplayLabel))
          return true;
        if (WorkshopLabelsMatch(aa.DisplayLabel, bb.DisplayLabel))
          return true;
      }

      string sceneA = SanitizeScene(aa.SceneName);
      string sceneB = SanitizeScene(bb.SceneName);
      if (!string.IsNullOrEmpty(sceneA) && sceneA == sceneB)
        return true;

      return false;
    }

    private static bool TryLoadMap(MapChanger.MapBookmark bm)
    {
      if (!bm.Valid) return false;

      MapChanger.MapBookmark load = bm;
      if (load.Kind == MapChanger.MapBookmarkKind.FreeRideSeed)
        return MapChanger.LoadBookmark(load);

      if (ModWorkshopLoader.IsBookmarkSubscribed(load))
      {
        load.Kind = MapChanger.MapBookmarkKind.SceneOnly;
        load.CustomSeed = 0;
        return ModWorkshopLoader.TryLoadBookmark(load);
      }

      PromoteBookmarkToSeed(ref load);

      if (load.Kind == MapChanger.MapBookmarkKind.BaseWorld && load.WorldInt > 0)
        return MapChanger.LoadBookmark(load);

      if (load.CustomSeed != 0)
      {
        load.Kind = MapChanger.MapBookmarkKind.SeedWorld;
        return MapChanger.LoadBookmark(load);
      }

      if (load.Kind == MapChanger.MapBookmarkKind.SceneOnly)
        return MapChanger.LoadBookmark(load);

      if (load.WorldInt > 0)
      {
        load.Kind = MapChanger.MapBookmarkKind.BaseWorld;
        return MapChanger.LoadBookmark(load);
      }

      if (!string.IsNullOrEmpty(load.DisplayLabel)
          && MapChanger.TryGetWorldIntForName(load.DisplayLabel, out int wi))
      {
        load.Kind = MapChanger.MapBookmarkKind.BaseWorld;
        load.WorldInt = wi;
        return MapChanger.LoadBookmark(load);
      }

      if (!string.IsNullOrEmpty(load.DisplayLabel)
          && MapChanger.TryFindSeedForMapName(load.DisplayLabel, out int parkSeed) && parkSeed != 0)
      {
        load.CustomSeed = parkSeed;
        load.Kind = MapChanger.MapBookmarkKind.SeedWorld;
        return MapChanger.LoadBookmark(load);
      }

      return false;
    }

    /// <summary>Built-in bike parks (e.g. Mt Palumbo) share scene names — resolve seed before mod.io.</summary>
    private static void PromoteBookmarkToSeed(ref MapChanger.MapBookmark bm)
    {
      if (bm.Kind == MapChanger.MapBookmarkKind.FreeRideSeed)
        return;

      if (bm.Kind == MapChanger.MapBookmarkKind.SceneOnly && bm.CustomSeed == 0)
        return;

      if (!string.IsNullOrEmpty(bm.SceneName) && LooksLikeWorkshopSceneName(bm.SceneName))
        return;

      if (bm.CustomSeed != 0 && MapChanger.IsKnownMapSeed(bm.CustomSeed)
          && MapChanger.CanResolveLevelSeed(bm.CustomSeed)) return;

      MapChanger.BuildMapList();
      int seed;

      if (!string.IsNullOrEmpty(bm.DisplayLabel)
          && MapChanger.TryFindSeedForMapName(bm.DisplayLabel, out seed) && seed != 0)
      {
        bm.CustomSeed = seed;
        bm.Kind = MapChanger.MapBookmarkKind.SeedWorld;
        return;
      }

      if (!string.IsNullOrEmpty(bm.SceneName))
      {
        if (MapChanger.TryFindSeedForMapName(
                MapChanger.SceneToDisplayLabel(bm.SceneName), out seed) && seed != 0
            || MapChanger.TryFindSeedForMapName(bm.SceneName, out seed) && seed != 0)
        {
          bm.CustomSeed = seed;
          bm.Kind = MapChanger.MapBookmarkKind.SeedWorld;
          return;
        }

        string safe = MapChanger.SanitizeStoragePartPublic(bm.SceneName);
        if (TryResolveSeedForSceneKey("scene_" + safe, out seed) && seed != 0)
        {
          bm.CustomSeed = seed;
          bm.Kind = MapChanger.MapBookmarkKind.SeedWorld;
        }
      }
    }

    private static MapChanger.MapBookmark BookmarkFromSpot(SpotFileEntry fe)
    {
      EnsureSpotMetadata(fe);
      MapChanger.MapBookmark bm = new MapChanger.MapBookmark
      {
        Valid = true,
        Kind = (MapChanger.MapBookmarkKind)fe.kind,
        DisplayLabel = fe.mapLabel,
        CustomSeed = fe.customSeed,
        WorldInt = fe.worldInt,
        SceneName = fe.sceneName,
        SessionSeed = fe.sessionSeed ?? "",
        SessionTypeInt = fe.sessionTypeInt
      };
      if (bm.Kind == MapChanger.MapBookmarkKind.None)
      {
        if (!string.IsNullOrEmpty(fe.sessionSeed)
            || (fe.mapKey != null && fe.mapKey.StartsWith("freeride_", System.StringComparison.Ordinal)))
          bm.Kind = MapChanger.MapBookmarkKind.FreeRideSeed;
        else if (fe.customSeed != 0)
          bm.Kind = MapChanger.MapBookmarkKind.SeedWorld;
        else if (fe.worldInt > 0)
          bm.Kind = MapChanger.MapBookmarkKind.BaseWorld;
        else
          bm.Kind = MapChanger.MapBookmarkKind.SceneOnly;
      }
      return bm;
    }

    private static string MapKeyFromBookmark(MapChanger.MapBookmark bm)
    {
      if (bm.Kind == MapChanger.MapBookmarkKind.FreeRideSeed
          && bm.WorldInt > 0 && !string.IsNullOrEmpty(bm.SessionSeed))
        return "freeride_" + bm.WorldInt + "_" + bm.SessionSeed;
      if (!string.IsNullOrEmpty(bm.DisplayLabel))
        return "park_" + NormalizeLabel(bm.DisplayLabel);
      if (bm.CustomSeed != 0 && MapChanger.CanResolveLevelSeed(bm.CustomSeed))
        return "seed_" + bm.CustomSeed;
      if (bm.WorldInt > 0)
        return "name_" + (bm.DisplayLabel ?? MapChanger.GetWorldDisplayName(bm.WorldInt)).Replace(' ', '_');
      string scene = SanitizeScene(bm.SceneName);
      if (!string.IsNullOrEmpty(scene))
        return "scene_" + scene;
      return "scene_unknown";
    }

    private static string MapKeyFromSpot(SpotFileEntry fe)
    {
      EnsureSpotMetadata(fe);
      if (!string.IsNullOrEmpty(fe.mapKey))
        return fe.mapKey;
      return MapKeyFromBookmark(BookmarkFromSpot(fe));
    }

  private static void EnsureSpotMetadata(SpotFileEntry fe, bool allowModCatalogLookup = true)
  {
    if (fe.mapKey != null && fe.mapKey.StartsWith("freeride_", System.StringComparison.Ordinal))
    {
      int worldInt;
      string keySeed;
      if (TryParseFreerideMapKey(fe.mapKey, out worldInt, out keySeed))
      {
        if (worldInt > 0)
          fe.worldInt = worldInt;
        if (string.IsNullOrEmpty(fe.sessionSeed) && !string.IsNullOrEmpty(keySeed))
          fe.sessionSeed = keySeed;
        else if (!string.IsNullOrEmpty(fe.sessionSeed)
            && !string.IsNullOrEmpty(keySeed)
            && keySeed != fe.sessionSeed)
          fe.mapKey = "freeride_" + fe.worldInt + "_" + fe.sessionSeed;
      }
      fe.kind = (int)MapChanger.MapBookmarkKind.FreeRideSeed;
      if (string.IsNullOrEmpty(fe.mapLabel))
        fe.mapLabel = FormatFreerideSpotLabel(fe.worldInt, fe.sessionSeed, null);
      else
        fe.mapLabel = FormatFreerideSpotLabel(fe.worldInt, fe.sessionSeed, fe.mapLabel);
      return;
    }

    if (fe.mapKey != null && fe.mapKey.StartsWith("scene_", System.StringComparison.Ordinal))
    {
      fe.kind = (int)MapChanger.MapBookmarkKind.SceneOnly;
      fe.customSeed = 0;
      if (string.IsNullOrEmpty(fe.mapLabel) && fe.mapKey.Length > 6)
        fe.mapLabel = PrettyLabel(MapChanger.SceneToDisplayLabel(fe.mapKey.Substring(6)));
      if (!allowModCatalogLookup
          || (!string.IsNullOrEmpty(fe.mapLabel) && !string.IsNullOrEmpty(fe.workshopPath)))
        return;
      if (MapChanger.InWorkshopLevel())
        return;

      if (!allowModCatalogLookup)
        return;

      string modName;
      if (ModWorkshopLoader.TryGetDisplayNameForSceneKey(fe.mapKey, out modName))
      {
        fe.mapLabel = modName;
        object workshopMod = ModWorkshopLoader.FindModForBookmarkPublic(BookmarkFromSpot(fe));
        if (workshopMod != null)
          fe.workshopPath = ModWorkshopLoader.NormalizeWorkshopPathForSave(
              ModWorkshopLoader.ReadModPathPublic(workshopMod));
        else
        {
          string path;
          if (ModWorkshopLoader.TryGetWorkshopPathForBookmark(BookmarkFromSpot(fe), out path))
            fe.workshopPath = path != null
                ? ModWorkshopLoader.NormalizeWorkshopPathForSave(path) : path;
        }
        return;
      }
    }

    if (!string.IsNullOrEmpty(fe.sceneName))
    {
      if (MapChanger.InWorkshopLevel())
        return;

      string workshopKey = "scene_" + SanitizeScene(fe.sceneName);

      if (!allowModCatalogLookup)
      {
        if (fe.mapKey == null || !fe.mapKey.StartsWith("scene_", System.StringComparison.Ordinal))
        {
          if (workshopKey.Length > 6)
          {
            fe.mapKey = workshopKey;
            fe.kind = (int)MapChanger.MapBookmarkKind.SceneOnly;
            fe.customSeed = 0;
            if (string.IsNullOrEmpty(fe.mapLabel))
              fe.mapLabel = PrettyLabel(MapChanger.SceneToDisplayLabel(fe.sceneName));
          }
        }
        return;
      }

      string modName;
      if (ModWorkshopLoader.TryGetDisplayNameForSceneKey(workshopKey, out modName))
      {
        fe.mapLabel = modName;
        fe.mapKey = workshopKey;
        fe.kind = (int)MapChanger.MapBookmarkKind.SceneOnly;
        fe.customSeed = 0;
        object workshopMod = ModWorkshopLoader.FindModForBookmarkPublic(BookmarkFromSpot(fe));
        if (workshopMod != null)
          fe.workshopPath = ModWorkshopLoader.NormalizeWorkshopPathForSave(
              ModWorkshopLoader.ReadModPathPublic(workshopMod));
        else
        {
          string path;
          if (ModWorkshopLoader.TryGetWorkshopPathForBookmark(BookmarkFromSpot(fe), out path))
            fe.workshopPath = path != null
                ? ModWorkshopLoader.NormalizeWorkshopPathForSave(path) : path;
        }
        return;
      }
    }

    // Repair biome mislabels: scene "ragesquid_riot" saved as "Highlands"
    if (!string.IsNullOrEmpty(fe.sceneName))
    {
      string fromScene = MapChanger.ResolveRideDisplayLabel(fe.sceneName, null, fe.customSeed);
      if (!string.IsNullOrEmpty(fromScene)
          && (string.IsNullOrEmpty(fe.mapLabel) || MapChanger.IsBaseWorldNamePublic(fe.mapLabel))
          && !MapChanger.IsBaseWorldNamePublic(fromScene))
      {
        fe.mapLabel = fromScene;
        fe.mapKey = "park_" + NormalizeLabel(fromScene);
        fe.kind = (int)MapChanger.MapBookmarkKind.SeedWorld;
      }
    }

    if (fe.customSeed != 0)
    {
      string fromSeed = MapChanger.FindMapNameForSeedPublic(fe.customSeed);
      if (!string.IsNullOrEmpty(fromSeed)
          && (string.IsNullOrEmpty(fe.mapLabel) || MapChanger.IsBaseWorldNamePublic(fe.mapLabel)))
      {
        fe.mapLabel = fromSeed;
        fe.mapKey = "park_" + NormalizeLabel(fromSeed);
        fe.kind = (int)MapChanger.MapBookmarkKind.SeedWorld;
      }
    }

    if (!string.IsNullOrEmpty(fe.mapLabel))
    {
      string stableKey = "park_" + NormalizeLabel(fe.mapLabel);
      if (string.IsNullOrEmpty(fe.mapKey)
          || fe.mapKey.StartsWith("seed_", System.StringComparison.Ordinal)
          || fe.mapKey.StartsWith("name_", System.StringComparison.Ordinal)
          || fe.mapKey.StartsWith("scene_", System.StringComparison.Ordinal))
        fe.mapKey = stableKey;

      int byName;
      if (MapChanger.TryFindSeedForMapName(fe.mapLabel, out byName) && byName != 0)
      {
        if (fe.customSeed != byName)
          ApplySeedToSpotFile(fe, byName);
        return;
      }
    }

    if (fe.customSeed != 0)
    {
      if (fe.kind == 0)
        fe.kind = (int)MapChanger.MapBookmarkKind.SeedWorld;
      if (string.IsNullOrEmpty(fe.mapKey))
        fe.mapKey = MapChanger.CanResolveLevelSeed(fe.customSeed)
            ? "seed_" + fe.customSeed
            : "name_" + NormalizeLabel(fe.mapLabel ?? ("Seed " + fe.customSeed));
      return;
    }

    MapChanger.BuildMapList();
    int seed;

    if (!string.IsNullOrEmpty(fe.mapKey) && TryResolveSeedForSceneKey(fe.mapKey, out seed) && seed != 0)
    {
      ApplySeedToSpotFile(fe, seed);
      return;
    }

    if (!string.IsNullOrEmpty(fe.sceneName))
    {
      if (MapChanger.TryFindSeedForMapName(
              MapChanger.SceneToDisplayLabel(fe.sceneName), out seed) && seed != 0
          || MapChanger.TryFindSeedForMapName(fe.sceneName, out seed) && seed != 0)
      {
        ApplySeedToSpotFile(fe, seed);
        return;
      }
    }

    if (fe.kind == 0 && !string.IsNullOrEmpty(fe.mapKey))
    {
      MapChanger.MapBookmark legacy = BookmarkFromLegacyKey(fe.mapKey, fe.mapLabel);
      fe.kind = (int)legacy.Kind;
      fe.customSeed = legacy.CustomSeed;
      fe.worldInt = legacy.WorldInt;
      if (string.IsNullOrEmpty(fe.sceneName))
        fe.sceneName = legacy.SceneName;
      if (string.IsNullOrEmpty(fe.mapLabel))
        fe.mapLabel = legacy.DisplayLabel;
      if (fe.customSeed != 0)
      {
        if (!string.IsNullOrEmpty(fe.mapLabel))
          fe.mapKey = "park_" + NormalizeLabel(fe.mapLabel);
        else
          fe.mapKey = "seed_" + fe.customSeed;
      }
    }
  }

  private static void ApplySeedToSpotFile(SpotFileEntry fe, int seed)
  {
    fe.customSeed = seed;
    fe.kind = (int)MapChanger.MapBookmarkKind.SeedWorld;
    if (string.IsNullOrEmpty(fe.mapKey))
    {
      if (!string.IsNullOrEmpty(fe.mapLabel))
        fe.mapKey = "park_" + NormalizeLabel(fe.mapLabel);
      else
        fe.mapKey = "seed_" + seed;
    }
    if (string.IsNullOrEmpty(fe.mapLabel))
      fe.mapLabel = MapChanger.FindMapNameForSeedPublic(seed) ?? ("Seed " + seed);
  }

    private static MapChanger.MapBookmark BookmarkFromLegacyKey(string mapKey, string label)
    {
      MapChanger.MapBookmark bm = new MapChanger.MapBookmark { Valid = true };
      int seed;
      if (MapChanger.TryParseSeedFromStorageKey(mapKey, out seed) && seed != 0)
      {
        bm.Kind = MapChanger.MapBookmarkKind.SeedWorld;
        bm.CustomSeed = seed;
        bm.DisplayLabel = MapChanger.FindMapNameForSeedPublic(seed) ?? label ?? ("Seed " + seed);
        return bm;
      }

      if (mapKey.StartsWith("freeride_", System.StringComparison.Ordinal))
      {
        int worldInt;
        string sessionSeed;
        if (TryParseFreerideMapKey(mapKey, out worldInt, out sessionSeed))
        {
          bm.Kind = MapChanger.MapBookmarkKind.FreeRideSeed;
          bm.WorldInt = worldInt;
          bm.SessionSeed = sessionSeed;
          bm.DisplayLabel = MapChanger.GetWorldDisplayName(worldInt);
          if (!string.IsNullOrEmpty(label))
            bm.DisplayLabel = label;
          return bm;
        }
      }

      if (mapKey.StartsWith("park_", System.StringComparison.Ordinal))
      {
        bm.DisplayLabel = label ?? PrettyLabel(mapKey.Substring(5));
        if (MapChanger.TryFindSeedForMapName(bm.DisplayLabel, out seed) && seed != 0)
        {
          bm.Kind = MapChanger.MapBookmarkKind.SeedWorld;
          bm.CustomSeed = seed;
        }
        return bm;
      }

      if (mapKey.StartsWith("name_", System.StringComparison.Ordinal))
      {
        string worldName = mapKey.Substring(5).Replace('_', ' ');
        bm.DisplayLabel = label ?? PrettyLabel(worldName);
        if (MapChanger.TryFindSeedForMapName(bm.DisplayLabel, out seed) && seed != 0)
        {
          bm.Kind = MapChanger.MapBookmarkKind.SeedWorld;
          bm.CustomSeed = seed;
        }
        else if (MapChanger.TryGetWorldIntForName(worldName, out int wi))
        {
          bm.Kind = MapChanger.MapBookmarkKind.BaseWorld;
          bm.WorldInt = wi;
        }
        return bm;
      }

      if (mapKey.StartsWith("scene_", System.StringComparison.Ordinal))
      {
        bm.Kind = MapChanger.MapBookmarkKind.SceneOnly;
        bm.SceneName = mapKey.Substring(6);
        bm.DisplayLabel = label ?? MapChanger.SceneToDisplayLabel(bm.SceneName);
        if (TryResolveSeedForSceneKey(mapKey, out seed) && seed != 0)
        {
          bm.CustomSeed = seed;
          bm.Kind = MapChanger.MapBookmarkKind.SeedWorld;
        }
        return bm;
      }

      bm.DisplayLabel = label ?? mapKey;
      return bm;
    }

    private static string ResolveLabelFromMapKey(string mapKey)
    {
      return BookmarkFromLegacyKey(mapKey, null).DisplayLabel ?? mapKey;
    }

    private static bool TryResolveSeedForSceneKey(string sceneStorageKey, out int seed)
    {
      seed = 0;
      if (!sceneStorageKey.StartsWith("scene_", System.StringComparison.Ordinal))
        return false;
      string scenePart = sceneStorageKey.Substring(6);
      if (MapChanger.TryFindSeedForMapName(MapChanger.SceneToDisplayLabel(scenePart), out seed))
        return true;
      return MapChanger.TryFindSeedForMapName(scenePart, out seed);
    }

    // ── Teleport ───────────────────────────────────────────────────

    private static bool HasPendingGo()
    {
      return !string.IsNullOrEmpty(_pendingMapKey) || _pendingSpot != null;
    }

    private static void SetPendingGo(SpotFileEntry spot)
    {
      if (spot == null) return;
      _pendingSpot = spot;
      _pendingMapKey = MapKeyFromSpot(spot);
      _pendingSlot = spot.slot;
      _pendingGoRunning = false;
    }

    public static bool TryGetPendingGoWorldPosition(out Vector3 pos)
    {
      pos = Vector3.zero;
      SpotFileEntry spot = GetPendingSpot();
      if (spot == null) return false;
      pos = new Vector3(spot.x, spot.y, spot.z);
      return true;
    }

    private static SpotFileEntry GetPendingSpot()
    {
      if (!string.IsNullOrEmpty(_pendingMapKey) && _pendingSlot >= 0)
        return ResolveFreshSpot(FindSpot(_pendingMapKey, _pendingSlot));
      return ResolveFreshSpot(_pendingSpot);
    }

    private static void StartDeferredTeleportIfPending()
    {
      if (!HasPendingGo() || _pendingGoRunning) return;
      _pendingGoRunning = true;
      MelonCoroutines.Start(DeferredTeleportRoutine());
    }

    private static bool IsRiderPresent()
    {
      GameObject local = GameObject.Find("Player_Human");
      if (!UnityNull.Alive(local) || !local.activeInHierarchy) return false;

      Vehicle vehicle = local.GetComponent<Vehicle>();
      if ((object)vehicle == null) return false;

      Rigidbody rb = vehicle.GetComponent<Rigidbody>();
      if ((object)rb == null)
        rb = vehicle.GetComponentInChildren<Rigidbody>();
      return (object)rb != null && rb.gameObject.activeInHierarchy;
    }

    private static bool IsRiderReadyForTeleport()
    {
      return IsRiderPresent() && StatsManager.ReadyForAutoLoad();
    }

    private static bool IsRiderReady()
    {
      if (!IsRiderPresent()) return false;
      if (StatsManager.ReadyForAutoLoad()) return true;
      return MapChanger.IsSessionStarted();
    }

    private static bool IsWorkshopPendingSpot(SpotFileEntry spot)
    {
      return spot != null && LooksLikeWorkshopSpot(spot);
    }

    private static bool IsTeleportTerrainReady(Vector3 dest)
    {
      if (!MapChanger.InWorkshopLevel())
        return true;
      if (!MapChanger.IsWorkshopWorldGeometryReady())
        return false;
      return MapChanger.IsGroundColliderNear(dest);
    }

    private static int GetReadyStableTicks(MapChanger.MapBookmark target)
    {
      if (target.Kind == MapChanger.MapBookmarkKind.SceneOnly && target.CustomSeed == 0)
        return 10;
      return 3;
    }

    private static bool IsMapReadyForPendingGo(MapChanger.MapBookmark target, out MapChanger.MapBookmark live)
    {
      live = new MapChanger.MapBookmark();
      string scene = MapChanger.GetCurrentSceneName();
      if (MapChanger.IsSystemScene(scene)) return false;

      if (target.Kind == MapChanger.MapBookmarkKind.SceneOnly && target.CustomSeed == 0)
      {
        if (!MapChanger.InWorkshopLevel())
          return false;
        if (string.IsNullOrEmpty(scene))
          return false;
        if (!MapChanger.IsWorkshopRidePlayable())
          return false;
        if (!string.IsNullOrEmpty(target.SceneName)
            && SanitizeScene(scene) != SanitizeScene(target.SceneName))
          return false;
      }

      MapChanger.MapBookmark captured;
      if (MapChanger.TryCaptureRideMapIdentity(out captured))
      {
        NormalizeBookmarkForStorage(ref captured);
        if (IsSameMap(target, captured))
        {
          live = captured;
          return true;
        }
      }

      if (target.Kind == MapChanger.MapBookmarkKind.FreeRideSeed
          && !string.IsNullOrEmpty(target.SessionSeed))
      {
        string liveSession;
        if (MapChanger.TryGetSessionSeedString(out liveSession)
            && MapChanger.FreerideSeedsMatch(target.SessionSeed, liveSession, target.WorldInt))
        {
          live = target;
          live.Valid = true;
          live.SceneName = scene;
          return true;
        }
      }

      if (target.CustomSeed != 0)
      {
        int liveSeed;
        if (MapChanger.TryGetLevelCustomSeed(out liveSeed) && liveSeed == target.CustomSeed)
        {
          live.Valid = true;
          live.Kind = MapChanger.MapBookmarkKind.SeedWorld;
          live.CustomSeed = liveSeed;
          live.SceneName = scene;
          live.DisplayLabel = target.DisplayLabel;
          return true;
        }
      }

      if (!string.IsNullOrEmpty(target.SceneName)
          && SanitizeScene(scene) == SanitizeScene(target.SceneName))
      {
        live = target;
        live.Valid = true;
        live.SceneName = scene;
        return true;
      }

      if (!string.IsNullOrEmpty(target.DisplayLabel))
      {
        int liveSeed;
        if (MapChanger.TryGetLevelCustomSeed(out liveSeed) && liveSeed != 0)
        {
          string liveLabel = MapChanger.FindMapNameForSeedPublic(liveSeed);
          if (NormalizeLabel(liveLabel) == NormalizeLabel(target.DisplayLabel))
          {
            live.Valid = true;
            live.Kind = MapChanger.MapBookmarkKind.SeedWorld;
            live.CustomSeed = liveSeed;
            live.DisplayLabel = liveLabel;
            live.SceneName = scene;
            return true;
          }
        }
      }

      return false;
    }

    private static void StartHoldPosition(Vector3 dest, float rotY, float seconds)
    {
      _holdDest = dest;
      _holdRotY = rotY;
      _holdUntil = Time.unscaledTime + seconds;
      _holdPositionActive = true;
    }

    private static bool TryGetRidePosition(out Vector3 pos)
    {
      pos = Vector3.zero;
      GameObject local = GameObject.Find("Player_Human");
      if (!UnityNull.Alive(local)) return false;

      Vehicle vehicle = local.GetComponent<Vehicle>();
      if ((object)vehicle != null)
      {
        Rigidbody rb = vehicle.GetComponent<Rigidbody>();
        if ((object)rb == null)
          rb = vehicle.GetComponentInChildren<Rigidbody>();
        if ((object)rb != null)
        {
          pos = rb.position;
          return true;
        }
      }

      pos = local.transform.position;
      return true;
    }

    private static bool IsNearDestination(Vector3 dest, float maxDistance)
    {
      Vector3 pos;
      if (!TryGetRidePosition(out pos)) return false;
      float maxSq = maxDistance * maxDistance;
      return (pos - dest).sqrMagnitude <= maxSq;
    }

    private static SpotFileEntry ResolveFreshSpot(SpotFileEntry spot)
    {
      if (spot == null) return null;
      SpotFileEntry fresh = FindSpot(MapKeyFromSpot(spot), spot.slot);
      return fresh ?? spot;
    }

    private static bool TeleportSpot(SpotFileEntry spot)
    {
      spot = ResolveFreshSpot(spot);
      if (spot == null) return false;

      ModLog.Debug("[SavedLoc] Teleport to " + spot.x.ToString("F1") + ", "
          + spot.y.ToString("F1") + ", " + spot.z.ToString("F1")
          + " (slot " + (spot.slot + 1) + " " + spot.mapLabel + ")");
      Vector3 dest = new Vector3(spot.x, spot.y, spot.z);
      if (!ApplyRidePosition(dest, spot.rotY))
        return false;
      MelonCoroutines.Start(SettleAtPosition(dest, spot.rotY, 6));
      return true;
    }

    private static System.Collections.IEnumerator SettleAtPosition(Vector3 dest, float rotY, int frames)
    {
      for (int i = 0; i < frames; i++)
      {
        yield return null;
        ApplyRidePosition(dest, rotY);
      }
    }

    private static System.Collections.IEnumerator DeferredTeleportRoutine()
    {
      SpotFileEntry spot = GetPendingSpot();
      if (spot == null)
      {
        ClearPendingGo();
        yield break;
      }

      while (ModWorkshopLoader.IsWorkshopLoadInProgress())
        yield return new WaitForSecondsRealtime(0.25f);

      MapChanger.MapBookmark target = BookmarkFromSpot(spot);
      int readyStable = 0;
      int needStable = GetReadyStableTicks(target);
      Vector3 pendingDest = new Vector3(spot.x, spot.y, spot.z);
      bool workshopPending = IsWorkshopPendingSpot(spot);

      for (int i = 0; i < 240; i++)
      {
        yield return new WaitForSecondsRealtime(0.25f);

        if (ModWorkshopLoader.IsWorkshopLoadInProgress())
        {
          readyStable = 0;
          continue;
        }

        spot = GetPendingSpot();
        if (spot == null)
        {
          ClearPendingGo();
          yield break;
        }
        target = BookmarkFromSpot(spot);
        needStable = GetReadyStableTicks(target);
        pendingDest = new Vector3(spot.x, spot.y, spot.z);
        workshopPending = IsWorkshopPendingSpot(spot);

        MapChanger.MapBookmark live;
        if (MapChanger.InWorkshopLevel())
            MapChanger.ApplySandboxWorkshopRidePublic();

        if (!IsMapReadyForPendingGo(target, out live) || !IsRiderReadyForTeleport())
        {
          readyStable = 0;
          continue;
        }

        if (!workshopPending && MapChanger.IsGeneratingRideState())
        {
          readyStable = 0;
          continue;
        }

        readyStable++;
        if (readyStable < needStable)
          continue;

        break;
      }

      if (readyStable < needStable)
      {
        ModLog.Feedback("[SavedLoc] Teleport timed out — terrain still loading. Try GO again.");
        ClearPendingGo();
        yield break;
      }

      ModLog.Debug("[SavedLoc] Rider ready — applying saved position...");
      for (int attempt = 0; attempt < 40; attempt++)
      {
        spot = GetPendingSpot();
        if (spot == null)
        {
          ClearPendingGo();
          yield break;
        }

        MapChanger.MapBookmark live;
        if (!IsMapReadyForPendingGo(BookmarkFromSpot(spot), out live) || !IsRiderReadyForTeleport())
        {
          yield return new WaitForSecondsRealtime(0.25f);
          continue;
        }

        Vector3 dest = new Vector3(spot.x, spot.y, spot.z);
        float rotY = spot.rotY;

        ApplyRidePosition(dest, rotY);
        yield return null;
        ApplyRidePosition(dest, rotY);

        if (IsNearDestination(dest, 25f))
        {
          for (int settle = 0; settle < 5; settle++)
          {
            yield return null;
            ApplyRidePosition(dest, rotY);
          }
          ModLog.Feedback("[SavedLoc] Teleported to saved spot.");
          CrewPerkManager.FinishTravelPerks();
          ClearPendingGo();
          _pendingGoRunning = false;
          yield break;
        }

        yield return new WaitForSecondsRealtime(0.35f);
      }

      ModLog.Feedback("[SavedLoc] Teleport timed out — try GO again.");
      ClearPendingGo();
      _pendingGoRunning = false;
    }

  private static bool ApplyRidePosition(Vector3 dest, float rotY)
  {
    try
    {
      GameObject local = GameObject.Find("Player_Human");
      if (!UnityNull.Alive(local)) return false;

      Vehicle vehicle = local.GetComponent<Vehicle>();
      Quaternion rot = Quaternion.Euler(0f, rotY, 0f);

      if ((object)vehicle != null)
      {
        local.transform.position = dest;
        local.transform.rotation = rot;

        Rigidbody rb = vehicle.GetComponent<Rigidbody>();
        if ((object)rb == null)
          rb = vehicle.GetComponentInChildren<Rigidbody>();

        if ((object)rb != null)
        {
          rb.position = dest;
          rb.rotation = rot;
          rb.velocity = Vector3.zero;
          rb.angularVelocity = Vector3.zero;
        }

        try
        {
          System.Reflection.MethodInfo resetMethod = vehicle.GetType().GetMethod(
              "Reset",
              System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
              null,
              new System.Type[] { typeof(bool) },
              null);
          if ((object)resetMethod != null)
            resetMethod.Invoke(vehicle, new object[] { false });
        }
        catch { }

        return true;
      }

      local.transform.position = dest;
      local.transform.rotation = rot;
      return true;
    }
    catch (System.Exception ex)
    {
      MelonLogger.Error("[SavedLoc] Teleport: " + ex.Message);
      Telemetry.ReportErrorAsync(ex, "SavedLocations");
      return false;
    }
  }

    private static void ClearPendingGo()
    {
      _pendingSpot = null;
      _pendingMapKey = "";
      _pendingSlot = -1;
      _pendingGoRunning = false;
      _holdPositionActive = false;
    }

    public static void CancelPendingTeleport()
    {
      ClearPendingGo();
    }

    public static void NotifyWorkshopLoadComplete()
    {
      if (!HasPendingGo() || _pendingGoRunning) return;
      if (ModWorkshopLoader.IsWorkshopLoadInProgress()) return;
      StartDeferredTeleportIfPending();
    }

    public static void OnWorkshopRideReadyWithoutTeleport()
    {
      if (!HasPendingGo())
        CrewPerkManager.FinishTravelPerks();
    }

    private static void tryCompletePendingGo()
    {
      if (!HasPendingGo() || _pendingGoRunning) return;
      if (ModWorkshopLoader.IsWorkshopLoadInProgress()) return;
      SpotFileEntry spot = GetPendingSpot();
      if (spot == null) return;
      if (!IsSameMap(BookmarkFromSpot(spot), _currentMap)) return;
      StartDeferredTeleportIfPending();
    }

    // ── Memory slots ───────────────────────────────────────────────

    private static void ClearMemorySlots()
    {
      for (int i = 0; i < SlotCount; i++)
      {
        _slots[i].Has = false;
        _slots[i].Name = "Spot " + (i + 1);
      }
    }

    private static void LoadSlotsForCurrentMap()
    {
      ClearMemorySlots();
      if (!_supported) return;

      for (int i = 0; i < SlotCount; i++)
      {
        SpotFileEntry fe = FindSpotForCurrentMap(i);
        if (fe == null) continue;

        _slots[i].Has = true;
        _slots[i].X = fe.x;
        _slots[i].Y = fe.y;
        _slots[i].Z = fe.z;
        _slots[i].RotY = fe.rotY;
        _slots[i].Name = string.IsNullOrEmpty(fe.name) ? "Spot " + (i + 1) : fe.name;
      }
    }

  // ── File spots ─────────────────────────────────────────────────

    private static SpotFileEntry FindSpotForCurrentMap(int slot)
    {
      if (!_supported) return null;
      SpotFileEntry best = null;
      for (int i = 0; i < _fileSpots.Count; i++)
      {
        SpotFileEntry fe = _fileSpots[i];
        if (fe == null || fe.slot != slot) continue;
        if (!IsSameMap(BookmarkFromSpot(fe), _currentMap)) continue;
        best = best == null ? fe : PickBetterSpot(best, fe);
      }
      return best;
    }

    private static SpotFileEntry FindSpot(string mapKey, int slot)
    {
      MapChanger.MapBookmark target = BookmarkFromLegacyKey(mapKey, null);
      SpotFileEntry best = null;
      for (int i = 0; i < _fileSpots.Count; i++)
      {
        SpotFileEntry fe = _fileSpots[i];
        if (fe == null || fe.slot != slot) continue;
        bool exact = MapKeyFromSpot(fe) == mapKey || fe.mapKey == mapKey;
        bool same = exact || IsSameMap(BookmarkFromSpot(fe), target);
        if (!same) continue;
        if (exact)
          return fe;
        best = best == null ? fe : PickBetterSpot(best, fe);
      }
      return best;
    }

    private static SpotFileEntry FindSpotByMapKey(string mapKey)
    {
      for (int i = 0; i < _fileSpots.Count; i++)
      {
        SpotFileEntry fe = _fileSpots[i];
        if (fe == null) continue;
        if (MapKeyFromSpot(fe) == mapKey || fe.mapKey == mapKey)
          return fe;
      }
      return null;
    }

    private static void UpsertFileSpot(int slot, MapChanger.MapBookmark bm, Vector3 pos, float rotY)
    {
      string storageKey = MapKeyFromBookmark(bm);
      RemoveAllSpotsMatchingMapSlot(storageKey, slot);

      SpotFileEntry fe = new SpotFileEntry { slot = slot };
      _fileSpots.Add(fe);

      fe.mapKey = storageKey;
      if (bm.Kind == MapChanger.MapBookmarkKind.FreeRideSeed)
        fe.mapLabel = FormatFreerideBookmarkLabel(bm);
      else
        fe.mapLabel = PrettyLabel(bm.DisplayLabel ?? "");
      fe.name = _slots[slot].Name;
      fe.x = pos.x;
      fe.y = pos.y;
      fe.z = pos.z;
      fe.rotY = rotY;
      fe.kind = (int)bm.Kind;
      fe.customSeed = bm.CustomSeed;
      fe.worldInt = bm.WorldInt;
      fe.sceneName = bm.SceneName;
      fe.sessionSeed = bm.SessionSeed ?? "";
      fe.sessionTypeInt = bm.SessionTypeInt;

      try
      {
        if (MapChanger.InWorkshopLevel())
          ApplyWorkshopSpotMetadata(fe, bm);
      }
      catch (System.Exception ex)
      {
        ModLog.Warn("[SavedLoc] Workshop spot metadata: " + ex.Message);
      }
    }

    private static void ApplyWorkshopSpotMetadata(SpotFileEntry fe, MapChanger.MapBookmark bm)
    {
      fe.kind = (int)MapChanger.MapBookmarkKind.SceneOnly;
      fe.customSeed = 0;
      if (!string.IsNullOrEmpty(bm.SceneName))
      {
        string sceneKey = "scene_" + SanitizeScene(bm.SceneName);
        if (sceneKey.Length > 6)
          fe.mapKey = sceneKey;
      }

      string modLabel;
      string modPath;
      if (ModWorkshopLoader.TryGetActiveWorkshopModInfo(out modLabel, out modPath))
      {
        if (!string.IsNullOrEmpty(modPath))
          fe.workshopPath = ModWorkshopLoader.NormalizeWorkshopPathForSave(modPath);
        if (!string.IsNullOrEmpty(modLabel))
          fe.mapLabel = modLabel;
      }
      else if (!string.IsNullOrEmpty(bm.DisplayLabel))
        fe.mapLabel = bm.DisplayLabel;
    }

    private static void RemoveDuplicateSpotsForIdentity(
        MapChanger.MapBookmark bm, int slot, SpotFileEntry keep)
    {
      string storageKey = MapKeyFromBookmark(bm);
      for (int i = _fileSpots.Count - 1; i >= 0; i--)
      {
        SpotFileEntry fe = _fileSpots[i];
        if (fe == null || fe == keep || fe.slot != slot) continue;
        if (fe.mapKey == storageKey)
          _fileSpots.RemoveAt(i);
      }
    }

    private static void DeduplicateFileSpots()
    {
      System.Collections.Generic.List<SpotFileEntry> kept = CollapseSpotEntries(_fileSpots);
      if (kept.Count == _fileSpots.Count)
      {
        for (int i = 0; i < kept.Count; i++)
          if (kept[i] != _fileSpots[i])
          {
            _fileSpots.Clear();
            for (int j = 0; j < kept.Count; j++)
              _fileSpots.Add(kept[j]);
            return;
          }
        return;
      }

      _fileSpots.Clear();
      for (int i = 0; i < kept.Count; i++)
        _fileSpots.Add(kept[i]);
    }

    private static System.Collections.Generic.List<SpotFileEntry> CollapseSpotEntries(
        System.Collections.Generic.List<SpotFileEntry> source)
    {
      var candidates = new System.Collections.Generic.List<SpotFileEntry>();
      for (int i = 0; i < source.Count; i++)
      {
        SpotFileEntry fe = source[i];
        if (fe == null) continue;
        if (fe.slot < 0 || fe.slot >= SlotCount) continue;
        candidates.Add(fe);
      }

      var kept = new System.Collections.Generic.List<SpotFileEntry>();
      while (candidates.Count > 0)
      {
        SpotFileEntry fe = candidates[0];
        candidates.RemoveAt(0);
        SpotFileEntry best = fe;
        MapChanger.MapBookmark bestBm = BookmarkFromSpot(best);

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
          SpotFileEntry other = candidates[i];
          if (other.slot != best.slot) continue;
          if (SpotDedupeKey(other) == SpotDedupeKey(best)
              || SharesSlotMapLabel(other, bestBm))
          {
            best = PickBetterSpot(best, other);
            bestBm = BookmarkFromSpot(best);
            candidates.RemoveAt(i);
          }
        }
        kept.Add(best);
      }
      return kept;
    }

    private static SpotFileEntry PickBetterSpot(SpotFileEntry a, SpotFileEntry b)
    {
      EnsureSpotMetadata(a);
      EnsureSpotMetadata(b);

      bool aFr = a.mapKey != null && a.mapKey.StartsWith("freeride_", System.StringComparison.Ordinal);
      bool bFr = b.mapKey != null && b.mapKey.StartsWith("freeride_", System.StringComparison.Ordinal);
      if (aFr && !bFr) return a;
      if (bFr && !aFr) return b;

      bool aCustom = !IsDefaultSpotName(a.name, a.slot);
      bool bCustom = !IsDefaultSpotName(b.name, b.slot);
      if (aCustom && !bCustom) return a;
      if (bCustom && !aCustom) return b;

      if (a.customSeed != 0 && b.customSeed == 0) return a;
      if (b.customSeed != 0 && a.customSeed == 0) return b;

      bool aPark = a.mapKey != null && a.mapKey.StartsWith("park_", System.StringComparison.Ordinal);
      bool bPark = b.mapKey != null && b.mapKey.StartsWith("park_", System.StringComparison.Ordinal);
      if (aPark && !bPark) return a;
      if (bPark && !aPark) return b;

      bool aSeedKey = a.mapKey != null && a.mapKey.StartsWith("seed_", System.StringComparison.Ordinal);
      bool bSeedKey = b.mapKey != null && b.mapKey.StartsWith("seed_", System.StringComparison.Ordinal);
      if (aSeedKey && !bSeedKey) return a;
      if (bSeedKey && !aSeedKey) return b;

      float magA = a.x * a.x + a.y * a.y + a.z * a.z;
      float magB = b.x * b.x + b.y * b.y + b.z * b.z;
      if (magA > 4f && magB < 4f) return a;
      if (magB > 4f && magA < 4f) return b;

      return a;
    }

    private static string DedupeKey(SpotFileEntry fe)
    {
      if (fe == null) return "null";
      if (!string.IsNullOrEmpty(fe.mapKey))
        return fe.mapKey + "|" + fe.slot;
      if (!string.IsNullOrEmpty(fe.mapLabel))
        return "lbl_" + NormalizeLabel(fe.mapLabel) + "|" + fe.slot;
      return SpotIdentityKey(fe) + "|" + fe.slot;
    }

    private static string SpotIdentityKey(SpotFileEntry fe)
    {
      if (!string.IsNullOrEmpty(fe.mapKey)) return "mk_" + fe.mapKey;
      if (fe.customSeed != 0) return "s_" + fe.customSeed;
      if (fe.worldInt > 0) return "w_" + fe.worldInt;
      string scene = SanitizeScene(fe.sceneName);
      if (!string.IsNullOrEmpty(scene)) return "sc_" + scene;
      return "k_" + (fe.mapKey ?? "");
    }

    private static string IdentityKeyFromBookmark(MapChanger.MapBookmark bm)
    {
      if (bm.CustomSeed != 0) return "s_" + bm.CustomSeed;
      if (bm.WorldInt > 0) return "w_" + bm.WorldInt;
      string scene = SanitizeScene(bm.SceneName);
      if (!string.IsNullOrEmpty(scene)) return "sc_" + scene;
      return "k_" + MapKeyFromBookmark(bm);
    }

    private static bool TryGetPlayerRidePosition(GameObject local, out Vector3 pos, out float rotY)
    {
      Vehicle vehicle = local.GetComponent<Vehicle>();
      if ((object)vehicle != null)
      {
        Rigidbody rb = vehicle.GetComponent<Rigidbody>();
        if ((object)rb == null)
          rb = vehicle.GetComponentInChildren<Rigidbody>();
        if ((object)rb != null)
        {
          pos = rb.position;
          rotY = rb.rotation.eulerAngles.y;
          return true;
        }
      }

      pos = local.transform.position;
      rotY = local.transform.eulerAngles.y;
      return true;
    }

    private static string SpotDedupeKey(SpotFileEntry fe)
    {
      if (fe == null) return "null";
      MapChanger.MapBookmark bm = BookmarkFromSpot(fe);
      if (bm.Kind == MapChanger.MapBookmarkKind.FreeRideSeed
          && bm.WorldInt > 0
          && !string.IsNullOrEmpty(bm.SessionSeed))
        return "fr_" + bm.WorldInt + "_" + bm.SessionSeed + "|" + fe.slot;

      MapChanger.MapBookmark promoted = bm;
      PromoteBookmarkToSeed(ref promoted);
      if (promoted.CustomSeed != 0)
        return "s_" + promoted.CustomSeed + "|" + fe.slot;
      if (promoted.WorldInt > 0)
        return "w_" + promoted.WorldInt + "|" + fe.slot;

      string scene = SanitizeScene(promoted.SceneName);
      if (!string.IsNullOrEmpty(scene))
        return "sc_" + scene + "|" + fe.slot;

      string label = NormalizeLabel(fe.mapLabel ?? "");
      if (!string.IsNullOrEmpty(label))
        return "lbl_" + label + "|" + fe.slot;

      return MapKeyFromSpot(fe) + "|" + fe.slot;
    }

    private static bool SharesSlotMapLabel(SpotFileEntry fe, MapChanger.MapBookmark target)
    {
      if (fe == null || !target.Valid) return false;
      string feLabel = NormalizeLabel(fe.mapLabel ?? "");
      string targetLabel = NormalizeLabel(target.DisplayLabel ?? "");
      if (string.IsNullOrEmpty(feLabel) || string.IsNullOrEmpty(targetLabel))
        return false;
      return feLabel == targetLabel;
    }

    private static void RemoveFileSpot(string mapKey, int slot)
    {
      RemoveAllSpotsMatchingMapSlot(mapKey, slot);
    }

    private static void RemoveFileSpotByKey(string mapKey, int slot)
    {
      RemoveAllSpotsMatchingMapSlot(mapKey, slot);
    }

    private static void RemoveAllSpotsMatchingMapSlot(string mapKey, int slot)
    {
      if (string.IsNullOrEmpty(mapKey)) return;
      MapChanger.MapBookmark target = BookmarkFromLegacyKey(mapKey, null);
      bool hasTarget = target.Valid;

      for (int i = _fileSpots.Count - 1; i >= 0; i--)
      {
        SpotFileEntry fe = _fileSpots[i];
        if (fe == null || fe.slot != slot) continue;

        string feKey = MapKeyFromSpot(fe);
        if (fe.mapKey == mapKey || feKey == mapKey)
        {
          _fileSpots.RemoveAt(i);
          continue;
        }

        if (hasTarget && IsSameMap(BookmarkFromSpot(fe), target))
          _fileSpots.RemoveAt(i);
        else if (hasTarget && SharesSlotMapLabel(fe, target))
          _fileSpots.RemoveAt(i);
      }
    }

    private static bool ApplySpotNameToFile(int slot, string name)
    {
      if (!_supported || string.IsNullOrEmpty(_mapKey))
        return false;

      MapChanger.MapBookmark target = _currentMap;
      bool any = false;
      for (int i = 0; i < _fileSpots.Count; i++)
      {
        SpotFileEntry fe = _fileSpots[i];
        if (fe == null || fe.slot != slot) continue;

        string feKey = MapKeyFromSpot(fe);
        if (fe.mapKey == _mapKey || feKey == _mapKey || IsSameMap(BookmarkFromSpot(fe), target)
            || SharesSlotMapLabel(fe, target))
        {
          fe.name = name;
          any = true;
        }
      }
      return any;
    }

    private static void LoadFileFromDisk()
    {
      _fileSpots.Clear();
      string path = FilePath;
      if (!File.Exists(path))
      {
        ModLog.Debug("[SavedLocations] No file at " + path);
        return;
      }

      try
      {
        string json = File.ReadAllText(path);
        if (string.IsNullOrEmpty(json)) return;

        // Unity JsonUtility only reliably reads the first array element — always parse each spot object.
        System.Collections.Generic.List<SpotFileEntry> parsed = ParseSpotsFromJsonManual(json);
        for (int i = 0; i < parsed.Count; i++)
        {
          SpotFileEntry fe = parsed[i];
          if (fe == null) continue;
          if (fe.slot < 0 || fe.slot >= SlotCount) continue;
          if (string.IsNullOrEmpty(fe.mapKey) && fe.customSeed == 0 && fe.x == 0f && fe.z == 0f)
            continue;
          _fileSpots.Add(fe);
        }

        bool repairedLabels = false;
        for (int i = 0; i < _fileSpots.Count; i++)
        {
          SpotFileEntry fe = _fileSpots[i];
          if (fe == null) continue;
          string before = fe.mapLabel;
          EnsureSpotMetadata(fe);
          if (before != fe.mapLabel)
            repairedLabels = true;
        }
        if (repairedLabels)
          SaveFileToDisk();

        ModLog.Debug("[SavedLocations] Loaded " + _fileSpots.Count + " spots from " + path);
      }
      catch (System.Exception ex)
      {
        ModLog.Warn("[SavedLocations] LoadFile: " + ex.Message);
      }
    }

    private static void SaveFileToDisk()
    {
      try
      {
        var sb = new System.Text.StringBuilder();
        sb.Append("{\n  \"spots\": [\n");
        for (int i = 0; i < _fileSpots.Count; i++)
        {
          if (i > 0) sb.Append(",\n");
          sb.Append("    ");
          WriteSpotJson(sb, _fileSpots[i]);
        }
        if (_fileSpots.Count > 0) sb.Append("\n");
        sb.Append("  ]\n}\n");

        string path = FilePath;
        File.WriteAllText(path, sb.ToString());
        ModLog.Debug("[SavedLocations] Saved " + _fileSpots.Count + " spots to " + path);
      }
      catch (System.Exception ex)
      {
        ModLog.Warn("[SavedLocations] SaveFile: " + ex.Message);
      }
    }

    private static void WriteSpotJson(System.Text.StringBuilder sb, SpotFileEntry fe)
    {
      sb.Append("{");
      AppendJsonString(sb, "mapKey", fe.mapKey);
      sb.Append(", ");
      AppendJsonString(sb, "mapLabel", fe.mapLabel);
      sb.Append(", ");
      AppendJsonInt(sb, "slot", fe.slot);
      sb.Append(", ");
      AppendJsonString(sb, "name", fe.name);
      sb.Append(", ");
      AppendJsonFloat(sb, "x", fe.x);
      sb.Append(", ");
      AppendJsonFloat(sb, "y", fe.y);
      sb.Append(", ");
      AppendJsonFloat(sb, "z", fe.z);
      sb.Append(", ");
      AppendJsonFloat(sb, "rotY", fe.rotY);
      sb.Append(", ");
      AppendJsonInt(sb, "kind", fe.kind);
      sb.Append(", ");
      AppendJsonInt(sb, "customSeed", fe.customSeed);
      sb.Append(", ");
      AppendJsonInt(sb, "worldInt", fe.worldInt);
      sb.Append(", ");
      AppendJsonInt(sb, "sessionTypeInt", fe.sessionTypeInt);
      sb.Append(", ");
      AppendJsonString(sb, "sceneName", fe.sceneName);
      sb.Append(", ");
      AppendJsonString(sb, "sessionSeed", fe.sessionSeed);
      if (!string.IsNullOrEmpty(fe.workshopPath))
      {
        sb.Append(", ");
        AppendJsonString(sb, "workshopPath", fe.workshopPath);
      }
      sb.Append("}");
    }

    private static void AppendJsonString(System.Text.StringBuilder sb, string key, string val)
    {
      sb.Append("\"").Append(key).Append("\":\"")
          .Append(EscapeJson(val ?? "")).Append("\"");
    }

    private static void AppendJsonFloat(System.Text.StringBuilder sb, string key, float val)
    {
      sb.Append("\"").Append(key).Append("\":").Append(val.ToString("R"));
    }

    private static void AppendJsonInt(System.Text.StringBuilder sb, string key, int val)
    {
      sb.Append("\"").Append(key).Append("\":").Append(val);
    }

    private static string EscapeJson(string s)
    {
      return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static System.Collections.Generic.List<SpotFileEntry> ParseSpotsFromJsonManual(string json)
    {
      var list = new System.Collections.Generic.List<SpotFileEntry>();
      int search = 0;
      while (search < json.Length)
      {
        int mapKeyIdx = json.IndexOf("\"mapKey\"", search, System.StringComparison.Ordinal);
        if (mapKeyIdx < 0) break;

        int objStart = json.LastIndexOf('{', mapKeyIdx);
        if (objStart < 0) break;

        int depth = 0;
        int objEnd = -1;
        for (int i = objStart; i < json.Length; i++)
        {
          char c = json[i];
          if (c == '{') depth++;
          else if (c == '}')
          {
            depth--;
            if (depth == 0)
            {
              objEnd = i;
              break;
            }
          }
        }

        if (objEnd < 0) break;

        string objJson = json.Substring(objStart, objEnd - objStart + 1);
        try
        {
          SpotFileEntry fe = JsonUtility.FromJson<SpotFileEntry>(objJson);
          if (fe != null && (!string.IsNullOrEmpty(fe.mapKey) || fe.customSeed != 0))
            list.Add(fe);
        }
        catch { }

        search = objEnd + 1;
      }
      return list;
    }

    private static void EnsurePrefsCategory()
    {
      if (_prefsCat == null)
        _prefsCat = MelonPreferences.CreateCategory("SavedLocations", "Spot Book");
    }

    private static string SanitizePrefMapKey(string mapKey)
    {
      if (string.IsNullOrEmpty(mapKey)) return "unknown";
      var sb = new System.Text.StringBuilder(mapKey.Length);
      for (int i = 0; i < mapKey.Length; i++)
      {
        char c = mapKey[i];
        if (char.IsLetterOrDigit(c) || c == '_')
          sb.Append(c);
        else
          sb.Append('_');
      }
      return sb.ToString();
    }

    private static bool MergeFromMelonPrefs()
    {
      try
      {
        EnsurePrefsCategory();
        bool added = false;

        foreach (MelonPreferences_Entry entry in _prefsCat.Entries)
        {
          string id = entry.Identifier;
          if (string.IsNullOrEmpty(id) || !id.StartsWith("P_")) continue;

          MelonPreferences_Entry<string> posEnt = entry as MelonPreferences_Entry<string>;
          if ((object)posEnt == null || string.IsNullOrEmpty(posEnt.Value)) continue;

          int lastUnderscore = id.LastIndexOf('_');
          if (lastUnderscore <= 2) continue;
          int slot;
          if (!int.TryParse(id.Substring(lastUnderscore + 1), out slot)
              || slot < 0 || slot >= SlotCount)
            continue;

          string mapKey = id.Substring(2, lastUnderscore - 2);
          if (FindSpot(mapKey, slot) != null) continue;

          string[] parts = posEnt.Value.Split(',');
          if (parts.Length < 3) continue;
          float x, y, z, ry = 0f;
          if (!float.TryParse(parts[0], out x)) continue;
          if (!float.TryParse(parts[1], out y)) continue;
          if (!float.TryParse(parts[2], out z)) continue;
          if (parts.Length >= 4) float.TryParse(parts[3], out ry);

          string safeKey = SanitizePrefMapKey(mapKey);
          string name = "Spot " + (slot + 1);
          string label = "";
          foreach (MelonPreferences_Entry aux in _prefsCat.Entries)
          {
            string auxId = aux.Identifier;
            if (auxId == "N_" + safeKey + "_" + slot)
            {
              MelonPreferences_Entry<string> nameEnt = aux as MelonPreferences_Entry<string>;
              if ((object)nameEnt != null && !string.IsNullOrEmpty(nameEnt.Value))
                name = nameEnt.Value;
            }
            else if (auxId == "L_" + safeKey + "_" + slot)
            {
              MelonPreferences_Entry<string> labelEnt = aux as MelonPreferences_Entry<string>;
              if ((object)labelEnt != null && !string.IsNullOrEmpty(labelEnt.Value))
                label = labelEnt.Value;
            }
          }

          MapChanger.MapBookmark bm = BookmarkFromLegacyKey(mapKey, label);
          SpotFileEntry fe = new SpotFileEntry
          {
            mapKey = mapKey,
            slot = slot,
            x = x,
            y = y,
            z = z,
            rotY = ry,
            name = name,
            mapLabel = string.IsNullOrEmpty(label) ? bm.DisplayLabel : label,
            kind = (int)bm.Kind,
            customSeed = bm.CustomSeed,
            worldInt = bm.WorldInt,
            sceneName = bm.SceneName
          };
          EnsureSpotMetadata(fe);
          _fileSpots.Add(fe);
          added = true;
        }

        if (added)
          ModLog.Debug("[SavedLocations] Restored spots from MelonPrefs backup.");
        return added;
      }
      catch (System.Exception ex)
      {
        ModLog.Debug("[SavedLocations] MelonPrefs merge: " + ex.Message);
        return false;
      }
    }

    // ── Helpers ────────────────────────────────────────────────────

    private static string FormatFreerideBookmarkLabel(MapChanger.MapBookmark bm)
    {
      return FormatFreerideSpotLabel(bm.WorldInt, bm.SessionSeed, bm.DisplayLabel);
    }

    private static string FormatFreerideSpotLabel(int worldInt, string sessionSeed, string displayLabel)
    {
      string seed = ResolveFreerideSeedForLabel(sessionSeed, displayLabel);
      string world = ResolveFreerideWorldLabel(worldInt, displayLabel, seed);
      if (!string.IsNullOrEmpty(seed))
      {
        string seedText = CompactSeedForDisplay(seed);
        return string.IsNullOrEmpty(world) ? seedText : (world + " - " + seedText);
      }
      return world;
    }

    private static string ResolveFreerideSeedForLabel(string sessionSeed, string displayLabel)
    {
      if (!string.IsNullOrEmpty(sessionSeed))
        return sessionSeed.Trim();

      if (string.IsNullOrEmpty(displayLabel)) return "";

      int idx = displayLabel.LastIndexOf(" - Seed ", System.StringComparison.Ordinal);
      if (idx >= 0)
        return displayLabel.Substring(idx + 9).Trim();

      if (displayLabel.StartsWith("Seed ", System.StringComparison.OrdinalIgnoreCase))
        return displayLabel.Substring(5).Trim();

      if (long.TryParse(displayLabel.Trim(), out long lone) && lone > 0L)
        return lone.ToString();

      return "";
    }

    private static string ResolveFreerideWorldLabel(int worldInt, string displayLabel, string seed)
    {
      if (!string.IsNullOrEmpty(displayLabel))
      {
        int idx = displayLabel.IndexOf(" - Seed ", System.StringComparison.Ordinal);
        if (idx > 0)
          return PrettyLabel(displayLabel.Substring(0, idx).Trim());

        if (!displayLabel.StartsWith("Seed ", System.StringComparison.OrdinalIgnoreCase)
            && long.TryParse(displayLabel.Trim(), out _))
        {
            // Numeric-only label — world comes from worldInt.
        }
        else if (!displayLabel.StartsWith("Seed ", System.StringComparison.OrdinalIgnoreCase)
            && displayLabel.IndexOf(" - ", System.StringComparison.Ordinal) < 0)
          return PrettyLabel(displayLabel);
      }

      if (worldInt > 0)
        return PrettyLabel(MapChanger.GetWorldDisplayName(worldInt));
      return "";
    }

    public static string CompactSeedForDisplay(string seed)
    {
      if (string.IsNullOrEmpty(seed)) return "";
      string trimmed = seed.Trim();
      if (trimmed.Length <= 14)
        return "Seed " + trimmed;
      return "Seed " + trimmed.Substring(0, 6) + "…" + trimmed.Substring(trimmed.Length - 4);
    }

    public static string GetFreerideListRowLabel(string mapLabel, string spotName, int slot)
    {
      if (!string.IsNullOrEmpty(spotName) && spotName != "Empty")
        return spotName;
      if (string.IsNullOrEmpty(mapLabel))
        return "Spot " + (slot + 1);
      return mapLabel;
    }

    private static bool TryParseFreerideMapKey(string mapKey, out int worldInt, out string sessionSeed)
    {
      worldInt = 0;
      sessionSeed = "";
      if (string.IsNullOrEmpty(mapKey) || !mapKey.StartsWith("freeride_", System.StringComparison.Ordinal))
        return false;
      string rest = mapKey.Substring(9);
      int sep = rest.IndexOf('_');
      if (sep <= 0 || sep >= rest.Length - 1) return false;
      if (!int.TryParse(rest.Substring(0, sep), out worldInt) || worldInt <= 0) return false;
      sessionSeed = rest.Substring(sep + 1);
      return !string.IsNullOrEmpty(sessionSeed);
    }

    private static string PrettyLabel(string label)
    {
      if (string.IsNullOrEmpty(label)) return label;
      if (label.StartsWith("Seed ", System.StringComparison.OrdinalIgnoreCase))
        return label;
      string[] parts = label.Split(' ');
      for (int i = 0; i < parts.Length; i++)
      {
        string p = parts[i];
        if (p.Length == 0) continue;
        parts[i] = p.Length == 1
            ? char.ToUpperInvariant(p[0]).ToString()
            : char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant();
      }
      return string.Join(" ", parts);
    }

    private static bool LooksLikeWorkshopSceneName(string sceneName)
    {
      if (string.IsNullOrEmpty(sceneName)) return false;
      if (sceneName.IndexOf(' ') >= 0 || sceneName.IndexOf('-') >= 0)
        return true;
      string lower = sceneName.ToLowerInvariant();
      return lower.Contains("modio") || lower == "modscene";
    }

    private static bool WorkshopLabelsMatch(string a, string b)
    {
      string na = NormalizeLabel(a);
      string nb = NormalizeLabel(b);
      if (string.IsNullOrEmpty(na) || string.IsNullOrEmpty(nb)) return false;
      if (na == nb) return true;
      if (na.Length >= 4 && nb.StartsWith(na, System.StringComparison.Ordinal)) return true;
      if (nb.Length >= 4 && na.StartsWith(nb, System.StringComparison.Ordinal)) return true;
      return false;
    }

    private static string NormalizeLabel(string label)
    {
      if (string.IsNullOrEmpty(label)) return "";
      System.Text.StringBuilder sb = new System.Text.StringBuilder(label.Length);
      for (int i = 0; i < label.Length; i++)
      {
        char c = label[i];
        if (char.IsLetterOrDigit(c))
          sb.Append(char.ToLowerInvariant(c));
      }
      return sb.ToString();
    }

    private static string SanitizeScene(string scene)
    {
      if (string.IsNullOrEmpty(scene)) return "";
      return MapChanger.SanitizeStoragePartPublic(scene);
    }
  }
}
