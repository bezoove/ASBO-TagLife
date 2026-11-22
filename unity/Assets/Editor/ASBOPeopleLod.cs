#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UMA;
using UMA.Editors;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Levels of detail for people: UMA's slot LOD generator run over every body, head, hair and clothing slot, so each
    /// carries 50%, 25% and 12% versions of itself. At runtime UMASimpleLOD (added to each resident by FreePack) picks
    /// the level by distance without rebuilding the character. Run once after importing UMA or new clothes.
    /// </summary>
    public static class ASBOPeopleLod
    {
        [MenuItem("ASBO/People/Generate Slot LODs")]
        public static void Menu() => Run(10000);
        public static void Run(int max)
        {
            var guids = AssetDatabase.FindAssets("t:SlotDataAsset");
            // Unity's own (native, fast) simplifier: UMA's custom one took many minutes a slot
            var opts = new SlotLodGenerator.LodGenOptions { MaxLodLevels = 4, MinTriangles = 150, TargetReductionPerLevel = 0.5f, PreserveBoundaryEdges = true, PreserveVolume = true, useUnityLodGenerator = true };
            int done = 0, had = 0, skipped = 0; var t0 = System.DateTime.Now;
            try
            {
                for (int i = 0; i < guids.Length && done < max; i++)
                {
                    var slot = AssetDatabase.LoadAssetAtPath<SlotDataAsset>(AssetDatabase.GUIDToAssetPath(guids[i]));
                    if (slot == null || UMAMeshData.IsNullOrEmptyMeshData(slot.meshData) || slot.meshData.submeshes == null || slot.meshData.submeshes.Length == 0) { skipped++; continue; }
                    int sub = Mathf.Clamp(slot.subMeshIndex, 0, slot.meshData.submeshes.Length - 1);
                    var sm = slot.meshData.submeshes[sub];
                    if (sm != null && sm.lodRanges != null && sm.lodRanges.Count > 1) { had++; continue; }
                    bool ok = false;
                    try { ok = SlotLodGenerator.GenerateAndApplyLods(slot, opts); } catch (System.Exception e) { Debug.LogWarning($"[ASBO] slot LOD {slot.name}: {e.Message}"); }
                    if (ok) { EditorUtility.SetDirty(slot); done++; } else skipped++;
                }
            }
            finally { AssetDatabase.SaveAssets(); }
            Debug.Log($"[ASBO] people LODs: {done} slots given LODs, {had} already had them, {skipped} skipped, {guids.Length} slots in all, {(System.DateTime.Now - t0).TotalSeconds:F0}s");
        }
    }
}
#endif
