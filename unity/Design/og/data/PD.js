// PD from the old game (v95), verbatim. Lines 127941-127951, 212530-212543, 212547-212547, 212549-212549, 212552-212552, 212564-212564, 212570-212570, 212579-212579, 212592-212592, 213422-213422 of game.pretty.js
     const PD = {
       mates: "Mates' rates at the bar (−10%)",
       steady: "Steady elbow (less sway)",
       grouping: "Tight grouping",
       moira: "Moira's tips on the fruit machine",
       gearDiscount: "Discount on darts gear",
       ronPrices: "Ron's mates' prices",
       tab: "Brenda will run a tab",
       protection: "Dave has your back",
       moiraTips: "Moira tips you the wink"
      },
 const PD = {
  pts: null,
  cap: 220,
  n: 0,
  pos: null,
  vel: null,
  life: null,
  size: null,
  al: null,
  gy: null,
  land: null,
  aS: null,
  aA: null
 };
  PD.cap = tierLow() ? 80 : tierMid() ? 160 : 220;
  PD.pos = new Float32Array(N * 3), PD.vel = new Float32Array(N * 3), PD.life = new Float32Array(N), PD.size = new Float32Array(N), PD.gy = new Float32Array(N), PD.land = new Uint8Array(N);
  g.setAttribute("position", pa), PD.aS = new THREE.BufferAttribute(new Float32Array(N), 1).setUsage(THREE.DynamicDrawUsage), g.setAttribute("aS", PD.aS), g.setDrawRange(0, 0), g.boundingSphere = new THREE.Sphere(V(0, 0, 0), 1e5);
  PD.pts = new THREE.Points(g, mat), PD.pts.frustumCulled = !1, PD.pts.visible = !1, PD.pts.renderOrder = 21, PD.pts.name = "goreDrops", Game.scene.add(PD.pts), PD.n = 0
  PD.pos[i * 3] = x, PD.pos[i * 3 + 1] = y, PD.pos[i * 3 + 2] = z, PD.vel[i * 3] = vx, PD.vel[i * 3 + 1] = vy, PD.vel[i * 3 + 2] = vz, PD.life[i] = 2.5, PD.size[i] = size, PD.gy[i] = gy || 0, PD.land[i] = spot ? 1 : 0, PD.aS.array[i] = size, PD.pts.visible = !0, G.stats.drops++, G.stats.maxDrops = Math.max(G.stats.maxDrops, PD.n)
  r && cam && (PD.pts.material.uniforms.uScale.value = r.domElement.height / (2 * Math.tan(cam.fov * Math.PI / 360)));
  PD.n = w, PD.pts.geometry.setDrawRange(0, w), PD.pts.visible = w > 0, PD.pts.geometry.attributes.position.needsUpdate = !0, PD.aS.needsUpdate = !0
  G.wsmears.length = 0, decalsClear(), PD.pts && (PD.n = 0, PD.pts.visible = !1, PD.pts.geometry.setDrawRange(0, 0)), D.mesh && D.mesh.parent !== Game.scene && Game.scene && Game.scene.add(D.mesh), PD.pts && PD.pts.parent !== Game.scene && Game.scene && Game.scene.add(PD.pts)
