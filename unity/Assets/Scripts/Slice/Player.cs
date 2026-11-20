using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace ASBO.Slice
{
    /// <summary>
    /// Third-person player: CharacterController movement relative to the camera with acceleration, turning and
    /// sprint; the Humanoid animator is fed the real ground speed (blend thresholds = mocap speeds, so feet plant).
    /// Camera: Cinemachine orbital follow + rotation composer + deoccluder (no clipping through walls).
    /// </summary>
    public class PlayerCtl : MonoBehaviour
    {
        public Car car;
        CharacterController cc; Person person; Camera cam;
        CinemachineCamera vcam; CinemachineOrbitalFollow orbit; Transform camTarget;
        float yaw, pitch = 12f, vy, curSpeed; Vector3 moveDir;
        bool inCar, spectate; int shotN; float groundedT;
        const float Walk = 1.55f, Run = 3.95f, Slow = 1.0f;

        public void Setup(Car c)
        {
            car = c;
            cc = gameObject.AddComponent<CharacterController>();
            cc.height = 1.76f; cc.radius = 0.28f; cc.center = new Vector3(0, 0.89f, 0); cc.stepOffset = 0.35f; cc.slopeLimit = 50f; cc.skinWidth = 0.03f;
            person = GetComponent<Person>();
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
            vg.AddComponent<CinemachineDeoccluder>();
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            yaw = transform.eulerAngles.y;
        }

        /// <summary>Debug teleport (number keys): stand at pos looking along yawDeg.</summary>
        public void Teleport(Vector3 pos, float yawDeg, float pitchDeg = 12f)
        {
            if (inCar) ToggleCar();
            cc.enabled = false; transform.position = pos; transform.rotation = Quaternion.Euler(0, yawDeg, 0); cc.enabled = true;
            yaw = yawDeg; pitch = pitchDeg; vy = 0f; curSpeed = 0f;
            camTarget.position = pos + Vector3.up * 1.55f;
            if (vcam != null) vcam.PreviousStateIsValid = false;
        }

        void SetVisible(bool v) { foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = v; }

        void Update()
        {
            var kb = Keyboard.current; var ms = Mouse.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (ms != null && ms.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (ms != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 d = ms.delta.ReadValue() * 0.12f;
                yaw += d.x; pitch = Mathf.Clamp(pitch - d.y, -25f, 60f);
            }
            if (kb.eKey.wasPressedThisFrame) ToggleCar();
            if (kb.vKey.wasPressedThisFrame) spectate = !spectate;
            if (kb.f9Key.wasPressedThisFrame)
            {
                string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots"); System.IO.Directory.CreateDirectory(dir);
                ScreenCapture.CaptureScreenshot(dir + "/shot_" + (shotN++) + ".png");
            }
            if (inCar) { transform.position = car.seatPoint.position - Vector3.up * 0.9f; transform.rotation = car.transform.rotation; }
            else Move(kb);
            UpdateCamera();
        }

        void Move(Keyboard kb)
        {
            Vector3 f = Quaternion.Euler(0, yaw, 0) * Vector3.forward, r = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            Vector2 inp = new Vector2((kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0), (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0));
            var pad = Gamepad.current; if (pad != null && pad.leftStick.ReadValue().sqrMagnitude > 0.04f) inp = pad.leftStick.ReadValue();
            float mag = Mathf.Clamp01(inp.magnitude);
            Vector3 want = (f * inp.y + r * inp.x); if (want.sqrMagnitude > 1e-4f) want.Normalize();
            float target = mag < 0.05f ? 0f : (kb.leftShiftKey.isPressed || (pad != null && pad.leftStickButton.isPressed) ? Run : kb.leftCtrlKey.isPressed ? Slow : Walk) * Mathf.Max(mag, 0.35f);
            // accelerate/decelerate like a person: quicker to stop than to sprint
            float acc = target > curSpeed ? (curSpeed < Walk ? 4.5f : 3.2f) : 7f;
            curSpeed = Mathf.MoveTowards(curSpeed, target, acc * Time.deltaTime);
            if (want.sqrMagnitude > 0.01f)
            {
                moveDir = want;
                float turn = Mathf.Lerp(720f, 380f, curSpeed / Run);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(want), turn * Time.deltaTime);
            }
            // move mostly along facing so the body leads the motion (no moonwalking)
            Vector3 dir = Vector3.Slerp(transform.forward, moveDir, 0.35f);
            bool grounded = cc.isGrounded; groundedT = grounded ? 0f : groundedT + Time.deltaTime;
            if (grounded && vy < 0f) vy = -2f; else vy -= 19.6f * Time.deltaTime;
            if (grounded && (kb.spaceKey.wasPressedThisFrame || (pad != null && pad.buttonSouth.wasPressedThisFrame))) { vy = 4.6f; person.Jump(); }
            cc.Move((dir * curSpeed + Vector3.up * vy) * Time.deltaTime);
            Vector3 hv = cc.velocity; hv.y = 0;
            person.Animate(Mathf.Min(hv.magnitude, curSpeed + 0.2f));
        }

        void ToggleCar()
        {
            if (!inCar)
            {
                if ((car.transform.position - transform.position).sqrMagnitude < 4.5f * 4.5f) { inCar = true; car.occupied = true; cc.enabled = false; SetVisible(false); }
            }
            else
            {
                inCar = false; car.occupied = false;
                transform.position = car.exitPoint.position - Vector3.up * 0.2f; cc.enabled = true; SetVisible(true);
            }
        }

        void UpdateCamera()
        {
            if (camTarget == null || orbit == null) return;
            Transform tgt = inCar ? car.transform : transform;
            Vector3 focus = tgt.position + Vector3.up * (inCar ? 1.6f : 1.55f);
            if (spectate && Person.LastHit != null) focus = Person.LastHit.BodyPos + Vector3.up * 0.6f;
            camTarget.position = Vector3.Lerp(camTarget.position, focus, 1f - Mathf.Exp(-14f * Time.deltaTime));
            if (inCar && car.Speed > 2f && Vector3.Dot(car.Velocity, car.transform.forward) > 0) yaw = Mathf.LerpAngle(yaw, car.transform.eulerAngles.y, 2f * Time.deltaTime);
            bool indoors = !inCar && GameAudio.I != null && GameAudio.I.InsideAny(transform.position + Vector3.up);
            orbit.Radius = Mathf.Lerp(orbit.Radius, inCar ? 8f : indoors ? 2.4f : spectate ? 6f : 4.2f, 4f * Time.deltaTime);
            var h = orbit.HorizontalAxis; h.Value = yaw; orbit.HorizontalAxis = h;
            var v = orbit.VerticalAxis; v.Range = new Vector2(-25f, 60f); v.Value = pitch; orbit.VerticalAxis = v;
        }
    }
}
