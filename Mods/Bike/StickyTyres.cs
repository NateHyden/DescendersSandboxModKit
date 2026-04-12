using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Pushes the bike into whatever surface is beneath it.
    // Uses a raycast along the bike's local-down axis so it works on
    // floors, slopes, walls and ceilings — anywhere the bike is oriented.
    public static class StickyTyres
    {
        public static bool Enabled { get; private set; } = false;

        // How hard to push into the surface (Newtons, continuous force).
        // 150 = good grip on ground/slopes.
        // 500+ needed to hold on walls. 1000+ for ceilings.
        public static float SuctionForce = 150f;

        // Maximum distance the raycast will detect a surface.
        private const float RayDistance = 2.5f;

        private static Rigidbody _rb = null;

        public static void Toggle()
        {
            Enabled = !Enabled;
            MelonLogger.Msg("[StickyTyres] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void FixedTick()
        {
            if (!Enabled) return;

            try
            {
                // Cache rigidbody
                if ((object)_rb == null)
                {
                    GameObject player = GameObject.Find("Player_Human");
                    if ((object)player == null) return;
                    _rb = player.GetComponentInChildren<Rigidbody>();
                }
                if ((object)_rb == null) return;

                // Cast a ray from the bike's centre in its local-down direction.
                // This automatically adapts to the bike's current orientation —
                // tilted on a slope, pointing sideways on a wall, etc.
                Vector3 origin = _rb.position;
                Vector3 localDown = -_rb.transform.up;

                RaycastHit hit;
                if (Physics.Raycast(origin, localDown, out hit, RayDistance))
                {
                    // Push along the surface's own normal (INTO the surface).
                    // Using hit.normal ensures the force is always perpendicular
                    // to the actual geometry rather than just local-down.
                    _rb.AddForce(-hit.normal * SuctionForce, ForceMode.Force);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[StickyTyres] FixedTick: " + ex.Message);
            }
        }

        public static void Reset()
        {
            Enabled = false;
            _rb = null;
        }
    }
}