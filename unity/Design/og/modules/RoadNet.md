# RoadNet (old game, v95)

Part of: Roads, pavements, zebra crossing, traffic

Source in game.pretty.js: 169447-170171, 171855-171855, 171887-171887, 178627-178629

## Behaviours (23)

- roads (`roads`, line 169456)
- build (`build`, line 169488)
- usable (`usable`, line 169532)
- other (`other`, line 169535)
- rank (`rank`, line 169538)
- route (`route`, line 169541)
- lane pt (`lanePt`, line 169577)
- lane (`lane`, line 169586)
- bulge (`bulge`, line 169589)
- path of (`pathOf`, line 169612)
- set path (`setPath`, line 169692)
- obstacles (`obstacles`, line 169711, 178627)
- plan (`plan`, line 169732)
- where (`where`, line 169820)
- cur leg (`curLeg`, line 169847)
- extend (`extend`, line 169858)
- uturn (`uturn`, line 169870)
- gap (`gap`, line 169898)
- give way (`giveWay`, line 169978)
- squeeze (`squeeze`, line 169995)
- step (`step`, line 170046)
- in view (`inView`, line 170119)
- spawn spot (`spawnSpot`, line 170129)

## Everything it says or shows (2)

- Duckett Back Lane
- Wharf Lane

## Labels and names (1)

- roadnet extend

## States and modes

- kind: turn, end, open, j
- state: drive, gone, offstage
- type: car
- goal: slot, out

## Tuning numbers (16)

- high: 3
- ashby: 2
- mill: 2
- east: 2
- canal: 2
- south: 2
- guard: 0
- CLEAR: .9
- blockT: 0
- dir: 1
- gap: 99
- best: 99
- _sqT: 0
- _stk: 0
- uturns: 0
- leftWest: 0
