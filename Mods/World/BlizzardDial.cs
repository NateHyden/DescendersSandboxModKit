using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using UnityEngine.Rendering;

namespace DescendersModMenu.Mods
{
    // Mesh-overlay snow with wheel ruts. Snow Line is player-relative with a
    // soft curve so mid dial values don't wipe valley snow (old linear map
    // hit minY≈11 at line 7 while the rider sat at y≈1).
    public static class BlizzardDial
    {
        public static bool Enabled = false;

        public static int SnowLevel { get; private set; } = 1;
        public static int SeasonIndex { get; private set; } = 0;

        public static readonly string[] SeasonNames = { "Spring", "Summer", "Autumn", "Winter" };

        // Season tints must be strong — old 15–35% lerps looked identical in-game.
        private static readonly Color BaseSnowColor = new Color(0.93f, 0.96f, 1f, 1f);
        private static readonly Color WinterTint = new Color(0.78f, 0.88f, 1f, 1f);   // cold blue-white
        private static readonly Color SpringTint = new Color(0.72f, 0.95f, 0.70f, 1f); // green melt
        private static readonly Color SummerTint = new Color(1f, 0.82f, 0.55f, 1f);    // dirty / warm
        private static readonly Color AutumnTint = new Color(0.95f, 0.70f, 0.45f, 1f); // orange mud
        private static readonly Color RutColor = new Color(0.45f, 0.50f, 0.58f, 1f);

        private const float SurfaceLift = 0.18f;
        private const float MinThickness = 0.35f;
        private const float MaxThickness = 1.4f;
        private const int GridResMin = 48;
        private const int GridResMax = 96;
        private const string RootName = "Sandbox_SnowOverlay";
        private const float CarveRadius = 0.55f;
        private const float CarveDepthFrac = 0.72f; // how much of thickness to punch down
        private const float CarveInterval = 0.08f; // min distance between stamps

        private static GameObject _root;
        private static Material _mat;
        private static bool _matOwned;
        private static readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        private static readonly List<SnowSheet> _sheets = new List<SnowSheet>();
        private static FieldInfo _stuntSurfaceField;
        private static Vector3 _lastCarvePos = new Vector3(99999f, 99999f, 99999f);

        private class SnowSheet
        {
            public Mesh Mesh;
            public Transform Xform;
            public Vector3[] Verts;
            public Color[] Colors;
            public int TopCount;
            public int Res;
            public int Stride;
            public float StepX;
            public float StepZ;
            public float SizeX;
            public float SizeZ;
            public int[] GridToTop; // stride*stride → top vert index or -1
            public float Thickness;
            public bool Dirty;
        }

        private struct SavedTerrainDetails
        {
            public float DetailDistance;
            public float DetailDensity;
        }
        private static readonly Dictionary<int, SavedTerrainDetails> _savedDetails = new Dictionary<int, SavedTerrainDetails>();

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Apply();
            else Restore();
            ModLog.Feedback("[BlizzardDial] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void IncreaseSnow() { if (SnowLevel < 10) { SnowLevel++; if (Enabled) Apply(); } }
        public static void DecreaseSnow() { if (SnowLevel > 1) { SnowLevel--; if (Enabled) Apply(); } }
        public static void SetSnowLevel(int v) { SnowLevel = Mathf.Clamp(v, 1, 10); if (Enabled) Apply(); }

        public static void NextSeason() { SeasonIndex = (SeasonIndex + 1) % 4; if (Enabled) Apply(); }
        public static void PrevSeason() { SeasonIndex = (SeasonIndex + 3) % 4; if (Enabled) Apply(); }
        public static void SetSeasonIndex(int v)
        {
            SeasonIndex = ((v % 4) + 4) % 4;
            if (Enabled) Apply();
        }

        public static string SeasonDisplay => SeasonNames[SeasonIndex];

        private static float SnowThickness()
        {
            float t = (SnowLevel - 1) / 9f;
            return Mathf.Lerp(MinThickness, MaxThickness, t);
        }

        private static Color SeasonSnowColor()
        {
            // Heavy blend so Spring/Summer/Autumn/Winter are obvious once Blizzard is ON.
            if (SeasonIndex == 1) return Color.Lerp(BaseSnowColor, SummerTint, 0.85f);
            if (SeasonIndex == 2) return Color.Lerp(BaseSnowColor, AutumnTint, 0.85f);
            if (SeasonIndex == 3) return Color.Lerp(BaseSnowColor, WinterTint, 0.75f);
            return Color.Lerp(BaseSnowColor, SpringTint, 0.80f);
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo getSurf = typeof(TerrainInfo).GetMethod(
                    "GetSurfaceInfoAt", BindingFlags.Public | BindingFlags.Instance);
                if ((object)getSurf != null)
                {
                    harmony.Patch(getSurf, postfix: new HarmonyMethod(
                        typeof(BlizzardDial_SurfacePatch).GetMethod(
                            "Postfix", BindingFlags.Public | BindingFlags.Static)));
                }
                DiagnosticsManager.Report("BlizzardDial", true);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[BlizzardDial] ApplyPatch: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "BlizzardDial");
                DiagnosticsManager.Report("BlizzardDial", false, ex.Message);
            }
        }

