// KINDS from the old game (v95), verbatim. Lines 19005-19005, 42600-42600, 79679-79820, 193650-193688 of game.pretty.js
 const KINDS = ["oak", "sycamore", "birch", "bare"];
 const KINDS = ["oak", "sycamore", "birch", "conifer", "oak", "sycamore", "oak", "bare", "conifer", "sycamore"];
  const KINDS = {
    cotton: {
     per: 8,
     tile: .0072,
     bump: 17e-5,
     make(seed) {
      const k = knit(8, 10, .17, .28),
       fz = pnoise(64, seed),
       sl = pnoise(16, seed + 3);
      return {
       h: (u, v) => .15 + .7 * k(u, v) + .15 * fz(u, v),
       c: (u, v) => .95 + .07 * sl(u, v) + .04 * fz(u * 3, v * 3)
      }
     }
    },
    knit: {
     per: 12,
     tile: .009,
     bump: 15e-5,
     make(seed) {
      const k = knit(12, 14, .2, .25),
       fz = pnoise(96, seed + 1),
       sl = pnoise(12, seed + 4);
      return {
       h: (u, v) => .2 + .5 * k(u, v) + .3 * fz(u, v),
       c: (u, v) => .94 + .08 * sl(u, v) + .05 * fz(u * 2, v * 2)
      }
     }
    },
    wool: {
     per: 4,
     tile: .012,
     bump: 55e-5,
     make(seed) {
      const k = knit(4, 5, .19, .3),
       fz = pnoise(48, seed + 2),
       tw = pnoise(24, seed + 9);
      return {
       h: (u, v) => .1 + .78 * k(u, v) * (.85 + .15 * tw(u, v)) + .12 * fz(u, v),
       c: (u, v) => .93 + .1 * tw(u * 2, v) + .04 * fz(u * 2, v * 2)
      }
     }
    },
    denim: {
     per: 9,
     tile: .008,
     bump: 15e-5,
     make(seed) {
      const sl = pnoise(16, 3, seed + 5),
       sl2 = pnoise(8, seed + 6),
       col = u => (Math.floor(u * 16) + .02) / 16;
      return {
       h: (u, v) => {
        const ph = frac(8 * u - 4 * v),
         ridge = Math.pow(.5 + .5 * Math.cos(TAU2 * ph), 1.6),
         slub = .6 + .4 * sl(col(u), v);
        return .12 + .76 * ridge * slub + .12 * sl2(u, v)
       },
       c: (u, v) => {
        const ph = frac(8 * u - 4 * v),
         ridge = Math.pow(.5 + .5 * Math.cos(TAU2 * ph), 1.6),
         slub = sl(col(u), v);
        return .9 + .16 * ridge + .12 * (slub - .5) + .05 * (sl2(u + .3, v) - .5)
       }
      }
     }
    },
    poplin: {
     per: 20,
     tile: .004,
     bump: 3e-5,
     make(seed) {
      const fz = pnoise(64, seed + 7);
      return {
       h: (u, v) => .5 + .3 * Math.sin(TAU2 * v * 20) + .12 * Math.sin(TAU2 * u * 20) * Math.sin(TAU2 * v * 20) + .08 * (fz(u, v) - .5),
       c: (u, v) => .985 + .03 * fz(u * 2, v * 2)
      }
     }
    },
    nylon: {
     per: 16,
     tile: .005,
     bump: 4e-5,
     make(seed) {
      const fz = pnoise(32, seed + 8);
      return {
       h: (u, v) => .5 + .22 * Math.sin(TAU2 * u * 16) * Math.sin(TAU2 * v * 16) + .18 * (Math.max(0, Math.cos(TAU2 * u) - .95) + Math.max(0, Math.cos(TAU2 * v) - .95)) * 10 + .1 * (fz(u, v) - .5),
       c: (u, v) => .99 + .02 * fz(u, v)
      }
     }
    },
    suit: {
     per: 20,
     tile: .024,
     bump: 12e-5,
     make(seed) {
      const fz = pnoise(48, seed + 10),
       tw = (u, v) => {
        const col = Math.floor(u * 4),
         s = col & 1 ? -1 : 1,
         ph = frac((u * 4 - col) * 5 + s * v * 20);
        return .5 + .5 * Math.cos(TAU2 * ph)
       };
      return {
       h: (u, v) => .5 + .4 * (tw(u, v) - .5) + .1 * (fz(u, v) - .5),
       c: (u, v) => .92 + .14 * tw(u, v) + .03 * fz(u * 2, v)
      }
     }
    },
    leather: {
     per: 14,
     tile: .02,
     bump: 12e-5,
     make(seed) {
      const vo = pvoronoi(14, seed + 11),
       cr = pnoise(6, seed + 12),
       fz = pnoise(64, seed + 13);
      return {
       h: (u, v) => {
        const [f1, f2] = vo(u, v);
        return .25 + .5 * sstep(0, .14, f2 - f1) * (1 - .35 * f1) + .25 * cr(u, v)
       },
       c: (u, v) => {
        const [f1, f2, id] = vo(u, v);
        return .86 + .14 * sstep(0, .1, f2 - f1) + .04 * (frac(id * .618) - .5) + .03 * (fz(u, v) - .5)
       }
      }
     }
    },
    tights: {
     per: 12,
     tile: .003,
     bump: 2e-5,
     make(seed) {
      const fz = pnoise(32, seed);
      return {
       h: (u, v) => .5 + .3 * Math.sin(TAU2 * u * 12) * Math.sin(TAU2 * v * 12),
       c: (u, v) => .99 + .02 * fz(u, v)
      }
     }
    }
   },
 const KINDS = {
   rain: [
    ["rain", .32, 1, 0]
   ],
   drizzle: [
    ["rain", .26, 1, 5200]
   ],
   birds: [
    ["birds", .2, 1, 0]
   ],
   wind: [
    ["wind", .12, .9, 1800]
   ],
   far: [
    ["traffic", .14, .92, 900]
   ],
   hum: [
    ["hum", .1, 1, 0],
    ["buzz", .035, 1, 6e3]
   ],
   room: [
    ["fan", .12, .85, 1600],
    ["tick", .16, 1, 0]
   ],
   engine: [
    ["engine", .55, 1, 0]
   ],
   street: [],
   extract: [
    ["fan", .16, 1.35, 2400]
   ],
   tv: [
    ["telly", .2, 1, 1100],
    ["tick", .05, .97, 0]
   ],
   pad: [
    ["score", .9, 1, 0, "music"]
   ]
  },
