# CivicLive (old game, v95)

Part of: Police station and hospital

Source in game.pretty.js: 172829-173368, 173383-173409, 173411-173416, 174648-174656, 177110-177112, 177114-177118, 177120-177134, 178557-178557, 178592-178599

## Behaviours (22)

- setup (`setup`, line 172833)
- buy (`buy`, line 172851)
- person (`person`, line 172883)
- say (`say`, line 172887)
- use (`use`, line 172893, 174648)
- ae desk (`aeDesk`, line 172981)
- seen (`seen`, line 173014)
- pol desk (`polDesk`, line 173041)
- engine model (`engineModel`, line 173081, 177110)
- engine init (`engineInit`, line 173146, 177114)
- blues (`blues`, line 173179)
- siren (`siren`, line 173189)
- route (`route`, line 173214)
- set path (`setPath`, line 173229)
- dispatch (`dispatch`, line 173238)
- follow (`follow`, line 173266)
- tick (`tick`, line 173291, 173411, 177120, 178592)
- park (`park`, line 173326)
- crew out (`crewOut`, line 173333)
- crew in (`crewIn`, line 173345)
- scene tick (`sceneTick`, line 173349)
- bobby tick (`bobbyTick`, line 173383)

## Everything it says or shows (75)

- Book in at A&E
- Ask at reception
- Pharmacy counter
- Vending machines
- Call the lift
- Stairs (staff only)
- The mortuary doors
- Drinks machine
- Front counter
- Evidence store
- Ring the bell
- The pole
- Visiting's two till four and six till eight. Wards are upstairs, lifts are out. Stairs are staff only. So... good luck.
- A&E is through there. If you're not bleeding, you'll be a while.
- Have you tried the GP? No? Nobody has. Take a seat.
- Hospital pharmacy
- Prescriptions: £9.90 an item. Or just this lot:
- Bottle of water
- Chewing gum
- The café is shut. A sign says: "Back at 7am. Pam".
- Friends of Ashby General · Café
- Run by volunteers. Everything homemade by Pam.
- Cup of tea
- A strong tea in a polystyrene cup. You feel a bit more human.
- Instant. Very instant. You sharpen up a touch.
- Slice of Victoria sponge
- Pam's sponge. It is, genuinely, the best thing in this hospital.
- The machine eats your pound. The crisps hang there, on the edge, forever.
- Vending machine
- Exact change only. It says.
- Energy drink
- You press the button. Nothing. A sign: OUT OF ORDER — engineer called. The date on it is from June.
- A keypad and a sign: STAFF ONLY. Wards are reached by the lifts. Which are broken.
- Locked. MORTUARY & PATHOLOGY — authorised personnel only. Something hums behind the doors, and it is very cold down here.
- A steel door with a keypad. EVIDENCE · NO ENTRY. You would need more than a hairpin for this one.
- Watch manager
- We're out on a shout, love. Come back later.
- Alright? This isn't a shop, you know. What can we do for you?
- If it's about the kitten up the tree, it came down on its own.
- Smoke alarm checks are free, if you want one. Bet yours has no battery in.
- You give the brass pole a twirl. It is very, very shiny. Somebody polishes this every day.
- Current waiting time: 4 hrs 35 mins
- "What seems to be the problem?" Martin looks you up and down. "You look fine to me. Are you just here for the vending machine?"
- Wait to be seen (about 1½ hours)
- Make a fuss (it never works)
- Ask for a plaster anyway
- "Everybody here is in pain, sir. Take a seat." The man with the finger in a bag of ice nods grimly.
- He slides a single plaster under the glass without a word.
- Seen to
- … hour… and … minutes on a plastic chair, then six minutes with a doctor who looked about twelve. Cleaned up, glued, strapped and told to drink some water. You feel a lot better.
- Back out there
- Desk sergeant
- The counter is closed. A phone on the wall: "For police assistance, lift the receiver."
- Ashby Police Station
- "Yes?" Sergeant Barraclough does not look up from his crossword.
- Ask for PC Hargreaves
- Bobby? He's in the canteen with a bacon roll. I wouldn't disturb him. Nobody disturbs him at his roll.
- He's out on patrol. Want me to radio him? No? Good.
- Report a crime
- Crime reference number... there. Don't hold your breath.
- Was anyone hurt? No? Then it's a form. Online. Next.
- Write it down on that pad. I'll... look at it.
- Lost property?
- Somebody handed in a lottery ticket. Nobody's claimed it. Go on then.
- One glove, a set of teeth and a ferret. None of them yours, I hope.
- You were in earlier. Nothing new.
- Bells go down at Ashby Fire Station: Pump 1 is turning out.
- On his break
- Do you mind? I'm on my break.
- You. What are you doing in here? Come to confess?
- I'm on my break.
- What now?
- A man is entitled to a bacon roll in peace.
- If you've come to hand yourself in, see the sergeant. I'm eating.
- I've got my eye on you. Even with my mouth full.

## Labels and names (4)

- btn sm primary · btn primary · bold 40px sans-serif · ind use

## States and modes

- role: drunk, bobby
- mode: home, out, scene, back, reverse
- type: triangle, lowpass
- phase: burn

## Tuning numbers (20)

- radius: 1.3, 1.1
- price: 1.5, 2.25, 1.2, .9, .95, 1.1
- vol: .15, .2, .25
- time: 4
- injury: 0
- emissiveIntensity: .15
- yaw: 0
- flash: 0
- top: 2.9, 3.1
- value: 0, 600, 1800
- spot: 0
- lane: -121.3
- want: 1, 0
- wait: 2.5
- dispatches: 0
- out: 0
- back: 0
- _sprayT: .12
- life: 1.4
- spread: .6
