using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Who someone is beyond how they look: a name, and how warm, flirty, greedy and hot-headed they are. Fixed per
    /// person (seeded from them), so the same neighbour is always the same sort of person.
    /// </summary>
    public static class Persona
    {
        public struct P { public string name; public float warmth, flirt, greed, pride; public bool romance; }
        static readonly Dictionary<Person, P> map = new Dictionary<Person, P>();
        static readonly string[] MaleNames = { "Gaz", "Dean", "Lee", "Wayne", "Craig", "Darren", "Shane", "Kev", "Tony", "Mick", "Jason", "Paul", "Ryan", "Dale", "Steve", "Carl", "Barry", "Neil", "Russ", "Terry" };
        static readonly string[] FemaleNames = { "Shaz", "Tracy", "Kelly", "Donna", "Leanne", "Jade", "Michelle", "Kerry", "Stacey", "Debbie", "Lisa", "Gemma", "Carol", "Mandy", "Bev", "Julie" };
        public static P Of(Person p)
        {
            if (p == null) return default;
            if (map.TryGetValue(p, out var x)) return x;
            uint h = (uint)Mathf.Abs(p.GetInstanceID() * 7919 + 13);
            var list = p.male ? MaleNames : FemaleNames;
            x = new P
            {
                name = p.isParamedic || p.label == "Paramedic" ? "The paramedic" : list[h % list.Length],
                warmth = ((h >> 3) % 100) / 100f, flirt = ((h >> 7) % 100) / 100f, greed = ((h >> 11) % 100) / 100f, pride = ((h >> 13) % 100) / 100f,
            };
            if (p.drunk) x.warmth = Mathf.Min(1f, x.warmth + 0.2f);
            x.romance = !p.isParamedic && x.flirt > 0.45f;
            map[p] = x; return x;
        }
        public static string Name(Person p) => p == null ? "Someone" : p.isPlayer ? "You" : Of(p).name;
    }

    /// <summary>
    /// How people feel about you: friendship (-100..100), grudge (0..100), romance (0..100), whether you've met, what's
    /// happened today, and your reputation round here. Fights and conversations both write it.
    /// </summary>
    public static class Rel
    {
        public struct R { public float friend, grudge, romance; public bool met; public int stage, day; public bool talked, cadged; public int flirted; public float lastTalk; }
        static readonly Dictionary<Person, R> map = new Dictionary<Person, R>();
        public static float Rep;
        public static int Day => Mathf.FloorToInt(Time.time / 1200f); // a "day" until the clock goes in: 20 minutes
        public static R Of(Person p)
        {
            if (p == null || !map.TryGetValue(p, out var r)) return default;
            if (r.day != Day) { r.day = Day; r.talked = r.cadged = false; r.flirted = 0; r.grudge = Mathf.Max(0f, r.grudge - 10f); map[p] = r; }
            return r;
        }
        public static void Set(Person p, R r) { if (p != null) { r.day = Day; map[p] = r; } }

        /// <summary>Change how someone feels about you, with the "likes you" / "proper mate" / romance milestones.</summary>
        public static void Bump(Person p, float friend, float romance = 0f, bool quiet = false)
        {
            if (p == null) return;
            var r = Of(p); float before = r.friend; var who = Persona.Of(p);
            r.friend = Mathf.Clamp(r.friend + friend, -100f, 100f);
            if (who.romance) r.romance = Mathf.Clamp(r.romance + romance, 0f, 100f);
            if (!quiet)
            {
                if (before < 30f && r.friend >= 30f) World.Toast(who.name + " likes you.", 2);
                if (before < 60f && r.friend >= 60f) World.Toast(who.name + " counts you as a proper mate.", 2);
            }
            if (who.romance)
            {
                int st = r.romance >= 75f ? 3 : r.romance >= 45f ? 2 : r.romance >= 20f ? 1 : 0;
                if (st > r.stage)
                {
                    r.stage = st;
                    World.Toast(st == 1 ? who.name + " is flirting back." : st == 2 ? "You and " + who.name + " are seeing each other." : "You and " + who.name + " are together.", 2);
                }
            }
            Set(p, r);
        }
    }

    /// <summary>Your money. The top-left of the screen shows it.</summary>
    public static class Econ
    {
        public static float Cash = 15f;
        public static bool Can(float v) => Cash + 1e-4f >= v;
        public static bool Spend(float v, string what) { if (!Can(v)) { World.Toast("You can't afford that.", 1); return false; } Cash -= v; return true; }
        public static void Give(float v) { Cash += v; }
        public static string Fmt(float v) => v < 1f && v > 0f ? Mathf.RoundToInt(v * 100f) + "p" : "£" + v.ToString("0.00");
    }

    /// <summary>
    /// What people say out on the estate and round town. The old game's lines where they fit outside the pub (pub talk
    /// stays in the pub), plus new ones in the same voice.
    /// </summary>
    public static class Lines
    {
        static readonly Dictionary<string, string[]> S = new Dictionary<string, string[]>
        {
            ["greetNew"] = new[] { "Alright?", "Can I help you?", "Sorry, do I know you?", "Y'alright, love?", "New face. You lost?", "You're not from round here, are you?" },
            ["hello"] = new[] { "Alright.", "Hiya.", "You again.", "Y'alright?", "Now then.", "What's occurring?" },
            ["helloFriend"] = new[] { "Oh, hiya!", "Alright, mate!", "Here they are!", "Alright, trouble.", "My favourite person round here. Don't tell anyone." },
            ["helloEnemy"] = new[] { "Keep away from me.", "Not you again.", "What do you want?", "Oh. It's you.", "Don't start." },
            ["chat"] = new[] {
                "Bus is never on time. Never.", "They've shut the bank now. Where are you meant to go?", "Have you seen what they want for a pint at the Oak?",
                "Parking round here's a joke.", "Council's had that road up three times this year.", "My lad's at the school. Little sod.",
                "It used to be all shops, this. Proper shops.", "Chippy's good. Don't get the fishcake.",
                "My missus thinks I'm at night school. Night school of life, mate.", "Saw a fox in the garden last week. Had more manners than half the people round here.",
                "They want to put a gastropub in on the high street. Small plates. Small plates! I want a big plate.", "Best thing about round here? Nobody asks questions.",
                "Someone's had the wheels off a van on Duckett Lane. Just the wheels.", "Council say they'll fix the lift in the spring. Which spring, they didn't say.",
                "Ice cream van's been round twice today. Twice. Something's going on there." },
            ["drunk"] = new[] { "I love you, mate. I mean it.", "Is it me or is the floor moving?", "I'm not drunk. You're drunk. Everyone's drunk.", "Hold on. Hold on. Where was I going?" },
            ["flirtOk"] = new[] { "Ha! Smooth. Keep going.", "Behave. ...Actually, don't.", "You're trouble, you are." },
            ["flirtNo"] = new[] { "Not a chance.", "Did that line work on anyone, ever?", "Easy, tiger." },
            ["flirtMuch"] = new[] { "Alright, alright. Don't overdo it." },
            ["bye"] = new[] { "See you.", "Ta-ra.", "Mind how you go." },
            ["busy"] = new[] { "Not now, I'm in the middle of something." },
            ["talkedOut"] = new[] { "We've done this, haven't we? Go on, I've got things to do.", "Is this going to become a regular thing?" },
            ["approach.chat"] = new[] { "Here, you got a minute?", "Oi, you. Settle something for me." },
            ["approach.hello"] = new[] { "Alright! Thought it was you.", "There they are." },
            ["approach.drunk"] = new[] { "You... you're alright, you are. C'mere.", "Mate. MATE. Listen." },
            ["approach.confront"] = new[] { "You. Yeah, you. We need a word.", "Remember me? Course you do." },
            ["approach.cadge"] = new[] { "Don't suppose you've got a quid for the bus?", "Lend us a couple of quid? I'm good for it." },
            ["approach.news"] = new[] { "Here, have you heard?", "You'll never guess." },
            ["approach.flirt"] = new[] { "Hiya, you. Fancy seeing you here.", "Oh, it's you. I was hoping it'd be you." },
            ["cadgeYes"] = new[] { "Lifesaver. I'll get you back. Probably.", "You're a diamond, you." },
            ["cadgeNo"] = new[] { "Tight git.", "Suit yourself." },
            ["sorryOk"] = new[] { "...Fine. But I'm watching you.", "Right. We'll leave it there. This once." },
            ["sorryNo"] = new[] { "Sorry don't cut it, mate.", "You will be." },
            ["squareUp"] = new[] { "Right then. Come on.", "Go on then." },
            ["ignored"] = new[] { "Charming.", "Suit yourself, then." },
            ["gossip"] = new[] {
                "Them at number four have got the bailiffs round again.", "Somebody's been going through the bins on the lane. Foxes, they reckon. Foxes with hands.",
                "That posh lot up at the cottages want CCTV on the lane. To watch us, like.", "There's a bloke selling meat out of a van by the garages. Don't ask what meat." },
            ["remark.eye"] = new[] { "Christ, who gave you that black eye?", "That eye is a picture. Mind you don't walk into any more doors.", "Nice shiner." },
            ["remark.lip"] = new[] { "You're bleeding, you daft sod.", "That lip looks nasty." },
            ["remark.nose"] = new[] { "Your nose... it's gone a funny shape.", "You want to get some ice on that nose." },
            ["remark.brow"] = new[] { "You've got a cut over your eye.", "That brow wants a plaster." },
        };
        public static string Say(string key) => S.TryGetValue(key, out var a) && a.Length > 0 ? a[Random.Range(0, a.Length)] : "...";

        /// <summary>Things people have heard about you (fights you've been in), for "have you heard?".</summary>
        public static readonly List<string> Rumours = new List<string>();
        public static void Rumour(string s) { Rumours.Add(s); if (Rumours.Count > 6) Rumours.RemoveAt(0); }
    }

    /// <summary>
    /// Talking to people. Walk up and TALK: they greet you as a stranger, a mate or someone they've got a problem with,
    /// and you can natter, flirt, or leave it. People also come up to you: for a chat, to say hello, drunk and swaying,
    /// to cadge a quid, with gossip, smiling if they fancy you, or for a word if you've given them a grudge (and that
    /// can turn into a scrap). They pass comment on a black eye or a split lip. Not while you're driving, running,
    /// fighting or already talking.
    /// </summary>
    public class Social : MonoBehaviour
    {
        public static Social I;
        public static void Create(Transform parent) { if (I != null) return; var g = new GameObject("Social"); g.transform.SetParent(parent, false); I = g.AddComponent<Social>(); }

        PlayerCtl pc; Person player; Fighter pf;
        public Person Talking { get; private set; }
        public Person Near { get; private set; }  // who TALK would talk to
        string pending; Person approacher; public float approachCool = 25f; float approachT, nextApproachCheck, remarkCool, lastSpeedCheck; Vector3 lastPos;

        // injuries people can see, from head hits in fights (eye, lip, nose, brow), each fading after a while
        public static readonly Dictionary<string, float> Marks = new Dictionary<string, float>();
        public static void Mark(string what, float secs) { Marks[what] = Mathf.Max(Marks.TryGetValue(what, out var t) ? t : 0f, Time.time + secs); }

        void Start() { pc = FindFirstObjectByType<PlayerCtl>(); if (pc != null) { player = pc.GetComponent<Person>(); pf = pc.GetComponent<Fighter>(); lastPos = pc.transform.position; } }

        bool PlayerFree(out float speed)
        {
            speed = 0f;
            if (pc == null || player == null || pc.InCar || player.state != Person.State.Walking || DialogueUI.Open) return false;
            if (Fighter.Session.Hot || (pf != null && (pf.Busy || pf.guard))) return false;
            speed = (pc.transform.position - lastPos).magnitude / Mathf.Max(Time.deltaTime, 1e-3f);
            return speed < 3f;
        }

        void Update()
        {
            if (pc == null) { Start(); if (pc == null) return; }
            bool free = PlayerFree(out float speed);
            lastPos = pc.transform.position;
            Near = free || DialogueUI.Open ? FindNear() : null;
            if (Talking != null && (Talking.state != Person.State.Walking || (Talking.transform.position - pc.transform.position).magnitude > 4.5f || IsFighting(Talking))) EndTalk(false);
            if (free) { Approaches(); Remarks(); }
            else if (approacher != null && Fighter.Session.Hot) CancelApproach(false);
        }

        static bool IsFighting(Person p) { var f = p.GetComponent<Fighter>(); return f != null && f.Fighting; }

        Person FindNear()
        {
            Person best = null; float bs = 99f; Vector3 me = pc.transform.position;
            foreach (var o in Person.All)
            {
                if (o == null || o == player || !o.gameObject.activeInHierarchy || o.state != Person.State.Walking || !o.Ready || IsFighting(o)) continue;
                Vector3 d = o.transform.position - me; if (Mathf.Abs(d.y) > 1f) continue; d.y = 0f;
                float dist = d.magnitude; if (dist > 2.3f) continue;
                float ang = Vector3.Angle(pc.transform.forward, d);
                float sc = dist + ang / 90f;
                if (o == approacher) sc -= 2f;
                if (sc < bs) { bs = sc; best = o; }
            }
            return best;
        }

        // ------------------------------------------------------------------------------------------------ you talking to them
        /// <summary>TALK pressed: answer whoever came over, or talk to whoever's in front of you.</summary>
        public void TalkPressed()
        {
            if (DialogueUI.Open) { EndTalk(true); return; }
            if (CasinoFloor.TryUse()) return; // at a table or a machine in the casino: sit down to it
            if (Gangs.TryDeal()) return; // by a dealer: see what he's got
            var n = Near; if (n == null) return;
            string kind = n == approacher ? pending : null;
            CancelApproach(true);
            Open(n, kind);
        }

        void Open(Person n, string kind)
        {
            Talking = n;
            var res = n.GetComponent<Resident>(); if (res != null) res.TalkWith(pc.transform, true);
            var r = Rel.Of(n); var who = Persona.Of(n);
            string line;
            if (kind != null) line = kind == "news" ? News(n) : Lines.Say("approach." + kind);
            else if (n.drunk && Random.value < 0.4f) line = Lines.Say("drunk");
            else if (!r.met) line = Lines.Say("greetNew");
            else if (r.friend <= -30f || r.grudge > 40f) line = Lines.Say("helloEnemy");
            else if (r.friend >= 30f) line = Lines.Say("helloFriend");
            else line = Lines.Say("hello");
            r.met = true; r.lastTalk = Time.time; Rel.Set(n, r);
            Say(n, line);
            if (kind == "confront") ShowConfront(n);
            else if (kind == "cadge") ShowCadge(n);
            else ShowRoot(n);
        }

        void Say(Person n, string line)
        {
            DialogueUI.Show(Persona.Name(n), line);
            if (GameAudio.I != null) GameAudio.I.PlayVoice("resident", n.transform.position + Vector3.up * 1.6f);
            n.SetTalk(true);
        }

        void ShowRoot(Person n)
        {
            var who = Persona.Of(n); var r = Rel.Of(n);
            var opts = new List<(string, System.Action)> { ("Have a natter", () => Natter(n)) };
            if (who.romance || r.romance > 0f) opts.Add(("Flirt", () => Flirt(n)));
            opts.Add(("Leave it", () => { Say(n, Lines.Say("bye")); EndTalk(true); }));
            DialogueUI.Options(opts);
        }

        void Natter(Person n)
        {
            var r = Rel.Of(n); var who = Persona.Of(n);
            if (r.talked) { Say(n, Lines.Say("talkedOut")); Rel.Bump(n, -1f, 0f, true); ShowRoot(n); return; }
            r.talked = true; Rel.Set(n, r);
            float gain = r.friend > 50f ? 2f : 3f + Mathf.Round(who.warmth * 4f);
            Rel.Bump(n, gain);
            string text = Random.value < 0.3f && Lines.Rumours.Count > 0 ? Lines.Rumours[Random.Range(0, Lines.Rumours.Count)] : Random.value < 0.25f ? Lines.Say("gossip") : n.drunk && Random.value < 0.4f ? Lines.Say("drunk") : Lines.Say("chat");
            Say(n, text);
            ShowRoot(n);
        }

        void Flirt(Person n)
        {
            var r = Rel.Of(n); var who = Persona.Of(n);
            if (r.flirted >= 2) { Say(n, Lines.Say("flirtMuch")); ShowRoot(n); return; }
            r.flirted++; Rel.Set(n, r);
            float p = 0.25f + who.flirt * 0.4f + r.friend / 250f + r.romance / 200f;
            if (Social.Marks.Count > 0) p -= 0.1f; // the black eye isn't helping
            if (who.romance && Random.value < Mathf.Clamp(p, 0.05f, 0.92f)) { Rel.Bump(n, 2f, 6f + Mathf.Round(who.flirt * 4f)); Say(n, Lines.Say("flirtOk")); }
            else { Rel.Bump(n, -3f, -1f); Say(n, Lines.Say("flirtNo")); }
            ShowRoot(n);
        }

        void ShowCadge(Person n)
        {
            DialogueUI.Options(new List<(string, System.Action)> {
                ("Here you go (£1)", () => { if (Econ.Spend(1f, "Gave " + Persona.Name(n) + " a quid")) { var r = Rel.Of(n); r.cadged = true; Rel.Set(n, r); Rel.Bump(n, 5f); Say(n, Lines.Say("cadgeYes")); } ShowRoot(n); }),
                ("Not today", () => { var r = Rel.Of(n); r.cadged = true; Rel.Set(n, r); Rel.Bump(n, -2f, 0f, true); Say(n, Lines.Say("cadgeNo")); ShowRoot(n); }),
            });
        }

        void ShowConfront(Person n)
        {
            DialogueUI.Options(new List<(string, System.Action)> {
                ("Sorry about before", () => {
                    var r = Rel.Of(n); var f = n.GetComponent<Fighter>(); float temper = f != null ? f.temper : 0.5f;
                    if (Random.value < 0.75f - temper * 0.5f + r.friend / 200f) { r.grudge = Mathf.Max(0f, r.grudge - 40f); Rel.Set(n, r); Rel.Bump(n, 8f, 0f, true); Say(n, Lines.Say("sorryOk")); EndTalkLater(2.5f); }
                    else { Say(n, Lines.Say("sorryNo")); StartFight(n); }
                }),
                ("Go on then", () => { Say(n, Lines.Say("squareUp")); StartFight(n); }),
                ("Walk away", () => { Rel.Bump(n, -2f, 0f, true); EndTalk(true); }),
            });
        }

        void StartFight(Person n)
        {
            var f = n.GetComponent<Fighter>();
            EndTalk(false);
            if (f != null && pf != null) { f.SquareUp(pf, true); pf.stanceT = 6f; }
        }

        string News(Person n)
        {
            if (Lines.Rumours.Count > 0 && Random.value < 0.7f) return Lines.Say("approach.news") + " " + Lines.Rumours[Random.Range(0, Lines.Rumours.Count)];
            return Lines.Say("approach.news") + " " + Lines.Say("gossip");
        }

        float endAt = -1f;
        void EndTalkLater(float secs) { endAt = Time.time + secs; DialogueUI.Options(new List<(string, System.Action)> { ("Leave it", () => EndTalk(true)) }); }
        void LateUpdate() { if (endAt > 0f && Time.time > endAt) { endAt = -1f; EndTalk(true); } }

        public void EndTalk(bool polite)
        {
            if (Talking != null)
            {
                Talking.SetTalk(false);
                var res = Talking.GetComponent<Resident>(); if (res != null) res.TalkWith(pc.transform, false);
            }
            Talking = null; endAt = -1f;
            DialogueUI.Hide(polite ? 1.4f : 0f);
            approachCool = Mathf.Max(approachCool, 20f);
        }

        // ------------------------------------------------------------------------------------------------ them coming to you
        void Approaches()
        {
            approachCool -= Time.deltaTime;
            if (approacher != null)
            {
                approachT += Time.deltaTime;
                if (approacher.state != Person.State.Walking || IsFighting(approacher)) { CancelApproach(false); return; }
                float d = (approacher.transform.position - pc.transform.position).magnitude;
                if (approachT > 25f || (approachT > 6f && d > 10f))
                {
                    // ignored: they take it personally (a grudge-holder might come anyway)
                    var r = Rel.Of(approacher);
                    if (pending == "confront" && d < 6f) { var f = approacher.GetComponent<Fighter>(); if (f != null && pf != null && f.temper > 0.6f) { World.Toast(Persona.Name(approacher) + " isn't letting it go.", 1); f.SquareUp(pf, true); } }
                    else { Rel.Bump(approacher, -1f, 0f, true); if (d < 6f) World.Toast(Persona.Name(approacher) + ": \"" + Lines.Say("ignored") + "\"", 0); }
                    CancelApproach(false);
                }
                return;
            }
            if (approachCool > 0f || Time.time < nextApproachCheck || Talking != null) return;
            nextApproachCheck = Time.time + 1.5f;
            Person best = null; string bestKind = null; float bs = 0f; Vector3 me = pc.transform.position;
            foreach (var n in Person.All)
            {
                if (n == null || n == player || !n.gameObject.activeInHierarchy || n.state != Person.State.Walking || !n.Ready || n.isParamedic) continue;
                var res = n.GetComponent<Resident>(); if (res == null || !res.Free) continue;
                float d = (n.transform.position - me).magnitude; if (d < 2.6f || d > 18f) continue;
                foreach (var (kind, w) in Reasons(n))
                {
                    float s = w * Random.Range(0.6f, 1.4f) * (1f - d / 24f);
                    if (s > bs) { bs = s; best = n; bestKind = kind; }
                }
            }
            if (best == null || bs < 0.8f) return;
            approacher = best; pending = bestKind; approachT = 0f;
            best.GetComponent<Resident>().ComeOver(pc.transform);
            string what = bestKind == "chat" ? " wants a chat" : bestKind == "hello" ? " says hello" : bestKind == "drunk" ? " is swaying over" : bestKind == "confront" ? " wants a word" : bestKind == "cadge" ? " is after something" : bestKind == "news" ? " has news" : " is smiling at you";
            World.Toast(Persona.Name(best) + what + (TouchControls.Wanted ? "  ·  TALK to answer" : "  ·  T to answer"), bestKind == "confront" ? 1 : 0);
        }

        /// <summary>Why this person might come over (each with a weight): the old game's reasons.</summary>
        List<(string, float)> Reasons(Person n)
        {
            var o = new List<(string, float)>(); var r = Rel.Of(n); var who = Persona.Of(n);
            if (r.lastTalk > 0f && Time.time - r.lastTalk < 150f) return o; // just talked: leave you be for a bit
            var f = n.GetComponent<Fighter>(); float temper = f != null ? f.temper : 0.4f;
            if (n.drunk) o.Add(("drunk", 1.2f + Random.value));
            if (r.friend <= -30f && r.grudge > 0f && temper > 0.3f) o.Add(("confront", 3f + temper * 4f));
            if (r.friend >= 20f) o.Add(("hello", 1f + r.friend / 50f));
            if (who.warmth > 0.55f && r.friend > -10f) o.Add(("chat", 0.5f + who.warmth));
            if (who.greed > 0.6f && r.friend > -10f && !r.cadged) o.Add(("cadge", 0.6f + who.greed * 1.5f));
            if (Lines.Rumours.Count > 0 && r.friend > -20f && who.warmth > 0.3f) o.Add(("news", 0.9f));
            if (who.romance && r.stage >= 1) o.Add(("flirt", 1.5f + r.romance / 40f));
            return o;
        }

        void CancelApproach(bool answered)
        {
            if (approacher != null && !answered) { var res = approacher.GetComponent<Resident>(); if (res != null) res.TalkWith(pc.transform, false); }
            approacher = null; if (!answered) pending = null;
            approachCool = Random.Range(110f, 200f) * 0.5f; // a minute or two before anyone else comes over
        }

        // ------------------------------------------------------------------------------------------------ comments on your face
        void Remarks()
        {
            remarkCool -= Time.deltaTime;
            if (remarkCool > 0f || Marks.Count == 0) return;
            string mark = null; foreach (var kv in Marks) if (kv.Value > Time.time) { mark = kv.Key; break; }
            if (mark == null) { Marks.Clear(); return; }
            foreach (var n in Person.All)
            {
                if (n == null || n == player || !n.gameObject.activeInHierarchy || n.state != Person.State.Walking || n.isParamedic || IsFighting(n)) continue;
                if ((n.transform.position - pc.transform.position).magnitude > 3f) continue;
                if (Random.value < 0.5f) { World.Toast(Persona.Name(n) + ": \"" + Lines.Say("remark." + mark) + "\"", 0); if (GameAudio.I != null) GameAudio.I.PlayVoice("resident", n.transform.position + Vector3.up * 1.6f); }
                remarkCool = Random.Range(40f, 80f);
                return;
            }
        }
    }
}
