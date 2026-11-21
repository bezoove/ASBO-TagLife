# Nav (old game, v95)

Part of: the old game's own graphics and engine code (Unity does this its own way)

Source in game.pretty.js: 9953-9953, 28924-28924, 40286-40286, 94914-95070, 96562-96638, 96640-96675, 96692-96692, 96696-96696, 96703-96703, 96721-96723, 96833-96851, 96855-96888, 151030-151032, 151165-151178, 152733-152744, 160364-160388, 160392-160399, 160403-160440, 166105-166107, 168488-168509, 202717-202723, 208223-208235, 208239-208239, 208243-208243, 208289-208289, 208293-208293, 209601-209601, 216611-216618

## Behaviours (14)

- build (`build`, line 94921, 96562, 96855, 151030, 151165, 152733)
- cell (`cell`, line 94947)
- free (`free`, line 94950)
- center (`center`, line 94953)
- nearest free (`nearestFree`, line 94959)
- los (`los`, line 94973, 96721)
- find (`find`, line 94983, 96640, 96833, 160364, 208223)
- label (`label`, line 96564, 202717)
- region at (`regionAt`, line 96601)
- wall between (`wallBetween`, line 96604)
- anchor (`anchor`, line 96620)
- spot ok (`spotOK`, line 96657)
- unreachable spots (`unreachableSpots`, line 96664)
- unwedge (`unwedge`, line 96670)

## States and modes

- type: obb, circle

## Tuning numbers (2)

- iter: 0
- _goalR: 0
