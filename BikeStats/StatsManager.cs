using System;
using System.IO;
using MelonLoader;
using UnityEngine;
using DescendersModMenu.Mods;
using DescendersModMenu.UI;

namespace DescendersModMenu.BikeStats
{
    public static class StatsManager
    {
        private static readonly string SaveFolder =
            Path.Combine(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData"),
                "DescendersModMenu"
            );

        private static readonly string SaveFile =
            Path.Combine(SaveFolder, "BikeStats.json");

        // ══════════════════════════════════════════════════════════════
        //  SAVE — captures everything EXCEPT:
        //    Graphics tab, Sky section, Modes, Ghost Replay, ESP
        // ══════════════════════════════════════════════════════════════
        public static void SaveStats()
        {
            try
            {
                EnsureSaveFolder();

                var data = new BikeStatsData
                {
                    // Bike / Stats
                    AccelerationLevel = Acceleration.Level,
                    MaxSpeedLevel = MaxSpeedMultiplier.Level,
                    LandingImpactLevel = LandingImpact.Level,
                    NoBailEnabled = NoBail.Enabled,
                    BikeIndex = BikeSwitcher.CurrentBikeIndex,

                    // Movement
                    SpinLevel = Movement.SpinLevel,
                    HopLevel = Movement.HopLevel,
                    WheelieLevel = Movement.WheelieLevel,
                    LeanLevel = Movement.LeanLevel,

                    // Suspension
                    SuspTravelLevel = Suspension.TravelLevel,
                    SuspStiffnessLevel = Suspension.StiffnessLevel,
                    SuspDampingLevel = Suspension.DampingLevel,

                    // Game Modifiers
                    WheelieBalanceLevel = GameModifierMods.WheelieBalanceLevel,
                    InAirCorrLevel = GameModifierMods.InAirCorrLevel,
                    FakieBalanceLevel = GameModifierMods.FakieBalanceLevel,
                    PumpStrengthLevel = GameModifierMods.PumpStrengthLevel,
                    IcePhysicsLevel = GameModifierMods.IcePhysicsLevel,

                    // World (NOT sky, NOT time of day — gravity only)
                    FovLevel = FOV.Level,
                    GravityLevel = Gravity.Level,

                    // Tyres
                    WideTyresEnabled = WideTyres.Enabled,
                    WideTyresLevel = WideTyres.Level,
                    StickyTyresEnabled = StickyTyres.Enabled,
                    StickyForce = StickyTyres.SuctionForce,

                    // Toggles
                    SlowMotionEnabled = SlowMotion.Enabled,
                    SlowMotionLevel = SlowMotion.Level,
                    CutBrakesEnabled = CutBrakes.Enabled,
                    NoSpeedCapEnabled = NoSpeedCap.Enabled,
                    ReverseSteerEnabled = ReverseSteering.Enabled,
                    IceModeEnabled = IceMode.Enabled,
                    MirrorModeEnabled = MirrorMode.Enabled,
                    DrunkModeEnabled = DrunkMode.Enabled,
                    FlyModeEnabled = FlyMode.Enabled,
                    SpeedrunTimerEnabled = SpeedrunTimer.Enabled,
                    SlowMoOnBailEnabled = SlowMoOnBail.Enabled,

                    WheelieAngleLimitEnabled = WheelieAngleLimit.Enabled,
                    WheelieAngleLimitLevel = WheelieAngleLimit.Level,
                    AirControlEnabled = AirControl.Enabled,
                    AirControlLevel = AirControl.Level,

                    // General toggles
                    AccelerationEnabled = Acceleration.Enabled,
                    MaxSpeedEnabled = MaxSpeedMultiplier.Enabled,
                    LandingImpactEnabled = LandingImpact.Enabled,
                    FovEnabled = FOV.Enabled,
                    AutoBalanceEnabled = AutoBalance.Enabled,
                    AutoBalanceStrengthLevel = AutoBalance.StrengthLevel,
                    NoSpeedWobblesEnabled = GameModifierMods.NoSpeedWobblesEnabled,

                    // Movement toggles
                    SpinEnabled = Movement.SpinEnabled,
                    HopEnabled = Movement.HopEnabled,
                    WheelieEnabled = Movement.WheelieEnabled,
                    LeanEnabled = Movement.LeanEnabled,

                    // Quick Brake
                    QuickBrakeEnabled = QuickBrake.Enabled,
                    QuickBrakeLevel = QuickBrake.Level,

                    // Floats
                    FlyMoveSpeed = FlyMode.MoveSpeed,
                    FlyClimbSpeed = FlyMode.ClimbSpeed,

                    // Menu Customiser
                    MenuPositionPreset = MenuCustomiser.PositionPreset,
                    MenuScaleLevel = MenuCustomiser.ScaleLevel,
                    MenuOpacityLevel = MenuCustomiser.OpacityLevel,

                    // Bike Torch
                    BikeTorchEnabled = BikeTorch.Enabled,
                    BikeTorchIntensityIndex = BikeTorch.IntensityIndex,

                    // Camera Shake
                    CameraShakeEnabled = CameraShake.Enabled,
                    CameraShakeLevel = CameraShake.Level,

                    // Center of Mass
                    CenterOfMassLR = CenterOfMass.OffsetLR,
                    CenterOfMassFB = CenterOfMass.OffsetFB,
                    CenterOfMassUD = CenterOfMass.OffsetUD,

                    // Exploding Props
                    ExplodingPropsEnabled = ExplodingProps.Enabled,

                    // Near Miss Sensitivity
                    NearMissEnabled = NearMissSensitivity.Enabled,
                    NearMissLevel = NearMissSensitivity.Level,

                    // Bike / Player Scale
                    BikeScale = BikeSize.CurrentScale,
                    PlayerScale = PlayerSize.CurrentScale,
                    BikeSizeLevel = BikeSize.Level,
                    PlayerSizeLevel = PlayerSize.Level,
                    InvisibleBikeEnabled = InvisibleBike.Enabled,
                    InvisiblePlayerEnabled = InvisiblePlayer.Enabled,
                    WheelSizeEnabled = WheelSize.IsEnabled,
                    WheelSizeMode = WheelSize.Mode,
                    WheelSizeLevel = WheelSize.Level,
                    FrontWheelSizeLevel = WheelSize.FrontLevel,
                    RearWheelSizeLevel = WheelSize.RearLevel,
                    IndividualWheelMode = WheelSize.IsIndividualMode,

                    // Suspension HUD
                    SuspensionHUDEnabled = SuspensionHUD.Enabled,

                    // Brake Fade
                    BrakeFadeEnabled = BrakeFade.Enabled,
                    BrakeBalanceLevel = BrakeFade.BalanceLevel,

                    // Tyre Pressure
                    TyrePressureEnabled = TyrePressure.Enabled,
                    TyrePressureLevel = TyrePressure.Level,

                    // Instant Respawn
                    InstantRespawnEnabled = InstantRespawn.Enabled,

                    // Bike Damage
                    BikeDamageEnabled = BikeDamage.Enabled,
                    HeadlightsOnlyEnabled = HeadlightsOnly.Enabled,
                    UIRemoverEnabled = UIRemover.Enabled,

                    // Wheelie HUD
                    WheelieHUDEnabled = WheelieHUD.Enabled,

                    // Trick Set Swap
                    TrickSetSwapEnabled = TrickSetSwap.Enabled,
                    TrickSetSwapSourceName = TrickSetSwap.CurrentSourceName,
                };

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFile, json);
                MelonLogger.Msg("[StatsManager] Saved to: " + SaveFile);
            }
            catch (Exception ex) { MelonLogger.Error("[StatsManager] SaveStats: " + ex.Message); }
        }

