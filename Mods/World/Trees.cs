using System.Collections.Generic;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DescendersModMenu.Mods
{
    // Enabled = true means trees/foliage are HIDDEN
    public static class Trees
    {
        public static bool Enabled = false;

        private static System.Type _terrainType = null;
        private static System.Reflection.PropertyInfo _dtfProp = null;
        private static System.Reflection.PropertyInfo _terrainDataProp = null;
        private static System.Reflection.PropertyInfo _treeInstancesProp = null;
        private static System.Reflection.PropertyInfo _treeDistanceProp = null;

        private static Dictionary<int, System.Array> _savedTreeInstances = new Dictionary<int, System.Array>();
        private static Dictionary<int, float> _savedTreeDistances = new Dictionary<int, float>();

        // Roots we deactivated — FindObjectsOfType misses inactive objects on
        // Unity 2017, so restore must use this list rather than a fresh search.
        private static readonly List<GameObject> _hiddenRoots = new List<GameObject>();

        private static float _tickTimer = 0f;
        private const float TickInterval = 1f;

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(!Enabled); // pass true = show, false = hide
            _tickTimer = 0f;
            ModLog.Feedback("[Trees] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void Tick()
        {
            if (!Enabled) return;
            _tickTimer += Time.deltaTime;
            if (_tickTimer < TickInterval) return;
            _tickTimer = 0f;
            ReassertHidden();
        }

        private static void ReassertHidden()
        {
            try
            {
                // MapMagic chunk apply resets drawTreesAndFoliage / treeInstances
                // / treeDistance on Terrains after our one-shot Apply(). Keep
                // fighting that, or terrain-tree capsules come back invisible.
                ApplyTerrainVisibility(false, log: false);

                int caughtRoots = HideTreeRoots(logNew: true);
                int caughtCols = DisableCollisionObjects(logNew: true);

                if (caughtRoots > 0 || caughtCols > 0)
                    ModLog.Debug("[Trees] Reassert: re-hid " + caughtRoots
                        + " root(s), disabled " + caughtCols + " CollisionObject collider(s).");
            }
            catch (System.Exception ex) { MelonLogger.Error("[Trees] ReassertHidden: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Trees"); }
        }

        public static void Apply(bool showTrees)
        {
            try
            {
                ApplyTerrainVisibility(showTrees, log: true);

                int rootCount;
                int colCount;
                if (!showTrees)
                {
                    _hiddenRoots.Clear();
                    rootCount = HideTreeRoots(logNew: false);
                    colCount = DisableCollisionObjects(logNew: false);
                    // Force PhysX to drop any lingering broadphase entries from
                    // the objects we just deactivated / collider-disabled.
                    try { Physics.SyncTransforms(); } catch { }
                }
                else
                {
                    rootCount = RestoreHiddenRoots();
                    colCount = EnableCollisionObjects();
                    try { Physics.SyncTransforms(); } catch { }
                }

                ModLog.Feedback("[Trees] " + (showTrees ? "SHOW" : "HIDE")
                    + " | terrain handled | tree roots=" + rootCount
                    + " | CollisionObject colliders=" + colCount);
            }
            catch (System.Exception ex) { MelonLogger.Error("[Trees] Apply: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Trees"); }
        }

        // ── Terrain (MapMagic TreesOutput / Unity treeInstances) ──────────

        private static void EnsureTerrainReflection()
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
            if ((object)_terrainType == null) return;
            if ((object)_dtfProp == null)
                _dtfProp = _terrainType.GetProperty("drawTreesAndFoliage");
            if ((object)_terrainDataProp == null)
                _terrainDataProp = _terrainType.GetProperty("terrainData");
            if ((object)_treeDistanceProp == null)
                _treeDistanceProp = _terrainType.GetProperty("treeDistance");
        }

        private static void ApplyTerrainVisibility(bool showTrees, bool log)
        {
            EnsureTerrainReflection();
            if ((object)_terrainType == null)
            {
                if (log) ModLog.Warn("[Trees] Terrain type not found (GO trees still handled).");
                return;
            }

            Object[] terrains = Object.FindObjectsOfType(_terrainType);
            int clearedCount = 0, restoredCount = 0;

            for (int i = 0; i < terrains.Length; i++)
            {
                Object terrain = terrains[i];
                if ((object)_dtfProp != null)
                    _dtfProp.SetValue(terrain, showTrees, null);

                // Same knob DevCommandsPerformance.ToggleTrees uses — render
                // distance only, but keep it in sync so MapMagic chunk apply
                // can't quietly turn drawing back on underneath us.
                if ((object)_treeDistanceProp != null)
                {
                    int key = terrain.GetInstanceID();
                    if (!showTrees)
                    {
                        try
                        {
                            float current = (float)_treeDistanceProp.GetValue(terrain, null);
                            if (!_savedTreeDistances.ContainsKey(key) && current > 0f)
                                _savedTreeDistances[key] = current;
                        }
                        catch { }
                        _treeDistanceProp.SetValue(terrain, 0f, null);
                    }
                    else if (_savedTreeDistances.TryGetValue(key, out float saved))
                    {
                        _treeDistanceProp.SetValue(terrain, saved, null);
                        _savedTreeDistances.Remove(key);
                    }
                }

                if ((object)_terrainDataProp == null) continue;
                object terrainData = _terrainDataProp.GetValue(terrain, null);
                if (terrainData == null) continue;

                if ((object)_treeInstancesProp == null)
                    _treeInstancesProp = terrainData.GetType().GetProperty("treeInstances");
                if ((object)_treeInstancesProp == null) continue;

                int tdKey = terrain.GetInstanceID();

                if (!showTrees)
                {
                    if (!_savedTreeInstances.ContainsKey(tdKey))
                    {
                        object current = _treeInstancesProp.GetValue(terrainData, null);
                        _savedTreeInstances[tdKey] = current as System.Array;
                    }
                    System.Array empty = System.Array.CreateInstance(_treeInstancesProp.PropertyType.GetElementType(), 0);
                    _treeInstancesProp.SetValue(terrainData, empty, null);
                    clearedCount++;

                    // Unity caches tree capsules on TerrainCollider. Clearing
                    // treeInstances alone doesn't always rebuild that cache in
                    // 2017.4 — bounce the collider so PhysX drops them.
                    BounceTerrainCollider(terrain);
                }
                else if (_savedTreeInstances.TryGetValue(tdKey, out System.Array saved) && saved != null)
                {
                    _treeInstancesProp.SetValue(terrainData, saved, null);
                    _savedTreeInstances.Remove(tdKey);
                    restoredCount++;
                    BounceTerrainCollider(terrain);
                }
            }

            if (log)
                ModLog.Debug("[Trees] terrain instances cleared=" + clearedCount + " restored=" + restoredCount
                    + " terrains=" + terrains.Length);
        }

        private static void BounceTerrainCollider(Object terrain)
        {
            try
            {
                Component c = terrain as Component;
                if ((object)c == null) return;
                TerrainCollider tc = c.GetComponent<TerrainCollider>();
                if (tc == null) return;
                tc.enabled = false;
                tc.enabled = true;
            }
            catch { }
        }

        // ── GameObject trees (ObjectOutput / curated props) ───────────────

        private static bool IsSceneObject(GameObject go)
        {
            if ((object)go == null || go == null) return false;
            // Prefab assets / DontSave junk from FindObjectsOfTypeAll
            Scene s = go.scene;
            return s.IsValid() && !string.IsNullOrEmpty(s.name);
        }

        private static GameObject ResolveTreeRoot(Tree tree)
        {
            Transform parent = tree.transform.parent;
            return (object)parent != null ? parent.gameObject : tree.gameObject;
        }

        // Returns how many roots newly deactivated this pass.
        private static int HideTreeRoots(bool logNew)
        {
            int caught = 0;
            // Include inactive — custom LOD systems leave Tree comps on
            // deactivated LOD children; FindObjectsOfType would miss them
            // and leave the still-active CollisionObject sibling alive.
            Tree[] trees = Resources.FindObjectsOfTypeAll<Tree>();
            HashSet<GameObject> seen = new HashSet<GameObject>();

            for (int i = 0; i < trees.Length; i++)
            {
                Tree t = trees[i];
                if ((object)t == null || t == null) continue;
                GameObject root = ResolveTreeRoot(t);
                if (!IsSceneObject(root)) continue;
                if (!seen.Add(root)) continue;

                // Also kill colliders under the root explicitly — belt and
                // suspenders if something re-parents CollisionObject out.
                DisableCollidersUnder(root.transform);

                if (root.activeSelf)
                {
                    root.SetActive(false);
                    caught++;
                }

                if (!_hiddenRoots.Contains(root))
                    _hiddenRoots.Add(root);
            }
            return caught;
        }

        private static int RestoreHiddenRoots()
        {
            int n = 0;
            for (int i = 0; i < _hiddenRoots.Count; i++)
            {
                GameObject root = _hiddenRoots[i];
                if ((object)root == null || root == null) continue;
                if (!root.activeSelf)
                {
                    root.SetActive(true);
                    n++;
                }
                EnableCollidersUnder(root.transform);
            }
            _hiddenRoots.Clear();
            return n;
        }

        private static void DisableCollidersUnder(Transform root)
        {
            Collider[] cols = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null) cols[i].enabled = false;
            }
        }

        private static void EnableCollidersUnder(Transform root)
        {
            Collider[] cols = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null) cols[i].enabled = true;
            }
        }

        // Catch CollisionObject* that aren't under a UnityEngine.Tree hierarchy
        // (or were reparented). Returns newly disabled collider count.
        private static int DisableCollisionObjects(bool logNew)
        {
            int caught = 0;
            Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                Transform tr = all[i];
                if ((object)tr == null || tr == null) continue;
                string name = tr.name;
                if (name == null || name.Length < 15 || !name.StartsWith("CollisionObject"))
                    continue;
                if (!IsSceneObject(tr.gameObject)) continue;

                Collider[] cols = tr.GetComponents<Collider>();
                for (int c = 0; c < cols.Length; c++)
                {
                    Collider col = cols[c];
                    if (col == null) continue;
                    if (col.enabled)
                    {
                        col.enabled = false;
                        caught++;
                    }
                }

                // If the CollisionObject itself is an orphaned active object
                // (no inactive ancestor), deactivate it too.
                if (tr.gameObject.activeInHierarchy)
                    tr.gameObject.SetActive(false);
            }
            return caught;
        }

        private static int EnableCollisionObjects()
        {
            // Restore path relies on re-enabled tree roots; CollisionObjects
            // under those roots come back with the root. Orphans that we
            // deactivated by name get a best-effort re-enable here.
            int n = 0;
            Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                Transform tr = all[i];
                if ((object)tr == null || tr == null) continue;
                string name = tr.name;
                if (name == null || !name.StartsWith("CollisionObject")) continue;
                if (!IsSceneObject(tr.gameObject)) continue;

                if (!tr.gameObject.activeSelf)
                {
                    tr.gameObject.SetActive(true);
                    n++;
                }
                Collider[] cols = tr.GetComponents<Collider>();
                for (int c = 0; c < cols.Length; c++)
                {
                    if (cols[c] != null) cols[c].enabled = true;
                }
            }
            return n;
        }

        public static void ClearCache()
        {
            _savedTreeInstances.Clear();
            _savedTreeDistances.Clear();
            _hiddenRoots.Clear();
        }

        public static void Reset()
        {
            if (Enabled) { Enabled = false; Apply(true); }
        }
    }
}
