// PUB from the old game (v95), verbatim. Lines 4483-4497 of game.pretty.js
const PUB = {
 H: 3,
 board: {
  x: -7,
  y: 1.73,
  z: -7.935
 },
 board2: {
  x: 5.7,
  y: 1.73,
  z: -7.935
 },
 OCHE: 2.37
};
PUB.ocheZ = PUB.board.z + PUB.OCHE, World.build = function(scene, quality) {
