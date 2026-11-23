using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace ASBO.Slice
{
    /// <summary>
    /// Third-person player. Velocity is smoothed like a person (quick to stop, slower to sprint) and the body
    /// turns along a smooth arc; the animator gets the true local-space velocity, so the 8-direction mocap blend
    /// leans and cross-steps through turns and the feet match the ground. Big direction changes from a standstill
    /// play a turn-on-the-spot clip. Default pace is a jog; Shift sprints, Ctrl walks.
    /// Vehicles: the old procedural Car or an RCC (Realistic Car Controller) car.
    /// </summary>
    public class PlayerCtl : MonoBehaviour
    {
        public Car car; RccVehicle rcc;
        [System.NonSerialized] public string scripted; float scriptT;
        [System.NonSerialized] public float showSpeed = -1f, showYaw = float.NaN, showRadius = -1f, showPitch = float.NaN;
        CharacterController cc; Person person; Camera cam;
        CinemachineCamera vcam; CinemachineOrbitalFollow orbit; Transform camTarget;
        float yaw, pitch = 12f, vy, bodyYaw, yawVel, turnLeft, turnTimer; Vector3 vel, velRef;
        bool inCar, spectate; int shotN;
        LocoSpeeds sp = new LocoSpeeds();

        PlayerCar PCar => PlayerCar.I;
        Transform VehT => PCar != null ? PCar.transform : rcc != null ? rcc.transform : car != null ? car.transform : null;
        float VehSpeed => PCar != null ? PCar.Speed : rcc != null ? rcc.Speed : car != null ? car.Speed : 0f;
        Vector3 VehVel => PCar != null ? PCar.Velocity : rcc != null ? rcc.Velocity : car != null ? car.Velocity : Vector3.zero;

        public void Setup(Car c)
        {
            car = c;
            rcc = FindFirstObjectByType<RccVehicle>();
            cc = gameObject.AddComponent<CharacterController>();
            // the collision capsule stops short of the top of the head: old cottage and pub doorways are under 2 m and their
            // floors sit a little proud, so a full-height capsule caught the lintels and you couldn't get through
            cc.height = 1.6f; cc.radius = 0.28f; cc.center = new Vector3(0, 0.81f, 0); cc.stepOffset = 0.35f; cc.slopeLimit = 50f; cc.skinWidth = 0.03f;
            person = GetComponent<Person>();
            if (FreePackTest.On) sp = UmaBody.Speeds(true);
            else sp = new LocoSpeeds { walk = 1.55f, run = 3.95f, sprint = 5.0f };
            cam = Camera.main; if (cam == null) { var g = new GameObject("Main Camera"); cam = g.AddComponent<Camera>(); g.tag = "MainCamera"; }
            cam.nearClipPlane = 0.08f; cam.farClipPlane = 600f;
            if (cam.GetComponent<CinemachineBrain>() == null) cam.gameObject.AddComponent<CinemachineBrain>();
            camTarget = new GameObject("CamTarget").transform; camTarget.SetParent(transform.parent, false);
            var vg = new GameObject("PlayerCam"); vg.transform.SetParent(transform.parent, false);
            vcam = vg.AddComponent<CinemachineCamera>();
            vcam.Lens = new LensSettings { FieldOfView = 55f, NearClipPlane = 0.08f, FarClipPlane = 600f };
            vcam.Follow = camTarget; vcam.LookAt = camTarget;
            orbit = vg.AddComponent<CinemachineOrbitalFollow>();
            orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbit.Radius = 4.2f;
            orbit.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
            // no follow lag of its own: our target is already smoothed on foot and locked to the car when driving,
            // so the view stays the same distance at any speed (it used to trail off behind at speed)
            orbit.TrackerSettings.PositionDamping = Vector3.zero; orbit.TrackerSettings.RotationDamping = Vector3.zero;
            var comp = vg.AddComponent<CinemachineRotationComposer>();
            comp.Composition.ScreenPosition = new Vector2(0f, 0.05f); comp.Damping = Vector2.zero;
            var deocc = vg.AddComponent<CinemachineDeoccluder>();
            deocc.CollideAgainst = ~((1 << 2) | (1 << Person.VehicleLayer) | (1 << Person.RagdollLayer)); // cars and bodies don't shove the camera about
            deocc.MinimumDistanceFromTarget = 0.25f;
            var av = deocc.AvoidObstacles; av.Enabled = true; av.CameraRadius = 0.12f;
            av.MinimumOcclusionTime = 0.25f; // a lamp post or a tree flicking past doesn't make it jump in and out
            av.DampingWhenOccluded = 0.15f; av.Damping = 0.35f;
            deocc.AvoidObstacles = av;
            touch = TouchControls.Wanted;
            if (touch) TouchControls.Create(this);
            else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            yaw = bodyYaw = transform.eulerAngles.y;
        }

        /// <summary>Get in or out of the nearest car (the car button).</summary>
        public void UseCar() => ToggleCar();

        public void Teleport(Vector3 pos, float yawDeg, float pitchDeg = 12f)
        {
            if (inCar) ToggleCar();
            cc.enabled = false; transform.position = pos; transform.rotation = Quaternion.Euler(0, yawDeg, 0); cc.enabled = true;
            yaw = bodyYaw = yawDeg; pitch = pitchDeg; vy = 0f; vel = Vector3.zero;
            camTarget.position = pos + Vector3.up * 1.55f;
            if (vcam != null) vcam.PreviousStateIsValid = false;
        }

        bool wasGrounded = true; float fallSpeed; Fighter fighter; bool lockedThisFrame, clickLock, facing; float lookT, holdR = 1.25f;
        public bool InCar => inCar; bool touch;
        Vector2 lookAdd;
        /// <summary>Camera turn from touch drags (degrees).</summary>
        public void AddLook(Vector2 deg) { lookAdd += deg; }

        Renderer[] hidden;
        void SetVisible(bool v) { hidden = null; foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = v; }

        // walking into a bin or a gnome barges it along
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.rigidbody == null || hit.rigidbody.isKinematic || hit.moveDirection.y < -0.3f) return;
            var k = hit.collider.GetComponentInParent<Knockable>();
            if (k != null) k.Nudge(hit.moveDirection, cc.velocity.magnitude);
        }

        void Update()
        {
            var kb = Keyboard.current; var ms = Mouse.current; var gp = Gamepad.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (ASBO.Casino.CasinoUI.Open) { vel = Vector3.zero; person.SetMove(Vector2.zero); UpdateCamera(); return; } // sat at a casino game
            lockedThisFrame = false;
            if (!touch && ms != null && ms.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; lockedThisFrame = true; }
            if (ms != null && ms.leftButton.isPressed && lockedThisFrame == false && clickLock) lockedThisFrame = true;
            if (ms != null && !ms.leftButton.isPressed) clickLock = false; else if (lockedThisFrame) clickLock = true;
            if (!touch && ms != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 d = ms.delta.ReadValue() * 0.12f;
                yaw += d.x; pitch = Mathf.Clamp(pitch - d.y, -25f, 60f);
                if (d.sqrMagnitude > 0.25f) lookT = Time.time;
            }
            // touch drags and the gamepad's right stick turn the camera too
            if (lookAdd != Vector2.zero) { lookT = Time.time; yaw += lookAdd.x; pitch = Mathf.Clamp(pitch - lookAdd.y, -25f, 60f); lookAdd = Vector2.zero; }
            if (gp != null) { var rs = gp.rightStick.ReadValue(); if (rs.sqrMagnitude > 0.02f) { lookT = Time.time; yaw += rs.x * 150f * Time.deltaTime; pitch = Mathf.Clamp(pitch - rs.y * 90f * Time.deltaTime, -25f, 60f); } }
            if ((kb != null && kb.eKey.wasPressedThisFrame) || (gp != null && gp.selectButton.wasPressedThisFrame)) ToggleCar();
            // talk to whoever's in front of you, or answer whoever came over: T / pad Y / TALK
            if (!inCar && Social.I != null && ((kb != null && kb.tKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame))) Social.I.TalkPressed();
            if (chairSat)
            {
                // sat on a stool, a chair, a sofa: stay put until you push the stick (or press TALK)
                person.SetMove(Vector2.zero); vel = Vector3.zero; transform.SetPositionAndRotation(chairPos, Quaternion.Euler(0, chairYaw, 0)); bodyYaw = chairYaw;
                Vector2 mv = kb == null ? Vector2.zero : new Vector2((kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0), (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0));
                if (gp != null && gp.leftStick.ReadValue().sqrMagnitude > 0.25f) mv = Vector2.one;
                if (FloatStick.Active && FloatStick.Value.sqrMagnitude > 0.25f) mv = Vector2.one;
                if (mv.sqrMagnitude > 0.1f && Time.time - chairT > 0.6f) StandUp();
                UpdateCamera(); return;
            }
            if (kb != null && kb.vKey.wasPressedThisFrame) spectate = !spectate;
            if (kb != null && kb.f9Key.wasPressedThisFrame)
            {
                string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots"); System.IO.Directory.CreateDirectory(dir);
                ScreenCapture.CaptureScreenshot(dir + "/shot_" + (shotN++) + ".png");
            }
            if (inCar && VehT != null)
            {
                if (seated) Sit();
                else
                {
                    transform.position = VehT.position + Vector3.up * 0.2f; transform.rotation = VehT.rotation;
                    // stay hidden while driving (the character system can switch renderers back on when it rebuilds)
                    if (hidden == null) hidden = GetComponentsInChildren<Renderer>(true);
                    foreach (var r in hidden) if (r != null && r.enabled) r.enabled = false;
                }
            }
            else if (person != null && person.state != Person.State.Walking) { vel = Vector3.zero; bodyYaw = transform.eulerAngles.y; } // floored: wait to get up
            else
            {
                // fists and feet. Punch: F / left click / pad X (tap: jab, cross, hook, body; hold: overhand). Kick: Q / pad B.
                // Shove: C / pad RB. Fists up: X / pad d-pad up.
                fighter ??= GetComponent<Fighter>();
                if (fighter != null && showSpeed < 0f)
                {
                    bool locked = !touch && ms != null && Cursor.lockState == CursorLockMode.Locked;
                    bool punchHeld = (kb != null && kb.fKey.isPressed) || (gp != null && gp.buttonWest.isPressed) || (locked && ms.leftButton.isPressed && !lockedThisFrame);
                    bool kick = (kb != null && kb.qKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame);
                    bool shove = (kb != null && kb.cKey.wasPressedThisFrame) || (gp != null && gp.rightShoulder.wasPressedThisFrame);
                    const bool guardHeld = false; // no guard button: fights are about hitting, not hiding
                    bool fists = (kb != null && kb.xKey.wasPressedThisFrame) || (gp != null && gp.dpad.up.wasPressedThisFrame);
                    bool can = !inCar && cc.isGrounded && turnTimer <= 0f;
                    fighter.PlayerInput(punchHeld, kick, shove, guardHeld, fists, can);
                    if (fighter.Busy) bodyYaw = transform.eulerAngles.y;
                }
                Move(kb);
            }
            UpdateCamera();
        }

        void Move(Keyboard kb)
        {
            if (showSpeed >= 0f)
            {
                // showcase: play a gait on the spot (blend tree lands exactly on that clip)
                vel = Vector3.zero; transform.rotation = Quaternion.Euler(0, bodyYaw, 0);
                person.SetMove(new Vector2(0f, showSpeed)); person.SetGrounded(true);
                return;
            }
            Quaternion camYaw = Quaternion.Euler(0, yaw, 0);
            Vector2 inp = kb == null ? Vector2.zero : new Vector2((kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0), (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0));
            bool fakeSprint = false;
            if (!string.IsNullOrEmpty(scripted))
            {
                // test pattern: jog away from camera, turn right, sprint, stop, turn round
                scriptT += Time.deltaTime; float t = scriptT;
                if (t < 2.2f) inp = new Vector2(0, 1);
                else if (t < 3.6f) inp = new Vector2(1, 0);
                else if (t < 5.2f) { inp = new Vector2(1, 0); fakeSprint = true; }
                else if (t < 6.4f) inp = Vector2.zero;
                else if (t < 8.4f) inp = new Vector2(-1, 0);
                else if (t < 9.6f) inp = Vector2.zero;
                else { scripted = null; scriptT = 0f; }
            }
            if (TestInput.HasValue) inp = TestInput.Value; // tests drive the player like a stick would
            var pad = Gamepad.current; if (pad != null && pad.leftStick.ReadValue().sqrMagnitude > 0.04f) inp = pad.leftStick.ReadValue();
            bool touchStick = FloatStick.Active; if (touchStick) inp = FloatStick.Value; // the floating thumb stick
            float mag = Mathf.Clamp01(inp.magnitude);
            Vector3 dir = camYaw * new Vector3(inp.x, 0, inp.y); if (dir.sqrMagnitude > 1e-4f) dir.Normalize();
            // crouch (Z / pad right stick click / the touch button): a slow, quiet sneak; stand up again only where there's headroom
            if ((kb != null && kb.zKey.wasPressedThisFrame) || (pad != null && pad.rightStickButton.wasPressedThisFrame) || crouchTap) { crouchTap = false; SetCrouch(!crouched); }
            bool sprint = !crouched && (fakeSprint || (kb != null && kb.leftShiftKey.isPressed) || (pad != null && pad.leftStickButton.isPressed) || touchStick && FloatStick.Sprint);
            bool walk = (kb != null && kb.leftCtrlKey.isPressed) || (mag > 0.05f && mag < (touchStick ? FloatStick.RunAt : 0.5f));
            float pace = sprint ? sp.sprint : walk ? sp.walk * (touchStick ? Mathf.Lerp(0.45f, 1f, mag / FloatStick.RunAt) : 1f) : sp.run; // a gentle push ambles, more walks briskly
            if (crouched) pace = Mathf.Min(pace, 1.6f); // up to the quick hunched sneak (the crouch blend matches its clips to the speed)
            pace *= Drugs.SpeedScale; // whack slows you right down, fizz speeds you up
            if (person.hurt) pace = person.HurtSpeed;
            if (fighter != null)
            {
                fighter.Steer = mag < 0.05f ? Vector3.zero : dir * mag;
                var tg = fighter.Target;
                // sprinting away from the one you're squared up to: you're legging it, out of the fight
                if (tg != null && sprint && mag > 0.3f && Vector3.Dot(dir, (tg.transform.position - transform.position).WithY(0).normalized) < -0.2f) fighter.RunOff();
                else if (tg != null && !sprint) pace = Mathf.Min(pace, sp.walk * 1.1f); // footwork, not jogging round them
            }
            Vector3 want = mag < 0.05f ? Vector3.zero : dir * pace;
            if (fighter != null)
            {
                want *= fighter.SpeedMul;
                // in a scrap (and not running away), keep facing whoever you're squared up to
                var foe = fighter.Target != null && !sprint ? fighter.Target : fighter.Busy ? fighter.NearestFoe() : null;
                if (foe != null && turnTimer <= 0f) { Vector3 fd = (foe.transform.position - transform.position).WithY(0); if (fd.sqrMagnitude > 0.01f) { bodyYaw = Mathf.MoveTowardsAngle(bodyYaw, Mathf.Atan2(fd.x, fd.z) * Mathf.Rad2Deg, 540f * Time.deltaTime); facing = true; } }
                else facing = false;
                if (fighter.Target != null && !sprint && !fighter.Down) want = Circle(fighter.Target, want);
            }
            bool grounded = cc.isGrounded;

            // turn on the spot: from (nearly) standing, a big change of direction plays the turn clip
            if (turnTimer > 0f)
            {
                turnTimer -= Time.deltaTime;
                float step = turnLeft / sp.turnSecs * Time.deltaTime;
                bodyYaw += step;
                want = Vector3.zero;
            }
            else if (want.sqrMagnitude > 0.01f && vel.magnitude < 0.4f && grounded && !facing)
            {
                float delta = Mathf.DeltaAngle(bodyYaw, Mathf.Atan2(want.x, want.z) * Mathf.Rad2Deg);
                if (Mathf.Abs(delta) > 75f && FreePackTest.On)
                {
                    turnLeft = Mathf.Clamp(delta, -100f, 100f); turnTimer = sp.turnSecs * 0.8f;
                    person.TurnInPlace(delta < 0f);
                    want = Vector3.zero;
                }
            }

            if (landLock > 0f) { landLock -= Time.deltaTime; want = Vector3.zero; }
            // pulling up from a run: the braking strides, and a stop that takes them (not a dead stop under a sliding body)
            var pp = Params();
            bool stopping = want.sqrMagnitude < 0.01f && vel.magnitude > 3.2f && !crouched && (fighter == null || fighter.Target == null);
            if (stopping && !runStopping && pp.runStop) { person.anim.SetTrigger("RunStop"); runStopping = true; }
            if (want.sqrMagnitude > 0.01f || vel.magnitude < 0.2f) runStopping = false;
            if (pp.stick) person.anim.SetBool("Stick", want.sqrMagnitude > 0.01f);
            // human-like acceleration: quicker to stop than to get going, sprint builds up
            float accTime = want.sqrMagnitude < vel.sqrMagnitude ? (runStopping ? 0.3f : 0.16f) : (vel.magnitude < sp.walk ? 0.22f : 0.38f);
            vel = Vector3.SmoothDamp(vel, want, ref velRef, accTime);
            if (want == Vector3.zero && vel.magnitude < 0.05f) vel = Vector3.zero;

            // body follows the travel direction along a smooth arc (tighter when slow); squared up in a fight, it strafes instead
            if (vel.sqrMagnitude > 0.04f && turnTimer <= 0f && !facing)
            {
                float target = Mathf.Atan2((want.sqrMagnitude > 0.01f ? want : vel).x, (want.sqrMagnitude > 0.01f ? want : vel).z) * Mathf.Rad2Deg;
                float t = Mathf.Lerp(0.07f, 0.2f, vel.magnitude / Mathf.Max(sp.sprint, 0.1f));
                bodyYaw = Mathf.SmoothDampAngle(bodyYaw, target, ref yawVel, t);
            }
            transform.rotation = Quaternion.Euler(0, bodyYaw, 0);

            if (grounded && !wasGrounded && fallSpeed < -3.5f && GameAudio.I != null) GameAudio.I.Land(transform.position);
            // a big drop (more than about a storey's fall): the landing buckles your knees and holds you a moment
            if (grounded && !wasGrounded && fallSpeed < -8.5f && person.anim != null && Params().hardLand) { person.anim.SetTrigger("HardLand"); landLock = 0.75f; }
            fallSpeed = vy; wasGrounded = grounded;
            if (grounded && vy < 0f) vy = -2f; else vy -= 19.6f * Time.deltaTime;
            if (grounded && turnTimer <= 0f && !climbing && ((kb != null && kb.spaceKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame) || jumpTap))
            {
                jumpTap = false;
                if (Ledge(out var top)) StartCoroutine(Climb(top));   // a wall or a fence in front, low enough: up and over onto it
                else if (!crouched) { vy = 4.6f; person.Jump(); }
            }
            if (climbing) return;
            cc.Move((vel + Vector3.up * vy) * Time.deltaTime);
            person.SetGrounded(cc.isGrounded);
            // in the air the feet are left to the jump (a short grace so stepping off a kerb doesn't flick them about)
            airT = cc.isGrounded ? 0f : airT + Time.deltaTime;
            fg ??= GetComponentInChildren<FootGround>(); if (fg != null) fg.airborne = airT > 0.06f || vy > 1f;

            // the animator gets the real local velocity (lateral part = the lean/cross-step while turning)
            Vector3 hv = cc.velocity; hv.y = 0f; if (hv.magnitude > vel.magnitude + 0.3f) hv = vel;
            Vector3 local = Quaternion.Inverse(transform.rotation) * hv;
            person.SetMove(new Vector2(local.x, local.z));
        }

        /// <summary>
        /// The fight circle: squared up to someone, a gentle pull keeps you at fighting distance (left alone you settle
        /// back to it), sideways takes you round them rather than off at a tangent, stepping in stops short of walking
        /// into them, and backing off is a touch heavier. Walk far enough away (or sprint off) and you're out of it.
        /// </summary>
        Vector3 Circle(Fighter tgt, Vector3 want)
        {
            Vector3 to = (tgt.transform.position - transform.position).WithY(0); float d = to.magnitude;
            if (d < 0.01f || d > 4f || tgt.Down) return want;
            const float ring = 1.25f, minGap = 0.62f;
            Vector3 n = to / d, tn = new Vector3(n.z, 0f, -n.x);
            float inR = Vector3.Dot(want, n), inT = Vector3.Dot(want, tn);
            if (inR > 0f) inR *= Mathf.Clamp01((d - minGap) / 0.35f); else inR *= 0.8f;
            Vector3 w = n * inR + tn * inT;
            if (Mathf.Abs(inT) > 0.2f && Mathf.Abs(inR) < Mathf.Abs(inT) * 0.35f) w += n * Mathf.Clamp((d - holdR) * 3f, -1.5f, 1.5f); // going round: hold the radius
            else holdR = Mathf.Clamp(d, minGap + 0.2f, 2.2f);
            if (want.sqrMagnitude < 0.01f && fighter.mode == Fighter.Mode.Idle && d < 2.6f) w += n * Mathf.Clamp((d - ring) * 1.3f, -0.55f, 0.55f); // the pull back to fighting distance
            if (d < minGap) w -= n * (minGap - d) * 6f; // never inside each other
            return w;
        }

        bool doorBusy;
        void ToggleCar()
        {
            if (doorBusy) return;
            if (!inCar)
            {
                // a car stopped in traffic, its driver's door right by you: you're having it
                var tc = TrafficCar.Takeable(transform.position, 2.6f, 3.5f);
                if (tc != null) { StartCoroutine(Carjack(tc)); return; }
                // the nearest car in reach: one already drivable, or a parked one (which becomes drivable as you get in)
                var near = PlayerCar.Nearest(transform.position, 4.5f);
                var parked = ParkedCar.Nearest(transform.position, 4.5f);
                if (parked != null && (near == null || (parked.transform.position - transform.position).sqrMagnitude < (near.transform.position - transform.position).sqrMagnitude)) near = parked.TakeOver();
                if (near != null) PlayerCar.I = near;
            }
            var vt = VehT; if (vt == null) return;
            if (!inCar) { if ((vt.position - transform.position).sqrMagnitude > 4.5f * 4.5f) return; StartCoroutine(GetIn(vt)); }
            else StartCoroutine(GetOut(vt));
        }

        /// <summary>
        /// Carjacking: the car stops, you yank the door and drag the driver out onto the road (they go down and get up
        /// raging or running), then it's yours: in you get and away.
        /// </summary>
        System.Collections.IEnumerator Carjack(TrafficCar tc)
        {
            doorBusy = true; tc.Hijack();
            Vector3 door = tc.DriverDoor; door.y = transform.position.y; Vector3 outw = tc.transform.right;
            // the driver: one kept ready out of sight (a new body takes a few seconds to put together), else made now
            var rng = new System.Random(tc.seed);
            Person victim = spare != null && spare.Ready ? spare : World.MakePerson(door + outw * 0.45f, CarBody.RandomPaint(rng), new Color(0.15f, 0.15f, 0.18f), "Driver");
            if (victim == spare) { spare = null; victim.transform.SetPositionAndRotation(door + outw * 0.45f + Vector3.down * 50f, Quaternion.LookRotation(outw)); }
            StartCoroutine(MakeSpare(6f));
            victim.transform.rotation = Quaternion.LookRotation(outw); LastCarjacked = victim;
            cc.enabled = false; transform.position = door - outw * 0.15f; bodyYaw = Mathf.Atan2(-outw.x, -outw.z) * Mathf.Rad2Deg; transform.rotation = Quaternion.Euler(0, bodyYaw, 0); cc.enabled = true; vel = Vector3.zero;
            if (person != null && person.anim != null && HasParam(person.anim, "Carjack")) person.anim.SetTrigger("Carjack");
            float until = Time.time + 4f; yield return new WaitForSeconds(0.8f);
            while (!victim.Ready && Time.time < until) yield return null;
            // out of the seat and down onto the road
            foreach (var t in tc.GetComponentsInChildren<Transform>(true)) if (t.name == "Driver") t.gameObject.SetActive(false);
            victim.transform.SetPositionAndRotation(door + outw * 0.45f, Quaternion.LookRotation(outw));
            if (victim.anim != null && HasParam(victim.anim, "PulledOut")) victim.anim.SetTrigger("PulledOut");
            yield return new WaitForSeconds(1.5f);
            if (victim != null) victim.Knockout(outw * 2.2f + Vector3.up * 0.4f, 5f, false);
            World.Toast("You drag the driver out onto the road.", 1);
            // the car's yours now
            var parent = tc.transform.parent; Vector3 pos = tc.transform.position + Vector3.up * 0.12f; float yaw0 = tc.transform.eulerAngles.y;
            var pc2 = PlayerCar.Create(pos, yaw0, tc.paint, tc.id, CarKit.PickWear(new System.Random(tc.seed), true), tc.seed, parent);
            Destroy(tc.gameObject);
            if (pc2 != null) { PlayerCar.I = pc2; yield return new WaitForSeconds(0.25f); doorBusy = false; StartCoroutine(GetIn(pc2.transform)); }
            else doorBusy = false;
        }

        /// <summary>Getting in: turn to the car, the door swings open, in you go, it shuts behind you.</summary>
        System.Collections.IEnumerator GetIn(Transform vt)
        {
            doorBusy = true;
            if (PCar != null)
            {
                PCar.Door(1f); Vector3 to = (PCar.ExitPoint - transform.position).WithY(0); if (to.sqrMagnitude > 0.04f) bodyYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                yield return new WaitForSeconds(0.32f);
            }
            inCar = true; cc.enabled = false;
            // you at the wheel: your own body sat in the driver's seat, hands on the wheel (cars without a seat: out of sight)
            seated = PCar != null && person != null && person.anim != null && HasParam(person.anim, "Seated");
            if (seated) { person.SetMove(Vector2.zero); person.anim.SetBool("Seated", true); fg ??= GetComponentInChildren<FootGround>(); if (fg != null) fg.off = true; Sit(); }
            else SetVisible(false);
            if (PCar != null) PCar.driven = true;
            else if (rcc != null && rcc.Car != null) { RCCP.RegisterPlayerVehicle(rcc.Car, true, true); rcc.driven = true; }
            else if (car != null) car.occupied = true;
            if (PCar != null) { yield return new WaitForSeconds(0.3f); PCar.Door(0f); }
            doorBusy = false;
        }

        /// <summary>
        /// Getting out: the door opens and you step out beside it, and it shuts. Do it while it's moving and you're thrown
        /// out: a stumble at a crawl, a tumble at town speeds, a proper splat going fast, hurt to match. The car rolls on
        /// without you.
        /// </summary>
        System.Collections.IEnumerator GetOut(Transform vt)
        {
            doorBusy = true;
            float spd = VehSpeed; Vector3 carVel = VehVel;
            if (PCar != null) { PCar.Door(1f); yield return new WaitForSeconds(spd > 2.5f ? 0.14f : 0.3f); spd = VehSpeed; carVel = VehVel; }
            inCar = false;
            if (seated) { seated = false; if (person != null && person.anim != null) person.anim.SetBool("Seated", false); fg ??= GetComponentInChildren<FootGround>(); if (fg != null) { fg.off = false; fg.sat = false; } }
            Vector3 exit;
            if (PCar != null) { PCar.driven = false; TouchCar.Clear(); exit = PCar.ExitPoint; }
            else if (rcc != null && rcc.Car != null) { rcc.driven = false; RCCP.SetControl(rcc.Car, false); RCCP.DeRegisterPlayerVehicle(); exit = vt.position - vt.right * 1.9f; }
            else { car.occupied = false; exit = car.exitPoint.position; }
            exit.y = vt.position.y; transform.position = exit; cc.enabled = true; SetVisible(true);
            bodyYaw = vt.eulerAngles.y; vel = Vector3.zero; transform.rotation = Quaternion.Euler(0, bodyYaw, 0);
            if (spd > 2.5f && person != null)
            {
                Vector3 side = PCar != null ? PCar.DoorSide * vt.right : vt.right;
                person.Bail(carVel * 0.9f + side * Mathf.Min(3f, 1f + spd * 0.08f), spd);
                if (fighter == null) fighter = GetComponent<Fighter>();
                if (fighter != null) fighter.Hurt(spd < 6f ? spd * 1.5f : spd < 13f ? 6f + spd * 2.6f : 20f + spd * 3.4f, spd > 15f);
                World.Toast(spd < 6f ? "You stumble out and go over." : spd < 13f ? "You throw yourself out and tumble down the road." : "You bail out at speed. The road is not kind.", 1);
            }
            if (PCar != null) { yield return new WaitForSeconds(spd > 2.5f ? 0.9f : 0.55f); PCar.Door(0f); }
            doorBusy = false;
        }

        // ------------------------------------------------------------------ crouching and climbing
        bool crouched, climbing; public static bool crouchTap, jumpTap;
        /// <summary>The last driver you dragged out of their car.</summary>
        [System.NonSerialized] public Person LastCarjacked;
        Person spare;
        /// <summary>A driver kept ready, out of sight below the town, so a carjack doesn't wait for a body to be made.</summary>
        System.Collections.IEnumerator MakeSpare(float after)
        {
            yield return new WaitForSeconds(after);
            if (spare != null) yield break;
            var r = new System.Random(Random.Range(0, 99999));
            spare = World.MakePerson(new Vector3(0f, -60f, 0f), CarBody.RandomPaint(r), new Color(0.15f, 0.15f, 0.18f), "Driver");
        }
        void OnEnable() { StartCoroutine(MakeSpare(8f)); }
        /// <summary>Tests: a stick held this way (camera-relative), or null for the real controls.</summary>
        public static Vector2? TestInput;
        bool runStopping; float landLock, airT;
        public bool Crouched => crouched;
        /// <summary>Crouch down or stand up (standing waits for room above your head).</summary>
        public void SetCrouch(bool on)
        {
            if (!on && crouched && Physics.SphereCast(transform.position + Vector3.up * 0.6f, 0.26f, Vector3.up, out _, 0.9f, ~((1 << 2) | (1 << Person.RagdollLayer) | (1 << Person.VehicleLayer)), QueryTriggerInteraction.Ignore)) return;
            crouched = on;
            if (on) { cc.height = 1.05f; cc.center = new Vector3(0, 0.54f, 0); } else { cc.height = 1.6f; cc.center = new Vector3(0, 0.81f, 0); }
            if (person != null && person.anim != null && HasParam(person.anim, "Crouch")) person.anim.SetBool("Crouch", on);
        }
        static readonly int ClimbMask = ~((1 << 2) | (1 << Person.RagdollLayer) | (1 << Person.VehicleLayer) | (1 << World.CamBlockLayer));
        /// <summary>Something to climb straight ahead: a face within reach, its top between knee and head-and-a-bit high, room to stand on it.</summary>
        bool Ledge(out Vector3 top) => Ledge(out top, out _);
        public string LedgeWhy() { Ledge(out _, out var why); return why; }
        bool Ledge(out Vector3 top, out string why)
        {
            top = default; why = ""; Vector3 f = transform.forward, p = transform.position;
            bool hit = false; float dist = 0f;
            foreach (float h in new[] { 0.35f, 0.75f, 1.2f, 1.6f }) if (Physics.Raycast(p + Vector3.up * h, f, out var w, 0.9f, ClimbMask, QueryTriggerInteraction.Ignore) && Mathf.Abs(w.normal.y) < 0.4f) { hit = true; dist = w.distance; why = $"face {w.collider.name} at {h}m/{dist:F2}; "; break; }
            if (!hit) { why = "no face ahead"; return false; }
            Vector3 over = p + f * (dist + 0.4f) + Vector3.up * 2.6f;
            if (!Physics.Raycast(over, Vector3.down, out var d, 2.6f, ClimbMask, QueryTriggerInteraction.Ignore) || d.normal.y < 0.7f) { why += "no top"; return false; }
            float rise = d.point.y - p.y; why += $"top {rise:F2}; "; if (rise < 0.45f || rise > 2.35f) { why += "wrong height"; return false; }
            // something to stand on, not the top rail of a fence: the surface carries on a bit further in
            if (!Physics.Raycast(over + f * 0.35f, Vector3.down, out var d2, 2.6f, ClimbMask, QueryTriggerInteraction.Ignore) || Mathf.Abs(d2.point.y - d.point.y) > 0.15f) { why += "too thin"; return false; }
            // room to stand up there
            if (Physics.CheckCapsule(d.point + Vector3.up * 0.35f, d.point + Vector3.up * 1.5f, 0.25f, ClimbMask, QueryTriggerInteraction.Ignore)) { why += "no headroom"; return false; }
            top = d.point; why += "ok"; return true;
        }
        /// <summary>Up onto a ledge: hands on, pull up (or a quick vault for low ones), step forward onto it.</summary>
        System.Collections.IEnumerator Climb(Vector3 top)
        {
            climbing = true; cc.enabled = false; vel = Vector3.zero; vy = 0f;
            float rise = top.y - transform.position.y; bool high = rise > 1.25f;
            if (person != null && person.anim != null) { if (high && HasParam(person.anim, "Climb")) person.anim.SetTrigger("Climb"); else person.Jump(); }
            var fg2 = GetComponentInChildren<FootGround>(); if (fg2 != null) fg2.off = true;
            Vector3 a = transform.position, up = new Vector3(a.x, top.y + 0.02f, a.z) + transform.forward * 0.15f, b = top + Vector3.up * 0.02f + transform.forward * 0.25f;
            float t1 = high ? 0.9f : 0.35f, t2 = high ? 0.55f : 0.3f;
            for (float t = 0; t < t1; t += Time.deltaTime) { transform.position = Vector3.Lerp(a, up, Mathf.SmoothStep(0, 1, t / t1)); yield return null; }
            for (float t = 0; t < t2; t += Time.deltaTime) { transform.position = Vector3.Lerp(up, b, Mathf.SmoothStep(0, 1, t / t2)); yield return null; }
            transform.position = b; cc.enabled = true; if (fg2 != null) fg2.off = false; climbing = false;
        }

        bool seated;
        bool chairSat; Vector3 chairPos; float chairYaw, chairT;
        /// <summary>Sat on a seat (not in a car).</summary>
        public bool OnSeat => chairSat;
        /// <summary>Sit down on a seat: the root goes at pos facing yaw, the sitting pose plays, no walking until you get up.</summary>
        public void SitOn(Vector3 pos, float yaw)
        {
            if (inCar || chairSat) return;
            if (crouched) SetCrouch(false);
            chairSat = true; chairPos = pos; chairYaw = yaw; chairT = Time.time; cc.enabled = false; vel = Vector3.zero; vy = 0f;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0)); bodyYaw = yaw;
            if (person != null && person.anim != null && HasParam(person.anim, "Sit")) person.anim.SetBool("Sit", true);
            fg ??= GetComponentInChildren<FootGround>(); if (fg != null) fg.off = true;
        }
        /// <summary>Get up and step out in front of the seat.</summary>
        public void StandUp()
        {
            if (!chairSat) return; chairSat = false;
            if (person != null && person.anim != null && HasParam(person.anim, "Sit")) person.anim.SetBool("Sit", false);
            Vector3 p = chairPos + Quaternion.Euler(0, chairYaw, 0) * Vector3.forward * 0.6f; float y = chairPos.y;
            if (Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out var hit, 3f, ~((1 << 2) | (1 << Person.RagdollLayer)), QueryTriggerInteraction.Ignore)) y = hit.point.y;
            transform.position = new Vector3(p.x, y + 0.02f, p.z); cc.enabled = true;
            fg ??= GetComponentInChildren<FootGround>(); if (fg != null) fg.off = false;
        }
        static bool HasParam(Animator a, string n) { foreach (var p in a.parameters) if (p.name == n) return true; return false; }
        // which of the newer parameters this body's controller has (looked up once per controller: reading the list allocates)
        struct PSet { public bool stick, runStop, hardLand; }
        PSet pset; RuntimeAnimatorController psetFor;
        PSet Params()
        {
            var a = person != null ? person.anim : null;
            if (a == null || a.runtimeAnimatorController == null) return default;
            if (psetFor != a.runtimeAnimatorController) { psetFor = a.runtimeAnimatorController; pset = new PSet { stick = HasParam(a, "Stick"), runStop = HasParam(a, "RunStop"), hardLand = HasParam(a, "HardLand") }; }
            return pset;
        }
        /// <summary>Into the driver's seat (every frame, and again after the animation so the pose's own drift can't move you).</summary>
        void Sit()
        {
            if (PCar == null || !PCar.Seat(out var root, out var hips, out var fl, out var fr, out var hl, out var hr, out var q)) return;
            transform.SetPositionAndRotation(root, q); bodyYaw = q.eulerAngles.y;
            fg ??= GetComponentInChildren<FootGround>();
            if (fg != null) { fg.sat = true; fg.satBody = hips; fg.satFootL = fl; fg.satFootR = fr; fg.satHandL = hl; fg.satHandR = hr; fg.satRot = q; }
        }
        FootGround fg;
        void LateUpdate() { if (inCar && seated) Sit(); }

        /// <summary>How far behind the car the camera sits: the same at every speed. The custom view setting will change it (saved).</summary>
        public static float CarView
        {
            get { if (carView < 0f) carView = PlayerPrefs.GetFloat("view.car", 7f); return carView; }
            set { carView = Mathf.Clamp(value, 4f, 12f); PlayerPrefs.SetFloat("view.car", carView); }
        }
        static float carView = -1f;

        void UpdateCamera()
        {
            if (camTarget == null || orbit == null) return;
            Transform tgt = inCar && VehT != null ? VehT : transform;
            Vector3 focus = tgt.position + Vector3.up * (inCar ? 1.6f : 1.55f);
            if (!inCar && person != null && person.state != Person.State.Walking) focus = person.BodyPos + Vector3.up * 0.5f;
            if (spectate && Person.LastHit != null) focus = Person.LastHit.BodyPos + Vector3.up * 0.6f;
            if (inCar && VehT != null && !doorBusy)
            {
                // locked to the car across the ground (no trailing behind at speed); only bumps are eased so the view doesn't shake
                var c = camTarget.position;
                camTarget.position = new Vector3(focus.x, Mathf.Lerp(c.y, focus.y, 1f - Mathf.Exp(-10f * Time.deltaTime)), focus.z);
            }
            else camTarget.position = Vector3.Lerp(camTarget.position, focus, 1f - Mathf.Exp(-14f * Time.deltaTime));
            if (inCar && VehT != null && VehSpeed > 2f && Vector3.Dot(VehVel, VehT.forward) > 0) yaw = Mathf.LerpAngle(yaw, VehT.eulerAngles.y, 2f * Time.deltaTime);
            bool indoors = !inCar && GameAudio.I != null && GameAudio.I.InsideAny(transform.position + Vector3.up);
            orbit.Radius = Mathf.Lerp(orbit.Radius, inCar ? CarView : indoors ? 2.4f : spectate ? 6f : 4.2f, 4f * Time.deltaTime);
            if (!inCar && Fighter.Session.Hot && fighter != null && Time.time - lookT > 0.8f)
            {
                var foe = fighter.Target != null ? fighter.Target : fighter.NearestFoe();
                if (foe != null) { Vector3 fd = (foe.transform.position - transform.position).WithY(0); if (fd.sqrMagnitude > 0.04f) yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(fd.x, fd.z) * Mathf.Rad2Deg, 1f - Mathf.Exp(-1.5f * Time.deltaTime)); }
            }
            if (Fighter.Session.Shake > 0f) camTarget.position += Random.insideUnitSphere * Fighter.Session.Shake;
            if (!float.IsNaN(showYaw)) yaw = showYaw;
            if (!float.IsNaN(showPitch)) pitch = showPitch;
            if (showRadius > 0f) orbit.Radius = showRadius;
            var h = orbit.HorizontalAxis; h.Value = yaw; orbit.HorizontalAxis = h;
            var v = orbit.VerticalAxis; v.Range = new Vector2(-25f, 60f); v.Value = pitch; orbit.VerticalAxis = v;
        }
    }
}
