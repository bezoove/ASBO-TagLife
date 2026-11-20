using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>3D audio manager: pooled sources, ambience, footsteps, impacts, indoor muffling and the voice bank.</summary>
    public class GameAudio : MonoBehaviour
    {
        public static GameAudio I;
        public static readonly List<Transform> Interiors = new List<Transform>();
        public class Zone { public Transform t; public List<CRect> rects; public float h; }
        public static readonly List<Zone> Zones = new List<Zone>();
        public static void RegisterZone(Transform t, List<CRect> rects, float h) { Zones.Add(new Zone { t = t, rects = rects, h = h }); }
        readonly List<AudioSource> pool = new List<AudioSource>();
        AudioSource wind; AudioLowPassFilter lowpass; float nextBird;
        static Dictionary<string, List<AudioClip>> voices;
        public bool ListenerInside { get; private set; }

        public static void Create(Transform parent)
        {
            Interiors.Clear(); Zones.Clear();
            var g = new GameObject("GameAudio"); g.transform.SetParent(parent, false);
            I = g.AddComponent<GameAudio>();
        }

        void Awake()
        {
            for (int i = 0; i < 28; i++)
            {
                var go = new GameObject("Src" + i); go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>(); s.playOnAwake = false; s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Logarithmic;
                pool.Add(s);
            }
            wind = gameObject.AddComponent<AudioSource>();
            wind.clip = AudioSynth.Wind(); wind.loop = true; wind.spatialBlend = 0f; wind.volume = 0.22f; wind.Play();
            LoadVoices();
        }

        static void LoadVoices()
        {
            if (voices != null) return;
            voices = new Dictionary<string, List<AudioClip>>();
            foreach (var c in Resources.LoadAll<AudioClip>("Voices"))
            {
                string key = c.name.Contains("_") ? c.name.Substring(0, c.name.IndexOf('_')) : c.name;
                if (!voices.TryGetValue(key, out var l)) voices[key] = l = new List<AudioClip>();
                l.Add(c);
            }
        }

        public static AudioClip Voice(string cat, int index = -1)
        {
            LoadVoices();
            if (!voices.TryGetValue(cat, out var l) || l.Count == 0) return null;
            return l[index >= 0 ? index % l.Count : Random.Range(0, l.Count)];
        }

        public AudioSource Play3D(AudioClip clip, Vector3 pos, float vol = 1f, float pitch = 1f, float maxDist = 70f, float minDist = 3f)
        {
            if (clip == null || this == null || pool == null || pool.Count == 0) return null;
            AudioSource best = null;
            foreach (var s in pool) if (s != null && !s.isPlaying) { best = s; break; }
            if (best == null) { float old = 1e9f; foreach (var s in pool) { if (s == null) continue; float rem = (s.clip != null ? s.clip.length : 0f) - s.time; if (rem < old) { old = rem; best = s; } } }
            if (best == null) return null;
            best.transform.position = pos; best.clip = clip; best.volume = vol; best.pitch = pitch;
            best.minDistance = minDist; best.maxDistance = maxDist; best.Play();
            return best;
        }

        public void PlayVoice(string cat, Vector3 pos, float delay = 0f, float vol = 1f, int index = -1)
        {
            var clip = Voice(cat, index);
            if (clip != null) StartCoroutine(Delayed(clip, pos, delay, vol));
        }

        System.Collections.IEnumerator Delayed(AudioClip c, Vector3 p, float d, float v)
        {
            if (d > 0f) yield return new WaitForSeconds(d);
            Play3D(c, p, v, 1f, 45f, 4f);
        }

        public static int SurfaceAt(Vector3 p)
        {
            if (I != null && I.InsideAny(p)) return 3;
            if (Mathf.Abs(p.z - World.RoadZ) < 7.5f) return 0;
            if (p.x > -2f && p.x < 4f && p.z > -20f && p.z < 12f) return 2;
            return 1;
        }

        public void Footstep(Vector3 pos, bool close)
        {
            int surf = SurfaceAt(pos);
            Play3D(AudioSynth.Step(surf), pos, close ? 0.55f : 0.35f, Random.Range(0.9f, 1.1f), 25f, 2f);
        }

        public void CarImpact(Vector3 pos, float severity)
        {
            float v = Mathf.Clamp01(severity / 14f);
            Play3D(AudioSynth.Thud(), pos, 0.6f + 0.4f * v, Random.Range(0.85f, 1.1f), 90f, 6f);
            Play3D(AudioSynth.Crack(), pos, 0.5f + 0.4f * v, Random.Range(0.9f, 1.1f), 70f, 4f);
            if (severity > 5f) StartCoroutine(Later(0.15f, () => Play3D(AudioSynth.Squelch(), pos, 0.7f, Random.Range(0.9f, 1.1f), 40f, 3f)));
        }

        public void Gore(Vector3 pos)
        {
            Play3D(AudioSynth.Crack(), pos, 0.9f, 0.8f, 60f, 4f);
            StartCoroutine(Later(0.1f, () => Play3D(AudioSynth.Squelch(), pos, 1f, 0.85f, 40f, 3f)));
        }

        System.Collections.IEnumerator Later(float t, System.Action a) { yield return new WaitForSeconds(t); a(); }

        public bool InsideAny(Vector3 p)
        {
            foreach (var z in Zones)
            {
                if (z.t == null) continue;
                Vector3 l = z.t.InverseTransformPoint(p);
                if (l.y < -0.5f || l.y > z.h) continue;
                foreach (var r in z.rects) if (r.In(l.x, l.z, -0.3f)) return true;
            }
            foreach (var t in Interiors)
            {
                if (t == null) continue;
                Vector3 l = t.InverseTransformPoint(p);
                if (Mathf.Abs(l.x) < 4.1f && Mathf.Abs(l.z) < 2.5f && l.y > -0.5f && l.y < 3f) return true;
            }
            return false;
        }

        void Update()
        {
            var cam = Camera.main; if (cam == null) return;
            if (lowpass == null) { lowpass = cam.GetComponent<AudioLowPassFilter>(); if (lowpass == null) lowpass = cam.gameObject.AddComponent<AudioLowPassFilter>(); lowpass.cutoffFrequency = 22000f; }
            ListenerInside = InsideAny(cam.transform.position);
            lowpass.cutoffFrequency = Mathf.Lerp(lowpass.cutoffFrequency, ListenerInside ? 1700f : 22000f, 6f * Time.deltaTime);
            wind.volume = Mathf.Lerp(wind.volume, ListenerInside ? 0.05f : 0.22f, 2f * Time.deltaTime);
            if (Time.time > nextBird && !ListenerInside)
            {
                nextBird = Time.time + Random.Range(3f, 9f);
                Vector3 p = cam.transform.position + new Vector3(Random.Range(-30f, 30f), Random.Range(4f, 12f), Random.Range(-30f, 30f));
                Play3D(AudioSynth.Bird(Random.Range(0, 4)), p, 0.35f, Random.Range(0.9f, 1.2f), 70f, 5f);
            }
        }
    }

    /// <summary>Engine, road and tyre sound for a car, pitch following speed and throttle.</summary>
    public class EngineAudio : MonoBehaviour
    {
        AudioSource eng, road; Car car; float rev;
        void Start()
        {
            car = GetComponent<Car>();
            eng = gameObject.AddComponent<AudioSource>(); eng.clip = AudioSynth.Engine(); eng.loop = true; eng.spatialBlend = 1f; eng.minDistance = 6f; eng.maxDistance = 80f; eng.volume = 0.3f; eng.Play();
            var rg = new GameObject("RoadNoise"); rg.transform.SetParent(transform, false);
            road = rg.AddComponent<AudioSource>(); road.clip = AudioSynth.RoadNoise(); road.loop = true; road.spatialBlend = 1f; road.minDistance = 5f; road.maxDistance = 60f; road.volume = 0f; road.Play();
        }
        void Update()
        {
            float sp = car.Speed;
            float thr = car.occupied && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.wKey.isPressed ? 1f : 0f;
            float target = 0.6f + Mathf.Min(sp, 36f) * 0.055f + thr * 0.35f;
            rev = Mathf.Lerp(rev, target, 4f * Time.deltaTime);
            eng.pitch = rev;
            eng.volume = (car.occupied ? 0.45f : 0.22f) + thr * 0.25f;
            road.volume = Mathf.Clamp01(sp / 20f) * 0.45f; road.pitch = 0.8f + sp * 0.02f;
        }
    }
}
