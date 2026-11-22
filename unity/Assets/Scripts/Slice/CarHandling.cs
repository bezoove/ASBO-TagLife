using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// How each kind of car drives: top speed, pull, weight, how it steers and how it lets go. The old game had a
    /// speed per car too (Astra 10, Cavalier 11, Golf 13, Focus 15); these are the same idea for our nine models.
    /// Under the physics car (RCC) these set its engine, weight, downforce and driving style; under the arcade car
    /// they set its pace, grip and steering.
    /// </summary>
    public sealed class CarHandling
    {
        public string id, name;
        public float topKmh = 165f;     // flat out (the boost adds a quarter on top)
        public float torque = 160f;     // engine pull, Nm
        public float mass = 1000f;      // kg
        public float lockScale = 1f;    // how much of full lock the wheel gives (lower = wider turning circle)
        public float steerRate = 1f;    // how quickly it turns in (1 = normal)
        public float grip = 1f;         // how hard it can corner before it slides (arcade car)
        public float downforce = 0f;    // RCC aero downforce
        public float brakes = 1f;       // stopping power (arcade car)
        public float tail = 1f;         // how readily the back steps out on the handbrake (arcade car)
        public string[] style = { "Arcade", "Balanced" }; // RCC driving style (Balanced, Drift, Race, Arcade): the first one found
        public string feel;             // a line for the HUD when you get in

        static Dictionary<string, CarHandling> table;

        public static CarHandling For(string id)
        {
            if (table == null) Fill();
            if (id != null && table.TryGetValue(id, out var h)) return h;
            return table["default"];
        }

        static void Fill()
        {
            table = new Dictionary<string, CarHandling>();
            void Add(CarHandling h) => table[h.id] = h;
            Add(new CarHandling { id = "default", name = "hatchback", feel = "An ordinary hatchback." });
            // little and light: nippy, darts about, runs out of puff
            Add(new CarHandling { id = "supermini", name = "supermini", topKmh = 148f, torque = 112f, mass = 820f, lockScale = 1.0f, steerRate = 1.25f, grip = 0.95f, tail = 1.1f, feel = "Tiny and light. Nips through gaps, runs out of puff at 90." });
            // the 70s hatch: old, loose, no help at all
            Add(new CarHandling { id = "hatch70", name = "old hatchback", topKmh = 142f, torque = 122f, mass = 880f, lockScale = 0.95f, steerRate = 0.9f, grip = 0.82f, brakes = 0.75f, tail = 1.35f, style = new[] { "Drift", "Arcade" }, feel = "Seventies tin. Loose, slow to stop, the back wants to come round." });
            // the 90s hatch: the everyday estate motor, does everything alright
            Add(new CarHandling { id = "hatch90", name = "hatchback", topKmh = 168f, torque = 150f, mass = 960f, lockScale = 1f, steerRate = 1.1f, grip = 1f, feel = "Everyday hatchback. Does everything alright." });
            // the 90s saloon: longer, quicker, steadier
            Add(new CarHandling { id = "saloon90", name = "saloon", topKmh = 192f, torque = 205f, mass = 1210f, lockScale = 0.92f, steerRate = 0.95f, grip = 1.05f, tail = 1.1f, style = new[] { "Balanced", "Arcade" }, feel = "Family saloon. Quick on the straight, steady through bends." });
            // the 90s estate: heavy at the back, ploughs on
            Add(new CarHandling { id = "estate90", name = "estate", topKmh = 176f, torque = 185f, mass = 1380f, lockScale = 0.88f, steerRate = 0.85f, grip = 0.95f, brakes = 0.9f, tail = 0.85f, style = new[] { "Balanced", "Arcade" }, feel = "Big estate. Heavy, wide turning circle, ploughs on in the corners." });
            // the boxy 80s saloon: a tank
            Add(new CarHandling { id = "boxy80", name = "old saloon", topKmh = 164f, torque = 215f, mass = 1440f, lockScale = 0.86f, steerRate = 0.75f, grip = 0.9f, brakes = 0.7f, tail = 1.2f, style = new[] { "Balanced", "Arcade" }, feel = "Eighties tank. Slow to turn, slower to stop, shrugs off a hit." });
            // the 80s coupe: rear-driven, tail-happy, fast
            Add(new CarHandling { id = "coupe80", name = "coupe", topKmh = 204f, torque = 235f, mass = 1150f, lockScale = 1f, steerRate = 1.1f, grip = 0.9f, tail = 1.6f, style = new[] { "Drift", "Arcade" }, feel = "Eighties coupe. Fast, and the back end steps out if you so much as look at it." });
            // the rally car: grips, turns, goes
            Add(new CarHandling { id = "rally", name = "rally car", topKmh = 228f, torque = 300f, mass = 1240f, lockScale = 1.05f, steerRate = 1.3f, grip = 1.3f, downforce = 2.5f, brakes = 1.25f, tail = 1.2f, style = new[] { "Race", "Arcade" }, feel = "Rally car. Grips like glue, turns on a sixpence, properly fast." });
            // the jeep: tall, heavy, slow, unstoppable
            Add(new CarHandling { id = "jeep", name = "jeep", topKmh = 136f, torque = 275f, mass = 1850f, lockScale = 0.82f, steerRate = 0.7f, grip = 0.8f, brakes = 0.8f, tail = 0.7f, style = new[] { "Balanced", "Arcade" }, feel = "Jeep. Tall and heavy, leans in corners, slow but goes through anything." });
            // the procedural cars and vans (traffic bodies)
            Add(new CarHandling { id = "van", name = "van", topKmh = 140f, torque = 240f, mass = 1900f, lockScale = 0.84f, steerRate = 0.75f, grip = 0.85f, brakes = 0.8f, tail = 0.8f, style = new[] { "Balanced", "Arcade" }, feel = "Van. Heavy and slow, leans like a boat." });
        }
    }
}
