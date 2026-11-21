# Hijack (old game, v95)

Part of: Taking cars: break-ins and carjacking

Source in game.pretty.js: 186667-186735

## Behaviours (3)

- safe spot (`safeSpot`, line 186677)
- place (`place`, line 186701)
- station (`station`, line 186707)

## Everything it says or shows (7)

- Closing.playerOutside (14l): town areas counted as inside the pub -> eviction -> Closing.forceOut -> Barred.expel [FIXED]
- Barred.expel (14k): any caller teleported to the pub door whatever the player's whereabouts [GUARDED]
- Bobby.arrest release (11n, 14r) and Life.cells (14t): pub door [NOW the police station front]
- Game.startNight (14_game) on a load not saved mid-night / an invalid saved position: pub entrance [NOW nearest free spot / the flat]
- Game.startNight error recovery (14o): pub entrance only if the player is in no area [kept]
- Tag wall (14zzzc): clamps to the zone edge only [kept]
- CivicLive.seen (14zzx): A&E reception, on purpose [kept]

## Tuning numbers (6)

- held: 0
- cleaned: 0
- expelSkipped: 0
- stationPut: 0
- loadFixed: 0
- yaw: 0
