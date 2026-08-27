using System;
using System.IO;
using MelonLoader;
using UnityEngine;
using DescendersModMenu.Mods;
using DescendersModMenu.UI;

namespace DescendersModMenu
{
    [Serializable]
    public class BindingsData
    {
        public string[] ModIds  = new string[0];
        public int[]    KeyCodes = new int[0];
        public int      MenuOpenCode = -2;
    }

    public static class KeyBindManager
    {
        public static readonly string[] ModIds = new string[]
        {
            // ── General ──────────────────────────────────────────────────
            "SlowMotion",      "FlyMode",         "NoBail",          "CutBrakes",
            "NoSpeedCap",      "ReverseSteering",  "IceMode",         "MirrorMode",
            "DrunkMode",       "SpeedrunTimer",    "SlowMoOnBail",    "GhostToggle",
            "GhostSave",       "WheelieLimit",     "AirControl",      "AutoBalance",
            "QuickBrake",      "BikeTorch",        "DiscoTorch",      "ExplodingProps",
            "NearMiss",        "StickyTyres",      "WideTyres",       "ESP",
            "BikeDamage",      "HeadlightsOnly",   "UIRemover",       "WheelieHUD",
            "InstantRespawn",  "TyrePressure",     "BrakeFade",       "SuspensionHUD",
            "TrickSetSwap",    "ScreenshotMode",   "NoSpeedWobbles",  "LandingImpact",
            "BlackDeath",      "Compass",          "RubberBand",      "FOV",
            "Acceleration",    "MaxSpeed",         "SessionHUD",      "TrickMultiplier",
            "PedalWhileTweak", "ChatHUD",           "ESPDistance",     "ESPTracers",
            "ESPWorldObjects",
            // ── Bike ─────────────────────────────────────────────────────
            "InvisibleBike",   "NextBike",         "PrevBike",        "BouncyBike",
            "HoverMode",
            // ── Move ─────────────────────────────────────────────────────
            "Spin",            "Hop",              "Wheelie",         "Lean",
            // ── Fun / World ───────────────────────────────────────────────
            "MoonMode",        "InvisiblePlayer",  "Trees",           "TurboWind",
            "Fog",             "Music",            "CameraShake",     "CartoonSquash",
            "CartoonJelly",    "Storm",            "DiscoMode",       "BlizzardDial",
            "ObjectPlacer",    "SpectateMode",     "TrailPainter",    "Confetti",
            "BigHead",         "ChaosMode",        "RandomBike",      "RandomMutator",
            "RandomWeather",   "RideOnWater",
            // ── Actions ───────────────────────────────────────────────────
            "SuperLaunch",     "TeleportCheckpoint", "JumpToFinish",    "RespawnAtStart",
            "SkipSong",        "Airhorn",            "ClearPerks",      "GhostClear",
            "TopSpeedReset",   "SpectateNext",       "SpectatePrev",    "TrailColour",
            "GoToShed",        "LeaveShed",          "AllMods",
            // ── Modes ─────────────────────────────────────────────────────
            "LavaRising",      "AvalancheMode",   "EarthquakeMode",   "PoliceChase",
            "BoulderDodge",    "SurvivalMode",    "TrickAttack",      "SpiderBike",
            "AvalancheFail",
            // ── Career ────────────────────────────────────────────────────
            "CompleteMissions","CompleteGrandTour","LevelReset",      "SponsorReset",     "MaxSponsorLevel",
            "UnlockAll"
        };

