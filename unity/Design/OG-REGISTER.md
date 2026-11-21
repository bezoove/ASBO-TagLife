# Old game register

Every item the Unity rebuild has to contain, with the old game's code behind it (the behaviour spec: its exact lines, numbers, states and rules) and what you asked for.
Each module file lists every behaviour with its line in og/game.pretty.js, everything it says or shows, its states and its tuning numbers.
Built by unity/tools/og_extract.py and og_register.py from www/index.html (v95).

## Your rules for the whole rebuild

- [x] **UK council estate setting, rough-looking people**: Tracksuits, hoodies, worn clothes, rough faces, no glamour.
  - You asked: People should look rough, not glamorous.
- [x] **Cars in the old game's UK styles**: All nine models from the old car table, scruffy variants, UK plates.
  - You asked: UK car styles, as the old game had them.
  - Old game: [DRIVE_CARS](og/modules/DRIVE_CARS.md), [CARJACK_NAMES](og/modules/CARJACK_NAMES.md) (0 behaviours, 8 lines said or shown, 5 tuning numbers)
- [x] **No real brand names or logos**: Every shop, car and product name is made up.
  - You asked: No real brands or logos anywhere in the game.
- [x] **Touch controls customisable like the old game**: Drag, resize, hide; separate on-foot and vehicle layouts; presets; reset.
  - Old game: [TOUCH_PRESETS](og/modules/TOUCH_PRESETS.md), [UI:controls](og/modules/UI-controls.md) (29 behaviours, 33 lines said or shown, 3 tuning numbers)
- [x] **Smooth frame rate checked on every change**: Frame-time readout in test builds; phones lower render scale and shadows themselves when they can't keep up.
  - You asked: Heavy optimisation pass, kept up for every change from now on.
  - You asked: Smooth on phones up to about five years old.
  - Old game: [Perf](og/modules/Perf.md), [PerfProf](og/modules/PerfProf.md), [Adapt](og/modules/Adapt.md), [Tier](og/modules/Tier.md), [DynRes](og/modules/DynRes.md), [MG](og/modules/MG.md), [BQ](og/modules/BQ.md), [P9](og/modules/P9.md), [TexRes](og/modules/TexRes.md), [Perf7](og/modules/Perf7.md), [Perf8](og/modules/Perf8.md), [CharPool](og/modules/CharPool.md), [SpatialQ](og/modules/SpatialQ.md) (66 behaviours, 0 lines said or shown, 125 tuning numbers)
- [x] **Commercially safe assets only**: Asset Store packs you added (standard licence), CC0 and our own work. Packs kept out of GitHub.
  - You asked: No non-commercial or restricted sources (no Mixamo, Ready Player Me, CC BY-NC).
- [~] **Sound and chat only where you are**: Each place has its own sound. The old game's rule is recorded; checked as each interior goes in.
  - You asked: Pub chatter and pint requests never outside the pub; every interior has its own sound.
  - You asked: Enforced with a rule and a test so it stays fixed.
  - Old game: [Locale](og/modules/Locale.md), [PubOnly](og/modules/PubOnly.md), [Spatial](og/modules/Spatial.md), [Zones](og/modules/Zones.md) (57 behaviours, 36 lines said or shown, 35 tuning numbers)
- [~] **Portrait and landscape**: Landscape now; portrait for darts like the old game.
  - You asked: Must work in portrait and landscape.
  - Old game: [Orient](og/modules/Orient.md) (5 behaviours, 2 lines said or shown, 0 tuning numbers)
- [ ] **iPhone version**: Later, after Android.
  - You asked: Wants it on iOS as well eventually.

## Story and setting

- [ ] **Intro: released from Marshfield Secure Unit, tag fitted**: "Eighteen months. You'd think they'd do a cake." The bag of belongings, licence conditions, the walk home.
  - You asked: Released from a detention centre but put on an ankle tag; the tag limits the player to the playable map.
  - Old game: [Release](og/modules/Release.md), [Intro](og/modules/Intro.md), [Intro4](og/modules/Intro4.md), [Intro5](og/modules/Intro5.md), [IntroSets](og/modules/IntroSets.md), [IntroLook](og/modules/IntroLook.md), [IntroHand](og/modules/IntroHand.md), [IntroAudio](og/modules/IntroAudio.md), [Cine](og/modules/Cine.md) (152 behaviours, 78 lines said or shown, 156 tuning numbers)
- [ ] **Electronic tag and zone**: Phone buzzes near the edge; leaving is a violation and the tag reports you.
  - You asked: Tag limits extended to cover the long road out of the estate.
  - Old game: [Tag](og/modules/Tag.md), [BUZZ](og/modules/BUZZ.md) (22 behaviours, 12 lines said or shown, 18 tuning numbers)
- [ ] **Home: the flat over the Golden Dragon, with Mum**: One bedroom, you sleep on the sofa; Mum's day, tea, meals, rent, notes.
  - You asked: Lives with his mum in the flat above the Chinese takeaway (one bedroom, sleeps on the sofa).
  - Old game: [Home](og/modules/Home.md), [Warm](og/modules/Warm.md), [NOTES](og/modules/NOTES.md) (67 behaviours, 100 lines said or shown, 45 tuning numbers)
- [ ] **Safe stash at home**: Items in it can't be stolen.
  - You asked: A safe item stash at home that items can't be stolen from.
  - Old game: [Stash](og/modules/Stash.md) (10 behaviours, 22 lines said or shown, 10 tuning numbers)
