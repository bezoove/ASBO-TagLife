using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ASBO.Slice
{
    /// <summary>
    /// Fists and feet for anyone. The idea comes from the old game (a scrap in the street: fists up, combos, guard,
    /// stamina, knockdowns, people piling in, cheering or legging it); the moves are motion capture.
    ///
    /// Player: tap PUNCH for jab, cross, hook, body shot in turn; hold it for a big overhand. KICK, SHOVE (gets through a
    /// guard), hold GUARD to block what comes from the front, FISTS to keep your fists up. Everything costs stamina; tired
    /// punches are slow and weak. Big hits knock people down (they get up and carry on); out of health is a knockout.
    /// Anyone hit squares up to whoever hit them; the faint-hearted run off once they're badly hurt.
    ///
    /// Each move has several versions (FightSet) picked at random, so no two punches look alike, and the two bodies
    /// close to exactly the distance that version reaches as it lands, so you see it connect. Once you're in a scrap you
    /// are squared up to one person (Target): you face them, a gentle pull keeps you at fighting distance, sideways goes
    /// round them, and pushing at someone else and swinging (or them hitting you) switches to them. Fists down or legging
    /// it gets you out. People fighting hold off at arm's length, circle, step in to throw and take turns on you.
    /// </summary>
    public class Fighter : MonoBehaviour
    {
        public enum Move { Jab, Cross, Hook, Body, Hay, Kick, Shove }
        public enum Mode { Idle, Atk, Stun }

        struct Mv
        {
            public string trig; public float dur, imp, reach, cone, dmg, stam, stun, knock, lunge, head, down; public bool heavy, noGuard;
        }
        // dur = how long the move takes (the animation at its speed), imp = when it lands; reach in metres, cone in degrees each side
        static readonly Mv[] MV =
        {
            new Mv { trig = "Punch", dur = 0.40f, imp = 0.29f, reach = 1.05f, cone = 32f, dmg = 6f,  stam = 4f,  stun = 0.30f, knock = 0.10f, lunge = 0.25f, head = 0.75f },
            new Mv { trig = "Cross", dur = 0.42f, imp = 0.21f, reach = 1.10f, cone = 32f, dmg = 10f, stam = 7f,  stun = 0.42f, knock = 0.22f, lunge = 0.35f, head = 0.60f },
            new Mv { trig = "Hook",  dur = 0.51f, imp = 0.27f, reach = 1.00f, cone = 42f, dmg = 12f, stam = 9f,  stun = 0.55f, knock = 0.30f, lunge = 0.30f, head = 0.85f, down = 0.12f },
            new Mv { trig = "Body",  dur = 0.39f, imp = 0.14f, reach = 1.00f, cone = 32f, dmg = 9f,  stam = 7f,  stun = 0.50f, knock = 0.25f, lunge = 0.20f, head = 0f,    down = 0.08f },
            new Mv { trig = "Hay",   dur = 0.95f, imp = 0.42f, reach = 1.15f, cone = 44f, dmg = 22f, stam = 20f, stun = 1.00f, knock = 0.70f, lunge = 0.60f, head = 0.80f, down = 0.50f, heavy = true },
            new Mv { trig = "Kick",  dur = 0.69f, imp = 0.50f, reach = 1.35f, cone = 26f, dmg = 11f, stam = 10f, stun = 0.55f, knock = 0.60f, lunge = 0.20f, head = 0f,    down = 0.20f },
            new Mv { trig = "Shove", dur = 0.60f, imp = 0.26f, reach = 1.10f, cone = 34f, dmg = 1f,  stam = 6f,  stun = 0.75f, knock = 1.50f, lunge = 0.20f, head = 0f,    down = 0.30f, noGuard = true },
        };
        static readonly Move[] Combo = { Move.Jab, Move.Cross, Move.Hook, Move.Body };

        public float hp = 100f, maxHp = 100f, stam = 100f;
        public float temper = 0.4f, build = 0.5f; // personality: how quick to anger and stay in, and how big
        public Mode mode;
        public Fighter foe; public bool aggr;     // who we're fighting, and whether we started it
        public bool guard;                       // blocking
        public bool stanceOn; public float stanceT; // fists up: toggled, or for a while after any fighting
        bool lowered; // the player put their fists down: they stay down until they throw something or get hit
        public bool Busy => mode != Mode.Idle;
        public bool Fighting => foe != null;
        public bool InFight => foe != null;
        public float Health01 => hp / maxHp;
        public float Stam01 => stam / 100f;
        public Person Body => p;
        public bool Down => p == null || p.state != Person.State.Walking;
        public static System.Action<Fighter, Fighter, float> OnHit; // attacker, victim, damage
        public static readonly List<Fighter> All = new List<Fighter>();

        Person p; Resident res; NavMeshAgent ag;
        float t, stunT, imm, stamT, comboT, think, atkCd, guardT, strafe = 1f, strafeT, over, repath, seqT, bufT, hitT;
        int seqI; Move cur; bool landed, exhausted, flee, committed, ko; Move? buf; Fighter aim;
        int fistLayer = -2; float fistW; Vector3 lastPos;
        float curImp = 0.3f, curDur = 0.45f, curContact = 0.85f; // this swing's version: when it lands, when you can go again, how close the bodies need to be
        public float LastSwing { get; private set; } = -99f;
        float ring = 1.25f, backoffT; // NPC: how far off they stand while circling; how long the player's had their fists down
        /// <summary>The player: who you're squared up to (the fight circle you're in), or null.</summary>
        public Fighter Target;
        /// <summary>The player's stick in the world (length 0..1): swing while pushing at someone else in the scrap and you switch to them.</summary>
        public Vector3 Steer;
        public bool FistsDown => lowered;
        int thinkFrame;
        public string Dbg => $"foe {(foe != null ? foe.name : "-")} ring {ring:F2} think {think:F2} atkCd {atkCd:F2} over {over:F2} flee {flee} en {enabled} thought {Time.frameCount - thinkFrame} frames ago stance {stanceT:F1}";
        float fledT = -99f, tgtT;

        void Awake()
        {
            p = GetComponent<Person>(); res = GetComponent<Resident>(); ag = GetComponent<NavMeshAgent>();
            uint h = (uint)(Mathf.Abs(gameObject.name.GetHashCode()) + transform.position.GetHashCode());
            temper = 0.15f + (h % 71) / 100f; build = ((h / 71) % 100) / 100f; // 0.15..0.85, 0..1
            if (p != null && p.drunk) temper = Mathf.Min(1f, temper + 0.25f);
            if (p != null && !p.male) build *= 0.6f;
            maxHp = hp = p != null && p.isPlayer ? 100f : Mathf.Round(70f + build * 34f);
        }
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); if (Fighting) Leave(null); }

        // ------------------------------------------------------------------------------------------------ every frame
        bool sized;
        void Update()
        {
            if (p == null) return;
            if (!sized) { sized = true; if (p.isPlayer) maxHp = hp = 100f; }
            if (p.isPlayer) Session.Tick();
            float dt = Time.deltaTime;
            imm = Mathf.Max(0f, imm - dt); stamT = Mathf.Max(0f, stamT - dt); hitT -= dt;
            comboT -= dt; seqT -= dt; bufT -= dt; if (bufT <= 0f) buf = null;
            if (stamT <= 0f && mode != Mode.Atk) stam = Mathf.Min(100f, stam + (guard ? 4f : 13f) * dt);
            if (Down) { mode = Mode.Idle; guard = false; SetFists(0f); return; }

            switch (mode)
            {
                case Mode.Atk:
                {
                    var M = MV[(int)cur]; t += dt;
                    float durNow = curDur * (exhausted ? 1.25f : 1f), impNow = curImp * (exhausted ? 1.25f : 1f);
                    if (aim != null && !landed && !aim.Down) Close(aim, curContact, impNow - t, dt);
                    if (!landed && t >= impNow * 0.7f && t - dt < impNow * 0.7f) Whoosh(cur);
                    if (!landed && t >= impNow) { landed = true; Resolve(M); }
                    if (t >= durNow) { mode = Mode.Idle; if (buf.HasValue) { var b = buf.Value; buf = null; Attack(b); } }
                    break;
                }
                case Mode.Stun:
                    stunT -= dt;
                    if (stunT <= 0f)
                    {
                        mode = Mode.Idle;
                        if (buf.HasValue && p.isPlayer) { var b = buf.Value; buf = null; Attack(b); }
                        else if (!p.isPlayer && Fighting && Random.value < 0.35f + temper * 0.4f) { think = Mathf.Min(think, Random.Range(0.03f, 0.15f)); atkCd = Mathf.Min(atkCd, 0.1f); } // straight back at you
                    }
                    break;
            }
            if (p.isPlayer)
            {
                KeepTarget();
                // fists stay up while someone's actually squaring up to you (not for any scrap across the estate)
                bool onMe = Fighting; if (!onMe) foreach (var f in All) if (f != null && f.foe == this && !f.Down) { onMe = true; break; }
                if (onMe && !lowered) stanceT = Mathf.Max(stanceT, 3f); else stanceT = Mathf.Max(0f, stanceT - dt);
                if (mode == Mode.Idle && !Session.Hot && hp < maxHp) hp = Mathf.Min(maxHp, hp + 0.35f * dt);
            }
            else if (Fighting) Think(dt);
            else { stanceT = Mathf.Max(0f, stanceT - dt * 1.5f); stanceOn = false; } // out of it: the fists come down after a few seconds
            bool fists = (stanceOn || stanceT > 0f || Fighting) && mode != Mode.Atk && mode != Mode.Stun;
            SetFists(fists ? 1f : 0f);
            if (p.anim != null && p.anim.enabled) p.anim.SetBool("Guard", guard);
        }

        /// <summary>Fists-up layer weight, eased (the punches themselves are full-body clips, so it drops out for them).</summary>
        void SetFists(float target)
        {
            var an = p != null ? p.anim : null; if (an == null || !an.enabled) return;
            if (fistLayer == -2 || an.runtimeAnimatorController == null) fistLayer = an.GetLayerIndex("Fists");
            if (fistLayer < 0) { fistLayer = -2; return; }
            fistW = Mathf.MoveTowards(fistW, target, Time.deltaTime * (target > fistW ? 6f : 9f));
            an.SetLayerWeight(fistLayer, fistW);
        }

        // ------------------------------------------------------------------------------------------------ the player
        float punchHeld; bool punchFired;
        /// <summary>Called by PlayerCtl each frame with the fight buttons.</summary>
        public static bool TestGuard; // scripted tests hold the guard
        public void PlayerInput(bool punchDown, bool kick, bool shove, bool guardHeld, bool fistsToggle, bool canAct)
        {
            guardHeld |= TestGuard;
            guard = guardHeld && canAct && mode == Mode.Idle && stam > 0f;
            if (guard) stanceT = Mathf.Max(stanceT, 3f);
            if (fistsToggle)
            {
                // the button shows what you'll get: up if they're down, down if they're up
                bool up = stanceOn || stanceT > 0f;
                if (up) { stanceOn = false; stanceT = 0f; lowered = true; Target = null; }
                else { stanceOn = true; lowered = false; }
            }
            if (!canAct) { punchHeld = 0f; punchFired = false; return; }
            if (punchDown)
            {
                punchHeld += Time.deltaTime;
                if (punchHeld >= 0.45f && !punchFired) { punchFired = true; Press(Move.Hay); } // held: the big one
            }
            else
            {
                if (punchHeld > 0f && !punchFired) Press(null); // tapped: next in the combo
                punchHeld = 0f; punchFired = false;
            }
            if (kick && !TryStamp()) Press(Move.Kick);
            if (shove) Press(Move.Shove);
        }
        /// <summary>
        /// KICK with someone on the floor at your feet (and nobody on their feet in front having a go): you stamp on
        /// their head instead. It hurts them badly, bloodies the floor and leaves them needing an ambulance.
        /// </summary>
        bool TryStamp()
        {
            if (mode == Mode.Atk || mode == Mode.Stun || p == null || p.anim == null || !p.anim.enabled) return false;
            if (Best(MV[(int)Move.Kick]) != null) return false;
            Person down = null; float bd = 1.8f;
            foreach (var q in Person.All)
            {
                if (q == null || q == p || (q.state != Person.State.Ragdoll && q.state != Person.State.Settled)) continue;
                Vector3 d = (q.HeadPos - transform.position).WithY(0); float dist = d.magnitude;
                if (dist > bd || Vector3.Angle(transform.forward, d) > 80f) continue;
                bd = dist; down = q;
            }
            if (down == null) return false;
            StartCoroutine(StampOn(down)); return true;
        }

        System.Collections.IEnumerator StampOn(Person down)
        {
            mode = Mode.Atk; t = 0f; cur = Move.Kick; landed = true; aim = null; curDur = 1.35f; curImp = 0.8f; LastSwing = Time.time; stanceT = 6f; guard = false;
            Vector3 d = (down.HeadPos - transform.position).WithY(0);
            if (d.sqrMagnitude > 0.01f) { transform.rotation = Quaternion.LookRotation(d); float step = d.magnitude - 0.42f; if (Mathf.Abs(step) > 0.05f) MoveBy(d.normalized * Mathf.Clamp(step, -0.5f, 1.2f)); }
            bool has = false; foreach (var q in p.anim.parameters) if (q.name == "Stamp") { has = true; break; }
            p.anim.speed = 1f; p.anim.SetTrigger(has ? "Stamp" : "Kick");
            yield return new WaitForSeconds(has ? 0.8f : 0.55f);
            if (down != null) down.Stamped(transform.forward);
        }

        /// <summary>The player's punch button as a single press (touch tests, scripted runs).</summary>
        public void PunchButton() => Press(null);

        void Press(Move? m)
        {
            stanceT = 6f; lowered = false;
            if (m == null)
            {
                int idx = seqT > 0f ? (seqI + 1) % Combo.Length : 0;
                if (Attack(Combo[idx])) { seqI = idx; seqT = 1.1f; comboT = 1.3f; }
                return;
            }
            Attack(m.Value);
        }

        /// <summary>How fast the player can move right now (slower while swinging, guarding or rocked).</summary>
        public float SpeedMul => Down ? 0f : mode == Mode.Stun ? 0.3f : mode == Mode.Atk ? 0.35f : guard ? 0.5f : (Session.Hot || stanceT > 0f) ? 0.88f : 1f;

        /// <summary>Who the player should square up to (nearest person fighting them), or null.</summary>
        public Fighter NearestFoe(float within = 5.5f)
        {
            Fighter best = null; float bd = within;
            foreach (var f in All)
            {
                if (f == this || !f.Fighting || f.Down || (f.foe != this && f.hitT < -6f)) continue;
                float d = (f.transform.position - transform.position).magnitude;
                if (d < bd) { bd = d; best = f; }
            }
            return best;
        }

        // ------------------------------------------------------------------------------------------------ moves
        public bool Attack(Move m)
        {
            if (p == null || p.anim == null || !p.anim.enabled || Down) return false;
            if (mode == Mode.Atk || mode == Mode.Stun) { if (p.isPlayer) { buf = m; bufT = 0.35f; } return false; }
            var M = MV[(int)m];
            // one of this move's versions, at random: its own timing and reach
            var v = FightSet.Pick(M.trig);
            curImp = v != null ? v.imp : M.imp; curDur = v != null ? v.dur : M.dur; curContact = v != null && v.contact > 0.1f ? v.contact : 0.85f;
            exhausted = stam < M.stam * 0.6f;
            mode = Mode.Atk; t = 0f; cur = m; landed = false; guard = false; LastSwing = Time.time;
            stam = Mathf.Max(0f, stam - M.stam); stamT = 0.7f; stanceT = 6f;
            aim = p.isPlayer ? PlayerAim(M) : foe;
            if (aim != null) { Vector3 d = (aim.transform.position - transform.position).WithY(0); if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d); }
            p.anim.speed = exhausted ? 0.8f : 1f;
            if (v != null) SetVar("AVar", v.idx);
            p.anim.SetTrigger(M.trig);
            return true;
        }

        static readonly Dictionary<RuntimeAnimatorController, HashSet<string>> ctrlParams = new Dictionary<RuntimeAnimatorController, HashSet<string>>();
        /// <summary>Which version to play (only on controllers that have them: the drunk and hurt ones may not).</summary>
        void SetVar(string param, int i)
        {
            var an = p.anim; var rc = an != null ? an.runtimeAnimatorController : null; if (rc == null) return;
            if (!ctrlParams.TryGetValue(rc, out var set)) { set = new HashSet<string>(); foreach (var q in an.parameters) set.Add(q.name); ctrlParams[rc] = set; }
            if (set.Contains(param)) an.SetFloat(param, i);
        }

        /// <summary>
        /// Step in (or back) so the two bodies are exactly as far apart as this blow reaches when it lands: the fist meets
        /// the face, the boot the belly, never thin air in front of them or an arm through their head.
        /// </summary>
        void Close(Fighter tgt, float contact, float left, float dt)
        {
            Vector3 d = (tgt.transform.position - transform.position).WithY(0); float dist = d.magnitude;
            if (dist < 0.01f || dist > contact + 1.3f) return;
            float gap = dist - contact;
            float s = gap > 0f ? Mathf.Min(gap * Mathf.Clamp01(dt / Mathf.Max(left, dt)), 4.5f * dt) : Mathf.Max(gap * 0.5f, -1.5f * dt);
            if (Mathf.Abs(s) > 1e-4f) MoveBy(d / dist * s);
        }

        // ------------------------------------------------------------------------------------------------ the player's fight circle
        /// <summary>Who the player's swing goes at: whoever the stick pushes at in the scrap, else the one you're squared up to, else whoever's in front.</summary>
        Fighter PlayerAim(Mv M)
        {
            if (Steer.sqrMagnitude > 0.09f)
            {
                Fighter pick = null; float ba = 40f;
                foreach (var f in All)
                {
                    if (f == this || f == Target || f.Down || f.p == null || f.foe != this) continue;
                    Vector3 d = (f.transform.position - transform.position).WithY(0); if (d.magnitude > curContact + 1.6f) continue;
                    float a = Vector3.Angle(Steer, d); if (a < ba) { ba = a; pick = f; }
                }
                if (pick != null) Engage(pick);
            }
            if (Target != null && !Target.Down && (Target.transform.position - transform.position).WithY(0).magnitude <= curContact + 1.4f) return Target;
            var b = Best(M, 0.5f);
            if (b != null) Engage(b);
            return b;
        }

        /// <summary>Square the player up to this one: face them, circle them, swing at them.</summary>
        public void Engage(Fighter f)
        {
            if (f == null || f == this || f.Down && f.ko) return;
            if (Target != f) tgtT = Time.time;
            Target = f; lowered = false;
        }

        /// <summary>The player legged it: out of the circle (people give up the chase once you're well away).</summary>
        public void RunOff() { if (Target != null) Target = null; fledT = Time.time; }
        public bool RanOff => Time.time - fledT < 6f;

        /// <summary>Each frame for the player: drop someone who's out of it, pick up whoever squares up to you.</summary>
        void KeepTarget()
        {
            if (lowered) { Target = null; return; }
            if (Target != null)
            {
                float d = (Target.transform.position - transform.position).WithY(0).magnitude;
                if (Target.p == null || Target.ko || Target.hp <= 0f || d > 6.5f || (!Target.Fighting && !Target.Down && Time.time - tgtT > 4f)) Target = null;
                else if (Target.Down) { var o = Hostile(Target, 3.5f); if (o != null) Engage(o); } // they're on the floor: the next one
            }
            if (Target == null && Time.time - fledT > 1.5f) { var o = Hostile(null, 4.5f); if (o != null) Engage(o); }
        }

        /// <summary>The nearest person on their feet having a go at the player.</summary>
        Fighter Hostile(Fighter except, float within)
        {
            Fighter best = null; float bd = within;
            foreach (var f in All)
            {
                if (f == this || f == except || f.foe != this || f.Down || f.ko) continue;
                float d = (f.transform.position - transform.position).WithY(0).magnitude; if (d < bd) { bd = d; best = f; }
            }
            return best;
        }

        void Whoosh(Move m)
        {
            if (GameAudio.I == null) return;
            var w = SfxBank.Pick(m == Move.Kick ? "whooshKick" : "whooshPunch");
            if (w != null) GameAudio.I.Play3D(w, transform.position + Vector3.up * 1.2f, m == Move.Hay ? 0.65f : 0.45f, Random.Range(0.95f, 1.08f), 20f, 2f);
        }

        void MoveBy(Vector3 v)
        {
            if (ag == null && !p.isPlayer) ag = GetComponent<NavMeshAgent>();
            var cc = GetComponent<CharacterController>();
            if (cc != null && cc.enabled) cc.Move(v);
            else if (ag != null && ag.enabled && ag.isOnNavMesh) ag.Move(v);
            else transform.position += v;
        }

        Fighter Best(Mv M, float extra = 0f)
        {
            Fighter best = null; float bs = 99f; bool hostile = false;
            if (p.isPlayer) foreach (var f in All) if (f.foe == this && !f.Down) { hostile = true; break; }
            foreach (var f in All)
            {
                if (f == this || f.Down || f.p == null) continue;
                if (!p.isPlayer && f != foe) continue;
                if (hostile && f.foe != this) continue; // while people are on you, swing at them, not at the crowd
                Vector3 d = f.transform.position - transform.position; if (Mathf.Abs(d.y) > 0.7f) continue; d.y = 0f;
                float dist = d.magnitude; if (dist > M.reach + 0.3f + extra) continue;
                float ang = Vector3.Angle(transform.forward, d); if (ang > M.cone + (p.isPlayer ? 26f + extra * 57f : 9f)) continue;
                float sc = dist + ang * Mathf.Deg2Rad * 0.6f - (f.Fighting && p.isPlayer ? 0.4f : 0f);
                if (sc < bs) { bs = sc; best = f; }
            }
            return best;
        }

        void Resolve(Mv M)
        {
            float dmg = M.dmg * Random.Range(0.88f, 1.12f) * DmgMul() * (exhausted ? 0.65f : 1f);
            var tgt = aim != null && !aim.Down && InReach(aim, M) ? aim : Best(M);
            if (tgt != null) tgt.TakeHitFrom(this, M, dmg);
            else if (p.isPlayer && !PropWorld.ThrowHeld()) { float imp = (M.trig == "Kick" ? 26f : M.trig == "Shove" ? 18f : 9f) + M.dmg * 0.4f; if (!PropWorld.Strike(transform.position + Vector3.up * 0.4f, transform.forward, M.reach + 0.3f, imp)) Knockable.Strike(transform.position + Vector3.up * 0.4f, transform.forward, M.reach + 0.3f, imp); } // a glass, a bin or a gnome in the way gets it
            if (p.anim != null) p.anim.speed = 1f;
        }

        bool InReach(Fighter f, Mv M)
        {
            Vector3 d = f.transform.position - transform.position; if (Mathf.Abs(d.y) > 0.7f) return false; d.y = 0f;
            return d.magnitude <= M.reach + 0.3f && Vector3.Angle(transform.forward, d) <= M.cone + 28f;
        }

        float DmgMul() => p.isPlayer ? 1f + (build - 0.5f) * 0.3f : 0.75f + build * 0.5f;

        // ------------------------------------------------------------------------------------------------ taking hits
        /// <summary>Hit by something that isn't a fist (a thrown thing, a fall): same reactions, no move behind it.</summary>
        public void TakeHit(Fighter from, Move m, float dmg, Vector3 push) => TakeHitFrom(from, MV[(int)m], dmg);

        void TakeHitFrom(Fighter from, Mv M, float dmg)
        {
            if (Down || hp <= 0f) return;
            if (p.isPlayer && p.GetComponent<PlayerCtl>() is PlayerCtl pc && pc.InCar) return;
            if (!p.isPlayer && !Fighting && from != null) SquareUp(from, false);
            if (p.isPlayer) { stanceT = 6f; lowered = false; }
            if (p.isPlayer && from != null && from != Target && !from.Down && (Target == null || Target.Down || Time.time - Target.LastSwing > 1.6f)) Engage(from);
            hitT = 0f;
            Vector3 fromPos = from != null ? from.transform.position : transform.position - transform.forward;
            Vector3 toMe = (transform.position - fromPos).WithY(0); if (toMe.sqrMagnitude < 1e-4f) toMe = -transform.forward; toMe.Normalize();
            bool blocked = guard && !M.noGuard && mode == Mode.Idle && stam > 0f && Vector3.Angle(transform.forward, -toMe) < 72f;
            bool head = M.head > 0f && Random.value < M.head;
            if (!blocked) dmg *= head ? 1.25f : 0.9f;
            if (mode == Mode.Stun && !blocked) dmg *= 1.2f;
            if (from != null) from.comboT = 1.3f;
            Vector3 at = transform.position + Vector3.up * (head ? 1.6f : 1.1f) - toMe * 0.15f;
            if (blocked)
            {
                dmg *= M.heavy ? 0.45f : 0.15f;
                stam -= 6f + dmg * 0.8f + (M.heavy ? 10f : 0f);
                Sound("block", at, 0.7f);
                if (stam <= 0f) { stam = 0f; guard = false; Stun(0.9f); if (p.isPlayer) World.Toast("Your guard is broken.", 1); }
            }
            else
            {
                Sound(M.heavy || dmg > 15f ? "hitKO" : head ? "hitFace" : "hitBody", at, M.heavy ? 1f : 0.8f);
                stam = Mathf.Max(0f, stam - (head ? 3f : dmg * 0.9f));
                if (GameAudio.I != null && (M.heavy || dmg > 12f || Random.value < 0.35f)) GameAudio.I.Pain(at, p.male, 0.06f);
                if (Gore.Level > 0 && (M.heavy || Random.value < 0.2f)) Gore.Burst(at, -toMe + Vector3.up * 0.5f, M.heavy ? 18 : 6);
            }
            // winding up your big punch: you ride the blow (less damage, no flinch) and the swing carries on through
            bool armoured = p.isPlayer && !blocked && mode == Mode.Atk && MV[(int)cur].heavy && t < curImp + 0.05f;
            if (armoured) dmg *= 0.55f;
            hp = Mathf.Max(0f, hp - dmg);
            OnHit?.Invoke(from, this, dmg);
            if (armoured && hp > 0f) { if (p.isPlayer) Session.Feel(0.05f, 0.04f); return; }
            if (p.isPlayer && !blocked && head) { if (M.heavy || dmg > 14f) Social.Mark("eye", 420f); else if (Random.value < 0.3f) Social.Mark(Random.value < 0.4f ? "lip" : Random.value < 0.5f ? "nose" : "brow", 240f); } // people notice
            MoveBy(toMe * M.knock * (blocked ? 0.3f : 1f) * 0.5f);
            if (p.isPlayer || (from != null && from.p != null && from.p.isPlayer)) Session.Feel(blocked ? 0f : M.heavy ? 0.09f : 0.05f, blocked ? 0.015f : 0.03f + Mathf.Clamp01(dmg / 12f) * 0.05f);

            if (hp <= 0f) { KnockOut(from, toMe); return; }
            if (!blocked && (dmg >= 15f || (M.down > 0f && Random.value < M.down * (stam < 25f ? 1.7f : 1f))))
            {
                // knocked down: on the floor a couple of seconds, then up and at it again
                mode = Mode.Idle; guard = false;
                p.Knockout(toMe * (2.2f + M.knock * 2f) + Vector3.up * 0.6f, M.heavy ? Random.Range(2.6f, 3.6f) : Random.Range(1.8f, 2.6f), false);
                return;
            }
            Stun(blocked ? 0.12f : M.stun * (imm > 0f ? 0.4f : 1f)); imm = 1.4f;
            if (!blocked && p.anim != null && p.anim.enabled)
            {
                string rt = head || M.head > 0f ? "Struck" : "StruckBody";
                var rv = FightSet.Pick(rt, M.heavy || dmg > 13f); if (rv != null) SetVar("RVar", rv.idx);
                p.anim.SetTrigger(rt);
            }
        }

        void Stun(float secs) { mode = Mode.Stun; stunT = Mathf.Max(secs, 0.05f); guard = false; buf = null; if (p.anim != null) p.anim.speed = 1f; }

        void Sound(string key, Vector3 at, float vol)
        {
            if (GameAudio.I == null) return;
            var c = SfxBank.Pick(key); if (c == null && key == "block") c = SfxBank.Pick("hitBody");
            if (c != null) GameAudio.I.Play3D(c, at, vol, Random.Range(0.93f, 1.07f), 35f, 3f);
        }

        void KnockOut(Fighter by, Vector3 dir)
        {
            mode = Mode.Idle; guard = false; ko = true;
            if (p.isPlayer) { Session.PlayerKO(this, by); p.Knockout(dir * 2.6f + Vector3.up * 0.7f, 3.4f, true); return; }
            Session.KOd(this, by);
            p.Knockout(dir * 3f + Vector3.up * 0.8f, Random.Range(9f, 14f), true);
            World.Toast(Session.NameOf(this) + " is out cold.", by != null && by.p != null && by.p.isPlayer ? 2 : 0);
            // the ones who started on you might think better of it
            foreach (var f in All) if (f != this && f.foe != null && f.foe.p != null && f.foe.p.isPlayer && f.aggr && Random.value < 0.3f) f.flee = true;
        }

        /// <summary>Hurt by something that isn't a fight (a fall, bailing out of a car): takes health, can knock you out.</summary>
        public void Hurt(float dmg, bool bad)
        {
            if (hp <= 0f) return;
            hp = Mathf.Max(bad ? 0f : 1f, hp - dmg);
            if (p != null && p.isPlayer && hp <= 0f) { ko = true; Session.PlayerKO(this, null); }
        }

        /// <summary>Called after getting up off the floor.</summary>
        public void Recover(bool afterKO)
        {
            mode = Mode.Idle; buf = null; if (p.anim != null) p.anim.speed = 1f;
            if (afterKO || ko) { hp = Mathf.Max(hp, maxHp * (p.isPlayer ? 0.22f : 0.3f)); stam = Mathf.Max(stam, 40f); ko = false; if (!p.isPlayer) Leave(null); }
        }

        // ------------------------------------------------------------------------------------------------ starting and stopping
        /// <summary>Square up to someone. byMe: we started it.</summary>
        public void SquareUp(Fighter other, bool byMe)
        {
            if (other == null || other == this || p == null || p.isPlayer || Down) return;
            if (Fighting) { if (other.p != null && other.p.isPlayer && !byMe) foe = other; return; }
            foe = other; aggr = byMe; flee = false; committed = false; over = 0f; backoffT = 0f;
            ring = 1.12f + build * 0.12f + Random.Range(0f, 0.12f); // arm's length and a bit: where they hold while circling
            think = Random.Range(0.15f, 0.45f); atkCd = Random.Range(0.3f, 0.8f); stanceT = 6f;
            hp = Mathf.Max(hp, maxHp * 0.35f);
            p.SetKneel(false); p.SetTalk(false); p.SetWave(false);
            if (res == null) res = GetComponent<Resident>();
            if (ag == null) ag = GetComponent<NavMeshAgent>();
            if (res != null) res.EnterFight();
            if (ag != null) ag.updateRotation = false;
            lastPos = transform.position;
            if (GameAudio.I != null) GameAudio.I.PlayVoice("hostile", transform.position + Vector3.up * 1.6f, 0.15f);
            Session.Started(this, other);
        }

        /// <summary>Done fighting: "flee" runs off, "won"/null goes back to what they were doing.</summary>
        public void Leave(string why)
        {
            if (foe == null && why == null) { guard = false; return; }
            foe = null; guard = false; flee = false; mode = Mode.Idle;
            if (ag != null) { ag.updateRotation = true; ag.acceleration = 3f; } // back to a resident's easy pace
            if (p != null && p.anim != null) p.anim.speed = 1f;
            if (res != null) res.ExitFight(why == "flee" ? Session.PlayerFighter : null);
            if (why == "flee") World.Toast(Session.NameOf(this) + " legs it.", 0);
        }

        // ------------------------------------------------------------------------------------------------ an NPC's head in a scrap
        float Skill => Mathf.Clamp(0.3f + temper * 0.35f + (build - 0.5f) * 0.25f, 0.15f, 1f);

        void Think(float dt)
        {
            thinkFrame = Time.frameCount;
            if (ag == null) ag = GetComponent<NavMeshAgent>(); // residents get their agent after we wake up
            var me = transform.position;
            if (foe == null || foe.Down || foe.hp <= 0f || (foe.p != null && foe.p.isPlayer && Session.PlayerOut))
            {
                over += dt; if (mode == Mode.Idle && over > 0.8f) Leave(flee ? "flee" : "won");
                Animate(Vector3.zero, dt);
                return;
            }
            over = 0f;
            Vector3 d = (foe.transform.position - me).WithY(0); float dist = d.magnitude; if (dist < 0.01f) d = transform.forward;
            if (dist > 25f) { Leave(null); return; }
            bool onPlayer = foe.p != null && foe.p.isPlayer;
            // the player legged it: chase a bit, then give up
            if (onPlayer && foe.RanOff && dist > 8f && mode == Mode.Idle) { World.Toast(Session.NameOf(this) + " doesn't bother chasing you.", 0); Leave(null); return; }
            // the player put their fists down: most leave it there ("Yeah, walk away"), the real nutters keep coming
            if (onPlayer && foe.lowered && mode == Mode.Idle)
            {
                backoffT += dt;
                if (backoffT > 0.8f && (!aggr || temper < 0.62f || hp < maxHp * 0.6f))
                {
                    World.Toast(Session.NameOf(this) + ": \"" + (aggr ? "Yeah, walk away." : "That's what I thought.") + "\"", 0);
                    if (GameAudio.I != null) GameAudio.I.PlayVoice("hostile", transform.position + Vector3.up * 1.6f, 0.1f);
                    Leave("won"); return;
                }
            }
            else backoffT = 0f;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), (mode == Mode.Atk ? 290f : 630f) * dt);
            if (mode != Mode.Idle) { Animate(Vector3.zero, dt); return; }

            // badly hurt: the faint-hearted leg it, the rest dig in
            if (!flee && hp < maxHp * 0.28f && !committed) { if (temper < 0.55f) flee = true; else committed = true; }
            if (flee) { Leave("flee"); return; }

            guardT -= dt; guard = guardT > 0f && stam > 0f; atkCd -= dt; think -= dt; strafeT -= dt;
            if (think <= 0f)
            {
                think = Random.Range(0.2f, 0.45f) * (1.3f - Skill * 0.6f);
                bool winding = foe.mode == Mode.Atk && foe.t < MV[(int)foe.cur].imp;
                if (winding && dist < 1.9f && stam > 8f && Random.value < 0.1f + Skill * 0.55f) { guardT = Random.Range(0.5f, 0.9f); guard = true; }
                else if (dist <= ring + 0.3f && atkCd <= 0f && !foe.Down && Crowded()) atkCd = Random.Range(0.15f, 0.45f); // someone else is swinging at them: wait your turn
                else if (dist <= ring + 0.3f && atkCd <= 0f && !foe.Down)
                {
                    var opts = new List<(Move, float)> { (Move.Jab, 5f), (Move.Cross, 4f), (Move.Hook, 3f), (Move.Body, 2f) };
                    if (Skill > 0.5f) opts.Add((Move.Kick, 1f));
                    if (temper > 0.5f && stam > 45f) opts.Add((Move.Hay, 1.4f));
                    if (foe.guard) opts.Add((Move.Shove, 3f));
                    if (stam < 12f) atkCd = Random.Range(0.4f, 0.9f);
                    else { Attack(Pick(opts)); atkCd = Random.Range(0.35f, 0.9f) * (1.35f - Skill * 0.55f) + (stam < 30f ? 0.5f : 0f); }
                }
                else if (dist < ring + 0.45f && Random.value < 0.6f) { strafe = Random.value < 0.5f ? 1f : -1f; strafeT = Random.Range(0.6f, 1.6f); } // move round them
            }
            if (mode != Mode.Idle) { Animate(Vector3.zero, dt); return; }

            Vector3 dir = d / Mathf.Max(dist, 0.01f), side = new Vector3(-dir.z, 0f, dir.x), v = Vector3.zero;
            if (dist > ring + 0.25f)
            {
                float sp = dist > 4.5f ? (onPlayer && foe.RanOff ? 3.8f : 3f) : 1.7f + Skill * 0.3f;
                if (ag != null && ag.enabled && ag.isOnNavMesh && dist > 2.2f) // path round parked cars and walls to get at them
                {
                    ag.isStopped = false; ag.speed = sp; ag.acceleration = 10f;
                    if (Time.time > repath) { repath = Time.time + 0.25f; ag.SetDestination(foe.transform.position - dir * ring); }
                    Animate(ag.velocity, dt, true); return;
                }
                v = dir * sp * Mathf.Clamp01((dist - ring) / 0.6f + 0.35f);
            }
            else
            {
                // in the circle: hold at arm's length and work round them
                v = dir * Mathf.Clamp((dist - ring) * 2.2f, -1.1f, 0.8f);
                if (strafeT > 0f && !guard) v += side * strafe * 0.85f;
            }
            if (dist < 0.6f) v -= dir * (0.6f - dist) * 6f; // never inside each other
            if (ag != null && ag.enabled && ag.isOnNavMesh) { ag.isStopped = true; ag.velocity = Vector3.zero; }
            v += Separation() * 1.6f;
            if (v.sqrMagnitude > 1e-4f)
            {
                if (ag == null || !ag.enabled) { p.Step(transform.position + v, v.magnitude); Animate(Vector3.zero, dt, true); return; }
                MoveBy(v * dt);
            }
            Animate(v, dt);
        }

        /// <summary>Someone else is mid-swing at the one we're fighting (so they take turns, not all pile in at once).</summary>
        bool Crowded() { foreach (var f in All) if (f != this && f.foe == foe && f.mode == Mode.Atk) return true; return false; }

        /// <summary>Keep a little room from everyone but the one we're fighting.</summary>
        Vector3 Separation()
        {
            Vector3 s = Vector3.zero, me = transform.position;
            foreach (var o in Person.All)
            {
                if (o == null || o == p || (foe != null && o == foe.p) || o.state != Person.State.Walking) continue;
                Vector3 e = (me - o.transform.position).WithY(0); float e2 = e.sqrMagnitude;
                if (e2 < 0.5f && e2 > 1e-5f) { float m = Mathf.Sqrt(e2); s += e / m * (0.7f - m); }
            }
            return s;
        }

        void Animate(Vector3 worldVel, float dt, bool agentDriven = false)
        {
            if (agentDriven) { if (worldVel.sqrMagnitude > 0.01f) p.Animate(worldVel.magnitude); return; }
            Vector3 local = Quaternion.Inverse(transform.rotation) * worldVel;
            p.SetMove(new Vector2(local.x, local.z));
        }

        static Move Pick(List<(Move m, float w)> o)
        {
            float tot = 0f; foreach (var x in o) tot += x.w;
            float r = Random.value * tot; foreach (var x in o) { r -= x.w; if (r <= 0f) return x.m; }
            return o[0].m;
        }

        // ================================================================================================ the scrap as a whole
        /// <summary>
        /// One scrap at a time around the player: who's in it, onlookers (watch, cheer, wince, shout), people piling in
        /// for a mate, the end ("word will get around"), and the player being knocked out. Hit-stop and camera shake too.
        /// </summary>
        public static class Session
        {
            public static bool Active; public static bool Hot; public static bool PlayerOut;
            public static bool PlayerStarted; static readonly List<Fighter> koed = new List<Fighter>();
            static float endT, barkT, hitStop; public static float Shake;
            static Fighter pf;
            public static Fighter PlayerFighter { get { if (pf == null) foreach (var f in All) if (f.p != null && f.p.isPlayer) { pf = f; break; } return pf; } }
            static readonly string[] Barks = { "Go on, son!", "Oi oi!", "Leave it, lads!", "Ooooh!", "Someone stop them!", "He felt that!", "Get in there!", "Leave it out!", "Here we go...", "Not on my doorstep!" };

            public static string NameOf(Fighter f) => f == null || f.p == null ? "Someone" : Persona.Name(f.p);

            public static void Started(Fighter n, Fighter other)
            {
                var P = PlayerFighter;
                if (!Active) { Active = true; koed.Clear(); PlayerStarted = false; barkT = Time.time; }
                if (P != null && other == P && !n.aggr) { PlayerStarted = true; Offences.Report("Fighting", 18f, n.transform.position); }
                if (P != null && other == P) P.stanceT = 6f;
                Spread(n, other);
            }

            /// <summary>Everyone near the start of a scrap reacts: mates pile in, the drunk and lairy cheer, others wince and watch.</summary>
            static void Spread(Fighter n, Fighter other)
            {
                var P = PlayerFighter; Vector3 at = n.transform.position;
                foreach (var o in All)
                {
                    if (o == n || o == other || o.p == null || o.p.isPlayer || o.Fighting || o.Down) continue;
                    float d = (o.transform.position - at).magnitude; if (d > 11f) continue;
                    var rel = Rel.Of(o.p);
                    if (P != null && other == P && n.aggr && rel.friend > 35f && o.temper > 0.3f && d < 7f && Random.value < 0.4f) { JoinIn(o, n, true); continue; }
                    if (P != null && other == P && !n.aggr && rel.friend < -5f && o.temper > 0.35f && o.p.drunk && d < 7f && Random.value < 0.3f + rel.grudge / 200f) { JoinIn(o, P, false); continue; }
                    if (o.res != null) o.res.Watch(n.transform, 14f, o.p.drunk && o.temper > 0.3f && Random.value < 0.6f);
                }
            }

            static void JoinIn(Fighter o, Fighter against, bool onYourSide)
            {
                o.SquareUp(against, true);
                World.Toast(NameOf(o) + (onYourSide ? " wades in on your side." : " wades in."), 1);
            }

            public static void KOd(Fighter f, Fighter by) { if (by != null && by.p != null && by.p.isPlayer && !koed.Contains(f)) koed.Add(f); }

            public static void PlayerKO(Fighter me, Fighter by)
            {
                PlayerOut = true;
                foreach (var f in All) if (f.foe == me) f.foe = null;
                World.Toast("Everything goes black.", 1);
                World.FadeTo(1f, 0.5f, 2.4f);
                me.StartCoroutine(ComeTo());
            }
            static IEnumerator ComeTo()
            {
                yield return new WaitForSeconds(3.2f);
                PlayerOut = false;
                World.Toast("You come to. They've wandered off, and your head is ringing.", 1);
            }

            /// <summary>Hit-stop (a split second of slow motion on a landed blow) and camera shake, for hits you're part of.</summary>
            public static void Feel(float stop, float shake) { hitStop = Mathf.Max(hitStop, stop); Shake = Mathf.Max(Shake, shake); }

            public static void Tick()
            {
                float udt = Time.unscaledDeltaTime;
                if (hitStop > 0f) { hitStop -= Mathf.Min(udt, 0.05f); Time.timeScale = hitStop > 0f ? 0.12f : 1f; }
                Shake = Mathf.Max(0f, Shake - udt * 0.25f);
                // still on: anyone with a foe, including someone knocked down who'll get up and carry on (not the knocked out)
                bool any = false; foreach (var f in All) if (f.foe != null && !f.ko) { any = true; break; }
                if (!Active) { if (!any) { Hot = false; return; } Active = true; koed.Clear(); }
                Hot = any;
                if (!any) { endT += udt; if (endT > 1.2f) End(); return; }
                endT = 0f;
                var P = PlayerFighter;
                if (P != null && Time.time - barkT > 3.2f)
                {
                    barkT = Time.time;
                    if (Random.value < 0.45f)
                    {
                        Fighter pick = null; int n = 0;
                        foreach (var o in All) if (o.p != null && !o.p.isPlayer && !o.Fighting && !o.Down && !o.p.isParamedic && (o.transform.position - P.transform.position).magnitude < 11f && Random.Range(0, ++n) == 0) pick = o;
                        if (pick != null)
                        {
                            World.Toast(NameOf(pick) + ": \"" + Barks[Random.Range(0, Barks.Length)] + "\"", 0);
                            if (GameAudio.I != null) GameAudio.I.PlayVoice("resident", pick.transform.position + Vector3.up * 1.6f);
                        }
                    }
                }
            }

            static void End()
            {
                Active = false; Hot = false; endT = 0f;
                foreach (var k in koed) if (k != null && k.p != null)
                {
                    var r = Rel.Of(k.p); r.friend -= 15f; r.grudge = Mathf.Max(r.grudge, 60f); r.met = true; Rel.Set(k.p, r);
                    // and the estate talks about it
                    Lines.Rumour(PlayerStarted ? "Heard you put " + NameOf(k) + " on the floor. They're telling everyone you jumped them." : "Heard you sorted " + NameOf(k) + " out. About time someone did.");
                }
                if (koed.Count > 0)
                {
                    Rel.Rep = Mathf.Clamp(Rel.Rep + (PlayerStarted ? 1 : 3) * Mathf.Min(3, koed.Count), 0, 100);
                    World.Toast(PlayerStarted ? "You left them on the floor. Word will get around." : "You came out on top. People saw it.", PlayerStarted ? 1 : 2);
                }
                koed.Clear();
            }
        }
    }
}
