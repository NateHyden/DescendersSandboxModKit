using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Hold F10 while riding to record all changing Vehicle float fields to desktop
    // The fields that STOP changing when you hit the speed wall are the cap candidates
    public static class SpeedWatcher
    {
        private static readonly Dictionary<string, float> _prevValues
            = new Dictionary<string, float>();

        private static readonly Dictionary<string, List<string>> _changeLog
            = new Dictionary<string, List<string>>();

        private static bool _wasHeld = false;
        private static float _sessionStart = 0f;

        // Call this from OnUpdate every frame
        public static void CheckHotkey()
        {
            bool held = Input.GetKey(KeyCode.F10);

            if (held)
            {
                if (!_wasHeld)
                {
                    // Just started holding - begin session
                    _prevValues.Clear();
                    _changeLog.Clear();
                    _sessionStart = Time.unscaledTime;
                    MelonLogger.Msg("[SpeedWatcher] Recording started - release F10 to save.");
                }

                // Record this frame
                RecordFrame();
                _wasHeld = true;
            }
            else if (_wasHeld)
            {
                // Just released - write to file
                _wasHeld = false;
                SaveToFile();
            }
        }

        private static void RecordFrame()
        {
            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) return;

            Vehicle vehicle = player.GetComponent<Vehicle>();
            if ((object)vehicle == null) return;

            float elapsed = Time.unscaledTime - _sessionStart;

            // Get current speed for context
            Rigidbody rb = vehicle.GetComponent<Rigidbody>();
            float speed = (object)rb != null ? rb.velocity.magnitude * 3.6f : 0f; // m/s to km/h

            FieldInfo[] fields = vehicle.GetType().GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if ((object)field == null) continue;
                if (!string.Equals(field.FieldType.Name, "Single", StringComparison.Ordinal)) continue;

                object raw;
                try { raw = field.GetValue(vehicle); }
                catch { continue; }

                if (!(raw is float)) continue;
                float current = (float)raw;

                string key = field.Name;

                if (_prevValues.ContainsKey(key))
                {
                    float prev = _prevValues[key];
                    float diff = Mathf.Abs(current - prev);

                    if (diff > 0.001f)
                    {
                        if (!_changeLog.ContainsKey(key))
                            _changeLog[key] = new List<string>();

                        _changeLog[key].Add(
                            string.Format("t={0:F2}s  speed={1:F1}km/h  value={2:F5}  delta={3:F5}",
                                elapsed, speed, current, current - prev)
                        );
                    }

                    _prevValues[key] = current;
                }
                else
                {
                    _prevValues.Add(key, current);
                }
            }
        }

        private static void SaveToFile()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string path = Path.Combine(desktop, "SpeedWatcher_" + timestamp + ".txt");

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("=== SPEED WATCHER DUMP ===");
                sb.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("Hold F10 while riding, release to save.");
                sb.AppendLine("Fields that STOP appearing near your speed cap are the limiter.");
                sb.AppendLine();

                if (_changeLog.Count == 0)
                {
                    sb.AppendLine("No field changes recorded. Make sure you are moving.");
                }
                else
                {
                    sb.AppendLine("Fields that changed during recording (" + _changeLog.Count + " total):");
                    sb.AppendLine();

                    foreach (string key in _changeLog.Keys)
                    {
                        List<string> entries = _changeLog[key];
                        sb.AppendLine("── " + key + " (" + entries.Count + " changes) ──");
                        // Show last 10 changes per field so we can see what happens at top speed
                        int start = Mathf.Max(0, entries.Count - 10);
                        for (int i = start; i < entries.Count; i++)
                            sb.AppendLine("  " + entries[i]);
                        sb.AppendLine();
                    }
                }

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                MelonLogger.Msg("[SpeedWatcher] Saved to " + path);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[SpeedWatcher] Save failed: " + ex.Message);
            }
        }
    }
}
