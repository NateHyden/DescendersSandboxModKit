using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Adds a coloured TrailRenderer behind the bike. Purely cosmetic —
    // the renderer is added on enable and fully removed on disable, so
    // there's nothing left behind when the mod is off.
    public static class TrailPainter
    {
        public static bool Enabled { get; private set; } = false;
        public static int ColourIndex { get; private set; } = 0;

        private static readonly Color[] Palette = new Color[]
        {
            new Color(1.00f, 0.20f, 0.80f), // pink
            new Color(0.20f, 1.00f, 0.40f), // green
            new Color(0.25f, 0.65f, 1.00f), // blue
            new Color(1.00f, 0.80f, 0.10f), // gold
            new Color(1.00f, 0.30f, 0.10f), // orange
        };
        public static readonly string[] ColourNames = { "Pink", "Green", "Blue", "Gold", "Orange" };

        private static TrailRenderer _trail = null;

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(Enabled);
            MelonLogger.Msg("[TrailPainter] -> " + (Enabled ? "ON colour=" + ColourNames[ColourIndex] : "OFF"));
        }

        public static void CycleColour()
        {
            ColourIndex = (ColourIndex + 1) % Palette.Length;
            if (Enabled) ApplyColour();
        }

        public static void SetColour(int index)
        {
            if (index < 0 || index >= Palette.Length) return;
            ColourIndex = index;
            if (Enabled) ApplyColour();
        }

        // Scene load destroys the trail with the old scene — self-heal by
        // recreating it if still enabled, rather than wiring into the big
        // snapshot/reapply block in ModEntry. Called every LateUpdate.
        public static void Tick()
        {
            if (!Enabled) return;
            if ((object)_trail == null) Apply(true);
        }


        private static void Apply(bool on)
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) { MelonLogger.Warning("[TrailPainter] Player_Human not found."); return; }
                Transform bikeModel = player.transform.Find("BikeModel");
                Transform anchor = (object)bikeModel != null ? bikeModel : player.transform;

                if (on)
                {
                    if ((object)_trail != null) return; // already applied
                    GameObject trailObj = new GameObject("SandboxTrail");
                    trailObj.transform.SetParent(anchor, false);
                    // Anchor's local origin sits near wheel-contact height on
                    // both BikeModel and the player root, so the trail was
                    // rendering half-buried in the terrain — lift it to
                    // roughly frame height.
                    trailObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                    _trail = trailObj.AddComponent<TrailRenderer>();
                    _trail.time = 1.2f;
                    _trail.startWidth = 0.18f;
                    _trail.endWidth = 0.02f;
                    _trail.material = new Material(Shader.Find("Sprites/Default"));
                    _trail.minVertexDistance = 0.05f;
                    ApplyColour();
                    MelonLogger.Msg("[TrailPainter] TrailRenderer attached to " + anchor.name + ".");
                }
                else
                {
                    if ((object)_trail != null)
                    {
                        UnityEngine.Object.Destroy(_trail.gameObject);
                        _trail = null;
                    }
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[TrailPainter] Apply: " + ex.Message); }
        }

        private static void ApplyColour()
        {
            if ((object)_trail == null) return;
            Color c = Palette[ColourIndex];
            _trail.startColor = c;
            Color end = c; end.a = 0f;
            _trail.endColor = end;
        }

        // Scene unload destroys the trail object with the scene — just
        // clear the reference so a fresh one gets built next scene if
        // still enabled. Called from the deferred-reapply system.
        public static void ClearCache() { _trail = null; }

        public static void Reset()
        {
            if (Enabled) Apply(false);
            Enabled = false;
            ColourIndex = 0;
            _trail = null;
        }
    }
}
