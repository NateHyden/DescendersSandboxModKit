using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    // ══════════════════════════════════════════════════════════════════════
    // ══════════════════════════════════════════════════════════════════════
    public static class BrakeFade
    {
        public static bool Enabled { get; private set; } = false;

        // ── Temperatures ──────────────────────────────────────────────
        public static float FrontTemp { get; private set; } = 0f;
        public static float RearTemp { get; private set; } = 0f;

        // ── Constants ─────────────────────────────────────────────────
        private const float MaxTemp = 300f;
        private const float FailureTemp = 300f;
        private const float FailureLockSecs = 3f;

        private const float FadePivotTemp = 150f;
        private const float FadePivotMult = 0.80f;
        private const float FadeUpperPower = 1.5f;

        private static float _frontBrakeShare = 0.60f;
        private static float _rearBrakeShare = 0.40f;
        public static float FrontBrakeShare => _frontBrakeShare;
        public static float RearBrakeShare => _rearBrakeShare;

        private static int _balanceLevel = 6;
        public static int BalanceLevel => _balanceLevel;

        public static void SetBalanceLevel(int level)
        {
            _balanceLevel = Mathf.Clamp(level, 1, 11);
            _frontBrakeShare = _balanceLevel * 0.10f;
            _rearBrakeShare = 1f - _frontBrakeShare;
            ModLog.Debug("[BrakeBalance] Level=" + _balanceLevel
                + " Front=" + (_frontBrakeShare * 100f).ToString("F0") + "%"
                + " Rear=" + (_rearBrakeShare * 100f).ToString("F0") + "%");
        }

        public static void IncreaseBalance() { if (_balanceLevel < 11) SetBalanceLevel(_balanceLevel + 1); }
        public static void DecreaseBalance() { if (_balanceLevel > 1) SetBalanceLevel(_balanceLevel - 1); }

        public static string BalanceDisplay =>
            (_frontBrakeShare * 100f).ToString("F0") + "F / " + (_rearBrakeShare * 100f).ToString("F0") + "R";

        private const float FrontHeatRate = 0.65f;
        private const float RearHeatRate = 0.52f;

        private const float FrontBaseCool = 3.0f;
        private const float RearBaseCool = 3.4f;
        private const float AirflowFactor = 0.0167f;

        private static bool _frontFailed = false;
        private static bool _rearFailed = false;
        private static float _frontFailTime = -999f;
        private static float _rearFailTime = -999f;

        public static float FadeMultiplier
        {
            get
            {
                float frontMult = _frontFailed ? 0f : ComputeFade(FrontTemp);
                float rearMult = _rearFailed ? 0f : ComputeFade(RearTemp);
                return frontMult * FrontBrakeShare + rearMult * RearBrakeShare;
            }
        }

        public static bool IsInFailure => _frontFailed && _rearFailed;

        public static bool FrontInLock => _frontFailed && (Time.time - _frontFailTime < FailureLockSecs);
        public static bool RearInLock => _rearFailed && (Time.time - _rearFailTime < FailureLockSecs);

        private static float ComputeFade(float temp)
        {
            if (temp <= 0f) return 1.0f;
            if (temp >= FailureTemp) return 0.0f;

            if (temp <= FadePivotTemp)
            {
                float t = temp / FadePivotTemp;
                return Mathf.Lerp(1.0f, FadePivotMult, t);
            }
            else
            {
                float t = (temp - FadePivotTemp) / (FailureTemp - FadePivotTemp);
                return FadePivotMult * (1.0f - Mathf.Pow(t, FadeUpperPower));
            }
        }

        private static Rigidbody _rigidbody = null;
        private static Wheel _frontWheel = null;
        private static Wheel _rearWheel = null;
        private static System.Reflection.FieldInfo _suspField = null;
        private static bool _searched = false;

        // ── Public API ────────────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            if (!Enabled) { FrontTemp = 0f; RearTemp = 0f; }
            ModLog.Feedback("[BrakeFade] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void ClearCache()
        {
            _rigidbody = null;
            _frontWheel = null;
            _rearWheel = null;
            _suspField = null;
            _searched = false;
            _groundCheckLogCount = 0;
            FrontTemp = 0f;
            RearTemp = 0f;
            _frontFailed = false;
            _rearFailed = false;
            _frontFailTime = -999f;
            _rearFailTime = -999f;
            BrakeFade_Patch.ClearCache();
            ModLog.Debug("[BrakeFade] Cache cleared.");
        }

        public static void Reset()
        {
            if (Enabled) ModLog.Feedback("[BrakeFade] Reset -> OFF");
            Enabled = false;
            _balanceLevel = 6;
            _frontBrakeShare = 0.60f;
            _rearBrakeShare = 0.40f;
            ClearCache();
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo fixedUpdate = typeof(VehicleController).GetMethod(
                    "FixedUpdate",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)fixedUpdate == null)
                { ModLog.Warn("[BrakeFade] VehicleController.FixedUpdate not found."); return; }

                MethodInfo postfix = typeof(BrakeFade_Patch).GetMethod(
                    "Postfix", BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(postfix));
                ModLog.Debug("[BrakeFade] Patched VehicleController.FixedUpdate.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[BrakeFade] ApplyPatch: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "BrakeFade"); }
        }

        public static void AddHeat(float brakeInput, float speedKmh)
        {
            if (!Enabled) return;
            try
            {
                float dt = Time.fixedDeltaTime;
                float now = Time.time;

                bool grounded = IsGrounded();
                float effectiveBrake = grounded ? brakeInput : 0f;

                float coolFactor = 1f + speedKmh * AirflowFactor;

                // ── Front disc ───────────────────────────────────────
                if (_frontFailed)
                {
                    if ((now - _frontFailTime) >= FailureLockSecs)
                        _frontFailed = false;
                }
                if (!_frontFailed)
                {
                    float frontNet = effectiveBrake * speedKmh * FrontHeatRate - FrontBaseCool * coolFactor;
                    FrontTemp = Mathf.Clamp(FrontTemp + frontNet * dt, 0f, MaxTemp);
                    if (FrontTemp >= FailureTemp) { _frontFailed = true; _frontFailTime = now; }
                }

                // ── Rear disc ────────────────────────────────────────
                if (_rearFailed)
                {
                    if ((now - _rearFailTime) >= FailureLockSecs)
                        _rearFailed = false;
                }
                if (!_rearFailed)
                {
                    float rearNet = effectiveBrake * speedKmh * RearHeatRate - RearBaseCool * coolFactor;
                    RearTemp = Mathf.Clamp(RearTemp + rearNet * dt, 0f, MaxTemp);
                    if (RearTemp >= FailureTemp) { _rearFailed = true; _rearFailTime = now; }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[BrakeFade] AddHeat: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "BrakeFade");
            }
        }

        public static float GetSpeedKmh()
        {
            EnsureRigidbody();
            if (!UnityNull.Alive(_rigidbody)) return 0f;
            return _rigidbody.velocity.magnitude * 3.6f;
        }

        private static int _groundCheckLogCount = 0;
        public static bool IsGrounded()
        {
            EnsureRigidbody();
            if ((object)_suspField == null) return true;

            try
            {
                float frontComp = UnityNull.Alive(_frontWheel)
                    ? Mathf.Clamp01((float)_suspField.GetValue(_frontWheel)) : 0f;
                float rearComp = UnityNull.Alive(_rearWheel)
                    ? Mathf.Clamp01((float)_suspField.GetValue(_rearWheel)) : 0f;

                bool grounded = frontComp > 0.01f || rearComp > 0.01f;

                if (_groundCheckLogCount < 4)
                {
                    _groundCheckLogCount++;
                    ModLog.Debug("[BrakeFade] IsGrounded: front=" + frontComp.ToString("F3")
                        + " rear=" + rearComp.ToString("F3") + " grounded=" + grounded);
                }
                return grounded;
            }
            catch { return true; }
        }

        private static void EnsureRigidbody()
        {
            if (_searched)
            {
                bool dead = ((object)_rigidbody != null && !UnityNull.Alive(_rigidbody))
                    || ((object)_frontWheel != null && !UnityNull.Alive(_frontWheel))
                    || ((object)_rearWheel != null && !UnityNull.Alive(_rearWheel));
                if (dead)
                {
                    _rigidbody = null;
                    _frontWheel = null;
                    _rearWheel = null;
                    _searched = false;
                }
            }
            if (_searched) return;
            _searched = true;
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if (!UnityNull.Alive(player)) { _searched = false; return; }

                _rigidbody = player.GetComponent<Rigidbody>();
                if (!UnityNull.Alive(_rigidbody))
                    ModLog.Warn("[BrakeFade] Rigidbody not found on Player_Human.");
                else
                    ModLog.Debug("[BrakeFade] Rigidbody cached OK.");

                Transform ft = player.transform.Find("wheel_front");
                Transform rt = player.transform.Find("wheel_back");
                if (UnityNull.Alive(ft)) _frontWheel = ft.GetComponent<Wheel>();
                if (UnityNull.Alive(rt)) _rearWheel = rt.GetComponent<Wheel>();

                Wheel w = UnityNull.Alive(_frontWheel) ? _frontWheel : _rearWheel;
                if (UnityNull.Alive(w))
                {
                    _suspField = w.GetType().GetField(
                        "<suspensionPress>k__BackingField",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                }

                ModLog.Debug("[BrakeFade] Wheels: front=" + UnityNull.Alive(_frontWheel)
                    + " rear=" + UnityNull.Alive(_rearWheel)
                    + " suspField=" + ((object)_suspField != null));
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[BrakeFade] EnsureRigidbody: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "BrakeFade");
            }
        }

        // ── OnGUI HUD ─────────────────────────────────────────────────

        private static Texture2D _tex = null;
        private static GUIStyle _hdrStyle = null;
        private static GUIStyle _lblStyle = null;
        private static GUIStyle _valStyle = null;
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

        private const float MarginRight = 30f;
        private const float MarginTop = 30f;
        private const float PanelW = 110f;
        private const float PanelH = 76f;
        private const float InnerPad = 8f;

        public static void OnGUI()
        {
            if (!Enabled) return;

            float s = Screen.height / 1080f;
            float pw = PanelW * s;
            float ph = PanelH * s;
            float pad = InnerPad * s;
            float x = Screen.width - pw - MarginRight * s;
            float y = MarginTop * s;

            int hdrFs = Mathf.Max(8, Mathf.RoundToInt(9f * s));
            int tmpFs = Mathf.Max(10, Mathf.RoundToInt(13f * s));
            int lblFs = Mathf.Max(9, Mathf.RoundToInt(11f * s));

            // ── Background ───────────────────────────────────────────
            DrawRect(x, y, pw, ph, new Color(0.059f, 0.059f, 0.078f, 0.85f));

            // ── Border ───────────────────────────────────────────────
            float b = Mathf.Max(1f, s);
            Color borderC = new Color(0.80f, 1.00f, 0.00f, 0.20f);
            DrawRect(x, y, pw, b, borderC);
            DrawRect(x, y + ph - b, pw, b, borderC);
            DrawRect(x, y, b, ph, borderC);
            DrawRect(x + pw - b, y, b, ph, borderC);

            // ── Header "BRAKES" ──────────────────────────────────────
            float hdrH = ph * 0.30f;
            float rowH = (ph - hdrH) * 0.5f;

            if (_hdrStyle == null) _hdrStyle = new GUIStyle(GUI.skin.label);
            _hdrStyle.fontSize = hdrFs;
            _hdrStyle.fontStyle = FontStyle.Bold;
            _hdrStyle.alignment = TextAnchor.MiddleCenter;
            _hdrStyle.normal.textColor = new Color(0.80f, 1.00f, 0.00f, 0.80f);
            GUI.Label(new Rect(x, y, pw, hdrH), "BRAKES", _hdrStyle);

            // ── Divider under header ──────────────────────────────────
            DrawRect(x + pad, y + hdrH - b, pw - pad * 2f, b,
                new Color(0.80f, 1.00f, 0.00f, 0.10f));

            // ── Front temp ───────────────────────────────────────────
            DrawTempRow(x, y + hdrH, pw, rowH, lblFs, tmpFs, "FRONT", FrontTemp, pad);

            // ── Rear temp ────────────────────────────────────────────
            DrawTempRow(x, y + hdrH + rowH, pw, rowH, lblFs, tmpFs, "REAR", RearTemp, pad);
        }

        private static void DrawTempRow(float x, float y, float pw, float rowH,
            int lblFs, int valFs, string label, float temp, float pad)
        {
            bool discFailed = label == "FRONT" ? _frontFailed : _rearFailed;
            bool discInLock = label == "FRONT" ? FrontInLock : RearInLock;

            Color tempCol;
            string valText;

            if (discFailed && discInLock)
            {
                bool flashOn = (Time.time % 0.5f) < 0.25f;
                tempCol = flashOn
                    ? new Color(1f, 1f, 1f, 1f)
                    : new Color(1f, 0.13f, 0.13f, 1f);
                valText = "FAILED";
            }
            else if (discFailed)
            {
                tempCol = new Color(1f, 0.13f, 0.13f, 1f);
                valText = Mathf.RoundToInt(temp) + "°C";
            }
            else
            {
                tempCol = GetTempColor(temp);
                valText = Mathf.RoundToInt(temp) + "°C";
            }

            if (_lblStyle == null) _lblStyle = new GUIStyle(GUI.skin.label);
            _lblStyle.fontSize = lblFs;
            _lblStyle.fontStyle = FontStyle.Bold;
            _lblStyle.alignment = TextAnchor.MiddleLeft;
            _lblStyle.normal.textColor = new Color(0.55f, 0.55f, 0.55f, 1f);

            if (_valStyle == null) _valStyle = new GUIStyle(GUI.skin.label);
            _valStyle.fontSize = valFs;
            _valStyle.fontStyle = FontStyle.Bold;
            _valStyle.alignment = TextAnchor.MiddleRight;
            _valStyle.normal.textColor = tempCol;

            GUI.Label(new Rect(x + pad, y, pw * 0.45f, rowH), label, _lblStyle);
            GUI.Label(new Rect(x + pad, y, pw - pad * 2f, rowH), valText, _valStyle);
        }

        private static Color GetTempColor(float temp)
        {
            if (temp <= 0f) return new Color(0.67f, 0.67f, 0.67f, 1f);
            if (temp <= 80f)
            {
                float t = temp / 80f;
                return Color.Lerp(new Color(0.67f, 0.67f, 0.67f, 1f),
                                  new Color(1.00f, 0.85f, 0.00f, 1f), t);
            }
            if (temp <= 150f)
            {
                float t = (temp - 80f) / 70f;
                return Color.Lerp(new Color(1.00f, 0.85f, 0.00f, 1f),
                                  new Color(1.00f, 0.40f, 0.00f, 1f), t);
            }
            if (temp <= 250f)
            {
                float t = (temp - 150f) / 100f;
                return Color.Lerp(new Color(1.00f, 0.40f, 0.00f, 1f),
                                  new Color(1.00f, 0.13f, 0.13f, 1f), t);
            }
            return new Color(1.00f, 0.13f, 0.13f, 1f);
        }

        private static void DrawRect(float rx, float ry, float rw, float rh, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(rx, ry, rw, rh), Tex);
            GUI.color = Color.white;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // ══════════════════════════════════════════════════════════════════════
    public static class BrakeFade_Patch
    {
        private static FieldInfo _vehicleField = null;


        public static void Postfix(VehicleController __instance)
        {
            if (!BrakeFade.Enabled) return;
            if (!UnityNull.Alive(__instance)) return;

            try
            {
                if ((object)_vehicleField == null)
                {
                    FieldInfo[] fields = __instance.GetType().GetFields(
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (string.Equals(fields[i].FieldType.Name, "Vehicle",
                            System.StringComparison.Ordinal))
                        { _vehicleField = fields[i]; break; }
                    }
                    if ((object)_vehicleField == null)
                    {
                        ModLog.Warn("[BrakeFade] Vehicle field not found on VehicleController.");
                        return;
                    }
                }

                Vehicle vehicle = _vehicleField.GetValue(__instance) as Vehicle;
                if (!UnityNull.Alive(vehicle)) return;

                if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                float brakeInput = Mathf.Clamp01(vehicle.NYsPlot);
                float speedKmh = BrakeFade.GetSpeedKmh();
                BrakeFade.AddHeat(brakeInput, speedKmh);

                float mult = BrakeFade.FadeMultiplier;
                if (mult < 1.0f)
                    vehicle.NYsPlot = vehicle.NYsPlot * mult;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[BrakeFade] Patch Postfix: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "BrakeFade");
            }
        }

        public static void ClearCache()
        {
            _vehicleField = null;

        }
    }
}