        public static readonly string[] ModLabels = new string[]
        {
            // ── General ──────────────────────────────────────────────────
            "Slow Motion",       "Fly Mode",               "No Bail",            "Cut Brakes",
            "Remove Speed Cap",  "Reverse Steering",        "Ice Mode",           "Mirror Mode",
            "Drunk Mode",        "Speedrun Timer",          "Slow Mo On Bail",    "Ghost Replay: Toggle",
            "Ghost Replay: Save","Wheelie Angle Limit",     "Air Control",        "Auto Balance",
            "Quick Brake",       "Bike Torch",              "Disco Torch",        "Exploding Props",
            "Near Miss Sensitivity", "Sticky Tyres",        "Wide Tyres",         "ESP",
            "Bike Damage",       "Headlights Only",         "UI Remover",         "Wheelie HUD",
            "Instant Respawn",   "Tyre Pressure",           "Brake Fade",         "Suspension HUD",
            "Trick Set Swap",    "Screenshot Mode",         "No Speed Wobbles",   "Landing Impact",
            "Black Death",       "Compass Always On",       "Rubber Band Steering","FOV",
            "Acceleration",      "Max Speed Multiplier",    "Session HUD",        "Trick Multiplier",
            "Pedal While Tweak", "Chat HUD",                "ESP Distance",       "ESP Tracers",
            "ESP World Objects",
            // ── Bike ─────────────────────────────────────────────────────
            "Invisible Bike",    "Next Bike",              "Previous Bike",       "Bouncy Bike",
            "Hover Mode",
            // ── Move ─────────────────────────────────────────────────────
            "Spin",              "Hop",                     "Wheelie",            "Lean",
            // ── Fun / World ───────────────────────────────────────────────
            "Moon Mode",         "Invisible Player",        "Trees & Foliage",    "Turbo Wind",
            "Fog Remover",       "Music Toggle",            "Camera Shake",       "Cartoon Squash: Bounce",
            "Cartoon Squash: Jelly", "Storm",               "Disco Mode",         "Blizzard Dial",
            "Object Placer",     "Spectate Mode",           "Trail Painter",      "Confetti On Trick",
            "Big Head Mode",     "Chaos Mode",              "Random Bike Switch", "Random Mutator",
            "Random Weather",    "Ride On Water",
            // ── Actions ───────────────────────────────────────────────────
            "Super Launch",      "Teleport to Checkpoint",  "Jump to Finish",     "Respawn at Start",
            "Skip Song",         "Airhorn",                 "Clear All Perks",    "Clear Saved Ghost",
            "Reset Top Speed",   "Spectate: Next",          "Spectate: Previous", "Trail Painter Colour",
            "Go To Shed",        "Leave Shed",              "Mods Master Switch",
            // ── Modes ─────────────────────────────────────────────────────
            "The floor is LAVA", "Avalanche Mode",        "Earthquake Mode",     "Police Chase",
            "Boulder Dodge",     "Survival Mode",         "Trick Attack",        "Spider Bike",
            "Avalanche Instant Fail",
            // ── Career ────────────────────────────────────────────────────
            "Complete All Missions", "Complete Grand Tour", "Reset Level Progress", "Reset Sponsor Progress", "Max Sponsor Level",
            "Unlock All"
        };

        public static int Count { get { return ModIds.Length; } }

        private static int[] _keyCodes = new int[0];

        public const int CtrlDPadUp = -1, CtrlDPadDown = -2, CtrlDPadLeft = -3, CtrlDPadRight = -4;
        public const int CtrlA = -5, CtrlB = -6, CtrlX = -7, CtrlY = -8;
        public const int CtrlLB = -9, CtrlRB = -10, CtrlLT = -11, CtrlRT = -12;
        public const int CtrlLSB = -13, CtrlRSB = -14;

        public static readonly int[] ControllerCodes = new int[]
        {
            CtrlDPadUp, CtrlDPadDown, CtrlDPadLeft, CtrlDPadRight,
            CtrlA, CtrlB, CtrlX, CtrlY,
            CtrlLB, CtrlRB, CtrlLT, CtrlRT,
            CtrlLSB, CtrlRSB
        };

        public static string ControllerName(int code)
        {
            switch (code)
            {
                case CtrlDPadUp:    return "D-Pad Up";
                case CtrlDPadDown:  return "D-Pad Down";
                case CtrlDPadLeft:  return "D-Pad Left";
                case CtrlDPadRight: return "D-Pad Right";
                case CtrlA:  return "A / Cross";
                case CtrlB:  return "B / Circle";
                case CtrlX:  return "X / Square";
                case CtrlY:  return "Y / Triangle";
                case CtrlLB: return "Left Bumper";
                case CtrlRB: return "Right Bumper";
                case CtrlLT: return "Left Trigger";
                case CtrlRT: return "Right Trigger";
                case CtrlLSB: return "Left Stick Click";
                case CtrlRSB: return "Right Stick Click";
                default: return "\u2014";
            }
        }

