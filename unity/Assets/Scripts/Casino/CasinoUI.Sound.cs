using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Casino
{
    public partial class CasinoUI
    {
        enum Snd { Chip, Card, Spin, Tick, Drop, Thunk, Win }
        AudioSource sfx; readonly Dictionary<Snd, AudioClip> clips = new Dictionary<Snd, AudioClip>();

        /// <summary>The table's own little noises, made in code: chips clicking, cards snapping, the ball, the reels, a win.</summary>
        void Sfx(Snd s)
        {
            if (sfx == null) { sfx = gameObject.AddComponent<AudioSource>(); sfx.playOnAwake = false; sfx.spatialBlend = 0f; }
            if (!clips.TryGetValue(s, out var c)) { c = Make(s); clips[s] = c; }
            if (c != null) sfx.PlayOneShot(c, s == Snd.Tick ? 0.25f : s == Snd.Win ? 0.5f : 0.6f);
        }

        static AudioClip Make(Snd s)
        {
            const int sr = 22050; var r = new System.Random((int)s + 7);
            float len = s switch { Snd.Chip => 0.07f, Snd.Card => 0.09f, Snd.Spin => 0.6f, Snd.Tick => 0.03f, Snd.Drop => 0.25f, Snd.Thunk => 0.12f, _ => 0.7f };
            int n = (int)(sr * len); var d = new float[n]; float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr, k = 1f - i / (float)n, noise = (float)(r.NextDouble() * 2 - 1); float v;
                switch (s)
                {
                    case Snd.Chip: v = (Mathf.Sin(t * 2 * Mathf.PI * 3400f) * 0.5f + Mathf.Sin(t * 2 * Mathf.PI * 5100f) * 0.3f + noise * 0.3f) * Mathf.Exp(-t * 70f); break;
                    case Snd.Card: lp += (noise - lp) * 0.35f; v = lp * Mathf.Exp(-t * 45f) * 1.4f; break;
                    case Snd.Tick: v = (Mathf.Sin(t * 2 * Mathf.PI * 2600f) + noise * 0.5f) * Mathf.Exp(-t * 160f) * 0.6f; break;
                    case Snd.Spin: lp += (noise - lp) * 0.08f; v = lp * Mathf.Sin(Mathf.PI * i / n) * 1.2f; break;
                    case Snd.Drop: v = (Mathf.Sin(t * 2 * Mathf.PI * 1900f) * Mathf.Exp(-t * 30f) + noise * 0.2f * Mathf.Exp(-t * 60f)) * 0.7f; break;
                    case Snd.Thunk: v = Mathf.Sin(t * 2 * Mathf.PI * (140f - t * 300f)) * Mathf.Exp(-t * 30f) + noise * 0.15f * Mathf.Exp(-t * 80f); break;
                    default:
                    {
                        // a little rising arpeggio
                        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f }; int ni = Mathf.Min(3, (int)(t / 0.12f)); float nt = t - ni * 0.12f;
                        v = (Mathf.Sin(nt * 2 * Mathf.PI * notes[ni]) * 0.6f + Mathf.Sin(nt * 4 * Mathf.PI * notes[ni]) * 0.15f) * Mathf.Exp(-nt * (ni == 3 ? 4f : 12f)) * 0.7f;
                        break;
                    }
                }
                d[i] = Mathf.Clamp(v, -1f, 1f) * Mathf.Min(1f, k * 20f);
            }
            var c = AudioClip.Create("casino_" + s, n, 1, sr, false); c.SetData(d, 0); return c;
        }
    }
}
