# Adapt (old game, v95)

Part of: Smooth frame rate checked on every change

Source in game.pretty.js: 187127-187305, 187357-187357, 187382-187382, 187387-187387, 192065-192067, 192069-192072, 192074-192082

## Behaviours (12)

- mode (`mode`, line 187191)
- m (`M`, line 187195)
- tier name (`tierName`, line 187198)
- tier for (`tierFor`, line 187201, 192065)
- base pr (`basePr`, line 187204, 192074)
- post on (`postOn`, line 187211)
- apply (`apply`, line 187214)
- set level (`setLevel`, line 187237)
- reset (`reset`, line 187241)
- want cadence (`wantCadence`, line 187254, 192069)
- gate (`gate`, line 187257)
- frame (`frame`, line 187265)

## States and modes

- mode: title

## Tuning numbers (23)

- level: 0
- scale: 1
- cadence: 60, 30
- heat: 0
- MIN_SCALE: .75
- prMax: 2, 1.75, 1.5
- budget: 37e5, 42e5, 24e5, 14e5
- min: 0, 1, 3
- max: 4, 0
- cad: 30
- sum: 0
- late: 0
- cpu: 0
- slow: 0
- fast: 0
- fast30: 0
- upWait: 6
- lastDown: -1e9
- settleUntil: 0
- _lastDraw: 0
- _cpu: 0
- sharp: .45
- tgt: 1e3
