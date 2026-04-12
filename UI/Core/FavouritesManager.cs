using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    // ── Custom delegates — Func<T> and Action (no params) are in System.Core which Unity 2017 Mono doesn't load ──
    public delegate bool FavBoolGetter();
    public delegate int FavIntGetter();
    public delegate float FavFloatGetter();
    public delegate string FavStringGetter();
    public delegate void FavAction();

    public struct ModFavEntry
    {
        public string Id;
        public string DisplayName;
        public string TabBadge;
        public Action<Transform> BuildControls;
        public FavBoolGetter IsActive;
    }

    public static class FavouritesManager
    {
        // ── Registry — populated during page CreatePage() ─────────────
        private static readonly Dictionary<string, ModFavEntry> _registry =
            new Dictionary<string, ModFavEntry>();

        // ── Favourites list — persisted to JSON ───────────────────────
        private static readonly Dictionary<string, byte> _favourites = new Dictionary<string, byte>();
        private static readonly List<string> _orderedFavs = new List<string>();

        // ── Star button refs (cleared on menu rebuild) ────────────────
        private static readonly Dictionary<string, Button> _starButtons =
            new Dictionary<string, Button>();

        // ── Refresh callbacks — populated during Rebuild ──────────────
        private static readonly Dictionary<string, FavAction> _refreshCallbacks =
            new Dictionary<string, FavAction>();

        // ── File path ─────────────────────────────────────────────────
        private static string FilePath
        {
            get
            {
                string dir = Path.Combine(
                    Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "UserData"),
                    "DescendersModMenu");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, "Favourites.json");
            }
        }

        // ── Registration ──────────────────────────────────────────────
        public static void Register(ModFavEntry entry)
        {
            _registry[entry.Id] = entry;
        }

        public static void RegisterStarButton(string id, Button btn)
        {
            _starButtons[id] = btn;
        }

        public static void ClearStarButtons()
        {
            _starButtons.Clear();
        }

        // ── Refresh callbacks ─────────────────────────────────────────
        public static void RegisterRefresh(string id, FavAction refresh)
        {
            _refreshCallbacks[id] = refresh;
        }

        public static void ClearRefreshCallbacks()
        {
            _refreshCallbacks.Clear();
        }

        public static void InvokeRefresh()
        {
            foreach (var kv in _refreshCallbacks)
            {
                try { kv.Value(); }
                catch { } // Silently ignore — callbacks may reference destroyed UI during scene transitions
            }
        }

        // ── Star button colour sync ───────────────────────────────────
        public static void RefreshAllStars()
        {
            foreach (var kv in _starButtons)
            {
                if ((object)kv.Value == null) continue;
                UIHelpers.SetStarActive(kv.Value, IsFavourited(kv.Key));
            }
        }

        // ── Queries ───────────────────────────────────────────────────
        public static bool IsFavourited(string id) { return _favourites.ContainsKey(id); }

        public static List<string> GetAll() { return new List<string>(_orderedFavs); }

        public static bool TryGetEntry(string id, out ModFavEntry entry)
        {
            return _registry.TryGetValue(id, out entry);
        }

        public static bool IsAnyActive
        {
            get
            {
                foreach (var id in _orderedFavs)
                {
                    ModFavEntry e;
                    if (_registry.TryGetValue(id, out e) && e.IsActive != null)
                    {
                        try { if (e.IsActive()) return true; } catch { }
                    }
                }
                return false;
            }
        }

        // ── Toggle ────────────────────────────────────────────────────
        public static void Toggle(string id)
        {
            if (_favourites.ContainsKey(id))
            {
                _favourites.Remove(id);
                _orderedFavs.Remove(id);
                MelonLogger.Msg("[Favs] Removed: " + id);
            }
            else
            {
                _favourites[id] = 0;
                _orderedFavs.Add(id);
                MelonLogger.Msg("[Favs] Added: " + id);
            }
            SaveToFile();
            RefreshAllStars();
            FavsPage.MarkDirty();
        }

        // ── Clear All ─────────────────────────────────────────────────
        public static void ClearAll()
        {
            _favourites.Clear();
            _orderedFavs.Clear();
            MelonLogger.Msg("[Favs] Cleared all favourites.");
            SaveToFile();
            RefreshAllStars();
            FavsPage.MarkDirty();
        }

        // ── Load / Save ───────────────────────────────────────────────
        public static void LoadFromFile()
        {
            _favourites.Clear();
            _orderedFavs.Clear();
            try
            {
                string path = FilePath;
                MelonLogger.Msg("[Favs] Load path: " + path);
                if (!File.Exists(path))
                {
                    MelonLogger.Msg("[Favs] No saved favourites file — starting empty.");
                    return;
                }
                string json = File.ReadAllText(path);
                MelonLogger.Msg("[Favs] Read " + json.Length + " chars from file.");
                var ids = ParseJsonArray(json);
                MelonLogger.Msg("[Favs] Parsed " + ids.Count + " IDs from JSON.");
                foreach (string id in ids)
                {
                    if (!string.IsNullOrEmpty(id) && !_favourites.ContainsKey(id))
                    {
                        _favourites[id] = 0;
                        _orderedFavs.Add(id);
                        MelonLogger.Msg("[Favs]   -> " + id);
                    }
                }
                MelonLogger.Msg("[Favs] Loaded " + _orderedFavs.Count + " favourites.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Favs] LoadFromFile failed: " + ex.Message);
            }
        }

        public static void SaveToFile()
        {
            try
            {
                string path = FilePath;
                var sb = new System.Text.StringBuilder();
                sb.Append("{\n  \"favourites\": [");
                for (int i = 0; i < _orderedFavs.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append("\n    \"").Append(EscapeJson(_orderedFavs[i])).Append("\"");
                }
                if (_orderedFavs.Count > 0) sb.Append("\n  ");
                sb.Append("]\n}");
                File.WriteAllText(path, sb.ToString());
                MelonLogger.Msg("[Favs] Saved " + _orderedFavs.Count + " favourites to: " + path);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Favs] SaveToFile failed: " + ex.Message);
            }
        }

        // ── Minimal JSON helpers ──────────────────────────────────────
        private static List<string> ParseJsonArray(string json)
        {
            var result = new List<string>();
            // Find "favourites" array
            int arrStart = json.IndexOf('[');
            int arrEnd = json.LastIndexOf(']');
            if (arrStart < 0 || arrEnd < 0 || arrEnd <= arrStart) return result;

            string inner = json.Substring(arrStart + 1, arrEnd - arrStart - 1);
            int i = 0;
            while (i < inner.Length)
            {
                int q1 = inner.IndexOf('"', i);
                if (q1 < 0) break;
                int q2 = inner.IndexOf('"', q1 + 1);
                if (q2 < 0) break;
                result.Add(inner.Substring(q1 + 1, q2 - q1 - 1));
                i = q2 + 1;
            }
            return result;
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}