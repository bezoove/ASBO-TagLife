// JOB_DEFS from the old game (v95), verbatim. Lines 101311-101459, 110008-110018 of game.pretty.js
 JOB_DEFS = {
  glasses: {
   giver: "brenda",
   title: "Collect the empties",
   desc: "Bring empty glasses back to the bar. 50p each, and a free drink when you hit ten.",
   need: 10,
   repeat: !0,
   min: 0
  },
  reg_readers: {
   giver: "reg",
   title: "Reg's reading glasses",
   desc: "Reg has lost his reading glasses somewhere in the pub. Brown case. He swears he had them earlier.",
   item: "readers",
   hide: !0,
   reward: {
    friend: 15,
    perk: "steady",
    text: "My glasses! Bless you. Here, let me show you something. Keep your elbow still and breathe out before you let go. Like this."
   }
  },
  terry_pint: {
   giver: "terry",
   title: "A pint for Terry",
   desc: "Terry's skint till Friday. Buy him a pint and he'll owe you one.",
   target: "terry",
   reward: {
    friend: 8,
    favour: 1,
    text: "You're a saint. I owe you one. A big one. Whatever you need."
   }
  },
  ron_collect: {
   giver: "ron",
   title: "Ron's thirty quid",
   desc: "Terry owes Ron £30. Get it off him. Ron says you can keep a fiver.",
   target: "terry",
   min: 1,
   reward: {
    money: 5,
    friend: 8,
    text: "Lovely. Pleasure doing business. Here's your cut."
   }
  },
  mickey_envelope: {
   giver: "mickey",
   title: "Special delivery",
   desc: "Take this envelope to Ron. Do not open it.",
   target: "ron",
   item: "envelope",
   min: 0,
   reward: {
    money: 40,
    respect: 8,
    text: "Good. Quick, quiet, no questions. I like that in a person."
   }
  },
  mickey_dive: {
   giver: "mickey",
   title: "Take a dive",
   desc: "Mickey has money riding on Kev. Lose a match to Kev with at least £20 on it. Make it look real. £150.",
   target: "kev",
   minRespect: 10,
   reward: {
    money: 150,
    respect: 15,
    text: "Beautiful. Very convincing. Remind me never to play cards with you."
   }
  },
  kev_wing: {
   giver: "kev",
   title: "Wingman",
   desc: "Kev fancies Shaz. He wants you to put in a good word.",
   target: "shaz",
   reward: {
    money: 20,
    friend: 10,
    text: "She said what? ...Right. Cheers anyway. Here's a score for your trouble."
   }
  },
  shaz_deano: {
   giver: "shaz",
   title: "Wipe the smirk off",
   desc: "Shaz wants someone to beat Deano at darts. For money. In front of everyone.",
   target: "deano",
   reward: {
    friend: 15,
    romance: 10,
    text: "Did you see his FACE? That's the best thing that's happened in this pub all year."
   }
  },
  reg_180: {
   giver: "reg",
   title: "Reg's challenge",
   desc: "Throw a 180 — practice counts — and Reg will teach you about grouping.",
   min: 2,
   reward: {
    friend: 12,
    perk: "grouping",
    text: "One hundred and eighty! I knew it. Right. Grouping. Same release, same spot, every time. You've got it now."
   }
  },
  moira_lighter: {
   giver: "moira",
   title: "Moira's lucky lighter",
   desc: "Moira's lost her lucky lighter. She reckons the beer garden.",
   item: "lighter",
   hide: "garden",
   reward: {
    money: 10,
    friend: 15,
    perk: "moira",
    text: "My lighter! Oh, you angel. Listen. When that machine's been eating money all night, that's when you play it. I'll tip you the wink."
   }
  },
  dave_brother: {
   giver: "dave",
   title: "Dave's brother",
   desc: "Mickey is leaning on Dave's brother for £100. Dave wants you to talk Mickey round, or pay it.",
   target: "mickey",
   min: 25,
   reward: {
    friend: 25,
    favour: 1,
    text: "You did that for our kid? I won't forget it. Any time you need a game, you've got one."
   }
  },
  callum_cover: {
   giver: "callum",
   title: "Cover for Callum",
   desc: "Callum is nipping out for ten minutes. If Brenda asks, he's changing a barrel.",
   target: "brenda",
   reward: {
    friend: 10,
    freeDrinks: 2,
    text: "Legend. Next two are on me."
   }
  },
  birthday: {
   giver: "*",
   title: "Birthday drink",
   desc: "It's {n}'s birthday and nobody's bought them a drink.",
   reward: {
    friend: 12,
    rumour: !0,
    text: "You remembered! Well, you didn't, I told you. Still counts."
   }
  }
 },
JOB_DEFS.ron_boot = {
 giver: "ron",
 title: "Ron's radio",
 desc: "Ron's lent a radio to a bloke who won't give it back. It's in the boot of the silver saloon in the car park. Ron will show you how to get in.",
 min: LOCK_TRUST.ronJob,
 reward: {
  money: 25,
  friend: 10,
  text: "That's my radio. Lovely. You've got hands for this, you know. If you need more picks, you know where I am."
 }
}, (() => {
