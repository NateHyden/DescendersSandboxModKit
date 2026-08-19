using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
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

        public static void ClearCache()
        {
            _finishLine = null;
        }

        public static FinishLine GetFinishLine()
        {
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
        private static readonly string IconFieldName = ((char)0x7F) + "cDpHh" + ((char)0x7C);

        private static FieldInfo _iconField;
        private static bool _searched = false;

        public static void Postfix(UI_InGame __instance)
        {
            if (!CompassAlwaysOn.Enabled) return;
            if (!UnityNull.Alive(__instance)) return;

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
                if (icon == null) return;
                if (icon.transform == null || icon.transform.parent == null) return;

                FinishLine finish = CompassAlwaysOn.GetFinishLine();
                if (finish == null) { icon.gameObject.SetActive(false); return; }

                icon.transform.parent.gameObject.SetActive(true);

                if (!UnityNull.Alive(Camera.main)) return;

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
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[CompassAlwaysOn] Postfix: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "CompassAlwaysOn");
            }
        }
    }
}

