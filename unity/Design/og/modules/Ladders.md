# Ladders (old game, v95)

Part of: Climbing and ladders

Source in game.pretty.js: 119759-120961, 120983-120983, 121107-121107, 175519-175550, 175577-175643, 175692-175712, 175985-176031

## Behaviours (56)

- is (`is`, line 119767)
- held (`held`, line 119770)
- all (`all`, line 119774)
- kd (`kd`, line 119777)
- mats (`mats`, line 119780)
- group (`group`, line 119831)
- parts (`parts`, line 119877)
- mesh of (`meshOf`, line 119881)
- attach (`attach`, line 119889)
- pose (`pose`, line 119910)
- set frame (`setFrame`, line 119917)
- line (`line`, line 119922)
- at (`at`, line 119946)
- solid at (`solidAt`, line 119949)
- face normal (`faceNormal`, line 119965)
- find lean (`findLean`, line 119990)
- find stand (`findStand`, line 120128)
- pin (`pin`, line 120169)
- lean (`lean`, line 120172)
- aframe (`aframe`, line 120181)
- unstand (`unstand`, line 120225)
- free (`free`, line 120232)
- lay flat (`layFlat`, line 120236)
- take down (`takeDown`, line 120242)
- place (`place`, line 120254)
- react (`react`, line 120269)
- alarm (`alarm`, line 120285)
- changed (`changed`, line 120304)
- clank (`clank`, line 120307)
- clatter (`clatter`, line 120322)
- knock (`knock`, line 120337)
- topple step (`toppleStep`, line 120373)
- landed (`landed`, line 120412)
- length (`length`, line 120444)
- seg dist (`segDist`, line 120447)
- carry (`carry`, line 120454)
- carry ik (`carryIK`, line 120502)
- cyc (`cyc`, line 120528)
- enter (`enter`, line 120534)
- climb pose (`climbPose`, line 120551)
- climb update (`climbUpdate`, line 120636)
- step off (`stepOff`, line 120660)
- get off top (`getOffTop`, line 120668)
- get on top (`getOnTop`, line 120698)
- fall (`fall`, line 120737)
- let go (`letGo`, line 120769)
- update (`update`, line 120779, 175692)
- cars (`cars`, line 120817)
- save (`save`, line 120858)
- spawn one (`spawnOne`, line 120933)
- spawn all (`spawnAll`, line 120947, 175985)
- sync inv (`syncInv`, line 175519)
- give new (`giveNew`, line 175525)
- cands (`cands`, line 175577)
- take loot (`takeLoot`, line 175994)
- loot tick (`lootTick`, line 176004)

## Everything it says or shows (64)

- Not while you are off your feet.
- Find some level ground first.
- Square up to the wall.
- Nothing up there to lean it on.
- No footing for it there.
- Something is in the way of its feet.
- Nothing in front to lean it against.
- No room for it there.
- It needs level ground.
- Not enough room to open it out.
- The ceiling is too low.
- Your hands are full.
- Put your drink down first.
- Your hands are full of glasses.
- It's up, but the edge is out of reach from the top of it.
- Up against the wall. Nothing to get off onto at the top.
- putting a ladder up against a house
- Oi! What are you doing with that ladder?
- That's not your house!
- I'm calling the police!
- Get down from there!
- Watch it!
- You nearly had me there!
- Bloody hell!
- Ow! Watch the ladder!
- Mind where you swing that thing!
- Oi! Eyes in the back of your head, have you?
- You'll have someone's eye out!
- climbing a ladder up a house
- The ladder sways. So do you.
- The edge is just out of reach. You'd want a longer ladder.
- Nothing up here but wall.
- The ladder goes, and so do you.
- Down you come.
- A five-metre ladder? It won't go in there.
- You shoulder the ….
- The … is at your feet. Your hands are full.
- Step up onto the tread
- Step up
- Stand the step ladder up
- Put the ladder up
- Put it up
- Pick up the …
- Pick up
- Put your drink down first. Finish it, or throw it.
- Climb the step ladder
- Climb the …
-  : `Take the … down`, step ? 
- Kick the step ladder over
- Kick the ladder away
- Kick it
- Climb down the step ladder
- Climb down
- Climb down the …
- Push the ladder away
- Push it
- Well. That was the way down.
- Come down here!
- Let's see you fly!
- Get down!
- From up here: the tower, the canal like a strip of tin, the church spire, the Eggy Duck's sign lit up. Somebody's having a barbecue.
- The whole of Ashby laid out. The trains, the gasholder frame, the park. You can see your house. Well, nearly.
- A football! Soft, but it still bounces. "PROPERTY OF DARREN".
- Found on a roof

## Labels and names (3)

- too high · ladder alarm · … (hands full)

## States and modes

- kind: stepladder, extladder, ladder, pigeons, mast, ball, stash
- type: circle
- state: gone, offstage

## Tuning numbers (37)

- STEP_T: .21
- seed: 41
- rings: 9
- contrast: .18
- seg: 8, 6
- ext: 0
- open: 0, 1
- lad: 1
- nlx: 0
- nlz: 0
- prev: .25
- extClick: 0
- sMax: 1.2
- pad: .05
- dur: .9, 1
- lookW: 1
- vol: .09, .12, .02, .18, .1, .15
- slide: -120, -150
- freq: 2400, 1500, 900, 3e3, 1200
- life: 1
- spread: .25
- cool: .45
- airW: 0
- phiMax: -1
- hold: 0
- said: 0, 1
- reach: 0
- speed: 0
- rate: 2.3
- tuck: 0
- lift: 0
- imp: 0
- _saveT: 3
- held: 1
- _kT: .3
- flyT: 0
- rl_mast1: 1
