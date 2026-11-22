using UnityEngine;
using UnityEngine.InputSystem;

namespace ASBO.Slice
{
    /// <summary>Touch buttons for driving (set by the on-screen controls, read by PlayerCar).</summary>
    public static class TouchCar
    {
        public static bool fwd, rev, brake, hand, left, right, boost, horn;
        public static void Clear() { fwd = rev = brake = hand = left = right = boost = horn = false; }
    }

    /// <summary>
    /// A car you can drive. FORWARD to go (drag up on it to boost), BRAKE slows you and, once you've stopped, reverses;
    /// HANDBRAKE locks the back wheels so the rear steps out and you slide round; LEFT / RIGHT steer. Quick and arcadey
    /// like the old game: lots of pace both ways, a tight turning circle at low speed, and at speed only as much turn as
    /// the tyres could really hold (so it's steady, not twitchy). The body sits on the ground at all four wheels, tilting
    /// with slopes and kerbs. Hitting a wall square on stops you (or bounces you back if you were flying); glancing it,
    /// you scrape along it and keep going. Walls and other cars are real obstacles (a rigidbody).
    /// With the RCC rig in the build it runs on real car physics instead (CarRig): suspension, tyres, weight and
    /// gears, driven by the same buttons. Either way each kind of car has its own pace and feel (CarHandling).
    /// </summary>
    public class PlayerCar : MonoBehaviour, IVehicle
    {
        // pace (the everyday hatch): about 70 mph flat out, 90 with the boost; 0-60 in under 4 s; a quick reverse.
        // Each kind of car scales these from its handling (CarHandling): top speed, pull for its weight, brakes, grip, lock.
        float Top = 31f, BoostTop = 40f, Acc = 8.5f, RevTop = 12f, RevAcc = 7f, BrakeDec = 17f, Lock = 0.68f, LatCap = 12f, SteerK = 1f, Tail = 1f;
        const float BoostRamp = 1.6f, HandDec = 6f;

        public static readonly System.Collections.Generic.List<PlayerCar> All = new System.Collections.Generic.List<PlayerCar>();
        public CarHandling Handling => h;
        public string Model => modelId;
        CarHandling h; string modelId;
        // the physics car (RCC): its controller, input, engine; what we feed it each step
        RCCP_CarController rcc; RCCP_Input rin; RCCP_Engine reng; RCCP_Gearbox gearbox; readonly RCCP_Inputs feed = new RCCP_Inputs(); bool coasting; float setTop = -1f;
        public bool Physical => rcc != null;

        bool drivenF;
        /// <summary>Someone's in the driving seat (the player). Getting in starts it up; getting out leaves it parked, or rolling on if you bailed.</summary>
        public bool driven
        {
            get => drivenF;
            set
            {
                if (drivenF == value) return; drivenF = value;
                if (value) I = this;
                if (rcc != null) { if (value) EnterRcc(); else ExitRcc(); }
            }
        }
        Rigidbody rb; Transform vis; Transform[] wheels; CarBody.Spec spec;
        float sp, steer, steerHold, bst, fwdT, revT, wheelSpin, bRoll, bPitch, acc, crunchT, yTarget, slip, yaw, gPitch, gRoll, grip = 10f, pivotZ;
        Vector3 hitN; float hitT; // the wall we're up against this step (horizontal normal), and when
        Vector3 moveDir = Vector3.forward; bool reversing;
        AudioSource eng, skid; bool realEngine;
        public float Slip => slip;
        public float Speed => Mathf.Abs(sp);
        public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
        public float SignedSpeed => sp;
        public static PlayerCar I;

