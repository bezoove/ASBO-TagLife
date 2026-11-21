# Loot (old game, v95)

Part of: Searching people who've been knocked out

Source in game.pretty.js: 115298-115932, 115949-115949, 144084-144090, 144615-144653

## Behaviours (36)

- live (`live`, line 115304)
- is out (`isOut`, line 115307)
- reach (`reach`, line 115316)
- center (`center`, line 115330)
- shot (`shot`, line 115345, 144615)
- searchable (`searchable`, line 115391)
- seed (`seed`, line 115394)
- ensure (`ensure`, line 115397)
- items (`items`, line 115472)
- loose (`loose`, line 115475)
- cash (`cash`, line 115479)
- is under (`isUnder`, line 115482)
- garments (`garments`, line 115485)
- bare (`bare`, line 115552)
- stripped (`stripped`, line 115556)
- touch (`touch`, line 115559)
- take cash (`takeCash`, line 115563)
- take item (`takeItem`, line 115576)
- mark lifted (`markLifted`, line 115585)
- note (`note`, line 115591)
- garment item (`garmentItem`, line 115595, 144084)
- take garment (`takeGarment`, line 115617)
- apply app (`applyApp`, line 115648)
- rebuild (`rebuild`, line 115655)
- sight (`sight`, line 115669)
- open (`open`, line 115691)
- close (`close`, line 115713)
- report (`report`, line 115728)
- spotted (`spotted`, line 115743)
- update (`update`, line 115786)
- take all (`takeAll`, line 115830)
- bulk step (`bulkStep`, line 115836)
- start strip (`startStrip`, line 115854)
- gone (`gone`, line 115861)
- leave home (`leaveHome`, line 115868)
- wake (`wake`, line 115881)

## Everything it says or shows (38)

- Front pocket
- Jacket pocket
- Shirt pocket
- Trouser pocket
- Back pocket
- Inside pocket
- Inside jacket pocket
- Looted from …
- Still warm. Not your size, probably, but you will not be the first to wear a stranger's shirt.
- Still warm and smelling of somebody's Friday night.
- Worn in by someone else's feet.
- It smells of someone else's hair.
- Not your prescription. Everything looks slightly wrong.
- Not yours.
- … Nicked off ….
- Press and hold the search box to go through their pockets.
- Hold Space (or press and hold the button) to go through their pockets.
- robbing somebody who was out cold
- OI! What are you doing to him?!
- Oi! Get your hands off him!
- Is that bloke being ROBBED?!
- Hey! Leave him alone!
- Brenda! Somebody is turning him over!
- Oh no. Oh no no no. Where are my TROUSERS?
- Who has had my clothes?! Brenda! BRENDA!
- I am in my UNDERPANTS!
- It was YOU. I'll kill you!
- … covers up and bolts for the door.
- … comes round in their underwear.
- Where are my SHOES?
- Where has my hat gone?
- Where's my jacket?!
- Where are my trousers?!
- I can't see a thing. Where are my glasses?
- My chain! Where is my chain?
- Somebody has had my things!
- Where are my darts?!
- … knows who put them on the floor.

## Labels and names (7)

- gold chain · loot rebuild · loot report · … saw you. · loot spotted · … is coming round. · loot shot

## States and modes

- state: gone, offstage, match, idle
- mode: down, slump, explore, sit, stand
- kind: item

## Tuning numbers (19)

- REACH: 1.75
- SEARCH_T: 1.5
- STRIP_T: 1
- score: 0
- side: 1
- day: 0
- cash: 0
- amount: 0
- base: 14, 18, 8, 10, 20
- money: 0
- lootHint: 1
- lift: 0
- freeze: 0
- wary: 240
- lookW: 1, .5
- dur: 1.4, 1.1, 2
- liftTrem: .2
- step: 0
- fleeBy: 0
