using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// What paramedics carry and use: blue and green response bags, a black defibrillator, an oxygen cylinder with its
    /// mask, the wheeled stretcher with mattress and blanket, and the PARAMEDIC lettering on the back of the dark green
    /// uniform. Built from simple shapes, sized like the real things.
    /// </summary>
    public static class MedicKit
    {
        static Material green, greenDark, blueDark, red, black, grey, chrome, yellow, hivis, silver, white, blue, orange, mattress;
        static void Mats()
        {
            if (green != null) return;
            green = Gfx.Mat(new Color(0.08f, 0.36f, 0.2f), null, 0.25f);
            greenDark = Gfx.Mat(new Color(0.05f, 0.2f, 0.12f), null, 0.2f);
            red = Gfx.Mat(new Color(0.62f, 0.06f, 0.05f), null, 0.3f);
            black = Gfx.Mat(new Color(0.05f, 0.05f, 0.055f), null, 0.35f);
            grey = Gfx.Mat(new Color(0.45f, 0.46f, 0.48f), null, 0.4f, 0.3f);
            chrome = Gfx.Mat(new Color(0.78f, 0.79f, 0.8f), null, 0.75f, 0.9f);
            yellow = Gfx.Mat(new Color(0.95f, 0.78f, 0.05f), null, 0.4f);
            hivis = Gfx.Emissive(new Color(0.82f, 1f, 0.12f), 0.35f);
            silver = Gfx.Mat(new Color(0.85f, 0.86f, 0.88f), null, 0.9f, 0.6f);
            white = Gfx.Mat(new Color(0.94f, 0.94f, 0.93f), null, 0.3f);
            blue = Gfx.Mat(new Color(0.09f, 0.26f, 0.6f), null, 0.15f);
            blueDark = Gfx.Mat(new Color(0.05f, 0.14f, 0.34f), null, 0.15f);
            orange = Gfx.Mat(new Color(0.72f, 0.14f, 0.09f), null, 0.15f);
            mattress = Gfx.Mat(new Color(0.08f, 0.09f, 0.1f), null, 0.45f);
        }

        static GameObject B(Transform p, Vector3 c, Vector3 s, Material m, string n = "Part") => Gfx.Box(p, c, s, m, 0, n, false);
        static GameObject C(Transform p, Vector3 c, Vector3 s, Material m, Vector3 euler, string n = "Part")
        {
            var g = Gfx.Prim(PrimitiveType.Cylinder, p, c, s, m, n, false); g.transform.localRotation = Quaternion.Euler(euler); return g;
        }

        /// <summary>A response bag, blue or green (handle on top, reflective strips, white cross panel). Origin at the handle.</summary>
        public static Transform Bag(bool blueBag = true)
        {
            Mats(); var t = new GameObject("ResponseBag").transform;
            B(t, new Vector3(0, -0.2f, 0), new Vector3(0.5f, 0.3f, 0.26f), blueBag ? blue : green, "Body");
            B(t, new Vector3(0, -0.05f, 0), new Vector3(0.52f, 0.025f, 0.27f), blueBag ? blueDark : greenDark, "Zip");
            B(t, new Vector3(0, -0.22f, 0.132f), new Vector3(0.5f, 0.035f, 0.004f), silver, "Strip");
            B(t, new Vector3(0, -0.22f, -0.132f), new Vector3(0.5f, 0.035f, 0.004f), silver, "Strip2");
            B(t, new Vector3(0, -0.17f, 0.134f), new Vector3(0.1f, 0.03f, 0.003f), white, "CrossH");
            B(t, new Vector3(0, -0.17f, 0.134f), new Vector3(0.03f, 0.1f, 0.003f), white, "CrossV");
            B(t, new Vector3(0, 0.0f, 0), new Vector3(0.18f, 0.025f, 0.03f), black, "Handle");
            return t;
        }

        /// <summary>The defibrillator: a black case with a screen and a yellow-green edge, handle on top. Origin at the handle.</summary>
        public static Transform Defib()
        {
            Mats(); var t = new GameObject("Defib").transform;
            B(t, new Vector3(0, -0.15f, 0), new Vector3(0.34f, 0.24f, 0.14f), black, "Case");
            B(t, new Vector3(0, -0.035f, 0), new Vector3(0.345f, 0.02f, 0.145f), green, "Trim");
            B(t, new Vector3(0, -0.15f, 0.071f), new Vector3(0.24f, 0.15f, 0.004f), grey, "Screen");
            B(t, new Vector3(0, -0.01f, 0), new Vector3(0.16f, 0.025f, 0.03f), black, "Handle");
            return t;
        }

        /// <summary>A portable oxygen cylinder (white shoulder, black body) in its carry bag. Origin at the strap.</summary>
        public static Transform Oxygen()
        {
            Mats(); var t = new GameObject("Oxygen").transform;
            C(t, new Vector3(0, -0.3f, 0), new Vector3(0.11f, 0.24f, 0.11f), black, new Vector3(0, 0, 0), "Cylinder");
            C(t, new Vector3(0, -0.04f, 0), new Vector3(0.1f, 0.03f, 0.1f), white, Vector3.zero, "Shoulder");
            B(t, new Vector3(0, 0.0f, 0), new Vector3(0.04f, 0.05f, 0.04f), chrome, "Valve");
            return t;
        }

        /// <summary>A clear green oxygen mask, to sit over a casualty's nose and mouth.</summary>
        public static Transform Mask()
        {
            Mats(); var t = new GameObject("O2Mask").transform;
            var m = Gfx.Mat(new Color(0.55f, 0.85f, 0.65f), null, 0.85f);
            var g = Gfx.Prim(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.075f, 0.09f, 0.05f), m, "Mask", false);
            return t;
        }

        /// <summary>
        /// The stretcher: yellow frame on legs and castors, black mattress with a raised back section, side rails and
        /// handles at both ends. Lies along z (the head end is -z). Returns the root; bedTop is the mattress surface height.
        /// </summary>
        public static Transform Stretcher(out float bedTop, out Transform[] wheels)
        {
            Mats(); var t = new GameObject("Stretcher").transform;
            const float L = 1.95f, W = 0.58f, deck = 0.62f;
            bedTop = deck + 0.11f;
            B(t, new Vector3(0, deck, 0), new Vector3(W, 0.05f, L), yellow, "Deck");
            foreach (int sx in new[] { -1, 1 }) B(t, new Vector3(sx * (W * 0.5f + 0.02f), deck + 0.16f, 0.05f), new Vector3(0.025f, 0.025f, L * 0.82f), chrome, "Rail");
            foreach (int sz in new[] { -1, 1 }) B(t, new Vector3(0, deck + 0.02f, sz * (L * 0.5f + 0.09f)), new Vector3(W * 0.85f, 0.03f, 0.03f), black, "Handle");
            B(t, new Vector3(0, deck + 0.075f, 0.25f), new Vector3(W * 0.92f, 0.1f, L * 0.66f), mattress, "Mattress");
            var back = B(t, new Vector3(0, deck + 0.12f, -0.62f), new Vector3(W * 0.92f, 0.1f, 0.62f), mattress, "Backrest");
            back.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            // under-frame, legs and castors
            B(t, new Vector3(0, deck - 0.12f, 0), new Vector3(W * 0.7f, 0.04f, L * 0.8f), grey, "UnderFrame");
            wheels = new Transform[4]; int wi = 0;
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
            {
                B(t, new Vector3(sx * W * 0.38f, (deck - 0.12f) * 0.55f, sz * L * 0.38f), new Vector3(0.035f, deck - 0.14f, 0.035f), chrome, "Leg");
                var w = C(t, new Vector3(sx * W * 0.38f, 0.065f, sz * L * 0.38f), new Vector3(0.12f, 0.02f, 0.12f), black, new Vector3(0, 0, 90f), "Castor");
                wheels[wi++] = w.transform;
            }
            return t;
        }

        /// <summary>The blanket over someone on the stretcher (red cellular, or a white sheet over the face for the dead).</summary>
        public static Transform Blanket(bool dead)
        {
            Mats(); var t = new GameObject("Blanket").transform;
            B(t, new Vector3(0, 0, 0), new Vector3(0.6f, 0.06f, dead ? 1.85f : 1.25f), dead ? white : orange, "Blanket");
            if (!dead) B(t, new Vector3(0, 0.031f, -0.55f), new Vector3(0.6f, 0.004f, 0.12f), Gfx.Mat(new Color(0.62f, 0.1f, 0.07f), null, 0.15f), "Edge");
            return t;
        }

        /// <summary>
        /// A paramedic's dark green uniform is the clothing itself (tinted by the character builder); this adds the
        /// reflective PARAMEDIC across the back, laid on the body's actual surface (found from the skinned mesh) so it
        /// neither floats off a slim back nor sinks into a broad one. Follows the chest bone so it bends with them.
        /// </summary>
        public static void DressParamedic(Animator a)
        {
            if (a == null || !a.isHuman) return; Mats();
            var chest = a.GetBoneTransform(HumanBodyBones.Chest) ?? a.GetBoneTransform(HumanBodyBones.Spine);
            var hips = a.GetBoneTransform(HumanBodyBones.Hips); var neck = a.GetBoneTransform(HumanBodyBones.Neck);
            if (chest == null || hips == null || neck == null) return;
            Vector3 fwd = a.transform.forward, up = a.transform.up, right = a.transform.right;
            float torso = Vector3.Distance(hips.position, neck.position);
            Vector3 at = hips.position + up * torso * 0.7f;
            float depth = BackDepth(a, at, fwd, up, right);
            var back = Lettering.Decal(chest, "PARAMEDIC", new Color(0.9f, 0.92f, 0.9f), chest.InverseTransformPoint(at - fwd * (depth + 0.006f)),
                chest.InverseTransformDirection(-fwd), chest.InverseTransformDirection(up), 0.05f);
            back.transform.localScale = new Vector3(back.transform.localScale.x * 0.95f, back.transform.localScale.y, 1f);
        }

        /// <summary>How far behind a point on the spine line the clothed back is, from a snapshot of the skinned mesh.</summary>
        static float BackDepth(Animator a, Vector3 at, Vector3 fwd, Vector3 up, Vector3 right)
        {
            float best = -1f;
            foreach (var smr in a.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.sharedMesh == null || !smr.enabled) continue;
                var m = new Mesh(); try { smr.BakeMesh(m, true); } catch { Object.Destroy(m); continue; }
                var v = m.vertices; var tr = smr.transform;
                for (int i = 0; i < v.Length; i++)
                {
                    Vector3 d = tr.TransformPoint(v[i]) - at;
                    if (Mathf.Abs(Vector3.Dot(d, up)) > 0.05f || Mathf.Abs(Vector3.Dot(d, right)) > 0.07f) continue;
                    float back = -Vector3.Dot(d, fwd); if (back > best) best = back;
                }
                Object.Destroy(m);
            }
            return best > 0.04f && best < 0.3f ? best : 0.13f;
        }
    }

    /// <summary>
    /// Hands reaching for something: a casualty's chest while treating them, the stretcher's handles while pushing.
    /// Sits on the Animator's GameObject next to FootGround (both get the IK pass).
    /// </summary>
    public class MedicHands : MonoBehaviour
    {
        public Transform left, right; public Vector3 leftPos, rightPos; public bool usePos;
        Animator anim; float w;
        void Awake() { anim = GetComponent<Animator>(); }
        public void Reach(Transform l, Transform r) { left = l; right = r; usePos = false; }
        public void ReachTo(Vector3 l, Vector3 r) { leftPos = l; rightPos = r; usePos = true; left = right = null; }
        public void Release() { left = right = null; usePos = false; }
        void OnAnimatorIK(int layer)
        {
            if (anim == null || layer != 0) return;
            bool on = usePos || left != null || right != null;
            w = Mathf.MoveTowards(w, on ? 1f : 0f, Time.deltaTime * 3f);
            if (w <= 0.001f) return;
            Vector3 l = usePos ? leftPos : left != null ? left.position : anim.GetIKPosition(AvatarIKGoal.LeftHand);
            Vector3 r = usePos ? rightPos : right != null ? right.position : anim.GetIKPosition(AvatarIKGoal.RightHand);
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, w); anim.SetIKPosition(AvatarIKGoal.LeftHand, l);
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, w); anim.SetIKPosition(AvatarIKGoal.RightHand, r);
        }
    }
}
