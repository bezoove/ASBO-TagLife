# FacePhoto (old game, v95)

Part of: Character creator, haircut and photo face

Source in game.pretty.js: 136235-136814

## Behaviours (9)

- load (`load`, line 136250)
- save (`save`, line 136261)
- image (`image`, line 136269)
- affine (`affine`, line 136286)
- landmarks (`landmarks`, line 136300)
- bake (`bake`, line 136327)
- clear (`clear`, line 136483)
- apply (`apply`, line 136490)
- skin tone (`skinTone`, line 136495)

## Everything it says or shows (14)

- Your face from a photo
- Pick a clear, straight-on photo of your face in good light. You line up your eyes and mouth, and it goes on your character.
- Drag the dots onto the middle of each eye and the middle of your mouth.
- Show it on my character
- Face width
- Keep the 3D eyebrows
- Your photo stays on this device. It is not uploaded or put in your cloud save.
- Reading the photo…
- That's a HEIC photo, which this browser can't read. Save or share it as a JPEG first.
- Couldn't read that picture. Try a JPEG or PNG.
- Couldn't read that picture.
- Photo on. Drag the dots onto your eyes and mouth, then check the preview.
- Loaded. The face appears once the preview has built.
- Loaded, but this browser is not keeping the photo between sessions (private mode?).

## Labels and names (5)

- face photo not stored · btn sm · 600 12px system-ui,sans-serif · btn sm warn · fphint fpstatus

## States and modes

- type: eye, mouth

## Tuning numbers (11)

- _faceMs: 0
- bx1: 0
- by1: 0
- ntri: 0
- globalAlpha: 1
- drag: -1
- lineWidth: 2
- best: -1
- min: .4, .8, .6
- max: 1, 1.25, 1.3
- step: .05
