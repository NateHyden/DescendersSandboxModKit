using MelonLoader;
using DescendersModMenu;
using DescendersModMenu.UI;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DescendersModMenu.Mods
{
    public static class ModChat
    {
        public const byte EventCode = 99;
        public const int MaxMessages = 50;
        public const int MaxLength = 300;

        public class ChatMessage
        {
            public string PlayerName;
            public string Text;
            public string Time;
            public bool IsSelf;
        }

        private static readonly List<ChatMessage> _messages = new List<ChatMessage>();
        public static IList<ChatMessage> Messages { get { return _messages; } }

        public static bool HasNewMessages { get; private set; }
        public static void ClearNewFlag() { HasNewMessages = false; }

        public static int UnreadCount { get; private set; }
        public static void MarkAsRead() { UnreadCount = 0; }

        private static Type _photonType = null;
        private static FieldInfo _eventDelegate = null;
        private static MethodInfo _raiseEvent = null;
        private static object _defaultOptions = null;
        private static PropertyInfo _localPlayer = null;
        private static PropertyInfo _nickName = null;
        private static PropertyInfo _inRoom = null;
        private static PropertyInfo _connectionState = null;
        private static System.Type _photonHashtable = null;
        private static bool _subscribed = false;
        private static bool _resolved = false;
        private static bool _photonAccessEnabled = false;
        private static bool _initRequested = false;
        private static bool _wasInRoom;
        private static bool _roomStateKnown;

        public static bool InRoom
        {
            get
            {
                try
                {
                    if (!_photonAccessEnabled || !Resolve()) return false;
                    if ((object)_inRoom == null) return false;
                    object v = _inRoom.GetValue(null, null);
                    return v is bool && (bool)v;
                }
                catch { return false; }
            }
        }

        public static string ConnectionStateLabel
        {
            get
            {
                try
                {
                    if (!_photonAccessEnabled) return "waiting-for-game";
                    if (!Resolve()) return "unresolved";
                    if ((object)_connectionState == null) return "unknown";
                    object v = _connectionState.GetValue(null, null);
                    return (object)v != null ? v.ToString() : "null";
                }
                catch (Exception ex) { return "err:" + ex.Message; }
            }
        }

        public static int PlayerListCount
        {
            get
            {
                try
                {
                    if (!_photonAccessEnabled || !Resolve() || (object)_photonType == null) return -1;
                    PropertyInfo pl = _photonType.GetProperty("CoH\u007C\u007EDq", BindingFlags.Public | BindingFlags.Static);
                    if ((object)pl == null) return -1;
                    object arr = pl.GetValue(null, null);
                    Array a = arr as Array;
                    return (object)a != null ? a.Length : -1;
                }
                catch { return -1; }
            }
        }

        public static string RoomName
        {
            get
            {
                try
                {
                    if (!_photonAccessEnabled || !Resolve() || (object)_photonType == null) return "";
                    PropertyInfo roomProp = _photonType.GetProperty("wkT\u0080REz", BindingFlags.Public | BindingFlags.Static);
                    if ((object)roomProp == null) return "";
                    object room = roomProp.GetValue(null, null);
                    if ((object)room == null) return "";
                    PropertyInfo nameProp = room.GetType().GetProperty("name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if ((object)nameProp == null)
                        nameProp = room.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                    if ((object)nameProp != null)
                    {
                        object n = nameProp.GetValue(room, null);
                        if ((object)n != null) return n.ToString();
                    }
                    return room.ToString();
                }
                catch { return ""; }
            }
        }

        public static bool OfflineMode
        {
            get
            {
                try
                {
                    if (!_photonAccessEnabled || !Resolve() || (object)_photonType == null) return false;
                    PropertyInfo off = _photonType.GetProperty("CEcjsH\u0083", BindingFlags.Public | BindingFlags.Static);
                    if ((object)off == null) return false;
                    object v = off.GetValue(null, null);
                    return v is bool && (bool)v;
                }
                catch { return false; }
            }
        }

        private static Delegate _handlerDelegate = null;

        // ── Init ──────────────────────────────────────────────────────────
        public static void Init()
        {
            _initRequested = true;
        }

        public static void EnablePhotonAccess()
        {
            if (_photonAccessEnabled) return;
            _photonAccessEnabled = true;
            if (_initRequested)
                EnsureSubscribed();
        }

        public static void Tick()
        {
            if (!_photonAccessEnabled) return;
            EnsureSubscribed();
            CheckLobbyChanged();
        }

        private static void CheckLobbyChanged()
        {
            bool inRoom = InRoom;
            if (!_roomStateKnown)
            {
                _wasInRoom = inRoom;
                _roomStateKnown = true;
                return;
            }
            if (_wasInRoom && !inRoom)
                ClearMessages();
            _wasInRoom = inRoom;
        }

        public static void OnMapChanged()
        {
            ClearMessages();
        }

        private static void EnsureSubscribed()
        {
            if (_subscribed) return;
            if (!Resolve()) return;
            Subscribe();
        }

        private static bool Resolve()
        {
            if (_resolved) return !((object)_raiseEvent == null);
            _resolved = true;

            try
            {
                Assembly asm = null;
                Assembly[] allAsm = AppDomain.CurrentDomain.GetAssemblies();
                for (int ai = 0; ai < allAsm.Length; ai++)
                    if (string.Equals(allAsm[ai].GetName().Name, "Assembly-CSharp", StringComparison.Ordinal))
                    { asm = allAsm[ai]; break; }
                if ((object)asm == null) return false;

                foreach (Type t in asm.GetTypes())
                {
                    if (!t.IsClass || !t.IsAbstract || !t.IsSealed) continue;
                    foreach (Type nested in t.GetNestedTypes())
                    {
                        if ((object)nested.BaseType != null && string.Equals(nested.BaseType.FullName, typeof(MulticastDelegate).FullName, StringComparison.Ordinal))
                        {
                            var invoke = nested.GetMethod("Invoke");
                            if ((object)invoke == null) continue;
                            var p = invoke.GetParameters();
                            if (p.Length == 3 && string.Equals(p[0].ParameterType.FullName, typeof(byte).FullName, StringComparison.Ordinal)
                                && string.Equals(p[1].ParameterType.FullName, typeof(object).FullName, StringComparison.Ordinal)
                                && string.Equals(p[2].ParameterType.FullName, typeof(int).FullName, StringComparison.Ordinal))
                            {
                                _photonType = t;
                                break;
                            }
                        }
                    }
                    if ((object)_photonType != null) break;
                }

                if ((object)_photonType == null)
                { MelonLogger.Error("[ModChat] PhotonNetwork type not found."); Telemetry.ReportErrorAsync(new Exception("PhotonNetwork type not found"), "ModChat"); return false; }

                foreach (FieldInfo f in _photonType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if ((object)f.FieldType.BaseType != null && string.Equals(f.FieldType.BaseType.FullName, typeof(MulticastDelegate).FullName, StringComparison.Ordinal))
                    {
                        var invoke = f.FieldType.GetMethod("Invoke");
                        if ((object)invoke == null) continue;
                        var p = invoke.GetParameters();
                        if (p.Length == 3 && string.Equals(p[0].ParameterType.FullName, typeof(byte).FullName, StringComparison.Ordinal)
                            && string.Equals(p[1].ParameterType.FullName, typeof(object).FullName, StringComparison.Ordinal)
                            && string.Equals(p[2].ParameterType.FullName, typeof(int).FullName, StringComparison.Ordinal))
                        { _eventDelegate = f; break; }
                    }
                }

                if ((object)_eventDelegate == null)
                {
                    foreach (EventInfo ev in _photonType.GetEvents(BindingFlags.Public | BindingFlags.Static))
                    {
                        Type ht = ev.EventHandlerType;
                        if ((object)ht == null) continue;
                        var invoke = ht.GetMethod("Invoke");
                        if ((object)invoke == null) continue;
                        var p = invoke.GetParameters();
                        if (p.Length == 3 && string.Equals(p[0].ParameterType.FullName, typeof(byte).FullName, StringComparison.Ordinal)
                            && string.Equals(p[1].ParameterType.FullName, typeof(object).FullName, StringComparison.Ordinal)
                            && string.Equals(p[2].ParameterType.FullName, typeof(int).FullName, StringComparison.Ordinal))
                        {
                            _eventDelegate = _photonType.GetField(ev.Name, BindingFlags.NonPublic | BindingFlags.Static);
                            if ((object)_eventDelegate == null)
                            {
                                MelonLogger.Error("[ModChat] OnEventCall backing field missing: " + ev.Name);
                                Telemetry.ReportErrorAsync(new Exception("OnEventCall backing field missing: " + ev.Name), "ModChat");
                            }
                            break;
                        }
                    }
                }

                foreach (MethodInfo m in _photonType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    var p = m.GetParameters();
                    if (p.Length == 4 && string.Equals(p[0].ParameterType.FullName, typeof(byte).FullName, StringComparison.Ordinal)
                        && string.Equals(p[1].ParameterType.FullName, typeof(object).FullName, StringComparison.Ordinal)
                        && string.Equals(p[2].ParameterType.FullName, typeof(bool).FullName, StringComparison.Ordinal))
                    { _raiseEvent = m; break; }
                }

                if ((object)_raiseEvent != null)
                {
                    var optType = _raiseEvent.GetParameters()[3].ParameterType;
                    foreach (FieldInfo f in optType.GetFields(BindingFlags.Public | BindingFlags.Static))
                        if (string.Equals(f.FieldType.FullName, optType.FullName, StringComparison.Ordinal)) { _defaultOptions = f.GetValue(null); break; }
                }

                _localPlayer = _photonType.GetProperty("gQ\u0060\u0083tus",
                    BindingFlags.Public | BindingFlags.Static);

                _inRoom = _photonType.GetProperty("La\u0080lETO", BindingFlags.Public | BindingFlags.Static);
                _connectionState = _photonType.GetProperty("W\u007Dikkp\u0080", BindingFlags.Public | BindingFlags.Static);

                Assembly[] htAsms = AppDomain.CurrentDomain.GetAssemblies();
                for (int ai = 0; ai < htAsms.Length; ai++)
                    if (string.Equals(htAsms[ai].GetName().Name, "Photon3Unity3D", StringComparison.Ordinal))
                    { _photonHashtable = htAsms[ai].GetType("ExitGames.Client.Photon.Hashtable"); break; }

                return (object)_raiseEvent != null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[ModChat] Resolve: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "ModChat");
                return false;
            }
        }

        private static void Subscribe()
        {
            if (_subscribed || (object)_eventDelegate == null) return;
            try
            {
                Type delType = _eventDelegate.FieldType;
                var handler = typeof(ModChat).GetMethod("OnPhotonEvent",
                    BindingFlags.NonPublic | BindingFlags.Static);
                _handlerDelegate = Delegate.CreateDelegate(delType, handler);

                Delegate existing = _eventDelegate.GetValue(null) as Delegate;
                _eventDelegate.SetValue(null,
                    (object)existing != null ? Delegate.Combine(existing, _handlerDelegate) : _handlerDelegate);

                _subscribed = true;
            }
            catch (Exception ex) { MelonLogger.Error("[ModChat] Subscribe: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "ModChat"); }
        }

        private static void Unsubscribe()
        {
            if (!_subscribed || (object)_eventDelegate == null || (object)_handlerDelegate == null) return;
            try
            {
                Delegate existing = _eventDelegate.GetValue(null) as Delegate;
                if ((object)existing != null)
                    _eventDelegate.SetValue(null, Delegate.Remove(existing, _handlerDelegate));
                _subscribed = false;
            }
            catch (Exception ex) { MelonLogger.Error("[ModChat] Unsubscribe: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "ModChat"); }
        }

        // ── Photon event handler ──────────────────────────────────────────
        private static void OnPhotonEvent(byte eventCode, object data, int senderId)
        {
            if (eventCode != EventCode) return;
            try
            {
                System.Collections.IDictionary ht = data as System.Collections.IDictionary;
                if ((object)ht == null) return;
                string name = ht["n"] as string;
                string msg = ht["m"] as string;
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(msg)) return;

                var cm = new ChatMessage
                {
                    PlayerName = name,
                    Text = msg,
                    Time = DateTime.Now.ToString("HH:mm"),
                    IsSelf = false
                };
                AddMessage(cm);
            }
            catch (Exception ex) { MelonLogger.Error("[ModChat] OnPhotonEvent: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "ModChat"); }
        }

        // ── Send ──────────────────────────────────────────────────────────
        public static bool Send(string message)
        {
            if (string.IsNullOrEmpty(message)) return false;
            if (message.Length > MaxLength) message = message.Substring(0, MaxLength);
            if (!_photonAccessEnabled) EnablePhotonAccess();
            if (!Resolve())
            {
                MelonLogger.Error("[ModChat] Send: Resolve() failed.");
                Telemetry.ReportErrorAsync(new Exception("ModChat Send Resolve failed"), "ModChat");
                return false;
            }
            if ((object)_raiseEvent == null)
            {
                MelonLogger.Error("[ModChat] Send: RaiseEvent is null.");
                Telemetry.ReportErrorAsync(new Exception("ModChat RaiseEvent null"), "ModChat");
                return false;
            }

            try
            {
                string playerName = GetLocalPlayerName();
                bool inRoom = InRoom;

                AddMessage(new ChatMessage
                {
                    PlayerName = playerName,
                    Text = message,
                    Time = DateTime.Now.ToString("HH:mm"),
                    IsSelf = true
                });

                if (!inRoom)
                    return true;

                System.Collections.IDictionary ht = null;
                try
                {
                    if ((object)_photonHashtable != null)
                        ht = System.Activator.CreateInstance(_photonHashtable) as System.Collections.IDictionary;
                }
                catch (Exception htEx)
                {
                    MelonLogger.Error("[ModChat] Photon Hashtable: " + htEx.Message);
                    Telemetry.ReportErrorAsync(htEx, "ModChat");
                }

                if ((object)ht == null)
                {
                    MelonLogger.Error("[ModChat] Could not create Photon Hashtable.");
                    Telemetry.ReportErrorAsync(new Exception("Photon Hashtable create failed"), "ModChat");
                    return true;
                }
                ht["n"] = playerName;
                ht["m"] = message;

                object result = null;
                try
                {
                    result = _raiseEvent.Invoke(null,
                        new object[] { (byte)EventCode, (object)ht, true, _defaultOptions });
                }
                catch (System.Reflection.TargetInvocationException tie)
                {
                    Exception inner = tie.InnerException;
                    MelonLogger.Error("[ModChat] Send TargetInvocationException: "
                        + ((object)inner != null ? inner.GetType().Name + ": " + inner.Message : "null inner"));
                    Telemetry.ReportErrorAsync(tie, "ModChat");
                    return true;
                }

                bool sent = result is bool && (bool)result;
                if (!sent)
                {
                    MelonLogger.Error("[ModChat] RaiseEvent returned false (inRoom=" + InRoom
                        + " state=" + ConnectionStateLabel + ")");
                    Telemetry.ReportErrorAsync(new Exception("RaiseEvent returned false"), "ModChat");
                }
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[ModChat] Send: " + ex.GetType().Name + ": " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "ModChat");
                return false;
            }
        }

        private static string GetLocalPlayerName()
        {
            try
            {
                if ((object)_localPlayer != null)
                {
                    object player = _localPlayer.GetValue(null, null);
                    if ((object)player != null)
                    {
                        if ((object)_nickName == null)
                            _nickName = player.GetType().GetProperty("DiQND\u0080L",
                                BindingFlags.Public | BindingFlags.Instance);
                        if ((object)_nickName != null)
                        {
                            object nick = _nickName.GetValue(player, null);
                            string s = nick != null ? nick.ToString() : null;
                            if (!string.IsNullOrEmpty(s)) return s;
                        }
                    }
                }

                PlayerManager pm = GameObject.FindObjectOfType<PlayerManager>();
                if ((object)pm == null) return "Unknown";
                PlayerInfoImpact pip = pm.GetPlayerImpact();
                if ((object)pip == null) return "Unknown";

                System.Type t = pip.GetType();
                while ((object)t != null)
                {
                    FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (!string.Equals(fields[i].FieldType.Name, "PhotonView", StringComparison.Ordinal)) continue;
                        object pv = fields[i].GetValue(pip);
                        if ((object)pv == null) continue;

                        PropertyInfo[] props = pv.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        for (int j = 0; j < props.Length; j++)
                        {
                            try
                            {
                                object val = props[j].GetValue(pv, null);
                                if ((object)val == null) continue;
                                string s = val.ToString();
                                if (string.IsNullOrEmpty(s)) continue;
                                if (string.Equals(props[j].Name, "tag", StringComparison.Ordinal)) continue;
                                if (string.Equals(props[j].Name, "name", StringComparison.Ordinal)) continue;
                                if (s.IndexOf('/') >= 0 || s.IndexOf('(') >= 0 || s.IndexOf('.') >= 0) continue;
                                bool hasLetter = false;
                                for (int k = 0; k < s.Length; k++)
                                    if (char.IsLetter(s[k])) { hasLetter = true; break; }
                                if (!hasLetter) continue;
                                return s;
                            }
                            catch { }
                        }
                        break;
                    }
                    t = t.BaseType;
                }
            }
            catch { }
            return "Unknown";
        }

        private static void AddMessage(ChatMessage msg)
        {
            _messages.Add(msg);
            if (_messages.Count > MaxMessages)
                _messages.RemoveAt(0);
            HasNewMessages = true;
            if (!msg.IsSelf && !MenuWindow.IsChatOpen)
                UnreadCount++;
            ModLog.Debug("[ModChat] <" + msg.PlayerName + "> " + msg.Text);
            try { ChatHUD.Notify(msg); }
            catch (Exception ex)
            {
                MelonLogger.Error("[ModChat] ChatHUD.Notify: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "ModChat");
            }
        }

        public static void ClearMessages()
        {
            UnreadCount = 0;
            if (_messages.Count == 0)
            {
                HasNewMessages = true;
                try { ChatHUD.Reset(); } catch { }
                return;
            }
            _messages.Clear();
            HasNewMessages = true;
            try { ChatHUD.Reset(); } catch { }
        }

        public static void Reset()
        {
            _roomStateKnown = false;
            ClearMessages();
            if (!_photonAccessEnabled) return;
            if (_subscribed) return;
            EnsureSubscribed();
        }
    }
}

