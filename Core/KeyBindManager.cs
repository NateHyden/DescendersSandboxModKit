using System;
using System.IO;
using MelonLoader;
using UnityEngine;
using DescendersModMenu.Mods;
using DescendersModMenu.UI;

namespace DescendersModMenu
{
    // ── Serialisation wrapper for JsonUtility ─────────────────────────────
    [Serializable]
    public class BindingsData
    {
        public string[] ModIds  = new string[0];
        public int[]    KeyCodes = new int[0];
    }

    public static class KeyBindManager
    {
        // ── Mod registry — parallel arrays, index is the slot ─────────────
        public static readonly string[] ModIds = new string[]
        {
            // ── General ──────────────────────────────────────────────────
            "SlowMotion",      "FlyMode",         "NoBail",          "CutBrakes",
            "NoSpeedCap",      "ReverseSteering",  "IceMode",         "MirrorMode",
            "DrunkMode",       "SpeedrunTimer",    "SlowMoOnBail",    "GhostToggle",
            "GhostSave",       "WheelieLimit",     "AirControl",      "AutoBalance",
            "QuickBrake",      "BikeTorch",        "ExplodingProps",  "NearMiss",
            "StickyTyres",     "WideTyres",        "ESP",             "BikeDamage",
            "HeadlightsOnly",  "UIRemover",        "WheelieHUD",      "InstantRespawn",
            "TyrePressure",    "BrakeFade",        "SuspensionHUD",   "TrickSetSwap",
            "ScreenshotMode",  "NoSpeedWobbles",
            // ── Bike ─────────────────────────────────────────────────────
            "InvisibleBike",   "NextBike",         "PrevBike",
            // ── Fun / World ───────────────────────────────────────────────
            "MoonMode",        "InvisiblePlayer",  "Trees",           "TurboWind",
            "Fog",             "Music",
            // ── Actions ───────────────────────────────────────────────────
            "SuperLaunch",     "TeleportCheckpoint",
            // ── Modes ─────────────────────────────────────────────────────
            "AvalancheMode",   "EarthquakeMode",   "PoliceChase",     "BoulderDodge",
            "SurvivalMode",    "TrickAttack"
        };

        public static readonly string[] ModLabels = new string[]
        {
            // ── General ──────────────────────────────────────────────────
            "Slow Motion",       "Fly Mode",               "No Bail",            "Cut Brakes",
            "Remove Speed Cap",  "Reverse Steering",        "Ice Mode",           "Mirror Mode",
            "Drunk Mode",        "Speedrun Timer",          "Slow Mo On Bail",    "Ghost Replay: Toggle",
            "Ghost Replay: Save","Wheelie Angle Limit",     "Air Control",        "Auto Balance",
            "Quick Brake",       "Bike Torch",              "Exploding Props",    "Near Miss Sensitivity",
            "Sticky Tyres",      "Wide Tyres",              "ESP",                "Bike Damage",
            "Headlights Only",   "UI Remover",              "Wheelie HUD",        "Instant Respawn",
            "Tyre Pressure",     "Brake Fade",              "Suspension HUD",     "Trick Set Swap",
            "Screenshot Mode",   "No Speed Wobbles",
            // ── Bike ─────────────────────────────────────────────────────
            "Invisible Bike",    "Next Bike",              "Previous Bike",
            // ── Fun / World ───────────────────────────────────────────────
            "Moon Mode",         "Invisible Player",        "Trees & Foliage",    "Turbo Wind",
            "Fog Remover",       "Music Toggle",
            // ── Actions ───────────────────────────────────────────────────
            "Super Launch",      "Teleport to Checkpoint",
            // ── Modes ─────────────────────────────────────────────────────
            "Avalanche Mode",    "Earthquake Mode",        "Police Chase",       "Boulder Dodge",
            "Survival Mode",     "Trick Attack"
        };

        public static int Count { get { return ModIds.Length; } }

        // ── Live binding state (sized in SetDefaults) ─────────────────────
        private static int[] _keyCodes = new int[0];

        private static readonly string SaveFolder = Path.Combine(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData"),
            "DescendersModMenu");
        private const string SaveFileName = "KeyBindings.json";

