using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Through-traffic on the long road. Drives on the left (UK), keeps distance, stops for anyone on the zebra
    /// or in the lane ahead, and becomes a physics wreck if rammed hard.
    /// </summary>
    public class TrafficCar : MonoBehaviour, IVehicle
    {
        public float dir = 1f;          // +1 eastbound (+x), -1 westbound
        public float laneZ, cruise = 12f;
        float speed; Rigidbody rb; Transform[] wheels; float wheelSpin; bool wrecked; float horn;
        AudioSource eng; float L;
        public float Speed => wrecked ? (rb != null ? rb.linearVelocity.magnitude : 0f) : speed;
        public Vector3 Velocity => wrecked ? rb.linearVelocity : transform.forward * speed;
        public static readonly List<TrafficCar> All = new List<TrafficCar>();
        static AudioClip engClip; static bool realEngine; float pitchBase = 1f;

        public static TrafficCar Spawn(Transform parent, float x, float dir, System.Random r)
        {
            var g = new GameObject("TrafficCar"); g.transform.SetParent(parent, false);
            var t = g.AddComponent<TrafficCar>();
            t.dir = dir; t.laneZ = dir > 0 ? World.RoadZ + 1.9f : World.RoadZ - 1.9f;
            g.transform.position = new Vector3(x, 0, t.laneZ);
            g.transform.rotation = Quaternion.Euler(0, dir > 0 ? 90f : -90f, 0);
            var spec = CarBody.Random(r);
            t.L = spec.L;
            t.wheels = CarBody.Build(g.transform, spec, spec.van ? CarBody.VanPaint(r) : CarBody.RandomPaint(r), true, r.Next());
            g.layer = Person.VehicleLayer; // bodies on the road pass through, not crushed
            var bc = g.AddComponent<BoxCollider>();
            float h = spec.van ? 2.0f : 1.4f;
            bc.center = new Vector3(0, 0.3f + h / 2f, 0); bc.size = new Vector3(1.7f, h, spec.L);
            t.rb = g.AddComponent<Rigidbody>(); t.rb.isKinematic = true; t.rb.mass = spec.van ? 2000f : 1200f; t.rb.interpolation = RigidbodyInterpolation.Interpolate;
            t.cruise = 11f + (float)r.NextDouble() * 3.5f; t.speed = t.cruise;
            if (engClip == null) { engClip = SfxBank.Pick("engine"); realEngine = engClip != null; if (engClip == null) engClip = AudioSynth.Engine(); }
            t.pitchBase = 0.9f + (float)r.NextDouble() * 0.2f;
            t.eng = g.AddComponent<AudioSource>(); t.eng.clip = engClip; t.eng.loop = true; t.eng.spatialBlend = 1f; t.eng.minDistance = 4f; t.eng.maxDistance = 60f; t.eng.volume = 0.18f;
            t.eng.time = (float)r.NextDouble() * engClip.length; t.eng.Play();
            All.Add(t);
            return t;
        }
        void OnDestroy() { All.Remove(this); }

        void FixedUpdate()
        {
            if (wrecked) return;
            float target = cruise;
            float front = transform.position.x + dir * L / 2f;
            // something ahead in our lane?
            float gap = 999f;
            foreach (var o in All) if (o != this && Mathf.Abs(o.laneZ - laneZ) < 1f && !o.wrecked) { float d = (o.transform.position.x - o.dir * o.L / 2f - front) * dir; if (d > 0 && d < gap) gap = d; }
            // an ambulance stopped (or crawling) in the lane: wait behind it
            foreach (var amb in Ambulance.All)
            {
                if (amb == null) continue; Vector3 ap = amb.transform.position;
                if (Mathf.Abs(ap.z - laneZ) > 1.9f) continue;
                float d = (ap.x - dir * amb.HalfLength - front) * dir; if (d > -1f && d < gap) gap = Mathf.Max(0f, d);
            }
            foreach (var p in Person.All)
            {
                if (p == null) continue;
                Vector3 pp = p.BodyPos;
                if (Mathf.Abs(pp.z - laneZ) > 1.6f) continue;
                float d = (pp.x - front) * dir; if (d > -0.5f && d < gap) gap = d;
            }
            var player = FindPlayerCar();
            if (player != null)
            {
                Vector3 cp = player.transform.position;
                if (Mathf.Abs(cp.z - laneZ) < 1.75f) { float d = (cp.x - front) * dir - 2.2f; if (d > -1f && d < gap) gap = d; }
            }
            // zebra at x=18: stop before it if anyone is on the crossing
            float zebraStop = 18f - dir * 4.5f;
            float toZebra = (zebraStop - front) * dir;
            if (toZebra > 0 && toZebra < 30f && ZebraBusy()) gap = Mathf.Min(gap, toZebra + 2f);
            float stopDist = gap - 2.5f;
            if (stopDist < 25f) target = Mathf.Min(target, Mathf.Max(0f, Mathf.Sqrt(Mathf.Max(0f, stopDist) * 2f * 5f)));
            float acc = target > speed ? 2.2f : 7f;
            speed = Mathf.MoveTowards(speed, target, acc * Time.fixedDeltaTime);
            if (stopDist < 3f && gap < 6f && speed < 0.5f) { horn -= Time.fixedDeltaTime; if (horn < 0f) { horn = 6f + Random.value * 6f; if (GameAudio.I != null) GameAudio.I.Play3D(AudioSynth.Thud(), transform.position, 0.0f); } }
            Vector3 next = transform.position + Vector3.right * dir * speed * Time.fixedDeltaTime;
            // wrap at the ends of the road
            if (next.x * dir > 230f) next.x = -230f * dir;
            next.z = laneZ; next.y = 0;
            rb.MovePosition(next);
        }

        static Car playerCar;
        // (looked up once: searching the scene every physics step for a car that isn't there was a real cost)
        static bool lookedForCar;
        static Component FindPlayerCar()
        {
            if (PlayerCar.I != null) return PlayerCar.I;
            if (!lookedForCar) { lookedForCar = true; playerCar = FindFirstObjectByType<Car>(); }
            return playerCar;
        }
        static bool ZebraBusy()
        {
            foreach (var p in Person.All) { if (p == null || p.state != Person.State.Walking) continue; var q = p.BodyPos; if (q.x > 16.2f && q.x < 19.8f && Mathf.Abs(q.z - World.RoadZ) < 5.2f) return true; }
            return false;
        }

        void Update()
        {
            wheelSpin += Speed * Time.deltaTime / 0.31f * Mathf.Rad2Deg;
            if (!wrecked && wheels != null) foreach (var w in wheels) w.localRotation = Quaternion.Euler(wheelSpin, 0, 90f);
            if (eng != null) { eng.pitch = realEngine ? pitchBase * (0.95f + Speed * 0.035f) : 0.55f + Speed * 0.05f; }
        }

        void OnCollisionEnter(Collision c)
        {
            if (wrecked) return;
            float rel = c.relativeVelocity.magnitude;
            if (rel < 4f) return;
            Wreck(c.relativeVelocity);
        }

        public void Wreck(Vector3 impulseDir)
        {
            wrecked = true;
            rb.isKinematic = false; rb.linearVelocity = transform.forward * speed; rb.angularDamping = 0.6f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (GameAudio.I != null) GameAudio.I.CarCrash(transform.position, impulseDir.magnitude);
            if (eng != null) eng.volume = 0.05f;
        }
    }

    public static class Traffic
    {
        public static void Spawn(Transform root)
        {
            var r = new System.Random(31);
            var g = new GameObject("Traffic").transform; g.SetParent(root, false);
            float[] east = { -200f, -130f, -60f, 70f }, west = { 190f, 120f, 40f, -90f };
            foreach (var x in east) TrafficCar.Spawn(g, x, 1f, r);
            foreach (var x in west) TrafficCar.Spawn(g, x, -1f, r);
        }
    }
}
