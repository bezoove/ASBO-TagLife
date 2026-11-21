// LINES from the old game (v95), verbatim. Lines 100400-100595, 101678-101678, 114002-114036, 115187-115187, 131429-131450, 147330-147340, 150396-150417, 195228-195243, 197620-197632, 214682-214688 of game.pretty.js
const LINES = {
 _: {
  greetNew: ["Alright? Don't think I've seen you in here before.", "New face. You lost, or just thirsty?", "Evening. You're not from round here, are you?", "Who let you in? Only joking. Mostly."],
  hello: ["Alright.", "Y'alright?", "Evening.", "You again.", "Now then.", "What's occurring?"],
  helloFriend: ["Here they are!", "Alright, trouble.", "Oi oi! Pull up a stool.", "My favourite person in here. Don't tell anyone."],
  helloEnemy: ["What do you want?", "Oh. It's you.", "Don't start.", "Thought I smelled something."],
  spotted: ["Oi. What are you creeping about for?", "Why are you skulking?", "Can I help you? You're hovering.", "Stop lurking, will you?", "I can see you, you know."],
  suspicious: ["Hmm?", "Who's that?", "Thought I heard something.", "You after something?", "...Hello?"],
  caughtLift: ["Oi! That's my pocket you've got your hand in!", "Get your hands off me!", "Thief! THIEF!", "What do you think you're doing?!"],
  witness: ["Oi! He's got your wallet!", "Mate! Check your pockets!", "I saw that! I saw that!"],
  robbed: ["My wallet! Where's my wallet?!", "Hang on... I've been done!", "Someone's had my pockets!", "Oh, you're joking. It's gone!"],
  accuse: ["It was you. You were stood right behind me.", "You. I saw you hovering. Hand it over.", "I know it was you. Don't make this ugly."],
  bluffOk: ["...Hm. Maybe I dropped it. Sorry.", "Right. Sorry. Long day.", "Hm. Fair enough. Sorry."],
  bluffNo: ["Do I look stupid?", "Don't give me that.", "Liar."],
  returnOk: ["...Right. Lucky for you you've got a conscience.", "Hand it over. And don't do it again.", "I'll let it go. This once."],
  chat: ["Brenda's had the carpet in here since 1987. You can tell by the way your shoes stick.", "My missus thinks I'm at night school. Night school of life, mate.", "That jukebox has played the same four songs since Christmas.", "Saw a fox in the beer garden last week. Had more manners than half the people in here.", "They want to put a gastropub in on the high street. Small plates. Small plates! I want a big plate.", "Don't have the scampi. Trust me.", "Best thing about this place? Nobody asks questions.", "I came in here for one in 2009."],
  challengeYes: ["Go on then. Get your arrows out.", "Thought you'd never ask.", "Lovely. {g} it is.", "Yeah, go on. Don't cry when you lose."],
  challengeMaybe: ["Dunno... what's in it for me?", "I might. Convince me.", "Maybe. I've had a long day.", "Hmm. I don't really know you, do I?"],
  challengeNo: ["Not tonight.", "I'm alright, ta.", "Nah. Not feeling it.", "Ask me later. Or never."],
  noMoney: ["I'm skint, mate. Unless you fancy a friendly.", "Wallet's empty. Spent it all on that bloody machine."],
  tooGood: ["I've seen you throw. I'm not a mug.", "Word gets round, you know. You're a shark.", "Play you? I'd rather set fire to my own money."],
  drinkThanks: ["Cheers! You're a gent.", "Oh, go on then. Ta very much.", "Now that's how you make friends round here.", "Lovely stuff. I'll get the next one. Probably."],
  drinkFull: ["I've already got one on the go, but I'll take it.", "Two-fisting it. Don't tell Brenda."],
  win: ["Better luck next time.", "Pay up, then.", "Unlucky, pal. Not.", "Should've stayed at home."],
  lose: ["Fair play. Well thrown.", "You jammy so-and-so.", "Yeah, alright, you win. Don't go on about it.", "Here's your money. Choke on it."],
  loseSore: ["That was luck, that.", "I want a rematch. Now.", "You've been practising, haven't you? Sneaky."],
  drunk: ["I love you, mate. I mean it. I love this pub.", "Is it me or is the floor moving?", "I'm not drunk. You're drunk. Everyone's drunk.", "Shhh. Shhh. Listen. ...No, forgot it."],
  flirtOk: ["Ha! Smooth. Keep going.", "Behave. ...Actually, don't.", "You're trouble, you are.", "Buy me another and we'll see."],
  flirtNo: ["Not a chance.", "Did that line work on anyone, ever?", "Easy, tiger.", "I'll pretend you didn't say that."],
  giftLove: ["For me? Oh, you shouldn't have! Seriously, nobody ever gets me anything.", "That's perfect. How did you know?"],
  giftOk: ["Ta. That's nice of you.", "Oh. Right. Thanks."],
  giftHate: ["What am I supposed to do with that?", "Is this a joke?"],
  bye: ["See you.", "Ta-ra.", "Mind how you go.", "Don't be a stranger."],
  busy: ["Not now, I'm in the middle of something."],
  owed: ["I owe you one. Remember that."],
  refuseFight: ["Leave it. Not worth it."],
  persuade: {
   drink: {
    ok: ["Well, if you're buying... go on then.", "A free pint? I'm in."],
    no: ["I can buy my own drinks, ta."]
   },
   stakes: {
    ok: ["Now you're talking my language.", "Double it? Go on, you're on."],
    no: ["That's too rich for me."]
   },
   taunt: {
    ok: ["Scared? Of you? Right, get up there.", "Oh, it's like that, is it? You're on."],
    no: ["Nice try. I'm not rising to it.", "Call me what you want. Still no."]
   },
   friendly: {
    ok: ["Just for fun? Yeah, alright.", "No money? Fine. Bragging rights only."],
    no: ["What's the point of that?"]
   },
   handicap: {
    ok: ["A head start? Now we're talking.", "Generous. Daft, but generous."],
    no: ["I don't need your charity."]
   },
   mates: {
    ok: ["Go on, because it's you.", "Alright, alright. For you."],
    no: ["We're not that close, mate."]
   },
   flirt: {
    ok: ["Oh, go on. Just because you asked nicely.", "When you put it like that..."],
    no: ["That's not going to work on me."]
   },
   favour: {
    ok: ["Fair enough. I owe you. Let's play."]
   },
   bribe: {
    ok: ["Paid to play darts? Easiest money I'll make all week.", "A tenner just to throw? Done."],
    no: ["I'm not a performing seal."]
   }
  },
  rumours: []
 },
 brenda: {
  spotted: ["You're skulking, love. Stop it. You'll put the customers off.", "I can see you creeping about. Cut it out."],
  caughtLift: ["NOT in my pub!", "Are you robbing my customers?! In MY pub?!"],
  greetNew: ["New face. I'm Brenda, I run this place. Pay for your drinks, mind your language, and don't bring trouble in here and we'll get on fine."],
  hello: ["What can I get you, love?", "Evening, love.", "You behaving yourself?"],
  helloFriend: ["There's my favourite customer. Don't let it go to your head.", "Hello, darling. Usual?"],
  chat: ["Twenty-eight years I've had this pub. Bill used to say I'd outlive the carpet. He was right.", "If you see Mickey's lot causing bother, you tell me. Not him. Me.", "Thursday's darts night. Proper competition. Ten pound entry.", "That Terry. Heart of gold, liver of a man twice his age.", "Don't feed that machine. It's got more of my regulars' wages than the bookies.", "I've barred better people than you, love. Remember that."],
  challengeNo: ["I'm working, love. Ask me after closing. Actually, don't.", "I don't play for money. Never have."],
  challengeYes: ["Go on then, one game. Loser buys the round. Don't tell anyone I'm soft."],
  busy: ["One minute, love, I'm serving."]
 },
 callum: {
  greetNew: ["Alright? I'm Callum. I'm on the bar Thursday to Sunday. If Brenda shouts, I'm changing a barrel."],
  hello: ["What'll it be?", "Alright, you.", "Look who it is."],
  chat: ["I'm saving up to open a cocktail bar. Brenda says I'm wasted pulling pints. She also says I'm wasted generally.", "You get to know people, working a bar. Everyone tells you everything after their fourth.", "Kev tips in fifty pence pieces. Every time. Like it's a gesture.", "I clock off at eleven. After that I'm a free man.", "Deano's barred from two other pubs. We're the last resort."],
  challengeNo: ["Not while I'm on shift. Brenda would have my head on a pump."],
  flirtOk: ["Careful, I might start pouring you bigger measures.", "You trying to get a free drink out of me? ...Is it working? Maybe."]
 },
 terry: {
  greetNew: ["Terry. Terry Bassett. They call me Two Pints because I always have... well. You'll see. What are you drinking? You're buying? Lovely."],
  hello: ["Alright, mate! Alright!", "Here he is! Or she is! Whoever you are!", "Mate. Mate. Have I told you about my knee?"],
  chat: ["I used to be a proper darts player, me. Then I discovered drinking. Then I discovered drinking more.", "Don't tell Mickey I'm in. I owe him a bit. A bit a lot.", "Friday's payday. Fridays are beautiful.", "My ex-wife got the house. I got the darts. I think I won, to be honest.", "Saw a man in here once drink a pint out of a boot. Was me. It was me."],
  challengeYes: ["A game? Mate, I've been waiting all night for someone to ask!", "Darts? Yes. Absolutely. What are we playing for? A pint? Two pints?"],
  drinkThanks: ["You beautiful human being.", "Mate. Mate. You're the best mate I've ever had. Since Gary. Gary was great."],
  win: ["I WON! Did everyone see that? Brenda! I won!"],
  lose: ["Ah, that's darts. That's darts, innit.", "Fair play. You're better than me. Most people are."]
 },
 shaz: {
  greetNew: ["Sharon. Shaz. Don't call me Sharon. If you're here for the darts, you're in the right pub. If you're here for Deano, you're in the wrong one."],
  hello: ["Alright?", "Oh, it's you. Good.", "Back for more?"],
  helloFriend: ["There they are. Was hoping you'd be in.", "Alright, gorgeous. Kidding. Maybe."],
  chat: ["I've been throwing since I was twelve. My dad had a board in the kitchen. Mum hated it.", "Deano and me went out for two years. Biggest mistake since the perm.", "Dave's good, but he gets twitchy on sixteens. Watch him.", "Don't let Kev wind you up. He's all tracksuit and no trousers.", "I'm only in here for the darts. And the company. Some of it."],
  challengeMaybe: ["I don't play strangers for money. Friendly, if you want."],
  tooGood: ["Oh, you're good now? Let's see it then. Friendly first."],
  win: ["Don't feel bad. I'm better than most of the men in here."],
  lose: ["Well thrown. Seriously. I'm impressed. Don't let it go to your head."],
  flirtOk: ["Ha! Go on then, keep talking.", "You've got a nerve. I like that."],
  flirtNo: ["Buy me a drink first, at least.", "Behave. I've had enough of smooth talkers."]
 },
 deano: {
  greetNew: ["What you looking at? ...Deano. Don't wear it out."],
  hello: ["What?", "Alright.", "You again."],
  helloEnemy: ["You've got some front coming over here.", "Walk away while you still can."],
  chat: ["I do eight hundred press-ups a day. Sometimes nine.", "Shaz'll come round. She always does.", "If anyone touches my pint, I'll know.", "I was nearly a semi-pro footballer. Nearly."],
  challengeYes: ["Darts? I'll destroy you. Twenty quid says so.", "Go on then. Hope you brought your wallet."],
  lose: ["That was a fluke. Do it again. Go on.", "You got lucky, that's all."],
  win: ["Told you. Told everyone."]
 },
 reg: {
  greetNew: ["Reg Whitlow. County champion, 1986 and 1987. You won't find my name on the internet but it's on that board over there. Sit down, sit down."],
  hello: ["Hello, young'un.", "Ah. Sit yourself down.", "Evening. Mind my stick."],
  chat: ["Darts is all in the elbow. Keep it still. Let the forearm do the work.", "In my day the board was elm, and you soaked it in water overnight. Smelled like a pond.", "Rhythm. That's the secret. Same speed every time.", "Don't go chasing trebles when you're on a finish. Take what the board gives you.", "The Duke? Lionel Fairweather. I beat him once. 1991. He's never forgiven me.", "Leave the bull alone unless you need it. It's a small target and a big ego."],
  challengeYes: ["For a pint? Go on, I'll give you a lesson.", "One game. I tire easy these days, mind."],
  win: ["Still got it. Just about.", "Elbow, son. Keep the elbow still."],
  lose: ["Well thrown. Proper well thrown. You've got something."]
 },
 kev: {
  greetNew: ["Kev. Flash Kev. You might've heard of me. You definitely will."],
  hello: ["Alright, loser.", "Oi oi.", "Come to watch a master?"],
  chat: ["These darts cost more than your car. Ninety-seven percent tungsten, mate.", "I'm going pro next year. Probably.", "My dad owns three car washes. I mean, I'll own three car washes.", "Dave's past it. Everyone knows it but him.", "The Duke's a legend, but legends get old."],
  challengeYes: ["You want some of this? Fifty quid, minimum.", "Sure. I need a warm-up."],
  lose: ["Whatever. I wasn't trying.", "Double or nothing. Now. Unless you're scared."],
  win: ["Easy. Too easy. Next."]
 },
 priya: {
  greetNew: ["Hi. Priya. I design darts, sort of. Barrels, grips, balance. If you want to throw better, half of it's equipment. The other half's you, sorry."],
  hello: ["Oh, hey.", "Hi! Good timing.", "Hello. Want to talk about grip patterns? Joke. Unless?"],
  chat: ["Front-loaded barrels are more forgiving if your release is a bit sloppy.", "Tungsten's denser, so the barrel's thinner, so you can group tighter. Physics.", "I don't really drink. I come for the darts and stay for the... no, just the darts.", "Ron sells 'tungsten' darts that are basically painted brass. Don't.", "Shark-fin grips are brilliant if your fingers sweat. Sorry. Too much information."],
  challengeYes: ["Sure, small stakes. I like collecting data.", "Okay! Fair warning, I'm better than I look."],
  win: ["Ha! Sorry. Not sorry."],
  lose: ["Interesting. Your release is really consistent. Annoyingly."]
 },
 mickey: {
  caughtLift: ["Sunshine. You have just made a very expensive mistake.", "My pocket. You're in my pocket."],
  greetNew: ["Michael Doyle. My friends call me Mickey. My enemies call me Mr Doyle, eventually. You owe me money, don't you? I never forget a face. Or a debt."],
  hello: ["Ah. My favourite investment.", "Come in. Close the door.", "Talk to me."],
  chat: ["Everybody in this pub owes somebody something. I just make sure the somebody is me.", "Brenda doesn't like me. Brenda doesn't have to like me. Brenda has to like her licence.", "I don't play darts. I own the people who play darts.", "Terry thinks I don't know he's in. I always know."],
  challengeNo: ["I don't play. I bet. Talk to Tank if you want a game in here."]
 },
 tank: {
  caughtLift: ["...", "Hands."],
  spotted: ["...", "Step back."],
  greetNew: ["..."],
  hello: ["...", "Mm.", "Mickey's busy.", "What."],
  chat: ["...I like darts.", "My nan taught me. She was hard as nails.", "Mickey says I'm not allowed to play people out there. Scares them.", "...Nice shoes."],
  challengeNo: ["Mickey says no.", "Not without Mickey's say-so."],
  challengeYes: ["...Okay. Mickey says it's fine."]
 },
 moira: {
  greetNew: ["Moira, love. Don't stand there, you're blocking my luck. That machine's been eating money all night. It's due. I can feel it in my waters."],
  hello: ["Hello, love!", "Ooh, it's you! Got a pound?", "Not now, love, I'm on a nudge."],
  chat: ["I won a hundred pound on that machine in 2014. Been chasing it ever since.", "Don't hold the cherries. Never hold the cherries.", "My second husband was a darts player. Couldn't hit a barn door. Still can't, he's dead.", "When nobody's won for ages, that's when it pays. That's the secret."],
  challengeNo: ["I don't do arrows, love. That machine's my sport."]
 },
 ron: {
  greetNew: ["Ron. Entrepreneur. Whatever you need, I can get. Watches, perfume, a ride to the airport, no questions. And I take bets on the gee-gees, if you're that way inclined."],
  hello: ["What can I do you for?", "My friend! My friend.", "Psst. Come here."],
  chat: ["Supply and demand. People demand, I supply. Simple.", "Everything's genuine. Genuinely something.", "I'd stay away from Mickey's back room if I were you. Unless you like owing people.", "Fancy a flutter? Next race is on the telly."],
  challengeYes: ["Darts? Oh, I'm terrible. Terrible. Tenner?"],
  lose: ["Ah, you got me. Fancy another? Make it more interesting?"],
  win: ["Beginner's luck. Mine, I mean."]
 },
 dave: {
  greetNew: ["Dave. People call me The Anvil. Club champion, five times. If you want a game, you'd better have won something first."],
  hello: ["Alright.", "Evening.", "Now then."],
  chat: ["Darts is simple. Hit the twenty. Then hit it again.", "Reg taught me everything. Don't tell him I said that.", "The Duke's the only man who's beaten me in this pub in five years. Smug sod.", "My brother's got himself in with Mickey. Idiot.", "I've thrown nine darts at that board more times than I've had hot dinners."],
  challengeNo: ["Come back when you've won something.", "You're not ready, son."],
  challengeYes: ["Right. Let's see what you've got. Proper money, mind."],
  lose: ["Well thrown. Didn't think you had it in you.", "...Good darts. I'll want that back."],
  win: ["Keep practising. You'll get there."]
 },
 duke: {
  greetNew: ["Lionel Fairweather. People call me The Duke, which is ridiculous, and which I have never once discouraged. You look like someone with ambitions."],
  hello: ["Good evening.", "Ah. The ambitious one.", "Do sit."],
  chat: ["I've never had a proper job. I've had a proper dart.", "Reginald beat me in 1991. I think about it most days.", "The secret to hustling is to lose beautifully until the stakes are worth winning.", "This pub is the last honest dishonest place in England."],
  challengeNo: ["I don't play nobodies, I'm afraid. Make yourself a somebody.", "Come back on a Saturday with a reputation and a thick wallet."],
  challengeYes: ["Very well. Let's make it interesting, shall we?"],
  lose: ["...Extraordinary. Well thrown. I mean that."],
  win: ["Don't feel bad. Everyone loses to me eventually."]
 }
};
}), LINES.brenda && (LINES.brenda.weeSee = ["RIGHT. That is ENOUGH.", "NOT in my pub, you animal!", "Toilets. Are. THROUGH. THERE."]);
}, LINES.bobby = {
 greetNew: ["Evening. PC Hargreaves. Everyone calls me Bobby, which I've given up fighting. Quiet little town. I'd like to keep it that way."],
 hello: ["Evening.", "Now then.", "All quiet, so far.", "Evening, {p}. Behaving?"],
 helloFriend: ["Evening, {p}! Good to see a friendly face.", "Now then, {p}. Keeping out of trouble?"],
 helloEnemy: ["Ah. {p}. I was hoping our paths would cross.", "Hello, hello. Hands where I can see them, eh?"],
 chat: ["Twenty-six years on this beat. Never had my helmet nicked. Well. Not since 1998.", "I like the quiet nights. Quiet nights mean the pub's doing its job.", "People think the job's all chases. It's mostly doors. Trying doors. You'd be amazed what's left unlocked.", "My wife says I should retire. My feet agree with her. My conscience doesn't."],
 bye: ["Mind how you go.", "Keep it tidy.", "Goodnight, {p}. Straight home, mind."],
 giftLove: ["Well. That's very kind. Quite improper, of course, but kind.", "For me? ...Is this a bribe? No. No, I can see it isn't. Thank you."],
 giftOk: ["Thank you. Very decent of you."],
 giftHate: ["I'll pretend I never saw that. Take it back."],
 challengeNo: ["Not on duty, I'm afraid.", "Darts? I'm on the clock, {p}."],
 warnQuiet: ["Now then, {p}. I'm keeping an eye on you.", "Evening, {p}. I'd steady on, if I were you.", "I've had my eye on you tonight. Don't give me a reason.", "Word gets round, {p}. I'd keep my hands in my pockets."],
 warnDrunk: ["Bit unsteady, aren't we? I'd think about home, if I were you.", "Steady on, {p}. Don't make me come over there."],
 halt: ["Oi! You there. Stop right there.", "Hello, hello, hello. What's all this, then?", "Just a moment, please."],
 question: ["Evening, {p}. A word. I've had a report of {crime}. Mind if I have a look in your pockets?", "Now then. What's all this about {crime}? Care to turn out your pockets?", "I won't keep you. Someone mentioned {crime}. Pockets, please."],
 clean: ["Hm. All in order. Mind how you go, then.", "Nothing there. Sorry to trouble you. Off you go.", "Well. That's that. Don't let me catch you at it."],
 found: ["Right. And where did all this come from?", "Well, well. I'll have those. And you'll be paying a fine."],
 arrest: ["I'm arresting you on suspicion of {crime}. You do not have to say anything...", "That's quite enough. {p}, I'm arresting you. You'll be coming with me to the station.", "Right. You're nicked. Don't make this harder than it needs to be."],
 caught: ["Got you. Right, that's enough running about.", "Not so fast. Not so fast at all."],
 seekGo: ["Right. Let's have a look round.", "Somebody's reported something. I'll take a look.", "On my way."],
 seekGone: ["Hm. Gone. I'll be about, mind.", "Lost him. Never mind. Night's young.", "I'll be keeping my eyes open."],
 ask: ["Excuse me. Seen {p} about?", "Have you seen someone come this way?", "Anyone come past in a hurry?"],
 pointed: ["Over there.", "That way. Went that way.", "{p}'s right there."],
 shrugged: ["Haven't seen anyone, officer.", "Not me.", "Sorry, no."],
 brib: ["Is that a bribe? Put it away. Right now.", "I'll forget I saw that. Now put it away."],
 bribeFail: ["Attempting to bribe a police officer. That's an offence, you know.", "That's an offence in itself. Right."],
 rajHello: ["Evening, Raj. Everything in order?", "Raj. Quiet night?"],
 rajReply: ["Quiet as the grave, Bobby. Shall I put the kettle on?", "Bobby! Tea? It's not a bribe. It's tea.", "All quiet. One man bought forty fireworks, but he seemed sensible."],
 brendaHello: ["Evening all.", "Evening, Brenda. Just having a look round.", "Evening all. Don't mind me."],
 brendaReply: ["Evening, Bobby. Orange squash?", "Hello, Bobby. Sit down, you're making the place look untidy.", "Evening, Bobby. Nothing to see here. Mostly."],
 windows: ["Evening all.", "Hm.", "Lights on. Good."],
 door: ["Locked. Good.", "Hm.", "Secure."],
 car: ["Hm.", "Tax's up to date, anyway.", "Nothing amiss."],
 drink: ["Just an orange juice, Brenda. Off duty. Nearly."]
};
}, LINES.bobby.evict = ["Right then. Time, ladies and gentlemen. That means you. Out we go.", "Come along now. Brenda's closing up, and so are you.", "Now then. The lady has asked nicely. I'm asking a bit less nicely."], LINES.bobby.evictOut = ["Mind how you go.", "Straight home, now.", "Nice and easy. That's it."], LINES.bobby.evictBye = ["Goodnight. Don't make me come back.", "Off you go. Goodnight."];
}, LINES.raj = {
 greetNew: ["Evening, evening! New face. I'm Raj, this is my shop, and I know everything that happens on this road. Everything. What can I get you?"],
 hello: ["Evening, my friend!", "Ah! You again. Good, good.", "Come in, come in. Mind the step.", "What's the news from across the road?"],
 helloFriend: ["My favourite customer! Don't tell the others.", "Here he is! Come in, come in."],
 helloEnemy: ["Oh. You. Don't touch anything.", "I'm watching you. The camera is watching you. The camera doesn't work, but still."],
 chat: ["Brenda came in for milk once. 1998. Bought a scratchcard. Won twenty quid. Never came back. I think about it a lot.", "Terry owes me four pounds sixty for a pasty. Since March. I'm not angry. I'm keeping a list.", "Mickey's lot buy the expensive cigarettes. Cash. Always cash. I ask no questions. I have a lot of questions.", "I sell more fireworks on a Tuesday than you'd believe. I don't ask why. Nobody asks why.", "My wife says I gossip. I don't gossip. I report.", "Had a fox in the stockroom last week. Ate a whole pasty. Better than most of my customers, that fox.", "You know what sells? Eggs. Not for eating, I think."],
 bye: ["Mind how you go. Mind the step.", "Come back soon. I'll be here. I'm always here.", "Ta-ra! Say hello to Brenda. She won't say it back."],
 giftLove: ["For me? Oh, you're a good one. A good one.", "Well, now! Nobody brings the shopkeeper anything."],
 giftOk: ["Very kind. Very kind.", "Oh. Ta."],
 giftHate: ["Hmm. Is this stolen? It looks stolen.", "No. No thank you. Keep it."],
 challengeNo: ["I can't leave the shop! Who'd mind the shop?", "I don't play darts. I sell darts. Different thing."],
 shop: {
  open: ["Have a look round. Everything's priced. Mostly.", "Knives, fireworks, pasties. A full life, in one shop.", "What'll it be? No rush. Well, a bit of rush.", "Shout if you want anything from the top shelf.", "Cash is king. Cards are also fine. Cash is king."],
  buy: ["Lovely. Anything else?", "There you go. Mind how you go with that.", "Good choice. Well, a choice.", "Ta very much. Say hello to Brenda.", "And that's... yes. That's right. Lovely."],
  buyWeapon: ["For... opening parcels. Yes. Of course.", "I didn't see you buy that. You didn't buy that.", "Don't bring it back in here. I mean that."],
  buyFw: ["Outside. OUTSIDE. Not in the pub. I've seen what happens.", "Stand well back. Read the box. Nobody reads the box.", "Light the blue touch paper and run, my friend."],
  buyBooze: ["Drink it somewhere I can't see you. Brenda hates me selling these.", "Careful with that, you've got a pub right there."],
  drunk: ["No. Look at you. Come back when you can see straight.", "Not to you, not tonight. I've got a licence to think about.", "You're swaying, my friend. No knives, no fireworks, no vodka. Pasty?", "Absolutely not. You can barely stand. Have a bottle of water."],
  skint: ["Your money's no good here. Because you haven't got any.", "Come back with some money. I take money.", "A little short, I think. Sell something to Ron."],
  bye: ["Come again! Mind the step.", "Ta-ra. Don't let the fireworks off in the pub.", "Cheerio. I'll tell Brenda you said hello."]
 }
};
}, typeof LINES < "u" && (LINES.pat = {
 greetNew: ["Oh! Hello, love. Are you the new one? I'm Pat. Number twelve. Mind the step."],
 hello: ["Hello, love.", "Ooh, it's you again.", "Evening, dear."],
 chat: ["Dennis has had that chair since 1979. I've stopped asking.", "They want twelve pound for a bag of coal now. Twelve!", "I don't go in that pub. Brenda's mother and me fell out in 1986."],
 bye: ["Ta-ra, love.", "Mind how you go."]
}, LINES.dennis = {
 greetNew: ["Dennis. Number twelve. Don't park there."],
 hello: ["Hm.", "Evening.", "You again."],
 chat: ["Forty years at the gasworks. Not one day off.", "That lot over the road. Noise till all hours.", "Don't talk to me while the snooker's on."],
 bye: ["Right.", "Shut the door on your way."]
}), Object.assign(ITEMS, {
}, LINES.stan = {
 greetNew: ["Stan. Stan's Motors. MOT, service, tyres, and whatever you did to that wheel. What can I do you for?", "Don't lean on the counter, it's wet. It's always wet. Stan. Pleased to meet you."],
 hello: ["Alright. Mind the oil.", "Go on, then. What's it doing? Noise? Smoke? Smell?", "Don't touch anything. Everything's hot or sharp.", "Morning. Or afternoon. I lose track under a car."],
 helloFriend: ["Here he is! Kettle's on. Don't sit on the Astra.", "Alright {p}! Want a brew?"],
 helloEnemy: ["Oh. You. I've got a wheel brace, you know.", "Keep your hands where I can see them."],
 chat: ["Everyone thinks a garage is about cars. It's about people lying to me about when they last had the oil changed.", "The pit's older than the council. Don't fall in. Terry did. Twice.", "I can tell what's wrong from the noise. People think it's magic. It's mostly guessing, loudly.", "That Cavalier in the yard? Burnt out at the pub in '09. Kept the radio. Kept the radio and nothing else.", "MOT's a cheap day out for the car. Not for the owner. Never for the owner.", "Do I do the pub's cars? No. The pub's cars do themselves, mostly onto the verge."],
 bye: ["Right. Cheers. Mind the shutter.", "Mind how you go. And don't touch my ramp.", "See you. Bring me a pasty."],
 giftLove: ["Well, now. That's nice. That's very nice. Thanks."],
 giftOk: ["Ta. Cheers."],
 giftHate: ["What am I meant to do with that? I fix cars."],
 challengeNo: ["Darts? I've got a customer. I mean I haven't. But I've got a car on the ramp."],
 bark: ["Pass us that spanner. No, the other one.", "Twelve-millimetre. Where's the twelve-millimetre?", "That'll need a new gasket. They always need a new gasket.", "Oi. Mind the pit.", "Kettle's gone cold again.", "Ah, give over. Cross-threaded.", "Radio's on the blink and so am I."],
 parts: {
  open: ["What d'you need? Plugs, leads, oil. The good stuff's behind the counter.", "Have a look. Everything has a price on it. Mostly."],
  buy: ["There you go. Mind the oil.", "Good choice. Cheap, too, for once."],
  skint: ["That'll be more than you've got."],
  sellOk: ["Scrap's scrap. That's a fair price.", "Hm. Fine. Money's money."],
  sellNone: ["You've nothing I want."]
 },
 repair: ["Right, I'll have a look at the cars outside. Give me a minute with the T-cut and a bit of filler.", "Smashed glass, cracks, crazing... I've seen worse. Not often."],
 closed: ["Shut. Come back tomorrow."]
};
})(), LINES.swat = {
 armed: ["ARMED POLICE! ARMED POLICE!", "ARMED POLICE! DON'T MOVE!", "ARMED POLICE! STAND STILL!"],
 hands: ["SHOW ME YOUR HANDS!", "HANDS! LET ME SEE YOUR HANDS!", "HANDS WHERE I CAN SEE THEM!"],
 ground: ["GET ON THE GROUND! NOW!", "GET DOWN! GET DOWN ON THE GROUND!", "DOWN ON THE FLOOR! NOW!"],
 still: ["DO NOT MOVE!", "STAY WHERE YOU ARE!", "STAND STILL!"],
 stop: ["STOP! ARMED POLICE!", "STOP RIGHT THERE!", "STOP OR YOU WILL BE TASERED!"],
 back: ["STAY BACK!", "BACK OFF! NOW!", "GET BACK!"],
 taser: ["TASER! TASER! TASER!"],
 baton: ["BATON GUN! STOP!", "STOP! BATON GUN!"],
 car: ["ARMED POLICE! ENGINE OFF!", "HANDS ON THE WHEEL!", "GET OUT OF THE VEHICLE! NOW!", "STEP OUT OF THE VEHICLE!"],
 bike: ["GET OFF THE BIKE!", "OFF THE BIKE! NOW!"],
 out: ["ARMED POLICE! COME OUT WITH YOUR HANDS UP!", "COME OUT! HANDS FIRST!", "WE KNOW YOU'RE IN THERE! COME OUT SLOWLY!"],
 comply: ["KEEP YOUR HANDS UP!", "HANDS UP. DON'T MOVE.", "STAY THERE! KEEP STILL!"],
 cuff: ["You're under arrest. You do not have to say anything.", "Hands behind your back. Now.", "Stay down. Cuffing."],
 lost: ["Suspect lost. Standing down.", "Lost visual. Back to the van."]
};
  typeof LINES > "u" || (LINES._.pets = {
   cashmere: ["Who's a good boy? You are, Cashmere!", "Go on then, Cashmere. Just one.", "No, you're not having my scratchings.", "Look at them eyes. He knows what he's doing.", "He's softer than a cushion, this one.", "Cashmere! Where's your ball, eh?"],
   cashmereFeed: ["Don't you be feeding him, he'll be sick on my carpet.", "He's had three packets off the regulars already.", "Spoilt rotten, that dog."],
   cashmereNibble: ["Mind your fingers! He nibbles. Never hurt nobody though.", "That's him saying hello. Soppy thing."],
   olly: ["That cat's caught more mice than the council ever has.", "Olly, get down off there.", "He's hunting again. Look at him go."],
   present: ["Olly! Not on my floor!", "Oh, he's brought you a present. You're honoured.", "Lovely. Another mouse. Somebody get the dustpan."],
   chewieBite: ["Told you. Don't touch Chewie.", "He does that. Everyone gets bit once.", "Little sod, isn't he? Lovely till you touch him."],
   chewie: ["That's Chewie. He's nobody's and everybody's.", "Don't stroke him, he'll have your fingers."]
  }, LINES.brenda && (LINES.brenda.pets = Object.assign({}, LINES.brenda.pets || {}, {
   cashmereFeed: ["Oi! Don't you be feeding him crisps, he'll be sick on my carpet.", "He's on a diet. Allegedly.", "Go on, but that's his last one tonight."],
   present: ["Olly! Not by my bar!", "Oh, for... Olly! Who's picking that up? Not me.", "He thinks he's paying his rent, that cat."],
   cashmereNibble: ["He's only playing, love. He nibbles. Never broke skin in his life."]
  })), typeof Locale < "u" && Locale.POOLS && Object.assign(Locale.POOLS, {
 if (typeof LINES < "u" && (LINES.medic = {
   mArrive: ["Ambulance! Let us through, please.", "Right, let's have a look at you.", "Mind your backs, coming through."],
   mTreat: ["Can you hear me? Squeeze my hand.", "Stay with me, mate. Stay with me.", "Pressure on that. Harder.", "Pulse is weak. Get a line in.", "You're alright, we've got you."],
   mDead: ["No output. Nothing we can do.", "I'm calling it. Get the sheet.", "He's gone. Police are on their way."],
   mLift: ["On three. One, two, three.", "Mind the head.", "Lift."],
   mBack: ["Give us some room, please.", "Stand back, please."]
  }), typeof Locale < "u" && Locale.POOLS)
