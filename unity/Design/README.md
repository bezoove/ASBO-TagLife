# Design

## OG-REGISTER.md: everything the rebuild must contain

The old browser game (v95, `www/index.html`) is the list of what the user wants: every idea, place, character, behaviour and line of dialogue. It is not a template for how things look or move: animation, poses, models, effects and feel are built new with Unity and the new assets, to be better than the old game (the user's rule). `OG-REGISTER.md` lists every item, each with:

- what the user asked for (their words, from chats and memory)
- the old game's modules behind it, each a file in `og/modules/` listing every behaviour (with its line in `og/game.pretty.js`), every line people say or the game shows, every state and mode, and every tuning number
- the old game's data tables verbatim in `og/data/` (dialogue sets, cast, items, drinks, jobs, cars...)

It also lists real brand names in the old text, which get made-up replacements (no real brands in the game).

## How a system gets built

1. Read its module files and the code at the listed lines in `og/game.pretty.js`.
2. Write its behaviour in plain English at the top of each module file (who does what, when, how often, what they say), so nothing depends on memory.
3. Build it with Unity's own systems and the new assets (NavMesh, Animator with motion capture, physics, Input System, audio): keep the idea, who does what and the words; improve the look, animation and feel. Old numbers are a starting point, not a rule.
4. Tick off every behaviour and line in the module file; only then mark the item done.
5. Check the frame-time readout before and after.

## Regenerating

    node + js-beautify + acorn:  npm i js-beautify acorn acorn-walk
    python3 unity/tools/og_extract.py    # old code -> og/game.pretty.js (delete og/defs.json first so it's re-parsed)
    python3 unity/tools/og_register.py   # register, module files, data tables
    python3 unity/tools/og_checklist_page.py <out.html> "<build label>"   # the checklist page
