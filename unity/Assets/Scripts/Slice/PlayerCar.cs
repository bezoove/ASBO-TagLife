using UnityEngine;
using UnityEngine.InputSystem;

namespace ASBO.Slice
{
    /// <summary>Touch buttons for driving (set by the on-screen controls, read by PlayerCar).</summary>
    public static class TouchCar
    {
        public static bool fwd, rev, brake, left, right, boost, horn;
        public static void Clear() { fwd = rev = brake = left = right = boost = horn = false; }
    }

    /// <summary>
    /// The player's car, driven the way the original game drove: hold Forward and the longer you hold it the faster you
    /// go (drag up on it to boost), Reverse backs up, Brake is a handbrake, and Left / Right buttons ease the steering in
    /// and let it straighten up by itself. Arcade handling ported from the old game's CAR_PHYS: no gears, no wheelspin,
    /// steering that tightens at low speed and calms down at speed. Walls and other cars stop it (it's a real rigidbody);
    /// kerbs and low edges it rides over, following the ground.
    /// </summary>
    public class PlayerCar : MonoBehaviour, IVehicle
    {
        // the old game's numbers
        const float Top = 15.5f, BoostTop = 26f, BoostRamp = 2.2f, Acc = 3.4f, RevTop = 4.5f, RevAcc = 2.4f, BrakeDec = 9.5f, Hand = 7.5f, SteerLo = 0.6f, SteerHi = 0.11f;

        public bool driven;
        Rigidbody rb; Transform vis; Transform[] wheels; CarBody.Spec spec;
        float sp, steer, steerHold, bst, fwdT, revT, wheelSpin, bRoll, bPitch, acc, crunchT, yTarget;
        AudioSource eng; bool realEngine;
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
        }

        struct Ctl { public float throttle, reverse, brake, steer, steerRaw; public bool boost, horn; }

        static float Damp(float a, float b, float k, float dt) => Mathf.Lerp(a, b, 1f - Mathf.Exp(-k * dt));