        public static bool IsControllerPressed(int code)
        {
            MenuInputGuard.ForceAllowInControl = true;
            try
            {
                var control = GetControl(code);
                if ((object)control == null) return false;
                return control.WasPressed;
            }
            catch (Exception ex) { ModLog.Warn("[KeyBindManager] IsControllerPressed(" + code + "): " + ex.Message); return false; }
            finally { MenuInputGuard.ForceAllowInControl = false; }
        }

        public static bool AnyControllerPressed(out int code)
        {
            for (int i = 0; i < ControllerCodes.Length; i++)
            {
                if (IsControllerPressed(ControllerCodes[i])) { code = ControllerCodes[i]; return true; }
            }
            code = 0;
            return false;
        }

        private static int _menuOpenCode = CtrlDPadDown;
        private static bool _skipMenuOpenCheck = false;
        private static bool _menuToggleThisFrame = false;
        private static int _handledFrame = -1;

        public static int  GetMenuOpenCode()          { return _menuOpenCode; }
        public static void SetMenuOpenCode(int code)  { _menuOpenCode = code; }
        public static void SkipMenuOpenCheck()        { _skipMenuOpenCheck = true; }

        /// <summary>True when the menu-open bind fired this frame (before GamepadCursor click).</summary>
        public static bool MenuToggleThisFrame => _menuToggleThisFrame;

        public static InControl.OneAxisInputControl GetMenuOpenControl()
        {
            return GetControl(_menuOpenCode);
        }

        /// <summary>
        /// True on the frame the menu-open controller bind is pressed (InControl WasPressed).
        /// Safe to call from both OnUpdate and OnLateUpdate — only returns true once per frame.
        /// LateUpdate catches the press when Melon runs before InControl in that frame.
        /// </summary>
        public static bool CheckMenuOpenPressed()
        {
            _menuToggleThisFrame = false;
            if (_handledFrame == Time.frameCount) return false;

            if (_skipMenuOpenCheck)
            {
                _skipMenuOpenCheck = false;
                return false;
            }

            if (_menuOpenCode == 0) return false;

            try
            {
                var control = GetControl(_menuOpenCode);
                if ((object)control == null) return false;
                if (!control.WasPressed) return false;

                _handledFrame = Time.frameCount;
                _menuToggleThisFrame = true;
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn("[KeyBindManager] CheckMenuOpenPressed: " + ex.Message);
                return false;
            }
        }

        public static bool IsControllerHeld(int code)
        {
            try
            {
                var control = GetControl(code);
                if ((object)control == null) return false;
                return control.IsPressed;
            }
            catch (Exception ex) { ModLog.Warn("[KeyBindManager] IsControllerHeld(" + code + "): " + ex.Message); return false; }
        }

        private static InControl.OneAxisInputControl GetControl(int code)
        {
            var dev = InControl.InputManager.ActiveDevice;
            if ((object)dev == null) return null;
            switch (code)
            {
                case CtrlDPadUp:    return dev.DPadUp;
                case CtrlDPadDown:  return dev.DPadDown;
                case CtrlDPadLeft:  return dev.DPadLeft;
                case CtrlDPadRight: return dev.DPadRight;
                case CtrlA:  return dev.Action1;
                case CtrlB:  return dev.Action2;
                case CtrlX:  return dev.Action3;
                case CtrlY:  return dev.Action4;
                case CtrlLB: return dev.LeftBumper;
                case CtrlRB: return dev.RightBumper;
                case CtrlLT: return dev.LeftTrigger;
                case CtrlRT: return dev.RightTrigger;
                case CtrlLSB: return dev.LeftStickButton;
                case CtrlRSB: return dev.RightStickButton;
                default: return null;
            }
        }

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
            _menuOpenCode = CtrlDPadDown;
        }

        public static int     GetKey(int slot)     { return slot >= 0 && slot < _keyCodes.Length ? _keyCodes[slot] : (int)KeyCode.None; }
        public static KeyCode GetKeyCode(int slot) { return (KeyCode)GetKey(slot); }

        public static bool HasBind(int slot)
        {
            int code = GetKey(slot);
            return code != 0 && code != (int)KeyCode.None;
        }

        public static string GetBindDisplay(int slot)
        {
            int code = GetKey(slot);
            if (!HasBind(slot)) return "\u2014";
            if (code < 0) return ControllerName(code);
            return ((KeyCode)code).ToString();
        }

