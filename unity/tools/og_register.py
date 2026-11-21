"""Builds the old-game register from unity/Design/og/game.pretty.js (made by og_extract.py) and og_map.py:
  unity/Design/og/modules/<Module>.md   every behaviour (with source lines), every line said or shown, tuning numbers, states
  unity/Design/og/data/<TABLE>.js       the old game's data tables, verbatim
  unity/Design/OG-REGISTER.md           the checklist, each item with the modules and wishes behind it
  unity/Design/og/register.json         the same, for the checklist page
    python3 unity/tools/og_register.py
"""
import os, re, json, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import og_extract as X
import og_map as MAP

D = os.path.join(X.ROOT, 'unity', 'Design')
OG = os.path.join(D, 'og')
L = open(os.path.join(OG, 'game.pretty.js')).read().split('\n')

def human(n):
    n = re.sub(r'^_+', '', n)
    w = re.sub(r'([a-z0-9])([A-Z])', r'\1 \2', n).replace('_', ' ').lower()
    return w

cache = {}
BRANDS = re.compile(r'\b(Astra|Cavalier|Golf|Fiesta|Corsa|Range Rover|Escort|Mondeo|Clio|Micra|Transit|Sierra|Capri|Polo|Punto|Focus|Nova|Metro|Maestro|BMW|Audi|Ford|Vauxhall|Volkswagen|VW|Mercedes|Nissan|Toyota|Honda|Peugeot|Renault|Fiat|Jaguar|Land Rover|Volvo|Skoda|Kia|Hyundai|Citroen|Rover|Subaru|Porsche|Tesco|Asda|Sainsbury\'?s?|Greggs|McDonald\'?s|KFC|Lidl|Aldi|Iceland|Argos|Boots|Primark|Poundland|Spar|Stella|Carling|Guinness|Foster\'?s|Strongbow|Heineken|Budweiser|Coke|Coca[- ]Cola|Pepsi|Red Bull|Walkers|Smirnoff|Jameson|Lambrini|Buckfast|Bacardi|Baileys|Jägermeister|Jager|Monster|Lucozade|Irn[- ]Bru|iPhone|Samsung|Nokia|PlayStation|Xbox|Nike|Adidas|Lacoste|Burberry|Stone Island|Reebok|Umbro|Kappa|Fila|Rolex|Spotify|YouTube|Facebook|Instagram|TikTok|Uber|Deliveroo|Just Eat|Amazon|eBay|Gucci|Calor|Rizla|Benson|Lambert|Marlboro|Mars|Snickers|Twix|Haribo|Pot Noodle|Hovis|Kellogg|Heinz|Bisto|Oxo|Marmite|Jaffa|Hobnob|Wotsits|Quavers|Monster Munch|Pringles|Doritos)\b')
def get(ref):
    if ref in cache: return cache[ref]
    if ':' in ref:
        mod, grp = ref.split(':')
        rx = re.compile(MAP.GROUPS[mod][grp])
        a = X.analyse(L, mod, lambda m: bool(rx.search(m)))
    elif ref in MAP.GROUPS:
        rxs = [re.compile(v) for v in MAP.GROUPS[ref].values()]
        a = X.analyse(L, ref, lambda m: not any(r.search(m) for r in rxs))
    else:
        a = X.analyse(L, ref)
    a['said'], a['labels'] = X.split_texts(a['texts'])
    cache[ref] = a
    return a

def fname(ref): return ref.replace(':', '-')

def write_module(ref, a, items):
    os.makedirs(os.path.join(OG, 'modules'), exist_ok=True)
    o = [f'# {ref} (old game, v95)', '']
    if items: o += ['Part of: ' + '; '.join(items), '']
    o += ['Source in game.pretty.js: ' + ', '.join(f'{x + 1}-{y + 1}' for x, y in a['spans'][:40]) + (' …' if len(a['spans']) > 40 else ''), '']
    if a['methods']:
        seen = {}
        for n, i in a['methods']: seen.setdefault(n, []).append(i + 1)
        o += [f'## Behaviours ({len(seen)})', '']
        o += [f'- {human(n)} (`{n}`, line {", ".join(map(str, v[:6]))})' for n, v in seen.items()] + ['']
    if a['said']:
        o += [f'## Everything it says or shows ({len(a["said"])})', ''] + [f'- {t}' for t in a['said']] + ['']
    if a['labels']:
        o += [f'## Labels and names ({len(a["labels"])})', ''] + ['- ' + ' · '.join(a['labels'][k:k + 8]) for k in range(0, len(a['labels']), 8)] + ['']
    if a['states']:
        o += ['## States and modes', ''] + [f'- {k}: {", ".join(v)}' for k, v in a['states'].items()] + ['']
    if a['nums']:
        o += [f'## Tuning numbers ({len(a["nums"])})', ''] + [f'- {k}: {", ".join(v)}' for k, v in a['nums'].items()] + ['']
    open(os.path.join(OG, 'modules', fname(ref) + '.md'), 'w').write('\n'.join(o))

