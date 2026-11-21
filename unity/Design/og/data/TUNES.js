// TUNES from the old game (v95), verbatim. Lines 115981-116031 of game.pretty.js
 const TUNES = [(L, v, c) => (voice(620, 1380, .85, {
  at: L,
  vol: v
 }), voice(1380, 620, .85, {
  at: L + .85,
  vol: v
 }), 1.7), (L, v, c) => (voice(985, 985, .3, {
  at: L,
  vol: v
 }), voice(740, 740, .3, {
  at: L + .32,
  vol: v
 }), .64), (L, v, c) => {
  if (c.step = (c.step || 0) % 2, c.step === 0) {
   for (let i = 0; i < 7; i++) voice(720, 1550, .17, {
    at: L + i * .19,
    vol: v
   });
   return c.step = 1, 1.4
  }
  for (let i = 0; i < 3; i++) voice(415, 415, .3, {
   at: L + i * .5,
   vol: v * 1.05,
   stack: [
    ["square", 1, 1],
    ["square", 1.012, .9],
    ["sawtooth", .5, .5]
   ]
  });
  return c.step = 0, 1.6
 }, (L, v, c) => {
  if (c.step = (c.step || 0) % 2, c.step === 0) {
   for (let i = 0; i < 12; i++) voice(i % 2 ? 1010 : 1450, i % 2 ? 1010 : 1450, .085, {
    at: L + i * .09,
    vol: v * .9,
    att: .005
   });
   return c.step = 1, 1.1
  }
  for (let i = 0; i < 4; i++) voice(360, 360, .16, {
   at: L + i * .22,
   vol: v * 1.1,
   att: .006,
   stack: [
    ["square", 1, 1],
    ["square", 1.015, .9],
    ["sawtooth", 2, .25]
   ]
  });
  return c.step = 0, .9
 }];
