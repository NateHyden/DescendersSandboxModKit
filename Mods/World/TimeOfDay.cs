using MelonLoader;
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
            // Only capture if we have not already — re-capturing after a mod change
            // would read the modified hour, not the original scene hour
            if (!_sceneDefaultCaptured)
                CaptureSceneDefault();
            SetLevel(_sceneDefaultLevel);
        }

        public static void CaptureSceneDefault()
        {
            _sceneDefaultCaptured = false;
            try
            {
                MonoBehaviour[] all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].GetType().Name != "TOD_Sky") continue;
                    System.Reflection.FieldInfo cycleField = all[i].GetType().GetField("Cycle",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if ((object)cycleField == null) break;
                    object cycle = cycleField.GetValue(all[i]);
                    if ((object)cycle == null) break;
                    System.Reflection.FieldInfo hourField = cycle.GetType().GetField("Hour",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if ((object)hourField == null) break;
                    float hour = (float)hourField.GetValue(cycle);
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
                    MelonLogger.Msg("[TimeOfDay] Scene default: " + hour + "h → Level " + best + " (" + Labels[best - 1] + ")");
                    return;
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[TimeOfDay] CaptureSceneDefault: " + ex.Message); }
        }

        // Used by Save/Load � stores the level without touching the game world
        public static void SetLevelSilent(int level)
        {
            if (level < 1) level = 1;
            if (level > 10) level = 10;
            Level = level;
        }

        public static void Apply()
        {
            // Capture the scene default BEFORE we change it — OnSceneWasInitialized
            // may have been too early if TOD_Sky wasn't ready yet
            if (!_sceneDefaultCaptured)
                CaptureSceneDefault();
            try
            {
                // Find TOD_Sky by scanning all MonoBehaviours
                MonoBehaviour[] all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                MonoBehaviour sky = null;
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].GetType().Name == "TOD_Sky")
                    {
                        sky = all[i];
                        break;
                    }
                }
                if ((object)sky == null) { MelonLogger.Warning("[TimeOfDay] TOD_Sky not found on this map."); return; }

                // Get Cycle object
                FieldInfo cycleField = sky.GetType().GetField("Cycle",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)cycleField == null) { MelonLogger.Warning("[TimeOfDay] Cycle field not found."); return; }

                object cycle = cycleField.GetValue(sky);
                if ((object)cycle == null) { MelonLogger.Warning("[TimeOfDay] Cycle is null."); return; }

                // Set Hour
                FieldInfo hourField = cycle.GetType().GetField("Hour",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)hourField != null)
                {
                    hourField.SetValue(cycle, Hours[Level - 1]);
                    MelonLogger.Msg("[TimeOfDay] Set to " + Labels[Level - 1] + " (" + Hours[Level - 1] + "h)");
                    return;
                }

                // Some versions use property
                PropertyInfo hourProp = cycle.GetType().GetProperty("Hour",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)hourProp != null)
                {
                    hourProp.SetValue(cycle, Hours[Level - 1], null);
                    MelonLogger.Msg("[TimeOfDay] Set to " + Labels[Level - 1] + " (" + Hours[Level - 1] + "h)");
                    return;
                }

                MelonLogger.Warning("[TimeOfDay] Hour field/property not found on Cycle.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[TimeOfDay] Apply: " + ex.Message); }
        }
    }
}