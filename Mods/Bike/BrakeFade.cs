using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // ══════════════════════════════════════════════════════════════════════
    //  BrakeFade — simulates disc brake heat / fade
    //
    //  Heat model (per FixedUpdate):
    //    Heat added   = brakeInput × speedKmh × heatRate
    //    Heat removed = baseCoolRate × (1 + speedKmh × airflowFactor)
    //    Front disc heats at 55% / rear at 45% → always diverge
    //    Front and rear have slightly different cooldown multipliers
    //
    //  Fade effect applied by multiplying vehicle.NYsPlot:
    //    0–80°C   → no effect (1.0×)
    //    80–200°C → progressive fade down to 0.45×
    //    200–300°C→ heavy fade down to 0.20×
    //
    //  OnGUI HUD renders two colour-coded temperature readings
    //  bottom-left, to the right of the SuspensionHUD bars.
    // ══════════════════════════════════════════════════════════════════════
    public static class BrakeFade
    {
        public static bool Enabled { get; private set; } = false;

        // ── Temperatures ──────────────────────────────────────────────
        public static float FrontTemp { get; private set; } = 0f;
        public static float RearTemp { get; private set; } = 0f;

        // ── Constants ─────────────────────────────────────────────────
        private const float MaxTemp = 300f;
        private const float FailureTemp = 300f;   // complete failure here
        private const float FailureLockSecs = 3f;     // seconds showing FAILED, then clears immediately

        // Two-segment fade:
        //   0–150°C: gentle ramp 0%→20% reduction (present but not alarming)
        //   150–300°C: aggressive power curve 20%→100% (rapidly worsens toward failure)
        private const float FadePivotTemp = 150f;   // segment boundary
        private const float FadePivotMult = 0.80f;  // multiplier at pivot (20% reduction)
        private const float FadeUpperPower = 1.5f;   // curve steepness above pivot

        // Front disc contributes 60% of braking, rear 40% — defaults, adjustable via BrakeBalance
        // 0% front = all rear, 100% front = all front, default 60/40
        private static float _frontBrakeShare = 0.60f;
        private static float _rearBrakeShare = 0.40f;
        public static float FrontBrakeShare => _frontBrakeShare;
        public static float RearBrakeShare => _rearBrakeShare;

        // BrakeBalance: level 1–11, level 6 = default 60/40
        // Level 1  = 10/90 (all rear), Level 6 = 60/40 (default), Level 11 = 100/0 (all front)
        private static int _balanceLevel = 6;
        public static int BalanceLevel => _balanceLevel;

        public static void SetBalanceLevel(int level)
        {
            _balanceLevel = Mathf.Clamp(level, 1, 11);
            // Level maps: 1=10%, 2=20% ... 6=60% ... 11=100% front share
            _frontBrakeShare = _balanceLevel * 0.10f;
            _rearBrakeShare = 1f - _frontBrakeShare;
            MelonLogger.Msg("[BrakeBalance] Level=" + _balanceLevel
                + " Front=" + (_frontBrakeShare * 100f).ToString("F0") + "%"
                + " Rear=" + (_rearBrakeShare * 100f).ToString("F0") + "%");
        }

        public static void IncreaseBalance() { if (_balanceLevel < 11) SetBalanceLevel(_balanceLevel + 1); }
        public static void DecreaseBalance() { if (_balanceLevel > 1) SetBalanceLevel(_balanceLevel - 1); }

        public static string BalanceDisplay =>
            (_frontBrakeShare * 100f).ToString("F0") + "F / " + (_rearBrakeShare * 100f).ToString("F0") + "R";

        // Heat: ~9 seconds of hard braking at 60km/h to reach failure
        private const float FrontHeatRate = 0.65f;
        private const float RearHeatRate = 0.52f;

        // Cool: 3°C/s stationary, ~6°C/s at 60km/h, ~8°C/s at 100km/h
        private const float FrontBaseCool = 3.0f;
        private const float RearBaseCool = 3.4f;
        private const float AirflowFactor = 0.0167f;

        // Failure state (latched per disc — independent front/rear)
        private static bool _frontFailed = false;
        private static bool _rearFailed = false;
        private static float _frontFailTime = -999f; // Time.time when front latched
        private static float _rearFailTime = -999f; // Time.time when rear latched

        // ── Fade multiplier (exported for the Harmony patch) ──────────
        // Front and rear discs are independent.
        // Combined = FrontMult×FrontBrakeShare + RearMult×RearBrakeShare
        // So front failure alone leaves 40% braking from the rear disc.
        // Both failed = 0.
        public static float FadeMultiplier
        {
            get
            {
                float frontMult = _frontFailed ? 0f : ComputeFade(FrontTemp);
                float rearMult = _rearFailed ? 0f : ComputeFade(RearTemp);
                return frontMult * FrontBrakeShare + rearMult * RearBrakeShare;
            }
        }

        // True only when both discs are in the lock phase (fully failed)
        public static bool IsInFailure => _frontFailed && _rearFailed;

        // Lock phase = within the 3s FAILED window (brakes completely cut)
        public static bool FrontInLock => _frontFailed && (Time.time - _frontFailTime < FailureLockSecs);
        public static bool RearInLock => _rearFailed && (Time.time - _rearFailTime < FailureLockSecs);

        // Two-segment fade curve:
        //  0–150°C: linear 0%→20% reduction (present but manageable)
        //  150–300°C: power curve 20%→100% (worsens rapidly — 250°C is ~63% reduction)
        private static float ComputeFade(float temp)
        {
            if (temp <= 0f) return 1.0f;
            if (temp >= FailureTemp) return 0.0f;

            if (temp <= FadePivotTemp)
            {
                // Gentle linear ramp: full → FadePivotMult
                float t = temp / FadePivotTemp;
                return Mathf.Lerp(1.0f, FadePivotMult, t);
            }
            else
            {
                // Aggressive power curve: FadePivotMult → 0
                float t = (temp - FadePivotTemp) / (FailureTemp - FadePivotTemp);
                return FadePivotMult * (1.0f - Mathf.Pow(t, FadeUpperPower));
            }
        }

        // ── Vehicle cache (speed + ground detection via wheel suspension) ──
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
            MelonLogger.Msg("[BrakeFade] -> " + (Enabled ? "ON" : "OFF"));
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
            MelonLogger.Msg("[BrakeFade] Cache cleared.");
        }

        public static void Reset()
        {
            if (Enabled) MelonLogger.Msg("[BrakeFade] Reset -> OFF");
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
                { MelonLogger.Warning("[BrakeFade] VehicleController.FixedUpdate not found."); return; }

                MethodInfo postfix = typeof(BrakeFade_Patch).GetMethod(
                    "Postfix", BindingFlags.Public | BindingFlags.Static);

                harmony.Patch(fixedUpdate, postfix: new HarmonyMethod(postfix));
                MelonLogger.Msg("[BrakeFade] Patched VehicleController.FixedUpdate.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[BrakeFade] ApplyPatch: " + ex.Message); }
        }

        // ── AddHeat — called from BrakeFade_Patch with real brake value ──
        // brakeInput: vehicle.NYsPlot read directly (compiled access)
        // speedKmh:   rigidbody velocity converted to km/h
        // speedKmh:   rigidbody velocity — also used by IsGrounded raycast
        public static void AddHeat(float brakeInput, float speedKmh)
        {
            if (!Enabled) return;
            try
            {
                float dt = Time.fixedDeltaTime;
                float now = Time.time;

                // Only generate heat when grounded — Physics.Raycast ground check
                bool grounded = IsGrounded();
                float effectiveBrake = grounded ? brakeInput : 0f;

                float coolFactor = 1f + speedKmh * AirflowFactor;

                // ── Front disc ───────────────────────────────────────
                // Lock: 3s FAILED window — brakes cut, temp frozen, no heating/cooling
                // After lock: failed flag clears immediately — normal physics resumes
                if (_frontFailed)
                {
                    if ((now - _frontFailTime) >= FailureLockSecs)
                        _frontFailed = false; // clear — fall through to normal logic next frame
                    // else: still in lock, temp stays frozen
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
            }
        }

        // ── GetSpeed — reads rigidbody velocity for the patch ─────────
        public static float GetSpeedKmh()
        {
            EnsureRigidbody();
            if ((object)_rigidbody == null) return 0f;
            return _rigidbody.velocity.magnitude * 3.6f;
        }

        // ── IsGrounded — suspension compression check ────────────────
        // If either wheel has compression > 0, at least one wheel is touching ground.
        // Both at 0 = fully extended = airborne. Same method SuspensionHUD uses.
        private static int _groundCheckLogCount = 0;
        public static bool IsGrounded()
        {
            EnsureRigidbody();
            if ((object)_suspField == null) return true; // safe default if field not found

            try
            {
                float frontComp = (object)_frontWheel != null
                    ? Mathf.Clamp01((float)_suspField.GetValue(_frontWheel)) : 0f;
                float rearComp = (object)_rearWheel != null
                    ? Mathf.Clamp01((float)_suspField.GetValue(_rearWheel)) : 0f;

                bool grounded = frontComp > 0.01f || rearComp > 0.01f;

                if (_groundCheckLogCount < 4)
                {
                    _groundCheckLogCount++;
                    MelonLogger.Msg("[BrakeFade] IsGrounded: front=" + frontComp.ToString("F3")
                        + " rear=" + rearComp.ToString("F3") + " grounded=" + grounded);
                }
                return grounded;
            }
            catch { return true; }
        }

        // ── EnsureRigidbody — caches rigidbody + wheels for speed and ground detection ──
        private static void EnsureRigidbody()
        {
            if (_searched) return;
            _searched = true;
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) { _searched = false; return; }

                _rigidbody = player.GetComponent<Rigidbody>();
                if ((object)_rigidbody == null)
                    MelonLogger.Warning("[BrakeFade] Rigidbody not found on Player_Human.");
                else
                    MelonLogger.Msg("[BrakeFade] Rigidbody cached OK.");

                // Cache wheels for suspension-based ground detection
                Transform ft = player.transform.Find("wheel_front");
                Transform rt = player.transform.Find("wheel_back");
                if ((object)ft != null) _frontWheel = ft.GetComponent<Wheel>();
                if ((object)rt != null) _rearWheel = rt.GetComponent<Wheel>();

                Wheel w = (object)_frontWheel != null ? _frontWheel : _rearWheel;
                if ((object)w != null)
                {
                    _suspField = w.GetType().GetField(
                        "<suspensionPress>k__BackingField",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                }

                MelonLogger.Msg("[BrakeFade] Wheels: front=" + ((object)_frontWheel != null)
                    + " rear=" + ((object)_rearWheel != null)
                    + " suspField=" + ((object)_suspField != null));
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[BrakeFade] EnsureRigidbody: " + ex.Message);
            }
        }

        // ── OnGUI HUD ─────────────────────────────────────────────────
        // Top-right corner, resolution-scaled.

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
                // Lock phase: flash FAILED white/red at 2Hz
                bool flashOn = (Time.time % 0.5f) < 0.25f;
                tempCol = flashOn
                    ? new Color(1f, 1f, 1f, 1f)
                    : new Color(1f, 0.13f, 0.13f, 1f);
                valText = "FAILED";
            }
            else if (discFailed)
            {
                // Cooling phase: show actual temp in red so player sees it dropping
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
            // Cold → grey, warm → yellow, hot → orange, critical → red
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
            return new Color(1.00f, 0.13f, 0.13f, 1f); // full red
        }

        private static void DrawRect(float rx, float ry, float rw, float rh, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(rx, ry, rw, rh), Tex);
            GUI.color = Color.white;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Harmony patch — multiplies brake input by fade factor post-FixedUpdate
    //  Runs AFTER CutBrakes_Patch (harmony applies postfixes in patch order)
    // ══════════════════════════════════════════════════════════════════════
    public static class BrakeFade_Patch
    {
        private static FieldInfo _vehicleField = null;


        public static void Postfix(VehicleController __instance)
        {
            if (!BrakeFade.Enabled) return;
            if ((object)__instance == null) return;

            try
            {
                // Get Vehicle from VehicleController (same approach as CutBrakes)
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
                        MelonLogger.Warning("[BrakeFade] Vehicle field not found on VehicleController.");
                        return;
                    }
                }

                Vehicle vehicle = _vehicleField.GetValue(__instance) as Vehicle;
                if ((object)vehicle == null) return;

                if (!string.Equals(vehicle.gameObject.name, "Player_Human",
                    System.StringComparison.Ordinal)) return;

                // Read brake input via DIRECT compiled access (guaranteed correct)
                // Feed into heat model BEFORE applying fade
                float brakeInput = Mathf.Clamp01(vehicle.NYsPlot);
                float speedKmh = BrakeFade.GetSpeedKmh();
                BrakeFade.AddHeat(brakeInput, speedKmh);

                // Apply fade — FadeMultiplier handles all cases:
                // partial front/rear failure, combined failure, and smooth fade.
                // Returns 0 when both discs fail, 0.4 when only front fails, etc.
                float mult = BrakeFade.FadeMultiplier;
                if (mult < 1.0f)
                    vehicle.NYsPlot = vehicle.NYsPlot * mult;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[BrakeFade] Patch Postfix: " + ex.Message);
            }
        }

        public static void ClearCache()
        {
            _vehicleField = null;

        }
    }
}