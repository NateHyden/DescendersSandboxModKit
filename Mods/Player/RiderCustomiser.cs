using MelonLoader;
using System;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Rider customisation - skin/hair/beard colour, hair/beard/body style.
    ///
    /// PlayerCustomization.SetSkinColor/SetHairColor/SetHairType/SetBodyType/SetBeardType/
    /// SetBeardColor are all real, clean, working methods - confirmed directly in the
    /// decompile, not guessed. Found on Player_Human's PlayerCustomization component, same
    /// GameObject.Find("Player_Human") pattern already proven elsewhere in this project
    /// (ESP's player lookup, BikeSwitcher).
    ///
    /// There's no clean "get current value" getter for any of these - the actual current
    /// index lives inside a RiderCustomizations data object the game itself owns, behind
    /// obfuscated fields with no safe public read path. So this tracks its own level per
    /// option, same pattern PlayerSize/CameraShake already use for things the game doesn't
    /// expose a getter for, rather than guessing at a reflection path into game state.
    ///
    /// Range is a generous fixed 0-19 rather than reading the real per-option count from
    /// GameData's ColorPalette/RiderHairTypes assets (which would need another layer of
    /// reflection to reach). Each setter's own bounds-checking already handles an index past
    /// the real count safely - confirmed in the decompile, e.g. ColorPalette's accessor
    /// returns a default colour instead of throwing. Worst case an option does nothing
    /// visually past the real max, not a crash.
    /// </summary>
    public static class RiderCustomiser
    {
        public const int MinLevel = 0;
        public const int MaxLevel = 19;

        public static int SkinColorLevel = 0;
        public static int HairColorLevel = 0;
        public static int HairTypeLevel = 0;
        public static int BeardColorLevel = 0;
        public static int BeardTypeLevel = 0;
        public static int BodyTypeLevel = 0;

        private static PlayerCustomization GetLocalCustomization()
        {
            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) return null;
            return player.GetComponent<PlayerCustomization>();
        }

        // ── Skin Colour ──────────────────────────────────────────────
        public static void IncreaseSkinColor() { SkinColorLevel = Mathf.Min(MaxLevel, SkinColorLevel + 1); ApplySkinColor(); }
        public static void DecreaseSkinColor() { SkinColorLevel = Mathf.Max(MinLevel, SkinColorLevel - 1); ApplySkinColor(); }
        public static void ApplySkinColor()
        {
            PlayerCustomization pc = GetLocalCustomization();
            if ((object)pc == null) { MelonLogger.Warning("[RiderCustomiser] Player_Human/PlayerCustomization not found."); return; }
            try { pc.SetSkinColor(SkinColorLevel); }
            catch (Exception ex) { MelonLogger.Error("[RiderCustomiser] SetSkinColor: " + ex.Message); }
        }

        // ── Hair Colour ──────────────────────────────────────────────
        public static void IncreaseHairColor() { HairColorLevel = Mathf.Min(MaxLevel, HairColorLevel + 1); ApplyHairColor(); }
        public static void DecreaseHairColor() { HairColorLevel = Mathf.Max(MinLevel, HairColorLevel - 1); ApplyHairColor(); }
        public static void ApplyHairColor()
        {
            PlayerCustomization pc = GetLocalCustomization();
            if ((object)pc == null) { MelonLogger.Warning("[RiderCustomiser] Player_Human/PlayerCustomization not found."); return; }
            try { pc.SetHairColor(HairColorLevel); }
            catch (Exception ex) { MelonLogger.Error("[RiderCustomiser] SetHairColor: " + ex.Message); }
        }

        // ── Hair Type ────────────────────────────────────────────────
        public static void IncreaseHairType() { HairTypeLevel = Mathf.Min(MaxLevel, HairTypeLevel + 1); ApplyHairType(); }
        public static void DecreaseHairType() { HairTypeLevel = Mathf.Max(MinLevel, HairTypeLevel - 1); ApplyHairType(); }
        public static void ApplyHairType()
        {
            PlayerCustomization pc = GetLocalCustomization();
            if ((object)pc == null) { MelonLogger.Warning("[RiderCustomiser] Player_Human/PlayerCustomization not found."); return; }
            try { pc.SetHairType(HairTypeLevel, false); }
            catch (Exception ex) { MelonLogger.Error("[RiderCustomiser] SetHairType: " + ex.Message); }
        }

        // ── Beard Colour ─────────────────────────────────────────────
        public static void IncreaseBeardColor() { BeardColorLevel = Mathf.Min(MaxLevel, BeardColorLevel + 1); ApplyBeardColor(); }
        public static void DecreaseBeardColor() { BeardColorLevel = Mathf.Max(MinLevel, BeardColorLevel - 1); ApplyBeardColor(); }
        public static void ApplyBeardColor()
        {
            PlayerCustomization pc = GetLocalCustomization();
            if ((object)pc == null) { MelonLogger.Warning("[RiderCustomiser] Player_Human/PlayerCustomization not found."); return; }
            try { pc.SetBeardColor(BeardColorLevel); }
            catch (Exception ex) { MelonLogger.Error("[RiderCustomiser] SetBeardColor: " + ex.Message); }
        }

        // ── Beard Type ───────────────────────────────────────────────
        public static void IncreaseBeardType() { BeardTypeLevel = Mathf.Min(MaxLevel, BeardTypeLevel + 1); ApplyBeardType(); }
        public static void DecreaseBeardType() { BeardTypeLevel = Mathf.Max(MinLevel, BeardTypeLevel - 1); ApplyBeardType(); }
        public static void ApplyBeardType()
        {
            PlayerCustomization pc = GetLocalCustomization();
            if ((object)pc == null) { MelonLogger.Warning("[RiderCustomiser] Player_Human/PlayerCustomization not found."); return; }
            try { pc.SetBeardType(BeardTypeLevel, false); }
            catch (Exception ex) { MelonLogger.Error("[RiderCustomiser] SetBeardType: " + ex.Message); }
        }

        // ── Body Type ────────────────────────────────────────────────
        public static void IncreaseBodyType() { BodyTypeLevel = Mathf.Min(MaxLevel, BodyTypeLevel + 1); ApplyBodyType(); }
        public static void DecreaseBodyType() { BodyTypeLevel = Mathf.Max(MinLevel, BodyTypeLevel - 1); ApplyBodyType(); }
        public static void ApplyBodyType()
        {
            PlayerCustomization pc = GetLocalCustomization();
            if ((object)pc == null) { MelonLogger.Warning("[RiderCustomiser] Player_Human/PlayerCustomization not found."); return; }
            try { pc.SetBodyType(BodyTypeLevel); }
            catch (Exception ex) { MelonLogger.Error("[RiderCustomiser] SetBodyType: " + ex.Message); }
        }

        // ── Reset all to default (0) ────────────────────────────────
        public static void ResetAll()
        {
            SkinColorLevel = HairColorLevel = HairTypeLevel = BeardColorLevel = BeardTypeLevel = BodyTypeLevel = 0;
            ApplySkinColor();
            ApplyHairColor();
            ApplyHairType();
            ApplyBeardColor();
            ApplyBeardType();
            ApplyBodyType();
        }
    }
}
