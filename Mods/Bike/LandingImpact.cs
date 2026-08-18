using System.Reflection;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Landing Impact — raises the minimum impact speed required to trigger a bail.
    //
    // Root cause of why the old approach did nothing:
    //   PlayerInfoImpact.OnImpact  = health point loss (roguelite lives system).
    //   Cyclist.aQkwp...()         = actual physical bail trigger.
    //   These are two separate systems. We had patched the wrong one.
    //
    // The bail trigger in Cyclist checks:
    //   if (impactForce > Jk\u0080l\u007Fg\u007D && speed * 2f > cxW\u005Em\u005Bm) Bail();
    //   cxW\u005Em\u005Bm = 15f (default min bail speed)
    //
    // Raising cxW\u005Em\u005Bm means you need a much harder hit to fall off.
    // At level 10 the threshold is so high that normal riding never triggers it.

    public static class LandingImpact
    {
        public static bool Enabled { get; private set; } = false;
        public static int Level { get; private set; } = 5;

        // Level 1 = default (15f), Level 10 = 200f — effectively no bail
        private static float GetThreshold()
        {
            return Mathf.Lerp(15f, 200f, (Level - 1) / 9f);
        }
        public static string DisplayValue
        {
            get { return ((int)GetThreshold()).ToString(); }
        }

        private static readonly float DefaultThreshold = 15f;
        private static FieldInfo _threshField = null;
        private static Cyclist _cachedCyclist = null;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Apply(); else Restore();
            ModLog.Feedback("[LandingImpact] -> " + (Enabled ? "ON (threshold " + GetThreshold() + ")" : "OFF"));
        }

        public static void Increase() { if (Level < 10) Level++; if (Enabled) Apply(); }
        public static void Decrease() { if (Level > 1) Level--; if (Enabled) Apply(); }

        public static void SetLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > 10) level = 10;
            Level = level;
            if (Enabled) Apply();
        }

        public static void Apply()
        {
            if (!Enabled) return;
            try
            {
                Cyclist c = GetCyclist();
                if (!UnityNull.Alive(c)) return;
                FieldInfo f = GetField(c);
                if ((object)f == null) return;
                f.SetValue(c, GetThreshold());
                ModLog.Feedback("[LandingImpact] Bail threshold -> " + GetThreshold());
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[LandingImpact] Apply: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "LandingImpact");
            }
        }

        private static void Restore()
        {
            try
            {
                Cyclist c = GetCyclist();
                if (!UnityNull.Alive(c) || (object)_threshField == null) return;
                _threshField.SetValue(c, DefaultThreshold);
                ModLog.Debug("[LandingImpact] Restored default threshold: " + DefaultThreshold);
            }
            catch { }
        }

        // Same silent-overwrite race as Acceleration/MaxSpeed - re-enforce every
        // frame instead of applying once. Called from OnLateUpdate.
        public static void Tick()
        {
            if (!Enabled) return;
            try
            {
                Cyclist c = GetCyclist();
                if (!UnityNull.Alive(c)) return;
                FieldInfo f = GetField(c);
                if ((object)f == null) return;

                float target = GetThreshold();
                float current = (float)f.GetValue(c);
                if (Mathf.Abs(current - target) > 0.01f)
                    f.SetValue(c, target);
            }
            catch { }
        }

        public static void Reset()
        {
            if (Enabled) Restore();
            Enabled = false;
            _cachedCyclist = null;
            _threshField = null;
        }

        private static Cyclist GetCyclist()
        {
            if (UnityNull.Alive(_cachedCyclist)) return _cachedCyclist;
            _cachedCyclist = null;
            GameObject player = GameObject.Find("Player_Human");
            if (!UnityNull.Alive(player)) return null;
            _cachedCyclist = player.GetComponent<Cyclist>();
            return UnityNull.Alive(_cachedCyclist) ? _cachedCyclist : null;
        }

        private static FieldInfo GetField(Cyclist c)
        {
            if ((object)_threshField != null) return _threshField;

            // Strategy 1: known obfuscated name — confirmed from assembly decompile
            // cxW\u005Em\u005Bm = public float, default 15f, under [Header("Bailing")]
            _threshField = typeof(Cyclist).GetField("cxW\u005Em\u005Bm",
                BindingFlags.Public | BindingFlags.Instance);
            if ((object)_threshField != null)
            {
                ModLog.Debug("[LandingImpact] Found bail threshold field by name.");
                return _threshField;
            }

            // Strategy 2: scan for a public float whose current value is ~15f
            // (the default — only works reliably on first call before any modification)
            FieldInfo[] fields = typeof(Cyclist).GetFields(
                BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                if (!string.Equals(fields[i].FieldType.Name, "Single",
                    System.StringComparison.Ordinal)) continue;
                object val = fields[i].GetValue(c);
                if ((object)val == null) continue;
                float f = (float)val;
                if (f >= 14f && f <= 16f)
                {
                    _threshField = fields[i];
                    ModLog.Debug("[LandingImpact] Found bail threshold via scan: "
                        + fields[i].Name + " = " + f);
                    return _threshField;
                }
            }

            ModLog.Warn("[LandingImpact] Could not find bail threshold field.");
            return null;
        }
    }
}