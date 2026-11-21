# Lockpick (old game, v95)

Part of: Locks and lock picking, with skill levels

Source in game.pretty.js: 131700-132385, 137022-137022, 137026-137026, 201020-201022

## Behaviours (26)

- open (`open`, line 131704)
- close (`close`, line 131778)
- caught (`caught`, line 131795)
- caught line (`caughtLine`, line 131818)
- confront opts (`confrontOpts`, line 131821)
- build (`build`, line 131852)
- resize (`resize`, line 131990)
- refresh hud (`refreshHud`, line 131996)
- say (`say`, line 132009)
- tut (`tut`, line 132034)
- tut advance (`tutAdvance`, line 132051)
- update (`update`, line 132055)
- unlock (`unlock`, line 132123)
- snap (`snap`, line 132127)
- buzz (`buzz`, line 132146, 201020)
- sfx (`sfx`, line 132151)
- draw (`draw`, line 132257)
- draw plate (`drawPlate`, line 132301)
- rivet (`rivet`, line 132320)
- draw face (`drawFace`, line 132324)
- arc text (`arcText`, line 132336)
- screw (`screw`, line 132348)
- draw plug (`drawPlug`, line 132352)
- draw driver (`drawDriver`, line 132363)
- draw pick (`drawPick`, line 132373)
- rrect (`rrect`, line 132382)

## Everything it says or shows (36)

- Drag to move the pick. Hold TURN.
- A / D to move the pick. Hold W to turn.
- … catches you at ….
- Oi! OI! That's my cash box! Out from behind my counter, now!
- That's Mickey's van, sunshine. You've got about three seconds.
- My van. My lock. Your hands. Explain.
- You what? That's Mickey Doyle's. Are you mental?
- What are you doing behind the bar?!
- Oi! What d'you think you're doing to that motor?
- Here! That's not your car!
- I can see you, you know. Hands off the car.
- "It's my mate's. He's lost his keys."
- "I dropped something, honest."
- … gives you a long look. "...Go on, then. Hop it."
- "Do I look stupid?"
- Slip them … to forget it
- Hush money, …
- … pockets it. "Never saw you."
- "Is that supposed to make it better?"
- Shove them and leg it
- Practice lock · Ron shows you
- Drag to move the pick · Hold TURN to turn the lock
- A / D or drag: move the pick  ·  Hold W, Space or right mouse: turn  ·  Esc: leave
- See the pick? Move it round the top of the lock. Slowly — feel for it.
- A / D, or drag
- Drag left and right
- Now turn the lock with the screwdriver. Don't force it.
- Hold W, Space or right mouse
- Feel it stop and shake? That's it binding. Let go before the pick snaps, move it a touch, try again.
- Let go, move the pick
- The further it turns, the closer you are. Find the spot where it goes all the way round.
- Move, then hold to turn
- Move, then hold TURN
- Got it
- The pick snaps!
- …px "IM Fell English SC", Georgia, serif

## Labels and names (3)

- lockpick done · lpr hidden · … lock…

## States and modes

- mode: lockpick, explore
- state: gone

## Tuning numbers (37)

- pick: 90
- rot: 0, 1
- maxRot: 0
- shake: 0, 1.4
- broken: 0
- snapT: 0, .85
- openT: -1, 0
- msgT: 0
- dragAcc: 0
- clickAcc: 0
- sndT: 0
- bindT: 0
- vibT: 0, .22
- animT: 0, 2.2
- moved: 0
- tutStep: 0
- lockHint: 1
- awS: 2
- fee: 10
- wary: 200
- inset: 0
- index: 40
- width: 100, 0, 2.4
- height: 100
- left: 50
- font: 400, 600, 500, 700
- shadow: 0
- top: 22
- radius: 50
- dur: 1.6
- max: 1.4
- slide: .35, -900, -25
- vol: .012, .02, .05, .025, .03, .018
- freq: 6e3, 900, 420, 2300, 5200, 3200
- sweepTo: 1800
- DEG: 0
- lineWidth: 3, 1
