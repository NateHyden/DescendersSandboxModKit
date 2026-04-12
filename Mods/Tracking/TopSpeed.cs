using System;
using System.IO;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class TopSpeed
    {
        public static float SessionTopSpeed { get; private set; } = 0f;
        private static bool _tracking = false;

        // Cached refs — avoid GameObject.Find every frame
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

        public static void Reset()
        {
            SessionTopSpeed = 0f;
            _cachedPlayer = null;
            _cachedRb = null;
            Save();
        }

        public static void ClearCache()
        {
            _cachedPlayer = null;
            _cachedRb = null;
        }

        // Call from OnUpdate every frame
        public static void Tick()
        {
            if (!_tracking) return;
            try
            {
                // Re-find player if cache is stale
                if ((object)_cachedPlayer == null || !_cachedPlayer.activeInHierarchy)
                {
                    _cachedPlayer = GameObject.Find("Player_Human");
                    _cachedRb = null;
                }
                if ((object)_cachedPlayer == null) return;

                if ((object)_cachedRb == null)
                    _cachedRb = _cachedPlayer.GetComponent<Rigidbody>();
                if ((object)_cachedRb == null) return;

                // Match the game's speedo formula exactly:
                // velocity.magnitude * 3.6 / gravity.magnitude * 9.81
                // At default gravity (-17.5) this equals roughly magnitude * 2.018
                float gravMag = Physics.gravity.magnitude;
                if (gravMag < 0.01f) gravMag = 17.5f; // safety
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
                    MelonLogger.Msg("[TopSpeed] Loaded: " + val.ToString("F1") + " km/h");
                }
            }
            catch (Exception ex) { MelonLogger.Warning("[TopSpeed] Load: " + ex.Message); }
        }

        private static void Save()
        {
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