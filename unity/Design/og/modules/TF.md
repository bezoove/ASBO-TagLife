# TF (old game, v95)

Part of: Residents with daily routines

Source in game.pretty.js: 178815-178840, 178874-178906, 178908-178941, 178946-178968, 178978-179216, 179223-179223, 179254-179254, 179262-179262, 188105-188111, 192256-192256, 192258-192260, 208297-208324, 213529-213531, 213533-213544

## Behaviours (18)

- dests (`dests`, line 178874)
- cell free (`cellFree`, line 178908)
- snap (`snap`, line 178915, 208297)
- pt of (`ptOf`, line 178939)
- in view (`inView`, line 178946)
- make (`make`, line 178978, 188105)
- attach (`attach`, line 179024)
- drop (`drop`, line 179029, 213529)
- kind now (`kindNow`, line 179034)
- pick dest (`pickDest`, line 179038)
- spawn point (`spawnPoint`, line 179057)
- crowded (`crowded`, line 179087)
- send (`send`, line 179093)
- arrive (`arrive`, line 179107)
- think (`think`, line 179126)
- other amb (`otherAmb`, line 179162)
- target (`target`, line 179166)
- tick (`tick`, line 179172, 213533)

## Everything it says or shows (1)

- The Eggy Duck

## Labels and names (1)

- tf think

## States and modes

- state: idle, walk
- mode: stand, enter, walk, linger, title
- kind: punter, stroller
- type: pub, takeaway

## Tuning numbers (40)

- POOL: 18
- MAX: 14
- CAP: 40
- DROP: 100
- MAKE_GAP: 1.6
- made: 0
- spawned: 0
- dropped: 0
- entered: 0
- noPath: 0
- stuck: 0
- makeMs: 0
- findMs: 0
- _mk: 0
- _sp: 0, .6
- radius: 1.2
- run: 0
- height: 1.7
- build: .5
- arrive: 0
- leave: 9999
- sigma: 60
- greed: .3
- pride: .3
- caution: .3
- warmth: .3
- temper: .1
- thirst: .4
- honesty: .4
- arriveAt: 0
- leaveAt: 1e9
- speed: 0
- tot: 0
- stuckT: 0, 9
- walkT: 0
- dur: 2
- seed: 9100
- _te: 1
- _ke: 1
- _gRef: 1.5
