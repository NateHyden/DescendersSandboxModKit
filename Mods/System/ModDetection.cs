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
        public const string ModVersion = "3.0.0";

        private static Type _photonNetType;    // upVWa84E
        private static FieldInfo _localPlayer; // gQ`083tus
        private static FieldInfo _allPlayers;  // CoH||EDq
        private static MethodInfo _setProps;   // KxvEguU
        private static FieldInfo _propsField;  // ttXJk{h
        private static bool _resolved;
        private static bool _tagged;

        // Obfuscated names — exact unicode from decompiler
        private static readonly string PhotonNetName = "upVWa\u0084E";
        private static readonly string LocalPlayerName = "gQ\u0060\u0083tus";
        private static readonly string AllPlayersName = "CoH\u007C\u007EDq";
        private static readonly string SetPropsName = "KxvEguU";
        private static readonly string CustomPropsName = "ttXJk\u007Bh";

        public class ModUser
        {
            public string Name;
            public string Version;
        }

        private static readonly List<ModUser> _modUsers = new List<ModUser>();
        public static IList<ModUser> ModUsers { get { return _modUsers; } }

        // ── Resolve Photon types via reflection ─────────────────────────
        private static bool Resolve()
        {
            if (_resolved) return (object)_setProps != null;

            try
            {
                // Step 1: Find the type (only once)
                if ((object)_photonNetType == null)
                {
                    Assembly asm = null;
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    for (int i = 0; i < assemblies.Length; i++)
                    {
                        if (assemblies[i].GetName().Name == "Assembly-CSharp")
                        { asm = assemblies[i]; break; }
                    }
                    if ((object)asm == null) return false;

                    Type[] types = asm.GetTypes();
                    for (int i = 0; i < types.Length; i++)
                    {
                        if (string.Equals(types[i].Name, PhotonNetName, StringComparison.Ordinal))
                        {
                            _photonNetType = types[i];
                            MelonLogger.Msg("[ModDetect] Found PhotonNetwork: " + types[i].Name.Length + " chars");
                            break;
                        }
                    }

                    if ((object)_photonNetType == null)
                    {
                        // Fallback: find any static class with a "RaiseEvent" error string
                        Type[] types2 = asm.GetTypes();
                        for (int i = 0; i < types2.Length; i++)
                        {
                            if (!types2[i].IsClass || !types2[i].IsAbstract || !types2[i].IsSealed) continue;
                            FieldInfo[] sfs = types2[i].GetFields(BindingFlags.Public | BindingFlags.Static);
                            if (sfs.Length > 20)
                            {
                                _photonNetType = types2[i];
                                MelonLogger.Msg("[ModDetect] Found PhotonNetwork (fallback): " + types2[i].Name);
                                break;
                            }
                        }
                    }

                    if ((object)_photonNetType == null) { _resolved = true; return false; }

                    // Get field references
                    FieldInfo[] allFields = _photonNetType.GetFields(BindingFlags.Public | BindingFlags.Static);
                    for (int i = 0; i < allFields.Length; i++)
                    {
                        string fn = allFields[i].Name;
                        if (string.Equals(fn, LocalPlayerName, StringComparison.Ordinal))
                            _localPlayer = allFields[i];
                        else if (string.Equals(fn, AllPlayersName, StringComparison.Ordinal))
                            _allPlayers = allFields[i];
                    }

                    if ((object)_localPlayer == null)
                    {
                        // Debug: log first few field names
                        MelonLogger.Msg("[ModDetect] LocalPlayer not found. Fields (" + allFields.Length + "):");
                        for (int i = 0; i < allFields.Length && i < 8; i++)
                            MelonLogger.Msg("[ModDetect]   [" + i + "] len=" + allFields[i].Name.Length + " type=" + allFields[i].FieldType.Name);
                        _resolved = true;
                        return false;
                    }
                }

                // Step 2: Get local player instance (may be null if not in room yet)
                if ((object)_localPlayer == null) { _resolved = true; return false; }

                object localP = _localPlayer.GetValue(null);
                if ((object)localP == null) return false; // Not in room yet — will retry

                // Step 3: Resolve methods on the player type (only once)
                if ((object)_setProps == null)
                {
                    Type playerType = localP.GetType();
                    MethodInfo[] methods = playerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        if (string.Equals(methods[i].Name, SetPropsName, StringComparison.Ordinal))
                        {
                            _setProps = methods[i];
                            MelonLogger.Msg("[ModDetect] Found SetCustomProperties");
                            break;
                        }
                    }

                    // Find custom props field/property
                    FieldInfo[] fields = playerType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (string.Equals(fields[i].Name, CustomPropsName, StringComparison.Ordinal))
                        { _propsField = fields[i]; break; }
                    }
                    if ((object)_propsField == null)
                    {
                        PropertyInfo[] props = playerType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        for (int i = 0; i < props.Length; i++)
                        {
                            if (string.Equals(props[i].Name, CustomPropsName, StringComparison.Ordinal))
                            {
                                MelonLogger.Msg("[ModDetect] Found custom props as property");
                                break;
                            }
                        }
                    }
                }

                _resolved = (object)_setProps != null;
                if (_resolved) MelonLogger.Msg("[ModDetect] Fully resolved");
                return _resolved;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[ModDetect] Resolve failed: " + ex.Message);
                return false;
            }
        }

        // ── Tag local player with mod property ──────────────────────────
        public static void TagLocalPlayer()
        {
            if (_tagged) return;
            try
            {
                if (!Resolve()) return;

                object localP = _localPlayer.GetValue(null);
                if ((object)localP == null) return;

                // Create a Hashtable with our mod key
                // ExitGames.Client.Photon.Hashtable extends System Hashtable
                // We need to find the type and create an instance
                Type htType = null;
                Type playerType = localP.GetType();
                ParameterInfo[] parms = _setProps.GetParameters();
                if (parms.Length > 0)
                    htType = parms[0].ParameterType;

                if ((object)htType == null) { MelonLogger.Warning("[ModDetect] Could not find Hashtable type"); return; }

                object ht = Activator.CreateInstance(htType);
                // Hashtable inherits IDictionary, use Add or indexer
                MethodInfo addMethod = htType.GetMethod("Add", new Type[] { typeof(object), typeof(object) });
                if ((object)addMethod != null)
                {
                    addMethod.Invoke(ht, new object[] { PropKey, ModVersion });
                }
                else
                {
                    // Try indexer
                    htType.GetProperty("Item").SetValue(ht, ModVersion, new object[] { PropKey });
                }

                // Call SetCustomProperties with optional params
                // KxvEguU(Hashtable, Hashtable = null, bool = false)
                _setProps.Invoke(localP, new object[] { ht, null, false });

                _tagged = true;
                MelonLogger.Msg("[ModDetect] Tagged local player with DescMM=" + ModVersion);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[ModDetect] TagLocalPlayer failed: " + ex.Message);
            }
        }

        // ── Scan all players for mod users ──────────────────────────────
        public static void Scan()
        {
            _modUsers.Clear();

            try
            {
                if (!Resolve()) return;
                if ((object)_allPlayers == null) return;

                object playersObj = _allPlayers.GetValue(null);
                if ((object)playersObj == null) return;

                Array players = playersObj as Array;
                if ((object)players == null) return;

                for (int i = 0; i < players.Length; i++)
                {
                    object player = players.GetValue(i);
                    if ((object)player == null) continue;

                    try
                    {
                        // Get custom properties hashtable
                        object props = null;
                        Type pType = player.GetType();

                        // Try field first
                        FieldInfo pf = pType.GetField(CustomPropsName,
                            BindingFlags.Public | BindingFlags.Instance);
                        if ((object)pf != null)
                        {
                            props = pf.GetValue(player);
                        }
                        else
                        {
                            // Try property
                            PropertyInfo pp = pType.GetProperty(CustomPropsName,
                                BindingFlags.Public | BindingFlags.Instance);
                            if ((object)pp != null)
                                props = pp.GetValue(player, null);
                        }

                        if ((object)props == null) continue;

                        // Check for our key — props is a Hashtable
                        IDictionary dict = props as IDictionary;
                        if ((object)dict == null) continue;

                        if (!dict.Contains(PropKey)) continue;

                        object verObj = dict[PropKey];
                        string version = (object)verObj != null ? verObj.ToString() : "?";

                        // Get player name via ToString()
                        string name = player.ToString();
                        if (string.IsNullOrEmpty(name)) name = "Unknown";

                        _modUsers.Add(new ModUser { Name = name, Version = version });
                    }
                    catch { }
                }

                MelonLogger.Msg("[ModDetect] Scan complete: " + _modUsers.Count + " mod user(s) found");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[ModDetect] Scan failed: " + ex.Message);
            }
        }
    }
}