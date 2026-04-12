using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Enabled = true means trees/foliage are HIDDEN
    public static class Trees
    {
        public static bool Enabled = false;

        private static System.Type _terrainType = null;
        private static System.Reflection.PropertyInfo _dtfProp = null;

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(!Enabled); // pass true = show, false = hide
        }

        public static void Apply(bool showTrees)
        {
            try
            {
                if ((object)_terrainType == null)
                {
                    System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                    for (int a = 0; a < assemblies.Length; a++)
                    {
                        _terrainType = assemblies[a].GetType("UnityEngine.Terrain");
                        if ((object)_terrainType != null) break;
                    }
                }
                if ((object)_terrainType == null) { MelonLogger.Warning("[Trees] Terrain type not found."); return; }
                if ((object)_dtfProp == null)
                    _dtfProp = _terrainType.GetProperty("drawTreesAndFoliage");
                if ((object)_dtfProp == null) { MelonLogger.Warning("[Trees] drawTreesAndFoliage not found."); return; }
                Object[] terrains = Object.FindObjectsOfType(_terrainType);
                for (int i = 0; i < terrains.Length; i++)
                    _dtfProp.SetValue(terrains[i], showTrees, null);
                MelonLogger.Msg("[Trees] drawTreesAndFoliage -> " + (showTrees ? "ON" : "OFF"));
            }
            catch (System.Exception ex) { MelonLogger.Error("[Trees] Apply: " + ex.Message); }
        }

        public static void Reset()
        {
            if (Enabled) { Enabled = false; Apply(true); }
        }
    }
}
