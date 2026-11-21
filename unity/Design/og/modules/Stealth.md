# Stealth (old game, v95)

Part of: Stealth: sneaking, being noticed, light and noise

Source in game.pretty.js: 9953-9953, 28924-28924, 40286-40286, 62249-62249, 93193-93193, 93207-93207, 93759-93759, 93970-93970, 94065-94065, 94145-94145, 94172-94172, 95629-96534, 101797-101797, 102125-102125, 102733-102733, 103748-103748, 103779-103779, 103866-103866, 104241-104241, 104717-104717, 104880-104880, 104914-104914, 104996-104996, 107108-107108, 107903-107903, 107943-107943, 108075-108075, 108164-108164, 108981-108981, 109027-109030, 109032-109035, 109037-109044, 109528-109534, 109536-109543, 110890-110890, 111100-111111, 111547-111547, 111646-111646, 112645-112645, 115050-115053 …

## Behaviours (46)

- init (`init`, line 95653)
- stamp (`stamp`, line 95660)
- recalc (`recalc`, line 95690)
- top at (`topAt`, line 95706)
- h idx (`hIdx`, line 95711)
- refresh blockers (`refreshBlockers`, line 95720)
- blocked (`blocked`, line 95735)
- ray len (`rayLen`, line 95750)
- build lamps (`buildLamps`, line 95763, 136960, 148778, 151183, 160198)
- light at (`lightAt`, line 95791)
- s (`S`, line 95819)
- level (`level`, line 95822)
- add xp (`addXp`, line 95828)
- add heat (`addHeat`, line 95835)
- is sneaking (`isSneaking`, line 95839, 137030, 152849)
- toggle sneak (`toggleSneak`, line 95842)
- update player (`updatePlayer`, line 95846, 137034, 152853)
- update (`update`, line 95867, 137020)
- vig (`vig`, line 95884, 109027, 115050, 131488, 139909, 149460)
- exposure (`exposure`, line 95901, 109032, 143426, 151042, 151189)
- update npc (`updateNpc`, line 95915, 109037, 111100, 115055, 137038, 145352)
- on rise (`onRise`, line 95974)
- on glance (`onGlance`, line 95984)
- bark (`bark`, line 95991, 145348, 154881)
- alert (`alert`, line 95997, 137047)
- roll (`roll`, line 96008, 109528)
- row cash (`rowCash`, line 96135)
- row value (`rowValue`, line 96138)
- can lift (`canLift`, line 96141, 115063)
- open lift (`openLift`, line 96183)
- lift shot (`liftShot`, line 96205)
- leave (`leave`, line 96247)
- lift player (`liftPlayer`, line 96254)
- lift keys (`liftKeys`, line 96272)
- lift update (`liftUpdate`, line 96283, 109536)
- grab (`grab`, line 96304, 138192)
- discover (`discover`, line 96335, 138212)
- accuse (`accuse`, line 96355)
- caught (`caught`, line 96362, 138196)
- confront opts (`confrontOpts`, line 96376)
- punish (`punish`, line 96425)
- init viz (`initViz`, line 96453)
- viz update (`vizUpdate`, line 96486, 146597)
- aware f (`awareF`, line 146528)
- seen fraction (`seenFraction`, line 146531)
- witness keep (`witnessKeep`, line 146546, 154806)

## Everything it says or shows (52)

- Tap Sneak, creep up behind someone, then tap Pick pockets.
- Press C to sneak. Creep up behind someone and press R to pick their pockets.
- Inside jacket pocket
- Back pocket
- Front trouser pocket
- Loose change
- Jacket pocket
- Front pocket
- Mobile phone
- Left wrist
- Gold wristwatch
- Cardigan pocket
- Pocket watch
- Shirt pocket
- Twenty cigarettes
- Hip pocket
- Brass lighter
- Roll of notes
- Shirt cuffs
- Silver cufflinks
- Right hand
- Signet ring
- Inside pocket
- Hip flask
- Coat pocket
- … has nothing left worth taking.
- Out of reach.
- Lifted from …
- … realises they've been robbed. Eyes narrow around the room.
- … calls out. … spins round.
- Hand it all back
- Returned to …
- Just steadying myself, mate
- Slip them … to forget it
- Hush money, …
- … pockets it. "I never saw you."
- "Is that supposed to make it better?"
- Shove them and leg it
- Brenda saw that. Her trust in you takes a hit.
- … takes … out of you, plus a bruise. The back room is shut to you.
- … gives you a hard shove${fine>0?
- You skid in a puddle of wee. Lovely.
- Too risky for you yet. That needs a steadier hand: Stealth rank ….
- The rocket goes sideways.
- That's not up. That is NOT up.
- Over the handlebars. Ow.
- You and the bike part company.
- The brakes go on and you go over the front.
- It hits something. You keep going.
- It swings round and you're off.
- You hit the road running... and then just hit the road.
- A pit bull comes for you, barking its head off.

## Labels and names (6)

- … is busy. · … is watching you. · … is cleaned out. · an empty wallet · for the trouble · a missing wallet

## States and modes

- type: obb, circle
- mode: lift, explore, sit, slump, stand, down, wee, lockpick, bed
- state: gone, match, offstage, walk
- kind: walk, cash, safe, lock, shards, puddle, brick, fence, hedge

## Tuning numbers (77)

- speed: 0
- doorsT: 0, .3
- vis: .8
- light: .3
- noise: .5
- noiseEff: .5
- seenBy: 0
- _xpT: 0
- _near: 0
- hall: .2
- corridor: .2
- back: .24
- gents: .45
- ladies: .45
- garden: .14
- games: .22
- snug: .12
- cellar: .1
- stash: .08
- yard: .12
- crowd: 0
- seen: 0
- near: 0
- stealthHint: 1
- half: 54, 62, 60, 50, 64, 70
- range: 9, 11, 10, 8.5, 12
- mod: 1, 1.35, 1.15, 1.1, .02, 1.4
- awS: 0, 2
- awHold: 0, 1.2, 1.4, 3, 1.5
- wary: 0, 45, 60, 120, 150, 200
- _expo: 0
- discoverAt: 1
- _losT: .11
- peri: 28
- gain: 0
- seenT: 0
- lookW: .9, .6, 1, .7
- lookT: 1, 1.2, .4
- tele: .42
- dur: .95, .9, 2.2
- vol: .035, .06, .05
- day: 0
- diff: .4, .42, .18, .56, .8, .6
- risk: 5, 6.5, 3, 8, 11, 10
- time: 1.6, 2, .9, 1.9, 2.8, 2.2
- duke: 300
- kev: 110
- ron: 90
- dave: 75
- mickey: 140
- brenda: 45
- frac: .42
- cap: 380
- base: 0, .12
- sel: 0
- emptyT: 0
- liftHint: 1
- score: 0
- freeze: 0
- lift: 0
- value: 0
- amt: 0
- cash: 0
- renderOrder: 4
- idleT: 0
- camT: 0
- flowK: 0
- total: 0
- _wit: 0
- _slipT: 6
- age: 0
- rest: 0
- stanceT: 6
- _lockedPing: 1.2
- glT: 5
- Tarmac: 1
- best: 0
