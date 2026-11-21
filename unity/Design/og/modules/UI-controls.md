# UI:controls (old game, v95)

Part of: Touch controls customisable like the old game

Source in game.pretty.js: 126626-126626, 126741-126755, 131379-131383, 133155-133163, 133165-133173, 133175-133199, 133201-133227, 133229-133254, 133455-133471, 133477-133483, 133485-133492, 133494-133530, 137524-137545, 143356-143363, 144481-144505, 146662-146675, 146685-146708, 146724-146752, 146804-146816, 163281-163291, 163910-163924, 185455-185494, 187105-187124, 187394-187464, 192126-192148, 200985-201010, 201232-201247, 203672-203684, 206508-206520, 206784-206799

## Behaviours (10)

- set prompt (`setPrompt`, line 126741, 146685, 201232)
- touch state update (`touchStateUpdate`, line 131379, 143356)
- touch init (`touchInit`, line 133155, 137524, 146804)
- touch refresh (`touchRefresh`, line 133165, 133477, 146724)
- touch editor (`touchEditor`, line 133175)
- settings (`settings`, line 133201, 144481, 163910, 185455, 187105, 187394)
- controls (`controls`, line 133229, 133494, 163281)
- veh touch init (`vehTouchInit`, line 133455)
- vehicle touch editor (`vehicleTouchEditor`, line 133485)
- touch prompt update (`touchPromptUpdate`, line 146662)

## Everything it says or shows (31)

- Keys while riding (tap one, then press the new key):
- Pedal / brake (hold to stop and roll back)
- Sprint, standing on the pedals
- Get off
- Left stick steers, RT / A pedal, LT brakes, B gets off, X bell
- On a bike / in a car
- Forward: the longer you hold it, the faster
- Get off / get out
- Left stick steers, RT / A go, LT brakes then reverses, Y brake, B out, X horn
- Quick save at every wee
- Quick save at every wee (don’t ask)
- Handbrake in a car (lock the back wheels: skid, drift) · brake on a bike
- Reverse (and the foot brake while moving forward)
- Realistic car handling (tyres, gears, suspension)
- Slide assist (counter-steer, traction control)
- Skip intros (start a new game straight at home)
- Intro voice-over (the browser’s British voice, where there is one)
- Profiler on (tap it to hide).
- Profiler off.
- Sharp and smooth: steps down only when the device needs it, and back up when it can.
- Always the full look, at a steady frame rate.
- A little lighter on the device.
- Cooler and longer-lasting: 30 fps, no shadows or glow (shadows change next time you load).
- Battery saver
- Cap at 30 fps
- Auto is set for this device: 
- a lighter look, 30 fps.
- Vibration (hits, crashes, pick-ups; phones that support it)
- Weather effects (rain, wet roads, lightning)
- Mood director (music, colour, pacing)
- Dynamic resolution (lower the 3D resolution when the screen is the bottleneck)

## Labels and names (13)

- veh touch · btn sm · veh editor link · settings vehicles · settings slots · settings driving · gfx settings · a balanced look.
- the full look. · settings haptics · settings weather · settings mood · settings dynres

## States and modes

- mode: explore
- kind: putdown, obj, pickup

## Tuning numbers (2)

- zIndex: 3
- lasting: 30
