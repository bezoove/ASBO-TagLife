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

        static readonly string[] Banned = { "underwear", "underpants", "bra", "swim", "challenger", "robe", "wrapped", "tights", "anime", "pigtail", "tornado", "pointy", "elf", "orc", "sylvan", "sprite", "glove", "shouder", "shoulder", "lace", "tanktop_zebra", "straps", "sportswear_top", "hive", "hoodie_turquoise", "bluewhite", "tshirt_turquoise" }; // printed brand names on hive/blueWhite/turquoise tee; the turquoise hoodie is open with nothing under it // hive = printed brand name; the turquoise hoodie is open with nothing under it

        static List<UMAWardrobeRecipe> Pool(RaceData race, string slot, params string[] mustContainAny)
        {
            return all.Where(r => string.Equals(r.wardrobeSlot, slot, System.StringComparison.OrdinalIgnoreCase)
                && (r.compatibleRaces == null || r.compatibleRaces.Count == 0 || r.compatibleRaces.Contains(race.raceName))
                && !Banned.Any(b => r.name.ToLowerInvariant().Contains(b))
                && (mustContainAny.Length == 0 || mustContainAny.Any(m => r.name.ToLowerInvariant().Contains(m)))).ToList();
        }

        /// <summary>UMA's multi-pass card-hair shader ignores the shared "Hair" colour, so set its root/tip colours directly.</summary>
        static void TintHair(UMAData d, Color c)
        {
            if (d == null) return;
            Color root = c * 0.75f, tip = Color.Lerp(c, Color.white, 0.12f); root.a = tip.a = c.a = 1f;
            foreach (var r in d.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || m.shader == null || !m.HasProperty("_RootColor")) continue;
                    m.SetColor("_BaseColor", c); m.SetColor("_RootColor", root);
                    if (m.HasProperty("_Tip_Color")) m.SetColor("_Tip_Color", tip);
                }
        }

        /// <summary>Multiply a garment's generated material by a colour (matched on the recipe/material name).</summary>
        static void TintClothes(UMAData d, Dictionary<string, Color> tints)
        {
            if (d == null || tints.Count == 0) return;
            foreach (var r in d.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    string n = m.name.ToLowerInvariant();
                    foreach (var kv in tints)
                        if (n.Contains(kv.Key)) { if (m.HasProperty("_Color")) m.SetColor("_Color", kv.Value); if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", kv.Value); }
                }
        }

        /// <summary>Colour individual garments by their overlays (before UMA builds its atlas), so a tint never spills onto skin or shoes sharing that atlas.</summary>
        static bool TintOverlays(UMAData d, Dictionary<string, Color> tints)
        {
            if (d == null || d.umaRecipe == null || tints.Count == 0) return false;
            bool changed = false;
            foreach (var slot in d.umaRecipe.slotDataList)
            {
                if (slot == null) continue;
                var ovs = slot.GetOverlayList(); if (ovs == null) continue;
                foreach (var ov in ovs)
                {
                    if (ov == null) continue;
                    string n = (ov.overlayName ?? "").ToLowerInvariant();
                    foreach (var kv in tints)
                        if (n.Contains(kv.Key)) { ov.colorData = new OverlayColorData(3) { color = kv.Value }; changed = true; break; }
                }
            }
            if (changed) d.Dirty(false, true, false);
            return changed;
        }

        static T Pick<T>(List<T> l, System.Random rng) => l.Count == 0 ? default : l[rng.Next(l.Count)];

        /// <summary>Create the UMA avatar under the person. onReady fires once the mesh, skeleton and Animator exist.</summary>
        public static DynamicCharacterAvatar Create(Person p, bool isMale, int seed, bool isPlayer, System.Action<UMAData> onReady, string role = null)
        {
            Init();
            var race = isMale ? male : female;
            if (race == null) return null;
            var rng = new System.Random(seed);
            var go = new GameObject("Body"); go.SetActive(false);
            go.transform.SetParent(p.transform, false);
            go.AddComponent<AnimEventSink>(); // the walk clips carry footstep events
            var dca = go.AddComponent<DynamicCharacterAvatar>();
            dca.RacePreset = race.raceName;
            bool stock = PlayerPrefs.GetInt("asbo_stock", 0) == 1; // before/after comparison: UMA defaults, no foot IK
            string k = (isMale ? "M" : "F") + (stock ? "_Stock" : p != null && p.drunk && isMale ? "_Drunk" : PlayerPrefs.GetInt("asbo_sa", 1) == 1 ? "_SA" : "");
            if (!ctrls.TryGetValue(k, out var ctrl)) { ctrl = Resources.Load<RuntimeAnimatorController>("Anim/Loco" + k); if (ctrl == null && k.EndsWith("_Drunk")) ctrl = Resources.Load<RuntimeAnimatorController>("Anim/LocoM_SA"); ctrls[k] = ctrl; }
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
            var tints = new Dictionary<string, Color>();
            float residentAge = 0.35f;
            if (role == "Paramedic")
            {
                // no uniform in the free wardrobe: grey sportswear tinted ambulance green, dark trousers
                wear.Add(Pick(Pool(race, "Chest", "hoodie_grey", "sweater_granit"), rng));
                wear.Add(Pick(Pool(race, "Legs", "sportpants_grey", "pants_granit", "sweatpants_black"), rng));
                wear.Add(Pick(Pool(race, "Feet", "shoe"), rng));
                wear.Add(Pick(Pool(race, "Hair", "short", "military", "haircut", "ponytail", "bun"), rng));
                tints["hoodie_grey"] = tints["sweater_granit"] = new Color(0.32f, 0.62f, 0.4f);
                tints["sportpants_grey"] = tints["pants_granit"] = new Color(0.3f, 0.42f, 0.34f);
            }
            else if (isPlayer)
            {
                // the player: hoodie, trackies and trainers
                wear.Add(Pick(Pool(race, "Chest", "hoodie_blue"), rng));
                wear.Add(Pick(Pool(race, "Legs", "sweatpants"), rng));
                wear.Add(Pick(Pool(race, "Feet", "shoe_low", "shoe"), rng));
                wear.Add(Pick(Pool(race, "Hair", "short", "military", "haircut"), rng));
            }
            else
            {
                // estate resident, after the original game's generator: hoodies, trackies, cheap jackets, tired colours
                float R() => (float)rng.NextDouble();
                if (isMale)
                {
                    float t = R();
                    wear.Add(Pick(Pool(race, "Chest", t < 0.62f ? "hoodie" : "tshirt"), rng));
                    float b = R();
                    wear.Add(Pick(Pool(race, "Legs", b < 0.75f ? "sweatpants" : b < 0.9f ? "sportpants" : "shorts_black"), rng));
                    wear.Add(Pick(Pool(race, "Feet", R() < 0.8f ? "shoe_low" : "shoes_tall"), rng));
                    float h = R();
                    if (h < 0.45f) wear.Add(Pick(Pool(race, "Hair", "military"), rng));      // buzz cut
                    else if (h < 0.85f) wear.Add(Pick(Pool(race, "Hair", "haircut"), rng));  // short back and sides
                    // else: shaved / bald
                    float bd = R();
                    if (bd < 0.25f) wear.Add(Pick(Pool(race, "Beard", "trimmed"), rng));
                    else if (bd < 0.35f) wear.Add(Pick(Pool(race, "Beard", "goatee"), rng));
                    else if (bd < 0.4f) wear.Add(Pick(Pool(race, "Beard", "neckbeard"), rng));
                }
                else
                {
                    float t = R();
                    wear.Add(Pick(Pool(race, "Chest", "sweater"), rng));
                    float b = R();
                    var legs = Pick(Pool(race, "Legs", "pants_granit"), rng);
                    if (legs == null) Debug.LogWarning("[ASBO] no female trackies recipe found");
                    wear.Add(legs);
                    wear.Add(Pick(Pool(race, "Feet", R() < 0.7f ? "shoe_low" : "shoes_tall"), rng));
                    float h = R();   // scraped back, ponytails and buns mostly
                    wear.Add(Pick(Pool(race, "Hair", h < 0.3f ? "pulledback" : h < 0.55f ? "ponytail" : h < 0.75f ? "bun" : h < 0.85f ? "bob" : h < 0.93f ? "straight" : "messyup"), rng));
                }
                // tired, cheap colours on the garments that take a tint; everything gets a bit grubby
                string[] tops = { "#2a4a8a", "#8a2028", "#2f5a32", "#b4b4b0", "#3a3a3c", "#6a4a7a", "#b88a3e", "#4a6a8a", "#7a6444", "#2d7a6a", "#a8442e", "#c8b46a", "#e4e2dc", "#5a5c60" };
                string[] bots = { "#2c3e5c", "#1d1d22", "#3a3530", "#4a4030", "#22324a", "#555555", "#2a2a2a" };
                float grime = 0.12f + R() * 0.35f;
                Color Grub(Color c) => Color.Lerp(c, c * new Color(0.82f, 0.78f, 0.7f), grime) * (1f - grime * 0.15f);
                Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
                foreach (var w in wear)
                {
                    if (w == null) continue; string n = w.name.ToLowerInvariant();
                    string key = n.Contains("hoodie") ? "hoodie" : n.Contains("tshirt") ? "tshirt" : n.Contains("sweater") ? "sweater" : n.Contains("jacket") ? "jacket"
                        : n.Contains("sweatpants") ? "sweatpants" : n.Contains("sportpants") ? "sportpants" : n.Contains("pants_granit") ? "pants_granit" : n.Contains("shorts") ? "shorts" : n.Contains("skirt") ? "skirt" : n.Contains("shoe") ? "shoe" : null;
                    if (key == null) continue;
                    float baseLum = n.Contains("white") ? 0.85f : n.Contains("grey") ? 0.42f : -1f; // how bright the garment's own texture is
                    bool isTop = key == "hoodie" || key == "tshirt" || key == "sweater" || key == "jacket";
                    Color c = baseLum > 0f && key != "shoe" ? Hex(isTop ? tops[rng.Next(tops.Length)] : bots[rng.Next(bots.Length)]) * Mathf.Min(2.3f, 0.8f / baseLum) : new Color(0.9f, 0.88f, 0.85f);
                    if (key == "shoe") c = Color.Lerp(Color.white, new Color(0.72f, 0.68f, 0.6f), grime * 1.5f); // white trainers, not so white
                    c.a = 1f; tints[key] = Grub(c);
                }
                // bodies: real shapes, not catwalk ones
                float age = R(), build = R(), belly = Mathf.Pow(R(), 1.5f) * (0.3f + age * 0.7f);
                float hgt = isMale ? 1.68f + R() * 0.22f : 1.55f + R() * 0.17f;
                var D = dca.predefinedDNA ?? (dca.predefinedDNA = new UMAPredefinedDNA());
                D.AddDNA("height", Mathf.Clamp01(0.5f + (hgt - (isMale ? 1.79f : 1.63f)) / 0.25f * 0.3f));
                D.AddDNA("upperWeight", 0.38f + build * 0.38f); D.AddDNA("lowerWeight", 0.4f + build * 0.32f);
                D.AddDNA("upperMuscle", 0.3f + R() * 0.3f); D.AddDNA("lowerMuscle", 0.38f + R() * 0.22f);
                D.AddDNA("belly", Mathf.Clamp01(0.5f + belly * 0.5f)); D.AddDNA("waist", Mathf.Clamp01(0.5f + belly * 0.32f));
                D.AddDNA("bodyFitness", 0.2f + R() * 0.3f);
                D.AddDNA("neckThickness", 0.45f + build * 0.2f);
                D.AddDNA("noseSize", 0.45f + R() * 0.25f + age * 0.08f); D.AddDNA("noseWidth", 0.45f + R() * 0.25f);
                if (isMale && R() < 0.3f) D.AddDNA("noseBroken", 0.55f + R() * 0.35f);
                D.AddDNA("jawsSize", 0.42f + R() * 0.22f); D.AddDNA("mandibleSize", 0.42f + R() * 0.22f); D.AddDNA("chinSize", 0.4f + R() * 0.25f);
                D.AddDNA("eyeSize", 0.4f + R() * 0.12f); D.AddDNA("cheekSize", 0.42f + R() * 0.18f);
                D.AddDNA("lipsSize", isMale ? 0.44f + R() * 0.1f : 0.4f + R() * 0.14f);
                D.AddDNA("headSize", 0.47f + R() * 0.07f);
                if (!isMale) { D.AddDNA("breastSize", 0.4f + R() * 0.22f + belly * 0.1f); D.AddDNA("gluteusSize", 0.45f + build * 0.22f); }
                residentAge = age;
            }
            wear.Add(Pick(Pool(race, "Eyebrows"), rng));
            foreach (var w in wear) if (w != null) dca.preloadWardrobeRecipes.recipes.Add(new DynamicCharacterAvatar.WardrobeRecipeListItem(w));

            // the original game's skin and hair palettes (mostly pale and pink: it's an English estate)
            Color HexC(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
            string[] skinHex = { "#f3d2bd", "#e9bfa0", "#d9a57f", "#c68a62", "#a86d47", "#8a5432", "#6b3d22", "#4a2a17" };
            string[] hairHex = { "#1b1512", "#2e2119", "#4a3121", "#6b4a2e", "#8f6a3e", "#b89058", "#d6b37a", "#a33a1c" };
            string[] greyHex = { "#c8c4bd", "#e8e6e1", "#8a8a8a" };
            int si = rng.Next(20); var skin = HexC(skinHex[si < 9 ? 0 : si < 15 ? 1 : si < 17 ? 2 : si < 18 ? 3 : 4 + rng.Next(4)]);
            skin = Color.Lerp(skin, skin * new Color(1.02f, 0.93f, 0.9f), (float)rng.NextDouble() * 0.5f); // a bit of colour in the cheeks
            dca.SetRawColor("Skin", new OverlayColorData(3) { color = skin }, false);
            // the recipes' own colour slots: neutral, so nothing comes out purple
            foreach (var n in new[] { "Shoes01", "Shoes02" }) dca.SetRawColor(n, new OverlayColorData(3) { color = new Color(0.86f, 0.85f, 0.82f) }, false);
            foreach (var n in new[] { "ClothingTop01", "ClothingTop02", "ClothingTop03", "ClothingBottom01", "ClothingBottom02", "ClothingBottom03" })
                dca.SetRawColor(n, new OverlayColorData(3) { color = new[] { new Color(0.2f, 0.2f, 0.22f), new Color(0.45f, 0.45f, 0.46f), new Color(0.16f, 0.22f, 0.36f), new Color(0.7f, 0.7f, 0.68f) }[rng.Next(4)] }, false);
            var hairCol = isPlayer ? HexC("#2e2119") : residentAge > 0.75f ? HexC(greyHex[rng.Next(3)]) : HexC(hairHex[rng.Next(hairHex.Length)]);
            dca.SetRawColor("Hair", new OverlayColorData(3) { color = hairCol }, false);
            dca.SetRawColor("Eyes", new OverlayColorData(3) { color = Color.Lerp(new Color(0.3f, 0.2f, 0.1f), new Color(0.3f, 0.5f, 0.7f), (float)rng.NextDouble()) }, false);

            dca.CharacterCreated = new UMADataEvent();
            dca.CharacterCreated.AddListener(d =>
            {
                var a = d.animator != null ? d.animator : d.GetComponentInChildren<Animator>();
                if (a != null && a.GetComponent<ArmTrim>() == null) a.gameObject.AddComponent<ArmTrim>().Bind(a);
                if (a != null && a.GetComponent<FootGround>() == null) a.gameObject.AddComponent<FootGround>().Bind(a);
                TintHair(d, hairCol);
                TintOverlays(d, tints); // rebuilds the textures once with the garment colours
                onReady?.Invoke(d);
            });
            if (dca.CharacterUpdated == null) dca.CharacterUpdated = new UMADataEvent();
            dca.CharacterUpdated.AddListener(d => TintHair(d, hairCol));
            go.SetActive(true);
            return dca;
        }
    }
}
