# Traffic (old game, v95)

Part of: Roads, pavements, zebra crossing, traffic

Source in game.pretty.js: 154210-154764, 159034-159034, 159110-159110, 159112-159112, 159167-159167, 159181-159181, 159199-159215, 159217-159223, 159690-159699, 159701-159712, 165079-165099, 165806-165806, 165814-165814, 165819-165821, 165823-165848, 165850-165854, 165856-165865, 165867-165873, 165875-165877, 165879-165891, 165893-165913, 170177-170181, 170183-170185, 170187-170208, 170210-170221, 170305-170311, 172042-172049, 172051-172053, 172055-172062, 178573-178578, 178580-178584, 178586-178588, 178633-178635, 178639-178646, 179564-179591, 179593-179604, 186738-186741, 186750-186750, 186789-186790, 196839-196846 …

## Behaviours (25)

- build one (`buildOne`, line 154224, 170179)
- driver one (`driverOne`, line 154242, 159199)
- obstacles (`obstacles`, line 154258, 178633)
- lane z (`laneZ`, line 154274)
- lane pts (`lanePts`, line 154291)
- blend (`blend`, line 154299)
- turn pts (`turnPts`, line 154307)
- plan (`plan`, line 154315, 165850, 170210)
- at (`at`, line 154363)
- free slot (`freeSlot`, line 154376, 178639)
- spawn (`spawn`, line 154381, 165819, 170177)
- despawn (`despawn`, line 154412, 165079, 165879, 172055, 178586, 196857)
- clear (`clear`, line 154415)
- update (`update`, line 154419, 207794)
- gap ahead (`gapAhead`, line 154445, 159690, 165893)
- step (`step`, line 154538, 159217, 159701, 165823, 170183, 172042)
- place car (`placeCar`, line 154557, 170305, 178573)
- door point (`doorPoint`, line 154593)
- dest of (`destOf`, line 154601, 165856, 170187)
- park up (`parkUp`, line 154620, 165875, 172051, 196848, 215565)
- parked step (`parkedStep`, line 154626, 165867, 179564)
- walk (`walk`, line 154675, 179593)
- honk (`honk`, line 154697)
- chars (`chars`, line 154710)
- hum (`hum`, line 154725)

## Labels and names (4)

- traffic driver · traffic model reset · van despawned by  · amb arrive

## States and modes

- state: parked, drive, gone, offstage, idle, jacked
- goal: out, slot
- mode: title, sit, stand
- type: car, lowpass, sawtooth

## Tuning numbers (32)

- spawnT: 8
- buildT: 2, 2.5, 30, 1e9
- spawned: 0
- parked: 0
- left: 0
- honks: 0
- cap: 1
- tax: 1
- seed: 9e3
- dir: -1
- blockT: 0
- steer: 0
- gap: 99
- top: 1.5
- speed: 0
- walkT: 1.2, 1
- vol: .05, .1, .04
- slide: -20
- freq: 300
- sweepTo: 500
- stuckT: 0
- value: 0, 400, 45, 600, .6
- arriveAt: 0
- drunk: 0
- _surfT: 0
- _surfT2: 0
- _surfWob: 0
- max: 4
- maxMoving: 3
- _offT: 2
- _rp: 0
- cruise: 0
