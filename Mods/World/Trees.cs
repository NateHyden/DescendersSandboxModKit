using System.Collections.Generic;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DescendersModMenu.Mods
{
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

        private static readonly List<GameObject> _hiddenRoots = new List<GameObject>();

        private static float _tickTimer = 0f;
        private const float TickInterval = 1f;

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(!Enabled);
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


        private static bool IsSceneObject(GameObject go)
        {
            if ((object)go == null || go == null) return false;
            Scene s = go.scene;
            return s.IsValid() && !string.IsNullOrEmpty(s.name);
        }

        private static GameObject ResolveTreeRoot(Tree tree)
        {
            Transform parent = tree.transform.parent;
            return (object)parent != null ? parent.gameObject : tree.gameObject;
        }

        private static int HideTreeRoots(bool logNew)
        {
            int caught = 0;
            Tree[] trees = Resources.FindObjectsOfTypeAll<Tree>();
            HashSet<GameObject> seen = new HashSet<GameObject>();

            for (int i = 0; i < trees.Length; i++)
            {
                Tree t = trees[i];
                if ((object)t == null || t == null) continue;
                GameObject root = ResolveTreeRoot(t);
                if (!IsSceneObject(root)) continue;
                if (!seen.Add(root)) continue;

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

                if (tr.gameObject.activeInHierarchy)
                    tr.gameObject.SetActive(false);
            }
            return caught;
        }

        private static int EnableCollisionObjects()
        {
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

