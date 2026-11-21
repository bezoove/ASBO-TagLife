# Homes (old game, v95)

Part of: Houses with people living in them

Source in game.pretty.js: 148066-148673, 148767-148767, 148769-148769, 148775-148775, 149278-149439, 149441-149448, 149450-149458

## Behaviours (40)

- house (`house`, line 148109)
- of (`of`, line 148112)
- c (`c`, line 148115)
- setup (`setup`, line 148118)
- settle (`settle`, line 148156)
- bed time (`bedTime`, line 148172)
- release (`release`, line 148177)
- pick spot (`pickSpot`, line 148180, 149278)
- place (`place`, line 148215, 149315)
- go (`go`, line 148224)
- approach (`approach`, line 148234)
- to bed (`toBed`, line 148240, 149323)
- un bed (`unBed`, line 148262, 149348)
- lights for (`lightsFor`, line 148269, 149354)
- go home (`goHome`, line 148274)
- home again (`homeAgain`, line 148289)
- plan (`plan`, line 148308)
- to pub (`toPub`, line 148336)
- think (`think`, line 148351)
- in mine (`inMine`, line 148376)
- watch (`watch`, line 148382, 149358)
- friendly (`friendly`, line 148407)
- welcome (`welcome`, line 148415)
- wake (`wake`, line 148422)
- alarm (`alarm`, line 148445)
- alarm tick (`alarmTick`, line 148483)
- took (`took`, line 148542)
- guard (`guard`, line 148565)
- picked (`picked`, line 148569)
- light tick (`lightTick`, line 148573, 149450)
- door tick (`doorTick`, line 148606)
- loot sync (`lootSync`, line 148627)
- show (`show`, line 148639)
- take (`take`, line 148651, 149441)
- switch light (`switchLight`, line 148664, 149435)
- labels (`labels`, line 148669)
- sleep pos (`sleepPos`, line 149387)
- heard at (`heardAt`, line 149401)
- sleep tick (`sleepTick`, line 149417)
- bump (`bump`, line 149428)

## Everything it says or shows (21)

- Oi! What are you doing in my house?!
- Who the hell are you? Get OUT!
- What d'you think you're playing at? This is my house!
- What's this? Who's there? I've got a stick and I know how to use it!
- Oh my God. Get out of my house! I'll scream!
- DENNIS! There's someone in the house!
- Right. You. Out. Before I ring the police.
- You're in MY house? You're dead, pal.
- Are you having a laugh? Get out of my house before I do you!
- Mate. Mate! This is my house, mate. Out.
- I'm ringing the police. I mean it!
- Go on. Get out!
- That's it, I'm calling the police!
- My window! Who did that?!
- OI! Who's chucking stuff at my window?!
- Hello? Is somebody there?
- … gets home and finds things missing. That'll be a call to PC Bobby.
- Took from a house
- … out of the ….
- Switch the light off
- Switch the light on

## Labels and names (2)

- …: "…" · breaking into a house

## States and modes

- state: idle, offstage, match, walk, gone
- mode: pub, stand, bed, toBed, home, walkHome, slump, sit, toPub, title
- kind: sofa

## Tuning numbers (27)

- reg: 1340, 420
- moira: 1400, 510
- terry: 1430, 555
- shaz: 1510, 570
- deano: 1530, 630
- pat: 1320, 405
- dennis: 1365, 435
- sus: 0, .99, .6, 1
- arriveAt: 5940
- leaveAt: 5940, 1e9
- _hcAcc: 0
- night: 1, 0, .04
- until: 1e12
- _ang: 0
- _expo: 1
- lookW: 1
- last: 0
- warned: 1, 2, 3
- out: 0
- _vanish: 0
- day: 1
- dur: .9
- _stErr: 1
- stir: 0, .4
- thr: 1
- rate: .6
- decay: .03
