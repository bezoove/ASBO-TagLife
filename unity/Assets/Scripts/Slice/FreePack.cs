using System.Collections.Generic;
using System.Linq;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

namespace ASBO.Slice
{
    [System.Serializable] public class LocoSpeeds { public float walk = 1.4f, run = 3.4f, sprint = 5.5f, turnSecs = 0.8f; }

    /// <summary>Builds UMA (Unity Multipurpose Avatar) people with real modelled clothing for the game's Person.</summary>
    public static class UmaBody
    {
        static RaceData male, female;
        static List<UMAWardrobeRecipe> all;
        static readonly Dictionary<string, RuntimeAnimatorController> ctrls = new Dictionary<string, RuntimeAnimatorController>();
        static readonly Dictionary<string, LocoSpeeds> speeds = new Dictionary<string, LocoSpeeds>();

        public static LocoSpeeds Speeds(bool isMale)
        {
            string k = isMale ? "M" : "F";
            if (!speeds.TryGetValue(k, out var s))
            {
                var ta = Resources.Load<TextAsset>("Anim/LocoSpeeds" + k);
                s = ta != null ? JsonUtility.FromJson<LocoSpeeds>(ta.text) : new LocoSpeeds();
                speeds[k] = s;
            }
            return s;
        }

        static void Init()
        {
            if (all != null) return;
            var idx = UMAAssetIndexer.Instance;
            var races = idx.GetAllAssets<RaceData>();
            bool Human(RaceData r) => r != null && r.raceName.ToLowerInvariant().Contains("human");
            male = races.Where(r => Human(r) && r.raceName.ToLowerInvariant().Contains("male") && !r.raceName.ToLowerInvariant().Contains("female")).OrderByDescending(r => r.raceName.Contains("3")).FirstOrDefault();
            female = races.Where(r => Human(r) && r.raceName.ToLowerInvariant().Contains("female")).OrderByDescending(r => r.raceName.Contains("3")).FirstOrDefault();
            all = idx.GetAllAssets<UMAWardrobeRecipe>().Where(r => r != null).ToList();
            Debug.Log("[ASBO] UMA races: " + (male ? male.raceName : "none") + " / " + (female ? female.raceName : "none") + ", wardrobe recipes: " + all.Count);
        }

        static readonly string[] Banned = { "underwear", "underpants", "bra", "swim", "challenger", "robe", "wrapped", "tights", "anime", "pigtail", "tornado", "pointy", "elf", "orc", "sylvan", "sprite", "glove", "shouder", "shoulder", "lace", "tanktop_zebra" };

        static List<UMAWardrobeRecipe> Pool(RaceData race, string slot, params string[] mustContainAny)
        {
            return all.Where(r => string.Equals(r.wardrobeSlot, slot, System.StringComparison.OrdinalIgnoreCase)
                && (r.compatibleRaces == null || r.compatibleRaces.Count == 0 || r.compatibleRaces.Contains(race.raceName))
                && !Banned.Any(b => r.name.ToLowerInvariant().Contains(b))
                && (mustContainAny.Length == 0 || mustContainAny.Any(m => r.name.ToLowerInvariant().Contains(m)))).ToList();
        }

        static T Pick<T>(List<T> l, System.Random rng) => l.Count == 0 ? default : l[rng.Next(l.Count)];

