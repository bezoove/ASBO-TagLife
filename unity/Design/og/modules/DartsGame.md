# DartsGame (old game, v95)

Part of: Darts

Source in game.pretty.js: 121130-122006, 133792-133792, 134987-134987, 143946-143946

## Behaviours (47)

- init (`init`, line 121149)
- vm x (`vmX`, line 121153)
- build viewmodel (`buildViewmodel`, line 121156)
- legacy viewmodel (`legacyViewmodel`, line 121177)
- board by id (`boardById`, line 121236)
- start match (`startMatch`, line 121239)
- start practice (`startPractice`, line 121291)
- wire match (`wireMatch`, line 121327)
- place (`place`, line 121335)
- recruit spectators (`recruitSpectators`, line 121346)
- begin turn (`beginTurn`, line 121362)
- nerves (`nerves`, line 121389)
- cam fp (`camFP`, line 121400)
- cam npc (`camNpc`, line 121421)
- cam board (`camBoard`, line 121428)
- can throw (`canThrow`, line 121432)
- down (`down`, line 121436)
- move (`move`, line 121452)
- up (`up`, line 121463)
- screen to board (`screenToBoard`, line 121485)
- swipe (`swipe`, line 121494)
- sway (`sway`, line 121497)
- spread mm (`spreadMM`, line 121514)
- throw at (`throwAt`, line 121524)
- rel mm (`relMM`, line 121540)
- shot target (`shotTarget`, line 121547)
- throw player (`throwPlayer`, line 121592)
- dart origin (`dartOrigin`, line 121595)
- release (`release`, line 121609)
- reset arm (`resetArm`, line 121629)
- on land (`onLand`, line 121633)
- on visit (`onVisit`, line 121651)
- spect react (`spectReact`, line 121673)
- ai step (`aiStep`, line 121682)
- update (`update`, line 121710)
- arm key (`armKey`, line 121736)
- vm update (`vmUpdate`, line 121747)
- render viewmodel (`renderViewmodel`, line 121798)
- finish (`finish`, line 121801)
- forfeit (`forfeit`, line 121844)
- draw chalk (`drawChalk`, line 121854)
- start bg (`startBg`, line 121881)
- bg turn (`bgTurn`, line 121931)
- bg update (`bgUpdate`, line 121943)
- end bg (`endBg`, line 121975)
- abort bg (`abortBg`, line 121989)
- bg odds (`bgOdds`, line 121999)

## Everything it says or shows (14)

- …. … versus ……. Game on!
- The board's in use.
- Bounce out!
- Game shot, and the match!
- Game shot, and the leg!
- Game shot!
- Match conceded.
- Mon - Practice night
- Sat - Big money
- Bet on … v …
- Bet on …
- … wins! You collect ….
- … beat …. Your bet's gone.
- Bet refunded

## Labels and names (1)

- first-person arm

## States and modes

- mode: x01, clock, oche, stand, highscore, darts
- state: match, between, done, play, idle
- kind: practice, match, leaving, toilet, human, ai, watch
- phase: turn, between, intro, wait

## Tuning numbers (31)

- sRef: 2.2
- holdT: 0
- yaw: -.12
- open: 0
- waitT: 1.4, 0, .9, 2.5, 1.6, .8
- dartsThisTurn: 0
- rate: .95, .85
- visits: 9999, 0
- darts: 0
- total: 0
- best: 0
- halfW: .27
- hfov: 2
- vfov: 2
- lookDy: 0, .02
- extra: 85
- base: 3.2
- calm: .65
- trem: 1.2
- focus: 1.05
- casual: .6, .55, 64, .7
- pub: 1, 46, .5
- pro: 1.25, 1.2, 30, .35
- dur: .6
- visitTotal: 0
- yawT: -.12
- winner: 1
- lineWidth: 3, 2
- start: 301
- legsToWin: 1
- radius: 2