        public static void Tick()
        {
            if (!Enabled || _sheets.Count == 0) return;
            try
            {
                GameObject player = PlayerCache.PlayerHuman;
                if ((object)player == null || player == null) return;

                Vector3 pos = player.transform.position;
                if ((pos - _lastCarvePos).sqrMagnitude < CarveInterval * CarveInterval) return;

                Wheel[] wheels = player.GetComponentsInChildren<Wheel>(true);
                if (wheels == null || wheels.Length == 0)
                {
                    CarveAt(pos);
                }
                else
                {
                    for (int i = 0; i < wheels.Length; i++)
                    {
                        Wheel w = wheels[i];
                        if ((object)w == null || w == null) continue;
                        CarveAt(w.transform.position);
                    }
                }
                _lastCarvePos = pos;
                FlushDirtySheets();
            }
            catch { }
        }

        private static void CarveAt(Vector3 worldPos)
        {
            for (int s = 0; s < _sheets.Count; s++)
            {
                SnowSheet sheet = _sheets[s];
                if ((object)sheet == null || (object)sheet.Xform == null || sheet.Xform == null) continue;
                if ((object)sheet.Mesh == null || sheet.Mesh == null) continue;

                Vector3 local = sheet.Xform.InverseTransformPoint(worldPos);
                if (local.x < -CarveRadius || local.z < -CarveRadius) continue;
                if (local.x > sheet.SizeX + CarveRadius || local.z > sheet.SizeZ + CarveRadius) continue;

                float r2 = CarveRadius * CarveRadius;
                int ix0 = Mathf.Clamp(Mathf.FloorToInt((local.x - CarveRadius) / sheet.StepX), 0, sheet.Res);
                int ix1 = Mathf.Clamp(Mathf.CeilToInt((local.x + CarveRadius) / sheet.StepX), 0, sheet.Res);
                int iz0 = Mathf.Clamp(Mathf.FloorToInt((local.z - CarveRadius) / sheet.StepZ), 0, sheet.Res);
                int iz1 = Mathf.Clamp(Mathf.CeilToInt((local.z + CarveRadius) / sheet.StepZ), 0, sheet.Res);

                float punch = sheet.Thickness * CarveDepthFrac;
                bool any = false;

                for (int iz = iz0; iz <= iz1; iz++)
                {
                    for (int ix = ix0; ix <= ix1; ix++)
                    {
                        int g = iz * sheet.Stride + ix;
                        int top = sheet.GridToTop[g];
                        if (top < 0) continue;

                        float gx = ix * sheet.StepX;
                        float gz = iz * sheet.StepZ;
                        float dx = gx - local.x;
                        float dz = gz - local.z;
                        float d2 = dx * dx + dz * dz;
                        if (d2 > r2) continue;

                        float falloff = 1f - (d2 / r2);
                        falloff *= falloff;
                        float drop = punch * falloff;

                        Vector3 v = sheet.Verts[top];
                        // Don't push below the bottom lid (topCount offset).
                        int bot = top + sheet.TopCount;
                        float floorY = sheet.Verts[bot].y + 0.02f;
                        float newY = Mathf.Max(floorY, v.y - drop);
                        if (newY >= v.y - 0.001f) continue;

                        v.y = newY;
                        sheet.Verts[top] = v;
                        sheet.Colors[top] = Color.Lerp(sheet.Colors[top], RutColor, 0.65f * falloff);
                        any = true;
                    }
                }

                if (any) sheet.Dirty = true;
            }
        }

