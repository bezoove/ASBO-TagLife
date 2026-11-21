# Gore (old game, v95)

Part of: Gore, injuries and blood

Source in game.pretty.js: 212306-213464, 213477-213477, 213645-213645, 213690-213690, 213712-213715, 213719-213719, 213728-213728, 215613-215613, 215625-215625

## Behaviours (39)

- minor (`minor`, line 212353)
- why (`why`, line 212360)
- atlas (`atlas`, line 212417)
- decals init (`decalsInit`, line 212453)
- decal (`decal`, line 212485)
- write decal (`writeDecal`, line 212500)
- decals tick (`decalsTick`, line 212511)
- decals clear (`decalsClear`, line 212527)
- drops init (`dropsInit`, line 212545)
- drop (`drop`, line 212567)
- drops tick (`dropsTick`, line 212574)
- spray (`spray`, line 212595)
- stain (`stain`, line 212655)
- stain grow (`stainGrow`, line 212665)
- smear res (`smearRes`, line 212672)
- car smear (`carSmear`, line 212702)
- weapon smear (`weaponSmear`, line 212729)
- limb res (`limbRes`, line 212754)
- limb mat (`limbMat`, line 212784)
- sever (`sever`, line 212814)
- drop limb (`dropLimb`, line 212867)
- limbs tick (`limbsTick`, line 212876)
- bruise (`bruise`, line 212902)
- weapon kind (`weaponKind`, line 212915)
- hurt (`hurt`, line 212925)
- down (`down`, line 212994)
- leave fight (`leaveFight`, line 213005)
- kill (`kill`, line 213019)
- detach (`detach`, line 213064)
- law (`law`, line 213125)
- scream (`scream`, line 213147)
- panic (`panic`, line 213184)
- casualty (`casualty`, line 213246)
- tick (`tick`, line 213268)
- throe (`throe`, line 213325)
- remove corpse (`removeCorpse`, line 213337)
- retire (`retire`, line 213355)
- sync amb (`syncAmb`, line 213381)
- reset (`reset`, line 213415)

## Everything it says or shows (16)

- gore leaveFight
- killing somebody with a car
- a death on the street
- Oh my God! OH MY GOD!
- Somebody call an ambulance!
- Is he breathing?! He's not breathing!
- Don't look. Don't look at it.
- Call an ambulance!
- Somebody help him!
- Jesus, are you alright?!
- Don't move him! Don't move him!
- Stay back! Everybody stay back!
- Control, I need an ambulance, urgent.
- Nobody touch anything.
- I can't feel my legs...
- Get an ambulance... please...

## Labels and names (7)

- gore fabric · gore smear · gore tf · gore amb · shooting somebody dead · killing somebody · gore law

## States and modes

- kind: resident
- role: dealer
- mode: sit, slump, stand, down, talk, fall, title
- state: dead, gone, done, wait

## Tuning numbers (53)

- seq: 0
- hurt: 0
- deaths: 0
- protectedSaved: 0
- dismember: 0
- downs: 0
- decals: 0
- drops: 0
- carHits: 0
- shots: 0
- beats: 0
- falls: 0
- spurts: 0
- maxDecals: 0
- maxDrops: 0
- gore: 2
- bleed: 0
- artT: 0
- hits: 0
- stains: 0
- pulse: 0, .75
- dripT: 0, .35
- poolT: 0
- last: 0
- cap: 64, 220
- polygonOffsetFactor: -4, -3
- polygonOffsetUnits: -4, -3
- count: 0
- renderOrder: 2, 21, 3
- age: 0
- live: 0
- value: 800
- spotBudget: 0
- life: 420
- str: 1.2, 1
- artP: 0, .12
- koT: 1e9
- throes: 3
- throeT: .6, .35
- vol: .05, .035, .04
- slide: 180
- freq: 1800
- ref: 3
- screamed: 0
- ran: 0
- lookW: 1
- dur: 1
- still: 0
- freeze: 0
- _plD: .5
- yaw: 0
- plBleed: 0
- plSevereT: -1e9
