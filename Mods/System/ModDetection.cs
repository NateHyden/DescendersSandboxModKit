using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;

namespace DescendersModMenu.Mods
{
    internal static class ModDetection
    {
        public const string PropKey = "DescMM";

        public static string ModVersion => BuildInfo.Version;

        private static Type _photonNetType;
        private static PropertyInfo _localPlayerProp;
        private static PropertyInfo _allPlayersProp;
        private static MethodInfo _setProps;
        private static PropertyInfo _propsProp;
        private static PropertyInfo _nickProp;
        private static bool _resolved;
        private static bool _tagged;
        private static float _nextScanTime;
        private const float ScanInterval = 2.5f;

        private static readonly string PhotonNetName = "upVWa\u0084E";
        private static readonly string LocalPlayerName = "gQ\u0060\u0083tus";
        private static readonly string AllPlayersName = "CoH\u007C\u007EDq";
        private static readonly string SetPropsName = "KxvEguU";
        private static readonly string CustomPropsName = "ttXJk\u007Bh";
        private static readonly string NickNameName = "DiQND\u0080L";

        public class ModUser
        {
            public string Name;
            public string Version;
        }

        private static readonly List<ModUser> _modUsers = new List<ModUser>();
        public static IList<ModUser> ModUsers { get { return _modUsers; } }

        private static bool Resolve()
        {
            if (_resolved) return (object)_setProps != null && (object)_localPlayerProp != null;

            try
            {
                if ((object)_photonNetType == null)
                {
                    Assembly asm = null;
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    for (int i = 0; i < assemblies.Length; i++)
                    {
                        if (string.Equals(assemblies[i].GetName().Name, "Assembly-CSharp", StringComparison.Ordinal))
                        { asm = assemblies[i]; break; }
                    }
                    if ((object)asm == null) return false;

                    Type[] types = asm.GetTypes();
                    for (int i = 0; i < types.Length; i++)
                    {
                        if (string.Equals(types[i].Name, PhotonNetName, StringComparison.Ordinal))
                        {
                            _photonNetType = types[i];
                            break;
                        }
                    }
                    if ((object)_photonNetType == null) { _resolved = true; return false; }

                    _localPlayerProp = _photonNetType.GetProperty(LocalPlayerName, BindingFlags.Public | BindingFlags.Static);
                    _allPlayersProp = _photonNetType.GetProperty(AllPlayersName, BindingFlags.Public | BindingFlags.Static);

                    if ((object)_localPlayerProp == null)
                    {
                        _resolved = true;
                        return false;
                    }
                }

                object localP = _localPlayerProp.GetValue(null, null);
                if ((object)localP == null) return false;

                if ((object)_setProps == null)
                {
                    Type playerType = localP.GetType();
                    _setProps = playerType.GetMethod(SetPropsName, BindingFlags.Public | BindingFlags.Instance);
                    _propsProp = playerType.GetProperty(CustomPropsName, BindingFlags.Public | BindingFlags.Instance);
                    _nickProp = playerType.GetProperty(NickNameName, BindingFlags.Public | BindingFlags.Instance);
                }

                _resolved = (object)_setProps != null;
                return _resolved;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[ModDetect] Resolve failed: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "ModDetection");
                return false;
            }
        }

