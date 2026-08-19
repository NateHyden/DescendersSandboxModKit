using System;
using System.IO;
using MelonLoader;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    public static class TopSpeed
    {
        public static float SessionTopSpeed { get; private set; } = 0f;
        private static bool _tracking = false;

        private static float _lastSaveTime = -999f;

        private static GameObject _cachedPlayer = null;
        private static Rigidbody _cachedRb = null;

        private static readonly string SaveFolder =
            Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData"), "DescendersModMenu");
        private static readonly string SaveFile =
            Path.Combine(SaveFolder, "TopSpeed.txt");

        public static string DisplayValue
        {
            get { return SessionTopSpeed > 0.1f ? SessionTopSpeed.ToString("F1") + " km/h" : "--"; }
        }

        public static void StartTracking() { _tracking = true; }
        public static void StopTracking() { _tracking = false; }

        public static void ResetSession()
        {
            SessionTopSpeed = 0f;
            _cachedPlayer = null;
            _cachedRb = null;
        }

        public static void Reset()
        {
            ResetSession();
            Save();
        }

        public static void ClearCache()
        {
            _cachedPlayer = null;
            _cachedRb = null;
        }

        public static void Tick()
        {
            if (!_tracking) return;
            try
            {
                if (!UnityNull.Alive(_cachedPlayer) || !_cachedPlayer.activeInHierarchy)
                {
                    _cachedPlayer = GameObject.Find("Player_Human");
                    _cachedRb = null;
                }
                if (!UnityNull.Alive(_cachedPlayer)) return;

                if (!UnityNull.Alive(_cachedRb))
                    _cachedRb = _cachedPlayer.GetComponent<Rigidbody>();
                if (!UnityNull.Alive(_cachedRb)) return;

                float gravMag = Physics.gravity.magnitude;
                if (gravMag < 0.01f) gravMag = 17.5f;
                float speed = _cachedRb.velocity.magnitude * 3.6f / gravMag * 9.81f;

                if (speed > SessionTopSpeed)
                {
                    SessionTopSpeed = speed;
                    Save();
                }
            }
            catch { }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(SaveFile)) return;
                string txt = File.ReadAllText(SaveFile).Trim();
                float val;
                if (float.TryParse(txt, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out val))
                {
                    SessionTopSpeed = val;
                    ModLog.Debug("[TopSpeed] Loaded: " + val.ToString("F1") + " km/h");
                }
            }
            catch (Exception ex) { ModLog.Warn("[TopSpeed] Load: " + ex.Message); }
        }

        private static void Save()
        {
            float now = Time.realtimeSinceStartup;
            if (now - _lastSaveTime < 3f) return;
            _lastSaveTime = now;
            try
            {
                if (!Directory.Exists(SaveFolder))
                    Directory.CreateDirectory(SaveFolder);
                File.WriteAllText(SaveFile, SessionTopSpeed.ToString("F2",
                    System.Globalization.CultureInfo.InvariantCulture));
            }
            catch { }
        }
    }
}

