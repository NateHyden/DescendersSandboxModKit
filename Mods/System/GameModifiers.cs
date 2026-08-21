using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    public static class GameModifierMods
    {
        public static int WheelieBalanceLevel { get; private set; } = 5;
        public static int InAirCorrLevel { get; private set; } = 5;
        public static int FakieBalanceLevel { get; private set; } = 5;
        public static int PumpStrengthLevel { get; private set; } = 5;
        public static int TweakSpeedLevel { get; private set; } = 5;
        public static int IcePhysicsLevel { get; private set; } = 5;

        private static float Delta(int level) { return (level - 5f) * 20f; }
        public static string DeltaDisplay(int level) { return Delta(level).ToString("+0;-0") + "%"; }

        private static void NotifyDial(string name, int oldLevel, int newLevel)
        {
            ModLog.Dial(name, oldLevel != 5, newLevel != 5);
        }

        public static void WheelieBalanceIncrease()
        {
            if (WheelieBalanceLevel >= 10) return;
            int old = WheelieBalanceLevel;
            WheelieBalanceLevel = old + 1;
            NotifyDial("Wheelie Balance", old, WheelieBalanceLevel);
            ApplyMod("WHEELIEBALANCE", WheelieBalanceLevel);
        }
        public static void WheelieBalanceDecrease()
        {
            if (WheelieBalanceLevel <= 1) return;
            int old = WheelieBalanceLevel;
            WheelieBalanceLevel = old - 1;
            NotifyDial("Wheelie Balance", old, WheelieBalanceLevel);
            ApplyMod("WHEELIEBALANCE", WheelieBalanceLevel);
        }
        public static void SetWheelieBalanceLevel(int v)
        {
            int old = WheelieBalanceLevel;
            WheelieBalanceLevel = System.Math.Max(1, System.Math.Min(10, v));
            NotifyDial("Wheelie Balance", old, WheelieBalanceLevel);
            ApplyMod("WHEELIEBALANCE", WheelieBalanceLevel);
        }

        public static void InAirCorrIncrease()
        {
            if (InAirCorrLevel >= 10) return;
            int old = InAirCorrLevel;
            InAirCorrLevel = old + 1;
            NotifyDial("Air Correction", old, InAirCorrLevel);
            ApplyMod("AIRCORRECTION", InAirCorrLevel);
        }
        public static void InAirCorrDecrease()
        {
            if (InAirCorrLevel <= 1) return;
            int old = InAirCorrLevel;
            InAirCorrLevel = old - 1;
            NotifyDial("Air Correction", old, InAirCorrLevel);
            ApplyMod("AIRCORRECTION", InAirCorrLevel);
        }
        public static void SetInAirCorrLevel(int v)
        {
            int old = InAirCorrLevel;
            InAirCorrLevel = System.Math.Max(1, System.Math.Min(10, v));
            NotifyDial("Air Correction", old, InAirCorrLevel);
            ApplyMod("AIRCORRECTION", InAirCorrLevel);
        }

        public static void FakieBalanceIncrease()
        {
            if (FakieBalanceLevel >= 10) return;
            int old = FakieBalanceLevel;
            FakieBalanceLevel = old + 1;
            NotifyDial("Fakie Balance", old, FakieBalanceLevel);
            ApplyMod("FAKIEBALANCE", FakieBalanceLevel);
        }
        public static void FakieBalanceDecrease()
        {
            if (FakieBalanceLevel <= 1) return;
            int old = FakieBalanceLevel;
            FakieBalanceLevel = old - 1;
            NotifyDial("Fakie Balance", old, FakieBalanceLevel);
            ApplyMod("FAKIEBALANCE", FakieBalanceLevel);
        }
        public static void SetFakieBalanceLevel(int v)
        {
            int old = FakieBalanceLevel;
            FakieBalanceLevel = System.Math.Max(1, System.Math.Min(10, v));
            NotifyDial("Fakie Balance", old, FakieBalanceLevel);
            ApplyMod("FAKIEBALANCE", FakieBalanceLevel);
        }

        public static void PumpStrengthIncrease()
        {
            if (PumpStrengthLevel >= 10) return;
            int old = PumpStrengthLevel;
            PumpStrengthLevel = old + 1;
            NotifyDial("Pump Strength", old, PumpStrengthLevel);
            ApplyMod("PUMPSTRENGTH", PumpStrengthLevel);
        }
        public static void PumpStrengthDecrease()
        {
            if (PumpStrengthLevel <= 1) return;
            int old = PumpStrengthLevel;
            PumpStrengthLevel = old - 1;
            NotifyDial("Pump Strength", old, PumpStrengthLevel);
            ApplyMod("PUMPSTRENGTH", PumpStrengthLevel);
        }
        public static void SetPumpStrengthLevel(int v)
        {
            int old = PumpStrengthLevel;
            PumpStrengthLevel = System.Math.Max(1, System.Math.Min(10, v));
            NotifyDial("Pump Strength", old, PumpStrengthLevel);
            ApplyMod("PUMPSTRENGTH", PumpStrengthLevel);
        }

        public static void TweakSpeedIncrease()
        {
            if (TweakSpeedLevel >= 10) return;
            int old = TweakSpeedLevel;
            TweakSpeedLevel = old + 1;
            NotifyDial("Tweak Speed", old, TweakSpeedLevel);
            ApplyMod("TWEAKSPEED", TweakSpeedLevel);
        }
        public static void TweakSpeedDecrease()
        {
            if (TweakSpeedLevel <= 1) return;
            int old = TweakSpeedLevel;
            TweakSpeedLevel = old - 1;
            NotifyDial("Tweak Speed", old, TweakSpeedLevel);
            ApplyMod("TWEAKSPEED", TweakSpeedLevel);
        }
        public static void SetTweakSpeedLevel(int v)
        {
            int old = TweakSpeedLevel;
            TweakSpeedLevel = System.Math.Max(1, System.Math.Min(10, v));
            NotifyDial("Tweak Speed", old, TweakSpeedLevel);
            ApplyMod("TWEAKSPEED", TweakSpeedLevel);
        }

        public static void IcePhysicsIncrease()
        {
            if (IcePhysicsLevel >= 10) return;
            int old = IcePhysicsLevel;
            IcePhysicsLevel = old + 1;
            NotifyDial("Ice Physics", old, IcePhysicsLevel);
            ApplyMod("OFFROADFRICTION", IcePhysicsLevel);
        }
        public static void IcePhysicsDecrease()
        {
            if (IcePhysicsLevel <= 1) return;
            int old = IcePhysicsLevel;
            IcePhysicsLevel = old - 1;
            NotifyDial("Ice Physics", old, IcePhysicsLevel);
            ApplyMod("OFFROADFRICTION", IcePhysicsLevel);
        }
        public static void SetIcePhysicsLevel(int v)
        {
            int old = IcePhysicsLevel;
            IcePhysicsLevel = System.Math.Max(1, System.Math.Min(10, v));
            NotifyDial("Ice Physics", old, IcePhysicsLevel);
            ApplyMod("OFFROADFRICTION", IcePhysicsLevel);
        }

        private static float IceMult(int level)
        {
            if (level <= 5)
                return 0.1f + (level - 1) * 0.225f;
            else
                return 1.0f + (level - 5) * 0.2f;
        }

        public static bool NoSpeedWobblesEnabled { get; private set; } = false;
        public static void NoSpeedWobblesToggle()
        {
            NoSpeedWobblesEnabled = !NoSpeedWobblesEnabled;
            ApplySpeedWobbles(NoSpeedWobblesEnabled ? 0.0f : 1.0f);
            ModLog.Feedback("[GameMod] NoSpeedWobbles -> " + (NoSpeedWobblesEnabled ? "ON" : "OFF"));
        }
        private static void ApplySpeedWobbles(float value)
        {
            try
            {
                GameData gameData = UnityEngine.Object.FindObjectOfType<GameData>();
                if ((object)gameData == null) return;
                FieldInfo modArrayField = gameData.GetType().GetField("\u0081jU\u0080h\u0084c",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)modArrayField == null) return;
                GameModifier[] mods = modArrayField.GetValue(gameData) as GameModifier[];
                if ((object)mods == null) return;
                for (int i = 0; i < mods.Length; i++)
                {
                    if ((object)mods[i] != null && mods[i].name == "SPEEDWOBBLES")
                    {
                        mods[i].modifiers[0].percentageValue = value;
                        PlayerManager pm = Object.FindObjectOfType<PlayerManager>();
                        if ((object)pm != null)
                        {
                            PlayerInfoImpact pi = pm.GetPlayerImpact();
                            if ((object)pi != null) pi.AddGameModifier(mods[i]);
                        }
                        break;
                    }
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[GameMod] SpeedWobbles: " + ex.Message); Telemetry.ReportErrorAsync(ex, "GameModifiers"); }
        }

        public static void NoSpeedWobblesReset()
        {
            if (NoSpeedWobblesEnabled)
                ApplySpeedWobbles(1.0f);
            NoSpeedWobblesEnabled = false;
        }

        public static void ApplyNoSpeedWobblesPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo fixedUpdate = typeof(Vehicle).GetMethod("FixedUpdate",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)fixedUpdate == null)
                { ModLog.Warn("[GameMod] NoSpeedWobbles: Vehicle.FixedUpdate not found."); return; }
                MethodInfo postfix = typeof(NoSpeedWobbles_Patch).GetMethod("Postfix",
                    BindingFlags.Public | BindingFlags.Static);
                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(postfix));
                ModLog.Debug("[GameMod] NoSpeedWobbles Vehicle patch applied.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[GameMod] NoSpeedWobbles Vehicle patch: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "GameModifiers"); }

            try
            {
                System.Type bikeCamType = typeof(BikeCamera);
                MethodInfo camFixedUpdate = bikeCamType.GetMethod("FixedUpdate",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)camFixedUpdate == null)
                { ModLog.Warn("[GameMod] NoSpeedWobbles: BikeCamera.FixedUpdate not found."); return; }
                MethodInfo camPostfix = typeof(NoSpeedWobbles_CamPatch).GetMethod("Postfix",
                    BindingFlags.Public | BindingFlags.Static);
                harmony.Patch(camFixedUpdate, postfix: new HarmonyMethod(camPostfix));
                ModLog.Debug("[GameMod] NoSpeedWobbles BikeCamera patch applied.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[GameMod] NoSpeedWobbles BikeCamera patch: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "GameModifiers"); }
        }

        public static void ApplyMod(string modName, int level)
        {
            try
            {
                GameData gameData = UnityEngine.Object.FindObjectOfType<GameData>();
                if ((object)gameData == null) { ModLog.Warn("[GameMod] GameData not found."); return; }
                FieldInfo modArrayField = gameData.GetType().GetField("\u0081jU\u0080h\u0084c",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)modArrayField == null) { ModLog.Warn("[GameMod] Mod array field not found."); return; }
                GameModifier[] mods = modArrayField.GetValue(gameData) as GameModifier[];
                if ((object)mods == null) { ModLog.Warn("[GameMod] Mod array is null."); return; }
                GameModifier target = null;
                for (int i = 0; i < mods.Length; i++)
                    if ((object)mods[i] != null && mods[i].name == modName)
                    { target = mods[i]; break; }
                if ((object)target == null) { ModLog.Warn("[GameMod] Modifier not found: " + modName); return; }
                // Percentage dials: (level-5)*20 → −80%…+100%. Game uses 1 + pct/100 as multiplier.
                // Always write the value (including 0% at level 5) so returning to stock actually clears the buff.
                float value = modName == "OFFROADFRICTION" ? IceMult(level) : Delta(level);
                target.modifiers[0].percentageValue = value;
                PlayerManager pm = UnityEngine.Object.FindObjectOfType<PlayerManager>();
                if ((object)pm == null) { ModLog.Warn("[GameMod] PlayerManager not found."); return; }
                PlayerInfoImpact pi = pm.GetPlayerImpact();
                if ((object)pi == null) { ModLog.Warn("[GameMod] PlayerInfoImpact not found."); return; }
                pi.AddGameModifier(target);
                ModLog.Debug("[GameMod] " + modName + " level " + level + " (" + value + "%)");
            }
            catch (System.Exception ex) { MelonLogger.Error("[GameMod] ApplyMod " + modName + ": " + ex.Message); Telemetry.ReportErrorAsync(ex, "GameModifiers"); }
        }

        public static void DumpAllModifiers()
        {
            try
            {
                GameData gameData = UnityEngine.Object.FindObjectOfType<GameData>();
                if ((object)gameData == null) { ModLog.Warn("[GameMod] DumpAllModifiers: GameData not found."); return; }
                FieldInfo modArrayField = gameData.GetType().GetField("\u0081jU\u0080h\u0084c",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)modArrayField == null) { ModLog.Warn("[GameMod] DumpAllModifiers: mod array field not found."); return; }
                GameModifier[] mods = modArrayField.GetValue(gameData) as GameModifier[];
                if ((object)mods == null) { ModLog.Warn("[GameMod] DumpAllModifiers: mod array is null."); return; }

                ModLog.Debug("[GameMod] === ALL GAME MODIFIERS (" + mods.Length + ") ===");
                for (int i = 0; i < mods.Length; i++)
                {
                    if ((object)mods[i] == null) { ModLog.Debug("[GameMod]   [" + i + "] <null>"); continue; }
                    float curVal = -1f;
                    try
                    {
                        if (mods[i].modifiers != null && mods[i].modifiers.Length > 0)
                            curVal = mods[i].modifiers[0].percentageValue;
                    }
                    catch { }
                    ModLog.Debug("[GameMod]   [" + i + "] name=" + mods[i].name + " value=" + curVal);
                }
                ModLog.Debug("[GameMod] === END DUMP ===");
            }
            catch (System.Exception ex) { MelonLogger.Error("[GameMod] DumpAllModifiers: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "GameModifiers"); }
        }
    }

    public static class NoSpeedWobbles_Patch
    {
        private static PropertyInfo _wobbleProp = null;
        private static bool _cached = false;

        public static void Postfix(Vehicle __instance)
        {
            if (!GameModifierMods.NoSpeedWobblesEnabled) return;
            if (!UnityNull.Alive(__instance)) return;

            if (!_cached)
            {
                _cached = true;
                PropertyInfo[] props = typeof(Vehicle).GetProperties(
                    BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < props.Length; i++)
                {
                    if (string.Equals(props[i].PropertyType.Name, "Single", System.StringComparison.Ordinal)
                        && props[i].CanWrite
                        && props[i].Name.Contains("kM"))
                    {
                        _wobbleProp = props[i];
                        ModLog.Debug("[GameMod] NoSpeedWobbles found property: " + props[i].Name);
                        break;
                    }
                }
            }
            if ((object)_wobbleProp != null)
            {
                try { _wobbleProp.SetValue(__instance, 0f, null); }
                catch { }
            }
        }
    }

    public static class NoSpeedWobbles_CamPatch
    {
        private static FieldInfo _shakeVel = null;
        private static FieldInfo _shakeOff = null;
        private static bool _cached = false;

        public static void Postfix(BikeCamera __instance)
        {
            if (!GameModifierMods.NoSpeedWobblesEnabled) return;
            if (!UnityNull.Alive(__instance)) return;

            if (!_cached)
            {
                _cached = true;
                FieldInfo[] fields = typeof(BikeCamera).GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (string.Equals(fields[i].FieldType.Name, "Vector3", System.StringComparison.Ordinal))
                    {
                        if ((object)_shakeVel == null) _shakeVel = fields[i];
                        else { _shakeOff = fields[i]; break; }
                    }
                }
            }

            try
            {
                if ((object)_shakeVel != null)
                    _shakeVel.SetValue(__instance, Vector3.zero);
                if ((object)_shakeOff != null)
                    _shakeOff.SetValue(__instance, Vector3.zero);
            }
            catch { }
        }
    }
}

