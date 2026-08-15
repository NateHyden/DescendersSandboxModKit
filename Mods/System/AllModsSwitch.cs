using System;
using DescendersModMenu.UI;

namespace DescendersModMenu.Mods
{
    // Header master switch: turn every live mod off, then restore the same set.
    public static class AllModsSwitch
    {
        public static bool Enabled { get; private set; } = true;

        private static bool[] _snap;

        public static void Toggle()
        {
            if (Enabled) DisableAll();
            else Restore();
        }

        private static Slot[] Slots()
        {
            return new Slot[]
            {
                S("Acceleration", () => Acceleration.Enabled, Acceleration.Toggle),
                S("MaxSpeed", () => MaxSpeedMultiplier.Enabled, MaxSpeedMultiplier.Toggle),
                S("NoSpeedCap", () => NoSpeedCap.Enabled, NoSpeedCap.Toggle),
                S("LandingImpact", () => LandingImpact.Enabled, LandingImpact.Toggle),
                S("NoBail", () => NoBail.Enabled, NoBail.Toggle),
                S("AutoBalance", () => AutoBalance.Enabled, AutoBalance.Toggle),
                S("QuickBrake", () => QuickBrake.Enabled, QuickBrake.Toggle),
                S("FOV", () => FOV.Enabled, FOV.Toggle),
                S("SlowMotion", () => SlowMotion.Enabled, SlowMotion.Toggle),
                S("SlowMoOnBail", () => SlowMoOnBail.Enabled, SlowMoOnBail.Toggle),
                S("BlackDeath", () => BlackDeath.Enabled, BlackDeath.Toggle),
                S("NoSpeedWobbles", () => GameModifierMods.NoSpeedWobblesEnabled, GameModifierMods.NoSpeedWobblesToggle),
                S("Compass", () => CompassAlwaysOn.Enabled, CompassAlwaysOn.Toggle),
                S("HoverMode", () => HoverMode.Enabled, HoverMode.Toggle),
                S("CutBrakes", () => CutBrakes.Enabled, CutBrakes.Toggle),
                S("ReverseSteering", () => ReverseSteering.Enabled, ReverseSteering.Toggle),
                S("RubberBand", () => RubberBandSteering.Enabled, RubberBandSteering.Toggle),
                S("IceMode", () => IceMode.Enabled, IceMode.Toggle),
                S("TyrePressure", () => TyrePressure.Enabled, TyrePressure.Toggle),
                S("SpiderBike", () => SpiderBike.Enabled, SpiderBike.Toggle),
                S("StickyTyres", () => StickyTyres.Enabled, StickyTyres.Toggle),
                S("WideTyres", () => WideTyres.Enabled, WideTyres.Toggle),
                S("AirControl", () => AirControl.Enabled, AirControl.Toggle),
                S("WheelieAngle", () => WheelieAngleLimit.Enabled, WheelieAngleLimit.Toggle),
                S("BikeTorch", () => BikeTorch.Enabled, BikeTorch.Toggle),
                S("DiscoTorch", () => BikeTorch.DiscoEnabled, BikeTorch.ToggleDisco),
                S("BikeDamage", () => BikeDamage.Enabled, BikeDamage.Toggle),
                S("BouncyBike", () => BouncyBike.Enabled, BouncyBike.Toggle),
                S("InstantRespawn", () => InstantRespawn.Enabled, InstantRespawn.Toggle),
                S("InvisibleBike", () => InvisibleBike.Enabled, InvisibleBike.Toggle),
                S("InvisiblePlayer", () => InvisiblePlayer.Enabled, InvisiblePlayer.Toggle),
                S("TrickSetSwap", () => TrickSetSwap.Enabled, TrickSetSwap.Toggle),
                S("Spin", () => Movement.SpinEnabled, Movement.ToggleSpin),
                S("Hop", () => Movement.HopEnabled, Movement.ToggleHop),
                S("Wheelie", () => Movement.WheelieEnabled, Movement.ToggleWheelie),
                S("Lean", () => Movement.LeanEnabled, Movement.ToggleLean),
                S("FlyMode", () => FlyMode.Enabled, FlyMode.Toggle),
                S("DrunkMode", () => DrunkMode.Enabled, DrunkMode.Toggle),
                S("MirrorMode", () => MirrorMode.Enabled, MirrorMode.Toggle),
                S("MoonMode", () => MoonMode.IsActive, MoonMode.Toggle),
                S("CameraShake", () => CameraShake.Enabled, CameraShake.Toggle),
                S("SpeedrunTimer", () => SpeedrunTimer.Enabled, SpeedrunTimer.Toggle),
                S("SessionHUD", () => SessionHUD.Enabled, SessionHUD.Toggle),
                S("TrickMultiplier", () => TrickMultiplier.Enabled, TrickMultiplier.Toggle),
                S("ESP", () => ESP.Enabled, ESP.Toggle),
                S("GhostReplay", () => GhostReplay.Enabled, GhostReplay.Toggle),
                S("Trees", () => Trees.Enabled, Trees.Toggle),
                S("Music", () => Music.Enabled, Music.Toggle),
                S("Fog", () => Fog.Enabled, Fog.Toggle),
                S("TurboWind", () => TurboWind.Enabled, TurboWind.Toggle),
                S("ExplodingProps", () => ExplodingProps.Enabled, ExplodingProps.Toggle),
                S("HeadlightsOnly", () => HeadlightsOnly.Enabled, HeadlightsOnly.Toggle),
                S("Storm", () => SkyColours.StormEnabled, SkyColours.ToggleStorm),
                S("DiscoMode", () => DiscoMode.Enabled, DiscoMode.Toggle),
                S("BlizzardDial", () => BlizzardDial.Enabled, BlizzardDial.Toggle),
                S("NearMiss", () => NearMissSensitivity.Enabled, NearMissSensitivity.Toggle),
                S("ObjectPlacer", () => ObjectPlacer.Enabled, ObjectPlacer.Toggle),
                S("UIRemover", () => UIRemover.Enabled, UIRemover.Toggle),
                S("ScreenshotMode", () => ScreenshotMode.Enabled, ScreenshotMode.Toggle),
                S("SpectateMode", () => SpectateMode.Enabled, SpectateMode.Toggle),
                S("SuspensionHUD", () => SuspensionHUD.Enabled, SuspensionHUD.Toggle),
                S("WheelieHUD", () => WheelieHUD.Enabled, WheelieHUD.Toggle),
                S("BrakeFade", () => BrakeFade.Enabled, BrakeFade.Toggle),
                S("TrailPainter", () => TrailPainter.Enabled, TrailPainter.Toggle),
                S("Confetti", () => ConfettiOnTrick.Enabled, ConfettiOnTrick.Toggle),
                S("BigHead", () => BigHeadMode.Enabled, BigHeadMode.Toggle),
                S("ChaosMode", () => ChaosMode.Enabled, ChaosMode.Toggle),
                S("RandomBike", () => RandomBikeSwitch.Enabled, RandomBikeSwitch.Toggle),
                S("RandomMutator", () => RandomMutatorOnCheckpoint.Enabled, RandomMutatorOnCheckpoint.Toggle),
                S("RandomWeather", () => RandomWeatherRoulette.Enabled, RandomWeatherRoulette.Toggle),
                S("Avalanche", () => AvalancheMode.Enabled, AvalancheMode.Toggle),
                S("Earthquake", () => EarthquakeMode.Enabled, EarthquakeMode.Toggle),
                S("PoliceChase", () => PoliceChaseMode.Enabled, PoliceChaseMode.Toggle),
                S("Survival", () => SurvivalMode.Enabled, SurvivalMode.Toggle),
                S("BoulderDodge", () => BoulderDodgeMode.Enabled, BoulderDodgeMode.Toggle),
                S("TrickAttack", () => TrickAttackMode.CurrentState != TrickAttackMode.State.Off, TrickAttackMode.Toggle)
            };
        }

