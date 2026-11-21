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

        Transform VehT => rcc != null ? rcc.transform : car != null ? car.transform : null;
        float VehSpeed => rcc != null ? rcc.Speed : car != null ? car.Speed : 0f;
        Vector3 VehVel => rcc != null ? rcc.Velocity : car != null ? car.Velocity : Vector3.zero;

        public void Setup(Car c)
        {
            car = c;
            rcc = FindFirstObjectByType<RccVehicle>();
            cc = gameObject.AddComponent<CharacterController>();
            cc.height = 1.76f; cc.radius = 0.28f; cc.center = new Vector3(0, 0.89f, 0); cc.stepOffset = 0.35f; cc.slopeLimit = 50f; cc.skinWidth = 0.03f;
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
            var comp = vg.AddComponent<CinemachineRotationComposer>();
            comp.Composition.ScreenPosition = new Vector2(0f, 0.05f);
            var deocc = vg.AddComponent<CinemachineDeoccluder>();
            deocc.CollideAgainst = ~(1 << 2);
            deocc.MinimumDistanceFromTarget = 0.25f;
            var av = deocc.AvoidObstacles; av.Enabled = true; av.CameraRadius = 0.12f; deocc.AvoidObstacles = av;
            touch = TouchControls.Wanted;
            if (touch) TouchControls.Create(this);
            else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            yaw = bodyYaw = transform.eulerAngles.y;
        }

        public void Teleport(Vector3 pos, float yawDeg, float pitchDeg = 12f)
        {
            if (inCar) ToggleCar();
            cc.enabled = false; transform.position = pos; transform.rotation = Quaternion.Euler(0, yawDeg, 0); cc.enabled = true;
            yaw = bodyYaw = yawDeg; pitch = pitchDeg; vy = 0f; vel = Vector3.zero;
            camTarget.position = pos + Vector3.up * 1.55f;
            if (vcam != null) vcam.PreviousStateIsValid = false;
        }

        bool wasGrounded = true; float fallSpeed;
        public bool InCar => inCar; bool touch;
        Vector2 lookAdd;
        /// <summary>Camera turn from touch drags (degrees).</summary>
        public void AddLook(Vector2 deg) { lookAdd += deg; }

        void SetVisible(bool v) { foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = v; }

        void Update()
        {
            var kb = Keyboard.current; var ms = Mouse.current; var gp = Gamepad.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (!touch && ms != null && ms.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (!touch && ms != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 d = ms.delta.ReadValue() * 0.12f;
                yaw += d.x; pitch = Mathf.Clamp(pitch - d.y, -25f, 60f);
            }
            // touch drags and the gamepad's right stick turn the camera too
            if (lookAdd != Vector2.zero) { yaw += lookAdd.x; pitch = Mathf.Clamp(pitch - lookAdd.y, -25f, 60f); lookAdd = Vector2.zero; }
            if (gp != null) { var rs = gp.rightStick.ReadValue(); if (rs.sqrMagnitude > 0.02f) { yaw += rs.x * 150f * Time.deltaTime; pitch = Mathf.Clamp(pitch - rs.y * 90f * Time.deltaTime, -25f, 60f); } }
            if ((kb != null && kb.eKey.wasPressedThisFrame) || (gp != null && gp.selectButton.wasPressedThisFrame)) ToggleCar();
            if (kb != null && kb.vKey.wasPressedThisFrame) spectate = !spectate;
            if (kb != null && kb.f9Key.wasPressedThisFrame)
            {
                string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots"); System.IO.Directory.CreateDirectory(dir);
                ScreenCapture.CaptureScreenshot(dir + "/shot_" + (shotN++) + ".png");
            }
            if (inCar && VehT != null) { transform.position = VehT.position + Vector3.up * 0.2f; transform.rotation = VehT.rotation; }
            else Move(kb);
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
            var pad = Gamepad.current; if (pad != null && pad.leftStick.ReadValue().sqrMagnitude > 0.04f) inp = pad.leftStick.ReadValue();
            float mag = Mathf.Clamp01(inp.magnitude);
            Vector3 dir = camYaw * new Vector3(inp.x, 0, inp.y); if (dir.sqrMagnitude > 1e-4f) dir.Normalize();
            bool sprint = fakeSprint || (kb != null && kb.leftShiftKey.isPressed) || (pad != null && pad.leftStickButton.isPressed);
            bool walk = (kb != null && kb.leftCtrlKey.isPressed) || (mag > 0.05f && mag < 0.5f);
            float pace = sprint ? sp.sprint : walk ? sp.walk : sp.run;
            Vector3 want = mag < 0.05f ? Vector3.zero : dir * pace;
            bool grounded = cc.isGrounded;

            // turn on the spot: from (nearly) standing, a big change of direction plays the turn clip
            if (turnTimer > 0f)
            {
                turnTimer -= Time.deltaTime;
                float step = turnLeft / sp.turnSecs * Time.deltaTime;
                bodyYaw += step;
                want = Vector3.zero;
            }
            else if (want.sqrMagnitude > 0.01f && vel.magnitude < 0.4f && grounded)
            {
                float delta = Mathf.DeltaAngle(bodyYaw, Mathf.Atan2(want.x, want.z) * Mathf.Rad2Deg);
                if (Mathf.Abs(delta) > 75f && FreePackTest.On)
                {
                    turnLeft = Mathf.Clamp(delta, -100f, 100f); turnTimer = sp.turnSecs * 0.8f;
                    person.TurnInPlace(delta < 0f);
                    want = Vector3.zero;
                }
            }

            // human-like acceleration: quicker to stop than to get going, sprint builds up
            float accTime = want.sqrMagnitude < vel.sqrMagnitude ? 0.16f : (vel.magnitude < sp.walk ? 0.22f : 0.38f);
            vel = Vector3.SmoothDamp(vel, want, ref velRef, accTime);
            if (want == Vector3.zero && vel.magnitude < 0.05f) vel = Vector3.zero;

            // body follows the travel direction along a smooth arc (tighter when slow)
            if (vel.sqrMagnitude > 0.04f && turnTimer <= 0f)
            {
                float target = Mathf.Atan2((want.sqrMagnitude > 0.01f ? want : vel).x, (want.sqrMagnitude > 0.01f ? want : vel).z) * Mathf.Rad2Deg;
                float t = Mathf.Lerp(0.07f, 0.2f, vel.magnitude / Mathf.Max(sp.sprint, 0.1f));
                bodyYaw = Mathf.SmoothDampAngle(bodyYaw, target, ref yawVel, t);
            }
            transform.rotation = Quaternion.Euler(0, bodyYaw, 0);

            if (grounded && !wasGrounded && fallSpeed < -3.5f && GameAudio.I != null) GameAudio.I.Land(transform.position);
            fallSpeed = vy; wasGrounded = grounded;
            if (grounded && vy < 0f) vy = -2f; else vy -= 19.6f * Time.deltaTime;
            if (grounded && turnTimer <= 0f && ((kb != null && kb.spaceKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame))) { vy = 4.6f; person.Jump(); }
            cc.Move((vel + Vector3.up * vy) * Time.deltaTime);
            person.SetGrounded(cc.isGrounded);

            // the animator gets the real local velocity (lateral part = the lean/cross-step while turning)
            Vector3 hv = cc.velocity; hv.y = 0f; if (hv.magnitude > vel.magnitude + 0.3f) hv = vel;
            Vector3 local = Quaternion.Inverse(transform.rotation) * hv;
            person.SetMove(new Vector2(local.x, local.z));
        }

        void ToggleCar()
        {
            var vt = VehT; if (vt == null) return;
            if (!inCar)
            {
                if ((vt.position - transform.position).sqrMagnitude > 4.5f * 4.5f) return;
                inCar = true; cc.enabled = false; SetVisible(false);
                if (rcc != null && rcc.Car != null) RCCP.RegisterPlayerVehicle(rcc.Car, true, true);
                else if (car != null) car.occupied = true;
            }
            else
            {
                inCar = false;
                Vector3 exit;
                if (rcc != null && rcc.Car != null) { RCCP.SetControl(rcc.Car, false); RCCP.DeRegisterPlayerVehicle(); exit = vt.position - vt.right * 1.9f; }
                else { car.occupied = false; exit = car.exitPoint.position; }
                exit.y = vt.position.y; transform.position = exit; cc.enabled = true; SetVisible(true);
                bodyYaw = vt.eulerAngles.y; vel = Vector3.zero;
            }
        }

        void UpdateCamera()
        {
            if (camTarget == null || orbit == null) return;
            Transform tgt = inCar && VehT != null ? VehT : transform;
            Vector3 focus = tgt.position + Vector3.up * (inCar ? 1.6f : 1.55f);
            if (spectate && Person.LastHit != null) focus = Person.LastHit.BodyPos + Vector3.up * 0.6f;
            camTarget.position = Vector3.Lerp(camTarget.position, focus, 1f - Mathf.Exp(-14f * Time.deltaTime));
            if (inCar && VehT != null && VehSpeed > 2f && Vector3.Dot(VehVel, VehT.forward) > 0) yaw = Mathf.LerpAngle(yaw, VehT.eulerAngles.y, 2f * Time.deltaTime);
            bool indoors = !inCar && GameAudio.I != null && GameAudio.I.InsideAny(transform.position + Vector3.up);
            orbit.Radius = Mathf.Lerp(orbit.Radius, inCar ? 8f : indoors ? 2.4f : spectate ? 6f : 4.2f, 4f * Time.deltaTime);
            if (!float.IsNaN(showYaw)) yaw = showYaw;
            if (!float.IsNaN(showPitch)) pitch = showPitch;
            if (showRadius > 0f) orbit.Radius = showRadius;
            var h = orbit.HorizontalAxis; h.Value = yaw; orbit.HorizontalAxis = h;
            var v = orbit.VerticalAxis; v.Range = new Vector2(-25f, 60f); v.Value = pitch; orbit.VerticalAxis = v;
        }
    }
}
