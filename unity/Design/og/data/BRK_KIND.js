// BRK_KIND from the old game (v95), verbatim. Lines 170851-170918, 170920-170920 of game.pretty.js
const BRK_KIND = {
  lamp: {
   need: 6.5,
   keep: .72,
   dmg: .7,
   cap: 9
  },
  fence: {
   need: 5.5,
   keep: .85,
   dmg: .4,
   cap: 5
  },
  sign: {
   need: 5,
   keep: .9,
   dmg: .4,
   cap: 5
  },
  bin: {
   need: 4,
   keep: .93,
   dmg: .3,
   cap: 3
  },
  bollard: {
   need: 5,
   keep: .8,
   dmg: .6,
   cap: 7
  },
  bench: {
   need: 4.5,
   keep: .88,
   dmg: .4,
   cap: 5
  },
  hedge: {
   need: 2.5,
   keep: .9,
   dmg: 0,
   cap: 0
  },
  shelter: {
   need: 6,
   keep: .75,
   dmg: .6,
   cap: 8
  },
  wall: {
   need: 9,
   keep: .55,
   dmg: .8,
   cap: 12
  },
  tree: {
   need: 7,
   keep: .65,
   dmg: .7,
   cap: 9
  },
  junk: {
   need: 2.5,
   keep: .94,
   dmg: .2,
   cap: 2.6
  }
 },
window.BRK_KIND = BRK_KIND, (function() {
