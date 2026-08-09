using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // A one-shot action, not a toggle (like SuperLaunch/TeleportCheckpoint
    // elsewhere in this project) — there's no ON/OFF state to revert since
    // it just fires a sound and stops. The clip itself is synthesised in
    // code (two-tone square wave with an envelope) rather than shipped as
    // an asset, since there's no audio file bundled with the project.
    public static class Airhorn
    {
        private static AudioClip _clip = null;

        public static void Honk()
        {
            try
            {
                if ((object)_clip == null) _clip = BuildClip();
                GameObject go = new GameObject("SandboxAirhorn");
                var src = go.AddComponent<AudioSource>();
                src.clip = _clip;
                src.spatialBlend = 0f; // 2D — always audible regardless of camera
                src.volume = 0.8f;
                src.Play();
                UnityEngine.Object.Destroy(go, _clip.length + 0.2f);
                MelonLogger.Msg("[Airhorn] Honk.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[Airhorn] Honk: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Airhorn"); }
        }

        // Two-tone air-horn: a low fundamental with a fifth stacked on top,
        // square-ish wave for that blaring truck-horn timbre, short attack,
        // sustained body, quick release.
        private static AudioClip BuildClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.9f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            const float freqLow = 349f;   // F4 — was 233f (Bb3), pitched up a fifth
            const float freqHigh = 523f;  // C5 — was 349f (F4)

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;

                // Envelope: fast attack, hold, short release
                float env;
                if (t < 0.03f) env = t / 0.03f;
                else if (t > duration - 0.12f) env = Mathf.Clamp01((duration - t) / 0.12f);
                else env = 1f;

                float low = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freqLow * t));
                float high = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freqHigh * t));
                float sample = (low * 0.6f + high * 0.4f) * env * 0.6f;

                data[i] = sample;
            }

            AudioClip clip = AudioClip.Create("SandboxAirhornClip", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
