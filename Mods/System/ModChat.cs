using MelonLoader;
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
        public const int MaxLength = 120;

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

        // Photon reflection cache
        private static Type _photonType = null;
        private static FieldInfo _eventDelegate = null; // fu\u0080P\u0084yF
        private static MethodInfo _raiseEvent = null; // nO\u0084yY\u005Bu
        private static object _defaultOptions = null; // \u005Ex\u0080gl\u007DS.qK\u005DlINI
        private static FieldInfo _localPlayer = null; // gQ`\u0083tus
        private static System.Type _photonHashtable = null; // ExitGames.Client.Photon.Hashtable
        private static bool _subscribed = false;
        private static bool _resolved = false;

        // Delegate wrapper — stored to allow unsubscription
        private static Delegate _handlerDelegate = null;

        // ── Init ──────────────────────────────────────────────────────────
        public static void Init()
        {
            if (!Resolve()) return;
            Subscribe();
            MelonLogger.Msg("[ModChat] Initialised. Event code: " + EventCode);
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

                // Find PhotonNetwork type (upVWa\u0084E)
                foreach (Type t in asm.GetTypes())
                {
                    if (!t.IsClass || !t.IsAbstract || !t.IsSealed) continue;
                    // Has the X}Osi~[ delegate = it's our PhotonNetwork
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
                { MelonLogger.Warning("[ModChat] PhotonNetwork type not found."); return false; }

                MelonLogger.Msg("[ModChat] PhotonNetwork: " + _photonType.Name);

                // fu\u0080P\u0084yF — the static OnEvent delegate field
                foreach (FieldInfo f in _photonType.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if ((object)f.FieldType.BaseType != null && string.Equals(f.FieldType.BaseType.FullName, typeof(MulticastDelegate).FullName, StringComparison.Ordinal))
                    {
                        var invoke = f.FieldType.GetMethod("Invoke");
                        if ((object)invoke == null) continue;
                        var p = invoke.GetParameters();
                        if (p.Length == 3 && string.Equals(p[0].ParameterType.FullName, typeof(byte).FullName, StringComparison.Ordinal))
                        { _eventDelegate = f; break; }
                    }
                }

                // nO\u0084yY\u005Bu — RaiseEvent(byte, object, bool, options)
                foreach (MethodInfo m in _photonType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    var p = m.GetParameters();
                    if (p.Length == 4 && string.Equals(p[0].ParameterType.FullName, typeof(byte).FullName, StringComparison.Ordinal)
                        && string.Equals(p[1].ParameterType.FullName, typeof(object).FullName, StringComparison.Ordinal)
                        && string.Equals(p[2].ParameterType.FullName, typeof(bool).FullName, StringComparison.Ordinal))
                    { _raiseEvent = m; break; }
                }

                // Default RaiseEventOptions — find type with static default field
                if ((object)_raiseEvent != null)
                {
                    var optType = _raiseEvent.GetParameters()[3].ParameterType;
                    foreach (FieldInfo f in optType.GetFields(BindingFlags.Public | BindingFlags.Static))
                        if (string.Equals(f.FieldType.FullName, optType.FullName, StringComparison.Ordinal)) { _defaultOptions = f.GetValue(null); break; }
                }

                // Local player field
                _localPlayer = _photonType.GetField("gQ\u0060\u0083tus",
                    BindingFlags.Public | BindingFlags.Static);

                // Cache Photon Hashtable type
                Assembly[] htAsms = AppDomain.CurrentDomain.GetAssemblies();
                for (int ai = 0; ai < htAsms.Length; ai++)
                    if (string.Equals(htAsms[ai].GetName().Name, "Photon3Unity3D", StringComparison.Ordinal))
                    { _photonHashtable = htAsms[ai].GetType("ExitGames.Client.Photon.Hashtable"); break; }
                MelonLogger.Msg("[ModChat] PhotonHashtable=" + ((object)_photonHashtable != null ? _photonHashtable.FullName : "NOT FOUND"));

                MelonLogger.Msg("[ModChat] raiseEvent=" + ((object)_raiseEvent != null)
                    + " eventDelegate=" + ((object)_eventDelegate != null)
                    + " localPlayer=" + ((object)_localPlayer != null));
                return true;
            }
            catch (Exception ex) { MelonLogger.Error("[ModChat] Resolve: " + ex.Message); return false; }
        }

        private static void Subscribe()
        {
            if (_subscribed || (object)_eventDelegate == null) return;
            try
            {
                // Get delegate invoke method to create handler
                Type delType = _eventDelegate.FieldType;
                var handler = typeof(ModChat).GetMethod("OnPhotonEvent",
                    BindingFlags.NonPublic | BindingFlags.Static);
                _handlerDelegate = Delegate.CreateDelegate(delType, handler);

                // Subscribe: fu\u0080P\u0084yF += handler
                Delegate existing = _eventDelegate.GetValue(null) as Delegate;
                _eventDelegate.SetValue(null,
                    (object)existing != null ? Delegate.Combine(existing, _handlerDelegate) : _handlerDelegate);

                _subscribed = true;
                MelonLogger.Msg("[ModChat] Subscribed to Photon events.");
            }
            catch (Exception ex) { MelonLogger.Error("[ModChat] Subscribe: " + ex.Message); }
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
            catch (Exception ex) { MelonLogger.Error("[ModChat] Unsubscribe: " + ex.Message); }
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
                MelonLogger.Msg("[ModChat] <" + name + "> " + msg);
            }
            catch (Exception ex) { MelonLogger.Error("[ModChat] OnPhotonEvent: " + ex.Message); }
        }

        // ── Send ──────────────────────────────────────────────────────────
        public static bool Send(string message)
        {
            if (string.IsNullOrEmpty(message)) return false;
            if (message.Length > MaxLength) message = message.Substring(0, MaxLength);
            if (!Resolve()) { MelonLogger.Warning("[ModChat] Send: Resolve() failed."); return false; }
            if ((object)_raiseEvent == null) { MelonLogger.Warning("[ModChat] Send: _raiseEvent is null."); return false; }

            try
            {
                MelonLogger.Msg("[ModChat] Send start. msg='" + message + "'");
                string playerName = GetLocalPlayerName();
                MelonLogger.Msg("[ModChat] Send: player='" + playerName + "'");

                // Photon requires ExitGames.Client.Photon.Hashtable, not System.Collections.Hashtable
                // Find it via reflection from the Photon3Unity3D assembly
                System.Collections.IDictionary ht = null;
                try
                {
                    if ((object)_photonHashtable != null)
                    {
                        ht = System.Activator.CreateInstance(_photonHashtable) as System.Collections.IDictionary;
                        MelonLogger.Msg("[ModChat] Using Photon Hashtable: " + _photonHashtable.FullName);
                    }
                }
                catch (Exception htEx) { MelonLogger.Warning("[ModChat] Photon Hashtable fallback: " + htEx.Message); }

                if ((object)ht == null)
                {
                    MelonLogger.Warning("[ModChat] Could not create Photon Hashtable!");
                    return false;
                }
                ht["n"] = playerName;
                ht["m"] = message;

                MelonLogger.Msg("[ModChat] Send: invoking " + _raiseEvent.Name
                    + " defaultOptions=" + (_defaultOptions != null ? _defaultOptions.GetType().Name : "null"));

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
                    if ((object)inner != null)
                        MelonLogger.Error("[ModChat] Inner stack: " + inner.StackTrace);
                    return false;
                }

                bool sent = result is bool && (bool)result;
                MelonLogger.Msg("[ModChat] Send result=" + sent);

                if (sent)
                {
                    AddMessage(new ChatMessage
                    {
                        PlayerName = playerName,
                        Text = message,
                        Time = DateTime.Now.ToString("HH:mm"),
                        IsSelf = true
                    });
                }
                return sent;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[ModChat] Send outer: " + ex.GetType().Name + ": " + ex.Message);
                MelonLogger.Error("[ModChat] Send stack: " + ex.StackTrace);
                return false;
            }
        }

        private static string GetLocalPlayerName()
        {
            try
            {
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
                                // Skip Unity built-in props
                                if (string.Equals(props[j].Name, "tag", StringComparison.Ordinal)) continue;
                                if (string.Equals(props[j].Name, "name", StringComparison.Ordinal)) continue;
                                // Skip anything with path/object chars
                                if (s.IndexOf('/') >= 0 || s.IndexOf('(') >= 0 || s.IndexOf('.') >= 0) continue;
                                // Must contain a letter AND a space (i.e. "Kareem Pie" not "1825" or "True")
                                bool hasLetter = false, hasSpace = false;
                                for (int k = 0; k < s.Length; k++)
                                {
                                    if (char.IsLetter(s[k])) hasLetter = true;
                                    if (char.IsWhiteSpace(s[k])) hasSpace = true;
                                }
                                if (!hasLetter || !hasSpace) continue;
                                MelonLogger.Msg("[ModChat] GetName='" + s + "' prop=" + props[j].Name + " type=" + props[j].PropertyType.Name);
                                return s;
                            }
                            catch { }
                        }
                        break;
                    }
                    t = t.BaseType;
                }
                MelonLogger.Warning("[ModChat] GetName: not found");
            }
            catch (Exception ex) { MelonLogger.Warning("[ModChat] GetLocalPlayerName: " + ex.Message); }
            return "Unknown";
        }

        private static void AddMessage(ChatMessage msg)
        {
            _messages.Add(msg);
            if (_messages.Count > MaxMessages)
                _messages.RemoveAt(0);
            HasNewMessages = true;
        }

        public static void ClearMessages() => _messages.Clear();

        public static void Reset() => Unsubscribe();
    }
}