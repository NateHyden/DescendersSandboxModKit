using MelonLoader;
using UnityEngine;
using System.Reflection;
using HarmonyLib;

namespace DescendersModMenu.Mods
{
    public static class SkyColours
    {
        public static readonly string[] PresetNames = {
            "Normal", "Blood Red", "Alien Green", "Synthwave", "Midnight", "Toxic"
        };

        private static readonly Color[] SunSkyColors = {
            new Color(0.99f, 0.87f, 0.76f, 1f),
            new Color(0.90f, 0.05f, 0.05f, 1f),
            new Color(0.05f, 0.80f, 0.10f, 1f),
            new Color(0.70f, 0.05f, 0.95f, 1f),
            new Color(0.02f, 0.02f, 0.12f, 1f),
            new Color(0.30f, 0.90f, 0.05f, 1f),
        };

        private static readonly Color[] MoonSkyColors = {
            new Color(0.10f, 0.16f, 0.25f, 1f),
            new Color(0.40f, 0.01f, 0.01f, 1f),
            new Color(0.01f, 0.30f, 0.05f, 1f),
            new Color(0.25f, 0.01f, 0.40f, 1f),
            new Color(0.01f, 0.01f, 0.08f, 1f),
            new Color(0.10f, 0.35f, 0.01f, 1f),
        };

        private static readonly Color[] FogColors = {
            new Color(0.87f, 0.59f, 0.60f, 1f),
            new Color(0.60f, 0.02f, 0.02f, 1f),
            new Color(0.02f, 0.55f, 0.05f, 1f),
            new Color(0.45f, 0.02f, 0.65f, 1f),
            new Color(0.01f, 0.01f, 0.10f, 1f),
            new Color(0.20f, 0.65f, 0.02f, 1f),
        };

        private static readonly Color[] AmbientColors = {
            new Color(0.99f, 0.90f, 0.81f, 1f),
            new Color(0.90f, 0.55f, 0.55f, 1f),
            new Color(0.55f, 0.90f, 0.55f, 1f),
            new Color(0.75f, 0.55f, 0.90f, 1f),
            new Color(0.35f, 0.35f, 0.55f, 1f),
            new Color(0.60f, 0.90f, 0.45f, 1f),
        };

        private static readonly float[] TODHours = { 12f, 19.5f, 22f, 21f, 0f, 14f };

        public static int CurrentPreset { get; private set; } = 0;
        public static bool StormEnabled { get; private set; } = false;

        // Rain intensity � scales particle emission rate on active storm EffectInstances
        // Level 1 = 0.5x (light), Level 5 = 1x (default), Level 10 = 3x (heavy)
        public static int RainIntensityLevel { get; private set; } = 5;
        private static readonly float[] RainMultipliers = { 0.5f, 0.65f, 0.8f, 0.9f, 1f, 1.3f, 1.7f, 2.0f, 2.5f, 3.0f };

        public static float GetRainMultiplier()
        {
            return RainMultipliers[System.Math.Max(0, System.Math.Min(RainMultipliers.Length - 1, RainIntensityLevel - 1))];
        }

        // Cached reflection for EffectList.LateUpdate postfix (hot path — called every frame per EffectList)
        private static FieldInfo _elEnvFlagsField = null;
        private static FieldInfo _elLtpField = null;
        private static FieldInfo _eiPsField = null;
        private static FieldInfo _eiBaseEffField = null;
        private static bool _elFieldsResolved = false;

        private static void ResolveEffectListFields()
        {
            if (_elFieldsResolved) return;
            _elFieldsResolved = true;
            _elEnvFlagsField = typeof(EffectList).GetField(
                "\u007Ejl\u0082liu", BindingFlags.NonPublic | BindingFlags.Instance);
            _elLtpField = typeof(EffectList).GetField(
                "ltpCVSt", BindingFlags.NonPublic | BindingFlags.Instance);
            _eiPsField = typeof(EffectInstance).GetField(
                "\u007FJm\u007DD\u0060\u007B", BindingFlags.Public | BindingFlags.Instance);
            _eiBaseEffField = typeof(EffectInstance).GetField(
                "\u0083W\u0083n\u0060Xw", BindingFlags.Public | BindingFlags.Instance);
            if ((object)_elEnvFlagsField == null) MelonLogger.Warning("[SkyColours] EL env flags field not found.");
            if ((object)_elLtpField == null) MelonLogger.Warning("[SkyColours] EL ltp field not found.");
            if ((object)_eiPsField == null) MelonLogger.Warning("[SkyColours] EI PS field not found.");
            if ((object)_eiBaseEffField == null) MelonLogger.Warning("[SkyColours] EI BaseEffect field not found.");
        }

