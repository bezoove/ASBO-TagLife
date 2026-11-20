using UnityEngine;
using UnityEngine.InputSystem;

namespace ASBO.Slice
{
    /// <summary>Third-person player: walk, run, enter/exit the car. Camera orbits with the mouse.</summary>
    public class PlayerCtl : MonoBehaviour
    {
        public Car car;
        CharacterController cc; Person person; Camera cam;
        float yaw, pitch = 14f, vy;
        bool inCar, spectate, firstPerson; int shotN;
        Vector3 camVel;

        public void Setup(Car c)
        {
            car = c; cc = gameObject.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.35f;
            person = GetComponent<Person>();
            cam = Camera.main; if (cam == null) { var g = new GameObject("Camera"); cam = g.AddComponent<Camera>(); g.tag = "MainCamera"; }
            cam.nearClipPlane = 0.15f; cam.farClipPlane = 600f;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            yaw = transform.eulerAngles.y;
        }

        /// <summary>Debug teleport (number keys): stand at pos looking along yawDeg, camera behind.</summary>
        public void Teleport(Vector3 pos, float yawDeg, float pitchDeg = 14f)
        {
            if (inCar) ToggleCar();
            cc.enabled = false; transform.position = pos; transform.rotation = Quaternion.Euler(0, yawDeg, 0); cc.enabled = true;
            yaw = yawDeg; pitch = pitchDeg; vy = 0f;
            if (cam != null) { cam.transform.position = pos + Vector3.up * 1.5f - Quaternion.Euler(pitch, yaw, 0) * Vector3.forward * 5f; camVel = Vector3.zero; }
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
                yaw += d.x; pitch = Mathf.Clamp(pitch - d.y, -10f, 55f);
            }
            if (kb.eKey.wasPressedThisFrame) ToggleCar();
            if (kb.vKey.wasPressedThisFrame) spectate = !spectate;
            if (kb.f9Key.wasPressedThisFrame)
            {
                string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Shots"); System.IO.Directory.CreateDirectory(dir);
                ScreenCapture.CaptureScreenshot(dir + "/shot_" + (shotN++) + ".png");
            }
            if (inCar) { transform.position = car.seatPoint.position - Vector3.up * 0.9f; }
            else Walk(kb);
        }

        void ToggleCar()
        {
            if (!inCar)
            {
                if ((car.transform.position - transform.position).sqrMagnitude < 4.5f * 4.5f)
                {
                    inCar = true; car.occupied = true; cc.enabled = false; SetVisible(false);
                }
            }
            else
            {
                inCar = false; car.occupied = false;
                transform.position = car.exitPoint.position - Vector3.up * 0.2f; cc.enabled = true; SetVisible(true);
            }
        }

        void Walk(Keyboard kb)
        {
            Vector3 f = Quaternion.Euler(0, yaw, 0) * Vector3.forward, r = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            Vector3 mv = f * ((kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0)) + r * ((kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0));
            float spd = kb.leftShiftKey.isPressed ? 5.2f : 2.4f;
            if (mv.sqrMagnitude > 0.01f)
            {
                mv.Normalize();
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(mv), 12f * Time.deltaTime);
                person.Animate(spd);
            }
            else mv = Vector3.zero;
            vy = cc.isGrounded ? -1f : vy - 18f * Time.deltaTime;
            if (cc.isGrounded && kb.spaceKey.wasPressedThisFrame) vy = 5f;
            cc.Move((mv * spd + Vector3.up * vy) * Time.deltaTime);
        }

        void LateUpdate()
        {
            if (cam == null) return;
            Transform tgt = inCar ? car.transform : transform;
            if (spectate && Person.LastHit != null)
            {
                Vector3 fc = Person.LastHit.BodyPos + Vector3.up * 0.8f;
                Quaternion sr = Quaternion.Euler(Mathf.Max(pitch, 18f), yaw, 0);
                Vector3 sw = fc - sr * Vector3.forward * 7f;
                cam.transform.position = Vector3.SmoothDamp(cam.transform.position, sw, ref camVel, 0.1f);
                cam.transform.rotation = Quaternion.LookRotation(fc - cam.transform.position);
                return;
            }
            bool indoors = !inCar && GameAudio.I != null && GameAudio.I.InsideAny(transform.position + Vector3.up);
            float dist = inCar ? 8.5f : indoors ? 2.6f : 5.0f, height = inCar ? 1.8f : indoors ? 1.6f : 1.5f;
            if (inCar)
            {
                float carYaw = car.transform.eulerAngles.y;
                if (car.Speed > 2f && Vector3.Dot(car.Velocity, car.transform.forward) > 0) yaw = Mathf.LerpAngle(yaw, carYaw, 2.0f * Time.deltaTime);
            }
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 focus = tgt.position + Vector3.up * height;
            Vector3 want = focus - rot * Vector3.forward * dist;
            bool blocked = false;
            var dirC = want - focus; float lenC = dirC.magnitude;
            foreach (var h in Physics.SphereCastAll(focus, 0.2f, dirC / Mathf.Max(lenC, 0.001f), lenC, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.GetComponentInParent<Car>() != null || h.collider.GetComponentInParent<Person>() != null) continue;
                if (h.distance <= 0f) continue;
                float dd = Mathf.Max(0.3f, h.distance - 0.05f);
                if (!blocked || dd < (want - focus).magnitude) { want = focus + dirC / lenC * dd; blocked = true; }
            }
            // too tight for third person (small cottage rooms): go first person at eye height
            bool fp = !inCar && (want - focus).magnitude < 1.1f;
            if (fp != firstPerson) { firstPerson = fp; if (!inCar) SetVisible(!fp); }
            if (fp)
            {
                cam.transform.position = transform.position + Vector3.up * 1.62f + Quaternion.Euler(0, yaw, 0) * Vector3.forward * 0.12f;
                cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0); camVel = Vector3.zero;
                return;
            }
            // snap in immediately when something is between the player and the camera, ease back out otherwise
            if (blocked && (cam.transform.position - focus).magnitude > (want - focus).magnitude) { cam.transform.position = want; camVel = Vector3.zero; }
            else cam.transform.position = Vector3.SmoothDamp(cam.transform.position, want, ref camVel, 0.08f);
            cam.transform.rotation = Quaternion.LookRotation(focus - cam.transform.position);
        }
    }
}
