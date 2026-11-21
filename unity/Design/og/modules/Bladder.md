# Bladder (old game, v95)

Part of: Bladder

Source in game.pretty.js: 101679-102212, 136936-136938, 137292-137313

## Behaviours (28)

- on (`on`, line 101715)
- fluid (`fluid`, line 101718)
- add (`add`, line 101721)
- tick (`tick`, line 101724)
- word (`word`, line 101731)
- hud (`hud`, line 101734)
- classify (`classify`, line 101744)
- press (`press`, line 101775)
- release btn (`releaseBtn`, line 101788)
- poll (`poll`, line 101791)
- begin (`begin`, line 101795)
- end (`end`, line 101799)
- fly (`fly`, line 101835)
- hands (`hands`, line 101840)
- accident (`accident`, line 101890)
- update (`update`, line 101893, 137292)
- trace (`trace`, line 101965)
- landed (`landed`, line 102015)
- puddle (`puddle`, line 102070)
- drop puddle (`dropPuddle`, line 102100)
- clear puddles (`clearPuddles`, line 102104)
- reset (`reset`, line 102107)
- update puddles (`updatePuddles`, line 102112)
- witnesses (`witnesses`, line 102138)
- react (`react`, line 102164)
- bar warn (`barWarn`, line 102179)
- bar out (`barOut`, line 102185, 136936)
- hit npc (`hitNpc`, line 102193)

## Everything it says or shows (13)

- Hold it...
- Nothing to give. Yet.
- Ahhh. Much better.
- You zip up. Still a bit full.
- Game saved.
- Oh no. Oh no no no. You can't hold it.
- You could do with the loo soon.
- You really, really need a wee.
- You are about to burst!
- You skid in a puddle of wee. Lovely.
- Callum: "Oi! Toilets! Before Brenda sees!"
- Brenda: "NOT in my pub! Toilets are through there. Last warning."
- Brenda: "OUT. You're barred. And take your puddle with you."

## States and modes

- mode: explore, stand, wee
- kind: toilet, ground, npc, object
- state: gone, offstage, match

## Tuning numbers (48)

- aim: .4
- flowK: 0
- idleT: 0
- camT: 0
- total: 0
- _acc: 0
- _snd: 0, .085
- _downAt: 0
- _step: 0, .05, .06
- _wit: 0, .25
- _warn: 0
- _slip: 0, .3
- started: 0
- pint: .27
- stout: .27
- cider: .27
- half: .15
- wine: .14
- whisky: .04
- shot: .03
- soft: .2
- bladder: 0
- sev: 0, .25, .45, .7
- weeFlow: 0
- freq: 700, 2500, 1700
- sweepTo: 260
- vol: .12, .045, .03
- attack: .25
- rate: 7
- _force: 2.6, 0
- wet: 25
- grav: -9.81
- kind: 0
- fin: .02
- fout: .97
- step: .025
- life: .35, .4, .3
- polygonOffsetFactor: -2
- polygonOffsetUnits: -2
- renderOrder: 3
- area: .01
- age: 0
- slipT: 0
- _weeWarn: 0
- stage: 0, 1, 2
- lookW: 1
- minT: 1.4
- maxT: 3.2
