#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using ASBO.Slice;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Builds Hawthorn Way (the council estate, <see cref="CouncilEstate"/>) into the playable scene: makes its
    /// materials (Resources/Estate) from the textures in tools/art/estate, levels the ground and clears field trees
    /// where it goes, builds the terraces, roads and lamps under the baked World, saves the meshes, plants trees on the
    /// green, rebakes the NavMesh with it in, and saves the scene. Run again any time: it replaces the old estate.
    /// Menu: ASBO/Art/Build Council Estate.
    /// </summary>
    public static class ASBOEstate
    {
        const string TestScene = "Assets/Scenes/AbbotsWend_Test.unity", Tex = "Assets/Art/Estate", MatDir = "Assets/Resources/Estate", MeshDir = "Assets/Art/Generated/Estate";
        const string SiteTex = "Assets/Art/Site", SiteMats = "Assets/Resources/Site", SiteMeshes = "Assets/Art/Generated/Site";
        /// <summary>Ground levelled for the building site (x, z), a little bigger than its fence.</summary>
        static readonly Rect SiteGround = new Rect(90f, -102f, 66f, 64f);
        /// <summary>Ground levelled for the high street: the north parade with the beer garden behind it, and the south parade.</summary>
        static readonly Rect HighNorth = new Rect(-140f, -24f, 94f, 28f), HighSouth = new Rect(-147f, -52f, 65f, 16f);
        /// <summary>Hawthorn Parade on the estate green (x, z footprint, with a margin).</summary>
        static readonly Rect EstateParade = new Rect(-34f, -73f, 12.5f, 30f);

        [MenuItem("ASBO/Art/Build Council Estate")]
        public static void Build()
        {
            ASBOSurfaces.ImportAndMake();
            Materials();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != TestScene) scene = EditorSceneManager.OpenScene(TestScene, OpenSceneMode.Single);
            var world = Object.FindFirstObjectByType<BakedWorld>();
            if (world == null) { Debug.LogError("[ASBO] estate: no baked World in " + TestScene); return; }
            var old = world.transform.Find(CouncilEstate.RootName); if (old != null) Object.DestroyImmediate(old.gameObject);
            var oldSite = world.transform.Find(ConstructionSite.RootName); if (oldSite != null) Object.DestroyImmediate(oldSite.gameObject);
            var oldCastle = world.transform.Find(Castle.RootName); if (oldCastle != null) Object.DestroyImmediate(oldCastle.gameObject);
            var oldHigh = world.transform.Find(HighStreet.RootName); if (oldHigh != null) Object.DestroyImmediate(oldHigh.gameObject);
            var oldCasino = world.transform.Find(CasinoHall.RootName); if (oldCasino != null) Object.DestroyImmediate(oldCasino.gameObject);
            var oldManor = world.transform.Find(TudorManor.RootName); if (oldManor != null) Object.DestroyImmediate(oldManor.gameObject);
            CasinoMaterials();
            var terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain != null) Ground(terrain);
            var root = CouncilEstate.Build(world.transform);
            if (root == null) return;
            var site = ConstructionSite.Build(world.transform);
            var castle = Castle.Build(world.transform, terrain);
            var high = HighStreet.Build(world.transform);
            var casino = CasinoHall.Build(world.transform);
            var manor = TudorManor.Build(world.transform);
            SaveMeshes(root, MeshDir);
            if (manor != null) SaveMeshes(manor, "Assets/Art/Generated/Manor");
            if (site != null) SaveMeshes(site, SiteMeshes);
            if (castle != null) SaveMeshes(castle, "Assets/Art/Generated/Castle");
            if (high != null) SaveMeshes(high, "Assets/Art/Generated/HighStreet");
            if (casino != null) SaveMeshes(casino, "Assets/Art/Generated/Casino");
            var built = new List<Transform> { root }; if (site != null) built.Add(site); if (castle != null) built.Add(castle); if (high != null) built.Add(high); if (casino != null) built.Add(casino); if (manor != null) built.Add(manor);
            foreach (var t in built)
                foreach (var r in t.GetComponentsInChildren<MeshRenderer>(true))
                {
                    bool moves = r.GetComponentInParent<Transform>() != null && r.transform.parent != null && r.transform.parent.name == "CraneTop";
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, moves ? 0 : StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
                    r.receiveGI = ReceiveGI.LightProbes; r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
                }
            if (terrain != null) Trees(terrain);
            Probes(world.transform);
            // NavMesh with the estate in it (its own asset, so the original Abbots Wend scene keeps its own)
            var surf = world.GetComponent<NavMeshSurface>();
            if (surf != null)
            {
                surf.BuildNavMesh();
                const string navPath = "Assets/Art/Generated/NavMesh_Test.asset";
                if (File.Exists(navPath)) AssetDatabase.DeleteAsset(navPath);
                AssetDatabase.CreateAsset(surf.navMeshData, navPath);
                EditorUtility.SetDirty(surf);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[ASBO] estate: built into " + TestScene);
        }

        // ------------------------------------------------------------------------------ materials
        static void Materials()
        {
            Directory.CreateDirectory(MatDir);
            foreach (var f in Directory.GetFiles(Tex, "*.png"))
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath(f.Replace('\\', '/'));
                bool win = f.Contains("estate_windows"), rail = f.Contains("railing");
                imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.mipmapEnabled = true; imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = 4;
                imp.alphaSource = win || rail ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None; imp.alphaIsTransparency = rail;
                imp.wrapMode = rail ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = win ? 1024 : 512, format = TextureImporterFormat.ASTC_6x6 });
                imp.SaveAndReimport();
            }
            Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}/{n}.png");
            Material Plain(string n, Color c, float smooth, float metal = 0f, Texture2D tex = null)
            {
                return Mat(n, null, m => { m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal); if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; } });
            }
            Material From(string n, string surface, Color tint) => Mat(n, Surfaces.Source(surface), m => { m.SetColor("_BaseColor", tint); });
            From("EstateRoof", "roof_slates_03", new Color(0.66f, 0.6f, 0.56f));
            From("EstateBrick", "buff_brick", new Color(1.2f, 1.16f, 1.06f));   // the light buff of the photos, a touch brighter than the raw scan
            From("EstateCream", "plaster_smooth", new Color(1.16f, 1.11f, 0.95f));
            From("EstateDrive", "hangar_concrete_floor", new Color(0.92f, 0.91f, 0.88f));
            From("EstateKerb", "concrete_layers", new Color(1.2f, 1.19f, 1.17f));
            From("EstatePave", "large_grey_tiles", new Color(0.88f, 0.88f, 0.86f));
            From("EstateFence", "hinoki_planks", new Color(0.66f, 0.48f, 0.34f));
            From("EstateStep", "concrete_layers", new Color(1.1f, 1.1f, 1.08f));
            Plain("EstateRidge", new Color(0.26f, 0.25f, 0.25f), 0.2f);
            Plain("EstateWhite", new Color(0.9f, 0.9f, 0.88f), 0.45f);
            Plain("EstateBlack", new Color(0.04f, 0.04f, 0.045f), 0.5f);
            Plain("EstatePost", new Color(0.03f, 0.03f, 0.03f), 0.4f);
            Plain("EstateLampColumn", new Color(0.5f, 0.52f, 0.54f), 0.4f, 0.6f);
            Plain("EstateLine", new Color(0.92f, 0.92f, 0.88f), 0.1f);
            Plain("EstateDoors", Color.white, 0.5f, 0f, T("estate_doors"));
            Plain("EstateSign", Color.white, 0.3f, 0f, T("sign_hawthorn_way_1_28"));
            var gd = T("garage_door");
            Plain("EstateGarageBlue", new Color(0.2f, 0.42f, 0.86f), 0.45f, 0.3f, gd);
            Plain("EstateGarageRed", new Color(0.82f, 0.13f, 0.11f), 0.45f, 0.3f, gd);
            Plain("EstateGarageWhite", new Color(1f, 1f, 0.98f), 0.45f, 0.3f, gd);
            Plain("EstateGarageBrown", new Color(0.56f, 0.37f, 0.23f), 0.4f, 0.3f, gd);
            From("EstateConcrete", "concrete_layers", new Color(1.32f, 1.31f, 1.27f));   // board-marked concrete, light grey
            From("EstateTowerPanel", "plaster_smooth", new Color(0.58f, 0.66f, 0.74f));
            Plain("EstateFelt", new Color(0.1f, 0.1f, 0.11f), 0.15f);
            Plain("EstateRubber", new Color(0.18f, 0.32f, 0.18f), 0.08f);
            Plain("EstateRailGreen", new Color(0.08f, 0.26f, 0.13f), 0.45f, 0.3f);
            Plain("EstatePlayRed", new Color(0.78f, 0.12f, 0.1f), 0.55f, 0.15f);
            Plain("EstatePlayYellow", new Color(0.95f, 0.74f, 0.08f), 0.55f, 0.15f);
            Plain("EstatePlayBlue", new Color(0.1f, 0.3f, 0.72f), 0.55f, 0.15f);
            Plain("EstateCourtSign", Color.white, 0.3f, 0f, T("sign_hawthorn_court"));
            Mat("EstateRailing", null, m =>
            {
                var t = T("railing"); m.SetTexture("_BaseMap", t); m.mainTexture = t; m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.4f); m.SetFloat("_Metallic", 0.3f);
                m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cull", 0f); m.renderQueue = 2450; m.SetOverrideTag("RenderType", "TransparentCutout");
            });
            SiteMaterials();
            CastleMaterials();
            TownMaterials();
            Mat("EstateWindows", null, m =>
            {
                var t = T("estate_windows"); m.SetTexture("_BaseMap", t); m.mainTexture = t; m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Metallic", 0f); m.SetFloat("_Smoothness", 1f); m.SetFloat("_SmoothnessTextureChannel", 1f); m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            });
            Mat("EstateLampGlow", null, m =>
            {
                m.SetColor("_BaseColor", new Color(1f, 0.85f, 0.6f)); m.SetColor("_EmissionColor", new Color(1f, 0.74f, 0.42f) * 1.6f); m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            });
            AssetDatabase.SaveAssets();
        }

        /// <summary>The building site's materials (Resources/Site), from tools/art/site textures and the scanned surfaces.</summary>
        static void SiteMaterials()
        {
            Directory.CreateDirectory(SiteMats);
            if (Directory.Exists(SiteTex))
                foreach (var f in Directory.GetFiles(SiteTex, "*.png"))
                {
                    var imp = (TextureImporter)AssetImporter.GetAtPath(f.Replace('\\', '/')); if (imp == null) continue;
                    bool cut = f.Contains("heras") || f.Contains("lattice");
                    imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.mipmapEnabled = true; imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = 4;
                    imp.alphaSource = cut ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None; imp.alphaIsTransparency = cut;
                    imp.wrapMode = TextureWrapMode.Repeat;
                    imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = 512, format = TextureImporterFormat.ASTC_6x6 });
                    imp.SaveAndReimport();
                }
            Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{SiteTex}/{n}.png");
            Material S(string n, Material from, System.Action<Material> set) => Mat(n, from, set, SiteMats);
            void Plain(string n, Color c, float smooth, float metal = 0f, Texture2D tex = null) => S(n, null, m => { m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal); if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; } });
            void Cut(string n, Texture2D tex, float smooth, float metal)
            {
                S(n, null, m =>
                {
                    m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
                    m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cull", 0f); m.renderQueue = 2450; m.SetOverrideTag("RenderType", "TransparentCutout");
                });
            }
            S("SiteMud", Surfaces.Source("rocky_trail"), m => m.SetColor("_BaseColor", new Color(0.66f, 0.54f, 0.42f)));
            S("SiteHardcore", Surfaces.Source("dry_riverbed_rock"), m => m.SetColor("_BaseColor", new Color(0.86f, 0.85f, 0.82f)));
            S("SiteBlocks", Surfaces.Source("stone_tile_wall"), m => m.SetColor("_BaseColor", new Color(0.92f, 0.92f, 0.9f)));
            Plain("SiteRebar", new Color(0.28f, 0.14f, 0.07f), 0.3f, 0.5f);
            Plain("SiteBoards", Color.white, 0.15f, 0f, T("boards"));
            Plain("SiteHoarding", Color.white, 0.3f, 0f, T("hoarding"));
            Plain("SiteSafety", Color.white, 0.35f, 0f, T("safety"));
            Cut("SiteHeras", T("heras"), 0.5f, 0.6f);
            Cut("SiteLattice", T("lattice"), 0.5f, 0.2f);
            Plain("SiteYellow", new Color(0.95f, 0.7f, 0.06f), 0.5f, 0.1f);
            Plain("SiteCabin", new Color(0.24f, 0.34f, 0.46f), 0.35f, 0.2f);
            Plain("SiteCabinTrim", new Color(0.86f, 0.86f, 0.84f), 0.35f);
            Plain("SiteBlue", new Color(0.1f, 0.32f, 0.72f), 0.45f);
            Plain("SiteSand", new Color(0.8f, 0.68f, 0.46f), 0.05f);
            Plain("SitePuddle", new Color(0.16f, 0.13f, 0.1f), 0.88f);
        }

        static void CastleMaterials()
        {
            const string dir = "Assets/Resources/Castle"; Directory.CreateDirectory(dir);
            Mat("CastleStone", Surfaces.Source("old_stone_wall"), m => m.SetColor("_BaseColor", new Color(0.7f, 0.72f, 0.68f)), dir);
            Mat("CastleStone2", Surfaces.Source("rustic_stone_wall_02"), m => m.SetColor("_BaseColor", new Color(0.64f, 0.66f, 0.62f)), dir);
            Mat("CastleGravel", Surfaces.Source("dry_riverbed_rock"), m => m.SetColor("_BaseColor", new Color(0.8f, 0.78f, 0.74f)), dir);
            Mat("CastleDark", null, m => { m.SetColor("_BaseColor", new Color(0.008f, 0.008f, 0.01f)); m.SetFloat("_Smoothness", 0f); m.SetFloat("_Metallic", 0f); }, dir);
            Mat("CastleIron", null, m => { m.SetColor("_BaseColor", new Color(0.12f, 0.11f, 0.1f)); m.SetFloat("_Smoothness", 0.4f); m.SetFloat("_Metallic", 0.6f); }, dir);
        }

        static void TownMaterials()
        {
            const string dir = "Assets/Resources/Town", tex = "Assets/Art/Town"; Directory.CreateDirectory(dir);
            if (Directory.Exists(tex))
                foreach (var f in Directory.GetFiles(tex, "*.png"))
                {
                    var imp = (TextureImporter)AssetImporter.GetAtPath(f.Replace('\\', '/')); if (imp == null) continue;
                    bool sash = f.Contains("sash"), tiles = f.Contains("tiles"), big = f.Contains("shopfronts");
                    imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.mipmapEnabled = true; imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = 4;
                    imp.alphaSource = sash ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None; imp.alphaIsTransparency = false;
                    imp.wrapMode = tiles ? TextureWrapMode.Repeat : TextureWrapMode.Clamp; imp.maxTextureSize = 2048;
                    imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = big ? 2048 : 512, format = TextureImporterFormat.ASTC_6x6 });
                    imp.SaveAndReimport();
                }
            Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/{n}.png");
            void Plain(string n, Color c, float smooth, float metal = 0f, Texture2D t = null) => Mat(n, null, m => { m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal); if (t != null) { m.SetTexture("_BaseMap", t); m.mainTexture = t; } }, dir);
            Mat("TownBrick", Surfaces.Source("red_brick_vic"), m => m.SetColor("_BaseColor", new Color(1.05f, 1.02f, 1.0f)), dir);
            Mat("TownStone", Surfaces.Source("plaster_smooth"), m => m.SetColor("_BaseColor", new Color(0.9f, 0.87f, 0.8f)), dir);
            Mat("TownRender", Surfaces.Source("plaster_smooth"), m => m.SetColor("_BaseColor", new Color(1.14f, 1.13f, 1.09f)), dir);
            Mat("TownSlate", Surfaces.Source("roof_slates_03"), m => m.SetColor("_BaseColor", new Color(0.66f, 0.68f, 0.74f)), dir);
            Mat("TownSash", null, m =>
            {
                var t = T("sash"); m.SetTexture("_BaseMap", t); m.mainTexture = t; m.SetColor("_BaseColor", Color.white); m.SetFloat("_Metallic", 0f); m.SetFloat("_Smoothness", 1f);
                m.SetFloat("_SmoothnessTextureChannel", 1f); m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            }, dir);
            Plain("TownShops", Color.white, 0.6f, 0f, T("shopfronts"));
            Plain("TownPubTiles", Color.white, 0.8f, 0f, T("pub_tiles"));
            Plain("TownPubSign", Color.white, 0.35f, 0f, T("pub_sign"));
            Plain("TownPaint", new Color(0.07f, 0.2f, 0.13f), 0.7f);
            Plain("TownFrame", new Color(0.16f, 0.16f, 0.18f), 0.5f, 0.2f);
            Plain("TownPot", new Color(0.55f, 0.28f, 0.18f), 0.25f);
        }

        /// <summary>The Silver Sixpence: carpet, wallpaper and the lit signs from tools/art/casino, plus plain felt, wood, gold and cladding.</summary>
        static void CasinoMaterials()
        {
            const string dir = "Assets/Resources/Casino", tex = "Assets/Art/Casino"; Directory.CreateDirectory(dir);
            if (Directory.Exists(tex))
                foreach (var f in Directory.GetFiles(tex, "*.png"))
                {
                    var imp = (TextureImporter)AssetImporter.GetAtPath(f.Replace('\\', '/')); if (imp == null) continue;
                    bool tiles = f.Contains("carpet") || f.Contains("wallpaper");
                    imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.mipmapEnabled = true; imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = 4;
                    imp.alphaSource = TextureImporterAlphaSource.None; imp.wrapMode = tiles ? TextureWrapMode.Repeat : TextureWrapMode.Clamp; imp.maxTextureSize = 2048;
                    imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = f.Contains("sign") ? 1024 : 512, format = TextureImporterFormat.ASTC_6x6 });
                    imp.SaveAndReimport();
                }
            Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/{n}.png");
            void Plain(string n, Color c, float smooth, float metal = 0f, Texture2D t = null) => Mat(n, null, m => { m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal); if (t != null) { m.SetTexture("_BaseMap", t); m.mainTexture = t; } }, dir);
            void Lit(string n, Texture2D t, Color glow) => Mat(n, null, m =>
            {
                m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.5f); m.SetFloat("_Metallic", 0f);
                if (t != null) { m.SetTexture("_BaseMap", t); m.mainTexture = t; m.SetTexture("_EmissionMap", t); }
                m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", glow); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }, dir);
            Plain("CasinoCarpet", Color.white, 0.05f, 0f, T("carpet"));
            Plain("CasinoWall", Color.white, 0.15f, 0f, T("wallpaper"));
            Lit("CasinoSign", T("sign"), new Color(1.6f, 1.5f, 1.4f));
            Lit("CasinoPylon", T("pylon"), new Color(1.5f, 1.4f, 1.4f));
            Lit("CasinoSlots", T("slots"), new Color(0.9f, 0.85f, 0.8f));
            Lit("CasinoLight", null, new Color(2.2f, 1.95f, 1.6f));
            Plain("CasinoRoulette", Color.white, 0.1f, 0f, T("roulette_felt"));
            Plain("CasinoBlackjack", Color.white, 0.1f, 0f, T("blackjack_felt"));
            Plain("CasinoFelt", new Color(0.07f, 0.36f, 0.19f), 0.08f);
            Plain("CasinoWood", new Color(0.28f, 0.11f, 0.06f), 0.7f);
            Plain("CasinoGold", new Color(0.85f, 0.66f, 0.28f), 0.75f, 0.9f);
            Plain("CasinoChrome", new Color(0.72f, 0.74f, 0.78f), 0.7f, 0.8f);
            Plain("CasinoClad", new Color(0.62f, 0.64f, 0.67f), 0.45f, 0.35f);
            Plain("CasinoNavy", new Color(0.05f, 0.06f, 0.14f), 0.4f);
            Plain("CasinoGlass", new Color(0.06f, 0.08f, 0.1f), 0.95f, 0.2f);
            Plain("CasinoCeiling", new Color(0.08f, 0.05f, 0.05f), 0.2f);
            Plain("CasinoCabinet", new Color(0.5f, 0.04f, 0.06f), 0.8f, 0.1f);
            // the bottles behind the bar: a strip of coloured glass, drawn here
            var bt = new Texture2D(256, 64, TextureFormat.RGBA32, true); var rr = new System.Random(4);
            for (int x = 0; x < 256; x++) for (int y = 0; y < 64; y++) bt.SetPixel(x, y, new Color(0.18f, 0.08f, 0.05f));
            for (int b = 0; b < 24; b++)
            {
                int bx = b * 10 + 4; var col = new[] { new Color(0.15f, 0.45f, 0.15f), new Color(0.6f, 0.35f, 0.08f), new Color(0.85f, 0.85f, 0.8f), new Color(0.5f, 0.1f, 0.1f), new Color(0.2f, 0.25f, 0.6f) }[rr.Next(5)];
                int top = 26 + rr.Next(18) + (b % 2) * 0; for (int x = bx; x < bx + 7; x++) for (int y = 4; y < top; y++) bt.SetPixel(x, y, col * (x == bx + 1 ? 1.4f : 1f));
                for (int x = bx + 2; x < bx + 5; x++) for (int y = top; y < top + 8; y++) bt.SetPixel(x, y, col);
                if (b % 12 == 0) for (int x = 0; x < 256; x++) bt.SetPixel(x, 2, new Color(0.5f, 0.4f, 0.2f));
            }
            bt.Apply(); var bp = tex + "/bottles_gen.png"; Directory.CreateDirectory(tex); File.WriteAllBytes(bp, bt.EncodeToPNG()); AssetDatabase.ImportAsset(bp);
            var bimp = (TextureImporter)AssetImporter.GetAtPath(bp); bimp.wrapMode = TextureWrapMode.Repeat; bimp.SaveAndReimport();
            Lit("CasinoBottles", AssetDatabase.LoadAssetAtPath<Texture2D>(bp), new Color(0.35f, 0.32f, 0.3f));
        }

        static Material Mat(string name, Material from, System.Action<Material> set) => Mat(name, from, set, MatDir);
        static Material Mat(string name, Material from, System.Action<Material> set, string dir)
        {
            string path = $"{dir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null && from != null) { m.CopyPropertiesFromMaterial(from); m.shaderKeywords = from.shaderKeywords; }
            if (m == null) { m = from != null ? new Material(from) { name = name } : new Material(Gfx.Lit) { name = name }; AssetDatabase.CreateAsset(m, path); }
            set(m); m.enableInstancing = true; EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------------------ meshes
        static void SaveMeshes(Transform root, string dir)
        {
            if (AssetDatabase.IsValidFolder(dir)) AssetDatabase.DeleteAsset(dir);
            Directory.CreateDirectory(dir); AssetDatabase.Refresh();
            int n = 0;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh; if (mesh == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh))) continue;
                AssetDatabase.CreateAsset(mesh, $"{dir}/{mf.transform.parent.name.Replace(' ', '_')}_{mf.name}_{n++}.asset");
            }
            Debug.Log($"[ASBO] estate: saved {n} meshes");
        }

        // ------------------------------------------------------------------------------ ground and trees
        /// <summary>Level the fields where the estate goes (smoothly back up to the hills), and clear field trees and long grass off it.</summary>
        static void Ground(Terrain t)
        {
            var td = t.terrainData; Vector3 tp = t.transform.position, size = td.size;
            int res = td.heightmapResolution; var h = td.GetHeights(0, 0, res, res);
            // the castle hill first (so the estate's levelling still wins where they meet)
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float wx = tp.x + x / (float)(res - 1) * size.x, wz = tp.z + y / (float)(res - 1) * size.z;
                    float hh = Castle.HillHeight(wx, wz); if (hh <= 0f) continue;
                    float target = (hh - tp.y) / size.y;
                    h[y, x] = Vector2.Distance(new Vector2(wx, wz), new Vector2(Castle.Centre.x, Castle.Centre.z)) <= Castle.Plateau ? target : Mathf.Max(h[y, x], target);
                }
            // the main road runs the whole length of the map: keep a level corridor along it (the fields were burying it out past the village)
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float wz = tp.z + y / (float)(res - 1) * size.z;
                    float d = Mathf.Max(0f, Mathf.Abs(wz - World.RoadZ) - 9f); if (d >= 22f) continue;
                    h[y, x] = Mathf.Lerp(-tp.y / size.y, h[y, x], Mathf.SmoothStep(0f, 1f, d / 22f));
                }
            var area = CouncilEstate.Area; const float fall = 28f;
            float Dist(Rect r, float wx, float wz) { float dx = Mathf.Max(0f, Mathf.Max(r.xMin - wx, wx - r.xMax)), dz = Mathf.Max(0f, Mathf.Max(r.yMin - wz, wz - r.yMax)); return Mathf.Sqrt(dx * dx + dz * dz); }
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float wx = tp.x + x / (float)(res - 1) * size.x, wz = tp.z + y / (float)(res - 1) * size.z;
                    float d = Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(Dist(area, wx, wz), Dist(SiteGround, wx, wz)), Mathf.Min(Dist(HighNorth, wx, wz), Dist(HighSouth, wx, wz))), Dist(CasinoHall.Plot, wx, wz)), Dist(TudorManor.Plot, wx, wz)); if (d >= fall) continue;
                    float k = Mathf.SmoothStep(0f, 1f, d / fall);
                    h[y, x] = Mathf.Lerp(-tp.y / size.y, h[y, x], k);
                }
            td.SetHeights(0, 0, h);
            // field trees inside the estate go
            var keep = new List<TreeInstance>();
            foreach (var ti in td.treeInstances)
            {
                float wx = tp.x + ti.position.x * size.x, wz = tp.z + ti.position.z * size.z;
                if (wx > area.xMin - 3f && wx < area.xMax + 3f && wz > area.yMin - 3f && wz < area.yMax + 3f) continue;
                if (wx > SiteGround.xMin - 3f && wx < SiteGround.xMax + 3f && wz > SiteGround.yMin - 3f && wz < SiteGround.yMax + 3f) continue;
                if (Vector2.Distance(new Vector2(wx, wz), new Vector2(Castle.Centre.x, Castle.Centre.z)) < Castle.Plateau + 4f || NearLane(wx, wz, 4f)) continue;
                if (Mathf.Abs(wz - World.RoadZ) < 10f) continue; // nothing growing in the road
                if (HighNorth.Contains(new Vector2(wx, wz)) || HighSouth.Contains(new Vector2(wx, wz))) continue;
                if (Grow(CasinoHall.Plot, 4f).Contains(new Vector2(wx, wz))) continue; // the casino and its car park
                if (Grow(TudorManor.Plot, 3f).Contains(new Vector2(wx, wz))) continue; // the Eggy Duck and its grounds
                keep.Add(ti);
            }
            td.treeInstances = keep.ToArray();
            // long grass off the roads, drives and house plots (it stays on the green and in the back gardens)
            int dr = td.detailResolution;
            var houses = CouncilEstate.Blocks; var c = CouncilEstate.Centreline();
            for (int layer = 0; layer < td.detailPrototypes.Length; layer++)
            {
                var map = td.GetDetailLayer(0, 0, dr, dr, layer); int cleared = 0;
                for (int y = 0; y < dr; y++)
                    for (int x = 0; x < dr; x++)
                    {
                        if (map[y, x] == 0) continue;
                        float wx = tp.x + (x + 0.5f) / dr * size.x, wz = tp.z + (y + 0.5f) / dr * size.z;
                        if (HighNorth.Contains(new Vector2(wx, wz)) || HighSouth.Contains(new Vector2(wx, wz)) || CasinoHall.Plot.Contains(new Vector2(wx, wz))) { map[y, x] = 0; cleared++; continue; }
                        if (new Rect(TudorManor.Origin.x - 6.5f, World.RoadZ + 7f, 45f, TudorManor.Origin.z + 22f - World.RoadZ - 7f).Contains(new Vector2(wx, wz))) { map[y, x] = 0; cleared++; continue; } // the manor and its car park (the beer garden keeps its grass)
                        if (ConstructionSite.Area.Contains(new Vector2(wx, wz)) || wx > ConstructionSite.GateX0 - 1f && wx < ConstructionSite.GateX1 + 1f && wz < -34f && wz > ConstructionSite.FrontZ - 1f) { map[y, x] = 0; cleared++; continue; }
                        if (wx < area.xMin || wx > area.xMax || wz < area.yMin || wz > area.yMax) continue;
                        if (Paved(new Vector3(wx, 0, wz), c)) { map[y, x] = 0; cleared++; }
                    }
                td.SetDetailLayer(0, 0, layer, map);
            }
            EditorUtility.SetDirty(td);
        }

        static bool Paved(Vector3 p, List<Vector3> c)
        {
            // near the loop road
            for (int i = 0; i < c.Count - 1; i++)
            {
                Vector3 a = c[i], b = c[i + 1], ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
                if ((a + ab * t - p).magnitude < CouncilEstate.RoadHalf + CouncilEstate.KerbW + CouncilEstate.PaveW + 0.6f) return true;
            }
            if (Mathf.Abs(p.x) < 1.6f && p.z < -37f && p.z > -105f) return true; // the footpath
            if (Grow(CouncilEstate.Tower, 3.2f).Contains(new Vector2(p.x, p.z))) return true;
            if (new Rect(CouncilEstate.GaragesX - 1f, CouncilEstate.GaragesZ - CouncilEstate.GarageD - 0.5f, CouncilEstate.GarageCount * CouncilEstate.GarageW + 2f, CouncilEstate.GarageD + 8f).Contains(new Vector2(p.x, p.z))) return true;
            if (Grow(CouncilEstate.Playground, 0.6f).Contains(new Vector2(p.x, p.z))) return true;
            if (EstateParade.Contains(new Vector2(p.x, p.z))) return true;
            if (p.x > 0.5f && p.x < CouncilEstate.Tower.xMin && Mathf.Abs(p.z - CouncilEstate.Tower.center.y) < 2f) return true; // path to the tower
            foreach (var b in CouncilEstate.Blocks)
            {
                var l = Quaternion.Inverse(Quaternion.Euler(0, b.yaw, 0)) * (p - b.origin);
                if (l.x > -0.6f && l.x < b.n * CouncilEstate.W + 0.6f && l.z < CouncilEstate.Forecourt + 0.4f && l.z > -CouncilEstate.D - 2.9f) return true;
            }
            return false;
        }

        static bool NearLane(float x, float z, float d)
        {
            var p = new Vector2(x, z); var L = Castle.Lane;
            for (int i = 0; i < L.Length - 1; i++)
            {
                Vector2 a = L[i], ab = L[i + 1] - L[i]; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                if (Vector2.Distance(a + ab * t, p) < d) return true;
            }
            return false;
        }

        static Rect Grow(Rect r, float m) => new Rect(r.xMin - m, r.yMin - m, r.width + 2f * m, r.height + 2f * m);

        /// <summary>Mature trees round the green: a row inside each pavement and a few loose on the grass, kept off the footpath.</summary>
        static void Trees(Terrain t)
        {
            var td = t.terrainData; if (td.treePrototypes.Length == 0) return;
            Vector3 tp = t.transform.position, size = td.size;
            var list = new List<TreeInstance>(td.treeInstances); var rng = new System.Random(77);
            void Add(float x, float z)
            {
                if (Mathf.Abs(x) < 4f) return;
                var q = new Vector2(x, z);
                if (Grow(CouncilEstate.Tower, 6f).Contains(q) || Grow(CouncilEstate.Playground, 3f).Contains(q) || Grow(EstateParade, 3f).Contains(q)) return;
                if (new Rect(CouncilEstate.GaragesX - 3f, CouncilEstate.GaragesZ - CouncilEstate.GarageD - 3f, CouncilEstate.GarageCount * CouncilEstate.GarageW + 6f, CouncilEstate.GarageD + 11f).Contains(q)) return;
                var ti = new TreeInstance
                {
                    position = new Vector3((x - tp.x) / size.x, 0f, (z - tp.z) / size.z), prototypeIndex = rng.Next(td.treePrototypes.Length),
                    widthScale = 0.85f + (float)rng.NextDouble() * 0.35f, color = Color.white, lightmapColor = Color.white, rotation = (float)rng.NextDouble() * Mathf.PI * 2f,
                };
                ti.heightScale = ti.widthScale * (0.95f + (float)rng.NextDouble() * 0.15f);
                list.Add(ti);
            }
            for (float z = -48f; z > -98f; z -= 12.5f) { Add(-29.5f + (float)rng.NextDouble(), z); Add(29.5f - (float)rng.NextDouble(), z - 5f); }
            for (float x = -22f; x < 24f; x += 13f) Add(x + (float)rng.NextDouble() * 2f, -98.5f);
            Add(-12f, -66f); Add(14f, -78f); Add(-18f, -84f); Add(8f, -46f);
            td.treeInstances = list.ToArray();
            t.Flush();
        }

        static void Probes(Transform world)
        {
            var old = world.Find("EstateProbes"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var pts = new List<Vector3>();
            for (float x = -76f; x <= 76f; x += 8f) for (float z = -136f; z <= -40f; z += 8f) { pts.Add(new Vector3(x, 0.8f, z)); pts.Add(new Vector3(x, 4f, z)); }
            var g = new GameObject("EstateProbes"); g.transform.SetParent(world, false);
            g.AddComponent<LightProbeGroup>().probePositions = pts.ToArray();
        }
    }
}
#endif
