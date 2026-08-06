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
        public const string Version = "1.4.0";
        public const string DownloadLink = null;
    }

    public class DescendersModMenu : MelonMod
    {
        private HarmonyLib.Harmony harmony;
        private float _lastRStickClick = -999f;
        private bool _pendingRStickSave = false;
        private float _rStickSaveTime = 0f;

        // == Deferred mod reapply after map change ==
        private bool _pendingReapply;
        private bool _pendingAutoLoad = true; // fires once when first Player_Human appears
        private bool _reapplyFlyMode, _reapplyDrunkMode, _reapplyMirrorMode, _reapplyWideTyres;
        private int _reapplyWideTyresLevel;
        private bool _reapplyFov, _reapplySpeedrunTimer, _reapplyAcceleration, _reapplyMaxSpeed;
        private bool _reapplyLandingImpact;
        private bool _reapplyMoveSpin, _reapplyMoveHop, _reapplyMoveWheelie, _reapplyMoveLean;
        private bool _reapplyBikeTorch; private int _reapplyBikeTorchIntensity;
        private bool _reapplyCameraShake; private int _reapplyCameraShakeLevel;
        private bool _reapplyNearMiss; private int _reapplyNearMissLevel;
        private bool _reapplyExplodingProps;
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
            MelonLogger.Msg("OnApplicationStart");
            try { CodeStage.AntiCheat.Detectors.InjectionDetector.Dispose(); MelonLogger.Msg("AntiCheat disposed: InjectionDetector"); }
            catch (System.Exception ex) { MelonLogger.Warning("AntiCheat dispose failed (InjectionDetector): " + ex.Message); }
            try { CodeStage.AntiCheat.Detectors.ObscuredCheatingDetector.Dispose(); MelonLogger.Msg("AntiCheat disposed: ObscuredCheatingDetector"); }
            catch (System.Exception ex) { MelonLogger.Warning("AntiCheat dispose failed (ObscuredCheatingDetector): " + ex.Message); }
            try { CodeStage.AntiCheat.Detectors.SpeedHackDetector.Dispose(); MelonLogger.Msg("AntiCheat disposed: SpeedHackDetector"); }
            catch (System.Exception ex) { MelonLogger.Warning("AntiCheat dispose failed (SpeedHackDetector): " + ex.Message); }
            try { CodeStage.AntiCheat.Detectors.TimeCheatingDetector.Dispose(); MelonLogger.Msg("AntiCheat disposed: TimeCheatingDetector"); }
            catch (System.Exception ex) { MelonLogger.Warning("AntiCheat dispose failed (TimeCheatingDetector): " + ex.Message); }
            try { CodeStage.AntiCheat.Detectors.WallHackDetector.Dispose(); MelonLogger.Msg("AntiCheat disposed: WallHackDetector"); }
            catch (System.Exception ex) { MelonLogger.Warning("AntiCheat dispose failed (WallHackDetector): " + ex.Message); }

            harmony = new HarmonyLib.Harmony("DescendersModMenu.Patches");
            try { harmony.PatchAll(); MelonLogger.Msg("Harmony patches applied."); DiagnosticsManager.Report("Harmony", true); }
            catch (System.Exception ex) { MelonLogger.Error("PatchAll failed: " + ex.Message); DiagnosticsManager.Report("Harmony", false, ex.Message); }
            try { NoSpeedCap.ApplyPatch(harmony); DiagnosticsManager.Report("NoSpeedCap", true); }
            catch (System.Exception ex) { MelonLogger.Error("NoSpeedCap.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("NoSpeedCap", false, ex.Message); }
            try { NoSpeedCap.ApplyVCPatch(harmony); DiagnosticsManager.Report("NoSpeedCap (VC)", true); }
            catch (System.Exception ex) { MelonLogger.Error("NoSpeedCap.ApplyVCPatch: " + ex.Message); DiagnosticsManager.Report("NoSpeedCap (VC)", false, ex.Message); }
            try { QuickBrake.ApplyPatch(harmony); DiagnosticsManager.Report("QuickBrake", true); }
            catch (System.Exception ex) { MelonLogger.Error("QuickBrake.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("QuickBrake", false, ex.Message); }
            try { CutBrakes.ApplyPatch(harmony); DiagnosticsManager.Report("CutBrakes", true); }
            catch (System.Exception ex) { MelonLogger.Error("CutBrakes.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("CutBrakes", false, ex.Message); }
            try { BrakeFade.ApplyPatch(harmony); DiagnosticsManager.Report("BrakeFade", true); }
            catch (System.Exception ex) { MelonLogger.Error("BrakeFade.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("BrakeFade", false, ex.Message); }
            try { BikeDamage.ApplyPatch(harmony); DiagnosticsManager.Report("BikeDamage", true); }
            catch (System.Exception ex) { MelonLogger.Error("BikeDamage.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("BikeDamage", false, ex.Message); }
            try { ReverseSteering.ApplyPatch(harmony); DiagnosticsManager.Report("ReverseSteering", true); }
            catch (System.Exception ex) { MelonLogger.Error("ReverseSteering.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("ReverseSteering", false, ex.Message); }
            try { AutoBalance.ApplyPatch(harmony); DiagnosticsManager.Report("AutoBalance", true); }
            catch (System.Exception ex) { MelonLogger.Error("AutoBalance.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("AutoBalance", false, ex.Message); }
            try { IceMode.ApplyPatch(harmony); DiagnosticsManager.Report("IceMode", true); }
            catch (System.Exception ex) { MelonLogger.Error("IceMode.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("IceMode", false, ex.Message); }
            try { TyrePressure.ApplyPatch(harmony); DiagnosticsManager.Report("TyrePressure", true); }
            catch (System.Exception ex) { MelonLogger.Error("TyrePressure.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("TyrePressure", false, ex.Message); }
            try { SkyColours.ApplyPatch(harmony); }
            catch (System.Exception ex) { MelonLogger.Error("SkyColours.ApplyPatch: " + ex.Message); }
            try { DrunkMode.ApplyPatch(harmony); DiagnosticsManager.Report("DrunkMode", true); }
            catch (System.Exception ex) { MelonLogger.Error("DrunkMode.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("DrunkMode", false, ex.Message); }
            try { SessionTrackers.ApplyBailPatch(harmony); DiagnosticsManager.Report("BailCounter", true); }
            catch (System.Exception ex) { MelonLogger.Error("BailPatch: " + ex.Message); DiagnosticsManager.Report("BailCounter", false, ex.Message); }
            try { SessionTrackers.ApplyCheckpointPatch(harmony); DiagnosticsManager.Report("CheckpointCounter", true); }
            catch (System.Exception ex) { MelonLogger.Error("CheckpointPatch: " + ex.Message); DiagnosticsManager.Report("CheckpointCounter", false, ex.Message); }
            try { GhostReplay.ApplyPatch(harmony); DiagnosticsManager.Report("GhostReplay", true); }
            catch (System.Exception ex) { MelonLogger.Error("GhostReplay.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("GhostReplay", false, ex.Message); }
            try { GameModifierMods.ApplyNoSpeedWobblesPatch(harmony); DiagnosticsManager.Report("NoSpeedWobbles", true); }
            catch (System.Exception ex) { MelonLogger.Error("NoSpeedWobbles patch: " + ex.Message); DiagnosticsManager.Report("NoSpeedWobbles", false, ex.Message); }
            try { WheelieAngleLimit.ApplyPatch(harmony); DiagnosticsManager.Report("WheelieAngleLimit", true); }
            catch (System.Exception ex) { MelonLogger.Error("WheelieAngleLimit patch: " + ex.Message); DiagnosticsManager.Report("WheelieAngleLimit", false, ex.Message); }
            try { TrickSetSwap.ApplyPatch(harmony); DiagnosticsManager.Report("TrickSetSwap", true); }
            catch (System.Exception ex) { MelonLogger.Error("TrickSetSwap patch: " + ex.Message); DiagnosticsManager.Report("TrickSetSwap", false, ex.Message); }
            try { MapChanger.ApplyPatch(harmony); DiagnosticsManager.Report("MapChanger", true); }
            catch (System.Exception ex) { MelonLogger.Warning("MapChanger.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("MapChanger", false, ex.Message); }
            try { NoBail.ApplyPatch(harmony); }
            catch (System.Exception ex) { MelonLogger.Error("NoBail.ApplyPatch: " + ex.Message); }
            try { SlowMoOnBail.ApplyPatch(harmony); }
            catch (System.Exception ex) { MelonLogger.Error("SlowMoOnBail.ApplyPatch: " + ex.Message); }
            try { CompassAlwaysOn.ApplyPatch(harmony); DiagnosticsManager.Report("CompassAlwaysOn", true); }
            catch (System.Exception ex) { MelonLogger.Error("CompassAlwaysOn.ApplyPatch: " + ex.Message); DiagnosticsManager.Report("CompassAlwaysOn", false, ex.Message); }
            try { OutfitPresets.Init(); }
            catch (System.Exception ex) { MelonLogger.Error("OutfitPresets.Init: " + ex.Message); }
            try { ModChat.Init(); }
            catch (System.Exception ex) { MelonLogger.Error("ModChat.Init: " + ex.Message); }
        }

        public override void OnLateInitializeMelon()
        {
            MelonLogger.Msg("OnApplicationLateStart");
            DiagnosticsManager.Report("SlowMotion", true); DiagnosticsManager.Report("FOV", true);
            DiagnosticsManager.Report("ESP", true); DiagnosticsManager.Report("NoBail", true);
            DiagnosticsManager.Report("Acceleration", true); DiagnosticsManager.Report("MaxSpeed", true);
            DiagnosticsManager.Report("BikeSwitcher", true); DiagnosticsManager.Report("TeleportToPlayer", true);
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
            DiagnosticsManager.Report("WideTyres", true); DiagnosticsManager.Report("StickyTyres", true);
            DiagnosticsManager.Report("FlyMode", true); DiagnosticsManager.Report("MirrorMode", true);
            DiagnosticsManager.Report("SpeedrunTimer", true); DiagnosticsManager.Report("SlowMoOnBail", true);
            DiagnosticsManager.Report("AirControl", true); DiagnosticsManager.Report("ModDetection", true);
            TopSpeed.Load(); TopSpeed.StartTracking();

            // Load favourites configuration
            try { UI.FavouritesManager.LoadFromFile(); }
            catch (System.Exception ex) { MelonLogger.Warning("FavouritesManager.LoadFromFile: " + ex.Message); }

            // Load key bindings
            try { KeyBindManager.LoadBindings(); MelonLogger.Msg("KeyBindManager.LoadBindings OK."); }
            catch (System.Exception ex) { MelonLogger.Warning("KeyBindManager.LoadBindings: " + ex.Message); }

            // Load colour scheme (applies to UITheme before the menu is ever built)
            try { UI.ColorSchemeManager.LoadAndApply(); MelonLogger.Msg("ColorSchemeManager.LoadAndApply OK."); }
            catch (System.Exception ex) { MelonLogger.Warning("ColorSchemeManager.LoadAndApply: " + ex.Message); }

            // Check for updates on a background thread
            try { UpdateChecker.CheckAsync(); } catch { }
            try { SteamPlayerCount.FetchAsync(); } catch { }
        }

        public override void OnSceneWasLoaded(int buildindex, string sceneName)
        {
            MelonLogger.Msg("OnSceneWasLoaded: " + buildindex + " | " + sceneName);
            GhostReplay.OnSceneLoaded();
        }

        public override void OnSceneWasInitialized(int buildindex, string sceneName)
        {
            MelonLogger.Msg("OnSceneWasInitialized: " + buildindex + " | " + sceneName);
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
        }

        // ================================================================
        //  SCENE TRANSITION: Snapshot -> Reset -> Restore
        // ================================================================
        //  PERSISTS across scenes: everything except the items below
        //  ALWAYS RESETS (never restored):
        //    Graphics tab, Sky section, Modes, Ghost Replay, ESP
        // ================================================================
        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            MelonLogger.Msg("OnSceneWasUnloaded: " + buildIndex + " | " + sceneName);

            // MenuWindow.CreateMenu() builds a plain GameObject with no
            // DontDestroyOnLoad, so Unity destroys the whole menu (and every
            // star button) on scene load. FavouritesManager's _starButtons
            // dict doesn't find out until RefreshAllStars() hits a dead ref
            // and self-heals one warning at a time — clear it here instead so
            // there's nothing stale left for the post-reapply RefreshAll() to
            // trip over. CreateMenu() re-populates it fresh next time the
            // menu is actually built, so this is a pure no-op if it isn't.
            try { FavouritesManager.ClearStarButtons(); } catch { }
            try { CompassAlwaysOn.ClearCache(); } catch { }

            // -- GUARD: intermediate scene (e.g. EmptyScene) --
            // If a reapply is already pending, this is a transition scene.
            // The snapshot from the REAL scene is still valid — don't overwrite it.
            if (_pendingReapply)
            {
                MelonLogger.Msg("[Reapply] Skipping intermediate scene (" + sceneName + ")");
                return;
            }

            // == SNAPSHOT: capture all mods that should persist ==

            // Immediate (Harmony / pure flags)
            bool wasSlowMotion = SlowMotion.Enabled;
            int slowMotionLv = SlowMotion.Level;
            bool wasCutBrakes = CutBrakes.Enabled;
            bool wasReverseSteering = ReverseSteering.Enabled;
            bool wasAutoBalance = AutoBalance.Enabled;
            int autoBalanceLv = AutoBalance.StrengthLevel;
            bool wasQuickBrake = QuickBrake.Enabled;
            bool wasWheelieAngle = WheelieAngleLimit.Enabled;
            bool wasNoSpeedWobbles = GameModifierMods.NoSpeedWobblesEnabled;
            bool wasSlowMoOnBail = SlowMoOnBail.Enabled;
            bool wasIceMode = IceMode.Enabled;
            bool wasTyrePressure = TyrePressure.Enabled;
            int tyrePressureLv = TyrePressure.Level;
            bool wasBikeDamage = BikeDamage.Enabled;
            bool wasHeadlightsOnly = HeadlightsOnly.Enabled;
            bool wasUIRemover = UIRemover.Enabled;
            bool wasInstantRespawn = InstantRespawn.Enabled;
            bool wasStickyTyres = StickyTyres.Enabled;
            bool wasAirControl = AirControl.Enabled;
            bool wasNoBail = NoBail.Enabled;
            int gravLevel = Gravity.Level;
            bool gravNeed = gravLevel != 5;

            // Deferred (need Player_Human)
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
            bool wasSuspHUD = SuspensionHUD.Enabled;
            bool wasBrakeFade = BrakeFade.Enabled;
            int brakeBalanceLv = BrakeFade.BalanceLevel;

            // Log
            MelonLogger.Msg("[Reapply] === SNAPSHOT (" + sceneName + ") ===");
            if (wasSlowMotion) MelonLogger.Msg("[Reapply]   SlowMotion lv=" + slowMotionLv);
            if (wasCutBrakes) MelonLogger.Msg("[Reapply]   CutBrakes");
            if (wasReverseSteering) MelonLogger.Msg("[Reapply]   ReverseSteering");
            if (wasAutoBalance) MelonLogger.Msg("[Reapply]   AutoBalance lv=" + autoBalanceLv);
            if (wasQuickBrake) MelonLogger.Msg("[Reapply]   QuickBrake");
            if (wasWheelieAngle) MelonLogger.Msg("[Reapply]   WheelieAngle");
            if (wasNoSpeedWobbles) MelonLogger.Msg("[Reapply]   NoSpeedWobbles");
            if (wasSlowMoOnBail) MelonLogger.Msg("[Reapply]   SlowMoOnBail");
            if (wasIceMode) MelonLogger.Msg("[Reapply]   IceMode");
            if (wasTyrePressure) MelonLogger.Msg("[Reapply]   TyrePressure lv=" + tyrePressureLv);
            if (wasInstantRespawn) MelonLogger.Msg("[Reapply]   InstantRespawn");
            if (wasStickyTyres) MelonLogger.Msg("[Reapply]   StickyTyres");
            if (wasAirControl) MelonLogger.Msg("[Reapply]   AirControl");
            if (wasNoBail) MelonLogger.Msg("[Reapply]   NoBail");
            if (gravNeed) MelonLogger.Msg("[Reapply]   Gravity lv=" + gravLevel);
            if (wasFlyMode) MelonLogger.Msg("[Reapply]   FlyMode");
            if (wasDrunkMode) MelonLogger.Msg("[Reapply]   DrunkMode");
            if (wasMirrorMode) MelonLogger.Msg("[Reapply]   MirrorMode");
            if (wasWideTyres) MelonLogger.Msg("[Reapply]   WideTyres lv=" + wideTyresLevel);
            if (wasFov) MelonLogger.Msg("[Reapply]   FOV");
            if (wasSpeedrunTimer) MelonLogger.Msg("[Reapply]   SpeedrunTimer");
            if (wasAcceleration) MelonLogger.Msg("[Reapply]   Acceleration");
            if (wasMaxSpeed) MelonLogger.Msg("[Reapply]   MaxSpeed");
            if (wasLandingImpact) MelonLogger.Msg("[Reapply]   LandingImpact");
            if (wasMoveSpin) MelonLogger.Msg("[Reapply]   Spin");
            if (wasMoveHop) MelonLogger.Msg("[Reapply]   Hop");
            if (wasMoveWheelie) MelonLogger.Msg("[Reapply]   Wheelie");
            if (wasMoveLean) MelonLogger.Msg("[Reapply]   Lean");
            if (wasBikeTorch) MelonLogger.Msg("[Reapply]   BikeTorch int=" + torchInt);
            if (wasCamShake) MelonLogger.Msg("[Reapply]   CameraShake lv=" + camShakeLv);
            if (wasNearMiss) MelonLogger.Msg("[Reapply]   NearMiss lv=" + nearMissLv);
            if (wasExploding) MelonLogger.Msg("[Reapply]   ExplodingProps");
            if (comNeed) MelonLogger.Msg("[Reapply]   COM " + cx + "/" + cy + "/" + cz);
            if (suspNeed) MelonLogger.Msg("[Reapply]   Susp " + sT + "/" + sS + "/" + sD);
            if (wasNoSpeedCap) MelonLogger.Msg("[Reapply]   NoSpeedCap");
            if (bikeScaleNeed) MelonLogger.Msg("[Reapply]   BikeScale=" + bikeScale);
            if (playerScaleNeed) MelonLogger.Msg("[Reapply]   PlayerScale=" + playerScale);
            if (wasInvisBike) MelonLogger.Msg("[Reapply]   InvisibleBike");
            if (wasInvisPlayer) MelonLogger.Msg("[Reapply]   InvisiblePlayer");
            if (wasWheelSize) MelonLogger.Msg("[Reapply]   WheelSize level=" + wheelSizeLevel + " mode=" + wheelSizeMode);
            if (wasIndividualWheel) MelonLogger.Msg("[Reapply]   IndividualWheel F=" + frontWheelLv + " R=" + rearWheelLv);
            if (wasSuspHUD) MelonLogger.Msg("[Reapply]   SuspensionHUD");
            if (wasBrakeFade) MelonLogger.Msg("[Reapply]   BrakeFade balance=" + brakeBalanceLv);

            // == RESET everything ==
            SlowMotion.Reset(); QuickBrake.Reset(); QuickBrake_Patch.ClearCache();
            CutBrakes.Reset(); ReverseSteering.Reset(); AutoBalance.Reset();
            WideTyres.Reset(); IceMode.Reset(); TyrePressure.Reset(); InstantRespawn.Reset(); BikeDamage.Reset(); HeadlightsOnly.Reset(); UIRemover.Reset(); ScreenshotMode.Reset(); SpeedrunTimer.Reset();
            GameModifierMods.NoSpeedWobblesReset();
            MirrorMode.Reset(); FlyMode.Reset(); DrunkMode.Reset();
            OutfitPresets.Reset(); ModChat.Reset(); SlowMoOnBail.Reset();
            StickyTyres.Reset(); WheelieAngleLimit.Reset(); AirControl.Reset();
            CenterOfMass.Reset();
            FOV.Reset(); Acceleration.Reset(); MaxSpeedMultiplier.Reset();
            Movement.Reset(); LandingImpact.Reset();
            NoBail.ClearCache(); WheelSize.Reset();
            if (NoSpeedCap.Enabled) NoSpeedCap.Toggle(); // Reset to OFF before immediate restore
            if (NoBail.Enabled) NoBail.Toggle(); // Reset to OFF before immediate restore
            BikeTorch.Reset(); CameraShake.Reset(); NearMissSensitivity.Reset();
            // Always-reset (NOT restored):
            SkyColours.Reset(); GraphicsSettings.Reset();
            AvalancheMode.Reset(); GhostReplay.Reset();
            EarthquakeMode.Reset(); PoliceChaseMode.Reset();
            TrickAttackMode.Reset(); BoulderDodgeMode.Reset();
            SurvivalMode.Reset();
            SessionTrackers.Reset(); ExplodingProps.Reset();
            SuspensionHUD.ClearCache();
            BrakeFade.ClearCache(); BrakeFade_Patch.ClearCache();
            TrickMultiplier.Reset();
            BikeDamage.ClearBoneCache();
            HeadlightsOnly.ClearCache();
            UIRemover.ClearCache();
            TrailPainter.ClearCache();
            BigHeadMode.ClearCache();
            // TopSpeed used to call Reset() here, which zeroes SessionTopSpeed AND
            // saves that 0 straight to TopSpeed.txt on disk - permanently wiping the
            // persisted best-speed record on every single level transition within a
            // career session (confirmed 2026-08-04: log shows "Loaded: 0.0 km/h" at
            // boot, and Reset() firing every scene change explains why it can never
            // climb past whatever was tracked on the very first level of a session).
            // ClearCache() only refreshes the stale Player_Human/Rigidbody refs (which
            // do need refreshing every scene - they point at destroyed objects
            // otherwise) without touching the tracked value at all.
            TopSpeed.ClearCache();
            if (ESP.Enabled) ESP.Toggle();

            // == RESTORE IMMEDIATE (Harmony patches) ==
            if (wasSlowMotion) { SlowMotion.SetLevel(slowMotionLv); SlowMotion.Toggle(); MelonLogger.Msg("[Reapply] IMM SlowMotion -> " + SlowMotion.Enabled + " lv=" + slowMotionLv); }
            if (wasCutBrakes) { CutBrakes.Toggle(); MelonLogger.Msg("[Reapply] IMM CutBrakes -> " + CutBrakes.Enabled); }
            if (wasReverseSteering) { ReverseSteering.Toggle(); MelonLogger.Msg("[Reapply] IMM ReverseSteering -> " + ReverseSteering.Enabled); }
            if (wasAutoBalance) { AutoBalance.SetStrengthLevel(autoBalanceLv); AutoBalance.Toggle(); MelonLogger.Msg("[Reapply] IMM AutoBalance -> " + AutoBalance.Enabled + " lv=" + autoBalanceLv); }
            if (wasQuickBrake) { QuickBrake.Toggle(); MelonLogger.Msg("[Reapply] IMM QuickBrake -> " + QuickBrake.Enabled); }
            if (wasWheelieAngle) { WheelieAngleLimit.Toggle(); MelonLogger.Msg("[Reapply] IMM WheelieAngle -> " + WheelieAngleLimit.Enabled); }
            if (wasNoSpeedWobbles) { GameModifierMods.NoSpeedWobblesToggle(); MelonLogger.Msg("[Reapply] IMM NoSpeedWobbles -> " + GameModifierMods.NoSpeedWobblesEnabled); }
            if (wasSlowMoOnBail) { SlowMoOnBail.Toggle(); MelonLogger.Msg("[Reapply] IMM SlowMoOnBail -> " + SlowMoOnBail.Enabled); }
            if (wasIceMode) { IceMode.Toggle(); MelonLogger.Msg("[Reapply] IMM IceMode -> " + IceMode.Enabled); }
            if (wasTyrePressure) { TyrePressure.SetLevel(tyrePressureLv); TyrePressure.Toggle(); MelonLogger.Msg("[Reapply] IMM TyrePressure -> " + TyrePressure.Enabled + " lv=" + tyrePressureLv); }
            if (wasBikeDamage) { BikeDamage.Toggle(); MelonLogger.Msg("[Reapply] IMM BikeDamage -> " + BikeDamage.Enabled); }
            if (wasHeadlightsOnly) { HeadlightsOnly.Toggle(); MelonLogger.Msg("[Reapply] IMM HeadlightsOnly -> " + HeadlightsOnly.Enabled); }
            if (wasUIRemover) { UIRemover.Toggle(); MelonLogger.Msg("[Reapply] IMM UIRemover -> " + UIRemover.Enabled); }
            if (wasInstantRespawn) { InstantRespawn.Toggle(); MelonLogger.Msg("[Reapply] IMM InstantRespawn -> " + InstantRespawn.Enabled); }
            if (wasStickyTyres) { StickyTyres.Toggle(); MelonLogger.Msg("[Reapply] IMM StickyTyres -> " + StickyTyres.Enabled); }
            if (wasAirControl) { AirControl.Toggle(); MelonLogger.Msg("[Reapply] IMM AirControl -> " + AirControl.Enabled); }
            if (wasNoSpeedCap) { NoSpeedCap.Toggle(); MelonLogger.Msg("[Reapply] IMM NoSpeedCap -> " + NoSpeedCap.Enabled); }
            if (wasNoBail) { NoBail.Toggle(); MelonLogger.Msg("[Reapply] IMM NoBail -> " + NoBail.Enabled); }
            if (gravNeed) { Gravity.SetLevel(gravLevel); Gravity.Apply(); MelonLogger.Msg("[Reapply] IMM Gravity lv=" + gravLevel); }
            MelonLogger.Msg("[Reapply] Immediate restores done.");

            // == QUEUE DEFERRED ==
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
            _reapplyCOMx = cx; _reapplyCOMy = cy; _reapplyCOMz = cz; _reapplyCOMNeeded = comNeed;
            _reapplySuspTravel = sT; _reapplySuspStiff = sS; _reapplySuspDamp = sD; _reapplySuspNeeded = suspNeed;
            _reapplyBikeScale = bikeScale; _reapplyBikeScaleNeeded = bikeScaleNeed;
            _reapplyPlayerScale = playerScale; _reapplyPlayerScaleNeeded = playerScaleNeed;
            _reapplyInvisibleBike = wasInvisBike;
            _reapplyInvisiblePlayer = wasInvisPlayer;
            _reapplyWheelSize = wasWheelSize; _reapplyWheelSizeMode = wheelSizeMode; _reapplyWheelSizeLevel = wheelSizeLevel;
            _reapplyIndividualWheel = wasIndividualWheel; _reapplyFrontWheelLevel = frontWheelLv; _reapplyRearWheelLevel = rearWheelLv;
            _reapplyBrakeFade = wasBrakeFade;

            if (_reapplyBrakeFade) { _reapplyBrakeFade = false; BrakeFade.SetBalanceLevel(brakeBalanceLv); BrakeFade.Toggle(); MelonLogger.Msg("[Reapply] IMM BrakeFade -> " + BrakeFade.Enabled); }

            _pendingReapply = wasFlyMode || wasDrunkMode || wasMirrorMode || wasWideTyres ||
                wasFov || wasSpeedrunTimer || wasAcceleration || wasMaxSpeed ||
                wasLandingImpact || wasMoveSpin || wasMoveHop || wasMoveWheelie || wasMoveLean ||
                wasBikeTorch || wasCamShake || wasNearMiss || wasExploding ||
                comNeed || suspNeed ||
                bikeScaleNeed || playerScaleNeed || wasInvisBike || wasInvisPlayer || wasWheelSize || wasIndividualWheel;

            if (_pendingReapply) MelonLogger.Msg("[Reapply] Deferred queued — waiting for Player_Human...");
            else MelonLogger.Msg("[Reapply] No deferred mods to reapply.");
        }

        public override void OnUpdate()
        {
            // == Auto-load saved settings once player first exists ==
            if (_pendingAutoLoad)
            {
                if ((object)GameObject.Find("Player_Human") != null)
                {
                    _pendingAutoLoad = false;
                    MelonLogger.Msg("[AutoLoad] Player_Human found — loading saved settings...");
                    try { StatsManager.LoadStats(); }
                    catch (System.Exception ex) { MelonLogger.Warning("[AutoLoad] " + ex.Message); }
                }
            }

            // == Deferred reapply ==
            if (_pendingReapply)
            {
                if ((object)GameObject.Find("Player_Human") != null)
                {
                    MelonLogger.Msg("[Reapply] === Player_Human found — APPLYING ===");
                    _pendingReapply = false;
                    int ok = 0, fail = 0;

                    if (_reapplyFlyMode) { _reapplyFlyMode = false; try { FlyMode.Toggle(); ok++; MelonLogger.Msg("[Reapply]   + FlyMode"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! FlyMode: " + ex.Message); } }
                    if (_reapplyDrunkMode) { _reapplyDrunkMode = false; try { DrunkMode.Toggle(); ok++; MelonLogger.Msg("[Reapply]   + DrunkMode"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! DrunkMode: " + ex.Message); } }
                    if (_reapplyMirrorMode) { _reapplyMirrorMode = false; try { MirrorMode.Toggle(); ok++; MelonLogger.Msg("[Reapply]   + MirrorMode"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! MirrorMode: " + ex.Message); } }
                    if (_reapplyWideTyres) { _reapplyWideTyres = false; try { WideTyres.SetLevel(_reapplyWideTyresLevel); WideTyres.Toggle(); ok++; MelonLogger.Msg("[Reapply]   + WideTyres lv=" + _reapplyWideTyresLevel); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! WideTyres: " + ex.Message); } }
                    if (_reapplyFov) { _reapplyFov = false; try { FOV.Toggle(); ok++; MelonLogger.Msg("[Reapply]   + FOV"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! FOV: " + ex.Message); } }
                    if (_reapplySpeedrunTimer) { _reapplySpeedrunTimer = false; try { SpeedrunTimer.Toggle(); ok++; MelonLogger.Msg("[Reapply]   + SpeedrunTimer"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! SpeedrunTimer: " + ex.Message); } }
                    if (_reapplyAcceleration) { _reapplyAcceleration = false; try { Acceleration.Toggle(); ok++; MelonLogger.Msg("[Reapply]   + Acceleration"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! Acceleration: " + ex.Message); } }
                    if (_reapplyMaxSpeed) { _reapplyMaxSpeed = false; try { MaxSpeedMultiplier.Toggle(); ok++; MelonLogger.Msg("[Reapply]   + MaxSpeed"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! MaxSpeed: " + ex.Message); } }
                    if (_reapplyLandingImpact) { _reapplyLandingImpact = false; try { LandingImpact.Toggle(); ok++; MelonLogger.Msg("[Reapply]   + LandingImpact"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! LandingImpact: " + ex.Message); } }
                    if (_reapplyMoveSpin) { _reapplyMoveSpin = false; try { Movement.ToggleSpin(); ok++; MelonLogger.Msg("[Reapply]   + Spin"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! Spin: " + ex.Message); } }
                    if (_reapplyMoveHop) { _reapplyMoveHop = false; try { Movement.ToggleHop(); ok++; MelonLogger.Msg("[Reapply]   + Hop"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! Hop: " + ex.Message); } }
                    if (_reapplyMoveWheelie) { _reapplyMoveWheelie = false; try { Movement.ToggleWheelie(); ok++; MelonLogger.Msg("[Reapply]   + Wheelie"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! Wheelie: " + ex.Message); } }
                    if (_reapplyMoveLean) { _reapplyMoveLean = false; try { Movement.ToggleLean(); ok++; MelonLogger.Msg("[Reapply]   + Lean"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! Lean: " + ex.Message); } }
                    if (_reapplyBikeTorch) { _reapplyBikeTorch = false; try { BikeTorch.IntensityIndex = _reapplyBikeTorchIntensity; BikeTorch.Toggle(); ok++; MelonLogger.Msg("[Reapply]   + BikeTorch"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! BikeTorch: " + ex.Message); } }
                    if (_reapplyCameraShake) { _reapplyCameraShake = false; try { CameraShake.SetLevel(_reapplyCameraShakeLevel); CameraShake.Toggle(); ok++; MelonLogger.Msg("[Reapply]   + CameraShake"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! CameraShake: " + ex.Message); } }
                    if (_reapplyNearMiss) { _reapplyNearMiss = false; try { NearMissSensitivity.SetLevel(_reapplyNearMissLevel); NearMissSensitivity.Toggle(); ok++; MelonLogger.Msg("[Reapply]   + NearMiss"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! NearMiss: " + ex.Message); } }
                    if (_reapplyExplodingProps) { _reapplyExplodingProps = false; try { ExplodingProps.Toggle(); ok++; MelonLogger.Msg("[Reapply]   + ExplodingProps"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! ExplodingProps: " + ex.Message); } }
                    if (_reapplyCOMNeeded) { _reapplyCOMNeeded = false; try { CenterOfMass.SetLR(_reapplyCOMx); CenterOfMass.SetFB(_reapplyCOMz); CenterOfMass.SetUD(_reapplyCOMy); ok++; MelonLogger.Msg("[Reapply]   + COM"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! COM: " + ex.Message); } }
                    if (_reapplySuspNeeded) { _reapplySuspNeeded = false; try { Suspension.SetTravelLevel(_reapplySuspTravel); Suspension.SetStiffnessLevel(_reapplySuspStiff); Suspension.SetDampingLevel(_reapplySuspDamp); ok++; MelonLogger.Msg("[Reapply]   + Suspension"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! Suspension: " + ex.Message); } }
                    if (_reapplyBikeScaleNeeded) { _reapplyBikeScaleNeeded = false; try { BikeSize.Apply(_reapplyBikeScale); ok++; MelonLogger.Msg("[Reapply]   + BikeScale=" + _reapplyBikeScale); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! BikeScale: " + ex.Message); } }
                    if (_reapplyPlayerScaleNeeded) { _reapplyPlayerScaleNeeded = false; try { PlayerSize.Apply(_reapplyPlayerScale); ok++; MelonLogger.Msg("[Reapply]   + PlayerScale=" + _reapplyPlayerScale); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! PlayerScale: " + ex.Message); } }
                    if (_reapplyInvisibleBike) { _reapplyInvisibleBike = false; try { InvisibleBike.SetEnabled(true); ok++; MelonLogger.Msg("[Reapply]   + InvisibleBike"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! InvisibleBike: " + ex.Message); } }
                    if (_reapplyInvisiblePlayer) { _reapplyInvisiblePlayer = false; try { InvisiblePlayer.SetEnabled(true); ok++; MelonLogger.Msg("[Reapply]   + InvisiblePlayer"); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! InvisiblePlayer: " + ex.Message); } }
                    if (_reapplyWheelSize) { _reapplyWheelSize = false; try { WheelSize.ApplyFromSave(true, _reapplyWheelSizeLevel, _reapplyWheelSizeMode); ok++; MelonLogger.Msg("[Reapply]   + WheelSize level=" + _reapplyWheelSizeLevel); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! WheelSize: " + ex.Message); } }
                    if (_reapplyIndividualWheel) { _reapplyIndividualWheel = false; try { WheelSize.ApplyIndividualFromSave(_reapplyFrontWheelLevel, _reapplyRearWheelLevel); ok++; MelonLogger.Msg("[Reapply]   + IndividualWheel F=" + _reapplyFrontWheelLevel + " R=" + _reapplyRearWheelLevel); } catch (System.Exception ex) { fail++; MelonLogger.Error("[Reapply]   ! IndividualWheel: " + ex.Message); } }

                    MelonLogger.Msg("[Reapply] === DONE: " + ok + " applied, " + fail + " failed ===");

                    // Verify immediate mods survived the deferred reapply
                    MelonLogger.Msg("[Reapply] VERIFY: NoSpeedCap=" + NoSpeedCap.Enabled
                        + " SlowMotion=" + SlowMotion.Enabled
                        + " CutBrakes=" + CutBrakes.Enabled
                        + " IceMode=" + IceMode.Enabled
                        + " StickyTyres=" + StickyTyres.Enabled
                        + " AirControl=" + AirControl.Enabled
                        + " AutoBalance=" + AutoBalance.Enabled
                        + " WheelieAngle=" + WheelieAngleLimit.Enabled
                        + " QuickBrake=" + QuickBrake.Enabled
                        + " ReverseSteering=" + ReverseSteering.Enabled
                        + " NoBail=" + NoBail.Enabled
                        + " Gravity=" + Gravity.Level
                        + " SuspHUD=" + SuspensionHUD.Enabled
                        + " BrakeFade=" + BrakeFade.Enabled);

                    // Refresh UI so menu toggles reflect restored state
                    try { MenuWindow.RefreshAll(); } catch { }
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
                if (!UI.BindsPage.IsListening && (Input.GetKeyDown(KeyCode.F6)
                    || KeyBindManager.CheckMenuOpenPressed())) MenuUI.ToggleMenu();
            }
            catch (System.Exception ex) { MelonLogger.Error("ToggleMenu: " + ex.Message); }
            try { UI.BindsPage.CheckController(); } catch (System.Exception ex) { MelonLogger.Error("BindsPage.CheckController: " + ex.Message); }

            try { SceneDumper.CheckHotkey(); } catch (System.Exception ex) { MelonLogger.Error("SceneDumper: " + ex.Message); }
            try { SpeedWatcher.CheckHotkey(); } catch (System.Exception ex) { MelonLogger.Error("SpeedWatcher: " + ex.Message); }
            try { TopSpeed.Tick(); } catch (System.Exception ex) { MelonLogger.Error("TopSpeed.Tick: " + ex.Message); }
            try { TrickMultiplier.Tick(); } catch (System.Exception ex) { MelonLogger.Error("TrickMultiplier.Tick: " + ex.Message); }
            try { ScreenshotMode.Tick(); } catch { }
            try { SessionTrackers.Tick(); } catch (System.Exception ex) { MelonLogger.Error("SessionTrackers.Tick: " + ex.Message); }
            try { MenuWindow.TickLive(); } catch { }
            try { MirrorMode.Tick(); } catch { }
            try { FlyMode.Tick(); } catch { }
            try { DrunkMode.Tick(); } catch { }
            try { OutfitPage.Tick(); } catch { }
            try { ChatPage.Tick(); } catch { }
            try { InfoPage.Tick(); } catch { }
            try { FavsPage.Tick(); } catch { }
            if (!OutfitPage.IsRenaming && !ChatPage.IsChatFocused && !MapPage.IsSeedFocused && !ModesPage.IsTAInputFocused && !UI.SearchPage.IsQueryFocused)
                try { SessionTrackers.CheckpointTick(); } catch { }
            try { ModesPage.Tick(); } catch { }
            try { AvalancheMode.Tick(); } catch { }
            try { PoliceChaseMode.Tick(); } catch { }
            try { TrickAttackMode.Tick(); } catch { }
            try { BoulderDodgeMode.Tick(); } catch { }
            try { SurvivalMode.Tick(); } catch { }
            try { GhostReplay.Tick(); } catch { }
            try { MapChanger.Tick(); } catch { }
            try { MapPage.SeedTick(); } catch { }
            try { UI.SearchPage.SearchTick(); } catch { }
            try { GhostPage.Tick(); } catch { }
            try { SlowMoOnBail.Tick(); } catch { }
            try { ChaosMode.Tick(); } catch { }
            try { ModDetection.TagLocalPlayer(); } catch { }
            if (!OutfitPage.IsRenaming && !ChatPage.IsChatFocused && !MapPage.IsSeedFocused && !ModesPage.IsTAInputFocused && !UI.BindsPage.IsListening && !UI.SearchPage.IsQueryFocused)
                try { KeyBindManager.CheckAll(); } catch (System.Exception ex) { MelonLogger.Error("KeyBindManager.CheckAll: " + ex.Message); }
        }

        public override void OnFixedUpdate()
        {
            try { AvalancheMode.FixedTick(); } catch { }
            try { EarthquakeMode.FixedTick(); } catch { }
            try { PoliceChaseMode.FixedTick(); } catch { }
            try { StickyTyres.FixedTick(); } catch { }
            try { AirControl.FixedTick(); } catch { }
            try { CenterOfMass.FixedTick(); } catch { }
            try { BoulderDodgeMode.FixedTick(); } catch { }
            try { BikeDamage.FixedTick(); } catch { }
            // BrakeFade heat model runs inside BrakeFade_Patch.Postfix (no separate tick needed)
        }

        public override void OnLateUpdate()
        {
            try { FOV.Apply(); } catch (System.Exception ex) { MelonLogger.Error("FOV.Apply: " + ex.Message); }
            try { SkyColours.Tick(); } catch (System.Exception ex) { MelonLogger.Error("SkyColours.Tick: " + ex.Message); }
            try { DrunkMode.LateTick(); } catch { }
            try { WheelSize.Tick(); } catch { }
            try { WideTyres.Tick(); } catch { }
            try { BikeDamage.Tick(); } catch { }
            try { BigHeadMode.Tick(); } catch { }
            try { TrailPainter.Tick(); } catch { }
            try { RandomBikeSwitch.Tick(); } catch { }
            // Vehicle/Cyclist stat mods that need to win the race against the game's
            // own bike-stat init, which runs after a one-time apply and silently
            // clobbers a plain field write (confirmed via scene dump 2026-08-04:
            // Acceleration's field showed the raw default instead of our multiplied
            // value, no exception, just overwritten). Re-enforcing every LateUpdate
            // frame beats that instead of applying once on scene load.
            try { Acceleration.Tick(); } catch (System.Exception ex) { MelonLogger.Error("Acceleration.Tick: " + ex.Message); }
            try { MaxSpeedMultiplier.Tick(); } catch (System.Exception ex) { MelonLogger.Error("MaxSpeedMultiplier.Tick: " + ex.Message); }
            try { LandingImpact.Tick(); } catch (System.Exception ex) { MelonLogger.Error("LandingImpact.Tick: " + ex.Message); }
        }

        public override void OnGUI()
        {
            try { ESP.OnGUI(); } catch (System.Exception ex) { MelonLogger.Error("ESP.OnGUI: " + ex.Message); }
            try { GhostHUD.Draw(); } catch (System.Exception ex) { MelonLogger.Error("GhostHUD.Draw: " + ex.Message); }
            try { PoliceHUD.Draw(); } catch { }
            try { TrickAttackHUD.Draw(); } catch { }
            try { SurvivalHUD.Draw(); } catch { }
            try { SessionHUD.Draw(); } catch { }
            try { SuspensionHUD.OnGUI(); } catch { }
            try { BrakeFade.OnGUI(); } catch { }
            try { WheelieHUD.OnGUI(); } catch { }
            try { UI.BindsPage.OnGUI(); } catch { }
        }

        public override void OnApplicationQuit()
        {
            MenuUI.RestoreCursor();
            SlowMotion.Reset(); QuickBrake.Reset(); QuickBrake_Patch.ClearCache();
            MelonLogger.Msg("OnApplicationQuit");
        }
    }
}