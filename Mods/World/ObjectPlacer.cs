using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Live placement: freeze the bike, fly a ghost around, confirm to drop a copy.
    // Prefers cloned in-scene Bike Park meshes (textures intact). Primitive blocks
    // remain as fallback when the current map has nothing harvestable.
    // Session-only: nothing is saved.
    public static class ObjectPlacer
    {
        public static bool Enabled { get; private set; } = false;
        public static bool IsPlacing { get; private set; } = false;
        public static bool IsAnyActive => Enabled || _placedRamps.Count > 0;
        public static int PlacedCount => _placedRamps.Count;
        public static int SelectedIndex { get; private set; } = 0;
        public static int HarvestedCount { get; private set; } = 0;
        public static int CatalogCount { get { EnsureCatalog(); return _catalog.Count; } }
        public static string SelectedName
        {
            get
            {
                EnsureCatalog();
                if (SelectedIndex < 0 || SelectedIndex >= _catalog.Count) return "--";
                return _catalog[SelectedIndex].Name;
            }
        }

        private struct Placeable
        {
            public string Id;
            public string Name;
            public string Group;
            public bool IsPrimitive;
            public PrimitiveType Shape;
            public Vector3 Scale;
            public Color Color;
            public GameObject Template;
        }

        public static readonly string[] GroupOrder =
        {
            "Ramps", "Kickers", "Platforms", "Rails", "Walls", "Props", "Blocks"
        };

        public static string GetNameAt(int index)
        {
            EnsureCatalog();
            if (index < 0 || index >= _catalog.Count) return "";
            return _catalog[index].Name;
        }

        public static string GetGroupAt(int index)
        {
            EnsureCatalog();
            if (index < 0 || index >= _catalog.Count) return "Blocks";
            return string.IsNullOrEmpty(_catalog[index].Group) ? "Blocks" : _catalog[index].Group;
        }

        public static bool IsHarvestedAt(int index)
        {
            EnsureCatalog();
            if (index < 0 || index >= _catalog.Count) return false;
            return !_catalog[index].IsPrimitive;
        }

        public static string GetIdAt(int index)
        {
            EnsureCatalog();
            if (index < 0 || index >= _catalog.Count) return "";
            return _catalog[index].Id ?? "";
        }

        public static bool IsFavAt(int index)
        {
            string id = GetIdAt(index);
            return id.Length > 0 && _favIds.Contains(id);
        }

        public static void ToggleFav(int index)
        {
            EnsurePrefs();
            string id = GetIdAt(index);
            if (id.Length == 0) return;
            if (_favIds.Contains(id))
            {
                _favIds.Remove(id);
                ModLog.Feedback("[ObjectPlacer] Unfavoured " + GetNameAt(index));
            }
            else
            {
                _favIds.Add(id);
                ModLog.Feedback("[ObjectPlacer] Favoured " + GetNameAt(index));
            }
            SavePrefs();
        }

        public static int FavCount
        {
            get
            {
                EnsureCatalog();
                int n = 0;
                for (int i = 0; i < _catalog.Count; i++)
                    if (_favIds.Contains(_catalog[i].Id)) n++;
                return n;
            }
        }

        public static void BumpMove(int dir) { EnsurePrefs(); MoveSpeedLevel = ClampLevel(MoveSpeedLevel + dir); SavePrefs(); }
        public static void BumpRotate(int dir) { EnsurePrefs(); RotateSpeedLevel = ClampLevel(RotateSpeedLevel + dir); SavePrefs(); }
        public static void BumpLift(int dir) { EnsurePrefs(); LiftSpeedLevel = ClampLevel(LiftSpeedLevel + dir); SavePrefs(); }

        private static int ClampLevel(int level)
        {
            if (level < 1) return 1;
            if (level > 10) return 10;
            return level;
        }

        private static readonly List<Placeable> _catalog = new List<Placeable>();
        private static GameObject _holder = null;

        private static readonly string[] RampKeywords =
        {
            "ramp", "kicker", "berm", "jump", "wood", "table", "hip", "gap",
            "wallride", "spine", "stepup", "step-up", "stepdown", "drop",
            "quarter", "lip", "takeoff", "landing", "boxjump", "box_jump",
            "feature", "deck", "rail", "ladder", "northshore", "north_shore"
        };

        private static readonly string[] BlockKeywords =
        {
            "player", "bike", "camera", "terrain", "tree", "fx", "particle",
            "ui", "canvas", "water", "sky", "grass", "leaf", "character",
            "rider", "lodgroup", "collider_only"
        };

        private static readonly float[] MoveSpeeds = { 3f, 5f, 7f, 8.5f, 10f, 13f, 16f, 20f, 26f, 34f };
        private static readonly float[] RotateSpeeds = { 30f, 45f, 60f, 75f, 90f, 115f, 140f, 175f, 220f, 280f };
        private static readonly float[] LiftSpeeds = { 1f, 1.5f, 2f, 2.5f, 3f, 4f, 5.5f, 7f, 9f, 12f };

        public static int MoveSpeedLevel { get; private set; } = 5;
        public static int RotateSpeedLevel { get; private set; } = 5;
        public static int LiftSpeedLevel { get; private set; } = 5;

        public static float MoveSpeed { get { return MoveSpeeds[MoveSpeedLevel - 1]; } }
        public static float YawSpeed { get { return RotateSpeeds[RotateSpeedLevel - 1]; } }
        public static float LiftSpeed { get { return LiftSpeeds[LiftSpeedLevel - 1]; } }
        public static float PitchSpeed { get { return YawSpeed * 0.67f; } }

        public static string MoveSpeedDisplay { get { return MoveSpeed.ToString("0.#"); } }
        public static string RotateSpeedDisplay { get { return YawSpeed.ToString("0"); } }
        public static string LiftSpeedDisplay { get { return LiftSpeed.ToString("0.#"); } }

        private static readonly HashSet<string> _favIds = new HashSet<string>();
        private static bool _prefsLoaded = false;
        private static bool _cancelPending = false;
        public static bool AutoCloseMenu { get; private set; } = true;
        public static bool ConsumeCancelPending()
        {
            if (!_cancelPending) return false;
            _cancelPending = false;
            return true;
        }

        public static void ToggleAutoCloseMenu()
        {
            AutoCloseMenu = !AutoCloseMenu;
            SavePrefs();
            ModLog.Feedback("[ObjectPlacer] Autoclose menu -> " + (AutoCloseMenu ? "ON" : "OFF"));
        }
        private const float PitchMin = -80f;
        private const float PitchMax = 80f;
        private const float StickDeadzone = 0.15f;
        private const float SpawnForwardDist = 6f;
        private const int MaxHarvest = 80;
        private static readonly HashSet<string> _harvestKeys = new HashSet<string>();

        private static readonly Vector3 CamOffset = new Vector3(0f, 4.5f, -9f);
        private static readonly Vector3 CamLookOffset = new Vector3(0f, 1f, 0f);

        private static Vehicle _vehicle = null;
        private static Rigidbody _rb = null;
        private static VehicleController _vc = null;
        private static Transform _playerTrans = null;

        private static FieldInfo _physField = null;
        private static MethodInfo _toggleCtrl = null;

        private static bool _savedKinematic = false;
        private static bool _savedGravity = false;
        private static bool _savedNoBail = false;

        private static GameObject _ghost = null;
        private static float _yaw = 0f;
        private static float _pitch = 0f;
        private static float _roll = 0f;

        private static readonly List<GameObject> _placedRamps = new List<GameObject>();

        public static void Toggle()
        {
            if (!Enabled) Enable();
            else Exit();
            ModLog.Feedback("[ObjectPlacer] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void PrevObject()
        {
            EnsureCatalog();
            if (_catalog.Count == 0) return;
            SelectedIndex--;
            if (SelectedIndex < 0) SelectedIndex = _catalog.Count - 1;
            RefreshGhostShape();
            ModLog.Feedback("[ObjectPlacer] Object -> " + SelectedName);
        }

        public static void NextObject()
        {
            EnsureCatalog();
            if (_catalog.Count == 0) return;
            SelectedIndex++;
            if (SelectedIndex >= _catalog.Count) SelectedIndex = 0;
            RefreshGhostShape();
            ModLog.Feedback("[ObjectPlacer] Object -> " + SelectedName);
        }

        public static void SetObject(int index)
        {
            EnsureCatalog();
            if (index < 0 || index >= _catalog.Count) return;
            if (SelectedIndex == index) return;
            SelectedIndex = index;
            RefreshGhostShape();
            ModLog.Feedback("[ObjectPlacer] Object -> " + SelectedName);
        }

        private static void Enable()
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null)
                {
                    ModLog.Warn("[ObjectPlacer] Player_Human not found.");
                    return;
                }

                _playerTrans = player.transform;
                _vehicle = player.GetComponent<Vehicle>();
                _vc = player.GetComponent<VehicleController>();
                _rb = player.GetComponentInChildren<Rigidbody>();

                if ((object)_vehicle == null || (object)_rb == null)
                {
                    ModLog.Warn("[ObjectPlacer] vehicle=" + ((object)_vehicle != null)
                        + " rb=" + ((object)_rb != null) + " - aborting.");
                    return;
                }

                if ((object)_physField == null)
                {
                    _physField = _vehicle.GetType().GetField("bYxcVhv",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if ((object)_physField == null)
                        _physField = typeof(Vehicle).GetField("bYxcVhv",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if ((object)_toggleCtrl == null)
                    _toggleCtrl = typeof(VehicleController).GetMethod("ToggleControl",
                        BindingFlags.Public | BindingFlags.Instance);

                _savedKinematic = _rb.isKinematic;
                _savedGravity = _rb.useGravity;
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.useGravity = false;
                _rb.isKinematic = true;

                if ((object)_physField != null) _physField.SetValue(_vehicle, false);
                if ((object)_vc != null && (object)_toggleCtrl != null)
                    _toggleCtrl.Invoke(_vc, new object[] { false, false });

                _savedNoBail = NoBail.Enabled;
                NoBail.SetEnabled(true);

                ScanMap();

                Enabled = true;
                ModLog.Debug("[ObjectPlacer] Placement mode ON. physField=" + ((object)_physField != null)
                    + " toggleCtrl=" + ((object)_toggleCtrl != null)
                    + " harvested=" + HarvestedCount);

                SpawnGhost();
            }
            catch (Exception ex)
            {
                ModLog.Error("[ObjectPlacer] Enable failed: " + ex.Message, ex, "ObjectPlacer");
                Enabled = false;
            }
        }

        private static void SpawnGhost()
        {
            try
            {
                Vector3 forward = _playerTrans.forward; forward.y = 0f;
                if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
                forward.Normalize();

                Vector3 targetXZ = _playerTrans.position + forward * SpawnForwardDist;
                float groundY = GetGroundHeight(targetXZ);
                Vector3 spawnPos = new Vector3(targetXZ.x, groundY, targetXZ.z);

                _ghost = BuildSelectedObject("ObjectPlacer_Ghost");
                if ((object)_ghost == null)
                {
                    ModLog.Warn("[ObjectPlacer] BuildSelectedObject returned null.");
                    IsPlacing = false;
                    return;
                }
                _yaw = _playerTrans.eulerAngles.y;
                _pitch = 0f;
                _roll = 0f;
                _ghost.transform.position = spawnPos;
                ApplyGhostRotation();
                PrepareInstance(_ghost, true);

                IsPlacing = true;
                ModLog.Debug("[ObjectPlacer] Ghost spawned (" + SelectedName + ") at " + spawnPos);
            }
            catch (Exception ex)
            {
                ModLog.Error("[ObjectPlacer] SpawnGhost failed: " + ex.Message, ex, "ObjectPlacer");
                IsPlacing = false;
            }
        }

        public static void Tick()
        {
            if (!Enabled) return;
            if (WasCancelPressed()) { Exit(true); return; }
            if ((object)_vehicle == null || (object)_rb == null) { Exit(); return; }

            try
            {
                if ((object)_physField != null) _physField.SetValue(_vehicle, false);
                if (!_rb.isKinematic)
                {
                    _rb.velocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                    _rb.isKinematic = true;
                }

                if (!IsPlacing || (object)_ghost == null) return;

                InControl.InputDevice dev = InControl.InputManager.ActiveDevice;

                float up = (float)dev.RightTrigger;
                float down = (float)dev.LeftTrigger;
                if (Input.GetKey(KeyCode.E)) up = 1f;
                if (Input.GetKey(KeyCode.Q)) down = 1f;
                float liftDelta = (up - down) * LiftSpeed * Time.deltaTime;
                if (Mathf.Abs(liftDelta) > 0.0001f)
                    _ghost.transform.position += Vector3.up * liftDelta;

                float lsX = (float)dev.LeftStick.X;
                float lsY = (float)dev.LeftStick.Y;
                if (Input.GetKey(KeyCode.A)) lsX = -1f;
                if (Input.GetKey(KeyCode.D)) lsX = 1f;
                if (Input.GetKey(KeyCode.W)) lsY = 1f;
                if (Input.GetKey(KeyCode.S)) lsY = -1f;
                if (Mathf.Abs(lsX) < StickDeadzone) lsX = 0f;
                if (Mathf.Abs(lsY) < StickDeadzone) lsY = 0f;
                if (lsX != 0f || lsY != 0f)
                {
                    Quaternion heading = Quaternion.Euler(0f, _yaw, 0f);
                    Vector3 move = heading * new Vector3(lsX, 0f, lsY);
                    _ghost.transform.position += move * MoveSpeed * Time.deltaTime;
                }

                float rsX = (float)dev.RightStick.X;
                float rsY = (float)dev.RightStick.Y;
                if (Mathf.Abs(rsX) > StickDeadzone)
                    _yaw += rsX * YawSpeed * Time.deltaTime;
                if (Mathf.Abs(rsY) > StickDeadzone)
                    _pitch += rsY * PitchSpeed * Time.deltaTime;
                _pitch = Mathf.Clamp(_pitch, PitchMin, PitchMax);

                float rollInput = 0f;
                if (dev.RightBumper.IsPressed) rollInput += 1f;
                if (dev.LeftBumper.IsPressed) rollInput -= 1f;
                if (Input.GetKey(KeyCode.C)) rollInput += 1f;
                if (Input.GetKey(KeyCode.Z)) rollInput -= 1f;
                if (rollInput > 1f) rollInput = 1f;
                if (rollInput < -1f) rollInput = -1f;
                if (rollInput != 0f)
                    _roll += rollInput * YawSpeed * Time.deltaTime;

                ApplyGhostRotation();

                if (dev.DPadLeft.WasPressed || Input.GetKeyDown(KeyCode.LeftBracket)
                    || Input.GetKeyDown(KeyCode.Comma))
                    PrevObject();
                else if (dev.DPadRight.WasPressed || Input.GetKeyDown(KeyCode.RightBracket)
                    || Input.GetKeyDown(KeyCode.Period))
                    NextObject();

                bool confirm = dev.Action1.WasPressed || Input.GetKeyDown(KeyCode.Return);

                if (confirm) ConfirmPlacement();

                Camera cam = Camera.main;
                if ((object)cam != null && (object)_ghost != null)
                {
                    Quaternion heading = Quaternion.Euler(0f, _yaw, 0f);
                    cam.transform.position = _ghost.transform.position + heading * CamOffset;
                    cam.transform.LookAt(_ghost.transform.position + CamLookOffset);
                }
            }
            catch (Exception ex)
            {
                ModLog.Error("[ObjectPlacer] Tick failed: " + ex.Message, ex, "ObjectPlacer");
                Exit();
            }
        }

        private static void ConfirmPlacement()
        {
            try
            {
                if ((object)_ghost == null) return;

                Vector3 pos = _ghost.transform.position;
                Quaternion rot = _ghost.transform.rotation;
                GameObject.Destroy(_ghost);
                _ghost = null;

                string safe = SelectedName.Replace(" ", "");
                GameObject placed = BuildSelectedObject("Placed_" + safe + "_" + _placedRamps.Count);
                if ((object)placed == null) return;
                placed.transform.position = pos;
                placed.transform.rotation = rot;
                PrepareInstance(placed, false);

                _placedRamps.Add(placed);
                ModLog.Debug("[ObjectPlacer] Placed " + SelectedName + " #" + _placedRamps.Count
                    + " at " + pos);

                SpawnGhost();
            }
            catch (Exception ex)
            {
                ModLog.Error("[ObjectPlacer] ConfirmPlacement failed: " + ex.Message, ex, "ObjectPlacer");
            }
        }

        private static bool WasCancelPressed()
        {
            try
            {
                InControl.InputDevice dev = InControl.InputManager.ActiveDevice;
                if ((object)dev != null && dev.Action2.WasPressed) return true;
            }
            catch { }
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
        }

        private static void Exit(bool fromCancel = false)
        {
            try
            {
                if ((object)_ghost != null) { GameObject.Destroy(_ghost); _ghost = null; }
                IsPlacing = false;

                if ((object)_vehicle != null && (object)_physField != null)
                    _physField.SetValue(_vehicle, true);

                if ((object)_rb != null)
                {
                    _rb.isKinematic = _savedKinematic;
                    _rb.useGravity = _savedGravity;
                    _rb.velocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }

                if ((object)_vc != null && (object)_toggleCtrl != null)
                    _toggleCtrl.Invoke(_vc, new object[] { true, true });

                NoBail.SetEnabled(_savedNoBail);

                ModLog.Debug("[ObjectPlacer] Placement mode OFF. " + _placedRamps.Count + " object(s) this session.");
            }
            catch (Exception ex)
            {
                ModLog.Error("[ObjectPlacer] Exit failed: " + ex.Message, ex, "ObjectPlacer");
            }

            Enabled = false;
            if (fromCancel)
            {
                _cancelPending = true;
                ModLog.Feedback("[ObjectPlacer] -> OFF");
            }
            _vehicle = null; _rb = null; _vc = null; _playerTrans = null;
        }

        private static void ApplyGhostRotation()
        {
            if ((object)_ghost == null) return;
            EnsureCatalog();
            Quaternion extra = Quaternion.identity;
            if (SelectedIndex >= 0 && SelectedIndex < _catalog.Count
                && _catalog[SelectedIndex].IsPrimitive
                && _catalog[SelectedIndex].Shape == PrimitiveType.Cylinder)
                extra = Quaternion.Euler(90f, 0f, 0f);
            _ghost.transform.rotation = Quaternion.Euler(_pitch, _yaw, _roll) * extra;
        }

        private static void RefreshGhostShape()
        {
            if (!IsPlacing || (object)_ghost == null) return;
            Vector3 pos = _ghost.transform.position;
            GameObject.Destroy(_ghost);
            _ghost = BuildSelectedObject("ObjectPlacer_Ghost");
            if ((object)_ghost == null) return;
            _ghost.transform.position = pos;
            ApplyGhostRotation();
            PrepareInstance(_ghost, true);
        }

        private static GameObject BuildSelectedObject(string name)
        {
            EnsureCatalog();
            if (SelectedIndex < 0 || SelectedIndex >= _catalog.Count) return null;
            Placeable e = _catalog[SelectedIndex];

            if (!e.IsPrimitive)
            {
                if ((object)e.Template == null) return BuildPrimitiveFallback(name);
                GameObject clone = UnityEngine.Object.Instantiate(e.Template);
                clone.name = name;
                clone.SetActive(true);
                clone.transform.SetParent(null, true);
                StripRuntimeJunk(clone);
                return clone;
            }

            return BuildPrimitive(name, e);
        }

        private static GameObject BuildPrimitiveFallback(string name)
        {
            Placeable fallback = default(Placeable);
            fallback.Name = "Ramp";
            fallback.IsPrimitive = true;
            fallback.Shape = PrimitiveType.Cube;
            fallback.Scale = new Vector3(2.5f, 1.5f, 4f);
            fallback.Color = new Color(0.75f, 0.35f, 0.1f);
            return BuildPrimitive(name, fallback);
        }

        private static GameObject BuildPrimitive(string name, Placeable e)
        {
            GameObject go = GameObject.CreatePrimitive(e.Shape);
            go.name = name;
            go.transform.localScale = e.Scale;

            Renderer rend = go.GetComponent<Renderer>();
            if ((object)rend != null)
                rend.material.color = e.Color;

            Collider col = go.GetComponent<Collider>();
            if ((object)col != null)
            {
                PhysicMaterial mat = new PhysicMaterial("ObjectPlacerMat");
                mat.staticFriction = 0.8f;
                mat.dynamicFriction = 0.8f;
                mat.frictionCombine = PhysicMaterialCombine.Maximum;
                mat.bounciness = 0f;
                mat.bounceCombine = PhysicMaterialCombine.Minimum;
                col.material = mat;
            }

            return go;
        }

        private static void PrepareInstance(GameObject go, bool ghost)
        {
            Collider[] cols = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
                if ((object)cols[i] != null) cols[i].enabled = !ghost;

            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                if ((object)rends[i] == null) continue;
                rends[i].enabled = true;
                if (ghost && SelectedIsPrimitive())
                    SetTransparent(rends[i].material);
            }
        }

        private static bool SelectedIsPrimitive()
        {
            EnsureCatalog();
            return SelectedIndex >= 0 && SelectedIndex < _catalog.Count && _catalog[SelectedIndex].IsPrimitive;
        }

        private static void SetTransparent(Material mat)
        {
            try
            {
                mat.SetFloat("_Mode", 3f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                Color c = mat.color;
                mat.color = new Color(c.r, c.g, c.b, 0.5f);
            }
            catch (Exception ex)
            {
                ModLog.Warn("[ObjectPlacer] SetTransparent skipped (non-critical): " + ex.Message);
            }
        }

        // ── Harvest live map meshes ──────────────────────────────────
        public static void ScanMap()
        {
            EnsureCatalog();

            int added = 0;
            try
            {
                MeshFilter[] filters = Resources.FindObjectsOfTypeAll<MeshFilter>();

                for (int i = 0; i < filters.Length; i++)
                {
                    if (HarvestedCount + added >= MaxHarvest) break;

                    MeshFilter mf = filters[i];
                    if ((object)mf == null) continue;
                    GameObject go = mf.gameObject;
                    if ((object)go == null || !go.activeInHierarchy) continue;
                    if (go.hideFlags != HideFlags.None) continue;
                    if (IsUnderHolder(go.transform)) continue;

                    Mesh mesh = mf.sharedMesh;
                    if ((object)mesh == null) continue;

                    string key = HarvestKey(mesh);
                    if (_harvestKeys.Contains(key)) continue;

                    string goName = go.name;
                    string meshName = mesh.name;
                    if (IsBlockedName(goName) || IsBlockedName(meshName)) continue;
                    if (!LooksLikeRamp(goName) && !LooksLikeRamp(meshName)
                        && !AncestorLooksLikeRamp(go.transform))
                        continue;

                    Vector3 size = Vector3.Scale(mesh.bounds.size, go.transform.lossyScale);
                    float max = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
                    if (max < 1.2f || max > 55f) continue;

                    GameObject root = FindHarvestRoot(go);
                    if ((object)root == null) continue;

                    GameObject template = UnityEngine.Object.Instantiate(root);
                    string label = UniqueHarvestName(CleanName(root.name), CleanName(meshName));
                    template.name = label;
                    StripRuntimeJunk(template);
                    template.SetActive(false);
                    EnsureHolder();
                    template.transform.SetParent(_holder.transform, false);
                    template.transform.localPosition = Vector3.zero;
                    template.transform.localRotation = Quaternion.identity;

                    _harvestKeys.Add(key);
                    Placeable p = new Placeable();
                    p.Id = "m:" + key;
                    p.Name = label;
                    p.Group = ClassifyGroup(label, false);
                    p.IsPrimitive = false;
                    p.Template = template;
                    _catalog.Insert(HarvestedCount + added, p);
                    added++;
                }
            }
            catch (Exception ex)
            {
                ModLog.Error("[ObjectPlacer] ScanMap: " + ex.Message, ex, "ObjectPlacer");
            }

            HarvestedCount += added;
            RefreshGhostShape();
            if (added > 0)
                ModLog.Feedback("[ObjectPlacer] Added " + added + " — library " + HarvestedCount);
            else
                ModLog.Feedback("[ObjectPlacer] No new objects — library " + HarvestedCount);
        }

        private static string HarvestKey(Mesh mesh)
        {
            Vector3 b = mesh.bounds.size;
            return mesh.name + "|" + mesh.vertexCount + "|"
                + b.x.ToString("F1") + "x" + b.y.ToString("F1") + "x" + b.z.ToString("F1");
        }

        private static string UniqueHarvestName(string rootName, string meshName)
        {
            string label = rootName;
            if (string.IsNullOrEmpty(label) || string.Equals(label, "LOD0", StringComparison.OrdinalIgnoreCase))
                label = meshName;
            if (string.IsNullOrEmpty(label)) label = "Map Object";

            bool taken = false;
            for (int i = 0; i < _catalog.Count; i++)
            {
                if (string.Equals(_catalog[i].Name, label, StringComparison.Ordinal))
                { taken = true; break; }
            }
            if (!taken) return label;

            int n = 2;
            while (n < 99)
            {
                string candidate = label + " " + n;
                bool hit = false;
                for (int i = 0; i < _catalog.Count; i++)
                {
                    if (string.Equals(_catalog[i].Name, candidate, StringComparison.Ordinal))
                    { hit = true; break; }
                }
                if (!hit) return candidate;
                n++;
            }
            return label;
        }

        private static bool LooksLikeRamp(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant();
            for (int i = 0; i < RampKeywords.Length; i++)
                if (n.IndexOf(RampKeywords[i], StringComparison.Ordinal) >= 0)
                    return true;
            return false;
        }

        private static bool IsBlockedName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant();
            for (int i = 0; i < BlockKeywords.Length; i++)
                if (n.IndexOf(BlockKeywords[i], StringComparison.Ordinal) >= 0)
                    return true;
            return false;
        }

        private static bool AncestorLooksLikeRamp(Transform t)
        {
            Transform p = t.parent;
            int depth = 0;
            while ((object)p != null && depth < 4)
            {
                if (LooksLikeRamp(p.name)) return true;
                p = p.parent;
                depth++;
            }
            return false;
        }

        private static GameObject FindHarvestRoot(GameObject go)
        {
            Transform t = go.transform;
            int guard = 0;
            while ((object)t.parent != null && guard < 6)
            {
                string n = t.name;
                bool lod = n.StartsWith("LOD", StringComparison.OrdinalIgnoreCase)
                    || n.StartsWith("Collision", StringComparison.OrdinalIgnoreCase);
                bool parentBetter = LooksLikeRamp(t.parent.name) && !LooksLikeRamp(n);
                if (!lod && !parentBetter) break;
                t = t.parent;
                guard++;
            }
            return t.gameObject;
        }

        private static void StripRuntimeJunk(GameObject go)
        {
            LODGroup[] lods = go.GetComponentsInChildren<LODGroup>(true);
            for (int i = 0; i < lods.Length; i++)
                if ((object)lods[i] != null) UnityEngine.Object.DestroyImmediate(lods[i]);

            MonoBehaviour[] mbs = go.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < mbs.Length; i++)
            {
                MonoBehaviour mb = mbs[i];
                if ((object)mb == null) continue;
                string tn = mb.GetType().Name;
                if (tn.IndexOf("GPUInstancer", StringComparison.OrdinalIgnoreCase) >= 0
                    || tn.IndexOf("Photon", StringComparison.OrdinalIgnoreCase) >= 0
                    || tn.IndexOf("Network", StringComparison.OrdinalIgnoreCase) >= 0)
                    UnityEngine.Object.DestroyImmediate(mb);
            }

            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
                if ((object)rends[i] != null) rends[i].enabled = true;
        }

        private static string CleanName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string n = raw.Replace("(Clone)", "").Trim();
            int cut = n.LastIndexOf('/');
            if (cut >= 0 && cut < n.Length - 1) n = n.Substring(cut + 1);
            if (n.Length > 28) n = n.Substring(0, 28);
            return n;
        }

        private static bool IsUnderHolder(Transform t)
        {
            if ((object)_holder == null) return false;
            Transform p = t;
            while ((object)p != null)
            {
                if (p.gameObject == _holder) return true;
                p = p.parent;
            }
            return false;
        }

        private static void EnsureHolder()
        {
            if ((object)_holder != null) return;
            _holder = new GameObject("ObjectPlacer_Templates");
            UnityEngine.Object.DontDestroyOnLoad(_holder);
            _holder.SetActive(false);
        }

        private static void EnsureCatalog()
        {
            EnsurePrefs();
            if (_catalog.Count > 0) return;
            SeedPrimitives();
        }

        private static void SeedPrimitives()
        {
            AddPrim("Block Ramp", PrimitiveType.Cube, new Vector3(2.5f, 1.5f, 4f), new Color(0.75f, 0.35f, 0.10f));
            AddPrim("Small Ramp", PrimitiveType.Cube, new Vector3(1.8f, 0.9f, 2.5f), new Color(0.80f, 0.45f, 0.15f));
            AddPrim("Big Ramp", PrimitiveType.Cube, new Vector3(4f, 2.2f, 7f), new Color(0.70f, 0.28f, 0.08f));
            AddPrim("Kicker", PrimitiveType.Cube, new Vector3(2.2f, 2f, 2.8f), new Color(0.85f, 0.30f, 0.12f));
            AddPrim("Platform", PrimitiveType.Cube, new Vector3(4f, 0.4f, 4f), new Color(0.45f, 0.45f, 0.48f));
            AddPrim("Wide Deck", PrimitiveType.Cube, new Vector3(8f, 0.4f, 3f), new Color(0.40f, 0.42f, 0.46f));
            AddPrim("Wall", PrimitiveType.Cube, new Vector3(0.4f, 3f, 4f), new Color(0.55f, 0.55f, 0.58f));
            AddPrim("Rail", PrimitiveType.Cube, new Vector3(0.25f, 0.25f, 6f), new Color(0.72f, 0.74f, 0.78f));
            AddPrim("Step", PrimitiveType.Cube, new Vector3(2f, 0.6f, 1.5f), new Color(0.50f, 0.42f, 0.32f));
            AddPrim("Log", PrimitiveType.Cylinder, new Vector3(0.6f, 2.5f, 0.6f), new Color(0.42f, 0.28f, 0.14f));
            AddPrim("Boulder", PrimitiveType.Sphere, new Vector3(2f, 2f, 2f), new Color(0.38f, 0.38f, 0.36f));
            AddPrim("Barrier", PrimitiveType.Cube, new Vector3(3f, 1.2f, 0.4f), new Color(0.85f, 0.15f, 0.12f));
        }

        private static void AddPrim(string name, PrimitiveType shape, Vector3 scale, Color color)
        {
            Placeable p = new Placeable();
            p.Id = "p:" + name;
            p.Name = name;
            p.Group = ClassifyGroup(name, true);
            p.IsPrimitive = true;
            p.Shape = shape;
            p.Scale = scale;
            p.Color = color;
            _catalog.Add(p);
        }

        private static string ClassifyGroup(string name, bool primitive)
        {
            if (string.IsNullOrEmpty(name)) return primitive ? "Blocks" : "Props";
            string n = name.ToLowerInvariant();
            if (n.IndexOf("kicker", StringComparison.Ordinal) >= 0
                || n.IndexOf("stepup", StringComparison.Ordinal) >= 0
                || n.IndexOf("step-up", StringComparison.Ordinal) >= 0)
                return "Kickers";
            if (n.IndexOf("rail", StringComparison.Ordinal) >= 0
                || n.IndexOf("ladder", StringComparison.Ordinal) >= 0)
                return "Rails";
            if (n.IndexOf("wall", StringComparison.Ordinal) >= 0
                || n.IndexOf("barrier", StringComparison.Ordinal) >= 0)
                return "Walls";
            if (n.IndexOf("platform", StringComparison.Ordinal) >= 0
                || n.IndexOf("deck", StringComparison.Ordinal) >= 0
                || (n.IndexOf("box", StringComparison.Ordinal) >= 0 && n.IndexOf("boxjump", StringComparison.Ordinal) < 0))
                return "Platforms";
            if (n.IndexOf("ramp", StringComparison.Ordinal) >= 0
                || n.IndexOf("berm", StringComparison.Ordinal) >= 0
                || n.IndexOf("jump", StringComparison.Ordinal) >= 0
                || n.IndexOf("takeoff", StringComparison.Ordinal) >= 0
                || n.IndexOf("landing", StringComparison.Ordinal) >= 0
                || n.IndexOf("spine", StringComparison.Ordinal) >= 0
                || n.IndexOf("hip", StringComparison.Ordinal) >= 0
                || n.IndexOf("gap", StringComparison.Ordinal) >= 0
                || n.IndexOf("table", StringComparison.Ordinal) >= 0
                || n.IndexOf("quarter", StringComparison.Ordinal) >= 0
                || n.IndexOf("wood", StringComparison.Ordinal) >= 0
                || n.IndexOf("northshore", StringComparison.Ordinal) >= 0)
                return "Ramps";
            if (primitive) return "Blocks";
            return "Props";
        }

        public static void ClearHarvested()
        {
            for (int i = _catalog.Count - 1; i >= 0; i--)
            {
                if (_catalog[i].IsPrimitive) continue;
                if ((object)_catalog[i].Template != null)
                    UnityEngine.Object.Destroy(_catalog[i].Template);
                _catalog.RemoveAt(i);
            }
            HarvestedCount = 0;
            _harvestKeys.Clear();
            if ((object)_holder != null)
            {
                UnityEngine.Object.Destroy(_holder);
                _holder = null;
            }
            if (_catalog.Count == 0) SeedPrimitives();
            if (SelectedIndex >= _catalog.Count) SelectedIndex = 0;
            RefreshGhostShape();
            ModLog.Feedback("[ObjectPlacer] Library cleared.");
        }

        private static float GetGroundHeight(Vector3 worldPos)
        {
            Terrain terrain = Terrain.activeTerrain;
            if ((object)(UnityEngine.Object)terrain != null
                && (object)(UnityEngine.Object)terrain.terrainData != null)
            {
                Vector3 rel = worldPos - terrain.transform.position;
                float nx = Mathf.InverseLerp(0f, terrain.terrainData.size.x, rel.x);
                float nz = Mathf.InverseLerp(0f, terrain.terrainData.size.z, rel.z);
                float h = terrain.terrainData.GetInterpolatedHeight(nx, nz)
                          + terrain.transform.position.y;
                if (h > 1f) return h;
            }

            RaycastHit hit;
            Vector3 castFrom = new Vector3(worldPos.x, worldPos.y + 500f, worldPos.z);
            if (Physics.Raycast(castFrom, Vector3.down, out hit, 1000f))
                return hit.point.y;

            return (object)_playerTrans != null ? _playerTrans.position.y : worldPos.y;
        }

        public static void ClearAll()
        {
            ClearPlaced(true);
        }

        private static void ClearPlaced(bool log)
        {
            bool had = _placedRamps.Count > 0 || (object)_ghost != null;
            for (int i = 0; i < _placedRamps.Count; i++)
                if ((object)_placedRamps[i] != null) GameObject.Destroy(_placedRamps[i]);
            _placedRamps.Clear();

            if ((object)_ghost != null) { GameObject.Destroy(_ghost); _ghost = null; }
            IsPlacing = false;

            if (log && had) ModLog.Feedback("[ObjectPlacer] Cleared session objects.");
        }

        public static void Reset()
        {
            // Keep the harvested library across scene changes — templates live
            // on a DontDestroyOnLoad holder. Only stop placing and drop instances.
            if (Enabled) Exit();
            ClearPlaced(false);
            _physField = null;
            _toggleCtrl = null;
        }

        private static string PrefsPath
        {
            get
            {
                string dir = Path.Combine(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData"),
                    "DescendersModMenu");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, "ObjectPlacer.txt");
            }
        }

        private static void EnsurePrefs()
        {
            if (_prefsLoaded) return;
            _prefsLoaded = true;
            LoadPrefs();
        }

        private static void LoadPrefs()
        {
            try
            {
                string path = PrefsPath;
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (string.IsNullOrEmpty(line)) continue;
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        string key = line.Substring(0, eq).Trim();
                        string val = line.Substring(eq + 1).Trim();
                        if (key == "move") { int n; if (int.TryParse(val, out n)) MoveSpeedLevel = ClampLevel(n); }
                        else if (key == "rotate") { int n; if (int.TryParse(val, out n)) RotateSpeedLevel = ClampLevel(n); }
                        else if (key == "lift") { int n; if (int.TryParse(val, out n)) LiftSpeedLevel = ClampLevel(n); }
                        else if (key == "autoclose") AutoCloseMenu = val != "0" && !string.Equals(val, "false", StringComparison.OrdinalIgnoreCase);
                        else if (key == "favs")
                        {
                            _favIds.Clear();
                            string[] parts = val.Split(',');
                            for (int p = 0; p < parts.Length; p++)
                            {
                                string id = parts[p].Trim();
                                if (id.Length > 0) _favIds.Add(id);
                            }
                        }
                    }
                    return;
                }

                MoveSpeedLevel = ClampLevel(PlayerPrefs.GetInt("DS_ObjectPlacer_Move", 5));
                RotateSpeedLevel = ClampLevel(PlayerPrefs.GetInt("DS_ObjectPlacer_Rotate", 5));
                LiftSpeedLevel = ClampLevel(PlayerPrefs.GetInt("DS_ObjectPlacer_Lift", 5));
                string raw = PlayerPrefs.GetString("DS_ObjectPlacer_Favs", "");
                _favIds.Clear();
                if (!string.IsNullOrEmpty(raw))
                {
                    string[] parts = raw.Split(',');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        string id = parts[i].Trim();
                        if (id.Length > 0) _favIds.Add(id);
                    }
                }
                if (_favIds.Count > 0 || MoveSpeedLevel != 5 || RotateSpeedLevel != 5 || LiftSpeedLevel != 5)
                    SavePrefs();
            }
            catch (Exception ex)
            {
                ModLog.Warn("[ObjectPlacer] LoadPrefs: " + ex.Message);
            }
        }

        private static void SavePrefs()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("move=").Append(MoveSpeedLevel).Append('\n');
                sb.Append("rotate=").Append(RotateSpeedLevel).Append('\n');
                sb.Append("lift=").Append(LiftSpeedLevel).Append('\n');
                sb.Append("autoclose=").Append(AutoCloseMenu ? "1" : "0").Append('\n');
                sb.Append("favs=");
                bool first = true;
                foreach (string id in _favIds)
                {
                    if (!first) sb.Append(',');
                    sb.Append(id);
                    first = false;
                }
                sb.Append('\n');
                File.WriteAllText(PrefsPath, sb.ToString());
            }
            catch (Exception ex)
            {
                ModLog.Warn("[ObjectPlacer] SavePrefs: " + ex.Message);
            }
        }
    }
}
