using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Real scanned surfaces (CC0 photo textures): tarmac, paving slabs, kerbs, stone and brick walls, slate roofs,
    /// concrete, gravel, metal. Each is a URP Lit material kept in Resources/Surfaces (colour, normal and a metal /
    /// smoothness mask), made by the editor tool ASBO/Art/Apply Surface Textures. Builders ask for one by name and
    /// say how many metres one unit of their mesh UVs covers; the material is scaled so the texture shows at its
    /// real size (a paving slab is a paving slab wherever it is used).
    /// </summary>
    public static class Surfaces
    {
        /// <summary>Real-world size of one repeat of each texture, in metres (from each scan's own reference plane).</summary>
        public static readonly Dictionary<string, float> Size = new Dictionary<string, float>
        {
            ["asphalt_02"] = 3f, ["cobblestone_embedded_asphalt"] = 1.9f, ["cobblestone_floor_09"] = 2.17f, ["cobblestone_pavement"] = 2.5f,
            ["concrete_layers"] = 1.55f, ["dry_riverbed_rock"] = 2f, ["hangar_concrete_floor"] = 2f, ["large_grey_tiles"] = 2.4f,
            ["metal_plate"] = 0.5f, ["old_stone_wall"] = 2f, ["plaster_brick_pattern"] = 2f, ["plastered_wall_05"] = 2f,
            ["red_brick_plaster_patch_02"] = 1.5f, ["rock_wall_08"] = 1.8f, ["rocky_trail"] = 2f, ["roof_slates_03"] = 3f,
            ["rustic_stone_wall_02"] = 1.5f, ["stone_tile_wall"] = 2f, ["stone_wall_04"] = 1.7f,
            ["limewash_stone"] = 2f, ["plaster_smooth"] = 2f,
            ["dark_rock_02"] = 2f, ["rock_pitted_mossy"] = 1f, ["bark_brown_01"] = 1f, ["coated_pine"] = 0.74f,
            ["eucalyptus_bark"] = 1.81f, ["hinoki_planks"] = 1.89f, ["jolcham_oak_bark_01"] = 2f, ["sakura_bark"] = 1.6f,
        };

        static readonly Dictionary<string, Material> loaded = new Dictionary<string, Material>();

        /// <summary>Pretend no surfaces are installed (builders fall back to their own procedural materials).</summary>
        public static bool Off;

        /// <summary>The shared material asset (scale 1 = one repeat per UV unit), or null if it isn't in the build.</summary>
        public static Material Source(string name)
        {
            if (Off) return null;
            if (loaded.TryGetValue(name, out var m) && m != null) return m;
            m = Resources.Load<Material>("Surfaces/" + name);
            loaded[name] = m; return m;
        }

        /// <summary>
        /// A copy of the surface for a mesh whose UVs run <paramref name="uvMetres"/> metres per unit, optionally tinted.
        /// Returns null when the surface isn't available, so callers can fall back to their own material.
        /// </summary>
        public static Material Mat(string name, float uvMetres = 1f, Color? tint = null, float bump = 1f)
        {
            var src = Source(name); if (src == null) return null;
            var m = new Material(src) { name = name };
            float s = uvMetres / (Size.TryGetValue(name, out var sz) ? sz : 2f);
            m.SetTextureScale("_BaseMap", new Vector2(s, s));
            if (tint.HasValue) m.SetColor("_BaseColor", tint.Value);
            if (!Mathf.Approximately(bump, 1f) && m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", bump);
            return m;
        }

        /// <summary>
        /// A box mesh like Unity's cube (unit size, centred) whose first UV set is in metres for an object of the given
        /// scale, so a surface tiles at its real size on every face. The second UV set (lightmaps) is kept from the
        /// cube, so baked lighting on a swapped-in box still lines up.
        /// </summary>
        public static Mesh MetreCube(Vector3 scale)
        {
            if (cube == null)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube = g.GetComponent<MeshFilter>().sharedMesh;
                if (Application.isPlaying) Object.Destroy(g); else Object.DestroyImmediate(g);
            }
            var m = Object.Instantiate(cube); m.name = $"MetreCube {scale.x:0.##}x{scale.y:0.##}x{scale.z:0.##}";
            var v = m.vertices; var n = m.normals; var uv0 = m.uv;
            var uv2 = new List<Vector2>(); m.GetUVs(1, uv2);
            if (uv2.Count != v.Length) m.SetUVs(1, uv0); // the lightmapper fell back to UV0 for the cube; keep that layout
            var uv = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 p = Vector3.Scale(v[i], scale), a = new Vector3(Mathf.Abs(n[i].x), Mathf.Abs(n[i].y), Mathf.Abs(n[i].z));
                if (a.y > 0.5f) uv[i] = new Vector2(p.x, p.z);
                else if (a.x > 0.5f) uv[i] = new Vector2(p.z, p.y);
                else uv[i] = new Vector2(p.x, p.y);
            }
            m.uv = uv; m.RecalculateTangents(); m.RecalculateBounds();
            return m;
        }
        static Mesh cube;

        /// <summary>A box with a real surface on it: metre UVs, so the texture is the right size on every face.</summary>
        public static GameObject Box(Transform parent, Vector3 center, Vector3 size, string surface, Material fallback, string name, bool collider = true, Color? tint = null)
        {
            var mat = Mat(surface, 1f, tint) ?? fallback;
            var g = Gfx.Box(parent, center, size, mat, 0, name, collider);
            if (Source(surface) != null) g.GetComponent<MeshFilter>().sharedMesh = MetreCube(size);
            return g;
        }
    }
}
