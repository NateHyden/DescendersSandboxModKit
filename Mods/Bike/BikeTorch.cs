using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class BikeTorch
    {
        // ── State ─────────────────────────────────────────────────────
        public static bool Enabled { get; private set; } = false;

        private static readonly float[]  IntensityValues = { 0.5f, 1.0f, 2.0f, 3.5f, 5.0f };
        private static readonly string[] IntensityLabels = { "Dim", "Low", "Medium", "High", "Max" };
        public  static int IntensityIndex = 2; // default Medium

        public static string IntensityDisplay => IntensityLabels[IntensityIndex];

        private static Light _torchLight = null;

        // ── Toggle / Selectors ────────────────────────────────────────
        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
            MelonLogger.Msg("[BikeTorch] " + (Enabled ? "ON" : "OFF"));
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
                if ((object)_torchLight != null)
                    _torchLight.enabled = false;
                return;
            }

            if ((object)_torchLight == null)
                FindOrCreateTorch();

            if ((object)_torchLight != null)
            {
                _torchLight.enabled      = true;
                _torchLight.intensity    = IntensityValues[IntensityIndex];
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
                    MelonLogger.Warning("[BikeTorch] Player_Human not found.");
                    return;
                }

                // Try to find the game's existing headlight (a Spot light on the bike)
                Light[] lights = player.GetComponentsInChildren<Light>(true);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Spot)
                    {
                        _torchLight = lights[i];
                        MelonLogger.Msg("[BikeTorch] Found existing spotlight: "
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

                MelonLogger.Msg("[BikeTorch] Created new spotlight on: " + host.name);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[BikeTorch] FindOrCreateTorch: " + ex.Message);
            }
        }

        // ── Reset (on scene unload) ───────────────────────────────────
        public static void Reset()
        {
            // Light component will be destroyed by Unity on scene unload.
            // Just clear the cache and state so next scene starts fresh.
            _torchLight = null;
            Enabled     = false;
        }
    }
}
