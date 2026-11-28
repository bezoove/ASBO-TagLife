using System.Collections.Generic;
using UnityEngine;
using ASBO.Casino;

namespace ASBO.Slice
{
    /// <summary>
    /// Everything in an interior is yours to mess with. Walk up to a thing and the prompt says what TALK does: pick up
    /// a glass (drink it, put it down, or punch to lob it), sit on a stool, open a door, pull a pint, use the till, the
    /// coffee machine, the kettle, the fridge, the computer, the safe, a slot machine or a table. A punch or kick that
    /// finds nobody sends the nearest loose thing flying. Cash you pocket is real money.
    /// </summary>
    public class PropWorld : MonoBehaviour
    {
        public static PropWorld I;
        PlayerCtl pc; Transform hand; GameObject held; PropLib.Entry heldE; bool heldDrunk; float useT;
        struct Target { public PropSet set; public int i; public PropDoor door; public PropLib.Entry e; public Vector3 at; public Throwable th; public Home home; public int spot; }
        Target? near; static GUIStyle style; string toastText; float toastT;
        readonly HashSet<string> flags = new HashSet<string>();
        Transform loose; bool littered;
        public static bool Holding => I != null && I.held != null;
        /// <summary>There's something TALK would do right now (for the touch button).</summary>
        public static bool HasPrompt => I != null && (I.near != null || I.held != null || (I.pc != null && I.pc.OnSeat));

        public static void Ensure() { if (I == null) new GameObject("PropWorld").AddComponent<PropWorld>(); }
        void Awake() { I = this; loose = new GameObject("LooseProps").transform; OffencesRun.Ensure(); }

        void Update()
        {
            if (pc == null) { pc = FindFirstObjectByType<PlayerCtl>(); if (pc == null) return; }
            Booze.Tick(); near = null;
            if (pc.InCar || CasinoUI.Open) return;
            if (pc.OnSeat) { near = null; return; }
            Vector3 me = pc.transform.position, fwd = pc.transform.forward; fwd.y = 0f; fwd.Normalize();
            float best = 9f;
            foreach (var s in PropSet.All)
            {
                if (s == null || s.recs == null) continue;
                Vector3 lp = s.transform.InverseTransformPoint(me);
                for (int i = 0; i < s.recs.Length; i++)
                {
                    if (s.gone[i]) continue;
                    var r = s.recs[i]; Vector3 d = r.pos - lp; if (d.x * d.x + d.z * d.z > 3.2f) continue;
                    var e = PropLib.Get(r.id); if (e == null || e.kind == PropLib.Kind.Static) continue;
                    Vector3 c = s.transform.TransformPoint(r.pos + Quaternion.Euler(0, r.yaw, 0) * e.center);
                    float sc = Score(me, fwd, c, e); if (sc < best) { best = sc; near = new Target { set = s, i = i, e = e, at = c }; }
                }
            }
            if (!littered && Time.time > 6f && Person.All.Count > 5) { littered = true; Throwables.Scatter(90); }
            foreach (var th in Throwable.All)
            {
                if (th == null || !th.Free || th.entry == null) continue;
                Vector3 c = th.transform.position + Vector3.up * 0.05f; Vector3 dd = c - me; dd.y = 0f; if (dd.sqrMagnitude > 1.3f) continue;
                float sc = Score(me, fwd, c, th.entry); if (sc < best) { best = sc; near = new Target { th = th, e = th.entry, at = c }; }
            }
            foreach (var h in Home.All)
            {
                if (h == null || !h.gameObject.activeInHierarchy) continue;
                if (h.Nearest(me, fwd, out int hs, out float hsc) && hsc - 0.35f < best) { best = hsc - 0.35f; near = new Target { home = h, spot = hs, at = h.transform.TransformPoint(h.spots[hs].pos) }; }
            }
            foreach (var d in PropDoor.All)
            {
                if (d == null || d.leaf == null) continue;
                if ((d.leaf.position - me).sqrMagnitude > 9f) continue;
                Vector3 c = d.leaf.position + Vector3.up * 1.0f; var e = PropLib.Get(d.id); if (e == null) continue;
                float sc = Score(me, fwd, c, e) - 0.5f; if (sc < best) { best = sc; near = new Target { door = d, e = e, at = c }; }
            }
        }
        static float Score(Vector3 me, Vector3 fwd, Vector3 c, PropLib.Entry e)
        {
            Vector3 d = c - me; float dy = d.y; d.y = 0f; float dist = d.magnitude;
            float reach = e.kind == PropLib.Kind.Use || e.kind == PropLib.Kind.Door ? 1.25f + Mathf.Max(e.size.x, e.size.z) * 0.45f : e.kind == PropLib.Kind.Sit ? 1.0f : 1.05f;
            if (dist > reach || dy < -0.4f || dy > 2.1f) return 99f;
            float ang = dist < 0.05f ? 0f : Vector3.Angle(fwd, d); if (ang > 75f) return 99f;
            return dist + ang / 60f + (e.kind == PropLib.Kind.Pickup ? 0f : 0.15f);
        }

