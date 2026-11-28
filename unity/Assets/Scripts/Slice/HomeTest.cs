using System.Collections;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>Debug: stats for every house, and a walk round one of them with a screenshot per room.</summary>
    public static class HomeTest
    {
        public static void Stats()
        {
            var sb = new System.Text.StringBuilder();
            int n = 0, spots = 0, doors = 0, rooms = 0; var kinds = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var h in Home.All) { if (h == null) continue; n++; spots += h.spots.Count; doors += h.doors.Count; rooms += h.rooms.Count; foreach (var s in h.spots) { kinds.TryGetValue(s.kind, out int c); kinds[s.kind] = c + 1; } }
            foreach (var k in kinds) sb.Append(k.Key + "=" + k.Value + " ");
            Debug.Log($"[ASBO] homes: {n} rooms={rooms} spots={spots} doors={doors} :: {sb}");
        }
        /// <summary>Physical reachability: flood a 20 cm grid through each house (doors held open, step-ups allowed) from the hall and report rooms it can't reach.</summary>
        public static void Reach() { new GameObject("HomeReach").AddComponent<Run3>(); }
        class Run3 : MonoBehaviour
        {
            IEnumerator Start()
            {
                yield return new WaitForSeconds(1f);
                var list = new System.Collections.Generic.List<Home>(Home.All); list.RemoveAll(x => x == null || x.shop || x.variant == null || !x.variant.StartsWith("g")); list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                int bad = 0, total = 0, diag = 0, dbgN = 0; var sb = new System.Text.StringBuilder();
                foreach (var h in list)
                {
                    foreach (var d in h.doors) if (d.door != null) foreach (var c in d.door.GetComponentsInChildren<Collider>()) c.enabled = false;
                    Physics.SyncTransforms();
                    var zr = h.zoneRects[0]; float cs = 0.1f; int nx = Mathf.CeilToInt((zr.xMax - zr.xMin) / cs), nz = Mathf.CeilToInt((HomeBuilder.HD1 - HomeBuilder.HD0) / cs);
                    Home.RoomRec start = null; foreach (var r in h.rooms) if (r.type == "hall" && r.lv == 0 && r.use != "stairfoot") { start = r; break; }
                    if (start == null) foreach (var r in h.rooms) if (r.lv == 0) { start = r; break; }
                    var seen = new System.Collections.Generic.HashSet<long>(); var q = new System.Collections.Generic.Queue<Vector3>(); var reached = new System.Collections.Generic.List<Vector3>();
                    Vector3 s0 = start.centre; q.Enqueue(new Vector3(s0.x, s0.y + 0.05f, s0.z));
                    int guard = 0;
                    while (q.Count > 0 && guard++ < 200000)
                    {
                        var p = q.Dequeue(); long key = ((long)Mathf.RoundToInt((p.x - zr.xMin) / cs) * 4096 + Mathf.RoundToInt((p.z + HomeBuilder.HD1) / cs)) * 64 + Mathf.RoundToInt(p.y / 0.5f);
                        if (!seen.Add(key)) continue; reached.Add(p);
                        foreach (var dv in new[] { new Vector2(cs, 0), new Vector2(-cs, 0), new Vector2(0, cs), new Vector2(0, -cs) })
                        {
                            float nxp = p.x + dv.x, nzp = p.z + dv.y; if (nxp < zr.xMin || nxp > zr.xMax || nzp > -HomeBuilder.HD0 || nzp < -HomeBuilder.HD1) continue;
                            Vector3 wp = h.transform.TransformPoint(new Vector3(nxp, p.y + 0.45f, nzp));
                            bool watch = h == list[0] && nxp > 4.5f && nxp < 5.0f && -nzp > 3.3f && -nzp < 3.8f && dbgN < 14;
                            if (!Physics.Raycast(wp, Vector3.down, out var hit, 0.95f, ~0, QueryTriggerInteraction.Ignore)) { if (watch) { dbgN++; sb.Append($"\n  watch ({nxp:0.00},{-nzp:0.00}) from y{p.y:0.00}: no ground"); } continue; }
                            float gy = h.transform.InverseTransformPoint(hit.point).y; if (gy - p.y > 0.4f || p.y - gy > 0.7f) { if (watch) { dbgN++; sb.Append($"\n  watch ({nxp:0.00},{-nzp:0.00}) from y{p.y:0.00}: step gy={gy:0.00} {hit.collider.name}"); } continue; }
                            Vector3 a = h.transform.TransformPoint(new Vector3(nxp, gy + 0.65f, nzp)), b = h.transform.TransformPoint(new Vector3(nxp, gy + 1.5f, nzp));
                            if (Physics.CheckCapsule(a, b, 0.28f, ~0, QueryTriggerInteraction.Ignore)) { if (watch) { dbgN++; sb.Append($"\n  watch ({nxp:0.00},{-nzp:0.00}) from y{p.y:0.00}: capsule at gy={gy:0.00}"); } continue; }
                            q.Enqueue(new Vector3(nxp, gy, nzp));
                        }
                    }
                    var miss = new System.Collections.Generic.List<string>();
                    foreach (var r in h.rooms)
                    {
                        if (r.use == "stairfoot") continue; total++; bool ok = false; foreach (var p in reached) if (Mathf.Abs(p.y - r.centre.y) < 0.9f && (p.x - r.centre.x) * (p.x - r.centre.x) + (p.z - r.centre.z) * (p.z - r.centre.z) < 1.6f * 1.6f) { ok = true; break; }
                        if (!ok) { bad++; miss.Add(r.type + r.lv + (r.use != r.type && r.use != null ? "/" + r.use : ""));
                            if (diag++ < 4)
                            {
                                Vector3 bp = reached[0]; float bd = 1e9f; foreach (var p in reached) { if (Mathf.Abs(p.y - r.centre.y) > 0.9f) continue; float dd2 = (p.x - r.centre.x) * (p.x - r.centre.x) + (p.z - r.centre.z) * (p.z - r.centre.z); if (dd2 < bd) { bd = dd2; bp = p; } }
                                sb.Append($"\n    [{h.name} {r.type}{r.lv}] centre local ({r.centre.x:0.0},{r.centre.z:0.0}) nearest reached ({bp.x:0.0},{bp.z:0.0}) dist {Mathf.Sqrt(bd):0.0}; blockers around it:");
                                foreach (var dv in new[] { new Vector2(0.2f, 0), new Vector2(-0.2f, 0), new Vector2(0, 0.2f), new Vector2(0, -0.2f) })
                                {
                                    Vector3 a = h.transform.TransformPoint(new Vector3(bp.x + dv.x, bp.y + 0.5f, bp.z + dv.y)), b2 = h.transform.TransformPoint(new Vector3(bp.x + dv.x, bp.y + 1.5f, bp.z + dv.y));
                                    foreach (var c in Physics.OverlapCapsule(a, b2, 0.2f, ~0, QueryTriggerInteraction.Ignore)) { var lb = h.transform.InverseTransformPoint(c.bounds.center); var ls = c.bounds.size; sb.Append($" {c.name}@({lb.x:0.0},{lb.y:0.0},{lb.z:0.0}) sz({ls.x:0.0},{ls.y:0.0},{ls.z:0.0})"); }
                                    sb.Append(" |");
                                }
                            }
                        }
                    }
                    Vector3 top = reached[0]; foreach (var p in reached) if (p.y > top.y) top = p; if (miss.Count > 0) sb.Append($"\n  {h.name} [{h.variant}]: top y={top.y:0.00} at ({top.x:0.0},{top.z:0.0}) zone x {zr.xMin:0.0}..{zr.xMax:0.0} reached {reached.Count} cells, unreachable [{string.Join(",", miss)}]");
                    foreach (var d in h.doors) if (d.door != null) foreach (var c in d.door.GetComponentsInChildren<Collider>()) c.enabled = true;
                    yield return null;
                }
                Debug.Log($"[ASBO] reach: {bad}/{total} rooms unreachable{sb}"); Destroy(gameObject);
            }
        }

        /// <summary>The same physical flood for the shops and the flats over them (unit footprint: lx across, d 0.14..10.86).</summary>
        public static void ReachShops() { new GameObject("ShopReach").AddComponent<RunShops>(); }
        class RunShops : MonoBehaviour
        {
            IEnumerator Start()
            {
                yield return new WaitForSeconds(1f);
                var list = new System.Collections.Generic.List<Home>(); foreach (var x in Home.All) if (x != null && x.shop) list.Add(x); list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                int bad = 0, total = 0, spots = 0, doors = 0, rummage = 0; var sb = new System.Text.StringBuilder();
                foreach (var h in list)
                {
                    foreach (var d in h.doors) if (d.door != null) foreach (var c in d.door.GetComponentsInChildren<Collider>()) c.enabled = false;
                    Physics.SyncTransforms();
                    spots += h.spots.Count; doors += h.doors.Count; foreach (var s in h.spots) if (s.kind == "rummage") rummage++;
                    var zr = h.zoneRects[0]; float cs = 0.1f;
                    Home.RoomRec start = null; foreach (var r in h.rooms) if (r.type == "shop") { start = r; break; }
                    var seen = new System.Collections.Generic.HashSet<long>(); var q = new System.Collections.Generic.Queue<Vector3>(); var reached = new System.Collections.Generic.List<Vector3>();
                    // start just inside the shop door, on the pavement side
                    Vector3 s0 = start.centre; q.Enqueue(new Vector3(s0.x, s0.y + 0.05f, -1.0f));
                    // seed from the doorway: find the front door
                    foreach (var d in h.doors) if (d.tag == "shopfront" && d.door != null) { var lp = h.transform.InverseTransformPoint(d.door.transform.position); q.Clear(); foreach (float ox in new[] { 0f, 0.35f, -0.35f }) q.Enqueue(new Vector3(lp.x + ox, 0.04f, -0.6f)); }
                    int guard = 0;
                    while (q.Count > 0 && guard++ < 400000)
                    {
                        var p = q.Dequeue(); long key = ((long)Mathf.RoundToInt((p.x - zr.xMin) / cs) * 4096 + Mathf.RoundToInt((p.z + 11f) / cs)) * 64 + Mathf.RoundToInt(p.y / 0.5f);
                        if (!seen.Add(key)) continue; reached.Add(p);
                        foreach (var dv in new[] { new Vector2(cs, 0), new Vector2(-cs, 0), new Vector2(0, cs), new Vector2(0, -cs) })
                        {
                            float nxp = p.x + dv.x, nzp = p.z + dv.y; if (nxp < zr.xMin || nxp > zr.xMax || nzp > -0.0f || nzp < -10.9f) continue;
                            Vector3 wp = h.transform.TransformPoint(new Vector3(nxp, p.y + 0.45f, nzp));
                            if (!Physics.Raycast(wp, Vector3.down, out var hit, 0.95f, ~0, QueryTriggerInteraction.Ignore)) continue;
                            float gy = h.transform.InverseTransformPoint(hit.point).y; if (gy - p.y > 0.4f || p.y - gy > 0.7f) continue;
                            Vector3 a = h.transform.TransformPoint(new Vector3(nxp, gy + 0.65f, nzp)), b = h.transform.TransformPoint(new Vector3(nxp, gy + 1.5f, nzp));
                            if (Physics.CheckCapsule(a, b, 0.28f, ~0, QueryTriggerInteraction.Ignore)) continue;
                            q.Enqueue(new Vector3(nxp, gy, nzp));
                        }
                    }
                    var miss = new System.Collections.Generic.List<string>(); float topY = 0f; foreach (var p in reached) topY = Mathf.Max(topY, p.y);
                    foreach (var r in h.rooms)
                    {
                        total++; bool ok = false; foreach (var p in reached) if (Mathf.Abs(p.y - r.centre.y) < 0.9f && (p.x - r.centre.x) * (p.x - r.centre.x) + (p.z - r.centre.z) * (p.z - r.centre.z) < 1.6f * 1.6f) { ok = true; break; }
                        if (!ok) { bad++; miss.Add(r.type + r.lv + (r.use != r.type && r.use != null ? "/" + r.use : "")); }
                    }
                    if (miss.Count > 0) sb.Append($"\n  {h.name} [{h.variant}] top y={topY:0.00} reached {reached.Count}, unreachable [{string.Join(",", miss)}]");
                    foreach (var d in h.doors) if (d.door != null) foreach (var c in d.door.GetComponentsInChildren<Collider>()) c.enabled = true;
                    yield return null;
                }
                Debug.Log($"[ASBO] shopreach: {bad}/{total} rooms unreachable in {list.Count} units; {spots} spots ({rummage} rummage), {doors} doors{sb}"); Destroy(gameObject);
            }
        }

        public static void MapShopNamed(string key, int lv) { foreach (var x in Home.All) if (x != null && x.shop && x.name.Replace(" ", "-") == key) { MapShopH(x, lv); return; } Debug.Log("[ASBO] shopmap: no " + key); }
        public static void MapShop(int idx, int lv)
        {
            var list = new System.Collections.Generic.List<Home>(); foreach (var x in Home.All) if (x != null && x.shop) list.Add(x); list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            MapShopH(list[idx], lv);
        }
        static void MapShopH(Home h, int lv)
        { float y = 0f; foreach (var r in h.rooms) if (r.lv == lv) { y = r.centre.y; break; }
            foreach (var dd in h.doors) if (dd.door != null) foreach (var c in dd.door.GetComponentsInChildren<Collider>()) c.enabled = false;
            Physics.SyncTransforms(); float x0 = h.zoneRects[0].xMin - 0.12f, wd = h.zoneRects[0].width + 0.24f;
            var sb = new System.Text.StringBuilder($"[ASBO] shopmap {h.name} [{h.variant}] floor {lv} y={y:0.00} width {wd:0.0}\n");
            for (float d = 0.1f; d < 11f; d += 0.1f)
            {
                for (float u = 0.1f; u < wd; u += 0.1f)
                {
                    Vector3 a = h.transform.TransformPoint(new Vector3(x0 + u, y + 0.65f, -d)), b = h.transform.TransformPoint(new Vector3(x0 + u, y + 1.5f, -d));
                    sb.Append(Physics.CheckCapsule(a, b, 0.28f, ~0, QueryTriggerInteraction.Ignore) ? '#' : '.');
                }
                sb.Append('\n');
            }
            foreach (var dd in h.doors) if (dd.door != null) foreach (var c in dd.door.GetComponentsInChildren<Collider>()) c.enabled = true;
            Debug.Log(sb.ToString());
        }
        /// <summary>ASCII map of the whole floor plan of one (unmirrored) house on one floor: # = capsule blocked, . = free (doors open).</summary>
        public static void Map(int house, int lv)
        {
            var list = new System.Collections.Generic.List<Home>(Home.All); list.RemoveAll(x => x == null); list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var h = list[house]; float x0 = h.zoneRects[0].xMin - 0.2f; float y = HomeBuilder.HY[lv];
            foreach (var dd in h.doors) if (dd.door != null) foreach (var c in dd.door.GetComponentsInChildren<Collider>()) c.enabled = false;
            Physics.SyncTransforms();
            var sb = new System.Text.StringBuilder($"[ASBO] map {h.name} [{h.variant}] floor {lv} (u across 0.2..5.2, d down 0.1..8.5)\n");
            for (float d = 0.1f; d < 8.5f; d += 0.1f)
            {
                for (float u = 0.2f; u < 5.25f; u += 0.1f)
                {
                    Vector3 a = h.transform.TransformPoint(new Vector3(x0 + u, y + 0.65f, -d)), b = h.transform.TransformPoint(new Vector3(x0 + u, y + 1.5f, -d));
                    sb.Append(Physics.CheckCapsule(a, b, 0.28f, ~0, QueryTriggerInteraction.Ignore) ? '#' : '.');
                }
                sb.Append('\n');
            }
            Debug.Log(sb.ToString());
        }
        public static void StairProbe(int house)
        {
            var list = new System.Collections.Generic.List<Home>(Home.All); list.RemoveAll(x => x == null); list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var h = list[house]; float x0 = h.zoneRects[0].xMin - 0.2f; var sb = new System.Text.StringBuilder("[ASBO] stairprobe:");
            foreach (var dd in h.doors) if (dd.door != null) foreach (var c in dd.door.GetComponentsInChildren<Collider>()) c.enabled = false;
            Physics.SyncTransforms();
            foreach (float u in new[] { 4.6f, 4.75f, 4.9f })
            {
                sb.Append($"\n u={u}:");
                for (float d = 3.9f; d >= 0.9f; d -= 0.2f)
                {
                    Vector3 wp = h.transform.TransformPoint(new Vector3(x0 + u, 3.2f, -d));
                    bool ray = Physics.Raycast(wp, Vector3.down, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore); float gy = ray ? h.transform.InverseTransformPoint(hit.point).y : -9f;
                    // from just above the tread under us, not from the ceiling
                    Vector3 wp2 = h.transform.TransformPoint(new Vector3(x0 + u, 0.15f + (3.9f - d) * 0.98f + 0.45f, -d)); bool r2 = Physics.Raycast(wp2, Vector3.down, out var hit2, 0.95f, ~0, QueryTriggerInteraction.Ignore); float g2 = r2 ? h.transform.InverseTransformPoint(hit2.point).y : -9f;
                    Vector3 a = h.transform.TransformPoint(new Vector3(x0 + u, g2 + 0.5f, -d)), b = h.transform.TransformPoint(new Vector3(x0 + u, g2 + 1.5f, -d));
                    var cols = Physics.OverlapCapsule(a, b, 0.28f, ~0, QueryTriggerInteraction.Ignore); string who = ""; foreach (var c in cols) { var lb = h.transform.InverseTransformPoint(c.bounds.center); var ls = c.bounds.size; who += $" {c.name}({lb.x - x0:0.0},{lb.y:0.0},{-lb.z:0.0};{ls.x:0.0},{ls.y:0.0},{ls.z:0.0})"; }
                    sb.Append($"\n   d{d:0.0} gy={g2:0.00} blocked={cols.Length}{who}");
                }
            }
            Debug.Log(sb.ToString());
        }
        public static void ProbeGap(int house, float d)
        {
            var list = new System.Collections.Generic.List<Home>(Home.All); list.RemoveAll(x => x == null); list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var h = list[house]; float x0 = h.zoneRects[0].xMin - 0.2f; var sb = new System.Text.StringBuilder($"[ASBO] probegap {h.name} d={d}:");
            foreach (var dd in h.doors) if (dd.door != null) foreach (var c in dd.door.GetComponentsInChildren<Collider>()) c.enabled = false;
            Physics.SyncTransforms();
            for (float u = 2.2f; u <= 4.4f; u += 0.1f)
            {
                Vector3 a = h.transform.TransformPoint(new Vector3(x0 + u, 0.15f + 0.5f, -d)), b = h.transform.TransformPoint(new Vector3(x0 + u, 0.15f + 1.5f, -d));
                var cols = Physics.OverlapCapsule(a, b, 0.2f, ~0, QueryTriggerInteraction.Ignore); sb.Append($"\n  u{u:0.0}:");
                foreach (var c in cols) { var lb = h.transform.InverseTransformPoint(c.bounds.center); var ls = c.bounds.size; sb.Append($" {c.name}@({lb.x - x0:0.00},{lb.y:0.0},{-lb.z:0.00}) sz({ls.x:0.00},{ls.y:0.0},{ls.z:0.00})"); }
            }
            Debug.Log(sb.ToString());
        }
        public static void Probe()
        {
            var list = new System.Collections.Generic.List<Home>(Home.All); list.RemoveAll(x => x == null); list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var h = list[0]; var sb = new System.Text.StringBuilder("[ASBO] probe:");
            foreach (var d in h.doors) if (d.door != null) foreach (var c in d.door.GetComponentsInChildren<Collider>()) c.enabled = false;
            Physics.SyncTransforms();
            foreach (float z in new[] { -1.4f, -2.3f })
                for (float x = 3.8f; x <= 5.1f; x += 0.1f)
                {
                    Vector3 wp = h.transform.TransformPoint(new Vector3(x, 1.2f, z));
                    bool ray = Physics.Raycast(wp, Vector3.down, out var hit, 1.5f, ~0, QueryTriggerInteraction.Ignore);
                    float gy = ray ? h.transform.InverseTransformPoint(hit.point).y : -9f;
                    Vector3 a = h.transform.TransformPoint(new Vector3(x, gy + 0.5f, z)), b = h.transform.TransformPoint(new Vector3(x, gy + 1.5f, z));
                    var cols = Physics.OverlapCapsule(a, b, 0.2f, ~0, QueryTriggerInteraction.Ignore); string who = cols.Length > 0 ? cols[0].name + "@" + cols[0].transform.parent?.name : "-";
                    sb.Append($"\n  z{z:0.0} x{x:0.0} gy={gy:0.00} {(ray ? hit.collider.name : "none")} blocked={cols.Length} {who}");
                }
            Debug.Log(sb.ToString());
        }
        public static void Burgle(int house) { new GameObject("HomeBurgle").AddComponent<Run2>().house = house; }
        class Run2 : MonoBehaviour
        {
            public int house;
            IEnumerator Start()
            {
                yield return new WaitForSeconds(1f);
                var pc = FindFirstObjectByType<PlayerCtl>(); var list = new System.Collections.Generic.List<Home>(Home.All); list.RemoveAll(x => x == null); list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                var h = list[house]; int locked = 0; foreach (var d in h.doors) if (d.locked) locked++;
                Debug.Log($"[ASBO] burgle: {h.name} doors={h.doors.Count} locked={locked} occupied={h.occupantIn}");
                int tried = 0, did = 0;
                for (int i = 0; i < h.spots.Count && tried < 6; i++)
                {
                    var sp = h.spots[i]; if (sp.kind != "rummage" || sp.lv != 0) continue; tried++;
                    Vector3 w = h.transform.TransformPoint(sp.pos); Vector3 back = Quaternion.Euler(0, h.transform.eulerAngles.y + sp.yaw, 0) * Vector3.forward;
                    pc.Teleport(w + back * 0.75f + Vector3.up * 0.1f, h.transform.eulerAngles.y + sp.yaw + 180f, 8f);
                    yield return new WaitForSeconds(0.6f);
                    bool ok = PropWorld.TryUse(); if (ok) did++;
                    Debug.Log($"[ASBO] burgle: {sp.tag} prompt='{h.Prompt(i)}' used={ok} room={h.rooms[sp.room].type}");
                    yield return new WaitForSeconds(4.5f);
                }
                Debug.Log($"[ASBO] burgle: used {did}/{tried}, pockets={Pockets.Items.Count} value={Pockets.Value:0} cash={Econ.Cash:0}");
                Destroy(gameObject);
            }
        }


        /// <summary>One shot from the middle of a room of a shop, looking along yaw (degrees, unit frame: 0 = towards the back) and pitch (down positive).</summary>
        public static void ShotRoom(string key, int room, float yaw, float pitch) { var g = new GameObject("ShotRoom").AddComponent<RunShotRoom>(); g.key = key; g.room = room; g.yaw = yaw; g.pitch = pitch; }
        class RunShotRoom : MonoBehaviour
        {
            public string key; public int room; public float yaw, pitch;
            IEnumerator Start()
            {
                yield return new WaitForSeconds(1f);
                var pc = FindFirstObjectByType<PlayerCtl>(); Home h = null; foreach (var x in Home.All) if (x != null && x.shop && x.name.Replace(" ", "-") == key) h = x;
                if (pc == null || h == null) { Destroy(gameObject); yield break; }
                var r = h.rooms[room]; pc.Teleport(h.transform.TransformPoint(new Vector3(r.centre.x, r.centre.y + 0.1f, r.centre.z)), 0f, 12f);
                var cg = new GameObject("ShotCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 80f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 80f;
                var src = Camera.main; if (src != null) { cam.clearFlags = src.clearFlags; cam.backgroundColor = src.backgroundColor; cam.cullingMask = src.cullingMask; }
                cg.transform.position = h.transform.TransformPoint(new Vector3(r.centre.x, r.centre.y + 1.6f, r.centre.z));
                cg.transform.rotation = h.transform.rotation * Quaternion.Euler(pitch, 180f - yaw, 0f);
                var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; yield return new WaitForSeconds(0.8f); cam.Render();
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
                string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots"); System.IO.File.WriteAllBytes(dir + $"/shotroom_{room}_{(int)yaw}_{(int)pitch}.png", tex.EncodeToPNG());
                Debug.Log($"[ASBO] shotroom {r.type}{r.lv} done"); Destroy(tex); Destroy(rt); Destroy(cg); Destroy(gameObject);
            }
        }
        public static void TourShop(string key, int maxShots) { var g = new GameObject("ShopTour").AddComponent<RunShopTour>(); g.key = key; g.max = maxShots; }
        class RunShopTour : MonoBehaviour
        {
            public string key; public int max;
            IEnumerator Start()
            {
                yield return new WaitForSeconds(1f);
                var pc = FindFirstObjectByType<PlayerCtl>(); Home h = null; foreach (var x in Home.All) if (x != null && x.shop && x.name.Replace(" ", "-") == key) h = x;
                if (pc == null || h == null) { Debug.Log("[ASBO] shoptour: nothing"); Destroy(gameObject); yield break; }
                Debug.Log($"[ASBO] shoptour: {h.name} kind={h.shopKind} rooms={h.rooms.Count} spots={h.spots.Count}");
                foreach (Transform ch in h.transform) { var mf = ch.GetComponent<MeshFilter>(); var mr = ch.GetComponent<MeshRenderer>(); if (mf != null && mf.sharedMesh != null) Debug.Log($"[ASBO] shopmesh {ch.name} active={ch.gameObject.activeSelf} verts={mf.sharedMesh.vertexCount} subs={mf.sharedMesh.subMeshCount} bounds={mf.sharedMesh.bounds.min}..{mf.sharedMesh.bounds.max} layer={ch.gameObject.layer} mats={(mr != null ? mr.sharedMaterials.Length : 0)}"); }
                int shot = 0;
                foreach (var r in h.rooms)
                {
                    if (shot >= max || r.type == "wc") continue;
                    float hw = Mathf.Abs(r.u1 - r.u0) * 0.5f, hd = Mathf.Abs(r.d1 - r.d0) * 0.5f; Vector3 c = r.centre;
                    Vector3 lf = new Vector3(c.x - hw + 0.35f, c.y + 1.65f, c.z + hd - 0.35f); Vector3 look = new Vector3(c.x + hw * 0.4f, c.y + 0.9f, c.z - hd * 0.6f) - lf;
                    pc.Teleport(h.transform.TransformPoint(new Vector3(c.x, c.y + 0.1f, c.z)), 0f, 12f);
                    var cg = new GameObject("TourCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 80f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 80f;
                    var src = Camera.main; if (src != null) { cam.clearFlags = src.clearFlags; cam.backgroundColor = src.backgroundColor; cam.cullingMask = src.cullingMask; }
                    cg.transform.position = h.transform.TransformPoint(lf); cg.transform.rotation = Quaternion.LookRotation(h.transform.TransformDirection(look));
                    { var wp = cg.transform.position; string up = Physics.Raycast(wp, Vector3.up, out var hu, 6f, ~0, QueryTriggerInteraction.Ignore) ? hu.collider.name + "@" + hu.distance.ToString("0.0") : "none"; string dn = Physics.Raycast(wp, Vector3.down, out var hd2, 6f, ~0, QueryTriggerInteraction.Ignore) ? hd2.collider.name + "@" + hd2.distance.ToString("0.0") : "none"; Debug.Log($"[ASBO] shotcam {r.type}{r.lv} pos {wp.x:0.0},{wp.y:0.0},{wp.z:0.0} local {lf.x:0.0},{lf.y:0.0},{lf.z:0.0} up {up} down {dn}"); }
                    var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt;
                    yield return new WaitForSeconds(2.0f); cam.Render();
                    var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
                    string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots"); System.IO.File.WriteAllBytes(dir + $"/shop_{key}_{shot}_{r.type}_{r.lv}.png", tex.EncodeToPNG());
                    Destroy(tex); Destroy(rt); Destroy(cg); shot++;
                }
                Debug.Log("[ASBO] shoptour: done"); Destroy(gameObject);
            }
        }
        public static void Tour(int house) { new GameObject("HomeTour").AddComponent<Run>().house = house; }
        class Run : MonoBehaviour
        {
            public int house;
            IEnumerator Start()
            {
                yield return new WaitForSeconds(1f);
                var pc = FindFirstObjectByType<PlayerCtl>(); var list = new System.Collections.Generic.List<Home>(Home.All); list.RemoveAll(x => x == null);
                list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                if (pc == null || house >= list.Count) { Debug.Log("[ASBO] tour: nothing"); Destroy(gameObject); yield break; }
                var h = list[house]; Debug.Log($"[ASBO] tour: {h.name} kind={h.H.kind} era={h.H.era} rooms={h.rooms.Count} spots={h.spots.Count}");
                int shot = 0;
                foreach (var r in h.rooms)
                {
                    if (shot >= 16) break;
                    float hw = Mathf.Abs(r.u1 - r.u0) * 0.5f - 0.35f, hd = Mathf.Abs(r.d1 - r.d0) * 0.5f - 0.35f;
                    bool alongX = hw >= hd; Vector3 lc = r.centre; Vector3 off = alongX ? new Vector3(hw, 0, 0) : new Vector3(0, 0, hd);
                    if (house % 2 == 1) off = -off;
                    Vector3 lf = lc - off; Vector3 look = off; float lyaw = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg;
                    pc.Teleport(h.transform.TransformPoint(lc) + Vector3.up * 0.1f, 0f, 12f);
                    var cg = new GameObject("TourCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 85f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 80f;
                    var src = Camera.main; if (src != null) { cam.clearFlags = src.clearFlags; cam.backgroundColor = src.backgroundColor; cam.cullingMask = src.cullingMask; }
                    cg.transform.position = h.transform.TransformPoint(lf + Vector3.up * 1.7f); cg.transform.rotation = Quaternion.LookRotation(h.transform.TransformDirection(look) + Vector3.down * 0.25f);
                    var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt;
                    yield return new WaitForSeconds(0.8f); cam.Render();
                    var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
                    string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots"); System.IO.File.WriteAllBytes(dir + $"/home{house}_{shot}_{r.type}_{r.lv}.png", tex.EncodeToPNG());
                    Destroy(tex); Destroy(rt); Destroy(cg); shot++;
                }
                Debug.Log("[ASBO] tour: done"); Destroy(gameObject);
            }
        }
    }
}
