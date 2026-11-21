# PoolGame (old game, v95)

Part of: Pool

Source in game.pretty.js: 123112-124049, 126431-126437, 126439-126486, 126514-126527, 126529-126533, 126535-126543, 138968-138976

## Behaviours (60)

- w3 (`W3`, line 123120)
- ball tex (`ballTex`, line 123123)
- cue mesh (`cueMesh`, line 123135)
- ribbon (`ribbon`, line 123147)
- set ribbon (`setRibbon`, line 123163)
- assets (`assets`, line 123178)
- start match (`startMatch`, line 123238)
- start practice (`startPractice`, line 123255)
- fresh (`fresh`, line 123266)
- open (`open`, line 123306)
- recruit spectators (`recruitSpectators`, line 123325)
- skill of (`skillOf`, line 123343)
- teleport (`teleport`, line 123363)
- end (`end`, line 123375)
- finish (`finish`, line 123382)
- forfeit (`forfeit`, line 123418)
- restore cam (`restoreCam`, line 123431)
- abort (`abort`, line 123434)
- release (`release`, line 123438, 126535)
- hide lamp (`hideLamp`, line 123441)
- begin turn (`beginTurn`, line 123445)
- group line (`groupLine`, line 123470)
- put cue (`putCue`, line 123480)
- auto aim (`autoAim`, line 123509)
- confirm place (`confirmPlace`, line 123522)
- drag cue (`dragCue`, line 123532)
- room behind (`roomBehind`, line 123540)
- stroke pull (`strokePull`, line 123547)
- begin stroke (`beginStroke`, line 123551)
- shoot (`shoot`, line 123565)
- strike now (`strikeNow`, line 123591)
- settle (`settle`, line 123597)
- update (`update`, line 123629)
- keys (`keys`, line 123665)
- think (`think`, line 123674)
- npc aim (`npcAim`, line 123686)
- stroke (`stroke`, line 123708)
- roll (`roll`, line 123714)
- start drop (`startDrop`, line 123735)
- sync all (`syncAll`, line 123748)
- draw guide (`drawGuide`, line 123783)
- cue frame (`cueFrame`, line 123816, 126431)
- shot ball (`shotBall`, line 123849)
- elev (`elev`, line 123855)
- set view (`setView`, line 123876)
- fit2d (`fit2d`, line 123879)
- shot cam (`shotCam`, line 123900)
- want cam (`wantCam`, line 123904)
- broadcast (`broadcast`, line 123938)
- cam update (`camUpdate`, line 123964)
- to table (`toTable`, line 123977)
- down (`down`, line 123987)
- move (`move`, line 124008)
- up (`up`, line 124034)
- nudge (`nudge`, line 124038)
- banner (`banner`, line 124042)
- actors (`actors`, line 124046)
- pose actors (`poseActors`, line 126439)
- body target (`bodyTarget`, line 126514, 138968)
- shooter ready (`shooterReady`, line 126529)

## Everything it says or shows (13)

- bold 30px Arial, sans-serif
- Pot what you like. Nobody is counting.
- You win the frame!
- … has ball in hand
- Your shot · table open
- …'s shot · table open
- Your shot
- Can't put it there.
- Pull the power slider down to set the shot.
- Table cleared
- Racking them up again…
- Black on the break
- It is spotted back

## Labels and names (2)

- … wins the frame · on the black

## States and modes

- state: match, idle
- phase: intro, over, aim, place, think, stroke, roll, rerack, pause, npcaim
- kind: match, leaving, toilet, watch, practice, place
- mode: stand, pool
- type: ball, cushion

## Tuning numbers (43)

- pow: .5
- rough: .5
- len: 1.45
- polygonOffsetFactor: -3, -2
- polygonOffsetUnits: -3, -2
- renderOrder: 6, 3, 7
- stake: 0
- turn: 0
- ang: 0
- evI: 0
- hudT: 0, .15
- pots: 0
- fouls: 0
- shots: 0
- gap: .02, -.012, .014
- wait: 0
- kev: .8
- dave: .66
- duke: .9
- tank: .7
- reg: .4
- terry: .34
- deano: .5
- ron: .45
- priya: .55
- shaz: .5
- callum: .62
- moira: .3
- winner: 1
- out: -1
- thinkT: 0
- back: .34, .8
- stop: 0
- settleT: 0
- rollT: 0
- pauseFor: 1.6
- power: .3, 0
- vmax: 0
- who: 0
- fov: 42
- bridgeSlide: 0
- cueU: 0
- cueK: 0
