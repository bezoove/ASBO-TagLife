# Approach (old game, v95)

Part of: People starting conversations with you

Source in game.pretty.js: 102230-102512, 102530-102547, 102569-102579, 104695-104695, 113517-113519, 115046-115048, 131484-131486, 145408-145416, 150066-150068, 150719-150721, 152228-152230, 193275-193287, 208380-208390, 208392-208400, 208402-208424, 213073-213073

## Behaviours (15)

- reset (`reset`, line 102246)
- is (`is`, line 102249)
- label (`label`, line 102252)
- reasons (`reasons`, line 102256, 115046, 131484, 150066, 150719, 152228)
- busy (`busy`, line 102269, 113517)
- player free (`playerFree`, line 102272)
- pick (`pick`, line 102278, 102530)
- start (`start`, line 102297, 208392)
- route (`route`, line 102312)
- arrive (`arrive`, line 102341)
- release (`release`, line 102358)
- finish (`finish`, line 102364)
- engage (`engage`, line 102367)
- ignored (`ignored`, line 102473)
- update (`update`, line 102482, 102540, 102569, 145408, 208402)

## Everything it says or shows (11)

- … is smiling at you
- … has a job for you
- You're on
- Not now
- Here, get one in (£3)
- Gave … a few quid
- Buy them a drink
- Not tonight
- Flirt back
- Have a natter
- Not just now

## Labels and names (7)

- … wants a chat · … says hello · … wants a game · … is after something · … wants a word · … has news · … is swaying over

## States and modes

- phase: wait, walk
- mode: down, explore, stand, talk, darts, pool, lift, cutscene
- state: match, gone, offstage, idle, walk
- kind: leaving, toilet, fruit, confront, drunk, flirt, approach, walk, puzzled

## Tuning numbers (14)

- cool: 40
- _chk: 0, 1.2, 3
- lastChallenge: -1e9
- repath: 0, 1
- tries: 0
- until: 1e9
- lookW: 1, .6
- lookT: 3, 2
- dur: 1.4
- vol: .08
- cadged: 1
- tipped: 1
- acc: .4
- freeze: 0
