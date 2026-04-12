using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class GameModifierMods
    {
        public static int WheelieBalanceLevel { get; private set; } = 1;
        public static int InAirCorrLevel { get; private set; } = 1;
        public static int FakieBalanceLevel { get; private set; } = 1;
        public static int PumpStrengthLevel { get; private set; } = 1;
        public static int IcePhysicsLevel { get; private set; } = 1;

        private static float Mult(int level) { return level * 0.333f + 0.667f; }

        public static void WheelieBalanceIncrease() { if (WheelieBalanceLevel < 10) { WheelieBalanceLevel++; ApplyMod("WHEELIEBALANCE", WheelieBalanceLevel); } }
        public static void WheelieBalanceDecrease() { if (WheelieBalanceLevel > 1) { WheelieBalanceLevel--; ApplyMod("WHEELIEBALANCE", WheelieBalanceLevel); } }
        public static void SetWheelieBalanceLevel(int v)
        {
            WheelieBalanceLevel = System.Math.Max(1, System.Math.Min(10, v));
            ApplyMod("WHEELIEBALANCE", WheelieBalanceLevel);
        }

        public static void InAirCorrIncrease() { if (InAirCorrLevel < 10) { InAirCorrLevel++; ApplyMod("AIRCORRECTION", InAirCorrLevel); } }
        public static void InAirCorrDecrease() { if (InAirCorrLevel > 1) { InAirCorrLevel--; ApplyMod("AIRCORRECTION", InAirCorrLevel); } }
        public static void SetInAirCorrLevel(int v)
        {
            InAirCorrLevel = System.Math.Max(1, System.Math.Min(10, v));
            ApplyMod("AIRCORRECTION", InAirCorrLevel);
        }

        public static void FakieBalanceIncrease() { if (FakieBalanceLevel < 10) { FakieBalanceLevel++; ApplyMod("FAKIEBALANCE", FakieBalanceLevel); } }
        public static void FakieBalanceDecrease() { if (FakieBalanceLevel > 1) { FakieBalanceLevel--; ApplyMod("FAKIEBALANCE", FakieBalanceLevel); } }
        public static void SetFakieBalanceLevel(int v)
        {
            FakieBalanceLevel = System.Math.Max(1, System.Math.Min(10, v));
            ApplyMod("FAKIEBALANCE", FakieBalanceLevel);
        }

        public static void PumpStrengthIncrease() { if (PumpStrengthLevel < 10) { PumpStrengthLevel++; ApplyMod("PUMPSTRENGTH", PumpStrengthLevel); } }
        public static void PumpStrengthDecrease() { if (PumpStrengthLevel > 1) { PumpStrengthLevel--; ApplyMod("PUMPSTRENGTH", PumpStrengthLevel); } }
        public static void SetPumpStrengthLevel(int v)
        {
            PumpStrengthLevel = System.Math.Max(1, System.Math.Min(10, v));
            ApplyMod("PUMPSTRENGTH", PumpStrengthLevel);
        }

        public static void IcePhysicsIncrease() { if (IcePhysicsLevel < 10) { IcePhysicsLevel++; ApplyMod("OFFROADFRICTION", IcePhysicsLevel); } }
        public static void IcePhysicsDecrease() { if (IcePhysicsLevel > 1) { IcePhysicsLevel--; ApplyMod("OFFROADFRICTION", IcePhysicsLevel); } }
        public static void SetIcePhysicsLevel(int v)
        {
            IcePhysicsLevel = System.Math.Max(1, System.Math.Min(10, v));
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
            MelonLogger.Msg("[GameMod] NoSpeedWobbles -> " + (NoSpeedWobblesEnabled ? "ON" : "OFF"));
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
            catch (System.Exception ex) { MelonLogger.Error("[GameMod] SpeedWobbles: " + ex.Message); }
        }

        public static void NoSpeedWobblesReset()
        {
            if (NoSpeedWobblesEnabled)
                ApplySpeedWobbles(1.0f);
            NoSpeedWobblesEnabled = false;
        }

        // Direct patch on Vehicle.FixedUpdate — zeroes the steering wobble property
        // every physics frame when enabled. More reliable than the GameModifier route
        // because PlayerInfoImpact can reset modifiers on scene transitions.
        public static void ApplyNoSpeedWobblesPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo fixedUpdate = typeof(Vehicle).GetMethod("FixedUpdate",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)fixedUpdate == null)
                { MelonLogger.Warning("[GameMod] NoSpeedWobbles: Vehicle.FixedUpdate not found."); return; }
                MethodInfo postfix = typeof(NoSpeedWobbles_Patch).GetMethod("Postfix",
                    BindingFlags.Public | BindingFlags.Static);
                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(postfix));
                MelonLogger.Msg("[GameMod] NoSpeedWobbles Vehicle patch applied.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[GameMod] NoSpeedWobbles Vehicle patch: " + ex.Message); }

            // Patch BikeCamera.FixedUpdate — calls RemoveCameraShake() every frame
            // to zero VgM\u007Fk\u0080u (shake velocity) and OXXnhI\u0081 (shake offset).
            // BikeCamera.FixedUpdate always calls the active camera mode function,
            // so this covers every camera view with a single patch.
            try
            {
                System.Type bikeCamType = typeof(BikeCamera);
                MethodInfo camFixedUpdate = bikeCamType.GetMethod("FixedUpdate",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)camFixedUpdate == null)
                { MelonLogger.Warning("[GameMod] NoSpeedWobbles: BikeCamera.FixedUpdate not found."); return; }
                MethodInfo camPostfix = typeof(NoSpeedWobbles_CamPatch).GetMethod("Postfix",
                    BindingFlags.Public | BindingFlags.Static);
                harmony.Patch(camFixedUpdate, postfix: new HarmonyMethod(camPostfix));
                MelonLogger.Msg("[GameMod] NoSpeedWobbles BikeCamera patch applied.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[GameMod] NoSpeedWobbles BikeCamera patch: " + ex.Message); }
        }

        public static void ApplyMod(string modName, int level)
        {
            try
            {
                GameData gameData = UnityEngine.Object.FindObjectOfType<GameData>();
                if ((object)gameData == null) { MelonLogger.Warning("[GameMod] GameData not found."); return; }
                FieldInfo modArrayField = gameData.GetType().GetField("\u0081jU\u0080h\u0084c",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)modArrayField == null) { MelonLogger.Warning("[GameMod] Mod array field not found."); return; }
                GameModifier[] mods = modArrayField.GetValue(gameData) as GameModifier[];
                if ((object)mods == null) { MelonLogger.Warning("[GameMod] Mod array is null."); return; }
                GameModifier target = null;
                for (int i = 0; i < mods.Length; i++)
                    if ((object)mods[i] != null && mods[i].name == modName)
                    { target = mods[i]; break; }
                if ((object)target == null) { MelonLogger.Warning("[GameMod] Modifier not found: " + modName); return; }
                float value = modName == "OFFROADFRICTION" ? IceMult(level) : Mult(level);
                target.modifiers[0].percentageValue = value;
                PlayerManager pm = UnityEngine.Object.FindObjectOfType<PlayerManager>();
                if ((object)pm == null) { MelonLogger.Warning("[GameMod] PlayerManager not found."); return; }
                PlayerInfoImpact pi = pm.GetPlayerImpact();
                if ((object)pi == null) { MelonLogger.Warning("[GameMod] PlayerInfoImpact not found."); return; }
                pi.AddGameModifier(target);
                MelonLogger.Msg("[GameMod] " + modName + " level " + level + " (" + value + ")");
            }
            catch (System.Exception ex) { MelonLogger.Error("[GameMod] ApplyMod " + modName + ": " + ex.Message); }
        }
    }

    // Postfix on Vehicle.FixedUpdate — directly zeros steering wobble property
    // Z\u0082kM\u005DJM when NoSpeedWobbles is enabled. This fires every physics frame
    // so it can't be overwritten by PlayerInfoImpact resets.
    public static class NoSpeedWobbles_Patch
    {
        private static PropertyInfo _wobbleProp = null;
        private static bool _cached = false;

        public static void Postfix(Vehicle __instance)
        {
            if (!GameModifierMods.NoSpeedWobblesEnabled) return;
            if ((object)__instance == null) return;

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
                        MelonLogger.Msg("[GameMod] NoSpeedWobbles found property: " + props[i].Name);
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
        // Remove all camera-shake vectors produced by the speed wobble system.
        // These are separate from the CameraAngle.cameraShake field.
        private static FieldInfo _shakeVel = null;   // VgM\u007Fk\u0080u  — Vector3
        private static FieldInfo _shakeOff = null;   // OXXnhI\u0081  — Vector3
        private static bool _cached = false;

        public static void Postfix(BikeCamera __instance)
        {
            if (!GameModifierMods.NoSpeedWobblesEnabled) return;
            if ((object)__instance == null) return;

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