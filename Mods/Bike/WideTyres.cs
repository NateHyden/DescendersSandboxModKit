using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class WideTyres
    {
        public static bool Enabled { get; private set; } = false;

        // Level 1-20. Level 5 = default (1x width).
        public static int Level { get; private set; } = 5;
        // Level 1 = 0.2x (very thin), Level 5 = 1.0x (default), Level 20 = 10.0x (ridiculous)
        private static readonly float[] WidthScales = {
            0.2f, 0.4f, 0.6f, 0.8f, 1.0f, 1.4f, 1.8f, 2.2f, 2.6f, 3.0f,
            3.5f, 4.0f, 4.5f, 5.0f, 5.5f, 6.5f, 7.5f, 8.5f, 9.5f, 10.0f
        };
        public static float Width { get { return WidthScales[Level - 1]; } }

        // Cached bone field references from BikeAnimation
        private static FieldInfo _backBoneField = null;
        private static FieldInfo _frontBoneField = null;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (!Enabled)
                ResetBones(); // snap back to 1x width on disable
            else
                Apply();     // apply current level on enable
            MelonLogger.Msg("[WideTyres] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void Increase()
        {
            if (Level < 20)
            {
                Level++;
                MelonLogger.Msg("[WideTyres] Increase -> Level " + Level + " (" + Width + "x)");
                Apply(); // always apply so slider previews live
            }
        }

        public static void Decrease()
        {
            if (Level > 1)
            {
                Level--;
                MelonLogger.Msg("[WideTyres] Decrease -> Level " + Level + " (" + Width + "x)");
                Apply(); // always apply so slider previews live
            }
        }

        public static void SetLevel(int v) { Level = System.Math.Max(1, System.Math.Min(20, v)); }

        // Called from OnLateUpdate every frame — reapplies after BikeModel Animation component runs.
        // Reads Y scale (set by WheelSizeTick in Update) to preserve wheel size while adding width.
        public static void Tick()
        {
            if (!Enabled) return;
            try
            {
                // Silent check — don't spam warnings every frame before player spawns
                if ((object)GameObject.Find("Player_Human") == null) return;
                Transform frontBone, backBone;
                if (!GetBones(out frontBone, out backBone)) return;
                float w = Width;
                if ((object)frontBone != null)
                {
                    float bs = frontBone.localScale.y;
                    if (bs <= 0f) bs = 1f;
                    frontBone.localScale = new Vector3(w * bs, bs, bs);
                }
                if ((object)backBone != null)
                {
                    float bs = backBone.localScale.y;
                    if (bs <= 0f) bs = 1f;
                    backBone.localScale = new Vector3(w * bs, bs, bs);
                }
            }
            catch { }
        }
        public static void Apply()
        {
            try
            {
                Transform frontBone, backBone;
                if (!GetBones(out frontBone, out backBone)) return;

                float w = Width;
                if ((object)frontBone != null)
                {
                    float bs = frontBone.localScale.y;
                    if (bs <= 0f) bs = 1f;
                    frontBone.localScale = new Vector3(w * bs, bs, bs);
                }
                if ((object)backBone != null)
                {
                    float bs = backBone.localScale.y;
                    if (bs <= 0f) bs = 1f;
                    backBone.localScale = new Vector3(w * bs, bs, bs);
                }

                MelonLogger.Msg("[WideTyres] Width -> " + w + "x (level " + Level + ")");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[WideTyres] Apply: " + ex.Message);
            }
        }

        private static void ResetBones()
        {
            try
            {
                Transform frontBone, backBone;
                if (!GetBones(out frontBone, out backBone)) return;
                // Preserve wheel size scale (Y) when resetting width — only reset X back to match Y/Z
                if ((object)frontBone != null)
                {
                    float bs = frontBone.localScale.y;
                    if (bs <= 0f) bs = 1f;
                    frontBone.localScale = new Vector3(bs, bs, bs);
                }
                if ((object)backBone != null)
                {
                    float bs = backBone.localScale.y;
                    if (bs <= 0f) bs = 1f;
                    backBone.localScale = new Vector3(bs, bs, bs);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[WideTyres] ResetBones: " + ex.Message);
            }
        }

        private static bool GetBones(out Transform frontBone, out Transform backBone)
        {
            frontBone = null;
            backBone = null;

            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null)
            {
                MelonLogger.Warning("[WideTyres] Player_Human not found.");
                return false;
            }

            Transform bikeModel = player.transform.Find("BikeModel");
            if ((object)bikeModel == null)
            {
                MelonLogger.Warning("[WideTyres] BikeModel not found.");
                return false;
            }

            BikeAnimation bikeAnim = bikeModel.GetComponent<BikeAnimation>();
            if ((object)bikeAnim != null)
            {
                // Cache field references on first call
                if ((object)_backBoneField == null || (object)_frontBoneField == null)
                {
                    FieldInfo[] fields = bikeAnim.GetType().GetFields(
                        BindingFlags.Public | BindingFlags.Instance);

                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (!string.Equals(fields[i].FieldType.Name, "Transform",
                            System.StringComparison.Ordinal)) continue;

                        Transform t = fields[i].GetValue(bikeAnim) as Transform;
                        if ((object)t == null) continue;

                        if (string.Equals(t.name, "backWheel_Jnt", System.StringComparison.Ordinal))
                        { _backBoneField = fields[i]; MelonLogger.Msg("[WideTyres] Found back bone: " + fields[i].Name); }
                        else if (string.Equals(t.name, "frontWheel_Jnt", System.StringComparison.Ordinal))
                        { _frontBoneField = fields[i]; MelonLogger.Msg("[WideTyres] Found front bone: " + fields[i].Name); }
                    }
                }

                if ((object)_backBoneField != null)
                    backBone = _backBoneField.GetValue(bikeAnim) as Transform;
                if ((object)_frontBoneField != null)
                    frontBone = _frontBoneField.GetValue(bikeAnim) as Transform;
            }
            else
            {
                // Fallback: navigate the hierarchy directly
                frontBone = bikeModel.Find("root_Jnt/Frame_Jnt/steer_Jnt/forkShockAbsorber_Jnt/frontWheel_Jnt");
                backBone = bikeModel.Find("root_Jnt/Frame_Jnt/backWheelRotator_Jnt/BackWheelShockAbsorber_Jnt/backWheel_Jnt");
            }

            return true;
        }

        public static void Reset()
        {
            Enabled = false;
            Level = 5;
            // Do NOT call ResetBones() here - Player_Human is already destroyed on scene unload.
            // Bones are part of the destroyed scene so they don't need resetting.
            // Just clear the cached field refs so they get re-resolved in the new scene.
            _backBoneField = null;
            _frontBoneField = null;
        }
    }
}