# Game:day (old game, v95)

Part of: Day, night, weather and the clock

Source in game.pretty.js: 133553-133553, 133566-133566, 133802-133837, 133850-133858, 133863-133912, 135469-135471, 135611-135613, 136881-136884, 136942-136944, 137055-137057, 138104-138117, 138119-138138, 138715-138715, 138750-138754, 138788-138799, 139014-139029, 139031-139063, 139078-139093, 140112-140120, 143316-143324, 145665-145665, 145724-145724, 145884-145890, 145894-145896, 145905-145921, 146051-146051, 146059-146059, 146181-146183, 146284-146291, 147143-147145, 150897-150897, 150901-150901, 152890-152895, 168227-168227, 186835-186853

## Behaviours (6)

- start night (`startNight`, line 133802, 135469, 135611, 136942, 138119, 139031)
- settle crowd (`settleCrowd`, line 133827, 146181)
- tick clock (`tickClock`, line 133850, 138750, 138788, 139078, 145884)
- end night (`endNight`, line 133863, 137055, 138104, 145905, 152890)
- next day (`nextDay`, line 133907, 139014, 145894)
- new day (`newDay`, line 145896)

## Everything it says or shows (24)

- Last orders at the bar!
- Net for the night
- Mickey's interest bites: your debt grew from … to …. Tank knows your face now.
- You still owe Mickey …. Due ….
- Brenda has your tab at …. She's not thrilled that it's still open.
- … is still waiting for their … back.
- You will feel this tomorrow.
- You were out cold for a while. Your head is ringing, and everyone in the pub will remember it.
- Brenda has you by the collar and out of the door. Her trust in you has taken a beating, and the whole pub will remember 
- Pockets picked
- Taken in total
- … sends a message: "Good night. x"
- The next thing you know there is gravel against your cheek and the night air on your face. Somebody has been through your pockets, and your jaw feels like a bag of marbles.
- Brenda marches you to the door by the scruff. "Out. And think about what you've done." The cold air hits you like a slap.
- You head out into the cold and walk home. The Eggy Duck will be there on ….
- Brenda rings the bell. "Time, ladies and gentlemen, please." The lights go up. You blink like a mole.
- Knocked out!
- Home time
- … evening. The doors are open.
- Your head is pounding.
- Brenda: "Come on, you. Finish up and out of that door."
- Brenda: "Right, I am locking up. Out, or I ring PC Bobby."
- , new Error(
- Ground floor.

## Labels and names (7)

- who the thief was. · the puddle. · who started it. · the scene you made. · bar duck · spray night · hijack load place

## States and modes

- mode: stand, darts, pool, talk, cutscene, lift, explore, title, boot
- state: gone

## Tuning numbers (23)

- clock: 1020, 1440, 1200
- stomach: 0
- blood: 0
- food: .2
- carrying: 0
- drinkLeft: 0
- carryHang: 0
- injury: 0
- bladder: .08
- wet: 0
- pitch: .18, .16
- distT: 2.6
- dist: 2.6
- reported: 0
- sneakXp: 0
- credits: 0
- warned: 0
- barredDay: -1
- evicted: 1
- evAt: 0
- endT: 0
- cutT: 0
- _banDays: 0
