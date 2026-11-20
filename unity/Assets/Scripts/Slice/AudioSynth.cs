using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>Procedural sound effects. Placeholders that stay responsive (engine pitch, siren) and can be swapped for recorded assets later.</summary>
    public static class AudioSynth
    {
        public const int SR = 32000;
        static System.Random rng = new System.Random(12345);
        static float N() => (float)(rng.NextDouble() * 2.0 - 1.0);

        static AudioClip Make(string name, float[] d, float peak = 0.8f)
        {
            float m = 1e-5f; for (int i = 0; i < d.Length; i++) m = Mathf.Max(m, Mathf.Abs(d[i]));
            float g = peak / m; for (int i = 0; i < d.Length; i++) d[i] *= g;
            var c = AudioClip.Create(name, d.Length, 1, SR, false); c.SetData(d, 0); return c;
        }

        static float[] Loopable(float[] src, int fade)
        {
            var o = new float[src.Length - fade];
            for (int i = 0; i < o.Length; i++) o[i] = src[i];
            for (int i = 0; i < fade; i++)
            {
                float t = i / (float)fade;
                o[i] = src[i] * t + src[o.Length + i] * (1f - t);
            }
            return o;
        }

        static AudioClip _engine, _road, _thud, _crack, _squelch, _wind, _fire, _sirenA;
        static AudioClip[] _steps = new AudioClip[8], _birds = new AudioClip[4];

        public static AudioClip Engine()
        {
            if (_engine != null) return _engine;
            int n = SR; var d = new float[n]; float f0 = 38f; // 38 whole cycles per second -> seamless loop
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR, ph = 2f * Mathf.PI * f0 * t, s = 0;
                for (int h = 1; h <= 16; h++) s += Mathf.Sin(ph * h + h * 0.7f) / (h * 0.9f + 0.4f);
                float pulse = 0.65f + 0.35f * Mathf.Sin(ph * 0.5f);   // firing pulses (half-cycle modulation, still loops)
                lp += (N() - lp) * 0.08f;
                d[i] = (float)System.Math.Tanh((s * pulse) * 0.9f) + lp * 0.12f;
            }
            _engine = Make("engine", Loopable(d, 800), 0.7f); return _engine;
        }

        public static AudioClip RoadNoise()
        {
            if (_road != null) return _road;
            int n = SR * 2; var d = new float[n]; float a = 0, b = 0;
            for (int i = 0; i < n; i++) { a += (N() - a) * 0.22f; b += (a - b) * 0.35f; d[i] = b; }
            _road = Make("road", Loopable(d, 3000), 0.6f); return _road;
        }

        public static AudioClip Thud()
        {
            if (_thud != null) return _thud;
            int n = (int)(SR * 0.6f); var d = new float[n]; float ph = 0, lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float f = 45f + 110f * Mathf.Exp(-t * 14f);
                ph += 2f * Mathf.PI * f / SR;
                lp += (N() - lp) * 0.12f;
                d[i] = Mathf.Sin(ph) * Mathf.Exp(-t * 8f) + lp * 0.8f * Mathf.Exp(-t * 28f);
            }
            _thud = Make("thud", d, 0.95f); return _thud;
        }

        public static AudioClip Crack()
        {
            if (_crack != null) return _crack;
            int n = (int)(SR * 0.28f); var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float ring = Mathf.Sin(2f * Mathf.PI * 1250f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 2300f * t) * 0.3f + Mathf.Sin(2f * Mathf.PI * 620f * t) * 0.4f;
                d[i] = (N() * Mathf.Exp(-t * 90f) + ring * Mathf.Exp(-t * 38f)) * (t < 0.002f ? t / 0.002f : 1f);
            }
            _crack = Make("crack", d, 0.9f); return _crack;
        }

        public static AudioClip Squelch()
        {
            if (_squelch != null) return _squelch;
            int n = (int)(SR * 0.7f); var d = new float[n]; float lp1 = 0, lp2 = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float cut = Mathf.Lerp(0.30f, 0.05f, t / 0.7f);
                float bubble = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * (24f + 18f * Mathf.Sin(t * 9f)) * t);
                lp1 += (N() - lp1) * cut; lp2 += (lp1 - lp2) * cut;
                d[i] = (lp1 - lp2) * 6f * bubble * Mathf.Exp(-t * 3.2f) * Mathf.Min(1f, t * 80f);
            }
            _squelch = Make("squelch", d, 0.8f); return _squelch;
        }

        public static AudioClip Step(int surface) // 0 asphalt/pavement, 1 grass, 2 gravel, 3 wood floor
        {
            surface = Mathf.Clamp(surface, 0, 3);
            int slot = surface * 2;
            var r = new System.Random(surface * 31 + 7);
            // two variants per surface
            int v = Random.Range(0, 2);
            if (_steps[slot + v] != null) return _steps[slot + v];
            int n = (int)(SR * (surface == 2 ? 0.28f : 0.16f)); var d = new float[n]; float lp = 0;
            float cut = surface == 0 ? 0.35f : surface == 1 ? 0.10f : surface == 2 ? 0.5f : 0.22f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float body = Mathf.Sin(2f * Mathf.PI * (surface == 3 ? 110f : 70f + v * 12f) * t) * Mathf.Exp(-t * 35f);
                float noise = 0;
                if (surface == 2) { if (((int)(t * 900f + v * 3)) % 7 == 0) noise = (float)(r.NextDouble() * 2 - 1); noise *= Mathf.Exp(-t * 9f); }
                else { lp += (((float)(r.NextDouble() * 2 - 1)) - lp) * cut; noise = lp * Mathf.Exp(-t * (surface == 1 ? 22f : 45f)); }
                d[i] = body * 0.8f + noise * (surface == 1 ? 1.2f : 1f);
            }
            _steps[slot + v] = Make("step" + surface + "_" + v, d, 0.5f); return _steps[slot + v];
        }

        public static AudioClip Wind()
        {
            if (_wind != null) return _wind;
            int n = SR * 6; var d = new float[n]; float a = 0, b = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                a += (N() - a) * 0.012f; b += (a - b) * 0.02f;
                float swell = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * t / 6f * 1f) * Mathf.Sin(2f * Mathf.PI * t / 6f * 2f + 1f);
                d[i] = b * swell;
            }
            _wind = Make("wind", Loopable(d, 6000), 0.5f); return _wind;
        }

        public static AudioClip Bird(int v)
        {
            v = Mathf.Clamp(v, 0, 3);
            if (_birds[v] != null) return _birds[v];
            int n = (int)(SR * 0.9f); var d = new float[n]; float ph = 0;
            var r = new System.Random(900 + v);
            int notes = 3 + v; float tpos = 0;
            for (int k = 0; k < notes; k++)
            {
                int start = (int)(SR * (0.05f + k * (0.7f / notes))); int len = (int)(SR * 0.09f);
                float f0 = 2400f + (float)r.NextDouble() * 1600f, f1 = f0 + ((float)r.NextDouble() - 0.4f) * 1800f;
                for (int i = 0; i < len && start + i < n; i++)
                {
                    float t = i / (float)len; ph += 2f * Mathf.PI * Mathf.Lerp(f0, f1, t) / SR;
                    d[start + i] += Mathf.Sin(ph + 0.4f * Mathf.Sin(t * 50f)) * Mathf.Sin(t * Mathf.PI);
                }
                tpos += 0;
            }
            _birds[v] = Make("bird" + v, d, 0.35f); return _birds[v];
        }

        public static AudioClip Fire()
        {
            if (_fire != null) return _fire;
            int n = SR * 4; var d = new float[n]; float lp = 0; var r = new System.Random(5);
            for (int i = 0; i < n; i++) { lp += (N() - lp) * 0.02f; d[i] = lp * 0.35f; }
            for (int p = 0; p < 40; p++)
            {
                int s = r.Next(0, n - 3000); float amp = 0.3f + (float)r.NextDouble() * 0.7f;
                for (int i = 0; i < 1800; i++) d[s + i] += (float)(r.NextDouble() * 2 - 1) * amp * Mathf.Exp(-i / 160f);
            }
            _fire = Make("fire", Loopable(d, 4000), 0.5f); return _fire;
        }
    }
}
