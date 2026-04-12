using HarmonyLib;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DescendersModMenu.UI;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Lets you use any other bike's trick set on your current bike.
    ///
    /// Strategy (v3 — direct array swap, BikeSwitcher hook):
    /// - Find the player's current BikeType via PlayerManager.GetPlayerImpact()
    /// - Snapshot its overrideGestures array reference
    /// - Replace overrideGestures with the chosen source bike's array
    /// - On disable, restore the snapshot
    ///
    /// This works regardless of which Cyclist.GetGesture variant the game calls,
    /// because they ALL read from BikeType.overrideGestures first.
    ///
    /// Auto-disable: BikeSwitcher.SetBike calls TrickSetSwap.Disable() at the
    /// start of every bike change, so the original gestures are restored on the
    /// outgoing bike type before the new one is selected.
    /// </summary>
    public static class TrickSetSwap
    {
        public static bool Enabled { get; private set; } = false;
        public static int SourceIndex { get; private set; } = 0;

        // ── Snapshot of the modified BikeType's original array ───────
        // We hold the BikeType we mutated so we can restore it even if
        // the player switches away before disabling.
        private static BikeType _patchedBike = null;
        private static Gesture[] _originalGestures = null;

        // ── Cached reflection to read PlayerInfoImpact.BikeType ──────
        private static FieldInfo _bikeTypeField = null;

        // ── Available bike types (lazy refresh) ──────────────────────
        private static List<BikeType> _availableBikes = null;
        public static List<BikeType> AvailableBikes
        {
            get
            {
                if (_availableBikes == null) RefreshAvailableBikes();
                return _availableBikes;
            }
        }
        public static int AvailableCount
        {
            get { return AvailableBikes != null ? AvailableBikes.Count : 0; }
        }

        public static string CurrentSourceName
        {
            get
            {
                var list = AvailableBikes;
                if (list == null || list.Count == 0) return "—";
                int i = Mathf.Clamp(SourceIndex, 0, list.Count - 1);
                return list[i].name;
            }
        }

        // ─────────────────────────────────────────────────────────────
        public static void RefreshAvailableBikes()
        {
            _availableBikes = new List<BikeType>();
            try
            {
                BikeType[] all = Resources.FindObjectsOfTypeAll<BikeType>();
                if ((object)all == null) return;

                var seen = new HashSet<string>();
                for (int i = 0; i < all.Length; i++)
                {
                    BikeType bt = all[i];
                    if ((object)bt == null) continue;
                    if (bt.overrideGestures == null || bt.overrideGestures.Length == 0) continue;
                    if (seen.Contains(bt.name)) continue;
                    seen.Add(bt.name);
                    _availableBikes.Add(bt);
                }

                MelonLogger.Msg("[TrickSetSwap] Discovered " + _availableBikes.Count + " bike type(s) with trick sets");
                for (int i = 0; i < _availableBikes.Count; i++)
                {
                    var bt = _availableBikes[i];
                    MelonLogger.Msg("[TrickSetSwap]   [" + i + "] " + bt.name + " (" + bt.overrideGestures.Length + " gestures)");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[TrickSetSwap] RefreshAvailableBikes: " + ex.Message);
            }
        }

        // ── Find the player's current BikeType via reflection ────────
        // PlayerInfoImpact is NOT a component on Player_Human — it lives on
        // PlayerManager and is retrieved via GetPlayerImpact(). Same pattern
        // BikeSwitcher uses.
        private static MethodInfo _getPlayerImpactMethod;
        private static PlayerInfoImpact GetPlayerImpact()
        {
            try
            {
                PlayerManager pm = UnityEngine.Object.FindObjectOfType<PlayerManager>();
                if ((object)pm == null) return null;

                if ((object)_getPlayerImpactMethod == null)
                {
                    _getPlayerImpactMethod = pm.GetType().GetMethod(
                        "GetPlayerImpact",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if ((object)_getPlayerImpactMethod == null)
                    {
                        MelonLogger.Warning("[TrickSetSwap] PlayerManager.GetPlayerImpact not found");
                        return null;
                    }
                }

                return _getPlayerImpactMethod.Invoke(pm, null) as PlayerInfoImpact;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[TrickSetSwap] GetPlayerImpact: " + ex.Message);
                return null;
            }
        }

        private static BikeType GetCurrentPlayerBikeType()
        {
            try
            {
                PlayerInfoImpact pii = GetPlayerImpact();
                if ((object)pii == null)
                {
                    MelonLogger.Warning("[TrickSetSwap] PlayerInfoImpact not available (PlayerManager.GetPlayerImpact returned null)");
                    return null;
                }

                if ((object)_bikeTypeField == null)
                {
                    FieldInfo[] fields = pii.GetType().GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        FieldInfo f = fields[i];
                        if (f.Name.Contains("k__BackingField")) continue;
                        if (string.Equals(f.FieldType.Name, "BikeType", StringComparison.Ordinal))
                        {
                            _bikeTypeField = f;
                            MelonLogger.Msg("[TrickSetSwap] Cached BikeType field: " + f.Name);
                            break;
                        }
                    }
                    if ((object)_bikeTypeField == null)
                    {
                        MelonLogger.Warning("[TrickSetSwap] BikeType field not found on PlayerInfoImpact");
                        return null;
                    }
                }

                return _bikeTypeField.GetValue(pii) as BikeType;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[TrickSetSwap] GetCurrentPlayerBikeType: " + ex.Message);
                return null;
            }
        }

        // ─────────────────────────────────────────────────────────────
        public static void Toggle()
        {
            if (Enabled) { Disable(); return; }

            // Enabling — find what bike we're currently on
            if (_availableBikes == null) RefreshAvailableBikes();
            if (_availableBikes == null || _availableBikes.Count == 0)
            {
                MelonLogger.Warning("[TrickSetSwap] No bike types with trick sets found — cannot enable");
                return;
            }

            BikeType target = GetCurrentPlayerBikeType();
            if ((object)target == null)
            {
                MelonLogger.Warning("[TrickSetSwap] Could not read current player bike type — cannot enable");
                return;
            }

            int srcIdx = Mathf.Clamp(SourceIndex, 0, _availableBikes.Count - 1);
            BikeType source = _availableBikes[srcIdx];

            if ((object)source == (object)target)
            {
                MelonLogger.Msg("[TrickSetSwap] Source bike (" + source.name + ") matches current bike — nothing to swap");
                return;
            }

            // Snapshot the target's current array so we can restore exactly
            _patchedBike = target;
            _originalGestures = target.overrideGestures;
            target.overrideGestures = source.overrideGestures;
            Enabled = true;

            int origLen = _originalGestures != null ? _originalGestures.Length : 0;
            int newLen = source.overrideGestures != null ? source.overrideGestures.Length : 0;
            MelonLogger.Msg("[TrickSetSwap] -> ON  | target=" + target.name
                + " (was " + origLen + " gestures) | source=" + source.name
                + " (" + newLen + " gestures)");
        }

        public static void Disable()
        {
            if (!Enabled) return;
            try
            {
                if ((object)_patchedBike != null)
                {
                    _patchedBike.overrideGestures = _originalGestures;
                    int restoredLen = _originalGestures != null ? _originalGestures.Length : 0;
                    MelonLogger.Msg("[TrickSetSwap] -> OFF | restored " + _patchedBike.name
                        + " (" + restoredLen + " gestures)");
                }
                else
                {
                    MelonLogger.Msg("[TrickSetSwap] -> OFF (no snapshot to restore)");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[TrickSetSwap] Disable restore failed: " + ex.Message);
            }
            finally
            {
                _patchedBike = null;
                _originalGestures = null;
                Enabled = false;
            }

            // Refresh the menu so the toggle knob/text update immediately,
            // even when this fires from the bike-switch postfix mid-gameplay.
            try { MenuWindow.RefreshAll(); } catch { }
        }

        public static void Reset() { Disable(); }

        // ─────────────────────────────────────────────────────────────
        // Source bike picker — does NOT re-apply automatically while enabled.
        // The user must toggle off and back on for a new source to take effect.
        // (Avoids surprise behaviour where browsing the picker silently changes
        // their tricks mid-run.)
        public static void NextSource()
        {
            var list = AvailableBikes;
            if (list == null || list.Count == 0) return;
            SourceIndex = (SourceIndex + 1) % list.Count;
        }

        public static void PrevSource()
        {
            var list = AvailableBikes;
            if (list == null || list.Count == 0) return;
            SourceIndex = (SourceIndex - 1 + list.Count) % list.Count;
        }

        public static void SetSourceByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            var list = AvailableBikes;
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].name, name, StringComparison.Ordinal))
                {
                    SourceIndex = i;
                    return;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Auto-disable is handled by an explicit call from BikeSwitcher.SetBike
        // (see BikeSwitcher.cs). The postfix-on-SetBikeTypeFromNum approach was
        // unreliable because the game has multiple call paths (init,
        // SetCasualModeBikeOverride, etc.) that fire the same method, making
        // BikeType-reference tracking go out of sync.
        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            MelonLogger.Msg("[TrickSetSwap] Auto-disable hooked via BikeSwitcher.SetBike (no Harmony patch needed)");
        }
    }
}