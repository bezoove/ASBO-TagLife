# Fight (old game, v95)

Part of: Punches, kicks, shove, guard and fists up

Source in game.pretty.js: 104540-105458, 106753-106809, 106942-106948, 106950-106961, 109021-109024, 109715-109718, 109720-109727, 109729-109739, 115074-115080, 115936-115942, 116195-116204, 128406-128406, 128409-128409, 128431-128431, 128434-128434, 131332-131334, 136914-136914, 136926-136928, 136930-136936, 137206-137224, 137226-137230, 137232-137242, 137244-137247, 137249-137251, 138253-138261, 138263-138271, 143292-143302, 143365-143369, 144655-144661, 144663-144672, 150075-150081, 152249-152257, 167753-167778, 167780-167795, 175677-175684, 175686-175689, 181430-181439, 185980-185991, 196900-196912, 201026-201032 …

## Behaviours (55)

- wrap (`wrap`, line 104558)
- pos (`pos`, line 104593)
- all (`all`, line 104596)
- build (`build`, line 104601)
- new cb (`newCb`, line 104604)
- skill (`skill`, line 104645)
- dmg mul (`dmgMul`, line 104652, 109021)
- ensure (`ensure`, line 104661)
- reset (`reset`, line 104665, 137249)
- start (`start`, line 104676, 115074, 136930, 138253, 150075, 152249)
- drop drink (`dropDrink`, line 104719)
- spread (`spread`, line 104733)
- join in (`joinIn`, line 104771)
- leave fight (`leaveFight`, line 104777, 212036)
- separate (`separate`, line 104790, 144663)
- end session (`endSession`, line 104795)
- brenda out (`brendaOut`, line 104806, 136926)
- move for (`moveFor`, line 104815, 109715)
- attack (`attack`, line 104827, 109729, 196900)
- best target (`bestTarget`, line 104851, 213654)
- resolve (`resolve`, line 104873, 116195, 181430)
- in reach (`inReach`, line 104888)
- landed (`landed`, line 104893, 106753, 175677, 201026, 206489, 213571)
- struck (`struck`, line 104938)
- hurt player (`hurtPlayer`, line 104960)
- push (`push`, line 104979, 137206)
- stun (`stun`, line 104989)
- knockdown (`knockdown`, line 104993, 213685)
- knock out (`knockOut`, line 105009, 115936, 138263, 144655, 167780, 212053)
- player ko (`playerKO`, line 105019, 215623)
- drop weapon (`dropWeapon`, line 105042)
- grab (`grab`, line 105048)
- weapon near (`weaponNear`, line 105061, 175686)
- foe of (`foeOf`, line 105072)
- tick (`tick`, line 105078, 137226, 213676)
- hold weapon (`holdWeapon`, line 105122)
- update (`update`, line 105145, 106950)
- arm up (`armUp`, line 105161)
- step npc (`stepNpc`, line 105176)
- npc think (`npcThink`, line 105218, 212040)
- can act (`canAct`, line 105283, 137244)
- input (`input`, line 105287, 131332, 143292)
- press (`press`, line 105301, 109720)
- touch attack (`touchAttack`, line 105316)
- toggle stance (`toggleStance`, line 105319, 106942)
- player ctl (`playerCtl`, line 105323, 137232)
- nearest foe (`nearestFoe`, line 105340)
- cam assist (`camAssist`, line 105351)
- slow (`slow`, line 105365)
- law (`law`, line 105368)
- bark (`bark`, line 105389)
- hud init (`hudInit`, line 105399)
- hud show (`hudShow`, line 105420)
- hud update (`hudUpdate`, line 105423)
- touch state (`touchState`, line 105443, 143365)

## Everything it says or shows (15)

- You left them on the floor. Word will get around.
- You came out on top. People saw it.
- Brenda: "Not in my pub." Her trust in you takes a hit.
- Brenda: "I saw who started it. Thank you."
- Brenda: "That is it. OUT."
- Your guard is broken.
- Everything goes black.
- You come to. They have gone back to their pint, and your head is ringing.
- Brenda: "OI! Not in my pub! Pack it in!"
- Brenda: "THAT IS ENOUGH! All of you. Sit down!"
- Brenda: "Oi! Break it up!"
- … wades in on your side.
- UISafe fight
- Take the shotgun
- attacking somebody on the ground

## Labels and names (9)

- … wades in. · … legs it. · … is out cold. · fpl ft hidden · fcharge hidden · knocking somebody out · glassfx melee · gore landed
- gore shot

## States and modes

- state: gone, offstage, fight, idle, match
- mode: darts, pool, lift, cutscene, talk, sit, slump, idle, atk, stun, stand, down, getup, explore, title
- kind: ashtray

## Tuning numbers (55)

- heat: 0
- charge: 0
- _bark: 0
- _ht: 0
- _end: 0
- _camT: 0
- stam: 100, 0, 40
- dur: 1, .7
- guardT: 0
- bufT: 0, .35
- combo: 0
- comboT: 0, 1.3
- stunT: 0, 1.2, 1.3, 1, .9
- imm: 0, 1.4
- koT: 0, 1e9
- atkCd: .4
- strafe: 1
- strafeT: 0
- stamT: 0, .7
- stanceT: 0, 6, 8
- lunged: 0
- temper: .4
- freeze: 0
- until: 1e9
- pDowns: 0
- strikes: 0
- joined: 0
- drinkLeft: 0
- lookW: 1
- reach: 1.5, 1
- dmg: 3
- extra: 0, .15
- life: .5, .9
- spread: .3, .5, .4
- stun: .8, .7
- knock: .6, .5
- head: .6
- down: .25, .2
- wary: 240
- injury: 1.5
- loss: 0
- rest: 0
- roll: 0
- over: 0
- seqT: 1.1, 0
- speedMul: 1, 0, .3, .35, .5, .88
- str: .9
- freq: 140
- vol: .5, .45
- slide: -25
- breaks: 1
- cap: 3
- speed: 0
- radius: 1.2
- height: .4