        /// <summary>
        /// Called from EffectList.LateUpdate Postfix — runs AFTER the game's own particle
        /// enable/disable logic, so our storm override always wins the toggle war.
        /// </summary>
        public static void EnforceStormOnEffectList(EffectList instance)
        {
            if (!StormEnabled) return;
            try
            {
                ResolveEffectListFields();
                if ((object)_elEnvFlagsField != null)
                {
                    bool[] envFlags = _elEnvFlagsField.GetValue(instance) as bool[];
                    if ((object)envFlags != null && envFlags.Length > 7)
                    {
                        envFlags[7] = true;
                        envFlags[4] = false;
                    }
                }
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════════════════
        //  CUSTOM RAIN — the game's EffectList system doesn't contain
        //  falling rain. We create our own ParticleSystem on the camera.
        // ═══════════════════════════════════════════════════════════════
        private static GameObject _rainObj = null;
        private static ParticleSystem _rainPS = null;

        public static void CreateRain()
        {
            if ((object)_rainObj != null) return;

            Camera cam = Camera.main;
            if ((object)cam == null) return;

            _rainObj = new GameObject("ModRain");
            _rainObj.transform.SetParent(cam.transform, false);
            // Centered above and slightly forward — large box ensures rain is visible in all directions
            _rainObj.transform.localPosition = new Vector3(0f, 12f, 8f);
            _rainObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            _rainPS = _rainObj.AddComponent<ParticleSystem>();
            _rainPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Main module
            var main = _rainPS.main;
            main.loop = true;
            main.startLifetime = 1.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(20f, 30f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new Color(0.7f, 0.75f, 0.85f, 0.5f);
            main.maxParticles = 20000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.3f;
            main.playOnAwake = false;

            // Emission
            var emission = _rainPS.emission;
            emission.enabled = true;
            emission.rateOverTime = GetRainEmissionRate();

            // Shape — box above camera
            var shape = _rainPS.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(150f, 1f, 150f);

            // Renderer — stretched billboard for rain streaks
            var renderer = _rainObj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 5f;
            renderer.velocityScale = 0.05f;
            renderer.material = new Material(Shader.Find("Particles/Alpha Blended"));
            // Create a small white texture for rain drops
            Texture2D tex = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int p = 0; p < 16; p++) pixels[p] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            renderer.material.mainTexture = tex;
            renderer.material.SetColor("_TintColor", new Color(0.6f, 0.65f, 0.8f, 0.3f));

            _rainPS.Play();
            MelonLogger.Msg("[SkyColours] Custom rain created. Rate=" + GetRainEmissionRate());
        }

        public static void DestroyRain()
        {
            if ((object)_rainPS != null)
            {
                _rainPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _rainPS = null;
            }
            if ((object)_rainObj != null)
            {
                Object.DestroyImmediate(_rainObj);
                _rainObj = null;
            }
        }

        public static void UpdateRainIntensity()
        {
            if ((object)_rainPS == null) return;
            var emission = _rainPS.emission;
            emission.rateOverTime = GetRainEmissionRate();
        }

        private static float GetRainEmissionRate()
        {
            // Level 1=200, 5=1000, 10=3000
            return RainMultipliers[System.Math.Max(0, System.Math.Min(RainMultipliers.Length - 1, RainIntensityLevel - 1))] * 2000f;
        }

        public static void SetRainIntensityLevel(int v)
        {
            RainIntensityLevel = System.Math.Max(1, System.Math.Min(10, v));
            if (StormEnabled) { ApplyRainIntensity(); UpdateRainIntensity(); }
        }

        public static void RainIntensityIncrease()
        {
            if (RainIntensityLevel < 10) RainIntensityLevel++;
            if (StormEnabled) { ApplyRainIntensity(); UpdateRainIntensity(); }
        }

        public static void RainIntensityDecrease()
        {
            if (RainIntensityLevel > 1) RainIntensityLevel--;
            if (StormEnabled) { ApplyRainIntensity(); UpdateRainIntensity(); }
        }

        private static ParticleSystem[] _cachedRainPS = null;
        private static float[] _defaultEmissionRates = null;

        private static void ApplyRainIntensity()
        {
            try
            {
                float mult = RainMultipliers[RainIntensityLevel - 1];

                // Collect particle systems from EffectInstances only
                EffectInstance[] instances = Object.FindObjectsOfType<EffectInstance>();
                if ((object)instances == null || instances.Length == 0)
                { MelonLogger.Warning("[SkyColours] No EffectInstances found."); return; }

                var psList = new System.Collections.Generic.List<ParticleSystem>();
                System.Reflection.FieldInfo psField = typeof(EffectInstance).GetField(
                    "\u007FJm\u007DD\u0060\u007B",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if ((object)psField == null) { MelonLogger.Warning("[SkyColours] PS field not found."); return; }

                for (int i = 0; i < instances.Length; i++)
                {
                    if ((object)instances[i] == null) continue;
                    ParticleSystem[] pss = psField.GetValue(instances[i]) as ParticleSystem[];
                    if ((object)pss == null) continue;
                    for (int j = 0; j < pss.Length; j++)
                        if ((object)pss[j] != null) psList.Add(pss[j]);
                }

                if (psList.Count == 0) { MelonLogger.Warning("[SkyColours] No PS in EffectInstances."); return; }

                // Cache default rates on first call (only reset by storm toggle, not slider)
                if ((object)_cachedRainPS == null)
                {
                    var validPS = new System.Collections.Generic.List<ParticleSystem>();
                    var validRates = new System.Collections.Generic.List<float>();
                    float defaultStormRate = 100f; // default for storm particles that spawn disabled

                    for (int i = 0; i < psList.Count; i++)
                    {
                        var em = psList[i].emission;
                        float rot = em.rateOverTime.mode == ParticleSystemCurveMode.Constant
                            ? em.rateOverTime.constant : em.rateOverTime.curveMultiplier;
                        float rod = em.rateOverDistance.mode == ParticleSystemCurveMode.Constant
                            ? em.rateOverDistance.constant : em.rateOverDistance.curveMultiplier;

                        // Use actual rate if > 0, otherwise use default storm rate
                        // Storm particles spawn disabled (rate=0) — we still need to cache them
                        float baseRate;
                        if (rot > 0f)
                            baseRate = rot;
                        else if (rod > 0f)
                            baseRate = rod * 10f;
                        else
                            baseRate = defaultStormRate;

                        validPS.Add(psList[i]);
                        validRates.Add(baseRate);

                        // Convert rateOverDistance emitters to rateOverTime
                        if (rod > 0f && rot <= 0f)
                        {
                            em.rateOverDistance = 0f;
                            em.rateOverTime = baseRate;
                        }
                        // Enable emission and set rate for particles that spawned disabled
                        if (rot <= 0f && rod <= 0f)
                        {
                            em.enabled = true;
                            em.rateOverTime = baseRate;
                        }
                    }

                    _cachedRainPS = validPS.ToArray();
                    _defaultEmissionRates = validRates.ToArray();

                    MelonLogger.Msg("[SkyColours] Storm PS cached: " + _cachedRainPS.Length
                        + " emitters (world only). Base rate="
                        + (_defaultEmissionRates.Length > 0 ? _defaultEmissionRates[0].ToString("F1") : "none"));
                }

                if (_cachedRainPS.Length == 0)
                { MelonLogger.Warning("[SkyColours] No emitters found on world EffectInstances."); return; }

                // Apply multiplier via rateOverTime
                int applied = 0;
                for (int i = 0; i < _cachedRainPS.Length; i++)
                {
                    if ((object)_cachedRainPS[i] == null) continue;
                    try
                    {
                        var em = _cachedRainPS[i].emission;
                        em.rateOverTime = _defaultEmissionRates[i] * mult;
                        applied++;
                    }
                    catch { }
                }
                MelonLogger.Msg("[SkyColours] Rain intensity x" + mult.ToString("F2") + " applied to " + applied + " systems.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[SkyColours] ApplyRainIntensity: " + ex.Message); }
        }

        private static int _idSunSky = -1;
        private static int _idMoonSky = -1;
        private static int _idFog = -1;
        private static int _idAmbient = -1;
        private static bool _idsLoaded = false;

        private static MonoBehaviour _skyComp = null;
        private static FieldInfo _cycleField = null;
        private static FieldInfo _hourField = null;

        // Captured scene defaults — set once on scene init before any mod touches them
        private static float _defaultHour = 12f;
        private static bool _defaultsCaptured = false;

        public static void CaptureSceneDefaults()
        {
            _defaultsCaptured = false;
            _defaultHour = 12f;
            try
            {
                MonoBehaviour[] all = Object.FindObjectsOfType<MonoBehaviour>();
                for (int i = 0; i < all.Length; i++)
                {
                    if (!string.Equals(all[i].GetType().Name, "TOD_Sky",
                        System.StringComparison.Ordinal)) continue;
                    FieldInfo cf = all[i].GetType().GetField("Cycle",
                        BindingFlags.Public | BindingFlags.Instance);
                    if ((object)cf == null) break;
                    object cycle = cf.GetValue(all[i]);
                    if ((object)cycle == null) break;
                    FieldInfo hf = cycle.GetType().GetField("Hour",
                        BindingFlags.Public | BindingFlags.Instance);
                    if ((object)hf == null) break;
                    _defaultHour = (float)hf.GetValue(cycle);
                    _defaultsCaptured = true;
                    MelonLogger.Msg("[SkyColours] Captured default hour=" + _defaultHour);
                    break;
                }
                if (!_defaultsCaptured)
                {
                    // TOD_Sky only exists in gameplay scenes, not the menu — skip silently
                    _defaultHour = 12f;
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SkyColours] CaptureSceneDefaults: " + ex.Message);
            }
        }

        // Restore sky to exactly what it was when the scene loaded
        public static void RestoreDefault()
        {
            CurrentPreset = 0;
            // Only capture if we haven't already — re-capturing after a preset
            // would read the preset's hour, not the original scene hour
            if (!_defaultsCaptured)
                CaptureSceneDefaults();
            if (_defaultsCaptured)
                SetTODHour(_defaultHour);
            _skyComp = null; // clear cache so next SetTODHour re-finds the component
            MelonLogger.Msg("[SkyColours] Restored default (preset=0, hour=" + (_defaultsCaptured ? _defaultHour : -1f) + ")");
        }

        // Used by Save/Load — stores the preset index without touching the game world
        public static void SetPresetSilent(int preset)
        {
            CurrentPreset = preset;
        }

        public static void ApplyPatch(HarmonyLib.Harmony harmony)
        {
            int patchCount = 0;
            // ── 1. TOD_Sky.LateUpdate (existing) ──────────────────────────
            try
            {
                System.Type todSkyType = null;
                System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    todSkyType = assemblies[i].GetType("TOD_Sky");
                    if ((object)todSkyType != null) break;
                }
                if ((object)todSkyType == null)
                {
                    MelonLogger.Warning("[SkyColours] TOD_Sky type not found for patching.");
                    DiagnosticsManager.Report("SkyColours", false, "TOD_Sky type not found");
                    return;
                }

                MethodInfo lateUpdate = todSkyType.GetMethod("LateUpdate",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)lateUpdate == null)
                {
                    MelonLogger.Warning("[SkyColours] TOD_Sky.LateUpdate not found.");
                    DiagnosticsManager.Report("SkyColours", false, "LateUpdate not found");
                    return;
                }

                MethodInfo postfix = typeof(SkyColours_Patch).GetMethod("Postfix",
                    BindingFlags.Public | BindingFlags.Static);
                harmony.Patch(lateUpdate, postfix: new HarmonyMethod(postfix));
                MelonLogger.Msg("[SkyColours] [1/4] Patched TOD_Sky.LateUpdate.");
                patchCount++;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SkyColours] [1/4] TOD_Sky patch FAILED: " + ex.Message);
            }

            // ── 2. EffectList.TLJ (prefix+postfix) ───────────────────────
            try
            {
                MethodInfo tljMethod = typeof(EffectList).GetMethod("TLJ\u0081Hrt",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)tljMethod != null)
                {
                    MethodInfo tljPrefix = typeof(SkyColours_TLJPatch).GetMethod("Prefix",
                        BindingFlags.Public | BindingFlags.Static);
                    MethodInfo tljPostfix = typeof(SkyColours_TLJPatch).GetMethod("Postfix",
                        BindingFlags.Public | BindingFlags.Static);
                    MelonLogger.Msg("[SkyColours] [2/4] TLJ found. prefix=" + ((object)tljPrefix != null) + " postfix=" + ((object)tljPostfix != null));
                    harmony.Patch(tljMethod,
                        prefix: new HarmonyMethod(tljPrefix),
                        postfix: new HarmonyMethod(tljPostfix));
                    MelonLogger.Msg("[SkyColours] [2/4] Patched EffectList.TLJ (prefix+postfix).");
                    patchCount++;
                }
                else
                    MelonLogger.Warning("[SkyColours] [2/4] TLJ method not found.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SkyColours] [2/4] TLJ patch FAILED: " + ex.Message);
            }

