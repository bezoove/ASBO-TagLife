// HAIR_LIST from the old game (v95), verbatim. Lines 86556-86565 of game.pretty.js
   HAIR_LIST = ["bald", "skinhead", "buzz", "crop", "fade", "short", "quiff", "spiky", "slick", "undercut", "flattop", "mohawk", "combover", "curtains", "sidesweep", "balding", "ring", "pensioner", "mullet", "curly", "perm", "afro", "braids", "locs", "bob", "bob2", "pixie", "midlength", "long", "ponytail", "bun"].map(v => [v, {
    combover: "Comb-over",
    ring: "Bald ring",
    locs: "Locs",
    bob2: "Bob (side)",
    sidesweep: "Side sweep",
    fade: "Skin fade",
    flattop: "Flat top",
    braids: "Braids (cornrows)"
   } [v] || cap2(v)]);