- [ ] **Day, night, weather and the clock**: Pub hours, "Time, gentlemen, please!", shop hours, daily events.
  - You asked: In-game time is not tied to pub opening hours.
  - Old game: [DayNight](og/modules/DayNight.md), [Clock](og/modules/Clock.md), [ClockFix](og/modules/ClockFix.md), [TIME_SCHED](og/modules/TIME_SCHED.md), [TIME](og/modules/TIME.md), [Weather](og/modules/Weather.md), [NightLight](og/modules/NightLight.md), [Game:day](og/modules/Game-day.md) (69 behaviours, 33 lines said or shown, 142 tuning numbers)
- [ ] **Saving and loading**: Save slots, autosave, saving on a wee.
  - You asked: Wee saves the game (old game).
  - Old game: [Slots](og/modules/Slots.md), [Game:save](og/modules/Game-save.md), [UI:save](og/modules/UI-save.md), [META](og/modules/META.md) (79 behaviours, 121 lines said or shown, 72 tuning numbers)
- [ ] **Character creator, haircut and photo face**: Line up your eyes and mouth on a photo; it stays on the phone.
  - You asked: Photo face on the player.
  - Old game: [UI:creator](og/modules/UI-creator.md), [FacePhoto](og/modules/FacePhoto.md), [Faces](og/modules/Faces.md), [Wardrobe](og/modules/Wardrobe.md), [HAIR_LIST](og/modules/HAIR_LIST.md), [PRICE](og/modules/PRICE.md), [Barber](og/modules/Barber.md) (26 behaviours, 73 lines said or shown, 127 tuning numbers)
- [ ] **Storylines and missions**: Loose and non-linear; build up money and status.
  - You asked: Slow slide into crime under a shady boss, with the option to betray him.
  - You asked: Romance.
  - You asked: Starting homeless, saving for a bedsit.
  - You asked: Buying clothes and items by phone.
  - You asked: A classic English bobby policing the town.
  - You asked: Missions that build up wealth and material status.
  - You asked: The game is about the main character; darts is one pub activity.
  - Old game: [Jobs](og/modules/Jobs.md), [JOB_DEFS](og/modules/JOB_DEFS.md) (16 behaviours, 100 lines said or shown, 13 tuning numbers)

## The town

- [~] **Roads, pavements, zebra crossing, traffic**: Main road, zebra and traffic in; the wider town road network isn't yet.
  - You asked: Pavements fixed so people walk on them.
  - Old game: [Traffic](og/modules/Traffic.md), [TRAFFIC](og/modules/TRAFFIC.md), [TownTraffic](og/modules/TownTraffic.md), [TownRoads](og/modules/TownRoads.md), [RoadNet](og/modules/RoadNet.md), [RN](og/modules/RN.md), [Town](og/modules/Town.md), [TOWN](og/modules/TOWN.md), [TownCorners](og/modules/TownCorners.md), [CORNER](og/modules/CORNER.md), [PedPaths](og/modules/PedPaths.md), [ROAD](og/modules/ROAD.md), [Land](og/modules/Land.md) (111 behaviours, 14 lines said or shown, 103 tuning numbers)
- [ ] **The long road out: underpass, forest, valley, hills**: Fast road from the underpass outside the Eggy Duck round to the other side.
  - You asked: Partly underground, forest, valley and steep hills; tag limits extended to cover it.
  - Old game: [Hwy](og/modules/Hwy.md), [HWY](og/modules/HWY.md) (33 behaviours, 0 lines said or shown, 27 tuning numbers)
- [~] **Abbots Wend: gated thatched-cottage community**: Cottages built; the hostile country folk, the gatehouse and alarms aren't yet.
  - You asked: Hostile country folk.
  - Old game: [POSH](og/modules/POSH.md), [POSH_LINES](og/modules/POSH_LINES.md), [POSH_ALARM](og/modules/POSH_ALARM.md), [Cot](og/modules/Cot.md), [PoshKit](og/modules/PoshKit.md) (116 behaviours, 60 lines said or shown, 107 tuning numbers)
- [ ] **The estate: flats, Hawthorn Court tower, garages, courtyard**
  - You asked: A block of flats.
  - You asked: A community playground area.
  - Old game: [ASH](og/modules/ASH.md), [HC](og/modules/HC.md), [FL](og/modules/FL.md), [FlatsDoors](og/modules/FlatsDoors.md), [Dwell](og/modules/Dwell.md), [Blind](og/modules/Blind.md), [EST](og/modules/EST.md) (154 behaviours, 52 lines said or shown, 94 tuning numbers)
- [ ] **The Eggy Duck pub**: Bar, carpet, garden, benches, toilets, fruit machine, pool room, hidden areas.
  - You asked: Bigger pub with a complex layout and hidden stealth areas.
  - You asked: Wider walkways, no blocked areas.
  - You asked: Carpeted section in front of the bar.
  - You asked: Garden and benches.
  - You asked: Urinals fixed (old-game bug).
  - You asked: Garden doorway not showing as bricks when coming back in.
  - You asked: Fruit machine not black in third person.
  - Old game: [World:pub](og/modules/World-pub.md), [PUB](og/modules/PUB.md), [PubSign](og/modules/PubSign.md), [POOLT](og/modules/POOLT.md), [HIDING_SPOTS](og/modules/HIDING_SPOTS.md) (48 behaviours, 62 lines said or shown, 66 tuning numbers)