        // ------------------------------------------------------------------------------------------ TALK
        /// <summary>TALK pressed: do whatever the prompt says. True if something happened.</summary>
        public static bool TryUse() => I != null && I.Use();
        bool Use()
        {
            if (pc == null || Time.time - useT < 0.25f) return false;
            if (pc.OnSeat) { pc.StandUp(); useT = Time.time; return true; }
            if (near == null)
            {
                if (held == null) return false;
                useT = Time.time;
                if (heldE != null && !heldDrunk && (!string.IsNullOrEmpty(heldE.drink) || !string.IsNullOrEmpty(heldE.food))) { Consume(); return true; }
                PutDown(); return true;
            }
            var t = near.Value; useT = Time.time;
            if (t.home != null) { t.home.Use(this, pc, t.spot); return true; }
            if (t.door != null) { Door(t.door); return true; }
            var e = t.e;
            switch (e.kind)
            {
                case PropLib.Kind.Pickup:
                    if (e.cash > 0f) { StartCoroutine(Pocket(t)); return true; }
                    if (held != null) PutDown();
                    PickUp(t); return true;
                case PropLib.Kind.Sit:
                    var s = t.set; Vector3 p = s.World(t.i); Quaternion q = s.Rot(t.i);
                    pc.SitOn(p + q * new Vector3(0f, Mathf.Max(0f, e.seat - 0.46f), e.id.Contains("stool") ? -0.16f : -0.10f), q.eulerAngles.y); return true;
                case PropLib.Kind.Use: Action(t); return true;
            }
            return false;
        }

        void Door(PropDoor d)
        {
            if (Home.TryRec(d, out var hh, out var hr) && hr.locked && d.target < 0.5f) { Say("Locked. You could always put your boot through it."); if (GameAudio.I != null) GameAudio.I.Knock(d.leaf.position, 2f, false); return; }
            if (d.target < 0.5f)
            {
                if (d.lockTag == "vault" && !flags.Contains("code")) { Say("A keypad and a time lock. It wants a four-digit code."); return; }
                if (d.lockTag == "vault") Say("1... 9... 8... 7. A clunk, a whirr, and the vault door swings open.");
                if (d.lockTag == "cage" && !flags.Contains("cagekey")) { Say("Locked. The cashier's got the key on a lanyard."); return; }
            }
            d.Toggle(pc.transform.position);
        }

