// ITEMS from the old game (v95), verbatim. Lines 97024-97247, 98374-98374, 102581-102587, 104341-104347, 109426-109433, 109767-109773, 113320-113330, 115222-115244, 118833-118847, 140476-140476, 140642-140642, 141181-141181, 143280-143280, 150313-150352 of game.pretty.js
 ITEMS = {
  watch: {
   name: '"Rolez" watch',
   desc: "Says Rolez on the face. Ticks, mostly.",
   base: 30,
   tags: ["knockoff"],
   icon: "watch"
  },
  perfume: {
   name: "Designer perfume",
   desc: "Smells like a duty-free accident.",
   base: 16,
   tags: ["knockoff"],
   icon: "perfume"
  },
  aftershave: {
   name: "Aftershave",
   desc: '"Pour Homme". Pour it on the homme.',
   base: 14,
   tags: ["knockoff"],
   icon: "perfume"
  },
  flowers: {
   name: "Forecourt flowers",
   desc: "Carnations from the petrol station. Romantic.",
   base: 7,
   tags: ["gift"],
   icon: "flower"
  },
  chocolates: {
   name: "Box of chocolates",
   desc: "Someone has already had the strawberry cream.",
   base: 6,
   tags: ["gift"],
   icon: "gift"
  },
  flights: {
   name: "Fancy flights",
   desc: "Holographic flights. Darts players love them.",
   base: 6,
   tags: ["gift"],
   icon: "dart"
  },
  scratchcard: {
   name: "Scratchcard",
   desc: "Scratch it. Go on.",
   base: 2,
   tags: ["use"],
   icon: "card"
  },
  lucky: {
   name: "Rabbit's foot",
   desc: "Lucky charm. Slightly fewer bounce-outs, apparently.",
   base: 15,
   tags: ["charm"],
   icon: "charm"
  },
  vinyl: {
   name: 'Northern Soul 7"',
   desc: "Rare-ish. Somebody will want it.",
   base: 22,
   tags: ["gift"],
   icon: "record"
  },
  book: {
   name: "The Inner Game of Darts",
   desc: "Read it to steady your nerves (+composure).",
   base: 9,
   tags: ["use"],
   icon: "book"
  },
  cigar: {
   name: "Cuban cigar",
   desc: "Possibly from Cuba. Possibly from Croydon.",
   base: 12,
   tags: ["gift"],
   icon: "cigar"
  },
  phone: {
   name: "Mobile phone",
   desc: "No charger. No questions.",
   base: 45,
   tags: ["hot"],
   icon: "phone"
  },
  laptop: {
   name: "Laptop",
   desc: "Still logged in to someone's email.",
   base: 90,
   tags: ["hot"],
   icon: "laptop"
  },
  meat: {
   name: "Meat raffle tray",
   desc: "Sausages, chops, mystery mince.",
   base: 14,
   tags: ["gift"],
   icon: "meat"
  },
  dartshirt: {
   name: "Darts shirt",
   desc: "Your name on the back. Opponents feel it.",
   base: 28,
   tags: ["wear"],
   icon: "shirt",
   wear: {
    top: "darts"
   }
  },
  chain: {
   name: "Gold chain",
   desc: "9 carat. Allegedly.",
   base: 20,
   tags: ["wear", "knockoff"],
   icon: "chain",
   wear: {
    chain: !0
   }
  },
  flatcap: {
   name: "Flat cap",
   desc: "Instantly 30% more trustworthy.",
   base: 12,
   tags: ["wear"],
   icon: "cap",
   wear: {
    hat: "flatcap"
   }
  },
  smartshirt: {
   name: "Smart shirt",
   desc: "Ironed. Makes a good impression.",
   base: 18,
   tags: ["wear"],
   icon: "shirt",
   wear: {
    top: "shirt"
   }
  },
  envelope: {
   name: "Sealed envelope",
   desc: "Thick. Do not open.",
   base: 0,
   tags: ["quest"],
   icon: "envelope"
  },
  keys: {
   name: "Terry's keys",
   desc: "A Spurs keyring and nine keys.",
   base: 0,
   tags: ["quest"],
   icon: "keys"
  },
  readers: {
   name: "Reg's reading glasses",
   desc: "Brown case. Smells of mints.",
   base: 0,
   tags: ["quest"],
   icon: "glasses"
  },
  lighter: {
   name: "Moira's lucky lighter",
   desc: 'Gold-ish. "Blackpool 1979".',
   base: 0,
   tags: ["quest"],
   icon: "lighter"
  },
  cigs: {
   name: "Twenty cigarettes",
   desc: "Duty-paid, probably. Someone else's.",
   base: 9,
   tags: ["hot"],
   icon: "cigar"
  },
  zippo: {
   name: "Brass lighter",
   desc: "Engraved with somebody else's initials.",
   base: 14,
   tags: ["hot"],
   icon: "lighter"
  },
  pocketwatch: {
   name: "Silver pocket watch",
   desc: "Heavy, ticking and worth more to its owner than to you.",
   base: 55,
   tags: ["hot"],
   icon: "watch"
  },
  goldwatch: {
   name: "Gold wristwatch",
   desc: "Actual gold. Actual heft. Actual trouble.",
   base: 120,
   tags: ["hot"],
   icon: "watch"
  },
  cufflinks: {
   name: "Silver cufflinks",
   desc: "Monogrammed. Not with your letters.",
   base: 38,
   tags: ["hot"],
   icon: "chain"
  },
  ring: {
   name: "Signet ring",
   desc: "Gold, with a crest. Somebody's grandfather's.",
   base: 46,
   tags: ["hot"],
   icon: "chain"
  },
  hipflask: {
   name: "Hip flask",
   desc: "Still has a tot of something in it. You sniff. Whisky.",
   base: 18,
   tags: ["hot"],
   icon: "pint"
  },
  debtcash: {
   name: "Terry's debt",
   desc: "Cash to hand over.",
   base: 0,
   tags: ["quest"],
   icon: "cash"
  }
 },
}), ITEMS.scratchcard.cat = "useful", ITEMS.flowers.cat = "useful", ITEMS.chocolates.cat = "useful";
ITEMS.cartons = {
 name: 'Carton of "tobacco"',
 desc: "Fell off a van. Many vans. Mostly this one.",
 base: 48,
 tags: ["hot"],
 icon: "cigar"
};
}), ITEMS.housekeys = {
 name: "Set of house keys",
 desc: "A Yale, a Chubb and a bottle opener. Nobody will thank you.",
 base: 3,
 tags: ["hot"],
 icon: "keys"
}, Object.assign(Sfx, {
})(), ITEMS.lockpick = {
 name: "Lockpick",
 desc: "A bent steel pick and a tension wrench. They snap. Keep a few.",
 base: 4,
 tags: ["tool"],
 icon: "pick",
 cat: "useful"
};
})(), ITEMS.ronradio = {
 name: "Ron's car radio",
 desc: "A Blaupunkt with half its buttons. Ron wants it back.",
 base: 0,
 tags: ["quest"],
 icon: "music"
};
ITEMS.barrage = {
 name: "Mega Barrage",
 desc: '49 shots, "display grade", forty seconds of mayhem. The box says to use it in a field. A big field. Well away from the pub.',
 base: 45,
 tags: ["firework"],
 icon: "barrage",
 objKind: "barrage",
 cat: "firework",
 age: !0,
 use: "firework"
};
})(), ITEMS.carkeys = {
 name: "Car keys",
 desc: "A fob, a Ford key and a leather tag. Somewhere in the car park somebody is about to have a worse night.",
 base: 6,
 tags: ["hot"],
 icon: "keys"
}, ITEMS.dartsset = {
 name: "Someone's darts",
 desc: "A well-travelled set in a plastic wallet, flights chewed to rags. They will want them back.",
 base: 22,
 tags: ["hot"],
 icon: "dart"
}, ITEMS.earrings = {
 name: "Earrings",
 desc: "Little gold hoops. Real enough.",
 base: 14,
 tags: ["wear", "hot"],
 icon: "chain",
 cat: "wear",
 wear: {
  earrings: !0
 }
};
ITEMS.boltcutters = {
 name: "Bolt cutters",
 desc: '24-inch, red handles. "For the allotment padlock". Snip a cable or a chain in seconds; a D-lock just laughs at them.',
 base: 22,
 tags: ["tool"],
 icon: "keys",
 cat: "tools"
}, ITEMS.grinder = {
 name: "Angle grinder",
 desc: "Cordless, with a cutting disc. Goes through a D-lock in seconds, loud as a jet and throwing sparks like Bonfire Night.",
 base: 55,
 tags: ["tool"],
 icon: "keys",
 cat: "tools"
}, typeof SHOP_STOCK < "u" && SHOP_STOCK.push({
}, OD.psword.cy = OD.psword.len / 2, OD.psword.lieR = OD.psword.rad, ITEMS.sword.objKind = "psword", (() => {
ITEMS.capgun.objKind = "capgun", ITEMS.fakepoo.objKind = "fakepoo", (() => {
}, ITEMS.camera.note = () => `${Photos.left()} shots left · ${Photos.list().length} photos kept.`;
 }, ITEMS.chalk.note = () => `${Math.round(Spray.left("chalk")/90*100)}% of the box left.`;
ITEMS.sparkplug = {
 name: "Spark plug",
 desc: "NGK. Still in its little box.",
 base: 2,
 tags: ["tool"],
 icon: "pick",
 cat: "useful"
}, ITEMS.jumpleads = {
 name: "Jump leads",
 desc: "Red to red, black to black. Everyone knows. Nobody does it right.",
 base: 9,
 tags: ["tool"],
 icon: "keys",
 cat: "useful"
}, ITEMS.oilcan = {
 name: "Can of 10W-40",
 desc: "A litre of oil. Handy for a squeaky door. Or a chip pan, if you are Terry.",
 base: 5,
 tags: ["tool"],
 icon: "bag",
 cat: "useful"
}, ITEMS.airfresh = {
 name: "Little Tree air freshener",
 desc: "Vanilla. Hangs from a mirror for nine months and then stops.",
 base: 1.5,
 tags: ["gift"],
 icon: "gift"
}, ITEMS.scrapmetal = {
 name: "Bit of scrap metal",
 desc: "Brass, copper, an exhaust bracket. Stan weighs it by eye.",
 base: 3,
 tags: ["scrap"],
 icon: "bag"
}, ITEMS.hubcap = {
 name: "Hubcap",
 desc: "Not yours. Off a Vauxhall. Stan does not ask.",
 base: 6,
 tags: ["hot"],
 icon: "bag"
}, CAST.stan = {
