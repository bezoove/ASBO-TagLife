using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Procedural humanoid: jointed primitive body with walk/kneel animation, and a physics ragdoll
    /// (CharacterJoints) switched on when struck by a vehicle. Severe impacts dismember; only unimportant people can die.
    /// </summary>
    public class Person : MonoBehaviour
    {
        public enum State { Walking, Ragdoll, Settled, Carried }
        public bool isPlayer, important, isParamedic, agentDriven;
        public Color shirt = Color.gray, trousers = new Color(0.15f, 0.15f, 0.25f);
        public List<Vector3> route = new List<Vector3>();
        public float speed = 1.3f;
        public State state = State.Walking;
        public bool dead, injured;
        public string label = "Person";
        public event Action<Person> Settled;
        public static readonly List<Person> All = new List<Person>();
        public static Person LastHit;

        Transform pelvis, torso, head, uArmL, uArmR, lArmL, lArmR, uLegL, uLegR, lLegL, lLegR;
        class Bone { public Transform t; public Rigidbody rb; public Collider col; public Bone parent; public bool severed; }
        readonly List<Bone> bones = new List<Bone>();
        readonly Dictionary<Transform, Bone> byT = new Dictionary<Transform, Bone>();
        CapsuleCollider trigger; Rigidbody rootRb;
        float phase, amp, targetAmp, kneel, kneelTarget; int wp; bool stepped;
        float hitTime, calmTimer; float lastStepSign;
        static Material skin, shoe;

        public void Init()
        {
            if (skin == null) { skin = Gfx.Mat(new Color(0.85f, 0.65f, 0.52f), null, 0.2f); shoe = Gfx.Mat(new Color(0.08f, 0.06f, 0.05f), null, 0.3f); }
            Build();
            All.Add(this);
        }
        void OnDestroy() { All.Remove(this); }

        Bone AddBone(Transform t, Bone parent, float mass, Collider c)
        {
            var rb = t.gameObject.AddComponent<Rigidbody>();
            rb.mass = mass; rb.isKinematic = true; rb.linearDamping = 0.15f; rb.angularDamping = 0.9f;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            c.enabled = false;
            var b = new Bone { t = t, rb = rb, col = c, parent = parent };
            bones.Add(b); byT[t] = b; return b;
        }

        Transform Pivot(string n, Transform parent, Vector3 lp) { var g = new GameObject(n).transform; g.SetParent(parent, false); g.localPosition = lp; return g; }

        CapsuleCollider Limb(Transform piv, Vector3 center, float r, float h, Material m, string vis = "Mesh")
        {
            var g = Gfx.Prim(PrimitiveType.Capsule, piv, center, new Vector3(r * 2f, h / 2f, r * 2f), m, vis, false);
            var c = piv.gameObject.AddComponent<CapsuleCollider>();
            c.center = center; c.radius = r; c.height = h; c.direction = 1;
            return c;
        }

        void Build()
        {
            var shirtM = Gfx.Mat(shirt, null, 0.1f);
            var trM = Gfx.Mat(trousers, null, 0.1f);
            pelvis = Pivot("Pelvis", transform, new Vector3(0, 0.96f, 0));
            var pb = AddBone(pelvis, null, 10f, Limb(pelvis, new Vector3(0, 0.02f, 0), 0.16f, 0.32f, trM));
            torso = Pivot("Torso", pelvis, new Vector3(0, 0.12f, 0));
            var tb = AddBone(torso, pb, 14f, Limb(torso, new Vector3(0, 0.27f, 0), 0.19f, 0.62f, shirtM));
            head = Pivot("Head", torso, new Vector3(0, 0.6f, 0));
            var hc = head.gameObject.AddComponent<SphereCollider>(); hc.center = new Vector3(0, 0.12f, 0); hc.radius = 0.11f;
            Gfx.Prim(PrimitiveType.Sphere, head, new Vector3(0, 0.12f, 0), Vector3.one * 0.24f, skin, "Head", false);
            AddBone(head, tb, 5f, hc);
            uArmL = Arm("ArmL", -0.25f, shirtM, tb, out lArmL);
            uArmR = Arm("ArmR", 0.25f, shirtM, tb, out lArmR);
            uLegL = Leg("LegL", -0.1f, trM, pb, out lLegL);
            uLegR = Leg("LegR", 0.1f, trM, pb, out lLegR);

            if (!isPlayer)
            {
                trigger = gameObject.AddComponent<CapsuleCollider>();
                trigger.isTrigger = true; trigger.center = new Vector3(0, 0.9f, 0); trigger.radius = 0.35f; trigger.height = 1.8f;
                rootRb = gameObject.AddComponent<Rigidbody>(); rootRb.isKinematic = true;
            }
        }

        Transform Arm(string n, float x, Material m, Bone parent, out Transform lower)
        {
            var up = Pivot(n, torso, new Vector3(x, 0.5f, 0));
            var ub = AddBone(up, parent, 2f, Limb(up, new Vector3(0, -0.15f, 0), 0.055f, 0.32f, m));
            lower = Pivot(n + "Lower", up, new Vector3(0, -0.3f, 0));
            AddBone(lower, ub, 1.5f, Limb(lower, new Vector3(0, -0.15f, 0), 0.05f, 0.3f, skin));
            return up;
        }

        Transform Leg(string n, float x, Material m, Bone parent, out Transform lower)
        {
            var up = Pivot(n, pelvis, new Vector3(x, -0.05f, 0));
            var ub = AddBone(up, parent, 7f, Limb(up, new Vector3(0, -0.22f, 0), 0.08f, 0.46f, m));
            lower = Pivot(n + "Lower", up, new Vector3(0, -0.44f, 0));
            AddBone(lower, ub, 4f, Limb(lower, new Vector3(0, -0.22f, 0), 0.065f, 0.44f, m));
            Gfx.Box(lower, new Vector3(0, -0.45f, 0.06f), new Vector3(0.12f, 0.08f, 0.26f), shoe, 0, "Shoe", false);
            return up;
        }

        // ---------------- locomotion ----------------
        public void Animate(float spd) { targetAmp = Mathf.Clamp01(spd / 1.4f); phase += spd * Time.deltaTime * 4.2f; stepped = true; }
        public void SetKneel(bool k) { kneelTarget = k ? 1f : 0f; }

        public void Step(Vector3 target, float spd)
        {
            Vector3 d = target - transform.position; d.y = 0;
            if (d.sqrMagnitude < 0.0004f) return;
            Vector3 dir = d.normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
            transform.position += dir * Mathf.Min(spd * Time.deltaTime, d.magnitude);
            Animate(spd);
        }

        void Update()
        {
            if (state != State.Walking) return;
            if (route.Count > 0 && !isPlayer && !agentDriven)
            {
                Vector3 t = route[wp];
                if ((t - transform.position).WithY(0).magnitude < 0.3f) wp = (wp + 1) % route.Count;
                else Step(t, speed);
            }
        }

        void LateUpdate()
        {
            if (state == State.Walking)
            {
                if (!stepped) targetAmp = 0f;
                amp = Mathf.Lerp(amp, targetAmp, 8f * Time.deltaTime);
                kneel = Mathf.Lerp(kneel, kneelTarget, 5f * Time.deltaTime);
                float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
                if (amp > 0.3f && GameAudio.I != null && Mathf.Sign(s) != lastStepSign)
                {
                    lastStepSign = Mathf.Sign(s);
                    var cam = Camera.main;
                    if (cam != null && (cam.transform.position - transform.position).sqrMagnitude < 28f * 28f) GameAudio.I.Footstep(transform.position, isPlayer);
                }
                float swing = 32f * amp;
                Quaternion walkUL = Quaternion.Euler(s * swing, 0, 0), walkUR = Quaternion.Euler(-s * swing, 0, 0);
                Quaternion walkLL = Quaternion.Euler(Mathf.Max(0f, -c) * 38f * amp, 0, 0), walkLR = Quaternion.Euler(Mathf.Max(0f, c) * 38f * amp, 0, 0);
                uLegL.localRotation = Quaternion.Slerp(walkUL, Quaternion.Euler(-80f, 0, 0), kneel);
                uLegR.localRotation = Quaternion.Slerp(walkUR, Quaternion.Euler(-15f, 0, 0), kneel);
                lLegL.localRotation = Quaternion.Slerp(walkLL, Quaternion.Euler(165f, 0, 0), kneel);
                lLegR.localRotation = Quaternion.Slerp(walkLR, Quaternion.Euler(95f, 0, 0), kneel);
                uArmL.localRotation = Quaternion.Euler(-s * 24f * amp - 35f * kneel, 0, 0);
                uArmR.localRotation = Quaternion.Euler(s * 24f * amp - 35f * kneel, 0, 0);
                lArmL.localRotation = Quaternion.Euler(-10f - 30f * amp * Mathf.Max(0, s), 0, 0);
                lArmR.localRotation = Quaternion.Euler(-10f - 30f * amp * Mathf.Max(0, -s), 0, 0);
                torso.localRotation = Quaternion.Euler(25f * kneel, 0, 0);
                pelvis.localPosition = new Vector3(0, 0.96f - 0.43f * kneel + Mathf.Abs(c) * 0.025f * amp, 0);
                stepped = false;
            }
            else if (state == State.Ragdoll)
            {
                hitTime += Time.deltaTime;
                float v = 0f;
                foreach (var b in bones) if (!b.severed) v = Mathf.Max(v, b.rb.linearVelocity.magnitude);
                calmTimer = v < 0.25f ? calmTimer + Time.deltaTime : 0f;
                if ((calmTimer > 1.2f && hitTime > 1.5f) || hitTime > 12f) Settle();
            }
        }

        // ---------------- being hit ----------------
        void OnTriggerEnter(Collider other)
        {
            if (isPlayer || state != State.Walking) return;
            var car = other.GetComponentInParent<IVehicle>();
            if (car == null) return;
            float sp = car.Speed;
            if (sp < 1.5f) return;
            Vector3 v = car.Velocity;
            Hit(v, other.ClosestPoint(transform.position + Vector3.up), sp);
        }

        /// <summary>Struck by something moving with velocity v. severity in m/s of closing speed.</summary>
        public void Hit(Vector3 v, Vector3 point, float severity)
        {
            if (state != State.Walking) return;
            state = State.Ragdoll; hitTime = 0; calmTimer = 0; LastHit = this;
            label = important ? "Important person" : "Passer-by";
            // outcome: only unimportant people die
            if (severity >= 7f && !important) dead = true; else injured = true;
            trigger.enabled = false; rootRb.isKinematic = true;
            foreach (var b in bones) { b.t.localRotation = Quaternion.identity; }
            pelvis.localPosition = new Vector3(0, 0.96f, 0);
            foreach (var b in bones) b.col.enabled = true;
            foreach (var b in bones) if (b.parent != null) AddJoint(b);
            Vector3 flat = v.normalized;
            foreach (var b in bones)
            {
                b.rb.isKinematic = false;
                b.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                float h = b.t.position.y;
                // car pushes the low parts hard and tosses the upper body up and over the bonnet
                Vector3 impulse = v * UnityEngine.Random.Range(0.85f, 1.05f) + Vector3.up * Mathf.Clamp(severity * 0.32f, 1f, 5f) * Mathf.Clamp01(h / 1.2f);
                b.rb.linearVelocity = impulse;
                b.rb.angularVelocity = UnityEngine.Random.onUnitSphere * Mathf.Clamp(severity * 0.45f, 1f, 6f);
            }
            Gore.Burst(point, flat + Vector3.up * 0.3f, 90);
            if (GameAudio.I != null)
            {
                GameAudio.I.CarImpact(point, severity);
                GameAudio.I.PlayVoice("shout", point, 0.25f, 1f);
                GameAudio.I.PlayVoice("witness", point + new Vector3(8f, 0, 4f), 1.6f, 1f);
            }
            if (severity >= 8f) StartCoroutine(Dismember(severity));
            StartCoroutine(BleedWhileMoving());
            Ambulance.Dispatch(this);
        }

        void AddJoint(Bone b)
        {
            var j = b.t.gameObject.AddComponent<CharacterJoint>();
            j.connectedBody = b.parent.rb; j.anchor = Vector3.zero; j.enableProjection = true; j.enablePreprocessing = false;
            string n = b.t.name; float lo = -30, hi = 30, s1 = 30, s2 = 30; Vector3 axis = Vector3.right;
            if (n.StartsWith("Leg") && !n.Contains("Lower")) { lo = -100; hi = 30; s1 = 25; s2 = 35; }
            else if (n.StartsWith("Leg")) { lo = -4; hi = 140; s1 = 5; s2 = 5; }
            else if (n.StartsWith("Arm") && !n.Contains("Lower")) { lo = -120; hi = 60; s1 = 70; s2 = 60; }
            else if (n.StartsWith("Arm")) { lo = -140; hi = 4; s1 = 5; s2 = 5; }
            else if (n == "Head") { lo = -40; hi = 40; s1 = 30; s2 = 30; }
            else if (n == "Torso") { lo = -30; hi = 30; s1 = 25; s2 = 25; }
            j.axis = axis; j.swingAxis = Vector3.up;
            j.lowTwistLimit = new SoftJointLimit { limit = lo };
            j.highTwistLimit = new SoftJointLimit { limit = hi };
            j.swing1Limit = new SoftJointLimit { limit = s1 };
            j.swing2Limit = new SoftJointLimit { limit = s2 };
        }

        IEnumerator Dismember(float severity)
        {
            yield return new WaitForSeconds(0.12f);
            if (Gore.Level == 0) yield break;
            // lower leg or forearm comes away; heavy impacts can take a leg at the thigh
            Transform[] cand = { lLegL, lLegR, lArmL, lArmR, uLegL };
            Transform cut = cand[UnityEngine.Random.Range(0, severity > 12f ? cand.Length : 4)];
            if (!byT.TryGetValue(cut, out var b) || b.severed) yield break;
            b.severed = true;
            var j = cut.GetComponent<CharacterJoint>(); if (j != null) Destroy(j);
            // stump keeps spurting from the parent bone
            var stump = b.parent.t;
            Vector3 off = stump.InverseTransformPoint(cut.position);
            Gore.Spurt(stump, off, 4.5f);
            if (GameAudio.I != null) GameAudio.I.Gore(cut.position);
            Gore.Burst(cut.position, Vector3.up, 60);
            cut.SetParent(null, true);
            b.rb.linearVelocity += UnityEngine.Random.onUnitSphere * 2f;
            // hide the stump end by shrinking nothing; severed limb stays as a physics prop
            foreach (var k in bones) if (k.parent == b) { var jj = k.t.GetComponent<CharacterJoint>(); if (jj != null) jj.connectedBody = b.rb; }
            Destroy(cut.gameObject, 90f);
        }

        IEnumerator BleedWhileMoving()
        {
            float t = 0;
            while (state == State.Ragdoll && t < 6f)
            {
                t += 0.2f;
                if (Gore.Level > 0 && UnityEngine.Random.value < 0.7f)
                {
                    Vector3 p = pelvis.position;
                    if (Physics.Raycast(p + Vector3.up * 0.2f, Vector3.down, out var h, 3f)) Gore.Splat(h.point, h.normal, UnityEngine.Random.Range(0.1f, 0.3f));
                }
                yield return new WaitForSeconds(0.2f);
            }
        }

        void Settle()
        {
            state = State.Settled;
            foreach (var b in bones) { if (b.rb != null) { b.rb.linearVelocity = Vector3.zero; b.rb.angularVelocity = Vector3.zero; } }
            if (Physics.Raycast(pelvis.position + Vector3.up * 0.3f, Vector3.down, out var h, 3f))
                Gore.Pool(h.point, dead ? 1.15f : 0.55f, 14f);
            Settled?.Invoke(this);
        }

        public Vector3 BodyPos => pelvis != null ? pelvis.position : transform.position;

        /// <summary>Removes the body (picked up by ambulance crew).</summary>
        public void TakeAway()
        {
            state = State.Carried;
            foreach (var b in bones) if (b.t != null) b.t.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }

    public static class VExt { public static Vector3 WithY(this Vector3 v, float y) { v.y = y; return v; } }
}
