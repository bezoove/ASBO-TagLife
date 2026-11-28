using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// A lived-in house at play: what's in it to search, who's home, which doors are locked. The furniture is baked
    /// (see HomeBuilder); this keeps the records of the things you can do in it. Nothing here is per-frame work
    /// unless you're within a few metres.
    /// </summary>
    public class Home : MonoBehaviour
    {
        public static readonly List<Home> All = new List<Home>();
        [System.Serializable] public class DoorRec { public PropDoor door; public string tag; public int lv, tier; [System.NonSerialized] public bool locked, broken; [System.NonSerialized] public int hits; }
        [System.Serializable] public class SpotRec { public string kind, label, tag; public Vector3 pos; public float yaw; public int room, lv; public bool hidden; public float h; }
        [System.Serializable] public class RoomRec { public string type, use; public int lv; public Vector3 centre; public float u0, u1, d0, d1; }

        public Household H; public string label;
        public List<DoorRec> doors = new List<DoorRec>(); public List<SpotRec> spots = new List<SpotRec>(); public List<RoomRec> rooms = new List<RoomRec>();
        public Vector3 centre; public float radius = 8f; public Vector3 WCentre => transform.TransformPoint(centre); public List<Rect> zoneRects = new List<Rect>();
        [System.NonSerialized] public bool[] done; [System.NonSerialized] public bool entered; [System.NonSerialized] public float noticedAt = -99f;
        [System.NonSerialized] public bool tvOn; [System.NonSerialized] GameObject furnGo;
        public string variant; public bool awake, occupantIn; // decided at play from the clock
        /// <summary>Shops and flats over them: the shop's own opening hours, and which part of the footprint the pooled room light looks at.</summary>
        public bool shop, openNow; public string shopKind; public float midZ = -4.3f, halfD = 4.6f;

        public void Setup(GameObject[] interior, GameObject furniture) { interiorGos = interior; furnGo = furniture; }
        [System.NonSerialized] GameObject[] interiorGos;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>Called once the world exists (the baked scene's zones and records are rebuilt each play).</summary>
        public static void AttachAll()
        {
            foreach (var h in FindObjectsByType<Home>(FindObjectsSortMode.None)) h.Attach();
        }
        public void Attach()
        {
            done = new bool[spots.Count];
            var list = new List<CRect>(); foreach (var r in zoneRects) list.Add(new CRect(r.xMin, r.xMax, r.yMin, r.yMax));
            if (list.Count > 0) GameAudio.RegisterZone(transform, list, 8.0f);
            var rn = new System.Random(H != null ? H.id * 31 + 7 : 1);
            foreach (var d in doors)
            {
                if (d.door == null) continue;
                d.locked = d.tier >= 0 && (d.tag == "front" || d.tag == "back") && rn.NextDouble() < (d.tier == 0 ? 0.35 : 0.9);
                d.door.lockTag = d.locked ? "home" : "";
            }
            // who's in: pensioners and the unemployed are usually home; most others are out in the day
            float hr = GameClock.Hour;
            float home = H == null || !H.Occupied ? 0f : H.kind == Household.Kind.Pensioner || H.kind == Household.Kind.Hoarder ? 0.9f : H.kind == Household.Kind.Student ? 0.6f : (hr > 17f || hr < 8f) ? 0.75f : 0.3f;
            occupantIn = rn.NextDouble() < home; awake = occupantIn && !(hr > 23.5f || hr < 7f) || (occupantIn && rn.NextDouble() < 0.12);
            if (shop)
            {
                // open 8 till 9 if somebody trades from it; otherwise shuttered and alarmed. The flat over it is its own matter.
                openNow = H != null && H.Occupied && hr >= 8f && hr < 21f; occupantIn = openNow; awake = openNow;
                foreach (var d in doors)
                {
                    if (d.door == null || d.tag != "shopfront") continue;
                    d.locked = !openNow; d.tier = openNow ? -1 : 1; d.door.lockTag = d.locked ? "home" : "";
                }
            }
        }

        // ------------------------------------------------------------------ doors
        public DoorRec RecFor(PropDoor d) { foreach (var r in doors) if (r.door == d) return r; return null; }
        public static bool TryRec(PropDoor d, out Home home, out DoorRec rec)
        {
            home = null; rec = null; if (d == null) return false;
            foreach (var h in All) { if (h == null) continue; var r = h.RecFor(d); if (r != null) { home = h; rec = r; return true; } }
            return false;
        }
        /// <summary>A kick or punch on a locked front or back door within reach. True if it landed on a door.</summary>
        public static bool Kick(Vector3 from, Vector3 fwd, float reach)
        {
            fwd.y = 0f; fwd.Normalize();
            foreach (var h in All)
            {
                if (h == null || (h.WCentre - from).sqrMagnitude > 20f * 20f) continue;
                foreach (var r in h.doors)
                {
                    if (r.door == null || !r.locked || r.broken || r.door.leaf == null) continue;
                    Vector3 c = r.door.leaf.position; Vector3 d = c - from; d.y = 0f; if (d.magnitude > reach + 0.5f || Vector3.Angle(fwd, d) > 60f) continue;
                    int need = r.tier == 0 ? 2 : r.tier == 1 ? 5 : 9;
                    r.hits++;
                    NoiseBus.Emit(c, 14f + r.hits * 3f, Player());
                    if (GameAudio.I != null) GameAudio.I.Knock(c, 4f, true);
                    if (r.hits >= need)
                    {
                        r.broken = true; r.locked = false; r.door.lockTag = ""; r.door.Toggle(from);
                        World.Toast("The frame splinters and the door gives. Loudly.", 1);
                        Offences.Report("Breaking in", h.shop ? 34f : 24f, c); h.Disturb(c, 1.0f);
                        if (h.shop) World.Toast("An alarm goes off, somewhere above the door.", 1);
                    }
                    else World.Toast(r.hits == 1 ? "You put your shoulder to it." : need - r.hits <= 1 ? "It's giving. Nearly." : "Thud. It holds.", 0);
                    return true;
                }
            }
            return false;
        }
        static Transform Player() { var p = FindFirstObjectByType<PlayerCtl>(); return p != null ? p.transform : null; }

        // ------------------------------------------------------------------ spots
        public string Prompt(int i)
        {
            var s = spots[i]; if (s.kind == "rummage") return done != null && done[i] ? "Nothing more in the " + Short(s.tag) : s.label;
            if (s.kind == "garage") { var lk = GetComponent<Lockup>(); if (lk != null) return lk.Prompt(s.room); }
            if (s.kind == "tv") return tvOn ? "Turn the telly off" : "Turn the telly on";
            return s.label;
        }
        static string Short(string t) => t;
        static readonly string[] Notices =
        {
            "RESIDENTS MEETING THURSDAY. Somebody has written 'boring' under it.", "LOST CAT. Answers to Tyson. Do not approach.", "The council will be inspecting the lifts. The notice is from 2019.",
            "NO BALL GAMES. NO DOGS. NO LOITERING. NO FUN.", "Whoever keeps nicking parcels: we know who you are. (Flat 12)", "Bulky waste collection: just leave it on the landing like everyone else does."
        };

        /// <summary>The nearest thing in this home TALK would do something with, from where the player stands facing fwd.</summary>
        public bool Nearest(Vector3 me, Vector3 fwd, out int best, out float score)
        {
            best = -1; score = 99f; if ((WCentre - me).sqrMagnitude > radius * radius * 1.3f) return false;
            for (int i = 0; i < spots.Count; i++)
            {
                var s = spots[i]; Vector3 w = transform.TransformPoint(s.pos);
                Vector3 d = w - me; float dy = d.y; d.y = 0f; float dist = d.magnitude;
                float reach = s.kind == "sit" || s.kind == "lie" ? 1.0f : s.kind == "rummage" ? 1.6f : 1.15f;
                if (dist > reach || dy < -0.6f || dy > 2.0f) continue;
                // a long reach (across a bed, over a worktop) mustn't go through a wall
                if (dist > 1.15f) { Vector3 eye = me + Vector3.up * 1.1f, to = w + Vector3.up * 0.75f; if (Physics.Raycast(eye, to - eye, out var blk, Mathf.Max(0f, (to - eye).magnitude - 0.6f), ~0, QueryTriggerInteraction.Ignore)) continue; }
                float ang = dist < 0.05f ? 0f : Vector3.Angle(fwd, d); if (ang > 70f) continue;
                float sc = dist + ang / 70f + 0.1f; if (sc < score) { score = sc; best = i; }
            }
            return best >= 0;
        }

        public void Use(PropWorld pw, PlayerCtl pc, int i)
        {
            var s = spots[i]; Vector3 w = transform.TransformPoint(s.pos);
            switch (s.kind)
            {
                case "rummage": if (!(done != null && done[i])) StartCoroutine(Rummage(pw, pc, i)); else pw.Say("You've been through that already."); break;
                case "sit": { float yaw = transform.eulerAngles.y + s.yaw; pc.SitOn(w + Vector3.up * Mathf.Max(0f, s.h - 0.46f) + Quaternion.Euler(0, yaw, 0) * new Vector3(0f, 0f, -0.08f), yaw); break; }
                case "lie": pw.Say("You sit on the edge of the bed. It creaks."); { float yaw = transform.eulerAngles.y + s.yaw; pc.SitOn(w + Vector3.up * 0.05f + Quaternion.Euler(0, yaw, 0) * new Vector3(0f, 0f, 0.2f), yaw); } break;
                case "tv": ToggleTv(i, w, pw); break;
                case "tap": pw.Say(s.tag == "bath" ? "You run a bath, then think better of it." : s.tag == "kettle" ? "The kettle's still warm. Somebody's been in." : "You rinse your hands. The water's freezing."); break;
                case "fridge": pw.Say(H != null && H.wealth > 0.5f ? "Posh cheese, olives and a bottle of fizz. Not your fridge." : H != null && H.tidy < 0.3f ? "Something green. Something furry. Half a lemon." : "A pint of milk, margarine and some leftovers in foil."); break;
                case "light": ToggleLight(i, w); break;
                case "flush": pw.Say("Flush. It takes three goes."); break;
                case "window": pw.Say("Net curtains, twitching."); break;
                case "chute": pw.Say(Random.value < 0.5f ? "The chute's jammed solid. Somebody's shoved a mattress down it." : "A warm, sour blast comes up the chute. You shut it quick."); break;
                case "say": pw.Say(s.tag); break;
                case "read": pw.Say(Notices[Random.Range(0, Notices.Length)]); break;
                case "garage": { var lk = GetComponent<Lockup>(); if (lk != null) lk.Use(pw, pc, s.room, w); break; }
                case "lift": { var ti = GetComponent<TowerInterior>(); if (ti != null) ti.CallLift(i); else pw.Say("The lift's broken."); break; }
                default: pw.Say(s.label); break;
            }
        }

        // ------------------------------------------------------------------ searching
        System.Collections.IEnumerator Rummage(PropWorld pw, PlayerCtl pc, int i)
        {
            var s = spots[i]; Vector3 w = transform.TransformPoint(s.pos);
            done[i] = true;
            float d = pc.ReachFor(w + Vector3.up * (s.tag == "underbed" || s.tag == "floorboard" ? 0.1f : 0.9f));
            pw.Say(Starter(s.tag));
            NoiseBus.Emit(w, s.tag == "drawers" || s.tag == "cupboard" || s.tag == "wardrobe" ? 7f : 4f, pc.transform);
            yield return new WaitForSeconds(Mathf.Max(0.5f, d) + 1.2f + (s.hidden ? 1.5f : 0f));
            var rng = new System.Random(H.id * 997 + i * 31 + (int)(Mathf.Abs(s.pos.x) * 100f));
            var lkp = GetComponent<Lockup>();
            var loot = lkp != null ? lkp.Loot(s.room, s.tag, rng) : s.tag == "post" ? Household.PostLoot(rng) : s.tag == "till" ? new Household.Loot("Till money", Mathf.Round(25f + (float)rng.NextDouble() * 120f), true) : s.tag == "stock" ? Household.StockLoot(shopKind, rng) : Household.Roll(H, rng, s.hidden);
            float emptyChance = s.tag == "post" ? 0.55f : s.tag == "till" ? 0.2f : s.tag == "stock" ? 0.35f : s.tag == "fridge" || s.tag == "bathcab" || s.tag == "basket" || s.tag == "coats" ? 0.7f : s.hidden ? 0.25f : 0.5f;
            emptyChance += H.stuff < 0.3f ? 0.15f : -0.1f;
            if (H.kind == Household.Kind.Vacant) emptyChance = 0.95f;
            if (rng.NextDouble() < emptyChance) { pw.Say(Empty(rng)); yield break; }
            Pockets.Take(loot, pw, s.hidden);
            if (loot.value >= 100f || s.tag == "till" || s.tag == "stock") { Offences.Report(shop ? "Robbery" : "Burglary", shop ? 20f : 14f, w); }
            if (shop && occupantIn && (s.tag == "till" || s.tag == "stock")) Disturb(w, 1f);
        }
        static string Starter(string tag)
        {
            switch (tag)
            {
                case "sofa": return "You check down the sofa...";
                case "drawers": return "You go through the drawers...";
                case "bedside": return "You open the bedside drawer...";
                case "underbed": return "You look under the bed...";
                case "wardrobe": return "You go through the wardrobe...";
                case "coats": return "You go through the coats...";
                case "fridge": return "You look in the fridge...";
                case "cupboard": return "You search the cupboards...";
                case "sink": return "You search the cupboard under the sink...";
                case "washer": return "You check the washing machine...";
                case "table": return "You look on the table...";
                case "shelf": return "You go through the shelf...";
                case "cabinet": return "You open the cabinet...";
                case "sideboard": return "You open the sideboard...";
                case "phone": return "You check the phone table drawer...";
                case "cistern": return "You lift the cistern lid...";
                case "bathcab": return "You open the bathroom cabinet...";
                case "basket": return "You look in the bathroom basket...";
                case "bag": return "You rip open the black bag...";
                case "floorboard": return "You prise up the loose floorboard...";
                case "safe": return "You try the wall safe...";
                case "till": return "You jemmy the till drawer...";
                case "stock": return "You go through the stock...";
                case "rail": return "You go through the rails...";
                case "counter": return "You rummage under the counter...";
                case "bin": return "You go through the bin...";
                case "fakeback": return "You feel for a false back...";
                case "post": return "You fish about in the post boxes...";
                case "bins": return "You lift the lid and hold your breath...";
                case "bench": return "You go through the workbench...";
                case "toolbox": return "You flip the toolbox open...";
                case "boxes": return "You rip the tape off the boxes...";
                case "tarp": return "You drag the tarp back...";
                case "tomb": return "You put your shoulder to the stone lid. It grinds...";
                default: return "You poke about in it...";
            }
        }
        static readonly string[] EmptyLines =
        {
            "A light, a lemon and a sense of loss.", "Dust, a button and the faint feeling of being watched. Empty.", "Nothing worth having.", "Receipts, a takeaway menu and a dead battery.",
            "Odd socks. Just odd socks.", "A rubber band and an expired coupon. Riveting.", "Bills. Final reminders, mostly."
        };
        static string Empty(System.Random r) => EmptyLines[r.Next(EmptyLines.Length)];

        // ------------------------------------------------------------------ small things
        void ToggleTv(int i, Vector3 w, PropWorld pw)
        {
            tvOn = !tvOn; pw.Say(tvOn ? "The telly comes on, loud. Some quiz show." : "Silence. You killed the quiz.");
            if (tvOn) { NoiseBus.Emit(w, 9f, Player()); StartCoroutine(TvGlow(i, w)); }
        }
        System.Collections.IEnumerator TvGlow(int i, Vector3 w)
        {
            var g = new GameObject("TvLight"); g.transform.position = w + Vector3.up * 0.6f; var l = g.AddComponent<Light>(); l.type = LightType.Point; l.range = 4.5f; l.intensity = 1.2f; l.shadows = LightShadows.None;
            float t = 0f;
            while (tvOn) { t += Time.deltaTime; l.color = Color.Lerp(new Color(0.5f, 0.7f, 1f), new Color(1f, 0.85f, 0.7f), Mathf.PerlinNoise(t * 2.2f, i)); l.intensity = 0.8f + Mathf.PerlinNoise(t * 5f, 3f) * 0.9f; yield return null; }
            Destroy(g);
        }
        readonly Dictionary<int, GameObject> lamps = new Dictionary<int, GameObject>();
        void ToggleLight(int i, Vector3 w)
        {
            if (lamps.TryGetValue(i, out var g) && g != null) { Destroy(g); lamps.Remove(i); return; }
            var rr = rooms[Mathf.Clamp(spots[i].room, 0, rooms.Count - 1)];
            var go = new GameObject("RoomLight"); go.transform.SetParent(transform, false); go.transform.localPosition = rr.centre + Vector3.up * 2.0f;
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.range = 6.5f; l.intensity = 2.2f; l.color = new Color(1f, 0.86f, 0.62f); l.shadows = LightShadows.None;
            lamps[i] = go;
        }

        /// <summary>You've made a racket in here: whoever is home may wake up, or notice you.</summary>
        public void Disturb(Vector3 at, float loud)
        {
            if (!occupantIn || H == null || !H.Occupied) return;
            if (Time.time - noticedAt < 20f) return; noticedAt = Time.time;
            var pc = FindFirstObjectByType<PlayerCtl>(); if (pc == null) return;
            if (awake) { World.Toast($"\"WHO'S THAT? GET OUT OF MY HOUSE!\"", 1); Offences.Wanted = Mathf.Min(100f, Offences.Wanted + 30f); }
            else if (Random.value < 0.6f * loud) { World.Toast("Somewhere upstairs, a bed creaks. Someone's awake.", 1); awake = true; }
        }

        // ------------------------------------------------------------------ distance culling of the inside
        float nextCull;
        static Light glow; static float glowT;
        /// <summary>One pooled warm light in whichever room the camera is in, so interiors aren't pitch black.</summary>
        void RoomGlow()
        {
            var cam = Camera.main; if (cam == null) return;
            Vector3 lp = transform.InverseTransformPoint(cam.transform.position);
            if (zoneRects.Count == 0 || lp.x < zoneRects[0].xMin || lp.x > zoneRects[0].xMax || Mathf.Abs(lp.z - midZ) > halfD || (WCentre - cam.transform.position).sqrMagnitude > 60f) return;
            RoomRec best = null; float bd = 9f;
            foreach (var r in rooms) { if (Mathf.Abs(r.centre.y - lp.y) > 1.6f) continue; if (lp.y + 1.5f > r.centre.y + 3f) continue; float d = new Vector2(r.centre.x - lp.x, r.centre.z - lp.z).magnitude; if (d < bd) { bd = d; best = r; } }
            if (best == null) return;
            if (glow == null) { var g = new GameObject("RoomGlow"); glow = g.AddComponent<Light>(); glow.type = LightType.Point; glow.shadows = LightShadows.None; glow.range = 7f; }
            glow.transform.position = transform.TransformPoint(best.centre + Vector3.up * 2.0f);
            bool night = GameClock.Night; glow.intensity = night ? 1.5f : 0.9f; glow.color = H != null && H.era >= 3 ? new Color(0.85f, 0.9f, 1f) : new Color(1f, 0.86f, 0.64f);
            glowT = Time.time;
        }
        void Update()
        {
            if (Time.time - glowT > 0.5f && glow != null) glow.intensity = 0f;
            RoomGlow();
            if (Time.time < nextCull) return; nextCull = Time.time + 0.5f + Random.value * 0.3f;
            var cam = Camera.main; if (cam == null) return;
            float d = Vector3.Distance(cam.transform.position, transform.TransformPoint(centre));
            bool near = d < 45f, mid = d < 70f;
            if (furnGo != null && furnGo.activeSelf != near) furnGo.SetActive(near);
            if (interiorGos != null) foreach (var g in interiorGos) if (g != null && g.activeSelf != mid) g.SetActive(mid);
            if (!entered && near) { }
        }
    }

    /// <summary>What you've nicked and not yet sold. Cash goes straight in your pocket; everything else is carried until a fence takes it.</summary>
    public static class Pockets
    {
        public static readonly List<Household.Loot> Items = new List<Household.Loot>();
        public static float Value { get { float v = 0f; foreach (var i in Items) v += i.value; return v; } }
        public static void Take(Household.Loot l, PropWorld pw, bool hidden)
        {
            if (l.cash) { Econ.Give(l.value); pw.Say($"{l.name}: £{l.value:0} in your pocket." + (hidden ? " Well hidden. Well found." : "")); return; }
            Items.Add(l);
            pw.Say(l.kind == "keys" ? "A set of spare house keys. Somebody's careless." : $"You pocket: {l.name}. A fence would give you about £{l.value:0}." + (hidden ? " Well hidden. Well found." : ""));
        }
        /// <summary>A fence gives about 40% of what it's worth, more for a regular.</summary>
        public static float Fence(float rate = 0.4f)
        {
            float v = 0f, n = 0; foreach (var i in Items) { v += i.value * rate; n++; }
            Items.Clear(); Econ.Give(v); return v;
        }
    }
}