        // ══════════════════════════════════════════════════════════════
        //  LOAD — restores everything that was saved (same exclusions)
        // ══════════════════════════════════════════════════════════════
        public static void LoadStats()
        {
            // Reset to clean defaults first — ensures mods that are currently ON
            // but saved as OFF get properly turned off
            try { ResetStats(); }
            catch (System.Exception ex) { MelonLogger.Warning("[StatsManager] Pre-load reset: " + ex.Message); }

            try
            {
                if (!File.Exists(SaveFile))
                { MelonLogger.Warning("[StatsManager] No save file found: " + SaveFile); return; }

                string json = File.ReadAllText(SaveFile);
                BikeStatsData data = JsonUtility.FromJson<BikeStatsData>(json);
                if (data == null)
                { MelonLogger.Warning("[StatsManager] JSON returned null."); return; }

                // Bike / Stats
                Acceleration.SetLevel(data.AccelerationLevel);
                MaxSpeedMultiplier.SetLevel(data.MaxSpeedLevel);
                LandingImpact.SetLevel(data.LandingImpactLevel);
                NoBail.SetEnabled(data.NoBailEnabled);
                BikeSwitcher.SetBike(data.BikeIndex);

                // Floats first
                FlyMode.MoveSpeed = data.FlyMoveSpeed;
                FlyMode.ClimbSpeed = data.FlyClimbSpeed;
                StickyTyres.SuctionForce = data.StickyForce;
                SlowMotion.SetLevel(data.SlowMotionLevel);

                // Levels
                Movement.SetSpinLevel(data.SpinLevel);
                Movement.SetHopLevel(data.HopLevel);
                Movement.SetWheelieLevel(data.WheelieLevel);
                Movement.SetLeanLevel(data.LeanLevel);
                Suspension.SetTravelLevel(data.SuspTravelLevel);
                Suspension.SetStiffnessLevel(data.SuspStiffnessLevel);
                Suspension.SetDampingLevel(data.SuspDampingLevel);
                GameModifierMods.SetWheelieBalanceLevel(data.WheelieBalanceLevel);
                GameModifierMods.SetInAirCorrLevel(data.InAirCorrLevel);
                GameModifierMods.SetFakieBalanceLevel(data.FakieBalanceLevel);
                GameModifierMods.SetPumpStrengthLevel(data.PumpStrengthLevel);
                GameModifierMods.SetIcePhysicsLevel(data.IcePhysicsLevel);
                FOV.SetLevel(data.FovLevel);
                Gravity.SetLevel(data.GravityLevel);
                WideTyres.SetLevel(data.WideTyresLevel);

                // Toggles — only enable if saved true, don't double-toggle
                if (data.WideTyresEnabled && !WideTyres.Enabled) WideTyres.Toggle();
                if (data.StickyTyresEnabled && !StickyTyres.Enabled) StickyTyres.Toggle();
                if (data.SlowMotionEnabled && !SlowMotion.Enabled) SlowMotion.Toggle();
                if (data.CutBrakesEnabled && !CutBrakes.Enabled) CutBrakes.Toggle();
                if (data.NoSpeedCapEnabled && !NoSpeedCap.Enabled) NoSpeedCap.Toggle();
                if (data.ReverseSteerEnabled && !ReverseSteering.Enabled) ReverseSteering.Toggle();
                if (data.IceModeEnabled && !IceMode.Enabled) IceMode.Toggle();
                if (data.MirrorModeEnabled && !MirrorMode.Enabled) MirrorMode.Toggle();
                if (data.DrunkModeEnabled && !DrunkMode.Enabled) DrunkMode.Toggle();
                if (data.FlyModeEnabled && !FlyMode.Enabled) FlyMode.Toggle();
                if (data.SpeedrunTimerEnabled && !SpeedrunTimer.Enabled) SpeedrunTimer.Toggle();
                if (data.SlowMoOnBailEnabled && !SlowMoOnBail.Enabled) SlowMoOnBail.Toggle();

                WheelieAngleLimit.SetLevel(data.WheelieAngleLimitLevel);
                AirControl.SetLevel(data.AirControlLevel);
                if (data.WheelieAngleLimitEnabled && !WheelieAngleLimit.Enabled) WheelieAngleLimit.Toggle();
                if (data.AirControlEnabled && !AirControl.Enabled) AirControl.Toggle();

                if (data.AccelerationEnabled && !Acceleration.Enabled) Acceleration.Toggle();
                if (data.MaxSpeedEnabled && !MaxSpeedMultiplier.Enabled) MaxSpeedMultiplier.Toggle();
                if (data.LandingImpactEnabled && !LandingImpact.Enabled) LandingImpact.Toggle();
                if (data.FovEnabled && !FOV.Enabled) FOV.Toggle();
                AutoBalance.SetStrengthLevel(data.AutoBalanceStrengthLevel);
                if (data.AutoBalanceEnabled && !AutoBalance.Enabled) AutoBalance.Toggle();
                if (data.NoSpeedWobblesEnabled && !GameModifierMods.NoSpeedWobblesEnabled) GameModifierMods.NoSpeedWobblesToggle();

                if (data.SpinEnabled && !Movement.SpinEnabled) Movement.ToggleSpin();
                if (data.HopEnabled && !Movement.HopEnabled) Movement.ToggleHop();
                if (data.WheelieEnabled && !Movement.WheelieEnabled) Movement.ToggleWheelie();
                if (data.LeanEnabled && !Movement.LeanEnabled) Movement.ToggleLean();

                QuickBrake.SetLevel(data.QuickBrakeLevel);
                if (data.QuickBrakeEnabled && !QuickBrake.Enabled) QuickBrake.Toggle();

                // Menu Customiser
                MenuCustomiser.PositionPreset = data.MenuPositionPreset;
                MenuCustomiser.ScaleLevel = data.MenuScaleLevel;
                MenuCustomiser.OpacityLevel = data.MenuOpacityLevel;
                MenuCustomiser.Apply();

                // Bike Torch
                BikeTorch.IntensityIndex = data.BikeTorchIntensityIndex;
                if (data.BikeTorchEnabled && !BikeTorch.Enabled) BikeTorch.Toggle();

                // Camera Shake
                CameraShake.SetLevel(data.CameraShakeLevel);
                if (data.CameraShakeEnabled && !CameraShake.Enabled) CameraShake.Toggle();

                // Center of Mass
                CenterOfMass.SetLR(data.CenterOfMassLR);
                CenterOfMass.SetFB(data.CenterOfMassFB);
                CenterOfMass.SetUD(data.CenterOfMassUD);

                // Exploding Props
                if (data.ExplodingPropsEnabled && !ExplodingProps.Enabled) ExplodingProps.Toggle();

                // Near Miss
                NearMissSensitivity.SetLevel(data.NearMissLevel);
                if (data.NearMissEnabled && !NearMissSensitivity.Enabled) NearMissSensitivity.Toggle();

                // Bike / Player Scale (deferred — may not have player yet)
                BikeSize.CurrentScale = data.BikeScale;
                PlayerSize.CurrentScale = data.PlayerScale;
                // These will be applied by the scene reapply system when Player_Human exists
                // Direct apply attempted here in case player already exists:
                if (data.BikeSizeLevel != 10) try { BikeSize.ApplyLevel(data.BikeSizeLevel); } catch { }
                else if (data.BikeScale != 1f) try { BikeSize.Apply(data.BikeScale); } catch { } // legacy
                if (data.PlayerSizeLevel != 10) try { PlayerSize.ApplyLevel(data.PlayerSizeLevel); } catch { }
                else if (data.PlayerScale != 1f) try { PlayerSize.Apply(data.PlayerScale); } catch { } // legacy
                if (data.InvisibleBikeEnabled) try { InvisibleBike.SetEnabled(true); } catch { }
                if (data.InvisiblePlayerEnabled) try { InvisiblePlayer.SetEnabled(true); } catch { }
                if (data.IndividualWheelMode)
                {
                    try { WheelSize.ApplyIndividualFromSave(data.FrontWheelSizeLevel, data.RearWheelSizeLevel); } catch { }
                }
                else if (data.WheelSizeEnabled)
                {
                    try { WheelSize.ApplyFromSave(true, data.WheelSizeLevel, data.WheelSizeMode); } catch { }
                }

                // Suspension HUD
                if (data.SuspensionHUDEnabled && !SuspensionHUD.Enabled) SuspensionHUD.Toggle();

                // Brake Fade
                BrakeFade.SetBalanceLevel(data.BrakeBalanceLevel);
                if (data.BrakeFadeEnabled && !BrakeFade.Enabled) BrakeFade.Toggle();

                // Tyre Pressure
                TyrePressure.SetLevel(data.TyrePressureLevel);
                if (data.TyrePressureEnabled && !TyrePressure.Enabled) TyrePressure.Toggle();

                // Instant Respawn
                if (data.InstantRespawnEnabled && !InstantRespawn.Enabled) InstantRespawn.Toggle();

                // Bike Damage
                if (data.BikeDamageEnabled && !BikeDamage.Enabled) BikeDamage.Toggle();
                if (data.HeadlightsOnlyEnabled && !HeadlightsOnly.Enabled) HeadlightsOnly.Toggle();
                if (data.UIRemoverEnabled && !UIRemover.Enabled) UIRemover.Toggle();

                // Wheelie HUD
                if (data.WheelieHUDEnabled && !WheelieHUD.Enabled) WheelieHUD.Toggle();

                // Trick Set Swap (set source by name BEFORE toggling so the right gestures load)
                if (!string.IsNullOrEmpty(data.TrickSetSwapSourceName))
                    TrickSetSwap.SetSourceByName(data.TrickSetSwapSourceName);
                if (data.TrickSetSwapEnabled && !TrickSetSwap.Enabled) TrickSetSwap.Toggle();

                MelonLogger.Msg("[StatsManager] Loaded from: " + SaveFile);
            }
            catch (Exception ex) { MelonLogger.Error("[StatsManager] LoadStats: " + ex.Message); }

            // Refresh all pages
            try { MovePage.RefreshAll(); } catch { }
            try { WorldPage.RefreshAll(); } catch { }
            try { BikePage.RefreshAll(); } catch { }
            try { FunPage.RefreshAll(); } catch { }
            try { GraphicsPage.RefreshAll(); } catch { }
            try { ModesPage.RefreshAll(); } catch { }
            try { SessionPage.RefreshAll(); } catch { }
            try { InfoPage.Refresh(); } catch { }
            try { GhostPage.RefreshAll(); } catch { }
        }

