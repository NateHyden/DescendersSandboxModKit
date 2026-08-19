using MelonLoader;
using DescendersModMenu;
using UnityEngine;

namespace DescendersModMenu.Mods
{
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
                src.spatialBlend = 0f;
                src.volume = 0.8f;
                src.Play();
                UnityEngine.Object.Destroy(go, _clip.length + 0.2f);
                ModLog.Debug("[Airhorn] Honk.");
            }
            catch (System.Exception ex) { MelonLogger.Error("[Airhorn] Honk: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "Airhorn"); }
        }

        private static AudioClip BuildClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.9f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            const float freqLow = 349f;
            const float freqHigh = 523f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;

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

