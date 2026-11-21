# NPCs (old game, v95)

Part of: Pub regulars and staff

Source in game.pretty.js: 95071-95625, 96677-96679, 96681-96699, 102564-102566, 113513-113515, 115016-115019, 115021-115030, 115032-115035, 115037-115044, 115944-115952, 121919-121919, 121997-121997, 131453-131464, 131466-131477, 131479-131482, 134808-134808, 136948-136950, 137354-137368, 138610-138616, 139132-139169, 145262-145270, 145289-145292, 145295-145301, 145303-145309, 145311-145316, 146134-146149, 146151-146181, 148677-148685, 148687-148697, 148699-148705, 150031-150039, 150041-150043, 150045-150054, 150056-150064, 150217-150224, 150693-150696, 150698-150709, 150711-150717, 150728-150736, 150958-150975 …

## Behaviours (23)

- rel (`rel`, line 95076)
- make generic (`makeGeneric`, line 95092, 180479)
- spawn for night (`spawnForNight`, line 95158, 146151, 148677, 150031, 150217, 152141)
- spawn (`spawn`, line 95175, 115032, 131479, 137354, 138610, 145289)
- clear (`clear`, line 95218, 139132, 167013, 172798, 179240, 183223)
- get (`get`, line 95222)
- present (`present`, line 95225)
- update (`update`, line 95229, 115037, 115944, 150056, 150728, 152189)
- pick look (`pickLook`, line 95240)
- arrive (`arrive`, line 95257, 115016, 131453, 150041, 150711, 152185)
- plan (`plan`, line 95272, 113513, 115021, 131466, 148699, 150045)
- free spot (`freeSpot`, line 95449, 96677, 136948)
- claim (`claim`, line 95456)
- release (`release`, line 95459)
- sit at (`sitAt`, line 95462, 150989)
- stand up (`standUp`, line 95471, 177289)
- go to (`goTo`, line 95479, 96681, 102564, 145311, 150981, 167021)
- move (`move`, line 95493, 145303, 150958, 151145, 161616, 175336)
- think (`think`, line 95559, 145295, 148687, 150977, 152161, 161398)
- finish drink (`finishDrink`, line 95596)
- choose drink (`chooseDrink`, line 95606)
- incap (`incap`, line 145262)
- can act (`canAct`, line 145268)

## Everything it says or shows (2)

- Checking the curfew…
- npcfit standUp

## Labels and names (12)

- extra punter · homes setup · home think · party setup · party think · party plan · homes think · homes plan
- amb think · cvp think · tf think · posh think

## States and modes

- state: offstage, gone, match, idle, walk
- mode: lean, explore, fire, sit, slump, stand, down, getup, ko, title
- kind: armchair, leaving, barstool, chat, sit, barhang, watch, fruit, serve, smoke, order, guard, toilet, walk, resident
- phase: exit, leave, flee

## Tuning numbers (48)

- friend: 0
- romance: 0
- respect: 0
- owesFavour: 0
- youOwe: 0
- wins: 0
- losses: 0
- lastTalkDay: -1
- grudge: 0
- skirtLen: .15
- sigma: 36
- thirst: .3
- honesty: .3
- drinkLeft: 0, 1
- talked: 0
- challenged: 0
- refused: 0
- drinksBought: 0
- matches: 0
- lostToPlayer: 0
- beatPlayer: 0
- lookT: 0
- mood: 0
- arriveAt: 1030, 1020, 1e9
- wary: 0
- discoverAt: 1
- lookW: .9, .85
- timer: 0
- until: 1e9
- fire: .3
- _stuck: 0
- _repaths: 0
- speed: 0
- _goalR: 0
- leaveAt: 5940, 1500, 1e9
- vol: .8
- day: 0
- drunk: 0
- _warned: 1
- _te: 1
- _ue: 1
- _err: 1
- _p7rate: 0
- radius: 1.4
- RATE: 0
- _kT: .15
- _kw: 0
- stuckT: 0
