# Cars (old game, v95)

Part of: Getting in and out, horn, lights

Source in game.pretty.js: 152947-153859, 153951-153951, 154104-154104, 154126-154126, 154131-154131, 154413-154413, 155562-155569, 155590-155595, 158410-158412, 158449-158452, 158554-158556, 159181-159181, 159276-159281, 159714-159716, 159732-159744, 162980-163243, 163245-163250, 163252-163255, 163257-163263, 163726-163726, 163779-163779, 163794-163807, 163809-163812, 163814-163821, 164826-165033, 165035-165038, 165040-165046, 165048-165063, 165065-165077, 165204-165225, 165233-165239, 165342-165344, 165346-165368, 168476-168484, 170240-170248, 170832-170839, 171377-171390, 171397-171406, 171413-171462, 172022-172036 …

## Behaviours (52)

- hide static (`hideStatic`, line 152951, 159732)
- show static (`showStatic`, line 152977, 155562, 159738)
- build (`build`, line 152984, 165040, 172022, 194636, 203914, 207834)
- wheels (`wheels`, line 153103)
- lamp mats (`lampMats`, line 153111)
- make dyn (`makeDyn`, line 153151, 155590, 158449, 165048)
- place (`place`, line 153162, 165035)
- init (`init`, line 153186)
- by id (`byId`, line 153206)
- by save (`bySave`, line 153211)
- save one (`saveOne`, line 153215)
- save all (`saveAll`, line 153228)
- load (`load`, line 153231)
- car name (`carName`, line 153241)
- smash tool (`smashTool`, line 153244)
- can pick (`canPick`, line 153248)
- prompt (`prompt`, line 153251)
- labels (`labels`, line 153283)
- use (`use`, line 153291)
- pick (`pick`, line 153319)
- smash (`smash`, line 153359, 181441)
- react (`react`, line 153400)
- get in (`getIn`, line 153410, 163257)
- rider at (`riderAt`, line 153423)
- mount (`mount`, line 153436, 159276, 163252, 163809)
- dismount (`dismount`, line 153444, 163245, 163814, 186795)
- exit spot (`exitSpot`, line 153447)
- camera (`camera`, line 153477, 163143)
- horn (`horn`, line 153484)
- noise (`noise`, line 153495)
- probes (`probes`, line 153498)
- blocked at (`blockedAt`, line 153516, 165342, 170240, 171383, 177720, 195001)
- update (`update`, line 153520, 162980, 163794, 165346, 168476, 170832)
- lights (`lights`, line 153614, 203924)
- crunch (`crunch`, line 153624, 159714, 164852)
- people (`people`, line 153668, 165065, 214052)
- watchers (`watchers`, line 153745)
- pose (`pose`, line 153768, 158410, 158554)
- engine (`engine`, line 153796)
- engine set (`engineSet`, line 153841, 163153, 163796)
- fov (`fov`, line 163150)
- tyres (`tyres`, line 163171)
- squeal (`squeal`, line 163207)
- marks (`marks`, line 164826)
- scrape (`scrape`, line 164936)
- power (`power`, line 164959, 165233)
- grip (`grip`, line 164990, 171397)
- top mul (`topMul`, line 164999)
- steer pull (`steerPull`, line 165006, 165207)
- body off (`bodyOff`, line 165011)
- front k (`frontK`, line 165204)
- paved (`paved`, line 171377, 195005, 209633)

## Everything it says or shows (48)

- Get in
- Get in the …
- Try the keys in the …
- Pick the …'s door · ……
- Pick lock
- Smash the …'s window (…)
- Put that down first.
- … lock. Way beyond a … — practise on easier ones.
- Your last pick snaps. No more picks.
- OI! What are you doing to that car?!
- Here! Leave it!
- I'm phoning the police!
- That's not your car!
- Oi! Get out of that motor!
- Is that your car? Is it?
- Hold Forward to go (longer = faster), Reverse to back up, Brake to stop. Steer on the right; Exit to get out.
- W go (hold: faster), S reverse, Space brake, A / D steer, Q horn, E get out.
- Key in, a turn, and it starts.
- Under the column, two wires twisted together: it coughs into life.
- You can't drive in there.
- Not in the car, you can't.
- The car stops at the kerb of it.
- The windscreen stars from corner to corner.
- That will buff out. (It will not.)
- Metal on brick: the worst noise there is.
- You... ran me OVER!
- Somebody get his number plate!
- Call an ambulance! And the police!
- You hit …….
- Are you blind?!
- I am WALKING here!
- You nearly had me!
- Oi! Slow DOWN!
- Pull over! PULL OVER!
- Is that your vehicle, sir?
- Tax disc? MOT? Licence?
- seen driving a stolen car
- The engine was still running. Off you go.
- Hold Forward to go (drag up: boost), Reverse to back up (and to brake while moving). Handbrake: lock the back wheels to skid and drift. Steer on the right; Exit to get out.
- W go (hold: faster, Shift boost), S reverse / foot brake, Space handbrake (skid, drift), A / D steer, Q horn, E get out.
- Steam from the grille. That engine is not well.
- It is falling apart around you.
- Something fell off. Probably important.
- Burnt out. It is going nowhere.
- The starter whirrs. Nothing.
- It coughs, shudders, and stalls.
- The glass is a sagging mosaic. One more.
- Cracks run across the window. Again.

## Labels and names (11)

- smashed a car window · took a car · ran somebody over · car damage · car damage (other) · car damage reapply · police dress · glassfx smash
- swat van dress · night car glow · amb dress

## States and modes

- state: gone, offstage
- mode: sit, stand
- type: lowpass, sawtooth, square, bandpass, triangle, bike

## Tuning numbers (59)

- renderOrder: 4
- emissiveIntensity: 1.6, .7, 2.2
- polygonOffsetFactor: -3
- polygonOffsetUnits: -3
- crackWs: 1
- top: 1.5
- radius: 1.25
- height: 1
- steer: 0
- open: 2, 3, 4
- vol: .05, .4, .045, .35, .18, .12
- dur: .4, .6
- freq: 4e3, 900, 3500, 300, 3200, 1500
- carHint: 1
- exitMax: 1.6
- gear: 0, 1
- engT: 0
- dist: 5.8
- follow: 2
- along: 1
- bump: .04
- roll: 0
- _nT: .5
- slide: -20, 40
- minT: 2.4
- maxT: 5
- value: 0, 500, 2.5, 18, 40, .4
- sweepTo: 500, 420
- throttle: 0
- reverse: 0
- brake: 0
- slipR: 0
- Fyf: 0
- Fyr: 0
- Fxr: 0
- lat: 0
- shiftT: .12, 0
- _smT: .035
- life: 2.4, .7
- spread: .45, .2
- drag: 1.2, 1.5
- turb: .6, .4
- _ax: 0
- carHint3: 1
- rpmA: 0
- _hbRec: 0
- _beta: 0
- _spin: 0
- _ptV: 0
- _hvV: 0
- _pw: 0, 1, .15, .6
- _stall: 1.3
- pitch: 0
- burnt: 2
- FLAT_GRIP: 1
- jolt: 0, .04
- _offT: .12
- _sprT: .07
- grav: 3.5