- [ ] **Shops and businesses**: Raj's, Golden Dragon, post office, bookies, chippy, launderette, barbers, pharmacy, pound shop and the rest.
  - Old game: [Shp](og/modules/Shp.md), [Shops3](og/modules/Shops3.md), [SHOP](og/modules/SHOP.md), [SHOP_STOCK](og/modules/SHOP_STOCK.md), [SHOP_CATS](og/modules/SHOP_CATS.md), [ARC](og/modules/ARC.md), [World:shop](og/modules/World-shop.md), [RAJ](og/modules/RAJ.md) (74 behaviours, 588 lines said or shown, 91 tuning numbers)
- [ ] **Industrial estate: scrapyard, pawn, taxi office, garage**
  - You asked: A garage business.
  - Old game: [INDX](og/modules/INDX.md), [IND](og/modules/IND.md), [Mech](og/modules/Mech.md) (48 behaviours, 359 lines said or shown, 31 tuning numbers)
- [ ] **Police station and hospital**: A&E reception, triage, pharmacy counter, police front desk.
  - Old game: [Civic](og/modules/Civic.md), [CivicLive](og/modules/CivicLive.md), [CVP](og/modules/CVP.md), [CIVIC](og/modules/CIVIC.md) (101 behaviours, 211 lines said or shown, 60 tuning numbers)
- [ ] **Houses with people living in them**: Their own routines, bedtimes, lights, and reactions when you're inside.
  - Old game: [Hm](og/modules/Hm.md), [Homes](og/modules/Homes.md), [Realism](og/modules/Realism.md), [BEDS](og/modules/BEDS.md) (142 behaviours, 45 lines said or shown, 93 tuning numbers)
- [ ] **Phonebox**: 999 free; A1 Taxis ("Twenty minutes, mate. Maybe thirty."), pizza, the speaking clock.
  - Old game: [Phonebox](og/modules/Phonebox.md) (8 behaviours, 22 lines said or shown, 8 tuning numbers)
- [ ] **Bus stop and Route 41**: Sit and wait; pick how long.
  - Old game: [Wait](og/modules/Wait.md), [Bus41](og/modules/Bus41.md), [Bus](og/modules/Bus.md) (22 behaviours, 11 lines said or shown, 21 tuning numbers)
- [ ] **Birds and wildlife**
  - Old game: [Birds](og/modules/Birds.md), [BD](og/modules/BD.md) (24 behaviours, 0 lines said or shown, 31 tuning numbers)
- [ ] **Big construction site**: Planned: a half-built block of flats with a tower crane, scaffolding you can climb, diggers, portacabins, fencing and hoardings. Builders on shift by day; a security guard and his dog at night. Tools, copper and scrap worth taking to the scrapyard.
  - You asked: A big construction site.
- [ ] **Creepy old castle on a big hill**: Planned: set apart from the town on its own hill, up a winding lane and footpath off the long road out. Crumbling walls, a keep, a dungeon and crypt, crows, fog and creaks after dark.
  - You asked: A creepy old castle set aside on a big hill.
- [ ] **Casino**: Planned: on the edge of town by the long road out, with a made-up name. Doorman, cashier's cage for chips, bar, croupiers, security and regulars. Only the game's own money goes in.
  - You asked: A casino where you can gamble your money.
- [~] **Props everywhere**: Hedges, walls, lamps, cars in; most small street and indoor props not yet.
  - You asked: World detail audit: recognisable props everywhere.
  - You asked: Door frames right (old-game bug).
  - Old game: [Decor](og/modules/Decor.md), [Props](og/modules/Props.md), [KIT](og/modules/KIT.md), [Grime](og/modules/Grime.md), [Decay](og/modules/Decay.md), [Walls](og/modules/Walls.md), [World:light](og/modules/World-light.md), [World:door](og/modules/World-door.md), [World](og/modules/World.md) (654 behaviours, 473 lines said or shown, 387 tuning numbers)

## People

- [~] **Residents with daily routines**: Walk about, visit, chat, go home. Still to come: households in the flats, sleeping, being woken by noise, the old game's full set of trips.
  - You asked: No people walking through walls (old-game bug).
  - Old game: [TF](og/modules/TF.md), [CP](og/modules/CP.md), [NPCFit](og/modules/NPCFit.md), [TownBeat](og/modules/TownBeat.md), [AMB](og/modules/AMB.md), [LINES_TF](og/modules/LINES_TF.md) (75 behaviours, 78 lines said or shown, 103 tuning numbers)
- [x] **Drunks, idling variety, chatting gestures**
  - You asked: Drunk people barely notice being weed on (old game).
- [ ] **Pub regulars and staff**: Who comes in when, where they sit, ordering, Brenda and Callum behind the bar, closing time.
  - Old game: [NPCs](og/modules/NPCs.md), [Bar](og/modules/Bar.md), [Closing](og/modules/Closing.md), [DRINKS](og/modules/DRINKS.md), [Ambience](og/modules/Ambience.md), [BARKS](og/modules/BARKS.md), [BRENDA](og/modules/BRENDA.md), [BRENDA_EVAC](og/modules/BRENDA_EVAC.md), [CHEERS](og/modules/CHEERS.md) (155 behaviours, 72 lines said or shown, 87 tuning numbers)
