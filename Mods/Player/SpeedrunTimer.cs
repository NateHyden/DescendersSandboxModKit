using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class SpeedrunTimer
    {
        public static bool Enabled { get; private set; } = false;

        private static UI_SpeedrunTimer _timer = null;

        // Session data field refs for reset
        // Singleton<SessionManager>.[\u007EqsVD|.\u0083ESVMoz.skY\u0080uhC = the elapsed time (double)
        private static FieldInfo _sessionDataField = null;  // \u0083ESVMoz on SessionManager
        private static FieldInfo _timeField = null;  // skY\u0080uhC on session data

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
            MelonLogger.Msg("[SpeedrunTimer] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void Apply()
        {
            try
            {
                if (Enabled)
                {
                    if ((object)_timer == null)
                    {
                        UI_SpeedrunTimer[] found = Resources.FindObjectsOfTypeAll<UI_SpeedrunTimer>();
                        if (found != null && found.Length > 0)
                        {
                            _timer = found[0];
                            MelonLogger.Msg("[SpeedrunTimer] Found existing instance.");
                        }
                    }

                    if ((object)_timer == null)
                    {
                        UIManager uiManager = Object.FindObjectOfType<UIManager>();
                        if ((object)uiManager != null)
                        {
                            _timer = uiManager.SpawnByType<UI_SpeedrunTimer>();
                            if ((object)_timer != null)
                                MelonLogger.Msg("[SpeedrunTimer] Spawned via UIManager.");
                            else
                                MelonLogger.Warning("[SpeedrunTimer] SpawnByType returned null.");
                        }
                        else
                            MelonLogger.Warning("[SpeedrunTimer] UIManager not found.");
                    }

                    if ((object)_timer != null)
                        _timer.gameObject.SetActive(true);
                }
                else
                {
                    if ((object)_timer != null)
                        _timer.gameObject.SetActive(false);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SpeedrunTimer] Apply: " + ex.Message);
            }
        }

        public static void ResetTime()
        {
            try
            {
                // Get SessionManager instance via FindObjectOfType
                SessionManager sm = Object.FindObjectOfType<SessionManager>();
                if ((object)sm == null) { MelonLogger.Warning("[SpeedrunTimer] SessionManager not found."); return; }

                // Cache \u0083ESVMoz field (session data object on SessionManager)
                if ((object)_sessionDataField == null)
                {
                    FieldInfo[] fields = typeof(SessionManager).GetFields(
                        BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        // \u0083ESVMoz — look for the field whose type has a public double skY\u0080uhC
                        FieldInfo[] innerFields = fields[i].FieldType.GetFields(
                            BindingFlags.Public | BindingFlags.Instance);
                        for (int j = 0; j < innerFields.Length; j++)
                        {
                            if (string.Equals(innerFields[j].Name, "skY\u0080uh\u007C",
                                System.StringComparison.Ordinal) &&
                                string.Equals(innerFields[j].FieldType.Name, "Double",
                                System.StringComparison.Ordinal))
                            {
                                _sessionDataField = fields[i];
                                _timeField = innerFields[j];
                                MelonLogger.Msg("[SpeedrunTimer] Found session data + time field.");
                                break;
                            }
                        }
                        if ((object)_sessionDataField != null) break;
                    }
                }

                if ((object)_sessionDataField == null || (object)_timeField == null)
                {
                    MelonLogger.Warning("[SpeedrunTimer] Could not find time field to reset.");
                    return;
                }

                object sessionData = _sessionDataField.GetValue(sm);
                if ((object)sessionData == null) return;

                _timeField.SetValue(sessionData, 0.0);
                MelonLogger.Msg("[SpeedrunTimer] Timer reset to 0.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SpeedrunTimer] ResetTime: " + ex.Message);
            }
        }

        public static void Reset()
        {
            Enabled = false;
            _timer = null;
            _sessionDataField = null;
            _timeField = null;
        }
    }
}