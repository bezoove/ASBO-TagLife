// TRAFFIC from the old game (v95), verbatim. Lines 154148-154209, 178643-178643 of game.pretty.js
const TRAFFIC = {
  laneE: 25.25,
  laneW: 27.75,
  xIn: -32,
  xOut: -33,
  turn: {
   cx: 84.6,
   cz: 27.05,
   R: 2.85
  },
  slots: [{
   id: "n1",
   x: -15.5,
   z: 24.8,
   dir: 1,
   dest: "pub"
  }, {
   id: "n2",
   x: 15.6,
   z: 24.85,
   dir: 1,
   dest: "house"
  }, {
   id: "n3",
   x: 47,
   z: 24.85,
   dir: 1,
   dest: "house"
  }, {
   id: "s1",
   x: -1.6,
   z: 28.2,
   dir: -1,
   dest: "shop"
  }, {
   id: "s2",
   x: 13.2,
   z: 28.2,
   dir: -1,
   dest: "house"
  }, {
   id: "s3",
   x: 62,
   z: 28.2,
   dir: -1,
   dest: "house"
  }],
  pool: [
   ["astra", "#7a7f86", 31],
   ["golf", "#2a3f6a", 32],
   ["micra", "#b8b2a2", 33],
   ["focus", "#8a1f22", 34],
   ["saloon", "#2f3a2e", 35],
   ["city", "#d0d4d8", 36]
  ],
  fixed: [{
   x: 6.6,
   z: 28.3,
   hl: 2.6,
   hw: 1
  }]
 },
    TRAFFIC.slots = TRAFFIC.slots.filter(S => !CarBump.roadParked().some(r => Math.hypot(r.x - S.x, r.z - S.z) < 3.2)), CarBump.stats.slotsDropped = n0 - TRAFFIC.slots.length
