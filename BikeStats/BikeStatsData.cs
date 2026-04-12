using System;

namespace DescendersModMenu.BikeStats
{
    [Serializable]
    public class BikeStatsData
    {
        // ── Bike / Stats ───────────────────────────────────────────────
        public int AccelerationLevel = 1;
        public bool AccelerationEnabled = false;
        public int MaxSpeedLevel = 1;
        public bool MaxSpeedEnabled = false;
        public int LandingImpactLevel = 1;
        public bool LandingImpactEnabled = false;
        public bool NoBailEnabled = false;
        public int BikeIndex = 0;

        // ── FOV ────────────────────────────────────────────────────────
        public int FovLevel = 5;
        public bool FovEnabled = false;

        // ── Auto Balance ───────────────────────────────────────────────
        public bool AutoBalanceEnabled = false;
        public int AutoBalanceStrengthLevel = 5;

        // ── No Speed Wobbles ───────────────────────────────────────────
        public bool NoSpeedWobblesEnabled = false;

        // ── Quick Brake ────────────────────────────────────────────────
        public bool QuickBrakeEnabled = false;
        public int QuickBrakeLevel = 5;

        // ── Movement ───────────────────────────────────────────────────
        public int SpinLevel = 1;
        public bool SpinEnabled = false;
        public int HopLevel = 1;
        public bool HopEnabled = false;
        public int WheelieLevel = 1;
        public bool WheelieEnabled = false;
        public int LeanLevel = 1;
        public bool LeanEnabled = false;

        // ── Suspension ─────────────────────────────────────────────────
        public int SuspTravelLevel = 5;
        public int SuspStiffnessLevel = 5;
        public int SuspDampingLevel = 5;

        // ── Game Modifiers ─────────────────────────────────────────────
        public int WheelieBalanceLevel = 1;
        public int InAirCorrLevel = 1;
        public int FakieBalanceLevel = 1;
        public int PumpStrengthLevel = 1;
        public int IcePhysicsLevel = 1;

        // ── World / Graphics ───────────────────────────────────────────
        public int GravityLevel = 5;
        public int TimeOfDayLevel = 4;
        public int SkyPreset = 0;

        // ── Tyres ──────────────────────────────────────────────────────
        public bool WideTyresEnabled = false;
        public int WideTyresLevel = 5;
        public bool StickyTyresEnabled = false;
        public float StickyForce = 150f;

        // ── Wheelie Angle Limit / Air Control ──────────────────────────
        public bool WheelieAngleLimitEnabled = false;
        public int WheelieAngleLimitLevel = 5;
        public bool AirControlEnabled = false;
        public int AirControlLevel = 5;

        // ── Toggles ────────────────────────────────────────────────────
        public bool SlowMotionEnabled = false;
        public int SlowMotionLevel = 5;
        public bool CutBrakesEnabled = false;
        public bool NoSpeedCapEnabled = false;
        public bool ReverseSteerEnabled = false;
        public bool IceModeEnabled = false;
        public bool MirrorModeEnabled = false;
        public bool DrunkModeEnabled = false;
        public bool FlyModeEnabled = false;
        public bool EspEnabled = false;
        public bool SpeedrunTimerEnabled = false;
        public bool SlowMoOnBailEnabled = false;
        public bool GhostReplayEnabled = false;

        // ── Floats ─────────────────────────────────────────────────────
        public float FlyMoveSpeed = 30f;
        public float FlyClimbSpeed = 20f;
        public float GhostAlpha = 0.45f;

        // ── Menu Customiser ────────────────────────────────────────────
        public int MenuPositionPreset = 0;  // 0=Center 1=TopLeft 2=TopRight
        public int MenuScaleLevel = 3;      // index into scale array (3 = 100%)
        public int MenuOpacityLevel = 8;    // index into opacity array (8 = 100%)

        // ── Bike Torch ─────────────────────────────────────────────────
        public bool BikeTorchEnabled = false;
        public int BikeTorchIntensityIndex = 2;

        // ── Camera Shake ───────────────────────────────────────────────
        public bool CameraShakeEnabled = false;
        public int CameraShakeLevel = 5;

        // ── Center of Mass ─────────────────────────────────────────────
        public float CenterOfMassLR = 0f;
        public float CenterOfMassFB = 0f;
        public float CenterOfMassUD = 0f;

        // ── Exploding Props ────────────────────────────────────────────
        public bool ExplodingPropsEnabled = false;

        // ── Near Miss Sensitivity ──────────────────────────────────────
        public bool NearMissEnabled = false;
        public int NearMissLevel = 5;

        // ── Bike / Player Scale ──────────────────────────────────────
        public float BikeScale = 1f;  // legacy — kept for old save compat
        public float PlayerScale = 1f;  // legacy — kept for old save compat
        public int BikeSizeLevel = 10;
        public int PlayerSizeLevel = 10;
        public bool InvisibleBikeEnabled = false;
        public bool InvisiblePlayerEnabled = false;
        public bool WheelSizeEnabled = false;
        public int WheelSizeMode = 0;   // legacy
        public int WheelSizeLevel = 10;
        public int FrontWheelSizeLevel = 10;
        public int RearWheelSizeLevel = 10;
        public bool IndividualWheelMode = false;

        // ── Graphics ───────────────────────────────────────────────────
        public bool GraphicsBloomEnabled = true;
        public bool GraphicsAmbientOccEnabled = true;
        public bool GraphicsVignetteEnabled = true;
        public bool GraphicsDepthOfFieldEnabled = false;  // v3.6.2: DOF defaults OFF (matches ResetStats)
        public bool GraphicsChromaticAbEnabled = true;

        // ── Sky Storm / Rain ───────────────────────────────────────────
        public bool StormEnabled = false;
        public int RainIntensityLevel = 5;

        // ── Suspension HUD ─────────────────────────────────────────────
        public bool SuspensionHUDEnabled = false;

        // ── Brake Fade ─────────────────────────────────────────────────
        public bool BrakeFadeEnabled = false;
        public int BrakeBalanceLevel = 6;  // 1=10F/90R … 6=60F/40R (default) … 11=100F/0R

        // ── Wheelie HUD ────────────────────────────────────────────────
        public bool WheelieHUDEnabled = false;

        // ── Trick Set Swap ─────────────────────────────────────────────
        // Saved by source bike NAME (not index) so save files survive any
        // future change to the bike-type discovery order.
        public bool TrickSetSwapEnabled = false;
        public string TrickSetSwapSourceName = "";

        // ── Tyre Pressure ──────────────────────────────────────────────
        public bool TyrePressureEnabled = false;
        public int TyrePressureLevel = 5;  // 1=Flat … 5=Stock … 10=Hard

        // ── Instant Respawn ────────────────────────────────────────────
        public bool InstantRespawnEnabled = false;

        // ── Bike Damage ────────────────────────────────────────────────
        public bool BikeDamageEnabled = false;

        // ── Headlights Only ────────────────────────────────────────────
        public bool HeadlightsOnlyEnabled = false;

        // ── UI Remover ─────────────────────────────────────────────────
        public bool UIRemoverEnabled = false;
    }
}