def write_table(name, a):
    os.makedirs(os.path.join(OG, 'data'), exist_ok=True)
    code = '\n'.join('\n'.join(L[x:y + 1]) for x, y in a['spans'])
    open(os.path.join(OG, 'data', name + '.js'), 'w').write(f'// {name} from the old game (v95), verbatim. Lines {", ".join(f"{x + 1}-{y + 1}" for x, y in a["spans"])} of game.pretty.js\n' + code + '\n')

def main():
    reg = []; used = {}
    for sec, items in MAP.SECTIONS:
        out = []
        for st, title, note, mods, tables, wishes in items:
            it = dict(s=st, t=title, n=note, w=wishes, m=[])
            for ref in mods:
                a = get(ref)
                if not a['spans']: print('no code for', ref); continue
                used.setdefault(ref, []).append(title)
                it['m'].append(ref)
            out.append(it)
        reg.append((sec, out))
    # everything not on the checklist still gets recorded
    tech = []
    for ref in MAP.TECH:
        if ref in used: continue
        a = get(ref)
        if a['spans']: tech.append(ref); used.setdefault(ref, []).append('the old game\'s own graphics and engine code (Unity does this its own way)')
    for ref, items in used.items():
        a = get(ref)
        if ':' not in ref and ref.replace('_', '').isupper() and len(a['methods']) < 3 and ref not in MAP.TECH: write_table(ref, a)
        write_module(ref, a, items)
    # per item summary + page data
    J = []; md = ['# Old game register', '',
                  'Every item the Unity rebuild has to contain, with the old game\'s code behind it (the behaviour spec: its exact lines, numbers, states and rules) and what you asked for.',
                  'Each module file lists every behaviour with its line in og/game.pretty.js, everything it says or shows, its states and its tuning numbers.',
                  'Built by unity/tools/og_extract.py and og_register.py from www/index.html (v95).', '']
    tot = dict(said=0, beh=0, nums=0)
    for sec, items in reg:
        md += [f'## {sec}', '']; js = []
        for it in items:
            said, beh, nums, states, labels = [], [], 0, [], []
            for ref in it['m']:
                a = get(ref)
                said += [t for t in a['said'] if t not in said]
                beh += [human(n) for n, _ in a['methods'] if human(n) not in beh]
                nums += len(a['nums']); labels += [t for t in a['labels'] if t not in labels]
                states += [f'{k}: {", ".join(v)}' for k, v in a['states'].items()]
            tot['said'] += len(said); tot['beh'] += len(beh); tot['nums'] += nums
            mark = {'done': '[x]', 'part': '[~]', 'todo': '[ ]'}[it['s']]
            md.append(f'- {mark} **{it["t"]}**' + (f': {it["n"]}' if it['n'] else ''))
            for w in it['w']: md.append(f'  - You asked: {w}')
            if it['m']:
                md.append('  - Old game: ' + ', '.join(f'[{r}](og/modules/{fname(r)}.md)' for r in it['m']) + f' ({len(beh)} behaviours, {len(said)} lines said or shown, {nums} tuning numbers)')
            js.append(dict(s=it['s'], t=it['t'], n=it['n'], w=it['w'], m=it['m'], said=said, beh=beh, nums=nums, states=states[:60], labels=labels[:120]))
        md.append(''); J.append(dict(name=sec, items=js))
    md += ['## Also recorded', '', 'The old game\'s own graphics and engine code, which Unity does its own way: ' + ', '.join(f'[{r}](og/modules/{fname(r)}.md)' for r in tech), '']
    # real names in the old game's text: the rebuild has no real brands, so these get made-up replacements
    hits = {}
    for sec in J:
        for it in sec['items']:
            for t in it['said'] + it['labels']:
                for b in BRANDS.findall(t): hits.setdefault(b, set()).add(t)
    md += ['## Real names in the old game\'s text (replace with made-up ones)', '']
    md += [f'- **{b}**: ' + ' / '.join(sorted(v)[:4]) for b, v in sorted(hits.items())] + ['']
    open(os.path.join(D, 'OG-REGISTER.md'), 'w').write('\n'.join(md))
    brands = {b: sorted(v)[:6] for b, v in hits.items()}
    json.dump(dict(sections=J, totals=tot, tech=tech, brands=brands), open(os.path.join(OG, 'register.json'), 'w'), ensure_ascii=False)
    print('items', sum(len(s['items']) for s in J), tot, 'modules', len(used))

if __name__ == '__main__':
    main()