- [ ] **The named cast**: Brenda, Callum, Terry, Shaz, Deano, Reg, Kev, Priya, Mickey, Tank, Moira, Ron, Dave, The Duke, PCSO Biggs, PCSO Pook.
  - Old game: [CAST](og/modules/CAST.md) (0 behaviours, 30 lines said or shown, 25 tuning numbers)
- [ ] **Talking to people**: Conversations, likes and dislikes, mates, gifts, flirting, dates, rumours, persuading, borrowing.
  - Old game: [Social](og/modules/Social.md), [LINES](og/modules/LINES.md), [LINES2](og/modules/LINES2.md), [ICONS](og/modules/ICONS.md), [SAY](og/modules/SAY.md), [NEUTRAL](og/modules/NEUTRAL.md), [CF_LINES](og/modules/CF_LINES.md), [PS_LINES](og/modules/PS_LINES.md), [UI:talk](og/modules/UI-talk.md), [Game:talk](og/modules/Game-talk.md), [PD](og/modules/PD.md) (75 behaviours, 896 lines said or shown, 41 tuning numbers)
- [ ] **People starting conversations with you**: They come over for a chat, a game, to cadge, to challenge you; not when you're busy or driving.
  - You asked: NPCs start interactions with the player (old game).
  - You asked: Chatty NPCs fixed.
  - Old game: [Approach](og/modules/Approach.md), [Chatty](og/modules/Chatty.md) (31 behaviours, 11 lines said or shown, 25 tuning numbers)
- [ ] **Reactions to what you do**: Laughing or tutting at your antics, "Who did that?!", blame for smashed windows, stink bombs, mess.
  - Old game: [Chaos](og/modules/Chaos.md), [Fun](og/modules/Fun.md), [Glass](og/modules/Glass.md), [Poo](og/modules/Poo.md), [Stink](og/modules/Stink.md) (32 behaviours, 53 lines said or shown, 80 tuning numbers)
- [ ] **Gangs**: T-Bone's lot and the Kestrel Crew: turf, colours, dealing, favours, rivalry, brawls.
  - Old game: [GANGS](og/modules/GANGS.md) (27 behaviours, 107 lines said or shown, 23 tuning numbers)
- [ ] **Cashmere, Chewie and Olly**: The pub dog, the street dog and the pub cat.
  - You asked: Cashmere: bigger cream and tan dog, the pub dog, playful, gently bites people.
  - You asked: Chewie: small brown dog, lives on the streets, nice but bites if touched.
  - You asked: Olly: tabby cat, the pub cat, always hunting.
  - Old game: [Pets](og/modules/Pets.md), [PetSpecies](og/modules/PetSpecies.md), [PetRig](og/modules/PetRig.md) (65 behaviours, 54 lines said or shown, 179 tuning numbers)
- [ ] **House party at Gary Pickering's flat**
  - Old game: [PARTY_APP](og/modules/PARTY_APP.md), [PARTY](og/modules/PARTY.md), [PartyMusic](og/modules/PartyMusic.md) (38 behaviours, 131 lines said or shown, 38 tuning numbers)
- [~] **Voices and babble**: A few recorded lines so far.
  - You asked: Character babble.
  - You asked: Human-sounding voices; free open voices were too flat; happy to pay for a much better voice service.
  - Old game: [Voices](og/modules/Voices.md), [Babble](og/modules/Babble.md) (12 behaviours, 2 lines said or shown, 25 tuning numbers)

## Your body

- [x] **Walk, run, sprint, jump, landing**: Faster running in v8.
  - You asked: Movement must look human: a massive overhaul of running, jumping, throwing and walking with motion capture.
  - Old game: [Player](og/modules/Player.md), [Gait](og/modules/Gait.md), [Moves](og/modules/Moves.md), [Mocap](og/modules/Mocap.md), [MocapData](og/modules/MocapData.md), [Anim](og/modules/Anim.md), [Anim2](og/modules/Anim2.md), [Input](og/modules/Input.md) (91 behaviours, 16 lines said or shown, 122 tuning numbers)
- [ ] **Sit on chairs, stools and benches**
  - You asked: Player can sit on upright chairs (old game).
  - Old game: [Sit](og/modules/Sit.md), [FpSeat](og/modules/FpSeat.md) (12 behaviours, 3 lines said or shown, 2 tuning numbers)
- [ ] **Climbing and ladders**: Fall too far and something in your knee goes click.
  - Old game: [Climb](og/modules/Climb.md), [Ladders](og/modules/Ladders.md), [LADDER_KINDS](og/modules/LADDER_KINDS.md) (76 behaviours, 70 lines said or shown, 59 tuning numbers)
- [ ] **First and third person view**
  - You asked: First/third person toggle, centre dot and a discreet info box (old game).
  - Old game: [View](og/modules/View.md), [CamRig](og/modules/CamRig.md) (39 behaviours, 10 lines said or shown, 31 tuning numbers)
- [ ] **Bladder**: Meter, warning, wee button, puddles, people's reactions.
  - You asked: Hands at the crotch while weeing (old game).
  - You asked: Drunks barely notice being weed on.
  - Old game: [Bladder](og/modules/Bladder.md) (30 behaviours, 13 lines said or shown, 48 tuning numbers)
- [ ] **Drink, food and being drunk**: Pints, sipping, getting merry, sobering up, snacks.
  - Old game: [Bar](og/modules/Bar.md), [DRINKS](og/modules/DRINKS.md), [FOOD](og/modules/FOOD.md), [CRISPS](og/modules/CRISPS.md), [CAN_DRINK](og/modules/CAN_DRINK.md) (13 behaviours, 29 lines said or shown, 7 tuning numbers)

