using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using DescendersModMenu; // Telemetry

namespace DescendersModMenu.Mods
{
    // CompassAlwaysOn — forces the game's own finish-line compass to stay
    // visible every frame, regardless of whether the player has picked up
    // the "Show Compass" Blue crew member perk this run (normally the only
    // way it appears outside of NoPath maps).
    //
    // Patches UI_InGame.UpdateCompass() with a Postfix — clean method name,
    // won't break on obfuscation. The original runs first and may hide the
    // compass container if the player lacks the perk; our postfix then
    // re-shows it and re-runs the same positioning math the game uses
    // (sourced from the decompile), driven off FinishLine + Player_Human
    // instead of the game's own private cached refs.
    public static class CompassAlwaysOn
    {
        public static bool Enabled { get; private set; } = false;

        private static FinishLine _finishLine;

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("[CompassAlwaysOn] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void Reset()
        {
            Enabled = false;
        }

        // FinishLine is scene-scoped — drop the cached ref on scene unload,
        // Postfix re-finds it lazily next time it's needed.
        public static void ClearCache()
        {
            _finishLine = null;
        }

        public static FinishLine GetFinishLine()
        {
            // Unity's own == here, not (object) — same reasoning as the icon
            // checks in the Postfix below. This is exactly the reference the
            // Discord error report pointed at: Component:get_transform()
            // throwing NullReferenceException from inside this method's
            // caller, because a destroyed-but-not-truly-null FinishLine from
            // the previous map was passing right through the old (object)
            // cast check and getting cached as if it were still valid.
            if (_finishLine == null) _finishLine = Object.FindObjectOfType<FinishLine>();
            return _finishLine;
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo updateCompass = typeof(UI_InGame).GetMethod(
                    "UpdateCompass", BindingFlags.Public | BindingFlags.Instance);

                if ((object)updateCompass == null)
                { ModLog.Warn("[CompassAlwaysOn] UI_InGame.UpdateCompass not found."); return; }

                MethodInfo postfix = typeof(CompassAlwaysOn_Patch).GetMethod(
                    "Postfix", BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(updateCompass, postfix: new HarmonyMethod(postfix));
                ModLog.Debug("[CompassAlwaysOn] Patched UI_InGame.UpdateCompass.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[CompassAlwaysOn] ApplyPatch: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "CompassAlwaysOn"); }
        }
    }

    public static class CompassAlwaysOn_Patch
    {
        // Icon field name on UI_InGame contains control-range chars (0x7F, 0x7C)
        // — not a compilable literal identifier, so it's looked up by exact
        // name via reflection, per project convention. Confirmed against the
        // decompile 2026-08. If this update breaks, re-scan UI_InGame for the
        // Image field referenced inside UpdateCompass() — see How_to_fix_after_update.md.
        private static readonly string IconFieldName = ((char)0x7F) + "cDpHh" + ((char)0x7C);

        private static FieldInfo _iconField;
        private static bool _searched = false;

        public static void Postfix(UI_InGame __instance)
        {
            if (!CompassAlwaysOn.Enabled) return;
            if ((object)__instance == null) return;

            try
            {
                if (!_searched)
                {
                    _searched = true;
                    _iconField = __instance.GetType().GetField(IconFieldName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    ModLog.Debug("[CompassAlwaysOn] icon field = "
                        + ((object)_iconField != null ? "found (" + _iconField.FieldType.Name + ")" : "NOT FOUND"));
                }
                if ((object)_iconField == null) return;

                Image icon = _iconField.GetValue(__instance) as Image;
                // Unity's own == here, deliberately NOT the (object) cast used
                // everywhere else in this codebase. (object) checks only catch
                // "never assigned" — they don't catch "destroyed" (Unity's fake-
                // null pattern: the C# reference is non-null but the underlying
                // native object is gone). Mid scene-transition, on "starting a
                // new map", that's exactly the state this can be in, and only
                // Unity's overloaded == correctly detects it.
                if (icon == null) return;
                if (icon.transform == null || icon.transform.parent == null) return;

                FinishLine finish = CompassAlwaysOn.GetFinishLine();
                if (finish == null) { icon.gameObject.SetActive(false); return; }

                icon.transform.parent.gameObject.SetActive(true);

                if ((object)Camera.main == null) return;

                Vector3 finishFlat = finish.transform.position;
                finishFlat.y = 0f;
                Vector3 normalized = (finishFlat - Camera.main.transform.position).normalized;

                float dot = Vector3.Dot(Camera.main.transform.forward, normalized);
                Vector3 cross = Vector3.Cross(Camera.main.transform.forward, normalized);

                float x = 500f * cross.y;
                if (x > 350f) x = 350f;
                else if (x < -350f) x = -350f;
                icon.rectTransform.anchoredPosition = new Vector2(x, 0f);

                GameObject player = GameObject.Find("Player_Human");
                var distTxt = __instance.UspzMHw;
                if ((object)player != null && distTxt != null)
                {
                    distTxt.text = Mathf.FloorToInt(
                        (finish.transform.position - player.transform.position).magnitude) + "m";
                }

                icon.gameObject.SetActive(dot >= 0f);
            }
            catch (UnityEngine.MissingReferenceException)
            {
                // Expected/benign: a reference from the scene we just left
                // got destroyed a frame or two before ClearCache() ran.
                // Transient — next frame re-fetches everything fresh. Not
                // a real bug, so no log spam and no telemetry report.
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[CompassAlwaysOn] Postfix: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CompassAlwaysOn");
            }
        }
    }
}
