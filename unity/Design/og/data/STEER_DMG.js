// STEER_DMG from the old game (v95), verbatim. Lines 165156-165171 of game.pretty.js
const STEER_DMG = {
 FRONT_V: 16,
 FRONT_START: .6,
 PULL_MAX: .06,
 LOCK_LOSS: .2,
 BENT_FRONT_V: 13,
 BENT_SIDE_V: 12,
 BENT_PULL: .025,
 BURST_V: 15,
 BURST_SURE_V: 20,
 BURST_P: .5,
 FLAT_PULL: .05,
 FLAT_GRIP: .82,
 ENGINE_START: 70
};
window.STEER_DMG = STEER_DMG, (function() {
