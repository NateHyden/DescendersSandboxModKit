using System;
using System.IO;
using MelonLoader;
using DescendersModMenu;
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
        // ══════════════════════════════════════════════════════════════
        public static void SaveStats()
        {
            try
            {
                EnsureSaveFolder();

                var data = new BikeStatsData
                {
                    AccelerationLevel = Acceleration.Level,
                    MaxSpeedLevel = MaxSpeedMultiplier.Level,
                    LandingImpactLevel = LandingImpact.Level,
                    NoBailEnabled = NoBail.Enabled,
                    BikeIndex = BikeSwitcher.CurrentBikeIndex,

                    SpinLevel = Movement.SpinLevel,
                    HopLevel = Movement.HopLevel,
                    WheelieLevel = Movement.WheelieLevel,
                    LeanLevel = Movement.LeanLevel,

                    SuspTravelLevel = Suspension.TravelLevel,
                    SuspStiffnessLevel = Suspension.StiffnessLevel,
                    SuspDampingLevel = Suspension.DampingLevel,

                    WheelieBalanceLevel = GameModifierMods.WheelieBalanceLevel,
                    InAirCorrLevel = GameModifierMods.InAirCorrLevel,
                    FakieBalanceLevel = GameModifierMods.FakieBalanceLevel,
                    PumpStrengthLevel = GameModifierMods.PumpStrengthLevel,
                    TweakSpeedLevel = GameModifierMods.TweakSpeedLevel,
                    IcePhysicsLevel = GameModifierMods.IcePhysicsLevel,

                    FovLevel = FOV.Level,
                    GravityLevel = Gravity.Level,

                    WideTyresEnabled = WideTyres.Enabled,
                    WideTyresLevel = WideTyres.Level,
                    StickyTyresEnabled = StickyTyres.Enabled,
                    StickyForce = StickyTyres.SuctionForce,
                    SpiderBikeEnabled = SpiderBike.Enabled,

                    SlowMotionEnabled = SlowMotion.Enabled,
                    SlowMotionLevel = SlowMotion.Level,
                    CutBrakesEnabled = CutBrakes.Enabled,
                    NoSpeedCapEnabled = NoSpeedCap.Enabled,
                    ReverseSteerEnabled = ReverseSteering.Enabled,
                    IceModeEnabled = IceMode.Enabled,
                    MirrorModeEnabled = MirrorMode.Enabled,
                    DrunkModeEnabled = DrunkMode.Enabled,
                    FlyModeEnabled = FlyMode.Enabled,
                    HoverModeEnabled = HoverMode.Enabled,
                    HoverModeHeight = HoverMode.HoverHeight,
                    SpeedrunTimerEnabled = SpeedrunTimer.Enabled,
                    SessionHUDEnabled = SessionHUD.Enabled,
                    TrickMultiplierLevel = TrickMultiplier.Level,
                    SlowMoOnBailEnabled = SlowMoOnBail.Enabled,
                    BlackDeathEnabled = BlackDeath.Enabled,

                    WheelieAngleLimitEnabled = WheelieAngleLimit.Enabled,
                    WheelieAngleLimitLevel = WheelieAngleLimit.Level,
                    AirControlEnabled = AirControl.Enabled,
                    AirControlLevel = AirControl.Level,

                    AccelerationEnabled = Acceleration.Enabled,
                    MaxSpeedEnabled = MaxSpeedMultiplier.Enabled,
                    LandingImpactEnabled = LandingImpact.Enabled,
                    FovEnabled = FOV.Enabled,
                    AutoBalanceEnabled = AutoBalance.Enabled,
                    AutoBalanceStrengthLevel = AutoBalance.StrengthLevel,
                    BouncyBikeEnabled = BouncyBike.Enabled,
                    BouncyBikeLevel = BouncyBike.BouncinessLevel,
                    NoSpeedWobblesEnabled = GameModifierMods.NoSpeedWobblesEnabled,

                    SpinEnabled = Movement.SpinEnabled,
                    HopEnabled = Movement.HopEnabled,
                    WheelieEnabled = Movement.WheelieEnabled,
                    LeanEnabled = Movement.LeanEnabled,

                    QuickBrakeEnabled = QuickBrake.Enabled,
                    QuickBrakeLevel = QuickBrake.Level,

                    FlyMoveSpeed = FlyMode.MoveSpeed,
                    FlyClimbSpeed = FlyMode.ClimbSpeed,

                    MenuPositionPreset = MenuCustomiser.PositionPreset,
                    MenuScaleLevel = MenuCustomiser.ScaleLevel,
                    MenuOpacityLevel = MenuCustomiser.OpacityLevel,

                    BikeTorchEnabled = BikeTorch.Enabled,
                    BikeTorchIntensityIndex = BikeTorch.IntensityIndex,

                    CameraShakeEnabled = CameraShake.Enabled,
                    CameraShakeLevel = CameraShake.Level,

                    CenterOfMassLR = CenterOfMass.OffsetLR,
                    CenterOfMassFB = CenterOfMass.OffsetFB,
                    CenterOfMassUD = CenterOfMass.OffsetUD,

                    ExplodingPropsEnabled = ExplodingProps.Enabled,

                    NearMissEnabled = NearMissSensitivity.Enabled,
                    NearMissLevel = NearMissSensitivity.Level,

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

                    SuspensionHUDEnabled = SuspensionHUD.Enabled,

                    BrakeFadeEnabled = BrakeFade.Enabled,
                    BrakeBalanceLevel = BrakeFade.BalanceLevel,

                    TyrePressureEnabled = TyrePressure.Enabled,
                    TyrePressureLevel = TyrePressure.Level,

                    InstantRespawnEnabled = InstantRespawn.Enabled,

                    BikeDamageEnabled = BikeDamage.Enabled,
                    HeadlightsOnlyEnabled = HeadlightsOnly.Enabled,
                    UIRemoverEnabled = UIRemover.Enabled,

                    WheelieHUDEnabled = WheelieHUD.Enabled,

                    TrickSetSwapEnabled = TrickSetSwap.Enabled,
                    TrickSetSwapSourceName = TrickSetSwap.CurrentSourceName,

                    LavaDifficultyLevel = LavaRising.DifficultyLevel,
                    LavaHeightRecords = LavaRising.ExportRecords(),

                    CompassAlwaysOnEnabled = CompassAlwaysOn.Enabled,
                    SpectateModeEnabled = SpectateMode.Enabled,
                    ScreenshotModeEnabled = ScreenshotMode.Enabled,
                    RubberBandSteeringEnabled = RubberBandSteering.Enabled,
                    RubberBandSteeringLevel = RubberBandSteering.Level,
                    PedalWhileTweakEnabled = PedalWhileTweak.Enabled,
                };

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFile, json);
                ModLog.Debug("[StatsManager] Saved to: " + SaveFile);
            }
            catch (Exception ex) { MelonLogger.Error("[StatsManager] SaveStats: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "StatsManager"); }
        }

        // ══════════════════════════════════════════════════════════════
        // ══════════════════════════════════════════════════════════════
        public static void LoadStats()
        {
            try { ResetStats(); }
            catch (System.Exception ex) { ModLog.Warn("[StatsManager] Pre-load reset: " + ex.Message); }

            try
            {
                if (!File.Exists(SaveFile))
                { ModLog.Warn("[StatsManager] No save file found: " + SaveFile); return; }

                string json = File.ReadAllText(SaveFile);
                BikeStatsData data = JsonUtility.FromJson<BikeStatsData>(json);
                if (data == null)
                { ModLog.Warn("[StatsManager] JSON returned null."); return; }

                Acceleration.SetLevel(data.AccelerationLevel);
                MaxSpeedMultiplier.SetLevel(data.MaxSpeedLevel);
                LandingImpact.SetLevel(data.LandingImpactLevel);
                NoBail.SetEnabled(data.NoBailEnabled);
                BikeSwitcher.SetBike(data.BikeIndex);

                FlyMode.MoveSpeed = data.FlyMoveSpeed;
                FlyMode.ClimbSpeed = data.FlyClimbSpeed;
                StickyTyres.SuctionForce = data.StickyForce;
                SlowMotion.SetLevel(data.SlowMotionLevel);

                Movement.SetSpinLevel(data.SpinLevel);
                Movement.SetHopLevel(data.HopLevel);
                Movement.SetWheelieLevel(data.WheelieLevel);
                Movement.SetLeanLevel(data.LeanLevel);
                Suspension.SetTravelLevel(data.SuspTravelLevel);
                Suspension.SetStiffnessLevel(data.SuspStiffnessLevel);
                Suspension.SetDampingLevel(data.SuspDampingLevel);
                GameModifierMods.SetWheelieBalanceLevel(data.WheelieBalanceLevel < 2 ? 5 : data.WheelieBalanceLevel);
                GameModifierMods.SetInAirCorrLevel(data.InAirCorrLevel < 2 ? 5 : data.InAirCorrLevel);
                GameModifierMods.SetFakieBalanceLevel(data.FakieBalanceLevel < 2 ? 5 : data.FakieBalanceLevel);
                GameModifierMods.SetPumpStrengthLevel(data.PumpStrengthLevel < 2 ? 5 : data.PumpStrengthLevel);
                GameModifierMods.SetTweakSpeedLevel(data.TweakSpeedLevel < 2 ? 5 : data.TweakSpeedLevel);
                GameModifierMods.SetIcePhysicsLevel(data.IcePhysicsLevel < 2 ? 5 : data.IcePhysicsLevel);
                FOV.SetLevel(data.FovLevel);
                Gravity.SetLevel(data.GravityLevel);
                WideTyres.SetLevel(data.WideTyresLevel);
                if (data.WideTyresLevel != 5 && data.WideTyresLevel >= 1)
                    WideTyres.Apply();
                if (data.StickyTyresEnabled && !StickyTyres.Enabled) StickyTyres.Toggle();
                if (data.SpiderBikeEnabled && !SpiderBike.Enabled) SpiderBike.Toggle();
                if (data.SlowMotionEnabled && !SlowMotion.Enabled) SlowMotion.Toggle();
                if (data.CutBrakesEnabled && !CutBrakes.Enabled) CutBrakes.Toggle();
                if (data.NoSpeedCapEnabled && !NoSpeedCap.Enabled) NoSpeedCap.Toggle();
                if (data.ReverseSteerEnabled && !ReverseSteering.Enabled) ReverseSteering.Toggle();
                if (data.IceModeEnabled && !IceMode.Enabled) IceMode.Toggle();
                if (data.MirrorModeEnabled && !MirrorMode.Enabled) MirrorMode.Toggle();
                if (data.DrunkModeEnabled && !DrunkMode.Enabled) DrunkMode.Toggle();
                if (data.FlyModeEnabled && !FlyMode.Enabled) FlyMode.Toggle();
                HoverMode.SetHeight(data.HoverModeHeight);
                if (data.HoverModeEnabled && !HoverMode.Enabled) HoverMode.Toggle();
                if (data.SpeedrunTimerEnabled && !SpeedrunTimer.Enabled) SpeedrunTimer.Toggle();
                if (data.SessionHUDEnabled && !SessionHUD.Enabled) SessionHUD.Toggle();
                TrickMultiplier.SetLevel(data.TrickMultiplierLevel);
                if (data.SlowMoOnBailEnabled && !SlowMoOnBail.Enabled) SlowMoOnBail.Toggle();
                if (data.BlackDeathEnabled && !BlackDeath.Enabled) BlackDeath.Toggle();

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
                BouncyBike.SetLevel(data.BouncyBikeLevel);
                if (data.BouncyBikeEnabled && !BouncyBike.Enabled) BouncyBike.Toggle();
                if (data.NoSpeedWobblesEnabled && !GameModifierMods.NoSpeedWobblesEnabled) GameModifierMods.NoSpeedWobblesToggle();

                if (data.SpinEnabled && !Movement.SpinEnabled) Movement.ToggleSpin();
                if (data.HopEnabled && !Movement.HopEnabled) Movement.ToggleHop();
                if (data.WheelieEnabled && !Movement.WheelieEnabled) Movement.ToggleWheelie();
                if (data.LeanEnabled && !Movement.LeanEnabled) Movement.ToggleLean();

                QuickBrake.SetLevelFromSave(data.QuickBrakeLevel);
                if (data.QuickBrakeEnabled && !QuickBrake.Enabled) QuickBrake.Toggle();

                MenuCustomiser.PositionPreset = data.MenuPositionPreset;
                MenuCustomiser.ScaleLevel = data.MenuScaleLevel;
                MenuCustomiser.OpacityLevel = data.MenuOpacityLevel;
                MenuCustomiser.Apply();

                BikeTorch.IntensityIndex = data.BikeTorchIntensityIndex;
                if (data.BikeTorchEnabled && !BikeTorch.Enabled) BikeTorch.Toggle();

                CameraShake.SetLevel(data.CameraShakeLevel);
                if (data.CameraShakeEnabled && !CameraShake.Enabled) CameraShake.Toggle();

                CenterOfMass.SetLR(data.CenterOfMassLR);
                CenterOfMass.SetFB(data.CenterOfMassFB);
                CenterOfMass.SetUD(data.CenterOfMassUD);

                if (data.ExplodingPropsEnabled && !ExplodingProps.Enabled) ExplodingProps.Toggle();

                NearMissSensitivity.SetLevel(data.NearMissLevel);
                if (data.NearMissEnabled && !NearMissSensitivity.Enabled) NearMissSensitivity.Toggle();

                BikeSize.CurrentScale = data.BikeScale;
                PlayerSize.CurrentScale = data.PlayerScale;
                if (data.BikeSizeLevel != 10) try { BikeSize.ApplyLevel(data.BikeSizeLevel); } catch (Exception ex) { ModLog.Warn("[StatsManager] LoadStats BikeSize: " + ex.Message); }
                else if (data.BikeScale != 1f) try { BikeSize.Apply(data.BikeScale); } catch (Exception ex) { ModLog.Warn("[StatsManager] LoadStats BikeScale(legacy): " + ex.Message); }
                if (data.PlayerSizeLevel != 10) try { PlayerSize.ApplyLevel(data.PlayerSizeLevel); } catch (Exception ex) { ModLog.Warn("[StatsManager] LoadStats PlayerSize: " + ex.Message); }
                else if (data.PlayerScale != 1f) try { PlayerSize.Apply(data.PlayerScale); } catch (Exception ex) { ModLog.Warn("[StatsManager] LoadStats PlayerScale(legacy): " + ex.Message); }
                if (data.InvisibleBikeEnabled) try { InvisibleBike.SetEnabled(true); } catch (Exception ex) { ModLog.Warn("[StatsManager] LoadStats InvisibleBike: " + ex.Message); }
                if (data.InvisiblePlayerEnabled) try { InvisiblePlayer.SetEnabled(true); } catch (Exception ex) { ModLog.Warn("[StatsManager] LoadStats InvisiblePlayer: " + ex.Message); }
                if (data.IndividualWheelMode)
                {
                    try { WheelSize.ApplyIndividualFromSave(data.FrontWheelSizeLevel, data.RearWheelSizeLevel); } catch (Exception ex) { ModLog.Warn("[StatsManager] LoadStats IndividualWheel: " + ex.Message); }
                }
                else if (data.WheelSizeLevel != 10 || data.WheelSizeMode != 0)
                {
                    try { WheelSize.ApplyFromSave(true, data.WheelSizeLevel, data.WheelSizeMode); } catch (Exception ex) { ModLog.Warn("[StatsManager] LoadStats WheelSize: " + ex.Message); }
                }

                if (data.SuspensionHUDEnabled && !SuspensionHUD.Enabled) SuspensionHUD.Toggle();

                BrakeFade.SetBalanceLevel(data.BrakeBalanceLevel);
                if (data.BrakeFadeEnabled && !BrakeFade.Enabled) BrakeFade.Toggle();

                TyrePressure.SetLevel(data.TyrePressureLevel);
                if (data.TyrePressureEnabled && !TyrePressure.Enabled) TyrePressure.Toggle();

                if (data.InstantRespawnEnabled && !InstantRespawn.Enabled) InstantRespawn.Toggle();

                if (data.BikeDamageEnabled && !BikeDamage.Enabled) BikeDamage.Toggle();
                if (data.HeadlightsOnlyEnabled && !HeadlightsOnly.Enabled) HeadlightsOnly.Toggle();
                if (data.UIRemoverEnabled && !UIRemover.Enabled) UIRemover.Toggle();

                if (data.WheelieHUDEnabled && !WheelieHUD.Enabled) WheelieHUD.Toggle();

                if (!string.IsNullOrEmpty(data.TrickSetSwapSourceName))
                    TrickSetSwap.SetSourceByName(data.TrickSetSwapSourceName);
                if (data.TrickSetSwapEnabled && !TrickSetSwap.Enabled) TrickSetSwap.Toggle();

                int lavaLv = data.LavaDifficultyLevel;
                if (lavaLv < 1 || lavaLv > 4) lavaLv = 2;
                LavaRising.SetDifficulty(lavaLv);
                if (!string.IsNullOrEmpty(data.LavaHeightRecords))
                    LavaRising.ImportRecords(data.LavaHeightRecords);

                if (data.CompassAlwaysOnEnabled && !CompassAlwaysOn.Enabled) CompassAlwaysOn.Toggle();
                if (data.SpectateModeEnabled && !SpectateMode.Enabled) SpectateMode.Toggle();
                if (data.ScreenshotModeEnabled && !ScreenshotMode.Enabled) ScreenshotMode.Toggle();
                int rbLevel = data.RubberBandSteeringLevel;
                if (rbLevel < 1 || rbLevel > 10) rbLevel = 5;
                RubberBandSteering.SetLevel(rbLevel);
                if (data.RubberBandSteeringEnabled && !RubberBandSteering.Enabled) RubberBandSteering.Toggle();
                if (data.PedalWhileTweakEnabled && !PedalWhileTweak.Enabled) PedalWhileTweak.Toggle();

                ModLog.Debug("[StatsManager] Loaded from: " + SaveFile);
            }
            catch (Exception ex) { MelonLogger.Error("[StatsManager] LoadStats: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "StatsManager"); }

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
        // ══════════════════════════════════════════════════════════════
        public static void ResetStats()
        {
            try
            {
                WorldPage.GlobalReset();
                BikePage.GlobalReset();
                FunPage.GlobalReset();
                OtherPage.GlobalReset();
                TrickMultiplier.Reset();
                if (CompassAlwaysOn.Enabled) CompassAlwaysOn.Toggle();
                if (SpectateMode.Enabled) SpectateMode.Toggle();
                if (ScreenshotMode.Enabled) ScreenshotMode.Toggle();
                if (RubberBandSteering.Enabled) RubberBandSteering.Toggle();
                RubberBandSteering.SetLevel(5);
                if (PedalWhileTweak.Enabled) PedalWhileTweak.Toggle();

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
                GameModifierMods.SetTweakSpeedLevel(5);
                GameModifierMods.SetIcePhysicsLevel(5);

                FOV.SetLevel(5);
                Gravity.SetLevel(5);
                TimeOfDay.ResetToSceneDefault();
                SkyColours.RestoreDefault();
                WideTyres.Reset();
                if (StickyTyres.Enabled) StickyTyres.Toggle();
                if (SpiderBike.Enabled) SpiderBike.Toggle();
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
                if (BlackDeath.Enabled) BlackDeath.Toggle();
                if (GhostReplay.Enabled) GhostReplay.Toggle();

                if (Acceleration.Enabled) Acceleration.Toggle();
                if (MaxSpeedMultiplier.Enabled) MaxSpeedMultiplier.Toggle();
                if (LandingImpact.Enabled) LandingImpact.Toggle();
                if (FOV.Enabled) FOV.Toggle();
                if (AutoBalance.Enabled) AutoBalance.Toggle();
                AutoBalance.SetStrengthLevel(5);
                BouncyBike.Reset();
                if (GameModifierMods.NoSpeedWobblesEnabled) GameModifierMods.NoSpeedWobblesToggle();

                if (Movement.SpinEnabled) Movement.ToggleSpin();
                if (Movement.HopEnabled) Movement.ToggleHop();
                if (Movement.WheelieEnabled) Movement.ToggleWheelie();
                if (Movement.LeanEnabled) Movement.ToggleLean();

                if (QuickBrake.Enabled) QuickBrake.Toggle();
                QuickBrake.SetLevel(1);

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

                BikeSize.CurrentScale = 1f;
                PlayerSize.CurrentScale = 1f;
                try { BikeSize.ApplyLevel(10); } catch { }
                try { PlayerSize.ApplyLevel(10); } catch { }
                if (InvisibleBike.Enabled) try { InvisibleBike.SetEnabled(false); } catch { }
                if (InvisiblePlayer.Enabled) try { InvisiblePlayer.SetEnabled(false); } catch { }
                try { WheelSize.Reset(); } catch { }

                if (!GraphicsSettings.BloomEnabled) GraphicsSettings.ToggleBloom();
                if (!GraphicsSettings.AmbientOccEnabled) GraphicsSettings.ToggleAO();
                if (!GraphicsSettings.VignetteEnabled) GraphicsSettings.ToggleVignette();
                if (GraphicsSettings.DepthOfFieldEnabled) GraphicsSettings.ToggleDOF();
                if (!GraphicsSettings.ChromaticAbEnabled) GraphicsSettings.ToggleChromatic();

                if (WheelieAngleLimit.Enabled) WheelieAngleLimit.Toggle();
                WheelieAngleLimit.SetLevel(5);

                if (AirControl.Enabled) AirControl.Toggle();
                AirControl.SetLevel(5);

                if (SkyColours.StormEnabled) SkyColours.ToggleStorm();
                SkyColours.SetRainIntensityLevel(5);
                DiscoMode.Reset();

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

                if (AvalancheMode.Enabled) AvalancheMode.Reset();
                if (EarthquakeMode.Enabled) EarthquakeMode.Reset();
                if (PoliceChaseMode.Enabled) PoliceChaseMode.Reset();
                if (TrickAttackMode.CurrentState != TrickAttackMode.State.Off) TrickAttackMode.Reset();
                if (BoulderDodgeMode.Enabled) BoulderDodgeMode.Reset();
                if (SurvivalMode.Enabled) SurvivalMode.Reset();
                LavaRising.Reset();
                LavaRising.SetDifficulty(2);
                LavaRising.ClearRecords();
                PersistClearedLavaRecords();

                ModLog.Debug("[StatsManager] Reset to defaults.");
            }
            catch (Exception ex) { MelonLogger.Error("[StatsManager] ResetStats: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "StatsManager"); }

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

        private static void PersistClearedLavaRecords()
        {
            try
            {
                if (!File.Exists(SaveFile)) return;
                string json = File.ReadAllText(SaveFile);
                BikeStatsData data = JsonUtility.FromJson<BikeStatsData>(json);
                if (data == null) return;
                data.LavaDifficultyLevel = 2;
                data.LavaHeightRecords = "";
                File.WriteAllText(SaveFile, JsonUtility.ToJson(data, true));
            }
            catch (Exception ex) { ModLog.Warn("[StatsManager] PersistClearedLavaRecords: " + ex.Message); }
        }
    }
}

