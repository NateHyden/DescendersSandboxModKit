using DescendersModMenu.Mods;
using DescendersModMenu.BikeStats;
using DescendersModMenu.UI;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu
{
    public static class BuildInfo
    {
        public const string Name = "Descenders Sandbox";
        public const string Description = "An advanced sandbox experience for Descenders";
        public const string Author = "NateHyden";
        public const string Company = null;
        public const string Version = "1.9.0";
        public const string DownloadLink = null;
    }

    public class DescendersModMenu : MelonMod
    {
        private HarmonyLib.Harmony harmony;
        private float _lastRStickClick = -999f;
        private bool _pendingRStickSave = false;
        private float _rStickSaveTime = 0f;

        private bool _pendingReapply;
        private bool _pendingAutoLoad = true;
        private bool _pendingTelemetryPing = true;
        private bool _pendingModifierDump = true;
        private bool _reapplyFlyMode, _reapplyDrunkMode, _reapplyMirrorMode, _reapplyWideTyres;
        private int _reapplyWideTyresLevel;
        private bool _reapplyFov, _reapplySpeedrunTimer, _reapplyAcceleration, _reapplyMaxSpeed;
        private bool _reapplyLandingImpact;
        private bool _reapplyMoveSpin, _reapplyMoveHop, _reapplyMoveWheelie, _reapplyMoveLean;
        private bool _reapplyBikeTorch; private int _reapplyBikeTorchIntensity;
        private bool _reapplyCameraShake; private int _reapplyCameraShakeLevel;
        private bool _reapplyNearMiss; private int _reapplyNearMissLevel;
        private bool _reapplyExplodingProps;
        private bool _reapplyHoverMode; private float _reapplyHoverHeight;
        private bool _reapplyBouncyBike; private int _reapplyBouncyLevel;
        private bool _reapplyBlizzardDial; private int _reapplyBlizzardSnow; private int _reapplyBlizzardSeason;
        private bool _reapplyDiscoMode; private int _reapplyDiscoSpeed;
        private bool _reapplyDiscoTorch;
        private bool _reapplyTrickMultiplier; private int _reapplyTrickMultiplierLevel;
        private bool _reapplySuspensionHUD;
        private bool _reapplyScreenshotMode;
        private float _reapplyCOMx, _reapplyCOMy, _reapplyCOMz; private bool _reapplyCOMNeeded;
        private int _reapplySuspTravel, _reapplySuspStiff, _reapplySuspDamp; private bool _reapplySuspNeeded;
        private float _reapplyBikeScale; private bool _reapplyBikeScaleNeeded;
        private float _reapplyPlayerScale; private bool _reapplyPlayerScaleNeeded;
        private bool _reapplyInvisibleBike;
        private bool _reapplyInvisiblePlayer;
        private bool _reapplyWheelSize; private int _reapplyWheelSizeMode; private int _reapplyWheelSizeLevel;
        private bool _reapplyIndividualWheel; private int _reapplyFrontWheelLevel, _reapplyRearWheelLevel;
        private bool _reapplyBrakeFade;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Starting Descenders Sandbox");
            try { CodeStage.AntiCheat.Detectors.InjectionDetector.Dispose(); }
            catch (System.Exception ex) { ModLog.Warn("AntiCheat dispose failed (InjectionDetector): " + ex.Message); }
            try { CodeStage.AntiCheat.Detectors.ObscuredCheatingDetector.Dispose(); }
            catch (System.Exception ex) { ModLog.Warn("AntiCheat dispose failed (ObscuredCheatingDetector): " + ex.Message); }
            try { CodeStage.AntiCheat.Detectors.SpeedHackDetector.Dispose(); }
            catch (System.Exception ex) { ModLog.Warn("AntiCheat dispose failed (SpeedHackDetector): " + ex.Message); }
            try { CodeStage.AntiCheat.Detectors.TimeCheatingDetector.Dispose(); }
            catch (System.Exception ex) { ModLog.Warn("AntiCheat dispose failed (TimeCheatingDetector): " + ex.Message); }
            try { CodeStage.AntiCheat.Detectors.WallHackDetector.Dispose(); }
            catch (System.Exception ex) { ModLog.Warn("AntiCheat dispose failed (WallHackDetector): " + ex.Message); }

            harmony = new HarmonyLib.Harmony("DescendersModMenu.Patches");
            try { harmony.PatchAll(); DiagnosticsManager.Report("Harmony", true); }
            catch (System.Exception ex) { MelonLogger.Error("PatchAll failed: " + ex.Message); DiagnosticsManager.Report("Harmony", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "PatchAll failed"); }
            try { NoSpeedCap.ApplyPatch(harmony); DiagnosticsManager.Report("NoSpeedCap", true); }
            catch (System.Exception ex) { MelonLogger.Error("NoSpeedCap.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("NoSpeedCap", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "NoSpeedCap"); }
            try { NoSpeedCap.ApplyVCPatch(harmony); DiagnosticsManager.Report("NoSpeedCap (VC)", true); }
            catch (System.Exception ex) { MelonLogger.Error("NoSpeedCap.ApplyVCPatch: " + ex.Message); DiagnosticsManager.Report("NoSpeedCap (VC)", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "NoSpeedCap.ApplyVCPatch"); }
            try { QuickBrake.ApplyPatch(harmony); DiagnosticsManager.Report("QuickBrake", true); }
            catch (System.Exception ex) { MelonLogger.Error("QuickBrake.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("QuickBrake", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "QuickBrake"); }
            try { CutBrakes.ApplyPatch(harmony); DiagnosticsManager.Report("CutBrakes", true); }
            catch (System.Exception ex) { MelonLogger.Error("CutBrakes.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("CutBrakes", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "CutBrakes"); }
            try { BrakeFade.ApplyPatch(harmony); DiagnosticsManager.Report("BrakeFade", true); }
            catch (System.Exception ex) { MelonLogger.Error("BrakeFade.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("BrakeFade", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "BrakeFade"); }
            try { BikeDamage.ApplyPatch(harmony); DiagnosticsManager.Report("BikeDamage", true); }
            catch (System.Exception ex) { MelonLogger.Error("BikeDamage.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("BikeDamage", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "BikeDamage"); }
            try { ReverseSteering.ApplyPatch(harmony); DiagnosticsManager.Report("ReverseSteering", true); }
            catch (System.Exception ex) { MelonLogger.Error("ReverseSteering.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("ReverseSteering", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "ReverseSteering"); }
            try { RubberBandSteering.ApplyPatch(harmony); DiagnosticsManager.Report("RubberBandSteering", true); }
            catch (System.Exception ex) { MelonLogger.Error("RubberBandSteering.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("RubberBandSteering", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "RubberBandSteering"); }
            try { AutoBalance.ApplyPatch(harmony); DiagnosticsManager.Report("AutoBalance", true); }
            catch (System.Exception ex) { MelonLogger.Error("AutoBalance.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("AutoBalance", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "AutoBalance"); }
            try { IceMode.ApplyPatch(harmony); DiagnosticsManager.Report("IceMode", true); }
            catch (System.Exception ex) { MelonLogger.Error("IceMode.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("IceMode", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "IceMode"); }
            try { BlizzardDial.ApplyPatch(harmony); }
            catch (System.Exception ex) { MelonLogger.Error("BlizzardDial.ApplyPatch: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BlizzardDial"); DiagnosticsManager.Report("BlizzardDial", false, ex.Message); }
            try { TyrePressure.ApplyPatch(harmony); DiagnosticsManager.Report("TyrePressure", true); }
            catch (System.Exception ex) { MelonLogger.Error("TyrePressure.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("TyrePressure", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "TyrePressure"); }
            try { SkyColours.ApplyPatch(harmony); }
            catch (System.Exception ex) { MelonLogger.Error("SkyColours.ApplyPatch: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "SkyColours"); }
            try { DrunkMode.ApplyPatch(harmony); DiagnosticsManager.Report("DrunkMode", true); }
            catch (System.Exception ex) { MelonLogger.Error("DrunkMode.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("DrunkMode", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "DrunkMode"); }
            try { SessionTrackers.ApplyBailPatch(harmony); DiagnosticsManager.Report("BailCounter", true); }
            catch (System.Exception ex) { MelonLogger.Error("BailPatch: " + ex.Message); DiagnosticsManager.Report("BailCounter", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "BailPatch"); }
            try { SessionTrackers.ApplyCheckpointPatch(harmony); DiagnosticsManager.Report("CheckpointCounter", true); }
            catch (System.Exception ex) { MelonLogger.Error("CheckpointPatch: " + ex.Message); DiagnosticsManager.Report("CheckpointCounter", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "CheckpointPatch"); }
            try { GhostReplay.ApplyPatch(harmony); DiagnosticsManager.Report("GhostReplay", true); }
            catch (System.Exception ex) { MelonLogger.Error("GhostReplay.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("GhostReplay", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "GhostReplay"); }
            try { GameModifierMods.ApplyNoSpeedWobblesPatch(harmony); DiagnosticsManager.Report("NoSpeedWobbles", true); }
            catch (System.Exception ex) { MelonLogger.Error("NoSpeedWobbles patch: " + ex.Message); DiagnosticsManager.Report("NoSpeedWobbles", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "NoSpeedWobbles"); }
            try { WheelieAngleLimit.ApplyPatch(harmony); DiagnosticsManager.Report("WheelieAngleLimit", true); }
            catch (System.Exception ex) { MelonLogger.Error("WheelieAngleLimit patch: " + ex.Message); DiagnosticsManager.Report("WheelieAngleLimit", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "WheelieAngleLimit"); }
            try { TrickSetSwap.ApplyPatch(harmony); DiagnosticsManager.Report("TrickSetSwap", true); }
            catch (System.Exception ex) { MelonLogger.Error("TrickSetSwap patch: " + ex.Message); DiagnosticsManager.Report("TrickSetSwap", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "TrickSetSwap"); }
            try { MapChanger.ApplyPatch(harmony); DiagnosticsManager.Report("MapChanger", true); }
            catch (System.Exception ex) { ModLog.Warn("MapChanger.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("MapChanger", false, ex.Message); }
            try { NoBail.ApplyPatch(harmony); }
            catch (System.Exception ex) { MelonLogger.Error("NoBail.ApplyPatch: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "NoBail"); }
            try { SlowMoOnBail.ApplyPatch(harmony); }
            catch (System.Exception ex) { MelonLogger.Error("SlowMoOnBail.ApplyPatch: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "SlowMoOnBail"); }
            try { CompassAlwaysOn.ApplyPatch(harmony); DiagnosticsManager.Report("CompassAlwaysOn", true); }
            catch (System.Exception ex) { MelonLogger.Error("CompassAlwaysOn.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("CompassAlwaysOn", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "CompassAlwaysOn"); }
            try { SpectateMode.ApplyPatch(harmony); DiagnosticsManager.Report("SpectateModePatch", true); }
            catch (System.Exception ex) { MelonLogger.Error("SpectateMode.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("SpectateModePatch", false, ex.Message);  Telemetry.ReportErrorAsync(ex, "SpectateMode"); }
            try { OutfitPresets.Init(); }
            catch (System.Exception ex) { MelonLogger.Error("OutfitPresets.Init: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "OutfitPresets.Init"); }
            try { ModChat.Init(); }
            catch (System.Exception ex) { MelonLogger.Error("ModChat.Init: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "ModChat.Init"); }

            try
            {
                var failures = new System.Collections.Generic.List<string>();
                foreach (var s in DiagnosticsManager.Statuses)
                    if (!s.OK) failures.Add(s.Name + ": " + s.Error);
                if (failures.Count > 0) Telemetry.ReportInitFailuresAsync(failures);
            }
            catch (System.Exception ex) { ModLog.Warn("Telemetry.ReportInitFailuresAsync: " + ex.Message); }
        }

        public override void OnLateInitializeMelon()
        {
            DiagnosticsManager.Report("SlowMotion", true); DiagnosticsManager.Report("FOV", true);
            DiagnosticsManager.Report("ESP", true); DiagnosticsManager.Report("NoBail", true);
            DiagnosticsManager.Report("Acceleration", true); DiagnosticsManager.Report("MaxSpeed", true);
            DiagnosticsManager.Report("BikeSwitcher", true); DiagnosticsManager.Report("TeleportToPlayer", true); DiagnosticsManager.Report("SpectateMode", true);
            DiagnosticsManager.Report("Movement", true); DiagnosticsManager.Report("Gravity", true);
            DiagnosticsManager.Report("TimeOfDay", true); DiagnosticsManager.Report("GameModifiers", true);
            DiagnosticsManager.Report("TopSpeed", true); DiagnosticsManager.Report("TeleportToCheckpoint", true);
            DiagnosticsManager.Report("Suspension", true); DiagnosticsManager.Report("Trees & Foliage", true);
            DiagnosticsManager.Report("Music", true); DiagnosticsManager.Report("Jump to Finish", true);
            DiagnosticsManager.Report("Skip Song", true); DiagnosticsManager.Report("Bike Size", true);
            DiagnosticsManager.Report("Player Size", true); DiagnosticsManager.Report("Invisible Player", true);
            DiagnosticsManager.Report("Turbo Wind", true); DiagnosticsManager.Report("No Mistakes", true);
            DiagnosticsManager.Report("Giant Everyone", true); DiagnosticsManager.Report("Invisible Bike", true);
            DiagnosticsManager.Report("Moon Mode", true); DiagnosticsManager.Report("Wheel Size", true);
            DiagnosticsManager.Report("Fog Remover", true); DiagnosticsManager.Report("SessionTrackers", true);
            DiagnosticsManager.Report("WideTyres", true); DiagnosticsManager.Report("StickyTyres", true); DiagnosticsManager.Report("SpiderBike", true);
            DiagnosticsManager.Report("FlyMode", true); DiagnosticsManager.Report("MirrorMode", true);
            DiagnosticsManager.Report("SpeedrunTimer", true); DiagnosticsManager.Report("SlowMoOnBail", true); DiagnosticsManager.Report("BlackDeath", true);
            DiagnosticsManager.Report("AirControl", true); DiagnosticsManager.Report("ModDetection", true);
            TopSpeed.Load(); TopSpeed.StartTracking();

            try { UI.FavouritesManager.LoadFromFile(); }
            catch (System.Exception ex) { ModLog.Warn("FavouritesManager.LoadFromFile: " + ex.Message); }

            try { KeyBindManager.LoadBindings(); }
            catch (System.Exception ex) { ModLog.Warn("KeyBindManager.LoadBindings: " + ex.Message); }

            try { UI.ColorSchemeManager.LoadAndApply(); }
            catch (System.Exception ex) { ModLog.Warn("ColorSchemeManager.LoadAndApply: " + ex.Message); }

            DiagnosticsManager.LogStartupSummary();

            try { UpdateChecker.CheckAsync(); } catch (System.Exception ex) { MelonLogger.Error("UpdateChecker.CheckAsync: " + ex.Message); Telemetry.ReportErrorAsync(ex, "UpdateChecker"); }
            try { SteamPlayerCount.FetchAsync(); } catch (System.Exception ex) { MelonLogger.Error("SteamPlayerCount.FetchAsync: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SteamPlayerCount"); }
        }

        public override void OnSceneWasLoaded(int buildindex, string sceneName)
        {
            GhostReplay.OnSceneLoaded();
        }

        public override void OnSceneWasInitialized(int buildindex, string sceneName)
        {
            SkyColours.CaptureSceneDefaults();
            GraphicsSettings.CaptureDefaultQuality();
            TimeOfDay.CaptureSceneDefault();
            try { UI.BikePage.CaptureSceneDefaults(); } catch { }
            try { UI.FunPage.CaptureSceneDefaults(); } catch { }
            GhostReplay.OnSceneInitialized();
            MapChanger.OnSceneInitialized();
            ExplodingProps.OnSceneInitialized(sceneName);
            if (buildindex == 1) MapChanger.BuildMapList();
            try { UI.SessionPage.RefreshAll(); } catch { }

            if (buildindex > 0)
            {
                try { ModChat.EnablePhotonAccess(); }
                catch (System.Exception ex) { ModLog.Warn("[ModChat] EnablePhotonAccess: " + ex.Message); }
            }
        }

        // ================================================================
        // ================================================================
        // ================================================================
        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            try { FavouritesManager.ClearStarButtons(); } catch (System.Exception ex) { MelonLogger.Error("FavouritesManager.ClearStarButtons: " + ex.Message); Telemetry.ReportErrorAsync(ex, "FavouritesManager"); }
            try { ModChat.OnMapChanged(); } catch { }
            try { ModesPage.ClearUiRefs(); } catch { }
            try { ChatPage.ClearUiRefs(); } catch { }
            try { LavaRisingPage.ClearUiRefs(); } catch { }
            try { InfoPage.ClearUiRefs(); } catch { }
            try { FavsPage.ClearUiRefs(); } catch { }
            try { MapPage.ClearUiRefs(); } catch { }
            try { EspPage.ClearUiRefs(); } catch { }
            try { ObjectPlacerPage.ClearUiRefs(); } catch { }
            try { BikePage.ClearUiRefs(); } catch { }
            try { GhostPage.ClearUiRefs(); } catch { }
            try { SessionPage.ClearUiRefs(); } catch { }
            try { LavaRising.ClearCache(); } catch (System.Exception ex) { MelonLogger.Error("LavaRising.ClearCache: " + ex.Message); Telemetry.ReportErrorAsync(ex, "LavaRising"); }
            try { CompassAlwaysOn.ClearCache(); } catch (System.Exception ex) { MelonLogger.Error("CompassAlwaysOn.ClearCache: " + ex.Message); Telemetry.ReportErrorAsync(ex, "CompassAlwaysOn"); }
            try { TimeOfDay.ClearCache(); } catch (System.Exception ex) { MelonLogger.Error("TimeOfDay.ClearCache: " + ex.Message); Telemetry.ReportErrorAsync(ex, "TimeOfDay"); }
            try { PlayerCache.Clear(); } catch (System.Exception ex) { MelonLogger.Error("PlayerCache.Clear: " + ex.Message); Telemetry.ReportErrorAsync(ex, "PlayerCache"); }

            if (_pendingReapply)
            {
                ModLog.Debug("[Reapply] Skipping intermediate scene (" + sceneName + ")");
                return;
            }


            bool wasSlowMotion = SlowMotion.Enabled;
            int slowMotionLv = SlowMotion.Level;
            bool wasCutBrakes = CutBrakes.Enabled;
            bool wasReverseSteering = ReverseSteering.Enabled;
            bool wasRubberBandSteering = RubberBandSteering.Enabled;
            int rubberBandLevel = RubberBandSteering.Level;
            bool wasAutoBalance = AutoBalance.Enabled;
            int autoBalanceLv = AutoBalance.StrengthLevel;
            bool wasQuickBrake = QuickBrake.Enabled;
            bool wasWheelieAngle = WheelieAngleLimit.Enabled;
            bool wasNoSpeedWobbles = GameModifierMods.NoSpeedWobblesEnabled;
            bool wasSlowMoOnBail = SlowMoOnBail.Enabled;
            bool wasBlackDeath = BlackDeath.Enabled;
            bool wasIceMode = IceMode.Enabled;
            bool wasTyrePressure = TyrePressure.Enabled;
            int tyrePressureLv = TyrePressure.Level;
            bool wasBikeDamage = BikeDamage.Enabled;
            bool wasHeadlightsOnly = HeadlightsOnly.Enabled;
            bool wasUIRemover = UIRemover.Enabled;
            bool wasInstantRespawn = InstantRespawn.Enabled;
            bool wasStickyTyres = StickyTyres.Enabled;
            bool wasSpiderBike = SpiderBike.Enabled;
            bool wasAirControl = AirControl.Enabled;
            bool wasNoBail = NoBail.Enabled;
            int gravLevel = Gravity.Level;
            bool gravNeed = gravLevel != 5;

            bool wasFlyMode = FlyMode.Enabled;
            bool wasDrunkMode = DrunkMode.Enabled;
            bool wasMirrorMode = MirrorMode.Enabled;
            bool wasWideTyres = WideTyres.Enabled;
            int wideTyresLevel = WideTyres.Level;
            bool wasFov = FOV.Enabled;
            bool wasSpeedrunTimer = SpeedrunTimer.Enabled;
            bool wasAcceleration = Acceleration.Enabled;
            bool wasMaxSpeed = MaxSpeedMultiplier.Enabled;
            bool wasLandingImpact = LandingImpact.Enabled;
            bool wasMoveSpin = Movement.SpinEnabled;
            bool wasMoveHop = Movement.HopEnabled;
            bool wasMoveWheelie = Movement.WheelieEnabled;
            bool wasMoveLean = Movement.LeanEnabled;
            bool wasBikeTorch = BikeTorch.Enabled;
            int torchInt = BikeTorch.IntensityIndex;
            bool wasCamShake = CameraShake.Enabled;
            int camShakeLv = CameraShake.Level;
            bool wasNearMiss = NearMissSensitivity.Enabled;
            int nearMissLv = NearMissSensitivity.Level;
            bool wasExploding = ExplodingProps.Enabled;
            bool wasHoverMode = HoverMode.Enabled;
            float hoverHeight = HoverMode.HoverHeight;
            bool wasBouncyBike = BouncyBike.Enabled;
            int bouncyLv = BouncyBike.BouncinessLevel;
            bool wasBlizzard = BlizzardDial.Enabled;
            int blizzardSnow = BlizzardDial.SnowLevel;
            int blizzardSeason = BlizzardDial.SeasonIndex;
            bool wasDisco = DiscoMode.Enabled;
            int discoSpeed = DiscoMode.SpeedLevel;
            bool wasDiscoTorch = BikeTorch.DiscoEnabled;
            bool wasTrickMult = TrickMultiplier.Enabled;
            int trickMultLv = TrickMultiplier.Level;
            bool wasSuspHUD = SuspensionHUD.Enabled;
            bool wasScreenshot = ScreenshotMode.Enabled;
            float cx = CenterOfMass.OffsetLR, cy = CenterOfMass.OffsetUD, cz = CenterOfMass.OffsetFB;
            bool comNeed = cx != 0f || cy != 0f || cz != 0f;
            int sT = Suspension.TravelLevel, sS = Suspension.StiffnessLevel, sD = Suspension.DampingLevel;
            bool suspNeed = sT != 5 || sS != 5 || sD != 5;
            bool wasNoSpeedCap = NoSpeedCap.Enabled;
            float bikeScale = BikeSize.CurrentScale;
            bool bikeScaleNeed = bikeScale != 1f;
            float playerScale = PlayerSize.CurrentScale;
            bool playerScaleNeed = playerScale != 1f;
            bool wasInvisBike = InvisibleBike.Enabled;
            bool wasInvisPlayer = InvisiblePlayer.Enabled;
            bool wasWheelSize = WheelSize.IsEnabled;
            int wheelSizeMode = WheelSize.Mode;
            int wheelSizeLevel = WheelSize.Level;
            bool wasIndividualWheel = WheelSize.IsIndividualMode;
            int frontWheelLv = WheelSize.FrontLevel;
            int rearWheelLv = WheelSize.RearLevel;
            bool wasBrakeFade = BrakeFade.Enabled;
            int brakeBalanceLv = BrakeFade.BalanceLevel;

            ModLog.Debug("[Reapply] === SNAPSHOT (" + sceneName + ") ===");
            if (wasSlowMotion) ModLog.Debug("[Reapply]   SlowMotion lv=" + slowMotionLv);
            if (wasCutBrakes) ModLog.Debug("[Reapply]   CutBrakes");
            if (wasReverseSteering) ModLog.Debug("[Reapply]   ReverseSteering");
            if (wasRubberBandSteering) ModLog.Debug("[Reapply]   RubberBandSteering lv=" + rubberBandLevel);
            if (wasAutoBalance) ModLog.Debug("[Reapply]   AutoBalance lv=" + autoBalanceLv);
            if (wasQuickBrake) ModLog.Debug("[Reapply]   QuickBrake");
            if (wasWheelieAngle) ModLog.Debug("[Reapply]   WheelieAngle");
            if (wasNoSpeedWobbles) ModLog.Debug("[Reapply]   NoSpeedWobbles");
            if (wasSlowMoOnBail) ModLog.Debug("[Reapply]   SlowMoOnBail");
            if (wasBlackDeath) ModLog.Debug("[Reapply]   BlackDeath");
            if (wasIceMode) ModLog.Debug("[Reapply]   IceMode");
            if (wasTyrePressure) ModLog.Debug("[Reapply]   TyrePressure lv=" + tyrePressureLv);
            if (wasInstantRespawn) ModLog.Debug("[Reapply]   InstantRespawn");
            if (wasStickyTyres) ModLog.Debug("[Reapply]   StickyTyres");
            if (wasSpiderBike) ModLog.Debug("[Reapply]   SpiderBike");
            if (wasAirControl) ModLog.Debug("[Reapply]   AirControl");
            if (wasNoBail) ModLog.Debug("[Reapply]   NoBail");
            if (gravNeed) ModLog.Debug("[Reapply]   Gravity lv=" + gravLevel);
            if (wasFlyMode) ModLog.Debug("[Reapply]   FlyMode");
            if (wasDrunkMode) ModLog.Debug("[Reapply]   DrunkMode");
            if (wasMirrorMode) ModLog.Debug("[Reapply]   MirrorMode");
            if (wasWideTyres) ModLog.Debug("[Reapply]   WideTyres lv=" + wideTyresLevel);
            if (wasFov) ModLog.Debug("[Reapply]   FOV");
            if (wasSpeedrunTimer) ModLog.Debug("[Reapply]   SpeedrunTimer");
            if (wasAcceleration) ModLog.Debug("[Reapply]   Acceleration");
            if (wasMaxSpeed) ModLog.Debug("[Reapply]   MaxSpeed");
            if (wasLandingImpact) ModLog.Debug("[Reapply]   LandingImpact");
            if (wasMoveSpin) ModLog.Debug("[Reapply]   Spin");
            if (wasMoveHop) ModLog.Debug("[Reapply]   Hop");
            if (wasMoveWheelie) ModLog.Debug("[Reapply]   Wheelie");
            if (wasMoveLean) ModLog.Debug("[Reapply]   Lean");
            if (wasBikeTorch) ModLog.Debug("[Reapply]   BikeTorch int=" + torchInt);
            if (wasCamShake) ModLog.Debug("[Reapply]   CameraShake lv=" + camShakeLv);
            if (wasNearMiss) ModLog.Debug("[Reapply]   NearMiss lv=" + nearMissLv);
            if (wasExploding) ModLog.Debug("[Reapply]   ExplodingProps");
            if (wasHoverMode) ModLog.Debug("[Reapply]   HoverMode h=" + hoverHeight);
            if (wasBouncyBike) ModLog.Debug("[Reapply]   BouncyBike lv=" + bouncyLv);
            if (wasBlizzard) ModLog.Debug("[Reapply]   BlizzardDial snow=" + blizzardSnow + " season=" + blizzardSeason);
            if (wasDisco) ModLog.Debug("[Reapply]   DiscoMode spd=" + discoSpeed);
            if (wasDiscoTorch) ModLog.Debug("[Reapply]   DiscoTorch");
            if (wasTrickMult) ModLog.Debug("[Reapply]   TrickMultiplier lv=" + trickMultLv);
            if (wasScreenshot) ModLog.Debug("[Reapply]   ScreenshotMode");
            if (comNeed) ModLog.Debug("[Reapply]   COM " + cx + "/" + cy + "/" + cz);
            if (suspNeed) ModLog.Debug("[Reapply]   Susp " + sT + "/" + sS + "/" + sD);
            if (wasNoSpeedCap) ModLog.Debug("[Reapply]   NoSpeedCap");
            if (bikeScaleNeed) ModLog.Debug("[Reapply]   BikeScale=" + bikeScale);
            if (playerScaleNeed) ModLog.Debug("[Reapply]   PlayerScale=" + playerScale);
            if (wasInvisBike) ModLog.Debug("[Reapply]   InvisibleBike");
            if (wasInvisPlayer) ModLog.Debug("[Reapply]   InvisiblePlayer");
            if (wasWheelSize) ModLog.Debug("[Reapply]   WheelSize level=" + wheelSizeLevel + " mode=" + wheelSizeMode);
            if (wasIndividualWheel) ModLog.Debug("[Reapply]   IndividualWheel F=" + frontWheelLv + " R=" + rearWheelLv);
            if (wasSuspHUD) ModLog.Debug("[Reapply]   SuspensionHUD");
            if (wasBrakeFade) ModLog.Debug("[Reapply]   BrakeFade balance=" + brakeBalanceLv);

            SlowMotion.Reset(); QuickBrake.Reset(); QuickBrake_Patch.ClearCache();
            CutBrakes.Reset(); ReverseSteering.Reset(); AutoBalance.Reset();
            RubberBandSteering.Reset(); RubberBandSteering.ClearCache();
            WideTyres.Reset(); IceMode.Reset(); TyrePressure.Reset(); InstantRespawn.Reset(); BikeDamage.Reset(); HeadlightsOnly.Reset(); UIRemover.Reset(); ScreenshotMode.Reset(); SpeedrunTimer.Reset();
            GameModifierMods.NoSpeedWobblesReset();
            MirrorMode.Reset(); FlyMode.Reset(); DrunkMode.Reset(); HoverMode.Reset();
            SpectateMode.Reset(); SpectateMode.ClearCache();
            OutfitPresets.Reset(); ModChat.Reset(); ModDetection.ResetTag(); ChatHUD.Reset(); SlowMoOnBail.Reset(); BlackDeath.Reset(); MultiplayerMenuGuard.Reset();
            StickyTyres.Reset(); SpiderBike.Reset(); WheelieAngleLimit.Reset(); AirControl.Reset();
            CenterOfMass.Reset();
            FOV.Reset(); Acceleration.Reset(); MaxSpeedMultiplier.Reset();
            Movement.Reset(); LandingImpact.Reset();
            NoBail.ClearCache(); WheelSize.Reset();
            if (NoSpeedCap.Enabled) NoSpeedCap.SetEnabled(false);
            if (NoBail.Enabled) NoBail.SetEnabled(false);
            BikeTorch.Reset(); CameraShake.Reset(); NearMissSensitivity.Reset();
            SkyColours.Reset(); GraphicsSettings.Reset();
            AvalancheMode.Reset(); GhostReplay.Reset();
            EarthquakeMode.Reset(); PoliceChaseMode.Reset();
            TrickAttackMode.Reset(); BoulderDodgeMode.Reset();
            SurvivalMode.Reset();
            LavaRising.Reset();
            SessionTrackers.Reset(); ExplodingProps.Reset();
            Trees.Reset(); BouncyBike.Reset(); BlizzardDial.Reset(); DiscoMode.Reset();
            ObjectPlacer.Reset();
            SuspensionHUD.ClearCache();
            BrakeFade.ClearCache(); BrakeFade_Patch.ClearCache();
            TrickMultiplier.Reset();
            BikeDamage.ClearBoneCache();
            HeadlightsOnly.ClearCache();
            UIRemover.ClearCache();
            TrailPainter.ClearCache();
            BigHeadMode.ClearCache();
            TopSpeed.ClearCache();
            try { ESP.ClearCache(); } catch (System.Exception ex) { MelonLogger.Error("ESP.ClearCache: " + ex.Message); Telemetry.ReportErrorAsync(ex, "ESP"); }
            if (ESP.Enabled) ESP.Toggle();

            ModLog.SuppressUserFeedback = true;
            try
            {
            if (wasSlowMotion) { SlowMotion.SetLevel(slowMotionLv); SlowMotion.Toggle(); ModLog.Debug("[Reapply] IMM SlowMotion -> " + SlowMotion.Enabled + " lv=" + slowMotionLv); }
            if (wasCutBrakes) { CutBrakes.Toggle(); ModLog.Debug("[Reapply] IMM CutBrakes -> " + CutBrakes.Enabled); }
            if (wasReverseSteering) { ReverseSteering.Toggle(); ModLog.Debug("[Reapply] IMM ReverseSteering -> " + ReverseSteering.Enabled); }
            if (wasRubberBandSteering) { RubberBandSteering.SetLevel(rubberBandLevel); RubberBandSteering.Toggle(); ModLog.Debug("[Reapply] IMM RubberBandSteering -> " + RubberBandSteering.Enabled + " lv=" + rubberBandLevel); }
            if (wasAutoBalance) { AutoBalance.SetStrengthLevel(autoBalanceLv); AutoBalance.Toggle(); ModLog.Debug("[Reapply] IMM AutoBalance -> " + AutoBalance.Enabled + " lv=" + autoBalanceLv); }
            if (wasQuickBrake) { QuickBrake.Toggle(); ModLog.Debug("[Reapply] IMM QuickBrake -> " + QuickBrake.Enabled); }
            if (wasWheelieAngle) { WheelieAngleLimit.Toggle(); ModLog.Debug("[Reapply] IMM WheelieAngle -> " + WheelieAngleLimit.Enabled); }
            if (wasNoSpeedWobbles) { GameModifierMods.NoSpeedWobblesToggle(); ModLog.Debug("[Reapply] IMM NoSpeedWobbles -> " + GameModifierMods.NoSpeedWobblesEnabled); }
            if (wasSlowMoOnBail) { SlowMoOnBail.Toggle(); ModLog.Debug("[Reapply] IMM SlowMoOnBail -> " + SlowMoOnBail.Enabled); }
            if (wasBlackDeath) { BlackDeath.Toggle(); ModLog.Debug("[Reapply] IMM BlackDeath -> " + BlackDeath.Enabled); }
            if (wasIceMode) { IceMode.Toggle(); ModLog.Debug("[Reapply] IMM IceMode -> " + IceMode.Enabled); }
            if (wasTyrePressure) { TyrePressure.SetLevel(tyrePressureLv); TyrePressure.Toggle(); ModLog.Debug("[Reapply] IMM TyrePressure -> " + TyrePressure.Enabled + " lv=" + tyrePressureLv); }
            if (wasBikeDamage) { BikeDamage.Toggle(); ModLog.Debug("[Reapply] IMM BikeDamage -> " + BikeDamage.Enabled); }
            if (wasHeadlightsOnly) { HeadlightsOnly.Toggle(); ModLog.Debug("[Reapply] IMM HeadlightsOnly -> " + HeadlightsOnly.Enabled); }
            if (wasUIRemover) { UIRemover.Toggle(); ModLog.Debug("[Reapply] IMM UIRemover -> " + UIRemover.Enabled); }
            if (wasInstantRespawn) { InstantRespawn.Toggle(); ModLog.Debug("[Reapply] IMM InstantRespawn -> " + InstantRespawn.Enabled); }
            if (wasStickyTyres) { StickyTyres.Toggle(); ModLog.Debug("[Reapply] IMM StickyTyres -> " + StickyTyres.Enabled); }
            if (wasSpiderBike) { SpiderBike.Toggle(); ModLog.Debug("[Reapply] IMM SpiderBike -> " + SpiderBike.Enabled); }
            if (wasAirControl) { AirControl.Toggle(); ModLog.Debug("[Reapply] IMM AirControl -> " + AirControl.Enabled); }
            if (wasNoSpeedCap) { NoSpeedCap.Toggle(); ModLog.Debug("[Reapply] IMM NoSpeedCap -> " + NoSpeedCap.Enabled); }
            if (wasNoBail) { NoBail.Toggle(); ModLog.Debug("[Reapply] IMM NoBail -> " + NoBail.Enabled); }
            if (gravNeed) { Gravity.SetLevel(gravLevel); Gravity.Apply(); ModLog.Debug("[Reapply] IMM Gravity lv=" + gravLevel); }
            ModLog.Debug("[Reapply] Immediate restores done.");

            _reapplyFlyMode = wasFlyMode; _reapplyDrunkMode = wasDrunkMode;
            _reapplyMirrorMode = wasMirrorMode; _reapplyWideTyres = wasWideTyres;
            _reapplyWideTyresLevel = wideTyresLevel;
            _reapplyFov = wasFov; _reapplySpeedrunTimer = wasSpeedrunTimer;
            _reapplyAcceleration = wasAcceleration; _reapplyMaxSpeed = wasMaxSpeed;
            _reapplyLandingImpact = wasLandingImpact;
            _reapplyMoveSpin = wasMoveSpin; _reapplyMoveHop = wasMoveHop;
            _reapplyMoveWheelie = wasMoveWheelie; _reapplyMoveLean = wasMoveLean;
            _reapplyBikeTorch = wasBikeTorch; _reapplyBikeTorchIntensity = torchInt;
            _reapplyCameraShake = wasCamShake; _reapplyCameraShakeLevel = camShakeLv;
            _reapplyNearMiss = wasNearMiss; _reapplyNearMissLevel = nearMissLv;
            _reapplyExplodingProps = wasExploding;
            _reapplyHoverMode = wasHoverMode; _reapplyHoverHeight = hoverHeight;
            _reapplyBouncyBike = wasBouncyBike; _reapplyBouncyLevel = bouncyLv;
            _reapplyBlizzardDial = wasBlizzard; _reapplyBlizzardSnow = blizzardSnow; _reapplyBlizzardSeason = blizzardSeason;
            _reapplyDiscoMode = wasDisco; _reapplyDiscoSpeed = discoSpeed;
            _reapplyDiscoTorch = wasDiscoTorch;
            _reapplyTrickMultiplier = wasTrickMult; _reapplyTrickMultiplierLevel = trickMultLv;
            _reapplySuspensionHUD = wasSuspHUD;
            _reapplyScreenshotMode = wasScreenshot;
            _reapplyCOMx = cx; _reapplyCOMy = cy; _reapplyCOMz = cz; _reapplyCOMNeeded = comNeed;
            _reapplySuspTravel = sT; _reapplySuspStiff = sS; _reapplySuspDamp = sD; _reapplySuspNeeded = suspNeed;
            _reapplyBikeScale = bikeScale; _reapplyBikeScaleNeeded = bikeScaleNeed;
            _reapplyPlayerScale = playerScale; _reapplyPlayerScaleNeeded = playerScaleNeed;
            _reapplyInvisibleBike = wasInvisBike;
            _reapplyInvisiblePlayer = wasInvisPlayer;
            _reapplyWheelSize = wasWheelSize; _reapplyWheelSizeMode = wheelSizeMode; _reapplyWheelSizeLevel = wheelSizeLevel;
            _reapplyIndividualWheel = wasIndividualWheel; _reapplyFrontWheelLevel = frontWheelLv; _reapplyRearWheelLevel = rearWheelLv;
            _reapplyBrakeFade = wasBrakeFade;

            if (_reapplyBrakeFade) { _reapplyBrakeFade = false; BrakeFade.SetBalanceLevel(brakeBalanceLv); BrakeFade.Toggle(); ModLog.Debug("[Reapply] IMM BrakeFade -> " + BrakeFade.Enabled); }
            if (_reapplySuspensionHUD) { _reapplySuspensionHUD = false; if (!SuspensionHUD.Enabled) SuspensionHUD.Toggle(); ModLog.Debug("[Reapply] IMM SuspensionHUD -> " + SuspensionHUD.Enabled); }
            if (_reapplyScreenshotMode) { _reapplyScreenshotMode = false; ScreenshotMode.Toggle(); ModLog.Debug("[Reapply] IMM ScreenshotMode -> " + ScreenshotMode.Enabled); }
            if (_reapplyTrickMultiplier) { _reapplyTrickMultiplier = false; TrickMultiplier.SetLevel(_reapplyTrickMultiplierLevel); ModLog.Debug("[Reapply] IMM TrickMultiplier lv=" + _reapplyTrickMultiplierLevel); }

            _pendingReapply = wasFlyMode || wasDrunkMode || wasMirrorMode || wasWideTyres ||
                wasFov || wasSpeedrunTimer || wasAcceleration || wasMaxSpeed ||
                wasLandingImpact || wasMoveSpin || wasMoveHop || wasMoveWheelie || wasMoveLean ||
                wasBikeTorch || wasCamShake || wasNearMiss || wasExploding ||
                wasHoverMode || wasBouncyBike || wasBlizzard || wasDisco || wasDiscoTorch ||
                comNeed || suspNeed ||
                bikeScaleNeed || playerScaleNeed || wasInvisBike || wasInvisPlayer || wasWheelSize || wasIndividualWheel;

            if (_pendingReapply) ModLog.Debug("[Reapply] Deferred queued — waiting for Player_Human...");
            else ModLog.Debug("[Reapply] No deferred mods to reapply.");
            }
            finally { ModLog.SuppressUserFeedback = false; }
        }

        public override void OnUpdate()
        {
            if (_pendingModifierDump || _pendingAutoLoad || _pendingTelemetryPing || _pendingReapply)
            {
                GameObject playerHuman = GameObject.Find("Player_Human");
                bool hasPlayer = (object)playerHuman != null;

                if (_pendingModifierDump && hasPlayer)
                {
                    _pendingModifierDump = false;
                    try { GameModifierMods.DumpAllModifiers(); } catch (System.Exception ex) { ModLog.Warn("GameModifierMods.DumpAllModifiers: " + ex.Message); }
                }

                if (_pendingAutoLoad && hasPlayer)
                {
                    _pendingAutoLoad = false;
                    ModLog.Debug("[AutoLoad] Player_Human found — loading saved settings...");
                    ModLog.SuppressUserFeedback = true;
                    try { StatsManager.LoadStats(); }
                    catch (System.Exception ex) { ModLog.Warn("[AutoLoad] " + ex.Message); }
                    finally { ModLog.SuppressUserFeedback = false; }
                    MelonLogger.Msg("Sandbox loaded");
                    try { ModChat.EnablePhotonAccess(); }
                    catch (System.Exception ex) { ModLog.Warn("[ModChat] EnablePhotonAccess: " + ex.Message); }
                }

                if (_pendingTelemetryPing && hasPlayer)
                {
                    _pendingTelemetryPing = false;
                    try { Telemetry.PingAsync(); } catch (System.Exception ex) { ModLog.Warn("Telemetry.PingAsync: " + ex.Message); }
                }

                if (_pendingReapply && hasPlayer)
                {
                    ModLog.Debug("[Reapply] === Player_Human found — APPLYING ===");
                    _pendingReapply = false;
                    int ok = 0, fail = 0;
                    ModLog.SuppressUserFeedback = true;
                    try
                    {
                    if (_reapplyFlyMode) { _reapplyFlyMode = false; try { FlyMode.Toggle(); ok++; ModLog.Debug("[Reapply]   + FlyMode"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! FlyMode: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! FlyMode"); } }
                    if (_reapplyDrunkMode) { _reapplyDrunkMode = false; try { DrunkMode.Toggle(); ok++; ModLog.Debug("[Reapply]   + DrunkMode"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! DrunkMode: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! DrunkMode"); } }
                    if (_reapplyMirrorMode) { _reapplyMirrorMode = false; try { MirrorMode.Toggle(); ok++; ModLog.Debug("[Reapply]   + MirrorMode"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! MirrorMode: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! MirrorMode"); } }
                    if (_reapplyWideTyres) { _reapplyWideTyres = false; try { WideTyres.SetLevel(_reapplyWideTyresLevel); WideTyres.Toggle(); ok++; ModLog.Debug("[Reapply]   + WideTyres lv=" + _reapplyWideTyresLevel); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! WideTyres: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! WideTyres"); } }
                    if (_reapplyFov) { _reapplyFov = false; try { FOV.Toggle(); ok++; ModLog.Debug("[Reapply]   + FOV"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! FOV: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! FOV"); } }
                    if (_reapplySpeedrunTimer) { _reapplySpeedrunTimer = false; try { SpeedrunTimer.Toggle(); ok++; ModLog.Debug("[Reapply]   + SpeedrunTimer"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! SpeedrunTimer: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! SpeedrunTimer"); } }
                    if (_reapplyAcceleration) { _reapplyAcceleration = false; try { Acceleration.Toggle(); ok++; ModLog.Debug("[Reapply]   + Acceleration"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! Acceleration: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! Acceleration"); } }
                    if (_reapplyMaxSpeed) { _reapplyMaxSpeed = false; try { MaxSpeedMultiplier.Toggle(); ok++; ModLog.Debug("[Reapply]   + MaxSpeed"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! MaxSpeed: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! MaxSpeed"); } }
                    if (_reapplyLandingImpact) { _reapplyLandingImpact = false; try { LandingImpact.Toggle(); ok++; ModLog.Debug("[Reapply]   + LandingImpact"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! LandingImpact: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! LandingImpact"); } }
                    if (_reapplyMoveSpin) { _reapplyMoveSpin = false; try { Movement.ToggleSpin(); ok++; ModLog.Debug("[Reapply]   + Spin"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! Spin: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! Spin"); } }
                    if (_reapplyMoveHop) { _reapplyMoveHop = false; try { Movement.ToggleHop(); ok++; ModLog.Debug("[Reapply]   + Hop"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! Hop: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! Hop"); } }
                    if (_reapplyMoveWheelie) { _reapplyMoveWheelie = false; try { Movement.ToggleWheelie(); ok++; ModLog.Debug("[Reapply]   + Wheelie"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! Wheelie: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! Wheelie"); } }
                    if (_reapplyMoveLean) { _reapplyMoveLean = false; try { Movement.ToggleLean(); ok++; ModLog.Debug("[Reapply]   + Lean"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! Lean: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! Lean"); } }
                    if (_reapplyBikeTorch) { _reapplyBikeTorch = false; try { BikeTorch.IntensityIndex = _reapplyBikeTorchIntensity; BikeTorch.Toggle(); ok++; ModLog.Debug("[Reapply]   + BikeTorch"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! BikeTorch: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! BikeTorch"); } }
                    if (_reapplyCameraShake) { _reapplyCameraShake = false; try { CameraShake.SetLevel(_reapplyCameraShakeLevel); CameraShake.Toggle(); ok++; ModLog.Debug("[Reapply]   + CameraShake"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! CameraShake: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! CameraShake"); } }
                    if (_reapplyNearMiss) { _reapplyNearMiss = false; try { NearMissSensitivity.SetLevel(_reapplyNearMissLevel); NearMissSensitivity.Toggle(); ok++; ModLog.Debug("[Reapply]   + NearMiss"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! NearMiss: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! NearMiss"); } }
                    if (_reapplyExplodingProps) { _reapplyExplodingProps = false; try { ExplodingProps.Toggle(); ok++; ModLog.Debug("[Reapply]   + ExplodingProps"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! ExplodingProps: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! ExplodingProps"); } }
                    if (_reapplyHoverMode) { _reapplyHoverMode = false; try { HoverMode.SetHeight(_reapplyHoverHeight); HoverMode.Toggle(); ok++; ModLog.Debug("[Reapply]   + HoverMode"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! HoverMode: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! HoverMode"); } }
                    if (_reapplyBouncyBike) { _reapplyBouncyBike = false; try { BouncyBike.SetLevel(_reapplyBouncyLevel); BouncyBike.Toggle(); ok++; ModLog.Debug("[Reapply]   + BouncyBike"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! BouncyBike: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! BouncyBike"); } }
                    if (_reapplyBlizzardDial) { _reapplyBlizzardDial = false; try { BlizzardDial.SetSnowLevel(_reapplyBlizzardSnow); BlizzardDial.SetSeasonIndex(_reapplyBlizzardSeason); BlizzardDial.Toggle(); ok++; ModLog.Debug("[Reapply]   + BlizzardDial"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! BlizzardDial: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! BlizzardDial"); } }
                    if (_reapplyDiscoMode) { _reapplyDiscoMode = false; try { DiscoMode.SetSpeed(_reapplyDiscoSpeed); DiscoMode.Toggle(); ok++; ModLog.Debug("[Reapply]   + DiscoMode"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! DiscoMode: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! DiscoMode"); } }
                    if (_reapplyDiscoTorch) { _reapplyDiscoTorch = false; try { if (!BikeTorch.DiscoEnabled) BikeTorch.ToggleDisco(); ok++; ModLog.Debug("[Reapply]   + DiscoTorch"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! DiscoTorch: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! DiscoTorch"); } }
                    if (_reapplyCOMNeeded) { _reapplyCOMNeeded = false; try { CenterOfMass.SetLR(_reapplyCOMx); CenterOfMass.SetFB(_reapplyCOMz); CenterOfMass.SetUD(_reapplyCOMy); ok++; ModLog.Debug("[Reapply]   + COM"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! COM: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! COM"); } }
                    if (_reapplySuspNeeded) { _reapplySuspNeeded = false; try { Suspension.SetTravelLevel(_reapplySuspTravel); Suspension.SetStiffnessLevel(_reapplySuspStiff); Suspension.SetDampingLevel(_reapplySuspDamp); ok++; ModLog.Debug("[Reapply]   + Suspension"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! Suspension: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! Suspension"); } }
                    if (_reapplyBikeScaleNeeded) { _reapplyBikeScaleNeeded = false; try { BikeSize.Apply(_reapplyBikeScale); ok++; ModLog.Debug("[Reapply]   + BikeScale=" + _reapplyBikeScale); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! BikeScale: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! BikeScale"); } }
                    if (_reapplyPlayerScaleNeeded) { _reapplyPlayerScaleNeeded = false; try { PlayerSize.Apply(_reapplyPlayerScale); ok++; ModLog.Debug("[Reapply]   + PlayerScale=" + _reapplyPlayerScale); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! PlayerScale: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! PlayerScale"); } }
                    if (_reapplyInvisibleBike) { _reapplyInvisibleBike = false; try { InvisibleBike.SetEnabled(true); ok++; ModLog.Debug("[Reapply]   + InvisibleBike"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! InvisibleBike: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! InvisibleBike"); } }
                    if (_reapplyInvisiblePlayer) { _reapplyInvisiblePlayer = false; try { InvisiblePlayer.SetEnabled(true); ok++; ModLog.Debug("[Reapply]   + InvisiblePlayer"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! InvisiblePlayer: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! InvisiblePlayer"); } }
                    if (_reapplyWheelSize) { _reapplyWheelSize = false; try { WheelSize.ApplyFromSave(true, _reapplyWheelSizeLevel, _reapplyWheelSizeMode); ok++; ModLog.Debug("[Reapply]   + WheelSize level=" + _reapplyWheelSizeLevel); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! WheelSize: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! WheelSize"); } }
                    if (_reapplyIndividualWheel) { _reapplyIndividualWheel = false; try { WheelSize.ApplyIndividualFromSave(_reapplyFrontWheelLevel, _reapplyRearWheelLevel); ok++; ModLog.Debug("[Reapply]   + IndividualWheel F=" + _reapplyFrontWheelLevel + " R=" + _reapplyRearWheelLevel); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! IndividualWheel: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Reapply   ! IndividualWheel"); } }

                    ModLog.Debug("[Reapply] === DONE: " + ok + " applied, " + fail + " failed ===");

                    ModLog.Debug("[Reapply] VERIFY: NoSpeedCap=" + NoSpeedCap.Enabled
                        + " SlowMotion=" + SlowMotion.Enabled
                        + " CutBrakes=" + CutBrakes.Enabled
                        + " IceMode=" + IceMode.Enabled
                        + " StickyTyres=" + StickyTyres.Enabled
                        + " SpiderBike=" + SpiderBike.Enabled
                        + " AirControl=" + AirControl.Enabled
                        + " AutoBalance=" + AutoBalance.Enabled
                        + " WheelieAngle=" + WheelieAngleLimit.Enabled
                        + " QuickBrake=" + QuickBrake.Enabled
                        + " ReverseSteering=" + ReverseSteering.Enabled
                        + " RubberBandSteering=" + RubberBandSteering.Enabled
                        + " NoBail=" + NoBail.Enabled
                        + " Gravity=" + Gravity.Level
                        + " SuspHUD=" + SuspensionHUD.Enabled
                        + " BrakeFade=" + BrakeFade.Enabled);

                    try { MenuWindow.RefreshAll(); }
                    catch (System.Exception ex)
                    {
                        MelonLogger.Error("MenuWindow.RefreshAll: " + ex.GetType().Name + ": " + ex.Message);
                        Telemetry.ReportErrorAsync(ex, "MenuWindow");
                    }
                    }
                    finally { ModLog.SuppressUserFeedback = false; }
                }
            }

            try
            {
                if (Input.GetKeyDown(KeyCode.JoystickButton8))
                {
                    if (SurvivalMode.Enabled && SurvivalMode.IsGameOver) SurvivalMode.ResetRun();
                    else { GhostReplay.SetSpawnMarker(); GhostPage.RefreshAll(); }
                }
                if (Input.GetKeyDown(KeyCode.JoystickButton9))
                {
                    float now = Time.realtimeSinceStartup;
                    float gap = now - _lastRStickClick; _lastRStickClick = now;
                    if (gap < 0.4f) { GhostReplay.Toggle(); GhostPage.RefreshAll(); _lastRStickClick = -999f; }
                    else { _pendingRStickSave = true; _rStickSaveTime = now + 0.4f; }
                }
                if (_pendingRStickSave && Time.realtimeSinceStartup >= _rStickSaveTime)
                {
                    _pendingRStickSave = false;
                    if (GhostReplay.IsRecording && GhostReplay.RecordedFrames >= 30) { GhostReplay.SaveRun(); GhostPage.RefreshAll(); }
                }
                if (!UI.BindsPage.IsListening)
                {
                    bool menuKey = KeyBindManager.CheckMenuOpenPressed();
                    if (!menuKey && Input.GetKeyDown(KeyCode.F6)
                        && !KeyBindManager.IsKeyBoundToMod(KeyCode.F6))
                        menuKey = true;
                    if (menuKey) MenuUI.ToggleMenu();
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("ToggleMenu: " + ex.Message); Telemetry.ReportErrorAsync(ex, "ToggleMenu"); }
            try { UI.BindsPage.CheckController(); } catch (System.Exception ex) { MelonLogger.Error("BindsPage.CheckController: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BindsPage.CheckController"); }

            try { SceneDumper.CheckHotkey(); } catch (System.Exception ex) { MelonLogger.Error("SceneDumper: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SceneDumper"); }
            try { SpeedWatcher.CheckHotkey(); } catch (System.Exception ex) { MelonLogger.Error("SpeedWatcher: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SpeedWatcher"); }
            try { TopSpeed.Tick(); } catch (System.Exception ex) { MelonLogger.Error("TopSpeed.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "TopSpeed"); }
            try { ESP.Tick(); } catch (System.Exception ex) { MelonLogger.Error("ESP.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "ESP"); }
            try { TrickMultiplier.Tick(); } catch (System.Exception ex) { MelonLogger.Error("TrickMultiplier.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "TrickMultiplier"); }
            try { ScreenshotMode.Tick(); } catch (System.Exception ex) { MelonLogger.Error("ScreenshotMode.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "ScreenshotMode"); }
            try { SessionTrackers.Tick(); } catch (System.Exception ex) { MelonLogger.Error("SessionTrackers.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SessionTrackers"); }
            try { MenuWindow.TickLive(); } catch (System.Exception ex) { MelonLogger.Error("MenuWindow.TickLive: " + ex.Message); Telemetry.ReportErrorAsync(ex, "MenuWindow"); }
            try { MirrorMode.Tick(); } catch (System.Exception ex) { MelonLogger.Error("MirrorMode.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "MirrorMode"); }
            try { FlyMode.Tick(); } catch (System.Exception ex) { MelonLogger.Error("FlyMode.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "FlyMode"); }
            try { DrunkMode.Tick(); } catch (System.Exception ex) { MelonLogger.Error("DrunkMode.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "DrunkMode"); }
            try { SpectateMode.Tick(); } catch (System.Exception ex) { MelonLogger.Error("SpectateMode.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SpectateMode"); }
            try { Trees.Tick(); } catch (System.Exception ex) { MelonLogger.Error("Trees.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Trees"); }
            try { BouncyBike.Tick(); } catch (System.Exception ex) { MelonLogger.Error("BouncyBike.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BouncyBike"); }
            try { OutfitPage.Tick(); } catch (System.Exception ex) { MelonLogger.Error("OutfitPage.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "OutfitPage"); }
            try { ChatPage.Tick(); } catch (System.Exception ex) { LogUiTickError("ChatPage.Tick", ex); }
            try { EspPage.Tick(); } catch (System.Exception ex) { LogUiTickError("EspPage.Tick", ex); }
            try { InfoPage.Tick(); } catch (System.Exception ex) { LogUiTickError("InfoPage.Tick", ex); }
            try { FavsPage.Tick(); } catch (System.Exception ex) { LogUiTickError("FavsPage.Tick", ex); }
            if (!OutfitPage.IsRenaming && !ChatPage.IsChatFocused && !MapPage.IsSeedFocused && !ModesPage.IsTAInputFocused && !UI.SearchPage.IsQueryFocused)
                try { SessionTrackers.CheckpointTick(); } catch (System.Exception ex) { MelonLogger.Error("SessionTrackers.CheckpointTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SessionTrackers"); }
            try { ModesPage.Tick(); } catch (System.Exception ex) { MelonLogger.Error("ModesPage.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "ModesPage"); }
            try { AvalancheMode.Tick(); } catch (System.Exception ex) { MelonLogger.Error("AvalancheMode.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "AvalancheMode"); }
            try { LavaRising.Tick(); } catch (System.Exception ex) { MelonLogger.Error("LavaRising.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "LavaRising"); }
            try { PoliceChaseMode.Tick(); } catch (System.Exception ex) { MelonLogger.Error("PoliceChaseMode.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "PoliceChaseMode"); }
            try { TrickAttackMode.Tick(); } catch (System.Exception ex) { MelonLogger.Error("TrickAttackMode.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "TrickAttackMode"); }
            try { BoulderDodgeMode.Tick(); } catch (System.Exception ex) { MelonLogger.Error("BoulderDodgeMode.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BoulderDodgeMode"); }
            try { ObjectPlacer.Tick(); } catch (System.Exception ex) { MelonLogger.Error("ObjectPlacer.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "ObjectPlacer"); }
            if (ObjectPlacer.ConsumeCancelPending())
            {
                try { ObjectPlacerPage.RefreshAll(); } catch { }
                if (ObjectPlacer.AutoCloseMenu && MenuUI.IsOpen)
                {
                    try { MenuUI.ToggleMenu(); } catch { }
                }
            }
            try { SurvivalMode.Tick(); } catch (System.Exception ex) { MelonLogger.Error("SurvivalMode.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SurvivalMode"); }
            try { GhostReplay.Tick(); } catch (System.Exception ex) { MelonLogger.Error("GhostReplay.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "GhostReplay"); }
            try { MapChanger.Tick(); } catch (System.Exception ex) { MelonLogger.Error("MapChanger.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "MapChanger"); }
            try { MapPage.SeedTick(); } catch (System.Exception ex) { LogUiTickError("MapPage.SeedTick", ex); }
            try { UI.SearchPage.SearchTick(); } catch { }
            try { GhostPage.Tick(); } catch (System.Exception ex) { MelonLogger.Error("GhostPage.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "GhostPage"); }
            try { SlowMoOnBail.Tick(); } catch (System.Exception ex) { MelonLogger.Error("SlowMoOnBail.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SlowMoOnBail"); }
            try { BlackDeath.Tick(); } catch (System.Exception ex) { MelonLogger.Error("BlackDeath.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BlackDeath"); }
            try { MultiplayerMenuGuard.Tick(); } catch (System.Exception ex) { MelonLogger.Error("MultiplayerMenuGuard.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "MultiplayerMenuGuard"); }
            try { ChaosMode.Tick(); } catch (System.Exception ex) { MelonLogger.Error("ChaosMode.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "ChaosMode"); }
            try { RandomWeatherRoulette.Tick(); } catch (System.Exception ex) { MelonLogger.Error("RandomWeatherRoulette.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "RandomWeatherRoulette"); }
            try { DiscoMode.Tick(); } catch (System.Exception ex) { MelonLogger.Error("DiscoMode.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "DiscoMode"); }
            try { BikeTorch.TickDisco(); } catch (System.Exception ex) { MelonLogger.Error("BikeTorch.TickDisco: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BikeTorch.TickDisco"); }
            try { ModDetection.Tick(); } catch (System.Exception ex) { MelonLogger.Error("ModDetection.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "ModDetection"); }
            try { ModChat.Tick(); } catch (System.Exception ex) { MelonLogger.Error("ModChat.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "ModChat"); }
            if (!OutfitPage.IsRenaming && !ChatPage.IsChatFocused && !MapPage.IsSeedFocused && !ModesPage.IsTAInputFocused && !UI.BindsPage.IsListening && !UI.SearchPage.IsQueryFocused)
                try { KeyBindManager.CheckAll(); } catch (System.Exception ex) { MelonLogger.Error("KeyBindManager.CheckAll: " + ex.Message); Telemetry.ReportErrorAsync(ex, "KeyBindManager"); }
        }

        public override void OnFixedUpdate()
        {
            try { AvalancheMode.FixedTick(); } catch (System.Exception ex) { MelonLogger.Error("AvalancheMode.FixedTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "AvalancheMode"); }
            try { LavaRising.FixedTick(); } catch (System.Exception ex) { MelonLogger.Error("LavaRising.FixedTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "LavaRising"); }
            try { EarthquakeMode.FixedTick(); } catch (System.Exception ex) { MelonLogger.Error("EarthquakeMode.FixedTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "EarthquakeMode"); }
            try { PoliceChaseMode.FixedTick(); } catch (System.Exception ex) { MelonLogger.Error("PoliceChaseMode.FixedTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "PoliceChaseMode"); }
            try { StickyTyres.FixedTick(); } catch (System.Exception ex) { MelonLogger.Error("StickyTyres.FixedTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "StickyTyres"); }
            try { SpiderBike.FixedTick(); } catch (System.Exception ex) { MelonLogger.Error("SpiderBike.FixedTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SpiderBike"); }
            try { AirControl.FixedTick(); } catch (System.Exception ex) { MelonLogger.Error("AirControl.FixedTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "AirControl"); }
            try { CenterOfMass.FixedTick(); } catch (System.Exception ex) { MelonLogger.Error("CenterOfMass.FixedTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "CenterOfMass"); }
            try { BoulderDodgeMode.FixedTick(); } catch (System.Exception ex) { MelonLogger.Error("BoulderDodgeMode.FixedTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BoulderDodgeMode"); }
            try { BikeDamage.FixedTick(); } catch (System.Exception ex) { MelonLogger.Error("BikeDamage.FixedTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BikeDamage"); }
            try { HoverMode.FixedTick(); } catch (System.Exception ex) { MelonLogger.Error("HoverMode.FixedTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "HoverMode"); }
            try { TurboWind.FixedTick(); } catch (System.Exception ex) { MelonLogger.Error("TurboWind.FixedTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "TurboWind"); }
        }

        public override void OnLateUpdate()
        {
            try { FOV.Apply(); } catch (System.Exception ex) { MelonLogger.Error("FOV.Apply: " + ex.Message); Telemetry.ReportErrorAsync(ex, "FOV"); }
            if (SkyColours.StormEnabled)
            {
                try { SkyColours.Tick(); } catch (System.Exception ex) { MelonLogger.Error("SkyColours.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SkyColours"); }
            }
            try { DrunkMode.LateTick(); } catch (System.Exception ex) { MelonLogger.Error("DrunkMode.LateTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "DrunkMode"); }
            try { SpectateMode.LateTick(); } catch (System.Exception ex) { MelonLogger.Error("SpectateMode.LateTick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SpectateMode"); }
            try { WheelSize.Tick(); } catch (System.Exception ex) { MelonLogger.Error("WheelSize.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "WheelSize"); }
            try { WideTyres.Tick(); } catch (System.Exception ex) { MelonLogger.Error("WideTyres.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "WideTyres"); }
            try { BikeDamage.Tick(); } catch (System.Exception ex) { MelonLogger.Error("BikeDamage.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BikeDamage"); }
            try { BigHeadMode.Tick(); } catch (System.Exception ex) { MelonLogger.Error("BigHeadMode.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BigHeadMode"); }
            try { TrailPainter.Tick(); } catch (System.Exception ex) { MelonLogger.Error("TrailPainter.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "TrailPainter"); }
            try { BlizzardDial.Tick(); } catch (System.Exception ex) { MelonLogger.Error("BlizzardDial.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BlizzardDial"); }
            try { TurboWind.Tick(); } catch (System.Exception ex) { MelonLogger.Error("TurboWind.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "TurboWind"); }
            try { RandomBikeSwitch.Tick(); } catch (System.Exception ex) { MelonLogger.Error("RandomBikeSwitch.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "RandomBikeSwitch"); }
            try { Acceleration.Tick(); } catch (System.Exception ex) { MelonLogger.Error("Acceleration.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "Acceleration"); }
            try { MaxSpeedMultiplier.Tick(); } catch (System.Exception ex) { MelonLogger.Error("MaxSpeedMultiplier.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "MaxSpeedMultiplier"); }
            try { LandingImpact.Tick(); } catch (System.Exception ex) { MelonLogger.Error("LandingImpact.Tick: " + ex.Message); Telemetry.ReportErrorAsync(ex, "LandingImpact"); }
        }

        private static void LogUiTickError(string where, System.Exception ex)
        {
            if (ex is MissingReferenceException || ex is System.NullReferenceException) return;
            string msg = where + ": " + ex.GetType().Name
                + (string.IsNullOrEmpty(ex.Message) ? "" : " - " + ex.Message);
            MelonLogger.Error(msg);
            Telemetry.ReportErrorAsync(ex, where);
        }

        public override void OnGUI()
        {
            try { ESP.OnGUI(); } catch (System.Exception ex) { MelonLogger.Error("ESP.OnGUI: " + ex.Message); Telemetry.ReportErrorAsync(ex, "ESP"); }
            try { GhostHUD.Draw(); } catch (System.Exception ex) { MelonLogger.Error("GhostHUD.Draw: " + ex.Message); Telemetry.ReportErrorAsync(ex, "GhostHUD"); }
            try { PoliceHUD.Draw(); } catch (System.Exception ex) { MelonLogger.Error("PoliceHUD.Draw: " + ex.Message); Telemetry.ReportErrorAsync(ex, "PoliceHUD"); }
            try { LavaRisingHUD.Draw(); } catch (System.Exception ex) { MelonLogger.Error("LavaRisingHUD.Draw: " + ex.Message); Telemetry.ReportErrorAsync(ex, "LavaRisingHUD"); }
            try { TrickAttackHUD.Draw(); } catch (System.Exception ex) { MelonLogger.Error("TrickAttackHUD.Draw: " + ex.Message); Telemetry.ReportErrorAsync(ex, "TrickAttackHUD"); }
            try { SurvivalHUD.Draw(); } catch (System.Exception ex) { MelonLogger.Error("SurvivalHUD.Draw: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SurvivalHUD"); }
            try { SessionHUD.Draw(); } catch (System.Exception ex) { MelonLogger.Error("SessionHUD.Draw: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SessionHUD"); }
            try { ChatHUD.Draw(); } catch (System.Exception ex) { MelonLogger.Error("ChatHUD.Draw: " + ex.Message); Telemetry.ReportErrorAsync(ex, "ChatHUD"); }
            try { SuspensionHUD.OnGUI(); } catch (System.Exception ex) { MelonLogger.Error("SuspensionHUD.OnGUI: " + ex.Message); Telemetry.ReportErrorAsync(ex, "SuspensionHUD"); }
            try { BrakeFade.OnGUI(); } catch (System.Exception ex) { MelonLogger.Error("BrakeFade.OnGUI: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BrakeFade"); }
            try { WheelieHUD.OnGUI(); } catch (System.Exception ex) { MelonLogger.Error("WheelieHUD.OnGUI: " + ex.Message); Telemetry.ReportErrorAsync(ex, "WheelieHUD"); }
            try { UI.BindsPage.OnGUI(); } catch { }
            try { BlackDeath.Draw(); } catch (System.Exception ex) { MelonLogger.Error("BlackDeath.Draw: " + ex.Message); Telemetry.ReportErrorAsync(ex, "BlackDeath"); }
        }

        public override void OnApplicationQuit()
        {
            MenuUI.RestoreCursor();
            SlowMotion.Reset(); QuickBrake.Reset(); QuickBrake_Patch.ClearCache();
            MelonLogger.Msg("OnApplicationQuit");
        }
    }
}

