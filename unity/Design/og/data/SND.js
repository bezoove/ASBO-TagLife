// SND from the old game (v95), verbatim. Lines 197559-197597, 203333-203336, 203378-203383, 203389-203392 of game.pretty.js
 const SND = {
   cashmere: {
    bark: () => S.bark(330),
    happy: () => {
     S.bark(360, .8)
    },
    growl: () => S.growl(150, .7),
    pant: () => S.pant(6),
    whine: () => S.whine(820),
    yelp: () => S.yelp(900),
    chomp: () => S.chomp()
   },
   chewie: {
    bark: () => S.yap(720),
    happy: () => S.bark(760, .6),
    growl: () => S.growl(240, 1),
    pant: () => S.pant(5),
    whine: () => S.whine(1150),
    yelp: () => S.yelp(1350),
    snap: () => {
     S.snap(), S.bark(800, .7)
    },
    chomp: () => S.chomp()
   },
   olly: {
    meow: () => S.meow(560 + Math.random() * 80),
    chirp: () => S.chirp(),
    purr: () => S.purr(2.4),
    purrShort: () => S.purr(1.2),
    hiss: () => Sfx.noiseBurst(.5, {
     freq: 4500,
     q: .7,
     type: "highpass",
     vol: .06,
     attack: .02
    }),
    chomp: () => S.chomp()
   }
  },
 const SND = {
  rain: null,
  hiss: null
 };
   SND.rain = bake(6, t => {
    const x = rn();
    l1 = (1 - a1) * x + a1 * l1, l2 = (1 - a2) * x + a2 * l2, l3 = (1 - a3) * x + a3 * l3, Math.random() < .012 && (drop = .5 + Math.random() * .8, dk = .9 + Math.random() * .07), drop *= dk;
    const sw = .8 + .2 * Math.sin(t * 1.3) * Math.sin(t * .7 + 1);
    return (l1 * 1.6 - l2 * .9) * sw + (l3 - l1) * .5 * drop
   }, .5)
   SND.hiss = bake(3, t => {
    const x = rn();
    return l1 = (1 - a1) * x + a1 * l1, l2 = (1 - a2) * x + a2 * l2, (l1 - l2) * (.85 + .15 * Math.sin(t * 9))
   }, .3)
