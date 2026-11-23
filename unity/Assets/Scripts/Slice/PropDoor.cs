using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>A door you can open and shut: swings about its hinge. Some are locked (a code, a key).</summary>
    public class PropDoor : MonoBehaviour
    {
        public string id, lockTag; public float open; public float target; public Transform leaf;
        public static readonly List<PropDoor> All = new List<PropDoor>();
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        public static PropDoor Make(Transform parent, string id, Vector3 p, float yaw, string tag)
        {
            var e = PropLib.Get(id); if (e == null) return null;
            var hinge = new GameObject("Door_" + id); hinge.transform.SetParent(parent, false);
            // the leaf is modelled centred with its hinge at x = e.hinge (Blender) = -hinge in Unity's mirrored x
            Vector3 hingeLocal = new Vector3(-e.hinge, 0f, 0f);
            hinge.transform.localPosition = p + Quaternion.Euler(0, yaw, 0) * hingeLocal; hinge.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var leaf = new GameObject("leaf"); leaf.transform.SetParent(hinge.transform, false); leaf.transform.localPosition = -hingeLocal;
            leaf.AddComponent<MeshFilter>().sharedMesh = e.mesh;
            var mr = leaf.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { PropLib.I.opaque, PropLib.I.glass }; mr.shadowCastingMode = ShadowCastingMode.Off;
            var bc = leaf.AddComponent<BoxCollider>(); bc.center = e.center; bc.size = e.size;
            leaf.layer = Bld.DetailLayer;
            var d = hinge.AddComponent<PropDoor>(); d.id = id; d.lockTag = tag ?? ""; d.leaf = leaf.transform;
            return d;
        }
        void Update()
        {
            if (Mathf.Abs(open - target) < 0.01f) return;
            open = Mathf.MoveTowards(open, target, Time.deltaTime * (id == "vault_door" ? 0.35f : 1.6f));
            transform.localRotation = Quaternion.Euler(0, baseYaw + Mathf.SmoothStep(0, 1, open) * (id == "vault_door" ? -110f : 100f) * swing, 0);
        }
        float baseYaw = float.NaN; float swing = 1f;
        public void Toggle(Vector3 from)
        {
            if (float.IsNaN(baseYaw)) baseYaw = transform.localEulerAngles.y;
            if (target < 0.5f)
            {
                // swing away from whoever's opening it
                Vector3 fwd = transform.forward; swing = Vector3.Dot(from - transform.position, fwd) > 0f ? 1f : -1f;
                if (id == "vault_door") swing = 1f;
                target = 1f;
            }
            else target = 0f;
            if (GameAudio.I != null) GameAudio.I.Knock(transform.position + Vector3.up, id == "vault_door" ? 6f : 2.5f, id != "door");
        }
    }
}
