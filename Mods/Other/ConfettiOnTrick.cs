using MelonLoader;
using UnityEngine;

namespace DescendersModMenu.Mods
{
    // Fires a confetti particle burst whenever the player lands after
    // airtime above the threshold. Hooked from SessionTrackers' existing
    // ground-transition detection (SessionTrackers.Tick landing branch)
    // via ConfettiOnTrick.OnLanded(airtime) — no new ground-detection
    // code needed, reuses the same onGround property SessionTrackers
    // already resolves for the Longest Airtime tracker.
    public static class ConfettiOnTrick
    {
        public static bool Enabled { get; private set; } = false;
        private const float AirtimeThreshold = 0.35f; // seconds — below this it's not a "trick", just a bump

        public static void Toggle()
        {
            Enabled = !Enabled;
            MelonLogger.Msg("[ConfettiOnTrick] -> " + (Enabled ? "ON" : "OFF"));
        }

        // Called from SessionTrackers when the player transitions air -> ground.
        public static void OnLanded(float airtime)
        {
            if (!Enabled) return;
            if (airtime < AirtimeThreshold) return;
            try { SpawnBurst(); }
            catch (System.Exception ex) { MelonLogger.Error("[ConfettiOnTrick] SpawnBurst: " + ex.Message); }
        }

        private static readonly Color[] ConfettiColours = new Color[]
        {
            Color.red, Color.yellow, Color.green, Color.cyan, Color.magenta, new Color(1f, 0.6f, 0f)
        };

        private static void SpawnBurst()
        {
            GameObject player = GameObject.Find("Player_Human");
            if ((object)player == null) return;

            PlayPop(player.transform.position);

            GameObject psObj = new GameObject("SandboxConfetti");
            psObj.transform.position = player.transform.position + Vector3.up * 1.4f;
            var ps = psObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1.0f;
            main.startLifetime = 1.7f;                                    // was 1.2f — hangs in the air longer
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.0f);  // was 3-7 — slower burst
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f); // back to original small size per feedback
            main.gravityModifier = 0.7f;                                  // lighter fall to match the slower speed
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 160) }); // was 90 — more particles at the original small size

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var renderer = psObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            main.startColor = ConfettiColours[Random.Range(0, ConfettiColours.Length)];

            ps.Play();
            UnityEngine.Object.Destroy(psObj, 3.2f);
        }

        // ── Pop sound ─────────────────────────────────────────────────
        // Synthesised the same way Airhorn.cs builds its clip — short burst
        // of filtered noise with a fast decay, reads as a "bang"/party-popper
        // pop rather than a tone.
        private static AudioClip _popClip = null;

        private static void PlayPop(Vector3 worldPos)
        {
            if ((object)_popClip == null) _popClip = BuildPopClip();
            GameObject go = new GameObject("SandboxConfettiPop");
            go.transform.position = worldPos;
            var src = go.AddComponent<AudioSource>();
            src.clip = _popClip;
            src.spatialBlend = 0f;
            src.volume = 1f;
            // Force highest priority (0 = highest, 256 = lowest, default 128) —
            // if the game's own landing/impact SFX are playing at the same
            // instant, Unity can silently virtualise (mute) lower-priority
            // voices when too many sounds compete. This guarantees ours wins.
            src.priority = 0;
            src.Play();
            UnityEngine.Object.Destroy(go, _popClip.length + 0.1f);
        }

        private static AudioClip BuildPopClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.55f; // was 0.3f — proper explosion length, not a quick pop
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            float prev = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;

                // Sharp opening "crack" — fast decay, wideband noise
                float crackEnv = Mathf.Exp(-t * 22f); // was 40f — hangs on a bit longer too
                float noise = Random.Range(-1f, 1f);
                float filtered = prev + 0.55f * (noise - prev); // was 0.5 — even less muffled
                prev = filtered;
                float crack = filtered * crackEnv;

                // Deep sub-bass "boom" — the actual explosion body, slower
                // decay so it rumbles rather than just ticking
                float boomFreq = Mathf.Lerp(120f, 38f, Mathf.Clamp01(t / 0.15f));
                float boomEnv = Mathf.Exp(-t * 6f); // was 14f — much longer rumble
                float boom = Mathf.Sin(2f * Mathf.PI * boomFreq * t) * boomEnv;

                // Low rumble noise layered under the boom for extra body
                float rumbleNoise = Random.Range(-1f, 1f) * Mathf.Exp(-t * 5f) * 0.35f;

                data[i] = Mathf.Clamp(crack * 0.6f + boom * 0.85f + rumbleNoise, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SandboxConfettiPopClip", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static void Reset()
        {
            Enabled = false;
        }
    }
}
