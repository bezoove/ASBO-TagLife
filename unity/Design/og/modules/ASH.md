# ASH (old game, v95)

Part of: The estate: flats, Hawthorn Court tower, garages, courtyard

Source in game.pretty.js: 31612-31991, 32208-32475, 32537-32537, 32555-32555, 32567-32574, 32578-32578, 32587-32590, 32594-32594, 32597-32631, 32642-34803, 34903-35602, 35663-35695, 35699-35699, 35704-35734, 35737-35762, 35766-35789, 60595-60645, 166001-166024, 166085-166085, 166119-166149, 166765-166825, 168028-168230, 186627-186631, 201886-201893, 201985-201985, 207615-207624

## Behaviours (70)

- y (`Y`, line 31631)
- c (`C`, line 31634)
- frame (`frame`, line 31638)
- tag col (`tagCol`, line 31687)
- rect (`rect`, line 31705)
- lrect (`lrect`, line 31711)
- lifted (`lifted`, line 31714)
- lbox (`lbox`, line 31732)
- lgeo (`lgeo`, line 31743)
- lwall (`lwall`, line 31749)
- lwall d (`lwallD`, line 31768)
- lwall cols (`lwallCols`, line 31787)
- lwall cols d (`lwallColsD`, line 31792)
- pane (`pane`, line 31797)
- pane d (`paneD`, line 31810)
- flush glass (`flushGlass`, line 31823)
- window (`window`, line 31910)
- doorway (`doorway`, line 31958)
- passage frame (`passageFrame`, line 32208)
- stair tower (`stairTower`, line 32212, 35704)
- build passage (`buildPassage`, line 32419)
- blocks.push (`blocks.push`, line 32587, 35663)
- after district (`afterDistrict`, line 32597)
- mats (`mats`, line 32642)
- atlas (`atlas`, line 32806)
- decal (`decal`, line 32960)
- deck plan (`deckPlan`, line 32970)
- assign households (`assignHouseholds`, line 33022)
- style (`style`, line 33043)
- flat kit (`flatKit`, line 33165)
- build deck flat (`buildDeckFlat`, line 33431)
- build deck flat gen (`buildDeckFlatGen`, line 33434)
- ceil light (`ceilLight`, line 33650)
- win dress (`winDress`, line 33669)
- flat door (`flatDoor`, line 33689)
- loot (`loot`, line 33714)
- row plan (`rowPlan`, line 33841)
- build tower shell (`buildTowerShell`, line 33914, 35766)
- tower core (`towerCore`, line 34181, 35737)
- tower roof (`towerRoof`, line 34364)
- tower flat plan (`towerFlatPlan`, line 34447)
- build row flat (`buildRowFlat`, line 34457, 60595)
- build tower corridor (`buildTowerCorridor`, line 34615)
- sign at (`signAt`, line 34771)
- decal w (`decalW`, line 34789)
- garage door (`garageDoor`, line 34903)
- garage areas (`garageAreas`, line 34925)
- storey of (`storeyOf`, line 34948, 35372)
- build garage row (`buildGarageRow`, line 34950)
- sign at local (`signAtLocal`, line 35149)
- make areas (`makeAreas`, line 35152)
- area at (`areaAt`, line 35378)
- inside (`inside`, line 35399)
- lights for (`lightsFor`, line 35402)
- make chunks (`makeChunks`, line 35443)
- flush group (`flushGroup`, line 35483, 201886)
- chunk steps (`chunkSteps`, line 35496)
- build chunk (`buildChunk`, line 35528, 207622)
- set vis (`setVis`, line 35541)
- chunk dist (`chunkDist`, line 35552)
- tick (`tick`, line 35558, 207615)
- ensure (`ensure`, line 35592)
- cam ok (`camOK`, line 166001)
- garage tick (`garageTick`, line 166119)
- garage open (`garageOpen`, line 166133)
- on chunk built (`onChunkBuilt`, line 166765)
- make usables (`makeUsables`, line 168028)
- lift panel (`liftPanel`, line 168174)
- ride (`ride`, line 168206)
- f.clutter (`F.clutter`, line 186627)

## Everything it says or shows (21)

- Ashgrove Tower
- Ashgrove Tower lobby
- Lock-up …, Ashgrove garages
- Ashgrove Tower stairs
- Ashgrove Tower, the motor room
- Ashgrove Tower roof
- Flat …, Ashgrove Tower (…)
- Ashgrove Tower, floor …
- Take the …
- The roof door (padlocked)
- Roof door
- Call the lift
- The other lift (OUT OF ORDER)
- Open the refuse chute
- Buzz a flat
- Entry panel
- Open the garage door
- Ashgrove Tower. Max 8 persons. No smoking. No spitting.
- You are on floor …. Somebody has scratched "KAZ" into the button for 7.
- Ground floor. The doors shudder open on a smell of disinfectant and something older.
- Ground floor.

## Labels and names (16)

-  (link passage) ·  (drying room) · est tower · est garages · est areas · … …px sans-serif · bold 22px sans-serif · airing cupboard
- bold 30px sans-serif · est chunk · est chunk built · walls railgap · walls top landing · walls motor room · fix9 tower solid · flat trim

## States and modes

- kind: deck, gym2, gym, burnt, grow, store, boxes, tower, garages, roof, room, stair, keys, stolen
- state: built, none, building
- type: obb

## Tuning numbers (61)

- WALL: .2
- LIN: .02
- len: 4.8, 0
- dep: 2.8
- land: .95, 1.25
- flight: 2.6
- wall: .2
- panesN: 0
- est: 1
- side: 1, 0
- off: .002
- renderOrder: 6
- cell: 3, 18, 15
- wsp: 1.15, 1.1
- wsp1: 1.25, 1.2
- bars: 0, 2
- copingH: .2
- height: 2.04, 2.03, 1.2, 2.3, 1, 1.3
- swing: 1, -1
- size: .42, .22, 256, .55, .25, .7
- ppm: 160, 140, 120
- indoor: 1, 0
- seed: 1201, 1202, 1203, 1204, 1205, 1206
- planks: 4
- emissiveIntensity: 0
- CELL: 128
- lineWidth: 8, 4, 10, 9, 3, 5
- neglected: .8
- squat: .9
- den: 1
- student: .6
- family: .45
- single: .35
- couple: .1
- elderly: .05
- proud: 0
- empty: .1
- arm: .17
- wallTH: .1
- width: .88, 1.74
- door: 7.8, 1.2, 4.6
- small: 1
- sideWin: 1
- flatsN: 0
- ceil: 99, 2.4
- doorA: .45
- doorB: 1.35
- open: 0
- want: 0
- base: 0
- SHOW: 32
- BUILD: 42
- tower: 1
- thr: 1
- _tft: 1
- _te: 1
- freq: 300
- vol: .18, .05, .12
- radius: .9, 1, .8, 1.3
- estY: 0
- _rl: 1
