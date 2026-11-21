# Skate (old game, v95)

Part of: Skateboard

Source in game.pretty.js: 141624-141761, 141784-141784

## Behaviours (8)

- outdoors at (`outdoorsAt`, line 141634)
- mk (`mk`, line 141638)
- start (`start`, line 141681)
- stop (`stop`, line 141690)
- fall (`fall`, line 141696)
- pre (`pre`, line 141711)
- post (`post`, line 141714)
- gait (`gait`, line 141756)

## Everything it says or shows (8)

- Hands full.
- On the board. Push off with the stick; Jump for an ollie.
- On the board. Push off; let go to coast; Space to ollie. Use it again to step off.
- Not in here. Brenda would have it in the bin and you in the street. Take it outside.
- The board goes one way, you go the other.
- You flip the board up under your arm. Not on Brenda's floors.
- A crack in the tarmac and a skinful of beer. Down you go.
- Straight into it. The board carries on without you.

## States and modes

- mode: explore, down, getup, sit
- state: gone, offstage

## Tuning numbers (12)

- lift: .075
- _roll: 0, .22
- _fallT: 0, 1
- minT: 1.4
- maxT: 3
- dir: 1
- _lifted: 0
- freq: 260
- attack: .03
- dur: .8
- lookW: 1
- lookT: 2
