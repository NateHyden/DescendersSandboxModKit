using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class TurboWind
    {
        public static bool Enabled = false;

        private static float _savedWindMain = -1f;
        private static System.Type _windZoneType = null;
        private static System.Reflection.PropertyInfo _windMainProp = null;
        private static System.Reflection.PropertyInfo _windTurbProp = null;

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(Enabled);
        }

        public static void Apply(bool enabled)
        {
            try
            {
                if ((object)_windZoneType == null)
                {
                    System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                    for (int a = 0; a < assemblies.Length; a++)
                    {
                        _windZoneType = assemblies[a].GetType("UnityEngine.WindZone");
                        if ((object)_windZoneType != null) break;
                    }
                }
                if ((object)_windZoneType == null) return;
                Object wz = GameObject.FindObjectOfType(_windZoneType);
                if ((object)wz == null) return;
                if ((object)_windMainProp == null)
                    _windMainProp = _windZoneType.GetProperty("windMain", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)_windTurbProp == null)
                    _windTurbProp = _windZoneType.GetProperty("windTurbulence", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (enabled)
                {
                    if (_savedWindMain < 0f && (object)_windMainProp != null)
                        _savedWindMain = (float)_windMainProp.GetValue(wz, null);
                    if ((object)_windMainProp != null) _windMainProp.SetValue(wz, 50f, null);
                    if ((object)_windTurbProp != null) _windTurbProp.SetValue(wz, 1f, null);
                    MelonLogger.Msg("[TurboWind] ON");
                }
                else
                {
                    if ((object)_windMainProp != null) _windMainProp.SetValue(wz, _savedWindMain >= 0f ? _savedWindMain : 1f, null);
                    if ((object)_windTurbProp != null) _windTurbProp.SetValue(wz, 0.5f, null);
                    MelonLogger.Msg("[TurboWind] OFF");
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[TurboWind] Apply: " + ex.Message); }
        }

        public static void Reset()
        {
            if (Enabled) { Enabled = false; Apply(false); }
        }
    }
}
