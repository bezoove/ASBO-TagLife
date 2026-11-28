using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// A row of lock-up garages you can get into: each bay has an up-and-over door (some padlocked, which a few heaves will tear
    /// out of the rotten frame) and its own owner's clutter: a mechanic's bench, a hoarder's boxes, somebody's stash under a tarp.
    /// Built in a local frame: the front at z = 0 facing -z, the bays running x 0..n*w, the inside towards +z.
    /// </summary>
    public class Lockup : MonoBehaviour
    {
        public int[] theme; public bool[] locked, startOpen; public Transform[] leaves;
        [System.NonSerialized] float[] open, target; [System.NonSerialized] int[] forced;

        public enum Theme { Mechanic, Hoarder, Stash, Knockoff, Diy, Dosser, Bike, Band, Car, Empty }

        void Init()
        {
            if (open != null || leaves == null) return;
            int n = leaves.Length; open = new float[n]; target = new float[n]; forced = new int[n];
            for (int i = 0; i < n; i++) { open[i] = target[i] = startOpen != null && i < startOpen.Length && startOpen[i] ? 1f : 0f; Pose(i); }
        }
        void Awake() => Init();
        /// <summary>Every door up and unlocked, at once (for the walk-in test).</summary>
        public void OpenAll() { Init(); if (open == null) return; for (int i = 0; i < open.Length; i++) { locked[i] = false; open[i] = target[i] = 1f; Pose(i); } }
        void Pose(int i) { if (leaves[i] != null) leaves[i].localRotation = Quaternion.Euler(-88f * Mathf.SmoothStep(0, 1, open[i]), 0, 0); }
        void Update()
        {
            if (open == null) return;
            for (int i = 0; i < open.Length; i++)
            {
                if (Mathf.Abs(open[i] - target[i]) < 0.005f) continue;
                open[i] = Mathf.MoveTowards(open[i], target[i], Time.deltaTime * 0.9f); Pose(i);
            }
        }

        public string Prompt(int b)
        {
            Init(); if (open == null || b < 0 || b >= open.Length) return "Garage";
            if (target[b] > 0.5f) return "Pull the garage door down";
            return locked[b] ? (forced[b] > 0 ? "Heave on the door again" : "Try the garage door") : "Lift the garage door";
        }

        public void Use(PropWorld pw, PlayerCtl pc, int b, Vector3 w)
        {
            Init(); if (open == null || b < 0 || b >= open.Length) return;
            if (locked[b])
            {
                forced[b]++; NoiseBus.Emit(w, 12f + forced[b] * 4f, pc != null ? pc.transform : null);
                if (GameAudio.I != null) GameAudio.I.Knock(w, 5f, true);
                if (forced[b] >= 3)
                {
                    locked[b] = false; target[b] = 1f;
                    World.Toast("The padlock hasp tears out of the rotten frame. You're in.", 1);
                    Offences.Report("Breaking in", 12f, w);
                }
                else pw.Say(forced[b] == 1 ? LockedLines[(b + theme[b]) % LockedLines.Length] : "You heave on the handle. The whole door booms. The padlock's giving.");
                return;
            }
            target[b] = target[b] > 0.5f ? 0f : 1f;
            if (GameAudio.I != null) GameAudio.I.Knock(w, 3f, true);
            if (target[b] > 0.5f && theme[b] == (int)Theme.Dosser) pw.Say("Somebody's been sleeping in here. The smell hits you first.");
        }
        static readonly string[] LockedLines =
        {
            "Padlocked. A big one. Somebody's proud of what's in there.", "Locked, and the handle's been greased so you can't grip it.",
            "Padlocked, with a second lock lower down. Interesting.", "Locked. Somebody's written 'NOTHING IN HERE' on it in marker."
        };

        /// <summary>What a rummage in bay b's tag turns up, by whose garage it is.</summary>
        public Household.Loot Loot(int b, string tag, System.Random r)
        {
            var t = b >= 0 && b < theme.Length ? (Theme)theme[b] : Theme.Empty;
            Household.Loot[] l;
            switch (t)
            {
                case Theme.Mechanic: l = new[] { new Household.Loot("Socket set in a case", 40f), new Household.Loot("Torque wrench", 30f), new Household.Loot("Alloy wheels, a pair", 90f), new Household.Loot("Jar of loose change and fuses", 9f, true), new Household.Loot("Impact driver", 55f) }; break;
                case Theme.Hoarder: l = new[] { new Household.Loot("Box of old cameras", 45f), new Household.Loot("Vinyl records, a crate", 60f), new Household.Loot("Biscuit tin of medals", 70f), new Household.Loot("Unopened toaster from 1994", 8f), new Household.Loot("A shoebox of letters and twenty quid", 20f, true) }; break;
                case Theme.Stash: l = new[] { new Household.Loot("Holdall of cash", 380f, true), new Household.Loot("Block wrapped in cling film", 300f, false, "drugs"), new Household.Loot("Bag of fizz", 160f, false, "drugs"), new Household.Loot("Bundle of phones, all locked", 90f), new Household.Loot("Scales and baggies", 15f) }; break;
                case Theme.Knockoff: l = new[] { new Household.Loot("Box of knock-off trainers", 60f), new Household.Loot("Sleeves of duty-free cigarettes", 70f), new Household.Loot("Fake designer handbags", 50f), new Household.Loot("Football shirts, wrong spelling", 25f) }; break;
                case Theme.Diy: l = new[] { new Household.Loot("Cordless drill", 45f), new Household.Loot("Angle grinder", 40f), new Household.Loot("Tin of screws and a fiver", 5f, true), new Household.Loot("Circular saw", 50f), new Household.Loot("Strimmer", 30f) }; break;
                case Theme.Dosser: l = new[] { new Household.Loot("A sleeping bag, still warm", 6f), new Household.Loot("Tobacco tin with some coins", 4f, true), new Household.Loot("Somebody's photo album", 1f), new Household.Loot("A radio that works", 10f) }; break;
                case Theme.Bike: l = new[] { new Household.Loot("Motorbike helmet", 50f), new Household.Loot("Exhaust, nearly new", 60f), new Household.Loot("Bike battery", 25f), new Household.Loot("Leather jacket", 45f) }; break;
                case Theme.Band: l = new[] { new Household.Loot("Effects pedals", 70f), new Household.Loot("Cymbals in a bag", 55f), new Household.Loot("Microphone and lead", 30f), new Household.Loot("Cash from last night's gig", 35f, true) }; break;
                case Theme.Car: l = new[] { new Household.Loot("Car stereo, ripped out", 35f), new Household.Loot("Satnav from the glovebox", 25f), new Household.Loot("Spare key on a fob", 5f), new Household.Loot("Car battery", 30f) }; break;
                default: l = new[] { new Household.Loot("A rusty spanner", 1f), new Household.Loot("Paint, half a tin", 2f) }; break;
            }
            return l[r.Next(l.Length)];
        }

        static void Coll(Transform g, Vector3 mn, Vector3 mx)
        {
            var c = new GameObject("Solid").AddComponent<BoxCollider>(); c.transform.SetParent(g, false); c.center = (mn + mx) * 0.5f; c.size = mx - mn;
        }

        /// <summary>
        /// Builds the bays under g (its frame as above): walls, roof, floors, an up-and-over leaf per bay, each bay's clutter and a Home
        /// carrying the door spots and rummage spots. openAt: bays that start with their door up. doorMat picks a door colour per bay.
        /// </summary>
        public static Lockup Build(Transform g, string name, int n, float w, float D, float h, float doorH, float jamb, Material wall, Material roof,
            System.Func<int, Material> doorMat, int seed, ICollection<int> openAt = null)
        {
            var E = CouncilEstate.M; var rng = new System.Random(seed); var k = new MeshKit(); var inside = new MeshKit();
            float L = n * w; const float t = 0.2f, ft = 0.15f;
            // the shell: ends, back, party walls, the front piers and lintel; a felt roof over and a plain ceiling under
            k.Box(wall, new Vector3(-0.15f, 0f, 0f), new Vector3(0.1f, h, D)); k.Box(wall, new Vector3(L - 0.1f, 0f, 0f), new Vector3(L + 0.15f, h, D));
            k.Box(wall, new Vector3(-0.15f, 0f, D - 0.15f), new Vector3(L + 0.15f, h, D));
            Coll(g, new Vector3(-0.15f, 0f, 0f), new Vector3(0.1f, h, D)); Coll(g, new Vector3(L - 0.1f, 0f, 0f), new Vector3(L + 0.15f, h, D)); Coll(g, new Vector3(-0.15f, 0f, D - 0.15f), new Vector3(L + 0.15f, h, D));
            for (int i = 1; i < n; i++) { float x = i * w; k.Box(wall, new Vector3(x - t * 0.5f, 0f, ft), new Vector3(x + t * 0.5f, h, D - 0.15f)); Coll(g, new Vector3(x - t * 0.5f, 0f, ft), new Vector3(x + t * 0.5f, h, D - 0.15f)); }
            for (int i = 0; i <= n; i++)
            {
                float a = i == 0 ? -0.15f : i * w - jamb, b = i == n ? L + 0.15f : i * w + jamb; if (b - a < 0.01f) continue;
                k.Box(wall, new Vector3(a, 0f, 0f), new Vector3(b, doorH, ft)); Coll(g, new Vector3(a, 0f, 0f), new Vector3(b, doorH, ft));
            }
            k.Box(wall, new Vector3(-0.15f, doorH, 0f), new Vector3(L + 0.15f, h, ft)); Coll(g, new Vector3(-0.15f, doorH, 0f), new Vector3(L + 0.15f, h, ft));
            k.Box(roof, new Vector3(-0.25f, h, -0.1f), new Vector3(L + 0.25f, h + 0.18f, D + 0.15f)); Coll(g, new Vector3(-0.25f, h, -0.1f), new Vector3(L + 0.25f, h + 0.18f, D + 0.15f));
            k.Box(E.white, new Vector3(-0.26f, h - 0.08f, -0.12f), new Vector3(L + 0.26f, h + 0.2f, -0.08f));

            var lk = g.gameObject.AddComponent<Lockup>(); lk.theme = new int[n]; lk.locked = new bool[n]; lk.startOpen = new bool[n]; lk.leaves = new Transform[n];
            var home = g.gameObject.AddComponent<Home>();
            var hh = Household.Make(Mathf.Abs(name.GetHashCode()) % 90000 + 3000, rng); hh.kind = Household.Kind.Couple; hh.wealth = 0.4f; hh.stuff = 0.7f; hh.family = name;
            home.H = hh; home.label = name; home.variant = "lockup";
            home.centre = new Vector3(L * 0.5f, 1.2f, D * 0.5f); home.radius = L * 0.5f + 3f; home.midZ = D * 0.5f; home.halfD = D * 0.5f + 0.5f;
            home.zoneRects.Add(Rect.MinMaxRect(0.1f, ft, L - 0.1f, D - 0.15f));

            var themes = new[] { Theme.Mechanic, Theme.Hoarder, Theme.Stash, Theme.Knockoff, Theme.Diy, Theme.Dosser, Theme.Bike, Theme.Band, Theme.Car, Theme.Hoarder, Theme.Diy, Theme.Mechanic };
            // the odd one cleared out

            for (int i = 0; i < n; i++)
            {
                float x0 = i * w + (i == 0 ? 0.1f : t * 0.5f), x1 = (i + 1) * w - (i == n - 1 ? 0.1f : t * 0.5f), cx = (i + 0.5f) * w;
                var th = rng.NextDouble() < 0.07 ? Theme.Empty : themes[rng.Next(themes.Length)]; lk.theme[i] = (int)th;
                bool up = openAt != null && openAt.Contains(i);
                lk.startOpen[i] = up; lk.locked[i] = !up && th != Theme.Dosser && th != Theme.Empty && (th == Theme.Stash || rng.NextDouble() < 0.55);
                // the floor, a little oil stain, a strip light
                inside.Box(E.concrete, new Vector3(x0, 0f, ft), new Vector3(x1, 0.03f, D - 0.15f));
                inside.Box(E.asphalt != null ? E.asphalt : E.black, new Vector3(cx - 0.25f, 0.03f, 2.5f), new Vector3(cx + 0.2f, 0.032f, 2.9f));
                inside.Box(E.concrete, new Vector3(x0, h - 0.03f, ft), new Vector3(x1, h, D - 0.15f)); // a soffit under the felt
                inside.Box(E.white, new Vector3(cx - 0.6f, h - 0.08f, D * 0.5f - 0.05f), new Vector3(cx + 0.6f, h - 0.02f, D * 0.5f + 0.05f));
                // the door: a leaf hinged along its top edge, swinging up and in under the roof
                float hw = w * 0.5f - jamb;
                var hinge = new GameObject("Garage door " + (i + 1)).transform; hinge.SetParent(g, false); hinge.localPosition = new Vector3(cx, doorH, 0.08f);
                var lkit = new MeshKit(); var dm = doorMat(i);
                lkit.Box(dm, new Vector3(-hw, -doorH, -0.035f), new Vector3(hw, 0f, 0.035f));
                for (int r = 1; r < 6; r++) lkit.Box(dm, new Vector3(-hw + 0.04f, -doorH + r * doorH / 6f - 0.012f, -0.05f), new Vector3(hw - 0.04f, -doorH + r * doorH / 6f + 0.012f, -0.035f)); // ribs
                lkit.Box(TownMats.steel != null ? TownMats.steel : E.black, new Vector3(-0.12f, -doorH + 0.75f, -0.07f), new Vector3(0.12f, -doorH + 0.82f, -0.035f)); // the handle
                if (lk.locked[i]) lkit.Box(E.black, new Vector3(0.18f, -doorH + 0.7f, -0.09f), new Vector3(0.26f, -doorH + 0.82f, -0.035f)); // the padlock
                if (rng.NextDouble() < 0.12) lkit.Box(E.felt, new Vector3(-hw + 0.2f, -doorH + 0.3f, -0.052f), new Vector3(hw - 0.4f, -doorH + 1.85f, -0.05f)); // scorched
                var leafGo = lkit.Build("leaf", hinge, false, ShadowCastingMode.Off);
                if (leafGo != null) { var bc = leafGo.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, -doorH * 0.5f, 0f); bc.size = new Vector3(hw * 2f, doorH, 0.1f); }
                lk.leaves[i] = hinge;
                home.spots.Add(new Home.SpotRec { kind = "garage", label = "Garage door", tag = "garage", pos = new Vector3(cx, 0f, -0.45f), room = i, lv = 0 });
                Fill(g, inside, home, th, i, x0, x1, ft + 0.1f, D - 0.2f, h, rng);
            }
            k.Build("Lockup shell", g, false, ShadowCastingMode.On);
            inside.Build("Lockup insides", g, false, ShadowCastingMode.Off);
            return lk;
        }

        /// <summary>One owner's clutter in a bay (x0..x1, z0..z1 inside), with the spots worth going through.</summary>
        static void Fill(Transform g, MeshKit k, Home home, Theme th, int bay, float x0, float x1, float z0, float z1, float h, System.Random rng)
        {
            var E = CouncilEstate.M;
            Material wood = E.fence != null ? E.fence : E.cream, card = E.cream, dark = TownMats.panelDark != null ? TownMats.panelDark : E.black, tyre = TownMats.tyre != null ? TownMats.tyre : E.black;
            Material red = TownMats.redPaint != null ? TownMats.redPaint : E.black, blue = TownMats.nhsBlue != null ? TownMats.nhsBlue : E.black, steel = TownMats.steel != null ? TownMats.steel : E.black;
            float cx = (x0 + x1) * 0.5f;
            void C(Vector3 mn, Vector3 mx) => Coll(g, mn, mx);
            void Spot(string tag, string label, Vector3 p, bool hidden = false) => home.spots.Add(new Home.SpotRec { kind = "rummage", tag = tag, label = label, pos = p, room = bay, lv = 0, hidden = hidden });
            void Bench(float zb) { k.Box(wood, new Vector3(x0 + 0.05f, 0.85f, zb - 0.65f), new Vector3(x1 - 0.05f, 0.9f, zb)); C(new Vector3(x0, 0f, zb - 0.65f), new Vector3(x1, 0.9f, zb)); foreach (float lx in new[] { x0 + 0.1f, x1 - 0.15f }) k.Box(wood, new Vector3(lx, 0f, zb - 0.6f), new Vector3(lx + 0.05f, 0.85f, zb - 0.05f)); }
            void Boxes(float bx0, float bx1, float bz0, float bz1, int rows)
            {
                C(new Vector3(bx0, 0f, bz0), new Vector3(bx1, rows * 0.42f, bz1));
                for (float bx = bx0; bx < bx1 - 0.3f; bx += 0.5f)
                    for (int r = 0; r < rows; r++) { float j = (float)rng.NextDouble() * 0.06f; k.Box(card, new Vector3(bx + j, r * 0.42f, bz0 + j), new Vector3(bx + 0.46f + j, r * 0.42f + 0.4f, bz1 - j)); }
            }
            void Tyres(Vector3 p, int nT) { for (int i = 0; i < nT; i++) k.Cyl(tyre, p + Vector3.up * (i * 0.22f), 0.32f, 0.21f, 12); }
            void Wheel(Vector3 c, float r, float wdt) { k.Push(Matrix4x4.TRS(c + new Vector3(wdt * 0.5f, 0f, 0f), Quaternion.Euler(0, 0, 90f), Vector3.one)); k.Cyl(tyre, Vector3.zero, r, wdt, 12); k.Pop(); }
            switch (th)
            {
                case Theme.Mechanic:
                    Bench(z1); k.Box(red, new Vector3(cx - 0.35f, 0.9f, z1 - 0.45f), new Vector3(cx + 0.2f, 1.18f, z1 - 0.15f));
                    k.Box(dark, new Vector3(x0 + 0.1f, 1.3f, z1 - 0.05f), new Vector3(x1 - 0.1f, 2.0f, z1)); // pegboard
                    Tyres(new Vector3(x0 + 0.45f, 0.03f, z1 - 1.4f), 4); k.Cyl(blue, new Vector3(x1 - 0.4f, 0.03f, z1 - 1.3f), 0.29f, 0.88f, 12);
                    k.Box(steel, new Vector3(cx - 0.3f, 0.03f, 2.3f), new Vector3(cx + 0.35f, 0.6f, 2.9f)); C(new Vector3(cx - 0.3f, 0f, 2.3f), new Vector3(cx + 0.35f, 0.6f, 2.9f)); // an engine on blocks
                    Spot("bench", "Go through the workbench", new Vector3(cx + 0.4f, 0f, z1 - 0.9f)); Spot("toolbox", "Open the toolbox", new Vector3(cx - 0.1f, 0f, z1 - 0.9f));
                    break;
                case Theme.Hoarder:
                    Boxes(x0 + 0.05f, x1 - 0.05f, z1 - 1.0f, z1, 5); Boxes(x0 + 0.05f, x0 + 1.0f, 1.6f, 3.6f, 4);
                    k.Box(dark, new Vector3(x1 - 0.8f, 0.03f, 2.2f), new Vector3(x1 - 0.1f, 0.65f, 2.8f)); // an old telly
                    k.Box(TownMats.bed != null ? TownMats.bed : card, new Vector3(x1 - 0.25f, 0.03f, 3.0f), new Vector3(x1 - 0.05f, 1.9f, 4.3f)); C(new Vector3(x1 - 0.8f, 0f, 2.2f), new Vector3(x1 - 0.05f, 1.9f, 4.3f)); // a mattress on its end
                    Spot("boxes", "Rip open the boxes", new Vector3(cx, 0f, z1 - 1.5f)); Spot("boxes", "Go through the boxes by the wall", new Vector3(x0 + 1.4f, 0f, 2.6f));
                    break;
                case Theme.Stash:
                    k.Box(dark, new Vector3(x0 + 0.1f, 0.03f, z1 - 1.0f), new Vector3(x0 + 1.0f, 0.45f, z1 - 0.1f)); k.Box(dark, new Vector3(x0 + 0.1f, 0.45f, z1 - 0.3f), new Vector3(x0 + 1.0f, 0.9f, z1 - 0.1f)); C(new Vector3(x0 + 0.1f, 0f, z1 - 1.0f), new Vector3(x0 + 1.0f, 0.9f, z1 - 0.1f)); // a knackered armchair
                    k.Box(wood, new Vector3(cx - 0.3f, 0.6f, z1 - 1.6f), new Vector3(cx + 0.3f, 0.64f, z1 - 1.0f)); k.Box(steel, new Vector3(cx - 0.02f, 0f, z1 - 1.32f), new Vector3(cx + 0.02f, 0.6f, z1 - 1.28f));
                    C(new Vector3(x1 - 1.3f, 0f, z1 - 1.2f), new Vector3(x1 - 0.1f, 0.75f, z1 - 0.1f)); k.Box(blue, new Vector3(x1 - 1.3f, 0.03f, z1 - 1.2f), new Vector3(x1 - 0.1f, 0.75f, z1 - 0.1f)); // a tarp over something square
                    k.Box(E.black, new Vector3(x1 - 0.7f, 0.03f, 1.0f), new Vector3(x1 - 0.15f, 0.32f, 1.35f)); // a holdall
                    Spot("tarp", "Lift the tarp", new Vector3(x1 - 0.7f, 0f, z1 - 1.6f), true); Spot("bag", "Unzip the holdall", new Vector3(x1 - 0.45f, 0f, 1.75f));
                    break;
                case Theme.Knockoff:
                    Boxes(x0 + 0.05f, x1 - 0.05f, z1 - 0.9f, z1, 4); Boxes(x0 + 0.05f, cx, 2.0f, 2.9f, 3);
                    k.Box(steel, new Vector3(x1 - 0.5f, 1.6f, 1.4f), new Vector3(x1 - 0.46f, 1.64f, 3.4f));
                    for (float z = 1.5f; z < 3.3f; z += 0.18f) k.Box(rng.NextDouble() < 0.5 ? E.black : red, new Vector3(x1 - 0.7f, 0.75f, z), new Vector3(x1 - 0.26f, 1.6f, z + 0.12f)); // jackets on a rail
                    Spot("boxes", "Rip open the boxes", new Vector3(cx, 0f, z1 - 1.4f)); Spot("rail", "Go through the jackets", new Vector3(x1 - 0.9f, 0f, 2.4f));
                    break;
                case Theme.Diy:
                    k.Box(wood, new Vector3(x0 + 0.05f, 0f, z1 - 0.45f), new Vector3(x1 - 0.05f, 0.04f, z1)); C(new Vector3(x0, 0f, z1 - 0.45f), new Vector3(x1, 2.0f, z1));
                    for (int s = 1; s <= 4; s++) k.Box(wood, new Vector3(x0 + 0.05f, s * 0.48f, z1 - 0.45f), new Vector3(x1 - 0.05f, s * 0.48f + 0.03f, z1)); // shelving
                    for (int i = 0; i < 9; i++) k.Cyl(i % 3 == 0 ? red : i % 3 == 1 ? E.white : blue, new Vector3(x0 + 0.25f + (i % 5) * 0.45f, 0.51f + (i / 5) * 0.48f, z1 - 0.22f), 0.11f, 0.2f, 10); // paint tins
                    k.Box(wood, new Vector3(x0 + 0.1f, 0.03f, 1.2f), new Vector3(x0 + 0.25f, 2.1f, 2.6f)); // timber against the wall
                    k.Box(TownMats.yellow != null ? TownMats.yellow : red, new Vector3(x1 - 0.85f, 0.03f, 2.0f), new Vector3(x1 - 0.2f, 0.4f, 2.6f)); C(new Vector3(x1 - 0.85f, 0f, 2.0f), new Vector3(x1 - 0.2f, 0.4f, 2.6f)); // a mower
                    Spot("shelf", "Go through the shelves", new Vector3(cx, 0f, z1 - 0.9f)); Spot("bench", "Look through the tool bags", new Vector3(x1 - 0.55f, 0f, 3.0f));
                    break;
                case Theme.Dosser:
                    k.Box(TownMats.bed != null ? TownMats.bed : card, new Vector3(x0 + 0.1f, 0.03f, z1 - 2.0f), new Vector3(x0 + 1.0f, 0.2f, z1 - 0.1f));
                    k.Box(TownMats.curtain != null ? TownMats.curtain : blue, new Vector3(x0 + 0.15f, 0.2f, z1 - 1.6f), new Vector3(x0 + 0.95f, 0.28f, z1 - 0.3f)); // a sleeping bag
                    for (int i = 0; i < 7; i++) k.Cyl(i % 2 == 0 ? red : steel, new Vector3(x0 + 1.3f + (float)rng.NextDouble() * 1.0f, 0.03f, z1 - 0.3f - (float)rng.NextDouble() * 2f), 0.033f, 0.12f, 8); // cans
                    k.Box(E.white, new Vector3(x1 - 0.6f, 0.03f, z1 - 0.6f), new Vector3(x1 - 0.2f, 0.4f, z1 - 0.2f)); // carrier bags
                    Spot("underbed", "Look under the mattress", new Vector3(x0 + 0.9f, 0f, z1 - 1.2f)); Spot("bag", "Go through the carrier bags", new Vector3(x1 - 0.6f, 0f, z1 - 1.0f));
                    break;
                case Theme.Bike:
                    C(new Vector3(cx - 0.2f, 0f, 1.55f), new Vector3(cx + 0.2f, 0.9f, 3.65f)); // a motorbike up on its stand: two wheels, a tank and a seat
                    Wheel(new Vector3(cx, 0.33f, 1.9f), 0.33f, 0.14f); Wheel(new Vector3(cx, 0.33f, 3.3f), 0.33f, 0.16f);
                    k.Box(red, new Vector3(cx - 0.16f, 0.5f, 2.1f), new Vector3(cx + 0.16f, 0.82f, 2.8f)); k.Box(E.black, new Vector3(cx - 0.13f, 0.72f, 2.8f), new Vector3(cx + 0.13f, 0.86f, 3.3f));
                    k.Box(steel, new Vector3(cx - 0.12f, 0.25f, 2.2f), new Vector3(cx + 0.12f, 0.5f, 2.9f));
                    Bench(z1); k.Box(red, new Vector3(x0 + 0.2f, 0.9f, z1 - 0.45f), new Vector3(x0 + 0.75f, 1.18f, z1 - 0.15f));
                    Spot("toolbox", "Open the toolbox", new Vector3(x0 + 0.6f, 0f, z1 - 0.9f)); Spot("tarp", "Check the panniers", new Vector3(cx + 0.55f, 0f, 2.6f));
                    break;
                case Theme.Band:
                    k.Box(E.cream, new Vector3(x0 + 0.05f, 0.4f, z1 - 0.05f), new Vector3(x1 - 0.05f, h - 0.2f, z1)); // egg boxes on the back wall
                    k.Box(TownMats.curtain != null ? TownMats.curtain : red, new Vector3(x0 + 0.1f, 0.03f, 1.2f), new Vector3(x1 - 0.1f, 0.05f, z1 - 0.1f)); // carpet offcut
                    k.Box(E.black, new Vector3(x0 + 0.15f, 0.05f, z1 - 0.6f), new Vector3(x0 + 0.85f, 0.85f, z1 - 0.15f)); k.Box(E.black, new Vector3(x1 - 0.75f, 0.05f, z1 - 0.55f), new Vector3(x1 - 0.15f, 0.65f, z1 - 0.15f)); C(new Vector3(x0 + 0.15f, 0f, z1 - 0.6f), new Vector3(x0 + 0.85f, 0.85f, z1 - 0.15f)); C(new Vector3(x1 - 0.75f, 0f, z1 - 0.55f), new Vector3(x1 - 0.15f, 0.65f, z1 - 0.15f)); // amps
                    k.Cyl(E.white, new Vector3(cx, 0.05f, 2.9f), 0.28f, 0.5f, 14); k.Cyl(E.white, new Vector3(cx - 0.45f, 0.45f, 2.5f), 0.17f, 0.18f, 12); k.Cyl(E.white, new Vector3(cx + 0.45f, 0.05f, 2.6f), 0.21f, 0.45f, 12);
                    k.Cyl(steel, new Vector3(cx + 0.55f, 0.05f, 3.2f), 0.01f, 1.0f, 6); k.Cyl(TownMats.yellow != null ? TownMats.yellow : steel, new Vector3(cx + 0.55f, 1.05f, 3.2f), 0.23f, 0.01f, 14);
                    Spot("boxes", "Open the flight cases", new Vector3(x0 + 0.5f, 0f, z1 - 1.0f)); Spot("shelf", "Look round the amps", new Vector3(x1 - 0.45f, 0f, z1 - 1.0f));
                    break;
                case Theme.Car:
                    // a car under a cover: a long low body and the cabin hump
                    k.Box(TownMats.panelDark != null ? TownMats.panelDark : blue, new Vector3(cx - 0.65f, 0.15f, 0.8f), new Vector3(cx + 0.65f, 0.95f, z1 - 0.4f)); C(new Vector3(cx - 0.72f, 0f, 0.8f), new Vector3(cx + 0.72f, 1.4f, z1 - 0.4f));
                    k.Box(TownMats.panelDark != null ? TownMats.panelDark : blue, new Vector3(cx - 0.58f, 0.95f, 1.9f), new Vector3(cx + 0.58f, 1.4f, 3.6f));
                    foreach (float wz in new[] { 1.4f, z1 - 1.1f }) foreach (float wx in new[] { cx - 0.72f, cx + 0.57f }) Wheel(new Vector3(wx, 0.3f, wz), 0.3f, 0.15f);
                    Spot("tarp", "Pull the cover back", new Vector3(cx, 0f, 0.45f));
                    break;
                default:
                    // cleared out but for the leftovers: a box, a dead bike against the wall, a broom
                    k.Box(card, new Vector3(x1 - 0.6f, 0.03f, z1 - 0.5f), new Vector3(x1 - 0.1f, 0.4f, z1 - 0.1f)); k.Box(card, new Vector3(x1 - 0.55f, 0.4f, z1 - 0.45f), new Vector3(x1 - 0.15f, 0.7f, z1 - 0.15f));
                    k.Push(Matrix4x4.TRS(new Vector3(x0 + 0.12f, 0.33f, 2.4f), Quaternion.Euler(0, 0, 90f), Vector3.one)); k.Cyl(tyre, Vector3.zero, 0.33f, 0.04f, 12); k.Pop();
                    k.Push(Matrix4x4.TRS(new Vector3(x0 + 0.12f, 0.33f, 3.4f), Quaternion.Euler(0, 0, 90f), Vector3.one)); k.Cyl(tyre, Vector3.zero, 0.33f, 0.04f, 12); k.Pop();
                    k.Box(steel, new Vector3(x0 + 0.09f, 0.62f, 2.45f), new Vector3(x0 + 0.13f, 0.66f, 3.3f)); k.Box(steel, new Vector3(x0 + 0.09f, 0.33f, 2.85f), new Vector3(x0 + 0.13f, 0.82f, 2.89f)); k.Box(E.black, new Vector3(x0 + 0.06f, 0.82f, 2.75f), new Vector3(x0 + 0.16f, 0.86f, 3.0f));
                    k.Box(wood, new Vector3(x1 - 0.15f, 0.03f, 1.2f), new Vector3(x1 - 0.12f, 1.4f, 1.23f));
                    Spot("boxes", "Look in the box", new Vector3(x1 - 0.4f, 0f, z1 - 0.9f));
                    break;
            }
        }
    }
}
