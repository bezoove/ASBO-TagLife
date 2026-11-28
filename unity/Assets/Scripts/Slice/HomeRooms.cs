using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    public partial class HomeBuilder
    {
        char lastWall;

        // ------------------------------------------------------------------ what each room is for
        void Assign()
        {
            bool vacant = H.kind == Household.Kind.Vacant;
            var gar = Find(0, "garage");
            if (gar != null)
            {
                float x = (float)rng.NextDouble();
                string role = vacant ? "empty" : H.kind == Household.Kind.Lad || H.kind == Household.Kind.Dealer ? (x < 0.4f ? "mancave" : x < 0.55f ? "gym" : "junk")
                    : H.kind == Household.Kind.Pensioner ? (x < 0.45f ? "workshop" : "junk") : H.kind == Household.Kind.Family ? (x < 0.2f ? "bedroom" : x < 0.3f ? "gym" : "junk")
                    : H.kind == Household.Kind.Hoarder ? "hoard" : (x < 0.15f ? "bedroom" : x < 0.25f ? "mancave" : "junk");
                gar.role = role; if (role == "mancave" || role == "gym" || role == "bedroom") gar.use = "converted";
            }
            // living upstairs: the front room on the first floor becomes the kitchen-diner and the ground floor back becomes the lounge
            bool liveUp = !flat && H.upsideDown && upper1 != 2 && !vacant;
            var lounge = Find(1, "lounge");
            if (lounge != null) { if (liveUp) { lounge.type = "kitchen"; lounge.role = "upstairsKitchen"; } else lounge.role = "lounge"; }
            foreach (var r in rooms)
            {
                if (r.lv != 0) continue;
                if (r.type == "kitchen" && liveUp) { r.type = "lounge"; r.role = "lounge"; }
                else if (r.type == "den") r.role = liveUp ? "lounge" : (H.gamer ? "gaming" : H.kind == Household.Kind.Pensioner ? "sitting" : "diner");
                else if (r.type == "dining") r.role = liveUp ? "lounge" : "diner";
            }
            // bedrooms in order: the biggest first
            var beds = rooms.FindAll(q => q.type == "bed"); beds.Sort((a, b) => ((b.u1 - b.u0) * (b.d1 - b.d0)).CompareTo((a.u1 - a.u0) * (a.d1 - a.d0)));
            int real = Mathf.Clamp(H.bedrooms, 1, beds.Count), kidsLeft = H.kids ? Mathf.Max(1, real - 1) : 0;
            for (int i = 0; i < beds.Count; i++)
            {
                var b = beds[i];
                if (vacant) { b.role = "empty"; continue; }
                if (i == 0) b.role = H.kind == Household.Kind.Student || H.kind == Household.Kind.Lad ? "master" : "master";
                else if (i < real) { if (kidsLeft > 0 && H.kids) { b.role = kidsLeft > 1 && Chance(0.5) ? "bunk" : (H.kind == Household.Kind.LoneMum && Chance(0.4) ? "nursery" : "kid"); kidsLeft--; } else b.role = "single"; }
                else b.role = H.gamer && Chance(0.6) ? "gaming" : Chance(0.5) ? "office" : H.stuff > 0.6f ? "junk" : "single";
                if (H.kind == Household.Kind.Hoarder && i > 0) b.role = "junk";
            }
            foreach (var r in rooms) if (r.role == null) r.role = r.type;
        }

        // ------------------------------------------------------------------ the lot
        void Furnish()
        {
            int mark = solids.Count; string why = "";
            bare.Clear(); for (int l = 0; l < 3; l++) hard[l].Clear();
            if (!Connected(out why)) { foreach (var rr in unreached) bare.Add(rr); Debug.LogWarning($"[ASBO] home {name} [g{ground}u{upper1}s{upper2}m{(mirror ? 1 : 0)}]: STRUCTURALLY cut off (no furniture): {why}\n{dbgMap}"); }
            for (int attempt = 0; attempt < 9; attempt++)
            {
                furn = new MeshKit(); deco = new MeshKit(); if (solids.Count > mark) solids.RemoveRange(mark, solids.Count - mark);
                home.spots.Clear(); home.rooms.Clear(); for (int l = 0; l < 3; l++) { clear[l].Clear(); hard[l].Clear(); }
                thin = attempt >= 6 ? 2 : attempt >= 3 ? 1 : 0;
                FurnishOnce();
                if (Connected(out why)) return;
                if (attempt == 7) foreach (var rr in unreached) skip.Add(rr);      // last resorts: leave the cut-off rooms bare
            }
            Debug.LogWarning($"[ASBO] home {name} [g{ground}u{upper1}s{upper2}m{(mirror ? 1 : 0)}]: still cut off after furnishing: {why}\n{dbgMap}");
        }
        readonly List<Room> unreached = new List<Room>(); int thin; string dbgMap = ""; readonly HashSet<Room> bare = new HashSet<Room>();
        static bool NearR(Rect r, float u, float d, float m) => u > r.xMin - m && u < r.xMax + m && d > r.yMin - m && d < r.yMax + m;
        /// <summary>Flood a 10 cm grid on each floor (walls, gaps, stairwells, furniture all inflated by a player's radius) and check every real room can be reached.</summary>
        bool Connected(out string why, int onlyLv = -1)
        {
            why = ""; unreached.Clear(); const float cs = 0.1f, rad = 0.31f;
            int nx = Mathf.CeilToInt((U1 - U0) / cs), nd = Mathf.CeilToInt((D1 - D0) / cs); bool ok = true;
            for (int lv = 0; lv < Levels; lv++)
            {
                if (onlyLv >= 0 && lv != onlyLv) continue;
                var blocked = new bool[nx, nd];
                for (int ix = 0; ix < nx; ix++)
                    for (int id = 0; id < nd; id++)
                    {
                        float u = U0 + (ix + 0.5f) * cs, d = D0 + (id + 0.5f) * cs; bool b = u < U0 + rad || u > U1 - rad || d < D0 + rad || d > D1 - rad;
                        if (!b) foreach (var h in hard[lv]) if (NearR(h, u, d, rad - 0.02f)) { b = true; break; }
                        if (!b) foreach (var v in voids) if (v.lv == lv && NearR(v.r, u, d, 0.1f)) { b = true; break; }
                        if (!b) foreach (var q in blocks[lv]) if (NearR(q, u, d, rad)) { b = true; break; }
                        if (!b) foreach (var w in walls)
                        {
                            if (w.lv != lv) continue; float along = w.axis == 'u' ? u : d, across = w.axis == 'u' ? d : u;
                            if (Mathf.Abs(across - w.at) >= w.thick * 0.5f + rad - 0.02f || along < w.from - 0.05f || along > w.to + 0.05f) continue;
                            bool gap = false; foreach (var g in w.gaps) if (Mathf.Abs(along - g.c) <= g.w * 0.5f - rad + 0.06f) { gap = true; break; }
                            if (!gap) { b = true; break; }
                        }
                        blocked[ix, id] = b;
                    }
                // seed
                float su, sd;
                if (lv == 0) { var hl = rooms.Find(q => q.lv == 0 && q.type == "hall" && q.use != "stairfoot"); su = hl.Cu; sd = hl.Cd; } else if (lv == 1) { su = 4.75f; sd = 0.56f; } else { su = 0.65f; sd = 4.6f; }
                int six = Mathf.Clamp(Mathf.FloorToInt((su - U0) / cs), 0, nx - 1), sid = Mathf.Clamp(Mathf.FloorToInt((sd - D0) / cs), 0, nd - 1);
                var seen = new bool[nx, nd]; var qu = new Queue<Vector2Int>();
                for (int rr = 0; rr < 8 && qu.Count == 0; rr++)
                    for (int dx = -rr; dx <= rr && qu.Count == 0; dx++)
                        for (int dz = -rr; dz <= rr; dz++) { int a = six + dx, c = sid + dz; if (a < 0 || c < 0 || a >= nx || c >= nd || blocked[a, c]) continue; qu.Enqueue(new Vector2Int(a, c)); seen[a, c] = true; break; }
                while (qu.Count > 0)
                {
                    var p = qu.Dequeue();
                    for (int k = 0; k < 4; k++)
                    {
                        int a = p.x + (k == 0 ? 1 : k == 1 ? -1 : 0), c = p.y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (a < 0 || c < 0 || a >= nx || c >= nd || blocked[a, c] || seen[a, c]) continue; seen[a, c] = true; qu.Enqueue(new Vector2Int(a, c));
                    }
                }
                foreach (var r in rooms)
                {
                    if (r.lv != lv || bare.Contains(r) || r.use == "stairfoot" || r.type == "cupboard" || Mathf.Min(r.u1 - r.u0, r.d1 - r.d0) < 0.8f) continue;
                    bool got = false;
                    for (int ix = 0; ix < nx && !got; ix++) for (int id = 0; id < nd; id++) if (seen[ix, id] && r.Has(U0 + (ix + 0.5f) * cs, D0 + (id + 0.5f) * cs)) { got = true; break; }
                    if (!got) { if (ok) { var sbm = new System.Text.StringBuilder($"floor {lv}: # blocked, o reached, . free but cut off\n"); for (int id = 0; id < nd; id++) { for (int ix = 0; ix < nx; ix++) sbm.Append(blocked[ix, id] ? '#' : seen[ix, id] ? 'o' : '.'); sbm.Append('\n'); } dbgMap = sbm.ToString(); } ok = false; unreached.Add(r); why += r.type + lv + (r.role != null && r.role != r.type ? "/" + r.role : "") + " "; }
                }
            }
            return ok;
        }

        void FurnishOnce()
        {
            // materials for this house
            switch (H.era)
            {
                case 0: wood = Surf("coated_pine", 0.9f, new Color(0.72f, 0.45f, 0.24f), 0.3f); wood2 = Surf("coated_pine", 0.9f, new Color(0.55f, 0.32f, 0.18f), 0.3f); break;
                case 1: wood = Surf("coated_pine", 0.9f, new Color(0.95f, 0.8f, 0.58f), 0.3f); wood2 = Surf("coated_pine", 0.9f, new Color(0.8f, 0.6f, 0.4f), 0.3f); break;
                case 2: wood = Flat(new Color(0.92f, 0.92f, 0.9f), 0.55f); wood2 = Flat(new Color(0.6f, 0.6f, 0.62f), 0.5f); break;
                default: wood = Flat(new Color(0.16f, 0.16f, 0.18f), 0.6f); wood2 = Surf("wood_slats_dark", 0.8f, new Color(0.9f, 0.8f, 0.7f), 0.4f); break;
            }
            metal = FlatM(new Color(0.7f, 0.72f, 0.75f), 0.7f, 0.8f); black = Flat(new Color(0.05f, 0.05f, 0.06f), 0.5f); ceramic = CotMats.ceramic;
            worktop = H.era >= 2 ? FlatM(new Color(0.18f, 0.18f, 0.2f), 0.8f, 0f) : Surf("coated_pine", 0.9f, new Color(0.85f, 0.7f, 0.5f), 0.3f);
            InitClear();
            foreach (var r in rooms) { r.taken.Clear(); }
            // room records for the runtime
            foreach (var r in rooms) home.rooms.Add(new Home.RoomRec { type = r.type, use = r.role, lv = r.lv, centre = P(r.Cu, Y[r.lv], r.Cd), u0 = r.u0, u1 = r.u1, d0 = r.d0, d1 = r.d1 });
            foreach (var r in rooms)
            {
                if (skip.Contains(r)) continue;
                if (r.type == "garage") Garage(r);
                else if (r.type == "hall") HallFill(r);
                else if (r.type == "landing") LandingFill(r);
                else if (r.type == "lounge" || (r.type == "den" && (r.role == "lounge" || r.role == "sitting")) || r.role == "lounge") Living(r);
                else if (r.type == "den" || r.type == "dining") Dining(r);
                else if (r.type == "kitchen") KitchenFill(r);
                else if (r.type == "utility") UtilityFill(r);
                else if (r.type == "cupboard") CupboardFill(r);
                else if (r.type == "wc") WcFill(r);
                else if (r.type == "bath") BathFill(r);
                else if (r.type == "bed") BedFill(r);
            }
            // every room: windows dressed, radiator, ceiling light and switch; then the mess
            foreach (var r in rooms) { Dress(r); if (H.Occupied && !skip.Contains(r)) Mess(r); }
            foreach (var w in wins) if (w.lv >= 0) { var wi = WinIn(w); var rm = RoomAt(w.lv, wi.x, wi.y); if (rm != null && rm.type != "garage" && H.Occupied) Curtains(w); }
        }

        // ------------------------------------------------------------------ every room
        void Dress(Room r)
        {
            // ceiling light
            float cu = r.Cu, cd = r.Cd;
            var rose = Flat(new Color(0.96f, 0.95f, 0.92f), 0.2f);
            Bx(deco, rose, cu - 0.09f, cu + 0.09f, C[r.lv] - 0.025f, C[r.lv], cd - 0.09f, cd + 0.09f);
            Bx(deco, Gfx.Emissive(new Color(1f, 0.92f, 0.75f), r.lv == 0 && r.type == "garage" ? 1.5f : 2.2f), cu - 0.14f, cu + 0.14f, C[r.lv] - 0.07f, C[r.lv] - 0.025f, cd - 0.14f, cd + 0.14f);
            // light switch beside the first doorway on a wall of this room
            foreach (var w in walls)
            {
                if (w.lv != r.lv) continue;
                foreach (var g in w.gaps)
                {
                    float su, sd, fu, fd;
                    if (w.axis == 'u') { if (!(r.Has(g.c, w.at - 0.3f) || r.Has(g.c, w.at + 0.3f))) continue; bool side = r.Has(g.c, w.at - 0.3f); su = g.c + g.w / 2 + 0.2f; sd = w.at + (side ? -0.07f : 0.07f); fu = 0f; fd = side ? 1f : -1f; sd += side ? 0f : 0f; }
                    else { if (!(r.Has(w.at - 0.3f, g.c) || r.Has(w.at + 0.3f, g.c))) continue; bool side = r.Has(w.at - 0.3f, g.c); su = w.at + (side ? -0.07f : 0.07f); sd = g.c + g.w / 2 + 0.2f; fu = side ? 1f : -1f; fd = 0f; }
                    Bx(deco, Flat(new Color(0.95f, 0.95f, 0.93f), 0.4f), su - 0.04f, su + 0.04f, Y[r.lv] + 1.2f, Y[r.lv] + 1.3f, sd - 0.04f, sd + 0.04f);
                    if (r.type != "garage" || true) AddSpot(r, "light", "Flick the light switch", "switch", su, sd, Y[r.lv], fu, fd);
                    goto done;
                }
            }
            done:
            // a radiator under the first window on a front or back wall
            foreach (var w in wins)
            {
                if (w.lv != r.lv || r.type == "garage") continue; float mu = (w.u0 + w.u1) * 0.5f; var wi = WinIn(w); var rm = RoomAt(r.lv, wi.x, wi.y); if (rm != r) continue;
                if (w.sill > 0.85f || w.sill < 0.5f) { if (w.sill < 0.5f) continue; }
                if (w.u1 - w.u0 > 3.5f && w.sill < 0.8f) { mu = w.u0 + 0.9f; }
                char sd = w.Side;
                if (sd == 'F' || sd == 'B') { furn.Push(P(mu, Y[r.lv], sd == 'F' ? D0 + 0.001f : D1 - 0.001f), sd == 'F' ? 0f : 180f); }
                else { furn.Push(P(sd == 'L' ? U0 + 0.001f : U1 - 0.001f, Y[r.lv], mu), WorldYaw(sd == 'L' ? -1f : 1f, 0f)); }
                Radiator(Mathf.Min(1.1f, w.u1 - w.u0 - 0.2f)); furn.Pop();
                break;
            }
        }

        // ------------------------------------------------------------------ the mess a household leaves
        void Mess(Room r)
        {
            if (r.type == "garage" || r.role == "empty") return;
            float amount = (1f - H.tidy) * (0.4f + H.stuff) + (H.kind == Household.Kind.Hoarder ? 1.2f : 0f);
            int n = Mathf.RoundToInt(amount * 5.5f * Mathf.Clamp01((r.u1 - r.u0) * (r.d1 - r.d0) / 8f + 0.3f));
            bool living = r.type == "lounge" || r.type == "den" || r.role == "lounge";
            for (int i = 0; i < n; i++)
            {
                float x = (float)rng.NextDouble();
                if (r.type == "bath" || r.type == "wc") { if (x < 0.5f) Scatter(r, 0.35f, 0.3f, 0.2f, yw => Pile(Color.HSVToRGB(R(0f, 1f), 0.3f, 0.8f), 0.3f)); continue; }
                if (r.type == "kitchen") { if (x < 0.35f) Scatter(r, 0.4f, 0.3f, 0.3f, yw => Bag(new Color(0.08f, 0.08f, 0.1f))); else if (x < 0.55f && H.drinker) Scatter(r, 0.3f, 0.3f, 0.12f, yw => Cans(4)); else Scatter(r, 0.4f, 0.4f, 0.05f, yw => Pizza()); continue; }
                if (x < 0.30f) Scatter(r, 0.5f, 0.45f, 0.2f, yw => Pile(Color.HSVToRGB(R(0f, 1f), R(0.2f, 0.6f), R(0.35f, 0.9f)), R(0.35f, 0.6f)));
                else if (x < 0.45f && H.drinker) Scatter(r, 0.3f, 0.3f, 0.12f, yw => Cans(R(3, 7) > 5 ? 6 : 3));
                else if (x < 0.55f && living) Scatter(r, 0.4f, 0.4f, 0.05f, yw => Pizza());
                else if (x < 0.65f && H.kids) Scatter(r, 0.6f, 0.6f, 0.2f, yw => Toys());
                else if (x < 0.75f && H.smoker && living) Scatter(r, 0.2f, 0.2f, 0.05f, yw => Ashtray());
                else if (x < 0.88f) Scatter(r, 0.5f, 0.4f, 0.4f, yw => Bag(Chance(0.5) ? new Color(0.08f, 0.08f, 0.1f) : new Color(0.7f, 0.65f, 0.4f)));
                else Scatter(r, 0.4f, 0.4f, 0.15f, yw => Pile(new Color(0.55f, 0.5f, 0.4f), 0.4f));
            }
            if (H.pets && living && Chance(0.8)) Put(r, 0.9f, 0.9f, 0.14f, "FBLR", DogBed, null, null, null, false, 0.02f, true, false);
            if (H.stuff > 0.7f) for (int i = 0; i < 2; i++) Put(r, 0.4f, 0.35f, 0.5f, "FBLR", () => { Bag(new Color(R(0.3f, 0.8f), R(0.3f, 0.7f), R(0.3f, 0.7f))); }, null, null, null, false, 0.02f, false, false);
        }

        // ------------------------------------------------------------------ rooms
        void Living(Room r)
        {
            bool vacant = !H.Occupied; if (vacant) return;
            float area = (r.u1 - r.u0) * (r.d1 - r.d0);
            var sofa = H.wealth > 0.5f && Chance(0.6) ? Surf("leather_quilt2", 0.7f, Shade(H.sofa, 1.1f), 0.5f) : Fab(H.sofa);
            // the telly wall first, the settee opposite it
            string walls = Chance(0.5) ? "FB" : "BF"; if (r.u1 - r.u0 > r.d1 - r.d0) walls = Chance(0.5) ? "LR" : "RL";
            string tvSides = walls + (walls == "FB" || walls == "BF" ? "LR" : "FB");
            float tvw = H.era >= 2 ? 1.3f : 1.0f;
            bool tv = Put(r, tvw, 0.42f, 0.9f, tvSides, () => TvUnit(tvw), "tv", "Turn the telly on", "tv");
            char tw = lastWall; lastTv = new Vector2(lastCand.cu, lastCand.cd); string opp = tw == 'F' ? "B" : tw == 'B' ? "F" : tw == 'L' ? "R" : "L";
            float sw = area > 12f ? 2.1f : area > 8f ? 1.9f : 1.6f;
            bool s1 = Put(r, sw, 0.92f, 0.9f, tv ? opp + "FBLR" : "FBLR", () => Sofa(sw, sofa), "sit", "Sit on the sofa", "sofa", false, 0.05f, false, true, 0.45f);
            if (s1) { var c = lastCand; AddSpot(r, "rummage", "Check down the sofa", "sofa", c.cu + (c.wall == 'F' || c.wall == 'B' ? sw / 2 - 0.1f : 0f), c.cd + (c.wall == 'L' || c.wall == 'R' ? sw / 2 - 0.1f : 0f), Y[r.lv], c.fu, c.fd, false); }
            if (thin < 1 && area > 9f && Chance(0.65)) { float cw = 0.95f; if (Put(r, cw, 0.9f, 0.9f, "FBLR", () => Armchair(sofa), "sit", "Sit in the armchair", "armchair", false, 0.05f, true, true, 0.45f)) { } }
            if (thin < 1 && area > 14f && Chance(0.5)) Put(r, 1.5f, 0.9f, 0.9f, "FBLR", () => Sofa(1.5f, sofa), "sit", "Sit on the settee", "settee", false, 0.05f, false, true, 0.45f);
            if (thin < 2 && tv && s1) { PutAt(r, (lastTv.x + lastCand.cu) * 0.5f, (lastTv.y + lastCand.cd) * 0.5f, 1.0f, 0.5f, 0.42f, tw == 'F' || tw == 'B', CoffeeTable, "rummage", "Look on the table", "table", false, 0.1f); }
            if (H.era == 0 || (H.era == 1 && Chance(0.4))) Put(r, 1.2f, 0.28f, 1.05f, "FBLR", FireplaceOld, null, null, null, false, 0.05f, false, true);
            Put(r, 1.4f, 0.46f, 0.88f, "FBLR", () => Sideboard(1.4f), "rummage", "Open the sideboard", "sideboard");
            if (H.stuff > 0.35f || H.kind == Household.Kind.Student) Put(r, 0.9f, 0.32f, 1.8f, "FBLR", () => Bookcase(0.9f), "rummage", "Go through the shelf", "shelf");
            if (H.stuff > 0.6f) Put(r, 0.9f, 0.32f, 1.8f, "FBLR", () => Bookcase(0.9f), "rummage", "Go through the shelf", "shelf");
            if (H.stuff > 0.45f) Put(r, 0.4f, 0.4f, 1.6f, "FBLR", StandardLamp, null, null, null, false, 0.02f, true);
            if (H.era >= 2 || H.wealth > 0.4f) Put(r, 0.4f, 0.4f, 1.1f, "FBLR", () => Plant(R(0.7f, 1.1f)), null, null, null, false, 0.02f, true);
            Rug(r, r.Cu, r.Cd, Mathf.Min(2.4f, (r.u1 - r.u0) - 0.7f), Mathf.Min(1.9f, (r.d1 - r.d0) - 0.7f), H.era >= 2 ? Surf("rug_brown", 1.5f, Shade(H.carpet, 1.1f), 0.1f) : CotMats.rug);
        }
        Cand lastCand; Vector2 lastTv;

        void Dining(Room r)
        {
            if (!H.Occupied) return;
            bool diner = r.role == "diner" || r.type == "dining";
            if (r.role == "gaming") { Put(r, 1.2f, 0.65f, 0.8f, "LRFB", () => Desk(true), null, null, null, false, 0.05f, false, true); Put(r, 1.0f, 0.9f, 0.5f, "FBLR", () => Sofa(1.0f, Fab(H.sofa)), "sit", "Sit down", "beanbag", false, 0.05f, false, true, 0.4f); PutAt(r, r.Cu, r.Cd, 0.6f, 0.6f, 0.9f, true, OfficeChair, "sit", "Sit at the computer", "pc", false, 0.05f, 0.5f); return; }
            if (r.role == "sitting") { Put(r, 0.95f, 0.9f, 0.9f, "FBLR", () => Armchair(Fab(H.sofa)), "sit", "Sit in the armchair", "armchair", false, 0.05f, true, true, 0.45f); Put(r, 1.0f, 0.46f, 0.9f, "FBLR", () => TvUnit(1.0f), "tv", "Turn the telly on", "tv"); Put(r, 1.3f, 0.46f, 0.88f, "FBLR", () => Sideboard(1.3f), "rummage", "Open the sideboard", "sideboard"); return; }
            float w = Mathf.Min(1.6f, Mathf.Max(r.u1 - r.u0, r.d1 - r.d0) - 1.2f); if (w < 0.9f) w = 0.9f;
            bool along = (r.u1 - r.u0) >= (r.d1 - r.d0);
            if (thin < 1 && Mathf.Min(r.u1 - r.u0, r.d1 - r.d0) > 2.2f && PutAt(r, r.Cu, r.Cd, w, 0.85f, 0.78f, along, () => DiningTable(w, 0.85f, 4), "rummage", "Look on the table", "table", false, 0.5f))
            {
                // chairs round the table, in the table's own spot
                var c = lastCand; int cn = w > 1.3f ? 4 : 2;
                for (int i = 0; i < cn; i++)
                {
                    bool sideA = i % 2 == 0; float off = (i / 2 == 0 ? -1f : 1f) * w * 0.22f; float cu = c.cu, cd = c.cd, fu = 0, fd = 0;
                    if (along) { cu = c.cu + off; cd = c.cd + (sideA ? -0.62f : 0.62f); fd = sideA ? 1f : -1f; } else { cd = c.cd + off; cu = c.cu + (sideA ? -0.62f : 0.62f); fu = sideA ? 1f : -1f; }
                    if (cn == 2) { cu = c.cu; cd = c.cd; if (along) { cu = c.cu + (sideA ? -w * 0.35f : w * 0.35f); cd = c.cd; fd = 0; fu = sideA ? 1f : -1f; } else { cd = c.cd + (sideA ? -w * 0.35f : w * 0.35f); cu = c.cu; fd = sideA ? 1f : -1f; fu = 0; } }
                    var fp = new Rect(cu - 0.22f, cd - 0.22f, 0.44f, 0.44f); if (!OkLoose(r, fp)) continue; r.taken.Add(fp);
                    furn.Push(P(cu, Y[r.lv], cd), WorldYaw(fu, fd)); Chair(); furn.Pop(); solids.Add((P(cu, Y[r.lv] + 0.45f, cd), new Vector3(0.44f, 0.9f, 0.44f), 0f)); HardRect(r.lv, fp);
                    AddSpot(r, "sit", "Sit down", "chair", cu, cd, Y[r.lv], fu, fd, false, 0.45f);
                }
            }
            Put(r, 1.4f, 0.46f, 0.88f, "FBLR", () => Sideboard(1.4f), "rummage", "Open the sideboard", "sideboard");
            if (H.stuff > 0.5f) Put(r, 0.9f, 0.32f, 1.8f, "FBLR", () => Bookcase(0.9f), "rummage", "Go through the shelf", "shelf");
            if (r.type == "dining" && H.kind == Household.Kind.Pensioner) Put(r, 1.2f, 0.28f, 1.05f, "FBLR", FireplaceOld, null, null, null, false, 0.05f, false, true);
        }
        bool OkLoose(Room r, Rect fp)
        {
            var inn = Inner(r); if (fp.xMin < inn.xMin - 0.01f || fp.xMax > inn.xMax + 0.01f || fp.yMin < inn.yMin - 0.01f || fp.yMax > inn.yMax + 0.01f) return false;
            foreach (var t in r.taken) if (t.Overlaps(fp) && !t.Equals(default(Rect))) { if (Mathf.Abs(t.width - 0.44f) > 0.01f && t.width * t.height > 0.8f) continue; return false; }
            foreach (var c in clear[r.lv]) if (c.Overlaps(fp)) return false; return true;
        }

        void KitchenFill(Room r)
        {
            if (!H.Occupied) return;
            bool up = r.role == "upstairsKitchen"; var inn = Inner(r);
            // the run along the longest free wall, appliances in a plausible order
            var order = new List<int>(); order.Add(0); order.Add(1); order.Add(2); order.Add(1); order.Add(3); order.Add(1); order.Add(1);
            string[] sides = (r.u1 - r.u0) >= (r.d1 - r.d0) ? new[] { "B", "F" } : new[] { "L", "R" };
            if (sides[0] == "B" && Chance(0.5)) sides = new[] { "F", "B" };
            bool done = KitchenRun(r, sides[0][0], order);
            if (!done || (r.u1 - r.u0) * (r.d1 - r.d0) > 11f) KitchenRun(r, sides[1][0], new List<int> { 4, 1, 1, 3 });
            float area = (r.u1 - r.u0) * (r.d1 - r.d0);
            if (thin < 1 && area > 9f && Mathf.Min(r.u1 - r.u0, r.d1 - r.d0) > 3.0f && (H.era >= 1 || H.kind == Household.Kind.Family) && !(flat && (up || area < 12f)))
            {
                float w = 1.2f; bool along = (r.u1 - r.u0) >= (r.d1 - r.d0);
                if (PutAt(r, r.Cu, r.Cd, w, 0.75f, 0.76f, along, () => DiningTable(w, 0.75f, 2, 0.74f), "rummage", "Look on the table", "table", false, 0.5f)) { var c = lastCand; for (int i = 0; i < 2; i++) { bool a = i == 0; float cu = along ? c.cu + (a ? -0.3f : 0.3f) : c.cu + (a ? -0.55f : 0.55f), cd = along ? c.cd + (a ? -0.55f : 0.55f) : c.cd + (a ? -0.3f : 0.3f); var fp = new Rect(cu - 0.22f, cd - 0.22f, 0.44f, 0.44f); if (!OkLoose(r, fp)) continue; r.taken.Add(fp); float fu = along ? 0 : (a ? 1 : -1), fd = along ? (a ? 1 : -1) : 0; furn.Push(P(cu, Y[r.lv], cd), WorldYaw(fu, fd)); Chair(); furn.Pop(); solids.Add((P(cu, Y[r.lv] + 0.45f, cd), new Vector3(0.44f, 0.9f, 0.44f), 0f)); HardRect(r.lv, fp); AddSpot(r, "sit", "Sit down", "chair", cu, cd, Y[r.lv], fu, fd, false, 0.45f); } }
            }
            if (up) { Put(r, 1.9f, 0.9f, 0.9f, "FBLR", () => Sofa(1.9f, Fab(H.sofa)), "sit", "Sit on the sofa", "sofa", false, 0.05f, false, true, 0.45f); Put(r, 1.2f, 0.42f, 0.9f, "FBLR", () => TvUnit(1.2f), "tv", "Turn the telly on", "tv"); }
        }
        // unit kinds: 0 fridge, 1 base unit, 2 cooker, 3 washer, 4 sink unit
        bool KitchenRun(Room r, char wall, List<int> kinds)
        {
            var inn = Inner(r); float pos = (wall == 'F' || wall == 'B') ? inn.xMin : inn.yMin, end = (wall == 'F' || wall == 'B') ? inn.xMax : inn.yMax; bool any = false;
            foreach (int k in kinds)
            {
                float w = k == 1 ? 0.6f + (rng.Next(3) == 0 ? 0.4f : 0f) : k == 4 ? 1.0f : 0.62f; float h = k == 0 ? 1.75f : 0.9f; bool placed = false;
                for (float p = pos; p + w <= end + 0.001f; p += 0.05f)
                {
                    Cand c; float cc = p + w / 2;
                    if (wall == 'F' || wall == 'B') { float cd = wall == 'F' ? inn.yMin + 0.31f : inn.yMax - 0.31f; c = new Cand { fp = new Rect(cc - w / 2, cd - 0.31f, w, 0.62f), cu = cc, cd = cd, fu = 0, fd = wall == 'F' ? 1 : -1, wall = wall }; }
                    else { float cu = wall == 'L' ? inn.xMin + 0.31f : inn.xMax - 0.31f; c = new Cand { fp = new Rect(cu - 0.31f, cc - w / 2, 0.62f, w), cu = cu, cd = cc, fu = wall == 'L' ? 1 : -1, fd = 0, wall = wall }; }
                    if (!Ok(r, c.fp, h, wall, 0.0f)) continue;
                    bool high = k != 0 && Ok(r, c.fp, 2.15f, wall, 0f) && Chance(0.8);
                    float ww = w; int kk = k; bool sink = k == 4 || (k == 1 && !any && Chance(0.3));
                    if (!Commit(r, c, w, 0.62f, h, () => { if (kk == 0) Fridge(); else if (kk == 2) { Cooker(); } else if (kk == 3) Washer(); else { KitchenUnit(ww, sink); } if (high && kk != 0 && kk != 3) { furn.Push(Vector3.zero, 0f); WallUnit(ww); furn.Pop(); } },
                        k == 0 ? "fridge" : k == 2 ? "use" : null, k == 0 ? "Open the fridge" : k == 2 ? "Check the cooker" : null, k == 0 ? "fridge" : "cooker", false, true, 0f)) continue;
                    if (k == 4 || k == 1 && sink) { AddSpot(r, "rummage", "Search the cupboard under the sink", "sink", c.cu, c.cd, Y[r.lv], c.fu, c.fd, false); AddSpot(r, "tap", "Put the kettle on", "kettle", c.cu, c.cd, Y[r.lv], c.fu, c.fd); }
                    else if (k == 1) AddSpot(r, "rummage", "Search the kitchen cupboards", "cupboard", c.cu, c.cd, Y[r.lv], c.fu, c.fd, false);
                    if (k == 3) AddSpot(r, "rummage", "Check the washing machine", "washer", c.cu, c.cd, Y[r.lv], c.fu, c.fd, false);
                    pos = p + w + 0.01f; placed = true; any = true; break;
                }
                if (!placed) continue;
            }
            return any;
        }

        void UtilityFill(Room r)
        {
            if (!H.Occupied) return;
            Put(r, 0.62f, 0.62f, 0.9f, "BLRF", Washer, "rummage", "Check the washing machine", "washer");
            Put(r, 0.62f, 0.62f, 0.9f, "BLRF", Freezer);
            Put(r, 0.8f, 0.4f, 1.6f, "LRBF", () => Shelving(0.8f, 1.6f, true), "rummage", "Go through the shelf", "shelf");
            if (Chance(0.6)) Put(r, 0.5f, 0.5f, 0.5f, "LRBF", () => Bag(new Color(0.8f, 0.78f, 0.7f)), "rummage", "Look in the laundry basket", "basket", false, 0.02f, false, false);
        }
        void CupboardFill(Room r)
        {
            Put(r, Mathf.Min(0.7f, r.u1 - r.u0 - 0.1f), 0.35f, 1.6f, "BFLR", () => Shelving(0.7f, 1.6f, true), "rummage", "Rifle through the cupboard", "cupboard", false);
        }
        void WcFill(Room r)
        {
            Put(r, 0.45f, 0.7f, 0.9f, "LRBF", () => Toilet(), "rummage", "Lift the cistern lid", "cistern", true, 0.03f);
            Put(r, 0.55f, 0.45f, 0.95f, "LRBF", Basin, "tap", "Wash your hands", "basin", false, 0.03f);
        }
        void BathFill(Room r)
        {
            bool shower = r.role == "shower" || r.role == "ensuite" || (H.era >= 3 && Chance(0.5));
            if (shower) Put(r, 0.95f, 0.95f, 2f, "LRBF", () => Bath(true), null, null, null, false, 0.02f, true);
            else Put(r, 0.72f, 1.72f, 0.56f, "LRBF", () => Bath(false), "tap", "Run the bath", "bath", false, 0.02f, true);
            Put(r, 0.45f, 0.7f, 0.9f, "LRBF", () => Toilet(), "rummage", "Lift the cistern lid", "cistern", true, 0.03f);
            Put(r, 0.55f, 0.45f, 0.95f, "LRBF", Basin, "tap", "Wash your hands", "basin", false, 0.03f);
            if (lastCand.wall != 'x') { var c = lastCand; furn.Push(P(c.cu, Y[r.lv], c.cd), WorldYaw(c.fu, c.fd)); furn.Push(new Vector3(0, 0, -0.27f), 0f); MirrorCab(); furn.Pop(); furn.Pop(); AddSpot(r, "rummage", "Open the bathroom cabinet", "bathcab", c.cu, c.cd, Y[r.lv], c.fu, c.fd, false); }
            if (Chance(0.7)) Put(r, 0.4f, 0.35f, 0.5f, "LRBF", () => { Cy(Fab(new Color(0.8f, 0.7f, 0.5f)), 0, 0, 0, 0.2f, 0.45f, 10); }, "rummage", "Look in the bathroom basket", "basket", false, 0.02f, false, false);
            Rug(r, r.Cu, r.Cd, 0.6f, 0.4f, Fab(Shade(H.curtain, 1.1f)));
        }

        void HallFill(Room r)
        {
            if (!H.Occupied) return;
            if (r.use == "stairfoot") { return; }
            // coats on the wall by the front door, shoes, the phone table
            Put(r, 1.0f, 0.2f, 1.9f, "FLRB", () => { B(wood, 0, 1.45f, 0, 1.0f, 0.04f, 0.1f); for (int i = 0; i < 4; i++) { B(Fab(new Color(R(0.1f, 0.6f), R(0.1f, 0.5f), R(0.1f, 0.5f))), -0.36f + i * 0.24f, 0.45f, 0.1f, 0.2f, 1.0f, 0.12f); } }, "rummage", "Go through the coats", "coats", false, 0.02f, false, false);
            Put(r, 0.9f, 0.38f, 0.8f, "LRFB", () => { B(wood, 0, 0f, 0, 0.9f, 0.78f, 0.36f); B(black, 0.2f, 0.78f, 0f, 0.2f, 0.07f, 0.22f); }, "rummage", "Check the phone table drawer", "phone");
            if (H.kids && Chance(0.7)) Put(r, 0.5f, 1.0f, 0.9f, "LRFB", () => { B(Flat(new Color(0.2f, 0.25f, 0.3f)), 0, 0.3f, 0, 0.5f, 0.5f, 0.9f); Cy(black, -0.2f, 0, 0.3f, 0.12f, 0.1f, 8); Cy(black, 0.2f, 0, 0.3f, 0.12f, 0.1f, 8); Cy(black, -0.2f, 0, -0.3f, 0.12f, 0.1f, 8); Cy(black, 0.2f, 0, -0.3f, 0.12f, 0.1f, 8); }, null, null, null, false, 0.05f, true, true);
            else if (Chance(0.35)) Put(r, 0.5f, 1.0f, 1.1f, "LRFB", Bike, null, null, null, false, 0.03f, true, true);
            Put(r, 0.7f, 0.3f, 0.3f, "FBLR", () => { for (int i = 0; i < 6; i++) B(Flat(Color.HSVToRGB(R(0f, 1f), 0.1f, R(0.2f, 0.9f))), -0.25f + i * 0.1f, 0, R(-0.05f, 0.05f), 0.09f, 0.1f, 0.24f); }, null, null, null, false, 0.02f, false, false);
            Rug(r, r.Cu, r.Cd, Mathf.Max(0.6f, r.u1 - r.u0 - 0.6f), Mathf.Max(0.6f, r.d1 - r.d0 - 0.6f), H.era >= 2 ? Fab(Shade(H.carpet, 1.15f)) : CotMats.rug2);
        }
        void LandingFill(Room r)
        {
            if (!H.Occupied) return;
            if (Chance(0.5)) Put(r, 0.45f, 0.4f, 0.5f, "FBLR", () => { Cy(Fab(new Color(0.8f, 0.7f, 0.5f)), 0, 0, 0, 0.2f, 0.5f, 10); }, "rummage", "Look in the laundry basket", "basket", false, 0.02f, false, false);
            if (Chance(0.5)) Put(r, 0.4f, 0.4f, 1.1f, "FBLR", () => Plant(R(0.6f, 1.0f)), null, null, null, false, 0.02f, true);
            if (Chance(0.4)) Put(r, 0.9f, 0.4f, 1.9f, "FBLR", () => Shelving(0.9f, 1.9f, true), "rummage", "Look in the airing cupboard", "cupboard", false);
        }

        void BedFill(Room r)
        {
            if (r.role == "empty" || !H.Occupied) { if (!H.Occupied && Chance(0.5)) Scatter(r, 0.5f, 0.5f, 0.3f, yw => Bag(new Color(0.08f, 0.08f, 0.1f))); return; }
            var rolling = r.role;
            float bw = rolling == "master" ? (H.wealth > 0.5f ? 1.6f : 1.4f) : rolling == "single" || rolling == "kid" || rolling == "guest" ? 0.95f : rolling == "bunk" ? 0.95f : 0.9f;
            bool tall = (r.u1 - r.u0) > 2.6f;
            var bedc = rolling == "kid" || rolling == "bunk" ? Color.HSVToRGB(R(0f, 1f), 0.5f, 0.9f) : H.bedding;
            if (rolling == "office" || rolling == "gaming")
            {
                Put(r, 1.2f, 0.65f, 0.8f, "LRFB", () => Desk(true), null, null, null, false, 0.05f, true);
                PutAt(r, lastCand.cu + 0.2f * (lastCand.fu), lastCand.cd + 0.5f * (lastCand.fd), 0.6f, 0.6f, 0.9f, true, OfficeChair, "sit", "Sit at the computer", "pc", false, 0.03f, 0.5f);
                Put(r, 1.0f, 0.4f, 1.8f, "LRFB", () => Bookcase(1.0f), "rummage", "Go through the shelf", "shelf");
                if (rolling == "gaming") Put(r, 1.0f, 0.9f, 0.5f, "LRFB", () => Sofa(1.0f, Fab(Color.HSVToRGB(R(0f, 1f), 0.5f, 0.6f))), "sit", "Flop on the beanbag", "beanbag", false, 0.05f, false, true, 0.4f);
                return;
            }
            if (rolling == "junk")
            {
                for (int i = 0; i < 5; i++) Put(r, 0.6f, 0.45f, 0.5f, "FBLR", () => { B(Flat(new Color(R(0.5f, 0.75f), R(0.4f, 0.6f), R(0.25f, 0.4f))), 0, 0, 0, 0.55f, R(0.3f, 0.5f), 0.42f); }, "rummage", "Poke about in a box", "bag", Chance(0.3), 0.02f, i < 2);
                Put(r, 1.0f, 0.5f, 0.85f, "FBLR", () => Drawers(1.0f), "rummage", "Go through the drawers", "drawers");
                return;
            }
            if (rolling == "nursery") { Put(r, 0.7f, 1.3f, 1.0f, "FBLR", () => Bed(1f, false, true, bedc), null, null, null, false, 0.05f, true); Put(r, 0.9f, 0.5f, 0.85f, "FBLR", () => Drawers(0.9f), "rummage", "Go through the drawers", "drawers"); Scatter(r, 0.6f, 0.6f, 0.2f, yw => Toys()); return; }
            bool bunk = rolling == "bunk";
            bool bedOk = Put(r, bw, 2.05f, bunk ? 1.8f : 0.55f, "BFLR", () => Bed(bw, bunk, false, bedc), "lie", "Sit on the bed", "bed", false, 0.04f, tall && Chance(0.4), true, 0.45f);
            if (bedOk)
            {
                var c = lastCand;
                AddSpot(r, "rummage", "Look under the bed", "underbed", c.cu, c.cd, Y[r.lv], c.fu, c.fd, true);
                if (bw > 1.2f) { float off = bw / 2 + 0.28f; foreach (float sg in new[] { -1f, 1f }) { float su = c.cu + (c.wall == 'F' || c.wall == 'B' ? sg * off : 0f), sd = c.cd + (c.wall == 'L' || c.wall == 'R' ? sg * off : 0f); su += c.fu * (-0.82f); sd += c.fd * (-0.82f); var fp = new Rect(su - 0.21f, sd - 0.19f, 0.42f, 0.38f); if (c.wall == 'L' || c.wall == 'R') fp = new Rect(su - 0.19f, sd - 0.21f, 0.38f, 0.42f); if (!OkLoose(r, fp)) continue; r.taken.Add(fp); furn.Push(P(su, Y[r.lv], sd), WorldYaw(c.fu, c.fd)); Bedside(); furn.Pop(); solids.Add((P(su, Y[r.lv] + 0.26f, sd), new Vector3(fp.width, 0.52f, fp.height), 0f)); HardRect(r.lv, fp); if (sg > 0) AddSpot(r, "rummage", "Open the bedside drawer", "bedside", su, sd, Y[r.lv], c.fu, c.fd, false); } }
            }
            float ww = rolling == "master" ? 1.3f : 0.95f;
            Put(r, ww, 0.6f, 1.95f, "LRFB", () => Wardrobe(ww, H.era >= 2), "rummage", "Go through the wardrobe", "wardrobe");
            Put(r, 0.9f, 0.48f, 0.85f, "LRFB", () => Drawers(0.9f), "rummage", "Go through the drawers", "drawers");
            if (rolling == "master" && H.era >= 2 && H.wealth > 0.3f) Put(r, 0.9f, 0.45f, 0.5f, "LRFB", () => { B(wood, 0, 0f, 0, 0.9f, 0.45f, 0.4f); B(black, 0, 0.45f, 0, 0.5f, 0.35f, 0.04f); }, null, null, null, false, 0.03f, false, true);
            if (rolling == "kid" || rolling == "bunk") { Put(r, 1.0f, 0.55f, 0.75f, "LRFB", () => Desk(false), null, null, null, false, 0.03f); Scatter(r, 0.6f, 0.6f, 0.2f, yw => Toys()); }
            if (rolling == "master" && H.kind != Household.Kind.Pensioner && Chance(0.3)) Put(r, 1.0f, 0.5f, 0.75f, "LRFB", () => Desk(false));
            if (H.stuff > 0.5f && Chance(0.6)) Put(r, 0.6f, 0.4f, 0.5f, "FBLR", () => Bag(new Color(R(0.3f, 0.8f), R(0.3f, 0.7f), R(0.3f, 0.7f))), "rummage", "Rip open the bag", "bag", false, 0.02f, true, false);
            if (Chance(0.25)) AddSpot(r, "rummage", "Prise up the loose floorboard", "floorboard", r.Cu, r.Cd, Y[r.lv], 0, 1, true);
        }

        void Garage(Room r)
        {
            if (r.role == "empty") return;
            string role = r.role;
            var inn = Inner(r);
            if (role == "junk" || role == "hoard" || role == "workshop")
            {
                Put(r, 1.8f, 0.62f, 1.0f, "LRB", Workbench, "rummage", "Search the workbench", "table", Chance(0.25));
                Put(r, 1.4f, 0.4f, 1.9f, "LRB", () => Shelving(1.4f, 1.9f, true), "rummage", "Go through the shelving", "shelf");
                Put(r, 0.7f, 0.65f, 0.95f, "LRB", Freezer, "rummage", "Look in the freezer", "fridge");
                if (role == "hoard") for (int i = 0; i < 8; i++) Put(r, 0.6f, 0.5f, 0.6f, "FBLR", () => B(Flat(new Color(R(0.5f, 0.75f), R(0.4f, 0.6f), R(0.25f, 0.4f))), 0, 0, 0, 0.55f, R(0.3f, 0.6f), 0.42f), "rummage", "Poke about in a box", "bag", Chance(0.3), 0.02f);
                if (role != "workshop") { Put(r, 0.5f, 1.0f, 1.1f, "LRB", Bike, null, null, null, false, 0.02f, true); Put(r, 0.5f, 1.0f, 1.1f, "LRB", Bike, null, null, null, false, 0.02f, true); }
                if (role == "workshop") Put(r, 1.0f, 0.1f, 1.8f, "LRB", () => { B(wood, 0, 0.5f, 0, 1.0f, 1.3f, 0.02f); for (int i = 0; i < 12; i++) B(metal, -0.4f + (i % 6) * 0.15f, 0.8f + (i / 6) * 0.35f, 0.02f, 0.03f, 0.2f, 0.03f); }, null, null, null, false, 0.01f, false, false);
                for (int i = 0; i < 4; i++) Scatter(r, 0.35f, 0.35f, 0.4f, yw => Bag(Chance(0.5) ? new Color(0.08f, 0.08f, 0.1f) : new Color(0.7f, 0.65f, 0.4f)));
                Rug(r, r.Cu, r.Cd, 0.1f, 0.1f, black);
                return;
            }
            if (role == "mancave")
            {
                Put(r, 2.1f, 0.92f, 0.9f, "RLB", () => Sofa(2.1f, Fab(Shade(H.sofa, 0.9f))), "sit", "Sit on the sofa", "sofa", false, 0.05f, false, true, 0.45f);
                Put(r, 1.4f, 0.46f, 0.9f, "LRF", () => TvUnit(1.4f), "tv", "Turn the telly on", "tv");
                Put(r, 0.62f, 0.62f, 0.9f, "LRB", () => Freezer(), "fridge", "Open the fridge", "barfridge");
                for (int i = 0; i < 3; i++) Scatter(r, 0.3f, 0.3f, 0.12f, yw => Cans(5));
                Put(r, 1.2f, 0.28f, 1.05f, "LRB", () => { B(wood, 0, 0.3f, 0, 0.5f, 0.5f, 0.02f); Cy(Flat(new Color(0.1f, 0.5f, 0.2f)), 0, 0.55f, 0.02f, 0.2f, 0.02f, 16); }, null, null, null, false, 0.01f, false, false);
                Rug(r, r.Cu, r.Cd, 2.0f, 1.6f, Fab(Shade(H.carpet, 0.8f)));
                return;
            }
            if (role == "gym")
            {
                Put(r, 1.8f, 0.7f, 0.5f, "LRB", () => { B(black, 0, 0.4f, 0, 0.35f, 0.08f, 1.6f); B(metal, 0, 0f, -0.5f, 0.05f, 0.9f, 0.05f); B(metal, 0, 0f, 0.5f, 0.05f, 0.9f, 0.05f); B(metal, 0, 0.9f, 0, 1.6f, 0.04f, 0.04f); foreach (float x in new[] { -0.7f, 0.7f }) B(black, x, 0.55f, 0, 0.06f, 0.4f, 0.4f); }, null, null, null, false, 0.05f);
                Put(r, 1.2f, 0.5f, 1.2f, "LRB", () => { B(metal, 0, 0f, 0, 0.04f, 1.2f, 1.2f); for (int i = 0; i < 4; i++) Cy(black, 0.1f, 0.2f + i * 0.25f, 0.3f * (i % 2 == 0 ? 1 : -1), 0.17f, 0.04f, 10); }, null, null, null, false, 0.05f);
                Put(r, 0.8f, 1.6f, 1.2f, "LRB", () => { B(black, 0, 0.1f, 0, 0.6f, 0.15f, 1.5f); B(black, 0, 0.9f, -0.7f, 0.7f, 0.1f, 0.1f); B(metal, 0.25f, 0.2f, -0.65f, 0.04f, 1.1f, 0.04f); }, "use", "Have a go on the treadmill", "treadmill");
                Rug(r, r.Cu, r.Cd, 2f, 2f, Flat(new Color(0.12f, 0.12f, 0.13f), 0.2f));
                return;
            }
            if (role == "bedroom")
            {
                Put(r, 1.4f, 2.05f, 0.55f, "LRB", () => Bed(1.4f, false, false, H.bedding), "lie", "Sit on the bed", "bed", false, 0.04f, true, true, 0.45f);
                Put(r, 1.0f, 0.6f, 1.95f, "LRB", () => Wardrobe(1.0f, false), "rummage", "Go through the wardrobe", "wardrobe");
                Put(r, 0.9f, 0.48f, 0.85f, "LRB", () => Drawers(0.9f), "rummage", "Go through the drawers", "drawers");
                Rug(r, r.Cu, r.Cd, 2.0f, 1.6f, Fab(Shade(H.carpet, 1.0f)));
            }
        }

        // ------------------------------------------------------------------ the lot ends here: build the objects
        void Finish()
        {
            var inGo = shell.Build("Shell", root, false, ShadowCastingMode.Off);
            var trimGo = fin.Build("Trim", root, false, ShadowCastingMode.Off);
            var glassGo = glass.Build("Glass", root, false, ShadowCastingMode.Off);
            var furnGo = furn.Build("Furniture", root, false, ShadowCastingMode.Off);
            var decoGo = deco.Build("Deco", root, false, ShadowCastingMode.Off); if (decoGo != null) SetLayer(decoGo, Bld.DetailLayer);
            if (furnGo != null) SetLayer(furnGo, Bld.DetailLayer);
            if (trimGo != null) SetLayer(trimGo, Bld.DetailLayer);
            var cols = new GameObject("Solids"); cols.transform.SetParent(root, false);
            foreach (var s in solids)
            {
                var bc = cols.AddComponent<BoxCollider>(); bc.center = s.c; bc.size = new Vector3(Mathf.Max(0.02f, s.s.x), Mathf.Max(0.02f, s.s.y), Mathf.Max(0.02f, s.s.z));
            }
            Vector3 za = P(U0, 0f, D0), zb = P(U1, 0f, D1);
            home.zoneRects.Add(Rect.MinMaxRect(Mathf.Min(za.x, zb.x), Mathf.Min(za.z, zb.z), Mathf.Max(za.x, zb.x), Mathf.Max(za.z, zb.z)));
            if (flat) { home.variant = flatVariant; home.centre = P(W * 0.5f, Y[0] + 1.3f, D * 0.5f); home.radius = Mathf.Max(7f, 0.62f * Mathf.Max(W, D)); home.midZ = -D * 0.5f; home.halfD = D * 0.5f + 0.3f; }
            else { home.variant = $"g{ground}-u{upper1}-s{upper2}-m{(mirror ? 1 : 0)}"; home.centre = P(W * 0.5f, 3.0f, D * 0.5f); home.radius = 7f; }
            home.Setup(new[] { inGo, trimGo, glassGo, decoGo }, furnGo);
        }
        static void SetLayer(GameObject g, int l) { g.layer = l; foreach (Transform t in g.transform) SetLayer(t.gameObject, l); }
    }
}
