using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// A person built on Unity's Humanoid animation system: imported rigged character (LOD0-2) + shared
    /// Animator Controller (mocap locomotion blend tree, talk/wave/kneel/jump). On a car strike the Animator
    /// hands over to a physics ragdoll built from the humanoid bones (Unity Ragdoll-Wizard style).
    /// Only unimportant people can die; severe impacts can sever limbs.
    /// </summary>
    public class Person : MonoBehaviour
    {
        public enum State { Walking, Ragdoll, Settled, Carried, GettingUp }
        public bool isPlayer, important, isParamedic, agentDriven;
        public Color shirt = Color.gray, trousers = new Color(0.15f, 0.15f, 0.25f);
        public List<Vector3> route = new List<Vector3>();
        public float speed = 1.3f;
        public State state = State.Walking;
        public bool dead, injured;
        public bool drunk; int variant;
        /// <summary>Will pick themselves up after going down (light knock from a car, or punched out).</summary>
        bool getUp;
        /// <summary>Hurt: bent over and limping (after getting up) until hurtUntil.</summary>
        public bool hurt; float hurtUntil; RuntimeAnimatorController normalCtrl; static RuntimeAnimatorController hurtCtrl;
        public float HurtSpeed => 0.66f;
        public string label = "Person";
        public string model; public Color skinTone = Color.white, hairColor = new Color(0.25f, 0.17f, 0.1f); public bool male = true;
        public event Action<Person> Settled;
        public static readonly List<Person> All = new List<Person>();
        public static Person LastHit;

        public Animator anim;
        GameObject body; SkinnedMeshRenderer[] smrs;
        class Bone { public Transform t; public Rigidbody rb; public Collider col; public Bone parent; public bool severed; }
        readonly List<Bone> bones = new List<Bone>();
        readonly Dictionary<Transform, Bone> byT = new Dictionary<Transform, Bone>();
        CapsuleCollider trigger; Rigidbody rootRb;
        float animSpeed, wantSpeed, stepT, hitTime, calmTimer; bool moved; int wp;
        static RuntimeAnimatorController ctrl;
        Transform hips;

        public void Init()
        {
            Build();
            All.Add(this);
            gameObject.AddComponent<Fighter>(); // everyone can scrap, paramedics included
        }
        void OnDestroy() { All.Remove(this); }

        // ---------------------------------------------------------------- build
        // ---------------------------------------------------------------- UMA body (free-pack test)
        bool uma; Vector2 wantLocal, curLocal; bool localSet;
        public bool Ready => anim != null;
        void BuildUma()
        {
            uma = true;
            int h = isPlayer ? 20261120 : Mathf.Abs(GetInstanceID() * 7919 + (int)(transform.position.x * 13f)); // the player always looks the same
            male = isPlayer || (string.IsNullOrEmpty(model) ? (h % 2) == 1 : model.StartsWith("m_"));
            if (isParamedic || label == "Paramedic") male = (h % 3) != 0;
            // about one bloke in five on the estate is half-cut, all day
            drunk = !isPlayer && male && !isParamedic && label != null && label.Contains("@") && (h / 7) % 5 == 0;
            variant = isPlayer ? 0 : (h / 3) % 3;
            UmaBody.Create(this, male, h, isPlayer, d =>
            {
                anim = d.animator != null ? d.animator : GetComponentInChildren<Animator>();
                if (anim == null) return;
                anim.applyRootMotion = false;
                // which idle and which way of talking this person has (0, 0.5, 1 pick clips in the controller's blends)
                foreach (var prm in anim.parameters) if (prm.name == "Variant") { anim.SetFloat("Variant", variant * 0.5f); break; }
                hips = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Hips) : anim.transform;
                smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var r in smrs) r.updateWhenOffscreen = false;
            }, isParamedic || label == "Paramedic" ? "Paramedic" : null);
            if (!isPlayer)
            {
                trigger = gameObject.AddComponent<CapsuleCollider>();
                trigger.isTrigger = true; trigger.center = new Vector3(0, 0.9f, 0); trigger.radius = 0.33f; trigger.height = 1.8f;
                rootRb = gameObject.AddComponent<Rigidbody>(); rootRb.isKinematic = true;
            }
        }

        /// <summary>Player: local-space velocity (x = right, y = forward) in m/s, drives the 8-direction blend.</summary>
        public void SetMove(Vector2 local) { wantLocal = local; localSet = true; moved = true; wantSpeed = local.magnitude; }
        public void TurnInPlace(bool left) { if (anim != null && anim.enabled && uma) anim.SetTrigger(left ? "TurnL" : "TurnR"); }
        public void SetGrounded(bool g) { if (anim != null && anim.enabled && uma) anim.SetBool("Grounded", g); }

        void Build()
        {
            if (FreePackTest.On) { BuildUma(); return; }
            ChooseModel();
            var prefab = Resources.Load<GameObject>("Characters/" + model) ?? Resources.Load<GameObject>("Characters/m_young_tee_short");
            body = Instantiate(prefab, transform);
            body.name = "Body"; body.transform.localPosition = Vector3.zero; body.transform.localRotation = Quaternion.identity;
            anim = body.GetComponent<Animator>(); if (anim == null) anim = body.AddComponent<Animator>();
            if (ctrl == null) ctrl = Resources.Load<RuntimeAnimatorController>("Anim/Humanoid");
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.cullingMode = isPlayer ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullUpdateTransforms;
            anim.SetFloat("Variant", UnityEngine.Random.value);
            if (ctrl != null) anim.Play("Locomotion", 0, UnityEngine.Random.value);
            hips = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Hips) : body.transform;
            smrs = body.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            bool stubble = male && (GetInstanceID() & 3) != 0;
            foreach (var r in smrs)
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    string n = ms[i] != null ? ms[i].name.ToLowerInvariant() : "";
                    if (n.StartsWith("skin")) ms[i] = CharModel.Skin(male, stubble, skinTone);
                    else if (n.StartsWith("haircards")) ms[i] = CharModel.HairCards(hairColor);
                    else if (n.StartsWith("hair")) ms[i] = CharModel.Hair(hairColor);
                    else if (n.StartsWith("top")) ms[i] = CharModel.Cloth(shirt);
                    else if (n.StartsWith("bottom")) ms[i] = CharModel.Cloth(trousers);
                    else if (n.StartsWith("shoe")) ms[i] = CharModel.Shoes(isParamedic ? new Color(0.05f, 0.05f, 0.05f) : new Color(0.13f, 0.09f, 0.06f));
                    else if (n.StartsWith("eye")) ms[i] = CharModel.Eyes();
                    else if (n.StartsWith("lash")) ms[i] = CharModel.Lashes();
                }
                r.sharedMaterials = ms;
                r.updateWhenOffscreen = false;
            }
            if (!isPlayer)
            {
                trigger = gameObject.AddComponent<CapsuleCollider>();
                trigger.isTrigger = true; trigger.center = new Vector3(0, 0.9f, 0); trigger.radius = 0.33f; trigger.height = 1.8f;
                rootRb = gameObject.AddComponent<Rigidbody>(); rootRb.isKinematic = true;
            }
        }

        void ChooseModel()
        {
            int h = Mathf.Abs(GetInstanceID() * 7919 + (int)(transform.position.x * 13f));
            if (string.IsNullOrEmpty(model))
            {
                if (isPlayer) model = "m_young_tee_short";
                else if (isParamedic || label == "Paramedic") model = (h % 3 == 0) ? "f_curvy_jacket_bun" : "m_fit_jacket_short";
                else { bool f = (h % 2) == 0; var list = f ? CharModel.Female : CharModel.Male; model = list[(h / 2) % list.Length]; }
            }
            male = model.StartsWith("m_");
            if (skinTone == Color.white && !isPlayer)
            {
                int t = (h / 7) % 10;
                skinTone = t < 6 ? new Color(1f, 0.97f, 0.95f) : t < 8 ? new Color(0.86f, 0.74f, 0.64f) : t < 9 ? new Color(0.66f, 0.52f, 0.42f) : new Color(0.45f, 0.34f, 0.27f);
            }
            if (model.Contains("old")) hairColor = (h % 2 == 0) ? new Color(0.72f, 0.7f, 0.68f) : new Color(0.88f, 0.87f, 0.85f);
            else
            {
                Color[] hc = { new Color(0.22f, 0.15f, 0.09f), new Color(0.1f, 0.08f, 0.06f), new Color(0.62f, 0.48f, 0.28f), new Color(0.42f, 0.2f, 0.1f), new Color(0.32f, 0.22f, 0.14f) };
                hairColor = hc[(h / 3) % hc.Length];
                if (skinTone.r < 0.7f) hairColor = new Color(0.07f, 0.06f, 0.05f);
            }
        }

        // ---------------------------------------------------------------- locomotion
        /// <summary>Tell the animator how fast we're moving (m/s). The blend tree thresholds are the clips' real speeds.</summary>
        public void Animate(float spd) { wantSpeed = spd; moved = true; }
        public void SetKneel(bool k) { if (anim != null && anim.enabled) anim.SetBool("Kneel", k); }
        public void SetTalk(bool t) { if (anim != null && anim.enabled) anim.SetBool("Talk", t); }
        public void SetWave(bool w) { if (anim != null && anim.enabled) anim.SetBool("Wave", w); }
        public void Jump() { if (anim != null && anim.enabled) anim.SetTrigger("Jump"); }

        public void Step(Vector3 target, float spd)
        {
            Vector3 d = target - transform.position; d.y = 0;
            if (d.sqrMagnitude < 0.0004f) return;
            Vector3 dir = d.normalized;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 360f * Time.deltaTime);
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
            if (state == State.GettingUp && blendBones != null && blendT < blendDur)
            {
                // ease from the ragdoll's pose into the get-up animation
                blendT += Time.deltaTime; float w = Mathf.SmoothStep(0f, 1f, blendT / blendDur);
                for (int i = 0; i < blendBones.Length; i++) if (blendBones[i] != null) blendBones[i].localRotation = Quaternion.Slerp(blendRot[i], blendBones[i].localRotation, w);
                hips.position = Vector3.Lerp(blendHips, hips.position, w);
            }
            if (state != State.Walking || anim == null) return;
            if (hurt && Time.time > hurtUntil) RestoreNormal();
            if (!moved) { wantSpeed = 0f; wantLocal = Vector2.zero; }
            animSpeed = Mathf.MoveTowards(animSpeed, wantSpeed, 9f * Time.deltaTime);
            if (uma)
            {
                Vector2 target = localSet ? wantLocal : new Vector2(0f, wantSpeed);
                curLocal = Vector2.Lerp(curLocal, target, 1f - Mathf.Exp(-14f * Time.deltaTime));
                anim.SetFloat("MoveX", curLocal.x); anim.SetFloat("MoveZ", curLocal.y);
            }
            else anim.SetFloat("Speed", animSpeed);
            moved = false; localSet = false;
            // footsteps at the gait's cadence
            if (animSpeed > 0.3f && GameAudio.I != null)
            {
                stepT += Time.deltaTime * (1.15f + 0.42f * animSpeed);
                if (stepT >= 1f)
                {
                    stepT -= 1f;
                    var cam = Camera.main;
                    if (cam != null && (cam.transform.position - transform.position).sqrMagnitude < 28f * 28f) GameAudio.I.Footstep(transform.position, isPlayer);
                }
            }
        }

        // ---------------------------------------------------------------- ragdoll
        void OnTriggerEnter(Collider other)
        {
            if (isPlayer || state != State.Walking) return;
            var car = other.GetComponentInParent<IVehicle>();
            if (car == null) return;
            float sp = car.Speed;
            if (sp < 1.5f) return;
            Hit(car.Velocity, other.ClosestPoint(transform.position + Vector3.up), sp);
        }

        public const int RagdollLayer = 29, VehicleLayer = 28;
        Bone AddBone(Transform t, Bone parent, float mass, Collider c)
        {
            var rb = t.gameObject.AddComponent<Rigidbody>();
            rb.mass = mass; rb.linearDamping = 0.1f; rb.angularDamping = 0.8f; rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // stability: more solver passes and gentle depenetration stop limbs being pulled apart (the "stretched" bodies)
            rb.solverIterations = 12; rb.solverVelocityIterations = 4; rb.maxDepenetrationVelocity = 3f;
            t.gameObject.layer = RagdollLayer;
            var b = new Bone { t = t, rb = rb, col = c, parent = parent };
            bones.Add(b); byT[t] = b; return b;
        }

        static CapsuleCollider CapTo(Transform t, Vector3 endWorld, float r, float extra)
        {
            Vector3 e = t.InverseTransformPoint(endWorld);
            var c = t.gameObject.AddComponent<CapsuleCollider>();
            float ax = Mathf.Abs(e.x), ay = Mathf.Abs(e.y), az = Mathf.Abs(e.z);
            c.direction = ax > ay && ax > az ? 0 : ay > az ? 1 : 2;
            float len = e.magnitude;
            c.center = e * 0.5f * (1f + extra / Mathf.Max(len, 0.01f));
            c.height = len + extra; c.radius = r;
            return c;
        }

        void Joint(Bone b, Vector3 twistWorld, Vector3 swingWorld, float lo, float hi, float s1, float s2)
        {
            var j = b.t.gameObject.AddComponent<CharacterJoint>();
            j.connectedBody = b.parent.rb; j.enableProjection = true; j.enablePreprocessing = false;
            j.axis = b.t.InverseTransformDirection(twistWorld); j.swingAxis = b.t.InverseTransformDirection(swingWorld);
            j.lowTwistLimit = new SoftJointLimit { limit = lo }; j.highTwistLimit = new SoftJointLimit { limit = hi };
            j.swing1Limit = new SoftJointLimit { limit = s1 }; j.swing2Limit = new SoftJointLimit { limit = s2 };
            j.projectionDistance = 0.02f; j.projectionAngle = 4f;
        }

        void BuildRagdoll()
        {
            if (!anim.isHuman) return;
            Transform B(HumanBodyBones hb) => anim.GetBoneTransform(hb);
            Vector3 right = transform.right, fwd = transform.forward, up = Vector3.up;
            var spine = B(HumanBodyBones.Spine); var chest = B(HumanBodyBones.Chest) ?? spine; var neck = B(HumanBodyBones.Neck); var head = B(HumanBodyBones.Head);
            var pel = AddBone(hips, null, 11f, AddSphere(hips, Vector3.Lerp(hips.position, spine.position, 0.3f), 0.14f));
            var sp = AddBone(spine, pel, 14f, CapTo(spine, neck.position, 0.15f, 0.06f));
            Joint(sp, right, fwd, -20, 25, 12, 12);
            var hd = AddBone(head, sp, 5f, AddSphere(head, head.position + up * 0.1f + fwd * 0.02f, 0.11f));
            Joint(hd, right, fwd, -40, 30, 30, 30);
            foreach (var side in new[] { 0, 1 })
            {
                var ua = B(side == 0 ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
                var la = B(side == 0 ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
                var hand = B(side == 0 ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
                var ul = B(side == 0 ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
                var ll = B(side == 0 ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
                var foot = B(side == 0 ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
                float sgn = side == 0 ? -1f : 1f;
                var bua = AddBone(ua, sp, 2.2f, CapTo(ua, la.position, 0.05f, 0.04f));
                Joint(bua, (la.position - ua.position).normalized, fwd, -70, 70, 60, 40);
                var bla = AddBone(la, bua, 1.6f, CapTo(la, hand.position + (hand.position - la.position).normalized * 0.12f, 0.045f, 0f));
                Joint(bla, up * sgn, fwd, -100, 2, 5, 5);
                var bul = AddBone(ul, pel, 7f, CapTo(ul, ll.position, 0.075f, 0.04f));
                Joint(bul, right, fwd, -25, 95, 30, 20);
                var bll = AddBone(ll, bul, 4f, CapTo(ll, foot.position + up * -0.06f, 0.06f, 0.04f));
                Joint(bll, right, fwd, -120, 2, 4, 4);
            }
            for (int i = 0; i < bones.Count; i++) for (int k = i + 1; k < bones.Count; k++) if (bones[i].col != null && bones[k].col != null) Physics.IgnoreCollision(bones[i].col, bones[k].col);
            Physics.IgnoreLayerCollision(RagdollLayer, VehicleLayer, true);
            Physics.IgnoreLayerCollision(RagdollLayer, RagdollLayer, true);
        }

        static SphereCollider AddSphere(Transform t, Vector3 centerWorld, float r)
        {
            var c = t.gameObject.AddComponent<SphereCollider>(); c.center = t.InverseTransformPoint(centerWorld); c.radius = r / Mathf.Max(0.001f, t.lossyScale.x); return c;
        }

        /// <summary>Struck by something moving with velocity v. severity in m/s of closing speed.</summary>
        public void Hit(Vector3 v, Vector3 point, float severity)
        {
            if (state != State.Walking || anim == null) return;
            if (isPlayer && GetComponent<PlayerCtl>() is PlayerCtl pcl && pcl.InCar) return;
            LastHit = this;
            label = important ? "Important person" : "Passer-by";
            if (severity >= 7f && !important) dead = true; else injured = true;
            // a light knock: down, a moment on the floor, then up and limping off (no ambulance)
            getUp = !dead && severity < 5.5f; knockKO = true; knockDelay = 0f;
            EnterRagdoll(v, severity);
            Vector3 flat = v.normalized;
            Gore.Burst(point, flat + Vector3.up * 0.3f, getUp ? 25 : 90);
            if (GameAudio.I != null)
            {
                GameAudio.I.CarImpact(point, severity);
                if (dead || !GameAudio.I.Pain(point + Vector3.up * 0.5f, male, 0.1f)) { if (!dead) GameAudio.I.PlayVoice("shout", point, 0.25f, 1f); }
                GameAudio.I.PlayVoice("witness", point + new Vector3(8f, 0, 4f), 1.6f, 1f);
            }
            if (severity >= 8f) StartCoroutine(Dismember(severity));
            StartCoroutine(BleedWhileMoving());
            if (!getUp) Ambulance.Dispatch(this);
        }

        void EnterRagdoll(Vector3 v, float severity)
        {
            state = State.Ragdoll; hitTime = 0; calmTimer = 0;
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>(); if (agent != null) agent.enabled = false;
            if (trigger != null) trigger.enabled = false;
            var cc = GetComponent<CharacterController>(); if (cc != null) cc.enabled = false;
            anim.enabled = false;
            BuildRagdoll();
            if (v.magnitude > 14f) v = v.normalized * 14f;
            foreach (var b in bones)
            {
                float h = b.t.position.y - transform.position.y;
                b.rb.linearVelocity = v * UnityEngine.Random.Range(0.85f, 1.05f) + Vector3.up * Mathf.Clamp(severity * 0.32f, 1f, 5f) * Mathf.Clamp01(h / 1.2f);
                b.rb.angularVelocity = UnityEngine.Random.onUnitSphere * Mathf.Clamp(severity * 0.45f, 1f, 6f);
            }
        }

        float knockDelay; bool knockKO = true;
        /// <summary>
        /// Floored in a fight: goes limp and falls the way the blow pushed. A knockdown (ko false) is back up after
        /// downSecs with a quick get-up and carries on; a knockout stays down longer and may get up limping.
        /// </summary>
        public void Knockout(Vector3 push, float downSecs = 3f, bool ko = true)
        {
            if (state != State.Walking || anim == null) return;
            injured = false; getUp = true; LastHit = this; knockDelay = downSecs; knockKO = ko;
            EnterRagdoll(push * 0.6f, 1.5f); // crumple rather than fly
        }

        // ---------------------------------------------------------------- getting back up
        IEnumerator GetUpAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (state != State.Settled || dead || anim == null || !anim.isHuman) yield break;
            foreach (var b in bones) if (b.severed) yield break;
            hurtCtrl ??= Resources.Load<RuntimeAnimatorController>("Anim/LocoHurt");
            if (hurtCtrl == null) yield break;
            var head = anim.GetBoneTransform(HumanBodyBones.Head);
            var lUp = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg); var rUp = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            // the pose the ragdoll ended in, to blend from
            blendBones = hips.GetComponentsInChildren<Transform>();
            blendRot = new Quaternion[blendBones.Length];
            for (int i = 0; i < blendBones.Length; i++) blendRot[i] = blendBones[i].localRotation;
            blendHips = hips.position;
            Vector3 ragDir = (head.position - hips.position).WithY(0);
            Vector3 bodyFwd = Vector3.Cross(rUp.position - lUp.position, head.position - hips.position);
            bool faceUp = bodyFwd.y > 0f; // the get-up starts face down: from on the back we roll over during the blend
            TearDownRagdoll();
            // stand the root under the hips, on the ground
            Vector3 root = hips.position;
            if (Physics.Raycast(root + Vector3.up * 0.5f, Vector3.down, out var gh, 3f, ~((1 << 2) | (1 << 31)), QueryTriggerInteraction.Ignore)) root.y = gh.point.y; else root.y = transform.position.y;
            transform.position = root;
            // switch to the hurt set and line its first frame up with the body on the floor
            if (normalCtrl == null) normalCtrl = anim.runtimeAnimatorController;
            anim.runtimeAnimatorController = hurtCtrl;
            anim.enabled = true; anim.applyRootMotion = false;
            anim.Play("GetUp", 0, 0f); anim.Update(0f);
            Vector3 animDir = (head.position - hips.position).WithY(0);
            if (animDir.sqrMagnitude > 1e-4f && ragDir.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(animDir, ragDir, Vector3.up), Vector3.up) * transform.rotation;
            anim.Update(0f);
            Vector3 off = (blendHips - hips.position).WithY(0); transform.position += off;
            anim.Update(0f);
            bool quick = !knockKO; // a knockdown in a scrap: straight back up
            anim.speed = quick ? 2.1f : 1f;
            blendT = 0f; blendDur = (faceUp ? 0.9f : 0.35f) / anim.speed;
            state = State.GettingUp;
            yield return new WaitForSeconds(5.4f / anim.speed);
            anim.speed = 1f;
            if (state != State.GettingUp) yield break;
            // up: after a knockout or a car, hurt for a while, then back to normal
            state = State.Walking; hurt = injured || (knockKO && UnityEngine.Random.value < 0.5f); hurtUntil = Time.time + (isPlayer ? 8f : injured ? 60f : 20f);
            if (!hurt) RestoreNormal();
            if (trigger != null) trigger.enabled = true;
            var cc = GetComponent<CharacterController>(); if (cc != null) cc.enabled = true;
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) { agent.enabled = true; if (agent.isOnNavMesh) agent.Warp(transform.position); agent.speed = hurt ? HurtSpeed : agent.speed; }
            var res = GetComponent<Resident>(); if (res != null) res.enabled = true;
            var f = GetComponent<Fighter>(); if (f != null) f.Recover(knockKO);
            knockKO = true;
            GotUp?.Invoke(this);
        }
        public event Action<Person> GotUp;
        Transform[] blendBones; Quaternion[] blendRot; Vector3 blendHips; float blendT, blendDur;

        void RestoreNormal()
        {
            hurt = false;
            if (normalCtrl != null && anim != null) { anim.runtimeAnimatorController = normalCtrl; foreach (var prm in anim.parameters) if (prm.name == "Variant") { anim.SetFloat("Variant", variant * 0.5f); break; } }
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>(); if (agent != null) agent.speed = drunk ? UnityEngine.Random.Range(0.58f, 0.68f) : UnityEngine.Random.Range(1.0f, 1.35f);
        }

        void TearDownRagdoll()
        {
            foreach (var b in bones) { var j = b.t != null ? b.t.GetComponent<CharacterJoint>() : null; if (j != null) DestroyImmediate(j); }
            foreach (var b in bones) { if (b.col != null) DestroyImmediate(b.col); if (b.rb != null) DestroyImmediate(b.rb); }
            bones.Clear(); byT.Clear();
        }

        void FixedUpdate()
        {
            if (state != State.Ragdoll) return;
            hitTime += Time.fixedDeltaTime;
            float v = 0f;
            foreach (var b in bones) if (!b.severed && b.rb != null) v = Mathf.Max(v, b.rb.linearVelocity.magnitude);
            calmTimer = v < 0.25f ? calmTimer + Time.fixedDeltaTime : 0f;
            if ((calmTimer > 1.2f && hitTime > 1.5f) || hitTime > 12f || (!knockKO && knockDelay > 0f && hitTime > knockDelay)) Settle(); // a knockdown in a scrap is up on time, settled or not
        }

        IEnumerator Dismember(float severity)
        {
            yield return new WaitForSeconds(0.12f);
            if (Gore.Level == 0 || !anim.isHuman) yield break;
            HumanBodyBones[] cand = { HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm, HumanBodyBones.LeftUpperLeg };
            var cut = anim.GetBoneTransform(cand[UnityEngine.Random.Range(0, severity > 12f ? cand.Length : 4)]);
            if (cut == null || !byT.TryGetValue(cut, out var b) || b.severed) yield break;
            b.severed = true;
            var j = cut.GetComponent<CharacterJoint>(); if (j != null) Destroy(j);
            var stump = b.parent.t;
            Gore.Spurt(stump, stump.InverseTransformPoint(cut.position), 4.5f);
            if (GameAudio.I != null) GameAudio.I.Gore(cut.position);
            Gore.Burst(cut.position, Vector3.up, 60);
            SeverVisual(cut, stump);
            cut.SetParent(null, true);
            b.rb.linearVelocity += UnityEngine.Random.onUnitSphere * 2f;
            foreach (var k in bones) if (k.parent == b) { var jj = k.t.GetComponent<CharacterJoint>(); if (jj != null) jj.connectedBody = b.rb; }
            Destroy(cut.gameObject, 90f);
        }

        /// <summary>Bake the severed limb (from the most detailed LOD) onto the flying part; collapse it to a stump on every LOD.</summary>
        void SeverVisual(Transform cut, Transform stump)
        {
            SkinnedMeshRenderer src = null;
            foreach (var r in smrs) if (r != null && (src == null || r.sharedMesh.vertexCount > src.sharedMesh.vertexCount)) src = r;
            if (src == null || !src.sharedMesh.isReadable) return;
            var bonesArr = src.bones; var sev = new HashSet<int>();
            for (int i = 0; i < bonesArr.Length; i++) if (bonesArr[i] != null && bonesArr[i].IsChildOf(cut)) sev.Add(i);
            if (sev.Count == 0) return;
            var mesh = src.sharedMesh; var bw = mesh.boneWeights; var uv = mesh.uv;
            var baked = new Mesh(); src.BakeMesh(baked, true);
            var bv = baked.vertices; var bn = baked.normals;
            var remap = new Dictionary<int, int>(); var V = new List<Vector3>(); var N = new List<Vector3>(); var U = new List<Vector2>();
            var subs = new List<List<int>>(); var mats = new List<Material>();
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var tri = mesh.GetTriangles(s); var outT = new List<int>();
                for (int k = 0; k < tri.Length; k += 3)
                {
                    if (!sev.Contains(bw[tri[k]].boneIndex0) || !sev.Contains(bw[tri[k + 1]].boneIndex0) || !sev.Contains(bw[tri[k + 2]].boneIndex0)) continue;
                    for (int q = 0; q < 3; q++)
                    {
                        int vi = tri[k + q];
                        if (!remap.TryGetValue(vi, out int ni))
                        {
                            ni = V.Count; remap[vi] = ni;
                            V.Add(cut.InverseTransformPoint(src.transform.TransformPoint(bv[vi])));
                            N.Add(cut.InverseTransformDirection(src.transform.TransformDirection(bn[vi])));
                            U.Add(uv[vi]);
                        }
                        outT.Add(ni);
                    }
                }
                if (outT.Count > 0) { subs.Add(outT); mats.Add(src.sharedMaterials[Mathf.Min(s, src.sharedMaterials.Length - 1)]); }
            }
            Destroy(baked);
            if (V.Count > 0)
            {
                var lm = new Mesh(); lm.SetVertices(V); lm.SetNormals(N); lm.SetUVs(0, U); lm.subMeshCount = subs.Count;
                for (int s = 0; s < subs.Count; s++) lm.SetTriangles(subs[s], s);
                lm.RecalculateBounds();
                var g = new GameObject("SeveredLimb"); g.transform.SetParent(cut, false);
                g.AddComponent<MeshFilter>().sharedMesh = lm; g.AddComponent<MeshRenderer>().sharedMaterials = mats.ToArray();
            }
            // collapse the limb's skin to a stump: every severed bone on every LOD now points at a zero-scale proxy at the joint
            var proxy = new GameObject("StumpProxy").transform; proxy.SetParent(stump, false); proxy.position = cut.position; proxy.localScale = Vector3.zero;
            foreach (var r in smrs)
            {
                if (r == null) continue;
                var arr = r.bones; bool ch = false;
                for (int i = 0; i < arr.Length; i++) if (arr[i] != null && arr[i].IsChildOf(cut)) { arr[i] = proxy; ch = true; }
                if (ch) r.bones = arr;
            }
        }

        IEnumerator BleedWhileMoving()
        {
            float t = 0;
            while (state == State.Ragdoll && t < 6f)
            {
                t += 0.2f;
                if (Gore.Level > 0 && UnityEngine.Random.value < 0.7f && hips != null)
                    if (Physics.Raycast(hips.position + Vector3.up * 0.2f, Vector3.down, out var h, 3f)) Gore.Splat(h.point, h.normal, UnityEngine.Random.Range(0.1f, 0.3f));
                yield return new WaitForSeconds(0.2f);
            }
        }

        void Settle()
        {
            state = State.Settled;
            foreach (var b in bones) if (b.rb != null) { b.rb.linearVelocity = Vector3.zero; b.rb.angularVelocity = Vector3.zero; }
            if (hips != null && Physics.Raycast(hips.position + Vector3.up * 0.3f, Vector3.down, out var h, 3f))
                if ((injured && !getUp) || dead) Gore.Pool(h.point, dead ? 1.15f : 0.55f, 14f);
            Settled?.Invoke(this);
            if (getUp && !dead) { StartCoroutine(GetUpAfter(knockDelay > 0f ? (knockKO ? knockDelay : Mathf.Max(0.2f, knockDelay - hitTime)) : UnityEngine.Random.Range(2.5f, 4.5f))); knockDelay = 0f; } // a knockdown counts the time spent falling
        }

        public Vector3 BodyPos => hips != null ? hips.position : transform.position;

        /// <summary>Removes the body (picked up by ambulance crew).</summary>
        /// <summary>
        /// Lifted onto a stretcher: out of the ragdoll, laid flat on the back with arms by the sides, head towards the
        /// stretcher's head end (its -z), hips at hipsLocal on the bed. Moves with the stretcher from then on.
        /// </summary>
        public void LieOn(Transform bed, Vector3 hipsLocal)
        {
            StopAllCoroutines();
            foreach (var b in bones) if (b.t != null && b.severed) b.t.gameObject.SetActive(false);
            TearDownRagdoll();
            state = State.Carried;
            var cc = GetComponent<CharacterController>(); if (cc != null) cc.enabled = false;
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>(); if (agent != null) agent.enabled = false;
            if (trigger != null) trigger.enabled = false;
            if (anim != null && anim.isHuman && anim.avatar != null)
            {
                anim.enabled = false;
                var hph = new HumanPoseHandler(anim.avatar, anim.transform);
                var pose = new HumanPose(); hph.GetHumanPose(ref pose);
                for (int i = 0; i < pose.muscles.Length; i++)
                {
                    string mn = HumanTrait.MuscleName[i];
                    pose.muscles[i] = mn.Contains("Arm Down-Up") ? -0.85f : mn.Contains("Forearm Stretch") ? 0.9f : mn.Contains("Arm Front-Back") ? 0.1f : mn.Contains("Finger") || mn.Contains("Thumb") ? 0.25f : 0f;
                }
                pose.bodyPosition = new Vector3(0f, pose.bodyPosition.y, 0f); pose.bodyRotation = Quaternion.identity;
                hph.SetHumanPose(ref pose); hph.Dispose();
            }
            transform.SetParent(bed, true);
            transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // on the back: face up, head to -z
            if (hips != null) transform.position += bed.TransformPoint(hipsLocal) - hips.position;
        }

        public void TakeAway()
        {
            state = State.Carried;
            foreach (var b in bones) if (b.t != null && b.severed) Destroy(b.t.gameObject);
            gameObject.SetActive(false);
        }
    }

    public static class VExt { public static Vector3 WithY(this Vector3 v, float y) { v.y = y; return v; } }
}
