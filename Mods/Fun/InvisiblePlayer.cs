using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class InvisiblePlayer
    {
        public static bool Enabled = false;
        private static Renderer[] _hiddenRenderers = null;

        public static void Toggle()
        {
            Enabled = !Enabled;
            Apply(Enabled);
        }

        public static void SetEnabled(bool v)
        {
            if (v != Enabled) { Enabled = v; Apply(v); }
        }

        private static void Apply(bool invisible)
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null) return;
                Transform cyclist = player.transform.Find("Cyclist");
                if ((object)cyclist == null) return;
                if (invisible)
                {
                    Renderer[] all = cyclist.GetComponentsInChildren<Renderer>(true);
                    var toHide = new System.Collections.Generic.List<Renderer>();
                    for (int i = 0; i < all.Length; i++) if (all[i].enabled) toHide.Add(all[i]);
                    _hiddenRenderers = toHide.ToArray();
                    for (int i = 0; i < _hiddenRenderers.Length; i++) _hiddenRenderers[i].enabled = false;
                }
                else
                {
                    if ((object)_hiddenRenderers != null)
                    {
                        for (int i = 0; i < _hiddenRenderers.Length; i++)
                            if ((object)_hiddenRenderers[i] != null) _hiddenRenderers[i].enabled = true;
                        _hiddenRenderers = null;
                    }
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[InvisiblePlayer] Apply: " + ex.Message); }
        }
    }
}
