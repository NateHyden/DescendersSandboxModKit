using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Picks a random GameModifierMods slider and a random level every time
    // a checkpoint is crossed. Hooked from SessionTrackers.CheckpointTick's
    // existing crossing-detection (CheckpointCount increment) via
    // RandomMutatorOnCheckpoint.OnCheckpoint() — reuses the game's own
    // modifier system (GameModifierMods.ApplyMod / SetXLevel), no new
    // reflection needed. Snapshots the five levels on enable and restores
    // them exactly via the same SetXLevel setters on disable.
    public static class RandomMutatorOnCheckpoint
    {
        public static bool Enabled { get; private set; } = false;
        public static string LastMutationDisplay { get; private set; } = "--";

        private static int _snapWheelie, _snapAirCorr, _snapFakie, _snapPump, _snapIce;
        private static bool _hasSnapshot = false;

        private static readonly string[] ModNames = { "Wheelie Balance", "Air Correction", "Fakie Balance", "Pump Strength", "Ice Physics" };

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled)
            {
                _snapWheelie = GameModifierMods.WheelieBalanceLevel;
                _snapAirCorr = GameModifierMods.InAirCorrLevel;
                _snapFakie = GameModifierMods.FakieBalanceLevel;
                _snapPump = GameModifierMods.PumpStrengthLevel;
                _snapIce = GameModifierMods.IcePhysicsLevel;
                _hasSnapshot = true;
                MelonLogger.Msg("[RandomMutator] ON — snapshotted current modifier levels.");
            }
            else
            {
                RestoreSnapshot();
                MelonLogger.Msg("[RandomMutator] OFF — restored original modifier levels.");
            }
        }

        // Called from SessionTrackers.CheckpointTick when CheckpointCount increments.
        public static void OnCheckpoint()
        {
            if (!Enabled) return;
            try
            {
                int idx = Random.Range(0, ModNames.Length);
                int level = Random.Range(1, 11); // 1-10 inclusive
                switch (idx)
                {
                    case 0: GameModifierMods.SetWheelieBalanceLevel(level); break;
                    case 1: GameModifierMods.SetInAirCorrLevel(level); break;
                    case 2: GameModifierMods.SetFakieBalanceLevel(level); break;
                    case 3: GameModifierMods.SetPumpStrengthLevel(level); break;
                    case 4: GameModifierMods.SetIcePhysicsLevel(level); break;
                }
                LastMutationDisplay = ModNames[idx] + " -> " + level;
                MelonLogger.Msg("[RandomMutator] Checkpoint mutation: " + LastMutationDisplay);
            }
            catch (System.Exception ex) { MelonLogger.Error("[RandomMutator] OnCheckpoint: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "RandomMutatorOnCheckpoint"); }
        }

        private static void RestoreSnapshot()
        {
            if (!_hasSnapshot) return;
            try
            {
                GameModifierMods.SetWheelieBalanceLevel(_snapWheelie);
                GameModifierMods.SetInAirCorrLevel(_snapAirCorr);
                GameModifierMods.SetFakieBalanceLevel(_snapFakie);
                GameModifierMods.SetPumpStrengthLevel(_snapPump);
                GameModifierMods.SetIcePhysicsLevel(_snapIce);
            }
            catch (System.Exception ex) { MelonLogger.Error("[RandomMutator] RestoreSnapshot: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "RandomMutatorOnCheckpoint"); }
            _hasSnapshot = false;
        }

        public static void Reset()
        {
            if (Enabled) RestoreSnapshot();
            Enabled = false;
            LastMutationDisplay = "--";
        }
    }
}
