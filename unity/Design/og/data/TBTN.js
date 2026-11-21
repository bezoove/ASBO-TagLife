// TBTN from the old game (v95), verbatim. Lines 128334-128447, 131336-131343, 143328-143340, 146793-146802 of game.pretty.js
const TBTN = {
  interact: {
   icon: "check",
   label: "Use",
   base: 84,
   act: () => Game.interact()
  },
  jump: {
   icon: "up",
   label: "Jump",
   base: 60,
   act: () => Game.jump()
  },
  sneak: {
   icon: "sneak",
   label: "Sneak",
   base: 58,
   toggle: !0,
   act: () => Stealth.toggleSneak()
  },
  sip: {
   icon: "pint",
   label: "Sip",
   base: 62,
   act: () => Game.sip()
  },
  phone: {
   icon: "phone",
   label: "Phone",
   base: 60,
   act: () => {
    UI.modal || UI.journal()
   }
  },
  camera: {
   icon: "camera",
   label: "Camera",
   base: 56,
   act: () => Game.recentre()
  },
  throw: {
   icon: "throw",
   label: "Throw",
   base: 64,
   hold: !0,
   down: () => {
    Objs.btnThrow = !0
   },
   up: () => {
    Objs.btnThrow = !1
   },
   act: () => {}
  },
  drop: {
   icon: "down",
   label: "Put down",
   base: 56,
   act: () => Objs.putDown()
  },
  fight: {
   icon: "fist",
   label: "Fists up",
   base: 58,
   toggle: !0,
   act: () => Fight.toggleStance()
  },
  hit: {
   icon: "fist",
   label: "Punch",
   base: 76,
   hold: !0,
   down: () => {
    Fight.btn.hit = !0
   },
   up: () => {
    Fight.btn.hit = !1
   },
   act: () => {}
  },
  shove: {
   icon: "shove",
   label: "Shove",
   base: 56,
   act: () => Fight.touchAttack("shove")
  },
  kick: {
   icon: "kick",
   label: "Kick",
   base: 56,
   act: () => Fight.touchAttack("kick")
  },
  guard: {
   icon: "shield",
   label: "Guard",
   base: 64,
   hold: !0,
   down: () => {
    Fight.btn.guard = !0
   },
   up: () => {
    Fight.btn.guard = !1
   },
   act: () => {}
  },
  wee: {
   icon: "drop",
   label: "Wee",
   base: 60,
   hold: !0,
   down: () => Bladder.press(),
   up: () => Bladder.releaseBtn(),
   act: () => {}
  }
 },
 TBTN.bag = {
  icon: "bag",
  label: "Bag",
  base: 58,
  act: () => {
   !UI.modal && Game.mode === "explore" && UI.inventory()
  }
 };
 }, Heat.ALIAS.graffiti = "damage", Heat.ALIAS.paint = "assault", Offences.LINES.warn.graffiti = [`Brenda: "Is that PAINT? On MY pub? Do that again and you're barred."`, `Brenda: "Put that can away or you're out on your ear."`], Offences.LINES.out.graffiti = 'Brenda: "I warned you. OUT. And you can come back tomorrow with a scrubbing brush."', TBTN.spray = {
  icon: "spray",
  label: "Spray",
  base: 76,
  hold: !0,
  down: () => {
   Spray.tdown = !0
  },
  up: () => {
   Spray.tdown = !1
  },
  act: () => {}
 };
 TBTN.interact.hold = !0, TBTN.interact.down = () => {
  if (UI2.useHeld = !0, UI2.useT = 0, UI2.useSprayed = !1, Spray.can()) {
   UI.prompt || (Spray.tdown = !0, UI2.useSprayed = !0, Sfx.init());
   return
  }
  Game.interact()
 }, TBTN.interact.up = () => {
  const was = UI2.useHeld;
  UI2.useHeld = !1, Spray.can() && was && !UI2.useSprayed && UI2.useT < .22 && Game.interact(), Spray.tdown = !1
 }, delete TBTN.spray;
