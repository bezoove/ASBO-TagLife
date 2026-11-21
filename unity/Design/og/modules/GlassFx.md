# GlassFx (old game, v95)

Part of: Pick up, carry, throw and smash things

Source in game.pretty.js: 180491-181480

## Behaviours (13)

- stroke poly (`strokePoly`, line 180537)
- ray (`ray`, line 180546)
- milky (`milky`, line 180571)
- pit (`pit`, line 180576)
- spall (`spall`, line 180580)
- mosaic (`mosaic`, line 180600)
- runs (`runs`, line 180632)
- draw lam (`drawLam`, line 180636)
- draw tem (`drawTem`, line 180707)
- draw lamp crack (`drawLampCrack`, line 180712)
- draw lamp broken (`drawLampBroken`, line 180732)
- draw mirror crack (`drawMirrorCrack`, line 180749)
- draw glitter (`drawGlitter`, line 180766)

## Everything it says or shows (5)

- Your boot bounces off the glass.
- Your knuckles bounce off the glass. It will take more than that.
- glassfx carHit
- The glass is a sagging mosaic. One more.
- Cracks run across the window. Again.

## Labels and names (6)

- smashing up a car · glassfx hit · glassfx reapply · glassfx melee · smashed a car window · glassfx smash

## States and modes

- mode: down

## Tuning numbers (33)

- jab: .18
- cross: .3
- hook: .32
- haymaker: .75
- kick: .55
- shove: .1
- head: .5
- fist: .25
- brick: 1
- bat: 1.8
- blunt: .9
- bottle: .5
- pass: 0
- lineWidth: 1, 2, .85, .8
- fall: 1
- ppm: 128
- emissiveIntensity: .4, .7
- polygonOffsetFactor: -3, -1, -2, -4
- polygonOffsetUnits: -3, -2, -4
- off: .005, .004
- renderOrder: 9, 8
- vol: .07, .14, .18, .2, .09, .08
- freq: 6200, 4800, 4200, 5200, 4500, 4e3
- kind: 2
- drag: .5
- grav: -9
- fin: .02
- fout: .4
- floor: .015
- color: 16777215
- envMapIntensity: 1.4
- dur: .4
- open: 4
