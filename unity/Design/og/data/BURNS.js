// BURNS from the old game (v95), verbatim. Lines 86052-86061 of game.pretty.js
  const BURNS = {
    short: [{
     id: "beard_full",
     keep: (x, y, z, top) => Math.abs(x) > .052 && y > top - .14
    }],
    long: [{
     id: "beard_full",
     keep: (x, y, z, top) => Math.abs(x) > .046 && y > top - .195
    }]
   },