## Fighting, injury and emergency services

- [~] **Punch, haymaker, kick**: Much faster in v8. Still to come: fists-up stance and the old game's other moves.
  - You asked: Punches, kicks and running a lot faster.
  - Old game: [Fight](og/modules/Fight.md), [Fists](og/modules/Fists.md), [FIGHT_ANIMS](og/modules/FIGHT_ANIMS.md), [ATK](og/modules/ATK.md), [TBTN](og/modules/TBTN.md) (104 behaviours, 20 lines said or shown, 62 tuning numbers)
- [~] **People fight back or run; knockouts; getting up; limping**: In. Still to come: others wading in, Brenda breaking it up, dropping drinks.
  - You asked: Survivors get up and limp.
- [x] **Paramedics can be hit and fight back**: v8.
  - You asked: Emergency workers can be punched and kicked and fight.
- [ ] **Weapons**: Flick knife, kitchen knife, baseball bat, cosh, knuckle duster, crowbar, shotgun.
  - Old game: [Weapons](og/modules/Weapons.md) (16 behaviours, 0 lines said or shown, 10 tuning numbers)
- [~] **Gore, injuries and blood**: Car-hit gore, pools; blood squares fixed in v8. Injuries that last, dismemberment not yet.
  - You asked: Injury, death, gore, ambulances and car-hit ragdoll.
  - Old game: [Gore](og/modules/Gore.md), [Injury](og/modules/Injury.md), [Ragdoll](og/modules/Ragdoll.md), [RagCar](og/modules/RagCar.md), [Rip](og/modules/Rip.md), [BURNS](og/modules/BURNS.md) (82 behaviours, 28 lines said or shown, 147 tuning numbers)
- [~] **Ambulance and paramedics**: New ambulance in v8; their lines and hospital trip not yet.
  - You asked: Ambulances match the new car style.
  - Old game: [Amb](og/modules/Amb.md), [AmbKit](og/modules/AmbKit.md), [HV](og/modules/HV.md) (74 behaviours, 25 lines said or shown, 88 tuning numbers)
- [ ] **Searching people who've been knocked out**: "Still warm. Not your size, probably."
  - Old game: [Loot](og/modules/Loot.md) (38 behaviours, 38 lines said or shown, 19 tuning numbers)
- [ ] **Armed police**: Van, armed police aiming guns, arrest, for deep trouble.
  - You asked: A police SWAT team for deep trouble.
  - Old game: [Swat](og/modules/Swat.md), [SwatKit](og/modules/SwatKit.md) (78 behaviours, 21 lines said or shown, 108 tuning numbers)

## Crime and the police

- [ ] **Stealth: sneaking, being noticed, light and noise**
  - Old game: [Stealth](og/modules/Stealth.md), [STEALTH_PERKS](og/modules/STEALTH_PERKS.md), [Sneak](og/modules/Sneak.md), [NEEDS_EYES](og/modules/NEEDS_EYES.md), [UI:stealth](og/modules/UI-stealth.md) (91 behaviours, 58 lines said or shown, 96 tuning numbers)
- [ ] **Pickpocketing**: Hold to lift, let go when they start to turn round.
  - You asked: Stealing made harder (old game).
  - Old game: [LOOT_NAMES](og/modules/LOOT_NAMES.md), [LIFT_TIERS](og/modules/LIFT_TIERS.md) (0 behaviours, 1 lines said or shown, 0 tuning numbers)
- [ ] **Rummaging houses and Mickey's stash**
  - Old game: [Rummage](og/modules/Rummage.md), [HLOOT](og/modules/HLOOT.md) (19 behaviours, 37 lines said or shown, 14 tuning numbers)
- [ ] **Locks and lock picking, with skill levels**: Apprentice, Adept, Expert; grinders and cutters.
  - Old game: [Locks](og/modules/Locks.md), [Lockpick](og/modules/Lockpick.md), [LOCKS](og/modules/LOCKS.md), [LOCK_RANKS](og/modules/LOCK_RANKS.md), [LOCK_PICK_PRICE](og/modules/LOCK_PICK_PRICE.md) (43 behaviours, 79 lines said or shown, 42 tuning numbers)
- [ ] **Offences, heat, warnings, arrest and cells**
  - Old game: [Heat](og/modules/Heat.md), [Offences](og/modules/Offences.md), [Game:crime](og/modules/Game-crime.md) (56 behaviours, 29 lines said or shown, 36 tuning numbers)
- [ ] **PC Hargreaves "Bobby"**: The town's classic English bobby: patrol, watching, warnings, "Hello, hello, hello".
  - You asked: A classic English bobby (Bobby) policing the town.
  - Old game: [Bobby](og/modules/Bobby.md) (68 behaviours, 42 lines said or shown, 35 tuning numbers)
- [ ] **PCSOs Biggs and Pook**: Their beat, the radio, mocking, and the places they won't go.
  - Old game: [PCSO](og/modules/PCSO.md), [Blind](og/modules/Blind.md) (26 behaviours, 105 lines said or shown, 32 tuning numbers)
- [ ] **Police car**
  - Old game: [PoliceCar](og/modules/PoliceCar.md), [PoliceKit](og/modules/PoliceKit.md) (25 behaviours, 3 lines said or shown, 44 tuning numbers)
