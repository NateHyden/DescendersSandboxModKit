using MelonLoader;
using UnityEngine;
using DescendersModMenu;

namespace DescendersModMenu.Mods
{
    // Full-screen black from bail until the player actually respawns (B).
    // A dedicated camera holds the black so BikeCamera switches cannot
    // punch through. Game RespawnOnTrack fires during camera/bail setup —
    // those are ignored for a couple of seconds after the crash.
    public static class BlackDeath
    {
        public static bool Enabled { get; private set; } = false;
        public static bool IsActive { get; private set; }

        private const float IgnoreRespawnSeconds = 2.5f;

        private static Texture2D _tex;
        private static GameObject _camGo;
        private static Camera _cam;
        private static float _startedAt = -999f;

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (!Enabled) Hide();
            ModLog.Feedback("[BlackDeath] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void OnBail()
        {
            if (!Enabled) return;
            IsActive = true;
            _startedAt = Time.unscaledTime;
            EnsureCam();
        }

        // Real respawn after lying on the ground. Camera-mode changes also
        // hit this patch, so ignore anything too soon after the bail.
        public static void OnRespawn()
        {
            if (!IsActive) return;
            if (Time.unscaledTime - _startedAt < IgnoreRespawnSeconds) return;
            Hide();
        }

        public static void Tick()
        {
            if (!IsActive) return;
            EnsureCam();
            if (PressedRespawn()) Hide();
        }

        public static void Reset()
        {
            Enabled = false;
            Hide();
        }

        public static void Draw()
        {
            if (!IsActive) return;
            if (!_tex)
            {
                _tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _tex.SetPixel(0, 0, Color.white);
                _tex.Apply();
            }
            Color prev = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _tex, ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        private static bool PressedRespawn()
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton1)) return true;
            if (Input.GetKeyDown(KeyCode.Joystick1Button1)) return true;
            if (Input.GetKeyDown(KeyCode.B)) return true;
            if (Input.GetKeyDown(KeyCode.Escape)) return true;
            if (Input.GetKeyDown(KeyCode.Backspace)) return true;
            try
            {
                if (Input.GetButtonDown("Cancel")) return true;
            }
            catch { }
            return false;
        }

        private static void EnsureCam()
        {
            if (!_cam)
            {
                if (_camGo) Object.Destroy(_camGo);
                _camGo = new GameObject("BlackDeathCam");
                Object.DontDestroyOnLoad(_camGo);
                _cam = _camGo.AddComponent<Camera>();
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = Color.black;
                _cam.cullingMask = 0;
                _cam.nearClipPlane = 0.1f;
                _cam.farClipPlane = 1f;
                _cam.orthographic = true;
                _cam.enabled = true;
            }
            _cam.depth = 500f;
            if (!_cam.enabled) _cam.enabled = true;
        }

        private static void Hide()
        {
            IsActive = false;
            if (_camGo)
            {
                Object.Destroy(_camGo);
                _camGo = null;
            }
            _cam = null;
        }
    }
}
