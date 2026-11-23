# casino_atlas.json + casino_props.json -> simple tab-separated files the Unity editor reads without a JSON library
import json, sys, os
d = sys.argv[1]
A = json.load(open(os.path.join(d, 'casino_atlas.json'))); M = json.load(open(os.path.join(d, 'casino_props.json')))
with open(os.path.join(d, 'casino_regions.txt'), 'w') as f:
    for k, v in A['regions'].items(): f.write(f"{k}\t{v[0]:.6f}\t{v[1]:.6f}\t{v[2]:.6f}\t{v[3]:.6f}\n")
    for k, v in A['palette'].items(): f.write(f"pal:{k}\t{v[0]:.6f}\t{v[1]:.6f}\t{v[0]:.6f}\t{v[1]:.6f}\n")
keys = ['kind', 'mass', 'label', 'action', 'seat', 'cash', 'drink', 'food', 'game', 'code', 'hinge', 'value']
with open(os.path.join(d, 'casino_props.txt'), 'w') as f:
    for k, v in M.items():
        s, c = v['size'], v['center']
        f.write('\t'.join([k] + [str(v.get(x, '')) for x in keys] + [f'{s[0]}', f'{s[1]}', f'{s[2]}', f'{c[0]}', f'{c[1]}', f'{c[2]}', str(v.get('tris', 0))]) + '\n')
print('ok', len(A['regions']), len(M))
