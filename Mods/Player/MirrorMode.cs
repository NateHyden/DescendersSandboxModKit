using MelonLoader;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    public static class MirrorMode
    {
        public static bool Enabled { get; private set; } = false;
        private static Camera _lastCam = null;
        private static bool _weEnabledReverseSteer = false;

        public static void Toggle()
        {
            Enabled = !Enabled;

            if (Enabled)
            {
                if (!ReverseSteering.Enabled)
                {
                    ReverseSteering.Toggle();
                    _weEnabledReverseSteer = true;
                }
                else
                    _weEnabledReverseSteer = false;
            }
            else
            {
                Restore();
                if (_weEnabledReverseSteer && ReverseSteering.Enabled)
                    ReverseSteering.Toggle();
                _weEnabledReverseSteer = false;
            }

            ModLog.Feedback("[MirrorMode] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void Tick()
        {
            if (!Enabled) return;
            Camera cam = Camera.main;
            if (!UnityNull.Alive(cam)) return;

            if (UnityNull.Alive(_lastCam) && (object)_lastCam != (object)cam)
                try { _lastCam.ResetProjectionMatrix(); } catch { }

            if ((object)_lastCam != (object)cam)
            {
                FlipCamera(cam);
                _lastCam = cam;
            }
        }

        private static void FlipCamera(Camera cam)
        {
            Matrix4x4 m = cam.projectionMatrix;
            m[0, 0] = -m[0, 0]; m[0, 1] = -m[0, 1];
            m[0, 2] = -m[0, 2]; m[0, 3] = -m[0, 3];
            cam.projectionMatrix = m;
            GL.invertCulling = true;
        }

        private static void Restore()
        {
            GL.invertCulling = false;
            try
            {
                if (UnityNull.Alive(_lastCam)) _lastCam.ResetProjectionMatrix();
                Camera cam = Camera.main;
                if (UnityNull.Alive(cam) && (object)cam != (object)_lastCam)
                    cam.ResetProjectionMatrix();
            }
            catch { }
            _lastCam = null;
        }

        public static void Reset()
        {
            if (_weEnabledReverseSteer && ReverseSteering.Enabled)
                ReverseSteering.Toggle();
            _weEnabledReverseSteer = false;
            Enabled = false;
            GL.invertCulling = false;
            try { Camera cam = Camera.main; if (UnityNull.Alive(cam)) cam.ResetProjectionMatrix(); } catch { }
            _lastCam = null;
        }
    }
}