        Ctl Read(float dt)
        {
            var c = new Ctl();
            bool fwdD = false, revD = false; float steerD = 0f, padT = 0f, padR = 0f; float? steerA = null;
            if (driven)
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    fwdD |= kb.wKey.isPressed || kb.upArrowKey.isPressed; revD |= kb.sKey.isPressed || kb.downArrowKey.isPressed;
                    if (kb.spaceKey.isPressed) c.brake = 1f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steerD -= 1f; if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steerD += 1f;
                    c.boost |= kb.leftShiftKey.isPressed; c.horn |= kb.hKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame;
                }
                fwdD |= TouchCar.fwd; revD |= TouchCar.rev; if (TouchCar.brake) c.brake = 1f; c.boost |= TouchCar.boost;
                if (TouchCar.left) steerD -= 1f; if (TouchCar.right) steerD += 1f;
                if (TouchCar.horn) { c.horn = true; TouchCar.horn = false; }
                foreach (var pad in Gamepad.all)
                {
                    if (pad.name.Contains("OnScreen") || pad.description.deviceClass == "OnScreen") continue;
                    float ax = pad.leftStick.ReadValue().x; if (Mathf.Abs(ax) > 0.15f) steerA = Mathf.Clamp((ax - Mathf.Sign(ax) * 0.15f) / 0.85f, -1f, 1f);
                    padT = Mathf.Max(padT, pad.rightTrigger.ReadValue(), pad.buttonSouth.isPressed ? 1f : 0f);
                    padR = Mathf.Max(padR, pad.leftTrigger.ReadValue());
                    if (pad.buttonNorth.isPressed) c.brake = 1f;
                    c.boost |= pad.rightShoulder.isPressed; c.horn |= pad.buttonWest.wasPressedThisFrame;
                }
            }
            fwdT = fwdD ? fwdT + dt : 0f; revT = revD ? revT + dt : 0f;
            c.throttle = fwdD ? Mathf.Clamp01(0.2f + 0.8f * fwdT / 1.5f) : 0f; if (fwdD && c.boost) c.throttle = 1f;
            c.reverse = revD ? Mathf.Clamp01(0.3f + 0.7f * revT / 1.2f) : 0f;
            c.throttle = Mathf.Max(c.throttle, padT); c.reverse = Mathf.Max(c.reverse, padR);
            // steering buttons ease in (faster when swapping sides) and let go back to straight
            if (steerD != 0f) steerHold = Mathf.Clamp(steerHold + steerD * dt * (steerHold != 0f && Mathf.Sign(steerD) != Mathf.Sign(steerHold) ? 6f : 2.6f), -1f, 1f);
            else steerHold = Damp(steerHold, 0f, 9f, dt);
            c.steer = steerA.HasValue && steerD == 0f ? steerA.Value : steerHold; c.steerRaw = steerD;
            if (!driven) c.brake = 1f;
            return c;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var c = Read(dt);
            // what the physics actually let us do (a wall or another car takes speed off)
            Vector3 fwd = rb.rotation * Vector3.forward;
            float actual = Vector3.Dot(rb.linearVelocity, fwd);
            if (Mathf.Abs(sp) > 1f && Mathf.Abs(actual) < Mathf.Abs(sp) * 0.6f)
            {
                float into = Mathf.Abs(sp) - Mathf.Abs(actual);
                if (into > 2.4f && Time.time > crunchT && GameAudio.I != null) { crunchT = Time.time + 0.4f; GameAudio.I.CarCrash(rb.position + fwd * Mathf.Sign(sp) * spec.m.Le, into); }
                sp = actual * (into > 6f ? -0.12f : 0.3f);
            }
            // the old game's drive model
            bool boost = c.boost && c.throttle > 0.5f && sp > 1f;
            bst = Mathf.Clamp01(bst + (boost ? dt / BoostRamp : -dt / 1.5f));
            float top = Mathf.Lerp(Top, BoostTop, bst);
            float a = 0f;
            if (c.throttle > 0.02f) a += sp >= -0.3f ? c.throttle * Acc * (1f + 0.45f * bst) * Mathf.Clamp(1f - Mathf.Pow(Mathf.Max(0f, sp) / top, 2f), -0.5f, 1f) * (sp < 2f ? 1.15f : 1f) : BrakeDec * 0.8f;
            if (c.reverse > 0.02f) { if (sp > 0.3f) a -= c.reverse * BrakeDec * 0.85f; else if (!(c.throttle > 0.02f)) a -= c.reverse * RevAcc * Mathf.Clamp(1f - Mathf.Pow(Mathf.Max(0f, -sp) / RevTop, 2f), -0.5f, 1f); }
            a -= (0.22f + 0.0035f * sp * sp) * Mathf.Sign(sp) * (sp > 0f ? 1f - Mathf.Clamp01(c.throttle) : 1f);
            if (!(c.throttle > 0.02f) && !(c.reverse > 0.02f)) a -= 0.55f * Mathf.Sign(sp);
            if (c.brake > 0.02f) a -= Mathf.Sign(sp) * Hand * c.brake * Mathf.Clamp01(Mathf.Abs(sp) * 3f);
            float sp0 = sp; sp += a * dt;
            if (sp0 * sp < 0f) sp = 0f;
            if (Mathf.Abs(sp) < 0.04f && !(c.throttle > 0.02f) && !(c.reverse > 0.02f)) sp = 0f;
            acc = (sp - sp0) / dt;
            float smax = Mathf.Lerp(SteerLo, SteerHi, Mathf.Clamp01(Mathf.Abs(sp) / 15f)) * Mathf.Min(1f, Mathf.Pow(15f / Mathf.Max(15f, Mathf.Abs(sp)), 2f));
            steer = Damp(steer, Mathf.Clamp(c.steer, -1f, 1f) * smax, 7f, dt);
            float slide = c.brake > 0.5f && Mathf.Abs(sp) > 5f ? 1.35f : 1f;
            float yawRate = sp * Mathf.Tan(steer) / spec.m.wb * slide;
            // ride over kerbs and slopes: hover the body on the ground under it
            Vector3 p = rb.position;
            if (Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out var gh, 3f, ~((1 << 2) | (1 << 31) | (1 << Person.VehicleLayer) | (1 << Person.RagdollLayer)), QueryTriggerInteraction.Ignore)) yTarget = gh.point.y;
            float vy = (yTarget - p.y) / Mathf.Max(dt, 1e-4f) * 0.35f;
            rb.linearVelocity = fwd * sp + Vector3.up * Mathf.Clamp(vy, -6f, 4f);
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
            if (eng != null)
            {
                float rev = realEngine ? 0.95f + Mathf.Min(Mathf.Abs(sp), 26f) * 0.045f + (driven && fwdT > 0f ? 0.15f : 0f) : 0.6f + Mathf.Abs(sp) * 0.05f;
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
