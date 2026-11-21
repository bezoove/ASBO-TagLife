# Jack (old game, v95)

Part of: Taking cars: break-ins and carjacking

Source in game.pretty.js: 158566-159185, 159229-159229, 159234-159237, 159271-159271, 159809-159809, 159853-160184, 160186-160189, 160498-160513, 172115-172143, 172145-172148, 186743-186751, 186753-186783, 196944-196947, 215584-215587

## Behaviours (28)

- door shape (`doorShape`, line 158574)
- extract (`extract`, line 158612)
- door (`door`, line 158677)
- door card (`doorCard`, line 158697)
- door to (`doorTo`, line 158711)
- handle world (`handleWorld`, line 158715)
- cand (`cand`, line 158723, 196944, 215584)
- start (`start`, line 158739, 159853)
- shot a (`shotA`, line 158765, 159939)
- shot b (`shotB`, line 158772, 159941)
- update (`update`, line 158780, 159941, 160498)
- head at (`headAt`, line 158909)
- unseat (`unseat`, line 158914, 160170)
- impact (`impact`, line 158921)
- crime (`crime`, line 158958)
- say (`say`, line 158986)
- pseudo (`pseudo`, line 158991)
- resist (`resist`, line 159000, 160182)
- fight (`fight`, line 159016, 172115)
- finish (`finish`, line 159050, 160186)
- steal (`steal`, line 159063, 172145, 186743)
- victims step (`victimsStep`, line 159122, 186753)
- reset (`reset`, line 159175)
- cam want (`camWant`, line 159905)
- cam init (`camInit`, line 159928)
- cam step (`camStep`, line 159935)
- drv at (`drvAt`, line 160148)
- cleanup (`cleanup`, line 160176)

## Everything it says or shows (21)

- Wh- what are you DOING?!
- Get OFF me!
- What's your problem?!
- dragged a driver out of their car
- Oh my God, he's dragging him out!
- Leave him ALONE!
- Somebody ring the police!
- Wrong car, sunshine.
- Come on then! COME ON!
- You picked the wrong bloke.
- … shoves you off. They're not going quietly.
- Get in
- My CAR! He's took my car!
- Somebody call the police! My car!
- I'll have you for this! I've got your face!
- the driver you pulled out reported you
- … grabs your wrists. They're not going quietly.
- Get your hands OFF me!
- Oh, you WANT some?
- I don't think so, pal.
- PC Hargreaves is out of the car and coming for you.

## Labels and names (6)

- carjack fight · carjack fight start · assaulting a police officer · police jack fight · police car · hijack victim walk

## States and modes

- mode: explore, stand, stolen
- state: idle, jacked

## Tuning numbers (33)

- jacks: 0
- fights: 0
- rate: 10, 12, 9, 14, 8
- minT: 30
- maxT: 60
- speed: 1.2, 0
- dur: .8
- footRest: 0
- freq: 260
- vol: .35
- life: .8, 600
- spread: .3
- kind: 1
- grow: .03
- dir: 1
- arriveAt: 0
- drunk: 0
- lock: 0
- steer: 0
- open: 1
- radius: 1.25
- height: 1
- slow: 1
- dOpen: 1.22
- cap: 3.4
- maxD: 2.2
- turn: 0
- sit: 1
- corr: 0
- buildT: 1e9
- lat: 0
- _ax: 0
- cruise: 0
