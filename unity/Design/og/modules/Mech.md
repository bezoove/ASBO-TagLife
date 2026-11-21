# Mech (old game, v95)

Part of: Industrial estate: scrapyard, pawn, taxi office, garage; Breaking into houses, alarms

Source in game.pretty.js: 150418-150690, 150733-150733, 150756-150756

## Behaviours (17)

- day (`day`, line 150424)
- on shift (`onShift`, line 150427)
- stan (`stan`, line 150431)
- live (`live`, line 150434)
- door open (`doorOpen`, line 150437)
- sync (`sync`, line 150440)
- apply (`apply`, line 150460)
- tick (`tick`, line 150469)
- watch (`watch`, line 150500)
- burglar (`burglar`, line 150508)
- say (`say`, line 150546)
- menu (`menu`, line 150550)
- parts (`parts`, line 150601)
- menu again (`menuAgain`, line 150622)
- sell (`sell`, line 150630)
- damaged (`damaged`, line 150652)
- repair (`repair`, line 150665)

## Everything it says or shows (14)

- A burglar alarm starts up in the building. Time to go.
- Nobody at the counter. The garage is shut.
- Any parts going?
- Got some scrap for you
- Can you sort the cars out?
- Have a natter
- Give them something
- See you later
- Never mind
- Anything else?
- Stan pays … for the lot.
- Nothing wrong with any cars that I can see.
- Stan wants … for the lot.
- Stan has had a go at the cars with a rag and a tin of filler. The marks are gone.

## Labels and names (1)

- burglar alarm

## States and modes

- state: gone, offstage, idle
- mode: stand, explore, talk

## Tuning numbers (16)

- alarmDay: -1
- nextBark: 0
- tickT: 0, .5
- shutA: 0
- doorA: 0
- day: 0
- until: 1e9, 0
- drunk: 0
- ref: 8
- vol: .4
- dur: 1.5
- tune: 3
- mute: 0
- tot: 0
- cost: 0
- _warned: 1