            // ── 3. EffectList.LateUpdate (storm enforcement) ─────────────
            try
            {
                MethodInfo elLateUpdate = typeof(EffectList).GetMethod("LateUpdate",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)elLateUpdate != null)
                {
                    MethodInfo elPostfix = typeof(SkyColours_EffectListPatch).GetMethod("Postfix",
                        BindingFlags.Public | BindingFlags.Static);
                    harmony.Patch(elLateUpdate, postfix: new HarmonyMethod(elPostfix));
                    MelonLogger.Msg("[SkyColours] [3/4] Patched EffectList.LateUpdate (storm enforcement).");
                    patchCount++;
                }
                else
                    MelonLogger.Warning("[SkyColours] [3/4] EffectList.LateUpdate not found.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SkyColours] [3/4] EffectList.LateUpdate patch FAILED: " + ex.Message);
            }

            // ── 4. EffectList.UpdateEnvironmentStates ─────────────────────
            try
            {
                MethodInfo uesMethod = typeof(EffectList).GetMethod("UpdateEnvironmentStates",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)uesMethod != null)
                {
                    MethodInfo uesPostfix = typeof(SkyColours_UpdateEnvPatch).GetMethod("Postfix",
                        BindingFlags.Public | BindingFlags.Static);
                    harmony.Patch(uesMethod, postfix: new HarmonyMethod(uesPostfix));
                    MelonLogger.Msg("[SkyColours] [4/4] Patched EffectList.UpdateEnvironmentStates.");
                    patchCount++;
                }
                else
                    MelonLogger.Warning("[SkyColours] [4/4] UpdateEnvironmentStates not found.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SkyColours] [4/4] UES patch FAILED: " + ex.Message);
            }

            MelonLogger.Msg("[SkyColours] Patch complete: " + patchCount + "/4 succeeded.");
            DiagnosticsManager.Report("SkyColours", patchCount >= 2);
        }

        public static void ApplyColours()
        {
            if (CurrentPreset == 0) return;
            try
            {
                if (!_idsLoaded)
                {
                    _idSunSky = Shader.PropertyToID("TOD_SunSkyColor");
                    _idMoonSky = Shader.PropertyToID("TOD_MoonSkyColor");
                    _idFog = Shader.PropertyToID("TOD_FogColor");
                    _idAmbient = Shader.PropertyToID("TOD_AmbientColor");
                    _idsLoaded = true;
                }
                Shader.SetGlobalColor(_idSunSky, SunSkyColors[CurrentPreset]);
                Shader.SetGlobalColor(_idMoonSky, MoonSkyColors[CurrentPreset]);
                Shader.SetGlobalColor(_idFog, FogColors[CurrentPreset]);
                Shader.SetGlobalColor(_idAmbient, AmbientColors[CurrentPreset]);
                RenderSettings.ambientLight = AmbientColors[CurrentPreset];
                RenderSettings.ambientSkyColor = AmbientColors[CurrentPreset];
                RenderSettings.fogColor = FogColors[CurrentPreset];
            }
            catch { }
        }

        public static void ApplyPreset(int index)
        {
            if (index < 0 || index >= PresetNames.Length) return;
            // Capture the original hour BEFORE we change it — this is the last safe moment
            // (OnSceneWasInitialized may have been too early if TOD_Sky wasn't ready yet)
            if (!_defaultsCaptured)
                CaptureSceneDefaults();
            CurrentPreset = index;
            SetTODHour(TODHours[index]);
            MelonLogger.Msg("[SkyColours] Preset -> " + PresetNames[index]);
        }

        public static void ToggleStorm()
        {
            StormEnabled = !StormEnabled;
            ApplyStorm();
            if (StormEnabled)
                CreateRain();
            else
                DestroyRain();
            MelonLogger.Msg("[SkyColours] Storm -> " + (StormEnabled ? "ON" : "OFF"));
        }

        private static VisualModifier _originalModifier = VisualModifier.None;
        private static bool _modifierStored = false;

        private static void ApplyStorm()
        {
            try
            {
                CameraEffects ce = Object.FindObjectOfType<CameraEffects>();
                if ((object)ce == null) { MelonLogger.Warning("[SkyColours] CameraEffects not found."); return; }

                SessionManager sm = Object.FindObjectOfType<SessionManager>();
                MethodInfo getWorld = typeof(SessionManager).GetMethod("GetWorld",
                    BindingFlags.Public | BindingFlags.Instance);
                World world = World.Highlands;
                if ((object)getWorld != null && (object)sm != null)
                {
                    world = (World)getWorld.Invoke(sm, null);
                    if (world == World.None) world = World.Highlands;
                }

                VisualModifier targetModifier = StormEnabled ? VisualModifier.Storm : _originalModifier;
                if ((object)sm != null)
                {
                    MethodInfo sessionStarted = typeof(SessionManager).GetMethod("SessionStarted",
                        BindingFlags.Public | BindingFlags.Instance);
                    bool inSession = (object)sessionStarted != null && (bool)sessionStarted.Invoke(sm, null);
                    if (inSession)
                    {
                        FieldInfo sessionDataFld = typeof(SessionManager).GetField(
                            "\u0083ESVMoz", BindingFlags.Public | BindingFlags.Instance);
                        if ((object)sessionDataFld != null)
                        {
                            object sessionData = sessionDataFld.GetValue(sm);
                            if ((object)sessionData != null)
                            {
                                FieldInfo levelInfoFld = sessionData.GetType().GetField(
                                    "vebf\u0081kn", BindingFlags.Public | BindingFlags.Instance);
                                if ((object)levelInfoFld != null)
                                {
                                    object levelInfo = levelInfoFld.GetValue(sessionData);
                                    if ((object)levelInfo != null)
                                    {
                                        FieldInfo visualModFld = levelInfo.GetType().GetField(
                                            "\u007CzvoQ\u0084\u005B", BindingFlags.Public | BindingFlags.Instance);
                                        if ((object)visualModFld != null)
                                        {
                                            if (StormEnabled && !_modifierStored)
                                            {
                                                _originalModifier = (VisualModifier)visualModFld.GetValue(levelInfo);
                                                _modifierStored = true;
                                                targetModifier = VisualModifier.Storm;
                                            }
                                            else if (!StormEnabled)
                                            {
                                                targetModifier = _modifierStored ? _originalModifier : VisualModifier.None;
                                                _modifierStored = false;
                                            }
                                            visualModFld.SetValue(levelInfo, targetModifier);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                MethodInfo destroySky = typeof(CameraEffects).GetMethod("S\u0083wckiX",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)destroySky != null)
                    try { destroySky.Invoke(ce, null); } catch { }

                MethodInfo setCE = typeof(CameraEffects).GetMethod("SetCameraEffects",
                    BindingFlags.Public | BindingFlags.Instance);
                World ceWorld = world;
                if (ceWorld == World.Overworld || (int)ceWorld < 1 || (int)ceWorld > 11)
                    ceWorld = World.Highlands;
                if ((object)setCE != null)
                    try { setCE.Invoke(ce, new object[] { ceWorld, targetModifier, null }); }
                    catch (System.Exception ex4) { MelonLogger.Warning("[SkyColours] SetCameraEffects fail: " + (ex4.InnerException != null ? ex4.InnerException.Message : ex4.Message)); }

                EffectList[] allEffectLists = Object.FindObjectsOfType<EffectList>();
                MethodInfo refreshEffects = typeof(EffectList).GetMethod("RefreshEffects",
                    BindingFlags.Public | BindingFlags.Instance);
                MethodInfo setEnvFlag = typeof(EffectList).GetMethod("SetEffectEnvironment",
                    BindingFlags.Public | BindingFlags.Instance);
                MethodInfo spawnEffects = typeof(EffectList).GetMethod("TLJ\u0081Hrt",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                bool stormOn = StormEnabled;
                System.Type efhType = (object)setEnvFlag != null
                    ? setEnvFlag.GetParameters()[0].ParameterType : null;
                for (int i = 0; i < allEffectLists.Length; i++)
                {
                    try
                    {
                        if ((object)refreshEffects != null) refreshEffects.Invoke(allEffectLists[i], null);
                        if ((object)setEnvFlag != null && (object)efhType != null)
                        {
                            setEnvFlag.Invoke(allEffectLists[i], new object[] { System.Enum.ToObject(efhType, 7), stormOn });
                            setEnvFlag.Invoke(allEffectLists[i], new object[] { System.Enum.ToObject(efhType, 4), !stormOn });
                        }
                        if ((object)spawnEffects != null) spawnEffects.Invoke(allEffectLists[i], null);
                    }
                    catch { }
                }

                _cachedEffectLists = allEffectLists;
                _effectListCacheTime = Time.time;

                MelonLogger.Msg("[SkyColours] Storm -> " + (StormEnabled ? "ON" : "OFF")
                    + " | world=" + ceWorld + " mod=" + targetModifier
                    + " | lists=" + allEffectLists.Length
                    + " | spawnMethod=" + ((object)spawnEffects != null));

                if (StormEnabled)
                {
                    _cachedRainPS = null; // force recache since particles just respawned
                    ApplyRainIntensity();
                }
            }
            catch (System.Exception ex)
            {
                System.Exception inner = ex;
                while (inner.InnerException != null) inner = inner.InnerException;
                MelonLogger.Error("[SkyColours] ApplyStorm: " + inner.Message);
            }
        }

        private static void SetTODHour(float hour)
        {
            try
            {
                if ((object)_skyComp == null)
                {
                    MonoBehaviour[] all = Object.FindObjectsOfType<MonoBehaviour>();
                    for (int i = 0; i < all.Length; i++)
                        if (string.Equals(all[i].GetType().Name, "TOD_Sky", System.StringComparison.Ordinal))
                        { _skyComp = all[i]; break; }
                }
                if ((object)_skyComp == null) return;
                if ((object)_cycleField == null)
                    _cycleField = _skyComp.GetType().GetField("Cycle", BindingFlags.Public | BindingFlags.Instance);
                if ((object)_cycleField == null) return;
                object cycle = _cycleField.GetValue(_skyComp);
                if ((object)cycle == null) return;
                if ((object)_hourField == null)
                    _hourField = cycle.GetType().GetField("Hour", BindingFlags.Public | BindingFlags.Instance);
                if ((object)_hourField != null)
                    _hourField.SetValue(cycle, hour);
            }
            catch { }
        }

        private static System.Type _tickEfhType = null;
        private static MethodInfo _tickSetEnvFlag = null;

        public static void Tick()
        {
            TickStorm(ref _tickEfhType, ref _tickSetEnvFlag);

            // Create rain if storm is on but rain doesn't exist yet (deferred from ToggleStorm)
            if (StormEnabled && (object)_rainObj == null)
                CreateRain();
        }

        private static EffectList[] _cachedEffectLists = null;
        private static float _effectListCacheTime = -999f;

        public static void TickStorm(ref System.Type efhType, ref MethodInfo setEnvFlag)
        {
            if (!StormEnabled) return;
            try
            {
                if ((object)setEnvFlag == null)
                    setEnvFlag = typeof(EffectList).GetMethod("SetEffectEnvironment",
                        BindingFlags.Public | BindingFlags.Instance);
                if ((object)setEnvFlag == null) return;

                if ((object)_cachedEffectLists == null || Time.time - _effectListCacheTime > 5f)
                {
                    _cachedEffectLists = Object.FindObjectsOfType<EffectList>();
                    _effectListCacheTime = Time.time;
                }

                if ((object)efhType == null)
                    efhType = setEnvFlag.GetParameters()[0].ParameterType;
                if ((object)efhType == null) return;

                object stormVal = System.Enum.ToObject(efhType, 7);
                object normalVal = System.Enum.ToObject(efhType, 4);

                for (int i = 0; i < _cachedEffectLists.Length; i++)
                {
                    try
                    {
                        setEnvFlag.Invoke(_cachedEffectLists[i], new object[] { stormVal, true });
                        setEnvFlag.Invoke(_cachedEffectLists[i], new object[] { normalVal, false });
                    }
                    catch { }
                }

                // Apply emission rate multiplier to cached PS
                // Don't force-enable — game's LateUpdate handles that via env flags
                if ((object)_cachedRainPS != null && _cachedRainPS.Length > 0)
                {
                    float mult = RainMultipliers[RainIntensityLevel - 1];
                    for (int i = 0; i < _cachedRainPS.Length; i++)
                    {
                        if ((object)_cachedRainPS[i] == null) continue;
                        try
                        {
                            var em = _cachedRainPS[i].emission;
                            if (em.enabled)
                                em.rateOverTime = _defaultEmissionRates[i] * mult;
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        public static void Reset()
        {
            if (StormEnabled) { StormEnabled = false; try { ApplyStorm(); } catch { } }
            DestroyRain();
            _modifierStored = false;
            _originalModifier = VisualModifier.None;
            CurrentPreset = 0;
            RainIntensityLevel = 5;
            _cachedRainPS = null;
            _defaultEmissionRates = null;
            _elFieldsResolved = false;
            _elEnvFlagsField = null;
            _elLtpField = null;
            _eiPsField = null;
            _eiBaseEffField = null;
            _skyComp = null;
            _cycleField = null;
            _hourField = null;
            _defaultsCaptured = false;
            _idsLoaded = false;
            _cachedEffectLists = null;
            _tickEfhType = null;
            _tickSetEnvFlag = null;
        }

        // Called by TLJ patch immediately after fresh EffectInstances are spawned
        public static void ApplyIntensityToEffectList(EffectList effectList)
        {
            if (!StormEnabled) return;
            try
            {
                float mult = RainMultipliers[RainIntensityLevel - 1];
                if (System.Math.Abs(mult - 1f) < 0.001f) return; // default � no change needed

                // Read ltpCVSt � the list of active particle EffectInstances just populated
                System.Reflection.FieldInfo ltpField = typeof(EffectList).GetField("ltpCVSt",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if ((object)ltpField == null) return;

                System.Collections.Generic.List<EffectInstance> instances =
                    ltpField.GetValue(effectList) as System.Collections.Generic.List<EffectInstance>;
                if ((object)instances == null || instances.Count == 0) return;

                System.Reflection.FieldInfo psField = typeof(EffectInstance).GetField(
                    "\u007FJm\u007DD\u0060\u007B",
                    BindingFlags.Public | BindingFlags.Instance);
                if ((object)psField == null) return;

                for (int i = 0; i < instances.Count; i++)
                {
                    if ((object)instances[i] == null) continue;
                    ParticleSystem[] pss = psField.GetValue(instances[i]) as ParticleSystem[];
                    if ((object)pss == null) continue;
                    for (int j = 0; j < pss.Length; j++)
                    {
                        if ((object)pss[j] == null) continue;
                        try
                        {
                            var em = pss[j].emission;
                            float rod = em.rateOverDistance.mode == ParticleSystemCurveMode.Constant
                                ? em.rateOverDistance.constant : em.rateOverDistance.curveMultiplier;
                            float rot = em.rateOverTime.mode == ParticleSystemCurveMode.Constant
                                ? em.rateOverTime.constant : em.rateOverTime.curveMultiplier;

                            if (rod > 0f)
                            {
                                // Convert to rateOverTime and apply intensity
                                em.rateOverDistance = 0f;
                                em.rateOverTime = rod * 10f * mult;
                            }
                            else if (rot > 0f)
                            {
                                em.rateOverTime = rot * mult;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SkyColours] ApplyIntensityToEffectList: " + ex.Message);
            }
        }
    }

    public static class SkyColours_TLJPatch
    {
        private static System.Reflection.FieldInfo _envField = null;
        private static bool _resolved = false;

        /// <summary>
        /// PREFIX: Runs BEFORE TLJ spawns LoopingParticle instances.
        /// Sets Visuals_Storm=true so storm rain particles actually get created.
        /// Without this, game refreshes create empty env flag arrays (all false),
        /// so TLJ skips storm particles entirely — causing rain to disappear.
        /// </summary>
        public static void Prefix(EffectList __instance)
        {
            if (!SkyColours.StormEnabled) return;
            try
            {
                if (!_resolved)
                {
                    _resolved = true;
                    _envField = typeof(EffectList).GetField(
                        "\u007Ejl\u0082liu",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                }
                if ((object)_envField == null) return;
                bool[] flags = _envField.GetValue(__instance) as bool[];
                if ((object)flags == null || flags.Length <= 7) return;
                flags[7] = true;  // Visuals_Storm
                flags[4] = false; // Visuals_Normal
            }
            catch { }
        }

        /// <summary>
        /// POSTFIX: Runs AFTER TLJ spawns particles — applies rain intensity
        /// to the freshly created ParticleSystems.
        /// </summary>
        public static void Postfix(EffectList __instance)
        {
            SkyColours.ApplyIntensityToEffectList(__instance);
        }
    }

    /// <summary>
    /// KEY FIX: Runs AFTER EffectList.LateUpdate — the game's own particle enable/disable
    /// logic has already run, so our storm enforcement is the final word each frame.
    /// This eliminates the toggle war that caused weak/flickering rain.
    /// </summary>
    public static class SkyColours_EffectListPatch
    {
        public static void Postfix(EffectList __instance)
        {
            SkyColours.EnforceStormOnEffectList(__instance);
        }
    }

    /// <summary>
    /// Runs AFTER UpdateEnvironmentStates — ensures the storm environment flag
    /// survives Awake() → UpdateEnvironmentStates() → TLJ so rain particles
    /// actually get spawned during RefreshEffects.
    /// </summary>
    public static class SkyColours_UpdateEnvPatch
    {
        private static System.Reflection.FieldInfo _envField = null;
        private static bool _resolved = false;

        public static void Postfix(EffectList __instance)
        {
            if (!SkyColours.StormEnabled) return;
            try
            {
                if (!_resolved)
                {
                    _resolved = true;
                    _envField = typeof(EffectList).GetField(
                        "\u007Ejl\u0082liu",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                }
                if ((object)_envField == null) return;
                bool[] flags = _envField.GetValue(__instance) as bool[];
                if ((object)flags == null || flags.Length <= 7) return;
                flags[7] = true;  // Visuals_Storm
                flags[4] = false; // Visuals_Normal
            }
            catch { }
        }
    }

    public static class SkyColours_Patch
    {
        private static System.Type _efhType = null;
        private static MethodInfo _setEnvFlag = null;

        public static void Postfix()
        {
            SkyColours.ApplyColours();
            SkyColours.TickStorm(ref _efhType, ref _setEnvFlag);
        }
    }
}