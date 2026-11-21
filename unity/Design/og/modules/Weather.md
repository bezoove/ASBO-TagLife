# Weather (old game, v95)

Part of: Day, night, weather and the clock

Source in game.pretty.js: 202726-203575

## Behaviours (12)

- pick next (`pickNext`, line 202934)
- extend (`extend`, line 202947)
- block at (`blockAt`, line 202971)
- grey col (`greyCol`, line 203110)
- look (`look`, line 203127)
- build rain (`buildRain`, line 203174)
- wet compile (`wetCompile`, line 203261)
- make wet (`makeWet`, line 203286)
- apply damp (`applyDamp`, line 203326)
- bake (`bake`, line 203338)
- make sounds (`makeSounds`, line 203364)
- setup sound (`setupSound`, line 203398)

## Everything it says or shows (5)

- Dry and bright
- Grey and cloudy
- Chucking it down
- Weather.force: unknown state 
- float pn = wxN(vWxP.xz * 0.33) * 0.68 + wxN(vWxP.xz * 1.37 + 5.3) * 0.32;
          wxPud = smoothstep(…, pn) * smoothstep(0.35, 0.95, uWx.x);

## Labels and names (2)

- weather look · weather frame

## States and modes

- kind: pub
- mode: title

## Tuning numbers (48)

- cloud: 0, .92, 1, .85
- grey: 0, .6, .72, .8, .86, .75
- dark: 0, .14, .2, .28, .36, .16
- fog: 0, .004, .008, .011, .017, .042
- rain: 0, .28, .62, 1, .16, .3
- wetT: 0, .6, .92, 1, .3
- sun: 1, .22, .1, .05, .02, .12
- thunder: 0, 1
- hum: 0, 1, 2, 3, 4
- overcast: .55, .28, .35, .15, .2, .5
- clear: .2, .16, .12, .3
- drizzle: .15, .3, .18, .2
- heavy: .05, .2, .4
- storm: .08, .15
- FADE: 40
- wet: 0
- flash: 0, .7
- seed: 1
- flashes: 0
- thunders: 0
- drops: 700, 1800, 3600
- day: 0
- acc: 0
- guard: 0
- shown: 0
- _le: 1
- warm: 0, 2
- value: 0, .55, .006, 9, 1e-4, 8
- renderOrder: 6
- wxD: 0.0
- wxPud: 0.0
- wxRing: 0.0
- _wx: 1
- dampLast: -1
- drop: 0, .5
- vol: .16, .09
- freq: 1400, 160, 110, 90, 2600
- attack: .004, .08, .3, .15
- sweepTo: 220, 60, 50, 45
- coverT: 0, .25
- sndT: 0, 1
- boltT: 30, 25
- flashT: 0, .22
- flash2: 0
- _st: 5
- outdoorK: 1
- night: 0
- _fe: 1
