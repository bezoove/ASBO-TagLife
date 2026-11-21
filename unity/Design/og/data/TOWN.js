// TOWN from the old game (v95), verbatim. Lines 27923-28046 of game.pretty.js
const TOWN = {
 X0: 93,
 X1: 298,
 Z0: -142,
 Z1: 152,
 TX0: 90,
 TZ0: -150,
 TILE: 60,
 RW: 2.6,
 PW: 2,
 CULL: 115,
 CULL_D: 55,
 CULL_R: 90,
 roads: [{
  id: "high",
  name: "High Street",
  ax: "x",
  c: 26.5,
  a0: 92,
  a1: 290,
  e0: "open",
  e1: "j"
 }, {
  id: "ashby",
  name: "Ashby Road",
  ax: "z",
  c: 100,
  a0: -92,
  a1: 142,
  e0: "j",
  e1: "j"
 }, {
  id: "mill",
  name: "Mill Lane",
  ax: "z",
  c: 188,
  a0: -124,
  a1: 142,
  e0: "end",
  e1: "j"
 }, {
  id: "east",
  name: "Eastway",
  ax: "z",
  c: 290,
  a0: -92,
  a1: 142,
  e0: "j",
  e1: "j"
 }, {
  id: "canal",
  name: "Canal Road",
  ax: "x",
  c: -92,
  a0: 100,
  a1: 290,
  e0: "j",
  e1: "j"
 }, {
  id: "south",
  name: "Southgate",
  ax: "x",
  c: 142,
  a0: 100,
  a1: 290,
  e0: "j",
  e1: "j"
 }, {
  id: "ent",
  name: "Enterprise Way",
  ax: "x",
  c: -30,
  a0: 188,
  a1: 266,
  e0: "j",
  e1: "turn",
  rw: 3
 }, {
  id: "coro",
  name: "Coronation Avenue",
  ax: "x",
  c: 66,
  a0: 100,
  a1: 188,
  e0: "j",
  e1: "j",
  rw: 2.5
 }, {
  id: "elm",
  name: "Elm Close",
  ax: "z",
  c: 124,
  a0: 66,
  a1: 100,
  e0: "j",
  e1: "turn",
  rw: 2.4
 }, {
  id: "rowan",
  name: "Rowan Close",
  ax: "z",
  c: 166,
  a0: 66,
  a1: 100,
  e0: "j",
  e1: "turn",
  rw: 2.4
 }, {
  id: "ashl",
  name: "Ashgrove Lane",
  ax: "x",
  c: -30,
  a0: 100,
  a1: 150,
  e0: "j",
  e1: "end",
  rw: 2.5
 }],
 canal: {
  z0: -110,
  z1: -102,
  wy: -.75
 }
};
