using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ASBO.Slice
{
    /// <summary>Lighting and grading: warm trilight ambient, sun colour, fog, and a mobile-friendly post stack (tonemap, bloom, grade, vignette).</summary>
    public static class Look
    {
        public static void Apply(Transform root)
        {
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun != null && sun.type == LightType.Directional)
            {
                sun.transform.rotation = Quaternion.Euler(42f, -35f, 0);
                sun.color = new Color(1f, 0.95f, 0.86f); sun.intensity = 1.35f;
                sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.82f;
                sun.shadowBias = 0.06f; sun.shadowNormalBias = 0.5f;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f) * 1.05f;
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.58f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.32f, 0.24f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.74f, 0.8f, 0.86f); RenderSettings.fogStartDistance = 90f; RenderSettings.fogEndDistance = 420f;

            var cam = Camera.main;
            if (cam != null)
            {
                var data = cam.GetComponent<UniversalAdditionalCameraData>() ?? cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            }
            var go = new GameObject("PostFX"); go.transform.SetParent(root, false);
            var vol = go.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 10;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var bl = prof.Add<Bloom>(true); bl.intensity.Override(0.35f); bl.threshold.Override(1.05f); bl.scatter.Override(0.6f); bl.highQualityFiltering.Override(false);
            var ca = prof.Add<ColorAdjustments>(true); ca.postExposure.Override(0.35f); ca.contrast.Override(12f); ca.saturation.Override(8f);
            var wb = prof.Add<WhiteBalance>(true); wb.temperature.Override(6f);
            var vg = prof.Add<Vignette>(true); vg.intensity.Override(0.18f); vg.smoothness.Override(0.5f);
            vol.sharedProfile = prof;
        }
    }
}