        public static void SetKey(int slot, KeyCode key)
        {
            SetCode(slot, (int)key);
        }

        public static void SetCode(int slot, int code)
        {
            if (slot < 0 || slot >= _keyCodes.Length) return;
            _keyCodes[slot] = code;
        }

        public static void ClearKey(int slot)
        {
            if (slot < 0 || slot >= _keyCodes.Length) return;
            _keyCodes[slot] = (int)KeyCode.None;
        }

        public static int FindConflict(KeyCode key, int exceptSlot)
        {
            return FindConflict((int)key, exceptSlot);
        }

        public static int FindConflict(int code, int exceptSlot)
        {
            if (code == 0 || code == (int)KeyCode.None) return -1;
            for (int i = 0; i < _keyCodes.Length; i++)
            {
                if (i == exceptSlot) continue;
                if (_keyCodes[i] == code) return i;
            }
            return -1;
        }

        public static bool IsKeyBoundToMod(KeyCode key)
        {
            return FindConflict(key, -1) >= 0;
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
                    ModIds       = (string[])ModIds.Clone(),
                    KeyCodes     = (int[])_keyCodes.Clone(),
                    MenuOpenCode = _menuOpenCode
                };
                File.WriteAllText(Path.Combine(SaveFolder, SaveFileName), JsonUtility.ToJson(data, true));
                ModLog.Debug("[KeyBindManager] Saved " + ModIds.Length + " bindings.");
            }
            catch (Exception ex) { MelonLogger.Error("[KeyBindManager] SaveBindings: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "KeyBindManager"); }
        }

        public static void LoadBindings()
        {
            SetDefaults();
            try
            {
                string path = Path.Combine(SaveFolder, SaveFileName);
                if (!File.Exists(path)) { ModLog.Debug("[KeyBindManager] No bindings file — using defaults (F2/F3/F4)."); return; }
                var data = JsonUtility.FromJson<BindingsData>(File.ReadAllText(path));
                if (data == null || data.ModIds == null || data.KeyCodes == null) { ModLog.Warn("[KeyBindManager] Corrupt bindings file — using defaults."); return; }
                int loaded = 0;
                for (int fi = 0; fi < data.ModIds.Length; fi++)
                {
                    int slot = IndexOf(data.ModIds[fi]);
                    if (slot < 0) continue;
                    if (fi < data.KeyCodes.Length) { _keyCodes[slot] = data.KeyCodes[fi]; loaded++; }
                }
                if (data.MenuOpenCode != 0) _menuOpenCode = data.MenuOpenCode;
                ModLog.Debug("[KeyBindManager] Loaded " + loaded + " bindings from file. MenuOpenCode=" + _menuOpenCode);
            }
            catch (Exception ex) { MelonLogger.Error("[KeyBindManager] LoadBindings: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "KeyBindManager"); }
        }

        private static bool _skipNextCheck = false;
        public static void SkipNextCheck() { _skipNextCheck = true; }

        public static void CheckAll()
        {
            if (_skipNextCheck) { _skipNextCheck = false; return; }
            for (int i = 0; i < _keyCodes.Length; i++)
            {
                int code = _keyCodes[i];
                if (code == 0 || code == (int)KeyCode.None) continue;
                // Don't fire a mod bind that matches the menu-open controller button.
                if (code < 0 && code == _menuOpenCode) continue;
                bool pressed = code < 0
                    ? IsControllerPressed(code)
                    : Input.GetKeyDown((KeyCode)code);
                if (!pressed) continue;
                FireMod(ModIds[i]);
            }
        }

        private static void RefreshModeUi()
        {
            try { ModesPage.RefreshAll(); } catch { }
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
                    case "DiscoTorch":         BikeTorch.ToggleDisco();                           break;
                    case "ExplodingProps":     ExplodingProps.Toggle();                           break;
                    case "NearMiss":           NearMissSensitivity.Toggle();                      break;
                    case "StickyTyres":        StickyTyres.Toggle();                              break;
                    case "SpiderBike":         SpiderBike.Toggle();                               break;
                    case "WideTyres":          if (WideTyres.IsModified) WideTyres.Reset(); else WideTyres.Increase(); break;
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
                    case "LandingImpact":      LandingImpact.Toggle();                            break;
                    case "BlackDeath":         BlackDeath.Toggle();                               break;
                    case "Compass":            CompassAlwaysOn.Toggle();                          break;
                    case "RubberBand":         RubberBandSteering.Toggle();                       break;
                    case "FOV":                FOV.Toggle();                                      break;
                    case "Acceleration":       Acceleration.Toggle();                             break;
                    case "MaxSpeed":           MaxSpeedMultiplier.Toggle();                       break;
                    case "SessionHUD":         SessionHUD.Toggle();                               break;
                    case "TrickMultiplier":    TrickMultiplier.Toggle();                          break;
                    case "PedalWhileTweak":    PedalWhileTweak.Toggle();                          break;
                    case "ChatHUD":            ChatHUD.Toggle();                                  break;
                    case "ESPDistance":        ESP.ToggleDistance();                              break;
                    case "ESPTracers":         ESP.ToggleTracers();                               break;
                    case "ESPWorldObjects":    ESP.ToggleWorldObjects();                          break;
                    case "InvisibleBike":      InvisibleBike.Toggle();                            break;
                    case "NextBike":           BikeSwitcher.NextBike();                           break;
                    case "PrevBike":           BikeSwitcher.PreviousBike();                       break;
                    case "BouncyBike":         BouncyBike.Toggle();                               break;
                    case "HoverMode":          HoverMode.Toggle();                                break;
                    case "Spin":               Movement.ToggleSpin();                             break;
                    case "Hop":                Movement.ToggleHop();                              break;
                    case "Wheelie":            Movement.ToggleWheelie();                          break;
                    case "Lean":               Movement.ToggleLean();                             break;
                    case "MoonMode":           MoonMode.Toggle();                                 break;
                    case "InvisiblePlayer":    InvisiblePlayer.Toggle();                          break;
                    case "Trees":              Trees.Toggle();                                    break;
                    case "TurboWind":          TurboWind.Toggle();                                break;
                    case "Fog":                Fog.Toggle();                                      break;
                    case "Music":              Music.Toggle();                                    break;
                    case "CameraShake":        CameraShake.Toggle();                              break;
                    case "CartoonSquash":
                        if (CartoonSquash.JellyMode) CartoonSquash.SetJellyEnabled(false);
                        CartoonSquash.Toggle();
                        break;
                    case "CartoonJelly":
                        if (CartoonSquash.Enabled) CartoonSquash.SetEnabled(false);
                        CartoonSquash.ToggleJelly();
                        break;
                    case "Storm":              SkyColours.ToggleStorm();                          break;
                    case "DiscoMode":          DiscoMode.Toggle();                                break;
                    case "BlizzardDial":       BlizzardDial.Toggle();                             break;
                    case "ObjectPlacer":       ObjectPlacer.Toggle();                             break;
                    case "SpectateMode":       SpectateMode.Toggle();                             break;
                    case "TrailPainter":       TrailPainter.Toggle();                             break;
                    case "Confetti":           ConfettiOnTrick.Toggle();                          break;
                    case "BigHead":            BigHeadMode.Toggle();                              break;
                    case "ChaosMode":          ChaosMode.Toggle();                                break;
                    case "RandomBike":         RandomBikeSwitch.Toggle();                         break;
                    case "RandomMutator":      RandomMutatorOnCheckpoint.Toggle();                break;
                    case "RandomWeather":      RandomWeatherRoulette.Toggle();                    break;
                    case "RideOnWater":        RideOnWater.Toggle();                              break;
                    case "SuperLaunch":        DoSuperLaunch();                                   break;
                    case "TeleportCheckpoint": TeleportToCheckpoint.Teleport();                   break;
                    case "JumpToFinish":       DoJumpToFinish();                                  break;
                    case "RespawnAtStart":     DoRespawnAtStart();                                break;
                    case "SkipSong":           DoSkipSong();                                      break;
                    case "Airhorn":            Airhorn.Honk();                                    break;
                    case "ClearPerks":         PerkMenu.ClearAllPerks();                          break;
                    case "GhostClear":         GhostReplay.ClearSavedRun();                       break;
                    case "TopSpeedReset":      TopSpeed.ResetSession();                           break;
                    case "SpectateNext":       SpectateMode.Next();                               break;
                    case "SpectatePrev":       SpectateMode.Previous();                           break;
                    case "TrailColour":        TrailPainter.CycleColour();                        break;
                    case "GoToShed":           OutfitPage.GoToShed();                             break;
                    case "LeaveShed":          OutfitPage.LeaveShed();                            break;
                    case "AllMods":            AllModsSwitch.Toggle();                            break;
                    case "LavaRising":         LavaRising.Toggle(); RefreshModeUi();                 break;
                    case "AvalancheMode":      AvalancheMode.Toggle(); RefreshModeUi();              break;
                    case "EarthquakeMode":     EarthquakeMode.Toggle(); RefreshModeUi();             break;
                    case "PoliceChase":        PoliceChaseMode.Toggle(); RefreshModeUi();            break;
                    case "BoulderDodge":       BoulderDodgeMode.Toggle(); RefreshModeUi();           break;
                    case "SurvivalMode":       SurvivalMode.Toggle(); RefreshModeUi();              break;
                    case "TrickAttack":        TrickAttackMode.Toggle(); RefreshModeUi();           break;
                    case "AvalancheFail":
                        AvalancheMode.InstantFail = !AvalancheMode.InstantFail;
                        ModLog.Feedback("[Avalanche] Instant Fail -> " + (AvalancheMode.InstantFail ? "ON" : "OFF"));
                        break;
                    case "CompleteMissions":   CareerReset.CompleteAllMissions();                  break;
                    case "CompleteGrandTour":  CareerReset.CompleteGrandTour();                   break;
                    case "LevelReset":         CareerReset.ResetLevelProgress();                  break;
                    case "SponsorReset":       CareerReset.ResetSponsorProgress();                break;
                    case "MaxSponsorLevel":    CareerReset.MaxSponsorLevel();                     break;
                    case "UnlockAll":          CareerReset.ToggleUnlockAll();                     break;
                    default: ModLog.Warn("[KeyBindManager] Unknown mod id: " + id);      break;
                }
            }
            catch (Exception ex) { MelonLogger.Error("[KeyBindManager] FireMod(" + id + "): " + ex.Message);  Telemetry.ReportErrorAsync(ex, "KeyBindManager"); }
        }

        private static void DoSuperLaunch()
        {
            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) { ModLog.Debug("[KeyBindManager] SuperLaunch: no Player_Human"); return; }
            Vehicle v = player.GetComponent<Vehicle>();
            if ((object)v == null) { ModLog.Debug("[KeyBindManager] SuperLaunch: no Vehicle"); return; }
            var setVel = typeof(Vehicle).GetMethod("SetVelocity",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if ((object)setVel == null) { ModLog.Warn("[KeyBindManager] SuperLaunch: SetVelocity not found"); return; }
            setVel.Invoke(v, new object[] { player.transform.forward * 80f + Vector3.up * 20f });
        }

        private static void DoJumpToFinish()
        {
            string err;
            if (!SessionCommands.TryJumpToFinish(out err))
            {
                if (!string.IsNullOrEmpty(err))
                    ModLog.Feedback("[JumpToFinish] " + err);
            }
        }

        private static void DoRespawnAtStart()
        {
            try
            {
                PlayerManager pm = UnityEngine.Object.FindObjectOfType<PlayerManager>();
                if ((object)pm == null)
                {
                    ModLog.Feedback("[RespawnAtStart] Not in a session.");
                    return;
                }
                var getPii = typeof(PlayerManager).GetMethod("GetPlayerImpact",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)getPii == null) return;
                object pii = getPii.Invoke(pm, null);
                if ((object)pii == null) return;
                var respawn = pii.GetType().GetMethod("RespawnAtStartLine",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                    null, new System.Type[] { typeof(bool) }, null);
                if ((object)respawn == null) return;
                respawn.Invoke(pii, new object[] { true });
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[KeyBindManager] RespawnAtStart: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "KeyBindManager");
            }
        }

        private static void DoSkipSong()
        {
            try { DevCommandsGameplay.SkipSong(); }
            catch (Exception ex)
            {
                MelonLogger.Error("[KeyBindManager] SkipSong: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "KeyBindManager");
            }
        }
    }
}