        public static void DisableAll()
        {
            Slot[] slots = Slots();
            _snap = new bool[slots.Length];
            int n = 0;
            ModLog.SuppressUserFeedback = true;
            try
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    try
                    {
                        bool on = slots[i].Get();
                        _snap[i] = on;
                        if (!on) continue;
                        slots[i].Toggle();
                        n++;
                    }
                    catch (Exception ex) { ModLog.Error(ex, "AllMods " + slots[i].Name); }
                }
            }
            finally { ModLog.SuppressUserFeedback = false; }

            Enabled = false;
            ModLog.Feedback("[All Mods] -> OFF (" + n + " paused)");
            RefreshUi();
        }

        public static void Restore()
        {
            if (_snap == null)
            {
                Enabled = true;
                ModLog.Feedback("[All Mods] -> ON");
                RefreshUi();
                return;
            }

            Slot[] slots = Slots();
            int n = 0;
            ModLog.SuppressUserFeedback = true;
            try
            {
                int count = _snap.Length < slots.Length ? _snap.Length : slots.Length;
                for (int i = 0; i < count; i++)
                {
                    if (!_snap[i]) continue;
                    try
                    {
                        if (!slots[i].Get()) slots[i].Toggle();
                        n++;
                    }
                    catch (Exception ex) { ModLog.Error(ex, "AllMods " + slots[i].Name); }
                }
            }
            finally { ModLog.SuppressUserFeedback = false; }

            _snap = null;
            Enabled = true;
            ModLog.Feedback("[All Mods] -> ON (restored " + n + ")");
            RefreshUi();
        }

        private static void RefreshUi()
        {
            try { MenuWindow.RefreshAllModsSwitch(); } catch { }
            try { MenuWindow.RefreshAll(); } catch { }
            try { WorldPage.RefreshAll(); } catch { }
            try { BikePage.RefreshAll(); } catch { }
            try { FunPage.RefreshAll(); } catch { }
            try { OtherPage.RefreshAll(); } catch { }
            try { ModesPage.RefreshAll(); } catch { }
            try { GhostPage.RefreshAll(); } catch { }
            try { EspPage.RefreshTexts(); } catch { }
            try { SessionPage.RefreshAll(); } catch { }
            try { ObjectPlacerPage.RefreshAll(); } catch { }
            try { GraphicsPage.RefreshAll(); } catch { }
            try { FavsPage.RefreshFavourites(); } catch { }
        }

        private static Slot S(string name, GetFn get, ActFn toggle)
        {
            Slot s;
            s.Name = name;
            s.Get = get;
            s.Toggle = toggle;
            return s;
        }

        private delegate bool GetFn();
        private delegate void ActFn();

        private struct Slot
        {
            public string Name;
            public GetFn Get;
            public ActFn Toggle;
        }
    }
}
