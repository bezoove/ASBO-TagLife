using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ASBO.Slice
{
    /// <summary>
    /// Frame-time watch and automatic quality on phones. Measures every frame (average, worst, garbage collections);
    /// when the phone can't hold its target it steps down render scale and shadow distance, and steps back up when
    /// there's room. A steady frame matters more than a high one: big swings are what you feel as jitter.
    /// The readout (top right) is on in test builds; it is how every change gets checked from now on.
    /// </summary>
    public class Perf : MonoBehaviour
    {
        public static Perf I;
        public static bool Show = true;
        public float AvgMs { get; private set; }
        public float WorstMs { get; private set; }
        public int Level { get; private set; } // 0 best .. 3 lightest
        public string Text { get; private set; } = "";

        float acc, worst, worstHold, windowT, judgeT, goodT; int frames, gc0, gcPerMin; float gcT;
        UniversalRenderPipelineAsset urp; float baseScale = 0.85f, baseShadow = 45f;
        static readonly float[] Scale = { 1f, 0.9f, 0.8f, 0.7f };
        static readonly float[] Shadow = { 1f, 0.75f, 0.55f, 0.4f };

        public static void Create(Transform parent)
        {
            var g = new GameObject("Perf"); g.transform.SetParent(parent, false); I = g.AddComponent<Perf>();
        }

        void Awake()
        {
            urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null) { baseScale = urp.renderScale; baseShadow = urp.shadowDistance; }
            gc0 = System.GC.CollectionCount(0);
            if (Application.isMobilePlatform) PhoneSettings();
        }

        /// <summary>One-off settings for phones: the terrain and camera cost a lot more than they need to.</summary>
        void PhoneSettings()
        {
            foreach (var t in Terrain.activeTerrains)
            {
                t.heightmapPixelError = 8f; t.basemapDistance = 70f;
                t.detailObjectDistance = 45f; t.detailObjectDensity = 0.6f; t.treeBillboardDistance = 70f; t.treeDistance = 400f;
                t.shadowCastingMode = ShadowCastingMode.Off; t.drawInstanced = true;
            }
            var cam = Camera.main; if (cam != null) cam.farClipPlane = Mathf.Min(cam.farClipPlane, 380f);
            QualitySettings.lodBias = 0.7f;
        }

        void Update()
        {
            float ms = Time.unscaledDeltaTime * 1000f;
            acc += ms; frames++; if (ms > worst) worst = ms;
            windowT += Time.unscaledDeltaTime; gcT += Time.unscaledDeltaTime;
            if (gcT >= 60f) { int n = System.GC.CollectionCount(0); gcPerMin = n - gc0; gc0 = n; gcT = 0f; }
            if (windowT < 0.5f) return;
            AvgMs = acc / Mathf.Max(1, frames);
            worstHold = Mathf.Max(worstHold * 0.6f, worst); WorstMs = worstHold;
            acc = 0f; frames = 0; worst = 0f; windowT = 0f;
            int gcNow = System.GC.CollectionCount(0) - gc0;
            Text = $"{Mathf.RoundToInt(1000f / Mathf.Max(1f, AvgMs))} fps  {AvgMs:F1} ms  worst {WorstMs:F0} ms  GC {Mathf.Max(gcPerMin, gcNow)}/min  q{Level}";
            if (!Application.isEditor) Judge();
        }

        void Judge()
        {
            int target = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60;
            float budget = 1000f / target;
            judgeT += 0.5f;
            if (AvgMs > budget * 1.12f && judgeT > 2.5f && Level < 3) { SetLevel(Level + 1); judgeT = 0f; goodT = 0f; }
            else if (AvgMs < budget * 0.8f) { goodT += 0.5f; if (goodT > 10f && Level > 0) { SetLevel(Level - 1); goodT = 0f; judgeT = 0f; } }
            else goodT = 0f;
        }

        void SetLevel(int l)
        {
            Level = l;
            if (urp == null) return;
            urp.renderScale = Mathf.Clamp(baseScale * Scale[l] / Scale[0], 0.55f, 1f);
            urp.shadowDistance = baseShadow * Shadow[l];
        }

        void OnDestroy() { if (urp != null && !Application.isEditor) { urp.renderScale = baseScale; urp.shadowDistance = baseShadow; } }

        void OnGUI()
        {
            if (!Show || World.HideHud || TouchControls.Editing || string.IsNullOrEmpty(Text)) return;
            var r = new Rect(12f, 64f, 420f, 22f);
            GUI.Label(r, Text);
        }
    }
}
