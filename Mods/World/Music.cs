using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Enabled = true means music is MUTED
    public static class Music
    {
        public static bool Enabled = false;
        private static float _savedVolume = 1f;

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(!Enabled); // true = play, false = mute
        }

        public static void Apply(bool play)
        {
            try
            {
                AudioManager mgr = Object.FindObjectOfType<AudioManager>();
                if ((object)mgr == null) { MelonLogger.Warning("[Music] AudioManager not found."); return; }

                System.Reflection.MethodInfo setMethod = mgr.GetType().GetMethod(
                    "SetCategoryVolume",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)setMethod == null) { MelonLogger.Warning("[Music] SetCategoryVolume not found."); return; }

                System.Reflection.ParameterInfo[] parms = setMethod.GetParameters();
                if (parms.Length < 2) { MelonLogger.Warning("[Music] Unexpected param count: " + parms.Length); return; }

                System.Type enumType = parms[0].ParameterType;
                object musicEnum = System.Enum.ToObject(enumType, 1);

                if (!play)
                {
                    System.Reflection.MethodInfo getMethod = mgr.GetType().GetMethod(
                        "GetCategoryVolume",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if ((object)getMethod != null)
                    {
                        object vol = getMethod.Invoke(mgr, new object[] { musicEnum });
                        if (vol is float) _savedVolume = (float)vol;
                    }
                    setMethod.Invoke(mgr, new object[] { musicEnum, 0f });
                    MelonLogger.Msg("[Music] Muted. Saved volume: " + _savedVolume);
                }
                else
                {
                    setMethod.Invoke(mgr, new object[] { musicEnum, _savedVolume });
                    MelonLogger.Msg("[Music] Restored volume: " + _savedVolume);
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[Music] Apply: " + ex.Message); }
        }

        public static void Reset()
        {
            if (Enabled) { Enabled = false; Apply(true); }
        }
    }
}
