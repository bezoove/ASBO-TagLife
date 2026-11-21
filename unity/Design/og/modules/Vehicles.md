# Vehicles (old game, v95)

Part of: Car controls: brake into reverse, handbrake skids, fast

Source in game.pretty.js: 118028-118171, 118183-118183, 132764-132764, 132767-132767, 132776-132776, 132779-132779, 132787-132787, 132795-132795, 133360-133360, 133363-133363, 133372-133372, 133375-133375, 133384-133384, 133387-133387, 133464-133464, 133467-133467, 152804-152804, 153983-154015, 154049-154053, 159181-159181, 159643-159650, 163844-163857, 163904-163906

## Behaviours (9)

- register (`register`, line 118040)
- get (`get`, line 118043)
- riding (`riding`, line 118046)
- keys (`keys`, line 118049)
- pad (`pad`, line 118053)
- read ctl (`readCtl`, line 118062, 159643)
- mount (`mount`, line 118103)
- dismount (`dismount`, line 118128, 154049, 163904)
- update (`update`, line 118142, 153983, 163844)

## Everything it says or shows (3)

- Slow down first: jumping off at this speed would hurt.
- No room to open the door here.
- Stop the car first.

## Labels and names (1)

- ride pose

## States and modes

- mode: explore, sit, stand
- type: car

## Tuning numbers (12)

- steerHold: 0
- throttle: 0, 1
- brake: 0, 1
- steerD: 0
- seatH: .9
- footRest: .3
- speed: 0
- crouch: 0
- dist: 3
- sens: .0055
- _lookT: 1.2
- zoom: 0
