using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    /// <summary>
    /// Trick playback speed. Gesture.animationSpeed is NOT playback rate — the game
    /// uses duration = clip.length / animationSpeed. Changing that alone only
    /// stretches the gesture timer (looks like a delay after the anim finishes).
    /// Real slow/fast needs:
    ///   1) animationSpeed scaled so gesture phases stay in sync with playback
    ///   2) live AnimationState.speed + Animator.speed while a gesture is active
    /// Level 5 = stock 1.0x.
    /// </summary>
    public static class TrickSpeed
    {
        public static int Level { get; private set; } = 5;

        public static bool IsModified { get { return Level != 5; } }

        public static float Multiplier { get { return Level * 0.2f; } }

        public static string LevelDisplay
        {
            get { return Multiplier.ToString("0.0") + "x"; }
        }

        private static FieldInfo _speedField;
        private static readonly Dictionary<int, float> _defaults = new Dictionary<int, float>();
        private static float _nextGestureRescan;
        private static bool _wasInGesture;
        private static Animator _riderAnimator;
        private static Animation _bikeAnimation;

        public static void Increase()
        {
            if (Level >= 10) return;
            bool was = IsModified;
            Level++;
            ApplyGestureDurations();
            ModLog.Dial("TrickSpeed", was, IsModified);
        }

        public static void Decrease()
        {
            if (Level <= 1) return;
            bool was = IsModified;
            Level--;
            ApplyGestureDurations();
            ModLog.Dial("TrickSpeed", was, IsModified);
        }

        public static void SetLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > 10) level = 10;
            bool was = IsModified;
            Level = level;
            ApplyGestureDurations();
            ModLog.Dial("TrickSpeed", was, IsModified);
        }

        public static void Reset()
        {
            bool was = IsModified;
            RestoreGestureDurations();
            _defaults.Clear();
            Level = 5;
            RestoreLivePlayback();
            _nextGestureRescan = 0f;
            _wasInGesture = false;
            _riderAnimator = null;
            _bikeAnimation = null;
            if (was) ModLog.Dial("TrickSpeed", true, false);
        }

        public static void ClearCache()
        {
            _defaults.Clear();
            _speedField = null;
            _nextGestureRescan = 0f;
            _wasInGesture = false;
            _riderAnimator = null;
            _bikeAnimation = null;
        }

        /// <summary>LateUpdate — apply playback after BikeAnimation.Update.</summary>
        public static void Tick()
        {
            try
            {
                if (!IsModified)
                {
                    if (_wasInGesture)
                    {
                        RestoreLivePlayback();
                        _wasInGesture = false;
                    }
                    return;
                }

                if (Time.unscaledTime >= _nextGestureRescan)
                {
                    _nextGestureRescan = Time.unscaledTime + 2f;
                    ApplyGestureDurations();
                }

                GameObject player = PlayerCache.PlayerHuman;
                if ((object)player == null) return;

                Cyclist cyclist = player.GetComponentInChildren<Cyclist>();
                bool inGesture = (object)cyclist != null && cyclist.GetGestureAmount() > 0.01f;

                if (inGesture)
                {
                    ApplyLivePlayback(Multiplier);
                    _wasInGesture = true;
                }
                else if (_wasInGesture)
                {
                    RestoreLivePlayback();
                    _wasInGesture = false;
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[TrickSpeed] Tick: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "TrickSpeed");
            }
        }

        /// <summary>
        /// Keeps gesture phase timers matched to playback: duration = clip.length / animationSpeed.
        /// </summary>
        private static void ApplyGestureDurations()
        {
            try
            {
                if (!EnsureSpeedField()) return;

                float mult = Multiplier;
                Gesture[] all = Resources.FindObjectsOfTypeAll<Gesture>();
                if ((object)all == null) return;

                for (int i = 0; i < all.Length; i++)
                {
                    Gesture g = all[i];
                    if ((object)g == null) continue;
                    int id = g.GetInstanceID();
                    float stock;
                    if (!_defaults.TryGetValue(id, out stock))
                    {
                        stock = (float)_speedField.GetValue(g);
                        if (stock <= 0.001f) stock = 1f;
                        _defaults[id] = stock;
                    }
                    float target = stock * mult;
                    float cur = (float)_speedField.GetValue(g);
                    if (Mathf.Abs(cur - target) > 0.0001f)
                        _speedField.SetValue(g, target);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[TrickSpeed] ApplyGestureDurations: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "TrickSpeed");
            }
        }

        private static void RestoreGestureDurations()
        {
            if (!EnsureSpeedField()) return;
            Gesture[] all = Resources.FindObjectsOfTypeAll<Gesture>();
            if ((object)all == null) return;
            for (int i = 0; i < all.Length; i++)
            {
                Gesture g = all[i];
                if ((object)g == null) continue;
                float stock;
                if (_defaults.TryGetValue(g.GetInstanceID(), out stock))
                    _speedField.SetValue(g, stock);
            }
        }

        private static bool EnsureSpeedField()
        {
            if ((object)_speedField != null) return true;
            _speedField = typeof(Gesture).GetField("animationSpeed",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if ((object)_speedField == null)
            {
                ModLog.Warn("[TrickSpeed] Gesture.animationSpeed field not found.");
                return false;
            }
            return true;
        }

        private static void ApplyLivePlayback(float mult)
        {
            CacheAnimComponents();

            if (UnityNull.Alive(_bikeAnimation))
            {
                foreach (AnimationState st in _bikeAnimation)
                {
                    if ((object)st == null) continue;
                    if (!st.enabled) continue;
                    st.speed = mult;
                }
            }

            if (UnityNull.Alive(_riderAnimator))
                _riderAnimator.speed = mult;
        }

        private static void RestoreLivePlayback()
        {
            if (UnityNull.Alive(_bikeAnimation))
            {
                foreach (AnimationState st in _bikeAnimation)
                {
                    if ((object)st == null) continue;
                    st.speed = 1f;
                }
            }

            if (UnityNull.Alive(_riderAnimator))
                _riderAnimator.speed = 1f;
        }

        private static void CacheAnimComponents()
        {
            if (UnityNull.Alive(_bikeAnimation) && UnityNull.Alive(_riderAnimator))
                return;

            GameObject player = PlayerCache.PlayerHuman;
            if ((object)player == null) return;

            if (!UnityNull.Alive(_bikeAnimation))
            {
                BikeAnimation bikeAnim = player.GetComponentInChildren<BikeAnimation>();
                if ((object)bikeAnim != null)
                {
                    _bikeAnimation = bikeAnim.GetComponent<Animation>();
                    if (!UnityNull.Alive(_bikeAnimation))
                        _bikeAnimation = bikeAnim.GetComponentInChildren<Animation>();
                }
            }

            if (!UnityNull.Alive(_riderAnimator))
            {
                CyclistModel model = player.GetComponentInChildren<CyclistModel>();
                if ((object)model != null)
                {
                    _riderAnimator = model.GetComponent<Animator>();
                    if (!UnityNull.Alive(_riderAnimator))
                        _riderAnimator = model.GetComponentInChildren<Animator>();
                }
                if (!UnityNull.Alive(_riderAnimator))
                    _riderAnimator = player.GetComponentInChildren<Animator>();
            }
        }
    }
}
