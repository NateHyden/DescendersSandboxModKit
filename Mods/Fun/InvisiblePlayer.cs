using MelonLoader;
using DescendersModMenu;
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
            ModLog.Feedback("[InvisiblePlayer] -> " + (Enabled ? "ON" : "OFF"));
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
                if (!UnityNull.Alive(player)) return;
                Transform cyclist = player.transform.Find("Cyclist");
                if (!UnityNull.Alive(cyclist)) return;
                if (invisible)
                {
                    Renderer[] all = cyclist.GetComponentsInChildren<Renderer>(true);
                    var toHide = new System.Collections.Generic.List<Renderer>();
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (!UnityNull.Alive(all[i])) continue;
                        if (all[i].enabled) toHide.Add(all[i]);
                    }
                    _hiddenRenderers = toHide.ToArray();
                    for (int i = 0; i < _hiddenRenderers.Length; i++)
                    {
                        if (UnityNull.Alive(_hiddenRenderers[i]))
                            _hiddenRenderers[i].enabled = false;
                    }
                }
                else
                {
                    if ((object)_hiddenRenderers != null)
                    {
                        for (int i = 0; i < _hiddenRenderers.Length; i++)
                        {
                            if (UnityNull.Alive(_hiddenRenderers[i]))
                                _hiddenRenderers[i].enabled = true;
                        }
                        _hiddenRenderers = null;
                    }
                }
            }
            catch (System.Exception ex) { MelonLogger.Error("[InvisiblePlayer] Apply: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "InvisiblePlayer"); }
        }
    }
}
