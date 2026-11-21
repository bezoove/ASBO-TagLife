"""Pull the old game's (www/index.html, v95) behaviour out into a register the Unity rebuild is ported from.

Writes unity/Design/og/: game.pretty.js (the old code, readable, with stable line numbers), one file per module
(methods with line numbers, every on-screen/spoken line, every tuning number, every state name), and the data
tables (dialogue, items, jobs, cast...) verbatim. Re-run after the old game changes:
    python3 unity/tools/og_extract.py   (needs node + js-beautify: npm i js-beautify)
"""
import re, os, json, subprocess, sys, collections
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, 'unity', 'Design', 'og')

def pretty_source():
    src = open(os.path.join(ROOT, 'www', 'index.html'), encoding='utf-8').read()
    js = '\n;\n'.join(m.group(1) for m in re.finditer(r'<script>(.*?)</script>', src, re.S))
    js = re.sub(r'data:[a-z/+\-]+;base64,[A-Za-z0-9+/=]{200,}', 'DATA', js)
    tmp = os.path.join(OUT, '_raw.js'); open(tmp, 'w').write(js)
    bt = os.environ.get('JSB', 'js-beautify')
    p = subprocess.run([bt, '-s', '1', '-w', '0', '--brace-style=collapse', tmp], capture_output=True, text=True)
    os.remove(tmp)
    if p.returncode: sys.exit(p.stderr)
    return p.stdout

def indent(l): return len(l) - len(l.lstrip(' '))

_DEFS = None
def defs():
    """Every named definition with its exact line range, from a real parse (og_defs.js)."""
    global _DEFS
    if _DEFS is None:
        p = os.path.join(OUT, 'defs.json')
        if not os.path.exists(p):
            r = subprocess.run(['node', '--max-old-space-size=6000', os.path.join(ROOT, 'unity', 'tools', 'og_defs.js'), os.path.join(OUT, 'game.pretty.js')], capture_output=True, text=True)
            if r.returncode: sys.exit(r.stderr)
            open(p, 'w').write(r.stdout)
        by = collections.defaultdict(list); fns = []
        for d in json.load(open(p)):
            if d['o']: by[d['o']].append(d)
            else: fns.append(d)
        fns.sort(key=lambda d: d['r'][0])
        _DEFS = (by, fns)
    return _DEFS

GLSL = re.compile(r'\b(vec[234]|gl_|uniform|varying|texture2D|#include|sampler2D|mat[34]|precision|float [a-z])\b')
CSSY = re.compile(r'(^|[;{ ])[a-z-]+:\s*[^ ]+;|px\b.*px\b|rgba?\(|^#[0-9a-f]{3,8}$|^\.[a-z]|<\w+[ >]|=>|\bfunction\b')
STR = re.compile(r'"((?:[^"\\\n]|\\.){3,})"|\'((?:[^\'\\\n]|\\.){3,})\'|`((?:[^`\\]|\\.){3,})`')

def texts(code):
    out = []
    for m in STR.finditer(code):
        s = next(g for g in m.groups() if g is not None)
        if len(s) > 600 or GLSL.search(s) or CSSY.search(s) or re.search(r'&&|\|\||typeof |===|!==|\bthis\.|\(\)', s): continue
        w = re.findall(r"[A-Za-z][a-z']+", s)
        if len(w) < 2 or not re.search(r'[a-z]{2,} [A-Za-z]', s): continue
        s = re.sub(r'\$\{[^}]*\}', '…', s).replace('\\n', ' ').replace('\\"', '"').replace("\\'", "'")
        out.append(s)
    return list(dict.fromkeys(out))

NUM = re.compile(r'\b([A-Za-z_][\w]*)\s*[:=]\s*(-?\d*\.?\d+(?:e-?\d+)?)\b(?![\w.(])')
STATE = re.compile(r'\.(state|mode|act|activity|phase|kind|goal|role|mood|stance|job|plan|type)\s*(?:===|!==|==|=)\s*"([a-zA-Z_][\w ]*)"')

JSKW = {'if', 'for', 'while', 'switch', 'return', 'catch', 'function', 'else', 'try', 'with', 'new', 'typeof', 'do'}

def module_spans(L, name, only=None):
    by, fns = defs()
    spans = []; out = []
    for d in by.get(name, []):
        k = d['k']
        if only is not None and (not k or not only(k.split('.')[0])): continue
        spans.append(tuple(d['r']))
        if k and d['f']: out.append((k, d['r'][0]))
    if only is None and len(out) < 3:  # a closure-style module: its inner functions are its behaviours
        for a, b in [tuple(d['r']) for d in by.get(name, []) if not d['k']]:
            for f in fns:
                if a < f['r'][0] <= b and f['k'] not in JSKW and len(f['k']) > 2: out.append((f['k'], f['r'][0]))
    spans.sort(); merged = []
    for a, b in spans:
        if merged and a <= merged[-1][1] + 1: merged[-1] = (merged[-1][0], max(merged[-1][1], b))
        else: merged.append((a, b))
    return merged, list(dict.fromkeys(out))

def analyse(L, name, only=None):
    spans, defs = module_spans(L, name, only)
    code = '\n'.join('\n'.join(L[a:b + 1]) for a, b in spans)
    nums = collections.OrderedDict()
    for k, v in NUM.findall(code):
        if len(k) < 3 or k in ('length', 'opacity', 'roughness', 'metalness', 'transparent'): continue
        nums.setdefault(k, [])
        if v not in nums[k] and len(nums[k]) < 6: nums[k].append(v)
    states = collections.OrderedDict()
    for f, v in STATE.findall(code): states.setdefault(f, []); v not in states[f] and states[f].append(v)
    return dict(spans=spans, methods=defs, lines=sum(b - a + 1 for a, b in spans), texts=texts(code), nums=nums, states=states)

DEBUG = re.compile(r'\b(build|init|update|tick|step|spawn|hook|listener|fail|failed|error|bake|cull|dispose|mats|patch|resources|apply|restore|furnish|interior|builder|sync|flock|split|crunch|refill|audit|listener|ms|enter|leave|act)\b')

def split_texts(ts):
    said, labels = [], []
    for t in ts:
        low = t == t.lower()
        if low and DEBUG.search(t): continue
        if low and len(t.split()) <= 4: labels.append(t)
        else: said.append(t)
    return said, labels

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    P = pretty_source(); open(os.path.join(OUT, 'game.pretty.js'), 'w').write(P)
    print('pretty lines', P.count('\n'))