        public static PlayerCar Create(Vector3 pos, float yaw, Color paint, string model = null, CarKit.Wear wear = CarKit.Wear.Clean, int seed = 4242, Transform parent = null)
        {
            model ??= CarKit.Has("hatch90") ? "hatch90" : "hatch_a"; // your motor: the modelled 90s hatch when it's in the build
            PlayerCar c = null;
            if (CarRig.Ready)
            {
                var rg = CarRig.Spawn(pos, yaw, parent);
                c = rg.AddComponent<PlayerCar>();
                if (c.BuildRcc(model, paint, wear, seed)) { CarRig.Finish(rg, parent); c.AfterRcc(); }
                else { Object.Destroy(rg.transform.parent != null ? rg.transform.parent.gameObject : rg); c = null; }
            }
            if (c == null)
            {
                var g = new GameObject("PlayerCar"); g.layer = Person.VehicleLayer; // ragdolls pass through cars; our own ground probe skips it
                g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
                if (parent != null) g.transform.SetParent(parent, true);
                c = g.AddComponent<PlayerCar>(); c.Build(model, paint, wear, seed);
            }
            if (I == null) I = c;
            return c;
        }

        /// <summary>Which handling a model gets: our nine modelled cars by name, vans as vans, the rest an ordinary hatch.</summary>
        static CarHandling HandlingFor(string model, CarBody.Spec spec) => CarHandling.For(CarKit.Has(model) ? model : spec != null && spec.van ? "van" : "default");

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); if (I == this) I = null; }

        /// <summary>The nearest drivable car within reach of p (null if none).</summary>
        public static PlayerCar Nearest(Vector3 p, float reach)
        {
            PlayerCar best = null; float bd = reach * reach;
            foreach (var c in All) { if (c == null) continue; float d = (c.transform.position - p).WithY(0).sqrMagnitude; if (d < bd) { bd = d; best = c; } }
            return best;
        }

        /// <summary>Scale the arcade car's numbers to this kind of car.</summary>
        void Scale()
        {
            Top = 31f * h.topKmh / 168f; BoostTop = Top * 1.29f; RevTop = Mathf.Min(13f, Top * 0.4f);
            float pull = (h.torque / h.mass) / (150f / 960f);
            Acc = 8.5f * Mathf.Clamp(Mathf.Sqrt(pull), 0.7f, 1.45f); RevAcc = 7f * Mathf.Clamp(Mathf.Sqrt(pull), 0.8f, 1.2f);
            BrakeDec = 17f * h.brakes; LatCap = 12f * h.grip; Lock = 0.68f * h.lockScale; SteerK = h.steerRate; Tail = h.tail;
        }

        /// <summary>The physics car: our body fitted to the RCC rig (we're on the rig object, still switched off).</summary>
        bool BuildRcc(string model, Color paint, CarKit.Wear wear, int seed)
        {
            modelId = model; spec = CarBody.Get(model); h = HandlingFor(model, spec); Scale();
            vis = new GameObject("Body").transform; vis.SetParent(transform, false);
            var rng = new System.Random(seed);
            wheels = CarKit.Has(model) ? CarKit.Build(vis, model, paint, wear, true, rng) : CarBody.Build(vis, spec, paint, true, seed, false);
            if (!CarRig.Fit(gameObject, vis, wheels, spec, h)) { Destroy(vis.gameObject); return false; }
            rb = GetComponent<Rigidbody>(); rcc = GetComponent<RCCP_CarController>();
            rin = GetComponentInChildren<RCCP_Input>(true); reng = GetComponentInChildren<RCCP_Engine>(true); gearbox = GetComponentInChildren<RCCP_Gearbox>(true);
            gameObject.AddComponent<Horn>();
            Dents.Add(gameObject, vis, h.mass);
            Obstacle();
            moveDir = transform.forward; yaw = transform.eulerAngles.y;
            return true;
        }

        /// <summary>People walk round it: cut out of their walking mesh while it's parked, steered round while it moves.</summary>
        void Obstacle()
        {
            var ob = gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>(); ob.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            float H = spec.van ? 2.0f : 1.42f;
            ob.center = transform.InverseTransformPoint(vis.TransformPoint(new Vector3(0f, H * 0.5f, 0f)));
            ob.size = new Vector3(spec.m.W + 0.4f, H, spec.m.L + 0.4f); ob.carving = true; ob.carveOnlyStationary = true;
        }

        /// <summary>Just switched on: parked, engine off, handbrake on, until someone gets in.</summary>
        void AfterRcc()
        {
            RCCP.SetControl(rcc, false); RCCP.SetEngine(rcc, false);
            StartCoroutine(OurLayers());
        }
        System.Collections.IEnumerator OurLayers()
        {
            yield return null; // RCC may put the car on its own layers when it starts: the body goes back on ours (ragdolls pass through, ground probes skip it)
            foreach (var t in vis.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Person.VehicleLayer;
            // and drawn smoothly between physics steps (without it the car judders against the camera, worse the faster it goes)
            if (rb != null) { if (rb.interpolation != RigidbodyInterpolation.Interpolate) Debug.Log("[ASBO] RCC car had interpolation " + rb.interpolation + ": put back"); rb.interpolation = RigidbodyInterpolation.Interpolate; }
            yield return new WaitForSeconds(0.5f);
            if (rb != null) rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void EnterRcc()
        {
            coasting = false;
            CarRig.Style(h);
            RCCP.SetControl(rcc, true); RCCP.SetEngine(rcc, true);
            if (rin != null) rin.overridePlayerInputs = true;
            if (h != null && !string.IsNullOrEmpty(h.feel)) World.Toast(h.feel, 1);
        }

        void ExitRcc()
        {
            if (Mathf.Abs(sp) > 1.2f) coasting = true; // bailed out: it rolls on without you
            else Park();
        }

        void Park()
        {
            coasting = false; feed.Clear();
            if (rin != null) { rin.OverrideInputs(feed); rin.DisableOverrideInputs(); rin.overridePlayerInputs = false; }
            RCCP.SetControl(rcc, false); RCCP.SetEngine(rcc, false); // handbrake on, quiet
        }

        /// <summary>One physics step of the RCC car: our buttons become its pedals, wheel and handbrake.</summary>
        static bool toldInterp;
        void DriveRcc(float dt)
        {
            // drawn smoothly between physics steps, always (RCC sets this itself in places; without it the car judders at speed)
            if (rb != null && rb.interpolation != RigidbodyInterpolation.Interpolate)
            {
                if (!toldInterp) { toldInterp = true; Debug.Log("[ASBO] RCC switched the car's interpolation to " + rb.interpolation + ": kept on Interpolate"); }
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            sp = Vector3.Dot(rb.linearVelocity, transform.forward);
            var c = Read(dt);
            if (driven)
            {
                bool boost = c.boost && c.throttle > 0.5f && sp > 1f;
                bst = Mathf.Clamp01(bst + (boost ? dt / BoostRamp : -dt / 1.2f));
                if (reng != null)
                {
                    float top = h.topKmh * (1f + 0.28f * bst);
                    if (Mathf.Abs(top - setTop) > 1f) { setTop = top; reng.maximumSpeed = top; }
                    reng.SetTorqueMultiplier(1f + 0.7f * bst);
                }
                // FORWARD after reversing: once it's all but stopped, straight into first (BRAKE into reverse is RCC's own auto-reverse)
                if (c.throttle > 0.05f && c.brakeRev < 0.05f && rcc.direction < 0 && Mathf.Abs(sp) < 0.6f && gearbox != null) gearbox.ShiftToGear(0);
                feed.throttleInput = c.throttle; feed.brakeInput = c.brakeRev; feed.handbrakeInput = c.hand;
                feed.steerInput = Mathf.Clamp(c.steer * h.lockScale, -1f, 1f); feed.nosInput = 0f; feed.clutchInput = 0f;
                if (rin != null) { rin.overridePlayerInputs = true; rin.OverrideInputs(feed); }
                if (c.horn) GetComponent<Horn>().Toot();
            }
            else if (coasting)
            {
                feed.Clear();
                if (rin != null) { rin.overridePlayerInputs = true; rin.OverrideInputs(feed); }
                if (Mathf.Abs(sp) < 1.2f) Park();
            }
            // how far it's sliding (the side-on part of its motion), for the skid checks elsewhere
            Vector3 v = rb.linearVelocity; v.y = 0f;
            slip = v.sqrMagnitude > 1f ? Vector3.Angle(v, transform.forward * Mathf.Sign(sp == 0f ? 1f : sp)) * Mathf.Clamp01(v.magnitude / 6f) : 0f;
            steer = (rcc != null ? rcc.steerAngle : 0f) * Mathf.Deg2Rad;
        }

        void Build(string model, Color paint, CarKit.Wear wear, int seed)
        {
            modelId = model; spec = CarBody.Get(model); h = HandlingFor(model, spec); Scale();
            vis = new GameObject("Body").transform; vis.SetParent(transform, false);
            wheels = CarKit.Has(model) ? CarKit.Build(vis, model, paint, wear, true, new System.Random(seed)) : CarBody.Build(vis, spec, paint, true, seed, false);
            Dents.Add(gameObject, vis, h.mass);
            float L = spec.m.L, W = spec.m.W, H = spec.van ? 2.0f : 1.42f;
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.32f + (H - 0.32f) * 0.5f, 0f); col.size = new Vector3(W * 0.98f, H - 0.32f, L * 0.98f);
            col.sharedMaterial = new PhysicsMaterial("CarSlide") { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0f };
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = h.mass; rb.useGravity = false; rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotation; // we turn it ourselves (yaw, and tilt to the ground)
            rb.linearDamping = 0f; rb.angularDamping = 0f;
            rb.centerOfMass = new Vector3(0f, 0.45f, 0f);
            yTarget = transform.position.y; yaw = transform.eulerAngles.y; pivotZ = spec.m.wxR;
            var ec = SfxBank.Pick("engine"); realEngine = ec != null;
            eng = gameObject.AddComponent<AudioSource>(); eng.clip = realEngine ? ec : AudioSynth.Engine(); eng.loop = true; eng.spatialBlend = 1f; eng.minDistance = 5f; eng.maxDistance = 70f; eng.volume = 0.12f; eng.Play();
            gameObject.AddComponent<Horn>(); Obstacle();
            skid = gameObject.AddComponent<AudioSource>(); skid.clip = SkidClip(); skid.loop = true; skid.spatialBlend = 1f; skid.minDistance = 5f; skid.maxDistance = 60f; skid.volume = 0f; skid.Play();
            moveDir = transform.forward;
        }

        /// <summary>Tyre squeal: noise through a resonant band that wobbles a little, made on the fly.</summary>
        static AudioClip SkidClip()
        {
            int sr = 22050, n = sr * 2; var d = new float[n]; float y1 = 0f, y2 = 0f; var rnd = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr, f = 1650f + 220f * Mathf.Sin(t * 2f * Mathf.PI * 3f) + 90f * Mathf.Sin(t * 2f * Mathf.PI * 11f);
                float w = 2f * Mathf.PI * f / sr, r = 0.985f, x = (float)(rnd.NextDouble() * 2 - 1);
                float y = x * 0.08f + 2f * r * Mathf.Cos(w) * y1 - r * r * y2; y2 = y1; y1 = y; d[i] = Mathf.Clamp(y * 0.35f, -1f, 1f);
            }
            var c = AudioClip.Create("skid", n, 1, sr, false); c.SetData(d, 0); return c;
        }

        struct Ctl { public float throttle, brakeRev, hand, steer, steerRaw; public bool boost, horn; }

        static float Damp(float a, float b, float k, float dt) => Mathf.Lerp(a, b, 1f - Mathf.Exp(-k * dt));

        Ctl Read(float dt)
        {
            var c = new Ctl();
            bool fwdD = false, brkD = false; float steerD = 0f, padT = 0f, padB = 0f; float? steerA = null;
            if (driven)
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    fwdD |= kb.wKey.isPressed || kb.upArrowKey.isPressed; brkD |= kb.sKey.isPressed || kb.downArrowKey.isPressed;
                    if (kb.spaceKey.isPressed) c.hand = 1f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steerD -= 1f; if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steerD += 1f;
                    c.boost |= kb.leftShiftKey.isPressed; c.horn |= kb.hKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame;
                }
                fwdD |= TouchCar.fwd; brkD |= TouchCar.brake || TouchCar.rev; if (TouchCar.hand) c.hand = 1f; c.boost |= TouchCar.boost;
                if (TouchCar.left) steerD -= 1f; if (TouchCar.right) steerD += 1f;
                if (TouchCar.horn) { c.horn = true; TouchCar.horn = false; }
                foreach (var pad in Gamepad.all)
                {
                    if (pad.name.Contains("OnScreen") || pad.description.deviceClass == "OnScreen") continue;
                    float ax = pad.leftStick.ReadValue().x; if (Mathf.Abs(ax) > 0.15f) steerA = Mathf.Clamp((ax - Mathf.Sign(ax) * 0.15f) / 0.85f, -1f, 1f);
                    padT = Mathf.Max(padT, pad.rightTrigger.ReadValue());
                    padB = Mathf.Max(padB, pad.leftTrigger.ReadValue());
                    if (pad.buttonSouth.isPressed || pad.buttonEast.isPressed) c.hand = 1f;
                    c.boost |= pad.rightShoulder.isPressed; c.horn |= pad.buttonWest.wasPressedThisFrame;
                }
            }
            fwdT = fwdD ? fwdT + dt : 0f; revT = brkD ? revT + dt : 0f;
            // buttons come in quickly now: most of the power straight away, the rest over half a second
            c.throttle = fwdD ? Mathf.Clamp01(0.65f + 0.35f * fwdT / 0.5f) : 0f; if (fwdD && c.boost) c.throttle = 1f;
            c.brakeRev = brkD ? 1f : 0f;
            c.throttle = Mathf.Max(c.throttle, padT); c.brakeRev = Mathf.Max(c.brakeRev, padB);
            // steering: to full lock in a fifth of a second, back to straight just as fast when you let go
            // buttons steer quickly when slow, more gently at speed (so a tap doesn't throw it off line); straighten up quickly
            float rate = Mathf.Lerp(6f, 2.6f, Mathf.Clamp01(Mathf.Abs(sp) / 28f)) * SteerK;
            if (steerD != 0f) steerHold = Mathf.Clamp(steerHold + steerD * dt * (steerHold != 0f && Mathf.Sign(steerD) != Mathf.Sign(steerHold) ? rate * 2.2f : rate), -1f, 1f);
            else steerHold = Damp(steerHold, 0f, 10f, dt);
            c.steer = steerA.HasValue && steerD == 0f ? steerA.Value : steerHold; c.steerRaw = steerD;
            if (!driven) c.brakeRev = 0f;
            return c;
        }

        void OnCollisionStay(Collision c) => Contact(c);
        void OnCollisionEnter(Collision c) => Contact(c);
        void Contact(Collision c)
        {
            // remember the side-on push of whatever we're against (walls, cars, posts); the ground doesn't count
            for (int i = 0; i < c.contactCount; i++)
            {
                var n = c.GetContact(i).normal; if (n.y > 0.55f) continue;
                n.y = 0f; if (n.sqrMagnitude < 1e-4f) continue; n.Normalize();
                if (hitT != Time.fixedTime || Vector3.Dot(n, moveDir) < Vector3.Dot(hitN, moveDir)) { hitN = n; hitT = Time.fixedTime; }
            }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (rcc != null) { DriveRcc(dt); return; }
            var c = Read(dt);
            Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            // up against something (from the last physics step): take off the part of our speed going into it and slide
            // along it with the rest. Square on loses nearly everything (and a big hit bounces back); a glance hardly anything.
            if (Time.fixedTime - hitT < dt * 1.5f && Mathf.Abs(sp) > 0.3f)
            {
                Vector3 vIn = moveDir * Mathf.Abs(sp); float into = -Vector3.Dot(vIn, hitN);
                if (into > 0.05f)
                {
                    if (into > 2.4f && Time.time > crunchT && GameAudio.I != null) { crunchT = Time.time + 0.4f; GameAudio.I.CarCrash(rb.position + moveDir * spec.m.Le, into); }
                    Vector3 slide = vIn + hitN * into; float along = slide.magnitude, cos = into / Mathf.Max(0.01f, vIn.magnitude);
                    if (into > 6f && cos > 0.75f) { sp = -Mathf.Sign(sp) * Mathf.Min(3f, into * 0.15f); } // head on, flat out: thrown back a little
                    else
                    {
                        float keep = Mathf.Lerp(0.92f, 0.2f, cos); // glancing keeps most of it
                        sp = Mathf.Sign(sp) * along * keep;
                        if (along > 0.3f) moveDir = Vector3.Slerp(moveDir, slide.normalized, 0.5f).normalized; // scrape along it
                    }
                }
            }
            // drive: forward pedal; brake that turns into reverse once you've stopped (let go and press again, or just hold)
            bool boost = c.boost && c.throttle > 0.5f && sp > 1f;
            bst = Mathf.Clamp01(bst + (boost ? dt / BoostRamp : -dt / 1.2f));
            float top = Mathf.Lerp(Top, BoostTop, bst);
            bool handOn = c.hand > 0.5f;
            float a = 0f;
            if (!driven) { reversing = false; }
            if (c.brakeRev > 0.02f && sp < 0.6f && !(c.throttle > 0.02f)) reversing = true;
            if (c.brakeRev <= 0.02f && sp > -0.2f) reversing = false;
            if (c.throttle > 0.02f)
            {
                reversing = false;
                if (sp >= -0.3f) a += c.throttle * Acc * (1f + 0.35f * bst) * Mathf.Clamp(1f - Mathf.Pow(Mathf.Max(0f, sp) / top, 2f), -0.5f, 1f) * (handOn ? 0.5f : 1f);
                else a += BrakeDec; // going backwards: forward pedal stops you first
            }
            if (c.brakeRev > 0.02f)
            {
                if (sp > 0.3f) a -= c.brakeRev * BrakeDec;
                else if (reversing) a -= c.brakeRev * RevAcc * Mathf.Clamp(1f - Mathf.Pow(Mathf.Max(0f, -sp) / RevTop, 2f), -0.5f, 1f);
            }
            a -= (0.25f + 0.0022f * sp * sp) * Mathf.Sign(sp) * (sp > 0f ? 1f - Mathf.Clamp01(c.throttle) : reversing ? 0f : 1f); // rolling resistance and air
            if (!(c.throttle > 0.02f) && !(c.brakeRev > 0.02f)) a -= 0.8f * Mathf.Sign(sp);
            if (handOn) a -= Mathf.Sign(sp) * HandDec * Mathf.Clamp01(Mathf.Abs(sp) * 2f);
            float sp0 = sp; sp += a * dt;
            if (sp0 * sp < 0f && !(reversing && sp < 0f)) sp = 0f;
            if (Mathf.Abs(sp) < 0.04f && !(c.throttle > 0.02f) && !(c.brakeRev > 0.02f)) sp = 0f;
            acc = (sp - sp0) / dt;
            // steering: a tight lock at low speed; at speed as much as the tyres would hold (about 1.2 g), so it's steady
            float v = Mathf.Abs(sp); bool sliding = handOn && v > 4f;
            float smax = Mathf.Min(Lock, Mathf.Atan(spec.m.wb * LatCap / Mathf.Max(v * v, 0.01f)));
            if (sliding) smax = Mathf.Min(Lock, smax * (1.6f + 0.6f * Tail) + 0.1f); // handbrake: the back's let go, so it'll come round much tighter
            steer = Damp(steer, Mathf.Clamp(c.steer, -1f, 1f) * smax, 12f, dt);
            float yawRate = sp * Mathf.Tan(steer) / spec.m.wb * (sliding ? 1f + 0.35f * Tail : 1f);
            // grip: the way we're moving catches up with the way we're pointing. On the handbrake it hardly does (the tail
            // steps out and we slide); let go and it grips again over half a second, catching the slide rather than snapping.
            Vector3 heading = fwd * Mathf.Sign(sp == 0f ? 1f : sp);
            grip = sliding ? Damp(grip, 1.1f / Tail, 8f, dt) : Mathf.MoveTowards(grip, 10f, dt * 18f);
            moveDir = Vector3.Slerp(moveDir.sqrMagnitude > 0.5f ? moveDir : heading, heading, 1f - Mathf.Exp(-grip * dt)).normalized;
            slip = Vector3.Angle(moveDir, heading) * Mathf.Clamp01(v / 6f);
            if (slip > 4f) sp -= Mathf.Sign(sp) * Mathf.Min(v, slip * 0.012f * v * dt); // sliding sideways scrubs speed
            // the car turns about its back axle like a real one; when the back lets go the pivot moves forward, so the tail comes round
            pivotZ = Mathf.Lerp(pivotZ, sliding ? spec.m.wxF * 0.35f : spec.m.wxR, 1f - Mathf.Exp(-6f * dt));
            Vector3 pos = rb.position; Vector3 pivotW = pos + fwd * pivotZ;
            yaw += yawRate * Mathf.Rad2Deg * dt;
            Vector3 fwd2 = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 nextH = pivotW + moveDir * Mathf.Abs(sp) * dt - fwd2 * pivotZ; nextH.y = pos.y;
            // sit on the ground at all four wheels: height from their average, and tilt (nose up a slope, lean over a kerb)
            float hFL = Ground(nextH, fwd2, -1f, spec.m.wxF), hFR = Ground(nextH, fwd2, 1f, spec.m.wxF), hRL = Ground(nextH, fwd2, -1f, spec.m.wxR), hRR = Ground(nextH, fwd2, 1f, spec.m.wxR);
            if (!float.IsNaN(hFL + hFR + hRL + hRR))
            {
                yTarget = (hFL + hFR + hRL + hRR) * 0.25f;
                float pitchT = Mathf.Atan2((hFL + hFR) * 0.5f - (hRL + hRR) * 0.5f, spec.m.wb) * Mathf.Rad2Deg, rollT = Mathf.Atan2((hFR + hRR) * 0.5f - (hFL + hRL) * 0.5f, spec.m.W * 0.85f) * Mathf.Rad2Deg;
                gPitch = Damp(gPitch, Mathf.Clamp(pitchT, -22f, 22f), 12f, dt); gRoll = Damp(gRoll, Mathf.Clamp(rollT, -18f, 18f), 12f, dt);
            }
            float vy = (yTarget - pos.y) / Mathf.Max(dt, 1e-4f) * 0.35f;
            var hv = (nextH - pos) / dt; hv.y = Mathf.Clamp(vy, -6f, 4f);
            rb.linearVelocity = hv; rb.angularVelocity = Vector3.zero;
            rb.rotation = Quaternion.Euler(-gPitch, yaw, gRoll); // set directly (rotation is frozen to the physics: walls can't spin it)
            // body lean and nose dip (on its springs), wheels, horn
            bRoll = Damp(bRoll, Mathf.Clamp(sp * yawRate * 0.006f, -0.035f, 0.035f), 5f, dt);
            bPitch = Damp(bPitch, Mathf.Clamp(acc * 0.0035f, -0.03f, 0.022f), 6f, dt);
            if (c.horn) GetComponent<Horn>().Toot();
        }

        /// <summary>Height of the ground under one wheel (side -1 left, +1 right; z along the car), or NaN if there's nothing there.</summary>
        float Ground(Vector3 at, Vector3 fwd, float side, float z)
        {
            Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);
            Vector3 p = at + fwd * z + right * side * spec.m.hw * 0.82f;
            if (Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out var gh, 3f, ~((1 << 2) | (1 << 31) | (1 << Person.VehicleLayer) | (1 << Person.RagdollLayer)), QueryTriggerInteraction.Ignore)) return gh.point.y;
            return float.NaN;
        }

        void Update()
        {
            if (rcc != null) return; // RCC spins, steers and springs the wheels, and makes its own engine and tyre noise
            if (vis != null) vis.localRotation = Quaternion.Euler(-bPitch * Mathf.Rad2Deg, 0f, -bRoll * Mathf.Rad2Deg);
            wheelSpin += sp / Mathf.Max(0.2f, spec.m.rad) * Time.deltaTime * Mathf.Rad2Deg;
            if (wheels != null)
                for (int i = 0; i < wheels.Length; i++)
                {
                    float st = i < 2 ? steer * Mathf.Rad2Deg : 0f;
                    wheels[i].localRotation = Quaternion.Euler(0f, st, 0f) * Quaternion.Euler(wheelSpin, 0f, 90f);
                }
            if (skid != null) skid.volume = Mathf.Lerp(skid.volume, Mathf.Clamp01((slip - 6f) / 25f) * 0.5f, 10f * Time.deltaTime);
            if (eng != null)
            {
                float rev = realEngine ? 0.95f + Mathf.Repeat(Mathf.Min(Mathf.Abs(sp), 40f), 13f) * 0.06f + Mathf.Min(Mathf.Abs(sp), 40f) * 0.012f + (driven && fwdT > 0f ? 0.15f : 0f) : 0.6f + Mathf.Abs(sp) * 0.04f; // rises and drops back through the gears
                eng.pitch = Mathf.Lerp(eng.pitch, rev, 6f * Time.deltaTime);
                eng.volume = Mathf.Lerp(eng.volume, driven ? 0.42f : 0.12f, 3f * Time.deltaTime);
            }
        }

        // the driver's door (a separate part on the modelled cars): which side it's on, its hinge and length, how open it is
        Transform doorHinge; float doorLen = 1f, doorOpen, doorWant; bool doorLooked;
        public float DoorSide { get { FindDoor(); return doorHinge != null ? Mathf.Sign(doorHinge.localPosition.x) : 1f; } }
        void FindDoor()
        {
            if (doorLooked || vis == null) return; doorLooked = true;
            foreach (var t in vis.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("DoorHinge")) { doorHinge = t; var bits = t.name.Split('|'); if (bits.Length > 1) float.TryParse(bits[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out doorLen); break; }
        }
        /// <summary>Open (1) or shut (0) the driver's door.</summary>
        public void Door(float open) { FindDoor(); doorWant = open; }
        void LateUpdate()
        {
            if (doorHinge == null) return;
            float was = doorOpen; doorOpen = Mathf.MoveTowards(doorOpen, doorWant, Time.deltaTime * (doorWant > doorOpen ? 3.4f : 2.6f));
            float e = doorOpen * doorOpen * (3f - 2f * doorOpen);
            doorHinge.localRotation = Quaternion.Euler(0f, -DoorSide * 68f * e, 0f);
            if (was > 0.02f && doorOpen <= 0.02f && doorWant == 0f && GameAudio.I != null) GameAudio.I.Play3D(AudioSynth.Thud(), doorHinge.position, 0.55f, 1.3f, 25f, 2f); // clunk
        }

        /// <summary>Where the driver gets out (right-hand drive: the offside), clear of the body and behind the open door.</summary>
        public Vector3 ExitPoint
        {
            get
            {
                FindDoor(); float side = DoorSide;
                float z = doorHinge != null ? transform.InverseTransformPoint(doorHinge.position).z - doorLen * 0.4f - 0.35f : 0.2f;
                return transform.position + transform.right * side * (spec.m.hw + 0.5f) + transform.forward * z;
            }
        }
    }

    /// <summary>A proper two-tone car horn, made on the fly.</summary>
    public class Horn : MonoBehaviour
    {
        static AudioClip clip; AudioSource src;
        public void Toot()
        {
            if (src == null) { src = gameObject.AddComponent<AudioSource>(); src.spatialBlend = 1f; src.minDistance = 6f; src.maxDistance = 90f; src.volume = 0.6f; }
            if (src.isPlaying) return;
            if (clip == null)
            {
                int sr = 22050, n = (int)(sr * 0.45f); var d = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)sr, env = Mathf.Clamp01(t * 60f) * Mathf.Clamp01((0.45f - t) * 30f);
                    float s = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 415f * t)) * 0.5f + Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 494f * t)) * 0.5f;
                    d[i] = s * 0.22f * env;
                }
                clip = AudioClip.Create("horn", n, 1, sr, false); clip.SetData(d, 0);
            }
            src.clip = clip; src.Play();
        }
    }
}
