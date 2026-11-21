# DayNight (old game, v95)

Part of: Day, night, weather and the clock

Source in game.pretty.js: 19327-19731, 19813-19813, 146234-146234, 146236-146238, 166068-166076, 192092-192100, 200501-200509, 203116-203124

## Behaviours (4)

- sun path (`sunPath`, line 146234)
- moon path (`moonPath`, line 146234)
- set (`set`, line 146236, 200501, 203116)
- update (`update`, line 166068, 192092)

## Labels and names (1)

- weather look

## States and modes

- mode: boot
- type: CircleGeometry

## Tuning numbers (19)

- glowK: .35, .6, 1, 1.1, .8, .4
- cloudA: .38, .5, .55, .3, .18
- fogD: .012, .013, .014, .016, .018, .021
- hemiI: .55, .5, .46, .42, .4, .38
- sunI: 3.2, 2.6, 1.7, .5, 0
- discK: 5, 4, 3, 2, 0
- value: 0, 1, .35
- minutes: 1260
- night: 1
- day: 0, 1
- sunEl: -30
- sunAz: 250
- moonEl: 0
- moonAz: 90
- stars: 1
- outdoorK: 0
- expo: .14, 0
- toneMappingExposure: 1.05
- _le: 1
