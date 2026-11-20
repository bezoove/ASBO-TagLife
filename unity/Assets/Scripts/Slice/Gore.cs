using System.Collections;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>Blood: pooled splatter decals, droplet bursts, arterial spurts and pools. Intensity from Gore.Level.</summary>
    public static class Gore
    {
        public static int Level = 2; // 0 off, 1 reduced, 2 full
        const int MaxDecals = 160;
        static Transform decalRoot;
        static GameObject[] decals = new GameObject[MaxDecals];
        static int decalIdx;
        static Material bloodMat, bloodGloss, particleMat;

        static void Init()
        {
            if (bloodMat != null) return;
            bloodMat = Gfx.Mat(Color.white, SplatTex(), 0.6f);
            bloodMat.SetFloat("_AlphaClip", 1f); bloodMat.EnableKeyword("_ALPHATEST_ON"); bloodMat.SetFloat("_Cutoff", 0.5f); bloodMat.renderQueue = 2450;
            bloodGloss = Gfx.Mat(new Color(0.28f, 0.01f, 0.02f), null, 0.95f);
            particleMat = new Material(Gfx.ParticleUnlit);
            particleMat.SetColor("_BaseColor", new Color(0.5f, 0.02f, 0.04f));
            decalRoot = new GameObject("BloodDecals").transform;
        }

        static Texture2D splat;
        static Texture2D SplatTex()
        {
            if (splat != null) return splat;
            const int S = 64; var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x - S / 2f) / (S / 2f), dy = (y - S / 2f) / (S / 2f);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float edge = 0.45f + 0.35f * Mathf.PerlinNoise(Mathf.Cos(ang) * 1.5f + 3f, Mathf.Sin(ang) * 1.5f + 3f) + 0.12f * Mathf.Sin(ang * 5f);
                    bool inside = r < edge || (Mathf.PerlinNoise(x * 0.35f, y * 0.35f) > 0.72f && r < 0.95f);
                    float shade = 0.7f + 0.3f * Mathf.PerlinNoise(x * 0.2f, y * 0.2f);
                    px[y * S + x] = inside ? new Color(0.38f * shade, 0.02f, 0.03f, 1f) : new Color(0, 0, 0, 0);
                }
            splat = new Texture2D(S, S, TextureFormat.RGBA32, true); splat.SetPixels32(px); splat.Apply(true);
            splat.wrapMode = TextureWrapMode.Clamp; return splat;
        }

        public static void Reset()
        {
            if (decalRoot != null) Object.Destroy(decalRoot.gameObject);
            bloodMat = null; decals = new GameObject[MaxDecals]; decalIdx = 0;
        }

        public static void Splat(Vector3 point, Vector3 normal, float size)
        {
            if (Level == 0) return;
            Init();
            if (decals[decalIdx] != null) Object.Destroy(decals[decalIdx]);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(decalRoot, true);
            q.transform.position = point + normal * 0.012f;
            q.transform.rotation = Quaternion.LookRotation(-normal) * Quaternion.Euler(0, 0, Random.Range(0, 360f));
            q.transform.localScale = new Vector3(size * Random.Range(0.7f, 1.4f), size * Random.Range(0.7f, 1.4f), 1f);
            q.GetComponent<Renderer>().sharedMaterial = bloodMat;
            decals[decalIdx] = q; decalIdx = (decalIdx + 1) % MaxDecals;
        }

        public static ParticleSystem MakeSystem(Transform parent, bool loop)
        {
            Init();
            var go = new GameObject("BloodFX");
            if (parent != null) go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = loop; main.playOnAwake = false;
            main.startLifetime = 1.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
            main.gravityModifier = 1.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Level == 1 ? 150 : 400;
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 22f; sh.radius = 0.02f;
            var col = ps.collision; col.enabled = true; col.type = ParticleSystemCollisionType.World; col.mode = ParticleSystemCollisionMode.Collision3D;
            col.dampen = 0.9f; col.bounce = 0.02f; col.lifetimeLoss = 0.6f; col.sendCollisionMessages = true;
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = particleMat;
            go.AddComponent<BloodCollider>();
            return ps;
        }

        /// <summary>Droplet burst at point travelling roughly along dir.</summary>
        public static void Burst(Vector3 point, Vector3 dir, int count)
        {
            if (Level == 0) return;
            if (Level == 1) count /= 2;
            var ps = MakeSystem(null, false);
            ps.transform.position = point; ps.transform.rotation = Quaternion.LookRotation(dir == Vector3.zero ? Vector3.up : dir);
            var sh = ps.shape; sh.angle = 40f;
            ps.Emit(count);
            Object.Destroy(ps.gameObject, 4f);
        }

        /// <summary>Pulsing arterial spurt attached to a stump.</summary>
        public static void Spurt(Transform stump, Vector3 localOffset, float seconds)
        {
            if (Level == 0) return;
            var ps = MakeSystem(stump, true);
            ps.transform.localPosition = localOffset; ps.transform.localRotation = Quaternion.Euler(-90f, 0, 0);
            var sh = ps.shape; sh.angle = 10f;
            var main = ps.main; main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
            var s = ps.gameObject.AddComponent<SpurtPulse>(); s.ps = ps; s.life = seconds;
            ps.Play();
        }

        public static void Pool(Vector3 groundPos, float finalRadius, float growSeconds)
        {
            if (Level == 0) return;
            Init();
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(decalRoot, true);
            g.transform.position = groundPos + Vector3.up * 0.014f;
            g.transform.localScale = new Vector3(0.2f, 0.004f, 0.2f);
            g.GetComponent<Renderer>().sharedMaterial = bloodGloss;
            g.transform.rotation = Quaternion.Euler(0, Random.Range(0f,360f), 0);
            g.AddComponent<PoolGrow>().Init(finalRadius * 1.3f, growSeconds);
        }
    }

    public class BloodCollider : MonoBehaviour
    {
        static readonly System.Collections.Generic.List<ParticleCollisionEvent> ev = new System.Collections.Generic.List<ParticleCollisionEvent>();
        ParticleSystem ps;
        void Awake() { ps = GetComponent<ParticleSystem>(); }
        void OnParticleCollision(GameObject other)
        {
            int n = ps.GetCollisionEvents(other, ev);
            for (int i = 0; i < n; i++)
                if (Random.value < 0.35f) Gore.Splat(ev[i].intersection, ev[i].normal, Random.Range(0.08f, 0.28f));
        }
    }

    public class SpurtPulse : MonoBehaviour
    {
        public ParticleSystem ps; public float life; float t;
        void Update()
        {
            t += Time.deltaTime;
            var em = ps.emission;
            float pulse = Mathf.Max(0f, Mathf.Sin(t * 9f));
            float fade = Mathf.Clamp01(1f - t / life);
            em.rateOverTime = 160f * pulse * fade;
            if (t > life) { em.rateOverTime = 0; Destroy(gameObject, 3f); enabled = false; }
        }
    }

    public class PoolGrow : MonoBehaviour
    {
        float target, dur, t;
        public void Init(float diameter, float seconds) { target = diameter; dur = seconds; }
        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0.15f, 1f, Mathf.Clamp01(t / dur));
            transform.localScale = new Vector3(target * k * 1.25f, 0.004f, target * k * 0.8f);
            if (t > dur) enabled = false;
        }
    }
}
