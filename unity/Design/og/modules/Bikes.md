# Bikes (old game, v95)

Part of: Bikes

Source in game.pretty.js: 118857-119681, 133334-133339, 133341-133347, 152844-152844, 152868-152868, 152873-152873, 155603-155608, 214064-214079

## Behaviours (39)

- init (`init`, line 118859)
- make model (`makeModel`, line 118911)
- add lock mesh (`addLockMesh`, line 118923)
- place (`place`, line 118929)
- by save (`bySave`, line 118933)
- save one (`saveOne`, line 118937)
- save all (`saveAll`, line 118949)
- load (`load`, line 118952)
- bike name (`bikeName`, line 118964)
- owner npc (`ownerNpc`, line 118967)
- tools (`tools`, line 118970)
- tool for (`toolFor`, line 118976)
- can pick (`canPick`, line 118980)
- prompt (`prompt`, line 118983)
- labels (`labels`, line 119026)
- use (`use`, line 119033)
- get on (`getOn`, line 119058, 133341)
- react (`react`, line 119076)
- pick (`pick`, line 119093)
- unlock (`unlock`, line 119131)
- start cut (`startCut`, line 119143)
- cancel cut (`cancelCut`, line 119163)
- cut update (`cutUpdate`, line 119167)
- cut seen (`cutSeen`, line 119209)
- stuck (`stuck`, line 119219)
- clear spot (`clearSpot`, line 119239)
- mount (`mount`, line 119268)
- dismount (`dismount`, line 119294)
- rest lean (`restLean`, line 119298)
- exit spot (`exitSpot`, line 119306)
- camera (`camera`, line 119335)
- horn (`horn`, line 119342)
- update (`update`, line 119352, 133334, 155603)
- people (`people`, line 119461, 214064)
- crash (`crash`, line 119531)
- watchers (`watchers`, line 119555)
- noise (`noise`, line 119585)
- pose (`pose`, line 119589)
- leg (`leg`, line 119645)

## Everything it says or shows (52)

- Get on
- Pick the … · ……
- Pick lock
- Cut lock
- … with a …: … to pick it
- Pick up the … and get on
- Get on the …
- Bolt cutters won't touch a D-lock. You'd need an angle grinder.
- Tap Sneak first: you pick a lock while sneaking.
- Sneak (C) to pick the lock.
- Put that down first.
- Left stick: up pedals, down brakes. Hold the arrows on the right to steer. Exit to get off.
- W pedal, S brake, A / D steer, Shift sprint, Q bell, E get off.
- Get off my bike, you little toerag!
- That's mine! Put it DOWN!
- Here! That's not your bike!
- Oi! I can see you, you know!
- Are you nicking that bike?
- Is that your bike?
- That's not your bike...
- Oi! Whose bike is that?
- … lock. Way beyond a … — practise on easier ones.
- Your last pick snaps. No more picks.
- The grinder screams into the lock...
- You get the jaws of the bolt cutters round it...
- You leave the lock half cut.
- You can't ride in there.
- Not on the bike. Get off and walk.
- The bike stops at the door.
- knocked somebody down with a bike
- ARGH! You maniac!
- My HIP! You absolute...
- Ring your bell, you idiot!
- You've broken me!
- You plough into …….
- Watch it!
- Oi! Eyes open!
- Do you mind?
- It's a pavement, not a velodrome.
- Over the handlebars. Ow.
- You and the bike part company.
- That's MY bike! Come back here!
- You give me that bike back!
- seen on a stolen bike
- Nice bike. Come with a receipt, did it?
- Lights on that? No? Thought not.
- Off the pavement, Lance Armstrong.
- Helmet! ...No? Your funeral.
- Ring ring! Ha. Get a car, mate.
- Is that a girl's bike? Only asking.
- Hold Forward to pedal (longer = faster, drag up to sprint), Reverse to back up, Brake to stop. Steer on the right; Exit to get off.
- W pedal, S back up, Space brake, A / D steer, Shift sprint, Q bell, E get off.

## Labels and names (3)

- took a bike · cut a bike lock · bike cut

## States and modes

- type: bike
- mode: explore, sit, stand
- state: gone, offstage

## Tuning numbers (34)

- steer: 0
- wheelA: 0
- stand: 0
- footDown: 1
- bump: 0, .05
- radius: 1.35
- height: .8
- top: 1
- vol: .05, .13, .03, .2, .25, .012
- bikeHint: 1
- lock: 0
- fxT: 0
- chkT: .3, 1
- freq: 2800, 3e3, 1800
- slide: 30
- worst: 0, 9
- want: .95
- dist: 3
- follow: 2.4
- _sq: .25
- along: 1
- drunk: 0
- _tick: 0
- _nT: .5
- minT: 1.8, 1.4
- maxT: 3.8, 3
- maxD: 3, 3.5
- dur: .6
- sit: 0
- Tarmac: 1
- pitch: -.15
- brake: 0
- throttle: 0
- bikeHint2: 1
