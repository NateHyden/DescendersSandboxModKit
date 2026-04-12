using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class SuspensionHUD
    {
        public static bool Enabled { get; private set; } = false;

        // ── Cached wheel refs (cleared on scene change) ───────────────
        private static Wheel _frontWheel = null;
        private static Wheel _rearWheel = null;
        private static bool _wheelsSearched = false;   // true once Player_Human was present & we ran full search

        // ── Reflection cache ──────────────────────────────────────────
        private static FieldInfo _suspField = null;
        private static bool _fieldCached = false;

        // ── Trick feed (UI_RepPopup) ──────────────────────────────────
        // Spawned once by UIManager — lives for the full session, not per-scene.
        // Cached separately so ClearCache() (scene change) never touches it.
        private static MonoBehaviour _repPopup = null;
        private static bool _repPopupSearched = false;

        // ── Hardtail detection ────────────────────────────────────────
        private static float _rearMaxComp = 0f;
        private static int _rearSamples = 0;
        private static bool _isHardtail = false;
        private const int HardtailFrames = 60;
        private const float HardtailThresh = 0.01f;

        // ── Auto-calibration ──────────────────────────────────────────
        // suspensionPress may be in metres (matching travel ~0.5) not 0-1.
        // Track the peak raw value per disc and normalise against it so bars
        // always fill proportionally regardless of the actual unit/range.
        private static float _frontCalibMax = 0.01f; // safe non-zero starting point
        private static float _rearCalibMax = 0.01f;
        private static int _calibLogCount = 0;     // log first few readings

        // ── OnGUI texture and cached styles ──────────────────────────
        private static Texture2D _tex = null;
        private static GUIStyle _lblStyle = null;
        private static GUIStyle _pctStyle = null;
        private static Texture2D Tex
        {
            get
            {
                if ((object)_tex == null)
                {
                    _tex = new Texture2D(1, 1);
                    _tex.SetPixel(0, 0, Color.white);
                    _tex.Apply();
                }
                return _tex;
            }
        }

        // ── Layout constants (base 1080p) ─────────────────────────────
        private const float BarW = 14f;
        private const float BarH = 220f;
        private const float BarGap = 8f;
        private const float MarginX = 30f;
        private const float MarginY = 40f;
        private const float LabelH = 14f;
        private const float PctH = 12f;

        // ─────────────────────────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            MelonLogger.Msg("[SuspensionHUD] -> " + (Enabled ? "ON" : "OFF"));
            SetTrickFeedVisible(!Enabled);
        }

        // ── Trick feed show/hide ───────────────────────────────────────
        private static void SetTrickFeedVisible(bool visible)
        {
            EnsureRepPopup();
            if ((object)_repPopup == null)
            {
                MelonLogger.Warning("[SuspensionHUD] UI_RepPopup not found — trick feed unchanged.");
                return;
            }
            _repPopup.gameObject.SetActive(visible);
            MelonLogger.Msg("[SuspensionHUD] Trick feed -> " + (visible ? "SHOWN" : "HIDDEN"));
        }

        // Cached type for UI_RepPopup — resolved once via assembly scan, avoids FindObjectsOfType
        private static System.Type _repPopupType = null;
        private static bool _repPopupTypeSearched = false;

        private static void EnsureRepPopup()
        {
            if (_repPopupSearched) return;
            _repPopupSearched = true;
            try
            {
                // Resolve UI_RepPopup type once — much cheaper than scanning all MonoBehaviours
                if (!_repPopupTypeSearched)
                {
                    _repPopupTypeSearched = true;
                    System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                    for (int a = 0; a < assemblies.Length; a++)
                    {
                        _repPopupType = assemblies[a].GetType("UI_RepPopup");
                        if ((object)_repPopupType != null) break;
                    }
                    MelonLogger.Msg("[SuspensionHUD] UI_RepPopup type: "
                        + ((object)_repPopupType != null ? "FOUND" : "NOT FOUND"));
                }

                if ((object)_repPopupType == null)
                {
                    MelonLogger.Warning("[SuspensionHUD] UI_RepPopup type not resolved — trick feed hide unavailable.");
                    return;
                }

                // FindObjectOfType(type) — Unity's internal fast path, stops at first match
                Object obj = Object.FindObjectOfType(_repPopupType);
                if ((object)obj != null)
                {
                    _repPopup = obj as MonoBehaviour;
                    MelonLogger.Msg("[SuspensionHUD] UI_RepPopup found: " + _repPopup.gameObject.name);
                }
                else
                {
                    MelonLogger.Warning("[SuspensionHUD] UI_RepPopup not found in scene.");
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SuspensionHUD] EnsureRepPopup: " + ex.Message);
            }
        }

        /// <summary>Nulls wheel refs and resets hardtail state. Called on scene unload.
        /// Does NOT touch Enabled — HUD persists across scenes.</summary>
        public static void ClearCache()
        {
            _frontWheel = null;
            _rearWheel = null;
            _wheelsSearched = false;
            _suspField = null;
            _fieldCached = false;
            _rearMaxComp = 0f;
            _rearSamples = 0;
            _isHardtail = false;
            _frontCalibMax = 0.01f;
            _rearCalibMax = 0.01f;
            _calibLogCount = 0;
            MelonLogger.Msg("[SuspensionHUD] Cache cleared (Enabled=" + Enabled + ")");
        }

        /// <summary>Full reset — turns HUD off, restores trick feed, clears cache.</summary>
        public static void Reset()
        {
            if (Enabled)
            {
                MelonLogger.Msg("[SuspensionHUD] Reset -> OFF");
                SetTrickFeedVisible(true); // restore trick feed before disabling
            }
            Enabled = false;
            ClearCache();
            // Note: _repPopup and _repPopupSearched intentionally NOT cleared —
            // UI_RepPopup persists for the whole session and is expensive to re-find.
        }

        // ─────────────────────────────────────────────────────────────
        public static void OnGUI()
        {
            if (!Enabled) return;

            EnsureWheels();

            // If neither wheel is available yet, bail silently
            if ((object)_frontWheel == null && (object)_rearWheel == null) return;

            float s = Screen.height / 1080f;
            float barW = BarW * s;
            float barH = BarH * s;
            float barGap = BarGap * s;
            float marginX = MarginX * s;
            float marginY = MarginY * s;
            float labelH = LabelH * s;
            float pctH = PctH * s;
            int labelFs = Mathf.Max(8, Mathf.RoundToInt(9f * s));
            int pctFs = Mathf.Max(7, Mathf.RoundToInt(8f * s));

            float frontRaw = GetCompression(_frontWheel);
            float rearRaw = GetCompression(_rearWheel);

            // ── Auto-calibrate: track peak raw values and normalise ────
            // suspensionPress unit is unknown — could be metres not 0-1.
            // Normalise against the highest value seen so bars always fill correctly.
            if (frontRaw > _frontCalibMax) _frontCalibMax = frontRaw;
            if (rearRaw > _rearCalibMax) _rearCalibMax = rearRaw;

            float frontComp = frontRaw / _frontCalibMax;
            float rearComp = rearRaw / _rearCalibMax;

            // Log first few raw+normalised readings so range can be confirmed in log
            if (_calibLogCount < 5 && (frontRaw > 0f || rearRaw > 0f))
            {
                _calibLogCount++;
                MelonLogger.Msg("[SuspensionHUD] raw F=" + frontRaw.ToString("F4")
                    + " R=" + rearRaw.ToString("F4")
                    + " | norm F=" + frontComp.ToString("F3")
                    + " R=" + rearComp.ToString("F3")
                    + " | calibMax F=" + _frontCalibMax.ToString("F4")
                    + " R=" + _rearCalibMax.ToString("F4"));
            }

            // ── Hardtail detection (sample for HardtailFrames then decide) ──
            if (!_isHardtail && _rearSamples < HardtailFrames)
            {
                if (rearRaw > _rearMaxComp) _rearMaxComp = rearRaw;
                _rearSamples++;
                if (_rearSamples >= HardtailFrames)
                {
                    _isHardtail = _rearMaxComp < HardtailThresh;
                    MelonLogger.Msg("[SuspensionHUD] Hardtail detection: maxRearComp="
                        + _rearMaxComp.ToString("F4")
                        + " isHardtail=" + _isHardtail);
                }
            }

            // ── Draw: bottom-left corner ──────────────────────────────
            float yBase = Screen.height - marginY - barH - labelH - pctH;

            DrawBar(marginX, yBase, barW, barH, labelH, pctH, labelFs, pctFs,
                frontComp, "F", false, s);
            DrawBar(marginX + barW + barGap, yBase, barW, barH, labelH, pctH, labelFs, pctFs,
                rearComp, "R", _isHardtail, s);
        }

        // ─────────────────────────────────────────────────────────────
        private static void DrawBar(float x, float y, float barW, float barH,
            float labelH, float pctH, int labelFs, int pctFs,
            float compression, string letter, bool hardtail, float s)
        {
            compression = Mathf.Clamp01(compression);

            Color bgC = new Color(0.059f, 0.059f, 0.078f, 0.85f);
            Color borderC = hardtail
                ? new Color(0.30f, 0.30f, 0.30f, 0.30f)
                : new Color(0.80f, 1.00f, 0.00f, 0.20f);

            float b = Mathf.Max(1f, s);

            // Background
            DrawRect(x, y, barW, barH, bgC);

            // Border (4 sides, 1px)
            DrawRect(x, y, barW, b, borderC);
            DrawRect(x, y + barH - b, barW, b, borderC);
            DrawRect(x, y, b, barH, borderC);
            DrawRect(x + barW - b, y, b, barH, borderC);

            if (hardtail)
            {
                // Extra dim overlay
                DrawRect(x, y, barW, barH, new Color(0f, 0f, 0f, 0.40f));
            }
            else if (compression > 0f)
            {
                // Fill from bottom up
                float fillH = barH * compression;
                DrawRect(x, y + barH - fillH, barW, fillH, GetBarColor(compression));
            }

            // Tick marks at 25 / 50 / 75 %
            Color tickC = new Color(1f, 1f, 1f, 0.10f);
            float tickW = barW - b * 2f;
            float tickH1 = Mathf.Max(1f, s);
            DrawRect(x + b, y + barH * 0.25f, tickW, tickH1, tickC);
            DrawRect(x + b, y + barH * 0.50f, tickW, tickH1, tickC);
            DrawRect(x + b, y + barH * 0.75f, tickW, tickH1, tickC);

            // Label "F" / "R"
            Color labelCol = hardtail
                ? new Color(0.333f, 0.333f, 0.333f, 1f)
                : new Color(0.80f, 1.00f, 0.00f, 1f);

            if (_lblStyle == null) _lblStyle = new GUIStyle(GUI.skin.label);
            _lblStyle.fontSize = labelFs;
            _lblStyle.fontStyle = FontStyle.Bold;
            _lblStyle.alignment = TextAnchor.MiddleCenter;
            _lblStyle.normal.textColor = labelCol;
            GUI.Label(new Rect(x, y + barH + 1f, barW, labelH), letter, _lblStyle);

            if (_pctStyle == null) _pctStyle = new GUIStyle(GUI.skin.label);
            _pctStyle.fontSize = pctFs;
            _pctStyle.fontStyle = FontStyle.Normal;
            _pctStyle.alignment = TextAnchor.MiddleCenter;
            _pctStyle.normal.textColor = new Color(0.541f, 0.541f, 0.541f, 1f);
            string pctText = hardtail
                ? "\u2014"
                : Mathf.RoundToInt(compression * 100f) + "%";
            GUI.Label(new Rect(x - 2f, y + barH + labelH, barW + 4f, pctH), pctText, _pctStyle);
        }

        // ─────────────────────────────────────────────────────────────
        private static void DrawRect(float rx, float ry, float rw, float rh, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(rx, ry, rw, rh), Tex);
            GUI.color = Color.white;
        }

        private static Color GetBarColor(float compression)
        {
            if (compression < 0.4f) return new Color(0.80f, 1.00f, 0.00f, 1f);   // lime
            if (compression < 0.7f) return new Color(1.00f, 0.60f, 0.00f, 1f);   // orange
            return new Color(1.00f, 0.20f, 0.20f, 1f);   // red
        }

        // ─────────────────────────────────────────────────────────────
        private static void EnsureWheels()
        {
            if (_wheelsSearched) return;       // already ran search this scene

            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) return; // silent — player not spawned yet

            // Mark searched so we don't repeat every frame even if search partially fails
            _wheelsSearched = true;

            try
            {
                MelonLogger.Msg("[SuspensionHUD] Player_Human found — searching for wheels...");

                // ── Approach A: named child transforms ────────────────
                Transform frontT = player.transform.Find("wheel_front");
                Transform rearT = player.transform.Find("wheel_back");

                MelonLogger.Msg("[SuspensionHUD] wheel_front transform: "
                    + ((object)frontT != null ? "FOUND (" + frontT.gameObject.name + ")" : "NOT FOUND"));
                MelonLogger.Msg("[SuspensionHUD] wheel_back transform: "
                    + ((object)rearT != null ? "FOUND (" + rearT.gameObject.name + ")" : "NOT FOUND"));

                if ((object)frontT != null) _frontWheel = frontT.GetComponent<Wheel>();
                if ((object)rearT != null) _rearWheel = rearT.GetComponent<Wheel>();

                MelonLogger.Msg("[SuspensionHUD] Front Wheel component: "
                    + ((object)_frontWheel != null ? "OK" : "NOT FOUND on wheel_front"));
                MelonLogger.Msg("[SuspensionHUD] Rear Wheel component: "
                    + ((object)_rearWheel != null ? "OK" : "NOT FOUND on wheel_back"));

                // ── Approach B: fallback if named approach failed ─────
                if ((object)_frontWheel == null && (object)_rearWheel == null)
                {
                    MelonLogger.Warning("[SuspensionHUD] Named transforms failed — fallback: GetComponentsInChildren<Wheel>()");
                    Wheel[] wheels = player.GetComponentsInChildren<Wheel>();
                    int wCount = (wheels != null) ? wheels.Length : 0;
                    MelonLogger.Msg("[SuspensionHUD] GetComponentsInChildren<Wheel>() found " + wCount + " wheel(s).");

                    if (wCount >= 1) { _frontWheel = wheels[0]; MelonLogger.Msg("[SuspensionHUD] Fallback front: " + wheels[0].gameObject.name); }
                    if (wCount >= 2) { _rearWheel = wheels[1]; MelonLogger.Msg("[SuspensionHUD] Fallback rear: " + wheels[1].gameObject.name); }

                    if (wCount == 0)
                        MelonLogger.Error("[SuspensionHUD] No Wheel components found on Player_Human subtree. HUD will not function.");
                }

                // ── Cache the reflection field ────────────────────────
                EnsureField();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SuspensionHUD] EnsureWheels exception: " + ex.Message);
            }
        }

        private static void EnsureField()
        {
            if (_fieldCached) return;
            _fieldCached = true; // set early — prevents re-scan on failure

            Wheel w = (object)_frontWheel != null ? _frontWheel : _rearWheel;
            if ((object)w == null)
            {
                MelonLogger.Warning("[SuspensionHUD] EnsureField: no wheel to introspect.");
                return;
            }

            try
            {
                // ── Attempt 1: direct backing field ──────────────────
                _suspField = w.GetType().GetField(
                    "<suspensionPress>k__BackingField",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if ((object)_suspField != null)
                {
                    // Verify it returns float and reads a sane value
                    float testVal = -1f;
                    try { testVal = (float)_suspField.GetValue(w); } catch { }
                    MelonLogger.Msg("[SuspensionHUD] <suspensionPress>k__BackingField FOUND."
                        + " FieldType=" + _suspField.FieldType.Name
                        + " CurrentValue=" + testVal.ToString("F4"));
                    return;
                }

                MelonLogger.Warning("[SuspensionHUD] <suspensionPress>k__BackingField NOT found by exact name.");

                // ── Attempt 2: scan NonPublic Instance fields for suspension-related names ──
                FieldInfo[] nonPubFields = w.GetType().GetFields(
                    BindingFlags.NonPublic | BindingFlags.Instance);
                MelonLogger.Msg("[SuspensionHUD] Scanning " + nonPubFields.Length + " NonPublic Instance fields on Wheel...");

                for (int i = 0; i < nonPubFields.Length; i++)
                {
                    string fn = nonPubFields[i].Name;
                    bool nameMatch =
                        fn.IndexOf("suspension", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        fn.IndexOf("susp", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        fn.IndexOf("press", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        fn.IndexOf("compress", System.StringComparison.OrdinalIgnoreCase) >= 0;

                    if (nameMatch)
                    {
                        MelonLogger.Msg("[SuspensionHUD] Candidate: " + fn + " (" + nonPubFields[i].FieldType.Name + ")");
                        if (string.Equals(nonPubFields[i].FieldType.Name, "Single", System.StringComparison.Ordinal))
                        {
                            _suspField = nonPubFields[i];
                            float v = -1f;
                            try { v = (float)_suspField.GetValue(w); } catch { }
                            MelonLogger.Msg("[SuspensionHUD] Using field: " + fn + " = " + v.ToString("F4"));
                            return;
                        }
                    }
                }

                // ── Attempt 3: log ALL float-returning 0-param public methods ──
                MelonLogger.Warning("[SuspensionHUD] No suspension field found by name scan. Listing all float 0-param public methods on Wheel:");
                MethodInfo[] methods = w.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
                int listed = 0;
                for (int i = 0; i < methods.Length; i++)
                {
                    ParameterInfo[] parms = methods[i].GetParameters();
                    if (parms.Length == 0
                        && string.Equals(methods[i].ReturnType.Name, "Single", System.StringComparison.Ordinal))
                    {
                        float val = -999f;
                        try { val = (float)methods[i].Invoke(w, null); } catch { }
                        MelonLogger.Msg("[SuspensionHUD]   method: " + methods[i].Name + "() = " + val.ToString("F4"));
                        listed++;
                    }
                }
                MelonLogger.Msg("[SuspensionHUD] Listed " + listed + " float 0-param public methods.");

                // ── Attempt 4: log ALL NonPublic fields (raw dump) ────
                MelonLogger.Warning("[SuspensionHUD] Dumping ALL NonPublic Instance fields on Wheel for manual inspection:");
                for (int i = 0; i < nonPubFields.Length; i++)
                    MelonLogger.Msg("[SuspensionHUD]   field[" + i + "]: " + nonPubFields[i].Name + " (" + nonPubFields[i].FieldType.Name + ")");

                MelonLogger.Error("[SuspensionHUD] Could not resolve suspension compression field. Bars will show 0.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SuspensionHUD] EnsureField exception: " + ex.Message);
            }
        }

        private static float GetCompression(Wheel w)
        {
            if ((object)w == null) return 0f;
            if ((object)_suspField == null) return 0f;
            try
            {
                // Return raw value — calibration normalisation happens in OnGUI
                float v = (float)_suspField.GetValue(w);
                return v < 0f ? 0f : v; // clamp only negative values
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SuspensionHUD] GetCompression: " + ex.Message);
                return 0f;
            }
        }
    }
}