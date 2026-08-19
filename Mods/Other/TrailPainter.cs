using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class TrailPainter
    {
        public static bool Enabled { get; private set; } = false;
        public static int ColourIndex { get; private set; } = 0;

        private static readonly Color[] Palette = new Color[]
        {
            new Color(1.00f, 0.20f, 0.80f),
            new Color(0.20f, 1.00f, 0.40f),
            new Color(0.25f, 0.65f, 1.00f),
            new Color(1.00f, 0.80f, 0.10f),
            new Color(1.00f, 0.30f, 0.10f),
        };
        public static readonly string[] ColourNames = { "Pink", "Green", "Blue", "Gold", "Orange" };

        private static TrailRenderer _trail = null;

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(Enabled);
            ModLog.Feedback("[TrailPainter] -> " + (Enabled ? "ON colour=" + ColourNames[ColourIndex] : "OFF"));
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

        public static void Tick()
        {
            if (!Enabled) return;
            if (!UnityNull.Alive(_trail))
            {
                _trail = null;
                Apply(true);
            }
        }


        private static void Apply(bool on)
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if (!UnityNull.Alive(player)) { ModLog.Warn("[TrailPainter] Player_Human not found."); return; }
                Transform bikeModel = player.transform.Find("BikeModel");
                Transform anchor = UnityNull.Alive(bikeModel) ? bikeModel : player.transform;

                if (on)
                {
                    if (UnityNull.Alive(_trail)) return;
                    _trail = null;
                    GameObject trailObj = new GameObject("SandboxTrail");
                    trailObj.transform.SetParent(anchor, false);
                    trailObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                    _trail = trailObj.AddComponent<TrailRenderer>();
                    _trail.time = 1.2f;
                    _trail.startWidth = 0.18f;
                    _trail.endWidth = 0.02f;
                    _trail.material = new Material(Shader.Find("Sprites/Default"));
                    _trail.minVertexDistance = 0.05f;
                    ApplyColour();
                    ModLog.Debug("[TrailPainter] TrailRenderer attached to " + anchor.name + ".");
                }
                else
                {
                    if (UnityNull.Alive(_trail))
                        UnityEngine.Object.Destroy(_trail.gameObject);
                    _trail = null;
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[TrailPainter] Apply: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "TrailPainter"); }
        }

        private static void ApplyColour()
        {
            if (!UnityNull.Alive(_trail)) { _trail = null; return; }
            Color c = Palette[ColourIndex];
            _trail.startColor = c;
            Color end = c; end.a = 0f;
            _trail.endColor = end;
        }

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

