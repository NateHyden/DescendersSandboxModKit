using System.Reflection;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Trick Multiplier. Recreated 2026-08-03 — this used to exist as the old
    /// ScoreManager.cs before the DescendersSandbox restructure and never got
    /// migrated across. Confirmed against the post-update decompile: both members
    /// still exist at the same obfuscated names, same defaults, same locations.
    ///
    /// Two separate things, both needed:
    ///   - uDh]dJt: public int field, the CAP the game will let the multiplier
    ///     climb to naturally through trick combos. On VehicleTricks' RUNTIME
    ///     SUBCLASS - NOT visible via typeof(VehicleTricks).GetField(), only via
    ///     instance.GetType().GetField(). Default 3.
    ///   - FnHLcjK: public float PROPERTY (not field), the CURRENT live multiplier
    ///     actually applied to scoring right now. On the base VehicleTricks type
    ///     itself (typeof(VehicleTricks).GetProperty(...) works fine here, unlike
    ///     the cap field). Raising just the cap alone does nothing visible until
    ///     the player earns their way up through combos - to make the chosen
    ///     number take effect immediately (what "OFF -> x10" should look like),
    ///     this has to be force-set every frame too. The game's own FixedUpdate
    ///     recalculates it from the live combo every frame, so a one-time write
    ///     gets overwritten instantly - matches the original ScoreManager design
    ///     notes ("must be reapplied every single frame").
    /// </summary>
    public static class TrickMultiplier
    {
        private static readonly int[] CapValues = { 3, 5, 10, 20 };

        public static int Level { get; private set; } = 0;
        public static bool Enabled { get { return Level > 0; } }

        public static string LevelDisplay
        {
            get { return Level == 0 ? "OFF" : "x" + CapValues[Level]; }
        }

        private static int _lastNonZeroLevel = 1;
        private static FieldInfo _capField;
        private static PropertyInfo _multiplierProp;
        private static bool _multiplierPropSearched;

        public static void Toggle()
        {
            if (Level > 0)
            {
                _lastNonZeroLevel = Level;
                Level = 0;
            }
            else
            {
                SetLevel(_lastNonZeroLevel > 0 ? _lastNonZeroLevel : 1);
            }
        }

        public static void Increase()
        {
            if (Level < CapValues.Length - 1) { Level++; if (Level > 0) _lastNonZeroLevel = Level; }
        }

        public static void Decrease()
        {
            if (Level > 0) { Level--; if (Level > 0) _lastNonZeroLevel = Level; }
        }

        public static void SetLevel(int level)
        {
            if (level < 0) level = 0;
            if (level > CapValues.Length - 1) level = CapValues.Length - 1;
            Level = level;
            if (Level > 0) _lastNonZeroLevel = Level;
        }

        /// <summary>Called every frame from ModEntry.OnUpdate. VehicleTricks is a
        /// fresh instance on every respawn (resets cap to the game default 3), so
        /// this has to run continuously rather than apply-once - same pattern as
        /// WideTyres.Tick() / BikeDamage.Tick() elsewhere in this project.</summary>
        public static void Tick()
        {
            try
            {
                if (Level == 0) return;

                GameObject player = PlayerCache.PlayerHuman;
                if ((object)player == null) return;

                VehicleTricks tricks = player.GetComponentInChildren<VehicleTricks>();
                if ((object)tricks == null) return;

                int target = CapValues[Level];

                if ((object)_capField == null)
                {
                    _capField = tricks.GetType().GetField("uDh\u005DdJt",
                        BindingFlags.Public | BindingFlags.Instance);
                    if ((object)_capField == null)
                        ModLog.Warn("[TrickMultiplier] Cap field (uDh]dJt) not found on VehicleTricks runtime subclass.");
                }
                if ((object)_capField != null)
                {
                    int currentCap = (int)_capField.GetValue(tricks);
                    if (currentCap != target)
                        _capField.SetValue(tricks, target);
                }

                if (!_multiplierPropSearched)
                {
                    _multiplierPropSearched = true;
                    _multiplierProp = typeof(VehicleTricks).GetProperty("FnHLcjK",
                        BindingFlags.Public | BindingFlags.Instance);
                    if ((object)_multiplierProp == null)
                        ModLog.Warn("[TrickMultiplier] Multiplier property (FnHLcjK) not found on VehicleTricks.");
                }
                if ((object)_multiplierProp != null && _multiplierProp.CanWrite)
                {
                    _multiplierProp.SetValue(tricks, (float)target, null);
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[TrickMultiplier] Tick: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "TrickMultiplier"); }
        }

        public static void Reset()
        {
            Level = 0;
        }
    }
}

