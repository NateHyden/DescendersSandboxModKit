using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DescendersModMenu.Mods
{
    public class SceneDumper : MonoBehaviour
    {
        private static bool isDumping = false;

        // Capture everything - instance AND static, public AND private
        private const BindingFlags AllInstance =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private const BindingFlags AllStatic =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private const BindingFlags AllInstanceAndStatic =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private const BindingFlags DeclaredOnly =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.DeclaredOnly;

        // How many array/list items to show before truncating (0 = unlimited)
        private const int MaxCollectionItems = 0;

        public static void CheckHotkey()
        {
            if (Input.GetKeyDown(KeyCode.Hash) && !isDumping)
            {
                DumpCurrentScene();
            }
        }

        public static void DumpCurrentScene()
        {
            try
            {
                isDumping = true;

                string desktop = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                Scene scene = SceneManager.GetActiveScene();

                MelonLogger.Msg("SceneDumper: Starting full dump...");

                // ── FILE 1: Full scene hierarchy ──────────────────────────────────────
                {
                    GameObject[] sceneRoots = scene.GetRootGameObjects();

                    // Persistent UI (Sponsor Office, overworld menus, etc.) lives under
                    // DontDestroyOnLoad, which GetActiveScene() never sees. Standard trick:
                    // a temp DDOL object's .scene handle IS the DDOL pseudo-scene, so grab
                    // its roots the same way, then drop the temp object.
                    GameObject[] ddolRoots;
                    {
                        GameObject temp = new GameObject("SceneDumper_DDOLProbe");
                        UnityEngine.Object.DontDestroyOnLoad(temp);
                        ddolRoots = temp.scene.GetRootGameObjects();
                        UnityEngine.Object.Destroy(temp);
                    }

                    GameObject[] roots = new GameObject[sceneRoots.Length + ddolRoots.Length];
                    sceneRoots.CopyTo(roots, 0);
                    ddolRoots.CopyTo(roots, sceneRoots.Length);

                    StringBuilder sb = new StringBuilder(32 * 1024 * 1024);

                    sb.AppendLine("=== DESCENDERS FULL SCENE DUMP ===");
                    sb.AppendLine("Scene Name:  " + scene.name);
                    sb.AppendLine("Scene Path:  " + scene.path);
                    sb.AppendLine("Date:        " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sb.AppendLine("Root Count:  " + roots.Length + " (" + sceneRoots.Length + " scene + " + ddolRoots.Length + " DontDestroyOnLoad)");
                    sb.AppendLine("Dump Flags:  Fields + Properties + Methods + Static + Inheritance chain");
                    sb.AppendLine();

                    for (int i = 0; i < roots.Length; i++)
                        DumpGameObjectRecursive(roots[i].transform, sb, 0);

                    string path = Path.Combine(desktop, "DescendersSceneDump_FULL_" + timestamp + ".txt");
                    File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                    MelonLogger.Msg("Scene dump -> " + path);
                }

                // ── FILE 2: Vehicle forensics ─────────────────────────────────────────
                {
                    StringBuilder sb = new StringBuilder(8 * 1024 * 1024);

                    sb.AppendLine("=== DESCENDERS VEHICLE FORENSIC DUMP ===");
                    sb.AppendLine("Scene: " + scene.name + "  |  Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sb.AppendLine();

                    List<Component> vehicles = FindComponentsByTypeName("Vehicle");
                    sb.AppendLine("Vehicle Count: " + vehicles.Count);
                    sb.AppendLine();

                    for (int i = 0; i < vehicles.Count; i++)
                        DumpComponentFull(vehicles[i], sb, "Vehicle[" + i + "]", includeInheritance: true);

                    string path = Path.Combine(desktop, "DescendersVehicleForensics_" + timestamp + ".txt");
                    File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                    MelonLogger.Msg("Vehicle dump -> " + path);
                }

                // ── FILE 3: Player forensics (PlayerInfoImpact + VehicleController) ──
                {
                    StringBuilder sb = new StringBuilder(8 * 1024 * 1024);

                    sb.AppendLine("=== DESCENDERS PLAYER FORENSIC DUMP ===");
                    sb.AppendLine("Scene: " + scene.name + "  |  Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sb.AppendLine();

                    string[] playerTypes = new[]
                    {
                        "PlayerInfoImpact",
                        "PlayerInfo",
                        "PlayerManager",
                        "PlayerCustomization",
                        "PlayerObjectives",
                        "VehicleController",
                        "VehicleEvents",
                        "VehicleTricks",
                        "VehicleReplay",
                        "VehicleNetworking",
                        "VehicleSounds",
                        "Cyclist",
                        "CyclistBehaviour",
                        "CyclistModel",
                    };

                    foreach (string typeName in playerTypes)
                    {
                        List<Component> comps = FindComponentsByTypeName(typeName);
                        if (comps.Count == 0) continue;

                        sb.AppendLine("########################################");
                        sb.AppendLine("TYPE: " + typeName + "  (" + comps.Count + " instance(s))");
                        sb.AppendLine("########################################");
                        sb.AppendLine();

                        for (int i = 0; i < comps.Count; i++)
                            DumpComponentFull(comps[i], sb, typeName + "[" + i + "]", includeInheritance: true);
                    }

                    string path = Path.Combine(desktop, "DescendersPlayerForensics_" + timestamp + ".txt");
                    File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                    MelonLogger.Msg("Player dump -> " + path);
                }

                // ── FILE 4: All unique component types found in scene ─────────────────
                {
                    StringBuilder sb = new StringBuilder(4 * 1024 * 1024);

                    sb.AppendLine("=== DESCENDERS COMPONENT TYPE INDEX ===");
                    sb.AppendLine("Scene: " + scene.name + "  |  Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sb.AppendLine();

                    Dictionary<string, int> typeCounts = new Dictionary<string, int>();
                    Component[] allComps = UnityEngine.Object.FindObjectsOfType<Component>();

                    for (int i = 0; i < allComps.Length; i++)
                    {
                        if ((object)allComps[i] == null) continue;
                        string tn = allComps[i].GetType().FullName ?? allComps[i].GetType().Name;
                        if (typeCounts.ContainsKey(tn))
                            typeCounts[tn]++;
                        else
                            typeCounts[tn] = 1;
                    }

                    // Sort alphabetically
                    List<string> typeNames = new List<string>(typeCounts.Keys);
                    typeNames.Sort();

                    sb.AppendLine("Total unique component types: " + typeNames.Count);
                    sb.AppendLine("Total component instances:    " + allComps.Length);
                    sb.AppendLine();

                    foreach (string tn in typeNames)
                        sb.AppendLine(typeCounts[tn].ToString().PadLeft(5) + "x  " + tn);

                    string path = Path.Combine(desktop, "DescendersComponentIndex_" + timestamp + ".txt");
                    File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                    MelonLogger.Msg("Component index -> " + path);
                }

                MelonLogger.Msg("SceneDumper: All files written to game directory.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Scene dump failed: " + ex);
            }
            finally
            {
                isDumping = false;
            }
        }

        // ─── Full Component Dump ──────────────────────────────────────────────────────
        // Dumps fields, properties, methods, events — optionally walking the inheritance chain

        private static void DumpComponentFull(Component comp, StringBuilder sb,
            string label, bool includeInheritance)
        {
            if ((object)comp == null) return;

            Type type = comp.GetType();

            sb.AppendLine("══════════════════════════════════════════════════════════");
            sb.AppendLine("  " + label);
            sb.AppendLine("  Type:              " + SafeTypeName(type));
            sb.AppendLine("  GameObject:        " + comp.gameObject.name);
            sb.AppendLine("  Path:              " + GetFullPath(comp.transform));
            sb.AppendLine("  InstanceID:        " + comp.GetInstanceID());
            sb.AppendLine("  ActiveSelf:        " + comp.gameObject.activeSelf);
            sb.AppendLine("  ActiveInHierarchy: " + comp.gameObject.activeInHierarchy);
            sb.AppendLine("  Position:          " + FormatVector3(comp.transform.position));
            sb.AppendLine("  Rotation:          " + FormatVector3(comp.transform.rotation.eulerAngles));
            sb.AppendLine();

            // Rigidbody info if present
            Rigidbody rb = null;
            try { rb = comp.GetComponent<Rigidbody>(); } catch { }
            if ((object)rb != null)
            {
                sb.AppendLine("  ── RIGIDBODY ──");
                sb.AppendLine("  Velocity:       " + FormatVector3(rb.velocity));
                sb.AppendLine("  Speed:          " + rb.velocity.magnitude.ToString("F3"));
                sb.AppendLine("  AngVelocity:    " + FormatVector3(rb.angularVelocity));
                sb.AppendLine("  Mass:           " + rb.mass);
                sb.AppendLine("  Drag:           " + rb.drag);
                sb.AppendLine("  AngularDrag:    " + rb.angularDrag);
                sb.AppendLine("  IsKinematic:    " + rb.isKinematic);
                sb.AppendLine("  UseGravity:     " + rb.useGravity);
                sb.AppendLine("  CenterOfMass:   " + FormatVector3(rb.centerOfMass));
                sb.AppendLine("  MaxAngVelocity: " + rb.maxAngularVelocity);
                sb.AppendLine();
            }

            // Walk the type hierarchy
            List<Type> typeChain = new List<Type>();
            Type current = type;
            while ((object)current != null && (object)current != (object)typeof(object))
            {
                typeChain.Add(current);
                if (!includeInheritance) break;
                current = current.BaseType;
            }

            // ── INSTANCE FIELDS ───────────────────────────────────────────────────────
            sb.AppendLine("  ── INSTANCE FIELDS ──");
            HashSet<string> seenFields = new HashSet<string>();
            foreach (Type t in typeChain)
            {
                FieldInfo[] fields = t.GetFields(AllInstance | BindingFlags.DeclaredOnly);
                if (fields.Length == 0) continue;

                sb.AppendLine("    [from " + t.Name + "]");
                foreach (FieldInfo f in fields)
                {
                    if (seenFields.Contains(f.Name)) continue;
                    seenFields.Add(f.Name);

                    string val;
                    try { val = FormatValue(f.GetValue(comp)); }
                    catch (Exception ex) { val = "[ERR: " + ex.GetType().Name + "]"; }

                    sb.AppendLine("    " + AccessModifier(f) + " " +
                                  SafeTypeName(f.FieldType) + " " + f.Name + " = " + val);
                }
            }
            sb.AppendLine();

            // ── STATIC FIELDS ─────────────────────────────────────────────────────────
            sb.AppendLine("  ── STATIC FIELDS ──");
            HashSet<string> seenStatic = new HashSet<string>();
            foreach (Type t in typeChain)
            {
                FieldInfo[] fields = t.GetFields(AllStatic | BindingFlags.DeclaredOnly);
                if (fields.Length == 0) continue;

                sb.AppendLine("    [from " + t.Name + "]");
                foreach (FieldInfo f in fields)
                {
                    if (seenStatic.Contains(f.Name)) continue;
                    seenStatic.Add(f.Name);

                    string val;
                    try { val = FormatValue(f.GetValue(null)); }
                    catch (Exception ex) { val = "[ERR: " + ex.GetType().Name + "]"; }

                    sb.AppendLine("    static " + AccessModifier(f) + " " +
                                  SafeTypeName(f.FieldType) + " " + f.Name + " = " + val);
                }
            }
            sb.AppendLine();

            // ── INSTANCE PROPERTIES ───────────────────────────────────────────────────
            sb.AppendLine("  ── INSTANCE PROPERTIES ──");
            HashSet<string> seenProps = new HashSet<string>();
            foreach (Type t in typeChain)
            {
                PropertyInfo[] props = t.GetProperties(AllInstance | BindingFlags.DeclaredOnly);
                if (props.Length == 0) continue;

                sb.AppendLine("    [from " + t.Name + "]");
                foreach (PropertyInfo p in props)
                {
                    if (seenProps.Contains(p.Name)) continue;
                    seenProps.Add(p.Name);
                    if (p.GetIndexParameters().Length > 0) continue;

                    string val;
                    try
                    {
                        MethodInfo getter = p.GetGetMethod(true);
                        val = (object)getter != null
                            ? FormatValue(p.GetValue(comp, null))
                            : "[no getter]";
                    }
                    catch (Exception ex) { val = "[ERR: " + ex.GetType().Name + "]"; }

                    sb.AppendLine("    " + SafeTypeName(p.PropertyType) + " " + p.Name + " = " + val);
                }
            }
            sb.AppendLine();

            // ── METHODS ───────────────────────────────────────────────────────────────
            sb.AppendLine("  ── METHODS ──");
            HashSet<string> seenMethods = new HashSet<string>();
            foreach (Type t in typeChain)
            {
                MethodInfo[] methods = t.GetMethods(DeclaredOnly);
                if (methods.Length == 0) continue;

                sb.AppendLine("    [from " + t.Name + "]");
                foreach (MethodInfo m in methods)
                {
                    string sig = FormatMethodSignature(m);
                    if (seenMethods.Contains(sig)) continue;
                    seenMethods.Add(sig);

                    sb.AppendLine("    " + sig);
                }
            }
            sb.AppendLine();

            // ── EVENTS ────────────────────────────────────────────────────────────────
            sb.AppendLine("  ── EVENTS / DELEGATES ──");
            HashSet<string> seenEvents = new HashSet<string>();
            foreach (Type t in typeChain)
            {
                EventInfo[] events = t.GetEvents(AllInstanceAndStatic | BindingFlags.DeclaredOnly);
                foreach (EventInfo ev in events)
                {
                    if (seenEvents.Contains(ev.Name)) continue;
                    seenEvents.Add(ev.Name);
                    sb.AppendLine("    event " + SafeTypeName(ev.EventHandlerType) + " " + ev.Name);
                }

                // Also catch delegate fields (events not exposed via EventInfo)
                FieldInfo[] delegateFields = t.GetFields(AllInstanceAndStatic | BindingFlags.DeclaredOnly);
                foreach (FieldInfo f in delegateFields)
                {
                    if (!typeof(Delegate).IsAssignableFrom(f.FieldType)) continue;
                    if (seenEvents.Contains("field:" + f.Name)) continue;
                    seenEvents.Add("field:" + f.Name);

                    string val;
                    try
                    {
                        Delegate d = f.GetValue(f.IsStatic ? null : (object)comp) as Delegate;
                        if ((object)d == null)
                        {
                            val = "NULL";
                        }
                        else
                        {
                            Delegate[] invList = d.GetInvocationList();
                            val = invList.Length + " subscriber(s)";
                            foreach (Delegate sub in invList)
                                val += "\n        -> " + SafeTypeName(sub.Method.DeclaringType) + "." + sub.Method.Name;
                        }
                    }
                    catch (Exception ex) { val = "[ERR: " + ex.GetType().Name + "]"; }

                    sb.AppendLine("    delegate-field " + SafeTypeName(f.FieldType) + " " + f.Name + " = " + val);
                }
            }
            sb.AppendLine();

            // ── SIBLING / PARENT COMPONENTS ───────────────────────────────────────────
            sb.AppendLine("  ── SIBLING COMPONENTS ──");
            try
            {
                Component[] siblings = comp.gameObject.GetComponents<Component>();
                foreach (Component s in siblings)
                {
                    if ((object)s == null || s.GetInstanceID() == comp.GetInstanceID()) continue;
                    sb.AppendLine("    " + SafeTypeName(s.GetType()));
                }
            }
            catch (Exception ex) { sb.AppendLine("    [ERR: " + ex.GetType().Name + "]"); }
            sb.AppendLine();

            sb.AppendLine("  ── PARENT CHAIN ──");
            try
            {
                Transform p = comp.transform.parent;
                int depth = 0;
                while ((object)p != null && depth < 8)
                {
                    sb.Append("    [" + depth + "] " + p.name + "  |  Components: ");
                    Component[] pc = p.GetComponents<Component>();
                    List<string> pcNames = new List<string>();
                    foreach (Component c in pc)
                        if ((object)c != null) pcNames.Add(c.GetType().Name);
                    sb.AppendLine(string.Join(", ", pcNames.ToArray()));
                    p = p.parent;
                    depth++;
                }
            }
            catch (Exception ex) { sb.AppendLine("    [ERR: " + ex.GetType().Name + "]"); }

            sb.AppendLine();
        }

        // ─── Scene Hierarchy Dump ─────────────────────────────────────────────────────

        private static void DumpGameObjectRecursive(Transform t, StringBuilder sb, int depth)
        {
            if ((object)t == null) return;

            string indent = new string(' ', depth * 2);
            GameObject go = t.gameObject;

            sb.AppendLine(indent + "┌─ GameObject: " + go.name);
            sb.AppendLine(indent + "│  Path:              " + GetFullPath(t));
            sb.AppendLine(indent + "│  InstanceID:        " + go.GetInstanceID());
            sb.AppendLine(indent + "│  ActiveSelf:        " + go.activeSelf);
            sb.AppendLine(indent + "│  ActiveInHierarchy: " + go.activeInHierarchy);
            sb.AppendLine(indent + "│  Tag:               " + go.tag);
            sb.AppendLine(indent + "│  Layer:             " + go.layer + " (" + LayerMask.LayerToName(go.layer) + ")");
            sb.AppendLine(indent + "│  IsStatic:          " + go.isStatic);
            sb.AppendLine(indent + "│  Position:          " + FormatVector3(t.position));
            sb.AppendLine(indent + "│  Rotation:          " + FormatVector3(t.rotation.eulerAngles));
            sb.AppendLine(indent + "│  LocalPosition:     " + FormatVector3(t.localPosition));
            sb.AppendLine(indent + "│  LocalScale:        " + FormatVector3(t.localScale));
            sb.AppendLine(indent + "│  ChildCount:        " + t.childCount);

            Component[] components = go.GetComponents<Component>();
            sb.AppendLine(indent + "│  Components (" + components.Length + "):");

            for (int i = 0; i < components.Length; i++)
            {
                Component c = components[i];
                if ((object)c == null)
                {
                    sb.AppendLine(indent + "│    [Missing Component]");
                    continue;
                }
                DumpComponentInScene(c, sb, indent + "│    ");
            }

            sb.AppendLine(indent + "└─");
            sb.AppendLine();

            for (int i = 0; i < t.childCount; i++)
                DumpGameObjectRecursive(t.GetChild(i), sb, depth + 1);
        }

        // Used inside the scene hierarchy — dumps fields + properties + methods per component
        private static void DumpComponentInScene(Component c, StringBuilder sb, string indent)
        {
            Type type = c.GetType();

            sb.AppendLine(indent + "▸ " + SafeTypeName(type));

            // ── Fields (walk full inheritance chain) ──────────────────────────────────
            List<Type> chain = GetTypeChain(type);

            HashSet<string> seenF = new HashSet<string>();
            bool wroteFieldHeader = false;
            foreach (Type t in chain)
            {
                FieldInfo[] fields = t.GetFields(AllInstanceAndStatic | BindingFlags.DeclaredOnly);
                foreach (FieldInfo f in fields)
                {
                    if (seenF.Contains(f.Name)) continue;
                    seenF.Add(f.Name);

                    if (!wroteFieldHeader)
                    {
                        sb.AppendLine(indent + "  Fields:");
                        wroteFieldHeader = true;
                    }

                    string val;
                    try { val = FormatValue(f.IsStatic ? f.GetValue(null) : f.GetValue(c)); }
                    catch (Exception ex) { val = "[ERR: " + ex.GetType().Name + "]"; }

                    string staticTag = f.IsStatic ? "static " : "";
                    sb.AppendLine(indent + "    " + staticTag + SafeTypeName(f.FieldType) +
                                  " " + f.Name + " = " + val);
                }
            }

            // ── Properties ────────────────────────────────────────────────────────────
            HashSet<string> seenP = new HashSet<string>();
            bool wrotePropHeader = false;
            foreach (Type t in chain)
            {
                PropertyInfo[] props = t.GetProperties(AllInstance | BindingFlags.DeclaredOnly);
                foreach (PropertyInfo p in props)
                {
                    if (seenP.Contains(p.Name)) continue;
                    seenP.Add(p.Name);
                    if (p.GetIndexParameters().Length > 0) continue;

                    if (!wrotePropHeader)
                    {
                        sb.AppendLine(indent + "  Properties:");
                        wrotePropHeader = true;
                    }

                    string val;
                    try
                    {
                        MethodInfo getter = p.GetGetMethod(true);
                        val = (object)getter != null ? FormatValue(p.GetValue(c, null)) : "[no getter]";
                    }
                    catch (Exception ex) { val = "[ERR: " + ex.GetType().Name + "]"; }

                    sb.AppendLine(indent + "    " + SafeTypeName(p.PropertyType) +
                                  " " + p.Name + " = " + val);
                }
            }

            // ── Methods ───────────────────────────────────────────────────────────────
            HashSet<string> seenM = new HashSet<string>();
            bool wroteMethodHeader = false;
            foreach (Type t in chain)
            {
                MethodInfo[] methods = t.GetMethods(DeclaredOnly);
                foreach (MethodInfo m in methods)
                {
                    string sig = FormatMethodSignature(m);
                    if (seenM.Contains(sig)) continue;
                    seenM.Add(sig);

                    if (!wroteMethodHeader)
                    {
                        sb.AppendLine(indent + "  Methods:");
                        wroteMethodHeader = true;
                    }

                    sb.AppendLine(indent + "    " + sig);
                }
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────────

        private static List<Type> GetTypeChain(Type type)
        {
            List<Type> chain = new List<Type>();
            Type current = type;
            while ((object)current != null && (object)current != (object)typeof(object))
            {
                chain.Add(current);
                current = current.BaseType;
            }
            return chain;
        }

        private static List<Component> FindComponentsByTypeName(string typeName)
        {
            List<Component> results = new List<Component>();
            Component[] all = UnityEngine.Object.FindObjectsOfType<Component>();
            for (int i = 0; i < all.Length; i++)
            {
                if ((object)all[i] == null) continue;
                if (string.Equals(all[i].GetType().Name, typeName, StringComparison.Ordinal))
                    results.Add(all[i]);
            }
            return results;
        }

        private static string AccessModifier(FieldInfo f)
        {
            if (f.IsPublic) return "public";
            if (f.IsPrivate) return "private";
            if (f.IsFamily) return "protected";
            if (f.IsAssembly) return "internal";
            return "private";
        }

        private static string FormatMethodSignature(MethodInfo m)
        {
            StringBuilder sb = new StringBuilder();

            if (m.IsStatic) sb.Append("static ");
            if (m.IsPublic) sb.Append("public ");
            else if (m.IsPrivate) sb.Append("private ");
            else if (m.IsFamily) sb.Append("protected ");

            sb.Append(SafeTypeName(m.ReturnType));
            sb.Append(" ");
            sb.Append(m.Name);
            sb.Append("(");

            ParameterInfo[] parameters = m.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(SafeTypeName(parameters[i].ParameterType));
                sb.Append(" ");
                sb.Append(parameters[i].Name ?? "param" + i);
            }

            sb.Append(")");
            return sb.ToString();
        }

        private static string FormatValue(object value)
        {
            if ((object)value == null) return "NULL";

            Type valueType = value.GetType();

            if (value is string)
                return "\"" + (string)value + "\"";

            if (value is bool || valueType.IsPrimitive || value is decimal)
                return value.ToString();

            if (value is Vector2)
            {
                Vector2 v2 = (Vector2)value;
                return "(" + v2.x + ", " + v2.y + ")";
            }
            if (value is Vector3)
            {
                Vector3 v3 = (Vector3)value;
                return "(" + v3.x + ", " + v3.y + ", " + v3.z + ")";
            }
            if (value is Vector4)
            {
                Vector4 v4 = (Vector4)value;
                return "(" + v4.x + ", " + v4.y + ", " + v4.z + ", " + v4.w + ")";
            }

            if (value is Quaternion)
            {
                Quaternion q = (Quaternion)value;
                return "euler(" + q.eulerAngles.x + ", " + q.eulerAngles.y + ", " + q.eulerAngles.z + ")";
            }

            if (value is Color)
            {
                Color col = (Color)value;
                return "rgba(" + col.r + ", " + col.g + ", " + col.b + ", " + col.a + ")";
            }

            if (value is Enum)
                return value.ToString() + " (" + Convert.ToInt32(value) + ")";

            Delegate d = value as Delegate;
            if ((object)d != null)
            {
                Delegate[] invList = d.GetInvocationList();
                return "Delegate[" + invList.Length + " subscriber(s)]";
            }

            UnityEngine.Object unityObj = value as UnityEngine.Object;
            if ((object)unityObj != null)
            {
                GameObject go = unityObj as GameObject;
                if ((object)go != null)
                    return "GameObject{\"" + go.name + "\" path=\"" + GetFullPath(go.transform) + "\"}";

                Component comp = unityObj as Component;
                if ((object)comp != null)
                    return "Component{" + comp.GetType().Name + " on \"" + comp.gameObject.name + "\" path=\"" + GetFullPath(comp.transform) + "\"}";

                return "UnityObject{" + unityObj.GetType().Name + " name=\"" + unityObj.name + "\"}";
            }

            Array arr = value as Array;
            if ((object)arr != null)
            {
                int limit = MaxCollectionItems > 0 ? Mathf.Min(arr.Length, MaxCollectionItems) : arr.Length;
                StringBuilder sb = new StringBuilder();
                sb.Append("Array[" + arr.Length + "]");
                if (arr.Length > 0)
                {
                    sb.Append(" { ");
                    for (int i = 0; i < limit; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(FormatValue(arr.GetValue(i)));
                    }
                    if (arr.Length > limit) sb.Append(", ...");
                    sb.Append(" }");
                }
                return sb.ToString();
            }

            IList list = value as IList;
            if ((object)list != null)
            {
                int limit = MaxCollectionItems > 0 ? Mathf.Min(list.Count, MaxCollectionItems) : list.Count;
                StringBuilder sb = new StringBuilder();
                sb.Append("List[" + list.Count + "]");
                if (list.Count > 0)
                {
                    sb.Append(" { ");
                    for (int i = 0; i < limit; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(FormatValue(list[i]));
                    }
                    if (list.Count > limit) sb.Append(", ...");
                    sb.Append(" }");
                }
                return sb.ToString();
            }

            IDictionary dict = value as IDictionary;
            if ((object)dict != null)
            {
                int limit = MaxCollectionItems > 0 ? MaxCollectionItems : dict.Count;
                StringBuilder sb = new StringBuilder();
                sb.Append("Dict[" + dict.Count + "]");
                int n = 0;
                bool first = true;
                foreach (DictionaryEntry kvp in dict)
                {
                    if (n >= limit) { sb.Append(", ..."); break; }
                    sb.Append(first ? " { " : ", ");
                    sb.Append(FormatValue(kvp.Key) + " => " + FormatValue(kvp.Value));
                    first = false;
                    n++;
                }
                if (dict.Count > 0) sb.Append(" }");
                return sb.ToString();
            }

            return value.ToString();
        }

        private static string SafeTypeName(Type t)
        {
            if ((object)t == null) return "NULL_TYPE";
            try { if (!string.IsNullOrEmpty(t.FullName)) return t.FullName; } catch { }
            try { if (!string.IsNullOrEmpty(t.Name)) return t.Name; } catch { }
            return "UNKNOWN_TYPE";
        }

        private static string GetFullPath(Transform t)
        {
            if ((object)t == null) return string.Empty;
            StringBuilder sb = new StringBuilder(t.name);
            Transform current = t.parent;
            while ((object)current != null)
            {
                sb.Insert(0, current.name + "/");
                current = current.parent;
            }
            return sb.ToString();
        }

        private static string FormatVector3(Vector3 v)
        {
            return "(" + v.x.ToString("F3") + ", " + v.y.ToString("F3") + ", " + v.z.ToString("F3") + ")";
        }
    }
}