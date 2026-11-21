// VBTN from the old game (v95), verbatim. Lines 132757-132798, 163263-163263 of game.pretty.js
const VBTN = {
  steerL: {
   icon: "steerL",
   label: "Left",
   base: 84,
   hold: !0,
   down: () => {
    Vehicles.btn.left = !0
   },
   up: () => {
    Vehicles.btn.left = !1
   }
  },
  steerR: {
   icon: "steerR",
   label: "Right",
   base: 84,
   hold: !0,
   down: () => {
    Vehicles.btn.right = !0
   },
   up: () => {
    Vehicles.btn.right = !1
   }
  },
  vexit: {
   icon: "vexit",
   label: "Get off",
   base: 62,
   act: () => {
    Vehicles.btn.exit = !0
   }
  },
  vhorn: {
   icon: "bell",
   label: "Bell",
   base: 54,
   act: () => {
    Vehicles.btn.horn = !0
   }
  }
 },
 }, typeof VBTN < "u" && VBTN.vbrake && (VBTN.vbrake.label = "Brake / Handbrake");