        public static void TagLocalPlayer()
        {
            try
            {
                if (!ModChat.InRoom)
                {
                    _tagged = false;
                    return;
                }

                if (!Resolve()) return;

                object localP = _localPlayerProp.GetValue(null, null);
                if ((object)localP == null)
                {
                    _tagged = false;
                    return;
                }

                if (_tagged) return;

                ParameterInfo[] parms = _setProps.GetParameters();
                Type htType = parms.Length > 0 ? parms[0].ParameterType : null;
                if ((object)htType == null) return;

                object ht = Activator.CreateInstance(htType);
                MethodInfo addMethod = htType.GetMethod("Add", new Type[] { typeof(object), typeof(object) });
                if ((object)addMethod != null)
                    addMethod.Invoke(ht, new object[] { PropKey, ModVersion });
                else
                {
                    PropertyInfo item = htType.GetProperty("Item");
                    if ((object)item != null)
                        item.SetValue(ht, ModVersion, new object[] { PropKey });
                }

                _setProps.Invoke(localP, new object[] { ht, null, false });
                _tagged = true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[ModDetect] TagLocalPlayer failed: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "ModDetection");
            }
        }

        public static void ResetTag()
        {
            _tagged = false;
            _modUsers.Clear();
            _nextScanTime = 0f;
        }

        public static void Scan()
        {
            _modUsers.Clear();

            try
            {
                if (!ModChat.InRoom)
                    return;
                if (!Resolve()) return;
                if ((object)_allPlayersProp == null) return;

                object playersObj = _allPlayersProp.GetValue(null, null);
                if ((object)playersObj == null) return;

                Array players = playersObj as Array;
                if ((object)players == null) return;

                for (int i = 0; i < players.Length; i++)
                {
                    object player = players.GetValue(i);
                    if ((object)player == null) continue;

                    try
                    {
                        object props = null;
                        Type pType = player.GetType();

                        PropertyInfo pp = _propsProp;
                        if ((object)pp == null)
                            pp = pType.GetProperty(CustomPropsName, BindingFlags.Public | BindingFlags.Instance);

                        if ((object)pp != null)
                            props = pp.GetValue(player, null);

                        if ((object)props == null) continue;

                        IDictionary dict = props as IDictionary;
                        if ((object)dict == null) continue;
                        if (!dict.Contains(PropKey)) continue;

                        object verObj = dict[PropKey];
                        string version = (object)verObj != null ? verObj.ToString() : "?";
                        string name = GetPlayerName(player, pType);
                        _modUsers.Add(new ModUser { Name = name, Version = version });
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[ModDetect] Scan failed: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "ModDetection");
            }
        }

        public static string FormatAllPlayerPropsDump()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            try
            {
                if (!ModChat.InRoom)
                {
                    sb.AppendLine("(not in Photon room)");
                    return sb.ToString();
                }
                if (!Resolve() || (object)_allPlayersProp == null)
                {
                    sb.AppendLine("(Photon resolve failed)");
                    return sb.ToString();
                }

                object playersObj = _allPlayersProp.GetValue(null, null);
                Array players = playersObj as Array;
                if ((object)players == null)
                {
                    sb.AppendLine("(no player array)");
                    return sb.ToString();
                }

                sb.AppendLine("players=" + players.Length);
                for (int i = 0; i < players.Length; i++)
                {
                    object player = players.GetValue(i);
                    if ((object)player == null) continue;
                    Type pType = player.GetType();
                    string name = GetPlayerName(player, pType);
                    sb.AppendLine("--- " + name + " ---");

                    try
                    {
                        PropertyInfo pp = _propsProp;
                        if ((object)pp == null)
                            pp = pType.GetProperty(CustomPropsName, BindingFlags.Public | BindingFlags.Instance);
                        object props = (object)pp != null ? pp.GetValue(player, null) : null;
                        IDictionary dict = props as IDictionary;
                        if ((object)dict == null || dict.Count == 0)
                        {
                            sb.AppendLine("  (no custom props)");
                            continue;
                        }
                        foreach (DictionaryEntry e in dict)
                        {
                            string k = (object)e.Key != null ? e.Key.ToString() : "null";
                            string v = (object)e.Value != null ? e.Value.ToString() : "null";
                            sb.AppendLine("  " + k + " = " + v);
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine("  err: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("dump failed: " + ex.Message);
            }
            return sb.ToString();
        }

        public static void Tick()
        {
            TagLocalPlayer();
            float now = UnityEngine.Time.unscaledTime;
            if (now < _nextScanTime) return;
            _nextScanTime = now + ScanInterval;
            Scan();
        }

        private static string GetPlayerName(object player, Type pType)
        {
            try
            {
                PropertyInfo nick = _nickProp;
                if ((object)nick == null)
                    nick = pType.GetProperty(NickNameName, BindingFlags.Public | BindingFlags.Instance);
                if ((object)nick != null)
                {
                    object n = nick.GetValue(player, null);
                    string s = n != null ? n.ToString() : null;
                    if (!string.IsNullOrEmpty(s)) return s;
                }
            }
            catch { }

            string fallback = player.ToString();
            return string.IsNullOrEmpty(fallback) ? "Unknown" : fallback;
        }
    }
}

