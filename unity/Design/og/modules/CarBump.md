# CarBump (old game, v95)

Part of: Crashes and damage

Source in game.pretty.js: 177736-178482, 178607-178607, 178611-178615, 178641-178641, 178643-178643

## Behaviours (30)

- gk (`gk`, line 177751)
- add parked (`addParked`, line 177754)
- parked near (`parkedNear`, line 177762)
- mass of (`massOf`, line 177776)
- is traffic (`isTraffic`, line 177779)
- bodies (`bodies`, line 177782)
- body (`body`, line 177801)
- kn (`kn`, line 177853)
- speed (`speed`, line 177864)
- axes (`axes`, line 177867)
- sat (`sat`, line 177871)
- corners (`corners`, line 177889)
- inside (`inside`, line 177899)
- contact (`contact`, line 177903)
- pair (`pair`, line 177913)
- move (`move`, line 178012)
- kick (`kick`, line 178027)
- model (`model`, line 178080)
- wob (`wob`, line 178086)
- step wob (`stepWob`, line 178107)
- effects (`effects`, line 178124)
- wake (`wake`, line 178214)
- slide step (`slideStep`, line 178263)
- world push (`worldPush`, line 178270)
- free step (`freeStep`, line 178294)
- re traffic (`reTraffic`, line 178397)
- place model (`placeModel`, line 178409)
- step (`step`, line 178432)
- ext step (`extStep`, line 178469)
- road parked (`roadParked`, line 178611)

## Everything it says or shows (3)

- Metal on metal.
- That is going to need a panel.
- Somebody's insurance just went up.

## Labels and names (5)

- bike crash · carbump damage · parked car · carbump wake · carbump pair

## States and modes

- kind: van, traffic, engine, parked, player, free, bike, pbike
- type: bike, car
- state: drive

## Tuning numbers (20)

- CELL: 10
- pairs: 0
- hits: 0
- shunted: 0
- knocked: 0
- bikes: 0
- maxPen: 0
- hold: 0, 1e9
- pad: .02
- ima: 1, 0
- iIa: 1, 0
- imb: 1, 0
- iIb: 1, 0
- freq: 900, 3200, 1500
- vol: .3, .18, .22
- slide: -20
- lock: 2
- steer: 0
- open: 0
- top: 1.5
