# Bobby (old game, v95)

Part of: PC Hargreaves "Bobby"

Source in game.pretty.js: 114037-115006, 115152-115187, 115190-115196, 138346-138346, 138765-138767, 144967-144967, 145098-145101, 145467-145486, 145942-145942, 146214-146218, 151203-151205, 152611-152613, 152615-152617, 152699-152710, 172059-172059, 172081-172113, 172121-172121, 196585-196592, 196872-196874, 196876-196883

## Behaviours (58)

- active (`active`, line 114065)
- is private (`isPrivate`, line 114069, 151203)
- say (`say`, line 114073)
- bark (`bark`, line 114077)
- snap (`snap`, line 114081)
- p (`P`, line 114088)
- face (`face`, line 114091)
- look at (`lookAt`, line 114095)
- public near (`publicNear`, line 114099)
- go (`go`, line 114119)
- halt (`halt`, line 114129)
- route (`route`, line 114136)
- new night (`newNight`, line 114171)
- arrived (`arrived`, line 114174)
- update (`update`, line 114188, 115190, 145467, 146214, 152699, 172081)
- perceive (`perceive`, line 114234)
- watch acts (`watchActs`, line 114273)
- patrol (`patrol`, line 114302)
- greet (`greet`, line 114320)
- next step (`nextStep`, line 114328)
- run step (`runStep`, line 114350)
- begin (`begin`, line 114400)
- doing (`doing`, line 114412)
- end step (`endStep`, line 114449)
- warn (`warn`, line 114457)
- watching (`watching`, line 114472)
- start approach (`startApproach`, line 114479)
- approach (`approach`, line 114482)
- confront (`confront`, line 114512, 196872)
- resume (`resume`, line 114533, 145098)
- end (`end`, line 114540)
- options (`options`, line 114543)
- talk out (`talkOut`, line 114596)
- pay fine (`payFine`, line 114612)
- bribe (`bribe`, line 114623)
- search (`search`, line 114641)
- take in (`takeIn`, line 114675)
- after release (`afterRelease`, line 114743)
- new night keep (`newNightKeep`, line 114754)
- start chase (`startChase`, line 114757)
- whistle (`whistle`, line 114770)
- chase (`chase`, line 114796)
- stakeout (`stakeout`, line 114832)
- intel (`intel`, line 114841, 152615)
- find (`find`, line 114851)
- stand (`stand`, line 114860)
- begin seek (`beginSeek`, line 114863)
- pick search (`pickSearch`, line 114875)
- seek (`seek`, line 114900)
- ask around (`askAround`, line 114916)
- on heat (`onHeat`, line 114952)
- assaulted (`assaulted`, line 114961)
- hud (`hud`, line 114971, 196876)
- sync meter (`syncMeter`, line 115000)
- evict (`evict`, line 115152)
- evict update (`evictUpdate`, line 115167)
- on evicted (`onEvicted`, line 138765)
- summon (`summon`, line 152611)

## Everything it says or shows (42)

- Talk your way out of it
- Turn out your pockets
- Pay the fine on the spot (…)
- Slip him …
- Leg it
- PC Hargreaves gives you a long, level look. "...All right. I'll take your word. This time."
- "Hm. I'll let it lie. For now. Behave."
- He closes his notebook. "I'll be seeing you, {p}."
- "Nice try. Not buying it."
- "Pull the other one. It's got bells on."
- giving the police the runaround
- "Can't pay? Then you can explain yourself down the station."
- Fixed penalty
- … fixed penalty. PC Hargreaves writes you a receipt, tuts, and lets you go.
- A tenner for the policeman
- PC Hargreaves looks at the note for a long moment. It vanishes. "I never saw that. And I never saw you."
- attempting to bribe a police officer
- A night in the cells. They let you out when the pubs are long shut. Charge: ….
- Two hours on a plastic mattress, a cup of tea in a paper cup, and a lecture about …. You are let go at the front desk.
- Down the station
- Taken from you: 
- It is …. Brenda has heard. Her trust in you has taken a hit, and PC Hargreaves has your name in his book.
- Back to the pub
- I heard you were down the station. Not in my pub, you don't carry on like that.
- That one
- PC Hargreaves is after you
- PC Hargreaves is looking for you
- PC Hargreaves has his eye on you
- The law is taking an interest in you
- "Right. Now." He takes you firmly by the elbow.
- Right then. Time, ladies and gentlemen. That means you. Out we go.
- Come along now. Brenda's closing up, and so are you.
- Now then. The lady has asked nicely. I'm asking a bit less nicely.
- Mind how you go.
- Straight home, now.
- Nice and easy. That's it.
- Goodnight. Don't make me come back.
- Off you go. Goodnight.
-  By the time you are out the town is dark and the pub is shut.
- Taken from you: ….
- PC Hargreaves has your name in his book. Your record makes him a little less patient next time.
- Armed police

## Labels and names (9)

- picking a lock · picking a pocket ·  in cash · found with stolen goods ·  recovered as stolen · btn primary · running from the police · assaulting a police officer
- police respond

## States and modes

- state: gone, offstage, idle, walk
- mode: stand, title, darts, pool, lift, explore, lockpick, cutscene, parked, stolen

## Tuning numbers (35)

- loop: 0
- sus: 0, .5, 1
- seenT: 0
- dist: 99
- warnT: -999
- greetT: -999
- pubT: -999
- _tick: 0, .12
- _lostT: 0
- grace: 0
- _cd: 0
- dtMin: 0
- lookT: 1.2, 2.5
- until: 1e9
- speed: 0
- dur: 0, 3.2, 16, 14, 11, 1.6
- yaw: 0
- sub: 0, 1, 2
- lookW: 1
- vol: .05, .04, .12, .07
- _rp: 0, .45, .5
- value: 0
- cash: 0
- _whis: 0, 9
- _head: 1.5
- walkSpeed: 3.05, 1.12
- searched: 0
- tries: 0
- _pw: 2.2, 1, .4
- _askT: 1
- fill: 0, 1, .55, .3, .12
- awS: 0
- _offT: 2, 1e6
- mins: 0
- lvl: 3
