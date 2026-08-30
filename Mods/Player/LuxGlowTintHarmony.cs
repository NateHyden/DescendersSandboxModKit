using HarmonyLib;

public static class LuxGlowTintHueAnimatePatch
{
    public static bool Prefix(HueAnimate __instance)
    {
        return !DescendersModMenu.Mods.LuxGlowTint.IsHueAnimateFrozen(__instance);
    }
}
