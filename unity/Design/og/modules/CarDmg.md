# CarDmg (old game, v95)

Part of: Crashes and damage

Source in game.pretty.js: 163956-164741, 165093-165093, 165118-165118, 165135-165135, 165137-165155, 165175-165204, 181276-181334, 181336-181339, 181341-181344, 181346-181359, 181361-181375, 181377-181384

## Behaviours (31)

- hook (`hook`, line 163959)
- ens (`ens`, line 163988)
- meshes (`meshes`, line 163992)
- is glass (`isGlass`, line 163996)
- dent all (`dentAll`, line 164000)
- add dent (`addDent`, line 164050)
- lift (`lift`, line 164054)
- part (`part`, line 164110)
- detach (`detach`, line 164167, 181341)
- step debris (`stepDebris`, line 164210, 181377)
- windows (`windows`, line 164239)
- crack tex (`crackTex`, line 164290)
- set win (`setWin`, line 164293, 181309)
- broken lamp mat (`brokenLampMat`, line 164329, 181349)
- smash lamp (`smashLamp`, line 164348, 181346)
- hit (`hit`, line 164392, 165175, 181276)
- hang bumper (`hangBumper`, line 164447)
- repivot (`repivot`, line 164463)
- pop lid (`popLid`, line 164473)
- open door (`openDoor`, line 164485, 181336)
- burst (`burst`, line 164494)
- wheel world (`wheelWorld`, line 164523)
- wheel look (`wheelLook`, line 164532)
- scratch all (`scratchAll`, line 164542)
- ignite (`ignite`, line 164581)
- glow mat (`glowMat`, line 164585)
- fire step (`fireStep`, line 164602)
- soot model (`sootModel`, line 164664)
- smoke step (`smokeStep`, line 164681)
- reapply (`reapply`, line 164705, 181361)
- crackle (`crackle`, line 165137)

## Everything it says or shows (4)

- BANG: a tyre goes.
- Flames from under the bonnet. GET OUT.
- It's getting hot in here. OUT!
- Smoke in the cabin. Get out of the car!

## Labels and names (2)

- glassfx hit · glassfx reapply

## States and modes

- type: lowpass

## Tuning numbers (25)

- r2i: 1
- lim: .02
- freq: 1800, 4e3, 5200, 4500, 700
- vol: .2, .03, .35, .12, .18, .45
- slide: -120, -60, -40
- renderOrder: 9, 4
- lineWidth: 1
- polygonOffsetFactor: -2, -3
- polygonOffsetUnits: -2, -3
- into: -.2
- span: 2
- life: .9, 3, 2.2
- spread: 1.2, .3, .25
- fire: 1, 0
- fireT: 40
- kind: 0
- drag: 1, .6
- grav: .8
- turb: .8, .5
- fin: .05
- fout: .6
- burnt: 1
- _hotT: 2.5
- _soot: 1
- value: 0, 1200