        /// <summary>Create the UMA avatar under the person. onReady fires once the mesh, skeleton and Animator exist.</summary>
        public static DynamicCharacterAvatar Create(Person p, bool isMale, int seed, bool isPlayer, System.Action<UMAData> onReady)
        {
            Init();
            var race = isMale ? male : female;
            if (race == null) return null;
            var rng = new System.Random(seed);
            var go = new GameObject("Body"); go.SetActive(false);
            go.transform.SetParent(p.transform, false);
            var dca = go.AddComponent<DynamicCharacterAvatar>();
            dca.RacePreset = race.raceName;
            bool stock = PlayerPrefs.GetInt("asbo_stock", 0) == 1; // before/after comparison: UMA defaults, no foot IK
            string k = (isMale ? "M" : "F") + (stock ? "_Stock" : "");
            if (!ctrls.TryGetValue(k, out var ctrl)) { ctrl = Resources.Load<RuntimeAnimatorController>("Anim/Loco" + k); ctrls[k] = ctrl; }
            dca.raceAnimationControllers.defaultAnimationController = ctrl;
            // body: legs a little closer under the pelvis than UMA's default (0.5)
            if (!stock)
            {
                dca.predefinedDNA = new UMAPredefinedDNA();
                dca.predefinedDNA.AddDNA("legSeparation", 0.36f + (float)rng.NextDouble() * 0.06f);
            }
            dca.applyRootMotion = false;
            dca.animatorCullingMode = isPlayer ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullUpdateTransforms;

            var wear = new List<UMAWardrobeRecipe>();
            if (isPlayer)
            {
                // the player: hoodie, trackies and trainers
                wear.Add(Pick(Pool(race, "Chest", "hoodie_grey", "hoodie"), rng));
                wear.Add(Pick(Pool(race, "Legs", "sweatpants", "sportpants"), rng));
                wear.Add(Pick(Pool(race, "Feet", "shoe_low", "shoe"), rng));
                wear.Add(Pick(Pool(race, "Hair", "short", "military", "haircut"), rng));
            }
            else
            {
                wear.Add(Pick(Pool(race, "Chest", "hoodie", "jacket", "tshirt", "sweater", "top", "tanktop", "dress"), rng));
                var top = wear[0];
                if (top == null || !top.name.ToLowerInvariant().Contains("dress"))
                    wear.Add(Pick(Pool(race, "Legs", "pants", "shorts", "skirt", "bottom"), rng));
                wear.Add(Pick(Pool(race, "Feet"), rng));
                wear.Add(Pick(Pool(race, "Hair"), rng));
                if (isMale && rng.NextDouble() < 0.35) wear.Add(Pick(Pool(race, "Beard"), rng));
            }
            wear.Add(Pick(Pool(race, "Eyebrows"), rng));
            foreach (var w in wear) if (w != null) dca.preloadWardrobeRecipes.recipes.Add(new DynamicCharacterAvatar.WardrobeRecipeListItem(w));

            Color[] skins = { new Color(0.96f, 0.82f, 0.72f), new Color(0.92f, 0.75f, 0.62f), new Color(0.78f, 0.6f, 0.47f), new Color(0.55f, 0.38f, 0.27f), new Color(0.36f, 0.24f, 0.17f) };
            Color[] hairs = { new Color(0.1f, 0.07f, 0.05f), new Color(0.25f, 0.16f, 0.09f), new Color(0.45f, 0.3f, 0.16f), new Color(0.7f, 0.55f, 0.3f), new Color(0.55f, 0.2f, 0.1f), new Color(0.7f, 0.7f, 0.68f) };
            int si = rng.Next(10); var skin = skins[si < 6 ? rng.Next(2) : si < 8 ? 2 : si < 9 ? 3 : 4];
            dca.SetRawColor("Skin", new OverlayColorData(3) { color = skin }, false);
            dca.SetRawColor("Hair", new OverlayColorData(3) { color = isPlayer ? hairs[1] : hairs[rng.Next(hairs.Length)] }, false);
            dca.SetRawColor("Eyes", new OverlayColorData(3) { color = Color.Lerp(new Color(0.3f, 0.2f, 0.1f), new Color(0.3f, 0.5f, 0.7f), (float)rng.NextDouble()) }, false);
            Color[] cloth = { new Color(0.15f, 0.15f, 0.17f), new Color(0.5f, 0.5f, 0.52f), new Color(0.12f, 0.2f, 0.4f), new Color(0.5f, 0.1f, 0.12f), new Color(0.2f, 0.35f, 0.22f), new Color(0.85f, 0.85f, 0.82f), new Color(0.6f, 0.45f, 0.3f) };
            foreach (var n in new[] { "ClothingTop01", "ClothingBottom01", "Shoes01", "ClothingTop02", "ClothingBottom02" })
                dca.SetRawColor(n, new OverlayColorData(3) { color = cloth[rng.Next(cloth.Length)] }, false);

            dca.CharacterCreated = new UMADataEvent();
            dca.CharacterCreated.AddListener(d => onReady?.Invoke(d));
            go.SetActive(true);
            return dca;
        }
    }
}
