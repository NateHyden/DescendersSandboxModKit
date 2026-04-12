using MelonLoader;
using UnityEngine;
using System.Reflection;

namespace DescendersModMenu.Mods
{
    // Air Control — damps angular velocity while both wheels are airborne.
    // Makes the bike more stable and controllable in the air.
    // Level 1 = very light damping, Level 10 = strong stabilisation.
    public static class AirControl
    {
        public static bool Enabled { get; private set; } = false;
        public static int Level { get; private set; } = 5;

        // Lerp strength toward zero angular velocity per second
        // Level 1 = 0.5, Level 10 = 5.0
        private static float DampStrength { get { return Mathf.Lerp(0.5f, 5f, (Level - 1) / 9f); } }
        public static string DisplayValue { get { return Level.ToString(); } }

        private static PropertyInfo _rbProp = null;
        private static PropertyInfo _vehicleGroundedProp = null; // TDEX{ib on Vehicle
        private static bool _cached = false;

        public static void Toggle()
        {
            Enabled = !Enabled;
            MelonLogger.Msg("[AirControl] -> " + (Enabled ? "ON (level " + Level + ")" : "OFF"));
        }

        public static void Increase() { if (Level < 10) Level++; }
        public static void Decrease() { if (Level > 1) Level--; }
        public static void SetLevel(int level) { Level = Mathf.Clamp(level, 1, 10); }

        public static void FixedTick()
        {
            if (!Enabled) return;
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) return;

                Vehicle vehicle = player.GetComponent<Vehicle>();
                if ((object)vehicle == null) return;

                if (!_cached) CacheRefs(vehicle);

                // Only act when vehicle is fully airborne
                bool onGround = false;
                if ((object)_vehicleGroundedProp != null)
                    onGround = (bool)_vehicleGroundedProp.GetValue(vehicle, null);
                if (onGround) return;

                Rigidbody rb = null;
                if ((object)_rbProp != null)
                    rb = _rbProp.GetValue(vehicle, null) as Rigidbody;
                if ((object)rb == null) return;

                // Smoothly damp angular velocity toward zero
                // This slows rotation in the air making it controllable
                rb.angularVelocity = Vector3.Lerp(
                    rb.angularVelocity,
                    Vector3.zero,
                    DampStrength * Time.fixedDeltaTime);
            }
            catch { }
        }

        private static void CacheRefs(Vehicle v)
        {
            _cached = true;

            PropertyInfo[] props = typeof(Vehicle).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < props.Length; i++)
            {
                if (!props[i].CanRead) continue;
                if (props[i].PropertyType.Equals(typeof(Rigidbody)) && (object)_rbProp == null)
                    _rbProp = props[i];
                // TDEX{ib starts with T — vehicle-level on-ground bool
                if (props[i].PropertyType.Equals(typeof(bool)) && props[i].Name.StartsWith("T") && (object)_vehicleGroundedProp == null)
                    _vehicleGroundedProp = props[i];
                if ((object)_rbProp != null && (object)_vehicleGroundedProp != null) break;
            }
        }

        public static void Reset()
        {
            Enabled = false;
            _cached = false;
            _rbProp = null;
            _vehicleGroundedProp = null;
        }
    }
}