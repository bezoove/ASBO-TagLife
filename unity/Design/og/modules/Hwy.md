# Hwy (old game, v95)

Part of: The long road out: underpass, forest, valley, hills

Source in game.pretty.js: 29093-29259, 29267-29307, 29386-29401, 29406-29484, 42914-43075, 43100-43219, 43259-43259, 43272-43272, 43299-43299, 43301-43316, 43372-43382, 43460-43463, 43468-43489, 43689-43692, 44270-44424, 44480-44480, 44531-44531, 195179-195179, 195185-195185, 195188-195188, 209425-209434, 209438-209446, 209450-209450, 216622-216632

## Behaviours (30)

- mk line (`mkLine`, line 29119)
- init (`init`, line 29175)
- near line (`nearLine`, line 29232)
- near (`near`, line 29267)
- point at (`pointAt`, line 29273)
- ph (`ph`, line 29285)
- lane (`lane`, line 29295)
- ch (`ch`, line 29297)
- prm (`prm`, line 29386)
- vnoise (`vnoise`, line 29406)
- fbm (`fbm`, line 29418)
- far (`far`, line 29425)
- gill x (`gillX`, line 29431)
- gorge y (`gorgeY`, line 29433)
- height at (`heightAt`, line 29438, 209425, 216622)
- in corr (`inCorr`, line 29475, 209438)
- on tarmac (`onTarmac`, line 29481)
- mats (`mats`, line 42914)
- sign rect (`signRect`, line 43060)
- ribbon (`ribbon`, line 43100)
- wall (`wall`, line 43138)
- lbox (`lbox`, line 43168)
- lcyl (`lcyl`, line 43182)
- lsign (`lsign`, line 43194)
- qsign (`qsign`, line 43216)
- ground y (`groundY`, line 43468)
- hole at (`holeAt`, line 43481)
- tree geos (`treeGeos`, line 44273)
- build forest (`buildForest`, line 44308)
- cull trees (`cullTrees`, line 44395)

## Labels and names (2)

- hwy sign atlas full · hwy guard

## Tuning numbers (19)

- rock: 0, .6
- forest: 0, .5
- stream: 0
- lid: 0, 1
- hole: 0, 1
- corr: 0
- gorge: 0, 1
- pad: 0
- lineWidth: 1, 2
- polygonOffsetFactor: -1, -3
- polygonOffsetUnits: -1, -3
- emissiveIntensity: .12
- rowH: 0
- ppm: 110
- punched: 0
- total: 0
- drawn: 0
- _ce: 1
- _ge: 1
