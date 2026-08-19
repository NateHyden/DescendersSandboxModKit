using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using System.Reflection;

namespace DescendersModMenu.Mods
{
    public static class TimeOfDay
    {
        private static readonly float[] Hours = {
            6f, 8f, 10f, 12f, 14f, 16f, 17.5f, 19f, 20.5f, 22f
        };
        private static readonly string[] Labels = {
            "Dawn", "Morning", "Mid AM", "Noon", "Afternoon",
            "Late PM", "Evening", "Dusk", "Twilight", "Night"
        };

        public static int Level { get; private set; } = 4;
        private static int _sceneDefaultLevel = 4;
        private static bool _sceneDefaultCaptured = false;

        private static MonoBehaviour _sky;
        private static FieldInfo _cycleField;
        private static FieldInfo _hourField;
        private static PropertyInfo _hourProp;
        private static bool _resolved;

        public static string DisplayValue { get { return Labels[Level - 1]; } }

        public static void Increase() { if (Level < 10) { Level++; Apply(); } }
        public static void Decrease() { if (Level > 1) { Level--; Apply(); } }
        public static void SetLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > 10) level = 10;
            Level = level;
            Apply();
        }

        public static void ResetToSceneDefault()
        {
            if (!_sceneDefaultCaptured)
                CaptureSceneDefault();
            SetLevel(_sceneDefaultLevel);
        }

        public static void ClearCache()
        {
            _sky = null;
            _cycleField = null;
            _hourField = null;
            _hourProp = null;
            _resolved = false;
            _sceneDefaultCaptured = false;
        }

        public static void CaptureSceneDefault()
        {
            _sceneDefaultCaptured = false;
            try
            {
                if (!EnsureSky())
                    return;

                object cycle = _cycleField.GetValue(_sky);
                if ((object)cycle == null) return;

                float hour = ReadHour(cycle);
                int best = 4;
                float bestDiff = float.MaxValue;
                for (int j = 0; j < Hours.Length; j++)
                {
                    float diff = Mathf.Abs(Hours[j] - hour);
                    if (diff < bestDiff) { bestDiff = diff; best = j + 1; }
                }
                _sceneDefaultLevel = best;
                _sceneDefaultCaptured = true;
                Level = best;
                ModLog.Debug("[TimeOfDay] Scene default: " + hour + "h â†’ Level " + best + " (" + Labels[best - 1] + ")");
            }
            catch (System.Exception ex) { MelonLogger.Error("[TimeOfDay] CaptureSceneDefault: " + ex.Message); Telemetry.ReportErrorAsync(ex, "TimeOfDay"); }
        }

        public static void SetLevelSilent(int level)
        {
            if (level < 1) level = 1;
            if (level > 10) level = 10;
            Level = level;
        }

        public static void Apply()
        {
            if (!_sceneDefaultCaptured)
                CaptureSceneDefault();
            try
            {
                if (!EnsureSky())
                {
                    ModLog.Warn("[TimeOfDay] TOD_Sky not found on this map.");
                    return;
                }

                object cycle = _cycleField.GetValue(_sky);
                if ((object)cycle == null) { ModLog.Warn("[TimeOfDay] Cycle is null."); return; }

                if ((object)_hourField != null)
                {
                    _hourField.SetValue(cycle, Hours[Level - 1]);
                    ModLog.Debug("[TimeOfDay] Set to " + Labels[Level - 1] + " (" + Hours[Level - 1] + "h)");
                    return;
                }

                if ((object)_hourProp != null)
                {
                    _hourProp.SetValue(cycle, Hours[Level - 1], null);
                    ModLog.Debug("[TimeOfDay] Set to " + Labels[Level - 1] + " (" + Hours[Level - 1] + "h)");
                    return;
                }

                ModLog.Warn("[TimeOfDay] Hour field/property not found on Cycle.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[TimeOfDay] Apply: " + ex.Message); Telemetry.ReportErrorAsync(ex, "TimeOfDay"); }
        }

        private static bool EnsureSky()
        {
            if (_resolved && (object)_sky != null && _sky)
                return (object)_cycleField != null;

            _resolved = true;
            _sky = null;
            _cycleField = null;
            _hourField = null;
            _hourProp = null;

            MonoBehaviour[] all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].GetType().Name != "TOD_Sky") continue;
                _sky = all[i];
                break;
            }
            if ((object)_sky == null) return false;

            _cycleField = _sky.GetType().GetField("Cycle", BindingFlags.Public | BindingFlags.Instance);
            if ((object)_cycleField == null) return false;

            object cycle = _cycleField.GetValue(_sky);
            if ((object)cycle == null) return false;

            System.Type cycleType = cycle.GetType();
            _hourField = cycleType.GetField("Hour", BindingFlags.Public | BindingFlags.Instance);
            if ((object)_hourField == null)
                _hourProp = cycleType.GetProperty("Hour", BindingFlags.Public | BindingFlags.Instance);

            return true;
        }

        private static float ReadHour(object cycle)
        {
            if ((object)_hourField != null)
                return (float)_hourField.GetValue(cycle);
            if ((object)_hourProp != null)
                return (float)_hourProp.GetValue(cycle, null);
            return Hours[3];
        }
    }
}

