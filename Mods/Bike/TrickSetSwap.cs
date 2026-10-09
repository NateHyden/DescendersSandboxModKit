using HarmonyLib;
using MelonLoader;
using DescendersModMenu;
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

        private static BikeType _patchedBike = null;
        private static Gesture[] _originalGestures = null;

        private static FieldInfo _bikeTypeField = null;

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

                ModLog.Debug("[TrickSetSwap] Discovered " + _availableBikes.Count + " bike type(s) with trick sets");
                for (int i = 0; i < _availableBikes.Count; i++)
                {
                    var bt = _availableBikes[i];
                    ModLog.Debug("[TrickSetSwap]   [" + i + "] " + bt.name + " (" + bt.overrideGestures.Length + " gestures)");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[TrickSetSwap] RefreshAvailableBikes: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "TrickSetSwap");
            }
        }

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
                        ModLog.Warn("[TrickSetSwap] PlayerManager.GetPlayerImpact not found");
                        return null;
                    }
                }

                return _getPlayerImpactMethod.Invoke(pm, null) as PlayerInfoImpact;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[TrickSetSwap] GetPlayerImpact: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "TrickSetSwap");
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
                    ModLog.Warn("[TrickSetSwap] PlayerInfoImpact not available (PlayerManager.GetPlayerImpact returned null)");
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
                            ModLog.Debug("[TrickSetSwap] Cached BikeType field: " + f.Name);
                            break;
                        }
                    }
                    if ((object)_bikeTypeField == null)
                    {
                        ModLog.Warn("[TrickSetSwap] BikeType field not found on PlayerInfoImpact");
                        return null;
                    }
                }

                return _bikeTypeField.GetValue(pii) as BikeType;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[TrickSetSwap] GetCurrentPlayerBikeType: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "TrickSetSwap");
                return null;
            }
        }

        // ─────────────────────────────────────────────────────────────
        public static void Toggle()
        {
            if (Enabled) { Disable(); return; }
            TryEnable(false);
        }

        /// <summary>
        /// True when the user wants the swap on. It survives bike changes: BikeSwitcher.SetBike
        /// suspends the patch on the outgoing bike and re-applies it to the new bike.
        /// Cleared by Disable() / Reset().
        /// </summary>
        public static bool Wanted { get; private set; } = false;

        /// <summary>
        /// Used when restoring saved state: remember the user wants the swap on even if it cannot
        /// be applied to the current bike yet (e.g. the bike is switched a moment later).
        /// </summary>
        public static void Request()
        {
            Wanted = true;
            if (!Enabled) TryEnable(false);
        }

        private static bool TryEnable(bool quiet)
        {
            if (_availableBikes == null) RefreshAvailableBikes();
            if (_availableBikes == null || _availableBikes.Count == 0)
            {
                ModLog.Warn("[TrickSetSwap] No bike types with trick sets found - cannot enable");
                return false;
            }

            BikeType target = GetCurrentPlayerBikeType();
            if ((object)target == null)
            {
                ModLog.Warn("[TrickSetSwap] Could not read current player bike type - cannot enable");
                return false;
            }

            int srcIdx = Mathf.Clamp(SourceIndex, 0, _availableBikes.Count - 1);
            BikeType source = _availableBikes[srcIdx];

            if ((object)source == (object)target)
            {
                ModLog.Debug("[TrickSetSwap] Source bike (" + source.name + ") matches current bike - nothing to swap");
                return false;
            }

            _patchedBike = target;
            _originalGestures = target.overrideGestures;
            target.overrideGestures = source.overrideGestures;
            Enabled = true;
            Wanted = true;

            int origLen = _originalGestures != null ? _originalGestures.Length : 0;
            int newLen = source.overrideGestures != null ? source.overrideGestures.Length : 0;
            string msg = "[TrickSetSwap] -> ON  | target=" + target.name
                + " (was " + origLen + " gestures) | source=" + source.name
                + " (" + newLen + " gestures)";
            if (quiet) ModLog.Debug(msg); else ModLog.Feedback(msg);
            return true;
        }

        public static void Disable()
        {
            Wanted = false;
            if (!Enabled) return;
            DisableCore(false);
        }

        private static void DisableCore(bool quiet)
        {
            try
            {
                if ((object)_patchedBike != null)
                {
                    _patchedBike.overrideGestures = _originalGestures;
                    int restoredLen = _originalGestures != null ? _originalGestures.Length : 0;
                    string msg = "[TrickSetSwap] -> OFF | restored " + _patchedBike.name
                        + " (" + restoredLen + " gestures)";
                    if (quiet) ModLog.Debug(msg); else ModLog.Feedback(msg);
                }
                else
                {
                    string msg = "[TrickSetSwap] -> OFF (no snapshot to restore)";
                    if (quiet) ModLog.Debug(msg); else ModLog.Feedback(msg);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[TrickSetSwap] Disable restore failed: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "TrickSetSwap");
            }
            finally
            {
                _patchedBike = null;
                _originalGestures = null;
                Enabled = false;
            }

            if (!quiet)
            {
                try { MenuWindow.RefreshAll(); } catch { }
            }
        }

        public static void Reset() { Disable(); }

        /// <summary>
        /// Called by BikeSwitcher.SetBike before the bike changes: restores the original gestures
        /// on the outgoing bike type but keeps Wanted, so the swap can follow the player.
        /// Returns true if the swap should be re-applied afterwards.
        /// </summary>
        public static bool SuspendForBikeChange()
        {
            if (Enabled)
            {
                Wanted = true;
                DisableCore(true);
            }
            return Wanted;
        }

        /// <summary>Called by BikeSwitcher.SetBike after the bike changed: re-applies the swap to the new bike.</summary>
        public static void ResumeAfterBikeChange()
        {
            if (!Wanted || Enabled) return;
            bool ok = TryEnable(true);
            ModLog.Debug("[TrickSetSwap] Re-apply after bike change -> "
                + (ok ? "ON" : "skipped (source matches the new bike, or bike unavailable)"));
            try { MenuWindow.RefreshAll(); } catch { }
        }

        // ─────────────────────────────────────────────────────────────
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
        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            ModLog.Debug("[TrickSetSwap] Auto-disable hooked via BikeSwitcher.SetBike (no Harmony patch needed)");
        }
    }
}

