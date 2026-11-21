// TIME_SCHED from the old game (v95), verbatim. Lines 145547-145618 of game.pretty.js
const TIME_SCHED = {
  brenda: {
   arrive: TIME.STAFF_ON,
   leave: TIME.STAFF_OFF,
   fixed: !0
  },
  callum: {
   arrive: d => d === 5 || d === 6 ? 720 : 1020,
   leave: TIME.STAFF_OFF,
   fixed: !0
  },
  terry: {
   arrive: 720,
   leave: 1350
  },
  ron: {
   arrive: 750,
   leave: 1350
  },
  reg: {
   arrive: 780,
   leave: 1170
  },
  moira: {
   arrive: 840,
   leave: 1260
  },
  priya: {
   arrive: 1110,
   leave: 1365
  },
  shaz: {
   arrive: 1170,
   leave: 1460
  },
  dave: {
   arrive: 1170,
   leave: 1425
  },
  deano: {
   arrive: 1200,
   leave: 1485
  },
  kev: {
   arrive: 1230,
   leave: 1480
  },
  duke: {
   arrive: 1260,
   leave: 1470
  },
  mickey: {
   arrive: 1020,
   leave: 1470,
   fixed: !0
  },
  tank: {
   arrive: 1020,
   leave: 1470,
   fixed: !0
  },
  raj: {
   arrive: TIME.SHOP_OPEN,
   leave: TIME.SHOP_CLOSE,
   fixed: !0
  },
  bobby: {
   arrive: TIME.BOBBY_ON,
   leave: 5940,
   fixed: !0
  }
 },
