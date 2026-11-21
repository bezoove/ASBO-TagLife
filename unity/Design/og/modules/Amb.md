# Amb (old game, v95)

Part of: Ambulance and paramedics

Source in game.pretty.js: 214631-215533, 215535-215535, 215613-215613

## Behaviours (39)

- siren (`siren`, line 214692)
- siren on (`sirenOn`, line 214714)
- build model (`buildModel`, line 214738)
- free model (`freeModel`, line 214765)
- ensure model (`ensureModel`, line 214769)
- pseudo (`pseudo`, line 214776)
- make crew (`makeCrew`, line 214797)
- crew ready (`crewReady`, line 214824)
- take crew (`takeCrew`, line 214837)
- hide crew (`hideCrew`, line 214843)
- place at (`placeAt`, line 214847)
- go to (`goTo`, line 214852)
- move crew (`moveCrew`, line 214869)
- say (`say`, line 214902)
- upd crew (`updCrew`, line 214913)
- valid (`valid`, line 214923)
- pending (`pending`, line 214928)
- new unit (`newUnit`, line 214933)
- van pt (`vanPt`, line 214964)
- target (`target`, line 214972)
- dispatch (`dispatch`, line 214977)
- replan (`replan`, line 215037)
- unseat (`unseat`, line 215055)
- seat (`seat`, line 215072)
- arrive (`arrive`, line 215081)
- next job (`nextJob`, line 215105)
- trolley follow (`trolleyFollow`, line 215118)
- lying pose (`lyingPose`, line 215138)
- place patient (`placePatient`, line 215152)
- scene tick (`sceneTick`, line 215164)
- to van (`toVan`, line 215257)
- boarding (`boarding`, line 215267)
- deliver (`deliver`, line 215297)
- leave (`leave`, line 215311)
- finish (`finish`, line 215331)
- update (`update`, line 215368)
- unit tick (`unitTick`, line 215414)
- reset (`reset`, line 215499)
- prewarm (`prewarm`, line 215506)

## Everything it says or shows (16)

- Ambulance! Let us through, please.
- Right, let's have a look at you.
- Mind your backs, coming through.
- Can you hear me? Squeeze my hand.
- Stay with me, mate. Stay with me.
- Pressure on that. Harder.
- Pulse is weak. Get a line in.
- You're alright, we've got you.
- No output. Nothing we can do.
- I'm calling it. Get the sheet.
- He's gone. Police are on their way.
- Mind the head.
- Give us some room, please.
- Stand back, please.
-  stops short (no kerb route)
- no way to send it

## Labels and names (20)

- van built  · amb crew ·  called for  · amb plan ·  arrived after  ·  patient gone ·  leaves empty ·  to the hospital with 
- amb deliver · resolved out of sight  · amb unit ·  time limit in  · time limit ·  van lost · van lost · no patient
- amb dispatch ·  blocked: stops short · amb prewarm · amb prewarm crew

## States and modes

- mode: stand, title, explore
- state: done, wait, assigned, drive, parked, tending, gone, loaded
- goal: out, slot

## Tuning numbers (58)

- calls: 0
- dispatched: 0
- arrivals: 0
- treated: 0
- covered: 0
- loaded: 0
- delivered: 0
- abstract: 0
- standDowns: 0
- stopShort: 0
- hurried: 0
- builds: 0
- maxUnits: 0
- routeLen: 0
- arriveT: 0
- nanCrew: 0
- CRUISE: 12.5, 9
- CAP: 2
- CLUSTER: 22
- MAXT: 300
- WALK: 1.35
- PUSH: 1.15
- dur: 4, 2
- tri: 4
- vol: .07
- rack: 0
- ladder: 0
- tow: 0
- alloy: 1
- sticker: 0
- clutter: 0
- tax: 1
- lit: 0
- cap: 1
- wantModel: 0
- temper: .2
- honesty: 1
- warmth: .8
- spd: 0
- sayT: 0
- _stk: 0
- speed: 0
- _unw: 0
- uid: 0
- sirenLvl: 0, .05
- replanT: 0, 2
- hospT: 0
- seed: 5150
- steer: 0
- jobI: 0
- actT: 0, 2.1
- lookW: .9
- bleed: 0
- cruise: 9
- stuckT: 0
- yaw: 0
- plBleed: 0
- plSevereT: -1e9