- [ ] **Getting barred from the pub**
  - You asked: No crash when barred (old-game bug).
  - Old game: [Barred](og/modules/Barred.md) (14 behaviours, 2 lines said or shown, 13 tuning numbers)
- [ ] **Breaking into houses, alarms**
  - Old game: [Mech](og/modules/Mech.md) (17 behaviours, 14 lines said or shown, 16 tuning numbers)
- [ ] **Taking cars: break-ins and carjacking**
  - Old game: [Jack](og/modules/Jack.md), [CarInt](og/modules/CarInt.md), [Hijack](og/modules/Hijack.md) (64 behaviours, 28 lines said or shown, 51 tuning numbers)
- [ ] **Graffiti and chalk**: Spray cans and pavement chalk.
  - Old game: [Spray](og/modules/Spray.md) (59 behaviours, 22 lines said or shown, 34 tuning numbers)

## Games, bets and the casino

- [ ] **Darts**: Swipe throws from your own view, pencil grip, AI opponents, match rules, the caller.
  - You asked: Swipe and release, held dart, natural throw, portrait view.
  - You asked: One continuous motion; index finger on top of the barrel; pencil grip.
  - You asked: Faster forward stroke; fix the torn sleeve.
  - You asked: Dartboard with no wobble, the whole board and numbers visible.
  - Old game: [DartsGame](og/modules/DartsGame.md), [DartsAI](og/modules/DartsAI.md), [Darts3D](og/modules/Darts3D.md), [DartHand](og/modules/DartHand.md), [DartDepth](og/modules/DartDepth.md), [DartParts](og/modules/DartParts.md), [FPThrow](og/modules/FPThrow.md), [Checkout](og/modules/Checkout.md), [GAME_NAMES](og/modules/GAME_NAMES.md), [Caller](og/modules/Caller.md), [GRIPS](og/modules/GRIPS.md), [Game:darts](og/modules/Game-darts.md), [UI:darts](og/modules/UI-darts.md) (94 behaviours, 46 lines said or shown, 92 tuning numbers)
- [ ] **Pool**: Physics, rules, AI players, spectators, cue and ball sounds.
  - You asked: Visible characters playing; ball sounds, cue hold, no overlaps, felt.
  - You asked: Pool room bugs from the old game fixed.
  - Old game: [PoolGame](og/modules/PoolGame.md), [PoolPhys](og/modules/PoolPhys.md), [PoolAI](og/modules/PoolAI.md), [PoolUI](og/modules/PoolUI.md), [PoolArt](og/modules/PoolArt.md), [PoolBody](og/modules/PoolBody.md), [POOL](og/modules/POOL.md) (115 behaviours, 22 lines said or shown, 86 tuning numbers)
- [ ] **Jukebox**: Paste a Spotify or YouTube playlist link; free public-domain songs built in.
  - Old game: [Jukebox](og/modules/Jukebox.md), [JUKE_TRACKS](og/modules/JUKE_TRACKS.md), [TUNES](og/modules/TUNES.md), [UI:juke](og/modules/UI-juke.md), [Game:juke](og/modules/Game-juke.md) (16 behaviours, 42 lines said or shown, 10 tuning numbers)
- [ ] **Fruit machine**
  - Old game: [FruitMachine](og/modules/FruitMachine.md), [FM_NAMES](og/modules/FM_NAMES.md) (17 behaviours, 1 lines said or shown, 11 tuning numbers)
- [ ] **Snap**: Cards for money with the regulars.
  - Old game: [Snap](og/modules/Snap.md), [CARDS](og/modules/CARDS.md) (9 behaviours, 17 lines said or shown, 3 tuning numbers)
- [ ] **Horse racing bets with Ron**
  - Old game: [Racing](og/modules/Racing.md), [RACE_HORSES](og/modules/RACE_HORSES.md), [RACE_COURSES](og/modules/RACE_COURSES.md) (10 behaviours, 19 lines said or shown, 3 tuning numbers)
- [ ] **Casino: roulette**: European wheel with a single zero; every bet on the layout at real payouts, so the house edge is the real one.
  - You asked: Properly working casino games where you gamble your money.
- [ ] **Casino: blackjack**: Six-deck shoe, dealer stands on 17, blackjack pays 3 to 2; hit, stand, double, split and insurance.
  - You asked: Properly working casino games where you gamble your money.
- [ ] **Casino: slot machines**: Real reels with fixed odds and pay tables, paying back a bit under what goes in, like real machines.
  - You asked: Properly working casino games where you gamble your money.
- [ ] **Casino: poker**: Texas Hold'em at a table with other players who bluff and fold like people; the house takes a cut of each pot.
  - You asked: Properly working casino games where you gamble your money.
- [ ] **Pick up, carry, throw and smash things**: Glasses, chairs, bottles.
  - You asked: Physics props: pick up, carry, move, throw, smash, damage.
  - You asked: No chair stuck in your hands (old-game bug).
  - Old game: [Objs](og/modules/Objs.md), [GlassFx](og/modules/GlassFx.md), [Brk](og/modules/Brk.md), [BRK_KIND](og/modules/BRK_KIND.md) (189 behaviours, 38 lines said or shown, 108 tuning numbers)

## Money and items

- [ ] **Money, prices and haggling**
  - Old game: [Econ](og/modules/Econ.md) (9 behaviours, 1 lines said or shown, 0 tuning numbers)
