using UnityEngine;

namespace DescendersModMenu.Mods
{
    internal static class PlayerCache
    {
        private static GameObject _player;
        private static float _nextRefresh;
        private const float RefreshInterval = 1.0f;

        public static GameObject PlayerHuman
        {
            get
            {
                float now = Time.unscaledTime;
                if ((object)_player != null && _player != null && now < _nextRefresh)
                    return _player;

                _player = GameObject.Find("Player_Human");
                _nextRefresh = now + RefreshInterval;
                return _player;
            }
        }

        public static void Clear()
        {
            _player = null;
            _nextRefresh = 0f;
        }
    }
}

