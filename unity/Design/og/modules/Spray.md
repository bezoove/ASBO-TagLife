# Spray (old game, v95)

Part of: Graffiti and chalk

Source in game.pretty.js: 142098-143189, 143288-143288, 143305-143305, 143307-143307, 143309-143309, 143327-143327, 143334-143334, 143337-143337, 146754-146763, 146795-146795, 146801-146801, 146819-146819, 146826-146826

## Behaviours (58)

- s (`S`, line 142122)
- held (`held`, line 142126)
- can (`can`, line 142130)
- held id (`heldId`, line 142133)
- is chalk (`isChalk`, line 142137)
- left (`left`, line 142140)
- col idx (`colIdx`, line 142144)
- skip roots (`skipRoots`, line 142147)
- candidates (`candidates`, line 142160)
- visible (`visible`, line 142176)
- tri count (`triCount`, line 142181)
- grid (`grid`, line 142184)
- grid hit (`gridHit`, line 142231)
- ray world (`rayWorld`, line 142289)
- ray people (`rayPeople`, line 142334)
- aim ray (`aimRay`, line 142367)
- nozzle (`nozzle`, line 142382)
- target (`target`, line 142389)
- basis (`basis`, line 142400)
- mk mesh (`mkMesh`, line 142409)
- probe (`probe`, line 142436)
- refresh (`refresh`, line 142452)
- new patch (`newPatch`, line 142457)
- from rec (`fromRec`, line 142483)
- remove patch (`removePatch`, line 142504)
- clear meshes (`clearMeshes`, line 142513)
- find patch (`findPatch`, line 142517)
- enc (`enc`, line 142537)
- dec (`dec`, line 142543)
- total dabs (`totalDabs`, line 142548)
- draw (`draw`, line 142553)
- dab (`dab`, line 142581)
- replay (`replay`, line 142590)
- preset dabs (`presetDabs`, line 142714)
- update (`update`, line 142726)
- cycle (`cycle`, line 142744)
- use (`use`, line 142747)
- empty (`empty`, line 142752)
- spray (`spray`, line 142761)
- start preset (`startPreset`, line 142825)
- preset tick (`presetTick`, line 142862)
- begin session (`beginSession`, line 142909)
- pub wall (`pubWall`, line 142924)
- shop wall (`shopWall`, line 142927)
- end session (`endSession`, line 142930)
- judge (`judge`, line 142934)
- notice tick (`noticeTick`, line 142974)
- paint u (`paintU`, line 142996)
- patch mat (`patchMat`, line 143010)
- patch char (`patchChar`, line 143027)
- apply npc (`applyNpc`, line 143032)
- paint npc (`paintNpc`, line 143046)
- save paint (`savePaint`, line 143055)
- npc paint tick (`npcPaintTick`, line 143062)
- sprayed (`sprayed`, line 143078)
- rebuild (`rebuild`, line 143122)
- new night (`newNight`, line 143132)
- chip update (`chipUpdate`, line 143164, 146754)

## Everything it says or shows (22)

- Worn down to nubs. That was the last of the chalk.
- The can coughs and runs dry.…
- Chalk is for the pavement, not people.
- Get down close to the ground with the chalk.
- Hold still, they won't. Freehand only on people.
- Aim at a wall or the floor, closer.
- Chalk is for the ground.
- Who's been spraying on MY walls?!
- Oh, for... Who did THIS?
- If I find out who did that...
- My shutters! Who did this? I'll check the tape.
- Again! Every week, again!
- MY CARDIGAN! Out! OUT of my shop!
- Are you MAD? This is wool!
- Right. That is assaulting a police officer.
- You've just sprayed a policeman. Think about that.
- … looks down at the paint and cracks up.
- … is NOT happy about the paint.
- Brenda has been out with a bucket and a wire brush: the paint on the pub is gone.
- Raj has scrubbed his shop front. He looks tired.
- The council cleaners have been round the street.
- The rain has washed the chalk away.

## Labels and names (1)

- … comes for you!

## States and modes

- state: gone, offstage
- mode: sit, slump, title, explore

## Tuning numbers (34)

- MAXP: 48
- SIZE: 1.4
- RES: 256
- LIFE: 40
- MAXD: 4e4
- RANGE: 3
- mode: 0
- _hiss: 0, .8, .2
- _arm: 0, .45
- _candT: -1
- _noticeT: 0, 1.2
- _lastPaintT: -9
- inv: 1
- near: .02, 0
- far: 4.2, .12
- anisotropy: 4
- polygonOffsetFactor: -4
- polygonOffsetUnits: -4
- renderOrder: 3
- off: .006
- dur: .6, .9
- drag: 2.6
- turb: .4
- fin: .05
- fout: .4
- dabs: 0
- lookW: .8, 1, .9
- lookT: 2, 3, 2.5
- _rb: 1
- wary: 120
- pub: 0
- shop: 0
- street: 0
- chalk: 0