        void Action(Target t)
        {
            var e = t.e; string a = e.action ?? "";
            switch (a)
            {
                case "slots": CasinoUI.Show(CasinoUI.Game.Slots, e.game < 0 ? t.i : e.game); Face(t); return;
                case "roulette": CasinoUI.Show(CasinoUI.Game.Roulette, 0); Face(t); return;
                case "blackjack": CasinoUI.Show(CasinoUI.Game.Blackjack, 0); Face(t); return;
                case "poker": CasinoUI.Show(CasinoUI.Game.Poker, 0); Face(t); return;
                case "cage": CasinoUI.Show(CasinoUI.Game.Cage, 0); Face(t); return;
                case "pour": Give(Pick("pint_lager", "pint_bitter", "pint_stout", "pint_cider")); Say("You pull yourself a pint. The barman's looking the other way."); return;
                case "optic": Give(Pick("shot_full", "tumbler_whisky")); Say("A quick one off the optic."); return;
                case "coffee": Give("cup_coffee"); Say("The machine hisses and gurgles. One coffee."); return;
                case "kettle": Give("mug_tea"); Say("You stick the kettle on and make a brew. Milk, two sugars."); return;
                case "fridge": Give(Pick("btl_sixpence", "btl_northfield", "btl_fizzo", "btl_millane", "btl_abbots")); Say("You help yourself from the fridge."); return;
                case "vend": if (Econ.Spend(1.2f, "snack")) { Give(Pick("crisps_ready", "crisps_salt", "crisps_cheese", "scratchings")); Say("Clunk. A packet drops."); } return;
                case "till":
                    if (!flags.Contains("till" + t.i)) { flags.Add("till" + t.i); float v = Random.Range(40f, 120f); Econ.Give(v); Say($"You get the drawer open and grab £{v:0} in notes."); Heat(v); }
                    else Say("Just coppers left in it."); return;
                case "pc": flags.Add("code"); Say("The takings spreadsheet. There's a sticky note on the screen: SAFE 1987. Don't tell Gaz."); return;
                case "cctv": Say("Nine little grey pictures: the doors, the cage, the bar, the car park. You can see yourself."); return;
                case "safe":
                    if (!flags.Contains("code")) { Say("A dial and a handle. You'd need the combination."); return; }
                    if (flags.Contains("safe")) { Say("Empty. You saw to that."); return; }
                    flags.Add("safe"); flags.Add("cagekey"); Econ.Give(3000f); Say("1-9-8-7. It opens: £3,000 and a ring of keys. You take the lot."); Heat(3000f); return;
                case "search": flags.Add("cagekey"); Say("Payslips, a staff list and, taped to the bottom drawer, a key marked CAGE."); return;
                case "locker": { float v = Random.value; if (v < 0.3f) { Econ.Give(5f); Say("Somebody's jacket. A fiver in the pocket."); } else if (v < 0.6f) Give(Pick("btl_fizzo", "crisps_cheese", "mug_dirty")); else Say("Just a smelly gym bag."); } return;
                case "count": Say("The note counter whirrs through a stack of twenties: £2,000 exactly."); return;
                case "microwave": Say("Ping. Somebody's lasagne, now nuclear."); return;
                case "toast": Say("Two slices go in. It's always the same bread."); return;
                case "tv": Say("The racing. Lucky Lad by a length."); return;
                case "water": Say("A paper cone of very cold water."); return;
                case "read": Say("STAFF ROTA... MEMO: Till 2 short again. Whoever it is, we WILL find out."); return;
                case "phone": Say("Dial tone. You put it down again."); return;
                case "bell": Say("DING DING! \"Last orders!\" Nobody moves."); if (GameAudio.I != null) GameAudio.I.Knock(t.at, 3f, true); return;
                case "sink": Say("You rinse a mug. Gold star."); return;
                case "open": Say("A cloud of steam and the smell of lemon cleaner."); return;
                case "keypad": Say(flags.Contains("code") ? "You punch in 1987. A green light." : "Beep-beep-baap. Red light."); return;
                case "spray": Say("You let off a blast of the extinguisher. Hilarious."); return;
                case "heal": Say("Plasters and a crushed cold pack. You patch yourself up."); return;
                case "door": return;
                default: Say(e.label); return;
            }
        }
        void Face(Target t) { if (t.set != null) { Vector3 d = t.at - pc.transform.position; d.y = 0f; if (d.sqrMagnitude > 0.01f) pc.transform.rotation = Quaternion.LookRotation(d); } Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        static string Pick(params string[] s) => s[Random.Range(0, s.Length)];
        void Heat(float v) { if (v >= 50f) { World.Toast(v >= 1000f ? "That'll be on the cameras." : "Keep your head down.", 1); Offences.Report("Theft", Mathf.Clamp(v * 0.1f, 10f, 60f), pc.transform.position); } }

        // ------------------------------------------------------------------------------------------ holding things
        Transform Hand()
        {
            if (hand != null) return hand;
            var an = pc.GetComponentInChildren<Animator>(); if (an != null && an.isHuman) hand = an.GetBoneTransform(HumanBodyBones.RightHand);
            return hand != null ? hand : pc.transform;
        }
        GameObject Spawn(PropLib.Entry e, Vector3 pos, Quaternion rot)
        {
            var g = new GameObject(e.id); g.transform.SetParent(loose, true); g.transform.SetPositionAndRotation(pos, rot);
            g.AddComponent<MeshFilter>().sharedMesh = e.mesh;
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { PropLib.I.opaque, PropLib.I.glass }; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var bc = g.AddComponent<BoxCollider>(); bc.center = e.center; bc.size = Vector3.Max(e.size, Vector3.one * 0.03f);
            var rb = g.AddComponent<Rigidbody>(); rb.mass = Mathf.Clamp(e.mass, 0.05f, 80f); rb.interpolation = RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var kn = g.AddComponent<Knockable>(); string id = e.id; kn.glass = id.StartsWith("pint") || id.StartsWith("half") || id.StartsWith("shot") || id.StartsWith("wine") || id.StartsWith("btl") || id.StartsWith("tumbler"); kn.loudBase = kn.glass ? 18f : 10f;
            return g;
        }
        GameObject Take(Target t)
        {
            if (t.th != null) return t.th.gameObject;
            var s = t.set; s.Hide(t.i);
            return t.e.cash > 0f ? null : Spawn(t.e, s.World(t.i), s.Rot(t.i));
        }
        bool reaching;
        /// <summary>Bend down or reach for it first; it's in your hand when your hand gets there.</summary>
        void PickUp(Target t) { if (!reaching) StartCoroutine(PickUpRun(t)); }
        System.Collections.IEnumerator PickUpRun(Target t)
        {
            reaching = true; float d = pc.ReachFor(t.at);
            if (d > 0f) yield return new WaitForSeconds(d);
            var g = Take(t); Hold(g, t.e); reaching = false;
        }
        System.Collections.IEnumerator Pocket(Target t)
        {
            if (reaching) yield break; reaching = true; float d = pc.ReachFor(t.at);
            if (d > 0f) yield return new WaitForSeconds(d);
            Take(t); Econ.Give(t.e.cash); Say($"You pocket £{t.e.cash:0}. Nobody saw. Probably."); Heat(t.e.cash); reaching = false;
        }
        void Give(string id) { var e = PropLib.Get(id); if (e == null) return; if (held != null) PutDown(); var h = Hand(); Hold(Spawn(e, h.position, pc.transform.rotation), e); }
        void Hold(GameObject g, PropLib.Entry e)
        {
            if (g == null) return;
            held = g; heldE = e; heldDrunk = false;
            var rb = g.GetComponent<Rigidbody>(); rb.isKinematic = true; g.GetComponent<Collider>().enabled = false;
            var h = Hand(); g.transform.SetParent(h, false);
            // in the palm: the item's middle a little in front of the hand, upright
            g.transform.localPosition = new Vector3(0.0f, 0.06f, 0.05f) - Vector3.up * e.center.y * 0.5f;
            g.transform.rotation = Quaternion.LookRotation(pc.transform.forward, Vector3.up);
        }
        void Release(Vector3 vel)
        {
            if (held == null) return;
            held.transform.SetParent(loose, true);
            var rb = held.GetComponent<Rigidbody>(); held.GetComponent<Collider>().enabled = true; rb.isKinematic = false; rb.linearVelocity = vel;
            held = null; heldE = null;
        }
        void PutDown()
        {
            if (held == null) return;
            Vector3 front = pc.transform.position + pc.transform.forward * 0.55f + Vector3.up * 1.4f;
            float y = pc.transform.position.y;
            if (Physics.Raycast(front, Vector3.down, out var hit, 1.6f, ~(1 << 2), QueryTriggerInteraction.Ignore)) y = hit.point.y;
            var g = held; Release(Vector3.zero);
            g.transform.SetPositionAndRotation(new Vector3(front.x, y + 0.005f, front.z), Quaternion.Euler(0f, pc.transform.eulerAngles.y, 0f));
        }
        /// <summary>Where a throw would go from the hand right now: the camera's aim, a bit above, slower for heavy things.</summary>
        Vector3 ThrowVel(float mass)
        {
            var cam = Camera.main; Vector3 aim = cam != null ? cam.transform.forward : pc.transform.forward;
            bool lob = mass < 0.6f; // cans, stones, bottles: a gentle underarm lob, higher and slower
            aim.y = Mathf.Clamp(aim.y, -0.2f, 0.6f) + (lob ? 0.34f : 0.18f); aim.Normalize();
            return aim * (lob ? 10.5f : Mathf.Lerp(15f, 5f, Mathf.Clamp01(mass / 12f)));
        }
        bool throwing;
        void Throw()
        {
            if (held == null || throwing) return;
            StartCoroutine(ThrowRun());
        }
        System.Collections.IEnumerator ThrowRun()
        {
            throwing = true;
            Vector3 flat = ThrowVel(1f); float wait = pc.ThrowWindup(flat);
            if (wait > 0f) yield return new WaitForSeconds(wait);
            if (held != null)
            {
                float mass = held.GetComponent<Rigidbody>().mass;
                var g = held; Release(ThrowVel(mass));
                g.GetComponent<Rigidbody>().angularVelocity = Random.insideUnitSphere * 9f;
                var kn = g.GetComponent<Knockable>(); if (kn != null) { kn.thrower = pc.transform; kn.thrownAt = Time.time; }
                if (heldE != null && heldE.id.StartsWith("pint")) Say("You lob the pint. Glass everywhere.");
            }
            throwing = false;
        }

        // the aim arc: a faint dotted curve from your hand to where it would land, shown while you're holding something
        LineRenderer arc;
        void LateUpdate()
        {
            bool show = held != null && !throwing && pc != null && !pc.InCar && !pc.OnSeat && !CasinoUI.Open && !heldDrunk;
            if (!show) { if (arc != null && arc.enabled) arc.enabled = false; return; }
            if (arc == null)
            {
                var g = new GameObject("ThrowArc"); arc = g.AddComponent<LineRenderer>(); arc.positionCount = 28; arc.widthMultiplier = 0.035f; arc.numCapVertices = 2;
                arc.material = Gfx.Mat(new Color(1f, 0.85f, 0.4f), null, 0f); arc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; arc.receiveShadows = false;
                arc.textureMode = LineTextureMode.Tile;
            }
            arc.enabled = true;
            Vector3 p = Hand().position + Vector3.up * 0.1f, v = ThrowVel(held.GetComponent<Rigidbody>().mass); int n = 0;
            for (; n < 28; n++)
            {
                arc.SetPosition(n, p); Vector3 np = p + v * 0.06f; v += Physics.gravity * 0.06f;
                if (Physics.Linecast(p, np, out var hit, ~((1 << 2) | (1 << 28) | (1 << 29)), QueryTriggerInteraction.Ignore)) { arc.SetPosition(n, hit.point); n++; break; }
                p = np;
            }
            arc.positionCount = Mathf.Max(2, n);
        }
        void Consume()
        {
            heldDrunk = true;
            string id = heldE.id; bool food = !string.IsNullOrEmpty(heldE.food);
            Say(food ? "You scoff the lot." : id.StartsWith("pint") || id.StartsWith("half") ? "You neck it. Lovely." : id.StartsWith("shot") ? "Straight down. Burns." : id.StartsWith("cup") || id.StartsWith("mug") ? "Nice cuppa." : "Down in one.");
            if (!food && id != "cup_coffee" && id != "mug_tea" && id != "btl_fizzo") Booze.Drink();
            // swap a full glass for the empty one
            string empty = id.StartsWith("pint") ? "pint_empty" : id.StartsWith("shot") ? "shot_empty" : id.StartsWith("wine") ? "wine_empty" : null;
            if (empty != null) { var g = held; held = null; Destroy(g); var e = PropLib.Get(empty); if (e != null) Hold(Spawn(e, Hand().position, pc.transform.rotation), e); heldDrunk = true; }
        }

        /// <summary>A punch while holding something: throw it instead. True if there was something to throw.</summary>
        /// <summary>Debug: put a throwable straight into the player's hand.</summary>
        public static bool DebugHold(string id) { if (I == null || I.pc == null) return false; var g = Throwables.Spawn(id, I.Hand().position, I.pc.transform.rotation); if (g == null) return false; I.Hold(g, g.GetComponent<Throwable>().entry); return true; }
        public static bool ThrowHeld() { if (I == null || I.held == null) return false; I.Throw(); return true; }

        /// <summary>A punch or kick that found nobody: the nearest loose prop in front goes flying. True if one did.</summary>
        public static bool Strike(Vector3 from, Vector3 fwd, float reach, float impulse)
        {
            if (I == null) return false;
            if (Home.Kick(from, fwd, reach)) return true;
            fwd.y = 0f; fwd.Normalize(); float best = 99f; PropSet bs = null; int bi = -1;
            foreach (var s in PropSet.All)
            {
                if (s == null || s.recs == null) continue;
                Vector3 lp = s.transform.InverseTransformPoint(from);
                for (int i = 0; i < s.recs.Length; i++)
                {
                    if (s.gone[i]) continue; var r = s.recs[i]; Vector3 d = r.pos - lp; if (d.x * d.x + d.z * d.z > (reach + 0.6f) * (reach + 0.6f) || Mathf.Abs(d.y + 0.4f) > 1.4f) continue;
                    var e = PropLib.Get(r.id); if (e == null || e.kind != PropLib.Kind.Pickup || e.mass > 30f) continue;
                    Vector3 w = s.transform.TransformPoint(r.pos + Vector3.up * e.center.y) - from; w.y = 0f; if (Vector3.Angle(fwd, w) > 55f) continue;
                    if (w.magnitude < best) { best = w.magnitude; bs = s; bi = i; }
                }
            }
            if (bs == null) return false;
            var t = new Target { set = bs, i = bi, e = PropLib.Get(bs.recs[bi].id) }; var g = I.Take(t); if (g == null) return true;
            var rb = g.GetComponent<Rigidbody>(); rb.AddForce((fwd + Vector3.up * 0.4f).normalized * impulse * Mathf.Clamp(1f / Mathf.Max(0.3f, rb.mass), 0.2f, 1.5f), ForceMode.Impulse);
            rb.angularVelocity = Random.insideUnitSphere * 6f;
            return true;
        }

        public void Say(string s) { toastText = s; toastT = Time.time + 3.5f; }

        void OnGUI()
        {
            if (pc == null || World.HideHud || CasinoUI.Open) return;
            style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(18, Screen.height / 30), fontStyle = FontStyle.Bold, wordWrap = true };
            string k = TouchControls.Wanted ? "TALK: " : "T: ";
            string t = null;
            if (pc.OnSeat) t = k + "Stand up";
            else if (near != null)
            {
                var n = near.Value;
                if (n.home != null) t = k + n.home.Prompt(n.spot);
                else if (n.door != null) t = k + (n.door.target > 0.5f ? "Shut the door" : n.e.label);
                else t = k + (n.e.kind == PropLib.Kind.Pickup ? (n.e.cash > 0f ? "Take the " + n.e.label.ToLower() : "Pick up " + Article(n.e.label)) : n.e.label);
            }
            else if (held != null) t = k + (heldE != null && !heldDrunk && !string.IsNullOrEmpty(heldE.drink) ? "Drink" : heldE != null && !heldDrunk && !string.IsNullOrEmpty(heldE.food) ? "Eat" : "Put it down") + "    PUNCH: Throw";
            if (t != null)
            {
                var r = new Rect(0, Screen.height * 0.72f, Screen.width, style.fontSize * 1.6f);
                style.normal.textColor = new Color(0, 0, 0, 0.75f); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), t, style);
                style.normal.textColor = new Color(1f, 0.85f, 0.4f); GUI.Label(r, t, style);
            }
            if (Time.time < toastT && toastText != null)
            {
                var r = new Rect(Screen.width * 0.15f, Screen.height * 0.62f, Screen.width * 0.7f, style.fontSize * 3f);
                style.normal.textColor = new Color(0, 0, 0, 0.8f); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), toastText, style);
                style.normal.textColor = Color.white; GUI.Label(r, toastText, style);
            }
        }
        static string Article(string s) { if (string.IsNullOrEmpty(s)) return "it"; char c = char.ToLower(s[0]); bool plural = s.EndsWith("s") && !s.EndsWith("ss"); return (plural ? "the " : "aeiou".IndexOf(c) >= 0 ? "an " : "a ") + char.ToLower(s[0]) + s.Substring(1); }
    }

    /// <summary>How many you've had: past a few the camera sways and your legs go.</summary>
    public static class Booze
    {
        public static float Level; static float last;
        public static void Drink() { Level = Mathf.Min(10f, Level + 1f); last = Time.time; if (Level >= 6f) World.Toast("You're steaming.", 1); else if (Level >= 3f) World.Toast("You're feeling it.", 0); }
        public static float Sway => Mathf.Clamp01((Level - 2f) / 6f);
        public static void Tick() { if (Time.time - last > 60f && Level > 0f) { Level = Mathf.Max(0f, Level - Time.deltaTime / 30f); } }
    }
}
