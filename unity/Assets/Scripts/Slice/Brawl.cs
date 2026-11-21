using System.Collections;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Fists and feet for anyone: jab, haymaker, kick; taking hits (stagger), and getting knocked out when health runs
    /// out (ragdoll, then up again later). The player drives it from buttons; residents from their brawling AI.
    /// </summary>
    public class Fighter : MonoBehaviour
    {
        public enum Move { Punch, Hay, Kick }
        public float health = 100f, maxHealth = 100f;
        Person p; float busyUntil, lastHurt = -99f, lastPunch = -99f;
        public bool Busy => Time.time < busyUntil;
        public float Health01 => health / maxHealth;
        public static System.Action<Fighter, Fighter, float> OnHit; // attacker, victim, damage
        public Person Body => p;

        void Awake() { p = GetComponent<Person>(); }

        // fighting back, for people who don't have a Resident brain (paramedics, passers-by)
        Fighter foe; float angryUntil, nextSwing;
        public bool Fighting => foe != null;

        void Update()
        {
            if (p == null || p.state != Person.State.Walking) return;
            if (Time.time - lastHurt > 4f && health < maxHealth) health = Mathf.Min(maxHealth, health + 6f * Time.deltaTime);
            if (foe != null) BrawlStep();
        }

        public void ProvokedBy(Fighter by)
        {
            if (by == null || p == null || p.isPlayer || p.state != Person.State.Walking) return;
            bool fight = p.isParamedic || p.label == "Paramedic" ? Random.value < 0.8f : p.male ? Random.value < 0.6f : Random.value < 0.15f;
            if (foe == by) { angryUntil = Time.time + 20f; return; }
            if (!fight) return;
            foe = by; angryUntil = Time.time + 25f; nextSwing = Time.time + Random.Range(0.3f, 0.7f);
            p.SetKneel(false);
            if (GameAudio.I != null) GameAudio.I.PlayVoice("hostile", transform.position + Vector3.up * 1.6f, 0.2f);
        }

        void BrawlStep()
        {
            var fb = foe.Body;
            float dist = fb != null ? (foe.transform.position - transform.position).WithY(0).magnitude : 999f;
            if (fb == null || fb.state != Person.State.Walking || dist > 25f || Time.time > angryUntil || p.hurt) { foe = null; return; }
            if (dist > 1.05f) { if (!Busy) p.Step(foe.transform.position, 2.8f); }
            else
            {
                Vector3 d = (foe.transform.position - transform.position).WithY(0);
                if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 540f * Time.deltaTime);
                if (Time.time > nextSwing && !Busy)
                {
                    float r = Random.value;
                    Attack(r < 0.55f ? Move.Punch : r < 0.8f ? Move.Hay : Move.Kick);
                    nextSwing = Time.time + Random.Range(0.7f, 1.5f);
                }
            }
        }

        /// <summary>Called after getting back up.</summary>
        public void Recover() { health = maxHealth * 0.6f; busyUntil = 0f; }

        static readonly (float dur, float impact, float reach, float dmg, float push)[] spec =
        {
            (0.42f, 0.21f, 1.2f, 22f, 2.6f),   // jab (anim at 1.9x)
            (0.6f, 0.26f, 1.35f, 38f, 3.6f),   // haymaker (1.75x)
            (0.72f, 0.38f, 1.45f, 32f, 4.2f),  // front kick (1.65x)
        };

        /// <summary>The player's punch button: a second press soon after a jab throws the big swing.</summary>
        public void PunchButton() { Attack(Time.time - lastPunch < 0.6f ? Move.Hay : Move.Punch); }

        public bool Attack(Move m)
        {
            if (p == null || p.anim == null || !p.anim.enabled || p.state != Person.State.Walking || Busy) return false;
            var sp = spec[(int)m];
            busyUntil = Time.time + sp.dur;
            if (m == Move.Punch) lastPunch = Time.time; else if (m == Move.Hay) lastPunch = -99f;
            // face whoever is nearest in front, so the swing lands
            var tgt = FindTarget(sp.reach + 0.6f, 75f);
            if (tgt != null) { Vector3 d = (tgt.transform.position - transform.position).WithY(0); if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d); }
            p.anim.SetTrigger(m == Move.Punch ? "Punch" : m == Move.Hay ? "Hay" : "Kick");
            StartCoroutine(Swing(m, sp.impact, sp.reach, sp.dmg, sp.push));
            return true;
        }

        IEnumerator Swing(Move m, float impact, float reach, float dmg, float push)
        {
            yield return new WaitForSeconds(impact * 0.6f);
            if (GameAudio.I != null) { var w = SfxBank.Pick(m == Move.Kick ? "whooshKick" : "whooshPunch"); if (w != null) GameAudio.I.Play3D(w, transform.position + Vector3.up * 1.2f, 0.5f, Random.Range(0.95f, 1.08f), 20f, 2f); }
            yield return new WaitForSeconds(impact * 0.4f);
            if (p.state != Person.State.Walking) yield break;
            var t = FindTarget(reach, 55f);
            if (t != null) t.TakeHit(this, m, dmg * Random.Range(0.85f, 1.15f), (t.transform.position - transform.position).WithY(0).normalized * push);
        }

        Fighter FindTarget(float reach, float cone)
        {
            Fighter best = null; float bd = reach;
            foreach (var o in Person.All)
            {
                if (o == null || o == p || o.state != Person.State.Walking) continue;
                Vector3 d = (o.transform.position - transform.position).WithY(0);
                float dist = d.magnitude; if (dist > bd || dist < 0.05f) continue;
                if (Vector3.Angle(transform.forward, d) > cone) continue;
                var f = o.GetComponent<Fighter>(); if (f == null) continue;
                best = f; bd = dist;
            }
            return best;
        }

        public void TakeHit(Fighter from, Move m, float dmg, Vector3 push)
        {
            if (p == null || p.state != Person.State.Walking) return;
            if (p.isPlayer && p.GetComponent<PlayerCtl>() is PlayerCtl pc && pc.InCar) return;
            health -= dmg; lastHurt = Time.time;
            Vector3 at = transform.position + Vector3.up * (m == Move.Kick ? 1.0f : 1.55f);
            bool ko = health <= 0f;
            if (GameAudio.I != null)
            {
                var c = SfxBank.Pick(ko ? "hitKO" : m == Move.Kick ? "hitBody" : "hitFace");
                if (c != null) GameAudio.I.Play3D(c, at, ko ? 1f : 0.8f, Random.Range(0.93f, 1.07f), 35f, 3f);
                if (ko || Random.value < 0.6f) GameAudio.I.Pain(at, p.male, 0.08f);
            }
            if (Gore.Level > 0 && (ko || Random.value < 0.3f)) Gore.Burst(at + transform.forward * 0.1f, -push.normalized + Vector3.up * 0.5f, ko ? 30 : 10);
            OnHit?.Invoke(from, this, dmg);
            if (ko)
            {
                health = 0f; busyUntil = 0f;
                p.Knockout(push + Vector3.up * 0.8f);
            }
            else
            {
                busyUntil = Time.time + 0.5f;
                if (p.anim != null && p.anim.enabled) p.anim.SetTrigger("Struck");
                StartCoroutine(Stagger(push.normalized * 0.25f));
            }
            var res = GetComponent<Resident>();
            if (res != null && from != null) res.Provoked(from);
            else if (from != null && !p.isPlayer) ProvokedBy(from);
        }

        IEnumerator Stagger(Vector3 shove)
        {
            var cc = GetComponent<CharacterController>(); var ag = GetComponent<UnityEngine.AI.NavMeshAgent>();
            for (float t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                Vector3 step = shove * (Time.deltaTime / 0.2f);
                if (cc != null && cc.enabled) cc.Move(step);
                else if (ag != null && ag.enabled && ag.isOnNavMesh) ag.Move(step);
                yield return null;
            }
        }
    }
}
