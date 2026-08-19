using MelonLoader;
using DescendersModMenu;
using System.Reflection;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class WideTyres
    {
        public static bool Enabled { get; private set; } = false;

        public static int Level { get; private set; } = 5;
        private static readonly float[] WidthScales = {
            0.2f, 0.4f, 0.6f, 0.8f, 1.0f, 1.4f, 1.8f, 2.2f, 2.6f, 3.0f,
            3.5f, 4.0f, 4.5f, 5.0f, 5.5f, 6.5f, 7.5f, 8.5f, 9.5f, 10.0f
        };
        public static float Width { get { return WidthScales[Level - 1]; } }

        private static FieldInfo _backBoneField = null;
        private static FieldInfo _frontBoneField = null;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (!Enabled)
            {
                ResetBones();
                _tickFront = null;
                _tickBack = null;
            }
            else
                Apply();
            ModLog.Feedback("[WideTyres] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void Increase()
        {
            if (Level < 20)
            {
                Level++;
                ModLog.Feedback("[WideTyres] Increase -> Level " + Level + " (" + Width + "x)");
                Apply();
            }
        }

        public static void Decrease()
        {
            if (Level > 1)
            {
                Level--;
                ModLog.Feedback("[WideTyres] Decrease -> Level " + Level + " (" + Width + "x)");
                Apply();
            }
        }

        public static void SetLevel(int v) { Level = System.Math.Max(1, System.Math.Min(20, v)); }

        public static void Tick()
        {
            if (!Enabled) return;
            try
            {
                if ((object)_tickFront == null || (object)_tickBack == null
                    || _tickFront == null || _tickBack == null)
                {
                    Transform frontBone, backBone;
                    if (!GetBones(out frontBone, out backBone)) return;
                    _tickFront = frontBone;
                    _tickBack = backBone;
                }

                float w = Width;
                if ((object)_tickFront != null && _tickFront != null)
                {
                    float bs = _tickFront.localScale.y;
                    if (bs <= 0f) bs = 1f;
                    _tickFront.localScale = new Vector3(w * bs, bs, bs);
                }
                if ((object)_tickBack != null && _tickBack != null)
                {
                    float bs = _tickBack.localScale.y;
                    if (bs <= 0f) bs = 1f;
                    _tickBack.localScale = new Vector3(w * bs, bs, bs);
                }
            }
            catch { }
        }

        private static Transform _tickFront;
        private static Transform _tickBack;
        public static void Apply()
        {
            try
            {
                Transform frontBone, backBone;
                if (!GetBones(out frontBone, out backBone)) return;

                float w = Width;
                if (UnityNull.Alive(frontBone))
                {
                    float bs = frontBone.localScale.y;
                    if (bs <= 0f) bs = 1f;
                    frontBone.localScale = new Vector3(w * bs, bs, bs);
                }
                if (UnityNull.Alive(backBone))
                {
                    float bs = backBone.localScale.y;
                    if (bs <= 0f) bs = 1f;
                    backBone.localScale = new Vector3(w * bs, bs, bs);
                }

                ModLog.Feedback("[WideTyres] Width -> " + w + "x (level " + Level + ")");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[WideTyres] Apply: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "WideTyres");
            }
        }

        private static void ResetBones()
        {
            try
            {
                Transform frontBone, backBone;
                if (!GetBones(out frontBone, out backBone)) return;
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
                Telemetry.ReportErrorAsync(ex, "WideTyres");
            }
        }

        private static bool GetBones(out Transform frontBone, out Transform backBone)
        {
            frontBone = null;
            backBone = null;

            GameObject player = PlayerCache.PlayerHuman;
            if ((object)player == null)
            {
                ModLog.Warn("[WideTyres] Player_Human not found.");
                return false;
            }

            Transform bikeModel = player.transform.Find("BikeModel");
            if ((object)bikeModel == null)
            {
                ModLog.Warn("[WideTyres] BikeModel not found.");
                return false;
            }

            BikeAnimation bikeAnim = bikeModel.GetComponent<BikeAnimation>();
            if ((object)bikeAnim != null)
            {
                if ((object)_backBoneField == null || (object)_frontBoneField == null)
                {
                    FieldInfo[] fields = bikeAnim.GetType().GetFields(
                        BindingFlags.Public | BindingFlags.Instance);

                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (!string.Equals(fields[i].FieldType.Name, "Transform",
                            System.StringComparison.Ordinal)) continue;

                        Transform t = fields[i].GetValue(bikeAnim) as Transform;
                        if (!UnityNull.Alive(t)) continue;

                        if (string.Equals(t.name, "backWheel_Jnt", System.StringComparison.Ordinal))
                        { _backBoneField = fields[i]; ModLog.Debug("[WideTyres] Found back bone: " + fields[i].Name); }
                        else if (string.Equals(t.name, "frontWheel_Jnt", System.StringComparison.Ordinal))
                        { _frontBoneField = fields[i]; ModLog.Debug("[WideTyres] Found front bone: " + fields[i].Name); }
                    }
                }

                if ((object)_backBoneField != null)
                    backBone = _backBoneField.GetValue(bikeAnim) as Transform;
                if ((object)_frontBoneField != null)
                    frontBone = _frontBoneField.GetValue(bikeAnim) as Transform;
            }
            else
            {
                frontBone = bikeModel.Find("root_Jnt/Frame_Jnt/steer_Jnt/forkShockAbsorber_Jnt/frontWheel_Jnt");
                backBone = bikeModel.Find("root_Jnt/Frame_Jnt/backWheelRotator_Jnt/BackWheelShockAbsorber_Jnt/backWheel_Jnt");
            }

            if (!UnityNull.Alive(frontBone)) frontBone = null;
            if (!UnityNull.Alive(backBone)) backBone = null;

            return true;
        }

        public static void Reset()
        {
            Enabled = false;
            Level = 5;
            _backBoneField = null;
            _frontBoneField = null;
            _tickFront = null;
            _tickBack = null;
        }
    }
}