        private static void SetDefaults()
        {
            _keyCodes = new int[ModIds.Length];
            for (int i = 0; i < _keyCodes.Length; i++) _keyCodes[i] = (int)KeyCode.None;
            int slotF2 = IndexOf("SlowMotion");    if (slotF2 >= 0) _keyCodes[slotF2] = (int)KeyCode.F2;
            int slotF3 = IndexOf("GhostToggle");   if (slotF3 >= 0) _keyCodes[slotF3] = (int)KeyCode.F3;
            int slotF4 = IndexOf("GhostSave");     if (slotF4 >= 0) _keyCodes[slotF4] = (int)KeyCode.F4;
        }

        public static int     GetKey(int slot)     { return slot >= 0 && slot < _keyCodes.Length ? _keyCodes[slot] : (int)KeyCode.None; }
        public static KeyCode GetKeyCode(int slot) { return (KeyCode)GetKey(slot); }

        public static void SetKey(int slot, KeyCode key)
        {
            if (slot < 0 || slot >= _keyCodes.Length) return;
            _keyCodes[slot] = (int)key;
        }

        public static void ClearKey(int slot)
        {
            if (slot < 0 || slot >= _keyCodes.Length) return;
            _keyCodes[slot] = (int)KeyCode.None;
        }

        public static int FindConflict(KeyCode key, int exceptSlot)
        {
            if (key == KeyCode.None) return -1;
            int code = (int)key;
            for (int i = 0; i < _keyCodes.Length; i++)
            {
                if (i == exceptSlot) continue;
                if (_keyCodes[i] == code) return i;
            }
            return -1;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < ModIds.Length; i++)
                if (ModIds[i] == id) return i;
            return -1;
        }

        public static void SaveBindings()
        {
            try
            {
                if (!Directory.Exists(SaveFolder)) Directory.CreateDirectory(SaveFolder);
                var data = new BindingsData
                {
                    ModIds   = (string[])ModIds.Clone(),
                    KeyCodes = (int[])_keyCodes.Clone()
                };
                File.WriteAllText(Path.Combine(SaveFolder, SaveFileName), JsonUtility.ToJson(data, true));
                MelonLogger.Msg("[KeyBindManager] Saved " + ModIds.Length + " bindings.");
            }
            catch (Exception ex) { MelonLogger.Error("[KeyBindManager] SaveBindings: " + ex.Message); }
        }

        public static void LoadBindings()
        {
            SetDefaults();
            try
            {
                string path = Path.Combine(SaveFolder, SaveFileName);
                if (!File.Exists(path)) { MelonLogger.Msg("[KeyBindManager] No bindings file — using defaults (F2/F3/F4)."); return; }
                var data = JsonUtility.FromJson<BindingsData>(File.ReadAllText(path));
                if (data == null || data.ModIds == null || data.KeyCodes == null) { MelonLogger.Warning("[KeyBindManager] Corrupt bindings file — using defaults."); return; }
                int loaded = 0;
                for (int fi = 0; fi < data.ModIds.Length; fi++)
                {
                    int slot = IndexOf(data.ModIds[fi]);
                    if (slot < 0) continue;
                    if (fi < data.KeyCodes.Length) { _keyCodes[slot] = data.KeyCodes[fi]; loaded++; }
                }
                MelonLogger.Msg("[KeyBindManager] Loaded " + loaded + " bindings from file.");
            }
            catch (Exception ex) { MelonLogger.Error("[KeyBindManager] LoadBindings: " + ex.Message); }
        }

        // Set by BindsPage.OnGUI when a key is committed, to suppress firing that
        // key in the same/next OnUpdate tick (execution order is not guaranteed).
        private static bool _skipNextCheck = false;
        public static void SkipNextCheck() { _skipNextCheck = true; }

        public static void CheckAll()
        {
            if (_skipNextCheck) { _skipNextCheck = false; return; }
            for (int i = 0; i < _keyCodes.Length; i++)
            {
                if (_keyCodes[i] == (int)KeyCode.None) continue;
                if (!Input.GetKeyDown((KeyCode)_keyCodes[i])) continue;
                FireMod(ModIds[i]);
            }
        }

