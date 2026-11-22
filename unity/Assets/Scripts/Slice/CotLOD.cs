using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>Hides furniture/interior detail and fire lights when the camera is far away (mobile budget).</summary>
    public class CotLOD : MonoBehaviour
    {
        [SerializeField] GameObject deco, furn; [SerializeField] List<Light> lights; float next;
        public void Setup(GameObject d, GameObject f, List<Light> l) { deco = d; furn = f; lights = l; }
        void Update()
        {
            if (Time.time < next) return; next = Time.time + 0.4f + Random.value * 0.2f;
            var cam = Camera.main; if (cam == null) return;
            float dist = Vector3.Distance(cam.transform.position, transform.position);
            bool near = dist < 42f;
            if (deco != null && deco.activeSelf != near) deco.SetActive(near);
            if (furn != null && furn.activeSelf != near) furn.SetActive(near);
            if (LightBudget.I == null) foreach (var l in lights) if (l != null) l.enabled = dist < 30f; // otherwise the light budget picks the nearest lights town-wide
        }
    }
}
