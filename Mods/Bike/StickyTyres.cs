using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class StickyTyres
    {
        public static bool Enabled { get; private set; } = false;

        public static float SuctionForce = 150f;

        private const float RayDistance = 2.5f;

        private static Rigidbody _rb = null;

        public static void Toggle()
        {
            Enabled = !Enabled;
            ModLog.Feedback("[StickyTyres] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void FixedTick()
        {
            if (!Enabled) return;

            try
            {
                if (!UnityNull.Alive(_rb))
                {
                    _rb = null;
                    GameObject player = GameObject.Find("Player_Human");
                    if ((object)player == null) return;
                    _rb = player.GetComponentInChildren<Rigidbody>();
                }
                if (!UnityNull.Alive(_rb)) return;

                Vector3 origin = _rb.position;
                Vector3 localDown = -_rb.transform.up;

                RaycastHit hit;
                if (Physics.Raycast(origin, localDown, out hit, RayDistance))
                {
                    _rb.AddForce(-hit.normal * SuctionForce, ForceMode.Force);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[StickyTyres] FixedTick: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "StickyTyres");
            }
        }

        public static void Reset()
        {
            Enabled = false;
            _rb = null;
        }
    }
}