        private static void FireMod(string id)
        {
            try
            {
                switch (id)
                {
                    case "SlowMotion":         SlowMotion.Toggle();                               break;
                    case "FlyMode":            FlyMode.Toggle();                                  break;
                    case "NoBail":             NoBail.Toggle();                                   break;
                    case "CutBrakes":          CutBrakes.Toggle();                                break;
                    case "NoSpeedCap":         NoSpeedCap.Toggle();                               break;
                    case "ReverseSteering":    ReverseSteering.Toggle();                          break;
                    case "IceMode":            IceMode.Toggle();                                  break;
                    case "MirrorMode":         MirrorMode.Toggle();                               break;
                    case "DrunkMode":          DrunkMode.Toggle();                                break;
                    case "SpeedrunTimer":      SpeedrunTimer.Toggle();                            break;
                    case "SlowMoOnBail":       SlowMoOnBail.Toggle();                             break;
                    case "GhostToggle":        GhostReplay.Toggle(); GhostPage.RefreshAll();      break;
                    case "GhostSave":          GhostReplay.SaveRun(); GhostPage.RefreshAll();     break;
                    case "WheelieLimit":       WheelieAngleLimit.Toggle();                        break;
                    case "AirControl":         AirControl.Toggle();                               break;
                    case "AutoBalance":        AutoBalance.Toggle();                              break;
                    case "QuickBrake":         QuickBrake.Toggle();                               break;
                    case "BikeTorch":          BikeTorch.Toggle();                                break;
                    case "ExplodingProps":     ExplodingProps.Toggle();                           break;
                    case "NearMiss":           NearMissSensitivity.Toggle();                      break;
                    case "StickyTyres":        StickyTyres.Toggle();                              break;
                    case "WideTyres":          WideTyres.Toggle();                                break;
                    case "ESP":                ESP.Toggle();                                      break;
                    case "BikeDamage":         BikeDamage.Toggle();                               break;
                    case "HeadlightsOnly":     HeadlightsOnly.Toggle();                           break;
                    case "UIRemover":          UIRemover.Toggle();                                break;
                    case "WheelieHUD":         WheelieHUD.Toggle();                               break;
                    case "InstantRespawn":     InstantRespawn.Toggle();                           break;
                    case "TyrePressure":       TyrePressure.Toggle();                             break;
                    case "BrakeFade":          BrakeFade.Toggle();                                break;
                    case "SuspensionHUD":      SuspensionHUD.Toggle();                            break;
                    case "TrickSetSwap":       TrickSetSwap.Toggle();                             break;
                    case "ScreenshotMode":     ScreenshotMode.Toggle();                           break;
                    case "NoSpeedWobbles":     GameModifierMods.NoSpeedWobblesToggle();           break;
                    case "InvisibleBike":      InvisibleBike.Toggle();                            break;
                    case "NextBike":           BikeSwitcher.NextBike();                           break;
                    case "PrevBike":           BikeSwitcher.PreviousBike();                       break;
                    case "MoonMode":           MoonMode.Toggle();                                 break;
                    case "InvisiblePlayer":    InvisiblePlayer.Toggle();                          break;
                    case "Trees":              Trees.Toggle();                                    break;
                    case "TurboWind":          TurboWind.Toggle();                                break;
                    case "Fog":                Fog.Toggle();                                      break;
                    case "Music":              Music.Toggle();                                    break;
                    case "SuperLaunch":        DoSuperLaunch();                                   break;
                    case "TeleportCheckpoint": TeleportToCheckpoint.Teleport();                   break;
                    case "AvalancheMode":      AvalancheMode.Toggle();                            break;
                    case "EarthquakeMode":     EarthquakeMode.Toggle();                           break;
                    case "PoliceChase":        PoliceChaseMode.Toggle();                          break;
                    case "BoulderDodge":       BoulderDodgeMode.Toggle();                         break;
                    case "SurvivalMode":       SurvivalMode.Toggle();                             break;
                    case "TrickAttack":        TrickAttackMode.Toggle();                          break;
                    default: MelonLogger.Warning("[KeyBindManager] Unknown mod id: " + id);      break;
                }
            }
            catch (Exception ex) { MelonLogger.Error("[KeyBindManager] FireMod(" + id + "): " + ex.Message); }
        }

        private static void DoSuperLaunch()
        {
            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) { MelonLogger.Msg("[KeyBindManager] SuperLaunch: no Player_Human"); return; }
            Vehicle v = player.GetComponent<Vehicle>();
            if ((object)v == null) { MelonLogger.Msg("[KeyBindManager] SuperLaunch: no Vehicle"); return; }
            var setVel = typeof(Vehicle).GetMethod("SetVelocity",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if ((object)setVel == null) { MelonLogger.Warning("[KeyBindManager] SuperLaunch: SetVelocity not found"); return; }
            setVel.Invoke(v, new object[] { player.transform.forward * 80f + Vector3.up * 20f });
        }
    }
}
