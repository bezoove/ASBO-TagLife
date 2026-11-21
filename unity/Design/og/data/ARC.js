// ARC from the old game (v95), verbatim. Lines 56476-56537 of game.pretty.js
const ARC = {
 X: [234.6, 240.7, 244.7, 250.8],
 Z: [4.6, 8.87, 13.13, 17.4],
 H: 4.2,
 HW: 4.6,
 UNITS: [{
  id: "poundshop",
  name: "Everything £1",
  side: -1,
  band: 2,
  hours: [8, 20],
  bg: "#ffd400",
  fg: "#c8102e",
  sub: "Everything a pound · or less · or a bit more"
 }, {
  id: "soletrader",
  name: "Sole Trader",
  side: 1,
  band: 2,
  hours: [9, 19],
  bg: "#111111",
  fg: "#ffffff",
  sub: "Trainers · Sportswear · Footy shirts"
 }, {
  id: "threadz",
  name: "Threadz",
  side: -1,
  band: 1,
  hours: [9, 19],
  bg: "#2a2a2e",
  fg: "#f2f0ea",
  sub: "Fashion"
 }, {
  id: "glint",
  name: "Glint",
  side: 1,
  band: 1,
  hours: [9.5, 18.5],
  bg: "#5a2a6a",
  fg: "#f0d070",
  sub: "Hats · Jewellery · Sunglasses"
 }, {
  id: "maison",
  name: "Maison Ashby",
  side: -1,
  band: 0,
  hours: [10, 18],
  bg: "#1d2a3a",
  fg: "#e8d8b0",
  sub: "Gentlemen & Ladies · Est. 2019"
 }, {
  id: "ostentatia",
  name: "OSTENTATIA",
  side: 1,
  band: 0,
  hours: [11, 17],
  bg: "#0b0b0b",
  fg: "#d4af37",
  sub: "By appointment (or not)"
 }],
 byId: {}
};
