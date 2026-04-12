using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class NearMissSensitivity
    {
        public static bool Enabled { get; private set; } = false;
        public static int Level { get; private set; } = 5;
        private const int MinLevel = 1;
        private const int MaxLevel = 10;
        private const float DefaultDistance = 1.2f;

        // Exponential scaling: level 5 = 1.2f (stock, exact), level 10 = 12.0f (exact)
        // distance = 1.2f * 10^((level-5)/5f)
        public static float Distance => DefaultDistance * UnityEngine.Mathf.Pow(10f, (Level - 5) / 5f);
        public static string DisplayValue => Level.ToString();

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(Enabled ? Distance : DefaultDistance);
            MelonLogger.Msg("[NearMiss] " + (Enabled ? "ON level=" + Level + " dist=" + Distance : "OFF restored default"));
        }

        public static void SetLevel(int v)
        {
            Level = System.Math.Max(1, System.Math.Min(10, v));
        }

        public static void Increase()
        {
            if (Level >= MaxLevel) return;
            Level++;
            if (Enabled) Apply(Distance);
        }

        public static void Decrease()
        {
            if (Level <= MinLevel) return;
            Level--;
            if (Enabled) Apply(Distance);
        }

        // Reset state only — no Apply() because scene objects are destroyed
        // during unload. ResetStats handles restoration via Toggle() which
        // calls Apply() when scene objects exist.
        public static void Reset()
        {
            Enabled = false;
            Level = 5;
        }

        private static void Apply(float distance)
        {
            try
            {
                // NearMissTrick instances live in VehicleTricks.ZduHweT (TrickInfo[] assigned
                // in the editor) — FindObjectsOfTypeAll won't reach them.
                // Find all VehicleTricks in scene and set directly.
                int count = 0;
                VehicleTricks[] allVT = UnityEngine.Object.FindObjectsOfType<VehicleTricks>();
                if (allVT == null || allVT.Length == 0)
                { MelonLogger.Warning("[NearMiss] No VehicleTricks found."); return; }
                for (int v = 0; v < allVT.Length; v++)
                {
                    TrickInfo[] infos = allVT[v].ZduHweT;
                    if ((object)infos == null) continue;
                    for (int i = 0; i < infos.Length; i++)
                    {
                        NearMissTrick nmt = infos[i] as NearMissTrick;
                        if ((object)nmt == null) continue;
                        nmt.nearMissDistance = distance;
                        count++;
                    }
                }
                MelonLogger.Msg("[NearMiss] nearMissDistance=" + distance + " applied to " + count + " instance(s).");
            }
            catch (System.Exception ex) { MelonLogger.Error("[NearMiss] Apply: " + ex.Message); }
        }
    }
}