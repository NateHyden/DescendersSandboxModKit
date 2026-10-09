using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using DescendersModMenu;
using InControl;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// When ON, the vanilla Set Respawn Point bind works while airborne or moving.
    /// Detects Modifier + Set Respawn Point on the local VehicleController and calls
    /// SetSessionMarker() directly — skips grounded / on-bike / accel gates.
    /// </summary>
    public static class SessionMarker
    {
        public static bool Enabled { get; private set; } = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("[Respawn Point] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void Reset()
        {
            Enabled = false;
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo update = typeof(VehicleController).GetMethod(
                    "Update", BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)update == null)
                {
                    ModLog.Warn("[Respawn Point] VehicleController.Update not found.");
                    return;
                }

                harmony.Patch(update,
                    prefix: new HarmonyMethod(typeof(SessionMarker_Patch).GetMethod("Prefix")));

                ModLog.Debug("[Respawn Point] Patched VehicleController.Update (Prefix only).");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[Respawn Point] ApplyPatch: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "SessionMarker");
            }
        }
    }

    public static class SessionMarker_Patch
    {
        private static bool _resolved;
        private static FieldInfo _piiField;
        private static FieldInfo _actionsField;
        private static MethodInfo _setSessionMarker;
        private static MethodInfo _getActionByName;

        private static VehicleController _localVc;
        private static float _nextLocalFind;
        private static int _lastFireFrame = -1;

        private static bool IsLocalController(VehicleController vc)
        {
            if (!UnityNull.Alive(_localVc))
            {
                float now = Time.unscaledTime;
                if (now < _nextLocalFind) return false;
                _nextLocalFind = now + 1f;
                GameObject go = GameObject.Find("Player_Human");
                _localVc = UnityNull.Alive(go) ? go.GetComponent<VehicleController>() : null;
                if (!UnityNull.Alive(_localVc) && UnityNull.Alive(go))
                    _localVc = go.GetComponentInChildren<VehicleController>();
            }
            return (object)vc == (object)_localVc;
        }

        public static void Prefix(VehicleController __instance)
        {
            if (!SessionMarker.Enabled) return;
            if (!UnityNull.Alive(__instance)) return;
            if (!IsLocalController(__instance)) return;

            try
            {
                EnsureResolved();
                if ((object)_setSessionMarker == null
                    || (object)_piiField == null
                    || (object)_actionsField == null
                    || (object)_getActionByName == null)
                    return;

                object actions = _actionsField.GetValue(__instance);
                if ((object)actions == null) return;

                PlayerAction modifier = _getActionByName.Invoke(actions, new object[] { "Modifier" }) as PlayerAction;
                PlayerAction setRespawn = _getActionByName.Invoke(actions, new object[] { "Set Respawn Point" }) as PlayerAction;
                if ((object)modifier == null || (object)setRespawn == null) return;
                if (!modifier.IsPressed || !setRespawn.WasPressed) return;

                int frame = Time.frameCount;
                if (frame == _lastFireFrame) return;
                _lastFireFrame = frame;

                object pii = _piiField.GetValue(__instance);
                if ((object)pii == null) return;

                _setSessionMarker.Invoke(pii, null);
                ModLog.Debug("[Respawn Point] SetSessionMarker (anywhere).");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[Respawn Point] Prefix: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "SessionMarker");
            }
        }

        private static void EnsureResolved()
        {
            if (_resolved) return;
            _resolved = true;

            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            FieldInfo[] vcFields = typeof(VehicleController).GetFields(bf);
            for (int i = 0; i < vcFields.Length; i++)
            {
                FieldInfo f = vcFields[i];
                if ((object)_piiField == null
                    && string.Equals(f.FieldType.Name, "PlayerInfoImpact", System.StringComparison.Ordinal))
                    _piiField = f;
                else if ((object)_actionsField == null
                    && typeof(PlayerActionSet).IsAssignableFrom(f.FieldType))
                    _actionsField = f;
            }

            if ((object)_piiField == null)
                ModLog.Warn("[Respawn Point] PlayerInfoImpact field not found on VehicleController.");
            if ((object)_actionsField == null)
                ModLog.Warn("[Respawn Point] PlayerActionSet field not found on VehicleController.");

            _setSessionMarker = typeof(PlayerInfoImpact).GetMethod(
                "SetSessionMarker", BindingFlags.Public | BindingFlags.Instance);
            if ((object)_setSessionMarker == null)
                ModLog.Warn("[Respawn Point] SetSessionMarker not found.");

            _getActionByName = typeof(PlayerActionSet).GetMethod(
                "GetPlayerActionByName",
                BindingFlags.Public | BindingFlags.Instance,
                null, new System.Type[] { typeof(string) }, null);
            if ((object)_getActionByName == null)
                ModLog.Warn("[Respawn Point] GetPlayerActionByName not found.");

            if ((object)_piiField != null
                && (object)_actionsField != null
                && (object)_setSessionMarker != null
                && (object)_getActionByName != null)
                ModLog.Debug("[Respawn Point] Resolved PII + actions + SetSessionMarker.");
        }
    }
}
