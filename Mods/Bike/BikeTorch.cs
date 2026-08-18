using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class BikeTorch
    {
        // ── State ─────────────────────────────────────────────────────
        public static bool Enabled { get; private set; } = false;
        public static bool DiscoEnabled { get; private set; } = false;

        private static readonly float[]  IntensityValues = { 0.5f, 1.0f, 2.0f, 3.5f, 5.0f };
        private static readonly string[] IntensityLabels = { "Dim", "Low", "Medium", "High", "Max" };
        public  static int IntensityIndex = 2; // default Medium

        public static string IntensityDisplay => IntensityLabels[IntensityIndex];

        private static readonly Color[] DiscoNeon =
        {
            new Color(1.00f, 0.05f, 0.55f, 1f),
            new Color(0.10f, 0.35f, 1.00f, 1f),
            new Color(0.20f, 1.00f, 0.20f, 1f),
            new Color(1.00f, 0.90f, 0.05f, 1f),
            new Color(1.00f, 0.35f, 0.05f, 1f),
            new Color(0.75f, 0.05f, 1.00f, 1f),
            new Color(0.05f, 1.00f, 0.95f, 1f),
            new Color(1.00f, 0.05f, 0.10f, 1f),
        };

        private static Light _torchLight = null;
        private static int _discoIndex = 0;
        private static float _discoNextFlip = 0f;

        // ── Toggle / Selectors ────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            if (!Enabled && DiscoEnabled)
                DiscoEnabled = false;
            Apply();
            ModLog.Feedback("[BikeTorch] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void ToggleDisco()
        {
            DiscoEnabled = !DiscoEnabled;
            if (DiscoEnabled)
            {
                // Disco torch needs the light on
                if (!Enabled)
                {
                    Enabled = true;
                    Apply();
                }
                _discoIndex = 0;
                _discoNextFlip = 0f;
                ApplyDiscoColour();
            }
            else
            {
                Apply(); // restores white
            }
            ModLog.Feedback("[BikeTorch] Disco -> " + (DiscoEnabled ? "ON" : "OFF"));
        }

        public static void PrevIntensity()
        {
            if (IntensityIndex > 0) { IntensityIndex--; Apply(); }
        }

        public static void NextIntensity()
        {
            if (IntensityIndex < IntensityValues.Length - 1) { IntensityIndex++; Apply(); }
        }

        // ── Apply ─────────────────────────────────────────────────────
        public static void Apply()
        {
            if (!Enabled)
            {
                if (UnityNull.Alive(_torchLight))
                    _torchLight.enabled = false;
                return;
            }

            if (!UnityNull.Alive(_torchLight))
            {
                _torchLight = null;
                FindOrCreateTorch();
            }

            if (UnityNull.Alive(_torchLight))
            {
                _torchLight.enabled   = true;
                _torchLight.intensity = IntensityValues[IntensityIndex];
                if (!DiscoEnabled)
                    _torchLight.color = Color.white;
            }
        }

        public static void TickDisco()
        {
            if (!DiscoEnabled || !Enabled) return;
            float now = Time.unscaledTime;
            if (now < _discoNextFlip) return;
            _discoIndex = (_discoIndex + 1) % DiscoNeon.Length;
            _discoNextFlip = now + 0.12f;
            ApplyDiscoColour();
        }

        private static void ApplyDiscoColour()
        {
            if (!UnityNull.Alive(_torchLight))
            {
                _torchLight = null;
                FindOrCreateTorch();
            }
            if (UnityNull.Alive(_torchLight))
            {
                _torchLight.enabled = true;
                _torchLight.color = DiscoNeon[_discoIndex];
            }
        }

        // ── Find or create spotlight ──────────────────────────────────
        private static void FindOrCreateTorch()
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null)
                {
                    ModLog.Warn("[BikeTorch] Player_Human not found.");
                    return;
                }

                // Try to find the game's existing headlight (a Spot light on the bike)
                Light[] lights = player.GetComponentsInChildren<Light>(true);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Spot)
                    {
                        _torchLight = lights[i];
                        ModLog.Debug("[BikeTorch] Found existing spotlight: "
                            + lights[i].gameObject.name);
                        return;
                    }
                }

                // No spotlight found — create one on the bike rigidbody's GameObject
                Rigidbody rb = player.GetComponentInChildren<Rigidbody>();
                GameObject host = (object)rb != null ? rb.gameObject : player;

                var torchGO = new GameObject("BikeTorchLight");
                torchGO.transform.SetParent(host.transform, false);
                // Position slightly forward and above the bike centre
                torchGO.transform.localPosition = new Vector3(0f, 0.3f, 0.5f);
                // Tilt down slightly so the beam hits the trail ahead
                torchGO.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);

                _torchLight               = torchGO.AddComponent<Light>();
                _torchLight.type          = LightType.Spot;
                _torchLight.spotAngle     = 45f;
                _torchLight.range         = 35f;
                _torchLight.color         = Color.white;
                _torchLight.shadows       = LightShadows.None;

                ModLog.Debug("[BikeTorch] Created new spotlight on: " + host.name);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[BikeTorch] FindOrCreateTorch: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "BikeTorch");
            }
        }

        // ── Reset (on scene unload) ───────────────────────────────────
        public static void Reset()
        {
            // Light component will be destroyed by Unity on scene unload.
            // Just clear the cache and state so next scene starts fresh.
            _torchLight = null;
            Enabled = false;
            DiscoEnabled = false;
            _discoIndex = 0;
            _discoNextFlip = 0f;
        }
    }
}
