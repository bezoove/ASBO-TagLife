using UnityEngine;

namespace ASBO.Slice
{
    public class FireFlicker : MonoBehaviour
    {
        Light l; float seed;
        void Start()
        {
            l = GetComponent<Light>(); seed = Random.value * 100f;
            var src = GetComponent<AudioSource>();
            if (src != null && src.clip == null) { src.clip = FireClip ??= AudioSynth.Fire(); src.loop = true; src.Play(); }
        }
        static AudioClip FireClip;
        void Update() { l.intensity = 2.6f + Mathf.PerlinNoise(Time.time * 6f, seed) * 1.6f; }
    }
}
