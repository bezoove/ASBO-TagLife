# Stash (old game, v95)

Part of: Safe stash at home

Source in game.pretty.js: 102588-102790, 110092-110098

## Behaviours (9)

- reset (`reset`, line 102592)
- door (`door`, line 102595)
- use (`use`, line 102598, 110092)
- begin (`begin`, line 102661)
- cancel (`cancel`, line 102714)
- update (`update`, line 102717)
- finish (`finish`, line 102749)
- alarm (`alarm`, line 102776)
- fire (`fire`, line 102779)

## Everything it says or shows (22)

- Empty. Mickey is not that careless twice.
- Cracking the safe
- You have been through every box already.
- Going through the boxes
- Reading the ledger
- Heavy lock. You wouldn't know where to start. Do some creeping about first.
- Picking the lock
- You draw the bolts. The yard door is open now, for anyone who knows.
- You shoot the bolts home.
- Slip out the back?
- The back gate lets out onto an alley. Go home this way and nobody sees you leave.
- Not yet
- 600 14px/1.2 "Barlow Semi Condensed", Arial, sans-serif
- Draw the bolts
- Shoot the bolts home
- You lose your concentration.
- The safe swings open: … in used notes and a ….
- Pockets and bag: ….
- Mickey's ledger: columns of names and numbers, none of them paid.
- The lock gives. The cellar is open from the yard.
- Raised voices from the back room. Mickey has found his stash disturbed.
- The back room is locked to you now.

## Labels and names (1)

- btn primary

## States and modes

- mode: explore
- kind: safe, lock

## Tuning numbers (10)

- alarmAt: 0, -1
- sound: 0
- anim: 0, 2.2
- zIndex: 60
- vol: .035, .03
- freq: 1900
- safeLooted: 1
- stashShelves: 1
- ledger: 1
- wary: 300