- [ ] **Loans from Mickey, favours, errands**: Mickey's interest, Tank at the door.
  - Old game: [TIME_SCHED](og/modules/TIME_SCHED.md) (0 behaviours, 0 lines said or shown, 2 tuning numbers)
- [ ] **Pockets and items**: All 66 from the old game: gifts, valuables, weapons, fireworks, food, drink and pranks.
  - Old game: [Items](og/modules/Items.md), [ITEMS](og/modules/ITEMS.md), [UI:items](og/modules/UI-items.md), [KINDS](og/modules/KINDS.md) (94 behaviours, 184 lines said or shown, 50 tuning numbers)
- [ ] **Using items**: Fireworks, air horn, water pistol, eggs, whoopee cushion, itching powder, stink bombs, superglue, kazoo, bubbles…
  - Old game: [ItemUse](og/modules/ItemUse.md), [Later](og/modules/Later.md) (24 behaviours, 65 lines said or shown, 25 tuning numbers)
- [ ] **Your journal and skills**
  - Old game: [UI:journal](og/modules/UI-journal.md) (7 behaviours, 18 lines said or shown, 0 tuning numbers)

## Toys and messing about

- [ ] **Skateboard**
  - Old game: [Skate](og/modules/Skate.md) (8 behaviours, 8 lines said or shown, 12 tuning numbers)
- [ ] **Yo-yo**: Walk the dog, round the world.
  - Old game: [Yoyo](og/modules/Yoyo.md) (3 behaviours, 7 lines said or shown, 3 tuning numbers)
- [ ] **Marbles**: "Your feet go out from under you on your own marbles. Karma."
- [ ] **Disposable camera and photos**
  - Old game: [Photos](og/modules/Photos.md) (4 behaviours, 7 lines said or shown, 9 tuning numbers)
- [ ] **Remote-control car**
  - Old game: [RC](og/modules/RC.md) (6 behaviours, 8 lines said or shown, 9 tuning numbers)
- [ ] **Bikes**: Pedal, sprint, bell, locks and cutting them.
  - You asked: Bikes rideable with vehicle controls, customisable in settings.
  - Old game: [Bikes](og/modules/Bikes.md), [BikeModel](og/modules/BikeModel.md), [BIKES](og/modules/BIKES.md), [BIKE_STYLE_NAME](og/modules/BIKE_STYLE_NAME.md), [BIKE_LOCK_NAME](og/modules/BIKE_LOCK_NAME.md) (46 behaviours, 52 lines said or shown, 52 tuning numbers)
- [ ] **Car surfing**: Jump on a moving car and try to stay on.
  - Old game: [Surf](og/modules/Surf.md) (20 behaviours, 17 lines said or shown, 15 tuning numbers)

## Driving

- [x] **Old game's controls and handling, with reverse**: Forward (hold to speed up, drag up to boost), Reverse, Brake, Left and Right. v8.
  - You asked: Car controls replicate the old game's.
  - Old game: [Driving](og/modules/Driving.md), [Vehicles](og/modules/Vehicles.md), [DRIVE3](og/modules/DRIVE3.md), [STEER_DMG](og/modules/STEER_DMG.md), [VBTN](og/modules/VBTN.md) (26 behaviours, 7 lines said or shown, 93 tuning numbers)
- [~] **Getting in and out, horn, lights**: In; the old game's door and exit routing not yet.
  - Old game: [Cars](og/modules/Cars.md), [CarExit](og/modules/CarExit.md), [World:cars](og/modules/World-cars.md) (139 behaviours, 55 lines said or shown, 104 tuning numbers)
- [x] **Traffic that stops at the zebra**
- [~] **Crashes and damage**: Sounds in; dents, cracked glass, tyres going, fire not yet.
  - Old game: [CarDmg](og/modules/CarDmg.md), [CarBump](og/modules/CarBump.md), [CARBUMP](og/modules/CARBUMP.md), [SkidMarks](og/modules/SkidMarks.md), [Glass](og/modules/Glass.md) (89 behaviours, 16 lines said or shown, 66 tuning numbers)
- [ ] **You visible in the driver's seat**
  - Old game: [Drivers](og/modules/Drivers.md) (8 behaviours, 0 lines said or shown, 5 tuning numbers)

## Sound and music

- [x] **Footsteps, engines, crashes, fight sounds, town ambience, dogs**
  - You asked: All sounds redone very well for the Unity version.
  - Old game: [Sfx](og/modules/Sfx.md), [SND](og/modules/SND.md) (58 behaviours, 2 lines said or shown, 25 tuning numbers)
- [ ] **Music**: Jukebox and the old game's free song library.

## Settings and controls

- [x] **Touch layout editor**
- [ ] **Keyboard and gamepad key rebinding**: The old game let you rebind driving keys.
  - Old game: [VKEYS_DEFAULT](og/modules/VKEYS_DEFAULT.md), [VKEY_NAMES](og/modules/VKEY_NAMES.md) (0 behaviours, 4 lines said or shown, 0 tuning numbers)
- [ ] **Settings screen**: Gore level, sound, view and controls help.
  - Old game: [Settings](og/modules/Settings.md), [UI](og/modules/UI.md) (61 behaviours, 13 lines said or shown, 13 tuning numbers)

## Also recorded

