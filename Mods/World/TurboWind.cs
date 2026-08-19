using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class TurboWind
    {
        public static bool Enabled = false;
        public static int PowerLevel { get; private set; } = 3;

        private const float PushForceMin = 2f;
        private const float PushForceMax = 28f;
        private const float LateralSwayMin = 0.5f;
        private const float LateralSwayMax = 8f;
        private const float WindMainMin = 8f;
        private const float WindMainMax = 80f;
        private const float WindTurbMin = 0.4f;
        private const float WindTurbMax = 2.5f;
        private const float ReassertInterval = 1.5f;

        private static float PowerT { get { return (PowerLevel - 1) / 9f; } }
        private static float PushForce { get { return Mathf.Lerp(PushForceMin, PushForceMax, PowerT); } }
        private static float LateralSway { get { return Mathf.Lerp(LateralSwayMin, LateralSwayMax, PowerT); } }
        private static float WindMainBoost { get { return Mathf.Lerp(WindMainMin, WindMainMax, PowerT); } }
        private static float WindTurbBoost { get { return Mathf.Lerp(WindTurbMin, WindTurbMax, PowerT); } }

        public static string PowerDisplay { get { return PowerLevel.ToString(); } }

        private static float _savedWindMain = -1f;
        private static float _savedWindTurb = -1f;
        private static float _savedGrassStrength = -1f;
        private static float _savedGrassSpeed = -1f;
        private static float _reassertTimer = 0f;
        private static float _swayPhase = 0f;

        private static System.Type _windZoneType = null;
        private static System.Reflection.PropertyInfo _windMainProp = null;
        private static System.Reflection.PropertyInfo _windTurbProp = null;

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(Enabled);
            _reassertTimer = 0f;
            ModLog.Feedback("[TurboWind] -> " + (Enabled ? "ON (power " + PowerLevel + ")" : "OFF"));
        }

        public static void IncreasePower()
        {
            if (PowerLevel >= 10) return;
            PowerLevel++;
            if (Enabled) Apply(true);
            ModLog.Feedback("[TurboWind] Power -> " + PowerLevel);
        }

        public static void DecreasePower()
        {
            if (PowerLevel <= 1) return;
            PowerLevel--;
            if (Enabled) Apply(true);
            ModLog.Feedback("[TurboWind] Power -> " + PowerLevel);
        }

        public static void SetPowerLevel(int level)
        {
            PowerLevel = Mathf.Clamp(level, 1, 10);
            if (Enabled) Apply(true);
        }

        public static void FixedTick()
        {
            if (!Enabled) return;
            try
            {
                GameObject player = PlayerCache.PlayerHuman;
                if ((object)player == null || player == null) return;
                Rigidbody rb = player.GetComponent<Rigidbody>();
                if ((object)rb == null) return;

                _swayPhase += Time.fixedDeltaTime * 0.7f;
                Vector3 force = new Vector3(
                    Mathf.Sin(_swayPhase) * LateralSway,
                    0f,
                    PushForce);
                rb.AddForce(force, ForceMode.Acceleration);
            }
            catch { }
        }

        public static void Tick()
        {
            if (!Enabled) return;
            _reassertTimer += Time.unscaledDeltaTime;
            if (_reassertTimer < ReassertInterval) return;
            _reassertTimer = 0f;
            ApplyWindZones(true);
            ApplyTerrainWind(true);
        }

        public static void Apply(bool enabled)
        {
            try
            {
                ApplyWindZones(enabled);
                ApplyTerrainWind(enabled);
                if (!enabled)
                {
                    _savedWindMain = -1f;
                    _savedWindTurb = -1f;
                    _savedGrassStrength = -1f;
                    _savedGrassSpeed = -1f;
                }
                ModLog.Debug("[TurboWind] Apply enabled=" + enabled + " power=" + PowerLevel);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[TurboWind] Apply: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "TurboWind");
            }
        }

        private static void EnsureWindZoneReflection()
        {
            if ((object)_windZoneType != null) return;
            System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < assemblies.Length; a++)
            {
                _windZoneType = assemblies[a].GetType("UnityEngine.WindZone");
                if ((object)_windZoneType != null) break;
            }
            if ((object)_windZoneType == null) return;
            _windMainProp = _windZoneType.GetProperty("windMain",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            _windTurbProp = _windZoneType.GetProperty("windTurbulence",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        }

        private static void ApplyWindZones(bool enabled)
        {
            EnsureWindZoneReflection();
            if ((object)_windZoneType == null) return;

            Object[] zones = Object.FindObjectsOfType(_windZoneType);
            if (zones == null || zones.Length == 0)
            {
                if (enabled)
                {
                    GameObject go = new GameObject("Sandbox_TurboWindZone");
                    Object.DontDestroyOnLoad(go);
                    Component wz = go.AddComponent(_windZoneType);
                    if ((object)_windMainProp != null) _windMainProp.SetValue(wz, WindMainBoost, null);
                    if ((object)_windTurbProp != null) _windTurbProp.SetValue(wz, WindTurbBoost, null);
                    ModLog.Debug("[TurboWind] Spawned WindZone.");
                }
                return;
            }

            for (int i = 0; i < zones.Length; i++)
            {
                Object wz = zones[i];
                if ((object)wz == null) continue;
                if (enabled)
                {
                    if (_savedWindMain < 0f && (object)_windMainProp != null)
                        _savedWindMain = (float)_windMainProp.GetValue(wz, null);
                    if (_savedWindTurb < 0f && (object)_windTurbProp != null)
                        _savedWindTurb = (float)_windTurbProp.GetValue(wz, null);
                    if ((object)_windMainProp != null) _windMainProp.SetValue(wz, WindMainBoost, null);
                    if ((object)_windTurbProp != null) _windTurbProp.SetValue(wz, WindTurbBoost, null);
                }
                else
                {
                    if ((object)_windMainProp != null)
                        _windMainProp.SetValue(wz, _savedWindMain >= 0f ? _savedWindMain : 1f, null);
                    if ((object)_windTurbProp != null)
                        _windTurbProp.SetValue(wz, _savedWindTurb >= 0f ? _savedWindTurb : 0.5f, null);
                    Component c = wz as Component;
                    if ((object)c != null && c != null && c.gameObject != null
                        && string.Equals(c.gameObject.name, "Sandbox_TurboWindZone", System.StringComparison.Ordinal))
                        Object.Destroy(c.gameObject);
                }
            }
        }

        private static void ApplyTerrainWind(bool enabled)
        {
            Terrain[] terrains = Object.FindObjectsOfType<Terrain>();
            if (terrains == null) return;
            float grassStr = Mathf.Lerp(0.4f, 1.2f, PowerT);
            float grassSpd = Mathf.Lerp(0.35f, 1.0f, PowerT);
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain t = terrains[i];
                if ((object)t == null || t == null) continue;
                TerrainData td = t.terrainData;
                if ((object)td == null || td == null) continue;
                try
                {
                    if (enabled)
                    {
                        if (_savedGrassStrength < 0f) _savedGrassStrength = td.wavingGrassStrength;
                        if (_savedGrassSpeed < 0f) _savedGrassSpeed = td.wavingGrassSpeed;
                        td.wavingGrassStrength = Mathf.Max(td.wavingGrassStrength, grassStr);
                        td.wavingGrassSpeed = Mathf.Max(td.wavingGrassSpeed, grassSpd);
                    }
                    else
                    {
                        if (_savedGrassStrength >= 0f) td.wavingGrassStrength = _savedGrassStrength;
                        if (_savedGrassSpeed >= 0f) td.wavingGrassSpeed = _savedGrassSpeed;
                    }
                }
                catch { }
            }
        }

        public static void Reset()
        {
            if (Enabled) { Enabled = false; Apply(false); }
            PowerLevel = 3;
            _reassertTimer = 0f;
            _swayPhase = 0f;
        }
    }
}

