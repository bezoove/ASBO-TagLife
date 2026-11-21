# Brk (old game, v95)

Part of: Pick up, carry, throw and smash things

Source in game.pretty.js: 170313-170716, 170820-170820, 171037-171037, 171051-171170, 171172-171321, 171325-171337, 200755-200766, 207143-207165

## Behaviours (18)

- gk (`gk`, line 170327)
- cell of (`cellOf`, line 170330)
- add (`add`, line 170333)
- near (`near`, line 170353)
- tagged (`tagged`, line 170365)
- post of (`postOf`, line 170377)
- add post (`addPost`, line 170394)
- posts near (`postsNear`, line 170399)
- index (`index`, line 170411)
- cut out (`cutOut`, line 170423, 207143)
- knock (`knock`, line 170476, 171172)
- splinters (`splinters`, line 170565)
- update (`update`, line 170588)
- land (`land`, line 170610, 200755)
- car check (`carCheck`, line 170646, 171102)
- split hedges (`splitHedges`, line 171051)
- bits (`bits`, line 171303)
- update2 (`update2`, line 171325)

## Labels and names (4)

- brk cut · knocking down a  · lamp post · bus shelter

## States and modes

- type: BoxGeometry, CylinderGeometry, obb
- kind: fence, bin, lamp, sign, hedge, junk, wall, bench, shelter

## Tuning numbers (13)

- CELL: 8
- knocked: 0
- bumped: 0
- vol: .06, .12, .16, .1, .07, .04
- slide: -60, -30, -80, -40
- freq: 2400, 900, 600, 3200, 700, 1400
- nSplit: 0
- spin: 0
- lift: 0, .35
- life: 1.2
- spread: .6
- grav: 1.5
- end: 1.25, .22
