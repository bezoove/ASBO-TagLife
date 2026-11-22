using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// One junction's traffic lights, run the British way: red, red-and-amber, green, amber, red, with an all-red gap
    /// between the roads and a pedestrian stage (green man, then flashing) whenever someone is waiting to cross. A
    /// pelican crossing rests on green for the traffic until someone wants to cross, then flashing amber hands it back.
    /// The zebra's Belisha beacons just flash. All the lamps are one mesh lit through a property block (one draw call).
    /// Traffic asks <see cref="Go"/> / <see cref="MustStop"/>; walkers ask <see cref="WalkNow"/>.
    /// </summary>
    public class JunctionSignal : MonoBehaviour
    {
        public TownPlan.JKind kind; public Vector3 centre; public bool zebra;
        public static readonly List<JunctionSignal> All = new List<JunctionSignal>();

        enum St { G0, A0, R0, RA1, G1, A1, R1, Walk, WalkFlash, RAll, RA0 }
        St st = St.G0; float t, stageLen = 12f; bool pedWanted, pedThisCycle; float pedAskT;
        const int RED = 1, AMBER = 2, GREEN = 4, RMAN = 8, GMAN = 16;
        int b0 = GREEN, b1 = RED, bp = RMAN; bool flashOn;
        MaterialPropertyBlock mpb; Renderer rend; static readonly int PhaseId = Shader.PropertyToID("_Phase");

        void OnEnable() { All.Add(this); rend = GetComponent<Renderer>(); mpb = new MaterialPropertyBlock(); t = Mathf.Abs(centre.x * 0.37f + centre.z * 0.11f) % 9f; } // junctions out of step with each other
        void OnDisable() { All.Remove(this); }

        public static JunctionSignal At(Vector3 p, float within = 3f)
        {
            foreach (var j in All) if ((j.centre - p).WithY(0).sqrMagnitude < within * within) return j;
            return null;
        }

        /// <summary>Group 0 = the main road / east-west, 1 = the side road / north-south.</summary>
        public bool Go(int group) => (group == 0 ? b0 : b1) == GREEN;
        /// <summary>Should a car this far from its stop line, at this speed, stop? (Amber: stop if you comfortably can.)</summary>
        public bool MustStop(int group, float toLine, float speed, bool chancer)
        {
            int b = group == 0 ? b0 : b1;
            if (b == GREEN) return false;
            if ((b & AMBER) != 0 && (b & RED) == 0) // plain amber (or a pelican's flashing amber)
            {
                if (kind == TownPlan.JKind.Pelican && st == St.WalkFlash) return PedOnCrossing(); // flashing amber: go if the crossing's clear
                float stopDist = speed * speed / (2f * 3.5f);
                return !(chancer || toLine < stopDist + 1.5f); // too close to stop: carry on through
            }
            if (chancer && b == RED && t < 1.2f && (st == St.R0 || st == St.R1)) return false; // just gone red: the odd one jumps it
            return true;
        }
        /// <summary>Is the green man showing?</summary>
        public bool WalkNow => bp == GMAN && st == St.Walk;
        /// <summary>A walker (or the player) pressed the button.</summary>
        public void AskToCross() { if (!pedWanted) pedAskT = Time.time; pedWanted = true; }

        bool PedOnCrossing()
        {
            foreach (var p in Person.All) { if (p == null || p.state != Person.State.Walking) continue; var q = p.BodyPos - centre; if (Mathf.Abs(q.x) < 2.2f && Mathf.Abs(q.z) < TownPlan.Half + 0.5f) return true; }
            return false;
        }

        void Update()
        {
            float dt = Time.deltaTime; t += dt;
            flashOn = (Time.time * 2f) % 1f < 0.55f;
            if (zebra) { Apply(0, 0, 0, flashOn ? AMBER : 0); return; }
            // anyone standing at a crossing's kerb wants to cross
            if (!pedWanted && Time.frameCount % 20 == 0)
                foreach (var p in Person.All)
                {
                    if (p == null || p.state != Person.State.Walking || p.isPlayer && Time.time < 2f) continue;
                    Vector3 q = p.transform.position - centre; float ax = Mathf.Abs(q.x), az = Mathf.Abs(q.z);
                    bool atKerb = kind == TownPlan.JKind.Pelican ? ax < 3.5f && az > TownPlan.Half && az < TownPlan.Half + 3.5f
                        : (ax < TownPlan.CrossTo + 1f && ax > TownPlan.CrossFrom - 1f && az > TownPlan.Half && az < TownPlan.Half + 3.5f) || (az < TownPlan.CrossTo + 1f && az > TownPlan.CrossFrom - 1f && ax > TownPlan.Half && ax < TownPlan.Half + 3.5f);
                    if (atKerb) { AskToCross(); break; }
                }
            if (kind == TownPlan.JKind.Pelican) Pelican(); else Cycle();
            int fb0 = b0, fb1 = b1, fbp = bp;
            if (st == St.WalkFlash) { if (kind == TownPlan.JKind.Pelican) fb0 = flashOn ? AMBER : 0; fbp = flashOn ? GMAN : 0; }
            Apply(fb0, fb1, fbp, 0);
        }

        void Next(St s, float len) { st = s; t = 0f; stageLen = len; }

        void Cycle()
        {
            bool tee = kind == TownPlan.JKind.Tee;
            if (t < stageLen) return;
            switch (st)
            {
                case St.G0: b0 = AMBER; Next(St.A0, 3f); break;
                case St.A0: b0 = RED; Next(St.R0, 1.5f); break;
                case St.R0: b1 = RED | AMBER; Next(St.RA1, 1.6f); break;
                case St.RA1: b1 = GREEN; Next(St.G1, tee ? 9f : 12f); break;
                case St.G1: b1 = AMBER; Next(St.A1, 3f); break;
                case St.A1: b1 = RED; pedThisCycle = pedWanted; Next(St.R1, 1.5f); break;
                case St.R1: if (pedThisCycle) { bp = GMAN; pedWanted = false; Next(St.Walk, 7f); } else { b0 = RED | AMBER; Next(St.RA0, 1.6f); } break;
                case St.Walk: Next(St.WalkFlash, 4f); break;
                case St.WalkFlash: bp = RMAN; Next(St.RAll, 1.5f); break;
                case St.RAll: b0 = RED | AMBER; Next(St.RA0, 1.6f); break;
                case St.RA0: b0 = GREEN; Next(St.G0, tee ? 18f : 14f); break;
            }
        }

        void Pelican()
        {
            switch (st)
            {
                case St.G0: if (pedWanted && t > 10f && Time.time - pedAskT > 2.5f) { b0 = AMBER; Next(St.A0, 3f); } break; // rests on green until asked
                case St.A0: if (t > stageLen) { b0 = RED; Next(St.R0, 1.5f); } break;
                case St.R0: if (t > stageLen) { bp = GMAN; pedWanted = false; Next(St.Walk, 6f); } break;
                case St.Walk: if (t > stageLen) { Next(St.WalkFlash, 5f); } break;
                case St.WalkFlash: if (t > stageLen) { bp = RMAN; b0 = GREEN; Next(St.G0, 0f); } break;
                default: Next(St.G0, 0f); b0 = GREEN; bp = RMAN; break;
            }
            b1 = b0;
        }

        void Apply(int g0, int g1, int ped, int beacon)
        {
            if (rend == null) return;
            mpb.SetVector(PhaseId, new Vector4(g0, g1, ped, beacon));
            rend.SetPropertyBlock(mpb);
        }
    }
}
