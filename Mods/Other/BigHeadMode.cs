using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Scales the player's head bone. Bone confirmed from forensics dump:
    // Player_Human/Cyclist/character_clothed_ragdoll/bicycleDude_Rig_V02_Slave_Root/
    //   .../C_Spine_6/C_Neck1/C_Neck2/C_Head1
    // Found by exact-name search first, falling back to a "contains" search
    // so a future rig rename doesn't silently break this — if neither hits,
    // logs every candidate bone name so the right one can be picked in one
    // test cycle rather than guessing blind.
    public static class BigHeadMode
    {
        public static bool Enabled { get; private set; } = false;
        public static int Level { get; private set; } = 15; // index into Scales, default a goofy-but-not-absurd size

        private static readonly float[] Scales =
        {
            1.5f, 2.0f, 2.5f, 3.0f, 3.5f, 4.0f, 4.5f, 5.0f, 5.5f, 6.0f,
            6.5f, 7.0f, 7.5f, 8.0f, 8.5f, 9.0f, 9.5f, 10.0f, 11.0f, 12.0f
        };
        public static string LevelDisplay => Scales[Level - 1].ToString("0.0") + "x";

        private static Transform _headBone = null;
        private static Vector3 _defaultScale = Vector3.one;
        private static bool _captured = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(Enabled);
            MelonLogger.Msg("[BigHeadMode] -> " + (Enabled ? "ON " + LevelDisplay : "OFF"));
        }

        public static void Increase() { if (Level < 20) { Level++; if (Enabled) ApplyScale(); } }
        public static void Decrease() { if (Level > 1) { Level--; if (Enabled) ApplyScale(); } }

        private static bool FindHeadBone()
        {
            if ((object)_headBone != null) return true;
            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) return false;

            Transform[] all = player.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (string.Equals(all[i].name, "bicycleDude_Rig_V02_Slave_C_Head1", System.StringComparison.Ordinal))
                { _headBone = all[i]; break; }

            if ((object)_headBone == null)
            {
                for (int i = 0; i < all.Length; i++)
                    if (all[i].name.IndexOf("Head", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    { _headBone = all[i]; MelonLogger.Msg("[BigHeadMode] Exact bone name missed, using fallback: " + all[i].name); break; }
            }

            if ((object)_headBone == null)
            {
                MelonLogger.Warning("[BigHeadMode] No head bone found. Candidate bones containing 'Neck' or 'Spine':");
                for (int i = 0; i < all.Length; i++)
                    if (all[i].name.IndexOf("Neck", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || all[i].name.IndexOf("Spine", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        MelonLogger.Msg("  - " + all[i].name);
                return false;
            }
            return true;
        }

        private static void Apply(bool on)
        {
            try
            {
                if (!FindHeadBone()) return;
                if (!_captured) { _defaultScale = _headBone.localScale; _captured = true; }

                if (on) ApplyScale();
                else _headBone.localScale = _defaultScale;
            }
            catch (System.Exception ex) { MelonLogger.Error("[BigHeadMode] Apply: " + ex.Message); }
        }

        private static void ApplyScale()
        {
            if ((object)_headBone == null) return;
            _headBone.localScale = _defaultScale * Scales[Level - 1];
        }

        // Character rig animation resets bone scale every frame (same
        // issue documented for WideTyres/BikeSize) — re-enforce in
        // OnLateUpdate rather than applying once, or the head snaps back
        // to normal size almost immediately.
        public static void Tick()
        {
            if (!Enabled) return;
            if ((object)_headBone == null) { if (!FindHeadBone()) return; if (!_captured) { _defaultScale = _headBone.localScale; _captured = true; } }
            ApplyScale();
        }


        // Scene unload destroys the rig with it — clear refs so the next
        // scene re-resolves cleanly. Called from the deferred-reapply system.
        public static void ClearCache() { _headBone = null; _captured = false; }

        public static void Reset()
        {
            if (Enabled) Apply(false);
            Enabled = false;
            Level = 15;
            _headBone = null;
            _captured = false;
        }
    }
}