The old game's own graphics and engine code, which Unity does its own way: [Humans](og/modules/Humans.md), [HUMAN_DATA](og/modules/HUMAN_DATA.md), [Garments](og/modules/Garments.md), [Accessories](og/modules/Accessories.md), [Fabric](og/modules/Fabric.md), [FabTex](og/modules/FabTex.md), [Skin](og/modules/Skin.md), [Tex](og/modules/Tex.md), [Mk](og/modules/Mk.md), [Post](og/modules/Post.md), [SSAO](og/modules/SSAO.md), [Polish](og/modules/Polish.md), [Fx](og/modules/Fx.md), [CarLab](og/modules/CarLab.md), [MH](og/modules/MH.md), [PARENT](og/modules/PARENT.md), [Kit](og/modules/Kit.md), [GEN](og/modules/GEN.md), [BUILD](og/modules/BUILD.md), [HOUSE](og/modules/HOUSE.md), [TOP](og/modules/TOP.md), [STY](og/modules/STY.md), [SIL](og/modules/SIL.md), [Collars](og/modules/Collars.md), [Hd](og/modules/Hd.md), [IK](og/modules/IK.md), [MOUTH_LIM](og/modules/MOUTH_LIM.md), [HeadSnap](og/modules/HeadSnap.md), [GPAR](og/modules/GPAR.md), [COL](og/modules/COL.md), [Shop2Look](og/modules/Shop2Look.md), [ShadowProxy](og/modules/ShadowProxy.md), [PM](og/modules/PM.md), [FONTS](og/modules/FONTS.md), [TitleArt](og/modules/TitleArt.md), [Nav](og/modules/Nav.md), [Preview](og/modules/Preview.md), [Device](og/modules/Device.md), [UI2](og/modules/UI2.md), [UISafe](og/modules/UISafe.md), [Fr](og/modules/Fr.md), [Place](og/modules/Place.md), [SPOTS](og/modules/SPOTS.md), [CFG](og/modules/CFG.md), [SIZE](og/modules/SIZE.md), [SHOES](og/modules/SHOES.md), [SHOE](og/modules/SHOE.md), [TOPS](og/modules/TOPS.md), [STYLES](og/modules/STYLES.md), [L2W](og/modules/L2W.md), [TINT](og/modules/TINT.md), [LC](og/modules/LC.md), [LEG](og/modules/LEG.md), [LAND](og/modules/LAND.md), [MS](og/modules/MS.md), [KIND](og/modules/KIND.md), [POSE](og/modules/POSE.md), [GUARD](og/modules/GUARD.md), [TW](og/modules/TW.md), [DEFAULT](og/modules/DEFAULT.md), [REL](og/modules/REL.md), [AT](og/modules/AT.md), [PT](og/modules/PT.md), [TWO](og/modules/TWO.md), [Old](og/modules/Old.md), [OUT](og/modules/OUT.md), [OUT2](og/modules/OUT2.md), [OUT_OK](og/modules/OUT_OK.md), [POOLS](og/modules/POOLS.md), [IT](og/modules/IT.md), [PIT](og/modules/PIT.md), [UW](og/modules/UW.md), [PB](og/modules/PB.md), [EDGE](og/modules/EDGE.md), [SQ](og/modules/SQ.md), [FRAME](og/modules/FRAME.md), [INR](og/modules/INR.md), [FIN](og/modules/FIN.md), [Life](og/modules/Life.md), [S3K](og/modules/S3K.md), [AS](og/modules/AS.md), [EV](og/modules/EV.md), [VOW](og/modules/VOW.md), [BY](og/modules/BY.md), [VTOUCH_DEFAULT2](og/modules/VTOUCH_DEFAULT2.md), [IDX](og/modules/IDX.md), [CAP](og/modules/CAP.md), [CarAudit](og/modules/CarAudit.md), [CarTown](og/modules/CarTown.md), [Flights](og/modules/Flights.md)

## Real names in the old game's text (replace with made-up ones)

- **Astra**: Here he is! Kettle's on. Don't sit on the Astra. / blue Astra / grey Astra
- **BMW**: Off a BMW, once. Polished by the sort of man who says "they just fell off".
- **Cavalier**: Cavalier saloon / If you're after a car, I've got a Cavalier. Nearly. / That Cavalier in the yard? Burnt out at the pub in '09. Kept the radio. Kept the radio and nothing else. / white Cavalier
- **Fiesta**: A little ceramic bullet. Four for a Fiesta. One for a very small nerd.
- **Focus**: blue Focus / red Focus
- **Ford**: A fob, a Ford key and a leather tag. Somewhere in the car park somebody is about to have a worse night. / Trust Ford
- **Golf**: blue Golf / green Golf
- **Micra**: beige Micra
- **Range Rover**: I know where you park your Range Rover.
- **Skoda**: A bunch on a plastic A1 Taxis fob. One of them starts the Skoda. Another opens something expensive. / The car wash is shut. Radu is asleep in a Skoda.
- **Spotify**: Only Spotify and YouTube links are supported. / Only Spotify and YouTube web links work here. / Paste a Spotify or YouTube link first. / Spotify album
- **Tesco**: Tesco can wait, Tesco always waits.
- **TikTok**: Get it on TikTok.
- **Vauxhall**: Not yours. Off a Vauxhall. Stan does not ask.
- **YouTube**: Only Spotify and YouTube links are supported. / Only Spotify and YouTube web links work here. / Paste a Spotify or YouTube link first. / That YouTube link has no playlist or video in it.
