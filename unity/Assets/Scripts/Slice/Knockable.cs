using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Something loose you can send flying: dustbins, garden gnomes, plant pots. A kick or punch that finds nobody to
    /// hit lands on the nearest one in front of you; cars and people barge them about, and they clatter when they hit
    /// something.
    /// </summary>
    public class Knockable : MonoBehaviour
    {
        public static readonly List<Knockable> All = new List<Knockable>();
        Rigidbody rb; float lastSound; bool metal;

        void OnEnable() { All.Add(this); rb = GetComponent<Rigidbody>(); metal = name.Contains("trash"); }
        void OnDisable() { All.Remove(this); }

        /// <summary>A blow that found no one: the nearest loose thing within reach in front takes it. True if something did.</summary>
        public static bool Strike(Vector3 from, Vector3 fwd, float reach, float impulse)
        {
            Knockable best = null; float bd = 99f; fwd.y = 0f; fwd.Normalize();
            foreach (var k in All)
            {
                if (k == null || k.rb == null) continue;
                Vector3 d = k.rb.worldCenterOfMass - from; if (Mathf.Abs(d.y) > 1.3f) continue; d.y = 0f;
                float dist = d.magnitude; if (dist > reach + 0.45f || Vector3.Angle(fwd, d) > 55f) continue;
                if (dist < bd) { bd = dist; best = k; }
            }
            if (best == null) return false;
            best.rb.WakeUp();
            best.rb.AddForceAtPosition((fwd + Vector3.up * 0.35f).normalized * impulse, best.rb.worldCenterOfMass + Vector3.up * 0.12f, ForceMode.Impulse);
            best.Clatter(impulse / Mathf.Max(1f, best.rb.mass) * 2f);
            return true;
        }

        /// <summary>Walked into: a gentle shove the way you're going.</summary>
        public void Nudge(Vector3 dir, float speed)
        {
            if (rb == null) return;
            dir.y = 0f; rb.WakeUp();
            rb.AddForceAtPosition(dir.normalized * Mathf.Min(speed, 4f) * rb.mass * 0.35f, rb.worldCenterOfMass + Vector3.up * 0.2f, ForceMode.Impulse);
        }

        /// <summary>Set when the player throws it: whoever threw it, and when.</summary>
        public Transform thrower; public float thrownAt = -99f;
        public bool glass; public float loudBase = 10f; float lastNoise;

        void OnCollisionEnter(Collision c)
        {
            float v = c.relativeVelocity.magnitude; if (v > 1.6f) Clatter(v);
            if (v < 3f || Time.time - lastNoise < 0.5f) return; lastNoise = Time.time;
            bool mine = thrower != null && Time.time - thrownAt < 8f;
            float loud = Mathf.Clamp(loudBase * (0.4f + v * 0.08f) * Mathf.Pow(Mathf.Max(0.2f, rb != null ? rb.mass : 1f), 0.2f), 3f, 45f);
            if (glass && v > 4.5f) { Smash(); loud *= 1.6f; }
            NoiseBus.Emit(transform.position, loud, mine ? thrower : null);
        }

        /// <summary>A glass thing hitting something hard: gone in a handful of shards.</summary>
        void Smash()
        {
            var mr = GetComponent<MeshRenderer>(); var mat = mr != null && mr.sharedMaterials.Length > 0 ? mr.sharedMaterials[mr.sharedMaterials.Length - 1] : null;
            if (GameAudio.I != null) GameAudio.I.Knock(transform.position, 14f, false);
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx"); Vector3 at = transform.position + Vector3.up * 0.05f;
            for (int i = 0; i < 7; i++)
            {
                var s = new GameObject("shard"); s.transform.position = at + Random.insideUnitSphere * 0.05f; s.transform.localScale = new Vector3(0.03f, 0.004f, 0.02f) * Random.Range(0.7f, 1.6f);
                s.AddComponent<MeshFilter>().sharedMesh = cube; var r = s.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var b = s.AddComponent<Rigidbody>(); b.mass = 0.01f; b.linearVelocity = Random.insideUnitSphere * 2.2f + Vector3.up * 1.2f; b.angularVelocity = Random.insideUnitSphere * 12f;
                s.AddComponent<BoxCollider>(); Object.Destroy(s, 5f);
            }
            Object.Destroy(gameObject);
        }

        void Clatter(float v)
        {
            if (Time.time - lastSound < 0.18f || GameAudio.I == null) return;
            lastSound = Time.time; GameAudio.I.Knock(transform.position, v, metal);
        }
    }
}