        // ══════════════════════════════════════════════════════════════
        //  RESET — resets EVERYTHING to defaults (including graphics,
        //  sky, modes, ghost, ESP — those aren't saved but still reset)
        // ══════════════════════════════════════════════════════════════
        public static void ResetStats()
        {
            try
            {
                WorldPage.GlobalReset();
                BikePage.GlobalReset();
                FunPage.GlobalReset();

                Acceleration.SetLevel(1);
                MaxSpeedMultiplier.SetLevel(1);
                LandingImpact.SetLevel(1);
                NoBail.SetEnabled(false);
                BikeSwitcher.SetBike(0);

                Movement.SetSpinLevel(1);
                Movement.SetHopLevel(1);
                Movement.SetWheelieLevel(1);
                Movement.SetLeanLevel(1);

                Suspension.SetTravelLevel(5);
                Suspension.SetStiffnessLevel(5);
                Suspension.SetDampingLevel(5);

                GameModifierMods.SetWheelieBalanceLevel(5);
                GameModifierMods.SetInAirCorrLevel(5);
                GameModifierMods.SetFakieBalanceLevel(5);
                GameModifierMods.SetPumpStrengthLevel(5);
                GameModifierMods.SetIcePhysicsLevel(5);

                FOV.SetLevel(5);
                Gravity.SetLevel(5);
                TimeOfDay.ResetToSceneDefault();
                SkyColours.RestoreDefault();
                WideTyres.SetLevel(5);

                FlyMode.MoveSpeed = 30f;
                FlyMode.ClimbSpeed = 20f;
                GhostReplay.GhostAlpha = 0.45f;
                StickyTyres.SuctionForce = 150f;
                SlowMotion.SetLevel(5);

                if (WideTyres.Enabled) WideTyres.Toggle();
                if (StickyTyres.Enabled) StickyTyres.Toggle();
                if (SlowMotion.Enabled) SlowMotion.Toggle();
                if (CutBrakes.Enabled) CutBrakes.Toggle();
                if (NoSpeedCap.Enabled) NoSpeedCap.Toggle();
                if (ReverseSteering.Enabled) ReverseSteering.Toggle();
                if (IceMode.Enabled) IceMode.Toggle();
                if (MirrorMode.Enabled) MirrorMode.Toggle();
                if (DrunkMode.Enabled) DrunkMode.Toggle();
                if (FlyMode.Enabled) FlyMode.Toggle();
                if (ESP.Enabled) ESP.Toggle();
                if (SpeedrunTimer.Enabled) SpeedrunTimer.Toggle();
                if (SlowMoOnBail.Enabled) SlowMoOnBail.Toggle();
                if (GhostReplay.Enabled) GhostReplay.Toggle();

                if (Acceleration.Enabled) Acceleration.Toggle();
                if (MaxSpeedMultiplier.Enabled) MaxSpeedMultiplier.Toggle();
                if (LandingImpact.Enabled) LandingImpact.Toggle();
                if (FOV.Enabled) FOV.Toggle();
                if (AutoBalance.Enabled) AutoBalance.Toggle();
                AutoBalance.SetStrengthLevel(5);
                if (GameModifierMods.NoSpeedWobblesEnabled) GameModifierMods.NoSpeedWobblesToggle();

                if (Movement.SpinEnabled) Movement.ToggleSpin();
                if (Movement.HopEnabled) Movement.ToggleHop();
                if (Movement.WheelieEnabled) Movement.ToggleWheelie();
                if (Movement.LeanEnabled) Movement.ToggleLean();

                if (QuickBrake.Enabled) QuickBrake.Toggle();
                QuickBrake.SetLevel(5);

                if (BikeTorch.Enabled) BikeTorch.Toggle();
                BikeTorch.IntensityIndex = 2;

                if (CameraShake.Enabled) CameraShake.Toggle();
                CameraShake.SetLevel(5);

                CenterOfMass.SetLR(0f);
                CenterOfMass.SetFB(0f);
                CenterOfMass.SetUD(0f);

                if (ExplodingProps.Enabled) ExplodingProps.Toggle();

                if (NearMissSensitivity.Enabled) NearMissSensitivity.Toggle();
                NearMissSensitivity.SetLevel(5);

                // Bike / Player Scale
                BikeSize.CurrentScale = 1f;
                PlayerSize.CurrentScale = 1f;
                try { BikeSize.ApplyLevel(10); } catch { }
                try { PlayerSize.ApplyLevel(10); } catch { }
                if (InvisibleBike.Enabled) try { InvisibleBike.SetEnabled(false); } catch { }
                if (InvisiblePlayer.Enabled) try { InvisiblePlayer.SetEnabled(false); } catch { }
                try { WheelSize.Reset(); } catch { }

                // Graphics — always reset to defaults (not saved)
                if (!GraphicsSettings.BloomEnabled) GraphicsSettings.ToggleBloom();
                if (!GraphicsSettings.AmbientOccEnabled) GraphicsSettings.ToggleAO();
                if (!GraphicsSettings.VignetteEnabled) GraphicsSettings.ToggleVignette();
                if (GraphicsSettings.DepthOfFieldEnabled) GraphicsSettings.ToggleDOF();
                if (!GraphicsSettings.ChromaticAbEnabled) GraphicsSettings.ToggleChromatic();

                if (WheelieAngleLimit.Enabled) WheelieAngleLimit.Toggle();
                WheelieAngleLimit.SetLevel(5);

                if (AirControl.Enabled) AirControl.Toggle();
                AirControl.SetLevel(5);

                // Sky — always reset (not saved)
                if (SkyColours.StormEnabled) SkyColours.ToggleStorm();
                SkyColours.SetRainIntensityLevel(5);

                SessionHUD.Enabled = false;
                if (SuspensionHUD.Enabled) SuspensionHUD.Toggle();
                if (BrakeFade.Enabled) BrakeFade.Toggle();
                BrakeFade.SetBalanceLevel(6);
                if (TyrePressure.Enabled) TyrePressure.Toggle();
                TyrePressure.SetLevel(5);
                if (InstantRespawn.Enabled) InstantRespawn.Toggle();
                if (BikeDamage.Enabled) BikeDamage.Toggle();
                if (HeadlightsOnly.Enabled) HeadlightsOnly.Toggle();
                if (UIRemover.Enabled) UIRemover.Toggle();
                if (WheelieHUD.Enabled) WheelieHUD.Toggle();
                if (TrickSetSwap.Enabled) TrickSetSwap.Disable();

                // Modes — always reset (not saved)
                if (AvalancheMode.Enabled) AvalancheMode.Reset();
                if (EarthquakeMode.Enabled) EarthquakeMode.Reset();
                if (PoliceChaseMode.Enabled) PoliceChaseMode.Reset();
                if (TrickAttackMode.CurrentState != TrickAttackMode.State.Off) TrickAttackMode.Reset();
                if (BoulderDodgeMode.Enabled) BoulderDodgeMode.Reset();
                if (SurvivalMode.Enabled) SurvivalMode.Reset();

                MelonLogger.Msg("[StatsManager] Reset to defaults.");
            }
            catch (Exception ex) { MelonLogger.Error("[StatsManager] ResetStats: " + ex.Message); }

            try { MovePage.RefreshAll(); } catch { }
            try { WorldPage.RefreshAll(); } catch { }
            try { BikePage.RefreshAll(); } catch { }
            try { FunPage.RefreshAll(); } catch { }
            try { GraphicsPage.RefreshAll(); } catch { }
            try { ModesPage.RefreshAll(); } catch { }
            try { SessionPage.RefreshAll(); } catch { }
            try { InfoPage.Refresh(); } catch { }
            try { GhostPage.RefreshAll(); } catch { }
        }

        private static void EnsureSaveFolder()
        {
            if (!Directory.Exists(SaveFolder))
                Directory.CreateDirectory(SaveFolder);
        }
    }
}