        private static void FlushDirtySheets()
        {
            for (int s = 0; s < _sheets.Count; s++)
            {
                SnowSheet sheet = _sheets[s];
                if ((object)sheet == null || !sheet.Dirty) continue;
                if ((object)sheet.Mesh == null || sheet.Mesh == null) continue;
                sheet.Mesh.vertices = sheet.Verts;
                sheet.Mesh.colors = sheet.Colors;
                sheet.Mesh.RecalculateNormals();
                sheet.Dirty = false;
            }
        }

        public static void Apply()
        {
            try
            {
                DestroyOverlays();
                EnsureMaterial();
                ApplySeasonToMaterial();
                _lastCarvePos = new Vector3(99999f, 99999f, 99999f);

                Terrain[] terrains = Object.FindObjectsOfType<Terrain>();
                if (terrains == null || terrains.Length == 0)
                {
                    TryCloneAuthoredSnow();
                    return;
                }

                _root = new GameObject(RootName);
                Object.DontDestroyOnLoad(_root);

                float thickness = SnowThickness();
                for (int i = 0; i < terrains.Length; i++)
                {
                    Terrain terrain = terrains[i];
                    if ((object)terrain == null || terrain == null) continue;
                    if ((object)terrain.terrainData == null) continue;
                    HideGrass(terrain);
                    BuildTerrainOverlay(terrain, thickness);
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[BlizzardDial] Apply: " + ex);  Telemetry.ReportErrorAsync(ex, "BlizzardDial"); }
        }

        private static void EnsureMaterial()
        {
            if (_matOwned && (object)_mat != null)
            {
                Object.Destroy(_mat);
                _mat = null;
                _matOwned = false;
            }

            // Sprites/Default multiplies vertex color — needed for dark ruts.
            Shader sh = Shader.Find("Sprites/Default");
            if ((object)sh == null) sh = Shader.Find("Unlit/Color");
            if ((object)sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");
            if ((object)sh == null) sh = Shader.Find("Diffuse");
            if ((object)sh == null) sh = Shader.Find("Standard");
            if ((object)sh == null) return;

            _mat = new Material(sh);
            _mat.name = "Sandbox_SnowOverlay_Mat";
            _matOwned = true;
            _mat.renderQueue = (int)RenderQueue.Geometry + 10;
            if (_mat.HasProperty("_Color")) _mat.SetColor("_Color", Color.white);
            if (_mat.HasProperty("_TintColor")) _mat.SetColor("_TintColor", Color.white);
            if (_mat.HasProperty("_MainTex")) _mat.SetTexture("_MainTex", Texture2D.whiteTexture);
        }

        private static void ApplySeasonToMaterial()
        {
            // Season lives in vertex colors now (base tint); keep mat white.
            if ((object)_mat == null || _mat == null) return;
            if (_mat.HasProperty("_Color")) _mat.SetColor("_Color", Color.white);
        }

        private static bool BuildTerrainOverlay(Terrain terrain, float thickness)
        {
            if ((object)_mat == null || _mat == null) return false;

            TerrainData td = terrain.terrainData;
            Vector3 origin = terrain.transform.position;
            Vector3 size = td.size;

            int res = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(size.x, size.z) / 10f), GridResMin, GridResMax);
            int stride = res + 1;
            int gridCount = stride * stride;

            Vector3[] topLocal = new Vector3[gridCount];
            Vector3[] botLocal = new Vector3[gridCount];
            Vector2[] uvs = new Vector2[gridCount];
            bool[] keep = new bool[gridCount];
            int kept = 0;

            float stepX = size.x / res;
            float stepZ = size.z / res;
            Color baseCol = SeasonSnowColor();

            for (int iz = 0; iz <= res; iz++)
            {
                for (int ix = 0; ix <= res; ix++)
                {
                    int idx = iz * stride + ix;
                    float lx = ix * stepX;
                    float lz = iz * stepZ;
                    float nx = (float)ix / res;
                    float nz = (float)iz / res;
                    float ly = td.GetInterpolatedHeight(nx, nz);

                    float wx = origin.x + lx;
                    float wz = origin.z + lz;

                    float n = Mathf.PerlinNoise(wx * 0.07f + 17.1f, wz * 0.07f + 3.7f);
                    float bump = (n - 0.35f) * thickness * 0.45f;

                    botLocal[idx] = new Vector3(lx, ly + SurfaceLift, lz);
                    topLocal[idx] = new Vector3(lx, ly + SurfaceLift + thickness + bump, lz);
                    uvs[idx] = new Vector2(nx * 12f, nz * 12f);

                    keep[idx] = true;
                    kept++;
                }
            }

            if (kept < 4) return false;

            List<Vector3> verts = new List<Vector3>(kept * 2 + 16);
            List<Vector2> meshUvs = new List<Vector2>(kept * 2 + 16);
            List<Color> meshCols = new List<Color>(kept * 2 + 16);
            List<int> tris = new List<int>(kept * 8);
            int[] remap = new int[gridCount];
            for (int i = 0; i < gridCount; i++) remap[i] = -1;

            for (int i = 0; i < gridCount; i++)
            {
                if (!keep[i]) continue;
                remap[i] = verts.Count;
                verts.Add(topLocal[i]);
                meshUvs.Add(uvs[i]);
                meshCols.Add(baseCol);
            }
            int topCount = verts.Count;
            for (int i = 0; i < gridCount; i++)
            {
                if (!keep[i]) continue;
                verts.Add(botLocal[i]);
                meshUvs.Add(uvs[i]);
                meshCols.Add(baseCol);
            }

            for (int iz = 0; iz < res; iz++)
            {
                for (int ix = 0; ix < res; ix++)
                {
                    int i00 = iz * stride + ix;
                    int i10 = i00 + 1;
                    int i01 = i00 + stride;
                    int i11 = i01 + 1;
                    if (!keep[i00] || !keep[i10] || !keep[i01] || !keep[i11]) continue;

                    int a = remap[i00];
                    int b = remap[i10];
                    int c = remap[i01];
                    int d = remap[i11];
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                    tris.Add(a + topCount); tris.Add(b + topCount); tris.Add(c + topCount);
                    tris.Add(b + topCount); tris.Add(d + topCount); tris.Add(c + topCount);
                }
            }

            if (tris.Count < 3) return false;

            AddBoundarySides(tris, remap, keep, topCount, res, stride);

            Vector3[] vertArr = verts.ToArray();
            int[] triArr = tris.ToArray();
            Vector2[] uvArr = meshUvs.ToArray();
            Color[] colArr = meshCols.ToArray();

            Mesh mesh = new Mesh();
            mesh.name = "SnowOverlay_" + terrain.name;
            if (vertArr.Length > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertArr;
            mesh.uv = uvArr;
            mesh.colors = colArr;
            mesh.triangles = triArr;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _ownedMeshes.Add(mesh);

            if (mesh.vertexCount < 3 || mesh.triangles == null || mesh.triangles.Length < 3)
                return false;

            GameObject go = new GameObject("SnowOverlay_" + terrain.name);
            go.layer = 0;
            go.transform.SetParent(terrain.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            if ((object)_root != null)
            {
                GameObject marker = new GameObject("SnowOverlay_Ref_" + terrain.name);
                marker.transform.SetParent(_root.transform, false);
            }

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.allowOcclusionWhenDynamic = false;
            mr.enabled = true;

            AttachSnowCollider(go, topLocal, keep, res, stride, terrain.name);
            TagAsSnowSurface(go);

            SnowSheet sheet = new SnowSheet();
            sheet.Mesh = mesh;
            sheet.Xform = go.transform;
            sheet.Verts = vertArr;
            sheet.Colors = colArr;
            sheet.TopCount = topCount;
            sheet.Res = res;
            sheet.Stride = stride;
            sheet.StepX = stepX;
            sheet.StepZ = stepZ;
            sheet.SizeX = size.x;
            sheet.SizeZ = size.z;
            sheet.GridToTop = remap;
            sheet.Thickness = thickness;
            sheet.Dirty = false;
            _sheets.Add(sheet);
            return true;
        }

        private static void AttachSnowCollider(
            GameObject go, Vector3[] topLocal, bool[] keep,
            int res, int stride, string terrainName)
        {
            List<Vector3> cVerts = new List<Vector3>();
            List<int> cTris = new List<int>();
            int[] cRemap = new int[topLocal.Length];
            for (int i = 0; i < cRemap.Length; i++) cRemap[i] = -1;

            for (int i = 0; i < topLocal.Length; i++)
            {
                if (!keep[i]) continue;
                cRemap[i] = cVerts.Count;
                cVerts.Add(topLocal[i]);
            }

            for (int iz = 0; iz < res; iz++)
            {
                for (int ix = 0; ix < res; ix++)
                {
                    int i00 = iz * stride + ix;
                    int i10 = i00 + 1;
                    int i01 = i00 + stride;
                    int i11 = i01 + 1;
                    if (!keep[i00] || !keep[i10] || !keep[i01] || !keep[i11]) continue;
                    int a = cRemap[i00], b = cRemap[i10], c = cRemap[i01], d = cRemap[i11];
                    cTris.Add(a); cTris.Add(c); cTris.Add(b);
                    cTris.Add(b); cTris.Add(c); cTris.Add(d);
                }
            }

            if (cVerts.Count < 3 || cTris.Count < 3) return;

            Mesh colMesh = new Mesh();
            colMesh.name = "SnowOverlay_Col_" + terrainName;
            if (cVerts.Count > 65535) colMesh.indexFormat = IndexFormat.UInt32;
            colMesh.vertices = cVerts.ToArray();
            colMesh.triangles = cTris.ToArray();
            colMesh.RecalculateBounds();
            _ownedMeshes.Add(colMesh);

            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = colMesh;
            mc.convex = false;
        }

        private static void TagAsSnowSurface(GameObject go)
        {
            StuntSurfaceType sst = go.AddComponent<StuntSurfaceType>();
            if ((object)_stuntSurfaceField == null)
            {
                FieldInfo[] fields = typeof(StuntSurfaceType).GetFields(
                    BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < fields.Length; i++)
                {
                    if ((object)fields[i].FieldType == (object)typeof(nwwOJzr))
                    {
                        _stuntSurfaceField = fields[i];
                        break;
                    }
                }
            }
            if ((object)_stuntSurfaceField != null)
                _stuntSurfaceField.SetValue(sst, nwwOJzr.Snow);
        }

        private static void AddSide(List<int> tris, int[] remap, int topCount, int i, int j)
        {
            if (remap[i] < 0 || remap[j] < 0) return;
            int ti = remap[i], tj = remap[j];
            int bi = ti + topCount, bj = tj + topCount;
            tris.Add(ti); tris.Add(tj); tris.Add(bi);
            tris.Add(tj); tris.Add(bj); tris.Add(bi);
        }

        private static void AddBoundarySides(
            List<int> tris, int[] remap, bool[] keep, int topCount, int res, int stride)
        {
            for (int iz = 0; iz <= res; iz++)
            {
                for (int ix = 0; ix < res; ix++)
                {
                    int a = iz * stride + ix;
                    int b = a + 1;
                    if (!keep[a] || !keep[b]) continue;
                    bool boundary = iz == 0 || iz == res;
                    if (!boundary)
                    {
                        int below = (iz - 1) * stride + ix;
                        int above = (iz + 1) * stride + ix;
                        bool lo = iz > 0 && keep[below] && keep[below + 1];
                        bool hi = iz < res && keep[above] && keep[above + 1];
                        if (lo == hi) continue;
                    }
                    AddSide(tris, remap, topCount, a, b);
                }
            }
            for (int ix = 0; ix <= res; ix++)
            {
                for (int iz = 0; iz < res; iz++)
                {
                    int a = iz * stride + ix;
                    int b = a + stride;
                    if (!keep[a] || !keep[b]) continue;
                    bool boundary = ix == 0 || ix == res;
                    if (!boundary)
                    {
                        bool left = ix > 0 && keep[a - 1] && keep[b - 1];
                        bool right = ix < res && keep[a + 1] && keep[b + 1];
                        if (left == right) continue;
                    }
                    AddSide(tris, remap, topCount, a, b);
                }
            }
        }

        private static int TryCloneAuthoredSnow()
        {
            GameObject src = GameObject.Find("Booters_Snow");
            if ((object)src == null || src == null) return 0;

            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);
            GameObject clone = Object.Instantiate(src);
            clone.name = "Booters_Snow_Sandbox";
            clone.transform.SetParent(_root.transform, true);
            float yScale = Mathf.Lerp(1f, 2.2f, (SnowLevel - 1) / 9f);
            for (int i = 0; i < clone.transform.childCount; i++)
            {
                Transform c = clone.transform.GetChild(i);
                Vector3 s = c.localScale;
                c.localScale = new Vector3(s.x, s.y * yScale, s.z);
            }
            return 1 + clone.transform.childCount;
        }

        private static void HideGrass(Terrain terrain)
        {
            int id = terrain.GetInstanceID();
            if (!_savedDetails.ContainsKey(id))
            {
                _savedDetails[id] = new SavedTerrainDetails
                {
                    DetailDistance = terrain.detailObjectDistance,
                    DetailDensity = terrain.detailObjectDensity
                };
            }
            terrain.detailObjectDistance = 0f;
            terrain.detailObjectDensity = 0f;
        }

        private static void RestoreGrass()
        {
            Terrain[] terrains = Object.FindObjectsOfType<Terrain>();
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain t = terrains[i];
                if ((object)t == null || t == null) continue;
                if (!_savedDetails.TryGetValue(t.GetInstanceID(), out SavedTerrainDetails s)) continue;
                t.detailObjectDistance = s.DetailDistance;
                t.detailObjectDensity = s.DetailDensity;
            }
            _savedDetails.Clear();
        }

        private static void DestroyOverlays()
        {
            _sheets.Clear();

            Terrain[] terrains = Object.FindObjectsOfType<Terrain>();
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain t = terrains[i];
                if ((object)t == null || t == null) continue;
                Transform tr = t.transform;
                for (int c = tr.childCount - 1; c >= 0; c--)
                {
                    Transform child = tr.GetChild(c);
                    if ((object)child == null) continue;
                    if (child.name != null && child.name.StartsWith("SnowOverlay_"))
                        Object.Destroy(child.gameObject);
                }
            }

            if ((object)_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
            for (int i = 0; i < _ownedMeshes.Count; i++)
            {
                if ((object)_ownedMeshes[i] != null) Object.Destroy(_ownedMeshes[i]);
            }
            _ownedMeshes.Clear();
        }

        private static void Restore()
        {
            try
            {
                DestroyOverlays();
                RestoreGrass();
            }
            catch (System.Exception ex) { MelonLogger.Error("[BlizzardDial] Restore: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "BlizzardDial"); }
        }

        public static void ClearCache()
        {
            if (Enabled) Restore();
            else
            {
                DestroyOverlays();
                _savedDetails.Clear();
            }
            if (_matOwned && (object)_mat != null)
            {
                Object.Destroy(_mat);
                _mat = null;
                _matOwned = false;
            }
        }

        public static void Reset()
        {
            if (Enabled) { Enabled = false; Restore(); }
            SnowLevel = 1;
            SeasonIndex = 0;
            ClearCache();
        }
    }

    public static class BlizzardDial_SurfacePatch
    {
        public static void Postfix(ref SurfaceInfo __result)
        {
            if (!BlizzardDial.Enabled) return;
            if ((object)__result == null) __result = new SurfaceInfo();
            __result.surfaceType = nwwOJzr.Snow;
        }
    }
}
