using System;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    public static class ExplodingProps
    {
        public static bool Enabled { get; private set; } = false;
        private static PropCollisionHandler _handler = null;

        // Career map scene names — exploding props disabled here
        private static readonly string[] CareerScenes =
        {
            "highlands", "forest", "canyon", "peaks", "hell",
            "desert", "jungle", "favela", "glaciers", "ridges",
            "overworld"
        };

        public static bool IsCareerScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            string lower = sceneName.ToLowerInvariant();
            for (int i = 0; i < CareerScenes.Length; i++)
                if (lower.StartsWith(CareerScenes[i])) return true;
            return false;
        }

        public static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Attach(); else Detach();
            MelonLogger.Msg("[ExplodingProps] -> " + (Enabled ? "ON" : "OFF"));
        }

        public static void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            if (Enabled) Attach(); else Detach();
        }

        private static void Attach()
        {
            try
            {
                GameObject player = GameObject.Find("Player_Human");
                if ((object)player == null)
                { MelonLogger.Warning("[ExplodingProps] Player_Human not found."); return; }

                _handler = player.GetComponent<PropCollisionHandler>();
                if ((object)_handler == null)
                    _handler = player.AddComponent<PropCollisionHandler>();

                _handler.enabled = true;
            }
            catch (Exception ex)
            { MelonLogger.Error("[ExplodingProps] Attach: " + ex.Message); }
        }

        private static void Detach()
        {
            try
            {
                if ((object)_handler != null && (object)(_handler as UnityEngine.Object) != null)
                    _handler.enabled = false;
            }
            catch { }
            _handler = null;
        }

        public static void Reset()
        {
            Enabled = false;
            Detach();
        }

        // Called from OnSceneWasInitialized — auto-disable on career maps
        public static void OnSceneInitialized(string sceneName)
        {
            if (Enabled && IsCareerScene(sceneName))
            {
                MelonLogger.Msg("[ExplodingProps] Career map detected — disabling.");
                Enabled = false;
                Detach();
            }
            else if (Enabled)
            {
                Attach(); // re-attach after scene load if still enabled
            }
        }
    }

    public class PropCollisionHandler : MonoBehaviour
    {
        private Vehicle _vehicle;
        private Rigidbody _rb;
        private MethodInfo _setVelocityMethod;
        private MethodInfo _playOneShotMethod;
        private bool _cached = false;

        private float _lastBounceTime = -999f;
        private const float BounceCooldown = 0.5f;
        private const float BounceSpeed = 18f;
        private const float MinImpactSpeed = 2f; // lowered from 5f — triggers on lighter touches

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _vehicle = GetComponent<Vehicle>();
        }

        private void CacheReflection()
        {
            if (_cached) return;
            _cached = true;

            try
            {
                _setVelocityMethod = typeof(Vehicle).GetMethod("SetVelocity",
                    BindingFlags.Public | BindingFlags.Instance, null,
                    new Type[] { typeof(Vector3) }, null);

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int a = 0; a < assemblies.Length; a++)
                {
                    Type rtType = assemblies[a].GetType("FMODUnity.RuntimeManager");
                    if ((object)rtType == null) continue;

                    MethodInfo[] methods = rtType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    for (int m = 0; m < methods.Length; m++)
                    {
                        if (methods[m].Name != "PlayOneShot") continue;
                        ParameterInfo[] parms = methods[m].GetParameters();
                        if (parms.Length == 2 &&
                            string.Equals(parms[0].ParameterType.Name, "String", StringComparison.Ordinal) &&
                            string.Equals(parms[1].ParameterType.Name, "Vector3", StringComparison.Ordinal))
                        { _playOneShotMethod = methods[m]; break; }
                    }
                    if ((object)_playOneShotMethod == null)
                    {
                        for (int m = 0; m < methods.Length; m++)
                        {
                            if (methods[m].Name != "PlayOneShot") continue;
                            ParameterInfo[] parms = methods[m].GetParameters();
                            if (parms.Length == 1 &&
                                string.Equals(parms[0].ParameterType.Name, "String", StringComparison.Ordinal))
                            { _playOneShotMethod = methods[m]; break; }
                        }
                    }
                    break;
                }
            }
            catch (Exception ex) { MelonLogger.Error("[ExplodingProps] CacheReflection: " + ex.Message); }
        }

        private void PlayCrashSound()
        {
            if ((object)_playOneShotMethod == null) return;
            try
            {
                ParameterInfo[] parms = _playOneShotMethod.GetParameters();
                if (parms.Length == 2)
                    _playOneShotMethod.Invoke(null, new object[] { "event:/sfx/impact/surface/vehicle", transform.position });
                else
                    _playOneShotMethod.Invoke(null, new object[] { "event:/sfx/impact/surface/vehicle" });
            }
            catch { }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!ExplodingProps.Enabled) return;
            if ((object)_vehicle == null) return;
            if ((object)_rb == null) return;
            if (Time.unscaledTime - _lastBounceTime < BounceCooldown) return;
            if (collision.contacts.Length == 0) return;

            Vector3 normal = collision.contacts[0].normal;
            if (normal.y > 0.5f) return;

            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < MinImpactSpeed) return;

            GameObject other = collision.gameObject;
            if ((object)other == null) return;
            string otherName = other.name;
            if (otherName == "Player_Human") return;
            if (otherName == "wheel_front" || otherName == "wheel_back") return;

            if (!_cached) CacheReflection();

            Vector3 bounceDir = Vector3.Reflect(_rb.velocity.normalized, normal);
            bounceDir.y = Mathf.Max(bounceDir.y, 0.4f);
            bounceDir = bounceDir.normalized;

            float speed = _rb.velocity.magnitude;
            float force = Mathf.Max(speed * 0.8f, BounceSpeed);
            Vector3 bounceVelocity = (bounceDir + Vector3.up * 0.3f) * force;

            if ((object)_setVelocityMethod != null)
                _setVelocityMethod.Invoke(_vehicle, new object[] { bounceVelocity });
            else
                _rb.velocity = bounceVelocity;

            PlayCrashSound();

            Rigidbody propRb = other.GetComponent<Rigidbody>();
            if ((object)propRb != null)
            {
                propRb.isKinematic = false;
                Vector3 awayDir = (other.transform.position - transform.position).normalized;
                awayDir.y = 0.5f;
                propRb.AddForce(awayDir * 20f, ForceMode.VelocityChange);
            }

            _lastBounceTime = Time.unscaledTime;
        }
    }
}