// LOCKS from the old game (v95), verbatim. Lines 109789-109875, 110153-110157, 110185-110189, 110207-110211, 110232-110236, 110251-110259, 110286-110290, 150761-150790 of game.pretty.js
 LOCKS = {
  carboot: {
   name: "Silver saloon",
   what: "boot",
   tier: 1,
   outdoor: !0,
   icon: "keys",
   loot() {
    const j = Locks.ronJob();
    if (j && !Econ.has("ronradio") && j.stage !== "got") return Econ.give("ronradio"), j.stage = "got", ["Ron's car radio, under a picnic blanket"];
    const c = roundMoney(rand(2, 9));
    Econ.add(c, "Car boot");
    const it = pick(["cigs", "flowers", "torch", "scratchcard", "cigs"]);
    return Econ.give(it), [fmtMoney(c, !0) + " in a cup holder", ITEMS[it].name]
   }
  },
  citycar: {
   name: "Yellow runabout",
   what: "driver's door",
   tier: 2,
   outdoor: !0,
   icon: "keys",
   loot() {
    const c = roundMoney(rand(4, 16));
    Econ.add(c, "Glovebox");
    const out = [fmtMoney(c, !0) + " in the glovebox"],
     it = pick(["phone", "cigs", "gum", "scratchcard", "phone"]);
    return ITEMS[it] && (Econ.give(it), out.push(ITEMS[it].name)), out
   }
  },
  yardpad: {
   name: "Yard door padlock",
   what: "padlock",
   tier: 2,
   outdoor: !0,
   icon: "keys",
   owner: "mickey",
   loot() {
    return Stash.finish("lock"), []
   }
  },
  vanback: {
   name: "Doyle Removals van",
   what: "back doors",
   plural: !0,
   tier: 3,
   outdoor: !0,
   icon: "keys",
   owner: "mickey",
   loot() {
    const c = roundMoney(Math.round(rand(30, 85)));
    Econ.add(c, "Mickey's van");
    const n = chance(.5) ? 2 : 1;
    Econ.give("cartons", n);
    const out = [fmtMoney(c, !0) + " in a bum bag", `${n} carton${n>1?"s":""} of "tobacco"`];
    return chance(.3) && (Econ.give("laptop"), out.push("a laptop")), Stealth.addHeat(.05), out
   }
  },
  drawer: {
   name: "Brenda's drawer",
   what: "drawer",
   tier: 3,
   outdoor: !1,
   icon: "keys",
   owner: "brenda",
   severe: !0,
   loot() {
    const c = roundMoney(Math.round(rand(40, 110)));
    Econ.add(c, "Brenda's drawer");
    const out = [fmtMoney(c, !0) + " — the float, in a bank bag"];
    return chance(.5) && (Econ.give("scratchcard", 2), out.push("two scratchcards")), Game.save.trust = Math.max(0, Game.save.trust - 2), Stealth.addHeat(.08), out
   }
  },
  lockbox: {
   name: "Raj's cash box",
   what: "cash box",
   tier: 4,
   outdoor: !1,
   icon: "keys",
   owner: "raj",
   after: 1260,
   loot() {
    const c = roundMoney(Math.round(rand(70, 170)));
    return Econ.add(c, "Raj's takings"), Stealth.addHeat(.08), [fmtMoney(c, !0) + " — the day's takings"]
   }
  }
 },
 if (this.yardLockInteract && (this.yardLockInteract.lockId = "yardpad", LOCKS.yardpad.it = this.yardLockInteract, LOCKS.yardpad.look = {
   x: 10.25,
   y: 1,
   z: -11.6
  }), typeof OUT < "u") {
   }), LOCKS.carboot.look = {
    x: kx,
    y: .74,
    z: kz
   }
   }), LOCKS.citycar.look = {
    x: lx2,
    y: .86,
    z: lz2
   }
  }), LOCKS.vanback.look = {
   x: px,
   y: .95,
   z: pz
  }
  }), LOCKS.drawer.look = {
   x: 3.632 - .02,
   y: .8,
   z: 2.55
  }, LOCKS.drawer.stand = {
   x: 3.2,
   z: 2.6,
   yaw: P / 2
  }, typeof SHOP < "u") {
  }), LOCKS.lockbox.look = {
   x,
   y: .52,
   z: z - .13
  }
 }, LOCKS.motsdoor = {
  name: "Stan's front door",
  what: "door",
  tier: 2,
  outdoor: !0,
  icon: "keys",
  loot() {
   return []
  }
 }, LOCKS.motstill = {
  name: "Stan's till",
  what: "till",
  tier: 3,
  outdoor: !1,
  icon: "keys",
  after: 0,
  loot() {
   const c = roundMoney(Math.round(rand(28, 75)));
   Econ.add(c, "Stan's till");
   const P = Game.player.root.position;
   try {
    Heat.crime("theft", 1, {
     x: P.x,
     z: P.z,
     why: "till"
    })
   } catch {}
   return [fmtMoney(c, !0) + " in the till drawer"]
  }
 };
