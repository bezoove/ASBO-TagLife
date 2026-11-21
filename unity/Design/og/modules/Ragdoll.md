# Ragdoll (old game, v95)

Part of: Gore, injuries and blood

Source in game.pretty.js: 90328-91218, 213947-213947, 214016-214022, 214024-214027

## Behaviours (28)

- hit (`hit`, line 214016, 90558)
- busy (`busy`, line 214024, 90520)
- make (`make`, line 90466)
- active (`active`, line 90515)
- sim count (`simCount`, line 90524)
- start (`start`, line 90530)
- add vel (`addVel`, line 90543)
- fall (`fall`, line 90563)
- slip (`slip`, line 90579)
- blast (`blast`, line 90597)
- all chars (`allChars`, line 90623)
- near colliders (`nearColliders`, line 90630)
- push out (`pushOut`, line 90644)
- ground at (`groundAt`, line 90678)
- step (`step`, line 90697)
- pelvis frame (`pelvisFrame`, line 90817)
- rest basis (`restBasis`, line 90835)
- set local (`setLocal`, line 90847)
- limb (`limb`, line 90852)
- drive (`drive`, line 90860)
- begin get up (`beginGetUp`, line 91063)
- pose key (`poseKey`, line 91094)
- key hips (`keyHips`, line 91099)
- get up (`getUp`, line 91106)
- apply key (`applyKey`, line 91120)
- post (`post`, line 91130)
- clear (`clear`, line 91186)
- reset (`reset`, line 91193)

## States and modes

- mode: fall, down, sit, slump, up
- type: circle

## Tuning numbers (28)

- pelvis: 3, .12
- spine: 2.2, .12
- chest: 3, .12
- neck: 1, .06
- head: 1.6, .1
- shL: 1.4, .06
- elL: .8, .045
- wrL: .5, .04
- shR: 1.4, .06
- elR: .8, .045
- wrR: .5, .04
- hipL: 2, .08
- knL: 1.3, .06
- anL: .6, .05
- toL: .3, .035
- hipR: 2, .08
- knR: 1.3, .06
- anR: .6, .05
- toR: .3, .035
- still: 0
- nearT: 0, .3
- minT: 1.4
- maxT: 3.5
- cap: 4.5
- best: 0
- moved: 0
- damp0: .996
- MAX: 4
