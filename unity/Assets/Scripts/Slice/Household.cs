using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Who lives in a home, and so how it looks. Nobody's place is the same: some haven't been touched since the seventies,
    /// some were done up last year; some are spotless, some you wouldn't sit down in; some are crammed, some nearly bare.
    /// Everything about a home's interior (colours, floors, furniture, mess, what's worth nicking and where it's hidden)
    /// is drawn from one of these, made from the home's seed, so the same home is always the same.
    /// </summary>
    [System.Serializable]
    public class Household
    {
        public enum Kind { Pensioner, Family, Lad, Couple, Student, LoneMum, Dealer, Hoarder, Vacant }
        public int id; public string family; public Kind kind;
        /// <summary>0 untouched for decades, 1 dated (nineties), 2 recent refit (grey and laminate), 3 modern and stylish.</summary>
        public int era;
        /// <summary>0..1: how clean and ordered it is, how much stuff is crammed in, how much money is about.</summary>
        public float tidy, stuff, wealth;
        public bool smoker, pets, kids, gamer, drinker, hasCar, alarm, upsideDown;
        public Color wallLiving, wallBed, wallKitchen, wallHall, trim, sofa, bedding, curtain, carpet;
        public string floorLiving, floorHall, floorBed, floorKitchen, floorBath, wallBath;
        /// <summary>Front door security: 0 on the latch (a shoulder will do), 1 deadlocked, 2 chained and bolted as well.</summary>
        public int doorTier;
        /// <summary>How many of the bedrooms are really bedrooms (the others: box rooms of junk, offices, gaming dens).</summary>
        public int bedrooms;
        public bool Occupied => kind != Kind.Vacant;
        public string Label => kind == Kind.Vacant ? "Empty flat" : family + " place";

        static readonly string[] Families =
        {
            "Pickering", "Hendry", "Okafor", "Brannigan", "Walsh", "Doyle", "Khan", "Mulligan", "Rowley", "Sharpe", "Dunmore", "Tilley", "Abbas", "Kowalski",
            "Fenwick", "Gallagher", "Barnes", "Osei", "Pritchard", "Cartwright", "Moran", "Hussain", "Slater", "Nolan", "Beckett", "Lomax", "Adeyemi", "Tuck",
            "Garrity", "Peplow", "Singh", "Rudge", "Costello", "Yates", "Marsden", "Begum", "Quigley", "Fairclough", "Hartnett", "Bell"
        };

        public static Household Make(int id, System.Random r)
        {
            var h = new Household { id = id };
            h.family = Families[(id * 7 + r.Next(Families.Length)) % Families.Length];
            double k = r.NextDouble();
            h.kind = k < 0.06 ? Kind.Vacant : k < 0.22 ? Kind.Pensioner : k < 0.46 ? Kind.Family : k < 0.58 ? Kind.Lad : k < 0.68 ? Kind.Couple : k < 0.76 ? Kind.Student : k < 0.88 ? Kind.LoneMum : k < 0.94 ? Kind.Dealer : Kind.Hoarder;
            float F() => (float)r.NextDouble();
            // era: pensioners and hoarders mostly untouched; couples and dealers mostly done up
            switch (h.kind)
            {
                case Kind.Pensioner: h.era = F() < 0.7f ? 0 : 1; h.tidy = 0.6f + F() * 0.4f; h.stuff = 0.5f + F() * 0.5f; h.wealth = F() * 0.35f; break;
                case Kind.Hoarder: h.era = F() < 0.6f ? 0 : 1; h.tidy = F() * 0.25f; h.stuff = 0.85f + F() * 0.15f; h.wealth = F() * 0.3f; break;
                case Kind.Family: h.era = r.Next(4) == 0 ? 0 : 1 + r.Next(3); h.tidy = F() * 0.8f; h.stuff = 0.45f + F() * 0.5f; h.wealth = 0.15f + F() * 0.45f; break;
                case Kind.Lad: h.era = 1 + r.Next(3); h.tidy = F() * 0.45f; h.stuff = 0.2f + F() * 0.4f; h.wealth = 0.1f + F() * 0.4f; break;
                case Kind.Couple: h.era = 2 + r.Next(2); h.tidy = 0.45f + F() * 0.55f; h.stuff = 0.25f + F() * 0.45f; h.wealth = 0.35f + F() * 0.45f; break;
                case Kind.Student: h.era = r.Next(3); h.tidy = F() * 0.5f; h.stuff = 0.15f + F() * 0.4f; h.wealth = F() * 0.2f; break;
                case Kind.LoneMum: h.era = r.Next(4); h.tidy = 0.25f + F() * 0.7f; h.stuff = 0.4f + F() * 0.45f; h.wealth = F() * 0.3f; break;
                case Kind.Dealer: h.era = 2 + r.Next(2); h.tidy = 0.3f + F() * 0.5f; h.stuff = 0.1f + F() * 0.35f; h.wealth = 0.65f + F() * 0.35f; break;
                default: h.era = r.Next(2); h.tidy = 0.5f; h.stuff = 0f; h.wealth = 0f; break;
            }
            h.smoker = F() < (h.kind == Kind.Pensioner ? 0.45f : h.kind == Kind.Lad ? 0.5f : 0.3f);
            h.pets = F() < 0.28f; h.kids = h.kind == Kind.Family || h.kind == Kind.LoneMum;
            h.gamer = h.kind == Kind.Lad || h.kind == Kind.Student || (h.kind == Kind.Family && F() < 0.5f);
            h.drinker = h.kind == Kind.Lad || h.kind == Kind.Hoarder || F() < 0.3f;
            h.hasCar = F() < 0.42f + h.wealth * 0.3f; h.alarm = h.kind == Kind.Dealer || (h.wealth > 0.55f && F() < 0.5f);
            h.upsideDown = F() < 0.3f; // living room upstairs, kitchen-diner too, bedrooms elsewhere
            h.doorTier = h.kind == Kind.Dealer ? 2 : h.kind == Kind.Pensioner ? (F() < 0.6f ? 2 : 1) : F() < 0.35f ? 0 : F() < 0.7f ? 1 : 2;
            h.bedrooms = h.kind == Kind.Family ? 3 + (F() < 0.4f ? 1 : 0) : h.kind == Kind.Couple || h.kind == Kind.Dealer ? 2 : h.kind == Kind.LoneMum ? 2 : 1 + r.Next(2);
            Palette(h, r);
            return h;
        }

        static void Palette(Household h, System.Random r)
        {
            float F() => (float)r.NextDouble();
            Color Pick(params Color[] c) => c[r.Next(c.Length)];
            switch (h.era)
            {
                case 0: // seventies and eighties, never redecorated: mustard, brown, orange, mint, patterned carpet
                    h.wallLiving = Pick(new Color(0.78f, 0.62f, 0.3f), new Color(0.72f, 0.5f, 0.32f), new Color(0.62f, 0.7f, 0.55f), new Color(0.82f, 0.7f, 0.55f));
                    h.wallBed = Pick(new Color(0.85f, 0.75f, 0.8f), new Color(0.7f, 0.78f, 0.7f), new Color(0.88f, 0.82f, 0.6f));
                    h.wallKitchen = Pick(new Color(0.9f, 0.85f, 0.6f), new Color(0.7f, 0.82f, 0.78f)); h.wallHall = Pick(new Color(0.8f, 0.72f, 0.6f), new Color(0.75f, 0.62f, 0.45f));
                    h.trim = new Color(0.88f, 0.86f, 0.78f); h.sofa = Pick(new Color(0.45f, 0.3f, 0.2f), new Color(0.5f, 0.38f, 0.22f), new Color(0.35f, 0.4f, 0.3f)); h.curtain = Pick(new Color(0.55f, 0.25f, 0.2f), new Color(0.7f, 0.5f, 0.2f), new Color(0.45f, 0.5f, 0.35f));
                    h.carpet = Pick(new Color(0.55f, 0.3f, 0.18f), new Color(0.5f, 0.38f, 0.2f), new Color(0.45f, 0.25f, 0.3f));
                    h.floorLiving = "carpet_hex"; h.floorHall = "carpet_hex"; h.floorBed = "carpet_hex"; h.floorKitchen = "tiles_red"; h.floorBath = "tiles_victorian"; h.wallBath = "tiles_metro_green"; break;
                case 1: // nineties magnolia, patterned
                    h.wallLiving = Pick(new Color(0.9f, 0.85f, 0.7f), new Color(0.85f, 0.8f, 0.72f), new Color(0.8f, 0.85f, 0.8f)); h.wallBed = Pick(new Color(0.9f, 0.82f, 0.82f), new Color(0.82f, 0.86f, 0.92f), new Color(0.92f, 0.88f, 0.75f));
                    h.wallKitchen = new Color(0.92f, 0.9f, 0.82f); h.wallHall = new Color(0.9f, 0.87f, 0.8f); h.trim = new Color(0.94f, 0.93f, 0.9f);
                    h.sofa = Pick(new Color(0.35f, 0.32f, 0.3f), new Color(0.4f, 0.22f, 0.25f), new Color(0.25f, 0.33f, 0.4f)); h.curtain = Pick(new Color(0.4f, 0.3f, 0.5f), new Color(0.6f, 0.2f, 0.25f), new Color(0.3f, 0.45f, 0.4f));
                    h.carpet = Pick(new Color(0.5f, 0.45f, 0.38f), new Color(0.35f, 0.38f, 0.45f), new Color(0.5f, 0.35f, 0.35f));
                    h.floorLiving = "carpet_hex"; h.floorHall = "parquet_cherry"; h.floorBed = "carpet_hex"; h.floorKitchen = "tiles_red"; h.floorBath = "tiles_victorian"; h.wallBath = "tiles_metro_green"; break;
                case 2: // recent refit: grey, laminate, white gloss
                    h.wallLiving = Pick(new Color(0.62f, 0.64f, 0.66f), new Color(0.78f, 0.78f, 0.76f), new Color(0.5f, 0.56f, 0.6f)); h.wallBed = Pick(new Color(0.85f, 0.85f, 0.85f), new Color(0.65f, 0.68f, 0.72f), new Color(0.8f, 0.75f, 0.78f));
                    h.wallKitchen = new Color(0.9f, 0.9f, 0.9f); h.wallHall = new Color(0.82f, 0.82f, 0.8f); h.trim = Color.white;
                    h.sofa = Pick(new Color(0.3f, 0.3f, 0.32f), new Color(0.55f, 0.55f, 0.55f), new Color(0.2f, 0.25f, 0.3f)); h.curtain = Pick(new Color(0.45f, 0.45f, 0.48f), new Color(0.8f, 0.8f, 0.78f));
                    h.carpet = Pick(new Color(0.45f, 0.45f, 0.47f), new Color(0.6f, 0.58f, 0.55f));
                    h.floorLiving = "wood_slats"; h.floorHall = "wood_slats"; h.floorBed = "carpet_office"; h.floorKitchen = "large_grey_tiles"; h.floorBath = "tiles_chevron"; h.wallBath = "tiles_grey_wall"; break;
                default: // stylish: feature wall, herringbone, dark accents
                    h.wallLiving = Pick(new Color(0.18f, 0.28f, 0.3f), new Color(0.25f, 0.3f, 0.4f), new Color(0.82f, 0.78f, 0.72f), new Color(0.35f, 0.35f, 0.38f)); h.wallBed = Pick(new Color(0.75f, 0.78f, 0.75f), new Color(0.25f, 0.3f, 0.35f), new Color(0.82f, 0.72f, 0.7f));
                    h.wallKitchen = Pick(new Color(0.92f, 0.92f, 0.9f), new Color(0.2f, 0.22f, 0.25f)); h.wallHall = new Color(0.86f, 0.85f, 0.82f); h.trim = new Color(0.95f, 0.95f, 0.95f);
                    h.sofa = Pick(new Color(0.2f, 0.32f, 0.32f), new Color(0.6f, 0.45f, 0.3f), new Color(0.22f, 0.22f, 0.25f), new Color(0.75f, 0.72f, 0.68f)); h.curtain = Pick(new Color(0.85f, 0.85f, 0.82f), new Color(0.2f, 0.25f, 0.3f));
                    h.carpet = Pick(new Color(0.5f, 0.5f, 0.52f), new Color(0.35f, 0.35f, 0.38f));
                    h.floorLiving = "parquet_herring"; h.floorHall = "parquet_herring"; h.floorBed = "carpet_office"; h.floorKitchen = "tiles_herring_teal"; h.floorBath = "tiles_black_deco"; h.wallBath = "tiles_metro_black"; break;
            }
            h.bedding = Color.HSVToRGB(F(), h.era == 3 ? 0.15f + F() * 0.2f : 0.2f + F() * 0.4f, 0.55f + F() * 0.4f);
            if (h.kind == Kind.Lad && F() < 0.6f) { h.floorLiving = "wood_slats_dark"; }
        }

        /// <summary>What's lying about to be found, by where it's likely to be. Values are what a fence would give you.</summary>
        [System.Serializable] public struct Loot { public string name; public float value; public bool cash; public string kind; public Loot(string n, float v, bool c = false, string k = "item") { name = n; value = v; cash = c; kind = k; } }

        public static readonly Loot[] Junk =
        {
            new Loot("Loose change", 2f, true), new Loot("Loose change", 4f, true), new Loot("A fiver", 5f, true), new Loot("A tenner", 10f, true),
            new Loot("Carriage clock", 15f), new Loot("Brass ornament", 8f), new Loot("China dog", 4f), new Loot("Glass swan", 6f), new Loot("Framed photo", 1f),
            new Loot("Pack of cigarettes", 6f), new Loot("Lighter", 2f), new Loot("Spare house keys", 0f, false, "keys"), new Loot("Prepaid phone", 12f), new Loot("Old mobile", 15f),
            new Loot("Wristwatch", 25f), new Loot("Gold-plated chain", 20f), new Loot("Costume jewellery", 10f), new Loot("Penknife", 5f), new Loot("Torch", 3f)
        };
        public static readonly Loot[] Decent =
        {
            new Loot("Tenner and a twenty", 30f, true), new Loot("Biscuit tin of money", 60f, true), new Loot("Jewellery box", 70f), new Loot("Games console", 110f),
            new Loot("Tablet", 90f), new Loot("Smartphone", 130f), new Loot("Gold ring", 120f), new Loot("Silver cutlery set", 80f), new Loot("Camera", 65f), new Loot("Designer trainers", 75f),
            new Loot("Branded headphones", 55f), new Loot("Handheld console", 50f), new Loot("Bottle of good whisky", 35f), new Loot("Watch, nice one", 150f)
        };
        public static readonly Loot[] Prize =
        {
            new Loot("Shoebox of cash", 250f, true), new Loot("Envelope of fifties", 400f, true), new Loot("Gold chain and pendant", 300f), new Loot("Laptop", 220f), new Loot("Big watch, heavy", 450f),
            new Loot("Gold bracelet", 350f), new Loot("Proper camera, lenses and all", 380f), new Loot("Stash: a bag of whack", 280f, false, "drugs"), new Loot("Bag of fizz", 180f, false, "drugs"), new Loot("Wad of cash in a sock", 320f, true)
        };

        public static Loot Roll(Household h, System.Random r, bool hidden)
        {
            float w = h.wealth + (hidden ? 0.15f : 0f);
            float x = (float)r.NextDouble();
            if (x < 0.06f + w * 0.28f && hidden) return Prize[r.Next(Prize.Length)];
            if (x < 0.22f + w * 0.45f) return Decent[r.Next(Decent.Length)];
            return Junk[r.Next(Junk.Length)];
        }
    }

}
