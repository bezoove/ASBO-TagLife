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
    /// The player's car. FORWARD to go (drag up on it to boost), BRAKE slows you and, once you've stopped, reverses;
    /// HANDBRAKE locks the back wheels so the rear steps out and you skid round; LEFT / RIGHT steer. Quick and arcadey:
    /// lots of pace both ways and a tight turning circle, steering that stays sharp at speed. Walls and other cars stop
    /// it (a real rigidbody); kerbs and low edges it rides over, following the ground.
    /// </summary>
    public class PlayerCar : MonoBehaviour, IVehicle
    {
        // pace: about 70 mph flat out, 90 with the boost; 0-60 in under 4 s; a quick reverse
        const float Top = 31f, BoostTop = 40f, BoostRamp = 1.6f, Acc = 8.5f, RevTop = 12f, RevAcc = 7f, BrakeDec = 17f, HandDec = 5.5f, Lock = 0.68f, LatCap = 24f;

        public bool driven;
        Rigidbody rb; Transform vis; Transform[] wheels; CarBody.Spec spec;
        float sp, steer, steerHold, bst, fwdT, revT, wheelSpin, bRoll, bPitch, acc, crunchT, yTarget, slip;
        Vector3 moveDir = Vector3.forward; bool reversing;
        AudioSource eng, skid; bool realEngine;
        public float Slip => slip;
        public float Speed => Mathf.Abs(sp);
        public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
        public float SignedSpeed => sp;
        public static PlayerCar I;

        public static PlayerCar Create(Vector3 pos, float yaw, Color paint, string model = "hatch_a")
        {
            var g = new GameObject("PlayerCar"); g.layer = Person.VehicleLayer; // ragdolls pass through cars; our own ground probe skips it
            g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var c = g.AddComponent<PlayerCar>(); c.Build(model, paint); I = c;
            return c;
        }

        void Build(string model, Color paint)
        {
            spec = CarBody.Get(model);
            vis = new GameObject("Body").transform; vis.SetParent(transform, false);
            wheels = CarBody.Build(vis, spec, paint, true, 4242, false);
            float L = spec.m.L, W = spec.m.W, H = spec.van ? 2.0f : 1.42f;
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.32f + (H - 0.32f) * 0.5f, 0f); col.size = new Vector3(W * 0.98f, H - 0.32f, L * 0.98f);
            col.sharedMaterial = new PhysicsMaterial("CarSlide") { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0f };
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = spec.van ? 1900f : 1150f; rb.useGravity = false; rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.linearDamping = 0f; rb.angularDamping = 0f;
            yTarget = transform.position.y;
            var ec = SfxBank.Pick("engine"); realEngine = ec != null;
            eng = gameObject.AddComponent<AudioSource>(); eng.clip = realEngine ? ec : AudioSynth.Engine(); eng.loop = true; eng.spatialBlend = 1f; eng.minDistance = 5f; eng.maxDistance = 70f; eng.volume = 0.12f; eng.Play();
            gameObject.AddComponent<Horn>();
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
            if (steerD != 0f) steerHold = Mathf.Clamp(steerHold + steerD * dt * (steerHold != 0f && Mathf.Sign(steerD) != Mathf.Sign(steerHold) ? 12f : 6f), -1f, 1f);
            else steerHold = Damp(steerHold, 0f, 12f, dt);
            c.steer = steerA.HasValue && steerD == 0f ? steerA.Value : steerHold; c.steerRaw = steerD;
            if (!driven) c.brakeRev = 0f;
            return c;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var c = Read(dt);
            // what the physics actually let us do (a wall or another car takes speed off)
            Vector3 fwd = rb.rotation * Vector3.forward;
            float actual = Vector3.Dot(rb.linearVelocity.WithY(0f), moveDir); // how fast we really went the way we were going
            if (Mathf.Abs(sp) > 1f && actual < Mathf.Abs(sp) * 0.6f)
            {
                float into = Mathf.Abs(sp) - Mathf.Max(0f, actual);
                if (into > 2.4f && Time.time > crunchT && GameAudio.I != null) { crunchT = Time.time + 0.4f; GameAudio.I.CarCrash(rb.position + moveDir * spec.m.Le, into); }
                sp = Mathf.Sign(sp) * Mathf.Max(0f, actual) * (into > 6f ? -0.12f : 0.3f);
            }
            // drive: forward pedal; brake that turns into reverse once you've stopped (let go and press again, or just hold)
            bool boost = c.boost && c.throttle > 0.5f && sp > 1f;
            bst = Mathf.Clamp01(bst + (boost ? dt / BoostRamp : -dt / 1.2f));
            float top = Mathf.Lerp(Top, BoostTop, bst);
            float a = 0f;
            if (!driven) { reversing = false; }
            if (c.brakeRev > 0.02f && sp < 0.6f && !(c.throttle > 0.02f)) reversing = true;
            if (c.brakeRev <= 0.02f && sp > -0.2f) reversing = false;
            if (c.throttle > 0.02f)
            {
                reversing = false;
                if (sp >= -0.3f) a += c.throttle * Acc * (1f + 0.35f * bst) * Mathf.Clamp(1f - Mathf.Pow(Mathf.Max(0f, sp) / top, 2f), -0.5f, 1f);
                else a += BrakeDec; // going backwards: forward pedal stops you first
            }
            if (c.brakeRev > 0.02f)
            {
                if (sp > 0.3f) a -= c.brakeRev * BrakeDec;
                else if (reversing) a -= c.brakeRev * RevAcc * Mathf.Clamp(1f - Mathf.Pow(Mathf.Max(0f, -sp) / RevTop, 2f), -0.5f, 1f);
            }
            a -= (0.25f + 0.0022f * sp * sp) * Mathf.Sign(sp) * (sp > 0f ? 1f - Mathf.Clamp01(c.throttle) : reversing ? 0f : 1f); // rolling resistance and air
            if (!(c.throttle > 0.02f) && !(c.brakeRev > 0.02f)) a -= 0.8f * Mathf.Sign(sp);
            bool handOn = c.hand > 0.5f;
            if (handOn) a -= Mathf.Sign(sp) * HandDec * Mathf.Clamp01(Mathf.Abs(sp) * 2f);
            float sp0 = sp; sp += a * dt;
            if (sp0 * sp < 0f && !(reversing && sp < 0f)) sp = 0f;
            if (Mathf.Abs(sp) < 0.04f && !(c.throttle > 0.02f) && !(c.brakeRev > 0.02f)) sp = 0f;
            acc = (sp - sp0) / dt;
            // steering: a tight lock at low speed, and as much as the tyres will hold at speed (no huge circles)
            float v = Mathf.Abs(sp);
            float smax = Mathf.Min(Lock, Mathf.Atan(spec.m.wb * LatCap / Mathf.Max(v * v, 0.01f)));
            if (handOn && v > 4f) smax = Mathf.Min(Lock, smax * 1.9f + 0.08f); // handbrake: the back steps out
            steer = Damp(steer, Mathf.Clamp(c.steer, -1f, 1f) * smax, 14f, dt);
            float yawRate = sp * Mathf.Tan(steer) / spec.m.wb;
            // grip: the way we're actually moving catches up with the way we're pointing; on the handbrake it barely does (the skid)
            Vector3 heading = fwd * Mathf.Sign(sp == 0f ? 1f : sp);
            float grip = handOn && v > 4f ? 1.6f : 10f;
            moveDir = Vector3.Slerp(moveDir.sqrMagnitude > 0.5f ? moveDir : heading, heading, 1f - Mathf.Exp(-grip * dt)).normalized;
            slip = Vector3.Angle(moveDir, heading) * Mathf.Clamp01(v / 6f);
            if (handOn && v > 4f) sp -= Mathf.Sign(sp) * slip * 0.02f * dt * v; // sliding scrubs speed
            // ride over kerbs and slopes: hover the body on the ground under it
            Vector3 p = rb.position;
            if (Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out var gh, 3f, ~((1 << 2) | (1 << 31) | (1 << Person.VehicleLayer) | (1 << Person.RagdollLayer)), QueryTriggerInteraction.Ignore)) yTarget = gh.point.y;
            float vy = (yTarget - p.y) / Mathf.Max(dt, 1e-4f) * 0.35f;
            rb.linearVelocity = moveDir * Mathf.Abs(sp) + Vector3.up * Mathf.Clamp(vy, -6f, 4f);
            rb.angularVelocity = new Vector3(0f, yawRate, 0f);
            // body lean and nose dip, wheels, engine
            bRoll = Damp(bRoll, Mathf.Clamp(sp * yawRate * 0.006f, -0.035f, 0.035f), 5f, dt);
            bPitch = Damp(bPitch, Mathf.Clamp(acc * 0.0035f, -0.03f, 0.022f), 6f, dt);
            if (c.horn) GetComponent<Horn>().Toot();
        }

        void Update()
        {
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

        /// <summary>Where the driver gets out (right-hand drive: the offside), clear of the body.</summary>
        public Vector3 ExitPoint => transform.position + transform.right * (spec.m.hw + 0.55f) + transform.forward * 0.2f;
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
