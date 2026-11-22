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
            new Mv { trig = "Hay",   dur = 0.65f, imp = 0.42f, reach = 1.15f, cone = 44f, dmg = 22f, stam = 20f, stun = 1.00f, knock = 0.70f, lunge = 0.60f, head = 0.80f, down = 0.50f, heavy = true },
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
                    float durNow = M.dur * (exhausted ? 1.25f : 1f), impNow = M.imp * (exhausted ? 1.25f : 1f);
                    if (aim != null && t < impNow) Lunge(aim, M.lunge * dt / impNow, M.reach);
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
                if (Session.Hot) stanceT = Mathf.Max(stanceT, 3f); else stanceT = Mathf.Max(0f, stanceT - dt);
                if (mode == Mode.Idle && !Session.Hot && hp < maxHp) hp = Mathf.Min(maxHp, hp + 0.35f * dt);
            }
            else if (Fighting) Think(dt);
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
            if (fistsToggle) { stanceOn = !stanceOn; if (stanceOn) stanceT = 8f; }
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
            if (kick) Press(Move.Kick);
            if (shove) Press(Move.Shove);
        }
        /// <summary>The player's punch button as a single press (touch tests, scripted runs).</summary>
        public void PunchButton() => Press(null);

        void Press(Move? m)
        {
            stanceT = 6f;
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
            exhausted = stam < M.stam * 0.6f;
            mode = Mode.Atk; t = 0f; cur = m; landed = false; guard = false;
            stam = Mathf.Max(0f, stam - M.stam); stamT = 0.7f; stanceT = 6f;
            aim = p.isPlayer ? Best(M, 0.5f) : foe;
            if (aim != null) { Vector3 d = (aim.transform.position - transform.position).WithY(0); if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d); }
            p.anim.speed = exhausted ? 0.8f : 1f;
            p.anim.SetTrigger(M.trig);
            return true;
        }

        void Whoosh(Move m)
        {
            if (GameAudio.I == null) return;
            var w = SfxBank.Pick(m == Move.Kick ? "whooshKick" : "whooshPunch");
            if (w != null) GameAudio.I.Play3D(w, transform.position + Vector3.up * 1.2f, m == Move.Hay ? 0.65f : 0.45f, Random.Range(0.95f, 1.08f), 20f, 2f);
        }

        void Lunge(Fighter tgt, float step, float reach)
        {
            Vector3 d = (tgt.transform.position - transform.position).WithY(0); float dist = d.magnitude;
            if (dist < 0.75f || dist > reach + 0.9f) return;
            float s = Mathf.Min(step, dist - 0.7f); if (s <= 0f) return;
            MoveBy(d / dist * s);
        }

        void MoveBy(Vector3 v)
        {
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
            else if (p.isPlayer) Knockable.Strike(transform.position + Vector3.up * 0.4f, transform.forward, M.reach + 0.3f, (M.trig == "Kick" ? 26f : M.trig == "Shove" ? 18f : 9f) + M.dmg * 0.4f); // a bin or a gnome in the way gets it
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
            if (p.isPlayer) stanceT = 6f;
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
            hp = Mathf.Max(0f, hp - dmg);
            OnHit?.Invoke(from, this, dmg);
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
            if (!blocked && p.anim != null && p.anim.enabled) p.anim.SetTrigger(head || M.head > 0f ? "Struck" : "StruckBody");
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
            foe = other; aggr = byMe; flee = false; committed = false; over = 0f;
            think = Random.Range(0.15f, 0.45f); atkCd = Random.Range(0.3f, 0.8f); stanceT = 6f;
            hp = Mathf.Max(hp, maxHp * 0.35f);
            p.SetKneel(false); p.SetTalk(false); p.SetWave(false);
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
            if (ag != null) ag.updateRotation = true;
            if (p != null && p.anim != null) p.anim.speed = 1f;
            if (res != null) res.ExitFight(why == "flee" ? Session.PlayerFighter : null);
            if (why == "flee") World.Toast(Session.NameOf(this) + " legs it.", 0);
        }

        // ------------------------------------------------------------------------------------------------ an NPC's head in a scrap
        float Skill => Mathf.Clamp(0.3f + temper * 0.35f + (build - 0.5f) * 0.25f, 0.15f, 1f);

        void Think(float dt)
        {
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
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), (mode == Mode.Atk ? 290f : 630f) * dt);
            if (mode != Mode.Idle) { Animate(Vector3.zero, dt); return; }

            // badly hurt: the faint-hearted leg it, the rest dig in
            if (!flee && hp < maxHp * 0.28f && !committed) { if (temper < 0.55f) flee = true; else committed = true; }
            if (flee) { Leave("flee"); return; }

            guardT -= dt; guard = guardT > 0f && stam > 0f; atkCd -= dt; think -= dt; strafeT -= dt;
            const float reach = 1f;
            if (think <= 0f)
            {
                think = Random.Range(0.2f, 0.45f) * (1.3f - Skill * 0.6f);
                bool winding = foe.mode == Mode.Atk && foe.t < MV[(int)foe.cur].imp;
                if (winding && dist < 1.9f && stam > 8f && Random.value < 0.1f + Skill * 0.55f) { guardT = Random.Range(0.5f, 0.9f); guard = true; }
                else if (dist <= reach && atkCd <= 0f && !foe.Down)
                {
                    var opts = new List<(Move, float)> { (Move.Jab, 5f), (Move.Cross, 4f), (Move.Hook, 3f), (Move.Body, 2f) };
                    if (Skill > 0.5f) opts.Add((Move.Kick, 1f));
                    if (temper > 0.5f && stam > 45f) opts.Add((Move.Hay, 1.4f));
                    if (foe.guard) opts.Add((Move.Shove, 3f));
                    if (stam < 12f) atkCd = Random.Range(0.4f, 0.9f);
                    else { Attack(Pick(opts)); atkCd = Random.Range(0.35f, 0.9f) * (1.35f - Skill * 0.55f) + (stam < 30f ? 0.5f : 0f); }
                }
                else if (dist < 1.3f && Random.value < 0.5f) { strafe = Random.value < 0.5f ? 1f : -1f; strafeT = Random.Range(0.5f, 1.2f); }
            }
            if (mode != Mode.Idle) { Animate(Vector3.zero, dt); return; }

            Vector3 dir = d / Mathf.Max(dist, 0.01f), v = Vector3.zero;
            if (dist > reach - 0.03f)
            {
                float sp = dist > 4.5f ? 3f : 1.7f + Skill * 0.3f;
                if (ag != null && ag.enabled && ag.isOnNavMesh && dist > 2.5f)
                {
                    ag.isStopped = false; ag.speed = sp;
                    if (Time.time > repath) { repath = Time.time + 0.25f; ag.SetDestination(foe.transform.position - dir * (reach - 0.2f)); }
                    Animate(ag.velocity, dt, true); return;
                }
                v = dir * sp;
            }
            else if (strafeT > 0f && !guard) v = new Vector3(-dir.z, 0f, dir.x) * strafe * 0.8f;
            else if (dist < 0.72f) v = -dir * 0.9f;
            if (ag != null && ag.enabled && ag.isOnNavMesh) { ag.isStopped = true; ag.velocity = Vector3.zero; }
            v += Separation() * 1.6f;
            if (v.sqrMagnitude > 1e-4f)
            {
                if (ag == null || !ag.enabled) { p.Step(transform.position + v, v.magnitude); Animate(Vector3.zero, dt, true); return; }
                MoveBy(v * dt);
            }
            Animate(v, dt);
        }

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
                if (P != null && other == P && !n.aggr) PlayerStarted = true;
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
