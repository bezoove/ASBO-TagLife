# TownTraffic (old game, v95)

Part of: Roads, pavements, zebra crossing, traffic

Source in game.pretty.js: 165454-165802, 170175-170177, 170223-170226, 170228-170238, 172038-172040, 194989-194992, 196865-196868, 207776-207790, 215577-215580

## Behaviours (13)

- active (`active`, line 165474, 170175)
- build one (`buildOne`, line 165477)
- driver one (`driverOne`, line 165496)
- cars (`cars`, line 165513, 172038, 196865, 215577)
- lane pt (`lanePt`, line 165516)
- bulge (`bulge`, line 165524)
- path of (`pathOf`, line 165547)
- set path (`setPath`, line 165622)
- plan (`plan`, line 165640, 170175)
- spawn spot (`spawnSpot`, line 165693, 170175)
- spawn (`spawn`, line 165716, 170223, 194989)
- where (`where`, line 165746, 170175)
- update (`update`, line 165772, 170228, 207776)

## States and modes

- mode: title
- state: parked, drive

## Tuning numbers (13)

- buildT: 3, 2.5, 30, 1e9
- spawnT: 4
- max: 4
- maxMoving: 3
- spawned: 0
- parked: 0
- despawned: 0
- cap: 1
- tax: 1
- seed: 12e3
- blockT: 0
- dir: 1
- steer: 0
