# World:light (old game, v95)

Part of: Props everywhere

Source in game.pretty.js: 3512-3512, 6510-6794, 6800-6800, 6809-6818, 6863-6919, 7209-7229, 7884-7884, 9436-9438, 10239-10262, 11205-11273, 11275-11277, 14157-14159, 14161-14185, 17242-17311, 19324-19326, 19825-19841, 19852-19854, 20293-20302, 20308-20314, 24045-24157, 25004-25008, 25012-25070, 27032-27050, 28842-28852, 28908-28914, 28982-29003, 45492-45499, 49171-49178, 62625-62652, 62654-62666, 63360-63379, 66904-66912, 113368-113375, 113377-113382, 125534-125541, 147217-147229, 147938-148056, 148058-148064, 155894-155924, 156116-156123 …

## Behaviours (14)

- build lights (`buildLights`, line 6510, 11275, 19825, 113368)
- update lights (`updateLights`, line 6763, 14157, 19852, 20308, 28982, 45492)
- add glow (`addGlow`, line 6809)
- bake at (`bakeAt`, line 6863)
- bake lighting (`bakeLighting`, line 6902)
- bake item (`bakeItem`, line 6906)
- low lamp (`lowLamp`, line 7209)
- o lamp post (`oLampPost`, line 10239, 168513, 200512)
- build outside lights (`buildOutsideLights`, line 11205, 14161, 20293, 24045, 25004, 28908)
- car lamp tex (`carLampTex`, line 17242)
- estate lights (`estateLights`, line 25012, 27032)
- town lamp (`townLamp`, line 28842)
- fl party lights (`flPartyLights`, line 63360)
- door light (`doorLight`, line 155894)

## Labels and names (5)

- chaos lights · house lights · polish look · weather frame · night light

## States and modes

- mode: pool, title
- type: BoxGeometry

## Tuning numbers (23)

- left: -11, -15, -12
- right: 11, 15, 12
- top: 11, 9.5, 12
- bottom: -11, -9.5, -12
- near: 5, .2, 1
- far: 30, 3.5, 95
- bias: -8e-4, -4e-4, -6e-4
- normalBias: .02, .03
- radius: 3, 2
- _lightFade: 1, 0
- intensity: 0
- rot: 0
- drop: .5
- lineWidth: 2, 4, 10, 12, 3
- renderOrder: -20, 6
- night: 1
- emissiveIntensity: .42
- _te: 1
- party: 1
- _hlErr: 1
- envMapIntensity: .28
- _err: 1
- _fe: 1
