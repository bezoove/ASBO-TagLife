# Objs (old game, v95)

Part of: Pick up, carry, throw and smash things

Source in game.pretty.js: 103073-104269, 106970-106994, 107387-107387, 108652-108830, 108853-108861, 108863-108875, 108877-108880, 108882-108890, 108892-108894, 108896-108909, 108911-108920, 108922-108944, 108984-109006, 109008-109015, 109017-109019, 109743-109766, 110296-110371, 110374-110485, 110503-110607, 110620-110691, 110813-110851, 110853-110869, 110871-110873, 110875-110877, 110894-110911, 110913-110915, 110917-110919, 110921-110930, 110932-111097, 111114-111116, 113454-113458, 113460-113463, 113465-113470, 113472-113511, 113958-113975, 116162-116193, 116218-116225, 116227-116229, 116231-116242, 116244-116248 …

## Behaviours (67)

- gp (`_gp`, line 103089)
- mats (`_mats`, line 103107)
- parts (`parts`, line 103224, 108652, 113472, 116941, 120964, 140478)
- init (`init`, line 103315, 108984, 116981)
- build bar (`buildBar`, line 103371)
- reset (`reset`, line 103413, 109017, 111114, 113465, 117049, 120979)
- populate (`populate`, line 103440)
- add (`add`, line 103462, 116282, 116999, 120969)
- remove (`remove`, line 103509)
- sync (`sync`, line 103522)
- surface (`surface`, line 103530, 151000, 216557)
- collide (`collide`, line 103543, 150996)
- wake (`wake`, line 103588)
- seat taken (`seatTaken`, line 103591)
- tuck (`tuck`, line 103595)
- update (`update`, line 103598, 109008, 116218, 121030, 138873)
- extent (`extent`, line 103631)
- step (`step`, line 103639, 151024)
- choose rest (`chooseRest`, line 103684, 110871, 120993)
- settle (`settle`, line 103698)
- go home (`goHome`, line 103702, 138861)
- push by walkers (`pushByWalkers`, line 103713, 121004)
- chance (`chance`, line 103737)
- hazards (`hazards`, line 103740)
- tidy (`tidy`, line 103753, 108877, 116277)
- decal (`decal`, line 103759)
- impact (`impact`, line 103772, 108911, 110894, 117098, 140699)
- spill (`spill`, line 103781)
- shards fx (`shardsFx`, line 103797)
- smash (`smash`, line 103820, 110913, 116162)
- heard (`heard`, line 103868, 110921, 138275)
- strike (`strike`, line 103895, 110917)
- hit body (`hitBody`, line 103926, 108922, 110932, 137253, 140707)
- basic hit (`basicHit`, line 103954)
- hand side (`handSide`, line 103970)
- nearby (`nearby`, line 103973, 108863, 113460, 116255, 117058)
- candidate (`candidate`, line 103994, 116244, 175645)
- pickup (`pickup`, line 104019, 110875, 116250, 175672, 201043)
- pocket (`pocket`, line 104041, 108853, 116231)
- owner of (`ownerOf`, line 104060)
- place held (`placeHeld`, line 104071, 106970, 109743, 113958, 121016)
- drop (`drop`, line 104096, 108882, 113454, 116227, 121050)
- put down (`putDown`, line 104112)
- release (`release`, line 104115)
- can throw (`canThrow`, line 104121, 117778, 121040)
- input (`input`, line 104125, 117783)
- aim yaw (`aimYaw`, line 104136, 110318)
- from hand (`fromHand`, line 104139, 108896)
- throw now (`throwNow`, line 104177)
- launch (`launch`, line 104204, 108892, 110853, 117570, 117729, 117877)
- update bar (`updateBar`, line 104243, 121044, 146767)
- can throw peek (`canThrowPeek`, line 104256)
- touch state (`touchState`, line 104259)
- throw speed el (`throwSpeedEl`, line 110307)
- solve (`solve`, line 110320)
- predict (`predict`, line 110374)
- arc make (`arcMake`, line 110503)
- arc show (`arcShow`, line 110605)
- draw arc (`drawArc`, line 110620)
- witnesses (`witnesses`, line 110813, 145460, 146570, 154821)
- anon noise (`anonNoise`, line 110946, 138285)
- seen noise (`seenNoise`, line 111073, 138300)
- release point (`releasePoint`, line 117551, 117710, 117867)
- begin aim (`beginAim`, line 117602, 117644)
- release aim (`releaseAim`, line 117609, 117720)
- cancel aim (`cancelAim`, line 117725)
- cancel ui (`cancelUi`, line 137514)

## Everything it says or shows (33)

- bold 26px Georgia, serif
- 600 14px/1.25 "Barlow Semi Condensed", Arial, sans-serif
- You skid on spilt beer.
- Brenda: "OUT. That's the lot. You're barred."
- Brenda: "That's the second.
- ) + ` One more and you're out.
- What the—?
- Brenda: "That is it. OUT."
- Put down
- Pocket the …
- Pick up the …
- Pocket it
- Pick up
- Put your drink down first. Finish it, or throw it.
- Your hands are full of glasses.
- … in the wallet, and a library card.
- Holding the …
- Empty. You bin it.
- Picked up: ….
- Egg. On your face.
- Was that you, …?
- …! Did you chuck that?
- Oi, …. Was that you?
- I bet that was ….
- Who did that?!
- Right. Who threw that?
- Oi! Who's chucking things about?!
- Brenda, … did that.
- Carrying the …
- Brenda: "Is that PAINT? On MY pub? Do that again and you're barred."
- Brenda: "Put that can away or you're out on your ear."
- Brenda: "I warned you. OUT. And you can come back tomorrow with a scrubbing brush."
- Callum collects the glasses.

## Labels and names (7)

- … (hands full) · glass hit · ladder attach · breaking glass · something thrown · throwing things · ladder prompts

## States and modes

- type: circle
- mode: explore, sit, down, talk, cutscene
- kind: shards, puddle, ball, cue, neck, chairleg, torch, none, wall, ceil, person, floor, table, leaving, toilet, puzzled, sparkler, chair, obj, brick
- state: gone, offstage, walk, match

## Tuning numbers (58)

- charge: 0
- breaks: 0, 1
- cool: 0, .25, .3, .9
- _slipT: 0, 6
- MAX: 200
- envMapIntensity: 1.6
- seg: 12, 14
- count: 0
- polygonOffsetFactor: -3
- polygonOffsetUnits: -3
- zIndex: 40
- _id: 1
- idle: 0
- age: 0, 100
- best: 0
- rest: 0
- tgtY: 0
- _tid: 3
- renderOrder: 2, 11, 10
- step: 0
- life: .5, .6
- spread: .5, .7, .6
- grav: -4, -9, -5
- drag: 1.5, .6, 1.4
- kind: 2
- fin: .02
- fout: .4
- floor: .015
- dur: .7, .8, 1, .9, .75, .62
- lookW: 1
- temper: .4
- wary: 240, 180
- score: .9
- cash: 0
- drinkLeft: 0
- fill: 0
- speed: 3.2, 3
- bestA: .42, 9
- intensity: 0
- _wb: 0
- yaw: 0
- face: 0
- mdx: 0
- mdy: 0
- bestL: .95
- maxT: 2.6, 3
- color: 16765802, 16769162, 1707781, 16774872, 16730682, 16751164
- pulse: .82
- lit: .88
- peri: 30, 0
- lookT: 2.5, 3
- awHold: 0
- _barkT: 0
- lineWidth: 8
- _ovW: 0
- heat: 0
- minT: 1.3
- cap: 3.6
