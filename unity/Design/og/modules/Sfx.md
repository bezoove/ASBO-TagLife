# Sfx (old game, v95)

Part of: Footsteps, engines, crashes, fight sounds, town ambience, dogs

Source in game.pretty.js: 300-811, 109371-109378, 133829-133829, 133836-133836, 134778-134778, 138954-138963, 139223-139223, 139227-139227, 145675-145675, 145688-145688, 146020-146020, 146024-146024, 193216-193219, 193221-193231, 193307-193318, 193345-193348, 203465-203479, 211741-211765

## Behaviours (51)

- init (`init`, line 314)
- resume (`resume`, line 340)
- set enabled (`setEnabled`, line 345)
- set volume (`setVolume`, line 348)
- now (`now`, line 351)
- src (`_src`, line 354)
- env (`_env`, line 358)
- tone (`tone`, line 361)
- noise burst (`noiseBurst`, line 375)
- whoosh (`whoosh`, line 393)
- thunk (`thunk`, line 402)
- clatter (`clatter`, line 418)
- wire (`wire`, line 435)
- cue hit (`cueHit`, line 441)
- ball click (`ballClick`, line 455)
- rail (`rail`, line 468)
- pocket drop (`pocketDrop`, line 479)
- chalk (`chalk`, line 502)
- coin (`coin`, line 510)
- coins (`coins`, line 521)
- notes (`notes`, line 524)
- till (`till`, line 538)
- pour (`pour`, line 553)
- glass (`glass`, line 567)
- bell (`bell`, line 576, 138954, 139223, 146020)
- tap (`tap`, line 585)
- ping (`ping`, line 591)
- bad (`bad`, line 599)
- good (`good`, line 606)
- door (`door`, line 613, 133829, 145675)
- step (`step`, line 623, 203465)
- thud (`thud`, line 631)
- punch (`punch`, line 641)
- reel tick (`reelTick`, line 651)
- reel stop (`reelStop`, line 657)
- fm win (`fmWin`, line 667)
- fm lose (`fmLose`, line 677)
- nudge (`nudge`, line 687)
- cheer (`cheer`, line 697)
- applause (`applause`, line 712)
- groan (`groan`, line 727)
- start ambient (`startAmbient`, line 736)
- set crowd (`setCrowd`, line 763)
- set fire (`setFire`, line 766)
- set outdoor (`setOutdoor`, line 769)
- amb tick (`_ambTick`, line 772, 193307)
- rustle (`rustle`, line 109371)
- at (`at`, line 193216)
- chime pa (`chimePA`, line 193221)
- set ambience (`setAmbience`, line 193345)
- shotgun (`shotgun`, line 211741)

## Everything it says or shows (2)

- Audio unavailable
- Signing in at the probation office…

## Labels and names (1)

- emitter key 

## States and modes

- state: running
- type: bandpass, lowpass

## Tuning numbers (21)

- volume: .8, 0
- _crowd: 0
- _crowdTarget: .4, 0
- _fire: 0
- _outdoor: 0
- value: -14, 3, .9, .55, .35, 0
- last: 0
- currentTime: 0
- sustain: 0
- vol: .3, .12, .5, .35, .2, .08
- attack: .005, .003, .03, .15, .25, .2
- slide: 0, -80, -70, -45, -60, -30
- freq: 1e3, 700, 1800, 380, 900, 2600
- sweepTo: 0, 2600, 900, 260
- power: 1
- times: 3
- level: 1
- intensity: 1
- dur: 1.6, 2
- fade: 1
- _bellWarned: 1
